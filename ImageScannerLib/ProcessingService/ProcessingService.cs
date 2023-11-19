using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using ImageScannerLib.Configuration;
using NLog;
using System.Reflection;

namespace ImageScannerLib.ProcessingService
{
    public class ProcessingService : BackgroundService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IConnection _connection;
        private readonly IModel _channel;

        private readonly string _queue = ConfigManager.GetConfigDetails().Queue;
        private readonly string _storePath = ConfigManager.GetConfigDetails().DestinationFolderPath;
        private readonly string _hostName = ConfigManager.GetConfigDetails().Hostname;
        public ProcessingService()
        {
            var factory = new ConnectionFactory() { HostName = _hostName };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(_queue, true, false, false, null);
            Directory.CreateDirectory(_storePath);

            logger.Info("Processing Service is initialized.");
            logger.Info($"Connection: {_connection}.");
            logger.Info($"Channel: {_channel}.");
            logger.Info($"Hostname:  {_hostName} .");
            logger.Info($"Queue:  {_queue} .");
            logger.Info($"Destination folder with files:  {_storePath} .");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _channel.BasicQos(0, 1, false);
            var consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(_queue, false, consumer);

            while (true)
            {
                consumer.Received += (sender, eventArgs) =>
                {
                    logger.Info("Received a chunk!");
                    var headers = eventArgs.BasicProperties.Headers;

                    string randomFileName = Encoding.UTF8.GetString((headers["output-file"] as byte[]));
                    bool isLastChunk = Convert.ToBoolean(headers["finished"]);
                    string localFileName = string.Concat(_storePath, randomFileName);

                    using (FileStream fileStream = new FileStream(localFileName, FileMode.Append, FileAccess.Write))
                    {
                        fileStream.Write(eventArgs.Body.ToArray(), 0, eventArgs.Body.Length);
                        fileStream.Flush();
                    }

                    logger.Info("Chunk saved. Finished? {0}", isLastChunk);
                    _channel.BasicAck(eventArgs.DeliveryTag, false);
                };
            }

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            logger.Info("Disposing resources.");

            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}
