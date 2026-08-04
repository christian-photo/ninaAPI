#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Utility;
using ninaAPI.Properties;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Interfaces;
using SimpleW;
using SimpleW.Modules;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        [Route("GET", "/event-history")]
        public void GetEventHistory()
        {
            CustomResponse response = new CustomResponse();

            try
            {
                List<object> result = new List<object>();
                foreach (CustomResponse r in WebSocketV2.Events)
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

            Response.WriteToResponse(response);
        }
    }

    public class WebSocketV2 : IWebSocket
    {
        // private static bool sendConsumerEvents = false;

        private static WebSocketV2 instance;
        public WebSocketV2()
        {
            instance = this;
        }

        public static async Task SendConsumerEvent(string consumer)
        {
            // Maybe not, because it would be a lot of unnecessary traffic since most devices update their info constantly and then this would be useless
            // return;
            // consumer = consumer.ToUpper();
            // Logger.Info($"Sending {consumer}-INFO-UPDATED");
            // if (sendConsumerEvents && consumer != "MOUNT")
            // {
            //     // Do not allow mount updates for now, because that would be a lot of unnecessary traffic
            //     // because every time the coordinates change, the info also changes. We could enable this with an extra bool
            //     // in the future
            //     await SendEvent(new CustomResponse() { Response = $"{consumer}-INFO-UPDATED", Type = CustomResponse.TypeSocket });
            // }
        }

        public static async Task SendAndAddEvent(string eventName, Dictionary<string, object> data)
        {
            await SendAndAddEvent(eventName, DateTime.Now, data);
        }

        public static async Task SendAndAddEvent(string eventName)
        {
            await SendAndAddEvent(eventName, DateTime.Now, null);
        }

        public static async Task SendAndAddEvent(string eventName, DateTime time)
        {
            await SendAndAddEvent(eventName, time, null);
        }

        public static async Task SendAndAddEvent(string eventName, DateTime time, Dictionary<string, object> data)
        {
            CustomResponse response = new CustomResponse();
            response.Type = CustomResponse.TypeSocket;

            Hashtable responseData = new Hashtable
            {
                { "Event", eventName }
            };
            if (data != null)
            {
                foreach (KeyValuePair<string, object> kvp in data)
                {
                    responseData.Add(kvp.Key, kvp.Value);
                }
            }

            response.Response = responseData;

            // Deep clone using JSON serialization (BinaryFormatter is disabled in .NET 8)
            string json = JsonConvert.SerializeObject(responseData);
            Hashtable eventTable = JsonConvert.DeserializeObject<Hashtable>(json);
            eventTable.Add("Time", time);
            CustomResponse Event = new CustomResponse() { Type = CustomResponse.TypeSocket, Response = eventTable };
            Events.Add(Event);

            await SendEvent(response);
        }

        public static bool IsAvailable => instance is not null;

        public static void SetUnavailable() => instance = null;

        public static List<CustomResponse> Events = new List<CustomResponse>();

        private static ThreadSafeList<WebSocketConnection> clients = new();

        private void OnMessageReceivedAsync(WebSocketConnection connection, WebSocketContext context, string text)
        {
            // if (text.Equals("enable-consumer-events"))
            // {
            //     sendConsumerEvents = true;
            // }
            // else if (text.Equals("disable-consumer-events"))
            // {
            //     sendConsumerEvents = false;
            // }
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
            Logger.Info($"WebSocket connected {connection.RemoteEndPoint}");
            clients.Add(connection);
        }

        private async ValueTask OnClientDisconnectedAsync(WebSocketConnection connection, WebSocketContext context)
        {
            Logger.Info($"WebSocket disconnected {connection.RemoteEndPoint}");
            clients.Remove(connection);
        }

        public static async Task<bool> SendEvent(CustomResponse payload)
        {
            try
            {
                if (instance is not null)
                {
                    await instance?.Send(payload);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"WebSocket SendEvent failed: {ex.Message}");
            }
            return false;
        }

        public async Task Send(CustomResponse payload)
        {
            foreach (var client in clients.ToList())
            {
                Logger.Trace("Sending to " + client.RemoteEndPoint.ToString());
                await client.SendTextAsync(JsonConvert.SerializeObject(payload));
            }
        }

        public void ConfigureWebSocket(WebSocketOptions options)
        {
            options.OnUnknown(async (conn, ctx, msg) =>
            {
                OnMessageReceivedAsync(conn, ctx, msg.RawText);
            });

            options.OnConnect = OnClientConnectedAsync;
            options.OnDisconnect = OnClientDisconnectedAsync;
        }
    }
}
