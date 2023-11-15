using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageScannerLib.ProcessingService
{
    public interface IProcessor
    {
        public List<string> GetIncomingImagesFromDataCapture(string routingKey, string exchange);
        public void StoreIncomingImagesInFolder(List<object> files, string folderName);
    }
}
