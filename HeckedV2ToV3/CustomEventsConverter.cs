using System;
using System.Collections.Generic;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class CustomEventsConverter
    {
        internal static void Convert(Dictionary<string, object?> customData)
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
                            data.ConvertAnimationProperties(type == "AssignPathAnimation");
                            data.RenameData("_easing", "easing");
                            data.RenameData("_duration", "duration");
                            data.RenameData("_track", "track");
                            break;

                        case "AssignTrackParent":
                            data.RenameData("_worldPositionStays", "worldPositionStays");
                            data.RenameData("_childrenTracks", "childrenTracks");
                            data.RenameData("_parentTrack", "parentTrack");
                            data.RenameData("_position", "offsetPosition");
                            data.RenameData("_rotation", "worldRotation");
                            data.RenameData("_localRotation", "localRotation");
                            data.RenameData("_scale", "scale");
                            break;

                        case "AssignPlayerToTrack":
                        case "AssignFogTrack":
                            data.RenameData("_track", "track");
                            break;
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
