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
            AdvancedAPI.Controls.Camera.Connected += async (_, _) => await SendAndAddEvent("CAMERA-CONNECTED");
            AdvancedAPI.Controls.Camera.Disconnected += async (_, _) => await SendAndAddEvent("CAMERA-DISCONNECTED");
            AdvancedAPI.Controls.Camera.DownloadTimeout += async (_, _) => await SendAndAddEvent("CAMERA-DOWNLOAD-TIMEOUT");

            AdvancedAPI.Controls.Mount.Connected += async (_, _) => await SendAndAddEvent("MOUNT-CONNECTED");
            AdvancedAPI.Controls.Mount.Disconnected += async (_, _) => await SendAndAddEvent("MOUNT-DISCONNECTED");
            AdvancedAPI.Controls.Mount.BeforeMeridianFlip += async (_, _) => await SendAndAddEvent("MOUNT-BEFORE-FLIP");
            AdvancedAPI.Controls.Mount.AfterMeridianFlip += async (_, _) => await SendAndAddEvent("MOUNT-AFTER-FLIP");
            AdvancedAPI.Controls.Mount.Homed += async (_, _) => await SendAndAddEvent("MOUNT-HOMED");
            AdvancedAPI.Controls.Mount.Parked += async (_, _) => await SendAndAddEvent("MOUNT-PARKED");
            AdvancedAPI.Controls.Mount.Unparked += async (_, _) => await SendAndAddEvent("MOUNT-UNPARKED");

            AdvancedAPI.Controls.Focuser.Connected += async (_, _) => await SendAndAddEvent("FOCUSER-CONNECTED");
            AdvancedAPI.Controls.Focuser.Disconnected += async (_, _) => await SendAndAddEvent("FOCUSER-DISCONNECTED");

            AdvancedAPI.Controls.Rotator.Connected += async (_, _) => await SendAndAddEvent("ROTATOR-CONNECTED");
            AdvancedAPI.Controls.Rotator.Disconnected += async (_, _) => await SendAndAddEvent("ROTATOR-DISCONNECTED");

            AdvancedAPI.Controls.Dome.Connected += async (_, _) => await SendAndAddEvent("DOME-CONNECTED");
            AdvancedAPI.Controls.Dome.Disconnected += async (_, _) => await SendAndAddEvent("DOME-DISCONNECTED");
            AdvancedAPI.Controls.Dome.Closed += async (_, _) => await SendAndAddEvent("DOME-SHUTTER-CLOSED");
            AdvancedAPI.Controls.Dome.Opened += async (_, _) => await SendAndAddEvent("DOME-SHUTTER-OPENED");
            AdvancedAPI.Controls.Dome.Homed += async (_, _) => await SendAndAddEvent("DOME-HOMED");
            AdvancedAPI.Controls.Dome.Parked += async (_, _) => await SendAndAddEvent("DOME-PARKED");

            AdvancedAPI.Controls.FilterWheel.Connected += async (_, _) => await SendAndAddEvent("FILTERWHEEL-CONNECTED");
            AdvancedAPI.Controls.FilterWheel.Disconnected += async (_, _) => await SendAndAddEvent("FILTERWHEEL-DISCONNECTED");
            AdvancedAPI.Controls.FilterWheel.FilterChanged += async (_, _) => await SendAndAddEvent("FILTERWHEEL-CHANGED");

            AdvancedAPI.Controls.Switch.Connected += async (_, _) => await SendAndAddEvent("SWITCH-CONNECTED");
            AdvancedAPI.Controls.Switch.Disconnected += async (_, _) => await SendAndAddEvent("SWITCH-DISCONNECTED");

            AdvancedAPI.Controls.SafetyMonitor.Connected += async (_, _) => await SendAndAddEvent("SAFETY-CONNECTED");
            AdvancedAPI.Controls.SafetyMonitor.Disconnected += async (_, _) => await SendAndAddEvent("SAFETY-DISCONNECTED");
            AdvancedAPI.Controls.SafetyMonitor.IsSafeChanged += async (_, _) => await SendAndAddEvent("SAFETY-CHANGED");

            AdvancedAPI.Controls.Guider.Connected += async (_, _) => await SendAndAddEvent("GUIDER-CONNECTED");
            AdvancedAPI.Controls.Guider.Disconnected += async (_, _) => await SendAndAddEvent("GUIDER-DISCONNECTED");
            AdvancedAPI.Controls.Guider.AfterDither += async (_, _) => await SendAndAddEvent("GUIDER-DITHER");
            AdvancedAPI.Controls.Guider.GuidingStarted += async (_, _) => await SendAndAddEvent("GUIDER-START");
            AdvancedAPI.Controls.Guider.GuidingStopped += async (_, _) => await SendAndAddEvent("GUIDER-STOP");

            AdvancedAPI.Controls.FlatDevice.Connected += async (_, _) => await SendAndAddEvent("FLAT-CONNECTED");
            AdvancedAPI.Controls.FlatDevice.Disconnected += async (_, _) => await SendAndAddEvent("FLAT-DISCONNECTED");

            AdvancedAPI.Controls.Weather.Connected += async (_, _) => await SendAndAddEvent("WEATHER-CONNECTED");
            AdvancedAPI.Controls.Weather.Disconnected += async (_, _) => await SendAndAddEvent("WEATHER-DISCONNECTED");

            AdvancedAPI.Controls.ImageSaveMediator.ImageSaved += ImageSaved;

            AdvancedAPI.Server.LogProcessor.NINALogEventSaved += LogProcessor_NINALogEventSaved;
        }


        public static List<HttpResponse> Images = new List<HttpResponse>();
        public static List<HttpResponse> Events = new List<HttpResponse>();

        private void ImageSaved(object sender, ImageSavedEventArgs e)
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

            Send(response);
        }

        private async void LogProcessor_NINALogEventSaved(object sender, NINALogEvent e) => await SendAndAddEvent(e.type, e.time);


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
