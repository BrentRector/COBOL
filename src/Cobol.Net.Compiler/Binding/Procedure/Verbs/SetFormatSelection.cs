// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>What ONE operand of a SET statement IS, as the seventeen general formats of ISO §14.9.39.2
/// distinguish operands. One member per brace the printed formats write, so a format's admissibility is a SET of
/// these and never a scalar "the category of operand zero" (kb/Work PB449).
/// <para>The members are properties of the ITEM (§8.5.2.1 Table 2's class and category, §13.18.38's index-name,
/// §13.18.38.3 SR30's register), so the same classification answers for a RECEIVING operand — which is what
/// selects the format — and for a SENDING one, which §14.9.39.2 constrains in each format's own sending brace
/// (<see cref="SetFormatSelection.SelectForTo"/>).</para></summary>
internal enum SetOperandKind
{
    /// <summary>The reference identifies nothing resolvable here. NOT a category verdict: the committing
    /// resolution that follows reports it (the R30 probe contract), so selection ignores these operands rather
    /// than letting an undefined name pick — or veto — a format.</summary>
    Unclassified,
    /// <summary>An INDEXED BY index-name (§13.18.38) — index-name-1 (Format 1) / index-name-3 (Format 2).</summary>
    IndexName,
    /// <summary>A USAGE INDEX data item — class index, §8.5.2.1 Table 2; identifier-1's first alternative.</summary>
    IndexDataItem,
    /// <summary>An integer data item (§14.9.39.3 SR1's second alternative, <see cref="PicInfo.IsIntegerDescription"/>).</summary>
    IntegerItem,
    /// <summary>A resolvable data item that is none of the above — admitted by Format 1's <c>identifier-1</c>
    /// BRACE and refused by its SR1 CATEGORY rule, which is a screen of its own (kb/Work PB212), not a format
    /// selection question. Selection therefore treats it as a Format-1 receiver.</summary>
    OtherDataItem,
    /// <summary>An OCCURS DYNAMIC CAPACITY register — data-name-2, §14.9.39.3 SR29 (Format 14).</summary>
    CapacityRegister,
    /// <summary>A dynamic-length elementary item — data-name-3, §14.9.39.3 SR33 (Format 16).</summary>
    DynamicLength,
    /// <summary>Category data-pointer — identifier-5 (Format 7, SR17) / identifier-9 (Format 10, SR23).</summary>
    DataPointer,
    /// <summary>Category program-pointer — identifier-7 (Format 9, SR21).</summary>
    ProgramPointer,
    /// <summary>Category function-pointer — identifier-12 (Format 8, SR20).</summary>
    FunctionPointer,
    /// <summary>Class object — identifier-3 (Format 5, SR8).</summary>
    ObjectReference,
    /// <summary>A message-tag data item — data-name-4 / data-name-5 (Format 17, §14.9.39.3 SR35 "Data-name-4
    /// and data-name-5 shall be message-tag data items"). §13.18.60.4 GR9 makes the class and category of such
    /// an item message-tag, which no other printed receiving brace names — so it selects Format 17 and nothing
    /// else, and Format 17 is a DECLINED facility that is refused by name (kb/Work PB453).</summary>
    MessageTag,
}

/// <summary>The ISO §14.9.39.2 general format a SET statement selects. Only the formats whose GRAMMAR shape is
/// shared — and so must be told apart semantically — appear here; the formats a reserved word already
/// discriminates (3 switch, 4 condition, 6 attribute, 11/12 locale, 13 last-exception, 15 content) are chosen in
/// <c>SetBinder.BindSet</c> by their own alternative.
/// <para>⛔ FORMAT 17 IS ONE OF THEM, AND THIS COMMENT USED TO SAY IT "CANNOT BE REACHED" (kb/Work PB453).
/// The MESSAGE-TAG entry is refused by name at the data description entry (COBOLNET1943) — but a refused entry
/// still produces a symbol, the statement still binds, and <c>SET MT-A TO MT-B</c> / <c>SET MT-A TO NULL</c>
/// therefore DID reach this selection and fell through to §14.9.39.3 SR8's object-reference screen and
/// §8.8.1.1's arithmetic screen. The user was told their message-tag item "shall be a USAGE OBJECT REFERENCE
/// data item" — a rule about a format their program is not, naming a fix that would not be one. The format is
/// a row in the table now, and it names the declined FACILITY.</para></summary>
internal enum SetFormat
{
    /// <summary>Format 1 — index-assignment.</summary>
    F1,
    /// <summary>Format 2 — index-arithmetic.</summary>
    F2,
    /// <summary>Format 5 — object-reference-assignment.</summary>
    F5,
    /// <summary>Format 7 — data-pointer-assignment.</summary>
    F7,
    /// <summary>Format 8 — function-pointer-assignment.</summary>
    F8,
    /// <summary>Format 9 — program-pointer-assignment.</summary>
    F9,
    /// <summary>Format 10 — data-pointer-arithmetic.</summary>
    F10,
    /// <summary>Format 14 — dynamic-capacity-table.</summary>
    F14,
    /// <summary>Format 16 — dynamic-length-elementary-data-item.</summary>
    F16,
    /// <summary>Format 17 — message-tag. The DATA half of the Annex A.3 item-4 asynchronous messaging facility,
    /// which this implementation declines (docs/CONFORMANCE.md §4 item 1); selected so §4.2.6's mandatory
    /// warning mechanism names the facility, exactly as COBOLNET1578 already does for its SEND/RECEIVE half.</summary>
    F17,
}

