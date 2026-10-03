// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⛔ THE ONE PLACE A FIXED-FORM DEBUGGING LINE BECOMES SOURCE OR COMMENT (kb/Work PB1705, owner decision R56).
/// <para>The COBOL-85 debug module compiles an indicator-<c>D</c> line only when the program's SOURCE-COMPUTER paragraph
/// says <c>WITH DEBUGGING MODE</c>; otherwise it is a comment line. The reference-format stage that reads the
/// indicator runs before any clause is bound — and before the clause is even in the text it has read — so it cannot
/// decide: it CARRIES the line (<see cref="CobolNet.Frontend.Preprocessor.ReferenceFormatProcessor.DebugLineCarrier"/>),
/// the lexer turns the carrier into a hidden <c>DEBUG_LINE</c> marker and lexes the line's text as ordinary tokens, and
/// this pass — which sees the whole token stream, the clause included — keeps those tokens (the line is source) or moves
/// them to the <see cref="CobolLexer.ABSENT_DEBUG_LINE"/> channel the parser, the §8.3.5 separator rules and every
/// other reader of the default channel never see (the line is a comment).</para>
/// <para><b>Per source unit.</b> ISO §12.3.5.4 GR1: "All clauses of the SOURCE-COMPUTER paragraph apply to the source
/// unit in which they are explicitly or implicitly specified and to any source unit contained within that source
/// unit" (<c>cite.py --check 12.3.5.4</c>), so a debugging line is source when its own unit — or a unit that contains
/// it (kb/Work PB1088, the binder's <c>InheritConfiguration</c> twin of this rule) — declares the mode. A unit is the
/// run of tokens from its <c>PROGRAM-ID</c>/<c>FUNCTION-ID</c>/<c>CLASS-ID</c>/<c>INTERFACE-ID</c>/<c>METHOD-ID</c>
/// paragraph header to its <c>END PROGRAM/FUNCTION/CLASS/INTERFACE/METHOD</c> header, nesting as the source nests.
/// The clause itself is read from the tokens of the unit's SOURCE-COMPUTER paragraph, never from a debugging line
/// (a mode switch written on a debugging line would decide its own line).</para>
/// <para>A debugging line's tokens are those after its marker on the marker's own line; a fixed continuation line
/// is joined to its line by the logical conversion (§6.5 6)), so it is part of the line.</para>
/// <para><b>A line inside an open PICTURE or subscript region</b> (kb/Work PB1913) is the one shape this pass cannot
/// settle by moving tokens. The lexer is in PICMODE or SUBSCRIPT there, and which of "source" and "comment" the line's
/// text is decides whether the text moves the lexer's own mode (a <c>)</c> that closes the subscript, the PICTURE
/// string that ends PICMODE), so the region modes skip the text and leave a
/// <see cref="CobolLexer.DEBUG_LINE_IN_REGION"/> marker. <see cref="Rewrite"/> then names the FIRST such line whose unit
/// declares the mode, and the frontend blanks that line's carrier (<see cref="WithCarrierBlanked"/>) and lexes the text
/// again: the line is plain source the second time. Only the first, because every later token of the first lex came
/// from a lexer that skipped a line which is in fact source, so a decision about a LATER line read off those tokens may
/// be wrong; the tokens up to and including the first kept line are exact (an earlier region line was a comment, and a
/// comment skipped in the region is what the lexer did). A region sits in the DATA or PROCEDURE DIVISION, after the
/// unit's SOURCE-COMPUTER paragraph, which is why the clause the decision reads is on the exact side.</para>
/// </summary>
public static class DebuggingLineRewriter
{
    /// <summary>Keep or hide every debugging line of the filled token stream per its source unit's
    /// <c>WITH DEBUGGING MODE</c> clause. Byte-identical (no token touched) when the text holds no debugging line.
    /// Must run before every other rewriter and every reader of the token list, and after
    /// <see cref="CommonTokenStream.Fill"/>.
    /// <para>Returns the first debugging line INSIDE A PICTURE OR SUBSCRIPT REGION that is source — the lexer skipped
    /// its text, so the caller must blank its carrier and lex again (<see cref="WithCarrierBlanked"/>), then call this
    /// again — or <see langword="null"/> when there is none, which is when the token stream is final.</para></summary>
    public static IToken? Rewrite(CommonTokenStream tokenStream)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();
        if (tokens is null) return null;
        var lines = DebugLines(tokens);
        if (lines.Count == 0) return null;

