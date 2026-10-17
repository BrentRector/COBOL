// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// THE witnesses for the <c>DOCUMENTED-NON-SUPPORT</c> traceability rows whose facility is ACCEPT-INERT — the
/// half of that verdict that is easy to skip. <c>tests/version-matrix/inventory-schema.json</c> is explicit:
/// "The notes field carries the decision reference; closing still needs a test proving we <b>diagnose</b> (or,
/// where the departure is a reported VALUE rather than a rejected construct, that the documented behaviour is
/// what actually happens) rather than silently miscompile." A declined facility a program can still write and
/// get a clean, silent compile out of is not declined — it is undiagnosed, and nothing else in the battery can
/// tell those apart.
///
/// <para>⛔ WHY THESE ARE NOT GOLDENS. The facilities left in this class are declined <b>ACCEPT-INERT</b>, so
/// their named diagnostics are <b>warnings</b> (COBOLNET1578-1580, docs/CONFORMANCE.md §1): the positive corpus
/// asserts a program COMPILES and byte-matches its <c>.out</c> and never looks at the warning channel, and the
/// negative corpus requires the compile to FAIL, which a warning does not. So a corpus golden can pin the
/// INERTNESS half and only a compile-diagnostic assertion can pin the DECLINE half. Each test below therefore
/// reads the SAME source the corpus runs — one source of truth for both halves — and adds the assertion the
/// corpus structurally cannot make. (feedback_green_test_can_hold_a_gap_open: a warning nothing asserts reads
/// as implemented support.)</para>
///
/// <para>⛔ SCREEN HANDLING (Annex A.4.2) IS NO LONGER WITNESSED HERE, and its absence is the point. It used to
/// be the largest client of this class, pinned as an accept-inert WARNING. kb/Work PB260 measured that posture
/// and refuted it: A.4.1 — "An implementation shall accept the syntax and provide the functionality for an
/// optional element only when support for that language element is claimed by the implementor" — is a
/// prohibition on ACCEPTING, so an unclaimed A.4 module is REFUSED, not warned. The whole module is now an
/// ERROR by name (COBOLNET1560 data/environment surface, COBOLNET1707 procedure surface) through the one funnel
/// <see cref="CobolNet.Binding.ScreenFacility"/>, which puts its witnesses where a refusal belongs: the NEGATIVE
/// corpus, 41 programs under <c>tests/conformance/negative/a42-*</c>, each <c>.err</c> naming the construct as
/// well as the code. <c>ScreenFacilityConstructDriftTests</c> holds that coverage complete against
/// <c>CobolScreen.g4</c>. Adding a screen test back here would need it to COMPILE, which is exactly what the
/// module may no longer do.</para>
///
/// <para>THE SPEC BASIS, per remaining facility. Commit/rollback is PROCESSOR-DEPENDENT (Annex A.3 item 6) and
/// §4.2.6 third paragraph makes the compile-time warning mechanism MANDATORY for one that is not supported, so
/// the COBOLNET1579 assertion is a conformance obligation, not a house style. VALIDATE (Annex A.4.14) is
/// OPTIONAL and additionally OBSOLETE (§4.2.13; Annex F.2 item 5): §4.2.7 requires only that the decline be
/// identified in user documentation, so COBOLNET1580 is this implementation's own stronger posture — and a
/// posture that is documented but unmeasured is exactly what these rows owe. kb/Work PB261 owns the
/// commit/rollback debt. Asynchronous messaging (Annex A.3 item 4) and the OPTIONS FLOAT-DECIMAL clause (A.3
/// item 13) are processor-dependent too, so their warnings (COBOLNET1578, COBOLNET2424) are §4.2.6 ¶3
/// obligations; kb/Work R43 made the rows conditioned solely on them derived verdicts that these facts witness.</para>
/// </summary>
public sealed class DocumentedNonSupportWitnessTests
{
    private const string CommitNonSupport = "COBOLNET1579";     // COMMIT/ROLLBACK — A.3 items 6-7 / §4.2.6 ¶3
    private const string ValidateNonSupport = "COBOLNET1580";   // VALIDATE — A.4.14 / §4.2.7 / F.2 item 5
    private const string RecordDelimiterNonSupport = "COBOLNET1778";   // RECORD DELIMITER — A.3 item 26 + A.1 item 150
    private const string McsNonSupport = "COBOLNET1578";               // SEND/RECEIVE — A.3 item 4 / §4.2.6 ¶3
    private const string FloatDecimalClauseNonSupport = "COBOLNET2424"; // OPTIONS FLOAT-DECIMAL — A.3 items 13, 19

