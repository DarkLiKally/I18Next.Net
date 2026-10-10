using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using I18Next.Net;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;

var backend = new JsonFileBackend(Path.Combine(AppContext.BaseDirectory, "locales"));
var translator = new DefaultTranslator(backend, new TraceLogger(), new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 },
    new DefaultInterpolator(new TraceLogger()));
var i18n = new I18NextNet(backend, translator)
{
    Language = "en",
    ModelSerializerOptions = new JsonSerializerOptions { TypeInfoResolver = SampleJsonContext.Default, PropertyNameCaseInsensitive = true }
};

foreach (var language in new[] { "en", "de" })
{
    Console.WriteLine(i18n.T(language, "welcome", new { name = "Jane" }));
    Console.WriteLine(i18n.T(language, "item", new { count = 3 }));
    Console.WriteLine(i18n.T(language, "price", new { value = 1234.5 }));
    Console.WriteLine(i18n.T(language, "order", new { user = new { name = "Jane" }, date = new DateTime(2026, 10, 4) }));
    Console.WriteLine(string.Join(", ", i18n.T<string[]>(language, "menu.items")));
    Console.WriteLine(i18n.T<Menu>(language, "menu").Title);
}

internal record Menu(string Title, string[] Items);

[JsonSerializable(typeof(Menu))]
internal partial class SampleJsonContext : JsonSerializerContext;
