using System;
using System.Collections.Generic;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class PointDefinitionsConverter
    {
        internal static void Convert(Dictionary<string, object?> customData)
        {
            if (!customData.TryGetValue("_pointDefinitions", out object? pointDefinitionsObject) || pointDefinitionsObject == null)
            {
                Console.WriteLine("No \"_pointDefinitions\" array found.");
                return;
            }

            List<object> pointDefinitions = (List<object>)pointDefinitionsObject;
            Dictionary<string, object> newPointDefinitions = new();
            foreach (object pointDefinitionObject in pointDefinitions)
            {
                try
                {
                    Dictionary<string, object> pointDefinition = (Dictionary<string, object>)pointDefinitionObject;
                    newPointDefinitions.Add((string)pointDefinition["_name"], pointDefinition["_points"]);
                }
                catch (Exception e)
                {
                    Program.ErrorCounter++;
                    Console.WriteLine("Failed converting point definition:");
                    Console.WriteLine(JsonSerializer.Serialize(pointDefinitionObject));
                    Console.WriteLine(e);
                    Console.WriteLine();
                }
            }

            customData["pointDefinitions"] = newPointDefinitions;
            customData.Remove("_pointDefinitions");
        }
    }
}
