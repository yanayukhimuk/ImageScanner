using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageScannerLib.DataCaptureService
{
    public interface IDataCapture
    {
        List<string> GetFilesFromLocalFolderOfSpecificFormat(string format, string folderName, bool isRecursive);
        void SendFilesToMainProcessingService(List<string> files, string routingKey, string exchange);
    }
}
