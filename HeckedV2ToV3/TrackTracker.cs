using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal class TrackTracker
    {
        private readonly Dictionary<string, HashSet<TrackType>> _trackedTracks = new();

        internal enum TrackType
        {
            Environment,
            Object,
            NoodleEvent
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

        internal HashSet<TrackType>? GetTrackType(Dictionary<string, object?> data, string key = "_track")
        {
            if (data.TryGetValue(key, out object? trackName) &&
                trackName != null &&
                _trackedTracks.TryGetValue((string)trackName, out HashSet<TrackType>? trackType))
            {
                return trackType;
            }

            return null;
        }

        private void AddTrack(Dictionary<string, object?> data, TrackType trackType, string key = "_track")
        {
            if (data.TryGetValue(key, out object? trackName) &&
                trackName != null)
            {
                GetHashSet((string)trackName).Add(trackType);
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
