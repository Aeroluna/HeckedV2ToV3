using System;
using System.Collections.Generic;
using System.Linq;

namespace HeckedV2ToV3
{
    internal static class ConverterExtras
    {
        internal static List<Dictionary<string, object>> SortByTime(this List<Dictionary<string, object>> objects)
        {
            return objects.OrderBy(n => (double)n["b"]).ToList();
        }

        internal static int ToInt(this object @double)
        {
            return Convert.ToInt32(@double);
        }

        internal static bool TryPopValue(this Dictionary<string, object?> dictionary, string key, out object? value)
        {
            bool contains = dictionary.TryGetValue(key, out value);
            if (contains)
            {
                dictionary.Remove(key);
            }

            return contains;
        }

        internal static void RenameData(this Dictionary<string, object?> dictionary, string original, string newName)
        {
            if (!dictionary.TryPopValue(original, out object? data))
            {
                return;
            }

            dictionary[newName] = data;
        }

        internal static void ConvertAnimationProperties(this Dictionary<string, object?> dictionary, bool path)
        {
            // chroma/Ne used same name, so have to double up
            if (dictionary.TryPopValue("_position", out object? positionData))
            {
                dictionary["offsetPosition"] = positionData;
                if (!path)
                {
                    dictionary["position"] = positionData;
                }
            }

            if (dictionary.TryPopValue("_rotation", out object? rotationData))
            {
                dictionary["rotation"] = rotationData;
                dictionary["offsetWorldRotation"] = rotationData;
            }

            dictionary.RenameData("_scale", "scale");
            dictionary.RenameData("_localRotation", "localRotation");
            dictionary.RenameData("_dissolve", "dissolve");
            dictionary.RenameData("_dissolveArrow", "dissolveArrow");
            dictionary.RenameData("_time", "time");
            dictionary.RenameData("_interactable", "interactable");
            dictionary.RenameData("_definitePosition", "definitePosition");
            dictionary.RenameData("_color", "color");
            dictionary.RenameData("_localPosition", "localPosition");
            dictionary.RenameData("_attenuation", "attenuation");
            dictionary.RenameData("_offset", "offset");
            dictionary.RenameData("_startY", "startY");
            dictionary.RenameData("_height", "height");
        }
    }
}
