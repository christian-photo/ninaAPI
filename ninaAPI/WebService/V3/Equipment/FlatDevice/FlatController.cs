#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.FlatDevice
{
    public class FlatController : WebApiController
    {
        private readonly IFlatDeviceMediator flatDevice;
        private readonly IApplicationStatusMediator appStatus;
        private readonly ResponseHandler responseHandler;

        public FlatController(IFlatDeviceMediator flatDevice, IApplicationStatusMediator appStatus, ResponseHandler responseHandler)
        {
            this.flatDevice = flatDevice;
            this.appStatus = appStatus;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task FlatInfo()
        {
            var info = flatDevice.GetInfo();
            await responseHandler.SendObject(HttpContext, new FlatInfoResponse(info));
        }

        [Route(HttpVerbs.Put, "/light")]
        public async Task FlatLight([JsonData] FlatLightUpdateBody body)
        {
            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!flatDevice.GetInfo().SupportsOnOff)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Flatdevice does not support on/off");
            }

            await flatDevice.ToggleLight(body.TurnOn, appStatus.GetStatus(), HttpContext.CancellationToken);

            await responseHandler.SendObject(HttpContext, new StringResponse("Flatdevice light set"));
        }

        [Route(HttpVerbs.Patch, "/brightness")]
        public async Task FlatBrightness([JsonData] FlatBrightnessUpdateBody body)
        {
            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (body.Brightness < flatDevice.GetInfo().MinBrightness || body.Brightness > flatDevice.GetInfo().MaxBrightness)
            {
                throw CommonErrors.ParameterOutOfRange(nameof(body.Brightness), flatDevice.GetInfo().MinBrightness, flatDevice.GetInfo().MaxBrightness);
            }

            await flatDevice.SetBrightness(body.Brightness, appStatus.GetStatus(), HttpContext.CancellationToken);

            await responseHandler.SendObject(HttpContext, new StringResponse("Flatdevice brightness set"));
        }

        [Route(HttpVerbs.Put, "/cover")]
        public async Task FlatCover([JsonData] FlatCoverUpdateBody body)
        {
            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!flatDevice.GetInfo().SupportsOpenClose)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Flatdevice does not support open/close");
            }

            if (body.Open)
            {
                await flatDevice.OpenCover(appStatus.GetStatus(), HttpContext.CancellationToken);
            }
            else
            {
                await flatDevice.CloseCover(appStatus.GetStatus(), HttpContext.CancellationToken);
            }

            await responseHandler.SendObject(HttpContext, new StringResponse("Flatdevice cover set"));
        }
    }

    public class FlatLightUpdateBody
    {
        public bool TurnOn { get; set; }
    }

    public class FlatBrightnessUpdateBody
    {
        public int Brightness { get; set; }
    }

    public class FlatCoverUpdateBody
    {
        public bool Open { get; set; }
    }
}