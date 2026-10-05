// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>⛔ THE SENDING OPERAND OF A <c>SET … TO</c> (ISO §14.9.39.2), AS WHAT IT IS — an identifier — and not as
/// "a bare data reference or nothing" (kb/Work PB1399 + PB1929).
/// <para>Every sending brace of §14.9.39.2's To-formats names an <i>identifier</i> (identifier-2, -4, -6, -8, -13),
/// and §8.4.3.1.3 SR1 makes that "any of the formats for an identifier". Three of those formats are not a data-name
/// reference: a function-identifier (§8.4.3.1.2 Format 1),
/// an inline method invocation (§8.4.3.1.2 Format 4) and the keyword-omitted function (§8.4.3.2.3 SR2, which the
/// grammar parses as a subscripted data reference). §8.4.3.2.1
/// ("a function-identifier references the unique data item that results from the evaluation of a function") and
/// §8.4.3.4.4 GR1 ("an inline method invocation references a temporary data item with the same class, category, and
/// content as the temp-identifier") make each of them a reference to a DATA ITEM, so the sender's class and category
/// are properties of that item and every format's sending rule asks them of it. The former classifier —
/// <c>OoBinder.OoExtractBareReference</c> — answered null for all three, so every format saw "no identifier" and
/// refused the sender as a literal or an arithmetic expression.</para>
/// <para>ONE KIND OF SENDER ARRIVES, three shapes: <see cref="Ref"/> (a data reference, still to be resolved by the
/// format that takes it), <see cref="Identifier"/> (a function-identifier or inline invocation, BOUND ONCE here — its
/// activation registers a statement pre-op, so binding it a second time would run a user function twice), or neither
/// (a literal or an arithmetic expression, which each format's own syntax rule refuses).</para></summary>
internal sealed record SetSender(string Text, Core.DataReferenceContext? Ref, BoundExpr? Bound, ParserRuleContext? Syntax)
{
    /// <summary>The sender is an identifier of some format — a data reference, a function-identifier or an inline
    /// method invocation — rather than a literal or an arithmetic expression.</summary>
    public bool IsIdentifierOperand => Ref is not null || Bound is not null;

    /// <summary>The bound operand of a function-identifier / invocation sender (null for a <see cref="Ref"/> or a
    /// non-identifier sender). <see cref="IntrinsicBinder.OperandOf"/> is the ONE expression→operand mapping: a user
    /// function's or invocation's result temporary is a <see cref="BoundFieldOperand"/> over its own data item.</summary>
    public BoundOperand? Operand => Bound is { } b ? IntrinsicBinder.OperandOf(b) : null;

    /// <summary>The temporary data item a user function or inline invocation RETURNS — the item whose class and
    /// category §8.4.3.2.1 / §8.4.3.4.4 GR1 give the identifier. Null for an intrinsic function (a computed value
    /// with no item of its own) and for every non-identifier sender.</summary>
    public Place? TemporaryItem => Bound is { } b ? IntrinsicBinder.TemporaryItemOf(b) : null;

    /// <summary>The §8.5.2.1 CLASS of a function-identifier / invocation sender (null when not statically decidable,
    /// and for a <see cref="Ref"/>, whose class the resolved item answers). An INDEX function (§15.2 item 6) is class
    /// index here — the storage model folds it into category numeric, which is why the class, never the category,
    /// answers (<see cref="IntrinsicArgumentRules.ClassOf"/>).</summary>
    public CobolClass? IdentifierClass => Operand is { } o ? IntrinsicArgumentRules.ClassOf(o) : null;

    /// <summary>A sender that is not an identifier at all: a literal or an arithmetic expression.</summary>
    public static SetSender OfExpression(string text) => new(text, null, null, null);

    /// <summary>A bare data reference sender, still to be resolved by the format that takes it.</summary>
    public static SetSender OfReference(Core.DataReferenceContext dref) => new(dref.GetText(), dref, null, null);
}

/// <summary>⛔ THE ONE CLASSIFIER OF A SET SENDER (kb/Work PB1399 + PB1929 — "fix both through one sender
/// classifier"). It reads the PARSE shape of the sending <c>arithmeticExpression</c> once, and binds a sole
/// function-identifier / inline invocation / keyword-omitted function once, so the format that takes the sender
/// asks its own syntax rule of a <see cref="SetSender"/> and never re-reads the tree.
/// <para>The reading is the relation operand's (<c>ConditionBinder.ComparisonOperandOf</c>): a SOLE identifier is
/// short-circuited BEFORE the expression spine, because the spine's §8.8.1.1 numeric-class screen would otherwise
/// refuse every non-numeric identifier at a position that is not arithmetic at all. A PARENTHESIZED function is
/// deliberately not a sole identifier — "an arithmetic expression enclosed in parentheses" (§8.8.1.1) — and keeps
/// the arithmetic screen.</para></summary>
internal sealed class SetSenders(StatementBinder host)
{
    /// <summary>Classify the sending operand of <c>SET … TO <paramref name="amount"/></c>. A refused identifier (one
    /// whose own binder reported) comes back with <see cref="SetSender.Bound"/> an error expression, which the caller
    /// stops on through <see cref="IsRefused"/>.</summary>
    public SetSender Classify(Core.ArithmeticExpressionContext amount)
    {
        string text = amount.GetText();
        if (ConditionBinder.SoleFunctionCall(amount) is { } fc)
            return Identified(fc, host.Intrinsic.BindIntrinsic(fc));
        if (ConditionBinder.SoleInlineInvocation(amount) is { } imi)
            return Identified(imi, host.Oo.OoBindInlineInvocation(imi));
        if (OoBinder.OoExtractBareReference(amount) is not { } dref) return SetSender.OfExpression(text);
        // §8.4.3.2.3 SR2 — a REPOSITORY function name written without the word FUNCTION parses as a subscripted
        // data reference; the binder's ONE detector tells the two apart (a declared data item wins).
        return host.Intrinsic.KeywordOmittedFunction(dref) is { } kof
            ? Identified(dref, kof)
            : SetSender.OfReference(dref);

        // The identifier's own text is spelled as written ("FUNCTION MAX (IA IB)"), never run together.
        SetSender Identified(ParserRuleContext syntax, BoundExpr bound) =>
            new(DataBinder.WrittenText(amount), null, bound, syntax);
    }

    /// <summary>True when the identifier's own binder already reported — the statement stops there, with no second
    /// diagnostic about a sender nobody could read (the R30 posture).</summary>
    public static bool IsRefused(SetSender sender) => sender.Bound is BoundExprError;
}
