#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Astrometry;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.Mount
{
    public class MountController : WebApiController
    {
        private readonly ITelescopeMediator mount;
        private readonly IProfileService profile;
        private readonly IImagingMediator imaging;
        private readonly IRotatorMediator rotator;
        private readonly IFilterWheelMediator filterwheel;
        private readonly IGuiderMediator guider;
        private readonly IDomeMediator dome;
        private readonly IDomeFollower domeFollower;
        private readonly IPlateSolverFactory plateSolver;
        private readonly IWindowServiceFactory windowService;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly ApiProcessMediator processMediator;
        private readonly ResponseHandler responseHandler;

        public MountController(
            ITelescopeMediator telescope,
            IProfileService profile,
            IImagingMediator imaging,
            IRotatorMediator rotator,
            IFilterWheelMediator filterwheel,
            IGuiderMediator guider,
            IDomeMediator dome,
            IDomeFollower domeFollower,
            IPlateSolverFactory plateSolver,
            IWindowServiceFactory windowService,
            IApplicationStatusMediator statusMediator,
            ApiProcessMediator processMediator,
            ResponseHandler responseHandler)
        {
            this.mount = telescope;
            this.profile = profile;
            this.imaging = imaging;
            this.rotator = rotator;
            this.filterwheel = filterwheel;
            this.guider = guider;
            this.dome = dome;
            this.domeFollower = domeFollower;
            this.plateSolver = plateSolver;
            this.windowService = windowService;
            this.statusMediator = statusMediator;
            this.processMediator = processMediator;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task MountInfo()
        {
            await responseHandler.SendObject(HttpContext, new MountInfoResponse(mount));
        }

        [Route(HttpVerbs.Post, "/home")]
        public async Task MountHome()
        {
            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (mount.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount parked");
            }
            else if (mount.GetInfo().AtHome)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount already homed");
            }
            else if (!mount.GetInfo().CanFindHome)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount cannot home");
            }

            if (mount.GetInfo().Slewing)
            {
                mount.StopSlew();
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await mount.FindHome(statusMediator.GetStatus(), token),
                ApiProcessType.MountHome
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Patch, "/tracking")]
        public async Task MountTrackingUpdate()
        {
            QueryParameter<TrackingMode> trackingModeParameter = new QueryParameter<TrackingMode>("mode", TrackingMode.Sidereal, true);

            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (mount.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount parked");
            }

            TrackingMode mode = trackingModeParameter.Get(HttpContext);

            if (!mount.GetInfo().TrackingEnabled)
            {
                mount.SetTrackingEnabled(true);
            }

            bool success = mount.SetTrackingMode(mode);

            if (!success)
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Tracking mode could not be set");
            }

            await responseHandler.SendObject(HttpContext, new
            {
                TrackingMode = mode
            });
        }

        [Route(HttpVerbs.Post, "/park")]
        public async Task MountPark()
        {
            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (mount.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount already parked");
            }
            else if (!mount.GetInfo().CanPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount cannot park");
            }

            if (mount.GetInfo().Slewing)
            {
                mount.StopSlew();
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await mount.ParkTelescope(statusMediator.GetStatus(), token),
                ApiProcessType.MountPark
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, "/unpark")]
        public async Task MountUnpark()
        {
            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (!mount.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount not parked");
            }

            Guid processId = processMediator.AddProcess(
                async (token) => await mount.UnparkTelescope(statusMediator.GetStatus(), token),
                ApiProcessType.MountPark // I think it should work if we leave it at that
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, "/flip")]
        public async Task MountFlip()
        {
            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (mount.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount parked");
            }
            else if (!mount.GetInfo().CanSlew)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount cannot slew");
            }

            // TODO: Check if we need more checks here

            Guid processId = processMediator.AddProcess(
                async (token) => await mount.MeridianFlip(mount.GetCurrentPosition(), token),
                ApiProcessType.MountSlew
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, "/slew")]
        public async Task MountSlew([JsonData] MountSlewConfig config)
        {
            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (mount.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount parked");
            }
            else if (!mount.GetInfo().CanSlew)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount cannot slew");
            }

            Guid processId;
            Coordinates coordinates = new Coordinates(Angle.ByDegree(config.RA), Angle.ByDegree(config.Dec), config.Epoch);

            if (config.SlewType == SlewType.Slew)
            {
                processId = processMediator.AddProcess(
                    async (token) => await mount.SlewToCoordinatesAsync(coordinates, token),
                    ApiProcessType.MountSlew
                );
            }
            else
            {
                SequenceItem item = null;
                if (config.SlewType == SlewType.Center)
                {
                    Center inst = new Center(profile, mount, imaging, filterwheel, guider,
                        dome, domeFollower, plateSolver, windowService);
                    inst.Coordinates.Coordinates = coordinates;

                    item = inst;
                }
                else if (config.SlewType == SlewType.Rotate)
                {
                    if (config.PositionAngle is null)
                    {
                        throw CommonErrors.ParameterMissing(nameof(config.PositionAngle));
                    }
                    CenterAndRotate inst = new CenterAndRotate(profile, mount, imaging, rotator, filterwheel,
                        guider, dome, domeFollower, plateSolver, windowService
                    );
                    inst.Coordinates.Coordinates = coordinates;
                    inst.PositionAngle = (double)config.PositionAngle;

                    item = inst;
                }

                processId = processMediator.AddProcess(
                    async (token) => await item.Execute(statusMediator.GetStatus(), token),
                    ApiProcessType.MountSlew
                );
            }

            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Post, "/stop-slew")]
        public async Task MountStopSlew()
        {
            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (!mount.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount not slewing");
            }

            mount.StopSlew();
            await responseHandler.SendObject(HttpContext, new StringResponse("Stopped slew"));
        }

        [Route(HttpVerbs.Patch, "/park")]
        public async Task MountSetPark()
        {
            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (!mount.GetInfo().CanSetPark || mount.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount cannot set park position");
            }
            ITelescope telescope = mount.GetDevice() as ITelescope;
            telescope.Setpark();

            await responseHandler.SendObject(HttpContext, new StringResponse("Park position set"));
        }

        [Route(HttpVerbs.Patch, "/sync")]
        public async Task MountSync([JsonData] MountSyncConfig config)
        {
            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (mount.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount slewing");
            }

            if (config.SolveAndSync)
            {
                SolveAndSync instruction = new SolveAndSync(profile, mount, rotator, imaging,
                    filterwheel, plateSolver, windowService);

                Guid processId = processMediator.AddProcess(
                    async (token) => await instruction.Execute(statusMediator.GetStatus(), token),
                    ApiProcessType.MountSolveAndSync
                );
                var result = processMediator.Start(processId);

                (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

                await responseHandler.SendObject(HttpContext, response, statusCode);
            }
            else
            {
                bool success = await mount.Sync(new Coordinates(Angle.ByDegree(config.RA), Angle.ByDegree(config.Dec), config.Epoch));

                if (!success)
                {
                    throw new HttpException(HttpStatusCode.InternalServerError, "Mount sync failed");
                }
                await responseHandler.SendObject(HttpContext, new StringResponse("Mount synced"));
            }
        }
    }
}
