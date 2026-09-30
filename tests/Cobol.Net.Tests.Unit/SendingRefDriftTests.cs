// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A NEW <see cref="SendingRef"/> MEMBER MUST DEFAULT TO <b>CHECKED</b>, NEVER TO EXEMPT.
///
/// <para>Each reading in <c>SendingRefRules</c> IS one standard rule's exemption list. The lists are small and
/// the enum is small, so the tempting spelling of a four-entry list is its complement — <c>r is Normal</c> — and
/// that spelling is a trap with a delay fuse: it is correct only while those four members are the WHOLE enum, and
/// it silently EXEMPTS every member added afterwards. kb/Work PB844 added <see cref="SendingRef.MoveToNumeric"/>
/// for §14.9.25.4 GR6 d) 1, and under the old <c>r is Normal</c> reading of §14.6.13.2 rule 3 that member would
/// have turned OFF the EC-DATA-NOT-FINITE check for <c>MOVE &lt;COMP-2&gt; TO &lt;PIC 9(5)&gt;</c> — a raise the
/// standard requires, lost to an enum member that never mentioned floating point.</para>
///
/// <para>So this test states each rule's exemption SET explicitly and asserts the complement: every other member,
/// <b>including one added tomorrow</b>, must come back CHECKED. Adding a member that genuinely earns an exemption
/// means adding it to the set here, in the same change set that adds it to the clause's own reading — which is
/// the point: the exemption becomes a decision someone had to write down twice, with the clause text in front of
/// them, instead of a default nobody chose.</para>
///
/// <para>The lists, from the clauses themselves:</para>
/// <list type="bullet">
/// <item>§14.6.13.2 <b>rule 2</b> (EC-DATA-INCOMPATIBLE, a fixed-point numeric sending item) — exempt in a class
/// condition and in VALIDATE.</item>
/// <item>§14.6.13.2 <b>rule 3</b> (EC-DATA-NOT-FINITE, a standard-float sending operand) — those two, plus a sign
/// condition and a same-usage MOVE.</item>
/// <item>§14.9.25.4 <b>GR6 d) 1</b> (EC-DATA-INCOMPATIBLE, an alphanumeric or national sending operand) — the one
/// rule whose list is an opt-IN, because GR6 d)'s own scope is the single context "when a numeric or
/// numeric-edited item is the receiving item".</item>
/// </list>
/// </summary>
public sealed class SendingRefDriftTests
{
    private static readonly SendingRef[] All = Enum.GetValues<SendingRef>();

    /// <summary>§14.6.13.2 rule 2's exemption list, as the clause states it — "a sending item is referenced in a
    /// class condition, or … a sending item is processed in a VALIDATE statement".</summary>
    private static readonly SendingRef[] Rule2Exempt = [SendingRef.ClassCondition, SendingRef.Validate];

    /// <summary>Rule 3's list: rule 2's two, plus a sign condition and a same-usage MOVE.</summary>
    private static readonly SendingRef[] Rule3Exempt =
        [SendingRef.ClassCondition, SendingRef.Validate, SendingRef.SignCondition, SendingRef.SameUsageMove];

    /// <summary>§14.9.25.4 GR6 d) 1 applies in GR6 d)'s scope and nowhere else.</summary>
    private static readonly SendingRef[] Gr6d1Applies = [SendingRef.MoveToNumeric];

    [Fact]
    public void FixedPointChecked_ExemptsExactlyRule2sList_AndChecksEveryOtherContext()
        => AssertPartition(Rule2Exempt, r => r.FixedPointChecked(), "§14.6.13.2 rule 2 (EC-DATA-INCOMPATIBLE)");

    [Fact]
    public void FloatChecked_ExemptsExactlyRule3sList_AndChecksEveryOtherContext()
        => AssertPartition(Rule3Exempt, r => r.FloatChecked(StandardFloat), "§14.6.13.2 rule 3 (EC-DATA-NOT-FINITE)");

    private static readonly PicInfo StandardFloat = new(PicCategory.Numeric, Usage.FloatBinary64, Length: 8, Digits: 0, Scale: 0, Signed: true);

    /// <summary>kb/Work PB1684: §14.6.13.2 3) scopes EC-DATA-NOT-FINITE to a STANDARD floating-point usage — an
    /// implementor float (COMP-2, FLOAT-LONG) is never checked, in any context.</summary>
    [Fact]
    public void FloatChecked_IsFalseForAnImplementorFloatUsage()
    {
        foreach (var u in new[] { Usage.Double, Usage.FloatShort, Usage.FloatLong, Usage.FloatExtended })
        {
            var pic = new PicInfo(PicCategory.Numeric, u, Length: 8, Digits: 0, Scale: 0, Signed: true);
            foreach (var r in All)
                Assert.False(r.FloatChecked(pic), $"{u} is not a standard floating-point usage; SendingRef.{r} checked it");
        }
    }

    [Fact]
    public void AlphanumericChecked_IsTrueForExactlyTheMoveToNumericContext()
    {
        foreach (var r in All)
            Assert.True(Gr6d1Applies.Contains(r) == r.AlphanumericChecked(),
                $"§14.9.25.4 GR6 d) 1 must be checked in {string.Join("/", Gr6d1Applies)} and nowhere else; "
                + $"SendingRef.{r} answered {r.AlphanumericChecked()}");
    }

    /// <summary>The opposite failure to the one above: a rule that names no context at all would pass an
    /// "exempt where listed" assertion vacuously. Every reading must select a NON-EMPTY, PROPER subset —
    /// it has to raise somewhere and be exempt somewhere, or it is not the clause's list.</summary>
    [Fact]
    public void EveryReading_SelectsSomeContextsAndNotAllOfThem()
    {
        Assert.NotEmpty(All);
        foreach (var (name, f) in new (string, Func<SendingRef, bool>)[]
                 {
                     ("FixedPointChecked", r => r.FixedPointChecked()),
                     ("FloatChecked", r => r.FloatChecked(StandardFloat)),
                     ("AlphanumericChecked", r => r.AlphanumericChecked()),
                 })
        {
            Assert.True(All.Any(f), $"{name} selects NOTHING — the rule can never raise");
            Assert.True(All.Any(r => !f(r)), $"{name} selects EVERYTHING — the rule has no exemption list");
        }
    }

    private static void AssertPartition(SendingRef[] exempt, Func<SendingRef, bool> checkedRead, string rule)
    {
        foreach (var r in All)
        {
            bool expected = !exempt.Contains(r);
            Assert.True(expected == checkedRead(r),
                expected
                    ? $"{rule} raises in every context but {string.Join("/", exempt)} — SendingRef.{r} came back "
                      + "EXEMPT. A new member must be added to that clause's list DELIBERATELY, never inherit an "
                      + "exemption from a reading written as `r is Normal`."
                    : $"{rule} exempts SendingRef.{r}, and the reading says it is checked.");
        }
    }
}
