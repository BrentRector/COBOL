// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The ISO §13.16.3 SR24 condition-name association screen's SHAPE, held against drift (kb/Work PB488).
///
/// <para><b>Why this test exists.</b> The screen REJECTS source, so its failure mode is not a missing
/// diagnostic — it is a compiler that refuses a legal program. SR24 is an EXCLUSION list, and every letter is
/// one word away from a shape it does not exclude: c) says "an ALPHANUMERIC group" where d) says "a group";
/// e) names four CLASSES where the neighbouring §13.18.60.3 SR4 names the six PHRASES that produce them;
/// g) says "a GROUP item subordinate to such a type declaration", which leaves an elementary member legal; and
/// h) says "a variable-length group", which §8.5.1.12.1 defines as a group with a DYNAMIC-LENGTH item or a
/// DYNAMIC-CAPACITY table subordinate — not an occurs-depending-on group. Widening any of those by one word
/// starts rejecting ordinary COBOL. The conformance goldens pin that end to end; this pins it at the table.</para>
///
/// <para><b>And it holds the table COMPLETE.</b> The screen is written as one row per lettered exclusion so a
/// ninth exclusion is a row rather than a ninth scattered test (CLAUDE.md rule 5). A table is only worth that
/// if something asserts it still carries every letter, in the standard's order, with the standard's words.</para>
/// </summary>
public sealed class ConditionNameAssociationDriftTests
{
    // ⛔ SR24 e)'s class test and SR4's phrase reader live on ItemCategory (kb/Work PB391) — the ONE §8.5.2
    // class/category reader — because §14.7.6 rule 4 names the SAME four classes with the same words and its
    // asker, CorrespondingBinder, is outside DataBinder. Public there, so no reflection.
    private static bool ExcludedClass(DataItem d) => ItemCategory.IsIndexMessageTagObjectOrPointer(d);

    private static string? Sr4PhraseOf(Usage? u) => ItemCategory.Sr4PhraseOf(u);

    private static DataItem Elem(string name, PicInfo pic, int level = 1) =>
        new() { Level = level, CobolName = name, CsName = name, Pic = pic };

    private static PicInfo Alnum(int len = 2) =>
        new(PicCategory.Alphanumeric, Usage.Display, Length: len, Digits: 0, Scale: 0, Signed: false);

    private static DataItem Group(string name, int level, params DataItem[] children)
    {
        var g = new DataItem { Level = level, CobolName = name, CsName = name };
        foreach (var c in children)
        {
            c.Parent = g;
            g.AddMember(c);
        }
        return g;
    }

    /// <summary>ISO §13.16.3 SR24's exclusion list, transcribed from <c>specs/ISO_COBOL.md</c> and validated
    /// with <c>python scripts/spec/cite.py --check 13.16.3</c>. The table under test must say exactly this.
    /// ⛔ If a row's words change, do not "fix" this string — re-read the clause and re-derive the predicate;
    /// the words are what the diagnostic quotes back to the programmer.</summary>
    private static readonly (char Letter, string Text)[] TheStandardsList =
    [
        ('a', "Another level 88 entry."),
        ('b', "A level 66 entry."),
        ('c', "An alphanumeric group containing items with a usage other than display."),
        ('d', "A group containing items described with a JUSTIFIED or SYNCHRONIZED clause."),
        ('e', "A data item of the class index, message-tag, object, or pointer."),
        ('f', "A data item described with the ANY LENGTH clause."),
        ('g', "A type declaration described with the STRONG phrase, or a group item subordinate to such a "
            + "type declaration."),
        ('h', "A variable-length group."),
    ];

    [Fact]
    public void TheTableIsExactlyTheStandardsEightLetteredExclusionsInOrder()
    {
        Assert.Equal(TheStandardsList.Length, DataBinder.Sr24Exclusions.Count);
        for (int i = 0; i < TheStandardsList.Length; i++)
        {
            Assert.Equal(TheStandardsList[i].Letter, DataBinder.Sr24Exclusions[i].Letter);
            Assert.Equal(TheStandardsList[i].Text, DataBinder.Sr24Exclusions[i].Text);
            Assert.False(string.IsNullOrWhiteSpace(DataBinder.Sr24Exclusions[i].Article),
                $"exclusion {TheStandardsList[i].Letter}) has no message phrasing — the diagnostic would name "
                + "the rule without naming what the entry actually is.");
        }
    }

