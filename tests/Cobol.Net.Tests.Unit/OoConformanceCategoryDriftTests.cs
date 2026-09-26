// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ARM THIS PINS REJECTED LEGAL SOURCE FOR FIVE WHOLE PICTURE CATEGORIES, SILENTLY AND FOR AS LONG AS
/// THE FUNCTION HAS EXISTED (fix-queue PB46). <see cref="OoConformance.DescriptionMismatch"/> — the ONE
/// §14.8.2.3.2 identical-description check every INVOKE argument and every override signature goes through —
/// switched on the formal's category with arms for object-reference, numeric and alphanumeric and a
/// <c>default:</c> that answered "formal category {c} is not yet carried across INVOKE". So a category-boolean,
/// category-national, numeric-edited, pointer or program-pointer FORMAL PARAMETER was impossible in every
/// passing mode — BY REFERENCE, BY CONTENT and bare alike — while the marshaling arms had carried all of them
/// the whole time.
/// <para>The failure was invisible to every gate because a "not yet carried" message READS like a deliberate
/// staging decision (feedback_green_test_can_hold_a_gap_open). Nothing was staged: §14.8.2.3.2 states one rule
/// for every category, and its own lettered exceptions b and c pair a BIT GROUP and a NATIONAL GROUP with the
/// matching elementary items — the standard contemplates exactly the categories the default arm refused.</para>
/// <para>So this test asserts the property the switch must keep: <b>no PICTURE category may fall into a
/// rejecting default</b>. A new <see cref="PicCategory"/> member fails here rather than silently becoming the
/// sixth category that cannot cross an INVOKE.</para>
/// </summary>
public sealed class OoConformanceCategoryDriftTests
{
    /// <summary>Every <see cref="PicCategory"/> that can describe an ELEMENTARY item. <see cref="PicCategory.Group"/>
    /// is excluded by construction, not by omission: a group item has no <see cref="PicInfo"/> at all and
    /// <c>DescriptionMismatch</c> returns through its <c>formal.IsGroup</c> branch before the switch.</summary>
    public static IEnumerable<object[]> ElementaryCategories =>
        Enum.GetValues<PicCategory>().Where(c => c is not PicCategory.Group).Select(c => new object[] { c });

    private static DataItem Item(PicCategory category) => new()
    {
        Level = 1,
        CobolName = "P",
        CsName = "P",
        Pic = PicFor(category),
    };

    /// <summary>A minimal, self-consistent description per category — the point is only that the two sides are
    /// IDENTICAL, which is precisely what §14.8.2.3.2 rule 2 requires ("the same ALIGN, BLANK WHEN ZERO,
    /// DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses").</summary>
    private static PicInfo PicFor(PicCategory category) => category switch
    {
        PicCategory.Numeric => new PicInfo(category, Usage.Display, Length: 4, Digits: 4, Scale: 0, Signed: true),
        PicCategory.NumericEdited => new PicInfo(category, Usage.Display, Length: 3, Digits: 3, Scale: 0, Signed: false)
            { EditMask = "ZZ9", Clause = PictureClauseIdentity.Of("ZZ9", '$', "$", decimalPointIsComma: false) },
        PicCategory.National => new PicInfo(category, Usage.National, Length: 4, Digits: 0, Scale: 0, Signed: false),
        PicCategory.Boolean => new PicInfo(category, Usage.Bit, Length: 4, Digits: 0, Scale: 0, Signed: false),
        PicCategory.ObjectReference => new PicInfo(category, Usage.ObjectReference, Length: 0, Digits: 0, Scale: 0, Signed: false)
            { ObjectRef = ObjectRefDescriptor.ObjectClass("CLS") },
        PicCategory.Pointer => new PicInfo(category, Usage.Pointer, Length: 0, Digits: 0, Scale: 0, Signed: false),
        PicCategory.ProgramPointer => new PicInfo(category, Usage.ProgramPointer, Length: 0, Digits: 0, Scale: 0, Signed: false),
        // A function-pointer is PICTURE-less like its two class-pointer siblings, and it is always
        // RESTRICTED (§13.18.60.2's TO phrase is unbracketed), so the self-consistent description carries a
        // prototype name — which is also the field §14.8.2.3.2's "both shall be restricted and of the same
        // type" sentence compares. kb/Work PB452 + PB817.
        PicCategory.FunctionPointer => new PicInfo(category, Usage.FunctionPointer, Length: 0, Digits: 0, Scale: 0, Signed: false)
            { RestrictedPrototypeName = "FPROTO" },
        _ => new PicInfo(category, Usage.Display, Length: 4, Digits: 0, Scale: 0, Signed: false),
    };

