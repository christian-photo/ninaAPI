#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.FlatDevice
{
    [Route($"/v3/api/equipment/{EquipmentConstants.FlatDeviceUrlName}")]
    public class FlatController : Controller
    {
        private readonly IFlatDeviceMediator flatDevice;
        private readonly IApplicationStatusMediator appStatus;
        private readonly ISerializerService serializer;

        public FlatController(IFlatDeviceMediator flatDevice, IApplicationStatusMediator appStatus, ISerializerService serializer)
        {
            this.flatDevice = flatDevice;
            this.appStatus = appStatus;
            this.serializer = serializer;
        }

        [Route("GET", "/")]
        public FlatInfoResponse FlatInfo()
        {
            return new FlatInfoResponse(flatDevice);
        }


        [Route("PATCH", "/light")]
        public async Task<StringResponse> FlatLight()
        {
            FlatLightUpdateBody body = serializer.Deserialize<FlatLightUpdateBody>(Request.BodyString);
            Validator.ValidateObject(body, new ValidationContext(body));

            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!flatDevice.GetInfo().SupportsOnOff)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Flatdevice does not support on/off");
            }

            await flatDevice.ToggleLight(body.TurnOn, appStatus.GetStatus(), Session.RequestAborted);

            return new StringResponse("Flatdevice light set");
        }

        [Route("PATCH", "/brightness")]
        public async Task<StringResponse> FlatBrightness()
        {
            FlatBrightnessUpdateBody body = serializer.Deserialize<FlatBrightnessUpdateBody>(Request.BodyString);
            Validator.ValidateObject(body, new ValidationContext(body));

            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!body.Brightness.IsBetween(flatDevice.GetInfo().MinBrightness, flatDevice.GetInfo().MaxBrightness))
            {
                throw CommonErrors.ParameterOutOfRange(nameof(body.Brightness), flatDevice.GetInfo().MinBrightness, flatDevice.GetInfo().MaxBrightness);
            }

            await flatDevice.SetBrightness(body.Brightness, appStatus.GetStatus(), Session.RequestAborted);

            return new StringResponse("Flatdevice brightness set");
        }

        [Route("POST", "/cover/open")]
        public async Task<StringResponse> FlatCoverOpen()
        {
            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!flatDevice.GetInfo().SupportsOpenClose)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Flatdevice does not support open/close");
            }

            await flatDevice.OpenCover(appStatus.GetStatus(), Session.RequestAborted);

            return new StringResponse("Flatdevice cover open");
        }

        [Route("POST", "/cover/close")]
        public async Task<StringResponse> FlatCoverClose()
        {
            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!flatDevice.GetInfo().SupportsOpenClose)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Flatdevice does not support open/close");
            }

            await flatDevice.CloseCover(appStatus.GetStatus(), Session.RequestAborted);

            return new StringResponse("Flatdevice cover close");
        }
    }

    public class FlatLightUpdateBody
    {
        [Required]
        public bool TurnOn { get; set; }
    }

    public class FlatBrightnessUpdateBody
    {
        [Required]
        public int Brightness { get; set; }
    }
}
