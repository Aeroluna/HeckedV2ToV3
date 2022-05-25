using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class ObstaclesConverter
    {
        internal static void Convert(Dictionary<string, object?> src, Dictionary<string, object?> dst)
        {
            if (!src.TryGetValue("_obstacles", out object? obstaclesObject) || obstaclesObject == null)
            {
                Console.WriteLine("No \"_obstacles\" array found.");
                return;
            }

            List<object> obstacles = (List<object>)obstaclesObject;
            List<Dictionary<string, object>> newObstacles = new();
            List<Dictionary<string, object>> fakeObstacles = new();
            foreach (object obstacleObject in obstacles)
            {
                try
                {
                    Dictionary<string, object> obstacle = (Dictionary<string, object>)obstacleObject;

                    int type = obstacle["_type"].ToInt();
                    Dictionary<string, object> newObstacle = new()
                    {
                        ["b"] = obstacle["_time"],
                        ["x"] = obstacle["_lineIndex"],
                        ["y"] = type != 1 ? 0 : 2,
                        ["d"] = obstacle["_duration"],
                        ["w"] = obstacle["_width"],
                        ["h"] = type != 1 ? 5 : 3
                    };

                    // Handle Custom Data
                    bool isFake = false;
                    if (obstacle.TryGetValue("_customData", out object? customDataObject))
                    {
                        Dictionary<string, object?> customData = (Dictionary<string, object?>)customDataObject;

                        if (customData.TryPopValue("_animation", out object? animation) && animation != null)
                        {
                            ((Dictionary<string, object?>)animation).ConvertAnimationProperties(true);
                            customData["animation"] = animation;
                        }

                        customData.RenameData("_track", "track");

                        if (customData.TryPopValue("_fake", out object? fake))
                        {
                            isFake = ((bool?)fake).GetValueOrDefault();
                        }

                        if (customData.TryPopValue("_interactable", out object? interactable))
                        {
                            customData["uninteractable"] = !(bool?)interactable;
                        }

                        customData.RenameData("_noteJumpStartBeatOffset", "noteJumpStartBeatOffset");
                        customData.RenameData("_noteJumpMovementSpeed", "noteJumpMovementSpeed");
                        customData.RenameData("_color", "color");
                        customData.RenameData("_scale", "size");
                        customData.RenameData("_localRotation", "localRotation");
                        customData.RenameData("_rotation", "worldRotation");
                        customData.RenameData("_position", "coordinates");

                        newObstacle["customData"] = customData;
                    }

                    if (isFake)
                    {
                        fakeObstacles.Add(newObstacle);
                    }
                    else
                    {
                        newObstacles.Add(newObstacle);
                    }
                }
                catch (Exception e)
                {
                    Program.ErrorCounter++;
                    Console.WriteLine("Failed converting obstacle:");
                    Console.WriteLine(JsonSerializer.Serialize(obstacleObject));
                    Console.WriteLine(e);
                    Console.WriteLine();
                }
            }

            dst["obstacles"] = newObstacles.SortByTime();
            if (fakeObstacles.Any())
            {
                ((Dictionary<string, object>)dst["customData"]!)["fakeObstacles"] = fakeObstacles.SortByTime();
            }
        }
    }
}
