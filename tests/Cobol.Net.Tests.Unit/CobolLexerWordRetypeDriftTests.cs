// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE <c>&gt;&gt;COBOL-WORDS</c> RETYPE IS APPLIED BY THE LEXER, AND NO KEYWORD RULE ACTS ON ITS OWN TOKEN (kb/Work
/// PB1372, PB1915; ISO §7.3.10.4 GR2/GR3/GR4). Every decision <c>CobolLexer</c> takes from a keyword — PIC's switch to
/// PICMODE, the FUNCTION argument region, the SUBSCRIPT trigger — is frozen at lex time, so a retype that ran after
/// lexing could reach none of them. <c>NextToken</c> retypes each token FIRST and acts on the EFFECTIVE word, which holds
/// only while no keyword rule carries a lexer command of its own (a command runs before <c>NextToken</c> can ask the
/// directive what the word is). This class measures both halves: the behaviour (a synonym of PIC/FUNCTION acts as the
/// keyword, a de-reserved PICTURE is a data-name) and the structure (no default-mode keyword rule has a mode command).
/// </summary>
public sealed class CobolLexerWordRetypeDriftTests
{
    private static List<IToken> Lex(string text, params CobolWordsOp[] ops)
    {
        var lexer = new CobolLexer(new AntlrInputStream(text));
        (TokenRetypes.None with { CobolWords = new CobolWordsMap(ops) }).PrimeLexer(lexer, EditionInfo.Latest);
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        return [.. stream.GetTokens().Where(t => t.Channel == TokenConstants.DefaultChannel && t.Type != TokenConstants.EOF)];
    }

    private static int[] Types(List<IToken> tokens) => [.. tokens.Select(t => t.Type)];

    [Fact]
    public void ASynonymOfPic_EntersPicMode()
    {
        var tokens = Lex("01 X PX 9(3) VALUE 7.\n", new CobolWordsOp(CobolWordsAction.Equate, "PIC", "PX", 0));
        Assert.Contains(tokens, t => t.Type == CobolLexer.PIC && t.Text == "PIC");        // re-spelled canonically
        Assert.Contains(tokens, t => t.Type == CobolLexer.PIC_STRING && t.Text == "9(3)");   // the picture string is ONE token
    }

    [Fact]
    public void ASubstituteForPicture_EntersPicMode_AndPictureIsADataName()
    {
        var tokens = Lex("01 X LOOKS 9(3) VALUE 7.\n01 PICTURE PIC X(3).\n",
            new CobolWordsOp(CobolWordsAction.Substitute, "PICTURE", "LOOKS", 0));
        Assert.Equal(2, tokens.Count(t => t.Type == CobolLexer.PIC));
        Assert.Contains(tokens, t => t.Type == CobolLexer.PIC_STRING && t.Text == "9(3)");
        Assert.Contains(tokens, t => t.Type == CobolLexer.IDENTIFIER && t.Text == "PICTURE");   // literal-4 is a user word now
    }

    [Fact]
    public void AnUndefinedPicture_IsADataName_AndSwallowsNothing()
    {
        var tokens = Lex("01 PICTURE PIC X(3) VALUE \"P\".\n", new CobolWordsOp(CobolWordsAction.Undefine, "PICTURE", null, 0));
        Assert.Equal([CobolLexer.INTEGERLIT, CobolLexer.IDENTIFIER, CobolLexer.PIC], Types(tokens).Take(3));
        Assert.Contains(tokens, t => t.Type == CobolLexer.PIC_STRING && t.Text == "X(3)");
    }

    /// <summary>One token type carries two COBOL words (<c>PIC : 'PIC' | 'PICTURE'</c>) and GR3 de-reserves exactly the
    /// one the literal names — the retype keeps the text test.</summary>
    [Fact]
    public void UndefiningPic_LeavesPictureAKeyword()
    {
        var tokens = Lex("01 PIC PICTURE X(3).\n", new CobolWordsOp(CobolWordsAction.Undefine, "PIC", null, 0));
        Assert.Equal(1, tokens.Count(t => t.Type == CobolLexer.PIC));
        Assert.Contains(tokens, t => t.Type == CobolLexer.IDENTIFIER && t.Text == "PIC");
        Assert.Contains(tokens, t => t.Type == CobolLexer.PIC_STRING && t.Text == "X(3)");
    }

