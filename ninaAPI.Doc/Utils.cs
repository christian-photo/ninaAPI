#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using Microsoft.OpenApi;

namespace ninaAPI.Doc
{
    public static class Utils
    {
        public static ISet<OpenApiTagReference> MakeTags(params string[] tags)
        {
            return new HashSet<OpenApiTagReference>(tags.Select(t => new OpenApiTagReference(t)));
        }

        public static int GetPublicPropertyCount(this Type type)
        {
            return type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Length;
        }

        public static void ValidateSchemaPropertyCount(OpenApiSchema schema, Type type)
        {
            if (schema.Properties.Count != type.GetPublicPropertyCount())
            {
                throw new Exception($"Schema has {schema.Properties.Count} properties, but type has {type.GetPublicPropertyCount()} properties");
            }
        }
    }
}