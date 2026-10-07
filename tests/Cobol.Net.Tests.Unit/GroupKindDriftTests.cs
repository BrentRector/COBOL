// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Binding.Validation;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SHAPE PIN FOR "WHICH KIND OF GROUP ITEM IS THIS?" (kb/Work PB392).
///
/// <para><b>Why this test exists.</b> ISO §14.9.2.3 SR6 and §14.9.44.3 SR6 name FOUR of the five group kinds —
/// <i>"alphanumeric group items, national group items, variable-length groups, or strongly-typed group
/// items"</i> — and the CORRESPONDING screen asked the boolean <c>DataItem.IsGroup</c> instead. The fifth kind,
/// a <c>GROUP-USAGE BIT</c> group, was therefore ADMITTED to ADD and SUBTRACT CORRESPONDING and the statement
/// ran as a silent no-op. A conformance golden proves the bit case is refused today; it cannot prove that the
/// screen still asks a SET tomorrow, which is the property that was missing. This pins the set.</para>
///
/// <para><b>And it holds the classification's two spec invariants.</b> §3.11 makes <i>alphanumeric group
/// item</i> the COMPLEMENT of the other four, and §13.18.29.3 SR1 — <i>"The GROUP-USAGE clause may be specified
/// only if the subject of the entry is a group item that is not strongly typed and not a variable-length
/// group"</i> — keeps the two GROUP-USAGE kinds disjoint from the other two. Those two facts are what let one
/// flags reader replace three hand-written copies of the classification
/// (<c>ItemCategory.IsAlphanumericGroup</c>, <c>ItemCategory.Face</c> and the <c>Admits</c> group arm).</para>
/// </summary>
public sealed class GroupKindDriftTests
{
    private static PicInfo Alnum(int len = 3) =>
        new(PicCategory.Alphanumeric, Usage.Display, Length: len, Digits: 0, Scale: 0, Signed: false);

    private static DataItem Elem(string name, PicInfo pic, int level = 5) =>
        new() { Level = level, CobolName = name, CsName = name, Pic = pic };

    private static DataItem Group(string name, GroupUsage usage = GroupUsage.None, DataItem? child = null)
    {
        var g = new DataItem { Level = 1, CobolName = name, CsName = name, GroupUsage = usage };
        DataItem c = child ?? Elem(name + "C", Alnum());
        c.Parent = g;
        g.AddMember(c);
        return g;
    }

    /// <summary>A group with a DYNAMIC-LENGTH elementary item subordinate — §8.5.1.12.1's own definition of a
    /// variable-length group ("at least one dynamic-length elementary item or dynamic-capacity table as a
    /// subordinate item"). Built through the model's stored fact, not through a second definition.</summary>
    private static DataItem VariableLengthGroup()
    {
        var leaf = Elem("VLEAF", Alnum());
        leaf.IsDynamicLength = true;
        return Group("VG", GroupUsage.None, leaf);
    }

    /// <summary>A strongly-typed group — §8.5.3.3. <c>StrongType</c> is the stored fact <c>ExpandTypes</c>
    /// writes for a group described with a <c>TYPEDEF … STRONG</c> type.</summary>
    private static DataItem StrongGroup()
    {
        var g = Group("SG");
        g.StrongType = true;
        g.TypeName = "T1";
        return g;
    }

    /// <summary>ISO §3.11 — <i>"group item except for a bit group item, a national group item, a strongly-typed
    /// group item, or a variable-length group item"</i>. ALPHANUMERIC is therefore the COMPLEMENT: it is set on
    /// a group when, and only when, no other kind is. A classification that reports both is a classification a
    /// rule admitting only one of them would answer wrongly.</summary>
    [Fact]
    public void AlphanumericIsTheComplementOfTheOtherFourKinds()
    {
        foreach (DataItem g in new[]
                 {
                     Group("AG"), Group("BG", GroupUsage.Bit), Group("NG", GroupUsage.National),
                     StrongGroup(), VariableLengthGroup(),
                 })
        {
            GroupKinds k = ItemCategory.GroupKindsOf(g);
            bool others = (k & ~GroupKinds.Alphanumeric) != GroupKinds.None;
            Assert.True(k.HasFlag(GroupKinds.Alphanumeric) != others,
                $"ISO §3.11 makes an alphanumeric group item the complement of the other four kinds, but "
                + $"'{g.CobolName}' classified as {k}.");
        }
    }

