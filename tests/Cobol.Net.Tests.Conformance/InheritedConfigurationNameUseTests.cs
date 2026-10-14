// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §8.3.2.2 — "Within a source element, a given user-defined word may be used as only one type of user-defined
/// word" — over the names a CONTAINING element's configuration section declares (kb/Work PB3153). §8.4.6.1 lets a
/// contained source element reference them ("may be referenced only by statements and entries either in that source
/// element that contains a configuration section or in any source element contained within that source element"),
/// and lets it use identical words of its own ("each source element may use identical user-defined words to name items
/// independent of the use of these user-defined words by other source elements"). So a contained program that
/// declares a paragraph PBK is legal, and the same program that ALSO uses PBK as the inherited type uses one word as
/// two types in its own source element: refused COBOLNET2692, exactly as the flat program that declares both is.
/// </summary>
/// <remarks>
/// One row per type §8.4.6.1 lists as declared in a configuration section, each at the first edition that has it.
/// The corpus goldens <c>negative/pb3153-contained-program-paragraph-spells-an-inherited-class-it-uses</c> and
/// <c>85/pb3153_inherited_special_names_words</c> pin the class-name and symbolic-character rows end to end;
/// <c>UserWordDeclarationDriftTests.EveryConfigurationSectionNameType_ResolvesThroughARecordingTable</c> keeps every
/// type's registry a recording one.
/// </remarks>
public sealed class InheritedConfigurationNameUseTests
{
    /// <summary>The configuration-section entry, an optional data entry that uses the name, an optional statement
    /// that uses it, and the edition the type is first available at.</summary>
    public static TheoryData<string, string, string, string, int> Kinds => new()
    {
        { "class-name", "CLASS PBK IS \"0\" THRU \"9\".", "", "IF X IS PBK DISPLAY \"Y\" END-IF.", 85 },
        { "alphabet-name", "ALPHABET PBK IS STANDARD-1.", "", "IF X IS PBK DISPLAY \"Y\" END-IF.", 85 },
        { "symbolic-character", "SYMBOLIC CHARACTERS PBK IS 66.", "", "DISPLAY PBK.", 85 },
        { "mnemonic-name", "CONSOLE IS PBK.", "", "DISPLAY \"Y\" UPON PBK.", 85 },
        { "condition-name", "SWITCH-1 IS PBM ON STATUS IS PBK.", "", "IF PBK DISPLAY \"Y\" END-IF.", 85 },
        { "locale-name", "LOCALE PBK IS \"en-US\".", "", "SET LOCALE LC_ALL TO PBK.", 2002 },
        { "ordering-name", "ORDER TABLE PBK IS \"ISO14651_2020_TABLE1\".", "",
            "DISPLAY FUNCTION STANDARD-COMPARE(\"a\" \"b\" PBK).", 2014 },
        { "dynamic-length-structure-name", "DYNAMIC LENGTH STRUCTURE PBK IS DELIMITED.",
            "01 D PIC X DYNAMIC LENGTH PBK LIMIT 5.", "", 2014 },
    };

