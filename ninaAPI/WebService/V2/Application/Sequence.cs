#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO.WebApi;
using EmbedIO;
using System;
using EmbedIO.Routing;
using ninaAPI.Utility;
using NINA.Core.Utility;
using NINA.Sequencer.Interfaces.Mediator;
using System.Collections.Generic;
using NINA.Sequencer.Container;
using System.Collections;
using NINA.Profile.Interfaces;

namespace ninaAPI.WebService.V2
{
    public partial class ControllerV2
    {
        [Route(HttpVerbs.Get, "/sequence/json")]
        public void SequenceJson()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                // TODO: Make returned sequence more descriptive, eg add specific fields for some instructions like af after time or loop unitl
                ISequenceMediator Sequence = AdvancedAPI.Controls.Sequence;

                if (!Sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 409));
                }
                else
                {
                    IList<IDeepSkyObjectContainer> targets = Sequence.GetAllTargets();
                    if (targets.Count == 0)
                    {
                        response = CoreUtility.CreateErrorTable(new Error("No DSO Container found", 409));
                    }
                    else
                    {
                        response.Response = getSequenceRecursivley(targets[0].Parent.Parent);
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

        private static List<Hashtable> getSequenceRecursivley(ISequenceContainer sequence)
        {
            List<Hashtable> result = new List<Hashtable>();
            foreach (var item in sequence.Items)
            {
                if (item is ISequenceContainer container)
                {
                    result.Add(new Hashtable()
                    {
                        { "Name", item.Name + "_Container" },
                        { "Status", item.Status.ToString() },
                        { "Description", item.Description },
                        { "Items", getSequenceRecursivley(container) }
                    });
                }
                else
                {
                    result.Add(new Hashtable()
                    {
                        { "Name", item.Name },
                        { "Status", item.Status.ToString() },
                        { "Description", item.Description }
                    });
                }
            }

            return result;
        }

        [Route(HttpVerbs.Get, "/sequence/start")]
        public void SequenceStart([QueryField] bool skipValidation)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                if (!sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 409));
                }
                else
                {
                    sequence.StartAdvancedSequence(skipValidation);
                    response.Response = "Sequence started";
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/sequence/stop")]
        public void SequenceStop()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                if (!sequence.Initialized)
                {
                    response = CoreUtility.CreateErrorTable(new Error("Sequence is not initialized", 409));
                }
                else
                {
                    sequence.CancelAdvancedSequence();
                    response.Response = "Sequence stopped";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/sequence/load")]
        public void SequenceLoad([QueryField] string sequencename)
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

                // Use either a name of a sequence or let the user upload a json file

                // ISequenceRootContainer container = JsonConvert.DeserializeObject<SequenceRootContainer>(sequenceJson); // Not working
                // sequence.SetAdvancedSequence(container);

                // response.Response = "Sequence updated";
                response.Response = "Not yet implemented";
                response.Success = false;
                response.StatusCode = 501;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }

        [Route(HttpVerbs.Get, "/sequence/list-available")]
        public void SequenceGetAvailable()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpResponse response = new HttpResponse();

            try
            {
                IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                string sequenceFolder = profile.SequenceSettings.DefaultSequenceFolder;

                response.Response = FileSystemHelper.GetFilesRecursively(sequenceFolder);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                response = CoreUtility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR);
            }

            HttpContext.WriteToResponse(response);
        }
    }
}
