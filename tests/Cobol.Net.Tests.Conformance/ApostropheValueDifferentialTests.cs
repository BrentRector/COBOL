// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// PHASE-05 Step 13 (exit criterion #6) — apostrophe-delimited literals, ISO/IEC 1989:2023 §8.3.3.1: the
/// quotation-mark and apostrophe forms are EQUAL-STANDING (a doubled OPENING delimiter is one embedded delimiter),
/// in every edition. Each case compiles the SAME program twice — once with '-delimited literals, once with the
/// "-delimited twin — and asserts byte-identical output. This locks the P5.1 <c>CobolLiteral</c> fix (the
/// hard-coded '"'-only guards silently miscompiled apostrophe VALUE/ALL/REPLACING literals) with a regression
/// golden across the full VALUE-init + figurative + INITIALIZE paths.
/// </summary>
public sealed class ApostropheValueDifferentialTests
{
    private static string RunOf(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Apos_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, source);
            string dll = Path.Combine(dir, "prog.dll");
            var r = CompiledProgramCache.Compile(new CobolNet.CompilerDriver.Options(src, dll, DialectLevel: 85, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "must compile strict: " + string.Join("\n", r.Errors));
            var (ok, stdout, detail) = CutRunner.Run(dll, dir);
            Assert.True(ok, detail);
            return stdout;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>Elementary + group-child VALUEs: the ' program equals the " program byte-for-byte.</summary>
    [Fact]
    public void ValueClauses_ApostropheFormEqualsQuoteForm()
    {
        const string body = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. APOS1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(3) VALUE {Q}AB{Q}.
            01 WS-G.
               05 WS-G1 PIC X(2) VALUE {Q}XY{Q}.
               05 WS-G2 PIC 9(3) VALUE 7.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "A=[" WS-A "]".
                DISPLAY "G=[" WS-G "]".
                STOP RUN.
            """;
        string apos = RunOf(body.Replace("{Q}", "'"));
        string quot = RunOf(body.Replace("{Q}", "\""));
        Assert.Equal(quot, apos);
        Assert.Contains("A=[AB ]", apos);
        Assert.Contains("G=[XY007]", apos);
    }

    /// <summary>The DOUBLED-OPENING-DELIMITER rule (§8.3.3.2.3 r3 — one embedded delimiter character), per form: the
    /// contents legitimately DIFFER across forms (an embedded ' vs an embedded "), so each asserts its own decode
    /// — the case the '"'-only guards silently miscompiled for the apostrophe form.</summary>
    [Fact]
    public void EmbeddedDelimiter_DecodesToOneCharacter_PerForm()
    {
        const string body = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. APOS3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-E PIC X(4) VALUE {LIT}.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "E=[" WS-E "]".
                STOP RUN.
            """;
        Assert.Contains("E=[A'B ]", RunOf(body.Replace("{LIT}", "'A''B'")));
        Assert.Contains("E=[A\"B ]", RunOf(body.Replace("{LIT}", "\"A\"\"B\"")));
    }

    /// <summary>ALL 'x' as a VALUE and as a MOVE sender, plus INITIALIZE … REPLACING BY 'x' (the guard sites the
    /// P5.1 fix repointed): ' equals ".</summary>
    [Fact]
    public void AllLiteralAndInitializeReplacing_ApostropheFormEqualsQuoteForm()
    {
        const string body = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. APOS2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-F PIC X(5) VALUE ALL {Q}Z{Q}.
            01 WS-M PIC X(4).
            01 WS-R.
               05 WS-R1 PIC X(3).
               05 WS-R2 PIC X(2).
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "F=[" WS-F "]".
                MOVE ALL {Q}Q{Q} TO WS-M.
                DISPLAY "M=[" WS-M "]".
                INITIALIZE WS-R REPLACING ALPHANUMERIC DATA BY {Q}K{Q}.
                DISPLAY "R=[" WS-R "]".
                STOP RUN.
            """;
        string apos = RunOf(body.Replace("{Q}", "'"));
        string quot = RunOf(body.Replace("{Q}", "\""));
        Assert.Equal(quot, apos);
        Assert.Contains("F=[ZZZZZ]", apos);
        Assert.Contains("M=[QQQQ]", apos);
    }
}
