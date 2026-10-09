// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Editions;

/// <summary>
/// One per-edition reserved-word row (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.4): the
/// four-edition reservation flags for a word, its confidence, and its provenance. Rows come from the generated
/// table (<c>ReservedWords.Table.cs</c>, emitted by <c>scripts/gen-reserved-words.ps1</c> from the in-repo ISO
/// 2023 §8.9 list + VCR row 32 + the GnuCOBOL per-standard 85/2002/2014 word lists — derived facts with
/// provenance); <c>tests/version-matrix/reserved-words.json</c> is the same data in its canonical form and a
/// drift test asserts they agree both directions.
/// </summary>
/// <param name="Word">The word, uppercase.</param>
/// <param name="R85">Reserved in ANSI X3.23-1985.</param>
/// <param name="R2002">Reserved in ISO/IEC 1989:2002.</param>
/// <param name="R2014">Reserved in ISO/IEC 1989:2014.</param>
/// <param name="R2023">Reserved in ISO/IEC 1989:2023 (§8.9).</param>
/// <param name="Confidence">"high" = the row may REJECT; anything lower is present but inert (the conservative
/// policy — a wrong entry must never reject a valid program; VCR scope-limit rule).</param>
/// <param name="Provenance">Where the classification comes from (spec §, annex item, or the documented
/// presumption pending older-standard evidence — the decision-1 provisional policy).</param>
public sealed record ReservedWordEntry(
    string Word, bool R85, bool R2002, bool R2014, bool R2023, string Confidence, string Provenance)
{
    /// <summary>Whether the word is reserved at <paramref name="edition"/> (85/2002/2014/2023).</summary>
    public bool IsReservedAt(int edition) => edition switch
    {
        85 => R85,
        2002 => R2002,
        2014 => R2014,
        _ => R2023,
    };
}

/// <summary>The generated §8.9 word table (the partial half is <c>ReservedWords.Table.cs</c>).</summary>
public static partial class ReservedWords
{
    private static Dictionary<string, ReservedWordEntry>? _byWord;

    /// <summary>Look up <paramref name="word"/> in the generated table, keyed by the Annex C fold
    /// (<see cref="CobolNames.Comparer"/>, kb/Work PB1965), so any spelling of the word finds its row.</summary>
    public static ReservedWordEntry? Find(string word) =>
        (_byWord ??= Entries.ToDictionary(e => e.Word, CobolNames.Comparer)).GetValueOrDefault(word);
}

/// <summary>
/// The per-compilation-unit EFFECTIVE reserved-word set — the seam the validator consults, never the raw table
/// (roadmap ISO-validation D9): the 2023 COBOL-WORDS directive (ISO Annex E.3.3 item 12) adds/removes/renames
/// words per compilation group, so the generated table is only the DEFAULT layer. Today the default is the only
/// layer; the COBOL-WORDS pass (roadmap Phase 7) composes modified instances over it.
/// </summary>
public sealed class ReservedWordSet
{
    /// <summary>The default set: exactly the generated table.</summary>
    public static ReservedWordSet Default { get; } = new();

    // The 2023 >>COBOL-WORDS overlay (ISO §7.3.10.4): RESERVE adds a word (GR5), UNDEFINE / SUBSTITUTE remove one
    // (GR3/GR4). Null on the Default set (no directive) so the effective behavior is byte-identical.
    private readonly IReadOnlySet<string>? _reserved;    // RESERVE literal-6 — a NEW reserved word
    private readonly IReadOnlySet<string>? _suppressed;  // UNDEFINE literal-3 + SUBSTITUTE literal-4 — de-reserved

    private ReservedWordSet() { }

    private ReservedWordSet(IReadOnlySet<string> reserved, IReadOnlySet<string> suppressed)
    {
        _reserved = reserved;
        _suppressed = suppressed;
    }

    /// <summary>Compose the effective per-compilation-group set from a <see cref="CobolWordsMap"/> override (ISO
    /// §7.3.10). An empty map yields <see cref="Default"/> (byte-identical). RESERVE words become reserved;
    /// UNDEFINE / SUBSTITUTE words are de-reserved (SR5 forbids a word being both, so no conflict).</summary>
    public static ReservedWordSet Compose(CobolWordsMap map) =>
        map.IsEmpty ? Default : new ReservedWordSet(map.Reserved, map.DeReserved);

    /// <summary>The entry for <paramref name="upperWord"/>, or null when the word is not in the effective set.</summary>
    public ReservedWordEntry? Find(string upperWord) => ReservedWords.Find(upperWord);

    /// <summary>True when <paramref name="upperWord"/> is reserved for the compilation group and may emit
    /// COBOLNET0901 when used as a user-defined word: a <c>&gt;&gt;COBOL-WORDS</c> UNDEFINE/SUBSTITUTE suppression
    /// wins (never rejects), then a RESERVE overlay reserves unconditionally, else the generated §8.9 table's
    /// HIGH-CONFIDENCE reserved-at-edition rule (the conservative policy).</summary>
    public bool RejectsAt(string upperWord, int edition)
    {
        if (_suppressed?.Contains(upperWord) == true) return false;   // UNDEFINE/SUBSTITUTE — a user word now
        if (_reserved?.Contains(upperWord) == true) return true;      // RESERVE — a new reserved word
        return Find(upperWord) is { Confidence: "high" } e && e.IsReservedAt(edition);
    }

