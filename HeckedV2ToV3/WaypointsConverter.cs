using System;
using System.Collections.Generic;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class WaypointsConverter
    {
        internal static void Convert(Dictionary<string, object?> src, Dictionary<string, object?> dst)
        {
            if (!src.TryGetValue("_waypoints", out object? waypointsObject) || waypointsObject == null)
            {
                Console.WriteLine("No \"_waypoints\" array found.");
                return;
            }

            List<object> waypoints = (List<object>)waypointsObject;
            List<Dictionary<string, object>> newWaypoints = new();
            foreach (object waypointObject in waypoints)
            {
                try
                {
                    Dictionary<string, object> waypoint = (Dictionary<string, object>)waypointObject;
                    Dictionary<string, object> newWaypoint = new()
                    {
                        ["b"] = waypoint["_time"],
                        ["x"] = waypoint["_lineIndex"],
                        ["y"] = waypoint["_lineLayer"],
                        ["d"] = waypoint["_offsetDirection"]
                    };

                    newWaypoints.Add(newWaypoint);
                }
                catch (Exception e)
                {
                    Program.ErrorCounter++;
                    Console.WriteLine("Failed converting waypoint:");
                    Console.WriteLine(JsonSerializer.Serialize(waypointObject));
                    Console.WriteLine(e);
                    Console.WriteLine();
                }
            }

            dst["waypoints"] = newWaypoints.SortByTime();
        }
    }
}
