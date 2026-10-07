// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1406 — the operand and class model of a concatenation expression (ISO §8.8.3). Every arm of
/// <c>ConcatFolder.Classify</c> and of the pairwise class fold is pinned here by a case whose expected verdict is
/// derived from the rule it names, so a mutant of any one arm fails a case:
/// <list type="bullet">
/// <item>§8.8.3.1 writes literal-1 / literal-2, and the two WORDS that stand for a literal are operands: a
/// constant-name (§13.10.3 SR2) and a symbolic-character (a figurative constant, §12.3.7.4 GR11 a). They are admitted
/// in EVERY literal position, because the concatenation rule is one grammar alternative of the literal rule.</item>
/// <item>§8.8.3.3 GR1 is applied to each (accumulated, next) pair: the accumulated operand is a concatenation
/// expression from the first pair on, so a class mix after two figuratives is §8.8.3.2 SR1's COBOLNET1540.</item>
/// <item>A word that stands for no literal is COBOLNET2473; a symbolic-character operand in a §12.3.7.3 SR11
/// SPECIAL-NAMES literal is COBOLNET2474; a numeric constant-name and a figurative with no character value in the
/// pair's class are SR1's COBOLNET1540; an ALL figurative operand stays COBOLNET1541.</item>
/// </list>
/// The positive VALUES are pinned by the <c>2014/pb1406_concat_word_operands</c> golden; this class pins the
/// verdicts, one diagnostic per case.
/// </summary>
public sealed class ConcatOperandTests
{
    private const string SymbolicClause = "SPECIAL-NAMES.\n    SYMBOLIC CHARACTERS SYM-A IS 66";
    private const string Symbolic = SymbolicClause + ".";

    private static string Program(string id, string data, string proc, string special = "", string idAs = "") =>
        ">>SOURCE FORMAT FREE\nIDENTIFICATION DIVISION.\nPROGRAM-ID. " + id + idAs + ".\n"
        + (special.Length > 0 ? "ENVIRONMENT DIVISION.\nCONFIGURATION SECTION.\n" + special + "\n" : "")
        + "DATA DIVISION.\nWORKING-STORAGE SECTION.\n" + data + "\nPROCEDURE DIVISION.\nMAIN.\n" + proc
        + "\n    STOP RUN.\n";

