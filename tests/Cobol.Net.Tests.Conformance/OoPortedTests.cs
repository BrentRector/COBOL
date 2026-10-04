// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The 14 legacy OoTests regression facts re-landed as greenfield facts (PHASE-09 Step 9; exit criterion 7) —
/// each asserts a REAL caught legacy bug; the legacy OoTests.cs stays frozen as the oracle until G8/P15.
/// </summary>
public sealed class OoPortedTests
{
    // ── The 14-fact accounting (ported + skipped = 14) ──────────────────────────────────────────────────────
    //
    // PORTED here (4):
    //   Invoke_UnsupportedArgForm_FailsLoudly      -> Ported_Invoke_UnsupportedArgForm_FailsLoudly
    //   Inherits_SubclassMethodLinkageIndex_Allowed -> Ported_Inherits_SubclassMethodLinkageIndex_Allowed
    //   Method_WithSection_FailsLoudly             -> Ported_Method_WithSection_FailsLoudly
    //   MultiMethod_WithParams_FailsLoudly         -> Ported_MultiMethod_WithParams_FailsLoudly
    //
    // SKIPPED as already asserted by an ENABLED greenfield artifact (10):
    //   Invoke_ByReferenceArg_Works                -> corpus golden oo_method_args (the same ACC/ADDTO
    //       USING…RETURNING BY REFERENCE pattern, per-instance BAL, byte-compared by CorpusRunnerTests)
    //   TypedObjectData_IsPerInstance              -> corpus goldens oo_instance_data (the SAME OOINST/BOX
    //       ---/---/XYZ program, verbatim) + oo_object_group (trap #1 — see the OoSpineTests header)
    //   Inherits_PolymorphicOverride_DispatchesToSubclass -> corpus golden oo_inherit (ANIMAL/DOG virtual
    //       dispatch through a base-typed reference); the legacy source's missing OVERRIDE attribute is
    //       itself asserted by OoSpineTests.Sr4a_RedefinitionWithoutOverride_0837Strict_InferredPermissive
    //   Invoke_Super_CallsBaseMethod               -> corpus golden oo_super (the same ANIMAL/DOG chain,
    //       "ANIMAL\nDOG") + OoSpineTests.ThreeLevelSuperChain_RunsBaseFirst_NoRecursion
    //   Invoke_Self_MultiMethodClass_SharesStateAndDispatches -> corpus golden oo_self (the SAME
    //       OOSELF/COUNTER DRIVE/BUMP N=2 program, verbatim)
    //   Invoke_MultiMethod_FirstMethodDoesNotFallIntoSecond -> OoSpineTests.Trap4_MethodFallThrough_
    //       DoesNotEnterSiblingMethod (its doc comment names this legacy guard)
    //   Invoke_SuperInRootClass_FailsLoudly        -> OoSpineTests.InheritanceDiagnostics_0829_0827_0820
    //       (SUPER in a root class is the clean COBOLNET0827; the legacy code was COBOL0115)
    //   Inherits_SubclassOwnData_FailsLoudly       -> SUPERSEDED: subclass-own OBJECT data is fully
    //       SUPPORTED (the legacy COBOL0113 staging reject is retired); the positive guard is
    //       OoSpineTests.SubclassOwnObjectData_IndependentPerInstance
    //   Inherits_CaseMismatchedOverride_StillOverrides -> OoSpineTests.Trap2_CaseMismatchedOverride_
    //       StillDispatches (COBOL names are case-insensitive — one C# slot, §8.3.2.2)
    //   Inherits_UnknownBase_FailsLoudly           -> OoSpineTests.InheritsUnknownBase_0821_KnownBaseCompiles
    //       (COBOLNET0821; the legacy code was COBOL0114)
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────

    private static (bool Ok, string Stdout, string Detail) CompileAndRun(string source, int edition = 2002)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_OoP9_" + Guid.NewGuid().ToString("N")[..8]);
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

    // ── Ported #1: the INVOKE USING argument forms (legacy Invoke_UnsupportedArgForm_FailsLoudly) ──────────

    /// <summary>The legacy ACC accumulator class (verbatim except the CLASS-ID rename): ADDTO adds its USING
    /// argument into per-instance BAL and delivers BAL through RETURNING.</summary>
    private static string AccClass(string cls) => $@"
       IDENTIFICATION DIVISION.
       CLASS-ID. {cls} INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. ADDTO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-AMT PIC 9(4).
       01 LK-RES PIC 9(4).
       PROCEDURE DIVISION USING LK-AMT RETURNING LK-RES.
       MAIN.
           ADD LK-AMT TO BAL.
           MOVE BAL TO LK-RES.
       END METHOD ADDTO.
       END OBJECT.
       END CLASS {cls}.
";

