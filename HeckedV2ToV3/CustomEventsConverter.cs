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
                        case "AssignPathAnimation":
                            if (type == "AnimateTrack")
                            {
                                data.ConvertAnimateTrackProperties(tracker);
                            }
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
                        case "AssignFogTrack":
                            data.RenameData("_track", "track");
                            break;
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
