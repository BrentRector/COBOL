// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// String operation binding: BindInspect, BindString, BindUnstring,
/// plus INSPECT pattern/region/delimiter helpers and validation.
/// </summary>
internal sealed class StringStatementBinder
{
    private readonly BindingContext _ctx;

    internal StringStatementBinder(BindingContext ctx) => _ctx = ctx;

    private (SourceLocation loc, TextSpan span) DiagAt(int line)
        => (new SourceLocation(_ctx.SourceName, 0, line, 0), TextSpan.Empty);

    // ── INSPECT ──

    internal BoundStatement? BindInspect(CobolParserCore.InspectStatementContext ctx)
    {
        // identifier-1 became `(functionCall | dataReference)` in the SHARED grammar (PB10 — §8.4.3.1.2 Format 1
        // makes a function-identifier an identifier, admissible here in Format 1 only). The legacy engine has no
        // function-identifier operand path, so it DECLINES the new shape cleanly rather than dereferencing the
        // now-nullable accessor. Carried, not abandoned: the legacy compiler survives to the P15 cut-over, and a
        // null here would be a crash where an unsupported-statement return is the established contract.
        if (ctx.dataReference() is not { } targetRef) return null;
        var targetExpr = _ctx.Expression.BindDataReferenceWithSubscripts(targetRef);
        if (targetExpr is not BoundIdentifierExpression targetId) return null;

        var tallying = new List<BoundInspectTallyingItem>();
        var replacing = new List<BoundInspectReplacingItem>();
        BoundInspectConverting? converting = null;

        var tallyPhrase = ctx.inspectTallyingPhrase();
        if (tallyPhrase != null)
        {
            foreach (var item in tallyPhrase.inspectTallyingItem())
            {
                var counterExpr = _ctx.Expression.BindDataReferenceWithSubscripts(item.dataReference());
                if (counterExpr is not BoundIdentifierExpression counterId) continue;

                // The shared grammar nests each phrase's operands under its adjective (PB1125): ALL / LEADING are
                // transitive across the operands that follow them, so every operand inherits the phrase's kind.
                foreach (var countPhrase in item.inspectCountPhrase())
                {
                    if (countPhrase.CHARACTERS() != null)
                    {
                        tallying.Add(new BoundInspectTallyingItem(counterId, InspectTallyKind.Characters, null,
                            BindInspectDelimiters(countPhrase.inspectDelimiters())));
                        continue;
                    }
                    // LEADING stays Leading; ALL, and the FIRST / TRAILING spellings the legacy engine has no
                    // dedicated tally kind for, count as All.
                    var kind = countPhrase.LEADING() != null ? InspectTallyKind.Leading : InspectTallyKind.All;
                    foreach (var operand in countPhrase.inspectCountOperand())
                        tallying.Add(new BoundInspectTallyingItem(counterId, kind,
                            ExtractInspectPattern(operand.inspectChar()), BindInspectDelimiters(operand.inspectDelimiters())));
                }
            }
        }

        var replPhrase = ctx.inspectReplacingPhrase();
        if (replPhrase != null)
        {
            foreach (var item in replPhrase.inspectReplacingItem())
            {
                if (item.CHARACTERS() != null)
                {
                    var characterReplacement = ExtractInspectPattern(item.inspectChar())
                        ?? InspectPatternValue.FromLiteral("");
                    replacing.Add(new BoundInspectReplacingItem(InspectReplaceKind.Characters,
                        InspectPatternValue.FromLiteral(""), characterReplacement,
                        BindInspectDelimiters(item.inspectDelimiters())));
                    continue;
                }

                // The shared grammar nests the pairs under their adjective (PB1125); each inherits its item's kind.
                var kind = item.FIRST() != null ? InspectReplaceKind.First
                    : item.LEADING() != null ? InspectReplaceKind.Leading
                    : InspectReplaceKind.All;
                foreach (var pair in item.inspectReplacingPair())
                {
                    var inspChars = pair.inspectChar();
                    var pattern = inspChars.Length > 0
                        ? ExtractInspectPattern(inspChars[0]) ?? InspectPatternValue.FromLiteral("")
                        : InspectPatternValue.FromLiteral("");
                    var replacement = inspChars.Length > 1
                        ? ExtractInspectPattern(inspChars[1]) ?? InspectPatternValue.FromLiteral("")
                        : InspectPatternValue.FromLiteral("");
                    replacing.Add(new BoundInspectReplacingItem(kind, pattern, replacement,
                        BindInspectDelimiters(pair.inspectDelimiters())));
                }
            }
        }

        var convPhrase = ctx.inspectConvertingPhrase();
        if (convPhrase != null)
        {
            var inspChars = convPhrase.inspectChar();
            var fromSet = inspChars.Length > 0
                ? ExtractInspectPattern(inspChars[0]) ?? InspectPatternValue.FromLiteral("")
                : InspectPatternValue.FromLiteral("");
            var toSet = inspChars.Length > 1
                ? ExtractInspectPattern(inspChars[1]) ?? InspectPatternValue.FromLiteral("")
                : InspectPatternValue.FromLiteral("");
            // CONVERTING's after-before-phrase is the same inspectDelimiters rule as the other two formats (PB1125).
            var region = BindInspectDelimiters(convPhrase.inspectDelimiters());
            converting = new BoundInspectConverting(fromSet, toSet, region);
        }

        var inspectStmt = new BoundInspectStatement(
            targetId, tallying, replacing, converting, backward: ctx.BACKWARD() != null);
        ValidateInspectStatement(inspectStmt, ctx.Start?.Line ?? 0);
        return inspectStmt;
    }

