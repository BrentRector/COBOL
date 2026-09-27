// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §4.2.6 non-support screen for <c>ARITHMETIC IS STANDARD-BINARY</c> fires for EVERY kind of source unit
/// that can carry an OPTIONS paragraph — and the set of such units is read out of the GRAMMAR, not out of a list
/// somebody remembered to update.
///
/// <para><b>Why this test exists.</b> The screen used to live in <c>DataBinder</c>, which never sees a METHOD's
/// or an INTERFACE's OPTIONS paragraph. Measured 2026-08-31 (kb/Work PB197): <c>METHOD-ID … OPTIONS. ARITHMETIC
/// IS STANDARD-BINARY.</c> compiled at <c>--std 2014</c> with <b>no diagnostic at all</b>, and INTERFACE-ID was
/// hollow the same way — a second arm the original finding did not name, found only by sweeping all of them.
/// §4.2.6 requires "a warning mechanism at compile time to indicate use of syntactically-detectable
/// processor-dependent language elements not supported by that implementation"; two of seven unit kinds had
/// none.</para>
///
/// <para><b>What makes it a drift test rather than seven more assertions.</b> The fixture table is checked
/// against the productions the grammar actually gives an <c>optionsParagraph</c> to. Add a new options-bearing
/// production — a future FUNCTION-ID skeleton, a new OO unit — and this fails until a fixture covers it, which
/// is the difference between "the arms we knew about are screened" and "every arm is screened".</para>
/// </summary>
public sealed class ArithmeticModeScreenDriftTests : CobolNetTestBase
{
    private const string Opt = "       OPTIONS.\n           ARITHMETIC IS STANDARD-BINARY.\n";

