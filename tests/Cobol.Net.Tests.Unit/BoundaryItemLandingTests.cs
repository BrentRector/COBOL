// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ACTIVATION BOUNDARY'S §14.2.3 GR9 / GR10 LANDING (kb/Work PB2549): a numeric formal registered with its profile and
/// its carrier (<see cref="BoundaryItem.Landing"/>) lands an argument by "a COMPUTE statement without the ROUNDED phrase"
/// into "a data item with the same description and the same number of bytes as the formal parameter", the landing the
/// activating element performs itself when it knows the formal (<see cref="CobolArgAdapt.LandForFormal{T}"/>). The golden
/// <c>2002/pb2549_runtime_located_call_landing</c> witnesses the lane end to end; this pins the item's own contract.
/// <para>A non-numeric elementary formal registers its record builder instead (<see cref="MoveLanding"/>, kb/Work PB2587):
/// GR9's "otherwise, a MOVE statement", applied to the sending operand the activating element states
/// (<see cref="CobolArg.Sending"/>).</para>
/// </summary>
public sealed class BoundaryItemLandingTests
{
    private static NumProfile Zoned(int digits, int scale = 0) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = false,
        Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
    };

    private static CobolArg Content(long unscaled, NumProfile argument) =>
        new(CobolPassMode.Content, ManagedPointer<long>.Cell(unscaled), argument);

    [Fact]
    public void NumericFormal_TakesTheComputesLowOrderDigits_InItsOwnDescription()
    {
        // 1234 into PIC 9(3): §14.7.5 with no SIZE ERROR phrase and checking off stores the low-order digits (DOC-A.1-70).
        var formal = new BoundaryItem(Zoned(3), Landing: CarrierLanding<long>.Instance);
        var landed = formal.Land(Content(1234, Zoned(4)), checking: false);
        Assert.Equal(234L, Assert.IsType<ManagedPointer<long>>(landed.Carrier).Value);
        Assert.Equal(Zoned(3), landed.Num);   // the allocated record IS the argument from here on (GR9's last sentence)
    }

    [Fact]
    public void NumericFormal_AlignsTheScale_WithoutRounding()
    {
        // 12.34 (PIC 99V99) into PIC 9(4): the COMPUTE truncates the fraction — 0012.
        var formal = new BoundaryItem(Zoned(4), Landing: CarrierLanding<long>.Instance);
        var landed = formal.Land(Content(1234, Zoned(4, 2)), checking: false);
        Assert.Equal(12L, Assert.IsType<ManagedPointer<long>>(landed.Carrier).Value);
    }

    [Fact]
    public void Checked_Overflow_RaisesEcSizeTruncation()
    {
        var formal = new BoundaryItem(Zoned(3), Landing: CarrierLanding<long>.Instance);
        var raised = Assert.Throws<CobolSizeError>(() => formal.Land(Content(1234, Zoned(4)), checking: true));
        Assert.Equal("EC-SIZE-TRUNCATION", raised.EcName);
    }

    [Fact]
    public void FormalWithNoLanding_PassesTheArgumentUntouched()
    {
        // A formal that states no landing (a group, a pointer, an ANY LENGTH item) converts nothing at the boundary.
        var argument = Content(1234, Zoned(4));
        Assert.Equal(argument, new BoundaryItem(null, 6).Land(argument, checking: false));
        // A checking-unit registration with a profile but no carrier lands nothing either.
        Assert.Equal(argument, new BoundaryItem(Zoned(3), 3).Land(argument, checking: false));
    }

    // ── §14.2.3 GR9's MOVE leg (kb/Work PB2587) — golden 2002/pb2587_content_move_leg is the lane end to end ──

    /// <summary>A record builder standing in for the compiler's receiving half: X(6) JUSTIFIED RIGHT.</summary>
    private static readonly MoveLanding JustifiedSix = new(static s => s.Characters.PadLeft(6)[^6..]);

    [Fact]
    public void MoveFormal_FillsItsRecord_FromTheStatedSendingOperand()
    {
        var argument = Content(-12, Zoned(3)) with { Sending = new MoveSending("012", Numeric: true, Unscaled: -12) };
        var landed = new BoundaryItem(null, Landing: JustifiedSix).Land(argument, checking: false);
        // The record of the formal's description crosses as characters with no numeric description (GR11), and it is
        // the argument from here on (GR9's last sentence), so it states no sending operand of its own.
        Assert.Equal("   012", Assert.IsType<ManagedPointer<string>>(landed.Carrier).Value);
        Assert.Null(landed.Num);
        Assert.Null(landed.Sending);
        Assert.Equal(CobolPassMode.Content, landed.Mode);
    }

    [Fact]
    public void MoveFormal_LeavesAnArgumentThatStatesNoSendingOperand_AndAnOmittedOne()
    {
        // A non-COBOL activator states no sending operand; an omitted argument (§14.9.4.4 GR11) has nothing to send.
        var plain = Content(1234, Zoned(4));
        Assert.Equal(plain, new BoundaryItem(null, Landing: JustifiedSix).Land(plain, checking: false));
        var omitted = new CobolArg(CobolPassMode.Content, ManagedPointer.Null, null) { Sending = new MoveSending("X") };
        Assert.Equal(omitted, new BoundaryItem(null, Landing: JustifiedSix).Land(omitted, checking: false));
    }

    [Fact]
    public void MoveSending_AlignsTheValue_ByTruncation()
    {
        // §14.6.8.2 4): aligned by decimal point, truncated — a MOVE has no ROUNDED phrase.
        Assert.Equal((Int128)1234, new MoveSending("12345", Numeric: true, Unscaled: 12345, Scale: 3).AtScale(2));
        Assert.Equal((Int128)123, new MoveSending("1.239", Numeric: true, Real: 1.239).AtScale(2));
        Assert.Equal(12.5, new MoveSending("1250", Numeric: true, Unscaled: 1250, Scale: 2).Binary64);
    }

    [Fact]
    public void WithValueSending_ReadsTheEvaluatedCell_NeverTheExpressionAgain()
    {
        var number = new CobolArg(CobolPassMode.Content, ManagedPointer<Int128>.Cell(-1250), Zoned(4, 2));
        var sent = Assert.IsType<MoveSending>(CobolArgAdapt.WithValueSending(number).Sending);
        Assert.True(sent.Numeric);
        Assert.Equal((Int128)(-1250), sent.Unscaled);
        Assert.Equal(2, sent.Scale);
        Assert.DoesNotContain("-", sent.Characters);   // §14.9.25.4 GR6 a): the operational sign is not moved

        var text = new CobolArg(CobolPassMode.Content, ManagedPointer<string>.Cell("ABC"), null);
        var chars = Assert.IsType<MoveSending>(CobolArgAdapt.WithValueSending(text).Sending);
        Assert.Equal("ABC", chars.Characters);
        Assert.False(chars.Numeric);
    }
}
