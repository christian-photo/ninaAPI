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
using NINA.Astrometry;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.Mount
{
    public class MountController : IHttpController
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
        private readonly IMeridianFlipVMFactory flipVMFactory;
        private readonly ICameraMediator camera;
        private readonly IFocuserMediator focuser;
        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;

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
            IMeridianFlipVMFactory flipVMFactory,
            ICameraMediator camera,
            IFocuserMediator focuser,
            ApiProcessMediator processMediator,
            ISerializerService serializer)
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
            this.flipVMFactory = flipVMFactory;
            this.camera = camera;
            this.focuser = focuser;
            this.processMediator = processMediator;
            this.serializer = serializer;
        }

        public MountInfoResponse MountInfo()
        {
            return new MountInfoResponse(mount);
        }

        public object MountHome()
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

            return (response, statusCode);
        }

        public StringResponse MountTrackingUpdate(UpdateTrackingModeBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (mount.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount parked");
            }

            if (!mount.GetInfo().TrackingEnabled)
            {
                mount.SetTrackingEnabled(true);
            }

            bool success = mount.SetTrackingMode(body.TrackingMode);

            if (!success)
            {
                throw new HttpException(HttpStatusCode.InternalServerError, "Tracking mode could not be set");
            }

            return new StringResponse("Tracking mode updated");
        }

        public object MountPark()
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

            return (response, statusCode);
        }

        public async Task<StringResponse> MountUnpark(HttpSession session)
        {
            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (!mount.GetInfo().AtPark)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount not parked");
            }

            await mount.UnparkTelescope(statusMediator.GetStatus(), session.RequestAborted);

            return new StringResponse("Unparked");
        }

        public object MountFlip(MountFlipConfig config)
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
            else if (!mount.GetInfo().TrackingEnabled)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount is not tracking");
            }
            else if (config.Recenter && !camera.GetInfo().Connected)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Recenter is enabled but camera is disconnected");
            }
            else if (config.AutofocusAfterFlip && (!focuser.GetInfo().Connected || !camera.GetInfo().Connected))
            {
                throw new HttpException(HttpStatusCode.Conflict, "Autofocus is enabled but focuser and/or camera is disconnected");
            }

            Guid processId = processMediator.AddProcess(MeridianFlipProcess.Create(flipVMFactory.Create(), mount, statusMediator));
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public object MountSlew(MountSlewConfig config)
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
            Coordinates coordinates = config.Coordinates.ToCoordinates();

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

            return (response, statusCode);
        }

        public StringResponse MountStopSlew()
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
            return new StringResponse("Stopped slew");
        }

        public StringResponse MountSetPark()
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

            return new StringResponse("Park position set");
        }

        public async Task<object> MountSync(MountSyncConfig config)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

            if (!mount.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Mount);
            }
            else if (mount.GetInfo().Slewing)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Mount slewing");
            }
            else if (!config.SolveAndSync && config.Coordinates == null)
            {
                throw new HttpException(HttpStatusCode.BadRequest, "Either coordinates or solve and sync must be provided");
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

                return (response, statusCode);
            }
            else
            {
                bool success = await mount.Sync(config.Coordinates.ToCoordinates());

                if (!success)
                {
                    throw new HttpException(HttpStatusCode.InternalServerError, "Mount sync failed");
                }
                return new StringResponse("Mount synced");
            }
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, () => MountInfo());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/home", () => MountHome());
            server.Map(HttpVerbs.PATCH.ToString(), prefix + "/tracking", (HttpRequest request) => MountTrackingUpdate(serializer.Deserialize<UpdateTrackingModeBody>(request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/park", () => MountPark());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/unpark", async (HttpSession session) => await MountUnpark(session));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/flip", (HttpRequest request) => MountFlip(serializer.Deserialize<MountFlipConfig>(request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/slew", (HttpRequest request) => MountSlew(serializer.Deserialize<MountSlewConfig>(request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/slew/stop", () => MountStopSlew());
            server.Map(HttpVerbs.PATCH.ToString(), prefix + "/park", () => MountSetPark());
            server.Map(HttpVerbs.PATCH.ToString(), prefix + "/sync", (HttpRequest request) => MountSync(serializer.Deserialize<MountSyncConfig>(request.BodyString)));
        }
    }
}
