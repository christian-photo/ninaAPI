#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using SimpleW.Modules;

namespace ninaAPI.WebService.Interfaces
{
    public interface IWebSocket
    {
        public void ConfigureWebSocket(WebSocketOptions options);
    }
}