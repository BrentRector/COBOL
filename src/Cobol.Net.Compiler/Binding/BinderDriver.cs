// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Binding.Model;
using CobolNet.Binding.Passes;
using CobolNet.Binding.Procedure;
using CobolNet.Common;
using CobolNet.Frontend.Generated;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// THE Binder phase (rearch PHASE-06 Step 2): binds a whole compilation group to an immutable
/// <see cref="BoundCompilation"/>, so the driver's Phase 2 is literally <c>Bind → VersionConformancePass → Emit</c>
/// and the emitter consumes the bound result instead of orchestrating binding. This class owns the formerly-hidden
/// second pass pipeline (the binder half of the once-fused bind+emit run-unit entry; the emit half is
/// <c>ProgramEmitter.Emit</c> since P7 Step 9n): TurnState → unit/class collection →
/// OO data/body binding → two-phase per-unit binding (ALL data before ANY procedure — the M2-UDF-1
/// forward-reference enabler) → the middle-end data-model passes (compiler-temp re-sync → UsageCollection →
/// image marking → OO harmonize → StorageForm) → the EC gate → file-connector registry-key qualification.
/// <para>The OO bind bodies live on <see cref="Compiler.Oo.OoDriver"/> (P9 R1 — a real binder collaborator;
/// the former emitter-hosted <c>IOoBindHost</c> seam is deleted). They only mutate binder state, never emit.</para>
/// </summary>
internal sealed class BinderDriver
{
    /// <summary>Bind the WHOLE compilation group in <paramref name="tree"/> under the targeted EDITION
    /// (<paramref name="edition"/> — bind-time rejection diagnostics accumulate there; the driver fails the
    /// compile when any exist, BEFORE emit). <paramref name="turnEvents"/> are the frontend's <c>&gt;&gt;TURN</c>
    /// directive events (ISO §7.3.25) — they build the group's compile-time TurnState (EC deep-dive D10);
    /// null/empty means the GR1 default, EC-ALL CHECKING OFF.</summary>
    public BoundCompilation Bind(Core.CompilationUnitContext tree, EditionContext edition,
        Frontend.Preprocessor.DirectiveResults? directives = null)
    {
        BindPipeline.ValidateFullChainOnce();   // the startup DAG assert over resolve prefix + group tail
        // The frontend's directive outputs travel as ONE record (kb/Work PB65); absent ⇒ every directive's default.
        // §14.9.28.4 GR14's implicit PUSH ALL / POP ALL around every exception-checking PERFORM's handlers are
        // already replayed into every event timeline: the front end places them, because the conditional-compilation
        // driver's state needs them too (kb/Work PB1004, PB1066 — Frontend.Parse).
        directives ??= Frontend.Preprocessor.DirectiveResults.None;
        var turnEvents = directives.TurnEvents;
        var refModZlEvents = directives.RefModZeroLengthEvents;
        var flagEvents = directives.FlagEvents;
        var cobolWordsMap = directives.CobolWordsMap;

        // The LEVEL-NUMBER screen (ISO §13.18.33.3) — run FIRST, before any binding. It is a pure SYNTAX rule
        // (a token plus the entry's section ancestry) and needs nothing the binder produces, while a level-number
        // outside its section's permitted set makes the storage tree it heads meaningless: `78 K VALUE 5.` used to
        // bind as a memberless group nested under the preceding entry and survive to a RUN-time abort. Screening
        // first means the user sees the level-number error instead of that cascade, and no bind-time failure on the
        // malformed structure can preempt it. The three sibling passes further down run POST-bind only because they
        // consume the bound model; this one has no such reason to wait. kb/Work PB485.
        global::CobolNet.Validation.LevelNumberPass.Run(tree, edition);
        // ISO §5.5 1) — every integer-n is NONZERO unless an associated rule says otherwise (kb/Work PB859). Pre-bind
        // for the level-number screen's reason: a pure syntax rule over the raw tree, and a zero OCCURS bound left to
        // the binder used to reach Roslyn as CS0029 in generated C#.
        global::CobolNet.Validation.IntegerOperandPass.Run(tree, edition);
        // WHERE a directive may be written, for the placement rules that need the parse tree (kb/Work PB1005, PB1065,
        // PB1377, PB1378): PUSH ALL / POP ALL (§7.3.22.3 SR3, §7.3.20.3 SR3), FLAG-02 / FLAG-14 (§7.3.14.3 SR1,
        // §7.3.15.3 SR1) between clauses and statements; LEAP-SECOND / PROPAGATE outside every compilation unit
        // (§7.3.17.3 SR1, §7.3.21.3 SR1); a PUSH/POP naming one inherits its rule (§7.3.20.3 SR2, §7.3.22.3 SR2). Each
        // rule is data on the directive's constructs.json row; a pure position rule over the tree and the directive
        // sites, with no tree walk unless the source has a site the pass judges.
        global::CobolNet.Validation.DirectivePlacementPass.Run(tree, directives.DirectiveSites, edition);

        // The group's compile-time TurnState (ISO §7.3.25; deep-dive D10) — built BEFORE binding so every unit's
        // statement binder folds the same source-ordered directive events (GR6: checking spans the compilation
        // group). Name/edition validation happens here (SR2 + the 2023-only families).
        var turn = TurnState.Build(turnEvents, edition);
        // The group's compile-time REF-MOD-ZERO-LENGTH resolution (ISO §7.3.23) — the per-line zero-length
        // allowance fold every ReferenceResolver queries when building a ref-mod Place (§8.4.3.3.4 item 5c).
        var refModZl = RefModZeroLengthState.Build(refModZlEvents);
        // The group's compile-time >>PROPAGATE resolution (ISO §7.3.21; kb/Work PB1119) — folded once per source
        // element below (program units in BindUnitData, methods in the roster loop).
        var propagate = PropagateState.Build(directives.PropagateEvents);

        var (units, classes, table) = CollectUnits(tree, edition,
            cobolWordsMap ?? CobolNet.Editions.CobolWordsMap.Empty);
        var session = new BindSession
        {
            Turn = turn, OoClasses = table, Edition = edition, RefModZeroLength = refModZl, Propagate = propagate,
            DirectiveSites = directives.DirectiveSites,
            CobolWords = cobolWordsMap ?? CobolNet.Editions.CobolWordsMap.Empty,
            Retypes = tree.TokenRetypes,
            LeapSecond = LeapSecondState.Build(directives.LeapSecondEvents),
            CompilationVariables = directives.CompilationVariables,
        };
        var oo = new OoDriver(session);   // P9 R1 — the OO bind driver is a binder collaborator, not an emitter seam
        foreach (var iface in table.Interfaces) oo.BindInterfaceData(iface);   // prototype formals (§10.6.2 SR4)
        foreach (var cls in classes) oo.BindClassData(cls);   // ALL signatures before ANY body (D1 pass-1)
        // §14.9.23.4 GR7 c)'s METHOD-side half, folded once now that every method symbol exists — at the method's
        // PROCEDURE DIVISION HEADER, the program twin's query point (TurnState.EnabledAtHeader; kb/Work PB1381).
        // See OoMethodSymbol.OoUniversalCheckingHere for why this is bind-time.
        foreach (var cls in table.Classes)
            foreach (var m in cls.Methods.Concat(cls.FactoryMethods))
                // A PROPERTY accessor SYNTHESIZED from a PROPERTY clause (§13.18.42) has no METHOD-ID in source
                // and therefore no method context at all — reading `m.Ctx.Start` on one is a NullReferenceException
                // that takes down the whole bind, which is exactly what it did to oo_property/oo_property_ref.
                // Its enclosing CLASS-ID line is the right query point: the accessor is part of that definition
                // and has no source position of its own to disagree with. A written method with no procedure
                // division answers at its METHOD-ID line (it has no header for a TURN to precede).
            {
                m.OoUniversalCheckingHere = turn.EnabledAtHeader(
                    "EC-OO-UNIVERSAL", m.Ctx?.procedureDivision(), (m.Ctx?.Start ?? cls.Ctx.Start).Line);
                // §14.8.4.1's ACTIVATED-method half (kb/Work PB1138): the EC-EXTERNAL conditions enabled "before the
                // Environment division" of the method — its first division after METHOD-ID, the fold a program unit
                // takes at its own (BindUnitData). A synthesized PROPERTY accessor has no divisions; its CLASS-ID line
                // stands in, exactly as for the EC-OO-UNIVERSAL half above.
                int divLine = m.Ctx is { } mc
                    ? (mc.environmentDivision()?.Start ?? mc.dataDivision()?.Start ?? mc.procedureDivision()?.Start ?? mc.Stop).Line
                    : cls.Ctx.Start.Line;
                m.ExternalCheckMaskHere = ExternalMaskAt(turn, divLine);
                // §7.3.21.4 GR1/GR3 — automatic propagation for the METHOD (kb/Work PB1119), folded at its METHOD-ID
                // line; §7.3.21.3 SR1 keeps the directive out of the class, so that is also the CLASS-ID's state.
                m.AutomaticPropagationHere = propagate.IsOnAt((m.Ctx?.Start ?? cls.Ctx.Start).Line);
            }
        // The group-level Describe gate reaches the class halves too (kb/Work PB1138): a method activation registers
        // its factory's or object's external descriptions (§14.9.23.4 GR7 d)), so the same zero-scaffolding switch the
        // program units carry decides whether a class half emits them at all.
        bool externalDescribe = Procedure.EcBinder.ExternalNames.Any(turn.AnyEnabledFor);
        bool odoReferenceChecking = turn.AnyEnabledFor("EC-BOUND-ODO");   // §13.18.38.4 GR7 (kb/Work PB1268)
        foreach (var cls in classes)
        {
            cls.Data.ExternalDescribe = externalDescribe;
            cls.FactoryData.ExternalDescribe = externalDescribe;
            cls.Data.OdoReferenceChecking = odoReferenceChecking;
            cls.FactoryData.OdoReferenceChecking = odoReferenceChecking;
        }

        OoConformance.ValidateInterfaceInheritance(table, edition);   // §11.6.3 SR5 — an inheriting interface conforms to all it inherits (kb/Work PB1502)
        OoConformance.ValidateOverrideSignatures(table, edition);   // §9.3.8.2 — after all formals resolve (slice 3a)
        OoConformance.ValidatePropertyAccessorPairs(table, edition);   // §8.4.3.9.3 SR7 — get RETURNING = set USING, over the formals just resolved
        var ooAdapters = OoConformance.ValidateImplements(table, edition);   // §9.3.11 via §9.3.8.2.3 (D-I1 — the binder is the authority; returns the covariant adapters)
        // TWO-PHASE binding (M2-UDF-1 key enabler): EVERY unit's DATA division binds before ANY procedure body
        // binds — a class's method bodies (below) as well as a program unit's (the ProcedureBinding group pass) —
        // so a function-identifier reference resolves the callee's RETURNING / USING signatures even when the
        // FUNCTION-ID unit FOLLOWS the caller in the compilation group (§8.4.3.2.4 GR1 — the caller-side temporary
        // takes the callee's RETURNING description; the same forward-reference discipline OoClassTable D1 gives
        // typed object references).
        foreach (var unit in units) BindUnitData(unit, session);
        // The group's REPOSITORY resolution tables (§12.3.8.4 GR10 / GR11), built once every DATA division is
        // bound and BEFORE the first body: a CLASS definition's REPOSITORY reaches its methods (§12.3.4 GR1 — "apply
        // to each directly or indirectly contained source unit"), and until kb/Work PB1100 the class bodies bound
        // first, against no table, so every user-function reference in a method drew COBOLNET1505.
        session.Repository = BuildGroupRepository(units, classes, session);
        foreach (var cls in classes) oo.BindClassBody(cls);

        // The whole-group middle-end (P6 Steps 3–4): the DECLARED manifest — ProcedureBinding →
        // UsageCollectionPass → StorageFormPass → VersionConformancePass (the NAMED terminal pass, the SOLE
        // edition gate; each pass's doc lives with its body; the ORDER lives in BindPipeline.GroupTail,
        // DAG-validated against the resolve prefix above). Once the tail completes, the edition sink carries
        // EVERY edition diagnostic — the driver's CheckOnly verdict needs nothing beyond this Bind.
        var ctx = new GroupBindContext(tree, units, classes, oo.InterfaceData, session);
        foreach (var pass in BindPipeline.GroupTail())
        {
            RequireAll(ctx, pass);      // watermark gate: the prerequisite RAN on every binder (P6 Step 6)
            pass.Run(ctx);
            foreach (var d in ctx.AllBinders()) d.MarkProduced(pass.Produces);
        }

        // The migration-flagging pass (ISO §7.3.14 FLAG-02 / §7.3.15 FLAG-14) — a SIBLING to the terminal
        // VersionConformancePass, run right after it: it is an orthogonal axis (directive-state-driven, always a
        // Warning, fires regardless of --std), so it is NOT a GroupTail manifest pass. A no-op (no parse-tree walk)
        // when the source carries no >>FLAG directive — the zero-overhead invariant.
        global::CobolNet.Validation.FlagConformancePass.Run(ctx, FlagState.Build(flagEvents), edition);

        // The expression-formation pass (ISO §8.8.1.2 Table 3 / §8.8.2 Table 4) — a THIRD sibling on the same
        // "orthogonal axis ⇒ separate pass" precedent. It is not edition gating (the tables have no introducedIn:
        // an invalid symbol pair is invalid in 1985, 2002, 2014 and 2023 alike) and not directive-state flagging,
        // so neither existing pass's charter covers it. It walks the RAW parse tree once, never once per binding
        // site; the rule itself is the shared frontend ArithmeticFormationRules, which the compile-time
        // expression evaluator invokes on its own trees. kb/Work PB158.
        global::CobolNet.Validation.ExpressionFormationPass.Run(tree, edition);

        // The literal-syntax pass (ISO §8.3.3.2.3 SR1/SR6, §8.3.3.4.3 SR1, §8.3.3.5.3 SR1/SR5) — a sibling on the
        // same footing: the length and hexadecimal-grouping rules are properties of the literal TOKEN, whatever
        // position it is written in, so they are asked once per token of the raw tree rather than once per
        // consuming funnel (which is how VALUE / CONSTANT / ALL / concatenation-operand literals escaped them).
        // kb/Work PB1393.
        global::CobolNet.Validation.LiteralScreenPass.Run(tree, edition);

        // The declined-optional-element pass (ISO Annex A.4.1) — a THIRD sibling on the same footing: it answers
        // "does this implementation claim support for this optional module", which is orthogonal to both the
        // edition axis (VersionConformancePass) and the directive axis (FlagConformancePass). It refuses the
        // DATA DIVISION / I-O-CONTROL surfaces of the declined A.4.14 VALIDATE and A.4.3 commit-and-rollback
        // facilities by name (COBOLNET1708/1709); the declined STATEMENTS keep their own bind-time
        // recognize-and-warn arm, and the declined modules' EXCEPTION-NAMES are refused in the one written-name
        // funnel (EcNameResolution → COBOLNET1710). A no-op below COBOL-2002.
        global::CobolNet.Validation.DeclinedFacilityPass.Run(ctx, edition);

        // The closed-general-format pass (ISO §4.2.2) — a FOURTH sibling on the same footing, and the one that
        // ends the grammar's last total sink. `genericClause : IDENTIFIER (IDENTIFIER|literal)*` was reached from
        // six sites spanning eight closed general formats and swallowed any word run the format did not define,
        // at every edition and every strictness; it now spells `unrecognizedClause` at every site and this pass
        // refuses each instance by name, naming the format and its § from ClosedFormats.ByContext (COBOLNET1941
        // for §13.16.2, 1970 for the clause-list formats, 1971 for the paragraph-list ones). Runs at EVERY
        // edition — unlike the declined pass there is no edition below which the sink cannot fire. kb/Work PB829.
        global::CobolNet.Validation.ClosedFormatPass.Run(ctx, edition);

        // The group EC gate: ANY use of the EC model (an enabling TURN, a RAISE/RESUME/F3/RAISING, an
        // EXCEPTION-* function) turns the machinery on; otherwise the generated source is byte-identical to a
        // pre-EC build (the zero-scaffolding invariant, SSOT §18.16).
        bool ecActive = turn.AnyEnabled || units.Any(u => u.Bound.Ec is { Any: true })
            || classes.Any(c => c.Bound.Ec is { Any: true } || c.FactoryBound.Ec is { Any: true });

        // Per-program file-connector namespace (moved from the emit half — a BIND-phase model fact, so no CodeGen
        // write into the binding model remains; P6 exit criterion #2): the runtime file registry is
        // run-unit-global, but a file connector is INTERNAL to its program (ISO §8.6.3): two programs declaring
        // the same file-name (the IC-suite PRINT-FILE pattern, e.g. IC101A's two units) must not clobber each
        // other's connectors. Name resolution is done (bound nodes hold FileModel references), so qualifying the
        // runtime key is purely a rename. An EXTERNAL FD instead keys by its run-unit EXTERNALIZED name
        // (ISO §13.18.22.4 GR4a: ONE external file connector per run unit, shared by every describer — two units'
        // FileModels with the same external name converge on ONE registry key, hence one connector; GR5: the name
        // is the FD name). Each FileModel lives in exactly ONE unit's Files list (a fix-E GLOBAL merge shares
        // references through FilesByName only), so no model is renamed twice.
        foreach (var unit in units)
            foreach (var file in unit.Data.Files)
                file.CobolName = file is { IsExternal: true, ExternalName: { } ext }
                    ? NamingConvention.ExternalFileBand + ext
                    : unit.Path + "::" + file.CobolName;

        // VCR 18/31 (ISO §12.4.5.3 GR1(i)/(h); §14.8.4.2; Annex E.2 items 12/24) — a COBOL-2023 requirement: all
        // corresponding file control entries of an EXTERNAL file connector in the run unit shall specify FILE STATUS
        // (VCR 18) and, for a relative file, RELATIVE KEY (VCR 31) naming the SAME corresponding external data item.
        // Version-conditioned structural SR ⇒ read DialectLevel directly (the binder-reads-edition doctrine). The
        // compile-time reach is the group's units only; true separate-compilation conformance is the VCR-15 runtime
        // EC-EXTERNAL-DATA-MISMATCH descriptor check.
        if (edition.DialectLevel >= 2023) CheckExternalFileConsistency(units, edition);
        // The OO analogue (M2-OO-1i): an OBJECT/FACTORY file connector is scoped to its class, not a program unit,
        // so the program loop above never sees it. A factory file (singleton) keys by class; an instance file keys
        // per object (a minted key held in a __fkey field — see QualifyClassFiles); an EXTERNAL class file keys
        // by its run-unit external name, exactly like a program's.
        foreach (var cls in classes) QualifyClassFiles(cls);

        // Declaratives emit the __IoCheck/__RunUse machinery, which reads CobolFile even when the unit declares
        // NO files (IC401M: mode-scoped USE procedures in a file-less flagging program) — the IO using must
        // cover both. A class-only file program (M2-OO-1i — an OBJECT/FACTORY file with no program-unit file)
        // needs it too, or the generated <c>CobolFile.Register</c>/OPEN in the class body has no CobolNet.Runtime.IO
        // import (CS0103).
        bool anyFiles = units.Any(u => u.Data.Files.Count > 0)
            || units.Any(u => u.Bound.Declaratives is { Count: > 0 })
            || classes.Any(c => c.Data.Files.Count > 0 || c.FactoryData.Files.Count > 0)
            // a METHOD's own declaratives emit the same __IoCheck (kb/Work PB1010)
            || classes.Any(c => c.Symbol.Methods.Concat(c.Symbol.FactoryMethods).Any(m => m.Binding is { Declaratives.Count: > 0 }));

        return new BoundCompilation(tree, units, classes, table, oo.InterfaceData, ooAdapters, turn, ecActive,
            anyFiles);
    }

