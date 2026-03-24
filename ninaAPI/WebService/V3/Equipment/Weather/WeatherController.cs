#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Interfaces;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.Weather
{
    public class WeatherController : IHttpController
    {
        private readonly IWeatherDataMediator weather;

        public WeatherController(IWeatherDataMediator weather)
        {
            this.weather = weather;
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, () => WeatherInfo());
        }

        public WeatherInfoResponse WeatherInfo()
        {
            return new WeatherInfoResponse(weather);
        }
    }
}
