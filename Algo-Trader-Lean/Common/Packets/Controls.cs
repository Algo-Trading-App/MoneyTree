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

using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Specifies values used to control algorithm limits
    /// </summary>
    public class Controls
    {
        /// <summary>
        /// The maximum number of minute symbols
        /// </summary>
        [JsonProperty(PropertyName = "iMinuteLimit")]
        public int MinuteLimit;

        /// <summary>
        /// The maximum number of second symbols
        /// </summary>
        [JsonProperty(PropertyName = "iSecondLimit")]
        public int SecondLimit;

        /// <summary>
        /// The maximum number of tick symbol
        /// </summary>
        [JsonProperty(PropertyName = "iTickLimit")]
        public int TickLimit;

        /// <summary>
        /// Ram allocation for this backtest in MB
        /// </summary>
        [JsonProperty(PropertyName = "iRamAllocation")]
        public int RamAllocation;

        /// <summary>
        /// The user backtesting log limit
        /// </summary>
        [JsonProperty(PropertyName = "iBacktestLogLimit")]
        public int BacktestLogLimit;

        /// <summary>
        /// The daily log limit of a user
        /// </summary>
        [JsonProperty(PropertyName = "iDailyLogLimit")]
        public int DailyLogLimit;

        /// <summary>
        /// The remaining log allowance for a user
        /// </summary>
        [JsonProperty(PropertyName = "iRemainingLogAllowance")]
        public int RemainingLogAllowance;

        /// <summary>
        /// Maximimum number of insights we'll store and score in a single backtest
        /// </summary>
        [JsonProperty(PropertyName = "iBacktestingMaxInsights")]
        public int BacktestingMaxInsights;

        /// <summary>
        /// Maximimum number of orders we'll allow in a backtest.
        /// </summary>
        [JsonProperty(PropertyName = "iBacktestingMaxOrders")]
        public int BacktestingMaxOrders { get; set; }

        /// <summary>
        /// Limits the amount of data points per chart series. Applies only for backtesting
        /// </summary>
        [JsonProperty(PropertyName = "iMaximumDataPointsPerChartSeries")]
        public int MaximumDataPointsPerChartSeries;

        /// <summary>
        /// The amount seconds used for timeout limits
        /// </summary>
        [JsonProperty(PropertyName = "iSecondTimeOut")]
        public int SecondTimeOut;

        /// <summary>
        /// Sets parameters used for determining the behavior of the leaky bucket algorithm that
        /// controls how much time is available for an algorithm to use the training feature.
        /// </summary>
        [JsonProperty(PropertyName = "oTrainingLimits")]
        public LeakyBucketControlParameters TrainingLimits;

        /// <summary>
        /// Limits the total size of storage used by <see cref="IObjectStore"/>
        /// </summary>
        [JsonProperty(PropertyName = "storageLimitMB")]
        public int StorageLimitMB;

        /// <summary>
        /// Limits the number of files to be held under the <see cref="IObjectStore"/>
        /// </summary>
        [JsonProperty(PropertyName = "storageFileCountMB")]
        public int StorageFileCount;

        /// <summary>
        /// The interval over which the <see cref="IObjectStore"/> will persistence the contents of
        /// the object store
        /// </summary>
        [JsonProperty(PropertyName = "persistenceIntervalSeconds")]
        public int PersistenceIntervalSeconds;

        /// <summary>
        /// Gets list of streaming data permissions
        /// </summary>
        [JsonProperty(PropertyName = "streamingDataPermissions")]
        public HashSet<string> StreamingDataPermissions;

        /// <summary>
        /// Initializes a new default instance of the <see cref="Controls"/> class
        /// </summary>
        public Controls()
        {
            MinuteLimit = 500;
            SecondLimit = 100;
            TickLimit = 30;
            RamAllocation = 1024;
            BacktestLogLimit = 10000;
            BacktestingMaxOrders = int.MaxValue;
            DailyLogLimit = 3000000;
            RemainingLogAllowance = 10000;
            BacktestingMaxInsights = 10000;
            MaximumDataPointsPerChartSeries = 4000;
            SecondTimeOut = 300;
            StorageLimitMB = Config.GetInt("storage-limit-mb", 5);
            StorageFileCount = Config.GetInt("storage-file-count", 100);
            PersistenceIntervalSeconds = Config.GetInt("persistence-interval-seconds", 5);

            // initialize to default leaky bucket values in case they're not specified
            TrainingLimits = new LeakyBucketControlParameters();

            StreamingDataPermissions = new HashSet<string>();
        }

        /// <summary>
        /// Gets the maximum number of subscriptions for the specified resolution
        /// </summary>
        public int GetLimit(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    return TickLimit;

                case Resolution.Second:
                    return SecondLimit;

                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                default:
                    return MinuteLimit;
            }
        }
    }
}
