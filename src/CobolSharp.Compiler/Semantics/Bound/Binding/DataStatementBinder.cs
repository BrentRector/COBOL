// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Data movement statement binding: BindDisplay, BindMove, BindMoveSendingOperand,
/// BindSet (dispatch), BindSetSwitch, BindSetBoolean, BindSetToValue, BindSetIndex,
/// BindInitialize, ClassifyReplacingItem, BindReplacingValue, BindAccept.
/// </summary>
internal sealed class DataStatementBinder
{
    private readonly BindingContext _ctx;

    internal DataStatementBinder(BindingContext ctx) => _ctx = ctx;

    // ── DISPLAY ──

    internal BoundDisplayStatement BindDisplay(CobolParserCore.DisplayStatementContext ctx)
    {
        var operands = new List<BoundExpression>();

        foreach (var child in ctx.children)
        {
            if (child is ITerminalNode t)
            {
                var kind = t.Symbol.Type;
                if (kind == CobolLexer.DISPLAY || kind == CobolLexer.DOT)
                    continue;
            }

            if (child is CobolParserCore.DataReferenceContext idCtx)
            {
                operands.Add(_ctx.Expression.BindDataReferenceWithSubscripts(idCtx));
            }
            else if (child is CobolParserCore.LiteralContext litCtx)
            {
                operands.Add(_ctx.Expression.BindLiteral(litCtx));
            }
        }

        return new BoundDisplayStatement(operands, ctx.displayNoAdvancing() != null);
    }

    // ── MOVE ──

    internal BoundStatement? BindMove(CobolParserCore.MoveStatementContext ctx)
    {
        // MOVE CORRESPONDING source TO target
        if (ctx.CORRESPONDING() != null)
            return _ctx.Arithmetic.BindCorresponding(CorrespondingKind.Move, ctx.dataReference(), ctx);

        var moveSource = ctx.moveSendingOperand();
        var moveTarget = ctx.moveReceivingPhrase();
        if (moveSource == null || moveTarget == null) return null;

        var source = BindMoveSendingOperand(moveSource);

        var targets = new List<BoundExpression>();
        var idList = moveTarget.dataReferenceList();
        if (idList != null)
        {
            foreach (var id in idList.dataReference())
                targets.Add(_ctx.Expression.BindDataReferenceWithSubscripts(id));
        }

        // MOVE type enforcement
        {
            var moveLoc = new SourceLocation(_ctx.SourceName, 0, ctx.Start?.Line ?? 0, 0);
            var moveSpan = TextSpan.Empty;
            foreach (var tgt in targets)
            {
                var tgtCat = tgt.Category;
                // Skip enforcement for group items (treated as alphanumeric byte move)
                if (tgt is BoundIdentifierExpression tgtId && tgtId.Symbol.IsGroup)
                    continue;
                // Skip enforcement for unknown categories
                if (tgtCat == CobolCategory.Unknown)
                    continue;

                // Determine effective source category for the MOVE check
                var effectiveSrcCat = source switch
                {
                    // ZERO/ZEROS/ZEROES is numerically compatible — treat as Numeric
                    BoundFigurativeExpression fig when fig.FigurativeKind == FigurativeKind.Zero
                        => CobolCategory.Numeric,
                    // Other figuratives (SPACE, HIGH-VALUE, etc.) are alphanumeric
                    BoundFigurativeExpression => CobolCategory.Alphanumeric,
                    // Numeric literals → Numeric
                    BoundLiteralExpression lit when lit.Category == CobolCategory.Numeric
                        => CobolCategory.Numeric,
                    // Other literals → their declared category
                    _ => source.Category,
                };

                if (effectiveSrcCat == CobolCategory.Unknown)
                    continue;

                // Figurative ZERO is a legal source for a boolean receiver (the all-'0' boolean value,
                // ISO §8.3.3.6.4 r4); without this it would be classified Numeric and rejected.
                if (tgtCat == CobolCategory.Boolean
                    && source is BoundFigurativeExpression { FigurativeKind: FigurativeKind.Zero })
                    effectiveSrcCat = CobolCategory.Boolean;

                if (!CategoryCompatibility.IsMoveLegal(effectiveSrcCat, tgtCat))
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL0901, moveLoc, moveSpan, effectiveSrcCat, tgtCat);

                // Check 1: MOVE ZERO to Alphabetic
                if (source is BoundFigurativeExpression fig2
                    && fig2.FigurativeKind == FigurativeKind.Zero
                    && tgtCat == CobolCategory.Alphabetic)
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL0908, moveLoc, moveSpan);

                // Check 2: HIGH-VALUE/LOW-VALUE/QUOTE to Numeric
                if (source is BoundFigurativeExpression fig3
                    && fig3.FigurativeKind is FigurativeKind.HighValue
                        or FigurativeKind.LowValue
                        or FigurativeKind.Quote
                    && tgtCat.IsNumericLike())
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL0906, moveLoc, moveSpan, fig3.FigurativeKind);

