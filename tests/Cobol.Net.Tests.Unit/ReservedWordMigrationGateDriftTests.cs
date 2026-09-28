// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Editions;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE MIGRATION MODE IS NOT A BLANKET EXEMPTION FROM THE §8.9 RESERVATION GATE (kb/Work PB792).
/// <para><c>--permissive</c> "accepts constructs the targeted edition REMOVED, warning instead of rejecting", so
/// it restores the pre-removal reading of a spelling ISO §8.9 TOOK AWAY (<c>ConstructAvailability.Removed</c> —
/// an existing program legitimately contains the word as a name). A spelling reserved at the compile edition AND
/// at every OLDER edition this compiler targets was never a user-defined word anywhere, so migration mode has
/// nothing to restore; §8.3.2.1 rule 1 ("Reserved words shall not be used as user-defined words or system-names")
/// held then as it holds now, and the verdict is <c>NotYetIntroduced</c> — an error on BOTH axes.</para>
/// <para>The gate was written as <c>Edition.Permissive || !reservedHere(w)</c>, which granted the migration
/// exemption to all 59 gated words including the ones no edition ever left free. The measured cost: at
/// <c>--std 2023 --permissive</c> the NIST report-writer program RW104A's card 024500 —
/// <c>03 VALUE "DETAIL LINE " COLUMN 20 PIC X(12).</c>, legal because §13.15.3 SR2 says "All other clauses may be
/// written in any order" — had COLUMN and 20 absorbed into the VALUE operand list, lost its §13.18.14 COLUMN
/// clause, and printed the detail field at column 1 instead of column 20 with no diagnostic at all.</para>
/// <para>Both tests are DERIVED from <c>tests/version-matrix/reserved-words.json</c>, so the next word §8.9
/// reserves at every edition is covered by construction and needs no edit here (CLAUDE.md rule 5 — never a
/// hand-maintained list where a structure belongs).</para>
/// </summary>
public sealed class ReservedWordMigrationGateDriftTests
{
    private static readonly int[] Editions = [85, 2002, 2014, 2023];

    /// <summary>⛔ THE RULE, over every row of the §8.9 table and every edition and both severity axes.
    /// <see cref="ReservedWordSet.AdmitsAsUserWord"/> is the ONE decision the parser's <c>cobolWord</c> gate
    /// (<c>CobolParserCoreBase.userWordHere</c>) makes, and it must equal: the word is not reserved here, OR the
    /// row may not reject at all (the conservative confidence policy — the funnel would report nothing, so the
    /// gate must not silently reject either), OR this is a permissive compile AND some older edition left the
    /// spelling free. A blanket permissive bypass fails on every always-reserved row; a gate that forgot the
    /// migration mode fails on every row an edition added.</summary>
    [Fact]
    public void AdmitsAsUserWord_GrantsTheMigrationExemption_OnlyWhereAnOlderEditionLeftTheWordFree()
    {
        var reserved = CobolWordsDriftTests.LoadReservedIntervals();
        var confidence = CobolWordsDriftTests.LoadConfidence();
        Assert.NotEmpty(reserved);

        var failures = new List<string>();
        int checkedPairs = 0, failed = 0, exemptionsGranted = 0, exemptionsRefused = 0;
        foreach (var (word, flags) in reserved)
        {
            bool mayReject = confidence.TryGetValue(word, out string? c) && c == "high";
            for (int i = 0; i < Editions.Length; i++)
                foreach (bool permissive in (bool[])[false, true])
                {
                    // Removed = §8.9 took the spelling away at some point at or before this edition.
                    bool removed = flags[..i].Contains(false);
                    bool expected = !flags[i] || !mayReject || (permissive && removed);
                    bool actual = ReservedWordSet.Default.AdmitsAsUserWord(word, EditionInfo.Of(Editions[i], permissive));
                    checkedPairs++;
                    if (flags[i] && mayReject && permissive) { if (removed) exemptionsGranted++; else exemptionsRefused++; }
                    if (actual == expected) continue;
                    failed++;
                    if (failures.Count < 8)
                        failures.Add($"{word}@{Editions[i]}{(permissive ? " --permissive" : "")}: expected "
                                     + $"admits={expected}, got {actual}");
                }
        }

        Assert.True(failed == 0,
            $"§8.9 user-word admission drift: {failed} of {checkedPairs} (word, edition, axis) triples "
            + $"disagree with the rule; first {failures.Count}: " + string.Join(" | ", failures));
        // The population assertion (feedback_verdict_evidence_invariant): a rule that granted — or refused —
        // EVERY permissive exemption would satisfy the loop above vacuously if the expectation were derived the
        // same wrong way. Both arms must be non-empty, and they are the two halves of kb/Work PB792.
        Assert.True(exemptionsGranted > 0 && exemptionsRefused > 0,
            $"the probe is not discriminating: granted={exemptionsGranted} refused={exemptionsRefused}");
    }

    // ── The behavioural half: a greedy operand list must not absorb the word under --permissive ──────────────

