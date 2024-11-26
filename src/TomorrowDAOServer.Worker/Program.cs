using System;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer;

public class Program
{

    public static void Main()
    {
        Type type = typeof(GetProposalListInput);
        // var jsonDocument = new JObject(
        //     new JProperty("code", 2000),
        //     new JProperty("data", GetPropertiesAsJson(typeof(GetProposalListResultDto)))
        // );
        var jsonDocument = new JObject(
            new JProperty("code", 2000),
            new JProperty("data", GetPropertiesAsJson(typeof(GetProposalListResultDto)))
        );
        Console.WriteLine(jsonDocument.ToString(Formatting.Indented));
    }
    
    public static JObject GetPropertiesAsJson(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var json = new JObject();

        foreach (var prop in properties)
        {
            var typeName = prop.PropertyType;

            // Handle nullable and generic types
            if (typeName.IsGenericType && typeName.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                typeName = Nullable.GetUnderlyingType(typeName);
            }

            if (!json.ContainsKey(char.ToLower(type.Name[0]) + type.Name.Substring(1)))
            {
                json.Add(char.ToLower(type.Name[0]) + type.Name.Substring(1), typeName.Name);
            }

            // If the property is a class and not a string, add nested properties
            if (typeName.IsClass && typeName != typeof(string))
            {
                json[prop.Name] = GetPropertiesAsJson(typeName);
            }
        }

        return json;
    }
}