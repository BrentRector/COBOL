// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE FORMAT 2 (table) VALUE GEOMETRY — ISO §13.18.63.3 SR18–SR23 and the §13.18.63.4 GR12–GR16 resolution,
/// in ONE place, run as the declared <c>ResolveTableValues</c> pass (see <c>BindPipeline</c>).
///
/// <para><b>Why a post-forest pass and not the entry binder.</b> Every one of these rules is written against
/// "each OCCURS clause for the subject of the entry <b>or superordinate to that entry</b>" (SR20/SR21) or against
/// an entry "<b>subordinate to</b> a data description entry that contains an OCCURS clause" (SR18) — properties of
/// the entry's ANCESTORS. <c>DataBinder.BindEntry</c> runs before <see cref="DataItem.Parent"/> is assigned
/// (<c>BindEntries</c> links the item into its parent AFTER the entry is built), so an entry-bind screen can ask
/// only about the entry itself. The screen that stood there did exactly that, and it stood in for all six rules
/// with ONE staged refusal: any shape whose subscript tuple was not exactly one long, or whose VALUE was not on
/// the OCCURS entry itself, was rejected as "not yet implemented" — including the shapes SR18 expressly PERMITS
/// (kb/Work PB505: `01 T OCCURS 3. 05 X PIC X(2) VALUE "AB" FROM (1).` is conforming source and was refused).
/// SR18 was never written down at all, SR20 sentence 1 and SR22's subordinate arm had no site, SR21's ordering
/// test was a scalar comparison that is only correct at one dimension, and SR23 could never fire.</para>
///
/// <para><b>What stays at entry bind.</b> The ALL-FORMATS LITERAL SCREEN (SR2/SR3 and, through SR16, SR10–SR15)
/// — <c>DataBinder.ScreenTableValueLiterals</c>, which routes each occurrence-literal through the SAME
/// <c>ScreenValueLiteral</c> funnel the Format-1 <see cref="DataItem.RawValue"/> takes (kb/Work PB208). It is a
/// property of the literal and the subject's own PICTURE, so it belongs exactly where the Format-1 screen is, and
/// keeping the two on one timeline is what
/// <c>ValueFormat2Tests.Format1AndFormat2_ScreenTheSameLiteralAlike</c> pins.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The §13.18.63.3 SR18–SR23 screen plus the §13.18.63.4 GR12–GR16 resolution, once per COMPOSED
    /// entry carrying a Format-2 (table) VALUE.
    ///
    /// <para>⛔ Runs over <see cref="CompositionForest"/> MINUS the TYPE DECLARATIONS, and the exclusion is the
    /// rules' own doing: every dimension count these rules test is a property of where the entry ENDS UP, and a
    /// template has no such place. `01 TT TYPEDEF. 05 X PIC X(2) VALUE "AB" FROM (1).` is an SR18 violation when
    /// read alone and CONFORMING the moment it is referenced from inside an OCCURS entry — §13.18.57.4 GR1 copies
    /// the description into the reference site, and the composed clone (which IS in this forest, once per
    /// reference) is the entry the rule is about. The same reasoning is why the SR13/SR14 group-VALUE screen
    /// reads the composition forest.</para></summary>
    internal void ResolveTableValues()
    {
        foreach (var item in CompositionForest())
        {
            if (item.TableValues is not { Count: > 0 } specs) continue;
            if (IsInsideTypeDeclaration(item)) continue;
            using var _ = Edition.At(item);
            string where = $"data item '{item.CobolName ?? "FILLER"}'";

            // ── §13.18.63.3 SR20/SR21's dimension list: "each OCCURS clause for the subject of the entry or
            //    superordinate to that entry, specified in the same order as a subscripted reference to the
            //    subject of the entry would be specified" — i.e. the OCCURS chain, MOST inclusive first.
            var dims = OccursChainOf(item);

            // ── SR18: "A data description entry that contains the VALUE clause shall contain an OCCURS clause
            //    or be subordinate to a data description entry that contains an OCCURS clause."
            if (dims.Count == 0)
            {
                Edition.Error(DiagnosticCatalog.TableValueWithoutOccurs, $"{where}: a Format 2 (table) VALUE "
                    + "clause is specified, but the entry neither contains an OCCURS clause nor is subordinate "
                    + "to a data description entry that contains one (ISO §13.18.63.3 SR18)");
                item.TableValues = null;   // nothing to key the literals to — never reaches an emitter
                continue;
            }

            foreach (var spec in specs) ScreenTableValueGeometry(item, spec, dims, where);

            // ── §13.18.63.4 GR12–GR15: the odometer fill. Built from the phrases that ARE well-formed for these
            //    dimensions (TableValueOdometer.Resolve skips the rest — a mis-shaped phrase has already been
            //    diagnosed and must not seed the wrong element, and under --permissive a demoted diagnostic
            //    still reaches emit).
            item.TableValuePlan = new TableValuePlan
            {
                Dims = dims,
                Literals = TableValueOdometer.Resolve(dims, specs),
            };
            // The emitters' fast-path guard: an OCCURS entry whose subtree has no table VALUE composes ONE
            // element initializer and repeats it, exactly as both lanes always did.
            for (DataItem? a = item; a is not null; a = a.Parent) a.ContainsTableValue = true;

            // ── §13.18.63.4 GR16: the initial capacity this clause gives each DYNAMIC dimension. "If more than
            //    one VALUE clause applies, the maximum value thus calculated becomes the initial capacity" — the
            //    accumulation lives on the OCCURS-carrying item, so two subordinate entries' VALUE clauses over
            //    the same dynamic table cannot each answer separately.
            for (int k = 0; k < dims.Count; k++)
            {
                if (!dims[k].Dynamic) continue;
                var owner = dims[k].Owner;
                int min = owner.OccursSpec?.InitialCap ?? 0;
                foreach (var spec in specs)
                {
                    if (spec.From.Count != dims.Count) continue;
                    if (TableValueOdometer.InitialCapacity(spec, k, min, dims[k].Max) is not { } cap) continue;
                    owner.TableValueInitialCapacity = Math.Max(owner.TableValueInitialCapacity ?? min, cap);
                }
            }
        }
    }

    /// <summary>The subject's dimension list — every entry from the ROOT down to the subject that carries an
    /// OCCURS clause (fixed, occurs-depending or dynamic-capacity), MOST inclusive first: exactly the order
    /// §13.18.63.3 SR20 names, "the same order as a subscripted reference to the subject of the entry would be
    /// specified". A dimension's ceiling is §13.18.63.4 GR12's "the maximum number of occurrences, or, in the
    /// case of a dynamic-capacity table, the expected number of occurrences, specified by its corresponding
    /// OCCURS clause" — <see cref="DataItem.Occurs"/> for a fixed or occurs-depending table (§8.5.1.8 allocates
    /// the maximum), the OCCURS TO capacity for a dynamic one, and null when a dynamic table declares none.</summary>
    private static List<TableValueDim> OccursChainOf(DataItem item)
    {
        var dims = new List<TableValueDim>();
        for (DataItem? n = item; n is not null; n = n.Parent)
        {
            if (!n.IsTable) continue;
            dims.Add(new TableValueDim(n, n.IsDynamicTable ? n.OccursSpec?.ExpectedMax : n.Occurs, n.IsDynamicTable));
        }
        dims.Reverse();
        return dims;
    }

    /// <summary>True when <paramref name="item"/> lies inside a TYPE DECLARATION (ISO §13.18.58) — a template
    /// that allocates no storage and has no composed OCCURS chain. See <see cref="ResolveTableValues"/>.</summary>
    private static bool IsInsideTypeDeclaration(DataItem item)
    {
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (n.IsTypedef) return true;
        return false;
    }

    /// <summary>One phrase's §13.18.63.3 SR20–SR23 screen. Each rule is stated once, and every rule that speaks
    /// about "each OCCURS clause for the subject of the entry or superordinate to that entry" is checked over the
    /// WHOLE tuple rather than over a first dimension.</summary>
    private void ScreenTableValueGeometry(
        DataItem item, TableValueSpec spec, IReadOnlyList<TableValueDim> dims, string where)
    {
        string phrase = $"{where}, Format 2 VALUE FROM ({spec.FromText})";

        // ── SR20 sentence 1 / SR21 sentence 1 — the subscript COUNT. "In one FROM phrase, there shall be one
        //    subscript-1 specified for each OCCURS clause for the subject of the entry or superordinate to that
        //    entry" (and, for a TO phrase, "there shall be one subscript-2 specified for each OCCURS clause …").
        //    A wrong count leaves every following test without a correspondence, so the phrase stops here.
        if (spec.From.Count != dims.Count)
        {
            Edition.Error(DiagnosticCatalog.TableValueSubscriptCount, $"{phrase}: the FROM phrase specifies "
                + $"{spec.From.Count} subscript(s) but the subject of the entry has {dims.Count} dimension(s) "
                + $"({DimensionList(dims)}) — there shall be one subscript-1 for each OCCURS clause for the "
                + "subject of the entry or superordinate to that entry, in subscripted-reference order "
                + "(ISO §13.18.63.3 SR20)");
            return;
        }
        if (spec.To is { } toList && toList.Count != dims.Count)
        {
            Edition.Error(DiagnosticCatalog.TableValueSubscriptCount, $"{phrase}: the TO phrase specifies "
                + $"{toList.Count} subscript(s) but the subject of the entry has {dims.Count} dimension(s) "
                + $"({DimensionList(dims)}) — there shall be one subscript-2 for each OCCURS clause for the "
                + "subject of the entry or superordinate to that entry, in subscripted-reference order "
                + "(ISO §13.18.63.3 SR21)");
            return;
        }

        // ── SR20 sentence 2 — "Each instance of subscript-1 shall not exceed the maximum number of occurrences
        //    specified in the OCCURS clause associated with that instance." (An occurrence number below 1
        //    identifies no table element at all, so the same code answers for it.)
        for (int k = 0; k < dims.Count; k++)
        {
            if (spec.From[k] >= 1 && spec.From[k] <= dims[k].Ceiling) continue;
            Edition.Error("COBOLNET1586", $"{phrase}: a Format 2 VALUE FROM subscript ({spec.FromWritten[k]}) is out of "
                + $"range 1..{dims[k].Ceiling} for {DimensionName(dims, k)} "
                + $"(ISO §13.18.63.3 SR20{CeilingProvenance(dims[k])})");
        }

        if (spec.To is { } to)
        {
            // ── SR21 sentence 2 — the same ceiling test for subscript-2.
            for (int k = 0; k < dims.Count; k++)
            {
                if (to[k] >= 1 && to[k] <= dims[k].Ceiling) continue;
                Edition.Error("COBOLNET1587", $"{phrase}: a Format 2 VALUE TO subscript ({spec.ToWritten![k]}) is out of range "
                    + $"1..{dims[k].Ceiling} for {DimensionName(dims, k)} "
                    + $"(ISO §13.18.63.3 SR21{CeilingProvenance(dims[k])})");
            }

            // ── SR21 sentence 3 — "The specification of subscript-2 in one TO phrase shall be such that the
            //    table element associated with subscript-2 is the same occurrence or a successive occurrence of
            //    the table element associated with the corresponding subscript-1." §13.18.63.4 GR12's fill order
            //    increments the LEAST inclusive subscript and carries into the next most inclusive one, so
            //    "successive" is the LEXICOGRAPHIC order of the whole tuple — a per-dimension comparison would
            //    reject `FROM (1 3) TO (2 1)`, which is two successive elements of a 2×3 table.
            if (Subscripts.Compare(new Subscripts([.. to]), new Subscripts([.. spec.From])) < 0)
                Edition.Error("COBOLNET1587", $"{phrase}: the TO subscripts ({spec.ToText}) identify a "
                    + $"table element PRECEDING the one the FROM subscripts ({spec.FromText}) "
                    + "identify — subscript-2 shall be the same occurrence or a successive occurrence of the "
                    + "table element associated with the corresponding subscript-1 (ISO §13.18.63.3 SR21)");

            // ── SR23 — "If the TO phrase is specified and an OCCURS clause with a DYNAMIC phrase but no TO
            //    phrase is specified in the same entry or in any superordinate entry, the values of subscript-1
            //    and subscript-2 corresponding to all levels higher than that of the OCCURS clause, if
            //    applicable, shall be equal". "Higher" is the COBOL level sense — the MORE inclusive dimensions,
            //    the ones to the LEFT of the unbounded one: the odometer may not carry out of a dimension with
            //    no ceiling. The rule binds EVERY such OCCURS clause, so a level is constrained when ANY less
            //    inclusive dimension is unbounded — TableValueOdometer.UnboundedBelow, the one statement of the
            //    rule's shape, which the fill's ElementCount reads too (kb/Work PB1716: the screen used to test
            //    only the OUTERMOST unbounded dimension, so two nested ones let `FROM (1 1) TO (2 1)` through
            //    to a fill that could never carry out of the inner one).
            for (int k = 0; k < dims.Count; k++)
            {
                if (spec.From[k] == to[k] || TableValueOdometer.UnboundedBelow(dims, k) is not { } d) continue;
                Edition.Error(DiagnosticCatalog.TableValueDynamicSpanLevels, $"{phrase}: subscript-1 "
                    + $"({spec.FromWritten[k]}) and subscript-2 ({spec.ToWritten![k]}) differ for {DimensionName(dims, k)}, "
                    + $"which is more inclusive than {DimensionName(dims, d)} — an OCCURS DYNAMIC clause "
                    + "with no TO phrase; the subscripts corresponding to all levels higher than that "
                    + "OCCURS clause shall be equal (ISO §13.18.63.3 SR23)");
            }
        }
        // ── SR22 — "A VALUE clause without the TO phrase shall not be specified in the same entry as an OCCURS
        //    clause with a DYNAMIC phrase but no TO phrase, OR IN ANY ENTRY SUBORDINATE TO SUCH AN OCCURS
        //    CLAUSE." Both arms, because every dimension in the list is the subject's own OCCURS or one
        //    superordinate to it — the subordinate arm was the missing half.
        else if (dims.FirstOrDefault(d => d.DynamicWithoutTo) is { } unbounded)
            Edition.Error("COBOLNET1588", $"{phrase}: a Format 2 VALUE with no TO phrase is not permitted "
                + $"{(ReferenceEquals(unbounded.Owner, item) ? "in the same entry as" : "in an entry subordinate to")}"
                + $" the OCCURS DYNAMIC clause on '{unbounded.Owner.CobolName ?? "FILLER"}', which specifies no TO "
                + "(expected) capacity (ISO §13.18.63.3 SR22)");
    }

    /// <summary>Says so IN THE MESSAGE when the ceiling a subscript was measured against is this implementation's
    /// rather than the program's: a number the source never wrote must never look like one it did.</summary>
    private static string CeilingProvenance(TableValueDim dim) => dim.Max is null
        ? "; the ceiling is this implementation's §8.5.1.9.1 maximum capacity — the OCCURS DYNAMIC clause "
          + "specifies no TO (expected) capacity, so the standard specifies no maximum for this dimension"
        : "";

    private static string DimensionName(IReadOnlyList<TableValueDim> dims, int k) =>
        dims.Count == 1
            ? $"the OCCURS clause on '{dims[k].Owner.CobolName ?? "FILLER"}'"
            : $"dimension {k + 1} (the OCCURS clause on '{dims[k].Owner.CobolName ?? "FILLER"}')";

    private static string DimensionList(IReadOnlyList<TableValueDim> dims) =>
        string.Join(", ", dims.Select(d => d.Owner.CobolName ?? "FILLER"));
}