    /// <summary>The ≥2023 external-file conformance check (ISO §14.8.4.2; §12.4.5.3 GR1(i)/(h); Annex E.2 items 9/12/24).
    /// Two conjuncts of §14.8.4.2. <b>Conjunct 1 — externality (COBOLNET1624, per connector, ANY describer count):</b>
    /// a specified FILE STATUS / RELATIVE KEY / LINAGE data item shall itself be an external data item. <b>Conjunct 2 —
    /// consistency (VCR 18/31, COBOLNET1573/1575, needs ≥2 in-group describers):</b> "if any specifies it, all shall",
    /// each naming the SAME corresponding external data item. Cross-compilation sameness (separately-built assemblies)
    /// is the runtime <c>ExternalTable</c> EC-EXTERNAL-DATA-MISMATCH check's face. (In-group LINAGE consistency,
    /// §13.4.5.4 GR2(c), is a separate longstanding requirement — not this 2023-gated check.)</summary>
    /// <summary>⛔ THE ONE FOLD of an ACTIVATED element's §14.8.4.1 half — "the EC-EXTERNAL exception conditions to be
    /// checked shall be enabled in both the activating and activated runtime elements, which for activated runtime
    /// elements shall be before the Environment division": the <see cref="Runtime.ExternalChecks"/> bits of the
    /// EC-EXTERNAL conditions the group's TurnState has enabled at <paramref name="divLine"/>, the element's first
    /// division header after its identification. A program unit (<see cref="BindUnitData"/>) and a method (the
    /// §14.9.23.4 GR7 d) fold above; kb/Work PB1138) both take it, so the two cannot answer the rule differently.</summary>
    private static int ExternalMaskAt(TurnState turn, int divLine) =>
        (turn.Enabled("EC-EXTERNAL-FORMAT-CONFLICT", null, divLine) ? (int)Runtime.ExternalChecks.FormatConflict : 0)
        | (turn.Enabled("EC-EXTERNAL-DATA-MISMATCH", null, divLine) ? (int)Runtime.ExternalChecks.DataMismatch : 0)
        | (turn.Enabled("EC-EXTERNAL-FILE-MISMATCH", null, divLine) ? (int)Runtime.ExternalChecks.FileMismatch : 0);

