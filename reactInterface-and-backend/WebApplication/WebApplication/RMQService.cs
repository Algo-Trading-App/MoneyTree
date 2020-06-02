using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

using RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebApplication
{
    public class RabbitModelPooledObjectPolicy : IPooledObjectPolicy<IModel>
    {
        readonly IConnection m_connection;

        public RabbitModelPooledObjectPolicy()
        {
            m_connection = getConnection();
        }

        private IConnection getConnection()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            return factory.CreateConnection();
        }

        public IModel Create()
        {
            return m_connection.CreateModel();
        }

        public bool Return(IModel obj)
        {
            if (obj.IsOpen)
                return true;

            obj?.Dispose();
            return false;
        }
    }

    public interface IRMQService
    {
        string ExecuteRequest<T>(T message, string exchangeName, string exchangeType, string routeKey) where T : class;
    }

    public class RMQService : IRMQService
    {
        readonly DefaultObjectPool<IModel> m_objectPool;
        readonly BlockingCollection<string> m_respQueue = new BlockingCollection<string>();

        public delegate void ReceiveRMQMessageDelegate(object sender, object result);

        public event ReceiveRMQMessageDelegate OnReceiveMessage;

        public RMQService(IPooledObjectPolicy<IModel> objectPolicy)
        {
            m_objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount * 2);
        }

        public string ExecuteRequest<T>(T request, string exchangeName = "", string exchangeType = "", string routeKey = "rpc_queue") where T : class
        {
            var channel = m_objectPool.Get();
            var replyQueueName = channel.QueueDeclare().QueueName;
            var consumer = new EventingBasicConsumer(channel);

            var props = channel.CreateBasicProperties();

            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    m_respQueue.Add(response);
                }
            };

            string message = JsonConvert.SerializeObject(request, new JsonSerializerSettings() { Formatting = Formatting.Indented, PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameHandling = TypeNameHandling.All });
            var messageBytes = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(
                exchange: exchangeName,
                routingKey: routeKey,
                basicProperties: props,
                body: messageBytes);

            channel.BasicConsume(
                consumer: consumer,
                queue: replyQueueName,
                autoAck: true);

            string output = m_respQueue.Take();

            return output;
        }
    }
}
