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

        internal static void ConvertAnimateTrackProperties(this Dictionary<string, object?> dictionary, TrackTracker tracker,  Dictionary<string, object?>? pointDefinitions)
        {
            List<(float Time, (List<object> Points, object Original) data)> PointsToDict(string name)
            {
                List<(float Time, (List<object> Points, object Original) data)> result = new();
                if (!dictionary.TryGetValue(name, out object? positionObject) || positionObject == null)
                {
                    return result;
                }

                List<object> list;
                if (positionObject is string pointDefinition)
                {
                    pointDefinitions!.TryGetValue(pointDefinition, out object? output);
                    list = (List<object>)output!;
                }
                else
                {
                    list = (List<object>)positionObject;
                }

                if (list.FirstOrDefault() is List<object>)
                {
                    foreach (object rawPoint in list)
                    {
                        List<object> points = ((List<object>)rawPoint).Where(n => n is double).ToList();
                        float time = Convert.ToSingle(points.Last());
                        points.RemoveAt(points.Count - 1);
                        result.Add(new ValueTuple<float, (List<object> Points, object Original)>(time, new ValueTuple<List<object>, object>(points, rawPoint)));
                    }
                }
                else
                {
                    result.Add(new ValueTuple<float, (List<object> Points, object Original)>(0, new ValueTuple<List<object>, object>(list, list)));
                }

                return result;
            }

            HashSet<TrackTracker.TrackType>? trackTypes = tracker.GetTrackType(dictionary);
            if (trackTypes == null)
            {
                return;
            }

            void AddIfExist(string original, string newName)
            {
                if (!dictionary.TryGetValue(original, out object? data))
                {
                    return;
                }

                dictionary[newName] = data;
            }

            if (trackTypes.Contains(TrackTracker.TrackType.Environment))
            {
                AddIfExist("_position", "position");
                AddIfExist("_rotation", "rotation");
                AddIfExist("_localRotation", "localRotation");

                LazyList finalPos = new();
                LazyList finalLocalPos = new();

                List<(float Time, (List<object> Points, object Original) data)> position = PointsToDict("_position");
                List<(float Time, (List<object> Points, object Original) data)> localPosition = PointsToDict("_localPosition");

                foreach ((float _, var (points, original)) in position)
                {
                    finalPos.Add(points.ToVector3() * 0.6f, original);
                }

                foreach ((float _, var (points, original)) in localPosition)
                {
                    finalLocalPos.Add(points.ToVector3() * 0.6f, original);
                }

                finalPos.Save(dictionary, "position");
                finalLocalPos.Save(dictionary, "localPosition");
            }

            if (trackTypes.Contains(TrackTracker.TrackType.Object))
            {
                AddIfExist("_position", "offsetPosition");
                AddIfExist("_rotation", "offsetWorldRotation");
                AddIfExist("_localRotation", "localRotation");
            }

            if (trackTypes.Contains(TrackTracker.TrackType.NoodleEvent))
            {
                LazyList finalPos = new();
                LazyList finalRot = new();

                List<(float Time, (List<object> Points, object Original) data)> position = PointsToDict("_position");
                List<(float Time, (List<object> Points, object Original) data)> rotation = PointsToDict("_rotation");
                List<(float Time, (List<object> Points, object Original) data)> localRotation = PointsToDict("_localRotation");

                foreach ((float key, var (points, original)) in position)
                {
                    WorldRotationToStandard.ConvertPositions(
                        points,
                        rotation.Linqsucksdick(n => n.Time <= key),
                        localRotation.Linqsucksdick(n => n.Time <= key),
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
                        position.Linqsucksdick(n => n.Time <= key),
                        rotation.Linqsucksdick(n => n.Time <= key),
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
                    List<object> result = new(originalList)
                    {
                        [0] = valuevalue.X,
                        [1] = valuevalue.Y,
                        [2] = valuevalue.Z
                    };
                    List.Add(result);
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

        private static List<object>? Linqsucksdick(this List<(float Time, (List<object> Points, object Original) data)> list, Func<(float Time, (List<object> Points, object Original)), bool> action)
        {
            List<(float Time, (List<object> Points, object Original) data)> listReverse = new(list);
            listReverse.Reverse();
            foreach ((float Time, (List<object> Points, object Original)) pair in listReverse)
            {
                if (action(pair))
                {
                    return pair.Item2.Points;
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
        }
    }
}
