// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Binding.Procedure;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ANTI-DUPLICATION PIN FOR ISO §14.7.6 RULE 2 (kb/Work PB391).
///
/// <para>Rule 2 — <i>"In a MOVE statement, at least one of the data items is an elementary data item and the
/// resulting move is valid according to the rules for the MOVE statement"</i> — sends the CORRESPONDING pairing
/// decision to the MOVE statement's own rules, whose category half is §14.9.25.3 SR10, Table 16. That table has
/// exactly ONE home, <see cref="MoveTable16"/>, whose own header says <i>"asking it in two places is how the two
/// answers drift"</i>. <c>CorrespondingBinder</c> asked it in a second place for a year, and the two answers
/// drifted in BOTH directions at once — five measured cells, three pairs moved that Table 16 refuses and two
/// silently dropped that it admits.</para>
///
/// <para><b>What this test is for.</b> A conformance golden proves the cells are right TODAY. It cannot
/// prove that the CORRESPONDING filter is still the SAME FUNCTION as the direct-MOVE guard tomorrow, which is
/// the property that was lost. So this walks the whole modeled Table-16 position matrix and asserts the binder's
/// rule-2 filter agrees with <see cref="MoveTable16.DataItemRefusal"/> on EVERY cell — which it can only do by
/// being a delegation. Re-introduce one private arm and this fails; it was verified to fail against the deleted
/// <c>CorrMoveValid</c> on eight sender rows.</para>
///
/// <para><b>And it pins the SECOND half, which is not Table 16 at all</b> (kb/Work PB391). §14.9.25.3 SR10 — the
/// rule that routes to the table — governs only <i>"all other cases not described in Syntax rules 8 and 9"</i>,
/// so rule 2 also owes SR8 (a fixed-width binary sender needs a numeric or numeric-edited receiver) and SR9 (a
/// variable-length group operand needs a compatible group opposite). Asking <see cref="MoveValidity.Table16Refusal"/>
/// alone skipped both, MEASURED: a <c>BINARY-LONG</c> namesake paired with a <c>PIC X(5)</c> one and overwrote
/// it while the written MOVE of the same two items was refused COBOLNET0819, and a variable-length-group
/// namesake paired with an elementary one and reached the run time as a <c>NotImplementedCobolFeatureException</c>.
/// The matrix carries a <c>Binary-long</c> and a <c>Variable-length-group</c> position for exactly that reason,
/// and <see cref="TheBoundAndItemKeyedShapeEntriesAnswerTogether"/> pins the two-arm split the fix could have
/// re-created.</para>
///
/// <para><b>And it pins the trap the deletion sprang.</b> <see cref="MoveValidity.Table16Refusal"/> ADMITS a
/// pointer × pointer pair — it screens group / boolean / national / alphabetic / numeric and returns null for
/// everything else — so routing rule 2 to it without rule 4's class exclusion would have converted a silent
/// EXCLUSION into a silent pointer copy. <see cref="CorrespondingRule4ClassExclusionIsTheOnlyThingRefusingAPointerPair"/>
/// asserts both halves of that, so the exclusion cannot be quietly dropped as redundant.</para>
/// </summary>
public sealed class CorrespondingRule2DriftTests
{
    /// <summary>The binder's §14.7.6 rule-2 filter, reached by reflection because it is a private detail of an
    /// internal binder. The throw names the rule rather than the member so a restructuring is told WHAT it has
    /// to keep true, not merely that a name moved.</summary>
    private static bool CorrRule2MoveValid(DataItem s, DataItem d) =>
        (bool)(typeof(CorrespondingBinder)
            .GetMethod("CorrRule2MoveValid", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "CorrespondingBinder.CorrRule2MoveValid is gone — ISO §14.7.6 rule 2 still has to be answered by "
                + "MoveTable16 and by nothing else. Re-point this test at whatever asks the table; do NOT delete "
                + "the assertion (kb/Work PB391)."))
            .Invoke(null, [s, d])!;

