// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A FLOATING-POINT FORMAL IS A NUMERIC RECEIVER OF THE §14.2.3 GR9/GR10 COMPUTE (kb/Work PB1114).
/// ISO §14.8.2.3.3 2) a) — "If the formal parameter is numeric, the conformance rules are the same as for a COMPUTE
/// statement with the argument as the sending operand" — has no floating-point exemption, and GR9's first leg says
/// the same ("if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase"). The landing
/// used to cover fixed-point formals only, and a float formal's description has ZERO digit positions, so the
/// fixed-point landing's capacity reduction (<c>value % 10^0</c>) stored 0. The float lane lands through
/// <see cref="FloatResultant"/> — the one transfer of an arithmetic value into a float resultant identifier —
/// under implied TRUNCATION (§14.7.4.3 rule 10: the nearest representable value nearer to zero).
/// <para>RED BEFORE THE FIX: the kb/Work PB1114 repro (a <c>PIC 9(3)V99</c> 12.5 BY CONTENT to a FLOAT-LONG formal of a
/// NESTED activation) printed <c>IN-S [0]</c> — the same zero the first row below asserts away — because
/// <c>LandForFormal</c>'s guard admitted fixed-point formals only and the callee's <c>Num</c> landed the fixed-point
/// argument through the float formal's zero-digit description.</para>
/// </summary>
public sealed class CallAbiFloatLandingTests
{
    private static NumProfile Fixed(int digits, int scale) => new()
    {
        Digits = digits,
        FractionDigits = scale,
        Signed = false,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Zoned,
    };

    /// <summary>The profile an emitted FLOAT-LONG formal carries (<c>_P_n</c>): no digit positions, IEEE binary64.</summary>
    private static NumProfile FloatLong => new()
    {
        Digits = 0,
        FractionDigits = 0,
        Signed = true,
        SignKind = NumericSign.TrailingOverpunch,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Ieee64,
        StorageLength = 8,
    };

    /// <summary>The profile an emitted FLOAT-SHORT formal carries: IEEE binary32.</summary>
    private static NumProfile FloatShort => FloatLong with { ByteForm = NumericByteForm.Ieee32, StorageLength = 4 };

    /// <summary>A fixed-point argument crosses to a FLOAT-LONG formal as its exact value: <c>PIC 9(3)V99</c> 12.50
    /// (unscaled 1250, scale 2) is 12.5 — the note's repro, where the formal read 0.</summary>
    [Fact]
    public void FixedPointArgument_LandsInAFloatLongFormalAsItsValue()
    {
        var arg = new CobolArg(CobolPassMode.Content, ManagedPointer<long>.Cell(1250L), Fixed(5, 2));
        var landed = CobolArgAdapt.LandForFormal<double>(arg, FloatLong, 0, checking: false);
        Assert.Equal(12.5d, Assert.IsType<ManagedPointer<double>>(landed.Carrier).Value);
        Assert.Equal(FloatLong, landed.Num);
        // …and the callee's adoption of the landed record is the identity.
        Assert.Equal(12.5d, CobolArgAdapt.Num<double>([landed], 0, FloatLong, 0).Value);
    }

    /// <summary>The landing is the un-ROUNDED COMPUTE: 0.1 is not a binary32 value, so §14.7.4.3 rule 10 lands the
    /// nearest value NEARER TO ZERO — the binary32 just below 0.1, never the round-to-nearest 0.1000000015. Landed in
    /// ONE rounding from the exact scaled value, not through a binary64 first.</summary>
    [Fact]
    public void FixedPointArgument_LandsInAFloatShortFormalTruncated()
    {
        var arg = new CobolArg(CobolPassMode.Content, ManagedPointer<long>.Cell(1L), Fixed(2, 1));
        var landed = CobolArgAdapt.LandForFormal<float>(arg, FloatShort, 0, checking: false);
        float v = Assert.IsType<ManagedPointer<float>>(landed.Carrier).Value;
        Assert.True((double)v < 0.1d, "the landed binary32 is not above 0.1");
        Assert.Equal(MathF.BitDecrement(0.1f), v);
    }