    private static void CheckExternalFileConsistency(IReadOnlyList<BoundUnit> units, EditionContext edition)
    {
        var byExternalName = units.SelectMany(u => u.Data.Files)
            .Where(f => f is { IsExternal: true, ExternalName: not null })
            .GroupBy(f => f.ExternalName!, CobolNames.Comparer);
        // The 2023 requirement REMOVES a prior-edition freedom (corresponding external-file SELECTs could name
        // inconsistent / non-external FILE STATUS + RELATIVE KEY items), so its severity follows the removal policy:
        // an Error under strict, downgraded to a Warning under --permissive migration mode — the same contract the
        // continuity witnesses need (an 85 NIST program that violates the new rule, e.g. IC227A with two differently-
        // named non-external FILE STATUS items, still COMPILES under permissive and rejects under strict).
        var severity = EditionSeverityPolicy.For(ConstructAvailability.Removed, edition.Edition);
        // §14.8.4.2 conjunct 1 — EXTERNALITY: a FILE STATUS / RELATIVE KEY / LINAGE data item associated with an
        // external file connector shall ITSELF be an external data item. One shared clause-parameterized diagnostic
        // (the §14.8.4.2 sentence names all three items together); the E.2-item-9 conformance-checking mechanism is
        // the 2023 addition, so it inherits the same Removed-freedom severity as the 1573/1575 consistency siblings.
        EditionDiagnostic Externality(string ext, string clause, string name) =>
            new("COBOLNET1624", severity, "external-file-item-not-external",
                $"external file '{ext}': the {clause} data item '{name}' shall be an external data item "
                + "(ISO §14.8.4.2; Annex E.2 item 9)",
                $"external file '{ext}'", "ISO §14.8.4.2 / Annex E.2 item 9");
        foreach (var group in byExternalName)
        {
            string externalName = group.Key.ToUpperInvariant();   // the name as the diagnostics show it; equality was the fold's above
            var conns = group.ToList();
            // Conjunct 1 (externality) is enforced for EVERY external connector, regardless of describer count — a
            // lone-program external file whose file-referencing item is non-external is a §14.8.4.2 violation.
            foreach (var f in conns)
            {
                if (f.FileStatusName is not null && ExternalItemIdentity(f.FileStatusItem) is null)
                    edition.Report(Externality(externalName, "FILE STATUS", f.FileStatusName));
                if (f.Organization == FileOrganization.Relative && f.RelativeKeyName is not null
                    && ExternalItemIdentity(f.RelativeKeyItem) is null)
                    edition.Report(Externality(externalName, "RELATIVE KEY", f.RelativeKeyName));
                if (f.Linage is { } lin)
                    foreach (var op in lin.Operands)
                        if (op.DataName is not null && ExternalItemIdentity(op.Item) is null)
                            edition.Report(Externality(externalName, "LINAGE", op.DataName));   // literal operands are exempt
            }
            // Conjunct 2 (cross-unit CONSISTENCY, §12.4.5.3 GR1(h)/(i)) needs ≥2 in-group describers to reconcile; the
            // single-describer externality face is enforced above, and cross-compilation sameness stays with the
            // ExternalTable runtime EC-EXTERNAL-DATA-MISMATCH check.
            if (conns.Count < 2) continue;

            // VCR 18 — FILE STATUS: if ANY corresponding SELECT specifies FILE STATUS, ALL shall, each naming the
            // same corresponding external data item (§12.4.5.3 GR1(i)).
            if (conns.Any(f => f.FileStatusName is not null))
            {
                var ids = conns.Select(f => ExternalItemIdentity(f.FileStatusItem)).ToList();
                if (ids.Any(id => id is null) || ids.Distinct(StringComparer.Ordinal).Count() > 1)
                    edition.Report(new EditionDiagnostic("COBOLNET1573", severity, "external-file-status-consistency",
                        $"external file '{externalName}': all corresponding SELECT statements in the run unit shall "
                        + "specify FILE STATUS naming the same corresponding external data item "
                        + "(ISO §12.4.5.3 GR1(i); Annex E.2 item 12)",
                        $"external file '{externalName}'", "ISO §12.4.5.3 GR1(i) / §14.8.4.2 / Annex E.2 item 12"));
            }

            // VCR 31 — RELATIVE KEY: for an external RELATIVE file, if ANY corresponding SELECT specifies RELATIVE
            // KEY, ALL shall, each naming the same corresponding external data item (§12.4.5.3 GR1(h)).
            if (conns.Any(f => f.Organization == FileOrganization.Relative)
                && conns.Any(f => f.RelativeKeyName is not null))
            {
                var ids = conns.Select(f => ExternalItemIdentity(f.RelativeKeyItem)).ToList();
                if (ids.Any(id => id is null) || ids.Distinct(StringComparer.Ordinal).Count() > 1)
                    edition.Report(new EditionDiagnostic("COBOLNET1575", severity, "external-relative-key-consistency",
                        $"external relative file '{externalName}': all corresponding SELECT statements in the run unit "
                        + "shall specify RELATIVE KEY naming the same corresponding external data item "
                        + "(ISO §12.4.5.3 GR1(h); Annex E.2 item 24)",
                        $"external relative file '{externalName}'", "ISO §12.4.5.3 GR1(h) / §14.8.4.2 / Annex E.2 item 24"));
            }
        }
    }

    /// <summary>The "corresponding external data item" identity of <paramref name="item"/> (ISO §14.8.4.2): the
    /// dotted qualified path from its EXTERNAL root (the root carries the EXTERNAL clause or an external TYPE), i.e.
    /// the externalized name plus the sub-path — null if the item is absent or is NOT an external data item. Two
    /// file-referencing items are the SAME corresponding external item iff their identities are equal. (Keys by the
    /// data-name, consistent with the run-unit ExternalStore cell key; the rare AS-literal externalized name is not
    /// honored there either.)</summary>
    internal static string? ExternalItemIdentity(DataItem? item)
    {
        if (item is null) return null;
        var path = new List<string>();
        var r = item;
        while (r.Parent is not null) { path.Add(r.CobolName ?? "?"); r = r.Parent; }
        if (!(r.HasExternalClause || r.ExternalFromType)) return null;   // root is not an external data item
        path.Add(r.CobolName ?? "?");
        path.Reverse();
        return string.Join(".", path).ToUpperInvariant();
    }

    /// <summary>Flatten the compilation group into the ordered unit lists — top-level program units in source
    /// order, each followed by its contained programs (containers precede containees; load-bearing for GLOBAL
    /// inheritance), plus the group's CLASS-ID units (the Phase-3 OO spine). The pass-1 class symbol table
    /// (deep-dive D1) is built HERE — before ANY unit binds — so a driver's typed object references and INVOKEs
    /// resolve classes defined later in the file. A contained <c>nestedProgram</c> parse context is re-shaped
    /// into a synthetic <c>programUnit</c> context (identical child shape) so the per-unit binders consume one
    /// context type.</summary>
    private static (List<BoundUnit> Programs, List<OoClassUnit> Classes, OoClassTable Table) CollectUnits(
        Core.CompilationUnitContext tree, EditionContext edition, CobolNet.Editions.CobolWordsMap words)
    {
        var all = new List<BoundUnit>();
        var usedClassNames = new HashSet<string>(StringComparer.Ordinal);
        var classDefs = new List<Core.ClassDefinitionContext>();
        var ifaceDefs = new List<Core.InterfaceDefinitionContext>();

        foreach (var group in tree.compilationGroup())
        {
            PrototypeUnitRules.ScreenOrder(group, edition);   // §10.6.2 SR1 (kb/Work PB894)
            classDefs.AddRange(group.classDefinition());
            ifaceDefs.AddRange(group.interfaceDefinition());   // §11.6 — collected, NEVER silently dropped (the W2 rule)
            foreach (var pu in group.programUnit())
                Collect(pu, null);
        }
        // §9.3.12 / §9.3.13 (kb/Work PB759): a parameterized definition is a SKELETON, never a class; each
        // REPOSITORY EXPANDS phrase creates one, and from here on an expansion is an ordinary definition.
        var expanded = OoExpansion.Expand(tree, classDefs, ifaceDefs, edition, words);
        var table = OoClassTable.Build(expanded.Classes, edition, expanded.Interfaces, expanded.ParameterizedNames);
        var classes = table.Classes.Select(sym => new OoClassUnit { Symbol = sym }).ToList();
        return (all, classes, table);

        void Collect(Core.ProgramUnitContext ctx, BoundUnit? parent)
        {
            var unit = MakeUnit(ctx, parent, all.Count, usedClassNames, edition);
            all.Add(unit);
            parent?.Children.Add(unit);
            foreach (var nested in ctx.nestedProgram())
                Collect(Reparent(nested), unit);
        }
    }

