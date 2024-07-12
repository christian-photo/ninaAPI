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
            AdvancedAPI.Controls.Camera.GetInfo().PropertyChanged += CameraChanged;
            AdvancedAPI.Controls.Telescope.GetInfo().PropertyChanged += TelescopeChanged;
            AdvancedAPI.Controls.Focuser.GetInfo().PropertyChanged += FocuserChanged;
            AdvancedAPI.Controls.Rotator.GetInfo().PropertyChanged += RotatorChanged;
            AdvancedAPI.Controls.Dome.GetInfo().PropertyChanged += DomeChanged;
            AdvancedAPI.Controls.FilterWheel.GetInfo().PropertyChanged += FWChanged;
            AdvancedAPI.Controls.Switch.GetInfo().PropertyChanged += SwitchChanged;
            AdvancedAPI.Controls.SafetyMonitor.GetInfo().PropertyChanged += SafetyChanged;
            AdvancedAPI.Controls.Guider.GetInfo().PropertyChanged += GuiderChanged;
            AdvancedAPI.Controls.FlatDevice.GetInfo().PropertyChanged += FlatChanged;
            AdvancedAPI.Controls.ImageSaveMediator.ImageSaved += ImageSaved;

            AdvancedAPI.Server.LogProcessor.NINALogEventSaved += LogProcessor_NINALogEventSaved;
        }

        public static List<HttpResponse> Images = new List<HttpResponse>();

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

        private async void LogProcessor_NINALogEventSaved(object sender, NINALogEvent e) => await Send(new HttpResponse() { Response = e.type, Type = HttpResponse.TypeSocket });

        private async void CameraChanged(object sender, PropertyChangedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.Camera.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "CAMERA-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }

        private int telescopeCounter = 0;

        private async void TelescopeChanged(object sender, PropertyChangedEventArgs e)
        {
            telescopeCounter++;
            if (telescopeCounter % 2 == 0) // less newtork traffic because of constant coordinate updating
            {
                return;
            }
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.Telescope.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "TELESCOPE-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }
        private async void FocuserChanged(object sender, PropertyChangedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.Focuser.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "FOCUSER-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }
        private async void RotatorChanged(object sender, PropertyChangedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.Rotator.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "ROTATOR-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }
        private async void DomeChanged(object sender, PropertyChangedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.Dome.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "DOME-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }
        private async void FWChanged(object sender, PropertyChangedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.FilterWheel.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "FILTERWHEEL-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }
        private async void SwitchChanged(object sender, PropertyChangedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.Switch.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "SWITCH-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }
        private async void SafetyChanged(object sender, PropertyChangedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.SafetyMonitor.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "SAFETY-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }
        private async void GuiderChanged(object sender, PropertyChangedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.Guider.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "GUIDER-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }
        private async void FlatChanged(object sender, PropertyChangedEventArgs e)
        {
            HttpResponse response = new HttpResponse() { Type = HttpResponse.TypeSocket };
            object value = AdvancedAPI.Controls.FlatDevice.GetInfo().TryGetPropertyValue<object>(e.PropertyName);

            response.Response = new Dictionary<string, object>()
            {
                { "Event", "FLAT-CHANGED" },
                { "PropertyName", e.PropertyName },
                { "Value", value }
            };

            await Send(response);
        }

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
            Logger.Debug("Sending " + payload.Response + " to WebSocket");
            foreach (IWebSocketContext context in ActiveContexts)
            {
                Logger.Debug("Sending to " + context.RemoteEndPoint.ToString());
                await SendAsync(context, JsonConvert.SerializeObject(payload));
            }
        }
    }
}
