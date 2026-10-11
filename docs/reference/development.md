# Development

```
dotnet build I18Next.Net.slnx
dotnet test --project tests/I18Next.Net.Tests
dotnet test --project tests/I18Next.Net.Tests -f net8.0 -p:LibraryTargetFramework=net6.0
dotnet run -c Release --project tests/I18Next.Net.Benchmarks
```

Every package has a `tests/*.Tests` project, the WPF tests need Windows. The tests use xUnit v3 on the Microsoft Testing
Platform (selected in `global.json`, requires the .NET 10 SDK), Shouldly and NSubstitute. The code style follows the
default .NET rules in `.editorconfig` (`dotnet format`), and the CI fails on NuGet packages with known vulnerabilities.

The second test command runs the tests against the .NET 6 build of the libraries.

The CLDR data for relative times, lists and the plural rule tests is generated from the official CLDR JSON packages:

```
python3 tools/cldr/generate.py
```
