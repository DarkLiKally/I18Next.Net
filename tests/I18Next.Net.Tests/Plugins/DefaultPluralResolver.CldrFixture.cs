using System.Collections;
using System.IO;
using System.Text.Json;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
public class DefaultPluralResolverCldrFixture
{
    private static IEnumerable GetSamples(string kind)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFiles", "cldr", "plural-samples.json")));

        foreach (var language in document.RootElement.GetProperty(kind).EnumerateObject())
        {
            foreach (var category in language.Value.EnumerateObject())
            {
                foreach (var sample in category.Value.EnumerateArray())
                    yield return new TestCaseData(language.Name, sample.GetInt32()).Returns(category.Name).SetName($"{kind} {language.Name} {sample.GetInt32()}");
            }
        }
    }

    public static IEnumerable CardinalSamples => GetSamples("cardinal");

    public static IEnumerable OrdinalSamples => GetSamples("ordinal");

    [TestCaseSource(nameof(CardinalSamples))]
    public string GetPluralCategory_CldrSamples_ShouldMatch(string language, int count)
    {
        return DefaultPluralResolver.GetPluralCategory(language, count);
    }

    [TestCaseSource(nameof(OrdinalSamples))]
    public string GetOrdinalPluralCategory_CldrSamples_ShouldMatch(string language, int count)
    {
        return DefaultPluralResolver.GetOrdinalPluralCategory(language, count);
    }
}
