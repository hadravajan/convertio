using Convertio.Cli;
using Convertio.Logging;

namespace Convertio.Conversion;

public sealed class BatchConverter(IImageConverter converter, ConsoleReporter reporter)
{
    /// <summary>
    /// Runs conversions for all resolved input files with bounded concurrency.
    /// Returns (results, skipped) so the caller can compute an exit code and print a summary.
    /// </summary>
    public async Task<(IReadOnlyList<ConversionResult> Results, IReadOnlyList<(string Path, string Reason)> Skipped)> RunAsync(
        IReadOnlyList<string> files,
        CliOptions options,
        CancellationToken cancellationToken)
    {
        reporter.BeginBatch(files.Count);

        using var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism);
        var results = new System.Collections.Concurrent.ConcurrentBag<ConversionResult>();
        var skipped = new System.Collections.Concurrent.ConcurrentBag<(string, string)>();

        var tasks = files.Select(async sourceFile =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var outputPath = OutputPathBuilder.Build(sourceFile, options.Format, options.OutputDirectory);

                if (File.Exists(outputPath) && !options.Overwrite)
                {
                    skipped.Add((sourceFile, $"output already exists at {outputPath}"));
                    reporter.ReportSkipped(sourceFile, $"already exists at {outputPath}");
                    return;
                }

                var result = await converter
                    .ConvertAsync(sourceFile, outputPath, options.Format, options.Quality, cancellationToken)
                    .ConfigureAwait(false);

                results.Add(result);
                reporter.ReportResult(result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return (results.ToList(), skipped.ToList());
    }
}
