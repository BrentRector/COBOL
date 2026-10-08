// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// THE group-level VALUE declaration screen (ISO §13.18.63.3 SR13/SR14; kb/Work PB184) — the syntax rules that
/// restrict what may sit UNDER an entry carrying a group-level VALUE clause. Partial-class extension over
/// <c>DataBinder</c>, run as the declared <c>CheckGroupValueDeclarations</c> pass (see <c>BindPipeline</c>).
///
/// <para><b>Why this exists as its own pass.</b> Three rules, all with the same subject, and NONE of them was
/// written down anywhere in the compiler — the inverse of the usual [[one_rule_one_place]] finding, where a rule
/// is written twice. Measured on 8ca74a3d, every one of these compiled clean and ran:</para>
/// <list type="bullet">
///   <item><c>01 GV VALUE "40537". 05 GB PIC 9 COMP-5 OCCURS 5.</c> — every occurrence initialized ZERO (the
///     group VALUE dropped on the floor). This is kb/Work PB184's measured symptom.</item>
///   <item><c>01 GJ VALUE "ABCD". 05 J1 PIC X(2) JUSTIFIED RIGHT.</c> — accepted silently.</item>
///   <item><c>01 GV VALUE "ABCD". 05 B PIC X(2) VALUE "ZZ".</c> — accepted silently, B's own VALUE discarded.</item>
/// </list>
///
/// <para><b>⚠ PB184's premise, corrected.</b> The note registered PB184 as a §13.18.63.4 GR5 DISTRIBUTION gap —
/// GR5 initializes "the group area … without consideration for the individual elementary or group items
/// contained within this group", so (the reasoning went) a COMP-5 leaf should take its slice of the literal's
/// BYTES and the <c>GroupValueSlicer</c> screen that excludes byte-form leaves was predicate drift left over
/// from the Tier-C boundary. SR14 refutes that premise: an alphanumeric group item is a group for which no
/// GROUP-USAGE clause is specified or implied, that is not strongly typed and is not a variable-length group
/// (§13.18.29.4 GR3; §8.5.2.1 — "an alphanumeric group item has class and category alphanumeric"), and SR14
/// requires EVERY data item subordinate to such a group carrying a VALUE to be usage
/// DISPLAY. The measured program is therefore NOT CONFORMING; there is no conforming area for the byte-form
/// distribution to exist over. GR5 is already implemented, correctly, for the whole population SR14 admits —
/// where every subordinate is usage DISPLAY, so the character image IS the byte image and a positional
/// character slice IS the area deposit. What was missing was the DIAGNOSTIC on the complement.</para>
///
/// <para>A syntax rule is a "shall", so the only conforming response to a violation is a compile-time
/// diagnostic (the same posture as the §13.18.63.3 SR2/SR3 numeric-VALUE screen, COBOLNET1625) — never a
/// silent member-wise substitute, and it is NOT dialect-gated: no edition of the standard permitted this.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The §13.18.63.3 SR1/SR13/SR14 group-level VALUE restrictions — ONE verdict per offending
    /// entry (the entry the programmer must change), anchored at that entry.
    ///
    /// <para>⛔ Runs over <see cref="CompositionForest"/>, NOT <see cref="ConformanceForest"/>. These rules are
    /// properties of the COMPOSED entry, not of the written clause list: `01 R TYPE T VALUE "ABCD".` composes an
    /// SR14 violation out of a VALUE the reference site wrote and a usage the TEMPLATE wrote, and neither entry
    /// carries it alone. The first landing of this screen took the written-entry forest, whose final filter drops
    /// every item with a non-null <c>TypeAnchor</c> — and <c>ExpandType</c> sets <c>TypeName</c> on the SUBJECT of
    /// the TYPE clause — so the reference site AND its whole clone subtree fell out. Measured: that program
    /// compiled clean (A = 0000, B = spaces) while its byte-identical inline spelling was rejected. §13.18.57.4
    /// GR3 makes the reference-site VALUE meaningful ("the content of the literal associated with that VALUE
    /// clause is used for the initial value associated with the subject of the entry"), and its NOTE points at
    /// the VALUE clause's own syntax rules for exactly this composition.</para>
    ///
    /// <para><b>Once per WRITTEN VALUE clause.</b> The subject test reads <see cref="DataItem.ValueCopiedFrom"/>:
    /// a template's VALUE assumed by a reference site (§13.18.57.4 GR1) or a SAME AS subject (§13.18.49 GR1)
    /// names the SAME source entry the template already answered for. Measured: `01 A VALUE "ABCD".
    /// 05 X PIC 9(4) COMP. 01 B SAME AS A.` reported COBOLNET1702 twice, both anchored at X's one declaration.
    /// (A SAME AS entry can never carry its OWN VALUE — §13.16.3 SR12, enforced as COBOLNET1555 — so the SAME AS
    /// population is covered entirely by its target.) That holds for SR13 and SR14 — properties of the entry that
    /// wrote the clause — and NOT for SR1, a property of what the subject IS, which a TYPE reference can compose
    /// out of a template that conforms alone (<see cref="ScreenAssumedGroupValueSubject"/>, kb/Work PB2517).</para>
    ///
    /// <para><b>SR1, both SR14 conjuncts, and SR13's second sentence.</b> SR14 is a single sentence carrying TWO
    /// independent restrictions and this repo's most reproducible defect shape is a two-arm rule with one arm
    /// implemented ([[two_arm_dispatch]], 9 instances); SR13's second sentence restricts the same subject's
    /// subtree, and SR1 restricts what the subject may BE. All four live here rather than in four places. SR16
    /// makes SR13/SR14 apply to the format 2 (table) VALUE as well, so the subject test reads BOTH VALUE
    /// carriers.</para></summary>
    internal void CheckGroupValueDeclarations()
    {
        foreach (var item in CompositionForest())
        {
            // The SUBJECT: a GROUP entry that WROTE a VALUE clause — format 1 (RawValue) or, per SR16,
            // format 2 (TableValues). A level-88 entry is a condition-name, not a node in the forest
            // (DataBinder's level stack attaches it to its conditional variable), so every descendant
            // reached below is a real data item — SR13/SR14's "data items subordinate to" exactly.
            if (!HasGroupValue(item)) continue;
            string subject = item.CobolName ?? item.CsName;

            // ⛔ A VALUE THE ENTRY ONLY ASSUMED (§13.18.57.4 GR1 / §13.18.49 GR1) is screened where it was
            // WRITTEN for every predicate its source could already answer — but SR1 is not such a predicate when
            // the composition is what creates its shape (kb/Work PB2517). See ScreenAssumedGroupValueSubject.
            if (item.ValueCopiedFrom is { } assumedFrom)
            {
                ScreenAssumedGroupValueSubject(item, assumedFrom, subject);
                continue;
            }

            // SR1 — the subject shall not be a strongly-typed group item or a variable-length group. Reported
            // at the subject and the subtree walk is SKIPPED: the entry itself has to change, and SR14's usage
            // conjunct does not govern either shape (§13.18.29.4 GR3 excludes both from "alphanumeric group
            // item"), so continuing would name a rule that does not apply.
            if (Sr1SubjectShape(item) is { } shape)
            {
                using var _ = Edition.At(item);
                Edition.Error(DiagnosticCatalog.GroupValueSubjectShape, $"data item '{subject}' specifies a "
                    + $"group-level VALUE clause but is {shape} — the subject of the entry shall not be a "
                    + "strongly-typed group item or a variable-length group (ISO §13.18.63.3 SR1)");
                continue;
            }

            // SR13 sentence 2 already rejects a VALUE at subordinate levels within a VALUE-carrying group, so a
            // nested subject re-walks ground its ancestor covered: `01 A VALUE "XX". 05 B VALUE "YY".
            // 10 C PIC 9 COMP.` reported C's usage violation once per VALUE-carrying ancestor. One offender,
            // one verdict — the outer subject's walk reaches the whole subtree.
            if (AncestorIsGroupValueSubject(item)) continue;

            // ⛔ NO BIT-PACKED REFUSAL HERE ANY MORE (kb/Work PB207, landed). A group with a USAGE BIT
            // descendant used to be rejected by name, COBOLNET0899, on the ground that §13.18.63.4 GR5 had no
            // area to deposit into. It has one, in the OTHER UNIT: §13.18.29.4 GR1b makes a bit group an as-if
            // `PICTURE 1(m)` item, so its area is m BOOLEAN POSITIONS from the §8.5.1.6.3 walk, and
            // GroupValueSlicer.AreaOf now composes it for both initializer lanes.
            // ⚠ AND THE REFUSAL SAT IN FRONT OF THE RULE THAT ACTUALLY GOVERNED THE OTHER HALF OF ITS
            // POPULATION: `01 G VALUE "AB". 05 B1 PIC 1(8) USAGE BIT.` is an ALPHANUMERIC group item with a
            // subordinate that is not usage DISPLAY — an SR14 violation, reported below — and it was told
            // instead that the compiler had not implemented something. Non-conforming source named as a
            // compiler gap is the worse of the two errors; the SR14 arm now sees it.

            // ── §13.18.63.3 SR13 sentence 1 + SR4/SR5/SR10 (their group sentences) — the LITERAL, screened
            //    against the SUBJECT'S OWN category and size (kb/Work PB206). A group HAS both: §8.5.2.1 gives
            //    it class and category, §13.18.29.4 GR1b/GR2b give a bit / national group its as-if PICTURE, and
            //    SR13 sentence 1 says "literal-1 shall be of the same category as the group item or shall be a
            //    figurative constant that is permitted in a MOVE statement to a receiving item of that
            //    category". That is the SAME question ValidateValueCategory already answers for every elementary
            //    subject, so the group asks IT rather than carrying a second copy of the answer.
            //    ⛔ BOTH VALUE CARRIERS. SR16 — "Syntax rules 10, 11,12,13,14,and 15 above apply" — extends
            //    SR13 to the format 2 (table) VALUE, and this arm read only the format-1 one. That was harmless
            //    while a group entry's table VALUE was DISCARDED; it stopped being harmless the moment
            //    §13.18.63.4 GR5's area deposit reached it (kb/Work PB505), because an unscreened literal then
            //    reaches storage. MEASURED before this arm: `05 GT OCCURS 2 VALUE "ABCDEFG" FROM (1) TO (2).`
            //    over two PIC X(2) members deposited a SILENTLY TRUNCATED "ABCD" where SR4's group sentence
            //    ("Alphanumeric literals in the VALUE clause of an alphanumeric group item shall not exceed the
            //    size of the group item") rejects it, and `VALUE 42 FROM (1) TO (2)` deposited SPACES where
            //    SR13 sentence 1 requires literal-1 to be of the group's own category — both diagnosed on the
            //    format-1 spelling of the identical entry. The ELEMENTARY subject's occurrence-literals ride
            //    DataBinder.ScreenTableValueLiterals; a GROUP subject's ride HERE, through the same
            //    ValidateValueCategory, because the SUBJECT is the group and its size is the group's.
            if (item.RawValue is not null || item.TableValues is not null)
            {
                using var _ = Edition.At(item);
                var subjectPic = GroupSubjectPic(item);
                var groupSubject = ValueSubject.ForGroup(subjectPic.Length);
                string groupWhere = $"data item '{subject}' ({GroupCategoryWord(subjectPic.Category)} group item, "
                    + "ISO §13.18.63.3 SR13)";
                if (item.RawValue is { } literal)
                    item.RawValue = ValidateValueCategory(subjectPic, literal, groupWhere, groupSubject);
                if (item.TableValues is { Count: > 0 } tableSpecs)
                {
                    var screened = new List<TableValueSpec>(tableSpecs.Count);
                    bool rewritten = false;
                    foreach (var spec in tableSpecs)
                    {
                        var lits = new List<string>(spec.Literals.Count);
                        foreach (string lit in spec.Literals)
                        {
                            string kept = ValidateValueCategory(subjectPic, lit,
                                $"data item '{subject}' ({GroupCategoryWord(subjectPic.Category)} group item), "
                                + $"Format 2 VALUE FROM ({spec.FromText}) "
                                + "(ISO §13.18.63.3 SR13 through SR16)", groupSubject);
                            rewritten |= !string.Equals(kept, lit, StringComparison.Ordinal);
                            lits.Add(kept);
                        }
                        screened.Add(spec with { Literals = lits });
                    }
                    if (rewritten) item.TableValues = screened;
                }
            }

            // SR14's usage conjunct is scoped to an ALPHANUMERIC group item — a group for which no GROUP-USAGE
            // clause is specified or implied, that is not strongly typed and is not a variable-length group
            // (§13.18.29.4 GR3; the last two are unreachable here, SR1 rejected them above, and the predicate
            // states the rule rather than the residue). A national group's / bit group's subordinates take
            // usage NATIONAL / BIT by their OWN rules (§13.18.29.3), which is why §13.18.63.3 SR14 names only the
            // alphanumeric one. THE predicate is ItemCategory's — this file used to keep a private second copy
            // of GR3 (kb/Work PB337); the one in ItemCategory was the incomplete half, which is the drift a
            // rule written down twice always produces.
            bool alphanumericGroup = ItemCategory.IsAlphanumericGroup(item);

            foreach (var sub in Subordinates(item))
            {
                // Anchor at the offending entry — except where that entry is a CLONE of a type declaration's
                // member: its DeclaredAt points into the TYPEDEF, which is legal on its own, and the violation
                // exists only at the reference site that added the VALUE. Anchor there instead.
                using var _ = Edition.At(StrongTypeModel.TypeAnchor(sub) == item ? item : sub);
                string name = sub.CobolName ?? "FILLER";

                // SR14 conjunct 1 — no JUSTIFIED, no SYNCHRONIZED anywhere in the subtree. (Unlike the usage
                // conjunct this one is NOT scoped to alphanumeric groups: the sentence's first clause says
                // "subordinate items within that group", full stop.)
                if (sub.Justified || sub.Synchronized)
                    Edition.Error(DiagnosticCatalog.GroupValueSubordinate, $"data item '{name}' is subordinate to "
                        + $"'{subject}', which specifies a group-level VALUE clause, and is described with a "
                        + $"{(sub.Justified ? "JUSTIFIED" : "SYNCHRONIZED")} clause — subordinate items within that "
                        + "group shall not be described with a JUSTIFIED or SYNCHRONIZED clause "
                        + "(ISO §13.18.63.3 SR14)");

                // SR13 sentence 2 — no VALUE clause at subordinate levels within this group. GR5 initializes
                // the AREA "without consideration for the individual elementary or group items contained
                // within this group", so a subordinate VALUE has no defined effect; the rule forbids writing
                // one rather than leaving the two initializations to race.
                if (sub.RawValue is not null || sub.TableValues is not null)
                    Edition.Error(DiagnosticCatalog.GroupValueSubordinate, $"data item '{name}' specifies a VALUE "
                        + $"clause and is subordinate to '{subject}', which specifies a group-level VALUE clause — "
                        + "the VALUE clause shall not be specified at subordinate levels within this group "
                        + "(ISO §13.18.63.3 SR13)");

                // SR14 conjunct 2 — every data item subordinate to an ALPHANUMERIC group item carrying a VALUE
                // shall be explicitly or implicitly usage DISPLAY. "Implicitly" is why this pass runs after
                // UsageInheritancePass: a group-level USAGE has already propagated to its leaves (§13.18.60.4
                // GR1), so an inherited COMP is caught on the leaf exactly as a written one is. A nested GROUP
                // is itself a data item — it is usage DISPLAY when it is an alphanumeric group ("An
                // alphanumeric group item is treated as though it had a usage of display", §8.5.2), and is
                // NOT when it carries GROUP-USAGE NATIONAL or BIT.
                if (!alphanumericGroup) continue;
                if (SubordinateUsageOf(sub) is not { } usage || usage is Usage.Display) continue;
                Edition.Error(DiagnosticCatalog.GroupValueSubordinate, $"data item '{name}' is subordinate to "
                    + $"'{subject}', an alphanumeric group item specifying a group-level VALUE clause, and is "
                    + $"described with usage {UsageFamilies.UsageWord(usage)} — all data items subordinate to an alphanumeric "
                    + "group item shall be explicitly or implicitly described with usage DISPLAY "
                    + "(ISO §13.18.63.3 SR14)");
            }
        }
    }

    /// <summary>The §13.18.63.3 VALUE-clause SUBJECT picture of a GROUP item: its §8.5.2.1 class-and-category and
    /// the size the rules measure a literal against.
    ///
    /// <para>⛔ NOT <see cref="DataItem.OperandPic"/>, which is deliberately null for an alphanumeric group
    /// because such a group is not an elementary OPERAND — §14.9.25.4 GR4 makes a MOVE across it a conversion-free
    /// character copy that Table 16 does not describe. Here the group is the SUBJECT OF A VALUE CLAUSE, a position
    /// in which §8.5.2.1 gives it a category outright ("an alphanumeric group item has class and category
    /// alphanumeric"; "a bit group item has class and category boolean"; "a national group item has class and
    /// category national") and §13.18.63.3 SR4/SR5/SR10 give it a size ("the size of the group item"). The bit and
    /// national arms are §13.18.29.4 GR1b/GR2b's as-if PICTURE, already derived once on the model
    /// (<see cref="DataItem.AsIfPic"/>) — boolean positions for a bit group, national positions for a national
    /// one; the alphanumeric arm is GR3's residue, its character positions being
    /// <see cref="DataItem.ImageWidth"/> and its usage display (§8.5.2 — "An alphanumeric group item is treated as
    /// though it had a usage of display").</para></summary>
    private static PicInfo GroupSubjectPic(DataItem group) =>
        group.AsIfPic
        ?? new PicInfo(PicCategory.Alphanumeric, Usage.Display, group.ImageWidth, Digits: 0, Scale: 0, Signed: false);

    /// <summary>The §8.5.2.1 name of a group item's category, for the SR13 diagnostic's subject description —
    /// the words the standard uses for the three kinds of group item, so the message names the rule's own
    /// vocabulary rather than an enum member.</summary>
    private static string GroupCategoryWord(PicCategory category) => category switch
    {
        PicCategory.Boolean => "a bit",
        PicCategory.National => "a national",
        _ => "an alphanumeric",
    };

    /// <summary>A GROUP entry carrying a VALUE clause — format 1 (<see cref="DataItem.RawValue"/>) or, per SR16,
    /// format 2 (<see cref="DataItem.TableValues"/>) — WRITTEN or ASSUMED.</summary>
    private static bool HasGroupValue(DataItem item) =>
        item.IsGroup && (item.RawValue is not null || item.TableValues is not null);

    /// <summary>The subject of §13.18.63.3 SR13/SR14 (and of SR1 as written): a GROUP entry that WROTE a VALUE
    /// clause. A VALUE this entry only ASSUMED from a type declaration or a SAME AS target belongs to the entry
    /// that wrote it (see <see cref="DataItem.ValueCopiedFrom"/>), which is screened where it is declared.</summary>
    private static bool IsGroupValueSubject(DataItem item) => HasGroupValue(item) && item.ValueCopiedFrom is null;

    /// <summary>SR1 for a group whose VALUE clause was ASSUMED from <paramref name="source"/> — the TYPE template's
    /// group, the cloned member it reproduces, or the SAME AS target (§13.18.57.4 GR1, §13.18.58.4 GR1,
    /// §13.18.49.4 GR1).
    ///
    /// <para>⛔ <b>Suppress only a predicate the source could already answer</b> (kb/Work PB2517; PB2071's same
    /// hazard). SR13 and SR14 are properties of the entry that wrote the clause and the entries under it, and the
    /// source answered for them. SR1 is a property of what the subject IS, and §8.5.3.1 makes a group strongly
    /// typed only when it is described with, or subordinate to an item described with, a TYPE clause naming a
    /// STRONG declaration — so the group inside <c>01 U IS TYPEDEF STRONG. 05 G VALUE "AB".</c> conforms (a
    /// template is not a typed item) and <c>01 V TYPE U.</c> is what composes the violation: V.G is a
    /// strongly-typed group item (§8.5.3.1 second case) that carries the VALUE (§13.18.58.4 GR3). The screen
    /// therefore asks the composed entry, and stays silent only when the source was already in the same shape —
    /// a chained declaration (<c>01 S2 TYPEDEF TYPE S</c>) or a SAME AS target already reported where the
    /// strength was first composed.</para>
    ///
    /// <para>Anchored at the entry whose TYPE clause composed the shape — the outermost strongly-typed entry
    /// (<see cref="StrongTypeModel.StrongRoot"/>), which for a nested reference is NOT the nearest TYPE clause
    /// (<c>01 Q IS TYPEDEF STRONG. 05 H2 TYPE S. 01 R TYPE Q.</c> is strong through R's clause, not H2's). The
    /// declaration is legal on its own, exactly as the SR14 arm's clone anchoring reasons, and the message names
    /// the subject because one reference site can compose several VALUE-carrying groups.</para></summary>
    private void ScreenAssumedGroupValueSubject(DataItem item, DataItem source, string subject)
    {
        if (Sr1SubjectShape(item) is not { } shape || Sr1SubjectShape(source) is not null) return;
        var anchor = StrongTypeModel.StrongRoot(item) ?? StrongTypeModel.TypeAnchor(item) ?? item;
        using var _ = Edition.At(anchor);
        Edition.Error(DiagnosticCatalog.GroupValueSubjectShape, $"data item '{subject}' carries a group-level "
            + $"VALUE clause assumed from the description of '{source.CobolName ?? source.CsName}' and is {shape}, "
            + $"composed by the TYPE clause of '{anchor.CobolName ?? anchor.CsName}' "
            + "— the subject of the entry shall not be a strongly-typed group item or a variable-length group "
            + "(ISO §13.18.63.3 SR1; §13.18.58.4 GR3)");
    }

    /// <summary>Whether any ancestor of <paramref name="item"/> is itself a group-level VALUE subject — the
    /// SR13-sentence-2 shape, already rejected once at the outer subject, whose subtree walk covers this one.</summary>
    private static bool AncestorIsGroupValueSubject(DataItem item)
    {
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (IsGroupValueSubject(p)) return true;
        return false;
    }

    /// <summary>The §13.18.63.3 SR1 shapes barred from being the subject of a VALUE clause, as the clause of the
    /// diagnostic naming which one was found, or null. A "variable-length group" is §8.5.1.12.1's — "a group item
    /// whose data description has at least one dynamic-length elementary item or dynamic-capacity table as a
    /// subordinate item" — NOT an occurs-depending group, whose size is its maximum.</summary>
    private static string? Sr1SubjectShape(DataItem item) =>
        StrongTypeModel.IsStrongGroup(item) ? "a strongly-typed group item (ISO §8.5.3)"
        : ReferenceResolver.HasVariableLengthSubordinate(item)
            ? "a variable-length group (ISO §8.5.1.12.1 — a dynamic-length elementary item or a "
              + "dynamic-capacity table is subordinate to it)"
        : null;

    /// <summary>The effective USAGE of one subordinate entry for §13.18.63.3 SR14: an elementary item's own
    /// (already inherited) usage; a nested group's <c>DISPLAY</c> unless it carries GROUP-USAGE NATIONAL or BIT.
    /// Null for an entry with neither (a recovery leaf with no PICTURE and no children) — SR14 cannot speak
    /// about an item the binder failed to describe, and a second diagnostic there would be noise.</summary>
    private static Usage? SubordinateUsageOf(DataItem sub) =>
        sub.IsGroup
            ? sub.GroupUsage switch
            {
                GroupUsage.National => Usage.National,
                GroupUsage.Bit => Usage.Bit,
                _ => Usage.Display,     // §8.5.2 — an alphanumeric group is treated as though usage display
            }
            : sub.Pic?.Usage;

    /// <summary>Every data item subordinate to <paramref name="group"/>, at any depth, in declaration order.
    /// SR13/SR14 say "subordinate to", not "immediately subordinate to", so the walk is the whole subtree —
    /// a COMP leaf two levels down is as much a violation as one directly under the subject.</summary>
    private static IEnumerable<DataItem> Subordinates(DataItem group)
    {
        foreach (var c in group.Children)
        {
            yield return c;
            foreach (var d in Subordinates(c)) yield return d;
        }
    }
}