    [Fact]
    public void ASynonymOfFunction_OpensTheArgumentRegion()
    {
        var tokens = Lex("DISPLAY FN UPPER-CASE(X).\n", new CobolWordsOp(CobolWordsAction.Equate, "FUNCTION", "FN", 0));
        Assert.Contains(tokens, t => t.Type == CobolLexer.FUNCTION);
        Assert.Contains(tokens, t => t.Type == CobolLexer.FNARG_LPAREN);
        Assert.Contains(tokens, t => t.Type == CobolLexer.FNARG_RPAREN);
    }

    [Fact]
    public void AnUndefinedKeyword_UsedAsASubscriptedDataName_OpensASubscript()
    {
        // The seam this replaces (a set of de-reserved types the lexer consulted at '('): the retype made it redundant.
        var tokens = Lex("COMPUTE MOVE(2) = 5.\n", new CobolWordsOp(CobolWordsAction.Undefine, "MOVE", null, 0));
        Assert.Contains(tokens, t => t.Type == CobolLexer.IDENTIFIER && t.Text == "MOVE");
        // The '(' after a data-name opens SUBSCRIPT mode: its content is lexed as SUB_ tokens, exactly as after any
        // ordinary data-name — and NOT as it is after a reserved MOVE, which opens none.
        string Shape(List<IToken> ts) => string.Join(" ", ts.Skip(2).Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t.Type)));
        Assert.Equal(Shape(Lex("COMPUTE XDATA(2) = 5.\n")), Shape(tokens));
        Assert.NotEqual(Shape(Lex("COMPUTE MOVE(2) = 5.\n")), Shape(tokens));
    }

    [Fact]
    public void WithoutADirective_TheLexerRetypesNothing()
    {
        var tokens = Lex("01 PX PIC 9(3).\n");
        Assert.Equal(CobolLexer.IDENTIFIER, tokens[1].Type);
        Assert.Equal(1, tokens.Count(t => t.Type == CobolLexer.PIC));
    }

    /// <summary>The structure that keeps "the next case automatic": a DEFAULT-mode rule that is a COBOL keyword must not
    /// carry <c>pushMode</c>, <c>popMode</c> or <c>mode</c> — the lexer would switch modes on a word the directive may have
    /// made a data-name, or miss one it made a keyword. A word that must switch modes does it in <c>NextToken</c>, after
    /// the retype (PIC is the one).</summary>
    [Fact]
    public void NoDefaultModeKeywordRule_CarriesAModeCommand()
    {
        string path = TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolLexer.g4");
        Assert.True(File.Exists(path), $"lexer grammar missing: {path}");
        string g4 = File.ReadAllText(path);
        g4 = Regex.Replace(g4, @"//[^\r\n]*", "");
        g4 = Regex.Replace(g4, @"/\*.*?\*/", "", RegexOptions.Singleline);
        int firstMode = Regex.Match(g4, @"^mode\s+\w+\s*;", RegexOptions.Multiline).Index;
        string defaultMode = g4[..firstMode];

        // A keyword rule: NAME : 'WORD' (| 'WORD')* [-> commands] ; — the literal is a COBOL word (letters, digits, hyphen).
        var keywordRule = new Regex(
            @"^(?<name>[A-Z][A-Z0-9_]*)\s*:\s*\(?\s*'[A-Za-z][A-Za-z0-9-]*'(?:\s*\|\s*'[A-Za-z][A-Za-z0-9-]*')*\s*\)?\s*(?<cmd>->[^;]*)?;",
            RegexOptions.Multiline);
        var rules = keywordRule.Matches(defaultMode).ToList();
        Assert.True(rules.Count > 400, $"the keyword-rule scan went vacuous: {rules.Count} rules");
        Assert.Contains(rules, m => m.Groups["name"].Value == "PIC");   // the rule this guard exists for

        var offenders = rules.Where(m => Regex.IsMatch(m.Groups["cmd"].Value, @"\b(pushMode|popMode|mode)\b"))
            .Select(m => m.Groups["name"].Value).ToList();
        Assert.True(offenders.Count == 0,
            "a default-mode keyword rule carries a lexer mode command, which runs before CobolLexer.NextToken can apply "
            + $">>COBOL-WORDS (kb/Work PB1372); switch modes in NextToken instead: {string.Join(", ", offenders)}");
    }
}
