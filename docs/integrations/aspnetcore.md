# ASP.NET Core

`I18Next.Net.AspNetCore` serves the translations of the registered backend to i18next in the browser, receives the
keys it reports as missing and maps localized URLs like `/de/produkte/42` to the canonical routes of the application.
Register I18Next as described in [Dependency injection](./dependency-injection) first.

## Translations for i18next in the browser

`MapI18NextResources` serves every namespace of the registered `ITranslationBackend` as i18next JSON, so the .NET
backend stays the single source of truth for server and client. Requires .NET 6 or later.

```csharp
app.MapI18NextResources("/locales/{lng}/{ns}.json", options =>
{
    options.Languages = ["en", "de"];
    options.Namespaces = ["translation", "common"];
    options.CacheControl = "no-cache";
});
```

```js
import i18next from "i18next";
import HttpBackend from "i18next-http-backend";

await i18next.use(HttpBackend).init({
    lng: "de",
    fallbackLng: "en",
    ns: ["translation", "common"],
    backend: { loadPath: "/locales/{{lng}}/{{ns}}.json" }
});
```

- The nesting and arrays of the original files are restored from the flat keys of the backend. Numbers and booleans
  are served as strings, like I18Next.Net reads them.
- Only the configured languages (case insensitive) and namespaces are served. Without a list, every language of
  letters, digits, `-` and `_` and every namespace of letters, digits, `-`, `_` and single dots is passed to the backend,
  so slashes and `..` never reach it. Configure the lists in production.
- Unknown languages or namespaces and namespaces the backend does not find return `404`.
- Responses carry an `ETag` and `If-None-Match` requests are answered with `304`. `CacheControl` defaults to
  `no-cache`, so browsers revalidate on every load. Set it to e.g. `public, max-age=3600` to cache longer, or `null`.
- The serialized JSON is cached in memory. It is invalidated when the backend reports changed translations, e.g. the
  `FileWatchingBackend` registered by `WatchTranslationFiles`, and after the `CacheExpiration` of an expiring backend.
- `GET` and `HEAD` are supported. CORS is left to the application: `app.MapI18NextResources().RequireCors("i18n")`.

When the pattern contains neither `{lng}` nor `{ns}`, both are read from the query string and several languages and
namespaces separated by `+` are returned at once, as `allowMultiLoading` with `i18next-multiload-backend-adapter`
expects (`{ "en": { "translation": { ... } } }`). At most 100 combinations can be requested at once.

```csharp
app.MapI18NextResources("/locales/resources.json");
```

```js
import MultiloadAdapter from "i18next-multiload-backend-adapter";

await i18next.use(MultiloadAdapter).init({
    backend: {
        backend: HttpBackend,
        backendOption: { loadPath: "/locales/resources.json?lng={{lng}}&ns={{ns}}", allowMultiLoading: true }
    }
});
```

## Missing keys from the browser

`MapI18NextMissingKeys` receives the keys i18next reports with `saveMissing: true` and passes each of them to the
registered [missing key handlers](../guide/missing-keys-and-logging) with a `MissingKeyEventArgs`. The sender is the
current `HttpContext`. Requires .NET 6 or later.

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapI18NextMissingKeys("/locales/add/{lng}/{ns}", options =>
    {
        options.Languages = ["en", "de"];
        options.Namespaces = ["translation"];
        options.MaxKeys = 100;
        options.MaxRequestBodySize = 64 * 1024;
    });
}
```

```js
await i18next.use(HttpBackend).init({
    saveMissing: true,
    backend: { loadPath: "/locales/{{lng}}/{{ns}}.json", addPath: "/locales/add/{{lng}}/{{ns}}" }
});
```

::: warning
The endpoint is meant for development. Anyone who can reach it can report keys, so do not expose it publicly without
authorization: `app.MapI18NextMissingKeys().RequireAuthorization()`.
:::

Only JSON objects (`Content-Type: application/json`) are accepted. The keys are the property names, the fallback values
are ignored. Unknown languages or namespaces return `404`, invalid bodies or more than `MaxKeys` keys `400` and larger
bodies `413`. Successful requests return `204`.

## Localized routes

`UseI18NextLocalizedRoutes` serves URLs like `/de/produkte/42` and `/en/products/42` from the canonical route
`/products/{id}`. The translated path segments come from a namespace of the backend, keyed by the canonical segment:

```json
// locales/de/routes.json
{
    "products": "produkte",
    "about": "über-uns"
}
```

```csharp
app.UseRequestLocalization(options =>
{
    options.AddSupportedCultures("en", "de").AddSupportedUICultures("en", "de").SetDefaultCulture("en");
    options.RequestCultureProviders.Insert(0, new PathLanguageRequestCultureProvider());
});

app.UseI18NextLocalizedRoutes(options =>
{
    options.Languages = ["en", "de"];
    options.DefaultLanguage = "en";
    options.Namespace = "routes";
    options.MissingLanguagePrefix = MissingLanguagePrefixBehavior.RedirectToDetectedLanguage;
    options.ExcludedPaths = ["/api", "/locales"];
});

app.UseRouting();

app.MapGet("/products/{id:int}", (int id, II18Next i18n) => i18n.T("product", new { id }));
```

::: tip
Minimal API applications run routing first unless `UseRouting` is called. Call it after `UseI18NextLocalizedRoutes`,
otherwise the localized paths are matched before they are translated.
:::

For a request with a language prefix the middleware

- moves the prefix to `PathBase` (`/de`) and translates the remaining segments back (`/products/42`), so endpoint
  routing, `LinkGenerator` and `Url.Content` work as usual,
- keeps segments without translation (IDs, untranslated segments, unknown paths) and the query string untouched,
- sets `CultureInfo.CurrentCulture`, `CultureInfo.CurrentUICulture` and the `IRequestCultureFeature` to the language,
  so `II18Next` with `IntegrateToAspNetCore` translates in that language.

Segments are compared case insensitive. A canonical segment still works below a language prefix (`/de/products/42`).
The translated segments are cached per language and loaded again when the backend reports changed translations.

Requests without a language prefix are passed on unchanged by default. With
`MissingLanguagePrefixBehavior.RedirectToDetectedLanguage`, `GET` and `HEAD` requests outside of the `ExcludedPaths`
are redirected (`302`) to the localized path in the language of the request culture (when request localization ran
before and a provider matched), the `Accept-Language` header or the default language.

`PathLanguageRequestCultureProvider` lets request localization take the culture from the language prefix. It works
before and after `UseI18NextLocalizedRoutes`; without it, request localization running after the middleware would
replace the culture of the prefix.

### Links and language switchers

```csharp
// in a request to /de/produkte/42
context.GetLocalizedPath("/products/7?page=2");    // /de/produkte/7?page=2, the language of the request
context.GetLocalizedPath("/about", "de");          // /de/%C3%BCber-uns
context.GetLocalizedRequestPath("en");             // /en/products/42, the current page in another language

var path = linkGenerator.GetPathByName("product", new { id = 7 });   // /products/7
context.GetLocalizedPath(path, "de");                                // /de/produkte/7
```

Paths are canonical, URL encoded and start with a slash. The path base of the application is prepended. Outside of a
request, create an `I18NextRouteLocalizer` with the backend and the options and call `LocalizePath` or
`LocalizePathAsync`.

The [`Example.MinimalApi`](https://github.com/DarkLiKally/I18Next.Net/tree/develop/samples/Example.MinimalApi) sample
shows the resources endpoint, the missing keys endpoint and localized routes.
