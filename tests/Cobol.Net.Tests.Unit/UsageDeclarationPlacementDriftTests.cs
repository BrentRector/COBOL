// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §13.18.60.3 declaration-placement screen's SHAPE, held against drift (kb/Work PB183).
///
/// <para><b>Why this test exists, and why it is the non-negotiable half of PB183.</b> The screen rejects source.
/// Its failure mode is therefore NOT a missing diagnostic — it is a compiler that refuses a legal program, and
/// the specific way that happens here is written into the standard: <b>SR14 names FIVE usage phrases and the
/// NEIGHBOURING SR4 names SIX</b>. The difference is INDEX. "The two lists should obviously be the same" is a
/// tidy-up any future editor could make in one line, and it would silently start rejecting
/// <c>05 IX USAGE INDEX.</c> inside an ordinary group — legal COBOL, and a shape real programs are full of.
/// A positive golden pins that end-to-end; this pins it at the predicate, and pins the two things a golden
/// cannot see: that EVERY usage member has a recorded verdict, and that the two screens of this family share
/// ONE class predicate.</para>
///
/// <para>SR14: "A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or
/// PROGRAM-POINTER phrase may be specified only for an elementary data item at level 1 or an elementary data
/// item subordinate to a type declaration that includes the STRONG phrase."</para>
/// <para>SR4: "The INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, and PROGRAM-POINTER phrases
/// shall not be specified in a data item described with the CONSTANT RECORD clause, or in any item subordinate
/// to a data item described with the CONSTANT RECORD clause."</para>
/// </summary>
public sealed class UsageDeclarationPlacementDriftTests
{
    private static MethodInfo M(string name) =>
        typeof(DataBinder).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"DataBinder.{name} is gone — the §13.18.60.3 screen was "
            + "restructured without updating its drift test. Re-derive the rule before re-shaping the test.");

    // ⛔ THE TWO PHRASE READERS LIVE ON ItemCategory (kb/Work PB391), not on DataBinder: SR4's population is
    // also §14.7.6 rule 4's CLASS exclusion and §13.16.3 SR24 e)'s, and CorrespondingBinder — the third asker —
    // is outside DataBinder. They are public there, so these are direct calls rather than reflection.
    private static string? Sr14PhraseOf(Usage? u) => ItemCategory.Sr14PhraseOf(u);
    private static string? Sr4PhraseOf(Usage? u) => ItemCategory.Sr4PhraseOf(u);
    private static bool PointerObjectClass(DataItem d) => (bool)M("PointerObjectClass").Invoke(null, [d])!;
    private static string? Sr14PhraseOf(DataItem d) => (string?)M("Sr14PhraseOf").Invoke(null, [d]);

    /// <summary>The recorded §13.18.60.3 verdict for EVERY <see cref="Usage"/> member: the SR14 phrase name the
    /// screen must report, or null when the rule does not name that usage. Keyed by the enum member's NAME so
    /// that adding a member — the MESSAGE-TAG arm when it lands, a future usage — fails
    /// <see cref="EveryUsageMemberCarriesAnSr14Verdict"/> until a verdict is recorded here. That is the whole
    /// point: a screen written over a hand-listed set silently stops being complete.</summary>
    private static readonly Dictionary<string, string?> Sr14Verdict = new()
    {
        ["Display"] = null,
        ["Binary"] = null,
        ["Packed"] = null,
        ["Comp5"] = null,
        ["Float"] = null,
        ["Double"] = null,
        // ⛔ INDEX IS DELIBERATELY NULL HERE AND "INDEX" IN THE SR4 TABLE. SR14's list omits it; SR4's includes
        // it. This one row is the difference between the two rules and the guard against unifying them.
        ["Index"] = null,
        ["ObjectReference"] = "OBJECT REFERENCE",
        ["National"] = null,
        ["Bit"] = null,
        ["Pointer"] = "POINTER",
        ["ProgramPointer"] = "PROGRAM-POINTER",
        // FUNCTION-POINTER has a bound model (kb/Work PB452/PB817: PicInfo.FunctionPointerItem), so BOTH the
        // written clause and the resolved profile name it.
        ["FunctionPointer"] = "FUNCTION-POINTER",
        ["FloatShort"] = null,
        ["FloatLong"] = null,
        ["FloatExtended"] = null,
        ["FloatBinary32"] = null,
        ["FloatBinary64"] = null,
        ["FloatBinary128"] = null,
        ["FloatDecimal16"] = null,
        ["FloatDecimal34"] = null,
        ["BinaryChar"] = null,
        ["BinaryShort"] = null,
        ["BinaryLong"] = null,
        ["BinaryDouble"] = null,
        // The member landed with kb/Work PB487 and this row is the verdict the missing one was holding open.
        // SR14 names MESSAGE-TAG FIRST in its five-phrase list, so the phrase arm carries it — the
        // FUNCTION-POINTER situation exactly: the usage is DECLINED non-support (Annex A.3 item 4), refused by
        // name at ParseUsage with COBOLNET1943, so its bound PicInfo is a recovery shape and the CLASS arm
        // never sees it, but the WRITTEN clause is visible to the screen and the rule governs it.
        // SR21 ("If the MESSAGE-TAG phrase is specified, no other usage phrase shall be specified in the same
        // data description entry") stays unreachable rather than unimplementable: no message-tag item can ever
        // BIND, so it is still deliberately NOT written as dead code.
        ["MessageTag"] = "MESSAGE-TAG",
    };

    [Fact]
    public void EveryUsageMemberCarriesAnSr14Verdict()
    {
        var members = Enum.GetNames<Usage>().ToHashSet();
        var recorded = Sr14Verdict.Keys.ToHashSet();
        Assert.True(members.SetEquals(recorded),
            "Every USAGE member needs a recorded ISO §13.18.60.3 SR14 verdict. Missing a verdict: ["
            + string.Join(", ", members.Except(recorded)) + "]; recorded for a member that no longer exists: ["
            + string.Join(", ", recorded.Except(members)) + "]. Do not delete the row — derive the rule's "
            + "verdict for the new usage and record it, or the screen quietly stops covering the clause.");
    }

    [Fact]
    public void Sr14PhraseArmMatchesTheRecordedVerdicts()
    {
        foreach (var u in Enum.GetValues<Usage>())
            Assert.Equal(Sr14Verdict[u.ToString()], Sr14PhraseOf(u));
        Assert.Null(Sr14PhraseOf((Usage?)null));
    }

    /// <summary>⛔ THE ANTI-UNIFICATION PIN. SR4's list is SR14's list PLUS INDEX — exactly, and nothing else.
    /// If this ever fails because INDEX moved into the SR14 set, the compiler has begun rejecting legal
    /// <c>05 IX USAGE INDEX.</c>; if it fails because the sets became equal the other way, SR4 stopped covering
    /// the phrase the CONSTANT RECORD rule names first.</summary>
    [Fact]
    public void Sr4ListIsExactlySr14ListPlusIndex()
    {
        var sr14 = Enum.GetValues<Usage>().Where(u => Sr14PhraseOf(u) is not null).ToHashSet();
        var sr4 = Enum.GetValues<Usage>().Where(u => Sr4PhraseOf(u) is not null).ToHashSet();

        Assert.True(sr4.IsSupersetOf(sr14), "ISO §13.18.60.3 SR4's list contains SR14's whole list.");
        Assert.Equal(new HashSet<Usage> { Usage.Index }, sr4.Except(sr14).ToHashSet());
        Assert.Empty(sr14.Except(sr4));
        Assert.DoesNotContain(Usage.Index, sr14);
        Assert.Equal("INDEX", Sr4PhraseOf(Usage.Index));
    }

    /// <summary>⛔ SR14's SUBJECT IS A USAGE PHRASE, READ FROM THE WRITTEN CLAUSE OR THE RESOLVED PROFILE'S USAGE
    /// (kb/Work PB819) — never the resolved CATEGORY. The REDEFINES screen of §13.18.44.3 SR12/SR14 (PB179) names
    /// CLASSES ("class object, message-tag, or pointer") and asks <c>PointerObjectClass</c>; the placement screen
    /// names the five PHRASES that produce those classes and asks <c>Sr14PhraseOf</c>. On every profile that HAS a
    /// class the two agree; they differ on exactly one population, the usage the compiler refuses by name, whose
    /// profile is a recovery shape with no class — which is the cell PB819 closed (arm B was keyed on the class and
    /// never saw it). A written clause on an item with no profile at all is seen by the phrase reader alone.</summary>
    [Fact]
    public void Sr14PhraseReaderAgreesWithTheClassPredicate_ExceptWhereTheUsageIsRefused()
    {
        (PicInfo Pic, bool ClassPredicate, string? Phrase)[] profiles =
        [
            (PicInfo.PointerItem(), true, "POINTER"),
            (PicInfo.ProgramPointerItem(), true, "PROGRAM-POINTER"),
            (PicInfo.FunctionPointerItem("P"), true, "FUNCTION-POINTER"),
            (PicInfo.ObjectReferenceItem(ObjectRefDescriptor.Universal), true, "OBJECT REFERENCE"),
            // The refused usage: a recovery profile that carries the WRITTEN usage and no class.
            (PicInfo.MessageTagRecovery(), false, "MESSAGE-TAG"),
            (PicInfo.IndexItem, false, null),
            (new PicInfo(PicCategory.Numeric, Usage.Display, Length: 1, Digits: 1, Scale: 0, Signed: false), false, null),
            (new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: 1, Digits: 0, Scale: 0, Signed: false), false, null),
        ];
        foreach (var (pic, classPredicate, phrase) in profiles)
        {
            var item = new DataItem { Level = 5, CsName = "X", Pic = pic };
            Assert.Equal(classPredicate, PointerObjectClass(item));
            Assert.Equal(phrase, Sr14PhraseOf(item));
        }
        // The WRITTEN clause alone (no profile): the phrase reader sees it for every usage the rule names.
        foreach (var u in Enum.GetValues<Usage>())
            Assert.Equal(Sr14Verdict[u.ToString()], Sr14PhraseOf(new DataItem { Level = 5, CsName = "X", OwnUsage = u }));
        // And a PICTURE-less group item (no usage, no profile) is in neither.
        Assert.Null(Sr14PhraseOf(new DataItem { Level = 1, CsName = "G" }));
        Assert.False(PointerObjectClass(new DataItem { Level = 1, CsName = "G" }));
    }

    /// <summary>⛔ INDEX IS NOT AMONG SR14's PHRASES. An index data item's <c>PicInfo</c> is
    /// <c>(Numeric, Usage.Index)</c>, and the phrase reader must not name it, so a "let's add Index for symmetry
    /// with SR4" edit fails here before it reaches a user (written clause AND resolved profile).</summary>
    [Fact]
    public void IndexIsNotAmongTheSr14Phrases()
    {
        var resolved = new DataItem { Level = 5, CsName = "IX", Pic = PicInfo.IndexItem };
        var written = new DataItem { Level = 5, CsName = "IX", OwnUsage = Usage.Index };
        Assert.Null(Sr14PhraseOf(resolved));
        Assert.Null(Sr14PhraseOf(written));
        Assert.False(PointerObjectClass(resolved));
    }

    /// <summary>SR14's "at level 1" arm, as ONE named predicate — and the recorded determination that a level-77
    /// entry satisfies it. §8.5.1.3.2 puts a 77 entry outside the level system ("three types of entries exist
    /// for which there is no true concept of level"), and §13.11.1 declares the level-1 and level-77 spellings
    /// ALTERNATIVES for one data element that "bear[s] no hierarchical relationship to any other data item" —
    /// which is precisely the property SR14's first arm stands for. The predicate is named so this determination
    /// is a one-line change if it is ever re-read the other way, and this test is where it is written down.
    /// Levels 66 and 88 never reach the screen: a RENAMES entry and a condition-name are not nodes in the
    /// forest it walks.</summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(77, true)]
    [InlineData(2, false)]
    [InlineData(5, false)]
    [InlineData(49, false)]
    public void Sr14PermittedLevelAdmitsLevel1AndLevel77Only(int level, bool permitted)
    {
        var m = M("Sr14PermittedLevel");
        Assert.Equal(permitted, (bool)m.Invoke(null, [level])!);
    }
}