    /// <summary>Build one <see cref="BoundUnit"/> from a program unit's IDENTIFICATION DIVISION: the DECLARED
    /// name (program-name-1 / user-function-name-1), the EXTERNALIZED name the <c>AS</c> phrase gives it, and the
    /// COMMON / INITIAL / RECURSIVE attributes with their per-edition + placement gates (§11.10.3 SR4–6).
    /// <para>ISO §11.10.4 GR1 keeps the two names DISTINCT — "Program-name-1 names the program declared by this
    /// program definition. Literal-1, if specified, is the name of the program that is externalized to the
    /// operating environment" — and this used to collapse them onto one field (kb/Work PB303), which would have
    /// broken §10.7.3 SR2's END PROGRAM match, §14.9.4.3 SR15's AS NESTED lookup and the §15.65.4 r4 MODULE-NAME
    /// determination the moment the phrase parsed. They are separate fields now.</para></summary>
    private static BoundUnit MakeUnit(
        Core.ProgramUnitContext ctx, BoundUnit? parent, int index, HashSet<string> usedClassNames, EditionContext edition)
    {
        var idBody = ctx.identificationDivision()?.identificationBody();
        var pid = idBody?.programIdParagraph();
        var fid = idBody?.functionIdParagraph();
        string name = pid?.programName()?.GetText()
            ?? fid?.programName()?.GetText()
            ?? $"PROGRAM{index}";
        bool isFunction = pid is null && fid is not null;
        // A signature-only prototype unit — a FUNCTION-ID prototype (M2-UDF-3) or a §11.10.2 Format-2 PROGRAM-ID
        // prototype (kb/Work PB894; §11.10.4 GR6 — program-prototype-name-1 identifies the prototype, literal-1
        // its externalized name, which the AS arm below already reads for either paragraph). ONE grammar node,
        // `prototypePhrase`, answers for both kinds — until PB894 this read the FUNCTION-ID paragraph alone, so
        // IsPrototype could never be true for a program. The COBOL-2002 introduction gates are
        // VersionConformancePass.Run (bound-arm over group.Units — BoundUnit.IsPrototype is drop-proof).
        bool isPrototype = (pid?.prototypePhrase() ?? fid?.prototypePhrase()) is not null;
        string what = $"{(isFunction ? "FUNCTION-ID" : "PROGRAM-ID")} '{name}'{(isPrototype ? " IS PROTOTYPE" : "")}";
        if (isPrototype)
            // §10.6.2 SR4 — the BODY of a prototype, one screen for every prototype kind.
            PrototypeUnitRules.ScreenBody(what,
                idBody?.identificationParagraph().Select(p => p.optionsParagraph()).FirstOrDefault(o => o is not null),
                ctx.environmentDivision(), ctx.dataDivision(), ctx.procedureDivision(), edition);
        // §10.6.1 — the SHAPE of EVERY unit kind (containment, end marker, the procedure-division bracket), one
        // table (kb/Work PB1507; it used to be asked of the prototype kinds alone).
        SourceUnitShape.Screen(what, SourceUnitShape.KindOf(isFunction, isPrototype),
            parent is null ? null : SourceUnitShape.KindOf(parent.IsFunction, parent.IsPrototype), ctx, edition);
        // §11.10.2 Format 1's attribute group is a §5.2.6.4 choice-indicator group — any order, each alternative at
        // most ONCE — and the grammar's `programIdAttribute+` is the superset the only-once half is read from.
        var attributes = pid?.programIdAttributes()?.programIdAttribute() ?? [];
        string group = $"PROGRAM-ID '{name}'";
        using var attributePosition = edition.At(pid);   // the PROGRAM-ID paragraph, for the repeat / 0886 / 0887 messages
        var initialAttr = ChoiceIndicators.AtMostOnce(edition,
            attributes.Where(a => a.commonProgramAttribute().INITIAL_() is not null).ToArray(),
            group, "the INITIAL attribute", "11.10.2");
        var commonAttr = ChoiceIndicators.AtMostOnce(edition,
            attributes.Where(a => a.commonProgramAttribute().COMMON() is not null).ToArray(),
            group, "the COMMON attribute", "11.10.2");
        var recursiveAttr = ChoiceIndicators.AtMostOnce(edition,
            attributes.Where(a => a.commonProgramAttribute().RECURSIVE() is not null).ToArray(),
            group, "the RECURSIVE attribute", "11.10.2");
        bool initial = initialAttr is not null, common = commonAttr is not null, recursive = recursiveAttr is not null;

        // §11.10.3 SR5 / SR6 — the CONTAINER chain ("directly or indirectly contains this program"). SR5 reads the
        // container's EFFECTIVE recursive attribute (the one §11.10.4 GR4 hands down is still "a recursive program");
        // SR6 reads a container's INITIAL clause, which is never inherited. The nearest offending container is named.
        if (initialAttr is not null)
            for (var anc = parent; anc is not null; anc = anc.Parent)
                if (anc.Recursive)
                {
                    using var _ = edition.At(initialAttr);
                    edition.Error(DiagnosticCatalog.ProgramAttributeContainment,
                        $"program '{name}' is INITIAL but is contained in the recursive program '{anc.Name}': the "
                        + "INITIAL clause shall not be specified if any program that directly or indirectly "
                        + "contains this program is a recursive program (ISO §11.10.3 SR5)");
                    break;
                }
        if (recursiveAttr is not null)
            for (var anc = parent; anc is not null; anc = anc.Parent)
                if (anc.Initial)
                {
                    using var _ = edition.At(recursiveAttr);
                    edition.Error(DiagnosticCatalog.ProgramAttributeContainment,
                        $"program '{name}' is RECURSIVE but is contained in the initial program '{anc.Name}': the "
                        + "RECURSIVE clause shall not be specified if any program that directly or indirectly "
                        + "contains this program is an initial program (ISO §11.10.3 SR6)");
                    break;
                }

        // §11.10.2 / §11.5.2's `[ AS literal-1 ]` — the externalized name (kb/Work PB303). Absent, §8.3.2.2 2)
        // externalizes the user-defined word itself, so the two names coincide.
        string externalized = name;
        if ((pid?.externalizedNamePhrase() ?? fid?.externalizedNamePhrase()) is { } asPhrase)
        {
            using var _ = edition.At(asPhrase);
            string kind = isFunction ? "FUNCTION-ID" : "PROGRAM-ID";
            string rule = isFunction ? "ISO §11.5.3 SR1" : "ISO §11.10.3 SR1";
            // §11.10.3 SR2 — FORMAT 1 only: "Literal-1 shall not be specified in a program that is contained
            // within another program." §8.3.2.2 2) externalizes "program-names of OUTERMOST programs", so a
            // containee has no externalized name for the phrase to give. §11.5.3 states no such rule (a
            // function definition is never contained), so the gate keys on the containment, not the kind.
            if (parent is not null)
                edition.Error(DiagnosticCatalog.ExternalizedNameContained,
                    $"{kind} '{name}' AS {asPhrase.literal().GetText()}: literal-1 shall not be specified in a "
                    + "program that is contained within another program (ISO §11.10.3 SR2)");
            else if (ExternalizedName.Screen(asPhrase.literal(), edition,
                         DiagnosticCatalog.ExternalizedNameLiteral,
                         $"{kind} '{name}' AS {asPhrase.literal().GetText()}", "literal-1", rule,
                         LiteralEnvironment.Unscoped) is { } lit1)
                externalized = lit1;
        }

        // program-id-recursive-2002: the pass owns the edition gate (Exec Step E).
        if (initial && recursive)
            edition.Error("COBOLNET0886",
                $"program '{name}': INITIAL and RECURSIVE are mutually exclusive — §11.10.2 Format 1 prints them as "
                + "the alternatives of ONE brace (ISO §11.10.2); the containment conflicts of the two attributes are "
                + "§11.10.3 SR5–6, checked above against the container chain");
        if (common && parent is null)
            edition.Error("COBOLNET0887",
                $"program '{name}': COMMON may be specified only in a CONTAINED program (ISO §11.10.3 SR4)");

        // §8.6.6 (:8821) "Functions and methods are always recursive" / §9.4 (:12529) "a user defined
        // function always possesses the recursive attribute and may call itself" — implicit, never the
        // explicit PROGRAM-ID attribute, so it rides AFTER the 0885/0886 gates. Registering Recursive here
        // is what keeps ProgramTable's §14.9.4.4 GR3f re-entry rejection (EC-PROGRAM-RECURSIVE-CALL) from
        // firing on a function's self-activation and selects the per-activation instance model (D3/D4).
        if (isFunction) recursive = true;
        // §11.10.4 GR4 (kb/Work PB133): "The RECURSIVE clause specifies that the program AND ANY PROGRAMS
        // CONTAINED WITHIN IT are recursive" — the attribute inherits down the containment tree (parents are
        // built before their children, so one parent read cascades transitively). The inherited attribute is
        // what SR5 reads of a container ("a recursive program"), so an INITIAL containee of a recursive
        // container is the nonconforming source SR5 refuses above — never a legal program carrying both
        // attributes (kb/Work PB1507; this comment used to call that shape legal). This is what lets the legal
        // R→C→R→C cycle through GR3f's re-entry check and lets a contained program call ITSELF (§8.4.6.3 r1's "in
        // the program itself") — both drew EC-PROGRAM-RECURSIVE-CALL / NOT-FOUND before.
        if (parent is { Recursive: true }) recursive = true;

        string baseName = "_PRG_" + DataItem.Sanitize(name).ToUpperInvariant();
        string className = baseName;
        for (int n = 2; !usedClassNames.Add(className); n++) className = $"{baseName}_{n}";
        return new BoundUnit
        {
            Name = name, ExternalizedName = externalized, ClassName = className, Ctx = ctx,
            Parent = parent, Initial = initial, Common = common, Recursive = recursive,
            IsFunction = isFunction, IsPrototype = isPrototype,
        };
    }

    /// <summary>Re-shape a <c>nestedProgram</c> context into a synthetic <c>programUnit</c> context by adopting
    /// its children (the two rules have the identical child sequence — the generated <c>dataDivision()</c> /
    /// <c>procedureDivision()</c> accessors scan DIRECT children only, so each unit binds exactly its own
    /// subtree, never a containee's — the IC235A nested-scoping lesson).</summary>
    private static Core.ProgramUnitContext Reparent(Core.NestedProgramContext nested)
    {
        var unit = new Core.ProgramUnitContext(null!, -1);
        for (int i = 0; i < nested.ChildCount; i++)
            switch (nested.GetChild(i))
            {
                case ParserRuleContext rc: unit.AddChild(rc); break;
                case ITerminalNode t: unit.AddChild(t); break;
            }
        return unit;
    }

    /// <summary>The DATA half of unit binding (phase 1 of the two-phase bind): the unit's DATA DIVISION on a
    /// per-unit <see cref="DataBinder"/> with a disjoint uid band (so nested-class struct/profile names never
    /// shadow a container's), then inject the containers' GLOBAL names (ISO §13.18.27 GR1–2 — nearest container
    /// first, each at its nesting depth; which one a reference names is §8.4.6.2.1 3)'s question, asked at the
    /// reference by <c>SymbolTable.NearestDeclaring</c>) and record the <c>ref</c>-bridges the nested class needs to reach the
    /// container's storage. Every unit passes through here BEFORE any unit's procedure binds
    /// (<see cref="BindUnitProcedure"/>) — the forward-reference enabler for user-function signatures.</summary>
    /// <summary>The line of a unit's first token — where §7.3.4 GR5's "all of the source text … that follows" is folded.
    /// A unit whose context carries no start token of its own (a header-less nested program, §11.2.1) takes its first
    /// descendant's line.</summary>
    private static int UnitFirstLine(Antlr4.Runtime.ParserRuleContext c)
    {
        if (c.Start is { } s) return s.Line;
        for (int i = 0; i < c.ChildCount; i++)
        {
            int line = c.GetChild(i) switch
            {
                Antlr4.Runtime.ParserRuleContext p => UnitFirstLine(p),
                Antlr4.Runtime.Tree.ITerminalNode t => t.Symbol.Line,
                _ => 0,
            };
            if (line > 0) return line;
        }

        return 0;
    }

    private static void BindUnitData(BoundUnit unit, BindSession session)
    {
        var edition = session.Edition;
        // The static-WS discriminator (ISO §13.5.4 GR1 + §14.6.2.3.2/.3; the full derivation is on
        // DataBinder.UnitStaticWs): a RECURSIVE-and-not-INITIAL unit — including every FUNCTION-ID unit
        // (§8.6.6 :8821 / §9.4 :12529, the implicit attribute set in MakeUnit) — owns ONE last-used WS copy
        // shared across activations, so its WS roots emit STATIC — with or without contained programs. A
        // containee's GLOBAL bridges alias the container's members (§13.18.27 GR2): an INSTANCE member is reached
        // through the `__outer` chain, a STATIC one through the container's class name, which C# requires
        // (CS0176: `instance.staticField` is not a C# expression). BoundUnit.AnchorOf chooses per member, so the
        // composition is one rule in one place (kb/Work PB1133; the staged COBOLNET0899 refusal is gone).
        var data = new DataBinder(edition)
        {
            OoClasses = session.OoClasses,
            RefModZeroLength = session.RefModZeroLength,
            CobolWords = session.CobolWords,   // >>COBOL-WORDS intrinsic-function-name synonym/removal (§7.3.10)
            Retypes = session.Retypes,         // the fragment re-parses read words as the tree does (kb/Work PB655)
            LeapSecond = session.LeapSecond.IsOnAt(UnitFirstLine(unit.Ctx)),   // >>LEAP-SECOND ON at THIS unit — the §15.3 seconds-subfield / time-form bound (§7.3.17, §7.3.4 GR5)
            CompilationVariables = session.CompilationVariables,   // CONSTANT … FROM reads the >>DEFINE table at its own line (§7.3.11.4 GR1)
            // The ANY LENGTH placement facts (ISO §13.18.2.3 SR2–SR4 — the rules differ for a contained
            // program, a function, and an outermost program): the unit kind is known only here.
            UnitIsContained = unit.Parent is not null,
            UnitIsFunction = unit.IsFunction,
            // §8.4.6.6 / §8.4.6.8 — the SELF leg of the two prototype-name scope rules (kb/Work PB452/PB817).
            UnitSelfName = unit.Name,
            UnitStaticWs = unit.Recursive && !unit.Initial,
        };
        data.CallSeedUids(session.TakeUidBand());

        // Configuration-section + OPTIONS inheritance (ISO §12.3.4 GR1 / §11.9.4 GR1; §12.3.3 SR1 — a contained
        // program cannot have its own configuration section): the WHOLE configuration-derived state of the
        // container — SPECIAL-NAMES (DECIMAL-POINT, CURRENCY, classes, alphabets, switches), the PROGRAM
        // COLLATING SEQUENCE, DEBUGGING MODE, the REPOSITORY specifiers — and the OPTIONS baseline, copied in
        // BEFORE this unit binds so its first literal and PICTURE already see them (kb/Work PB60 /
        // AR-15.67.3-5: only the REPOSITORY sets were inherited, after Bind, and a contained program under
        // DECIMAL-POINT IS COMMA parsed NUMVAL("123,45") as 0). One level suffices: the container inherited
        // from ITS container before it bound (units bind container-first).
        if (unit.Parent is not null) data.InheritConfiguration(unit.Parent.Data);
        // The container's GLOBAL constant-names (§13.18.27.4 GR1–GR2; kb/Work PB1009) — compile-time
        // substitutions, so they join BEFORE this unit binds, and a local data-name shadows them after. EVERY
        // container, nearest first (the nearest global declaration wins): an intermediate program's own NON-global
        // declaration of the name hides nothing from the programs it contains.
        int constantDepth = 0;
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            data.InheritGlobalConstants(anc.Data, ++constantDepth);
            // …and its GLOBAL type declarations (§13.18.58.4 GR3; §8.4.6.2.2 — a type-name described with a GLOBAL
            // clause is a global name; kb/Work PB1303): the same BEFORE-Bind, nearest-first, local-shadows contract —
            // ExpandTypes runs inside data.Bind, so a TYPE clause here already sees the container's type-name.
            data.InheritGlobalTypeDecls(anc.Data);
        }
        // The member names every container's GLOBAL root will occupy in this unit's class (kb/Work PB1047): the
        // bridges are emitted for every global root below, so a LOCAL root spelled like one (the §8.4.6.2.1 3) a)
        // shadowing case) must take another C# name — its stem too, or a member DERIVED from the stem (a REDEFINES
        // backing) would re-spell the bridge (DataBinder.InheritedMemberNamesOf).
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
            foreach (var g in anc.Data.CallGlobalRoots)
                data.ReserveInheritedMemberNames(anc.Data.InheritedMemberNamesOf(g));

