﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class EnvironmentConverter
    {
        internal static void Convert(Dictionary<string, object?> customData)
        {
            if (!customData.TryGetValue("_environment", out object? environmentObject) || environmentObject == null)
            {
                Console.WriteLine("No \"_environment\" array found.");
                return;
            }

            List<object> environment = (List<object>)environmentObject;
            List<Dictionary<string, object?>> newEnvironment = new();
            foreach (object environmentDefinitionObject in environment)
            {
                try
                {
                    Dictionary<string, object> environmentDefinition = (Dictionary<string, object>)environmentDefinitionObject;
                    Dictionary<string, object?> newEnvironmentDefinition = new()
                    {
                        ["id"] = environmentDefinition["_id"],
                        ["lookupMethod"] = environmentDefinition["_lookupMethod"]
                    };

                    void AddIfExist(string original, string newName)
                    {
                        if (environmentDefinition.TryGetValue(original, out object? data))
                        {
                            newEnvironmentDefinition[newName] = data;
                        }
                    }

                    AddIfExist("_duplicate", "duplicate");
                    AddIfExist("_active", "active");
                    AddIfExist("_scale", "scale");
                    AddIfExist("_rotation", "rotation");
                    AddIfExist("_localRotation", "localRotation");

                    if (environmentDefinition.TryGetValue("_position", out object? positionData))
                    {
                        newEnvironmentDefinition["position"] = ((List<object>)positionData).Select(n => System.Convert.ToDouble(n) * 0.6).ToList();
                    }

                    if (environmentDefinition.TryGetValue("_localPosition", out object? localPositionData))
                    {
                        newEnvironmentDefinition["localPosition"] = ((List<object>)localPositionData).Select(n => System.Convert.ToDouble(n) * 0.6).ToList();
                    }

                    AddIfExist("_lightID", "lightID");
                    AddIfExist("_track", "track");

                    newEnvironment.Add(newEnvironmentDefinition);
                }
                catch (Exception e)
                {
                    Program.ErrorCounter++;
                    Console.WriteLine("Failed converting environment definition:");
                    Console.WriteLine(JsonSerializer.Serialize(environmentDefinitionObject));
                    Console.WriteLine(e);
                    Console.WriteLine();
                }
            }

            customData["environment"] = newEnvironment;
            customData.Remove("_environment");
        }
    }
}
