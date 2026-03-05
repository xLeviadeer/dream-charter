using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_Charter {
    internal static partial class Interfacing {

        // -- Multiselector --

        // - Display Options -

        /// <summary>
        /// display a list of options, numbered
        /// </summary>
        /// <param name="options"> the options list to display from </param>
        private static void _displayOptions<T>(IEnumerable<(string name, string description, Func<T> _)> options)
            where T : class? {
            int i = 0;
            foreach (var option in options) {
                Console.WriteLine($"{i}. {option.name} - {option.description}");
                i += 1;
            }
        }

        /// <summary>
        /// display a list of options, numbered
        /// </summary>
        /// <param name="options"> the options list to display from </param>
        private static void _displayOptions(IEnumerable<(string name, string description, Action _)> options) {
            int i = 0;
            foreach (var option in options) {
                Console.WriteLine($"{i}. {option.name} - {option.description}");
                i += 1;
            }
        }

        /// <summary>
        /// display a list of options
        /// </summary>
        /// <param name="options"> the options list to display from </param>
        private static void _displayOptions(IEnumerable<(string name, string description)> options) {
            foreach (var option in options) {
                Console.WriteLine($"* {option.name} - {option.description}");
            }
        }

        // - Multiselector -

        /// <summary>
        /// show a list of options and let the user select one
        /// </summary>
        /// <typeparam name="T"> the type of value which func returns </typeparam>
        /// <param name="inputMessage"> the message to show when asking the user for input </param>
        /// <param name="options"> the list of options to choose from </param>
        /// <returns> returns whatever func returns </returns>
        private static T? _multiselector_Input<T>(
            string inputMessage,
            IEnumerable<(string name, string description, Func<T> func)> options,
            bool do_PrintSplitter = true
        ) where T : class? {
            // as list
            var optionsList = options.ToList();

            // show options
            _displayOptions(optionsList);
            Console.WriteLine();

            // get selection input
            string? input = Console.AskForInput(inputMessage);
            if (input is null) { return null; }

            // try to parse selection
            int selection;
            try {
                selection = int.Parse(input);
            } catch (OverflowException) {
                _printSplitter(_splitterSize.small);
                Console.WriteLine("selection invalid, try again");
                return null;
            } catch (FormatException) {
                _printSplitter(_splitterSize.small);
                Console.WriteLine("selection invalid, try again");
                return null;
            }

            // print splitter
            if (do_PrintSplitter) { _printSplitter(_splitterSize.small); }

            // range check
            if (selection >= optionsList.Count) {
                Console.WriteLine("selection invalid, try again");
                return null;
            }

            // execute
            return optionsList[selection].func();
        }

        /// <summary>
        /// show a list of options and let the user select one
        /// </summary>
        /// <param name="inputMessage"> the message to show when asking the user for input </param>
        /// <param name="options"> the list of options to choose from </param>
        /// <returns> the input that the user typed </returns>
        private static string? _multiselector_Input(
            string inputMessage,
            IEnumerable<(string name, string description, Action action)> options,
            bool do_PrintSplitter = true
        ) {
            // as list
            var optionsList = options.ToList();

            // show options
            _displayOptions(optionsList);
            Console.WriteLine();

            // get selection input
            string? input = Console.AskForInput(inputMessage);
            if (input is null) { return null; }

            // try to parse selection
            int selection;
            try {
                selection = int.Parse(input);
            } catch (OverflowException) {
                _printSplitter(_splitterSize.small);
                Console.WriteLine("selection invalid, try again");
                return input;
            } catch (FormatException) {
                _printSplitter(_splitterSize.small);
                Console.WriteLine("selection invalid, try again");
                return input;
            }

            // print splitter
            if (do_PrintSplitter) { _printSplitter(_splitterSize.small); }

            // range check
            if (selection >= optionsList.Count) {
                Console.WriteLine("selection invalid, try again");
                return input;
            }

            // execute
            optionsList[selection].action();
            return input;
        }

        /// <summary>
        /// show a list of options and let the user select one
        /// </summary>
        /// <typeparam name="T"> the type of value which func returns </typeparam>
        /// <param name="title"> the title to display before the options list </param>
        /// <param name="inputMessage"> the message to show when asking the user for input </param>
        /// <param name="options"> the list of options to choose from </param>
        /// <returns> returns whatever func returns </returns>
        private static T? _multiselector_Input<T>(
            string title,
            string inputMessage,
            IEnumerable<(string name, string description, Func<T> func)> options,
            bool do_PrintSplitter = true
        ) where T : class? {
            // show title
            Console.WriteLine(title);

            // run non-title version
            return _multiselector_Input<T>(inputMessage, options, do_PrintSplitter);
        }

        /// <summary>
        /// show a list of options and let the user select one
        /// </summary>
        /// <param name="title"> the title to display before the options list </param>
        /// <param name="inputMessage"> the message to show when asking the user for input </param>
        /// <param name="options"> the list of options to choose from </param>
        /// <returns> returns whatever func returns </returns>
        private static string? _multiselector_Input(
            string title,
            string inputMessage,
            IEnumerable<(string name, string description, Action action)> options,
            bool do_PrintSplitter = true
        ) {
            // show title
            Console.WriteLine(title);

            // run non-title version
            return _multiselector_Input(inputMessage, options, do_PrintSplitter);
        }
    }
}
