using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace I18Next.Net.Plugins;

public class SprintfPostProcessor : IPostProcessor
{
    public string Keyword => "sprintf";

    public string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        return value;
    }

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
