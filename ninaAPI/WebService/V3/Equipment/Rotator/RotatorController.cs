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
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.Rotator
{
    [Route($"/v3/api/equipment/{EquipmentConstants.RotatorUrlName}")]
    public class RotatorController : Controller
    {
        private readonly IRotatorMediator rotator;
        private readonly IProfileService profile;
        private readonly IImagingMediator imaging;
        private readonly ITelescopeMediator mount;
        private readonly IFilterWheelMediator filterwheel;
        private readonly IPlateSolverFactory plateSolver;
        private readonly IWindowServiceFactory windowService;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;

        public RotatorController(
            IRotatorMediator rotator,
            IProfileService profile,
            IImagingMediator imaging,
            ITelescopeMediator telescope,
            IFilterWheelMediator filterwheel,
            IPlateSolverFactory plateSolver,
            IWindowServiceFactory windowService,
            IApplicationStatusMediator statusMediator,
            ApiProcessMediator processMediator,
            ISerializerService serializer)
        {
            this.mount = telescope;
            this.profile = profile;
            this.imaging = imaging;
            this.rotator = rotator;
            this.filterwheel = filterwheel;
            this.plateSolver = plateSolver;
            this.windowService = windowService;
            this.statusMediator = statusMediator;
            this.processMediator = processMediator;
            this.serializer = serializer;
        }

        [Route("GET", "/")]
        public RotatorInfoResponse RotatorInfo()
        {
            return new RotatorInfoResponse(rotator);
        }

        [Route("POST", "/move")]
        public object RotatorMove()
        {
            RotatorMoveConfig config = serializer.Deserialize<RotatorMoveConfig>(Request.BodyString);
            Validator.ValidateObject(config, new ValidationContext(config));

            if (!rotator.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Rotator);
            }
            else if (rotator.GetInfo().IsMoving)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Rotator currently moving");
            }

            Guid processId;

            if (!config.MoveMechanical)
            {
                processId = processMediator.AddProcess(
                    async (token) => await rotator.Move(config.Position, token),
                    ApiProcessType.RotatorMove
                );
            }
            else
            {
                processId = processMediator.AddProcess(
                    async (token) => await rotator.MoveMechanical(config.Position, token),
                    ApiProcessType.RotatorMove
                );
            }

            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        [Route("PATCH", "/sync")]
        public object RotatorSync()
        {
            RotatorSyncConfig config = serializer.Deserialize<RotatorSyncConfig>(Request.BodyString);
            Validator.ValidateObject(config, new ValidationContext(config));

            if (!rotator.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Rotator);
            }
            else if (rotator.GetInfo().IsMoving)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Rotator currently moving");
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
                rotator.Sync(config.SkyAngle);

                return new StringResponse("Rotator synced");
            }
        }
    }
}
