using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using Bunit;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using Xunit;

namespace I18Next.Net.Blazor.Tests;

public class BlazorI18NextFixture : BunitContext
{
    private IBlazorI18Next I18n => Services.GetRequiredService<IBlazorI18Next>();

    private BunitJSModuleInterop SetupModule(string storedLanguage = null)
    {
        var module = JSInterop.SetupModule(BlazorI18Next.ModulePath);
        module.Setup<string>("getLanguage", "i18nextLng", ".AspNetCore.Culture").SetResult(storedLanguage);
        module.SetupVoid("setLanguage", _ => true).SetVoidResult();

        return module;
    }

    private static ITranslationBackend CreateDelayedBackend(ConcurrentDictionary<(string, string), TaskCompletionSource<ITranslationTree>> loads)
    {
        var backend = Substitute.For<ITranslationBackend>();

        backend.LoadNamespaceAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(c =>
        {
            var completion = new TaskCompletionSource<ITranslationTree>();
            loads[(c.ArgAt<string>(0), c.ArgAt<string>(1))] = completion;

            return completion.Task;
        });

        return backend;
    }

    private static async Task<TaskCompletionSource<ITranslationTree>> WaitForLoadAsync(
        ConcurrentDictionary<(string, string), TaskCompletionSource<ITranslationTree>> loads, string language, string @namespace)
    {
        for (var i = 0; i < 500 && !loads.ContainsKey((language, @namespace)); i++)
            await Task.Delay(10);

        return loads[(language, @namespace)];
    }

    private static ITranslationBackend CreateEmptyBackend()
    {
        var backend = Substitute.For<ITranslationBackend>();
        backend.LoadNamespaceAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult<ITranslationTree>(null));

