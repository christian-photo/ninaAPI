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
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.Mount
{
    public class MeridianFlipProcess : ApiProcess
    {
        private readonly IMeridianFlipVM flipVM;

        private MeridianFlipProcess(Func<CancellationToken, Task> action, ApiProcessType type, IMeridianFlipVM flipVM) : base(action, type)
        {
            this.flipVM = flipVM;
        }

        public static MeridianFlipProcess Create(IMeridianFlipVM flipVM, ITelescopeMediator mount, IApplicationStatusMediator statusMediator)
        {
            return new MeridianFlipProcess(
                async (token) => await flipVM.MeridianFlip(
                    mount.GetCurrentPosition(),
                    TimeSpan.Zero,
                    token
            ), ApiProcessType.MountFlip, flipVM);
        }

        public override object GetProgress()
        {
            object progress;

            if ((Status == ApiProcessStatus.Running || Status == ApiProcessStatus.Finished) && flipVM != null)
            {
                progress = new
                {
                    Status = Status,
                    FlipStatus = flipVM.Status,
                    TotalSteps = flipVM.Steps.Count,
                    CurrentStep = flipVM.Steps.ActiveStep
                };
            }
            else
            {
                progress = new StatusResponse(Status.ToString());
            }

            return progress;
        }
    }
}