    /// <summary>ISO §13.18.29.3 SR1 — <i>"The GROUP-USAGE clause may be specified only if the subject of the
    /// entry is a group item that is not strongly typed and not a variable-length group"</i> — is what makes a
    /// BIT or NATIONAL arm terminal in the reader. If that ever stops holding, the two terminal arms silently
    /// start hiding a strong or variable-length kind from a rule that admits it.</summary>
    [Theory]
    [InlineData(GroupUsage.Bit, GroupKinds.Bit)]
    [InlineData(GroupUsage.National, GroupKinds.National)]
    public void AGroupUsageGroupIsNeitherStronglyTypedNorVariableLength(GroupUsage usage, GroupKinds expected)
    {
        var leaf = Elem("L", Alnum());
        leaf.IsDynamicLength = true;
        var g = Group("G", usage, leaf);
        g.StrongType = true;
        g.TypeName = "T1";
        Assert.Equal(expected, ItemCategory.GroupKindsOf(g));
    }

    /// <summary>The ONE-READER pin: <c>IsAlphanumericGroup</c> — the spelling every §13.18.29.4 GR3 rule asks —
    /// must BE the reader's alphanumeric flag, not a fourth hand-written conjunction beside it. That duplication
    /// is what kb/Work PB337 measured once already, in this same file.</summary>
    [Fact]
    public void IsAlphanumericGroupIsTheReadersAlphanumericFlag()
    {
        foreach (DataItem d in new[]
                 {
                     Group("AG"), Group("BG", GroupUsage.Bit), Group("NG", GroupUsage.National),
                     StrongGroup(), VariableLengthGroup(), Elem("E", Alnum(), level: 1),
                 })
            Assert.Equal(ItemCategory.GroupKindsOf(d).HasFlag(GroupKinds.Alphanumeric),
                ItemCategory.IsAlphanumericGroup(d));
    }

    /// <summary>An item that is not a group has NO kind. §8.5.1.3.1 reserves "elementary" for a record's
    /// undivided subdivisions; either way a rule about group items must not be given an answer about something
    /// that is not one.</summary>
    [Fact]
    public void ANonGroupHasNoKind()
    {
        Assert.Equal(GroupKinds.None, ItemCategory.GroupKindsOf(Elem("E", Alnum(), level: 1)));
        // A PICTURE-less entry with no subordinates — the compiler's error-recovery artifact.
        Assert.Equal(GroupKinds.None,
            ItemCategory.GroupKindsOf(new DataItem { Level = 1, CobolName = "Z", CsName = "Z" }));
    }

    /// <summary>⛔ THE RULE ROWS CARRY WHAT THE CLAUSES SAY. ADD §14.9.2.3 SR6 and SUBTRACT §14.9.44.3 SR6 are
    /// the same sentence, so their admitted sets must be the same value; MOVE §14.9.25.3 SR12 says only "group
    /// data items", so its set is every kind — which NOTE 5 under §14.9.25.4 GR11 confirms for the two the
    /// arithmetic verbs exclude ("bit group items and national group items are processed as group items").
    /// Narrowing MOVE's row here would start rejecting legal source.</summary>
    [Fact]
    public void TheThreeCorrespondingRowsCarryTheirOwnClausesSets()
    {
        Assert.Equal(CorrespondingOperandRule.Add.Admitted, CorrespondingOperandRule.Subtract.Admitted);
        Assert.Equal(GroupKinds.Any, CorrespondingOperandRule.Move.Admitted);
        Assert.False(CorrespondingOperandRule.Add.Admitted.HasFlag(GroupKinds.Bit),
            "ISO §14.9.2.3 SR6 names alphanumeric, national, variable-length and strongly-typed groups — a bit "
            + "group is the one kind it leaves out, and §14.7.6 rule 3 (both items numeric) is why.");
        foreach (GroupKinds k in new[]
                 {
                     GroupKinds.Alphanumeric, GroupKinds.National, GroupKinds.VariableLength,
                     GroupKinds.StronglyTyped,
                 })
            Assert.True(CorrespondingOperandRule.Add.Admitted.HasFlag(k), $"SR6 admits {k} by name.");
        // Only the arithmetic spellings name level-66; SR12 does not, and quoting it there was a miscitation.
        Assert.True(CorrespondingOperandRule.Add.ExcludesLevel66);
        Assert.True(CorrespondingOperandRule.Subtract.ExcludesLevel66);
        Assert.False(CorrespondingOperandRule.Move.ExcludesLevel66);
    }

    /// <summary>The diagnostic's admitted-set phrase is GENERATED from the set, so it cannot disagree with the
    /// check. Asserted against SR6's own words, in SR6's own order.</summary>
    [Fact]
    public void TheAdmittedSetSpellsItselfInTheStandardsWords()
    {
        Assert.Equal(
            "alphanumeric group items, national group items, variable-length groups, or strongly-typed group items",
            ItemCategory.Spell(CorrespondingOperandRule.Add.Admitted));
        // §14.9.25.3 SR12 writes the whole set as two words, not as an enumeration.
        Assert.Equal("group data items", ItemCategory.Spell(GroupKinds.Any));
    }
}
