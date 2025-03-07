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
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class WeatherWatcher : INinaWatcher, IWeatherDataConsumer
    {
        private readonly Func<object, EventArgs, Task> WeatherConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("WEATHER-CONNECTED");
        private readonly Func<object, EventArgs, Task> WeatherDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("WEATHER-DISCONNECTED");

        public void Dispose()
        {
            AdvancedAPI.Controls.Weather.RemoveConsumer(this);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.Weather.Connected += WeatherConnectedHandler;
            AdvancedAPI.Controls.Weather.Disconnected += WeatherDisconnectedHandler;
            AdvancedAPI.Controls.Weather.RegisterConsumer(this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Weather.Connected -= WeatherConnectedHandler;
            AdvancedAPI.Controls.Weather.Disconnected -= WeatherDisconnectedHandler;
            AdvancedAPI.Controls.Weather.RemoveConsumer(this);
        }

        public async void UpdateDeviceInfo(WeatherDataInfo deviceInfo)
        {
            await WebSocketV2.SendConsumerEvent("WEATHER");
        }
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/equipment/weather/info")]
        public void WeatherInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IWeatherDataMediator Weather = AdvancedAPI.Controls.Weather;

                WeatherDataInfo info = Weather.GetInfo();
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/weather/connect")]
        public async Task WeatherConnect([QueryField] bool skipRescan)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IWeatherDataMediator weather = AdvancedAPI.Controls.Weather;

                if (!weather.GetInfo().Connected)
                {
                    if (!skipRescan)
                    {
                        await weather.Rescan();
                    }
                    await weather.Connect();
                }
                response.Response = "Weather connected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/weather/disconnect")]
        public async Task WeatherDisconnect()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IWeatherDataMediator weather = AdvancedAPI.Controls.Weather;

                if (weather.GetInfo().Connected)
                {
                    await weather.Disconnect();
                }
                response.Response = "Weather disconnected";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }
}
