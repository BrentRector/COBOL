// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB757 — the OMITTED argument and the OPTIONAL formal on the METHOD arm of the omitted-argument model
/// (the method ABI's <c>(ref T, bool omitted)</c> pair — COBOLNET_OO_DESIGN D6) and on the user-defined-function
/// arm. The end-to-end run over both INVOKE spellings, trailing omission, GR1c forwarding across the method/program
/// boundary in both directions, a universal receiver and a FUNCTION is the golden
/// <c>2002/pb757_invoke_omitted</c>; these cases pin the arms it does not reach — the 2023 inline form, forwarding
/// through a universal receiver — and the rejections each rule owns.
/// </summary>
public sealed class MethodOmittedArgumentTests
{
    private static IReadOnlyList<string> ErrorsOf(string source, int edition = 2002)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok, "must be rejected");
        return errors;
    }

    private static string Run(string source, int edition)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(source, edition);
        Assert.True(ok, detail);
        return stdout.Replace("\r\n", "\n").TrimEnd();
    }

    private const string TakeClass = """
            IDENTIFICATION DIVISION.
            CLASS-ID. {0} INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. CHK.
            DATA DIVISION.
            LINKAGE SECTION.
            01 X PIC X(4).
            01 R PIC X.
            PROCEDURE DIVISION USING OPTIONAL X RETURNING R.
                IF X IS OMITTED MOVE "O" TO R ELSE MOVE "G" TO R END-IF.
            END METHOD CHK.
            METHOD-ID. FWDU.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 U USAGE OBJECT REFERENCE.
            01 C PIC X.
            LINKAGE SECTION.
            01 F PIC X(4).
            PROCEDURE DIVISION USING OPTIONAL F.
                SET U TO SELF.
                INVOKE U "CHK" USING F RETURNING C.
                DISPLAY "FWDU=" C.
            END METHOD FWDU.
            END OBJECT.
            END CLASS {0}.
            """;

    /// <summary>§8.4.3.4.2 prints OMITTED in the inline form's argument brace, and §8.4.3.4.4 GR1 makes those the
    /// arguments of the equivalent INVOKE — so §14.9.23.4 GR9 holds: the condition is true in the method.</summary>
    [Fact]
    public void InlineInvocation_Omitted_ConditionTrueInMethod()
        => Assert.Equal("O=O\nG=G", Run($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOA1.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CMOA1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE CMOA1.
            01 W PIC X(4) VALUE "ABCD".
            01 C PIC X.
            PROCEDURE DIVISION.
                INVOKE CMOA1 "NEW" RETURNING O.
                MOVE O :: "CHK" (OMITTED) TO C.
                DISPLAY "O=" C.
                MOVE O :: "CHK" (W) TO C.
                DISPLAY "G=" C.
                STOP RUN.
            END PROGRAM MOA1.
            {{string.Format(TakeClass, "CMOA1")}}
            """, 2023));

    /// <summary>§8.4.3.4.4 GR1 c) through a UNIVERSAL receiver: the forwarded formal's presence rides the
    /// <c>CobolInvokeArg</c> box (§14.9.23.4 GR7c's runtime path), so omission stays transitive.</summary>
    [Fact]
    public void UniversalForward_OmittedFormal_StaysOmitted()
        => Assert.Equal("FWDU=O\nFWDU=G", Run($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOA2.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CMOA2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE CMOA2.
            01 W PIC X(4) VALUE "ABCD".
            PROCEDURE DIVISION.
                INVOKE CMOA2 "NEW" RETURNING O.
                INVOKE O "FWDU" USING OMITTED.
                INVOKE O "FWDU" USING W.
                STOP RUN.
            END PROGRAM MOA2.
            {{string.Format(TakeClass, "CMOA2")}}
            """, 2002));

    /// <summary>§14.8.2.1: only TRAILING formals "specified with an OPTIONAL phrase" may be omitted from the
    /// argument list — B is not OPTIONAL, so one argument for two formals is an arity violation (0828).</summary>
    [Fact]
    public void TrailingOmission_OfNonOptionalFormal_0828()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOA3.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CMOA3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE CMOA3.
            01 N PIC 9(3).
            PROCEDURE DIVISION.
                INVOKE CMOA3 "NEW" RETURNING O.
                INVOKE O "TWO" USING N.
                STOP RUN.
            END PROGRAM MOA3.
            IDENTIFICATION DIVISION.
            CLASS-ID. CMOA3 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. TWO.
            DATA DIVISION.
            LINKAGE SECTION.
            01 A PIC 9(3).
            01 B PIC X(4).
            PROCEDURE DIVISION USING A B.
                CONTINUE.
            END METHOD TWO.
            END OBJECT.
            END CLASS CMOA3.
            """), "is not OPTIONAL");

    /// <summary>§9.3.8.2.3 rule 8: "The presence or absence of the OPTIONAL phrase is the same for corresponding
    /// parameters" — an IMPLEMENTS whose method drops the prototype's OPTIONAL does not conform (0841).</summary>
    [Fact]
    public void Implements_OptionalPresenceDiffers_0841()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            INTERFACE-ID. IMOA4.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            DATA DIVISION.
            LINKAGE SECTION.
            01 X PIC X(4).
            PROCEDURE DIVISION USING OPTIONAL X.
            END METHOD M1.
            END INTERFACE IMOA4.
            IDENTIFICATION DIVISION.
            CLASS-ID. CMOA4.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                INTERFACE IMOA4.
            IDENTIFICATION DIVISION.
            OBJECT. IMPLEMENTS IMOA4.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            DATA DIVISION.
            LINKAGE SECTION.
            01 X PIC X(4).
            PROCEDURE DIVISION USING X.
                CONTINUE.
            END METHOD M1.
            END OBJECT.
            END CLASS CMOA4.
            """), "rule 8");

    /// <summary>§11.7.3 SR9 holds an overriding method's parameter declarations to §9.3.8.2.3 — rule 8 included
    /// (0829).</summary>
    [Fact]
    public void Override_OptionalPresenceDiffers_0829()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. BMOA5.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. OV.
            DATA DIVISION.
            LINKAGE SECTION.
            01 X PIC X(4).
            PROCEDURE DIVISION USING X.
                CONTINUE.
            END METHOD OV.
            END OBJECT.
            END CLASS BMOA5.
            IDENTIFICATION DIVISION.
            CLASS-ID. CMOA5 INHERITS BMOA5.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BMOA5.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. OV OVERRIDE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 X PIC X(4).
            PROCEDURE DIVISION USING OPTIONAL X.
                CONTINUE.
            END METHOD OV.
            END OBJECT.
            END CLASS CMOA5.
            """), "§9.3.8.2.3 rule 8");

    /// <summary>§9.3.6 match rule 3 b) through a universal receiver: a spelled OMITTED needs an OPTIONAL formal, so a
    /// method whose formal is not OPTIONAL does not MATCH the invocation and is never bound; with no matching method
    /// in the hierarchy, §9.3.6 6) sets EC-OO-METHOD (§14.9.23.4 GR7 b); kb/Work PB1500) — the run unit stops before
    /// the method runs.</summary>
    [Fact]
    public void Universal_OmittedIntoNonOptionalFormal_StopsAtRuntime()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOA6.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CMOA6.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 U USAGE OBJECT REFERENCE.
            PROCEDURE DIVISION.
                INVOKE CMOA6 "NEW" RETURNING U.
                DISPLAY "BEFORE".
                INVOKE U "REQ" USING OMITTED.
                DISPLAY "AFTER".
                STOP RUN.
            END PROGRAM MOA6.
            IDENTIFICATION DIVISION.
            CLASS-ID. CMOA6 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. REQ.
            DATA DIVISION.
            LINKAGE SECTION.
            01 X PIC X(4).
            PROCEDURE DIVISION USING X.
                DISPLAY "IN REQ".
            END METHOD REQ.
            END OBJECT.
            END CLASS CMOA6.
            """, 2002);
        string all = stdout + detail;
        Assert.Contains("BEFORE", all);
        Assert.DoesNotContain("IN REQ", all);
        Assert.DoesNotContain("AFTER", all);
        Assert.Contains("EC-OO-METHOD", all);
    }

    /// <summary>§8.8.4.8.3 SR1 inside a METHOD: data-name-1 shall be a formal parameter of THAT source element —
    /// a method LINKAGE item that is not in its USING phrase is not one (1686).</summary>
    [Fact]
    public void OmittedCondition_OnMethodNonFormal_1686()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. CMOA7.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC X.
            LINKAGE SECTION.
            01 X PIC X(4).
            PROCEDURE DIVISION USING OPTIONAL X.
                IF W IS OMITTED DISPLAY "?" END-IF.
            END METHOD M.
            END OBJECT.
            END CLASS CMOA7.
            """), "COBOLNET1686");

    /// <summary>§14.8.2.1 on the FUNCTION arm: a trailing argument may be omitted only when its formal is
    /// OPTIONAL — B is not (1506).</summary>
    [Fact]
    public void Function_TrailingOmissionOfNonOptional_1506()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            FUNCTION-ID. FMOA8.
            DATA DIVISION.
            LINKAGE SECTION.
            01 A PIC X(4).
            01 B PIC X(4).
            01 R PIC X.
            PROCEDURE DIVISION USING OPTIONAL A B RETURNING R.
                MOVE "P" TO R.
                GOBACK.
            END FUNCTION FMOA8.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOA8.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION FMOA8.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC X(4).
            PROCEDURE DIVISION.
                DISPLAY FUNCTION FMOA8(W).
                STOP RUN.
            END PROGRAM MOA8.
            """), "COBOLNET1506");
}
