#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ninaAPI.Utility.Http
{
    public class ApiProcess : IDisposable
    {
        private Task process;
        private CancellationTokenSource token;
        private readonly Func<CancellationToken, Task> action;

        public Guid ProcessId { get; }
        public ApiProcessType ProcessType { get; }

        public ApiProcess(Func<CancellationToken, Task> action, ApiProcessType type)
        {
            this.action = action;
            this.ProcessType = type;
            token = new CancellationTokenSource();

            ProcessId = Guid.NewGuid();
        }

        /// <summary>
        /// Starts the process and assigns it a cancellation token
        /// </summary>
        public void Start()
        {
            ProcessStarted?.Invoke(this, EventArgs.Empty);
            process = Task.Run(() => action(token.Token), token.Token);
            process.ContinueWith(_ => ProcessFinished?.Invoke(this, EventArgs.Empty));
        }

        public static event EventHandler ProcessStarted;
        public static event EventHandler ProcessFinished;

        /// <summary>
        /// Stop the process using the assigned cancellation token
        /// </summary>
        public void Stop()
        {
            if (process != null)
            {
                token.Cancel();
            }
        }

        /// <summary>
        /// Wait for the process to finish
        /// </summary>
        /// <returns>A task that completes when the process has finished</returns>
        public async Task WaitForExit()
        {
            while (Status == ApiProcessStatus.Pending)
            {
                await Task.Delay(10);
            }
            await process;
        }

        /// <summary>
        /// This is different from status because it can contain additional information
        /// </summary>
        /// <returns></returns>
        public virtual object GetProgress()
        {
            return new StatusResponse(Status.ToString());
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
        NotFound
    }

    public class ApiProcessType
    {
        /// <summary>
        /// The name of the process, e.g. CameraCool
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Wether multiple processes of this type can run at the same time
        /// </summary>
        [JsonIgnore]
        public bool AllowMultiple { get; }

        /// <summary>
        /// A list of other process types that should not run at the same time
        /// </summary>
        [JsonIgnore]
        public ImmutableList<ApiProcessType> Conflicts { get; }

        private ApiProcessType(string name, bool allowMultiple, params ApiProcessType[] conflicts)
        {
            Name = name;
            AllowMultiple = allowMultiple;
            Conflicts = [.. conflicts, this];
        }

        public bool ConflictsWith(ApiProcessType type) => Conflicts.Contains(type) && !AllowMultiple;

        public static readonly ApiProcessType CameraCool = new("CameraCool", false, CameraWarm);
        public static readonly ApiProcessType CameraWarm = new("CameraWarm", false, CameraCool);
        public static readonly ApiProcessType CameraCapture = new("CameraCapture", false, FocuserAutofocus);
        public static readonly ApiProcessType CaptureSave = new("CaptureSave", true);
        public static readonly ApiProcessType CapturePrepare = new("CapturePrepare", true);
        public static readonly ApiProcessType FocuserMove = new("FocuserMove", false, FocuserAutofocus);
        public static readonly ApiProcessType FocuserAutofocus = new("AutoFocus", false, FocuserMove, CameraCapture);
        public static readonly ApiProcessType DomeOpenShutter = new("DomeOpenShutter", false, DomeCloseShutter);
        public static readonly ApiProcessType DomeCloseShutter = new("DomeCloseShutter", false, DomeOpenShutter);
        public static readonly ApiProcessType DomeSlew = new("DomeSlew", false);
        public static readonly ApiProcessType DomePark = new("DomePark", false, DomeSlew);
        public static readonly ApiProcessType DomeFindHome = new("DomeFindHome", false, DomePark, DomeSlew);
    }
}