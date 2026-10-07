// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding.Procedure;

/// <summary>The KIND of source element whose PROCEDURE DIVISION is being bound. ISO §14.2.2 SR10 enumerates the
/// five source elements that may carry a Format-1/2 procedure division — "a function definition, a function
/// prototype definition, a method definition, a program definition, or a program prototype definition" — and the
/// EXIT / GOBACK placement rules are written against that enumeration, not against a program/not-program bit.
/// <para>⛔ THE ENUMERATION IS THE POINT (kb/Work PB403): <c>BindExit</c>'s SR7 arm used to test the single
/// predicate <c>host.InMethod</c>, so of the four non-program elements exactly one was refused and a FUNCTION
/// definition ending in EXIT PROGRAM was accepted AND given the program-activation machinery. A scalar where the
/// rule names a SET is the shape that rejects — or here admits — silently.</para></summary>
internal enum SourceElementKind
{
    /// <summary>A program definition (ISO §10.3) — the only element whose procedure division EXIT PROGRAM's
    /// §14.9.14.3 SR7 and GOBACK's program arm are written about.</summary>
    Program,

    /// <summary>A program prototype definition (ISO §10.6; PROGRAM-ID … IS PROTOTYPE). Its procedure division is
    /// still a PROGRAM's — the noun in §14.2.2 SR10's fifth item is "program" — so it is admitted wherever SR7
    /// says "a program procedure division"; the alternative reading would reject source the standard never
    /// forbids.
    /// <para>REACHABLE since kb/Work PB894 made §11.10.2 Format 2 writable (<c>prototypePhrase</c>, shared with
    /// FUNCTION-ID). Any statement in a prototype's procedure division is refused by §10.6.2 SR4 f)
    /// (COBOLNET2272, <c>PrototypeUnitRules</c>), NOT by the placement rules — which is what
    /// <c>ExitPlacementContextDriftTests.ProgramPrototypeDefinition_ExitProgram_IsRefusedBySr4fNotBySr7</c>
    /// pins; §14.9.4.3 SR13's NESTED screen (<c>CallBinder</c>) refuses this element, because its "program
    /// definition" does not name it.</para></summary>
    ProgramPrototype,

    /// <summary>A function definition (ISO §10.4; FUNCTION-ID). NOT a program procedure division.</summary>
    FunctionDefinition,

    /// <summary>A function prototype definition (ISO §10.6; FUNCTION-ID … IS PROTOTYPE). NOT a program
    /// procedure division — the noun is "function".</summary>
    FunctionPrototype,

    /// <summary>A method definition (ISO §11.7). NOT a program procedure division — a method returns through
    /// GOBACK / EXIT METHOD.</summary>
    MethodDefinition,
}

/// <summary>One frame of the bind-time ENCLOSING-CONSTRUCT STACK — the lexical constructs a statement can be
/// written INSIDE that a placement syntax rule asks about. Pushed by the binder that descends into the
/// construct's statement block and popped when it leaves, so the innermost frame is the last.</summary>
internal enum EnclosingConstruct
{
    /// <summary>An inline PERFORM's imperative-statement-1 (ISO §14.9.28.2 Format 2).</summary>
    InlinePerform,

    /// <summary>An exception-checking PERFORM (ISO §14.9.28.2 Format 3), pushed over the WHOLE statement —
    /// imperative-statement-1, every handler body and the FINALLY phrase — because §14.9.14.4 GR4 gives an
    /// EXIT PERFORM written in any of them the same meaning: the implicit CONTINUE before END-PERFORM.</summary>
    ExceptionCheckingPerform,

    /// <summary>A WHEN / WHEN OTHER / WHEN COMMON phrase of an exception-checking PERFORM (imperative-statement
    /// 2/3/4) — the position §14.9.33.3 SR1 and §14.9.14.3 SR6 name beside "a declarative procedure".</summary>
    PerformWhen,

    /// <summary>A FINALLY phrase (imperative-statement-5). NOT a WHEN phrase: RESUME is not admitted there
    /// (§14.9.33.3 SR1), which is why the two handler kinds are separate frames.</summary>
    PerformFinally,
}

