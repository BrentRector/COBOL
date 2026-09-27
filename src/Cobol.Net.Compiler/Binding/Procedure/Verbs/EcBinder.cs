// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

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
            if (oref.NULL_() is not null || oref.SUPER() is not null)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0848",
                    $"RAISE {(oref.NULL_() is not null ? "NULL" : "SUPER")}: NULL and SUPER shall not be "
                    + "specified as the raised object (ISO §14.9.29.3 SR2)");
            }
            ctx.EcState.Raise = true;   // the machinery gate — the object channel is live once used
            if (oref.SELF() is not null)
            {
                if (!host.InMethod)
                {
                    return BoundRejected.Report(ctx.Edition, "COBOLNET0848",
                        "RAISE SELF may be specified only within a method definition (ISO §8.4.3.8.3 SR1)");
                }
                return new BoundRaiseObject(null);
            }
            // kb/Work PB1030: a reference that did not resolve is the resolver's diagnostic, never SR2's.
            if (host.Expr.ResolveSending(oref.dataReference()!).PlaceOrReported(ctx.Edition) is not { } op)
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
        if (where.InPerformWhen)
        {
            ctx.EcState.Resume = true;
            if (r.NEXT() is not null) return new BoundResume(ResumeSignal.NextStatement);
            return BoundRejected.Report(ctx.Edition, "COBOLNET1610", "RESUME in a WHEN phrase of an exception-checking PERFORM shall "
                + "specify NEXT STATEMENT (ISO §14.9.33.3 SR1)");
        }
        if (where.Declarative is not { } decl)
        {
            // XS-RESUME-PLACEMENT (§14.9.28.3): a RESUME in imperative-statement-1 or FINALLY of an F3 PERFORM
            // (neither a declarative nor a WHEN phrase) lands here too — the same "declarative or WHEN only" rule.
            return BoundRejected.Report(ctx.Edition, "COBOLNET0712", "RESUME may be specified only in a declarative or a WHEN phrase of "
                + "an exception-checking PERFORM (ISO §14.9.33.3 SR1)");
        }
        // SR2 — not in a GLOBAL-phrase declarative (a RESUME executed within a global declarative's DYNAMIC
        // scope is CONTINUE, GR1 — realized by __RunGlobalUse swallowing the signal; the STATIC case rejects).
        if (decl.Global)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0713", "RESUME shall not be specified in a declarative procedure whose "
                + "USE statement carries the GLOBAL phrase (ISO §14.9.33.3 SR2)");
        }
        ctx.EcState.Resume = true;
        if (r.NEXT() is not null) return new BoundResume(ResumeSignal.NextStatement);

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
                PdRaising: [.. ctx.EcState.PdRaising.OrderBy(n => n, StringComparer.OrdinalIgnoreCase)]);
        }
        if (raising.cobolWord() is not { } ecWord)
        {
            // The identifier leg (GOBACK §14.9.18.3 SR4 / EXIT §14.9.14.3 SR5 — the EC-OO wave): propagate an exception
            // OBJECT to the activator. SR4d: never a universal reference. SR4a: the DECLARED class (or a
            // superclass) shall appear in the containing source element's PD-header RAISING phrase —
            // discharged at COMPILE time, which makes the activated-side EC-OO-EXCEPTION rule-1 check
            // STATICALLY true in v1 (D-EO5: a typed reference only ever holds a conforming object, no
            // universal identifier-1 exists, and factory objects cannot enter a typed reference).
            if (raising.dataReference() is not { } dref) return null;
            // kb/Work PB1030: a reference that did not resolve is the resolver's diagnostic (null = reported).
            if (host.Expr.ResolveSending(dref).PlaceOrReported(ctx.Edition) is not { } op) return null;
            if (op.Item.Pic is not { Category: PicCategory.ObjectReference } opic)
            {
                ctx.Edition.Error("COBOLNET0849",
                    $"{site.Context} '{DataBinder.WrittenText(dref)}': identifier-1 shall be a USAGE OBJECT REFERENCE "
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
                ctx.Edition.Error("COBOLNET0849",
                    $"{site.Context} '{op.Item.CobolName}': identifier-1 shall not be a UNIVERSAL object "
                    + $"reference ({site.Cite(site.ObjectRule, "d")})");
                return null;
            }
            // a) / b) / c) — each asks the identifier's description (the ObjectRefDescriptor tuple) against the
            // PD-header RAISING phrase (the RaisingTarget tuples), FACTORY flag to FACTORY flag.
            if (RaisingObjectMismatch(host.OoClasses, od, ctx.EcState.PdRaisingObjects) is { } bad)
                ctx.Edition.Error("COBOLNET0849", $"{site.Context} '{op.Item.CobolName}': {bad.Message} "
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
                    || !string.Equals(t.Name, c.Name, StringComparison.OrdinalIgnoreCase)) continue;
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

    /// <summary>The EC-SIZE family an arithmetic statement can raise through the checked-store path
    /// (§14.7.5 size error ↔ Table 13: store truncation, intermediate overflow, zero divide, exponentiation).</summary>
    private static readonly string[] SizeNames =
        // EC-SIZE-UNDERFLOW was missing from this enumeration (kb/Work PB145 — the same missing-member shape
        // as DELETE FILE in QueryFor): >>TURN EC-SIZE-UNDERFLOW CHECKING ON never reached a statement's mask,
        // so a §8.8.1.5.2 r2 too-small raise was caught by ON SIZE ERROR but NEVER RECORDED — FUNCTION
        // EXCEPTION-STATUS read the stale prior name inside the phrase.
        ["EC-SIZE-TRUNCATION", "EC-SIZE-OVERFLOW", "EC-SIZE-UNDERFLOW", "EC-SIZE-ZERO-DIVIDE", "EC-SIZE-EXPONENTIATION"];

    /// <summary>The EC-I-O family a file connector can set to exist — §9.1.13.1's status-derived names plus the
    /// ones a rule names outright (EC-I-O-LINAGE, §13.18.34.4 GR6 b) 2) — in THE canonical mask order
    /// (<see cref="ExceptionCatalog.IoMaskNames"/>; the emitter's per-statement mask bits).</summary>
    private static readonly string[] IoNames = ExceptionCatalog.IoMaskNames;

    /// <summary>The conditions a format 1 SORT with an INPUT PROCEDURE raises itself (kb/Work PB1036): -ACTIVE
    /// (a nested SORT, §14.9.40.4 GR10 / GR13) and -FILE-OPEN (a GIVING file, or a USING file, open when its
    /// phase commences — §14.9.40.4 GR9).</summary>
    private static readonly string[] SortProcedureNames = ["EC-SORT-MERGE-ACTIVE", "EC-SORT-MERGE-FILE-OPEN"];

    /// <summary>…and one with a USING phrase, whose implicit release also raises -RELEASE (§14.9.40.4 GR12 b)).</summary>
    private static readonly string[] SortUsingNames = [.. SortProcedureNames, "EC-SORT-MERGE-RELEASE"];

    /// <summary>The conditions a MERGE raises itself (kb/Work PB1036): -ACTIVE (§14.9.24.4 GR8), -FILE-OPEN
    /// (GR7 / GR12), -RELEASE (GR7 b)) and -SEQUENCE (GR6).</summary>
    private static readonly string[] MergeNames = [.. SortUsingNames, "EC-SORT-MERGE-SEQUENCE"];

    /// <summary>The OO fatal conditions an INVOKE raises (§14.9.23.4 GR5 EC-OO-NULL, GR7b EC-OO-METHOD).
    /// A PRECISE per-node gate, not an ambient tail one: an INVOKE is a distinguishable bound node, so the guard
    /// binds only on an actual INVOKE under <c>&gt;&gt;TURN EC-OO-* CHECKING ON</c>.</summary>
    private static readonly string[] OoInvokeNames = ["EC-OO-NULL", "EC-OO-METHOD", "EC-OO-UNIVERSAL"];

    /// <summary>The conditions ONLY a capacity SET (Format 14) can raise, so they bind PRECISELY on that node:
    /// EC-FLOW-SEARCH (§14.9.39.4 GR31 — the SET executed during a SEARCH of the same table) and EC-BOUND-SET
    /// (GR30's second arm — the new capacity above the expected maximum capacity; kb/Work PB460). Implicit growth
    /// raises EC-BOUND-OVERFLOW instead (§8.5.1.9.6 GR1), so EC-BOUND-SET has no inline raise site and needs no
    /// ambient gate.</summary>
    private static readonly string[] SetCapacityNames = ["EC-FLOW-SEARCH", "EC-BOUND-SET"];

    /// <summary>The conditions an INITIATE raises (ISO §14.9.21.4 GR2 EC-REPORT-ACTIVE, GR3 EC-REPORT-FILE-MODE
    /// — GR3 being the detection half of §14.9.27.4 GR7 — and §14.9.49.4 GR10 EC-FLOW-REPORT). PRECISE: the raise
    /// sites are <c>CobolReport.Initiate</c>'s three preconditions, reached from exactly this bound node.</summary>
    private static readonly string[] InitiateNames = ["EC-REPORT-ACTIVE", "EC-REPORT-FILE-MODE", "EC-FLOW-REPORT"];

    /// <summary>The conditions a GENERATE raises (ISO §14.9.16.4 GR7 EC-REPORT-INACTIVE, §14.9.49.4 GR10
    /// EC-FLOW-REPORT). PRECISE, as <see cref="InitiateNames"/>.</summary>
    private static readonly string[] GenerateNames = ["EC-REPORT-INACTIVE", "EC-FLOW-REPORT"];

    /// <summary>The conditions a TERMINATE raises (ISO §14.9.46.4 GR1 EC-REPORT-INACTIVE, §14.9.49.4 GR10
    /// EC-FLOW-REPORT). PRECISE, as <see cref="InitiateNames"/>.</summary>
    private static readonly string[] TerminateNames = ["EC-REPORT-INACTIVE", "EC-FLOW-REPORT"];

    /// <summary>The EC-PROGRAM family a CALL/CANCEL raises through <c>CobolCallException</c>.
    /// <para>⛔ NOT EC-PROGRAM-ARG-OMITTED (kb/Work PB971). It sat here as though the ACTIVATOR raised it; §14.9.4.4
    /// GR12 raises it at a REFERENCE in the called program, so its membership enabled the flag only around the
    /// CALL — in the caller, whose flags the activation boundary sets aside — and never at the callee statement
    /// that references the formal. It is now queried for EVERY statement under the element-kind rule
    /// (<see cref="ArgOmittedName"/>).</para></summary>
    private static readonly string[] ProgramNames =
    [
        "EC-PROGRAM-NOT-FOUND", "EC-PROGRAM-RECURSIVE-CALL", "EC-PROGRAM-CANCEL-ACTIVE",
        // §14.8.2.1 via §14.9.4.4 GR3d (kb/Work PB133 wave C2b) — the dynamic Format-1 count check at
        // activation; membership here is BOTH what lets the site's catch arm name it AND what makes
        // CallEmitter pass the ACTIVATING half of GR3d's enabled-in-both gate (siteArgMismatchChecking).
        "EC-PROGRAM-ARG-MISMATCH",
        // §14.9.4.4 GR3b — CALL through a program-pointer holding the predefined address NULL. Membership here is
        // what makes QueryFor(BoundCallProgram) report the CALL as checkable, so EcWrap wraps it in
        // BoundEcChecked and CallEmitter emits the name-filtered catch. Without it the runtime raise (ProgramTable
        // .CallPointer) has no guard to be caught by; without that raise using this name, the guard never matches.
        "EC-PROGRAM-PTR-NULL",
    ];

    /// <summary>The condition a USER-FUNCTION activation raises through <c>CobolCallException</c> that a CALL
    /// statement does not: ISO §8.4.3.2.4 GR6b — "If the function is not found, the EC-FUNCTION-NOT-FOUND
    /// exception condition is set to exist, the function is not activated, and execution continues as specified
    /// in General rule 6f", and GR6f runs "any declarative … associated with that exception condition".
    /// Queried only for a <c>BoundCallProgram</c> with <c>IsFunction</c> — the hoisted activation of a
    /// function-identifier — because that is the only node whose emitted invocation passes
    /// <c>notFoundEc: "EC-FUNCTION-NOT-FOUND"</c>. EC-FUNCTION-PTR-NULL (GR6c) is raised only by an activation
    /// THROUGH a function-pointer (<c>ProgramTable.CallFunctionPointer</c>, kb/Work PB847), so it is queried
    /// precisely for that node below rather than listed here; EC-FUNCTION-ARG-OMITTED (GR8) is not an ACTIVATION
    /// failure at all — it is raised at a reference inside the function (kb/Work PB971), and every statement of
    /// the function queries it under the element-kind rule (<see cref="ArgOmittedName"/>).</summary>
    private static readonly string[] FunctionActivationNames = ["EC-FUNCTION-NOT-FOUND"];

    /// <summary>The EC-EXTERNAL family a CALL raises through <c>CobolCallException</c> when the activated
    /// element's external descriptions do not conform (ISO §14.8.4 / §14.9.4.4 GR3e; the checkable trio —
    /// EC-EXTERNAL-IMP has no raise site, this implementation defines no implementor-specific external checks).
    /// The site-enabled subset ALSO drives the emitted CALL-site mask (§14.8.4.1's activating-element half).</summary>
    internal static readonly string[] ExternalNames =
    [
        "EC-EXTERNAL-FORMAT-CONFLICT", "EC-EXTERNAL-DATA-MISMATCH", "EC-EXTERNAL-FILE-MISMATCH",
    ];

    /// <summary>Give every <see cref="IActivatingStatement"/> in <paramref name="node"/> the activating
    /// statement's own checking profile (§14.9.18.4 GR1 b)). A no-op when nothing is enabled at the line — the
    /// zero-scaffolding gate: such a site emits byte-identical text.
    /// <para>BOTH container shapes are traversed, and both must be: a bind-time desugar hoists its activation
    /// into a <see cref="BoundSequence"/>, and <see cref="BoundImplicitSeries.Rewrap"/> keeps a multi-operand
    /// statement's SERIES outermost with that sequence as its FIRST member (kb/Work PB419), so a
    /// sequence-only recursion would miss a function-identifier written in an operand of a multi-operand
    /// CLOSE / FREE / INITIALIZE / INITIATE / OPEN / TERMINATE — the activation would carry an empty profile
    /// and the condition it stages would never be raised here. <c>QueryFor</c> below traverses the same two
    /// shapes for the same reason.</para></summary>
    private static BoundStatement StampActivators(BoundStatement node, EcCheckingProfile profile) =>
        profile.IsEmpty ? node
        : node switch
        {
            BoundSequence seq => new BoundSequence([.. seq.Steps.Select(st => StampActivators(st, profile))]),
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
        if (bound is IActivatingStatement or BoundSequence or BoundImplicitSeries or BoundActivationSite)   // the shapes a profile lands on
            bound = StampActivators(bound, ctx.EcState.Turn.ProfileAt(line));
        var enabled = new List<(string Ec, FileModel? File)>();
        void Query(IEnumerable<string> names, FileModel? file = null)
        {
            foreach (string n in names)
                if (ctx.EcState.Turn.Enabled(n, file?.CobolName, line))
                    enabled.Add((n, file));
        }

        // A desugar wrapper (a hoisted user-function activation / property-op sequence) is TRANSPARENT to
        // the family selection: the carrying statement keeps ITS families, and each hoisted
        // BoundCallProgram step contributes the EC-PROGRAM family — otherwise a checked COMPUTE that also
        // carries a function reference would silently lose its EC-SIZE wrap (the M2-UDF-1 review finding;
        // the property-op sequence had the same latent hole since DEVLOG 607).
        void QueryFor(BoundStatement node)
        {
            if (node is BoundSequence seq)
            {
                foreach (var step in seq.Steps) QueryFor(step);
                return;
            }
            // An implicit-statement series (ISO §14.9.20.4 GR3 and its six siblings — see BoundImplicitSeries) is
            // ONE written statement, so the >>TURN query is the UNION over its members: every implicit statement
            // inherits the same statement's enabled set, which is what §7.3.25.4 GR6 keys on the source LINE.
            if (node is BoundImplicitSeries series)
            {
                foreach (var member in series.Members) QueryFor(member);
                return;
            }
            // A statement carrying OPERAND activations (kb/Work PB892) keeps its own families, and its activations
            // contribute theirs — including the per-evaluation ones inside a condition or an operand, which no
            // statement-shaped step exposes to the cases below. Which activation kinds the windows hold is not
            // visible here, so every activation family is asked: the conservative wrap, as for the ambient
            // families (a name no raise site of this statement can produce binds a guard that never fires).
            if (node is BoundActivationSite site)
            {
                Query(ProgramNames);
                Query(ExternalNames);
                Query(FunctionActivationNames);
                Query(OoInvokeNames);
                QueryFor(site.Inner);
                return;
            }
            switch (node)
            {
                // The arithmetic statements — the structural marker, not a name list (kb/Work PB75): the statement's
                // own §14.7.5 shape (EmitArith) takes the EC-SIZE family.
                case IArithmeticStatement:
                    Query(SizeNames);
                    break;
                case BoundStringStmt:
                    Query(["EC-OVERFLOW-STRING"]);
                    break;
                case BoundUnstringStmt:
                    Query(["EC-OVERFLOW-UNSTRING"]);
                    break;
                case BoundOpen o:
                    foreach (var f in o.Files) Query(IoNames, f.File);
                    break;
                case BoundClose c:
                    foreach (var (file, _) in c.Files) Query(IoNames, file);
                    break;
                case BoundUnlock ul: Query(IoNames, ul.File); break;   // §14.9.47: UNLOCK is an I-O operation
                case BoundRead rd: Query(IoNames, rd.File); break;
                case BoundWrite wr: Query(IoNames, wr.File); break;
                case BoundRewrite rw: Query(IoNames, rw.File); break;
                case BoundKeyedRead k: Query(IoNames, k.File); break;
                case BoundKeyedWrite k: Query(IoNames, k.File); break;
                case BoundKeyedRewrite k: Query(IoNames, k.File); break;
                case BoundKeyedDelete k: Query(IoNames, k.File); break;
                // DELETE FILE (§14.9.10 F2, 2023) was the ONE I-O statement missing from this enumeration —
                // no (EC-I-O, file) pair, no __IoCheckEc mask, so GR20 b)'s enabled EC never set even after
                // the onExceptionHandled fix threaded through (kb/Work PB141).
                case BoundKeyedDeleteFile k: Query(IoNames, k.File); break;
                case BoundKeyedStart k: Query(IoNames, k.File); break;
                case BoundInvoke or BoundInvokeUniversal:
                    Query(OoInvokeNames);   // §14.9.23.4 GR5 / GR7b
                    break;
                // CA37 is PRECISE: EC-FLOW-SEARCH can only arise from a capacity SET (§14.9.39.4 GR31), which is
                // one bound node, so the guard binds exactly there. Its twin EC-BOUND-TABLE-LIMIT is NOT precise
                // — growth also happens on IMPLICIT receiving-reference growth, which renders inline — so it
                // takes the ambient tail gate below. The two are deliberately not merged.
                case BoundSetCapacity:
                    Query(SetCapacityNames);
                    break;
                // §14.9.18.4 GR6 — PRECISE, like BoundSetCapacity: the only statement that can set
                // EC-FLOW-GLOBAL-GOBACK is a GOBACK, and the emitter's run-time `__useActive` test is the raise
                // site. Without this arm the name never entered a statement's enabled set, so the runtime flag
                // stayed false and `>>TURN EC-FLOW-GLOBAL-GOBACK CHECKING ON` reached nothing (kb/Work PB409 —
                // the same shape as PB452's EC-PROGRAM-NOT-FOUND and PB326's report family).
                case BoundGoback:
                    Query(["EC-FLOW-GLOBAL-GOBACK"]);
                    break;
                // The three Report Writer verbs (kb/Work PB326). PRECISE like BoundSetCapacity: each condition's
                // ONLY raise site is a precondition of the engine call this node emits, so the guard binds
                // exactly here. Without these arms >>TURN EC-FLOW-REPORT / EC-REPORT-FILE-MODE / -ACTIVE /
                // -INACTIVE CHECKING ON wrapped nothing, the runtime's ...Checking flag stayed false, and the
                // condition could reach no declarative at all.
                case BoundInitiate:
                    Query(InitiateNames);
                    break;
                // The sort-merge flow conditions (kb/Work PB349). PRECISE: the ONLY raise sites are CobolSort's
                // RELEASE / RETURN statement entries, reached from exactly these bound nodes — the implicit USING
                // release and GIVING return use the unchecked primitives, because GR1 of each verb governs the
                // STATEMENT the program writes. Without these arms the names reached no statement's enabled set.
                // PB1036 adds the sort-merge STATEMENT conditions each verb's own rules name: RELEASE's record
                // size (§13.18.43.4 GR14 b) / GR19 b)) and the -ACTIVE rule for a RELEASE in an output procedure
                // / a RETURN in an input procedure (§14.9.40.4 GR10 / GR13, §14.9.24.4 GR8).
                case BoundRelease:
                    Query(["EC-FLOW-RELEASE", "EC-SORT-MERGE-ACTIVE", "EC-SORT-MERGE-RELEASE"]);
                    break;
                case BoundReturn:
                    Query(["EC-FLOW-RETURN", "EC-SORT-MERGE-RETURN", "EC-SORT-MERGE-ACTIVE"]);
                    break;
                // The SORT / MERGE statements themselves (kb/Work PB1036). PRECISE: every raise is CobolSort's, at
                // the point the verb's rule names — -ACTIVE at statement start (a nested SORT/MERGE, §14.9.40.4
                // GR10 / GR13, §14.9.24.4 GR8), -FILE-OPEN before each USING / GIVING transfer (§14.9.40.4 GR9,
                // §14.9.24.4 GR7 / GR12), -RELEASE in the implicit USING release (§14.9.40.4 GR12 b), §14.9.24.4
                // GR7 b)), and MERGE's -SEQUENCE (§14.9.24.4 GR6). A name the statement's shape cannot raise is not
                // asked (a SORT with no USING releases nothing implicitly).
                // ⛔ AND THE EC-I-O FAMILY PER USING / GIVING FILE: the implicit transfers perform as-if OPEN / READ /
                // WRITE / CLOSE statements "such that any applicable USE procedures are executed" (§14.9.40.4
                // GR12 / GR15, §14.9.24.4 GR7 / GR12), and each renders SequentialIoEmitter.EmitUseHook, whose
                // EC-I-O raise reads THIS statement's per-file mask. With no arm here the mask was always 0, so
                // `>>TURN EC-I-O CHECKING ON` over a SORT USING a missing file ran no EC-I-O declarative and set
                // no EXCEPTION-STATUS, and PB993's `__verbRule` disposition was unreachable (kb/Work PB1036).
                case BoundSort so:
                    Query(so.Using.Count > 0 ? SortUsingNames : SortProcedureNames);
                    foreach (var f in so.Using.Concat(so.Giving)) Query(IoNames, f);
                    break;
                case BoundMerge mg:
                    Query(MergeNames);
                    foreach (var f in mg.Using.Concat(mg.Giving)) Query(IoNames, f);
                    break;
                case BoundGenerate:
                    Query(GenerateNames);
                    break;
                case BoundTerminate:
                    Query(TerminateNames);
                    break;
                // The SEARCH range conditions (§14.9.37.4 GR4/GR6/GR9) RAISE via BoundSearch's own bound
                // Check* flags — this arm exists so the statement carries the wrapper's AMBIENT context
                // (kb/Work R14): the emitted 2-argument Set calls in EmitSearchScan pick up the §15.32.3 r2 /
                // §15.30.3 r2 pair from it. Before, a WITH LOCATION no-match SEARCH answered 63 spaces.
                case BoundSearch:
                    Query(["EC-RANGE-SEARCH-INDEX", "EC-RANGE-SEARCH-NO-MATCH"]);
                    break;
                // CONTINUE AFTER (§14.9.9.4 GR1 — kb/Work PB138 fixed the §14.9.8.4 miscite, which is
                // COMPUTE's clause): the raise is CobolTiming's — bound through
                // BoundContinueAfter.CheckLessThanZero — and 2-argument; the arm supplies the ambient pair
                // exactly like SEARCH (kb/Work R14).
                case BoundContinueAfter:
                    Query(["EC-CONTINUE-LESS-THAN-ZERO"]);
                    break;
                case BoundCallProgram call:
                    Query(ProgramNames);
                    Query(ExternalNames);   // §14.9.4.4 GR3e — the CALL is the EC-EXTERNAL raise point (§14.8.4)
                    // A USER-FUNCTION activation is a DIFFERENT raise: a locate miss is EC-FUNCTION-NOT-FOUND
                    // (§8.4.3.2.4 GR6b), not EC-PROGRAM-NOT-FOUND, and GR6f sends it to "any declarative … that
                    // is associated with that exception condition". Without this Query the name never entered
                    // the statement's enabled set, so >>TURN EC-FUNCTION-NOT-FOUND CHECKING ON wrapped nothing
                    // and the condition could reach no declarative at all (kb/Work PB233).
                    if (call.IsFunction) Query(FunctionActivationNames);
                    // PRECISE, like BoundSetFunctionAddress: only an activation THROUGH a function-pointer can find
                    // the pointer NULL (§8.4.3.2.4 GR6c; kb/Work PB847 added the raise site and this query together).
                    if (call.IsFunction && call.IsPointerTarget) Query(["EC-FUNCTION-PTR-NULL"]);
                    break;
                case BoundCancel:
                    Query(ProgramNames);    // CANCEL raises no EC-EXTERNAL — external state persists (§14.9.5 GR8)
                    break;
                // ⛔ THE DEAD RAISE THIS ENUMERATION WAS MISSING (kb/Work PB452, measured before it was fixed:
                // `>>TURN EC-PROGRAM-NOT-FOUND CHECKING ON` over `SET pp TO ENTRY "NOSUCH"` with a matching
                // declarative printed only "AFTER SET"). EC-PROGRAM-NOT-FOUND was named ONLY in ProgramNames,
                // which was queried for BoundCallProgram / BoundCancel — so PtrEmitter.EmitSetEntry's
                // checking-gated §8.4.3.13 GR4 block could never be emitted: ecState.Info.Enabled never carried
                // the name for THIS node. PRECISE, like BoundSetCapacity: the §8.4.3.13 GR4 locate miss is the
                // only condition this node raises.
                case BoundSetEntry:
                    Query(["EC-PROGRAM-NOT-FOUND"]);
                    break;
                // The Format-8 twin (§8.4.3.12.4 GR4's locate miss and §14.9.39.4 GR14's signature screen) —
                // PRECISE for the same reason, and added WITH its raise site rather than after it (kb/Work PB452).
                case BoundSetFunctionAddress:
                    Query(["EC-FUNCTION-NOT-FOUND", "EC-FUNCTION-PTR-INVALID"]);
                    break;
                case BoundFree:
                    Query(["EC-STORAGE-NOT-ALLOC"]);   // §14.9.15 GR1c (nonfatal; Phase-4b inc 2)
                    break;
                // EC-RANGE-PERFORM-VARYING (fatal, §14.9.28.4 GR3): a PERFORM VARYING that initializes an index-name
                // from a non-positive FROM item raises it. Unlike the blanket ambient gates below, a PERFORM is ONE
                // identifiable node, so a PRECISE case (not a whole-statement gate) drives the FatalAmbientGates
                // wrapper — the emitted index-init check (ControlFlowEmitter) throws inside that try for USE-F3.
                case BoundInlinePerform { Control: PerformVarying { CheckIndexRange: true } }:
                case BoundOutOfLinePerform { Control: PerformVarying { CheckIndexRange: true } }:
                    Query(["EC-RANGE-PERFORM-VARYING"]);
                    break;
            }
            // ⛔ THE EC-SIZE FAMILY IS AMBIENT FOR EVERY OTHER STATEMENT TOO (kb/Work PB75). §14.7.5: the size error
            // condition "may occur as a result of … the evaluation of an arithmetic expression" — a condition, a
            // function argument, a subscript, an INVOKE argument all render inline — and without a SIZE ERROR
            // phrase the level-3 EC-SIZE-* "is set to exist, and processing proceeds as specified in 14.6.13.1.3".
            // The raise sites are unconditional (CobolSizeError, a CobolFatalException), so the guard around a
            // size-error-free statement is harmless; an ARITHMETIC statement took its family above and EmitArith
            // owns its handling — the emitter's generic guard skips it (IArithmeticStatement), so the family is not
            // queried twice here. `IF 10 ** 100000 > 5` under STANDARD-DECIMAL was an unhandled stack trace.
            if (node is not IArithmeticStatement) Query(SizeNames);
            // EC-ARGUMENT-FUNCTION rides any intrinsic-bearing statement (the ambient statement gate — the
            // intrinsic renders inline inside expressions, so the guard wraps the STATEMENT).
            if (ctx.EcState.Turn.Enabled("EC-ARGUMENT-FUNCTION", null, line) && ContainsIntrinsic(node))
                enabled.Add(("EC-ARGUMENT-FUNCTION", null));
            // EC-ORDER-NOT-SUPPORTED (fatal, §15.85.4 r2) rides the SAME ambient gate, for the same reason:
            // FUNCTION STANDARD-COMPARE renders inline inside an arbitrary expression, so the guard wraps the
            // statement and CobolIntrinsics.StandardCompare consults the flag (kb/Work PB101 T7). Harmless around
            // a STANDARD-COMPARE-free statement — no other site sets it.
            if (ctx.EcState.Turn.Enabled("EC-ORDER-NOT-SUPPORTED", null, line) && ContainsIntrinsic(node))
                enabled.Add(("EC-ORDER-NOT-SUPPORTED", null));
            // The EC-LOCALE family (kb/Work PB64 T1; DESIGN-locale-facility §4.10) rides ambient per-statement gates:
            // EC-LOCALE-MISSING and EC-LOCALE-INVALID-PTR are PRECISE — only SET LOCALE (§14.9.39.4 GR24 / GR21) and a
            // NAMED IS LOCALE collating sequence at use (§8.2.1 — inline in a relation, a SORT key, a MAX/MIN …) can
            // raise them, and the named sequence is not one node kind, so MISSING rides any statement while INVALID-PTR
            // rides the SET; EC-LOCALE-INCOMPATIBLE (§8.8.4.2.11, L6) is an inline comparison outcome — any statement.
            // Each raise fires only at its site, so a guard around an unrelated statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-LOCALE-MISSING", null, line))
                enabled.Add(("EC-LOCALE-MISSING", null));
            if (node is BoundSetLocale && ctx.EcState.Turn.Enabled("EC-LOCALE-INVALID-PTR", null, line))
                enabled.Add(("EC-LOCALE-INVALID-PTR", null));
            if (ctx.EcState.Turn.Enabled("EC-LOCALE-INCOMPATIBLE", null, line))
                enabled.Add(("EC-LOCALE-INCOMPATIBLE", null));
            // EC-LOCALE-INVALID (§8.2.1 — incomplete locale content) rides the LOCALE intrinsics (T4: LOCALE-DATE/-TIME/
            // -TIME-FROM-SECONDS; T5 the case functions' LOCALE phrase; T6 the monetary operations), which render
            // inline — any intrinsic-bearing statement, like EC-ARGUMENT-FUNCTION — and, in a module WITH a CHARACTER
            // CLASSIFICATION (T5), ANY statement: a class test (§12.3.6.4 GR7b) is not an intrinsic-bearing statement
            // and can raise it at use (LocaleFacts.Require), as can a case function without a phrase (GR7a). Each raise
            // fires only at its site, so the guard around an unrelated statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-LOCALE-INVALID", null, line) && (ContainsIntrinsic(node) || ctx.Data.Classification is not null))
                enabled.Add(("EC-LOCALE-INVALID", null));
            // EC-LOCALE-SIZE (§13.18.40.5 r14 b; PB64 T6 — the ONE raise site is CobolLocaleEdit.Format's move of
            // the hypothetical item into the SIZE-declared item): any statement that stores into a format-2 item
            // can raise it — a MOVE, an arithmetic store, INITIALIZE, a VALUE-composed level-88 compare never
            // (reads don't edit). Wrapped conservatively (any statement in a checking-on region), the
            // EC-BOUND-OVERFLOW precedent: the raise fires only at its site, so the guard around a
            // locale-item-free statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-LOCALE-SIZE", null, line))
                enabled.Add(("EC-LOCALE-SIZE", null));
            // EC-DATA-CONVERSION (nonfatal, §15.19.4 r1/r3) rides any intrinsic-bearing statement too — FUNCTION
            // CONVERT sets it when an untranslatable character forces the substitution character; the ambient
            // gate records it while checking is enabled (harmless around a non-CONVERT intrinsic — no site sets it).
            if (ctx.EcState.Turn.Enabled("EC-DATA-CONVERSION", null, line) && ContainsIntrinsic(node))
                enabled.Add(("EC-DATA-CONVERSION", null));
            // EC-BOUND-OVERFLOW (nonfatal, §8.5.1.9.6 GR1) rides an ambient per-statement gate: a dynamic-capacity
            // table's implicit growth past its expected capacity records the last exception status while checking is
            // enabled. Wrapped conservatively (any statement in a checking-on region) — the raise site
            // (CobolDynTable.RefReceiving) fires ONLY on an actual dyn-table receiving grow-past-expected, so the
            // flag around a dyn-table-free statement is a harmless no-op (nonfatal, no site sets it). A precise
            // "references a dynamic table" filter is a documented future refinement.
            if (ctx.EcState.Turn.Enabled("EC-BOUND-OVERFLOW", null, line))
                enabled.Add(("EC-BOUND-OVERFLOW", null));
            // EC-FLOW-USE (fatal, §14.9.49.4 GR2 — kb/Work PB368) rides an ambient per-statement gate, and it has
            // to: GR2's subject is "a statement [that] raises an exception condition that would cause the
            // execution of a USE procedure that had previously been activated", and the set of statements that
            // can cause a USE procedure to be executed is every statement that can raise ANY condition with a
            // declarative — every I-O verb (GR3a/GR3b file and open-mode tiers), every RWCS verb (GR8 BEFORE
            // REPORTING), a RAISE, a CALL, and any statement whose inline raise site (subscript, ref-mod,
            // pointer, size error) reaches the GR3c–g Format-3 tiers. There is no node kind to key on, exactly
            // as for EC-BOUND-REF-MOD below; and the SYNTACTIC alternative — "a statement lexically inside
            // DECLARATIVES" — is wrong outright, because a declarative may PERFORM a paragraph anywhere in the
            // procedure division and GR2 says "during the EXECUTION of a USE procedure". The raise fires only
            // inside the generated __RunUse guard, so the flag around a statement that invokes no declarative
            // is a no-op.
            if (ctx.EcState.Turn.Enabled("EC-FLOW-USE", null, line))
                enabled.Add(("EC-FLOW-USE", null));
            // THE *-ARG-OMITTED CONDITION OF THIS ELEMENT'S KIND (kb/Work PB971 — §14.9.4.4 GR12 program,
            // §8.4.3.2.4 GR8 function, §14.9.23.4 GR10 method) rides an ambient per-statement gate for the reason
            // EC-BOUND-REF-MOD does: its raise site is the REFERENCE, rendered inline through the formal's guarded
            // root (OmittedFormalGuard) in any operand of any verb, so no node kind can key it. The guard raises
            // only when the argument was actually omitted, so the flag around a formal-free statement is a no-op.
            if (ctx.EcState.Turn.Enabled(ArgOmittedName, null, line))
                enabled.Add((ArgOmittedName, null));
            // EC-BOUND-REF-MOD (fatal, §8.4.3.3.4) rides an ambient per-statement gate: a reference modification
            // whose leftmost/length is out of range (or an unallowed zero-length) raises it while checking is
            // enabled. Wrapped conservatively (any statement in a checking-on region) — the raise fires only at an
            // actual out-of-range ref-mod evaluation, so the guard around a ref-mod-free statement is harmless (the
            // catch never fires). A precise ContainsRefMod filter is a documented follow-on.
            if (ctx.EcState.Turn.Enabled("EC-BOUND-REF-MOD", null, line))
                enabled.Add(("EC-BOUND-REF-MOD", null));
            // EC-DATA-PTR-NULL / EC-BOUND-PTR (fatal, §13.18.5.4 GR3/GR4) and EC-SIZE-ADDRESS (fatal, §14.9.39
            // Format 10 GR19) ride ambient per-statement gates for the same reason EC-BOUND-REF-MOD does: a BASED
            // dereference renders INLINE through the generated bridge property that aliases CobolPtr.Deref, so it
            // is not one node kind that a precise QueryFor case could match. Wrapped conservatively — any
            // statement in a checking-on region — which is harmless, because each raise fires only at an actual
            // pointer operation and the guard around a pointer-free statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-DATA-PTR-NULL", null, line))
                enabled.Add(("EC-DATA-PTR-NULL", null));
            if (ctx.EcState.Turn.Enabled("EC-BOUND-PTR", null, line))
                enabled.Add(("EC-BOUND-PTR", null));
            if (ctx.EcState.Turn.Enabled("EC-SIZE-ADDRESS", null, line))
                enabled.Add(("EC-SIZE-ADDRESS", null));
            // EC-RANGE-PTR (fatal, §14.9.39.4 GR20) joins its three neighbours for the same reason: the raise
            // fires inside CobolPtr's displacement site, which a SET pointer statement reaches through the same
            // inline render as the three above, so the gate around a pointer-free statement never catches
            // anything. GR19 and GR20 are TWO rules over one statement — the amount's value and the result's
            // range — and they need two gates, or turning checking on for one silently arms the other's raise
            // (kb/Work PB465).
            if (ctx.EcState.Turn.Enabled("EC-RANGE-PTR", null, line))
                enabled.Add(("EC-RANGE-PTR", null));
            // EC-BOUND-SUBSCRIPT (§8.4.2.3.4 GR2) and EC-BOUND-ODO (§13.18.38.4 GR7) are ambient for the same
            // reason: a subscripted reference renders inline through CobolTable.At and an ODO group extent
            // through CobolTable.OdoExtent, neither of which is a distinguishable node kind at the statement
            // level. The guard around a table-free statement never catches anything.
            if (ctx.EcState.Turn.Enabled("EC-BOUND-SUBSCRIPT", null, line))
                enabled.Add(("EC-BOUND-SUBSCRIPT", null));
            if (ctx.EcState.Turn.Enabled("EC-BOUND-ODO", null, line))
                enabled.Add(("EC-BOUND-ODO", null));
            // EC-RANGE-INDEX (fatal, §13.18.38.4 GR2) joins them for the same reason and with the same argument.
            // §13.18.38.4 GR2 names THREE statements that may create a value for an index — "An index may be
            // modified only by a PERFORM VARYING statement, a SEARCH statement, and a SET statement" — and the
            // §14.9.39.4 GR2 a) 1. b / 2. a / 3. b and GR4 a) limits are that same limit. A precise QueryFor case
            // per node kind would be a hand-maintained list of every emitter path that stores into an index
            // (SetEmitter's store/augment pair alone is ridden by three verbs), and the next such path would
            // silently not be checkable — the EC-DATA-INCOMPATIBLE node-kind list is exactly how kb/Work PB230
            // happened. The raise fires only inside CobolIndex, so the flag around an index-free statement is a
            // no-op. kb/Work PB459.
            if (ctx.EcState.Turn.Enabled("EC-RANGE-INDEX", null, line))
                enabled.Add(("EC-RANGE-INDEX", null));
            // EC-BOUND-TABLE-LIMIT (§14.9.39.4 GR30) is ambient, unlike its CA37 twin: a dynamic table grows
            // both from an explicit capacity SET and from an IMPLICIT receiving reference, and the latter renders
            // inline through CobolDynTable.RefReceiving with no statement-level node of its own.
            if (ctx.EcState.Turn.Enabled("EC-BOUND-TABLE-LIMIT", null, line))
                enabled.Add(("EC-BOUND-TABLE-LIMIT", null));
            // EC-STORAGE-NOT-AVAIL (§14.9.2.4 ALLOCATE GR / CobolDynString's growth sites) and EC-RANGE-INVALID
            // (§14.9.13.4 — a THRU range with the ends reversed; CobolString.ThruMember) raise from RUNTIME
            // sites that render inline with no statement-level node, exactly like the gates above — ambient,
            // conservative, harmless around statements that never reach the raise. They ride the tail so the
            // 2-argument Set at those sites picks up the ambient (statement, location) pair (kb/Work R14).
            if (ctx.EcState.Turn.Enabled("EC-STORAGE-NOT-AVAIL", null, line))
                enabled.Add(("EC-STORAGE-NOT-AVAIL", null));
            if (ctx.EcState.Turn.Enabled("EC-RANGE-INVALID", null, line))
                enabled.Add(("EC-RANGE-INVALID", null));
            // EC-DATA-NOT-FINITE (fatal, §14.6.13.2 item 3) rides an ambient per-statement gate: any non-exempt read
            // of a NaN/±Inf standard-float sending operand raises it while checking is enabled. Wrapped conservatively
            // (any statement in a checking-on region) — the always-emitted CobolFloat.Sending wrap at the two float
            // read chokepoints raises only on an actual non-finite float read, so the guard around a float-free
            // statement is harmless. A precise "references a float sending operand" filter is a documented follow-on.
            if (ctx.EcState.Turn.Enabled("EC-DATA-NOT-FINITE", null, line))
                enabled.Add(("EC-DATA-NOT-FINITE", null));
            // EC-DATA-OVERFLOW (fatal, §14.9.25.4 GR6 d)4.a) is MOVE-only: a MOVE whose finite algebraic value
            // overflows a single-precision float receiver to ±Inf. A precise "has a single-float receiver" filter is
            // a documented follow-on (like the ContainsRefMod note); MOVE CORRESPONDING expands to BoundMove steps
            // which this sees through the BoundSequence recursion.
            if (node is BoundMove && ctx.EcState.Turn.Enabled("EC-DATA-OVERFLOW", null, line))
                enabled.Add(("EC-DATA-OVERFLOW", null));
            // EC-RANGE-INSPECT-SIZE (fatal, §14.9.22.4 GR14/GR15/GR22 — kb/Work PB1126) is INSPECT's own: the three
            // rules that set it are written over INSPECT's operands and nothing else, and its only raise sites are
            // CobolInspect.Replace / Convert, so the node kind IS the rule's scope (unlike the ambient families above,
            // whose raise sites render inline into any verb).
            if (node is BoundInspect && ctx.EcState.Turn.Enabled("EC-RANGE-INSPECT-SIZE", null, line))
                enabled.Add(("EC-RANGE-INSPECT-SIZE", null));
            // EC-DATA-INCOMPATIBLE (fatal, §14.6.13.2) rides an AMBIENT per-statement gate, exactly like its own
            // sibling EC-DATA-NOT-FINITE above — the two are rules 3 and 2/4 of ONE clause about one subject, the
            // content of a sending operand that is not valid, and they get one shape.
            // ⛔ THIS USED TO READ `node is BoundMove &&`, AND THAT NODE-KIND TEST WAS THE WHOLE DEFECT
            // (kb/Work PB230). It scoped the family to rule 4 — "a numeric-edited data item is the sending operand
            // of a de-editing MOVE" — while rule 2 is not MOVE-specific at all: "When the content of a numeric
            // sending item that is not described with a standard floating-point usage is REFERENCED DURING THE
            // EXECUTION OF A STATEMENT and the content of that sending operand would evaluate to false in a
            // numeric class condition … an EC-DATA-INCOMPATIBLE exception condition is set to exist". So ADD,
            // SUBTRACT, MULTIPLY, DIVIDE, COMPUTE, every CORRESPONDING form (§14.7.6's last paragraph aggregates
            // the implied statements' condition), every comparison, DISPLAY, STRING, SORT — and a plain numeric
            // MOVE — were not even made CHECKABLE, so a program that explicitly asked to be checked was not.
            // A node-kind list is what produced that, so there is no node-kind list any more: the raise fires
            // only at an actual windowed sending read (CobolNum.ParseImageSending / SendingImage, and
            // CobolEdit.DeEdit for rule 4), so the guard around a statement with no such read is a no-op —
            // the same conservative-wrap argument every ambient family above rests on.
            if (ctx.EcState.Turn.Enabled("EC-DATA-INCOMPATIBLE", null, line))
                enabled.Add(("EC-DATA-INCOMPATIBLE", null));
        }
        QueryFor(bound);

        if (enabled.Count == 0) return bound;
        // A sequence's steps can re-contribute a family (two hoisted activations ⇒ ProgramNames twice) —
        // the checked wrapper carries each (name, connector) once.
        if (bound is BoundSequence or BoundImplicitSeries or BoundActivationSite) enabled = enabled.Distinct().ToList();
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

    /// <summary>Does a bound statement (or a statement nested inside it) contain an intrinsic-function call — the
    /// EC-ARGUMENT-FUNCTION wrap test? Checks THIS statement's own operand/expression shapes via <see cref="DirectIntrinsic"/>,
    /// then recurses EVERY nested statement through the generated <see cref="BoundStatementTree.StatementChildren"/>
    /// (PHASE-07 Step 6h) — so the walk is now TOTAL over containers (the former hand-list missed SEARCH/keyed/WRITE/…
    /// phrase bodies). A wrap around a statement whose intrinsic argument is in fact valid is a no-op, so a wider walk
    /// is conservative — never mis-executes.</summary>
    private static bool ContainsIntrinsic(BoundStatement s) =>
        DirectIntrinsic(s) || s.StatementChildren().Any(ContainsIntrinsic);

    /// <summary>The intrinsic in THIS statement's OWN operands/expressions/conditions (not its nested statements —
    /// those are the recursion's job via <see cref="BoundStatementTree.StatementChildren"/>).
    ///
    /// <para>⛔ <b>THIS WAS A HAND-WRITTEN SWITCH OVER ~17 STATEMENT KINDS WITH <c>_ => false</c>, AND THE DEFAULT
    /// ARM WAS A SILENT WRONG ANSWER</b> (fix-queue PB26). <b>ISO §15.3 item 14 attaches EC-ARGUMENT-FUNCTION to
    /// the FUNCTION REFERENCE</b> — "If the evaluation of an argument results in an incorrect value … the
    /// EC-ARGUMENT-FUNCTION exception condition is set to exist" — with no statement-kind qualification anywhere in
    /// it. So the ambient checking gate must be emitted wherever a function reference is, and the switch made it
    /// depend on whether someone had remembered to add an arm: <c>FUNCTION LOG10(0)</c> raised in COMPUTE, MOVE,
    /// DISPLAY and IF, and was SILENT in STRING and every other unlisted kind. Measured, not reasoned.</para>
    ///
    /// <para>The list is now a STRUCTURE (CLAUDE.md rule 5): <see cref="BoundStatementTree.OwnValueParts"/> is
    /// generated from the semantic model by reading every property of every statement leaf, so a statement kind
    /// added tomorrow is covered without an edit here — and <c>EcArgumentFunctionGateDriftTests</c> fails the build
    /// if that ever stops being true.</para></summary>
    private static bool DirectIntrinsic(BoundStatement s) => s.OwnValueParts().Any(PartHasIntrinsic);

    /// <summary>One generated value part → does it carry an intrinsic call? The four value hierarchies each have
    /// their own walker below; a part of any other shape (a <c>Place</c>, a receiver) carries no expression and
    /// answers false.</summary>
    private static bool PartHasIntrinsic(object part) => part switch
    {
        BoundExpr e => ExprHasIntrinsic(e),
        BoundCondition c => CondHasIntrinsic(c),
        BoundOperand o => OpHasIntrinsic(o),
        BoundBoolExpr b => BoolExprHasIntrinsic(b),
        _ => false,
    };

    private static bool OpHasIntrinsic(BoundOperand op) => op switch
    {
        BoundComputedOperand c => ExprHasIntrinsic(c.Expr),
        BoundBoolOperand b => BoolExprHasIntrinsic(b.Expr),
        _ => false,
    };

    /// <summary>The intrinsic walk over the boolean channel (ISO §8.8.2). Boolean-op operands are boolean items/
    /// literals today — intrinsic operands inside a boolean expression are named residue — but the walk is TOTAL
    /// from day one (the DEVLOG-607 rule: a new node must register in every exhaustive walk).</summary>
    private static bool BoolExprHasIntrinsic(BoundBoolExpr e) => e switch
    {
        BoundBoolBinary b => BoolExprHasIntrinsic(b.Left) || BoolExprHasIntrinsic(b.Right),
        BoundBoolNot n => BoolExprHasIntrinsic(n.Operand),
        BoundBoolShift s => BoolExprHasIntrinsic(s.Operand) || ExprHasIntrinsic(s.Count),   // the count is a numeric expr
        BoundBoolCall => true,   // a boolean-result function reference (kb/Work PB68)
        _ => false,
    };

    private static bool ExprHasIntrinsic(BoundExpr e) => e switch
    {
        BoundIntrinsicCall => true,
        BoundBinary b => ExprHasIntrinsic(b.Left) || ExprHasIntrinsic(b.Right),
        BoundNegate n => ExprHasIntrinsic(n.Operand),
        BoundPower p => ExprHasIntrinsic(p.Base) || ExprHasIntrinsic(p.Exp),
        _ => false,
    };

    private static bool CondHasIntrinsic(BoundCondition c) => c switch
    {
        BoundRelational r => OpHasIntrinsic(r.Left) || OpHasIntrinsic(r.Right),
        BoundLogical l => l.Operands.Any(CondHasIntrinsic),
        BoundNot n => CondHasIntrinsic(n.Operand),
        BoundSignCondition s => ExprHasIntrinsic(s.Expr),
        BoundBooleanCondition bc => BoolExprHasIntrinsic(bc.Expr),
        _ => false,
    };
}
