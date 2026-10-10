# Trimming and Native AOT

`I18Next.Net`, `I18Next.Net.Abstractions`, `I18Next.Net.Extensions` and `I18Next.Net.Yaml` are trimming and Native AOT
compatible on .NET 8 and later.

## Arguments

Translation arguments are usually passed as anonymous objects:

```csharp
i18n.T("welcome", new { name = "Jane" });
```

Their properties are read with reflection, which doesn't work in trimmed applications: the trimmer removes the
properties because nothing else uses them. Add the source generator package to convert the anonymous objects into
dictionaries at compile time:

```xml
<PackageReference Include="I18Next.Net.Generators" Version="2.1.0" PrivateAssets="all" />
```

The generator replaces the calls of `T`, `Ta`, `TObject`, `TaObject`, `Exists` and `ExistsAsync` with anonymous
arguments, including nested anonymous objects, using [interceptors](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-12#interceptors).
It is enabled when `PublishAot`, `PublishTrimmed` or `IsAotCompatible` is set and requires the .NET 9 SDK or later.
Set `I18NextInterceptArguments` to `true` or `false` to enable or disable it explicitly.

Without the generator, pass dictionaries or use the [generated translation methods](/integrations/source-generator),
which don't use reflection either:

```csharp
i18n.T("welcome", new Dictionary<string, object> { ["name"] = "Jane" });
i18n.Translation().Welcome("Jane");
```

## Mapping translations to models

`T<TModel>` maps string arrays, string lists and dictionaries without reflection. Other models are mapped with
`System.Text.Json`, which needs a source generated context in trimmed applications:

```csharp
[JsonSerializable(typeof(Menu))]
internal partial class AppJsonContext : JsonSerializerContext;

i18n.ModelSerializerOptions = new JsonSerializerOptions { TypeInfoResolver = AppJsonContext.Default, PropertyNameCaseInsensitive = true };

var menu = i18n.T<Menu>("menu");
```

With dependency injection set `ModelSerializerOptions` with `Configure(o => o.ModelSerializerOptions = ...)`. Numbers
are read from strings automatically, as all translations are strings.

## Sample

[Example.NativeAot](https://github.com/DarkLiKally/I18Next.Net/tree/develop/samples/Example.NativeAot) is published as
Native AOT application in the CI build:

```sh
dotnet publish samples/Example.NativeAot -c Release -r linux-x64
```
