// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The SET LOCALE formats (ISO §14.9.39 Formats 11 set-locale / 12 save-locale; Annex A.4.9 item 9) — kb/Work PB92
/// made them recognized-and-refused by name (ONE COBOLNET1518, never "'LOCALE' is not defined" plus false 0901s or a
/// bare parse error); kb/Work PB64 T1 IMPLEMENTS them. What this class pins now: every legal shape COMPILES CLEAN at
/// 2002+ (no stray 0901 about the format's own keywords, no undefined-name noise — the §8.9 funnel exempts the
/// statement's subtree), the illegal shapes draw exactly ONE named syntax-rule diagnostic, the two formats gate at the
/// 2002 edition, and the one shape that has a COBOL-85 reading keeps it. The runtime semantics are the goldens
/// pb64t1_* of conformance-suite/cases/ and the unit LocaleStateTests.
/// </summary>
public sealed class SetLocaleDispositionTests
{
    private const string Header = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB64T1T.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               SPECIAL-NAMES.
                   LOCALE FR IS "fr-FR".
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WS-P USAGE POINTER.
               01 WS-X PIC X(20).
               PROCEDURE DIVISION.
        """;

    [Theory]
    [InlineData("SET LOCALE LC_ALL TO USER-DEFAULT.")]
    [InlineData("SET LOCALE LC_TIME TO SYSTEM-DEFAULT.")]
    [InlineData("SET LOCALE LC_NUMERIC LC_TIME LC_COLLATE TO FR.")]
    [InlineData("SET LOCALE LC_TIME LC_ALL TO FR.")]                    // two different alternatives: legal, redundant
    [InlineData("SET LOCALE LC_COLLATE TO WS-P.")]
    [InlineData("SET LOCALE USER-DEFAULT TO FR.")]
    [InlineData("SET LOCALE USER-DEFAULT TO WS-P.")]
    [InlineData("SET WS-P TO LOCALE LC_ALL.")]
    [InlineData("SET WS-P TO LOCALE USER-DEFAULT.")]
    public void SetLocale_LegalShapes_CompileClean(string stmt)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Header + $"\n               {stmt}\n               STOP RUN.\n", 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.Empty(errors);
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET0901"));
    }

    [Theory]
    [InlineData("SET LOCALE LC_TIME LC_TIME TO FR.", "COBOLNET1666")]             // §5.2.6.4 — each at most once
    [InlineData("SET LOCALE USER-DEFAULT TO SYSTEM-DEFAULT.", "COBOLNET1667")]     // SR25
    [InlineData("SET LOCALE USER-DEFAULT TO USER-DEFAULT.", "COBOLNET1667")]       // SR25
    [InlineData("SET LOCALE LC_ALL TO NOPE.", "COBOLNET1664")]                     // SR26 — not a locale-name (nor an item)
    [InlineData("SET LOCALE LC_ALL TO WS-X.", "COBOLNET1668")]                     // SR27 — not a data-pointer
    [InlineData("SET WS-X TO LOCALE LC_ALL.", "COBOLNET1668")]                     // SR28 — not a data-pointer
    public void SetLocale_IllegalShapes_DrawExactlyOneNamedRule(string stmt, string code)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Header + $"\n               {stmt}\n               STOP RUN.\n", 2023);
        Assert.False(ok);
        Assert.Single(errors);
        EditionHarness.AssertHasDiagnostic(errors, code);
    }

    /// <summary>The two formats are 2002 introductions (construct rows set-locale-2002 / set-save-locale-2002): below
    /// 2002 the LC_ shapes — which have no '85 reading — draw the introduction diagnostic, not an undefined-name error.</summary>
    [Theory]
    [InlineData("SET LOCALE LC_COLLATE TO FR.")]
    [InlineData("SET WS-P TO LOCALE LC_ALL.")]
    public void SetLocale_Below2002_IsTheIntroductionDiagnostic(string stmt)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Header + $"\n               {stmt}\n               STOP RUN.\n", 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0900");
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1639"));   // never "'LOCALE' is not defined"
    }

    /// <summary>At COBOL-85 LOCALE is a user word: `SET LOCALE TO I5` over an integer item named LOCALE is Format 1
    /// (§14.9.39 — an elementary integer item receiver) and runs; the USER-DEFAULT-first shape is the one locale arm
    /// that stays edition-gated because `SET LOCALE USER-DEFAULT TO I7` IS a legal '85 two-receiver SET. The senders
    /// are index-names because §14.9.39.3 SR4 requires one for a numeric receiver ("If identifier-1 references a
    /// numeric data item, index-name-2 shall be specified" — COBOLNET2326, kb/Work PB212).</summary>
    [Fact]
    public void At85_LocaleIsAUserWord_SetFormat1Runs()
    {
        string src = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB92T85.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 LOCALE PIC 9.
                   01 USER-DEFAULT PIC 9.
                   01 T.
                      05 E PIC X OCCURS 9 INDEXED BY I5 I7.
                   PROCEDURE DIVISION.
                       SET I5 TO 5.
                       SET I7 TO 7.
                       SET LOCALE TO I5.
                       SET LOCALE USER-DEFAULT TO I7.
                       DISPLAY LOCALE USER-DEFAULT.
                       STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.Equal("77", stdout.Trim());
    }
}
