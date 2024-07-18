#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebSockets;
using Namotion.Reflection;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace ninaAPI.WebService
{
    public class WebSocket : WebSocketModule
    {
        public WebSocket(string urlPath) : base(urlPath, true)
        {
            AdvancedAPI.Controls.Camera.Connected += CameraConnection;
            AdvancedAPI.Controls.Camera.Disconnected += CameraConnection;

            AdvancedAPI.Controls.Telescope.Connected += TelescopeConnection;
            AdvancedAPI.Controls.Telescope.Disconnected += TelescopeConnection;

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
            e.Image = null;
            e.Statistics.Histogram.Clear();

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "IMAGE-SAVE" },
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
            };

            Images.Add(response);

            await Send(response);
        }

        private async void LogProcessor_NINALogEventSaved(object sender, NINALogEvent e)
        {
            HttpResponse response = new HttpResponse()
            {
                Response = new Hashtable() 
                { 
                    { "Event", e.type },
                    { "Time", DateTime.Now }, 
                },
                Type = HttpResponse.TypeSocket
            };
            Events.Add(response);
            await Send(response);
        }


        private async Task CameraConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "CAMERA-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.Camera.GetInfo().Connected },
                { "Time", DateTime.Now },
            };
            Events.Add(response);
            await Send(response);
        }


        private async Task TelescopeConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "TELESCOPE-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.Telescope.GetInfo().Connected },
                { "Time", DateTime.Now },
            };
            Events.Add(response);
            await Send(response);
        }
        private async Task FocuserConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "FOCUSER-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.Focuser.GetInfo().Connected },
                { "Time", DateTime.Now },
            };
            Events.Add(response);
            await Send(response);
        }

        private async Task RotatorConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "ROTATOR-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.Rotator.GetInfo().Connected },
                { "Time", DateTime.Now },
            };
            Events.Add(response);
            await Send(response);
        }

        private async Task DomeConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "DOME-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.Dome.GetInfo().Connected },
                { "Time", DateTime.Now },
            };
            Events.Add(response);
            await Send(response);
        }

        private async Task FWConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "FILTERWHEEL-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.FilterWheel.GetInfo().Connected },
                { "Time", DateTime.Now },
            };
            Events.Add(response);
            await Send(response);
        }

        private async Task SwitchConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "SWITCH-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.Switch.GetInfo().Connected },
                { "Time", DateTime.Now },
            };
            Events.Add(response);
            await Send(response);
        }
        private async Task SafetyConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "SAFETY-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.SafetyMonitor.GetInfo().Connected },
                { "Time", DateTime.Now },
            };
            Events.Add(response);
            await Send(response);
        }
        private async Task GuiderConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "GUIDER-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.Guider.GetInfo().Connected },
                { "Time", DateTime.Now },
            };
            Events.Add(response);
            await Send(response);
        }

        private async Task FlatConnection(object arg1, EventArgs arg2)
        {
            HttpResponse response = new HttpResponse();

            response.Response = new Hashtable()
            {
                { "Event", "FLAT-CONNECTION" },
                { "Connected", AdvancedAPI.Controls.FlatDevice.GetInfo().Connected },
                { "Time", DateTime.Now },
            }; 
            Events.Add(response);

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