    private static DataItem Elem(string name, PicInfo pic) =>
        new() { Level = 5, CobolName = name, CsName = name, Pic = pic };

    private static PicInfo P(PicCategory cat, int len = 4, int digits = 0, int scale = 0,
                             bool alphabetic = false, string? edit = null, Usage usage = Usage.Display) =>
        new(cat, usage, len, digits, scale, Signed: false) { IsAlphabetic = alphabetic, EditMask = edit };

    /// <summary>Every Table-16 position this data model can describe, named the way the printed table names its
    /// rows and columns (<c>specs/ISO_COBOL.md</c>, §14.9.25.3 Table 16). The numeric row's Integer/Noninteger
    /// split and the alphabetic / edited riders are separate entries because <see cref="Table16Operand"/> keys on
    /// them and <see cref="PicCategory"/> deliberately cannot.</summary>
    /// <para>The last two are NOT Table-16 positions: a <c>BINARY-LONG</c> item sits in the table's Numeric row
    /// like any other binary integer, and a variable-length group sits in no row at all. They are here because
    /// §14.9.25.3 SR8 and SR9 key on those SHAPES and SR10 defers to both, so rule 2's delegation is only
    /// complete if the filter answers them too.</para>
    private static readonly string[] AllPositions =
    [
        "Alphabetic", "Alphanumeric", "Alphanumeric-edited", "Boolean", "National", "National-edited",
        "Numeric-integer", "Numeric-noninteger", "Numeric-edited", "Group",
        "Binary-long", "Variable-length-group",
    ];

    public static TheoryData<string> Positions => [.. AllPositions];

    private static DataItem At(string position) => position switch
    {
        "Alphabetic" => Elem("P", P(PicCategory.Alphanumeric, alphabetic: true)),
        "Alphanumeric" => Elem("P", P(PicCategory.Alphanumeric)),
        "Alphanumeric-edited" => Elem("P", P(PicCategory.Alphanumeric, edit: "XXBXX")),
        "Boolean" => Elem("P", P(PicCategory.Boolean, usage: Usage.Bit)),
        "National" => Elem("P", P(PicCategory.National, usage: Usage.National)),
        "National-edited" => Elem("P", P(PicCategory.National, usage: Usage.National, edit: "NNBNN")),
        "Numeric-integer" => Elem("P", P(PicCategory.Numeric, digits: 4)),
        "Numeric-noninteger" => Elem("P", P(PicCategory.Numeric, digits: 4, scale: 2)),
        "Numeric-edited" => Elem("P", P(PicCategory.NumericEdited, digits: 4, edit: "ZZZ9")),
        "Group" => GroupOf("P"),
        // §14.9.25.3 SR8's shape: usage binary-long. Its Table-16 POSITION is the Numeric/Integer row — SR8 is a
        // rule ABOUT that row's members, not a row of its own, which is why the two answers can differ.
        "Binary-long" => Elem("P", P(PicCategory.Numeric, digits: 9, usage: Usage.BinaryLong)),
        // §8.5.1.12.1's shape: a group with a dynamic-length elementary subordinate. Only COBOL-2014 can declare
        // one; the model carries it as a flag, so the test needs no edition.
        "Variable-length-group" => VarLenGroupOf("P"),
        _ => throw new ArgumentOutOfRangeException(nameof(position), position, "unmodelled Table-16 position"),
    };

    private static DataItem GroupOf(string name)
    {
        var g = new DataItem { Level = 5, CobolName = name, CsName = name };
        var c = new DataItem { Level = 10, CobolName = "C", CsName = "C", Pic = P(PicCategory.Alphanumeric) };
        g.AddMember(c);
        return g;
    }

    private static DataItem VarLenGroupOf(string name)
    {
        var g = new DataItem { Level = 5, CobolName = name, CsName = name };
        var c = new DataItem
        {
            Level = 10, CobolName = "C", CsName = "C", IsDynamicLength = true,
            Pic = P(PicCategory.Alphanumeric, len: 1),
        };
        g.AddMember(c);
        return g;
    }

