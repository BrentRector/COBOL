// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §A.4.9 locale-module EDITION GATING, arm by arm as the module went live (fix-queue PB27 §③ opened this
/// class when the elements were documented non-support and their edition claims were SUPPRESSED; every
/// suppression has now lifted): the four locale FUNCTIONS' D8 window is enforced since kb/Work PB64 T4
/// (COBOLNET1502 below it, binding at and after it), the case functions' LOCALE phrase is a 2002 construct gate
/// since T5 (COBOLNET0900 below 2002), and the NUMVAL-C / TEST-NUMVAL-C LOCALE keyword is a 2002 construct gate
/// since T6 — the LAST arm, with which COBOLNET1518 itself was deleted.
/// <para>
/// ⛔ THE FORCING FUNCTION, ANSWERED (the class summary once demanded it): the introduction years these gates
/// assert were unverifiable from the 2023 text alone — §8.11 lists intrinsic NAMES with no edition data, Annex E
/// covers only 2014→2023, the repo holds no 2002/2014 text. The construct-gate rows carry the answer the
/// catalog settled on: the LOCALE keyword and phrases ride the 2002 locale facility (Annex A.4.9's home
/// edition), LOCALE-TIME-FROM-SECONDS the provisional 2014 window (kb/Work R28 — WG4 CD 1.2 Annex D.2), all
/// marked provisional in constructs.json under ratified decision #1 (no further standards acquisition).
/// </para>
/// </summary>
public sealed class LocaleModuleNonSupportTests
{
    private static IReadOnlyList<string> CompileErrors(string procedureBody, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "cn_a49_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "a49.cob");
            File.WriteAllText(src,
                "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. A49NS.\n"
                + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
                + "       01 R PIC X(20).\n       01 S PIC 9(8) VALUE 3600.\n"
                + "       PROCEDURE DIVISION.\n       MAIN.\n" + procedureBody + "           STOP RUN.\n");
            return CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "a49.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto)).Errors;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>⚙ THE SUPPRESSION LIFTED a third time — the LAST (kb/Work PB64 T6; the module is CLAIMED whole):
    /// the LOCALE keyword of NUMVAL-C / TEST-NUMVAL-C is LIVE, so it is a 2002 construct gate — COBOLNET0900
    /// below 2002 naming the keyword — and, at and after 2002, the keyword binds (here to an UNDECLARED
    /// locale-name FR, the one undeclared-locale-name diagnostic COBOLNET1664 — this helper declares no
    /// SPECIAL-NAMES LOCALE). Never 1518 any more, at any edition — the diagnostic is deleted.</summary>
    [Theory]
    [InlineData(85, true)]
    [InlineData(2002, false)]
    [InlineData(2014, false)]
    [InlineData(2023, false)]
    public void TheNumvalCLocaleKeyword_IsEditionGated_NowThatItIsLive(int edition, bool gated)
    {
        var errors = CompileErrors("           COMPUTE S = FUNCTION NUMVAL-C(\"12.34\" LOCALE FR).\n", edition);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1518"));
        if (gated) Assert.Contains(errors, e => e.Contains("COBOLNET0900") && e.Contains("LOCALE keyword"));
        Assert.Contains(errors, e => e.Contains("COBOLNET1664"));
    }

    /// <summary>⚙ THE SUPPRESSION LIFTED a second time (kb/Work PB64 T5): the LOCALE phrase of LOWER-CASE / UPPER-CASE
    /// is LIVE, so it is a 2002 construct gate — COBOLNET0900 below 2002 naming the phrase — and, at and after 2002,
    /// the phrase binds (here to an UNDECLARED locale-name, the one undeclared-locale-name diagnostic COBOLNET1664 —
    /// this helper declares no SPECIAL-NAMES LOCALE). Never 1518 any more, at any edition.</summary>
    [Theory]
    [InlineData(85, true)]
    [InlineData(2002, false)]
    [InlineData(2014, false)]
    [InlineData(2023, false)]
    public void TheCaseFunctionLocalePhrase_IsEditionGated_NowThatItIsLive(int edition, bool gated)
    {
        var errors = CompileErrors("           MOVE FUNCTION LOWER-CASE(R LOCALE FR) TO R.\n", edition);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1518"));
        if (gated) Assert.Contains(errors, e => e.Contains("COBOLNET0900") && e.Contains("LOCALE phrase"));
        else Assert.DoesNotContain(errors, e => e.Contains("COBOLNET0900"));
        Assert.Contains(errors, e => e.Contains("COBOLNET1664"));
    }

    /// <summary>⚙ THE SUPPRESSION LIFTED (kb/Work PB64 T4 — the forcing function the class summary promised): the four
    /// locale functions bind Runtime, so their D8 window is enforced again — LOCALE-TIME-FROM-SECONDS (2014, kb/Work
    /// R28's provisional edge) is COBOLNET1502 below 2014 and binds at 2014/2023; LOCALE-DATE (2002) likewise below
    /// 2002. Never 1518 any more.</summary>
    [Theory]
    [InlineData(85, true)]
    [InlineData(2002, true)]
    [InlineData(2014, false)]
    [InlineData(2023, false)]
    public void ALocaleFunction_KeepsItsEditionWindow_NowThatItIsLive(int edition, bool rejected)
    {
        var errors = CompileErrors("           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(S) TO R.\n", edition);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1518"));
        if (rejected) Assert.Contains(errors, e => e.Contains("COBOLNET1502") && e.Contains("LOCALE-TIME-FROM-SECONDS"));
        else Assert.DoesNotContain(errors, e => e.Contains("LOCALE-TIME-FROM-SECONDS"));
    }

    /// <summary>⛔ THE FAILING DIRECTION, which is what keeps the suppression scoped to non-support rather than
    /// quietly disabling the D8 edition window for every function. TRIM is §15.90, IntroducedIn 2014 and fully
    /// SUPPORTED, so below 2014 it must still be rejected BY EDITION.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    public void ASupportedFunction_KeepsItsEditionGate(int edition)
    {
        var errors = CompileErrors("           MOVE FUNCTION TRIM(\"  ab  \") TO R.\n", edition);
        Assert.Contains(errors, e => e.Contains("COBOLNET1502") && e.Contains("TRIM"));
    }

    /// <summary>…and at its own edition that same function compiles, so the gate is a WINDOW and not a ban.</summary>
    [Fact]
    public void ASupportedFunction_CompilesAtItsOwnEdition()
    {
        var errors = CompileErrors("           MOVE FUNCTION TRIM(\"  ab  \") TO R.\n", 2014);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1502"));
    }
}
