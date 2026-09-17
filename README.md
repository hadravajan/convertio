# Convertio

![CI](https://github.com/hadravajan/convertio/actions/workflows/ci.yml/badge.svg)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![License: MIT](https://img.shields.io/badge/license-MIT-blue)

Batch image format converter and size optimizer, built as a production-shaped .NET 8
console app. Converts JPEG/PNG/WebP/BMP/GIF/TIFF sources to JPEG, PNG, or WebP with a
configurable quality/size trade-off, in parallel, over single files or whole directory
trees.

```
$ convertio ./photos -f webp -q 75 -o ./optimized -r
Converting 4 file(s)...

[1/4] OK   sunset.png    2.4MB -> 410KB (-82.9%)  [61ms]
[2/4] OK   team.jpg      1.1MB -> 298KB (-72.9%)  [34ms]
[3/4] OK   logo.bmp      3.8MB -> 89KB  (-97.7%)  [52ms]
[4/4] OK   banner.tiff   5.2MB -> 640KB (-87.7%)  [78ms]

---------------------------------------------
Summary
---------------------------------------------
Succeeded : 4
Failed    : 0
Size      : 12.5MB -> 1.4MB (88.8% reduction)
Elapsed   : 0.09s
```

## Features

- **JPEG, PNG, WebP output** — decode from JPEG, PNG, WebP, BMP, GIF, or TIFF.
- **Format-aware quality**: JPEG/WebP use standard lossy quality (1–100); PNG, being
  lossless by spec, maps quality onto palette size via Wu color quantization instead,
  so it still gets a real size/fidelity trade-off rather than a fake no-op slider.
- **Batch and recursive** — point it at files, a directory, or several of each; `-r`
  descends into subdirectories.
- **Bounded parallel processing** — concurrent conversions capped at CPU core count by
  default (`-p` to override), so a 500-file batch doesn't thrash the machine.
- **Per-file error isolation** — one corrupt or unsupported file fails and is reported;
  the rest of the batch keeps going.
- **Atomic writes** — each output is written to a temp file and renamed into place, so
  Ctrl+C or a crash mid-encode never leaves a truncated file behind.
- **Safe by default** — skips existing outputs unless `-y/--overwrite` is passed; never
  overwrites the source file even when converting a format to itself (`photo.jpg -f jpg`
  produces `photo.converted.jpg`, not a clobbered original).
- **Scriptable exit codes** — `0` success, `1` partial failure, `2` bad arguments, `130`
  cancelled — so it composes cleanly in shell scripts and CI.

## Why this shape

- **[SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)** for decode/encode
  — a pure-managed, cross-platform library with JPEG/PNG/WebP codecs in one package.
  Deliberately not `System.Drawing`, which is Windows-only and unsupported for this use
  on modern .NET.
- **No CLI framework dependency.** `System.CommandLine` is still pre-1.0 with breaking
  changes across previews; a hand-rolled ~150-line parser is simpler, has zero surprise
  upgrade risk, and is trivially unit tested (see `Convertio.Tests/ArgumentParserTests.cs`).
- **`SemaphoreSlim`-bounded `Task.WhenAll`**, not unbounded parallelism — batch
  conversion is CPU-bound; letting hundreds of tasks race the thread pool at once helps
  no one.

## Project layout

```
Convertio/
├── Cli/
│   ├── ArgumentParser.cs       # hand-rolled arg parsing, no external CLI package
│   ├── CliOptions.cs           # parsed options + supported input extensions
│   └── InputFileResolver.cs    # expands file/dir args into a deduped file list
├── Conversion/
│   ├── ImageConverter.cs       # ImageSharp encode/decode, per-format quality mapping
│   ├── BatchConverter.cs       # bounded-parallel orchestration
│   ├── OutputPathBuilder.cs    # output path + same-format collision handling
│   ├── ConversionResult.cs
│   └── OutputImageFormat.cs
├── Logging/
│   └── ConsoleReporter.cs      # progress lines + run summary
└── Program.cs                  # wiring, cancellation, exit codes

Convertio.Tests/                # xUnit tests for all pure logic above
```

## Build & run

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/hadravajan/convertio.git
cd convertio
dotnet restore
dotnet build -c Release
dotnet run -c Release --project Convertio -- <args>
```

Run the tests:

```bash
dotnet test
```

Publish a self-contained single binary:

```bash
dotnet publish Convertio -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
# win-x64 / osx-x64 / osx-arm64 also supported
```

## Usage

```
convertio <input...> -f <jpg|png|webp> [options]

  -f, --format <fmt>      Output format: jpg, png, or webp. Required.
  -q, --quality <1-100>   Output quality. Default: 80.
  -o, --output <dir>      Directory to write converted files into. Default: alongside source.
  -r, --recursive         Descend into subdirectories when an input is a directory.
  -y, --overwrite         Overwrite existing output files instead of skipping them.
  -p, --parallel <n>      Max concurrent conversions. Default: processor count.
  -h, --help              Show help.
```

```bash
convertio photo.png -f webp -q 75
convertio ./photos -f jpg -q 85 -o ./out -r
convertio a.png b.bmp c.tiff -f webp -q 90 -y
```

## Testing approach

`Convertio.Tests` covers every pure-logic component with xUnit: format parsing,
argument parsing (valid input and every rejected-input path), output path computation
(including the same-format collision case), and file discovery (recursive vs.
non-recursive, extension filtering, deduplication, missing-path errors).

The actual encode/decode path (`ImageConverter`) isn't unit tested against real image
bytes here — that would need real sample images and is better covered by an integration
test with fixture files if this grows further. What *is* verified end-to-end is the full
CLI pipeline (argument parsing → file discovery → parallel batch conversion → skip/
overwrite logic → same-format collision handling → summary/exit codes) against a stand-in
encoder, which caught and fixed real bugs during development (including an xUnit
`InlineData` array-typing issue) before this was pushed.

## License

MIT — see [LICENSE](LICENSE).
