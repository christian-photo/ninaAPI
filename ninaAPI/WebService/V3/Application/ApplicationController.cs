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
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.Properties;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Service;
using SimpleW;

namespace ninaAPI.WebService.V3.Application
{
    public class ApplicationController : IHttpController
    {
        private readonly IProfileService profileService;
        private readonly IApplicationMediator applicationMediator;
        private readonly ISerializerService serializer;

        public ApplicationController(IProfileService profileService, IApplicationMediator applicationMediator, ISerializerService serializer)
        {
            this.profileService = profileService;
            this.applicationMediator = applicationMediator;
            this.serializer = serializer;
        }

        public List<LogLine> GetLogEntries(HttpSession session)
        {
            PagerParameterSet pagerParameter = PagerParameterSet.Default();
            QueryParameter<LogLevelEnum> logLevel = new QueryParameter<LogLevelEnum>("level", LogLevelEnum.INFO, false);
            pagerParameter.Evaluate(session.Request);
            logLevel.Get(session.Request);

            string currentLogFile = Directory.GetFiles(Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs")).OrderByDescending(File.GetCreationTime).First();

            string[] logLines = [];

            using (var stream = new FileStream(currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string content = reader.ReadToEnd();
                logLines = content.Split('\n');
            }

            List<string> filteredLogLines = logLines.Where(line => IsLineAboveLevel(line, logLevel.Value)).Reverse().ToList();
            filteredLogLines = new Pager<string>(filteredLogLines).GetPage(pagerParameter.PageParameter.Value, pagerParameter.PageSizeParameter.Value);

            List<LogLine> parsed = filteredLogLines.Select(LogLine.Parse).ToList();

            return parsed;
        }

        public object GetApplicationTab()
        {
            IApplicationVM vm = (IApplicationVM)applicationMediator.GetType().GetField("handler", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(applicationMediator);
            int index = vm.TabIndex;

            return new { CurrentTab = (ApplicationTab)index };
        }

        public StringResponse SetApplicationTab(ApplicationTabChangeRequest request)
        {
            Validator.ValidateObject(request, new ValidationContext(request));

            applicationMediator.ChangeTab(request.Tab);
            return new StringResponse("Tab changed");
        }

        public async Task GetScreenshot(HttpSession session)
        {
            // Here only scale, size, format and quality are used and these are the only ones that will be documented
            ImageQueryParameterSet parameters = ImageQueryParameterSet.ByProfile(profileService.ActiveProfile);

            parameters.Evaluate(session.Request);

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

            await session.Response.Body(writer.Encode(parameters.Quality.Value), writer.MimeType).SendAsync();

            screenshot.Dispose();
        }

        public List<string> GetPlugins()
        {
            string path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
            List<string> plugins = [.. Directory.GetDirectories(path).Select(Path.GetFileName)];

            return plugins;
        }

        public object GetPluginSettings()
        {
            return new
            {
                AccessControlHeaderEnabled = Settings.Default.UseAccessControlHeader,
                ShouldCreateThumbnails = Settings.Default.CreateThumbnails,
            };
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

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/log", (HttpSession session) => GetLogEntries(session));
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/tab", () => GetApplicationTab());
            server.Map(HttpVerbs.PUT.ToString(), $"{prefix}/tab", (HttpSession session) => SetApplicationTab(serializer.Deserialize<ApplicationTabChangeRequest>(session.Request.BodyString)));
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/screenshot", async (HttpSession session) => await GetScreenshot(session));
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/plugins", () => GetPlugins());
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/plugin/settings", () => GetPluginSettings());
        }
    }

    public class ApplicationTabChangeRequest
    {
        [Required]
        public ApplicationTab Tab { get; set; }
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