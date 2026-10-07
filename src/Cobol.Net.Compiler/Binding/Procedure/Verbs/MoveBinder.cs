// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The MOVE figurative-constant edition gates (ISO/IEC 1989:2023 §14.9.25.3 SR5; roadmap Phase 2 W2 track A —
/// VCR rows 1 / 92 / 128): SR5 permits "an ALL "literal" figurative constant (containing only digits) or an ALL
/// symbolic-character (representing a digit)" to move "to an integer numeric item"; "in all other cases, the move
/// of an alphanumeric figurative constant (SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal", or ALL
/// symbolic-character) to either a numeric item or a numeric-edited item is prohibited". The prohibition is NEW in
/// 2023 — these moves were permitted through ISO 2014 (Annex E.2 item 1 bullet 1, the removals list) — so the
/// registry row "move-alphanumeric-figurative-removed-2023" routes it: silent below 2023, error at 2023 strict,
/// warning at 2023 --permissive (the pre-removal semantics preserved). The surviving digit-only-ALL-to-integer
/// case is itself obsolete at 2023 (the SR5 NOTE; Annex F.2 item 2) — row "move-all-digit-integer-obsolete-2023",
/// a 0903 warning at ≥2023 and silent below.
/// </summary>
/// <summary>The MOVE verb binder (P7 Step 10e — a real collaborator over <see cref="BinderContext"/>;
/// absorbs the former <c>StatementBinder.MoveFigurative</c> partial WHOLE: the SR1 class-index errors, the
/// pre-removal <c>MarkImageForced</c> storage marking, the Table-16 legality arms, and the W2 ref-mod
/// image marking fire at the SAME per-statement points — the collected-fact choreography the
/// <c>StorageFormPass</c> and <c>VersionConformancePass.GateMove</c> depend on is byte-preserved).</summary>
/// <summary>
/// The syntax rules ONE verb's <c>… FROM {identifier-1 | literal-1}</c> phrase carries IN ADDITION to the MOVE
/// statement's own — one row per verb, read by <see cref="MoveBinder.BindFromPhrase"/>.
///
/// <para>⛔ The columns are SETS and NULLABLE clauses, not flags, because the three verbs' rules genuinely
/// differ and a scalar "the function must be alphanumeric" column would reject legal source: RELEASE
/// (§14.9.32.3 SR2) and WRITE (§14.9.51.3 SR4) admit an alphanumeric or national function, REWRITE
/// (§14.9.35.3 SR9) also admits a BOOLEAN one, and only RELEASE (§14.9.32.3 SR4) forbids a zero-length
/// literal-1. Adding the next FROM phrase is a row here, not another screen
/// (feedback_model_the_rule_shape_not_one_case).</para>
/// </summary>
/// <param name="Statement">How the statement names itself in a diagnostic — <c>RELEASE … FROM</c>.</param>
/// <param name="MoveRuleCite">The clause making the phrase an implicit MOVE, plus the clause subjecting
/// identifier-1/literal-1 to the MOVE statement's own syntax rules.</param>
/// <param name="FunctionCategories">The result categories a function-identifier in this position may have, or
/// <see langword="null"/> where the verb states no such rule.</param>
/// <param name="FunctionRuleCite">The quoted rule behind <paramref name="FunctionCategories"/>.</param>
/// <param name="ZeroLengthLiteralRuleCite">The quoted rule forbidding a zero-length literal-1, or
/// <see langword="null"/> where the verb states none.</param>
internal sealed record FromPhraseRules(
    string Statement,
    string MoveRuleCite,
    PicCategory[]? FunctionCategories,
    string? FunctionRuleCite,
    string? ZeroLengthLiteralRuleCite)
{
    /// <summary>The <see cref="ImplicitMovePhrase"/> every move of THIS verb's FROM phrase carries — built once
    /// per verb, beside the three static rows, rather than once per bound statement. It is the same object the
    /// two INTO phrases keep on <c>IntoPhraseRules.Read</c> / <c>.Return</c>, their own twin of this row; a phrase is a
    /// property of the VERB, not of an occurrence of it, so all five are singletons.</summary>
    public ImplicitMovePhrase Phrase { get; } = new(Statement, MoveRuleCite);

    /// <summary>RELEASE (ISO §14.9.32) — the only FROM phrase carrying the zero-length-literal rule.</summary>
    public static readonly FromPhraseRules Release = new(
        "RELEASE … FROM",
        "ISO §14.9.32.4 GR4 a); §14.9.32.3 SR3",
        [PicCategory.Alphanumeric, PicCategory.National],
        "ISO §14.9.32.3 SR2 — \"If identifier-1 is a function-identifier, it shall reference an alphanumeric or "
        + "national function.\"",
        "ISO §14.9.32.3 SR4 — \"Literal-1 shall not be a zero-length literal.\"");

    /// <summary>WRITE (ISO §14.9.51). §14.9.51.3 SR4 is UNCONDITIONAL and stricter than the FILE-less SR11
    /// ("alphanumeric, boolean, or national"), so a conforming program satisfies both only inside SR4's set —
    /// which is what is enforced. The FILE-phrase arm (SR10) is unreachable: Annex A.4.13 item 2 is declined.</summary>
    public static readonly FromPhraseRules Write = new(
        "WRITE … FROM",
        "ISO §14.9.51.4 GR5 a); §14.9.51.3 SR6",
        [PicCategory.Alphanumeric, PicCategory.National],
        "ISO §14.9.51.3 SR4 — \"If identifier-1 is a function-identifier, it shall reference an alphanumeric or "
        + "national function.\"",
        null);

    /// <summary>REWRITE (ISO §14.9.35). §14.9.35.3 SR9's set is the WIDER one — it admits a boolean function —
    /// and it is the applicable rule because the FILE phrase (SR8's condition) is declined, A.4.13 item 2.</summary>
    public static readonly FromPhraseRules Rewrite = new(
        "REWRITE … FROM",
        "ISO §14.9.35.4 GR7 a); §14.9.35.3 SR6",
        [PicCategory.Alphanumeric, PicCategory.Boolean, PicCategory.National],
        "ISO §14.9.35.3 SR9 — \"If identifier-1 references a function and the FILE phrase is not specified, "
        + "identifier-1 shall reference an alphanumeric, boolean, or national function.\"",
        null);
}

