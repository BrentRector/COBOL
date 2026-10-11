// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATE THAT HOLDS <see cref="PictureComposition"/>'s COPY OF TABLE 10 EQUAL TO THE STANDARD.
/// <para>
/// The composition validator's whole design bet (kb/Work PB528; data-model design D24) is that ISO §13.18.40.6's
/// Table 10 belongs in the compiler as DATA rather than as hand-written positional <c>if</c>s — that is what makes
/// SR25 and SR26 need no code of their own and what makes the NEXT precedence question automatic. A 24×24 table
/// nobody re-derives is a snapshot of what someone read once, so this re-parses the table out of
/// <c>specs/ISO_COBOL.md</c> on every build and compares all 576 cells. The markdown transcription was itself
/// verified cell-for-cell against the canonical PDF (printed folios 459-460) by geometry, so the chain from the
/// printed standard to the array is closed.
/// </para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class PictureTable10DriftTests
{
    /// <summary>Parse Table 10 out of the transcription: the data rows are the markdown rows whose trailing 24
    /// cells are all empty or a lone <c>x</c> (the header rows carry the group labels and symbol names).</summary>
    private static List<string[]> ParseTable10()
    {
        string[] src = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(src, l => l.StartsWith("**Table 10 ", StringComparison.Ordinal));
        Assert.True(start >= 0, "specs/ISO_COBOL.md no longer carries a '**Table 10 …**' caption");
        var rows = new List<string[]>();
        for (int i = start; i < src.Length && i < start + 60; i++)
        {
            string l = src[i].Trim();
            if (!l.StartsWith('|')) continue;
            var cells = l.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length < 25) continue;
            var tail = cells[^24..];
            if (!tail.All(c => c.Length == 0 || c is "x" or "X")) continue;
            rows.Add(tail.Select(c => c.Length == 0 ? "." : "x").ToArray());
        }
        return rows;
    }

    /// <summary>Every cell of the compiler's table is the cell the standard prints.</summary>
    [Fact]
    public void Table10_InTheCompiler_MatchesTheTranscription()
    {
        var spec = ParseTable10();
        Assert.Equal(24, spec.Count);
        Assert.Equal(24, PictureComposition.Table10Rows.Length);
        for (int r = 0; r < 24; r++)
        {
            var code = PictureComposition.Table10Rows[r].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.True(code.Length == 24, $"PictureComposition.Table10Rows[{r}] has {code.Length} cells, expected 24");
            Assert.True(spec[r].SequenceEqual(code),
                $"ISO §13.18.40.6 Table 10 row {r} ({(PicRole)r}) no longer matches specs/ISO_COBOL.md:\n"
                + $"  spec: {string.Join(' ', spec[r])}\n  code: {string.Join(' ', code)}");
        }
    }

    /// <summary>
    /// The table's POPULATION, asserted before the comparison above is believed. A parse that produced 24 rows of
    /// all-blank cells would satisfy an equality test against an all-blank array while forbidding every picture;
    /// a MISSING observation is not a NEGATIVE one (<c>feedback_verdict_evidence_invariant</c>). The printed table
    /// carries 163 'x' marks, counted from the PDF's own glyphs.
    /// </summary>
    [Fact]
    public void Table10_IsPopulated_AndAgreesWithTheGlyphCount()
    {
        var spec = ParseTable10();
        int marks = spec.Sum(r => r.Count(c => c == "x"));
        Assert.Equal(163, marks);
        int packed = PictureComposition.MayPrecede.Sum(m => System.Numerics.BitOperations.PopCount((uint)m));
        Assert.Equal(marks, packed);
        // Two rows are entirely blank in the printed table and both are load-bearing: nothing may precede a
        // LEADING sign (which is how SR25's "leftmost" half binds) and nothing may precede 'S' (SR18).
        Assert.Equal(0, PictureComposition.MayPrecede[(int)PicRole.SignLeading]);
        Assert.Equal(0, PictureComposition.MayPrecede[(int)PicRole.OperationalSign]);
        // …and three COLUMNS are entirely blank, which is how "shall be the LAST symbol" binds without a rule:
        foreach (var last in new[] { PicRole.SignTrailing, PicRole.CrDb })
            Assert.True(PictureComposition.MayPrecede.All(m => (m & (1 << (int)last)) == 0),
                $"the {last} column is no longer blank — nothing may FOLLOW a trailing sign or CR/DB");
    }

    /// <summary>The enum and the array index the same 24 symbols in the standard's printed order.</summary>
    [Fact]
    public void PicRole_CoversTable10Exactly()
    {
        var values = Enum.GetValues<PicRole>();
        Assert.Equal(24, values.Length);
        for (int i = 0; i < 24; i++) Assert.Equal(i, (int)values[i]);
    }
}
