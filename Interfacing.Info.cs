using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_Charter {
    internal static partial class Interfacing {

        // -- Info --

        private static void _displayInfos()
            => _multiselector_Input(
                "= Display Info =",
                "choose an info to display (int): ",
                [
                    ("Display location info", "displays the info for a location by name", _locationInfo),
                    ("Display common object info", "displays the info for a common object by name", _objectInfo),
                ]
            );

        // - Location Info -

        private static void _locationInfo() {
            // error if no locations
            if (DreamLocation.LIST.Count == 0) {
                Console.WriteLine("there are no locations to display");
                return;
            }

            // show list of common locations
            DreamLocation.PrintListIds();
            Console.WriteLine();

            // get input and display
            string? input = Console.AskForInput("pick a location (locationId): ");
            if (input is null) {
                Console.WriteLine("invalid locationId");
            } else if (DreamLocation.LIST.ContainsKey(input)) {
                Console.WriteLine(DreamLocation.LIST[input].ToString());

                // show backpaths
                List<string> sources = DreamLocation.GRAPH.Edges
                    .Where(edge => edge.Target == input)
                    .Select(edge => edge.Source)
                    .ToList();
                foreach (string source in sources) {
                    Console.WriteLine($"{DreamPath.SEP_BACK} {source}");
                }
            } else {
                Console.WriteLine("invalid locationId");
            }
        }

        // - Object Info -

        private static void _objectInfo() {
            // error if no locations
            if (DreamObject.LIST.Count == 0) {
                Console.WriteLine("there are no objects to display");
                return;
            }

            // show list of common objects
            DreamObject.PrintListIds();
            Console.WriteLine();

            // get input and display
            string? input = Console.AskForInput("pick an object (objectId): ")
                ?.Trim();
            if (input is null) { Console.WriteLine("invalid objectId"); } else if (DreamObject.LIST.ContainsKey(input)) {
                // show object
                Console.WriteLine(DreamObject.LIST[input].ToString());

                // show backpaths
                foreach (string parent in DreamLocation.COMMON_OBJ_PARENT_MAP[input]
                    .Select(objWithParent => objWithParent.ParentName)) {
                    Console.WriteLine($"{CommonObject.PREFIX_BACK} {parent}");
                }
            } else {
                Console.WriteLine("invalid objectId");
            }
        }
    }
}
