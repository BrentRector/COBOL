// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Frontend.Common;

/// <summary>Where a line of RESULTANT text came from (kb/Work PB82): the file it was read from and its 1-based
/// physical line in that file — the main source file, or a copybook incorporated by COPY (ISO §7.2 text
/// manipulation: the compiler compiles the resultant text; the user edits the source files). A LINE only: the column
/// a resultant position stands at in that line is the <see cref="LineOrigin"/>'s to say.</summary>
public readonly record struct SourceOrigin(string File, int Line)
{
    /// <summary>This origin as a diagnostic <see cref="SourceLocation"/> (whose line is 0-based; the origin's is the
    /// 1-based physical line) at 0-based physical <paramref name="column"/> — the ONE conversion, so no reporter
    /// re-derives the off-by-one.</summary>
    public SourceLocation ToLocation(int column) => new(File, 0, Math.Max(Line - 1, 0), column);
}

/// <summary>One stretch of a resultant line piece (kb/Work PB1801): the characters from 0-based resultant column
/// <see cref="ResultColumn"/> up to the next span's came from <see cref="Origin"/>, the first of them at 0-based
/// PHYSICAL column <see cref="SourceColumn"/> of that line and each next one a column further. A physical column is an
/// index of the line as the file holds it — before the line-entry stage expanded its tabs, so a TAB counts one, as in
/// every editor (the reference-format stage that bears the spans splits them at each tab).</summary>
public readonly record struct OriginSpan(int ResultColumn, SourceOrigin Origin, int SourceColumn);

/// <summary>
/// THE ORIGIN OF ONE RESULTANT LINE PIECE (kb/Work PB1801): its spans in resultant-column order. The resultant text is
/// not the text the user wrote — the fixed-form logical conversion drops positions 1–7 and joins a continuation line's
/// text onto the latest logical line (§6.5), COPY splices library text into a line, REPLACE substitutes text of another
/// length (§7.2) — so a resultant line maps to its source as a list of stretches, each at its own physical line and
/// column, and the LINE that names the piece (<see cref="Origin"/>) is the origin of its FIRST stretch (kb/Work PB82).
/// The spans are born where the reference format reads the physical lines and carried by <see cref="OriginWriter"/>
/// through every later stage; <see cref="Locate"/> is the one place a resultant column becomes a physical one.
/// </summary>
public sealed class LineOrigin
{
    private readonly OriginSpan[] _spans;

    /// <summary>A piece whose every character comes from <paramref name="origin"/>, the first at 0-based physical
    /// <paramref name="sourceColumn"/>.</summary>
    public LineOrigin(SourceOrigin origin, int sourceColumn = 0) => _spans = [new OriginSpan(0, origin, sourceColumn)];

    /// <param name="spans">At least one span, in strictly increasing <see cref="OriginSpan.ResultColumn"/> order.</param>
    public LineOrigin(OriginSpan[] spans)
    {
        if (spans.Length == 0)
            throw new ArgumentException("a line piece has at least one origin span (kb/Work PB1801)", nameof(spans));
        for (int i = 1; i < spans.Length; i++)
            if (spans[i].ResultColumn <= spans[i - 1].ResultColumn)
                throw new ArgumentException("origin spans are in strictly increasing resultant-column order (kb/Work PB1801)", nameof(spans));
        _spans = spans;
    }

    /// <summary>The file and physical line that name this piece: those of its first stretch (kb/Work PB82).</summary>
    public SourceOrigin Origin => _spans[0].Origin;

    /// <summary>The stretches, in resultant-column order.</summary>
    public ReadOnlySpan<OriginSpan> Spans => _spans;

