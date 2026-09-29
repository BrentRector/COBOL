// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

/// <summary>The shared state of ONE group bind (rearch PHASE-06 Step 2): built by <see cref="BinderDriver.Bind"/>
/// and handed to the <see cref="Compiler.Oo.OoDriver"/> + the per-unit binds so both sides of the driver's per-unit binds draw
/// from the SAME uid-band sequence and fold the SAME compile-time TurnState — exactly as the fused
/// <c>CallBindRunUnit</c> did with instance fields.</summary>
internal sealed class BindSession
{
    public required TurnState Turn { get; init; }

    /// <summary>The group's POSITION-RULED directive sites (ISO §7.3.20.3 SR4, §7.3.22.3 SR4, §7.3.25.3 SR5):
    /// where each &gt;&gt;TURN / &gt;&gt;PUSH / &gt;&gt;POP was written, in the final line frame. Every unit's
    /// EC bind state receives it, so the ONE lexical-containment predicate (kb/Work PB595 / owner decision D20)
    /// can decide all three bans. Empty when the source carries none of the three.</summary>
    public IReadOnlyList<Frontend.Preprocessor.DirectiveSite> DirectiveSites { get; init; } = [];
    public required OoClassTable OoClasses { get; init; }
    public required EditionContext Edition { get; init; }

    /// <summary>The group's compile-time <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> resolution (ISO §7.3.23) — the per-line
    /// zero-length allowance fold every unit's <see cref="ReferenceResolver"/> queries when building a ref-mod
    /// Place. Defaults to <see cref="RefModZeroLengthState.Empty"/> (the OFF default, no directive).</summary>
    public RefModZeroLengthState RefModZeroLength { get; init; } = RefModZeroLengthState.Empty;

    /// <summary>The group's compile-time <c>&gt;&gt;PROPAGATE</c> resolution (ISO §7.3.21; kb/Work PB1119) — folded
    /// once per source element (<c>BinderDriver</c>: a program unit's <see cref="DataBinder.AutomaticPropagation"/>,
    /// a method's <c>OoMethodSymbol.AutomaticPropagationHere</c>). Defaults to the §7.3.21.4 GR4 OFF.</summary>
    public PropagateState Propagate { get; init; } = PropagateState.Empty;

    /// <summary>The group's <c>&gt;&gt;COBOL-WORDS</c> override (ISO §7.3.10) — the per-compilation-group
    /// reserved/context/intrinsic word-table modification. <see cref="VersionConformancePass"/> composes the
    /// effective <c>ReservedWordSet</c> from it (RESERVE/UNDEFINE/SUBSTITUTE), and the intrinsic binder resolves
    /// function-name synonyms through it. Defaults to <see cref="CobolWordsMap.Empty"/> (no directive).</summary>
    public Editions.CobolWordsMap CobolWords { get; init; } = Editions.CobolWordsMap.Empty;

    /// <summary>The post-lex token decisions the tree was parsed under (kb/Work PB655 — the §8.9 words the token-level
    /// gate freed, plus the <c>&gt;&gt;COBOL-WORDS</c> map), handed to every fragment re-parse so it reads each word as
    /// the tree does. From <c>CompilationUnitContext.TokenRetypes</c>.</summary>
    public Frontend.Parsing.TokenRetypes Retypes { get; init; } = Frontend.Parsing.TokenRetypes.None;

    /// <summary>The group's <c>&gt;&gt;LEAP-SECOND</c> state (ISO §7.3.17; kb/Work PB65): true when ON — a
    /// formatted-time argument's seconds subfield may be 60 (§15.3.3.3) and standard numeric time form is bounded at
    /// 86,401 (GR4). Every unit's <see cref="DataBinder.LeapSecond"/> carries it to the intrinsic renderer.</summary>
    public bool LeapSecond { get; init; }

    /// <summary>The compilation group's REPOSITORY resolution sources (<see cref="GroupRepository"/>), built ONCE by
    /// <c>BinderDriver.Bind</c> after every DATA division has bound and before the FIRST procedure body binds —
    /// a CLASS's method bodies as well as a program unit's (kb/Work PB1100). Reading it earlier is a driver-order
    /// defect, so the getter throws rather than answering from an empty table.</summary>
    public GroupRepository Repository
    {
        get => _repository ?? throw new InvalidOperationException(
            "BindSession.Repository was read before BinderDriver.Bind built it (the tables need every unit's DATA "
            + "division bound, and every procedure body — class and program — reads them)");
        set => _repository = value;
    }
    private GroupRepository? _repository;

    private int _uidBand;

    /// <summary>Take the next disjoint 100k uid band (one per DataBinder, so nested-class struct/profile names
    /// never shadow a container's — the band discipline of the fused pipeline's <c>_callUidBand</c>).</summary>
    public int TakeUidBand()
    {
        int band = _uidBand;
        _uidBand += 100_000;
        return band;
    }
}

/// <summary>The two whole-group tables a REPOSITORY specifier resolves against (ISO §12.3.8.4): the user-defined
/// functions by user-function-name (GR11 a) / b) — definitions over same-name prototypes) and the program
/// definitions by EXTERNALIZED name (GR10 a) / b)). One pair per compilation group; each source element narrows
/// it to what ITS specifiers name (<c>BinderDriver.UserFunctionsOf</c> / <c>ProgramPrototypesOf</c>).</summary>
internal sealed record GroupRepository(
    IReadOnlyDictionary<string, UserFunctionSignature> UserFunctions,
    IReadOnlyDictionary<string, CalleeSignature> ProgramDefinitions);
