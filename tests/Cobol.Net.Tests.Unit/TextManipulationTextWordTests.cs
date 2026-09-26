// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The text-manipulation stage (COPY + REPLACE, ISO §7.2) over §7.2.2.5 text-words — kb/Work PB1350 (what a
/// text-word is), PB1351 (how two text-words compare, §7.2.3.4 9) c) / §7.2.4.4 8) c)) and PB1354 (the COPY
/// statement is PARSED from its §7.2.3.2 general format, not hand-scanned to the next '.'). Every expectation is
/// derived from the cited rule, and every case goes through the real merged driver
/// (<see cref="ConditionalCompilationProcessor.ProcessWithCopy"/>), so COPY and REPLACE are exercised on the one path
/// the compiler runs.
/// </summary>
public sealed class TextManipulationTextWordTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB1350_" + Guid.NewGuid().ToString("N")[..8]);

    public TextManipulationTextWordTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ } }

    private void Copybook(string name, string content) => File.WriteAllText(Path.Combine(_dir, name), content);

    private (string Text, DiagnosticBag Diags) Run(string mainText, int edition = 2023)
    {
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, "t.cob", strict: true, dialectLevel: edition, permissive: false);
        string text = ConditionalCompilationProcessor.ProcessWithCopy(mainText, _dir, copy,
            CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: edition);
        return (text, bag);
    }

    private static string Codes(DiagnosticBag bag) => string.Join(",", bag.Diagnostics.Select(d => d.Code));

    private static void Clean(DiagnosticBag bag) => Assert.False(bag.HasErrors, string.Join("\n", bag.Diagnostics));

    // ---- PB1350: text-word formation (§7.2.2.5, §8.3.5) ----

    [Fact] // §7.2.2.5 1): the colon is a separator, so 1:5 holds the text-words 1 : 5 and ==5== matches.
    public void Colon_SeparatesTextWords()
    {
        var (text, bag) = Run("REPLACE ==5== BY ==2==.\n    MOVE WA(1:5) TO WB.\n");
        Clean(bag);
        Assert.Contains("WA(1:2)", text);
    }

    [Fact] // §8.3.5 3) / 2): a period or comma not followed by a space is no separator — ZZ.ZZ and Z,ZZ9 are ONE word.
    public void PictureStrings_AreSingleTextWords()
    {
        var (text, bag) = Run("REPLACE ==ZZ== BY ==99== ==ZZ9== BY ==999==.\n01 A PIC ZZ.ZZ.\n01 B PIC Z,ZZ9.\n01 C PIC ZZ9.\n");
        Clean(bag);
        Assert.Contains("PIC ZZ.ZZ.", text);
        Assert.Contains("PIC Z,ZZ9.", text);
        Assert.Contains("PIC 999.", text);   // the control: a whole text-word still matches
    }

    [Fact] // §7.2.2.5 2): a literal INCLUDING its opening delimiter (X" N" per §8.3.5 5)) and its doubled quotes is one word.
    public void Literals_WithPrefixOrDoubledQuote_AreSingleTextWords()
    {
        var (text, bag) = Run("REPLACE ==\"41\"== BY ==\"42\"== ==\"PQ\"== BY ==\"RS\"== ==\"Q\"== BY ==\"Z\"==.\n"
            + "    DISPLAY X\"41\" N\"PQ\" \"P\"\"Q\" nx\"0041\".\n");
        Clean(bag);
        Assert.Contains("DISPLAY X\"41\" N\"PQ\" \"P\"\"Q\" nx\"0041\".", text);
    }

    [Fact] // §7.2.3.4 9) c) 2.: each concatenation operand and operator is a separate text-word — a prefixed operand too.
    public void ConcatenationOperands_AreSeparateTextWords()
    {
        var (text, bag) = Run("REPLACE ==\"CD\"== BY ==\"ZZ\"== ==X\"42\"== BY ==X\"43\"==.\n    DISPLAY \"AB\"&\"CD\" \"A\"&X\"42\".\n");
        Clean(bag);
        Assert.Contains("\"AB\"&\"ZZ\" \"A\"&X\"43\".", text);
    }

    [Fact] // §7.2.3.4 9) c) 6.: a comment is a space — whatever its text, including a programmer's "*> DEBUG: …".
    public void Comment_ReadingLikeTheDebugCarrier_IsASpace()
    {
        var (text, bag) = Run("REPLACE ==DISPLAY KK== BY ==DISPLAY W1==.\n    DISPLAY\n*> DEBUG: note to self\n    KK.\n");
        Clean(bag);
        Assert.Contains("DISPLAY W1", text);
        Assert.DoesNotContain("KK", text.Replace("*> DEBUG: note to self", ""));
    }

    [Fact] // The fixed-form debugging line keeps its COBOL-85 matching rule through the noncharacter carrier.
    public void FixedFormDebuggingLine_TextWordsTakePartInMatching()
    {
        var free = ReferenceFormatProcessor.ConvertFixedToFreeMapped(
            "000100     DISPLAY\n000200D    KK.\n", "t.cob").Text;
        Assert.Contains(ReferenceFormatProcessor.DebugLineCarrier + "    KK.", free);
        var (text, bag) = Run("REPLACE ==DISPLAY KK== BY ==DISPLAY W1==.\n" + free);
        Clean(bag);
        Assert.Contains("DISPLAY W1.", text);   // the match spans the debugging line's text-word
        Assert.DoesNotContain("KK", text);
    }

    [Fact] // §7.2.4.4 8) c) 1.: a separator comma or semicolon is a space — on either side of the comparison.
    public void SeparatorCommaAndSemicolon_AreSpaces()
    {
        var (text, bag) = Run("REPLACE ==MOVE A , B== BY ==MOVE C TO D==.\n    MOVE A; B.\n");
        Clean(bag);
        Assert.Contains("MOVE C TO D.", text);
    }

    [Fact] // §7.2.4.4 8) b): LEADING compares against ONE text-word — each side of a colon is its own.
    public void LeadingPartialWord_AppliesOnEachSideOfAColon()
    {
        var (text, bag) = Run("REPLACE LEADING ==OLD-== BY ==NEW-==.\n    DISPLAY TBL(OLD-A:OLD-B).\n");
        Clean(bag);
        Assert.Contains("TBL(NEW-A:NEW-B)", text);
    }

    [Fact] // §7.2.3.4 9) f): a non-pseudo-text literal-4 (pre-2023) is read as ONE literal, doubled quote and all.
    public void CopyReplacing_Literal4WithDoubledQuote_Before2023()
    {
        Copybook("bk4.cpy", "    DISPLAY ZZ-TXT.\n");
        var (text, bag) = Run("COPY bk4 REPLACING ZZ-TXT BY \"A\"\"B\".\n", edition: 2014);
        Clean(bag);
        Assert.Contains("DISPLAY \"A\"\"B\".", text);
    }

    // ---- PB1351: text-word comparison (§7.2.3.4 9) c) 3.–4. / §7.2.4.4 8) c) 3.–4.) ----

    [Fact] // c) 3.: letter case is significant in the content of "…" and N"…" — =="abc"== does not match "ABC".
    public void LiteralContent_IsCaseSensitive()
    {
        var (text, bag) = Run("REPLACE ==\"abc\"== BY ==\"ZZZ\"==.\n    DISPLAY \"ABC\" \"abc\".\n");
        Clean(bag);
        Assert.Contains("DISPLAY \"ABC\" \"ZZZ\".", text);
    }

    [Fact] // c) 3.: … but not in a hexadecimal format, nor in the prefix, nor in a character-string.
    public void HexLiteralAndWords_AreCaseInsensitive()
    {
        var (text, bag) = Run("REPLACE ==x\"4a\"== BY ==X\"4B\"== ==old-name== BY ==NEW-NAME==.\n    DISPLAY X\"4A\" OLD-NAME.\n");
        Clean(bag);
        Assert.Contains("DISPLAY X\"4B\" NEW-NAME.", text);
    }

    [Fact] // c) 4. a.: the two representations of the quotation symbol match; c) 4. b.: a doubled quote is one.
    public void QuoteRepresentations_AndDoubledQuotes_Match()
    {
        var (text, bag) = Run("REPLACE ==\"QQ\"== BY ==\"RR\"== =='p\"q'== BY ==\"OO\"==.\n    DISPLAY 'QQ' \"p\"\"q\".\n");
        Clean(bag);
        Assert.Contains("DISPLAY \"RR\" \"OO\".", text);
    }

    [Fact] // The COPY REPLACING arm shares the comparison (the same rules, §7.2.3.4 9) c)).
    public void CopyReplacing_ComparesLiteralsByTheSameRules()
    {
        Copybook("lits.cpy", "01 LV PIC X(3) VALUE \"abc\".\n01 LQ PIC X(3) VALUE 'xyz'.\n01 HX PIC X VALUE X\"41\".\n");
        var (text, bag) = Run("COPY lits REPLACING ==\"ABC\"== BY ==\"MMM\"== ==\"xyz\"== BY ==\"NNN\"== ==\"41\"== BY ==\"42\"==.\n");
        Clean(bag);
        Assert.Contains("VALUE \"abc\"", text);
        Assert.Contains("VALUE \"NNN\"", text);
        Assert.Contains("VALUE X\"41\"", text);
    }

    // ---- PB1354: the COPY statement parsed from its general format (§7.2.3.2) ----

    [Theory] // [SUPPRESS [PRINTING]] precedes the REPLACING phrase — the phrase is applied, not lost.
    [InlineData("SUPPRESS PRINTING")]
    [InlineData("SUPPRESS")]
    public void SuppressThenReplacing_AppliesTheReplacing(string suppress)
    {
        Copybook("bk1.cpy", "01 AA-X PIC X(5) VALUE \"HELLO\".\n");
        var (text, bag) = Run($"COPY bk1 {suppress} REPLACING ==AA-X== BY ==BB-Y==.\n");
        Clean(bag);
        Assert.Contains("BB-Y", text);
        Assert.DoesNotContain("AA-X", text);
    }

    [Fact] // GR6: the statement ends at its SEPARATOR period — a period inside pseudo-text is part of the operand.
    public void PeriodInsidePseudoText_DoesNotEndTheStatement()
    {
        Copybook("bk2.cpy", "01 AV PIC X(3) VALUE \"AAA\".\n");
        var (text, bag) = Run("COPY bk2 REPLACING ==VALUE \"AAA\".== BY ==VALUE \"ZZZ\".==.\n    DISPLAY AV.\n");
        Clean(bag);
        Assert.Contains("VALUE \"ZZZ\".", text);
        Assert.Contains("DISPLAY AV.", text);
    }

    [Fact] // A pseudo-text delimiter inside a literal is literal content, not the operand's end.
    public void PseudoTextDelimiterInsideLiteral_IsContent()
    {
        Copybook("bk3.cpy", "    DISPLAY ZZ-TXT.\n");
        var (text, bag) = Run("COPY bk3 REPLACING ==ZZ-TXT== BY ==\"A==B\"==.\n");
        Clean(bag);
        Assert.Contains("DISPLAY \"A==B\".", text);
    }

    [Theory] // Each header outside the general format is COBOLNET2449 (and names the problem), never a silent compile.
    [InlineData("COPY bk1 GARBAGE MORE.")]
    [InlineData("COPY bk1 PRINTING.")]
    [InlineData("COPY bk1 REPLACING.")]
    [InlineData("COPY bk1 REPLACING ==AA-X== ==BB-Y==.")]
    [InlineData("COPY bk1 REPLACING ==AA-X== BY ==BB-Y==")]
    [InlineData("COPY bk1 REPLACING ==AA-X BY ==BB-Y.")]
    [InlineData("COPY.")]
    public void MalformedHeader_DrawsSyntaxError(string statement)
    {
        Copybook("bk1.cpy", "01 AA-X PIC X(5) VALUE \"HELLO\".\n");
        var (_, bag) = Run(statement + "\n");
        Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET2449" && d.IsError);
    }

    [Theory] // §7.2.3.3 SR4 (concatenation / figurative constant) and SR5 (alphanumeric literals only) — COBOLNET2450.
    [InlineData("COPY \"BK\" & \"1\".")]
    [InlineData("COPY SPACE.")]
    [InlineData("COPY ALL \"X\".")]
    [InlineData("COPY N\"bk1\".")]
    [InlineData("COPY B\"01\".")]
    [InlineData("COPY bk1 OF ZERO.")]
    public void IllegalLiteralForm_DrawsSr4OrSr5(string statement)
    {
        Copybook("bk1.cpy", "01 AA-X PIC X(5) VALUE \"HELLO\".\n");
        var (_, bag) = Run(statement + "\n");
        Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET2450" && d.IsError);
    }

    [Theory] // SR5's alphanumeric literal — plain or hexadecimal — names the library text by its VALUE.
    [InlineData("COPY \"bk1\".")]
    [InlineData("COPY 'bk1'.")]
    [InlineData("COPY X\"626B31\".")]
    public void AlphanumericLiteral_NamesTheLibraryText(string statement)
    {
        Copybook("bk1.cpy", "01 AA-X PIC X(5) VALUE \"HELLO\".\n");
        var (text, bag) = Run(statement + "\n");
        Clean(bag);
        Assert.Contains("AA-X", text);
    }

    [Theory] // SR1: a COPY statement within a COPY statement (its REPLACING operands or its header) — COBOLNET2451,
             // and the inner COPY is NOT spliced from the operand.
    [InlineData("COPY bk1 REPLACING ==AA-X== BY ==COPY bk2.==.")]
    [InlineData("COPY bk1 COPY bk2.")]
    public void CopyWithinCopy_DrawsSr1(string statement)
    {
        Copybook("bk1.cpy", "01 AA-X PIC X(5) VALUE \"HELLO\".\n");
        Copybook("bk2.cpy", "01 ZZ-Z PIC X(3) VALUE \"ZZZ\".\n");
        var (_, bag) = Run(statement + "\n");
        Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET2451" && d.IsError);
    }

    [Theory] // SR2: "A COPY statement shall be preceded by a space" — after a parenthesis, or glued behind a period.
    [InlineData("01 Q PIC X.COPY bk1.")]
    [InlineData("    MOVE A(COPY bk1.")]
    public void CopyNotPrecededByASpace_DrawsSr2(string line)
    {
        Copybook("bk1.cpy", "01 AA-X PIC X(5) VALUE \"HELLO\".\n");
        var (_, bag) = Run(line + "\n");
        Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET2451" && d.IsError);
    }

    [Fact] // The control for SR1/SR2: COPY after a level number or a data-name, and a COPY in a literal or comment.
    public void LegalCopyPlacements_StayClean()
    {
        Copybook("bk1.cpy", "01 AA-X PIC X(5) VALUE \"HELLO\".\n");
        var (text, bag) = Run("77 COPY bk1.\n01 LIT PIC X(9) VALUE \"COPY bk2.\". *> COPY bk2.\n");
        Clean(bag);
        Assert.Contains("AA-X", text);
        Assert.Contains("VALUE \"COPY bk2.\"", text);
    }

    [Fact] // GR10 on the SUPPRESS path: the REPLACING phrase is specified, so a nested COPY in the library is an error.
    public void SuppressReplacingOverNestedCopy_DrawsGr10()
    {
        Copybook("inner.cpy", "01 N1 PIC X VALUE \"N\".\n");
        Copybook("outer.cpy", "COPY inner.\n01 N2 PIC X VALUE \"M\".\n");
        var (_, bag) = Run("COPY outer SUPPRESS REPLACING ==N2== BY ==N3==.\n");
        Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET1640" && d.IsError);
    }

    [Fact] // The REPLACE arm of the same statement parse: a period inside pseudo-text is an operand text-word.
    public void ReplaceStatement_PeriodInsidePseudoText()
    {
        var (text, bag) = Run("REPLACE ==STOP RUN.== BY ==GOBACK.==.\n    STOP RUN.\n");
        Clean(bag);
        Assert.Contains("GOBACK.", text);
        Assert.DoesNotContain("STOP RUN", text);
    }

    [Fact] // REPLACE missing its BY is COBOLNET2449 (a non-pseudo-text operand stays COBOLNET1641, one report).
    public void ReplaceStatement_MissingBy_DrawsSyntaxError()
    {
        var (_, bag) = Run("REPLACE ==A== ==B==.\n    DISPLAY A.\n");
        Assert.Equal("COBOLNET2449", Codes(bag));
        var (_, bag2) = Run("REPLACE \"A\" BY \"B\".\n    DISPLAY A.\n");
        Assert.Equal("COBOLNET1641", Codes(bag2));
    }
}
