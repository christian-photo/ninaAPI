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
using SimpleW;

namespace ninaAPI.WebService.V3.Equipment.Focuser
{
    [Route($"/v3/api/equipment/{EquipmentConstants.FocuserUrlName}")]
    public class FocuserController : Controller
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

        [Route("GET", "/")]
        public FocuserInfoResponse FocuserInfo()
        {
            return new FocuserInfoResponse(focuser);
        }

        [Route("POST", "/move")]
        public object FocuserMove()
        {
            FocuserMoveBody body = serializer.Deserialize<FocuserMoveBody>(Request.BodyString);
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

        [Route("PATCH", "/temp-comp")]
        public StringResponse FocuserTemperatureCompensation()
        {
            FocuserTempCompBody body = serializer.Deserialize<FocuserTempCompBody>(Request.BodyString);
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

        // This needs to be static because the controller instance is disposed after the request
        private static IAutoFocusVM autoFocusVM;

        [Route("POST", "/auto-focus")]
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

        [Route("GET", "/auto-focus/list-reports")]
        public object AutoFocusListReports()
        {
            var files = FileSystemHelper.GetFilesRecursively(FileSystemHelper.GetAutofocusFolder());

            return files.Select(f => new
            {
                Filename = Path.GetFileNameWithoutExtension(f),
                Date = File.GetCreationTime(f),
            });
        }

        [Route("GET", "/auto-focus/get-report")]
        public async Task<string> AutoFocusGetReport()
        {
            QueryParameter<string> filenameParameter = new QueryParameter<string>("filename", string.Empty, true);

            string filename = filenameParameter.Get(Request);
            string file = Path.Combine(FileSystemHelper.GetAutofocusFolder(), $"{filename}.json");
            if (!File.Exists(file))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Report not found");
            }

            string json = await Retry.Do(() => File.ReadAllText(file), TimeSpan.FromMilliseconds(50), 5);
            return json; // TODO: See if it works to return it raw
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
