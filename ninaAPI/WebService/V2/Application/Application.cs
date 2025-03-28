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
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using NINA.Image.ImageAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using NINA.Core.Utility.WindowService;

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
    }
}