/// <summary>Which written shape the receiving operands stand in — the two grammar rules that carry more than one
/// general format (<c>setToValueStatement</c> / <c>setObjectReferenceStatement</c> and
/// <c>setIndexStatement</c>).</summary>
[Flags]
internal enum SetDirections
{
    /// <summary><c>SET … TO …</c>.</summary>
    To = 1,
    /// <summary><c>SET … {UP|DOWN} BY …</c>.</summary>
    UpDown = 2,
    /// <summary>Both (Format 14 is written all three ways).</summary>
    Any = To | UpDown,
}

/// <summary>⛔ THE ONE PLACE A SET STATEMENT'S GENERAL FORMAT IS CHOSEN (ISO §14.9.39.2; kb/Work PB449 + PB456).
/// <para>Several of §14.9.39.2's seventeen printed formats share one token shape, so the format is a SEMANTIC
/// question — and §14.9.39.2 asks it of the whole receiving brace, every one of which is written
/// <c>{ … } …</c> (one or more operands). Before this, each candidate format peeked at <c>receivers[0]</c> — and
/// at the SENDER — on its own, so the SAME two operands gave a correct SR23 diagnostic in one order and a
/// run-time crash in the other, and a statement no printed format admits fell through to Format 1/2 arithmetic
/// (kb/Work PB449 §1). A scalar "the category of operand zero" was standing in for what the standard makes a
/// SET-valued question (feedback_model_the_rule_shape_not_one_case).</para>
/// <para><b>The shape.</b> Classify every receiving operand ONCE (<see cref="KindOf"/>), then pick the single
/// row of <see cref="Formats"/> whose printed receiving brace admits ALL of them. When no row does, pick the
/// NEAREST row — the first, in specificity order, that admits ANY of them — and the selected format's OWN
/// syntax rule then refuses the operands it does not admit, BY NAME and in the format's own words. That is what
/// makes the two orders agree: <c>SET P1 WS-N UP BY 4</c> and <c>SET WS-N P1 UP BY 4</c> both select Format 10
/// and both draw SR23 against WS-N. When nothing matches at all there IS no format, and
/// <see cref="ReportNoFormat"/> says so instead of executing the statement as Format 1/2.</para>
/// <para><b>The sender does not select — except against Format 1's catch-all brace.</b> §14.9.39.2 writes the
/// format's identity into its receiving brace; the sending brace is then constrained by that format's own syntax
/// rules (SR9 for Format 5, SR17/SR20/SR21 for the carriers). Routing on the sender is what let <c>SET U TO 5</c>
/// — an object-reference receiver with a literal sender — escape every operand rule and abort at run time
/// (kb/Work PB456): the re-route's precondition was "the sender is exactly one bare data reference", so it
/// declined, and nothing stood behind it. The ONE case where the sender still decides is the one where the
/// receiving brace says nothing: Format 1's <c>identifier-1</c> admits any identifier, so a receiving list can
/// select Format 1 while the SENDER is of a category Format 1's own sending brace cannot hold —
/// <c>arithmetic-expression-1</c> takes numeric operands only (§8.8.1.1) and SR2 makes <c>identifier-2</c> class
/// index. A data item of class object, or of category data-pointer / program-pointer / function-pointer, is
/// admitted as a sending operand by exactly ONE printed format each (Format 5, 7, 9, 8), so that is the format
/// the statement is, and its own receiving rule refuses the receiver BY NAME. Before this,
/// <c>SET N4 TO U</c> reached the ARITHMETIC screen and was reported as "'U' is not a numeric operand"
/// (COBOLNET0844) — true of §8.8.1.1 and silent about §14.9.39.3 SR8, which is the rule the program broke.
/// <see cref="SelectForTo"/> is that arm; <see cref="Select"/> stays the receiving-list-only answer.</para></summary>
internal sealed class SetFormatSelection(BinderContext ctx, StatementBinder host)
{
    /// <summary>One table row: the receiving brace as printed, plus the syntax rule that governs the operands it
    /// admits. <see cref="Mask"/> is <see cref="Admits"/> as a bit per
    /// <see cref="SetOperandKind"/>, computed once at class initialization: selection runs at every SET
    /// statement in the program, and the set algebra it needs ("are all present kinds admitted", "is any") is a
    /// single machine instruction over a mask where it would otherwise allocate a <c>HashSet</c> per statement.
    /// <see cref="Admits"/> stays the readable form the table is written in and the drift test reads.
    /// <para><see cref="SendsOnly"/> is the other half of the printed format: the operand kinds this format's
    /// SENDING brace admits and Format 1's does NOT (§8.8.1.1 numeric operands and SR2's class index). A kind
    /// named here is named by exactly one row, which is what makes a sender able to identify the format when the
    /// receiving list landed on Format 1's catch-all brace — <see cref="SelectForTo"/>.</para></summary>
    private readonly record struct Row(
        SetFormat Format, SetDirections Dir, SetOperandKind[] Admits, SetOperandKind[] SendsOnly,
        string Rule, string Brace)
    {
        public uint Mask { get; } = Bits(Admits);

        public uint SendsMask { get; } = Bits(SendsOnly);
    }

