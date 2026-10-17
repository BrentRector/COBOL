// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Keeps the ISO §15.x.1 RESULT-TYPE tables wired in, and keeps the population derived from the STANDARD rather
/// than from anybody's memory (fix-queue PB15).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ THE DEFECT THIS GUARDS AGAINST WAS FOUND TWICE AND FIXED PARTIALLY BOTH TIMES. Twenty §15 functions have a
/// type that DEPENDS ON THEIR ARGUMENTS — "The type of this function depends on the type of argument-1 as
/// follows", a two-column table in the function's own §15.x.1 clause. The catalog stored ONE SCALAR type, so each
/// such function needed a hand-written exception in <c>IntrinsicBinder</c>. CA25 wrote one covering three names
/// (<c>RuntimeMethod is "UpperCase" or "LowerCase" or "Reverse"</c>); V54 wrote a second covering MAX and MIN.
/// Nothing reminded anyone of the other TEN, and nothing could: a name list has no way to say what it omits.
/// </para>
/// <para>
/// Measured 2026-08-04, every one a SILENT under-rejection — a national result mislabelled alphanumeric walks
/// straight through the §14.9.25.3 Table-16 MOVE guard, so <c>MOVE FUNCTION TRIM(national-item) TO PIC X</c>
/// compiled clean where the standard requires a diagnostic. Eight functions: TRIM, SUBSTITUTE, CONCAT,
/// BASECONVERT and the four FORMATTED-*.
/// </para>
/// <para>
/// ⭐ SO THIS TEST DOES NOT CARRY A LIST. It re-derives the population from <c>specs/ISO_COBOL.md</c> on every
/// run, by the STRUCTURAL key — a markdown table in §15.x.1 whose header names the "Function type" column. A
/// future edition that gives an existing function a result-type table, or adds a function that has one, fails
/// this test until its catalog row declares a rule. That is what makes the next case automatic rather than
/// merely making this case small (CLAUDE.md rule 5).
/// </para>
/// <para>
/// ⚠ Keying on the TABLE and not on the sentence is deliberate: the clauses do not agree on wording. §15.38.1
/// says "depends on the argument type", §15.39.1 "depends on the type of argument-1", §15.18.1 "depends upon the
/// argument types". A first cut of this scanner keyed on the prose and silently found ten of the twenty — the
/// dead-guard failure, inside the guard against a dead lookup.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class IntrinsicResultTypeDriftTests
{
    private static string CatalogSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));

    private static string BinderSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs"));

    private static string ResolverSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicResultType.cs"));

    /// <summary>
    /// Every §15 function whose §15.x.1 "General" clause carries a Function-type TABLE — i.e. whose type is a
    /// function of its arguments. Derived from the standard itself, never from a list kept here.
    /// </summary>
    private static Dictionary<string, string> SpecArgumentDependentFunctions()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));

        // "### 15.39 FORMATTED-DATE function" → clause 15.39 = FORMATTED-DATE
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var nameRx = new Regex(@"^### (?<c>15\.\d+) (?<n>[A-Z0-9-]+) function\s*$", RegexOptions.Compiled);
        foreach (string l in lines)
            if (nameRx.Match(l) is { Success: true } m)
                names[m.Groups["c"].Value] = m.Groups["n"].Value;

        Assert.True(names.Count >= 90,
            $"only {names.Count} §15 function headings parsed from the spec — the transcription's heading shape "
            + "changed and this guard has gone blind; fix the regex, do not lower the floor.");

        var generalRx = new Regex(@"^#### (?<c>15\.\d+)\.1 General\s*$", RegexOptions.Compiled);
        var headerRx = new Regex(@"\|\s*\**Function type\**\s*\|", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < lines.Length; i++)
        {
            if (generalRx.Match(lines[i]) is not { Success: true } g) continue;
            string clause = g.Groups["c"].Value;
            bool hasTable = false;
            for (int j = i + 1; j < lines.Length && !lines[j].StartsWith("###", StringComparison.Ordinal)
                                                 && !lines[j].StartsWith("#### ", StringComparison.Ordinal); j++)
                if (headerRx.IsMatch(lines[j])) { hasTable = true; break; }
            if (hasTable && names.TryGetValue(clause, out string? fn))
                found[fn] = clause + ".1";
        }
        return found;
    }

    /// <summary>Every catalog row, as (function, declared result rule — "Fixed" when the column is omitted).</summary>
    private static List<(string Name, string Rule)> CatalogRows()
    {
        var rx = new Regex(
            """Add\(new\("(?<n>[A-Z0-9-]+)",\s*IntrinsicType\.\w+,(?<body>[^;]*?)\)\);""",
            RegexOptions.Compiled);
        var ruleRx = new Regex(@"Result:\s*IntrinsicResultRule\.(?<r>\w+)", RegexOptions.Compiled);
        var rows = new List<(string, string)>();
        foreach (Match m in rx.Matches(CatalogSource()))
        {
            string body = m.Groups["body"].Value;
            rows.Add((m.Groups["n"].Value,
                      ruleRx.Match(body) is { Success: true } r ? r.Groups["r"].Value : "Fixed"));
        }
        return rows;
    }

    [Fact]
    public void TheScanners_StillSeeTheirSubjects()
    {
        var spec = SpecArgumentDependentFunctions();
        var rows = CatalogRows();
        // Floors, not exact counts: adding functions must not fail these, but a regex that silently stops
        // matching must. 20 argument-dependent clauses were measured in ISO/IEC 1989:2023.
        Assert.True(spec.Count >= 20,
            $"only {spec.Count} §15.x.1 result-type tables found in specs/ISO_COBOL.md (expected at least 20) — "
            + "the scanner has gone blind; fix it, do not lower the floor.");
        Assert.True(rows.Count >= 79,
            $"only {rows.Count} catalog rows parsed — the Add(new(...)) shape changed and this guard has gone "
            + "blind; fix the regex, do not lower the floor.");
    }

    /// <summary>
    /// ⛔ THE CENTRAL ASSERTION, and the one that is impossible to satisfy with a hand-maintained list: every
    /// function the STANDARD gives an argument-dependent type declares a rule other than Fixed.
    /// </summary>
    [Fact]
    public void EveryFunctionWithASpecResultTypeTable_DeclaresARule()
    {
        var spec = SpecArgumentDependentFunctions();
        var rows = CatalogRows().ToDictionary(r => r.Name, r => r.Rule, StringComparer.OrdinalIgnoreCase);

        var missing = spec
            .Where(kv => rows.TryGetValue(kv.Key, out string? rule) && rule == "Fixed")
            .Select(kv => $"{kv.Key} ({kv.Value})")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            "these §15 functions carry a RESULT-TYPE TABLE in the standard but their IntrinsicCatalog row still "
            + $"declares IntrinsicResultRule.Fixed: [{string.Join(", ", missing)}]. Their type DEPENDS ON THEIR "
            + "ARGUMENTS, so a scalar mislabels every call that selects another row of the table — silently, "
            + "because a mislabelled national result passes the §14.9.25.3 Table-16 MOVE guard instead of being "
            + "rejected by it. Add the Result: column; do not add a name to a list in the binder (PB15).");
    }

    /// <summary>
    /// The converse, and the reason this cannot rot in the other direction: a row may not claim an
    /// argument-dependent rule the standard does not give it. An invented rule REJECTS LEGAL SOURCE, which
    /// CLAUDE.md rule 4 forbids outright — the same asymmetry <c>IntrinsicArgumentRules</c> documents.
    /// </summary>
    [Fact]
    public void NoRowClaimsARule_TheStandardDoesNotGiveIt()
    {
        var spec = SpecArgumentDependentFunctions();
        var invented = CatalogRows()
            .Where(r => r.Rule != "Fixed" && !spec.ContainsKey(r.Name))
            .Select(r => $"{r.Name} → {r.Rule}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        Assert.True(invented.Count == 0,
            $"catalog row(s) declare an argument-dependent result rule with no §15.x.1 result-type table behind "
            + $"it: [{string.Join(", ", invented)}]. Either the standard grew a table this scanner cannot see "
            + "(fix the scanner) or the rule is a guess (delete it).");
    }

    /// <summary>
    /// ⛔ THE RULE IS READ. This is the exact fact CA25 and V54 each left half-true, and it cannot be inferred
    /// from the catalog looking well-maintained — <c>IntrinsicArgumentClassDriftTests</c> exists because a
    /// fully-populated column with no reader is the hardest defect in this codebase to see.
    /// </summary>
    [Fact]
    public void TheResultTypeRule_IsActuallyWiredIn()
    {
        string binder = BinderSource();
        Assert.True(binder.Contains("IntrinsicResultType.Resolve", StringComparison.Ordinal),
            "IntrinsicBinder no longer resolves the §15.x.1 result-type tables — every argument-dependent "
            + "function is back to the catalog's hardcoded scalar type (PB15).");

        // ⚠ MATCH CODE, NOT PROSE. The first version of this assertion ran over the whole file and fired on the
        // COMMENT that explains what the two deleted name lists were — a guard tripped by its own documentation,
        // which would have taught the next person to delete the explanation rather than keep the invariant.
        // Caught by running the guard instead of assuming it (feedback_green_gates_arent_evidence).
        string code = StripComments(binder);
        Assert.False(
            Regex.IsMatch(code, @"RuntimeMethod is ""UpperCase""|Name is ""MAX"" or ""MIN"""),
            "a hand-written function-name list has reappeared in IntrinsicBinder's result-category resolution. "
            + "That is the shape PB15 removed: it cannot say what it omits, which is how ten functions stayed "
            + "wrong through two fixes. Put the rule on the catalog row instead.");
    }

    /// <summary>The source with <c>//</c> and <c>///</c> lines removed, so a source-form guard cannot fire on the
    /// prose describing the very pattern it forbids.</summary>
    private static string StripComments(string source) =>
        string.Join('\n', source.Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));

    /// <summary>
    /// ⛔ EVERY BESPOKE BIND PATH RESOLVES TOO. TRIM and SUBSTITUTE build their own <c>BoundIntrinsicCall</c>
    /// (they parse phrase keywords), so a correct catalog row alone left them wrong — the two-arm dispatch in its
    /// silent form, this repository's most reproducible defect shape. Any construction site that hardcodes a
    /// string category for a function whose row is NOT Fixed is that bug returning.
    /// </summary>
    [Fact]
    public void NoBespokeBindPath_HardcodesAStringResultCategory()
    {
        string binder = BinderSource();
        var argumentDependent = CatalogRows()
            .Where(r => r.Rule != "Fixed").Select(r => r.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // The bespoke binds are named for their function; find each `BindXxx` method that constructs a node with
        // a literal Alphanumeric/National category and check it is not one of the argument-dependent functions.
        foreach (Match m in Regex.Matches(binder,
                     @"private BoundExpr Bind(?<fn>\w+)\((?s).*?(?=\n    /// <summary>|\n    private |\z)"))
        {
            string body = m.Value;
            if (!Regex.IsMatch(body,
                    @"new BoundIntrinsicCall\([^)]*PicCategory\.(Alphanumeric|National)\)")) continue;

            string method = m.Groups["fn"].Value;
            string hit = argumentDependent.FirstOrDefault(
                f => method.Replace("-", "").Equals(f.Replace("-", ""), StringComparison.OrdinalIgnoreCase)) ?? "";
            Assert.True(hit.Length == 0,
                $"Bind{method} constructs its BoundIntrinsicCall with a HARDCODED string category, but FUNCTION "
                + $"{hit}'s catalog row declares an argument-dependent result rule. A bespoke bind path that "
                + "does not call IntrinsicResultType.Resolve leaves the defect intact with the fix present and "
                + "every test green — that is exactly how PB15 survived CA25 and V54 (TRIM and SUBSTITUTE).");
        }
    }

    /// <summary>Every member of the rule enum is handled by the resolver — an unhandled member is a silent Fixed.</summary>
    [Fact]
    public void EveryResultRule_IsHandledByTheResolver()
    {
        string src = ResolverSource();
        int at = src.IndexOf("public enum IntrinsicResultRule", StringComparison.Ordinal);
        Assert.True(at > 0, "IntrinsicResultRule is gone");
        string block = src[at..src.IndexOf("\n}", at, StringComparison.Ordinal)];

        var members = Regex.Matches(block, @"^\s{4}(?<m>[A-Z]\w+),\s*$", RegexOptions.Multiline)
            .Select(m => m.Groups["m"].Value).ToList();
        Assert.True(members.Count >= 7,
            $"only {members.Count} IntrinsicResultRule members parsed — the enum's shape changed and this guard "
            + "has gone blind; fix the regex, do not lower the floor.");

        int resolveAt = src.IndexOf("public static IntrinsicType Resolve", StringComparison.Ordinal);
        Assert.True(resolveAt > 0, "IntrinsicResultType.Resolve is gone");
        string resolver = src[resolveAt..];

        var unhandled = members
            .Where(m => m != "Fixed" && !resolver.Contains($"IntrinsicResultRule.{m}", StringComparison.Ordinal))
            .ToList();
        Assert.True(unhandled.Count == 0,
            $"IntrinsicResultRule member(s) with no arm in Resolve: [{string.Join(", ", unhandled)}]. An "
            + "unhandled member falls through to the declared scalar type — a rule that reads as implemented and "
            + "does nothing.");
    }

    /// <summary>The PB15 fixtures exist and are REGISTERED — an unregistered golden never runs.</summary>
    [Fact]
    public void ThePb15Fixtures_ExistAndAreRegistered()
    {
        var refusals = SuiteManifest.Manifest.Refusals;
        foreach (string name in new[]
                 {
                     "pb15-trim-national-result-to-an",
                     "pb15-substitute-national-result-to-an",
                     "pb15-concat-national-result-to-an",
                     "pb15-formatted-date-national-result-to-an",
                 })
        {
            Assert.True(File.Exists(TestRepo.SuiteCase(name)), $"{name}.cob missing");
            Assert.True(refusals.ContainsKey(name),
                $"{name} has no entry in conformance-suite/wiseowl/diagnostics.json — it would never be refused");
        }

        Assert.True(SuiteManifest.Manifest.Editions["2023"].ContainsKey("pb15_result_type_follows_argument"),
            "pb15_result_type_follows_argument is not a conformance-suite test accepted at 2023");
    }
}
