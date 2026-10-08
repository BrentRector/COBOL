// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Binding.Bound;

// The CALL / CANCEL / EXIT PROGRAM / GOBACK bound nodes (P7 Step 10j: the binder half moved to
// Binding/Procedure/Verbs/CallBinder.cs; these types STAY here — Udf cross-constructs
// BoundCallProgram, the VersionConformancePass gates read UsedOverflowSpelling/ReturningSource, and
// the source-generated visitor keys on this namespace).

// ── Bound nodes — CALL / CANCEL / EXIT PROGRAM / GOBACK (ISO §14.9.4 / §14.9.5 / §14.9.14 / §14.9.18;
//    COBOLNET_INTERPROGRAM_DESIGN D1–D4) ────────────────────────────────────────────────────────────────────────

/// <summary>ISO §14.9.4.4 GR8 over a BOUND argument expression: "an argument that consists merely of a single
/// identifier or literal is regarded as an identifier or literal rather than an arithmetic or boolean
/// expression". A BY VALUE argument binds through <c>BindByValueExpr</c>, so a written literal arrives wrapped
/// in a <see cref="BoundComputedOperand"/> — and inside an expression a leading '−' is taken by
/// <c>unaryExpression</c> first, so <c>BY VALUE -1.234E-5</c> arrives as <c>BoundNegate(BoundNumLiteral)</c>
/// while <c>BY VALUE -0.00001234</c> arrives bare. This is the ONE reduction that recovers the literal from
/// either spelling.
/// <para>⛔ IT LIVES HERE, NOT IN EITHER CONSUMER (kb/Work PB165). The emitter had a private copy and the
/// binder needed the same answer for §14.8.2.3.3 conformance; two copies of one rule is how the CONFORMANCE
/// verdict and the EMITTED carrier come to disagree about what an argument is. The binder's screen and
/// <c>CallEmitter.ArgText</c>'s numeric-literal funnel now read the same reduction.</para></summary>
public static class Gr8ArgumentLiteral
{
    /// <summary>The literal text of <paramref name="e"/> with any leading sign folded in, or null when it is a
    /// genuine runtime expression.</summary>
    public static string? NumericText(BoundExpr e) => e switch
    {
        BoundNumLiteral n => n.Text,
        BoundNegate g => NumericText(g.Operand) is { } t ? Negated(t) : null,
        _ => null,
    };

    /// <summary>The literal text of the algebraic negation of <paramref name="text"/> (ISO §8.3.3.3.2 rule 2 —
    /// a sign, if used, is the leftmost character; §8.3.3.3.3 rule 2 makes a signed significand sign the whole
    /// floating-point literal).</summary>
    private static string Negated(string text)
    {
        string t = text.Trim().TrimStart('+');
        return t.StartsWith('-') ? t[1..] : "-" + t;
    }
}

/// <summary>One CALL USING argument: its resolved pass mode (the §14.9.4.4 GR5 transitivity already applied at
/// bind time), and either a resolved <see cref="Place"/> (a data-reference argument) or a bound
/// <see cref="Value"/> operand (a literal — inherently BY CONTENT — or a BY VALUE expression, §14.9.4.3 SR4).</summary>
public sealed record BoundCallArg(CobolPassMode Mode, Place? Place, BoundOperand? Value, bool Omitted = false)
{
    /// <summary>A <b>boolean-expression-1</b> argument (ISO §14.9.4.2 Format 2; kb/Work PB238) — the third of
    /// the BY CONTENT brace's four operand shapes, and the one the BY VALUE brace deliberately does not print.
    /// <para>It is its OWN value channel, not a spelling of <see cref="Value"/>: a boolean expression evaluates
    /// to a '0'/'1' bit-string value (§8.8.2) whose length is fixed by §8.8.2 rule 10, never to an algebraic
    /// value, so it can neither be rendered by the numeric renderer nor stored by the numeric store. The exact
    /// counterpart of <c>BoundInvokeArg.ContentBool</c>, which PB46 landed on the INVOKE side of the same rule;
    /// this slot is what closes §14.9.4.3 SR17's third channel on the CALL side.</para>
    /// <para>BY CONTENT by construction — an expression has no storage to write back to.</para></summary>
    public BoundBoolExpr? ContentBool { get; init; }

    /// <summary>An ADDRESS-IDENTIFIER argument (ISO §14.9.4.3 SR3; kb/Work PB239) — §8.4.3.1.2 identifier
    /// Format 9, in its DATA arm (§8.4.3.11, <c>ADDRESS OF identifier-1</c>). Its OWN value channel beside
    /// <see cref="Place"/> and <see cref="Value"/>, because it is neither: §8.4.3.11.4 GR1 makes it "a unique data
    /// item of class pointer and category data-pointer", which has no storage of the program's to alias.
    /// <para>⛔ SR4 makes it a SENDING operand WHATEVER the mode — "If the BY REFERENCE phrase is not specified
    /// or implied for an identifier-2 OR IF IDENTIFIER-2 IS AN ADDRESS-IDENTIFIER, identifier-2 is a sending
    /// operand" — and SR5 withholds the receiving role from it, so it always crosses as a detached pointer
    /// VALUE: a callee's store into its formal reaches that value and never the program's storage (Annex D:
    /// "it will never be updated even when passed by reference"). <see cref="Mode"/> still records the mode
    /// the source wrote, which the §14.9.4.3 SR19/SR21 mode correspondence reads.</para></summary>
    public BoundAddressOf? DataAddress { get; init; }