    /// <summary>A constant-name or symbolic-character operand in each position a literal can be written in: a
    /// statement operand (both operand orders), a VALUE clause, a level-88 VALUE, another constant's literal-1, a
    /// DISPLAY operand, a relation operand and a boolean relation (the routing predicate
    /// <c>IsBooleanValueOperand</c> must class the chain boolean through the constant).</summary>
    [Theory]
    [InlineData("01", "MOVE literal & symbolic", "01 W PIC X(4).", "    MOVE \"X\" & SYM-A TO W.", Symbolic)]
    [InlineData("02", "MOVE symbolic & literal", "01 W PIC X(4).", "    MOVE SYM-A & \"X\" TO W.", Symbolic)]
    [InlineData("03", "MOVE two symbolics (GR1b)", "01 W PIC X(4).", "    MOVE SYM-A & SYM-A TO W.", Symbolic)]
    [InlineData("04", "MOVE literal & constant", "01 K CONSTANT AS \"Q\".\n01 W PIC X(4).", "    MOVE \"X\" & K TO W.", "")]
    [InlineData("05", "MOVE national constant", "01 K CONSTANT AS N\"Q\".\n01 W PIC N(4).", "    MOVE K & SPACE & N\"R\" TO W.", "")]
    [InlineData("06", "VALUE", "01 K CONSTANT AS \"Q\".\n01 W PIC X(4) VALUE \"A\" & K.", "    DISPLAY W.", "")]
    [InlineData("07", "level-88 VALUE", "01 K CONSTANT AS \"Q\".\n01 W PIC X(2).\n   88 C VALUE \"A\" & K.", "    DISPLAY W.", "")]
    [InlineData("08", "CONSTANT literal-1", "01 K CONSTANT AS \"Q\".\n01 K2 CONSTANT AS K & \"R\".", "    DISPLAY K2.", "")]
    [InlineData("09", "DISPLAY", "01 K CONSTANT AS \"Q\".", "    DISPLAY K & \"R\".", "")]
    [InlineData("10", "relation", "01 K CONSTANT AS \"Q\".\n01 W PIC X(2).", "    IF W = K & \"R\" DISPLAY W END-IF.", "")]
    [InlineData("11", "boolean relation", "01 K CONSTANT AS B\"1\".\n01 B PIC 1(2).", "    IF B = K & ZERO DISPLAY B END-IF.", "")]
    public void WordOperand_IsALiteralInEveryLiteralPosition(string id, string position, string data, string proc,
        string special)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("PB1406A" + id, data, proc, special), 2023);
        Assert.True(ok, $"[{position}] a constant-name / symbolic-character is a concatenation operand (§8.8.3.1; "
            + "§13.10.3 SR2; §12.3.7.4 GR11):\n" + string.Join("\n", errors));
    }

    /// <summary>Each refusal arm, with the one code its rule owns.</summary>
    [Theory]
    [InlineData("01", "two figuratives then national (GR1b, SR1)", "01 W PIC N(4).", "    MOVE SPACE & SPACE & N\"AB\" TO W.", "", "", "COBOLNET1540")]
    [InlineData("02", "two figuratives then boolean (GR1b, SR1)", "01 W PIC 1(4).", "    MOVE ZERO & ZERO & B\"1\" TO W.", "", "", "COBOLNET1540")]
    [InlineData("03", "literal class mix after a pair", "01 W PIC X(4).", "    MOVE \"A\" & SPACE & N\"B\" TO W.", "", "", "COBOLNET1540")]
    [InlineData("04", "numeric constant-name", "01 K CONSTANT AS 5.\n01 W PIC X(4).", "    MOVE \"X\" & K TO W.", "", "", "COBOLNET1540")]
    [InlineData("05", "symbolic-character in class boolean", "01 W PIC 1(4).", "    MOVE B\"1\" & SYM-A TO W.", Symbolic, "", "COBOLNET1540")]
    [InlineData("06", "ALL symbolic-character", "01 W PIC X(4).", "    MOVE \"X\" & ALL SYM-A TO W.", Symbolic, "", "COBOLNET1541")]
    [InlineData("07", "data-name", "01 Y PIC X(2).\n01 W PIC X(4).", "    MOVE \"X\" & Y TO W.", "", "", "COBOLNET2473")]
    [InlineData("08", "undefined word", "01 W PIC X(4).", "    MOVE \"X\" & NOPE TO W.", "", "", "COBOLNET2473")]
    [InlineData("09", "constant-name in PROGRAM-ID AS (no table yet)", "01 K CONSTANT AS \"Q\".", "    DISPLAY K.", "", " AS \"P\" & K", "COBOLNET2473")]
    [InlineData("10", "SR11 CLASS literal-5", "01 W PIC X.", "    DISPLAY W.", SymbolicClause + "\n    CLASS CC IS \"B\" & SYM-A.", "", "COBOLNET2474")]
    [InlineData("11", "SR11 ALPHABET literal-1", "01 W PIC X.", "    DISPLAY W.", SymbolicClause + "\n    ALPHABET AL IS \"B\" & SYM-A.", "", "COBOLNET2474")]
    public void RefusedOperand_DrawsItsRulesCode(string id, string what, string data, string proc, string special,
        string idAs, string code)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("PB1406R" + id, data, proc, special, idAs), 2023);
        Assert.False(ok, $"[{what}] shall be rejected with {code}");
        Assert.Single(errors, e => e.Contains(code));
    }
}
