// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Frontend.Common;

/// <summary>Where a line of RESULTANT text came from (kb/Work PB82): the file it was read from and its 1-based
/// physical line in that file — the main source file, or a copybook incorporated by COPY (ISO §7.2 text
/// manipulation: the compiler compiles the resultant text; the user edits the source files).</summary>
public readonly record struct SourceOrigin(string File, int Line)
{
    /// <summary>This origin as a diagnostic <see cref="SourceLocation"/> (whose line is 0-based; the origin's is the
    /// 1-based physical line) — the ONE conversion, so no reporter re-derives the off-by-one.</summary>
    public SourceLocation ToLocation(int column = 0) => new(File, 0, Math.Max(Line - 1, 0), column);
}

/// <summary>
/// The RESULTANT-line → SOURCE-origin map of one compilation unit (kb/Work PB82) — the <c>#line</c>-style table the
/// preprocessing chain builds (fixed-form continuation joins, conditional compilation, COPY expansion, REPLACE) and
/// every user-facing position consumes: the parser's diagnostic positions, the binder's diagnostic cursor, and
/// <c>FUNCTION EXCEPTION-LOCATION</c>'s line identifier. Internal anchors that must stay in RESULTANT space (the
/// &gt;&gt;TURN / &gt;&gt;FLAG event lines compared with token lines) never go through it.
/// </summary>
public sealed class SourceLineMap
{
    private readonly SourceOrigin[] _origins;

    /// <param name="origins">One origin per resultant line piece (<c>text.Split('\n')</c> order), index 0 = line 1.</param>
    public SourceLineMap(SourceOrigin[] origins) => _origins = origins;

    /// <summary>The identity map: resultant line n of <paramref name="file"/> is source line n.</summary>
    public static SourceLineMap Identity(string file, int lineCount)
    {
        var o = new SourceOrigin[Math.Max(lineCount, 1)];
        for (int i = 0; i < o.Length; i++) o[i] = new SourceOrigin(file, i + 1);
        return new SourceLineMap(o);
    }

    /// <summary>The origin of RESULTANT line <paramref name="line"/> (1-based, the ANTLR token line), or null when the
    /// line is outside the mapped text (a synthetic position).</summary>
    public SourceOrigin? Origin(int line) =>
        line >= 1 && line <= _origins.Length ? _origins[line - 1] : null;

    /// <summary>The diagnostic location of RESULTANT line <paramref name="line"/> (1-based): its source origin, or
    /// <paramref name="fallbackFile"/> at that same line when the line is outside the map.</summary>
    public SourceLocation Locate(int line, string fallbackFile, int column = 0) =>
        (Origin(line) ?? new SourceOrigin(fallbackFile, line)).ToLocation(column);
}

/// <summary>Text under transformation together with its per-line origins (kb/Work PB82): <see cref="Lines"/> has one
/// entry per piece of <c>Text.Split('\n')</c> — the invariant every mapped stage preserves.</summary>
public sealed class MappedText
{
    public string Text { get; }
    public SourceOrigin[] Lines { get; }
    private int[]? _lineStarts;

    public MappedText(string text, SourceOrigin[] lines)
    {
        int pieces = 1;
        foreach (char c in text) if (c == '\n') pieces++;
        if (lines.Length != pieces)
            throw new InvalidOperationException(
                $"MappedText: {lines.Length} origin(s) for {pieces} line piece(s) — a preprocessing stage broke the line-origin invariant (kb/Work PB82)");
        Text = text;
        Lines = lines;
    }

    /// <summary>Every line of <paramref name="text"/> originating in <paramref name="file"/>, line n at n (1-based).</summary>
    public static MappedText Identity(string text, string file)
    {
        int pieces = 1;
        foreach (char c in text) if (c == '\n') pieces++;
        var lines = new SourceOrigin[pieces];
        for (int i = 0; i < pieces; i++) lines[i] = new SourceOrigin(file, i + 1);
        return new MappedText(text, lines);
    }

