using BenchmarkDotNet.Running;

namespace Auth.Benchmarks;

internal static class Program
{
    public static int Main(string[] args) {
        string resultsDir;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])) {
            resultsDir = args[0];
        } else {
            // Default to results/benchmarks in the project root (assuming we are in tests/Auth.Benchmarks/bin/...)
            // Actually, better to just use a relative path from current directory or provide it via build script.
            resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "results", "benchmarks");
        }
        
        resultsDir = Path.GetFullPath(resultsDir);

        if (!Directory.Exists(resultsDir)) {
            Directory.CreateDirectory(resultsDir);
        }

#pragma warning disable CA1303 // Do not pass literals as localized parameters

        Console.WriteLine("Running benchmarks...");

        BenchmarkRunner.Run<PasswordHashBench>();
        BenchmarkRunner.Run<JwtCreationBench>();
        BenchmarkRunner.Run<JwtValidationBench>();
        BenchmarkRunner.Run<ClaimsExtractionBench>();
        BenchmarkRunner.Run<TokenVersionCheckBench>();

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var reportPath = Path.Combine(resultsDir, $"benchmark-report-{timestamp}.md");

        Console.WriteLine("Benchmarks complete.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        Console.WriteLine($"Results will be saved to: {reportPath}");
        return 0;
    }
}
