#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using ninaAPI.WebService.Interfaces;
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.Focuser
{
    public class FocuserController : IHttpController
    {
        private readonly IFocuserMediator focuser;
        private readonly IFilterWheelMediator filterWheel;
        private readonly IApplicationStatusMediator statusMediator;
        private readonly IAutoFocusVMFactory autofocusFactory;
        private readonly ApiProcessMediator processMediator;
        private readonly ISerializerService serializer;

        public FocuserController(
            IFocuserMediator focuser,
            IFilterWheelMediator filterWheel,
            IApplicationStatusMediator statusMediator,
            IAutoFocusVMFactory autofocusFactory,
            ApiProcessMediator processMediator,
            ISerializerService serializer)
        {
            this.focuser = focuser;
            this.filterWheel = filterWheel;
            this.statusMediator = statusMediator;
            this.processMediator = processMediator;
            this.autofocusFactory = autofocusFactory;
            this.serializer = serializer;
        }

        public FocuserInfoResponse FocuserInfo()
        {
            return new FocuserInfoResponse(focuser);
        }

        public object FocuserMove(FocuserMoveBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            if (!focuser.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Focuser);
            }
            else if (focuser.GetInfo().IsMoving || focuser.GetInfo().IsSettling)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Focuser is moving");
            }
            var processId = processMediator.AddProcess(async (token) => await focuser.MoveFocuser(body.Position, token), ApiProcessType.FocuserMove);
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public StringResponse FocuserTemperatureCompensation(FocuserTempCompBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            if (!focuser.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Focuser);
            }
            else if (!focuser.GetInfo().TempCompAvailable)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Temperature compensation not available");
            }

            focuser.ToggleTempComp(body.CompensationEnabled);

            return new StringResponse("Temperature compensation set");
        }

        private IAutoFocusVM autoFocusVM;

        public object StartAutoFocus()
        {
            if (!focuser.GetInfo().Connected)
            {
                throw CommonErrors.DeviceNotConnected(Device.Focuser);
            }
            else if (focuser.GetInfo().IsMoving || focuser.GetInfo().IsSettling)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Focuser is moving");
            }
            else if (FocuserWatcher.IsAutoFocusRunning)
            {
                throw new HttpException(HttpStatusCode.Conflict, "An autofocus is already running");
            }

            autoFocusVM = autofocusFactory.Create();

            var processId = processMediator.AddProcess(AutoFocusProcess.Create(autoFocusVM, filterWheel, statusMediator));
            var result = processMediator.Start(processId);

            (object response, int statusCode) = ResponseFactory.CreateProcessStartedResponse(result, processMediator, processMediator.GetProcess(processId, out var process) ? process : null);

            return (response, statusCode);
        }

        public object AutoFocusListReports()
        {
            var files = FileSystemHelper.GetFilesRecursively(FileSystemHelper.GetAutofocusFolder());

            return files.Select(f => new
            {
                Filename = Path.GetFileNameWithoutExtension(f),
                Date = File.GetCreationTime(f),
            });
        }

        public async Task<string> AutoFocusGetReport(HttpSession session)
        {
            QueryParameter<string> filenameParameter = new QueryParameter<string>("filename", string.Empty, true);

            string filename = filenameParameter.Get(session.Request);
            string file = Path.Combine(FileSystemHelper.GetAutofocusFolder(), $"{filename}.json");
            if (!File.Exists(file))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Report not found");
            }

            string json = await Retry.Do(() => File.ReadAllText(file), TimeSpan.FromMilliseconds(50), 5);
            return json; // TODO: See if it works to return it raw
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), prefix, () => FocuserInfo());
            server.Map(HttpVerbs.POST.ToString(), prefix + "/move", (HttpSession session) => FocuserMove(serializer.Deserialize<FocuserMoveBody>(session.Request.BodyString)));
            server.Map(HttpVerbs.PATCH.ToString(), prefix + "/temp-comp", (HttpSession session) => FocuserTemperatureCompensation(serializer.Deserialize<FocuserTempCompBody>(session.Request.BodyString)));
            server.Map(HttpVerbs.POST.ToString(), prefix + "/auto-focus", () => StartAutoFocus());
            server.Map(HttpVerbs.GET.ToString(), prefix + "/auto-focus/list-reports", (HttpSession session) => AutoFocusListReports());
            server.Map(HttpVerbs.GET.ToString(), prefix + "/auto-focus/get-report", async (HttpSession session) => await AutoFocusGetReport(session));
        }
    }

    public class FocuserMoveBody
    {
        [Required]
        public int Position { get; set; }
    }

    public class FocuserTempCompBody
    {
        [Required]
        public bool CompensationEnabled { get; set; }
    }
}
