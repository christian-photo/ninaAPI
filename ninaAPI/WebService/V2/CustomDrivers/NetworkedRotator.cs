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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using ninaAPI.Properties;
using ninaAPI.Utility;
using ninaAPI.WebService.Interfaces;
using SimpleW;
using SimpleW.Modules;

namespace ninaAPI.WebService.V2.CustomDrivers
{
    [Export(typeof(IEquipmentProvider))]
    public class NetworkedRotatorProvider : IEquipmentProvider<IRotator>
    {
        public string Name => "Networked Rotator";

        public IList<IRotator> GetEquipment()
        {
            return [new NetworkedRotator()];
        }
    }


    public class NetworkedRotator : BaseINPC, IRotator
    {
        public bool CanReverse => true;
        private bool reverse;

        public bool Reverse
        {
            get => reverse;
            set
            {
                reverse = value;
                RaisePropertyChanged();
            }
        }

        private bool synced;

        public bool Synced
        {
            get => synced;
            private set
            {
                synced = value;
                RaisePropertyChanged();
            }
        }

        public string Id => "Networked Rotator";

        public string Name => "Networked Rotator";

        public string DisplayName => "Networked Rotator";

        public string Category => "Advanced API";

        public string Description => "A networked manual rotator";

        public string DriverInfo => "n.A.";

        public string DriverVersion => "1.0";

        public bool IsMoving { get; set; }

        public bool Connected { get; set; }

        public float Position { get; set; }

        public float StepSize { get; set; }

        public float TargetPosition { get; set; }

        public bool HasSetupDialog => false;

        public async Task<bool> Move(float position, CancellationToken ct)
        {
            IsMoving = true;

            TargetPosition = Position + position;
            if (TargetPosition - Position > 180)
            {
                TargetPosition = TargetPosition - 360;
            }

            if (TargetPosition - Position < -180)
            {
                TargetPosition = TargetPosition + 360;
            }

            MoveRequested?.Invoke(this, null);

            // Reference: https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
            var window = WindowService.ShowDialog(this, Loc.Instance["LblRotationRequired"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
            WindowTaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(() => WindowTaskSource.SetCanceled()))
            {
                await Task.WhenAny(WindowTaskSource.Task, window.Task);
            }


            MoveFinished?.Invoke(this, null);

            IsMoving = false;

            if (ct.IsCancellationRequested)
            {
                _ = WindowService.Close();
                ct.ThrowIfCancellationRequested();
            }

            Position = AstroUtil.EuclidianModulus(TargetPosition, 360);
            return true;
        }

        public async Task<bool> MoveAbsolute(float position, CancellationToken ct)
        {
            return await Move(position - Position, ct);
        }

        public async Task<bool> MoveAbsoluteMechanical(float position, CancellationToken ct)
        {
            return await MoveAbsolute(position, ct);
        }

        public Task<bool> Connect(CancellationToken token)
        {
            Connected = true;
            return Task.FromResult(Connected);
        }

        public void Disconnect()
        {
            Connected = false;
        }

        public void Halt()
        {
        }

        private IWindowService windowService;

        public IWindowService WindowService
        {
            get
            {
                if (windowService == null)
                {
                    windowService = new WindowService();
                }
                return windowService;
            }
            set => windowService = value;
        }

        public float Rotation => Math.Abs(TargetPosition - Position);

        public float AbsTargetPosition
        {
            get
            {
                if (TargetPosition < 0) return TargetPosition + 360;
                return TargetPosition % 360;
            }
        }

        public string Direction
        {
            get
            {
                if ((TargetPosition - Position < 0 && !Reverse) || (TargetPosition - Position >= 0 && Reverse))
                {
                    return Loc.Instance["LblCounterclockwise"];
                }
                else
                {
                    return Loc.Instance["LblClockwise"];
                }
            }
        }

        public float MechanicalPosition => Position;

        public void Sync(float skyAngle)
        {
            Position = skyAngle;
            Synced = true;
        }

        public void SetupDialog()
        {

        }

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters)
        {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw)
        {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw)
        {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw)
        {
            throw new NotImplementedException();
        }

        public TaskCompletionSource<bool> WindowTaskSource { get; private set; }
        public static event EventHandler MoveRequested;
        public static event EventHandler MoveFinished;
    }

    public class NetworkedRotatorSocket : IWebSocket
    {
        public NetworkedRotatorSocket()
        {
            NetworkedRotator.MoveRequested += NetworkedRotator_MoveRequested;
            NetworkedRotator.MoveFinished += NetworkedRotator_RotationCompleted;
        }

        private string makeRotationResponse()
        {
            var obj = new
            {
                Position = rotator.Position,
                TargetPosition = rotator.TargetPosition,
                Rotation = rotator.Rotation,
            };
            return JsonConvert.SerializeObject(obj);
        }

        private string makeRotationCompletedResponse()
        {
            var obj = new
            {
                Message = "Rotation completed",
            };
            return JsonConvert.SerializeObject(obj);
        }

        private string makeRotationRequestedResponse()
        {
            var obj = new
            {
                Message = "N/A",
            };
            return JsonConvert.SerializeObject(obj);
        }

        private NetworkedRotator rotator;

        private readonly ThreadSafeList<WebSocketConnection> clients = new();

        private async void NetworkedRotator_MoveRequested(object sender, object _)
        {
            rotator = sender as NetworkedRotator;
            foreach (WebSocketConnection client in clients.ToList())
            {
                await client.SendTextAsync(makeRotationResponse());
            }
        }

        private async void NetworkedRotator_RotationCompleted(object sender, object _)
        {
            rotator = null;
            foreach (WebSocketConnection client in clients.ToList())
            {
                await client.SendTextAsync(makeRotationCompletedResponse());
            }
        }

        private async ValueTask OnMessageReceivedAsync(WebSocketConnection connection, WebSocketContext context, string text)
        {
            if (text.Equals("get-target-position"))
            {
                await connection.SendTextAsync(rotator is null ? makeRotationRequestedResponse() : makeRotationResponse());
            }
            else if (text.Equals("rotation-completed"))
            {
                if (rotator is null)
                {
                    await connection.SendTextAsync(makeRotationCompletedResponse());
                }
                else
                {
                    rotator?.WindowTaskSource?.SetCanceled();
                    rotator?.WindowService?.Close();
                    rotator = null;
                }
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

        private async ValueTask OnClientDisconnectedAsync(WebSocketConnection connection, WebSocketContext context)
        {
            Logger.Info("Networked Rotator WebSocket disconnected " + connection.RemoteEndPoint.ToString());
            clients.Remove(connection);
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
            Logger.Info("Networked Rotator WebSocket connected " + connection.RemoteEndPoint.ToString());
            clients.Add(connection);
        }
    }
}