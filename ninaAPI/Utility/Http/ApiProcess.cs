#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.Utility.Http
{
    public class ApiProcess : IDisposable
    {
        private Task process;
        private CancellationTokenSource token;
        private readonly Action<CancellationToken> action;

        public ApiProcess(Action<CancellationToken> action)
        {
            this.action = action;
            token = new CancellationTokenSource();
        }

        public void Start()
        {
            process = Task.Run(() => action(token.Token), token.Token);
        }

        public void Stop()
        {
            if (process != null)
            {
                token.Cancel();
            }
        }

        public async Task WaitForExit()
        {
            await process;
        }

        public ApiProcessStatus Status
        {
            get
            {
                if (process == null)
                {
                    return ApiProcessStatus.Pending;
                }
                else if (process.IsCompleted)
                {
                    return ApiProcessStatus.Finished;
                }
                else
                {
                    return ApiProcessStatus.Running;
                }
            }
        }

        public void Dispose()
        {
            Stop();
            token.Dispose();
        }
    }

    public enum ApiProcessStatus
    {
        Pending,
        Running,
        Finished,
        Failed
    }
}