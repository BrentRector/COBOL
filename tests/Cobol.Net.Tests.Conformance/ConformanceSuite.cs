// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>One test of the conformance suite at one edition, as the generator's manifest states it
/// (<c>conformance-suite/harness/generate.py#entry_for</c>; DESIGN-conformance-suite.md §4, §5).</summary>
/// <param name="Id">The test's id: its file's base name.</param>
/// <param name="Clause">The clause folder it is filed under.</param>
/// <param name="Source">Its program, relative to the suite root.</param>
/// <param name="SourceFormat"><c>fixed</c> or <c>free</c>.</param>
/// <param name="Files">The files it declares (copybooks, data files), relative to the suite root.</param>
/// <param name="Requires">The Annex A.4 modules and A.3 elements it needs.</param>
/// <param name="Words">Its words that are none of the standard's at this edition (what a profile's extra reserved
/// words are checked against, ISO §4.2.10).</param>
/// <param name="Expect"><c>accept</c> or <c>flag</c>.</param>
/// <param name="ImplementorDefined">An accepted test's Annex A.1 items whose value its output depends on.</param>
/// <param name="ExpectedOutput">An accepted test's expected output per A.1 value key (<c>""</c> when it has none).</param>
/// <param name="Line">A flagged test's marked line.</param>
/// <param name="Control">A flagged test's control: the accepted twin with the violation removed.</param>
/// <param name="InSuite">Whether the per-edition suites carry it (the rest are WiseOwl's own tests).</param>
internal sealed record SuiteTest(
    string Id, string Clause, string Source, string SourceFormat, IReadOnlyList<string> Files,
    IReadOnlyList<string> Requires, IReadOnlyList<string> Words, string Expect, IReadOnlyList<int> ImplementorDefined,
    IReadOnlyDictionary<string, string> ExpectedOutput, int Line, string? Control, bool InSuite);

/// <summary>One of WiseOwl COBOL's refusals (<c>conformance-suite/wiseowl/diagnostics.json</c>, carried into the
/// wiseowl manifest): the editions at which WiseOwl refuses the program, and the code or message substring it
/// reports.</summary>
internal sealed record SuiteRefusal(
    string Id, string Source, string SourceFormat, IReadOnlyList<string> Files, IReadOnlyList<string> RefuseAt,
    string Expect);

/// <summary>What WiseOwl COBOL's profile (<c>conformance-suite/profiles/wiseowl.json</c>) claims: the editions, the
/// Annex A.4 modules and A.3 elements, its value of each Annex A.1 item the standard enumerates, and the words it
/// reserves beyond the standard's, per edition.</summary>
internal sealed record SuiteProfile(
    IReadOnlySet<string> Editions, IReadOnlySet<string> Claims, IReadOnlyDictionary<int, string> ImplementorDefined,
    IReadOnlyDictionary<string, IReadOnlySet<string>> ExtraReservedWords);

/// <summary>
/// ⛔ THE CONFORMANCE SUITE AS WISEOWL COBOL'S OWN PIPELINE READS IT (DESIGN-conformance-suite.md §5 "In WiseOwl's own
/// pipeline", §13 S1c; kb/Work PB3176): the generator's <c>wiseowl</c> manifest, the WiseOwl adapter's diagnostic
/// pattern and the WiseOwl profile. <see cref="ConformanceSuiteRunner"/> executes them in-process.
/// </summary>
/// <remarks>
/// <para><b>It never reads a test header.</b> <c>harness/generate.py</c> is the ONE reader of the headers (§10: two
/// parsers of the headers would be two implementations of the suite's semantics), so the manifest is generated once
/// per test process, into a directory of this process's own (<c>generate.py --out</c>), and read from there; the
/// repository's <c>suites/</c> is never written under a concurrent <c>run.py</c>. A refused tree makes both theories
/// of <see cref="ConformanceSuiteTests"/> red at discovery, with the generator's refusals; a missing interpreter is a
/// loud failure, never a skip (<see cref="PythonInstrument"/>).</para>
/// <para><b>The adapter and the profile are read, never restated.</b> <c>harness/config.py</c> is their one loader
/// and validator, and the generator's audit (<c>generate.py --check</c>, a gate audit) validates every one of them, so
/// this class reads only the fields the in-process runner needs, from files already proved well formed: how a
/// WiseOwl diagnostic names its line (the adapter's <c>diagnostic</c>, a Python pattern whose one Python-only form,
/// <c>(?P&lt;name&gt;</c>, is .NET's <c>(?&lt;name&gt;</c>) and what the profile claims.
/// <c>ConformanceSuiteRunnerDriftTests</c> holds the two runners' verdicts equal, which is what keeps this reading
/// and <c>run.py</c>'s the same.</para>
/// </remarks>
internal static class ConformanceSuite
{
    /// <summary>The compiler this pipeline runs the suite on: the name of its adapter (<c>adapters/wiseowl.json</c>).</summary>
    internal const string Compiler = "wiseowl";

