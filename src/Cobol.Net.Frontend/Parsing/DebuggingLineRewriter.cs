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
/// </summary>
public static class DebuggingLineRewriter
{
    /// <summary>Keep or hide every debugging line of the filled token stream per its source unit's
    /// <c>WITH DEBUGGING MODE</c> clause. Byte-identical (no token touched) when the text holds no debugging line.
    /// Must run before every other rewriter and every reader of the token list, and after
    /// <see cref="CommonTokenStream.Fill"/>.</summary>
    public static void Rewrite(CommonTokenStream tokenStream)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();
        if (tokens is null) return;
        var lines = DebugLines(tokens);
        if (lines.Count == 0) return;

        var visible = VisibleIndices(tokens, lines);
        var units = new Stack<UnitFrame>();
        var outside = new UnitFrame(inheritedMode: false);   // debugging lines before any unit header: no clause can govern them
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
                    if (units.Count > 0) Resolve(tokens, units.Pop());
                    break;
                case CobolLexer.SOURCE_COMPUTER when units.Count > 0 && DeclaresMode(tokens, visible, v):
                    units.Peek().DeclaresMode = true;
                    break;
            }
        }
        while (nextLine < lines.Count) (units.Count > 0 ? units.Peek() : outside).Lines.Add(lines[nextLine++]);
        while (units.Count > 0) Resolve(tokens, units.Pop());
        Resolve(tokens, outside);
        tokenStream.Seek(0);
    }

    /// <summary>Hide every debugging line, as a comment: the reading of a re-lexed FRAGMENT of source text
    /// (<see cref="FragmentParse"/>), whose text comes from a region the main lexer read in a mode that skips a debugging
    /// line as a comment (SUBSCRIPT) — the one reading both lexes of that text agree on.</summary>
    public static void HideAll(CommonTokenStream tokenStream)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();
        if (tokens is null) return;
        foreach (var line in DebugLines(tokens)) Hide(tokens, line);
        tokenStream.Seek(0);
    }

    /// <summary>The tokens a debugging line owns: its marker at <see cref="Extent.Start"/> and every following token
    /// on the marker's line, to <see cref="Extent.End"/> exclusive.</summary>
    private readonly record struct Extent(int Start, int End);

    private static List<Extent> DebugLines(IList<IToken> tokens)
    {
        var lines = new List<Extent>();
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type != CobolLexer.DEBUG_LINE) continue;
            int line = tokens[i].Line, end = i + 1;
            while (end < tokens.Count && tokens[end].Type != TokenConstants.EOF && tokens[end].Line == line) end++;
            lines.Add(new Extent(i, end));
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

    private static void Resolve(IList<IToken> tokens, UnitFrame unit)
    {
        if (unit.Mode) return;                          // the unit (or a container) declared the mode: its lines are source
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
