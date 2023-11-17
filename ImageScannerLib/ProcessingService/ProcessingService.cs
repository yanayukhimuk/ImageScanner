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

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, eventArgs) =>
            {
                var documentName = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                logger.Info($"Received document is : {documentName}.");

                File.Move(documentName, Path.Combine(_storePath, Path.GetFileName(documentName)));

                logger.Info($"Document {documentName} moved to folder {_storePath}.");

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };

            _channel.BasicConsume(_queue, false, consumer);

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
