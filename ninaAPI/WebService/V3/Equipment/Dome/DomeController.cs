#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Model;
using Swan;

namespace ninaAPI.WebService.V3.Equipment.Dome
{
    public class DomeController : WebApiController
    {
        private readonly ResponseHandler responseHandler;
        private readonly IDomeMediator dome;
        private readonly IDomeFollower domeFollower;
        private readonly ITelescopeMediator mount;
        private readonly ApiProcessMediator processMediator;

        public DomeController(ResponseHandler responseHandler, IDomeMediator dome, IDomeFollower domeFollower, ITelescopeMediator mount, ApiProcessMediator processMediator)
        {
            this.responseHandler = responseHandler;
            this.dome = dome;
            this.domeFollower = domeFollower;
            this.mount = mount;
            this.processMediator = processMediator;
        }

        [Route(HttpVerbs.Get, $"/{EquipmentConstants.DomeUrlName}")]
        public async Task DomeInfo()
        {
            DomeInfoResponse info = new DomeInfoResponse(dome, domeFollower);

            await responseHandler.SendObject(HttpContext, info);
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.DomeUrlName}/shutter/open")]
        public async Task DomeOpenShutter()
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

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.DomeUrlName}/shutter/close")]
        public async Task DomeCloseShutter()
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

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.DomeUrlName}/stop-movement")]
        public async Task DomeStopMovement()
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

            await responseHandler.SendObject(HttpContext, new StringResponse("Dome movement stopped"));
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.DomeUrlName}/set-follow")]
        public async Task DomeSetFollow([JsonData] DomeFollowBody body)
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
                await dome.EnableFollowing(System.Threading.CancellationToken.None);
            }
            else
            {
                await dome.DisableFollowing(System.Threading.CancellationToken.None);
            }

            await responseHandler.SendObject(HttpContext, new StringResponse("Dome follower updated"));
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.DomeUrlName}/sync")]
        public async Task DomeSync([JsonData] DomeSyncBody body)
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
                System.Threading.CancellationToken.None
            );

            if (!success)
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Dome sync failed");
            }

            await responseHandler.SendObject(HttpContext, new StringResponse("Dome synced"));
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.DomeUrlName}/slew")]
        public async Task DomeSlew([JsonData] DomeSlewBody body)
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

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Patch, $"/{EquipmentConstants.DomeUrlName}/park")]
        public async Task DomeSetPark()
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

            await responseHandler.SendObject(HttpContext, new StringResponse("Park position set"));
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.DomeUrlName}/park")]
        public async Task DomePark()
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

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, $"/{EquipmentConstants.DomeUrlName}/home")]
        public async Task DomeFindHome()
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

            await responseHandler.SendObject(HttpContext, response, statusCode);
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