    /// <summary>The PROGRAM arm of the same identifier Format 9 (§8.4.3.13, <c>ADDRESS OF PROGRAM …</c>) —
    /// "a unique data item of class pointer and category program-pointer" (§8.4.3.13.4 GR1), crossing exactly
    /// as <see cref="DataAddress"/> does. kb/Work PB239.</summary>
    public BoundProgramAddress? ProgramAddress { get; init; }

    /// <summary>The CORRESPONDING FORMAL PARAMETER (ISO §14.2.3 GR2 — the positional correspondence), when the
    /// activated element's description is known to the ACTIVATING element at bind time, and null otherwise.
    /// <para>⛔ It is null for exactly the crossing §14.2.3 GR9's FIRST branch describes — "a program for which
    /// there is no program-specifier in the REPOSITORY paragraph of the activating runtime element and there is
    /// no NESTED phrase specified on the CALL statement" — whose allocated record is "of the same length as the
    /// argument" and whose argument "is moved to this allocated record without conversion". That is not an
    /// accident of this implementation: the standard's own partition between the no-conversion crossing and the
    /// COMPUTE/SET/MOVE crossing (GR9's second branch and GR10, and the same split in §14.8.2.3.3 rules 1 and
    /// 2) is precisely the partition between "the activating element cannot know the formal" and "it can", and
    /// §14.8.2's conformance loop in <c>CallBinder</c> is where it becomes known.</para>
    /// <para>Set for a Format-2 CALL (AS NESTED or a program prototype with a §12.3.8.4 GR10 a) definition) and
    /// for a user-defined FUNCTION reference. <c>CallEmitter.ArgText</c> reads it to perform GR9/GR10's COMPUTE
    /// on the ACTIVATING side, where those rules put it (kb/Work PB640).</para></summary>
    public DataItem? Formal { get; init; }

    /// <summary>The argument's §14.8.2 description (<c>ActivationDescriptions.OfCallArgument</c>) when the activating
    /// element holds no signature of the activated program (<see cref="BoundCallProgram.CalleeSignatureKnown"/> false): what
    /// §14.9.4.4 GR3 d) compares with the activated program's registered formal at call initiation (kb/Work PB165). Null
    /// otherwise, and for an omitted argument.</summary>
    public CobolNet.Runtime.ActivationDescription? Description { get; init; }
}

