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

namespace ninaAPI.Utility
{
    public static class DelayedAction
    {
        public static void Execute(TimeSpan delay, Action action)
        {
            Task.Delay(delay).ContinueWith(t => action());
        }
    }

    public class RetriggerableAction
    {
        private readonly Action _action;
        private readonly TimeSpan _delay;
        private CancellationTokenSource _cts;
        private readonly object _lock = new object();

        public RetriggerableAction(Action action, TimeSpan delay)
        {
            _action = action;
            _delay = delay;
        }

        public void Trigger()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
            }

            var token = _cts.Token;

            Task.Delay(_delay, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    _action();
            }, TaskContinuationOptions.NotOnCanceled);
        }

        public void Cancel()
        {
            lock (_lock)
                _cts?.Cancel();
        }
    }
}