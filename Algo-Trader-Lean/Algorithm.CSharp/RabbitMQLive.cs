using System;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating how to setup a custom brokerage message handler. Using the custom messaging
    /// handler you can ensure your algorithm continues operation through connection failures.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="brokerage models" />
    public class RabbitMQLive : QCAlgorithm
    {
        private bool _submittedMarketOnCloseToday;
        private Security _security;
        private DateTime last = DateTime.MinValue;
        private JObject jsonmessage;
        private List<string> equityList = new List<string> { };
        private ConnectionFactory factory = new ConnectionFactory();
        private IConnection connection;
        private IModel channel;
        private EventingBasicConsumer consumer;


        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(25000);

            AddSecurity(SecurityType.Equity, "SPY", Resolution.Second, fillDataForward: true, extendedMarketHours: true);

            _security = Securities["SPY"];

            //Set the brokerage message handler:
            SetBrokerageMessageHandler(new BrokerageMessageHandler(this));

            // Create new connection factory
            //var factory = new ConnectionFactory()
            //{
            //    HostName = "localhost"
            //};

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            factory.HostName = "localhost";


            // Set up queue for RabbitMQ
            channel.QueueDeclare(queue: "backtest",
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            // Create object for rabbitMQ producer
            consumer = new EventingBasicConsumer(channel);

            // Set up consumer message handler
            consumer.Received += (model, ea) =>
            {
				Debug("THIS COMSUMER RECIEVE IS BEING CALLED");
				var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                jsonmessage = JObject.Parse(message);



                foreach (string element in jsonmessage["equities"].ToObject<List<string>>())
                {
                    Debug(element);
                    AddEquity(element, Resolution.Daily);
                    equityList.Add(element);
                    Debug(element);
                }
            };

            channel.BasicConsume(queue: "backtest",
                                    autoAck: true,
                                    consumer: consumer);
        }

        public void OnData(TradeBars data)
        {
            channel.BasicConsume(queue: "backtest",
                                        autoAck: true,
                                        consumer: consumer);

			foreach (string element in equityList)
			{
                if (!Portfolio[element].Invested)
				{
					Order(element, 100);
					Debug("Purchased " + element + " on " + Time.ToShortDateString());
				}
			}

			if (Time.Date != last.Date) // each morning submit a market on open order
            {
                _submittedMarketOnCloseToday = false;
                MarketOnOpenOrder("SPY", 100);
                last = Time;

                if (Portfolio.HoldStock) return;
                Order("SPY", 100);
                Debug("Purchased SPY on " + Time.ToShortDateString());

                Sell("SPY", 50);
                Debug("Sell SPY on " + Time.ToShortDateString());
            }

            if (!_submittedMarketOnCloseToday && _security.Exchange.ExchangeOpen) // once the exchange opens submit a market on close order
            {
                _submittedMarketOnCloseToday = true;
                MarketOnCloseOrder("SPY", -100);
            }
        }

        public override void OnOrderEvent(OrderEvent fill)
        {
            var order = Transactions.GetOrderById(fill.OrderId);
            Console.WriteLine(Time + " - " + order.Type + " - " + fill.Status + ":: " + fill);
        }
    }

    /// <summary>
    /// Handle the error messages in a custom manner
    /// </summary>
    public class BrokerageMessageHandler : IBrokerageMessageHandler
    {
        private readonly IAlgorithm _algo;
        public BrokerageMessageHandler(IAlgorithm algo) { _algo = algo; }

        /// <summary>
        /// Process the brokerage message event. Trigger any actions in the algorithm or notifications system required.
        /// </summary>
        /// <param name="message">Message object</param>
        public void Handle(BrokerageMessageEvent message)
        {
            var toLog = $"{_algo.Time.ToStringInvariant("o")} Event: {message.Message}";
            _algo.Debug(toLog);
            _algo.Log(toLog);
        }
    }
}