    /// <summary>The legacy OODRV driver (verbatim except the PROGRAM-ID/CLASS-ID renames) with the INVOKE
    /// USING form under test spliced in.</summary>
    private static string ArgFormDriver(string pid, string cls, string invokeUsing) => $@"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {pid}.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS {cls}.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE {cls}.
       01 AMT PIC 9(4) VALUE 7.
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE {cls} ""NEW"" RETURNING A.
           {invokeUsing}
           DISPLAY ""R="" R.
           STOP RUN.
       END PROGRAM {pid}.
" + AccClass(cls);

    /// <summary>Legacy bug (adversarial review, DEVLOG 448): each grammar-legal-but-then-unsupported USING
    /// form had to be rejected loudly (COBOL0111), never SILENTLY DROPPED — a silent drop shifts the trailing
    /// RETURNING slot into an argument position (wrong binding / runtime crash). Greenfield: the bare-literal
    /// and BY CONTENT forms are now SUPPORTED (§14.8.2.3.3 COMPUTE-rule conversion — see
    /// OoSpineTests.ByContentNumeric_ComputeRuleConversion), so the no-silent-drop contract is proven by the
    /// CORRECT delivered value instead; the BY VALUE form (no BY VALUE formals yet — §14.9.23.3 SR5b) stays a
    /// loud compile-time COBOLNET0828, never a drop.</summary>
    [Fact]
    public void Ported_Invoke_UnsupportedArgForm_FailsLoudly()
    {
        // Legacy InlineData 1 — bare literal: delivered by CONTENT/COMPUTE rules; BAL = 0 + 10.
        var (okLit, outLit, detailLit) = CompileAndRun(
            ArgFormDriver("OODRVP9A", "ACCP9A", @"INVOKE A ""ADDTO"" USING 10 RETURNING R."));
        Assert.True(okLit, detailLit);
        Assert.Equal("R=0010", CutRunner.Normalize(outLit));

        // Legacy InlineData 2 — BY CONTENT: the caller's AMT (7) crosses by value-copy; BAL = 0 + 7.
        var (okCont, outCont, detailCont) = CompileAndRun(
            ArgFormDriver("OODRVP9B", "ACCP9B", @"INVOKE A ""ADDTO"" USING BY CONTENT AMT RETURNING R."));
        Assert.True(okCont, detailCont);
        Assert.Equal("R=0007", CutRunner.Normalize(outCont));

        // Legacy InlineData 3 — BY VALUE: every formal is BY REFERENCE today, so this is the loud
        // compile-time 0828 (ISO §14.9.23.3 SR5b) — a clean reject, never a silent miscompile.
        var (okVal, errsVal, _) = EditionHarness.CompileFull(
            ArgFormDriver("OODRVP9C", "ACCP9C", @"INVOKE A ""ADDTO"" USING BY VALUE AMT RETURNING R."), 2002);
        Assert.False(okVal, "a BY VALUE argument against a BY REFERENCE formal must be rejected loudly, "
            + "never silently dropped");
        EditionHarness.AssertHasDiagnostic(errsVal, "COBOLNET0828");
    }

    // ── Ported #2: subclass method LINKAGE with OCCURS … INDEXED BY ─────────────────────────────────────────

    /// <summary>Legacy bug (adversarial review): a subclass method whose LINKAGE has an OCCURS … INDEXED BY
    /// (the index-name is synthetically placed in the WS area) is NOT subclass OBJECT data — it must compile,
    /// not trip the legacy subclass-own-data reject (COBOL0113). Greenfield: subclass-own data is fully
    /// supported (no 0113 analog exists), so this port asserts the combination compiles AND runs.
    /// PORTED-VERBATIM NOTE: the driver's <c>USING T</c> (PIC 9, one character) does not conform to the
    /// method's 3-character group formal LK under strict §14.8.2.3.2 — the legacy's laxer conformance
    /// accepted it; the run adjudicates (OoConformance.DescriptionMismatch may 0828 it).</summary>
    [Fact]
    public void Ported_Inherits_SubclassMethodLinkageIndex_Allowed()
    {
        string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOIDXP9.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CLBP9.
           CLASS CLSP9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CLSP9.
       01 T PIC X(3) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CLSP9 ""NEW"" RETURNING O.
           INVOKE O ""USE-TBL"" USING T.
           STOP RUN.
       END PROGRAM OOIDXP9.
       IDENTIFICATION DIVISION.
       CLASS-ID. CLBP9 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PLACEHOLDER.
       PROCEDURE DIVISION.
       P.
           CONTINUE.
       END METHOD PLACEHOLDER.
       END OBJECT.
       END CLASS CLBP9.
       IDENTIFICATION DIVISION.
       CLASS-ID. CLSP9 INHERITS FROM CLBP9.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CLBP9.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. USE-TBL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK.
          05 ELEM PIC 9 OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION USING LK.
       P.
           CONTINUE.
       END METHOD USE-TBL.
       END OBJECT.
       END CLASS CLSP9.
";
        var (ok, stdout, detail) = CompileAndRun(src);
        Assert.True(ok, detail);   // must NOT false-fire a subclass-data (or any other) reject
        // (Port adjudication: the legacy passed a nonconforming PIC 9 argument its lax checker accepted; the
        //  greenfield's SPEC-CORRECT §14.8.2.3.2 strict check rejects that, so the argument here is the
        //  conforming X(3) — the guarded bug [a LINKAGE INDEXED BY table inside a subclass method] is intact.)
        Assert.Equal("", CutRunner.Normalize(stdout));
    }

