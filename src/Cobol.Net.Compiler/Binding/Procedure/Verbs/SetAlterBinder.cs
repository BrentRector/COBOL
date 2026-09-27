// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Editions.Diagnostics;
using CobolNet.Binding.Bound;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The ALTER + SPECIAL-NAMES-switch binder (P7 Step 10n — the LAST plain verb, exactly ONE
/// instance per unit: the lazy whole-program ALTER prepass latches <c>_alterSwFields</c> against the
/// COMPLETE procedure table, which it reads through host edges (Paragraphs/ParaSections/CurrentSection —
/// the §8.4.2.2 in-section-first save/mutate/restore preserved verbatim; the ALTER prepass reads the table
/// through <c>ctx.Table</c> since the 10t ProcedureTableBuilder hoist). <c>SwitchCondOf</c> keeps the
/// level-88-first caller contract (NC211A). The five bound types stayed in
/// <c>Binding/Bound/BoundAlterSwitches.cs</c> — BoundAlter/BoundGoToAlterable are VersionConformancePass
/// gate anchors.</summary>
internal sealed class SetAlterBinder(BinderContext ctx)
{
    // ── ALTER + the 85-only target-less GO TO (ANSI X3.23-1985; deleted by ISO/IEC 1989:2002) ────────────────

    /// <summary>ALTER-target paragraph pc → its generated <c>_alter_&lt;para&gt;</c> field (D4), from the lazy
    /// whole-program prepass (<see cref="AlterEnsureScan"/>).</summary>
    private Dictionary<int, string>? _alterSwFields;

    /// <summary>Paragraph parse context → its pc, so a GO TO locates its OWNING paragraph without re-walking.</summary>
    private Dictionary<Core.SentenceContext, int>? _alterSwParaPc;   // sentence -> owning paragraph pc

    /// <summary>The whole-program ALTER prepass (port of the legacy <c>ScanAlterTargets</c> BEHAVIOR): collect
    /// every <c>ALTER proc-1 TO …</c> target paragraph and assign it its D4 field, BEFORE any GO TO binds — a GO TO
    /// in a target paragraph must bind alterable regardless of whether its ALTER appears earlier or later in the
    /// source. Runs once, on first demand (after <c>CollectParagraphs</c>, so the procedure table is complete);
    /// each target name resolves with the ALTER site's own section context (ISO §8.4.2.2 in-section-first).</summary>
    private void AlterEnsureScan()
    {
        if (_alterSwFields is not null) return;
        _alterSwFields = new Dictionary<int, string>();
        _alterSwParaPc = new Dictionary<Core.SentenceContext, int>();
        for (int i = 0; i < ctx.Table.Paragraphs.Count; i++)
            foreach (var sent in ctx.Table.Paragraphs[i].Sentences) _alterSwParaPc[sent] = i;

        var saved = ctx.CurrentSection;
        for (int i = 0; i < ctx.Table.Paragraphs.Count; i++)
            foreach (var al in ctx.Table.Paragraphs[i].Sentences.SelectMany(AlterStatementsIn))
                foreach (var entry in al.alterEntry())
                {
                    if (entry.procedureName() is not { Length: >= 2 } names) continue;
                    ctx.CurrentSection = ctx.Table.ParaSections[i];
                    // proc-1 names a PARAGRAPH (a section resolves to a multi-pc range and is excluded; the
                    // sole-GO-TO shape check happens at the ALTER's own bind, where it can fail loud).
                    // The QUIET resolution, deliberately: this is a PRESCAN, and the ALTER's own bind reports
                    // the unresolvable name once, through the ONE operand resolution (kb/Work PB390).
                    if (ctx.Table.ResolveProcedureQuiet(names[0]) is { IsParagraph: true } t)
                        // Method "P_<name>" → field "_alter_<name>" (D4); COBOL names cannot start with '_',
                        // so the field can never collide with a data item's emitted field.
                        _alterSwFields.TryAdd(t.Start, "_alter_" + ctx.Table.Paragraphs[t.Start].Method[2..]);
                }
        ctx.CurrentSection = saved;
    }

    /// <summary>Every <c>alterStatement</c> context under <paramref name="node"/> (ALTER is an imperative
    /// statement — it may sit inside IF branches etc., so the scan is a full subtree walk).</summary>
    private static IEnumerable<Core.AlterStatementContext> AlterStatementsIn(IParseTree node)
    {
        if (node is Core.AlterStatementContext al) { yield return al; yield break; }
        for (int i = 0; i < node.ChildCount; i++)
            foreach (var inner in AlterStatementsIn(node.GetChild(i)))
                yield return inner;
    }

    /// <summary>The pc of the paragraph lexically containing <paramref name="node"/> (walk to the enclosing
    /// <c>paragraphDefinition</c>), or null outside any collected paragraph. Requires <see cref="AlterEnsureScan"/>.</summary>
    private int? AlterOwningPc(IParseTree node)
    {
        for (IParseTree? n = node; n is not null; n = n.Parent)
            if (n is Core.SentenceContext p && _alterSwParaPc!.TryGetValue(p, out int pc))
                return pc;
        return null;
    }

    /// <summary>Bind a resolved single-target GO TO: in an ALTER-target paragraph it transfers to the CURRENT
    /// altered target with the WRITTEN target as the field's initial value (ANSI-85 ALTER GR — until an ALTER
    /// executes, the written GO TO governs); otherwise it is the plain §14.9.17 Format 1 transfer.</summary>
    public BoundStatement AlterGoTo(Core.GoToStatementContext g, int writtenTarget)
    {
        AlterEnsureScan();
        return AlterOwningPc(g) is { } pc && _alterSwFields!.TryGetValue(pc, out var field)
            ? new BoundGoToAlterable(field, writtenTarget, ctx.SourceLine(g))
            : new BoundGoTo(writtenTarget, ctx.SourceLine(g));
    }

    /// <summary>Bind the target-less <c>GO TO.</c> (ANSI X3.23-1985 only — obsolete there, DELETED by ISO/IEC
    /// 1989:2002; §14.9.17 of the 2023 standard requires procedure-name-1): it may appear only in a single-GO-TO
    /// paragraph referenced by an ALTER and must be ALTERed before execution, else execution is UNDEFINED — bound
    /// as the alterable transfer with default −1 (dispatcher exit) when ALTERed somewhere, or a constant −1
    /// transfer when no ALTER ever names the paragraph (the legacy's NIST-proven realization of "undefined").</summary>
    public BoundStatement AlterBindBareGoTo(Core.GoToStatementContext g)
    {
        // ⛔ NO MALFORMED-SHAPE ARM HERE ANY MORE (kb/Work PB412). This method used to open with
        // `if (g.dataReference() is not null) return new BoundUnsupported("GO TO DEPENDING without
        // procedure-names …")` — a shape §14.9.17.2 prints no format for, answered by EMITTING A PROGRAM that
        // aborts at run time on the loud stage. `goToStatement` now spells one alternative per printed format,
        // so that shape is a syntax error in the parser and this arm was unreachable code pinning the wrong
        // stage for an illegal source construct.
        // The bare-GO-TO removal gate (BareGotoRemoved2002) fires on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitGoToStatement, GoToFormat.AnsiAlterable — the same classifier this method's caller
        // dispatched on); Step 14h.4a.
        // At 85 the construct is an OBSOLETE element: accepted with no failing diagnostic (the obsolete-element
        // flag awaits the EditionContext warning channel — it must not fail the 85 compile).
        AlterEnsureScan();
        return AlterOwningPc(g) is { } pc && _alterSwFields!.TryGetValue(pc, out var field)
            ? new BoundGoToAlterable(field, -1, ctx.SourceLine(g))
            : new BoundGoTo(-1, ctx.SourceLine(g));
    }

    /// <summary>Bind <c>ALTER {proc-1 TO [PROCEED TO] proc-2}…</c> (ANSI X3.23-1985; 85-ONLY — rejected at 2002+
    /// as a deleted element). Each proc-1 shall be a paragraph consisting of a single sentence that is exactly one
    /// GO TO Format 1 (with or without a written target); execution replaces that GO TO's transfer target with
    /// proc-2 (a section proc-2 transfers to its first paragraph, the §14.9.17 GR1 GO TO rule).</summary>
    public BoundStatement BindAlter(Core.AlterStatementContext al)
    {
        // ALTER was REMOVED by ISO 2002; the edition gate moved to the post-bind VersionConformancePass
        // (PHASE-03 Step 14b), firing on the self-identifying BoundAlter node. At 85: obsolete element, accepted
        // with no failing diagnostic (warning channel pending, as above).
        AlterEnsureScan();
        var entries = new List<BoundAlterEntry>();
        bool bad = false;   // screen EVERY entry, so a second bad one is not hidden by the first
        foreach (var entry in al.alterEntry())
        {
            if (entry.procedureName() is not { Length: >= 2 } names)
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, $"ALTER '{entry.GetText()}': each ALTER entry is procedure-name-1 TO "
                    + "[PROCEED TO] procedure-name-2 (X3.23-1985 ALTER statement)");
            // ⛔ BOTH ALTER OPERANDS ARE procedure-names AND GO THROUGH THE ONE RESOLUTION (kb/Work PB390):
            // an unresolvable name is reported at COMPILE time, not staged to a run-time abort blaming a gap in
            // WiseOwl COBOL. proc-1 additionally has to be a PARAGRAPH (a section resolves to a multi-pc range and
            // the ALTER shape rule is about a single GO TO sentence), so a RESOLVED section takes the shape arm
            // below rather than the name arm — telling the user "that is a section" beats "unknown procedure".
            if (ctx.Table.ResolveProcedureOperand(names[0], "ALTER") is not { } target) { bad = true; continue; }
            if (!target.Range.IsParagraph || !AlterIsSoleGoToParagraph(target.Range.Start))
            {
                ctx.Validation.RejectStatementOperand($"ALTER '{names[0].GetChild(0).GetText()}' — the procedure "
                    + "to be altered shall be a paragraph consisting of a single sentence that is one GO TO "
                    + "statement (ANSI X3.23-1985 ALTER syntax rule)"
                    + (target.Range.IsParagraph ? "" : "; this name resolves to a SECTION"));
                bad = true;
                continue;
            }
            if (ctx.Table.ResolveProcedureOperand(names[1], "ALTER TO PROCEED TO") is not { } dest)
            { bad = true; continue; }
            entries.Add(new BoundAlterEntry(_alterSwFields![target.Range.Start], dest.Range.Start));
        }
        return bad ? BoundRejected.Reported(ctx.Edition) : new BoundAlter(entries);
    }

    /// <summary>True when paragraph <paramref name="pc"/> consists of a SINGLE sentence whose only statement is a
    /// GO TO Format 1 — written target or target-less, never DEPENDING (the ANSI-85 ALTER shape requirement).</summary>
    private bool AlterIsSoleGoToParagraph(int pc)
    {
        var sentences = ctx.Table.Paragraphs[pc].Sentences;
        if (sentences.Length != 1) return false;
        var stmts = sentences[0].statement();
        // The format question goes to the ONE classifier (kb/Work PB412) — never re-derived from the optional
        // children here. Format 1 and the target-less ANSI-85 arm are both alterable; Format 2 is not.
        return stmts.Length == 1 && stmts[0].goToStatement() is { } g
            && GoToFormats.Of(g) is not GoToFormat.Depending;
    }

    // ── SPECIAL-NAMES external switches: SET Format 3 + the switch-status condition (ISO §12.3.7) ────────────

    /// <summary>The switch-status condition a bare unsubscripted reference names (ISO §8.8.4.6; §8.4.4.2 Format 1
    /// SR1 — the condition-name shall be associated with a switch-name in SPECIAL-NAMES), else null. The CALLER
    /// must have tried level-88 resolution FIRST (<see cref="ConditionOf"/>): a name defined as both resolves as
    /// the level-88 (legacy resolution order; NC211A regression guard).</summary>
    public BoundCondition? SwitchCondOf(Core.DataReferenceContext dref)
    {
        if (dref.dataReferenceSuffix().Length != 0) return null;   // a status condition-name takes no subscript/qualifier
        return dref.cobolWord()?.GetText() is { } name && ctx.Data.SwitchConditions.TryGetValue(name, out var sc)
            ? new BoundSwitchCondition(sc.ImplementorName, sc.IsOn)
            : null;
    }

    /// <summary>The external switch a reference names the ON/OFF STATUS of, else null — asked for the
    /// DIAGNOSTIC's sake rather than for a bound condition (kb/Work PB390): §14.9.39.3 SR6's discriminating
    /// operand is a condition-name associated with a switch instead of a conditional variable, and a message
    /// can only say so if it can tell the two apart. It goes THROUGH <see cref="SwitchCondOf"/> rather than
    /// re-reading <c>SwitchConditions</c>, so "what is a switch-status condition-name" stays one predicate.</summary>
    public string? SwitchNameOf(Core.DataReferenceContext dref) =>
        SwitchCondOf(dref) is BoundSwitchCondition sw ? sw.ImplementorName : null;

    /// <summary>Bind <c>SET {{mnemonic-name-1}… TO {ON|OFF}}…</c> (ISO §14.9.39 Format 3).
    /// <para>⛔ THE GROUPS ARE READ, NOT RE-DERIVED (kb/Work PB450). The grammar used to write the printed outer
    /// repetition as an inline <c>SET (dataReference+ TO (ON|OFF))+</c> group, which flattens every phrase into
    /// one <c>dataReference()</c> list and one <c>TO()</c> list — so this method reassembled the grouping by
    /// comparing token indices, and the LEGACY binder carried a second hand-written copy of the same
    /// re-assembly. <c>setSwitchPhrase</c> is now the printed unit, so a phrase IS a node and the loop is the
    /// rule: one group, its receivers, its ON/OFF.</para>
    /// <para>Every receiver must name a settable external switch's mnemonic (SR5) — an unresolvable name fails
    /// loud, never a silent skip.</para>
    /// <para>⛔ THE OPERAND IS LOOKED UP IN THE ONE MNEMONIC REGISTRY and its row's KIND decides (kb/Work PB454).
    /// This used to read a second, switch-only map (<c>DataBinder.SwitchMnemonics</c>) built by a second walk of
    /// the same SPECIAL-NAMES entries, and that second walk is where an Option-2 switch clause (status phrases,
    /// no mnemonic) once entered the switch-NAME itself as a mnemonic, so <c>SET SWITCH-1 TO ON</c> bound. One
    /// walk (<see cref="MnemonicRegistry"/>, which also carries the §12.3.4 GR1 inheritance into contained units
    /// and the OO scopes) now answers every mnemonic question — SET's, WRITE ADVANCING's, ACCEPT/DISPLAY's and the
    /// condition binder's — so the switch/device/feature distinction is decided in one place.</para></summary>
    public BoundStatement SwitchBindSet(Core.SetSwitchStatementContext sw)
    {
        var switches = new List<(string Name, bool On)>();
        bool bad = false;
        foreach (var phrase in sw.setSwitchPhrase())
        {
            bool on = phrase.ON() is not null;
            foreach (var dref in phrase.dataReference())
            {
                string name = dref.cobolWord()?.GetText() ?? dref.GetText();
                if (!ctx.Mnemonics.Of(dref).TryGetValue(name, out var row) || row.Kind != SystemNameKind.Switch)
                {
                    // SR5 is decided here, so it is REPORTED here (kb/Work PB390 — the Format-3 sibling of the
                    // Format-4 condition-name rule; both used to ship as a run-time "not implemented" abort).
                    ctx.Validation.RejectStatementOperand($"SET '{name}' TO {(on ? "ON" : "OFF")} — "
                        + "\"mnemonic-name-1 shall be associated with an external switch, the status of which may "
                        + "be altered\" (ISO §14.9.39.3 SR5), and "
                        + (row is not null
                            ? $"'{name}' is the mnemonic-name of the {ImplementorNames.KindWord(row.Kind)} '{row.Name}'"
                            : ImplementorNames.Lookup(name) is { } named
                                ? $"'{name}' is itself a {ImplementorNames.KindWord(named.Kind)}, an implementor-name "
                                  + "(ISO §8.3.2.3.11), not a mnemonic-name — the SPECIAL-NAMES entry "
                                  + $"'{named.Name} IS mnemonic-name' declares the name SET takes"
                                : $"no SPECIAL-NAMES paragraph in this source element associates '{name}' with a switch-name"));
                    bad = true;
                    continue;   // screen EVERY receiver: two bad mnemonics draw two diagnostics
                }
                switches.Add((row.Name, on));
            }
        }
        return bad ? BoundRejected.Reported(ctx.Edition) : new BoundSetSwitches(switches);
    }
}