    private static string Program(string entry, string data, string use) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB3153O.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            {entry}
        PROCEDURE DIVISION.
            CALL "PB3153I".
            STOP RUN.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB3153I.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 X PIC X VALUE "5".
        {data}
        PROCEDURE DIVISION.
            {use}
            GO TO PBK.
        PBK.
            DISPLAY "P".
        END PROGRAM PB3153I.
        END PROGRAM PB3153O.
        """;

    /// <summary>The contained program USES the inherited name and declares a paragraph spelled like it: two types of
    /// user-defined word in its own source element (§8.3.2.2), refused COBOLNET2692 naming both.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void AUseOfTheInheritedName_BesideAParagraphOfTheSameSpelling_IsRefused(string kind, string entry,
        string data, string use, int edition)
    {
        var (ok, diags) = EditionHarness.Compile(Program(entry, data, use), edition);
        Assert.False(ok, $"PBK used as a {kind} and declared as a paragraph-name in one source element must be refused");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET2692");
        EditionHarness.AssertHasDiagnostic(diags, kind);
    }

    /// <summary>The same contained program WITHOUT the use declares only the paragraph: legal (§8.4.6.1, "each source
    /// element may use identical user-defined words"), so no COBOLNET2692.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheParagraphAlone_IsLegal(string kind, string entry, string data, string use, int edition)
    {
        _ = (kind, data, use);
        var (_, diags) = EditionHarness.Compile(Program(entry, "", ""), edition);
        EditionHarness.AssertNoDiagnostic(diags, "COBOLNET2692");
    }

    /// <summary>§8.4.6.1: "a source element's reference to such a name, even when it is a different type of
    /// user-defined word, is to the item which that source element describes". SET LOCALE … TO admits a locale-name
    /// and an identifier, so the contained program's own PBL is the operand, not its container's locale-name PBL: a
    /// data-pointer item is legal, and an alphanumeric one is §14.9.39.3 SR27's refusal (COBOLNET1668), never a use
    /// of the locale-name (COBOLNET2692).</summary>
    [Theory]
    [InlineData("USAGE POINTER", true)]
    [InlineData("PIC X", false)]
    public void SetLocaleTo_TheElementsOwnItem_WinsOverAnInheritedLocaleName(string description, bool legal)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3153LO.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                LOCALE PBL IS "en-US".
            PROCEDURE DIVISION.
                CALL "PB3153LI".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3153LI.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PBL {description}.
            PROCEDURE DIVISION.
                SET LOCALE LC_ALL TO PBL.
            END PROGRAM PB3153LI.
            END PROGRAM PB3153LO.
            """;
        var (ok, diags) = EditionHarness.Compile(source, 2002);
        EditionHarness.AssertNoDiagnostic(diags, "COBOLNET2692");
        Assert.Equal(legal, ok);
        if (!legal) EditionHarness.AssertHasDiagnostic(diags, "COBOLNET1668");
    }

    /// <summary>The same rule at WRITE … ADVANCING, which admits a mnemonic-name and an identifier: the contained
    /// program's own PBT is the line count, not its container's mnemonic-name PBT for C01. An integer item is legal; an
    /// alphanumeric one is §14.9.51.3 SR14's refusal (COBOLNET2365), never a use of the mnemonic-name.</summary>
    [Theory]
    [InlineData("PIC 9 VALUE 2", true)]
    [InlineData("PIC X VALUE \"2\"", false)]
    public void WriteAdvancing_TheElementsOwnItem_WinsOverAnInheritedMnemonicName(string description, bool legal)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3153WO.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                C01 IS PBT.
            PROCEDURE DIVISION.
                CALL "PB3153WI".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3153WI.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "pb3153w.dat".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 R PIC X(10).
            WORKING-STORAGE SECTION.
            01 PBT {description}.
            PROCEDURE DIVISION.
                OPEN OUTPUT F.
                WRITE R AFTER ADVANCING PBT.
                CLOSE F.
            END PROGRAM PB3153WI.
            END PROGRAM PB3153WO.
            """;
        var (ok, diags) = EditionHarness.Compile(source, 85);
        EditionHarness.AssertNoDiagnostic(diags, "COBOLNET2692");
        Assert.Equal(legal, ok);
        if (!legal) EditionHarness.AssertHasDiagnostic(diags, "COBOLNET2365");
    }

    /// <summary>A METHOD is a source element of its own (§10.5), and the class-name its class's SPECIAL-NAMES declares
    /// reaches it the same way: method M1A uses PB53MDIG in a class condition and declares a paragraph PB53MDIG (refused
    /// at the use), while method M1B only declares the paragraph (legal).</summary>
    [Fact]
    public void AMethodsUseOfItsClassesClassName_BesideAParagraphOfTheSameSpelling_IsRefused()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            CLASS-ID. PB53MCLS INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CLASS PB53MDIG IS "0" THRU "9".
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M1A.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 X PIC X VALUE "5".
            PROCEDURE DIVISION.
                IF X IS PB53MDIG DISPLAY "DIG" END-IF.
                GO TO PB53MDIG.
            PB53MDIG.
                GOBACK.
            END METHOD M1A.
            METHOD-ID. M1B.
            PROCEDURE DIVISION.
                GO TO PB53MDIG.
            PB53MDIG.
                GOBACK.
            END METHOD M1B.
            END OBJECT.
            END CLASS PB53MCLS.
            """;
        var (ok, diags) = EditionHarness.Compile(source, 2002);
        Assert.False(ok);
        Assert.Single(diags, d => d.Contains("COBOLNET2692", StringComparison.Ordinal));
        EditionHarness.AssertHasDiagnostic(diags, "'PB53MDIG' names two types of user-defined word in this source "
            + "element (paragraph-name and class-name)");
    }
}
