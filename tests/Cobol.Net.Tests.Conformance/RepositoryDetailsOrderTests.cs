// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §12.3.8.4 GR10 / GR11 — where a REPOSITORY prototype's details come from (kb/Work PB989; DESIGN-external-
/// repository §8.2, §8.3, slice 6): a definition "specified previously in the same compilation group" (a), else a
/// prototype definition in it (b), else the external repository (c), the whole search keyed by EXTERNALIZED name and
/// run AS OF the referencing source element's position. The goldens <c>2002/pb989_*</c> and
/// <c>negative/pb989-*</c> carry the run-time and rejection witnesses; these tests carry what a golden cannot state: a
/// warning's presence and absence, a run-time failure's name, and the one arm (c) that a later slice replaces.
/// </summary>
public sealed class RepositoryDetailsOrderTests
{
    /// <summary>A function PROTOTYPE whose AS literal-1 differs from its word, a function DEFINITION that shares the
    /// WORD but not the externalized name, and a caller between them. §11.5.4 GR2: literal-1 "is the name of the
    /// function prototype that is externalized to the operating environment", so the REPOSITORY entry
    /// <c>FUNCTION P989GONE</c> — which names the prototype — activates the function externalized "P989NOWHERE", and the
    /// following definition P989GONE (externalized "P989GONE") is not it: GR11 a) accepts only a definition "specified
    /// previously". The old word-keyed merge replaced the prototype with that definition and activated it.</summary>
    private const string PrototypeWordSharedByAFollowingDefinition = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. P989GONE AS "P989NOWHERE" IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X PIC 9(4).
        01 L-R PIC 9(4).
        PROCEDURE DIVISION USING L-X RETURNING L-R.
        END FUNCTION P989GONE.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P989RUN.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            FUNCTION P989GONE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-R PIC 9(4).
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY "BEFORE".
            COMPUTE WS-R = FUNCTION P989GONE(7).
            DISPLAY "NEVER " WS-R.
            STOP RUN.
        END PROGRAM P989RUN.
        IDENTIFICATION DIVISION.
        FUNCTION-ID. P989GONE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X PIC 9(4).
        01 L-R PIC 9(4).
        PROCEDURE DIVISION USING L-X RETURNING L-R.
        P.
            COMPUTE L-R = L-X * 5.
            GOBACK.
        END FUNCTION P989GONE.
        """;

    [Fact]   // GR11 b) + §11.5.4 GR2: the prototype's literal-1 is the activation key, never the following definition.
    public void PrototypeLiteral_IsTheActivatedName_EvenWhenAFollowingDefinitionSharesItsWord()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(PrototypeWordSharedByAFollowingDefinition, 2002);
        Assert.False(ok, "P989NOWHERE is defined nowhere: the activation must fail, not reach P989GONE (stdout: " + stdout + ")");
        Assert.Contains("EC-FUNCTION-NOT-FOUND", detail, StringComparison.Ordinal);
        Assert.Contains("P989NOWHERE", detail, StringComparison.Ordinal);
        Assert.Equal("BEFORE", stdout.Trim());
    }

    /// <summary>The §8.4.6.7 / SR10 word reading and the GR11 externalized-name reading name two functions: a
    /// definition externalized "P989W" (its word P989WX) and a definition P989W AS "P989Z". D-R3: GR11 a) wins and the
    /// compilation says so (COBOLNET2969).</summary>
    private const string TwoFunctionsOneWord = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. P989W AS "P989Z".
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-R PIC 9(4).
        PROCEDURE DIVISION RETURNING L-R.
        P.
            MOVE 1 TO L-R.
            GOBACK.
        END FUNCTION P989W.
        IDENTIFICATION DIVISION.
        FUNCTION-ID. P989WX AS "P989W".
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-R PIC 9(4).
        PROCEDURE DIVISION RETURNING L-R.
        P.
            MOVE 2 TO L-R.
            GOBACK.
        END FUNCTION P989WX.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P989TWO.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            {0}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-R PIC 9(4).
        PROCEDURE DIVISION.
        MAIN.
            COMPUTE WS-R = FUNCTION {1}.
            DISPLAY WS-R.
            STOP RUN.
        END PROGRAM P989TWO.
        """;