    // ── Ported #3: a SECTION inside a METHOD-ID (legacy Method_WithSection_FailsLoudly) ─────────────────────

    /// <summary>Legacy bug (DEVLOG 455): a SECTION inside a METHOD-ID was not method-scoped — its paragraphs
    /// fell OUTSIDE the method's dispatch range and were SILENTLY SKIPPED, so the legacy rejected loudly
    /// (COBOL0116) rather than emit wrong output. Greenfield: a section inside a method is a method-local pc
    /// range (StatementBinder — "the legacy COBOL0116 reject is superseded"), so the correct assertion is now
    /// the POSITIVE one: MAIN-SEC's paragraph MAIN falls through into DRIVE-SEC's paragraph and both DISPLAYs run.
    /// (MAIN sits in a section of its own: §14.4.1 — once one paragraph is in a section all shall be, kb/Work PB1146.)</summary>
    [Fact]
    public void Ported_Method_WithSection_FailsLoudly()
    {
        const string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOSECP9.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CLKP9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CLKP9.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CLKP9 ""NEW"" RETURNING O.
           INVOKE O ""DRIVE"".
           STOP RUN.
       END PROGRAM OOSECP9.
       IDENTIFICATION DIVISION.
       CLASS-ID. CLKP9 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. DRIVE.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN.
           DISPLAY ""DRIVE-MAIN"".
       DRIVE-SEC SECTION.
       DRIVE-P1.
           DISPLAY ""DRIVE-P1"".
       END METHOD DRIVE.
       END OBJECT.
       END CLASS CLKP9.
";
        var (ok, stdout, detail) = CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("DRIVE-MAIN\nDRIVE-P1", CutRunner.Normalize(stdout));
    }

    // ── Ported #4: multiple methods where one has USING/RETURNING (legacy MultiMethod_WithParams) ──────────

    /// <summary>Legacy bug (DEVLOG 455): a class with MULTIPLE methods where any had USING/RETURNING was a
    /// later slice — the legacy rejected loudly (COBOL0117) rather than crash at run time with CROSS-WIRED
    /// per-method param buffers. Greenfield: per-method LINKAGE is structural (the cross-wiring is
    /// impossible — OoSpineTests.Trap6_SiblingMethodLinkage_NoCrossWiring), so the correct assertion is now
    /// the POSITIVE one: the two-method class with USING+RETURNING on both compiles and runs cleanly (the
    /// legacy driver DISPLAYs nothing — the guarded property is a clean compile+run, no crash).</summary>
    [Fact]
    public void Ported_MultiMethod_WithParams_FailsLoudly()
    {
        const string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOMPP9.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CALCP9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C USAGE OBJECT REFERENCE CALCP9.
       01 X PIC 9(4) VALUE 5.
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CALCP9 ""NEW"" RETURNING C.
           INVOKE C ""ADDONE"" USING X RETURNING R.
           STOP RUN.
       END PROGRAM OOMPP9.
       IDENTIFICATION DIVISION.
       CLASS-ID. CALCP9 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. ADDONE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-IN PIC 9(4).
       01 LK-OUT PIC 9(4).
       PROCEDURE DIVISION USING LK-IN RETURNING LK-OUT.
       MAIN.
           COMPUTE LK-OUT = LK-IN + 1.
       END METHOD ADDONE.
       METHOD-ID. TRIPLEV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-T PIC 9(4).
       01 LK-TR PIC 9(4).
       PROCEDURE DIVISION USING LK-T RETURNING LK-TR.
       MAIN.
           COMPUTE LK-TR = LK-T * 3.
       END METHOD TRIPLEV.
       END OBJECT.
       END CLASS CALCP9.
";
        var (ok, stdout, detail) = CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("", CutRunner.Normalize(stdout));
    }
}