    /// <summary>One compilable fixture per grammar production that carries an <c>optionsParagraph</c>. The KEY is
    /// the production's name in the .g4 — that is what ties the table to the grammar sweep below.</summary>
    private static readonly Dictionary<string, string> Fixtures = new()
    {
        // identificationParagraph is the PROGRAM-ID / FUNCTION-ID arm (CobolParserCore.g4) — both spellings are
        // covered, since a function's identification body is a different path through the same production.
        ["identificationParagraph"] = $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. SBDRIFTPROG.
            {Opt}       PROCEDURE DIVISION.
                   MAIN.
                       DISPLAY "X".
                       STOP RUN.
            """,
        ["classDefinition"] = $"""
                   IDENTIFICATION DIVISION.
                   CLASS-ID. SBDRIFTCLS.
            {Opt}       END CLASS SBDRIFTCLS.
            """,
        ["factoryParagraph"] = $"""
                   IDENTIFICATION DIVISION.
                   CLASS-ID. SBDRIFTFAC.
                   IDENTIFICATION DIVISION.
                   FACTORY.
            {Opt}       PROCEDURE DIVISION.
                   END FACTORY.
                   END CLASS SBDRIFTFAC.
            """,
        ["objectParagraph"] = $"""
                   IDENTIFICATION DIVISION.
                   CLASS-ID. SBDRIFTOBJ.
                   IDENTIFICATION DIVISION.
                   OBJECT.
            {Opt}       PROCEDURE DIVISION.
                   END OBJECT.
                   END CLASS SBDRIFTOBJ.
            """,
        ["interfaceDefinition"] = $"""
                   IDENTIFICATION DIVISION.
                   INTERFACE-ID. SBDRIFTIFC.
            {Opt}       END INTERFACE SBDRIFTIFC.
            """,
        ["methodDefinition"] = $"""
                   IDENTIFICATION DIVISION.
                   CLASS-ID. SBDRIFTMTH.
                   IDENTIFICATION DIVISION.
                   FACTORY.
                   PROCEDURE DIVISION.
                   METHOD-ID. M1.
            {Opt}       DATA DIVISION.
                   LINKAGE SECTION.
                   01  R PIC X(4).
                   PROCEDURE DIVISION RETURNING R.
                   MAIN-P.
                       MOVE "OK" TO R.
                   END METHOD M1.
                   END FACTORY.
                   END CLASS SBDRIFTMTH.
            """,
    };

    /// <summary>Every .g4 rule whose BODY references <c>optionsParagraph</c> (the rule of that name excepted).
    /// The repo's grammar style puts a rule's name alone on its line, followed by an indented <c>:</c> body.</summary>
    private static SortedSet<string> ProductionsCarryingOptions()
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        var ruleName = new Regex(@"^(?<name>[a-z][A-Za-z0-9_]*)\s*$");
        foreach (string path in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4",
                                                         SearchOption.AllDirectories))
        {
            string? current = null;
            foreach (string line in File.ReadAllLines(path))
            {
                var m = ruleName.Match(line);
                if (m.Success) { current = m.Groups["name"].Value; continue; }
                if (line.TrimStart().StartsWith("//")) continue;                    // a comment is not a reference
                string body = line.Split("//")[0];                                  // nor is a trailing comment
                if (current is not null and not "optionsParagraph" && body.Contains("optionsParagraph"))
                    found.Add(current);
            }
        }
        return found;
    }

    [Fact]
    public void EveryOptionsBearingProduction_HasAFixture()
    {
        var grammar = ProductionsCarryingOptions();
        Assert.NotEmpty(grammar);   // a sweep that found nothing would pass every other assertion vacuously
        var covered = new SortedSet<string>(Fixtures.Keys, StringComparer.Ordinal);
        Assert.True(grammar.SetEquals(covered),
            "the grammar's options-bearing productions and this test's fixtures have diverged.\n"
            + $"  grammar : {string.Join(", ", grammar)}\n"
            + $"  fixtures: {string.Join(", ", covered)}\n"
            + "Add a fixture for the new production — a source unit that can select ARITHMETIC IS STANDARD-BINARY "
            + "and is never screened is exactly the hole kb/Work PB197 recorded.");
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void StandardBinary_IsDeclinedInEveryUnitKind_PinnedToSpec(string production)
    {
        // §4.2.6: "An implementation shall provide a warning mechanism at compile time to indicate use of
        // syntactically-detectable processor-dependent language elements not supported by that implementation."
        // WiseOwl COBOL's is the hard COBOLNET0806 (see docs/CONFORMANCE.md §1 on why an error, not a warning).
        var errors = CompileAt2014(production, Fixtures[production]);
        int count = errors.Count(e => e.Contains("COBOLNET0806", StringComparison.Ordinal));
        Assert.True(count >= 1,
            $"[{production}] ARITHMETIC IS STANDARD-BINARY drew no COBOLNET0806. Diagnostics:\n"
            + string.Join("\n", errors.DefaultIfEmpty("(none)")));
        // Exactly once, not once per binder that happens to look: the screen sits at the single point the
        // clause is READ, so §11.9.4 GR1 inheritance never re-reports a clause the unit did not write.
        Assert.Equal(1, count);
    }

    [Fact]
    public void StandardBinary_PerEditionDiagnostics_AreDistinct_PinnedToSpec()
    {
        // ⛔ MEASURED, after a falsification attempt showed the two legs were NOT distinguished by the negative
        // corpus alone: swapping the 2002 case's expected code to COBOLNET0806 still PASSED, because 0806 fires
        // at 2002 too. The legs ARE different — just not in the direction the single-substring check can see —
        // so the discrimination is pinned here, negatives included.
        //
        //   --std 2002 : 0900 (the introduction gate — STANDARD-BINARY arrives in COBOL-2014) AND 0806
        //   --std 2014 : 0806 alone
        //   --std 2023 : 0806 AND 0903 (the obsolescence flag — §8.8.1.4.1 NOTE 1 / Annex F.2 item 3)
        //
        // Asserting the ABSENCES is what makes this a test: without "no 0900 at 2014" an introduction-gate
        // regression that fired at every edition would look identical to a healthy compiler.
        string source = Fixtures["identificationParagraph"];

        var at2002 = CompileAt("e2002", source, 2002);
        Assert.Contains(at2002, d => d.Contains("COBOLNET0900", StringComparison.Ordinal));
        Assert.Contains(at2002, d => d.Contains("COBOLNET0806", StringComparison.Ordinal));

        var at2014 = CompileAt("e2014", source, 2014);
        Assert.Contains(at2014, d => d.Contains("COBOLNET0806", StringComparison.Ordinal));
        Assert.DoesNotContain(at2014, d => d.Contains("COBOLNET0900", StringComparison.Ordinal));
        Assert.DoesNotContain(at2014, d => d.Contains("COBOLNET0903", StringComparison.Ordinal));

        var at2023 = CompileAt("e2023", source, 2023);
        Assert.Contains(at2023, d => d.Contains("COBOLNET0806", StringComparison.Ordinal));
        Assert.Contains(at2023, d => d.Contains("COBOLNET0903", StringComparison.Ordinal));
        Assert.DoesNotContain(at2023, d => d.Contains("COBOLNET0900", StringComparison.Ordinal));
    }

    public static TheoryData<string> FixtureNames()
    {
        var data = new TheoryData<string>();
        foreach (string k in Fixtures.Keys.Order(StringComparer.Ordinal)) data.Add(k);
        return data;
    }

    private List<string> CompileAt2014(string tag, string source) => CompileAt(tag, source, 2014);

    private List<string> CompileAt(string tag, string source, int dialect)
    {
        string src = Path.Combine(TempDir, $"{tag}_{dialect}.cob");
        File.WriteAllText(src, source);
        var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: dialect));
        // BOTH channels: 0806/0900 are errors, 0903 is a WARNING, and a test that read only Errors would report
        // "the obsolescence flag is missing" for a compiler that emits it perfectly.
        return [.. r.Errors, .. r.Warnings];
    }
}
