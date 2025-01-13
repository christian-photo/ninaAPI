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
        public void ApplicationScreenshot([QueryField] bool resize, [QueryField] int quality, [QueryField] string size, [QueryField] double scale)
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

                if (scale == 0 && resize)
                    response.Response = BitmapHelper.ResizeAndConvertBitmap(source, new_size, quality);
                if (scale != 0 && resize)
                    response.Response = BitmapHelper.ScaleAndConvertBitmap(source, scale, quality);
                if (!resize)
                    response.Response = BitmapHelper.ScaleAndConvertBitmap(source, 1, quality);
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
