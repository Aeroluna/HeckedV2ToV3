using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

        internal static T? PopOrDefault<T>(this Dictionary<string, object?> dictionary, string key)
        {
            if (dictionary.TryPopValue(key, out object? value))
            {
                return (T?)value;
            }

            return default;
        }

        internal static void RenameData(this Dictionary<string, object?> dictionary, string original, string newName)
        {
            if (!dictionary.TryPopValue(original, out object? data))
            {
                return;
            }

            dictionary[newName] = data;
        }

        internal static void ConvertAnimateTrackProperties(this Dictionary<string, object?> dictionary, TrackTracker tracker)
        {
            HashSet<TrackTracker.TrackType>? trackTypes = tracker.GetTrackType(dictionary);

            if (!dictionary.TryGetValue("_track", out object? trackName) ||
                trackName == null)
            {
                Console.WriteLine("Could not find track name.");
                return;
            }

            if (trackTypes == null)
            {
                Console.WriteLine($"Could not find source for track [{trackName}], unused track?");
                return;
            }


            void AddIfExist(string original, string newName, bool conflict = false)
            {
                if (dictionary.TryGetValue(original, out object? data))
                {
                    if (conflict)
                    {
                        if (dictionary.ContainsKey(newName))
                        {
                            throw new InvalidOperationException($"Found conflict for [{newName}] property.");
                        }
                    }

                    dictionary[newName] = data;
                }
            }

            if (trackTypes.Contains(TrackTracker.TrackType.Environment))
            {
                AddIfExist("_position", "position");
                AddIfExist("_rotation", "rotation");
                AddIfExist("_localRotation", "localRotation");
            }

            if (trackTypes.Contains(TrackTracker.TrackType.Object))
            {
                AddIfExist("_position", "offsetPosition");
                AddIfExist("_rotation", "offsetWorldRotation");
                AddIfExist("_localRotation", "localRotation");
            }

            if (trackTypes.Contains(TrackTracker.TrackType.NoodleEvent))
            {
                SortedDictionary<float, (List<object> Points, object Original)> PointsToDict(string name)
                {
                    SortedDictionary<float, (List<object> Points, object Original)> result = new();
                    if (!dictionary.TryGetValue(name, out object? positionObject) || positionObject == null)
                    {
                        return result;
                    }

                    List<object> list = (List<object>)positionObject;
                    foreach (object rawPoint in list)
                    {
                        if (rawPoint is List<object> pointsRaw)
                        {
                            List<object> points = pointsRaw.Where(n => n is double).ToList();
                            float time = Convert.ToSingle(points.Last());
                            points.RemoveAt(points.Count - 1);
                            result.Add(time, new ValueTuple<List<object>, object>(points, rawPoint));
                        }
                        else
                        {
                            result.Add(0, new ValueTuple<List<object>, object>(new List<object> { rawPoint }, rawPoint));
                        }
                    }

                    return result;
                }

                LazyList finalPos = new();
                LazyList finalRot = new();

                SortedDictionary<float, (List<object> Points, object Original)> position = PointsToDict("_position");
                SortedDictionary<float, (List<object> Points, object Original)> rotation = PointsToDict("_rotation");
                SortedDictionary<float, (List<object> Points, object Original)> localRotation = PointsToDict("_localRotation");

                foreach ((float key, var (points, original)) in position)
                {
                    WorldRotationToStandard.ConvertPositions(
                        points,
                        rotation.Linqsucksdick(n => n.Key <= key),
                        localRotation.Linqsucksdick(n => n.Key <= key),
                        out Vector3? finalLocalPosition,
                        out _);
                    finalPos.Add(finalLocalPosition, original);
                }

                // idk im gonna cry
                /*foreach ((float key, var (points, original)) in rotation)
                {
                    WorldRotationToStandard.ConvertPositions(
                        position.LINQSUCKSDICK(n => n.Key <= key),
                        points,
                        localRotation.LINQSUCKSDICK(n => n.Key <= key),
                        out Vector3? finalLocalPosition,
                        out _);
                    finalRot.Add(finalLocalPosition, original);
                }*/

                foreach ((float key, var (points, original)) in rotation)
                {
                    WorldRotationToStandard.ConvertPositions(
                        position.Linqsucksdick(n => n.Key <= key),
                        rotation.Linqsucksdick(n => n.Key <= key),
                        points,
                        out Vector3? finalLocalPosition,
                        out _);
                    finalRot.Add(finalLocalPosition, original);
                }

                finalPos.Save(dictionary, "localPosition");
                finalRot.Save(dictionary, "localRotation");
            }

            dictionary.Remove("_position");
            dictionary.Remove("_rotation");
            dictionary.Remove("_localPosition");
        }

        private class LazyList
        {
            private List<object>? _list;

            private List<object> List
            {
                get { return _list ??= new List<object>(); }
            }

            internal void Add(Vector3? value, object original)
            {
                if (value == null) return;
                Vector3 valuevalue = value.Value;
                if (original is List<object> originalList)
                {
                    originalList[0] = valuevalue.X;
                    originalList[1] = valuevalue.Y;
                    originalList[2] = valuevalue.Z;
                    List.Add(original);
                }
                else
                {
                    List.Add(valuevalue);
                }
            }

            internal void Save(Dictionary<string, object?> dictionary, string name)
            {
                if (_list != null)
                {
                    dictionary[name] = _list;
                }
            }
        }

        private static List<object>? Linqsucksdick(this SortedDictionary<float, (List<object> Points, object Original)> dictionary, Func<KeyValuePair<float, (List<object> Points, object Original)>, bool> action)
        {
            foreach (KeyValuePair<float, (List<object> Points, object Original)> pair in dictionary.Reverse())
            {
                if (action(pair))
                {
                    return pair.Value.Points;
                }
            }

            return null;
        }

        internal static void ConvertAnimationProperties(this Dictionary<string, object?> dictionary)
        {
            dictionary.RenameData("_position", "offsetPosition");
            dictionary.RenameData("_rotation", "offsetWorldRotation");
            dictionary.RenameData("_localRotation", "localRotation");

            dictionary.RenameData("_scale", "scale");
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
