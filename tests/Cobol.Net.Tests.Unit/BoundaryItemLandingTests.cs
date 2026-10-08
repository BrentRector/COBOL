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
    public void FormalWithNoCarrier_PassesTheArgumentUntouched()
    {
        // A non-numeric formal states no carrier: its SET or MOVE is the activated element's adapter's.
        var argument = Content(1234, Zoned(4));
        Assert.Equal(argument, new BoundaryItem(null, 6).Land(argument, checking: false));
        // A checking-unit registration with a profile but no carrier lands nothing either.
        Assert.Equal(argument, new BoundaryItem(Zoned(3), 3).Land(argument, checking: false));
    }
}
