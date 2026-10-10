using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Resources;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

namespace I18Next.Net.FluentValidation.Tests;

public class ValidatorOptionsFixture
{
    private static InMemoryBackend CreateBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("de", "translation", "Name", "Vorname");
        backend.AddTranslation("de", "validation", "NotEmptyValidator", "Bitte '{{PropertyName}}' angeben.");
        backend.AddTranslation("de", "validation", "nameTooLong", "'{{PropertyName}}' darf höchstens {{MaxLength}} Zeichen haben.");

        return backend;
    }

    private static ServiceProvider CreateServices(Action<I18NextFluentValidationOptions> configure = null)
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n
            .AddBackend(CreateBackend())
            .UseDefaultLanguage("de")
            .AddFluentValidationLocalization(configure));

        return services.BuildServiceProvider();
    }

    private static string[] Validate(Person person)
    {
        return new PersonValidator().Validate(person).Errors.Select(e => e.ErrorMessage).ToArray();
    }

    [Fact]
    public void Validate_LanguageManager_ShouldTranslateMessages()
    {
        var i18Next = new I18NextNet(CreateBackend(), new DefaultTranslator(CreateBackend())) { Language = "de" };
        var languageManager = ValidatorOptions.Global.LanguageManager;
        var displayNameResolver = ValidatorOptions.Global.DisplayNameResolver;

        try
        {
            ValidatorOptions.Global.LanguageManager = new I18NextLanguageManager(i18Next);
            ValidatorOptions.Global.DisplayNameResolver = new I18NextDisplayNameResolver(i18Next).Resolve;

            Validate(new Person()).ShouldBe(["Bitte 'Vorname' angeben.", "Bitte 'Email' angeben."]);
            Validate(new Person { Name = new string('a', 30), Email = "invalid", Age = 200 }).ShouldBe([
                "'Vorname' darf höchstens 20 Zeichen haben.",
                "'Email' ist keine gültige E-Mail-Adresse.",
                "'Age' muss zwischen 0 und 120 sein. Es wurde 200 eingegeben."
            ]);
        }
        finally
        {
            ValidatorOptions.Global.LanguageManager = languageManager;
            ValidatorOptions.Global.DisplayNameResolver = displayNameResolver;
        }
    }

    [Fact]
    public async Task AddFluentValidationLocalization_HostedService_ShouldAssignAndRestoreGlobals()
    {
        Func<Type, MemberInfo, LambdaExpression, string> previousResolver = (_, member, _) => member?.Name == nameof(Person.Email) ? "E-Mail" : null;
        var languageManager = ValidatorOptions.Global.LanguageManager;
        var displayNameResolver = ValidatorOptions.Global.DisplayNameResolver;

        try
        {
            ValidatorOptions.Global.DisplayNameResolver = previousResolver;

            await using var provider = CreateServices();
            var hostedService = provider.GetServices<IHostedService>().ShouldHaveSingleItem();

            await hostedService.StartAsync(default);

            ValidatorOptions.Global.LanguageManager.ShouldBeSameAs(provider.GetRequiredService<I18NextLanguageManager>());
            provider.GetRequiredService<ILanguageManager>().ShouldBeSameAs(provider.GetRequiredService<I18NextLanguageManager>());
            Validate(new Person()).ShouldBe(["Bitte 'Vorname' angeben.", "Bitte 'E-Mail' angeben."]);

            await hostedService.StopAsync(default);

            ValidatorOptions.Global.LanguageManager.ShouldBeSameAs(languageManager);
            ValidatorOptions.Global.DisplayNameResolver.ShouldBeSameAs(previousResolver);
        }
        finally
        {
            ValidatorOptions.Global.LanguageManager = languageManager;
            ValidatorOptions.Global.DisplayNameResolver = displayNameResolver;
        }
    }

    [Fact]
    public async Task AddFluentValidationLocalization_DisplayNamesDisabled_ShouldKeepDisplayNameResolver()
    {
        var languageManager = ValidatorOptions.Global.LanguageManager;
        var displayNameResolver = ValidatorOptions.Global.DisplayNameResolver;

        try
        {
            await using var provider = CreateServices(o => o.TranslateDisplayNames = false);
            var hostedService = provider.GetServices<IHostedService>().ShouldHaveSingleItem();

            await hostedService.StartAsync(default);

            ValidatorOptions.Global.DisplayNameResolver.ShouldBeSameAs(displayNameResolver);
            Validate(new Person()).ShouldBe(["Bitte 'Name' angeben.", "Bitte 'Email' angeben."]);

            await hostedService.StopAsync(default);

            ValidatorOptions.Global.LanguageManager.ShouldBeSameAs(languageManager);
        }
        finally
        {
            ValidatorOptions.Global.LanguageManager = languageManager;
            ValidatorOptions.Global.DisplayNameResolver = displayNameResolver;
        }
    }

    [Fact]
    public async Task StopAsync_ReplacedGlobals_ShouldKeepReplacements()
    {
        var languageManager = ValidatorOptions.Global.LanguageManager;
        var displayNameResolver = ValidatorOptions.Global.DisplayNameResolver;

        try
        {
            await using var provider = CreateServices();
            var hostedService = provider.GetServices<IHostedService>().ShouldHaveSingleItem();
            var otherLanguageManager = new LanguageManager();
            Func<Type, MemberInfo, LambdaExpression, string> otherResolver = (_, _, _) => null;

            await hostedService.StartAsync(default);

            ValidatorOptions.Global.LanguageManager = otherLanguageManager;
            ValidatorOptions.Global.DisplayNameResolver = otherResolver;

            await hostedService.StopAsync(default);

            ValidatorOptions.Global.LanguageManager.ShouldBeSameAs(otherLanguageManager);
            ValidatorOptions.Global.DisplayNameResolver.ShouldBeSameAs(otherResolver);
        }
        finally
        {
            ValidatorOptions.Global.LanguageManager = languageManager;
            ValidatorOptions.Global.DisplayNameResolver = displayNameResolver;
        }
    }

    [Fact]
    public void AddFluentValidationLocalization_Configure_ShouldApplyOptions()
    {
        using var provider = CreateServices(o => o.KeyPrefix = "fluent");

        provider.GetRequiredService<I18NextLanguageManager>().Options.KeyPrefix.ShouldBe("fluent");
        provider.GetRequiredService<I18NextDisplayNameResolver>().Options.KeyPrefix.ShouldBe("fluent");
    }

    public class Person
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public int Age { get; set; }
    }

    private sealed class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(20).WithErrorCode("nameTooLong");
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Age).InclusiveBetween(0, 120);
        }
    }
}