    /// <summary>⭐ THE WHOLE MATRIX. For every sending × receiving position the model can describe, the binder's
    /// rule-2 filter answers exactly what <see cref="MoveTable16.DataItemRefusal"/> answers — which is only
    /// possible if it delegates. A hundred and forty-four cells is the smallest statement of "one rule, one
    /// place" that a second private table cannot satisfy by accident.</summary>
    [Theory]
    [MemberData(nameof(Positions))]
    public void Rule2FilterIsMoveTable16OnEveryModeledCell(string sender)
    {
        DataItem s = At(sender);
        foreach (string receiver in AllPositions)
        {
            DataItem d = At(receiver);
            bool moveRules = MoveTable16.DataItemRefusal(s, d) is null;
            Assert.True(moveRules == CorrRule2MoveValid(s, d),
                $"ISO §14.7.6 rule 2 and the §14.9.25.3 MOVE rules disagree for {sender} -> {receiver}: the MOVE "
                + $"rules say {(moveRules ? "Yes" : "No")} and the CORRESPONDING filter says "
                + $"{(!moveRules ? "Yes" : "No")}. Rule 2 is a delegation to the MOVE rules; a second copy of one "
                + "of them has been reintroduced.");
        }
    }

    /// <summary>⛔ THE TWO-ARM PIN on the two composite entries. The §14.9.25.3 chain is asked from two entries —
    /// <c>MoveTable16.Validity</c>, over a BOUND operand, which the written MOVE, INITIALIZE and the INVOKE screen
    /// reach, and <c>MoveTable16.DataItemRefusal</c>, over two data items, which §14.7.6 rule 2 reaches (a
    /// CORRESPONDING pair has no bound operand until after the pairing decision). Fixing one and not the other is
    /// exactly the defect kb/Work PB391 landed against, so for every modeled sender x receiver the two entries
    /// must return the SAME reason. They can only do that by running one chain (kb/Work PB878 made the per-rule
    /// readers private, so no caller can compose a different one).</summary>
    [Theory]
    [MemberData(nameof(Positions))]
    public void TheBoundAndItemKeyedShapeEntriesAnswerTogether(string sender)
    {
        DataItem s = At(sender);
        var bound = new BoundFieldOperand(new MemberPlace(new AccessPath([]), s));
        foreach (string receiver in AllPositions)
        {
            DataItem d = At(receiver);
            Assert.Equal(MoveTable16.DataItemRefusal(s, d),
                         MoveTable16.Validity(bound, new MemberPlace(new AccessPath([]), d))?.Reason);
        }
    }

    /// <summary>The five cells kb/Work PB391 MEASURED wrong, as the table's own verdicts. The matrix above
    /// proves the two askers agree; this proves they agree on the RIGHT answer, so a drift in
    /// <see cref="MoveTable16"/> itself cannot make both wrong together and still pass.</summary>
    [Theory]
    // Table 16, as printed: Numeric/Integer -> Alphabetic = No; Alphabetic -> Numeric = No;
    // Numeric/Integer -> Boolean = No; National -> National = Yes; Boolean -> Boolean = Yes;
    // Numeric/Noninteger -> National = No.
    [InlineData("Numeric-integer", "Alphabetic", false)]
    [InlineData("Alphabetic", "Numeric-integer", false)]
    [InlineData("Numeric-integer", "Boolean", false)]
    [InlineData("National", "National", true)]
    [InlineData("Boolean", "Boolean", true)]
    [InlineData("Numeric-noninteger", "National", false)]
    public void TheFiveMeasuredCellsCarryTable16sPrintedVerdict(string sender, string receiver, bool corresponds)
        => Assert.Equal(corresponds, CorrRule2MoveValid(At(sender), At(receiver)));

