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
    public class DataCapture : IDataCapture
    {
        private static List<string> UpdatedFiles = new List<string>();
        private ConnectionFactory Factory { get; set; }
        private IConnection Connection { get; set; }
        private IModel Channel { get; set; }
        public DataCapture(string hostname)
        {
            this.Factory = new ConnectionFactory() { HostName = hostname };
            this.Connection = Factory.CreateConnection();
            this.Channel = Connection.CreateModel();
        }
        public List<string> GetFilesFromLocalFolderOfSpecificFormat(string format, string searchFolder, bool isRecursive = false)
        {
            List<string> filesFound = new();

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = searchFolder;
            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Filter = "*." + format;
            watcher.Created += new FileSystemEventHandler(OnCreated);
            watcher.EnableRaisingEvents = true;

            //var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            //filesFound.AddRange(Directory.GetFiles(searchFolder, String.Format("*.{0}", format), searchOption));

            return filesFound;
        }

        public void SendFilesToMainProcessingService(List<string> files, string routingKey, string exchange)
        {
            this.Channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct, durable: true, autoDelete: false);

            var body = files.SelectMany(i => Encoding.UTF8.GetBytes(i + Environment.NewLine)).ToArray();

            this.Channel.BasicPublish(exchange: exchange,
                routingKey: routingKey,
                basicProperties: null,
                body: body);

            Console.WriteLine($"Fies with format : {routingKey} have been sent into Direct Exchange");
        }

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            UpdatedFiles.Add(e.Name);
            Console.WriteLine("New file has been added" + e.Name);
        }
    }
}