    /// <summary>The physical origin of 0-based resultant <paramref name="column"/>: the span holding it, rebased to
    /// start there. A column before the first span (text a stage wrote ahead of the source's, such as the debugging-line
    /// carrier) stands at the first span's start; a column past the end of the piece continues its last span.</summary>
    public OriginSpan PointAt(int column)
    {
        int k = 0;
        while (k + 1 < _spans.Length && _spans[k + 1].ResultColumn <= column) k++;
        var span = _spans[k];
        return new OriginSpan(column, span.Origin, span.SourceColumn + Math.Max(column - span.ResultColumn, 0));
    }

    /// <summary>The diagnostic location of 0-based resultant <paramref name="column"/> of this piece — the file, line
    /// AND column the user edits.</summary>
    public SourceLocation Locate(int column)
    {
        var at = PointAt(column);
        return at.Origin.ToLocation(at.SourceColumn);
    }

    /// <summary>This piece from resultant <paramref name="column"/> on, rebased so that column is column 0 — the origin of
    /// a sub-text that begins inside the piece (<see cref="MappedText.Slice"/>).</summary>
    public LineOrigin From(int column)
    {
        if (column <= 0) return this;
        var spans = new List<OriginSpan> { PointAt(column) with { ResultColumn = 0 } };
        foreach (var span in _spans)
            if (span.ResultColumn > column) spans.Add(span with { ResultColumn = span.ResultColumn - column });
        return new LineOrigin([.. spans]);
    }
}

/// <summary>
/// The RESULTANT-line → SOURCE-origin map of one compilation unit (kb/Work PB82, PB1801) — the <c>#line</c>-style table
/// the preprocessing chain builds (fixed-form continuation joins, conditional compilation, COPY expansion, REPLACE) and
/// every user-facing position consumes: the parser's diagnostic positions, the binder's diagnostic cursor, and
/// <c>FUNCTION EXCEPTION-LOCATION</c>'s line identifier. Internal anchors that must stay in RESULTANT space (the
/// &gt;&gt;TURN / &gt;&gt;FLAG event lines compared with token lines) never go through it.
/// </summary>
public sealed class SourceLineMap
{
    private readonly LineOrigin[] _origins;

    /// <param name="origins">One origin per resultant line piece (<c>text.Split('\n')</c> order), index 0 = line 1.</param>
    public SourceLineMap(LineOrigin[] origins) => _origins = origins;

    /// <summary>The identity map: resultant line n of <paramref name="file"/> is source line n, column for column.</summary>
    public static SourceLineMap Identity(string file, int lineCount)
    {
        var o = new LineOrigin[Math.Max(lineCount, 1)];
        for (int i = 0; i < o.Length; i++) o[i] = new LineOrigin(new SourceOrigin(file, i + 1));
        return new SourceLineMap(o);
    }

    /// <summary>The file and physical line of RESULTANT line <paramref name="line"/> (1-based, the ANTLR token line), or
    /// null when the line is outside the mapped text (a synthetic position).</summary>
    public SourceOrigin? Origin(int line) => line >= 1 && line <= _origins.Length ? _origins[line - 1].Origin : null;

    /// <summary>THE diagnostic location of RESULTANT line <paramref name="line"/> (1-based) at 0-based resultant
    /// <paramref name="column"/>: the physical file, line and column it came from (kb/Work PB1801), or
    /// <paramref name="fallbackFile"/> at that same line and column when the line is outside the map.</summary>
    public SourceLocation Locate(int line, int column, string fallbackFile) =>
        line >= 1 && line <= _origins.Length
            ? _origins[line - 1].Locate(column)
            : new SourceOrigin(fallbackFile, line).ToLocation(column);
}

/// <summary>Text under transformation together with its per-line origins (kb/Work PB82, PB1801): <see cref="Lines"/>
/// has one entry per piece of <c>Text.Split('\n')</c> — the invariant every mapped stage preserves.</summary>
public sealed class MappedText
{
    public string Text { get; }
    public LineOrigin[] Lines { get; }
    private int[]? _lineStarts;

