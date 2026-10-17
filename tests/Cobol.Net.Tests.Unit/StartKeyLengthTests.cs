// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB357 — §14.9.41.4 GR14: "If arithmetic-expression-1 does not evaluate to a positive nonzero integer that
/// is less than or equal to the length of the associated key, the I-O status value in the file connector
/// referenced by file-name-1 is set to '23'". The predicate is asked of the value the expression PRODUCED, so the
/// emitter must hand the connector that value (a scaled Int128 + scale, or a double) and never an
/// <c>(int)(Align(…, 0))</c> narrowing of it. The behaviour is also pinned end to end by the corpus golden
/// <c>conformance-suite/cases/9.1.13.5-Invalid-key-condition-with-unsuccessful-completion/pb357_start_length_not_integer.cob</c>.
/// </summary>
public sealed class StartKeyLengthTests
{
    [Theory]
    [InlineData(2, 0, 4, true, 2)]          // LENGTH 2 on a 4-position key
    [InlineData(40, 1, 4, true, 4)]         // 4.0 — integral at scale 1
    [InlineData(25, 1, 4, false, 0)]        // 2.5 — not an integer
    [InlineData(15, 1, 4, false, 0)]        // 3 / 2 = 1.5
    [InlineData(0, 0, 4, false, 0)]         // zero is not positive
    [InlineData(-1, 0, 4, false, 0)]        // negative
    [InlineData(5, 0, 4, false, 0)]         // beyond the key
    [InlineData(4294967297, 0, 4, false, 0)] // the 32-bit wrap to 1 the old narrowing produced
    public void ScaledLane_AnswersGr14OnTheExactValue(long scaled, int scale, int key, bool ok, int expected)
    {
        Assert.Equal(ok, StartKeyLength.OfScaled(scaled, scale, 1).TryCompareLength(key, out int len));
        Assert.Equal(expected, len);
    }

    [Fact]
    public void ScaledLane_HugeIntegerIsOutOfRange_NotWrapped()
    {
        var huge = StartKeyLength.OfScaled(Int128.MaxValue, 0, 2);   // no product is formed before the bound
        Assert.False(huge.TryCompareLength(8, out _));
    }

    [Theory]
    [InlineData(2.0, true, 2)]
    [InlineData(2.5, false, 0)]
    [InlineData(double.NaN, false, 0)]
    [InlineData(double.PositiveInfinity, false, 0)]
    [InlineData(1e30, false, 0)]
    public void RealLane_AnswersGr14OnTheDouble(double v, bool ok, int expected)
    {
        Assert.Equal(ok, StartKeyLength.OfReal(v, 1).TryCompareLength(4, out int len));
        Assert.Equal(expected, len);
    }

    [Fact]
    public void NationalCount_IsPositions_ScaledToStorageAfterTheTest()
    {
        // §14.9.41.4 GR13 — a count of national character positions; a 4-position national key is 8 storage units.
        Assert.True(StartKeyLength.OfScaled(3, 0, 2).TryCompareLength(8, out int len));
        Assert.Equal(6, len);
        Assert.False(StartKeyLength.OfScaled(5, 0, 2).TryCompareLength(8, out _));
    }

    [Fact]
    public void DefaultWidth_IsDataName1sLength()
    {
        Assert.True(StartKeyLength.OfWidth(4).TryCompareLength(4, out int len));
        Assert.Equal(4, len);
    }

    // ── drift: the emit site must not decide GR14's answer ────────────────────────────────────────────────────

    /// <summary>True when the START emitter narrows the WITH LENGTH count to an int before the connector sees it.</summary>
    internal static bool EmitStartNarrowsTheLength(string emitterSource)
    {
        int at = emitterSource.IndexOf("public void EmitStart(", StringComparison.Ordinal);
        if (at < 0) return true;   // a missing site is not evidence of a correct one
        int end = emitterSource.IndexOf("\n    }\n", at, StringComparison.Ordinal);
        string body = emitterSource[at..(end < 0 ? emitterSource.Length : end)];
        return body.Contains("(int)(", StringComparison.Ordinal) || !body.Contains("StartKeyLength", StringComparison.Ordinal);
    }

    [Fact]
    public void EmitStart_CarriesTheCountUnnarrowed()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "KeyedIoEmitter.cs"))
            .Replace("\r\n", "\n");
        Assert.False(EmitStartNarrowsTheLength(src),
            "KeyedIoEmitter.EmitStart must hand the WITH LENGTH count to the connector as a StartKeyLength built from "
            + "the exact lane (RuntimeApi.StartKeyLengthScaled / StartKeyLengthReal), never `(int)(Align(…, 0))`: "
            + "§14.9.41.4 GR14 is a predicate on arithmetic-expression-1 itself (kb/Work PB357).");
        // ⛔ the failure proof — the PB357 shape must be caught.
        const string defective = "    public void EmitStart(BoundKeyedStart sta)\n    {\n"
            + "        string len = $\"{natBytes} * (int)({NumericRenderer.Align(num.Render(le, ReceiverContext.None), 0)})\";\n    }\n";
        Assert.True(EmitStartNarrowsTheLength(defective));
    }
}
