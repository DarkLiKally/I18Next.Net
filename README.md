# LocalizerLocalizerLocalizerLocalizerLocalizerLocalizerLocalizerLocalizerLocalizer

I18Next.Net is a port of I18Next for JavaScript. It implements most of the features provided by the original library and
enhances this by some more .Net specific features.

## Installation

Nuget:
```
Install-Package I18Next.Net
```

.NET Cli:
```
dotnet add package I18Next.Net
```

See [Nuget](https://www.nuget.org/packages/I18Next.Net/) for more information.

If you want to integrate with `IServiceCollection` also install `I18Next.Net.Extensions`
```
Install-Package I18Next.Net.Extensions
```

For further integration with AspNetCore you are also required to add the I18Next.Net.AspNetCore package.
```
Install-Package I18Next.Net.AspNetCore
```

See the usage information below for further help.

## Basic Usage

Just for now, a quick description on how to use the library in an AspNetCore MVC application.

First you have to register the required services in your application startup. If you call the `IntegrateToAspNetCore`
method you can use the default configuration for AspNetCore applications which includes an HTML-escaping interpolator
and the language detection based on the requests `Accept-Language` header (internally it is based on the current thread
language). I'll explain further below how to make the language detection work.
```csharp
services.AddI18NextLocalization(i18n => i18n.IntegrateToAspNetCore());
```

Now you're ready to go using I18Next by simply injecting `IStringLocalizer` into your controllers and services.
```csharp
// ...

private readonly IStringLocalizer<HomeController> _localizer;

public HomeController(IStringLocalizer<HomeController> localizer)
{
    _localizer = localizer;
}

public IActionResult About()
{
    ViewData["Message"] = _localizer["about.description"];

    return View();
}

// ...
```


If you also want to use [view localization](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-2.1#view-localization)
in your mvc application you should configure I18Next view localization by adding the following to your MVC builder.
```csharp
services.AddMvc()
    .AddI18NextViewLocalization();
```


In order to make the language detection work let the app use request localization. Don't forget to specify the
supported languages.
```csharp
app.UseRequestLocalization(options => options.AddSupportedCultures("de", "en"));
```

You can always take a look at the `Example.WebApp` in the `examples` directory until there is a proper documentation
about all the provided functionality.


To customize the locale file locations or use other backends just take a look at the i18next builder possibilities
provided by the service registration.
```csharp
services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddBackend(new JsonFileBackend("wwwroot/locales"))
    .UseDefaultLanguage("es"));
```
There are lots of more configuration options. More documentation will follow in the future.


## Todo

* Auto build pipeline
* Further documentation
* More tests
* More examples
* Configurable auto namespace/scope selection for `IStringLocalizer<T>`

