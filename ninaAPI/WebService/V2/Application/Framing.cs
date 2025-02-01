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
using ninaAPI.Utility;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.Threading.Tasks;
using NINA.WPF.Base.SkySurvey;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces.Mediator;

namespace ninaAPI.WebService.V2
{
    public class FramingInfoContainer
    {
        public double BoundHeight { get; set; }
        public double BoundWidth { get; set; }
        public int CameraHeight { get; set; }
        public int CameraWidth { get; set; }
        public double CameraPixelSize { get; set; }
        public int DecDegrees { get; set; }
        public int DecMinutes { get; set; }
        public double DecSeconds { get; set; }
        public int RAHours { get; set; }
        public int RAMinutes { get; set; }
        public double RASeconds { get; set; }
        public double FieldOfView { get; set; }
        public double FocalLength { get; set; }
        public int HorizontalPanels { get; set; }
        public int VerticalPanels { get; set; }
        public FramingRectangle Rectangle { get; set; }
    }
    public partial class ControllerV2
    {
        private FramingInfoContainer GetFramingInfo()
        {
            IFramingAssistantVM framing = AdvancedAPI.Controls.FramingAssistant;
            FramingInfoContainer info = new FramingInfoContainer()
            {
                BoundHeight = framing.BoundHeight,
                BoundWidth = framing.BoundWidth,
                CameraHeight = framing.CameraHeight,
                CameraWidth = framing.CameraWidth,
                CameraPixelSize = framing.CameraPixelSize,
                DecDegrees = framing.DecDegrees,
                DecMinutes = framing.DecMinutes,
                DecSeconds = framing.DecSeconds,
                RAHours = framing.RAHours,
                RAMinutes = framing.RAMinutes,
                RASeconds = framing.RASeconds,
                FieldOfView = framing.FieldOfView,
                FocalLength = framing.FocalLength,
                HorizontalPanels = framing.HorizontalPanels,
                VerticalPanels = framing.VerticalPanels,
                Rectangle = framing.Rectangle,
            };
            return info;
        }

        [Route(HttpVerbs.Get, "/framing/info")]
        public void FramingInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                response.Response = GetFramingInfo();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/framing/set-source")]
        public void FramingSetSource([QueryField] string source)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFramingAssistantVM framing = AdvancedAPI.Controls.FramingAssistant;

                if (Enum.TryParse(source, out SkySurveySource result))
                {
                    framing.FramingAssistantSource = result;
                    response.Response = "Source updated";
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("Invalid source", 400));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        // Framing Assistant View needs to have benn opened once before to be initialized
        [Route(HttpVerbs.Get, "/framing/set-coordinates")]
        public void FramingSetCoordinates([QueryField] double RAangle, [QueryField] double DECangle)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFramingAssistantVM framing = AdvancedAPI.Controls.FramingAssistant;

                framing.SetCoordinates(new DeepSkyObject(string.Empty, new Coordinates(Angle.ByDegree(RAangle), Angle.ByDegree(DECangle), Epoch.J2000), string.Empty, null));
                response.Response = "Coordinates updated";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/framing/slew")]
        public async Task FramingSlew([QueryField] string slew_option, [QueryField] bool waitForResult)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFramingAssistantVM framing = AdvancedAPI.Controls.FramingAssistant;

                if (waitForResult)
                    await framing.SlewToCoordinatesCommand.ExecuteAsync(slew_option ?? string.Empty); // SlewOption is either Center Rotate or empty
                else
                    framing.SlewToCoordinatesCommand.Execute(slew_option ?? string.Empty); // SlewOption is either Center Rotate or empty

                response.Response = waitForResult ? "Slew finished" : "Slew started";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/framing/set-rotation")]
        public void FramingSetRotation([QueryField] double rotation)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IFramingAssistantVM framing = AdvancedAPI.Controls.FramingAssistant;

                framing.Rectangle.TotalRotation = 360 - rotation;
                response.Response = "Rotation updated";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/framing/determine-rotation")]
        public async Task FramingDetermineRotation([QueryField] bool waitForResult)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ICameraMediator camera = AdvancedAPI.Controls.Camera;
                IFramingAssistantVM framing = AdvancedAPI.Controls.FramingAssistant;
                if (framing.RectangleCalculated && camera.GetInfo().Connected && camera.IsFreeToCapture(framing))
                {
                    Task<bool> rotationTask = (Task<bool>)framing.GetType().GetMethod("GetRotationFromCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(framing, [null]);

                    if (waitForResult)
                    {
                        await rotationTask;
                        response.Response = GetFramingInfo();
                        response.Success = rotationTask.Result;
                    }
                    else
                    {
                        response.Response = "Determine rotation started";
                    }
                }
                else
                {
                    response = CoreUtility.CreateErrorTable(new Error("Could not start process", 400));
                }
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
