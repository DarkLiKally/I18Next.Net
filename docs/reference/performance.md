# Performance

The benchmarks translate the same resources with the same twelve scenarios in I18Next.Net and in i18next on Node.js. The
resources are in `tests/I18Next.Net.Benchmarks/locales`, the .NET benchmarks use BenchmarkDotNet and the Node.js
benchmarks a batched timing loop with warm-up that reports the median time per call.

| Scenario | i18next 26.4.2 on Node 22 | I18Next.Net on .NET 10 |
|---|---:|---:|
| Simple key | 2,485 ns | 129 ns |
| Nested key | 2,596 ns | 161 ns |
| Interpolation | 3,206 ns | 346 ns |
| Multiple interpolations | 4,201 ns | 614 ns |
| Plural | 4,943 ns | 465 ns |
| Context and plural | 5,298 ns | 513 ns |
| Nesting | 6,662 ns | 731 ns |
| Number format | 5,476 ns | 662 ns |
| Fallback language | 3,720 ns | 294 ns |
| Namespace | 2,926 ns | 175 ns |
| Return objects | 18,219 ns | 1,241 ns |
| Missing key | 4,176 ns | 271 ns |

Translations are resolved synchronously when the namespace is already loaded. The arguments of anonymous objects are
read with compiled getters, interpolation runs in a single pass and parsed formats are cached, so a simple translation
allocates 112 bytes.

## Running the benchmarks

```
dotnet run -c Release --project tests/I18Next.Net.Benchmarks -- --filter '*'

cd tests/I18Next.Net.Benchmarks/node
npm install
npm run bench
```
