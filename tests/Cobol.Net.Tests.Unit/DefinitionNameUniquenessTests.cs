// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE NAME, ONE DEFINITION — AND THE COMPLEMENT, WHICH IS WHERE THE DAMAGE FROM OVER-CHECKING WOULD BE
/// (kb/Work PB660).
/// </summary>
/// <remarks>
/// <para>
/// ISO §8.3.2.2 states the group-wide rule twice over one population — "Within a run unit, all instances of a
/// given name that is externalized to the operating environment shall identify the same kind of entity or item.
/// Except for method-names and property-names, when two or more source elements identify something with the same
/// externalized name, they refer to the same instance" — and §8.4.6.3 states the contained-scope rule once: "The
/// names assigned to programs that are contained directly or indirectly within the same outermost program shall
/// be unique within that outermost program."  NEITHER was enforced for programs. Measured on the tree this test
/// landed against: two outermost <c>PROGRAM-ID. P3DND.</c> definitions ran the SECOND, the same pair under one
/// <c>AS</c> literal ran the FIRST, two same-named contained programs ran the first, and a program and a function
/// under one name both registered and both ran.
/// </para>
/// <para>
/// The ACCEPT half is not decoration. Each of its three shapes is legal source that a coarser check — "no two
/// program-names alike in a compilation group" — would reject: §8.4.6.3's scope is ONE outermost program, a
/// containee's name is not externalized at all, and what §8.3.2.2 makes unique is the EXTERNALIZED name, which
/// the AS phrase decouples from the declared word. A rule that only ever gets tested on its refusals cannot tell
/// anyone it has grown too wide.
/// </para>
/// <para>
/// PROVEN TO FAIL before being trusted: deleting either loop of
/// <c>BinderDriver.CheckDefinitionNameUniqueness</c> turns its own REJECT cases red, and widening loop (2) from
/// the outermost program's subtree to the whole group turns <c>TwoContainersMayEachContainOneName</c> red.
/// </para>
/// </remarks>
public sealed class DefinitionNameUniquenessTests : CobolNetTestBase
{
    private string CompileOnly(string source, int dialectLevel = 2002)
    {
        string srcPath = Path.Combine(TempDir, "dnu.cob");
        File.WriteAllText(srcPath, source);
        var result = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, Path.Combine(TempDir, "dnu.dll"), DialectLevel: dialectLevel, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
        return result.Success ? "" : string.Join("\n", result.Errors);
    }

    // ── REJECT ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>§8.3.2.2, the SAME-KIND sentence: two outermost program definitions under one externalized
    /// name. Both spellings — the declared word (no AS phrase maps it to itself) and two AS literals that
    /// coincide — are ONE fault, because the rule is about the externalized name in both cases.</summary>
    [Theory]
    [InlineData("PROGRAM-ID. DNUA.", "PROGRAM-ID. DNUA.")]
    [InlineData("PROGRAM-ID. DNUA AS \"DNUX\".", "PROGRAM-ID. DNUB AS \"DNUX\".")]
    [InlineData("PROGRAM-ID. DNUX.", "PROGRAM-ID. DNUB AS \"DNUX\".")]
    public void TwoOutermostProgramsMayNotShareAnExternalizedName(string first, string second)
    {
        string errors = CompileOnly($"""
                   IDENTIFICATION DIVISION.
                   {first}
                   PROCEDURE DIVISION.
                   P1. GOBACK.
                   IDENTIFICATION DIVISION.
                   {second}
                   PROCEDURE DIVISION.
                   P2. GOBACK.

                   """);
        Assert.Contains("COBOLNET2213", errors);
    }

    /// <summary>§8.3.2.2, the CROSS-KIND sentence: a program definition and a function definition are not the
    /// same kind of entity, and its list item 1 puts both in the externalized population.</summary>
    [Fact]
    public void AProgramAndAFunctionMayNotShareAnExternalizedName()
    {
        string errors = CompileOnly("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUSAME.
                   PROCEDURE DIVISION.
                   P1. GOBACK.
                   END PROGRAM DNUSAME.
                   IDENTIFICATION DIVISION.
                   FUNCTION-ID. DNUSAME.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01 R PIC 9(4).
                   PROCEDURE DIVISION RETURNING R.
                   F1. MOVE 1 TO R. GOBACK.
                   END FUNCTION DNUSAME.

                   """);
        Assert.Contains("COBOLNET2213", errors);
    }

    /// <summary>§8.4.6.3: two programs contained within ONE outermost program. A DIFFERENT code from the
    /// group-wide one, because a containee's name is not externalized and its scope is its container.</summary>
    [Fact]
    public void TwoProgramsInOneContainerMayNotShareAName()
    {
        string errors = CompileOnly("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUCTM.
                   PROCEDURE DIVISION.
                   M1. GOBACK.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUCTD.
                   PROCEDURE DIVISION.
                   C1. GOBACK.
                   END PROGRAM DNUCTD.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUCTD.
                   PROCEDURE DIVISION.
                   C2. GOBACK.
                   END PROGRAM DNUCTD.
                   END PROGRAM DNUCTM.

                   """);
        Assert.Contains("COBOLNET2214", errors);
    }

