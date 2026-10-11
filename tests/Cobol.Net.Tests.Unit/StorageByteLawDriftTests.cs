// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Common;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ A BYTE BECOMES A CHARACTER IN EXACTLY ONE PLACE, AND HIGH-VALUE IS ANSWERED BY EXACTLY ONE MEMBER
/// (kb/Work PB1759; owner decisions R51/R52; design COBOLNET_FILES_DESIGN D29). The storage-byte law — byte 0xFF is
/// the character U+FFFF, every other byte b is U+00bb — lives in <c>CobolNet.Runtime.StorageByte</c>, and the native
/// HIGH-VALUE in <c>NativeCollatingSequence</c>. A new <c>(char)(byte)…</c> cast in the compiler or runtime images a
/// byte outside the law (a binary 0xFF would become U+00FF and <c>IF REC = HIGH-VALUES</c> would turn false after
/// <c>MOVE HIGH-VALUES TO REC</c>), and a U+00FF character literal is the retired Latin-1 HIGH-VALUE pin coming back
/// (kb/Work PB1093). Both are refused here, so the next byte-form codec or figurative site is routed through the one
/// law by construction.</summary>
[Trait("Sweep", "tree")]
public sealed class StorageByteLawDriftTests
{
    private static readonly Regex ByteToCharCast = new(@"\(char\)\s*\(byte\)", RegexOptions.Compiled);
    private static readonly Regex LatinHighValuePin =
        new(@"'\\u00[fF][fF]'|'\u00FF'|\(char\)\s*0x[fF][fF]\b|\(char\)\s*255\b", RegexOptions.Compiled);

    private static IEnumerable<(string File, int Line, string Text)> Hits(Regex rx)
    {
        foreach (string project in new[] { "Cobol.Net.Runtime", "Cobol.Net.Compiler", "Cobol.Net.Frontend" })
            foreach (string file in Directory.EnumerateFiles(TestRepo.Src(project), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/obj/") || file.Replace('\\', '/').Contains("/bin/")) continue;
                if (Path.GetFileName(file) == "StorageByte.cs") continue;
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
    public void NoByteIsImagedAsACharacterOutsideTheStorageByteLaw()
    {
        var hits = Hits(ByteToCharCast).ToList();
        Assert.True(hits.Count == 0, "image the byte through StorageByte.ToChar, never (char)(byte):\n"
            + string.Join("\n", hits.Select(h => $"{h.File}:{h.Line}: {h.Text}")));
    }

    /// <summary>⛔ A HEXADECIMAL ALPHANUMERIC LITERAL IS A BYTE-TO-CHARACTER SITE TOO (kb/Work PB2919). X"hh" is the
    /// character whose DISPLAY byte is hh (§8.3.3.2.4 GR4), so it is imaged by the same law as a binary item's
    /// storage: X"FF" is U+FFFF, which makes <c>IF GX = X"FFFFFFFE"</c> true for the REDEFINES view of a binary -2.
    /// Every decoder of the pair form (the literal, the COPY library-text name's digits) is this one mapping.</summary>
    [Fact]
    public void HexAlphanumericLiteralPairsAreImagedByTheStorageByteLaw()
    {
        for (int b = 0; b <= 0xFF; b++)
        {
            string expected = StorageByte.ToChar((byte)b).ToString();
            Assert.Equal(expected, CobolLiteral.DecodeHex($"X\"{b:X2}\""));
            Assert.Equal(expected, CobolLiteral.DecodeHex($"X'{b:x2}'"));
            Assert.Equal(expected, CobolLiteral.Decode($"X\"{b:X2}\""));
            Assert.Equal(expected, CobolLiteral.DecodeHexDigits($"{b:X2}"));
        }
        Assert.Equal(NativeCollatingSequence.HighValue, CobolLiteral.DecodeHex("X\"FF\"")[0]);
        Assert.Equal("￿￿￿þ", CobolLiteral.DecodeHex("X\"FFFFFFFE\""));
        Assert.Equal(StorageByte.ToChars([0xFF, 0xFF, 0xFF, 0xFE]), CobolLiteral.DecodeHex("X\"FFFFFFFE\""));
        Assert.Null(CobolLiteral.DecodeHexDigits("F"));
        Assert.Null(CobolLiteral.DecodeHexDigits("ZZ"));
    }

    /// <summary>A national hexadecimal group is a UTF-16 code unit (§8.3.3.5.4 GR4), not a byte: NX"00FF" stays U+00FF.</summary>
    [Fact]
    public void HexNationalLiteralGroupsAreCodeUnitsNotBytes()
    {
        Assert.Equal("ÿ", CobolLiteral.Decode("NX\"00FF\""));
    }

    [Fact]
    public void TheRetiredU00FFHighValuePinIsNotSpelledAnywhere()
    {
        var hits = Hits(LatinHighValuePin).ToList();
        Assert.True(hits.Count == 0, "HIGH-VALUE is NativeCollatingSequence.HighValue (U+FFFF); a U+00FF char literal is the retired pin:\n"
            + string.Join("\n", hits.Select(h => $"{h.File}:{h.Line}: {h.Text}")));
    }
}
