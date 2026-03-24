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
using ninaAPI.WebService.Interfaces;
using SimpleW;

namespace ninaAPI.WebService.V3.Application.Sequence
{
    public class SequenceController : IHttpController
    {
        private readonly ISequenceMediator sequence;
        private readonly ISerializerService serializer;

        public SequenceController(ISequenceMediator sequenceMediator, ISerializerService serializer)
        {
            this.sequence = sequenceMediator;
            this.serializer = serializer;
        }

        // TODO: Improve
        public async Task GetSequence(HttpSession session)
        {
            QueryParameter<bool> baseParameter = new QueryParameter<bool>("base", false, false);
            bool baseSequence = baseParameter.Get(session.Request);

            if (baseSequence)
            {
                var root = sequence.GetSequenceRoot();
                string tempFile = Path.Combine(FileSystemHelper.GetProcessTempFolder(), "sequence.json");
                await sequence.SaveContainer(root, tempFile, session.RequestAborted);
                string json = File.ReadAllText(tempFile);
                File.Delete(tempFile);

                await session.Response.Text(json, contentType: "application/json").SendAsync();
            }
            else
            {
                ISequenceRootContainer root = sequence.GetSequenceRoot();

                await session.Response.Text(serializer.Serialize(root, true), contentType: "application/json").SendAsync();
            }
        }

        // Loads the sequence either from a file (if name is provided) or from the request body
        public StringResponse SetSequence(HttpRequest request)
        {
            QueryParameter<string> nameParameter = new QueryParameter<string>("name", string.Empty, true);
            string name = nameParameter.Get(request);

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
                json = request.BodyString;
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

        public StringResponse EditSequence(SequenceEditBody body)
        {
            Validator.ValidateObject(body, new ValidationContext(body));

            CoreUtility.SetValueReflected(sequence.GetSequenceRoot(), body.PathDescription, body.Value);

            return new StringResponse("Value was updated");
        }

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

        public IReadOnlyCollection<NINA.Sequencer.SequenceItem.ISequenceItem> GetRunningItems()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            return sequence.GetAdvancedSequencerCurrentRunningItems();
        }

        // Automatically stops the sequence
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

        public StringResponse StopSequence()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            sequence.CancelAdvancedSequence();

            return new StringResponse("Sequence stopped");
        }

        public async Task<StringResponse> StartSequence(HttpRequest request)
        {
            QueryParameter<bool> validateParameter = new QueryParameter<bool>("validate", false, false);
            bool validate = validateParameter.Get(request);

            await sequence.StartAdvancedSequence(!validate);

            return new StringResponse("Sequence started");
        }

        public async Task<StringResponse> SetTarget(HttpRequest request, TargetUpdate target)
        {
            Validator.ValidateObject(target, new ValidationContext(target));

            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            var targets = sequence.GetAllTargetsInAdvancedSequence();

            QueryParameter<int> targetIndexParameter = new QueryParameter<int>("target", 0, true, (target) => target.IsBetween(0, targets.Count - 1));
            int targetIndex = targetIndexParameter.Get(request);

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

        public IEnumerable<SequenceTarget> GetTargets()
        {
            if (!sequence.Initialized)
            {
                throw new HttpException(HttpStatusCode.Conflict, "Sequence is not initialized");
            }

            var targets = sequence.GetAllTargetsInAdvancedSequence();
            return targets.Select(x => new SequenceTarget(x));
        }

        public StringResponse SkipSequence(HttpRequest request)
        {
            QueryParameter<SequenceSkipType> typeParameter = new QueryParameter<SequenceSkipType>("type", SequenceSkipType.SkipCurrentItems, true);
            SequenceSkipType type = typeParameter.Get(request);

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

            return new StringResponse("Skipped in sequence");
        }

        public void Configure(SimpleWServer server, string prefix)
        {
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/", (HttpSession session) => GetSequence(session));
            server.Map(HttpVerbs.PUT.ToString(), $"{prefix}/", (HttpRequest request) => SetSequence(request));
            server.Map(HttpVerbs.PATCH.ToString(), $"{prefix}/", (HttpRequest request) => EditSequence(serializer.Deserialize<SequenceEditBody>(request.BodyString)));
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/available", () => GetAvailableSequences());
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/running-items", () => GetRunningItems());
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/reset", () => ResetSequenceProgress());
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/stop", () => StopSequence());
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/start", async (HttpRequest request) => await StartSequence(request));
            server.Map(HttpVerbs.PATCH.ToString(), $"{prefix}/target", async (HttpRequest request) => await SetTarget(request, serializer.Deserialize<TargetUpdate>(request.BodyString)));
            server.Map(HttpVerbs.GET.ToString(), $"{prefix}/target", () => GetTargets());
            server.Map(HttpVerbs.POST.ToString(), $"{prefix}/skip", (HttpRequest request) => SkipSequence(request));
        }
    }
}
