#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;

namespace ninaAPI.WebService.V3.Websocket.TPPA
{
    public class TppaSocket : WebSocketModule, ISubscriber
    {
        private readonly IMessageBroker messageBroker;

        public TppaSocket(string url, IMessageBroker messageBroker) : base(url, true)
        {
            this.messageBroker = messageBroker;
        }

        // MessageBroker message
        public async Task OnMessageReceived(IMessage message)
        {
            if (message.Topic == "PolarAlignmentPlugin_PolarAlignment_AlignmentError" && message.Version == 1)
            {
                await Send(TppaResponse.Event("alignment-error", message.Content));
            }
            else if (message.Topic == "PolarAlignmentPlugin_PolarAlignment_Progress")
            {
                ApplicationStatus status = (ApplicationStatus)message.Content;

                await Send(TppaResponse.Event("progress-update", new
                {
                    Status = status.Status,
                    Progress = status.Progress / status.MaxProgress,
                }
                ));
            }
        }

        // Websocket message
        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
        {
            string message = Encoding.GetString(buffer);

            try
            {
                TppaConfig config = JsonConvert.DeserializeObject<TppaConfig>(message);
                config.Validate(); // Automatically throws if the config is invalid
                await messageBroker.Publish(new TppaMessage(Guid.NewGuid(), config.Action, config));
                await Send(TppaResponse.Success("Invoked action"));
            }
            catch (JsonException ex)
            {
                Logger.Error(ex);
                await Send(TppaResponse.Error($"Error while parsing message: {ex.Message}"));
            }
            catch (ArgumentException ex)
            {
                Logger.Error(ex);
                await Send(TppaResponse.Error($"Error while processing TPPA message: {ex.Message}"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await Send(TppaResponse.Error($"Error while processing TPPA message: {ex.Message}"));
            }
        }

        public async Task Send(object payload)
        {
            string json = JsonConvert.SerializeObject(payload);
            Logger.Trace("Sending " + json + " to TPPA WebSocket");
            foreach (IWebSocketContext context in ActiveContexts)
            {
                await SendAsync(context, json);
            }
        }
    }

    public class TppaResponse
    {
        public string Sender { get; } = "Server";
        public bool? Successful { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

        public static TppaResponse Success(string message)
        {
            return new TppaResponse()
            {
                Successful = true,
                Message = message,
            };
        }

        public static TppaResponse Error(string message)
        {
            return new TppaResponse()
            {
                Successful = false,
                Message = message
            };
        }

        public static TppaResponse Event(string eventName, object data)
        {
            return new TppaResponse()
            {
                Successful = null,
                Message = eventName,
                Data = data
            };
        }
    }
}