using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace I18Next.Net.Plugins;

/// <summary>
///     Replaces sprintf-style placeholders like <c>%s</c> or <c>%d</c> with the values of the <c>sprintf</c> array
///     argument in order, similar to the i18next-sprintf-postprocessor. Enabled per translation with the
///     <c>postProcess: "sprintf"</c> argument, e.g. <c>new { postProcess = "sprintf", sprintf = new object[] { "a", 2 } }</c>.
/// </summary>
/// <remarks>
///     Every <c>%</c> together with the following character is a placeholder; the values are formatted with the current
///     culture. Named placeholders like <c>%(name)s</c> are not supported.
/// </remarks>
public class SprintfPostProcessor : IPostProcessor
{
    /// <inheritdoc />
    public string Keyword => "sprintf";

    /// <inheritdoc />
    public string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        return value;
    }

    /// <inheritdoc />
    /// <exception cref="System.FormatException">The translation contains more placeholders than values are given.</exception>
    public string ProcessResult(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        if (args == null)
            return value;

        if (!args.TryGetValue("sprintf", out var sprintfArgs) || sprintfArgs == null)
            return value;

        if (!sprintfArgs.GetType().IsArray)
            return value;

        return sprintfArgs is not IEnumerable enumerable ? value : SprintfFormatProxy(value, [.. enumerable.Cast<object>()]);
    }

    private static string SprintfFormatProxy(string input, params object[] args)
    {
        var i = 0;

        // TODO Add processing of property path
        // 'Hello %(users[0].name)s, %(users[1].name)s and %(users[2].name)s'
        input = input.Replace("{", "{{").Replace("}", "}}");
        input = Regex.Replace(input, "%.", m => $"{{{i++}}}");

        return string.Format(input, args);
    }
}
