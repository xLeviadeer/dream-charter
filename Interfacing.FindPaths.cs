using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualBasic;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Dream_Charter {
    internal static partial class Interfacing {

        // -- Find Paths --

        private const string NO_PATH_FOUND_MESSAGE = "! no path found !";

        /// <summary>
        /// moves to a given path if possible
        /// </summary>
        /// <remarks>
        /// does ⊰not⊱ accept back movements
        /// </remarks>
        /// <param name="path"> the path to move through </param>
        /// <returns> a boolean, true if successful </returns>
        private static bool _moveToPath(ActivePath path) {
            // check if path has any values
            if (path.Count <= 0) {
                Console.WriteLine("cannot to move to empty path");
                return false;
            }
            string pathStart = path[0];

            // check reserved location
            if (_checkReservedLocation(
                path[^1], 
                ReservedLocationAbility.AllowsPathing, 
                do_Print: false
            )) {
                // prints splitter in check method
                return false;
            }

            // check if the path starts where the user starts
            if (pathStart == _currentLocationString()) {
                _moveMultipath(path);
                return true;
            } 

            // check if the path starts at home
            if (pathStart == DreamLocation.StartingLocationId) {
                _moveHome(do_Print: false);
                _moveMultipath(path);
                return true;
            }

            Console.WriteLine("cannot move to a path which does not start at the current location or home");
            return false;
        }

        private static void _findPaths() {
            // error if no locations
            if (DreamLocation.LIST.Count < 2) {
                Console.WriteLine("there is not enough locations to make a path");
                return;
            }

            // show list of common locations
            DreamLocation.PrintListIds();
            Console.WriteLine();

            // show list of common objects
            DreamObject.PrintListIds();
            Console.WriteLine();

            // show a list of objects
            DreamLocation.PrintListObjectNames();
            Console.WriteLine();

            // show helper
            Console.WriteLine("== while picking locations the following can be used ==");
            Console.WriteLine("locationId for a specific location");
            Console.WriteLine("objecId for the closest location with a common object or object in it");
            Console.WriteLine(". for the current location");
            Console.WriteLine("// for home (default location)");
            Console.WriteLine();

            // init start and stop (they will change every loop)
            _pathPoint startingLocation;
            _pathPoint stoppingLocation;

            // get starting location
            if (!_getLocation_Out(
                "pick a starting location",
                out startingLocation!,
                do_AllowObjects: false
            )) { return; }

            // continuation helper
            ActivePath fullPath = [startingLocation.PlaceName]; // list of location names
            bool do_BuildPath = true;
            void askToContinue() {
                // ask if to continue pathing
                string? input = Console.AskForInput("would you like to continue building a path (y, n, s for ⸉set to current path⸉): ")
                    ?.Trim()
                    .ToLower();
                // stop looping checks
                Console.If_InputNo(input, () => {
                    do_BuildPath = false;
                });
                if (input == "s") {
                    _moveToPath(fullPath);
                    do_BuildPath = false;
                }
            }

            // startLocName look for getting following locations
            while (do_BuildPath) {
                // get a new ending point
                if (!_getLocation_Out(
                    "pick the next stop; pick an ending location",
                    out stoppingLocation!
                )) { 
                    Console.WriteLine(); 
                    askToContinue(); 
                    continue; 
                }
                Console.WriteLine();

                // get a pathing mode
                if (!_getPathMode_Out(
                    out _pathModeSettings? settings
                )) { 
                    Console.WriteLine();
                    askToContinue();
                    continue; 
                }
                _printSplitter(_splitterSize.small);

                // try to path from start to end
                ActivePath? currPath = _findPath(settings, startingLocation, stoppingLocation);

                // mode
                ActivePath.RobustPathShowMode mode = (_do_AlwaysRobustPaths)
                    ? ActivePath.RobustPathShowMode.Show
                    : ActivePath.RobustPathShowMode.Hide;

                // if successfully pathed
                string updated = string.Empty;
                if (currPath is not null) {

                    // show path
                    Console.WriteLine("! found path !");
                    currPath.Print(mode);
                    Console.WriteLine();

                    // append to total path
                    string? input = Console.AskForInput("would you like to update the full path (y, n): ")
                        ?.Trim();
                    Console.If_InputYes(input, () => {
                        fullPath.AddRange(currPath[1..]);
                        updated = "updated ";

                        // set the starting location to the stopping location
                        startingLocation = new(currPath[^1], _searchableType.location);
                    });
                }

                // show full path
                Console.WriteLine($"⌄ full path {updated}⌄");
                fullPath.Print(mode);
                Console.WriteLine();

                // ask to continue
                askToContinue();
            }
        }

        // - Location Input Getter - 

        private enum _searchableType {
            location,
            obj,
            commonObj
        }

        private sealed record _pathPoint(
            string PlaceName,
            _searchableType Type
        );

        private static bool _getLocation_Out(
            string locationString,
            [NotNullWhen(true)] out _pathPoint? nameAndType,
            bool do_AllowObjects = true
        ) {
            // get input
            string? input = Console.AskForInput($"{locationString}: ");

            // input helper
            _pathPoint? parseInput(string? input) {
                switch (input) {
                    case null: return null;
                    case ".": return new(_currentLocationString(), _searchableType.location);
                    case "//": return new(DreamLocation.StartingLocationId, _searchableType.location);
                    case var _ when DreamLocation.LIST.ContainsKey(input): return new(input, _searchableType.location);
                    case var _ when DreamObject.LIST.ContainsKey(input): return new(input, _searchableType.commonObj);
                    case var _ when DreamLocation.OBJ_PARENT_MAP.ContainsKey(input.ToLower()): return new(input.ToLower(), _searchableType.obj);
                    default: return null;
                }
            }

            // parse location
            nameAndType = parseInput(input);
            if (nameAndType is null) {
                _printSplitter(_splitterSize.large);
                Console.WriteLine("invalid location provided");
                return false;
            }

            // alow objects check
            if (
                (!do_AllowObjects)
                && (nameAndType.Type != _searchableType.location)
            ) {
                _printSplitter(_splitterSize.large);
                Console.WriteLine("this location cannot be an object search");
                return false;
            }
            return true;
        }

        // - Mode Input Getter - 

        private enum _pathFindMode {
            shortest,
            cheapest,
            bounded
        }

        private record _pathModeSettings(
            _pathFindMode Type,
            int? Bound = null
        );

        private static _pathModeSettings? _getPathBound_Input() {
            // get input and try to return if it's a number
            string? input = Console.AskForInput("choose a cost value ceiling (int): ");
            if (input is not null) {
                // try to parse number
                int number;
                try {
                    number = int.Parse(input);
                } catch (OverflowException) {
                    _printSplitter(_splitterSize.small);
                    Console.WriteLine("cost was too large");
                    return null;
                } catch (FormatException) {
                    _printSplitter(_splitterSize.small);
                    Console.WriteLine("cost was not a number");
                    return null;
                }

                // success case
                return new _pathModeSettings(_pathFindMode.bounded, number);
            }

            // null fail
            _printSplitter(_splitterSize.small);
            Console.WriteLine("cost was not provided");
            return null;
        }

        private static bool _getPathMode_Out(
            [NotNullWhen(true)] out _pathModeSettings? pathFindSettings
        ) {
            pathFindSettings = _multiselector_Input(
                "= path finding options =",
                "choose a way to find this path (int): ",
                [
                    (
                        "Find shortest path",
                        "finds the fastest path between two points regardless of cost",
                        () => new _pathModeSettings(_pathFindMode.shortest)
                    ),
                    (
                        "Find cheapest bound path",
                        "finds the fastest path between two points as long as no one travel is more than a provided cost",
                        _getPathBound_Input
                    ),
                    (
                        "Find cheapest path",
                        "finds the cheapest costing path between two points",
                        () => new _pathModeSettings(_pathFindMode.cheapest)
                    )
                ],
                do_PrintSplitter: false
            );

            // null check
            return (pathFindSettings is not null);
        }

        // - Pathing -

        /// <summary>
        /// finds the least costly path between the startLocName and endLocName for the given graph
        /// </summary>
        /// <param name="graph"> the graph to find paths on </param>
        /// <param name="startLocName"> the starting point </param>
        /// <param name="endLocName"> the ending point </param>
        /// <returns> a list of node identifiers taken to arrive from the startLocName to endLocName ╎ includes both the start and end </returns>
        private static ActivePath? _tryPathFor(
            TryFunc<string, IEnumerable<TaggedEdge<string, int>>> pathTryer,
            string startLocName,
            string endLocName
        ) => _tryPathFor(pathTryer, startLocName, endLocName, out List<TaggedEdge<string, int>>? _);

        /// <summary>
        /// finds the least costly path between the startLocName and endLocName for the given graph
        /// </summary>
        /// <param name="graph"> the graph to find paths on </param>
        /// <param name="startLocName"> the starting point </param>
        /// <param name="endLocName"> the ending point </param>
        /// <param name="pathEdges"> the path as a list of tagged edges </param>
        /// <returns> a list of node identifiers taken to arrive from the startLocName to endLocName ╎ includes both the start and end </returns>
        private static ActivePath? _tryPathFor(
            TryFunc<string, IEnumerable<TaggedEdge<string, int>>> pathTryer,
            string startLocName,
            string endLocName,
            out List<TaggedEdge<string, int>>? pathEdges
        ) {
            // try to get path
            if (pathTryer(endLocName, out IEnumerable<TaggedEdge<string, int>> path)) {
                // converts to path of identifiers rather than tagged edges
                pathEdges = path.ToList();
                return new ActivePath([startLocName])
                    .Concat(path.Select(edge => edge.Target))
                    .ToActivePath();
            } else {
                pathEdges = null;
                return null;
            }
        }

        /// <summary>
        /// Checks if the given vertex exists on the given graph
        /// </summary>
        /// <param name="graph"> the graph to look for the vertex is </param>
        /// <param name="vertexName"> the vertex to look for in the graph </param>
        /// <returns> a boolean, true if in the graph </returns>
        private static bool _vertexExists(
            AdjacencyGraph<string, TaggedEdge<string, int>> graph,
            string vertexName
        ) {
            if (!graph.ContainsVertex(vertexName)) {
                Console.WriteLine($"graph doesn't contain vertex '{vertexName}'");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Completes a graph search for an object path ending
        /// </summary>
        /// <param name="parents"> a list of parent string names that have this object on them </param>
        /// <param name="graph"> the graph to search on </param>
        /// <param name="start"> the starting point </param>
        /// <param name="end"> the ending point ╎ expected to be an object </param>
        /// <returns> a path or null </returns>
        private static ActivePath? _objectSearch(
            string[] parents,
            AdjacencyGraph<string, TaggedEdge<string, int>> graph,
            _pathPoint start,
            _pathPoint end
        ) {
            // shortest path getter
            var pathTryer = graph.ShortestPathsDijkstra(
                (edge) => edge.Tag,
                start.PlaceName
            );

            // search from start to all parents as ends
            List<ActivePath> foundPaths = new();
            List<List<TaggedEdge<string, int>>> foundEdges = new();
            foreach (string parent in parents) {
                // check if this parent exists as an end
                if (!_vertexExists(graph, parent)) { return null; }

                // try to get a path with this end
                ActivePath? path = _tryPathFor(
                    pathTryer,
                    start.PlaceName,
                    parent,
                    out var foundEdge
                );
                if (path is not null) {
                    foundPaths.Add(path);
                    foundEdges.Add(foundEdge!.ToList());
                }
            }

            // check if any paths were found
            if (foundPaths.Count < 0) {
                Console.WriteLine(NO_PATH_FOUND_MESSAGE);
                return null;
            }

            // find the fastest path
            int lowestCostPathIndex = 0;
            int lowestCost = int.MaxValue;
            for (int i = 0; i < foundPaths.Count; i++) {
                List<TaggedEdge<string, int>> edge = foundEdges[i];

                // compare cost
                int totalCost = edge.Sum(e => e.Tag);
                if (
                    (lowestCost > totalCost)
                    || (lowestCost < 0)
                ) {
                    lowestCostPathIndex = i;
                    lowestCost = totalCost;
                }
            }

            // return lowest cost
            return foundPaths[lowestCostPathIndex];
        }

        /// <summary>
        /// Finds a path using a graph, start and end point
        /// </summary>
        /// <param name="graph"> the graph to get the path from </param>
        /// <param name="start"> the starting point </param>
        /// <param name="end"> the ending point </param>
        /// <returns> a path </returns>
        /// <exception cref="ArgumentException"> throws when _searchableType is not location, commonObj or obj </exception>
        private static ActivePath? _findPathFor(
            AdjacencyGraph<string, TaggedEdge<string, int>> graph,
            _pathPoint start,
            _pathPoint end
        ) {
            // startLocName will always be a non-object, but we will double check it
            if (start.Type != _searchableType.location) {
                throw new ArgumentException($"start cannot be a point as an object: {start.PlaceName}");
            }

            // check that start exists
            if (!_vertexExists(graph, start.PlaceName)) { return null; }

            // if the startLocName and endLocName are the same
            if (start.PlaceName == end.PlaceName) {
                return [start.PlaceName];
            }

            // switch for object search behavior
            switch (end.Type) {

                // location based search
                case _searchableType.location: {
                    // check that vertexName exists
                    if (!_vertexExists(graph, end.PlaceName)) { return null; }

                    // shortest path getter
                    var pathTryer = graph.ShortestPathsDijkstra(
                        ((edge) => edge.Tag),
                        start.PlaceName
                    );

                    // get path
                    ActivePath? path = _tryPathFor(pathTryer, start.PlaceName, end.PlaceName);
                    if (path is null) {
                        Console.WriteLine(NO_PATH_FOUND_MESSAGE);
                        return null;
                    }
                    return path;
                }

                // object based search
                case _searchableType.obj: {
                    // search for respective parents
                    ActivePath? path = _objectSearch(
                        (
                            DreamLocation.OBJ_PARENT_MAP[end.PlaceName]
                            .Select(objWithParent => objWithParent.ParentName)
                            .ToArray()
                        ),
                        graph,
                        start,
                        end
                    );
                    if (path is null) {
                        Console.WriteLine(NO_PATH_FOUND_MESSAGE);
                        return null;
                    }
                    return path;
                }

                // common object based search
                case _searchableType.commonObj: {
                    ActivePath? path = _objectSearch(
                        (
                            DreamLocation.COMMON_OBJ_PARENT_MAP[end.PlaceName]
                            .Select(objWithParent => objWithParent.ParentName)
                            .ToArray()
                        ),
                        graph,
                        start,
                        end
                    );
                    if (path is null) {
                        Console.WriteLine(NO_PATH_FOUND_MESSAGE);
                        return null;
                    }
                    return path;
                }

                default:
                    throw new ArgumentException("searchableType was not a valid type");
            }
        }

        private static AdjacencyGraph<string, TaggedEdge<string, int>> _graphRemoveUnpathable(AdjacencyGraph<string, TaggedEdge<string, int>> graph) {
            var new_graph = new AdjacencyGraph<string, TaggedEdge<string, int>>();

            // add verices
            foreach (string vertex in graph.Vertices) { new_graph.AddVertex(vertex); }

            // add edges (except unpathable)
            foreach (TaggedEdge<string, int> edge in graph.Edges) {
                if (_checkReservedLocation(edge.Target, ReservedLocationAbility.AllowsPathfinding, do_Print: false, do_Silent: true)) { continue; }
                new_graph.AddEdge(edge);
            }

            // return
            return new_graph;
        }

        private static ActivePath? _findPath(
            _pathModeSettings mode,
            _pathPoint start,
            _pathPoint end
        ) {
            switch (mode.Type) {
                case _pathFindMode.cheapest: return _findPathFor(_graphRemoveUnpathable(DreamLocation.GRAPH), start, end);
                case _pathFindMode.shortest: return _findPathFor(_graphRemoveUnpathable(DreamLocation.GRAPH_COSTLESS), start, end);
                case _pathFindMode.bounded: // create a new graph with all nodes that are greater than the bound missing
                    // add vertexes
                    var graph = new AdjacencyGraph<string, TaggedEdge<string, int>>();
                    foreach (string locationId in DreamLocation.LIST.Keys) {
                        graph.AddVertex(locationId);
                    }

                    // add edges
                    foreach ((string locationId, DreamLocation location) in DreamLocation.LIST) {
                        foreach (DreamPath path in location.Paths) {
                            if (path.Cost < mode.Bound) {
                                graph.AddEdge(new TaggedEdge<string, int>(
                                    locationId,
                                    path.LocationId,
                                    DreamLocation.GRAPH_COSTLESS_WEIGHT
                                ));
                            }
                        }
                    }

                    // run on new graph
                    return _findPathFor(_graphRemoveUnpathable(graph), start, end);
                default: throw new ArgumentException("_pathFindMode was not a valid mode");
            }
        }
    }
}
