using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using I18Next.Net.Benchmarks;

var assembly = typeof(I18NextBenchmark).Assembly;
var debuggable = assembly.GetCustomAttribute<DebuggableAttribute>();
var isOptimized = debuggable == null || debuggable.IsJITOptimizerDisabled == false;

var config = !isOptimized ? new DebugBuildConfig() : DefaultConfig.Instance;

BenchmarkRunner.Run(assembly, config);
