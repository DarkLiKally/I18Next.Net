```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Processor 2.10GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=.NET 10.0  Runtime=.NET 10.0  Toolchain=net10.0  
IterationCount=3  LaunchCount=1  WarmupCount=3  

```
| Method                 | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Simple                 |   128.7 ns |   422.2 ns |  23.14 ns |  1.02 |    0.22 | 0.0007 |     112 B |        1.00 |
| NestedKey              |   160.5 ns |   269.3 ns |  14.76 ns |  1.27 |    0.22 | 0.0007 |     112 B |        1.00 |
| Interpolation          |   346.3 ns |   378.9 ns |  20.77 ns |  2.75 |    0.44 | 0.0038 |     520 B |        4.64 |
| MultipleInterpolations |   613.9 ns | 3,704.3 ns | 203.05 ns |  4.87 |    1.59 | 0.0038 |     608 B |        5.43 |
| Plural                 |   465.4 ns |   360.0 ns |  19.73 ns |  3.69 |    0.57 | 0.0052 |     744 B |        6.64 |
| ContextAndPlural       |   512.6 ns |   299.0 ns |  16.39 ns |  4.07 |    0.62 | 0.0057 |     872 B |        7.79 |
| Nesting                |   730.6 ns |   296.2 ns |  16.24 ns |  5.80 |    0.88 | 0.0086 |    1240 B |       11.07 |
| NumberFormat           |   662.1 ns |   345.7 ns |  18.95 ns |  5.25 |    0.80 | 0.0038 |     648 B |        5.79 |
| FallbackLanguage       |   293.8 ns |   461.5 ns |  25.29 ns |  2.33 |    0.39 | 0.0024 |     328 B |        2.93 |
| Namespace              |   174.5 ns |   195.3 ns |  10.70 ns |  1.39 |    0.22 | 0.0012 |     184 B |        1.64 |
| ReturnObjects          | 1,240.8 ns |   373.1 ns |  20.45 ns |  9.85 |    1.48 | 0.0153 |    2256 B |       20.14 |
| MissingKey             |   271.4 ns |   379.4 ns |  20.80 ns |  2.15 |    0.35 | 0.0024 |     352 B |        3.14 |