    /// <summary>The kinds as a bit set — one bit per <see cref="SetOperandKind"/> member.</summary>
    private static uint Bits(IEnumerable<SetOperandKind> kinds)
    {
        uint m = 0;
        foreach (var k in kinds) m |= 1u << (int)k;
        return m;
    }

    /// <summary>⛔ ORDERED BY SPECIFICITY — the register/carrier formats first, Format 1's catch-all
    /// <c>identifier-1</c> brace LAST — because the nearest-row fallback reads it in order. A new amount- or
    /// carrier-taking format is a row here and nothing else; <c>SetFormatSelectionDriftTests</c> pins that
    /// within one direction no kind is admitted by two rows, so the table can never become ambiguous.</summary>
    private static readonly Row[] Formats =
    [
        new(SetFormat.F14, SetDirections.Any,    [SetOperandKind.CapacityRegister], [], "ISO §14.9.39.3 SR29",
            "Format 14's receiving operand is data-name-2, a register defined in the CAPACITY phrase of a dynamic-capacity-table OCCURS clause"),
        new(SetFormat.F16, SetDirections.To,     [SetOperandKind.DynamicLength],    [], "ISO §14.9.39.3 SR33",
            "Format 16's receiving operand is data-name-3, a dynamic-length elementary data item"),
        new(SetFormat.F7,  SetDirections.To,     [SetOperandKind.DataPointer],
            [SetOperandKind.DataPointer],                                               "ISO §14.9.39.3 SR17",
            "Format 7's receiving operand is identifier-5, of category data-pointer"),
        new(SetFormat.F8,  SetDirections.To,     [SetOperandKind.FunctionPointer],
            [SetOperandKind.FunctionPointer],                                           "ISO §14.9.39.3 SR20",
            "Format 8's receiving operand is identifier-12, of category function-pointer"),
        new(SetFormat.F9,  SetDirections.To,     [SetOperandKind.ProgramPointer],
            [SetOperandKind.ProgramPointer],                                            "ISO §14.9.39.3 SR21",
            "Format 9's receiving operand is identifier-7, of category program-pointer"),
        new(SetFormat.F5,  SetDirections.To,     [SetOperandKind.ObjectReference],
            [SetOperandKind.ObjectReference],                                           "ISO §14.9.39.3 SR8",
            "Format 5's receiving operand is identifier-3, an item of class object permitted as a receiving item"),
        // Format 17 (message-tag) — a DECLINED facility with a row, not a hole. §14.9.39.3 SR35 makes BOTH its
        // operands message-tag data items, so the kind names this row from either side and `SET N TO MT` is
        // Format 17 just as `SET MT TO N` is; SetBinder answers it with the §4.2.6 facility refusal.
        new(SetFormat.F17, SetDirections.To,     [SetOperandKind.MessageTag],
            [SetOperandKind.MessageTag],                                                "ISO §14.9.39.3 SR35",
            "Format 17's receiving operand is data-name-4, a message-tag data item"),
        new(SetFormat.F10, SetDirections.UpDown, [SetOperandKind.DataPointer],      [], "ISO §14.9.39.3 SR23",
            "Format 10's receiving operand is identifier-9, of category data-pointer"),
        new(SetFormat.F2,  SetDirections.UpDown, [SetOperandKind.IndexName],        [], "ISO §14.9.39.2 Format 2 / §14.9.39.4 GR4",
            "Format 2's receiving operand is index-name-3, an index-name of an INDEXED BY phrase"),
        // ⛔ LAST, AND DELIBERATELY WIDE. Format 1's brace is `{ index-name-1 | identifier-1 } …`: the BRACE
        // admits any identifier and SR1 ("a data item of class index or an integer data item") is a CATEGORY
        // screen over it, not a selection question. That screen is its own open item (kb/Work PB212); putting it
        // here would make a missing screen look like a missing FORMAT and would change the diagnostic every
        // non-integer Format-1 receiver draws.
        new(SetFormat.F1,  SetDirections.To,
            [SetOperandKind.IndexName, SetOperandKind.IndexDataItem, SetOperandKind.IntegerItem, SetOperandKind.OtherDataItem],
            [],
            "ISO §14.9.39.3 SR1",
            "Format 1's receiving operand is index-name-1 or identifier-1, a data item of class index or an integer data item"),
    ];

