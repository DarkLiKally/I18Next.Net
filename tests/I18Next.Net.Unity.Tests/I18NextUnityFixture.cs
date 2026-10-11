using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using NSubstitute;

using Shouldly;

using UnityEngine;

using Xunit;

namespace I18Next.Net.Unity.Tests
{
    [Collection(nameof(I18NextUnity))]
    public class I18NextUnityFixture : IDisposable
    {
        public I18NextUnityFixture()
        {
            UnityTestEnvironment.Reset();
            UnityTestEnvironment.AddTranslations();
        }

        public void Dispose()
        {
            UnityTestEnvironment.Reset();
        }

        [Fact]
        public void Instance_NotSet_ShouldTranslateFromResources()
        {
            I18NextUnity.Language.ShouldBe("en");
            I18NextUnity.T("title").ShouldBe("Menu");
            I18NextUnity.T("greeting", new Dictionary<string, object> { ["name"] = "Jane" }).ShouldBe("Hello Jane");
            I18NextUnity.T("common:title").ShouldBe("Common menu");
        }

        [Fact]
        public void Instance_NotSet_ShouldUseSystemLanguage()
        {
            Application.systemLanguage = SystemLanguage.German;

            I18NextUnity.Language.ShouldBe("de");
            I18NextUnity.T("title").ShouldBe("Menü");
        }

        [Fact]
        public void CreateDefault_UnknownSystemLanguage_ShouldUseFallbackLanguage()
        {
            Application.systemLanguage = SystemLanguage.Unknown;

            var i18Next = I18NextUnity.CreateDefault("de");

            i18Next.Language.ShouldBe("de");
            i18Next.FallbackLanguages.ShouldBe(new[] { "de" });
        }

        [Fact]
        public void CreateDefault_MissingTranslation_ShouldUseFallbackLanguage()
        {
            Application.systemLanguage = SystemLanguage.Japanese;

            var i18Next = I18NextUnity.CreateDefault();

            i18Next.Language.ShouldBe("ja");
            i18Next.T("title").ShouldBe("Menu");
        }

        [Theory]
        [InlineData(1, "1 item")]
        [InlineData(5, "5 items")]
        public void CreateDefault_Count_ShouldResolveVersion4Plurals(int count, string expected)
        {
            var i18Next = I18NextUnity.CreateDefault();

            i18Next.T("item", new Dictionary<string, object> { ["count"] = count }).ShouldBe(expected);
        }

        [Fact]
        public void CreateDefault_ShouldLogToUnityConsole()
        {
            var i18Next = I18NextUnity.CreateDefault();

            i18Next.Logger.ShouldBeOfType<UnityLogger>();
        }

