#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.FlatDevice;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Equipment
{
    public class ConnectController : WebApiController
    {
        private readonly ICameraMediator camera;
        private readonly IDomeMediator dome;
        private readonly IFilterWheelMediator filterWheel;
        private readonly IFlatDeviceMediator flatDevice;
        private readonly IFocuserMediator focuser;
        private readonly IGuiderMediator guider;
        private readonly ITelescopeMediator mount;
        private readonly IRotatorMediator rotator;
        private readonly ISafetyMonitorMediator safetyMonitor;
        private readonly ISwitchMediator switchMediator;
        private readonly IWeatherDataMediator weatherData;
        private readonly ResponseHandler responseHandler;

        public ConnectController(
            ICameraMediator camera,
            IDomeMediator dome,
            IFilterWheelMediator filterWheel,
            IFlatDeviceMediator flatDevice,
            IFocuserMediator focuser,
            IGuiderMediator guider,
            ITelescopeMediator mount,
            IRotatorMediator rotator,
            ISafetyMonitorMediator safetyMonitor,
            ISwitchMediator switchMediator,
            IWeatherDataMediator weatherData,
            ResponseHandler responseHandler)
        {
            this.camera = camera;
            this.dome = dome;
            this.filterWheel = filterWheel;
            this.flatDevice = flatDevice;
            this.focuser = focuser;
            this.guider = guider;
            this.mount = mount;
            this.rotator = rotator;
            this.safetyMonitor = safetyMonitor;
            this.switchMediator = switchMediator;
            this.weatherData = weatherData;
            this.responseHandler = responseHandler;
        }

        [Route(HttpVerbs.Get, "/{device}/list-devices")]
        public async Task ListDevices(Device device)
        {
            var chooser = GetDeviceChooserVM(device);

            var deviceList = chooser.Devices.Select(d => new DeviceChooserEntry(d));

            await responseHandler.SendObject(HttpContext, deviceList);
        }

        [Route(HttpVerbs.Post, "/{device}/connect")]
        public async Task DeviceConnect(Device device)
        {
            var chooser = GetDeviceChooserVM(device);

            var deviceId = new QueryParameter<string>("deviceId", chooser.SelectedDevice.Id, false, (id) => chooser.Devices.Any(d => d.Id == id));
            var targetDevice = chooser.Devices.First(d => d.Id == deviceId.Get(HttpContext));

            chooser.SelectedDevice = targetDevice;
            bool success = await chooser.SelectedDevice.Connect(System.Threading.CancellationToken.None);

            if (success)
            {
                await responseHandler.SendObject(HttpContext, new StringResponse($"{targetDevice.DisplayName} connected"));
            }
            else
            {
                throw new HttpException(HttpStatusCode.InternalServerError, $"{targetDevice.DisplayName} could not be connected");
            }
        }

        [Route(HttpVerbs.Get, "/equipment/{device}/disconnect")]
        public async Task DeviceDisconnect(Device device)
        {
            var chooser = GetDeviceChooserVM(device);

            if (!chooser.SelectedDevice.Connected)
            {
                throw new HttpException(HttpStatusCode.BadRequest, $"No {device} connected");
            }

            chooser.SelectedDevice.Disconnect();

            await responseHandler.SendObject(HttpContext, new StringResponse($"{chooser.SelectedDevice.DisplayName} disconnected"));
        }

        [Route(HttpVerbs.Get, "/equipment/{device}/rescan")]
        public async Task DeviceRescan(Device device)
        {
            var chooser = GetDeviceChooserVM(device);

            await chooser.GetEquipment();

            await ListDevices(device);
        }

        private IDeviceChooserVM GetDeviceChooserVM(Device device)
        {
            switch (device)
            {
                case Device.Camera:
                    var cam = (CameraVM)typeof(CameraMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(camera);
                    return cam.DeviceChooserVM;
                case Device.Dome:
                    var domeVM = (DomeVM)typeof(DomeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dome);
                    return domeVM.DeviceChooserVM;
                case Device.Filterwheel:
                    var fw = (FilterWheelVM)typeof(FilterWheelMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(filterWheel);
                    return fw.DeviceChooserVM;
                case Device.FlatDevice:
                    var fd = (FlatDeviceVM)typeof(FlatDeviceMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(flatDevice);
                    return fd.DeviceChooserVM;
                case Device.Focuser:
                    var focuserVM = (FocuserVM)typeof(FocuserMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(focuser);
                    return focuserVM.DeviceChooserVM;
                case Device.Guider:
                    var guiderVM = (GuiderVM)typeof(GuiderMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(guider);
                    return guiderVM.DeviceChooserVM;
                case Device.Mount:
                    var mountVM = (TelescopeVM)typeof(TelescopeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(mount);
                    return mountVM.DeviceChooserVM;
                case Device.Rotator:
                    var rotatorVM = (RotatorVM)typeof(RotatorMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(rotator);
                    return rotatorVM.DeviceChooserVM;
                case Device.Safetymonitor:
                    var sf = (SafetyMonitorVM)typeof(SafetyMonitorMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(safetyMonitor);
                    return sf.DeviceChooserVM;
                case Device.Switch:
                    var swit = (SwitchVM)typeof(SwitchMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(switchMediator);
                    return swit.DeviceChooserVM;
                case Device.Weather:
                    var weather = (WeatherDataVM)typeof(WeatherDataMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(weatherData);
                    return weather.DeviceChooserVM;
                default:
                    throw new HttpException(HttpStatusCode.NotFound, "Device not found");
            }
        }
    }

    internal class DeviceChooserEntry(IDevice device)
    {
        public string Name { get; set; } = device.Name;
        public string DisplayName { get; set; } = device.DisplayName;
        public string Description { get; set; } = device.Description;
        public string DeviceId { get; set; } = device.Id;
        public string Category { get; set; } = device.Category;
        public string DriverVersion { get; set; } = device.DriverVersion;
        public string DriverInfo { get; set; } = device.DriverInfo;
        public bool Connected { get; set; } = device.Connected;
    }
}