    /// <summary>The rows, for the drift test that pins the table unambiguous.</summary>
    internal static IEnumerable<(SetFormat Format, SetDirections Dir, SetOperandKind[] Admits,
                                 SetOperandKind[] SendsOnly)> Rows =>
        Formats.Select(f => (f.Format, f.Dir, f.Admits, f.SendsOnly));

    /// <summary>What ONE receiving operand is (§14.9.39.2's receiving braces, in the order that makes each
    /// answer decidable): the CAPACITY register first — it is implicitly defined at the OCCURS entry and is not
    /// a storage name (§13.18.38 GR15) — then an index-name, which is not a data item at all (§13.18.38.3 SR7),
    /// then the item's own description.
    /// <para>⛔ PURE. The probe is the R30 non-diagnosing form: a name that identifies nothing answers
    /// <see cref="SetOperandKind.Unclassified"/> and the committing resolution the selected format performs
    /// reports it, so a typo is never re-reported here and never steers the format.</para></summary>
    public SetOperandKind KindOf(Core.DataReferenceContext dref)
    {
        // ⛔ A PREDEFINED OBJECT REFERENCE IS CLASSIFIED BEFORE THE GENERAL LOOKUP. §8.4.3.6.3 SR2 describes
        // EXCEPTION-OBJECT as "class object and category object reference", and NO data description entry
        // declares it — so the general resolution below answers "'EXCEPTION-OBJECT' is not defined"
        // (COBOLNET1639), which is false about a name the standard itself declares, and the statement then
        // binds as Format 1. Classified here, the receiving list selects Format 5 and the rule the program
        // actually broke is the one it draws: §8.4.3.6.3 SR1, "EXCEPTION-OBJECT shall not be specified as a
        // receiving operand" (COBOLNET2196, reported by OoBindSetObjectRef — the SAME code the general receiving
        // chokepoint draws, because it is the same rule; kb/Work PB922).
        // ⚠ Its three siblings cannot reach this classifier: NULL, SELF and SUPER are grammar TOKENS
        // (`objectReference`), not cobolWords, so they cannot head a `dataReference` in a receiving position at
        // all — `SET SELF TO G` is a parse error, and §8.4.3.7.3 SR1 / §8.4.3.8.3 SR2 are enforced by the
        // grammar rather than here. EXCEPTION-OBJECT is the one spelled as an ordinary word.
        if (ctx.Refs.IsExceptionObjectRegister(dref)) return SetOperandKind.ObjectReference;          // §8.4.3.6.3 SR2
        if (ctx.Refs.CapacityRegisterFor(dref) is not null) return SetOperandKind.CapacityRegister;   // SR29
        if (host.Expr.IndexFieldOf(dref) is not null) return SetOperandKind.IndexName;                // §13.18.38.3 SR7
        if (ctx.Refs.Probe(dref) is not { } sniff) return SetOperandKind.Unclassified;
        return KindOfItem(sniff.Item, sniff.OperandCategory);
    }

