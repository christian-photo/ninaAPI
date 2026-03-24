#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using SimpleW;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class RotatorWatcher : INinaWatcher, IRotatorConsumer
    {
        private readonly Func<object, EventArgs, Task> RotatorConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("ROTATOR-CONNECTED");
        private readonly Func<object, EventArgs, Task> RotatorDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("ROTATOR-DISCONNECTED");

        public void Dispose()
        {
            AdvancedAPI.Controls.Rotator.RemoveConsumer(this);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.Rotator.Connected += RotatorConnectedHandler;
            AdvancedAPI.Controls.Rotator.Disconnected += RotatorDisconnectedHandler;
            AdvancedAPI.Controls.Rotator.Moved += RotatorMovedHandler;
            AdvancedAPI.Controls.Rotator.MovedMechanical += RotatorMovedMechanicalHandler;
            AdvancedAPI.Controls.Rotator.Synced += RotatorSyncedHandler;
            AdvancedAPI.Controls.Rotator.RegisterConsumer(this);
        }

        private async void RotatorSyncedHandler(object sender, RotatorEventArgs e)
        {
            await WebSocketV2.SendAndAddEvent("ROTATOR-SYNCED");
        }

        private async Task RotatorMovedMechanicalHandler(object arg1, RotatorEventArgs args)
        {
            await WebSocketV2.SendAndAddEvent("ROTATOR-MOVED-MECHANICAL", DateTime.Now, new Dictionary<string, object>() {
                { "From", args.From },
                { "To", args.To }
            });
        }

        private async Task RotatorMovedHandler(object arg1, RotatorEventArgs args)
        {
            await WebSocketV2.SendAndAddEvent("ROTATOR-MOVED", DateTime.Now, new Dictionary<string, object>() {
                { "From", args.From },
                { "To", args.To }
            });
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Rotator.Connected -= RotatorConnectedHandler;
            AdvancedAPI.Controls.Rotator.Disconnected -= RotatorDisconnectedHandler;
            AdvancedAPI.Controls.Rotator.Moved -= RotatorMovedHandler;
            AdvancedAPI.Controls.Rotator.MovedMechanical -= RotatorMovedMechanicalHandler;
            AdvancedAPI.Controls.Rotator.Synced -= RotatorSyncedHandler;
            AdvancedAPI.Controls.Rotator.RemoveConsumer(this);
        }

        public async void UpdateDeviceInfo(RotatorInfo deviceInfo)
        {
            await WebSocketV2.SendConsumerEvent("ROTATOR");
        }
    }

    public partial class ControllerV2
    {
        private static CancellationTokenSource RotatorToken;


        [Route("GET", "/equipment/rotator/info")]
        public void RotatorInfo()
        {
            CustomResponse response = new CustomResponse();

            try
            {
                IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

                RotatorInfo info = rotator.GetInfo();
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/rotator/move")]
        public void RotatorMove(float position)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

                if (!rotator.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Rotator not connected", 409));
                }
                else
                {
                    RotatorToken?.Cancel();
                    RotatorToken = new CancellationTokenSource();
                    rotator.Move(position, RotatorToken.Token);
                    response.Response = "Rotator move started";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/rotator/move-mechanical")]
        public void RotatorMoveMechanical(float position)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

                if (!rotator.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Rotator not connected", 409));
                }
                else
                {
                    RotatorToken?.Cancel();
                    RotatorToken = new CancellationTokenSource();
                    rotator.MoveMechanical(position, RotatorToken.Token);
                    response.Response = "Rotator move started";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/rotator/reverse")]
        public void RotatorReverse(bool reverseDirection)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                if (!AdvancedAPI.Controls.Rotator.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Rotator not connected", 409));
                }
                else
                {
                    var rotator = (RotatorVM)typeof(RotatorMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AdvancedAPI.Controls.Rotator);
                    rotator.ReverseCommand.Execute(reverseDirection);

                    response.Response = "Reverse set";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/rotator/set-mechanical-range")]
        public void RotatorSetRange(RotatorRangeTypeEnum range, float rangeStartPosition = -1)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                AdvancedAPI.Controls.Profile.ActiveProfile.RotatorSettings.RangeType = range;
                if (!Request.IsParameterOmitted(nameof(rangeStartPosition)))
                {
                    AdvancedAPI.Controls.Profile.ActiveProfile.RotatorSettings.RangeStartMechanicalPosition = rangeStartPosition;
                }

                response.Response = "Range set";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/rotator/stop-move")]
        public void RotatorStopMove()
        {
            CustomResponse response = new CustomResponse();

            try
            {
                if (!AdvancedAPI.Controls.Rotator.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Rotator not connected", 409));
                }
                else
                {
                    RotatorToken?.Cancel();
                    response.Response = "Rotator move stopped";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }
    }
}
