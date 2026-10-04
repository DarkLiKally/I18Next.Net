using System;
using System.Collections.Generic;
using System.IO;

using BenchmarkDotNet.Attributes;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

namespace I18Next.Net.Benchmarks;

[MemoryDiagnoser]
public class TranslationBenchmarks
{
    private readonly I18NextNet _i18Next;

    public TranslationBenchmarks()
    {
        var logger = new TraceLogger();
        var backend = new JsonFileBackend(Path.Combine(AppContext.BaseDirectory, "locales"));
        var translator = new DefaultTranslator(backend, logger, new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 },
            new DefaultInterpolator(logger));

        _i18Next = new I18NextNet(backend, translator) { Language = "en" };
        _i18Next.SetFallbackLanguages("en");

        foreach (var language in new[] { "en", "de" })
        {
            _i18Next.T(language, "simple");
            _i18Next.T(language, "common:save");
        }
    }

    [Benchmark(Baseline = true)]
    public string Simple()
    {
        return _i18Next.T("simple");
    }

    [Benchmark]
    public string NestedKey()
    {
        return _i18Next.T("deep.nested.key");
    }

    [Benchmark]
    public string Interpolation()
    {
        return _i18Next.T("greeting", new { name = "Jane" });
    }

    [Benchmark]
    public string MultipleInterpolations()
    {
        return _i18Next.T("multiple", new { a = "one", b = "two", c = "three" });
    }

    [Benchmark]
    public string Plural()
    {
        return _i18Next.T("item", new { count = 5 });
    }

    [Benchmark]
    public string ContextAndPlural()
    {
        return _i18Next.T("friend", new { context = "male", count = 2 });
    }

    [Benchmark]
    public string Nesting()
    {
        return _i18Next.T("nesting");
    }

    [Benchmark]
    public string NumberFormat()
    {
        return _i18Next.T("price", new { value = 1234.5 });
    }

    [Benchmark]
    public string FallbackLanguage()
    {
        return _i18Next.T("de", "onlyEnglish");
    }

    [Benchmark]
    public string Namespace()
    {
        return _i18Next.T("common:save");
    }

    [Benchmark]
    public IDictionary<string, object> ReturnObjects()
    {
        return _i18Next.TObject("menu");
    }

    [Benchmark]
    public string MissingKey()
    {
        return _i18Next.T("missing");
    }
}
