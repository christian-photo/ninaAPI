#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Properties;
using ninaAPI.Utility;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using SimpleW;
using SimpleW.Modules;

namespace ninaAPI.WebService.V3.Websocket.MountControl
{
    public class MountControlSocket : IWebSocket
    {
        private readonly RetriggerableAction primaryTimer;
        private double eastRate;
        private double westRate;
        private readonly RetriggerableAction secondaryTimer;
        private double northRate;
        private double southRate;

        private static readonly Lock _timerLock = new Lock();

        private readonly ITelescopeMediator mount;
        private readonly ISerializerService serializer;

        public MountControlSocket(ITelescopeMediator mount, ISerializerService serializer)
        {
            primaryTimer = new RetriggerableAction(new Action(() => mount.MoveAxis(TelescopeAxes.Primary, 0)), TimeSpan.FromMilliseconds(2000));
            secondaryTimer = new RetriggerableAction(new Action(() => mount.MoveAxis(TelescopeAxes.Secondary, 0)), TimeSpan.FromMilliseconds(2000));
            this.mount = mount;
            this.serializer = serializer;
        }

        private async Task OnMessageReceivedAsync(WebSocketConnection connection, WebSocketContext context, string text)
        {
            string status = "OK";

            if (!mount.GetInfo().Connected)
            {
                await connection.SendTextAsync(serializer.Serialize(new { Status = "Mount not connected" }));
                return;
            }
            if (mount.GetInfo().AtPark)
            {
                await connection.SendTextAsync(serializer.Serialize(new { Status = "Mount parked" }));
                return;
            }
            if (mount.GetInfo().Slewing)
            {
                await connection.SendTextAsync(serializer.Serialize(new { Status = "Mount slewing" }));
                return;
            }

            try
            {
                var request = serializer.Deserialize<MountControlRequest>(text);
                switch (request.Direction)
                {
                    case DirectionEnum.East:
                        if (eastRate != request.Rate)
                        {
                            mount.MoveAxis(TelescopeAxes.Primary, request.Rate);
                        }
                        lock (_timerLock)
                        {
                            primaryTimer.Trigger();
                            eastRate = request.Rate;
                        }
                        break;
                    case DirectionEnum.West:
                        if (westRate != request.Rate)
                        {
                            mount.MoveAxis(TelescopeAxes.Primary, -request.Rate);
                        }
                        lock (_timerLock)
                        {
                            primaryTimer.Trigger();
                            westRate = request.Rate;
                        }
                        break;
                    case DirectionEnum.North:
                        if (northRate != request.Rate)
                        {
                            mount.MoveAxis(TelescopeAxes.Secondary, request.Rate);
                        }
                        lock (_timerLock)
                        {
                            secondaryTimer.Trigger();
                            northRate = request.Rate;
                        }
                        break;
                    case DirectionEnum.South:
                        if (southRate != request.Rate)
                        {
                            mount.MoveAxis(TelescopeAxes.Secondary, -request.Rate);
                        }
                        lock (_timerLock)
                        {
                            secondaryTimer.Trigger();
                            southRate = request.Rate;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                status = "Error while processing request";
                Logger.Error(ex);
            }

            await connection.SendTextAsync(serializer.Serialize(new { Status = status }));
        }

        public void ConfigureWebSocket(WebSocketOptions options)
        {
            options.OnUnknown(async (conn, ctx, msg) =>
            {
                await OnMessageReceivedAsync(conn, ctx, msg.RawText);
            });

            options.OnConnect = async (conn, ctx) =>
            {
                if (Settings.Default.UseAuth)
                {
                    if (ctx.Session.Principal == HttpPrincipal.Anonymous)
                    {
                        Logger.Warning($"Unauthorized WebSocket connection attempt from {conn.RemoteEndPoint}");
                        await conn.CloseAsync(1008, "Unauthorized");
                        return;
                    }
                }
            };
        }
    }

    public class MountControlRequest
    {
        public DirectionEnum Direction { get; set; }
        public double Rate { get; set; }
    }

    public enum DirectionEnum
    {
        East,
        West,
        North,
        South
    }
}


