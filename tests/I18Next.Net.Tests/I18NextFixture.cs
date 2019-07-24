using System;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests
{
    [TestFixture]
    public class I18NextFixture
    {
        private InMemoryBackend _backend;
        private I18NextNet _i18next;

        [SetUp]
        public void Setup()
        {
            SetupBackend();

            var translator = new DefaultTranslator(_backend);
            _i18next = new I18NextNet(_backend, translator);

        }
        private void SetupBackend()
        {
            var backend = new InMemoryBackend();

            backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
            backend.AddTranslation("en", "translation", "exampleKey2", "My English fallback.");
            backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");

            _backend = backend;
        }

        [Test]
        public void English()
        {
            _i18next.Language = "en";
            Assert.AreEqual("My English text.", _i18next.T("exampleKey"));
        }

        [Test]
        public void German()
        {
            _i18next.Language = "de";
            Assert.AreEqual("Mein deutscher text.", _i18next.T("exampleKey"));
        }

        [Test]
        public void NoFallbackLanguage_MissingTranslation_ReturnsKey()
        {
            _i18next.Language = "de";
            Assert.AreEqual("exampleKey2", _i18next.T("exampleKey2"));
        }

        [Test]
        public void FallbackLanguageIsSet_MissingTranslation_ReturnsFallback()
        {
            _i18next.Language = "de";
            _i18next.SetFallbackLanguage("en");
            Assert.AreEqual("My English fallback.", _i18next.T("exampleKey2"));
        }

        [Test]
        public void MissingLanguage_ReturnsFallback()
        {
            _i18next.Language = "jp";
            _i18next.SetFallbackLanguage("en");
            Assert.AreEqual("My English fallback.", _i18next.T("exampleKey2"));
        }
    }
}
