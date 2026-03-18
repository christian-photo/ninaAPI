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
using ninaAPI.WebService.Interfaces;
using ninaAPI.WebService.V3.Websocket.Event;
using SimpleW;

namespace ninaAPI.WebService.V3
{
    public class ControllerV3 : IHttpController
    {
        private readonly ResponseHandler responseHandler;
        private readonly ApiProcessMediator processMediator;

        public ControllerV3(ResponseHandler responseHandler, ApiProcessMediator processMediator)
        {
            this.responseHandler = responseHandler;
            this.processMediator = processMediator;
        }

        public string Index()
        {
            return $"ninaAPI: https://github.com/christian-photo/ninaAPI/, https://christian-photo.github.io/github-page/projects/ninaAPI/v3/doc/api, https://github.com/christian-photo/ninaAPI/wiki/Websocket-V3";
        }

        public async Task<object> GetVersion()
        {
            return new { Version = Assembly.GetAssembly(typeof(AdvancedAPI)).GetName().Version.ToString() };
        }

        public async Task<object> GetTime()
        {
            return new { Time = DateTime.Now };
        }

        public async Task<object> GetApplicationStart()
        {
            return new { Time = CoreUtil.ApplicationStartDate };
        }

        public async Task<object> GetNINAVersion(HttpRequest request)
        {
            QueryParameter<bool> friendlyParameter = new QueryParameter<bool>("friendly", false, false);
            friendlyParameter.Get(request);

            bool friendly = friendlyParameter.Value;

            return new { Version = friendly ? CoreUtil.VersionFriendlyName : CoreUtil.Version };
        }

        public async Task<object> GetProcessStatus(string id)
        {
            if (!Guid.TryParse(id, out Guid processId))
            {
                throw new HttpException(HttpStatusCode.BadRequest, "ID could not be parsed");
            }
            object progress = processMediator.GetProgress(processId) ?? throw new HttpException(HttpStatusCode.NotFound, "Process not found");
            return progress;
        }

        public async Task<StatusResponse> AbortProcess(string id)
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

        public async Task<List<WebSocketHistoryEvent>> GetEventHistory(HttpRequest request)
        {
            PagerParameterSet pagerParameter = PagerParameterSet.Default();
            pagerParameter.Evaluate(request);

            EventHistoryManager history = (AdvancedAPI.V3 as V3Api).GetEventWebSocket().EventHistoryManager;
            var events = history.GetEventHistoryPage(pagerParameter.PageParameter.Value, pagerParameter.PageSizeParameter.Value);

            return events;
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, () => Index());
            server.Map(HttpVerbs.GET.ToString(), prefix + "/version", async () => await GetVersion());
            server.Map(HttpVerbs.GET.ToString(), prefix + "/time", async () => await GetTime());
            server.Map(HttpVerbs.GET.ToString(), prefix + "/time/application-start", async () => await GetApplicationStart());
            server.Map(HttpVerbs.GET.ToString(), prefix + "/version/nina", async (HttpRequest request) => await GetNINAVersion(request));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/process/:id", async (string id) => await GetProcessStatus(id));
            server.Map(HttpVerbs.DELETE.ToString(), prefix + "/process/:id", async (string id) => await AbortProcess(id));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/process/:id/wait", async (string id) => await WaitForProcess(id));
            server.Map(HttpVerbs.GET.ToString(), prefix + "/events", async (HttpRequest request) => await GetEventHistory(request));
        }
    }
}