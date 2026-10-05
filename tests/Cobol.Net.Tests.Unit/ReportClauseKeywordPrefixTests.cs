// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Preprocessor;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The keyword prefixes of the report-writer LINE, COLUMN and SOURCE clauses are the CLOSED sets their formats print,
/// never the cross product of their words (kb/Work PB1221). Every combination of the words each grammar rule could
/// spell is parsed, and the parser's verdict is compared with the printed set:
/// <list type="bullet">
/// <item>LINE — ISO §13.18.35.2 Format 1, the rendered diagram: the brace {LINE NUMBER IS | LINE NUMBERS ARE |
/// LINES ARE}, NUMBER / NUMBERS / IS / ARE not underlined (optional within their alternative, §5.2.6.3).</item>
/// <item>COLUMN — §13.18.14.2 Format 1: the six-way spelling brace {COLUMN NUMBER | COLUMN NUMBERS | COLUMNS |
/// COL NUMBER | COL NUMBERS | COLS}, then the alignment word, then [IS | ARE] split by §13.18.14.3 SR4 ("The keyword ARE
/// may be specified only if COLUMNS, COLS, or NUMBERS is specified") and SR5 ("The keyword IS shall not be specified
/// if COLUMNS, COLS, or NUMBERS is specified") — across the alignment word, which sits between them.</item>
/// <item>SOURCE — §13.18.53.2: the brace {SOURCE IS | SOURCES ARE}.</item>
/// </list>
/// The whole matrix is enumerated, so a future grammar edit that re-opens any one combination fails here by name,
/// and an accepted spelling is also checked to have parsed as the clause with ONE operand (a context-sensitive
/// NUMBERS read as a constant-name operand would be a different program).
/// </summary>
public sealed class ReportClauseKeywordPrefixTests
{
    private static readonly string[] Numbers = ["", "NUMBER", "NUMBERS"];
    private static readonly string[] Connectives = ["", "IS", "ARE"];

    /// <summary>§13.18.35.2 Format 1's first brace, one entry per printed alternative: (spelling, number word,
    /// connective), the last two optional words.</summary>
    private static bool LinePrinted(string spelling, string number, string connective) =>
        (spelling == "LINE" && number is "" or "NUMBER" && connective is "" or "IS")
        || (spelling == "LINE" && number is "" or "NUMBERS" && connective is "" or "ARE")
        || (spelling == "LINES" && number == "" && connective is "" or "ARE");

    /// <summary>§13.18.14.2 Format 1's spelling brace (NUMBER / NUMBERS ride COLUMN and COL only) with §13.18.14.3
    /// SR4 and SR5 over the connective.</summary>
    private static bool ColumnPrinted(string spelling, string number, string connective)
    {
        bool standalonePlural = spelling is "COLUMNS" or "COLS";
        if (standalonePlural && number != "") return false;               // the six-way spelling brace
        bool plural = standalonePlural || number == "NUMBERS";
        if (connective == "ARE" && !plural) return false;                 // SR4
        if (connective == "IS" && plural) return false;                   // SR5
        return true;
    }

    /// <summary>§13.18.53.2: {SOURCE IS | SOURCES ARE}.</summary>
    private static bool SourcePrinted(string spelling, string connective) =>
        connective == "" || (spelling == "SOURCE") == (connective == "IS");

    public static TheoryData<string, string, string> LineCases()
    {
        var data = new TheoryData<string, string, string>();
        foreach (string s in new[] { "LINE", "LINES" })
            foreach (string n in Numbers)
                foreach (string c in Connectives) data.Add(s, n, c);
        return data;
    }

    public static TheoryData<string, string, string, string> ColumnCases()
    {
        var data = new TheoryData<string, string, string, string>();
        foreach (string s in new[] { "COLUMN", "COL", "COLUMNS", "COLS" })
            foreach (string n in Numbers)
                foreach (string a in new[] { "", "RIGHT" })
                    foreach (string c in Connectives) data.Add(s, n, a, c);
        return data;
    }

    public static TheoryData<string, string> SourceCases()
    {
        var data = new TheoryData<string, string>();
        foreach (string s in new[] { "SOURCE", "SOURCES" })
            foreach (string c in Connectives) data.Add(s, c);
        return data;
    }

