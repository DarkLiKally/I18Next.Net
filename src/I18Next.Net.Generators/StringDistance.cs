using System;

namespace I18Next.Net.Generators;

internal static class StringDistance
{
    /// <summary>
    ///     Returns the optimal string alignment distance (Damerau-Levenshtein with adjacent transpositions) of two strings, or
    ///     <paramref name="maximum" /> + 1 when it is larger than <paramref name="maximum" />.
    /// </summary>
    public static int Get(string source, string target, int maximum)
    {
        if (Math.Abs(source.Length - target.Length) > maximum)
            return maximum + 1;

        var beforePrevious = new int[target.Length + 1];
        var previous = new int[target.Length + 1];
        var current = new int[target.Length + 1];

        for (var j = 0; j <= target.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            current[0] = i;
            var rowMinimum = i;

            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                var distance = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + cost);

                if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                    distance = Math.Min(distance, beforePrevious[j - 2] + 1);

                current[j] = distance;
                rowMinimum = Math.Min(rowMinimum, distance);
            }

            if (rowMinimum > maximum)
                return maximum + 1;

            (beforePrevious, previous, current) = (previous, current, beforePrevious);
        }

        return Math.Min(previous[target.Length], maximum + 1);
    }
}
