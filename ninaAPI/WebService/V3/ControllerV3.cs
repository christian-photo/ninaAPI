#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using NINA.Core.Utility;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.WebService.V3.Websocket.Event;
using SimpleW;
using SimpleW.Service.BasicAuth;

namespace ninaAPI.WebService.V3
{
    [Route("/v3/api")]
    [BasicAuth]
    public class ControllerV3 : Controller
    {
        private readonly ApiProcessMediator processMediator;

        public ControllerV3(ApiProcessMediator processMediator)
        {
            this.processMediator = processMediator;
        }

        [Route("GET", "/")]
        public string Index()
        {
            return $"ninaAPI: https://github.com/christian-photo/ninaAPI/, https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api, https://github.com/christian-photo/ninaAPI/wiki/Websocket-V3";
        }

        [Route("GET", "/version")]
        public object GetVersion()
        {
            return new { Version = Assembly.GetAssembly(typeof(AdvancedAPI)).GetName().Version.ToString() };
        }

        [Route("GET", "/time")]
        public object GetTime()
        {
            return new { Time = DateTime.Now };
        }

        [Route("GET", "/time/application-start")]
        public object GetApplicationStart()
        {
            return new { Time = CoreUtil.ApplicationStartDate };
        }

        [Route("GET", "/version/nina")]
        public object GetNINAVersion()
        {
            QueryParameter<bool> friendlyParameter = new QueryParameter<bool>("friendly", false, false);
            friendlyParameter.Get(Request);

            bool friendly = friendlyParameter.Value;

            return new { Version = friendly ? CoreUtil.VersionFriendlyName : CoreUtil.Version };
        }

        [Route("GET", "/process/:id")]
        public object GetProcessStatus(string id)
        {
            if (!Guid.TryParse(id, out Guid processId))
            {
                throw new HttpException(HttpStatusCode.BadRequest, "ID could not be parsed");
            }
            object progress = processMediator.GetProgress(processId) ?? throw new HttpException(HttpStatusCode.NotFound, "Process not found");
            return progress;
        }

        [Route("DELETE", "/process/:id")]
        public StatusResponse AbortProcess(string id)
        {
            if (!Guid.TryParse(id, out Guid processId))
            {
                throw new HttpException(HttpStatusCode.BadRequest, "ID could not be parsed");
            }
            bool found = processMediator.Stop(processId);
            if (!found)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Process not found");
            }
            return new StatusResponse(processMediator.GetStatus(processId));
        }

        [Route("GET", "/process/:id/wait")]
        public async Task<StatusResponse> WaitForProcess(string id)
        {
            if (!Guid.TryParse(id, out Guid processId))
            {
                throw new HttpException(HttpStatusCode.BadRequest, "ID could not be parsed");
            }
            bool found = processMediator.GetProcess(processId, out ApiProcess process);
            if (!found)
            {
                throw new HttpException(HttpStatusCode.NotFound, "Process not found");
            }
            await process.WaitForExit();
            return new StatusResponse(process.Status);
        }

        [Route("GET", "/events")]
        public List<WebSocketHistoryEvent> GetEventHistory()
        {
            PagerParameterSet pagerParameter = PagerParameterSet.Default();
            pagerParameter.Evaluate(Request);

            EventHistoryManager history = (AdvancedAPI.V3 as V3Api).GetEventWebSocket().EventHistoryManager;
            var events = history.GetEventHistoryPage(pagerParameter.PageParameter.Value, pagerParameter.PageSizeParameter.Value);

            return events;
        }
    }
}