    /// <summary>The 0-based line piece containing character position <paramref name="pos"/>.</summary>
    public int LineIndexAt(int pos)
    {
        var starts = _lineStarts ??= BuildLineStarts(Text);
        // binary search for the last start <= pos
        int lo = 0, hi = starts.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (starts[mid] <= pos) lo = mid; else hi = mid - 1;
        }
        return lo;
    }

    /// <summary>The origin of the line piece containing character position <paramref name="pos"/>.</summary>
    public SourceOrigin OriginAt(int pos) => Lines[LineIndexAt(pos)];

    /// <summary>A sub-text (<paramref name="start"/>, <paramref name="length"/>) with the origins of its own pieces.</summary>
    public MappedText Slice(int start, int length)
    {
        int first = LineIndexAt(start);
        string sub = Text.Substring(start, length);
        int pieces = 1;
        foreach (char c in sub) if (c == '\n') pieces++;
        var lines = new SourceOrigin[pieces];
        for (int i = 0; i < pieces; i++) lines[i] = Lines[Math.Min(first + i, Lines.Length - 1)];
        return new MappedText(sub, lines);
    }

    private static int[] BuildLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (int i = 0; i < text.Length; i++) if (text[i] == '\n') starts.Add(i + 1);
        return starts.ToArray();
    }
}

/// <summary>Builds a <see cref="MappedText"/> from appended segments, each segment carrying the origins of its own
/// line pieces (kb/Work PB82). An output line piece takes the origin of the FIRST content written into it, so a
/// COPY statement's leading text, the copybook's lines, and the trailing remainder each keep their own file and
/// line — the splice points of <c>ExpandCopiesOneLevel</c> and the replacements of <c>ApplyReplacements</c>.</summary>
public sealed class OriginWriter
{
    private readonly StringBuilder _sb = new();
    private readonly List<SourceOrigin> _lines = new();
    private SourceOrigin? _current;    // the origin of the piece being written (null until content arrives)
    private SourceOrigin _last;        // the last origin seen — a bare newline on an empty piece takes it

    /// <summary>The number of line pieces completed so far — the 0-based index of the piece the next content opens
    /// when the current piece is empty (just after a newline).</summary>
    public int LineCount => _lines.Count;

    /// <summary>Append <paramref name="s"/>, whose k-th line piece originates at <paramref name="pieceOrigin"/>(k).
    /// Bulk-appends piece by piece (the whole compilation unit flows through here — never char by char).</summary>
    public void Append(ReadOnlySpan<char> s, Func<int, SourceOrigin> pieceOrigin)
    {
        int piece = 0;
        while (!s.IsEmpty)
        {
            if (_current is null) { _current = pieceOrigin(piece); _last = _current.Value; }
            int nl = s.IndexOf('\n');
            if (nl < 0) { _sb.Append(s); return; }
            _sb.Append(s[..(nl + 1)]);
            _lines.Add(_current.Value);
            _current = null;
            piece++;
            s = s[(nl + 1)..];
        }
    }

    /// <summary>Append <paramref name="s"/> with every piece originating at <paramref name="origin"/>.</summary>
    public void Append(ReadOnlySpan<char> s, SourceOrigin origin) => Append(s, _ => origin);

    /// <summary>Append a slice of a mapped input — the slice's pieces keep the input's origins.</summary>
    public void AppendSlice(MappedText input, int start, int length)
    {
        if (length <= 0) return;
        int firstPiece = input.LineIndexAt(start);
        Append(input.Text.AsSpan(start, length), k => input.Lines[Math.Min(firstPiece + k, input.Lines.Length - 1)]);
    }

    /// <summary>Append a whole mapped text — its pieces keep their origins.</summary>
    public void AppendMapped(MappedText input) => Append(input.Text.AsSpan(), k => input.Lines[Math.Min(k, input.Lines.Length - 1)]);

    /// <summary>End a line: a newline whose piece, if still empty, originates at <paramref name="origin"/>.</summary>
    public void NewLine(SourceOrigin origin) => Append("\n".AsSpan(), origin);

    public MappedText Finish()
    {
        _lines.Add(_current ?? _last);   // the final piece (possibly empty after a trailing newline)
        return new MappedText(_sb.ToString(), _lines.ToArray());
    }
}
