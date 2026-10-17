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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using SimpleW;
using SimpleW.Service.BasicAuth;

namespace ninaAPI.WebService.V3.Equipment.Switch
{
    [Route($"/v3/api/equipment/{EquipmentConstants.SwitchUrlName}")]
    [BasicAuth]
    public class SwitchController : Controller
    {
        private readonly ISwitchMediator @switch;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly ISerializerService serializer;

        public SwitchController(ISwitchMediator @switch, IApplicationStatusMediator statusMediator, ISerializerService serializer)
        {
            this.@switch = @switch;
            this.statusMediator = statusMediator;
            this.serializer = serializer;
        }

        [Route("GET", "/")]
        public SwitchInfoResponse SwitchInfo()
        {
            return new SwitchInfoResponse(@switch);
        }

        [Route("PATCH", "/")]
        public async Task<StringResponse> SwitchSetValue()
        {
            SwitchSetValueConfig config = serializer.Deserialize<SwitchSetValueConfig>(Request.BodyString);
            Validator.ValidateObject(config, new ValidationContext(config));

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
            await @switch.SetSwitchValue(config.SwitchId, config.Value, statusMediator.GetStatus(), Session.RequestAborted);

            return new StringResponse("Switch value updated");
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
