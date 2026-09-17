using Convertio.Conversion;

namespace Convertio.Logging;

/// <summary>
/// Console output for a batch run. A lock around writes keeps interleaved output from
/// parallel conversions readable — Console itself is thread-safe per-call, but a
/// multi-part colored line isn't atomic without one.
/// </summary>
public sealed class ConsoleReporter
{
    private readonly object _writeLock = new();
    private int _completed;
    private int _total;

    public void BeginBatch(int total)
    {
        _total = total;
        _completed = 0;
        Console.WriteLine($"Converting {total} file(s)...");
        Console.WriteLine();
    }

    public void ReportResult(ConversionResult result)
    {
        var completed = Interlocked.Increment(ref _completed);

        lock (_writeLock)
        {
            Console.Write($"[{completed}/{_total}] ");

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("OK   ");
                Console.ResetColor();
                var name = Path.GetFileName(result.SourcePath);
                Console.WriteLine(
                    $"{name}  {FormatBytes(result.OriginalSizeBytes)} -> {FormatBytes(result.OutputSizeBytes)} " +
                    $"({result.ReductionPercent:+0.0;-0.0;0.0}%)  [{result.Elapsed.TotalMilliseconds:0}ms]");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("FAIL ");
                Console.ResetColor();
                var name = Path.GetFileName(result.SourcePath);
                Console.WriteLine($"{name}  {result.ErrorMessage}");
            }
        }
    }

    public void ReportSkipped(string sourcePath, string reason)
    {
        var completed = Interlocked.Increment(ref _completed);
        lock (_writeLock)
        {
            Console.Write($"[{completed}/{_total}] ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("SKIP ");
            Console.ResetColor();
            Console.WriteLine($"{Path.GetFileName(sourcePath)}  {reason}");
        }
    }

    public void PrintSummary(IReadOnlyList<ConversionResult> results, IReadOnlyList<(string Path, string Reason)> skipped, TimeSpan elapsed)
    {
        var succeeded = results.Where(r => r.Success).ToList();
        var failed = results.Where(r => !r.Success).ToList();

        var originalTotal = succeeded.Sum(r => r.OriginalSizeBytes);
        var outputTotal = succeeded.Sum(r => r.OutputSizeBytes);
        var reduction = originalTotal <= 0 ? 0 : (1.0 - (double)outputTotal / originalTotal) * 100.0;

        Console.WriteLine();
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine("Summary");
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine($"Succeeded : {succeeded.Count}");
        Console.WriteLine($"Failed    : {failed.Count}");
        if (skipped.Count > 0)
        {
            Console.WriteLine($"Skipped   : {skipped.Count} (already exist, use -y/--overwrite)");
        }
        if (succeeded.Count > 0)
        {
            Console.WriteLine($"Size      : {FormatBytes(originalTotal)} -> {FormatBytes(outputTotal)} ({reduction:0.0}% reduction)");
        }
        Console.WriteLine($"Elapsed   : {elapsed.TotalSeconds:0.00}s");

        if (failed.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failures:");
            Console.ResetColor();
            foreach (var f in failed)
            {
                Console.WriteLine($"  {f.SourcePath}: {f.ErrorMessage}");
            }
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.##}{units[unitIndex]}";
    }
}
