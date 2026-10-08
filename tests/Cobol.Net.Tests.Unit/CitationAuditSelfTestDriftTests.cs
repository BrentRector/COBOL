// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATE OVER THE TWO CITATION AUDITS' OWN FIXTURES — <c>scripts/spec/audit_code_citations.py</c> and
/// <c>scripts/spec/audit_doc_citations.py</c>, each driven through its <c>--self-test</c>.
/// </summary>
/// <remarks>
/// <para>
/// CLAUDE.md rule 1 makes an unvalidated citation a defect, and these two scripts are the only mechanical
/// enforcement of it. Both already ran <c>--check</c> in every gate (<c>scripts/build-local.*</c>, battery
/// phase 1) — but <c>--check</c> reporting zero is evidence only if the checks it ran can still FAIL, and
/// nothing ran either script's <c>--self-test</c>. That is not hypothetical here: the ELIDED arm
/// (kb/Work PB900 arm B) was added to <c>audit_doc_citations.scan</c> while the self-test kept its own
/// four-line re-implementation of the MISFILED ruling beside it, so the new arm was driven by nothing and every
/// case still printed PASS. <c>feedback_green_gates_arent_evidence</c>, in the gate that exists to catch
/// inherited citations.
/// </para>
/// <para>
/// ⛔ THE CASE NAMES ARE ASSERTED, NOT JUST THE EXIT CODE — a self-test quietly reduced to its happy path still
/// exits 0, and an arm deleted from <c>SELF_TEST</c> takes its own proof with it. Each name below is one arm of
/// one audit; adding an arm means adding its case here, which is the point.
/// </para>
/// </remarks>
public sealed class CitationAuditSelfTestDriftTests
{
    private static ProcessObservation RunSelfTest(string script)
    {
        string path = TestRepo.Scripts("spec", script);
        Assert.True(File.Exists(path), $"the citation audit is missing: {path}");
        return PythonInstrument.Run(path, "--self-test");
    }

    /// <summary>
    /// The CONSTRUCT-vs-CLAUSE audit: PHANTOM, SUBJECT, HEADER and the ordinal family (FORMAT, FORMAT-RULE,
    /// FORMAT-NAME, RULE, SUBITEM), each fired on its real defect and silent on the repaired twin.
    /// </summary>
    [Fact]
    public void CodeCitationAudit_ProvesEveryCheckCanFail()
    {
        var r = RunSelfTest("audit_code_citations.py");
        Assert.Equal(0, r.ExitCode);
        Assert.Contains("SELF-TEST: PASS", r.Stdout, StringComparison.Ordinal);
        foreach (string arm in new[] { "fires SUBJECT", "fires HEADER", "fires PHANTOM", "fires FORMAT",
                                       "fires RULE", "fires SUBITEM", "fires FORMAT-NAME", "fires FORMAT-RULE",
                                       // the DIAGNOSTIC-STRING family (kb/Work PB838) — a citation a USER
                                       // reads, which is the population with the highest cost per error
                                       "fires DIAG-NO-RULE", "fires DIAG-UNQUALIFIED" })
        {
            Assert.True(r.Stdout.Contains(arm, StringComparison.Ordinal),
                $"`audit_code_citations.py --self-test` no longer drives '{arm}' — a check that has never been "
                + $"seen to fail is not evidence that the tree is clean.\n{r.Stdout}{r.Stderr}");
        }

        // Every firing case is paired with a REPAIRED twin that must be silent: an arm that fires on everything
        // is as useless as one that fires on nothing, and only the twin separates the two.
        Assert.True(r.Stdout.Contains("silent on the REPAIRED", StringComparison.Ordinal),
            $"no repaired twin is driven — the arms are proven able to fire and not to stop.\n{r.Stdout}");
        Assert.True(r.Stdout.Contains("silent calibration", StringComparison.Ordinal),
            "the DIAGNOSTIC-STRING family drives no SILENT case — its three calibrations (an interpolated "
            + "`SR{rule}`, the same bare kind in a COMMENT, and an ordinal written `SR 10` with a space) are "
            + $"what make it gateable, and each was a false positive before it was made.\n{r.Stdout}");
    }

    /// <summary>
    /// The QUOTED-FRAGMENT audit: MISFILED (the text is real and filed under the wrong clause), ELIDED (the
    /// clause is right and the quotation marks are around words the standard does not contain in that order),
    /// and the ABSENT bucket a paraphrase must land in rather than be accused from.
    /// </summary>
    [Fact]
    public void DocCitationAudit_ProvesEveryArmCanFail()
    {
        var r = RunSelfTest("audit_doc_citations.py");
        Assert.Equal(0, r.ExitCode);
        Assert.Contains("SELF-TEST: PASS", r.Stdout, StringComparison.Ordinal);
        foreach (string mustDrive in new[]
                 {
                     "misfiled on prefix, misfiled",          // the citation precedes the quote
                     "misfiled on postfix, misfiled",         // and follows it — both orders are read
                     "elided   on elided quotation",          // PB900 arm B, verbatim as it shipped
                     "silent   on elided quotation, repaired",
                     "absent   on paraphrase, not an elision", // what makes ELIDED gateable
                     "fires   on a file WITHOUT",              // the whole-file opt-out suppresses something
                     "silent  on the same file WITH it",
                     // kb/Work PB2525: one process per citation was 75 of the audit's 79 s
                     "ok   in-process: every ruling made with no process spawned",
                 })
        {
            Assert.True(r.Stdout.Contains(mustDrive, StringComparison.Ordinal),
                $"`audit_doc_citations.py --self-test` no longer drives '{mustDrive}' — the arm it proves is "
                + $"unwitnessed, and its silence on the tree means nothing.\n{r.Stdout}{r.Stderr}");
        }
    }

    /// <summary>
    /// The citation gate ITSELF (<c>cite.py --check</c>, kb/Work PB308/PB309, R66): an all-punctuation quotation is
    /// refused rather than matching every clause, the trailing-paragraph and sub-item rule paths are pinned, and a
    /// quotation of either the corrected or the PRINTED form of a marked correction is found in its own clause only.
    /// </summary>
    [Fact]
    public void CiteGate_ProvesEveryGuardCanFail()
    {
        var r = RunSelfTest("cite.py");
        Assert.Equal(0, r.ExitCode);
        Assert.Contains("SELF-TEST: PASS", r.Stdout, StringComparison.Ordinal);
        foreach (string arm in new[] { "fires EMPTY-NEEDLE (check)", "fires EMPTY-NEEDLE (find)", "pins 14.7.7 2) bare",
                                       "pins 14.9.8.4 1) b)", "accepts the PRINTED form (C6)", "fires on a wrong clause" })
        {
            Assert.True(r.Stdout.Contains(arm, StringComparison.Ordinal),
                $"`cite.py --self-test` no longer drives '{arm}'.\n{r.Stdout}{r.Stderr}");
        }
    }
}
