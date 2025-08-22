#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebApi;
using EmbedIO;
using System;
using EmbedIO.Routing;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;

namespace ninaAPI.WebService.V3.Equipment
{
    class CameraInfoResponse : CameraInfo
    {
        public static CameraInfoResponse FromCam(ICameraMediator cam)
        {
            return new CameraInfoResponse()
            {
                Connected = cam.GetInfo().Connected,
                CanSetTemperature = cam.GetInfo().CanSetTemperature,
                HasDewHeater = cam.GetInfo().HasDewHeater,
                IsExposing = cam.GetInfo().IsExposing,
                PixelSize = cam.GetInfo().PixelSize,
                BinX = cam.GetInfo().BinX,
                BinY = cam.GetInfo().BinY,
                Battery = cam.GetInfo().Battery,
                Offset = cam.GetInfo().Offset,
                OffsetMin = cam.GetInfo().OffsetMin,
                OffsetMax = cam.GetInfo().OffsetMax,
                DefaultOffset = cam.GetInfo().DefaultOffset,
                USBLimit = cam.GetInfo().USBLimit,
                USBLimitMin = cam.GetInfo().USBLimitMin,
                USBLimitMax = cam.GetInfo().USBLimitMax,
                DefaultGain = cam.GetInfo().DefaultGain,
                GainMin = cam.GetInfo().GainMin,
                GainMax = cam.GetInfo().GainMax,
                CanSetGain = cam.GetInfo().CanSetGain,
                Gains = cam.GetInfo().Gains,
                CoolerOn = cam.GetInfo().CoolerOn,
                CoolerPower = cam.GetInfo().CoolerPower,
                HasShutter = cam.GetInfo().HasShutter,
                Temperature = cam.GetInfo().Temperature,
                TemperatureSetPoint = cam.GetInfo().TemperatureSetPoint,
                ReadoutModes = cam.GetInfo().ReadoutModes,
                ReadoutMode = cam.GetInfo().ReadoutMode,
                ReadoutModeForSnapImages = cam.GetInfo().ReadoutModeForSnapImages,
                ReadoutModeForNormalImages = cam.GetInfo().ReadoutModeForNormalImages,
                IsSubSampleEnabled = cam.GetInfo().IsSubSampleEnabled,
                SubSampleX = cam.GetInfo().SubSampleX,
                SubSampleY = cam.GetInfo().SubSampleY,
                SubSampleWidth = cam.GetInfo().SubSampleWidth,
                SubSampleHeight = cam.GetInfo().SubSampleHeight,
                ExposureMax = cam.GetInfo().ExposureMax,
                ExposureMin = cam.GetInfo().ExposureMin,
                LiveViewEnabled = cam.GetInfo().LiveViewEnabled,
                CanShowLiveView = cam.GetInfo().CanShowLiveView,
                SupportedActions = cam.GetInfo().SupportedActions,
                CanSetUSBLimit = cam.GetInfo().CanSetUSBLimit,
                Name = cam.GetInfo().Name,
                DisplayName = cam.GetInfo().DisplayName,
                DeviceId = cam.GetInfo().DeviceId,
                BayerOffsetX = cam.GetInfo().BayerOffsetX,
                BayerOffsetY = cam.GetInfo().BayerOffsetY,
                BinningModes = cam.GetInfo().BinningModes,
                BitDepth = cam.GetInfo().BitDepth,
                CameraState = cam.GetInfo().CameraState,
                XSize = cam.GetInfo().XSize,
                YSize = cam.GetInfo().YSize,
                CanGetGain = cam.GetInfo().CanGetGain,
                CanSetOffset = cam.GetInfo().CanSetOffset,
                CanSubSample = cam.GetInfo().CanSubSample,
                Description = cam.GetInfo().Description,
                DewHeaterOn = cam.GetInfo().DewHeaterOn,
                DriverInfo = cam.GetInfo().DriverInfo,
                DriverVersion = cam.GetInfo().DriverVersion,
                ElectronsPerADU = cam.GetInfo().ElectronsPerADU,
                ExposureEndTime = cam.GetInfo().ExposureEndTime,
                LastDownloadTime = cam.GetInfo().LastDownloadTime,
                SensorType = cam.GetInfo().SensorType,
                Gain = cam.GetInfo().Gain,
                offsetMax = cam.GetInfo().offsetMax,
                offsetMin = cam.GetInfo().offsetMin,
                TargetTemp = cam.TargetTemp,
                AtTargetTemp = cam.AtTargetTemp,
            };
        }
        public double TargetTemp { get; set; }
        public bool AtTargetTemp { get; set; }
    }

    public class CameraController : WebApiController
    {
        [Route(HttpVerbs.Get, "/info")]
        public void CameraInfo()
        {
            Logger.Info("Requesting camera info");
            object response;
            try
            {
                ICameraMediator cam = AdvancedAPI.Controls.Camera;
                CameraInfoResponse info = CameraInfoResponse.FromCam(cam);
                response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError();
            }

            HttpContext.WriteResponse(response);
        }
    }
}
