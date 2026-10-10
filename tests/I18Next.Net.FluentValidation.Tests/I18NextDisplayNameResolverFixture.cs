using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.FluentValidation.Tests;

public class I18NextDisplayNameResolverFixture
{
    public I18NextDisplayNameResolverFixture()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("de", "translation", "Name", "Vorname");
        backend.AddTranslation("de", "translation", "fields.email", "E-Mail-Adresse");
        backend.AddTranslation("de", "translation", "fields.city", "Ort");
        backend.AddTranslation("de", "fields", "Name", "Ihr Vorname");

        _i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "de" };
        _resolver = new I18NextDisplayNameResolver(_i18Next);
    }

    private readonly I18NextNet _i18Next;
    private readonly I18NextDisplayNameResolver _resolver;

    private static MemberInfo GetMember(string name)
    {
        return typeof(Person).GetProperty(name);
    }

    [Fact]
    public void Resolve_PropertyName_ShouldTranslate()
    {
        _resolver.Resolve(typeof(Person), GetMember(nameof(Person.Name)), null).ShouldBe("Vorname");
    }

    [Fact]
    public void Resolve_DisplayAttribute_ShouldUseNameAsKey()
    {
        _resolver.Resolve(typeof(Person), GetMember(nameof(Person.Email)), null).ShouldBe("E-Mail-Adresse");
    }

    [Fact]
    public void Resolve_DisplayNameAttribute_ShouldUseDisplayNameAsKey()
    {
        _resolver.Resolve(typeof(Person), GetMember(nameof(Person.City)), null).ShouldBe("Ort");
    }

    [Fact]
    public void Resolve_ResourceDisplayAttribute_ShouldUsePropertyNameAsKey()
    {
        _resolver.Resolve(typeof(Person), GetMember(nameof(Person.Resource)), null).ShouldBeNull();
    }

    [Fact]
    public void Resolve_MissingTranslation_ShouldReturnNull()
    {
        _resolver.Resolve(typeof(Person), GetMember(nameof(Person.Phone)), null).ShouldBeNull();
        _resolver.Resolve(typeof(Person), null, null).ShouldBeNull();
    }

    [Fact]
    public void Resolve_Disabled_ShouldReturnNull()
    {
        var resolver = new I18NextDisplayNameResolver(_i18Next, new I18NextFluentValidationOptions { TranslateDisplayNames = false });

        resolver.Resolve(typeof(Person), GetMember(nameof(Person.Name)), null).ShouldBeNull();
    }

    [Fact]
    public void Resolve_DisplayNameNamespace_ShouldUseNamespace()
    {
        var resolver = new I18NextDisplayNameResolver(_i18Next, new I18NextFluentValidationOptions { DisplayNameNamespace = "fields" });

        resolver.Resolve(typeof(Person), GetMember(nameof(Person.Name)), null).ShouldBe("Ihr Vorname");
    }

    [Fact]
    public void Constructor_NullArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new I18NextDisplayNameResolver(null));
        Should.Throw<ArgumentNullException>(() => new I18NextDisplayNameResolver(_i18Next, null));
    }

    public class Person
    {
        public string Name { get; set; }

        [Display(Name = "fields.email")]
        public string Email { get; set; }

        [DisplayName("fields.city")]
        public string City { get; set; }

        [Display(Name = nameof(Resources.Name), ResourceType = typeof(Resources))]
        public string Resource { get; set; }

        public string Phone { get; set; }
    }

    public static class Resources
    {
        public static string Name => "Name from resources";
    }
}
