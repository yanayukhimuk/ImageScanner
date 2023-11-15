using ImageScannerLib.Configuration;
using ImageScannerLib.DataCaptureService;
using ImageScannerLib.ProcessingService;
using ImageScannerLib.Constants;
using System.Reflection;

namespace ImageScanner
{
    internal class Program
    {
        static readonly string HostName = ConfigManager.GetConfigDetails().Hostname;
        static readonly string Exchange = ConfigManager.GetConfigDetails().Exchange;

        static void Main(string[] args)
        {
            string path = GetDirectoryPath(ConfigManager.GetConfigDetails().Directory);
            
            //FileSystemWatcher watcher = null; - catch is folder content changed -> subscribe to folder 
            //Read files partially (100 MB)
            //Sequence id

            StartDataCaptureService(path, ImageScannerConstants.PDF);

            //Task.Run(() => StartDataCaptureService(path, ImageScannerConstants.PDF));
            //Task.Run(() => StartDataCaptureService(path, ImageScannerConstants.JPG));
            //Task.Run(() => StartDataCaptureService(path, ImageScannerConstants.DOCX));

            Processor processor = new(HostName);
            var x = processor.GetIncomingImagesFromDataCapture(routingKey: ImageScannerConstants.PDF, exchange: Exchange);
        }

        private static void StartDataCaptureService(string path, string format)
        {
            DataCapture dataCapture = new(HostName);
            var files = dataCapture.GetFilesFromLocalFolderOfSpecificFormat(format, path);
            dataCapture.SendFilesToMainProcessingService(files: files, routingKey: format, exchange: Exchange);
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