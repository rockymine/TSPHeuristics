using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TravellingSalesmanProblem.Algorithms {
    /* The worked sum a move shows: added edge lengths minus removed ones.
     *
     * Each move used to round every term to one decimal and the result separately, from the exact
     * delta, so the printed arithmetic was often false - "3.2+7.1-4.5-6.3=-0.6", where the terms
     * give -0.5 - in 73 of 140 history rows checked. A student checking a hand calculation against
     * it was told they were wrong. Terms are now shown to two decimals and the result is the sum
     * of the terms as shown, so the line is true as printed; it can differ from the exact delta
     * by at most a few hundredths. Formatting is culture invariant: in a German browser the old
     * ToString() wrote decimal commas, which KaTeX spaced like a list. */
    public static class MoveCalculation {
        public static string Format(IEnumerable<double> added, IEnumerable<double> removed) {
            var plus = added.Select(Round).ToList();
            var minus = removed.Select(Round).ToList();
            var result = Math.Round(plus.Sum() - minus.Sum(), 2) + 0d;

            var terms = string.Join("+", plus.Select(Text)) + string.Concat(minus.Select(t => "-" + Text(t)));
            return $"${terms}={Text(result)}$";
        }

        private static double Round(double value) => Math.Round(value, 2) + 0d;

        private static string Text(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
