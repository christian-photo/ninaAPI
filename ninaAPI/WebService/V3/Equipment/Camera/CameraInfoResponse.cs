#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;

namespace ninaAPI.WebService.V3.Equipment.Camera
{
    public class CameraInfoResponse : CameraInfo
    {
        public static CameraInfoResponse FromCam(ICameraMediator cam)
        {
            var info = cam.GetInfo();
            return new CameraInfoResponse()
            {
                Connected = info.Connected,
                CanSetTemperature = info.CanSetTemperature,
                HasDewHeater = info.HasDewHeater,
                IsExposing = info.IsExposing,
                PixelSize = info.PixelSize,
                BinX = info.BinX,
                BinY = info.BinY,
                Battery = info.Battery,
                Offset = info.Offset,
                OffsetMin = info.OffsetMin,
                OffsetMax = info.OffsetMax,
                DefaultOffset = info.DefaultOffset,
                USBLimit = info.USBLimit,
                USBLimitMin = info.USBLimitMin,
                USBLimitMax = info.USBLimitMax,
                DefaultGain = info.DefaultGain,
                GainMin = info.GainMin,
                GainMax = info.GainMax,
                CanSetGain = info.CanSetGain,
                Gains = info.Gains,
                CoolerOn = info.CoolerOn,
                CoolerPower = info.CoolerPower,
                HasShutter = info.HasShutter,
                Temperature = info.Temperature,
                TemperatureSetPoint = info.TemperatureSetPoint,
                ReadoutModes = info.ReadoutModes,
                ReadoutMode = info.ReadoutMode,
                ReadoutModeForSnapImages = info.ReadoutModeForSnapImages,
                ReadoutModeForNormalImages = info.ReadoutModeForNormalImages,
                IsSubSampleEnabled = info.IsSubSampleEnabled,
                SubSampleX = info.SubSampleX,
                SubSampleY = info.SubSampleY,
                SubSampleWidth = info.SubSampleWidth,
                SubSampleHeight = info.SubSampleHeight,
                ExposureMax = info.ExposureMax,
                ExposureMin = info.ExposureMin,
                LiveViewEnabled = info.LiveViewEnabled,
                CanShowLiveView = info.CanShowLiveView,
                SupportedActions = info.SupportedActions,
                CanSetUSBLimit = info.CanSetUSBLimit,
                Name = info.Name,
                DisplayName = info.DisplayName,
                DeviceId = info.DeviceId,
                BayerOffsetX = info.BayerOffsetX,
                BayerOffsetY = info.BayerOffsetY,
                BinningModes = info.BinningModes,
                BitDepth = info.BitDepth,
                CameraState = info.CameraState,
                XSize = info.XSize,
                YSize = info.YSize,
                CanGetGain = info.CanGetGain,
                CanSetOffset = info.CanSetOffset,
                CanSubSample = info.CanSubSample,
                Description = info.Description,
                DewHeaterOn = info.DewHeaterOn,
                DriverInfo = info.DriverInfo,
                DriverVersion = info.DriverVersion,
                ElectronsPerADU = info.ElectronsPerADU,
                ExposureEndTime = info.ExposureEndTime,
                LastDownloadTime = info.LastDownloadTime,
                SensorType = info.SensorType,
                Gain = info.Gain,
                HasBattery = info.HasBattery,
            };
        }
    }
}