// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1548 / PB1506 — the standard class BASE (ISO/IEC 1989:2023 §16). The end-to-end run over New and
/// FactoryObject through every receiver form (class-name, FACTORY OF reference, SELF in an inherited factory
/// method, a reference described BASE, a universal reference) is the golden <c>2002/pb1548_standard_class_base</c>;
/// the class-name refusal and the inline refusal are the negatives <c>pb1548-new-without-base</c> and
/// <c>pb1548-inline-new-active-class</c>. These cases pin the arms the goldens do not reach: New refused through
/// each OTHER §14.9.23.3 receiver rule, the REPOSITORY requirement, the §12.3.8.4 GR6 precedence of a group-defined
/// class named BASE, FactoryObject's override rules, and the runtime conformance of a universal New.
/// </summary>
public sealed class StandardClassBaseTests
{
    private static IReadOnlyList<string> ErrorsOf(string source, int edition = 2002)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok, "must be rejected");
        return errors;
    }

    private static (bool Ok, string Stdout, string Detail) CompileAndRun(string source, int edition = 2002)
    {
        string dir = CutRunner.NewTempDir("CobolNet_Base");
        try
        {
            string src = CompiledProgramCache.StageSource(Path.Combine(dir, "prog.cob"), source);
            string dll = Path.Combine(dir, "prog.dll");
            var r = CompiledProgramCache.Compile(new CobolNet.CompilerDriver.Options(src, dll, DialectLevel: edition, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "must compile strict: " + string.Join("\n", r.Errors));
            return CutRunner.Run(dll, dir);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>§14.9.23.3 SR4 f): SELF "New" in a factory method names a method of "the factory interface of the
    /// class containing the INVOKE statement" — which has no New unless the class inherits BASE.</summary>
    [Fact]
    public void SelfNew_InFactoryOfAClassWithoutBase_Is2448_Sr4f()
    {
        var errors = ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. SCB1.
            FACTORY.
            PROCEDURE DIVISION.
            METHOD-ID. MAKE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK USAGE OBJECT REFERENCE SCB1.
            PROCEDURE DIVISION RETURNING LK.
                INVOKE SELF "NEW" RETURNING LK.
            END METHOD MAKE.
            END FACTORY.
            END CLASS SCB1.
            """);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2448");
        EditionHarness.AssertHasDiagnostic(errors, "SR4 f)");
    }

    /// <summary>§14.9.23.3 SR4 h): SUPER "New" in a factory method names a method of "the factory interface of a
    /// class inherited by the class containing the INVOKE statement" — here a superclass that does not inherit
    /// BASE either.</summary>
    [Fact]
    public void SuperNew_WhenTheSuperclassLacksBase_Is2448_Sr4h()
    {
        var errors = ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. SCB2A.
            END CLASS SCB2A.

            IDENTIFICATION DIVISION.
            CLASS-ID. SCB2B INHERITS FROM SCB2A.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS SCB2A.
            FACTORY.
            PROCEDURE DIVISION.
            METHOD-ID. MAKE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK USAGE OBJECT REFERENCE SCB2B.
            PROCEDURE DIVISION RETURNING LK.
                INVOKE SUPER "NEW" RETURNING LK.
            END METHOD MAKE.
            END FACTORY.
            END CLASS SCB2B.
            """);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2448");
        EditionHarness.AssertHasDiagnostic(errors, "SR4 h)");
    }

    /// <summary>§14.9.23.3 SR4 a): through a reference described FACTORY OF a class, literal-1 names "a method
    /// contained in the factory interface of that object-class-name".</summary>
    [Fact]
    public void FactoryOfReferenceNew_OnAClassWithoutBase_Is2448_Sr4a()
    {
        var errors = ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCB3M.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS SCB3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 F USAGE OBJECT REFERENCE FACTORY OF SCB3.
            01 O USAGE OBJECT REFERENCE SCB3.
            PROCEDURE DIVISION.
                SET F TO SCB3
                INVOKE F "NEW" RETURNING O
                STOP RUN.
            END PROGRAM SCB3M.

            IDENTIFICATION DIVISION.
            CLASS-ID. SCB3.
            END CLASS SCB3.
            """);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2448");
        EditionHarness.AssertHasDiagnostic(errors, "SR4 a)");
    }

    /// <summary>§14.9.23.3 SR4 c): through a reference described FACTORY OF ACTIVE-CLASS, literal-1 names "a method
    /// contained in the factory interface of the class containing the INVOKE statement" — no New without BASE.</summary>
    [Fact]
    public void FactoryOfActiveClassNew_InAClassWithoutBase_Is2448_Sr4c()
    {
        var errors = ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. SCB3C.
            FACTORY.
            PROCEDURE DIVISION.
            METHOD-ID. MAKE.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 F USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
            LINKAGE SECTION.
            01 LK USAGE OBJECT REFERENCE ACTIVE-CLASS.
            PROCEDURE DIVISION RETURNING LK.
                SET F TO SELF
                INVOKE F "NEW" RETURNING LK.
            END METHOD MAKE.
            END FACTORY.
            END CLASS SCB3C.
            """);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2448");
        EditionHarness.AssertHasDiagnostic(errors, "SR4 c)");
    }

    /// <summary>§11.3.3 SR2 + §8.4.6.4: the standard class is reached like any other — INHERITS FROM BASE without
    /// `CLASS BASE` in the REPOSITORY is COBOLNET0821, and the message names the missing entry.</summary>
    [Fact]
    public void InheritsFromBase_WithoutTheRepositoryEntry_Is0821_NamingTheStandardClass()
    {
        var errors = ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. SCB4 INHERITS FROM BASE.
            END CLASS SCB4.
            """);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0821");
        EditionHarness.AssertHasDiagnostic(errors, "is the standard class BASE");
    }

    /// <summary>§12.3.8.4 GR6 (implementor-defined — OoStandardClasses): a class DEFINED in the compilation group
    /// under the name BASE is the one the group's specifiers mean. This one declares no New, so a subclass of it
    /// has none either — the standard class did not leak in behind it.</summary>
    [Fact]
    public void AGroupDefinedClassNamedBase_WinsOverTheStandardClass()
    {
        var errors = ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCB5M.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS SCB5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE SCB5.
            PROCEDURE DIVISION.
                INVOKE SCB5 "NEW" RETURNING O
                STOP RUN.
            END PROGRAM SCB5M.

            IDENTIFICATION DIVISION.
            CLASS-ID. BASE.
            END CLASS BASE.

            IDENTIFICATION DIVISION.
            CLASS-ID. SCB5 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS SCB5.
            """);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2448");
    }

    /// <summary>§16.2 gives both BASE methods a header with no USING phrase.</summary>
    [Fact]
    public void New_WithUsingArguments_Is0826()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCB6M.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS SCB6.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE SCB6.
            01 N PIC 9 VALUE 1.
            PROCEDURE DIVISION.
                INVOKE SCB6 "NEW" USING N RETURNING O
                STOP RUN.
            END PROGRAM SCB6M.

            IDENTIFICATION DIVISION.
            CLASS-ID. SCB6 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS SCB6.
            """), "COBOLNET0826");

    /// <summary>FactoryObject is an inherited instance method of every BASE subclass (§9.3.9), so redefining it
    /// without OVERRIDE is §11.7.3 SR4a's COBOLNET0837.</summary>
    [Fact]
    public void FactoryObject_RedefinedWithoutOverride_Is0837()
        => EditionHarness.AssertHasDiagnostic(ErrorsOf("""
            IDENTIFICATION DIVISION.
            CLASS-ID. SCB7 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. FactoryObject.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
            PROCEDURE DIVISION RETURNING LK.
                INVOKE SUPER "FactoryObject" RETURNING LK.
            END METHOD FactoryObject.
            END OBJECT.
            END CLASS SCB7.
            """), "COBOLNET0837");

    /// <summary>BaseInterface's FactoryObject is not FINAL, so a subclass may OVERRIDE it; SUPER reaches BASE's, and
    /// an invocation through a typed reference dispatches to the override. Through a UNIVERSAL factory reference,
    /// New creates the factory's class, and the result conforms to a receiver described with that class.</summary>
    [Fact]
    public void FactoryObject_Override_AndUniversalNew_IntoAConformingTypedReceiver()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCB8M.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS SCB8.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE SCB8.
            01 F USAGE OBJECT REFERENCE FACTORY OF SCB8.
            01 U USAGE OBJECT REFERENCE.
            PROCEDURE DIVISION.
                INVOKE SCB8 "NEW" RETURNING O
                INVOKE O "FactoryObject" RETURNING F
                INVOKE F "WHO"
                SET U TO F
                INVOKE U "NEW" RETURNING O
                INVOKE O "FactoryObject" RETURNING F
                STOP RUN.
            END PROGRAM SCB8M.

            IDENTIFICATION DIVISION.
            CLASS-ID. SCB8 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            FACTORY.
            PROCEDURE DIVISION.
            METHOD-ID. WHO.
            PROCEDURE DIVISION.
                DISPLAY "SCB8-FACTORY".
            END METHOD WHO.
            END FACTORY.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. FactoryObject OVERRIDE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
            PROCEDURE DIVISION RETURNING LK.
                DISPLAY "OVERRIDDEN"
                INVOKE SUPER "FactoryObject" RETURNING LK.
            END METHOD FactoryObject.
            END OBJECT.
            END CLASS SCB8.
            """);
        Assert.True(ok, detail);
        Assert.Equal("OVERRIDDEN\nSCB8-FACTORY\nOVERRIDDEN", CutRunner.Normalize(stdout));
    }

    /// <summary>§14.9.23.4 GR7c through a universal receiver: New creates an object of the FACTORY's class, and a
    /// typed receiving item of an unrelated class does not conform — a runtime stop naming both classes, never an
    /// unchecked cast (CobolObject.NarrowUniversal).</summary>
    [Fact]
    public void UniversalNew_IntoANonConformingTypedReceiver_StopsAtRunTime()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCB9M.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS SCB9A
                CLASS SCB9B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 U USAGE OBJECT REFERENCE.
            01 B USAGE OBJECT REFERENCE SCB9B.
            PROCEDURE DIVISION.
                SET U TO SCB9A
                DISPLAY "BEFORE"
                INVOKE U "NEW" RETURNING B
                DISPLAY "NOT REACHED"
                STOP RUN.
            END PROGRAM SCB9M.

            IDENTIFICATION DIVISION.
            CLASS-ID. SCB9A INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS SCB9A.

            IDENTIFICATION DIVISION.
            CLASS-ID. SCB9B INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS SCB9B.
            """);
        Assert.False(ok, "the non-conforming delivery must stop the run unit");
        Assert.Equal("BEFORE", CutRunner.Normalize(stdout));
        Assert.Contains("does not conform", detail);
    }
}