    /// <summary>The two rules §14.9.25.3 SR10 DEFERS to, as their own text reads, asserted through the same
    /// filter. These are not Table-16 cells and the table cannot decide them: a <c>BINARY-LONG</c> item is an
    /// ordinary Numeric/Integer row member to the table, and a variable-length group has no row at all. SR8 —
    /// <i>"identifier-2 shall reference a numeric or numeric-edited item"</i> — is therefore the only thing
    /// separating the first two rows below from the last two, and SR9 the only thing separating the fifth from
    /// the sixth.</summary>
    [Theory]
    // SR8: the receiver must be numeric or numeric-edited — everything else, GROUP included, is refused.
    [InlineData("Binary-long", "Alphanumeric", false)]
    [InlineData("Binary-long", "National", false)]
    [InlineData("Binary-long", "Group", false)]
    [InlineData("Binary-long", "Numeric-integer", true)]
    [InlineData("Binary-long", "Numeric-edited", true)]
    // SR9 / §8.5.1.12.1: a variable-length group moves only to or from a COMPATIBLE GROUP, in either direction.
    [InlineData("Variable-length-group", "Alphanumeric", false)]
    [InlineData("Alphanumeric", "Variable-length-group", false)]
    [InlineData("Variable-length-group", "Group", false)]
    [InlineData("Variable-length-group", "Variable-length-group", true)]
    public void TheTwoRulesSr10DefersToCarryTheirOwnVerdict(string sender, string receiver, bool corresponds)
        => Assert.Equal(corresponds, CorrRule2MoveValid(At(sender), At(receiver)));

    /// <summary>⛔ THE TRAP. §14.7.6 rule 4 — <i>"Neither data item ... is of class index, message-tag, object, or
    /// pointer"</i> — is the ONLY thing that keeps a pointer namesake pair out of the correspondence set: Table 16
    /// has no pointer row or column and <see cref="MoveValidity.Table16Refusal"/> therefore ADMITS the pair. Before
    /// kb/Work PB391 the exclusion was an ACCIDENT of the private copy's <c>_ =&gt; false</c> default, and
    /// deleting that copy without adding rule 4 would have made <c>MOVE CORRESPONDING</c> copy pointers
    /// silently. Both halves are asserted so neither can be dropped as redundant.</summary>
    [Fact]
    public void CorrespondingRule4ClassExclusionIsTheOnlyThingRefusingAPointerPair()
    {
        DataItem ptr = Elem("K", PicInfo.PointerItem());
        Assert.True(CorrRule2MoveValid(ptr, ptr),
            "Table 16 has no pointer row — rule 2 cannot be what excludes a pointer pair.");
        Assert.True(ItemCategory.IsIndexMessageTagObjectOrPointer(ptr),
            "ISO §14.7.6 rule 4's class exclusion is what excludes it, and CorrEligible asks exactly this.");

        // The four classes rule 4 names, each reached the way the model actually carries it. INDEX is a
        // NUMERIC PicInfo distinguished only by its usage (§8.5.2.1 Table 2), and MESSAGE-TAG / FUNCTION-POINTER
        // never gain a PicInfo at all — only the written DataItem.OwnUsage sees them.
        foreach (var pic in new[]
                 {
                     PicInfo.PointerItem(), PicInfo.ProgramPointerItem(),
                     PicInfo.ObjectReferenceItem(ObjectRefDescriptor.Universal), PicInfo.IndexItem,
                 })
            Assert.True(ItemCategory.IsIndexMessageTagObjectOrPointer(Elem("K", pic)));
        foreach (var u in new[] { Usage.MessageTag, Usage.FunctionPointer })
            Assert.True(ItemCategory.IsIndexMessageTagObjectOrPointer(
                new DataItem { Level = 5, CobolName = "K", CsName = "K", OwnUsage = u }));

        // ...and an ordinary alphanumeric child is NOT excluded — the over-rejection direction, which is the
        // hazard a widened class predicate would create (it would silently empty the correspondence set).
        Assert.False(ItemCategory.IsIndexMessageTagObjectOrPointer(Elem("M", P(PicCategory.Alphanumeric))));
    }
}