    /// <summary>
    /// WHY <paramref name="upperWord"/> rejects at <paramref name="edition"/> — the availability of the construct
    /// "this spelling used as a user-defined word", so the ONE
    /// <see cref="EditionSeverityPolicy"/> decides the severity axis instead of the emit site asserting it.
    /// <para>⛔ THE DISTINCTION IS NOT COSMETIC, and hard-coding <see cref="ConstructAvailability.Removed"/> got
    /// it wrong for every RE-RESERVED word. A spelling an edition TOOK AWAY (COMMIT: user-definable until 2023
    /// reserved it) is the migration case <c>--permissive</c> exists for — an existing program legitimately
    /// contains it. A spelling that was reserved at the target edition AND at every edition before it
    /// (RECEIVE / END-RECEIVE at COBOL-85, where the '85 communication module owns them) was NEVER a user word
    /// there, so no conforming program of that vintage can contain one and there is nothing to migrate: it is
    /// <see cref="ConstructAvailability.NotYetIntroduced"/>, an error on BOTH axes (CA14's policy, swept).</para>
    /// </summary>
    public ConstructAvailability UserWordVerdictAt(string upperWord, int edition)
    {
        if (!RejectsAt(upperWord, edition)) return ConstructAvailability.Available;
        // A >>COBOL-WORDS RESERVE (ISO §7.3.10.4 GR5) is the PROGRAM's own reservation, not the edition's: the
        // spelling was a user word until this compilation group's directive took it, which is the removed shape.
        if (_reserved?.Contains(upperWord) == true) return ConstructAvailability.Removed;
        if (Find(upperWord) is not { } e) return ConstructAvailability.Removed;
        foreach (int older in EditionInfo.Before(edition))
            if (!e.IsReservedAt(older)) return ConstructAvailability.Removed;
        return ConstructAvailability.NotYetIntroduced;
    }

    /// <summary>⛔ THE §8.9 USER-DEFINED-WORD SLOT ADMISSION RULE (kb/Work PB693, PB792) — true when a compile at
    /// <paramref name="edition"/> may read <paramref name="upperWord"/> as a user-defined word. The frontend's
    /// token-level reservation gate (<c>CobolParserCoreBase.userWordHere</c> → <c>ReservationGateRewriter</c>,
    /// kb/Work PB655) IS this predicate, so the gate and the §8.9 funnel cannot disagree about which occurrences are names (§8.3.2.1 rule 1:
    /// "Reserved words shall not be used as user-defined words or system-names").
    /// <para>⛔ THE MIGRATION MODE IS NOT A BLANKET EXEMPTION, and writing it as one (<c>Permissive || …</c> in the
    /// parser) was kb/Work PB792. <c>--permissive</c> "accepts constructs the targeted edition REMOVED", so it
    /// restores the pre-removal reading of a spelling §8.9 TOOK AWAY — <see cref="ConstructAvailability.Removed"/>,
    /// where an existing program legitimately contains the word as a name. A spelling reserved at this edition AND
    /// at every older edition this compiler targets (COLUMN and DESTINATION are reserved at all four) was never a
    /// user word anywhere, so migration mode has nothing to restore: the verdict is
    /// <see cref="ConstructAvailability.NotYetIntroduced"/>, an error on BOTH axes, and admitting the word let every
    /// greedy operand list absorb it — <c>03 VALUE "DETAIL LINE " COLUMN 20 PIC X(12).</c> (NIST RW104A card
    /// 024500, legal by §13.15.3 SR2 "All other clauses may be written in any order") lost its §13.18.14 COLUMN
    /// clause and printed in the wrong place, silently.</para>
    /// <para>The axis therefore routes through the ONE <see cref="EditionSeverityPolicy"/>: the slot admits the
    /// word exactly where a §8.9 violation here would be DOWNGRADED to a warning, and never where the policy says
    /// error. Allocation-free (<see cref="EditionInfo.Before"/> is a span) — it runs inside ANTLR's speculative
    /// prediction.</para></summary>
    public bool AdmitsAsUserWord(string upperWord, EditionInfo edition)
        // Confidence-blind, exactly as the gate has always been: a reserved row that may not REJECT (the
        // conservative policy) still yields Available below, so the gate and the funnel agree by construction.
        => Find(upperWord) is not { } e
        || !e.IsReservedAt(edition.Year)
        || EditionSeverityPolicy.For(UserWordVerdictAt(upperWord, edition.Year), edition)
           is not EditionSeverity.Error;

    /// <summary>⛔ THE ONE §8.9 user-word-violation SENTENCE (kb/Work PB693). TWO stages report it and they must
    /// say the same thing: the bound-tree funnel (<c>VersionConformancePass.FlagReservedUserWord</c>), and the
    /// PARSER's error listener for the occurrences the reservation gate makes unparseable — a REFERENCE to the
    /// word, where no name-slot alternative can match and a raw COBOL0001 would never name §8.9. It lives here,
    /// in the assembly both stages already reference, so the wording, the code and the clause have one
    /// definition (<c>feedback_one_rule_one_place</c>).</summary>
    public static string UserWordViolationMessage(string upperWord, int edition)
        => $"'{upperWord}' is a reserved word in COBOL-{edition} and cannot be used as a "
           + "user-defined word (ISO §8.9)";
}