    /// <summary>What a RESOLVED data item is, as §14.9.39.2's braces distinguish operands — the half of
    /// <see cref="KindOf"/> that needs no reference, so a sender that IS an item without being written as a data
    /// reference (the temporary a function-identifier or an inline invocation returns — §8.4.3.2.1, §8.4.3.4.4 GR1) is
    /// classified by the SAME table as one that is (kb/Work PB1929).</summary>
    private static SetOperandKind KindOfItem(DataItem item, PicCategory? operandCategory)
    {
        if (item.IsDynamicLength) return SetOperandKind.DynamicLength;                                // SR33
        if (item.Pic is { Usage: Usage.Index }) return SetOperandKind.IndexDataItem;                  // §8.5.2.1 Table 2
        // ⛔ ASKED THROUGH THE USAGE, NOT THE CATEGORY (kb/Work PB453). MESSAGE-TAG is a DECLINED usage, so its
        // entry carries a recovery PicInfo whose CATEGORY is a placeholder — reading `OperandCategory` here
        // answers "alphanumeric" and the operand falls through to Format 1 / Format 5 and draws a rule that is
        // false about the program. ItemCategory.IsMessageTag reads the written clause AND the resolved usage,
        // which is the pair §13.18.60.3 SR14's own screen reads.
        if (ItemCategory.IsMessageTag(item)) return SetOperandKind.MessageTag;                        // SR35
        return operandCategory switch
        {
            PicCategory.Pointer => SetOperandKind.DataPointer,                 // SR17 / SR23
            PicCategory.ProgramPointer => SetOperandKind.ProgramPointer,       // SR21
            PicCategory.FunctionPointer => SetOperandKind.FunctionPointer,     // SR20
            PicCategory.ObjectReference => SetOperandKind.ObjectReference,     // SR8
            _ => item.Pic is { IsIntegerDescription: true }
                ? SetOperandKind.IntegerItem : SetOperandKind.OtherDataItem,  // SR1
        };
    }

    /// <summary>What a SET SENDER is, as the sending braces distinguish operands (kb/Work PB1399 + PB1929). A data
    /// reference is classified like a receiver; a function-identifier or inline invocation by the data item it
    /// references — its returned temporary when it has one (a user function's RETURNING item, an invocation's
    /// result), and class INDEX for an index intrinsic (§15.2 item 6), which has no item but is of class index just
    /// as an index data item is. Anything else — a numeric or character intrinsic's computed value, a literal, an
    /// expression — is <see cref="SetOperandKind.Unclassified"/>: it names no carrier format's sending brace, and
    /// the format that takes it refuses it by its own rule.</summary>
    public SetOperandKind KindOfSender(SetSender sender)
    {
        if (sender.Ref is { } dref) return KindOf(dref);
        if (sender.TemporaryItem is { } temp)
            return KindOfItem(temp.Item, Table16Operand.Of(temp).Category);
        return sender.IdentifierClass is CobolClass.Index ? SetOperandKind.IndexDataItem : SetOperandKind.Unclassified;
    }

    /// <summary>Classify every receiving operand of one SET statement, in source order.</summary>
    public SetOperandKind[] KindsOf(IReadOnlyList<Core.DataReferenceContext> receivers)
    {
        var kinds = new SetOperandKind[receivers.Count];
        for (int i = 0; i < receivers.Count; i++) kinds[i] = KindOf(receivers[i]);
        return kinds;
    }