    /// <summary>Every row's predicate fires on ITS OWN shape and the screen names ITS letter. A row whose
    /// predicate nothing can trigger is a lookup nobody has ever contradicted — including row a), which the
    /// binder cannot reach (no <see cref="DataItem"/> in the forest carries level 88, because a condition-name
    /// is a <see cref="Condition88"/>) and which is therefore verified HERE or nowhere.</summary>
    [Fact]
    public void EveryLetteredExclusionFiresOnItsOwnShape()
    {
        Assert.Equal('a', Excl(new DataItem { Level = 88, CobolName = "C", CsName = "C" }));
        Assert.Equal('b', Excl(new DataItem { Level = 66, CobolName = "R", CsName = "R" }));

        var comp = Elem("X", new PicInfo(PicCategory.Numeric, Usage.Binary, 4, 4, 0, false), level: 5);
        Assert.Equal('c', Excl(Group("G", 1, comp, Elem("Y", Alnum(), 5))));

        var just = Elem("J", Alnum(), 5);
        just.Justified = true;
        Assert.Equal('d', Excl(Group("G", 1, just)));
        var sync = Elem("S", Alnum(), 5);
        sync.Synchronized = true;
        Assert.Equal('d', Excl(Group("G", 1, sync)));
        // §13.18.55.3 SR1 permits SYNCHRONIZED on the GROUP and §13.18.55.4 GR1 makes that a description of
        // its members, so the subject's OWN clause reaches d) — the binder does not propagate the flag down.
        var syncGroup = Group("G", 1, Elem("A", Alnum(), 5));
        syncGroup.Synchronized = true;
        Assert.Equal('d', Excl(syncGroup));

        Assert.Equal('e', Excl(Elem("IX", PicInfo.IndexItem)));
        var anyLen = Elem("AL", Alnum());
        anyLen.IsAnyLength = true;
        Assert.Equal('f', Excl(anyLen));

        var strongDecl = new DataItem { Level = 1, CobolName = "T", CsName = "T", IsTypedef = true, TypedefStrong = true };
        Assert.Equal('g', Excl(strongDecl));
        var subGroup = Group("SG", 5, Elem("F1", Alnum(), 10));
        subGroup.Parent = strongDecl;
        Assert.Equal('g', Excl(subGroup));

        var dynLen = Elem("D", Alnum(), 5);
        dynLen.IsDynamicLength = true;
        Assert.Equal('h', Excl(Group("G", 1, dynLen)));
        var dynCap = new DataItem
        {
            Level = 5, CobolName = "E", CsName = "E", Pic = Alnum(),
            OccursSpec = new OccursSpec { Min = 1, Max = 3, IsDynamic = true },
        };
        Assert.Equal('h', Excl(Group("G", 1, dynCap)));
    }

    /// <summary>⛔ THE OVER-REJECTION PIN. Each shape below is one SR24 does NOT exclude and each is one word
    /// away from a letter that would catch it.</summary>
    [Fact]
    public void ShapesTheRuleDoesNotExcludeAreNotExcluded()
    {
        // A plain elementary DISPLAY item — the overwhelmingly common conditional variable.
        Assert.Null(DataBinder.ConditionalVariableExclusion(Elem("P", Alnum())));

        // c) reaches only a group "containing items with a usage other than display".
        Assert.Null(DataBinder.ConditionalVariableExclusion(
            Group("AG", 1, Elem("X", Alnum(), 5), Elem("Y", Alnum(), 5))));

        // c) reaches only an ALPHANUMERIC group. §13.18.29.4 GR2 makes a national group "an elementary item of
        // usage national described with PICTURE N(m)", so its national members do not exclude it.
        var natGroup = Group("NG", 1,
            Elem("NF", new PicInfo(PicCategory.National, Usage.National, 2, 0, 0, false), 5));
        natGroup.GroupUsage = GroupUsage.National;
        Assert.Null(DataBinder.ConditionalVariableExclusion(natGroup));

        // g)'s second arm is "a GROUP item subordinate to such a type declaration" — an elementary member is
        // not one.
        var strongDecl = new DataItem { Level = 1, CobolName = "T", CsName = "T", IsTypedef = true, TypedefStrong = true };
        var member = Elem("F1", Alnum(), 5);
        member.Parent = strongDecl;
        Assert.Null(DataBinder.ConditionalVariableExclusion(member));

        // A WEAK type declaration is not excluded at all — g) names the STRONG phrase.
        Assert.Null(DataBinder.ConditionalVariableExclusion(
            new DataItem { Level = 1, CobolName = "W", CsName = "W", IsTypedef = true }));
    }

