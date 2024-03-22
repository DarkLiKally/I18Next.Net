using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Localizer.Backends;
using Localizer.Plugins;

namespace Localizer.Benchmarks;

[SimpleJob(RuntimeMoniker.Net48)]
[SimpleJob(RuntimeMoniker.Net50)]
[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.NetCoreApp31)]
[MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter, RPlotExporter]
public class I18NextBenchmark
{
    private InMemoryBackend _backend;
    private I18NextNet _i18Next;

    public I18NextBenchmark()
    {
        SetupBackend();
        var translator = new DefaultTranslator(_backend);
        _i18Next = new I18NextNet(_backend, translator);
    }

    [Benchmark(Baseline = true)]
    public void WithoutArgs()
    {
        _i18Next.Language = "en";
        _i18Next.T("exampleKey1");
    }

    [Benchmark]
    public void WithSimpleArg()
    {
        _i18Next.Language = "en";
        _i18Next.T("exampleKey1", new { arg1 = "Simple Placeholder" });
    }

    private void SetupBackend()
    {
        var backend = new InMemoryBackend();

        for (var i = 0; i < 1000; i++)
        {
            backend.AddTranslation("en", "translation", $"exampleKey{i}", $"NS 1 My English text {i}.");
            backend.AddTranslation("de", "translation", $"exampleKey{i}", $"NS 1 Mein deutscher text {i}.");
            backend.AddTranslation("en", "translation2", $"exampleKey{i}", $"NS 2 My English text {i} {{{{arg1}}}}.");
            backend.AddTranslation("de", "translation2", $"exampleKey{i}", $"NS 2 Mein deutscher text {i} {{{{arg1}}}}.");
        }

        _backend = backend;
    }
}
