using System;
using System.IO;

using I18Next.Net;
using I18Next.Net.Backends;
using I18Next.Net.Formatters;
using I18Next.Net.Plugins;

var logger = new TraceLogger();
var backend = new JsonFileBackend(Path.Combine(AppContext.BaseDirectory, "locales"));
var interpolator = new DefaultInterpolator(logger);
interpolator.Formatters.Add(new DateFnsFormatter());

var translator = new DefaultTranslator(backend, logger, new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 }, interpolator);
translator.PostProcessors.Add(new SprintfPostProcessor());
translator.PostProcessors.Add(new IntervalPostProcessor());

var pseudoOptions = new PseudoLocalizationOptions { WrapStrings = true };
pseudoOptions.LanguagesToPseudo.Add("en");
translator.PostProcessors.Add(new PseudoLocalizationPostProcessor(pseudoOptions));

var i18n = new I18NextNet(backend, translator);
i18n.SetFallbackLanguages("en");

var now = new DateTime(2026, 10, 4, 14, 30, 0);

foreach (var language in new[] { "en", "de" })
{
    i18n.Language = language;

    Console.WriteLine($"--- {language} ({i18n.Dir()}) ---");

    Section("Interpolation and nesting");
    Console.WriteLine(i18n.T("welcome", new { name = "Jane", appName = "<I18Next.Net>" }));
    Console.WriteLine(i18n.T("intro"));

    Section("Plurals, ordinals and context");
    Console.WriteLine(i18n.T("inbox.unread", new { count = 0 }));
    Console.WriteLine(i18n.T("inbox.unread", new { count = 1 }));
    Console.WriteLine(i18n.T("inbox.unread", new { count = 7 }));
    Console.WriteLine(i18n.T("place", new { count = 2, ordinal = true }));
    Console.WriteLine(i18n.T("distance", new { count = 1.5 }));
    Console.WriteLine(i18n.T("friend", new { context = "female" }));

    Section("Objects and arrays");
    Console.WriteLine(i18n.T("menu.items", new { joinArrays = " | " }));
    Console.WriteLine(string.Join(", ", i18n.T<string[]>("menu.items")));
    Console.WriteLine(i18n.T<Menu>("menu", new { name = "Jane" }).Title);

    Section("Formats");
    Console.WriteLine(i18n.T("price", new { value = 1234.5 }));
    Console.WriteLine(i18n.T("amount", new { value = 1234.5678, formatParams = new { value = new { maximumFractionDigits = 1 } } }));
    Console.WriteLine(i18n.T("date", new { value = now }));
    Console.WriteLine(i18n.T("time", new { value = now }));
    Console.WriteLine(i18n.T("dateFns", new { value = now }));
    Console.WriteLine(i18n.T("updated", new { value = -1 }));
    Console.WriteLine(i18n.T("guests", new { value = new[] { "Anna", "Ben", "Carl" } }));
    Console.WriteLine(i18n.T("choice", new { value = new[] { "Tea", "Coffee" } }));
    Console.WriteLine(i18n.T("shout", new { value = "quiet" }));

    Section("Fixed translators, namespaces and fallbacks");
    var common = i18n.GetFixedT(@namespace: "common");
    Console.WriteLine($"{common.T("save")} / {common.T("cancel")}");
    Console.WriteLine(i18n.T("title", new { keyPrefix = "inbox" }));
    Console.WriteLine(i18n.T("onlyEnglish"));
    Console.WriteLine(i18n.T("missing", new { defaultValue = "Default for {{name}}", name = "Jane" }));

    Section("Post processors");
    Console.WriteLine(i18n.T("sprintf", new { postProcess = "sprintf", sprintf = new[] { "a", "b", "c" } }));
    Console.WriteLine(i18n.T("interval", new { postProcess = "interval", count = 4 }));
    Console.WriteLine(i18n.T("welcome", new { postProcess = "pseudo", name = "Jane", appName = "I18Next" }));

    Console.WriteLine();
}

return;

static void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine($"# {title}");
}

internal class Menu
{
    public string Title { get; set; }

    public string[] Items { get; set; }
}
