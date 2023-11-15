using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageScannerLib.Configuration
{
    public class ConfigDetails
    {
        public string RabbitMqUri { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Hostname { get; set; }
        public string Queue { get; set; }
        public string Exchange { get; set; }
        public string Directory { get; set; }
    }
}
