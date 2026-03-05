using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_Charter {

    /// <summary>
    /// Adds extensions to handle conversion to active paths
    /// </summary>
    internal static class ActivePathExtension {
        extension(IEnumerable<string> pathEnumerable) {
            public ActivePath ToActivePath()
                => new ActivePath(pathEnumerable);
        }
    }

    /// <summary>
    /// A class which holds a path
    /// </summary>
    internal sealed class ActivePath : List<string> {

        // --- VARIABLES ---

        internal const char PATH_SPLIT = '>';

        // -- Indexing Wrapping --

        public ActivePath this[Range range] {
            get {
                var (start, length) = range.GetOffsetAndLength(Count);
                return new ActivePath(this.Skip(start).Take(length));
            }
        }

        public string this[Index index] {
            get => base[index.GetOffset(Count)];
            set => base[index.GetOffset(Count)] = value;
        }

        // --- CONSTRUCTORS ---

        public ActivePath() : base() { }
        public ActivePath(IEnumerable<string> collection) : base(collection) { }
        public ActivePath(int capacity) : base(capacity) { }

        // --- METHODS ---

        internal enum RobustPathShowMode {
            Hide,
            Ask,
            Show
        }

        // bool do_RobustPathAsk = true

        internal void Print(RobustPathShowMode showMode) {
            // show regular path
            Console.WriteLine(this.ToPathString());

            // check robustness
            if (showMode == RobustPathShowMode.Show) {
                // split
                Console.Write("...or ");

                // show robust path
                (string, List<DreamPath>)? robustPath = this.PathToRobustPath();
                if (robustPath is null) {
                    Console.WriteLine("could not create robust path");
                    return;
                }
                (string startingLocationId, List<DreamPath> paths) = robustPath.Value;
                Console.WriteLine(ToPathStringRobust(startingLocationId, paths));

            } else if (showMode == RobustPathShowMode.Ask) {
                // split and option
                string? input = Console.AskForInput("...would you like to see robust path (y, n): ")
                    ?.Trim()
                    .ToLower();

                // if yes
                Console.If_InputYes(input, () => {
                    // show robust path
                    (string, List<DreamPath>)? robustPath = this.PathToRobustPath();
                    if (robustPath is null) {
                        Console.WriteLine("could not create robust path");
                        return;
                    }
                    (string startingLocationId, List<DreamPath> paths) = robustPath.Value;
                    Console.WriteLine(ToPathStringRobust(startingLocationId, paths));
                });
            }
        }

        internal string ToPathString()
            => $"{ActivePath.PATH_SPLIT} {string.Join($" {ActivePath.PATH_SPLIT} ", this)}";

        internal static string ToPathStringRobust(
            string startLocationId,
            IEnumerable<DreamPath> paths
        ) {
            // check for empty paths
            if (paths.Count() == 0) {
                return $"{ActivePath.PATH_SPLIT} {startLocationId}";
            }

            // set up robust path
            List<DreamPath> pathsList = paths.ToList();
            StringBuilder robustPath = new($"{ActivePath.PATH_SPLIT} {startLocationId}");

            // if there are multiple paths
            if (pathsList.Count > 0) {
                foreach (DreamPath path in pathsList) {
                    robustPath.Append($"\n{path.ToStringPlain()}");
                }
            }

            // notNumber
            return robustPath.ToString();
        }

        internal (string startLocationId, List<DreamPath> paths)? PathToRobustPath() {
            // new path
            List<DreamPath> robustPath = [];
            string start = this[0];

            // foreach location
            if (this.Count > 0) {
                for (int i = 0; i < this.Count - 1; i++) { // doesn't iterate the last path element
                    string location = this[i];

                    // check if not a real location
                    if (!DreamLocation.LIST.ContainsKey(location)) {
                        return null;
                    }

                    // get location and target
                    DreamLocation containingLocation = DreamLocation.LIST[location];
                    string target = this[i + 1];

                    // get containing target
                    DreamPath[] containingPossibleTargets = containingLocation.Paths.Where(p => p.LocationId == target).ToArray();

                    // return based on length
                    DreamPath choosenContainingTarget;
                    if (containingPossibleTargets.Length <= 0) { // none found, error
                        return null;
                    } else if (containingPossibleTargets.Length == 1) { // exactly 1 location, return it
                        choosenContainingTarget = containingPossibleTargets[0];
                    } else { // multiple locations, return lowest cost
                        choosenContainingTarget = containingPossibleTargets[0];
                        foreach (DreamPath containingTarget in containingPossibleTargets[1..]) {
                            if (containingTarget.Cost < choosenContainingTarget.Cost) {
                                choosenContainingTarget = containingTarget;
                            }
                        }

                    }

                    // add choosen to path
                    robustPath.Add(choosenContainingTarget);
                }
            }

            // return
            return (start, robustPath);
        }
    }
}
