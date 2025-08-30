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
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment.Focuser
{
    public class FocuserController : WebApiController
    {
        private readonly IFocuserMediator focuser;
        private readonly ResponseHandler responseHandler;

        public FocuserController(
            IFocuserMediator focuser,
            ResponseHandler responseHandler)
        {
            this.focuser = focuser;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/info")]
        public async Task FocuserInfo()
        {
            object response;

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                response = focuser.GetInfo();
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        private ApiProcess focuserMoveProcess;

        [Route(HttpVerbs.Post, "/move/start")]
        public async Task FocuserMove([QueryField] int position)
        {
            StringResponse response = new StringResponse();

            try
            {
                IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;
                if (!focuser.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Focuser);
                }
                else if (focuser.GetInfo().IsMoving || focuser.GetInfo().IsSettling)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Focuser is moving");
                }
                focuserMoveProcess = new ApiProcess((token) => focuser.MoveFocuser(position, token));
                focuserMoveProcess.Start();
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Get, "/move/status")]
        public async Task FocuserMoveStatus()
        {
            StatusResponse response = new StatusResponse();

            try
            {
                response.Status = focuserMoveProcess?.Status.ToString() ?? ApiProcessStatus.Finished.ToString();
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Post, "/move/abort")]
        public async Task FocuserMoveAbort()
        {
            StringResponse response = new StringResponse();

            try
            {
                if (focuserMoveProcess is null)
                {
                    response.Message = "Focuser move not running";
                }
                else if (focuserMoveProcess.Status == ApiProcessStatus.Running || focuserMoveProcess.Status == ApiProcessStatus.Pending)
                {
                    focuserMoveProcess.Stop();
                    response.Message = "Focuser move aborted";
                }
                else
                {
                    response.Message = "Focuser move not running";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }

            await responseHandler.SendObject(HttpContext, response);
        }


        private ApiProcess autoFocusProcess;
        private IAutoFocusVM autoFocusVM;

        [Route(HttpVerbs.Post, "/auto-focus/start")]
        public async Task StartAutoFocus()
        {
            StringResponse response = new StringResponse();

            try
            {
                if (!focuser.GetInfo().Connected)
                {
                    throw CommonErrors.DeviceNotConnected(Device.Focuser);
                }
                else if (focuser.GetInfo().IsMoving || focuser.GetInfo().IsSettling)
                {
                    throw new HttpException(HttpStatusCode.Conflict, "Focuser is moving");
                }

                autoFocusVM = AdvancedAPI.Controls.AutoFocusFactory.Create();

                autoFocusProcess = new ApiProcess(
                    (token) => autoFocusVM.StartAutoFocus(
                        AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter,
                        token,
                        AdvancedAPI.Controls.StatusMediator.GetStatus()
                    )
                );
                autoFocusProcess.Start();
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Get, "/auto-focus/status")]
        public async Task AutoFocusStatus()
        {
            object response;

            try
            {
                ApiProcessStatus status = autoFocusProcess?.Status ?? ApiProcessStatus.Finished;
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
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Post, "/auto-focus/abort")]
        public async Task AutoFocusAbort()
        {
            StringResponse response = new StringResponse();

            try
            {
                if (autoFocusProcess is null)
                {
                    response.Message = "Autofocus not running";
                }
                else if (autoFocusProcess.Status == ApiProcessStatus.Running || autoFocusProcess.Status == ApiProcessStatus.Pending)
                {
                    autoFocusProcess.Stop();
                    response.Message = "Autofocus aborted";
                }
                else
                {
                    response.Message = "Autofocus not running";
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Get, "/auto-focus/list-reports")]
        public async Task AutoFocusListReports()
        {
            object response;
            try
            {
                var files = FileSystemHelper.GetFilesRecursively(FileSystemHelper.GetAutofocusFolder());
                response = files.Select(f => new
                {
                    Filename = Path.GetFileNameWithoutExtension(f),
                    Date = File.GetCreationTime(f),
                });
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }

            await responseHandler.SendObject(HttpContext, response);
        }

        [Route(HttpVerbs.Get, "/auto-focus/get-report")]
        public async Task AutoFocusGetReport()
        {
            QueryParameter<string> filenameParameter = new QueryParameter<string>("filename", string.Empty, true);

            try
            {
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
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw CommonErrors.UnknwonError(ex);
            }
        }
    }
}