using System.Drawing.Text;
using System.Reflection;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Microsoft.OpenApi;

const string BASE_ROUTE = "/v3/api";

var ninAPIAssembly = Assembly.GetAssembly(typeof(ninaAPI.WebService.V3.V3Api));

void AddDocumentMetadata(OpenApiDocument doc)
{
    doc.Info = new OpenApiInfo()
    {
        Title = "Advanced API",
        Version = ninAPIAssembly.GetName().Version.ToString(3),
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

foreach (Type type in shouldBeDocumented)
{
    var endpoints = GetAndFilterMethods(type);

    foreach (var endpoint in endpoints)
    {
        RouteAttribute route = endpoint.GetCustomAttribute<RouteAttribute>()!;
        string path = route.Route;
        HttpVerbs method = route.Verb;

        Console.WriteLine($"Enter endpoint {method} {path}");
        var summary = Console.ReadLine();
        var description = Console.ReadLine();
        var parameters = endpoint.GetParameters().Select(p => new OpenApiParameter
        {
            Name = p.Name,
            Required = true,
            Schema = MakeParameterSchema(p.ParameterType.Name)
        }).ToList();

        do
        {
            Console.WriteLine($"Enter parameter name (leave empty to finish)");
            string parameter = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(parameter))
            {
                break;
            }
            Console.WriteLine($"Enter parameter type");
            var parameterType = MakeParameterSchema(Console.ReadLine());
            Console.WriteLine($"Enter parameter description");
            string parameterDescription = Console.ReadLine();
            if (parameterType.Type == JsonSchemaType.Integer || parameterType.Type == JsonSchemaType.Number)
            {
                Console.WriteLine($"Enter parameter minimum");
                string minimum = Console.ReadLine();
                Console.WriteLine($"Enter parameter maximum");
                string maximum = Console.ReadLine();
                parameterType.Minimum = string.IsNullOrEmpty(minimum) ? null : minimum;
                parameterType.Maximum = string.IsNullOrEmpty(maximum) ? null : maximum;
            }
            Console.WriteLine("Is the parameter required? (y/n)");
            string requiredInput = Console.ReadLine();
            bool required = false;
            if ((requiredInput?.ToLower() ?? "n") == "y")
            {
                required = true;
            }
            if (!required)
            {
                Console.WriteLine("Enter default value");
                string defaultValue = Console.ReadLine();
                parameterType.Default = defaultValue;
            }
            parameters.Add(new OpenApiParameter()
            {
                Name = parameter,
                Required = required,
                Schema = parameterType,
                Description = parameterDescription
            });
        } while (true);
    }
}

OpenApiSchema MakeParameterSchema(string typeName)
{
    JsonSchemaType schemaType = typeName switch
    {
        "String" => JsonSchemaType.String,
        "Int32" => JsonSchemaType.Integer,
        "Int16" => JsonSchemaType.Integer,
        "Int64" => JsonSchemaType.Integer,
        "UInt32" => JsonSchemaType.Integer,
        "UInt16" => JsonSchemaType.Integer,
        "UInt64" => JsonSchemaType.Integer,
        "Double" => JsonSchemaType.Number,
        "Boolean" => JsonSchemaType.Boolean,
        "Single" => JsonSchemaType.Number, // Float
        _ => JsonSchemaType.String,
    };
    return new OpenApiSchema()
    {
        Type = schemaType
    };
}