// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// METHOD is a user-defined word at COBOL-2023 and a keyword where a format spells it (kb/Work PB2623). The printed
/// §8.9 list of ISO/IEC 1989:2023 does not contain METHOD (it names METHOD-ID), so §4.2.8 leaves it free at 2023, while
/// the END METHOD marker (§10.7.2) and the EXIT METHOD statement (removed at 2023 by Annex E.2 item 1 and
/// kept under <c>--permissive</c>) still spell it.
/// </summary>
/// <remarks>
/// The END METHOD half is the corpus golden <c>2023/pb2623_method_is_a_user_word_at_2023</c>. This class holds the
/// half the strict corpus cannot reach: EXIT METHOD survives only under <c>--permissive</c> at 2023, and a program
/// that declares a data item METHOD has EVERY METHOD token retyped to a user word by the §8.9 reservation gate, so the
/// keyword position (<c>methodKeyword</c>, CobolOO.g4) must still read it.
/// </remarks>
public sealed class MethodUserWordTests
{
    /// <summary>A method that ends with EXIT METHOD, in a program that also declares a data item named METHOD.</summary>
    private const string ExitMethodWithMethodDataItem = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB2623E.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS CPB2623E.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 METHOD PIC X(5) VALUE "HELLO".
        01 O USAGE OBJECT REFERENCE CPB2623E.
        PROCEDURE DIVISION.
        MAIN-P.
            DISPLAY METHOD.
            INVOKE CPB2623E "NEW" RETURNING O.
            INVOKE O "PING".
            STOP RUN.
        END PROGRAM PB2623E.

        IDENTIFICATION DIVISION.
        CLASS-ID. CPB2623E INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        METHOD-ID. PING.
        PROCEDURE DIVISION.
        MAIN-P.
            DISPLAY "PINGED".
            EXIT METHOD.
        END METHOD PING.
        END OBJECT.
        END CLASS CPB2623E.
        """;

    /// <summary>Under <c>--permissive</c> at 2023 the removed EXIT METHOD is a warning (COBOLNET0902), and the data item
    /// named METHOD does not stop the statement's keyword from being read: no syntax error, the program compiles.</summary>
    [Fact]
    public void ExitMethod_UnderPermissive_ReadsTheRetypedKeyword()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ExitMethodWithMethodDataItem, 2023, permissive: true);
        Assert.True(ok, string.Join("\n", errors));
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0902");
    }

    /// <summary>THE ARM THAT PROVES THE PROBE CAN FAIL: the same program targeting COBOL-2014, where §8.9 reserves
    /// METHOD, is refused at the data item (COBOLNET0901) — so the 2023 success above is the edition's, not the
    /// harness's.</summary>
    [Fact]
    public void MethodDataItem_At2014_IsRefused()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(ExitMethodWithMethodDataItem, 2014);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0901");
    }
}
