#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.Focuser
{
    public class FocuserController : WebApiController
    {
        private readonly IFocuserMediator focuser;
        private readonly IFilterWheelMediator filterWheel;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly IAutoFocusVMFactory autofocusFactory;
        private readonly ResponseHandler responseHandler;
        private readonly ApiProcessMediator processMediator;

        public FocuserController(
            IFocuserMediator focuser,
            IFilterWheelMediator filterWheel,
            IApplicationStatusMediator statusMediator,
            IAutoFocusVMFactory autofocusFactory,
            ResponseHandler responseHandler,
            ApiProcessMediator processMediator)
        {
            this.focuser = focuser;
            this.filterWheel = filterWheel;
            this.statusMediator = statusMediator;
            this.responseHandler = responseHandler;
            this.processMediator = processMediator;
            this.autofocusFactory = autofocusFactory;
        }

        [Route(HttpVerbs.Get, "/info")]
        public async Task FocuserInfo()
        {
            var info = focuser.GetInfo();

            await responseHandler.SendObject(HttpContext, info);
        }


        [Route(HttpVerbs.Post, "/move/start")]
        public async Task FocuserMove([QueryField] int position)
        {
            object response;
            int statusCode = 200;

            if (!focuser.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Focuser);
            }
            else if (focuser.GetInfo().IsMoving || focuser.GetInfo().IsSettling)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Focuser is moving");
            }
            var processId = processMediator.AddProcess(async (token) => await focuser.MoveFocuser(position, token), ApiProcessType.FocuserMove);
            var result = processMediator.Start(processId);

            if (result == ApiProcessStartResult.Conflict)
            {
                response = ResponseFactory.CreateProcessConflictsResponse(processMediator, processMediator.GetProcess(processId, out var process) ? process : null);
                statusCode = (int)HttpStatusCode.Conflict;
            }
            else
            {
                response = ResponseFactory.CreateProcessResponse(result, processId);
            }

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        private IAutoFocusVM autoFocusVM;

        [Route(HttpVerbs.Post, "/auto-focus/start")]
        public async Task StartAutoFocus()
        {
            object response;
            int statusCode = 200;

            if (!focuser.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Focuser);
            }
            else if (focuser.GetInfo().IsMoving || focuser.GetInfo().IsSettling)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Focuser is moving");
            }
            // TODO: Check if an autofocus is running when it gets added in NINA

            autoFocusVM = autofocusFactory.Create();

            var processId = processMediator.AddProcess(async (token) => await autoFocusVM.StartAutoFocus(
                    filterWheel.GetInfo().SelectedFilter,
                    token,
                    statusMediator.GetStatus()
                ), ApiProcessType.FocuserAutofocus
            );
            var result = processMediator.Start(processId);

            if (result == ApiProcessStartResult.Conflict)
            {
                response = ResponseFactory.CreateProcessConflictsResponse(processMediator, processMediator.GetProcess(processId, out var process) ? process : null);
                statusCode = (int)HttpStatusCode.Conflict;
            }
            else
            {
                response = ResponseFactory.CreateProcessResponse(result, processId);
            }

            await responseHandler.SendObject(HttpContext, response, statusCode);
        }

        [Route(HttpVerbs.Get, "/auto-focus/status")]
        public async Task AutoFocusStatus()
        {
            object response;

            // TODO: Find a solution for this. The process should be retrievable from the process mediator but how do we get the additional information?
            ApiProcessStatus status = ApiProcessStatus.Finished;
            if ((status == ApiProcessStatus.Running || status == ApiProcessStatus.Finished) && autoFocusVM != null)
            {
                response = new
                {
                    Status = status,
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
                response = new StatusResponse
                {
                    Status = status.ToString(),
                };
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Get, "/auto-focus/list-reports")]
        public async Task AutoFocusListReports()
        {
            var files = FileSystemHelper.GetFilesRecursively(FileSystemHelper.GetAutofocusFolder());
            object response = files.Select(f => new
            {
                Filename = Path.GetFileNameWithoutExtension(f),
                Date = File.GetCreationTime(f),
            });

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Get, "/auto-focus/get-report")]
        public async Task AutoFocusGetReport()
        {
            QueryParameter<string> filenameParameter = new QueryParameter<string>("filename", string.Empty, true);

            string filename = filenameParameter.Get(HttpContext);
            string file = Path.Combine(FileSystemHelper.GetAutofocusFolder(), $"{filename}.json");
            if (File.Exists(file))
            {
                string json = await Retry.Do(() => File.ReadAllText(file), TimeSpan.FromMilliseconds(50), 5);
                await responseHandler.SendString(HttpContext, json);
            }
            else
            {
                throw new HttpException(HttpStatusCode.NotFound, "Report not found");
            }
        }
    }
}