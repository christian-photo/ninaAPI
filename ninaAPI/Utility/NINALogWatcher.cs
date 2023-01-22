using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI
{
    public class NINALogWatcher
    {
        private Thread watcherThread;
        private string LogFile;
        private bool stopped;
        private NINALogMessageProcessor processor;

        public NINALogWatcher(NINALogMessageProcessor processor)
        {
            this.processor = processor;
        }
        
        public void Start()
        {
            LogFile = GetActiveLogFile();
            if (LogFile != null)
            {
                watcherThread = new Thread(Watch);
                watcherThread.Start();
            }
        }

        public void Stop()
        {
            if (watcherThread != null)
            {
                stopped = true;
                watcherThread.Abort();
                watcherThread = null;
            }
        }

        private void Watch()
        {
            FileSystemWatcher watcher = null;
            try
            {
                AutoResetEvent autoReset = new AutoResetEvent(false);
                watcher = new FileSystemWatcher(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA", "Logs"));
                watcher.Filter = LogFile;
                watcher.EnableRaisingEvents = true;
                watcher.Changed += (sender, e) =>
                {
                    autoReset.Set();
                };

                FileStream fs = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (StreamReader sr = new StreamReader(fs)) 
                {
                    List<string> lines = new List<string>();
                    while (!stopped)
                    {
                        string line = sr.ReadLine();

                        if (line != null)
                        {
                            lines.Add(line);
                        }
                        else
                        {
                            if (lines.Count > 0)
                            {
                                processor.processLogMessages(lines);
                                lines.Clear();
                            }

                            autoReset.WaitOne(2000);
                        }
                    }

                    autoReset.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private string GetActiveLogFile()
        {
            try
            {
                string logDirectory = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs");
                string version = CoreUtil.Version;
                int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                Regex re = new Regex(@"^\d{8}-\d{6}-" + $"{version}.{processId}.log$", RegexOptions.Compiled);

                List<string> fileList = new List<string>(Directory.GetFiles(logDirectory));
                foreach (string file in fileList)
                {
                    if (re.IsMatch(Path.GetFileName(file)))
                    {
                        return file;
                    }
                }

                Logger.Warning($"failed to find active NINA log file in {logDirectory}, cannot process log events for ninaAPI");
                return null;
            }
            catch (Exception e)
            {
                Logger.Warning($"failed to find active NINA log file, cannot process log events for ninaAPI: {e.Message} {e.StackTrace}");
                return null;
            }
        }
    }
}
