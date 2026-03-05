using QuikGraph;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;

namespace Dream_Charter {

    internal sealed record DreamPath : IInformationContainer {
        internal const char PREFIX = '-';
        internal const string SEP = "->";
        internal const string SEP_BACK = "<-";
        internal const char COST_CHAR = '#';

        internal required string LocationId;
        internal readonly List<string> Directions = new();
        internal uint Cost = 0;
        public List<Information> Informations { get; } = new();

        [SetsRequiredMembers]
        internal DreamPath(string locationId, string[] directions, uint cost=0) {
            LocationId = locationId;
            if (directions.Length < 1) { throw new ArgumentException($"error creating {nameof(DreamPath)}: {nameof(DreamPath)} leading to '{locationId}' must have at least 1 direction"); }
            Directions = directions.ToList();
            Cost = cost;
        }

        public string ToStringPlain()
            => $"{PREFIX} {string.Join($" {SEP} ", this.Directions)} {SEP} {this.LocationId}";

        public override string ToString()
            => this.ToStringPlain()
            + ((Cost != 0) ? $" {COST_CHAR}{Cost}" : string.Empty)
            + $"{((IInformationContainer)this).ToStringInformationContainer()}\n";
    }

    internal sealed record Object : IInformationContainer {
        internal const char PREFIX = '+';

        internal required string Name;
        public List<Information> Informations { get; } = new();

        [SetsRequiredMembers]
        internal Object(string name) {
            Name = name;
        }

        public override string ToString()
            => $"{PREFIX} {this.Name}"
            + $"{((IInformationContainer)this).ToStringInformationContainer()}\n";

        /// <summary>
        /// composes multiple objects information into one object with all information
        /// </summary>
        /// <param name="objs"> the objects with information to compose ╎ the first object's name is used as the name </param>
        /// <returns> a new object with information from the composed objects </returns>
        internal static Object ComposeObjects(params Object[] objs) {
            // create a new object with name of first object
            Object composedObj = new Object(objs[0].Name);
            
            // for every object and info, add
            foreach (Object obj in objs) {
                foreach (Information info in obj.Informations) {
                    composedObj.Informations.Add(info);
                }
            }

            // return composed
            return composedObj;
        }
    }

    internal sealed record CommonObject : IInformationContainer {
        internal const char PREFIX = '^';
        internal const char PREFIX_BACK = '⌄';

        internal required string ObjectId;
        public List<Information> Informations { get; } = new();

        [SetsRequiredMembers]
        internal CommonObject(string objectId) {
            ObjectId = objectId;
        }

        public override string ToString()
            => $"{PREFIX} {this.ObjectId}"
            + $"{((IInformationContainer)this).ToStringInformationContainer()}\n";
    }

    internal sealed record Curiosity : IInformationContainer {
        internal const char PREFIX = '*';

        internal required string Name;
        public List<Information> Informations { get; } = new();

        [SetsRequiredMembers]
        internal Curiosity(string name) {
            Name = name;
        }

        public override string ToString()
            => $"{PREFIX} {this.Name}"
            + $"{((IInformationContainer)this).ToStringInformationContainer()}\n";
    }

    internal sealed record ObjWithParent(
        string ParentName,
        Object Obj
    );

    internal sealed record CommonObjWithParent(
        string ParentName,
        CommonObject Obj
    );

    internal enum ReservedLocationAbility {
        None,
        AllowsPathing,
        AllowsPathfinding
    }

    internal sealed record ReservedLocation {

        // --- VARIABLES ---

        // -- Static --

        private static HashSet<string> _usedIds = [];

        // -- Instance --

        internal string Id { get; private init; }
        internal HashSet<ReservedLocationAbility> Abilities { get; private init; }

        // --- CONSTRUCTOR ---

        internal ReservedLocation(
            string id,
            HashSet<ReservedLocationAbility> abilities
        ) {
            // id
            id = id
                .Trim()
                .ToLower();
            if (_usedIds.Contains(id)) { throw new ArgumentException($"the same id╌⸉{id}⸉╌cannot be used twice"); }
            this.Id = id;

            // abilities
            this.Abilities = abilities;
        }

