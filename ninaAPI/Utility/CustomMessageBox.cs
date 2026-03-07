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

using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NINA.Core.MyMessageBox;

namespace ninaAPI.Utility
{
    public static class CustomMessageBox
    {
        public static async Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxResult defaultresult, CancellationToken token)
        {
            MessageBoxResult messageBoxResult = defaultresult;
            return await Application.Current.Dispatcher.Invoke(async delegate
            {
                MyMessageBox myMessageBox = new MyMessageBox
                {
                    Title = caption,
                    Text = messageBoxText
                };
                if (button == MessageBoxButton.OKCancel)
                {
                    myMessageBox.CancelVisibility = Visibility.Visible;
                    myMessageBox.OKVisibility = Visibility.Visible;
                    myMessageBox.YesVisibility = Visibility.Hidden;
                    myMessageBox.NoVisibility = Visibility.Hidden;
                }
                else if (button == MessageBoxButton.YesNo)
                {
                    myMessageBox.CancelVisibility = Visibility.Hidden;
                    myMessageBox.OKVisibility = Visibility.Hidden;
                    myMessageBox.YesVisibility = Visibility.Visible;
                    myMessageBox.NoVisibility = Visibility.Visible;
                }
                else if (button == MessageBoxButton.OK)
                {
                    myMessageBox.CancelVisibility = Visibility.Hidden;
                    myMessageBox.OKVisibility = Visibility.Visible;
                    myMessageBox.YesVisibility = Visibility.Hidden;
                    myMessageBox.NoVisibility = Visibility.Hidden;
                }
                else
                {
                    myMessageBox.CancelVisibility = Visibility.Hidden;
                    myMessageBox.OKVisibility = Visibility.Visible;
                    myMessageBox.YesVisibility = Visibility.Hidden;
                    myMessageBox.NoVisibility = Visibility.Hidden;
                }

                Window mainWindow = Application.Current.MainWindow;
                Window window = new MyMessageBoxView
                {
                    DataContext = myMessageBox,
                    Owner = mainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                window.Closed += delegate
                {
                    Application.Current.MainWindow.Focus();
                };
                mainWindow.Opacity = 0.8;
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                Task t = Task.Run(() => Application.Current.Dispatcher.Invoke(window.ShowDialog), cts.Token);
                while (!window.DialogResult.HasValue && !token.IsCancellationRequested)
                {
                    await Task.Delay(100);
                }
                window.Close();

                mainWindow.Opacity = 1.0;
                if (!window.DialogResult.HasValue)
                {
                    return defaultresult;
                }

                if (window.DialogResult.GetValueOrDefault())
                {
                    if (myMessageBox.YesVisibility == Visibility.Visible)
                    {
                        return MessageBoxResult.Yes;
                    }

                    return MessageBoxResult.OK;
                }

                return MessageBoxResult.None;
            });
        }
    }
}
