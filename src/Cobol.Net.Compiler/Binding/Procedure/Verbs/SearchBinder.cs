// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SEARCH / SEARCH ALL verb binder (P7 Step 10m — a real collaborator over
/// <see cref="BinderContext"/>, on the shared <c>BoundSearch</c> machinery; the scope-aware index-cell
/// is the table's own declaration (<c>DataItem.Indexes</c>, kb/Work PB919), the dynamic-table bound <c>OdoModel.SearchBound</c>).</summary>
internal sealed class SearchBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>⛔ IDENTIFIER-1, FOR BOTH FORMATS, RESOLVED AND SCREENED IN ONE PLACE (kb/Work PB443). Format 1
    /// and Format 2 print the SAME operand and are bound by the SAME three ALL-FORMATS syntax rules, and they had
    /// two copies of the same eight lines — the literal shape of this project's most reproducible defect, a
    /// dispatch with two arms of which only one ever gets fixed.
    /// <para>What those eight lines did: <c>dref.cobolWord()</c> reduced the whole written reference to its BASE
    /// WORD, so the qualifiers, the subscripts and any reference modifier were discarded UNREAD, and
    /// <c>candidates.FirstOrDefault(i =&gt; i.IsTable)</c> then broke a same-name tie by DECLARATION ORDER.
    /// <c>SEARCH E IN G2</c>, with a table <c>E</c> in each of two groups, searched <c>G1</c>'s — a silent wrong
    /// answer on legal, unambiguous COBOL — and none of §14.9.37.3 SR1–SR3 could even be asked, because nothing
    /// downstream ever saw a subscript or a modifier at all. The resolution is now the ordinary §8.4.2.2 one
    /// (<c>ReferenceResolver.ResolveTableOperand</c>) and the rules are the one check catalog's.</para>
    /// <para>The nested-dynamic guard stays HERE and stays a <c>BoundUnsupported</c>: it is a genuine WiseOwl COBOL
    /// increment, not a rule the source violates (a subscripted capacity path over the enclosing indices), which
    /// is exactly the job PB236 left the carrier.</para></summary>
    /// <param name="dref">identifier-1 as written.</param>
    /// <param name="verb">"SEARCH" or "SEARCH ALL".</param>
    /// <param name="bail">What to bind INSTEAD when this returns null: a <c>BoundRejected</c> after a reported syntax
    /// rule (the compile has already failed — ISO §4.2.2 ¶2), or the nested-dynamic deferral.</param>
    /// <returns>The table identifier-1 names, or null.</returns>
    private DataItem? Identifier1(Core.DataReferenceContext dref, string verb, out BoundStatement bail)
    {
        bail = null!;   // assigned on each null return below — a refusal is put on the ledger only when it is one (PB1029)
        if (ctx.Validation.ResolveSearchTable(dref, verb, ctx.Refs) is not { } table)   // fixed OR dynamic (D9)
        {
            bail = BoundRejected.Reported(ctx.Edition);
            return null;
        }
        // A dynamic table NESTED under another table has no whole-table path (BuildTablePath null), so the AT-END bound
        // (§8.5.1.9.1 current capacity) and the EnterSearch/ExitSearch bracket cannot be addressed by name — a
        // subscripted capacity path over the enclosing indices is a later increment. Reject rather than let
        // SearchBound fall back to Count=0 and silently scan ZERO occurrences (OCCURS DYNAMIC review #5; D9).
        if (table.IsDynamicTable && ReferenceResolver.BuildTablePath(table) is null)
        {
            bail = new BoundUnsupported($"{verb} of the dynamic-capacity table '{table.CobolName}' nested under "
                + "another table (the scan bound over its current capacity needs a subscripted access path — a "
                + "later increment)");
            return null;
        }
        return table;
    }

    /// <summary>The ALL-FORMATS phrases both formats print identically — <c>[ AT END imperative-statement-1 ]</c>
    /// and the WHEN arms' <c>{ imperative-statement-2 | NEXT SENTENCE }</c> under an optional END-SEARCH — bound
    /// in ONE place so the two arms cannot disagree (feedback_two_arm_dispatch). §14.9.37.3 SR4 is asked here
    /// for both formats. The AT END phrase is exactly the printed one: the NOT AT END phrase neither format
    /// prints is refused by the GRAMMAR now (kb/Work PB446 — it used to parse, and a helper here refused it).
    /// A NEXT SENTENCE arm binds through the statement funnel, which admits it only as the WHOLE arm
    /// (<see cref="StatementBinder.IsNextSentenceArm"/>).</summary>
    private (List<BoundStatement>? AtEnd, List<BoundSearchWhen> Whens) Phrases(string verb,
        Core.SearchAtEndClauseContext? ae, ITerminalNode? endSearch,
        IReadOnlyList<(Core.ConditionContext Cond, Core.StatementBlockContext Arm)> whens)
    {
        ctx.Validation.CheckSearchEndSearchNextSentence(verb, endSearch, whens.Select(w => w.Arm));
        var atEnd = ae is null ? null : host.BindBlocks([ae.statementBlock()]);
        // A WHEN condition re-evaluates on every scan pass — §14.9.37.4 GR1, "Any subscripting specified in a
        // WHEN phrase is evaluated each time the conditions in that WHEN phrase are evaluated", over GR4's "The
        // process is then repeated using the new index setting" (Format 1) and per probe of GR9's
        // implementor-specified technique (Format 2) — so a user-function reference inside it activates per
        // evaluation: the per-evaluation wrapper (§8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2).
        var bound = whens.Select(w =>
            {
                var udfMark = host.Udf.Mark;
                var cond = host.Udf.UdfAttachPerEvaluation(host.Cond.BindCondition(w.Cond), udfMark);
                return new BoundSearchWhen(cond, host.BindBlocks([w.Arm]));
            })
            .ToList();
        return (atEnd, bound);
    }

    /// <summary>Bind a serial SEARCH (ISO §14.9.37 Format 1). identifier-1 is resolved and screened by
    /// <see cref="Identifier1"/>; the scan uses the table's FIRST index (§14.9.37.4 GR3 a) — unless VARYING names
    /// another index OF THE SAME TABLE, which then IS the search index (GR3 c) 1.: "If index-name-1 is specified
    /// in the INDEXED BY phrase in the OCCURS clause associated with identifier-1, the index referenced by
    /// index-name-1 is the search index"); VARYING a different table's index (GR3 c) 2.) or a data item (GR3 b)
    /// increments that item in step with the search index.</summary>
    public BoundStatement BindSearch(Core.SearchStatementContext s)
    {
        var drefs = s.dataReference();
        if (Identifier1(drefs[0], "SEARCH", out var bail) is not { } table) return bail;

        string searchIx = table.Indexes[0].Cell;   // the table's OWN first declaration (kb/Work PB919)
        BoundSetTarget? also = null;
        if (drefs.Length > 1)   // the VARYING phrase
        {
            var v = drefs[1];
            if (host.Expr.IndexFieldOf(v) is { } vix)
            {
                if (table.Indexes.Any(d => d.Cell == vix)) searchIx = vix;   // same table (GR3 c) 1.)
                else also = new SetIndexTarget(vix);                                          // other table (GR3 c) 2.)
            }
            else if (host.Expr.ResolveReceiving(v) is { } p)                                       // data item (GR3 b)
            {
                // §14.9.37.3 SR5, BOTH sentences (kb/Work PB211): identifier-2 is a class-closed position — "a
                // data item whose usage is index or a data item that is an integer" — screened by the ONE
                // operand-class screen; and it "shall not be subscripted by the first or only index-name" of
                // identifier-1, whose occurrence would otherwise move with every step of the scan (measured: the
                // illegal spelling printed NONE where the legal one printed HIT). Both are reported; the bind
                // stops at a refusal (§4.2.2 — the compile has failed).
                string text = DataBinder.WrittenText(v);
                bool admitted = OperandClassScreen.Screen(ctx.Edition, OperandPositions.SearchVaryingIdentifier, p, text);
                if (table.Indexes.Count > 0 && ctx.Refs.SubscriptNamesIndex(v, table.Indexes[0]))
                {
                    ctx.Edition.Error(DiagnosticCatalog.SearchVaryingOperand,
                        $"SEARCH VARYING identifier-2 '{text}' is subscripted by '{table.IndexNames[0]}', the first "
                        + $"index-name in the INDEXED phrase of '{table.CobolName}'s OCCURS clause, which ISO "
                        + "§14.9.37.3 SR5 prohibits");
                    admitted = false;
                }
                if (!admitted) return BoundRejected.Reported(ctx.Edition);
                also = new SetPlaceTarget(p);
            }
            else return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236, PB881)
        }

        var (atEnd, whens) = Phrases("SEARCH", s.searchAtEndClause(), s.END_SEARCH(),
            [.. s.searchWhenClause().Select(wc => (wc.condition(), wc.statementBlock()))]);
        return new BoundSearch(searchIx, table.Occurs ?? 0, also, atEnd, whens,
            DependItem: OdoModel.SearchDepending(table, ctx.Refs),
            DynTable: table.IsDynamicTable ? ReferenceResolver.BuildTablePath(table) : null,   // EC-FLOW-SEARCH bracket (GR31, D9)
            // §14.9.37.4 GR4: an out-of-range initial index (SEARCH-INDEX) / a scan advancing off the end (NO-MATCH)
            // sets the nonfatal range EC when checking is enabled — captured at the statement line (F10/CONTINUE template).
            CheckSearchIndex: ctx.EcState.Turn.Enabled("EC-RANGE-SEARCH-INDEX", null, s.Start.Line),
            CheckSearchNoMatch: ctx.EcState.Turn.Enabled("EC-RANGE-SEARCH-NO-MATCH", null, s.Start.Line));
    }

    /// <summary>Bind <c>SEARCH ALL</c> (ISO §14.9.37 Format 2 — the ordered-table search form). The initial index
    /// setting is ignored and this implementation scans from occurrence 1; both are inside the latitude
    /// §14.9.37.4 GR9 grants — "A non serial type of search operation MAY take place. The initial setting of the
    /// search index is ignored. Its setting is varied during the search operation in a manner specified by the
    /// implementor" — so a serial probe is one permitted technique, not a concession.
    /// <para>⛔ The LATITUDE IS THE TECHNIQUE, NOT THE RANGE. The same rule's next sentence — "At no time is it
    /// set to a value that exceeds the value that corresponds to the last element of the table or is less than
    /// the value that corresponds to the first element of the table" — is a hard bound on whatever technique is
    /// chosen, and it is why <c>ControlFlowEmitter.EmitAllScan</c> is a SEPARATE lowering from the Format-1
    /// serial scan rather than the same loop under a flag (kb/Work PB447).</para>
    /// <para>⛔ THE CONFORMANCE ARGUMENT DOES NOT REST ON SYNTAX RULE 7, AND MUST NOT BE "RESTORED" TO (kb/Work
    /// PB445). SR7 requires only that the OCCURS clause CARRY a KEY phrase — a requirement on the data
    /// description entry, saying nothing about how the data is ordered at run time. The proposition a search
    /// technique would need is §14.9.37.4 GR5 a) ("The contents of each key data item referenced in the WHEN
    /// phrase shall be sequenced in the table according to the ASCENDING or DESCENDING phrase"), and that is a
    /// condition the PROGRAM shall satisfy, never a guarantee the compiler may rely on — GR6 exists precisely to
    /// define behaviour when it is false, and GR6 a) 2. ends "It is undefined which of these alternatives
    /// occurs". A serial scan over unsequenced data therefore lands inside GR6 a)'s first alternative, which is
    /// an outcome the standard explicitly allows. The technique needs no syntax rule at all.</para>
    /// <para>SR7–SR13 are screened by <c>ctx.Validation.CheckSearchAllFormat2</c> (the ONE Format-2 operand model,
    /// COBOLNET1964–1966); bound onto the same <see cref="BoundSearch"/> node with <c>IsAll</c>, which selects the
    /// Format-2 lowering.</para></summary>
    public BoundStatement BindSearchAll(Core.SearchAllStatementContext s)
    {
        if (Identifier1(s.dataReference(), "SEARCH ALL", out var bail) is not { } table) return bail;

        // The Format-2 operand rules (ISO §14.9.37.3 SR7–SR13; kb/Work PB445). Screened BEFORE the WHEN phrases
        // bind, so a violation is reported against the source's own operands rather than after whatever the
        // general condition binder made of them; the bind proceeds either way (§4.2.2 ¶2 — the indication is at
        // compile time, and one compile reports every violation it can see).
        ctx.Validation.CheckSearchAllFormat2(s, table, ctx.Refs, host.Cond);

        // Format 2 prints exactly ONE WHEN phrase (its ellipsis is on the AND bracket — kb/Work PB446), and the
        // grammar now spells that, so there is one arm.
        var wc = s.searchAllWhenClause();
        var (atEnd, whens) = Phrases("SEARCH ALL", s.searchAtEndClause(), s.END_SEARCH(),
            [(wc.condition(), wc.statementBlock())]);
        return new BoundSearch(table.Indexes[0].Cell, table.Occurs ?? 0,
            AlsoVaried: null, atEnd, whens, IsAll: true, DependItem: OdoModel.SearchDepending(table, ctx.Refs),
            DynTable: table.IsDynamicTable ? ReferenceResolver.BuildTablePath(table) : null,   // EC-FLOW-SEARCH bracket (GR31, D9)
            // SEARCH ALL forces the index to 1 (GR9 ignores the initial setting) so SEARCH-INDEX can never arise;
            // only an unsuccessful scan (incl. an empty table) sets EC-RANGE-SEARCH-NO-MATCH.
            CheckSearchIndex: false,
            CheckSearchNoMatch: ctx.EcState.Turn.Enabled("EC-RANGE-SEARCH-NO-MATCH", null, s.Start.Line));
    }
}