    /// <summary>Every numeric carrier of the boundary lands in a float formal as its value: the four integer tiers
    /// at scale 2, both float carriers (a FLOAT-SHORT into a FLOAT-LONG formal is exact).</summary>
    [Theory]
    [InlineData("long")]
    [InlineData("ulong")]
    [InlineData("Int128")]
    [InlineData("UInt128")]
    [InlineData("double")]
    [InlineData("float")]
    public void EveryNumericCarrier_LandsInAFloatFormalAsItsValue(string carrier)
    {
        (ManagedPointer Carrier, int Scale) arg = carrier switch
        {
            "long" => (ManagedPointer<long>.Cell(150L), 2),
            "ulong" => (ManagedPointer<ulong>.Cell(150UL), 2),
            "Int128" => (ManagedPointer<Int128>.Cell((Int128)150), 2),
            "UInt128" => (ManagedPointer<UInt128>.Cell((UInt128)150), 2),
            "double" => (ManagedPointer<double>.Cell(1.5d), 0),
            "float" => (ManagedPointer<float>.Cell(1.5f), 0),
            _ => throw new ArgumentOutOfRangeException(nameof(carrier)),
        };
        var args = new[] { new CobolArg(CobolPassMode.Value, arg.Carrier, Fixed(7, arg.Scale)) };
        Assert.Equal(1.5d, CobolArgAdapt.NumValue<double>(args, 0, FloatLong, 0).Value);
        Assert.Equal(1.5d, Assert.IsType<ManagedPointer<double>>(
            CobolArgAdapt.LandForFormal<double>(args[0], FloatLong, 0, checking: false).Carrier).Value);
    }

    /// <summary>The 16-byte UNSIGNED container reaches 2^128 − 1, past every signed carrier (kb/Work R10): its value
    /// crosses as itself, never as the two's-complement reading of its bits.</summary>
    [Fact]
    public void UnsignedWideArgument_LandsAsItsMagnitude()
    {
        var arg = new CobolArg(CobolPassMode.Content, ManagedPointer<UInt128>.Cell((UInt128)1 << 127), Fixed(31, 0));
        var landed = CobolArgAdapt.LandForFormal<double>(arg, FloatLong, 0, checking: false);
        Assert.Equal(Math.Pow(2, 127), Assert.IsType<ManagedPointer<double>>(landed.Carrier).Value);
    }

    /// <summary>A value past the float format's range is §14.7.5 case 3's size error: with EC-SIZE checking enabled
    /// at the activating statement the landing RAISES EC-SIZE-TRUNCATION (no-phrase rule 4); with it off the
    /// no-phrase disposition stands and the value lands at the nearest value nearer to zero, the format's largest
    /// finite value (rule 10 with an unbounded exponent).</summary>
    [Fact]
    public void ValuePastTheFormat_RaisesUnderChecking_AndLandsAtMaxValueWithout()
    {
        var arg = new CobolArg(CobolPassMode.Content, ManagedPointer<double>.Cell(1e300d), FloatLong);
        var ex = Assert.Throws<CobolSizeError>(() => CobolArgAdapt.LandForFormal<float>(arg, FloatShort, 0, checking: true));
        Assert.Equal("EC-SIZE-TRUNCATION", ex.EcName);
        var landed = CobolArgAdapt.LandForFormal<float>(arg, FloatShort, 0, checking: false);
        Assert.Equal(float.MaxValue, Assert.IsType<ManagedPointer<float>>(landed.Carrier).Value);
    }

    /// <summary>The residual callee-side views (a crossing whose formal the activator could not know — §14.9.4.4 GR9's
    /// first branch) share the float landing: <see cref="CobolArgAdapt.Num"/>'s converting view reads the value and
    /// writes back un-scaled into the caller's own cell.</summary>
    [Fact]
    public void ConvertingView_ReadsTheValue_AndWritesBackInTheCallersRepresentation()
    {
        var cell = ManagedPointer<long>.Cell(1250L);
        var view = CobolArgAdapt.Num<double>([new CobolArg(CobolPassMode.Reference, cell, Fixed(5, 2))], 0, FloatLong, 0);
        Assert.Equal(12.5d, view.Value);
        view.Value = 7.25d;
        Assert.Equal(725L, cell.Value);
    }
}
