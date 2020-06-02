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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.DataFeeds.WorkScheduling;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Utilities related to data <see cref="Subscription"/>
    /// </summary>
    public static class SubscriptionUtils
    {
        /// <summary>
        /// Creates a new <see cref="Subscription"/> which will directly consume the provided enumerator
        /// </summary>
        /// <param name="request">The subscription data request</param>
        /// <param name="enumerator">The data enumerator stack</param>
        /// <returns>A new subscription instance ready to consume</returns>
        public static Subscription Create(
            SubscriptionRequest request,
            IEnumerator<BaseData> enumerator)
        {
            var exchangeHours = request.Security.Exchange.Hours;
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var dataEnumerator = new SubscriptionDataEnumerator(
                request.Configuration,
                exchangeHours,
                timeZoneOffsetProvider,
                enumerator
            );
            return new Subscription(request, dataEnumerator, timeZoneOffsetProvider);
        }


        /// <summary>
        /// Setups a new <see cref="Subscription"/> which will consume a blocking <see cref="EnqueueableEnumerator{T}"/>
        /// that will be feed by a worker task
        /// </summary>
        /// <param name="request">The subscription data request</param>
        /// <param name="enumerator">The data enumerator stack</param>
        /// <returns>A new subscription instance ready to consume</returns>
        public static Subscription CreateAndScheduleWorker(
            SubscriptionRequest request,
            IEnumerator<BaseData> enumerator)
        {
            var exchangeHours = request.Security.Exchange.Hours;
            var enqueueable = new EnqueueableEnumerator<SubscriptionData>(true);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var subscription = new Subscription(request, enqueueable, timeZoneOffsetProvider);

            Func<int, bool> produce = (workBatchSize) =>
            {
                try
                {
                    var count = 0;
                    while (enumerator.MoveNext())
                    {
                        // subscription has been removed, no need to continue enumerating
                        if (enqueueable.HasFinished)
                        {
                            enumerator.DisposeSafely();
                            return false;
                        }

                        var subscriptionData = SubscriptionData.Create(subscription.Configuration, exchangeHours,
                            subscription.OffsetProvider, enumerator.Current);

                        // drop the data into the back of the enqueueable
                        enqueueable.Enqueue(subscriptionData);

                        count++;
                        // stop executing if added more data than the work batch size, we don't want to fill the ram
                        if (count > workBatchSize)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception, $"Subscription worker task exception {request.Configuration}.");
                }

                // we made it here because MoveNext returned false or we exploded, stop the enqueueable
                enqueueable.Stop();
                // we have to dispose of the enumerator
                enumerator.DisposeSafely();
                return false;
            };

            WeightedWorkScheduler.Instance.QueueWork(produce,
                // if the subscription finished we return 0, so the work is prioritized and gets removed
                () => enqueueable.HasFinished ? 0 : enqueueable.Count);

            return subscription;
        }
    }
}