    [Fact]   // D-R3 step 3: the externalized name wins and the other function is reported, at the specifier.
    public void ExternalizedNameWins_AndTheFunctionThatOnlyTheWordNamesIsReported()
    {
        string source = string.Format(TwoFunctionsOneWord, "FUNCTION P989W", "P989W");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, 2002);
        Assert.True(ok, string.Join("\n", errors));
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET2969");
        Assert.Contains(warnings, w => w.Contains("COBOLNET2969", StringComparison.Ordinal) && w.Contains("P989Z", StringComparison.Ordinal));
        var run = EditionHarness.CompileAndRun(source, 2002);
        Assert.True(run.Ok, run.Detail);
        Assert.Equal("0002", run.Stdout.Trim());
    }

    [Fact]   // The same pair, named through its AS literal: GR11 a) with literal-5 selects P989W AS "P989Z" and warns of nothing.
    public void AnAsLiteralThatNamesTheOtherFunction_SelectsItWithoutAWarning()
    {
        string source = string.Format(TwoFunctionsOneWord, "FUNCTION P989Q AS \"P989Z\"", "P989Q");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, 2002);
        Assert.True(ok, string.Join("\n", errors));
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET2969");
        var run = EditionHarness.CompileAndRun(source, 2002);
        Assert.True(run.Ok, run.Detail);
        Assert.Equal("0001", run.Stdout.Trim());
    }

    [Theory]   // §10.6.2 SR3 PAIRS a prototype with a definition of its externalized name: one function, so nothing to report.
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void APrototypeAndItsDefinition_AreOneFunction_AndDrawNoWarning(int edition)
    {
        const string source = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. P989PAIR IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-R PIC 9(4).
            PROCEDURE DIVISION RETURNING L-R.
            END FUNCTION P989PAIR.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. P989PAIR.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-R PIC 9(4).
            PROCEDURE DIVISION RETURNING L-R.
            P.
                MOVE 9 TO L-R.
                GOBACK.
            END FUNCTION P989PAIR.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. P989PRG.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION P989PAIR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION P989PAIR.
                DISPLAY WS-R.
                STOP RUN.
            END PROGRAM P989PRG.
            """;
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
        Assert.True(ok, string.Join("\n", errors));
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET2969");
    }

    /// <summary>The CLASS arm of the same search: a class's REPOSITORY reaches every method (§12.3.4 GR1), and its
    /// "previously" is the CLASS's start. The function is written before the class in one variant and after it in the
    /// other, and nothing else differs.</summary>
    private const string ClassNamingAFunction = """
        IDENTIFICATION DIVISION.
        CLASS-ID. P989K INHERITS BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE
            FUNCTION P989CF.
        IDENTIFICATION DIVISION.
        FACTORY.
        PROCEDURE DIVISION.
        METHOD-ID. FT.
        DATA DIVISION.
        LINKAGE SECTION.
        01 X PIC 9(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING X RETURNING R.
        P-MAIN.
            COMPUTE R = FUNCTION P989CF(X) + 1.
        END METHOD FT.
        END FACTORY.
        END CLASS P989K.
        """;

    private const string FunctionForTheClass = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. P989CF.
        DATA DIVISION.
        LINKAGE SECTION.
        01 N PIC 9(4).
        01 R PIC 9(4).
        PROCEDURE DIVISION USING N RETURNING R.
        P-MAIN.
            COMPUTE R = N * 2
            GOBACK.
        END FUNCTION P989CF.
        """;

    [Theory]   // GR11 a): the definition ahead of the class is "specified previously"; the one after it is not.
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void AClassRepository_TakesAFunctionOnlyFromAheadOfTheClass(bool functionFirst, bool binds)
    {
        string source = functionFirst
            ? FunctionForTheClass + "\n" + ClassNamingAFunction
            : ClassNamingAFunction + "\n" + FunctionForTheClass;
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2002);
        Assert.Equal(binds, ok);
        if (!binds) EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1505");
    }

    /// <summary>GR10 a)/b): a program definition BELOW the caller, a program prototype definition ahead of both.</summary>
    private const string CallerBetweenPrototypeAndDefinition = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P989CALLEE IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-A PIC 9(4).
        PROCEDURE DIVISION USING L-A.
        END PROGRAM P989CALLEE.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P989CALLER.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            PROGRAM P989CALLEE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-A PIC 9(4) VALUE 1.
        01 WS-B PIC 9(4) VALUE 2.
        PROCEDURE DIVISION.
        MAIN.
            CALL P989CALLEE USING WS-A WS-B.
            STOP RUN.
        END PROGRAM P989CALLER.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P989CALLEE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-A PIC 9(4).
        PROCEDURE DIVISION USING L-A.
        P.
            GOBACK.
        END PROGRAM P989CALLEE.
        """;

    [Fact]   // GR10 b): the prototype ahead of the caller supplies the details, so the second argument is refused.
    public void AProgramPrototypeAheadOfTheCaller_SuppliesTheDetails_OfADefinitionThatFollows()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(CallerBetweenPrototypeAndDefinition, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1684");
    }

    [Fact]   // GR10 a) is "specified PREVIOUSLY": with NO prototype, the definition that follows is the arm-c) program.
    public void AProgramDefinitionThatFollowsTheCaller_IsNotGr10A_SoItsFormalsAreNotCheckedAtCompileTime()
    {
        // The prototype block is the first unit: drop it, keep the caller and the following definition. Arm c) — "the
        // details are taken from the external repository" — is this implementation's run-unit program registry, which
        // carries no compile-time signature; DESIGN-external-repository slice 7 replaces it with the metadata
        // repository (and SR14's REPO-1 for a name no provider answers), and this test is that slice's to change.
        const string Header = "IDENTIFICATION DIVISION.";
        string withoutPrototype = string.Concat(CallerBetweenPrototypeAndDefinition
            .Split(Header, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(unit => Header + unit));
        var (ok, errors, _) = EditionHarness.CompileFull(withoutPrototype, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }
}
