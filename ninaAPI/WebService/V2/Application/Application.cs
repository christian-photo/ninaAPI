#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebApi;
using EmbedIO;
using System;
using EmbedIO.Routing;
using System.Drawing;
using ninaAPI.Utility;
using NINA.Core.Utility;
using System.Windows.Media.Imaging;
using NINA.Image.ImageAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.Collections;
using System.Windows.Forms;
using NINA.Sequencer.SequenceItem.Utility;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/application/switch-tab")]
        public void ApplicationSwitchTab([QueryField] string tab)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                response.Response = "Switched tab";
                switch (tab)
                {
                    case "equipment":
                        AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.EQUIPMENT);
                        break;
                    case "skyatlas":
                        AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.SKYATLAS);
                        break;
                    case "framing":
                        AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.FRAMINGASSISTANT);
                        break;
                    case "flatwizard":
                        AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.FLATWIZARD);
                        break;
                    case "sequencer":
                        AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.SEQUENCE);
                        break;
                    case "imaging":
                        AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.IMAGING);
                        break;
                    case "options":
                        AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.OPTIONS);
                        break;
                    default:
                        response = CoreUtility.CreateErrorTable(new Error("Invalid application tab", 400));
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/application/get-tab")]
        public void ApplicationGetTab()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IApplicationVM vm = (IApplicationVM)AdvancedAPI.Controls.Application.GetType().GetField("handler", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Application);
                int index = vm.TabIndex;
                switch (index)
                {
                    case 0:
                        response.Response = "equipment";
                        break;
                    case 1:
                        response.Response = "skyatlas";
                        break;
                    case 2:
                        response.Response = "framing";
                        break;
                    case 3:
                        response.Response = "flatwizard";
                        break;
                    case 4:
                        response.Response = "sequencer";
                        break;
                    case 5:
                        response.Response = "imaging";
                        break;
                    case 6:
                        response.Response = "options";
                        break;
                    default:
                        response = CoreUtility.CreateErrorTable(new Error("Invalid application tab", 400));
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/application/screenshot")]
        public async Task ApplicationScreenshot([QueryField] bool resize, [QueryField] int quality, [QueryField] string size, [QueryField] double scale, [QueryField] bool stream)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                quality = Math.Clamp(quality, -1, 100);
                if (quality == 0)
                    quality = -1; // quality should be set to -1 for png if omitted

                if (resize && string.IsNullOrWhiteSpace(size))
                    size = "640x480";

                Size new_size = Size.Empty;
                if (resize)
                {
                    string[] s = size.Split('x');
                    int width = int.Parse(s[0]);
                    int height = int.Parse(s[1]);
                    new_size = new Size(width, height);
                }

                Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                        Screen.PrimaryScreen.Bounds.Y,
                                        0, 0,
                                        screenshot.Size,
                                        CopyPixelOperation.SourceCopy);
                }

                BitmapSource source = ImageUtility.ConvertBitmap(screenshot);

                if (stream)
                {
                    BitmapEncoder encoder = null;
                    if (scale == 0 && resize)
                    {
                        BitmapSource image = BitmapHelper.ResizeBitmap(source, new_size);
                        encoder = BitmapHelper.GetEncoder(image, quality);
                    }
                    if (scale != 0 && resize)
                    {
                        BitmapSource image = BitmapHelper.ScaleBitmap(source, scale);
                        encoder = BitmapHelper.GetEncoder(image, quality);
                    }
                    if (!resize)
                    {
                        BitmapSource image = BitmapHelper.ScaleBitmap(source, 1);
                        encoder = BitmapHelper.GetEncoder(image, quality);
                    }
                    HttpContext.Response.ContentType = quality == -1 ? "image/png" : "image/jpg";
                    using (MemoryStream memory = new MemoryStream())
                    {
                        encoder.Save(memory);
                        await HttpContext.Response.OutputStream.WriteAsync(memory.ToArray());
                        return;
                    }
                }
                else
                {

                    if (scale == 0 && resize)
                        response.Response = BitmapHelper.ResizeAndConvertBitmap(source, new_size, quality);
                    if (scale != 0 && resize)
                        response.Response = BitmapHelper.ScaleAndConvertBitmap(source, scale, quality);
                    if (!resize)
                        response.Response = BitmapHelper.ScaleAndConvertBitmap(source, 1, quality);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/application/plugins")]
        public void ApplicationPlugins()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                string path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
                List<string> plugins = [.. Directory.GetDirectories(path).Select(Path.GetFileName)];
                response.Response = plugins;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/application/logs")]
        public void GetRecentLogs([QueryField(true)] int lineCount, [QueryField] string level)
        {
            HttpResponse response = new HttpResponse();

            List<Hashtable> logs = new List<Hashtable>();

            if (string.IsNullOrEmpty(level))
            {
                level = string.Empty;
            }

            try
            {
                string currentLogFile = Directory.GetFiles(Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs")).OrderByDescending(File.GetCreationTime).First();

                string[] logLines = [];

                using (var stream = new FileStream(currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string content = reader.ReadToEnd();
                        logLines = content.Split('\n');
                    }
                }

                List<string> filteredLogLines = new List<string>();
                foreach (string line in logLines)
                {
                    bool valid = true;

                    if (!line.Contains('|' + level + '|') && !string.IsNullOrEmpty(level))
                    {
                        valid = false;
                    }
                    if (line.Contains("DATE|LEVEL|SOURCE|MEMBER|LINE|MESSAGE"))
                    {
                        valid = false;
                    }
                    if (valid)
                    {
                        filteredLogLines.Add(line);
                    }
                }
                IEnumerable<string> lines = filteredLogLines.TakeLast(lineCount);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 6)
                    {
                        logs.Add(new Hashtable() {
                            { "Timestamp", parts[0] },
                            { "Level", parts[1] },
                            { "Source", parts[2] },
                            { "Member", parts[3] },
                            { "Line", parts[4] },
                            { "Message", string.Join('|', parts.Skip(5)).Trim() }
                        });
                    }
                }
                logs.Reverse();

                response.Response = logs;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        // [Route(HttpVerbs.Get, "/application/windows")]
        // public void GetApplicationWindows()
        // {
        //     HttpResponse response = new HttpResponse();

        //     try
        //     {
        //         response.Response = CreateWindowMeta();
        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.Error(ex);
        //         response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
        //     }

        //     HttpContext.WriteToResponse(response);
        // }

        // [Route(HttpVerbs.Get, "/application/windows/close")]
        // public void GetApplicationWindows([QueryField] int windowId)
        // {
        //     HttpResponse response = new HttpResponse();

        //     try
        //     {
        //         var windowMeta = CreateWindowMeta().Find(w => w.Id == windowId);
        //         if (windowMeta is not null)
        //         {
        //             System.Windows.Application.Current.Dispatcher.Invoke(() =>
        //             {
        //                 foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
        //                 {
        //                     if (window.DataContext?.GetType().FullName == windowMeta.DataContextClassName && window.Title == windowMeta.Title)
        //                     {
        //                         window.Close();
        //                         break;
        //                     }
        //                 }
        //             });

        //             response.Response = "Closed window";
        //         }
        //         else
        //         {
        //             response = CoreUtility.CreateErrorTable(new Error("Invalid window id", 400));
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.Error(ex);
        //         response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
        //     }

        //     HttpContext.WriteToResponse(response);
        // }

        // private List<WindowMeta> CreateWindowMeta()
        // {
        //     List<WindowMeta> windows = new List<WindowMeta>();
        //     System.Windows.Application.Current.Dispatcher.Invoke(() =>
        //     {
        //         foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
        //         {
        //             WindowMeta meta = new WindowMeta
        //             {
        //                 Title = window.Title,
        //                 DataContextClassName = window.DataContext?.GetType().FullName,
        //             };
        //             windows.Add(meta);
        //         }
        //     });

        //     return windows;
        // }
    }

    internal class WindowMeta
    {
        public int Id => Math.Abs((Title + DataContextClassName).GetHashCode());
        public string Title { get; set; }
        public string DataContextClassName { get; set; }
    }
}