    /// <summary>The conformance-suite test's source text — the witness and the suite run the SAME file.</summary>
    private static string Golden(string id) => File.ReadAllText(TestRepo.SuiteCase(id));

    /// <summary>The recognize-and-warn contract: at every named edition the source COMPILES (the facility is
    /// declined ACCEPT-INERT, not refused) and the NAMED non-support diagnostic rides the non-failing
    /// channel.</summary>
    private static void AssertRecognizedAndNamed(string source, string code, params int[] editions)
    {
        foreach (int edition in editions)
        {
            var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
            Assert.True(ok, $"--std {edition}: an ACCEPT-INERT declined facility must still COMPILE "
                + $"(docs/CONFORMANCE.md §1): {string.Join("\n", errors)}");
            EditionHarness.AssertHasDiagnostic(warnings, code);
        }
    }

    /// <summary>COMMIT / ROLLBACK → COBOLNET1579. This is the one assertion in this class the STANDARD itself
    /// compels: Annex A.3 item 6 makes the facility processor-dependent, and §4.2.6 ¶3 — "An implementation
    /// shall provide a warning mechanism at compile time to indicate use of syntactically-detectable
    /// processor-dependent language elements not supported by that implementation" — makes the compile-time
    /// warning mandatory. §14.9.7.4 GR3/GR4/GR5 ride the decline through A.4.1. kb/Work PB261.
    /// <para>⛔ AND IT IS THE ONLY EVIDENCE AVAILABLE, which is why the golden's <c>.out</c> cannot close these
    /// rows on its own: §14.9.7.4 GR1 and §14.9.36.4 GR1 both read "If this statement is executed when there is
    /// no active APPLY COMMIT clause, then it has the same effect as a CONTINUE statement with no additional
    /// phrases", so with no APPLY COMMIT clause in the program a conforming processor and a declining one are
    /// behaviourally IDENTICAL. The observable difference needs an APPLY COMMIT clause, and that clause does not
    /// parse (the sibling row GR-14.9.7.4-1) — so the compile-time name is the whole witness.</para></summary>
    [Fact]
    public void CommitRollbackFacility_NamedNonSupportWarning_PinnedToSpec() =>
        AssertRecognizedAndNamed(Golden("pb261_commit_facility_witness"), CommitNonSupport, 2023);

    /// <summary>VALIDATE → COBOLNET1580, at the editions the facility exists (2002+; at --std 85 VALIDATE is
    /// an ordinary user-defined word). §13.18.40.4 GR19's only trigger is the format validation stage of a
    /// VALIDATE statement, so the decline is what makes the rule unreachable.
    /// <para>⚠ The golden's <c>.out</c> pins INERTNESS only, never the decline: §14.9.50.4 GR5 ("the execution
    /// of the VALIDATE statement does not terminate and the content of the invalid data item does not change")
    /// and §13.18.17.4 GR1 ("The data item itself remains unchanged") make an UNCHANGED subject the conforming
    /// outcome too, so "SUBJECT UNCHANGED" discriminates nothing. Only this warning does.</para></summary>
    [Fact]
    public void ValidateFacility_NamedNonSupportWarning_PinnedToSpec() =>
        AssertRecognizedAndNamed(Golden("dns_validate_picture_locale_witness"),
            ValidateNonSupport, 2002, 2014, 2023);

