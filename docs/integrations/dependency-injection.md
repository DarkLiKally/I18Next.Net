# Dependency injection and ASP.NET Core

```csharp
services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddBackend(new JsonFileBackend("wwwroot/locales"))
    .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
    .AddFormatter<MomentJsFormatter>()
    .AddPostProcessor<SprintfPostProcessor>()
    .UseDefaultLanguage("en")
    .UseDefaultNamespace("translation")
    .UseFallbackLanguage("en")
    .UseFallbackLanguagesFor("de-CH", "fr")
    .UseFallbackNamespace("common"));

services.AddControllersWithViews()
    .AddI18NextViewLocalization();
```

`IntegrateToAspNetCore` registers the `HtmlInterpolator` and detects the language of every request from the current
culture. Enable request localization to set the culture from the `Accept-Language` header:

```csharp
app.UseRequestLocalization(options => options.AddSupportedCultures("de", "en"));
```

Inject `II18Next`, `IStringLocalizer`, `IStringLocalizer<T>` or `IViewLocalizer`:

```csharp
public class HomeController : Controller
{
    private readonly IStringLocalizer<HomeController> _localizer;
    private readonly II18Next _i18n;

    public HomeController(IStringLocalizer<HomeController> localizer, II18Next i18n)
    {
        _localizer = localizer;
        _i18n = i18n;
    }

    public IActionResult About()
    {
        ViewData["Message"] = _localizer["about.description"];
        ViewData["Items"] = _i18n.T("cart.items", new { count = 3 });

        var missing = _localizer["unknown"].ResourceNotFound;   // true

        return View();
    }
}
```

```razor
@inject IViewLocalizer Localizer

<h1>@Localizer["about.title"]</h1>
```
