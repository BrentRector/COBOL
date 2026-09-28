// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.CommandLine;
using System.Diagnostics;
using CobolNet;
using CobolNet.Runtime;

namespace CobolNet.Cli;

/// <summary>
/// The <c>cobol</c> command-line driver: a thin shell over <see cref="CompilerDriver"/> — it parses arguments
/// (via <c>System.CommandLine</c>, Microsoft's first-party parser), prints diagnostics, optionally runs the
/// compiled program, and maps the compile outcome to a process exit code. All compilation logic lives in the
/// Cobol.Net.Compiler library (so it is testable without an exe).
/// </summary>
/// <remarks>
/// Usage: <c>cobol &lt;source.cob&gt; [-o out.dll] [--nist [NAME]] [--std 85|2002|2014|2023] [--permissive]
/// [--copy DIR] [--run] [--sign-encoding ibm|ascii]</c>. Every value option accepts BOTH <c>--opt value</c> and <c>--opt=value</c>; no
/// option can swallow a following flag. The generated C# is written next to the output assembly
/// (<c>&lt;name&gt;.g.cs</c>) so the translation is directly inspectable.
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        // ⛔ BEFORE ANYTHING IS WRITTEN. Diagnostics carry ISO citations — `(ISO §11.10.3 SR5–6)` — and on a
        // default Windows console (cp437) the section sign left this process as the single byte 0x15 and the
        // en dash as '-', in every redirected capture and every CI log, because this entry point never set an
        // encoding while CobolNet.Runtime's did (kb/Work PB899 — the two-arm dispatch with one arm fixed). One
        // rule, one place: CobolNet.Runtime.IO.StandardStreams, which the run-unit entry calls too.
        CobolNet.Runtime.IO.StandardStreams.EnsureUtf8();
        return BuildParser().Command.Parse(args).Invoke();
    }

    /// <summary>Build the argument parser (exposed for unit testing the grammar without spawning a process):
    /// the configured <see cref="RootCommand"/> plus a pure resolver that maps a <see cref="ParseResult"/> to the
    /// <see cref="CliOptions"/> DTO (the <c>--nist</c>/<c>--std</c> defaulting is here, testable in isolation).</summary>
    internal static (RootCommand Command, Func<ParseResult, CliOptions> Resolve) BuildParser()
    {
        var sourceArgument = new Argument<string>("source")
        {
            Description = "The COBOL source file to compile.",
        };
        var outputOption = new Option<string?>("-o", "--output")
        {
            Description = "Output assembly path (default: the source path with a .dll extension).",
            HelpName = "out.dll",
        };
        var nistOption = new Option<string?>("--nist")
        {
            Description = "Enable NIST CCVS placeholder preprocessing; the optional test name defaults to the "
                + "source file's base name.",
            Arity = ArgumentArity.ZeroOrOne,   // the name is optional — `--nist` alone is legal
            HelpName = "NAME",
        };
        var stdOption = new Option<int?>("--std")
        {
            Description = "Target ISO edition: 85, 2002, 2014, or 2023 (default: 2023, or 85 with --nist).",
            HelpName = "YEAR",
        };
        stdOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int?>() is { } y and not (85 or 2002 or 2014 or 2023))
                result.AddError($"--std must be one of 85, 2002, 2014, 2023 (got {y}).");
        });
        var permissiveOption = new Option<bool>("--permissive")
        {
            Description = "Accept constructs the targeted edition removed, warning instead of rejecting (the "
                + "migration mode; orthogonal to --std/--nist).",
        };
        var copyOption = new Option<string[]>("--copy")
        {
            Description = "A COPY library-text search directory (repeatable), searched in order after the current "
                + "working directory; the source file's own directory is searched only when named here.",
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = false,   // `--copy A --copy B`, never grabbing the source positional
            HelpName = "DIR",
        };
        var runOption = new Option<bool>("--run")
        {
            Description = "Run the compiled assembly after a successful compile.",
        };
        // kb/Work PB803 (owner decision 2026-09-09): the DISPLAY over-punch convention, Annex A.1 items 177/178.
        // A REPRESENTATION option, orthogonal to --std/--permissive — every edition grants the same latitude
        // (ISO §13.18.52.4 GR4 / GR5 b). An unknown value is refused by the parser exactly as --std's is: a CLI
        // argument error, never a COBOLNET source diagnostic (nothing about the SOURCE is wrong).
        var signEncodingOption = new Option<string?>("--sign-encoding")
        {
            Description = "DISPLAY operational-sign convention for a signed item whose sign is not SEPARATE: "
                + "ibm (default — {A-I / }J-R, IBM / Micro Focus compatible) or ascii (plain digits / p-y).",
            HelpName = string.Join("|", ZonedSign.OptionSpellings),
        };
        signEncodingOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<string?>() is { } v && !ZonedSign.TryParseOption(v, out _))
                result.AddError($"--sign-encoding must be one of {string.Join(", ", ZonedSign.OptionSpellings)} (got {v}).");
        });

        var root = new RootCommand("cobol — translate a COBOL source unit to typed-native .NET (the Roslyn backend).")
        {
            sourceArgument, outputOption, nistOption, stdOption, permissiveOption, copyOption, runOption,
            signEncodingOption,
        };

        CliOptions Resolve(ParseResult parse)
        {
            string source = parse.GetValue(sourceArgument)!;
            // The NIST test name is OPTIONAL: `--nist` present means NIST mode (GetResult is non-null even with
            // no value); an absent name is derived from the source file's base name (the CCVS convention).
            bool nistEnabled = parse.GetResult(nistOption) is not null;
            string? nistName = nistEnabled
                ? parse.GetValue(nistOption) ?? Path.GetFileNameWithoutExtension(source)
                : null;
            // --std wins when given; else the NIST CCVS corpus is COBOL-85, otherwise the 2023 default (DEVLOG 519).
            int std = parse.GetValue(stdOption) ?? (nistEnabled ? 85 : 2023);

            // The validator above has already refused any unparseable value, so a false here can only be the
            // ABSENT option — the documented IBM default (kb/Work PB803).
            _ = ZonedSign.TryParseOption(parse.GetValue(signEncodingOption), out var signEncoding);

            return new CliOptions(
                source, parse.GetValue(outputOption), nistName, std,
                parse.GetValue(copyOption) ?? [], parse.GetValue(runOption), parse.GetValue(permissiveOption),
                signEncoding);
        }

        // A no-emit BATCH check subcommand (the INV-1 continuity sweep fast path): parse + edition-validate +
        // bind-check many (source, edition) entries in ONE warm process — skipping the Roslyn backend AND the
        // ~2s-per-file `dotnet` cold-start that made the sweep ~1400 cold full compiles.
        var manifestArgument = new Argument<string>("manifest")
        {
            Description = "A TSV file; each line `source<TAB>std<TAB>nist-name<TAB>permissive(0|1)` is "
                + "parse+bind-checked (no Roslyn emit). Prints `source<TAB>std<TAB>PASS|FAIL` per line.",
        };
        var checkBatch = new Command("check-batch",
            "Parse + edition-validate + bind-check many (source, edition) entries in one process (no Roslyn emit).")
        {
            manifestArgument,
        };
        checkBatch.SetAction(parse => RunCheckBatch(parse.GetValue(manifestArgument)!));
        root.Subcommands.Add(checkBatch);

        // The root's OWN action (a bare `cobol <source>` compiles) — set here so the root is invokable WITHOUT a
        // subcommand (otherwise System.CommandLine treats a command that has subcommands but no action as a pure
        // group and errors "Required command was not provided").
        root.SetAction(parse => RunCompile(Resolve(parse)));

        return (root, Resolve);
    }

    /// <summary>Parse + edition-validate + bind-check every entry of a TSV manifest in one warm process, in
    /// parallel (CompilerDriver.Compile is proven concurrency-safe — ConcurrentCompilationTests). Each entry
    /// uses the no-emit CheckOnly path (no Roslyn backend, no file writes), so what were ~1400 cold-`dotnet`
    /// full compiles collapse to a single parallel in-process pass. Prints one `source&lt;TAB&gt;std&lt;TAB&gt;
    /// PASS|FAIL` line per entry and always exits 0 — the per-entry verdicts are the output the caller aggregates
    /// (e.g. scripts/version-continuity-sweep.sh classifies SKIP85 / OK / BREAKS).</summary>
    private static int RunCheckBatch(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"error: manifest not found: {manifestPath}");
            return 66;
        }
        string[] entries = File.ReadAllLines(manifestPath).Where(l => l.Trim().Length > 0).ToArray();
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();
        Parallel.ForEach(entries, line =>
        {
            string[] f = line.Split('\t');
            string source = f[0];
            int std = f.Length > 1 && int.TryParse(f[1], out int s) ? s : 2023;
            string? nist = f.Length > 2 && f[2].Length > 0 ? f[2] : null;
            bool permissive = f.Length > 3 && f[3] == "1";
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                source, NistTestName: nist, DialectLevel: std, Permissive: permissive, CheckOnly: true));
            results.Add($"{source}\t{std}\t{(r.Success ? "PASS" : "FAIL")}");
        });
        foreach (string line in results)
            Console.Out.WriteLine(line);
        return 0;
    }

    /// <summary>Compile per the resolved options, print diagnostics, and map the outcome to a POSIX exit code.</summary>
    private static int RunCompile(CliOptions options)
    {
        var result = CompilerDriver.Compile(new CompilerDriver.Options(
            options.SourcePath, options.OutputPath, options.NistTestName, options.DialectLevel, options.CopyPaths,
            options.Permissive, SignEncoding: options.SignEncoding));

        // Edition warnings (obsolete/archaic 0903 flags; removed constructs under --permissive) print to stderr
        // ALWAYS — success or failure — so migration users see them without a failing build (P2.1).
        foreach (string w in result.Warnings)
            Console.Error.WriteLine($"  {w}");

        if (!result.Success)
        {
            if (result.Status == CompilerDriver.Outcome.BackendError)
                Console.Error.WriteLine($"error: backend compilation failed (generated C# at {result.GeneratedCsPath}):");
            foreach (string e in result.Errors)
                Console.Error.WriteLine($"  {e}");

            // POSIX sysexits: 66 = no input, 65 = data/format error (source), 70 = internal software error (backend).
            return result.Status switch
            {
                CompilerDriver.Outcome.SourceNotFound => 66,
                CompilerDriver.Outcome.FrontendError => 65,
                CompilerDriver.Outcome.BackendError => 70,
                _ => 70,
            };
        }

        return options.Run ? Run(result.OutputDll) : 0;
    }

    /// <summary>Launch the compiled assembly via the shared <c>dotnet</c> host and forward its exit code.</summary>
    private static int Run(string dllPath)
    {
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"") { UseShellExecute = false };
        using var proc = Process.Start(psi);
        proc!.WaitForExit();
        return proc.ExitCode;
    }
}
