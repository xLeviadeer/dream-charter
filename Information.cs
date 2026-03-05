using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_Charter {
    internal interface IInformationContainer {

        // --- VARIABLES ---

        // - Concrete -

        public const char PREFIX = '=';

        internal static bool DoAggressiveInlining = false;

        // - Abstract -

        List<Information> Informations { get; }

        // --- METHODS ---

        // - Concrete -

        public string ToStringInformationContainer() {
            // empty string if nothing
            if (this.Informations.Count == 0) {
                return string.Empty;

                // if nesting level 0 and only 1 info, inline
            } else if (
                (this.Informations.Count == 1)
                && DoAggressiveInlining
            ) {
                return $" = {Informations[0].Text}";

                // multiple infos
            } else {
                return Informations.Aggregate(
                    "",
                    (last_value, info_tuple) => {
                        return $"{last_value}\n{new string('\t', info_tuple.NestingLevel)}{PREFIX} {info_tuple.Text}";
                    }
                );
            }
        }
    }

    internal sealed record Information {
        internal string Text { get; set; }
        internal int NestingLevel { get; set; }

        public Information(
            string text,
            int nestingLevel
        ) {
            Text = text;
            NestingLevel = nestingLevel;
        }
    }
}
