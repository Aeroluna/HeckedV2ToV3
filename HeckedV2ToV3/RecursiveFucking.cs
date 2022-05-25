using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class RecursiveFucking
    {
        // System.Text.Json < Newtonsoft.Json
        internal static object? Fuck(JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Array => jsonElement.EnumerateArray().Select(Fuck).ToList(),
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Number => jsonElement.GetDouble(),
                JsonValueKind.Object => Fuck(jsonElement.Deserialize<Dictionary<string, JsonElement>>() ?? throw new InvalidOperationException()),
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.Undefined =>  throw new InvalidOperationException("Unidentifiable type"),
                _ => throw new InvalidOperationException("Unidentifiable type")
            };
        }

        internal static Dictionary<string, object?> Fuck(Dictionary<string, JsonElement> jsonDictionary)
        {
            Dictionary<string, object?> result = new();
            foreach ((string key, JsonElement value) in jsonDictionary)
            {
                result[key] = Fuck(value);
            }

            return result;
        }
    }
}
