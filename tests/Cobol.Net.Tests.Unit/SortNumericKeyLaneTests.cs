// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ A NUMERIC SORT KEY ORDERS BY ITS ALGEBRAIC VALUE ACROSS ITS WHOLE CONTAINER (kb/Work PB186).
/// ISO §14.9.40.4 GR8 / GR19 compare key data items "according to the rules for comparison of operands in a
/// relation condition" — for numeric operands §8.8.4.2.4's algebraic value. An UNSIGNED 16-byte binary key holds
/// [0, 2^128), and the signed Int128 decode lane returns every value at or above 2^127 as a NEGATIVE number, so
/// both SORT forms need the unsigned lane: the runtime key column (Format 1 and MERGE) and the emitted table
/// comparer (Format 2), which now decodes through the ONE windowed reader instead of a private copy.</summary>
public sealed class SortNumericKeyLaneTests
{
    private static NumProfile Binary(int width, bool signed) => new()
    {
        Digits = 31, FractionDigits = 0, Signed = signed, Truncation = NumericTruncation.BinaryCapacity,
        ByteForm = NumericByteForm.Binary, StorageLength = width,
    };

    /// <summary>The predicate is the IMAGE's: only an unsigned radix-2 image of 16 bytes exceeds Int128. An 8-byte
    /// unsigned image fits Int128 whole (ParseImage returns it non-negative); a signed one orders by its sign.</summary>
    [Theory]
    [InlineData(16, false, true)]
    [InlineData(16, true, false)]
    [InlineData(8, false, false)]
    public void ImageExceedsInt128_IsTheUnsignedSixteenByteImage(int width, bool signed, bool expected) =>
        Assert.Equal(expected, Binary(width, signed).ImageExceedsInt128);

    [Theory]
    [InlineData(false, new[] { "SEV", "HI8", "ALL" })]
    [InlineData(true, new[] { "ALL", "HI8", "SEV" })]
    public void UnsignedWideBinaryKey_OrdersByValue_AboveTwoToThe127(bool descending, string[] expected)
    {
        const string name = "PB186-UNIT-SD";
        static string Rec(byte[] key, string tag) => new string(key.Select(b => (char)b).ToArray()) + tag;
        var seven = new byte[16];
        seven[15] = 7;
        CobolSort.Init(name);
        CobolSort.Release(name, Rec(Enumerable.Repeat((byte)0xFF, 16).ToArray(), "ALL"), 0, 0, int.MaxValue);   // 2^128 - 1
        CobolSort.Release(name, Rec(seven, "SEV"), 0, 0, int.MaxValue);
        CobolSort.Release(name, Rec(Enumerable.Repeat((byte)0x80, 16).ToArray(), "HI8"), 0, 0, int.MaxValue);   // >= 2^127
        var keys = new[] { new CobolSort.Key(0, 16, descending, CobolSort.KeyClass.Numeric, Binary(16, signed: false)) };
        CobolSort.Sort(name, keys, duplicatesInOrder: true);
        var order = new List<string>();
        while (CobolSort.Return(name, out string rec)) order.Add(rec[16..]);
        CobolSort.Close(name);
        Assert.Equal(expected, order);
    }

    /// <summary>The source half: no CodeGen file but the ones named here decodes a numeric IMAGE through a
    /// runtime lane of its own. <c>NumericRenderer.WindowedNum</c> is THE reader of a windowed leaf's value;
    /// the table-SORT comparer used to carry a private SIGNED-only copy of it (kb/Work PB186), so a future
    /// lane added to the one reader never reached it. Each other file listed decodes for a different job and
    /// says why at its call site: the codecs that BUILD the image, the alphanumeric-rendering channel (whose
    /// own three-lane dispatch is <c>OperandText.NonTextBytes</c>), and two STORE-side re-derivations
    /// (INSPECT's sign, SET's INDEX augment) that are not sending reads of a numeric value, and the OPTIONS INITIALIZE
    /// background (InitialStateBackground), which seeds a native float carrier from its fill image (kb/Work PB1134).</summary>
    [Fact]
    public void OnlyTheNamedSites_DecodeANumericImage()
    {
        string codegen = TestRepo.Src("Cobol.Net.Compiler", "CodeGen");
        Assert.True(Directory.Exists(codegen), $"CodeGen moved: {codegen} is not a directory.");
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "RuntimeApi.cs", "NumericRenderer.cs", "OperandText.cs", "GroupImageCodec.cs", "InspectEmitter.cs",
            "SetEmitter.cs", "InitialStateBackground.cs",
        };
        var decode = new Regex(@"\bNumParseImage(U128|Float|U)?\(", RegexOptions.Compiled);
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(codegen, "*.cs", SearchOption.AllDirectories))
        {
            if (allowed.Contains(Path.GetFileName(file))) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;
                if (decode.IsMatch(code)) offenders.Add($"{Path.GetRelativePath(codegen, file)}:{i + 1}: {code}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A numeric image is decoded outside the named sites — read a windowed leaf's value through "
            + "NumericRenderer.WindowedNum (kb/Work PB186):\n" + string.Join("\n", offenders));
    }
}
