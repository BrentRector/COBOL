// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE SEQUENCE AXIS OF A PLACEMENT RULE — where a statement stands among the statements written beside it
/// (kb/Work PB397). <see cref="EnclosingContext"/> answers "what is this statement INSIDE?" (source element,
/// PERFORM, WHEN phrase, declarative); it cannot answer the question a second family of rules asks: which
/// SEQUENCE of statements is this one in, and where? ISO states three rules over that axis — §14.9.14.3 SR1
/// (a bare EXIT is a sentence by itself, the only sentence in its paragraph), §14.9.17.3 SR2 (a Format 1 GO TO is
/// the last statement of its consecutive sequence of imperative statements) and §14.9.42.3 SR1 (STOP is the last
/// statement of its block) — and each was accepted silently, because the binder had no place to ask.
///
/// <para><b>Why a view over the parse tree and not binder state.</b> The grammar makes the sequence a rule of its
/// own: every <c>statement</c> is written as an element of exactly one <c>sentence</c> (<c>statement+ DOT</c>) or
/// exactly one <c>statementBlock</c> (<c>statement+</c>, the imperative-statement of a phrase of a conditional or
/// delimited-scope statement), and every <c>sentence</c> is an element of exactly one paragraph, section header
/// run or procedure-division header run. So the position is a pure function of the statement's own ancestors:
/// it needs no cursor, cannot go stale, and is the same for a statement bound twice. The cursor-based
/// <see cref="BinderContext.Enclosing"/> remains the answer for rules about CONTAINMENT.</para>
///
/// <para><b>The two sequence kinds are different on purpose.</b> A <c>sentence</c>'s statements end at its period;
/// a <c>statementBlock</c>'s end at the phrase's end (ELSE, END-IF, WHEN, ...). §14.9.17.3 SR2 and §14.9.42.3 SR1
/// are stated over EITHER, so <see cref="IsLast"/> is asked of both; §14.9.14.3 SR1 is stated over the SENTENCE
/// only, so <see cref="IsSentenceByItself"/> is false for an EXIT inside a phrase — the sentence it is in is the
/// enclosing statement's.</para>
/// </summary>
internal readonly struct StatementPosition
{
    private readonly Core.StatementContext[] _siblings;

    private StatementPosition(ParserRuleContext sequence, Core.StatementContext[] siblings, int index)
    {
        Sequence = sequence;
        _siblings = siblings;
        Index = index;
    }

    /// <summary>The <c>sentence</c> or <c>statementBlock</c> holding the statement.</summary>
    public ParserRuleContext Sequence { get; }

    /// <summary>The statement's zero-based index in its sequence.</summary>
    public int Index { get; }

    /// <summary>How many statements the sequence has.</summary>
    public int Count => _siblings.Length;

    /// <summary>Is the statement the last of its sequence — the predicate §14.9.17.3 SR2 and §14.9.42.3 SR1
    /// state as "the last statement in that sequence" / "the last statement in any discreet block"?</summary>
    public bool IsLast => Index == _siblings.Length - 1;

    /// <summary>The statement written immediately after this one in the sequence. Only valid when
    /// <see cref="IsLast"/> is false.</summary>
    public Core.StatementContext Following => _siblings[Index + 1];

    /// <summary>The <c>sentence</c> the statement is a direct element of, or null when it is an element of a
    /// phrase's <c>statementBlock</c> (it is then nested in another statement, never a sentence of its own).</summary>
    public Core.SentenceContext? Sentence => Sequence as Core.SentenceContext;

    /// <summary>Is the statement the whole of a sentence — "in a sentence by itself" (§14.9.14.3 SR1)?</summary>
    public bool IsSentenceByItself => Sentence is not null && _siblings.Length == 1;

    /// <summary>How many sentences the statement's sentence shares its paragraph with, itself included — the
    /// paragraph being §14.4.3's: a paragraph-name and its sentences, or, with the name omitted, the sentences
    /// after the procedure division header or a section header (so a section without paragraphs is ONE such
    /// paragraph, the second alternative of §14.9.14.3 SR1). Zero when the statement is not a sentence's
    /// element.</summary>
    public int SentencesInParagraph =>
        Sentence?.Parent is ParserRuleContext paragraph ? paragraph.GetRuleContexts<Core.SentenceContext>().Length : 0;

    /// <summary>The position of <paramref name="s"/>. TOTAL by the grammar: a <c>statement</c> is an element of a
    /// <c>sentence</c> or of a <c>statementBlock</c> and of nothing else, so the throw is a drift detector — a
    /// third container added to the grammar must be given a meaning here, never read as "no sequence".</summary>
    public static StatementPosition Of(Core.StatementContext s) => s.Parent switch
    {
        Core.SentenceContext sentence => At(sentence, sentence.statement(), s),
        Core.StatementBlockContext block => At(block, block.statement(), s),
        var other => throw new InvalidOperationException(
            $"A statement is an element of a sentence or a statement block; this one's parent is {other?.GetType().Name ?? "null"}."),
    };

    private static StatementPosition At(ParserRuleContext sequence, Core.StatementContext[] siblings, Core.StatementContext s) =>
        new(sequence, siblings, Array.IndexOf(siblings, s));
}

