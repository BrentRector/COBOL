// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using BenchmarkDotNet.Attributes;
using CobolNet.Benchmarks.Collation;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;

namespace CobolNet.Benchmarks.GeneratedCode;

/// <summary>
/// GENERATED-PROGRAM HOT PATHS (kb/Work PB2117, the instrument kb/Work A6 asked for): what the code this compiler
/// emits costs to RUN, on the four shapes A6 named: MOVE-heavy loops across every storage form, PERFORM dispatch,
/// sequential file I/O and indexed file I/O. Each program is a COBOL source under <c>Programs/</c>, compiled once in
/// <see cref="Setup"/> at the shipping default edition and then run in this process through its entry point, so the
/// number is the generated code and the runtime it calls, with no process start-up in it.
/// <para>
/// The same sources are what <c>scripts/arch/perf_baseline.py</c> times as whole processes against GnuCOBOL 3.2 (the
/// external comparison A6's storage-model question needs): one source, two consumers, so the two numbers always
/// measure the same COBOL.
/// </para>
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class GeneratedProgramBenchmarks
{
    [Params("MOVEHOT", "PERFHOT", "SEQHOT", "IDXHOT")]
    public string Program { get; set; } = "";

    private MethodInfo _entryPoint = null!;
    private string _workDirectory = "";
    private string _expected = "";

    /// <summary>Compile the program, load it, and run it once as the WITNESS (dotnet-engineering): its whole
    /// standard output must be the line <c>Programs/witnesses.tsv</c> states for it, computed from the program's own
    /// header comment. A program that stopped early, skipped its loop or lost a record prints something else, and
    /// the case fails here instead of being timed.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _workDirectory = Directory.CreateTempSubdirectory("pb2117-run-").FullName;
        string source = ProgramsDirectory(Program + ".cob");
        string dll = Path.Combine(_workDirectory, Program + ".dll");
        var result = CompilerDriver.Compile(new CompilerDriver.Options(source, dll));
        if (!result.Success)
            throw new InvalidOperationException(
                $"{Program}: compile failed ({result.Status}): {string.Join("; ", result.Errors)}");

        _entryPoint = Assembly.LoadFrom(dll).EntryPoint
                      ?? throw new InvalidOperationException($"{Program}: the generated assembly has no entry point");
        _expected = ExpectedWitness(Program);
        // A file the program ASSIGNs by a relative name resolves against the current directory, so every run's
        // files land in this case's own scratch directory (BenchmarkDotNet runs each case in its own process).
        Directory.SetCurrentDirectory(_workDirectory);
        // The runtime switches the console to UTF-8 on its first activation, and doing so replaces Console.Out; do
        // it now, so the redirection in Run is never undone underneath it.
        StandardStreams.EnsureUtf8();

        var witness = new StringWriter();
        RunWith(witness);
        string actual = witness.ToString().TrimEnd('\r', '\n');
        if (actual != _expected)
            throw new InvalidOperationException($"{Program}: witness mismatch: expected [{_expected}], got [{actual}]");
        Console.WriteLine($"// witness {Program}: [{actual}]");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try { Directory.Delete(_workDirectory, recursive: true); }
        catch (IOException) { /* the loaded assembly can still be mapped; the temp root is the OS's to reclaim */ }
        catch (UnauthorizedAccessException) { /* same */ }
    }

    /// <summary>One whole run of the program: its single DISPLAY goes to a null writer, and the method returns the
    /// witness length so the call cannot be discarded.</summary>
    [Benchmark]
    public int Run()
    {
        var sink = new CountingWriter();
        RunWith(sink);
        return sink.Count;
    }

    private void RunWith(TextWriter output)
    {
        var saved = Console.Out;
        Console.SetOut(output);
        try { _entryPoint.Invoke(null, _entryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]); }
        finally { Console.SetOut(saved); }
    }

    /// <summary>The directory holding the hot-path sources and their witnesses.</summary>
    internal static string ProgramsDirectory(params string[] segments) =>
        TestRepo.Tests(["Cobol.Net.Benchmarks", "Programs", .. segments]);

    /// <summary>The program's expected output line from <c>Programs/witnesses.tsv</c> (program TAB line).</summary>
    internal static string ExpectedWitness(string program) =>
        File.ReadLines(ProgramsDirectory("witnesses.tsv"))
            .Where(l => !l.StartsWith('#'))
            .Select(l => l.Split('\t', 2))
            .SingleOrDefault(f => f.Length == 2 && f[0] == program)?[1]
        ?? throw new InvalidOperationException($"Programs/witnesses.tsv has no line for {program}");

    /// <summary>A writer that keeps only a character count: the run's output cost without the console.</summary>
    private sealed class CountingWriter : TextWriter
    {
        public int Count { get; private set; }
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        public override void Write(char value) => Count++;
        public override void Write(string? value) => Count += value?.Length ?? 0;
        public override void Write(char[] buffer, int index, int count) => Count += count;
    }
}
