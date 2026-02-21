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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.Switch
{
    public class SwitchController : WebApiController
    {
        private readonly ISwitchMediator @switch;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly ResponseHandler responseHandler;

        public SwitchController(ISwitchMediator @switch, IApplicationStatusMediator statusMediator, ResponseHandler responseHandler)
        {
            this.@switch = @switch;
            this.statusMediator = statusMediator;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task SwitchInfo()
        {
            await responseHandler.SendObject(HttpContext, @switch.GetInfo());
        }

        [Route(HttpVerbs.Patch, "/")]
        public async Task SwitchSetValue([JsonData] SwitchSetValueConfig config)
        {
            if (!@switch.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Switch);
            }
            var matches = @switch.GetInfo().WritableSwitches.Where(x => x.Id == config.SwitchId);
            if (!matches.Any())
            {
                throw new HttpException(HttpStatusCode.NotFound, $"Switch with id {config.SwitchId} not found");
            }
            var targetSwitch = matches.First();
            if (!config.Value.IsBetween(targetSwitch.Minimum, targetSwitch.Maximum))
            {
                throw CommonErrors.ParameterOutOfRange(nameof(config.Value), targetSwitch.Minimum, targetSwitch.Maximum);
            }

            // TODO: Check if this needs to be a process
            await @switch.SetSwitchValue(config.SwitchId, config.Value, statusMediator.GetStatus(), System.Threading.CancellationToken.None);
        }
    }

    public class SwitchSetValueConfig
    {
        [Range(0, short.MaxValue)]
        [Required]
        public short SwitchId { get; set; }

        [Required]
        public double Value { get; set; }
    }
}
