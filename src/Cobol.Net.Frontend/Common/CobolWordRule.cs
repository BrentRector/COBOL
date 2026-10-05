// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Frontend.Common;

/// <summary>
/// ⛔ THE §8.3.2.1 WORD-LENGTH CEILING, IN ONE PLACE — because it was in one place (the VersionConformancePass
/// <c>VisitCobolWord</c> funnel, which walks the MAIN parse tree) and that place cannot see DIRECTIVE-carried
/// words: a 44-character exception-name in <c>&gt;&gt;TURN</c> and a 44-character compilation-variable-name in
/// <c>&gt;&gt;DEFINE</c> both compiled clean at <c>--std 2002</c> while the same word in a RAISE statement was
/// correctly rejected (found by kb/Work R05's own conformance fact — the evidence ledger's "correctly rejected
/// with COBOLNET1567" was measured on the statement spelling only). §8.3.2.1 covers every COBOL word — "a
/// compiler-directive word, a context-sensitive word, an intrinsic-function-name, a reserved word, a
/// system-name, or a user-defined word" — so the directive stages enforce the SAME rule through the SAME text,
/// reporting on their own channels. (&gt;&gt;COBOL-WORDS asks it of the fresh word of each entry — SR4 requires "a
/// COBOL word that meets the requirements for a user-defined data-name" — because a word no statement uses never
/// reaches the tree funnel; kb/Work PB1373.)
///
/// <para>The ceiling: 63 at COBOL-2023 (Annex E.3.3 item 11 — a RELAXATION, so firing below 2023 for a 32..63
/// word is a length error, not an introduction gate), 31 at 2002/2014, 30 at 1985. Above 63 is a hard cap at
/// every edition. The CHARACTER half of §8.3.2.1 is here too (kb/Work PB1402): from 2002 each character outside the
/// basic repertoire must be where the edition's Annex B.3 permits it (<see cref="CharacterViolation"/>, COBOLNET2773);
/// the below-2002 gates of the underscore and the extended letters stay in the tree funnel, because every
/// <c>&gt;&gt;</c> directive is itself rejected below 2002, so neither can reach a directive word.</para>
/// </summary>
public static class CobolWordRule
{
    /// <summary>The §8.3.2.1 maximum COBOL-word length for a targeted edition.</summary>
    public static int MaxLength(int dialectLevel) =>
        dialectLevel >= 2023 ? 63 : dialectLevel >= 2002 ? 31 : 30;

    /// <summary>The COBOLNET1567 message when <paramref name="word"/> exceeds the ceiling; null when legal.
    /// One text for every reporting channel — the tree funnel and the directive stages. The length is counted in
    /// CHARACTERS, not UTF-16 units (<see cref="CobolCharacterRepertoire.LengthInCharacters"/>): a supplementary-plane
    /// letter is one character, a combining character is one of its own (§8.1.3.2 GR4 c), kb/Work PB1402).</summary>
    public static string? LengthViolation(string word, int dialectLevel)
    {
        int length = CobolCharacterRepertoire.LengthInCharacters(word);
        return length > MaxLength(dialectLevel)
            ? $"the COBOL word '{word}' is {length} characters, exceeding the "
              + $"{MaxLength(dialectLevel)}-character maximum for COBOL-{dialectLevel} "
              + "(ISO §8.3.2.1 — COBOL-2023 raised the limit to 63)"
            : null;
    }

    /// <summary>The COBOLNET2773 message when a character of <paramref name="word"/> is not where the edition's Annex B
    /// permits it (<see cref="CobolCharacterRepertoire.Violation"/>); null when every character is legal. Asked only
    /// from COBOL 2002, which introduced extended letters: below it ANY character outside the basic repertoire is the
    /// construct gate <c>user-word-extended-letter-2002</c>, which the caller asks instead (kb/Work PB1402).</summary>
    public static string? CharacterViolation(string word, int dialectLevel) =>
        dialectLevel >= 2002 ? CobolCharacterRepertoire.Violation(word, dialectLevel) : null;

    /// <summary>True when <paramref name="word"/>'s character violation at <paramref name="dialectLevel"/> is one the
    /// COBOL 2002/2014 Annex B did not have — Annex E.2 item 4 deleted U+037A and made U+30FB medial at 2023 — so the
    /// word is the REMOVED construct <c>user-word-character-removed-2023</c> (COBOLNET0902; a warning that keeps the 2014
    /// rule under <c>--permissive</c>) rather than COBOLNET2773.</summary>
    public static bool RemovedAt2023(string word, int dialectLevel) =>
        dialectLevel >= 2023 && CobolCharacterRepertoire.Violation(word, 2014) is null;

    /// <summary>Every §8.3.2.1 violation of a DIRECTIVE-carried word (a <c>&gt;&gt;DEFINE</c> name, a <c>&gt;&gt;TURN</c>
    /// operand) — the length and the Annex B characters — as (code, message), for the directive stages to report on
    /// their own channels. Every directive is itself a COBOL-2002 introduction, so the below-2002 extended-letter gate
    /// never applies to one. A 2023 removal (<see cref="RemovedAt2023"/>) carries the removed-construct code, as the
    /// tree funnel's does; the directive channels report errors only.</summary>
    public static IEnumerable<(string Code, string Message)> DirectiveWordViolations(string word, int dialectLevel)
    {
        if (LengthViolation(word, dialectLevel) is { } length)
            yield return (DiagnosticCatalog.WordLengthExceeded.Code, length);
        if (CharacterViolation(word, dialectLevel) is { } character)
            yield return (RemovedAt2023(word, dialectLevel) ? EditionCodes.RemovedConstruct : DiagnosticCatalog.WordCharacterNotPermitted.Code,
                character);
    }
}
