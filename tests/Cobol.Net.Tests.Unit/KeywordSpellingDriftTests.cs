// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <b>ONE TOKEN PER RESERVED WORD</b> (kb/Work PB510) and the §13.18.40.3 SR7 separator-period rule (kb/Work
/// PB569) — the two places the lexer used to erase a distinction the standard draws.
/// <para>The lexer folded ZERO/ZEROS/ZEROES (and each figurative singular/plural pair) into one token, so every
/// general format that prints ONE of them as a keyword (§5.2.2) — BLANK WHEN ZERO (§13.18.8.2), the sign
/// condition's ZERO (§8.8.4.7.2), OPTIONS INITIALIZE's BINARY ZEROES / HIGH-VALUES / LOW-VALUES / SPACES
/// (§11.9.10.2) — accepted every spelling. The spellings are now distinct tokens and their interchangeability is
/// written ONCE, in the grammar's figurative-word rules; these tests keep that shape from regrowing and prove the
/// refusal is live in both directions (a gate that can only go green is not evidence).</para>
/// </summary>
public sealed class KeywordSpellingDriftTests
{
    /// <summary>⛔ THE DRIFT GATE, derived from the spec tables and the LEXER ITSELF: no two §8.9 ∪ §8.10 words
    /// share a token type, except PIC / PICTURE — the one pair the standard makes equivalent everywhere it is
    /// written (§13.18.40.3 SR5 "PIC is an abbreviation for PICTURE"). A lexer rule that folds two reserved words
    /// again lands here, naming the words.</summary>
    [Fact]
    public void NoTwoReservedWords_ShareATokenType_ExceptPicAndPicture()
    {
        var byType = new Dictionary<int, List<string>>();
        foreach (string w in CobolWordsReachDriftTests.SpecWords())
            if (CobolKeywordTokens.TryTokenType(w, out int t) && t != CobolKeywordTokens.IdentifierType)
                (byType.TryGetValue(t, out var l) ? l : byType[t] = []).Add(w.ToUpperInvariant());
        Assert.True(byType.Count > 400, $"only {byType.Count} keyword token types reached — the probe broke");

        var folded = byType.Values.Where(ws => ws.Count > 1)
            .Select(ws => string.Join("/", ws.Order(StringComparer.Ordinal))).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(["PIC/PICTURE"], folded);
    }

    /// <summary>The interchangeability is written ONCE per family — the FIRST set of its grammar rule is exactly
    /// the §8.3.3.6.2 alternatives. The error strategy's COBOLNET2418 arm reads the families from here, so a
    /// family rule that lost a spelling would silently stop naming it.</summary>
    [Fact]
    public void FigurativeWordRules_AreTheSpecFamilies()
    {
        var parser = new CobolParserCore(new Antlr4.Runtime.CommonTokenStream(new CobolLexer(new Antlr4.Runtime.AntlrInputStream(""))));
        var families = CobolErrorStrategy.FigurativeFamilies(parser)
            .Select(f => string.Join("|", f.ToIntegerList().Select(t => CobolLexer.DefaultVocabulary.GetLiteralName(t)!.Trim('\''))
                .Order(StringComparer.Ordinal)))
            .ToList();
        Assert.Equal(["ZERO|ZEROES|ZEROS", "SPACE|SPACES", "HIGH-VALUE|HIGH-VALUES", "LOW-VALUE|LOW-VALUES", "QUOTE|QUOTES"],
            families);
    }

    public static TheoryData<string, string, string> KeywordPositions => new()
    {
        // position, written, expected diagnostic ("" = compiles)
        { "BLANK WHEN ZERO", "01 B PIC 9(3) BLANK WHEN ZERO.", "" },
        { "BLANK ZERO", "01 B PIC 9(3) BLANK ZERO.", "" },
        { "BLANK WHEN ZEROS", "01 B PIC 9(3) BLANK WHEN ZEROS.", "COBOLNET2418" },
        { "BLANK ZEROES", "01 B PIC 9(3) BLANK ZEROES.", "COBOLNET2418" },
    };

    /// <summary>Both directions at the data-division keyword position: the printed spelling compiles (with and
    /// without the optional WHEN), a sibling spelling is refused BY NAME rather than as a bare syntax error.</summary>
    [Theory]
    [MemberData(nameof(KeywordPositions))]
    public void BlankWhenZero_AdmitsOnlyThePrintedSpelling(string _, string entry, string expected)
    {
        var (ok, errors) = Compile($"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. KSD1.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   {entry}
                   PROCEDURE DIVISION.
                       MOVE 0 TO B
                       STOP RUN.
            """, 2023);
        if (expected.Length == 0) Assert.True(ok, string.Join("\n", errors));
        else Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal));
    }

    /// <summary>The procedure-division keyword position, where ANTLR's fallback reports the failure at the IS /
    /// NOT before the word, or at the word with no ZERO in the expected set — the COBOLNET2418 sign arm must name
    /// the misspelled keyword in both shapes; the relation spelled with the figurative constant still compiles.</summary>
    [Theory]
    [InlineData("N IS ZERO", "")]
    [InlineData("N IS NOT ZERO", "")]
    [InlineData("N = ZEROS", "")]
    [InlineData("N NOT = ZEROES", "")]
    [InlineData("N IS ZEROS", "COBOLNET2418")]
    [InlineData("N IS NOT ZEROES", "COBOLNET2418")]
    [InlineData("N NOT ZEROS", "COBOLNET2418")]
    public void SignCondition_AdmitsOnlyZero(string condition, string expected)
    {
        var (ok, errors) = Compile($"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. KSD2.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 N PIC S9(3) VALUE 0.
                   PROCEDURE DIVISION.
                       IF {condition} DISPLAY "Y" END-IF
                       STOP RUN.
            """, 2023);
        if (expected.Length == 0) Assert.True(ok, string.Join("\n", errors));
        else Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal));
    }

    /// <summary>§13.18.40.3 SR7 in both directions (kb/Work PB569): a trailing ',' or '.' followed immediately
    /// by the separator period is legal wherever the clause sits last; followed by a separator comma or
    /// semicolon it is refused — the ',' arm AND the '.' arm, which the refutation had called structural.</summary>
    [Theory]
    [InlineData("01 W PIC 999,.", "")]
    [InlineData("01 W PIC 999..", "")]
    [InlineData("01 W USAGE DISPLAY PIC 999,.", "")]
    [InlineData("01 W PIC 999, VALUE ZERO.", "")]
    [InlineData("01 W PIC 999,, USAGE DISPLAY.", "COBOLNET2419")]
    [InlineData("01 W PIC 999,; VALUE ZERO.", "COBOLNET2419")]
    [InlineData("01 W PIC 999., VALUE ZERO.", "COBOLNET2419")]
    [InlineData("01 W PIC 999.; VALUE ZERO.", "COBOLNET2419")]
    public void PictureTrailingSymbol_IsFollowedByTheSeparatorPeriod(string entry, string expected)
    {
        var (ok, errors) = Compile($"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. KSD3.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   {entry}
                   PROCEDURE DIVISION.
                       STOP RUN.
            """, 2023);
        if (expected.Length == 0) Assert.True(ok, string.Join("\n", errors));
        else Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal));
    }

    private static (bool Ok, IReadOnlyList<string> Errors) Compile(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Ksd_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success, r.Success ? [] : [.. r.Errors]);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