    /// <summary><c>conformance-suite/</c>: the suite root every manifest path is relative to.</summary>
    internal static string Root { get; } = TestRepo.At("conformance-suite");

    /// <summary>The generator, the one reader of the test headers.</summary>
    internal static string Generator { get; } = TestRepo.At("conformance-suite", "harness", "generate.py");

    /// <summary>The compiler-neutral runner, <c>harness/run.py</c>.</summary>
    internal static string Harness { get; } = TestRepo.At("conformance-suite", "harness", "run.py");

    private static readonly Lazy<WiseOwlManifest> s_manifest =
        new(LoadManifest, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<(SuiteProfile Profile, Regex Diagnostic)> s_compiler =
        new(LoadCompiler, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The wiseowl manifest: every test of both trees at each edition it names, and the refusals.</summary>
    internal static WiseOwlManifest Manifest => s_manifest.Value;

    /// <summary>The WiseOwl profile.</summary>
    internal static SuiteProfile Profile => s_compiler.Value.Profile;

    /// <summary>A WiseOwl diagnostic as its adapter reads one: a <c>line</c> group, and a <c>file</c> group.</summary>
    internal static Regex Diagnostic => s_compiler.Value.Diagnostic;

    /// <summary>Run the generator, writing every manifest under <paramref name="outDir"/>; throws, with the
    /// generator's own output, when it refuses the tree.</summary>
    internal static void Generate(string outDir)
    {
        ProcessObservation r = PythonInstrument.Run(Generator, "--out", outDir);
        if (r.ExitCode != 0)
            throw new InvalidOperationException(
                $"conformance-suite/harness/generate.py refused the suite (exit {r.ExitCode}), so no manifest exists "
                + $"to run:\n{r.Stdout}\n{r.Stderr}");
    }

    /// <summary>The theory rows of the suite's tests: <c>(edition, id)</c> for every test at every edition the
    /// wiseowl manifest names it. No sentinel row: a manifest that holds no test is a red, never a pass over
    /// nothing.</summary>
    internal static IEnumerable<object[]> Tests() =>
        Manifest.Editions.SelectMany(e => e.Value.Keys.Order(StringComparer.Ordinal).Select(id => new object[] { e.Key, id }));

    /// <summary>The theory rows of the refusals: <c>(id, edition)</c> for every edition each refusal names.</summary>
    internal static IEnumerable<object[]> Refusals() =>
        Manifest.Refusals.Values.OrderBy(r => r.Id, StringComparer.Ordinal)
            .SelectMany(r => r.RefuseAt.Select(e => new object[] { r.Id, e }));

    private static WiseOwlManifest LoadManifest()
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Suite_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Generate(dir);
            return WiseOwlManifest.Parse(File.ReadAllText(Path.Combine(dir, Compiler, "manifest.json")));
        }
        finally { CutRunner.TryDelete(dir); }
    }

