using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Localizer.Plugins;

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

        if (!args.ContainsKey("sprintf"))
            return value;

        if (!args["sprintf"].GetType().IsArray)
            return value;

        if (args["sprintf"] is not IEnumerable enumerable)
            return value;

        return SprintfFormatProxy(value, enumerable.Cast<object>().ToArray());
    }

    private static string SprintfFormatProxy(string input, params object[] args)
    {
        var i = 0;

        // TODO Add processing of property path
        // 'Hello %(users[0].name)s, %(users[1].name)s and %(users[2].name)s'
        input = Regex.Replace(input, "%.", m => $"{{{(++i).ToString()}}}");

        return string.Format(input, args);
    }
}
