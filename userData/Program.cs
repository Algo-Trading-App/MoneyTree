using System;
using System.Text;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UDMRequestClass;

namespace Messaging
{
    public class Program 
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting demo server...");
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: true, arguments: null);
                channel.BasicQos(0, 1, false);
                var consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: consumer);
                Console.WriteLine(" [x] Awaiting RPC requests");


                consumer.Received += (model, ea) =>
                {
                    string response = null;

                    var body = ea.Body;
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        var message = Encoding.UTF8.GetString(body.ToArray());
                        Console.WriteLine(string.Format(" [.] Received message: {0}", message));
                        UDMRequest obj = JsonConvert.DeserializeObject<UDMRequest>(message, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, PreserveReferencesHandling = PreserveReferencesHandling.All });
                        response = RequestHandler.MainHandler(obj);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(" [.] " + ex.Message);
                        response = "";
                    }
                    finally
                    {
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        Console.WriteLine(" [.] Response Sent");
                    }
                };

                Console.WriteLine(" Type 'exit' or 'quit' to exit.");
                string input;
                while (true)
                {
                    input = Console.ReadLine();
                    if (input.ToLower() == "quit" || input.ToLower() == "exit")
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine(" Type 'exit' or 'quit' to exit.");
                    }
                }
            }
        }
    }
}

