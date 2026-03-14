#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Mediator;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Utility;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;

namespace ninaAPI.WebService.V3.Application.Sequence
{
    public class SequenceController : WebApiController
    {
        private readonly ISequenceMediator sequence;
        private readonly ResponseHandler responseHandler;

        public SequenceController(ISequenceMediator sequenceMediator, ResponseHandler responseHandler)
        {
            this.sequence = sequenceMediator;
            this.responseHandler = responseHandler;
        }

        // TODO: Improve
        [Route(HttpVerbs.Get, "/")]
        public async Task GetSequence()
        {
            QueryParameter<bool> baseParameter = new QueryParameter<bool>("base", false, false);
            bool baseSequence = baseParameter.Get(HttpContext);

            if (baseSequence)
            {
                var root = sequence.GetSequenceRoot();
                string tempFile = Path.Combine(FileSystemHelper.GetProcessTempFolder(), "sequence.json");
                await sequence.SaveContainer(root, tempFile, CancellationToken);
                await responseHandler.SendRaw(HttpContext, File.ReadAllText(tempFile));
                File.Delete(tempFile);
            }
            else
            {
                ISequenceRootContainer root = sequence.GetSequenceRoot();
                await responseHandler.SendSequence(HttpContext, root);
            }
        }

        // Loads the sequence either from a file (if name is provided) or from the request body
        [Route(HttpVerbs.Put, "/")]
        public async Task SetSequence()
        {
            QueryParameter<string> nameParameter = new QueryParameter<string>("name", string.Empty, true);
            string name = nameParameter.Get(HttpContext);

            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }
            else if (sequence.IsAdvancedSequenceRunning())
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is currently running");
            }

            string json = string.Empty;

            if (nameParameter.WasProvided)
            {
                IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
                string sequenceFolder = profile.SequenceSettings.DefaultSequenceFolder;
                string filepath = Path.Combine(sequenceFolder, name + ".json");

                if (!File.Exists(filepath))
                {
                    throw new HttpException(HttpStatusCode.NotFound, "Sequence was not found");
                }

                json = File.ReadAllText(filepath);
            }
            else
            {
                json = await HttpContext.GetRequestBodyAsStringAsync();
            }

            var mediator = (SequenceMediator)sequence;
            object nav = mediator.GetType().GetField("sequenceNavigation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mediator);
            var factory = (ISequencerFactory)nav.GetType().GetField("factory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(nav);

            var converter = new SequenceJsonConverter(factory);

            ISequenceContainer container = converter.Deserialize(json);

            SequenceRootContainer root;

            if (container is DeepSkyObjectContainer dso)
            {
                root = factory.GetContainer<SequenceRootContainer>();
                root.Name = Loc.Instance["LblAdvancedSequenceTitle"];
                root.Add(factory.GetContainer<StartAreaContainer>());
                var target = factory.GetContainer<TargetAreaContainer>();
                target.Add(dso);
                root.Add(target);
                root.Add(factory.GetContainer<EndAreaContainer>());
            }
            else
            {
                root = (SequenceRootContainer)container;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() => sequence.SetAdvancedSequence(root));

            await responseHandler.SendObject(HttpContext, new StringResponse("Sequence updated"));
        }

        [Route(HttpVerbs.Patch, "/")]
        public async Task EditSequence([JsonData] SequenceEditBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            CoreUtility.SetValueReflected(sequence.GetSequenceRoot(), body.PathDescription, body.Value);

            await responseHandler.SendObject(HttpContext, new StringResponse("Value was updated"));
        }

        [Route(HttpVerbs.Get, "/available")]
        public async Task GetAvailableSequences()
        {
            IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
            string sequenceFolder = profile.SequenceSettings.DefaultSequenceFolder;

            List<string> f = [];

            List<string> files = FileSystemHelper.GetFilesRecursively(sequenceFolder);
            foreach (string file in files)
            {
                if (file.EndsWith(".json"))
                {
                    var cleaned = Path.GetRelativePath(sequenceFolder, file)
                                      .Replace("\\", "/")
                                      .Replace(".json", "");
                    f.Add(cleaned);
                }
            }

            await responseHandler.SendObject(HttpContext, f);
        }

        [Route(HttpVerbs.Get, "/running-items")]
        public async Task GetRunningItems()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            await responseHandler.SendSequence(HttpContext, sequence.GetAdvancedSequencerCurrentRunningItems());
        }

        // Automatically stops the sequence
        [Route(HttpVerbs.Post, "/reset")]
        public async Task ResetSequenceProgress()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            ISequenceRootContainer root = sequence.GetSequenceRoot();
            System.Windows.Application.Current.Dispatcher.Invoke(root.ResetAll);

            await responseHandler.SendObject(HttpContext, new StringResponse("Sequence progress reset"));
        }

        [Route(HttpVerbs.Post, "/stop")]
        public async Task StopSequence()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            sequence.CancelAdvancedSequence();

            await responseHandler.SendObject(HttpContext, new StringResponse("Sequence stopped"));
        }

        [Route(HttpVerbs.Post, "/start")]
        public async Task StartSequence()
        {
            QueryParameter<bool> validateParameter = new QueryParameter<bool>("validate", false, false);
            bool validate = validateParameter.Get(HttpContext);

            await sequence.StartAdvancedSequence(!validate);

            await responseHandler.SendObject(HttpContext, new StringResponse("Sequence started"));
        }

        [Route(HttpVerbs.Patch, "/target")]
        public async Task SetTarget([JsonData] TargetUpdate target)
        {
            Validator.ValidateObject(target, new ValidationContext(target));

            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            var targets = sequence.GetAllTargetsInAdvancedSequence();

            QueryParameter<int> targetIndexParameter = new QueryParameter<int>("target", 0, true, (target) => target.IsBetween(0, targets.Count - 1));
            int targetIndex = targetIndexParameter.Get(HttpContext);

            IDeepSkyObjectContainer container = targets[targetIndex];
            if (target.Coordinates != null)
            {
                container.Target.InputCoordinates.Coordinates = target.Coordinates.ToCoordinates();
            }
            if (target.TargetName != null)
            {
                container.Target.TargetName = target.TargetName;
                container.Name = target.TargetName;
            }
            if (target.PositionAngle != null)
            {
                container.Target.PositionAngle = target.PositionAngle.Value;
            }


            await responseHandler.SendObject(HttpContext, new StringResponse("Target updated"));
        }

        [Route(HttpVerbs.Get, "/target")]
        public async Task GetTargets()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            var targets = sequence.GetAllTargetsInAdvancedSequence();
            await responseHandler.SendObject(HttpContext, targets.Select(x => new SequenceTarget(x)));
        }

        [Route(HttpVerbs.Post, "/skip")]
        public async Task SkipSequence()
        {
            QueryParameter<SequenceSkipType> typeParameter = new QueryParameter<SequenceSkipType>("type", SequenceSkipType.SkipCurrentItems, true);
            SequenceSkipType type = typeParameter.Get(HttpContext);

            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }
            else if (!sequence.IsAdvancedSequenceRunning())
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence not running");
            }

            ISequenceRootContainer root = sequence.GetSequenceRoot();

            switch (type)
            {
                case SequenceSkipType.SkipCurrentItems: root.SkipCurrentRunningItems(); break;
                case SequenceSkipType.SkipToImaging: root.Items[0].Skip(); break;
                case SequenceSkipType.SkipToEnd: root.Items[0].Skip(); root.Items[1].Skip(); break;
            }

            await responseHandler.SendObject(HttpContext, new StringResponse("Skipped in sequence"));
        }
    }
}