/// <summary>One frame of <see cref="EnclosingContext"/>'s stack: the construct, and — for an inline PERFORM — whether
/// that PERFORM is written "with the VARYING phrase or either the TEST BEFORE or TEST AFTER phrase", the one fact
/// ISO §14.9.28.3 SR8 asks of every PERFORM a statement is UNDER (kb/Work PB434; the fact itself is
/// <c>PerformFormat.SpecifiesVaryingOrTest</c>). A frame, not a second stack beside the first, so the lexical
/// "where am I bound?" answer stays one structure.</summary>
internal readonly record struct EnclosingFrame(EnclosingConstruct Construct, bool SpecifiesVaryingOrTest = false);

/// <summary>
/// ⛔ THE ONE "WHERE AM I BOUND?" PROBE — the answer every PLACEMENT syntax rule needs, computed once at the bind
/// cursor instead of re-derived by hand at each verb (kb/Work PB403, PB404).
///
/// <para><b>Why it exists.</b> ISO writes at least eight placement rules over four verbs — §14.9.14.3 SR2, SR6,
/// SR7, SR8; §14.9.18.3 SR1, SR5; §14.9.33.3 SR1, SR2 — and every one of them asks some part of the same
/// question: which source element is this, am I inside a declarative (and does its USE carry GLOBAL), am I inside
/// an inline or exception-checking PERFORM, am I inside a WHEN phrase. Before this type, RESUME answered it by
/// scanning <c>ctx.Table.Declaratives</c> in its own binder, the exception-checking PERFORM answered the
/// EXIT-PERFORM-CYCLE half by WALKING ITS OWN PARSE SUBTREE after the fact, EXIT PROGRAM answered a quarter of it
/// with one boolean, and EXIT PERFORM's placement half was not answered at all — which is how
/// <c>EXIT PERFORM</c> outside every PERFORM compiled clean and emitted a bare <c>break;</c> that spun the pc
/// dispatcher forever. One question written down four times is the defect; the extraction is the fix.</para>
///
/// <para><b>What it is.</b> A read-only view over <see cref="BinderContext"/>'s live bind position: the source
/// element kind, the enclosing-construct stack, and the declarative section containing the bind cursor. It
/// carries no state of its own and is recomputed per read, so it cannot go stale.</para>
/// </summary>
internal readonly struct EnclosingContext
{
    private readonly List<EnclosingFrame>? _stack;

    internal EnclosingContext(SourceElementKind element, List<EnclosingFrame> stack, BoundDeclarative? declarative)
    {
        SourceElement = element;
        _stack = stack;
        Declarative = declarative;
    }

    /// <summary>The kind of source element whose procedure division is being bound (ISO §14.2.2 SR10).</summary>
    public SourceElementKind SourceElement { get; }

    /// <summary>Is this a PROGRAM's procedure division — the predicate §14.9.14.3 SR7 states as "a program
    /// procedure division"? True for a program definition and for a program prototype definition (§10.6); false
    /// for the three FUNCTION / METHOD elements §14.2.2 SR10 names beside them.</summary>
    public bool InProgramProcedureDivision =>
        SourceElement is SourceElementKind.Program or SourceElementKind.ProgramPrototype;

    /// <summary>The declarative section containing the bind cursor, or null outside the declaratives portion —
    /// the ONE lookup behind §14.9.14.3 SR2, §14.9.18.3 SR1 and §14.9.33.3 SR1/SR2.</summary>
    public BoundDeclarative? Declarative { get; }

    /// <summary>Is this statement in a declarative procedure (ISO §14.3)?</summary>
    public bool InDeclarative => Declarative is not null;

    /// <summary>Is this statement in a declarative procedure "for which the GLOBAL phrase is specified in the
    /// associated USE statement" (ISO §14.9.14.3 SR2, §14.9.18.3 SR1, §14.9.33.3 SR2)?</summary>
    public bool InGlobalDeclarative => Declarative is { Global: true };

    /// <summary>Is this statement in a USE BEFORE REPORTING procedure (a Format-2 declarative, ISO §14.9.49.2) —
    /// the position §14.9.45.3 SR1 admits SUPPRESS in and §14.9.49.3 SR10 / SR11 restrict (kb/Work PB363)?</summary>
    public bool InBeforeReportingProcedure => Declarative is { ReportGroup: not null };

    /// <summary>The NEAREST enclosing PERFORM statement's format, or null when this statement is inside none.
    /// The WHEN / FINALLY frames are transparent here: a statement in a handler body is still inside its
    /// exception-checking PERFORM (§14.9.14.4 GR4). An OUT-OF-LINE PERFORM never appears — it does not
    /// lexically contain the statements it runs, which is exactly why §14.9.14.3 SR8 excludes it.</summary>
    public EnclosingConstruct? NearestPerform
    {
        get
        {
            if (_stack is null) return null;
            for (int i = _stack.Count - 1; i >= 0; i--)
                if (_stack[i].Construct is EnclosingConstruct.InlinePerform or EnclosingConstruct.ExceptionCheckingPerform)
                    return _stack[i].Construct;
            return null;
        }
    }

    /// <summary>Is this statement inside "an inline or exception-checking PERFORM statement" — the one predicate
    /// ISO §14.9.14.3 SR8's first sentence states for EXIT PERFORM?</summary>
    public bool InPerform => NearestPerform is not null;

    /// <summary>Is the NEAREST enclosing PERFORM an exception-checking (Format-3) one? The CYCLE prohibition in
    /// §14.9.14.3 SR8's second sentence is about the PERFORM an EXIT PERFORM CYCLE would cycle, which is the
    /// nearest one — a CYCLE inside an ordinary inline PERFORM nested in a Format-3 PERFORM is legal.</summary>
    public bool InExceptionCheckingPerform => NearestPerform is EnclosingConstruct.ExceptionCheckingPerform;

    /// <summary>Is this statement in "a WHEN phrase in a PERFORM statement" (ISO §14.9.14.3 SR6, §14.9.18.3 SR5,
    /// §14.9.33.3 SR1)? ANY enclosing WHEN frame counts — a statement nested in an inline PERFORM written inside
    /// a WHEN phrase is still an imperative statement in that WHEN phrase.</summary>
    public bool InPerformWhen => _stack is not null && _stack.Exists(f => f.Construct == EnclosingConstruct.PerformWhen);

    /// <summary>Is this statement lexically UNDER an inline PERFORM "with the VARYING phrase or either the TEST
    /// BEFORE or TEST AFTER phrase" (ISO §14.9.28.3 SR8)? ANY enclosing such frame counts, however deep — an inline
    /// PERFORM's imperative-statement-1 is its specified set (§14.9.28.4 GR4), so everything nested in it is in its
    /// range. The OUT-OF-LINE half of "under" (a statement reached through a PERFORM of a procedure) is not lexical
    /// and is answered over the bound procedures by <c>UntilExitPlacement</c>.</summary>
    public bool UnderVaryingOrTestPerform => _stack is not null && _stack.Exists(f => f.SpecifiesVaryingOrTest);

    /// <summary>⛔ THE ONE PREDICATE BEHIND "only in a declarative procedure or a WHEN phrase of a PERFORM
    /// statement" — the position THREE rules name, once per statement: the RAISING LAST phrase of GOBACK
    /// (§14.9.18.3 SR5) and of EXIT (§14.9.14.3 SR6), and the RESUME statement itself (§14.9.33.3 SR1).
    /// <para>kb/Work PB410 is what makes it one property rather than three tests: SR5 was written down TWICE in
    /// the binder — not at all on the program arm, and as an UNCONDITIONAL refusal on the method arm, by a
    /// diagnostic whose own text quoted the rule the source satisfied. The rule carries no method qualifier and
    /// no verb qualifier, so neither does this predicate; what differs per statement is only the ORDINAL the
    /// message cites, which travels on <see cref="EcRaiseSite"/>.</para></summary>
    public bool InDeclarativeOrPerformWhen => InDeclarative || InPerformWhen;
}

