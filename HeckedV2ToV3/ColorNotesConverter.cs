using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HeckedV2ToV3
{
    internal static class ColorNotesConverter
    {
        internal static void Convert(Dictionary<string, object?> src, Dictionary<string, object?> dst)
        {
            if (!src.TryGetValue("_notes", out object? notesObject) || notesObject == null)
            {
                Console.WriteLine("No \"_notes\" array found.");
                return;
            }

            List<object> notes = (List<object>)notesObject;
            List<Dictionary<string, object>> newNotes = new();
            List<Dictionary<string, object>> newBombs = new();
            List<Dictionary<string, object>> fakeNotes = new();
            List<Dictionary<string, object>> fakeBombs = new();
            foreach (object noteObject in notes)
            {
                try
                {
                    Dictionary<string, object> note = (Dictionary<string, object>)noteObject;
                    int type = note["_type"].ToInt();
                    bool bomb = type == 3;

                    Dictionary<string, object> newNote = new()
                    {
                        ["b"] = note["_time"],
                        ["x"] = note["_lineIndex"],
                        ["y"] = note["_lineLayer"]
                    };

                    if (!bomb)
                    {
                        newNote["c"] = type;
                        newNote["d"] = note["_cutDirection"];
                        newNote["a"] = 0;
                    }

                    // Handle Custom Data
                    bool isFake = false;
                    if (note.TryGetValue("_customData", out object? customDataObject))
                    {
                        Dictionary<string, object?> customData = (Dictionary<string, object?>)customDataObject;

                        if (customData.TryPopValue("_animation", out object? animation) && animation != null)
                        {
                            ((Dictionary<string, object?>)animation).ConvertAnimationProperties();
                            customData["animation"] = animation;
                        }

                        customData.RenameData("_track", "track");

                        if (customData.TryPopValue("_fake", out object? fake))
                        {
                            isFake = ((bool?)fake).GetValueOrDefault();
                        }

                        if (customData.TryPopValue("_cutDirection", out object? cutDirectionObject))
                        {
                            if (!bomb)
                            {
                                double cutDirection = System.Convert.ToDouble(cutDirectionObject);
                                newNote["a"] = cutDirection.ToInt();
                                newNote["d"] = 0;
                            }
                        }

                        if (customData.TryPopValue("_interactable", out object? interactable))
                        {
                            customData["uninteractable"] = !(bool?)interactable;
                        }

                        if (customData.TryPopValue("_disableSpawnEffect", out object? disableSpawnEFfect))
                        {
                            customData["spawnEffect"] = !(bool?)disableSpawnEFfect;
                        }

                        customData.RenameData("_noteJumpStartBeatOffset", "noteJumpStartBeatOffset");
                        customData.RenameData("_noteJumpMovementSpeed", "noteJumpMovementSpeed");
                        customData.RenameData("_flip", "flip");
                        customData.RenameData("_disableNoteGravity", "disableNoteGravity");
                        customData.RenameData("_disableNoteLook", "disableNoteLook");
                        customData.RenameData("_color", "color");
                        customData.RenameData("_localRotation", "localRotation");
                        customData.RenameData("_rotation", "worldRotation");
                        customData.RenameData("_position", "coordinates");

                        newNote["customData"] = customData;
                    }

                    if (isFake)
                    {
                        if (bomb)
                        {
                            fakeBombs.Add(newNote);
                        }
                        else
                        {
                            fakeNotes.Add(newNote);
                        }
                    }
                    else
                    {
                        if (bomb)
                        {
                            newBombs.Add(newNote);
                        }
                        else
                        {
                            newNotes.Add(newNote);
                        }
                    }
                }
                catch (Exception e)
                {
                    Program.ErrorCounter++;
                    Console.WriteLine("Failed converting note:");
                    Console.WriteLine(JsonSerializer.Serialize(noteObject));
                    Console.WriteLine(e);
                    Console.WriteLine();
                }
            }

            dst["colorNotes"] = newNotes.SortByTime();
            dst["bombNotes"] = newBombs.SortByTime();
            if (fakeNotes.Any())
            {
                ((Dictionary<string, object>)dst["customData"]!)["fakeColorNotes"] = fakeNotes.SortByTime();
            }

            if (fakeBombs.Any())
            {
                ((Dictionary<string, object>)dst["customData"]!)["fakeBombNotes"] = fakeBombs.SortByTime();
            }
        }
    }
}