    /// <summary>The SIBLING SWEEP arm: §8.3.2.2's list item 1 puts object-class-names in the SAME externalized
    /// population as program-names, so a CLASS-ID and an outermost PROGRAM-ID under one name are two kinds under
    /// one name. Before kb/Work PB660 each namespace policed only itself — class-vs-class by COBOLNET0820 under
    /// §8.4.6.4, function-vs-function by COBOLNET1508, program-vs-program by nothing — and nothing ever compared
    /// a definition of one kind against a definition of another.</summary>
    [Fact]
    public void AClassAndAProgramMayNotShareAnExternalizedName()
    {
        string errors = CompileOnly("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUBOTH.
                   PROCEDURE DIVISION.
                   P1. GOBACK.
                   END PROGRAM DNUBOTH.
                   IDENTIFICATION DIVISION.
                   CLASS-ID. DNUBOTH.
                   END CLASS DNUBOTH.

                   """);
        Assert.Contains("COBOLNET2213", errors);
    }

    /// <summary>§8.4.6.4 owns class-name and interface-name uniqueness and the OO class table enforces it on the
    /// declared WORD (COBOLNET0820 / COBOLNET0840). The group-wide externalized check must not RESTATE that
    /// sentence — one fault, one diagnostic — so a duplicate CLASS-ID draws 0820 and NOT 2213.</summary>
    [Fact]
    public void ADuplicateClassNameStaysTheOoTablesDiagnostic()
    {
        string errors = CompileOnly("""
                   IDENTIFICATION DIVISION.
                   CLASS-ID. DNUCLS.
                   END CLASS DNUCLS.
                   IDENTIFICATION DIVISION.
                   CLASS-ID. DNUCLS.
                   END CLASS DNUCLS.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUCLSM.
                   PROCEDURE DIVISION.
                   M1. GOBACK.
                   END PROGRAM DNUCLSM.

                   """);
        Assert.Contains("COBOLNET0820", errors);
        Assert.DoesNotContain("COBOLNET2213", errors);
    }

    // ── ACCEPT (the complement — legal source a wider check would refuse) ──────────────────────────────────

    /// <summary>§8.4.6.3's scope is ONE outermost program: two different containers may each contain a program
    /// of the same name, and rule 1 of the same clause is what keeps the two references apart.</summary>
    [Fact]
    public void TwoContainersMayEachContainOneName()
    {
        var (ok, stdout, detail) = CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUPM.
                   PROCEDURE DIVISION.
                   M1. CALL "DNUPA" CALL "DNUPB" STOP RUN.
                   END PROGRAM DNUPM.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUPA.
                   PROCEDURE DIVISION.
                   A1. CALL "DNUIN" AS NESTED GOBACK.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUIN.
                   PROCEDURE DIVISION.
                   A2. DISPLAY "A-INNER" GOBACK.
                   END PROGRAM DNUIN.
                   END PROGRAM DNUPA.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUPB.
                   PROCEDURE DIVISION.
                   B1. CALL "DNUIN" AS NESTED GOBACK.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUIN.
                   PROCEDURE DIVISION.
                   B2. DISPLAY "B-INNER" GOBACK.
                   END PROGRAM DNUIN.
                   END PROGRAM DNUPB.

                   """, dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("A-INNER\r\nB-INNER", stdout);
    }

    /// <summary>§8.3.2.2 makes the EXTERNALIZED name unique, and the AS phrase decouples it from the declared
    /// word — so two outermost programs may share the word while externalizing different names, and both stay
    /// callable by their own literal.</summary>
    [Fact]
    public void TwoOutermostProgramsMayShareAWordUnderDifferentAsLiterals()
    {
        var (ok, stdout, detail) = CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUWM.
                   PROCEDURE DIVISION.
                   M1. CALL "DNUX1" CALL "DNUX2" STOP RUN.
                   END PROGRAM DNUWM.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUPW AS "DNUX1".
                   PROCEDURE DIVISION.
                   W1. DISPLAY "W-ONE" GOBACK.
                   END PROGRAM DNUPW.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUPW AS "DNUX2".
                   PROCEDURE DIVISION.
                   W2. DISPLAY "W-TWO" GOBACK.
                   END PROGRAM DNUPW.

                   """, dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("W-ONE\r\nW-TWO", stdout);
    }

    /// <summary>A PROTOTYPE beside its same-name definition is the INTENDED shape, not a duplicate: §10.6.2
    /// SR3 legislates FOR it: "If a compilation group contains both a function definition and a function
    /// prototype definition with the same externalized name, the signatures of these two compilation units
    /// shall be the same."</summary>
    [Fact]
    public void AFunctionPrototypeMayAccompanyItsDefinition()
    {
        var (ok, stdout, detail) = CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   FUNCTION-ID. DNUPROTO IS PROTOTYPE.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01 R PIC 9(4).
                   PROCEDURE DIVISION RETURNING R.
                   END FUNCTION DNUPROTO.
                   IDENTIFICATION DIVISION.
                   FUNCTION-ID. DNUPROTO.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01 R PIC 9(4).
                   PROCEDURE DIVISION RETURNING R.
                   F1. MOVE 42 TO R. GOBACK.
                   END FUNCTION DNUPROTO.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. DNUPROTOM.
                   ENVIRONMENT DIVISION.
                   CONFIGURATION SECTION.
                   REPOSITORY.
                       FUNCTION DNUPROTO.
                   PROCEDURE DIVISION.
                   M1. DISPLAY "F=" FUNCTION DNUPROTO GOBACK.
                   END PROGRAM DNUPROTOM.

                   """, dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("F=0042", stdout);
    }
}