    internal InspectPatternValue? ExtractInspectPattern(CobolParserCore.InspectCharContext? ctx)
    {
        if (ctx == null) return null;
        if (ctx.literal() != null)
            return InspectPatternValue.FromLiteral(ExtractLiteralString(ctx.literal()));
        if (ctx.dataReference() != null)
        {
            var bound = _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());
            if (bound is BoundIdentifierExpression idExpr)
                return InspectPatternValue.FromDataRef(idExpr);
            // Fallback: unresolved identifier → use name as literal
            return InspectPatternValue.FromLiteral(ctx.dataReference().cobolWord().GetText());
        }
        if (ctx.figurativeConstant() != null)
        {
            if (ctx.figurativeConstant().spaceWord() != null) return InspectPatternValue.FromLiteral(" ");
            if (ctx.figurativeConstant().zeroWord() != null) return InspectPatternValue.FromLiteral("0");
            if (ctx.figurativeConstant().highValueWord() != null) return InspectPatternValue.FromLiteral("\xFF");
            if (ctx.figurativeConstant().lowValueWord() != null) return InspectPatternValue.FromLiteral("\x00");
            if (ctx.figurativeConstant().quoteWord() != null) return InspectPatternValue.FromLiteral("\"");
            return InspectPatternValue.FromLiteral(ctx.figurativeConstant().GetText());
        }
        return null;
    }

    internal string ExtractStringValue(
        CobolParserCore.DataReferenceContext[]? ids,
        CobolParserCore.LiteralContext[]? lits)
    {
        // Return the first available literal or identifier text
        if (lits != null && lits.Length > 0) return ExtractLiteralString(lits[0]);
        if (ids != null && ids.Length > 0) return ids[0].GetText();
        return "";
    }

    internal string ExtractNthStringValue(
        CobolParserCore.DataReferenceContext[]? ids,
        CobolParserCore.LiteralContext[]? lits,
        int n)
    {
        // Combine identifiers and literals in parse order, pick nth
        // For simplicity: literals first, then identifiers
        var all = new List<string>();
        if (ids != null) foreach (var id in ids) all.Add(id.GetText());
        if (lits != null) foreach (var lit in lits) all.Add(ExtractLiteralString(lit));

        // Actually need to respect parse order. Use child index ordering.
        // Simpler approach: just use the grammar structure.
        // For REPLACING: first id/lit pair is pattern, second is replacement
        // The grammar puts them as separate children: ALL <id|lit> BY <id|lit>
        // So we need ordered extraction.
        var ordered = new List<(int index, string value)>();
        if (ids != null)
            foreach (var id in ids)
                ordered.Add((id.SourceInterval.a, id.GetText()));
        if (lits != null)
            foreach (var lit in lits)
                ordered.Add((lit.SourceInterval.a, ExtractLiteralString(lit)));
        ordered.Sort((a, b) => a.index.CompareTo(b.index));

        return n < ordered.Count ? ordered[n].value : "";
    }

    internal string ExtractLiteralString(CobolParserCore.LiteralContext lit)
    {
        var nonNum = lit.nonNumericLiteral();
        if (nonNum != null)
        {
            // Handle figurative constants (SPACE, ZERO, etc.) inside nonNumericLiteral
            var fig = nonNum.figurativeConstant();
            if (fig != null)
            {
                if (fig.spaceWord() != null) return " ";
                if (fig.zeroWord() != null) return "0";
                if (fig.highValueWord() != null) return "\xFF";
                if (fig.lowValueWord() != null) return "\x00";
                if (fig.quoteWord() != null) return "\"";
                // ALL "literal" — extract the literal string
                if (fig.allLiteral()?.STRINGLIT() is { } allSl)
                {
                    string raw = allSl.GetText();
                    if (raw.Length >= 2) return raw[1..^1];
                }
                return fig.GetText();
            }

            string text = nonNum.GetText();
            if (text.Length >= 2 &&
                ((text[0] == '"' && text[^1] == '"') ||
                 (text[0] == '\'' && text[^1] == '\'')))
            {
                char q = text[0];
                return text[1..^1].Replace(new string(q, 2), new string(q, 1));
            }
            return text;
        }
        return lit.GetText();
    }

    internal BoundInspectRegion BindInspectDelimiters(CobolParserCore.InspectDelimitersContext? ctx)
    {
        if (ctx == null) return BoundInspectRegion.Empty;

        InspectPatternValue? beforePattern = null;
        bool beforeInitial = false;
        InspectPatternValue? afterPattern = null;
        bool afterInitial = false;

        // Grammar: BEFORE INITIAL? inspectChar (AFTER INITIAL? inspectChar)?
        //        | AFTER INITIAL? inspectChar (BEFORE INITIAL? inspectChar)?
        var chars = ctx.inspectChar();
        var initials = ctx.INITIAL_();

        if (ctx.BEFORE() != null && ctx.AFTER() != null)
        {
            // Both present — first matches the leading keyword
            if (ctx.BEFORE().Symbol.TokenIndex < ctx.AFTER().Symbol.TokenIndex)
            {
                beforePattern = chars.Length > 0 ? ExtractInspectPattern(chars[0]) : null;
                beforeInitial = initials.Length > 0;
                afterPattern = chars.Length > 1 ? ExtractInspectPattern(chars[1]) : null;
                afterInitial = initials.Length > 1;
            }
            else
            {
                afterPattern = chars.Length > 0 ? ExtractInspectPattern(chars[0]) : null;
                afterInitial = initials.Length > 0;
                beforePattern = chars.Length > 1 ? ExtractInspectPattern(chars[1]) : null;
                beforeInitial = initials.Length > 1;
            }
        }
        else if (ctx.BEFORE() != null)
        {
            beforePattern = chars.Length > 0 ? ExtractInspectPattern(chars[0]) : null;
            beforeInitial = initials.Length > 0;
        }
        else if (ctx.AFTER() != null)
        {
            afterPattern = chars.Length > 0 ? ExtractInspectPattern(chars[0]) : null;
            afterInitial = initials.Length > 0;
        }

        return new BoundInspectRegion(beforePattern, beforeInitial, afterPattern, afterInitial);
    }

    // ── STRING ──

    internal BoundStatement? BindString(CobolParserCore.StringStatementContext ctx)
    {
        var sendings = new List<BoundStringSending>();
        foreach (var phrase in ctx.stringSendingPhrase())
        {
            // Grammar: strUnstrOperand delimitedByPhrase?  — the operand alternatives now live in ONE named rule
            // (strUnstrOperand -> strUnstrSender | literal | figurativeConstant), so reach them through it. A
            // function-identifier sender (DA4) is greenfield-only: it lands on none of these arms and skips, which
            // is this binder's existing posture for any shape it does not implement. Legacy survives only as the
            // P15 differential oracle.
            var sendOp = phrase.strUnstrOperand();
            BoundExpression value;
            if (sendOp?.strUnstrSender()?.dataReference() is { } valId)
                value = _ctx.Expression.BindDataReferenceWithSubscripts(valId);
            else if (sendOp?.literal() is { } valLit)
                value = _ctx.Expression.BindLiteral(valLit);
            else if (sendOp?.figurativeConstant() is { } valFig)
                value = _ctx.Expression.BindFigurativeConstantExpression(valFig);
            else
                continue;

            BoundExpression? delimiter = null;
            bool delimitedBySize = false;

            if (phrase.delimitedByPhrase() is { } delim)
            {
                if (delim.SIZE() != null)
                {
                    delimitedBySize = true;
                }
                else if (delim.strUnstrOperand()?.strUnstrSender()?.dataReference() is { } delimId)
                {
                    delimiter = _ctx.Expression.BindDataReferenceWithSubscripts(delimId);
                }
                else if (delim.strUnstrOperand()?.literal() is { } delimLit)
                {
                    delimiter = _ctx.Expression.BindLiteral(delimLit);
                }
                else if (delim.strUnstrOperand()?.figurativeConstant() is { } delimFig)
                {
                    delimiter = _ctx.Expression.BindFigurativeConstantExpression(delimFig);
                }
            }

            sendings.Add(new BoundStringSending(value, delimiter, delimitedBySize));
        }

        // Propagate delimiters: items without a delimiter inherit from the next item
        for (int i = sendings.Count - 2; i >= 0; i--)
        {
            if (sendings[i].Delimiter == null && !sendings[i].DelimitedBySize)
            {
                var next = sendings[i + 1];
                sendings[i] = new BoundStringSending(sendings[i].Value, next.Delimiter, next.DelimitedBySize);
            }
        }

        // INTO
        var intoPhrase = ctx.stringIntoPhrase();
        if (intoPhrase == null) return null;
        var intoExpr = _ctx.Expression.BindDataReferenceWithSubscripts(intoPhrase.dataReference());

        // POINTER
        BoundExpression? pointer = null;
        if (ctx.stringWithPointer() is { } ptrCtx)
            pointer = _ctx.Expression.BindDataReferenceWithSubscripts(ptrCtx.dataReference());

        // ON OVERFLOW / NOT ON OVERFLOW
        var onOverflow = new List<BoundStatement>();
        var notOnOverflow = new List<BoundStatement>();
        if (ctx.stringOnOverflow() is { } ovCtx)
        {
            var impStmts = ovCtx.statementBlock();
            // Since 2026-07-19 the NOT-LED arm may carry TWO blocks (`NOT ON OVERFLOW b1 (ON OVERFLOW b2)?`)
            // — ISO 5.2.6.4's choice indicators permit both phrases in either order. The old
            // `NOT() != null && Length == 1` test cannot distinguish which phrase came first (a forward-order
            // pair also has a NOT token), so the LEADING TOKEN is the discriminator.
            bool notFirst = ovCtx.Start?.Type == CobolParserCore.NOT;
            var first = notFirst ? notOnOverflow : onOverflow;
            var second = notFirst ? onOverflow : notOnOverflow;
            if (impStmts.Length >= 1)
            {
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) first.Add(bound);
                }
            }
            if (impStmts.Length >= 2)
            {
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) second.Add(bound);
                }
            }
        }

        var stringStmt = new BoundStringStatement(sendings, intoExpr, pointer, onOverflow, notOnOverflow);
        ValidateStringStatement(stringStmt, ctx.Start?.Line ?? 0);
        return stringStmt;
    }

    // ── UNSTRING ──

    internal BoundStatement? BindUnstring(CobolParserCore.UnstringStatementContext ctx)
    {
        // Source identifier — reached through the named strUnstrSender rule (functionCall | dataReference). A
        // function-identifier source is greenfield-only (DA4); legacy binds the data-reference arm as before and
        // survives only as the P15 differential oracle.
        if (ctx.strUnstrSender()?.dataReference() is not { } srcRef) return null;
        var sourceExpr = _ctx.Expression.BindDataReferenceWithSubscripts(srcRef);

        // DELIMITED BY phrase (optional) — supports OR-separated delimiters
        var delimiterItems = new List<(BoundExpression Expr, bool IsAll)>();
        if (ctx.unstringDelimiterPhrase() is { } delimCtx)
        {
            foreach (var item in delimCtx.unstringDelimiterItem())
            {
                bool itemAll = item.ALL() != null;
                BoundExpression itemExpr;
                var delimOp = item.strUnstrOperand();
                if (delimOp?.strUnstrSender()?.dataReference() is { } delimId)
                    itemExpr = _ctx.Expression.BindDataReferenceWithSubscripts(delimId);
                else if (delimOp?.literal() is { } delimLit)
                    itemExpr = _ctx.Expression.BindLiteral(delimLit);
                else if (delimOp?.figurativeConstant() is { } delimFig)
                    itemExpr = _ctx.Expression.BindFigurativeConstantExpression(delimFig);
                else
                    continue;   // a shape legacy does not implement (e.g. DA4's function-identifier) — skip, as before
                delimiterItems.Add((itemExpr, itemAll));
            }
        }
        var delimiters = new List<BoundUnstringDelimiter>();
        foreach (var d in delimiterItems)
            delimiters.Add(new BoundUnstringDelimiter(d.Expr, d.IsAll));

        // ONE INTO phrase, one or more receivers (ISO §14.9.48.2; kb/Work PB1183)
        var intos = new List<BoundUnstringInto>();
        foreach (var target in ctx.unstringIntoPhrase().unstringIntoTarget())
        {
            var identifiers = target.dataReference();
            int idIdx = 0;

            // First identifier is the INTO target
            var targetExpr = _ctx.Expression.BindDataReferenceWithSubscripts(identifiers[idIdx++]);

            // DELIMITER IN (optional)
            BoundExpression? delimiterIn = null;
            if (target.DELIMITER() != null && idIdx < identifiers.Length)
                delimiterIn = _ctx.Expression.BindDataReferenceWithSubscripts(identifiers[idIdx++]);

            // COUNT IN (optional)
            BoundExpression? countIn = null;
            if (target.COUNT() != null && idIdx < identifiers.Length)
                countIn = _ctx.Expression.BindDataReferenceWithSubscripts(identifiers[idIdx++]);

            intos.Add(new BoundUnstringInto(targetExpr, countIn, delimiterIn));
        }

        // WITH POINTER (optional)
        BoundExpression? pointer = null;
        if (ctx.unstringWithPointer() is { } ptrCtx)
            pointer = _ctx.Expression.BindDataReferenceWithSubscripts(ptrCtx.dataReference());

        // TALLYING IN (optional)
        BoundExpression? tallying = null;
        if (ctx.unstringTallying() is { } tallyCtx)
            tallying = _ctx.Expression.BindDataReferenceWithSubscripts(tallyCtx.dataReference());

        // ON OVERFLOW / NOT ON OVERFLOW
        var onOverflow = new List<BoundStatement>();
        var notOnOverflow = new List<BoundStatement>();
        if (ctx.unstringOnOverflow() is { } ovCtx2)
        {
            var impStmts2 = ovCtx2.statementBlock();
            // Same leading-token discriminator as STRING above (ISO 5.2.6.4 — either order, since 2026-07-19).
            bool notFirst2 = ovCtx2.Start?.Type == CobolParserCore.NOT;
            {
                var first2 = notFirst2 ? notOnOverflow : onOverflow;
                var second2 = notFirst2 ? onOverflow : notOnOverflow;
                if (impStmts2.Length >= 1)
                {
                    foreach (var stmt in impStmts2[0].statement())
                    {
                        var bound = _ctx.BindStatement(stmt);
                        if (bound != null) first2.Add(bound);
                    }
                }
                if (impStmts2.Length >= 2)
                {
                    foreach (var stmt in impStmts2[1].statement())
                    {
                        var bound = _ctx.BindStatement(stmt);
                        if (bound != null) second2.Add(bound);
                    }
                }
            }
        }

        var unstringStmt = new BoundUnstringStatement(sourceExpr, delimiters,
            intos, pointer, tallying, onOverflow, notOnOverflow);
        ValidateUnstringStatement(unstringStmt, ctx.Start?.Line ?? 0);
        return unstringStmt;
    }

    // ═══════════════════════════════════
    // Statement-level semantic validation
    // ═══════════════════════════════════

    internal void ValidateStringStatement(BoundStringStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // INTO must be alphanumeric or group
        if (stmt.Into is BoundIdentifierExpression intoId && !intoId.Symbol.IsGroup
            && intoId.Category != CobolCategory.Alphanumeric
            && intoId.Category != CobolCategory.AlphanumericEdited)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1301, loc, span);
        // POINTER must be integer numeric
        if (stmt.Pointer is BoundIdentifierExpression ptrId
            && !CategoryCompatibility.IsNumericFamily(ptrId.Category))
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1304, loc, span);
    }

    internal void ValidateUnstringStatement(BoundUnstringStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Source must be alphanumeric or group
        if (stmt.Source is BoundIdentifierExpression srcId && !srcId.Symbol.IsGroup
            && srcId.Category != CobolCategory.Alphanumeric
            && srcId.Category != CobolCategory.AlphanumericEdited)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1401, loc, span);
        // POINTER must be integer numeric
        if (stmt.Pointer is BoundIdentifierExpression ptrId
            && !CategoryCompatibility.IsNumericFamily(ptrId.Category))
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1405, loc, span);
        // TALLYING must be integer numeric
        if (stmt.Tallying is BoundIdentifierExpression tallyId
            && !CategoryCompatibility.IsNumericFamily(tallyId.Category))
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1406, loc, span);
    }

    internal void ValidateInspectStatement(BoundInspectStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Target must be alphanumeric, numeric-edited, or group
        // COBOL-85 §14.9.21: INSPECT operates on the display representation
        if (stmt.Target.Symbol.IsElementary
            && stmt.Target.Category != CobolCategory.Alphanumeric
            && stmt.Target.Category != CobolCategory.AlphanumericEdited
            && stmt.Target.Category != CobolCategory.Numeric
            && stmt.Target.Category != CobolCategory.NumericEdited)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1501, loc, span);
        // TALLYING counters must be integer numeric
        foreach (var item in stmt.Tallying)
        {
            if (!CategoryCompatibility.IsNumericFamily(item.Counter.Category))
                _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1502, loc, span);
        }
    }
}