    [Theory]
    [MemberData(nameof(LineCases))]
    public void LinePrefix_ParsesExactlyWhenPrinted(string spelling, string number, string connective)
    {
        var (tree, diags) = Parse(Program(line: Words(spelling, number, connective)));
        AssertVerdict(LinePrinted(spelling, number, connective), diags, tree,
            t => Descendants<CobolParserCore.ReportLineClauseContext>(t).Single().reportLineOperand().Length);
    }

    [Theory]
    [MemberData(nameof(ColumnCases))]
    public void ColumnPrefix_ParsesExactlyWhenPrinted(string spelling, string number, string alignment, string connective)
    {
        var (tree, diags) = Parse(Program(column: Words(spelling, number, alignment, connective)));
        AssertVerdict(ColumnPrinted(spelling, number, connective), diags, tree,
            t => Descendants<CobolParserCore.ReportColumnClauseContext>(t).Single(c => c.Start.Line == ColumnLine)
                .reportColumnOperand().Length);
    }

    [Theory]
    [MemberData(nameof(SourceCases))]
    public void SourcePrefix_ParsesExactlyWhenPrinted(string spelling, string connective)
    {
        var (tree, diags) = Parse(Program(source: Words(spelling, connective)));
        AssertVerdict(SourcePrinted(spelling, connective), diags, tree,
            t => Descendants<CobolParserCore.ReportSourceClauseContext>(t).Single().reportValueOperand().Length);
    }

    /// <summary>A printed prefix parses clean and as the clause with its one operand. Any other is NOT read as a
    /// keyword prefix: it is a parse error, or — the one other reading the standard gives it — NUMBERS is not a
    /// keyword there. NUMBERS is a context-sensitive word of the COLUMN and LINE clauses, and ISO §8.10 makes it "a
    /// user-defined word" wherever the general format does not permit it, so `COLS NUMBERS 5` is a two-operand
    /// multiple COLUMN clause whose first operand is a constant-name NUMBERS (refused at bind when none is declared),
    /// never the prefix `COLS NUMBERS`.</summary>
    private static void AssertVerdict(bool printed, DiagnosticBag diags, CobolParserCore.CompilationUnitContext? tree,
        Func<CobolParserCore.CompilationUnitContext, int> operandCount)
    {
        if (printed)
        {
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.Equal(1, operandCount(tree!));
        }
        else
            Assert.True(diags.HasErrors || operandCount(tree!) != 1,
                "a keyword prefix no format prints was accepted by the parser");
    }

    private static string Words(params string[] words) => string.Join(" ", words.Where(w => w.Length > 0));

    /// <summary>The 1-based source line of the COLUMN clause under test in <see cref="Program"/>.</summary>
    private const int ColumnLine = 16;

    private static string Program(string line = "LINE", string column = "COLUMN", string source = "SOURCE") =>
        "IDENTIFICATION DIVISION.\n" +
        "PROGRAM-ID. PB1221U.\n" +
        "ENVIRONMENT DIVISION.\n" +
        "INPUT-OUTPUT SECTION.\n" +
        "FILE-CONTROL.\n" +
        "    SELECT RPT ASSIGN TO \"pb1221u.rpt\".\n" +
        "DATA DIVISION.\n" +
        "FILE SECTION.\n" +
        "FD RPT REPORT IS R-1.\n" +
        "WORKING-STORAGE SECTION.\n" +
        "01 WS-X PIC X VALUE \"S\".\n" +
        "REPORT SECTION.\n" +
        "RD R-1.\n" +
        "01 DET-A TYPE DE.\n" +
        $"02 {line} PLUS 1.\n" +
        $"03 {column} 5 PIC X VALUE \"A\".\n" +
        $"03 COLUMN 7 PIC X {source} WS-X.\n" +
        "PROCEDURE DIVISION.\n" +
        "MAIN.\n" +
        "    STOP RUN.\n";

    private static (CobolParserCore.CompilationUnitContext? Tree, DiagnosticBag Diags) Parse(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), "pb1221_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            return (tree, diags);
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }

    private static IEnumerable<T> Descendants<T>(IParseTree node) where T : class
    {
        if (node is T hit) yield return hit;
        for (int i = 0; i < node.ChildCount; i++)
            foreach (var d in Descendants<T>(node.GetChild(i))) yield return d;
    }
}