                // Check 3: Numeric noninteger literal to Alphanumeric
                if (source is BoundLiteralExpression srcLit
                    && srcLit.Category == CobolCategory.Numeric
                    && srcLit.Value is decimal decVal
                    && decVal != decimal.Truncate(decVal)
                    && tgtCat.IsAlphanumericLike()
                    && !tgtCat.IsNumericLike())
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL0907, moveLoc, moveSpan);
            }
        }

        return new BoundMoveStatement(source, targets, isRounded: false);
    }

    internal BoundExpression BindMoveSendingOperand(CobolParserCore.MoveSendingOperandContext ctx)
    {
        // moveSource: literal | functionCall | dataReference (COBOL-85 + 1989 Amendment)
        var litCtx = ctx.literal();
        if (litCtx != null) return _ctx.Expression.BindLiteral(litCtx);

        if (ctx.functionCall() != null)
            return _ctx.Expression.BindFunctionCall(ctx.functionCall());

        if (ctx.dataReference() != null)
            return _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    // ── SET ──

    internal BoundStatement? BindSet(CobolParserCore.SetStatementContext ctx)
    {
        // SET mnemonic-name+ TO {ON | OFF} (switch setting)
        if (ctx.setSwitchStatement() is { } swCtx)
            return BindSetSwitch(swCtx);

        // SET condition-name TO TRUE/FALSE
        if (ctx.setBooleanStatement() is { } boolCtx)
            return BindSetBoolean(boolCtx);

        // SET identifier TO value
        if (ctx.setToValueStatement() is { } toCtx)
            return BindSetToValue(toCtx);

        // SET identifier UP/DOWN BY integer
        if (ctx.setIndexStatement() is { } idxCtx)
            return BindSetIndex(idxCtx);

        // SET ADDRESS OF based TO ptr  /  SET ptr TO ADDRESS OF item (Stage-4 pointers, ISO §14.9.39)
        if (ctx.setAddressStatement() is { } addrCtx)
            return BindSetAddress(addrCtx);

        // SET pointer TO NULL / SET pointer TO another-pointer (parses as the object-reference form, since
        // NULL_ and a data-reference are objectReference alternatives). OO object-reference SET is not yet
        // implemented; here we handle only the pointer case (Phase-1).
        if (ctx.setObjectReferenceStatement() is { } objCtx)
            return BindSetObjectReference(objCtx);

        return null;
    }

    internal BoundStatement? BindSetObjectReference(CobolParserCore.SetObjectReferenceStatementContext ctx)
    {
        // The D-U7 grammar widened the rule to `dataReference+` (ISO §14.9.39 F5 {identifier-3}…) for the
        // greenfield; the FROZEN legacy keeps its single-target scope — first target only (oracle-only code).
        if (_ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference(0))
                is not BoundIdentifierExpression target)
            return null;
        // Pointers (Stage-4): SET pointer TO NULL / SET pointer TO pointer stores into the target's managed
        // ManagedPointer field — NOT an 8-byte byte handle (DEVLOG 431). SELF/SUPER object references are OO
        // (not yet implemented).
        if (target.Symbol.ResolvedType?.Category != CobolCategory.Pointer)
            return null;

        var objRef = ctx.objectReference().objectReferenceAtom();
        if (objRef == null) return null; // a function-identifier or inline-invocation sender — post-oracle
        if (objRef.predefinedNull() != null)
            return new BoundSetPointerStatement(target.Symbol, PointerSetSourceKind.Null);
        if (objRef.dataReference() != null
            && _ctx.Expression.BindDataReferenceWithSubscripts(objRef.dataReference())
                is BoundIdentifierExpression srcPtr
            && srcPtr.Symbol.ResolvedType?.Category == CobolCategory.Pointer)
            return new BoundSetPointerStatement(target.Symbol, PointerSetSourceKind.FromPointer, srcPtr.Symbol);
        return null;
    }

    /// <summary>
    /// Stage-4 pointers: bind <c>SET ADDRESS OF based TO ptr</c> and <c>SET ptr TO ADDRESS OF item</c> (the two
    /// alternatives of <c>setAddressStatement</c>, ISO §14.9.39) to a <see cref="BoundSetPointerStatement"/>.
    /// <list type="bullet">
    /// <item><c>SET ADDRESS OF b TO p</c> — store p's managed reference into b's data-address pointer (FromPointer,
    /// target = the BASED/LINKAGE item b).</item>
    /// <item><c>SET p TO ADDRESS OF x</c> — build a <c>ManagedPointer</c> over x's storage (FromAddressOf; x becomes
    /// byte-backed via classifier trigger 6).</item>
    /// </list>
    /// ⛔ THE SHARED GRAMMAR RULE NOW CARRIES THE WHOLE PRINTED FORMAT (kb/Work PB450): §14.9.39.2 Format 7 is
    /// <c>SET { ADDRESS OF data-name-1 | identifier-5 } … TO identifier-6</c>, a receiving LIST whose operands
    /// each carry their own spelling, so the alternatives are no longer told apart by token ORDER — each
    /// <c>setAddressReceiver</c> states its own kind and <c>setAddressSender</c> states the sender's. This
    /// legacy binder is the differential ORACLE until the P15 cut-over and binds only the two arity-one shapes
    /// it always did; a multi-receiver or mixed statement returns null and falls through to the unsupported
    /// path, exactly as any other post-oracle construct does. The greenfield <c>PtrBinder.BindSetAddress</c>
    /// implements the format.
    /// </summary>
    internal BoundStatement? BindSetAddress(CobolParserCore.SetAddressStatementContext ctx)
    {
        var recvs = ctx.setAddressReceiver();
        var send = ctx.setAddressSender();
        // kb/Work PB239: the sender's ADDRESS phrase is now the shared `dataAddressIdentifier` rule, so its operand
        // is read through that node; `sendRef` is the operand in either spelling.
        var sendAddr = send?.dataAddressIdentifier();
        var sendRef = sendAddr?.dataReference() ?? send?.dataReference();
        if (recvs.Length != 1 || sendRef == null) return null;
        var recv = recvs[0];

        if (recv.ADDRESS() != null)
        {
            // SET ADDRESS OF based TO ptr : the target is the based/linkage item (no expression location — it is
            // addressed through its own pointer); the source is another pointer whose value is copied in.
            if (sendAddr != null) return null;   // ADDRESS OF sender into a based receiver — greenfield only
            var basedSym = _ctx.Semantic.ResolveData(recv.dataReference().cobolWord().GetText());
            if (basedSym == null) return null;
            if (_ctx.Expression.BindDataReferenceWithSubscripts(sendRef) is not BoundIdentifierExpression srcPtr
                || srcPtr.Symbol.ResolvedType?.Category != CobolCategory.Pointer)
                return null;
            return new BoundSetPointerStatement(basedSym, PointerSetSourceKind.FromPointer, srcPtr.Symbol);
        }

        // SET ptr TO ADDRESS OF item : build a ManagedPointer over the addressed item's storage.
        if (sendAddr == null) return null;
        if (_ctx.Expression.BindDataReferenceWithSubscripts(recv.dataReference()) is not BoundIdentifierExpression ptr
            || ptr.Symbol.ResolvedType?.Category != CobolCategory.Pointer)
            return null;
        var addrItem = _ctx.Expression.BindDataReferenceWithSubscripts(sendRef);
        if (addrItem is not BoundIdentifierExpression) return null;
        return new BoundSetPointerStatement(ptr.Symbol, PointerSetSourceKind.FromAddressOf, addressOfItem: addrItem);
    }

    /// <summary>
    /// Stage-4 pointers: bind the ALLOCATE statement (ISO §14.9.3). Form 1 (<c>ALLOCATE n CHARACTERS</c>) carries a
    /// size expression; form 2 (<c>ALLOCATE based-item</c>) carries the BASED item. RETURNING names the pointer that
    /// receives the address (required for form 1 SR2; optional for form 2).
    /// </summary>
    internal BoundStatement? BindAllocate(CobolParserCore.AllocateStatementContext ctx)
    {
        bool initialized = ctx.INITIALIZED() != null;
        var drs = ctx.dataReference();
        DataSymbol? returning = null;

        if (ctx.arithmeticExpression() is { } sizeCtx && ctx.CHARACTERS() != null)
        {
            // Form 1: ALLOCATE n CHARACTERS [INITIALIZED] [RETURNING p]. The lone dataReference is RETURNING p.
            var size = _ctx.Expression.BindArithmeticExpr(sizeCtx);
            if (ctx.RETURNING() != null && drs.Length >= 1)
                returning = _ctx.Semantic.ResolveData(drs[0].cobolWord().GetText());
            return new BoundAllocateStatement(size, null, initialized, returning);
        }

        // Form 2: ALLOCATE based-item [INITIALIZED] [RETURNING p]. drs[0] = based item; drs[1] = RETURNING p.
        if (drs.Length == 0) return null;
        var based = _ctx.Semantic.ResolveData(drs[0].cobolWord().GetText());
        if (based == null) return null;
        if (ctx.RETURNING() != null && drs.Length >= 2)
            returning = _ctx.Semantic.ResolveData(drs[1].cobolWord().GetText());
        return new BoundAllocateStatement(null, based, initialized, returning);
    }

    /// <summary>
    /// Stage-4 pointers: bind the FREE statement (ISO §14.9.15) — set each data-pointer operand to NULL (reusing the
    /// pointer-store-NULL path; the GC reclaims the released byte[]).
    /// </summary>
    internal BoundStatement? BindFree(CobolParserCore.FreeStatementContext ctx)
    {
        var stmts = new List<BoundStatement>();
        foreach (var dr in ctx.dataReference())
        {
            var sym = _ctx.Semantic.ResolveData(dr.cobolWord().GetText());
            if (sym?.ResolvedType?.Category == CobolCategory.Pointer)
                stmts.Add(new BoundSetPointerStatement(sym, PointerSetSourceKind.Null));
        }
        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    internal BoundStatement? BindSetSwitch(CobolParserCore.SetSwitchStatementContext ctx)
    {
        // Grammar: SET setSwitchPhrase+   where   setSwitchPhrase : dataReference+ TO (ON | OFF)
        // The printed §14.9.39.2 Format-3 unit is its own rule (kb/Work PB450), so the grouping is READ off the
        // phrase nodes. This used to walk token indices to reassemble what the parser already knew.
        var switches = new List<(string Name, bool SetToOn)>();
        foreach (var phrase in ctx.setSwitchPhrase())
        {
            bool setToOn = phrase.ON() != null;
            foreach (var dref in phrase.dataReference())
            {
                var switchInfo = _ctx.Semantic.ResolveImplementorSwitch(dref.cobolWord().GetText());
                if (switchInfo != null)
                    switches.Add((switchInfo.ImplementorName, setToOn));
            }
        }

        if (switches.Count == 0) return null;
        // Return a bound node that the Binder can lower to IrSetSwitch
        return new BoundSetSwitchStatement(switches);
    }

    internal BoundStatement? BindSetBoolean(CobolParserCore.SetBooleanStatementContext ctx)
    {
        // Grammar: SET setConditionPhrase+   where   setConditionPhrase : dataReference+ TO (TRUE | FALSE)
        // §14.9.39.2 Format 4's printed outer repetition, one phrase per group (kb/Work PB450), so TRUE/FALSE
        // is read per GROUP and not once for the statement.
        var stmts = new List<BoundStatement>();

        foreach (var phrase in ctx.setConditionPhrase())
        {
            bool setToTrue = phrase.TRUE_() != null;
            foreach (var idCtx in phrase.dataReference())
            {
                string name = idCtx.cobolWord().GetText();
                var condSym = _ctx.Semantic.ResolveConditionName(name);
                if (condSym != null)
                    stmts.Add(new BoundSetConditionStatement(condSym, setToTrue));
            }
        }

        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    internal BoundStatement? BindSetToValue(CobolParserCore.SetToValueStatementContext ctx)
    {
        var identifiers = ctx.dataReference();
        var valueExpr = _ctx.Expression.BindArithmeticExpr(ctx.arithmeticExpression());
        if (valueExpr == null) return null;

        var stmts = new List<BoundStatement>();
        foreach (var idCtx in identifiers)
        {
            // Check if it's a condition name first
            string name = idCtx.cobolWord().GetText();
            var condSym = _ctx.Semantic.ResolveConditionName(name);
            if (condSym != null)
            {
                stmts.Add(new BoundSetConditionStatement(condSym, true));
                continue;
            }

            // Regular data item: SET identifier TO value
            var targetId = _ctx.Expression.BindDataReferenceWithSubscripts(idCtx);
            if (targetId is not BoundIdentifierExpression boundTarget) continue;
            // Pointer target (SET p TO q): a pointer assignment copies the source pointer's managed reference into
            // the target's ManagedPointer field — NOT the numeric index-set path, NOT an 8-byte handle copy
            // (Stage-4, DEVLOG 431). The grammar routes SET p TO q here (q parses as an arithmeticExpression);
            // the only valid source for a pointer target is another pointer.
            if (boundTarget.Symbol.ResolvedType?.Category == CobolCategory.Pointer)
            {
                if (valueExpr is BoundIdentifierExpression srcPtr
                    && srcPtr.Symbol.ResolvedType?.Category == CobolCategory.Pointer)
                    stmts.Add(new BoundSetPointerStatement(
                        boundTarget.Symbol, PointerSetSourceKind.FromPointer, srcPtr.Symbol));
                continue;
            }
            stmts.Add(new BoundSetIndexStatement(boundTarget, SetOperation.Assign, valueExpr));
        }

        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    internal BoundStatement? BindSetIndex(CobolParserCore.SetIndexStatementContext ctx)
    {
        var op = ctx.UP() != null ? SetOperation.UpBy : SetOperation.DownBy;
        var deltaExpr = _ctx.Expression.BindArithmeticExpr(ctx.arithmeticExpression());
        if (deltaExpr == null) return null;

        var stmts = new List<BoundStatement>();
        foreach (var idCtx in ctx.dataReference())
        {
            var targetId = _ctx.Expression.BindDataReferenceWithSubscripts(idCtx);
            if (targetId is not BoundIdentifierExpression boundTarget) continue;
            // SET p UP/DOWN BY n on a POINTER is pointer arithmetic (ISO §14.9.39 Format 10): adjust the managed
            // pointer's address by n bytes, NOT the numeric index path.
            if (boundTarget.Symbol.ResolvedType?.Category == CobolCategory.Pointer)
                stmts.Add(new BoundPointerArithStatement(boundTarget.Symbol, op == SetOperation.UpBy, deltaExpr));
            else
                stmts.Add(new BoundSetIndexStatement(boundTarget, op, deltaExpr));
        }

        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    // ── INITIALIZE ──

    internal BoundStatement? BindInitialize(CobolParserCore.InitializeStatementContext ctx)
    {
        var targets = new List<DataSymbol>();
        var idList = ctx.initializeOperandList();
        if (idList == null) return null;

        foreach (var idCtx in idList.dataReference())
        {
            var sym = _ctx.Semantic.ResolveData(idCtx.cobolWord().GetText());
            if (sym != null) targets.Add(sym);
        }

        if (targets.Count == 0) return null;

        var categoryReplacements = new List<BoundInitializeCategoryReplacement>();
        var replacingPhrase = ctx.initializeReplacingPhrase();
        if (replacingPhrase != null)
        {
            foreach (var item in replacingPhrase.initializeReplacingItem())
            {
                var category = ClassifyReplacingItem(item);
                var valueExpr = BindReplacingValue(item);
                if (valueExpr != null)
                    categoryReplacements.Add(new BoundInitializeCategoryReplacement(category, valueExpr));
            }
        }

        // COBOL-2002 phrases (§14.9.20): WITH FILLER, [ALL|category] TO VALUE, THEN TO DEFAULT.
        bool withFiller = ctx.FILLER() != null;
        var toValueCtx = ctx.initializeCategoryToValue();
        bool toValue = toValueCtx != null;
        InitializeCategory? toValueCategory = toValueCtx?.initializeCategory() is { } catCtx
            ? ClassifyCategory(catCtx)   // null for "ALL TO VALUE" / bare "TO VALUE"
            : null;
        bool toDefault = ctx.initializeDefaultPhrase() != null;

        return new BoundInitializeStatement(
            targets, categoryReplacements, withFiller, toValue, toValueCategory, toDefault);
    }

    internal InitializeCategory ClassifyReplacingItem(CobolParserCore.InitializeReplacingItemContext ctx)
        => ClassifyCategory(ctx.initializeCategory());

    /// <summary>Map an INITIALIZE category-name to this FROZEN engine's InitializeCategory enum (shared by the
    /// REPLACING and TO VALUE phrases).
    /// <para>⛔ THE GRAMMAR'S category-name IS NOW A SET OF THIRTEEN WORDS (ISO §14.9.20.2's choice-indicator
    /// brace; kb/Work PB415), and this engine's bound model carries ONE category drawn from six. It is the frozen
    /// differential ORACLE, opt-in behind <c>COBOLSHARP_LEGACY_DIFFERENTIAL</c> and deleted at P15, so it is not
    /// extended to the standard's shape: a spelling it cannot represent raises here, failing that one oracle case
    /// loudly rather than silently answering for a different category than the source names.</para></summary>
    internal static InitializeCategory ClassifyCategory(CobolParserCore.InitializeCategoryContext cat)
    {
        var names = cat.initializeCategoryName();
        if (names.Length != 1)
            throw new NotSupportedException(
                "the frozen legacy engine models one category per category-name; ISO §14.9.20.2 admits one or "
                + $"more ({names.Length} specified). Greenfield-only source — see kb/Work PB415.");
        var n = names[0];
        if (n.ALPHANUMERIC_EDITED() != null) return InitializeCategory.AlphanumericEdited;
        if (n.NUMERIC_EDITED() != null) return InitializeCategory.NumericEdited;
        if (n.ALPHABETIC() != null) return InitializeCategory.Alphabetic;
        if (n.ALPHANUMERIC() != null) return InitializeCategory.Alphanumeric;
        if (n.NUMERIC() != null) return InitializeCategory.Numeric;
        if (n.BOOLEAN() != null) return InitializeCategory.Boolean;
        throw new NotSupportedException(
            $"the frozen legacy engine has no INITIALIZE category for '{n.GetText()}'. Greenfield-only source — "
            + "see kb/Work PB415.");
    }

    internal BoundExpression? BindReplacingValue(CobolParserCore.InitializeReplacingItemContext ctx)
    {
        var litCtx = ctx.literal();
        if (litCtx != null) return _ctx.Expression.BindLiteral(litCtx);

        var idCtx = ctx.dataReference();
        if (idCtx != null)
        {
            var sym = _ctx.Semantic.ResolveData(idCtx.cobolWord().GetText());
            if (sym != null) return new BoundIdentifierExpression(sym, sym.ResolvedType?.Category ?? CobolCategory.Alphanumeric);
        }

        return null;
    }

    // ── ACCEPT ──

    internal BoundStatement? BindAccept(CobolParserCore.AcceptStatementContext ctx)
    {
        var targetId = _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());
        if (targetId is not BoundIdentifierExpression boundTarget) return null;

        var sourceKind = AcceptSourceKind.None;
        var sourceCtx = ctx.acceptSource();
        if (sourceCtx != null)
        {
            if (sourceCtx.DATE() != null && sourceCtx.YYYYMMDD() != null)
                sourceKind = AcceptSourceKind.DateYYYYMMDD;
            else if (sourceCtx.DATE() != null) sourceKind = AcceptSourceKind.Date;
            else if (sourceCtx.TIME() != null) sourceKind = AcceptSourceKind.Time;
            else if (sourceCtx.DAY_OF_WEEK() != null) sourceKind = AcceptSourceKind.DayOfWeek;
            else if (sourceCtx.DAY() != null && sourceCtx.YYYYDDD() != null)
                sourceKind = AcceptSourceKind.DayYYYYDDD;
            else if (sourceCtx.DAY() != null) sourceKind = AcceptSourceKind.Day;
        }

        return new BoundAcceptStatement(boundTarget, sourceKind);
    }
}