    /// <summary>⛔ c)'s SUBJECT IS §3.11's "ALPHANUMERIC GROUP ITEM", WHICH EXCLUDES FOUR KINDS (kb/Work PB947). The
    /// predicate was "a group with no GROUP-USAGE clause" — two of the four — so a VARIABLE-LENGTH group holding a
    /// non-DISPLAY member answered c) and the first-match walk never reached h), the letter it violates; a
    /// STRONGLY-TYPED group instance answered c) too, though SR24 letter g) is the type DECLARATION's and the
    /// instance is barred by §13.18.57.3 SR2. Both now come out of c), and the ONE spelling that says so is
    /// <c>ItemCategory.IsAlphanumericGroup</c>.</summary>
    [Fact]
    public void LetterCAsksTheAlphanumericGroupNotTheGroupWithoutGroupUsage()
    {
        static DataItem Comp(string name, int level = 5) =>
            Elem(name, new PicInfo(PicCategory.Numeric, Usage.Binary, 4, 4, 0, false), level);
        static DataItem Dyn(string name)
        {
            var d = Elem(name, Alnum(), 5);
            d.IsDynamicLength = true;
            return d;
        }

        // A variable-length group: letter h), never c) — the non-DISPLAY member is beside the dynamic one.
        Assert.Equal('h', Excl(Group("VG", 1, Comp("X"), Dyn("D"))));
        // …and nested one level down, where the HasVariableLengthSubordinate walk and the c) walk both recurse.
        var inner = Group("IN", 5, Comp("Y", 10), Dyn("D2"));
        Assert.Equal('h', Excl(Group("OUTER", 1, inner)));

        // A strongly-typed group instance (it carries StrongType from its TYPE clause) is no alphanumeric group, and
        // g) is the declaration's letter, so SR24 excludes it by no letter; COBOLNET1537 owns the instance.
        var strongInstance = Group("SG", 1, Comp("P1"));
        strongInstance.StrongType = true;
        Assert.Null(DataBinder.ConditionalVariableExclusion(strongInstance));

        // The plain alphanumeric group keeps c).
        Assert.Equal('c', Excl(Group("AG", 1, Comp("X2"))));
    }

    /// <summary>⛔ AN OCCURS DEPENDING ON GROUP IS NOT A VARIABLE-LENGTH GROUP. §8.5.1.12.1: "A variable-length
    /// group is a group item whose data description has at least one dynamic-length elementary item or
    /// dynamic-capacity table as a subordinate item." The standard names the occurs-depending-on group
    /// separately wherever it means it, so reading ODO into h) would reject a shape COBOL-85 programs are full
    /// of. The PB488 finding's own probe for h) used an ODO table and so measured the wrong construct; this is
    /// the pin that the implementation did not inherit that mistake.</summary>
    [Fact]
    public void OccursDependingOnGroupIsNotAVariableLengthGroup()
    {
        var odo = new DataItem
        {
            Level = 5, CobolName = "E", CsName = "E", Pic = Alnum(),
            OccursSpec = new OccursSpec { Min = 1, Max = 3, DependingName = "N" },
        };
        var g = Group("G", 1, Elem("N", new PicInfo(PicCategory.Numeric, Usage.Display, 1, 1, 0, false), 5), odo);
        Assert.Null(DataBinder.ConditionalVariableExclusion(g));
    }

    /// <summary>SR24 e) names the CLASSES index, message-tag, object and pointer; §13.18.60.3 SR4 names the SIX
    /// USAGE PHRASES that produce exactly those classes. They are ONE population and are resolved through ONE
    /// predicate, so a usage added to either side cannot drift the other. (SR14's five-phrase set is the one
    /// that must stay DIFFERENT — it omits INDEX — and <c>UsageDeclarationPlacementDriftTests</c> pins that.)
    /// </summary>
    [Fact]
    public void ExclusionEIsExactlySr4sUsagePopulation()
    {
        foreach (var u in Enum.GetValues<Usage>())
        {
            var item = Elem("X", new PicInfo(PicCategory.Numeric, u, 1, 1, 0, false));
            Assert.Equal(Sr4PhraseOf(u) is not null, ExcludedClass(item));
        }
        // The WRITTEN clause arm: MESSAGE-TAG and FUNCTION-POINTER never gain a PicInfo (the usage is refused
        // or staged at ParseUsage), so only DataItem.OwnUsage sees them and the screen must read it.
        foreach (var u in new[] { Usage.MessageTag, Usage.FunctionPointer, Usage.Pointer, Usage.Index })
            Assert.True(ExcludedClass(new DataItem { Level = 1, CobolName = "X", CsName = "X", OwnUsage = u }));
        Assert.False(ExcludedClass(new DataItem { Level = 1, CobolName = "G", CsName = "G" }));
    }

    /// <summary>Exclusion e)'s row carries the §13.18.60.3 SR11 cross-citation, because the two rules state the
    /// same prohibition and a programmer who looks up one should find the other.</summary>
    [Fact]
    public void ExclusionERowCitesSr11AsWell()
    {
        var e = DataBinder.Sr24Exclusions.Single(r => r.Letter == 'e');
        Assert.Contains("13.18.60.3 SR11", e.Also);
        foreach (var other in DataBinder.Sr24Exclusions.Where(r => r.Letter != 'e'))
            Assert.Equal("", other.Also);
    }

    private static char Excl(DataItem d) =>
        DataBinder.ConditionalVariableExclusion(d) is { } row
            ? row.Letter
            : throw new InvalidOperationException(
                $"ISO §13.16.3 SR24 excludes '{d.CobolName}' and the screen admitted it.");
}