    /// <summary>THE format selection (ISO §14.9.39.2). <paramref name="exact"/> is true when the chosen format's
    /// receiving brace admits EVERY classified operand; false when it is the nearest row and the format's own
    /// syntax rule is about to refuse the rest. Null when no printed format admits ANY of the operands — the
    /// residual arm, reported by <see cref="ReportNoFormat"/>.
    /// <para>Operands classified <see cref="SetOperandKind.Unclassified"/> are ignored: they carry no category
    /// evidence. When EVERY operand is unclassified the direction's own catch-all is chosen (Format 1 for TO;
    /// for UP/DOWN there is none but Format 2), so the statement still reaches a binder that reports the
    /// unresolved name.</para></summary>
    public static SetFormat? Select(IReadOnlyList<SetOperandKind> kinds, SetDirections dir, out bool exact)
    {
        exact = true;
        uint present = 0;
        foreach (var k in kinds)
            if (k != SetOperandKind.Unclassified) present |= 1u << (int)k;
        if (present == 0) return dir == SetDirections.UpDown ? SetFormat.F2 : SetFormat.F1;
        foreach (var row in Formats)                                         // every present kind admitted
            if ((row.Dir & dir) != 0 && (present & ~row.Mask) == 0) return row.Format;
        exact = false;
        foreach (var row in Formats)                                         // the NEAREST row — any admitted
            if ((row.Dir & dir) != 0 && (present & row.Mask) != 0) return row.Format;
        return null;
    }

    /// <summary>THE format selection for the <c>SET … TO …</c> shape, where §14.9.39.2 also prints a SENDING
    /// brace per format. <see cref="Select"/>'s answer stands, with ONE exception: when the receiving list
    /// selected Format 1 — whose <c>identifier-1</c> brace admits any identifier — and the sender is of a
    /// category Format 1's own sending brace cannot hold, the statement is the format whose sending brace DOES
    /// name that category, and that format's receiving rule refuses the receiver by name.
    /// <para>Format 1's sending brace is <c>{ arithmetic-expression-1 | index-name-2 | identifier-2 }</c>:
    /// §8.8.1.1 admits only numeric operands in an arithmetic expression and §14.9.39.3 SR2 makes identifier-2
    /// "a data item of class index", so an item of class object or of category data-/program-/function-pointer
    /// is admissible in NO Format-1 sending position — while each is named by exactly one other format's
    /// sending brace (<see cref="Row.SendsOnly"/>). Without this the sender reached the arithmetic screen and
    /// the user was told "'U' is not a numeric operand" (§8.8.1.1) about a statement whose broken rule is
    /// §14.9.39.3 SR8 (kb/Work PB456; the two OO reds that dropped this cluster from train 40).</para>
    /// <para>⛔ ONLY over Format 1. Every other row's receiving brace is category-specific, so its own sender
    /// rule is already the one that speaks, and only a receiving list carrying real category evidence is
    /// narrowed: an all-unclassified list still reaches the binder that reports the undefined NAME (R30), never
    /// a rule about a category nobody could read.</para></summary>
    public SetFormat? SelectForTo(IReadOnlyList<SetOperandKind> kinds, SetSender sender, out bool exact)
    {
        var format = Select(kinds, SetDirections.To, out exact);
        if (format != SetFormat.F1 || !sender.IsIdentifierOperand) return format;
        bool anyEvidence = false;
        foreach (var k in kinds) anyEvidence |= k != SetOperandKind.Unclassified;
        if (!anyEvidence) return format;
        // The sender is classified only here — one extra probe per `SET … TO <identifier>` whose receiving
        // list is Format 1's, and none at all for the carrier and register formats.
        uint senderBit = 1u << (int)KindOfSender(sender);
        foreach (var row in Formats)
            if ((row.Dir & SetDirections.To) != 0 && (row.SendsMask & senderBit) != 0)
            {
                exact = false;
                return row.Format;
            }
        return format;
    }

    /// <summary>⛔ THE RECEIVING OPERANDS OF A SET, AS THE PROGRAMMER WROTE THEM — the ONE place a SET
    /// diagnostic spells its receiver list (kb/Work PB388, the sweep that finished the elision family).
    /// <para>Every receiving brace in §14.9.39.2 is written <c>{ … } …</c>, so every SET diagnostic about a
    /// receiver is about a LIST, and nine call sites had each written their own
    /// <c>string.Join(' ', refs.Select(r =&gt; $"'{r.GetText()}'"))</c>. The duplication was not the harm: the
    /// harm was that five OTHER messages, holding the same list, opened <c>SET … TO</c> with a U+2026 instead,
    /// and the diagnostic renderer transliterates U+2026 to ASCII — so the user read <c>SET . TO SELF</c>, a
    /// statement nobody wrote. With one helper the naming is what a new arm reaches for, which is what makes
    /// the next SET diagnostic name its receivers without anyone remembering to.</para></summary>
    public static string Written(IReadOnlyList<Core.DataReferenceContext> receivers) =>
        string.Join(' ', receivers.Select(r => $"'{r.GetText()}'"));

