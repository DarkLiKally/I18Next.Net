using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.Godot.Tests;

public class GodotFileBackendFixture
{
    [Fact]
    public async Task LoadNamespaceAsync_ExistingFile_ShouldReadJson()
    {
        var backend = new TestBackend();
        backend.Files["res://locales/en/translation.json"] = """{ "title": "Hello", "menu": { "start": "Start" }, "items": [ "a" ] }""";

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.Namespace.ShouldBe("translation");
        tree.GetValue("title", null).ShouldBe("Hello");
        tree.GetValue("menu.start", null).ShouldBe("Start");
        tree.GetValue("items.0", null).ShouldBe("a");
    }

    [Fact]
    public async Task LoadNamespaceAsync_RegionalLanguageWithoutFile_ShouldReadLanguagePart()
    {
        var backend = new TestBackend();
        backend.Files["res://locales/de/common.json"] = """{ "title": "Hallo" }""";

        var tree = await backend.LoadNamespaceAsync("de-DE", "common");

        tree.GetValue("title", null).ShouldBe("Hallo");
        backend.RequestedPaths.ShouldBe(["res://locales/de-DE/common.json", "res://locales/de/common.json"]);
    }

    [Fact]
    public async Task LoadNamespaceAsync_RegionalLanguageWithFile_ShouldPreferRegionalFile()
    {
        var backend = new TestBackend();
        backend.Files["res://locales/de/translation.json"] = """{ "title": "Hallo" }""";
        backend.Files["res://locales/de-AT/translation.json"] = """{ "title": "Servus" }""";

        var tree = await backend.LoadNamespaceAsync("de-AT", "translation");

        tree.GetValue("title", null).ShouldBe("Servus");
    }

    [Fact]
    public async Task LoadNamespaceAsync_MissingFile_ShouldReturnNull()
    {
        var backend = new TestBackend();

        (await backend.LoadNamespaceAsync("fr", "translation")).ShouldBeNull();
    }

    [Theory]
    [InlineData("user://translations")]
    [InlineData("user://translations/")]
    public async Task LoadNamespaceAsync_BasePath_ShouldReadFromBasePath(string basePath)
    {
        var backend = new TestBackend(basePath);

        await backend.LoadNamespaceAsync("en", "translation");

        backend.RequestedPaths.ShouldBe(["user://translations/en/translation.json"]);
    }

    [Fact]
    public async Task LoadNamespaceAsync_InvalidJson_ShouldThrow()
    {
        var backend = new TestBackend();
        backend.Files["res://locales/en/translation.json"] = "{ \"title\": ";

        await Should.ThrowAsync<JsonException>(() => backend.LoadNamespaceAsync("en", "translation"));
    }

    private sealed class TestBackend : GodotFileBackend
    {
        public TestBackend()
        {
        }

        public TestBackend(string basePath)
            : base(basePath)
        {
        }

        public Dictionary<string, string> Files { get; } = [];

        public List<string> RequestedPaths { get; } = [];

        protected override string ReadFile(string path)
        {
            RequestedPaths.Add(path);

            return Files.GetValueOrDefault(path);
        }
    }
}
