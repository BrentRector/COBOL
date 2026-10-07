// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// A resolved procedure range — the ISO §14.9.28.4 GR4 <i>specified set of statements</i> of an out-of-line
/// PERFORM, and the same value for every other construct that names one (a SORT/MERGE INPUT/OUTPUT PROCEDURE,
/// §14.9.40.4 GR10/GR11). It is a RETURN-ADDRESS pair, not a physical block: execution begins at
/// <see cref="Start"/> and the compiler-inserted return mechanism fires when pc <see cref="End"/> falls off its
/// own end (control-flow deep-dive D5). Every consumer that must know whether the set is empty asks
/// <see cref="IsEmpty"/>; nothing decodes the pair arithmetically.
/// </summary>
/// <remarks>
/// <para>⛔ <b>EMPTINESS CANNOT BE DERIVED FROM THE PAIR, AND THAT IS THE WHOLE REASON THIS TYPE EXISTS</b>
/// (kb/Work PB440). A section with ZERO paragraphs is legal COBOL (§14.4.2 — "a section header followed by zero,
/// one, or more successive paragraphs"), and it resolves to <c>(s, s−1)</c>. An INVERTED THRU range —
/// procedure-name-2 physically PRECEDING procedure-name-1, reached by a GO TO, which §14.9.28.4 GR6 explicitly
/// permits ("There is no necessary relationship between procedure-name-1 and procedure-name-2") and NIST NC102A
/// PFM-TEST-F1-10 exercises — also resolves to <c>End &lt; Start</c>, and when the two procedures are ADJACENT it
/// resolves to the SAME pair <c>(s, s−1)</c>. One must execute nothing; the other must execute from <c>s</c>
/// until control reaches <c>s−1</c>. So <c>End &lt; Start</c> is ambiguous, every hand-written
/// <c>start &gt; end</c> / <c>Start &lt;= End</c> test was answering the wrong question, and the bit is CARRIED.</para>
/// <para>The canonical empty range is still <c>(s, s−1)</c> rather than a sentinel, because that pair is exactly
/// what makes GR4's THRU composition come out right: an empty procedure-name-1's <see cref="Start"/> is the pc at
/// which execution continues past it, and an empty procedure-name-2's <see cref="End"/> is the last pc before it,
/// so <c>PERFORM empty-sec THRU p</c> and <c>PERFORM p THRU empty-sec</c> compose with no special case
/// (see <see cref="Through"/>).</para>
/// </remarks>
public readonly record struct PcRange
{
    private PcRange(int start, int end, bool isEmpty)
    {
        Start = start;
        End = end;
        IsEmpty = isEmpty;
    }

    /// <summary>The pc at which execution of the set begins (§14.9.28.4 GR5 — "control is transferred to the
    /// first statement of the specified set of statements"). Meaningful even when <see cref="IsEmpty"/>: it is
    /// then the pc at which execution CONTINUES past the empty procedure, which is what makes composition work.</summary>
    public int Start { get; }

    /// <summary>The pc whose fall-through fires the compiler-inserted return mechanism (§14.9.28.4 GR5a/GR5b;
    /// §14.9.40.4 GR11 for a sort procedure). May be less than <see cref="Start"/> for a legal INVERTED range.</summary>
    public int End { get; }

    /// <summary>The specified set of statements is EMPTY — procedure-name-1 (or the whole THRU composition) names
    /// a section with zero paragraphs (§14.4.2). GR4 defines the set and an empty set is a set: the PERFORM's
    /// control phrase still executes in full (GR8/GR9/GR10/GR13), with a body that does nothing, and no transfer
    /// of control takes place (GR5). The dispatcher must never be asked to run such a range — its return test
    /// (<c>__atExit &amp;&amp; __pc == __exitPc + 1</c>) cannot fire on it, so it would run to the end of the pc
    /// space (kb/Work PB440).</summary>
    public bool IsEmpty { get; }

    /// <summary>A single paragraph's range (§8.4.2.2 — a paragraph-name resolves to one pc).</summary>
    public static PcRange At(int pc) => new(pc, pc, isEmpty: false);

    /// <summary>A non-empty range spanning <paramref name="start"/>..<paramref name="end"/> inclusive. The pair
    /// may be inverted (GR6): <paramref name="end"/> is the NAMED exit, not a numeric upper bound.</summary>
    public static PcRange Of(int start, int end) => new(start, end, isEmpty: false);

    /// <summary>The EMPTY range of a zero-paragraph section (§14.4.2) whose paragraphs would have begun at
    /// <paramref name="startPc"/>: execution continues at <paramref name="startPc"/>, the return mechanism sits
    /// after <c>startPc − 1</c>, and there is no first statement to transfer to.</summary>
    public static PcRange EmptyAt(int startPc) => new(startPc, startPc - 1, isEmpty: true);

    /// <summary>Compose <c>procedure-name-1 THRU procedure-name-2</c> (§14.9.28.4 GR4 — "all statements beginning
    /// with the first statement of procedure-name-1 and ending with the last statement of procedure-name-2";
    /// GR5b — the return mechanism is after the last statement of procedure-name-2). The composition is empty
    /// only when BOTH endpoints are empty procedures AND nothing lies between them, i.e. this range's entry pc is
    /// exactly one past the composed exit; any statement in between (or a non-empty endpoint) makes the set
    /// non-empty, including the inverted arrangements GR6 permits.</summary>
    public PcRange Through(PcRange last) =>
        new(Start, last.End, isEmpty: IsEmpty && last.IsEmpty && Start == last.End + 1);

    /// <summary>This range is exactly one paragraph — what ALTER's procedure-name-1 requires (a paragraph
    /// containing a single GO TO), so a section name (a multi-pc range) and an empty section are both excluded.</summary>
    public bool IsParagraph => !IsEmpty && Start == End;

    /// <summary>⛔ DOES A PARAGRAPH LIE LEXICALLY WITHIN THIS PROCEDURE? — the question a SYNTAX rule asks of a range
    /// ("shall not appear in an input or output procedure", §14.9.40.3 SR3), asked of the TEXT: every paragraph from
    /// the first named procedure to the last, in whichever physical order the two stand. Never <c>Start &lt;= pc &lt;=
    /// End</c> read as a numeric interval — for a legal INVERTED THRU range (§14.9.28.4 GR6, <see cref="End"/> physically
    /// before <see cref="Start"/>) that interval is the empty set, which is how a MERGE written inside an inverted
    /// SORT input procedure escaped its prohibition (kb/Work PB812). The EMPTY range has no paragraph.
    /// <para>This is the LEXICAL model, and it is the right one for a syntax rule. §14.9.40.4 GR10 defines the RANGE of
    /// a sort procedure by the statements EXECUTED as the result of a transfer of control (a PERFORM out of the procedure
    /// and back extends it beyond any span of the text), but GR10 attaches its own consequence to that dynamic range —
    /// the EC-SORT-MERGE-ACTIVE exception condition, raised at run time (kb/Work PB1036) — and a paragraph reached from
    /// both inside and outside the procedure is not "in" it for the syntax rules. No reachability walk replaces this
    /// method (kb/Work PB812's determination). A rule that IS about execution (§14.9.28.3 SR8's "under") builds its
    /// closure in <c>ProcedureReach.Closure</c>, seeded from <see cref="SpannedPcs"/>.</para></summary>
    public bool Spans(int pc) => !IsEmpty && pc >= SpanLow && pc <= SpanHigh;

    /// <summary>The paragraphs <see cref="Spans"/> answers membership of, lowest pc first — the seed set of the
    /// reachability model (<c>ProcedureReach.Closure</c>), so the lexical span is computed in ONE place.</summary>
    public IEnumerable<int> SpannedPcs()
    {
        if (IsEmpty) yield break;
        for (int pc = SpanLow; pc <= SpanHigh; pc++) yield return pc;
    }

    private int SpanLow => Math.Min(Start, End);
    private int SpanHigh => Math.Max(Start, End);

    public override string ToString() => IsEmpty ? $"[empty @{Start}]" : $"[{Start}..{End}]";
}
