// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The inter-program file/storage joint edges (the IC-residue wave): the BY REFERENCE full-allocation rule for
/// occurs-depending groups (ISO/IEC 1989:2023 §14.2.3 GR8 — IC207A), the EXTERNAL FD run-unit-shared connector
/// and record area (§13.18.22.4 GR4a/GR4b — IC227A), GLOBAL FD visibility in contained programs (§13.18.30 —
/// IC233A/IC234A), the cross-program GLOBAL USE dispatch (§14.9.49.4 GR4 — IC233A), and the cross-assembly
/// run-unit composition probe (§14.6.1 / §14.9.4.4 GR3b — the implementor-defined locate step). Differential
/// against the NIST-IC-green legacy oracle where it covers the construct; the cross-assembly probe is
/// greenfield-only (the legacy's DiscoverProgram is its own mechanism) and asserted directly.
/// </summary>
public sealed class InterProgramFileDifferentialTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    /// <summary>Spec-pinned (no oracle): asserted against the ISO-derived expected output directly — used where
    /// the legacy has a verified hole (every use documents the hole + the deciding §).</summary>
    private static void AssertSpecPinned(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(expected, cout);
    }

    // ── Fix C: BY REFERENCE ODO group — the FULL allocation crosses the boundary (§14.2.3 GR8) ────────────

    /// <summary>§14.2.3 GR8: BY REFERENCE — the formal "occupies the same storage area as the argument". The
    /// STORAGE is the maximum allocation: an occurs-depending group's current-extent window (§13.18.38 GR8) is a
    /// sending-OPERAND rule for MOVE/compare, never a storage-aliasing rule. The callee (fixed OCCURS 5 formal)
    /// must see positions past the caller's current extent (EEE), and its store into position 4 must reach the
    /// caller's storage (IC207A's CONTENTS-OF-TABLE check).</summary>
    [Fact]
    public void CallByReference_OdoGroup_FullAllocationCrossesBoundary()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ODOREF1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CNT PIC 9 VALUE 5.
            01 TBL.
               02 TBL-EL PIC XXX OCCURS 1 TO 5 TIMES DEPENDING ON WS-CNT.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE "AAABBBCCCDDDEEE" TO TBL.
                MOVE 3 TO WS-CNT.
                CALL "ODOREF1S" USING TBL.
                MOVE 5 TO WS-CNT.
                DISPLAY "EL4=" TBL-EL (4) "]".
                DISPLAY "EL5=" TBL-EL (5) "]".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ODOREF1S.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-TBL.
               02 LK-EL PIC XXX OCCURS 5.
            PROCEDURE DIVISION USING LK-TBL.
            SUB-P.
                DISPLAY "SEES5=" LK-EL (5) "]".
                MOVE "ZZZ" TO LK-EL (4).
                EXIT PROGRAM.
            """);

    // ── Fix D: EXTERNAL FD — ONE run-unit connector + ONE record area (§13.18.22.4 GR4a/GR4b) ─────────────

    /// <summary>§13.18.22.4 GR4a (one EXTERNAL file connector per run unit) + GR4b (the record data is external,
    /// shared by every describer): the IC227A shape — the main OPENs OUTPUT and fills the record area; the
    /// separately-described subprogram WRITEs without opening (the open mode lives on the SHARED connector) and
    /// its differently-named record holds the MAIN's data (the SHARED record area, externalized by the FD name
    /// per GR5). The read-back proves both halves at once. SPEC-PINNED: the legacy oracle DROPS the shared
    /// record area in this minimal two-unit shape (it reads back spaces — a hole; its IC227A pass goes through
    /// CCVS's fuller protocol), and GR4b's one-record-area rule decides ("the record data is external").</summary>
    [Fact]
    public void ExternalFd_SharedConnectorAndRecordArea_AcrossPrograms()
        => AssertSpecPinned("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXTFD1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT EF ASSIGN TO "EXTFD1-T1".
            DATA DIVISION.
            FILE SECTION.
            FD EF IS EXTERNAL.
            01 EF-REC PIC X(12).
            PROCEDURE DIVISION.
            MAIN-P.
                OPEN OUTPUT EF.
                MOVE "MAIN-FILLED" TO EF-REC.
                CALL "EXTFD1S".
                CLOSE EF.
                OPEN INPUT EF.
                READ EF AT END DISPLAY "EOF".
                DISPLAY "GOT=" EF-REC "]".
                CLOSE EF.
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXTFD1S.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT EF ASSIGN TO "EXTFD1-T1".
            DATA DIVISION.
            FILE SECTION.
            FD EF IS EXTERNAL.
            01 EF-RECS PIC X(12).
            PROCEDURE DIVISION.
            SUB-P.
                WRITE EF-RECS.
                EXIT PROGRAM.
            """, "GOT=MAIN-FILLED ]");

    // ── Fix E: GLOBAL FD + cross-program GLOBAL USE (§13.18.30; §14.9.49.4 GR4) ────────────────────────────

    /// <summary>§13.18.30: the file-name (and record-names) of a GLOBAL FD are GLOBAL names — the contained
    /// program (no file section of its own) OPENs, WRITEs the owner's record, and CLOSEs the owner's ONE
    /// connector; the container reads the data back (the IC233A file-visibility half).</summary>
    [Fact]
    public void GlobalFd_ContainedProgramReachesOwnersConnectorAndRecord()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GLFD1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT GF ASSIGN TO "GLFD1-T1".
            DATA DIVISION.
            FILE SECTION.
            FD GF GLOBAL.
            01 GF-REC PIC X(11).
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "GLFD1W".
                OPEN INPUT GF.
                READ GF AT END DISPLAY "EOF".
                DISPLAY "GOT=" GF-REC "]".
                CLOSE GF.
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GLFD1W.
            PROCEDURE DIVISION.
            SUB-P.
                OPEN OUTPUT GF.
                MOVE "FROM-NESTED" TO GF-REC.
                WRITE GF-REC.
                CLOSE GF.
                EXIT PROGRAM.
            END PROGRAM GLFD1W.
            END PROGRAM GLFD1.
            """);

    /// <summary>§14.9.49.4 GR4b: with NO qualifying declarative in the contained program, "a qualifying
    /// declarative with the GLOBAL attribute in the next inclusive directly containing source element" runs —
    /// in the DECLARING program's instance (§8.4.6.2, its data). The contained OPEN INPUT of a missing
    /// non-OPTIONAL file (status 35, "in the process of being opened" — GR6b) fires the outer's USE GLOBAL …
    /// ON INPUT (the IC233A dispatch half). SPEC-PINNED on the FS value: §12.4.5.8.4 GR1 NOTE 1 — "data-name-1
    /// is updated by references to file-name in contained programs even though data-name-1 is a local name" —
    /// so the handler sees 35; the legacy leaves the owner's local item at 00 (a verified hole: it dispatches
    /// the GLOBAL declarative but skips the NOTE-1 status routing).</summary>
    [Fact]
    public void GlobalUse_ContainedFailureFiresOuterGlobalDeclarative()
        => AssertSpecPinned("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GLUSE1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT GF ASSIGN TO "GLUSE1-NOFILE"
                FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD GF GLOBAL.
            01 GF-REC PIC X(10).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX VALUE "00".
            PROCEDURE DIVISION.
            DECLARATIVES.
            ERR-SECT SECTION. USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON INPUT.
            ERR-P.
                DISPLAY "GLOBAL-HANDLER FS=" WS-FS.
            END DECLARATIVES.
            MAIN-SECT SECTION.
            MAIN-P.
                CALL "GLUSE1R".
                DISPLAY "BACK".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GLUSE1R.
            PROCEDURE DIVISION.
            SUB-P.
                OPEN INPUT GF.
                DISPLAY "SUB-CONT".
                EXIT PROGRAM.
            END PROGRAM GLUSE1R.
            END PROGRAM GLUSE1.
            """, "GLOBAL-HANDLER FS=35\nSUB-CONT\nBACK");

    /// <summary>§14.9.49.4 GR4a beats GR4b: the contained program's OWN declarative naming the inherited GLOBAL
    /// file (legal — §13.18.30 makes the file-name visible; the IC234A bind shape that previously raised
    /// COBOLNET0897) is selected over the outer's GLOBAL one.</summary>
    [Fact]
    public void GlobalFd_ContainedOwnUseOnInheritedFile_BeatsOuterGlobal()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GLUSE2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT GF ASSIGN TO "GLUSE2-NOFILE"
                FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD GF GLOBAL.
            01 GF-REC PIC X(10).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX VALUE "00".
            PROCEDURE DIVISION.
            DECLARATIVES.
            OUT-SECT SECTION. USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON INPUT.
            OUT-P.
                DISPLAY "OUTER-HANDLER".
            END DECLARATIVES.
            MAIN-SECT SECTION.
            MAIN-P.
                CALL "GLUSE2R".
                DISPLAY "BACK".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GLUSE2R.
            PROCEDURE DIVISION.
            DECLARATIVES.
            IN-SECT SECTION. USE AFTER STANDARD ERROR PROCEDURE ON GF.
            IN-P.
                DISPLAY "INNER-HANDLER".
            END DECLARATIVES.
            SUB-SECT SECTION.
            SUB-P.
                OPEN INPUT GF.
                DISPLAY "SUB-CONT".
                EXIT PROGRAM.
            END PROGRAM GLUSE2R.
            END PROGRAM GLUSE2.
            """);

    // ── Fix G: cross-assembly run-unit composition (§14.6.1; §14.9.4.4 GR3b — greenfield-only) ────────────

    private static void CompileTo(string source, string dir, string name, int dialect = 85)
    {
        string src = Path.Combine(dir, name + ".cob");
        src = CompiledProgramCache.StageSource(src, source);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: dialect, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r.Success, $"compile {name}: {string.Join("; ", r.Errors)}");
    }

    private const string XasmCaller = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. XASMC1.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-CTR PIC 9(4) VALUE 0010.
        PROCEDURE DIVISION.
        MAIN-P.
            DISPLAY "CALLER-START".
            CALL "XASMS1" USING WS-CTR.
            DISPLAY "AFTER=" WS-CTR.
            STOP RUN.
        """;

    private const string XasmCallee = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. XASMS1.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-CTR PIC 9(4).
        PROCEDURE DIVISION USING LK-CTR.
        SUB-P.
            DISPLAY "SUB-SEES=" LK-CTR.
            ADD 32 TO LK-CTR.
            EXIT PROGRAM.
        """;

    /// <summary>"A run unit contains one or more runtime modules. A runtime module results from compiling a
    /// compilation unit" (§14.6.1); §14.9.4.4 GR3b — the runtime "attempts to locate" the called program, the
    /// mechanics implementor-defined: the registry's rule-4 fallthrough probes the application directory for the
    /// sibling module <c>XASMS1.dll</c>, invokes its public <c>__CobolModule.Register()</c>, and the CALL
    /// proceeds with full BY REFERENCE semantics across the assembly boundary.</summary>
    [Fact]
    public void Call_SeparatelyCompiledSiblingModule_ResolvesAndAliases()
    {
        string dir = CutRunner.NewTempDir("xasm");
        try
        {
            CompileTo(XasmCaller, dir, "XASMC1");
            CompileTo(XasmCallee, dir, "XASMS1");
            var (ok, stdout, detail) = CutRunner.Run(Path.Combine(dir, "XASMC1.dll"), dir);
            Assert.True(ok, detail);
            Assert.Equal("CALLER-START\nSUB-SEES=0010\nAFTER=0042", stdout);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>Without the sibling module the CALL still raises the EC-PROGRAM-NOT-FOUND surface (§14.9.4.4
    /// GR3b) — the probe is a quiet miss, never a behavior change for genuinely absent programs.</summary>
    [Fact]
    public void Call_AbsentSiblingModule_StillNotFound()
    {
        string dir = CutRunner.NewTempDir("xasmn");
        try
        {
            CompileTo(XasmCaller, dir, "XASMC1");
            var (ok, _, detail) = CutRunner.Run(Path.Combine(dir, "XASMC1.dll"), dir);
            Assert.False(ok, "a CALL to an absent program must terminate the run unit loudly (no exception phrase)");
            Assert.Contains("EC-PROGRAM-NOT-FOUND", detail);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    // ── UDF-3: separately-compiled function prototypes (ISO §11.5 Format 2 / §8.13 / §12.3.8; greenfield-only) ──

    private const string UdfProtoCaller = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. SQUARER IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X PIC 9(4).
        01 L-R PIC 9(6).
        PROCEDURE DIVISION USING L-X RETURNING L-R.
        END FUNCTION SQUARER.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. UPROTOX.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            FUNCTION SQUARER.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-N PIC 9(4) VALUE 0007.
        01 WS-R PIC 9(6).
        PROCEDURE DIVISION.
        MAIN.
            COMPUTE WS-R = FUNCTION SQUARER(WS-N).
            DISPLAY "P=" WS-R.
            STOP RUN.
        END PROGRAM UPROTOX.
        """;

    private const string UdfProtoDefinition = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. SQUARER.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X PIC 9(4).
        01 L-R PIC 9(6).
        PROCEDURE DIVISION USING L-X RETURNING L-R.
        COMPUTE-IT.
            COMPUTE L-R = L-X * L-X.
            GOBACK.
        END FUNCTION SQUARER.
        """;

    /// <summary>ISO §12.3.8 GR11(c) / §8.4.3.2.4 GR6b — the caller declares only a FUNCTION-ID … IS PROTOTYPE for
    /// SQUARER (its signature) and invokes FUNCTION SQUARER(x); the separately-compiled DEFINITION lives in a
    /// sibling assembly SQUARER.dll. The prototype registers NO runtime module, so the UDF-lowered call falls
    /// through to the registry's sibling probe (the same §14.9.4.4 GR3b path as cross-assembly CALL), loads
    /// SQUARER.dll, and the RETURNING value crosses the assembly boundary. 7*7 = 49 ⇒ P=000049.</summary>
    [Fact]
    public void Udf_SeparatelyCompiledPrototypeTarget_ResolvesCrossAssembly()
    {
        string dir = CutRunner.NewTempDir("udfproto");
        try
        {
            CompileTo(UdfProtoCaller, dir, "UPROTOX", 2002);
            CompileTo(UdfProtoDefinition, dir, "SQUARER", 2002);
            var (ok, stdout, detail) = CutRunner.Run(Path.Combine(dir, "UPROTOX.dll"), dir);
            Assert.True(ok, detail);
            Assert.Equal("P=000049", stdout);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>ISO §8.4.3.2.4 GR6b + Table 13 — with the SQUARER definition absent, the UDF locate miss raises
    /// EC-FUNCTION-NOT-FOUND (Fatal), NOT the CALL's EC-PROGRAM-NOT-FOUND: the run unit terminates loudly with the
    /// function-specific condition name (the prototype supplied a signature but no activation target exists).</summary>
    [Fact]
    public void Udf_AbsentFunctionDefinition_RaisesFunctionNotFound()
    {
        string dir = CutRunner.NewTempDir("udfproton");
        try
        {
            CompileTo(UdfProtoCaller, dir, "UPROTOX", 2002);
            var (ok, _, detail) = CutRunner.Run(Path.Combine(dir, "UPROTOX.dll"), dir);
            Assert.False(ok, "a FUNCTION reference to an absent separately-compiled definition must terminate loudly");
            Assert.Contains("EC-FUNCTION-NOT-FOUND", detail);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
