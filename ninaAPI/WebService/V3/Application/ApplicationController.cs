#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.Properties;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Service;

namespace ninaAPI.WebService.V3.Application
{
    public class ApplicationController : WebApiController
    {
        private readonly IProfileService profileService;
        private readonly IApplicationMediator applicationMediator;
        private readonly ResponseHandler responseHandler;

        public ApplicationController(IProfileService profileService, IApplicationMediator applicationMediator, ResponseHandler responseHandler)
        {
            this.profileService = profileService;
            this.applicationMediator = applicationMediator;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/log")]
        public async Task GetLogEntries()
        {
            QueryParameter<int> lineCount = new QueryParameter<int>("lineCount", 100, false);
            QueryParameter<LogLevelEnum> logLevel = new QueryParameter<LogLevelEnum>("level", LogLevelEnum.INFO, false);
            lineCount.Get(HttpContext);
            logLevel.Get(HttpContext);

            string currentLogFile = Directory.GetFiles(Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs")).OrderByDescending(File.GetCreationTime).First();

            string[] logLines = [];

            using (var stream = new FileStream(currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string content = reader.ReadToEnd();
                logLines = content.Split('\n');
            }

            List<string> filteredLogLines = logLines.Where(line => IsLineAboveLevel(line, logLevel.Value)).ToList();
            filteredLogLines = filteredLogLines.TakeLast(lineCount.Value).ToList();

            List<LogLine> parsed = filteredLogLines.Select(LogLine.Parse).ToList();
            parsed.Reverse();

            await responseHandler.SendObject(HttpContext, parsed);
        }

        [Route(HttpVerbs.Get, "/log/settings")]
        public async Task GetLogSettings()
        {
            await responseHandler.SendObject(HttpContext,
                new LoggerSettings() { Level = profileService.ActiveProfile.ApplicationSettings.LogLevel }
            );
        }

        [Route(HttpVerbs.Patch, "/log/settings")]
        public async Task UpdateLogLevel([JsonData] LoggerSettings settings)
        {
            if (!settings.Level.HasValue)
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid log level");
            }

            Logger.SetLogLevel(settings.Level.Value);
            await responseHandler.SendObject(HttpContext, new StringResponse("Log level updated"));
        }

        [Route(HttpVerbs.Get, "/tab")]
        public async Task GetApplicationTab()
        {
            IApplicationVM vm = (IApplicationVM)applicationMediator.GetType().GetField("handler", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(applicationMediator);
            int index = vm.TabIndex;

            await responseHandler.SendObject(HttpContext, new { CurrentTab = (ApplicationTab)index });
        }

        [Route(HttpVerbs.Put, "/tab")]
        public async Task SetApplicationTab([JsonData] ApplicationTabChangeRequest request)
        {
            if (!request.Tab.HasValue)
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid tab");
            }
            applicationMediator.ChangeTab(request.Tab.Value);
            await responseHandler.SendObject(HttpContext, new StringResponse("Tab changed"));
        }

        [Route(HttpVerbs.Get, "/screenshot")]
        public async Task GetScreenshot()
        {
            // Here only scale, size, format and quality are used and these are the only ones that will be documented
            ImageQueryParameterSet parameters = ImageQueryParameterSet.ByProfile(profileService.ActiveProfile);

            parameters.Evaluate(HttpContext);

            Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(
                    Screen.PrimaryScreen.Bounds.X,
                    Screen.PrimaryScreen.Bounds.Y,
                    0, 0,
                    screenshot.Size,
                    CopyPixelOperation.SourceCopy
                );
            }

            BitmapSource source = ImageUtility.ConvertBitmap(screenshot);

            source = ImageService.ResizeBitmap(source, parameters);
            ImageWriter writer = ImageWriter.GetImageWriter(source, parameters.Format.Value);

            await responseHandler.SendBytes(HttpContext, writer.Encode(parameters.Quality.Value), writer.MimeType);

            screenshot.Dispose();
        }

        [Route(HttpVerbs.Get, "/plugins")]
        public async Task GetPlugins()
        {
            string path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
            List<string> plugins = [.. Directory.GetDirectories(path).Select(Path.GetFileName)];

            await responseHandler.SendObject(HttpContext, plugins);
        }

        [Route(HttpVerbs.Get, "/plugin/settings")]
        public async Task GetPluginSettings()
        {
            await responseHandler.SendObject(HttpContext, new
            {
                AccessControlHeaderEnabled = Settings.Default.UseAccessControlHeader,
                ShouldCreateThumbnails = Settings.Default.CreateThumbnails,
            });
        }

        private static bool IsLineAboveLevel(string line, LogLevelEnum level)
        {
            // ERROR is the highest level and is represented by 0
            for (int i = (int)level; i >= 0; i--)
            {
                if (line.Contains($"|{(LogLevelEnum)i}|"))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class LoggerSettings
    {
        public LogLevelEnum? Level { get; set; }
    }

    public class ApplicationTabChangeRequest
    {
        public ApplicationTab? Tab { get; set; }
    }

    public class LogLine
    {
        public DateTime Timestamp { get; set; }
        public LogLevelEnum Level { get; set; }
        public string Source { get; set; }
        public string Member { get; set; }
        public int Line { get; set; }
        public string Message { get; set; }

        public static LogLine Parse(string line)
        {
            string[] parts = line.Split('|');
            if (parts.Length >= 6)
            {
                return new LogLine()
                {
                    Timestamp = DateTime.Parse(parts[0]),
                    Level = (LogLevelEnum)Enum.Parse(typeof(LogLevelEnum), parts[1]),
                    Source = parts[2],
                    Member = parts[3],
                    Line = int.Parse(parts[4]),
                    Message = string.Join('|', parts.Skip(5)).Trim()
                };
            }
            else
            {
                // TODO: Should this throw an error?
                return null;
            }
        }
    }
}