        var visible = VisibleIndices(tokens, lines);
        var units = new Stack<UnitFrame>();
        var outside = new UnitFrame(inheritedMode: false);   // debugging lines before any unit header: no clause can govern them
        var kept = new List<int>();                          // the region lines that are source (token indices)
        int nextLine = 0;
        for (int v = 0; v < visible.Count; v++)
        {
            int at = visible[v];
            // Attach every debugging line that begins before this token to the unit it sits in.
            while (nextLine < lines.Count && lines[nextLine].Start < at)
                (units.Count > 0 ? units.Peek() : outside).Lines.Add(lines[nextLine++]);

            switch (tokens[at].Type)
            {
                case CobolLexer.PROGRAM_ID or CobolLexer.FUNCTION_ID or CobolLexer.CLASS_ID
                    or CobolLexer.INTERFACE_ID or CobolLexer.METHOD_ID:
                    units.Push(new UnitFrame(units.Count > 0 && units.Peek().Mode));
                    break;
                case CobolLexer.END when v + 1 < visible.Count && IsUnitKind(tokens[visible[v + 1]].Type):
                    if (units.Count > 0) Resolve(tokens, units.Pop(), kept);
                    break;
                case CobolLexer.SOURCE_COMPUTER when units.Count > 0 && DeclaresMode(tokens, visible, v):
                    units.Peek().DeclaresMode = true;
                    break;
            }
        }
        while (nextLine < lines.Count) (units.Count > 0 ? units.Peek() : outside).Lines.Add(lines[nextLine++]);
        while (units.Count > 0) Resolve(tokens, units.Pop(), kept);
        Resolve(tokens, outside, kept);
        tokenStream.Seek(0);
        return kept.Count == 0 ? null : tokens[kept.Min()];
    }

    /// <summary>The text with the carrier of <paramref name="regionLine"/> (a <see cref="CobolLexer.DEBUG_LINE_IN_REGION"/>
    /// marker <see cref="Rewrite"/> returned) replaced by as many spaces, so the line becomes plain source at the same
    /// columns and every line, column and offset after it is unchanged. The marker's text starts with the carrier.</summary>
    public static string WithCarrierBlanked(string text, IToken regionLine)
    {
        string carrier = CobolNet.Frontend.Preprocessor.ReferenceFormatProcessor.DebugLineCarrier;
        int at = regionLine.StartIndex;
        if (regionLine.Type != CobolLexer.DEBUG_LINE_IN_REGION
            || string.CompareOrdinal(text, at, carrier, 0, carrier.Length) != 0)
            throw new InvalidOperationException(
                $"DebuggingLineRewriter.WithCarrierBlanked: line {regionLine.Line} is not a skipped region debugging line whose carrier stands at offset {at}");
        return string.Concat(text.AsSpan(0, at), new string(' ', carrier.Length), text.AsSpan(at + carrier.Length));
    }

    /// <summary>Hide every debugging line, as a comment: the reading of a re-lexed FRAGMENT of source text
    /// (<see cref="FragmentParse"/>), whose text is a slice of the text the main lexer finally read. A debugging line that
    /// is SOURCE had its carrier blanked before that lex (<see cref="WithCarrierBlanked"/>), so the carriers that remain
    /// in the slice are exactly the lines that are comments — the one reading both lexes of the text agree on.</summary>
    public static void HideAll(CommonTokenStream tokenStream)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();
        if (tokens is null) return;
        foreach (var line in DebugLines(tokens)) Hide(tokens, line);
        tokenStream.Seek(0);
    }

    /// <summary>The tokens a debugging line owns: its marker at <see cref="Start"/> and every following token on the
    /// marker's line, to <see cref="End"/> exclusive. A region line (<see cref="Region"/>) owns the marker alone — its
    /// text was skipped, not lexed.</summary>
    private readonly record struct Extent(int Start, int End, bool Region);

    private static List<Extent> DebugLines(IList<IToken> tokens)
    {
        var lines = new List<Extent>();
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type == CobolLexer.DEBUG_LINE_IN_REGION) { lines.Add(new Extent(i, i + 1, Region: true)); continue; }
            if (tokens[i].Type != CobolLexer.DEBUG_LINE) continue;
            int line = tokens[i].Line, end = i + 1;
            while (end < tokens.Count && tokens[end].Type != TokenConstants.EOF && tokens[end].Line == line) end++;
            lines.Add(new Extent(i, end, Region: false));
            i = end - 1;
        }
        return lines;
    }

    /// <summary>The indices of the tokens the unit and clause scan reads: default channel, outside every debugging line.</summary>
    private static List<int> VisibleIndices(IList<IToken> tokens, List<Extent> lines)
    {
        var visible = new List<int>(tokens.Count);
        int line = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            while (line < lines.Count && lines[line].End <= i) line++;
            if (line < lines.Count && i >= lines[line].Start) continue;
            if (tokens[i].Channel == TokenConstants.DefaultChannel && tokens[i].Type != TokenConstants.EOF) visible.Add(i);
        }
        return visible;
    }

    private static bool IsUnitKind(int type)
        => type is CobolLexer.PROGRAM or CobolLexer.FUNCTION or CobolLexer.CLASS or CobolLexer.INTERFACE or CobolLexer.METHOD;

    /// <summary>Whether the SOURCE-COMPUTER paragraph whose header word is <paramref name="visible"/>[<paramref name="at"/>]
    /// holds <c>DEBUGGING MODE</c> (the <c>WITH</c> is optional in the clause, and the clause follows the computer-name;
    /// kb/Work PB830). The paragraph ends at its period; the empty form
    /// <c>SOURCE-COMPUTER. OBJECT-COMPUTER. …</c> (§12.3.5.3 SR1 lets the second period go) ends at the next paragraph's
    /// header word.</summary>
    private static bool DeclaresMode(IList<IToken> tokens, List<int> visible, int at)
    {
        int i = at + 1;
        if (i >= visible.Count || tokens[visible[i]].Type != CobolLexer.DOT) return false;
        for (i++; i < visible.Count; i++)
        {
            int type = tokens[visible[i]].Type;
            if (type is CobolLexer.DOT or CobolLexer.OBJECT_COMPUTER or CobolLexer.SPECIAL_NAMES or CobolLexer.REPOSITORY
                or CobolLexer.PROCEDURE or CobolLexer.DATA or CobolLexer.PROGRAM_ID or CobolLexer.END)
                return false;
            if (type == CobolLexer.DEBUGGING && i + 1 < visible.Count && tokens[visible[i + 1]].Type == CobolLexer.MODE)
                return true;
        }
        return false;
    }

    private static void Resolve(IList<IToken> tokens, UnitFrame unit, List<int> keptRegionLines)
    {
        if (unit.Mode)                                  // the unit (or a container) declared the mode: its lines are source
        {
            foreach (var line in unit.Lines)
                if (line.Region) keptRegionLines.Add(line.Start);   // its text was skipped: the caller lexes it again
            return;
        }
        foreach (var line in unit.Lines) Hide(tokens, line);
    }

    /// <summary>A debugging line that is a comment: its text tokens leave the default channel (the marker stays hidden).</summary>
    private static void Hide(IList<IToken> tokens, Extent line)
    {
        for (int i = line.Start + 1; i < line.End; i++)
            if (tokens[i].Channel == TokenConstants.DefaultChannel)
                tokens[i] = new CommonToken(tokens[i]) { Channel = CobolLexer.ABSENT_DEBUG_LINE };
    }

    /// <summary>One source unit being walked: whether a container or the unit itself declared the mode (so its lines are
    /// source), and the debugging lines inside it that are not inside a unit it contains.</summary>
    private sealed class UnitFrame(bool inheritedMode)
    {
        public bool DeclaresMode { get; set; }
        public bool Mode => inheritedMode || DeclaresMode;
        public List<Extent> Lines { get; } = [];
    }
}
