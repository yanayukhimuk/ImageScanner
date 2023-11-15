using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Runtime.InteropServices.Marshalling;

namespace ImageScannerLib.Configuration
{
    public class ConfigManager
    {
        private static ConfigDetails configDetails;

        public static ConfigDetails GetConfigDetails()
        {
            if (configDetails == null)
            {
                configDetails = LoadConfigData();
            }
            return configDetails;
        }

        private static ConfigDetails? LoadConfigData()
        {
            configDetails = new ConfigDetails();

            configDetails.RabbitMqUri = ConfigurationManager.AppSettings["rabbitUrl"];
            configDetails.Username = ConfigurationManager.AppSettings["username"];
            configDetails.Password = ConfigurationManager.AppSettings["password"];
            configDetails.Hostname = ConfigurationManager.AppSettings["hostname"];
            configDetails.Queue = ConfigurationManager.AppSettings["queue"];
            configDetails.Exchange = ConfigurationManager.AppSettings["exchange"];
            configDetails.Directory = ConfigurationManager.AppSettings["directoryPath"];

            return configDetails;
        }
    }
}
