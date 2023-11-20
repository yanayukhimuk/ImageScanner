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
using System.Diagnostics.Tracing;

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
        private readonly string _path = "C:\\Users\\Yana_Yukhimuk\\source\\repos\\ImageScanner\\ImageScanner\\bin\\Debug\\net7.0\\";
        private readonly string _TempFolder = "TempFiles";
        public ProcessingService()
        {
            var factory = new ConnectionFactory() { HostName = _hostName };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(_queue, true, false, false, null);
            Directory.CreateDirectory(_storePath);
            Directory.CreateDirectory(_TempFolder);

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
            EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(_queue, false, consumer);
            bool isLastChunk = false;

            consumer.Received += OnReceived;

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            logger.Info("Disposing resources.");

            _channel.Close();
            _connection.Close();
            base.Dispose();
        }

        public void OnReceived(object model, BasicDeliverEventArgs args)
        {
            Console.WriteLine("Received a chunk!");
            var headers = args.BasicProperties.Headers;
            var fileName = Encoding.UTF8.GetString(headers["output-file"] as byte[]); // + folder + server
            var isLastChunk = Convert.ToBoolean(headers["finished"]);
            var filesize = Convert.ToInt32(headers["file-size"]);
            var seqId = Encoding.UTF8.GetString(headers["seqId"] as byte[]);

            fileName = string.Concat(_path, _storePath, "\\", fileName);
            string tempFileName = string.Concat(_path, _TempFolder, "\\", seqId); 

            using (FileStream fileStream = new FileStream(tempFileName, FileMode.Append, FileAccess.Write))
            {
                fileStream.Write(args.Body.ToArray(), 0, args.Body.Length);
                fileStream.Flush();
            }

            FileInfo fi = new FileInfo(tempFileName);

            if (fi.Length == filesize)
            {
                File.Move(tempFileName, fileName);
            }
            Console.WriteLine("Chunk saved. Finished? {0}", isLastChunk);
            _channel.BasicAck(args.DeliveryTag, false);
        }
    }
}