/// <summary>
/// ⛔ THE ASKERS — one method per PLACEMENT RULE SHAPE the EXIT / GOBACK / RESUME family shares, so a rule the
/// standard states once per statement is WRITTEN ONCE here and cited per statement by the caller's
/// <see cref="EcRaiseSite"/> (kb/Work PB404, PB409, PB410).
///
/// <para><b>Why a screen and not an <c>if</c> at each verb.</b> <see cref="EnclosingContext"/> answers "where am
/// I bound?"; it does not answer "is that legal here?". Before this type the second half was the part that kept
/// going missing: §14.9.18.3 SR1 and §14.9.14.3 SR2 had NO asker at all (a GOBACK or EXIT PROGRAM in a GLOBAL
/// declarative compiled clean while the byte-identical RESUME program was refused), and §14.9.18.3 SR5 had TWO
/// askers that disagreed — nothing on the program arm and an unconditional refusal on the method arm. One
/// predicate + one message + one citation channel means the next statement that acquires one of these rules is
/// one call, and <c>ExitPlacementContextDriftTests</c> is where its row goes.</para>
///
/// <para>Each returns TRUE when the statement is REFUSED (the diagnostic has been raised), so a caller reads
/// <c>if (PlacementRules.X(...)) return BoundRejected.Reported(ctx.Edition);</c> — the refusal the statement funnel
/// verifies drew its error (kb/Work PB1029).</para>
/// </summary>
internal static class PlacementRules
{
    /// <summary>ISO §14.9.18.3 SR1 (GOBACK) / §14.9.14.3 SR2 (EXIT Format 2) — "shall not be specified in a
    /// declarative procedure for which the GLOBAL phrase is specified in the associated USE statement".
    /// <para>⛔ SPECIFIED IN, not EXECUTED WITHIN THE RANGE OF. The second is §14.9.18.4 GR6's run-time relation
    /// over legal source (a GOBACK written in an ordinary paragraph that a global declarative PERFORMs), and it
    /// is raised by the EMITTER as EC-FLOW-GLOBAL-GOBACK — not here. A bind-time approximation of GR6 would
    /// reject exactly the programs SR1 already covers and miss exactly the ones it does not.</para>
    /// <para>⚠ RESUME's §14.9.33.3 SR2 is the same sentence for a third statement but keeps its own
    /// COBOLNET0713: its refusal sits inside a longer decision (GR1 makes a RESUME in a global declarative's
    /// DYNAMIC scope a CONTINUE, which is the arm this screen has no counterpart for).</para></summary>
    public static bool RefusedInGlobalDeclarative(BinderContext ctx, EcRaiseSite site)
    {
        if (!ctx.Enclosing.InGlobalDeclarative) return false;
        ctx.Edition.Error(DiagnosticCatalog.ReturnInGlobalDeclarative,
            $"{site.Verb} shall not be specified in a declarative procedure whose USE statement carries the "
            + $"GLOBAL phrase ({site.Cite(site.GlobalDeclarativeRule)})");
        return true;
    }

    /// <summary>ISO §14.9.18.3 SR5 (GOBACK) / §14.9.14.3 SR6 (EXIT) — "The LAST phrase may be specified only in
    /// a declarative procedure or WHEN phrase of a PERFORM statement". The two clauses word it differently
    /// ("or a WHEN phrase in a PERFORM statement") and mean the same two positions, so ONE predicate answers
    /// both and the ordinal comes from the site.
    /// <para>⛔ NO METHOD QUALIFIER. §14.9.18.4 GR4 makes a method's GOBACK a GOBACK and §14.9.18.3 carries no
    /// "in a program" wording, so the method arm asks exactly this — it used to refuse the phrase outright, in a
    /// message that quoted this very rule (kb/Work PB410).</para></summary>
    public static bool RefusedRaisingLastHere(BinderContext ctx, EcRaiseSite site)
    {
        if (ctx.Enclosing.InDeclarativeOrPerformWhen) return false;
        ctx.Edition.Error(DiagnosticCatalog.RaisingLastOutOfPlace,
            $"{site.Context} LAST EXCEPTION: the LAST phrase may be specified only in a declarative procedure "
            + $"or a WHEN phrase of a PERFORM statement ({site.Cite(site.LastRule)})");
        return true;
    }
}
