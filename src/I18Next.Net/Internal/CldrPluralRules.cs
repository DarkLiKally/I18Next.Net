using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace I18Next.Net.Internal;

internal static class CldrPluralRules
{
    private static readonly string[] CategoryOrder = ["zero", "one", "two", "few", "many"];

    private static readonly ConcurrentDictionary<string, Func<Operands, string>> Rules = new(StringComparer.OrdinalIgnoreCase);

    public static string GetCategory(string language, decimal number)
    {
        return Rules.GetOrAdd(language ?? "en", Compile)(new Operands(number));
    }

    private static Func<Operands, string> Compile(string language)
    {
        var rules = CldrData.Default.GetPluralRules(language);

        if (rules == null)
            return _ => "other";

        var conditions = CategoryOrder
            .Where(rules.ContainsKey)
            .Select(category => (Category: category, Condition: ParseCondition(rules[category])))
            .ToArray();

        return operands =>
        {
            foreach (var (category, condition) in conditions)
            {
                if (condition(operands))
                    return category;
            }

            return "other";
        };
    }

    private static Func<Operands, bool> ParseCondition(string rule)
    {
        var alternatives = Split(rule, " or ")
            .Select(alternative => Split(alternative, " and ").Select(ParseRelation).ToArray())
            .ToArray();

        return operands => alternatives.Any(relations => relations.All(relation => relation(operands)));
    }

    private static Func<Operands, bool> ParseRelation(string relation)
    {
        var negated = relation.Contains("!=");
        var parts = relation.Split([negated ? "!=" : "="], 2, StringSplitOptions.None);
        var expression = parts[0].Trim();
        var modulo = 0m;
        var moduloIndex = expression.IndexOf('%');

        if (moduloIndex > 0)
        {
            modulo = decimal.Parse(expression.Substring(moduloIndex + 1).Trim(), CultureInfo.InvariantCulture);
            expression = expression.Substring(0, moduloIndex).Trim();
        }

        var operand = expression[0];
        var ranges = parts[1].Split(',').Select(ParseRange).ToArray();

        return operands =>
        {
            var value = operands.Get(operand);

            if (modulo != 0)
                value %= modulo;

            var matches = ranges.Any(range => range.Start == range.End ? value == range.Start : value == decimal.Truncate(value) && value >= range.Start && value <= range.End);

            return matches != negated;
        };
    }

    private static (decimal Start, decimal End) ParseRange(string range)
    {
        var bounds = range.Split(["..",], StringSplitOptions.None);
        var start = decimal.Parse(bounds[0].Trim(), CultureInfo.InvariantCulture);

        return (start, bounds.Length > 1 ? decimal.Parse(bounds[1].Trim(), CultureInfo.InvariantCulture) : start);
    }

    private static IEnumerable<string> Split(string value, string separator)
    {
        return value.Split([separator], StringSplitOptions.None).Select(p => p.Trim());
    }

    private readonly struct Operands
    {
        private readonly decimal _n;
        private readonly decimal _i;
        private readonly int _v;
        private readonly int _w;
        private readonly decimal _f;
        private readonly decimal _t;

        public Operands(decimal number)
        {
            _n = Math.Abs(number);
            _i = decimal.Truncate(_n);
            _v = (decimal.GetBits(number)[3] >> 16) & 0xFF;

            var fraction = _n - _i;
            _f = decimal.Truncate(fraction * Pow10(_v));

            var t = _f;
            var w = _v;

            while (w > 0 && t % 10 == 0)
            {
                t /= 10;
                w--;
            }

            _t = t;
            _w = w;
        }

        public decimal Get(char operand)
        {
            return operand switch
            {
                'n' => _n,
                'i' => _i,
                'v' => _v,
                'w' => _w,
                'f' => _f,
                't' => _t,
                _ => 0
            };
        }

        private static decimal Pow10(int exponent)
        {
            var result = 1m;

            for (var i = 0; i < exponent; i++)
                result *= 10;

            return result;
        }
    }
}
