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
[Trait("Sweep", "tree")]
public sealed class CitationAuditSelfTestDriftTests
{
    /// <summary>The audit's self-test still DECLARES every case (kb/Work PB2882, PB3022): the runner
    /// (scripts/self_tests.py) runs it in every gate and CI job and fails on a red case; what it cannot see is a case
    /// DELETED from the table, so each is named here as the table spells it.</summary>
    private static void AssertDeclares(string script, params string[] cases) =>
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("spec", script), cases);

    /// <summary>
    /// The CONSTRUCT-vs-CLAUSE audit: PHANTOM, SUBJECT, HEADER and the ordinal family (FORMAT, FORMAT-RULE,
    /// FORMAT-NAME, RULE, SUBITEM), each fired on its real defect and silent on the repaired twin.
    /// </summary>
    [Fact]
    public void CodeCitationAudit_ProvesEveryCheckCanFail()
    {
        AssertDeclares("audit_code_citations.py",
            "(\"SUBJECT\",", "(\"HEADER\",", "(\"PHANTOM\",", "(\"FORMAT\",", "(\"RULE\",", "(\"SUBITEM\",",
            "(\"FORMAT-NAME\",", "(\"FORMAT-RULE\",",
            // the DIAGNOSTIC-STRING family (kb/Work PB838) — a citation a USER reads, the highest cost per error
            "(\"DIAG-NO-RULE\",", "(\"DIAG-UNQUALIFIED\",",
            // every firing case is paired with a REPAIRED twin that must be silent, and the DIAGNOSTIC-STRING family
            // drives its SILENT calibrations: an arm that fires on everything is as useless as one that fires on nothing
            "silent on the REPAIRED", "silent calibration");
    }

    /// <summary>
    /// The QUOTED-FRAGMENT audit: MISFILED (the text is real and filed under the wrong clause), ELIDED (the
    /// clause is right and the quotation marks are around words the standard does not contain in that order),
    /// and the ABSENT bucket a paraphrase must land in rather than be accused from.
    /// </summary>
    [Fact]
    public void DocCitationAudit_ProvesEveryArmCanFail()
    {
        AssertDeclares("audit_doc_citations.py",
            "(\"prefix, misfiled\", \"misfiled\"",           // the citation precedes the quote
            "(\"postfix, misfiled\", \"misfiled\"",          // and follows it — both orders are read
            "(\"elided quotation\", \"elided\"",             // PB900 arm B, verbatim as it shipped
            "(\"elided quotation, repaired\", None",
            "(\"paraphrase, not an elision\", \"absent\"",   // what makes ELIDED gateable
            "fires   on a file WITHOUT",                     // the whole-file opt-out suppresses something
            "silent  on the same file WITH it",
            // kb/Work PB2525: one process per citation was 75 of the audit's 79 s
            "in-process: every ruling made with no process spawned");
    }

    /// <summary>
    /// The citation gate ITSELF (<c>cite.py --check</c>, kb/Work PB308/PB309, R66): an all-punctuation quotation is
    /// refused rather than matching every clause, the trailing-paragraph and sub-item rule paths are pinned, and a
    /// quotation of either the corrected or the PRINTED form of a marked correction is found in its own clause only.
    /// </summary>
    [Fact]
    public void CiteGate_ProvesEveryGuardCanFail()
    {
        AssertDeclares("cite.py", "fires EMPTY-NEEDLE (check)", "fires EMPTY-NEEDLE (find)", "pins 14.7.7 2) bare",
            "pins 14.9.8.4 1) b)", "accepts the PRINTED form (C6)", "fires on a wrong clause");
    }
}
