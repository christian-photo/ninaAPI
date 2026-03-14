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
using EmbedIO;
using ninaAPI.Utility.Http;

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
            if (pageSize == -1)
                return list;

            if (page < 1 || pageSize < 1)
                return [];

            int start = (page - 1) * pageSize;

            if (start >= list.Count)
                return [];

            int count = Math.Min(pageSize, list.Count - start);

            return list.GetRange(start, count);
        }
    }

    public class PagerParameterSet
    {
        public QueryParameter<int> PageParameter { get; set; }
        public QueryParameter<int> PageSizeParameter { get; set; }

        private PagerParameterSet() { }

        public static PagerParameterSet Default()
        {
            return new PagerParameterSet()
            {
                PageParameter = new QueryParameter<int>("page", 0, false, (page) => page >= 0),
                PageSizeParameter = new QueryParameter<int>("page-size", 20, false, (size) => size >= -1)
            };
        }

        public void Evaluate(IHttpContext context)
        {
            PageParameter.Get(context);
            PageSizeParameter.Get(context);
        }
    }
}
