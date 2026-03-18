#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NINA.Core.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using SimpleW.Modules;

namespace ninaAPI.WebService.V3.Websocket.Event
{
    public class EventWebSocket : IEventSocket
    {
        public EventHistoryManager EventHistoryManager { get; }
        public bool HasConnections => !Clients.IsEmpty;

        private readonly ConcurrentDictionary<Guid, WebSocketClient> Clients = new();
        private readonly ISerializerService serializer;

        public EventWebSocket(ISerializerService serializer, EventHistoryManager eventHistory)
        {
            this.serializer = serializer;
            this.EventHistoryManager = eventHistory;
        }

        public async Task SendEvent(WebSocketEvent e)
        {
            foreach (var client in Clients.Values)
            {
                if (client.Config.SubscriptionManager.IsSubscribed(e.Channel))
                {
                    await client.Connection.SendTextAsync(serializer.Serialize(e));
                }
                else
                {
                    Logger.Trace($"Client {client.Connection.RemoteEndPoint} not subscribed to channel {e.Channel}, skipping...");
                }
            }
        }

        private async Task OnMessageReceivedAsync(WebSocketConnection connection, WebSocketContext context, string text)
        {
            ClientMessage message = null;
            Guid clientId = connection.Id;

            try
            {
                Logger.Debug($"Client {connection.RemoteEndPoint} sent message: {text}");
                message = serializer.Deserialize<ClientMessage>(text);

                if (message.Sender == "Subscribe") // TODO: Support lists of channels
                {
                    Clients[clientId].Config.SubscriptionManager.Subscribe(Enum.Parse<WebSocketChannel>(message.Data.ToString()));
                    await connection.SendTextAsync(serializer.Serialize(ClientMessage.Reply(message, "Subscribed")));
                }
                else if (message.Sender == "Unsubscribe")
                {
                    Clients[clientId].Config.SubscriptionManager.Unsubscribe(Enum.Parse<WebSocketChannel>(message.Data.ToString()));
                    await connection.SendTextAsync(serializer.Serialize(ClientMessage.Reply(message, "Unsubscribed")));
                }
                else if (message.Sender == "AvailableChannels")
                {
                    await connection.SendTextAsync(serializer.Serialize(ClientMessage.Reply(message, Enum.GetValues<WebSocketChannel>())));
                }
                else if (message.Sender == "SubscribedChannels")
                {
                    await connection.SendTextAsync(serializer.Serialize(ClientMessage.Reply(message, Clients[clientId].Config.SubscriptionManager.GetSubscribedChannels())));
                }
                else
                {
                    Logger.Warning($"Message from {connection.RemoteEndPoint} was invalid");
                    await connection.SendTextAsync(serializer.Serialize(ClientMessage.Reply(message, "Invalid message")));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await connection.SendTextAsync(serializer.Serialize(ClientMessage.Reply(message ?? new ClientMessage(), new { Error = "Error encountered while reading message", Message = ex.Message })));
            }
        }

        private async ValueTask OnClientConnectedAsync(WebSocketConnection connection, WebSocketContext context)
        {
            Logger.Info($"Client {connection.RemoteEndPoint} connected");
            Clients.TryAdd(connection.Id, new WebSocketClient(connection, context));
        }

        private async ValueTask OnClientDisconnectedAsync(WebSocketConnection connection, WebSocketContext context)
        {
            Logger.Info($"Client {connection.RemoteEndPoint} disconnected");
            Clients.TryRemove(connection.Id, out _);
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

    public class WebSocketClient(WebSocketConnection connection, WebSocketContext context)
    {
        public WebSocketConnection Connection { get; set; } = connection;
        public WebSocketContext Context { get; set; } = context;
        public ClientConfiguration Config { get; set; } = new();
    }

    public class ClientMessage
    {
        /// <summary>
        /// Sender should not be sent by the client, that is only for the 
        /// </summary>
        public string Sender { get; set; }
        public string RequestId { get; set; }
        public object Data { get; set; }

        public static ClientMessage Reply(ClientMessage request, object data)
        {
            return new ClientMessage()
            {
                Sender = "Server",
                RequestId = request.RequestId,
                Data = data
            };
        }
    }
}
