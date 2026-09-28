// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE DRIFT GUARD ON A DETERMINATION: the Format-1 USE selection tiers of ISO/IEC 1989:2023 §14.9.49.4 —
/// GR3 a)/GR5 (file-name scope) and GR3 b)/GR6 b)–e) (open-mode scope) — are the SAME at 85, 2002, 2014 and
/// 2023, so the generated selector must be TEXTUALLY IDENTICAL at all four (kb/Work PB344).
///
/// <para><b>Why this test exists.</b> Annex E.2 item 19 a)/b) reads as if the open-mode tier were a 2023
/// change, and it was once implemented as a <c>&lt;=2014</c> guard. It is not one: Annex E is INFORMATIVE,
/// §E.1 scopes it to "a list of the substantive changes between the previous COBOL standard and this Working
/// Draft International Standard" — ONE prior edition, so it says nothing about 1985 or 2002 — and item 19's
/// own justification classes the prior state as defective TEXT, "The previous COBOL Standard was not clear or
/// missing processing of some I-O exceptions", each sub-item adding "This appears to be an error in previous
/// standards". A silence is not a prohibition, and for 1985 the behaviour is fixed the OTHER way by that
/// edition's own validation suite: the guard turned five NIST CCVS programs RED on CI (SQ122A, SQ136A, SQ137A,
/// SQ138A, SQ148A — SQ137A's own remark is "INPUT DECLARATIVE NOT EXECUTED").</para>
///
/// <para><b>Why it is not a golden.</b> The tier pair is rendered by THREE selectors —
/// <c>DispatchEmitter.__IoCheck</c>, <c>EcEmitter.__IoCheckEc</c> and the §14.9.49.4 GR4 b) outward GLOBAL
/// walk <c>ProgramEmitter.__RunGlobalUse</c> — and only the first two are reachable from an observable output
/// difference in a single program. Comparing the EMITTED TEXT covers all three at once, and it fails on the
/// re-introduction of ANY per-edition condition, in any of them, whether or not a golden happens to exercise
/// that arm. The complement is asserted too: a run in which a selector was never emitted FAILS, so the test
/// can never pass by comparing nothing (feedback_measure_the_selectors_complement).</para>
/// </summary>
public sealed class UseTierEditionInvarianceDriftTests
{
    /// <summary>An outer program with a GLOBAL file-name-scoped declarative AND a GLOBAL open-mode one (so the
    /// GR4 b) outward walk renders both tiers), containing a program with its own open-mode declaratives over
    /// an INDEXED file (so the local selector renders both tiers over all four open modes and an invalid key
    /// condition is reachable). Every construct is COBOL-85, so the same text compiles at all four editions.
    /// <c>{0}</c> is the directive prefix — empty, or the >>TURN that forces the EC-model selector.</summary>
    private const string Source = """
        {0}IDENTIFICATION DIVISION.
        PROGRAM-ID. USETIER1.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT GF ASSIGN TO "usetier1-gf.dat"
            FILE STATUS IS WS-FS.
        DATA DIVISION.
        FILE SECTION.
        FD GF GLOBAL.
        01 GF-REC PIC X(10).
        WORKING-STORAGE SECTION.
        01 WS-FS PIC XX VALUE "00".
        PROCEDURE DIVISION.
        DECLARATIVES.
        G-FILE SECTION. USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON GF.
        G-FILE-P.
            DISPLAY "G-FILE " WS-FS.
        G-IN SECTION. USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON INPUT.
        G-IN-P.
            DISPLAY "G-IN " WS-FS.
        G-OUT SECTION. USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
        G-OUT-P.
            DISPLAY "G-OUT " WS-FS.
        END DECLARATIVES.
        MAIN-SECT SECTION.
        MAIN-P.
            CALL "USETIER2".
            STOP RUN.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. USETIER2.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT KF ASSIGN TO "usetier2-kf.dat"
            ORGANIZATION IS INDEXED
            ACCESS MODE IS RANDOM
            RECORD KEY IS KF-K
            FILE STATUS IS WS-KS.
        DATA DIVISION.
        FILE SECTION.
        FD KF.
        01 KF-REC.
           05 KF-K PIC X(4).
           05 KF-V PIC X(6).
        WORKING-STORAGE SECTION.
        01 WS-KS PIC XX VALUE "00".
        PROCEDURE DIVISION.
        DECLARATIVES.
        L-IN SECTION. USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
        L-IN-P.
            DISPLAY "L-IN " WS-KS.
        L-IO SECTION. USE AFTER STANDARD ERROR PROCEDURE ON I-O.
        L-IO-P.
            DISPLAY "L-IO " WS-KS.
        L-EXT SECTION. USE AFTER STANDARD ERROR PROCEDURE ON EXTEND.
        L-EXT-P.
            DISPLAY "L-EXT " WS-KS.
        L-FILE SECTION. USE AFTER STANDARD ERROR PROCEDURE ON KF.
        L-FILE-P.
            DISPLAY "L-FILE " WS-KS.
        END DECLARATIVES.
        SUB-SECT SECTION.
        SUB-P.
            OPEN INPUT GF.
            OPEN I-O KF.
            MOVE "K001" TO KF-K.
            READ KF.
            WRITE KF-REC.
            CLOSE KF.
            EXIT PROGRAM.
        END PROGRAM USETIER2.
        END PROGRAM USETIER1.
        """;

