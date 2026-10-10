# Blazor

`I18Next.Net.Blazor` translates Blazor Server, WebAssembly, static server rendering and Auto apps with the same
i18next JSON files a JavaScript frontend uses. The `II18Next` instance is shared, the language belongs to each user: the
scoped `IBlazorI18Next` holds it per circuit on the server and per browser tab in WebAssembly.

```csharp
builder.Services.AddI18NextLocalization(i18n => i18n
    .IntegrateToBlazor(o => o.SupportedLanguages = ["en", "de"])
    .AddBackend(new JsonFileBackend(Path.Combine(builder.Environment.WebRootPath, "locales")))
    .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
    .UseDefaultLanguage("en")
    .UseFallbackLanguage("en"));
```

```razor
@* _Imports.razor *@
@using I18Next.Net.Blazor
```

`IntegrateToBlazor` registers `IBlazorI18Next` and detects the initial language from the current culture. Detected and
stored languages are matched against `SupportedLanguages`: `de-AT` becomes `de`, unknown languages become the default
language. Enable request localization so prerendering and new circuits use the language the user chose before:

```csharp
app.UseRequestLocalization(options => options
    .AddSupportedCultures("en", "de")
    .AddSupportedUICultures("en", "de")
    .SetDefaultCulture("en"));
```

## Components

Inherit from `I18NextComponentBase` to translate with `T` and render again when the language or the translations change:

```razor
@inherits I18NextComponentBase

<h1>@T("title")</h1>
<p>@T("cart.items", new { count = 3 })</p>
<p>@T("common:save")</p>
```

`Trans` renders a translation like the react-i18next component. Markup is rendered as text unless `AllowHtml` is set,
which renders the tags of `AllowedHtmlTags` (`b`, `br`, `code`, `em`, `i`, `p`, `s`, `small`, `strong`, `sub`, `sup`,
`u`) without attributes. `Components` renders named tags with your own markup:

```json
{
    "intro": "Hello <strong>{{name}}</strong>!",
    "terms": "Read the <link>terms</link>."
}
```

```razor
<Trans Key="intro" Args="@(new { name = user.Name })" AllowHtml="true" />
<Trans Key="terms" Components="Links" />
<Trans Key="save" Ns="common" />

@code {
    private static readonly Dictionary<string, RenderFragment<RenderFragment>> Links = new()
    {
        ["link"] = TermsLink
    };

    private static RenderFragment TermsLink(RenderFragment content) => @<a href="/terms">@content</a>;
}
```

Scripts, attributes and unknown tags are always rendered as text, also when they come from interpolated values.
Interpolated values may still contain the allowed tags, so keep `AllowedHtmlTags` to formatting tags.

`LanguageSelector` renders a `<select>` with the supported languages and their native names. Pass `Languages` and
`DisplayName` to change them, other attributes like `class` are applied to the `<select>`:

```razor
<LanguageSelector class="form-select" aria-label="@T("language")" />
```

## Changing the language

```razor
@inject IBlazorI18Next I18n

<button @onclick='() => I18n.ChangeLanguageAsync("de")'>Deutsch</button>
```

`ChangeLanguageAsync` loads the namespaces of the new language, renders the components again and stores the language in
the `.AspNetCore.Culture` cookie of the ASP.NET Core `CookieRequestCultureProvider` and in the `i18nextLng` local storage
key of the i18next browser language detector. It also sets `lang` and `dir` of the `<html>` element. Set
`CookieName` or `StorageKey` to `null` to disable a storage.

Each interactive runtime has its own language: components rendered statically keep the language of the request until
the next navigation, and a server circuit and the WebAssembly runtime of the same tab don't share a changed language
before the page is loaded again. Use `I18n.Language` and `I18n.Dir()` in `App.razor` to render `<html>` with the
language of the request:

```razor
@inject IBlazorI18Next I18n

<html lang="@I18n.Language" dir="@I18n.Dir()">
```

## WebAssembly

WebAssembly loads the JSON files over HTTP with the `HttpBackend`, e.g. from `wwwroot/locales/{lng}/{ns}.json` of the
server project. Translating with `T` is synchronous, and WebAssembly cannot wait for HTTP while rendering, so load the
translations before the application runs. `InitializeAsync` uses the stored language and loads the namespaces of
`Namespaces` (the default namespace if empty) of the language and its fallback languages:

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddI18NextLocalization(i18n => i18n
    .IntegrateToBlazor(o =>
    {
        o.SupportedLanguages = ["en", "de"];
        o.Namespaces = ["translation", "common"];
    })
    .AddHttpBackend(configureHttpClient: c => c.ConfigureHttpClient(h => h.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)))
    .UseDefaultLanguage("en")
    .UseFallbackLanguage("en"));

var host = builder.Build();

await host.Services.GetRequiredService<IBlazorI18Next>().InitializeAsync();
await host.RunAsync();
```

Load other namespaces before rendering them with `await I18n.LoadNamespacesAsync("admin")`, e.g. in `OnInitializedAsync`.
Translations nesting keys of namespaces that are not loaded and backends whose cache expires need to load while
rendering and don't work in WebAssembly.

## Hot reload

`WatchTranslationFiles` reloads changed translation files on the server and renders all components inheriting from
`I18NextComponentBase` again:

```csharp
builder.Services.AddI18NextLocalization(i18n =>
{
    i18n.IntegrateToBlazor()
        .AddBackend(new JsonFileBackend(locales));

    if (builder.Environment.IsDevelopment())
        i18n.WatchTranslationFiles(locales);
});
```

The [`Example.Blazor`](https://github.com/DarkLiKally/I18Next.Net/tree/develop/samples/Example.Blazor) sample shows an
interactive server page, a WebAssembly page and a statically rendered page sharing one set of translation files.
