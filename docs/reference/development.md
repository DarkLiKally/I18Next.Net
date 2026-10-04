# Development

```
dotnet build I18Next.Net.slnx
dotnet test tests/I18Next.Net.Tests
dotnet run -c Release --project tests/I18Next.Net.Benchmarks
```

The tests use xUnit, Shouldly and NSubstitute. The code style follows the default .NET rules in `.editorconfig`
(`dotnet format`), and the CI fails on NuGet packages with known vulnerabilities.

The CLDR data for relative times, lists and the plural rule tests is generated from the official CLDR JSON packages:

```
python3 tools/cldr/generate.py
```