        data.Bind(unit.Ctx);
        unit.Data = data;

        // The unit's EC-EXTERNAL enablement facts (ISO §14.8.4.1): the ACTIVATED-element mask is the group
        // TurnState folded at the unit's first post-Identification division header line ("which for activated
        // runtime elements shall be before the Environment division"); the group-level Describe gate is any
        // EC-EXTERNAL enabling event anywhere in the group (a mask-zero unit still registers its descriptions
        // so a later-enabled element can check against them; no event anywhere ⇒ zero-scaffolding).
        int divLine = unit.Ctx.environmentDivision()?.Start.Line
            ?? unit.Ctx.dataDivision()?.Start.Line
            ?? unit.Ctx.procedureDivision()?.Start.Line
            ?? int.MaxValue;
        data.ExternalCheckMask = ExternalMaskAt(session.Turn, divLine);
        // §14.9.4.4 GR3 d)'s ACTIVATED half (kb/Work PB133 wave C2b) — folded at the unit's PROCEDURE DIVISION
        // HEADER, never at divLine: the before-Environment-division point above is §14.8.4.1's rule for the
        // EC-EXTERNAL conditions ALONE, and §7.3.25.4 GR6 / GR8 scope a TURN to the "procedure division headers
        // that follow" (kb/Work PB1381 — TurnState.EnabledAtHeader, which the METHOD twin asks too).
        data.ArgMismatchChecking = session.Turn.EnabledAtHeader(
            "EC-PROGRAM-ARG-MISMATCH", unit.Ctx.procedureDivision(), int.MaxValue);
        data.ExternalDescribe = Procedure.EcBinder.ExternalNames.Any(session.Turn.AnyEnabledFor);
        data.OdoReferenceChecking = session.Turn.AnyEnabledFor("EC-BOUND-ODO");   // §13.18.38.4 GR7 (kb/Work PB1268)
        // §7.3.21.4 GR1/GR3 — automatic propagation for this program or function (kb/Work PB1119), folded at the
        // unit's first line (§7.3.21.3 SR1: never inside a compilation unit, so a contained program folds to its
        // container's state). Its IDENTIFICATION DIVISION is the first line: a CONTAINED unit's Ctx is the synthetic
        // context Reparent builds, whose own Start is null (see NameCtx below).
        data.AutomaticPropagation = session.Propagate.IsOnAt(unit.Ctx.identificationDivision().Start.Line);

        // GLOBAL FD inheritance (ISO §13.18.27.4 GR1: the file-name of a GLOBAL FD is a GLOBAL name, visible in every
        // directly/indirectly contained program; §13.18.27 GR1–2 — nearest container first, a local declaration
        // shadows, which TryAdd realizes since local files are already present). Merge into FilesByName ONLY —
        // never Files: the child must not re-register, re-qualify, or CANCEL-close the owner's connector; its
        // bound verbs hold the SHARED FileModel reference, so the owner's one-time PROG::FILE qualification
        // automatically keys the child's verbs to the owner's connector. (EXTERNAL is NOT global — §13.18.22
        // NOTE 1: an EXTERNAL non-GLOBAL FD's name is not visible in contained programs.) The record-name half
        // of §13.18.27.4 GR1 rides the standard GLOBAL-root bridges (DataBinder.CallBindExternalAndGlobal adds a
        // GLOBAL FD's records to CallGlobalRoots).
        // A container's NON-global FD is recorded too, for one question only: a GLOBAL record under it (§13.18.27.3
        // SR1 b)) is visible here while its file is not, and WRITE / REWRITE of it is refused by name (kb/Work PB1193).
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
            foreach (var f in anc.Data.Files)
                if (f.IsGlobal)
                    data.FilesByName.TryAdd(f.CobolName, f);
                else
                    data.ContainerLocalFiles.Add(f);

