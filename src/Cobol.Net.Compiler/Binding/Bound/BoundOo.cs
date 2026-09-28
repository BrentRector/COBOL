// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>An INVOKE's resolved call form (OO deep-dive D1/D5 — the binder chooses; both backends render).</summary>
public enum InvokeForm
{
    /// <summary>The standard class BASE's New (ISO §16.2.1) on a NAMED factory object: <c>INVOKE Class "New"
    /// RETURNING obj</c> (<see cref="BoundInvoke.ClassCsName"/> — the class's factory singleton) or through a FACTORY OF
    /// reference (<see cref="BoundInvoke.Receiver"/>). Both render <c>obj = (T)factory.__New()</c>: the runtime
    /// BASE__FACTORY's one body, whose covariant <c>__Create</c> is the generated constructor (deep-dive D4).</summary>
    New,
    /// <summary><c>INVOKE obj "M" …</c> → <c>RequireNonNull(obj).M(…)</c> — virtual dispatch on the runtime
    /// class (§9.3.6) behind the §14.9.23.4 GR5 null guard.</summary>
    Instance,
    /// <summary><c>INVOKE SELF "M" …</c> → <c>this.M(…)</c> — VIRTUAL dispatch on the RUNTIME class
    /// (§8.4.3.8 GR2: a subclass override wins even when the SELF call sits in an inherited base method —
    /// oo_self_polymorphic). Never null — no guard (slice 3b).</summary>
    Self,
    /// <summary><c>INVOKE SUPER "M" …</c> → <c>base.M(…)</c> — NON-virtual, resolution STARTS at the base
    /// class (§8.4.3.8 GR3 restricted search), so an override calling its base cannot recurse (slice 3b).</summary>
    Super,
    /// <summary><c>INVOKE class-name "M" …</c> (non-NEW) → <c>CLS__FACTORY.__Instance.M(…)</c> — a FACTORY
    /// method through the class's factory singleton (§11.4/§9.3.6; brief D11 — never null, no guard;
    /// virtual, so an inherited factory override dispatches).</summary>
    Factory,
    /// <summary><c>INVOKE SELF|SUPER "New" RETURNING r</c> inside a FACTORY method → <c>r = (T)this.__New()</c>
    /// (§16.2.1.2 GR1 ACTIVE-CLASS creation: the covariant per-class <c>__Create</c> override makes an inherited
    /// factory MAKE create the RUNTIME factory's class — the canonical factory pattern).</summary>
    NewSelf,
}

/// <summary>A bound INVOKE (ISO §14.9.23; deep-dive D5): the RESOLVED call form plus everything the backend
/// needs to render it — no name lookup happens at emit time. <paramref name="ClassCsName"/> is the emitted C#
/// type (New form); <paramref name="Receiver"/>/<paramref name="MethodCsName"/> drive the Instance form (the
/// method name is the ROSTER's exact spelling — COBOL names compare case-insensitively, §8.3.2.2, and the
/// C# override chain must reuse one spelling — the legacy trap-#2 rule); <paramref name="Returning"/> receives
/// the invocation result (NEW's created object, or the method's <paramref name="ReturningSource"/> item per
/// §14.9.23.4 GR8); <paramref name="Args"/> carries the positionally-bound USING arguments (D6 — GR3).</summary>
public sealed record BoundInvoke(
    InvokeForm Form, string? ClassCsName, Place? Receiver, string? MethodCsName, Place? Returning,
    IReadOnlyList<BoundInvokeArg>? Args = null, DataItem? ReturningSource = null,
    string? OwnerCsName = null)
    : BoundStatement, IActivatingStatement
{
    /// <inheritdoc/>
    public CobolNet.Runtime.Exceptions.EcCheckingProfile ActivatorChecking { get; init; }
        = CobolNet.Runtime.Exceptions.EcCheckingProfile.None;

    /// <inheritdoc/>
    public BoundStatement WithActivatorChecking(CobolNet.Runtime.Exceptions.EcCheckingProfile profile)
        => this with { ActivatorChecking = profile };

}

