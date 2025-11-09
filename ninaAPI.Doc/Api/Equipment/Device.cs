#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using Microsoft.OpenApi;
using NINA.Equipment.Equipment;

namespace ninaAPI.Doc.Api.Equipment
{
    public static class Device
    {
        public static Dictionary<string, IOpenApiSchema> DeviceInfoSchema()
        {
            var info = DeviceInfo.CreateDefaultInstance<DeviceInfo>();
            var dict = new Dictionary<string, IOpenApiSchema>()
            {
                [nameof(info.Connected)] = new OpenApiSchema()
                {
                    Type = JsonSchemaType.Boolean,
                },
                [nameof(info.Name)] = new OpenApiSchema()
                {
                    Type = JsonSchemaType.String
                },
                [nameof(info.DisplayName)] = new OpenApiSchema()
                {
                    Type = JsonSchemaType.String
                },
                [nameof(info.Description)] = new OpenApiSchema()
                {
                    Type = JsonSchemaType.String
                },
                [nameof(info.DriverInfo)] = new OpenApiSchema()
                {
                    Type = JsonSchemaType.String
                },
                [nameof(info.DriverVersion)] = new OpenApiSchema()
                {
                    Type = JsonSchemaType.String
                },
                [nameof(info.DeviceId)] = new OpenApiSchema()
                {
                    Type = JsonSchemaType.String
                }
            };

            if (typeof(DeviceInfo).GetPublicPropertyCount() != dict.Count)
            {
                throw new Exception("DeviceInfo schema is incomplete");
            }

            return dict;
        }

        public static Dictionary<string, IOpenApiSchema> BuildDeviceInfoSchema(params (string, IOpenApiSchema)[] schemas)
        {
            var dict = DeviceInfoSchema();
            foreach ((string key, IOpenApiSchema schema) in schemas)
            {
                dict[key] = schema;
            }
            return dict;
        }

        public static List<string> DeviceInfoRequired()
        {
            var info = DeviceInfo.CreateDefaultInstance<DeviceInfo>();
            return
            [
                nameof(info.Connected),
            ];
        }
    }
}