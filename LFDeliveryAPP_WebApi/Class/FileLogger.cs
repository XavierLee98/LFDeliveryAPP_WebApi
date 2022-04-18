using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Class
{
    public class FileLogger : IDisposable
    {
        /// <summary>
        /// Last error message genrated
        /// </summary>
        public string LastErrorMesaage { get; private set; } = string.Empty;
        public string filePath { get; set; }
        /// <summary>
        ///  Dispose code
        /// </summary>
        public void Dispose() => GC.Collect();

        /// <summary>
        /// The construtor, entry point
        /// </summary>
        public FileLogger()
        {
            // to do code
        }

        /// <summary>
        /// write message into a current date time log, for debuging
        /// </summary>
        /// <param name="logMessage">The exception message from any module</param>
        public bool WriteLog(string logMessage)
        {
            try
            {
                string destinationPath=null;
                if (filePath == null)
                {
                    destinationPath = Path.Combine(Path.GetTempFileName(), @$"\DeliveryAppLog");
                }
                else
                {
                    destinationPath = Path.Combine(filePath, @$"\DeliveryAppLog");
                }

                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                string targetFileName = Path.Combine(destinationPath, $"{DateTime.Now:yyyy-MM-dd)}.txt");
                File.AppendAllText(targetFileName, $"{DateTime.Now:HH:mm:ss)} => \n{logMessage}\n");

                return true;
            }
            catch (Exception excep)
            {
                LastErrorMesaage = $"{excep}";
                return false;
            }
        }

        // sample reference code
        // https://stackoverflow.com/questions/35310078/how-to-write-to-a-file-in-net-core
        //        var watcher = new BluetoothLEAdvertisementWatcher();
        //        var logPath = System.IO.Path.GetTempFileName();
        //        var logFile = System.IO.File.Create(logPath);
        //        var logWriter = new System.IO.StreamWriter(logFile);
        //        logWriter.WriteLine("Log message");
        //logWriter.Dispose();

    }
}