        // GLOBAL RD inheritance (ISO §13.18.27.3 SR1 e) / §13.18.27.4 GR1–GR2; kb/Work PB369) — the report-name,
        // its groups and its sum counters, nearest container first so the nearer declaration hides. Like a GLOBAL
        // FD, the report is SHARED, never re-declared: the child's verbs hold the container's ReportModel and
        // reach its engine through the __outer chain (DataBinder.ReportDepth).
        int reportDepth = 0;
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            reportDepth++;
            foreach (var r in anc.Data.Reports)
                if (r.IsGlobal)
                    data.InheritGlobalReport(r, reportDepth);
        }

        // kb/Work PB971 — the formals' *-ARG-OMITTED guards, set BEFORE any contained program binds (a parent
        // binds before its children), so the GLOBAL bridge below can carry a guarded root's presence member.
        Procedure.EcBinder.MarkFormals(data.LinkageFormals, SourceElementKindOf(unit), session.Turn);

        int depth = 0;
        var bridgedMembers = new HashSet<string>(StringComparer.Ordinal);
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            depth++;
            foreach (var g in anc.Data.CallGlobalRoots)
            {
                if (g.CobolName is null) continue;
                // EVERY global root joins, shadowed spelling or not (kb/Work PB1047 / PB1243): §8.4.6.2.1 1) puts
                // "all global names that are defined in source element A and in any source elements that directly
                // or indirectly contain source element A" in the set, and §8.4.6.2.2 makes every data-name
                // subordinate to a global name a global name in its own right — so a local 01 G hides the
                // container's G, never its subordinate Y. WHICH candidate a reference names is §8.4.6.2.1 3)'s
                // nearest-declaring-element rule, applied after qualification (SymbolTable.NearestDeclaring); the
                // depth recorded here is what it reads. (This loop used to skip a root whose SPELLING was already
                // present, dropping its whole subtree, and to merge the rest into one flat tier.)
                data.InheritGlobalSubtree(g, depth);
                // Every container member a reference to the root can render, from the ONE list the container's
                // binder owns (kb/Work PB1009 — the per-residence arms used to be spelled here, and the
                // carrier-resident formal and the BASED item's address pointer were missing from them). A local
                // root never collides with a bridge's member name: ReserveInheritedMemberNames ran before Bind.
                // A SET keyed on the emitted member (kb/Work PB1523): two global roots can name ONE member — the records of
                // a GLOBAL FD share a Tier-B class's backing, a GLOBAL redefiner and its anchor share one field — and a
                // member declared twice is CS0102. The member names are unique along the container chain
                // (ReserveInheritedMemberNames), so equal names are the same member.
                foreach (var bridge in anc.Data.GlobalBridgesOf(g, member => anc.AnchorOf(member, depth)))   // consumed here, within this iteration
                    if (bridgedMembers.Add(bridge.Field)) unit.Bridges.Add(bridge);
            }
        }

        // Every tier is in place now — this unit's own names and every container's globals — so an inherited
        // constant a NEARER data-name hides can leave (§8.4.6.2.1 3); kb/Work PB1009 / PB1047).
        data.DropShadowedConstants();
        unit.Refs = new ReferenceResolver(data);
    }

    /// <summary>ALWAYS-ON (P6 Step 6): assert <paramref name="pass"/>'s declared prerequisite phase has been
    /// PRODUCED on every binder of the group before it runs (a per-pass integer compare per binder — immaterial;
    /// was Debug-only until CI's Release leg exposed the divergence, DEVLOG 774).</summary>
    private static void RequireAll(GroupBindContext ctx, GroupBindPass pass)
    {
        foreach (var d in ctx.AllBinders()) d.Require(pass.Requires, pass.Name);
    }

    /// <summary>The <c>ProcedureBinding</c> GROUP pass body (P6 Step 3 — <c>BindPipeline.GroupTail</c>, Requires
    /// <c>FilesResolved</c>, Produces <c>ProcedureBound</c>): bind every unit's PROCEDURE DIVISION against the
    /// group's REPOSITORY tables (<see cref="BindSession.Repository"/>, built by <see cref="Bind"/> before the
    /// class bodies).</summary>
    internal static void BindProcedures(GroupBindContext ctx)
    {
        foreach (var unit in ctx.Units) BindUnitProcedure(unit, ctx.Session);
    }

    /// <summary>Build the group's REPOSITORY resolution tables (§12.3.8.4 GR10 / GR11) — once, between the DATA
    /// phase and the first procedure body.</summary>
    private static GroupRepository BuildGroupRepository(IReadOnlyList<BoundUnit> units,
                                                        IReadOnlyList<OoClassUnit> classes, BindSession session)
    {
        // BEFORE either table is built (kb/Work PB660): a compilation group that DEFINES one name twice
        // is nonconforming source, and both tables below silently keep the first definition and drop the
        // second — the shape §8.3.2.2 exists to forbid.
        CheckDefinitionNameUniqueness(units, classes, session.OoClasses, session.Edition);
        CheckPrototypeSignaturePairs(units, session.Edition);
        // kb/Work PB237 — the compilation group's program definitions by EXTERNALIZED name, the search space
        // §12.3.8.4 GR10 a) names. Built once for the whole group, exactly like the user-function table beside it.
        // PB894 adds GR10 b): an in-group program PROTOTYPE definition, behind the definitions.
        return new GroupRepository(BuildUserFunctionTable(units, session.Edition), BuildProgramDetailsTable(units));
    }

    /// <summary>The PROCEDURE half of unit binding (phase 2): every unit's DATA is already bound
    /// (<see cref="BindUnitData"/>) and the group's user-function signature table is built, so a
    /// <c>FUNCTION user-name(args)</c> reference resolves its callee's RETURNING/USING descriptions
    /// regardless of unit order in the source (§8.4.3.2.4 GR1).</summary>
    private static void BindUnitProcedure(BoundUnit unit, BindSession session)
    {
        var data = unit.Data;
        var binder = new StatementBinder(data, unit.Refs)
        {
            OoClasses = session.OoClasses,
            UserFunctions = UserFunctionsOf(data, unit, session.Repository.UserFunctions),
            // §8.4.6.6 — inside a function definition its OWN name is a referable function-prototype-name
            // (self-recursion without a repository entry; §12.3.8.3 SR11 makes a present self-entry a no-op —
            // SpecifiesItself, which UserFunctionsOf asks).
            UdfSelfName = unit.IsFunction ? unit.Name : null,
            // ISO §14.2.2 SR10's enumeration of the five source elements that may carry a Format-1/2 procedure
            // division — the input to every EXIT / GOBACK placement rule (kb/Work PB403). Derived HERE, beside
            // UdfSelfName, from the same two unit facts, so the "am I a function?" answer cannot fork.
            UnitKind = SourceElementKindOf(unit),
            // §15.65.3 argument rule 1 — MODULE-NAME NESTED requires a contained program.
            InNestedProgram = unit.Parent is not null,
            // kb/Work PB131 — the AS NESTED callee set (§14.9.4.3 SR15): the caller's directly-contained
            // children, plus every COMMON program contained in a (transitive) ancestor — the §10.7.2
            // visibility the runtime ResolveVisible applies, computed statically so GR9's formal-mode
            // lookup and SR15's scope check both happen at BIND time.
            NestedCallables = NestedCallablesOf(unit),
            // kb/Work PB237 — the unit's visible program prototypes (§12.3.8.2 specifiers resolved through
            // §12.3.8.4 GR10, plus §8.4.6.8's containing-program spelling).
            ProgramPrototypes = ProgramPrototypesOf(data, unit, session.Repository.ProgramDefinitions),
            UnitRecursive = unit.Recursive,   // §14.9.7.3 SR1 / §14.9.36.3 SR1 (kb/Work PB137)
        };
        binder.ConfigureEc(session.Turn, session.DirectiveSites, unit.Name);   // the EC bind context (TURN fold + directive sites + §15.30 location element)
        unit.Bound = binder.Bind(unit.Ctx);
        // The boundary-copied GROUP formals + RETURNING item are registered whole-group-referenced (so StorageFormPass
        // flips their numeric-DISPLAY leaves to image storage, and the formal's FromImage/AsImage round trip
        // type-checks — ISO §14.2.3 GR8 / §14.9.25.4 MOVE GR4) by the post-bind UsageCollectionPass, from data.LinkageFormals
        // + data.LinkageReturning. The pre-flip early-resolve of every formal existed ONLY for that side effect (which
        // ReferenceResolver no longer performs) — deleted, PHASE-05 Step 5.
    }

    /// <summary>Classify a bound unit as one of ISO §14.2.2 SR10's source elements (kb/Work PB403). The two unit
    /// facts the classification needs — FUNCTION-ID vs PROGRAM-ID, and the <c>IS PROTOTYPE</c> tail — are already
    /// carried by <see cref="BoundUnit"/>; a method definition is NOT classified here, because a method's
    /// statements bind on its CLASS unit's binder under an entered method scope (<c>BinderContext.SourceElement</c>).
    /// <para>A FUNCTION prototype's procedure division is BOUND by this compiler — the §10.1 general format
    /// admits one and the parser accepts its paragraphs, measured: a prototype carrying <c>EXIT PROGRAM</c>
    /// reaches <c>BindExit</c> (and is refused there by §10.6.2 SR4 f) — COBOLNET2272 — before any placement
    /// rule matters). A PROGRAM prototype classifies the same way since kb/Work PB894 made §11.10.2 Format 2
    /// writable and <c>MakeUnit</c> read <c>IsPrototype</c> off the shared <c>prototypePhrase</c>.</para></summary>
    private static SourceElementKind SourceElementKindOf(BoundUnit unit) => unit.IsFunction
        ? (unit.IsPrototype ? SourceElementKind.FunctionPrototype : SourceElementKind.FunctionDefinition)
        : (unit.IsPrototype ? SourceElementKind.ProgramPrototype : SourceElementKind.Program);

    /// <summary>The AS NESTED callee table for one caller (kb/Work PB131; §14.9.4.3 SR15 + §10.7.2):
    /// name → the callee's bound PD-header SIGNATURE. Directly-contained children first; a COMMON program
    /// contained in an ancestor is visible too (nearest wins on a name clash, matching §10.7.2's scope) — EXCEPT from
    /// within that common program's own subtree unless it is RECURSIVE (§8.4.6.3 2); kb/Work PB1460), the exception
    /// the run-time resolver applies through the same <see cref="CobolNet.Runtime.ProgramNameScope"/>.
    /// <para>The signature carries the RETURNING item as well as the formals (kb/Work PB204): §14.9.4.3 SR25
    /// makes §14.8.3, Returning items apply to a Format-2 CALL exactly as §14.8.2 applies to its arguments, and
    /// with AS NESTED both halves of that pair are statically known. Carrying only the formals is what left the
    /// RETURNING half unchecked — the same shape as <c>UserFunctionSignature</c>, which has carried both since
    /// it was written.</para></summary>
    private static Dictionary<string, CalleeSignature> NestedCallablesOf(BoundUnit unit)
    {
        var map = new Dictionary<string, CalleeSignature>(CobolNet.Runtime.ExternalizedNames.Comparer);
        foreach (var c in unit.Children)
            map.TryAdd(c.Name, new CalleeSignature(c.Data.LinkageFormals, c.Data.LinkageReturning));
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
            foreach (var c in anc.Children)
                if (c.Common && CobolNet.Runtime.ProgramNameScope.CommonProgramReferable(c, c.Recursive, unit, u => u.Parent))
                    map.TryAdd(c.Name, new CalleeSignature(c.Data.LinkageFormals, c.Data.LinkageReturning));
        return map;
    }

    // ── The program-prototype registry (kb/Work PB237; ISO §12.3.8.2 program-specifier → §12.3.8.4 GR10) ──────
    // The PROGRAM twin of BuildUserFunctionTable below: GR10 a)/b)/c) has the identical shape to GR11 a)/b)/c),
    // and the two tables are built at the same point for the same reason — every unit's DATA has bound, so a
    // callee's PD-header signature is a fact no matter where in the group its definition sits.

    /// <summary>⛔ THE ONE UNIQUENESS CHECK OVER A COMPILATION GROUP'S DEFINITION NAMES (kb/Work PB660), in
    /// the two scopes the standard gives them.
    /// <list type="number">
    /// <item><b>The EXTERNALIZED scope — the whole group</b> (§8.3.2.2, COBOLNET2213). Its list item 1
    /// externalizes "program-names of OUTERMOST programs … and user-function-names", and the clause then states
    /// the rule twice over that one population: a CROSS-KIND pair is refused by <i>"all instances of a given
    /// name that is externalized to the operating environment shall identify the same kind of entity or
    /// item"</i>, and a SAME-KIND pair by <i>"when two or more source elements identify something with the same
    /// externalized name, they refer to the same instance"</i> — two distinct definitions cannot be ONE
    /// instance. Programs and functions are therefore ONE namespace here, not two, which is why this replaced
    /// the function-only duplicate report that used to live in <see cref="BuildUserFunctionTable"/>.</item>
    /// <item><b>The CONTAINED scope — one outermost program</b> (§8.4.6.3, COBOLNET2214): <i>"The names
    /// assigned to programs that are contained directly or indirectly within the same outermost program shall
    /// be unique within that outermost program."</i> A containee's name is not externalized at all, so its
    /// scope is its outermost program and two different outermost programs may each contain an <c>X</c>.</item>
    /// </list>
    /// <para>MEASURED BEFORE WRITING, because the note claimed the contained half was already enforced:
    /// NEITHER half was. Two outermost <c>PROGRAM-ID. P3DND.</c> definitions compiled and ran the SECOND; the
    /// same pair under one <c>AS "P3DUPX"</c> ran the FIRST; two same-named CONTAINED programs ran the first;
    /// and a program and a function sharing one name both registered and both ran. The registrar emitted
    /// <c>ProgramRegistry.Register("P3DND", …)</c> twice under one path — order-dependent, and silent.</para>
    /// <para>PROTOTYPES ARE EXCLUDED BY THE RULE, NOT FOR CONVENIENCE. §10.6.2 says so twice, once per kind:
    /// SR2 — <i>"If a compilation group contains both a program definition and a program prototype definition
    /// with the same externalized name, the signatures of these two compilation units shall be the same"</i> —
    /// and SR3, the function twin. A prototype sharing a definition's externalized name is the shape the
    /// standard legislates FOR, and §12.3.8.4 GR10 a) is what consumes it.</para></summary>
    private static void CheckDefinitionNameUniqueness(IReadOnlyList<BoundUnit> units, IReadOnlyList<OoClassUnit> classes,
                                                      OoClassTable oo, EditionContext edition)
    {
        // (1) The group-wide EXTERNALIZED namespace. Its population is §8.3.2.2's list items 1 AND 2 as far as
        //     this compiler models them: outermost program definitions, function definitions, object-class
        //     definitions and interface definitions (item 1), and the EXTERNAL data items, records and file
        //     connectors of every source element (item 2, kb/Work PB1404). A FUNCTION-ID unit is never
        //     contained, so one containment test covers both unit kinds; a class/interface definition is never a
        //     unit at all, which is why it arrives from the OO table (and is why a CLASS-ID sharing a PROGRAM-ID's
        //     externalized name used to compile clean — each namespace policed only ITSELF; the EXTERNAL items
        //     were the same hole, one list further down the same clause).
        var externalized =
            new Dictionary<string, (string Kind, string Spelling, string Word)>(CobolNet.Runtime.ExternalizedNames.Comparer);
        foreach (var (kind, spelling, word, name, at, shared) in ExternalizedDefinitions(units, classes, oo))
        {
            if (externalized.TryGetValue(name, out var first))
            {
                // §8.4.6.4 states its OWN compilation-group uniqueness for object-class-names and
                // interface-names, and the OO class table enforces it on the declared WORD (COBOLNET0820 /
                // COBOLNET0840). A pair already reported THERE is skipped here rather than doubled — but only
                // that pair: two class definitions whose words DIFFER and whose AS literals coincide are
                // §8.3.2.2's business alone, and nothing else in the compiler looks at them.
                if (IsOo(kind) && IsOo(first.Kind) && CobolNames.Same(word, first.Word)) continue;
                // Two EXTERNAL items of ONE kind under one name are the SAME INSTANCE — that sharing is what the
                // EXTERNAL clause is for ("they refer to the same instance"), and whether their descriptions agree is
                // §13.18.22.4 / EC-EXTERNAL-*'s question. Only a pair of DIFFERENT kinds conflicts.
                if (shared && first.Kind == kind) continue;
                using var _ = edition.At(at);
                string why = first.Kind == kind
                    ? $"two {kind} definitions cannot be one instance (§8.3.2.2: \"when two or more source "
                      + "elements identify something with the same externalized name, they refer to the same "
                      + "instance\")"
                    : $"{Noun(kind)} and {Noun(first.Kind)} are not the same kind of entity or item "
                      + "(§8.3.2.2: \"all instances of a given name that is externalized to the operating "
                      + "environment shall identify the same kind of entity or item\")";
                edition.Error(DiagnosticCatalog.DuplicateExternalizedDefinition,
                    $"{spelling} and {first.Spelling} both externalize the name "
                    + $"'{name}' in this compilation group — {why}");
                continue;   // the FIRST stays the survivor, so one duplicated name reports exactly once
            }
            externalized[name] = (kind, spelling, word);
        }

        // (2) The per-OUTERMOST-PROGRAM contained namespace (§8.4.6.3). "Directly or indirectly", so the
        //     walk is each outermost program's whole containment subtree, flattened onto ONE set per root.
        foreach (var root in units)
        {
            if (root.Parent is not null || root.IsFunction) continue;
            var contained = new Dictionary<string, BoundUnit>(CobolNet.Runtime.ExternalizedNames.Comparer);
            foreach (var c in Containees(root))
                if (!contained.TryAdd(c.Name, c))
                {
                    using var _ = edition.At(NameCtx(c));
                    edition.Error(DiagnosticCatalog.DuplicateContainedProgramName,
                        $"program '{c.Name}' contained in '{root.Name}' repeats the name of another program "
                        + $"contained in '{root.Name}' — the names assigned to programs that are contained "
                        + "directly or indirectly within the same outermost program shall be unique within that "
                        + "outermost program (ISO §8.4.6.3)");
                }
        }

        // The unit's own program-name token, for the diagnostic position. A CONTAINED unit's `Ctx` is the
        // SYNTHETIC programUnit context Reparent builds (`new ProgramUnitContext(null!, -1)`), whose Start
        // is null — so positioning on it silently produced a diagnostic with NO source location at all,
        // while the outermost arm of the same message carried one. One accessor, both arms.
        static ParserRuleContext? NameCtx(BoundUnit u)
        {
            var body = u.Ctx.identificationDivision()?.identificationBody();
            return (ParserRuleContext?)(body?.programIdParagraph()?.programName()
                ?? body?.functionIdParagraph()?.programName()) ?? u.Ctx;
        }

        // Every program contained directly or indirectly in `root` — §8.4.6.3's own scope words.
        static IEnumerable<BoundUnit> Containees(BoundUnit root)
        {
            foreach (var child in root.Children)
            {
                yield return child;
                foreach (var g in Containees(child)) yield return g;
            }
        }

        // §8.3.2.2's list item 1, as this compiler models it — ONE sequence, so a definition kind cannot be
        // policed by its own namespace and by nothing else. Each element carries the KIND (for the two
        // messages), the SPELLING shown to the user, the EXTERNALIZED name that is the key, and where to
        // report. The list's remaining members are not definitions in a compilation group: method-names and
        // property-names are the two §8.3.2.2 EXEMPTS from the same-instance sentence by name, and a
        // function-prototype-name / program-prototype-name names a definition elsewhere (§12.3.8.4 GR10).
        static IEnumerable<(string Kind, string Spelling, string Word, string Name, DiagnosticCursor At, bool Shared)>
            ExternalizedDefinitions(IReadOnlyList<BoundUnit> units, IReadOnlyList<OoClassUnit> classes, OoClassTable oo)
        {
            foreach (var u in units)
            {
                if (u.IsPrototype || u.Parent is not null) continue;
                string kind = u.IsFunction ? "FUNCTION-ID" : "PROGRAM-ID";
                yield return (kind, Spell(kind, u.Name, u.ExternalizedName), u.Name, u.ExternalizedName,
                              CursorOf(NameCtx(u)), false);
            }
            foreach (var c in oo.Classes)
                yield return ("CLASS-ID", Spell("CLASS-ID", c.Name, c.ExternalizedName), c.Name,
                              c.ExternalizedName, CursorOf(c.Ctx), false);
            foreach (var i in oo.Interfaces)
                yield return ("INTERFACE-ID", Spell("INTERFACE-ID", i.Name, i.ExternalizedName), i.Name,
                              i.ExternalizedName, CursorOf(i.Ctx), false);
            // §8.3.2.2 list item 2 — the EXTERNAL data items, records and files of EVERY source element: a
            // contained program's and a class's (object and factory halves) included, since the clause names the
            // item, not the unit that holds it (kb/Work PB1404). ONE producer per source element
            // (DataBinder.ExternalizedSubjects), the same one its own §13.18.22.3 SR2 screen reads.
            foreach (var data in units.Where(u => !u.IsPrototype).Select(u => u.Data)
                         .Concat(classes.SelectMany(cu => new[] { cu.Data, cu.FactoryData })))
                foreach (var s in data.ExternalizedSubjects())
                    yield return (s.Kind == ExternalizedSubjectKind.File ? "EXTERNAL file" : "EXTERNAL data item",
                                  $"EXTERNAL {s.Spelling}", s.Word, s.Name, s.At, true);
        }

        // A parse node's first token as a diagnostic position (an unset cursor leaves the current one standing).
        static DiagnosticCursor CursorOf(ParserRuleContext? ctx) =>
            ctx?.Start is { } t ? new DiagnosticCursor(t.Line, t.Column) : default;

        // How a KIND reads in the cross-kind sentence: a definition of the unit kinds, or the EXTERNAL item itself.
        static string Noun(string kind) => kind.StartsWith("EXTERNAL", StringComparison.Ordinal) ? $"an {kind}" : $"a {kind} definition";

        // The two kinds §8.4.6.4 gives their own compilation-group uniqueness sentence, and whose check
        // therefore already exists elsewhere (OoClassTable).
        static bool IsOo(string kind) => kind is "CLASS-ID" or "INTERFACE-ID";

        // How a definition is named back to the user: the DECLARED word, plus the AS literal whenever one gave
        // the externalized name a different spelling (§11.10.4 GR1 / §11.5.4 GR1 / §11.3.4 GR1 /
        // §11.6.4 GR1) — without it a clash between two different words reads as a message about one word
        // written twice.
        static string Spell(string kind, string name, string externalized) =>
            string.Equals(name, externalized, StringComparison.Ordinal)
                ? $"{kind} '{name}'"
                : $"{kind} '{name}' AS \"{externalized}\"";
    }

    /// <summary>§10.6.2 SR2 and SR3 — the SAME sentence written once per kind: "If a compilation group contains
    /// both a program definition and a program prototype definition with the same externalized name, the
    /// signatures of these two compilation units shall be the same" (SR2), and its function twin (SR3). ONE check
    /// for both, over the ONE signature comparison (<see cref="PrototypeSignatures.Same"/> — mode, OPTIONAL and
    /// §14.8.2's description per formal, and the returning item). kb/Work PB894: SR2 had no check at all (a
    /// program prototype could not be written), and SR3's was an argument-COUNT comparison keyed on the
    /// declared word rather than the externalized name the rule names.</summary>
    private static void CheckPrototypeSignaturePairs(IReadOnlyList<BoundUnit> units, EditionContext edition)
    {
        foreach (var proto in units)
        {
            if (!proto.IsPrototype) continue;
            var definition = units.FirstOrDefault(d => d is { IsPrototype: false, Parent: null }
                && d.IsFunction == proto.IsFunction && CobolNet.Runtime.ExternalizedNames.Same(d.ExternalizedName, proto.ExternalizedName));
            if (definition is null
                || PrototypeSignatures.Same(Signature(proto), Signature(definition)))
                continue;
            string kind = proto.IsFunction ? "function" : "program";
            var body = proto.Ctx.identificationDivision()?.identificationBody();
            using var _ = edition.At((ParserRuleContext?)(body?.programIdParagraph()?.programName()
                ?? body?.functionIdParagraph()?.programName()) ?? proto.Ctx);
            edition.Error("COBOLNET1513",
                $"{kind} prototype '{proto.Name}' and the {kind} definition '{definition.Name}' share the "
                + $"externalized name \"{proto.ExternalizedName}\" but not a signature — the USING formals (count, "
                + "BY REFERENCE / BY VALUE, OPTIONAL, description — a group's subordinate entries included) or the "
                + "RETURNING item differ; the signatures of these two compilation units shall be the same (ISO "
                + $"§10.6.2 {(proto.IsFunction ? "SR3" : "SR2")}; §13.7.3 SR2)");
        }

        static CalleeSignature Signature(BoundUnit u) => new(u.Data.LinkageFormals, u.Data.LinkageReturning);
    }

    /// <summary>The IN-GROUP half of §12.3.8.4 GR10 — externalized name → the calling details a REPOSITORY
    /// program-specifier takes from this compilation group. GR10 a) (a program DEFINITION) and GR10 b) (a program
    /// PROTOTYPE definition, §11.10.2 Format 2 — kb/Work PB894) have the SAME consequence, "the details are taken
    /// from" that unit, so they are ONE table: definitions are registered first and a prototype only fills a
    /// name no definition holds, which is exactly a)'s "otherwise" precedence. A name in neither is GR10 c), the
    /// external repository — this implementation's run-unit program registry.
    /// <para>The compilation group's program definitions by EXTERNALIZED name — the search space of ISO
    /// §12.3.8.4 general rule 10 a): "if the externalized name of the program prototype is the externalized name
    /// of a program definition specified previously in the same compilation group, the details are taken from that
    /// program definition, which is the program that will be called".</para>
    /// <para>OUTERMOST program definitions only: a contained program is part of its container's program
    /// definition, is not a compilation-group source unit (§10.6.1), and is reachable only through §14.9.4.3 SR15's
    /// AS NESTED — which has its own table. FUNCTION-ID units are excluded because §9.4 puts them in the function
    /// namespace, and prototype units because they have no body. The key is <c>BoundUnit.ExternalizedName</c>
    /// — GR10 a) says "externalized name" twice and §11.10.4 GR1 makes that the AS literal when one is written
    /// (kb/Work PB303; before the phrase parsed, MakeUnit collapsed it onto <c>Name</c>).</para>
    /// <para>DETERMINATION on GR10 a)'s word "previously": the ORDER is not enforced. GR10 a) and c) are the two
    /// arms this implementation can reach, and for a later in-group definition both name the SAME program — c)
    /// takes "the details … from the external repository for the program with the same name", and this
    /// implementation's external repository is the run unit's program registry, which the later definition is in.
    /// Enforcing the order would therefore reject nothing illegal and would only DOWNGRADE a later definition's
    /// signature from a compile-time §14.8.2 check to a run-time EC-PROGRAM-ARG-MISMATCH.</para></summary>
    private static Dictionary<string, CalleeSignature> BuildProgramDetailsTable(IReadOnlyList<BoundUnit> units)
    {
        var map = new Dictionary<string, CalleeSignature>(CobolNet.Runtime.ExternalizedNames.Comparer);
        foreach (bool prototypes in (bool[])[false, true])
            foreach (var u in units)
                if (u is { IsFunction: false, Parent: null } && u.IsPrototype == prototypes)
                    map.TryAdd(u.ExternalizedName, new CalleeSignature(u.Data.LinkageFormals, u.Data.LinkageReturning));
        return map;
    }

    /// <summary>The program prototypes ONE source element may name (ISO §8.4.6.8, Scope of program-prototype-names:
    /// "Program-prototype-names referenced within a source element shall be either the program-name of a
    /// containing program definition or a program-prototype-name declared in the REPOSITORY paragraph").
    /// Both spellings register here, so §14.9.4.3 SR16 and §14.9.5.3 SR3 are ONE lookup:
    /// <list type="number">
    /// <item>every §12.3.8.2 program-specifier visible to the element (<c>DataBinder.ProgramSpecifiers</c>, which
    /// already inherits its container's — §12.3.8.4 GR10's "scope of the containing environment division"; for a
    /// CLASS's OBJECT or FACTORY forest, the class-level REPOSITORY, which §12.3.4 GR1 applies to every method —
    /// kb/Work PB1100), resolved through GR10 against <paramref name="programDefinitions"/>;</item>
    /// <item>for a program <paramref name="unit"/>, its own name and every containing program definition's name,
    /// which §8.4.6.8 admits with no specifier at all — and which is also exactly what §12.3.8.3 syntax rule 15 means
    /// by "references to program-prototype-name-1 are to the named program definition and this program-specifier is
    /// ignored": a self- or container-named specifier resolves to that definition, so registering the definition
    /// LAST and letting it overwrite realizes SR15 without a second code path. A class forest
    /// (<paramref name="unit"/> null) has neither: a class is not a program definition and contains none.</item>
    /// </list></summary>
    internal static Dictionary<string, ProgramPrototype> ProgramPrototypesOf(
        DataBinder data, BoundUnit? unit, IReadOnlyDictionary<string, CalleeSignature> programDefinitions)
    {
        var map = new Dictionary<string, ProgramPrototype>(CobolNames.Comparer);
        foreach (var (name, spec) in data.ProgramSpecifiers)
            // GR10 a) / b): the in-group definition — else the in-group program PROTOTYPE definition (kb/Work
            // PB894) — supplies the details; BuildProgramDetailsTable already layered the two in that order.
            // Otherwise GR10 c) — the external repository, i.e. this implementation's run-unit program registry,
            // resolved at execution (§14.9.4.4 GR3 b)) — so the prototype is legal and simply carries no
            // compile-time signature.
            map[name] = new ProgramPrototype(name, spec.ExternalizedName,
                programDefinitions.GetValueOrDefault(spec.ExternalizedName));
        // §8.4.6.8's second spelling: "the program-name of a containing program definition" is a referable
        // program-prototype-name with NO specifier at all. It also subsumes §12.3.8.3 SR15's containing-program
        // half — a specifier naming a container is "ignored" and references go to that definition, which is
        // precisely what overwriting the specifier's entry with the definition does.
        for (var u = unit?.Parent; u is not null; u = u.Parent)
            if (!u.IsFunction) map[u.Name] = SelfPrototype(u);
        // §12.3.8.3 SR15's OTHER half — "the name of the program definition in which this REPOSITORY paragraph is
        // specified". Scoped to a WRITTEN specifier, because SR15 is a rule about a specifier and §8.4.6.8's
        // no-specifier spelling says "containing", never "this". (§11.10.4 GR4's RECURSIVE attribute is what makes
        // such a self-call reachable at run time; without it §14.9.4.4 GR3 f) raises EC-PROGRAM-RECURSIVE-CALL.)
        if (SpecifiesItself(unit, data.ProgramSpecifiers, function: false))
            map[unit!.Name] = SelfPrototype(unit);
        return map;

        // The prototype NAME is the referable word (§8.4.6.8), its EXTERNALIZED name is what CallBinder
        // emits as the activation target (§14.9.4.4 GR3 b) → §8.3.2.2) — so the two arguments are the unit's
        // two names, never the same one twice: a `PROGRAM-ID. P AS "PX".` that CALLs itself by the word P
        // must still activate "PX", the only name the run-unit registry holds it under (kb/Work PB303).
        static ProgramPrototype SelfPrototype(BoundUnit u) =>
            new(u.Name, u.ExternalizedName, new CalleeSignature(u.Data.LinkageFormals, u.Data.LinkageReturning));
    }

    /// <summary>⛔ THE ONE SELF-SPECIFIER RULE, asked by both prototype arms (kb/Work PB1084). §12.3.8.3 states it
    /// once per specifier kind — SR11: "If the specified function-prototype-name-1 is the name of the function
    /// definition in which this REPOSITORY paragraph is specified, references to function-prototype-name-1 are to
    /// that function definition and this function-specifier is ignored"; SR15, the same sentence for a
    /// program-prototype-name-1 and its program definition — so a REPOSITORY entry naming the element it is written
    /// in never redirects that element's references, whatever its <c>AS</c> literal says. True when
    /// <paramref name="unit"/> is a definition of the <paramref name="function"/> kind and
    /// <paramref name="specifiers"/> names it. The FUNCTION arm used to skip this test and apply
    /// <c>FUNCTION FACT AS "OTHERF"</c> inside FUNCTION-ID. FACT as a remap, so the recursive self-call bound
    /// against OTHERF's signature (COBOLNET1506) while the program arm was right.</summary>
    private static bool SpecifiesItself<T>(BoundUnit? unit, IReadOnlyDictionary<string, T> specifiers, bool function)
        => unit is not null && unit.IsFunction == function && specifiers.ContainsKey(unit.Name);

    /// <summary>The user-defined functions ONE source element may reference, by the function-prototype-name it
    /// WRITES — the function twin of <see cref="ProgramPrototypesOf"/> (kb/Work PB974). A specifier with no AS phrase
    /// names the group's function of that word (the <paramref name="group"/> table's key, unchanged). A specifier
    /// <c>FUNCTION name AS literal-5</c> names the function whose EXTERNALIZED name is literal-5 — §12.3.8.4 GR11
    /// NOTE 2: "Literal-5, if specified, is the externalized name of the function prototype" — searched per GR11 a)
    /// / b) over the group's definitions and then its prototypes (the table already holds a definition in place of
    /// its same-name prototype). No match leaves the name unmapped: GR11 c)'s external repository, which this
    /// implementation holds no compile-time signature for, and the reference draws COBOLNET1505 at its use.
    /// A specifier naming the function definition it is written in is IGNORED (§12.3.8.3 SR11,
    /// <see cref="SpecifiesItself{T}"/>) and the name resolves to that definition's own signature — registered LAST,
    /// as <see cref="ProgramPrototypesOf"/> registers its self entry. <paramref name="data"/> is a program unit's
    /// forest or a CLASS's OBJECT / FACTORY forest (whose specifiers are the class REPOSITORY's — §12.3.4 GR1,
    /// kb/Work PB1100; <paramref name="unit"/> is then null). A forest with no remapping specifier shares the group
    /// table (no copy).</summary>
    internal static IReadOnlyDictionary<string, UserFunctionSignature> UserFunctionsOf(
        DataBinder data, BoundUnit? unit, IReadOnlyDictionary<string, UserFunctionSignature> group)
    {
        bool selfSpecified = SpecifiesItself(unit, data.FunctionSpecifiers, function: true);
        Dictionary<string, UserFunctionSignature>? own = null;
        foreach (var (name, externalized) in data.FunctionSpecifiers)
        {
            if (string.Equals(name, externalized, StringComparison.Ordinal)) continue;
            own ??= new Dictionary<string, UserFunctionSignature>(group, CobolNames.Comparer);
            var target = group.Values.FirstOrDefault(f => CobolNet.Runtime.ExternalizedNames.Same(f.Externalized, externalized));
            if (target is null) own.Remove(name);
            else own[name] = target;
        }
        if (selfSpecified)
        {
            own ??= new Dictionary<string, UserFunctionSignature>(group, CobolNames.Comparer);
            own[unit!.Name] = new UserFunctionSignature(unit.Name, unit.ExternalizedName,
                unit.Data.LinkageReturning, unit.Data.LinkageFormals);
        }
        return own ?? group;
    }

    /// <summary>Build the compilation group's user-function signature table (name → bound RETURNING item +
    /// USING formals), between the DATA and PROCEDURE bind phases: FUNCTION-ID units only (ISO §9.4 — the
    /// binder's function namespace never sees PROGRAM-ID units; §8.4.6.6 scope of function-prototype-names).
    /// The §14.2 procedure-division-header rule "The RETURNING phrase shall be specified in a function
    /// definition" (:23666) is checked HERE, once per unit — even an uncalled function without RETURNING is
    /// ill-formed.</summary>
    private static Dictionary<string, UserFunctionSignature> BuildUserFunctionTable(
        IReadOnlyList<BoundUnit> units, EditionContext edition)
    {
        // Partition the group's FUNCTION-ID units by name into DEFINITIONS (a real body) and PROTOTYPES
        // (signature-only, §11.5 Format 2). A prototype precedes all other units (§10.6.2 SR1), so a naive
        // first-wins TryAdd would false-report the FOLLOWING same-name definition as a duplicate (1508) — the
        // partition prevents that. Every function unit must carry a RETURNING (§14.2 :23666) — checked once here.
        var defs = new Dictionary<string, BoundUnit>(CobolNames.Comparer);
        var protos = new Dictionary<string, BoundUnit>(CobolNames.Comparer);
        foreach (var u in units)
        {
            if (!u.IsFunction) continue;
            if (u.Data.LinkageReturning is null)
                edition.Error("COBOLNET1507",
                    $"FUNCTION-ID '{u.Name}': the RETURNING phrase shall be specified in a function {(u.IsPrototype ? "prototype" : "definition")} "
                    + "(ISO §14.2, procedure division header) — the function cannot deliver a result without it");
            // The duplicate report moved (kb/Work PB660). It used to key on the WORD and cite §8.4.6.6,
            // which is the scope of function-prototype-NAMES and says nothing about uniqueness — a real
            // clause answering a different question. Two function DEFINITIONS collide because they
            // externalize one name (§8.3.2.2), the same sentence two outermost PROGRAM definitions
            // collide under, so ONE check reports both: CheckDefinitionNameUniqueness, run before this.
            // COBOLNET1508 survives for the clash §8.4.6.7 does own — a REPOSITORY `FUNCTION word` entry
            // names the user-function-NAME, so two definitions sharing a word are ambiguous even when
            // their AS literals differ — and for PROTOTYPE units, which the externalized check excludes
            // (§10.6.2 SR3 pairs a function prototype with a same-name definition on purpose).
            var bucket = u.IsPrototype ? protos : defs;
            if (bucket.TryGetValue(u.Name, out var firstSameWord))
            {
                if (u.IsPrototype || !CobolNet.Runtime.ExternalizedNames.Same(u.ExternalizedName, firstSameWord.ExternalizedName))
                    edition.Error("COBOLNET1508",
                        $"duplicate FUNCTION-ID '{u.Name}' in the compilation group — two function "
                        + $"{(u.IsPrototype ? "prototypes" : "definitions")} share one user-function-name, which "
                        + "a REPOSITORY paragraph entry can no longer name unambiguously (ISO §8.4.6.7: \"A "
                        + "user-function-name may be referenced in the REPOSITORY paragraph of any source element "
                        + "that follows that function definition within the compilation group\")");
            }
            else bucket[u.Name] = u;
        }

        // §12.3.8 GR11(a) — an in-group DEFINITION is authoritative over a same-name PROTOTYPE (:14871); a lone
        // prototype supplies the signature for a separately-compiled target (:14875 / §8.4.3.2.4 GR6b :6997).
        var table = new Dictionary<string, UserFunctionSignature>(CobolNames.Comparer);
        foreach (var (name, u) in defs)
            table[name] = new UserFunctionSignature(name, u.ExternalizedName, u.Data.LinkageReturning, u.Data.LinkageFormals);
        foreach (var (name, p) in protos)
        {
            // §10.6.2 SR3's same-signature obligation is CheckPrototypeSignaturePairs' (both kinds, one test).
            if (defs.ContainsKey(name)) continue;   // the definition's signature is authoritative (GR11a)
            table[name] = new UserFunctionSignature(name, p.ExternalizedName, p.Data.LinkageReturning, p.Data.LinkageFormals);
        }
        return table;
    }

    /// <summary>Qualify a class's OBJECT/FACTORY file connectors into the run-unit registry namespace (M2-OO-1i —
    /// the OO analogue of the per-program qualification in <see cref="Bind"/>). A FACTORY file (the class
    /// singleton, §9.3.14.2) keys by class — <c>Class::FACT::name</c>; an EXTERNAL class file keys by its run-unit
    /// external name (§13.18.22.4 GR4a — one connector shared by every describer, inc 5). An OBJECT (instance) file
    /// is per-object (inc 4): a class-qualified BASE key plus a minted per-object <c>__fkey</c> field. Name
    /// resolution is done (bound nodes hold FileModel references), so this is a pure rename. (Relocated from the
    /// emitter, P6 Step 5 — a Bind-phase FileModel mutation.)</summary>
    private static void QualifyClassFiles(OoClassUnit cls)
    {
        // OBJECT (instance) files: one connector per object (§9.1.4). A non-EXTERNAL file keeps a class-qualified
        // BASE key (the seed MintInstanceKey suffixes with a per-object #N) and a minted-key FIELD; an EXTERNAL
        // instance file keys by its run-unit external name like any describer (§13.18.22.4 GR4a — inc 5).
        foreach (var f in cls.Data.Files)
            if (f is { IsExternal: true, ExternalName: { } ext })
                f.CobolName = NamingConvention.ExternalFileBand + ext;
            else if (f.IsSortMerge)
                // An SD is NOT a host connector — its store is the name-keyed in-memory CobolSort (§13.4.6), and
                // OoEmitFileMembers / EmitFileRegistration both skip SDs (host = !IsSortMerge). So it must keep a
                // STATIC key (no InstanceKeyField), or FileKeyExpr would emit an undeclared this.__fkey_X for a
                // SORT/MERGE/RELEASE/RETURN in a method (M2-OO-1i review). Class-qualified for cross-class uniqueness.
                f.CobolName = cls.CsName + "::SORT::" + f.CobolName;
            else
            {
                f.InstanceKeyField = NamingConvention.InstanceFileKeyName(f.CobolName);
                f.CobolName = cls.CsName + NamingConvention.InstanceFileBand + f.CobolName;
            }
        // FACTORY files: the class singleton (§9.3.14.2) — a static class-qualified key (no per-object field).
        foreach (var f in cls.FactoryData.Files)
            f.CobolName = f is { IsExternal: true, ExternalName: { } ext }
                ? NamingConvention.ExternalFileBand + ext
                : cls.CsName + NamingConvention.FactoryFileBand + f.CobolName;
    }
}
