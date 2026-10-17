// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>A test's verdict and why (<see cref="ConformanceSuiteRunner"/>).</summary>
internal readonly record struct SuiteResult(string Verdict, string Reason);

/// <summary>
/// ⛔ THE IN-PROCESS RUNNER OF THE CONFORMANCE SUITE (DESIGN-conformance-suite.md §5, §13 S1c; kb/Work PB3176): one
/// test at one edition, judged exactly as <c>conformance-suite/harness/run.py</c> judges it through the WiseOwl
/// adapter, but compiled by <see cref="CompilerDriver"/> in this process (through <see cref="CompiledProgramCache"/>,
/// so a re-gate recompiles nothing it already compiled) instead of by one <c>cobol</c> process per test.
/// </summary>
/// <remarks>
/// <para><b>The verdicts (§5)</b>, spelled as <c>harness/config.py</c> spells them:</para>
/// <list type="bullet">
///   <item><b>PASS</b>: an accepted test compiled, ran, and its standard output, normalized
///     (<see cref="CutRunner.Normalize"/>), equals its expected output for the profile's Annex A.1 values; a flagged
///     test's control compiled and, with the warning mechanism invoked (<c>--flag-extensions</c>, the adapter's
///     <c>flag</c>; ISO §4.2.2), a diagnostic was reported at its marked line.</item>
///   <item><b>FAIL</b>: the output differs; an accepted test did not translate; a flagged test's control compiled
///     but no diagnostic was reported at the marked line. Whether a flagged program also translates is not judged,
///     and neither is a program's exit status (§3.2: it is not standard).</item>
///   <item><b>NOT APPLICABLE</b>: the profile lacks the edition, a module or element the test requires, or the test
///     uses a word the profile reserves beyond the standard (ISO §4.2.10); or the flagged test's control did not
///     compile.</item>
///   <item><b>NO VERDICT</b>: the compiler threw (in-process, an exception is the crash the adapter's crash exit
///     codes name), the program never finished (<see cref="HarnessNonObservationException"/>: a run past the one
///     harness budget is a non-observation, never a value), or the harness could not run the test.</item>
/// </list>
/// <para><b>Each test runs in a fresh working directory</b> holding a byte copy of each declared file, with stdin
/// closed and the default locales pinned (<see cref="CutRunner.RunExit"/>; the WiseOwl adapter pins the same for
/// <c>run.py</c>); the program is compiled from a directory of its own holding its source
/// and declared files, named as its copy library (<c>run.py</c> compiles in the working directory, which the
/// <c>cobol</c> command searches first, DOC-A.1-40), and a flagged test's control compiles in a directory inside the
/// test's. The working directory is removed afterwards.</para>
/// </remarks>
internal static class ConformanceSuiteRunner
{
    internal const string Pass = "PASS";
    internal const string Fail = "FAIL";
    internal const string NotApplicable = "NOT APPLICABLE";
    internal const string NoVerdict = "NO VERDICT";

    /// <summary>The verdict of <paramref name="test"/> at <paramref name="edition"/>.</summary>
    internal static SuiteResult Judge(string edition, SuiteTest test)
    {
        if (WhyNotApplicable(edition, test) is { } why) return new(NotApplicable, why);
        string work = CutRunner.NewTempDir("suite");
        try
        {
            return test.Expect == "accept" ? Accept(edition, test, work) : Flagged(edition, test, work);
        }
        catch (HarnessNonObservationException e)
        {
            return new(NoVerdict, $"the program did not finish, so nothing was observed: {e.Message}");
        }
        catch (Exception e) when (e is SuiteHarnessException or IOException or UnauthorizedAccessException)
        {
            return new(NoVerdict, $"the harness could not run it: {e.Message}");
        }
        finally { CutRunner.TryDelete(work); }
    }

    /// <summary>Compile a refused program (<c>wiseowl/diagnostics.json</c>) at <paramref name="edition"/> as the suite
    /// compiles it (the ordinary compile, its declared files beside it), and return the compiler's result.</summary>
    internal static CompilerDriver.Result CompileRefusal(SuiteRefusal refusal, string edition)
    {
        string work = CutRunner.NewTempDir("refuse");
        try
        {
            string source = Stage(refusal.Id, refusal.Source, refusal.Files, work);
            var (result, crash) = Compile(source, Path.Combine(work, refusal.Id + ".dll"), edition, refusal.SourceFormat,
                flag: false);
            return result ?? throw new SuiteHarnessException($"the compiler crashed on {refusal.Id} at {edition}: {crash}");
        }
        finally { CutRunner.TryDelete(work); }
    }

