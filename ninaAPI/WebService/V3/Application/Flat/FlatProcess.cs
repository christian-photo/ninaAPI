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
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.WPF.Base.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Application.Flat
{
    public class SkyFlatProcess : ApiProcess
    {
        private readonly SkyFlat instruction;

        private SkyFlatProcess(Func<CancellationToken, Task> action, ApiProcessType type, SkyFlat instruction) : base(action, type)
        {
            this.instruction = instruction;
        }

        public static SkyFlatProcess Create(SkyFlat instruction, IApplicationStatusMediator statusMediator)
        {
            return new SkyFlatProcess(
                async (token) => await instruction.Execute(statusMediator.GetStatus(), token),
                ApiProcessType.FlatInstruction,
                instruction
            );
        }

        public override object GetProgress()
        {
            object progress;

            if ((Status == ApiProcessStatus.Running || Status == ApiProcessStatus.Finished) && instruction != null)
            {
                progress = new
                {
                    Status = Status,
                    DeterminedHistogramADU = instruction.DeterminedHistogramADU,
                    CurrentExposureTime = instruction.GetExposureItem().ExposureTime,
                    TotalIterations = instruction.GetIterations().Iterations,
                    CompletedIterations = instruction.GetIterations().CompletedIterations, // TODO: Add progress to documentation
                };
            }
            else
            {
                progress = new StatusResponse(Status);
            }

            return progress;
        }
    }


    public class AutoBrightnessFlatProcess : ApiProcess
    {
        private readonly AutoBrightnessFlat instruction;

        private AutoBrightnessFlatProcess(Func<CancellationToken, Task> action, ApiProcessType type, AutoBrightnessFlat instruction) : base(action, type)
        {
            this.instruction = instruction;
        }

        public static AutoBrightnessFlatProcess Create(AutoBrightnessFlat instruction, IApplicationStatusMediator statusMediator)
        {
            return new AutoBrightnessFlatProcess(
                async (token) => await instruction.Execute(statusMediator.GetStatus(), token),
                ApiProcessType.FlatInstruction,
                instruction
            );
        }

        public override object GetProgress()
        {
            return new
            {
                Status = Status,
                DeterminedHistogramADU = instruction.DeterminedHistogramADU,
                CurrentExposureTime = instruction.GetExposureItem().ExposureTime,
                DeterminedPanelBrightness = instruction.GetSetBrightnessItem().Brightness, // Not set from the beginning
                TotalIterations = instruction.GetIterations().Iterations,
                CompletedIterations = instruction.GetIterations().CompletedIterations, // TODO: Add progress to documentation
            };
        }
    }
}