/// <summary>
/// ⛔ THE SEQUENCE-PLACEMENT ASKERS — one ROW per rule stated over a statement sequence, one check
/// (<see cref="RefusedOutOfSequence"/>) asked of every statement by the statement funnel (kb/Work PB397). A verb
/// binder never asks it: the funnel is the one place every statement passes through, so a rule added as a row
/// cannot be missed by a verb that forgets to ask, which is how three rules came to be enforced nowhere.
///
/// <para>The next rule of this shape — §14.9.19's required consequent, an EXIT PROGRAM or GOBACK sequence rule an
/// older edition states — is a row: a statement-shape probe, a descriptor, and a predicate over
/// <see cref="StatementPosition"/>.</para>
/// </summary>
internal static partial class PlacementRules
{
    /// <summary>One placement rule over the statement sequence: the descriptor it reports, the rule as the
    /// diagnostic quotes it, and the predicate — null when the position is legal, else why it is not.</summary>
    private sealed record SequenceRule(
        DiagnosticDescriptor Descriptor, string Cite, string Shall, Func<StatementPosition, string?> Violation);

    /// <summary>§14.9.14.3 SR1 — Format 1 only: a sentence by itself AND the only sentence of its paragraph.</summary>
    private static readonly SequenceRule ExitAlone = new(
        DiagnosticCatalog.ExitNotAlone, "ISO §14.9.14.3 SR1",
        "The EXIT statement shall appear in a sentence by itself that shall be the only sentence in the paragraph "
        + "or in a section without paragraphs",
        p => p.Sentence is null
                ? "is written inside another statement, not as a sentence of its own"
            : !p.IsSentenceByItself
                ? $"shares its sentence with {p.Count - 1} other statement{(p.Count == 2 ? "" : "s")}"
            : p.SentencesInParagraph != 1
                ? $"is one of {p.SentencesInParagraph} sentences in its paragraph"
            : null);

    /// <summary>§14.9.17.3 SR2 — a Format 1 GO TO ends its consecutive sequence of imperative statements.</summary>
    private static readonly SequenceRule GoToLast = new(
        DiagnosticCatalog.GoToNotLast, "ISO §14.9.17.3 SR2",
        "A Format 1 GO TO that appears in a consecutive sequence of imperative statements shall be the last "
        + "statement in that sequence",
        FollowedBy);

    /// <summary>§14.9.42.3 SR1 — STOP ends its block of code (read as the same sequence; D-SEQ).</summary>
    private static readonly SequenceRule StopLast = new(
        DiagnosticCatalog.StopNotLast, "ISO §14.9.42.3 SR1",
        "The STOP statement shall be specified only as the last statement in any discreet block of code",
        FollowedBy);

    private static string? FollowedBy(StatementPosition p) =>
        p.IsLast ? null : $"is followed by {p.Following.Start.Text.ToUpperInvariant()} in the same sequence, which can never execute";

    /// <summary>The sequence rule that governs <paramref name="s"/>, or null for the statements that carry none
    /// (almost all of them — the probe is one type test on the statement's only child, so the funnel pays nothing
    /// for the rest). The shape tests are the printed formats the rules name: a bare EXIT is Format 1 alone
    /// (§14.9.14.2), a GO TO is Format 1 unless it is the DEPENDING Format 2 — the 1985 target-less GO TO is
    /// that edition's Format 1 (its procedure-name is optional there, and ALTER supplies it), so the rule binds
    /// it too — and a STOP is the STOP RUN statement, the only STOP §14.9.42 has. The X3.23-1985 STOP literal
    /// (deleted 2002) is not governed: it communicates to the operator and CONTINUES with the next statement
    /// (<see cref="Bound.BoundStopLiteral"/>), so a statement after it does execute and nothing makes it last.</summary>
    private static SequenceRule? SequenceRuleOf(Core.StatementContext s) => s.GetChild(0) switch
    {
        Core.ExitStatementContext { ChildCount: 1 } => ExitAlone,
        Core.GoToStatementContext g when GoToFormats.Of(g) != GoToFormat.Depending => GoToLast,
        Core.StopStatementContext stop when stop.RUN() is not null => StopLast,
        _ => null,
    };

    /// <summary>Is <paramref name="s"/> written where the sequence rule for its verb forbids (ISO §14.9.14.3 SR1,
    /// §14.9.17.3 SR2, §14.9.42.3 SR1)? TRUE when REFUSED — the diagnostic has been reported at the statement —
    /// so the funnel reads <c>if (PlacementRules.RefusedOutOfSequence(ctx, s)) core = BoundRejected.Reported(…);</c>.
    /// Asked of the PARSE TREE, so the verdict is the same wherever and however often the statement binds.</summary>
    public static bool RefusedOutOfSequence(BinderContext ctx, Core.StatementContext s)
    {
        if (SequenceRuleOf(s) is not { } rule || rule.Violation(StatementPosition.Of(s)) is not { } why) return false;
        ctx.Edition.Error(rule.Descriptor, $"{rule.Shall} ({rule.Cite}); this one {why}");
        return true;
    }
}