    /// <summary>RECORD DELIMITER → COBOLNET1778, at EVERY edition (the clause is in the '85 file control entry
    /// and every edition since), and — the point of the pair — ONCE PER ARM. §4.2.6 ¶3 compels the warning for
    /// the STANDARD-1 arm: Annex A.3 item 26 makes it processor-dependent and §12.4.5.11.4 GR2 makes its medium
    /// a tape drive this implementation has none of. The feature-name arm is declined on a DIFFERENT licence —
    /// §12.4.5.11.3 SR2 leaves the available names to the implementor and Annex A.1 item 150 makes providing
    /// them optional, and this implementation provides none (docs/CONFORMANCE.md §7) — so nothing about the
    /// STANDARD-1 fix implies it fires.
    /// <para>⛔ THE ASSERTION IS ON THE ARM, NOT ON THE CODE, and that is the whole design. kb/Work PB292's own
    /// warning was that "a fix touching only the STANDARD-1 arm repeats the defect shape" — and a test that
    /// only asked for COBOLNET1778 over a two-SELECT program would PASS with the feature-name arm still silent
    /// (feedback_two_arm_dispatch). Each fact therefore demands the diagnostic that quotes ITS OWN clause
    /// spelling back, which is exactly the `seen` half <see cref="CobolNet.Binding.EditionContext.Declined"/>
    /// composes at the site.</para>
    /// <para>⚠ The golden's <c>.out</c> pins INERTNESS only and cannot pin the decline: §12.4.5.11.4 GR1 —
    /// "Any method used shall not be reflected in the record area or the record size used within the function,
    /// method, or program" — means a conforming processor honouring STANDARD-1 and this one ignoring it print
    /// the SAME lengths. Only the compile-time name discriminates.</para></summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void RecordDelimiterStandard1_NamedNonSupportWarning_PinnedToSpec(int edition) =>
        AssertArmNamed(edition, "RECORD DELIMITER IS STANDARD-1");

    /// <summary>The feature-name-1 arm of the same required choice — see the pair's rationale above.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void RecordDelimiterFeatureName_NamedNonSupportWarning_PinnedToSpec(int edition) =>
        AssertArmNamed(edition, "RECORD DELIMITER IS PB292-TAPE-FORMAT");

    /// <summary>The same STANDARD-1 arm written WITHOUT the optional word. §12.4.5.11.2's printed diagram
    /// underlines RECORD, DELIMITER and STANDARD-1 and leaves IS unmarked, so `RECORD DELIMITER STANDARD-1` is
    /// the same clause and must draw the same decline — a decline keyed on the optional word would be a
    /// spelling test wearing a conformance test's clothes.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void RecordDelimiterOptionalWordOmitted_NamedNonSupportWarning_PinnedToSpec(int edition) =>
        AssertArmNamed(edition, "RECORD DELIMITER STANDARD-1");

