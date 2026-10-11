using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using Shouldly;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using Xunit;

namespace I18Next.Net.Unity.Tests
{
    [Collection(nameof(I18NextUnity))]
    public class LocalizedTextFixture : IDisposable
    {
        public LocalizedTextFixture()
        {
            UnityTestEnvironment.Reset();
            UnityTestEnvironment.AddTranslations();
        }

        public void Dispose()
        {
            UnityTestEnvironment.Reset();
        }

        private static (LocalizedText Component, Text Text) CreateText(string key, Action<LocalizedText> configure = null)
        {
            var gameObject = new GameObject();
            var text = gameObject.AddComponent<Text>();
            text.text = "Designer text";

            var component = UnityTestEnvironment.AddComponent<LocalizedText>(gameObject);
            component.Key = key;
            configure?.Invoke(component);

            UnityTestEnvironment.SendMessage(component, "Start");

            return (component, text);
        }

        [Fact]
        public void Start_Key_ShouldShowTranslation()
        {
            var (_, text) = CreateText("title");

            text.text.ShouldBe("Menu");
        }

        [Fact]
        public void Start_EmptyKey_ShouldKeepText()
        {
            var (_, text) = CreateText(null);

            text.text.ShouldBe("Designer text");
        }

        [Fact]
        public void Key_BeforeStart_ShouldNotTranslate()
        {
            var gameObject = new GameObject();
            var text = gameObject.AddComponent<Text>();
            var component = UnityTestEnvironment.AddComponent<LocalizedText>(gameObject);

            component.Key = "title";

            text.text.ShouldBeNull();
        }

        [Fact]
        public void Key_Changed_ShouldTranslateAgain()
        {
            var (component, text) = CreateText("title");

            component.Key = "item";
            component.Count = 2;

            text.text.ShouldBe("2 items");
        }

        [Fact]
        public void Namespace_ShouldTranslateFromNamespace()
        {
            var (_, text) = CreateText("title", c => c.Namespace = "common");

            text.text.ShouldBe("Common menu");
        }

        [Fact]
        public void Arguments_ShouldInterpolateAndParseCount()
        {
            var (component, text) = CreateText("greeting", c => c.Arguments.Add(new LocalizedTextArgument("name", "Jane")));

            text.text.ShouldBe("Hello Jane");

            component.Key = "item";
            component.Arguments.Add(new LocalizedTextArgument("count", "1"));
            component.Refresh();

            text.text.ShouldBe("1 item");
        }

        [Fact]
        public void Arguments_WithoutName_ShouldBeIgnored()
        {
            var (_, text) = CreateText("greeting", c =>
            {
                c.Arguments.Add(new LocalizedTextArgument(null, "Jane"));
                c.Arguments.Add(null);
                c.Args = new Dictionary<string, object> { ["name"] = "John" };
            });

            text.text.ShouldBe("Hello John");
        }

        [Fact]
        public void Args_ShouldOverrideArguments()
        {
            var (component, text) = CreateText("greeting", c => c.Arguments.Add(new LocalizedTextArgument("name", "Jane")));

            component.Args = new Dictionary<string, object> { ["name"] = "John" };

            text.text.ShouldBe("Hello John");
        }

        [Fact]
        public void Args_Object_ShouldInterpolateProperties()
        {
            var (_, text) = CreateText("greeting", c => c.Args = new { name = "Jane" });

            text.text.ShouldBe("Hello Jane");
        }

        [Theory]
        [InlineData("many", "Total: many")]
        [InlineData(42, "Total: 42")]
        public void Args_Value_ShouldBeAvailableAsValue(object value, string expected)
        {
            var (_, text) = CreateText("total", c => c.Args = value);

            text.text.ShouldBe(expected);
        }

        [Theory]
        [InlineData(1, "1 item")]
        [InlineData(3, "3 items")]
        [InlineData("1", "1 item")]
        [InlineData("1.5", "1.5 items")]
        public void Count_ShouldResolvePlural(object count, string expected)
        {
            var (_, text) = CreateText("item", c => c.Count = count);

            text.text.ShouldBe(expected);
        }

