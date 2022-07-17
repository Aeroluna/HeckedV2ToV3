using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class BeatmapEventsConverter
    {
        private static readonly float[] _spawnRotations =
        {
            -60f,
            -45f,
            -30f,
            -15f,
            15f,
            30f,
            45f,
            60f
        };

        internal static void Convert(Dictionary<string, object?> src, Dictionary<string, object?> dst)
        {
            if (!src.TryGetValue("_events", out object? eventsObject) || eventsObject == null)
            {
                Console.WriteLine("No \"_events\" array found.");
                return;
            }

            bool warnedGradient = false;

            Dictionary<int, double[]> legacyColors = new();
            List<Dictionary<string, object>> events = ((List<object>)eventsObject).Cast<Dictionary<string, object>>().ToList();
            List<Dictionary<string, object>> bpmEvents = new();
            List<Dictionary<string, object>> rotationEvents = new();
            List<Dictionary<string, object>> basicBeatmapEvents = new();
            List<Dictionary<string, object>> colorBoostBeatmapEvents = new();
            foreach (Dictionary<string, object> basicEvent in events)
            {
                try
                {
                    int type = basicEvent["_type"].ToInt();
                    int value = basicEvent["_value"].ToInt();
                    if (value > 2000000000)
                    {
                        value -= 2000000000;
                        int red = (value >> 16) & 0x0ff;
                        int green = (value >> 8) & 0x0ff;
                        int blue = value & 0x0ff;
                        double[] colorSetter = new double[3];
                        legacyColors[type] = colorSetter;
                        colorSetter[0] = red / 255d;
                        colorSetter[1] = green / 255d;
                        colorSetter[2] = blue / 255d;
                        continue;
                    }

                    Dictionary<string, object> newEvent = new()
                    {
                        ["b"] = basicEvent["_time"]
                    };

                    double floatValue;
                    if (basicEvent.TryGetValue("_floatValue", out object? floatValueObject))
                    {
                        floatValue = (double)floatValueObject;
                    }
                    else
                    {
                        floatValue = 1;
                    }

                    switch (type)
                    {
                        case 5:
                            newEvent["o"] = value == 1;
                            colorBoostBeatmapEvents.Add(newEvent);
                            break;

                        case 14:
                            newEvent["e"] = 0;
                            newEvent["r"] = SpawnRotationForEventValue(value);
                            rotationEvents.Add(newEvent);
                            break;

                        case 15:
                            newEvent["e"] = 1;
                            newEvent["r"] = SpawnRotationForEventValue(value);
                            rotationEvents.Add(newEvent);
                            break;

                        case 100:
                            newEvent["m"] = floatValue;
                            bpmEvents.Add(newEvent);
                            break;

                        default:
                            newEvent["et"] = type;
                            newEvent["i"] = value;
                            newEvent["f"] = floatValue;
                            basicBeatmapEvents.Add(newEvent);
                            break;
                    }

                    // Handle Custom Data
                    Dictionary<string, object?> customData;
                    legacyColors.TryGetValue(type, out double[]? legacyColor);
                    if (basicEvent.TryGetValue("_customData", out object? customDataObject))
                    {
                        customData = (Dictionary<string, object?>)customDataObject;
                    }
                    else if (type is >= 0 and <= 4 && value > 0 && legacyColor != null)
                    {
                        customData = new Dictionary<string, object?>();
                    }
                    else
                    {
                        continue;
                    }

                    if (legacyColor != null && value > 0)
                    {
                        customData["color"] = legacyColor;
                    }

                    switch (type)
                    {
                        case 5:
                        case 14:
                        case 15:
                        case 100:
                            break;

                        case 12:
                        case 13:
                            customData.RenameData("_lockPosition", "lockRotation");
                            customData.RenameData("_speed", "speed");
                            break;

                        case 8:
                            customData.RenameData("_nameFilter", "nameFilter");
                            customData.RenameData("_rotation", "rotation");
                            customData.RenameData("_step", "step");
                            customData.RenameData("_prop", "prop");
                            customData.RenameData("_speed", "speed");
                            customData.RenameData("_direction", "direction");

                            if (customData.TryPopValue("_reset", out object? reset) && ((bool?)reset).GetValueOrDefault())
                            {
                                customData["step"] = 0;
                                customData["prop"] = 50;
                                customData["speed"] = 50;
                            }

                            break;

                        case 9:
                            customData.RenameData("_step", "step");
                            customData.RenameData("_speed", "speed");
                            break;

                        default:
                            customData.RenameData("_lightID", "lightID");
                            customData.RenameData("_color", "color");
                            customData.RenameData("_easing", "easing");
                            customData.RenameData("_lerpType", "lerpType");

                            if (!customData.ContainsKey("color")
                                && customData.TryPopValue("_lightGradient", out object? gradientObject))
                            {
                                if (!warnedGradient)
                                {
                                    warnedGradient = true;
                                    Console.WriteLine("Attempting to convert [_lightGradient](s), conversion may be imperfect.");
                                }

                                Dictionary<string, object> gradient = (Dictionary<string, object>)gradientObject!;
                                customData["color"] = gradient["_startColor"];
                                customData["easing"] = gradient["_easing"];

                                double duration = (double)gradient["_duration"];
                                double endBeat = (double)basicEvent["_time"] + duration;
                                int currentIndex = events.FindIndex(n => n == basicEvent);
                                int nextEventIndex = events
                                    .FindIndex(currentIndex + 1, n => n["_type"].ToInt() == type);
                                if (nextEventIndex != -1)
                                {
                                    Dictionary<string, object> nextEvent = events[nextEventIndex];
                                    double nextEventTime = (double)nextEvent["_time"];
                                    if (endBeat > nextEventTime)
                                    {
                                        endBeat = nextEventTime;
                                    }
                                }

                                newEvent["i"] = 4;

                                basicBeatmapEvents.Add(new Dictionary<string, object>
                                {
                                    ["b"] = endBeat,
                                    ["et"] = type,
                                    ["i"] = 1,
                                    ["f"] = 1,
                                    ["customData"] = new Dictionary<string, object>
                                    {
                                        ["color"] = gradient["_endColor"]
                                    }
                                });
                            }
                            break;
                    }

                    newEvent["customData"] = customData;
                }
                catch (Exception e)
                {
                    Program.ErrorCounter++;
                    Console.WriteLine("Failed converting event:");
                    Console.WriteLine(JsonSerializer.Serialize(basicEvent));
                    Console.WriteLine(e);
                    Console.WriteLine();
                }
            }

            dst["basicBeatmapEvents"] = basicBeatmapEvents.SortByTime();

            if (bpmEvents.Any())
            {
                dst["bpmEvents"] = bpmEvents.SortByTime();
            }

            if (rotationEvents.Any())
            {
                dst["rotationEvents"] = rotationEvents.SortByTime();
            }

            if (colorBoostBeatmapEvents.Any())
            {
                dst["colorBoostBeatmapEvents"] = colorBoostBeatmapEvents.SortByTime();
            }
        }

        private static float SpawnRotationForEventValue(int index)
        {
            if (index >= 0 && index < _spawnRotations.Length)
            {
                return _spawnRotations[index];
            }
            return 0f;
        }
    }
}
