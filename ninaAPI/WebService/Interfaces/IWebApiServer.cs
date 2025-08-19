#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;

namespace ninaAPI.WebService.Interfaces
{
    public interface IWebApiServer
    {
        public void Start(params IHttpApi[] apis);
        public void Stop();

        public bool IsRunning();

        public event EventHandler<EventArgs> Started;
        public event EventHandler<EventArgs> Stopped;
    }
}