/// <summary>One bound INVOKE argument (deep-dive D6; §14.9.23.4 GR6): the FORMAL it corresponds to
/// positionally (its description drives the marshaling — §14.8.2's strict conformance was validated at bind,
/// so the crossing is type-preserving), the identifier source place OR the literal (decoded string / raw
/// numeric text), and whether the argument writes back (BY REFERENCE identifier — changes visible to the
/// caller; BY CONTENT and the §14.9.23.3 SR 10 object-data auto-CONTENT case do not).</summary>
public sealed record BoundInvokeArg(
    DataItem Formal, Place? Source, string? NumericLiteral, string? StringLiteral, bool WriteBack,
    bool ByContent = false)
{
    /// <summary>A BY CONTENT <b>arithmetic-expression-1</b> argument (ISO §14.9.23.2; fix-queue PB46) — the
    /// operand shape the format admits that is neither an identifier nor a literal, so it has no
    /// <see cref="Source"/> and no literal text.</summary>
    /// <remarks>
    /// It is BY CONTENT by construction: §14.9.23.3 SR9 confines BY REFERENCE to an identifier, and an
    /// expression has no storage to write back to — <see cref="WriteBack"/> is always false for it.
    /// </remarks>
    public BoundExpr? ContentExpr { get; init; }

    /// <summary>A BY CONTENT <b>boolean-expression-1</b> argument (ISO §14.9.23.2; fix-queue PB46) — the third
    /// operand shape the format's BY CONTENT branch admits, and the one BY VALUE deliberately does not.</summary>
    /// <remarks>
    /// It is its OWN value channel, not a spelling of <see cref="ContentExpr"/>: a boolean expression evaluates
    /// to a '0'/'1' bit-string value (§8.8.2; D-B1) whose length is fixed by §8.8.2 rule 10, never to an
    /// algebraic value, so it can neither be rendered by the numeric renderer nor stored by the numeric store.
    /// A bare boolean LITERAL argument rides this same slot — it is a boolean VALUE with no storage, which is
    /// exactly what this channel carries. BY CONTENT by construction, like <see cref="ContentExpr"/>.
    /// </remarks>
    public BoundBoolExpr? ContentBool { get; init; }

    /// <summary>A figurative-constant or ALL-literal <b>literal-2</b> (a <see cref="BoundFigurative"/> or
    /// <see cref="BoundAllLiteral"/>; ISO §8.3.3.6.3 SR1 — "A figurative constant may be used whenever 'literal'
    /// appears in a format"; kb/Work PB1617). Its OWN channel, not a spelling of <see cref="StringLiteral"/>: its
    /// value has no length of its own — §14.2.3 GR9's MOVE into the formal's allocated record sizes it
    /// (§8.3.3.6.4 GR2) and the formal's category picks HIGH-/LOW-VALUE's sequence — so the emitter renders it
    /// through the ONE argument fill the CALL and function lanes use (<c>CallEmitter.FigurativeArgumentImage</c>).
    /// BY CONTENT by construction: a literal never meets SR9 (§14.9.23.4 GR6 a) 2.).</summary>
    public BoundOperand? ContentFill { get; init; }

    /// <summary>The argument is OMITTED (kb/Work PB757): the OMITTED phrase was written (ISO §14.9.23.2) or the
    /// argument is a trailing one omitted from the statement (§14.8.2.1; §9.3.6 match rule 1). It has no source and no literal;
    /// the omitted-argument condition for <see cref="Formal"/> shall be true in the invoked method (§14.9.23.4
    /// GR9), and the binder has proved the formal OPTIONAL (§14.9.23.3 SR18).</summary>
    public bool Omitted { get; init; }

    /// <summary>An ADDRESS-IDENTIFIER argument (§14.9.23.3 SR9; kb/Work PB1021): no <see cref="Source"/>, no
    /// literal — the pointer VALUE §8.4.3.11.4 GR1 / §8.4.3.13.4 GR1 create, a SENDING operand whatever the
    /// written phrase (SR19), so <see cref="WriteBack"/> is always false for it.</summary>
    public BoundAddressOperand? Address { get; init; }

    /// <summary>The predefined NULL as the argument (kb/Work PB1137 + PB1630): an identifier (§8.4.3.1.2 Format 6,
    /// the NULL object reference of class object, §8.4.3.7.3 SR2; Format 8, the NULL address of class pointer,
    /// §8.4.3.10.1) passed BY CONTENT into a formal of class object reference or pointer — whichever the formal's
    /// class makes it (§8.4.3.10.3 SR1 names "the associated data item's class"), so the value that crosses is the
    /// formal's own null. No <see cref="Source"/>, no literal, never written back.</summary>
    public bool PredefinedNull { get; init; }

    /// <summary>The predefined object reference SELF as the argument (ISO §8.4.3.8; kb/Work PB1137): the object on
    /// which the containing method runs, passed BY CONTENT into an object-reference formal — no
    /// <see cref="Source"/>, never written back (§8.4.3.8.3 bars SELF as a receiving operand).</summary>
    public bool SelfObject { get; init; }
}

