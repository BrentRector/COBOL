// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The APPLY COMMIT antecedent family under <c>--permissive</c> (kb/Work PB666): the syntax rules of CLAIMED clauses
/// and statements whose antecedent is "a file subject to an APPLY COMMIT clause" are enforced in their own right
/// (COBOLNET2974, <c>DataBinder.ScreenApplyCommitSubject</c>), not only "witnessed by the refusal of the antecedent".
/// </summary>
/// <remarks>
/// <para>⛔ WHY THIS EXISTS. kb/Work PB371 (owner, 2026-09-02) recorded the family CONFORMS because COBOLNET1709 refuses
/// the APPLY COMMIT clause (Annex A.4.3 is not claimed), and each row's witness is the negative fixture that pins that
/// refusal. But COBOLNET1709 is <c>PermissiveInert</c>: under <c>--permissive</c> the clause is a warning, the program
/// binds — and every one of these fixtures, each written in its rule's own forbidden shape, compiled with ZERO errors.
/// The negative corpus runs strict only, so it could not see it; <c>EditionHarness.CompileFull(…, permissive: true)</c>
/// is the seam, exactly as for SR8's exemption in <see cref="OpenSharingLockModeTests"/>.</para>
/// <para>The sources ARE the corpus fixtures, read from disk, so the forbidden shape is written once.
/// <c>apply-commit-same-area-shared</c> is deliberately absent: §12.4.6.4.4 GR3 is a general rule about an ACTIVE
/// clause's sharing at run time, and no APPLY COMMIT clause is ever active here in either lane.</para>
/// </remarks>
public sealed class ApplyCommitSubjectRuleTests
{
    /// <summary>A negative fixture's program text without its <c>*&gt;</c> header comment lines.</summary>
    private static string Fixture(string name) =>
        string.Join('\n', ConformanceCorpus.NegativeCase(name).Source.Split('\n')
            .Where(line => !line.TrimStart().StartsWith("*>", StringComparison.Ordinal)));

    /// <summary>Each fixture's forbidden shape draws its rule under <c>--permissive</c>, beside the declined clause's
    /// warning — the rule is enforced, not merely unreachable.</summary>
    [Theory]
    [InlineData("apply-commit-open-sharing", "§14.9.27.3 SR7")]
    [InlineData("apply-commit-lock-mode", "§12.4.5.9.3 SR1")]
    [InlineData("apply-commit-same-area", "§12.4.6.4.3 SR11")]
    [InlineData("apply-commit-read-lock", "§14.9.30.3 SR5")]
    [InlineData("apply-commit-rewrite-lock", "§14.9.35.3 SR5")]
    [InlineData("apply-commit-unlock", "§14.9.47.3 SR2")]
    public void ForbiddenShape_IsRefusedUnderPermissive(string fixture, string rule)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Fixture(fixture), 2023, permissive: true);
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1709");   // the clause WAS seen and declined
        Assert.False(ok, $"{fixture}: {rule} forbids this shape for a file subject to an APPLY COMMIT clause");
        Assert.Contains(errors, e => e.Contains("COBOLNET2974", StringComparison.Ordinal)
                                     && e.Contains(rule, StringComparison.Ordinal));
    }

    /// <summary>THE ARM THAT PROVES THE PROBE CAN FAIL: the SR7 fixture with its I-O-CONTROL paragraph removed — the
    /// same OPEN … SHARING on a file no APPLY COMMIT clause names — compiles under <c>--permissive</c>, so the refusal
    /// above is the antecedent's and not the statement's.</summary>
    [Fact]
    public void WithoutTheApplyCommitClause_TheSharingPhraseIsLegal()
    {
        string source = string.Join('\n', Fixture("apply-commit-open-sharing").Split('\n')
            .Where(line => !line.Contains("I-O-CONTROL", StringComparison.Ordinal)
                           && !line.Contains("APPLY COMMIT", StringComparison.Ordinal)));
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023, permissive: true);
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET2974");
        Assert.True(ok, string.Join("\n", errors));
    }
}
