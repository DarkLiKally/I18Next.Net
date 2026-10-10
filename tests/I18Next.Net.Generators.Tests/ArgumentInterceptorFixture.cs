using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using Shouldly;

using Xunit;

namespace I18Next.Net.Generators.Tests;

public class ArgumentInterceptorFixture
{
    private const string Source = """
                                  using System;
                                  using System.Threading.Tasks;
                                  using I18Next.Net;

                                  namespace App;

                                  public static class Usage
                                  {
                                      public static async Task Run(I18NextNet i18n)
                                      {
                                          II18Next shared = i18n;

                                          i18n.T("welcome", new { name = "Jane" });
                                          shared.T("en", "order", new { user = new { name = "Jane" }, count = 2, @class = "a" });
                                          await shared.Ta("welcome", new { name = "Ben" });
                                          i18n.TObject("menu", new { name = "Jane" });
                                          await i18n.TaObject("de", "menu", new { name = "Jane" });
                                          i18n.T<string[]>("menu.items", new { name = "Jane" });
                                          await shared.Ta<string[]>("de", "menu.items", new { name = "Jane" });
                                          i18n.Exists("welcome", new { count = 1 });
                                          await i18n.ExistsAsync("de", "welcome", new { count = 1 });
                                          i18n.GetFixedT("de").T("welcome", new { name = "Fixed" });
                                          i18n.T(args: new { name = "Named" }, key: "welcome");
                                      }
                                  }
                                  """;

    [Fact]
    public async Task PublishAot_ShouldConvertAnonymousArgumentsToDictionaries()
    {
        var result = Run(Source, new Dictionary<string, string> { ["build_property.PublishAot"] = "true" });

        result.Diagnostics.ShouldBeEmpty();
        result.CompilationErrors.ShouldBeEmpty();
        result.Source.ShouldContain("[global::System.Runtime.CompilerServices.InterceptsLocation(1, ");
        result.Source.ShouldContain("public static TModel Intercept");
        result.Source.ShouldContain("this global::I18Next.Net.FixedT @this");
        result.Source.ShouldContain("Cast(args, new { user = new { name = default(string) }, count = default(int), @class = default(string) })");
        result.Source.ShouldContain("[\"class\"] = value.@class");
        result.Source.Split(["InterceptsLocation(1, "], StringSplitOptions.None).Length.ShouldBe(12);

        var translator = new RecordingTranslator();
        await Execute(result.Compilation, new I18NextNet(new InMemoryBackend(), translator));

        translator.Arguments.Count.ShouldBe(11);
        translator.Arguments.ShouldAllBe(a => a is Dictionary<string, object>);
        translator.Arguments[0]["name"].ShouldBe("Jane");
        translator.Arguments[1]["user"].ShouldBeOfType<Dictionary<string, object>>()["name"].ShouldBe("Jane");
        translator.Arguments[1]["count"].ShouldBe(2);
        translator.Arguments[1]["class"].ShouldBe("a");
        translator.Arguments[2]["name"].ShouldBe("Ben");
        translator.Arguments[9]["name"].ShouldBe("Fixed");
        translator.Arguments[10]["name"].ShouldBe("Named");
    }

    [Theory]
    [InlineData("build_property.PublishTrimmed", "true", true)]
    [InlineData("build_property.IsAotCompatible", "True", true)]
    [InlineData("build_property.I18NextInterceptArguments", "true", true)]
    [InlineData("build_property.PublishAot", "false", false)]
    [InlineData("build_property.EnableTrimAnalyzer", "true", false)]
    public void BuildProperties_ShouldEnableInterceptors(string property, string value, bool expected)
    {
        var result = Run(Source, new Dictionary<string, string> { [property] = value });

        result.Sources.Length.ShouldBe(expected ? 1 : 0);
        result.CompilationErrors.ShouldBeEmpty();
    }

    [Fact]
    public void InterceptArgumentsDisabled_ShouldOverridePublishAot()
    {
        var result = Run(Source, new Dictionary<string, string>
        {
            ["build_property.PublishAot"] = "true",
            ["build_property.I18NextInterceptArguments"] = "false"
        });

        result.Sources.ShouldBeEmpty();
    }

