#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.DataAnnotations;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using ninaAPI.WebService.V3.Model;

namespace ninaAPI.WebService.V3.Application.Sequence
{
    /// <summary>
    /// All properties of this class are required
    /// </summary>
    public class TargetUpdate
    {
        [Required]
        public string TargetName { get; set; }

        [Required]
        public HttpCoordinates Coordinates { get; set; }

        [Required]
        [Range(0, 360)]
        public double Rotation { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int TargetIndex { get; set; }
    }

    public enum SequenceSkipType
    {
        SkipCurrentItems,
        SkipToImaging,
        SkipToEnd,
    }

    public class SequenceTarget
    {
        public InputTarget Target { get; set; }
        public string Name { get; set; }
        public SequenceEntityStatus Status { get; set; }

        public SequenceTarget(IDeepSkyObjectContainer copyMe)
        {
            this.Target = copyMe.Target;
            this.Name = copyMe.Name;
            this.Status = copyMe.Status;
        }
    }
}
