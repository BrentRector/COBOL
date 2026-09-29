// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE READER OF A GENERAL FORMAT'S CHOICE INDICATORS — ISO §5.2.6.4 — for the half a grammar cannot
/// express.
///
/// <para><b>What the figure says.</b> A brace or bracket carrying choice indicators (the `|` bars just inside
/// it) means: "When enclosed by brackets, zero or more of the alternatives contained within the choice
/// indicators shall be specified, but any single alternative may be specified only once" (braces: one or more),
/// and "The alternatives may be specified in any order." A parser rule can say ZERO-OR-MORE and ANY ORDER with
/// one <c>*</c>; it can say ONLY ONCE only by enumerating every ordering, which is factorial in the number of
/// alternatives and unreadable at three. So the grammar writes <c>(a | b | c)*</c> and the ONLY-ONCE half is
/// read here, once, by every format that has such a group.</para>
///
/// <para><b>Why it is a type and not an <c>if</c> at the verb</b> (kb/Work PB407, CLAUDE.md rule 5). The GOBACK
/// tail was written <c>(raisingPhrase | statusPhrase)?</c> — an ordered at-most-ONE stack — so four legal
/// spellings were refused as syntax errors. Relaxing that one rule to <c>*</c> and writing "if both, error" at
/// the GOBACK binder would make the NEXT figure with choice indicators need its own second copy. §14.9.18.2 is
/// not the only such figure; §14.9.20.2's category-name brace, §13.18.13.2's FOR ALPHANUMERIC / FOR NATIONAL
/// pair and §12.3.6.2's SOURCE-COMPUTER clauses each read the same rule, and each wrote it out again.</para>
/// </summary>
internal static class ChoiceIndicators
{
    /// <summary>Take the ONE occurrence of a choice-indicator alternative that a general format admits, and
    /// diagnose a repeat (§5.2.6.4, "any single alternative may be specified only once"). Returns the FIRST
    /// occurrence so binding continues on a shape the rest of the binder can read; null when the alternative
    /// was not specified at all — which the enclosing BRACKET makes legal, and which is why this returns a
    /// nullable rather than demanding one.</summary>
    /// <param name="edition">The one diagnostic sink.</param>
    /// <param name="occurrences">Every parse of this alternative, in written order (ANTLR's array for a
    /// <c>*</c>/<c>+</c> sub-rule).</param>
    /// <param name="statement">The statement as the user wrote it, for the message ("GOBACK").</param>
    /// <param name="alternative">The alternative as the figure names it ("the RAISING phrase").</param>
    /// <param name="clause">The clause whose general format this is ("14.9.18.2"), so the message cites the
    /// FIGURE as well as §5.2.6.4.</param>
    public static T? AtMostOnce<T>(EditionContext edition, T[] occurrences,
                                   string statement, string alternative, string clause)
        where T : ParserRuleContext
    {
        if (occurrences.Length > 1)
            edition.Error(DiagnosticCatalog.ChoiceAlternativeRepeated,
                $"{statement}: {alternative} is specified {occurrences.Length} times; ISO §{clause}'s general "
                + "format encloses it in choice indicators, and §5.2.6.4 admits any single alternative only "
                + "once (in any order, but once)");
        return occurrences.Length > 0 ? occurrences[0] : null;
    }

    /// <summary>⛔ THE ONE READER of the FOR ALPHANUMERIC / FOR NATIONAL choice pair (kb/Work PB1075). Five general
    /// formats print <c>{ | FOR ALPHANUMERIC IS x-1 | FOR NATIONAL IS x-2 | }</c> — a brace with choice indicators —
    /// as the alternative to a positional <c>IS x-1 [x-2]</c> form: the PROGRAM COLLATING SEQUENCE and CHARACTER
    /// CLASSIFICATION clauses (§12.3.6.2), the file control entry's file-level COLLATING SEQUENCE clause
    /// (§12.4.5.7.2 Format 1), the SORT/MERGE COLLATING SEQUENCE phrase (§14.9.40.2 / §14.9.24.2) and the CODE-SET
    /// clause (§13.18.13.2). Each used to walk its FOR phrases itself: four reported a repeat under their own
    /// messages (two of them the bare string COBOLNET0898) and the file clause let the LAST repeat win in silence.
    /// A repeat is §5.2.6.4's violation, read by <see cref="AtMostOnce{T}"/>; the FIRST phrase of each class is
    /// returned, so binding continues on the first as written.</summary>
    /// <param name="edition">The one diagnostic sink.</param>
    /// <param name="forPhrases">The FOR phrases as parsed (the grammar's <c>+</c> superset), in written order.</param>
    /// <param name="isNational">Whether a FOR phrase is the FOR NATIONAL alternative.</param>
    /// <param name="statement">The clause as the user wrote it, for the message.</param>
    /// <param name="clause">The subclause whose general format prints the pair.</param>
    public static (T? Alphanumeric, T? National) ForPhrasePair<T>(EditionContext edition, T[] forPhrases,
        Func<T, bool> isNational, string statement, string clause) where T : ParserRuleContext
    {
        var alnum = AtMostOnce(edition, forPhrases.Where(f => !isNational(f)).ToArray(), statement,
            "the FOR ALPHANUMERIC phrase", clause);
        var nat = AtMostOnce(edition, forPhrases.Where(isNational).ToArray(), statement, "the FOR NATIONAL phrase", clause);
        return (alnum, nat);
    }

    /// <summary>The alphabet-name pair of the four formats among <see cref="ForPhrasePair{T}"/>'s five whose operands
    /// are alphabet-names (every one but CHARACTER CLASSIFICATION): the FOR phrases when any is written, else the IS
    /// form's alphabet-name-1 [alphabet-name-2].</summary>
    /// <param name="edition">The one diagnostic sink.</param>
    /// <param name="forPhrases">The FOR phrases as parsed, in written order.</param>
    /// <param name="isNational">Whether a FOR phrase is the FOR NATIONAL alternative.</param>
    /// <param name="alphabetOf">The alphabet-name a FOR phrase names.</param>
    /// <param name="isForm">The IS form's alphabet-name words (alphabet-name-1, then alphabet-name-2), read when no FOR
    /// phrase is written.</param>
    /// <param name="statement">The clause as the user wrote it, for the message.</param>
    /// <param name="clause">The subclause whose general format prints the pair.</param>
    public static (string? Alphanumeric, string? National) AlphabetPair<T>(EditionContext edition, T[] forPhrases,
        Func<T, bool> isNational, Func<T, string> alphabetOf, IReadOnlyList<ParserRuleContext> isForm,
        string statement, string clause) where T : ParserRuleContext
    {
        if (forPhrases.Length == 0)
            return (isForm.Count > 0 ? isForm[0].GetText() : null, isForm.Count > 1 ? isForm[1].GetText() : null);
        var (alnum, nat) = ForPhrasePair(edition, forPhrases, isNational, statement, clause);
        return (alnum is null ? null : alphabetOf(alnum), nat is null ? null : alphabetOf(nat));
    }
}