    public MappedText(string text, LineOrigin[] lines)
    {
        int pieces = 1;
        foreach (char c in text) if (c == '\n') pieces++;
        if (lines.Length != pieces)
            throw new InvalidOperationException(
                $"MappedText: {lines.Length} origin(s) for {pieces} line piece(s) — a preprocessing stage broke the line-origin invariant (kb/Work PB82)");
        Text = text;
        Lines = lines;
    }

    /// <summary>Every line of <paramref name="text"/> originating in <paramref name="file"/>, line n at n (1-based) and
    /// column c at c.</summary>
    public static MappedText Identity(string text, string file)
    {
        int pieces = 1;
        foreach (char c in text) if (c == '\n') pieces++;
        var lines = new LineOrigin[pieces];
        for (int i = 0; i < pieces; i++) lines[i] = new LineOrigin(new SourceOrigin(file, i + 1));
        return new MappedText(text, lines);
    }

    /// <summary>The 0-based line piece containing character position <paramref name="pos"/>.</summary>
    public int LineIndexAt(int pos)
    {
        var starts = LineStarts;
        // binary search for the last start <= pos
        int lo = 0, hi = starts.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (starts[mid] <= pos) lo = mid; else hi = mid - 1;
        }
        return lo;
    }

    /// <summary>The physical origin of character position <paramref name="pos"/>: its file, line and column (kb/Work
    /// PB1801) — what text a stage WRITES in place of the text at <paramref name="pos"/> (a REPLACE replacement, the
    /// text of a COPY statement) takes as its own.</summary>
    public LineOrigin PointAt(int pos)
    {
        int k = LineIndexAt(pos);
        var at = Lines[k].PointAt(pos - LineStarts[k]);
        return new LineOrigin(at.Origin, at.SourceColumn);
    }

    /// <summary>THE diagnostic location of character position <paramref name="pos"/> — the file, line and column the
    /// user edits (kb/Work PB1801). Every preprocessing stage that reports at a position of its text asks this.</summary>
    public SourceLocation LocateAt(int pos)
    {
        int k = LineIndexAt(pos);
        return Lines[k].Locate(pos - LineStarts[k]);
    }

    /// <summary>A sub-text (<paramref name="start"/>, <paramref name="length"/>) with the origins of its own pieces — the
    /// first rebased to begin where the sub-text does.</summary>
    public MappedText Slice(int start, int length)
    {
        int first = LineIndexAt(start);
        string sub = Text.Substring(start, length);
        int pieces = 1;
        foreach (char c in sub) if (c == '\n') pieces++;
        var lines = new LineOrigin[pieces];
        lines[0] = Lines[first].From(start - LineStarts[first]);
        for (int i = 1; i < pieces; i++) lines[i] = Lines[Math.Min(first + i, Lines.Length - 1)];
        return new MappedText(sub, lines);
    }

    /// <summary>The character position where 0-based line piece <paramref name="piece"/> begins.</summary>
    public int LineStart(int piece) => LineStarts[piece];

    private int[] LineStarts => _lineStarts ??= BuildLineStarts(Text);

    private static int[] BuildLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (int i = 0; i < text.Length; i++) if (text[i] == '\n') starts.Add(i + 1);
        return starts.ToArray();
    }
}

/// <summary>Builds a <see cref="MappedText"/> from appended segments, each segment carrying the origins of its own
/// line pieces (kb/Work PB82, PB1801). Every character written keeps the physical position it came from: a slice of a
/// mapped input carries the input's spans, rebased to the column it lands on, and text a stage writes itself (a REPLACE
/// replacement, a COPY statement left in place) takes ONE span at the position it stands for. An output line piece is
/// named by the origin of the FIRST content written into it, so a COPY statement's leading text, the copybook's lines,
/// and the trailing remainder each keep their own file, line and column — the splice points of
/// <c>ExpandCopiesOneLevel</c> and the replacements of <c>ApplyReplacements</c>.</summary>
public sealed class OriginWriter
{
    private readonly StringBuilder _sb = new();
    private readonly List<LineOrigin> _lines = new();
    private readonly List<OriginSpan> _spans = new();   // the spans of the piece being written
    private int _column;                                  // the resultant column the next character lands on

