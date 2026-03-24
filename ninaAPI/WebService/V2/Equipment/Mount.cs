#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

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
using ninaAPI.Utility.Http;
using ninaAPI.WebService.Interfaces;
using SimpleW;
using SimpleW.Modules;
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
        [Route("GET", "/equipment/mount/info")]
        public void MountInfo()
        {
            CustomResponse response = new CustomResponse();

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

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/mount/home")]
        public void MountHome()
        {
            CustomResponse response = new CustomResponse();

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

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/mount/tracking")]
        public void MountTrackingMode(int mode)
        {
            CustomResponse response = new CustomResponse();

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

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/mount/park")]
        public void MountPark()
        {
            CustomResponse response = new CustomResponse();

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
                else
                {
                    if (mount.GetInfo().Slewing)
                    {
                        mount.StopSlew();
                    }
                    mount.ParkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), new CancellationTokenSource().Token);
                    response.Response = "Parking";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/mount/unpark")]
        public void MountUnpark()
        {
            CustomResponse response = new CustomResponse();

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

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/mount/flip")]
        public void MountFlip()
        {
            CustomResponse response = new CustomResponse();

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

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/mount/slew")]
        public async Task MountSlew(double ra, double dec, bool waitForResult = false, bool center = false, bool rotate = false, double rotationAngle = 0)
        {
            CustomResponse response = new CustomResponse();

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

            Response.WriteToResponse(response);
        }

        private static CancellationTokenSource SlewCenterToken;

        [Route("GET", "/equipment/mount/slew/stop")]
        public void MountStopSlew()
        {
            CustomResponse response = new CustomResponse();

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
                    response.Response = "Stopped slew";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/mount/set-park-position")]
        public async Task MountSetPark()
        {
            CustomResponse response = new CustomResponse();

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

            Response.WriteToResponse(response);
        }

        [Route("GET", "/equipment/mount/sync")]
        public async Task MountSync(double ra = -1, double dec = -1)
        {
            CustomResponse response = new CustomResponse();

            try
            {
                ITelescopeMediator mount = AdvancedAPI.Controls.Mount;

                if (!mount.GetInfo().Connected)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount not connected", 400));
                }
                else if (!mount.GetInfo().CanSetPark || mount.GetInfo().AtPark)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Mount is parked", 400));
                }
                else
                {
                    if (!Request.IsParameterOmitted(nameof(ra)) && !Request.IsParameterOmitted(nameof(dec)))
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

            Response.WriteToResponse(response);
        }
    }

    public class MountAxisMoveSocket : IWebSocket
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

        private async Task OnMessageReceivedAsync(WebSocketConnection connection, WebSocketContext context, string text)
        {
            CustomResponse response = new CustomResponse();
            response.Type = CustomResponse.TypeSocket;
            try
            {
                var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(text);
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
            await connection.SendTextAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

        public void ConfigureWebSocket(WebSocketOptions options)
        {
            options.OnUnknown(async (conn, ctx, msg) =>
            {
                await OnMessageReceivedAsync(conn, ctx, msg.RawText);
            });
        }
    }
}
