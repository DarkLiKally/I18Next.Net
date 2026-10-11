using System;
using System.Collections.Generic;

using Shouldly;

using UnityEngine;

using Xunit;

namespace I18Next.Net.Unity.Tests
{
    [Collection(nameof(I18NextUnity))]
    public class SystemLanguageDetectorFixture : IDisposable
    {
        public SystemLanguageDetectorFixture()
        {
            UnityTestEnvironment.Reset();
        }

        public void Dispose()
        {
            UnityTestEnvironment.Reset();
        }

        [Theory]
        [InlineData(SystemLanguage.English, "en")]
        [InlineData(SystemLanguage.German, "de")]
        [InlineData(SystemLanguage.ChineseSimplified, "zh-Hans")]
        [InlineData(SystemLanguage.ChineseTraditional, "zh-Hant")]
        [InlineData(SystemLanguage.Hebrew, "he")]
        [InlineData(SystemLanguage.Hindi, "hi")]
        [InlineData(SystemLanguage.Hungarian, "hu")]
        [InlineData(SystemLanguage.Norwegian, "nb")]
        [InlineData(SystemLanguage.Unknown, null)]
        public void GetLanguageCode_ShouldReturnLanguageCode(SystemLanguage language, string expected)
        {
            SystemLanguageDetector.GetLanguageCode(language).ShouldBe(expected);
        }

        [Fact]
        public void GetLanguageCode_AllLanguages_ShouldReturnDistinctCodes()
        {
            var languages = (SystemLanguage[])Enum.GetValues(typeof(SystemLanguage));
            var codes = new HashSet<string>();

            foreach (var language in languages)
            {
                if (language == SystemLanguage.Unknown)
                    continue;

                var code = SystemLanguageDetector.GetLanguageCode(language);

                code.ShouldNotBeNull(language.ToString());
                codes.Add(code).ShouldBeTrue(language.ToString());
            }
        }

        [Fact]
        public void GetLanguage_ShouldUseSystemLanguage()
        {
            Application.systemLanguage = SystemLanguage.French;

            new SystemLanguageDetector().GetLanguage().ShouldBe("fr");
        }

        [Fact]
        public void GetLanguage_UnknownSystemLanguage_ShouldReturnFallbackLanguage()
        {
            Application.systemLanguage = SystemLanguage.Unknown;

            new SystemLanguageDetector("de").GetLanguage().ShouldBe("de");
        }

        [Fact]
        public void Constructor_MissingFallbackLanguage_ShouldThrow()
        {
            Should.Throw<ArgumentNullException>(() => new SystemLanguageDetector(" "));
        }
    }
}
