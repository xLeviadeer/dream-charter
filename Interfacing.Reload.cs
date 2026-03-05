using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_Charter {
    internal static partial class Interfacing {

        // -- Reload --

        private static void _reload()
            => _reload(true);

        private static void _reload(bool doConfirm) {
            // reload
            string oldStartingLocationId = DreamLocation.StartingLocationId;
            try {
                DreamObject.Reload();
                DreamLocation.Reload();
            } catch (Exception e) {
                Console.WriteLine($"an error occurred: '{e.Message}'");
                _doLockdownMode = true;
                _updateRichPresence(true);
                return;
            }

            // check for changed starting location
            if (oldStartingLocationId != DreamLocation.StartingLocationId) {
                // reset location history
                _locationHistory = DEFAULT_LOCATION_HISTORY;
            } else {
                // ensure all parts of the location history still exist
                foreach (string? location in _locationHistory) {
                    // at least one didn't exist
                    if (
                        (location is null)
                        || (!DreamLocation.LIST.ContainsKey(location))
                    ) {
                        // reset _location history
                        _locationHistory = DEFAULT_LOCATION_HISTORY;
                        break;
                    }
                }
            }

            // disable lockdwon mode if reloaded without issue
            if (_doLockdownMode) { 
                _doLockdownMode = false;
                _updateRichPresence();
            }
            if (doConfirm) { Console.WriteLine("reloaded data sucessfully"); }
        }
    }
}