    [Fact]
    public void UnsupportedArguments_ShouldNotBeIntercepted()
    {
        var result = Run("""
                         using System.Collections.Generic;
                         using I18Next.Net;

                         namespace App;

                         public class Usage
                         {
                             public static void Run<TValue>(II18Next i18n, TValue generic, object value)
                             {
                                 i18n.T("a", new Dictionary<string, object> { ["name"] = "x" });
                                 i18n.T("a", value);
                                 i18n.T("a", new { generic });
                                 i18n.T("a", new { hidden = new Hidden() });
                                 i18n.T("a", new { items = new[] { new { name = "x" } } });
                                 i18n.T("a");
                                 new Other().T("a", new { name = "x" });
                                 Other.Ta("a", new { name = "x" });
                             }

                             private class Hidden;
                         }

                         public class Other
                         {
                             public string T(string key, object args) => key;

                             public static string Ta(string key, object args) => key;
                         }
                         """, new Dictionary<string, string> { ["build_property.PublishAot"] = "true" });

        result.CompilationErrors.ShouldBeEmpty();
        result.Sources.ShouldBeEmpty();
    }

    [Fact]
    public void SameShape_ShouldShareConversion()
    {
        var result = Run("""
                         using I18Next.Net;

                         public static class Usage
                         {
                             public static void Run(II18Next i18n)
                             {
                                 i18n.T("a", new { name = "x" });
                                 i18n.T("b", new { name = "y" });
                                 i18n.T("en", "c", new { name = "z" });
                             }
                         }
                         """, new Dictionary<string, string> { ["build_property.PublishAot"] = "true" });

        result.CompilationErrors.ShouldBeEmpty();
        result.Source.ShouldContain("ToDictionary0");
        result.Source.ShouldNotContain("ToDictionary1");
        result.Source.ShouldContain("Intercept1");
        result.Source.ShouldNotContain("Intercept2");
    }

    private static InterceptorResult Run(string source, Dictionary<string, string> properties)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest).WithFeatures([new("InterceptorsNamespaces", ArgumentInterceptorGenerator.GeneratedNamespace)]);
        var compilation = CSharpCompilation.Create("Test" + Guid.NewGuid().ToString("N"), [CSharpSyntaxTree.ParseText(source, parseOptions, "Test.cs")],
            GeneratorTestHost.CreateCompilation("").References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create([new ArgumentInterceptorGenerator().AsSourceGenerator()], [], parseOptions, new OptionsProvider(properties));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        return new InterceptorResult(output, diagnostics, [.. output.SyntaxTrees.Skip(1).Select(t => t.ToString())],
            [.. output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)]);
    }

    private static async Task Execute(Compilation compilation, I18NextNet i18Next)
    {
        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        emitResult.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitResult.Diagnostics));

        stream.Position = 0;
        var context = new AssemblyLoadContext(null, true);

        try
        {
            var assembly = context.LoadFromStream(stream);
            var method = assembly.GetType("App.Usage")!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!;

            await (Task)method.Invoke(null, [i18Next])!;
        }
        finally
        {
            context.Unload();
        }
    }

    private sealed class InterceptorResult(Compilation compilation, ImmutableArray<Diagnostic> diagnostics, string[] sources,
        ImmutableArray<Diagnostic> compilationErrors)
    {
        public Compilation Compilation { get; } = compilation;

        public ImmutableArray<Diagnostic> CompilationErrors { get; } = compilationErrors;

        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;

        public string Source => Sources.Single();

        public string[] Sources { get; } = sources;
    }

    private sealed class OptionsProvider(Dictionary<string, string> properties) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(properties);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    private sealed class Options(Dictionary<string, string> properties) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value) => properties.TryGetValue(key, out value);
    }

    private sealed class RecordingTranslator : ITranslator
    {
        public List<IDictionary<string, object>> Arguments { get; } = [];

        public List<IPostProcessor> PostProcessors { get; } = [];

        public event EventHandler<MissingKeyEventArgs> MissingKey
        {
            add { }
            remove { }
        }

        public Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options)
        {
            Arguments.Add(args);

            return Task.FromResult(key);
        }

        public Task<IDictionary<string, object>> TranslateObjectAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options)
        {
            Arguments.Add(args);

            return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object> { ["0"] = "a" });
        }

        public Task<bool> ExistsAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options)
        {
            Arguments.Add(args);

            return Task.FromResult(true);
        }
    }
}
