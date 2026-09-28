// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The edition gate on the §12.3.7 SPECIAL-NAMES <c>LOCALE</c> clause (fix-queue PB25).
///
/// <para>The clause is an <b>§A.4.9 item 10</b> optional-locale element, so at 2002+ it must PARSE and be
/// DIAGNOSED as documented non-support (COBOLNET1518) rather than fail as a parse error — non-support is
/// conformant per §4.2.7 / §A.4.1 only when it is diagnosed.</para>
///
/// <para>⛔ WHY THE GATE IS LOAD-BEARING, AND WHY IT IS TESTED. <b>LOCALE is reserved only from 2002</b>
/// (§8.9; <c>reserved-words.json</c> r85 = false, r2002/r2014/r2023 = true). At COBOL-85 it is an ordinary
/// user-defined word, so <c>SPECIAL-NAMES. LOCALE IS FOO.</c> is a perfectly legal implementor-switch entry
/// there and must keep parsing as one. A predicate that fired at every edition would turn legal 85 source into a
/// documented-non-support error — the exact class of breakage <c>edition_gate_sweep</c> exists for.</para>
/// </summary>
public sealed class SpecialNamesLocaleEditionTests
{
    private static DiagnosticBag Parse(string specialNames, int edition)
    {
        string src = "IDENTIFICATION DIVISION.\nPROGRAM-ID. SNLOCALE.\nENVIRONMENT DIVISION.\n"
            + "CONFIGURATION SECTION.\nSPECIAL-NAMES.\n" + specialNames
            + "DATA DIVISION.\nWORKING-STORAGE SECTION.\n01 X PIC X(3) VALUE \"AbC\".\n"
            + "PROCEDURE DIVISION.\nMAIN.\n    DISPLAY X\n    STOP RUN.\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_snloc_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = edition }.Parse(path, diags);
            return diags;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    /// <summary>At 2002+ the clause PARSES (and since kb/Work PB64 T1 the binder DECLARES the locale-name; before, it
    /// drew the cited COBOLNET1518). A parse error here would mean no diagnostic ever got the chance to speak.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void TheLocaleClause_Parses_SoTheBinderCanDiagnoseIt(int edition)
    {
        var diags = Parse("    LOCALE FR IS \"fr_FR\".\n", edition);
        Assert.False(diags.HasErrors,
            "the SPECIAL-NAMES LOCALE clause must PARSE at 2002+ so it can be diagnosed as §A.4.9 documented "
            + "non-support; a parse error is an unexplained rejection. Got: "
            + string.Join(" | ", diags.Diagnostics.Select(d => d.ToString())));
    }

    /// <summary>⛔ THE COUNTERPART THAT MUST NOT BREAK: at COBOL-85, LOCALE is a user word, so
    /// <c>LOCALE IS FOO</c> is an ordinary implementor-switch entry and still parses.</summary>
    [Fact]
    public void At85_LocaleIsAnOrdinaryUserWord_AndTheSwitchEntryStillParses()
    {
        var diags = Parse("    LOCALE IS FOO.\n", 85);
        Assert.False(diags.HasErrors,
            "`SPECIAL-NAMES. LOCALE IS FOO.` is legal COBOL-85 (LOCALE is not reserved until 2002, §8.9) and must "
            + "still parse as an implementor-switch entry. Got: "
            + string.Join(" | ", diags.Diagnostics.Select(d => d.ToString())));
    }

    /// <summary>…and the same bare switch shape is still a switch entry at 2023, because the predicate requires a
    /// locale-NAME after the word. Without this, the predicate would annex `LOCALE IS mnemonic` as a malformed
    /// locale clause at every modern edition.</summary>
    [Fact]
    public void At2023_ABareLocaleIsSwitchEntry_IsNotAnnexedAsALocaleClause()
    {
        var diags = Parse("    LOCALE IS FOO.\n", 2023);
        Assert.False(diags.HasErrors,
            "`LOCALE IS FOO` has no locale-name, so it is not the §12.3.7 LOCALE clause and must fall through to "
            + "implementorSwitchEntry. Got: " + string.Join(" | ", diags.Diagnostics.Select(d => d.ToString())));
    }

    /// <summary>The FULL pipeline's diagnostics — the parse-only harness above cannot see the §8.9 funnel, which
    /// runs in the VersionConformancePass after bind.</summary>
    private static IReadOnlyList<string> CompileErrors(string specialNames)
    {
        string dir = Path.Combine(Path.GetTempPath(), "cn_snloc_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "snloc.cob");
            File.WriteAllText(src,
                "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. SNLOCFULL.\n"
                + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n"
                + specialNames
                + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
                + "       01 X PIC X(3) VALUE \"AbC\".\n"
                + "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY X\n           STOP RUN.\n");
            return CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "snloc.dll"), DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto)).Errors;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>
    /// ⛔ THE CLAUSE'S OWN KEYWORD IS NOT A USER-DEFINED WORD (fix-queue PB27). <c>LOCALE</c> is not a lexer
    /// token, so §12.3.7's <c>localeClause</c> matches the clause KEYWORD through <c>cobolWord</c> — and the §8.9
    /// funnel was reading it as a user-defined word, printing <i>"'LOCALE' is a reserved word … and cannot be
    /// used as a user-defined word"</i> beside the correct COBOLNET1518 about a program that uses it as no such
    /// thing. A true diagnostic and a false one, together.
    /// <para>⚠ WHY A NEGATIVE FIXTURE COULD NOT CATCH THIS, and why the assertion below is a NOT: a negative
    /// fixture's <c>.err</c> names ONE expected code, so <c>pb25-special-names-locale-a49</c> stayed green
    /// through the whole defect — it asserted 1518 was present and could not ask whether anything else was
    /// (feedback_green_test_can_hold_a_gap_open).</para>
    /// </summary>
    [Fact]
    public void TheLocaleKeyword_IsNotFunneledAsAUserDefinedWord()
    {
        // Since kb/Work PB64 T1 the clause DECLARES the locale-name (no COBOLNET1518 any more): the program compiles
        // clean — and still nothing is said about the clause's own keyword.
        var errors = CompileErrors("           LOCALE FR IS \"fr_FR\".\n");
        Assert.Empty(errors);
    }

    /// <summary>⛔ THE FAILING DIRECTION, which is what makes the exemption position-EXACT rather than a blanket
    /// over the clause. Slot [1] is locale-name-1 — a genuine user-defined word — so a §8.9-reserved word there
    /// is still a violation. <c>LOCALE</c> itself is the probe: it is reserved at 2002+ AND lexes as an
    /// identifier, so it reaches the funnel (a token-lexed reserved word like MOVE dies in the parser and never
    /// tests this at all — the first probe of this did exactly that).</summary>
    [Fact]
    public void ButLocaleNameItself_IsStillFunneled()
    {
        var errors = CompileErrors("           LOCALE LOCALE IS \"fr_FR\".\n");
        Assert.Contains(errors, e => e.Contains("COBOLNET0901") && e.Contains("LOCALE"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1518"));   // the clause itself is implemented (PB64 T1)
    }
}
