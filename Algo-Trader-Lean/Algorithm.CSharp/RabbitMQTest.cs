/*
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
*/

using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash. This is a skeleton
    /// framework you can use for designing an algorithm.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class RabbitMQTest : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private JObject jsonmessage;
        private TradeBars tradeBars;
        private List<string> equityList = new List<string> { };

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 01, 01);  //Set Start Date
            SetEndDate(2019, 01, 31);    //Set End Date
            SetCash(1000000);             //Set Strategy Cash

            // Create new connection factory
            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Set up queue for RabbitMQ
                channel.QueueDeclare(queue: "backtest",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);


                // Create object for rabbitMQ producer
                var consumer = new EventingBasicConsumer(channel);

                // Get single call to clear queue
                BasicGetResult result = channel.BasicGet("backtest", false);
                if (result == null)
                {
                    // No message available at this time.
                    //SetStartDate(2012, 10, 07);  //Set Start Date
                    //SetEndDate(2019, 10, 11);    //Set End Date
                    //SetCash(1000000);             //Set Strategy Cash
                }
                else
                {
                    IBasicProperties props = result.BasicProperties;
                    byte[] body = result.Body;
                    var message = Encoding.UTF8.GetString(body);
                    jsonmessage = JObject.Parse(message);


                    // TODO change this to parse start dates from JSON
                    //SetStartDate(2018, 10, 07);  //Set Start Date
                    //SetEndDate(2019, 10, 11);    //Set End Date
                    //SetCash(1000000);             //Set Strategy Cash


                    foreach (string element in jsonmessage["timeFrames"][0]["securities"].ToObject<List<string>>())
                    {
                        AddEquity(element, Resolution.Daily);
                        equityList.Add(element);
                        Debug(element);
                    }
                }


                // Set up consumer message handler
                consumer.Received += (model, ea) =>
                {
                    //Debug("THIS COMSUMER RECIEVE IS BEING CALLED");
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    jsonmessage = JObject.Parse(message);
                };

                channel.BasicConsume(queue: "backtest",
                                        autoAck: true,
                                        consumer: consumer);

                //channel.QueuePurge("backtest");
                channel.Close();
            }

            //symbol = QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA);


            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.

            // There are other assets with similar methods. See "Selecting Options" etc for more details.
            // AddFuture, AddForex, AddCfd, AddOption
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            tradeBars = data.Bars;
            if (!Portfolio.Invested)
            {
                foreach (string element in equityList)
                {
                    Debug(element);
                    //TODO change purchase from 0.1 to values parsed from JSON
                    SetHoldings(element, 0.1);
                }

                //SetHoldings(_tsla, 1);
                Debug("Purchased Stock");
            }

        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "263.153%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.663%"},
            {"Sharpe Ratio", "4.824"},
            {"Probabilistic Sharpe Ratio", "66.954%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0.996"},
            {"Annual Standard Deviation", "0.219"},
            {"Annual Variance", "0.048"},
            {"Information Ratio", "-4.864"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "1.061"},
            {"Total Fees", "$3.26"},
            {"Fitness Score", "0.248"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "94.3"},
            {"Portfolio Turnover", "0.248"},
            {"Total Insights Generated", "1"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "1"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}