    /// <summary>⛔ THE COMPLEMENT (feedback_measure_the_selectors_complement). The three facts above prove the
    /// warning FIRES; this one proves it fires on the CLAUSE and not on the file, the file's organization or
    /// the RECORD VARYING clause — file F4 of the same golden is variable-length, sequential and opened,
    /// written and read exactly like the other three, and writes NO RECORD DELIMITER clause. It is also the
    /// only file in the program under which §12.4.5.11.4 GR5 speaks at all ("If the RECORD DELIMITER clause is
    /// not specified …"), so a warning there would contradict the determination filed as Annex A.1 item 151.
    /// A diagnostic that cannot be silent is not evidence of anything.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void RecordDelimiterAbsent_DrawsNoWarning_AndOnlyTheThreeClausesDo(int edition)
    {
        string source = Golden("pb292_record_delimiter_witness");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
        Assert.True(ok, $"--std {edition}: {string.Join("\n", errors)}");
        var raised = warnings.Where(w => w.Contains(RecordDelimiterNonSupport, StringComparison.Ordinal))
            .ToList();
        Assert.Equal(3, raised.Count);
        Assert.DoesNotContain(raised, w => w.Contains("on file 'F4'", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>ASYNCHRONOUS MESSAGING → COBOLNET1578, ONCE PER STATEMENT (kb/Work R43 / PB1198). Annex A.3
    /// item 4 makes the facility processor-dependent ("dependent on the capability of a processor to allow run
    /// units to communicate with each other") and §4.2.6 ¶3 makes the compile-time warning mandatory for a
    /// declined one. The facility has TWO statements, RECEIVE (§14.9.31) and SEND (§14.9.38), and until this test
    /// only the SEND site was asserted anywhere (GobackGeneralFormatTests) — so the count, not the code, is the
    /// assertion: a decline wired into one statement's binder arm and not the other would still produce one
    /// COBOLNET1578 (feedback_two_arm_dispatch). The facility is a COBOL-2023 introduction; below it the two formats
    /// draw COBOLNET0900 and no COBOLNET1578 (negative/pb374-async-messaging-below-2023, kb/Work PB374), and at
    /// 2014 both words are ordinary user-defined words (at 2002 they are still reserved for the obsolete communication
    /// facility, kb/Work PB656; negative/user-word-receive, version-matrix row receive-as-user-word).
    /// <para>The golden's <c>.out</c> pins the INERT half: with no message I-O neither ON EXCEPTION nor NOT ON
    /// EXCEPTION runs, where a conforming facility runs exactly one of them at each statement (§14.9.31.4 GR4/GR5,
    /// §14.9.38.4 GR2/GR3).</para></summary>
    [Fact]
    public void McsFacility_NamedNonSupportWarning_AtBothStatements_PinnedToSpec()
    {
        string source = Golden("w59_mcs_facility_witness");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, 2023);
        Assert.True(ok, $"an ACCEPT-INERT declined facility must still COMPILE: {string.Join("\n", errors)}");
        Assert.Equal(2, warnings.Count(w => w.Contains(McsNonSupport, StringComparison.Ordinal)));
    }

    /// <summary>BELOW 2023 THE FORMATS ARE AN INTRODUCTION, NOT THE FACILITY (kb/Work PB374). RECEIVE and SEND are
    /// §8.9-reserved at COBOL-85 too (the '85 communication module), so a reservation test recognized the 2023 formats
    /// at --std 85 and compiled them under the 2023 facility's COBOLNET1578. Every edition below 2023 now refuses each
    /// statement with the introduction gate, and names no facility it does not have: the negative corpus asserts the
    /// error code only, so the absence of the warning is asserted here.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    public void McsStatements_Below2023_AnswerTheIntroductionGate_NotTheFacility(int edition)
    {
        string source = Golden("pb374-async-messaging-below-2023");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok, $"the asynchronous messaging formats compiled at COBOL-{edition}");
        Assert.Equal(2, errors.Count(e => e.Contains("COBOLNET0900", StringComparison.Ordinal)));
        Assert.DoesNotContain(warnings, w => w.Contains(McsNonSupport, StringComparison.Ordinal));
    }

    /// <summary>THE SAME FACILITY'S EXCEPTION-NAMES → COBOLNET1578, ONCE PER WRITTEN NAME, AND STILL ACCEPTED (kb/Work
    /// PB374). The EC-MCS names compiled in SILENCE against a facility with no setting site; asynchronous messaging is
    /// Annex A.3 item 4, processor-dependent, so §4.2.6's warning mechanism is owed for them as for SEND and RECEIVE,
    /// and the posture is accept-inert, never the refusal the optional modules' names get. The golden writes the names
    /// at four of the six sites one funnel serves (TURN, USE, RAISE twice, a PERFORM WHEN), so the count is the
    /// assertion: a site that bypassed the funnel would drop one (feedback_two_arm_dispatch). Every warning names the
    /// exception-name it is about, and the golden's <c>.out</c> pins that the accepted names still work: the RAISE
    /// reaches the USE declarative and the WHEN phrase (§14.9.29.4 GR1).</summary>
    [Fact]
    public void McsExceptionNames_NamedNonSupportWarning_AtEverySite_AndAccepted()
    {
        string source = Golden("pb374_ec_mcs_names_accept_inert");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, 2023);
        Assert.True(ok, $"an accept-inert facility's exception-names must still COMPILE: {string.Join("\n", errors)}");
        var mcs = warnings.Where(w => w.Contains(McsNonSupport, StringComparison.Ordinal)).ToList();
        Assert.Equal(5, mcs.Count);
        Assert.Equal(3, mcs.Count(w => w.Contains("EC-MCS-INVALID-TAG", StringComparison.Ordinal)));
        Assert.Equal(2, mcs.Count(w => w.Contains("EC-MCS-MESSAGE-LENGTH", StringComparison.Ordinal)));
    }

