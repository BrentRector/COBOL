// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler;
using CobolNet.Frontend.Preprocessor;

namespace CobolSharp.CLI;

/// <summary>
/// Entry point for the CobolSharp CLI. Dispatches to compile (default) or preprocess subcommands.
/// </summary>
public class Program
{
    /// <summary>BSD sysexits.h EX_SOFTWARE: an internal software error, returned for an unexpected
    /// (uncaught) compiler exception. Distinct from 0 (success) and 1 (a normal, diagnosed failure).</summary>
    private const int ExitInternalError = 70;

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help")
            {
                PrintUsage();
                return 0;
            }

            // Explicit subcommand: preprocess
            if (args[0] == "preprocess")
            {
                if (args.Length < 2)
                {
                    Console.Error.WriteLine("Error: no source file specified.");
                    Console.Error.WriteLine("Usage: cobolsharp preprocess <file.cob> [-o <output>]");
                    return 1;
                }

                return RunPreprocess(args[1..]);
            }

            // Default: compile. Accept "compile" as optional explicit subcommand.
            var compileArgs = args[0] == "compile" ? args[1..] : args;
            return RunCompile(compileArgs);
        }
        catch (Exception ex)
        {
            // An UNEXPECTED exception is an internal compiler bug: every expected, already-diagnosed
            // failure returns 0/1 cleanly via the dispatch above and never reaches here. Surface a
            // diagnosable internal-compiler-error to stderr (never a raw CLR crash dump) and return a
            // dedicated non-crash exit code, so a code-generation defect is reportable rather than an
            // opaque process abort (rc 134/139-class). (DEVLOG 308)
            string? src = TryFindSourceArg(args);
            Console.Error.WriteLine(
                $"<cobolsharp>: error COBOL0600: Internal compiler error: {ex.GetType().Name}: " +
                $"{ex.Message}. Please report this, including the stack trace below.");
            if (src != null)
                Console.Error.WriteLine($"  Source: {src}");
            Console.Error.WriteLine(ex.ToString());
            return ExitInternalError;
        }
    }

    /// <summary>
    /// Best-effort recovery of the source path from <paramref name="args"/> for the internal-error
    /// message: the first non-option, non-subcommand token, skipping the value of a value-taking option.
    /// </summary>
    private static string? TryFindSourceArg(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a is "compile" or "preprocess") continue;
            if (a is "-o" or "--standard" or "--copy-path" or "-I") { i++; continue; } // skip option value
            if (!a.StartsWith('-')) return a;
        }
        return null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("CobolSharp — COBOL compiler for .NET");
        Console.WriteLine();
        Console.WriteLine("Usage: cobolsharp [options] <file.cob>");
        Console.WriteLine("       cobolsharp preprocess <file.cob> [-o <output>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o <output>              Output file path (default: <program-id>.dll)");
        Console.WriteLine("  --standard <version>     COBOL standard version (default: cobol85)");
        Console.WriteLine("                           Values: default (permissive), cobol85, cobol2002,");
        Console.WriteLine("                           cobol2014, cobol2023. Named versions reject");
        Console.WriteLine("                           non-standard constructs; 'default' accepts them.");
        Console.WriteLine("  --nist [name]            Enable NIST test suite preprocessing");
        Console.WriteLine("                           Replaces XXXXX### placeholders; derives test");
        Console.WriteLine("                           name from source filename if not specified.");
        Console.WriteLine("                           Implies --standard default unless one is given.");
        Console.WriteLine("  --typed-fields           Experimental: enable .NET-native typed-field codegen");
        Console.WriteLine("                           (data-model migration). Output stays byte-identical.");
        Console.WriteLine("  -h, --help               Show this help message");
    }

    private static int RunPreprocess(string[] args)
    {
        string? sourcePath = null;
        string? outputPath = null;
        var copyPaths = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length)
                outputPath = args[++i];
            else if ((args[i] == "--copy-path" || args[i] == "-I") && i + 1 < args.Length)
                copyPaths.Add(args[++i]);
            else if (!args[i].StartsWith('-'))
                sourcePath = args[i];
        }

        if (sourcePath is null || !File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Error: source file not found: {sourcePath}");
            return 1;
        }

        string rawText = File.ReadAllText(sourcePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";

        // Auto-discover a sibling copylib/ directory (NIST layout), mirroring compile mode.
        string siblingCopyLib = Path.GetFullPath(Path.Combine(sourceDir, "..", "copylib"));
        if (Directory.Exists(siblingCopyLib))
            copyPaths.Add(siblingCopyLib);

        // Phase 0a: Reference format normalization (the line-entry stage drops NIST archive markers: CCVS dialect on)
        string normalized = ReferenceFormatProcessor.NormalizeToFreeFormMapped(rawText,
            gates: null, sourcePath, initialFixed: true, out _, ccvsIndicators: true).Text;

        // Phase 0b: COPY/REPLACE expansion
        var copyProcessor = new CopyProcessor(copyPaths);
        string processed = copyProcessor.Process(normalized, sourceDir);

        if (outputPath != null)
        {
            File.WriteAllText(outputPath, processed);
            Console.WriteLine($"Preprocessed to: {outputPath}");
        }
        else
        {
            Console.Write(processed);
        }

        return 0;
    }

    private static int RunCompile(string[] args)
    {
        string? sourcePath = null;
        string? outputPath = null;
        string standard = "cobol85";
        bool standardSpecified = false;
        string? nistTestName = null;
        bool enableTypedFields = false;
        var copyPaths = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
            else if (args[i] == "--standard" && i + 1 < args.Length)
            {
                standard = args[++i].ToLowerInvariant();
                standardSpecified = true;
                if (standard is not ("default" or "cobol85" or "cobol2002" or "cobol2014" or "cobol2023"))
                {
                    Console.Error.WriteLine($"Error: unknown standard '{standard}'. Use: default, cobol85, cobol2002, cobol2014, cobol2023");
                    return 1;
                }
            }
            else if ((args[i] == "--copy-path" || args[i] == "-I") && i + 1 < args.Length)
            {
                copyPaths.Add(args[++i]);
            }
            else if (args[i] == "--nist")
            {
                // Enable NIST preprocessing; test name derived from source filename
                nistTestName = ""; // will be derived from source path later
            }
            else if (args[i] == "--typed-fields")
            {
                // Experimental: opt into the .NET-native data-model migration's typed-field codegen
                // (docs/DATA_MODEL_ARCHITECTURE.md). Default OFF; observable output stays byte-identical.
                enableTypedFields = true;
            }
            else if (!args[i].StartsWith('-'))
            {
                sourcePath = args[i];
            }
            else
            {
                Console.Error.WriteLine($"Unknown option: {args[i]}");
                return 1;
            }
        }

        // The NIST CCVS suite is written in the permissive 1980s/90s dialect (it contains a small
        // number of non-standard constructs the era's compilers tolerated — see
        // docs/dialect-strictness.md). Unless an explicit --standard is given, --nist runs in the
        // permissive Default dialect so those documented leniencies are accepted; named-strict modes
        // (e.g. --standard cobol2023) still reject them.
        if (nistTestName != null && !standardSpecified)
            standard = "default";

        if (sourcePath is null)
        {
            Console.Error.WriteLine("Error: no source file specified.");
            return 1;
        }

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Error: source file not found: {sourcePath}");
            return 1;
        }

        var compilation = new Compilation();
        compilation.Options = new CobolSharp.Compiler.Semantics.CompilationOptions
        {
            Dialect = standard switch
            {
                "cobol85" => CobolSharp.Compiler.Semantics.DialectMode.StrictCobol85,
                "cobol2002" => CobolSharp.Compiler.Semantics.DialectMode.Cobol2002,
                "cobol2014" => CobolSharp.Compiler.Semantics.DialectMode.Cobol2014,
                "cobol2023" => CobolSharp.Compiler.Semantics.DialectMode.Cobol2023,
                _ => CobolSharp.Compiler.Semantics.DialectMode.Default
            }
        };
        compilation.Options.EnableTypedFields = enableTypedFields;

        // NIST mode: derive test name from source filename if not explicit
        if (nistTestName != null)
        {
            if (nistTestName == "")
                nistTestName = Path.GetFileNameWithoutExtension(sourcePath);
            compilation.NistTestName = nistTestName;

            // Auto-discover the NIST copy library (sibling of the programs/ directory),
            // so COPY statements resolve without the harness needing an explicit path.
            string srcDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";
            string nistCopyLib = Path.GetFullPath(Path.Combine(srcDir, "..", "copylib"));
            if (Directory.Exists(nistCopyLib))
                copyPaths.Add(nistCopyLib);
        }

        foreach (var p in copyPaths)
            compilation.AddCopySearchPath(p);

        var result = compilation.Compile(sourcePath, outputPath);

        foreach (var diagnostic in result.Diagnostics)
        {
            Console.Error.WriteLine(diagnostic);
        }

        if (result.Success)
        {
            Console.WriteLine($"Compiled successfully: {result.OutputPath}");
            return 0;
        }
        else
        {
            Console.Error.WriteLine("Compilation failed.");
            return 1;
        }
    }
}
