#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ninaAPI.WebService.GET
{
    public class EquipmentMediator
    {
        public static Dictionary<string, string> GetCamera(string property)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                ICameraMediator camera = AdvancedAPI.Controls.Camera;

                if (property.Equals("all"))
                {
                    foreach (PropertyInfo info in camera.GetInfo().GetType().GetProperties())
                    {
                        result.Add(info.Name, info.GetValue(camera.GetInfo()).ToString());
                    }
                    return result;
                }
                result.Add(property, camera.GetInfo().GetType().GetProperty(property).GetValue(camera.GetInfo()).ToString());

                return result;

            } catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return result;
        }

        public static Dictionary<string, string> GetTelescope(string property)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                ITelescopeMediator telescope = AdvancedAPI.Controls.Telescope;

                if (property.Equals("all"))
                {
                    foreach (PropertyInfo info in telescope.GetInfo().GetType().GetProperties())
                    {
                        result.Add(info.Name, info.GetValue(telescope.GetInfo()).ToString());
                    }
                    return result;
                }
                result.Add(property, telescope.GetInfo().GetType().GetProperty(property).GetValue(telescope.GetInfo()).ToString());
                return result;

            } catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return result;
        }
    }
}
