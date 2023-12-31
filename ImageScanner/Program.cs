﻿using ImageScannerLib.Configuration;
using ImageScannerLib.DataCaptureService;
using ImageScannerLib.ProcessingService;
using ImageScannerLib.Constants;
using System.Reflection;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageScanner
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
                services.AddHostedService<ProcessingService>(provider =>
                {
                    return new ProcessingService();
                });
            });
    }
}