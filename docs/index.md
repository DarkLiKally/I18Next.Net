---
layout: home

hero:
  name: I18Next.Net
  text: i18next for .NET
  tagline: The same translation files and features as i18next, with dependency injection, IStringLocalizer and ASP.NET Core integration.
  actions:
    - theme: brand
      text: Get started
      link: /guide/getting-started
    - theme: alt
      text: Feature parity
      link: /reference/parity

features:
  - title: i18next compatible
    details: JSON v1 to v4 plurals, context, nesting, fallbacks, objects and arrays, getFixedT, keyPrefix and the built-in Intl formats.
  - title: Browser identical formatting
    details: number, currency, datetime, relativetime and list formats produce the output of the browser Intl APIs, based on bundled CLDR data.
  - title: Many backends
    details: JSON, YAML, XML, INI, gettext, HTTP, in-memory and delegate backends, chained with caching and expiry.
  - title: Typed keys
    details: A source generator creates key constants and typed translation methods from your JSON files and checks them for missing keys.
  - title: Fast
    details: Translations are resolved synchronously from cached namespaces with few allocations, many times faster than i18next on Node.
  - title: .NET Standard 2.0 to .NET 10
    details: Runs on .NET Framework 4.6.2 and later, .NET 6, .NET 8, .NET 10 and .NET 11.
---
