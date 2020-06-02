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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Oanda REST API base class
    /// </summary>
    public abstract class OandaRestApiBase : Brokerage, IDataQueueHandler
    {
        private static readonly TimeSpan SubscribeDelay = TimeSpan.FromMilliseconds(250);
        private DateTime _lastSubscribeRequestUtcTime = DateTime.MinValue;
        private bool _subscriptionsPending;

        private bool _isConnected;

        /// <summary>
        /// This lock is used to sync 'PlaceOrder' and callback 'OnTransactionDataReceived'
        /// </summary>
        protected readonly object Locker = new object();
        /// <summary>
        /// This container is used to keep pending to be filled market orders, so when the callback comes in we send the filled event
        /// </summary>
        protected readonly ConcurrentDictionary<int, OrderStatus> PendingFilledMarketOrders = new ConcurrentDictionary<int, OrderStatus>();

        /// <summary>
        /// The connection handler for pricing
        /// </summary>
        protected readonly IConnectionHandler PricingConnectionHandler;

        /// <summary>
        /// The connection handler for transactions
        /// </summary>
        protected readonly IConnectionHandler TransactionsConnectionHandler;

        /// <summary>
        /// The list of ticks received
        /// </summary>
        protected readonly List<Tick> Ticks = new List<Tick>();

        /// <summary>
        /// The list of currently subscribed symbols
        /// </summary>
        protected HashSet<Symbol> SubscribedSymbols = new HashSet<Symbol>();

        /// <summary>
        /// A lock object used to synchronize access to subscribed symbols
        /// </summary>
        protected readonly object LockerSubscriptions = new object();

        /// <summary>
        /// The symbol mapper
        /// </summary>
        protected OandaSymbolMapper SymbolMapper;

        /// <summary>
        /// The order provider
        /// </summary>
        protected IOrderProvider OrderProvider;

        /// <summary>
        /// The security provider
        /// </summary>
        protected ISecurityProvider SecurityProvider;

        /// <summary>
        /// The Oanda enviroment
        /// </summary>
        protected Environment Environment;

        /// <summary>
        /// The Oanda access token
        /// </summary>
        protected string AccessToken;

        /// <summary>
        /// The Oanda account ID
        /// </summary>
        protected string AccountId;

        /// <summary>
        /// The Oanda agent string
        /// </summary>
        protected string Agent;

        /// <summary>
        /// The HTTP header key for Oanda agent
        /// </summary>
        protected const string OandaAgentKey = "OANDA-Agent";

        /// <summary>
        /// The default HTTP header value for Oanda agent
        /// </summary>
        public const string OandaAgentDefaultValue = "QuantConnect/0.0.0 (LEAN)";

        /// <summary>
        /// Initializes a new instance of the <see cref="OandaRestApiBase"/> class.
        /// </summary>
        /// <param name="symbolMapper">The symbol mapper.</param>
        /// <param name="orderProvider">The order provider.</param>
        /// <param name="securityProvider">The holdings provider.</param>
        /// <param name="environment">The Oanda environment (Trade or Practice)</param>
        /// <param name="accessToken">The Oanda access token (can be the user's personal access token or the access token obtained with OAuth by QC on behalf of the user)</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="agent">The Oanda agent string</param>
        protected OandaRestApiBase(OandaSymbolMapper symbolMapper, IOrderProvider orderProvider, ISecurityProvider securityProvider, Environment environment, string accessToken, string accountId, string agent)
            : base("Oanda Brokerage")
        {
            SymbolMapper = symbolMapper;
            OrderProvider = orderProvider;
            SecurityProvider = securityProvider;
            Environment = environment;
            AccessToken = accessToken;
            AccountId = accountId;
            Agent = agent;

            PricingConnectionHandler = new DefaultConnectionHandler { MaximumIdleTimeSpan = TimeSpan.FromSeconds(20) };
            PricingConnectionHandler.ConnectionLost += OnPricingConnectionLost;
            PricingConnectionHandler.ConnectionRestored += OnPricingConnectionRestored;
            PricingConnectionHandler.ReconnectRequested += OnPricingReconnectRequested;
            PricingConnectionHandler.Initialize(null);

            TransactionsConnectionHandler = new DefaultConnectionHandler { MaximumIdleTimeSpan = TimeSpan.FromSeconds(20) };
            TransactionsConnectionHandler.ConnectionLost += OnTransactionsConnectionLost;
            TransactionsConnectionHandler.ConnectionRestored += OnTransactionsConnectionRestored;
            TransactionsConnectionHandler.ReconnectRequested += OnTransactionsReconnectRequested;
            TransactionsConnectionHandler.Initialize(null);
        }

        private void OnPricingConnectionLost(object sender, EventArgs e)
        {
            Log.Trace("OnPricingConnectionLost(): pricing connection lost.");

            OnMessage(BrokerageMessageEvent.Disconnected("Pricing connection with Oanda server lost. " +
                                                         "This could be because of internet connectivity issues. "));
        }

        private void OnPricingConnectionRestored(object sender, EventArgs e)
        {
            Log.Trace("OnPricingConnectionRestored(): pricing connection restored");

            OnMessage(BrokerageMessageEvent.Reconnected("Pricing connection with Oanda server restored."));
        }

        private void OnPricingReconnectRequested(object sender, EventArgs e)
        {
            Log.Trace("OnPricingReconnectRequested(): resubscribing symbols.");

            // check if we have a connection
            GetInstrumentList();

            // restore rates session
            List<Symbol> symbolsToSubscribe;
            lock (LockerSubscriptions)
            {
                symbolsToSubscribe = SubscribedSymbols.ToList();
            }
            SubscribeSymbols(symbolsToSubscribe);

            Log.Trace("OnPricingReconnectRequested(): symbols resubscribed.");
        }

        private void OnTransactionsConnectionLost(object sender, EventArgs e)
        {
            Log.Trace("OnTransactionsConnectionLost(): transactions connection lost.");

            OnMessage(BrokerageMessageEvent.Disconnected("Transactions connection with Oanda server lost. " +
                                                         "This could be because of internet connectivity issues. "));
        }

        private void OnTransactionsConnectionRestored(object sender, EventArgs e)
        {
            Log.Trace("OnTransactionsConnectionRestored(): transactions connection restored");

            OnMessage(BrokerageMessageEvent.Reconnected("Transactions connection with Oanda server restored."));
        }

        private void OnTransactionsReconnectRequested(object sender, EventArgs e)
        {
            Log.Trace("OnTransactionsReconnectRequested(): restarting transaction stream.");

            // check if we have a connection
            GetInstrumentList();

            // restore events session
            StopTransactionStream();
            StartTransactionStream();

            Log.Trace("OnTransactionsReconnectRequested(): transaction stream restarted.");
        }

        /// <summary>
        /// Dispose of the brokerage instance
        /// </summary>
        public override void Dispose()
        {
            PricingConnectionHandler.ConnectionLost -= OnPricingConnectionLost;
            PricingConnectionHandler.ConnectionRestored -= OnPricingConnectionRestored;
            PricingConnectionHandler.ReconnectRequested -= OnPricingReconnectRequested;
            PricingConnectionHandler.Dispose();

            TransactionsConnectionHandler.ConnectionLost -= OnTransactionsConnectionLost;
            TransactionsConnectionHandler.ConnectionRestored -= OnTransactionsConnectionRestored;
            TransactionsConnectionHandler.ReconnectRequested -= OnTransactionsReconnectRequested;
            TransactionsConnectionHandler.Dispose();
        }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected => _isConnected &&
            !TransactionsConnectionHandler.IsConnectionLost &&
            !PricingConnectionHandler.IsConnectionLost;

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            // Register to the event session to receive events.
            StartTransactionStream();

            _isConnected = true;

            TransactionsConnectionHandler.EnableMonitoring(true);
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            TransactionsConnectionHandler.EnableMonitoring(false);
            PricingConnectionHandler.EnableMonitoring(false);

            StopTransactionStream();
            StopPricingStream();

            _isConnected = false;
        }

        /// <summary>
        /// Gets the list of available tradable instruments/products from Oanda
        /// </summary>
        public abstract List<string> GetInstrumentList();

        /// <summary>
        /// Retrieves the current rate for each of a list of instruments
        /// </summary>
        /// <param name="instruments">the list of instruments to check</param>
        /// <returns>Dictionary containing the current quotes for each instrument</returns>
        public abstract Dictionary<string, Tick> GetRates(List<string> instruments);

        /// <summary>
        /// Starts streaming transactions for the active account
        /// </summary>
        public abstract void StartTransactionStream();

        /// <summary>
        /// Stops streaming transactions for the active account
        /// </summary>
        public abstract void StopTransactionStream();

        /// <summary>
        /// Starts streaming prices for a list of instruments
        /// </summary>
        public abstract void StartPricingStream(List<string> instruments);

        /// <summary>
        /// Stops streaming prices for all instruments
        /// </summary>
        public abstract void StopPricingStream();

        /// <summary>
        /// Downloads a list of TradeBars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="resolution">The requested resolution</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of bars</returns>
        public abstract IEnumerable<TradeBar> DownloadTradeBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone);

        /// <summary>
        /// Downloads a list of QuoteBars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="resolution">The requested resolution</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of bars</returns>
        public abstract IEnumerable<QuoteBar> DownloadQuoteBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone);

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (Ticks)
            {
                var copy = Ticks.ToArray();
                Ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            lock (LockerSubscriptions)
            {
                var symbolsToSubscribe = (from symbol in symbols
                                          where !SubscribedSymbols.Contains(symbol) && CanSubscribe(symbol)
                                          select symbol).ToList();
                if (symbolsToSubscribe.Count == 0)
                    return;

                Log.Trace("OandaBrokerage.Subscribe(): {0}", string.Join(",", symbolsToSubscribe.Select(x => x.Value)));

                // Oanda does not allow more than a few rate streaming sessions,
                // so we only use a single session for all currently subscribed symbols
                symbolsToSubscribe = symbolsToSubscribe.Union(SubscribedSymbols.ToList()).ToList();

                SubscribedSymbols = symbolsToSubscribe.ToHashSet();

                ProcessSubscriptionRequest();
            }
        }

        /// <summary>
        /// Removes the specified symbols from the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            lock (LockerSubscriptions)
            {
                var symbolsToUnsubscribe = (from symbol in symbols
                                            where SubscribedSymbols.Contains(symbol)
                                            select symbol).ToList();
                if (symbolsToUnsubscribe.Count == 0)
                    return;

                Log.Trace("OandaBrokerage.Unsubscribe(): {0}", string.Join(",", symbolsToUnsubscribe.Select(x => x.Value)));

                // Oanda does not allow more than a few rate streaming sessions,
                // so we only use a single session for all currently subscribed symbols
                var symbolsToSubscribe = SubscribedSymbols.ToList().Where(x => !symbolsToUnsubscribe.Contains(x)).ToList();

                SubscribedSymbols = symbolsToSubscribe.ToHashSet();

                ProcessSubscriptionRequest();
            }
        }

        /// <summary>
        /// Groups multiple subscribe/unsubscribe calls to avoid closing and reopening the streaming session on each call
        /// </summary>
        private void ProcessSubscriptionRequest()
        {
            if (_subscriptionsPending) return;

            _lastSubscribeRequestUtcTime = DateTime.UtcNow;
            _subscriptionsPending = true;

            Task.Run(() =>
            {
                while (true)
                {
                    DateTime requestTime;
                    List<Symbol> symbolsToSubscribe;
                    lock (LockerSubscriptions)
                    {
                        requestTime = _lastSubscribeRequestUtcTime.Add(SubscribeDelay);
                        symbolsToSubscribe = SubscribedSymbols.ToList();
                    }

                    if (DateTime.UtcNow > requestTime)
                    {
                        // restart streaming session
                        SubscribeSymbols(symbolsToSubscribe);

                        lock (LockerSubscriptions)
                        {
                            _lastSubscribeRequestUtcTime = DateTime.UtcNow;
                            if (SubscribedSymbols.Count == symbolsToSubscribe.Count)
                            {
                                // no more subscriptions pending, task finished
                                _subscriptionsPending = false;
                                break;
                            }
                        }
                    }

                    Thread.Sleep(200);
                }
            });
        }

        /// <summary>
        /// Returns true if this brokerage supports the specified symbol
        /// </summary>
        private static bool CanSubscribe(Symbol symbol)
        {
            // ignore unsupported security types
            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Cfd)
                return false;

            // ignore universe symbols
            return !symbol.Value.Contains("-UNIVERSE-");
        }

        /// <summary>
        /// Subscribes to the requested symbols (using a single streaming session)
        /// </summary>
        /// <param name="symbolsToSubscribe">The list of symbols to subscribe</param>
        protected void SubscribeSymbols(List<Symbol> symbolsToSubscribe)
        {
            var instruments = symbolsToSubscribe
                .Select(symbol => SymbolMapper.GetBrokerageSymbol(symbol))
                .ToList();

            PricingConnectionHandler.EnableMonitoring(false);

            StopPricingStream();

            if (instruments.Count > 0)
            {
                StartPricingStream(instruments);

                PricingConnectionHandler.EnableMonitoring(true);
            }
        }
    }
}
