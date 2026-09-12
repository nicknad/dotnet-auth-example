using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Auth.Benchmarks;

internal static class Program
{
    public static int Main(string[] args) {
        string resultsDir;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) && !args[0].StartsWith('-')) {
            resultsDir = args[0];
            args = args.Skip(1).ToArray();
        } else {
            // Default to results/benchmarks in the current working directory.
            resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "results", "benchmarks");
        }

        resultsDir = Path.GetFullPath(resultsDir);
        Directory.CreateDirectory(resultsDir);

#pragma warning disable CA1303 // Do not pass literals as localized parameters

        Console.WriteLine("Running benchmarks...");

        var config = DefaultConfig.Instance
            .WithArtifactsPath(resultsDir);

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

        Console.WriteLine($"Benchmark artifacts saved to: {resultsDir}");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        return 0;
    }
}
