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
using NINA.Core.Model.Equipment;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.FilterWheel
{
    public class FilterWheelController : WebApiController
    {
        private readonly IFilterWheelMediator filterWheel;
        private readonly IProfileService profile;
        private readonly IApplicationStatusMediator appStatus;
        private readonly ResponseHandler responseHandler;
        private readonly ApiProcessMediator processMediator;

        public FilterWheelController(IFilterWheelMediator filterWheel, IProfileService profile, IApplicationStatusMediator appStatus, ResponseHandler responseHandler, ApiProcessMediator processMediator)
        {
            this.filterWheel = filterWheel;
            this.profile = profile;
            this.appStatus = appStatus;
            this.responseHandler = responseHandler;
            this.processMediator = processMediator;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task FilterWheelInfo()
        {
            var info = filterWheel.GetInfo();

            await responseHandler.SendObject(HttpContext, new FilterWheelInfoResponse(info, profile.ActiveProfile));
        }

        [Route(HttpVerbs.Get, "/filter")]
        public async Task GetFilterInfo()
        {
            QueryParameter<short> positionParameter = new QueryParameter<short>("position", 0, true, (position) => position.IsBetween(0, profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1));
            short position = positionParameter.Get(HttpContext);

            FilterData filter = FilterData.FromFilterPosition(position, profile.ActiveProfile);

            await responseHandler.SendObject(HttpContext, filter);
        }

        [Route(HttpVerbs.Put, "/filter")]
        public async Task SetFilter()
        {
            QueryParameter<short> positionParameter = new QueryParameter<short>("position", 0, true, (position) => position.IsBetween(0, profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1));

            if (!filterWheel.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Filterwheel);
            }

            short position = positionParameter.Get(HttpContext);

            FilterInfo filter = FilterData.ToFilter(position, profile.ActiveProfile);

            var processId = processMediator.AddProcess(
                async (token) => await filterWheel.ChangeFilter(filter, token, appStatus.GetStatus()),
                ApiProcessType.FilterWheelChangeFilter
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }
    }
}