    /// <summary>A report group description entry whose VALUE clause is followed by <c>{0}</c>. It carries a COLUMN
    /// clause because §13.15.3 SR13 requires one in every elementary entry with a VALUE clause (kb/Work PB853 made
    /// that rule live, and the COLUMN-less control stopped compiling). The VALUE operand
    /// list (<c>valueItem</c>, ISO §13.18.63.2 format 4 — <c>{VALUE IS|VALUES ARE} {literal-1} …</c>, one or MORE
    /// literals, so the list is deliberately greedy and must NOT be narrowed) bottoms out at <c>cobolWord</c>,
    /// which is the user-defined-word slot §8.3.2.1 rule 1 governs. When the gate admits an always-reserved word
    /// the loop swallows it and the clause it begins vanishes; when the gate holds, the word has no reading here
    /// and the entry is refused — identically on both severity axes, because the migration mode has nothing to
    /// restore for such a word.
    /// <para>The entry writes COLUMN because §13.15.3 SR13 requires it of every elementary report entry with a VALUE
    /// clause — the control used to omit it, and became non-conforming source the day kb/Work PB853 enforced SR13
    /// (COBOLNET2247).</para></summary>
    private const string ProbeTemplate = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB792D{0}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT PRT ASSIGN TO "pb792drift.txt".
        DATA DIVISION.
        FILE SECTION.
        FD PRT REPORT IS R-DRIFT.
        REPORT SECTION.
        RD R-DRIFT PAGE LIMIT 20 LINES.
        01 DET TYPE DE LINE PLUS 1.
           03 COLUMN 1 VALUE "AB"{1} PIC X(2).
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT PRT.
            INITIATE R-DRIFT.
            GENERATE DET.
            TERMINATE R-DRIFT.
            CLOSE PRT.
            STOP RUN.
        """;

    private static bool ProbeCompiles(string dir, string tag, string wordOrEmpty, int edition, bool permissive)
    {
        string src = Path.Combine(dir, $"p{tag}.cob");
        File.WriteAllText(src, string.Format(ProbeTemplate, tag,
            wordOrEmpty.Length == 0 ? "" : " " + wordOrEmpty));
        return CompilerDriver.Compile(new CompilerDriver.Options(
            src, Path.Combine(dir, $"p{tag}.dll"), DialectLevel: edition, Permissive: permissive,
            CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto)).Success;
    }

    /// <summary>⛔ THE REGRESSION THAT LET RW104A PRINT IN THE WRONG PLACE. For every reservation-gated word and
    /// every edition at which §8.9 reserves it AND reserved it at every older edition, the greedy VALUE operand
    /// list must reach the SAME verdict strict and permissive — and must refuse the word, because it is not a
    /// user-defined word at any edition this compiler targets. MEASURED against the blanket bypass restored: the
    /// permissive arm accepted 15 of the 31 such (word, edition) pairs, silently — the other 16 survive only
    /// because ANTLR's lookahead happens to reject the swallowed reading for those spellings, which is luck, not
    /// a gate.
    /// <para>The control program (the same entry with no extra word) proves the probe is not merely broken —
    /// without it a template typo would make every case "reject" and the test would be a false green
    /// (feedback_green_gates_arent_evidence).</para></summary>
    [Fact]
    public void AlwaysReservedGatedWord_IsRefusedByAGreedyOperandList_UnderPermissiveToo()
    {
        var reserved = CobolWordsDriftTests.LoadReservedIntervals();
        var gate = CobolWordsDriftTests.DerivedGateSet();
        Assert.NotEmpty(gate);

        string dir = Directory.CreateTempSubdirectory("pb792drift").FullName;
        try
        {
            Assert.True(ProbeCompiles(dir, "CTL", "", 85, permissive: false),
                "the control report entry does not compile at --std 85 — the probe template is broken, not the gate");
            Assert.True(ProbeCompiles(dir, "CTLP", "", 2023, permissive: true),
                "the control report entry does not compile at --std 2023 --permissive — the probe template is broken");

            var failures = new List<string>();
            int pairs = 0, failed = 0, tag = 0;
            foreach (string token in gate.OrderBy(t => t, StringComparer.Ordinal))
            {
                string word = CobolWordsDriftTests.ToWord(token);
                if (!reserved.TryGetValue(word, out var flags)) continue;
                for (int i = 0; i < Editions.Length; i++)
                {
                    // Reserved here and at every older edition => NotYetIntroduced => never a user word.
                    if (!flags[i] || flags[..i].Contains(false)) continue;
                    pairs++;
                    tag++;
                    bool strict = ProbeCompiles(dir, $"S{tag}", word, Editions[i], permissive: false);
                    bool permissive = ProbeCompiles(dir, $"P{tag}", word, Editions[i], permissive: true);
                    if (!strict && !permissive) continue;
                    failed++;
                    if (failures.Count < 8)
                        failures.Add($"{word}@{Editions[i]}: strict accepts={strict}, permissive accepts={permissive}");
                }
            }

            Assert.True(pairs > 0, "no always-reserved gated word found — the derivation broke, not the gate");
            Assert.True(failed == 0,
                $"the VALUE operand list absorbed an always-reserved word ({failed} of {pairs} "
                + $"(word, edition) pairs; §8.3.2.1 rule 1, kb/Work PB792); first {failures.Count}: "
                + string.Join(" | ", failures));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }
}
