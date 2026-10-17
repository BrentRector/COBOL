// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Keeps the COMPILER-SIDE half of every mode-or-clause-derived intrinsic argument MEASURED (kb/Work PB256).
/// </summary>
/// <remarks>
/// <para>
/// Several runtime intrinsic bodies take a behaviour decision as a PARAMETER — <c>commaMode</c> (§15.67.3 r5 /
/// §15.68.3 r4 d / §15.69.3 r4, the DECIMAL-POINT IS COMMA separator roles), <c>digitCap</c> (§15.93.4 /
/// §15.94.4 / §15.95.4 r1 b)'s mode-keyed sub-notes) and <c>anycase</c> (§15.68.3 r4 f). The arm that COMPUTES
/// each of them lives in the compiler (<c>IntrinsicRenderer.CommaFlag</c> / <c>DigitCapFlag</c> /
/// <c>AnycaseFlag</c>, reading <c>ctx.Data.DecimalPointIsComma</c>, <c>ArithmeticModes.NumvalDigitCap</c> and the
/// bound call's ANYCASE phrase), and a unit test that passes the value as a LITERAL exercises the runtime scan
/// and NOTHING left of it. That was the measured state for the whole TEST- family: three of the fourteen
/// (function × construct) pairs below had no end-to-end witness of any kind, and one unit test was named
/// <c>DigitCap_FollowsArithmeticMode</c> while no arithmetic mode was in scope in it at all.
/// </para>
/// <para>
/// So the pairs are DERIVED rather than listed: the functions come from the renderer's own arms (which arm
/// interpolates which flag) crossed with the catalog's COBOL spelling, and the corpus is searched for a witness.
/// Adding a flag to a new render arm — or introducing a new function that takes one — fails this test until a
/// golden compiles that function with the source construct that turns the flag ON. That is the property the
/// note's mechanism needed and did not have: nothing anywhere went red while the arms were untested.
/// </para>
/// <para>
/// ⚠ WHAT THIS GATE CLAIMS, EXACTLY. It is a COVERAGE gate, not a semantics gate: a witness is an ENABLED
/// positive golden WITH a sibling <c>.out</c> (so it runs and is compared, not merely compiled) in which the
/// construct and the function reference appear in ONE top-level source-unit scope — a container and its
/// contained programs counting as one scope, because §11.9.4 / §8.3.2 make the configuration and OPTIONS
/// inheritable downward and <c>2023/pb60_nested_configuration_inheritance</c> is exactly that shape. An OPTIONS
/// override inside a containee is not modelled here; the witness golden's own <c>.out</c> is what proves the
/// semantics. Comment text is stripped before the search, so a golden that merely MENTIONS a clause in its
/// header cannot certify it — <see cref="CommentedConstruct_IsNotAWitness"/> fires that failure branch once, so
/// this file's silence is evidence rather than habit.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class IntrinsicModeFlagCoverageDriftTests
{
    /// <summary>The compile-time flags a rendered call can carry, and the SOURCE construct that turns each ON.
    /// The key is the flag's spelling in <c>IntrinsicRenderer</c>'s interpolated call text.</summary>
    private static readonly (string Flag, string Construct, string Rule, Regex Match)[] Flags =
    [
        ("CommaFlag", "DECIMAL-POINT IS COMMA", "§15.67.3 r5 / §15.68.3 r4 d / §15.69.3 r4",
            new Regex(@"DECIMAL-POINT\s+IS\s+COMMA", RegexOptions.IgnoreCase)),
        ("DigitCapFlag", "ARITHMETIC IS STANDARD[-DECIMAL]", "§15.93.4 / §15.94.4 / §15.95.4 r1 b) 2-4",
            new Regex(@"ARITHMETIC\s+IS\s+STANDARD", RegexOptions.IgnoreCase)),
        ("AnycaseFlag", "ANYCASE", "§15.68.3 r4 f",
            new Regex(@"\bANYCASE\b", RegexOptions.IgnoreCase)),
    ];

    /// <summary>RuntimeMethod → COBOL function name, read out of the ONE catalog table's rows
    /// (<c>Add(new("NUMVAL-C", …, "ss", "NumvalC", IntrinsicBind.…))</c>). ⛔ The detailed scan must reach EVERY
    /// row the file declares, not merely many of them: a row shape this regex cannot see is a function this gate
    /// is blind to, which is the same under-count that let the arms go untested in the first place. The
    /// <see cref="IntrinsicBind"/>-fold rows carry an EMPTY RuntimeMethod (they emit no runtime call at all) and
    /// are counted but not mapped.</summary>
    private static Dictionary<string, string> CobolNameByRuntimeMethod()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));
        var declared = Regex.Matches(src, @"Add\(new\(""(?<cobol>[A-Z0-9\-]+)""")
            .Select(m => m.Groups["cobol"].Value).ToHashSet(StringComparer.Ordinal);
        Assert.True(declared.Count > 80, $"only {declared.Count} IntrinsicCatalog rows found — this guard is blind");

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(src,
            @"Add\(new\(""(?<cobol>[A-Z0-9\-]+)""[^;]*?""(?<spec>[a-zA-Z]*)""\s*,\s*""(?<rt>[A-Za-z0-9]*)""\s*,\s*IntrinsicBind"))
        {
            seen.Add(m.Groups["cobol"].Value);
            if (m.Groups["rt"].Value.Length > 0) map[m.Groups["rt"].Value] = m.Groups["cobol"].Value;
        }
        Assert.Equal(declared.OrderBy(x => x, StringComparer.Ordinal),
                     seen.OrderBy(x => x, StringComparer.Ordinal));
        return map;
    }

    /// <summary>Every (COBOL function, flag) pair the renderer can emit, attributed by walking the renderer's
    /// source and remembering the LAST arm label seen before each flag interpolation. Arm labels come in three
    /// spellings — <c>case "X":</c>, <c>"X" =&gt;</c> and <c>"X" when … =&gt;</c> — and a token only counts as a
    /// label when its name is a catalogued RuntimeMethod, so an unrelated string literal cannot claim a flag.</summary>
    private static SortedSet<(string Fn, string Flag)> RenderedFlagPairs()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        var byMethod = CobolNameByRuntimeMethod();
        string flagAlt = string.Join("|", Flags.Select(f => f.Flag));
        var rx = new Regex(
            @"(?<label>(?:case\s+)?""(?<n>[A-Za-z0-9]+)""(?:\s+or\s+""[A-Za-z0-9]+"")*\s*(?::|=>|when\b))"
            + @"|(?<flag>\{(?<f>" + flagAlt + @")(?:\(ic\))?\})");
        var pairs = new SortedSet<(string, string)>();
        string? current = null;
        foreach (Match m in rx.Matches(src))
        {
            if (m.Groups["label"].Success)
            {
                if (byMethod.TryGetValue(m.Groups["n"].Value, out string? cobol)) current = cobol;
                continue;
            }
            Assert.NotNull(current);      // a flag before any arm label would mean the walk lost its anchor
            pairs.Add((current!, m.Groups["f"].Value));
        }
        Assert.True(pairs.Count >= 10,
            $"only {pairs.Count} (function, flag) pairs found in IntrinsicRenderer — the arm walk is blind");
        return pairs;
    }

    /// <summary>The comment-free text of one source line — a fixed-form indicator comment is dropped whole, and
    /// an inline <c>*&gt;</c> is cut at its first occurrence.</summary>
    private static string StripComment(string line)
    {
        int at = line.IndexOf("*>", StringComparison.Ordinal);
        string code = at >= 0 ? line[..at] : line;
        return code.TrimStart().StartsWith('*') ? string.Empty : code;
    }

    /// <summary>One top-level source-unit scope per entry: a containing program and every program contained in
    /// it stay together (the configuration/OPTIONS inheritance direction), while sibling units in one file are
    /// separated — which is what stops a construct in unit 1 from certifying a function reference in unit 2.</summary>
    internal static List<string> UnitScopes(string source)
    {
        var scopes = new List<string>();
        var sb = new StringBuilder();
        int depth = 0;
        foreach (string raw in source.Split('\n'))
        {
            string line = StripComment(raw);
            sb.Append(line).Append('\n');
            if (Regex.IsMatch(line, @"\bPROGRAM-ID\b", RegexOptions.IgnoreCase)) depth++;
            if (!Regex.IsMatch(line, @"\bEND\s+PROGRAM\b", RegexOptions.IgnoreCase)) continue;
            if (--depth > 0) continue;
            depth = 0;
            scopes.Add(sb.ToString());
            sb.Clear();
        }
        if (sb.ToString().Trim().Length > 0) scopes.Add(sb.ToString());
        return scopes;
    }

    internal static bool ScopeWitnesses(string scope, string cobolFn, Regex construct) =>
        construct.IsMatch(scope)
        && Regex.IsMatch(scope, @"FUNCTION\s+" + Regex.Escape(cobolFn) + @"\s*\(", RegexOptions.IgnoreCase);

    /// <summary>The conformance-suite tests accepted at an edition, which the runner RUNS and COMPARES rather than
    /// only compiling them.</summary>
    private static IEnumerable<(string Name, string Source)> RunnableGoldens()
    {
        foreach (var (ed, tests) in SuiteManifest.Manifest.Editions)
        {
            foreach (SuiteTest test in tests.Values.Where(t => t.Expect == "accept").OrderBy(t => t.Id, StringComparer.Ordinal))
            {
                yield return ($"{ed}/{test.Id}", File.ReadAllText(Path.Combine(SuiteManifest.Root, test.Source)));
            }
        }
    }

    [Fact]
    public void EveryModeFlagArm_HasARunnableGoldenExercisingTheOnState()
    {
        var pairs = RenderedFlagPairs();
        var goldens = RunnableGoldens().Select(g => (g.Name, Scopes: UnitScopes(g.Source))).ToList();
        Assert.NotEmpty(goldens);

        var missing = new List<string>();
        foreach ((string fn, string flag) in pairs)
        {
            (_, string construct, string rule, Regex match) = Flags.Single(f => f.Flag == flag);
            bool covered = goldens.Any(g => g.Scopes.Any(s => ScopeWitnesses(s, fn, match)));
            if (!covered)
                missing.Add($"FUNCTION {fn} takes the {flag} argument ({rule}), and no enabled+runnable golden "
                            + $"compiles it in a source unit specifying {construct}");
        }
        Assert.True(missing.Count == 0,
            "The compiler-side arm that computes a runtime intrinsic argument is UNMEASURED for:\n  "
            + string.Join("\n  ", missing)
            + "\nAdd a conformance golden (with its .out) that compiles the function under that construct — a unit "
            + "test passing the value as a literal exercises the runtime scan and nothing left of it. kb/Work PB256.");
    }

    /// <summary>⛔ THE FAILURE BRANCH, FIRED ONCE. A golden whose header merely NAMES the clause must not count
    /// as a witness, and neither must a sibling unit's clause — the two ways this gate could go green while the
    /// arm stayed untested.</summary>
    [Fact]
    public void CommentedConstruct_IsNotAWitness()
    {
        Regex dpc = Flags.Single(f => f.Flag == "CommaFlag").Match;

        const string commentOnly =
            "      *> DECIMAL-POINT IS COMMA is NOT specified here\n"
            + "       PROGRAM-ID. X.\n"
            + "           COMPUTE R = FUNCTION TEST-NUMVAL(\"1,5\")\n";
        Assert.All(UnitScopes(commentOnly), s => Assert.False(ScopeWitnesses(s, "TEST-NUMVAL", dpc)));

        const string siblingUnits =
            "       PROGRAM-ID. A.\n"
            + "           DECIMAL-POINT IS COMMA.\n"
            + "           DISPLAY 1\n"
            + "       END PROGRAM A.\n"
            + "       PROGRAM-ID. B.\n"
            + "           COMPUTE R = FUNCTION TEST-NUMVAL(\"1,5\")\n"
            + "       END PROGRAM B.\n";
        var scopes = UnitScopes(siblingUnits);
        Assert.Equal(2, scopes.Count);
        Assert.All(scopes, s => Assert.False(ScopeWitnesses(s, "TEST-NUMVAL", dpc)));

        // …and the positive control, so "false everywhere" is not how this test passes.
        const string oneUnit =
            "       PROGRAM-ID. C.\n"
            + "           DECIMAL-POINT IS COMMA.\n"
            + "           COMPUTE R = FUNCTION TEST-NUMVAL(\"1,5\")\n"
            + "       END PROGRAM C.\n";
        Assert.Contains(UnitScopes(oneUnit), s => ScopeWitnesses(s, "TEST-NUMVAL", dpc));

        // A CONTAINED program inherits its container's clause and IS one scope with it (§11.9.4 / §8.3.2).
        const string contained =
            "       PROGRAM-ID. OUTER.\n"
            + "           DECIMAL-POINT IS COMMA.\n"
            + "       PROGRAM-ID. INNER.\n"
            + "           COMPUTE R = FUNCTION TEST-NUMVAL(\"1,5\")\n"
            + "       END PROGRAM INNER.\n"
            + "       END PROGRAM OUTER.\n";
        Assert.Single(UnitScopes(contained));
        Assert.Contains(UnitScopes(contained), s => ScopeWitnesses(s, "TEST-NUMVAL", dpc));
    }
}
