// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// CALL/ENTRY binding: BindCall, BindCancel, BindEntry.
/// </summary>
internal sealed class CallBinder
{
    private readonly BindingContext _ctx;

    internal CallBinder(BindingContext ctx) => _ctx = ctx;

    // ── INVOKE (OO COBOL, ISO §14.9.23) ──

    /// <summary>
    /// Bind <c>INVOKE {class-name|object} "method" [RETURNING obj]</c> (slice 1). <c>"NEW"</c> on a class-name
    /// target is the built-in factory (construct an instance); any other method on an object reference is an
    /// instance call. The target is syntactically a <c>dataReference</c>; NEW vs instance is decided by the
    /// method name + whether the target resolves to an object-reference data item. USING args / value RETURNING
    /// are later slices.
    /// </summary>
    internal BoundStatement? BindInvoke(CobolParserCore.InvokeStatementContext ctx)
    {
        var objRef = ctx.invokeTarget()?.objectReference();
        // INVOKE SELF "m" — a virtual call to a sibling method on `this` (ISO §8.4.3.8); INVOKE SUPER "m" — a
        // non-virtual call to the base class's method (the override-calls-base pattern). Both have no data-reference
        // target (the receiver is `this`); the binder marks them so the emitter pushes ldarg.0 as the receiver.
        bool isSelf = objRef?.SELF() != null;
        bool isSuper = objRef?.SUPER() != null;
        string targetText = "";
        if (!isSuper && !isSelf)
        {
            var targetRef = objRef?.dataReference();
            if (targetRef == null) return null; // NULL target / other forms — later slices
            targetText = targetRef.cobolWord().GetText();
        }

        // Method name: a literal "NEW"/"method" (slice 1; a data-name method selector is a later slice).
        var methodLit = ctx.invokeMethodName()?.literal();
        if (methodLit == null) return null;
        string methodName = methodLit.GetText().Trim('"', '\'');

        // RETURNING obj — for NEW the object-reference item that receives the instance; for an instance call the
        // data item that receives the method's RETURNING value (slice 2).
        DataSymbol? returning = ctx.invokeReturning()?.dataReference() is { } retRef
            ? _ctx.Semantic.ResolveData(retRef.cobolWord().GetText())
            : null;

        if (!isSuper && !isSelf && string.Equals(methodName, "NEW", StringComparison.OrdinalIgnoreCase))
            // INVOKE class-name "NEW" RETURNING obj — construct an instance of the target class.
            return new BoundInvokeStatement(isNew: true, className: targetText, targetObject: null,
                targetClassName: null, methodName: methodName, args: System.Array.Empty<BoundExpression>(),
                returning: returning);

        // INVOKE obj "method" USING … RETURNING … — instance invocation; the receiver's declared class drives
        // dispatch. Slice 2 supports BY REFERENCE data-reference arguments (the COBOL default). The other
        // grammar-legal forms (literal, BY VALUE arithmetic-expression, BY CONTENT) need a synthesized value
        // location (a later OO slice); they are REJECTED LOUDLY here (COBOL0111) rather than silently dropped —
        // a dropped arg would shift the trailing RETURNING slot and miscompile. (Adversarial review, DEVLOG 448.)
        var args = new List<BoundExpression>();
        if (ctx.invokeUsing() is { } using_)
            foreach (var argCtx in using_.invokeArgument())
            {
                var invokeRef = argCtx.dataReference() ?? CobolNet.Frontend.Expressions.SoleOperand.DataRef(argCtx.arithmeticExpression());
                bool supported = invokeRef != null
                    && argCtx.CONTENT() == null && argCtx.VALUE() == null;
                if (!supported)
                {
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0111,
                        new SourceLocation(_ctx.SourceName, 0, argCtx.Start.Line, argCtx.Start.Column),
                        TextSpan.Empty);
                    continue;
                }
                if (_ctx.Expression.BindDataReferenceWithSubscripts(invokeRef!) is { } boundArg)
                    args.Add(boundArg);
            }

        if (isSelf)
            // INVOKE SELF "m" — virtual call to a sibling method on `this` (resolved against the enclosing class +
            // its INHERITS chain at emit time; dispatched callvirt so an override in a subclass wins).
            return new BoundInvokeStatement(isNew: false, className: null, targetObject: null,
                targetClassName: null, methodName: methodName, args: args, returning: returning, isSelf: true);

        if (isSuper)
            // INVOKE SUPER "m" — non-virtual call to the base class's method (receiver is `this`; resolved + emitted
            // against the enclosing class's base at emit time).
            return new BoundInvokeStatement(isNew: false, className: null, targetObject: null,
                targetClassName: null, methodName: methodName, args: args, returning: returning, isSuper: true);