    private static string? WhyNotApplicable(string edition, SuiteTest test)
    {
        SuiteProfile profile = ConformanceSuite.Profile;
        if (!profile.Editions.Contains(edition))
            return $"the {ConformanceSuite.Compiler} profile does not support {edition}";
        string[] missing = [.. test.Requires.Where(r => !profile.Claims.Contains(r))];
        if (missing.Length > 0)
            return $"it requires {string.Join(", ", missing)}, which the profile does not claim";
        string[] reserved = profile.ExtraReservedWords.TryGetValue(edition, out var extra)
            ? [.. test.Words.Where(extra.Contains).Distinct().Order(StringComparer.Ordinal)]
            : [];
        return reserved.Length > 0
            ? $"it uses {string.Join(", ", reserved)}, which the compiler reserves beyond the standard (ISO 4.2.10)"
            : null;
    }

    private static SuiteResult Accept(string edition, SuiteTest test, string work)
    {
        string source = Stage(test.Id, test.Source, test.Files, work);
        string dll = Path.Combine(work, test.Id + ".dll");
        var (result, crash) = Compile(source, dll, edition, test.SourceFormat, flag: false);
        if (result is null) return new(NoVerdict, $"the compiler crashed: {crash}");
        if (!result.Success) return new(Fail, $"it did not translate ({result.Status}): {FirstLines(result.Errors)}");

        var (_, stdout, _) = CutRunner.RunExit(dll, work);   // the exit status is not judged (§3.2)
        string key = string.Join(".", test.ImplementorDefined.Order().Select(item =>
            ConformanceSuite.Profile.ImplementorDefined.TryGetValue(item, out string? value)
                ? $"{item}={value}"
                : throw new SuiteHarnessException($"the profile states no value of Annex A.1 item {item}")));
        if (!test.ExpectedOutput.TryGetValue(key, out string? expectedPath))
            throw new SuiteHarnessException($"the manifest has no expected output for {(key.Length == 0 ? "no A.1 value" : key)}");
        string expected = CutRunner.Normalize(File.ReadAllText(Path.Combine(ConformanceSuite.Root, expectedPath)));
        return stdout == expected ? new(Pass, "") : new(Fail, Difference(expected, stdout, expectedPath));
    }

    private static SuiteResult Flagged(string edition, SuiteTest test, string work)
    {
        if (test.Control is null || !ConformanceSuite.Manifest.Editions[edition].TryGetValue(test.Control, out SuiteTest? control))
            throw new SuiteHarnessException($"its control {test.Control} is not a test of the {edition} manifest");
        string source = Stage(test.Id, test.Source, test.Files, work);
        string controlWork = Path.Combine(work, "control");
        string controlSource = Stage(control.Id, control.Source, control.Files, controlWork);

        var (compiled, crash) = Compile(controlSource, Path.Combine(controlWork, control.Id + ".dll"), edition,
            control.SourceFormat, flag: false);
        if (compiled is null) return new(NoVerdict, $"the compiler, on its control {control.Id}, crashed: {crash}");
        if (!compiled.Success)
            return new(NotApplicable, $"its control {control.Id} did not compile ({compiled.Status}), so the compiler "
                + "lacks what the test needs (§3.2)");

        var (flagged, flagCrash) = Compile(source, Path.Combine(work, test.Id + ".dll"), edition, test.SourceFormat, flag: true);
        if (flagged is null) return new(NoVerdict, $"the compiler crashed: {flagCrash}");
        int[] lines = DiagnosticLines(flagged, Path.GetFileName(source));
        if (lines.Contains(test.Line)) return new(Pass, "");
        string where = lines.Length > 0 ? $"diagnostics at line(s) {string.Join(", ", lines)}" : "no diagnostic reported";
        return new(Fail, $"no diagnostic at the marked line {test.Line} ({where})");
    }

