using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Dream_Charter {

    internal interface IDreamDocument<Extension>
        where Extension : DreamDocument<Extension>, IDreamDocument<Extension> {

        // --- VARIABLES ---

        // -- Static --

        // - Abstract -

        public abstract static string Ext { get; set; }
        public abstract static string CreationPath { get; set; }

        public static abstract string Name { get; }

        public static abstract Dictionary<string, Extension> LIST { get; }

        public static abstract Extension Parse(string? text);

        public static abstract void Reload();

        // - Concrete -

        internal static HashSet<string> USED_NAMES => Extension.LIST.Keys.ToHashSet();
    }

    /// <summary>
    /// base DreamDocument class for dream documents
    /// </summary>
    internal abstract class DreamDocument<Extension> : IParsable<Extension>, IInformationContainer
        where Extension : DreamDocument<Extension>, IDreamDocument<Extension> {

        // --- VARIABLES ---

        // -- Static --

        public static readonly string DATA_FOLDER = Path.Combine(".", "data");

        public const int SPACES_TO_TABS = 4;

        // -- Instance --

        internal required string Id { get; init; }
        public List<Information> Informations { get; } = new();

        // --- CONSTRUCTORS ---

        [SetsRequiredMembers]
        internal DreamDocument(string id) {
            this.Id = id;
        }

        // --- METHODS ---

        // -- Static --

        // - Duplciates -

        internal static void CheckDuplicate(string id) {
            if (IDreamDocument<Extension>.USED_NAMES.Contains(id)) {
                throw new InvalidOperationException($"{nameof(DreamDocument<Extension>)} with id {id} already exists; duplicate {nameof(DreamDocument<Extension>)}s cannot exist");
            }
        }

        // - List Ids -

        public static void PrintListIds() {
            Console.WriteLine($"List of {Extension.Name}⧼s⧽");
            foreach (var id in Extension.LIST.Keys) {
                Console.WriteLine(id);
            }
        }

        // - Denest -

        /// <summary>
        /// denests an object's info by the specified amount.
        /// information cannot nest negatively and will be clamped at 0
        /// </summary>
        /// <param name="amount"> amount of tabs to move back </param>
        internal void Denest(int amount) {
            for (int i = 0; i < Informations.Count; i++) {
                Informations[i].NestingLevel = Math.Max(Informations[i].NestingLevel - amount, 0);
            }
        }

        // - Parse -

        public static Extension Parse(string? text, IFormatProvider? _)
            => Extension.Parse(text);

        // - Try Parse -

        public static bool TryParse(
            [NotNullWhen(true)] string? text,
            [MaybeNullWhen(false)] out Extension result
        ) {
            try {
                result = Extension.Parse(text);
                return true;
            } catch (Exception e) when ((e is FormatException) || (e is ArgumentNullException)) {
                result = null;
                return false;
            }
        }

        public static bool TryParse(
            [NotNullWhen(true)] string? text,
            IFormatProvider? _,
            [MaybeNullWhen(false)] out Extension result
        ) => DreamDocument<Extension>.TryParse(text, out result);

        // - File Checking -

        /// <summary>
        /// runs the callback on each file
        /// </summary>
        /// <param name="callback"> the function to run on each file given ╎ the passed string is a file name </param>
        internal static void ForFilesOfExt(Action<string> callback) {
            foreach (var file in Directory.EnumerateFiles(
                DATA_FOLDER,
                $"*.{Extension.Ext}",
                SearchOption.AllDirectories
            )) { callback.Invoke(file); }
        }
    }
}
