#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using ninaAPI.Properties;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Interfaces;
using SimpleW;
using SimpleW.Modules;

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

    public class TPPASocket : IWebSocket, ISubscriber
    {
        private readonly ThreadSafeList<WebSocketConnection> clients = new();

        public TPPASocket()
        {
            AdvancedAPI.Controls.MessageBroker.Subscribe("PolarAlignmentPlugin_PolarAlignment_AlignmentError", this);
            AdvancedAPI.Controls.MessageBroker.Subscribe("PolarAlignmentPlugin_PolarAlignment_Progress", this);
        }

        private async Task OnMessageReceivedAsync(WebSocketConnection connection, WebSocketContext context, string text)
        {
            string topic;
            object content = null;
            string response;

            try
            {
                TPPARequest r = JsonConvert.DeserializeObject<TPPARequest>(text, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Include }); // TODO: Document this
                topic = r.Action;
                content = r;
            }
            catch
            {
                topic = text;
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
            await Send(new CustomResponse()
            {
                Type = CustomResponse.TypeSocket,
                Response = response
            });
        }

        private async ValueTask OnClientConnectedAsync(WebSocketConnection connection, WebSocketContext context)
        {
            if (Settings.Default.UseAuth)
            {
                if (context.Session.Principal == HttpPrincipal.Anonymous)
                {
                    Logger.Warning($"Unauthorized WebSocket connection attempt from {connection.RemoteEndPoint}");
                    await connection.CloseAsync(1008, "Unauthorized");
                    return;
                }
            }
            Logger.Info("TPPA WebSocket connected " + connection.RemoteEndPoint.ToString());
            clients.Add(connection);
        }

        private async ValueTask OnClientDisconnectedAsync(WebSocketConnection connection, WebSocketContext context)
        {
            Logger.Info("TPPA WebSocket disconnected " + connection.RemoteEndPoint.ToString());
            clients.Remove(connection);
        }

        public async Task Send(CustomResponse payload)
        {
            Logger.Trace("Sending " + payload.Response + " to TPPA WebSocket");
            foreach (WebSocketConnection client in clients.ToList())
            {
                await client.SendTextAsync(JsonConvert.SerializeObject(payload));
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

                    await Send(new CustomResponse()
                    {
                        Type = CustomResponse.TypeSocket,
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

                    await Send(new CustomResponse()
                    {
                        Type = CustomResponse.TypeSocket,
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

        public void ConfigureWebSocket(WebSocketOptions options)
        {
            options.OnUnknown(async (conn, ctx, msg) =>
            {
                await OnMessageReceivedAsync(conn, ctx, msg.RawText);
            });

            options.OnConnect = OnClientConnectedAsync;
            options.OnDisconnect = OnClientDisconnectedAsync;
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