        [Fact]
        public void Count_WithDictionaryArgs_ShouldNotChangeArgs()
        {
            var args = new Dictionary<string, object> { ["name"] = "Jane" };
            var (_, text) = CreateText("item", c =>
            {
                c.Args = args;
                c.Count = 2;
            });

            text.text.ShouldBe("2 items");
            args.ContainsKey("count").ShouldBeFalse();
        }

        [Fact]
        public void LanguageChanged_ShouldTranslateAgain()
        {
            var (_, text) = CreateText("title");

            I18NextUnity.Language = "de";

            text.text.ShouldBe("Menü");
        }

        [Fact]
        public void InstanceChanged_ShouldTranslateAgain()
        {
            var (_, text) = CreateText("title");
            var backend = new InMemoryBackend();
            backend.AddTranslation("en", "translation", "title", "Other menu");

            I18NextUnity.Instance = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };

            text.text.ShouldBe("Other menu");
        }

        [Fact]
        public void TranslationsChanged_ShouldTranslateAgain()
        {
            var backend = new ChangingBackend();
            I18NextUnity.Instance = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };
            var (_, text) = CreateText("title");

            text.text.ShouldBe("Menu");

            backend.Change("Changed menu");

            text.text.ShouldBe("Changed menu");
        }

        [Fact]
        public void LanguageChanged_WhileDisabled_ShouldTranslateWhenEnabled()
        {
            var (component, text) = CreateText("title");

            component.enabled = false;
            UnityTestEnvironment.SendMessage(component, "OnDisable");

            I18NextUnity.Language = "de";

            text.text.ShouldBe("Menu");

            component.enabled = true;
            UnityTestEnvironment.SendMessage(component, "OnEnable");

            text.text.ShouldBe("Menü");
        }

        [Fact]
        public void LanguageChanged_InactiveGameObject_ShouldNotTranslate()
        {
            var (component, text) = CreateText("title");

            component.gameObject.SetActive(false);
            I18NextUnity.Language = "de";

            text.text.ShouldBe("Menu");
        }

        [Fact]
        public void LanguageChanged_OnOtherThread_ShouldTranslateOnMainThread()
        {
            var context = new QueuedSynchronizationContext();
            UnityTestEnvironment.Reset(context);
            UnityTestEnvironment.AddTranslations();
            var (_, text) = CreateText("title");
            var i18Next = I18NextUnity.Instance;

            UnityTestEnvironment.RunOnOtherThread(() => i18Next.Language = "de");

            text.text.ShouldBe("Menu");

            context.RunPending();

            text.text.ShouldBe("Menü");
        }

        [Fact]
        public void LocalizedTmpText_ShouldShowTranslationInTmpText()
        {
            var gameObject = new GameObject();
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            var component = UnityTestEnvironment.AddComponent<LocalizedTmpText>(gameObject);
            component.Key = "title";

            UnityTestEnvironment.SendMessage(component, "Start");

            text.text.ShouldBe("Menu");

            I18NextUnity.Language = "de";

            text.text.ShouldBe("Menü");
        }

        [Fact]
        public void LocalizedTmpText_WithoutTmpText_ShouldNotThrow()
        {
            var component = UnityTestEnvironment.AddComponent<LocalizedTmpText>(new GameObject());
            component.Key = "title";

            Should.NotThrow(() => UnityTestEnvironment.SendMessage(component, "Start"));
        }

        private sealed class ChangingBackend : INotifyingTranslationBackend
        {
            private readonly InMemoryBackend _backend = new InMemoryBackend();

            public ChangingBackend()
            {
                _backend.AddTranslation("en", "translation", "title", "Menu");
            }

            public event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

            public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
            {
                return _backend.LoadNamespaceAsync(language, @namespace);
            }

            public void Change(string title)
            {
                _backend.AddTranslation("en", "translation", "title", title);
                TranslationsChanged?.Invoke(this, new TranslationsChangedEventArgs("en", "translation"));
            }
        }
    }
}
