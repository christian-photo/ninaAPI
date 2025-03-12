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
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class SwitchWatcher : INinaWatcher, ISwitchConsumer
    {
        private readonly Func<object, EventArgs, Task> SwitchConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("SWITCH-CONNECTED");
        private readonly Func<object, EventArgs, Task> SwitchDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("SWITCH-DISCONNECTED");

        public void Dispose()
        {
            AdvancedAPI.Controls.Switch.RemoveConsumer(this);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.Switch.Connected += SwitchConnectedHandler;
            AdvancedAPI.Controls.Switch.Disconnected += SwitchDisconnectedHandler;
            AdvancedAPI.Controls.Switch.RegisterConsumer(this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Switch.Connected -= SwitchConnectedHandler;
            AdvancedAPI.Controls.Switch.Disconnected -= SwitchDisconnectedHandler;
            AdvancedAPI.Controls.Switch.RemoveConsumer(this);
        }

        public async void UpdateDeviceInfo(SwitchInfo deviceInfo)
        {
            await WebSocketV2.SendConsumerEvent("SWITCH");
        }
    }

    public partial class ControllerV2
    {
        private static CancellationTokenSource SwitchToken;

        [Route(HttpVerbs.Get, "/equipment/switch/info")]
        public void SwitchInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ISwitchMediator sw = AdvancedAPI.Controls.Switch;

                SwitchInfo info = sw.GetInfo();
                response.Response = info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/switch/set")]
        public void SwitchSet([QueryField] short index, [QueryField] double value)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ISwitchMediator sw = AdvancedAPI.Controls.Switch;

                if (!sw.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Switch not connected", 409));
                }
                SwitchToken?.Cancel();
                SwitchToken = new CancellationTokenSource();
                sw.SetSwitchValue(index, value, AdvancedAPI.Controls.StatusMediator.GetStatus(), SwitchToken.Token);
                response.Response = "Switch value updated";
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
