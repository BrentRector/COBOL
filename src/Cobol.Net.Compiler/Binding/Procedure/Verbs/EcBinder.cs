// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// The EC exception-condition binder (P7 Step 10r; ISO/IEC 1989:2023 §14.6.13;
/// COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D9–D12): binds RAISE (§14.9.29), RESUME (§14.9.33), SET LAST
/// EXCEPTION TO OFF (§14.9.39 F13), the GOBACK/EXIT RAISING phrase (§14.9.18/§14.9.14), and — because bound
/// nodes carry no parse context — performs the compile-time TurnState fold HERE (the statement's source line
/// is a parse-tree property): a statement whose kind has any enabled relevant exception-name is wrapped in
/// <see cref="BoundEcChecked"/> carrying the bind-time decision; a statement with none binds UNwrapped, so
/// checking-off emits nothing new (deep-dive D10 / SSOT §18.16). State lives on
/// <see cref="BinderContext.EcState"/> (shared with the Declaratives half) and the bind cursor on
/// <see cref="BinderContext.BindCursor"/>; <c>EcWrap</c> is still invoked at the host BindStatement exit.
/// The intrinsic-presence walks moved VERBATIM — their generated-visitor conversion stays FLAGGED as a
/// behavior-sensitive follow-up (the plan block). Host edges (InMethod/OoClasses/ResolveProcedure/
/// Declaratives/EntryPc/Paragraphs/ParaSections) flip at 10t.
/// </summary>
internal sealed partial class EcBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Configure the EC bind context (called per bound unit — <see cref="BinderDriver"/> for program
    /// units, the OO bind half for class rosters):
    /// the compilation group's TurnState, its position-ruled directive sites (§7.3.25.3 SR5 and its PUSH/POP
    /// siblings) and this unit's PROGRAM-ID name (the §15.30.3 r2 location element).</summary>
    public void ConfigureEc(TurnState turn, IReadOnlyList<Frontend.Preprocessor.DirectiveSite> sites,
        string programName)
    {
        ctx.EcState.Turn = turn;
        ctx.EcState.DirectiveSites = sites;
        ctx.EcState.ProgramName = programName;
    }

    /// <summary>Record a FUNCTION EXCEPTION-* binding (sets the group EC gate so the generated source carries
    /// the runtime using; called from the intrinsic bind when the catalog row is an Ec* runtime method).</summary>
    public void EcNoteFunction() => ctx.EcState.Functions = true;

    // ── RAISE (§14.9.29) ─────────────────────────────────────────────────────────────────────────────────────

    public BoundStatement BindRaise(Core.RaiseStatementContext r)
    {
        // raise-statement-2002: the pass owns the edition gate (Exec Step E).
        if (r.cobolWord() is not { } ecWord)
        {
            // RAISE identifier-1 — an exception OBJECT (§14.9.29.3 SR2/SR3; §14.6.13.1.5). NOT TURN-gated
            // (§7.3.25 takes exception-NAMES only) and never fatal by itself (GR2).
            var oref = r.objectReference();
            // The plain term — null for an inline invocation or an object-view, which the computed arm below binds.
            var atom = oref.objectReferenceAtom()?.objectReferenceTerm();
            if (atom?.predefinedNull() is not null || atom?.selfAndSuper()?.SUPER() is not null)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0848",
                    $"RAISE {(atom.predefinedNull() is not null ? "NULL" : "SUPER")}: NULL and SUPER shall not be "
                    + "specified as the raised object (ISO §14.9.29.3 SR2)");
            }
            ctx.EcState.Raise = true;   // the machinery gate — the object channel is live once used
            // identifier-1 written as an inline invocation or a function-identifier (kb/Work PB1197, PB1425): SR2 asks
            // the CLASS of the item it references, so the temporary is the raised object when it is one.
            if (host.Oo.OoBindComputedObjectReference(oref) is { } computed)
                return host.Oo.OoObjectReferenceTemporary(computed, oref, "COBOLNET0848",
                        "identifier-1 shall be an object reference (ISO §14.9.29.3 SR2)") is { } raised
                    ? new BoundRaiseObject(raised) : BoundRejected.Reported(ctx.Edition);
            if (atom!.selfAndSuper()?.SELF() is not null)
            {
                if (!host.InMethod)
                {
                    return BoundRejected.Report(ctx.Edition, "COBOLNET0848",
                        "RAISE SELF may be specified only within a method definition (ISO §8.4.3.8.3 SR1)");
                }
                return new BoundRaiseObject(null);
            }
            // kb/Work PB1030: a reference that did not resolve is the resolver's diagnostic, never SR2's.
            if (host.Expr.ResolveSending(atom.dataReference()!).PlaceOrReported(ctx.Edition) is not { } op)
                return BoundRejected.Reported(ctx.Edition);
            if (op.Item.Pic?.Category is not PicCategory.ObjectReference)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0848",
                    $"RAISE '{oref.GetText()}': identifier-1 shall be a USAGE OBJECT REFERENCE data item "
                    + "(ISO §14.9.29.3 SR2)");
            }
            return new BoundRaiseObject(op);
        }

        if (EcResolveLevel3(ecWord.GetText(), EcRaiseSite.Raise) is not { } info)
            return BoundRejected.Reported(ctx.Edition);   // diagnosed — fail the compile, bind a placeholder
        ctx.EcState.Raise = true;
        int line = r.Start.Line;
        bool enabled = ctx.EcState.Turn.Enabled(info.Name, null, line);
        // kb/Work PB2505: a nonfatal name raised with checking off raises nothing (§14.6.13.1.1, §14.6.13.1.4) and the
        // emitter writes no code for it (EcEmitter.EmitRaise reads this same bool), so the statement is a CONTINUE the
        // source does not look like. Say so. A fatal name is not advised: with checking off it terminates (PB2504).
        if (!enabled && !info.IsFatal)
        {
            ctx.Edition.Warning(DiagnosticCatalog.RaiseCheckingNotEnabled,
                $"RAISE EXCEPTION {info.Name}: checking for {info.Name} is not enabled at this statement, so the RAISE "
                + "raises nothing and execution continues as if the exception did not occur (ISO §14.6.13.1.1, "
                + $"§14.6.13.1.4); write >>TURN {info.Name} CHECKING ON before it if it is meant to raise");
        }
        bool withLoc = enabled && ctx.EcState.Turn.WithLocation(info.Name, null, line);
        return new BoundRaise(info.Name, info.IsFatal, enabled, withLoc, EcLocation(line));
    }

    /// <summary>Resolve and validate a written exception-name for the RAISE/RAISING contexts — the ONE funnel
    /// (kb/Work R05) plus this site's LEVEL-3 requirement (RAISE §14.9.29.3 SR1 / GOBACK §14.9.18.3 SR2 / EXIT
    /// §14.9.14.3 SR3, checked before the introduction gate so the level error keeps priority). The SITE carries
    /// its own clause and ordinal (kb/Work PB388) — this path serves three statements. Null after
    /// diagnosing.</summary>
    private EcInfo? EcResolveLevel3(string name, EcRaiseSite site) =>
        EcNameResolution.TryResolve(ctx.Edition, name, site.Context, out var info, level3: site)
            ? info : null;

    // ── RESUME (§14.9.33) ────────────────────────────────────────────────────────────────────────────────────

    public BoundStatement BindResume(Core.ResumeStatementContext r)
    {
        // resume-statement-2002: the pass owns the edition gate (Exec Step E).
        // SR1 — RESUME may appear in a declarative OR a WHEN phrase of an exception-checking PERFORM (§14.9.33.3
        // SR1). In a WHEN phrase it shall specify NEXT STATEMENT (XS-RESUME-OPERAND, COBOLNET1610) — the
        // ResumeSignal(targetPc) pc-jump path is bound ONLY for a declarative RESUME AT procedure-name.
        // ⛔ BOTH POSITIONS COME FROM THE ONE BIND-POSITION PROBE (kb/Work PB403/PB404): `ctx.Enclosing` answers
        // "am I in a WHEN phrase?" and "which declarative am I in?" for EVERY placement rule, so RESUME's rule
        // and the EXIT / GOBACK rules that state the same two positions cannot drift apart — which they had,
        // RESUME being the only one of the family that asked at all.
        var where = ctx.Enclosing;
        // SR2 — not in a GLOBAL-phrase declarative — IS ASKED FIRST (kb/Work PB1160), the order
        // PlacementRules.RefusedInGlobalDeclarative gives GOBACK and EXIT. SR2 is a flat prohibition ("shall not be
        // specified in a declarative procedure for which the GLOBAL phrase is specified"): it has no exception for a
        // statement nested in a WHEN phrase inside that declarative, so the WHEN-phrase arm below must never be
        // reached for one. (A RESUME EXECUTED within a global declarative's dynamic scope is CONTINUE, GR1 —
        // EcEmitter.EmitResume; this is the STATIC case, which rejects.)
        if (where.Declarative is { Global: true })
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0713", "RESUME shall not be specified in a declarative procedure whose "
                + "USE statement carries the GLOBAL phrase (ISO §14.9.33.3 SR2)");
        }
        if (where.InPerformWhen)
        {
            ctx.EcState.Resume = true;
            if (r.NEXT() is not null) return new BoundResume(DispatchResult.ResumeNext);
            return BoundRejected.Report(ctx.Edition, "COBOLNET1610", "RESUME in a WHEN phrase of an exception-checking PERFORM shall "
                + "specify NEXT STATEMENT (ISO §14.9.33.3 SR1)");
        }
        if (where.Declarative is null)
        {
            // XS-RESUME-PLACEMENT (§14.9.28.3): a RESUME in imperative-statement-1 or FINALLY of an F3 PERFORM
            // (neither a declarative nor a WHEN phrase) lands here too — the same "declarative or WHEN only" rule.
            return BoundRejected.Report(ctx.Edition, "COBOLNET0712", "RESUME may be specified only in a declarative or a WHEN phrase of "
                + "an exception-checking PERFORM (ISO §14.9.33.3 SR1)");
        }
        ctx.EcState.Resume = true;
        if (r.NEXT() is not null) return new BoundResume(DispatchResult.ResumeNext);

        // SR3 — procedure-name-1 shall be in the NONdeclarative portion.
        var pn = r.procedureName()!;
        // An unresolvable procedure-name is the ONE operand resolution's verdict, reported at COMPILE time
        // (kb/Work PB390) — never a BoundUnsupported claiming WiseOwl COBOL has not implemented RESUME.
        if (ctx.Table.ResolveProcedureOperand(pn, "RESUME AT", kind: ProcedureReferenceKind.Resume) is not { } target)
            return BoundRejected.Reported(ctx.Edition);
        // ⛔ THE RESOLUTION CARRIES THE SECTION, SO THE RULE ASKS THE RULE'S OWN QUESTION (kb/Work PB433).
        // This used to read `target.Start < ctx.Table.EntryPc` — pc arithmetic re-deriving "is this procedure
        // declarative?" from the layout choice that declarative paragraphs occupy the low pcs. §14.9.33.3 SR3
        // is written about the PORTION a procedure is in, which is a property of its SECTION (§14.3: the
        // declaratives portion consists of sections), and that is now what the resolution hands over.
        if (target.IsDeclarative)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0714", $"RESUME AT '{pn.GetText()}': the procedure shall be in the "
                + "nondeclarative portion of the program (ISO §14.9.33.3 SR3)");
        }
        return new BoundResume(target.Range.Start);   // GR3 — as if GO TO procedure-name-1
    }

    // ── SET LAST EXCEPTION TO OFF (§14.9.39 Format 13) ───────────────────────────────────────────────────────

    public BoundStatement BindSetLastException()
    {
        // set-last-exception-2002: the pass owns the edition gate (Exec Step E).
        ctx.EcState.Functions = true;   // touches the runtime last-exception register — the group EC gate
        return new BoundSetLastException();
    }

    // ── The GOBACK / EXIT PROGRAM RAISING phrase (§14.9.18 / §14.9.14 F2) ────────────────────────────────────

    /// <summary>Table 13's EC-RAISING-NOT-SPECIFIED (§14.6.13.1.6) — GR1b3a's substitute condition, and the
    /// name §14.9.18.3 SR2's compile-time diagnostic names.</summary>
    private const string RaisingNotSpecified = "EC-RAISING-NOT-SPECIFIED";

    /// <summary>Bind a RAISING phrase. Returns null for the identifier (exception-object) form — the caller
    /// degrades to a loud placeholder until the OO wave.</summary>
    public BoundRaising? EcBindRaising(Core.RaisingPhraseContext raising, int line, EcRaiseSite site)
    {
        // statement-raising-2002: the pass owns the edition gate (Exec Step E).
        ctx.EcState.Raising = true;
        if (raising.LAST() is not null)
        {
            // §14.9.18.3 SR5 / §14.9.14.3 SR6 — the LAST phrase's PLACEMENT, through the ONE asker (kb/Work
            // PB410, PB404). It sits HERE, on the shared raising binder, because that is the single path every
            // statement with a RAISING phrase takes — GOBACK, EXIT PROGRAM, EXIT FUNCTION and (since PB410) the
            // method arm — so no verb can acquire the phrase without acquiring its rule.
            // ⛔ REPORT AND KEEP BINDING, never `return null`: null is this method's EXCEPTION-OBJECT signal, and
            // every caller turns it into a BoundUnsupported saying "RAISING identifier (exception object — the OO
            // wave)". A placement violation would then print a SECOND diagnostic about a form the program did not
            // write. The Error above already fails the compile, so the node below is never emitted.
            PlacementRules.RefusedRaisingLastHere(ctx, site);
            // RAISING LAST EXCEPTION (§14.9.18.4 GR1b3): the name is the run-unit last exception status, so the
            // whole determination is a RUN-TIME one. What the BINDER owns is GR1b3a's other operand — "the
            // RAISING phrase of the procedure division header of the source element in which this EXIT statement
            // is contained" — and the §15.32.3 r2 / §15.30.3 r2 operands of the SUBSTITUTED condition, which
            // this statement itself causes (§7.3.25.4 GR7 keys them on the TURN governing THAT name HERE).
            bool rnsLoc = ctx.EcState.Turn.WithLocation(RaisingNotSpecified, null, line);
            return new BoundRaising(null, IsLast: true, Fatal: false,
                WithLocation: rnsLoc,
                StatementName: rnsLoc ? site.Verb.Split(' ')[0] : null,
                Location: rnsLoc ? EcLocation(line) : null,
                PdRaising: [.. ctx.EcState.PdRaising.OrderBy(n => n, CobolNames.Comparer)]);
        }
        if (raising.cobolWord() is not { } ecWord)
        {
            // The identifier leg (GOBACK §14.9.18.3 SR4 / EXIT §14.9.14.3 SR5 — the EC-OO wave): propagate an exception
            // OBJECT to the activator. SR4d: never a universal reference. SR4a: the DECLARED class (or a
            // superclass) shall appear in the containing source element's PD-header RAISING phrase —
            // discharged at COMPILE time, which makes the activated-side EC-OO-EXCEPTION rule-1 check
            // STATICALLY true in v1 (D-EO5: a typed reference only ever holds a conforming object, no
            // universal identifier-1 exists, and factory objects cannot enter a typed reference).
            if (raising.objectReference() is not { } oref) return null;
            bool isNull = oref.objectReferenceAtom()?.objectReferenceTerm()?.predefinedNull() is not null;
            Place? op = isNull ? null : RaisingObjectOperand(oref, site);
            if (!isNull && op is null) return null;   // reported
            if (op?.Item.Pic is not { Category: PicCategory.ObjectReference } opic)
            {
                // NULL references no object, and §14.9.18.4 GR1 b) 2. makes "the object referenced by identifier-1"
                // the exception object — there is nothing to propagate (kb/Work PB1425's determination).
                ctx.Edition.Error(DiagnosticCatalog.RaisingObjectOperandRule, isNull
                    ? $"{site.Context} NULL: the predefined NULL references no object, and the object referenced by "
                      + $"identifier-1 is what becomes the exception object in the activating runtime element "
                      + $"({site.Cite(site.ObjectRule)}; GR1 b) 2.)"
                    : $"{site.Context} '{DataBinder.WrittenText(oref)}': identifier-1 shall be a USAGE OBJECT REFERENCE "
                      + $"data item ({site.Cite(site.ObjectRule)})");
                return null;
            }
            // ⛔ SR5d (EXIT) / SR4d (GOBACK) ask ONE thing — is this reference UNIVERSAL — and before kb/Work PB389 it was
            // spelled "no class name recorded", which is equally true of a factory, an interface and an
            // ACTIVE-CLASS reference: the moment those became declarable, legal operands would have been
            // refused by a message naming a rule the program did not break. The descriptor answers it.
            var od = opic.ObjectRef ?? ObjectRefDescriptor.Universal;
            if (od.IsUniversal)
            {
                ctx.Edition.Error(DiagnosticCatalog.RaisingObjectOperandRule,
                    $"{site.Context} '{op.Item.CobolName}': identifier-1 shall not be a UNIVERSAL object "
                    + $"reference ({site.Cite(site.ObjectRule, "d")})");
                return null;
            }
            // a) / b) / c) — each asks the identifier's description (the ObjectRefDescriptor tuple) against the
            // PD-header RAISING phrase (the RaisingTarget tuples), FACTORY flag to FACTORY flag.
            if (RaisingObjectMismatch(host.OoClasses, od, ctx.EcState.PdRaisingObjects) is { } bad)
                ctx.Edition.Error(DiagnosticCatalog.RaisingObjectOperandRule, $"{site.Context} '{op.Item.CobolName}': {bad.Message} "
                    + $"({site.Cite(site.ObjectRule, bad.Sub)})");
            return new BoundRaising(null, IsLast: false, Fatal: false, ObjectSource: op);
        }

        if (EcResolveLevel3(ecWord.GetText(), site) is not { } info)
            return new BoundRaising("EC-RAISING-IMP", false, false);   // diagnosed; placeholder
        // The level-3 rule's SECOND paragraph (GOBACK §14.9.18.3 SR2 / EXIT §14.9.14.3 SR3): an EC-USER name
        // shall appear in the PD-header RAISING phrase — the statically detectable half binds as an error; the
        // runtime condition is EC-RAISING-NOT-SPECIFIED.
        if (info.Level2Parent is "EC-USER" && !ctx.EcState.PdRaising.Contains(info.Name))
            ctx.Edition.Error("COBOLNET0717", $"{site.Context} {info.Name}: an EC-USER exception-name shall be "
                + "specified in the RAISING phrase of the procedure division header "
                + $"({site.Cite(site.Level3Rule)} — otherwise EC-RAISING-NOT-SPECIFIED, Table 13)");
        // kb/Work R07: the location operands travel like BoundRaise's — WITH LOCATION per THIS name at THIS
        // line (§7.3.25.4 GR7); the statement name is the Table 12 row (verb's first word: GOBACK, or EXIT —
        // EXIT PROGRAM / FUNCTION / METHOD are formats of the EXIT statement).
        // ⛔ NO `Enabled:` ARGUMENT (kb/Work PB408). This element's own §7.3.25 fold answers "is checking
        // enabled HERE"; §14.9.18.4 GR1 b) asks whether it is enabled in the ACTIVATING runtime element, which
        // is a different element and not knowable here. Staging is unconditional; the activating statement's
        // EcCheckingProfile decides.
        return new BoundRaising(info.Name, IsLast: false,
            Fatal: info.IsFatal,
            WithLocation: ctx.EcState.Turn.WithLocation(info.Name, null, line),
            StatementName: site.Verb.Split(' ')[0], Location: EcLocation(line));
    }

    /// <summary>⛔ THE RAISING PHRASE'S identifier-1 IS ANY IDENTIFIER THAT IS AN OBJECT REFERENCE (kb/Work PB1425):
    /// GOBACK §14.9.18.3 SR4 / EXIT §14.9.14.3 SR5, "Identifier-1 shall be an object reference", ask its class, and
    /// §8.4.3.1.3 SR1 lets any identifier format answer — so it is bound through the same doors RAISE's identifier-1
    /// takes: a computed identifier (an inline invocation, a function-identifier, an object-view) to the temporary it
    /// references, SELF to its ACTIVE-CLASS temporary (§8.4.3.8.4 GR1 — whose description c) then asks), a data
    /// reference through the sending resolver. SUPER is refused by §8.4.3.8.3 SR3 inside the OO door. NULL is the
    /// caller's (it references no object). Null after reporting.</summary>
    private Place? RaisingObjectOperand(Core.ObjectReferenceContext oref, EcRaiseSite site)
    {
        if (host.Oo.OoBindComputedObjectReference(oref) is { } computed)
            return host.Oo.OoObjectReferenceTemporary(computed, oref, DiagnosticCatalog.RaisingObjectOperandRule.Code,
                $"identifier-1 shall be an object reference ({site.Cite(site.ObjectRule)})");
        var term = oref.objectReferenceAtom()!.objectReferenceTerm()!;
        if (term.selfAndSuper() is { } selfOrSuper)
            return host.Oo.OoBindOoIdentifier(selfOrSuper) is BoundNumRef self ? self.Place : null;
        // kb/Work PB1030: a reference that did not resolve is the resolver's diagnostic (null = reported).
        return host.Expr.ResolveSending(term.dataReference()!).PlaceOrReported(ctx.Edition);
    }

    /// <summary>Capture the PROCEDURE DIVISION header RAISING phrase of a program / function unit (§14.2.1) through
    /// the ONE partition (<see cref="RaisingPhrase.Partition"/>) — consumed by the SR2 exception-name check and
    /// the SR4/SR5 identifier check above.</summary>
    public void EcCollectPdRaising(Core.ProcedureDivisionContext pd)
        => EcLoadPdRaising(RaisingPhrase.Partition(pd.raisingClause(), host.OoClasses, ctx.Edition,
            "PROCEDURE DIVISION RAISING"));

    /// <summary>Load one source element's partitioned header RAISING phrase as the current per-element state —
    /// a program's (above) or a METHOD's (per-method reset: methods of one class bind through ONE binder).</summary>
    public void EcLoadPdRaising(IReadOnlyList<RaisingTarget> targets)
    {
        ctx.EcState.PdRaising.Clear();
        ctx.EcState.PdRaisingObjects.Clear();
        foreach (var t in targets)
            if (t.Kind is RaisingTargetKind.ExceptionName) ctx.EcState.PdRaising.Add(t.Name);
            else ctx.EcState.PdRaisingObjects.Add(t);
    }

    /// <summary>
    /// GOBACK §14.9.18.3 SR4 a)–c) / EXIT §14.9.14.3 SR5 a)–c) — ONE rule, printed twice with the same three
    /// sub-items, asked of the identifier's description <paramref name="od"/> (never universal here: d) is
    /// screened first) against the containing element's header RAISING phrase <paramref name="raising"/>.
    /// Returns null when the operand conforms, else the violated sub-item and why (kb/Work PB815/PB814).
    /// <list type="bullet">
    ///   <item>a) object-class-name — "the class identified by that object-class-name or one of the
    ///     superclasses of that class shall be specified in the RAISING phrase … and the presence or absence of
    ///     the FACTORY phrase is the same in the data description entry of identifier-1 as in the RAISING
    ///     phrase".</item>
    ///   <item>b) interface-name — "the interface referenced by that interface-name shall conform to an
    ///     interface specified in the RAISING phrase" (§9.3.8.2.3 — <see cref="OoConformance.InterfaceConformsTo"/>);
    ///     neither side of this alternative carries a FACTORY phrase, so the FACTORY half is satisfied by
    ///     construction.</item>
    ///   <item>c) ACTIVE-CLASS — "the class of the object containing the … statement, or one of the super classes
    ///     of that object, and the presence or absence of the FACTORY phrase shall be the same as that specified
    ///     in the RAISING phrase". The descriptor's name IS the containing class (§13.18.60.3 SR16), so a) and c)
    ///     are the same superclass walk. ⚠ For EXIT PROGRAM this leg has no reachable subject: SR7 confines EXIT
    ///     PROGRAM to a program procedure division and §13.18.60.3 SR16 confines ACTIVE-CLASS to a class — the
    ///     shared walk answers it for GOBACK and the EXIT METHOD form, and no EXIT-specific screen exists.</item>
    /// </list>
    /// </summary>
    private static (string Sub, string Message)? RaisingObjectMismatch(OoClassTable? table,
        ObjectRefDescriptor od, IReadOnlyList<RaisingTarget> raising)
    {
        // A description written with a parameter-name of the definition bound as itself (kb/Work PB2937): its class or
        // interface is each expansion's actual (§9.3.8.2.4), which may be one the RAISING phrase lists or conform to
        // one, so only a phrase that lists no class or interface with the identifier's FACTORY presence refuses it in
        // every expansion.
        if (od.Formal)
            return raising.Any(t => t.Kind is RaisingTargetKind.Interface
                                    || t.Kind is RaisingTargetKind.ObjectClass && t.Factory == od.Factory)
                ? null
                : (od.Kind is ObjectRefKind.Interface ? "b" : "a", $"no class or interface the RAISING phrase of the "
                    + "procedure division header of the containing source element specifies can be the one "
                    + $"'{od.Name}' names in an expansion — it shall be specified there");
        if (od.Kind is ObjectRefKind.Interface)
        {
            var mine = table?.FindInterface(od.Name!);
            foreach (var t in raising)
                if (t.Kind is RaisingTargetKind.Interface && mine is not null
                    && table?.FindInterface(t.Name) is { } listed
                    && OoConformance.InterfaceConformsTo(table!, mine, listed))
                    return null;
            return ("b", $"its interface '{od.Name}' shall conform to an interface specified in the RAISING phrase "
                + "of the procedure division header of the containing source element — interface conformance per "
                + "ISO §9.3.8.2.3, and");
        }

        // a) object-class-name / c) ACTIVE-CLASS: the class or a superclass, with the same FACTORY presence.
        string sub = od.Kind is ObjectRefKind.ActiveClass ? "c" : "a";
        string what = od.Kind is ObjectRefKind.ActiveClass
            ? $"the class containing it ('{od.Name}', ACTIVE-CLASS)" : $"its declared class '{od.Name}'";
        RaisingTarget? factoryMismatch = null;
        for (var c = table?.Find(od.Name!); c is not null; c = c.Base)
            foreach (var t in raising)
            {
                if (t.Kind is not RaisingTargetKind.ObjectClass
                    || !CobolNames.Same(t.Name, c.Name)) continue;
                if (t.Factory == od.Factory) return null;
                factoryMismatch ??= t;
            }
        if (factoryMismatch is { } fm)
            return (sub, $"the RAISING phrase of the procedure division header specifies '{fm.Spelled}', and the "
                + $"FACTORY phrase is {(od.Factory ? "" : "not ")}specified in the description of identifier-1 — its "
                + "presence or absence shall be the same in both");
        return (sub, $"{what} (or a superclass) shall be specified in the RAISING phrase of the procedure division "
            + "header of the containing source element");
    }

    // ── The per-statement TurnState fold (deep-dive D10) ─────────────────────────────────────────────────────

    /// <summary>Give every <see cref="IActivatingStatement"/> in <paramref name="node"/> the activating
    /// statement's own checking profile (§14.9.18.4 GR1 b)). A no-op when nothing is enabled at the line — the
    /// zero-scaffolding gate: such a site emits byte-identical text.
    /// <para>BOTH container shapes are traversed, and both must be: a bind-time desugar hoists its activation
    /// into a <see cref="BoundSequence"/>, and <see cref="BoundImplicitSeries.Rewrap"/> keeps a multi-operand
    /// statement's SERIES outermost with that sequence as its FIRST member (kb/Work PB419), so a
    /// sequence-only recursion would miss a function-identifier written in an operand of a multi-operand
    /// CLOSE / FREE / INITIALIZE / INITIATE / OPEN / TERMINATE — the activation would carry an empty profile
    /// and the condition it stages would never be raised here. <see cref="DeclaredRaisableNames"/> traverses the
    /// same shapes for the same reason.</para></summary>
    private static BoundStatement StampActivators(BoundStatement node, EcCheckingProfile profile) =>
        profile.IsEmpty ? node
        : node switch
        {
            BoundSequence seq => new BoundSequence([.. seq.Steps.Select(st => StampActivators(st, profile))]),
            BoundReceiverBrackets brackets => brackets.Mapped(st => StampActivators(st, profile)),
            BoundImplicitSeries ser =>
                new BoundImplicitSeries([.. ser.Members.Select(st => StampActivators(st, profile))]),
            BoundActivationSite site => new BoundActivationSite(StampActivators(site.Inner, profile)),
            IActivatingStatement a => a.WithActivatorChecking(profile),
            _ => node,
        };

    /// <summary>Wrap <paramref name="bound"/> in <see cref="BoundEcChecked"/> when the TurnState enables any
    /// exception-name RELEVANT to its kind at this statement's line (§7.3.25.4 GR6); otherwise return it
    /// untouched — the zero-scaffolding gate. The relevant set is the statement kind's raise points
    /// (the implemented families; names this implementation does not yet raise bind no wrapper — the
    /// §14.6.13.1.1 unimplemented-element license, recorded in the deep-dive).</summary>
    public BoundStatement EcWrap(Core.StatementContext s, BoundStatement bound)
    {
        if (!ctx.EcState.Turn.AnyEnabled) return bound;
        int line = s.Start.Line;
        // §14.9.18.4 GR1 b) — THE ACTIVATOR'S HALF, stamped here because here is the one exit every statement
        // funnels through. A condition a callee's GOBACK / EXIT … RAISING stages is raised in the ACTIVATING
        // runtime element only "if checking for that exception condition is enabled in the activating runtime
        // element", and RAISING LAST EXCEPTION picks the name at RUN time — so the activating statement carries
        // THIS element's §7.3.25 state at THIS line into the run. The recursion reaches the activations a
        // desugar hoisted into a sequence (a user-function reference inside a COMPUTE); a new activating node
        // inherits the stamp by implementing IActivatingStatement, never by remembering to set a property.
        if (bound is IActivatingStatement or BoundSequence or BoundImplicitSeries or BoundActivationSite or BoundReceiverBrackets)   // the shapes a profile lands on
            bound = StampActivators(bound, ctx.EcState.Turn.ProfileAt(line));
        // ⛔ ONE ENUMERATION (kb/Work PB3102, COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D13): the exception-names this statement
        // can raise come from DeclaredRaisableNames — the statement-shape rows, the operand rows and the ambient rows —
        // and this is the ONE place they are filtered by the TurnState at the statement's line (§7.3.25.4 GR6). Nothing
        // else computes a statement's names, so a name added to a table is checkable by construction.
        var scope = new EcRaiseScope(ElementKindOf(ctx.SourceElement), ctx.Data.Classification is not null);
        var enabled = DeclaredRaisableNames(bound, scope)
            .Where(d => ctx.EcState.Turn.Enabled(d.Ec, d.File?.CobolName, line))
            .Distinct()   // a sequence's steps can re-contribute a family (two hoisted activations ⇒ ProgramNames twice)
            .ToList();

        if (enabled.Count == 0) return bound;
        ctx.EcState.Checked = true;
        if (enabled.Any(e => e.Ec.StartsWith("EC-I-O", StringComparison.Ordinal))) ctx.EcState.IoChecked = true;
        // §15.32.3 r3: the recorded name comes from Table 12's 'Statement name' column, resolved from the
        // statement KIND (the parse rule) — the first TOKEN gave GO where Table 12 requires GO TO, and no token
        // can repair it because TO is an optional word (`GO PARA.` never spells one). kb/Work R04.
        // WITH LOCATION is resolved PER (name, file) pair — §15.32.3 r1 keys the answer on the TURN option of
        // the condition that was RAISED, so one WITH LOCATION directive must not contaminate the statement's
        // other enabled conditions (kb/Work R06; the former statement-level Any() did exactly that).
        var info = new EcStatementInfo(
            [.. enabled.Select(e => (e.Ec, e.File,
                ctx.EcState.Turn.WithLocation(e.Ec, e.File?.CobolName, line)))],
            Table12StatementNames.NameOf(s), EcLocation(line));
        // ⛔ THE WRAPPER DISTRIBUTES OVER AN IMPLICIT-STATEMENT SERIES, IT DOES NOT ENCLOSE IT. A multi-operand
        // CLOSE / FREE / INITIALIZE / INITIATE / OPEN / TERMINATE / VALIDATE *is* a separate statement per operand
        // (ISO §14.9.6.4 GR10, §14.9.15.4 GR2, §14.9.20.4 GR3, §14.9.21.4 GR5, §14.9.27.4 GR20, §14.9.46.4 GR4,
        // §14.9.50.4 GR3), and each of those seven rules names the resumption point of a declarative's RESUME …
        // NEXT STATEMENT as "the next implicit … statement, if any" — the §14.9.33.4 GR2 a) escape ("unless
        // general rules associated with the applicable statement specify otherwise"). The checked wrapper IS the
        // statement site the `-2` action falls out of, so one wrapper around the whole series would put the
        // landing past the LAST operand: measured as a silent wrong answer on INITIALIZE (kb/Work PB419).
        // Each member gets the SAME EcStatementInfo — one written statement, one >>TURN scope, one Table-12 name
        // and one §15.30.3 r2 location — because they are one statement's implicit expansion, not seven.
        return bound is BoundImplicitSeries ser
            ? new BoundImplicitSeries([.. ser.Members.Select(m => (BoundStatement)new BoundEcChecked(m, info))])
            : new BoundEcChecked(bound, info);
    }

    /// <summary>The §15.30.3 r2 location string for a statement on <paramref name="line"/>:
    /// "element-name; paragraph[ OF section]|section; line-id" (the line-id is the final preprocessed-text line
    /// number — the implementor-defined identifier of the source line).</summary>
    /// <summary>The §15.30.3 r2b location string (kb/Work PB63): part 1 the element name — "as specified in the
    /// FUNCTION-ID, METHOD-ID, or PROGRAM-ID paragraph of the function, method, or program containing the
    /// statement", so a statement inside a METHOD names the method, not its class; part 2 the procedure field —
    /// (a) no paragraph-name and no section-name: empty ("; ; "), (b) a paragraph-name, plus " OF section" when
    /// the paragraph is within a section, (c) a section-name and no paragraph-name: the section-name alone
    /// (the paragraph-name-OMITTED paragraph carries the empty name, never a placeholder — ProcedureTableBuilder);
    /// part 3 the implementor-defined line identifier (docs/CONFORMANCE.md §4 determination; kb/Work PB82) — the
    /// line of the statement's first token IN THE FILE THAT PHYSICALLY HOLDS IT: a bare number for the main
    /// source, <c>copybook-name(line)</c> for a statement inside COPY-incorporated text — <paramref name="line"/>
    /// is the RESULTANT (token) line, mapped here through the preprocessing chain's origin table.</summary>
    private string EcLocation(int line)
    {
        var origin = ctx.Edition.OriginOf(line);
        string lineId = string.Equals(origin.File, ctx.Edition.SourceFile, StringComparison.Ordinal)
            ? origin.Line.ToString()
            : $"{Path.GetFileName(origin.File)}({origin.Line})";
        string para = ctx.BindCursor >= 0 && ctx.BindCursor < ctx.Table.Paragraphs.Count ? ctx.Table.Paragraphs[ctx.BindCursor].Cobol : "";
        string? sec = ctx.BindCursor >= 0 && ctx.BindCursor < ctx.Table.ParaSections.Count ? ctx.Table.ParaSections[ctx.BindCursor]?.Name : null;
        string proc = para.Length == 0
            ? sec ?? ""                                          // (c) the section alone, or (a) nothing at all
            : para + (sec is not null ? " OF " + sec : "");      // (b) paragraph [OF section]
        string element = ctx.CurrentMethodScope?.MethodName ?? ctx.EcState.ProgramName;
        return $"{element}; {proc}; {lineId}";
    }
}
