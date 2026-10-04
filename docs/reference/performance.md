# Performance

Both libraries translate the same resources (`tests/I18Next.Net.Benchmarks/locales`) with the same scenarios. The .NET
numbers are the mean of BenchmarkDotNet 0.15.8, the Node.js numbers the median of a batched timing loop after warm-up
(`tests/I18Next.Net.Benchmarks/node`). Measured on an Intel Xeon 2.10 GHz with 4 cores, Ubuntu 24.04.

| Scenario | i18next 26.4.2 on Node 22 | I18Next.Net on .NET 8 | I18Next.Net on .NET 10 | Allocated on .NET 10 | Faster than i18next |
|---|---:|---:|---:|---:|---:|
| Simple key `t("simple")` | 3,244 ns | 221 ns | 141 ns | 112 B | 23× |
| Nested key `t("deep.nested.key")` | 2,862 ns | 218 ns | 141 ns | 112 B | 20× |
| Interpolation | 3,447 ns | 447 ns | 328 ns | 352 B | 11× |
| Three interpolations | 4,133 ns | 642 ns | 447 ns | 424 B | 9× |
| Plural | 4,888 ns | 580 ns | 420 ns | 536 B | 12× |
| Context and plural | 5,100 ns | 698 ns | 494 ns | 656 B | 10× |
| Nesting `$t(...)` | 6,553 ns | 1,035 ns | 750 ns | 1240 B | 9× |
| `number` format | 5,505 ns | 819 ns | 600 ns | 464 B | 9× |
| Fallback language | 4,167 ns | 462 ns | 306 ns | 328 B | 14× |
| Other namespace | 2,949 ns | 249 ns | 160 ns | 184 B | 18× |
| `returnObjects` / `TObject` | 17,541 ns | 1,551 ns | 1,136 ns | 2120 B | 15× |
| Missing key | 4,664 ns | 370 ns | 273 ns | 352 B | 17× |

Loaded namespaces are resolved synchronously, the arguments of anonymous objects are read with compiled getters,
interpolation runs in a single pass and parsed formats are cached.

```
dotnet run -c Release --project tests/I18Next.Net.Benchmarks -- --filter '*'

cd tests/I18Next.Net.Benchmarks/node
npm install
npm run bench
```
