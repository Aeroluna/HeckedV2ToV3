using System;
using System.Collections.Generic;
using System.Linq;

namespace HeckedV2ToV3
{
    internal class TrackTracker
    {
        private readonly Dictionary<string, HashSet<TrackType>> _trackedTracks = new();

        internal enum TrackType
        {
            Environment,
            Object,
            NoodleEvent,
            FogEvent
        }

        internal TrackTracker(IReadOnlyDictionary<string, object?> customData, IReadOnlyDictionary<string, object?> beatmapData)
        {
            try
            {
                if (customData.TryGetValue("_customEvents", out object? customEventsObject) && customEventsObject != null)
                {
                    bool warnedPlayerTrack = false;
                    bool warnedTrackParent = false;
                    foreach (object customEventObject in (List<object>)customEventsObject)
                    {
                        Dictionary<string, object> customEvent = (Dictionary<string, object>)customEventObject;
                        string type = (string)customEvent["_type"];
                        Dictionary<string, object?> data = (Dictionary<string, object?>)customEvent["_data"];
                        switch (type)
                        {
                            case "AssignPlayerToTrack":
                                if (!warnedPlayerTrack)
                                {
                                    warnedPlayerTrack = true;
                                    Console.WriteLine("Attempting to convert [AssignPlayerToTrack] event(s), conversion may be imperfect.");
                                }

                                AddTrack(data, TrackType.NoodleEvent);
                                break;

                            case "AssignTrackParent":
                                if (!warnedTrackParent)
                                {
                                    warnedTrackParent = true;
                                    Console.WriteLine("Attempting to convert [AssignTrackParent] event(s), conversion may be imperfect.");
                                }

                                AddTrack(data, TrackType.NoodleEvent, "_parentTrack");
                                break;

                            case "AssignFogTrack":
                                AddTrack(data, TrackType.FogEvent);
                                break;
                        }
                    }
                }

                if (customData.TryGetValue("_environment", out object? environmentObject) && environmentObject != null)
                {
                    foreach (object environmentDefinitionObject in (List<object>)environmentObject)
                    {
                        AddTrack((Dictionary<string, object?>)environmentDefinitionObject, TrackType.Environment);
                    }
                }

                if (beatmapData.TryGetValue("_notes", out object? notesObject) && notesObject != null)
                {
                    List<object> notes = (List<object>)notesObject;
                    foreach (object noteObject in notes)
                    {
                        if (((Dictionary<string, object>)noteObject).TryGetValue("_customData", out object? customDataObject))
                        {
                            AddTrack((Dictionary<string, object?>)customDataObject, TrackType.Object);
                        }
                    }
                }

                // ReSharper disable once InvertIf
                if (beatmapData.TryGetValue("_obstacles", out object? obtacleObject) && obtacleObject != null)
                {
                    List<object> obstacles = (List<object>)obtacleObject;
                    foreach (object obstacleObject in obstacles)
                    {
                        if (((Dictionary<string, object>)obstacleObject).TryGetValue("_customData", out object? customDataObject))
                        {
                            AddTrack((Dictionary<string, object?>)customDataObject, TrackType.Object);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Program.ErrorCounter++;
                Console.WriteLine("Failed tracking tracks.");
                Console.WriteLine(e);
                Console.WriteLine();
            }
        }

        internal HashSet<TrackType>? GetTrackType(Dictionary<string, object?> dictionary)
        {
            if (!dictionary.TryGetValue("_track", out object? trackName) ||
                trackName == null)
            {
                Console.WriteLine("Could not find track name.");
                return null;
            }

            switch (trackName)
            {
                case List<object> list:
                    IEnumerable<TrackType> result = Enumerable.Empty<TrackType>();
                    list.ForEach(n =>
                    {
                        if (_trackedTracks.TryGetValue((string)n, out HashSet<TrackType>? trackTypes))
                        {
                            result = result.Concat(trackTypes);
                        }
                    });

                    return new HashSet<TrackType>(result);

                case string name:
                    if (_trackedTracks.TryGetValue(name, out HashSet<TrackType>? trackType))
                    {
                        return trackType;
                    }

                    break;
            }

            Console.WriteLine($"Could not find source for track [{trackName}], unused track?");
            return null;
        }

        private void AddTrack(Dictionary<string, object?> data, TrackType trackType, string key = "_track")
        {
            void Add(string name)
            {
                GetHashSet(name).Add(trackType);
            }

            if (!data.TryGetValue(key, out object? trackName) || trackName == null)
            {
                return;
            }

            switch (trackName)
            {
                case List<object> list:
                    list.ForEach(n => Add((string)n));
                    break;

                case string name:
                    Add(name);
                    break;
            }
        }

        private HashSet<TrackType> GetHashSet(string track)
        {
            // ReSharper disable once InvertIf
            if (!_trackedTracks.TryGetValue(track, out HashSet<TrackType>? trackTypes))
            {
                trackTypes = new HashSet<TrackType>();
                _trackedTracks[track] = trackTypes;
            }

            return trackTypes;
        }
    }
}
