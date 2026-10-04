using BenchmarkDotNet.Running;

using I18Next.Net.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(TranslationBenchmarks).Assembly).Run(args);
