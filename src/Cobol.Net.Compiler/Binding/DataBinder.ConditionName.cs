// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;

using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// THE §13.16.3 SR24 CONDITION-NAME ASSOCIATION SCREEN (kb/Work PB488) — which entry a level-88 condition-name
/// entry may be associated with, and whether it follows one at all. Partial-class extension over
/// <c>DataBinder</c>, run as the declared <c>CheckConditionNameAssociations</c> pass (see <c>BindPipeline</c>).
///
/// <para><b>Why this exists.</b> <c>DataBinder.BindCondition</c> was a VALUE decoder: it captured the level-88
/// entry's literals and hung the <see cref="Condition88"/> off whatever <see cref="DataItem"/> the caller handed
/// it, and the caller handed it <c>stack.Peek()</c> unconditionally. SR24's association sentence and ALL EIGHT of
/// its lettered exclusions had no site anywhere in the compiler. Measured on 1840778e, one probe per leg, every
/// one of these compiled and all but two of them then ANSWERED:</para>
/// <list type="bullet">
///   <item>b) <c>66 R1 RENAMES A THRU B.</c> followed by <c>88 COND-R VALUE "CD".</c> — a level-66 entry is never
///     pushed on the level stack, so <c>stack.Peek()</c> walked back past it and COND-R silently tested the
///     PRECEDING <c>05 B</c>. The probe printed BOUND-TO-B: a condition-name written over a renamed span that
///     answers about a different item is a WRONG ANSWER, not a permissiveness.</item>
///   <item>the association sentence — <c>88 ORPHAN VALUE "AB".</c> as the first entry of WORKING-STORAGE was
///     dropped without a word (<c>if (stack.Count > 0)</c>) and surfaced as a RUN-TIME
///     <c>NotImplementedCobolFeatureException</c> on <c>IF ORPHAN</c>.</item>
///   <item>c) an alphanumeric group containing <c>05 X PIC 9(4) USAGE BINARY</c>, d) a group containing a
///     JUSTIFIED item, f) a LINKAGE <c>01 AL PIC X ANY LENGTH.</c>, g) a <c>TYPEDEF STRONG</c> template —
///     each took a group 88 and evaluated it.</item>
///   <item>e) <c>01 IX USAGE INDEX. 88 C VALUE 1.</c> compiled and the condition took the TRUE branch after
///     <c>SET IX TO 1</c> — the compiler had IMPLEMENTED a construct the standard forbids. The pointer spelling
///     <c>01 P USAGE POINTER. 88 C VALUE NULL.</c> emitted <c>if ((P == NULLL))</c> into the generated C# and
///     handed the user <c>error CS0103: The name 'NULLL' does not exist in the current context</c>.</item>
///   <item>h) a group with a dynamic-length member took an 88 and crashed at run time in the group image.</item>
/// </list>
///
/// <para><b>⛔ THE EIGHT EXCLUSIONS ARE A SET OF ITEM SHAPES, SO THEY ARE ONE TABLE AND NOT EIGHT SCATTERED
/// TESTS</b> (CLAUDE.md rule 5 — prefer the shape that makes the NEXT case automatic). <see cref="Sr24Exclusions"/>
/// carries one row per lettered exclusion, in the standard's own order, each row holding the rule's own words;
/// <see cref="ConditionalVariableExclusion"/> returns the FIRST row that applies and the diagnostic quotes it. A
/// ninth exclusion is a row. <c>ConditionNameAssociationDriftTests</c> asserts the table is exactly the letters
/// a–h and exercises every predicate, so "automatic" stays true and no row becomes a lookup nothing reads.</para>
///
/// <para><b>⛔ WHY IT IS A POST-BIND PASS AND NOT A TEST INSIDE <c>BindCondition</c>.</b> SR24 asks about the
/// conditional variable's SUBORDINATES in four of its eight exclusions (c, d, g, h) — and a condition-name entry
/// "shall immediately follow the entry describing the item", which for a GROUP means it is written BEFORE that
/// group's subordinate entries. At <c>BindCondition</c> time <c>parent.Children</c> is still empty, so a screen
/// there would answer "not a group, no excluded member" for exactly the shapes the rule is about. The
/// association is RECORDED at bind time (where source order is the fact) and ADJUDICATED once the forest is
/// complete. The recorded list is also what carries the two shapes <see cref="ConformanceForest"/> cannot: a
/// level-66 alias, which lives on <see cref="DataItem.Renames66"/> and not in the tree, and an association with
/// NO item at all.</para>
///
/// <para><b>Once per WRITTEN entry.</b> Only <c>BindEntries</c> records, and a TYPE clone's condition-names are
/// copied by <c>CloneItem</c> rather than re-bound, so a <c>TYPEDEF</c> template's 88 is adjudicated once at the
/// template — the entry the programmer must change — however many <c>TYPE</c> reference sites exist. That is the
/// same once-per-source contract <c>CheckUsageDeclarations</c> gets from <see cref="ConformanceForest"/>.</para>
///
/// <para><b>Edition axis: UNGATED, and that direction is the safe one.</b> COBOL-85's condition-name rule already
/// excluded another 88, a level-66 item, "a group containing items with descriptions including JUSTIFIED,
/// SYNCHRONIZED or USAGE (other than USAGE IS DISPLAY)" and an index data item — the 2023 list splits c) from d),
/// narrows c) to ALPHANUMERIC groups (national and bit groups did not exist in 1985) and widens e) to classes
/// that did not exist in 1985 either. Every exclusion this table can fire on below 2023 is one the 1985 rule
/// stated too, and the shapes the later letters name (ANY LENGTH, STRONG, dynamic-length) are themselves gated
/// by their own introduction gates, so they cannot be written below the edition that introduced them. There is
/// nothing to gate.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>One WRITTEN level-88 entry and the entry it immediately follows (ISO §13.16.3 SR24's own
    /// subject), captured at bind time because SOURCE ORDER is a fact only <c>BindEntries</c> has.
    /// <paramref name="Variable"/> null = the 88 follows no entry describing a data item.</summary>
    internal readonly record struct ConditionAssociation(string Name, DataItem? Variable, DiagnosticCursor At);

    private readonly List<ConditionAssociation> _conditionAssociations = [];

    /// <summary>Record one condition-name → conditional-variable association for
    /// <see cref="CheckConditionNameAssociations"/>. Called from <c>BindEntries</c>' level-88 arm for EVERY
    /// written 88, including the ones it cannot bind.</summary>
    private void RecordConditionAssociation(string name, DataItem? variable) =>
        _conditionAssociations.Add(new ConditionAssociation(name, variable, Edition.Cursor));

    /// <summary>ISO §13.16.3 SR24, both halves: every level-88 entry follows an entry describing a data item,
    /// and that item is not one of the eight shapes the rule excludes.</summary>
    internal void CheckConditionNameAssociations()
    {
        foreach (var (name, variable, at) in _conditionAssociations)
        {
            using var _ = Edition.At(at);
            if (variable is null)
            {
                Edition.Error(DiagnosticCatalog.ConditionNameNoConditionalVariable, $"condition-name '{name}': "
                    + "this level-88 entry does not immediately follow a data description entry describing a "
                    + "data item — the condition-name entries for a particular conditional variable shall "
                    + "immediately follow the entry describing the item with which the condition-name is "
                    + "associated (ISO §13.16.3 SR24)");
                continue;
            }
            if (ConditionalVariableExclusion(variable) is not { } excluded) continue;
            Edition.Error(DiagnosticCatalog.ConditionNameVariableExcluded, $"condition-name '{name}': the entry "
                + $"it follows describes '{variable.CobolName ?? "FILLER"}', and a condition-name may not be "
                + $"associated with {excluded.Article} — ISO §13.16.3 SR24 excludes "
                + $"\"{excluded.Letter}) {excluded.Text}\"{excluded.Also}");
        }
    }

    /// <summary>ISO §13.16.3 SR24 read as an EXCLUSION test, in the <c>ReferenceResolver.RefModExclusion</c>
    /// shape: the lettered exclusion that bars <paramref name="candidate"/> from being a conditional variable,
    /// or null when it may be one. The FIRST matching row wins, and the rows are in the standard's own order, so
    /// the letter in the diagnostic is the one the programmer looks up.</summary>
    internal static Sr24Exclusion? ConditionalVariableExclusion(DataItem candidate)
    {
        foreach (var row in Sr24Exclusions)
            if (row.Applies(candidate)) return row;
        return null;
    }

    /// <summary>One lettered exclusion of ISO §13.16.3 SR24. <paramref name="Text"/> is the standard's own
    /// sentence; <paramref name="Article"/> is the same shape phrased for a message ("a level 66 entry");
    /// <paramref name="Also"/> carries a second rule stating the SAME prohibition, empty for most rows.</summary>
    internal readonly record struct Sr24Exclusion(char Letter, string Text, string Article,
        Func<DataItem, bool> Applies, string Also = "");

    /// <summary>⛔ THE EIGHT EXCLUSIONS OF ISO §13.16.3 SR24, as a table — the ONE place the rule is written
    /// down. Each predicate is a static lambda held in this static array, so the walk allocates nothing.</summary>
    internal static readonly IReadOnlyList<Sr24Exclusion> Sr24Exclusions =
    [
        // a) Another level 88 entry.
        // ⛔ HELD BY CONSTRUCTION, and the row is here because the rule is. `BindEntries` leaves its
        // `lastDescribed` cursor alone on a level-88 entry, so a run of consecutive 88s all associate with the
        // one entry they follow — which is the rule's INTENT (SR24's second sentence speaks of "the
        // condition-name entries", plural, "for a particular conditional variable"). No DataItem in the forest
        // carries Level 88, so this predicate is unreachable from BindEntries today; the drift test constructs
        // the shape directly and asserts it names a), because a row nothing can contradict is a row nobody has
        // verified (and because a future model that DID give an 88 a DataItem must not silently start accepting
        // `88 A VALUE 1. 88 B VALUE 2.` as an association of B with A).
        new('a', "Another level 88 entry.", "another level-88 entry", static d => d.Level == 88),

        // b) A level 66 entry.
        // The measured wrong answer: a 66 RENAMES alias is attached to its owning record's Renames66 list and
        // never pushed on the level stack, so the old `stack.Peek()` reader walked BACK PAST it.
        new('b', "A level 66 entry.", "a level-66 RENAMES entry", static d => d.Level == 66),

        // c) An alphanumeric group containing items with a usage other than display.
        // ⛔ INHERITED CITATION, RE-DERIVED (kb/Work PB932). This cited §8.5.1.3, which is the LEVELS clause and
        // says nothing about group categories. "Alphanumeric group item" is defined in §3.11 — "group item
        // except for a bit group item, a national group item, a strongly-typed group item, or a variable-length
        // group item" — and made one by §13.18.29.4 GR3. ⚠ THE PREDICATE BELOW IS THE TWO-EXCLUSION READING the
        // wrong citation carried: it excludes the bit and national groups and not the STRONGLY-TYPED or
        // VARIABLE-LENGTH ones, so it over-includes. Registered as a lead rather than changed here — in this
        // model, IsGroup with GroupUsage.None. The membership test reaches EVERY depth: the rule says
        // "containing", not "immediately containing", and a COMP item two levels down denies the group a
        // character image exactly as one level down does.
        new('c', "An alphanumeric group containing items with a usage other than display.",
            "an alphanumeric group containing an item whose usage is not DISPLAY",
            static d => d.IsGroup && d.GroupUsage is GroupUsage.None
                && AnySubordinate(d, static x => UsageOtherThanDisplay(x))),

        // d) A group containing items described with a JUSTIFIED or SYNCHRONIZED clause.
        // ANY group, not only an alphanumeric one — the letter says "A group" where c) says "An alphanumeric
        // group", and the difference is the drafting.
        // ⛔ THE SUBJECT'S OWN SYNCHRONIZED CLAUSE COUNTS. §13.18.55.3 SR1 permits SYNCHRONIZED on a group
        // ("The SYNCHRONIZED clause may be specified for group and elementary items") and §13.18.55.4 GR1 makes
        // that a description of its members: "When this clause is specified for a group item, it is treated as
        // though it had instead been separately specified for each of the subordinate elementary items for which
        // this clause is permitted." The binder records the clause on the entry that wrote it and does not
        // propagate it, so reading only the subordinates' flags would miss `01 G SYNCHRONIZED.` entirely.
        // JUSTIFIED has no such arm — §13.18.31.3 SR1 confines it to the elementary item level.
        new('d', "A group containing items described with a JUSTIFIED or SYNCHRONIZED clause.",
            "a group containing an item described with a JUSTIFIED or SYNCHRONIZED clause",
            static d => d.IsGroup
                && (d.Synchronized || AnySubordinate(d, static x => x.Justified || x.Synchronized))),

        // e) A data item of the class index, message-tag, object, or pointer.
        new('e', "A data item of the class index, message-tag, object, or pointer.",
            "a data item of the class index, message-tag, object, or pointer",
            static d => ItemCategory.IsIndexMessageTagObjectOrPointer(d),
            Also: "; the same prohibition is ISO §13.18.60.3 SR11, \"An elementary data item of class index, "
                + "message-tag, object, or pointer shall not be a conditional variable\""),

        // f) A data item described with the ANY LENGTH clause.
        new('f', "A data item described with the ANY LENGTH clause.",
            "a data item described with the ANY LENGTH clause", static d => d.IsAnyLength),

        // g) A type declaration described with the STRONG phrase, or a group item subordinate to such a type
        // declaration.
        // ⛔ THE DECLARATION SIDE, not the post-expansion StrongTypeModel.StrongRoot — the rule's subject is
        // "a type declaration", i.e. the TYPEDEF STRONG template entry the programmer wrote, and a TYPE
        // REFERENCE to it is governed by a different rule (§13.18.57.3 SR2 bars an 88 immediately after any
        // TYPE-clause entry, COBOLNET1537). The parent walk is UnderStrongTypeDeclaration, shared with
        // §13.18.60.3 SR14, whose second arm is the same words. The second arm here is confined to GROUP items
        // because the letter says "a group item subordinate to such a type declaration": an ELEMENTARY member
        // of a strong template may carry a condition-name.
        new('g', "A type declaration described with the STRONG phrase, or a group item subordinate to such a "
                + "type declaration.",
            "a STRONG type declaration or a group item subordinate to one",
            static d => d.DeclaresStrongType || (d.IsGroup && UnderStrongTypeDeclaration(d))),

        // h) A variable-length group.
        // §8.5.1.12.1: "A variable-length group is a group item whose data description has at least one
        // dynamic-length elementary item or dynamic-capacity table as a subordinate item." ⛔ NOT an
        // occurs-depending-on group — the standard names the two shapes SEPARATELY wherever it means both
        // (§13.18.12.3 SR2: "Identifier-1 shall reference an alphanumeric data item that shall not be an
        // occurs-depending-on group item, a variable-length group, or a dynamic-length elementary item" —
        // three things, listed side by side), and reading ODO into this letter would reject legal COBOL-85.
        // ReferenceResolver.HasVariableLengthSubordinate IS §8.5.1.12.1's definition; this is the delegation.
        new('h', "A variable-length group.", "a variable-length group",
            static d => d.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(d)),
    ];

    /// <summary>"an item with a usage other than display", for SR24 c). A BIT or NATIONAL group counts: it is
    /// "treated as an elementary item of usage bit / national described with PICTURE 1(m) / N(m)"
    /// (§13.18.29.4 GR1/GR2), which is a usage other than display however deep it sits.</summary>
    private static bool UsageOtherThanDisplay(DataItem d) =>
        d.GroupUsage is not GroupUsage.None
        || (d.Children.Count == 0 && (d.Pic?.Usage ?? d.OwnUsage) is { } u && u is not Usage.Display);

    /// <summary>Does any item subordinate to <paramref name="group"/>, at any depth, satisfy
    /// <paramref name="predicate"/>? Recursion depth is the record's LEVEL depth, which ISO §13.18.33.3 caps at
    /// 49 — <c>LevelNumberPass</c> owns that rule and has already enforced it by the time this pass runs, so the
    /// walk cannot run deep and this is a pointer, not a second copy. It allocates nothing (the predicates are
    /// cached static lambdas) and runs once per WRITTEN level-88 entry, of which a program has a handful.</summary>
    private static bool AnySubordinate(DataItem group, Func<DataItem, bool> predicate)
    {
        foreach (var child in group.Children)
            if (predicate(child) || AnySubordinate(child, predicate)) return true;
        return false;
    }
}