    /// <summary>Two IDENTICALLY described elementary items conform — for EVERY category. This is the whole
    /// §14.8.2.3.2 rule-2 identity case, and the arm that used to answer otherwise is the defect.</summary>
    [Theory]
    [MemberData(nameof(ElementaryCategories))]
    public void IdenticalDescriptions_Conform_InEveryCategory(PicCategory category)
    {
        string? why = OoConformance.DescriptionMismatch(Item(category), Item(category));
        Assert.True(why is null,
            $"category {category} does not conform to ITSELF under §14.8.2.3.2 rule 2: {why}");
    }

    /// <summary>The failing direction, so a vacuously-passing identity test cannot hide a rule that checks
    /// nothing (feedback_green_gates_arent_evidence): a WIDER picture must still be refused in every category
    /// whose PICTURE has a size. Length-less carriers (the object/pointer families) have no size clause to
    /// differ in and are skipped.
    /// <para>⚠ The numeric arm measures the picture by DIGITS, not by <see cref="PicInfo.Length"/> — the two
    /// are independent fields and only the digit/scale/sign triple is the numeric PICTURE — so this widens both
    /// rather than assuming they move together.</para></summary>
    [Theory]
    [MemberData(nameof(ElementaryCategories))]
    public void AWiderPicture_IsRefused_WhereThePictureHasASize(PicCategory category)
    {
        var formal = Item(category);
        if (formal.Pic!.Length == 0) return;   // PICTURE-less carrier — no size clause exists
        var wider = new DataItem
        {
            Level = 1, CobolName = "Q", CsName = "Q",
            Pic = formal.Pic with
            {
                Length = formal.Pic.Length + 2,
                Digits = formal.Pic.Digits == 0 ? 0 : formal.Pic.Digits + 2,
            },
        };
        Assert.NotNull(OoConformance.DescriptionMismatch(formal, wider));
    }

    /// <summary>USAGE is a rule-2 clause in its own right, and for a BOOLEAN item it is genuinely free: PIC 1
    /// is USAGE DISPLAY or USAGE BIT (§13.18.60.3 SR5) and both map to the same '0'/'1' character storage
    /// (D-B1), so nothing but this compare keeps the two declarations identical.</summary>
    [Fact]
    public void BooleanFormal_RefusesADifferentUsage()
    {
        var bit = Item(PicCategory.Boolean);
        var display = new DataItem
        {
            Level = 1, CobolName = "Q", CsName = "Q",
            Pic = bit.Pic! with { Usage = Usage.Display },
        };
        Assert.NotNull(OoConformance.DescriptionMismatch(bit, display));
    }

    /// <summary>An EDITED picture's identity is its editing character-string: two numeric-edited items of the
    /// same character count are still different PICTURE clauses, which rule 2 names directly.</summary>
    [Fact]
    public void EditedFormal_RefusesADifferentMask_AtTheSameLength()
    {
        var zz9 = Item(PicCategory.NumericEdited);
        var starred = new DataItem
        {
            Level = 1, CobolName = "Q", CsName = "Q",
            Pic = zz9.Pic! with { EditMask = "**9", Clause = PictureClauseIdentity.Of("**9", '$', "$", decimalPointIsComma: false) },
        };
        Assert.NotNull(OoConformance.DescriptionMismatch(zz9, starred));
    }
}
