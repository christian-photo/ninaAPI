#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility.Http;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V3.Equipment.Weather
{
    public class WeatherController : WebApiController
    {
        private readonly IWeatherDataMediator weather;
        private readonly ResponseHandler responseHandler;

        public WeatherController(IWeatherDataMediator weather, ResponseHandler responseHandler)
        {
            this.weather = weather;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task WeatherInfo()
        {
            await responseHandler.SendObject(HttpContext, weather.GetInfo());
        }
    }
}
