using System.IO;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Generators.Tests;

public class GeneratedAccessorsFixture
{
    private readonly I18NextNet _i18Next;

    public GeneratedAccessorsFixture()
    {
        var backend = new JsonFileBackend(Path.Combine(System.AppContext.BaseDirectory, "Locales"));
        var translator = new DefaultTranslator(backend, new TraceLogger(), new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 },
            new DefaultInterpolator(new TraceLogger()));

        _i18Next = new I18NextNet(backend, translator) { Language = "en" };
    }

    [Fact]
    public void Keys_ShouldContainNamespacedKeys()
    {
        L.Keys.Translation.Welcome.ShouldBe("translation:welcome");
        L.Keys.Translation.Item.ShouldBe("translation:item");
        L.Keys.Translation.Menu.Key.ShouldBe("translation:menu");
        L.Keys.Translation.Menu.Title.ShouldBe("translation:menu.title");
        L.Keys.Translation.Menu.Items.ShouldBe("translation:menu.items");
        L.Keys.Translation.Errors._404.ShouldBe("translation:errors.404");
        L.Keys.Translation.UserName.ShouldBe("translation:user_name");
        L.Keys.Common.Save.ShouldBe("common:save");

        _i18Next.T(L.Keys.Common.Cancel).ShouldBe("Cancel");
    }

    [Fact]
    public void Placeholders_ShouldBecomeParameters()
    {
        _i18Next.Translation().Welcome("Jane").ShouldBe("Hello Jane!");
        _i18Next.Translation().Welcome(name: "Jane", language: "de").ShouldBe("Hallo Jane!");
        _i18Next.Translation().Nested(person: "Ben").ShouldBe("Hello Ben!");
    }

    [Fact]
    public void Plurals_ShouldTakeCount()
    {
        _i18Next.Translation().Item(1).ShouldBe("1 item");
        _i18Next.Translation().Item(5).ShouldBe("5 items");
        _i18Next.Translation().Item(5, "de").ShouldBe("5 Elemente");
    }

    [Fact]
    public void OrdinalOnlyPlurals_ShouldAlwaysUseOrdinal()
    {
        _i18Next.Translation().Place(1).ShouldBe("1st place");
        _i18Next.Translation().Place(22).ShouldBe("22nd place");
        _i18Next.Translation().Place(13).ShouldBe("13th place");
        _i18Next.Translation().Place(3, "de").ShouldBe("3. Platz");
    }

    [Fact]
    public void Contexts_ShouldBeOptional()
    {
        _i18Next.Translation().Friend().ShouldBe("A friend");
        _i18Next.Translation().Friend(context: "female").ShouldBe("A girlfriend");
        _i18Next.Translation().Friend(2, "male").ShouldBe("2 boyfriends");
        _i18Next.Translation().Friend(1, "male", "de").ShouldBe("1 Freund");
    }

    [Fact]
    public void Groups_ShouldBeNested()
    {
        _i18Next.Translation().Menu.Title(new { name = "Jane" }).ShouldBe("Menu of Jane");
        _i18Next.Translation().Errors._404().ShouldBe("Not found");
        _i18Next.Translation().Errors.Class(language: "de").ShouldBe("Schlüsselwort");
        _i18Next.Translation().UserName().ShouldBe("User name");
        _i18Next.Common().Save().ShouldBe("Save");
    }

    [Fact]
    public void Arrays_ShouldReturnStringArrays()
    {
        _i18Next.Translation().Menu.Items("Jane").ShouldBe(["Home", "About Jane"]);
        _i18Next.Translation().Menu.Items("Jane", "de").ShouldBe(["Start", "Über Jane"]);
    }

    [Fact]
    public void Groups_ShouldReturnObjectsAndModels()
    {
        var menu = _i18Next.Translation().Menu.ToObject(new { user = new { name = "Jane" }, name = "Ben" });

        menu["title"].ShouldBe("Menu of Jane");
        menu["items"].ShouldBe(new object[] { "Home", "About Ben" });

        var model = _i18Next.Translation().Menu.To<MenuModel>(new { name = "Ben" }, "de");

        model.Items.ShouldBe(["Start", "Über Ben"]);
    }

    [Fact]
    public async Task AsyncAccessors_ShouldTranslate()
    {
        (await _i18Next.Translation().WelcomeAsync("Jane")).ShouldBe("Hello Jane!");
        (await _i18Next.Translation().ItemAsync(2, "de")).ShouldBe("2 Elemente");
        (await _i18Next.Translation().Menu.ItemsAsync("Jane")).ShouldBe(["Home", "About Jane"]);
        (await _i18Next.Translation().Menu.ToObjectAsync()).ShouldContainKey("title");
        (await _i18Next.Translation().Menu.ToAsync<MenuModel>(language: "de")).Items[0].ShouldBe("Start");
        (await _i18Next.Common().CancelAsync("de")).ShouldBe("Abbrechen");
    }

    private class MenuModel
    {
        public string[] Items { get; set; }

        public string Title { get; set; }
    }
}
