// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1660 — a directive operand is split into words on the COBOL character space and nothing else (ISO §8.3.5
/// 1): "The COBOL character space is a separator"; <c>cite.py --check 8.3.5 "The COBOL character space is a
/// separator"</c> → OK 1)). The no-break space U+00A0 only LOOKS like one, so a directive written with it between two
/// words has ONE operand word that is no word of the directive's format, and the stage says so — it used to read two
/// words and accept the directive. Each stage that splits an operand is asked, because the defect was one copy of the
/// split per stage.
/// </summary>
public sealed class DirectiveOperandSpaceTests
{
    private static readonly string Nbsp = ((char)0x00A0).ToString();

    [Fact] // >>TURN: EC-SIZE<NBSP>CHECKING is one word, which is no exception-name — never "EC-SIZE" then "CHECKING".
    public void Turn_DoesNotSplitOperandOnNoBreakSpace()
    {
        var bag = new DiagnosticBag();
        TurnDirectiveProcessor.Process($">>TURN EC-SIZE{Nbsp}CHECKING ON\n", 2023, bag, "t.cob");
        Assert.True(bag.HasErrors, "an operand word joined by U+00A0 was split into two words");
    }

    [Fact] // The control: the same directive with the COBOL character space is accepted.
    public void Turn_SplitsOperandOnTheCobolCharacterSpace()
    {
        var bag = new DiagnosticBag();
        TurnDirectiveProcessor.Process(">>TURN EC-SIZE CHECKING ON\n", 2023, bag, "t.cob");
        Assert.False(bag.HasErrors);
    }

    [Fact] // >>COBOL-WORDS: UNDEFINE<NBSP>"MOVE" is not UNDEFINE "MOVE" — no word is de-reserved by it.
    public void CobolWords_DoesNotSplitOperandOnNoBreakSpace()
    {
        var bag = new DiagnosticBag();
        var (_, map, _) = CobolWordsDirectiveProcessor.Process($">>COBOL-WORDS UNDEFINE{Nbsp}\"MOVE\"\n", bag, "t.cob");
        Assert.DoesNotContain("MOVE", map.DeReserved);
    }

    [Fact] // The line classifier of the format detector: a line of U+00A0 alone is program text, not a blank line (§6.3.6).
    public void BlankLine_IsOnlyCobolSpaces()
    {
        Assert.True(CobolNet.Editions.CobolSpace.IsBlank("     "));
        Assert.False(CobolNet.Editions.CobolSpace.IsBlank("  " + Nbsp));
    }
}
