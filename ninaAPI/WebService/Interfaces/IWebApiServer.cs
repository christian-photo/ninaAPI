#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ninaAPI.WebService.Interfaces
{
    public interface IWebApiServer
    {
        public Task Start(ServiceProvider provider, params IHttpApi[] apis);
        public Task Stop();

        public bool IsRunning();

        public event EventHandler<EventArgs> Started;
        public event EventHandler<EventArgs> Stopped;
    }
}