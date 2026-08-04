#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;

namespace ninaAPI.Utility.Http
{
    public class ApiProcessMediator
    {
        private readonly ConcurrentDictionary<Guid, ApiProcess> processes;

        public ApiProcessMediator()
        {
            processes = new();
        }

        public Guid AddProcess(ApiProcess process)
        {
            processes.AddOrUpdate(process.ProcessId, process, (_, __) => process);
            return process.ProcessId;
        }

        public Guid AddProcess(Func<CancellationToken, Task> action, ApiProcessType type)
        {
            var process = new ApiProcess(action, type);
            return AddProcess(process);
        }

        public bool RemoveProcess(ApiProcess process)
        {
            return RemoveProcess(process.ProcessId);
        }

        public bool RemoveProcess(Guid id)
        {
            return processes.Remove(id, out ApiProcess _);
        }

        public void StopAll()
        {
            foreach (ApiProcess process in processes.Values)
            {
                process.Stop();
            }
        }

        /// <summary>
        /// Stops the process with the given id. It is guaranteed that the process is stopped if the method returns true.
        /// </summary>
        /// <param name="id">The id of the process that should be stopped</param>
        /// <returns>True if the process was found and stopped, false otherwise</returns>
        public bool Stop(Guid id)
        {
            bool found = GetProcess(id, out ApiProcess process);
            if (found)
            {
                process.Stop();
                return true;
            }
            Logger.Debug($"Process {id} not found");
            return false;
        }

        /// <summary>
        /// Starts the process with the given id. It is guaranteed that the process is started if the method returns true.
        /// </summary>
        /// <param name="id">The id of the process that should be started</param>
        /// <returns>True if the process was found and started, false otherwise</returns>
        public ApiProcessStartResult Start(Guid id)
        {
            bool found = GetProcess(id, out ApiProcess process);
            if (found)
            {
                if (process.Status == ApiProcessStatus.Running)
                {
                    return ApiProcessStartResult.AlreadyRunning;
                }
                var conflicts = CheckForConflicts(process.ProcessType, id);
                if (!process.ProcessType.AllowMultiple && conflicts.Count != 0)
                {
                    return ApiProcessStartResult.Conflict;
                }

                process.Start();
                return ApiProcessStartResult.Started;
            }
            Logger.Debug($"Process {id} not found");
            return ApiProcessStartResult.NotFound;
        }

        /// <summary>
        /// Returns the status of the process with the given id
        /// </summary>
        /// <param name="id">The id of the process</param>
        /// <returns>The status of the process</returns>
        public ApiProcessStatus GetStatus(Guid id)
        {
            bool found = GetProcess(id, out ApiProcess process);
            if (found)
            {
                return process.Status;
            }
            Logger.Debug($"Process {id} not found");
            return ApiProcessStatus.NotFound;
        }

        public object GetProgress(Guid id)
        {
            bool found = GetProcess(id, out ApiProcess process);
            if (found)
            {
                return process.GetProgress();
            }
            Logger.Debug($"Process {id} not found");
            return null;
        }

        /// <summary>
        /// Checks if there are any processes of the given type running
        /// </summary>
        /// <param name="type">The type of process to check for</param>
        /// <returns>The processes that are in conflict</returns>
        public List<ApiProcess> CheckForConflicts(ApiProcessType type, Guid id)
        {
            return [.. processes.Values.Where(p => type.ConflictsWith(p.ProcessType) && p.Status == ApiProcessStatus.Running && !p.ProcessId.Equals(id))];
        }

        public bool GetProcess(Guid id, out ApiProcess process)
        {
            return processes.TryGetValue(id, out process);
        }
    }

    public enum ApiProcessStartResult
    {
        Started,
        AlreadyRunning,
        Conflict,
        NotFound,
    }
}