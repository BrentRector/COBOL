// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ ISO §13.16.3 SR24 — WHICH LETTER the refusal quotes (kb/Work PB947). Every lettered exclusion produces the same
/// code, COBOLNET1976, and only the quoted sentence tells them apart, so a corpus <c>.err</c> naming the code cannot
/// see the defect this pins: a VARIABLE-LENGTH group (letter h) was refused under letter c) — "an alphanumeric group
/// containing items with a usage other than display" — because c)'s subject was asked as "a group with no
/// GROUP-USAGE clause", which is two of §3.11's FOUR exclusions from "alphanumeric group item".
/// <para>They are xUnit rows rather than corpus entries because a corpus <c>.err</c> is a substring that must be
/// PRESENT, and two of the three assertions here are that a letter is ABSENT from one entry's message while present
/// in its neighbour's.</para>
/// </summary>
public sealed class ConditionNameAssociationLetterTests
{
    private const int Edition = 2023;

    private static List<string> Compile(string pid, string dataDivision)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {dataDivision}
            PROCEDURE DIVISION.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(source, Edition);
        Assert.False(ok, $"[{pid}] must be REJECTED at --std {Edition}");
        return errors.ToList();
    }

    private static string ForName(List<string> errors, string conditionName)
    {
        var mine = errors.Where(e => e.Contains($"'{conditionName}'", StringComparison.Ordinal)).ToList();
        Assert.True(mine.Count > 0, $"no diagnostic names '{conditionName}'; got:\n{string.Join("\n", errors)}");
        return string.Join("\n", mine);
    }

    /// <summary>The note's own repro: the SAME non-DISPLAY member under an alphanumeric group (c) and under a
    /// variable-length group (h). A variable-length group is not an alphanumeric group (§3.11), so it earns h) and
    /// never c).</summary>
    [Fact]
    public void VariableLengthGroupWithNonDisplayMember_QuotesLetterH_AndAlphanumericGroupQuotesLetterC()
    {
        var errors = Compile("PB947LETTERS", """
            01 G1.
               88 G1-A VALUE "XXXXXX".
               05 P1 PIC 9(4) COMP.
               05 P2 PIC X(2).
            01 VG.
               88 VG-A VALUE "XXXXXX".
               05 V1 PIC 9(4) COMP.
               05 V2 PIC X DYNAMIC LENGTH LIMIT IS 5.
            """);

        string alnum = ForName(errors, "G1-A");
        Assert.Contains("COBOLNET1976", alnum);
        Assert.Contains("\"c) An alphanumeric group containing items with a usage other than display.\"", alnum);
        Assert.DoesNotContain("h) A variable-length group", alnum);

        string variable = ForName(errors, "VG-A");
        Assert.Contains("COBOLNET1976", variable);
        Assert.Contains("\"h) A variable-length group.\"", variable);
        Assert.DoesNotContain("c) An alphanumeric group", variable);
    }

    /// <summary>A STRONGLY-TYPED group instance is not an alphanumeric group (§3.11) either. SR24 letter g) is the
    /// type DECLARATION's, so the instance is barred by the rule that is about it — §13.18.57.3 SR2, COBOLNET1537, an
    /// 88 immediately after a TYPE-clause entry — and by no SR24 letter. It used to draw a second, wrong refusal under
    /// letter c) for the COMP member its TYPE brings.</summary>
    [Fact]
    public void StronglyTypedGroupInstance_IsRefusedByTheTypeClauseRule_AndByNoSr24Letter()
    {
        var errors = Compile("PB947STRONG", """
            01 T IS TYPEDEF STRONG.
               05 P1 PIC 9(4) COMP.
               05 P2 PIC X(2).
            01 SG TYPE T.
               88 SG-A VALUE "XXXXXX".
            """);

        string mine = ForName(errors, "SG-A");
        Assert.Contains("COBOLNET1537", mine);
        Assert.DoesNotContain("COBOLNET1976", mine);
    }
}
