// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The ISO §15.4 returned-value maximum (Annex A.1 item 93, docs/CONFORMANCE.md row <c>DOC-A.1-93</c>; kb/Work
/// PB2631): ONE number, <see cref="CobolIntrinsics.ReturnedValueMaximum"/>, asked through ONE guard by every function
/// whose returned value can outgrow its arguments. The determination is the carrier's ceiling, so it is the SAME number
/// as row <c>DOC-A.1-62</c>'s dynamic-length maximum, and the 8,191-position literal maximum is not a returned-value
/// bound. These run with EC-ARGUMENT-FUNCTION checking off, so a raise surfaces as the documented zero-length value.
/// </summary>
public sealed class ReturnedValueMaximumTests
{
    /// <summary>The published determination IS the enforced number, so the two cannot drift apart.</summary>
    [Fact]
    public void TheMaximum_IsTheNumberRowDocA193Publishes()
    {
        string register = File.ReadAllText(TestRepo.Docs("CONFORMANCE.md"));
        string row = register.Split('\n').SingleOrDefault(l => l.StartsWith("| DOC-A.1-93 |", StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException("docs/CONFORMANCE.md §7 carries no `| DOC-A.1-93 |` row: Annex A.1 "
                                                  + "item 93 (returned value length) is a REQUIRED and DOCUMENTED element.");
        Assert.Contains(CobolIntrinsics.ReturnedValueMaximum.ToString("N0", CultureInfo.InvariantCulture), row, StringComparison.Ordinal);
        Assert.Equal(CobolDynString.MaxLength, CobolIntrinsics.ReturnedValueMaximum);
    }

    /// <summary>Past the literal maximum and inside the returned-value maximum: a whole value.</summary>
    [Fact]
    public void PastTheLiteralMaximum_ReturnsTheWholeValue()
    {
        Assert.Equal(8192, CobolIntrinsics.BooleanOfInteger(5, 8192).Length);
        Assert.Equal(10_000, CobolIntrinsics.Concat(new string('a', 5000), new string('b', 5000)).Length);
        Assert.Equal(8192, CobolIntrinsics.BaseConvert(new string('F', 2048), 16, 2).Length);
    }

    /// <summary>One position past the maximum is §15.4's EC-ARGUMENT-FUNCTION and row DOC-A.1-93's zero-length value,
    /// answered before the value is allocated (a boolean item of argument-2 positions, §15.13.4 r1).</summary>
    [Fact]
    public void OnePastTheMaximum_IsTheZeroLengthValue()
    {
        Assert.Equal("", CobolIntrinsics.BooleanOfInteger(5, CobolIntrinsics.ReturnedValueMaximum + 1L));
    }
}