    /// <summary>Put a test's declared files in <paramref name="work"/> (byte copies, for its run) and its source with
    /// them in a compile directory of its own, through <see cref="CompiledProgramCache.StageSource"/> so the same test
    /// compiles from the same path every run; returns the source path to compile. The compile directory is the
    /// program's copy library.</summary>
    private static string Stage(string id, string source, IReadOnlyList<string> files, string work)
    {
        Directory.CreateDirectory(work);
        string sourceName = id + ".cob";
        var companions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string declared in files)
        {
            string name = Path.GetFileName(declared);
            if (name == sourceName)
                throw new SuiteHarnessException($"its declared file {declared} has the name the harness gives its source");
            string from = Path.Combine(ConformanceSuite.Root, declared);
            File.Copy(from, Path.Combine(work, name));
            companions[name] = File.ReadAllText(from);
        }
        string compileDir = Path.Combine(work, "compile");
        Directory.CreateDirectory(compileDir);
        return CompiledProgramCache.StageSource(Path.Combine(compileDir, sourceName),
            File.ReadAllText(Path.Combine(ConformanceSuite.Root, source)), companions);
    }

    /// <summary>Compile <paramref name="source"/> as the adapter's <c>compile</c> (or, with <paramref name="flag"/>,
    /// its <c>flag</c>: the §4.2.10 warning mechanism invoked) does: the edition, the test's reference format, the
    /// source's own directory as the copy library. A thrown exception is the compiler's crash, returned as text with
    /// its stack, so the NO VERDICT it becomes names where the compiler failed.</summary>
    private static (CompilerDriver.Result? Result, string? Crash) Compile(string source, string dll, string edition,
        string sourceFormat, bool flag)
    {
        if (!InitialReferenceFormatOption.TryParse(sourceFormat, out InitialReferenceFormat format))
            throw new SuiteHarnessException($"source-format '{sourceFormat}' is not one of fixed, free");
        var options = new CompilerDriver.Options(source, dll, DialectLevel: int.Parse(edition, CultureInfo.InvariantCulture),
            CopyPaths: [Path.GetDirectoryName(source)!], SourceFormat: format, FlagExtensions: flag);
        try { return (CompiledProgramCache.Compile(options), null); }
        catch (Exception e) when (e is not (HarnessNonObservationException or OutOfMemoryException))
        {
            return (null, e.ToString());
        }
    }

    /// <summary>The lines of the source named <paramref name="sourceName"/> at which the compile reported a
    /// diagnostic, read from what the <c>cobol</c> command prints (its compile-time output, its warnings and its
    /// errors) with the adapter's pattern; a diagnostic of another file (a copybook's) is not the test's.</summary>
    private static int[] DiagnosticLines(CompilerDriver.Result result, string sourceName)
    {
        var lines = new SortedSet<int>();
        IEnumerable<string> printed = result.CompileOutput.Select(c => c.Text).Concat(result.Warnings).Concat(result.Errors)
            .SelectMany(text => text.ReplaceLineEndings("\n").Split('\n'));
        foreach (string line in printed)
        {
            var m = ConformanceSuite.Diagnostic.Match(line);
            if (!m.Success) continue;
            var file = m.Groups["file"];
            if (file.Success && !string.Equals(Path.GetFileName(file.Value.Trim()), sourceName, StringComparison.OrdinalIgnoreCase))
                continue;
            lines.Add(int.Parse(m.Groups["line"].Value, CultureInfo.InvariantCulture));
        }
        return [.. lines];
    }

    private static string FirstLines(IReadOnlyList<string> text)
    {
        string joined = string.Join(" | ", text.SelectMany(t => t.Split('\n')).Select(l => l.Trim()).Where(l => l.Length > 0).Take(3));
        return joined.Length == 0 ? "no output" : joined[..Math.Min(joined.Length, 400)];
    }

    private static string Difference(string expected, string actual, string expectedPath)
    {
        string[] want = expected.Split('\n'), got = actual.Split('\n');
        for (int i = 0; i < Math.Max(want.Length, got.Length); i++)
        {
            string w = i < want.Length ? want[i] : "<end of output>";
            string g = i < got.Length ? got[i] : "<end of output>";
            if (w != g)
                return $"the output differs from {expectedPath} at line {i + 1}: expected '{Clip(w)}', got '{Clip(g)}'";
        }
        return $"the output differs from {expectedPath}";

        static string Clip(string s) => s.Length <= 160 ? s : s[..160];
    }
}

/// <summary>The harness could not run a test (a manifest or file problem): its verdict is NO VERDICT.</summary>
internal sealed class SuiteHarnessException(string message) : Exception(message);
