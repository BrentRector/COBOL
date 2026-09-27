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

    private static int[] LinesOf(MappedText m) => m.Lines.Select(o => o.Line).ToArray();

    // ── MappedText / OriginWriter primitives ──────────────────────────────────────────────────────────────

    [Fact]
    public void MappedText_AssertsOneOriginPerLinePiece()
    {
        var ok = MappedText.Identity("a\nb\nc", "f");
        Assert.Equal([1, 2, 3], LinesOf(ok));
        Assert.Equal(new SourceOrigin("f", 2), ok.OriginAt(2));   // position 2 = the 'b' piece
        Assert.Throws<InvalidOperationException>(() => new MappedText("a\nb", [new SourceOrigin("f", 1)]));
    }

    [Fact]
    public void OriginWriter_EachOutputPieceTakesTheOriginOfItsFirstContent()
    {
        var w = new OriginWriter();
        w.Append("head ".AsSpan(), new SourceOrigin("main", 5));
        w.NewLine(new SourceOrigin("main", 5));
        w.AppendMapped(MappedText.Identity("c1\nc2", "cpy"));
        w.NewLine(new SourceOrigin("main", 5));
        w.Append("tail".AsSpan(), new SourceOrigin("main", 6));
        var m = w.Finish();
        Assert.Equal("head \nc1\nc2\ntail", m.Text);
        Assert.Equal([("main", 5), ("cpy", 1), ("cpy", 2), ("main", 6)], m.Lines.Select(o => (o.File, o.Line)).ToArray());
        var slice = m.Slice(6, 5);   // "c1\nc2"
        Assert.Equal([("cpy", 1), ("cpy", 2)], slice.Lines.Select(o => (o.File, o.Line)).ToArray());
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
        var m = ReferenceFormatProcessor.NormalizeToFreeFormMapped(src, 2023, false, null, "p.cob");
        var lines = m.Text.Split('\n');
        int bIndex = Array.FindIndex(lines, l => l.Contains("01 B PIC X"));
        Assert.True(bIndex >= 0, m.Text);
        Assert.Equal(7, m.Lines[bIndex].Line);           // the line AFTER the join is still physical line 7
        int aIndex = Array.FindIndex(lines, l => l.Contains("01 A PIC"));
        Assert.Equal(5, m.Lines[aIndex].Line);           // the joined line keeps its head line
        // and IS joined — the short continued line read as space-filled to margin R, so positions 33–72 are part of
        // the literal (§6.3.5 "any spaces at the end of the fixed-form continued line are part of the literal";
        // DOC-A.1-157; kb/Work PB1491)
        Assert.Contains("\"abc" + new string(' ', 40) + "def\"", lines[aIndex]);
    }

    [Fact]
    public void NistArchiveMarkers_AreBlankedNotDropped()
    {
        string src = "*HEADER,COBOL,SM101A\n       IDENTIFICATION DIVISION.\n*END-OF,SM101A\n";
        string stripped = ReferenceFormatProcessor.StripNistArchiveMarkers(src);
        Assert.Equal(src.Count(c => c == '\n'), stripped.Count(c => c == '\n'));
        Assert.StartsWith("\n       IDENTIFICATION", stripped);
    }

    // ── COPY: the main text keeps its lines, the copied text names the copybook, the text after resumes ──

    [Fact]
    public void Copy_MapsMainAndCopybookLines()
    {
        Copybook("three.cpy", "01 C1 PIC X.\n01 C2 PIC X.\n01 C3 PIC X.\n");
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, "t.cob", strict: true, dialectLevel: 2023, permissive: false);
        var main = MappedText.Identity("01 A PIC X.\nCOPY three.\n01 B PIC X.\n", "t.cob");
        var m = ConditionalCompilationProcessor.ProcessWithCopyMapped(main, _dir, copy,
            CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        Assert.False(bag.HasErrors, string.Join("\n", bag.Diagnostics));
        var lines = m.Text.Split('\n');
        int b = Array.FindIndex(lines, l => l.Contains("01 B PIC X"));
        Assert.Equal(("t.cob", 3), (m.Lines[b].File, m.Lines[b].Line));
        int c2 = Array.FindIndex(lines, l => l.Contains("01 C2"));
        Assert.EndsWith("three.cpy", m.Lines[c2].File);
        Assert.Equal(2, m.Lines[c2].Line);
        int a = Array.FindIndex(lines, l => l.Contains("01 A PIC X"));
        Assert.Equal(("t.cob", 1), (m.Lines[a].File, m.Lines[a].Line));
    }

    [Fact]
    public void Replace_StatementLinesVanish_FollowingLinesKeepTheirSourceNumbers()
    {
        // REPLACE is Step 3 of the merged text-manipulation driver (ISO §7.2.1), run over the expanded group.
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, "t.cob", strict: true, dialectLevel: 2023, permissive: false);
        var m = ConditionalCompilationProcessor.ProcessWithCopyMapped(
            MappedText.Identity("01 A PIC X.\nREPLACE ==A== BY ==B==.\n01 C PIC X.\n01 A PIC X.\n", "t.cob"),
            _dir, copy, CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        var lines = m.Text.Split('\n');
        Assert.DoesNotContain(lines, l => l.Contains("REPLACE"));
        int c = Array.FindIndex(lines, l => l.Contains("01 C PIC X"));
        Assert.Equal(3, m.Lines[c].Line);
        int b = Array.FindLastIndex(lines, l => l.Contains("01 B PIC X"));   // the replaced A → B on source line 4
        Assert.Equal(4, m.Lines[b].Line);
    }

    // ── the cursor + the prefix ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EditionContext_StampsTheCursorThroughTheMap_AndBareWithoutOne()
    {
        var ed = new EditionContext(2023)
        {
            SourceFile = "main.cob",
            LineMap = new SourceLineMap([new("main.cob", 1), new("main.cob", 2), new("lib.cpy", 7), new("main.cob", 4)]),
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
        ed.LineMap = new SourceLineMap([new("m.cob", 1), new("m.cob", 5)]);
        Assert.Equal(5, ed.SourceLineOf(2));
        Assert.Equal(new SourceOrigin("m.cob", 5), ed.OriginOf(2));
        Assert.Equal(new SourceOrigin("m.cob", 3), ed.OriginOf(3));   // beyond the map: identity on the main file
    }
}
