using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace HeckedV2ToV3
{
    internal static class Program
    {
        private static readonly Version _version260 = new("2.6.0");

        public static int ErrorCounter { get; set; }

        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (args.Length > 0)
            {
                int finishedFiles = 0;
                foreach (string dat in args)
                {
                    ErrorCounter = 0;
                    string file = Path.GetFileName(dat);
                    Dictionary<string, JsonElement> beatmapDataElements;
                    try
                    {
                        beatmapDataElements = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(dat))
                                              ?? throw new InvalidOperationException("Returned null.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[{file}] is not a valid JSON, skipping.");
                        Console.WriteLine(e);
                        Console.WriteLine();
                        continue;
                    }

                    Console.WriteLine($"Deserializing [{file}].");

                    Dictionary<string, object?> beatmapData = RecursiveFucking.Fuck(beatmapDataElements);

                    if (!beatmapData.TryGetValue("_version", out object? version)
                        && !beatmapData.TryGetValue("version", out version))
                    {
                        // fallback
                        version = "2.0.0";
                    }

                    if (new Version((string)version!).CompareTo(_version260) > 0)
                    {
                        Console.WriteLine($"[{file}] was not version 2.6.0 or lower, skipping.\n");
                        continue;
                    }

                    Dictionary<string, object?> customData;
                    if (beatmapData.TryGetValue("_customData", out object? customDataObject) && customDataObject != null)
                    {
                        customData = (Dictionary<string, object?>)customDataObject;
                    }
                    else
                    {
                        customData = new Dictionary<string, object?>();
                    }

                    TrackTracker tracker = new(customData, beatmapData);
                    CustomEventsConverter.Convert(customData, tracker);
                    PointDefinitionsConverter.Convert(customData);
                    EnvironmentConverter.Convert(customData);

                    Dictionary<string, object?> newBeatmapData = new()
                    {
                        ["version"] = "3.0.0",
                        ["customData"] = customData,
                        ["useNormalEventsAsCompatibleEvents"] = true
                    };
                    ColorNotesConverter.Convert(beatmapData, newBeatmapData);
                    ObstaclesConverter.Convert(beatmapData, newBeatmapData);
                    BeatmapEventsConverter.Convert(beatmapData, newBeatmapData);
                    WaypointsConverter.Convert(beatmapData, newBeatmapData);
                    SpecialEventsKeywordFiltersConverter.Convert(beatmapData, newBeatmapData);

                    if (ErrorCounter > 0)
                    {
                        Console.WriteLine($"Encountered [{ErrorCounter}] errors while converting [{file}], skipping writing.\n");
                    }
                    else
                    {
                        try
                        {
                            string path = Path.GetDirectoryName(dat) ??
                                          throw new ArgumentNullException(nameof(args), $"A string in [{nameof(args)}] was null.");
                            file = file.Insert(file.LastIndexOf(".", StringComparison.Ordinal), "_new");
                            JsonSerializerOptions options = new();
                            options.Converters.Add(new TruncatedDoubleConverter());
                            File.WriteAllText($"{path}\\{file}", JsonSerializer.Serialize(newBeatmapData, options));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Exception writing [{file}].");
                            Console.WriteLine(e);
                            Console.WriteLine();
                            continue;
                        }

                        finishedFiles++;
                        Console.WriteLine($"Created file: [{file}]\n");
                    }
                }

                Console.WriteLine(finishedFiles > 0 ? $"Successfully converted {finishedFiles} maps." : "No files written to.");
            }
            else
            {
                Console.WriteLine("No arguments detected.");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey(true);
        }
    }
}