/// <summary>A bound UNIVERSAL-receiver INVOKE (deep-dive D10/D-U5): there is NO formal roster at compile
/// time, so the bound facts differ in KIND from <see cref="BoundInvoke"/> — the method selector is a
/// bind-normalized literal OR a data-item Place read at runtime (§14.9.23.3 SR7), and every argument
/// carries its caller-side CONFORMANCE DESCRIPTOR (OoClassTable.ConformanceDescriptor — checked by the
/// callee's generated switch at runtime per §14.9.23.4 GR7c, mismatch → EC-OO-UNIVERSAL). Every argument
/// is BY REFERENCE (SR6 — implicit), so every argument writes back through its box.</summary>
public sealed record BoundInvokeUniversal(
    Place Receiver, string? MethodLiteral, Place? MethodSource,
    IReadOnlyList<BoundUniversalArg> Args, Place? Returning, string? ReturningDescriptor)
    : BoundStatement, IActivatingStatement
{
    /// <inheritdoc/>
    public CobolNet.Runtime.Exceptions.EcCheckingProfile ActivatorChecking { get; init; }
        = CobolNet.Runtime.Exceptions.EcCheckingProfile.None;

    /// <inheritdoc/>
    public BoundStatement WithActivatorChecking(CobolNet.Runtime.Exceptions.EcCheckingProfile profile)
        => this with { ActivatorChecking = profile };

}

/// <summary>One universal-dispatch argument: the storage and its conformance descriptor (D-U3).</summary>
public sealed record BoundUniversalArg(Place? Source, string Descriptor)
{
    /// <summary>A spelled OMITTED argument (ISO §14.9.23.2; kb/Work PB757) — no source; its descriptor is
    /// <c>CobolInvokeArg.OmittedDescriptor</c>, and the callee's switch admits it only against an OPTIONAL formal
    /// (§9.3.6 match rule 3 b), checked at runtime through a universal receiver (§14.9.23.4 GR7c).</summary>
    public bool Omitted => Source is null && Address is null;

    /// <summary>An ADDRESS-IDENTIFIER argument (ISO §14.9.23.3 SR9; kb/Work PB1137): no <see cref="Source"/> — the
    /// pointer VALUE §8.4.3.11.4 GR1 / §8.4.3.13.4 GR1 create crosses in the box under its class-pointer
    /// <see cref="Descriptor"/>, and SR19 makes it a SENDING operand, so the box is never copied back.</summary>
    public BoundAddressOperand? Address { get; init; }
}

/// <summary>SET Format 5 — object-reference assignment (ISO §14.9.39 :31162; D-U7): copy ONE sender
/// reference into each target in order (GR9/GR10). The sender is a Place, the NULL figurative, SELF
/// (legal only inside a method; renders <c>this</c>), a class-name (SR13 — renders the D11 factory
/// singleton <c>{SourceFactoryCs}.__Instance</c>), or the EXCEPTION-OBJECT register (§8.4.3.6 — the
/// EC-OO wave; implicitly UNIVERSAL, so a TYPED target takes the generated runtime narrow check,
/// §9.3.8.2 :12291 → EC-OO-UNIVERSAL).</summary>
public sealed record BoundSetObjectRef(IReadOnlyList<Place> Targets, Place? Source, bool SourceIsNull, bool SourceIsSelf) : BoundStatement
{
    public string? SourceFactoryCs { get; init; }
    public bool FromExceptionObject { get; init; }
}

/// <summary>A method-context <c>GOBACK</c> / (pre-2023) <c>EXIT METHOD</c> (ISO §14.9.18.4 GR4; deep-dive D8 —
/// the one decision that silently miscompiles if missed): terminates the executing METHOD only, returning
/// control to the INVOKE site — never the run unit (<see cref="BoundStop"/>) and never the program activation
/// (<see cref="BoundGoback"/>). Rendered as <c>throw new MethodReturn()</c>, caught at the method's public
/// entry (the ProgramReturn-pattern realization of D8: a plain <c>return</c> cannot unwind the nested bounded
/// <c>__Dispatch</c> frames an out-of-line PERFORM stacks).</summary>
public sealed record BoundMethodReturn(BoundRaising? Raising = null) : BoundStatement;
// Raising: GOBACK/EXIT METHOD … RAISING from a method (§14.9.18.4 GR1b; the EC-OO wave) — STAGED before
// the MethodReturn throw; the INVOKE site's pickup applies the §14.6.13.1.5 activator rules AFTER the
// RETURNING delivery + copy-outs (GR1b's result-before-exception ordering falls out of the throw/catch).
