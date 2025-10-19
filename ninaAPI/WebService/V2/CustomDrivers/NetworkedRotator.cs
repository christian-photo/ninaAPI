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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;

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
            Logger.Debug($"Moving asdfsadfasdf to {Position + position}");
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

            if (ct.IsCancellationRequested)
            {
                _ = WindowService.Close();
                ct.ThrowIfCancellationRequested();
            }
            Position = AstroUtil.EuclidianModulus(TargetPosition, 360);

            MoveFinished?.Invoke(this, null);

            IsMoving = false;
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

    public class NetworkedRotatorSocket : WebSocketModule
    {
        public NetworkedRotatorSocket(string urlPath) : base(urlPath, true)
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

        private async void NetworkedRotator_MoveRequested(object sender, object _)
        {
            rotator = sender as NetworkedRotator;
            foreach (IWebSocketContext context in ActiveContexts)
            {
                await SendAsync(context, makeRotationResponse());
            }
        }

        private async void NetworkedRotator_RotationCompleted(object sender, object _)
        {
            rotator = null;
            foreach (IWebSocketContext context in ActiveContexts)
            {
                await SendAsync(context, makeRotationCompletedResponse());
            }
        }

        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
        {
            string message = Encoding.GetString(buffer);
            if (message.Equals("get-target-position"))
            {
                await SendAsync(context, rotator is null ? makeRotationRequestedResponse() : makeRotationResponse());
            }
            else if (message.Equals("rotation-completed"))
            {
                if (rotator is null)
                {
                    await SendAsync(context, makeRotationCompletedResponse());
                }
                else
                {
                    rotator?.WindowTaskSource?.SetCanceled();
                    rotator?.WindowService?.Close();
                    rotator = null;
                }
            }
        }
    }
}