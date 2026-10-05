// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ A BINARY32 RECEIVER NEVER TAKES A VALUE THAT HAS ALREADY BEEN ROUNDED TO BINARY64 (kb/Work PB1110).
/// ISO §14.6.8.3 rule 2 converts the algebraic value "in a manner consistent with the specifications of ISO/IEC
/// 60559:2020" — one correctly rounded conversion. A <c>(float)</c> cast of a value that a binary64 conversion
/// produced is a SECOND rounding: a decimal a hair above a binary32 midpoint lands ON the midpoint in binary64 and
/// the cast ties to even, storing the lower neighbour. The conversions that exist for the job are
/// <c>CobolFloat.ScaledToSingle</c> / <c>CobolDec.ToSingle</c> (a decimal or fixed-point sender),
/// <c>CobolIntrinsics.NumvalFSingle</c> (a text) and <c>FloatResultant</c> (an arithmetic resultant); the landings
/// that found the defect were MOVE (the first), then ACCEPT's device transfer. This sweep refuses the shape at any
/// site that spells it, so the next landing is routed through the one conversion by construction.
/// <para>A binary64 SENDER narrowed to binary32 is a single rounding of the value the sender holds and is not the
/// shape: it never mentions a decimal-to-binary64 converter inside the cast, so it is not matched.</para></summary>
public sealed class Binary32SingleRoundingDriftTests
{
    /// <summary>A <c>(float)</c> cast whose operand text names a decimal→binary64 converter.</summary>
    private static readonly Regex NarrowedDecimalConversion = new(
        @"\(float\)[^;]*\b(ScaledToDouble|ToDouble\(\)|NumvalFDouble|double\.Parse)", RegexOptions.Compiled);

    private static IEnumerable<(string File, int Line, string Text)> Hits(Regex rx)
    {
        foreach (string project in new[] { "Cobol.Net.Runtime", "Cobol.Net.Compiler" })
            foreach (string file in Directory.EnumerateFiles(TestRepo.Src(project), "*.cs", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                if (path.Contains("/obj/") || path.Contains("/bin/")) continue;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string code = lines[i].TrimStart();
                    if (code.StartsWith("//", StringComparison.Ordinal)) continue;
                    if (rx.IsMatch(code)) yield return (file, i + 1, code);
                }
            }
    }

    [Fact]
    public void NoFloatCastNarrowsADecimalSendersBinary64()
    {
        var hits = Hits(NarrowedDecimalConversion).ToList();
        Assert.True(hits.Count == 0,
            "convert a decimal/fixed/text value to binary32 through CobolFloat.ScaledToSingle / CobolDec.ToSingle / "
            + "CobolIntrinsics.NumvalFSingle, never a (float) cast of its binary64 (ISO §14.6.8.3 rule 2):\n"
            + string.Join("\n", hits.Select(h => $"{h.File}:{h.Line}: {h.Text}")));
    }

    /// <summary>The sweep must be able to fail: the regex matches the exact shape the defect had, so a rename of
    /// the converters cannot silently turn the test into a no-op.</summary>
    [Theory]
    [InlineData("return (float)src.ToDouble();", true)]
    [InlineData("float r = (float)CobolFloat.ScaledToDouble(u, s);", true)]
    [InlineData("w.Line($\"(float)({x}).ToDouble()\");", true)]
    [InlineData("float r = (float)src;", false)]
    [InlineData("return CobolFloat.ScaledToSingle(u, s);", false)]
    public void TheSweepMatchesTheDefectShapeAndNothingElse(string line, bool expected) =>
        Assert.Equal(expected, NarrowedDecimalConversion.IsMatch(line));
}
