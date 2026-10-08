// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The INSPECT verb binder (P7 Step 10c — the FIRST real binder collaborator over
/// <see cref="BinderContext"/>, extracted from the <c>StatementBinder.Inspect</c> partial; the census's
/// cleanest file proves the Step-9-style pattern: ctx + transitional host edges for the shared operand
/// spine). The edition-invariant SR error-halves live in <c>StatementValidation</c> (pure checks — the
/// SR6/SR9 figurative-expansion operand REWRITE is bind logic and stays here); the 0845 BACKWARD gate moved
/// VERBATIM (the pass-folding is Exec Step E's scope). The <c>BoundInspect*</c> records/enums stayed in
/// <c>Binding/Bound/BoundInspect.cs</c> — the Tally/Replace enum ordinals are runtime ABI.</summary>
internal sealed class InspectBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind INSPECT (ISO §14.9.22): resolve identifier-1 (SR1 — an alphanumeric or national group or an elementary
    /// usage display or national item), flatten the TALLYING/REPLACING operands across all counters in SOURCE order (the GR8a
    /// shared-cycle order), and bind CONVERTING to its from/to maps (GR20). BACKWARD is 2023-only
    /// (VERSION_CHANGE_REFERENCE row 77 / E.3.3 item 34); TRAILING and tallying FIRST are not in any ISO format —
    /// both fail loud rather than silently aliasing to ALL.</summary>
    public BoundStatement Bind(Core.InspectStatementContext ins)
    {
        // ── identifier-1 may be a FUNCTION-IDENTIFIER, but only where the statement does not MODIFY it (PB10).
        // §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so this position admits one; §8.4.3.2.3
        // SR1 bars it from a RECEIVING operand. INSPECT splits BY FORMAT, and the split is derived, not assumed:
        // §14.9.22.4 GR1 concedes only that "for purposes of determining its length, identifier-1 is treated as a
        // sending data item" (a scoped concession that would be pointless if it were generally sending); GR7 has
        // each match "tallied (format 1) or replaced by literal-3 (format 2)"; and GR20 makes format 4 execute AS
        // a format 2 over the same identifier-1. So Format 1 (TALLYING alone) SENDS and Formats 2/3/4 RECEIVE.
        // ⚠ The screen keys on the PHRASES PRESENT rather than on a format number, because that is what the
        // grammar gives us and it is the same predicate the emitter already computes as `mutated` — one fact,
        // not two representations of it.
        bool modifies = ins.inspectReplacingPhrase() is not null || ins.inspectConvertingPhrase() is not null;
        // Both function-identifier SPELLINGS funnel here — the keyword form (`FUNCTION F(…)`, PB10) and the
        // §8.4.3.2.3 SR2 keyword-OMITTED form (kb/Work R15): SR2 makes them ONE reference, so they share one
        // SR1 screen and one bind. The omitted form used to fall through name resolution into the loud stage
        // ("INSPECT of unresolvable item"), compiling clean and dying at run time — the PB7 shape in the one
        // operand slot that never consulted the hook. KeywordOmittedFunction itself yields to a declared data
        // item, so a genuine subscripted reference still resolves as data below.
        BoundStatement BindFunctionTarget(BoundOperand fnOperand, string spelling)
        {
            if (modifies)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.FunctionIdentifierReceiving,
                    $"INSPECT identifier-1 is the function-identifier '{spelling}', but this INSPECT "
                    + (ins.inspectConvertingPhrase() is not null
                        ? "CONVERTS it (ISO §14.9.22.2 Format 4, which §14.9.22.4 GR20 executes as a Format 2 "
                          + "over the same identifier-1)"
                        : "REPLACES characters in it (ISO §14.9.22.2 Format 2/3, §14.9.22.4 GR7)")
                    + " — a receiving operand, which ISO §8.4.3.2.3 SR1 bars a function-identifier from. A "
                    + "function-identifier IS legal as INSPECT identifier-1 in Format 1 (TALLYING only)");
            }
            // Format 1: identifier-1 is only READ. It binds as an ordinary sending operand and flows to the
            // emitter's AsString read; nothing stores back, so no Place is needed. SR1's usage constraint is
            // satisfied by construction — a function's returned value is a temporary elementary item of the
            // function's own category (§15.4), never a binary/packed/float/index storage form.
            return BindPhrases(fnOperand, ins);
        }
        if (ins.functionCall() is { } fnTarget)
            return BindFunctionTarget(host.Intrinsic.IntrinsicOperand(fnTarget), fnTarget.GetText());
        // §8.4.3.1.2 Format 4 in the same SENDING position as Format 1 (kb/Work PB428): INSPECT's
        // identifier-1 is a sending operand for TALLYING and a receiving one for REPLACING/CONVERTING,
        // which BindFunctionTarget already partitions — one reading, two identifier formats.
        if (ins.inlineMethodInvocation() is { } imiTarget)
            return BindFunctionTarget(host.Oo.OoInlineInvocationOperand(imiTarget), imiTarget.GetText());
        if (host.Intrinsic.KeywordOmittedFunction(ins.dataReference()) is { } kof)
            return BindFunctionTarget(IntrinsicBinder.OperandOf(kof), ins.dataReference().GetText());
        // An INDEX-NAME as identifier-1 (kb/Work R16): INSPECT is none of §13.18.38.3 r7's five index-name
        // contexts — a compile diagnostic, not the unresolvable-item runtime stage below.
        // ⛔ Both lanes, deliberately: identifier-1 is an IDENTIFIER SLOT, so there is no occurrence-number
        // coercion to offer under --permissive (the r7 lane split is written down at
        // ExpressionBinder.ScreenIndexNameOperand and ReferenceResolver.IndexNameInPositionError — kb/Work PB219).
        if (host.Expr.IndexFieldOf(ins.dataReference()) is not null)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.IndexNameContext,
                $"INSPECT identifier-1 is the index-name '{ins.dataReference().GetText()}', which is not an "
                + "identifier (ISO §8.4.3.1.2); §13.18.38.3 r7 admits an index-name only as a subscript, in "
                + "PERFORM/SEARCH VARYING, in SET, or in a relation condition");
        }
        // ⛔ IDENTIFIER-1 IS A RECEIVING OPERAND EXACTLY WHEN THE STATEMENT MODIFIES IT (kb/Work PB881). §14.9.22.3
        // SR8 makes it "a sending operand" in Format 1 (TALLYING only); REPLACING and CONVERTING store into it, so
        // it is then a receiving data item and every receiving-operand prohibition applies — §13.18.15.3 SR2 above
        // all: `INSPECT CA REPLACING ALL "a" BY "b"` over a CONSTANT RECORD rewrote the constant, where the
        // identical MOVE is refused. The SAME `modifies` fact BindFunctionTarget partitions on selects the entry,
        // so the function-identifier arm and the data-reference arm cannot disagree (feedback_two_arm_dispatch).
        Place? target;
        if (modifies)
        {
            if ((target = host.Expr.ResolveReceiving(ins.dataReference())) is null)
                return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236)
            ctx.Data.OoClaimInterleavedReceiver(target);      // InspectEmitter places an object-property identifier-1's accessors around its access (kb/Work PB2078)
        }
        else if (host.Expr.ResolveSending(ins.dataReference()) is var sent && (target = sent.Place) is null)
            return sent.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
        // SR1: identifier-1 is an alphanumeric/national group or an elementary usage DISPLAY/NATIONAL item — a
        // binary/packed/float/index elementary item has no character image to inspect. USAGE NATIONAL joined
        // the admitted set at Phase 4a (M2-DATA-3): a national item is a plain string under D-N1, so the
        // character-based INSPECT machinery applies unchanged (the cross-class operand-MIX validation across
        // the whole operand set is SR4, enforced in BindPhrases — kb/Work PB980). Display-form boolean items pass the Display arm.
        // ⛔ DA7 — A COMPILE-TIME DIAGNOSTIC, not a run-time stage. The verdict was always right (SR1 genuinely bars
        // an elementary binary/packed/float/index identifier-1), but returning BoundUnsupported meant the illegal
        // program COMPILED CLEAN and threw only when control reached the INSPECT. The standard promises a syntax
        // error, so a user was getting a crash where they were owed a diagnostic. Edition-invariant — SR1 is
        // unchanged at 85/2002/2014/2023, so this is deliberately NOT gated. Note the rule constrains only an
        // ELEMENTARY operand: `target.Item.Pic is { }` is false for a GROUP, so an alphanumeric group — including
        // one holding a BINARY/PACKED leaf, which SR1 admits outright as "an alphanumeric or national group item" —
        // never reaches this check.
        // kb/Work PB856: SR1's elementary arm is SR2's, read from the ONE predicate both rules share, and its group
        // arm names exactly two kinds — so a bit, strongly-typed or variable-length group is refused too.
        if (!Validation.StatementValidation.IsInspectIdentifier1(target.Item))
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CharacterOperandUsage,
                $"INSPECT identifier-1 '{target.Item.CobolName}' is "
                + (ItemCategory.IsGroupItem(target.Item)
                    ? $"one of the {ItemCategory.Spell(ItemCategory.GroupKindsOf(target.Item))}"
                    : $"an elementary item of USAGE {ItemCategory.UsageOf(target.Item)?.ToString() ?? "(none)"}")
                + "; SR1 admits an alphanumeric or national GROUP item or an ELEMENTARY item described implicitly "
                + "or explicitly as usage display or national (ISO §14.9.22.3 SR1)");
        }

        return BindPhrases(new BoundFieldOperand(target), ins);
    }

    /// <summary>Bind the TALLYING / REPLACING / CONVERTING phrases over an already-bound identifier-1. Split out
    /// from <see cref="Bind"/> so the DATA-REFERENCE and FUNCTION-IDENTIFIER targets share one body rather than
    /// two copies of the phrase walk (PB10) — the target's SHAPE is the only thing that differs, and it differs
    /// only in whether a Place exists to store back into.</summary>
    private BoundStatement BindPhrases(BoundOperand targetOperand, Core.InspectStatementContext ins)
    {
        // ⛔ ISO §14.9.22.3 SR4 (kb/Work PB980; formerly "Phase-4a residue #12"): "If any of identifier-1,
        // identifier-3, identifier-4, identifier-5, identifier-6, identifier-7, literal-1, literal-2, literal-3,
        // literal-4, or literal-5 references an elementary data item or literal of class boolean or national,
        // then all shall reference a data item or literal of class boolean or national, respectively." The
        // TALLYING counter (identifier-2) is not named. The rule shape STRING SR1 and UNSTRING SR3 share, asked of
        // the ONE predicate (AllOrNothingClass).
        _sr4Operands = [Sr4Entry(targetOperand)];
        _identifier1IsBoolean = IntrinsicArgumentRules.ClassOf(targetOperand) == CobolClass.Boolean;
        try
        {
            var bound = BindPhraseOperands(targetOperand, ins);
            if (bound is BoundInspect)
            {
                var all = _sr4Operands.Select(e => e.Class).ToArray();
                var triggers = _sr4Operands.Where(e => e.Triggers).Select(e => e.Class).ToArray();
                foreach (var governing in (ReadOnlySpan<CobolClass>)[CobolClass.Boolean, CobolClass.National])
                    if (AllOrNothingClass.Violated(governing, all, triggers))
                    {
                        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CharacterOperandClassMix, $"INSPECT '{ins.dataReference()?.GetText() ?? "identifier-1"}' "
                            + AllOrNothingClass.Offence(governing, "ISO §14.9.22.3 SR4") + " (ISO §14.9.22.3 SR4)");
                    }
            }
            return bound;
        }
        finally { _sr4Operands = null; _identifier1IsBoolean = false; }
    }

    private BoundStatement BindPhraseOperands(BoundOperand targetOperand, Core.InspectStatementContext ins)
    {
        bool backward = ins.BACKWARD() is not null;   // inspect-backward-2023: the pass owns the edition gate (Exec Step E)

        var tallying = new List<BoundInspectTally>();
        if (ins.inspectTallyingPhrase() is { } tallyPhrase)
            foreach (var item in tallyPhrase.inspectTallyingItem())
            {
                // ⛔ THE TALLYING COUNTER IS A RECEIVING OPERAND, SO IT RESOLVES AT THE RECEIVING CHOKEPOINT
                // (kb/Work PB429). §14.9.22.4 GR12 a): "the content of the data item referenced by identifier-2
                // is incremented by one for each occurrence of literal-1 matched" — which makes identifier-2 a
                // receiver, and ExpressionBinder.ResolveReceiving is where every receiver-side
                // rule is written down ONCE: §8.4.3.15.3 SR1 admits PAGE-COUNTER here ("any context where an
                // integer data item may appear") and SR3 refuses LINE-COUNTER, a constant-name is refused by
                // §13.10.4 GR1, EXCEPTION-OBJECT by §8.4.3.6.3 SR1. The sending resolver used here before knew
                // none of them, so a legal PAGE-COUNTER counter fell out as an unresolved name and became a
                // COBOLNET1756 run-time abort — the chokepoint's other arm, unfixed.
                if (host.Expr.ResolveReceiving(item.dataReference()) is not { } counter)
                    return new BoundUnsupported("INSPECT TALLYING count operand");   // the chokepoint reported it — not a deferral (kb/Work PB236)
                ctx.Validation.CheckInspectTallyCounter(counter);   // SR5 — pure check; binding continues
                // The grammar spells the printed phrase (PB1125): ONE FOR per counter, each CHARACTERS / ALL / LEADING
                // phrase carrying its adjective once and then its operands — GR10, ALL and LEADING are transitive
                // across the operands that follow them, so each operand below inherits the phrase's adjective.
                foreach (var cp in item.inspectCountPhrase())
                {
                    if (cp.CHARACTERS() is not null)
                    {
                        var (cBefore, cAfter) = InspectDelimiters(cp.inspectDelimiters());
                        tallying.Add(new BoundInspectTally(counter, InspectTallyKind.Characters, null, cBefore, cAfter));
                        continue;
                    }
                    if (cp.FIRST() is not null || cp.TRAILING() is not null)
                        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "INSPECT … TALLYING … FOR "
                            + (cp.FIRST() is not null ? "FIRST" : "TRAILING")
                            + ": the tallying-phrase of ISO §14.9.22.2 prints only CHARACTERS, ALL and LEADING");
                    InspectTallyKind kind = cp.ALL() is not null ? InspectTallyKind.All : InspectTallyKind.Leading;
                    foreach (var operand in cp.inspectCountOperand())
                    {
                        var (before, after) = InspectDelimiters(operand.inspectDelimiters());
                        tallying.Add(new BoundInspectTally(counter, kind, InspectCharOperand(operand.sendingOperand()).Op, before, after));
                    }
                }
            }

        var replacing = new List<BoundInspectReplace>();
        if (ins.inspectReplacingPhrase() is { } replPhrase)
        {
            // GR16: ALL, FIRST, and LEADING are transitive across the pairs that follow them until the next adjective
            // — the grammar nests them under their adjective (PB1125), so each pair inherits its item's kind.
            foreach (var item in replPhrase.inspectReplacingItem())
            {
                if (item.CHARACTERS() is not null)
                {
                    var (cBefore, cAfter) = InspectDelimiters(item.inspectDelimiters());
                    var (rep, repFigurative) = InspectCharOperand(item.sendingOperand());
                    ctx.Validation.CheckInspectCharactersReplacement(rep);   // SR7 — pure check
                    replacing.Add(new BoundInspectReplace(InspectReplaceKind.Characters, null, rep, cBefore, cAfter, repFigurative));
                    continue;
                }
                if (item.TRAILING() is not null)
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "INSPECT … REPLACING TRAILING: the replacing-phrase of ISO §14.9.22.2 "
                        + "prints only CHARACTERS, ALL, LEADING and FIRST");
                InspectReplaceKind kind = item.ALL() is not null ? InspectReplaceKind.All
                    : item.FIRST() is not null ? InspectReplaceKind.First
                    : InspectReplaceKind.Leading;

                foreach (var pair in item.inspectReplacingPair())
                {
                    var (before, after) = InspectDelimiters(pair.inspectDelimiters());
                    var (pat, _) = InspectCharOperand(pair.sendingOperand(0));
                    var (rep2, figurative) = InspectCharOperand(pair.sendingOperand(1));
                    // SR6 / GR14: a figurative literal-3 "is expanded or contracted to be the size of literal-1", and
                    // GR14 says the same of identifier-3 — whose size is a RUN-TIME fact for a function-identifier or a
                    // dynamic-length item. So the runtime sizes it, for every pattern shape alike (kb/Work PB1126: a
                    // bind-time expansion that answered only a static width left `ALL FUNCTION TRIM(P) BY SPACES` a
                    // one-character replacement the size check then skipped). Only two LITERALS are checked here.
                    ctx.Validation.CheckInspectReplacingSize(pat, rep2, figurative);   // SR6 — pure check
                    replacing.Add(new BoundInspectReplace(kind, pat, rep2, before, after, figurative));
                }
            }
        }

        BoundInspectConvert? converting = null;
        if (ins.inspectConvertingPhrase() is { } conv)
        {
            // §14.9.22.2 Format 4 (kb/Work R38, adjudicated 2026-08-08): CONVERTING's operands are
            // {identifier-6 | literal-4} TO {identifier-7 | literal-5} — an ALPHABET-name is NEITHER (a
            // SPECIAL-NAMES name is not an identifier), so GnuCOBOL's CONVERTING-alphabet extension
            // (`INSPECT X CONVERTING BETA TO ALPHA` over `ALPHABET BETA IS EBCDIC`) is not ISO in any
            // edition. The reference kept the honest declared-as-alphabet-name STAGING pending this
            // adjudication (R32/R38's diagnostic half); the adjudicated posture is the R37-family
            // compile-time rejection, at the position whose format decides.
            foreach (var ch in conv.sendingOperand())
                if (ch.dataReference()?.cobolWord()?.GetText() is { } w
                    && !ctx.Symbols.TryResolve(w, ctx.ActiveScope, out _)
                    && (ctx.Data.Alphabets.ContainsKey(w) || ctx.Data.NationalAlphabets.ContainsKey(w)))
                    ctx.Edition.Error(DiagnosticCatalog.UndefinedReference,
                        $"INSPECT CONVERTING references the ALPHABET-name '{w}' — Format 4's operands are "
                        + "an identifier or a literal (ISO §14.9.22.2), and an alphabet-name is neither "
                        + "(the CONVERTING-alphabet form is a GnuCOBOL extension). Write the character "
                        + "strings, or a data item holding them");
            var (from, _) = InspectCharOperand(conv.sendingOperand(0));
            // Literal-5 is the ONE INSPECT literal SR3 does not bar from beginning with the word ALL (kb/Work PB1128).
            var (to, figurative) = InspectCharOperand(conv.sendingOperand(1), literal5: true);
            // SR9 / GR22: a figurative literal-5 takes literal-4's / identifier-6's size — at RUN time, the same
            // reason as SR6 above (kb/Work PB1126).
            ctx.Validation.CheckInspectConvertingSize(from, to, figurative);   // SR9 — pure check
            // The after-before-phrase is the ONE inspectDelimiters rule the other two formats use (PB1125).
            var (before, after) = InspectDelimiters(conv.inspectDelimiters());
            converting = new BoundInspectConvert(from, to, before, after, figurative);
        }

        return new BoundInspect(targetOperand, tallying, replacing, converting, backward);
    }

    /// <summary>Bind a per-operand BEFORE/AFTER delimiter pair (ISO §14.9.22.2 after-before-phrase; both may
    /// appear on one operand, in either order — disambiguated by token index, since the grammar's two alternatives
    /// share the merged BEFORE/AFTER accessors). INITIAL is a noise word.</summary>
    private (BoundOperand? Before, BoundOperand? After) InspectDelimiters(Core.InspectDelimitersContext? c)
    {
        if (c is null) return (null, null);
        var chars = c.sendingOperand();
        if (c.BEFORE() is { } b && c.AFTER() is { } a)
        {
            var first = InspectCharOperand(chars[0]).Op;
            var second = InspectCharOperand(chars[1]).Op;
            return b.Symbol.TokenIndex < a.Symbol.TokenIndex ? (first, second) : (second, first);
        }
        if (c.BEFORE() is not null) return (InspectCharOperand(chars[0]).Op, null);
        if (c.AFTER() is not null) return (null, InspectCharOperand(chars[0]).Op);
        return (null, null);
    }

    /// <summary>Bind an INSPECT operand (identifier-3..7 / literal-1..5 / figurative): a figurative constant is an
    /// implicit ONE-character item (ISO §14.9.22.3 SR3 — and a figurative beginning with ALL is forbidden there);
    /// an identifier reads its FULL raw image at run time (GR5/GR6 — no trimming; GR4d de-signs a signed numeric
    /// operand at the read). <c>Figurative</c> reports the figurative origin so SR6/SR9 can expand a replacement
    /// to the pattern size.</summary>
    private (BoundOperand Op, bool Figurative) InspectCharOperand(Core.SendingOperandContext c, bool literal5 = false)
    {
        var bound = InspectCharOperandOf(c, literal5);
        // ⛔ SR3: "when identifier-1 is of class boolean, the figurative constant is of class boolean and only the
        // figurative constant ZERO may be specified" (kb/Work PB1127) — every other figurative, a symbolic-character
        // and the ALL-literal literal-5 included, is a syntax error, not a character the runtime then guesses at.
        if (_identifier1IsBoolean && bound.Figurative && bound.Op is not BoundFigurative { Kind: 'Z' })
            return (BoundOperandError.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule,
                $"INSPECT operand '{c.GetText()}' is a figurative constant other than ZERO, but identifier-1 is of "
                + "class boolean, and only the figurative constant ZERO may be specified there (ISO §14.9.22.3 SR3)",
                $"INSPECT operand '{c.GetText()}' (ISO §14.9.22.3 SR3)"), false);
        // SR4's operand record (kb/Work PB980). A FIGURATIVE operand (and a bare symbolic character, which IS one)
        // takes identifier-1's class by SR3 — "When identifier-1 is of class national, the class of the figurative
        // constant is national; when identifier-1 is of class boolean, the figurative constant is of class
        // boolean" — so it is never recorded and can never be the mismatch. The ALL-literal figurative literal-5
        // is the exception: SR3's class sentence is written over "literal-1, literal-2, or literal-4", and an
        // `ALL N"…"` carries the class of the LITERAL written in it, so SR4 asks it like any literal.
        if (!bound.Figurative || bound.Op is BoundAllLiteral)
            _sr4Operands?.Add(Sr4Entry(bound.Op));
        return bound;
    }

    /// <summary>The operand classes of the INSPECT statement being bound, for ISO §14.9.22.3 SR4 — set for the
    /// duration of <see cref="BindPhrases"/> and read once at its end. Every sending-operand position funnels
    /// through <see cref="InspectCharOperand"/>, so recording there reaches all of them without a per-phrase copy.</summary>
    private List<(CobolClass? Class, bool Triggers)>? _sr4Operands;

    /// <summary>True while binding an INSPECT whose identifier-1 is of class boolean — SR3's "only the figurative
    /// constant ZERO may be specified" is asked of every operand against it (kb/Work PB1127). Set and cleared with
    /// <see cref="_sr4Operands"/>.</summary>
    private bool _identifier1IsBoolean;

    /// <summary>One SR4 entry: the operand's §8.5.2.1 class, and whether it can TRIGGER the rule — SR4 is written
    /// over an operand that "references an ELEMENTARY data item or literal of class boolean or national", while
    /// "all shall reference a data item or literal" of that class, so a group conforms by its class but never
    /// triggers.</summary>
    private static (CobolClass? Class, bool Triggers) Sr4Entry(BoundOperand op) =>
        (IntrinsicArgumentRules.ClassOf(op), op is not BoundFieldOperand { Place.Item.IsGroup: true });

    /// <summary>⛔ SR3's last sentence, asked of every literal position (kb/Work PB1127): "Literal-1, literal-2,
    /// literal-3, literal-4, or literal-5 shall not be a zero-length literal" — a zero-length literal has no
    /// character to tally, replace or convert, and as a BEFORE/AFTER delimiter it silently changed the answer
    /// (BEFORE '' counted the whole item). Edition-invariant: the rule names no edition.</summary>
    private BoundOperand ScreenZeroLength(BoundOperand literal, Core.SendingOperandContext c) =>
        literal is BoundStringLiteral { Value.Length: 0 }
            ? BoundOperandError.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule,
                $"INSPECT operand '{c.GetText()}' is a zero-length literal, which literal-1 through literal-5 shall "
                + "not be (ISO §14.9.22.3 SR3)",
                $"INSPECT operand '{c.GetText()}' (ISO §14.9.22.3 SR3)")
            : literal;

    private (BoundOperand Op, bool Figurative) InspectCharOperandOf(Core.SendingOperandContext c, bool literal5)
    {
        var fig = c.literal()?.nonNumericLiteral()?.figurativeConstant();
        if (fig is not null)
        {
            // ⛔ SR3: "Literal-1, literal-2, literal-3, and literal-4 shall not be a figurative constant that begins
            // with the word ALL" — EVERY form beginning with the word (ALL literal-1 PB71, ALL symbolic-character-1
            // PB110, and ALL ZERO/SPACE/HIGH-VALUE/LOW-VALUE/QUOTE, which this screen used to let through as the bare
            // word), and ONLY those four: literal-5 is not named, so `CONVERTING "ABC" TO ALL "Q"` is legal and was
            // refused (kb/Work PB1128).
            if (fig.ALL() is not null)
            {
                if (!literal5)
                    return (BoundOperandError.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule,
                        $"INSPECT operand '{ConcatFolder.Spelling(fig)}' is a figurative constant that begins with the "
                        + "word ALL, which literal-1, literal-2, literal-3 and literal-4 shall not be (ISO §14.9.22.3 SR3)",
                        "INSPECT operand ALL figurative (ISO §14.9.22.3 SR3)"), false);
                // Literal-5's ALL literal-1 / ALL symbolic-character-1, through THE ONE figurative binder (it folds a
                // concatenated or constant-name literal-1 and reports §8.3.3.6.3 SR2/SR4): a BoundAllLiteral whose
                // value the runtime repeats to identifier-6's / literal-4's size (GR22), exactly as §8.3.3.6.4 GR2
                // repeats any ALL literal to its associated size. ALL ZERO / SPACE / … binds as the bare word's
                // BoundFigurative and takes the one-character arm below — the same figurative, filled to size.
                var allFig = host.Expr.FigurativeOperand(fig);
                if (allFig is BoundAllLiteral or BoundOperandError) return (allFig, allFig is BoundAllLiteral);
            }
            // A one-character figurative stays a BoundFigurative, so the emitter reads HIGH-/LOW-VALUE from the
            // collating sequence of identifier-1's category (§8.3.3.6.4 GR6/GR7; kb/Work PB1414) — not the native pin.
            return (host.Expr.FigurativeOperand(fig), true);
        }
        // ⛔ A FUNCTION-IDENTIFIER OPERAND (ISO §8.4.3.1.2 Format 1; fix-queue PB45). §14.9.22.2 writes these as
        // `identifier-n | literal-n` and every one is a SENDING operand, so `INSPECT S TALLYING N FOR ALL
        // FUNCTION UPPER-CASE(P)` is conforming. It used to be a PARSE error; once the grammar admitted it, this
        // arm was still missing and the operand fell through to the not-implemented tail, which reported a
        // MISLEADING reason — "a numeric literal is not a valid INSPECT literal" — for something that is neither
        // numeric nor a literal. Routed through the ONE intrinsic-operand entry the other verbs use.
        if (c.functionCall() is { } fc) return (host.Intrinsic.IntrinsicOperand(fc), false);
        if (c.inlineMethodInvocation() is { } imi)   // §8.4.3.1.2 Format 4; kb/Work PB428
            return (host.Oo.OoInlineInvocationOperand(imi), false);
        // ⛔ THE ONE nonNumericLiteral MAPPING (kb/Work DA3), not a private copy of it (kb/Work PB1427). This arm
        // used to decode concatenation / STRINGLIT / NATLIT / BOOLLIT by hand — a copy with no HEXLIT arm, so
        // `INSPECT … FOR ALL X"41"` (§8.3.3.2 Format 2 is the ALPHANUMERIC literal) fell to the tail below and was
        // refused as "a numeric literal", and with no NULL arm, so the predefined NULL — which is no INSPECT
        // operand (§8.4.3.10.3 SR1) — drew the same wrong reason. The mapping folds a concatenation (§8.8.3.3
        // GR3), decodes each literal class char-correct (the SR4 class mix is recorded by InspectCharOperand and
        // enforced in BindPhrases — kb/Work PB980), and refuses NULL by the ONE SR1 screen (COBOLNET2576).
        if (host.Expr.NonNumericLiteralOperand(c.literal()?.nonNumericLiteral()) is { } litOp)
            return (ScreenZeroLength(litOp, c), false);
        if (c.dataReference() is { } dref)
        {
            // ⛔ A CONSTANT-NAME IS A LITERAL HERE (kb/Work PB1127): §13.10.3 SR2 "may be used anywhere that a format
            // specifies a literal", §13.10.4 GR1 "as if literal-1 ... were written" — so it substitutes BEFORE the
            // identifier arm below, which would refuse it as an undefined data-name. The SAME bound shape the written
            // literal produces, so SR3's class/zero-length screens and SR4/SR6/SR7/SR9 all see it as one.
            if (host.Expr.ConstantOperand(dref) is { } konst)
                return konst is BoundNumericLiteral
                    ? (BoundOperandError.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule,
                        $"INSPECT operand '{dref.GetText()}' is a numeric constant-name; each INSPECT literal shall be an "
                        + "alphanumeric, boolean, or national literal (ISO §14.9.22.3 SR3, §13.10.3 SR2)",
                        $"INSPECT operand '{dref.GetText()}' (ISO §14.9.22.3 SR3)"), false)
                    : (ScreenZeroLength(konst, c), false);
            // A bare symbolic character IS a figurative constant (§12.3.7.4 GR11; kb/Work PB110) — one character,
            // with the SR6 / GR14 figurative expansion the literal figuratives get.
            if (ctx.Data.SymbolicOf(dref) is { } sym)
                return (new BoundStringLiteral(sym.Value) { Category = sym.National ? PicCategory.National : PicCategory.Alphanumeric }, true);
            if (host.Expr.ResolveSending(dref) is var pr && pr.Place is not { } p)
                return (pr.OperandError(ctx.Edition), false);   // the resolver's answer (kb/Work PB1030)
            ctx.Validation.CheckInspectOperandUsage(p, dref.GetText());   // SR2 — pure check
            return (new BoundFieldOperand(p), false);
        }
        // The grammar admits a numeric literal here; SR3 does not (alphanumeric/boolean/national literals only).
        return (BoundOperandError.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule,
            $"INSPECT operand '{c.GetText()}' is a numeric literal; each INSPECT literal shall be an alphanumeric, "
            + "boolean, or national literal (ISO §14.9.22.3 SR3)",
            $"INSPECT operand '{c.GetText()}' (ISO §14.9.22.3 SR3)"), false);
    }
}
