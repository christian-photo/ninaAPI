#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace ninaAPI 
{

    [Export(typeof(ResourceDictionary))]
    partial class Options : ResourceDictionary
    {
        public Options() 
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo(e.Uri.OriginalString) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}