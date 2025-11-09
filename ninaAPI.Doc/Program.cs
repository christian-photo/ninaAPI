#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.Reflection;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Microsoft.OpenApi;
using ninaAPI.Doc.Api.Equipment;

const string BASE_ROUTE = "/v3/api";

var ninAPIAssembly = Assembly.GetAssembly(typeof(ninaAPI.WebService.V3.V3Api))!;

void AddDocumentMetadata(OpenApiDocument doc)
{
    doc.Info = new OpenApiInfo()
    {
        Title = "Advanced API",
        Version = ninAPIAssembly.GetName()?.Version?.ToString(3) ?? "0.0.0",
    };
    doc.Servers = new List<OpenApiServer>()
    {
        new OpenApiServer()
        {
            Url = $"http://localhost:1888/{BASE_ROUTE}",
            Description = "V3 api server",
        }
    };
}

List<Type> FindControllers()
{
    List<Type> controllers = new List<Type>();
    foreach (Type type in ninAPIAssembly.GetTypes())
    {
        if (type.IsPublic && type.IsClass && !type.IsAbstract && type.Namespace.StartsWith("ninaAPI.WebService.V3") && type.BaseType == typeof(WebApiController))
        {
            controllers.Add(type);
        }
    }
    return controllers;
}

List<MethodInfo> GetAndFilterMethods(Type type)
{
    List<MethodInfo> methods = new List<MethodInfo>();
    foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
    {
        if (method.GetCustomAttribute<RouteAttribute>() != null)
        {
            methods.Add(method);
        }
    }
    return methods;
}

List<Type> shouldBeDocumented = FindControllers();

var doc = new OpenApiDocument();
AddDocumentMetadata(doc);

// foreach (Type type in shouldBeDocumented)
// {
//     var endpoints = GetAndFilterMethods(type);
// }

Guider.CompleteDoc(doc);

foreach (OpenApiError error in doc.Validate(ValidationRuleSet.GetDefaultRuleSet()))
{
    Console.WriteLine(error.Message);
}
doc.SerializeAsV31(new OpenApiJsonWriter(Console.Out));