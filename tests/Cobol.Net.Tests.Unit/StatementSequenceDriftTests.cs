// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using Antlr4.Runtime;
using CobolNet.Binding.Procedure;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;
using Xunit;

namespace CobolNet.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE STATEMENT SEQUENCE IS ONE PARSE-TREE FACT, AND THE THREE SEQUENCE RULES ASK IT OF ONE VIEW (kb/Work
/// PB397, CLAUDE.md rule 5). ISO §14.9.14.3 SR1, §14.9.17.3 SR2 and §14.9.42.3 SR1 are stated over a sequence of
/// statements; <c>StatementPosition</c> reads the sequence from a statement's own ancestors, which is total ONLY
/// while the grammar writes a <c>statement</c> as an element of a <c>sentence</c> or a <c>statementBlock</c> and
/// nowhere else. This net fails the moment a third container gains a <c>statement</c> child, so the new container is
/// given a meaning in <c>StatementPosition.Of</c> — a rule that silently stopped being asked is exactly what
/// PB397 closed.
/// </summary>
public sealed class StatementSequenceDriftTests
{
    [Fact]
    public void OnlySentenceAndStatementBlock_HoldStatements()
    {
        var holders = typeof(Core).GetNestedTypes(BindingFlags.Public)
            .Where(t => typeof(ParserRuleContext).IsAssignableFrom(t))
            .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Any(m => m.Name == "statement" && m.ReturnType == typeof(Core.StatementContext[])))
            .Select(t => t.Name)
            .Order()
            .ToArray();
        Assert.Equal(["SentenceContext", "StatementBlockContext"], holders);
    }

    [Fact]
    public void Position_InASentence_CountsIndexAndLast()
    {
        var sentence = Parse(p => p.sentence(), "DISPLAY \"A\" GO TO P STOP RUN.");
        var statements = sentence.statement();
        Assert.Equal(3, statements.Length);
        for (int i = 0; i < 3; i++)
        {
            var position = StatementPosition.Of(statements[i]);
            Assert.Same(sentence, position.Sentence);
            Assert.Equal(i, position.Index);
            Assert.Equal(3, position.Count);
            Assert.Equal(i == 2, position.IsLast);
            Assert.False(position.IsSentenceByItself);
        }
        Assert.Same(statements[1], StatementPosition.Of(statements[0]).Following);
    }

    [Fact]
    public void Position_InAPhrase_IsNotASentence()
    {
        var iff = Parse(p => p.sentence(), "IF X = 1 EXIT END-IF.").statement(0).ifStatement();
        var exit = iff.statementBlock(0).statement(0);
        var position = StatementPosition.Of(exit);
        Assert.Null(position.Sentence);
        Assert.False(position.IsSentenceByItself);
        Assert.True(position.IsLast);
        Assert.Equal(0, position.SentencesInParagraph);
    }

    [Theory]
    [InlineData("P. EXIT.", 1)]
    [InlineData("P. DISPLAY \"A\". EXIT. DISPLAY \"B\".", 3)]
    public void SentencesInParagraph_CountsTheParagraphsSentences(string paragraph, int sentences)
    {
        var definition = Parse(p => p.paragraphDefinition(), paragraph);
        var exit = definition.sentence().Single(s => s.statement(0).exitStatement() is not null).statement(0);
        var position = StatementPosition.Of(exit);
        Assert.True(position.IsSentenceByItself);
        Assert.Equal(sentences, position.SentencesInParagraph);
    }

    private static T Parse<T>(Func<Core, T> rule, string text) where T : ParserRuleContext
    {
        var lexer = new CobolLexer(new AntlrInputStream(text));
        var parser = new Core(new CommonTokenStream(lexer)) { Edition = EditionInfo.Of(2023) };
        var tree = rule(parser);
        Assert.Equal(0, parser.NumberOfSyntaxErrors);
        return tree;
    }
}
