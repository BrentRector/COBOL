// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB759 — parameterized classes and interfaces (ISO §9.3.12 / §9.3.13) and their REPOSITORY expansion
/// (§12.3.8.2 EXPANDS; §12.3.8.4 GR5 / GR8), realized as a RE-PARSE of the definition's own tokens with each
/// formal replaced by its actual (<c>Oo/OoExpansion.cs</c>). The end-to-end run — an expansion's own factory
/// object, a formal in the INHERITS position, an interface expanded under one name in two source elements, and
/// an expansion whose actual is itself an expansion — is the golden <c>2002/pb759_parameterized_class</c>. These
/// cases pin every rejection the syntax and general rules own, the §12.3.8.4 GR1 confinement of the
/// parameterized name, and the one-report-per-fact rule that keeps a defect in the definition from being
/// reported once per expansion.
/// </summary>
public sealed class ParameterizedClassTests
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

    private const string Box = """
        IDENTIFICATION DIVISION.
        CLASS-ID. PCBOX.
        END CLASS PCBOX.
        IDENTIFICATION DIVISION.
        INTERFACE-ID. PCIFC.
        END INTERFACE PCIFC.

        """;

    private const string Holder = """
        IDENTIFICATION DIVISION.
        CLASS-ID. PCHOLD USING ELEM.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS ELEM.
        END CLASS PCHOLD.

        """;

    private static string Driver(string repository, string data = "") => $"""

        IDENTIFICATION DIVISION.
        PROGRAM-ID. PCDRV.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
        {repository}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {data}
        PROCEDURE DIVISION.
            STOP RUN.
        END PROGRAM PCDRV.
        """;

    /// <summary>§11.3.2 / §11.6.2 — the USING clause PARSES on both definitions; a skeleton that nothing expands
    /// compiles and emits nothing (§9.3.12: it is a "generic or skeleton class").</summary>
    [Fact]
    public void UnexpandedSkeletons_Compile()
        => Compiles(Holder + """
            IDENTIFICATION DIVISION.
            INTERFACE-ID. PCIHOLD USING T.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                INTERFACE T.
            END INTERFACE PCIHOLD.
            """ + Driver("CLASS PCHOLD"));

    /// <summary>§11.3.3 SR8: "Parameter-name-1 shall be a name specified in a class-specifier or an
    /// interface-specifier in the REPOSITORY paragraph of this class definition."</summary>
    [Fact]
    public void ClassParameterNotInRepository_2239()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. PCSR8 USING ELEM.
            END CLASS PCSR8.
            """), "COBOLNET2239");

    /// <summary>§11.3.3 SR9 / §11.6.3 SR7: "A given parameter-name shall not appear more than once in a USING
    /// clause."</summary>
    [Fact]
    public void RepeatedParameterName_2239()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            INTERFACE-ID. PCSR7 USING T T.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS T.
            END INTERFACE PCSR7.
            """), "more than once");

    /// <summary>§11.3.4 GR6: "Parameter-name-1 may be specified within this class definition only where an
    /// object-class-name or an interface-name is permitted." A data-name is not such a position — and since every
    /// occurrence is substituted, accepting it would silently rename the item in each expansion.</summary>
    [Fact]
    public void ParameterNameAsADataName_2239()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. PCGR6 USING ELEM.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS ELEM.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ELEM PIC X.
            END OBJECT.
            END CLASS PCGR6.
            """), "ISO §11.3.4 GR6");

    /// <summary>§12.3.8.3 SR3: "The EXPANDS phrase shall not be specified in the REPOSITORY paragraph of a class
    /// definition that contains a USING clause in its CLASS-ID paragraph".</summary>
    [Fact]
    public void ExpandsInsideParameterizedDefinition_2239()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(Box + """
            IDENTIFICATION DIVISION.
            CLASS-ID. PCSR3 USING ELEM.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS ELEM
                CLASS PCBOX
                CLASS PCSR3
                CLASS PCSR3B EXPANDS PCSR3 USING PCBOX.
            END CLASS PCSR3.
            """), "ISO §12.3.8.3 SR3");

    /// <summary>§12.3.8.4 GR5: "The number of parameters in the USING phrase of the EXPANDS phrase of the
    /// class-specifier shall be the same as the number of parameters in the USING clause of the CLASS-ID
    /// paragraph of object-class-name-2."</summary>
    [Fact]
    public void ParameterCountMismatch_2240()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(Box + Holder
            + Driver("CLASS PCBOX CLASS PCHOLD CLASS PCHB EXPANDS PCHOLD USING PCBOX PCBOX")), "ISO §12.3.8.4 GR5");

    /// <summary>§12.3.8.3 SR4: the parameterized name and each actual "shall be defined in the same REPOSITORY
    /// paragraph where object-class-name-1 is defined" — here the actual is not.</summary>
    [Fact]
    public void ActualNotDeclaredInSameParagraph_2240()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(Box + Holder
            + Driver("CLASS PCHOLD CLASS PCHB EXPANDS PCHOLD USING PCBOX")), "ISO §12.3.8.3 SR4");

    /// <summary>§12.3.8.4 GR5 creates a class "from the parameterized class object-class-name-2": a class with no
    /// USING clause has no formal to replace.</summary>
    [Fact]
    public void ExpandsANonParameterizedClass_2240()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(Box
            + Driver("CLASS PCBOX CLASS PCX EXPANDS PCBOX USING PCBOX")), "no formal parameter to replace");

    /// <summary>§12.3.8.4 GR8 — an INTERFACE specifier expands a parameterized INTERFACE; naming a parameterized
    /// class there creates nothing.</summary>
    [Fact]
    public void InterfaceSpecifierExpandsAClass_2240()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(Box + Holder
            + Driver("CLASS PCBOX CLASS PCHOLD INTERFACE PCIX EXPANDS PCHOLD USING PCBOX")), "ISO §12.3.8.4 GR8");

    /// <summary>The formal ELEM is declared by a class-specifier, so the GR5 substitution of an interface for it
    /// would write `CLASS PCIFC` — a class-specifier naming an interface.</summary>
    [Fact]
    public void ActualOfTheWrongKind_2240()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(Box + Holder
            + Driver("INTERFACE PCIFC CLASS PCHOLD CLASS PCHB EXPANDS PCHOLD USING PCIFC")), "is an interface but the formal");

    /// <summary>§9.3.12: expansions with different actual parameters "shall not have the same externalized
    /// object-class-name".</summary>
    [Fact]
    public void OneNameTwoExpansions_2240()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf(Box + Holder + """
            IDENTIFICATION DIVISION.
            CLASS-ID. PCBOX2.
            END CLASS PCBOX2.
            """ + Driver("CLASS PCBOX CLASS PCBOX2 CLASS PCHOLD "
                + "CLASS PCHB EXPANDS PCHOLD USING PCBOX CLASS PCHB EXPANDS PCHOLD USING PCBOX2")), "ISO §9.3.12");

    /// <summary>§9.3.12: two specifiers with the same name, definition and actuals are "the same class instance"
    /// — ONE class, so the program compiles (a second emitted type of the same name would be a Roslyn error).</summary>
    [Fact]
    public void OneNameOneExpansion_InTwoSourceElements_Compiles()
        => Compiles(Box + Holder + """
            IDENTIFICATION DIVISION.
            CLASS-ID. PCUSER.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS PCBOX
                CLASS PCHOLD
                CLASS PCHB EXPANDS PCHOLD USING PCBOX.
            END CLASS PCUSER.
            """ + Driver("CLASS PCBOX CLASS PCHOLD CLASS PCHB EXPANDS PCHOLD USING PCBOX",
                "01 R USAGE OBJECT REFERENCE PCHB."));

    /// <summary>§12.3.8.4 GR1: "If object-class-name-1 is a class described with the USING phrase,
    /// object-class-name-1 may be specified only in the REPOSITORY paragraph" — a typed reference to the
    /// skeleton itself is refused through the referring construct's own code, naming GR1.</summary>
    [Fact]
    public void SkeletonNameOutsideRepository_NamesGr1()
    {
        var errors = ErrorsOf(Box + Holder + Driver("CLASS PCHOLD", "01 R USAGE OBJECT REFERENCE PCHOLD."));
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0813");
        EditionHarness.AssertHasDiagnostic(errors, "ISO §12.3.8.4 GR1");
    }

    /// <summary>The expansion RE-BINDS the definition's own lines, so a defect in a line no formal touches is
    /// found once per expansion — and reported ONCE: one fact, one diagnostic.</summary>
    [Fact]
    public void DefectInTheSkeleton_ReportedOnce_NotOncePerExpansion()
    {
        var errors = ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. PCB1.
            END CLASS PCB1.
            IDENTIFICATION DIVISION.
            CLASS-ID. PCB2.
            END CLASS PCB2.
            IDENTIFICATION DIVISION.
            CLASS-ID. PCBAD USING ELEM.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS ELEM.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            PROCEDURE DIVISION.
                MOVE 1 TO PCNOSUCH.
            END METHOD M.
            END OBJECT.
            END CLASS PCBAD.
            """ + Driver("CLASS PCB1 CLASS PCB2 CLASS PCBAD "
                + "CLASS PCX1 EXPANDS PCBAD USING PCB1 CLASS PCX2 EXPANDS PCBAD USING PCB2"));
        Assert.Single(errors, e => e.Contains("PCNOSUCH", StringComparison.Ordinal));
    }

    /// <summary>Below the introducing edition the parameterized definition and the EXPANDS specifier draw the
    /// OO introduction diagnostic, not a parse error about a missing period (the pre-PB759 COBOL0307).</summary>
    [Fact]
    public void At85_OoIntroductionDiagnostic_NotAParseError()
    {
        var errors = ErrorsOf(Box + Holder
            + Driver("CLASS PCBOX CLASS PCHOLD CLASS PCHB EXPANDS PCHOLD USING PCBOX"), 85);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0900");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL0307");
    }
}
