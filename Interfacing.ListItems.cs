using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_Charter {
    internal static partial class Interfacing {

        // -- List Items --

        private static void _listItems()
            => _multiselector_Input(
                "= List Items =",
                "choose an item to list (int): ",
                [
                    ("List curiosities", "lists all curiosities by their locations", _listCuriosities),
                    ("List objects", "lists all objects which are not common objects and their respective location", _listObjects),
                ]
            );

        // - List Curiosities -

        private static void _listCuriosities() {
            // for locations
            foreach (var kvp in DreamLocation.LIST) {
                DreamLocation location = kvp.Value;

                // skip if no curiosities
                if (location.Curiosities.Count == 0) { continue; }
                Console.WriteLine(location.Id.ToUpper());

                // for curiosities
                foreach (Curiosity curiosity in location.Curiosities) {
                    Console.WriteLine($"\t{curiosity
                        .ToString()
                        .Replace("\n", "\t") // adds extra tab spacing
                    }");
                }
            }
        }

        // - List Objects -

        private static void _listObjects() {
            // show a list of objects with duplicate counts and where they are located
            //      while doing this build a list of flagged problem objects which exist in multiple places
            var flaggedObjsWithParents = new List<ObjWithParent[]>();
            foreach ((string objName, List<ObjWithParent> objsWithParents) in DreamLocation.OBJ_PARENT_MAP) {
                // create header and add to flagged
                var header = new StringBuilder(objName.ToUpper());
                if (objsWithParents.Count > 1) {
                    header.Append(" ⚠");
                    flaggedObjsWithParents.Add(objsWithParents.ToArray());
                }

                // write
                Console.WriteLine(header);
                foreach (var objWithParent in objsWithParents) {
                    Console.WriteLine($"\t{DreamPath.SEP_BACK} {objWithParent.ParentName}");
                }
            }
            Console.WriteLine();

            // if flagged objects have been found
            int objsRequiringAction = flaggedObjsWithParents.Count;
            if (objsRequiringAction > 0) {
                // state action required & ask if user wants to take action
                if (objsRequiringAction == 1) {
                    Console.WriteLine($"! 1 object requires action !");
                } else {
                    Console.WriteLine($"! {objsRequiringAction} objects require action !");
                }
                string? takeActionInput = Console.AskForInput("Convert objects to Common Objects (y, n): ")
                    ?.Trim()
                    .ToLower();

                // check if user stated yes
                Console.If_InputYes(takeActionInput, () => {
                    // convert objects
                    _convertCommonObjects(flaggedObjsWithParents.ToArray());

                    // remind user compositions are non-destructive
                    Console.WriteLine();
                    Console.WriteLine("⚠  compositions are non-destructive; objects must be deleted from their parents to fully resolve warnings");
                    Console.WriteLine();
                    Console.AskForInput("...press anything to continue and try to reload");
                    _reload();
                });
                Console.WriteLine();
            }

            // get input for object search
            string? input = Console.AskForInput("Search for an object (objectName): ")
                ?.ToLower();
            Console.WriteLine();

            // search for object
            if (input is not null) {
                // collect all locations the object by the given name is in
                var objLocs = new List<ObjWithParent>();
                foreach ((_, DreamLocation location) in DreamLocation.LIST) {
                    foreach (Object obj in location.Objects) {
                        if (obj.Name.ToLower() == input) {
                            objLocs.Add(new(location.Id, obj));
                        }
                    }
                }

                // if none found
                if (objLocs.Count <= 0) { goto not_found; }

                // show each object with it's location
                Console.WriteLine("! found object⧼s⧽ !");
                Console.WriteLine();
                foreach (ObjWithParent objLoc in objLocs) {
                    Console.Write($"{objLoc.ParentName} {objLoc.Obj.ToString()}");
                }
                return;
            }
        not_found:
            Console.WriteLine("! object could not be found !");
        }

        // - Conversion Helper -

        /// <summary>
        /// Prompts and confirms with the user a list of object conversions 
        /// </summary>
        /// <remarks>
        /// Objects are unvalidated; objects are expected to be validated before they are passed
        /// </remarks>
        /// <param objsWithParentsList="objs"> an array of ⸉location & objects⸉ arrays</param>
        private static void _convertCommonObjects(ObjWithParent[][] objsWithParentsList) {
            // compose each pair
            foreach (ObjWithParent[] objsWithParents in objsWithParentsList) {
                string nameUpper = objsWithParents[0].Obj.Name
                    .ToUpper();

                // show where composition comes from
                Console.WriteLine();
                Console.WriteLine($"- {nameUpper} -");
                Console.WriteLine("...is composed from ⌄");
                foreach (ObjWithParent objWithParent in objsWithParents) {
                    Console.WriteLine($"\t{DreamPath.SEP_BACK} {objWithParent.ParentName}");
                }

                // compose object and show
                Object composedObj = Object.ComposeObjects(objsWithParents
                    .Select(objWithParent => objWithParent.Obj)
                    .ToArray()
                );
                Console.WriteLine(composedObj.ToString());

                // ask user to confirm composed object
                string? inputConfirm = Console.AskForInput("Accept composition as new common object (y, n): ")
                    ?.Trim()
                    .ToLower();

                // yes input
                Console.If_InputYes(inputConfirm, () => _createNewCommon(composedObj));
            }
        }
    }
}
