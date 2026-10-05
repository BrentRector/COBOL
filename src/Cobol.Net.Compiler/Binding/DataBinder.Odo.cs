// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Compiler.Oo;
using CobolNet.Runtime;

/// <summary>
/// The OCCURS DEPENDING ON half of the data binder (ISO/IEC 1989:2023 §13.18.38): Format-2 clause capture
/// (<see cref="OdoBindOccursSpec"/>) and the post-build resolution + structural-validation pass
/// (<see cref="OdoResolve"/>). Partial-class extension over <c>DataBinder</c> — the entry binder stores the array
/// capacity (the MAXIMUM, §8.5.1.8) in <see cref="DataItem.Occurs"/> and the bounds/DEPENDING/KEY surface here.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>Capture an OCCURS clause's structured description: the Format-2 <c>integer-1 TO integer-2</c>
    /// bounds and DEPENDING ON name, plus the ASCENDING/DESCENDING KEY data-names (Formats 1 and 2, §13.18.38
    /// GR3). Returns <see langword="null"/> for a plain keyless fixed table — <see cref="DataItem.Occurs"/>
    /// alone carries those (the dominant case stays allocation-free).</summary>
    private OccursSpec? OdoBindOccursSpec(Core.OccursClauseContext occ, string where, int? maxBound)
    {
        NarrowToDataDivisionFormats(occ, where);
        bool depending = occ.DEPENDING() is not null;
        // ONE list in PHRASE ORDER — ISO §13.18.38.4 GR3 "If more than one data-name-2 is specified, they are
        // specified in descending order of significance", which is what §14.9.37.3 SR11 is a rule about. Splitting
        // the phrase into per-direction lists would lose the relative order of a mixed
        // `ASCENDING KEY IS A B DESCENDING KEY IS C`. The name AND ITS QUALIFIERS (kb/Work PB1018): §13.18.38.3 SR3
        // confines data-name-2 to the OCCURS entry or an entry subordinate to it, but two subordinates may share the
        // name, and then `K OF B` is the only thing that says which one is the key (§8.4.2.2.3 SR1). The capture
        // used to keep the base word alone, and the lookup took the first K under the table.
        var keys = new List<OccursKey>();
        foreach (var kc in occ.occursKeyClause())
        {
            bool descending = kc.DESCENDING() is not null;
            // SCREENED first (kb/Work PB885): §13.18.38.3 SR2 "Data-name-1 and data-name-2 shall not be
            // subscripted" and SR5 "Data-name-2 shall be specified without the subscripting normally required" —
            // a written subscript used to be dropped here in silence. A refused key is not recorded: the
            // refusal is the verdict, and a key named by its written spelling would only draw a second one.
            foreach (var k in kc.dataReference())
            {
                var (keyName, keyQuals) = ClauseDataName(k, $"{where}: OCCURS … KEY IS");
                if (!_refusedClauseOperands.Contains(keyName)) keys.Add(new OccursKey(keyName, keyQuals, descending));
            }
        }

        // Format 4 — a DYNAMIC-capacity table (§13.18.38 Format 4, D9): capture CAPACITY IN / FROM / TO / INITIALIZED
        // (each at most once and in the printed order — NarrowToDataDivisionFormats has refused anything else, so a
        // phrase here is the only one of its kind). ALWAYS returns a spec (a keyless dynamic table still needs IsDynamic recorded,
        // unlike a keyless fixed table where DataItem.Occurs alone suffices). DataItem.Occurs stays null — a dynamic
        // table has no fixed physical capacity; its storage is the out-of-line CobolDynTable.
        if (occ.DYNAMIC() is not null)
        {
            // COBOL-2014 introduction gate: VersionConformancePass ParseArm.VisitOccursClause (rearch 14g.3,
            // recognition — once per source occursClause; a per-DataItem bound-arm walk would over-count TYPE clones).
            string? capName = null; int? fromCap = null; int? toCap = null; bool initialized = false;
            foreach (var ph in occ.occursDynamicPhrase())
            {
                if (ph.CAPACITY() is not null && ph.dataReference() is { } capRef)
                    capName = CapacityRegisterName(capRef, where);
                else if (ph.INITIALIZED() is not null) initialized = true;
                else if (ph.FROM() is not null && ph.integerOperand() is { } fl)
                {
                    if (IntegerOperandValue(fl, where) is { } from) fromCap = DynamicBoundWithinMaximum(ph, "FROM", from);
                }
                else if (ph.TO() is not null && ph.integerOperand() is { } tl)
                {
                    if (IntegerOperandValue(tl, where) is { } to) toCap = DynamicBoundWithinMaximum(ph, "TO", to);
                }
            }
            var dyn = new OccursSpec
            {
                Min = fromCap ?? 0, Max = 0, IsDynamic = true,
                CapacityName = capName, InitialCap = fromCap, ExpectedMax = toCap, Initialized = initialized,
            };
            dyn.Keys.AddRange(keys);
            return dyn;
        }
        if (!depending && keys.Count == 0) return null;

        // Each fixed bound is an integer literal or an integer constant-name (§13.10.3 SR2); the caller already
        // resolved the LAST bound (the maximum) via IntegerOperandValue — <paramref name="maxBound"/> — so an
        // unresolvable bound reports exactly once. Only integer-1 of a Format-2 pair resolves here.
        var bounds = occ.integerOperand();
        int max = maxBound ?? 0;
        // Format 2 is `OCCURS integer-1 TO integer-2 … DEPENDING …` (§13.18.38 general formats); `OCCURS n
        // DEPENDING` without TO was REFUSED by NarrowToDataDivisionFormats (kb/Work PB1265) and binds here, under
        // that failed compile, with minimum 1 only so the rest of the entry still binds. Min feeds only the SR16
        // check and the later EC-BOUND-ODO bounds — allocation is ALWAYS Max (§8.5.1.8).
        int min = !depending ? max
            : bounds.Length > 1 ? IntegerOperandValue(bounds[0], where) ?? 1
            : 1;
        // data-name-1 — a QUALIFIED-DATA-NAME through the ONE data-name-n capture (kb/Work PB885). The whole
        // reference's GetText() stood here: `DEPENDING ON CNT OF G1` became the undefined name `CNTOFG1` (legal
        // source rejected), and `DEPENDING ON WS-TE (2)` drew "not defined" instead of §13.18.38.3 SR2.
        string? depName = null;
        IReadOnlyList<string> depQuals = [];
        if (depending && occ.dataReference() is { } depRef)
            (depName, depQuals) = ClauseDataName(depRef, $"{where}: OCCURS … DEPENDING ON");
        var spec = new OccursSpec
        {
            Min = min,
            Max = max,
            DependingName = depName,
            DependingQualifiers = depQuals,
        };
        spec.Keys.AddRange(keys);
        return spec;
    }

    /// <summary>⛔ THE DATA DIVISION'S OCCURS CLAUSE IS FORMAT 1, 2 OR 4 — NARROWED HERE FROM THE GRAMMAR'S SUPERSET
    /// (kb/Work PB1265). <c>occursClause</c> parses every phrase of every format optionally and Format 4's phrases in
    /// any order and number, so that the refusal can name the rule; the report section narrows the same parse to
    /// Format 3 (<c>DataBinder.Reports.cs#ReportOccursOf</c>), and this is the other arm. ISO §13.18.38.2:
    /// <list type="bullet">
    /// <item>Format 2 prints <c>integer-1 TO integer-2 TIMES DEPENDING ON data-name-1</c> with nothing bracketed, so a
    /// TO without DEPENDING and a DEPENDING without TO are each in no data-division format (the bracketed
    /// <c>[ integer-1 TO ]</c> and <c>[ DEPENDING … ]</c> are Format 3's, the report writer's).</item>
    /// <item>STEP integer-3 exists only in Format 3.</item>
    /// <item>Format 4 prints <c>[ CAPACITY IN data-name-3 ] [ FROM integer-4 ] [ TO integer-5 ] [ INITIALIZED ]</c>:
    /// each phrase at most once, and — §5.2.1, "The words, phrases, clauses, punctuation, and operands in each
    /// general format shall be written in the compilation group in the sequence given in the general format, unless
    /// otherwise specified by the rules of that format" — in that order. A repeated FROM used to bind its last value
    /// in silence.</item>
    /// </list>
    /// Each violation is COBOLNET2789; the entry still binds (under the failed compile) so its other clauses are
    /// checked.</summary>
    private static readonly string[] Format4PhraseNames = ["CAPACITY IN", "FROM", "TO", "INITIALIZED"];

    private void NarrowToDataDivisionFormats(Core.OccursClauseContext occ, string where)
    {
        void Refuse(Antlr4.Runtime.ParserRuleContext at, string what)
        {
            using var _ = Edition.At(at);
            Edition.Error(DiagnosticCatalog.OccursFormatNotPrinted, $"{where}: OCCURS clause — {what}");
        }
        if (occ.DYNAMIC() is not null)
        {
            static int Rank(Core.OccursDynamicPhraseContext p) =>
                p.CAPACITY() is not null ? 0 : p.FROM() is not null ? 1 : p.TO() is not null ? 2 : 3;
            var names = Format4PhraseNames;
            int last = -1;
            foreach (var ph in occ.occursDynamicPhrase())
            {
                int r = Rank(ph);
                if (r == last)
                    Refuse(ph, $"the {names[r]} phrase is written twice; Format 4 prints each of its phrases once "
                        + "(ISO §13.18.38.2: OCCURS DYNAMIC [ CAPACITY IN data-name-3 ] [ FROM integer-4 ] "
                        + "[ TO integer-5 ] [ INITIALIZED ])");
                else if (r < last)
                    Refuse(ph, $"the {names[r]} phrase is written after the {names[last]} phrase; Format 4 prints "
                        + "CAPACITY IN, FROM, TO, INITIALIZED in that order (ISO §13.18.38.2), and \"The words, "
                        + "phrases, clauses, punctuation, and operands in each general format shall be written in the "
                        + "compilation group in the sequence given in the general format\" (ISO §5.2.1)");
                last = Math.Max(last, r);
            }
            return;
        }
        if (occ.occursStepPhrase() is { } step)
            Refuse(step, "the STEP phrase belongs only to the report-writer format, which a report group "
                + "description entry uses (ISO §13.18.38.2 Format 3: OCCURS [ integer-1 TO ] integer-2 TIMES "
                + "[ DEPENDING ON data-name-1 ] [ STEP integer-3 ])");
        bool hasTo = occ.TO() is not null, depending = occ.DEPENDING() is not null;
        if (hasTo != depending)
            Refuse(occ, (hasTo ? "integer-1 TO integer-2 is written without DEPENDING ON data-name-1"
                               : "DEPENDING ON data-name-1 is written without integer-1 TO")
                + "; a data description entry's occurs-depending table is Format 2, which prints "
                + "\"OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1\" with no part optional, and a fixed "
                + "table is Format 1, \"OCCURS integer-2 TIMES\" (ISO §13.18.38.2)");
    }

    /// <summary>⛔ §13.18.38.3 SR29 (kb/Work PB1264): "The implementor shall specify a maximum permissible value for
    /// integer-4 and integer-5. Their values shall not exceed this maximum value." The maximum is the highest
    /// permissible occurrence number of a dynamic-capacity table, <c>CobolDynTable.MaxOccurrences</c>
    /// (docs/CONFORMANCE.md DOC-A.1-60), and a FROM / TO value above it is refused HERE — it used to compile and
    /// give capacity 0, a table smaller than its written minimum. Returns null for a refused value, under an
    /// already-failed compile.</summary>
    private int? DynamicBoundWithinMaximum(Core.OccursDynamicPhraseContext phrase, string which, int? value)
    {
        if (value is not { } v || v <= CobolNet.Runtime.CobolDynTable<byte>.MaxOccurrences) return value;
        using var _ = Edition.At(phrase);
        Edition.Error(DiagnosticCatalog.OccursDynamicBoundAboveMaximum,
            $"OCCURS DYNAMIC {which} {v}: the value exceeds this implementation's maximum of "
            + $"{CobolNet.Runtime.CobolDynTable<byte>.MaxOccurrences:N0} for integer-4 and integer-5 "
            + "(docs/CONFORMANCE.md DOC-A.1-60); ISO §13.18.38.3 SR29: \"Their values shall not exceed this maximum "
            + "value.\"");
        return null;
    }

    /// <summary>The name a <c>CAPACITY IN data-name-3</c> phrase DEFINES (ISO §13.18.38.3 SR30 — "Data-name-3 shall
    /// not be defined elsewhere in the source element", i.e. the phrase is its definition), or null when the
    /// written operand is not a data-name. SR31 — "Data-name-3 shall not be subscripted" — and the §8.4.2.2.2
    /// qualified-data-name shape are the <see cref="ClauseDataName"/> screen; a QUALIFIER is refused here too,
    /// because a defining occurrence names the register and SR30 itself supplies its qualification ("it shall be
    /// treated as though implicitly defined at the same level as the entry containing the OCCURS clause"). The
    /// capture was the whole reference's <c>GetText()</c>, so <c>CAPACITY IN CAP3 (1)</c> silently defined a
    /// register spelled <c>CAP3(1)</c> (kb/Work PB885's sibling sweep).</summary>
    private string? CapacityRegisterName(Core.DataReferenceContext capRef, string where)
    {
        var (name, quals) = ClauseDataName(capRef, $"{where}: OCCURS DYNAMIC CAPACITY IN");
        if (_refusedClauseOperands.Contains(name)) return null;
        if (quals.Count == 0) return name;
        using var _ = Edition.At(capRef);
        Edition.Error(DiagnosticCatalog.ClauseOperandNotADataName, $"{where}: OCCURS DYNAMIC CAPACITY IN "
            + $"'{WrittenText(capRef)}' is qualified; data-name-3 is DEFINED by the phrase (ISO §13.18.38.3 SR30), "
            + "and a defining occurrence is a bare data-name whose qualification SR30 itself supplies");
        return null;
    }

    /// <summary>Post-build OCCURS KEY pass (kb/Work PB1018): resolve every data-name-2 of every table's KEY phrase
    /// ONCE, into <see cref="OccursSpec.ResolvedKeys"/>, which SEARCH ALL and the table SORT then read.
    /// <para>The candidates are the table's OWN subtree (ISO §13.18.38.3 SR3 — "The first specification of
    /// data-name-2 shall be the name of either the entry containing the OCCURS clause or an entry subordinate to
    /// the entry containing the OCCURS clause. Subsequent specification of data-name-2 shall be subordinate to the
    /// entry containing the OCCURS clause"), narrowed by the WRITTEN qualifiers through the ONE §8.4.2.2 matcher and
    /// counted by the ONE ambiguity verdict (§8.4.2.2.3 SR1). A key that identifies nothing there is SR3's error,
    /// COBOLNET2353 — it used to be reported by nobody: an unknown key compiled clean and the table SORT then
    /// quietly did nothing. The subtree walk, not the name index, because a TYPEDEF clone's members are off the
    /// index and each clone must bind its OWN key (§13.18.58.4 GR1), exactly as its DEPENDING ON does.</para></summary>
    internal void OccursKeyResolve()
    {
        foreach (var table in AllItems())
        {
            if (table.OccursSpec is not { Keys.Count: > 0 } spec) continue;
            using var _ = Edition.At(table);
            string subject = table.CobolName ?? table.CsName;
            spec.ResolvedKeys.Clear();
            for (int i = 0; i < spec.Keys.Count; i++)
            {
                var key = spec.Keys[i];
                string written = WrittenQualified(key.Name, key.Qualifiers);
                var within = SubtreeCandidates(table, key.Name, key.Qualifiers);
                DataItem? item = UniqueOrReportAmbiguous(within, $"OCCURS … KEY IS data-name-2 of '{subject}'", written,
                    out bool ambiguous);
                if (item is null && !ambiguous)
                    Edition.Error(DiagnosticCatalog.OccursKeyNotWithinTable,
                        $"OCCURS … KEY IS '{written}' on '{subject}': no data item so named "
                        + (key.Qualifiers.Count > 0 ? "under the written qualifiers " : "")
                        + $"is '{subject}' itself or subordinate to it — \"The first specification of data-name-2 "
                        + "shall be the name of either the entry containing the OCCURS clause or an entry subordinate "
                        + "to the entry containing the OCCURS clause\" (ISO §13.18.38.3 SR3)");
                else if (i > 0 && ReferenceEquals(item, table))
                {
                    Edition.Error(DiagnosticCatalog.OccursKeyNotWithinTable,
                        $"OCCURS … KEY IS '{written}' on '{subject}': only the FIRST key data-name may name the "
                        + "table entry itself — \"Subsequent specification of data-name-2 shall be subordinate to the "
                        + "entry containing the OCCURS clause\" (ISO §13.18.38.3 SR3)");
                    item = null;
                }
                else if (item is not null && OccursKeyItemFault(table, item) is { } fault)
                {
                    Edition.Error(DiagnosticCatalog.OccursKeyItemNotAdmitted,
                        $"OCCURS … KEY IS '{written}' on '{subject}': {fault}");
                    item = null;
                }
                spec.ResolvedKeys.Add(item);
            }
        }
    }

    /// <summary>⛔ WHAT a resolved KEY data-name-2 may BE (kb/Work PB1263) — §13.18.38.3's four constraints on the item,
    /// asked once the name has resolved within the table (SR3, above). Null when the key is admitted; otherwise the
    /// broken rule, quoted. Each one exists because a key must have exactly ONE ordered value per table element for
    /// SEARCH ALL and the table SORT to mean anything:
    /// <list type="bullet">
    /// <item>SR4 — "If data-name-2 is subordinate to an alphanumeric group item, bit group item, national group item,
    /// or strongly-typed group item that is subordinate to the entry containing the OCCURS clause, that group item
    /// shall not contain an OCCURS clause": no table may sit BETWEEN the table and its key. The rule's own list of
    /// group kinds is asked (<see cref="ItemCategory.GroupKindsOf"/>), so a group that is ONLY a variable-length
    /// group is not named by it.</item>
    /// <item>SR6 — "The data item identified by data-name-2 shall not contain an OCCURS clause except when
    /// data-name-2 is the subject of the entry".</item>
    /// <item>SR8 — "The KEY phrase shall not be specified for a data item of class boolean, message-tag, object, or
    /// pointer" (<see cref="ItemCategory.IsBooleanMessageTagObjectOrPointer"/>).</item>
    /// <item>SR9 — "Data-name-2 shall not reference a variable-length group", §8.5.1.12.1's "group item whose data
    /// description has at least one dynamic-length elementary item or dynamic-capacity table as a subordinate
    /// item".</item>
    /// </list></summary>
    private static string? OccursKeyItemFault(DataItem table, DataItem key)
    {
        const GroupKinds Sr4Kinds = GroupKinds.Alphanumeric | GroupKinds.Bit | GroupKinds.National | GroupKinds.StronglyTyped;
        // SR4 asks about a group BETWEEN the table entry and its key. When the key IS the table (SR6 admits that), no group
        // lies between them: key.Parent is already above the table, so the walk below would never meet it and would
        // charge the key with any OCCURS group that merely ENCLOSES the table (a table inside OUTER OCCURS 2).
        for (DataItem? g = ReferenceEquals(key, table) ? null : key.Parent; g is not null && !ReferenceEquals(g, table); g = g.Parent)
            if (g.IsTable && (ItemCategory.GroupKindsOf(g) & Sr4Kinds) != 0)
                return $"the key is subordinate to '{g.CobolName ?? g.CsName}', a group within the table that contains an "
                    + "OCCURS clause — \"If data-name-2 is subordinate to an alphanumeric group item, bit group item, "
                    + "national group item, or strongly-typed group item that is subordinate to the entry containing the "
                    + "OCCURS clause, that group item shall not contain an OCCURS clause\" (ISO §13.18.38.3 SR4)";
        if (!ReferenceEquals(key, table) && key.IsTable)
            return "the key's own entry contains an OCCURS clause — \"The data item identified by data-name-2 shall not "
                + "contain an OCCURS clause except when data-name-2 is the subject of the entry\" (ISO §13.18.38.3 SR6)";
        if (ItemCategory.IsBooleanMessageTagObjectOrPointer(key))
            return "the key is a data item of class boolean, message-tag, object or pointer — \"The KEY phrase shall not "
                + "be specified for a data item of class boolean, message-tag, object, or pointer\" (ISO §13.18.38.3 SR8)";
        if (ItemCategory.GroupKindsOf(key).HasFlag(GroupKinds.VariableLength))
            return "the key is a variable-length group (ISO §8.5.1.12.1) — \"Data-name-2 shall not reference a "
                + "variable-length group\" (ISO §13.18.38.3 SR9)";
        return null;
    }

    /// <summary>
    /// Post-build OCCURS DEPENDING ON pass (runs once the whole forest and the redefines classes exist): resolve
    /// each Format-2 table's data-name-1 and enforce the structural syntax rules the GR8 character-prefix model
    /// relies on, each with its ISO citation — SR16 bounds, SR17 integer data-name-1, SR2 unsubscripted
    /// data-name-1, SR1(b)/SR10 no "complex ODO" (an occurs-depending table never nests under another OCCURS, at
    /// EVERY edition — the legacy comment claiming 2002+ legality was wrong), §13.18.44 SR no ODO inside an
    /// explicit REDEFINES, SR22 trailing table, SR20 data-name-1 placement. Violations are bind-time rejections
    /// (<c>Edition.Error</c> fails the compile) — never a silently mis-sized table (SSOT §1.4).
    /// </summary>
    internal void OdoResolve()
    {
        // §13.18.38.3 SR10 / §8.4.2.3.3 SR3 (kb/Work PB1260): "as long as the number of subscripts required does not
        // exceed seven" — a table nested under seven others needs eight. Reported ONCE, at the first entry that
        // exceeds it (an eighth dimension), not again for everything beneath it.
        foreach (var table in ConformanceForest())
        {
            // The ONE arity walk (DataItem.SubscriptArity — SubscriptAdmissionDriftTests pins that it is written nowhere else).
            if (!table.IsTable || table.SubscriptArity != 8) continue;
            using var _ = Edition.At(table);
            Edition.Error(DiagnosticCatalog.TableNestingTooDeep,
                $"table '{table.CobolName ?? table.CsName}' is nested under seven other OCCURS entries: a reference to "
                + "it needs eight subscripts, and \"as long as the number of subscripts required does not exceed "
                + "seven\" (ISO §13.18.38.3 SR10) — at most seven subscripts may be specified (§8.4.2.3.3 SR3)");
        }

        foreach (var item in AllItems())
        {
            if (item.OccursSpec is not { DependingName: { } depName } spec) continue;
            using var _ = Edition.At(item);
            string subject = item.CobolName ?? item.CsName;

            // SR16: 0 ≤ integer-1 < integer-2.
            if (spec.Min < 0 || spec.Min >= spec.Max)
                Edition.Error("COBOLNET0850", $"OCCURS {spec.Min} TO {spec.Max} on '{subject}': integer-1 shall "
                    + "be greater than or equal to zero and less than integer-2 (ISO §13.18.38.3 SR16)");

            // data-name-1 resolution. A counter under a group the table is ALSO subordinate to is found first:
            // §8.4.2.2.1 rule 5 makes the groups superordinate to both the data-name and the subject IMPLICIT
            // qualifiers of a data-name referenced in a data description entry clause. That is also what binds a
            // TYPEDEF clone's internal DEPENDING to the clone's own sibling (review DEVLOG 664 fix #4; §13.18.57.4
            // GR1 — the type is "coded in place"). (This comment used to justify the own-record preference by
            // "§13.18.38.3 SR20, data-name-1 lies within the same record"; SR20 forbids data-name-1 a byte position
            // between the OCCURS entry and the end of its record and places no counter anywhere, kb/Work PB978.)
            // Otherwise the scope-aware set (M2-OO-1h): a method table's data-name-1 resolves in the owning
            // method's scope first (§11.7.4 GR5), then a visible object/program item.
            // A data-name-1 the capture REFUSED (§13.18.38.3 SR2 — subscripted, or not a data-name at all) was
            // reported there; one fault, one verdict (kb/Work PB885).
            if (_refusedClauseOperands.Contains(depName)) continue;
            // ⛔ THE SET IS COUNTED (kb/Work PB978) — one survivor, or §8.4.2.2.3 SR1's ambiguity through the ONE
            // verdict; never the first declared, which the unqualified fallback here used to take (`cands[0]`): with
            // CNT declared under two groups `OCCURS 1 TO 9 DEPENDING ON CNT` compiled clean and ran on the first.
            string writtenDep = WrittenQualified(depName, spec.DependingQualifiers);
            string depFace = $"OCCURS … DEPENDING ON data-name-1 of '{subject}'";
            var tier = EntryClauseCandidates(item, depName, spec.DependingQualifiers, ScopeOf(item.Root));
            if (tier.Count == 0)
            {
                Edition.Error("COBOLNET0851", $"OCCURS … DEPENDING ON '{writtenDep}' on '{subject}': data-name-1 "
                    + (spec.DependingQualifiers.Count > 0
                        ? "is not defined under the given qualifiers (ISO §8.4.2.2.1: uniqueness shall be established "
                          + "through qualification)"
                        : "is not defined (ISO §13.18.38 Format 2)"));
                continue;
            }
            if (UniqueOrReportAmbiguous(tier, depFace, writtenDep, out bool _) is not { } dep) continue;
            spec.Depending = dep;

            // SR17: data-name-1 shall describe an integer (an index item is NOT an integer data item).
            if (dep.Pic is not { IsUnscaledInteger: true })
                Edition.Error("COBOLNET0852", $"OCCURS … DEPENDING ON '{depName}' on '{subject}': data-name-1 "
                    + "shall describe an integer (ISO §13.18.38.3 SR17)");

            // SR18 / SR21 (kb/Work PB1261): data-name-1 must share the table record's SCOPE and RESIDENCE attributes,
            // or the table's current extent would depend on a counter that a program sharing the record cannot see
            // (GLOBAL) or that is not shared with it (EXTERNAL — two run-unit programs would disagree about the
            // record's length). "Described in the same data division" holds by construction: this pass runs inside
            // Bind, before a contained program inherits any container's names, so data-name-1 resolved among this
            // element's own items.
            if (DependingAttributeFault(item, dep) is { } attributeFault)
                Edition.Error(DiagnosticCatalog.OccursDependingAttributeMismatch,
                    $"OCCURS … DEPENDING ON '{writtenDep}' on '{subject}': {attributeFault}");

            // SR2: data-name-1 shall not be subscripted (it cannot lie within any table). A TABLE is any OCCURS — a
            // Format-4 DYNAMIC one leaves Occurs null (kb/Work PB1260) — and "lies within a table" is the ONE arity
            // answer, DataItem.SubscriptArity (a walk written nowhere else: SubscriptAdmissionDriftTests).
            if (dep.Parent is { SubscriptArity: > 0 })
                Edition.Error("COBOLNET0853", $"OCCURS … DEPENDING ON '{depName}' on '{subject}': "
                    + "data-name-1 shall not be subscripted (ISO §13.18.38.3 SR2)");

            // SR1(b)/SR10: "complex ODO" is illegal at every edition — tables may be nested only when the
            // DEPENDING phrase is absent, and no OCCURS subject may have an occurs-depending table beneath it. The
            // nearest enclosing table (DYNAMIC included — `Occurs is not null` let an ODO table nest under OCCURS
            // DYNAMIC, kb/Work PB1260) is the last of the parent's subscript levels.
            if (item.Parent is { SubscriptArity: > 0 } enclosing)
                Edition.Error("COBOLNET0854", $"occurs-depending table '{subject}' is subordinate to the "
                    + $"OCCURS item '{enclosing.SubscriptLevels()[^1].CobolName}': tables may be nested only when the "
                    + "DEPENDING phrase is "
                    + "absent (ISO §13.18.38.3 SR1(b)/SR10)");

            // §13.18.44 SR: neither the redefined item nor a redefinition may include an occurs-depending table.
            // (The FD multi-record shared AREA is §9.1.2 record sharing — synthesized with no written REDEFINES
            // clause — and is exempt: only an explicitly-written REDEFINES anywhere in the class trips this.)
            for (DataItem? a = item; a is not null; a = a.Parent)
                if (a.RedefinesTargetName is not null
                    || (a.Class is { } cls && cls.Members.Any(mm => mm.RedefinesTargetName is not null)))
                {
                    Edition.Error("COBOLNET0855", $"occurs-depending table '{subject}' lies within a REDEFINES "
                        + "area: neither the original nor a redefinition may include an OCCURS DEPENDING ON "
                        + "table (ISO §13.18.44.3 SR5)");
                    break;
                }

            // SR22: within its record the subject may be followed only by entries subordinate to it — the
            // variable tail is the record's TRAILING storage (the GR8 character-prefix model relies on this).
            // A later sibling that itself REDEFINES an earlier one adds no storage and does not violate SR22.
            for (DataItem? n = item; n is { Parent: { } parent }; n = parent)
            {
                int idx = parent.Children.IndexOf(n);
                if (idx >= 0 && parent.Children.Skip(idx + 1).Any(s => s.RedefinesTargetName is null))
                {
                    Edition.Error("COBOLNET0856", $"occurs-depending table '{subject}' is followed by a "
                        + "non-subordinate entry in its record: the subject of an OCCURS DEPENDING ON entry may "
                        + "be followed, within that record, only by data items subordinate to it "
                        + "(ISO §13.18.38.3 SR22)");
                    break;
                }
            }

            // SR20: "The data item defined by data-name-1 shall not occupy a byte position within the range of the
            // first byte position defined by the data description entry containing the OCCURS clause and the last
            // byte position defined by the record description entry containing that OCCURS clause."
            if (DependingInsideTableRange(item, dep))
                Edition.Error("COBOLNET0857", $"OCCURS … DEPENDING ON '{depName}' on '{subject}': "
                    + "data-name-1 shall not occupy a byte position within the range from the table's first byte "
                    + "position to the last byte position of its record (ISO §13.18.38.3 SR20)"
                    + (ReferenceEquals(dep.Root, item.Root) ? ""
                        : $" — '{dep.Root.CobolName}' and '{item.Root.CobolName}' are one storage area"));
        }
    }

    /// <summary>⛔ §13.18.38.3 SR20 over the record's STORAGE AREA, not its entry (kb/Work PB1261). The range is
    /// byte positions — from the table's first to its record's last — and a byte position belongs to an AREA, which
    /// more than one record description can describe: §13.18.33.4 GR3, "Multiple level 1 entries subordinate to a FD
    /// or SD entry represent implicit redefinitions of the same area", and a level-1 REDEFINES. Such records share
    /// one <see cref="RedefinesClass"/> and each begins at the area's first byte, so a counter in a sibling record
    /// sits at its own record offset in the same area. The test compared record ROOTS and so let that counter
    /// through, though it overlays the table's bytes.
    /// <para>Within ONE record, leaf order IS byte order for the canonical storage, so the counter must lie strictly
    /// before the table. Across the records of one area the counter's byte window is compared with the table's:
    /// its offset in its own record against the table's offset and its record's extent (the table at its maximum,
    /// §8.5.1.8 — the allocation every record of the area is described against).</para></summary>
    private static bool DependingInsideTableRange(DataItem table, DataItem dep)
    {
        if (ReferenceEquals(dep.Root, table.Root))
        {
            var leaves = LeavesOf(table.Root).ToList();
            int tableStart = leaves.FindIndex(l => OdoModel.IsWithin(l, table));
            int depIdx = leaves.FindIndex(l => ReferenceEquals(l, dep));
            return tableStart >= 0 && depIdx >= tableStart;
        }
        if (dep.Root.Class is not { } area || !ReferenceEquals(table.Root.Class, area)) return false;
        if (RecordLayout.FirstOccurrenceOffsetOf(table) is not { } first || RecordLayout.OffsetOf(dep) is not { } at)
            return false;
        int recordEnd = RecordLayout.PhysicalWidth(table.Root);
        return at < recordEnd && at + RecordLayout.PhysicalOccurrenceWidth(dep) > first;
    }

    /// <summary>The §13.18.38.3 SR18 / SR21 screen of data-name-1's ATTRIBUTES against the table's record (kb/Work
    /// PB1261); null when they agree.
    /// <para>SR18 — "If the OCCURS clause is specified in an entry subordinate to one containing the GLOBAL clause,
    /// data-name-1, if specified, shall be a global name and shall reference a data item that is described in the
    /// same data division". The entry containing the OCCURS clause is never level 1 (SR1 a)), so it is subordinate to
    /// its record, and to the FD of a file-section record: §13.18.27.3 SR1 admits GLOBAL on both.
    /// <see cref="IsGlobalRecord"/> is "a global name" for data-name-1 (§8.4.6.2.2).</para>
    /// <para>SR21 — "If the OCCURS clause is specified in a data description entry included in a record description
    /// entry containing the EXTERNAL clause, data-name-1 shall reference a data item possessing the external
    /// attribute that is described in the same data division". The TABLE's side is the record entry's own clause
    /// (or the EXTERNAL type it is "subject to the same rules" as, §13.18.22.4 GR3); data-name-1's is the
    /// <see cref="HasExternalAttribute"/> attribute.</para></summary>
    private string? DependingAttributeFault(DataItem table, DataItem dep)
    {
        if (IsGlobalRecord(table.Root) && !IsGlobalRecord(dep.Root))
            return $"the table's record '{table.Root.CobolName}' is global and data-name-1 is not a global name — \"If "
                + "the OCCURS clause is specified in an entry subordinate to one containing the GLOBAL clause, "
                + "data-name-1, if specified, shall be a global name\" (ISO §13.18.38.3 SR18)";
        if ((table.Root.HasExternalClause || table.Root.ExternalFromType) && !HasExternalAttribute(dep.Root))
            return $"the table's record '{table.Root.CobolName}' is EXTERNAL and data-name-1 does not possess the "
                + "external attribute — \"data-name-1 shall reference a data item possessing the external attribute "
                + "that is described in the same data division\" (ISO §13.18.38.3 SR21)";
        return null;
    }

    /// <summary>Is <paramref name="record"/> — and so every data-name subordinate to it — a global name? ISO
    /// §8.4.6.2.2: "A constant-name, file-name, record-name, report-name, screen-name, or type-name described with a
    /// GLOBAL clause is a global name. All data-names and screen-names subordinate to a global name are global
    /// names" — the record's own GLOBAL clause, or its file description's (§13.18.27.3 SR1 b) and d)).</summary>
    private bool IsGlobalRecord(DataItem record) =>
        record.HasGlobalClause || Files.Any(f => f.IsGlobal && f.Records.Contains(record));

    /// <summary>Does <paramref name="record"/> possess the external attribute? ISO §8.6.3: a working-storage record
    /// "is given the external attribute by the presence of the EXTERNAL clause in its data description entry" (or
    /// by an EXTERNAL type, §13.18.22.4 GR3), and "If the EXTERNAL clause is included in the file description entry,
    /// its records and their subordinate data items attain the external attribute".</summary>
    private bool HasExternalAttribute(DataItem record) =>
        record.HasExternalClause || record.ExternalFromType || Files.Any(f => f.IsExternal && f.Records.Contains(record));

    /// <summary>
    /// Post-build OCCURS DYNAMIC pass (ISO §13.18.38 Format 4 / §8.5.1.9; data-model D9): for each dynamic-capacity
    /// table carrying a <c>CAPACITY IN data-name-3</c> phrase, synthesize the IMPLICITLY-defined CAPACITY register
    /// (SR30) — a VIEW over the table's current capacity, an unsigned integer (SR31) — and index it by name so the
    /// <see cref="ReferenceResolver"/> can build a <see cref="CapacityRegisterPlace"/>. The register is NOT a stored
    /// field (no <c>FieldEmitter</c> entry): its value IS the runtime <c>CobolDynTable&lt;T&gt;.Capacity</c>. A
    /// register-name that duplicates an explicit data-name (or another table's register) violates the
    /// implicit-definition rule → COBOLNET1523. Placement/declaration guards: SR28 FROM ≤ TO (1522); the FILE
    /// SECTION prohibition §8.5.1.9.1 (1526).
    /// <para>⛔ A FORMAT 1 VALUE ON OR UNDER A DYNAMIC ENTRY IS NOT THIS PASS'S BUSINESS — see the note below the
    /// SR28 guard. It carries no capacity derivation to stage, so there is nothing here to guard (kb/Work
    /// PB500).</para>
    /// </summary>
    internal void DynamicResolve()
    {
        // §8.5.1.9.1 item 3 (:8195) — the roots of every FILE SECTION record, so a dynamic table in one is rejected.
        var fileRecordRoots = new HashSet<DataItem>(Files.SelectMany(f => f.Records));

        foreach (var item in AllItems())
        {
            if (item.OccursSpec is not { IsDynamic: true } spec) continue;
            using var _ = Edition.At(item);
            string subject = item.CobolName ?? item.CsName;

            // §8.5.1.9.1 item 3 (:8195) — a dynamic-capacity table "may be defined in any place, OTHER THAN the file
            // section" (its out-of-line CobolDynTable has no place in a record image). Reject a dynamic table whose
            // storage root is an FD/SD record.
            DataItem root = item; while (root.Parent is { } p) root = p;
            if (fileRecordRoots.Contains(root))
                Edition.Error("COBOLNET1526", $"OCCURS DYNAMIC on '{subject}': a dynamic-capacity table shall not be "
                    + "defined in the FILE SECTION (ISO §8.5.1.9.1)");

            // SR28 (:19987): integer-4 (FROM) shall be nonnegative and integer-5 (TO), if both present, shall be
            // GREATER THAN integer-4. (FROM<0 cannot be written — the grammar takes an unsigned integerLiteral.)
            if (spec.InitialCap is { } from && spec.ExpectedMax is { } to && to <= from)
                Edition.Error("COBOLNET1522", $"OCCURS DYNAMIC FROM {from} TO {to} on '{subject}': the expected "
                    + $"capacity (TO integer-5) shall be greater than the minimum capacity (FROM integer-4) "
                    + "(ISO §13.18.38.3 SR28)");

            // ⛔ NO GUARD BELONGS HERE FOR A **FORMAT 1** VALUE ON OR UNDER A DYNAMIC ENTRY, AND THE RULE THAT
            //    LOOKS LIKE ONE DOES NOT REACH IT (kb/Work PB500 — this is the SECOND arm of the same two-arm
            //    dispatch the fixed-capacity lane already got right).
            //
            //    A COBOLNET1528 refusal stood here, citing §13.18.63.4 GR16 ("If an OCCURS clause with the DYNAMIC
            //    phrase is specified in the same entry as the VALUE clause, or in any entry superordinate to it,
            //    the initial capacity of the associated dynamic-capacity table is calculated according to the
            //    following subrules") and reading its subrule (b) ("If no TO phrase is specified in the VALUE
            //    clause, the initial capacity is set equal to the expected capacity specified in the OCCURS
            //    clause") as reaching a Format 1 `VALUE IS literal-1`, which trivially has no TO phrase. IT DOES
            //    NOT. GR16 sits under the **FORMAT 2** general-rule heading (GR11–GR16), and §13.18.63 states
            //    cross-band application EXPLICITLY and in ONE direction only — GR11 "General rules 1, 2, 3, 4, 5,
            //    6, 7, 8, and 10 above apply", GR17, GR21, GR24 all import FORMAT 1 rules INTO another band, and
            //    nothing imports GR12–GR16 into FORMAT 1. The syntax rules settle it independently: §13.18.63.3
            //    SR22 ("A VALUE clause without the TO phrase shall not be specified in the same entry as an OCCURS
            //    clause with a DYNAMIC phrase but no TO phrase, or in any entry subordinate to such an OCCURS
            //    clause") is the rule that keeps GR16b from having no operand, and IT TOO is in the FORMAT 2 band
            //    (SR16–SR23) with no FORMAT 1 counterpart. Were GR16 to reach Format 1, `05 A PIC X OCCURS DYNAMIC
            //    FROM 2 VALUE "Z".` would hit GR16b with no expected capacity to set — a hole the standard would
            //    have had to close and did not, because there is no hole.
            //
            //    So a Format 1 VALUE here is CONFORMING SOURCE with a fully determined meaning, and refusing it
            //    was rejecting legal COBOL (§13.18.38.3 has no rule forbidding VALUE on a Format 4 entry):
            //      • CAPACITY — §14.6.2.3.2 item 6, "For each dynamic-capacity table, except where the table is
            //        defined by an elementary entry with a VALUE clause, the capacity of the table is set to the
            //        minimum capacity specified in the corresponding OCCURS clause", with §13.18.38.4 GR16
            //        "Integer-4 is the minimum capacity of the table. If integer-4 is absent, a value of zero is
            //        assumed for it." The item-6 EXCEPTION is the carve-out that lets a GR16-derived (Format 2)
            //        capacity survive this step, and it changes NOTHING for a Format 1 VALUE either way: §8.5.1.9.1
            //        gives the same number from the other side — "The current capacity of a dynamic-capacity table
            //        may be initialized explicitly in the FROM phrase of the OCCURS clause or implicitly in the
            //        VALUE clause. If neither is specified, the current capacity is initialized to zero" — and
            //        GR16 is the only "implicitly in the VALUE clause" mechanism there is. FROM (or zero) both ways.
            //      • CONTENT — §13.18.63.4 GR9, "A VALUE clause specified in a data description entry that contains
            //        an OCCURS clause or in an entry that is subordinate to an OCCURS clause causes every occurrence
            //        of the associated data item to be assigned the specified value", reinforced by §13.18.38.4 GR1
            //        (band "FORMATS 1, 2 AND 4" — Format 4 IS the dynamic-capacity table): "Except for the OCCURS
            //        clause itself, all data description clauses associated with an item whose description includes
            //        an OCCURS clause apply to each occurrence of the item described."
            //
            //    Both are already what the emitter does with no code of its own: ValueInitializer.FieldInit opens a
            //    CobolDynTable at `OccursSpec.InitialCap ?? 0` and seeds EVERY occurrence from the one-occurrence
            //    initializer, which for a Format 1 VALUE is DataItem.ValueAt → RawValue. The refusal's two arms
            //    disagreed with each other, too, which is what gave the defect away: the GROUP arm fired only when
            //    the OCCURS carried a TO, so `OCCURS DYNAMIC FROM 3.` with subordinate VALUEs compiled and seeded
            //    correctly while `OCCURS DYNAMIC FROM 3 TO 9.` — the SAME construct, one optional phrase apart —
            //    was refused, and the ELEMENTARY arm ignored the TO entirely and refused both.

            // The register: an unsigned-integer VIEW over the table's Capacity (SR31) — a native-binary PicInfo so
            // the numeric pipeline reads {tablePath}.Capacity (a long) as a scale-0 integer; no stored field. The
            // implementor digit count is 10 (unsigned BinaryLong): the CobolDynTable implementor maximum,
            // 0x3FFF_FFFF ≈ 1.07e9, fits in 10 digits (§8.5.1.9.1 — "a number of digits sufficient to hold the
            // maximum"). Kept off ByName/Roots — reachable ONLY through CapacityRegisters (the resolver hook).
            // ⛔ MINTED FOR EVERY DYNAMIC TABLE, NAMED ONLY WHEN `CAPACITY IN data-name-3` IS WRITTEN (kb/Work
            // PB61): the view over the table's current capacity is what FUNCTION LENGTH / BYTE-LENGTH read for a
            // variable-length group (§15.50.4 r7c / §15.14.4 r6c — "based on their current capacity"), whether or
            // not the program gave the register a name. An unnamed register has no CobolName and no
            // CapacityRegisters entry, so no COBOL reference can reach it.
            // ⛔ IT CARRIES A REAL POSITION IN THE HIERARCHY (kb/Work PB457): §13.18.38.3 SR30 — "If qualifiers are
            // required for uniqueness, it shall be treated as though implicitly defined at the same level as the
            // entry containing the OCCURS clause" — so its Parent is the OCCURS entry's Parent, making it a SIBLING
            // of the table. That one assignment is what lets the ONE §8.4.2.2 qualifier matcher
            // (DataBinder.QualifierChainMatches) answer `WS-CAP OF WS-TABLE`, and what makes the register of a table
            // NESTED under another table answer SubscriptArity > 0 — the §8.4.2.3.3 SR3/SR5-vs-§13.18.38.3 SR31 conflict that
            // ReferenceResolver.CapacityRegisterFor reports, instead of the flat name-dictionary's false "not
            // defined". It is NOT added to Parent.Children: the register has no storage and takes no record slot.
            var reg = new DataItem
            {
                Level = 49,
                CsName = NamingConvention.CapacityRegisterName(item.CsName),
                CobolName = spec.CapacityName,
                Pic = PicInfo.BinaryItem(Usage.BinaryLong, signed: false),
                Parent = item.Parent,
                Uid = _uidCounter++,
            };
            spec.CapacityRegister = reg;

            if (spec.CapacityName is not { } capName) continue;

            // SR30 — data-name-3 is implicitly defined at the OCCURS entry, so it must not also be an explicit
            // data-name (a duplicate definition) nor the CAPACITY register of another dynamic table.
            // (This pass runs inside Bind, before a contained program inherits its containers' globals, so the
            // map holds only this element's own registers here — the "elsewhere in the source element" SR30 means.)
            if (ByName.ContainsKey(capName) || _capacityRegisters.ContainsKey(capName))
            {
                Edition.Error("COBOLNET1523", $"CAPACITY IN '{capName}' on '{subject}': data-name-3 is implicitly "
                    + "defined by the OCCURS DYNAMIC entry and shall not duplicate another data-name or "
                    + "CAPACITY register (ISO §13.18.38.3 SR30)");
                continue;
            }
            // "Defined elsewhere" is any user-defined word of ANOTHER type too (§8.3.2.2: a given user-defined word "may be
            // used as only one type of user-defined word"): the register IS a data-name (SR30 defines it as one), so it
            // is announced through the ONE declaration funnel (kb/Work PB1264, PB1083), which holds it to the
            // one-type-per-word rule against a file-name, an index-name, a condition-name, a section- or paragraph-name
            // declared before OR after it — the procedure-division binder declares those later and its declaration is
            // the one refused. This used to be a hand-written file-name scan beside the data-name test.
            DeclareUserWord(capName, UserWordKind.DataName);
            AddCapacityRegister(capName, item);
        }
    }

    /// <summary>The ONE insert into <see cref="CapacityRegisters"/>: this element's own register (from
    /// <see cref="DynamicResolve"/>, first) and each container's GLOBAL one (from <see cref="InheritGlobalSubtree"/>,
    /// nearest container first) — appended, so each name's list stays in nearest-declaring-element order (kb/Work
    /// PB1674).</summary>
    private void AddCapacityRegister(string name, DataItem table)
    {
        if (!_capacityRegisters.TryGetValue(name, out var tables)) _capacityRegisters[name] = tables = [];
        tables.Add(table);
    }
}
