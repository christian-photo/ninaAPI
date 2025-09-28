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

namespace ninaAPI.WebService.V3.Equipment.Focuser
{
    public class AutoFocusProcess : ApiProcess
    {
        private readonly IAutoFocusVM autoFocusVM;

        private AutoFocusProcess(Func<CancellationToken, Task> action, ApiProcessType type, IAutoFocusVM autoFocusVM) : base(action, type)
        {
            this.autoFocusVM = autoFocusVM;
        }

        public static AutoFocusProcess Create(IAutoFocusVM autoFocusVM, IFilterWheelMediator filterWheel, IApplicationStatusMediator statusMediator)
        {
            return new AutoFocusProcess(
                async (token) => await autoFocusVM.StartAutoFocus(
                    filterWheel.GetInfo().SelectedFilter,
                    token,
                    statusMediator.GetStatus()
            ), ApiProcessType.FocuserAutofocus, autoFocusVM);
        }

        public override object GetProgress()
        {
            object progress;

            if ((Status == ApiProcessStatus.Running || Status == ApiProcessStatus.Finished) && autoFocusVM != null)
            {
                progress = new
                {
                    Status = Status,
                    Duration = autoFocusVM.AutoFocusDuration,
                    AcquiredPoints = autoFocusVM.FocusPoints,
                    LastPoint = autoFocusVM.LastAutoFocusPoint,
                    FinalPoint = autoFocusVM.FinalFocusPoint,
                    GaussianFitting = autoFocusVM.GaussianFitting,
                    HyperbolicFitting = autoFocusVM.HyperbolicFitting,
                    TrendlineFitting = autoFocusVM.TrendlineFitting,
                    QuadraticFitting = autoFocusVM.QuadraticFitting,
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