        internal ReservedLocation(
            string id,
            ReservedLocationAbility ability
        ) : this(id, [ability]) { }

        internal ReservedLocation(
            string id
        ) : this(id, [ReservedLocationAbility.None]) { }
    }

    /// <summary>
    /// DreamLocation class for managing a dream location
    /// </summary>
    internal sealed class DreamLocation : DreamDocument<DreamLocation>,
        IDreamDocument<DreamLocation> {

        // --- VARIABLES ---

        // -- Static --
        
        internal const string EXT_FILE = "common_location_extension.txt";
        internal const string DEFAULT_EXT = "dreamlocation";
        public static string Ext { get; set; }

        internal const string CREATION_PATH_FILE = "common_location_creation.txt";
        internal const string DEFAULT_CREATION_PATH = "";
        public static string CreationPath { get; set; }

        internal const string STARTING_LOCATION_FILE = "default_location.txt";
        internal const string DEFAULT_STARTING_LOCATION_ID = "overworld";
        internal static string StartingLocationId { get; set; }

        internal const string EXIT_DIRECTION_FILE = "exit_keyword.txt";
        internal const string DEFAULT_EXIT_DIRECTION = "wake up";
        internal static string ExitDirection { get; set; }

        internal static ReservedLocation RES_INFINITE { get; } = new(
            "i",
            ReservedLocationAbility.AllowsPathing
        );
        internal static ReservedLocation RES_ANYWHERE { get; } = new(
            "a",
            [
                ReservedLocationAbility.AllowsPathing,
                ReservedLocationAbility.AllowsPathfinding
            ]
        );
        internal static ImmutableArray<ReservedLocation> RESERVED_LOCATIONS { get; } = [ 
            new("?"),
            RES_INFINITE,
            RES_ANYWHERE
        ];
        public static string Name { get; } = "Common Location";
        public static Dictionary<string, DreamLocation> LIST { get; } = new();
        public static Dictionary<string, List<ObjWithParent>> OBJ_PARENT_MAP { get; } = new();
        public static Dictionary<string, List<CommonObjWithParent>> COMMON_OBJ_PARENT_MAP { get; } = new();
        public static AdjacencyGraph<string, TaggedEdge<string, int>> GRAPH { get; } = new();
        public const int GRAPH_COSTLESS_WEIGHT = 1;
        public static AdjacencyGraph<string, TaggedEdge<string, int>> GRAPH_COSTLESS { get; } = new();

        // -- Instance --

        internal readonly List<DreamPath> Paths = new();
        internal readonly List<Object> Objects = new();
        internal readonly List<CommonObject> CommonObjects = new();
        internal readonly List<Curiosity> Curiosities = new();

        // --- CONSTRUCTORS ---

        [SetsRequiredMembers]
        internal DreamLocation(string id)
            : base(id) { }

        // --- METHODS ---

        // -- Static --

        // - Parse -

        private enum _parseMode {
            id,
            path,
            obj,
            common_obj,
            curisosity,
            information
        }

        private static readonly ImmutableDictionary<char, _parseMode> KEYWORD_TO_PARSE_MODE = new Dictionary<char, _parseMode>() {
            [DreamPath.PREFIX] = _parseMode.path,
            [Object.PREFIX] = _parseMode.obj,
            [CommonObject.PREFIX] = _parseMode.common_obj,
            [Curiosity.PREFIX] = _parseMode.curisosity,
            [IInformationContainer.PREFIX] = _parseMode.information
        }.ToImmutableDictionary();

        public static DreamLocation Parse(string? text) {
            // null text
            if (text is null) { throw new FormatException($"error parsing {nameof(DreamLocation)}: provided text was null"); }

            // mode controller and object
            _parseMode currMode = _parseMode.id;
            _parseMode? topLevelObjectMode = null;
            DreamLocation? parsedObj = null;

            // collector
                // collects a string to be used later
            StringBuilder collector = new();
            bool completed() => collector.Length == 0;

            // nesting level tracker
            const int NESTING_LEVEL_DEFAULT = 0;
            int nestingLevel = NESTING_LEVEL_DEFAULT; 
            int lastNestingLevel = NESTING_LEVEL_DEFAULT; 
            int secondLastNestingLevel = NESTING_LEVEL_DEFAULT;

            // loop setup
            int i = 0;
            int lastIndex = text.Length - 1;
            bool foundNewCharactersSinceLastNewLine = false;

            // finalization helper
                // completes the action for the respective current mode
                // resets the collector
            bool findingPath = false;
            List<string> pathDirections = new();

            // cost finding helper
            uint cost = 0;
            void findCost(string text, out string cleanedText) {
                const string PATTERN = @"(?:\s#[0-9]+\s|#[0-9]+)"; // :? just means to not run special, extra, grouping logic that I dont need
                MatchCollection matches = new Regex(PATTERN).Matches(text);
                if (matches.Count > 0) {
                    foreach (Match match in matches) {
                        string value = new Regex(@"[0-9]+").Match(match.Value).Value;
                        if (value == string.Empty) { continue; }
                        try {
                            cost += uint.Parse(value);
                        } catch (OverflowException) {
                            throw new FormatException($"error finding cost {nameof(DreamLocation)} (collector: {collector}, text: {text}): provided cost '{value}' too large; max cost is {uint.MaxValue}");
                        }
                        
                    }
                }
                cleanedText = Regex.Replace(text, PATTERN, "");
            }

            void finalize(_parseMode? nextMode) {
                // check if the collector has nothing
                if (
                    (collector.Length == 0)
                    || collector.ToString().IsWhiteSpace()
                ) { throw new ArgumentNullException($"error finalizing {nameof(DreamLocation)}: attempting to use collector which has no value"); }
                string cleanedCollector = collector
                        .ToString()
                        .Trim()
                        .ToLower();

                // if id mode
                if (currMode == _parseMode.id) {
                    parsedObj = new DreamLocation(cleanedCollector);
                    if (lastNestingLevel != NESTING_LEVEL_DEFAULT) {
                        throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): the default formatting level must be 0; id must be completely un-nested");
                    }
                    goto finish;
                } else if (parsedObj is null) {
                    throw new InvalidOperationException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): parser left id parse mode before collecting id and setting to parsedObj");
                }

                // switch over mode
                switch (currMode) {
                    case _parseMode.path: {
                        // check based on if first finding a path
                        if (!findingPath) { // first time
                            // default nesting check
                            if (nestingLevel != NESTING_LEVEL_DEFAULT) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): paths must be not be nested"); }

                            // separator arrow check
                            if (cleanedCollector.StartsWith(DreamPath.SEP[1])) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): paths cannot start with an arrow"); }

                            // look for cost
                            findCost(cleanedCollector, out cleanedCollector);

                            // add direction
                            pathDirections.Add(cleanedCollector);
                            findingPath = true;

                        } else if (
                            (nextMode != _parseMode.path)
                            || text[i + 1] != DreamPath.SEP[1]
                        ) { // last time
                            // nesting check
                            if (
                                (lastNestingLevel != NESTING_LEVEL_DEFAULT)
                                && (lastNestingLevel != (NESTING_LEVEL_DEFAULT + 1))
                            ) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): path extra directions and locations can only be nested by 1"); }

                            // separator arrow check
                            if (!cleanedCollector.StartsWith(DreamPath.SEP[1])) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): path location must be separated by an arrow"); }
                            cleanedCollector = cleanedCollector[1..].Trim();
                            if (
                                (cleanedCollector.Length == 0)
                                || cleanedCollector.IsWhiteSpace()
                            ) { throw new ArgumentNullException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): attempting to use collector which has no value"); }