        var targetObject = _ctx.Semantic.ResolveData(targetText);
        return new BoundInvokeStatement(isNew: false, className: null, targetObject: targetObject,
            targetClassName: targetObject?.ObjectClassName, methodName: methodName, args: args,
            returning: returning);
    }

    // ── CALL ──

    internal BoundStatement? BindCall(CobolParserCore.CallStatementContext ctx)
    {
        var targetCtx = ctx.callTarget();
        if (targetCtx == null) return null;

        // Extract target name: literal or data reference
        string targetName;
        bool isDynamic;
        if (targetCtx.literal() is { } litCtx)
        {
            // CALL "LITERAL" — static call (program name known at compile time)
            targetName = litCtx.GetText().Trim('"', '\'');
            isDynamic = false;
        }
        else if (targetCtx.dataReference() is { } dataRefCtx)
        {
            // CALL identifier — dynamic call (program name computed at runtime)
            targetName = dataRefCtx.cobolWord().GetText();
            isDynamic = true;
        }
        else
        {
            return null;
        }

        // USING arguments. The BY REFERENCE / BY CONTENT / BY VALUE phrase is transitive: it applies
        // to every argument that follows it until another such phrase is encountered (ISO §14.8 CALL,
        // general rule 5). Before any phrase the default is BY REFERENCE. The grammar attaches a
        // phrase only to its first data-name, so bare arguments inherit the most recent explicit mode.
        var arguments = new List<BoundCallArgument>();
        if (ctx.callUsingPhrase() is { } usingCtx)
        {
            // kb/Work PB239: an address-identifier argument (§14.9.4.3 SR3/SR4) is a greenfield-only construct —
            // this differential oracle never modelled it, so the statement falls through to the unsupported
            // path exactly as any other post-oracle construct does (never a null operand bound as an argument).
            if (usingCtx.callArgument().Any(a => a.addressIdentifier() != null
                    || a.callByReference()?.addressIdentifier() != null
                    || a.callByContent()?.addressIdentifier() != null
                    || a.callByValue()?.addressIdentifier() != null))
                return null;
            var currentMode = ParameterMode.ByReference;
            foreach (var argCtx in usingCtx.callArgument())
            {
                if (argCtx.callByReference() is { } byRef)
                {
                    currentMode = ParameterMode.ByReference;
                    var expr = _ctx.Expression.BindDataReferenceWithSubscripts(byRef.dataReference());
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(currentMode, expr));
                }
                else if (argCtx.callByContent() is { } byContent)
                {
                    currentMode = ParameterMode.ByContent;
                    BoundExpression? expr = null;
                    if (CobolNet.Frontend.Expressions.SoleOperand.DataRef(byContent.arithmeticExpression()) is { } dr)
                        expr = _ctx.Expression.BindDataReferenceWithSubscripts(dr);
                    else if (byContent.literal() is { } lit)
                        expr = _ctx.Expression.BindLiteral(lit);
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(currentMode, expr));
                }
                else if (argCtx.callByValue() is { } byValue)
                {
                    currentMode = ParameterMode.ByValue;
                    // kb/Work PB238: `callByValue` gained the `literal` arm §14.9.4.2 Format 2 prints as
                    // literal-2, so `arithmeticExpression()` is now nullable here. The legacy oracle binds the
                    // new spelling through the same literal entry point its BY CONTENT arm above uses.
                    BoundExpression? expr =
                        byValue.arithmeticExpression() is { } vax
                            ? _ctx.Expression.BindAdditiveExpression(vax.additiveExpression())
                        : byValue.literal() is { } vlit ? _ctx.Expression.BindLiteral(vlit)
                        : null;
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(currentMode, expr));
                }
                else if (CobolNet.Frontend.Expressions.SoleOperand.DataRef(argCtx.arithmeticExpression()) is { } bareRef)
                {
                    // Bare argument: inherit the most recent explicit mode (transitive); default BY REFERENCE.
                    var expr = _ctx.Expression.BindDataReferenceWithSubscripts(bareRef);
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(currentMode, expr));
                }
            }
        }

        // RETURNING
        BoundIdentifierExpression? returningTarget = null;
        if (ctx.callReturningPhrase() is { } retCtx)
        {
            var retExpr = _ctx.Expression.BindDataReferenceWithSubscripts(retCtx.dataReference());
            returningTarget = retExpr as BoundIdentifierExpression;
        }

        // ON EXCEPTION / NOT ON EXCEPTION (independently optional per spec)
        var onException = new List<BoundStatement>();
        var notOnException = new List<BoundStatement>();
        var excPhrases = ctx.callExceptionPhrases();
        if (excPhrases?.callOnExceptionPhrase() is { } excCtx)
        {
            foreach (var stmt in excCtx.statementBlock().statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) onException.Add(bound);
            }
        }
        if (excPhrases?.callNotOnExceptionPhrase() is { } notExcCtx)
        {
            foreach (var stmt in notExcCtx.statementBlock().statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) notOnException.Add(bound);
            }
        }

        return new BoundCallStatement(targetName, isDynamic, arguments, returningTarget,
            onException, notOnException);
    }

    // ── CANCEL ──

    internal BoundCancelStatement BindCancel(CobolParserCore.CancelStatementContext ctx)
    {
        var targets = new List<BoundCancelTarget>();
        foreach (var target in ctx.cancelTarget())
        {
            if (target.literal() is { } lit)
                // CANCEL "literal" — the program-name is the literal value (static).
                targets.Add(new BoundCancelTarget(lit.GetText().Trim('"', '\''), IsDynamic: false));
            else if (target.dataReference() is { } dr)
                // CANCEL identifier — the program-name is the data item's runtime content (dynamic).
                targets.Add(new BoundCancelTarget(dr.cobolWord().GetText(), IsDynamic: true));
        }
        return new BoundCancelStatement(targets);
    }

    // ── ENTRY ──

    internal BoundEntryStatement? BindEntry(CobolParserCore.EntryStatementContext ctx)
    {
        string entryName = ctx.literal().GetText().Trim('"', '\'');

        var usingNames = new List<string>();
        if (ctx.usingClause() is { } usingCtx)
        {
            // Per-parameter since the greenfield P10 Step-10 BY VALUE header grammar; ENTRY formals are
            // BY REFERENCE in this legacy oracle (its corpus predates BY VALUE headers).
            foreach (var prm in usingCtx.usingParameter())
            {
                var dr = prm.usingByReference()?.dataReference()
                    ?? prm.usingByValue()?.dataReference()
                    ?? prm.dataReference();
                usingNames.Add(dr.cobolWord().GetText());
            }
        }

        return new BoundEntryStatement(entryName, usingNames);
    }
}
