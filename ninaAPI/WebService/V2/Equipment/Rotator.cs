#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
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


        [Route(HttpVerbs.Get, "/equipment/rotator/info")]
        public void RotatorInfo()
        {
            HttpResponse response = new HttpResponse();

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

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/rotator/move")]
        public void RotatorMove([QueryField] float position)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

                if (!rotator.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Rotator not connected", 409));
                }
                RotatorToken?.Cancel();
                RotatorToken = new CancellationTokenSource();
                rotator.Move(position, RotatorToken.Token);
                response.Response = "Rotator move started";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/rotator/move-mechanical")]
        public void RotatorMoveMechanical([QueryField] float position)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

                if (!rotator.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Rotator not connected", 409));
                }
                RotatorToken?.Cancel();
                RotatorToken = new CancellationTokenSource();
                rotator.MoveMechanical(position, RotatorToken.Token);
                response.Response = "Rotator move started";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }
}
