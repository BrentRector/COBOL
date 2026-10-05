// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §8.3.5 2): "The COBOL characters comma and semicolon, immediately followed by a space, are separators that may be
/// used anywhere the separator space is used" — read ONCE for every directive operand, by the directive-line parse
/// (<see cref="CompilerDirectiveLine"/>), and not by each stage's tokenizer (kb/Work PB1373). Every stage that splits its
/// operand on the space used to reject the legal spelling.
/// </summary>
public sealed class DirectiveOperandSeparatorTests
{
    [Theory]
    [InlineData("EC-SIZE, CHECKING ON", "EC-SIZE  CHECKING ON")]
    [InlineData("A; B", "A  B")]
    [InlineData("A,B", "A,B")]                  // no space after: not a separator (and a numeric literal 1,5 stays whole)
    [InlineData("A, B, C", "A  B  C")]
    [InlineData("\"A, B\", \"C\"", "\"A, B\"  \"C\"")]   // inside a character-string: untouched
    [InlineData("\"A\"\", B\", C", "\"A\"\", B\"  C")]   // a doubled quotation symbol stays inside the literal
    [InlineData("'A; B'; C", "'A; B'  C")]
    [InlineData("A,", "A,")]                    // ends the operand: §7.3.3 3), 4) — nothing but spaces may follow a directive
    [InlineData("", "")]
    public void SeparatorsAsSpaces_ReadsTheSeparatorsOutsideLiterals(string operand, string expected) =>
        Assert.Equal(expected, CompilerDirectiveLine.SeparatorsAsSpaces(operand));

    [Fact] // the directive-line parse applies it, after the inline comment is gone (a comma before it ends the operand).
    public void TryParse_ReadsTheSeparatorsOfTheOperand()
    {
        Assert.True(CompilerDirectiveLine.TryParse(">>COBOL-WORDS EQUATE \"A\", WITH \"B\" *> why, not", out var d));
        Assert.Equal("EQUATE \"A\"  WITH \"B\"", d.Operand);
        Assert.True(CompilerDirectiveLine.TryParse(">>COBOL-WORDS RESERVE \"A\", *> why", out var trailing));
        Assert.Equal("RESERVE \"A\",", trailing.Operand);
    }

    [Fact] // §7.3.25.2 — the TURN stage's operand, a sibling of COBOL-WORDS, takes the separator comma and semicolon too.
    public void Turn_TakesSeparatorCommaAndSemicolon()
    {
        var bag = new DiagnosticBag();
        var (_, events) = TurnDirectiveProcessor.Process(">>TURN EC-SIZE; EC-BOUND, CHECKING OFF\n", 2023, bag, "t.cob");
        Assert.False(bag.HasErrors);
        Assert.Equal(["EC-SIZE", "EC-BOUND"], events.Single().Names.Select(n => n.Ec));
    }

    [Fact] // §7.3.14.2 / §7.3.15.2 — and the FLAG stage's.
    public void Flag_TakesSeparatorCommaAndSemicolon()
    {
        var bag = new DiagnosticBag();
        var (_, events) = FlagDirectiveProcessor.Process(">>FLAG-14 ALL; ON\n", bag, "t.cob");
        Assert.False(bag.HasErrors);
        Assert.Single(events);
    }

    [Fact] // the closed-word rows too: the central COBOLNET1911 check reads the same operand, and the separator may stand
    // right after the directive word itself, where a space would (§8.3.5 2)).
    public void ClosedWordRow_TakesASeparatorAfterTheDirectiveWord()
    {
        var bag = new DiagnosticBag();
        ConditionalCompilationProcessor.Process(">>LISTING, OFF\n", Frontend.Frontend.LeftDirectives, bag, "t.cob", 2023);
        Assert.False(bag.HasErrors);
    }
}
