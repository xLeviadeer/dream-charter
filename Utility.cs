using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Dream_Charter {
    internal static class ConsoleExtension {

        // static Console extension
        extension(Console) {

            // - Ask for Input -

            public static string? AskForInput(string message) {
                Console.Write(message);
                return Console.ReadLine();
            }

            // - If Input Yes/No -

            public static void If_InputYes(string? input, Action callback) {
                switch (input) {
                    case "y":
                    case "yes":
                        callback.Invoke();
                        break;
                }
            }

            public static void If_InputNo(string? input, Action callback) {
                switch (input) {
                    case "n":
                    case "no":
                        callback.Invoke();
                        break;
                }
            }
        }
    }

    internal static class  FileExtensions {
        
        // static File extension
        extension(File) {

            // - Default File Creation -

            public static string GetOrCreateDefault(string path, string defaultValue) {
                // check existence
                if (!Path.Exists(path)) {
                    // create file and return default
                    File.WriteAllText(path, defaultValue);
                    return defaultValue;
                } else {
                    // return read value
                    return File.ReadAllText(path);
                }
            }
        }
    }
}
