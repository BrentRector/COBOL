// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// The EC exception-condition bind state (P7 Step 10r — the plan's "EcBindState on ctx"): the per-unit
/// mutable state the <c>EcBinder</c> members and the Declaratives half share, hoisted off the god class.
/// <see cref="Turn"/>/<see cref="ProgramName"/> are configured per bound unit (ConfigureEc);
/// <see cref="PdRaising"/>/<see cref="PdRaisingObjects"/> hold the PROCEDURE DIVISION header RAISING lists
/// (§14.2.1/§14.2.2 — per-method reset via EcLoadPdRaising); the seven bits accumulate the emitter's
/// <see cref="EcFeatures"/> gating summary in ctor order.
/// </summary>
internal sealed class EcBindState
{
    /// <summary>The compilation group's source-ordered &gt;&gt;TURN fold (§7.3.25).</summary>
    public TurnState Turn { get; set; } = TurnState.Empty;

    /// <summary>The compilation group's POSITION-RULED directive sites (ISO §7.3.20.3 SR4, §7.3.22.3 SR4,
    /// §7.3.25.3 SR5 — where each &gt;&gt;TURN / &gt;&gt;PUSH / &gt;&gt;POP was written, in the final line
    /// frame). The ONE input to the lexical-containment predicate owner decision D20 requires (kb/Work PB595).</summary>
    public IReadOnlyList<Frontend.Preprocessor.DirectiveSite> DirectiveSites { get; set; } = [];

    /// <summary>The directive sites already reported inside an exception-checking PERFORM. NESTED Format-3
    /// PERFORMs both contain the same directive line, and the rule is about the DIRECTIVE: it is one violation
    /// and one warning, whichever statement's bind reaches it first.</summary>
    public HashSet<int> ReportedDirectiveBans { get; } = [];

    /// <summary>This unit's PROGRAM-ID name — the §15.30.3 r2 location element.</summary>
    public string ProgramName { get; set; } = "";

    /// <summary>PD-header RAISING exception-names (§14.2.1; the GOBACK/EXIT SR2 check).</summary>
    public HashSet<string> PdRaising { get; } = new(CobolNames.Comparer);

    /// <summary>PD-header RAISING object alternatives — `[FACTORY OF] object-class-name-1` and
    /// `interface-name-1` (§14.2.1 / §14.2.2 SR8–SR9), as the <see cref="Model.RaisingTarget"/> tuples the
    /// GOBACK §14.9.18.3 SR4 / EXIT §14.9.14.3 SR5 identifier check compares against (kb/Work PB815/PB814).</summary>
    public List<Model.RaisingTarget> PdRaisingObjects { get; } = [];

    // ⛔ `InF3When` LIVED HERE and is gone (kb/Work PB403). "Am I binding inside a WHEN phrase of an
    //    exception-checking PERFORM?" is one question that FOUR syntax rules ask — §14.9.33.3 SR1 (RESUME),
    //    §14.9.14.3 SR6 and §14.9.18.3 SR5 (the RAISING LAST placement), and the XS-RESUME-OPERAND ban — and a
    //    bool owned by the EC state answered it for exactly one of them. It is now a frame of the ONE bind
    //    position probe: `ctx.Enclosing.InPerformWhen` (see EnclosingContext), pushed by EcBindExceptionPerform
    //    around each handler body and popped with it, so a nested bind cannot leave it set.

    /// <summary>This unit contains at least one exception-checking (Format-3) PERFORM → the emitter must install
    /// the ambient F3-frame stack and route raise sites through <c>__EcPerform</c> even when the unit declares no
    /// F3 USE declaratives (the <c>EcDispatchExpr</c> UnitHasF3 gate is insufficient — §5.4/§5.2-6).</summary>
    public bool F3Perform { get; set; }

    /// <summary>Allocate the next per-unit Format-3 PERFORM id (0-based) — disambiguates nested F3 PERFORMs for the
    /// emitted try/catch(<c>ExitPerformSignal</c>)-<c>when(Id==n)</c> boundary and the imp-1/FINALLY labels
    /// (<c>__f3fin{n}</c>/<c>__f3end{n}</c>). Reset per unit.</summary>
    private int _f3PerformCounter;
    public int NextF3PerformId() => _f3PerformCounter++;

    // EcFeatures accumulators (the emitter's gating summary — BoundProgram.Ec), in ctor order.
    public bool Checked { get; set; }
    public bool IoChecked { get; set; }
    public bool Raise { get; set; }
    public bool Resume { get; set; }
    public bool F3 { get; set; }
    public bool Functions { get; set; }
    public bool Raising { get; set; }

    public EcFeatures BuildFeatures() => new(Checked, IoChecked, Raise, Resume, F3, Functions, Raising, F3Perform);
}
