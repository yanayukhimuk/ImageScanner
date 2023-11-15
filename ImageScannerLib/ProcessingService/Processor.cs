using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;

namespace ImageScannerLib.ProcessingService
{
    public class Processor : IProcessor
    {
        private ConnectionFactory Factory { get; set; }
        private IConnection Connection { get; set; }
        private IModel Channel { get; set; }

        public Processor(string hostname)
        {
            this.Factory = new ConnectionFactory() { HostName = hostname };
            this.Connection = Factory.CreateConnection();
            this.Channel = Connection.CreateModel();
        }
        public List<string> GetIncomingImagesFromDataCapture(string routingKey, string exchange)
        {
            this.Channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct, durable: true, autoDelete: false);

            var queueName = this.Channel.QueueDeclare().QueueName;
            this.Channel.QueueBind(queue: queueName, exchange: exchange, routingKey: routingKey);

            var consumer = new EventingBasicConsumer(this.Channel);

            List<string> files = new();

            consumer.Received += (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var msg = System.Text.Encoding.UTF8.GetString(body);
                files.Add(msg);
            };

            this.Channel.BasicConsume(queue: queueName,
                autoAck: true,
                consumer: consumer);

            Console.WriteLine("Images have been received");

            return files;
        }

        public void StoreIncomingImagesInFolder(List<object> files, string folderName)
        {
            throw new NotImplementedException();
        }
    }
}