    /// <summary>A DECLINED OPTIONAL MODULE'S EXCEPTION-NAME UNDER <c>--permissive</c> IS ACCEPTED, AND THEN IT RESOLVES
    /// (kb/Work PB374 review R1-1). COBOLNET1710 is an Error descriptor marked <c>PermissiveInert</c>, so
    /// <c>EditionContext.Declined</c> warns rather than refuses under <c>--permissive</c>; the exception-name funnel
    /// decided by a local severity test instead, warned the name as accepted, dropped it anyway, and the RAISE that
    /// wrote it died as COBOLNET2362, a refusal no diagnostic names. Each source is the strict-mode NEGATIVE that
    /// pins the refusal (so the two postures read one file), at the three sites that reach the funnel by different
    /// paths: RAISE, &gt;&gt;TURN and a USE declarative.</summary>
    [Theory]
    [InlineData("declined-ec-validate-raise", "EC-VALIDATE-CONTENT")]
    [InlineData("declined-ec-validate-turn", "EC-VALIDATE")]
    [InlineData("declined-ec-validate-use", "EC-VALIDATE-FORMAT")]
    [InlineData("declined-ec-flow-rollback-raise", "EC-FLOW-ROLLBACK")]
    [InlineData("declined-ec-flow-commit-turn", "EC-FLOW-COMMIT")]
    [InlineData("declined-ec-flow-apply-commit-use", "EC-FLOW-APPLY-COMMIT")]
    public void DeclinedModuleExceptionName_Permissive_IsAcceptedAndResolves(string negative, string name)
    {
        string source = Golden(negative);
        var (strictOk, _, _) = EditionHarness.CompileFull(source, 2023);
        Assert.False(strictOk, $"{negative} compiled under the strict posture; the refusal it pins is gone");

        var (ok, errors, warnings) = EditionHarness.CompileFull(source, 2023, permissive: true);
        Assert.True(ok, $"a PermissiveInert declined exception-name must compile under --permissive: "
                        + string.Join("\n", errors));
        Assert.Contains(warnings, w => w.Contains("COBOLNET1710", StringComparison.Ordinal)
                                       && w.Contains(name, StringComparison.Ordinal)
                                       && w.Contains("is accepted", StringComparison.Ordinal));
    }

