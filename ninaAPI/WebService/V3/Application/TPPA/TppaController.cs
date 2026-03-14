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
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Plugin.Interfaces;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Model;

namespace ninaAPI.WebService.V3.Application.TPPA
{
    public class TppaController : WebApiController
    {
        private readonly IMessageBroker messageBroker;
        private readonly ResponseHandler responseHandler;

        public TppaController(IMessageBroker messageBroker, ResponseHandler responseHandler)
        {
            this.messageBroker = messageBroker;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Post, "/start-alignment")]
        public async Task StartAlignment([JsonData] TppaStartConfig config)
        {
            Validator.ValidateObject(config, new ValidationContext(config));

            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "PolarAlignmentPlugin_DockablePolarAlignmentVM_StartAlignment", config));
            await responseHandler.SendObject(HttpContext, new StringResponse("Started alignment"));
        }

        [Route(HttpVerbs.Post, "/stop-alignment")]
        public async Task StopAlignment()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "PolarAlignmentPlugin_DockablePolarAlignmentVM_StopAlignment", string.Empty));
            await responseHandler.SendObject(HttpContext, new StringResponse("Stopped alignment"));
        }

        [Route(HttpVerbs.Post, "/pause-alignment")]
        public async Task PauseAlignment()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "PolarAlignmentPlugin_PolarAlignment_PauseAlignment", string.Empty));
            await responseHandler.SendObject(HttpContext, new StringResponse("Paused alignment"));
        }

        [Route(HttpVerbs.Post, "/resume-alignment")]
        public async Task ResumeAlignment()
        {
            await messageBroker.Publish(new NINAMessage(Guid.NewGuid(), "PolarAlignmentPlugin_PolarAlignment_ResumeAlignment", string.Empty));
            await responseHandler.SendObject(HttpContext, new StringResponse("Resumed alignment"));
        }
    }
}