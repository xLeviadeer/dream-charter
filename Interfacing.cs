
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Msagl.Drawing;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Search;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dream_Charter {

    internal static partial class Interfacing {

        // --- VARIABLES ---

        // - Debug -

        private const bool DO_CLEAR = true;

        // - Options -

        private static readonly ImmutableArray<(string name, string description, Action action)> OPTIONS = [
            ("Settings", "shows different togglable settings", _settings),
            ("Reload", "reloads all data", _reload),
            ("Display Common Info", "displays info, either location or object", _displayInfos),
            ("List Special Items", "lists items, either objects or curiosities", _listItems),
            ("Find Paths", "finds paths, either shortest or cheapest", _findPaths),
            ("Create", "creates a blank location or object", _create),
            ("Show", "shows a location visualization graph", _show)
        ];

        private static readonly ImmutableArray<(string name, string description)> EXTRA_INSTRUCTIONS = [
            (". (back)", "use a `.` to denote travelling backwards. Add multiple periods to specify the backwards travel distance. Travels backwards via your travel history."),
            ($", ({DreamLocation.ExitDirection})", $"use a `,` to denote travelling backwards. Add multiple commas to specify the backwards travel distance. Travels backwards via the '{DreamLocation.ExitDirection}' path. If the '{DreamLocation.ExitDirection}' path does not exist then nothing will happen."),
            ("locationId (travel)", "type the name of a path's location in the current location to travel to it"),
            ("> locationId (multi-travel)", $"type a travel path to traverse it at once. Must begin with '{ActivePath.PATH_SPLIT}' but not end with '{ActivePath.PATH_SPLIT}'. Each location should be separated by '{ActivePath.PATH_SPLIT}'"),
            ("objectId (show)", "type the name of a common object in the current location to show it's data"),
            ("/ (slash)", "paths may loop as you explore, use `/` to remove loops. Simply removing loops may not be the most efficient path"),
            ("// (home)", $"use `//` to jump back to {DreamLocation.StartingLocationId} and reset the path"),
            ("r (robust)", "use `r` to show the current path as a robust path (a path with directions included)"),
            (".i (instant)", $"use `.i` to instant travel to a location via quickest path. Use /i to instant travel from home instead of the current location")
        ];

        // - Location History -

        private static Stack<string> DEFAULT_LOCATION_HISTORY => new([DreamLocation.StartingLocationId]);
        private static Stack<string> _locationHistory { get; set; } = DEFAULT_LOCATION_HISTORY;
        private static string _currentLocationString() => _locationHistory.Peek();
        private static DreamLocation _currentLocation() => DreamLocation.LIST[_locationHistory.Peek()];
        private static ActivePath _locationHistoryPath() => _locationHistory
                    .AsEnumerable()
                    .Reverse()
                    .ToActivePath();

        // - Lockdown Mode -

        private static bool _doLockdownMode { get; set; } = false;

        // - Common Types Enum -

        private enum _commonType {
            location,
            obj
        }

        // --- SPLITTER ---

        private enum _splitterSize {
            small,
            large
        }

        private static readonly ImmutableDictionary<_splitterSize, int> SPLITTER_SIZE_TO_INT = new Dictionary<_splitterSize, int>() {
            [_splitterSize.small] = 40,
            [_splitterSize.large] = 120
        }.ToImmutableDictionary();

        private static void _printSplitter(_splitterSize size) {
            Console.WriteLine();
            Console.WriteLine(new string('-', SPLITTER_SIZE_TO_INT[size]));
        }

        // --- MAIN LOOP ---

        public static void Start() {
            // set console encoding for upside down caret symbol
            Console.OutputEncoding = Encoding.UTF8;

            // check if data exists
            if (!Directory.Exists(DreamLocation.DATA_FOLDER)) { _clearData(doConfirm: false); }

            // attempt to run reload to start things up
                // this will enable lockdown mode if needed and then travel into the loop
            _reload(doConfirm: false);

            // enable discord components
            DiscordRichPresence.Start();

            // loop
            while (true) {
                _loop();
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        private static void _loop() {
            // lockdown mode loop
            if (_doLockdownMode) {
                Console.WriteLine("you are currently in lockdown mode. In lockdown mode nothing can run until the program syntax has been successfully reloaded. Correct syntax issues to continue. Press any key to reload");
                Console.AskForInput("press anything...");
                _reload();
                return;
            }

            // get selection input
            string? input = _selectionOption_Input(out bool isUnprocessedString);
            if (
                (input is null) // null input
                || (!isUnprocessedString) // if the user made a selection which wasn't a possible explore function
            ) { return; }

            // execute an explore function if applicable
            _tryExecuteExploreOperation(input);
        }

        private static string? _selectionOption_Input(out bool isUnprocessedString) {
            // switch based on min info
            switch (_doMinInfo) {
                case true:
                    // min info header
                    Console.WriteLine("=== Options ===");

                    // display options to user
                    for (int i = 0; i < OPTIONS.Length; i++) {
                        var option = OPTIONS[i];
                        Console.Write($"{i}. {option.name} | ");
                    }

                    // display extra info
                    foreach (var notation in EXTRA_INSTRUCTIONS[..^1]) {
                        Console.Write($"{notation.name} | ");
                    }
                    Console.WriteLine(EXTRA_INSTRUCTIONS[^1].name);
                    Console.WriteLine();
                    break;

                case false:
                    // display options to user
                    Console.WriteLine("=== Index Selectable Options ===");
                    _displayOptions(OPTIONS);
                    Console.WriteLine();

                    // display extra info
                    Console.WriteLine("=== Exploration Information ===");
                    _displayOptions(EXTRA_INSTRUCTIONS);
                    Console.WriteLine();
                    break;
            }

            // display the current location
            Console.WriteLine("~~~ \\/ your location \\/ ~~~");
            ActivePath locationHistoryListNoLast = _locationHistoryPath()[..^1];
            Console.Write($"{
                    locationHistoryListNoLast.ToPathString()
                }{
                    ((locationHistoryListNoLast.Count() > 0) ? $" {ActivePath.PATH_SPLIT} " : "")
                }"
            );
            Console.WriteLine(_currentLocation().ToString());
            Console.WriteLine();

            // wait for user selection
            string? input = Console.AskForInput("explore... (int index, string navigator): ");
            input = input
                ?.Trim()
                .ToLower();
            if (input is null) { goto is_not_unprocessed_string; }

            // try to parse as number
            int selection;
            try {
                selection = int.Parse(input);
                if (DO_CLEAR) { Console.Clear(); }
            } catch (OverflowException) {
                if (DO_CLEAR) { Console.Clear(); }
                _printSplitter(_splitterSize.large);
                Console.WriteLine("selection invalid, try again");
                goto is_not_unprocessed_string;
            } catch (FormatException) {
                if (DO_CLEAR) { Console.Clear(); }
                goto is_unprocessed_string;
            }

            // print splitter
            _printSplitter(_splitterSize.large);

            // range check
            if (selection >= OPTIONS.Length) {
                Console.WriteLine("selection invalid, try again");
                goto is_not_unprocessed_string;
            }

            // execute
            OPTIONS[selection].action();
            // goes to not unprocessed

        is_not_unprocessed_string:
            isUnprocessedString = false;
            return input;
        is_unprocessed_string:
            isUnprocessedString = true;
            return input;
        }

        private static void _updateRichPresence(bool doDebugging = false) {
            // skip
            if (!DiscordRichPresence.ClientEnabled) { return; }

            // set
            DiscordRichPresence.Client.UpdateDetails(doDebugging
                ? "Debugging..."
                : $"Exploring ⟨{_currentLocationString()}⟩..."
            );
        }

        // -- Exploration --
        #region Exploration

        private static bool _moveBack(uint count, bool do_Print = true) {
            if (do_Print) { _printSplitter(_splitterSize.large); }

            // path back
            bool failed = false;
            int i = 0;
            for (; i < (count - 1); i++) {
                if (
                    (_locationHistory.Count == 1)
                    && (_currentLocationString() == DreamLocation.StartingLocationId)
                ) {
                    Console.WriteLine($"cannot continue backwards behind {DreamLocation.StartingLocationId}, stopped early");
                    failed = true;
                    break;
                }
                _locationHistory.Pop();
                _updateRichPresence();
            }

            if (do_Print) { Console.WriteLine($"traveled back {i} times"); }
            return !failed;
        }

        private static bool _moveUp(uint count, bool do_Print = false) {
            _printSplitter(_splitterSize.large);

            // path up
            bool failed = false;
            DreamLocation currLocation = _currentLocation();
            int i = 0;
            for (; i < (count - 1); i++) {
                // cannot wake up from start
                if (currLocation.Id == DreamLocation.StartingLocationId) {
                    Console.WriteLine($"cannot continue backwards behind {DreamLocation.StartingLocationId}, stopped early");
                    failed = true;
                    break;
                }

                // find wake up
                DreamPath? wakeUpDreamPath = currLocation.Paths
                    .Where(path => path.Directions[0] == DreamLocation.ExitDirection)
                    .FirstOrDefault();
                if (wakeUpDreamPath is not null) {
                    // check for reserved
                    if (_checkReservedLocation(wakeUpDreamPath.LocationId)) { break; }

                    _locationHistory.Push(wakeUpDreamPath.LocationId);
                    
                    continue;
                }

                // no wake up location
                Console.WriteLine($"cannot '{DreamLocation.ExitDirection}' further, stopped early");
                break;
            }

            if (do_Print) { Console.WriteLine($"woke up {i} times"); }
            _updateRichPresence();
            return !failed;
        }

        private static void _moveHome(bool do_Print = true) {
            if (do_Print) { _printSplitter(_splitterSize.large); }

            // jump to home
            _locationHistory = DEFAULT_LOCATION_HISTORY;
            _updateRichPresence();

            if (do_Print) { Console.WriteLine("moved home"); }
        }

        private static void _slash() {
            _printSplitter(_splitterSize.large);

            // simplify path
            ActivePath locationHistoryList = _locationHistoryPath();
            for (int i = 0; i < locationHistoryList.Count; i++) {
                string location = locationHistoryList[i];
                ActivePath continuedLocation = locationHistoryList[(i + 1)..];

                // slice space between this and the next instance if one exists
                if (continuedLocation.Contains(location)) {
                    int locationIndex = locationHistoryList.LastIndexOf(location);
                    locationHistoryList = locationHistoryList[..i]
                        .Concat(locationHistoryList[locationIndex..])
                        .ToActivePath();
                }
            }

            // set back to stack
            Interfacing._locationHistory = new Stack<string>(locationHistoryList);
            _updateRichPresence();

            Console.WriteLine("simplified path");
        }

        private static void _showRobust() {
            _printSplitter(_splitterSize.large);

            // get robust path
            (string, List<DreamPath>)? robustPath = _locationHistoryPath().PathToRobustPath();
            if (robustPath is null) {
                Console.WriteLine("could not create robust path");
                return;
            }
            (string startingLocationId, List<DreamPath> paths) = robustPath.Value;

            // display
            Console.WriteLine("showing robust path");
            Console.WriteLine(ActivePath.ToPathStringRobust(startingLocationId, paths));
        }

        private const string INSTANT_HOMED_GROUP = "homed";
        private const string INSTANT_FIRST_GROUP = "first";
        private const string INSTANT_REMAINING_GROUP = "remaining";
        private static bool _moveInstant(Match match, ref string input) {
            // check its homed
            if (match.Groups[INSTANT_HOMED_GROUP].Success) {
                _moveHome(do_Print: false);
            }

            // check if first is present (and therefore is multipath)
            ActivePath? path;
            bool isMultipath;
            if (match.Groups[INSTANT_FIRST_GROUP].Success) {
                string locationId = match.Groups[INSTANT_FIRST_GROUP].Value;
                path = _findPath(
                    new(_pathFindMode.shortest),
                    new(_currentLocationString(), _searchableType.location),
                    new(locationId, _searchableType.location)
                );
                isMultipath = true;
                input = match.Groups[INSTANT_REMAINING_GROUP].Value;
            } else { // single path
                string locationId = match.Groups[INSTANT_REMAINING_GROUP].Value;
                path = _findPath(
                    new(_pathFindMode.shortest),
                    new(_currentLocationString(), _searchableType.location),
                    new(locationId, _searchableType.location)
                );
                isMultipath = false;
            }

            // try to move to path
            if (path is null) {
                Console.WriteLine("could not instant move to the provided path");
                return false;
            }
            _moveToPath(path);

            // if it's not a multipath, end here
            return isMultipath;
        }

        private static bool _checkReservedLocation(
            string locationId, 
            HashSet<ReservedLocationAbility> abilityExclusionFlags,
            bool do_Print = true,
            bool do_Silent = false
        ) {
            if (do_Print) { _printSplitter(_splitterSize.large); }

            // for each reserved location
            foreach (ReservedLocation location in DreamLocation.RESERVED_LOCATIONS) {
                // check for matching flag (skips early if none)
                if (abilityExclusionFlags.Any(ability =>
                    (ability != ReservedLocationAbility.None)
                    && location.Abilities.Contains(ability)
                )) { continue; }

                // check location
                if (locationId == location.Id) {
                    if (!do_Silent) { Console.WriteLine("pathing to this reserved location is not permitted. Use location info display to view reserved locations."); }
                    return true;
                }
            }
            return false; 
        }

        private static bool _checkReservedLocation(
            string locationId,
            ReservedLocationAbility abilityFlag,
            bool do_Print = true,
            bool do_Silent = false
        ) => _checkReservedLocation(locationId, [abilityFlag], do_Print, do_Silent);

        private static bool _checkReservedLocation(
            string locationId,
            bool do_Print = true,
            bool do_Silent = false
        ) => _checkReservedLocation(locationId, [ReservedLocationAbility.None], do_Print, do_Silent);

        /// <summary>
        /// moves to a path
        /// </summary>
        /// <param name="path"> a path of locationId names </param>
        /// <param name="has_BackMovement"> a quick flag for if the first movment contains period. is not required to work correctly╌but, speeds up the process </param>
        /// <param name="do_Validation"> whether or not to validate if locations exist </param>
        /// <param name="do_Print"> whether or not to print path changes </param>
        /// <returns> a boolean, true if successfully moved </returns>
        private static bool _moveMultipath(
            ActivePath path,
            bool has_BackMovement = false,
            bool do_Validation = true,
            bool do_Print = true
        ) {
            _printSplitter(_splitterSize.large);

            // check if path has any values
            if (path.Count <= 0) {
                Console.WriteLine("cannot to move to empty path");
                return false;
            }

            // save the current path in-case an error occurs
            ActivePath oldPath = _locationHistoryPath();

            // try to do period based movement
            ActivePath adjPath = new(path);
            if (
                has_BackMovement
                || (PERIOD_REGEX.Match(adjPath[0]).Success)
            ) {
                uint count = (uint)adjPath[0].Trim().Length;
                if (!_moveBack(count, do_Print: false)) {
                    Interfacing._locationHistory = new Stack<string>(oldPath); // reset location history
                    return false;
                }
                adjPath.RemoveAt(0);

            // path start is the current location exactly
            } else if (adjPath[0] == _currentLocationString()) {
                adjPath.RemoveAt(0);
                if (adjPath.Count <= 0) {
                    goto success;
                }
            }

            // for each potential location in the path
            foreach (string locationId in adjPath) {
                // try to move path
                if (!_movePath(locationId, do_Validation, do_Print: false)) {
                    Interfacing._locationHistory = new Stack<string>(oldPath); // reset location history
                    return false;
                }
            }

        success:
            _updateRichPresence();

            // print success
            if (do_Print) {
                Console.WriteLine("moved successfully");
            }

            // return
            return true;
        }

        /// <summary>
        /// moves to a provided location id
        /// </summary>
        /// <param name="locationId"> the location id to move to </param>
        /// <param name="do_Validation"> whether to validate that the location exists before moving </param>
        /// <param name="do_Print"> whether prints when moved </param>
        /// <returns> a boolean, true is successfully moved </returns>
        private static bool _movePath(
            string locationId, 
            bool do_Validation = true,
            bool do_Print = true
        ) {
            // validate
            if (
                do_Validation 
                && !_currentLocation().Paths
                    .Select(p => p.LocationId)
                    .Contains(locationId)
            ) {
                Console.WriteLine($"location '{locationId}' does not exist on {_currentLocationString()}");
                return false;
            }

            // check for reserved locations
            if (_checkReservedLocation(locationId, ReservedLocationAbility.AllowsPathing, do_Print)) { return false; }

            // path in to location
            _locationHistory.Push(locationId);
            _updateRichPresence();

            // print
            if (do_Print) {
                Console.WriteLine($"moved into {locationId}");
            }

            // return
            return true;
        }

        private static void _moveObject(string objectId) {
            _printSplitter(_splitterSize.large);

            // show common object 
            Console.WriteLine("showing common object data");
            Console.WriteLine();
            Console.WriteLine(DreamObject.LIST[objectId].ToString());
        }

        private static readonly Regex PERIOD_REGEX = new Regex(@"^\.+$");
        private static readonly Regex COMMA_REGEX = new Regex(@"^\,+$");
        private static void _tryExecuteExploreOperation(string input) {

            // go back check
            MatchCollection periodMatch = PERIOD_REGEX.Matches(input);
            if (
                (input.Length > 0) // the amount of periods is more than 1
                && (periodMatch.Count > 0) // matched
            ) {
                _moveBack((uint)input.Length);
                return;
            }

            // go up check
            MatchCollection commaMatch = COMMA_REGEX.Matches(input);
            if (
                (input.Length > 0) // the amount of commas is more than 1
                && (commaMatch.Count > 0) // matched
            ) {
                _moveUp((uint)input.Length);
                return;
            } 
            
            // go home check
            if (input == @"//") {
                _moveHome();
                return;
            } 
            
            // slash check
            if (input == @"/") {
                _slash();
                return;
            } 
            
            // robust check
            if (input == @"r") {
                _showRobust();
                return;
            }

            string optionalSpaces = @"(?:\s)*";

            // move instant check
            string instantFormat = @$"^(?:(?:\.i\s)|(?<{INSTANT_HOMED_GROUP}>/i\s))(?:(?<{INSTANT_FIRST_GROUP}>[^>]+)\s>{optionalSpaces})?(?<{INSTANT_REMAINING_GROUP}>.+)$";
            Match instantMatch = new Regex(instantFormat).Match(input);
            if (instantMatch.Success) {
                if (!_moveInstant(instantMatch, ref input)) { return; }
            }

            // move by multipath check
            string periodsFormat = @"(\.+)?"; // group 1
            string locationIdsFormat = $@"((?:{optionalSpaces}[^>\s])+)"; // group 2
            string splitLocationsFormat = $@"(?:>(?:{optionalSpaces}{locationIdsFormat}{optionalSpaces}))+";
            string pathFormat = @$"^{periodsFormat}{optionalSpaces}{splitLocationsFormat}$";
            Match multipathMatch = new Regex(pathFormat).Match(input);
            if (multipathMatch.Captures.Count > 0) {
                var path = new ActivePath();

                // check if period is present
                bool has_BackMovement = multipathMatch.Groups[1].Success;
                if (has_BackMovement) {
                    path.Add(multipathMatch.Groups[1].Value);
                }

                // checks for location ids
                path.AddRange(multipathMatch.Groups[2].Captures
                    .Select(cap => cap.Value)
                    .ToActivePath()
                );

                // try to move to path
                _moveMultipath(path, has_BackMovement);
                return;
            }

            // move by locationId check
            if (
                (DreamLocation.LIST.ContainsKey(input))
                && (_currentLocation().Paths.Select(p => p.LocationId).Contains(input))
            ) {
                _movePath(input, do_Validation: false);
                return;
            } 
            
            // move by objectId check
            if (
                (DreamObject.LIST.ContainsKey(input))
                && (_currentLocation().CommonObjects.Select(o => o.ObjectId).Contains(input)) 
            ) {
                _moveObject(input);
                return;
            } 
            
            // invalid
            _printSplitter(_splitterSize.large);
            Console.WriteLine("selection invalid, try again");
        }

        #endregion
    }
}
