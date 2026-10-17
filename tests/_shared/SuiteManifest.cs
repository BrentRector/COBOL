// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;

namespace CobolNet.Tests.Shared;

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
/// <param name="Options">A WiseOwl test's WiseOwl compile options (<c>conformance-suite/wiseowl/options.json</c>), by
/// option: <c>sign-encoding</c>, <c>flag-extensions</c>; empty for every other test.</param>
internal sealed record SuiteTest(
    string Id, string Clause, string Source, string SourceFormat, IReadOnlyList<string> Files,
    IReadOnlyList<string> Requires, IReadOnlyList<string> Words, string Expect, IReadOnlyList<int> ImplementorDefined,
    IReadOnlyDictionary<string, string> ExpectedOutput, int Line, string? Control, bool InSuite,
    IReadOnlyDictionary<string, string> Options);

/// <summary>One of WiseOwl COBOL's refusals (<c>conformance-suite/wiseowl/diagnostics.json</c>, carried into the
/// wiseowl manifest): the editions at which WiseOwl refuses the program, and the code or message substring it
/// reports.</summary>
internal sealed record SuiteRefusal(
    string Id, string Source, string SourceFormat, IReadOnlyList<string> Files, IReadOnlyList<string> RefuseAt,
    string Expect, IReadOnlyDictionary<string, string> Options);

/// <summary>
/// ⛔ THE CONFORMANCE SUITE'S <c>wiseowl</c> MANIFEST, AS EVERY TEST PROJECT READS IT (DESIGN-conformance-suite.md §4,
/// §5; kb/Work PB3176, PB3177): every test of both trees at each edition it names, and WiseOwl's refusals. It is generated
/// once per test process by <c>conformance-suite/harness/generate.py --out</c> into a directory of the process's own
/// and read from there: the generator is the ONE reader of the test headers (§10), so no test reads a header (which
/// edition a test is accepted at, which refusals a program carries) any other way. A refused tree throws, with the
/// generator's refusals; a missing interpreter is a loud failure, never a skip (<see cref="PythonInstrument"/>).
/// </summary>
internal static class SuiteManifest
{
    /// <summary><c>conformance-suite/</c>: the suite root every manifest path is relative to.</summary>
    internal static string Root { get; } = TestRepo.Suite();

    /// <summary>The generator, the one reader of the test headers.</summary>
    internal static string Generator { get; } = TestRepo.Suite("harness", "generate.py");

    private static readonly Lazy<WiseOwlManifest> s_manifest =
        new(LoadManifest, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The wiseowl manifest: every test of both trees at each edition it names, and the refusals.</summary>
    internal static WiseOwlManifest Manifest => s_manifest.Value;

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
            return WiseOwlManifest.Parse(File.ReadAllText(Path.Combine(dir, "wiseowl", "manifest.json")));
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
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
                Strings(r.Value.GetProperty("refuse-at")), r.Value.GetProperty("expect").GetString()!,
                Map(r.Value.GetProperty("options")));
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
            t.GetProperty("in-suite").GetBoolean(), Map(t.GetProperty("options")));
    }

    private static IReadOnlyDictionary<string, string> Map(JsonElement map) =>
        map.EnumerateObject().ToDictionary(o => o.Name, o => o.Value.GetString()!, StringComparer.Ordinal);

    private static IReadOnlyList<string> Strings(JsonElement array) => [.. array.EnumerateArray().Select(e => e.GetString()!)];
}
