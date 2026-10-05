// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  SORT / MERGE / RELEASE / RETURN (ISO/IEC 1989:2023 §14.9.40 / §14.9.24 / §14.9.32 / §14.9.34) — the sort-merge
//  subsystem's bound nodes. The sort store is an in-memory, per-SD record-IMAGE buffer (CobolNet.Runtime.IO.CobolSort);
//  keys are compile-time (offset, length, kind) descriptors into the SD record image, ONE comparison policy for
//  numeric (algebraic, §14.9.40 GR8 — never collated) and alphanumeric (collated per GR5 precedence) keys
//  (COBOLNET_DESIGN §8.2 — typed key descriptors over serialized images, offsets computed at compile time).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>The SORT/MERGE/RELEASE/RETURN verb binder (P7 Step 10i — a real collaborator over
/// <see cref="BinderContext"/>): <c>ResolveProcedure</c> stays a HOST edge called at the SAME bind point
/// (INPUT/OUTPUT PROCEDURE resolution is position-dependent — never snapshot early). The RELEASE <c>FROM</c>
/// and RETURN <c>INTO</c> phrases bind through <c>host.Move</c> — they are implicit MOVEs and the MOVE binder is
/// where a MOVE's rules live (kb/Work PB348). ⛔ That removed this binder's LAST reach into
/// <c>SequentialIoBinder</c> (the former <c>WriteSource</c> operand hand-off, which applied none of RELEASE's
/// own §14.9.32.3 SR2/SR3/SR4), so the constructor no longer takes one: the sort verbs and the sequential I-O
/// verbs are independent collaborators again. The 0870/0871/0872 gates moved
/// VERBATIM with their exact control flow (report-and-continue at the table-SORT/RELEASE sites vs
/// report+BoundUnsupported at alphabet-name-2 — Exec Step E folds them). The 8 bound types stayed in
/// <c>Binding/Bound/BoundSort.cs</c>.</summary>
internal sealed class SortBinder(BinderContext ctx, StatementBinder host)
{
    // ── SORT (ISO §14.9.40) ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind SORT, splitting Format 1 (file sort) from Format 2 (table sort) by resolving the operand:
    /// a name declared in FILE-CONTROL/an SD is the file format; otherwise a table data item (ISO §14.9.40 —
    /// the formats share one general shape; the grammar defers the split to this semantic layer).</summary>
    public BoundStatement BindSort(Core.SortStatementContext s)
    {
        var operand = s.sortFileName().dataReference();
        string name = SubjectName(s);
        return ctx.Data.FilesByName.TryGetValue(name, out var file)
            ? SortBindFile(s, file)
            : SortBindTable(s, operand, name);
    }

    /// <summary>The base word of the SORT's first operand — the name the file / table split is decided by.</summary>
    private static string SubjectName(Core.SortStatementContext s)
    {
        var operand = s.sortFileName().dataReference();
        return operand.cobolWord()?.GetText() ?? operand.GetText();
    }

    /// <summary>Is this SORT the FILE format (Format 1)? The parse node is shared by both formats and the split is
    /// semantic — a name declared in FILE-CONTROL / an SD is the file format — so every rule written about "a SORT
    /// statement" under the FORMAT 1 heading (§14.9.40.3 SR3-SR12) asks THIS, never the node type: §14.9.40.3 SR3's
    /// exception-checking-PERFORM ban bound a legal Format-2 table SORT until kb/Work PB1139.</summary>
    internal static bool IsFileFormat(BinderContext ctx, Core.SortStatementContext s) =>
        ctx.Data.FilesByName.ContainsKey(SubjectName(s));

    /// <summary>⛔ THE DECLARATIVE LEG OF THE SORT/MERGE PLACEMENT RULES (kb/Work PB1139): "A SORT statement shall not
    /// appear in … a declarative procedure" (§14.9.40.3 SR3, FORMAT 1) and a MERGE statement "may appear anywhere in
    /// the procedure division except … in a declarative procedure" (§14.9.24.3 SR1). Asked ONCE here for both verbs,
    /// from the bind position's own <see cref="EnclosingContext.InDeclarative"/>; it holds at every edition (Annex
    /// E.2 item 20 changed only the SORT/MERGE-procedure half of MERGE's rule, at 2023). The statement is reported
    /// and the bind continues, so one compile reports every violation it can see. The other two regions are the
    /// exception-checking PERFORM's (<c>EcBinder.CheckCrossStatementBans</c>, parse-tree) and the sort-merge
    /// procedures' (<c>VersionConformancePass.GateSortMergeProcedures</c>, over the bound paragraphs).</summary>
    private void ScreenDeclarativePlacement(bool merge)
    {
        if (!ctx.Enclosing.InDeclarative) return;
        ctx.Edition.Error(DiagnosticCatalog.SortMergePlacement, merge
            ? "a MERGE statement shall not appear in a declarative procedure (ISO §14.9.24.3 SR1: \"A MERGE statement may "
                + "appear anywhere in the procedure division except … in a declarative procedure\")"
            : "a SORT statement shall not appear in a declarative procedure (ISO §14.9.40.3 SR3: \"A SORT statement shall "
                + "not appear in imperative-statement-1 of an exception-checking PERFORM statement, in an input or output "
                + "procedure, or in a declarative procedure\")");
    }

