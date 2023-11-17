﻿using ImageScannerLib.Configuration;
using Microsoft.Extensions.Hosting;
using NLog;
using RabbitMQ.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    var body = Encoding.UTF8.GetBytes(filePath);
                    _channel.BasicPublish("", _queue, null, body);

                    logger.Info($"File is being movied through channel:  {_channel}, and queue: {_queue}.");
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
