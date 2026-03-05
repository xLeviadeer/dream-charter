using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_Charter {
    internal static partial class Interfacing {

        // -- Create --

        private static void _create()
            => _multiselector_Input(
                "= Create New Common =",
                "choose an object type to create (int): ",
                [
                    ("Common Location", "creates a new common location", _createLocation),
                    ("Common Object", "creates a new common object", _createObject)
                ]
            );

        private static void _createLocation()
            => _createNewCommon(_commonType.location);

        private static void _createObject()
            => _createNewCommon(_commonType.obj);

        // - Creatable Types -

        private static string _nameOfType(_commonType type) {
            switch (type) {
                case _commonType.location: return "location";
                case _commonType.obj: return "object";
                default: throw new ArgumentException($"{nameof(type)} was not a valid value");
            }
        }

        private static string _nameOfType<Extension>(DreamDocument<Extension> commonDoc)
            where Extension : DreamDocument<Extension>, IDreamDocument<Extension> {
            switch (commonDoc) {
                case DreamLocation: return "location";
                case DreamObject: return "object";
                default: throw new ArgumentException($"{nameof(commonDoc)} was not a valid value");
            }
        }

        // - Creation Functions -

        private static bool _writeNewCommon<Extension>(DreamDocument<Extension> commonDoc)
            where Extension : DreamDocument<Extension>, IDreamDocument<Extension> {

            // check that the file doesn't already exist
            bool do_return_false = false;
            DreamDocument<Extension>.ForFilesOfExt((file) => {
                if (Path.GetFileNameWithoutExtension(file) == commonDoc.Id) {
                    Console.WriteLine($"could not create new common {_nameOfType(commonDoc)} '{commonDoc.Id}' because a file with the same name already exists");
                    do_return_false = true;
                    return;
                }
            });
            if (do_return_false) { return false; }

            // write
            File.WriteAllText(
                Path.Combine(DreamDocument<Extension>.DATA_FOLDER, Extension.CreationPath, $"{commonDoc.Id}.{Extension.Ext}"),
                commonDoc.ToString()
            );
            return true;
        }

        /// <summary>
        /// creates a blank common object or location
        /// </summary>
        /// <remarks>
        /// does ⊰not⊱ reload the program
        /// </remarks>
        /// <param name="type"> the type of object to create </param>
        private static void _createNewCommon(_commonType type) {
            // get name
            string? inputName = Console.AskForInput($"what is the name for the new common {_nameOfType(type)} (string): ");
            if (inputName is null) {
                Console.WriteLine("name not entered, try again");
                return;
            }

            // create new object
            bool successful = false;
            switch (type) {
                case _commonType.location:
                    successful = _writeNewCommon(new DreamLocation(inputName));
                    break;
                case _commonType.obj:
                    successful = _writeNewCommon(new DreamObject(inputName));
                    break;
            }

            // success write
            if (successful) {
                Console.WriteLine($"successfully created common {_nameOfType(type)} '{inputName}'");
            }
        }

        /// <summary>
        /// creates a common object derived from an object
        /// </summary>
        /// <remarks>
        /// does ⊰not⊱ reload the program
        /// </remarks>
        /// <param name="derivedFrom"> an object from which to create a common object from </param>
        private static void _createNewCommon(Object derivedFrom) {
            // get name
            string name = derivedFrom.Name;

            // create new object
            DreamObject commonObj = new DreamObject(name);
            foreach (var info in derivedFrom.Informations) {
                commonObj.Informations.Add(info);
            }
            commonObj.Denest(1);

            // success write
            bool successful = _writeNewCommon(commonObj);
            if (successful) {
                Console.WriteLine($"successfully created common {_nameOfType(commonObj)} '{name}'");
            }
        }
    }
}