    /// <summary>Bind the Format-1 file sort: SD operand (SR4), keys (SR6 + GR1/GR2), DUPLICATES (GR3),
    /// COLLATING (GR5), and the release/return phase sources (USING/INPUT PROCEDURE, GIVING/OUTPUT PROCEDURE —
    /// the general format requires one of each pair).</summary>
    private BoundStatement SortBindFile(Core.SortStatementContext s, FileModel file)
    {
        ScreenDeclarativePlacement(merge: false);   // §14.9.40.3 SR3, the declarative leg (kb/Work PB1139)
        // SR4 is a SYNTAX RULE, so it is decided here and not by a run-time loud (kb/Work PB236).
        if (!file.IsSortMerge)
        {
            ctx.Validation.RejectStatementOperand($"SORT file '{file.CobolName}' is not described in a sort-merge "
                + "description entry (ISO §14.9.40.3 SR4 — file-name-1 shall be described in an SD)");
            return BoundRejected.Reported(ctx.Edition);
        }
        if (RecordLessSd(file)) return new BoundNop();

        // Format 1 prints the KEY phrase in BRACES with an ellipsis (§14.9.40.2) — at least one is required, and
        // its data-name-1 is required too (braces, not the Format-2 brackets). The grammar's `sortKeyPhrase*` is
        // shared with Format 2, whose phrase MAY be omitted (§14.9.40.3 SR15, kb/Work PB846), so the Format-1
        // arity is screened here, where the operand's format is known.
        if (s.sortKeyPhrase().Length == 0)
        {
            ctx.Validation.RejectStatementOperand($"SORT of file '{file.CobolName}' requires at least one "
                + "ASCENDING/DESCENDING KEY phrase (ISO §14.9.40.2 Format 1 general format — the KEY phrase is "
                + "braced with an ellipsis; only the Format-2 table sort may omit it, §14.9.40.3 SR15)");
            return BoundRejected.Reported(ctx.Edition);
        }
        var keys = new List<BoundSortMergeKey>();
        foreach (var phrase in s.sortKeyPhrase())
            if (!SortAddFileKeys(phrase.DESCENDING() is not null, phrase.dataReferenceList(), file, keys, SortKeyRules.FileSort))
                return BoundRejected.Reported(ctx.Edition);   // reported by SortAddFileKeys (PB236, PB1030)
        // ⛔ THE KEYS ARE SCREENED BEFORE THE RECORD'S IMAGE IS ASKED ABOUT (kb/Work PB1173): a record with a pointer or
        // object leaf has no image the store can hold, which is the compiler's gap, while a key of class pointer is the
        // SOURCE's error (§14.9.40.3 SR6 c)) — and the gap used to answer first, so the source's error was never told.
        if (SortRecordOf(file) is null)
            // The MECHANISM is derived from the record itself (the R40 fleet: a fixed "VARIABLE-LENGTH"
            // string misdiagnosed a pointer-leafed record — the same wrong-cause defect twice removed).
            return new BoundUnsupported(TierCIsland.Reason(file.Records[0], "SORT SD record of"));

        var (collating, collErr) = SortBindCollating(s.sortCollatingPhrase());
        if (collErr is { } ce) return ce;

        // Release phase source (ISO §14.9.40 GR9a): USING file list or INPUT PROCEDURE pc range.
        var usingFiles = new List<FileModel>();
        if (s.sortUsingPhrase() is { } up && !SortMapIoFiles(up.dataReferenceList(), usingFiles, merge: false, giving: false))
            return BoundRejected.Reported(ctx.Edition);   // SortMapIoFiles REPORTED (PB236, PB1171)
        PcRange? inputProc = null;
        if (s.sortInputProcedurePhrase() is { } ipp)
        {
            if (SortRange(ipp.procedureName(), "SORT INPUT PROCEDURE", "§14.9.40.2") is not { } ipr)
                return BoundRejected.Reported(ctx.Edition);   // reported by the ONE procedure-name resolution (kb/Work PB390)
            inputProc = ipr;
        }
        // Return phase target (GR9c): GIVING file list or OUTPUT PROCEDURE pc range.
        var givingFiles = new List<FileModel>();
        if (s.sortGivingPhrase() is { } gp && !SortMapIoFiles(gp.dataReferenceList(), givingFiles, merge: false, giving: true))
            return BoundRejected.Reported(ctx.Edition);   // SortMapIoFiles REPORTED (PB236, PB1171)
        PcRange? outputProc = null;
        if (s.sortOutputProcedurePhrase() is { } opp)
        {
            if (SortRange(opp.procedureName(), "SORT OUTPUT PROCEDURE", "§14.9.40.2") is not { } opr)
                return BoundRejected.Reported(ctx.Edition);   // reported by the ONE procedure-name resolution (kb/Work PB390)
            outputProc = opr;
        }
        if ((usingFiles.Count == 0 && inputProc is null) || (givingFiles.Count == 0 && outputProc is null))
        {
            ctx.Validation.RejectStatementOperand("SORT Format 1 requires {INPUT PROCEDURE | USING} and "
                + "{OUTPUT PROCEDURE | GIVING} (ISO §14.9.40.2 general format)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }
        if (!ScreenSameClauses(file, usingFiles, givingFiles, merge: false)) return BoundRejected.Reported(ctx.Edition);   // SR10
        if (!ScreenIndexedGivingKey(keys, givingFiles, merge: false)) return BoundRejected.Reported(ctx.Edition);         // SR9
        if (!ScreenTransferRecordSizes(file, usingFiles, givingFiles, merge: false)) return BoundRejected.Reported(ctx.Edition);   // SR5, SR11

        return new BoundSort(file, keys, s.sortDuplicatesPhrase() is not null, collating,
            usingFiles, inputProc, givingFiles, outputProc, SortVaryingOf(file));
    }

    /// <summary>Bind the Format-2 in-place TABLE sort (ISO §14.9.40 GR18–GR24) over the typed element array.
    /// Introduced by ISO/IEC 1989:2002 (the table-SORT format is absent from ANSI X3.23-1985; M2 feature catalog,
    /// docs/ISO2023_CONFORMANCE_PLAN.md) — rejected below <c>--std 2002</c>.</summary>
    private BoundStatement SortBindTable(Core.SortStatementContext s, Core.DataReferenceContext operand, string name)
    {
        // table-sort-2002: the pass owns the edition gate (Exec Step E — the F2 shape is syntactic).

        // Format 2 has NO USING/GIVING/procedure phrases (ISO §14.9.40.2 — the in-place table sort).
        if (s.sortUsingPhrase() is not null || s.sortGivingPhrase() is not null
            || s.sortInputProcedurePhrase() is not null || s.sortOutputProcedurePhrase() is not null)
        {
            ctx.Validation.RejectStatementOperand($"SORT of '{name}': USING/GIVING/INPUT/OUTPUT PROCEDURE apply "
                + "only to a sort-merge FILE operand (ISO §14.9.40.2 — Format 2 sorts the table in place)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }

        // SR13: data-name-2 shall have an OCCURS clause. Resolved like SEARCH's identifier-1 — the ordinary §8.4.2.2
        // resolution of the WRITTEN reference, qualifiers and all (ReferenceResolver.ResolveTableOperand). This arm
        // used to look up the base word and take the FIRST same-named item with an OCCURS clause, so `SORT E OF G2`
        // with a table E in each of two groups sorted G1's — the SEARCH arm's PB-era defect, left in its twin
        // (kb/Work PB1018's sibling sweep). An ambiguous or undeclared name is reported by the resolver itself.
        bool declared = ctx.Symbols.TryResolve(name, ctx.ActiveScope, out _);
        DataItem? table = declared ? ctx.Refs.ResolveTableOperand(operand)?.Item : null;
        // SR13 is "an OCCURS clause", every format of it: a dynamic-capacity table (OCCURS DYNAMIC, §13.18.38
        // Format 4) has no fixed integer but is a table all the same (DataItem.IsTable).
        if (table is not { IsTable: true })
        {
            if (declared && table is null && ctx.Refs.WasDiagnosed(operand)) return BoundRejected.Reported(ctx.Edition);
            ctx.Validation.RejectStatementOperand($"SORT of '{name}' — neither a SELECTed/SD file nor an OCCURS "
                + "table: file-name-1 shall be described in an SD (ISO §14.9.40.3 SR4) and data-name-2 shall be "
                + "described with an OCCURS clause (SR13)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }
        // §14.9.40.3 SR13's second sentence — "Subscripting shall be specified in accordance with 8.4.2.3" — for the
        // subject of a table SORT (§8.4.2.3.3 SR3, SR5 e), SR6): one subscript for each ENCLOSING table, none for the
        // table the statement sorts (a rightmost ALL says the same). Read ONCE here, so `SORT E(2)` over a table inside
        // `ROW OCCURS 2` sorts ROW(2)'s E and an omitted or surplus subscript is the SOURCE's error, named (kb/Work
        // PB1055 — the written list used to be discarded unread, and both spellings drew one deferral).
        if (ctx.Refs.ReadTableSubjectSubscripts(operand, table, out var outer) is { } subjectAnswer)
            return subjectAnswer.Refusal(ctx.Edition);
        // WHERE the elements live (kb/Work PB1175 / PB1055): a table inside a REDEFINES class, a record area shared by
        // several 01s, or a BASED / EXTERNAL record has NO element array — its elements are windows over the one
        // backing — while every other table (nested or not) is a typed array reached by its whole-table path.
        TableSortStorage storage;
        if (table.Class is not null)
        {
            // A subject whose class backing is not reachable from the statement (a class inside an OCCURS) is a
            // shape the ONE place builder has not built — the resolver's own deferral, never a second answer here.
            if (ctx.Refs.ResolveItemAt(table, [.. outer, "1"]) is null)
                return new BoundUnsupported($"SORT of table '{name}': "
                    + DeferredShapes.Describe(DeferredShape.NestedClassBacking));
            // A pointer-class member's VALUE rides the area's managed slot, not its bytes (§14.9.3.4 GR9; kb/Work
            // PB231), and the elements move as byte images: the emitter carries each element's slots with its image
            // (kb/Work PB1922), so every such member needs a window of its own here, placed by the ONE place builder.
            if (SlotWindow.MembersOf(table).Any(m => ctx.Refs.ResolveItemAt(m, [.. outer, "1"]) is null))
                return new BoundUnsupported($"SORT of table '{name}': "
                    + DeferredShapes.Describe(DeferredShape.UnbuiltAccessPath));
            storage = new TableSortStorage.SharedArea(outer);
        }
        else if (ReferenceResolver.BuildTablePath(table, outer) is { } arrayPath)
            storage = new TableSortStorage.TypedArray(arrayPath);
        else
            return new BoundUnsupported($"SORT of table '{name}': "
                + DeferredShapes.Describe(DeferredShape.UnbuiltAccessPath));
        // §14.9.40.4 GR20 — "The number of occurrences of table elements referenced by data-name-2 is determined by
        // the rules in the OCCURS clause": the CURRENT count (§13.18.38.4 GR7 for OCCURS DEPENDING, the current
        // capacity for a dynamic-capacity table), never the physical array (kb/Work PB1174).
        if (ctx.Refs.CurrentOccurrenceCount(table, outer) is not { } count)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule,
                $"SORT of table '{name}': its current number of occurrences cannot be addressed (ISO §14.9.40.4 GR20)");

        var keys = new List<BoundTableSortKey>();
        if (s.sortKeyPhrase().Length == 0)
        {
            // §14.9.40.4 GR21 — "If the KEY phrase is not specified, the sequence is determined by the KEY phrase in
            // the data description entry of the table referenced by data-name-2", admitted ONLY under §14.9.40.3
            // SR15 — "The KEY phrase may be omitted only if the description of the table referenced by
            // data-name-2 contains a KEY phrase" (kb/Work PB846). The grammar's key-phrase list is `*` for exactly
            // this reason: the omission is a Format-2 syntax rule that needs the RESOLVED table, so the screen
            // lives here and names SR15 instead of surfacing as a parse error. The table's KEY phrase is read
            // through the ONE ordered model SEARCH ALL also reads (OccursSpec.Keys / OdoModel.KeyItems, kb/Work
            // PB445) — significance order and per-key direction are the phrase's own (§13.18.38.4 GR3), which is
            // GR21's "determined by" read literally.
            var specKeys = table.OccursSpec?.Keys ?? [];
            if (specKeys.Count == 0)
            {
                ctx.Validation.RejectStatementOperand($"SORT of table '{name}' omits the KEY phrase, but the OCCURS "
                    + "clause of the table has no KEY phrase either (ISO §14.9.40.3 SR15 — the KEY phrase may be "
                    + "omitted only if the description of the table referenced by data-name-2 contains a KEY phrase)");
                return BoundRejected.Reported(ctx.Edition);
            }
            var keyItems = OdoModel.KeyItems(table);
            for (int i = 0; i < specKeys.Count; i++)
            {
                // An unresolvable OCCURS KEY data-name is the data description's own error (§13.18.38.3 SR3),
                // reported where the OCCURS clause is bound; nothing further to say here.
                if (keyItems[i] is not { } tk) return BoundRejected.Reported(ctx.Edition);
                if (Inadmissible(tk) is { } tkRefusal) return tkRefusal;
                if (TableSortKey(table, storage, outer, specKeys[i].Descending, tk) is not { } k)
                    return TableSortKeyUnsupported(specKeys[i].Written);
                keys.Add(k);
            }
        }
        foreach (var phrase in s.sortKeyPhrase())
        {
            bool desc = phrase.DESCENDING() is not null;
            var drefs = phrase.dataReferenceList()?.dataReference() ?? [];
            if (drefs.Length == 0)
            {
                // GR23: data-name-1 omitted — the table ELEMENT itself is the key data item.
                if (Inadmissible(table) is { } elementRefusal) return elementRefusal;
                keys.Add(new BoundTableSortKey(desc, "", table));
                continue;
            }
            foreach (var dref in drefs)
            {
                string kn = DataBinder.WrittenText(dref);
                // §14.9.40.3 SR14 b) — "Key data names shall not be subscripted" — and §8.4.3.3.3's NOTE (no
                // reference-modifier where the format prints data-name-n): the one data-name-n screen, ahead of
                // KeyReference, which keeps the qualifiers and DROPS every other suffix — `SORT E ON ASCENDING
                // KEY K(4:3)` used to sort on all of K (kb/Work PB481, measured).
                // A table key lies UNDER the table's OCCURS by SR14 a), so the subscript sentence is SR14 b)'s own.
                if (!DataBinder.ScreenDataNameShape(dref, "SORT table key", ctx.Edition,
                        "\"Key data names shall not be subscripted\" (ISO §14.9.40.3 SR14 b))"))
                    return BoundRejected.Reported(ctx.Edition);
                // §14.9.40.3 SR14 a)'s own walk — "The data item identified by a key data-name shall be the same
                // as, or subordinate to, the data item referenced by data-name-2" — over data-name-2's subtree,
                // narrowed by the WRITTEN qualifiers and counted (kb/Work PB1018): the same §8.4.2.2 subtree
                // resolution the OCCURS KEY phrase takes (DataBinder.OccursKeyResolve, §13.18.38.3 SR3). The base
                // word alone used to be looked up here and the FIRST same-named item taken, so `SORT E ASCENDING
                // K OF B` keyed on A's K. Ambiguity is §8.4.2.2.3 SR1's verdict, reported by the ONE verdict.
                var (baseName, quals) = DataBinder.KeyReference(dref);
                DataItem? key = ctx.Data.UniqueOrReportAmbiguous(ctx.Data.SubtreeCandidates(table, baseName, quals),
                    "SORT table key", kn, out bool ambiguous);
                if (key is null)
                {
                    if (!ambiguous)
                        ctx.Validation.RejectStatementOperand($"SORT table key '{kn}' is not data-name-2 nor "
                            + "subordinate to it (ISO §14.9.40.3 SR14a)");   // PB236
                    return BoundRejected.Reported(ctx.Edition);
                }
                // ⛔ TWO VERDICTS, WHICH USED TO SHARE ONE DEFERRAL (kb/Work PB909): an inner OCCURS between the key
                // and data-name-2 is the SOURCE's error (SR14 e), while a REDEFINES-view key is legal source this
                // typed-array path has not built. Asked in that order, so a key that is both is refused, not deferred.
                if (KeyUnderInnerOccurs(table, key))
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule, $"SORT table key '{kn}': \"If the data item identified by a key "
                        + "data-name is subordinate to data-name-2, it shall not be described with an OCCURS clause, and "
                        + "it shall not be subordinate to an entry that is also subordinate to data-name-2 and contains "
                        + "an OCCURS clause\" (ISO §14.9.40.3 SR14 e)");
                if (Inadmissible(key) is { } keyRefusal) return keyRefusal;   // SR14 c), d) — the ONE predicate (kb/Work PB1173)
                // A key of class national orders under the NATIONAL collating sequence — the GR5 lead-in
                // ("the national collating sequence that applies to the comparison of key data items of class
                // national"), resolved by GR5a/GR5b like its alphanumeric twin. It used to stage LOUD here, with
                // a comment citing GR5b (the program-collating-sequence PRECEDENCE step, not the class rule) and
                // claiming the file sort was "separately blocked by the D-N2 FD/SD record gate" — a gate PB327
                // removed. The comparator now selects on the key's class (kb/Work PB678), so both formats sort a
                // national key under the national sequence and nothing is staged.
                if (TableSortKey(table, storage, outer, desc, key) is not { } k) return TableSortKeyUnsupported(kn);
                keys.Add(k);
            }
        }

        // A key with no stored field on the element struct (a REDEFINES view, kb/Work PB599) moves the WHOLE statement
        // to window reads: one comparer reads every key one way, so the typed array keeps its storage and learns
        // the enclosing occurrences the windows are placed by.
        if (storage is TableSortStorage.TypedArray typedStorage && keys.Any(k => k.MemberPath is null))
            storage = typedStorage with { KeyWindowOuter = outer };
        // A window-read key is read through its own window, which the ONE place builder positions (the same
        // question the subject asked above): a key it cannot place is the resolver's deferral, never a guess here.
        if (storage switch
            {
                TableSortStorage.SharedArea sa => sa.OuterIndexExprs,
                TableSortStorage.TypedArray { KeyWindowOuter: { } w } => w,
                _ => null,
            } is { } windowOuter)
            foreach (var k in keys)
                if (ctx.Refs.ResolveItemAt(k.Key, [.. windowOuter, "1"]) is null)
                    return new BoundUnsupported($"SORT table key '{k.Key.CobolName}': "
                        + DeferredShapes.Describe(DeferredShape.UnbuiltAccessPath));

        var (collating, collErr) = SortBindCollating(s.sortCollatingPhrase());
        if (collErr is { } ce) return ce;
        return new BoundTableSort(storage, table, count, keys, s.sortDuplicatesPhrase() is not null, collating);

        // §14.9.40.3 SR14 c) and d) for one key, through the ONE key-admissibility predicate shared with SORT Format 1
        // and MERGE (kb/Work PB1173): the refusal node when the key may not be one, else null.
        BoundStatement? Inadmissible(DataItem key)
        {
            if (SortKeyAdmission.Violation(key, SortKeyRules.TableSort) is not { } violation) return null;
            ctx.Validation.RejectStatementOperand(violation);
            return BoundRejected.Reported(ctx.Edition);
        }
    }

    // ── MERGE (ISO §14.9.24) ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind MERGE (ISO §14.9.24): SD operand (SR — file-name-1 shall be described in an SD), keys
    /// (GR3 — required; transitive ASC/DESC, statement-order significance), COLLATING (GR5 ≡ SORT GR5), the
    /// REQUIRED ≥2-file USING (the general format), and GIVING / OUTPUT PROCEDURE (GR8/GR12). INPUT PROCEDURE
    /// does not exist for MERGE (general format — the grammar has no such phrase).</summary>
    public BoundStatement BindMerge(Core.MergeStatementContext m)
    {
        var operand = m.mergeFileName().dataReference();
        string name = operand.cobolWord()?.GetText() ?? operand.GetText();
        ScreenDeclarativePlacement(merge: true);   // §14.9.24.3 SR1, the declarative leg (kb/Work PB1139)
        // TWO verdicts, not one (kb/Work PB236): "no such file-name" is §8.4.2.1, "declared but under an FD"
        // is §14.9.24.3 — and both used to be answered by the same run-time loud.
        if (!ctx.Validation.ResolveFile(name, "MERGE", out var file)) return BoundRejected.Reported(ctx.Edition);
        if (!file.IsSortMerge)
        {
            ctx.Validation.RejectStatementOperand($"MERGE file '{name}' is not described in a sort-merge "
                + "description entry (ISO §14.9.24.3 — file-name-1 shall be described in an SD)");
            return BoundRejected.Reported(ctx.Edition);
        }
        if (RecordLessSd(file)) return new BoundNop();

        var keys = new List<BoundSortMergeKey>();
        foreach (var phrase in m.mergeKeyPhrase())
            if (!SortAddFileKeys(phrase.DESCENDING() is not null, phrase.dataReferenceList(), file, keys, SortKeyRules.Merge))
                return BoundRejected.Reported(ctx.Edition);   // reported by SortAddFileKeys (PB236, PB1030)
        if (SortRecordOf(file) is null)   // after the keys, for the reason BindSort gives (kb/Work PB1173)
            return new BoundUnsupported($"MERGE '{file.CobolName}' without a usable SD record (Tier-C byte island, deferred)");

        var (collating, collErr) = SortBindCollating(m.sortCollatingPhrase());
        if (collErr is { } ce) return ce;

        var usingFiles = new List<FileModel>();
        if (!SortMapIoFiles(m.mergeUsingPhrase().dataReferenceList(), usingFiles, merge: true, giving: false))
            return BoundRejected.Reported(ctx.Edition);   // SortMapIoFiles REPORTED (PB236, PB1171)
        if (usingFiles.Count < 2)
        {
            ctx.Validation.RejectStatementOperand("MERGE requires at least two USING files (ISO §14.9.24.2 "
                + "general format — USING file-name-2 {file-name-3}…)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }

        var givingFiles = new List<FileModel>();
        if (m.mergeGivingPhrase() is { } gp && !SortMapIoFiles(gp.dataReferenceList(), givingFiles, merge: true, giving: true))
            return BoundRejected.Reported(ctx.Edition);   // SortMapIoFiles REPORTED (PB236, PB1171)
        PcRange? outputProc = null;
        if (m.mergeOutputProcedurePhrase() is { } opp)
        {
            if (SortRange(opp.procedureName(), "MERGE OUTPUT PROCEDURE", "§14.9.24.2") is not { } opr)
                return BoundRejected.Reported(ctx.Edition);   // reported by the ONE procedure-name resolution (kb/Work PB390)
            outputProc = opr;
        }
        if (givingFiles.Count == 0 && outputProc is null)
        {
            ctx.Validation.RejectStatementOperand("MERGE requires {OUTPUT PROCEDURE | GIVING} "
                + "(ISO §14.9.24.2 general format)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }
        // SR7 — "File-names shall not be repeated within the MERGE statement" (kb/Work PB1139): over file-name-1 (an SD,
        // which SortMapIoFiles has already barred from USING / GIVING), file-name-2/3 and file-name-4 together, by FILE
        // — `USING F1 F1`, `USING F1 F2 GIVING F1` and `GIVING F3 F3` are each one name written twice.
        var written = new HashSet<FileModel>(ReferenceEqualityComparer.Instance);
        foreach (var f in usingFiles.Concat(givingFiles))
            if (!written.Add(f))
            {
                ctx.Validation.RejectStatementOperand($"MERGE file '{f.CobolName}' is repeated: \"File-names shall not be "
                    + "repeated within the MERGE statement\" (ISO §14.9.24.3 SR7)");
                return BoundRejected.Reported(ctx.Edition);
            }
        if (!ScreenSameClauses(file, usingFiles, givingFiles, merge: true)) return BoundRejected.Reported(ctx.Edition);   // SR11
        if (!ScreenIndexedGivingKey(keys, givingFiles, merge: true)) return BoundRejected.Reported(ctx.Edition);         // SR10
        if (!ScreenTransferRecordSizes(file, usingFiles, givingFiles, merge: true)) return BoundRejected.Reported(ctx.Edition);   // SR3, SR12
        // VCR 27 (2014→2023): a MERGE newly PROHIBITED inside another MERGE's output procedure / a file-SORT's input
        // or output procedure (§14.9.24; Annex E.2 item 20) is the ≥2023 static diagnostic COBOLNET1572 — a
        // procedure-range cross-pass in VersionConformancePass.GateSortMergeProcedures (the paragraph-pc ranges are
        // available on this BoundMerge/BoundSort). Below 2023 the runtime EC-SORT-MERGE-ACTIVE raise in
        // CobolSort.Init covers the dynamic case when checking is enabled (kb/Work PB1036).
        return new BoundMerge(file, keys, collating, usingFiles, givingFiles, outputProc, SortVaryingOf(file));
    }

    // ── RELEASE (ISO §14.9.32) ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind RELEASE (ISO §14.9.32): record-name-1 shall name a logical record of an SD entry and may be
    /// qualified (SR1); FROM ≡ MOVE then RELEASE (GR4). The EC-FLOW-RELEASE legality check (GR1 — only inside the
    /// executing SORT's input procedure) is dynamic: <c>CobolSort.ReleaseStatement</c> tests the store's procedure
    /// phase, and <c>EcBinder.QueryFor</c> gives this node its >>TURN gate (kb/Work PB349).</summary>
    public BoundStatement BindRelease(Core.ReleaseStatementContext rel)
    {
        if (rel.dataReference() is not { } rn)
            return new BoundUnsupported($"RELEASE record '{rel.dataReference()?.GetText()}' (unresolvable record-name)");
        if (host.Expr.ResolveSending(rn) is var recordAnswer && recordAnswer.Place is not { } record)
            return recordAnswer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
        // ⛔ SR1 IS A SYNTAX RULE AND IS DECIDED HERE, NOT AT RUN TIME (kb/Work PB236, row SR-14.9.32.3-1).
        // The STAGE was the wrong one, and the cost was measured: with the statement on a path the flow GO TOs
        // past, the program compiled clean AND ran to normal completion with no message at any stage — illegal
        // source shipped in silence. ISO §4.2.2 ¶2 makes the compile-time mechanism mandatory for "the general
        // formats and the explicit syntax rules".
        // ⛔ AND THE PREDICATE WAS NOT THE RULE EITHER (kb/Work PB347). SR1 has two halves and they now sit in
        // two places, each shared with whoever else is under it: "the name of a logical record" is
        // ResolveRecordName's, held in common with WRITE §14.9.51.3 SR5 and REWRITE §14.9.35.3 SR1 (and it is
        // what rejects `RELEASE SR-DATA` and `RELEASE SRT-REC(1:3)`); "in a SORT-MERGE file description entry"
        // is RELEASE's alone, and CheckReleaseRecord asks it of a reference that already IS a logical record.
        if (!ctx.Validation.ResolveRecordName(record, rn.GetText(), "RELEASE",
                "record-name-1 \"shall be the name of a logical record in a sort-merge file description entry "
                + "and it may be qualified\" (ISO §14.9.32.3 SR1)",
                "ISO §13.18.27.3 SR3 — \"If the GLOBAL clause is not specified in the file description entry of a "
                + "containing program, the file shall not be referenced directly or indirectly by any input-output "
                + "statements in any contained program\"", out var file))
            return BoundRejected.Reported(ctx.Edition);
        if (!ctx.Validation.CheckReleaseRecord(file, rn.GetText())) return BoundRejected.Reported(ctx.Edition);
        // RELEASE ... FROM is an IMPLICIT MOVE and is bound as one (ISO §14.9.32.4 GR4 a); kb/Work PB348), so
        // §14.9.32.3 SR2 (the function-identifier class), SR3 (valid as a MOVE sending operand with
        // record-name-1 as the receiver) and SR4 (no zero-length literal-1) are applied HERE, at bind time,
        // together with the storage facts StorageFormPass consumes.
        // RELEASE ... FROM literal-1: ANSI X3.23-1985 admits only identifier-1 in the FROM phrase; the literal
        // operand is a later-standard extension of the format (present in ISO/IEC 1989:2023 §14.9.32.2;
        // VERSION_CHANGE_REFERENCE ledger instructs gating pending verification against the 2002/2014 texts).
        // release-from-literal-2002: the pass owns the edition gate (Exec Step E).
        BoundMove? from = rel.releaseFrom() is { } rf
            ? host.Move.BindFromPhrase(FromPhraseRules.Release, record, rf.dataReference(), rf.literal(),
                                       rf.functionCall(), rf.inlineMethodInvocation())
            : null;
        // ⛔ THE SIZE OF A FIXED-LENGTH SD'S RECORD IS THE LARGEST RECORD DESCRIPTION'S (kb/Work PB322 determination F;
        // docs/CONFORMANCE.md DOC-A.1-147). With no RECORD clause the implicit clause is implementor-defined
        // (§13.18.43.4 GR5), and the implied Format 1's integer-1 is "the record size of the largest record description
        // entry in this file description entry" (GR5 a)), so every record of the sort file is that size and a RELEASE
        // of a shorter record description (a secondary 01 of a multi-01 SD) releases MORE bytes than record-name-1
        // holds: §14.9.32.4 GR6 leaves the extra bytes' content undefined (Annex A.2 item 45), and the determination
        // is the one WRITE and REWRITE already make (D-WRT1, FileModel.TransfersPastRecord) — position n past the end of
        // record-name-1 is position n of the record area. The released length used to be the named record's OWN size,
        // which left ONE SD holding records of two sizes and wrote a short record into a longer GIVING record.
        int width = file.TransfersPastRecord(record.Item) && ctx.Refs.RecordArea(file) is { } area
            ? Model.RecordLayout.AreaWidth(area.Item)
            : Model.RecordLayout.AreaWidth(record.Item);
        return new BoundRelease(file, record, width, from, SortVaryingOf(file));
    }

    // ── RETURN (ISO §14.9.34) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind RETURN (ISO §14.9.34): file-name-1 shall be described by an SD (SR1); INTO ≡ RETURN then
    /// MOVE record-area → identifier-1 (GR5); the AT END and NOT AT END phrases may be written in REVERSED order
    /// (SR4 — detected by the phrase's leading NOT). The EC-FLOW-RETURN / EC-SORT-MERGE-RETURN legality checks
    /// (GR1/GR3) are dynamic: <c>CobolSort.ReturnStatement</c> tests the store's procedure phase and at-end latch,
    /// and <c>EcBinder.QueryFor</c> gives this node its >>TURN gate (kb/Work PB349).</summary>
    public BoundStatement BindReturn(Core.ReturnStatementContext r)
    {
        string name = r.fileName().GetText();
        // ⛔ TWO VERDICTS, NOT ONE (kb/Work PB236, row SR-14.9.34.3-1). This test used to conflate "no such
        // file-name exists" — a §8.4.2.1 name-resolution failure — with "the file exists but is described by an
        // FD", which is §14.9.34.3 SR1, and answered both with a run-time loud. They are different diagnoses and
        // the user needs the right one: telling someone whose file has an FD that the name is undefined sends
        // them hunting for a declaration that is right there.
        if (!ctx.Validation.ResolveFile(name, "RETURN", out var file)) return BoundRejected.Reported(ctx.Edition);
        if (!ctx.Validation.CheckReturnFile(file)) return BoundRejected.Reported(ctx.Edition);
        // GR3 makes the record available in the WHOLE record area — resolve it through the LARGEST record's view
        // (ReferenceResolver.RecordArea, ISO §13.18.33.4 GR3); a shorter Records[0] window would truncate the
        // store (ST111A's 50/75/100 SD). SortRecordOf stays the usability gate (the Tier-C byte-island fence).
        if (RecordLessSd(file)) return new BoundNop();
        if (SortRecordOf(file) is null || ctx.Refs.RecordArea(file) is not { } area)
            return new BoundUnsupported($"RETURN '{name}' without a usable SD record area");
        // RETURN ... INTO is an IMPLICIT MOVE and is bound as one (ISO §14.9.34.4 GR5 b); kb/Work PB348) -
        // THE SAME call READ ... INTO makes, because GR5 b) and §14.9.30.4 GR4 b) are the same sentence.
        BoundMove? into = null;
        if (r.INTO() is not null)
        {
            if (r.dataReference() is not { } d || host.Expr.ResolveReceiving(d) is not { } ip)
                return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236, PB881)
            into = host.Move.BindIntoPhrase(file, area, ip, IntoPhraseRules.Return);
        }
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.returnAtEndPhrase() is { } ae)
            // §14.9.34.3 SR4 — the phrases may be written in reversed order; Split's positional swap covers
            // BOTH the NOT-only form and the full reversed pair (P7 Step 10b).
            (atEnd, notAtEnd) = PhraseBlocks.Split(ae.statementBlock(), PhraseBlocks.StartsWithNot(ae), b => host.BindBlocks([b]));
        // ⛔ §14.9.34.2 prints `AT END imperative-statement-1` on its own line with NO brackets, between the
        // bracketed `[ NOT AT END … ]` and `[ END-RETURN ]` — so the AT END phrase is MANDATORY on every
        // RETURN (§5.2.6.2: brackets are the only thing that makes a portion omissible; §5.2.2: an underlined
        // keyword is required subject to those conventions). SR4's "when specified" permits REVERSING the two
        // phrases, never omitting either. ONE test covers BOTH grammar arms because Split has already
        // normalized position out: `atEnd is null` holds for the absent phrase AND for the reversed spelling
        // that writes only NOT AT END (kb/Work PB350 — the two-arm trap: screening `returnAtEndPhrase() is
        // null` alone would have left the NOT-only arm compiling).
        ctx.Validation.ScreenOmittedRequiredPhrase(atEnd is null, "AT END", "RETURN",
            "its general format prints the AT END line without brackets, and §14.9.34.4 GR3 gives that phrase "
            + "the only defined destination for control when no next logical record exists — without it a "
            + "RETURN at end of data falls through onto a record area the same rule leaves undefined",
            "ISO §14.9.34.2");
        return new BoundReturn(file, area, into, atEnd, notAtEnd, SortVaryingOf(file));
    }

    // ── Shared sort-family helpers ─────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ An SD with NO record description entry is ILLEGAL SOURCE, not a compiler gap (kb/Work PB345).
    /// §13.4.6.3 SR2 — "One or more record description entries shall be associated with the sort-merge file
    /// description entry." — and <c>DataBinder.BindFileSection</c> has already rejected it, COBOLNET1837. Until
    /// PB345 the three sort-family verbs fell into their <c>BoundUnsupported</c> arms instead, so the ONLY
    /// diagnostic a record-less SD ever drew was <c>SORT 'S1' without an SD record — not implemented</c>
    /// (COBOLNET1756, a WARNING): the compiler apologising for the source's error and then aborting the run unit
    /// at the statement. The statement binds to a no-op here because the entry's own error already failed the
    /// compile; announcing a DEFERRAL on top of it would be a second, wrong diagnosis of the same fault.</summary>
    private static bool RecordLessSd(FileModel file) => file.Records.Count == 0;

    /// <summary>The SD's canonical record (the first 01 — secondary 01s share its area via the synthesized
    /// REDEFINES, ISO §9.1.2), or null when absent / not image-capable (the sort store carries record IMAGES —
    /// zoned, radix-2, BCD and the kb/Work PB164 IEEE forms per the leaves' pinned byte representations,
    /// COBOLNET_DESIGN §14.4/§8.2; only a variable-length or pointer/object-leafed record keeps the record out
    /// of the image store — every NUMERIC leaf kind joined the image, kb/Work PB164 + R40 — deferred, loud).
    /// <para>A VARIABLE-LENGTH record whose current extent composes (<see cref="DataItem.CurrentExtentImageCapable"/>)
    /// is admitted since kb/Work PB981: the store carries its CONTIGUOUS image (ISO §8.5.1.11.2 — RELEASE sends
    /// <c>CurrentImage()</c>) and RETURN decomposes it back (determination D-FRA, docs/CONFORMANCE.md §3).</para></summary>
    private static DataItem? SortRecordOf(FileModel file) =>
        file.Records.Count > 0 && (file.Records[0].IsElementary || file.Records[0].IsImageCapable
            || file.Records[0].CurrentExtentImageCapable)
            ? file.Records[0] : null;

    /// <summary>Bind one ASC/DESC key phrase's data-names into <paramref name="keys"/> (ISO §14.9.40 GR1 — the
    /// direction word is transitive across the phrase's data-names; each <c>sortKeyPhrase</c>/<c>mergeKeyPhrase</c>
    /// begins with its own ASCENDING|DESCENDING, so per-phrase application IS GR1 — and GR2: significance is
    /// statement order, which the appended list preserves). Returns false when the phrase was REFUSED and reported.</summary>
    private bool SortAddFileKeys(bool descending, Core.DataReferenceListContext? list, FileModel file,
        List<BoundSortMergeKey> keys, SortKeyRules rules)
    {
        var drefs = list?.dataReference() ?? [];
        if (drefs.Length == 0)
            return Reject("SORT/MERGE key phrase without data-name-1 — the file formats require key data-names "
                + "(ISO §14.9.40.2 Format 1 / §14.9.24.2)");
        foreach (var dref in drefs)
        {
            // §14.9.40.2 Format 1 / §14.9.24.2 print `KEY { data-name-1 } …` — a qualified-data-name, never an
            // identifier: §8.4.3.3.3's NOTE forbids the reference-modifier and §14.9.40.3 SR6 b) the subscript. The one
            // data-name-n screen refuses both BEFORE the resolver, which would otherwise hand back the base item
            // with the modifier dropped and key the file on the whole field (kb/Work PB481).
            if (!DataBinder.ScreenDataNameShape(dref, "SORT/MERGE key", ctx.Edition, DataBinder.NotSubjectToOccurs))
                return false;
            // ⛔ WHICH ITEMS MAY BE A KEY is the ONE admissibility predicate's (kb/Work PB1173, PB1052): §14.9.40.3 SR6 b)
            // c) d) f) / §14.9.24.3 SR4, asked of the NAMED item before any occurrence is resolved — the resolver's own
            // SR5 screen would answer a key under an OCCURS with "a table element needs a subscript", which is true
            // and is not the rule this statement breaks (SR6 b): no subscript would make it legal either).
            if (ctx.Refs.ResolveTableOperand(dref) is { } named && SortKeyAdmission.Violation(named.Item, rules) is { } violation)
                return Reject(violation);
            // Qualification supported (e.g. ST139A's `KEY-1 OF DATA-NAME-1`) via the one reference resolver.
            // A key that did not resolve carries the resolver's diagnostic, never a second "unresolvable" one
            // (kb/Work PB1030).
            if (host.Expr.ResolveSending(dref).PlaceOrReported(ctx.Edition) is not { } kp) return false;
            DataItem item = kp.Item;
            DataItem root = SortRootOf(item);
            if (!file.Records.Contains(root))
                return Reject($"SORT/MERGE key '{DataBinder.WrittenText(dref)}' is not described in a record of '{file.CobolName}' "
                    + "(ISO §14.9.40.3 SR6a)");
            if (Model.RecordLayout.OffsetInRecord(root, item) is null)
                return Reject($"SORT/MERGE key '{DataBinder.WrittenText(dref)}' — key data-names shall not be subject to any OCCURS "
                    + "clause (ISO §14.9.40.3 SR6b/SR6f)");
            // ⛔ THE CLASS IS THE OPERAND'S, AND IT IS ASKED ONCE (kb/Work PB678). §14.9.40.4 GR5 / §14.9.24.4 GR5
            // select the collating sequence by the KEY's class, so the key descriptor carries the class rather
            // than a single "numeric?" bit — and it reads OperandPic, the ONE operand-category reader (D20), so a
            // national or bit GROUP key is the national / boolean operand §13.18.29.4 GR2b/GR1b makes it.
            CollatingClass cls = CollatingSelection.Of(item.OperandPic);
            // ⛔ THE WINDOW IS IN BYTES (kb/Work PB327 + PB678). OffsetInRecord above walks the PHYSICAL codec
            // layout — §14.9.40.3 SR6e's "same character positions" over the record IMAGE, whose basis is bytes —
            // so the length must be the key's BYTE extent too. ByteWidth IS ImageWidth for every leaf kind but
            // NATIONAL, whose position is two bytes (§13.18.60.4 GR8; D-N1), so this read is byte-identical for
            // every non-national key and is what stops a national key's window covering only its first half:
            // with ImageWidth the three keys N"A"/N"B"/N"C" all collapsed onto the shared high byte U+0000 and
            // compared EQUAL, which is a stable sort returning the release order.
            int len = item.IsGroup ? Model.RecordLayout.AreaWidth(item) : item.ByteWidth;
            if (len <= 0) return Reject($"SORT/MERGE key '{DataBinder.WrittenText(dref)}' has no character image");
            // ⛔ THE KEY'S WINDOW IN ITS RECORD, AND HOW FAR IT CAN REACH (kb/Work PB1025): RecordLayout.KeyWindowOf
            // is the ONE answer the indexed key rules read too. A key a dynamic-length item or a dynamic-capacity
            // table precedes has no fixed position in the contiguous image the store holds (§8.5.1.11.2), so it
            // carries its record's layout and is located per record at run time (D-KWV); it used to be refused by
            // name here (kb/Work PB981), which rejected legal source whenever trailing fixed material kept it
            // within the minimum record size.
            if (Model.RecordLayout.KeyWindowOf(root, item, len) is not { } win)
                return Reject($"SORT/MERGE key '{DataBinder.WrittenText(dref)}' — key data-names shall not be subject to any OCCURS "
                    + "clause (ISO §14.9.40.3 SR6b/SR6f)");
            // §14.9.40.3 SR6 g) / §14.9.24.3 SR4 g): with variable-length records every key lies within the first
            // n bytes, n the minimum record size — for ANY record, so a key that follows a variable-length member is
            // measured at its furthest reach (every preceding member at its maximum). ⛔ The minimum is the file's
            // (§13.18.43.4 GR9 when the RECORD clause states none — FileModel.VaryMin), and the rule applies to
            // every file whose records vary, including the implied Format 2 of a variable-length record (D-FRA);
            // it used to fire only when a RECORD clause wrote integer-2.
            if (file.RecordSizeVaries && file.VaryMin is var min && win.MaxEnd > min)
                ctx.Edition.Error("COBOLNET0874", $"SORT/MERGE key '{DataBinder.WrittenText(dref)}' "
                    + (win.FollowsVariable
                        ? $"follows a variable-length member of record '{root.CobolName}' and can reach byte {win.MaxEnd}"
                        : $"occupies character positions {win.Offset + 1}..{win.MaxEnd}")
                    + $" of the record, but '{file.CobolName}' describes variable-length records with minimum size "
                    + $"{min} — all key data items shall be contained within the first {min} bytes "
                    + "(ISO §14.9.40.3 SR6 g); §14.9.24.3 SR4 g) for MERGE)");
            // A numeric key carries the LEAF ITSELF, so the runtime decodes its window with the leaf's own
            // profile — the one description of its bytes (zoned digits for DISPLAY, radix-2 / BCD for
            // BINARY / PACKED, the IEEE interchange forms for the float family — kb/Work PB164 wave 2; V59).
            // §14.9.40 GR8 + §8.8.4.2.4: numeric keys compare by ALGEBRAIC value regardless of how their
            // usage is described, so the decode must match the representation exactly — CobolSort's column
            // builder dispatches on the profile's ByteForm (a float key's raw big-endian IEEE bytes would
            // order every negative after every positive, so it takes the algebraic double lane).
            keys.Add(new BoundSortMergeKey(descending, win.Offset, len, cls,
                cls is CollatingClass.Numeric ? item : null,
                win.FollowsVariable ? ctx.Refs.ResolveItem(root) : null));
        }
        return true;

        bool Reject(string err) { ctx.Validation.RejectStatementOperand(err); return false; }   // PB236
    }

    /// <summary>Resolve the COLLATING SEQUENCE phrase into the GR5 sequence PAIR. ISO §14.9.40.4 GR5 /
    /// §14.9.24.4 GR5 determine the two SEPARATELY, each in this order of precedence: a) the statement's own
    /// phrase — alphabet-name-1 for keys of class alphabetic and alphanumeric, alphabet-name-2 for keys of class
    /// national, and a NATIVE/STANDARD-1/STANDARD-2 alphabet there FORCES the native order over any PCS; b) the
    /// program collating sequences. Null in either half = that class's native order, and a phrase naming only one
    /// class leaves the OTHER on its program collating sequence (GR5b, per class). The COLLATING keyword itself may
    /// be omitted in the source — it is an OPTIONAL word (§5.2.3: the printed formats underline only SEQUENCE), so
    /// `SEQUENCE IS alphabet-name` (ST139A's spelling) is standard and nothing flags it (kb/Work PB1139). Alphabet-name-2 / the FOR
    /// NATIONAL form are CLASS-VALIDATED here by the one test every COLLATING SEQUENCE operand shares
    /// (<c>DataBinder.CollatingAlphabetFault</c>; §14.9.40.3 SR1/SR2 — a UTF-8/UTF-16 alphabet references NO
    /// collating sequence, §12.3.7 Table 6), and since kb/Work PB678 the
    /// resolved national half IS carried into the bound node: a national key reaches the comparator (PB327 admitted
    /// national leaves to FD/SD records) and GR5 is what tells it which sequence to use.</summary>
    private (SortCollation Collation, BoundStatement? Error) SortBindCollating(Core.SortCollatingPhraseContext? c)
    {
        // GR5b — the program collating sequences, per class (null ⇒ that class's native order).
        var pcs = new SortCollation(ctx.Data.Collating, ctx.Data.NationalCollating);
        if (c is null) return (pcs, null);

        var (alnumName, natName) = ChoiceIndicators.AlphabetPair(ctx.Edition, c.collatingForPhrase(),
            f => f.NATIONAL() is not null, f => f.cobolWord().GetText(), c.cobolWord(),
            "SORT/MERGE COLLATING SEQUENCE", "14.9.40.2");

        // Alphabet-name-2 (national keys, GR5a): resolve + class-validate, and CARRY the sequence (PB678). A name
        // that fails either check leaves the national half on the program collating sequence — the diagnostic is
        // the verdict, and inventing a sequence for a rejected alphabet-name would only add a second wrong answer.
        // BOTH slots ask the ONE class test the PROGRAM COLLATING SEQUENCE and the file COLLATING SEQUENCE clauses
        // ask (DataBinder.CollatingAlphabetFault, kb/Work PB1082) — SORT states the rules as SR1/SR2, MERGE as SR5/SR6.
        NationalAlphabetDef? nat = pcs.National;
        if (natName is not null)
        {
            if (ctx.Data.CollatingAlphabetFault(natName, national: true) is { } natFault)
                ctx.Edition.Error("COBOLNET0898", DataBinder.CollatingAlphabetViolation("SORT/MERGE COLLATING SEQUENCE",
                    natName, national: true, natFault, "ISO §14.9.40.3 SR2; §14.9.24.3 SR6"));
            else if (ctx.Data.NationalAlphabets.TryGetValue(natName, out var def))
                nat = def.IsIdentity ? null : def;   // GR5a — an identity national alphabet (NATIVE/UCS-4) ⇒ native
        }

        // Alphabet-name-1 (alphabetic/alphanumeric keys, GR5a); a FOR NATIONAL-only phrase leaves the
        // alphanumeric keys on the program collating sequence (GR5b per class).
        if (alnumName is null) return (pcs with { National = nat }, null);
        if (!ctx.Data.Alphabets.TryGetValue(alnumName, out var alnumDef))
        {
            if (ctx.Data.CollatingAlphabetFault(alnumName, national: false) is { } fault)
                ctx.Edition.Error("COBOLNET0898", DataBinder.CollatingAlphabetViolation("SORT/MERGE COLLATING SEQUENCE",
                    alnumName, national: false, fault, "ISO §14.9.40.3 SR1; §14.9.24.3 SR5"));
            // A FOR NATIONAL alphabet in the slot leaves the statement bindable (the alphanumeric keys fall to the
            // native order); any other word is no alphabet at all and the statement is rejected (PB236).
            if (ctx.Data.NationalAlphabets.ContainsKey(alnumName)) return (new SortCollation(null, nat), null);
            return (SortCollation.Native, BoundRejected.Reported(ctx.Edition));
        }
        // GR5a — the statement's own sequences (an identity alphabet ⇒ native, no carrier emitted).
        return (new SortCollation(alnumDef.IsIdentity ? null : alnumDef, nat), null);
    }

    /// <summary>Map a USING/GIVING file list to <see cref="FileModel"/>s. Each shall be an FD file — never an SD
    /// (ISO §14.9.40.3 SR8) — of ANY organization: the implicit OPEN/READ/WRITE/CLOSE of GR12/GR15 run through the
    /// runtime file facade, which dispatches to the sequential, relative and indexed connectors alike
    /// (<c>SortEmitter</c>; kb/Work PB994). A relative or indexed file is admitted when its access mode is not
    /// RANDOM (§12.4.5.5.2 SR1 for USING and GIVING; §14.9.40.3 SR12 / §14.9.24.3 SR13 for USING).</summary>
    /// <remarks>The name resolves through the ONE statement file-name resolution, <c>ResolveFile</c>, which also
    /// refuses a REPORT file with the statement's own restatement of §13.4.5.3 SR9 — SORT §14.9.40.3 SR8 / MERGE
    /// §14.9.24.3 SR9, "described in a file description entry that is not for a report file" (kb/Work PB1171; this
    /// method used to look the name up privately and asked only the sort-merge half). A failure there is REPORTED
    /// and answers false; so does every other refusal here, each reported once.</remarks>
    private bool SortMapIoFiles(Core.DataReferenceListContext? list, List<FileModel> files, bool merge, bool giving)
    {
        string verb = merge ? "MERGE USING/GIVING" : "SORT USING/GIVING";
        string reportRule = merge
            ? "ISO §14.9.24.3 SR9 — \"File-name-2, file-name-3, and file-name-4 shall be described in a file "
              + "description entry that is not for a report file and is not a sort-merge file description entry\""
            : "ISO §14.9.40.3 SR8 — \"File-name-2 and file-name-3 shall be described in a file description entry "
              + "that is not for a report file and is not a sort-merge file description entry\"";
        foreach (var dref in list?.dataReference() ?? [])
        {
            string name = dref.cobolWord()?.GetText() ?? dref.GetText();
            if (!ctx.Validation.ResolveFile(name, verb, out var f, statementRule: reportRule))
                return false;
            if (f.IsSortMerge)
                return ctx.Validation.RejectStatementOperand(
                    $"{verb} file '{name}' shall not be a sort-merge file ({(merge ? "ISO §14.9.24.3 SR9" : "ISO §14.9.40.3 SR8")})");
            // ⛔ THE ACCESS-MODE RULE, BY NAME (kb/Work PB994). §12.4.5.5.2 SR1 — "The RANDOM clause shall not be
            // specified for file-names specified in the USING or GIVING phrase of a SORT or MERGE statement" — holds for
            // both phrases; §14.9.40.3 SR12 / §14.9.24.3 SR13 restate it for USING ("If file-name-2 references a
            // relative or an indexed file, its access mode shall be sequential or dynamic"). The refusal used to say
            // the keyed organizations were "the G5 slice" — a statement about the compiler — for every such file.
            if (f.AccessMode == FileAccessMode.Random)
                return ctx.Validation.RejectStatementOperand(
                    $"{verb} file '{name}' is described with ACCESS MODE IS RANDOM: \"The RANDOM clause shall not be specified "
                    + "for file-names specified in the USING or GIVING phrase of a SORT or MERGE statement\" (ISO §12.4.5.5.2 SR1"
                    + (giving ? ")" : merge ? "; §14.9.24.3 SR13)" : "; §14.9.40.3 SR12)"));
            files.Add(f);
        }
        return true;
    }

    /// <summary>⛔ THE INDEXED-GIVING KEY RULE, ONE SCREEN FOR BOTH VERBS (kb/Work PB994). §14.9.40.3 SR9: "If file-name-3
    /// references an indexed file, the first specification of data-name-1 shall be associated with an ASCENDING phrase
    /// and the data item referenced by that data-name-1 shall begin at the same byte location within its record and
    /// occupy the same number of bytes as the prime record key for that file"; §14.9.24.3 SR10 is the MERGE twin ("shall
    /// occupy the same byte positions in its record as the data item associated with the prime record key"). The records
    /// reach an indexed file in the sort's order, so only an ascending first key on the prime key's bytes writes them
    /// in the prime key's order — without this screen, lifting the keyed-file refusal would turn an over-rejection into
    /// an under-rejection. Returns false when a violation was REPORTED.</summary>
    private bool ScreenIndexedGivingKey(IReadOnlyList<BoundSortMergeKey> keys, IReadOnlyList<FileModel> givingFiles, bool merge)
    {
        if (keys.Count == 0) return true;
        var first = keys[0];
        foreach (var g in givingFiles)
        {
            if (g.Organization != FileOrganization.Indexed || g.RecordKeyItem is not { } prime) continue;
            string? why = null;
            if (first.Descending)
                why = "the first specification of data-name-1 is associated with a DESCENDING phrase";
            else if (Model.RecordLayout.KeyWindowOf(SortRootOf(prime), prime, prime.ByteWidth) is { } pw
                     && (pw.Offset != first.Offset || pw.Bytes != first.Length))
                why = $"the first key occupies bytes {first.Offset + 1}..{first.Offset + first.Length} of its record but the "
                    + $"prime record key '{prime.CobolName}' occupies bytes {pw.Offset + 1}..{pw.Offset + pw.Bytes}";
            if (why is null) continue;
            return ctx.Validation.RejectStatementOperand($"{(merge ? "MERGE" : "SORT")} GIVING file '{g.CobolName}' is an indexed file, "
                + $"so the first key shall be ASCENDING and lie at the prime record key's byte location and size: {why} "
                + $"({(merge ? "ISO §14.9.24.3 SR10" : "ISO §14.9.40.3 SR9")})");
        }
        return true;
    }

    /// <summary>⛔ THE RECORD-SIZE RULES BETWEEN A SORT-MERGE FILE AND THE FILES OF ITS USING AND GIVING PHRASES, ONE SCREEN
    /// FOR BOTH VERBS AND BOTH DIRECTIONS (kb/Work PB995). USING (§14.9.40.3 SR5; MERGE's twin §14.9.24.3 SR3): "If the
    /// file description entry for file-name-1 describes variable-length records, the file description entry for
    /// file-name-2 shall describe neither records smaller than the smallest record nor larger than the largest record
    /// described for file-name-1. If the file description entry for file-name-1 describes fixed-length records, the file
    /// description entry for file-name-2 shall not describe a record that is larger than the record described for
    /// file-name-1." GIVING (§14.9.40.3 SR11; MERGE's twin §14.9.24.3 SR12) is the same rule with the two roles
    /// exchanged: the GIVING file's description is the bound, and the sort-merge file's records are the ones measured.
    /// Both reduce to ONE comparison of two <see cref="FileModel.RecordSizeRange"/>s — the bounding file's variable-length
    /// form bounds both ends, its fixed-length form only the upper one. A file with no record description has no size to
    /// compare (its own entry is refused where it is described). Returns false when a violation was REPORTED.</summary>
    private bool ScreenTransferRecordSizes(FileModel sd, IReadOnlyList<FileModel> usingFiles, IReadOnlyList<FileModel> givingFiles,
        bool merge)
    {
        string verb = merge ? "MERGE" : "SORT";
        foreach (var (other, giving) in usingFiles.Select(f => (f, false)).Concat(givingFiles.Select(f => (f, true))))
        {
            if (sd.Records.Count == 0 || other.Records.Count == 0) continue;
            // The file whose description BOUNDS the other: the sort-merge file for USING, the GIVING file for GIVING.
            var (bound, measured) = giving ? (other, sd) : (sd, other);
            var (lo, hi) = bound.RecordSizeRange;
            var (min, max) = measured.RecordSizeRange;
            bool tooSmall = bound.RecordSizeVaries && min < lo, tooLarge = max > hi;
            if (!tooSmall && !tooLarge) continue;
            string rule = (merge, giving) switch
            {
                (false, false) => "ISO §14.9.40.3 SR5",
                (false, true) => "ISO §14.9.40.3 SR11",
                (true, false) => "ISO §14.9.24.3 SR3",
                (true, true) => "ISO §14.9.24.3 SR12",
            };
            string Describe(FileModel f) =>
                f.RecordSizeVaries ? $"variable-length records of {f.RecordSizeRange.Min} to {f.RecordSizeRange.Max} bytes"
                : $"fixed-length records of {f.RecordSizeRange.Max} bytes";
            ctx.Validation.RejectStatementOperand($"{verb} {(giving ? "GIVING" : "USING")} file '{other.CobolName}': "
                + $"'{bound.CobolName}' describes {Describe(bound)} but '{measured.CobolName}' describes {Describe(measured)} — "
                + $"the file description entry for '{measured.CobolName}' shall "
                + (bound.RecordSizeVaries ? "describe neither records smaller than the smallest record nor larger than the largest record"
                    : "not describe a record that is larger than the record")
                + $" described for '{bound.CobolName}' ({rule})");
            return false;
        }
        return true;
    }

    /// <summary>⛔ THE SAME-CLAUSE RULES OF THE SORT AND MERGE STATEMENTS, ONE SCREEN (kb/Work PB1139), over the I-O-CONTROL
    /// membership <see cref="FileModel.SameClauses"/> records for every SAME format. §14.9.40.3 SR10: "No pair of
    /// file-names in the same SORT statement may be specified in the same SAME SORT AREA or SAME SORT-MERGE AREA
    /// clause. File-names associated with the GIVING phrase shall not be specified in the same SAME AREA clause."
    /// §14.9.24.3 SR11: "No pair of file-names in a MERGE statement may be specified in the same SAME AREA, SAME SORT
    /// AREA, or SAME SORT-MERGE AREA clause. The only file-names in a MERGE statement that may be specified in the same
    /// SAME RECORD AREA clause are those associated with the GIVING phrase." — a pair of the statement's files (file-name-1
    /// included) in one barred clause, whatever else the clause names. Returns false when a violation was REPORTED.</summary>
    private bool ScreenSameClauses(FileModel sd, IReadOnlyList<FileModel> usingFiles, IReadOnlyList<FileModel> givingFiles,
        bool merge)
    {
        var statement = new List<FileModel> { sd };
        statement.AddRange(usingFiles.Concat(givingFiles).Where(f => !ReferenceEquals(f, sd)).Distinct().ToList());
        string verb = merge ? "MERGE" : "SORT";
        string rule = merge ? "ISO §14.9.24.3 SR11" : "ISO §14.9.40.3 SR10";
        foreach (var clause in statement.SelectMany(f => f.SameClauses).Distinct())
        {
            var named = statement.Where(clause.Members.Contains).ToList();
            if (named.Count < 2) continue;
            string? why = clause.Kind switch
            {
                SameClauseKind.SortMergeArea =>
                    $"no pair of file-names in the {verb} statement may be specified in the same {clause.Written} clause",
                SameClauseKind.Area when merge =>
                    "no pair of file-names in the MERGE statement may be specified in the same SAME AREA clause",
                SameClauseKind.Area when named.Count(givingFiles.Contains) > 1 =>
                    "file-names associated with the GIVING phrase shall not be specified in the same SAME AREA clause",
                SameClauseKind.RecordArea when merge && named.Any(f => !givingFiles.Contains(f)) =>
                    "the only file-names in a MERGE statement that may be specified in the same SAME RECORD AREA clause "
                    + "are those associated with the GIVING phrase",
                _ => null,
            };
            if (why is null) continue;
            ctx.Validation.RejectStatementOperand($"{verb} files {string.Join(", ", named.Select(f => $"'{f.CobolName}'"))} "
                + $"are named in one SAME clause: {why} ({rule})");
            return false;
        }
        return true;
    }

    /// <summary>An INPUT/OUTPUT PROCEDURE name pair → the inclusive pc range (ISO §14.9.40 GR10/GR13 — the range
    /// composes like PERFORM: a single SECTION name is its whole paragraph range, THRU extends through the second
    /// procedure's end). Resolved by the ONE procedure resolver, so section/qualified semantics match PERFORM.</summary>
    private PcRange? SortRange(Core.ProcedureNameContext[] names, string phrase, string formatClause)
    {
        if (names.Length == 0)
        {
            ctx.Validation.RejectStatementOperand(
                $"{phrase} — the general format prints procedure-name-1 (ISO {formatClause})");
            return null;
        }
        // ⛔ EACH NAME REPORTS ITSELF (kb/Work PB390). SORT's own private "unknown procedure" message folded
        // into the ONE procedure-name resolution, which also ends a small lie: the caller used to report
        // procedureName(0) whichever of the two names had failed, so a bad THRU name accused the good one.
        if (ctx.Table.ResolveProcedureOperand(names[0], phrase) is not { } first) return null;
        if (names.Length < 2) return first.Range;
        if (ctx.Table.ResolveProcedureOperand(names[1], phrase + " THRU") is not { } thru) return null;
        return first.Range.Through(thru.Range);   // GR4-style composition, EMPTY-aware (kb/Work PB440)
    }

    /// <summary>The varying-record model of an SD/FD for the sort verbs (§13.18.43 GR13/GR15), with the DEPENDING
    /// item (when declared) resolved to its place; null when the file's records are fixed-length. Min/max default
    /// per GR9/GR10 (the smallest/largest record described) via the FileModel accessors.</summary>
    private SortVaryingInfo? SortVaryingOf(FileModel file)
    {
        if (!file.RecordSizeVaries) return null;   // an explicit variable-length RECORD clause, or D-FRA's implied Format 2 (kb/Work PB981)
        Place? dep = file.VaryingDependingItem is { } d ? ctx.Refs.ResolveItem(d) : null;
        return new SortVaryingInfo(dep, file.VaryMin, file.VaryMax);
    }

    /// <summary>The record root (01) an item belongs to.</summary>
    private static DataItem SortRootOf(DataItem item)
    {
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        return root;
    }

    /// <summary>One Format-2 key over <paramref name="key"/> — the ONE construction both key sources share (the
    /// statement's KEY phrase, §14.9.40.4 GR2, and the table's own OCCURS KEY phrase, GR21), so the member-path
    /// rule cannot drift between them. A key of a <see cref="TableSortStorage.SharedArea"/> table is the item itself,
    /// addressed through the class's window law, so it must lie in the SAME class as the table; a typed-array key is
    /// a member path over the element struct — or, when it lies behind a REDEFINES view of the element (no stored field
    /// on the struct), a key with a NULL member path that the statement reads through its window at an occurrence number
    /// (<see cref="TableSortStorage.TypedArray.KeyWindowOuter"/>; kb/Work PB599). <see langword="null"/> only for a key
    /// no window can be placed for (see <see cref="TableSortKeyUnsupported"/>).</summary>
    private BoundTableSortKey? TableSortKey(DataItem table, TableSortStorage storage, IReadOnlyList<string> outer,
        bool descending, DataItem key)
    {
        if (storage is TableSortStorage.SharedArea)
            return ReferenceEquals(key.Class, table.Class) ? new BoundTableSortKey(descending, "", key) : null;
        if (SortMemberPath(table, key) is { } path) return new BoundTableSortKey(descending, path, key);
        // No stored field: a REDEFINES-view key (§13.18.44.4 GR1 — every view of the class is the one backing). A key
        // under an inner OCCURS is not a view problem and has no window either (§14.9.40.3 SR14 e / §13.18.38.3 SR6).
        return key.Class is not null && !KeyUnderInnerOccurs(table, key)
            && ctx.Refs.ResolveItemAt(key, [.. outer, "1"]) is not null
            ? new BoundTableSortKey(descending, null, key) : null;
    }

    private static BoundUnsupported TableSortKeyUnsupported(string keyName) =>
        new($"SORT table key '{keyName}': the key has no stored field on the element struct and no window the place "
            + "builder can position — a key under an inner OCCURS on the table's own KEY phrase (§13.18.38.3 SR6; the "
            + "declaration's refusal is kb/Work PB1263), or a view of a class this statement cannot address "
            + "(an inner OCCURS between a written key and data-name-2 is refused before this, §14.9.40.3 SR14 e)");

    /// <summary>§14.9.40.3 SR14 e): the key, or an entry between it and data-name-2, carries an OCCURS clause.
    /// A predicate over the DATA DESCRIPTION alone — the member path below answers a storage question and returns
    /// null for a REDEFINES view too, so it cannot tell the source's error from the compiler's gap.</summary>
    private static bool KeyUnderInnerOccurs(DataItem table, DataItem key)
    {
        for (DataItem? n = key; n is not null && !ReferenceEquals(n, table); n = n.Parent)
            if (n.Occurs is not null) return true;
        return false;
    }

    /// <summary>The C# member path of <paramref name="key"/> RELATIVE to a table-element variable ("" when the key
    /// IS the element), or null when an inner OCCURS / REDEFINES view intervenes (SR14e; the suppressed view field
    /// does not exist on the element struct).</summary>
    private static string? SortMemberPath(DataItem table, DataItem key)
    {
        if (ReferenceEquals(table, key)) return "";
        if (key.Class is not null) return null;   // a Tier-A/B view member — no stored field on the struct
        var segs = new List<string>();
        for (DataItem? n = key; n is not null && !ReferenceEquals(n, table); n = n.Parent)
        {
            if (n.Occurs is not null) return null;   // §14.9.40.3 SR14 e — refused earlier by KeyUnderInnerOccurs
            segs.Add(n.CsName);
            if (n.Parent is null) return null;        // ran off the root without meeting the table
        }
        segs.Reverse();
        return string.Join(".", segs);
    }

}
