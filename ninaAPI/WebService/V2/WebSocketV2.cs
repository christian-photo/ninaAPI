#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebSockets;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class WebSocketV2 : WebSocketModule
    {
        public WebSocketV2(string urlPath) : base(urlPath, true)
        {
            AdvancedAPI.Controls.Camera.Connected += CameraConnection;
            AdvancedAPI.Controls.Camera.Disconnected += CameraConnection;

            AdvancedAPI.Controls.Mount.Connected += TelescopeConnection;
            AdvancedAPI.Controls.Mount.Disconnected += TelescopeConnection;

            AdvancedAPI.Controls.Focuser.Connected += FocuserConnection;
            AdvancedAPI.Controls.Focuser.Disconnected += FocuserConnection;

            AdvancedAPI.Controls.Rotator.Connected += RotatorConnection;
            AdvancedAPI.Controls.Rotator.Disconnected += RotatorConnection;

            AdvancedAPI.Controls.Dome.Connected += DomeConnection;
            AdvancedAPI.Controls.Dome.Disconnected += DomeConnection;

            AdvancedAPI.Controls.FilterWheel.Connected += FWConnection;
            AdvancedAPI.Controls.FilterWheel.Disconnected += FWConnection;

            AdvancedAPI.Controls.Switch.Connected += SwitchConnection;
            AdvancedAPI.Controls.Switch.Disconnected += SwitchConnection;

            AdvancedAPI.Controls.SafetyMonitor.Connected += SafetyConnection;
            AdvancedAPI.Controls.SafetyMonitor.Disconnected += SafetyConnection;

            AdvancedAPI.Controls.Guider.Connected += GuiderConnection;
            AdvancedAPI.Controls.Guider.Disconnected += GuiderConnection;

            AdvancedAPI.Controls.FlatDevice.Connected += FlatConnection;
            AdvancedAPI.Controls.FlatDevice.Disconnected += FlatConnection;

            AdvancedAPI.Controls.Weather.Connected += WeatherConnection;
            AdvancedAPI.Controls.Weather.Disconnected += WeatherConnection;

            AdvancedAPI.Controls.ImageSaveMediator.ImageSaved += ImageSaved;

            AdvancedAPI.Server.LogProcessor.NINALogEventSaved += LogProcessor_NINALogEventSaved;
        }

        public static List<HttpResponse> Images = new List<HttpResponse>();
        public static List<HttpResponse> Events = new List<HttpResponse>();

        private async void ImageSaved(object sender, ImageSavedEventArgs e) 
        {
            if (!e.MetaData.Image.ImageType.Equals("LIGHT"))
                return;

            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "IMAGE-SAVE" },
                { "ImageStatistics", new Dictionary<string, object>() { 
                    { "ExposureTime", e.Duration },
                    { "Index", e.MetaData.Image.Id - 1 },
                    { "Filter", e.Filter },
                    { "RmsText", e.MetaData.Image.RecordedRMS.TotalText },
                    { "Temperature", e.MetaData.Camera.Temperature },
                    { "CameraName", e.MetaData.Camera.Name },
                    { "Gain", e.MetaData.Camera.Gain },
                    { "Offset", e.MetaData.Camera.Offset },
                    { "Date", DateTime.Now },
                    { "TelescopeName", e.MetaData.Telescope.Name },
                    { "FocalLength", e.MetaData.Telescope.FocalLength },
                    { "StDev", e.Statistics.StDev },
                    { "Mean", e.Statistics.Mean },
                    { "Median", e.Statistics.Median },
                    { "Stars", e.StarDetectionAnalysis.DetectedStars },
                    { "HFR", e.StarDetectionAnalysis.HFR }
                    }
                }
            };

            HttpResponse imageEvent = new HttpResponse() { Type = HttpResponse.TypeSocket, Response = new Dictionary<string, object>() { { "Event", "IMAGE-SAVE" }, { "Time", DateTime.Now } } };

            Images.Add(response);
            Events.Add(imageEvent);

            await Send(response);
        }

        private async void LogProcessor_NINALogEventSaved(object sender, NINALogEvent e)
        {
            await SendAndAddEvent(e.type, e.time);
        }


        private bool[] firstConnect = [true, true, true, true, true, true, true, true, true, true, true]; // on the first connect, the connection event is recieved twice, using bool to work around that
        private async Task CameraConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[0])
            {
                firstConnect[0] = false;
                return;
            }
            await SendAndAddEvent("CAMERA-CONNECTION");
        }


        private async Task TelescopeConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[1])
            {
                firstConnect[1] = false;
                return;
            }
            await SendAndAddEvent("TELESCOPE-CONNECTION");
        }
        private async Task FocuserConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[2])
            {
                firstConnect[2] = false;
                return;
            }
            await SendAndAddEvent("FOCUSER-CONNECTION");
        }

        private async Task RotatorConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[3])
            {
                firstConnect[3] = false;
                return;
            }
            await SendAndAddEvent("ROTATOR-CONNECTION");
        }

        private async Task DomeConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[4])
            {
                firstConnect[4] = false;
                return;
            }
            await SendAndAddEvent("DOME-CONNECTION");
        }

        private async Task FWConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[5])
            {
                firstConnect[5] = false;
                return;
            }
            await SendAndAddEvent("FILTERWHEEL-CONNECTION");
        }

        private async Task SwitchConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[6])
            {
                firstConnect[6] = false;
                return;
            }
            await SendAndAddEvent("SWITCH-CONNECTION");
        }
        private async Task SafetyConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[7])
            {
                firstConnect[7] = false;
                return;
            }
            await SendAndAddEvent("SAFETY-CONNECTION");
        }
        private async Task GuiderConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[8])
            {
                firstConnect[8] = false;
                return;
            }
            await SendAndAddEvent("GUIDER-CONNECTION");
        }

        private async Task FlatConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[9])
            {
                firstConnect[9] = false;
                return;
            }
            await SendAndAddEvent("FLAT-CONNECTION");
        }

        private async Task WeatherConnection(object arg1, EventArgs arg2)
        {
            if (firstConnect[10])
            {
                firstConnect[10] = false;
                return;
            }
            await SendAndAddEvent("WEATHER-CONNECTION");
        }

        public async Task SendAndAddEvent(string eventName)
        {
            HttpResponse response = new HttpResponse();
            response.Type = HttpResponse.TypeSocket;

            response.Response = new Hashtable()
            {
                { "Event", eventName },
            };
            HttpResponse Event = new HttpResponse() { Type = HttpResponse.TypeSocket, Response = new Dictionary<string, object>() { { "Event", eventName }, { "Time", DateTime.Now } } };
            Events.Add(Event);

            await Send(response);
        }

        public async Task SendAndAddEvent(string eventName, DateTime time)
        {
            HttpResponse response = new HttpResponse();
            response.Type = HttpResponse.TypeSocket;

            response.Response = new Hashtable()
            {
                { "Event", eventName },
            };
            HttpResponse Event = new HttpResponse() { Type = HttpResponse.TypeSocket, Response = new Dictionary<string, object>() { { "Event", eventName }, { "Time", time } } };
            Events.Add(Event);

            await Send(response);
        }

        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            Encoding.GetString(rxBuffer); // Do something with it
            return Task.CompletedTask;
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Logger.Debug("WebSocket connected " + context.RemoteEndPoint.ToString());
            return Task.CompletedTask;
        }

        public async Task Send(HttpResponse payload)
        {
            Logger.Debug("Sending " + payload.Response + " to WebSocket");
            foreach (IWebSocketContext context in ActiveContexts)
            {
                Logger.Debug("Sending to " + context.RemoteEndPoint.ToString());
                await SendAsync(context, JsonConvert.SerializeObject(payload));
            }
        }
    }
}