/// <summary><c>CALL {literal|identifier} [USING …] [RETURNING …] [ON …][NOT ON …]</c> (ISO §14.9.4 Format 1).
/// <paramref name="LiteralName"/> is the static target (SR2 — a non-zero-length alphanumeric literal);
/// <paramref name="DynamicName"/> the runtime-resolved identifier target (GR3b). The exception phrases carry
/// the bound imperatives; the OVERFLOW-vs-EXCEPTION spelling is edition-gated at bind time and semantically
/// identical here (at 85 the only exception condition IS the resolution failure the OVERFLOW phrase catches).</summary>
public sealed record BoundCallProgram(
    string? LiteralName,
    BoundOperand? DynamicName,
    IReadOnlyList<BoundCallArg> Args,
    Place? Returning,
    IReadOnlyList<BoundStatement>? OnException,
    IReadOnlyList<BoundStatement>? NotOnException) : BoundStatement, IActivatingStatement
{
    /// <inheritdoc/>
    public CobolNet.Runtime.Exceptions.EcCheckingProfile ActivatorChecking { get; init; }
        = CobolNet.Runtime.Exceptions.EcCheckingProfile.None;

    /// <inheritdoc/>
    public BoundStatement WithActivatorChecking(CobolNet.Runtime.Exceptions.EcCheckingProfile profile)
        => this with { ActivatorChecking = profile };


    /// <summary>True when this node is the lowering of a user-defined FUNCTION reference (M2-UDF): a locate
    /// miss stamps EC-FUNCTION-NOT-FOUND (Fatal, ISO §8.4.3.2.4 GR6b / Table 13) rather than the CALL's
    /// EC-PROGRAM-NOT-FOUND. Runtime dispatch is otherwise identical (the shared activation ABI).</summary>
    public bool IsFunction { get; init; }

    /// <summary>True when this CALL was written with the archaic ON OVERFLOW spelling (in either the ON or the NOT
    /// ON phrase) — the COBOL-74-carried synonym for ON EXCEPTION, REMOVED at ISO 2023 (Annex E.2 item 1c). The
    /// edition gate reads this in <see cref="Validation.VersionConformancePass"/> (rearch PHASE-03 Step 14d); the
    /// bound handlers are otherwise identical to the ON EXCEPTION form.</summary>
    public bool UsedOverflowSpelling { get; init; }

    /// <summary>True when <see cref="DynamicName"/> is a POINTER operand that holds the activated element's
    /// address, never a name string: a PROGRAM-POINTER CALL target (ISO §14.9.4.3 SR1 — identifier-1 may
    /// reference a program-pointer data item; P10 Step 7) activates through <c>ProgramRegistry.CallPointer</c>,
    /// and — with <see cref="IsFunction"/> — a function-identifier written with function-pointer-name-1
    /// (§8.4.3.2.4 GR4/GR6c; kb/Work PB847) activates through <c>ProgramRegistry.CallFunctionPointer</c>. The
    /// category of the operand and <see cref="IsFunction"/> always agree; the emitter's ONE invocation renderer
    /// reads the flag pair.</summary>
    public bool IsPointerTarget { get; init; }

    /// <summary>True when the ACTIVATING element held the activated element's formal parameters and RETURNING item when it
    /// was compiled — an AS NESTED call, a program-prototype whose details §12.3.8.4 GR10 a) / b) took from this
    /// compilation group, a user-defined function (§12.3.8.4 GR11) — so §14.8.2 / §14.8.3 were checked at bind time and
    /// every argument carries its <see cref="BoundCallArg.Formal"/>. False for the lanes whose activated program is located
    /// by name at run time with no compile-time signature: a dynamic Format-1 CALL, and a program-prototype whose details
    /// GR10 c) takes from the external repository. Only those describe their operands for §14.9.4.4 GR3 d)'s run-time
    /// check (<c>CallEmitter</c>; kb/Work PB165).</summary>
    public bool CalleeSignatureKnown { get; init; }

    /// <summary>The RETURNING item's §14.8.3 description (<c>ActivationDescriptions.OfPlace</c>) when
    /// <see cref="CalleeSignatureKnown"/> is false, for §14.9.4.4 GR3 d)'s run-time comparison with the activated program's
    /// registered returning item (kb/Work PB165); null otherwise.</summary>
    public CobolNet.Runtime.ActivationDescription? ReturningDescription { get; init; }
}

/// <summary><c>CANCEL {literal|identifier}…</c> (ISO §14.9.5): each target's next CALL finds its initial state
/// (GR3); contained programs cascade in reverse source order (GR4); open files close implicitly (GR9).</summary>
public sealed record BoundCancel(
    IReadOnlyList<(string? LiteralName, BoundOperand? DynamicName)> Targets) : BoundStatement;

/// <summary><c>EXIT PROGRAM [RAISING …]</c> (ISO §14.9.14 Format 2): in a program NOT under the control of a
/// calling runtime element it is equivalent to CONTINUE (GR2 — "no exception condition is raised even if the
/// RAISING phrase is specified"); in a called program it returns to the activator per the GOBACK rules (GR3),
/// staging <paramref name="Raising"/> for re-raise in the activator. The distinction is a RUNTIME property of
/// the activation, so the bound node is unconditional and the emitted code tests the activation flag.
/// (Archaic at 2023 — Annex F.1; flagged, not rejected.)</summary>
public sealed record BoundExitProgram(BoundRaising? Raising = null) : BoundStatement;

/// <summary><c>GOBACK [RETURNING x] [RAISING … | WITH {NORMAL|ERROR} STATUS [value]]</c> (ISO §14.9.18):
/// terminates the executing program — return to the caller in a called program (GR2), STOP-equivalent in a main
/// program (GR3). <paramref name="ReturningSource"/> moves into the procedure-division RETURNING item before
/// return (the activation result, GR2); <paramref name="Raising"/> stages an exception condition for re-raise in
/// the activator; <paramref name="Status"/> (COBOL-2023, mutually exclusive with RAISING) passes the termination
/// status to the OS — but ONLY in a main program (GR3/GR10; a called-program status phrase is inert, GR2), so the
/// emit guards it with <c>!__asCalled</c>. COBOL-2002+ (the STATUS phrase 2023+).</summary>
public sealed record BoundGoback(Place? ReturningSource, BoundRaising? Raising = null,
    TerminationStatus? Status = null) : BoundStatement
{
    /// <summary>The move of <see cref="ReturningSource"/> into the program's procedure-division RETURNING item,
    /// BOUND through <c>MoveBinder.BindMoveOf</c> (<see cref="ImplicitMovePhrase.GobackReturning"/>) so the
    /// written GOBACK RETURNING gets the syntax screens and storage facts an explicit MOVE of the same pair does
    /// (kb/Work PB880 — the emitter used to build it). Null when there is no RETURNING phrase, or when the
    /// program has no RETURNING item (the emitter's loud arm reports that case at run time).</summary>
    public BoundMove? ReturningMove { get; init; }
}
