#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using Microsoft.OpenApi;
using ninaAPI.Utility;

namespace ninaAPI.Doc
{
    public static class CommonSchemas
    {
        public static OpenApiSchema ProcessStartedSuccessSchema()
        {
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>()
                {
                    ["Status"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Description = "The status of the process",
                        Example = "Started"
                    },
                    ["ProcessId"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Description = "The id of the process. You can use this to check the status, stop the process, or wait for it to finish",
                        Example = "29438d71-16a7-45de-9763-91b9c60248cf"
                    }
                },
            };
            return schema;
        }

        public static OpenApiSchema ParameterInvalidSchema(string parameter = "x")
        {
            return new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>()
                {
                    ["Error"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Description = "The error message",
                        Example = "Bad Request"
                    },
                    ["Message"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Description = "The error message",
                        Example = $"Parameter {parameter} is invalid"
                    },
                },
            };
        }

        public static OpenApiSchema DeviceNotConnectedSchema(Device device)
        {
            return new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>()
                {
                    ["Error"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Description = "The error message",
                        Example = "Conflict"
                    },
                    ["Message"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Description = "The error message",
                        Example = $"{device} is not connected"
                    },
                },
            };
        }

        public static OpenApiSchema ProcessConflictsSchema()
        {
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>()
                {
                    ["Error"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Description = "The error message",
                        Example = "Conflict"
                    },
                    ["Message"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Description = "The error message",
                        Example = "Process <process id> (<process type>) could not be started because other processes conflict with it"
                    },
                    ["Conflicts"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.Array,
                        Items = ApiProcessSchema(),
                        Description = "A list of conflicting processes"
                    }
                },
            };
            return schema;
        }

        public static OpenApiSchema ApiProcessSchema()
        {
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>()
                {
                    ["ProcessId"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Format = "guid",
                        Description = "The id of the process. You can use this to check the status, stop the process, or wait for it to finish",
                        Example = "29438d71-16a7-45de-9763-91b9c60248cf"
                    },
                    ["ProcessType"] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>()
                        {
                            ["Name"] = new OpenApiSchema()
                            {
                                Type = JsonSchemaType.String,
                                Description = "The name of the process, e.g. CameraCool",
                                Example = "CameraCool"
                            },
                        }
                    }
                }
            };

            return schema;
        }
    }
}