// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE CALLER NAMES A CARRIER TYPE FROM <c>DataItem.ElementType</c> AND THE CALLEE READS IT BY A TYPE SWITCH —
/// SO THE TWO SETS ARE ONE SET, AND NOTHING TELLS THEM APART AT COMPILE TIME (kb/Work PB238).
/// </summary>
/// <remarks>
/// <para>
/// <c>CallEmitter.ArgText</c> emits <c>ManagedPointer&lt;{CallNumCarrier(p)}&gt;.Cell(…)</c> for every place the
/// character route (<c>CallPlaceIsString</c>) declines, and <c>CallNumCarrier</c> is <c>DataItem.ElementType</c>
/// — the item's own field type. The callee side is <c>CobolArgAdapt</c>'s private <c>ReadNumericCell</c> /
/// <c>ReadRealCell</c> type switches. A carrier the caller can name and the callee cannot read does not fail to
/// compile. Until kb/Work PB615 it did not fail at the boundary either: the adapter fell to the §14.9.4.4 GR12
/// OMITTED carrier, whose read raised EC-PROGRAM-ARG-OMITTED only under checking and otherwise returned ZERO — a
/// SILENT zero (MEASURED: the boundary assertion below was first written as <c>ThrowsAny</c> and was red). It
/// now fails the activation with EC-PROGRAM-ARG-MISMATCH (GR3d), which is what that assertion pins. The hole
/// PB238 found was the same shape — <c>double</c> was on neither side, so a float leaf was routed onto the
/// CHARACTER image and decoded with <c>CobolNum.ParseDisplay</c> through the receiver's zoned profile instead.
/// </para>
/// <para>
/// ⚠ This is the drift guard the fix is paired with, not a restatement of it. The SIX carriers below are the
/// whole numeric vocabulary of the boundary: kb/Work R12's four integer tiers (<c>long</c>, <c>ulong</c>,
/// <c>Int128</c>, <c>UInt128</c> — the >18-digit and unsigned-binary containers) plus PB238's float lane
/// (<c>double</c>, <c>float</c>). Adding a seventh to <c>DataItem.ElementType</c> without an arm in
/// <c>CobolArgAdapt</c> is what the boundary assertion pins: it fails LOUD rather than reading zero.
/// </para>
/// <para>
/// PROVEN TO FAIL before being trusted: deleting the <c>ManagedPointer&lt;float&gt;</c> arm of
/// <c>ReadRealCell</c> makes the <c>float</c> theory case red (it reads 0 instead of 150), and deleting the
/// <c>ManagedPointer&lt;ulong&gt;</c> arm of <c>ReadNumericCell</c> makes the <c>ulong</c> case red.
/// </para>
/// </remarks>
public sealed class CallAbiNumericCarrierDriftTests
{
    /// <summary>The carried description of an UNSIGNED DISPLAY argument of <paramref name="digits"/> digits at
    /// <paramref name="scale"/> — the shape the (digits, scale) pair this ABI used to carry described.</summary>
    private static NumProfile Meta(int digits, int scale) => new()
    {
        Digits = digits,
        FractionDigits = scale,
        Signed = false,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Zoned,
    };

    /// <summary>The callee's profile for a <c>PIC S9(5)V99</c> BY VALUE formal — 7 digits, scale 2.</summary>
    private static NumProfile Formal => new()
    {
        Digits = 7,
        FractionDigits = 2,
        Signed = true,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Binary,
    };

