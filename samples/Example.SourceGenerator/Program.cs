using System;
using System.IO;

using Example.SourceGenerator;

using I18Next.Net;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;

var logger = new TraceLogger();
var backend = new JsonFileBackend(Path.Combine(AppContext.BaseDirectory, "locales"));
var translator = new DefaultTranslator(backend, logger, new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 },
    new DefaultInterpolator(logger));
var i18n = new I18NextNet(backend, translator) { Language = "en" };

foreach (var language in new[] { "en", "de" })
{
    var texts = i18n.Translation();

    Console.WriteLine(texts.Welcome(name: "Jane", language: language));
    Console.WriteLine(texts.Cart.Title(language));
    Console.WriteLine(texts.Cart.Items(count: 3, language: language));
    Console.WriteLine(texts.Cart.Total(amount: 42.5, language: language));
    Console.WriteLine(texts.Friend(context: "female", language: language));
    Console.WriteLine(string.Join(" → ", texts.Steps(language)));
    Console.WriteLine();
}

Console.WriteLine($"Key constant: {L.Keys.Translation.Cart.Title}");
