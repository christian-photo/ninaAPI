using NINA.Core.Utility;
using ninaAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace ninaAPI
{

    public class NINALogWatcher
    {
        private NINALogMessageProcessor processor;
        private string logDirectory;
        private string activeLogFile;
        private Thread watcherThread = null;
        private bool stopped = false;
        private AutoResetEvent autoReset = null;

        public NINALogWatcher(NINALogMessageProcessor processor)
        {
            this.processor = processor;
            this.logDirectory = LogUtils.GetLogDirectory();
            this.activeLogFile = getActiveLogFile(logDirectory);
        }

        public void Start()
        {
            Stop();

            if (activeLogFile != null)
            {
                Logger.Info($"web viewer: watching log file: {activeLogFile}");
                stopped = false;
                Watch(logDirectory, activeLogFile);
            }
        }

        public void Stop()
        {
            if (watcherThread != null)
            {
                Logger.Debug("web viewer: stopping log watcher");
                stopped = true;
                watcherThread = null;
            }
        }

        private void Watch(string logDirectory, string activeLogFile)
        {
            watcherThread = new Thread(() => {
                FileSystemWatcher fsWatcher = null;
                try
                {
                    autoReset = new AutoResetEvent(false);
                    fsWatcher = new FileSystemWatcher(logDirectory);
                    fsWatcher.Filter = activeLogFile;
                    fsWatcher.EnableRaisingEvents = true;
                    fsWatcher.Changed += (s, e) => autoReset.Set();

                    FileStream fs = new FileStream(activeLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
                catch (Exception e)
                {
                    if (e is ThreadAbortException)
                    {
                        if (fsWatcher != null)
                        {
                            fsWatcher.EnableRaisingEvents = false;
                            fsWatcher.Dispose();
                        }

                        Logger.Debug("web view log watcher has been stopped/aborted");
                    }
                    else
                    {
                        Logger.Warning($"failed to process log messages for web viewer: {e.Message} {e.StackTrace}");
                    }
                }
            });

            watcherThread.Name = "WSHV log watcher thread";
            watcherThread.Start();
        }

        private string getActiveLogFile(string logDirectory)
        {

            try
            {
                Regex re = new Regex(LogUtils.GetLogFileRE(), RegexOptions.Compiled);
                List<string> fileList = new List<string>(Directory.GetFiles(logDirectory));

                foreach (string file in fileList)
                {
                    if (re.IsMatch(Path.GetFileName(file)))
                    {
                        return file;
                    }
                }

                Logger.Warning($"failed to find active NINA log file in {logDirectory}, cannot process log events for web viewer");
                return null;
            }
            catch (Exception e)
            {
                Logger.Warning($"failed to find active NINA log file in {logDirectory}, cannot process log events for web viewer: {e.Message} {e.StackTrace}");
                return null;
            }
        }
    }

    public class LogUtils
    {

        public static string GetLogDirectory()
        {
            return Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs");
        }

        public static string GetLogFileRE()
        {
            // NINA log files match yyyyMMdd-HHmmss-VERSION.PID-yyyyMM.log - see NINA.Core.Utility.Logger.
            // Note that daily log file rolling was added in Sept 2023 - that added the '-yyyyMMdd' to the end.
            // It was then switched to monthly rolling in Oct 2023 so now '-yyyyMM' but RE will match either.

            string version = CoreUtil.Version;
            int processId = Environment.ProcessId;
            return @"^\d{8}-\d{6}-" + $"{version}.{processId}" + @"-\d{6,8}.log$";
        }

        private LogUtils()
        {
        }
    }
}