    /// <summary>Every carrier the CALL boundary may name for a numeric argument reaches the callee as its VALUE.
    /// <para>Each case is the same COBOL value, 1.50, in that carrier's own terms — the four integer tiers carry
    /// the UNSCALED 150 with <c>CobolArg.Scale</c> 2 (ISO §14.2.3 GR10's "COMPUTE statement without the ROUNDED
    /// phrase" then rescales to the formal's own scale, which here is the identity), and the two float carriers
    /// carry the binary64/32 value 1.5 with no scale meta at all, because a binary floating-point item has no
    /// PICTURE digits and the quantization is the RECEIVER's.</para></summary>
    [Theory]
    [InlineData("long")]
    [InlineData("ulong")]
    [InlineData("Int128")]
    [InlineData("UInt128")]
    [InlineData("double")]
    [InlineData("float")]
    public void EveryNamedCarrier_ReachesTheCalleeAsItsValue(string carrier)
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
        var args = new[] { new CobolArg(CobolPassMode.Value, arg.Carrier, Meta(7, arg.Scale)) };
        Assert.Equal(150L, CobolArgAdapt.NumValue<long>(args, 0, Formal, 2).Value);
    }

    /// <summary>A carrier OUTSIDE the six is a SUPPLIED argument the formal cannot read — never the §14.9.4.4
    /// GR12 omitted carrier (kb/Work PB615). §14.9.4.4 GR3d makes a §14.8.2 conformance violation "the program
    /// call is not successful" with EC-PROGRAM-ARG-MISMATCH, so the adoption throws that condition, marked as
    /// raised at adoption so the activation boundary attributes it to THIS call's GR3h. The test FAILS the day a
    /// seventh carrier is added to <c>DataItem.ElementType</c> without an arm in <c>CobolArgAdapt</c> only if
    /// that carrier also stops throwing — and it went red when this adapter still fell to the omitted carrier
    /// (it read ZERO, silently: the defect PB615 closed).</summary>
    [Fact]
    public void ACarrierOutsideTheSix_FailsTheActivationWithArgMismatch()
    {
        var args = new[] { new CobolArg(CobolPassMode.Value, ManagedPointer<decimal>.Cell(1.5m), Meta(7, 2)) };
        var ex = Assert.Throws<CobolCallException>(() => CobolArgAdapt.NumValue<long>(args, 0, Formal, 2));
        Assert.Equal("EC-PROGRAM-ARG-MISMATCH", ex.EcName);
        Assert.True(ex.RaisedAtAdoption);
        // The OMITTED argument is the other case and keeps its GR11/GR12 carrier — no throw at adoption.
        Assert.True(CobolArgAdapt.NumValue<long>([new CobolArg(CobolPassMode.Value, ManagedPointer.Null, null)], 0, Formal, 2).IsNull);
    }

    /// <summary>The callee's profile for a <c>PIC S9(9)V9(9)</c> formal — 18 digits, scale 9 (kb/Work PB288).</summary>
    private static NumProfile Wide => new()
    {
        Digits = 18,
        FractionDigits = 9,
        Signed = true,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Binary,
    };

    /// <summary>⛔ THE TWO ARMS ARE ONE RULE (kb/Work PB288). ISO §14.2.3 GR9 and GR10 describe the SAME
    /// conversion — "if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase" into a
    /// record of the formal's description — and GR11 resolves every reference to the formal through that same
    /// description, so <see cref="CobolArgAdapt.Num"/>'s GR8 aliasing view and
    /// <see cref="CobolArgAdapt.NumValue"/>'s GR10 detached copy owe the IDENTICAL value for the identical
    /// argument. They did not: <c>Num</c> rescaled with the unchecked <c>CobolNum.Rescale</c> straight into
    /// <c>T.CreateTruncating</c> — two binary wraps in series, no digit-capacity landing — while <c>NumValue</c>
    /// composed with <c>CobolNum.Store</c>. 10^30 crossing to a <c>PIC S9(9)V9(9)</c> formal arrived as
    /// 873995514006732800 through one arm and −123822295304634368 through the other; the §14.7.5 case-3
    /// no-phrase disposition (CONFORMANCE.md DOC-A.1-70) is the result's LOW-ORDER digits, 0.
    /// <para>PROVEN TO FAIL: reverting either <c>CobolArgAdapt.Land</c> call site to the bare
    /// <c>CobolNum.Rescale</c> / <c>CobolFloat.ToScaledUnchecked</c> makes the matching row red.</para></summary>
    [Theory]
    // The 31-digit fixed-point argument (§8.3.3.3.2 admits 1 through 31 digits) at scale 0. Aligned at the
    // formal's scale 9 the result is 10^39; the formal's 18 digit positions keep its low-order 18 — all zero.
    [InlineData("Int128-10e30", 0L)]
    // The binary64 nearest 10^30 is exactly 1000000000000000019884624838656; at scale 9 that is
    // 1000000000000000019884624838656000000000, whose low-order 18 digits are 624838656000000000.
    [InlineData("double-1e30", 624838656000000000L)]
    // An 18-digit argument at scale 0: aligned at scale 9 it is 123456789012345678000000000 (27 digits),
    // whose low-order 18 are 012345678000000000 — i.e. the formal shows 12345678.000000000.
    [InlineData("Int128-18digit", 12345678000000000L)]
    // A value the formal HOLDS — the control that keeps the landing from degenerating to a blanket zero.
    [InlineData("Int128-inrange", 123456000000L)]
    public void TheGr8ViewAndTheGr10Copy_LandIdentically(string shape, long expected)
    {
        (ManagedPointer Carrier, int Scale) arg = shape switch
        {
            "Int128-10e30" => (ManagedPointer<Int128>.Cell(Int128.Parse("1000000000000000000000000000000")), 0),
            "double-1e30" => (ManagedPointer<double>.Cell(1.0e30d), 0),
            "Int128-18digit" => (ManagedPointer<Int128>.Cell((Int128)123456789012345678L), 0),
            "Int128-inrange" => (ManagedPointer<Int128>.Cell((Int128)123456), 3),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        var byRef = new[] { new CobolArg(CobolPassMode.Content, arg.Carrier, Meta(38, arg.Scale)) };
        var byValue = new[] { new CobolArg(CobolPassMode.Value, arg.Carrier, Meta(38, arg.Scale)) };
        long view = CobolArgAdapt.Num<long>(byRef, 0, Wide, 9).Value;
        long copy = CobolArgAdapt.NumValue<long>(byValue, 0, Wide, 9).Value;
        Assert.Equal(expected, view);
        Assert.Equal(expected, copy);

        // ⛔ AND THE ACTIVATING SIDE IS THE SAME LANDING (kb/Work PB640). ISO §14.2.3 GR9/GR10 put this COMPUTE
        // in the ACTIVATING runtime element ("allocated by the activating runtime element during the process of
        // initiating the activation"), so wherever the formal's description is knowable at the call site the
        // caller performs it -- and the callee's own landing must then be the IDENTITY over the result, or one
        // crossing would be landed twice and the second landing could move the value again.
        var landed = CobolArgAdapt.LandForFormal<long>(byValue[0], Wide, 9, checking: false);
        Assert.Equal(expected, ((ManagedPointer<long>)landed.Carrier).Value);
        Assert.Equal(9, landed.Scale);
        Assert.Equal(Wide.Digits, landed.Digits);
        Assert.Equal(expected, CobolArgAdapt.NumValue<long>([landed], 0, Wide, 9).Value);
        Assert.Equal(expected, CobolArgAdapt.Num<long>([landed with { Mode = CobolPassMode.Content }], 0, Wide, 9).Value);
    }

    /// <summary>⛔ THE CHECKED LANDING RAISES WHERE THE UNCHECKED ONE KEEPS LOW-ORDER DIGITS (kb/Work PB640).
    /// A crossing whose value is "further from zero than permitted for the associated resultant data item"
    /// (ISO §14.7.5 case 3) sets EC-SIZE-TRUNCATION to exist when checking is enabled (§14.7.5 no-phrase rule
    /// 4); with checking off the documented disposition (CONFORMANCE.md DOC-A.1-70) is the result's low-order
    /// digits, which the theory above pins. The SAME argument must take both, selected only by the flag -- a
    /// checked landing that also changed the VALUE, or an unchecked one that threw, would be a second rule.
    /// <para>PROVEN TO FAIL: making <c>LandForFormal</c> ignore <c>checking</c> makes the first three rows red;
    /// making it raise unconditionally makes the fourth red and DOC-A.1-70 unreachable.</para></summary>
    [Theory]
    [InlineData("Int128-10e30", true)]
    [InlineData("double-1e30", true)]
    [InlineData("Int128-18digit", true)]
    [InlineData("Int128-inrange", false)]
    public void TheCheckedLandingRaisesExactlyWhereTheValueDoesNotFit(string shape, bool raises)
    {
        (ManagedPointer Carrier, int Scale) arg = shape switch
        {
            "Int128-10e30" => (ManagedPointer<Int128>.Cell(Int128.Parse("1000000000000000000000000000000")), 0),
            "double-1e30" => (ManagedPointer<double>.Cell(1.0e30d), 0),
            "Int128-18digit" => (ManagedPointer<Int128>.Cell((Int128)123456789012345678L), 0),
            "Int128-inrange" => (ManagedPointer<Int128>.Cell((Int128)123456), 3),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        var a = new CobolArg(CobolPassMode.Value, arg.Carrier, Meta(38, arg.Scale));
        if (raises)
        {
            var ex = Assert.Throws<CobolSizeError>(() => CobolArgAdapt.LandForFormal<long>(a, Wide, 9, checking: true));
            Assert.Equal("EC-SIZE-TRUNCATION", ex.EcName);
        }
        else
        {
            var ok = CobolArgAdapt.LandForFormal<long>(a, Wide, 9, checking: true);
            Assert.Equal(123456000000L, ((ManagedPointer<long>)ok.Carrier).Value);
        }
    }

    /// <summary>The §14.9.4.4 GR11 OMITTED argument has no value to be the COMPUTE's sending operand, so the
    /// activating-side landing passes it through UNCHANGED -- including under checking, where raising for an
    /// argument that was never supplied would invent a size error out of an omission.</summary>
    [Fact]
    public void AnOmittedArgument_IsNotLanded()
    {
        var omitted = new CobolArg(CobolPassMode.Content, ManagedPointer.Null, null);
        Assert.Equal(omitted, CobolArgAdapt.LandForFormal<long>(omitted, Wide, 9, checking: true));
    }

    /// <summary>A carrier outside the six (see <see cref="ACarrierOutsideTheSix_FailsTheActivationWithArgMismatch"/>)
    /// crosses the activating side UNCHANGED rather than being reinterpreted here -- the degrade stays the one
    /// the callee-side adapter owns, written in one place.</summary>
    [Fact]
    public void ACarrierOutsideTheSix_CrossesTheActivatingSideUntouched()
    {
        var a = new CobolArg(CobolPassMode.Value, ManagedPointer<decimal>.Cell(1.5m), Meta(7, 2));
        Assert.Equal(a, CobolArgAdapt.LandForFormal<long>(a, Wide, 9, checking: true));
    }

    /// <summary>The <c>PIC S9(4)V99</c> DISPLAY description, trailing over-punch (the IBM default) — the argument
    /// AND the image-carried formal of the kb/Work PB873 probe.</summary>
    private static NumProfile SignedDisplay => new()
    {
        Digits = 6,
        FractionDigits = 2,
        Signed = true,
        SignKind = NumericSign.TrailingOverpunch,
        Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Zoned,
    };

    /// <summary>⛔ BOTH IMAGE ARMS KEEP THE OPERATIONAL SIGN (kb/Work PB873 — ISO §14.2.3 GR8 / GR9's first
    /// branch for <see cref="CobolArgAdapt.Text"/>, GR10's record "of the same description as the formal
    /// parameter" for <see cref="CobolArgAdapt.TextValue"/>). Each arm used to build its image from a hard-coded
    /// <c>Signed = false</c> profile, so <c>-12.34</c> arrived as <c>00123D</c> (+12.34) on both; the image is
    /// now the carried description's own. PROVEN TO FAIL: restoring either arm's unsigned profile makes its case
    /// red. The write-back through the GR8 view lands the sign in the caller's cell too.</summary>
    [Fact]
    public void ASignedArgument_KeepsItsSign_OnBothImageArms()
    {
        var cell = ManagedPointer<long>.Cell(-1234L);
        var byRef = new[] { new CobolArg(CobolPassMode.Reference, cell, SignedDisplay) };
        var view = CobolArgAdapt.Text(byRef, 0, 6, null, static () => "");
        Assert.Equal("00123M", view.Value);
        view.Value = "00133M";                       // the callee's SUBTRACT 1, stored through the GR8 view
        Assert.Equal(-1334L, cell.Value);

        var byValue = new[] { new CobolArg(CobolPassMode.Value, ManagedPointer<long>.Cell(-1234L), SignedDisplay) };
        Assert.Equal("00123M", CobolArgAdapt.TextValue(byValue, 0, 6, SignedDisplay, 2, null, static () => "").Value);
    }

    /// <summary>The carried description decides the image's SHAPE, not only its sign: a <c>SIGN LEADING
    /// SEPARATE</c> argument is one position longer and a <c>COMP-5</c> argument is its binary bytes (§14.2.3 GR8
    /// — the formal sees the storage). Both were an unsigned zoned digit run before kb/Work PB873.</summary>
    [Fact]
    public void TheCarriedDescription_DecidesTheImageShape()
    {
        var leadingSeparate = new NumProfile
        {
            Digits = 3, FractionDigits = 0, Signed = true, SignKind = NumericSign.LeadingSeparate,
            Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
        };
        var ls = new[] { new CobolArg(CobolPassMode.Reference, ManagedPointer<long>.Cell(-123L), leadingSeparate) };
        Assert.Equal("-123", CobolArgAdapt.Text(ls, 0, -1, null, static () => "").Value);

        var comp5 = new NumProfile
        {
            Digits = 4, FractionDigits = 0, Signed = true, SignKind = NumericSign.BinaryMinus,
            Truncation = NumericTruncation.BinaryCapacity, ByteForm = NumericByteForm.Binary, StorageLength = 2,
        };
        var c5 = new[] { new CobolArg(CobolPassMode.Reference, ManagedPointer<long>.Cell(258L), comp5) };
        Assert.Equal("\u0001\u0002", CobolArgAdapt.Text(c5, 0, 2, null, static () => "").Value);
    }
}