        return backend;
    }

    [Theory]
    [InlineData("de", "de")]
    [InlineData("DE", "de")]
    [InlineData("de-AT", "de")]
    [InlineData("fr", "en")]
    [InlineData("", "en")]
    public void Language_DetectedLanguage_ShouldBeMatchedWithSupportedLanguages(string detectedLanguage, string expectedLanguage)
    {
        Services.AddTestI18Next(detectedLanguage: detectedLanguage);

        I18n.Language.ShouldBe(expectedLanguage);
    }

    [Theory]
    [InlineData("de", "de-DE")]
    [InlineData("de-AT", "de-DE")]
    [InlineData("en-GB", "en-US")]
    public void Language_RegionalSupportedLanguages_ShouldMatchLanguagePart(string detectedLanguage, string expectedLanguage)
    {
        Services.AddTestI18Next(detectedLanguage: detectedLanguage, configure: o => o.SupportedLanguages = ["en-US", "de-DE"]);

        I18n.Language.ShouldBe(expectedLanguage);
    }

    [Fact]
    public void Language_DefaultLanguageNotSupported_ShouldUseFirstSupportedLanguage()
    {
        Services.AddTestI18Next(detectedLanguage: "it", configure: o => o.SupportedLanguages = ["de", "fr"]);

        I18n.Language.ShouldBe("de");
    }

    [Theory]
    [InlineData("fr", "fr")]
    [InlineData("", "en")]
    public void Language_NoSupportedLanguages_ShouldUseDetectedLanguage(string detectedLanguage, string expectedLanguage)
    {
        Services.AddTestI18Next(detectedLanguage: detectedLanguage, configure: _ => { });

        I18n.Language.ShouldBe(expectedLanguage);
    }

    [Fact]
    public void T_ShouldTranslateIntoLanguageOfUser()
    {
        Services.AddTestI18Next(detectedLanguage: "de");

        I18n.T("title").ShouldBe("Willkommen");
        I18n.T("greeting", new { name = "Jane" }).ShouldBe("Hallo Jane");
        I18n.T("items", new { count = 2 }).ShouldBe("2 Elemente");
        I18n.T("common:save").ShouldBe("Speichern");
        I18n.T("onlyEnglish").ShouldBe("Only in English");
        I18n.Instance.Language.ShouldBe("en");
        I18n.Instance.T("title").ShouldBe("Welcome");
    }

    [Fact]
    public async Task T_TwoUsers_ShouldTranslateIndependently()
    {
        Services.AddTestI18Next();
        SetupModule();

        var i18Next = Services.GetRequiredService<II18Next>();
        var options = Services.GetRequiredService<IOptions<I18NextBlazorOptions>>();
        var first = new BlazorI18Next(i18Next, options, JSInterop.JSRuntime);
        var second = new BlazorI18Next(i18Next, options, JSInterop.JSRuntime);

        await first.ChangeLanguageAsync("de");

        first.T("title").ShouldBe("Willkommen");
        second.T("title").ShouldBe("Welcome");
        i18Next.Language.ShouldBe("en");
    }

    [Fact]
    public void TGeneric_ShouldMapGroup()
    {
        Services.AddTestI18Next();

        var user = I18n.T<Dictionary<string, string>>("user");

        user["name"].ShouldBe("Name");
        user["role"].ShouldBe("Role");
    }

    [Fact]
    public void Exists_ShouldCheckLanguageOfUser()
    {
        Services.AddTestI18Next(detectedLanguage: "de", configure: o => o.SupportedLanguages = ["en", "de"]);

        I18n.Exists("title").ShouldBeTrue();
        I18n.Exists("unknown").ShouldBeFalse();
    }

    [Fact]
    public void GetFixedT_ShouldUseLanguageOfUserAndNamespace()
    {
        Services.AddTestI18Next(detectedLanguage: "de");

        var t = I18n.GetFixedT("common");

        t.Language.ShouldBe("de");
        t.T("save").ShouldBe("Speichern");
        I18n.GetFixedT(keyPrefix: "user").T("name").ShouldBe("Name");
    }

    [Theory]
    [InlineData("ar", "rtl")]
    [InlineData("de", "ltr")]
    public void Dir_ShouldUseLanguageOfUser(string language, string expectedDirection)
    {
        Services.AddTestI18Next(detectedLanguage: language, configure: _ => { });

        I18n.Dir().ShouldBe(expectedDirection);
    }

    [Fact]
    public async Task ChangeLanguageAsync_ShouldRaiseLanguageChangedAndStoreLanguage()
    {
        Services.AddTestI18Next();
        var module = SetupModule();
        LanguageChangedEventArgs args = null;
        I18n.LanguageChanged += (_, e) => args = e;

        await I18n.ChangeLanguageAsync("de-CH");

        I18n.Language.ShouldBe("de");
        I18n.T("title").ShouldBe("Willkommen");
        args.ShouldNotBeNull();
        args.OldLanguage.ShouldBe("en");
        args.NewLanguage.ShouldBe("de");
        module.VerifyInvoke("setLanguage").Arguments.ShouldBe(["de", "ltr", "i18nextLng", ".AspNetCore.Culture"]);
    }

    [Fact]
    public async Task ChangeLanguageAsync_SameLanguage_ShouldOnlyStoreLanguage()
    {
        Services.AddTestI18Next();
        var module = SetupModule();
        var raised = false;
        I18n.LanguageChanged += (_, _) => raised = true;

        await I18n.ChangeLanguageAsync("en");

        raised.ShouldBeFalse();
        module.VerifyInvoke("setLanguage");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ChangeLanguageAsync_NoLanguage_ShouldThrow(string language)
    {
        Services.AddTestI18Next();

        await Should.ThrowAsync<ArgumentNullException>(() => I18n.ChangeLanguageAsync(language));
    }

    [Fact]
    public async Task ChangeLanguageAsync_ShouldLoadNamespacesBeforeChangingLanguage()
    {
        var loads = new ConcurrentDictionary<(string, string), TaskCompletionSource<ITranslationTree>>();
        var inner = TestServices.CreateBackend();
        Services.AddTestI18Next(CreateDelayedBackend(loads));
        SetupModule();

        var change = I18n.ChangeLanguageAsync("de");

        change.IsCompleted.ShouldBeFalse();
        I18n.Language.ShouldBe("en");

        (await WaitForLoadAsync(loads, "de", "translation")).SetResult(await inner.LoadNamespaceAsync("de", "translation"));
        (await WaitForLoadAsync(loads, "en", "translation")).SetResult(await inner.LoadNamespaceAsync("en", "translation"));
        await change;

        I18n.Language.ShouldBe("de");
        I18n.T("title").ShouldBe("Willkommen");
        I18n.T("onlyEnglish").ShouldBe("Only in English");
        loads.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ChangeLanguageAsync_OverlappingChanges_ShouldKeepLastLanguage()
    {
        var loads = new ConcurrentDictionary<(string, string), TaskCompletionSource<ITranslationTree>>();
        Services.AddTestI18Next(CreateDelayedBackend(loads), configure: o => o.SupportedLanguages = ["en", "de", "fr"]);
        SetupModule();

        var first = I18n.ChangeLanguageAsync("de");
        var second = I18n.ChangeLanguageAsync("fr");

        (await WaitForLoadAsync(loads, "fr", "translation")).SetResult(null);
        (await WaitForLoadAsync(loads, "en", "translation")).SetResult(null);
        await second;
        (await WaitForLoadAsync(loads, "de", "translation")).SetResult(null);
        await first;

        I18n.Language.ShouldBe("fr");
    }

    [Fact]
    public async Task ChangeLanguageAsync_JavaScriptUnavailable_ShouldChangeLanguage()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<IJSObjectReference>("import", Arg.Any<object[]>()).Throws(new InvalidOperationException("Prerendering"));
        Services.AddTestI18Next();
        Services.AddSingleton(jsRuntime);

        await I18n.ChangeLanguageAsync("de");

        I18n.Language.ShouldBe("de");
    }

    [Fact]
    public async Task ChangeLanguageAsync_Disconnected_ShouldChangeLanguage()
    {
        Services.AddTestI18Next();
        var module = JSInterop.SetupModule(BlazorI18Next.ModulePath);
        module.SetupVoid("setLanguage", _ => true).SetException(new JSDisconnectedException("Disconnected"));

        await I18n.ChangeLanguageAsync("de");

        I18n.Language.ShouldBe("de");
    }

    [Fact]
    public async Task InitializeAsync_StoredLanguage_ShouldBeUsed()
    {
        Services.AddTestI18Next();
        SetupModule("de-AT");
        var raised = false;
        I18n.LanguageChanged += (_, _) => raised = true;

        await I18n.InitializeAsync();

        I18n.Language.ShouldBe("de");
        raised.ShouldBeTrue();
    }

    [Fact]
    public async Task InitializeAsync_NoStoredLanguage_ShouldKeepDetectedLanguage()
    {
        Services.AddTestI18Next(detectedLanguage: "de");
        var module = SetupModule();

        await I18n.InitializeAsync();

        I18n.Language.ShouldBe("de");
        module.VerifyNotInvoke("setLanguage");
    }

    [Fact]
    public async Task InitializeAsync_JavaScriptUnavailable_ShouldKeepDetectedLanguage()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<IJSObjectReference>("import", Arg.Any<object[]>()).Throws(new InvalidOperationException("Prerendering"));
        Services.AddTestI18Next(detectedLanguage: "de");
        Services.AddSingleton(jsRuntime);

        await I18n.InitializeAsync();

        I18n.Language.ShouldBe("de");
    }

    [Fact]
    public async Task InitializeAsync_StorageDisabled_ShouldNotUseJavaScript()
    {
        Services.AddTestI18Next(detectedLanguage: "de", configure: o =>
        {
            o.StorageKey = null;
            o.CookieName = null;
        });

        await I18n.InitializeAsync();

        I18n.Language.ShouldBe("de");
        JSInterop.Invocations.Count.ShouldBe(0);
    }

    [Fact]
    public async Task InitializeAsync_ShouldLoadConfiguredNamespacesOfLanguageAndFallbackLanguages()
    {
        var backend = CreateEmptyBackend();
        Services.AddTestI18Next(backend, "de", o =>
        {
            o.Namespaces = ["translation", "common"];
            o.StorageKey = null;
            o.CookieName = null;
        });

        await I18n.InitializeAsync();

        await backend.Received(1).LoadNamespaceAsync("de", "translation");
        await backend.Received(1).LoadNamespaceAsync("de", "common");
        await backend.Received(1).LoadNamespaceAsync("en", "translation");
        await backend.Received(1).LoadNamespaceAsync("en", "common");
    }

    [Fact]
    public async Task LoadNamespacesAsync_ShouldLoadGivenNamespaces()
    {
        var backend = CreateEmptyBackend();
        Services.AddTestI18Next(backend, "de");

        await I18n.LoadNamespacesAsync("common");

        await backend.Received(1).LoadNamespaceAsync("de", "common");
        await backend.DidNotReceive().LoadNamespaceAsync("de", "translation");
    }

    [Fact]
    public void TranslationsChanged_ShouldBeForwardedUntilDisposed()
    {
        var backend = new NotifyingBackend(TestServices.CreateBackend());
        Services.AddTestI18Next(backend);
        var i18n = (BlazorI18Next)I18n;
        var changes = new List<TranslationsChangedEventArgs>();
        i18n.TranslationsChanged += (_, e) => changes.Add(e);

        backend.RaiseTranslationsChanged("de", "translation");
        i18n.Dispose();
        backend.RaiseTranslationsChanged("en", "translation");

        changes.Count.ShouldBe(1);
        changes[0].Language.ShouldBe("de");
        backend.SubscriberCount.ShouldBe(1);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeModule()
    {
        var module = Substitute.For<IJSObjectReference>();
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<IJSObjectReference>("import", Arg.Any<object[]>()).Returns(new ValueTask<IJSObjectReference>(module));
        Services.AddTestI18Next();
        Services.AddSingleton(jsRuntime);
        var i18n = (BlazorI18Next)I18n;

        await i18n.ChangeLanguageAsync("de");
        await i18n.DisposeAsync();
        await i18n.DisposeAsync();

        await module.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Disconnected_ShouldNotThrow()
    {
        var module = Substitute.For<IJSObjectReference>();
        module.DisposeAsync().Returns(_ => throw new JSDisconnectedException("Disconnected"));
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<IJSObjectReference>("import", Arg.Any<object[]>()).Returns(new ValueTask<IJSObjectReference>(module));
        Services.AddTestI18Next();
        Services.AddSingleton(jsRuntime);
        var i18n = (BlazorI18Next)I18n;

        await i18n.ChangeLanguageAsync("de");

        await Should.NotThrowAsync(() => i18n.DisposeAsync().AsTask());
    }

    [Fact]
    public void Constructor_MissingArguments_ShouldThrow()
    {
        var i18Next = Substitute.For<II18Next>();
        var options = Options.Create(new I18NextBlazorOptions());
        var jsRuntime = Substitute.For<IJSRuntime>();

        Should.Throw<ArgumentNullException>(() => new BlazorI18Next(null, options, jsRuntime));
        Should.Throw<ArgumentNullException>(() => new BlazorI18Next(i18Next, null, jsRuntime));
        Should.Throw<ArgumentNullException>(() => new BlazorI18Next(i18Next, options, null));
    }
}
