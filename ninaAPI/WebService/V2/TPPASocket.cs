#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebSockets;
using Grpc.Core;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using ninaAPI.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class TPPARequest
    {
        public string Action { get; set; }
        public bool? ManualMode { get; set; }
        public int? TargetDistance { get; set; }
        public int? MoveRate { get; set; }
        public bool? EastDirection { get; set; }
        public bool? StartFromCurrentPosition { get; set; }
        public int? AltDegrees { get; set; }
        public int? AltMinutes { get; set; }
        public double? AltSeconds { get; set; }
        public int? AzDegrees { get; set; }
        public int? AzMinutes { get; set; }
        public double? AzSeconds { get; set; }
        public double? AlignmentTolerance { get; set; }
        public string? Filter { get; set; }
        public double? ExposureTime { get; set; }
        public short? Binning { get; set; }
        public int? Gain { get; set; }
        public int? Offset { get; set; }
        public double? SearchRadius { get; set; }
    }

    public class TPPASocket : WebSocketModule, ISubscriber
    {
        public TPPASocket(string urlPath) : base(urlPath, true)
        {
            AdvancedAPI.Controls.MessageBroker.Subscribe("PolarAlignmentPlugin_PolarAlignment_AlignmentError", this);
            AdvancedAPI.Controls.MessageBroker.Subscribe("PolarAlignmentPlugin_PolarAlignment_Progress", this);
        }

        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            string message = Encoding.GetString(rxBuffer); // Do something with it
            string topic;
            object content = null;
            string response;

            try
            {
                TPPARequest r = JsonConvert.DeserializeObject<TPPARequest>(message, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Include }); // TODO: Document this
                topic = r.Action;
                content = r;
            }
            catch
            {
                topic = message;
            }


            if (topic.Equals("start-alignment"))
            {
                topic = "PolarAlignmentPlugin_DockablePolarAlignmentVM_StartAlignment";
                response = "started procedure";
            }
            else if (topic.Equals("stop-alignment"))
            {
                topic = "PolarAlignmentPlugin_DockablePolarAlignmentVM_StopAlignment";
                response = "stopped procedure";
            }
            else if (topic.Equals("pause-alignment"))
            {
                topic = "PolarAlignmentPlugin_PolarAlignment_PauseAlignment";
                response = "paused procedure";
            }
            else if (topic.Equals("resume-alignment"))
            {
                topic = "PolarAlignmentPlugin_PolarAlignment_ResumeAlignment";
                response = "resumed procedure";
            }
            else
            {
                return;
            }

            Guid correlatedGuid = Guid.NewGuid();
            await AdvancedAPI.Controls.MessageBroker.Publish(new TPPAMessage(correlatedGuid, topic, content));
            await Send(new HttpResponse()
            {
                Type = HttpResponse.TypeSocket,
                Response = response
            });
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Logger.Info("TPPA WebSocket connected " + context.RemoteEndPoint.ToString());
            return Task.CompletedTask;
        }

        public async Task Send(HttpResponse payload)
        {
            Logger.Trace("Sending " + payload.Response + " to TPPA WebSocket");
            foreach (IWebSocketContext context in ActiveContexts)
            {
                await SendAsync(context, JsonConvert.SerializeObject(payload));
            }
        }

        // From ISubscriber
        public async Task OnMessageReceived(IMessage message)
        {
            try
            {
                if (message.Topic == "PolarAlignmentPlugin_PolarAlignment_AlignmentError" && message.Version == 1)
                {
                    Type t = message.Content.GetType();

                    double AzimuthError = (double)t.GetProperty("AzimuthError").GetValue(message.Content, null);
                    double AltitudeError = (double)t.GetProperty("AltitudeError").GetValue(message.Content, null);
                    double TotalError = (double)t.GetProperty("TotalError").GetValue(message.Content, null);

                    await Send(new HttpResponse()
                    {
                        Type = HttpResponse.TypeSocket,
                        Response = new Dictionary<string, double>
                    {
                        { "AzimuthError", AzimuthError },
                        { "AltitudeError", AltitudeError },
                        { "TotalError", TotalError },
                    }
                    });
                }
                else if (message.Topic == "PolarAlignmentPlugin_PolarAlignment_Progress")
                {
                    ApplicationStatus status = (ApplicationStatus)message.Content;

                    await Send(new HttpResponse()
                    {
                        Type = HttpResponse.TypeSocket,
                        Response = new
                        {
                            Status = status.Status,
                            Progress = status.Progress / status.MaxProgress,
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while processing TPPA message");
            }
        }
    }

    public class TPPAMessage(Guid correlatedGuid, string topic, object content) : IMessage
    {
        public Guid SenderId => Guid.Parse(AdvancedAPI.PluginId);

        public string Sender => nameof(ninaAPI);

        public DateTimeOffset SentAt => DateTime.UtcNow;

        public Guid MessageId => Guid.NewGuid();

        public DateTimeOffset? Expiration => null;

        public Guid? CorrelationId => correlatedGuid;

        public int Version => 1;

        public IDictionary<string, object> CustomHeaders => new Dictionary<string, object>();

        public string Topic => topic;

        public object Content => content;
    }
}
