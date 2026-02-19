#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Mediator;
using NINA.Sequencer.Serialization;
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
            ISequenceRootContainer root = sequence.GetSequenceRoot();
            await responseHandler.SendSequence(HttpContext, root);
        }

        [Route(HttpVerbs.Put, "/update")]
        public async Task SetSequence()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }
            else if (sequence.IsAdvancedSequenceRunning())
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is currently running");
            }

            var mediator = (SequenceMediator)sequence;
            object nav = mediator.GetType().GetField("sequenceNavigation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mediator);
            var factory = (ISequencerFactory)nav.GetType().GetField("factory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(nav);

            var converter = new SequenceJsonConverter(factory);

            ISequenceContainer container = converter.Deserialize(await HttpContext.GetRequestBodyAsStringAsync());

            System.Windows.Application.Current.Dispatcher.Invoke(() => sequence.SetAdvancedSequence((SequenceRootContainer)container));

            await responseHandler.SendObject(HttpContext, new StringResponse("Sequence updated"));
        }

        [Route(HttpVerbs.Put, "/load")]
        public async Task LoadSequenceFromFile()
        {
            QueryParameter<string> nameParameter = new QueryParameter<string>("name", string.Empty, true);
            string name = nameParameter.Get(HttpContext);

            IProfile profile = AdvancedAPI.Controls.Profile.ActiveProfile;
            string sequenceFolder = profile.SequenceSettings.DefaultSequenceFolder;
            string filepath = Path.Combine(sequenceFolder, name + ".json");

            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }
            else if (sequence.IsAdvancedSequenceRunning())
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is currently running");
            }
            else if (!File.Exists(filepath))
            {
                throw new HttpException(HttpStatusCode.NotFound, "Sequence was not found");
            }

            var mediator = (SequenceMediator)sequence;
            object nav = mediator.GetType().GetField("sequenceNavigation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mediator);
            ISequencerFactory factory = (ISequencerFactory)nav.GetType().GetField("factory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(nav);

            var converter = new SequenceJsonConverter(factory);

            ISequenceContainer container = converter.Deserialize(File.ReadAllText(filepath));

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

            System.Windows.Application.Current.Dispatcher.Invoke(() => sequence.SetAdvancedSequence((SequenceRootContainer)container));

            await responseHandler.SendObject(HttpContext, new StringResponse("Sequence updated"));
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
            await responseHandler.SendObject(HttpContext, sequence.GetAdvancedSequencerCurrentRunningItems());
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

        [Route(HttpVerbs.Put, "/target")]
        public async Task SetTarget([JsonData] TargetUpdate target)
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            var targets = sequence.GetAllTargetsInAdvancedSequence();
            if (!target.TargetIndex.IsBetween(0, targets.Count - 1))
            {
                throw new HttpException(HttpStatusCode.Conflict, "Target index is out of range, minimum: 0, maximum: " + (targets.Count - 1));
            }

            IDeepSkyObjectContainer container = targets[target.TargetIndex];
            container.Target.InputCoordinates.Coordinates = target.Coordinates.ToCoordinates();
            container.Target.TargetName = target.TargetName;
            container.Target.PositionAngle = target.Rotation;
            container.Name = target.TargetName;

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
            await responseHandler.SendObject(HttpContext, targets);
        }
    }
}
