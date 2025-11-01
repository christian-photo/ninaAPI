#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3
{
    public class ControllerV3 : WebApiController
    {
        private readonly ResponseHandler responseHandler;
        private readonly ApiProcessMediator processMediator;

        public ControllerV3(ResponseHandler responseHandler, ApiProcessMediator processMediator)
        {
            this.responseHandler = responseHandler;
            this.processMediator = processMediator;
        }

        [Route(HttpVerbs.Get, "/")]
        public string Index()
        {
            return $"ninaAPI: https://github.com/christian-photo/ninaAPI/, https://bump.sh/christian-photo/doc/advanced-api, https://bump.sh/christian-photo/doc/advanced-api-websockets";
        }

        [Route(HttpVerbs.Get, "/version")]
        public async Task GetVersion()
        {
            await responseHandler.SendObject(
                HttpContext,
                new { Version = Assembly.GetAssembly(typeof(AdvancedAPI)).GetName().Version.ToString() }
            );
        }

        [Route(HttpVerbs.Get, "/time")]
        public async Task GetTime()
        {
            await responseHandler.SendObject(
                HttpContext,
                new { Time = DateTime.Now }
            );
        }

        [Route(HttpVerbs.Get, "/time/application-start")]
        public async Task GetApplicationStart()
        {
            await responseHandler.SendObject(
                HttpContext,
                new { Time = CoreUtil.ApplicationStartDate }
            );
        }

        [Route(HttpVerbs.Get, "/version/nina")]
        public async Task GetNINAVersion([QueryField] bool friendly)
        {
            await responseHandler.SendObject(
                HttpContext,
                new { Version = friendly ? CoreUtil.VersionFriendlyName : CoreUtil.Version }
            );
        }

        [Route(HttpVerbs.Get, "/process/{id}/status")]
        public async Task GetProcessStatus(string id)
        {
            if (!Guid.TryParse(id, out Guid processId))
            {
                throw new HttpException(HttpStatusCode.BadRequest, "ID could not be parsed");
            }
            object progress = processMediator.GetProgress(processId) ?? throw new HttpException(HttpStatusCode.NotFound, "Process not found");
            await responseHandler.SendObject(
                HttpContext,
                progress
            );
        }

        [Route(HttpVerbs.Post, "/process/{id}/abort")]
        public async Task AbortProcess(string id)
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
            await responseHandler.SendObject(
                HttpContext,
                new StatusResponse(processMediator.GetStatus(processId).ToString())
            );
        }

        [Route(HttpVerbs.Get, "/process/{id}/wait")]
        public async Task WaitForProcess(string id)
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
            await responseHandler.SendObject(
                HttpContext,
                new StatusResponse(process.Status.ToString())
            );
        }
    }
}