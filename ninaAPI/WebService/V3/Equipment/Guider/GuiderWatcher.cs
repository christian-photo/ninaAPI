#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading.Tasks;
using NINA.Core.Interfaces;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;

namespace ninaAPI.WebService.V3.Equipment.Guider
{
    public class GuideStep(IGuideStep guideStep)
    {
        public double RADistanceRaw { get; set; } = guideStep.RADistanceRaw;
        public double DECDistanceRaw { get; set; } = guideStep.DECDistanceRaw;

        public double RADuration { get; set; } = guideStep.RADuration;
        public double DECDuration { get; set; } = guideStep.DECDuration;
    }

    public class GuiderWatcher : EventWatcher, IGuiderConsumer
    {
        private readonly IGuiderMediator guider;

        public static int GuideStepHistoryLength { get; set; } = 100;
        public static ThreadSafeList<GuideStep> GuideStepHistory { get; } = new();

        public GuiderWatcher(EventHistoryManager history, IGuiderMediator guider) : base(history)
        {
            this.guider = guider;
            Channel = WebSocketChannel.Equipment;
        }

        public void Dispose()
        {
            StopWatchers();
        }

        public override void StartWatchers()
        {
            guider.Connected += GuiderConnectedHandler;
            guider.Disconnected += GuiderDisconnectedHandler;
            guider.AfterDither += GuiderAfterDitherHandler;
            guider.GuideEvent += GuiderGuideEventHandler;
            guider.GuidingStarted += GuiderGuidingStartedHandler;
            guider.GuidingStopped += GuiderGuidingStoppedHandler;
            guider.RegisterConsumer(this);
        }

        public override void StopWatchers()
        {
            guider.Connected -= GuiderConnectedHandler;
            guider.Disconnected -= GuiderDisconnectedHandler;
            guider.AfterDither -= GuiderAfterDitherHandler;
            guider.GuideEvent -= GuiderGuideEventHandler;
            guider.GuidingStarted -= GuiderGuidingStartedHandler;
            guider.GuidingStopped -= GuiderGuidingStoppedHandler;
            guider.RemoveConsumer(this);
        }

        private async void GuiderGuideEventHandler(object sender, IGuideStep e)
        {
            GuideStep step = new GuideStep(e);

            GuideStepHistory.Add(step);
            if (GuideStepHistory.Count > GuideStepHistoryLength)
            {
                GuideStepHistory.RemoveAt(0);
            }

            await SubmitEvent("GUIDER-GUIDE-EVENT", step, WebSocketChannel.Guiding);
        }

        private async Task GuiderConnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("GUIDER-CONNECTED");
        private async Task GuiderDisconnectedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("GUIDER-DISCONNECTED");
        private async Task GuiderAfterDitherHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("GUIDER-DITHER-COMPLETED"); // TODO: Maybe this should go to the guiding channel or both
        private async Task GuiderGuidingStartedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("GUIDER-GUIDING-STARTED");
        private async Task GuiderGuidingStoppedHandler(object sender, EventArgs e) => await SubmitAndStoreEvent("GUIDER-GUIDING-STOPPED");

        public async void UpdateDeviceInfo(GuiderInfo deviceInfo)
        {
            await SubmitEvent("GUIDER-INFO-UPDATE", new GuiderInfoResponse(guider), WebSocketChannel.GuiderInfoUpdate);
        }
    }
}
