using ImageScannerLib.Configuration;
using ImageScannerLib.ProcessingService;

namespace ImageGetter
{
    internal class Program
    {
        static readonly string HostName = ConfigManager.GetConfigDetails().Hostname;
        static readonly string Queue = ConfigManager.GetConfigDetails().Queue;
        static readonly string Exchange = ConfigManager.GetConfigDetails().Exchange;
        static void Main(string[] args)
        {

            Processor processingService = new(HostName);
            var receivedFiles = processingService.GetIncomingImagesFromDataCapture(Queue, Exchange);
        }
    }
}