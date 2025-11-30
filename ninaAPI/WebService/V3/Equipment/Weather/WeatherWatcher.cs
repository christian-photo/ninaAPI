#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.Weather
{
    public class WeatherWatcher : EventWatcher, IWeatherDataConsumer
    {
        private readonly IWeatherDataMediator weather;

        public WeatherWatcher(EventHistoryManager history, IWeatherDataMediator weather) : base(history)
        {
            this.weather = weather;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            weather.Connected += WeatherConnectedHandler;
            weather.Disconnected += WeatherDisconnectedHandler;

            weather.RegisterConsumer(this);
        }

        public override void StopWatchers()
        {
            weather.Connected -= WeatherConnectedHandler;
            weather.Disconnected -= WeatherDisconnectedHandler;

            weather.RemoveConsumer(this);
        }

        private async Task WeatherConnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("WEATHER-CONNECTED");
        private async Task WeatherDisconnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("WEATHER-DISCONNECTED");

        public void UpdateDeviceInfo(WeatherDataInfo deviceInfo)
        {

        }
    }
}
