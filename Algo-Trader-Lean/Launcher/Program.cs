﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QuantConnect.Lean.Launcher
{
    public class Program
    {
        private const string _collapseMessage = "Unhandled exception breaking past controls and causing collapse of algorithm node. This is likely a memory leak of an external dependency or the underlying OS terminating the LEAN engine.";

        static Program()
        {
            AppDomain.CurrentDomain.AssemblyLoad += (sender, e) =>
            {
                if (e.LoadedAssembly.FullName.ToLowerInvariant().Contains("python"))
                {
                    Log.Trace($"Python for .NET Assembly: {e.LoadedAssembly.GetName()}");
                }
            };
        }

        static void Main(string[] args)
        {
            Console.WriteLine("THIS WORKS");

            // Create new connection factory
            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

                //Initialize:
                var mode = "RELEASE";
#if DEBUG
            mode = "DEBUG";
#endif

            if (OS.IsWindows)
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }

            // expect first argument to be config file name
            if (args.Length > 0)
            {
                Config.MergeCommandLineArgumentsWithConfiguration(LeanArgumentParser.ParseArguments(args));
            }

            var environment = Config.Get("environment");
            var liveMode = Config.GetBool("live-mode");
            Log.DebuggingEnabled = Config.GetBool("debug-mode");
            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

            //Name thread for the profiler:
            Thread.CurrentThread.Name = "Algorithm Analysis Thread";
            Log.Trace("Engine.Main(): LEAN ALGORITHMIC TRADING ENGINE v" + Globals.Version + " Mode: " + mode + " (" + (Environment.Is64BitProcess ? "64" : "32") + "bit)");
            Log.Trace("Engine.Main(): Started " + DateTime.Now.ToShortTimeString());

            //Import external libraries specific to physical server location (cloud/local)
            LeanEngineSystemHandlers leanEngineSystemHandlers;
            try
            {
                leanEngineSystemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            //Setup packeting, queue and controls system: These don't do much locally.
            leanEngineSystemHandlers.Initialize();


            LeanEngineAlgorithmHandlers leanEngineAlgorithmHandlers;
            try
            {
                leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            //-> Pull job from QuantConnect job queue, or, pull local build:
            string assemblyPath;
            var job = leanEngineSystemHandlers.JobQueue.NextJob(out assemblyPath);

            if (job == null)
            {
                const string jobNullMessage = "Engine.Main(): Sorry we could not process this algorithm request.";
                Log.Error(jobNullMessage);
                throw new ArgumentException(jobNullMessage);
            }

            // if the job version doesn't match this instance version then we can't process it
            // we also don't want to reprocess redelivered jobs
            if (VersionHelper.IsNotEqualVersion(job.Version) || job.Redelivered)
            {
                Log.Error("Engine.Run(): Job Version: " + job.Version + "  Deployed Version: " + Globals.Version + " Redelivered: " + job.Redelivered);
                //Tiny chance there was an uncontrolled collapse of a server, resulting in an old user task circulating.
                //In this event kill the old algorithm and leave a message so the user can later review.
                leanEngineSystemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, _collapseMessage);
                leanEngineSystemHandlers.Notify.SetAuthentication(job);
                leanEngineSystemHandlers.Notify.Send(new RuntimeErrorPacket(job.UserId, job.AlgorithmId, _collapseMessage));
                leanEngineSystemHandlers.JobQueue.AcknowledgeJob(job);
                return;
            }

            if (liveMode)
            {
                try
                {
                    WorkerThread curThread = new WorkerThread();
                    //WorkerThread curThread = WorkerThread.Instance;

                    var algorithmManager = new AlgorithmManager(liveMode, job);

                    leanEngineSystemHandlers.LeanManager.Initialize(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, job, algorithmManager);

                    var engine = new Engine.Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, liveMode);

                    engine.Run(job, algorithmManager, assemblyPath, curThread);

                    int threadId = curThread._workerThread.GetHashCode();


                    //Console.WriteLine(curThread._workerThread.);


                    Log.Trace("\n\n\n\n");
                }

                finally
                {
                    //Delete the message from the job queue:
                    leanEngineSystemHandlers.JobQueue.AcknowledgeJob(job);
                    Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);

                    // clean up resources
                    leanEngineSystemHandlers.Dispose();
                    leanEngineAlgorithmHandlers.Dispose();
                    Log.LogHandler.Dispose();

                    //Log.Trace("Program.Main(): Exiting Lean...");

                    //Environment.Exit(0);
                }

            }
            else
            {
                // Set up queue for RabbitMQ
                channel.QueueDeclare(queue: "backtestTrigger",
                                            durable: false,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null);

                // Create object for rabbitMQ consumer
                var consumer = new EventingBasicConsumer(channel);

                // Set up consumer message handler
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    JObject jsonmessage = JObject.Parse(message);

                    try
                    {
                        WorkerThread curThread = new WorkerThread();
                        //WorkerThread curThread = WorkerThread.Instance;

                        var algorithmManager = new AlgorithmManager(liveMode, job);

                        leanEngineSystemHandlers.LeanManager.Initialize(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, job, algorithmManager);

                        var engine = new Engine.Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, liveMode);

                        engine.Run(job, algorithmManager, assemblyPath, curThread);

                        int threadId = curThread._workerThread.GetHashCode();


                        //Console.WriteLine(curThread._workerThread.);


                        Log.Trace("\n\n\n\n");
                    }

                    finally
                    {
                        //Delete the message from the job queue:
                        leanEngineSystemHandlers.JobQueue.AcknowledgeJob(job);
                        Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);

                        // clean up resources
                        leanEngineSystemHandlers.Dispose();
                        leanEngineAlgorithmHandlers.Dispose();
                        Log.LogHandler.Dispose();

                        //Log.Trace("Program.Main(): Exiting Lean...");

                        //Environment.Exit(0);
                    }

                    //foreach (string element in jsonmessage["timeFrames"])
                    //{
                    //    Log.Trace(element);
                    //}
                };

                channel.BasicConsume(queue: "backtestTrigger", autoAck: true, consumer: consumer);
            }
        }
    }
}