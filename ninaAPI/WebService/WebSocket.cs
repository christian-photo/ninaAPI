#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebSockets;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
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

        private async void ImageSaved(object sender, ImageSavedEventArgs e) => await Send(new HttpResponse() { Response = "IMAGE-NEW", Type = HttpResponse.TypeSocket });

        private async void LogProcessor_NINALogEventSaved(object sender, NINALogEvent e) => await Send(new HttpResponse() { Response = e.type, Type = HttpResponse.TypeSocket });

        private async void CameraChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "CAMERA-CHANGED", Type = HttpResponse.TypeSocket });
        private async void TelescopeChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "TELESCOPE-CHANGED", Type = HttpResponse.TypeSocket });
        private async void FocuserChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "FOCUSER-CHANGED", Type = HttpResponse.TypeSocket });
        private async void RotatorChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "ROTATOR-CHANGED", Type = HttpResponse.TypeSocket });
        private async void DomeChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "DOME-CHANGED", Type = HttpResponse.TypeSocket });
        private async void FWChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "FILTERHWEEL-CHANGED", Type = HttpResponse.TypeSocket });
        private async void SwitchChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "SWITCH-CHANGED", Type = HttpResponse.TypeSocket });
        private async void SafetyChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "SAFETY-CHANGED", Type = HttpResponse.TypeSocket });
        private async void GuiderChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "GUIDER-CHANGED", Type = HttpResponse.TypeSocket });
        private async void FlatChanged(object sender, PropertyChangedEventArgs e) => await Send(new HttpResponse() { Response = "FLAT-CHANGED", Type = HttpResponse.TypeSocket });

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
