#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using NINA.Core.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;

namespace ninaAPI.WebService.V3.Websocket.Event
{
    public class EventWebSocket : WebSocketModule, IEventSocket
    {
        public EventHistoryManager EventHistoryManager { get; }
        public bool IsActive => ActiveContexts.Count > 0;

        private readonly ConcurrentDictionary<IWebSocketContext, ClientConfiguration> Clients = new();
        private readonly ISerializerService serializer;

        public EventWebSocket(string url, ISerializerService serializer, EventHistoryManager eventHistory) : base(url, true)
        {
            this.serializer = serializer;
            this.EventHistoryManager = eventHistory;
        }

        public async Task SendEvent(WebSocketEvent e)
        {
            foreach (var (client, clientConfig) in Clients)
            {
                if (client.WebSocket.State != WebSocketState.Open && client.WebSocket.State != WebSocketState.Connecting)
                {
                    Logger.Warning($"Client {client.RemoteEndPoint} not connected, removing...");
                    Clients.TryRemove(client, out _);
                    continue;
                }
                if (clientConfig.SubscriptionManager.IsSubscribed(e.Channel))
                {
                    await SendAsync(client, serializer.Serialize(e));
                }
                else
                {
                    Logger.Trace($"Client {client.RemoteEndPoint} not subscribed to channel {e.Channel}, skipping...");
                }
            }
        }

        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
        {
            ClientMessage message = null;
            try
            {
                string json = Encoding.GetString(buffer);
                Logger.Debug($"Client {context.RemoteEndPoint} sent message: {json}");
                message = serializer.Deserialize<ClientMessage>(json);

                if (message.Type == "Subscribe")
                {
                    Clients[context].SubscriptionManager.Subscribe(Enum.Parse<WebSocketChannel>(message.Data.ToString()));
                    await SendAsync(context, serializer.Serialize(ClientMessage.Reply(message, "Subscribed")));
                }
                else if (message.Type == "Unsubscribe")
                {
                    Clients[context].SubscriptionManager.Unsubscribe(Enum.Parse<WebSocketChannel>(message.Data.ToString()));
                    await SendAsync(context, serializer.Serialize(ClientMessage.Reply(message, "Unsubscribed")));
                }
                else if (message.Type == "AvailableChannels")
                {
                    await SendAsync(context, serializer.Serialize(ClientMessage.Reply(message, Enum.GetValues<WebSocketChannel>())));
                }
                else if (message.Type == "SubscribedChannels")
                {
                    await SendAsync(context, serializer.Serialize(ClientMessage.Reply(message, Clients[context].SubscriptionManager.GetSubscribedChannels())));
                }
                else
                {
                    Logger.Debug($"Message from {context.RemoteEndPoint} was invalid");
                    await SendAsync(context, serializer.Serialize(ClientMessage.Reply(message, "Invalid message")));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await SendAsync(context, serializer.Serialize(ClientMessage.Reply(message ?? new ClientMessage(), new { Error = "Error encountered while reading message", Message = ex.Message })));
            }
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Logger.Info($"Client {context.RemoteEndPoint} connected");
            Clients.TryAdd(context, new ClientConfiguration());
            return base.OnClientConnectedAsync(context);
        }

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            Logger.Info($"Client {context.RemoteEndPoint} disconnected");
            Clients.TryRemove(context, out _);
            return base.OnClientDisconnectedAsync(context);
        }
    }

    public class ClientMessage
    {
        public string Type { get; set; }
        public string RequestId { get; set; }
        public object Data { get; set; }

        public static ClientMessage Reply(ClientMessage request, object data)
        {
            return new ClientMessage()
            {
                Type = "Server",
                RequestId = request.RequestId,
                Data = data
            };
        }
    }
}
