#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


namespace ninaApi.Utility.Serialization
{
    public interface ISerializerService
    {
        public string Serialize(object obj, bool isSequence = false);
        public T Deserialize<T>(string json);
    }
}