    /// <summary>The number of line pieces completed so far — the 0-based index of the piece the next content opens
    /// when the current piece is empty (just after a newline).</summary>
    public int LineCount => _lines.Count;

    /// <summary>Append <paramref name="s"/>, text this stage writes itself: every piece of it originates at
    /// <paramref name="at"/> (<see cref="MappedText.PointAt"/>), its characters a column further each.</summary>
    public void Append(ReadOnlySpan<char> s, LineOrigin at)
    {
        while (true)
        {
            int nl = s.IndexOf('\n');
            if (nl < 0)
            {
                Write(s, at, 0);
                return;
            }
            Write(s[..nl], at, 0);
            EndPiece(at, 0);
            s = s[(nl + 1)..];
        }
    }

    /// <summary>Append a slice of a mapped input — every character keeps its input origin.</summary>
    public void AppendSlice(MappedText input, int start, int length)
    {
        if (length <= 0) return;
        var s = input.Text.AsSpan(start, length);
        int piece = input.LineIndexAt(start);
        int column = start - input.LineStart(piece);
        while (true)
        {
            int nl = s.IndexOf('\n');
            var origin = input.Lines[Math.Min(piece, input.Lines.Length - 1)];
            if (nl < 0)
            {
                Write(s, origin, column);
                return;
            }
            Write(s[..nl], origin, column);
            EndPiece(origin, column);
            s = s[(nl + 1)..];
            piece++;
            column = 0;
        }
    }

    /// <summary>Append a whole mapped text — its characters keep their origins.</summary>
    public void AppendMapped(MappedText input) => AppendSlice(input, 0, input.Text.Length);

    /// <summary>End a line: a newline whose piece, if still empty, originates at <paramref name="origin"/>.</summary>
    public void NewLine(LineOrigin origin) => EndPiece(origin, 0);

    public MappedText Finish()
    {
        // The final piece (possibly empty after a trailing newline): an empty one is named like the piece before it.
        _lines.Add(_spans.Count > 0 ? new LineOrigin([.. _spans])
            : _lines.Count > 0 ? _lines[^1] : new LineOrigin(new SourceOrigin("", 0)));
        return new MappedText(_sb.ToString(), _lines.ToArray());
    }

    /// <summary>Write <paramref name="run"/> (no newline), whose first character is at resultant column
    /// <paramref name="column"/> of <paramref name="origin"/>: the origin's spans over the run, rebased to the column the
    /// run lands on, each one that merely continues the span before it folded into it.</summary>
    private void Write(ReadOnlySpan<char> run, LineOrigin origin, int column)
    {
        if (run.IsEmpty) return;
        AddSpan(origin.PointAt(column) with { ResultColumn = _column });
        foreach (var span in origin.Spans)
            if (span.ResultColumn > column && span.ResultColumn < column + run.Length)
                AddSpan(span with { ResultColumn = _column + span.ResultColumn - column });
        _sb.Append(run);
        _column += run.Length;
    }

    private void AddSpan(OriginSpan span)
    {
        if (_spans.Count > 0)
        {
            var prev = _spans[^1];
            if (prev.Origin == span.Origin && prev.SourceColumn + (span.ResultColumn - prev.ResultColumn) == span.SourceColumn)
                return;   // the same stretch, continued
        }
        _spans.Add(span);
    }

    /// <summary>End the piece being written: named by its first content, or — when it holds none — by
    /// <paramref name="origin"/> at <paramref name="column"/>, where the newline that ends it came from.</summary>
    private void EndPiece(LineOrigin origin, int column)
    {
        _sb.Append('\n');
        _lines.Add(_spans.Count > 0 ? new LineOrigin([.. _spans]) : origin.From(column));
        _spans.Clear();
        _column = 0;
    }
}
