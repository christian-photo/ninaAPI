#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using Microsoft.OpenApi;
using NINA.Equipment.Equipment.MyGuider;
using ninaAPI.WebService.V3.Equipment.Guider;

namespace ninaAPI.Doc.Api.Equipment
{
    public static class Guider
    {
        public static void CompleteDoc(OpenApiDocument doc)
        {
            doc.Paths.Add("/equipment/guider/", Info());
            doc.Paths.Add("/equipment/guider/guiding/start", GuidingStart());
        }

        public static OpenApiPathItem Info()
        {
            OpenApiPathItem item = new OpenApiPathItem();

            item.Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Get] = new OpenApiOperation()
                {
                    Tags = Utils.MakeTags(nameof(Guider)),
                    Summary = "Guider Info",
                    Description = "Returns basic information about the guider. For the guide graph, see guiding/graph",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Guider info",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = GuiderInfoSchema()
                                }
                            }
                        }
                    }
                }
            };

            return item;
        }

        public static OpenApiPathItem GuidingStart()
        {
            OpenApiPathItem item = new OpenApiPathItem();

            item.Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Post] = new OpenApiOperation()
                {
                    Tags = Utils.MakeTags(nameof(Guider)),
                    Summary = "Start Guiding",
                    Description = "Starts guiding. The guider has to be connected. If forceCalibration is true, the guider will calibrate before guiding, otherwise it will use the last calibration available, but it may need to calibrate anyway.",
                    Parameters = [
                        new OpenApiParameter()
                        {
                            Name = "forceCalibration",
                            In = ParameterLocation.Query,
                            Required = false,
                            Description = "If true, the guider will force a calibration before guiding",
                            Schema = new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Boolean,
                                Default = false
                            }
                        }
                    ],
                    Responses = new OpenApiResponses()
                    {
                        ["200"] = new OpenApiResponse()
                        {
                            Description = "The status of the process",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = CommonSchemas.ProcessStartedSuccessSchema()
                                }
                            }
                        },
                        ["400"] = new OpenApiResponse()
                        {
                            Description = "Parameter was invalid. This means, forceCalibration was not a boolean",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = CommonSchemas.ParameterInvalidSchema("forceCalibration")
                                }
                            }
                        },
                        ["409"] = new OpenApiResponse()
                        {
                            Description = "The guider is not connected",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = CommonSchemas.DeviceNotConnectedSchema(Utility.Device.Guider)
                                }
                            }
                        },
                    }
                }
            };

            return item;
        }

        private static OpenApiSchema GuiderInfoSchema()
        {
            var info = new GuiderInfoResponse();

            var schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = Device.BuildDeviceInfoSchema(
                    (nameof(info.CanClearCalibration), new OpenApiSchema()
                    {
                        Type = JsonSchemaType.Boolean
                    }),
                    (nameof(info.CanSetShiftRate), new OpenApiSchema()
                    {
                        Type = JsonSchemaType.Boolean
                    }),
                    (nameof(info.CanGetLockPosition), new OpenApiSchema()
                    {
                        Type = JsonSchemaType.Boolean
                    }),
                    (nameof(info.SupportedActions), new OpenApiSchema()
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema()
                        {
                            Type = JsonSchemaType.String
                        }
                    }),
                    (nameof(info.RMSError), RMSErrorSchema()),
                    (nameof(info.PixelScale), new OpenApiSchema()
                    {
                        Type = JsonSchemaType.Number,
                        Description = "The pixel scale of the guider",
                        Minimum = "0",
                    }),
                    (nameof(info.State), new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Description = "The current state of the guider"
                    })
                ),
                Required = new HashSet<string>([
                    ..Device.DeviceInfoRequired(),
                    nameof(info.CanClearCalibration),
                    nameof(info.CanSetShiftRate),
                    nameof(info.CanGetLockPosition),
                    nameof(info.PixelScale)
                ])
            };

            Utils.ValidateSchemaPropertyCount(schema, info.GetType());

            return schema;
        }

        private static IOpenApiSchema RMSErrorSchema(string additionalDescription = "")
        {
            var rms = new RMSError();
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>()
                {
                    [nameof(rms.RA)] = RMSUnitSchema(),
                    [nameof(rms.Dec)] = RMSUnitSchema(),
                    [nameof(rms.Total)] = RMSUnitSchema(),
                    [nameof(rms.PeakRA)] = RMSUnitSchema(),
                    [nameof(rms.PeakDec)] = RMSUnitSchema(),
                },
                Required = new HashSet<string>([nameof(rms.RA), nameof(rms.Dec), nameof(rms.Total), nameof(rms.PeakRA), nameof(rms.PeakDec)]),
                Description = additionalDescription
            };

            Utils.ValidateSchemaPropertyCount(schema, rms.GetType());
            return schema;
        }

        private static IOpenApiSchema RMSUnitSchema(string additionalDescription = "")
        {
            var unit = new RMSUnit(0, 0);
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>()
                {
                    [nameof(unit.Pixel)] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.Number,
                        Description = "The root mean square error in pixels"
                    },
                    [nameof(unit.Arcseconds)] = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.Number,
                        Description = "The root mean square error in arcseconds"
                    },
                },
                Required = new HashSet<string>([nameof(unit.Pixel), nameof(unit.Arcseconds)]),
                Description = additionalDescription
            };

            Utils.ValidateSchemaPropertyCount(schema, unit.GetType());
            return schema;
        }
    }
}