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
*/

using System;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class TickTests
    {
        [Test]
        public void ConstructsFromLine()
        {
            const string line = "15093000,1456300,100,P,T,0";

            var baseDate = new DateTime(2013, 10, 08);
            var tick = new Tick(Symbols.SPY, line, baseDate);

            var ms = (tick.Time - baseDate).TotalMilliseconds;
            Assert.AreEqual(15093000, ms);
            Assert.AreEqual(1456300, tick.LastPrice * 10000m);
            Assert.AreEqual(100, tick.Quantity);
            Assert.AreEqual("P", tick.Exchange);
            Assert.AreEqual("T", tick.SaleCondition);
            Assert.AreEqual(false, tick.Suspicious);
        }

        [Test]
        public void ConstructsFromLineWithDecimalTimestamp()
        {
            const string line = "18000677.3,3669.12,0.0040077,3669.13,3.40618718";

            var config = new SubscriptionDataConfig(
                typeof(Tick), Symbols.BTCUSD, Resolution.Tick, TimeZones.Utc, TimeZones.Utc,
                false, false, false, false, TickType.Quote);
            var baseDate = new DateTime(2019, 1, 15);

            var tick = new Tick(config, line, baseDate);

            var ms = (tick.Time - baseDate).TotalMilliseconds;
            Assert.AreEqual(18000677, ms);
            Assert.AreEqual(3669.12, tick.BidPrice);
            Assert.AreEqual(0.0040077, tick.BidSize);
            Assert.AreEqual(3669.13, tick.AskPrice);
            Assert.AreEqual(3.40618718, tick.AskSize);
        }

        [Test]
        public void ReadsFuturesTickFromLine()
        {
            const string line = "86399572,52.62,5,usa,,0,False";

            var baseDate = new DateTime(2013, 10, 08);
            var symbol = Symbol.CreateFuture(Futures.Energies.CrudeOilWTI, QuantConnect.Market.NYMEX, new DateTime(2017, 2, 28));
            var config = new SubscriptionDataConfig(typeof(Tick), symbol, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
            var tick = new Tick(config, line, baseDate);

            var ms = (tick.Time - baseDate).TotalMilliseconds;
            Assert.AreEqual(86399572, ms);
            Assert.AreEqual(52.62, tick.LastPrice);
            Assert.AreEqual(5, tick.Quantity);
            Assert.AreEqual("usa", tick.Exchange);
            Assert.AreEqual("", tick.SaleCondition);
            Assert.AreEqual(false, tick.Suspicious);
        }

        [TestCase("14400135,0,0,1680000,400,NASDAQ,00000001,0", 0, 0, 168, 400)]
        [TestCase("14400135,10000,10,0,0,NASDAQ,00000001,0", 1, 10, 0, 0)]
        [TestCase("14400135,10000,10,20000,20,NASDAQ,00000001,0", 1, 10, 2, 20)]
        public void EquityQuoteTick(string line, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            var baseDate = new DateTime(2013, 10, 08);
            var config = new SubscriptionDataConfig(typeof(Tick),
                Symbols.SPY,
                Resolution.Tick,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false,
                false,
                TickType.Quote);
            var tick = new Tick(config, line, baseDate);

            var expectedValue = (askPrice + bidPrice) / 2;
            if (askPrice == 0 || bidPrice == 0)
            {
                expectedValue = askPrice + bidPrice;
            }

            var ms = (tick.Time - baseDate).TotalMilliseconds;
            Assert.AreEqual(14400135, ms);
            Assert.AreEqual(expectedValue, tick.Value);
            Assert.AreEqual(expectedValue, tick.LastPrice);
            Assert.AreEqual(0, tick.Quantity);
            Assert.AreEqual(askPrice, tick.AskPrice);
            Assert.AreEqual(askSize, tick.AskSize);
            Assert.AreEqual(bidPrice, tick.BidPrice);
            Assert.AreEqual(bidSize, tick.BidSize);
            Assert.AreEqual("NASDAQ", tick.Exchange);
            Assert.AreEqual("00000001", tick.SaleCondition);
            Assert.IsFalse(tick.Suspicious);
        }
    }
}
