// Modified

#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/


/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebSockets;
using NINA.Core.Locale;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2.CustomDrivers
{
    [Export(typeof(IEquipmentProvider))]
    public class NetworkedFilterWheelProvider : IEquipmentProvider<IFilterWheel>
    {
        public string Name => "Networked Filter Wheel";

        public IList<IFilterWheel> GetEquipment()
        {
            return [new NetworkedFilterWheel()];
        }
    }


    public class NetworkedFilterWheel : BaseINPC, IFilterWheel
    {

        public static string TargetFilter { get; set; } = string.Empty;
        public static CancellationTokenSource TokenSource { get; private set; } = new CancellationTokenSource();

        public static event EventHandler<string> FilterChangeRequested;

        public NetworkedFilterWheel()
        {
        }

        private bool connected;

        public string Category { get; } = "Advanced API";

        public bool Connected
        {
            get => connected;
            set
            {
                connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description => "A networked manual filter wheel";

        public string DriverInfo => "n.A.";

        public string DriverVersion => "1.0";

        public short InterfaceVersion => 1;

        public int[] FocusOffsets => this.Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => this.Filters.Select((x) => x.Name).ToArray();

        private short position;

        public short Position
        {
            get => position;

            set
            {
                TokenSource = new CancellationTokenSource();
                TargetFilter = this.Filters[value].Name;
                FilterChangeRequested?.Invoke(this, TargetFilter);
                CustomMessageBox.Show(
                    string.Format(Loc.Instance["LblPleaseChangeToFilter"], TargetFilter),
                    Loc.Instance["LblFilterChangeRequired"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxResult.OK,
                    TokenSource.Token).Wait();
                TargetFilter = string.Empty;
                FilterChangeRequested?.Invoke(this, TargetFilter);
                position = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<FilterInfo> Filters => AdvancedAPI.Controls.Profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters;

        public IList<string> SupportedActions => new List<string>();

        public bool HasSetupDialog => false;

        public string Id => "Networked Filter Wheel";

        public string Name => "Networked Filter Wheel";
        public string DisplayName => "Networked Filter Wheel";

        public Task<bool> Connect(CancellationToken token)
        {
            Connected = true;
            if (Filters.Count == 0)
            {
                var filter = new FilterInfo(Loc.Instance["LblFilter"] + 1, 0, 0, -1, new BinningMode(1, 1), -1, -1);
                AdvancedAPI.Controls.Profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Add(filter);
                RaisePropertyChanged(nameof(Filters));
            }
            return Task.FromResult(true);
        }

        public void Disconnect()
        {
            Connected = false;
        }

        public void SetupDialog()
        {
        }

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
    }

    public class NetworkedFilterWheelSocket : WebSocketModule
    {
        public NetworkedFilterWheelSocket(string urlPath) : base(urlPath, true)
        {
            NetworkedFilterWheel.FilterChangeRequested += NetworkedFilterWheel_FilterChangeRequested;
        }

        private void NetworkedFilterWheel_FilterChangeRequested(object sender, string filter)
        {
            foreach (IWebSocketContext context in ActiveContexts)
            {
                SendAsync(context, string.IsNullOrEmpty(filter) ? "Change Complete" : filter);
            }
        }

        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
        {
            string message = Encoding.GetString(buffer);
            if (message.Equals("get-target-filter"))
            {
                await SendAsync(context, string.IsNullOrEmpty(NetworkedFilterWheel.TargetFilter) ? "N/A" : NetworkedFilterWheel.TargetFilter);
            }
            else if (message.Equals("filter-changed"))
            {
                NetworkedFilterWheel.TokenSource.Cancel();
                NetworkedFilterWheel.TargetFilter = string.Empty;
            }
        }
    }
}