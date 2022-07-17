using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class CustomEventsConverter
    {
        internal static void Convert(Dictionary<string, object?> customData, TrackTracker tracker)
        {
            if (!customData.TryGetValue("_customEvents", out object? customEventsObject) || customEventsObject == null)
            {
                Console.WriteLine("No \"_customEvents\" array found.");
                return;
            }

            Dictionary<string, object?>? pointDefinitions = null;
            if (customData.TryGetValue("pointDefinitions", out object? pointDefinitionsObject) && pointDefinitionsObject != null)
            {
                pointDefinitions = (Dictionary<string, object?>)pointDefinitionsObject;
            }

            List<object> customEvents = (List<object>)customEventsObject;
            List<Dictionary<string, object>> newCustomEvents = new();
            foreach (object customEventObject in customEvents)
            {
                try
                {
                    Dictionary<string, object> customEvent = (Dictionary<string, object>)customEventObject;

                    string type = (string)customEvent["_type"];

                    Dictionary<string, object?> data = (Dictionary<string, object?>)customEvent["_data"];
                    Dictionary<string, object> newCustomEvent = new()
                    {
                        ["b"] = customEvent["_time"],
                        ["t"] = type,
                        ["d"] = data
                    };

                    switch (type)
                    {
                        case "AnimateTrack":
                            HashSet<TrackTracker.TrackType>? trackTypes = tracker.GetTrackType(data);
                            if (trackTypes != null &&
                                trackTypes.Contains(TrackTracker.TrackType.FogEvent))
                            {
                                Dictionary<string, object?> bloomFogEnvironment = new();

                                void AddIfExistComponent(string old, string newName)
                                {
                                    if (data.TryGetValue(old, out object? value))
                                    {
                                        bloomFogEnvironment[newName] = value;
                                    }
                                }

                                AddIfExistComponent("_attenuation", "attenuation");
                                AddIfExistComponent("_offset", "offset");
                                AddIfExistComponent("_startY", "startY");
                                AddIfExistComponent("_height", "height");

                                Dictionary<string, object?> newdata = new()
                                {
                                    ["track"] = data["_track"],
                                    ["BloomFogEnvironment"] = bloomFogEnvironment
                                };

                                newCustomEvent["t"] = "AnimateComponent";
                                newCustomEvent["d"] = newdata;

                                if (data.TryGetValue("_easing", out object? easing))
                                {
                                    newdata["easing"] = easing;
                                }

                                if (data.TryGetValue("_duration", out object? duration))
                                {
                                    newdata["duration"] = duration;
                                }

                                continue;
                            }

                            data.ConvertAnimateTrackProperties(tracker, pointDefinitions);
                            goto AssignPathAnimation;

                        case "AssignPathAnimation":
                            AssignPathAnimation:
                            data.ConvertAnimationProperties();
                            data.RenameData("_easing", "easing");
                            data.RenameData("_duration", "duration");
                            data.RenameData("_track", "track");
                            break;

                        case "AssignTrackParent":
                            data.RenameData("_worldPositionStays", "worldPositionStays");
                            data.RenameData("_childrenTracks", "childrenTracks");
                            data.RenameData("_parentTrack", "parentTrack");
                            data.RenameData("_scale", "scale");
                            break;

                        case "AssignPlayerToTrack":
                            data.RenameData("_track", "track");
                            break;

                        case "AssignFogTrack":
                            if (!customData.TryGetValue("environment", out object? environmentObject) || environmentObject == null)
                            {
                                environmentObject = new List<Dictionary<string, object?>>();
                                customData["environment"] = environmentObject;
                            }

                            ((List<Dictionary<string, object?>>)environmentObject).Add(new Dictionary<string, object?>
                            {
                                ["id"] = ".[0]Environment",
                                ["lookupMethod"] = "EndsWith",
                                ["track"] = data["_track"]!
                            });

                            continue;
                    }

                    if (type is "AssignTrackParent" or "AssignPlayerToTrack")
                    {
                        List<object>? position = data.PopOrDefault<List<object>>("_position");
                        List<object>? rotation = data.PopOrDefault<List<object>>("_rotation");
                        List<object>? localRotation = data.PopOrDefault<List<object>>("_localRotation");
                        WorldRotationToStandard.ConvertPositions(position, rotation, localRotation, out Vector3? convertedPos, out Vector3? convertedRot);
                        if (convertedPos.HasValue)
                        {
                            Vector3 value = convertedPos.Value;
                            data["localPosition"] = new[] {value.X, value.Y, value.Z};
                        }

                        if (convertedRot.HasValue)
                        {
                            Vector3 value = convertedRot.Value;
                            data["localRotation"] = new[] {value.X, value.Y, value.Z};
                        }
                    }

                    newCustomEvents.Add(newCustomEvent);
                }
                catch (Exception e)
                {
                    Program.ErrorCounter++;
                    Console.WriteLine("Failed converting custom event:");
                    Console.WriteLine(JsonSerializer.Serialize(customEventObject));
                    Console.WriteLine(e);
                    Console.WriteLine();
                }
            }

            customData["customEvents"] = newCustomEvents.SortByTime();
            customData.Remove("_customEvents");
        }
    }
}
