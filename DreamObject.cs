using QuikGraph.Algorithms.Search;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dream_Charter {

    /// <summary>
    /// DreamObject class for managing a DreamObject
    /// </summary>
    internal sealed class DreamObject : DreamDocument<DreamObject>, 
        IDreamDocument<DreamObject> {

        // --- VARIABLES ---

        // -- Static --

        internal const string EXT_FILE = "common_object_extension.txt";
        internal const string DEFAULT_EXT = "dreamobject";
        public static string Ext { get; set; }

        internal const string CREATION_PATH_FILE = "common_object_creation.txt";
        internal const string DEFAULT_CREATION_PATH = "";
        public static string CreationPath { get; set; }

        public static string Name { get; } = "Common Object";

        public static Dictionary<string, DreamObject> LIST { get; } = new();

        // --- CONSTRUCTORS ---

        [SetsRequiredMembers]
        internal DreamObject(string id) 
            : base(id) { }

        // --- METHODS ---

        // -- Static --

        // - Parse -

        private enum _parseMode {
            id,
            information
        }

        private static readonly ImmutableDictionary<char, _parseMode> KEYWORD_TO_PARSE_MODE = new Dictionary<char, _parseMode>() {
            [IInformationContainer.PREFIX] = _parseMode.information
        }.ToImmutableDictionary();

        public static DreamObject Parse(string? text) {
            // null text
            if (text is null) { throw new FormatException($"provided text was null"); }

            // mode controller and object
            _parseMode currMode = _parseMode.id;
            DreamObject? parsedObj = null;
            
            // collector
                // collects a string to be used later
            StringBuilder collector = new();
            bool completed() => collector.Length == 0;

            // nesting level tracker
            const int NESTING_LEVEL_DEFAULT = 0;
            int nestingLevel = NESTING_LEVEL_DEFAULT;
            int lastNestingLevel = NESTING_LEVEL_DEFAULT;
            int secondLastNestingLevel = NESTING_LEVEL_DEFAULT;

            // finalization helper
                // completes the action for the respective current mode
                // resets the collector
            void finalize() {
                // check if the collector has nothing
                if (
                    (collector.Length == 0)
                    || collector.ToString().IsWhiteSpace()
                ) { throw new ArgumentNullException("attempting to use collector which has no value"); }
                string cleanedCollector = collector
                        .ToString()
                        .Trim()
                        .ToLower();

                // if id mode
                if (currMode == _parseMode.id) {
                    parsedObj = new DreamObject(cleanedCollector);
                    if (lastNestingLevel != NESTING_LEVEL_DEFAULT) {
                        throw new FormatException($"error finalizing obj {nameof(DreamLocation)} (collector: {collector}): the default formatting level must be 0; id must be completely un-nested");
                    }
                    goto finish;
                } else if (parsedObj is null) {
                    throw new InvalidOperationException($"error finalizing obj {nameof(DreamLocation)} (collector: {collector}): parser left id parse mode before collecting id and setting to parsedObj");
                }

                // switch over mode
                switch (currMode) {
                    case _parseMode.information: {
                        // check nesting level
                        if (
                            (lastNestingLevel != secondLastNestingLevel)
                            && (lastNestingLevel != (secondLastNestingLevel + 1))
                            && (lastNestingLevel > secondLastNestingLevel)
                            || (
                                (parsedObj.Informations.Count == 0)
                                && (lastNestingLevel != NESTING_LEVEL_DEFAULT)
                            )
                        ) { throw new FormatException($"error finalizing obj {nameof(DreamLocation)} (collector: {collector}): information must be tabbed in by 1 and be on a single line"); }

                        // add information
                        parsedObj.Informations.Add(new(cleanedCollector, lastNestingLevel));
                        break;
                    }
                }

            finish:
                // clear the collector
                    // marks as completed
                collector.Clear();
            }

            // start loop
            int lastIndex = text.Length - 1;
            bool foundNewCharactersSinceLastNewLine = false;
            for (int i = 0; i < text.Length; i++) {
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
                        throw new FormatException("tabs cannot be used inline; tabs must only prepend lines");
                    case var _ when isEmptyLine:
                        // skips whitespaces if no characters have been found on this line yet
                        continue;
                }

                // check for keywords, finalize and switch modes
                if (KEYWORD_TO_PARSE_MODE.Keys.Contains(c)) {
                    finalize();
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
                finalize(); 
            }

            // check if nothing found
            if (parsedObj == null) { throw new FormatException("the text did not contain a valid id"); }

            // return new object
            return parsedObj;
        }

        // - To String -

        public override string ToString()
            => $"{this.Id.ToUpper()}"
            + ((IInformationContainer)this).ToStringInformationContainer();

        // - Reload -

        public static void Reload() {
            // clear existing
            DreamObject.LIST.Clear();

            // create loctions folder if needed
            if (!Path.Exists(DATA_FOLDER)) {
                Directory.CreateDirectory(DATA_FOLDER);
            }

            // create extension file if needed
            Ext = File.GetOrCreateDefault(
                Path.Combine(DATA_FOLDER, EXT_FILE),
                DEFAULT_EXT
            );

            // create default path
            CreationPath = File.GetOrCreateDefault(
                Path.Combine(DATA_FOLDER, CREATION_PATH_FILE),
                DEFAULT_CREATION_PATH
            );

            // for each dream document in the directory create a variable for it in the dictionary
            ForFilesOfExt((file) => {
                // read text
                string content = File.ReadAllText(file);

                // parse
                content = content.Replace(new string(' ', SPACES_TO_TABS), "\t"); // convert spaces into tabs
                var obj = DreamObject.Parse(content);

                // add to list
                DreamObject.CheckDuplicate(obj.Id);
                DreamObject.LIST[obj.Id] = obj;
            });
        }
    }
}
