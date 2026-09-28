// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Compiler.Oo;

/// <summary>One method of a class roster (ISO §11.7): the COBOL name, the emitted C# method name, the header
/// USING/RETURNING presence recorded at pass-1, and — filled by <c>DataBinder.OoBindMethodData</c> at class
/// bind time (BEFORE any statement binds, so every INVOKE site in the group sees the full signature) — the
/// resolved formal list, RETURNING item, and data roots (port slice 2, deep-dive D3/D6).</summary>
public sealed record OoMethodSymbol(
    string Name, bool HasUsing, bool HasReturning,
    CobolParserCore.MethodDefinitionContext Ctx)
{
    /// <summary>The name this method is externalized under (ISO §11.7.4 GR1 b): "the name is
    /// method-name-1. However, literal-1, if specified, is the name of method that is externalized to the
    /// operating environment"). It defaults to <see cref="Name"/>, and it is the ROSTER KEY: §14.9.23.2
    /// gives INVOKE no word form for the method at all — only literal-1 or identifier-2 — and §14.9.23.4
    /// GR2 a) resolves that literal "as described in 8.3.2.2, User-defined words", i.e. against the
    /// externalized name. Keying the roster here also realizes §11.7.3 SR9's "if method-name-1 OR
    /// literal-1 is the same as a method-name inherited or implemented" without a second lookup path
    /// (kb/Work PB303).</summary>
    public string ExternalizedName { get; init; } = Name;

    /// <summary>The key a UNIVERSAL dispatch selects this method by (kb/Work PB1405): §8.3.2.2 1) treats a universal
    /// INVOKE's method-name "as a COBOL word that maps to the externalized name of the method to be invoked", so the
    /// key is <see cref="ExternalizedName"/> — the same roster key the typed path resolves by — under the ONE
    /// case-insensitive word mapping the invocation side applies (<see cref="CobolNet.Runtime.CobolObject.NormalizeMethodName"/>,
    /// the roster's <c>OrdinalIgnoreCase</c> comparer at run time). Never <see cref="Name"/>: an AS phrase replaces it.</summary>
    public string DispatchKey => CobolNet.Runtime.CobolObject.NormalizeMethodName(ExternalizedName);

    /// <summary>The emitted C# method name. Starts as the sanitized-uppercase COBOL name; an OVERRIDE adopts
    /// its base slot's name verbatim (C# requires the override member name to match), and a collision with
    /// the owning class's type name (C# CS0542) or an emitted field takes a deterministic suffix — always a
    /// SYMBOL-level rename so every INVOKE site in the group follows automatically (§8.3.2.2: the
    /// user-word→externalized-name mapping is implementor-defined).</summary>
    public required string CsName { get; set; }

    /// <summary>The class that declares this method (set at pass-1) — the marshaling qualifier for the
    /// formal's class-level statics (numeric profiles) at CONTENT-conversion call sites.</summary>
    public OoClassSymbol Owner { get; set; } = null!;

    /// <summary>The METHOD's half of §14.9.23.4 GR7c's "enabled in BOTH" gate: whether EC-OO-UNIVERSAL checking
    /// is enabled for THIS method's procedure division header. Folded at BIND time from the compilation group's
    /// line-keyed TurnState (<c>TurnState.EnabledAtHeader</c>) and baked by <c>OoEmitter</c> as a compile-time
    /// literal.
    ///
    /// <para>⛔ It is folded HERE and not in the emitter because CODEGEN OWNS NO TurnState — every
    /// <c>&gt;&gt;TURN</c> query in the compiler lives in the binder and codegen consumes only the results.
    /// Reading TurnState from an emitter would be a second mechanism for a job the binder already does.</para>
    ///
    /// <para>The HEADER line is the query point: the raise sites are in the <c>__CobolInvoke</c> PROLOGUE — the
    /// arity, per-argument descriptor and RETURNING checks, i.e. the conformance of the header's USING / RETURNING
    /// — which run before any statement of the method body, and ISO §7.3.25.4 GR6 / GR8 scope a TURN to the
    /// "procedure division headers that follow". A TURN between METHOD-ID and the header therefore governs it
    /// (kb/Work PB1381; the METHOD-ID line used to be asked). A class- or program-level directive earlier in the
    /// file is picked up for free, since the fold walks every event with a lower line.</para></summary>
    public bool OoUniversalCheckingHere { get; set; }

    /// <summary>The METHOD's half of §14.8.4.1's "enabled in both the activating and activated runtime elements":
    /// the <c>ExternalChecks</c> bits of the EC-EXTERNAL conditions enabled "before the Environment division" of this
    /// method (<c>BinderDriver.ExternalMaskAt</c>, the one fold the program units' mask takes too), baked by
    /// <c>OoEmitter.EmitMethod</c> as the self mask of the method activation's external-item registrations
    /// (§14.9.23.4 GR7 d); kb/Work PB1138). Folded at bind time for the same reason as
    /// <see cref="OoUniversalCheckingHere"/>: codegen owns no TurnState.</summary>
    public int ExternalCheckMaskHere { get; set; }

    /// <summary>True for a FACTORY method (§11.4) — the SELF/SUPER roster selector and diagnostic wording;
    /// its formals' profiles/statics live on the FACTORY class, so CONTENT-conversion call sites qualify by
    /// <see cref="OoClassSymbol.FactoryCsName"/>.</summary>
    public bool IsFactory { get; init; }

    /// <summary>METHOD-ID … OVERRIDE (§11.7 SR3/SR4a — the OVERRIDE/FINAL wave, DEVLOG 605): an explicit
    /// override declaration. Redefinition WITHOUT it is the SR4a 0837 (error strict; warning + the pre-wave
    /// name-match inference under <c>--permissive</c> — the documented migration leniency).</summary>
    public bool HasOverride { get; init; }

    /// <summary>METHOD-ID … [IS] FINAL (§11.7 GR3 — shall not be overridden; 0839 on the attempt). Emits
    /// C# <c>sealed override</c> (or a non-virtual fresh slot at a root).</summary>
    public bool IsFinal { get; init; }

    /// <summary>The property-accessor identity (§11.7/§13.18.42 — the PROPERTY wave): 'G'/'S' for a GET/SET
    /// accessor of <see cref="PropertyName"/> (explicit <c>METHOD-ID. GET|SET PROPERTY p</c> or synthesized
    /// from a PROPERTY clause), '\0' for an ordinary named method. Accessor rosters key by the PINNED
    /// implementor-defined names <c>__GET_&lt;P&gt;</c>/<c>__SET_&lt;P&gt;</c> (§11.7.4 GR1a — the `__`
    /// cannot-collide rule), so override/0829/implements machinery applies to accessors unchanged.</summary>
    public char Accessor { get; init; }
    public string? PropertyName { get; init; }

    /// <summary>Which method of the standard class BASE this is (ISO §16.2 — its factory interface's New, its
    /// object interface's FactoryObject), or <see cref="StandardMethod.None"/> for every method written in the
    /// compilation group. A standard method has no METHOD-ID in source (its <see cref="Ctx"/> is null, like a
    /// synthesized PROPERTY accessor's) and no COBOL body: the runtime class <c>CobolNet.Runtime.BASE</c> is its
    /// implementation, so an invocation site binds it through its own form rather than the user-method tail.</summary>
    public StandardMethod Standard { get; init; }

    /// <summary>The method's own OPTIONS paragraph folded over the class channel's model (§10.6.1 method
    /// skeleton / §11.9.4 GR1; kb/Work PB135) — null when the method writes none. BindMethodRoster swaps it
    /// in around THIS method's body bind.</summary>
    public OptionsModel? MethodOptions { get; set; }

    /// <summary>The method's PD-header RAISING phrase (§14.2.1), partitioned by <c>RaisingPhrase.Partition</c>
    /// into the §14.2.1 tuples — loaded into the statement binder's per-source-element state before this
    /// method's body binds (a method IS a source element, §14.9.18.3 SR2/SR4).</summary>
    public List<RaisingTarget> Raising { get; } = [];

    /// <summary>For a PROPERTY-clause-synthesized accessor: the SUBJECT data item (the emitter renders a
    /// direct field read/write — observably identical to the spec's implicit MOVE method, §13.18.42 GR1/GR2
    /// :21214-21229, because the cloned descriptions are identical by construction). Null for explicit
    /// GET/SET PROPERTY methods (they carry real bodies).</summary>
    public DataItem? PropertySubject { get; set; }







    /// <summary>The AFTER-DATA-BIND half of the description (P9 R7 — phase-explicit): attached by
    /// <c>DataBinder.OoBindMethodData</c> once this method's data has bound; the pc range is stamped into it
    /// later still by <c>StatementBinder.BindClassBody</c>. Reading it before attach is a null-deref — the
    /// intended type-level ordering guard (never a silent -1 sentinel).</summary>
    public OoMethodBinding? Binding { get; internal set; }

    /// <summary>The method's own name scope (§11.7 GR5 shadowing; sibling invisibility — trap #6).</summary>
    public OoMethodDataScope DataScope { get; } = new();

    /// <summary>The base-chain method this method OVERRIDES (slice 3a — §9.3.6 dispatch is on the runtime
    /// class; D7: emitted as C# <c>override</c>), or null for a fresh <c>virtual</c> slot. Marked at pass-1
    /// by name (the OVERRIDE attribute is not in the grammar yet — the documented SR4a leniency); the
    /// SIGNATURE conformance (§9.3.8.2) validates after all class data binds
    /// (<see cref="OoConformance.ValidateOverrideSignatures"/>).</summary>
    public OoMethodSymbol? OverrideOf { get; set; }
}
