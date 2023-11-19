using ImageScannerLib.Configuration;
using Microsoft.Extensions.Hosting;
using NLog;
using RabbitMQ.Client;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ImageScannerLib.DataCaptureService
{
    public class DataCaptureService : BackgroundService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _fileExtension;

        private FileSystemWatcher _watcher;
        private readonly string _hostName = ConfigManager.GetConfigDetails().Hostname;
        private readonly string _queue = ConfigManager.GetConfigDetails().Queue;
        private readonly string _watchFolderPath = GetDirectoryPath(ConfigManager.GetConfigDetails().WatchFolderPath);
        private readonly int _messageSizeBytes = 65_536;

        public DataCaptureService(string fileExtention)
        {
            _fileExtension = fileExtention;

            var factory = new ConnectionFactory() { HostName = _hostName };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(_queue, true, false, false, null);

            logger.Info("Data Capture Service is initialized.");
            logger.Info($"Connection: {_connection}.");
            logger.Info($"Channel: {_channel}.");
            logger.Info($"File Extension:  {_fileExtension} .");
            logger.Info($"Hostname:  {_hostName} .");
            logger.Info($"Queue:  {_queue} .");
            logger.Info($"Folder with files:  {_watchFolderPath} .");
        }   
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _watcher = new FileSystemWatcher(_watchFolderPath)
            {
                EnableRaisingEvents = true,
                Filter = $"*.{_fileExtension}"
            };

            _watcher.Created += (sender, args) =>
            {
                logger.Info("New file has been created.");

                var filePath = args.FullPath;
                if (File.Exists(filePath) && Path.GetExtension(filePath) == $".{_fileExtension}")
                {
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    StreamReader streamReader = new StreamReader(fileStream);

                    int remainingFileSize = Convert.ToInt32(fileStream.Length);
                    int totalFileSize = Convert.ToInt32(fileStream.Length);
                    bool finished = false;

                    string randomFileName = string.Concat("large_file", Guid.NewGuid(), $".{_fileExtension}");
                    byte[] buffer;

                    while (fileStream.CanRead)
                    {
                        if (remainingFileSize <= 0) break;
                        int read = 0;
                        if (remainingFileSize > _messageSizeBytes)
                        {
                            buffer = new byte[_messageSizeBytes];
                            read = fileStream.Read(buffer, 0, _messageSizeBytes);
                        }
                        else
                        {
                            buffer = new byte[remainingFileSize];
                            read = fileStream.Read(buffer, 0, remainingFileSize);
                            finished = true;
                        }

                        IBasicProperties basicProperties = _channel.CreateBasicProperties();
                        basicProperties.Persistent = true;
                        basicProperties.Headers = new Dictionary<string, object>();
                        basicProperties.Headers.Add("output-file", randomFileName);
                        basicProperties.Headers.Add("finished", finished);

                        _channel.BasicPublish("", _queue, basicProperties, buffer);
                        remainingFileSize -= read;
                    }

                    logger.Info($"File fas been moved through channel:  {_channel}, and queue: {_queue}.");
                }
            };

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _watcher?.Dispose();
            _channel.Close();
            _connection.Close();

            logger.Info("Disposing resources.");

            base.Dispose();
        }

        private static string GetDirectoryPath(string folderName)
        {
            string workingDirectory = Environment.CurrentDirectory;
            var directory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            var path = Path.Combine(directory, folderName);
            return path;
        }
    }
}