    private static (SuiteProfile, Regex) LoadCompiler()
    {
        using var adapter = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "adapters", Compiler + ".json")));
        string profileName = adapter.RootElement.GetProperty("profile").GetString()!;
        string pattern = adapter.RootElement.GetProperty("diagnostic").GetString()!;
        if (pattern.Contains("(?P=", StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"adapters/{Compiler}.json's diagnostic uses a Python back-reference (?P=…), which this reader does not translate");
        var diagnostic = new Regex(pattern.Replace("(?P<", "(?<", StringComparison.Ordinal), RegexOptions.CultureInvariant);

        using var profile = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "profiles", profileName + ".json")));
        JsonElement p = profile.RootElement;
        var claims = new HashSet<string>(StringComparer.Ordinal);
        foreach (string key in new[] { "modules", "elements" })
            if (p.TryGetProperty(key, out JsonElement claimed))
                claims.UnionWith(claimed.EnumerateObject().Select(c => c.Name));
        var a1 = p.TryGetProperty("implementor-defined", out JsonElement values)
            ? values.EnumerateObject().ToDictionary(v => int.Parse(v.Name), v => v.Value.GetString()!)
            : [];
        var extra = p.TryGetProperty("extra-reserved-words", out JsonElement words)
            ? words.EnumerateObject().ToDictionary(
                e => e.Name, e => (IReadOnlySet<string>)e.Value.EnumerateArray().Select(w => w.GetString()!).ToHashSet(StringComparer.Ordinal))
            : [];
        var editions = p.GetProperty("editions").EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.Ordinal);
        return (new SuiteProfile(editions, claims, a1, extra), diagnostic);
    }
}

/// <summary>The generator's <c>suites/wiseowl/manifest.json</c>: edition → id → test, and id → refusal.</summary>
internal sealed record WiseOwlManifest(
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, SuiteTest>> Editions,
    IReadOnlyDictionary<string, SuiteRefusal> Refusals)
{
    /// <summary>Read the manifest's JSON. A field the generator always writes is read with
    /// <see cref="JsonElement.GetProperty(string)"/>, so a manifest of another shape throws rather than run a test
    /// with a default in its place.</summary>
    internal static WiseOwlManifest Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var editions = new Dictionary<string, IReadOnlyDictionary<string, SuiteTest>>(StringComparer.Ordinal);
        foreach (JsonProperty edition in doc.RootElement.GetProperty("editions").EnumerateObject())
        {
            var tests = new Dictionary<string, SuiteTest>(StringComparer.Ordinal);
            foreach (JsonElement t in edition.Value.EnumerateArray())
            {
                var test = Test(t);
                if (!tests.TryAdd(test.Id, test))
                    throw new InvalidOperationException($"the wiseowl manifest names {test.Id} twice at {edition.Name}");
            }
            editions[edition.Name] = tests;
        }

        var refusals = new Dictionary<string, SuiteRefusal>(StringComparer.Ordinal);
        foreach (JsonProperty r in doc.RootElement.GetProperty("diagnostics").EnumerateObject())
        {
            refusals[r.Name] = new SuiteRefusal(r.Name, r.Value.GetProperty("source").GetString()!,
                r.Value.GetProperty("source-format").GetString()!, Strings(r.Value.GetProperty("files")),
                Strings(r.Value.GetProperty("refuse-at")), r.Value.GetProperty("expect").GetString()!);
        }
        return new WiseOwlManifest(editions, refusals);
    }

    private static SuiteTest Test(JsonElement t)
    {
        string expect = t.GetProperty("expect").GetString()!;
        bool accepted = expect switch
        {
            "accept" => true,
            "flag" => false,
            _ => throw new InvalidOperationException($"the wiseowl manifest's expect '{expect}' is neither accept nor flag"),
        };
        return new SuiteTest(
            t.GetProperty("id").GetString()!, t.GetProperty("clause").GetString()!, t.GetProperty("source").GetString()!,
            t.GetProperty("source-format").GetString()!, Strings(t.GetProperty("files")), Strings(t.GetProperty("requires")),
            Strings(t.GetProperty("words")), expect,
            accepted ? [.. t.GetProperty("implementor-defined").EnumerateArray().Select(i => i.GetInt32())] : [],
            accepted
                ? t.GetProperty("expected-output").EnumerateObject().ToDictionary(o => o.Name, o => o.Value.GetString()!, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal),
            accepted ? 0 : t.GetProperty("line").GetInt32(),
            accepted ? null : t.GetProperty("control").GetString()!,
            t.GetProperty("in-suite").GetBoolean());
    }

    private static IReadOnlyList<string> Strings(JsonElement array) => [.. array.EnumerateArray().Select(e => e.GetString()!)];
}
