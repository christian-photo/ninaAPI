#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Model;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.Dome
{
    public class DomeController : IHttpController
    {
        private readonly IDomeMediator dome;
        private readonly IDomeFollower domeFollower;
        private readonly ITelescopeMediator mount;
        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;

        public DomeController(IDomeMediator dome, IDomeFollower domeFollower, ITelescopeMediator mount, ApiProcessMediator processMediator, ISerializerService serializer)
        {
            this.dome = dome;
            this.domeFollower = domeFollower;
            this.mount = mount;
            this.processMediator = processMediator;
            this.serializer = serializer;
        }

        public DomeInfoResponse DomeInfo()
        {
            DomeInfoResponse info = new DomeInfoResponse(dome, domeFollower);

            return info;
        }

        public object DomeOpenShutter()
        {
            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (dome.GetInfo().ShutterStatus == ShutterState.ShutterOpen || dome.GetInfo().ShutterStatus == ShutterState.ShutterOpening)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Shutter is already open or opening");
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await dome.OpenShutter(token),
                ApiProcessType.DomeOpenShutter
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public object DomeCloseShutter()
        {
            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (dome.GetInfo().ShutterStatus == ShutterState.ShutterClosed || dome.GetInfo().ShutterStatus == ShutterState.ShutterClosing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Shutter is already closed or closing");
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await dome.CloseShutter(token),
                ApiProcessType.DomeCloseShutter
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public StringResponse DomeStopMovement()
        {
            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (!dome.GetInfo().Slewing)
            {
                // TODO: Check if this conflicts with shutter open/close
                throw new HttpException(HttpStatusCode.Conflict, "Dome is not slewing");
            }

            var vm = typeof(DomeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dome) as DomeVM;
            vm.StopCommand.Execute(null);

            return new StringResponse("Dome movement stopped");
        }

        public async Task<StringResponse> DomeSetFollow(HttpSession session, DomeFollowBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (!dome.GetInfo().DriverCanFollow)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome driver cannot follow");
            }

            if (body.ShouldFollow)
            {
                await dome.EnableFollowing(session.RequestAborted);
            }
            else
            {
                await dome.DisableFollowing(session.RequestAborted);
            }

            return new StringResponse("Dome follower updated");
        }

        public async Task<StringResponse> DomeSync(HttpSession session, DomeSyncBody body)
        {
            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (!mount.GetInfo().Connected && (body?.Coordinates == null || body?.SideOfPier == null))
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (!dome.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is currently slewing");
            }

            bool success = await dome.SyncToScopeCoordinates(
                body?.Coordinates?.ToCoordinates() ?? mount.GetInfo().Coordinates,
                body?.SideOfPier ?? mount.GetInfo().SideOfPier,
                session.RequestAborted
            );

            if (!success)
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Dome sync failed");
            }

            return new StringResponse("Dome synced");
        }

        public async Task<object> DomeSlew(DomeSlewBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (!dome.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is currently slewing");
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await dome.SlewToAzimuth(body.Azimuth, token),
                ApiProcessType.DomeSlew
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public StringResponse DomeSetPark()
        {
            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (!dome.GetInfo().CanSetPark || dome.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome can not set park position");
            }

            var vm = typeof(DomeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dome) as DomeVM;
            vm.SetParkPositionCommand.Execute(null);

            return new StringResponse("Park position set");
        }

        public object DomePark()
        {
            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (dome.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is already parked");
            }
            else if (!dome.GetInfo().CanPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome can not park");
            }
            else if (dome.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is slewing");
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await dome.Park(token),
                ApiProcessType.DomePark
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public object DomeFindHome()
        {
            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (dome.GetInfo().AtHome)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is already homed");
            }
            else if (!dome.GetInfo().CanFindHome)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome can not find home");
            }
            else if (dome.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is parked");
            }
            else if (dome.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is slewing");
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await dome.FindHome(token),
                ApiProcessType.DomeFindHome
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, () => DomeInfo());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/shutter/open", () => DomeOpenShutter());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/shutter/close", () => DomeCloseShutter());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/stop-movement", () => DomeStopMovement());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/set-follow", (HttpSession session) => DomeSetFollow(session, serializer.Deserialize<DomeFollowBody>(session.Request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/sync", (HttpSession session) => DomeSync(session, serializer.Deserialize<DomeSyncBody>(session.Request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/slew", (HttpSession session) => DomeSlew(serializer.Deserialize<DomeSlewBody>(session.Request.BodyString)));
            server.Map(HttpVerbs.PATCH.ToString(), prefix + "/park", () => DomeSetPark());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/park", () => DomePark());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/home", () => DomeFindHome());
        }
    }

    public class DomeFollowBody
    {
        [Required]
        public bool ShouldFollow { get; set; }
    }

    public class DomeSyncBody
    {
        public HttpCoordinates Coordinates { get; set; }
        public PierSide SideOfPier { get; set; }
    }

    public class DomeSlewBody
    {
        [Required]
        [Range(0, 360)]
        public double Azimuth { get; set; }
    }
}
