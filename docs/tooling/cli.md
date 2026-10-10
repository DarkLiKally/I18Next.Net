# Command-line tool and machine translation

`I18Next.Net.Tool` is a .NET tool that keeps i18next translation files in sync with the code: it extracts keys, checks
the files in CI, sorts them, converts between formats, migrates old plurals and fills missing translations with
machine translation. It works on the `{path}/{lng}/{ns}.json` layout the `JsonFileBackend` reads.

```sh
dotnet new tool-manifest            # once per repository
dotnet tool install I18Next.Net.Tool
dotnet i18next --help
```

Every command has `--help`. The exit code is `0` when everything is fine, `1` when problems were found or files would
change in `--check` mode and `2` for invalid arguments, unreadable files or failed requests.

These options select the translation files of most commands:

| Option | Default | Description |
|---|---|---|
| `-p`, `--path` | `locales` | The directory with the `{lng}/{ns}.json` files |
| `-l`, `--languages` | all language directories | The languages to process, e.g. `-l de,fr` |
| `-n`, `--namespaces` | all namespace files | The namespaces to process |
| `--key-separator` | `.` | The separator of nested keys, an empty string or `false` for flat keys |

The files are written as UTF-8 without byte order mark with two spaces indentation. Key order and nesting are kept and
the line endings of existing files are preserved.

## extract

```sh
dotnet i18next extract --source src --path src/MyApp/locales
dotnet i18next extract --check      # in CI: fails when keys are missing in the files
```

Scans C# files with Roslyn and Razor (`.cshtml`, `.razor`) files with regular expressions and adds missing keys to the
files of all languages. It finds

- `T`, `Ta`, `TObject`, `TaObject`, `T<TModel>`, `Ta<TModel>`, `Exists` and `ExistsAsync` calls with a string literal key,
  also with the language and namespace arguments like `T("de", "common", "key")`
- variables, fields and properties created with `GetFixedT(...)` or `new FixedT(...)`, applying their namespace and key prefix
- `localizer["key"]` indexers of `IStringLocalizer` and `IHtmlLocalizer` variables whose name contains `localizer`
- the `keyPrefix`, `context` and `count` arguments of anonymous objects and dictionaries

A key like `common:save` goes to the `common` namespace, other keys to `--default-namespace` (`translation`). A call
with a `count` gets the plural keys of each language, e.g. `item_one` and `item_other` in English and `item_one`,
`item_few`, `item_many` and `item_other` in Russian (`item_ordinal_...` with `ordinal = true`). For an array of
fallback keys the last key is extracted. Keys built at runtime cannot be found.

| Option | Description |
|---|---|
| `-s`, `--source` | Files or directories to scan, the current directory by default. `bin`, `obj` and `node_modules` are skipped |
| `--default-value empty\|key` | The value of new keys, an empty string by default |
| `--remove-unused` | Removes keys which are not used. Plural and context variants, children of used keys and keys nested with `$t(...)` count as used |
| `--functions` | Additional method names, e.g. `--functions Translate,Tr` |
| `--localizers` | Additional names of variables with a key indexer, e.g. `--localizers L` |
| `--namespace-separator` | The namespace separator, `:` by default |
| `--check`, `--dry-run` | Writes nothing and fails when files would change |

## check

```sh
dotnet i18next check --reference en --source src
```

Compares all languages with the reference language (`en` or the first language by default) and reports

- missing translations, including the plural forms each language needs
- empty values
- placeholder mismatches, e.g. a translation using `{{nmae}}` instead of `{{name}}` or missing a `$t(key)` nesting.
  `{{count}}` is ignored in plural forms, since `_one` forms often leave it out
- with `--source`, keys of the reference language which are not used in the code and keys used in the code which are
  missing in the reference language

```text
Comparing de, ru with en.
Missing translations (1):
  ru  translation:item_many
Placeholder mismatches (1):
  de  translation:greeting  missing {{name}}; unexpected {{nmae}}
2 problems found.
```

## sort

```sh
dotnet i18next sort
dotnet i18next sort --check
```

Sorts the keys of all objects ordinally and normalizes the formatting. Arrays keep their order.

## convert

```sh
dotnet i18next convert Resources/Strings.de.resx locales/de/translation.json
dotnet i18next convert locales/en/translation.json en.yaml
dotnet i18next convert locales yaml-locales --to yaml       # every file of a directory
dotnet i18next convert flat.json nested.json --nested
```

Converts between i18next JSON, YAML and `.resx`. The format comes from the file extension or `--from` and `--to`.
Dotted `.resx` names become nested keys and nested keys become dotted names in `.resx` files; only string resources are
read. `--flat` writes flat keys like `"menu.title"`, `--nested` nests them at the key separator.

