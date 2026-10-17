// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>What WiseOwl COBOL's profile (<c>conformance-suite/profiles/wiseowl.json</c>) claims: the editions, the
/// Annex A.4 modules and A.3 elements, its value of each Annex A.1 item the standard enumerates, and the words it
/// reserves beyond the standard's, per edition.</summary>
internal sealed record SuiteProfile(
    IReadOnlySet<string> Editions, IReadOnlySet<string> Claims, IReadOnlyDictionary<int, string> ImplementorDefined,
    IReadOnlyDictionary<string, IReadOnlySet<string>> ExtraReservedWords);

/// <summary>
/// ⛔ THE CONFORMANCE SUITE AS WISEOWL COBOL'S OWN PIPELINE READS IT (DESIGN-conformance-suite.md §5 "In WiseOwl's own
/// pipeline", §13 S1c; kb/Work PB3176): the generator's <c>wiseowl</c> manifest, the WiseOwl adapter's diagnostic
/// pattern and the WiseOwl profile (the manifest itself is <see cref="SuiteManifest"/>, which the Unit tests read
/// too). <see cref="ConformanceSuiteRunner"/> executes them in-process.
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

    /// <summary>The compiler-neutral runner, <c>harness/run.py</c>.</summary>
    internal static string Harness { get; } = TestRepo.At("conformance-suite", "harness", "run.py");

    private static readonly Lazy<(SuiteProfile Profile, Regex Diagnostic)> s_compiler =
        new(LoadCompiler, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The WiseOwl profile.</summary>
    internal static SuiteProfile Profile => s_compiler.Value.Profile;

    /// <summary>A WiseOwl diagnostic as its adapter reads one: a <c>line</c> group, and a <c>file</c> group.</summary>
    internal static Regex Diagnostic => s_compiler.Value.Diagnostic;

    private static (SuiteProfile, Regex) LoadCompiler()
    {
        using var adapter = JsonDocument.Parse(File.ReadAllText(Path.Combine(SuiteManifest.Root, "adapters", Compiler + ".json")));
        string profileName = adapter.RootElement.GetProperty("profile").GetString()!;
        string pattern = adapter.RootElement.GetProperty("diagnostic").GetString()!;
        if (pattern.Contains("(?P=", StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"adapters/{Compiler}.json's diagnostic uses a Python back-reference (?P=…), which this reader does not translate");
        var diagnostic = new Regex(pattern.Replace("(?P<", "(?<", StringComparison.Ordinal), RegexOptions.CultureInvariant);

        using var profile = JsonDocument.Parse(File.ReadAllText(Path.Combine(SuiteManifest.Root, "profiles", profileName + ".json")));
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

