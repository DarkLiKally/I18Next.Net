using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace I18Next.Net.Generators.Tests;

internal static class GeneratorTestHost
{
    private static readonly Lazy<MetadataReference[]> References = new(() =>
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!.Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p)),
        MetadataReference.CreateFromFile(typeof(II18Next).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(I18NextNet).Assembly.Location)
    ]);

    public static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("Test", [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), "Test.cs")],
            References.Value, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    public static GeneratorResult Run(string source, IDictionary<string, string> files)
    {
        var compilation = CreateCompilation(source);
        var driver = CSharpGeneratorDriver.Create([new I18NextResourcesGenerator().AsSourceGenerator()], CreateAdditionalTexts(files),
            new CSharpParseOptions(LanguageVersion.Latest));

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        var result = driver.GetRunResult();

        return new GeneratorResult(
            diagnostics,
            result.GeneratedTrees.Select(t => t.ToString()).ToArray(),
            output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray());
    }

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, IDictionary<string, string> files)
    {
        var compilation = CreateCompilation(source);
        var options = new AnalyzerOptions(CreateAdditionalTexts(files));
        var analyzers = compilation.WithAnalyzers([new UnknownKeyAnalyzer()], options);

        var diagnostics = await analyzers.GetAnalyzerDiagnosticsAsync();

        return [.. diagnostics.OrderBy(d => d.Location.SourceSpan.Start)];
    }

    public static ImmutableArray<AdditionalText> CreateAdditionalTexts(IDictionary<string, string> files)
    {
        return [.. files.Select(f => (AdditionalText)new InMemoryAdditionalText(f.Key, f.Value))];
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(text, Encoding.UTF8);
        }
    }
}

internal sealed class GeneratorResult(ImmutableArray<Diagnostic> diagnostics, string[] sources, ImmutableArray<Diagnostic> compilationErrors)
{
    public ImmutableArray<Diagnostic> CompilationErrors { get; } = compilationErrors;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;

    public string Source => Sources.Single();

    public string[] Sources { get; } = sources;
}