## migrate

```sh
dotnet i18next migrate --check
dotnet i18next migrate
```

Renames i18next JSON v3 plurals to the v4 format of the CLDR plural categories:

| Language | v3 | v4 |
|---|---|---|
| `en` | `item`, `item_plural` | `item_one`, `item_other` |
| `ru` | `item_0`, `item_1`, `item_2` | `item_one`, `item_few`, `item_many` |
| `ja` | `item_0` | `item_other` |

Each v3 suffix gets the category of the first count using it. Forms v3 did not have, like `item_other` for fractional
counts in Russian, are not created; `check` reports them as missing. A key which already exists is not overwritten but
reported, and the command fails.

## translate

```sh
export DEEPL_AUTH_KEY=...
dotnet i18next translate --source-language en --dry-run
dotnet i18next translate --source-language en --languages de,fr

dotnet i18next translate --provider azure --auth-key ... --region westeurope
```

Translates the keys which are missing or empty in the other languages from the source language. Plural keys get the
forms of the target language, translated from the same form or the `_other` form of the source language.
`--dry-run` lists the keys without calling the service and fails when there are any.

| Option | Description |
|---|---|
| `--provider deepl\|azure` | The service, `deepl` by default |
| `--auth-key` | The API key, `DEEPL_AUTH_KEY` or `AZURE_TRANSLATOR_KEY` by default |
| `--region` | The Azure resource region, `AZURE_TRANSLATOR_REGION` by default |
| `--endpoint` | A different base address, e.g. a proxy or a custom Azure endpoint |

Machine translations are a starting point, have them reviewed before shipping.

## Machine translation in code

`I18Next.Net.MachineTranslation` contains the translators the tool uses. `IMachineTranslator` translates a batch of
texts:

```csharp
IMachineTranslator deepL = new DeepLTranslator(httpClient, authKey) { Formality = "less" };   // keys ending with :fx use the free API
IMachineTranslator azure = new AzureTranslator(httpClient, key, "westeurope");

var texts = await deepL.TranslateAsync(["Hello {{name}}", "<b>{{count}}</b> new $t(messages)"], "en", "de");
var text = await azure.TranslateAsync("Hello {{name}}", "en", "de");
```

Interpolations, nestings and HTML tags are kept: they are sent as ignored XML tags to DeepL and as `notranslate` spans
to Azure and put back afterwards. Requests are split into batches and failed requests throw a
`MachineTranslationException`. Regional languages are sent without the region unless the service supports the variant
(`en-GB` and `pt-BR` for DeepL, `zh-Hans` or `fr-CA` for Azure); `LanguageMappings` overrides the codes.

`FuncMachineTranslator` plugs in anything else, e.g. a language model through its SDK. The texts are passed as they are,
so the prompt should ask to keep the placeholders:

```csharp
var llm = new FuncMachineTranslator(async (texts, source, target, cancellationToken) =>
{
    var json = await AskModelAsync(
        $"Translate this JSON array of i18next strings from {source} to {target}. Keep {{{{...}}}}, $t(...) and HTML tags " +
        $"unchanged and answer with a JSON array only: {JsonSerializer.Serialize(texts)}", cancellationToken);

    return JsonSerializer.Deserialize<string[]>(json);
});
```

### Translating missing keys while developing

`MachineTranslationMissingKeyHandler` translates keys which are missing in a language but exist in the source language.
The current call still returns the fallback, the following calls use the translation. Every key is translated once per
handler, concurrent lookups of the same key share one request and failures are logged instead of thrown.

```csharp
var files = new JsonFileBackend("locales");
var translations = new InMemoryBackend();
var backend = new ChainedBackend(translations, files);

var translator = new DefaultTranslator(backend);
translator.MissingKeyHandlers.Add(new MachineTranslationMissingKeyHandler(new DeepLTranslator(authKey), files, "en", translations));

var i18n = new I18NextNet(backend, translator) { Language = "de" };
i18n.SetFallbackLanguages("en");
```

The handler copies the existing namespace from `files` into the `InMemoryBackend` before adding the first translation,
so the in-memory namespace can be put in front of the files. To save the translations instead, pass a callback:

```csharp
new MachineTranslationMissingKeyHandler(machineTranslator, files, "en", async translated =>
    await SaveAsync(translated.Language, translated.Namespace, translated.Key, translated.Text));
```

Use it only while developing: it sends every missing key to the service, and running `dotnet i18next translate` and
reviewing the result is the better way to ship translations.
