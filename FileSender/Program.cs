using ImageScannerLib.Constants;
using ImageScannerLib.DataCaptureService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileSender
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<DataCaptureService>(provider =>
                {
                    return new DataCaptureService(ImageScannerConstants.PDF);
                });
            });
    }
}