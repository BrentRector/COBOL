// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE ONE ROLE CLASSIFICATION OF THE ARITHMETIC VERBS' MIXED OPERAND RULES (kb/Work PB1142).
/// <para>
/// ISO §14.9.2.2 / §14.9.44.2 / §14.9.26.2 / §14.9.12.2 print the TO / FROM / BY / INTO operand TWICE: Format 1
/// writes <c>{identifier-2 [rounded-phrase]}</c> — the RECEIVER, "stored as the new value of the data item
/// referenced by identifier-2" (§14.9.26.4 GR1) — and Format 2 writes ONE <c>{identifier-2 | literal-2}</c> in
/// a SENDING role. The grammar parses the union in two rules (kb/Work PB134, PB2114): <c>receiversOrSendingOperand</c>,
/// the TO / FROM / INTO operand of ADD, SUBTRACT and DIVIDE, and <c>multiplyByOperand</c>, MULTIPLY's BY operand.
/// Each carries a receiver alternative beside the sending-only ones (a literal, §8.4.3.1.2 Format 1's
/// function-identifier, Format 4's inline method invocation — kb/Work PB428), and the binder narrows it back per
/// format.
/// </para>
/// <para>
/// ⛔ WHY THE RECEIVER IS DEFINED POSITIVELY. The per-verb screens used to enumerate the NON-receivers by hand —
/// <c>literal() || functionCall()</c> — and PB428 then added <c>inlineMethodInvocation</c> to all four rules
/// without adding it to the four lists: an inline invocation written as a Format-1 receiver was silently DROPPED
/// by ADD / SUBTRACT / MULTIPLY (the statement bound with no receiver for it) and crashed DIVIDE's emitter on an
/// empty receiver list. That is the two-arm shape exactly: the function-identifier arm was screened, its Format-4
/// twin was not. Here a receiver is what the grammar's RECEIVING rules produce and nothing else, so every sending
/// alternative a mixed rule ever gains is a non-receiver without an edit — §8.4.3.2.3 SR1 ("A function-identifier
/// shall not be specified as a receiving operand") and §8.4.3.4.3 SR1 ("Inline method invocation shall not be
/// specified as a receiving operand") hold by construction. <c>ArithmeticOperandRoleDriftTests</c> pins
/// <see cref="MixedRules"/> and <see cref="ReceiverRules"/> against the <c>.g4</c>, so a new mixed rule or a new
/// receiving rule fails at build time rather than as a dropped receiver.
/// </para>
/// </summary>
internal static class ArithmeticOperandRole
{
    /// <summary>The grammar rules that carry BOTH roles — every rule offering a receiving rule beside a sending
    /// identifier format. Each verb binder hands its node to <see cref="FirstNonReceiver"/> in Format 1.</summary>
    internal static readonly IReadOnlyList<Type> MixedRules =
    [
        typeof(Core.ReceiversOrSendingOperandContext),
        typeof(Core.MultiplyByOperandContext),
    ];

    /// <summary>The rules whose node denotes the receiver role inside a mixed rule (<see cref="IsReceiver"/>'s
    /// two arms). <c>receivingOperand</c> (MULTIPLY's BY operand) is <c>dataReference | literal</c>, so only its
    /// data-reference arm is a receiver.</summary>
    internal static readonly IReadOnlyList<Type> ReceiverRules =
    [
        typeof(Core.ReceivingArithmeticOperandContext),
        typeof(Core.ReceivingOperandContext),
    ];

    /// <summary>Is <paramref name="operand"/> — one rule child of a mixed-rule node — a RECEIVER: a data
    /// reference the parser placed through a receiving rule? Everything else (a literal, a function-identifier,
    /// an inline method invocation, any sending alternative added later) is not.</summary>
    internal static bool IsReceiver(ParserRuleContext operand) => operand switch
    {
        Core.ReceivingArithmeticOperandContext => true,
        Core.ReceivingOperandContext r => r.dataReference() is not null,
        _ => false,
    };

    /// <summary>The first operand under <paramref name="mixed"/> that is NOT a receiver, or null when every
    /// operand is one. The ROUNDED phrase qualifies the operand before it and is not an operand.</summary>
    internal static ParserRuleContext? FirstNonReceiver(ParserRuleContext mixed)
    {
        if (mixed.children is null) return null;
        foreach (var child in mixed.children)
            if (child is ParserRuleContext operand and not Core.RoundedPhraseContext && !IsReceiver(operand))
                return operand;
        return null;
    }

    /// <summary>What a non-receiving operand IS, naming the rule that bars it from the receiving role — the
    /// DESCRIPTION half only; <see cref="IsReceiver"/> has already decided.</summary>
    internal static string Describe(ParserRuleContext operand) => operand switch
    {
        Core.FunctionCallContext => "a function-identifier (ISO §8.4.3.2.3 SR1: \"A function-identifier shall "
            + "not be specified as a receiving operand.\")",
        Core.InlineMethodInvocationContext => "an inline method invocation (ISO §8.4.3.4.3 SR1: \"Inline method "
            + "invocation shall not be specified as a receiving operand.\")",
        Core.LiteralContext or Core.ReceivingOperandContext => "a literal",
        _ => "not a receiving identifier",
    };
}
