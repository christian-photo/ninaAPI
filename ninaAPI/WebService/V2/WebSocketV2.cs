#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/event-history")]
        public void GetEventHistory()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                List<object> result = new List<object>();
                foreach (HttpResponse r in WebSocketV2.Events)
                {
                    result.Add(r.Response);
                }
                response.Response = result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }

    public class WebSocketV2 : WebSocketModule
    {
        private static WebSocketV2 instance;
        public WebSocketV2(string urlPath) : base(urlPath, true)
        {
            instance = this;

            AdvancedAPI.Controls.ImageSaveMediator.ImageSaved += ImageSaved;

            AdvancedAPI.Server.LogProcessor.NINALogEventSaved += LogProcessor_NINALogEventSaved;
        }

        public static async Task SendAndAddEvent(string eventName)
        {
            await SendAndAddEvent(eventName, DateTime.Now);
        }

        public static async Task SendAndAddEvent(string eventName, DateTime time)
        {
            HttpResponse response = new HttpResponse();
            response.Type = HttpResponse.TypeSocket;

            response.Response = new Hashtable()
            {
                { "Event", eventName },
            };
            HttpResponse Event = new HttpResponse() { Type = HttpResponse.TypeSocket, Response = new Dictionary<string, object>() { { "Event", eventName }, { "Time", time } } };
            Events.Add(Event);

            await instance?.Send(response);
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

        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            Encoding.GetString(rxBuffer); // Do something with it
            return Task.CompletedTask;
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Logger.Info("WebSocket connected " + context.RemoteEndPoint.ToString());
            return Task.CompletedTask;
        }

        public async Task Send(HttpResponse payload)
        {
            foreach (IWebSocketContext context in ActiveContexts)
            {
                Logger.Trace("Sending to " + context.RemoteEndPoint.ToString());
                await SendAsync(context, JsonConvert.SerializeObject(payload));
            }
        }
    }
}
