// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Phase-3 OO SPINE facts (docs/COBOLNET_OO_DESIGN.md — spine part 2: ClassUnit collection + the pass-1
/// class symbol table, the emit-into-a-type parameterization, method PC-dispatch + BoundMethodReturn (D8),
/// INVOKE NEW / no-arg instance binding, typed object references): the deep-dive's ADVERSARIAL REGRESSION
/// TRAPS reproduced as day-one tests (each was a REAL caught legacy bug — traps #1/#4/#10 run here or in the
/// enabled 2002 corpus), the D8 control-flow contract (method GOBACK vs STOP RUN vs EXIT METHOD), and the OO
/// diagnostic band (COBOLNET0813/0820–0827). Two-object independence (trap #1) runs as the ENABLED corpus
/// programs oo_instance_data / oo_object_group (CorpusRunnerTests byte-compares them).
/// </summary>
public sealed class OoSpineTests
{
    /// <summary>A driver + one class with the given OBJECT-paragraph procedure division body.</summary>
    // Raw-string source is CRLF on a Windows checkout (autocrlf) but LF on Linux — normalize to LF so the
    // `.Replace("… \n …")` object-data injections used below match on BOTH CI platforms (the Windows job failed a
    // load-bearing object-WS injection when this was CRLF; DEVLOG 641). The compiler reads LF source fine.
    private static string DriverAndClass(string pid, string cls, string driverBody, string classBody) => ($$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{pid}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS {{cls}}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 T USAGE OBJECT REFERENCE {{cls}}.
        PROCEDURE DIVISION.
        MAIN.
        {{driverBody}}
            STOP RUN.
        END PROGRAM {{pid}}.

        IDENTIFICATION DIVISION.
        CLASS-ID. {{cls}} INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        {{classBody}}
        END OBJECT.
        END CLASS {{cls}}.
        """).Replace("\r\n", "\n");

    private static (bool Ok, string Stdout, string Detail) CompileAndRun(string source, int edition = 2002)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Oo_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, source);
            string dll = Path.Combine(dir, "prog.dll");
            var r = CompiledProgramCache.Compile(new CobolNet.CompilerDriver.Options(
                src, dll, DialectLevel: edition, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "must compile strict: " + string.Join("\n", r.Errors));
            return CutRunner.Run(dll, dir);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    // ── The adversarial traps (deep-dive "Adversarial regression traps" — each a REAL legacy bug) ──────────

    /// <summary>Trap #4 — method fall-through: falling off a method's LAST paragraph is the implicit method
    /// return (the exit-bounded <c>__Dispatch(entry, last)</c> range), NEVER a run into the next method's
    /// paragraphs (the legacy <c>Invoke_MultiMethod_FirstMethodDoesNotFallIntoSecond</c> guard).</summary>
    [Fact]
    public void Trap4_MethodFallThrough_DoesNotEnterSiblingMethod()
    {
        var (ok, stdout, detail) = CompileAndRun(DriverAndClass("OOSP1", "OSPC1", """
                INVOKE OSPC1 "NEW" RETURNING T.
                INVOKE T "M1".
                DISPLAY "AFTER-M1".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                DISPLAY "M1-A".
            PARA-B.
                DISPLAY "M1-B".
            END METHOD M1.
            METHOD-ID. M2.
            PROCEDURE DIVISION.
            PARA-C.
                DISPLAY "M2-MUST-NOT-PRINT".
            END METHOD M2.
            """));
        Assert.True(ok, detail);
        Assert.Equal("M1-A\nM1-B\nAFTER-M1", CutRunner.Normalize(stdout));
    }

    /// <summary>Trap #10 — cross-method PERFORM: a method definition is its own SOURCE ELEMENT (§3.164 with
    /// §3.165 and §11.7.1), so §8.4.6.1 confines its paragraph-names to it and a PERFORM
    /// of a sibling method's paragraph is REFUSED, never a silent cross-method transfer.
    /// <para>⛔ THE STAGE MOVED, AND THE INTENT DID NOT (kb/Work PB390). This test used to require the RUN to
    /// die — it asserted the <c>NotImplementedCobolFeatureException</c> text "unknown procedure 'PARA-C'" — so
    /// a GREEN test was pinning the broken stage: the program COMPILED, shipped an assembly, and blamed a gap
    /// in WiseOwl COBOL for what is an error in the source (ISO §4.2.2 ¶2 requires the compile-time mechanism).
    /// The refusal is now the compile-time COBOLNET1639, and the method-local explanation rides it.</para></summary>
    [Fact]
    public void Trap10_CrossMethodPerform_IsRefusedAtCompileTime()
    {
        var errors = ErrorsOf(DriverAndClass("OOSP2", "OSPC2", """
                INVOKE OSPC2 "NEW" RETURNING T.
                INVOKE T "M1".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                PERFORM PARA-C.
            END METHOD M1.
            METHOD-ID. M2.
            PROCEDURE DIVISION.
            PARA-C.
                DISPLAY "M2-PARA".
            END METHOD M2.
            """));
        string e = Assert.Single(errors, x => x.Contains("COBOLNET1639", StringComparison.Ordinal));
        Assert.Contains("PERFORM 'PARA-C' names no procedure", e, StringComparison.Ordinal);
        Assert.Contains("METHOD-LOCAL", e, StringComparison.Ordinal);
        Assert.Contains("§3.164", e, StringComparison.Ordinal);
    }

    // ── D8: the method-return / run-unit-stop / program-return split (§14.9.18.4 GR4 / §14.9.43) ───────────

    /// <summary>GOBACK in a method terminates the METHOD only (§14.9.18.4 GR4; D8): the INVOKE site continues.
    /// The GOBACK sits inside an out-of-line PERFORM, so the MethodReturn signal must unwind the NESTED
    /// bounded __Dispatch frame too (the reason D8's realization is catch-at-entry, not a plain return) —
    /// and the paragraphs after the PERFORM and after the GOBACK must both be skipped.</summary>
    [Fact]
    public void MethodGoback_ReturnsFromMethodOnly_UnwindingNestedPerformFrames()
    {
        var (ok, stdout, detail) = CompileAndRun(DriverAndClass("OOSP3", "OSPC3", """
                INVOKE OSPC3 "NEW" RETURNING T.
                INVOKE T "M1".
                DISPLAY "DRIVER-CONTINUES".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                PERFORM DEEP.
                DISPLAY "MUST-NOT-PRINT-1".
            PARA-B.
                DISPLAY "MUST-NOT-PRINT-2".
            DEEP.
                DISPLAY "IN-DEEP".
                GOBACK.
            END METHOD M1.
            """));
        Assert.True(ok, detail);
        Assert.Equal("IN-DEEP\nDRIVER-CONTINUES", CutRunner.Normalize(stdout));
    }

    /// <summary>STOP RUN inside a method terminates the RUN UNIT (§14.9.43) — it must NOT be caught at the
    /// method boundary as if it were a method return (the D8 anti-conflation fact).</summary>
    [Fact]
    public void StopRun_InsideMethod_TerminatesRunUnit()
    {
        var (ok, stdout, detail) = CompileAndRun(DriverAndClass("OOSP4", "OSPC4", """
                INVOKE OSPC4 "NEW" RETURNING T.
                INVOKE T "M1".
                DISPLAY "MUST-NOT-PRINT".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                DISPLAY "IN-METHOD".
                STOP RUN.
            END METHOD M1.
            """));
        Assert.True(ok, detail);
        Assert.Equal("IN-METHOD", CutRunner.Normalize(stdout));
    }

    /// <summary>EXIT METHOD is the method-return synonym in the editions that HAVE it (2002/2014; the
    /// exit-method-window registry row already 0902s it at 2023 — Annex E.2 removal, deep-dive correction #2):
    /// statements after it in the method are skipped; the INVOKE site continues.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    public void ExitMethod_Pre2023_IsMethodReturn(int edition)
    {
        var (ok, stdout, detail) = CompileAndRun(DriverAndClass("OOSP5", "OSPC5", """
                INVOKE OSPC5 "NEW" RETURNING T.
                INVOKE T "M1".
                DISPLAY "AFTER".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                DISPLAY "BEFORE-EXIT".
                EXIT METHOD.
            PARA-B.
                DISPLAY "MUST-NOT-PRINT".
            END METHOD M1.
            """), edition);
        Assert.True(ok, detail);
        Assert.Equal("BEFORE-EXIT\nAFTER", CutRunner.Normalize(stdout));
    }

    // ── The OO diagnostic band (bind-time; the four-compilers rule's complete-behavior half) ────────────────

    private static IReadOnlyList<string> ErrorsOf(string source, int edition = 2002)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok, "must be rejected");
        return errors;
    }

    /// <summary>§13.18.60.4 — the declared class of a typed object reference must be a class of the group.</summary>
    [Fact]
    public void TypedObjectReference_UnknownClass_0813()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP6.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE OBJECT REFERENCE NOSUCH.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOSP6.
            """), "COBOLNET0813");

    /// <summary>§10.7 — END CLASS names its class.</summary>
    [Fact]
    public void EndClassNameMismatch_0820()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC7.
            END CLASS OTHER7.
            """), "COBOLNET0820");

    /// <summary>D9 (v1) — method names unique per class; §12063 overloading is optional and deferred.</summary>
    [Fact]
    public void DuplicateMethodName_0822()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC8.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            END METHOD M1.
            METHOD-ID. M1.
            END METHOD M1.
            END OBJECT.
            END CLASS OSPC8.
            """), "COBOLNET0822");

    /// <summary>Trap #8 — INHERITS of an unknown base is LOUD (0821), never silently a root class; a KNOWN
    /// base COMPILES (slice 3a — `: BASE` emission; the former 0899 staging is retired).</summary>
    [Fact]
    public void InheritsUnknownBase_0821_KnownBaseCompiles()
    {
        var unknown = ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC9 INHERITS FROM NOBASE.
            END CLASS OSPC9.
            """);
        EditionHarness.AssertHasDiagnostic(unknown, "COBOLNET0821");
        var (okKnown, knownErrors, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPB10.
            END CLASS OSPB10.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC10 INHERITS FROM OSPB10.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS OSPB10.
            END CLASS OSPC10.
            """, 2002);
        Assert.True(okKnown, "INHERITS FROM a known base must compile (slice 3a): "
            + string.Join("\n", knownErrors));
    }

    /// <summary>§14.9.23.3 SR4 b) — for a TYPED receiver an unknown method is a COMPILE-time diagnostic (the
    /// static analog of EC-OO-METHOD, GR7b).</summary>
    [Fact]
    public void InvokeUnknownMethod_0825()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP11", "OSPC11", """
                INVOKE OSPC11 "NEW" RETURNING T.
                INVOKE T "NOSUCHM".
            """, """
            METHOD-ID. M1.
            END METHOD M1.
            """)), "COBOLNET0825");

    /// <summary>§16.2.1 — NEW's only result is the reference: RETURNING is required and USING is meaningless;
    /// the receiver must conform (§14.8 — a typed receiver of an UNRELATED class rejects).</summary>
    [Fact]
    public void InvokeNew_ShapeAndConformance_0826()
    {
        EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP12", "OSPC12", """
                INVOKE OSPC12 "NEW".
            """, "")), "COBOLNET0826");
        EditionHarness.AssertHasDiagnostic(ErrorsOf($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP13.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS OSPC13
                CLASS OSPD13.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W USAGE OBJECT REFERENCE OSPD13.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE OSPC13 "NEW" RETURNING W.
                STOP RUN.
            END PROGRAM OOSP13.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC13 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS OSPC13.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPD13.
            END CLASS OSPD13.
            """), "COBOLNET0826");
    }

    /// <summary>§14.9.14.3 SR7 — EXIT PROGRAM only in a program procedure division; the method form only in a
    /// method (both COBOLNET0827).</summary>
    [Fact]
    public void ExitPlacement_0827()
    {
        EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP14", "OSPC14", """
                INVOKE OSPC14 "NEW" RETURNING T.
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                EXIT PROGRAM.
            END METHOD M1.
            """)), "COBOLNET0827");
        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP15.
            PROCEDURE DIVISION.
            MAIN.
                EXIT METHOD.
                STOP RUN.
            END PROGRAM OOSP15.
            """), "COBOLNET0827");
    }

    /// <summary>M2-OO-1i — a method definition shall NOT own an ENVIRONMENT DIVISION, FILE SECTION, REPORT
    /// SECTION, or SCREEN SECTION: each may appear only in a factory or instance definition (ISO §12.4.3 SR1 /
    /// §13.4.3 SR1 / §13.9 / §13.10). A method's own data division is limited to LOCAL-STORAGE (§13.6.3) +
    /// LINKAGE (§13.7.3). Each violation is a HARD COBOLNET1519 (superseding the old "recognized but not yet
    /// implemented" 0899 — the construct is spec-forbidden, not merely unimplemented). Object/factory FILE-CONTROL
    /// + FILE SECTION ARE legal (the M2-OO-1i object/factory leg); a method references those files via
    /// §11.7.4 GR5, it never declares its own.</summary>
    [Theory]
    [InlineData("""
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 F-REC PIC X(4).
        """)]
    [InlineData("""
        DATA DIVISION.
        REPORT SECTION.
        """)]
    [InlineData("""
        DATA DIVISION.
        SCREEN SECTION.
        """)]
    public void MethodMayNotOwnEnvOrFileSection_1519(string methodSection)
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSPME", "OSPCME", """
                INVOKE OSPCME "NEW" RETURNING T.
            """, $$"""
            METHOD-ID. M.
            {{methodSection}}
            PROCEDURE DIVISION.
            PARA-A.
                CONTINUE.
            END METHOD M.
            """)), "COBOLNET1519");

    /// <summary>M2-OO-1i — the GLOBAL clause shall not be specified in a factory / instance / method definition
    /// (ISO §13.18.27.3 SR4). A FACTORY <c>FD … IS GLOBAL</c> is COBOLNET1520 — GLOBAL is a nested-PROGRAM
    /// containment mechanism that does not cross the class boundary; program↔class file sharing is EXTERNAL only
    /// (§9.1.5). (The FACTORY file itself is legal — inc 3 — only the GLOBAL clause on it is rejected.)</summary>
    [Fact]
    public void FactoryFile_Global_1520()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOGF1.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS GFCLS.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOGF1.

            IDENTIFICATION DIVISION.
            CLASS-ID. GFCLS.
            IDENTIFICATION DIVISION.
            FACTORY.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT GF ASSIGN TO "g.dat".
            DATA DIVISION.
            FILE SECTION.
            FD GF IS GLOBAL.
            01 GF-REC PIC X(4).
            PROCEDURE DIVISION.
            END FACTORY.
            IDENTIFICATION DIVISION.
            OBJECT.
            END OBJECT.
            END CLASS GFCLS.
            """).Replace("\r\n", "\n")), "COBOLNET1520");

    /// <summary>M2-OO-1i — the GLOBAL clause is barred in an INSTANCE (object) definition too (ISO §13.18.27.3
    /// SR4). An OBJECT <c>FD … IS GLOBAL</c> is COBOLNET1520. (The OBJECT file itself is legal — inc 4, a
    /// per-object connector — only the GLOBAL clause on it is rejected.)</summary>
    [Fact]
    public void ObjectFile_Global_1520()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOGO1.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS GOCLS.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOGO1.

            IDENTIFICATION DIVISION.
            CLASS-ID. GOCLS.
            IDENTIFICATION DIVISION.
            OBJECT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT GOF ASSIGN TO "g.dat".
            DATA DIVISION.
            FILE SECTION.
            FD GOF IS GLOBAL.
            01 GOF-REC PIC X(4).
            PROCEDURE DIVISION.
            END OBJECT.
            END CLASS GOCLS.
            """).Replace("\r\n", "\n")), "COBOLNET1520");

    /// <summary>kb/Work PB369 — §13.18.27.3 SR4 bars GLOBAL on a REPORT description entry too (SR1 e) is the
    /// third entry kind it names). The RD GLOBAL staging used to answer this shape with "not yet implemented";
    /// once the clause was implemented for programs, the class arm needed its own SR4 refusal.</summary>
    [Fact]
    public void ObjectReport_Global_1520()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOGR1.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS GRCLS.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOGR1.

            IDENTIFICATION DIVISION.
            CLASS-ID. GRCLS.
            IDENTIFICATION DIVISION.
            OBJECT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT GRF ASSIGN TO "g.rpt".
            DATA DIVISION.
            FILE SECTION.
            FD GRF REPORT IS GR-1.
            REPORT SECTION.
            RD GR-1 IS GLOBAL.
            01 GR-DET TYPE DE LINE PLUS 1.
               02 COLUMN 1 PIC XX VALUE "DE".
            PROCEDURE DIVISION.
            END OBJECT.
            END CLASS GRCLS.
            """).Replace("\r\n", "\n")), "COBOLNET1520");

    /// <summary>M2-OO-1i review — §13.18.27.3 SR4 bars GLOBAL on a DATA item too (not just an FD) in a factory /
    /// instance / method definition. A GLOBAL level-01 in an OBJECT WORKING-STORAGE is COBOLNET1520 (was silently
    /// accepted — a false-negative diagnostic).</summary>
    [Fact]
    public void ObjectData_Global_1520()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOGD1.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS GDCLS.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOGD1.

            IDENTIFICATION DIVISION.
            CLASS-ID. GDCLS.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G PIC X IS GLOBAL.
            PROCEDURE DIVISION.
            END OBJECT.
            END CLASS GDCLS.
            """).Replace("\r\n", "\n")), "COBOLNET1520");

    /// <summary>M2-OO-1i review — a GLOBAL data item in a METHOD (here LINKAGE) is COBOLNET1520, superseding the
    /// old mislabeled COBOLNET0899 "not yet implemented": §13.18.27.3 SR4 makes GLOBAL spec-FORBIDDEN in a method,
    /// not merely unimplemented.</summary>
    [Fact]
    public void MethodData_Global_1520()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOGD2", "GDCL2", """
                INVOKE GDCL2 "NEW" RETURNING T.
            """, """
            METHOD-ID. M.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-G PIC X IS GLOBAL.
            PROCEDURE DIVISION.
            MAIN.
                CONTINUE.
            END METHOD M.
            """)), "COBOLNET1520");

    /// <summary>The staged boundaries stay LOUD (never a silent drop): INVOKE SELF (slice 3b) reaches the
    /// runtime not-implemented guard; an arity mismatch (slice 2 — trap #3: a dropped/extra argument would
    /// shift every following slot, the legacy DEVLOG-449 blocker) is a compile-time 0828.</summary>
    [Fact]
    public void Trap3_InvokeArityMismatch_0828()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP16", "OSPC16", """
                INVOKE OSPC16 "NEW" RETURNING T.
                INVOKE T "M1" USING T.
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            PARA-A.
                DISPLAY "X".
            END METHOD M1.
            """)), "COBOLNET0828");

    // ── Slice 2 (deep-dive D3/D6 — method LINKAGE/LOCAL-STORAGE/WS + INVOKE USING/RETURNING) ───────────────

    /// <summary>Trap #6 — sibling-method LINKAGE cross-wiring: two methods each declare <c>LK-V</c> with
    /// DIFFERENT descriptions; each resolves its OWN (per-method data scopes, §11.7 GR5) — the legacy
    /// static-<c>_linkage_</c> field bug is structurally impossible.</summary>
    [Fact]
    public void Trap6_SiblingMethodLinkage_NoCrossWiring()
    {
        var (ok, stdout, detail) = CompileAndRun($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP17.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS OSPC17.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE OBJECT REFERENCE OSPC17.
            01 N1 PIC 9(4) VALUE 7.
            01 S1 PIC X(4) VALUE "ABCD".
            PROCEDURE DIVISION.
            MAIN.
                INVOKE OSPC17 "NEW" RETURNING T.
                INVOKE T "M1" USING N1.
                INVOKE T "M2" USING S1.
                STOP RUN.
            END PROGRAM OOSP17.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC17 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-V PIC 9(4).
            PROCEDURE DIVISION USING LK-V.
            MAIN.
                DISPLAY "M1-NUM=" LK-V.
            END METHOD M1.
            METHOD-ID. M2.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-V PIC X(4).
            PROCEDURE DIVISION USING LK-V.
            MAIN.
                DISPLAY "M2-STR=" LK-V.
            END METHOD M2.
            END OBJECT.
            END CLASS OSPC17.
            """);
        Assert.True(ok, detail);
        Assert.Equal("M1-NUM=0007\nM2-STR=ABCD", CutRunner.Normalize(stdout));
    }

    /// <summary>Methods are implicitly RECURSIVE (spec :12032): recursion through a typed object-reference
    /// FORMAL, with LOCAL-STORAGE re-initialized per activation (§14.5.3) — each frame keeps its own LK-N
    /// (the local-function dispatcher captures per-activation locals), and the driver's BY REFERENCE argument
    /// is untouched (the method never writes LK-N).</summary>
    [Fact]
    public void MethodRecursion_ViaObjectRefFormal_ReentrantLocals()
    {
        var (ok, stdout, detail) = CompileAndRun($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP18.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS OSPC18.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE OBJECT REFERENCE OSPC18.
            01 N PIC 9(2) VALUE 3.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE OSPC18 "NEW" RETURNING T.
                INVOKE T "COUNTDOWN" USING N T.
                DISPLAY "N-AFTER=" N.
                STOP RUN.
            END PROGRAM OOSP18.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC18 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. COUNTDOWN.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 LS-NEXT PIC 9(2) VALUE 0.
            LINKAGE SECTION.
            01 LK-N PIC 9(2).
            01 LK-SELF USAGE OBJECT REFERENCE OSPC18.
            PROCEDURE DIVISION USING LK-N LK-SELF.
            MAIN.
                DISPLAY "AT-" LK-N.
                IF LK-N > 1
                    SUBTRACT 1 FROM LK-N GIVING LS-NEXT
                    INVOKE LK-SELF "COUNTDOWN" USING LS-NEXT LK-SELF
                END-IF.
                DISPLAY "UP-" LK-N.
            END METHOD COUNTDOWN.
            END OBJECT.
            END CLASS OSPC18.
            """);
        Assert.True(ok, detail);
        Assert.Equal("AT-03\nAT-02\nAT-01\nUP-01\nUP-02\nUP-03\nN-AFTER=03", CutRunner.Normalize(stdout));
    }

    /// <summary>§14.9.23.3 SR 10 both ways: OBJECT data may NOT cross an INVOKE BY REFERENCE — a BARE
    /// object-data argument is assumed BY CONTENT (GR6a2: the callee's writes are invisible to the object's
    /// state), and an EXPLICIT BY REFERENCE of object data is the compile-time 0828. A driver WS argument
    /// (SR 9) crosses BY REFERENCE and the callee's write IS visible.</summary>
    [Fact]
    public void ObjectData_AutoContent_Sr10()
    {
        const string cls = """
            IDENTIFICATION DIVISION.
            CLASS-ID. {C} INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OD PIC 9(2) VALUE 5.
            PROCEDURE DIVISION.
            METHOD-ID. RUNIT.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-SELF USAGE OBJECT REFERENCE {C}.
            PROCEDURE DIVISION USING LK-SELF.
            MAIN.
                INVOKE LK-SELF "BUMP" USING {REF}OD.
                DISPLAY "OD=" OD.
            END METHOD RUNIT.
            METHOD-ID. BUMP.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-N PIC 9(2).
            PROCEDURE DIVISION USING LK-N.
            MAIN.
                ADD 1 TO LK-N.
            END METHOD BUMP.
            END OBJECT.
            END CLASS {C}.
            """;
        const string drv = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {P}.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS {C}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE OBJECT REFERENCE {C}.
            01 WN PIC 9(2) VALUE 5.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE {C} "NEW" RETURNING T.
                INVOKE T "RUNIT" USING T.
                INVOKE T "BUMP" USING WN.
                DISPLAY "WN=" WN.
                STOP RUN.
            END PROGRAM {P}.

            """;
        var (ok, stdout, detail) = CompileAndRun(
            (drv + cls).Replace("{P}", "OOSP19").Replace("{C}", "OSPC19").Replace("{REF}", ""));
        Assert.True(ok, detail);
        // OD unchanged (auto-CONTENT — SR 10); the driver's WS item updated (BY REFERENCE — SR 9).
        Assert.Equal("OD=05\nWN=06", CutRunner.Normalize(stdout));
        EditionHarness.AssertHasDiagnostic(ErrorsOf(
            (drv + cls).Replace("{P}", "OOSP20").Replace("{C}", "OSPC20").Replace("{REF}", "BY REFERENCE ")),
            "COBOLNET0828");
    }

    /// <summary>§14.8.2 strict REFERENCE conformance: a description mismatch (PIC 9(4) formal, PIC 9(5)
    /// argument) is the compile-time 0828 — never a silent re-scale across the boundary. RETURNING pairing
    /// mismatches (either direction) are equally loud (the deep-dive signature-check edge case).</summary>
    [Fact]
    public void InvokeConformanceAndReturningPairing_0828()
    {
        string Mk(string pid, string cls, string driverArgPic, string invoke) => DriverAndClass(pid, cls, invoke, """
            METHOD-ID. M1.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-A PIC 9(4).
            PROCEDURE DIVISION USING LK-A.
            MAIN.
                DISPLAY LK-A.
            END METHOD M1.
            """).Replace("01 T USAGE OBJECT", $"01 W {driverArgPic}.\n01 T USAGE OBJECT");
        EditionHarness.AssertHasDiagnostic(ErrorsOf(Mk("OOSP21", "OSPC21", "PIC 9(5) VALUE 1", """
                INVOKE OSPC21 "NEW" RETURNING T.
                INVOKE T "M1" USING W.
            """)), "COBOLNET0828");
        EditionHarness.AssertHasDiagnostic(ErrorsOf(Mk("OOSP22", "OSPC22", "PIC 9(4) VALUE 1", """
                INVOKE OSPC22 "NEW" RETURNING T.
                INVOKE T "M1" USING W RETURNING W.
            """)), "COBOLNET0828");   // method declares no RETURNING
    }

    /// <summary>ISO §13.5.3 SR1 — "Within a class definition, the working-storage section may be specified only in a
    /// factory definition or an instance definition, but not in a method definition" — is the standard's own text, not
    /// a 2023 window (kb/Work PB1308, decision R62; Annex E.2 lists no removal of it), so a method's WORKING-STORAGE
    /// SECTION is refused at EVERY edition with COBOLNET1519 and <c>--permissive</c> does not relax it (a syntax-rule
    /// violation is not an edition-gated construct). The legal way to share a counter across activations is the
    /// instance definition's working-storage, which the same program then compiles and runs: one copy per INSTANCE,
    /// persistent across activations, while the method's LOCAL-STORAGE is initial on every activation (§8.6.4).</summary>
    [Theory]
    [InlineData(2002, false)]
    [InlineData(2014, false)]
    [InlineData(2023, false)]
    [InlineData(2002, true)]
    [InlineData(2023, true)]
    public void MethodWorkingStorage_IsRefusedAtEveryEdition(int dialect, bool permissive)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(MethodCounterSource("OOSP23", "OSPC23",
            "WORKING-STORAGE SECTION.\n01 CTR PIC 9(2) VALUE 0."), dialect, permissive: permissive);
        Assert.False(ok, $"method WS must be rejected at --std {dialect} (ISO §13.5.3 SR1)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1519");
    }

    [Fact]
    public void MethodStorage_ObjectWorkingStorageSharesAcrossActivations_LocalStorageDoesNot()
    {
        // The same counter, moved to where §13.5.3 SR1 allows it: the instance definition's working-storage.
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP24.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS OSPC24.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T1 USAGE OBJECT REFERENCE OSPC24.
            01 T2 USAGE OBJECT REFERENCE OSPC24.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE OSPC24 "NEW" RETURNING T1.
                INVOKE OSPC24 "NEW" RETURNING T2.
                INVOKE T1 "TICK".
                INVOKE T2 "TICK".
                INVOKE T1 "TICK".
                STOP RUN.
            END PROGRAM OOSP24.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC24 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OBJ-CTR PIC 9(2) VALUE 0.
            PROCEDURE DIVISION.
            METHOD-ID. TICK.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 LOC-CTR PIC 9(2) VALUE 0.
            PROCEDURE DIVISION.
            MAIN.
                ADD 1 TO OBJ-CTR.
                ADD 1 TO LOC-CTR.
                DISPLAY "OBJ=" OBJ-CTR " LOC=" LOC-CTR.
            END METHOD TICK.
            END OBJECT.
            END CLASS OSPC24.
            """;
        var (ok, stdout, detail) = CompileAndRun(src, 2002);
        Assert.True(ok, detail);
        // T1, T2, T1: object data is one copy per INSTANCE and persists; local-storage starts over every time.
        Assert.Equal("OBJ=01 LOC=01\nOBJ=01 LOC=01\nOBJ=02 LOC=01", CutRunner.Normalize(stdout));
    }

    /// <summary>The one-method counter program of the method-storage tests, the method's data division supplied.</summary>
    private static string MethodCounterSource(string program, string cls, string methodData) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{program}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS {{cls}}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 T1 USAGE OBJECT REFERENCE {{cls}}.
        PROCEDURE DIVISION.
        MAIN.
            INVOKE {{cls}} "NEW" RETURNING T1.
            INVOKE T1 "TICK".
            STOP RUN.
        END PROGRAM {{program}}.

        IDENTIFICATION DIVISION.
        CLASS-ID. {{cls}} INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        METHOD-ID. TICK.
        DATA DIVISION.
        {{methodData}}
        PROCEDURE DIVISION.
        MAIN.
            ADD 1 TO CTR.
            DISPLAY "CTR=" CTR.
        END METHOD TICK.
        END OBJECT.
        END CLASS {{cls}}.
        """;

    // ── Slice 3a/3b (INHERITS + SELF/SUPER — deep-dive D5/D7, §8.4.3.8, §9.3.6/§9.3.8.2) ───────────────────

    /// <summary>A three-level SUPER chain (trap #10's chain case): C : B : A, each override calls SUPER then
    /// speaks — the non-virtual restricted search (§8.4.3.8 GR3) runs A, B, C in order and cannot recurse;
    /// the same instance through a ROOT-typed reference still dispatches virtually to C's override (GR2).</summary>
    [Fact]
    public void ThreeLevelSuperChain_RunsBaseFirst_NoRecursion()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP24.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CH24A.
                CLASS CH24B.
                CLASS CH24C.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 C3 USAGE OBJECT REFERENCE CH24C.
            01 A1 USAGE OBJECT REFERENCE CH24A.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE CH24C "NEW" RETURNING C3.
                INVOKE C3 "M".
                INVOKE CH24C "NEW" RETURNING A1.
                INVOKE A1 "M".
                STOP RUN.
            END PROGRAM OOSP24.

            IDENTIFICATION DIVISION.
            CLASS-ID. CH24A INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "A".
            END METHOD M.
            END OBJECT.
            END CLASS CH24A.

            IDENTIFICATION DIVISION.
            CLASS-ID. CH24B INHERITS FROM CH24A.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CH24A.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M OVERRIDE.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE SUPER "M".
                DISPLAY "B".
            END METHOD M.
            END OBJECT.
            END CLASS CH24B.

            IDENTIFICATION DIVISION.
            CLASS-ID. CH24C INHERITS FROM CH24B.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CH24B.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M OVERRIDE.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE SUPER "M".
                DISPLAY "C".
            END METHOD M.
            END OBJECT.
            END CLASS CH24C.
            """);
        Assert.True(ok, detail);
        Assert.Equal("A\nB\nC\nA\nB\nC", CutRunner.Normalize(stdout));
    }

    /// <summary>Trap #2 — a CASE-MISMATCHED override spelling ("Speak" vs "SPEAK") still overrides and still
    /// dispatches virtually (COBOL names are case-insensitive, §8.3.2.2; the uppercase CsName convention
    /// collapses both onto ONE C# slot — the legacy silently took a NEW slot and dispatched to the base).</summary>
    [Fact]
    public void Trap2_CaseMismatchedOverride_StillDispatches()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP25.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE25.
                CLASS SUB25.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 B USAGE OBJECT REFERENCE BASE25.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE SUB25 "NEW" RETURNING B.
                INVOKE B "SPEAK".
                STOP RUN.
            END PROGRAM OOSP25.

            IDENTIFICATION DIVISION.
            CLASS-ID. BASE25 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. SPEAK.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "BASE".
            END METHOD SPEAK.
            END OBJECT.
            END CLASS BASE25.

            IDENTIFICATION DIVISION.
            CLASS-ID. SUB25 INHERITS FROM BASE25.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE25.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. Speak OVERRIDE.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "SUB".
            END METHOD Speak.
            END OBJECT.
            END CLASS SUB25.
            """);
        Assert.True(ok, detail);
        Assert.Equal("SUB", CutRunner.Normalize(stdout));
    }

    /// <summary>Subclass-OWN object data: per-instance on the DERIVED class, independent across instances,
    /// while an INHERITED method reads BASE state — the C#-native inheritance dividend (the legacy rejected
    /// subclass-own data outright).</summary>
    [Fact]
    public void SubclassOwnObjectData_IndependentPerInstance()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP26.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE26.
                CLASS SUB26.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 S1 USAGE OBJECT REFERENCE SUB26.
            01 S2 USAGE OBJECT REFERENCE SUB26.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE SUB26 "NEW" RETURNING S1.
                INVOKE SUB26 "NEW" RETURNING S2.
                INVOKE S1 "BUMPS".
                INVOKE S1 "GETS".
                INVOKE S2 "GETS".
                INVOKE S1 "GETB".
                STOP RUN.
            END PROGRAM OOSP26.

            IDENTIFICATION DIVISION.
            CLASS-ID. BASE26 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BN PIC 9(2) VALUE 10.
            PROCEDURE DIVISION.
            METHOD-ID. GETB.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "B=" BN.
            END METHOD GETB.
            END OBJECT.
            END CLASS BASE26.

            IDENTIFICATION DIVISION.
            CLASS-ID. SUB26 INHERITS FROM BASE26.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE26.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SN PIC 9(2) VALUE 20.
            PROCEDURE DIVISION.
            METHOD-ID. BUMPS.
            PROCEDURE DIVISION.
            MAIN.
                ADD 1 TO SN.
            END METHOD BUMPS.
            METHOD-ID. GETS.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "S=" SN.
            END METHOD GETS.
            END OBJECT.
            END CLASS SUB26.
            """);
        Assert.True(ok, detail);
        Assert.Equal("S=21\nS=20\nB=10", CutRunner.Normalize(stdout));
    }

    /// <summary>§9.3.8.2 — an override whose SIGNATURE does not conform to the overridden method is the
    /// compile-time COBOLNET0829 (never a Roslyn CS error on user source); trap #7 — SUPER in a root class
    /// and SELF outside any method are clean 0827 placement diagnostics; an INHERITS cycle is 0820.</summary>
    [Fact]
    public void InheritanceDiagnostics_0829_0827_0820()
    {
        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP27.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOSP27.

            IDENTIFICATION DIVISION.
            CLASS-ID. B27.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L PIC 9(4).
            PROCEDURE DIVISION USING L.
            MAIN.
                DISPLAY L.
            END METHOD M.
            END OBJECT.
            END CLASS B27.

            IDENTIFICATION DIVISION.
            CLASS-ID. S27 INHERITS FROM B27.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS B27.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L PIC 9(5).
            PROCEDURE DIVISION USING L.
            MAIN.
                DISPLAY L.
            END METHOD M.
            END OBJECT.
            END CLASS S27.
            """), "COBOLNET0829");
        // SUPER in a ROOT class: written without the DriverAndClass helper, whose class inherits the standard class
        // BASE (kb/Work PB1548) — a class that INHERITS FROM BASE is not a root, and its SUPER searches BASE.
        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC28.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE SUPER "M1".
            END METHOD M1.
            END OBJECT.
            END CLASS OSPC28.
            """), "COBOLNET0827");
        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP29.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOSP29.

            IDENTIFICATION DIVISION.
            CLASS-ID. A29 INHERITS FROM B29.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS B29.
            END CLASS A29.

            IDENTIFICATION DIVISION.
            CLASS-ID. B29 INHERITS FROM A29.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS A29.
            END CLASS B29.
            """), "COBOLNET0820");
    }

    // ── The 3a/3b adversarial-review fixes (workflow find→verify, DEVLOG 603) ───────────────────────────────

    /// <summary>§14.8.3.3 rule 1 — RETURNING delivery follows SET rules: a method returning a SUBCLASS object
    /// delivers into a receiver declared with the SUPERCLASS (widening — SET SR12a2), and into a UNIVERSAL
    /// receiver; C# covariance renders it directly. The former strict-identity 0828 was the review's confirmed
    /// spec violation.</summary>
    [Fact]
    public void ReturningWidening_SubclassIntoBaseAndUniversalReceivers()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP30.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS ANI30.
                CLASS DOG30.
                CLASS MAK30.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 M USAGE OBJECT REFERENCE MAK30.
            01 A USAGE OBJECT REFERENCE ANI30.
            01 U USAGE OBJECT REFERENCE.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE MAK30 "NEW" RETURNING M.
                INVOKE M "MAKE" RETURNING A.
                INVOKE A "SPEAK".
                INVOKE M "MAKE" RETURNING U.
                STOP RUN.
            END PROGRAM OOSP30.

            IDENTIFICATION DIVISION.
            CLASS-ID. ANI30 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. SPEAK.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "GENERIC".
            END METHOD SPEAK.
            END OBJECT.
            END CLASS ANI30.

            IDENTIFICATION DIVISION.
            CLASS-ID. DOG30 INHERITS FROM ANI30.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS ANI30.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. SPEAK OVERRIDE.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "WOOF".
            END METHOD SPEAK.
            END OBJECT.
            END CLASS DOG30.

            IDENTIFICATION DIVISION.
            CLASS-ID. MAK30 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE
                CLASS DOG30.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. MAKE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-D USAGE OBJECT REFERENCE DOG30.
            PROCEDURE DIVISION RETURNING LK-D.
            MAIN.
                INVOKE DOG30 "NEW" RETURNING LK-D.
            END METHOD MAKE.
            END OBJECT.
            END CLASS MAK30.
            """);
        Assert.True(ok, detail);
        Assert.Equal("WOOF", CutRunner.Normalize(stdout));   // virtual dispatch through the widened receiver
    }

    /// <summary>§9.3.8.2.3 rules 5a/5c2 — a COVARIANT override RETURNING (the override returns a SUBCLASS of
    /// the base method's returning class) is legal and dispatches; §14.8.2.3.2 rule 2 — a SIGN-clause
    /// mismatch on a BY REFERENCE argument is the 0828 the old check silently passed.</summary>
    [Fact]
    public void CovariantOverrideReturning_And_SignClauseConformance()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP31.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS ANI31.
                CLASS DOG31.
                CLASS FAC31.
                CLASS SUB31.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 F USAGE OBJECT REFERENCE FAC31.
            01 A USAGE OBJECT REFERENCE ANI31.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE SUB31 "NEW" RETURNING F.
                INVOKE F "MAKE" RETURNING A.
                INVOKE A "SPEAK".
                STOP RUN.
            END PROGRAM OOSP31.

            IDENTIFICATION DIVISION.
            CLASS-ID. ANI31 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. SPEAK.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "GENERIC".
            END METHOD SPEAK.
            END OBJECT.
            END CLASS ANI31.

            IDENTIFICATION DIVISION.
            CLASS-ID. DOG31 INHERITS FROM ANI31.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS ANI31.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. SPEAK OVERRIDE.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "WOOF".
            END METHOD SPEAK.
            END OBJECT.
            END CLASS DOG31.

            IDENTIFICATION DIVISION.
            CLASS-ID. FAC31 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE
                CLASS ANI31.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. MAKE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-A USAGE OBJECT REFERENCE ANI31.
            PROCEDURE DIVISION RETURNING LK-A.
            MAIN.
                INVOKE ANI31 "NEW" RETURNING LK-A.
            END METHOD MAKE.
            END OBJECT.
            END CLASS FAC31.

            IDENTIFICATION DIVISION.
            CLASS-ID. SUB31 INHERITS FROM FAC31.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS FAC31
                CLASS DOG31.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. MAKE OVERRIDE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-D USAGE OBJECT REFERENCE DOG31.
            PROCEDURE DIVISION RETURNING LK-D.
            MAIN.
                INVOKE DOG31 "NEW" RETURNING LK-D.
            END METHOD MAKE.
            END OBJECT.
            END CLASS SUB31.
            """);
        Assert.True(ok, detail);
        Assert.Equal("WOOF", CutRunner.Normalize(stdout));

        EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP32", "OSPC32", """
                INVOKE OSPC32 "NEW" RETURNING T.
                INVOKE T "M" USING WS.
            """, """
            METHOD-ID. M.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK PIC S9(4).
            PROCEDURE DIVISION USING LK.
            MAIN.
                DISPLAY LK.
            END METHOD M.
            """).Replace("01 T USAGE OBJECT",
                "01 WS PIC S9(4) SIGN LEADING SEPARATE VALUE -1.\n01 T USAGE OBJECT")), "SIGN clause mismatch");
    }

    /// <summary>§14.8.2.3.3 rule 2a — BY CONTENT numeric arguments follow COMPUTE rules: ANY numeric argument
    /// (or literal, §9.3.6 rule 5 — truncation legal) converts into the formal's description; the value
    /// crosses rescaled through the OWNER's internal profile.</summary>
    [Fact]
    public void ByContentNumeric_ComputeRuleConversion()
    {
        var (ok, stdout, detail) = CompileAndRun(DriverAndClass("OOSP33", "OSPC33", """
                INVOKE OSPC33 "NEW" RETURNING T.
                INVOKE T "SHOW" USING BY CONTENT W5.
                INVOKE T "SHOW" USING 42.5.
            """, """
            METHOD-ID. SHOW.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK PIC 9(3)V9.
            PROCEDURE DIVISION USING LK.
            MAIN.
                DISPLAY "LK=" LK.
            END METHOD SHOW.
            """).Replace("01 T USAGE OBJECT", "01 W5 PIC 9(5) VALUE 123.\n01 T USAGE OBJECT"));
        Assert.True(ok, detail);
        Assert.Equal("LK=1230\nLK=0425", CutRunner.Normalize(stdout));
    }

    /// <summary>§8.4.6.2.1 rule 3a — a METHOD-LOCAL declaration shadows the object level in EVERY lookup
    /// path: SEARCH's table resolution and the subscript index-name lookup must see the method's item, never
    /// the object's same-named table/index (the review's scope-bypass findings). And level-66 RENAMES in method
    /// data is now LIVE (M2-OO-1h step 1, DEVLOG 637 — the alias resolves structurally over the method record).
    /// <para>⛔ THE STAGE MOVED, AND THE INTENT DID NOT (kb/Work PB443, the PB390 shape again). The shadow half
    /// used to require the RUN to die: `SEARCH TAB2` over a method-local non-table returned a
    /// <c>BoundUnsupported</c>, so the program COMPILED, shipped an assembly and aborted the run unit claiming
    /// WiseOwl COBOL had not implemented a feature — a GREEN test pinning the broken stage, on a rule ISO §4.2.2 ¶2
    /// puts in the compile-time mechanism. It is now COBOLNET2075 quoting §14.9.37.3 SR2, and the SHADOWING
    /// claim is asserted more sharply than before: the error names TAB2 as "not a table", which only the
    /// METHOD-LOCAL `PIC X(4)` is — the object's same-named item IS a table with an INDEXED phrase, so a lookup
    /// that had bypassed the method scope would have compiled clean.</para></summary>
    [Fact]
    public void MethodScope_SearchAndIndexLookups_And66Renames()
    {
        // SEARCH of a method-local NON-table (shadowing an object-level TABLE of the same name) must bind
        // the METHOD-LOCAL item — the not-a-table verdict fires, never a silent search of the object's
        // table (§8.4.6.2.1 rule 3a: shadowing is replacement).
        var shadowErrors = ErrorsOf(DriverAndClass("OOSP34", "OSPC34", """
                INVOKE OSPC34 "NEW" RETURNING T.
                INVOKE T "M".
            """, """
            METHOD-ID. M.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 TAB2 PIC X(4).
            PROCEDURE DIVISION.
            MAIN.
                SEARCH TAB2
                    WHEN 1 = 1 CONTINUE
                END-SEARCH.
            END METHOD M.
            """).Replace("PROCEDURE DIVISION.\nMETHOD-ID. M.", """
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TWRAP.
               05 TAB2 PIC 9 OCCURS 3 INDEXED BY IXO.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            """));
        string shadow = Assert.Single(shadowErrors, x => x.Contains("COBOLNET2075", StringComparison.Ordinal));
        Assert.Contains("'TAB2' is not a table", shadow, StringComparison.Ordinal);
        Assert.Contains("§14.9.37.3 SR2", shadow, StringComparison.Ordinal);
        // level-66 RENAMES in method LOCAL-STORAGE now compiles and runs — the alias reads the record span
        // (§13.18.45 GR2), resolved structurally over the method's own record (M2-OO-1h step 1).
        var (ok66, out66, detail66) = CompileAndRun(DriverAndClass("OOSP35", "OSPC35", """
                INVOKE OSPC35 "NEW" RETURNING T.
                INVOKE T "M".
            """, """
            METHOD-ID. M.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 LS-REC.
               05 LS-A PIC X(2) VALUE "PQ".
               05 LS-B PIC X(2) VALUE "RS".
            66 LS-ALIAS RENAMES LS-A THRU LS-B.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "AL=" LS-ALIAS.
                GOBACK.
            END METHOD M.
            """));
        Assert.True(ok66, detail66);
        Assert.Contains("AL=PQRS", out66);
    }

    /// <summary>§11.7.4 GR5 (M2-OO-1h step 2) — a method table's OCCURS DEPENDING data-name-1 may resolve to a
    /// VISIBLE, UNSHADOWED OBJECT item (the global fallback in LookupDataInScopeOf), not only a method-local name.
    /// The gate is gone; a false COBOLNET0851 "not defined" must not fire on a method-scoped depending name.</summary>
    [Fact]
    public void MethodOdo_DependingOnVisibleObjectData()
    {
        var (ok, output, detail) = CompileAndRun(DriverAndClass("OOSPO2", "OSPO2C", """
                INVOKE OSPO2C "NEW" RETURNING T.
                INVOKE T "M".
            """, """
            METHOD-ID. M.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 TBL.
               05 ELT PIC X OCCURS 1 TO 5 DEPENDING ON OCNT.
            PROCEDURE DIVISION.
            MAIN.
                MOVE "X" TO ELT(1).
                MOVE "Y" TO ELT(2).
                MOVE "Z" TO ELT(3).
                DISPLAY "T=" TBL.
                GOBACK.
            END METHOD M.
            """).Replace("PROCEDURE DIVISION.\nMETHOD-ID. M.", """
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OCNT PIC 9 VALUE 3.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            """));
        Assert.True(ok, detail);
        Assert.Contains("T=XYZ", output);   // extent 3 (the object OCNT) — depending-name resolved via the fallback
    }

    /// <summary>§13.18.44.3 SR (M2-OO-1h step 3) — a method 01 REDEFINES may only overlay a preceding item in the
    /// SAME method scope; naming an OBJECT item is out of scope → COBOLNET1577 (renumbered from the 1518 collision, review V11), never a silent cross-scope bind
    /// (the pre-fix behavior bound the method redefiner to the object item through the global Roots pool).</summary>
    [Fact]
    public void MethodRedefines_TargetInObjectScope_Rejected()
    {
        EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSPR3", "OSPR3C", """
                INVOKE OSPR3C "NEW" RETURNING T.
            """, """
            METHOD-ID. M.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 MREDEF REDEFINES OBJNUM PIC X(4).
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY MREDEF.
                GOBACK.
            END METHOD M.
            """).Replace("PROCEDURE DIVISION.\nMETHOD-ID. M.", """
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OBJNUM PIC 9(4) VALUE 1.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            """)), "COBOLNET1577");
    }

    /// <summary>§13.18.44.3 SR (M2-OO-1h review B) — a REDEFINES target must be in the SAME data description; a
    /// LOCAL-STORAGE 01 may NOT redefine a method LINKAGE 01 (their storage classes differ — caller storage vs
    /// per-activation LOCAL; a method has no WORKING-STORAGE, §13.5.3 SR1). The scope is the redefiner's OWN section,
    /// so the cross-section target is not found → 1577.</summary>
    [Fact]
    public void MethodRedefines_CrossSection_Rejected()
    {
        EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSPXS", "OSPXSC", """
                INVOKE OSPXSC "NEW" RETURNING T.
            """, """
            METHOD-ID. M.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 LVIEW REDEFINES WNUM PIC X(4).
            LINKAGE SECTION.
            01 WNUM PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY LVIEW.
                GOBACK.
            END METHOD M.
            """)), "COBOLNET1577");
    }

    // ── The FACTORY slice (§11.4; brief D11 — DEVLOG 604) ───────────────────────────────────────────────────

    /// <summary>An instance method and a factory method may SHARE a name (§9.3.6 — two interfaces): INVOKE
    /// through the class-name resolves the FACTORY roster, through an object the INSTANCE roster — dual
    /// dispatch, no collision. A factory METHOD-ID named NEW is the 0836 v1 restriction; INVOKE class-name
    /// of a method in NEITHER factory roster is the SR3 0825; SUPER in a ROOT class's factory method is the
    /// trap-#7 0827 (factory flavor); a BY REFERENCE argument reading FACTORY WS violates SR 10 (0828).</summary>
    [Fact]
    public void Factory_DualRoster_AndDiagnosticBand()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP36.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS OSPC36.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE OBJECT REFERENCE OSPC36.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE OSPC36 "NEW" RETURNING T.
                INVOKE OSPC36 "PING".
                INVOKE T "PING".
                STOP RUN.
            END PROGRAM OOSP36.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC36 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            FACTORY.
            PROCEDURE DIVISION.
            METHOD-ID. PING.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "FACTORY-PING".
            END METHOD PING.
            END FACTORY.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. PING.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "INSTANCE-PING".
            END METHOD PING.
            END OBJECT.
            END CLASS OSPC36.
            """);
        Assert.True(ok, detail);
        Assert.Equal("FACTORY-PING\nINSTANCE-PING", CutRunner.Normalize(stdout));

        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC37.
            IDENTIFICATION DIVISION.
            FACTORY.
            PROCEDURE DIVISION.
            METHOD-ID. NEW.
            PROCEDURE DIVISION.
            END METHOD NEW.
            END FACTORY.
            END CLASS OSPC37.
            """), "COBOLNET0836");
        EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP38", "OSPC38", """
                INVOKE OSPC38 "NOFM".
            """, """
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            END METHOD M1.
            """)), "COBOLNET0825");
        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP39.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOSP39.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC39.
            IDENTIFICATION DIVISION.
            FACTORY.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE SUPER "M".
            END METHOD M.
            END FACTORY.
            END CLASS OSPC39.
            """), "COBOLNET0827");
        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP40.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM OOSP40.

            IDENTIFICATION DIVISION.
            CLASS-ID. OSPC40.
            IDENTIFICATION DIVISION.
            FACTORY.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FW PIC 9(2) VALUE 5.
            PROCEDURE DIVISION.
            METHOD-ID. M2.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LN PIC 9(2).
            PROCEDURE DIVISION USING LN.
            MAIN.
                ADD 1 TO LN.
            END METHOD M2.
            METHOD-ID. DRIVE.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE SELF "M2" USING BY REFERENCE FW.
            END METHOD DRIVE.
            END FACTORY.
            END CLASS OSPC40.
            """), "COBOLNET0828");
    }

    /// <summary>FACTORY is a legal USER WORD at COBOL-85 (§8.9 reserves it only from 2002) — the continuity
    /// invariant for the newly-admitted token; at 2002+ the funnel 0901s it.</summary>
    [Fact]
    public void FactoryWord_UserNameAt85_Reserved2002Plus()
    {
        var (ok85, errors85, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP41.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FACTORY PIC 9(2) VALUE 7.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY FACTORY.
                STOP RUN.
            END PROGRAM OOSP41.
            """, 85);
        Assert.True(ok85, "FACTORY must be a legal user word at --std 85: " + string.Join("\n", errors85));
        var (ok02, errors02, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP42.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FACTORY PIC 9(2) VALUE 7.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY FACTORY.
                STOP RUN.
            END PROGRAM OOSP42.
            """, 2002);
        Assert.False(ok02, "FACTORY is §8.9-reserved at 2002+ — the funnel must 0901");
        EditionHarness.AssertHasDiagnostic(errors02, "COBOLNET0901");
    }

    // ── The OVERRIDE/FINAL attribute wave (§11.7 SR3/SR4a/GR3, §11.3 GR3 — DEVLOG 605) ─────────────────────

    /// <summary>§11.7 SR4a — redefining an inherited method WITHOUT the OVERRIDE attribute is 0837 STRICT
    /// (the pre-wave name-match inference is retired as the default); under <c>--permissive</c> it is a
    /// WARNING and the inference stands (the documented migration leniency), so the pre-wave program still
    /// runs with virtual dispatch.</summary>
    [Fact]
    public void Sr4a_RedefinitionWithoutOverride_0837Strict_InferredPermissive()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP43.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BAS43.
                CLASS SUB43.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 B USAGE OBJECT REFERENCE BAS43.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE SUB43 "NEW" RETURNING B.
                INVOKE B "SPEAK".
                STOP RUN.
            END PROGRAM OOSP43.

            IDENTIFICATION DIVISION.
            CLASS-ID. BAS43 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. SPEAK.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "BASE".
            END METHOD SPEAK.
            END OBJECT.
            END CLASS BAS43.

            IDENTIFICATION DIVISION.
            CLASS-ID. SUB43 INHERITS FROM BAS43.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BAS43.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. SPEAK.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "SUB".
            END METHOD SPEAK.
            END OBJECT.
            END CLASS SUB43.
            """;
        var (okStrict, errsStrict, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(okStrict, "SR4a: redefinition without OVERRIDE must be rejected strict");
        EditionHarness.AssertHasDiagnostic(errsStrict, "COBOLNET0837");
        var (okPerm, errsPerm, warnsPerm) = EditionHarness.CompileFull(src, 2002, permissive: true);
        Assert.True(okPerm, "the migration leniency: --permissive keeps the pre-wave inference: "
            + string.Join("\n", errsPerm));
        EditionHarness.AssertHasDiagnostic(warnsPerm, "COBOLNET0837");
    }

    /// <summary>§11.7 SR3 — OVERRIDE with no matching superclass method (incl. no INHERITS at all) is 0838;
    /// §11.7 GR3 / §11.3 GR3 — overriding a FINAL method and inheriting a FINAL class are the 0839 family.</summary>
    [Fact]
    public void OverrideFinal_0838_0839()
    {
        EditionHarness.AssertHasDiagnostic(ErrorsOf(DriverAndClass("OOSP44", "OSPC44", """
                INVOKE OSPC44 "NEW" RETURNING T.
            """, """
            METHOD-ID. M OVERRIDE.
            PROCEDURE DIVISION.
            END METHOD M.
            """)), "COBOLNET0838");
        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. BAS45.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M IS FINAL.
            PROCEDURE DIVISION.
            END METHOD M.
            END OBJECT.
            END CLASS BAS45.

            IDENTIFICATION DIVISION.
            CLASS-ID. SUB45 INHERITS FROM BAS45.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BAS45.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M OVERRIDE.
            PROCEDURE DIVISION.
            END METHOD M.
            END OBJECT.
            END CLASS SUB45.
            """), "COBOLNET0839");
        EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. BAS46 IS FINAL.
            END CLASS BAS46.

            IDENTIFICATION DIVISION.
            CLASS-ID. SUB46 INHERITS FROM BAS46.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BAS46.
            END CLASS SUB46.
            """), "COBOLNET0839");
    }

    /// <summary>OVERRIDE is a legal USER word at COBOL-85 (§8.9 reserves it from 2002) — the continuity
    /// invariant for the newly-admitted token; the funnel 0901s it at 2002+.</summary>
    [Fact]
    public void OverrideWord_UserNameAt85_Reserved2002Plus()
    {
        var (ok85, errors85, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP47.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OVERRIDE PIC 9(2) VALUE 3.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY OVERRIDE.
                STOP RUN.
            END PROGRAM OOSP47.
            """, 85);
        Assert.True(ok85, "OVERRIDE must be a legal user word at --std 85: " + string.Join("\n", errors85));
        var (ok02, errors02, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP48.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OVERRIDE PIC 9(2) VALUE 3.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY OVERRIDE.
                STOP RUN.
            END PROGRAM OOSP48.
            """, 2002);
        Assert.False(ok02, "OVERRIDE is §8.9-reserved at 2002+ — the funnel must 0901");
        EditionHarness.AssertHasDiagnostic(errors02, "COBOLNET0901");
    }

    // ── The INTERFACE/PROPERTY wave (§11.5/§11.6/§11.8/§13.18.42 — DEVLOG 606) ──────────────────────────────

    private const string SpeakerInterface = """
        IDENTIFICATION DIVISION.
        INTERFACE-ID. ISPK50.
        PROCEDURE DIVISION.
        METHOD-ID. SPEAK.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-N PIC 9(4).
        PROCEDURE DIVISION USING LK-N.
        END METHOD SPEAK.
        END INTERFACE ISPK50.
        """;

    /// <summary>THE headline 0841: PIC 9(4) and PIC 9(8) formals BOTH project to C# <c>ref long</c>, so
    /// Roslyn would accept the implementation — §9.3.8.2.3 rules 2/3 demand identical descriptions and only
    /// the binder can check them. The under-rejection direction of the D-I1 authority argument.</summary>
    [Fact]
    public void Implements_NumericDescriptionMismatch_0841()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf($$"""
            {{SpeakerInterface}}

            IDENTIFICATION DIVISION.
            CLASS-ID. CSPK50.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                INTERFACE ISPK50.
            IDENTIFICATION DIVISION.
            OBJECT. IMPLEMENTS ISPK50.
            PROCEDURE DIVISION.
            METHOD-ID. SPEAK.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-N PIC 9(8).
            PROCEDURE DIVISION USING LK-N.
            END METHOD SPEAK.
            END OBJECT.
            END CLASS CSPK50.
            """), "COBOLNET0841");

    /// <summary>§9.3.11 — a class shall implement ALL prototypes of its interfaces (incl. inherited).</summary>
    [Fact]
    public void Implements_MissingMethod_0841()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf($$"""
            {{SpeakerInterface}}

            IDENTIFICATION DIVISION.
            CLASS-ID. CSPK51.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                INTERFACE ISPK50.
            IDENTIFICATION DIVISION.
            OBJECT. IMPLEMENTS ISPK50.
            END OBJECT.
            END CLASS CSPK51.
            """), "COBOLNET0841");

    /// <summary>The P6 phase-review find (DEVLOG 775): the crossing-form harmonize must cover
    /// interface-IMPLEMENTATION pairs, not just override chains. The implementing method's body flips its
    /// numeric-DISPLAY formal to image storage (a ref-mod store — StoreAsImage), while the interface PROTOTYPE's
    /// identical formal has no body and stays native: without the implements-pair unification the emitted C# is
    /// interface member `M(ref long)` vs class method `M(ref string)` — Roslyn CS0535/CS0738 on legal COBOL
    /// (§9.3.11 / §9.3.8.2.3: the descriptions ARE identical; the storage form is a compile-time decision the
    /// harmonize must settle on BOTH sides).</summary>
    [Fact]
    public void Implements_RefModStoreInFormal_HarmonizesInterfaceCrossing()
    {
        var (ok, stdout, detail) = CompileAndRun($$"""
            {{SpeakerInterface}}

            IDENTIFICATION DIVISION.
            CLASS-ID. CSPK54 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE
                INTERFACE ISPK50.
            IDENTIFICATION DIVISION.
            OBJECT. IMPLEMENTS ISPK50.
            PROCEDURE DIVISION.
            METHOD-ID. SPEAK.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-N PIC 9(4).
            PROCEDURE DIVISION USING LK-N.
            M-1.
                MOVE "7" TO LK-N (1:1).
                DISPLAY "SPOKE " LK-N.
            END METHOD SPEAK.
            END OBJECT.
            END CLASS CSPK54.

            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP54.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CSPK54
                INTERFACE ISPK50.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 OBJ USAGE OBJECT REFERENCE CSPK54.
            01 WS-N PIC 9(4) VALUE 1234.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE CSPK54 "NEW" RETURNING OBJ.
                INVOKE OBJ "SPEAK" USING WS-N.
                DISPLAY "AFTER " WS-N.
                STOP RUN.
            END PROGRAM OOSP54.
            """);
        Assert.True(ok, detail);
        // The ref-mod store replaces position 1 of "1234" with '7'; BY REFERENCE makes it visible to the caller.
        Assert.Contains("SPOKE 7234", stdout);
        Assert.Contains("AFTER 7234", stdout);
    }

    /// <summary>§11.3.2 permits several INHERITS bases; WiseOwl COBOL v1 restricts to SINGLE inheritance and
    /// rejects 2+ LOUDLY (SSOT §18 #18 / A.4.10 — the R9 fix: the repetition PARSES per the superset-parse
    /// doctrine, then pass-1 raises 0849; previously the 2nd base was a bare syntax error).</summary>
    [Fact]
    public void Class_MultiBaseInherits_0849()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. MBSPK60 INHERITS FROM MBBASEA MBBASEB.
            END CLASS MBSPK60.
            IDENTIFICATION DIVISION.
            CLASS-ID. MBBASEA.
            END CLASS MBBASEA.
            IDENTIFICATION DIVISION.
            CLASS-ID. MBBASEB.
            END CLASS MBBASEB.
            """), "COBOLNET0849");

    /// <summary>§10.7 — END INTERFACE names its interface (the 0840 structural family).</summary>
    [Fact]
    public void Interface_EndNameMismatch_0840()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            INTERFACE-ID. ISPK52.
            END INTERFACE OTHER52.
            """), "COBOLNET0840");

    /// <summary>§10.6.2 SR4 f) — a method prototype is a header only. A body is COBOLNET2272, the ONE
    /// prototype-body screen every prototype kind shares (kb/Work PB894; formerly the 0840 family).</summary>
    [Fact]
    public void Interface_PrototypeWithBody_2272()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            INTERFACE-ID. ISPK53.
            PROCEDURE DIVISION.
            METHOD-ID. PING.
            PROCEDURE DIVISION.
            MAIN.
                CONTINUE.
            END METHOD PING.
            END INTERFACE ISPK53.
            """), "COBOLNET2272");

    /// <summary>§11.7 SR6 — a GET accessor has no USING and exactly one RETURNING (0842).</summary>
    [Fact]
    public void AccessorShape_GetWithUsing_0842()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. CPRP54.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. GET PROPERTY BAL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-V PIC 9(4).
            PROCEDURE DIVISION USING LK-V.
            END METHOD.
            END OBJECT.
            END CLASS CPRP54.
            """), "COBOLNET0842");

    /// <summary>§11.7 SR5 — a PROPERTY-clause subject shall not ALSO have an explicit accessor (0842).</summary>
    [Fact]
    public void PropertyClause_DuplicateExplicitAccessor_0842()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. CPRP55.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BAL PIC 9(4) PROPERTY.
            PROCEDURE DIVISION.
            METHOD-ID. GET PROPERTY BAL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-R PIC 9(4).
            PROCEDURE DIVISION RETURNING LK-R.
            END METHOD.
            END OBJECT.
            END CLASS CPRP55.
            """), "COBOLNET0842");

    /// <summary>The property REFERENCE binds (§8.4.3.9 — the GR1 implicit get-INVOKE desugar; DEVLOG 607
    /// retired the DEVLOG-606 named-0899 stage) when the §8.4.3.9.3 SR1 REPOSITORY specifier is present.</summary>
    [Fact]
    public void PropertyReference_Binds_WithSpecifier()
    {
        var (ok, errors, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP56.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CPRP56.
                PROPERTY BAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A USAGE OBJECT REFERENCE CPRP56.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE CPRP56 "NEW" RETURNING A.
                DISPLAY BAL OF A.
                STOP RUN.
            END PROGRAM OOSP56.

            IDENTIFICATION DIVISION.
            CLASS-ID. CPRP56 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BAL PIC 9(4) PROPERTY.
            PROCEDURE DIVISION.
            END OBJECT.
            END CLASS CPRP56.
            """, 2002);
        Assert.True(ok, "a specifier-backed property reference must bind (GR1): " + string.Join("\n", errors));
    }

    /// <summary>PROPERTY/GET/INTERFACE are legal USER words at 85 (§8.9 reserves them from 2002) — the
    /// continuity invariant for the wave's newly-admitted tokens; the funnel 0901s them at 2002+.</summary>
    [Theory]
    [InlineData("PROPERTY")]
    [InlineData("GET")]
    [InlineData("INTERFACE")]
    public void InterfaceWaveWords_UserNamesAt85_Reserved2002Plus(string word)
    {
        string src(string pid) => $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 {word} PIC 9(2) VALUE 3.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY {word}.
                STOP RUN.
            END PROGRAM {pid}.
            """;
        var (ok85, errors85, _) = EditionHarness.CompileFull(src("OOSP57"), 85);
        Assert.True(ok85, $"{word} must be a legal user word at --std 85: " + string.Join("\n", errors85));
        var (ok02, errors02, _) = EditionHarness.CompileFull(src("OOSP58"), 2002);
        Assert.False(ok02, $"{word} is §8.9-reserved at 2002+ — the funnel must 0901");
        EditionHarness.AssertHasDiagnostic(errors02, "COBOLNET0901");
    }

    /// <summary>IMPLEMENTS is §8.10 CONTEXT-SENSITIVE (spec :10853), NOT §8.9-reserved — a legal user word
    /// at EVERY edition, including 2023 (the deliberate asymmetry with the XOR-recipe words).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ImplementsWord_UserWordAtAllEditions(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP59.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 IMPLEMENTS PIC 9(2) VALUE 7.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY IMPLEMENTS.
                STOP RUN.
            END PROGRAM OOSP59.
            """, edition);
        Assert.True(ok, $"IMPLEMENTS must stay a user word at --std {edition} (§8.10 context-sensitive): "
            + string.Join("\n", errors));
    }

    /// <summary>SET SR10/§9.3.8.2 — a class that does NOT implement the interface cannot widen into an
    /// interface-typed receiver (the negative of the oo_interface golden's NEW-RETURNING path).</summary>
    [Fact]
    public void InterfaceReceiver_NonImplementingClass_Rejected()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf($$"""
            {{SpeakerInterface}}

            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOSP60.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CSPK60.
                INTERFACE ISPK50.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 S USAGE OBJECT REFERENCE ISPK50.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE CSPK60 "NEW" RETURNING S.
                STOP RUN.
            END PROGRAM OOSP60.

            IDENTIFICATION DIVISION.
            CLASS-ID. CSPK60 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS CSPK60.
            """), "COBOLNET0826");

    // ── Property REFERENCES (§8.4.3.9 — the GR1–GR3 desugar; DEVLOG 607) ────────────────────────────────────

    private static string PropRefDriver(string pid, string cls, string repository, string statements) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS {cls}.
        {repository}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 A USAGE OBJECT REFERENCE {cls}.
        01 W PIC 9(4).
        PROCEDURE DIVISION.
        MAIN.
            INVOKE {cls} "NEW" RETURNING A.
        {statements}
            STOP RUN.
        END PROGRAM {pid}.
        """;

    /// <summary>§8.4.3.9.3 SR1 — a property reference requires a REPOSITORY PROPERTY specifier (0843).</summary>
    [Fact]
    public void PropertyReference_NoRepositorySpecifier_0843()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(PropRefDriver("OOSP61", "CPRR61", "",
            "    DISPLAY BAL OF A.") + """

            IDENTIFICATION DIVISION.
            CLASS-ID. CPRR61 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BAL PIC 9(4) PROPERTY.
            PROCEDURE DIVISION.
            END OBJECT.
            END CLASS CPRR61.
            """), "COBOLNET0843");

    /// <summary>§8.4.3.9.3 SR3 — a SENDING property reference needs a GET accessor; WITH NO GET → 0843.</summary>
    [Fact]
    public void PropertyReference_SendingWithNoGet_0843()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(PropRefDriver("OOSP62", "CPRR62", "    PROPERTY BAL.",
            "    MOVE BAL OF A TO W.") + """

            IDENTIFICATION DIVISION.
            CLASS-ID. CPRR62 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BAL PIC 9(4) PROPERTY WITH NO GET.
            PROCEDURE DIVISION.
            END OBJECT.
            END CLASS CPRR62.
            """), "COBOLNET0843");

    /// <summary>§8.4.3.9.3 SR4 — a RECEIVING property reference needs a SET accessor; WITH NO SET → 0843.
    /// The polarity is CLASSIFIED (BoundStores), so the same reference is fine as a sender.</summary>
    [Fact]
    public void PropertyReference_ReceivingWithNoSet_0843()
    {
        string cls = """

            IDENTIFICATION DIVISION.
            CLASS-ID. CPRR63 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BAL PIC 9(4) VALUE 5 PROPERTY WITH NO SET.
            PROCEDURE DIVISION.
            END OBJECT.
            END CLASS CPRR63.
            """;
        EditionHarness.AssertHasDiagnostic(ErrorsOf(PropRefDriver("OOSP63", "CPRR63", "    PROPERTY BAL.",
            "    MOVE 9 TO BAL OF A.") + cls), "COBOLNET0843");
        var (ok, errors, _) = EditionHarness.CompileFull(PropRefDriver("OOSP64", "CPRR63", "    PROPERTY BAL.",
            "    MOVE BAL OF A TO W.") + cls, 2002);
        Assert.True(ok, "the SAME property must remain readable (GR1 — sending only): "
            + string.Join("\n", errors));
    }

    // ── The UNIVERSAL wave (D10, §13.18.60.4 / §14.9.23 GR7c / §14.9.39 F5 / §8.8.4.2 F3 — DEVLOG 608) ──────

    /// <summary>One driver + two classes whose same-named method takes DIFFERENT PIC formals — THE
    /// polymorphic hazard: a wrong-shaped crossing through universal must NOT bind (§9.3.6 match rule 3 —
    /// EC-OO-METHOD, kb/Work PB1500), never deliver silently wrong data (both formals project to C# <c>ref long</c>);
    /// a BOUND method's residual violation (a RETURNING description that differs) is GR7c's EC-OO-UNIVERSAL.
    ///
    /// <para>⚠ The <c>&gt;&gt;TURN</c> is REQUIRED for these tests to assert the EC NAME, and its position is
    /// load-bearing: §14.9.23.4 GR7c sets EC-OO-UNIVERSAL only "if checking for it is enabled in BOTH the
    /// activated method and the activating runtime element". At the top of the compilation group it covers the
    /// program AND the classes, so the tests exercise the enabled-in-both path. Without it nothing may be
    /// attributed — the crossing is still refused, but as a non-attributing implementor stop, which is what
    /// <c>ExceptionConditionConformanceTests.OoUniversal_EnabledInActivatorOnly_…</c> covers. These tests
    /// previously carried no directive and passed only because the raise was unconditional.</para></summary>
    private static string UnivHazard(string pid, string statements) => $$"""
        >>TURN EC-OO-UNIVERSAL CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{pid}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS CUH4A.
            CLASS CUH8B.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 U USAGE OBJECT REFERENCE.
        01 N4 PIC 9(4) VALUE 1.
        01 N8 PIC 9(8) VALUE 1.
        PROCEDURE DIVISION.
        MAIN.
        {{statements}}
            STOP RUN.
        END PROGRAM {{pid}}.

        IDENTIFICATION DIVISION.
        CLASS-ID. CUH4A INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        METHOD-ID. BUMP.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-N PIC 9(4).
        PROCEDURE DIVISION USING LK-N.
        MAIN.
            ADD 1 TO LK-N.
        END METHOD BUMP.
        METHOD-ID. RD4.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-R PIC 9(4).
        PROCEDURE DIVISION RETURNING LK-R.
        MAIN.
            MOVE 42 TO LK-R.
        END METHOD RD4.
        END OBJECT.
        END CLASS CUH4A.

        IDENTIFICATION DIVISION.
        CLASS-ID. CUH8B INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        METHOD-ID. BUMP.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-N PIC 9(8).
        PROCEDURE DIVISION USING LK-N.
        MAIN.
            ADD 1 TO LK-N.
        END METHOD BUMP.
        END OBJECT.
        END CLASS CUH8B.
        """;

    [Fact]
    public void Universal_WrongShapedArg_NoMatch_EcOoMethod()
    {
        // U holds a CUH8B (formal 9(8)); the caller crosses N4 (9(4)) — same C# type, WRONG description. §9.3.6 match
        // rule 3 e) makes the description a condition of RESOLUTION, so BUMP is not bound and step 6) sets EC-OO-METHOD
        // (kb/Work PB1500) — never silently wrong data, and never GR7c's conformance violation of an unbound method.
        var (ok, stdout, detail) = CompileAndRun(UnivHazard("OOUV10", """
            INVOKE CUH8B "NEW" RETURNING U.
            INVOKE U "BUMP" USING N4.
        """));
        Assert.False(ok, "a non-matching universal INVOKE must FAIL: " + stdout);
        Assert.Contains("EC-OO-METHOD", detail);
        Assert.DoesNotContain("EC-OO-UNIVERSAL", detail);
    }

    [Fact]
    public void Universal_RightShapedArg_Runs()
    {
        var (ok, _, detail) = CompileAndRun(UnivHazard("OOUV11", """
            INVOKE CUH4A "NEW" RETURNING U.
            INVOKE U "BUMP" USING N4.
            INVOKE CUH8B "NEW" RETURNING U.
            INVOKE U "BUMP" USING N8.
        """));
        Assert.True(ok, "conforming universal crossings must run: " + detail);
    }

    [Fact]
    public void Universal_ArityMismatch_NoMatch_EcOoMethod()
    {
        // §9.3.6 match rule 1 (an equal number of parameters) is a condition of resolution — kb/Work PB1500.
        var (ok, _, detail) = CompileAndRun(UnivHazard("OOUV12", """
            INVOKE CUH4A "NEW" RETURNING U.
            INVOKE U "BUMP".
        """));
        Assert.False(ok);
        Assert.Contains("EC-OO-METHOD", detail);
        Assert.DoesNotContain("EC-OO-UNIVERSAL", detail);
    }

    [Fact]
    public void Universal_ReturningPresenceMismatch_NoMatch_EcOoMethod()
    {
        // BUMP declares no RETURNING — §9.3.6 match rule 1: "if there is a returning item in the invocation there shall be
        // a returning item in the invoked method", so BUMP is not bound (kb/Work PB1500).
        var (ok, _, detail) = CompileAndRun(UnivHazard("OOUV13", """
            INVOKE CUH4A "NEW" RETURNING U.
            INVOKE U "BUMP" USING N4 RETURNING N8.
        """));
        Assert.False(ok);
        Assert.Contains("EC-OO-METHOD", detail);
        Assert.DoesNotContain("EC-OO-UNIVERSAL", detail);
    }

    [Fact]
    public void Universal_ReturningDescriptionMismatch_Bound_EcOoUniversal()
    {
        // RD4 RETURNING PIC 9(4) into the caller's PIC 9(8): the pair can meet in a MOVE, so §9.3.6 match rule 7 BINDS
        // GET — and §14.8.3.3 then requires the SAME PICTURE, which §14.9.23.4 GR7 c) checks of the bound method:
        // EC-OO-UNIVERSAL, checking being enabled in both elements (kb/Work PB1500).
        var (ok, _, detail) = CompileAndRun(UnivHazard("OOUVRD", """
            INVOKE CUH4A "NEW" RETURNING U.
            INVOKE U "RD4" RETURNING N8.
        """));
        Assert.False(ok);
        Assert.Contains("EC-OO-UNIVERSAL", detail);
    }

    [Fact]
    public void Universal_ReturningReferenceClassMismatch_NoMatch_EcOoMethod()
    {
        // RD4 RETURNING PIC 9(4) into an OBJECT REFERENCE: no MOVE or SET can deliver it, so §9.3.6 match rules 6/7
        // fail and nothing is bound (kb/Work PB1500).
        var (ok, _, detail) = CompileAndRun(UnivHazard("OOUVRC", """
            INVOKE CUH4A "NEW" RETURNING U.
            INVOKE U "RD4" RETURNING U.
        """));
        Assert.False(ok);
        Assert.Contains("EC-OO-METHOD", detail);
    }

    [Fact]
    public void Universal_UnknownMethod_EcOoMethod()
    {
        var (ok, _, detail) = CompileAndRun(UnivHazard("OOUV14", """
            INVOKE CUH4A "NEW" RETURNING U.
            INVOKE U "NOSUCH".
        """));
        Assert.False(ok);
        Assert.Contains("EC-OO-METHOD", detail);
    }

    [Fact]
    public void Universal_NullReceiver_EcOoNull()
    {
        var (ok, _, detail) = CompileAndRun(UnivHazard("OOUV15", """
            INVOKE U "BUMP" USING N4.
        """));
        Assert.False(ok);
        Assert.Contains("EC-OO-NULL", detail);
    }

    /// <summary>§14.9.23.3 SR6 — BY CONTENT/BY VALUE and literal arguments are compile-rejected through a
    /// universal receiver (0866).</summary>
    [Theory]
    [InlineData("INVOKE U \"BUMP\" USING BY CONTENT N4.")]
    [InlineData("INVOKE U \"BUMP\" USING BY VALUE N4.")]
    [InlineData("INVOKE U \"BUMP\" USING 5.")]
    public void Universal_ForbiddenArgShapes_0866(string stmt)
        => EditionHarness.AssertHasDiagnostic(
            ErrorsOf(UnivHazard("OOUV16", "    INVOKE CUH4A \"NEW\" RETURNING U.\n    " + stmt)),
            "COBOLNET0866");

    /// <summary>§14.9.39 F5 SR12 — a UNIVERSAL sender cannot narrow into a TYPED receiver (an object view
    /// is the narrowing tool); SUPER is never a sender (SR9); a non-object receiver fails SR8.</summary>
    [Theory]
    [InlineData("SET G TO U.")]
    [InlineData("SET U TO SUPER.")]
    [InlineData("SET N4 TO U.")]
    public void SetObjectRef_Violations_0867(string stmt)
        => EditionHarness.AssertHasDiagnostic(ErrorsOf($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOUV17.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CUV17.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 U USAGE OBJECT REFERENCE.
            01 G USAGE OBJECT REFERENCE CUV17.
            01 N4 PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                {{stmt}}
                STOP RUN.
            END PROGRAM OOUV17.

            IDENTIFICATION DIVISION.
            CLASS-ID. CUV17.
            END CLASS CUV17.
            """), "COBOLNET0867");

    // ── The EC-OO wave (§14.9.29 / §14.9.18 SR4 / §14.9.49 F4 / §8.4.3.6 — DEVLOG 609) ─────────────────────

    private static string EcOoDriver(string pid, string decls, string statements) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{pid}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS CEOX1
            CLASS CEOX2.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 E USAGE OBJECT REFERENCE CEOX1.
        01 X2 USAGE OBJECT REFERENCE CEOX2.
        01 U USAGE OBJECT REFERENCE.
        01 N4 PIC 9(4).
        PROCEDURE DIVISION.
        {{decls}}
        MAIN SECTION.
        MAIN-P.
            INVOKE CEOX1 "NEW" RETURNING E.
        {{statements}}
            STOP RUN.
        END PROGRAM {{pid}}.

        IDENTIFICATION DIVISION.
        CLASS-ID. CEOX1 INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        END CLASS CEOX1.

        IDENTIFICATION DIVISION.
        CLASS-ID. CEOX2.
        END CLASS CEOX2.
        """;

    /// <summary>§14.9.29.4 GR2 — a RAISE of an object with no matching declarative CONTINUES with the next
    /// statement; it is never fatal by itself.</summary>
    [Fact]
    public void RaiseObject_NoDeclarative_ContinuesNextStatement()
    {
        var (ok, stdout, detail) = CompileAndRun(EcOoDriver("OOEC10", "",
            "    RAISE E.\n    DISPLAY \"CONTINUED\"."));
        Assert.True(ok, detail);
        Assert.Equal("CONTINUED", CutRunner.Normalize(stdout));
    }

    /// <summary>§14.6.13.1.5 rule 4 + §14.9.18.4 GR1 b) — the EC-OO-EXCEPTION conversion of an object no F4 declarative
    /// took is RAISED in the activating element only "if checking for that exception condition is enabled" THERE
    /// (§14.6.13.1.1: off by default), and then it enters the F3 tiers: a USE AFTER EC EC-OO declarative catches it.
    /// EC-OO-EXCEPTION is FATAL (Table 13), so surviving it needs RESUME AT NEXT STATEMENT (§14.6.13.1.3 #5 NOTE 2
    /// — the same rule as every fatal named condition). The checking-OFF twin below is kb/Work PB1121.</summary>
    [Fact]
    public void GobackRaisingObject_NoF4_ActivatorChecked_F3CatchesEcOoException()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            >>TURN EC-OO-EXCEPTION CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOEC11.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CEOY1
                CLASS CEOY2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 S USAGE OBJECT REFERENCE CEOY2.
            PROCEDURE DIVISION.
            DECLARATIVES.
            CATCH-SEC SECTION.
                USE AFTER EC EC-OO.
            CATCH-P.
                DISPLAY "F3-CAUGHT".
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                INVOKE CEOY2 "NEW" RETURNING S.
                INVOKE S "BOOM".
                DISPLAY "AFTER".
                STOP RUN.
            END PROGRAM OOEC11.

            IDENTIFICATION DIVISION.
            CLASS-ID. CEOY1 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS CEOY1.

            IDENTIFICATION DIVISION.
            CLASS-ID. CEOY2 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE
                CLASS CEOY1.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-E USAGE OBJECT REFERENCE CEOY1.
            PROCEDURE DIVISION.
            METHOD-ID. BOOM.
            PROCEDURE DIVISION RAISING CEOY1.
            MAIN.
                INVOKE CEOY1 "NEW" RETURNING W-E.
                GOBACK RAISING W-E.
            END METHOD BOOM.
            END OBJECT.
            END CLASS CEOY2.
            """);
        Assert.True(ok, detail);
        Assert.Equal("F3-CAUGHT\nAFTER", CutRunner.Normalize(stdout));
    }

    /// <summary>kb/Work PB1121 — the checking-OFF twin of <see cref="GobackRaisingObject_NoF4_ActivatorChecked_F3CatchesEcOoException"/>:
    /// EC-OO-EXCEPTION checking is not enabled in the activator, so §14.9.18.4 GR1 b) raises nothing, the F3
    /// declarative does not run and the INVOKE completes. (The unrecovered checked arm is
    /// <c>FatalRaiseSelectionTests.ObjectPropagation_ActivatorChecked_NoHandler_TerminatesAbnormally</c>.)</summary>
    [Fact]
    public void GobackRaisingObject_NoF4_ActivatorUnchecked_RaisesNothing()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOEC11B.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CEOY1B
                CLASS CEOY2B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 S USAGE OBJECT REFERENCE CEOY2B.
            PROCEDURE DIVISION.
            DECLARATIVES.
            CATCH-SEC SECTION.
                USE AFTER EC EC-OO.
            CATCH-P.
                DISPLAY "F3-WRONG".
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                INVOKE CEOY2B "NEW" RETURNING S.
                INVOKE S "BOOM".
                DISPLAY "AFTER".
                STOP RUN.
            END PROGRAM OOEC11B.

            IDENTIFICATION DIVISION.
            CLASS-ID. CEOY1B INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS CEOY1B.

            IDENTIFICATION DIVISION.
            CLASS-ID. CEOY2B INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE
                CLASS CEOY1B.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-E USAGE OBJECT REFERENCE CEOY1B.
            PROCEDURE DIVISION.
            METHOD-ID. BOOM.
            PROCEDURE DIVISION RAISING CEOY1B.
            MAIN.
                INVOKE CEOY1B "NEW" RETURNING W-E.
                GOBACK RAISING W-E.
            END METHOD BOOM.
            END OBJECT.
            END CLASS CEOY2B.
            """);
        Assert.True(ok, detail);
        Assert.Equal("AFTER", CutRunner.Normalize(stdout));
    }

    /// <summary>§9.3.8.2 :12291 — SET typed TO EXCEPTION-OBJECT narrows at RUNTIME; a wrong-class object
    /// raises EC-OO-UNIVERSAL.</summary>
    [Fact]
    public void SetTypedFromExceptionObject_WrongClass_EcOoUniversal()
    {
        var (ok, _, detail) = CompileAndRun(EcOoDriver("OOEC12", "",
            "    RAISE E.\n    SET X2 TO EXCEPTION-OBJECT."));
        Assert.False(ok);
        Assert.Contains("EC-OO-UNIVERSAL", detail);
    }

    /// <summary>The 0848 band: RAISE NULL (SR2), RAISE of a non-object item (SR2), and EXCEPTION-OBJECT as
    /// a receiving operand (§8.4.3.6 SR1).</summary>
    [Theory]
    [InlineData("    RAISE NULL.", "COBOLNET0848")]
    [InlineData("    RAISE N4.", "COBOLNET0848")]
    // kb/Work PB922: ONE rule, ONE code. ISO §8.4.3.6.3 SR1 is a rule about EVERY receiving operand, so SET's
    // arm now draws the SAME COBOLNET2196 the general receiving chokepoint does, and with SR1's own clause number
    // (the message said §8.4.3.6, where cite.py --check FAILS).
    [InlineData("    SET EXCEPTION-OBJECT TO E.", "COBOLNET2196")]
    public void RaiseObject_BindViolations(string stmt, string code)
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(EcOoDriver("OOEC13", "", stmt)), code);

    /// <summary>The 0849 band (§14.9.18.3 SR4): a UNIVERSAL identifier (SR4d) and a declared class missing
    /// from the PD-header RAISING list (SR4a).</summary>
    [Theory]
    [InlineData("    GOBACK RAISING U.")]
    [InlineData("    GOBACK RAISING E.")]
    public void GobackRaising_BindViolations_0849(string stmt)
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(EcOoDriver("OOEC14", "", stmt)), "COBOLNET0849");

    /// <summary>§14.2.2 SR7 — a header RAISING exception-name shall be level-3 EC-USER (0858).</summary>
    [Fact]
    public void MethodHeaderRaising_NonEcUser_0858()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. CEOZ1.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            PROCEDURE DIVISION RAISING EC-SIZE-TRUNCATION.
            MAIN.
                CONTINUE.
            END METHOD M1.
            END OBJECT.
            END CLASS CEOZ1.
            """), "COBOLNET0858");

    /// <summary>§14.9.49.3 SR16 — USE AFTER EXCEPTION OBJECT names a class of the group (0859).</summary>
    [Fact]
    public void UseF4_UnknownClass_0859()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(EcOoDriver("OOEC15",
            """
            DECLARATIVES.
            BAD-SEC SECTION.
                USE AFTER EXCEPTION OBJECT NOSUCH.
            BAD-P.
                CONTINUE.
            END DECLARATIVES.
            """,
            "    CONTINUE.")), "COBOLNET0859");

    /// <summary>§8.8.4.2.1 Format 3 — ordering operators and object-vs-non-object mixes reject (0868).</summary>
    [Theory]
    [InlineData("IF U > G DISPLAY \"X\" END-IF.")]
    [InlineData("IF U = N4 DISPLAY \"X\" END-IF.")]
    public void ObjectRelation_Violations_0868(string stmt)
        => EditionHarness.AssertHasDiagnostic(ErrorsOf($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OOUV18.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CUV18.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 U USAGE OBJECT REFERENCE.
            01 G USAGE OBJECT REFERENCE CUV18.
            01 N4 PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                {{stmt}}
                STOP RUN.
            END PROGRAM OOUV18.

            IDENTIFICATION DIVISION.
            CLASS-ID. CUV18.
            END CLASS CUV18.
            """), "COBOLNET0868");
}
