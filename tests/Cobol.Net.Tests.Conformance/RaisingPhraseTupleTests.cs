// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB815 + PB814 — the procedure-division-header RAISING phrase (ISO §14.2.1) as the tuple it is printed
/// as, `{ exception-name-1 | [FACTORY OF] object-class-name-1 | interface-name-1 } …`, partitioned ONCE
/// (<c>RaisingPhrase.Partition</c>) for program and METHOD-ID headers, and consumed by the GOBACK §14.9.18.3 SR4 /
/// EXIT §14.9.14.3 SR5 identifier check. The end-to-end run (every alternative, both header arms, the
/// declaratives that catch each object) is the golden <c>2002/pb815_raising_factory_interface</c>; these cases pin
/// the REJECTIONS each syntax rule owns and the two arms the golden does not reach — EXIT PROGRAM and the
/// per-method header state.
/// </summary>
public sealed class RaisingPhraseTupleTests
{
    private static IReadOnlyList<string> ErrorsOf(string source, int edition = 2002)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok, "must be rejected");
        return errors;
    }

    private static void Compiles(string source, int edition = 2002)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>§14.2.2 SR9: "Interface-name-1 shall be the name of an interface specified in the REPOSITORY
    /// paragraph." The interface is DEFINED in the group but this program's REPOSITORY does not declare it, so
    /// it is not an interface-name in this position (§8.4.6.4) — 0858.</summary>
    [Fact]
    public void HeaderRaising_InterfaceNotInRepository_0858()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RPT1.
            PROCEDURE DIVISION RAISING IRPT1.
            MAIN.
                GOBACK.
            END PROGRAM RPT1.
            IDENTIFICATION DIVISION.
            INTERFACE-ID. IRPT1.
            END INTERFACE IRPT1.
            """), "COBOLNET0858");

    /// <summary>§14.2.1 prints `[ FACTORY OF ]` on the object-class-name-1 line only (§14.2.2 SR8 names that
    /// operand a class) — `RAISING FACTORY OF &lt;interface&gt;` is not a printed spelling.</summary>
    [Fact]
    public void HeaderRaising_FactoryOfInterface_0858()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RPT2.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                INTERFACE IRPT2.
            PROCEDURE DIVISION RAISING FACTORY OF IRPT2.
            MAIN.
                GOBACK.
            END PROGRAM RPT2.
            IDENTIFICATION DIVISION.
            INTERFACE-ID. IRPT2.
            END INTERFACE IRPT2.
            """), "COBOLNET0858");

    /// <summary>§14.2.2 SR8 on the FACTORY alternative, METHOD-ID arm: the class is not in the method's
    /// REPOSITORY scope — 0858 through the SAME partition the program arm uses.</summary>
    [Fact]
    public void MethodHeaderRaising_FactoryOfUndeclaredClass_0858()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. CRPT3.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            PROCEDURE DIVISION RAISING FACTORY OF NOSUCH3.
            MAIN.
                CONTINUE.
            END METHOD M1.
            END OBJECT.
            END CLASS CRPT3.
            """), "COBOLNET0858");

    private static string ExitProgramRaising(string pid, string header, string usage) => $$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {{pid}}.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS C{{pid}}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 F USAGE OBJECT REFERENCE {{usage}}.
            PROCEDURE DIVISION RAISING {{header}}.
            MAIN.
                SET F TO C{{pid}}.
                EXIT PROGRAM RAISING F.
            END PROGRAM {{pid}}.
            IDENTIFICATION DIVISION.
            CLASS-ID. C{{pid}} INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS C{{pid}}.
            """;

    /// <summary>§14.9.14.3 SR5 a), the EXIT PROGRAM arm of the shared check: the header's FACTORY phrase and
    /// identifier-1's agree (OF omitted on the header — it is an optional word).</summary>
    [Fact]
    public void ExitProgramRaising_FactoryOnBothEnds_Compiles()
        => Compiles(ExitProgramRaising("RPT4", "FACTORY CRPT4", "FACTORY OF CRPT4"));

    /// <summary>§14.9.14.3 SR5 a): "the presence or absence of the FACTORY phrase shall be the same" — the header
    /// lists the class WITH FACTORY, identifier-1 is an INSTANCE reference of it.</summary>
    [Fact]
    public void ExitProgramRaising_FactoryPresenceDiffers_4211()
    {
        var errors = ErrorsOf(ExitProgramRaising("RPT5", "FACTORY OF CRPT5", "CRPT5")
            .Replace("SET F TO CRPT5.", "INVOKE CRPT5 \"NEW\" RETURNING F."));
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET4211");
        Assert.Contains(errors, e => e.Contains("§14.9.14.3 SR5a", StringComparison.Ordinal));
    }

    /// <summary>A method IS a source element: each method's GOBACK is checked against ITS OWN header. The
    /// header state used to be loaded in the roster-registration loop, so both methods here were checked
    /// against M2's (empty) header and M1's legal `GOBACK RAISING EXCEPTION EC-USER-RPT6` drew COBOLNET0717
    /// (§14.9.18.3 SR2).</summary>
    [Fact]
    public void MethodRaising_EachMethodChecksItsOwnHeader()
        => Compiles("""
            IDENTIFICATION DIVISION.
            CLASS-ID. CRPT6.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            PROCEDURE DIVISION RAISING EC-USER-RPT6.
            MAIN.
                GOBACK RAISING EXCEPTION EC-USER-RPT6.
            END METHOD M1.
            METHOD-ID. M2.
            PROCEDURE DIVISION.
            MAIN.
                GOBACK.
            END METHOD M2.
            END OBJECT.
            END CLASS CRPT6.
            """);
}
