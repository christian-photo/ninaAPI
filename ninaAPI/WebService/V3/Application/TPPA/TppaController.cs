#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NINA.Plugin.Interfaces;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.Model;
using SimpleW;

namespace ninaAPI.WebService.V3.Application.TPPA
{
    public class TppaController : IHttpController
    {
        private readonly IMessageBroker messageBroker;
        private readonly ISerializerService serializer;

        public TppaController(IMessageBroker messageBroker, ISerializerService serializer)
        {
            this.messageBroker = messageBroker;
            this.serializer = serializer;
        }

        public async Task<StringResponse> StartAlignment(TppaStartConfig config)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "PolarAlignmentPlugin_DockablePolarAlignmentVM_StartAlignment", config));
            return new StringResponse("Started alignment");
        }

        public async Task<StringResponse> StopAlignment()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "PolarAlignmentPlugin_DockablePolarAlignmentVM_StopAlignment", string.Empty));
            return new StringResponse("Stopped alignment");
        }

        public async Task<StringResponse> PauseAlignment()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "PolarAlignmentPlugin_PolarAlignment_PauseAlignment", string.Empty));
            return new StringResponse("Paused alignment");
        }

        public async Task<StringResponse> ResumeAlignment()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "PolarAlignmentPlugin_PolarAlignment_ResumeAlignment", string.Empty));
            return new StringResponse("Resumed alignment");
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/start-alignment", (HttpRequest request) => StartAlignment(serializer.Deserialize<TppaStartConfig>(request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/stop-alignment", () => StopAlignment());
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/pause-alignment", () => PauseAlignment());
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/resume-alignment", () => ResumeAlignment());
        }
    }
}