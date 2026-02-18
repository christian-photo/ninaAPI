#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Application
{
    public class SequenceController : WebApiController
    {
        private readonly ISequenceMediator sequenceMediator;
        private readonly ResponseHandler responseHandler;

        public SequenceController(ISequenceMediator sequenceMediator, ResponseHandler responseHandler)
        {
            this.sequenceMediator = sequenceMediator;
            this.responseHandler = responseHandler;
        }

        // TODO: Improve
        [Route(HttpVerbs.Get, "/")]
        public async Task GetSequence()
        {
            ISequenceRootContainer root = sequenceMediator.GetSequenceRoot();
            await responseHandler.SendSequence(HttpContext, root);
        }
    }
}