        [Fact]
        public async Task CreateResourcesBackend_ShouldLoadAndUnloadTextAsset()
        {
            var backend = I18NextUnity.CreateResourcesBackend();

            var tree = await backend.LoadNamespaceAsync("de", "translation");

            tree.GetValue("title", null).ShouldBe("Menü");
            Resources.UnloadedAssets.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task CreateResourcesBackend_RegionalLanguage_ShouldFallBackToLanguagePart()
        {
            var backend = I18NextUnity.CreateResourcesBackend();

            var tree = await backend.LoadNamespaceAsync("de-AT", "translation");

            tree.GetValue("title", null).ShouldBe("Menü");
        }

        [Theory]
        [InlineData("i18n")]
        [InlineData("i18n/")]
        [InlineData("/i18n/")]
        public async Task CreateResourcesBackend_Path_ShouldLoadFromPath(string path)
        {
            UnityTestEnvironment.AddTextAsset("i18n/en/translation", "{ \"title\": \"Custom\" }");

            var backend = I18NextUnity.CreateResourcesBackend(path);

            (await backend.LoadNamespaceAsync("en", "translation")).GetValue("title", null).ShouldBe("Custom");
        }

        [Fact]
        public async Task CreateResourcesBackend_MissingNamespace_ShouldReturnNull()
        {
            var backend = I18NextUnity.CreateResourcesBackend();

            (await backend.LoadNamespaceAsync("fr", "translation")).ShouldBeNull();
            Resources.UnloadedAssets.ShouldBeEmpty();
        }

        [Fact]
        public async Task CreateStreamingAssetsBackend_ShouldReadJsonFiles()
        {
            var directory = Path.Combine(Path.GetTempPath(), "i18next-unity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(directory, "locales", "de"));
            File.WriteAllText(Path.Combine(directory, "locales", "de", "translation.json"), UnityTestEnvironment.German);
            Application.streamingAssetsPath = directory;

            try
            {
                var backend = I18NextUnity.CreateStreamingAssetsBackend();

                (await backend.LoadNamespaceAsync("de", "translation")).GetValue("title", null).ShouldBe("Menü");
                (await backend.LoadNamespaceAsync("en", "translation")).ShouldBeNull();
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void Instance_Set_ShouldRaiseTranslationsChanged()
        {
            var events = new List<TranslationsChangedEventArgs>();
            I18NextUnity.TranslationsChanged += (_, e) => events.Add(e);

            var i18Next = CreateI18Next(new InMemoryBackend());
            I18NextUnity.Instance = i18Next;
            I18NextUnity.Instance = i18Next;

            I18NextUnity.Instance.ShouldBeSameAs(i18Next);
            events.ShouldHaveSingleItem();
            events[0].Language.ShouldBeNull();
            events[0].Namespace.ShouldBeNull();
        }

        [Fact]
        public void Language_Set_ShouldRaiseLanguageChanged()
        {
            var events = new List<LanguageChangedEventArgs>();
            I18NextUnity.LanguageChanged += (_, e) => events.Add(e);

            I18NextUnity.Language = "de";

            events.ShouldHaveSingleItem();
            events[0].OldLanguage.ShouldBe("en");
            events[0].NewLanguage.ShouldBe("de");
            I18NextUnity.T("title").ShouldBe("Menü");
        }

        [Fact]
        public void Instance_Replaced_ShouldNotRaiseEventsOfPreviousInstance()
        {
            var previous = CreateI18Next(new InMemoryBackend());
            I18NextUnity.Instance = previous;
            I18NextUnity.Instance = CreateI18Next(new InMemoryBackend());

            var raised = false;
            I18NextUnity.LanguageChanged += (_, _) => raised = true;

            previous.Language = "de";

            raised.ShouldBeFalse();
        }

        [Fact]
        public void TranslationsChanged_NotifyingBackend_ShouldBeRaised()
        {
            var backend = Substitute.For<INotifyingTranslationBackend>();
            I18NextUnity.Instance = CreateI18Next(backend);

            var events = new List<TranslationsChangedEventArgs>();
            I18NextUnity.TranslationsChanged += (_, e) => events.Add(e);

            backend.TranslationsChanged += Raise.EventWith(backend, new TranslationsChangedEventArgs("en", "translation"));

            events.ShouldHaveSingleItem();
            events[0].Language.ShouldBe("en");
            events[0].Namespace.ShouldBe("translation");
        }

        [Fact]
        public void LanguageChanged_OnOtherThread_ShouldBeRaisedOnMainThread()
        {
            var context = new QueuedSynchronizationContext();
            UnityTestEnvironment.Reset(context);
            UnityTestEnvironment.AddTranslations();

            var i18Next = I18NextUnity.Instance;
            var threadIds = new List<int>();
            I18NextUnity.LanguageChanged += (_, _) => threadIds.Add(Environment.CurrentManagedThreadId);

            UnityTestEnvironment.RunOnOtherThread(() => i18Next.Language = "de");

            threadIds.ShouldBeEmpty();
            context.PendingCount.ShouldBe(1);

            context.RunPending();

            threadIds.ShouldBe(new[] { Environment.CurrentManagedThreadId });
        }

        [Fact]
        public void LanguageChanged_OnMainThread_ShouldBeRaisedImmediately()
        {
            var context = new QueuedSynchronizationContext();
            UnityTestEnvironment.Reset(context);
            UnityTestEnvironment.AddTranslations();

            var raised = false;
            I18NextUnity.LanguageChanged += (_, _) => raised = true;

            I18NextUnity.Language = "de";

            raised.ShouldBeTrue();
            context.PendingCount.ShouldBe(0);
        }

        [Fact]
        public void Initialize_ShouldResetInstanceAndEvents()
        {
            var i18Next = CreateI18Next(new InMemoryBackend());
            I18NextUnity.Instance = i18Next;

            var raised = false;
            I18NextUnity.LanguageChanged += (_, _) => raised = true;

            I18NextUnity.Initialize();
            i18Next.Language = "de";

            raised.ShouldBeFalse();
            I18NextUnity.Instance.ShouldNotBeSameAs(i18Next);
        }

        private static I18NextNet CreateI18Next(ITranslationBackend backend)
        {
            return new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };
        }
    }
}
