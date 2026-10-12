// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using CobolNet.Binding;
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB82 — the RESULTANT-line → source-origin map the preprocessing chain builds (fixed-form continuation
/// joins, COPY incorporation, REPLACE, the NIST archive-marker strip) and the diagnostic CURSOR the binder-side
/// <see cref="EditionContext"/> stamps onto every diagnostic reported while a walker has it positioned.
/// </summary>
public sealed class SourceLineMapTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_LineMap_" + Guid.NewGuid().ToString("N")[..8]);

    public SourceLineMapTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ } }

    private void Copybook(string name, string content) => File.WriteAllText(Path.Combine(_dir, name), content);

    private static int[] LinesOf(MappedText m) => m.Lines.Select(o => o.Origin.Line).ToArray();

    // ── MappedText / OriginWriter primitives ──────────────────────────────────────────────────────────────

    [Fact]
    public void MappedText_AssertsOneOriginPerLinePiece()
    {
        var ok = MappedText.Identity("a\nb\nc", "f");
        Assert.Equal([1, 2, 3], LinesOf(ok));
        Assert.Equal("f(2,1)", ok.LocateAt(2).ToString());   // position 2 = the 'b' piece, its first column
        Assert.Throws<InvalidOperationException>(() => new MappedText("a\nb", [new LineOrigin(new SourceOrigin("f", 1))]));
    }

    [Fact]
    public void OriginWriter_EachOutputPieceTakesTheOriginOfItsFirstContent()
    {
        var w = new OriginWriter();
        w.Append("head ".AsSpan(), new LineOrigin(new SourceOrigin("main", 5)));
        w.NewLine(new LineOrigin(new SourceOrigin("main", 5)));
        w.AppendMapped(MappedText.Identity("c1\nc2", "cpy"));
        w.NewLine(new LineOrigin(new SourceOrigin("main", 5)));
        w.Append("tail".AsSpan(), new LineOrigin(new SourceOrigin("main", 6)));
        var m = w.Finish();
        Assert.Equal("head \nc1\nc2\ntail", m.Text);
        Assert.Equal([("main", 5), ("cpy", 1), ("cpy", 2), ("main", 6)], m.Lines.Select(o => (o.Origin.File, o.Origin.Line)).ToArray());
        var slice = m.Slice(6, 5);   // "c1\nc2"
        Assert.Equal([("cpy", 1), ("cpy", 2)], slice.Lines.Select(o => (o.Origin.File, o.Origin.Line)).ToArray());
    }

    // ── kb/Work PB1801: a resultant line maps to its source COLUMN by COLUMN ─────────────────────────────────

    [Fact]
    public void LineOrigin_LocatesEachStretchAtItsOwnLineAndColumn()
    {
        // "01 A VALUE 'xy" (line 5, program text from physical column 7) joined with "z'." (line 6, from column 12)
        var o = new LineOrigin([new OriginSpan(0, new SourceOrigin("p", 5), 7), new OriginSpan(14, new SourceOrigin("p", 6), 12)]);
        Assert.Equal(new SourceOrigin("p", 5), o.Origin);                  // the piece is named by its first stretch
        Assert.Equal("p(5,8)", o.Locate(0).ToString());
        Assert.Equal("p(5,21)", o.Locate(13).ToString());
        Assert.Equal("p(6,13)", o.Locate(14).ToString());                  // the joined fragment: its own line and column
        Assert.Equal("p(6,15)", o.Locate(16).ToString());
        Assert.Throws<ArgumentException>(() => new LineOrigin([new OriginSpan(3, new SourceOrigin("p", 1), 0), new OriginSpan(3, new SourceOrigin("p", 2), 0)]));
    }

    [Fact]
    public void OriginWriter_ASliceKeepsItsColumns_AndWrittenTextStandsAtThePointItReplaced()
    {
        var input = MappedText.Identity("MOVE OLD-NAME TO X.", "r.cob");
        var w = new OriginWriter();
        w.AppendSlice(input, 0, 5);                             // "MOVE "
        w.Append("NEW".AsSpan(), input.PointAt(5));             // a REPLACE replacement for OLD-NAME
        w.AppendSlice(input, 13, input.Text.Length - 13);       // " TO X."
        var m = w.Finish();
        Assert.Equal("MOVE NEW TO X.", m.Text);
        Assert.Equal("r.cob(1,6)", m.LocateAt(5).ToString());   // NEW stands where OLD-NAME was written
        Assert.Equal("r.cob(1,18)", m.LocateAt(12).ToString()); // X keeps its own column, past the longer text replaced
        Assert.Equal("r.cob(1,18)", m.Slice(12, 2).LocateAt(0).ToString());   // a slice begun mid-line is rebased
    }

    [Fact] // kb/Work PB3031: written text counts a CHARACTER POSITION per character (ISO §6.1 1) a)), not a UTF-16 unit
    public void OriginWriter_WrittenTextWithSupplementaryCharacters_CountsOnePositionPerCharacter()
    {
        string wide = "\U0001000D\U0001000E";
        var input = MappedText.Identity("MOVE OLD TO X.", "r.cob");
        var w = new OriginWriter();
        w.AppendSlice(input, 0, 5);
        w.Append((wide + " B" + wide + "C").AsSpan(), input.PointAt(5));   // 2 + 1 + 1 + 2 + 1 = 7 positions, 11 units
        w.AppendSlice(input, 8, input.Text.Length - 8);
        var m = w.Finish();
        Assert.Equal("MOVE " + wide + " B" + wide + "C TO X.", m.Text);
        Assert.Equal("r.cob(1,6)", m.LocateAt(5).ToString());    // the first letter: the replaced word's position
        Assert.Equal("r.cob(1,6)", m.LocateAt(6).ToString());    // its second unit: the same position
        Assert.Equal("r.cob(1,7)", m.LocateAt(7).ToString());    // the second letter
        Assert.Equal("r.cob(1,9)", m.LocateAt(10).ToString());   // B: the two letters and the blank stand at positions 6-8
        Assert.Equal("r.cob(1,10)", m.LocateAt(11).ToString());  // the first letter of the second pair
        Assert.Equal("r.cob(1,12)", m.LocateAt(15).ToString());  // C: seven positions in all
        Assert.Equal("r.cob(1,14)", m.LocateAt(m.Text.Length - 1).ToString());   // the final '.' keeps its own column
    }

    /// <summary>⛔ No consumer can reach a column-less origin of a POSITION (kb/Work PB1801): MappedText answers a position
    /// with a full <see cref="SourceLocation"/> (or a <see cref="LineOrigin"/> to write at), never a bare
    /// <see cref="SourceOrigin"/>, so a diagnostic cannot again print the column of the resultant text.</summary>
    [Fact]
    public void MappedText_ExposesNoColumnlessOriginOfAPosition()
    {
        var bare = typeof(MappedText).GetMethods().Where(m => m.ReturnType == typeof(SourceOrigin)).Select(m => m.Name).ToArray();
        Assert.Empty(bare);
    }

    // ── the normalizer: a fixed-form continuation JOINS lines; the head line's number survives ────────────

    [Fact]
    public void FixedForm_ContinuationJoin_KeepsTheHeadLineOrigin()
    {
        string src =
            "000100 IDENTIFICATION DIVISION.\n" +
            "000200 PROGRAM-ID. X.\n" +
            "000300 DATA DIVISION.\n" +
            "000400 WORKING-STORAGE SECTION.\n" +
            "000500 01 A PIC X(20) VALUE \"abc\n" +
            "000600-    \"def\".\n" +
            "000700 01 B PIC X.\n";
        var m = ReferenceFormatProcessor.NormalizeToFreeFormMapped(src, null, "p.cob");
        var lines = m.Text.Split('\n');
        int bIndex = Array.FindIndex(lines, l => l.Contains("01 B PIC X"));
        Assert.True(bIndex >= 0, m.Text);
        Assert.Equal(7, m.Lines[bIndex].Origin.Line);           // the line AFTER the join is still physical line 7
        int aIndex = Array.FindIndex(lines, l => l.Contains("01 A PIC"));
        Assert.Equal(5, m.Lines[aIndex].Origin.Line);           // the joined line keeps its head line
        // and IS joined — the short continued line read as space-filled to margin R, so positions 33–72 are part of
        // the literal (§6.3.5 "any spaces at the end of the fixed-form continued line are part of the literal";
        // DOC-A.1-157; kb/Work PB1491)
        Assert.Contains("\"abc" + new string(' ', 40) + "def\"", lines[aIndex]);
    }

    // ── COPY: the main text keeps its lines, the copied text names the copybook, the text after resumes ──

    [Fact]
    public void Copy_MapsMainAndCopybookLines()
    {
        Copybook("three.cpy", "01 C1 PIC X.\n01 C2 PIC X.\n01 C3 PIC X.\n");
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, dialectLevel: 2023, permissive: false);
        var main = MappedText.Identity("01 A PIC X.\nCOPY three.\n01 B PIC X.\n", "t.cob");
        var m = ConditionalCompilationProcessor.ProcessWithCopyMapped(main, copy,
            CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        Assert.False(bag.HasErrors, string.Join("\n", bag.Diagnostics));
        var lines = m.Text.Split('\n');
        int b = Array.FindIndex(lines, l => l.Contains("01 B PIC X"));
        Assert.Equal(("t.cob", 3), (m.Lines[b].Origin.File, m.Lines[b].Origin.Line));
        int c2 = Array.FindIndex(lines, l => l.Contains("01 C2"));
        // The path is the one the search FOUND: DOC-A.1-40 tries `.CPY` before `.cpy`, and on a case-insensitive file
        // system (Windows) `three.CPY` is this same file (kb/Work PB1355).
        Assert.EndsWith("three.cpy", m.Lines[c2].Origin.File, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, m.Lines[c2].Origin.Line);
        int a = Array.FindIndex(lines, l => l.Contains("01 A PIC X"));
        Assert.Equal(("t.cob", 1), (m.Lines[a].Origin.File, m.Lines[a].Origin.Line));
    }

    [Fact]
    public void Replace_StatementLinesVanish_FollowingLinesKeepTheirSourceNumbers()
    {
        // REPLACE is Step 3 of the merged text-manipulation driver (ISO §7.2.1), run over the expanded group.
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, dialectLevel: 2023, permissive: false);
        var m = ConditionalCompilationProcessor.ProcessWithCopyMapped(
            MappedText.Identity("01 A PIC X.\nREPLACE ==A== BY ==B==.\n01 C PIC X.\n01 A PIC X.\n", "t.cob"),
            copy, CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        var lines = m.Text.Split('\n');
        Assert.DoesNotContain(lines, l => l.Contains("REPLACE"));
        int c = Array.FindIndex(lines, l => l.Contains("01 C PIC X"));
        Assert.Equal(3, m.Lines[c].Origin.Line);
        int b = Array.FindLastIndex(lines, l => l.Contains("01 B PIC X"));   // the replaced A → B on source line 4
        Assert.Equal(4, m.Lines[b].Origin.Line);
    }

    // ── the cursor + the prefix ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EditionContext_StampsTheCursorThroughTheMap_AndBareWithoutOne()
    {
        var ed = new EditionContext(2023)
        {
            SourceFile = "main.cob",
            LineMap = new SourceLineMap([new(new SourceOrigin("main.cob", 1)), new(new SourceOrigin("main.cob", 2)), new(new SourceOrigin("lib.cpy", 7)), new(new SourceOrigin("main.cob", 4))]),
        };
        ed.Error("COBOLNET9999", "bare");
        Assert.Equal("error COBOLNET9999: bare", ed.Diagnostics[^1]);
        using (ed.At(3, 11))
        {
            ed.Error("COBOLNET9999", "in the copybook");
            Assert.Equal("lib.cpy(7,12): error COBOLNET9999: in the copybook", ed.Diagnostics[^1]);
            using (ed.At(4, 0))
            {
                ed.Warning("COBOLNET9998", "nested");
                Assert.Equal("main.cob(4,1): warning COBOLNET9998: nested", ed.Warnings[^1]);
            }
            ed.Error("COBOLNET9999", "restored");   // the inner scope restored the outer position
            Assert.StartsWith("lib.cpy(7,12): ", ed.Diagnostics[^1]);
        }
        ed.Error("COBOLNET9999", "cleared");
        Assert.Equal("error COBOLNET9999: cleared", ed.Diagnostics[^1]);
        // an UNSET captured cursor keeps the current position — never "line 0"
        using (ed.At(2, 3))
        using (ed.At(default(DiagnosticCursor)))
        {
            ed.Error("COBOLNET9999", "kept");
            Assert.StartsWith("main.cob(2,4): ", ed.Diagnostics[^1]);
        }
        // a line outside the map (a synthetic position) falls back to the main file
        using (ed.At(99, 0)) { ed.Error("COBOLNET9999", "beyond"); Assert.StartsWith("main.cob(99,1): ", ed.Diagnostics[^1]); }
    }

    [Fact]
    public void SourceLineOf_MapsAndFallsBackToIdentity()
    {
        var ed = new EditionContext(2023) { SourceFile = "m.cob" };
        Assert.Equal(12, ed.SourceLineOf(12));                       // no map: identity
        ed.LineMap = new SourceLineMap([new(new SourceOrigin("m.cob", 1)), new(new SourceOrigin("m.cob", 5))]);
        Assert.Equal(5, ed.SourceLineOf(2));
        Assert.Equal(new SourceOrigin("m.cob", 5), ed.OriginOf(2));
        Assert.Equal(new SourceOrigin("m.cob", 3), ed.OriginOf(3));   // beyond the map: identity on the main file
    }
}
