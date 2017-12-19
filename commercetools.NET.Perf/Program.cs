using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using commercetools.NET.Perf.Benchmarks;

namespace commercetools.NET.Perf
{
    public class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<DynamicModelCreationBenchmark>(
                ManualConfig
                    .Create(DefaultConfig.Instance)
                    .With(Job.LegacyJitX86)
                    .With(Job.LegacyJitX64)
                    .With(Job.RyuJitX86)
                    .With(Job.RyuJitX64)
                    .With(ExecutionValidator.FailOnError));
        }
    }
}
