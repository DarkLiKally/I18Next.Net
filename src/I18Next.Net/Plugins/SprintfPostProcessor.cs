using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace I18Next.Net.Plugins
{
    public class SprintfPostProcessor : IPostProcessor
    {
        public string Keyword => "sprintf";

        public string Process(string key, string value, IDictionary<string, object> args)
        {
            if (args == null)
                return value;

            if (!args.ContainsKey("sprintf"))
                return value;

            if (!args["sprintf"].GetType().IsArray)
                return value;

            var enumerable = (IEnumerable) args["sprintf"];

            return SprintfFormatProxy(value, enumerable.Cast<object>().ToArray());
        }

        private static string SprintfFormatProxy(string input, params object[] args)
        {
            var i = 0;

            input = Regex.Replace(input, "%.", m => $"{{{(++i).ToString()}}}");

            return string.Format(input, args);
        }
    }
}
