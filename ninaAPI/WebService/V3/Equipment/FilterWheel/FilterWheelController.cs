#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.ComponentModel.DataAnnotations;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.FilterWheel
{
    public class FilterWheelController : IHttpController
    {
        private readonly IFilterWheelMediator filterWheel;
        private readonly IProfileService profile;
        private readonly IApplicationStatusMediator appStatus;
        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;

        public FilterWheelController(IFilterWheelMediator filterWheel, IProfileService profile, IApplicationStatusMediator appStatus, ApiProcessMediator processMediator, ISerializerService serializer)
        {
            this.filterWheel = filterWheel;
            this.profile = profile;
            this.appStatus = appStatus;
            this.processMediator = processMediator;
            this.serializer = serializer;
        }

        public FilterWheelInfoResponse FilterWheelInfo()
        {
            return new FilterWheelInfoResponse(filterWheel, profile.ActiveProfile);
        }

        public object SetFilter(HttpSession session)
        {
            QueryParameter<short> positionParameter = new QueryParameter<short>("position", 0, true, (position) => position.IsBetween(0, profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1));

            if (!filterWheel.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Filterwheel);
            }

            short position = positionParameter.Get(session.Request);

            FilterInfo filter = FilterData.ToFilter(position, profile.ActiveProfile);

            var processId = processMediator.AddProcess(
                async (token) => await filterWheel.ChangeFilter(filter, token, appStatus.GetStatus()),
                ApiProcessType.FilterWheelChangeFilter
            );
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public FilterData AddFilter(FilterData filter)
        {
            Validator.ValidateObject(filter, new ValidationContext(filter));
            // In the FilterData object, the position is not used, everything else is optional except the name
            var filterPosition = profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count;
            FilterInfo filterInfo = new FilterInfo(
                filter.Name,
                filter.FocusOffset ?? 0,
                (short)filterPosition,
                filter.AutoFocusExposureTime ?? -1,
                filter.AutoFocusBinning ?? new BinningMode(1, 1),
                filter.AutoFocusGain ?? -1,
                filter.AutoFocusOffset ?? -1
            );

            profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Add(filterInfo);

            return FilterData.FromFilter(filterInfo);
        }

        public StringResponse RemoveFilter(HttpSession session)
        {
            QueryParameter<short> positionParameter = new QueryParameter<short>("position", 0, true, (position) => position.IsBetween(0, profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count - 1));
            short position = positionParameter.Get(session.Request);

            var filters = profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            filters.RemoveAt(position);
            for (short i = 0; i < filters.Count; i++)
            {
                filters[i].Position = i;
            }

            return new StringResponse("Filter removed");
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, () => FilterWheelInfo());
            server.Map(HttpVerbs.PUT.ToString(), prefix + "/filter", (HttpSession session) => SetFilter(session));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/filter", (HttpSession session) => AddFilter(serializer.Deserialize<FilterData>(session.Request.BodyString)));
            server.Map(HttpVerbs.DELETE.ToString(), prefix + "/filter", (HttpSession session) => RemoveFilter(session));
        }
    }
}