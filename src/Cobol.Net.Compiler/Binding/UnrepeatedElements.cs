// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE READER OF "WRITTEN ONCE" FOR A GENERAL-FORMAT ELEMENT THAT CARRIES NO ELLIPSIS — ISO §5.2.6.2 /
/// §5.2.7 — the <see cref="ChoiceIndicators"/> twin for a format whose elements are separate brackets rather
/// than one choice-indicator group.
///
/// <para><b>What the figure says.</b> A bracket "indicate[s] that the syntax element contained within the
/// brackets … may be explicitly specified or that portion of the general format may be omitted" (§5.2.6.2), and
/// repetition exists only where an ellipsis stands after a delimiter (§5.2.7). A syntax rule that lets the
/// elements be written in ANY ORDER — §13.14.3 SR2 for the report description entry's clauses, §13.18.39.3 SR4
/// for the PAGE clause's phrases — is an order licence, not a repetition licence. A parser rule can say
/// "any order" with one <c>*</c> and "each once" only by enumerating every ordering, so the grammar writes the
/// <c>*</c> and the "each once" half is read HERE (kb/Work PB483's sibling sweep: <c>PAGE LIMIT 30 LINES
/// HEADING 1 HEADING 2</c> kept the second value silently, and two CONTROL clauses silently concatenated into one
/// hierarchy).</para>
///
/// <para>Callers count the occurrences of each element in written order and hand the count here, so the message
/// and the rule are written down once for every format that reads them.</para>
/// </summary>
internal static class UnrepeatedElements
{
    /// <summary>Diagnose an element written <paramref name="count"/> &gt; 1 times (COBOLNET2423).</summary>
    /// <param name="edition">The one diagnostic sink.</param>
    /// <param name="count">How many times the element was written.</param>
    /// <param name="where">The construct as the user wrote it ("RD 'R-1'").</param>
    /// <param name="element">The element as the figure names it ("the CONTROL clause").</param>
    /// <param name="clause">The clause whose general format this is ("13.14.2").</param>
    /// <param name="orderFree">Whether a rule of the format lets its elements be written in any order (§13.14.3 SR2);
    /// false where the figure's printed sequence binds (§5.2.1 — the OPTIONS paragraph, the CONFIGURATION SECTION),
    /// so the message never grants an order licence the format does not (kb/Work PB1508).</param>
    public static void AtMostOnce(EditionContext edition, int count, string where, string element, string clause,
        bool orderFree = true)
    {
        if (count > 1)
            edition.Error(DiagnosticCatalog.FormatElementRepeated,
                $"{where}: {element} is written {count} times; ISO §{clause}'s general format encloses it in its "
                + "own bracket with no ellipsis, so it may be written at most once (§5.2.6.2, §5.2.7)"
                + (orderFree ? " — the elements may be written in any order, but each once"
                             : " — once, in the sequence the format prints (§5.2.1)"));
    }
}
