using System.Threading.Tasks;
using FluentAssertions;
using Localizer.Backends;
using NUnit.Framework;

namespace Localizer.Tests.Backends;

[TestFixture]
public class InMemoryBackendFixture
{
    [SetUp]
    public void SetUp()
    {
        _backend = new InMemoryBackend();

        _backend.AddTranslation("en-US", "test", "Value1", "Translated value 1");
        _backend.AddTranslation("en-US", "test", "Value2", "Translated value 2");
        _backend.AddTranslation("en-US", "test", "SectionA.Value1", "Translated section value 1");
        _backend.AddTranslation("en-US", "test", "SectionA.Value2", "Translated section value 2");

        _backend.AddTranslation("de", "test", "Value1", "Translated value 1");
        _backend.AddTranslation("de", "test", "Value2", "Translated value 2");
        _backend.AddTranslation("de", "test", "SectionA.Value1", "Translated section value 1");
        _backend.AddTranslation("de", "test", "SectionA.Value2", "Translated section value 2");
    }

    private InMemoryBackend _backend;

    [Test]
    public async Task AddTranslation_AlterExistingEntry_TranslationShouldBeAltered()
    {
        _backend.AddTranslation("de", "test", "Value2", "Altered translated value");

        var tree = await _backend.LoadNamespaceAsync("de", "test");

        tree.Should().NotBeNull();

        var value = tree.GetValue("Value2", null);

        value.Should().Be("Altered translated value");
    }

    [Test]
    public async Task AddTranslation_AlterExistingNestedEntry_TranslationShouldBeAltered()
    {
        _backend.AddTranslation("de", "test", "SectionA.Value1", "Altered nested translated value");

        var tree = await _backend.LoadNamespaceAsync("de", "test");

        tree.Should().NotBeNull();

        var value = tree.GetValue("SectionA.Value1", null);

        value.Should().Be("Altered nested translated value");
    }

    [Test]
    public async Task AddTranslation_NewNestedTranslation_TranslationShouldBeAdded()
    {
        _backend.AddTranslation("fr", "test", "SectionX.ValueX", "New nested translated value");

        var tree = await _backend.LoadNamespaceAsync("fr", "test");

        tree.Should().NotBeNull();

        var value = tree.GetValue("SectionX.ValueX", null);

        value.Should().Be("New nested translated value");
    }

    [Test]
    public async Task AddTranslation_NewTranslation_TranslationShouldBeAdded()
    {
        _backend.AddTranslation("fr", "test", "ValueX", "New translated value");

        var tree = await _backend.LoadNamespaceAsync("fr", "test");

        tree.Should().NotBeNull();

        var value = tree.GetValue("ValueX", null);

        value.Should().Be("New translated value");
    }

    [Test]
    public async Task LoadNamespaceAsync_ExtractLanguagePart_ShouldProvideTranslationsForOnlyTheLanguagePart()
    {
        var tree = await _backend.LoadNamespaceAsync("de-DE", "test");

        tree.Should().NotBeNull();

        tree.GetValue("Value1", null).Should().Be("Translated value 1");
        tree.GetValue("Value2", null).Should().Be("Translated value 2");
    }

    [Test]
    public async Task LoadNamespaceAsync_NestedKeys_ShouldProvideCorrectTranslations()
    {
        var tree = await _backend.LoadNamespaceAsync("en-US", "test");

        tree.Should().NotBeNull();

        tree.GetValue("SectionA.Value1", null).Should().Be("Translated section value 1");
        tree.GetValue("SectionA.Value2", null).Should().Be("Translated section value 2");
    }

    [Test]
    public async Task LoadNamespaceAsync_RootKeys_ShouldProvideCorrectTranslations()
    {
        var tree = await _backend.LoadNamespaceAsync("en-US", "test");

        tree.Should().NotBeNull();

        tree.GetValue("Value1", null).Should().Be("Translated value 1");
        tree.GetValue("Value2", null).Should().Be("Translated value 2");
    }
}