internal sealed class MoveBinder(BinderContext ctx, StatementBinder host, CorrespondingBinder corr)
{
    private int _elementLoopVar;   // program-unique §14.6.9.2 element-loop counter (__vlmN — `__` cannot occur in COBOL names)

    public BoundStatement Bind(Core.MoveStatementContext move)
    {
        if (move.CORRESPONDING() is not null || move.CORR() is not null)   // Format 2 — BOTH tokens (§14.9.25.3 SR11)
            return corr.Bind(CorrVerb.Move, move.dataReference(), CobolRounding.Truncation, null);
        // ⛔ THE MESSAGE NO LONGER NAMES "MOVE CORRESPONDING" (kb/Work PB421). It used to read "MOVE
        // CORRESPONDING / unsupported MOVE form", and that was the arm `MOVE <sending> CORRESPONDING id-3 TO
        // id-4` reached — a shape §14.9.25.2 prints no format for, which `moveReceivingPhrase`'s deleted second
        // alternative admitted and this loud stage then answered AT RUN TIME. Format 2 is handled whole at the
        // CORRESPONDING test above, so what remains here is a defensive arm over the one printed receiving
        // shape, and it must not claim an unimplemented feature that this binder in fact implements.
        if (move.moveSendingOperand() is not { } send || move.moveReceivingPhrase()?.dataReferenceList() is not { } targets)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, $"MOVE '{move.GetText()}': Format 1 prints one sending operand and "
                + "TO identifier-2 … (ISO §14.9.25.2)");
        BoundOperand source = send.literal() is { } lit ? host.Expr.LiteralOperand(lit)
            : send.dataReference() is { } dref ? host.Expr.FieldOperand(dref)
            // MOVE FUNCTION … TO targets (ISO §14.9.25 + §15.2 — a function is a sending item of its category).
            : send.functionCall() is { } sfc ? host.Intrinsic.IntrinsicOperand(sfc)
            // MOVE {inline method invocation} TO targets — §14.9.25.3 SR3 designates identifier-1 as a
            // sending operand, and §8.4.3.1.2 Format 4 is one of the eleven identifiers that designation
            // reaches (kb/Work PB428). §8.4.3.4.3 SR1 keeps it off the RECEIVING side structurally.
            : send.inlineMethodInvocation() is { } simi ? host.Oo.OoInlineInvocationOperand(simi)
            : BoundOperandError.Refused(ctx.Edition, "MOVE source");
        // An INDEX-NAME sending operand (kb/Work R16): MOVE is not among §13.18.38.3 r7's five index-name
        // contexts — the same judgment the SR1 arm below applies to class-index DATA ITEMS (COBOLNET0809).
        // Before this, a string-category receiver aborted at RUN time and a numeric one silently computed.
        if (send.dataReference() is { } sdref
            && host.Expr.ScreenIndexNameOperand(source, sdref.GetText(), "a MOVE sending operand"))
            source = BoundOperandError.Refused(ctx.Edition, $"MOVE of the index-name '{DataBinder.WrittenText(sdref)}' (ISO §13.18.38.3 r7)");
        var resolved = host.Expr.ResolveTargets(targets.dataReference());
        return BindMoveOf(source, resolved);
    }

    /// <summary>
    /// ⛔ <b>THE ONE APPLICATION OF THE MOVE STATEMENT'S OWN RULES TO A SENDER/RECEIVER PAIR</b> — the explicit
    /// MOVE statement above, and EVERY implicit move a phrase's rules define (<c>READ … INTO</c>,
    /// <c>RETURN … INTO</c>, <c>WRITE/REWRITE/RELEASE … FROM</c>), which reach it through
    /// <see cref="BindFromPhrase"/> / <see cref="BindIntoPhrase"/>.
    ///
    /// <para><b>Why it exists (kb/Work PB348).</b> The implicit moves used to be constructed in the EMITTER —
    /// <c>move.Emit(new BoundMove(from, [rl.Record]))</c> — downstream of every screen below AND of the storage
    /// facts the emitter's own output depends on. Two consequences, both measured: <c>RELEASE SRT-REC FROM
    /// WS-NUM</c> (a PIC 9(3) sender into a PIC A(8) record) compiled clean where the identical explicit
    /// <c>MOVE</c> drew COBOLNET0819; and <c>RELEASE SRT-NUM FROM QUOTE</c> ABORTED THE RUN with an unhandled
    /// <c>NotImplementedCobolFeatureException</c>, because <see cref="MarkImageForced"/> — a fact
    /// <c>StorageFormPass</c> consumes — is collected HERE and the emitter-built move never passed through.
    /// §14.9.32.4 GR4 makes <c>RELEASE record-name-1 FROM x</c> exactly <c>MOVE x TO record-name-1</c> followed
    /// by the same RELEASE, so "exactly" has to include the bind.</para>
    ///
    /// <para>Everything a MOVE's own rules impose lives in this one call, so a phrase added tomorrow inherits it
    /// by construction rather than by an edit. <paramref name="implicitOf"/> is null for the explicit statement
    /// and names the phrase otherwise, so each screen's diagnostic reports the statement that was written.</para>
    /// </summary>
    public BoundMove BindMoveOf(BoundOperand source, IReadOnlyList<Place> targets,
                                ImplicitMovePhrase? implicitOf = null)
    {
        // ⛔ THE SYNTAX SCREENS RUN UNLESS THE STATEMENT HAS ALREADY ASKED THE SAME QUESTION ITS OWN WAY
        // (kb/Work PB880). INITIALIZE is the one such statement: §14.9.20.3 SR4 asks the MOVE validity question
        // once per REPLACING CATEGORY (InitializeBinder.CheckReplacingMoveValidity, through the same chain), and
        // the §14.9.20.4 GR6a/GR6c senders are valid by construction — so asking it again per receiver would
        // report one error twice. Everything below the screens (the freeze, the storage facts) runs for every
        // move, because that is what an implicit move built anywhere else used to miss.
        if (implicitOf is not { ValidityAskedByStatement: true })
        {
            // The §14.9.25.3 SR1 operand-class check (the SR5 edition gates are VersionConformancePass Step 14f).
            MoveOperandClassChecks(source, targets, implicitOf);
            // §14.9.25.3 SR2 / SR6–SR8 / SR9 / SR10 — the whole validity chain, MoveTable16.Validity, framed per
            // rule. SR9 rides it (kb/Work PB393's screen; every FROM / INTO phrase's "shall be valid … in a MOVE
            // statement" rule inherits it here), and so does SR2 (data-model D17).
            MoveCategoryLegality(source, targets, implicitOf);
        }
        // A ref-mod slice store on a numeric-DISPLAY receiver needs image backing for ANY sender (§8.4.3.3.4 GR6;
        // the W2 adversarial-review round-trip-loss fix — see MarkRefModStoreImage).
        MarkRefModStoreImage(targets);
        // ⛔ §14.9.25.4 GR1 — "If identifier-1 is reference-modified, subscripted, or is a function-identifier,
        // the reference modifier, subscript, or function-identifier is evaluated only once, immediately before
        // data is moved to the first of the receiving operands", and the rule's own equivalence writes the
        // required shape out: MOVE a (b) TO b, c (b) ≡ MOVE a (b) TO temp / MOVE temp TO b / MOVE temp to c (b),
        // "where 'temp' is an intermediate result item provided by the implementor". The SENDING value is
        // frozen into that item HERE (kb/Work PB394) — the receiver side already conforms, since item
        // identification for identifier-2 happens immediately before each store (the per-target loop in
        // MoveEmitter is the receiver half of the same rule).
        // ⭐ ONLY when there is more than one receiving operand, and that is a PROOF, not a shortcut: with a
        // single identifier-2 the source expression is rendered exactly once already, and the equivalence's own
        // first two steps collapse — temp's description IS identifier-1's, so `MOVE a (b) TO temp / MOVE temp TO
        // b` applies the identity copy and then the same conversion `MOVE a (b) TO b` applies. The intermediate
        // is unobservable at N = 1, and MOVE is the most executed verb in a COBOL program.
        // ⭐ AND the SECOND reason to freeze it, the same rule's other sentence: GR1's "If identifier-1 is a
        // zero-length item, it is as if literal-1 were specified as a zero-length literal" makes the sender's
        // LENGTH decide which value is stored, and "the length … is evaluated only once" then forbids reading a
        // reference modifier or a function-identifier twice to learn it. Materializing lands the value in
        // SendingValueTemp's dynamic-length carrier, whose length the emitted test reads off a field
        // (kb/Work PB425; MoveClassifier.NeedsLengthFreeze names the shapes).
        if ((targets.Count > 1 || MoveClassifier.NeedsLengthFreeze(source, targets))
            && host.SendingValue.Materialize(source, "move") is { } frozen)
            source = new BoundFieldOperand(frozen);
        // ⛔ §14.6.9.2's element moves are bound HERE, beside the group move they follow (kb/Work PB1144), so every
        // GR9 move — the written MOVE and each implicit phrase move alike — carries them, and each is a MOVE this
        // method screened and storage-marked rather than one the emitter synthesizes.
        var move = new BoundMove(source, targets) { ImplicitOf = implicitOf, ElementMoves = ElementMovesOf(source, targets) };
        // The fill's STORAGE fact is collected off the CONSTRUCTED node, so it is asked of the same per-target
        // store the emitter renders — including the sender §14.9.25.4 GR2 substitutes (kb/Work PB425).
        MarkFillImageStorage(move);
        return move;
    }

    // ── ISO §14.6.9.2's element moves of a §14.9.25.4 GR9 group move (kb/Work PB1144) ─────────────────────────

    /// <summary>⛔ THE §14.6.9.2 ELEMENT MOVES OF A VARIABLE-LENGTH GROUP MOVE — "Correspondingly numbered elements
    /// are moved according to the rules of the MOVE statement specified in 14.9.25, MOVE statement" — one
    /// <see cref="TableElementMove"/> per receiving operand GR9 governs (<see cref="MoveClassifier.VariableLengthGroupSender"/>,
    /// the antecedent the emitter's group transfer asks) and per corresponding table pair of that operand pair
    /// (<see cref="VariableLengthCompatibility.CorrespondingTables"/>, the same walk §14.9.25.3 SR9 matched them by).
    /// <para>The group transfer (<c>CobolVarGroup</c>) moves each table as element IMAGES, and an image IS the
    /// MOVE's result for exactly two shapes, which take no element move:</para>
    /// <list type="bullet">
    ///   <item>a GROUP element pair — §14.9.25.4 GR4 makes a fixed-length group move an alphanumeric move
    ///     "without conversion", and §8.5.1.12.3's matching made the two elements the same byte length;</item>
    ///   <item>an elementary pair with IDENTICAL descriptions (<see cref="OoConformance.DescriptionMismatch"/>, the
    ///     ONE identical-description check) — the conversion is then the identity.</item>
    /// </list>
    /// <para>Every other elementary pair — any category pair §14.9.25.3 admits, since §8.5.1.12.3 compares only
    /// the elements' byte lengths ("Two corresponding tables match when the byte length of their elements is equal
    /// and their elements are compatible", and compatibility is defined for groups) — gets the real elementary MOVE,
    /// bound by <see cref="BindMoveOf"/> over the two element places at a loop-local occurrence number, so its
    /// Table-16 legality is asked of the ELEMENT pair (a refusal is a diagnostic at the MOVE the program wrote) and
    /// GR6's conversion is the one every MOVE uses.</para></summary>
    private IReadOnlyList<TableElementMove> ElementMovesOf(BoundOperand source, IReadOnlyList<Place> targets)
    {
        List<TableElementMove>? moves = null;
        for (int t = 0; t < targets.Count; t++)
        {
            if (MoveClassifier.VariableLengthGroupSender(source, targets[t]) is not { } send) continue;
            foreach (var (sTable, rTable) in VariableLengthCompatibility.CorrespondingTables(send.Item, targets[t].Item))
            {
                if (sTable.IsGroup || rTable.IsGroup) continue;                         // GR4: the image IS the move
                if (OoConformance.DescriptionMismatch(sTable, rTable) is null) continue;  // identity conversion
                (moves ??= []).Add(ElementMove(t, send, sTable, targets[t], rTable));
            }
        }
        return moves ?? [];
    }

    /// <summary>One corresponding pair's element move: the sending and receiving element places at the loop's
    /// occurrence number, each reached from its OWN group's resolved place through the ONE bind-time cursor
    /// (<see cref="PlaceCursor"/> — so a subscripted or qualified group keeps its subscripts), and the two current
    /// counts of §14.6.9.1 (<see cref="OccurrenceCounts.Current"/>).</summary>
    private TableElementMove ElementMove(int target, Place send, DataItem sTable, Place recv, DataItem rTable)
    {
        string v = $"__vlm{_elementLoopVar++}";
        var sCur = TableCursor(send, sTable);
        var rCur = TableCursor(recv, rTable);
        var sCount = sCur is null ? null : OccurrenceCounts.Current(sTable, sCur.StoragePath, ctx.Refs);
        var rCount = rCur is null ? null : OccurrenceCounts.Current(rTable, rCur.StoragePath, ctx.Refs);
        if (sCur is null || rCur is null || sCount is null || rCount is null)
            return new TableElementMove(target, v, sCount, rCount, null,
                $"the element moves of the corresponding tables '{sTable.CobolName ?? "FILLER"}' and "
                + $"'{rTable.CobolName ?? "FILLER"}' of a variable-length group MOVE: a table reached through a storage "
                + "form whose element places this implementation does not build (ISO §14.6.9.2)");
        var elementMove = BindMoveOf(new BoundFieldOperand(ElementPlace(sCur, sTable, v)),
            [ElementPlace(rCur, rTable, v)], ImplicitMovePhrase.TableElement);
        return new TableElementMove(target, v, sCount, rCount, elementMove, null);
    }

    /// <summary>The cursor at <paramref name="table"/> inside the resolved group <paramref name="group"/>: down the
    /// table's ancestor chain from the group (§8.5.1.12's atoms flatten only scalar subordinate groups, so no OCCURS
    /// level lies between them). Null when the table is not a subordinate of the group's item (a level-66 alias,
    /// whose atoms are its span's leaves) or a storage form on the way has no cursor.</summary>
    private static PlaceCursor? TableCursor(Place group, DataItem table)
    {
        var chain = new Stack<DataItem>();
        DataItem? n = table;
        for (; n is not null && !ReferenceEquals(n, group.Item); n = n.Parent) chain.Push(n);
        PlaceCursor? cur = n is null ? null : PlaceCursor.Over(group);
        while (cur is not null && chain.Count > 0) cur = cur.Child(chain.Pop());
        return cur;
    }

    /// <summary>Occurrence <paramref name="v"/> of <paramref name="table"/> at its cursor: a dynamic-capacity
    /// table's element through its direction-specific accessor (data-model D9 — a read is RefSending, a store
    /// RefReceiving, which within the current capacity never grows it), any other table through its OCCURS index.</summary>
    private static Place ElementPlace(PlaceCursor tableCursor, DataItem table, string v) =>
        (table.IsDynamicTable ? tableCursor.DynamicElement(v) : null)?.ToPlace() ?? tableCursor.Indexed(v).ToPlace();

    // ── The … FROM and … INTO phrases (kb/Work PB348) ───────────────────────────────────────────────────────

    /// <summary>
    /// Bind a <c>… FROM {identifier-1 | literal-1}</c> phrase into its implicit MOVE — WRITE (ISO §14.9.51),
    /// REWRITE (§14.9.35) and RELEASE (§14.9.32) alike. Null when no FROM phrase is written.
    ///
    /// <para>The operand is bound, the verb's OWN phrase syntax rules are applied (<paramref name="rules"/>),
    /// and the move is then screened by <see cref="BindMoveOf"/> under the MOVE statement's rules — which is
    /// what each verb's "shall be valid as a sending operand in a MOVE statement specifying record-name-1 as
    /// the receiving operand" rule says to do (§14.9.32.3 SR3 · §14.9.35.3 SR6 · §14.9.51.3 SR6).</para>
    ///
    /// <para>⛔ This is the successor to the former <c>SequentialIoBinder.WriteSource</c>, which bound the
    /// operand and inspected NOTHING — a three-way hand-off from the three verbs, each of which carries its own
    /// syntax rules that were therefore applied by no one, and the move itself was then built in the emitter.
    /// The home moved here because this class is where a MOVE's rules live; the sort binder no longer reaches
    /// across into the sequential I-O binder for it.</para>
    /// </summary>
    public BoundMove? BindFromPhrase(FromPhraseRules rules, Place record, Core.DataReferenceContext? dref,
                                     Core.LiteralContext? lit, Core.FunctionCallContext? fc,
                                     Core.InlineMethodInvocationContext? imi)
    {
        if (dref is null && lit is null && fc is null && imi is null) return null;   // no FROM phrase
        BoundOperand source =
            // §8.4.3.1.2 Format 4 (kb/Work PB428). The per-verb FunctionCategories screen below is
            // deliberately NOT extended to it: §14.9.51.3 SR4, §14.9.35.3 SR9 and §14.9.32.3 SR2 each
            // restrict "a FUNCTION-IDENTIFIER" by name, and none states a rule for an inline invocation —
            // so what governs it is the phrase's own implicit MOVE, which BindMoveOf applies.
            imi is not null ? host.Oo.OoInlineInvocationOperand(imi)
            : fc is not null ? host.Intrinsic.IntrinsicOperand(fc)
            : lit is not null ? host.Expr.LiteralOperand(lit)
            : host.Expr.FieldOperand(dref!);

        // ── The FUNCTION-IDENTIFIER class restriction, a SET per verb because the rules name different sets:
        // RELEASE §14.9.32.3 SR2 and WRITE §14.9.51.3 SR4 admit "an alphanumeric or national function";
        // REWRITE §14.9.35.3 SR9 admits "an alphanumeric, boolean, or national function" when the FILE phrase is
        // not specified (and the FILE arm, §14.9.35.3 SR8 / §14.9.51.3 SR10, is Annex A.4.13 item 2 — DECLINED
        // COBOLNET1706 before this point, so only the no-FILE readings are reachable). A scalar "must be
        // alphanumeric" column here would reject legal source on the REWRITE arm.
        // The class is read off the BOUND operand through the same Table-16 sender position the category screen
        // uses, so a folded integer function (FUNCTION LENGTH constant-folds to a numeric literal, which is how
        // it escaped every earlier reading of this position) still answers "numeric", not "unknown". ──
        if (fc is not null && rules.FunctionCategories is { } admitted
            && MoveTable16.SenderPosition(source).Category is var cat && !admitted.Contains(cat))
            ctx.Edition.Error(DiagnosticCatalog.FromPhraseFunctionClass,
                $"{rules.Statement} {fc.GetText()}: {rules.FunctionRuleCite} — this function's result is of "
                + $"category {cat.ToString().ToLowerInvariant()}");

        // ── §14.9.32.3 SR4, "Literal-1 shall not be a zero-length literal." RELEASE's alone: neither
        // §14.9.51.3 nor §14.9.35.3 states it for the FROM phrase of WRITE / REWRITE, so it is carried on the
        // rules row rather than applied to every verb. The compiler already owns the predicate — it is the same
        // BoundStringLiteral test ControlFlowBinder applies to the STOP RUN / GOBACK status literal. ──
        if (rules.ZeroLengthLiteralRuleCite is { } zlCite && source is BoundStringLiteral { Value.Length: 0 })
            ctx.Edition.Error(DiagnosticCatalog.FromPhraseZeroLengthLiteral,
                $"{rules.Statement}: {zlCite}");

        return BindMoveOf(source, [record], rules.Phrase);
    }

    /// <summary>
    /// Bind a <c>… INTO identifier-1</c> phrase into its implicit MOVE — the sequential READ, the keyed READ and
    /// the sort RETURN (ISO §14.9.30.4 GR4 b) and §14.9.34.4 GR5 b), which are the same sentence: "The current
    /// record is moved from the record area to the area specified by identifier-1 according to the rules for the
    /// MOVE statement without the CORRESPONDING phrase").
    ///
    /// <para>The SENDER is the record area sliced to its §13.18.43.4 GR16 byte count, and — for a FORMAT 2
    /// <c>RECORD IS VARYING</c> file only — designated an alphanumeric group move by those same rules. That
    /// choice is <see cref="BoundCurrentRecord"/>'s and was built in ONE place already (kb/Work PB339); it moved
    /// here from the emitter with the move it belongs to, because an operand is a bind-time object.</para>
    ///
    /// <para>⛔ The verb's OWN syntax rules for the phrase are applied FIRST — §14.9.30.3 SR1/SR2 for READ and
    /// §14.9.34.3 SR2/SR3 for RETURN, through <c>StatementValidation.CheckIntoReceiver</c> (kb/Work PB337) —
    /// exactly as <see cref="BindFromPhrase"/> applies each FROM verb's own rules before handing the pair to
    /// <see cref="BindMoveOf"/>. They were applied by NO ONE: the operand was resolved and never inspected, so a
    /// strongly-typed group could be filled from an alphanumeric record image, which nothing else in this
    /// compiler allows. The check runs here rather than at the three binders BECAUSE this is the one place all
    /// three funnel through — the next INTO-bearing verb inherits it by construction.</para>
    ///
    /// <para>The move is bound EVEN WHEN the phrase is refused, because these are pure checks (the
    /// <c>StatementValidation</c> contract) and the bound shape downstream — <c>BoundRead.IntoMove</c> and its
    /// siblings — stays what every emitter and drift test expects for a statement that was written.</para>
    /// </summary>
    public BoundMove BindIntoPhrase(FileModel file, Place area, Place receiver, IntoPhraseRules rules)
    {
        ctx.Validation.CheckIntoReceiver(file, receiver, rules);
        // §13.18.43.4 GR16 a) reads the DEPENDING item; GR16 b) applies when the phrase is absent. Resolved
        // HERE, once, for all three INTO arms — the sequential READ, the keyed READ and the sort RETURN.
        Place? depending = file is { Varying.DependingName: not null, VaryingDependingItem: { } d }
            ? ctx.Refs.ResolveItem(d) : null;
        // A FIXED-length file keeps the plain record-area operand: its current record IS the whole area
        // (§13.18.43.4 GR6 — integer-1 bytes in every record), including the short-final-record '04' case of
        // §14.9.30.4 GR14, where the area right of the last valid character is undefined rather than short.
        return BindMoveOf(
            file.Varying is { } v ? new BoundCurrentRecord(area, file, depending, v.VaryingClause)
                                  : new BoundFieldOperand(area),
            [receiver], rules.Phrase);
    }

    /// <summary>
    /// The §14.9.25.3 SR1 OPERAND-CLASS check on one bound MOVE (Format 1) — version-invariant, every sender and
    /// receiver kind. The SR5 figurative EDITION gates it used to carry live in
    /// <c>VersionConformancePass.GateMove</c> (Step 14f) and the pre-removal storage marking in
    /// <see cref="MarkFigurativeFillImage"/>, so what is left here is one rule: "The class of identifier-1 or
    /// identifier-2 shall not be index, message-tag, object, or pointer."
    /// </summary>
    private void MoveOperandClassChecks(BoundOperand source, IReadOnlyList<Place> targets,
                                        ImplicitMovePhrase? implicitOf)
    {
        // The §14.9.25.3 SR1 class check FIRST — version-invariant, every operand kind: "The class of
        // identifier-1 or identifier-2 shall not be index, message-tag, object, or pointer." A MOVE operand of
        // one of those classes is invalid at EVERY edition, never an Annex-E removal (the W2 adversarial review
        // caught the 0902 row mislabeling it "permitted through 2014").
        // SR1 reaches a FUNCTION sender through §15.2 item 6 (kb/Work PB124 wave 5b): "Index functions.
        // These are of the class and category index." — MAX/MIN over index arguments IS one, and its result's
        // storage category (Numeric) made it indistinguishable from a numeric sender here, so
        // MOVE FUNCTION MAX(IX1 IX2) TO 9(n) silently stored an occurrence-number image (GR-15.2-6).
        // ⛔ BOTH SENDER ARMS NOW ASK ONE READER (kb/Work PB416): MoveTable16.SenderClassRefusal classifies
        // through IntrinsicArgumentRules.ClassOf — the ONE §8.5.2.1 Table-2 answer — so §14.9.20.3 SR4's
        // hypothetical MOVE (INITIALIZE REPLACING) gets the identical verdict, and the index-NAME sender shape
        // neither hand-written arm matched is covered by construction.
        // ⛔ AND SO DOES THE RECEIVING ARM (kb/Work PB423). It read `t.Item.Pic is { Usage: Usage.Index }` — one
        // hand-written USAGE, under a comment claiming "message-tag/object/pointer classes cannot reach a bound
        // MOVE yet (their usages are compile-gated skeletons, W2 track B)". PicInfo's own XML docs called all
        // three usages LIVE, and the measurement agreed: `MOVE P TO Y` over a USAGE POINTER sender printed
        // Y=[CobolNet] (the CLR carrier's type name) into an alphanumeric item, and `MOVE NULL TO P` reached
        // Roslyn and surfaced CS0029 about generated C#. One rule, one core, asked of both positions.
        if (MoveTable16.SenderClassRefusal(source) is { } classRefusal)
            ctx.Edition.Error("COBOLNET0809", classRefusal
                + (source is BoundFieldOperand sIdx
                    ? $" — MOVE {sIdx.Place.Item.CobolName}{ImplicitMovePhrase.Via(implicitOf)}"
                    : ImplicitMovePhrase.Via(implicitOf)));
        foreach (var t in targets)
            if (MoveTable16.ReceiverClassRefusal(t) is { } recvRefusal)
                ctx.Edition.Error("COBOLNET0809",
                    $"{recvRefusal} — {ImplicitMovePhrase.WhereOf(implicitOf, t.Item.CobolName)}");

        // (The §14.9.25.3 SR5 EDITION gates — MoveAllDigitIntegerObsolete2023 / MoveQuoteNumericObsolete2014 /
        // MoveAlphanumericFigurativeRemoved2023 — moved to the post-bind VersionConformancePass (Step 14f),
        // which re-derives the SAME classification from the bound MOVE's WRITTEN source. The pre-removal STORAGE
        // marking is MarkFigurativeFillImage, off the constructed node.)
    }

    /// <summary>
    /// Pre-removal FILL STORAGE (reachable at 85/2002/2014 + 2023 --permissive regardless of the SR5 gate, and at
    /// EVERY edition through §14.9.25.4 GR2): a NON-digit fill — SPACE/QUOTE/HIGH-VALUE/LOW-VALUE, an ALL literal
    /// with a non-digit, or the figurative SPACE GR2 substitutes for a zero-length literal — deposits the fill
    /// CHARACTERS as the receiver's character image (provisional; the pre-2023 fill — MOVE QUOTE TO
    /// PIC 9(3) leaves three quotation marks, IS NUMERIC is then false, and a later read decodes deterministically
    /// per §14.6.13.2). Flag an eligible elementary numeric-DISPLAY receiver <c>StoreAsImage</c> — the SAME §14.9
    /// MOVE GR4 whole-group image substrate, never a parallel mechanism.
    /// <para>⛔ ELIGIBILITY IS THE NODE'S OWN <see cref="MoveKind"/>, asked of the constructed
    /// <see cref="BoundMove"/> (kb/Work PB425). It used to be a hand-copy of <see cref="MoveClassifier"/>'s
    /// figurative-into-numeric test written out again here — two statements of one rule, and the copy could not
    /// see the GR2/GR3 substitution the classifier applies, so <c>MOVE "" TO PIC 9(3)</c> would have rendered a
    /// space image into an item with no image backing. What remains below is only the STORAGE-shape half the kind
    /// does not decide: a digit-only ALL stores its numeric value natively (it classifies Convert, so the kind
    /// already excludes it), a numeric-edited receiver is string-backed by nature (likewise not this kind), a
    /// Tier-B REDEFINES window / NumericImagePlace already writes its image, and a REDEFINES shared-storage alias
    /// keeps its (already-run) tier flag. <c>MoveEmitter.EmitFigurativeToNumericImage</c> reads the same kind and
    /// the same flag, which is what keeps the two ends in step.</para>
    /// </summary>
    private void MarkFillImageStorage(BoundMove move)
    {
        for (int i = 0; i < move.Targets.Count; i++)
        {
            var t = move.Targets[i];
            // Two reasons, one storage fact: the sender IS the fill (a written figurative / non-digit ALL, or the
            // one §14.9.25.4 GR2 substitutes for a zero-length literal), or the sender MAY BE the fill at run
            // time (GR1's zero-length-item clause — the emitted store's zero arm writes the same image, so the
            // receiver has to be able to hold one whichever way the test goes).
            if (move.Stores[i].Kind is not MoveKind.FigurativeToNumericImage
                && MoveClassifier.ZeroLengthItemRoute(move.Stores[i].Sender, t) is null) continue;
            if (t is not (RedefViewPlace or NumericImagePlace) && t.Item.Class is null
                && t.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })   // CARRIAGE, not image form (kb/Work PB646)
                ctx.Data.MarkImageForced(t.Item);   // the collected image fact
        }
    }

    /// <summary>ISO §14.9.25.3 SR2, SR6–SR10 for one MOVE, per receiver — asked of <see cref="MoveTable16.Validity(BoundOperand, Place)"/>
    /// and framed here under each rule's own diagnostic (COBOLNET1533 · COBOLNET1931 · COBOLNET0819). Version-
    /// invariant: SR5's edition rows are <c>VersionConformancePass</c>'s.</summary>
    private void MoveCategoryLegality(BoundOperand source, IReadOnlyList<Place> targets,
                                      ImplicitMovePhrase? implicitOf)
    {
        foreach (var t in targets)
        {
            // ⛔ THE WHOLE §14.9.25.3 VALIDITY QUESTION, ASKED OF THE ONE CHAIN (kb/Work PB878). This method used
            // to compose ShapeRefusal + Refusal by hand and skip GROUP receivers outright, while SR2 and SR9 were
            // asked by two more screens (StatementValidation.CheckStrongMove / .CheckVariableLengthMove) — four
            // calls to answer one question, which is the shape that let INITIALIZE and INVOKE ask a subset. The
            // RULES now live in MoveTable16.Validity, in SR order; what stays here is the explicit statement's
            // FRAMING — each rule's own diagnostic code, and SR10's --permissive re-reading.
            // ⚠ A GROUP RECEIVER IS ASKED TOO. Table 16 exempts it itself (§14.9.25.4 GR4) and SR2/SR9 are
            // group rules; the one rule the old `continue` hid is SR8, whose letter — "identifier-2 shall
            // reference a numeric or numeric-edited item" — a group item does not satisfy. MOVE CORRESPONDING's
            // pairing filter already read it that way (kb/Work PB391); the written MOVE now agrees with it.
            if (MoveTable16.Validity(source, t) is not { } refusal) continue;
            string where = ImplicitMovePhrase.WhereOf(implicitOf, t.Item.CobolName);
            switch (refusal.Rule)
            {
                case MoveRule.StrongGroup:
                    ctx.Edition.Error(DiagnosticCatalog.StrongMoveMismatch, "MOVE to strongly-typed group "
                        + $"'{t.Item.CobolName ?? t.Item.CsName}': {refusal.Reason}{ImplicitMovePhrase.Via(implicitOf)}");
                    break;
                case MoveRule.VariableLength:
                    // Name the operand that IS the variable-length group — the receiver when it is one.
                    var recv = MoveTable16.OperandItem(t);
                    var vl = recv is not null && VariableLengthCompatibility.IsVariableLength(recv)
                        ? recv : MoveTable16.OperandItem(source)!;
                    ctx.Edition.Error(DiagnosticCatalog.MoveVariableLengthIncompatible,
                        $"MOVE involving the variable-length group '{vl.CobolName ?? vl.CsName}': {refusal.Reason} — "
                        + "ISO §14.9.25.3 SR9 requires the two groups to be compatible as specified in §8.5.1.12"
                        + ImplicitMovePhrase.Via(implicitOf));
                    break;
                case MoveRule.SourceShape:
                    ctx.Edition.Error("COBOLNET0819", $"{where}: {refusal.Reason}");
                    break;
                default:
                    Table16Framing(source, t, where, refusal.Reason);
                    break;
            }
        }
    }

    /// <summary>SR10's framing: the two leniencies (kb/Work PB73) — a NUMERIC-typed function into a character
    /// receiver (Table 16's Noninteger row; every earlier release admitted it as the CONFORMANCE.md item-92 text
    /// form) and a reference-modified ALPHABETIC view read as plain alphanumeric (GnuCOBOL's reading; PB72's
    /// 2026-08-09 erasure) — accepted under --permissive with a warning when the lenient reading admits the move;
    /// every other refusal is an error on both axes. It re-asks TABLE 16 ALONE, and that is the rule: the lenient
    /// reading is defined against Table 16's cells, and the chain has already cleared SR2/SR8/SR9 to get here.</summary>
    private void Table16Framing(BoundOperand source, Place t, string where, string refusal)
    {
        Table16Operand senderPos = MoveTable16.SenderPosition(source);
        bool senderIsFunction = source is BoundComputedOperand { Expr: BoundIntrinsicCall };
        bool senderIsView = source is BoundFieldOperand { Place: RefModPlace };
        if (ctx.Edition.Permissive
            && MoveTable16.Refusal(Table16Operand.Lenient(senderPos, senderIsFunction, senderIsView),
                                   Table16Operand.Lenient(Table16Operand.Of(t), false, t is RefModPlace)) is null)
            ctx.Edition.Warning("COBOLNET0819", $"{where}: {refusal}; accepted under --permissive "
                + (senderIsFunction ? "as the function's literal text (a NUMERIC-typed function is the Noninteger sender, ISO §15.2 item 4)"
                                    : "reading the reference-modified view as plain alphanumeric (ISO §8.4.3.3.4 GR6 keeps it alphabetic)"));
        else
            ctx.Edition.Error("COBOLNET0819", $"{where}: MOVE is invalid — {refusal}"
                + (senderIsFunction && senderPos is { Category: PicCategory.Numeric, IsNonInteger: true }
                    ? " (a NUMERIC-typed function is the Noninteger sender, §15.2 item 4 / §8.4.3.2.3 SR11; an INTEGER function moves to a character receiver; --permissive accepts this as the function's literal text)"
                    : ""));
    }


    /// <summary>
    /// Ref-mod STORE backing (the W2 adversarial-review fix, DEVLOG 595): a MOVE into a reference-modified
    /// slice of a numeric USAGE-DISPLAY item writes CHARACTERS into the item's character positions
    /// (§8.4.3.3.4 GR6 — the unique result is an elementary alphanumeric item). Without image backing the resolver
    /// wraps <c>NumericImagePlace(long)</c> and the spliced image ROUND-TRIPS through the <c>long</c> on
    /// store, silently losing any non-digit deposit (<c>MOVE SPACE TO N(1:2)</c> left N's digits — and the
    /// observable result flipped with whether UNRELATED code elsewhere image-backed the item). Mark the
    /// underlying item <c>StoreAsImage</c> at bind time for EVERY sender kind — digits round-trip either way,
    /// so the flag is safe, and the byte-semantics model (a ref-mod store is a character-cell write) is what
    /// §14.6.8's fixed-width transfer implies. Same substrate + eligibility as the figurative pass; a Tier-B
    /// window already writes character images; a REDEFINES-class member keeps its (already-run) tier
    /// classification — the emitter's narrow loud guard covers that residue.
    /// </summary>
    private void MarkRefModStoreImage(IReadOnlyList<Place> targets)
    {
        foreach (var t in targets)
            if (t is RefModPlace rm
                && rm.Item is { Class: null, Pic: { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display } } item)   // CARRIAGE, not image form (kb/Work PB646)
                ctx.Data.MarkImageForced(item);   // the collected image fact
    }
}