                            // ensure no cost
                            if (cleanedCollector.Contains(DreamPath.COST_CHAR)) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): a location id cannot include a cost value; try using an anonymous cost instead: `- direction -> #1 -> location`"); }

                            // create
                            parsedObj.Paths.Add(new DreamPath(cleanedCollector, pathDirections.ToArray(), cost));
                            pathDirections.Clear();
                            findingPath = false;
                            cost = 0;

                        } else { // middle time
                            // nesting check
                            if (
                                (lastNestingLevel != NESTING_LEVEL_DEFAULT)
                                && (lastNestingLevel != (NESTING_LEVEL_DEFAULT + 1))
                            ) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): path extra directions and locations can only be nested by 1"); }

                            // separator arrow check
                            if (!cleanedCollector.StartsWith(DreamPath.SEP[1])) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector} on {parsedObj.Id ?? "‹unknown›"}): extra path directions must be separated by arrows"); }
                            cleanedCollector = cleanedCollector[1..].Trim();
                            if (
                                (cleanedCollector.Length == 0)
                                || cleanedCollector.IsWhiteSpace()
                            ) { throw new ArgumentNullException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): attempting to use collector which has no value"); }

                            // look for cost
                            findCost(cleanedCollector, out cleanedCollector);

                            // add direction
                            pathDirections.Add(cleanedCollector);
                            findingPath = true;
                        }
                        if (
                            cleanedCollector.Contains(DreamPath.SEP[1])
                            || cleanedCollector.Contains(DreamPath.PREFIX)
                        ) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): path cannot contain prefixes or arrows inside of it's locations or directions"); }

                        break;
                    }

                    case _parseMode.obj:
                        // nesting check
                        if (lastNestingLevel != NESTING_LEVEL_DEFAULT) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): objects must be not be nested"); }

                        parsedObj.Objects.Add(new Object(cleanedCollector));
                        break;

                    case _parseMode.common_obj:
                        // nesting check
                        if (lastNestingLevel != NESTING_LEVEL_DEFAULT) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): common objects must be not be nested"); }

                        parsedObj.CommonObjects.Add(new CommonObject(cleanedCollector));
                        break;

                    case _parseMode.curisosity:
                        // nesting check
                        if (lastNestingLevel != NESTING_LEVEL_DEFAULT) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): curiosities must be not be nested"); }

                        parsedObj.Curiosities.Add(new Curiosity(cleanedCollector));
                        break;

                    case _parseMode.information: {
                        // null check (toplevel)
                        if (topLevelObjectMode is null) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): information cannot start as double nested"); }

                        // check nesting level
                        if (
                            (lastNestingLevel != secondLastNestingLevel)
                            && (lastNestingLevel != (secondLastNestingLevel + 1))
                            && (lastNestingLevel != (secondLastNestingLevel - 1))
                        ) { throw new FormatException($"error finalizing {nameof(DreamLocation)} (collector: {collector}): information must be tabbed in by 1 and be on a single line"); }

                        // if toplevel info
                        Information info = new(cleanedCollector, lastNestingLevel);
                        if (lastNestingLevel == 0) {
                            parsedObj.Informations.Add(info);
                            goto finish;
                        }

                        // check what to put underneath
                        switch (topLevelObjectMode) {
                            case _parseMode.path:
                                parsedObj.Paths[^1].Informations.Add(info);
                                break;
                            case _parseMode.obj:
                                parsedObj.Objects[^1].Informations.Add(info);
                                break;
                            case _parseMode.common_obj:
                                parsedObj.CommonObjects[^1].Informations.Add(info);
                                break;
                            case _parseMode.curisosity:
                                parsedObj.Curiosities[^1].Informations.Add(info);
                                break;
                            case _parseMode.information:
                                parsedObj.Informations.Add(info);
                                break;
                        }
                        break;
                    }
                }

            finish:
                // clear the collector
                    // marks as completed
                collector.Clear();
            }

            // start loop
            for (; i < text.Length; i++) {
                char c = text[i];

                // check for spacing characters
                    // spacing characters do not mark as not completed because they don't modify the collector
                bool isEmptyLine = !foundNewCharactersSinceLastNewLine && c.ToString().IsWhiteSpace();
                switch (c) {
                    case '\n':
                        secondLastNestingLevel = lastNestingLevel;
                        lastNestingLevel = nestingLevel;
                        nestingLevel = NESTING_LEVEL_DEFAULT;
                        foundNewCharactersSinceLastNewLine = false;
                        continue;
                    case '\t' when isEmptyLine:
                        nestingLevel += 1;
                        continue;
                    case '\t':
                        // check for inline tabs
                        throw new FormatException($"error parsing {nameof(DreamLocation)} (collector: {collector}): tabs cannot be used inline; tabs must only prepend lines");
                    case var _ when isEmptyLine:
                        // skips whitespaces if no characters have been found on this line yet
                        continue;
                }

                // check for keywords, finalize and switch modes
                if (KEYWORD_TO_PARSE_MODE.Keys.Contains(c)) {
                    finalize(KEYWORD_TO_PARSE_MODE[c]);
                    if (lastNestingLevel == 0) { topLevelObjectMode = currMode; }
                    currMode = KEYWORD_TO_PARSE_MODE[c];
                    continue;
                }

                // add to collecter
                    // marks as incomplete 
                collector.Append(c);
                foundNewCharactersSinceLastNewLine = true;
            }

            // if not completed, finalize with current mode and collector contents
            if (!completed()) {
                secondLastNestingLevel = lastNestingLevel;
                lastNestingLevel = nestingLevel;
                finalize(null);
            }

            // check if nothing found
            if (parsedObj == null) { throw new FormatException($"error parsing {nameof(DreamLocation)} (collector: {collector}): the text did not contain a valid id"); }

            // return new object
            return parsedObj;
        }

        // - List Ids -

        public static void PrintListObjectNames() {
            Console.WriteLine($"List of {nameof(Object)}⧼s⧽");
            foreach (var objName in DreamLocation.OBJ_PARENT_MAP.Keys) {
                Console.WriteLine(objName);
            }
        }

        // - To String -

        public override string ToString() {
            string infoString = ((IInformationContainer)this).ToStringInformationContainer();
            return $"{this.Id.ToUpper()}\n"
                + string.Join("", this.Paths)
                + string.Join("", this.Objects)
                + string.Join("", this.CommonObjects)
                + string.Join("", this.Curiosities)
                + ((infoString.Length == 0) ? string.Empty : infoString[1..]);
        }

        // - Reload -

        public static void Reload() {
            // clear existing
            DreamLocation.LIST.Clear();
            DreamLocation.OBJ_PARENT_MAP.Clear();
            DreamLocation.COMMON_OBJ_PARENT_MAP.Clear();
            DreamLocation.GRAPH.Clear();
            DreamLocation.GRAPH_COSTLESS.Clear();

            // add reserved
            DreamLocation? iLocation = null;
            DreamLocation? aLocation = null;
            foreach (ReservedLocation location in RESERVED_LOCATIONS) {
                DreamLocation newLoc = new DreamLocation(location.Id);
                if (location.Id == RES_INFINITE.Id) { iLocation = newLoc; }
                else if (location.Id == RES_ANYWHERE.Id) { aLocation = newLoc; }
                DreamLocation.LIST[location.Id] = newLoc;
                DreamLocation.GRAPH.AddVertex(location.Id);
                DreamLocation.GRAPH_COSTLESS.AddVertex(location.Id);
            }

            // create loctions folder if needed
            if (!Path.Exists(DATA_FOLDER)) {
                Directory.CreateDirectory(DATA_FOLDER);
            }

            // create extension file in needed
            Ext = File.GetOrCreateDefault(
                Path.Combine(DATA_FOLDER, EXT_FILE),
                DEFAULT_EXT
            );

            // create default path
            CreationPath = File.GetOrCreateDefault(
                Path.Combine(DATA_FOLDER, CREATION_PATH_FILE),
                DEFAULT_CREATION_PATH
            );

            // create default path file if needed
            StartingLocationId = File.GetOrCreateDefault(
                Path.Combine(DATA_FOLDER, STARTING_LOCATION_FILE),
                DEFAULT_STARTING_LOCATION_ID
            );

            // create exit direction file if needed
            ExitDirection = File.GetOrCreateDefault(
                Path.Combine(DATA_FOLDER, EXIT_DIRECTION_FILE),
                DEFAULT_EXIT_DIRECTION
            );

            // for each dream document in the directory create a variable for it in the dictionary
            ForFilesOfExt((file) => {

                // read text
                string content = File.ReadAllText(file);

                // parse
                content = content.Replace(new string(' ', SPACES_TO_TABS), "\t"); // convert spaces into tabs
                var location = DreamLocation.Parse(content);

                // check if common object references are real
                // assumes common objects have already been reloaded; reload order is important
                foreach (CommonObject obj in location.CommonObjects) {
                    if (!DreamObject.LIST.ContainsKey(obj.ObjectId)) {
                        throw new FormatException($"common object reference, '{obj.ObjectId}' cannot be found in '{location.Id}'");
                    }
                }

                // creates the object to parents map
                foreach (Object obj in location.Objects) {
                    string nameLower = obj.Name.ToLower();
                    ObjWithParent objWithParent = new(location.Id.ToLower(), obj);
                    if (DreamLocation.OBJ_PARENT_MAP.ContainsKey(nameLower)) {
                        DreamLocation.OBJ_PARENT_MAP[nameLower].Add(objWithParent);
                    } else {
                        DreamLocation.OBJ_PARENT_MAP[nameLower] = [objWithParent];
                    }
                }

                // creates the common object to parents map
                foreach (CommonObject obj in location.CommonObjects) {
                    CommonObjWithParent objWithParent = new(location.Id, obj);
                    if (DreamLocation.COMMON_OBJ_PARENT_MAP.ContainsKey(obj.ObjectId)) {
                        DreamLocation.COMMON_OBJ_PARENT_MAP[obj.ObjectId].Add(objWithParent);
                    } else {
                        DreamLocation.COMMON_OBJ_PARENT_MAP[obj.ObjectId] = [objWithParent];
                    }
                }

                // add to list and graph
                DreamLocation.CheckDuplicate(location.Id);
                DreamLocation.LIST.Add(location.Id, location);
                DreamLocation.GRAPH.AddVertex(location.Id);
                DreamLocation.GRAPH_COSTLESS.AddVertex(location.Id);

                // add path from i and a
                iLocation!.Paths.Add(new DreamPath(location.Id, ["␣"]));
                aLocation!.Paths.Add(new DreamPath(location.Id, ["␣"]));
            });

            // check for default
            if (!DreamLocation.LIST.ContainsKey(StartingLocationId)) { throw new InvalidOperationException($"{StartingLocationId} must exist"); }

            // add each path to the graph
                // remember, this needs to be done separately so that each node exists in the graph before trying to connect them
            foreach (var kvp in DreamLocation.LIST) {
                string id = kvp.Key;
                DreamPath[] paths = kvp.Value.Paths.ToArray();

                // for every path
                foreach (var path in paths) {
                    try {
                        // try to add to graph
                        DreamLocation.GRAPH.AddEdge(
                            new TaggedEdge<string, int>(
                                id,
                                path.LocationId,
                                (int)path.Cost
                            )
                        );
                        DreamLocation.GRAPH_COSTLESS.AddEdge(
                            new TaggedEdge<string, int>(
                                id,
                                path.LocationId,
                                GRAPH_COSTLESS_WEIGHT
                            )
                        );
                    } catch (VertexNotFoundException) {
                        throw new FormatException($"location {path.LocationId} in {id} is not a valid location");
                    }
                }
            }
        }
    }
}
