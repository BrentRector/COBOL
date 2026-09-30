// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
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
public sealed class StorageByteLawDriftTests
{
    private static readonly Regex ByteToCharCast = new(@"\(char\)\s*\(byte\)", RegexOptions.Compiled);
    private static readonly Regex LatinHighValuePin =
        new(@"'\\u00[fF][fF]'|'\u00FF'|\(char\)\s*0x[fF][fF]\b|\(char\)\s*255\b", RegexOptions.Compiled);

    private static IEnumerable<(string File, int Line, string Text)> Hits(Regex rx)
    {
        foreach (string project in new[] { "Cobol.Net.Runtime", "Cobol.Net.Compiler" })
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

    [Fact]
    public void TheRetiredU00FFHighValuePinIsNotSpelledAnywhere()
    {
        var hits = Hits(LatinHighValuePin).ToList();
        Assert.True(hits.Count == 0, "HIGH-VALUE is NativeCollatingSequence.HighValue (U+FFFF); a U+00FF char literal is the retired pin:\n"
            + string.Join("\n", hits.Select(h => $"{h.File}:{h.Line}: {h.Text}")));
    }
}
