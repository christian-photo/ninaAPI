#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Net;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.FlatDevice
{
    public class FlatController : IHttpController
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

        public FlatInfoResponse FlatInfo()
        {
            return new FlatInfoResponse(flatDevice);
        }

        public async Task<StringResponse> FlatLight(HttpSession session, FlatLightUpdateBody body)
        {
            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!flatDevice.GetInfo().SupportsOnOff)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Flatdevice does not support on/off");
            }

            await flatDevice.ToggleLight(body.TurnOn, appStatus.GetStatus(), session.RequestAborted);

            return new StringResponse("Flatdevice light set");
        }

        public async Task<StringResponse> FlatBrightness(HttpSession session, FlatBrightnessUpdateBody body)
        {
            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!body.Brightness.IsBetween(flatDevice.GetInfo().MinBrightness, flatDevice.GetInfo().MaxBrightness))
            {
                throw CommonErrors.ParameterOutOfRange(nameof(body.Brightness), flatDevice.GetInfo().MinBrightness, flatDevice.GetInfo().MaxBrightness);
            }

            await flatDevice.SetBrightness(body.Brightness, appStatus.GetStatus(), session.RequestAborted);

            return new StringResponse("Flatdevice brightness set");
        }

        public async Task<StringResponse> FlatCoverOpen(HttpSession session)
        {
            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!flatDevice.GetInfo().SupportsOpenClose)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Flatdevice does not support open/close");
            }

            await flatDevice.OpenCover(appStatus.GetStatus(), session.RequestAborted);

            return new StringResponse("Flatdevice cover open");
        }

        public async Task<StringResponse> FlatCoverClose(HttpSession session)
        {
            if (!flatDevice.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.FlatDevice);
            }
            else if (!flatDevice.GetInfo().SupportsOpenClose)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Flatdevice does not support open/close");
            }

            await flatDevice.CloseCover(appStatus.GetStatus(), session.RequestAborted);

            return new StringResponse("Flatdevice cover close");
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, () => FlatInfo());
            server.Map(HttpVerbs.PATCH.ToString(), prefix + "/light", async (HttpSession session) => await FlatLight(session, serializer.Deserialize<FlatLightUpdateBody>(session.Request.BodyString)));
            server.Map(HttpVerbs.PATCH.ToString(), prefix + "/brightness", async (HttpSession session) => await FlatBrightness(session, serializer.Deserialize<FlatBrightnessUpdateBody>(session.Request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/cover/open", async (HttpSession session) => await FlatCoverOpen(session));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/cover/close", async (HttpSession session) => await FlatCoverClose(session));
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
}
