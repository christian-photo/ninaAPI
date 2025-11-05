#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.CodeDom;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
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

        [Route(HttpVerbs.Get, "/info")]
        public async Task DomeInfo()
        {
            DomeInfoResponse info = new DomeInfoResponse(dome.GetInfo(), domeFollower);

            await responseHandler.SendObject(HttpContext, info);
        }

        [Route(HttpVerbs.Post, "/open-shutter")]
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

        [Route(HttpVerbs.Post, "/close-shutter")]
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

        [Route(HttpVerbs.Post, "/stop-movement")]
        public async Task DomeStopMovement()
        {
            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (!dome.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is not slewing");
            }

            var vm = typeof(DomeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dome) as DomeVM;
            vm.StopCommand.Execute(null);

            await responseHandler.SendObject(HttpContext, new StringResponse("Dome movement stopped"));
        }

        [Route(HttpVerbs.Post, "/set-follow")]
        public async Task DomeSetFollow()
        {
            QueryParameter<bool> enabledParameter = new QueryParameter<bool>("enabled", false, true);

            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (!dome.GetInfo().DriverCanFollow)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome driver cannot follow");
            }

            bool enabled = enabledParameter.Get(HttpContext);

            if (enabled)
            {
                await dome.EnableFollowing(System.Threading.CancellationToken.None);
            }
            else
            {
                await dome.DisableFollowing(System.Threading.CancellationToken.None);
            }

            await responseHandler.SendObject(HttpContext, new StringResponse("Dome follower updated"));
        }

        [Route(HttpVerbs.Post, "/sync")]
        public async Task DomeSync()
        {
            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (!dome.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is currently slewing");
            }

            bool success = await dome.SyncToScopeCoordinates(mount.GetInfo().Coordinates, mount.GetInfo().SideOfPier, System.Threading.CancellationToken.None);

            if (!success)
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Dome sync failed");
            }

            await responseHandler.SendObject(HttpContext, new StringResponse("Dome synced with mount"));
        }

        [Route(HttpVerbs.Post, "/slew")]
        public async Task DomeSlew()
        {
            QueryParameter<double> azimuthParameter = new QueryParameter<double>("azimuth", double.NaN, true, (azimuth) => azimuth.IsBetween(0, 360));

            if (!dome.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Dome);
            }
            else if (!dome.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Dome is currently slewing");
            }

            double azimuth = azimuthParameter.Get(HttpContext);

            Guid processId = processMediator.AddProcess(
                async (token) => await dome.SlewToAzimuth(azimuth, token),
                ApiProcessType.DomeSlew
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, "/set-park")]
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

        [Route(HttpVerbs.Post, "/park")]
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

        [Route(HttpVerbs.Post, "/find-home")]
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
}