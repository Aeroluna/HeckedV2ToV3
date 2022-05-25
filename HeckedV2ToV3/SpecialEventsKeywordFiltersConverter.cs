using System;
using System.Collections.Generic;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class SpecialEventsKeywordFiltersConverter
    {
        internal static void Convert(Dictionary<string, object?> src, Dictionary<string, object?> dst)
        {
            if (!src.TryGetValue("_specialEventsKeywordFilters", out object? specialEventsKeywordFiltersObject) || specialEventsKeywordFiltersObject == null)
            {
                Console.WriteLine("No \"_specialEventsKeywordFilters\" array found.");
                return;
            }

            try
            {
                List<object> specialEventsForKeywords = (List<object>)((Dictionary<string, object>)specialEventsKeywordFiltersObject)["_keywords"];
                List<object> newSpecialEventsForKeywords = new();

                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (object specialEventsForKeywordObject in specialEventsForKeywords)
                {
                        Dictionary<string, object> specialEventsForKeyword = (Dictionary<string, object>)specialEventsForKeywordObject;
                        Dictionary<string, object> newSpecialEventsForKeyword = new()
                        {
                            ["k"] = specialEventsForKeyword["_keyword"],
                            ["e"] = specialEventsForKeyword["_specialEvents"]
                        };

                        newSpecialEventsForKeywords.Add(newSpecialEventsForKeyword);
                }

                dst["basicEventTypesWithKeywords"] = new Dictionary<string, object>
                {
                    ["d"] = newSpecialEventsForKeywords
                };
            }
            catch (Exception e)
            {
                Program.ErrorCounter++;
                Console.WriteLine("Failed converting specialEventsKeywordFilters:");
                Console.WriteLine(JsonSerializer.Serialize(specialEventsKeywordFiltersObject));
                Console.WriteLine(e);
                Console.WriteLine();
            }
        }
    }
}
