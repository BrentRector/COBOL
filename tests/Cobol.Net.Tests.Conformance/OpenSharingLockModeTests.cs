// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §14.9.27.3 SR8 — "When file-name-1 is not subject to an APPLY COMMIT clause, then if the sharing phrase
/// is omitted from the OPEN statement and the ALL phrase is specified in the SHARING clause of the file control
/// entry for file-name-1 or if the ALL phrase is specified on the OPEN statement, the LOCK MODE clause shall be
/// specified in the file control entry for file-name-1." — asserted on the two axes the .cob corpus cannot
/// reach (kb/Work PB319).
///
/// <para>⛔ WHY THESE ARE xUnit FACTS AND NOT CORPUS ENTRIES. (1) The negative corpus matches its <c>.err</c>
/// file as a SUBSTRING of the diagnostic stream, so it is blind to the rule being reported TWICE — which is
/// exactly what happened while SR8 was written down in two places, and exactly the drift a future re-addition
/// would reintroduce. Only a COUNT catches it. (2) The APPLY COMMIT exemption is reachable only under
/// <c>--permissive</c> (COBOLNET1709 declines the clause but is <c>PermissiveInert</c>), and neither corpus
/// runner has a permissive axis — <c>EditionHarness.CompileFull(…, permissive: true)</c> is the only seam.</para>
///
/// <para>The behaviour these guard is pinned from the other side by <c>conformance:cases/14.9.27-OPEN/pb319_sr8_antecedent</c>
/// (the antecedent falsified by a non-ALL phrase and by a file never opened),
/// <c>conformance:cases/14.9.27-OPEN/sharing-all-no-lockmode</c> (disjunct 1) and
/// <c>conformance:cases/14.9.27-OPEN/pb316-open-sharing-all-no-lockmode</c> (disjunct 2).</para>
/// </summary>
public sealed class OpenSharingLockModeTests
{
    /// <summary>A file control entry saying SHARING WITH ALL OTHER with no LOCK MODE clause, opened with no
    /// sharing phrase — SR8's disjunct 1 holds, so the program is illegal. Written with an optional
    /// <paramref name="applyCommit"/> paragraph so the exemption and its control arm are the SAME program.</summary>
    private static string Violation(string programId, bool applyCommit) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {programId}.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F1 ASSIGN TO "pb319t.dat"
                       ORGANIZATION IS SEQUENTIAL
                       SHARING WITH ALL OTHER.
               {(applyCommit ? "I-O-CONTROL." : "")}
                   {(applyCommit ? "APPLY COMMIT ON F1." : "")}
               DATA DIVISION.
               FILE SECTION.
               FD F1.
               01 F1-REC PIC X(10).
               PROCEDURE DIVISION.
               MAIN.
                   OPEN OUTPUT F1
                   CLOSE F1
                   STOP RUN.

        """;

    private static int Count1512(IEnumerable<string> diagnostics) =>
        diagnostics.Count(d => d.Contains("COBOLNET1512", StringComparison.Ordinal));

    /// <summary>⛔ THE DRIFT GUARD FOR THE EXTRACTION. One violation of one rule draws exactly ONE diagnostic.
    /// Before kb/Work PB319 this program drew TWO COBOLNET1512s in two different spellings — one off the SELECT
    /// clause alone (<c>DataBinder.BindFileControl</c>) and one from the OPEN binder — because SR8 was written
    /// down twice. A substring-matching <c>.err</c> golden cannot see that; the count can.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Sr8Violation_DrawsExactlyOneDiagnostic_FromTheOpenBinder(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Violation("PB319ONE", applyCommit: false), edition);
        Assert.False(ok, "SR8's disjunct 1 holds: the sharing phrase is omitted from the OPEN and the file "
            + "control entry says ALL, so the LOCK MODE clause is required");
        Assert.Equal(1, Count1512(errors));
        EditionHarness.AssertHasDiagnostic(errors, "OPEN of file 'F1'");
    }

    /// <summary>SR8's leading conjunct: a file SUBJECT TO AN APPLY COMMIT clause is exempt. The exemption is not
    /// decorative — §12.4.5.9.3 SR1 forbids writing a LOCK MODE clause "for a file that is the subject of an
    /// APPLY COMMIT clause", so without the exemption SR8 would demand the one clause another rule forbids and
    /// the program could not be written at all. Reachable because COBOLNET1709 is <c>PermissiveInert</c>: under
    /// <c>--permissive</c> the declined clause is a warning and the program compiles.</summary>
    [Fact]
    public void ApplyCommitFile_IsExemptFromSr8()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(
            Violation("PB319APC", applyCommit: true), 2023, permissive: true);
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1709");   // the clause WAS seen and declined
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET1512");
        Assert.True(ok, "§14.9.27.3 SR8 exempts a file subject to an APPLY COMMIT clause: "
            + string.Join("\n", errors));
    }

    /// <summary>THE ARM THAT PROVES THE PROBE CAN FAIL. Byte-for-byte the same program as the exemption fact
    /// minus its I-O-CONTROL paragraph, compiled the same way: without the APPLY COMMIT clause the file is not
    /// exempt and SR8 fires. Without this leg the fact above would pass just as well against a compiler that had
    /// simply stopped enforcing SR8 under <c>--permissive</c>.</summary>
    [Fact]
    public void WithoutTheApplyCommitClause_TheSameProgramIsRejectedUnderPermissive()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Violation("PB319NAP", applyCommit: false), 2023, permissive: true);
        Assert.False(ok, "no APPLY COMMIT clause, so SR8's leading conjunct holds and the rule applies");
        Assert.Equal(1, Count1512(errors));
    }
}
