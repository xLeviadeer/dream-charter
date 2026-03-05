using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_Charter {
    internal static partial class Interfacing {

        // -- Settings --

        private static void _settings()
            => _multiselector_Input(
                "= Settings =",
                "choose a setting to change (int): ",
                [
                    ("minimize info", $"{{{_doMinInfo}}} toggles compacting and hiding of excess info", _minInfo),
                    ("always show robust paths", $"{{{_do_AlwaysRobustPaths}}} toggles whether to always show robust paths and hence skip the robust path dialog", _alwaysRobustPaths),
                    ("aggressive inlining", $"{{{IInformationContainer.DoAggressiveInlining}}} toggles whether or not to use agressive inlining. When on, nested information fields will be condensed more harshly onto other fields", _aggressiveInlining),
                    ("clear data", "deletes all data and resets the overworld file", _clearData),
                ]
            );

        // - Min Info -

        private static bool _doMinInfo = false;

        private static void _minInfo() {
            _doMinInfo = !_doMinInfo;
            Console.WriteLine($"set showing minimum to {_doMinInfo}");
        }

        // - Always Robust Paths -

        private static bool _do_AlwaysRobustPaths = false;

        private static void _alwaysRobustPaths() {
            _do_AlwaysRobustPaths = !_do_AlwaysRobustPaths;
            Console.WriteLine($"set always showing robust paths to {_do_AlwaysRobustPaths}");
        }

        // - Aggressive Inlining -

        private static void _aggressiveInlining() {
            IInformationContainer.DoAggressiveInlining = !IInformationContainer.DoAggressiveInlining;
            Console.WriteLine($"set aggressive inlining to {IInformationContainer.DoAggressiveInlining}");
        }

        // - Clear Data - 

        private static void _clearData()
            => _clearData(true);

        private static void _clearData(bool doConfirm) {
            // double check
            string fullpath = Path.GetFullPath(DreamLocation.DATA_FOLDER);
            string? input = "y";
            if (doConfirm) {
                input = Console.AskForInput($"are you sure you want to clear data? this will delete the contents of '{fullpath}' (I am sure, n): ")
                    ?.Trim()
                    .ToLower();
            }
            Console.WriteLine();
            if (input == "i am sure") {
                // delete dir
                if (Directory.Exists(fullpath)) {
                    Directory.Delete(fullpath, true);
                }

                // catch permission errors
                try {
                    // create dir
                    Directory.CreateDirectory(fullpath);

                    // re-add overworld
                    File.WriteAllText(
                        Path.Combine(fullpath, $"{DreamLocation.StartingLocationId}.{DreamLocation.Ext}"),
                        DreamLocation.StartingLocationId
                    );
                } catch (Exception err) when (
                    err is DirectoryNotFoundException
                    || err is IOException
                ) {
                    Console.WriteLine("could not recreate data folder ╎ make sure you have the data folder closed and try again.");
                    return;
                }

                // log
                Console.WriteLine("successfully wiped all data");
            }
        }
    }
}
