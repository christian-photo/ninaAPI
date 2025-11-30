#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
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
            if (page < 1 || pageSize < 1)
                return [];

            int start = (page - 1) * pageSize;

            if (start >= list.Count)
                return [];

            int count = Math.Min(pageSize, list.Count - start);

            return list.GetRange(start, count);
        }
    }
}