    /// <summary>COBOLNET2112 — the residual arm §14.9.39.2 needs and did not have: NO printed general format's
    /// receiving brace admits these operands, so the statement is refused rather than executed as Format 1/2
    /// arithmetic (kb/Work PB449 — <c>SET WS-N UP BY 4</c> over <c>PIC 9(4)</c> ran, and answered 5).</summary>
    /// <param name="amount">The sending/amount operand AS WRITTEN. ⛔ Never a U+2026 ellipsis: the diagnostic
    /// renderer transliterates it to ASCII, and the user then reads a statement nobody wrote (kb/Work PB388).</param>
    public void ReportNoFormat(IReadOnlyList<Core.DataReferenceContext> receivers,
                              IReadOnlyList<SetOperandKind> kinds, SetDirections dir, string amount)
    {
        string written = Written(receivers);
        string what = string.Join("; ", receivers
            .Select((r, i) => $"'{r.GetText()}' is {Describe(kinds[i])}")
            .Distinct());
        string braces = string.Join("; ", Formats.Where(f => (f.Dir & dir) != 0).Select(f => f.Brace));
        ctx.Edition.Error(DiagnosticCatalog.SetNoFormatAdmitsReceiver,
            $"SET {written} {(dir == SetDirections.UpDown ? "UP/DOWN BY" : "TO")} {amount}: no SET general format "
            + $"admits this receiving operand list — {what}, and {braces} (ISO §14.9.39.2)");
    }

    /// <summary>COBOLNET2112 over the operands a SELECTED format does not admit — the arm for the formats whose
    /// own binder screens only what it can resolve (Format 2's index-names, Format 14's and Format 16's single
    /// receiver). The carrier formats report their own SR through their own binder instead, which is why this is
    /// not called for them.</summary>
    public void ReportNotAdmitted(IReadOnlyList<Core.DataReferenceContext> receivers,
                                  IReadOnlyList<SetOperandKind> kinds, SetFormat format)
    {
        var row = Formats.First(f => f.Format == format);
        for (int i = 0; i < receivers.Count; i++)
        {
            if (kinds[i] == SetOperandKind.Unclassified || row.Admits.Contains(kinds[i])) continue;
            ctx.Edition.Error(DiagnosticCatalog.SetNoFormatAdmitsReceiver,
                $"SET '{receivers[i].GetText()}': {row.Brace}, and '{receivers[i].GetText()}' is "
                + $"{Describe(kinds[i])} ({row.Rule})");
            return;
        }
    }

    /// <summary>COBOLNET2112 for a format whose printed receiving operand carries NO ellipsis — Format 14's
    /// <c>data-name-2</c> and Format 16's <c>data-name-3</c> are each ONE operand (§14.9.39.2), so a second
    /// receiver means the statement is not that format either.</summary>
    public void ReportNotAdmittedCardinality(IReadOnlyList<Core.DataReferenceContext> receivers, SetFormat format)
    {
        var row = Formats.First(f => f.Format == format);
        ctx.Edition.Error(DiagnosticCatalog.SetNoFormatAdmitsReceiver,
            $"SET {Written(receivers)}: {row.Brace} — ONE operand, "
            + $"written with no ellipsis, and {receivers.Count} receiving operands are specified ({row.Rule})");
    }

    /// <summary>The operand's kind in the standard's own words, for a diagnostic.</summary>
    private static string Describe(SetOperandKind k) => k switch
    {
        SetOperandKind.IndexName => "an index-name",
        SetOperandKind.IndexDataItem => "an index data item",
        SetOperandKind.IntegerItem => "an integer data item",
        SetOperandKind.CapacityRegister => "a dynamic-capacity register",
        SetOperandKind.DynamicLength => "a dynamic-length elementary item",
        SetOperandKind.DataPointer => "of category data-pointer",
        SetOperandKind.ProgramPointer => "of category program-pointer",
        SetOperandKind.FunctionPointer => "of category function-pointer",
        SetOperandKind.ObjectReference => "of class object",
        SetOperandKind.MessageTag => "a message-tag data item",
        SetOperandKind.OtherDataItem => "neither an index-name nor an integer data item",
        _ => "not resolvable here",
    };
}
