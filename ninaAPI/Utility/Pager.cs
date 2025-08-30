#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Collections.Generic;

namespace ninaAPI.Utility
{
    public class Pager<T>
    {
        private readonly List<T> list;

        public Pager(List<T> list)
        {
            this.list = list;
        }

        public List<T> GetPage(int page, int pageSize)
        {
            var start = (page - 1) * pageSize;
            var end = start + pageSize;
            return list.GetRange(start, end);
        }
    }
}