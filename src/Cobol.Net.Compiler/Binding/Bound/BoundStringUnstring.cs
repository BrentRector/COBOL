// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The STRING/UNSTRING bound nodes (P7 Step 10d: the binder half moved to
// Binding/Procedure/Verbs/StringUnstringBinder.cs; these records STAY here — the source-generated
// visitor and StatementChildren key on this namespace).

// The entire STRING/UNSTRING surface — including NOT ON OVERFLOW and END-STRING/END-UNSTRING — is
// COBOL-85 (both verbs' phrases were complete by 1985); no edition gate applies. The post-85 deltas — class
// national operands (a national receiver stores through the same image writer as an alphanumeric one, kb/Work
// PB1179), the zero-length-item rules (UNSTRING GR2 terminates before any other rule, kb/Work PB1184) and
// dynamic-length SIZE (§14.9.43.4 GR1) — are bound here and emitted by StringEmitter; the EC-OVERFLOW-STRING /
// EC-OVERFLOW-UNSTRING names (2002+, GR8b / GR16b) are raised through EcEmitter.EmitOverflow; the ON/NOT ON
// OVERFLOW control flow itself is edition-invariant.

/// <summary>One STRING sending operand with its governing delimiter (ISO §14.9.43.2): the DELIMITED phrase
/// written after a run of senders governs every sender of that run, so the binder back-propagates it
/// (<c>StringUnstringBinder.BindString</c>); a run with no following phrase is DELIMITED BY SIZE (SR9).
/// <paramref name="BySize"/> and a non-null <paramref name="Delimiter"/> are mutually exclusive.</summary>
public sealed record BoundStringSending(BoundOperand Value, BoundOperand? Delimiter, bool BySize);

/// <summary><c>STRING sendings… INTO into [WITH POINTER ptr] [ON/NOT ON OVERFLOW …]</c> (ISO §14.9.43): each
/// sending transfers character by character at the pointer position into the receiver — whose untouched portions
/// are PRESERVED (GR7, never space-filled) — under the GR8 per-character range check.</summary>
public sealed record BoundStringStmt(
    IReadOnlyList<BoundStringSending> Sendings, Place Into, Place? Pointer,
    IReadOnlyList<BoundStatement>? OnOverflow, IReadOnlyList<BoundStatement>? NotOnOverflow) : BoundStatement
{
    /// <summary>The category of the statement's characters — National when identifier-3 is of class national,
    /// otherwise Alphanumeric. ISO §14.9.43.4 GR2: "When a figurative constant is specified as literal-1 or
    /// literal-2, it refers to an implicit one character data item whose usage shall be the same as the usage of
    /// identifier-3, either display or national", so this is the context §8.3.3.6.4 GR6 reads to choose the
    /// program collating sequence a HIGH-VALUE / LOW-VALUE is taken from (kb/Work PB1185).</summary>
    public required PicCategory CharacterCategory { get; init; }
}

/// <summary>One UNSTRING delimiter (ISO §14.9.48.2): its value (a literal, a figurative — a single character per
/// GR7 — or a field read at execution) and whether the ALL phrase collapses contiguous occurrences (GR7).</summary>
public sealed record BoundUnstringDelimiter(BoundOperand Value, bool All);

/// <summary>One UNSTRING receiving area with its optional DELIMITER IN / COUNT IN companions (§14.9.48.2), and
/// the STORES ISO §14.9.48.4 GR11 c)/d) make of it — each a <see cref="BoundMove"/> BOUND through
/// <c>MoveBinder.BindMoveOf</c> from the statement's conceptual item (<see cref="BoundUnstringStmt.Examined"/> /
/// <see cref="BoundUnstringStmt.Delimiting"/>), because both rules move "according to the rules for the MOVE
/// statement" (kb/Work PB979). They are child statements, so the generated <c>StatementChildren</c> walk reaches
/// them like any other move.
/// <para><paramref name="ZeroFill"/> is GR8's own store for a NUMERIC receiver — "When any examination encounters
/// two contiguous delimiters, the current receiving area shall be … zero-filled if it is described as numeric" —
/// which the MOVE rules alone would not give (a zero-length alphanumeric sender is SPACE by §14.9.25.4 GR1/GR2).
/// Null for every other receiver, where GR8's space fill IS the MOVE rules' answer.</para>
/// <para>The GR11 b) examination SIZE is not carried: it is the receiving area's size at the moment the
/// statement executes (an ANY LENGTH or reference-modified receiver has no static one), so the emitter asks
/// <c>ReceivingStore.ExaminationSize</c> of <paramref name="Target"/> at the point of use.</para></summary>
public sealed record BoundUnstringReceiver(
    Place Target, Place? DelimiterIn, Place? CountIn, BoundMove Store, BoundMove? DelimiterStore, BoundMove? ZeroFill);

/// <summary><c>UNSTRING source [DELIMITED BY …] INTO receivers… [WITH POINTER ptr] [TALLYING IN tly]
/// [ON/NOT ON OVERFLOW …]</c> (ISO §14.9.48).</summary>
public sealed record BoundUnstringStmt(
    // ⛔ DA4: an OPERAND, not a Place. §14.9.48.2 writes the sender as identifier-1 and §8.4.3.1.2 Format 1 makes a
    // function-identifier an identifier, so `UNSTRING FUNCTION UPPER-CASE(S) …` is conforming source. A function
    // result has no Place, and the emitter never needed one: it already renders the sender through
    // OperandText.AsString (THE one string-context renderer), it was merely wrapping the Place to get there.
    BoundOperand Source, IReadOnlyList<BoundUnstringDelimiter> Delimiters, IReadOnlyList<BoundUnstringReceiver> Receivers,
    Place? Pointer, Place? Tallying,
    IReadOnlyList<BoundStatement>? OnOverflow, IReadOnlyList<BoundStatement>? NotOnOverflow) : BoundStatement
{
    /// <summary>The category of the statement's characters — National when identifier-1 is a national data item,
    /// otherwise Alphanumeric. ONE fact, two rules read it: GR7 ("When a figurative constant is used as the
    /// delimiter, it stands for a single-character national literal if identifier-1 is a national data item;
    /// otherwise, it stands for a single-character alphanumeric literal" — the context §8.3.3.6.4 GR6 reads to
    /// choose the program collating sequence of a HIGH-VALUE / LOW-VALUE delimiter, kb/Work PB1185) and GR11 c)/d)
    /// (the category of <see cref="Examined"/> and <see cref="Delimiting"/>, minted from it).</summary>
    public required PicCategory CharacterCategory { get; init; }

    /// <summary>GR11 c)'s conceptual item — "the characters examined, excluding any delimiting characters",
    /// "treated as an elementary national data item if identifier-1 is of category national, and otherwise as an
    /// elementary alphanumeric data item" (<c>SendingValueTemp.ConceptualCharacterItem</c>). The emitter writes each
    /// examination into it; every receiver's <see cref="BoundUnstringReceiver.Store"/> moves FROM it.</summary>
    public required Place Examined { get; init; }

    /// <summary>GR11 d)'s conceptual item — the delimiting characters, same category rule — present exactly when
    /// some receiver carries a DELIMITER IN phrase.</summary>
    public Place? Delimiting { get; init; }
}