    /// <summary>⛔ The tiers at 85 / 2002 / 2014 / 2023 are the same TEXT. Any per-edition condition
    /// re-introduced into any of the three selectors fails here.</summary>
    [Fact]
    public void PlainSelectors_AreByteIdenticalAtEveryEdition()
        => AssertSelectorsIdentical("", ["__IoCheck(", "__RunGlobalUse("], 85, 2002, 2014, 2023);

    /// <summary>The same, for the EC-model selector. <c>&gt;&gt;TURN</c> is a COBOL-2002 directive, so this arm
    /// is compared over the three editions that have it — the tiers it renders are the same rule.</summary>
    [Fact]
    public void EcModelSelector_IsByteIdenticalAtEveryEditionThatHasTurn()
        => AssertSelectorsIdentical(">>TURN EC-I-O CHECKING ON\n", ["__IoCheckEc(", "__RunGlobalUse("],
            2002, 2014, 2023);

    private static void AssertSelectorsIdentical(string directives, string[] selectors, params int[] editions)
    {
        var byEdition = new Dictionary<int, Dictionary<string, string>>();
        foreach (int edition in editions)
        {
            string generated = Emit(directives, edition);
            var found = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string sel in selectors)
            {
                string? body = MethodText(generated, sel);
                Assert.True(body is not null,
                    $"--std {edition}: the emitted source contains no {sel} — this test would compare nothing");
                found[sel] = body!;
            }
            byEdition[edition] = found;
        }
        int baseline = editions[0];
        foreach (int edition in editions[1..])
            foreach (string sel in selectors)
                Assert.True(byEdition[baseline][sel] == byEdition[edition][sel],
                    $"{sel} differs between --std {baseline} and --std {edition}: the §14.9.49.4 GR3/GR5/GR6 "
                    + $"tiers are EDITION-INVARIANT (kb/Work PB344).\n--- {baseline} ---\n"
                    + byEdition[baseline][sel] + $"\n--- {edition} ---\n" + byEdition[edition][sel]);
    }

    private static string Emit(string directives, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_UseTier_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "usetier.cob");
            src = CompiledProgramCache.StageSource(src, Source.Replace("{0}", directives));
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "usetier.dll"), DialectLevel: edition, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, $"--std {edition}: " + string.Join("\n", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>The text of the first method whose declaration contains <paramref name="signature"/>, from the
    /// declaration's line start through the brace that closes its body — null when it is not emitted.</summary>
    private static string? MethodText(string generated, string signature)
    {
        int at = generated.IndexOf(signature, StringComparison.Ordinal);
        if (at < 0) return null;
        int start = generated.LastIndexOf('\n', at) + 1;
        int open = generated.IndexOf('{', at);
        if (open < 0) return null;
        int depth = 0;
        for (int i = open; i < generated.Length; i++)
        {
            if (generated[i] == '{') depth++;
            else if (generated[i] == '}' && --depth == 0)
                return generated[start..(i + 1)].Replace("\r\n", "\n");
        }
        return null;
    }
}
