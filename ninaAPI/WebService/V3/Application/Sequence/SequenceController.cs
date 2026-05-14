#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Mediator;
using NINA.Sequencer.Serialization;
using ninaAPI.Utility;
using ninaAPI.Utility.Http;
using ninaAPI.Utility.Serialization;
using SimpleW;

namespace ninaAPI.WebService.V3.Application.Sequence
{
    [Route("/v3/api/sequence")]
    public class SequenceController : Controller
    {
        private readonly ISequenceMediator sequence;
        private readonly ISerializerService serializer;

        public SequenceController(ISequenceMediator sequenceMediator, ISerializerService serializer)
        {
            this.sequence = sequenceMediator;
            this.serializer = serializer;
        }

        // TODO: Improve
        [Route("GET", "/")]
        public async Task GetSequence()
        {
            QueryParameter<bool> baseParameter = new QueryParameter<bool>("base", false, false);
            bool baseSequence = baseParameter.Get(Request);

            if (baseSequence)
            {
                var root = sequence.GetSequenceRoot();
                string tempFile = Path.Combine(FileSystemHelper.GetProcessTempFolder(), "sequence.json");
                await sequence.SaveContainer(root, tempFile, Session.RequestAborted);
                string json = File.ReadAllText(tempFile);
                File.Delete(tempFile);

                await Response.Text(json, contentType: "application/json").SendAsync();
            }
            else
            {
                ISequenceRootContainer root = sequence.GetSequenceRoot();

                await Response.Text(serializer.Serialize(root, true), contentType: "application/json").SendAsync();
            }
        }

        // Loads the sequence either from a file (if name is provided) or from the request body
        [Route("PUT", "/")]
        public StringResponse SetSequence()
        {
            QueryParameter<string> nameParameter = new QueryParameter<string>("name", string.Empty, true);
            string name = nameParameter.Get(Request);

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
                json = Request.BodyString;
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

            return new StringResponse("Sequence updated");
        }

        [Route("PATCH", "/")]
        public StringResponse EditSequence(SequenceEditBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            CoreUtility.SetValueReflected(sequence.GetSequenceRoot(), body.PathDescription, body.Value);

            return new StringResponse("Value was updated");
        }

        [Route("GET", "/available")]
        public List<string> GetAvailableSequences()
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

            return f;
        }

        [Route("GET", "/running-items")]
        public IReadOnlyCollection<NINA.Sequencer.SequenceItem.ISequenceItem> GetRunningItems()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            return sequence.GetAdvancedSequencerCurrentRunningItems();
        }

        // Automatically stops the sequence
        [Route("POST", "/reset")]
        public StringResponse ResetSequenceProgress()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            ISequenceRootContainer root = sequence.GetSequenceRoot();
            System.Windows.Application.Current.Dispatcher.Invoke(root.ResetAll);

            return new StringResponse("Sequence progress reset");
        }

        [Route("POST", "/stop")]
        public StringResponse StopSequence()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            sequence.CancelAdvancedSequence();

            return new StringResponse("Sequence stopped");
        }

        [Route("POST", "/start")]
        public async Task<StringResponse> StartSequence()
        {
            QueryParameter<bool> validateParameter = new QueryParameter<bool>("validate", false, false);
            bool validate = validateParameter.Get(Request);

            await sequence.StartAdvancedSequence(!validate);

            return new StringResponse("Sequence started");
        }

        [Route("PATCH", "/target")]
        public async Task<StringResponse> SetTarget()
        {
            TargetUpdate target = serializer.Deserialize<TargetUpdate>(Request.BodyString);
            Validator.ValidateObject(target, new ValidationContext(target));

            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            var targets = sequence.GetAllTargetsInAdvancedSequence();

            QueryParameter<int> targetIndexParameter = new QueryParameter<int>("target", 0, true, (target) => target.IsBetween(0, targets.Count - 1));
            int targetIndex = targetIndexParameter.Get(Request);

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


            return new StringResponse("Target updated");
        }

        [Route("GET", "/target")]
        public IEnumerable<SequenceTarget> GetTargets()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            var targets = sequence.GetAllTargetsInAdvancedSequence();
            return targets.Select(x => new SequenceTarget(x));
        }

        [Route("POST", "/skip")]
        public StringResponse SkipSequence()
        {
            SequenceSkipModel skipModel = serializer.Deserialize<SequenceSkipModel>(Request.BodyString);
            Validator.ValidateObject(skipModel, new ValidationContext(skipModel));

            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }
            else if (!sequence.IsAdvancedSequenceRunning())
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence not running");
            }

            ISequenceRootContainer root = sequence.GetSequenceRoot();

            switch (skipModel.SkipType)
            {
                case SequenceSkipType.SkipCurrentItems: root.SkipCurrentRunningItems(); break;
                case SequenceSkipType.SkipToImaging: root.Items[0].Skip(); break;
                case SequenceSkipType.SkipToEnd: root.Items[0].Skip(); root.Items[1].Skip(); break;
            }

            return new StringResponse("Skipped in sequence");
        }
    }
}
