// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE STRING / UNSTRING OPERAND SCREENS, ONE ROW PER FORM AND PER EDITION (kb/Work PB1181, PB1182, PB1527). The
/// rules are edition-invariant (the verbs' syntax rules are the COBOL-85 ones), so a form is refused at every edition
/// that can WRITE it; <c>fromEdition</c> is the first edition that can (a CONSTANT entry and a bit group are 2002
/// constructs, a boolean literal likewise).
/// <para>They are xUnit rows rather than corpus entries because the corpus's <c>.err</c> is ONE substring per
/// program: a program holding five offending statements is green while four of them compile clean. One compile per
/// (statement, edition) states the pairing the finding made — the form, the rule that refuses it, and the diagnostic
/// that names that rule — and fails on the form that regresses. The legal neighbours of every row are the positive
/// controls <c>85/pb1181_string_unstring_screen_controls</c> and <c>2002/pb1182_constant_name_literal_positions</c>.</para>
/// </summary>
public sealed class StringUnstringOperandScreenTests
{
    private static string Prog(string pid, string extraData, string statement) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 S PIC X(12) VALUE "AB,CD5EF".
        01 A PIC X(4).
        01 B PIC X(4).
        01 C PIC X(4).
        01 OUT1 PIC X(10) VALUE SPACES.
        01 N1 PIC 9V9 VALUE 1.2.
        01 N2 PIC 99P VALUE 10.
        01 NQ PIC PP99 VALUE 7.
        01 M PIC 9(4).
        {extraData}
        PROCEDURE DIVISION.
            {statement}
            STOP RUN.
        """;

    private const string Constants = """
        01 K CONSTANT AS "XY,ZW".
        01 KN CONSTANT AS 5.
        """;

    private const string BitGroup = """
        01 BG GROUP-USAGE BIT.
           05 BG1 PIC 1(2).
        """;

    private const string StrongGroup = """
        01 TG TYPEDEF STRONG.
           05 SA PIC X(3).
           05 SB PIC X(3).
        01 SG TYPE TG.
        """;

    private static void Refuses(string pid, string extraData, string statement, string code, string rule, int fromEdition)
    {
        foreach (int edition in EditionHarness.Editions.Where(e => e >= fromEdition))
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid + "E" + edition, extraData, statement), edition);
            Assert.False(ok, $"[{pid}] `{statement}` must be REJECTED at --std {edition}");
            EditionHarness.AssertHasDiagnostic(errors, code);
            EditionHarness.AssertHasDiagnostic(errors, rule);
        }
    }

    /// <summary>§14.9.43.3 SR3 — "Literal-2 shall not be a zero-length literal." The run-time kernel read a zero-length
    /// delimiter as DELIMITED BY SIZE (GR3 b)'s rule for a zero-length IDENTIFIER-2), so the program ran and printed
    /// its whole sender.</summary>
    [Theory]
    [InlineData("S3A", "STRING \"AB,CD\" DELIMITED BY \"\" INTO OUT1.")]
    [InlineData("S3B", "STRING \"AB,CD\" DELIMITED BY X\"\" INTO OUT1.")]
    public void String_ZeroLengthLiteral2_IsSR3(string pid, string statement) =>
        Refuses("PB1181" + pid, "", statement, "COBOLNET1651", "ISO §14.9.43.3 SR3", fromEdition: 85);

    /// <summary>§14.9.43.3 SR8 — "Where identifier-1 or identifier-2 is an elementary numeric data item, it shall be
    /// described as an integer without the symbol 'P' in its picture character-string." Only SR7's identifier-4 had
    /// ever been asked: `STRING N1 …` over <c>PIC 9V9</c> sent "12". A V alone is a non-integer, P either side is the
    /// symbol the rule bars; identifier-1 and identifier-2 are separate positions.</summary>
    [Theory]
    [InlineData("S8A", "STRING N1 DELIMITED BY SIZE INTO OUT1.", "STRING sending operand 'N1'")]
    [InlineData("S8B", "STRING N2 DELIMITED BY SIZE INTO OUT1.", "symbol P")]
    [InlineData("S8C", "STRING NQ DELIMITED BY SIZE INTO OUT1.", "symbol P")]
    [InlineData("S8D", "STRING \"AB05CD\" DELIMITED BY N1 INTO OUT1.", "STRING DELIMITED BY 'N1'")]
    [InlineData("S8E", "STRING \"AB05CD\" DELIMITED BY N2 INTO OUT1.", "symbol P")]
    public void String_NonIntegerOrPScaledNumericIdentifier_IsSR8(string pid, string statement, string wording)
    {
        Refuses("PB1182" + pid, "", statement, "COBOLNET1651", "ISO §14.9.43.3 SR8", fromEdition: 85);
        Refuses("PB1182" + pid + "W", "", statement, "COBOLNET1651", wording, fromEdition: 85);
    }

    /// <summary>§14.9.43.3 SR2 — "Literal-1 or literal-2 shall not be a figurative constant that begins with the word
    /// ALL." `ALL SPACES` / `ALL ZEROS` bound to a plain figurative that had lost the word, so only `ALL "lit"` was
    /// ever refused.</summary>
    [Theory]
    [InlineData("S2A", "STRING ALL SPACES DELIMITED BY SIZE INTO OUT1.")]
    [InlineData("S2B", "STRING ALL ZEROS DELIMITED BY SIZE INTO OUT1.")]
    [InlineData("S2C", "STRING \"A B\" DELIMITED BY ALL SPACES INTO OUT1.")]
    public void String_FigurativeBeginningWithAll_IsSR2(string pid, string statement) =>
        Refuses("PB1181" + pid, "", statement, "COBOLNET1757", "ISO §14.9.43.3 SR2", fromEdition: 85);

    /// <summary>§14.9.48.3 SR1 — "Literal-1 and literal-2 shall be literals of the category alphanumeric or national
    /// and shall be neither a figurative constant that begins with the word ALL nor a zero-length literal." Its
    /// per-delimiter check read FIELD and intrinsic operands only, so a numeric literal split the sender on the digit,
    /// a zero-length one left every receiver untouched and `ALL ALL ","` was folded into the ALL phrase.</summary>
    [Theory]
    [InlineData("U1A", "UNSTRING S DELIMITED BY 5 INTO A B C.", "is a numeric literal")]
    [InlineData("U1B", "UNSTRING S DELIMITED BY \"\" INTO A B C.", "zero-length literal")]
    [InlineData("U1C", "UNSTRING S DELIMITED BY ALL \"\" INTO A B C.", "zero-length literal")]
    [InlineData("U1D", "UNSTRING S DELIMITED BY ALL ALL \",\" INTO A B C.", "begins with the word ALL")]
    [InlineData("U1E", "UNSTRING S DELIMITED BY \",\" OR ALL ALL \",\" INTO A B C.", "begins with the word ALL")]
    [InlineData("U1F", "UNSTRING S DELIMITED BY ALL ALL SPACES INTO A B C.", "begins with the word ALL")]
    [InlineData("U1G", "UNSTRING S DELIMITED BY \",\" OR X\"\" INTO A B C.", "zero-length literal")]
    public void Unstring_DelimiterLiteral_IsSR1(string pid, string statement, string wording)
    {
        Refuses("PB1181" + pid, "", statement, "COBOLNET1651", "ISO §14.9.48.3 SR1", fromEdition: 85);
        Refuses("PB1181" + pid + "W", "", statement, "COBOLNET1651", wording, fromEdition: 85);
    }

    /// <summary>§14.9.48.3 SR1 on the BOOLEAN literal (a 2002 literal) and on a CONSTANT-NAME standing for a numeric
    /// literal — the substituted literal is a literal like any other (§13.10.3 SR2).</summary>
    [Theory]
    [InlineData("U1H", "", "UNSTRING S DELIMITED BY B\"1\" INTO A B C.", "is a boolean literal")]
    [InlineData("U1I", Constants, "UNSTRING S DELIMITED BY KN INTO A B C.", "is a numeric literal")]
    public void Unstring_BooleanOrNumericConstantDelimiter_IsSR1(string pid, string extraData, string statement, string wording)
    {
        Refuses("PB1181" + pid, extraData, statement, "COBOLNET1651", "ISO §14.9.48.3 SR1", fromEdition: 2002);
        Refuses("PB1181" + pid + "W", extraData, statement, "COBOLNET1651", wording, fromEdition: 2002);
    }

    /// <summary>§14.9.48.3 SR4 — "Numeric items shall not be specified with the symbol 'P' in their picture
    /// character-string." The receiver arm tested category and usage only (the legal <c>PIC 9V9</c> receiver is in the
    /// positive golden).</summary>
    [Theory]
    [InlineData("U4A", "UNSTRING S DELIMITED BY \",\" INTO N2 M.")]
    [InlineData("U4B", "UNSTRING S DELIMITED BY \",\" INTO NQ M.")]
    public void Unstring_PScaledNumericReceiver_IsSR4(string pid, string statement) =>
        Refuses("PB1182" + pid, "", statement, "COBOLNET1626", "ISO §14.9.48.3 SR4", fromEdition: 85);

    /// <summary>§14.9.48.3 SR4 on the GROUP receivers the screen used to exempt wholesale: a BIT group is class and
    /// category boolean (§13.18.29.4 GR1 a)) and a strongly-typed group's category is its type-name (§8.5.2.1) —
    /// neither is "usage display and category alphabetic, alphanumeric, or numeric".</summary>
    [Theory]
    [InlineData("U4C", BitGroup, "UNSTRING S DELIMITED BY \",\" INTO BG M.")]
    [InlineData("U4D", StrongGroup, "UNSTRING S DELIMITED BY \",\" INTO SG M.")]
    public void Unstring_BitOrStronglyTypedGroupReceiver_IsSR4(string pid, string extraData, string statement) =>
        Refuses("PB1182" + pid, extraData, statement, "COBOLNET1626", "ISO §14.9.48.3 SR4", fromEdition: 2002);

    /// <summary>§14.9.48.3 SR8 with §13.10.3 SR2 — identifier-1 is "the data item", and a constant-name stands only
    /// where a format specifies a LITERAL. The receiving side refused it already (COBOLNET1548); the sending side ran
    /// the constant's text through the splitter.</summary>
    [Fact]
    public void Unstring_ConstantNameSender_IsSR8() =>
        Refuses("PB1182U8", Constants, "UNSTRING K DELIMITED BY \",\" INTO A B.", "COBOLNET1651", "ISO §14.9.48.3 SR8",
            fromEdition: 2002);
}
