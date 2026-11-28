#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using ninaAPI.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class MountInfo : TelescopeInfo
    {
        public TrackingMode? TrackingMode { get; set; }
        public MountInfo(ITelescopeMediator t)
        {
            var info = t.GetInfo();
            var props = info.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            var thisType = typeof(MountInfo);
            foreach (var prop in props)
            {
                thisType.GetProperty(prop.Name).SetValue(this, prop.GetValue(info));
            }

            TrackingMode = (t.GetDevice() as ITelescope)?.TrackingMode;
        }
    }

    public class MountWatcher : INinaWatcher, ITelescopeConsumer
    {
        private readonly Func<object, EventArgs, Task> MountConnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-CONNECTED");
        private readonly Func<object, EventArgs, Task> MountDisconnectedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-DISCONNECTED");
        private readonly Func<object, BeforeMeridianFlipEventArgs, Task> MountBeforeMeridianFlipHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-BEFORE-FLIP");
        private readonly Func<object, AfterMeridianFlipEventArgs, Task> MountAfterMeridianFlipHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-AFTER-FLIP");
        private readonly Func<object, EventArgs, Task> MountHomedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-HOMED");
        private readonly Func<object, EventArgs, Task> MountParkedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-PARKED");
        private readonly Func<object, EventArgs, Task> MountUnparkedHandler = async (_, _) => await WebSocketV2.SendAndAddEvent("MOUNT-UNPARKED");

        public void Dispose()
        {
            AdvancedAPI.Controls.Mount.RemoveConsumer(this);
        }

        public void StartWatchers()
        {
            AdvancedAPI.Controls.Mount.Connected += MountConnectedHandler;
            AdvancedAPI.Controls.Mount.Disconnected += MountDisconnectedHandler;
            AdvancedAPI.Controls.Mount.BeforeMeridianFlip += MountBeforeMeridianFlipHandler;
            AdvancedAPI.Controls.Mount.AfterMeridianFlip += MountAfterMeridianFlipHandler;
            AdvancedAPI.Controls.Mount.Homed += MountHomedHandler;
            AdvancedAPI.Controls.Mount.Parked += MountParkedHandler;
            AdvancedAPI.Controls.Mount.Unparked += MountUnparkedHandler;
            AdvancedAPI.Controls.Mount.RegisterConsumer(this);
        }

        public void StopWatchers()
        {
            AdvancedAPI.Controls.Mount.Connected -= MountConnectedHandler;
            AdvancedAPI.Controls.Mount.Disconnected -= MountDisconnectedHandler;
            AdvancedAPI.Controls.Mount.BeforeMeridianFlip -= MountBeforeMeridianFlipHandler;
            AdvancedAPI.Controls.Mount.AfterMeridianFlip -= MountAfterMeridianFlipHandler;
            AdvancedAPI.Controls.Mount.Homed -= MountHomedHandler;
            AdvancedAPI.Controls.Mount.Parked -= MountParkedHandler;
            AdvancedAPI.Controls.Mount.Unparked -= MountUnparkedHandler;
            AdvancedAPI.Controls.Mount.RemoveConsumer(this);
        }

        public async void UpdateDeviceInfo(TelescopeInfo deviceInfo)
        {
            await WebSocketV2.SendConsumerEvent("MOUNT");
        }
    }

    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/equipment/mount/info")]
        public void MountInfo()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;
                response.Response = new MountInfo(mount);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/home")]
        public void MountHome()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount parked", 409));
                }
                else if (mount.GetInfo().AtHome)
                {
                    response.Response = "Mount already homed";
                }
                else
                {
                    if (mount.GetInfo().Slewing)
                    {
                        mount.StopSlew();
                    }
                    mount.FindHome(AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    response.Response = "Homing";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/tracking")]
        public void MountTrackingMode([QueryField] int mode)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount parked", 409));
                }
                else
                {
                    // Siderial: 0
                    // Lunar: 1
                    // Solar: 2
                    // King: 3
                    // Stopped: 4 (but actually 5)
                    if (mode == 4)
                        mode++;

                    if (mode >= 0 && mode < 6)
                    {
                        response.Response = "Tracking mode changed";
                        response.Success = mount.SetTrackingMode((TrackingMode)mode);
                    }
                    else
                    {
                        response = CoreUtility.CreateErrorTable(new Error("Invalid tracking mode", 400));
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        private static Task<bool> MountParkTask;
        private static CancellationTokenSource MountParkToken;
        private static readonly SemaphoreSlim MountParkLock = new SemaphoreSlim(1, 1);

        private static async Task<bool> IsMountParking(ITelescopeMediator mount, CancellationToken token)
        {
            var vm = typeof(TelescopeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(mount) as TelescopeVM;
            if (MountParkTask?.IsCompleted != false && vm?.IsParkingOrHoming != true)
            {
                return false;
            }

            // A park that was stopped (e.g. by slew/stop) keeps waiting for the mount to reach the park position, so only trust it while the mount is moving
            DateTime start = DateTime.Now;
            while (DateTime.Now - start < TimeSpan.FromSeconds(2))
            {
                if (IsMountSlewing(mount))
                {
                    return true;
                }
                await Task.Delay(250, token);
            }

            Logger.Warning("Mount is marked as parking but is not moving, parking again");
            return false;
        }

        private static async Task CancelMountPark(CancellationToken token)
        {
            MountParkToken?.Cancel();
            if (MountParkTask?.IsCompleted == false)
            {
                await Task.WhenAny(MountParkTask, Task.Delay(TimeSpan.FromSeconds(5), token));
            }
        }

        private static bool IsMountSlewing(ITelescopeMediator mount)
        {
            // Read the device directly, the cached info can lag behind by the device polling interval
            return (mount.GetDevice() as ITelescope)?.Slewing ?? mount.GetInfo().Slewing;
        }

        private static async Task StopMountSlewing(ITelescopeMediator mount, CancellationToken token)
        {
            // N.I.N.A. queues a slew that is issued while another one is running and starts it as soon as the mount stops.
            // Mounts (e.g. OnStep) also reject a park command while still decelerating.
            // So keep stopping until the mount stays still, then it is safe to park.
            while (true)
            {
                mount.StopSlew();
                SlewCenterToken?.Cancel();

                DateTime stopIssued = DateTime.Now;
                while (IsMountSlewing(mount) && DateTime.Now - stopIssued < TimeSpan.FromSeconds(2))
                {
                    await Task.Delay(250, token);
                }

                if (!IsMountSlewing(mount))
                {
                    // N.I.N.A. checks for a queued slew once per second, give it time to start before parking
                    await Task.Delay(TimeSpan.FromSeconds(2), token);
                    if (!IsMountSlewing(mount))
                    {
                        return;
                    }
                }
            }
        }

        private static async Task<bool> StopSlewAndPark(ITelescopeMediator mount, CancellationToken token)
        {
            // Make sure a stalled park does not keep running next to the new one
            await CancelMountPark(token);

            if (IsMountSlewing(mount))
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                try
                {
                    await StopMountSlewing(mount, timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    Logger.Warning("Mount did not stop slewing within 30 seconds or the request was cancelled, not parking");
                    return false;
                }
            }

            MountParkToken = new CancellationTokenSource();
            MountParkTask = mount.ParkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), MountParkToken.Token);
            return true;
        }

        [Route(HttpVerbs.Get, "/equipment/mount/park")]
        public async Task MountPark()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response.Response = "Mount already parked";
                }
                // If the lock is taken, another request is currently stopping a slew before parking
                else if (!await MountParkLock.WaitAsync(0))
                {
                    response.Response = "Parking";
                }
                else
                {
                    try
                    {
                        // A park that is already running reports Slewing = true. Stopping it here would abort the park, so leave it alone.
                        if (await IsMountParking(mount, HttpContext.CancellationToken))
                        {
                            response.Response = "Parking";
                        }
                        else if (!await StopSlewAndPark(mount, HttpContext.CancellationToken))
                        {
                            response = CoreUtility.CreateErrorTable(new Error("Mount did not stop slewing", 409));
                        }
                        else
                        {
                            response.Response = "Parking";
                        }
                    }
                    finally
                    {
                        MountParkLock.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/unpark")]
        public void MountUnpark()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (!mount.GetInfo().AtPark)
                {
                    response.Response = "Mount not parked";
                }
                else
                {
                    mount.UnparkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    response.Response = "Unparking";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/flip")]
        public void MountFlip()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount parked", 409));
                }
                else
                {
                    mount.MeridianFlip(mount.GetInfo().Coordinates, new CancellationTokenSource().Token);
                    response.Response = "Flipping";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/slew")]
        public async Task MountSlew([QueryField] double ra, [QueryField] double dec, [QueryField] bool waitForResult, [QueryField] bool center, [QueryField] bool rotate, [QueryField] double rotationAngle)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount parked", 409));
                }
                else
                {
                    if (rotate)
                    {
                        CenterAndRotate cr = new CenterAndRotate(AdvancedAPI.Controls.Profile, mount, AdvancedAPI.Controls.Imaging, AdvancedAPI.Controls.Rotator, AdvancedAPI.Controls.FilterWheel, AdvancedAPI.Controls.Guider, AdvancedAPI.Controls.Dome, AdvancedAPI.Controls.DomeFollower, AdvancedAPI.Controls.PlateSolver, AdvancedAPI.Controls.WindowFactory);
                        cr.Coordinates.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                        cr.PositionAngle = rotationAngle;

                        SlewCenterToken?.Cancel();
                        SlewCenterToken = new CancellationTokenSource();
                        if (waitForResult)
                        {
                            await cr.Execute(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewCenterToken.Token);
                            response.Success = cr.Status == SequenceEntityStatus.FINISHED;
                            response.Response = response.Success ? "Slew finished" : "Slew failed";
                        }
                        else
                        {
                            cr.Execute(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewCenterToken.Token);
                            response.Response = "Slew started";
                        }
                    }
                    else if (center)
                    {
                        Center instruction = new Center(AdvancedAPI.Controls.Profile, mount, AdvancedAPI.Controls.Imaging, AdvancedAPI.Controls.FilterWheel, AdvancedAPI.Controls.Guider, AdvancedAPI.Controls.Dome, AdvancedAPI.Controls.DomeFollower, AdvancedAPI.Controls.PlateSolver, AdvancedAPI.Controls.WindowFactory);
                        instruction.Coordinates.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);

                        SlewCenterToken?.Cancel();
                        SlewCenterToken = new CancellationTokenSource();
                        Logger.Info(JsonConvert.SerializeObject(instruction.Coordinates.Coordinates));
                        if (waitForResult)
                        {
                            await instruction.Execute(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewCenterToken.Token);
                            response.Success = instruction.Status == SequenceEntityStatus.FINISHED;
                            response.Response = response.Success ? "Slew finished" : "Slew failed";
                        }
                        else
                        {
                            instruction.Execute(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewCenterToken.Token);
                            response.Response = "Slew started";
                        }
                    }
                    else
                    {
                        if (waitForResult)
                        {
                            bool result = await mount.SlewToCoordinatesAsync(new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000), CancellationToken.None);
                            response.Success = result;
                            response.Response = result ? "Slew finished" : "Slew failed";
                        }
                        else
                        {
                            mount.SlewToCoordinatesAsync(new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000), CancellationToken.None);
                            response.Response = "Slew started";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        private static CancellationTokenSource SlewCenterToken;

        [Route(HttpVerbs.Get, "/equipment/mount/slew/stop")]
        public void MountStopSlew()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 409));
                }
                else
                {
                    mount.StopSlew();
                    SlewCenterToken?.Cancel();
                    MountParkToken?.Cancel();
                    response.Response = "Stopped slew";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/set-park-position")]
        public async Task MountSetPark()
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 400));
                }
                else if (!mount.GetInfo().CanSetPark || mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount can not set park position", 400));
                }
                else
                {
                    var vm = typeof(TelescopeMediator).GetField("handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(mount) as TelescopeVM;
                    bool result = await vm.SetParkPosition();
                    response.Success = result;
                    response.Response = result ? "Park position set" : "";
                    response.Error = result ? "" : "Park position update failed";
                    response.StatusCode = result ? 200 : 400;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/equipment/mount/sync")]
        public async Task MountSync([QueryField] double ra, [QueryField] double dec)
        {
            HttpResponse response = new HttpResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 400));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount is parked", 400));
                }
                else
                {
                    if (!HttpContext.IsParameterOmitted(nameof(ra)) && !HttpContext.IsParameterOmitted(nameof(dec)))
                    {
                        await mount.Sync(new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000));
                        response.Response = "Synced";
                    }
                    else
                    {
                        SolveAndSync instruction = new SolveAndSync(
                            AdvancedAPI.Controls.Profile,
                            mount,
                            AdvancedAPI.Controls.Rotator,
                            AdvancedAPI.Controls.Imaging,
                            AdvancedAPI.Controls.FilterWheel,
                            AdvancedAPI.Controls.PlateSolver,
                            AdvancedAPI.Controls.WindowFactory);
                        await instruction.Run(AdvancedAPI.Controls.StatusMediator.GetStatus(), CancellationToken.None);
                        response.Response = "Synced";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }

    public class MountAxisMoveSocket : WebSocketModule
    {
        private static DateTime eastTimer;
        private double eastRate;
        private static DateTime westTimer;
        private double westRate;
        private static DateTime northTimer;
        private double northRate;
        private static DateTime southTimer;
        private double southRate;

        private static object _timerLock = new object();

        public MountAxisMoveSocket(string urlPath) : base(urlPath, true)
        {

        }

        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
        {
            HttpResponse response = new HttpResponse();
            response.Type = HttpResponse.TypeSocket;
            try
            {
                var message = Encoding.GetString(buffer);
                var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                string direction = json["direction"].ToString().ToLower();
                double rate = double.Parse(json["rate"].ToString());

                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 400));
                }
                else if (mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount parked", 400));
                }
                else
                {
                    switch (direction)
                    {
                        case "east":
                            if (eastRate != rate)
                            {
                                mount.MoveAxis(TelescopeAxes.Primary, rate);
                            }
                            lock (_timerLock)
                            {
                                eastTimer = DateTime.Now;
                                eastRate = rate;
                            }
                            DelayedAction.Execute(TimeSpan.FromMilliseconds(2000), () =>
                            {
                                lock (_timerLock)
                                {
                                    Logger.Debug($"Time since last message: {DateTime.Now - eastTimer}");
                                    if (DateTime.Now - eastTimer > TimeSpan.FromSeconds(1.8)) // This difference is due to the inaccuracy of the cpu scheduler
                                    {
                                        mount.MoveAxis(TelescopeAxes.Primary, 0);
                                        eastRate = 0;
                                    }
                                }
                            });
                            break;

                        case "west":
                            if (westRate != rate)
                            {
                                mount.MoveAxis(TelescopeAxes.Primary, -rate);
                            }
                            lock (_timerLock)
                            {
                                westTimer = DateTime.Now;
                                westRate = rate;
                            }
                            DelayedAction.Execute(TimeSpan.FromMilliseconds(2000), () =>
                            {
                                lock (_timerLock)
                                {
                                    Logger.Debug($"Time since last message: {DateTime.Now - westTimer}");
                                    if (DateTime.Now - westTimer > TimeSpan.FromSeconds(1.8))
                                    {
                                        mount.MoveAxis(TelescopeAxes.Primary, 0);
                                        westRate = 0;
                                    }
                                }
                            });
                            break;

                        case "north":
                            if (northRate != rate)
                            {
                                mount.MoveAxis(TelescopeAxes.Secondary, rate);
                            }
                            lock (_timerLock)
                            {
                                northTimer = DateTime.Now;
                                northRate = rate;
                            }
                            DelayedAction.Execute(TimeSpan.FromMilliseconds(2000), () =>
                            {
                                lock (_timerLock)
                                {
                                    Logger.Debug($"Time since last message: {DateTime.Now - northTimer}");
                                    if (DateTime.Now - northTimer > TimeSpan.FromSeconds(1.8))
                                    {
                                        mount.MoveAxis(TelescopeAxes.Secondary, 0);
                                        northRate = 0;
                                    }
                                }
                            });
                            break;

                        case "south":
                            if (southRate != rate)
                            {
                                mount.MoveAxis(TelescopeAxes.Secondary, -rate);
                            }
                            lock (_timerLock)
                            {
                                southTimer = DateTime.Now;
                                southRate = rate;
                            }
                            DelayedAction.Execute(TimeSpan.FromMilliseconds(2000), () =>
                            {
                                lock (_timerLock)
                                {
                                    Logger.Debug($"Time since last message: {DateTime.Now - southTimer}");
                                    if (DateTime.Now - southTimer > TimeSpan.FromSeconds(1.8))
                                    {
                                        mount.MoveAxis(TelescopeAxes.Secondary, 0);
                                        southRate = 0;
                                    }
                                }
                            });
                            break;

                        default:
                            response = CoreUtility.CreateErrorTable(new Error("Invalid direction", 400));
                            break;
                    }
                    response.Response = rate == 0 ? "Stopped Move" : "Moving";
                }
            }
            catch (Exception ex)
            {
                response = CoreUtility.CreateErrorTable(new Error(ex.Message, 400));
            }
            await context.WebSocket.SendAsync(Encoding.GetBytes(System.Text.Json.JsonSerializer.Serialize(response)), true);
        }
    }
}