    /// <summary>The accepted name is a REAL operand, not a tolerated one: the reviewer's own scenario, a USE declarative
    /// for EC-VALIDATE-CONTENT and a RAISE of it under <c>--permissive</c>, with checking enabled by &gt;&gt;TURN, runs
    /// the declarative (§14.9.29.4 GR1, "If exception-name-1 is specified, the associated exception condition is
    /// raised") and continues after the RAISE, the condition being NONFATAL in Table 13. The complement keeps the
    /// rule honest: EC-SCREEN's descriptor is not PermissiveInert (an inert screen statement would move the wrong
    /// data), so <c>--permissive</c> still refuses its names.</summary>
    [Fact]
    public void DeclinedModuleExceptionName_Permissive_RaiseReachesTheDeclarative_ScreenStillRefused()
    {
        const string source = """
                   >>TURN EC-VALIDATE-CONTENT CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB374PV.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   D-SEC SECTION.
                       USE AFTER EXCEPTION CONDITION EC-VALIDATE-CONTENT.
                   D-PARA.
                       DISPLAY "HANDLER".
                   END DECLARATIVES.
                   MAIN SECTION.
                   M-PARA.
                       RAISE EXCEPTION EC-VALIDATE-CONTENT.
                       DISPLAY "AFTER".
                       STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(source, 2023, permissive: true);
        Assert.True(ok, detail);
        Assert.Equal("HANDLER\nAFTER", stdout.ReplaceLineEndings("\n").TrimEnd('\n'));

        var (screenOk, screenErrors, _) = EditionHarness.CompileFull(Golden("a42-ec-screen-raise"), 2023,
                                                                     permissive: true);
        Assert.False(screenOk, "EC-SCREEN is not PermissiveInert; --permissive must not accept its names");
        Assert.Contains(screenErrors, e => e.Contains("COBOLNET1707", StringComparison.Ordinal));
    }

    /// <summary>The OPTIONS FLOAT-DECIMAL clause → COBOLNET2424 (kb/Work R43 / PB579). Annex A.3 item 13 makes
    /// the clause processor-dependent and dependent on the standard decimal floating-point usages, which are not
    /// provided (item 19); §4.2.6 ¶3 compels the warning. It is the decline that LEAKED — the clause compiled
    /// with no diagnostic at all at 2014 and 2023 until this change. The complement is asserted too: the same
    /// program with the claimed FLOAT-BINARY clause in its place draws no COBOLNET2424, so the warning is about
    /// the declined clause and not about the OPTIONS paragraph.</summary>
    [Theory]
    [InlineData(2014)]
    [InlineData(2023)]
    public void FloatDecimalClause_NamedNonSupportWarning_PinnedToSpec(int edition)
    {
        string source = Golden("w59_float_decimal_clause_witness");
        AssertRecognizedAndNamed(source, FloatDecimalClauseNonSupport, edition);

        string claimed = source.Replace("FLOAT-DECIMAL DEFAULT IS BINARY-ENCODING HIGH-ORDER-RIGHT.",
            "FLOAT-BINARY DEFAULT IS HIGH-ORDER-LEFT.", StringComparison.Ordinal);
        Assert.NotEqual(source, claimed);
        var (ok, errors, warnings) = EditionHarness.CompileFull(claimed, edition);
        Assert.True(ok, string.Join("\n", errors));
        Assert.DoesNotContain(warnings, w => w.Contains(FloatDecimalClauseNonSupport, StringComparison.Ordinal));
    }

    /// <summary>EVERY legal spelling of the declined FLOAT-DECIMAL clause reaches the COBOLNET2424 warning — never a
    /// parse error (kb/Work PB1508, wave 59 H; owner decision R43 item 5: a decline that diagnoses legal source
    /// wrongly is a leak). §11.9.9.2's phrases are enclosed in CHOICE INDICATORS, and §5.2.6.4 says "The
    /// alternatives may be specified in any order": the endianness-first order was refused with an anonymous
    /// COBOL0307 by a second, ORDERED copy of the USAGE clause's phrase group.</summary>
    [Theory]
    [InlineData("FLOAT-DECIMAL IS HIGH-ORDER-RIGHT DECIMAL-ENCODING.")]
    [InlineData("FLOAT-DECIMAL DEFAULT HIGH-ORDER-LEFT BINARY-ENCODING.")]
    [InlineData("FLOAT-DECIMAL IS DECIMAL-ENCODING.")]
    [InlineData("FLOAT-DECIMAL HIGH-ORDER-LEFT.")]
    public void FloatDecimalClause_EveryPhraseOrder_ReachesTheWarning_NotAParseError(string clause)
    {
        string source = Golden("w59_float_decimal_clause_witness")
            .Replace("FLOAT-DECIMAL DEFAULT IS BINARY-ENCODING HIGH-ORDER-RIGHT.", clause, StringComparison.Ordinal);
        Assert.Contains(clause, source, StringComparison.Ordinal);
        AssertRecognizedAndNamed(source, FloatDecimalClauseNonSupport, 2014, 2023);
    }

    /// <summary>One arm of the RECORD DELIMITER decline, at one edition: the source COMPILES (accept-inert) and
    /// a COBOLNET1778 warning quotes back <paramref name="armSpelling"/>. Reads the corpus golden, so the
    /// inertness half and the decline half never diverge.</summary>
    private static void AssertArmNamed(int edition, string armSpelling)
    {
        string source = Golden("pb292_record_delimiter_witness");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
        Assert.True(ok, $"--std {edition}: an ACCEPT-INERT declined facility must still COMPILE "
            + $"(docs/CONFORMANCE.md §1): {string.Join("\n", errors)}");
        var armed = warnings.Where(w => w.Contains(RecordDelimiterNonSupport, StringComparison.Ordinal)
                                        && w.Contains(armSpelling, StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(armed.Count > 0, $"--std {edition}: expected a {RecordDelimiterNonSupport} warning naming "
            + $"'{armSpelling}' (ISO §4.2.6 ¶3 — the warning mechanism is mandatory for a syntactically-"
            + $"detectable unsupported processor-dependent element); got:\n"
            + string.Join("\n", warnings.DefaultIfEmpty("(none)")));
    }
}
