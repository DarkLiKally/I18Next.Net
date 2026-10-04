using System.Collections.Generic;
using System.IO;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Backends;

public class JsonPluralFormatsFixture
{
    private static I18NextNet CreateI18Next(JsonFormat jsonFormat, string directory)
    {
        var backend = new JsonFileBackend(Path.Combine("TestFiles", "plurals", directory));
        var logger = new TraceLogger();
        var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = jsonFormat };
        var translator = new DefaultTranslator(backend, logger, pluralResolver, new DefaultInterpolator(logger));

        return new I18NextNet(backend, translator);
    }

    public static IEnumerable<object[]> PluralTestData
    {
        get
        {
            // @formatter:off
            foreach (var (format, directory) in new[] { (JsonFormat.Version1, "v1"), (JsonFormat.Version2, "v2"), (JsonFormat.Version3, "v3") })
            {
                yield return new object[] { format, directory, "en", 0, "0 items" };
                yield return new object[] { format, directory, "en", 1, "1 item" };
                yield return new object[] { format, directory, "en", 2, "2 items" };
                yield return new object[] { format, directory, "en-US", 7, "7 items" };
                yield return new object[] { format, directory, "ru", 1, "1 предмет" };
                yield return new object[] { format, directory, "ru", 2, "2 предмета" };
                yield return new object[] { format, directory, "ru", 5, "5 предметов" };
                yield return new object[] { format, directory, "ru", 11, "11 предметов" };
                yield return new object[] { format, directory, "ru", 21, "21 предмет" };
                yield return new object[] { format, directory, "ru", 22, "22 предмета" };
                yield return new object[] { format, directory, "ru", 0, "0 предметов" };
                yield return new object[] { format, directory, "ar", 0, "zero" };
                yield return new object[] { format, directory, "ar", 1, "one" };
                yield return new object[] { format, directory, "ar", 2, "two" };
                yield return new object[] { format, directory, "ar", 3, "few" };
                yield return new object[] { format, directory, "ar", 11, "many" };
                yield return new object[] { format, directory, "ar", 100, "other" };
            }

            yield return new object[] { JsonFormat.Version3, "v3", "ja", 1, "1個" };
            yield return new object[] { JsonFormat.Version3, "v3", "ja", 5, "5個" };

            yield return new object[] { JsonFormat.Version4, "v4", "en", 0, "No items" };
            yield return new object[] { JsonFormat.Version4, "v4", "en", 1, "1 item" };
            yield return new object[] { JsonFormat.Version4, "v4", "en", 2, "2 items" };
            yield return new object[] { JsonFormat.Version4, "v4", "en-GB", 3, "3 items" };
            yield return new object[] { JsonFormat.Version4, "v4", "ru", 1, "1 предмет" };
            yield return new object[] { JsonFormat.Version4, "v4", "ru", 2, "2 предмета" };
            yield return new object[] { JsonFormat.Version4, "v4", "ru", 5, "5 предметов" };
            yield return new object[] { JsonFormat.Version4, "v4", "ru", 11, "11 предметов" };
            yield return new object[] { JsonFormat.Version4, "v4", "ru", 21, "21 предмет" };
            yield return new object[] { JsonFormat.Version4, "v4", "ru", 22, "22 предмета" };
            yield return new object[] { JsonFormat.Version4, "v4", "ru", 0, "0 предметов" };
            yield return new object[] { JsonFormat.Version4, "v4", "ar", 0, "zero" };
            yield return new object[] { JsonFormat.Version4, "v4", "ar", 1, "one" };
            yield return new object[] { JsonFormat.Version4, "v4", "ar", 2, "two" };
            yield return new object[] { JsonFormat.Version4, "v4", "ar", 3, "few" };
            yield return new object[] { JsonFormat.Version4, "v4", "ar", 11, "many" };
            yield return new object[] { JsonFormat.Version4, "v4", "ar", 100, "other" };
            yield return new object[] { JsonFormat.Version4, "v4", "ja", 1, "1個" };
            yield return new object[] { JsonFormat.Version4, "v4", "ja", 5, "5個" };
            // @formatter:on
        }
    }

    [Theory]
    [MemberData(nameof(PluralTestData), DisableDiscoveryEnumeration = true)]
    public void T_PluralFormat_ShouldResolveMatchingKey(JsonFormat jsonFormat, string directory, string language, int count, string expected)
    {
        var i18Next = CreateI18Next(jsonFormat, directory);

        i18Next.T(language, "item", new { count }).ShouldBe(expected);
    }

    [Theory]
    [InlineData(JsonFormat.Version1, "v1")]
    [InlineData(JsonFormat.Version2, "v2")]
    [InlineData(JsonFormat.Version3, "v3")]
    [InlineData(JsonFormat.Version4, "v4")]
    public void T_ContextAndPlural_ShouldPreferContextPluralKey(JsonFormat jsonFormat, string directory)
    {
        var i18Next = CreateI18Next(jsonFormat, directory);

        i18Next.T("en", "friend_male", new { count = 1 }).ShouldBe("1 boyfriend");
        i18Next.T("en", "friend", new { count = 1, context = "male" }).ShouldBe("1 boyfriend");
        i18Next.T("en", "friend", new { count = 4, context = "male" }).ShouldBe("4 boyfriends");
    }

    [Theory]
    [InlineData(JsonFormat.Version3, "v3", 1, "A friend")]
    [InlineData(JsonFormat.Version3, "v3", 3, "3 friends")]
    [InlineData(JsonFormat.Version4, "v4", 1, "A friend")]
    [InlineData(JsonFormat.Version4, "v4", 3, "3 friends")]
    public void T_UnknownContext_ShouldFallBackToPluralKey(JsonFormat jsonFormat, string directory, int count, string expected)
    {
        CreateI18Next(jsonFormat, directory).T("en", "friend", new { count, context = "female" }).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, "1st place")]
    [InlineData(2, "2nd place")]
    [InlineData(3, "3rd place")]
    [InlineData(4, "4th place")]
    [InlineData(11, "11th place")]
    [InlineData(22, "22nd place")]
    public void T_Version4Ordinals_ShouldResolveOrdinalKeys(int count, string expected)
    {
        CreateI18Next(JsonFormat.Version4, "v4").T("en", "place", new { count, ordinal = true }).ShouldBe(expected);
    }

    [Fact]
    public void T_MissingPluralKeys_ShouldReturnKeyAndReportPossibleKeys()
    {
        var i18Next = CreateI18Next(JsonFormat.Version4, "v4");
        var possibleKeys = new List<string>();
        ((DefaultTranslator) i18Next.Translator).MissingKey += (_, args) => possibleKeys.AddRange(args.PossibleKeys);

        i18Next.T("en", "missing", new { count = 2 }).ShouldBe("missing");

        possibleKeys.ShouldBe(new[] { "missing", "missing_other" });
    }
}
