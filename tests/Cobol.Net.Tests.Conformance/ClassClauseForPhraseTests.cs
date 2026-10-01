// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB976 — THE CLASS CLAUSE'S FOR PHRASE IS READ, and every class-dependent sub-rule of ISO §12.3.7.3 SR17
/// ("When the CLASS clause is specified:") keys on that one reading. The phrase was parsed and IGNORED, so the b-
/// and c-series went unenforced and SR-12.3.7.3-17 was recorded CONFORMS on evidence that never exercised it.
/// <para>One fact per sub-rule and class (<c>feedback_two_arm_dispatch</c>: the b arm and the c arm are pinned
/// separately), the SR11 zero-length rule both clauses share, and kb/Work PB977's refusal of a FOR phrase written
/// AFTER the definition — one diagnostic for the three FOR-bearing clauses. Every negative has a positive control
/// below it, so a regression that rejected every CLASS clause cannot read as green.</para>
/// </summary>
public sealed class ClassClauseForPhraseTests
{
    private static string Prog(string pid, string specialNames) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            ALPHABET AA IS NATIVE
            ALPHABET NA FOR NATIONAL IS NATIVE
            ALPHABET S1 IS STANDARD-1
            {specialNames}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 FILLER PIC X.
        PROCEDURE DIVISION.
            STOP RUN.
        """;

    private static void Rejects(string pid, string specialNames, string expected, int edition = 2023)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, specialNames), edition);
        Assert.False(ok, $"[{pid}] must be REJECTED at --std {edition}");
        EditionHarness.AssertHasDiagnostic(errors, expected);
    }

    /// <summary>§12.3.7.3 SR17 d) — "<i>Alphabet-name-4 shall not reference an alphabet specified with the LOCALE
    /// phrase.</i>" Both classes (the national arm is the one row SR-12.3.7.3-L7.5 lumps with c5): a LOCALE alphabet
    /// defines a collating sequence and no coded character set (§12.3.7.4 GR7 Table 6), so the ONE coded-set resolver
    /// refuses it by SR17 d's name.</summary>
    [Theory]
    [InlineData("PB1557D", "ALPHABET LA IS LOCALE\n    CLASS CL IS 1 THRU 3 IN LA.", "COBOLNET1669: CLASS CL … IN LA: the alphabet 'LA' is associated with a locale")]
    [InlineData("PB1557DN", "ALPHABET LN FOR NATIONAL IS LOCALE\n    CLASS CN FOR NATIONAL IS 1 THRU 3 IN LN.", "COBOLNET1669: CLASS CN … IN LN: the alphabet 'LN' is associated with a locale")]
    public void InLocaleAlphabet_IsSR17d(string pid, string clauses, string expected)
    {
        Rejects(pid, clauses, expected);
        Rejects(pid + "R", clauses, "ISO §12.3.7.3 SR17 d");
    }

    /// <summary>§12.3.7.3 SR17 b) 1. / c) 1. — "When the IN phrase is specified, alphabet-name-4 shall reference an
    /// alphabet that defines an alphanumeric / a national character set". SR17 a) implies ALPHANUMERIC when no FOR
    /// phrase is written, so the unmarked class takes the b arm.</summary>
    [Theory]
    [InlineData("PB976B1", "CLASS C3 IS 1 THRU 5 IN NA.", "COBOLNET1671: CLASS C3 IN NA: alphabet-name-4 shall reference an alphabet that defines an ALPHANUMERIC character set — this alphabet is FOR NATIONAL (ISO §12.3.7.3 SR17 b1)")]
    [InlineData("PB976B1F", "CLASS C3 FOR ALPHANUMERIC IS 1 THRU 5 IN NA.", "(ISO §12.3.7.3 SR17 b1)")]
    [InlineData("PB976C1", "CLASS C2 FOR NATIONAL IS 1 THRU 5 IN AA.", "COBOLNET1671: CLASS C2 FOR NATIONAL IN AA: alphabet-name-4 shall reference an alphabet that defines a NATIONAL character set — this alphabet is alphanumeric (ISO §12.3.7.3 SR17 c1)")]
    public void InAlphabetOfTheOtherClass_IsSR17_1(string pid, string clause, string expected) => Rejects(pid, clause, expected);

    /// <summary>§12.3.7.3 SR17 b) 2. / c) 2. — "Literal-5, if numeric, shall be an unsigned integer and shall have
    /// a value within the range of one through …" the native set or, under IN, alphabet-name-4's set. Both halves
    /// of the sentence, on both arms.</summary>
    [Theory]
    [InlineData("PB976B2S", "CLASS C9 IS +65 THRU 66.", "COBOLNET1671: CLASS C9: +65 — each numeric literal shall be an UNSIGNED integer (ISO §12.3.7.3 SR17 b2)")]
    [InlineData("PB976C2S", "CLASS C9 FOR NATIONAL IS +65.", "(ISO §12.3.7.3 SR17 c2)")]
    [InlineData("PB976B2R", "CLASS C9 IS 1 THRU 129 IN S1.", "the ordinal 129 does not exist in the character set referenced by the IN alphabet (STANDARD-1, 128 characters) — ISO §12.3.7.3 SR17 b2")]
    [InlineData("PB976C2R", "CLASS C9 FOR NATIONAL IS 65537.", "(ISO §12.3.7.3 SR17 c2)")]
    // kb/Work PB1557's sibling: an integer too long for an `int` is still an INTEGER, held to the ordinal rule — it
    // used to fail int.TryParse and be reported under SR17 b3, the NONINTEGER literal's class rule.
    [InlineData("PB1557B2L", "CLASS C9 IS 12345678901.", "COBOLNET1671: CLASS C9: the ordinal 12345678901 does not exist in the native alphanumeric character set")]
    [InlineData("PB1557B2I", "CLASS C9 IS 1 THRU 99999999999999999999 IN S1.", "the ordinal 99999999999999999999 does not exist in the character set referenced by the IN alphabet (STANDARD-1, 128 characters) — ISO §12.3.7.3 SR17 b2")]
    public void Ordinal_IsSR17_2(string pid, string clause, string expected) => Rejects(pid, clause, expected);

    /// <summary>§12.3.7.3 SR17 b) 3. / c) 3. — "Each noninteger literal shall be an alphanumeric / a national
    /// literal." ⛔ The b arm also pins the classifier: a N"…" literal used to pass the ALPHANUMERIC test (it asked
    /// "is this a quoted literal?", true for every prefix), in the ALPHABET clause too — the sweep's second fact.</summary>
    [Theory]
    [InlineData("PB976B3", "CLASS C1 IS N\"0\" THRU N\"9\".", "COBOLNET1671: CLASS C1: N\"0\" — each noninteger literal shall be an alphanumeric literal (ISO §12.3.7.3 SR17 b3)")]
    [InlineData("PB976C3", "CLASS HN FOR NATIONAL IS \"0\" THRU \"9\".", "COBOLNET1671: CLASS HN FOR NATIONAL: \"0\" — each noninteger literal shall be a NATIONAL literal")]
    [InlineData("PB976C3A", "CLASS HN FOR NATIONAL IS ALL \"AB\".", "COBOLNET1671: CLASS HN FOR NATIONAL: ALL \"AB\" — each noninteger literal shall be a NATIONAL literal")]
    [InlineData("PB976B3AL", "CLASS C9 IS \"A\".\n    ALPHABET AX IS N\"A\" \"B\".", "COBOLNET1906: ALPHABET AX: N\"A\" — each noninteger literal shall be an alphanumeric literal (ISO §12.3.7.3 SR14 b2)")]
    public void LiteralOfTheOtherClass_IsSR17_3(string pid, string clause, string expected) => Rejects(pid, clause, expected);

    /// <summary>§12.3.7.3 SR17 b) 4. / c) 4. — "Each alphanumeric / national literal, when a THROUGH phrase is
    /// specified, shall be one character in length." The multi-character operand used to be taken as a plain
    /// literal-5 with literal-6 silently dropped.</summary>
    [Theory]
    [InlineData("PB976B4", "CLASS C9 IS \"AB\" THRU \"C\".", "COBOLNET1671: CLASS C9: the operand 'AB' is 2 characters — each alphanumeric literal, when a THROUGH phrase is specified, shall be one character in length (ISO §12.3.7.3 SR17 b4)")]
    [InlineData("PB976C4", "CLASS C9 FOR NATIONAL IS N\"A\" THRU N\"YZ\".", "(ISO §12.3.7.3 SR17 c4)")]
    public void MultiCharacterThroughOperand_IsSR17_4(string pid, string clause, string expected) => Rejects(pid, clause, expected);

    /// <summary>kb/Work PB1091 (row SR-12.3.7.3-L7.4) — SR17 c4 is a rule about NATIONAL LITERALS ("Each national literal,
    /// when a THROUGH phrase is specified, shall be one character in length"); a NUMERIC literal is an ordinal, names one
    /// character of its set by construction, and is held to c2's range rule. The one-character test is asked of the operand
    /// AS WRITTEN, so an ordinal under <c>IN</c> UCS-4 that names a SUPPLEMENTARY character (two UTF-16 code units) is no
    /// longer refused under c4. What a THROUGH phrase needs of it is a position in the NATIVE set (§12.3.7.4 GR12:
    /// "the contiguous characters in the native character set beginning with … literal-5 … ending with … literal-6"),
    /// which a character of two code units has none in — refused under that rule's own message (implementor-defined,
    /// DOC-A.1-188). The BMP ordinals of the same IN alphabet, and a supplementary ordinal OUTSIDE a THROUGH phrase, are
    /// legal.</summary>
    [Theory]
    [InlineData("PB1091C7", "ALPHABET U4 FOR NATIONAL IS UCS-4\n    CLASS C9 FOR NATIONAL IS 65536 THRU 65537 IN U4.")]
    [InlineData("PB1091C7L", "ALPHABET U4 FOR NATIONAL IS UCS-4\n    CLASS C9 FOR NATIONAL IS 66 THRU 65537 IN U4.")]
    public void SupplementaryOrdinalUnderThrough_IsRefusedByGR12_NotC4(string pid, string clauses)
    {
        Rejects(pid, clauses, "(it needs 2 of its 16-bit code units)");
        Rejects(pid + "G", clauses, "ISO §12.3.7.4 GR12");
        var (_, errors, _) = EditionHarness.CompileFull(Prog(pid + "N", clauses), 2023);
        Assert.DoesNotContain(errors, e => e.Contains("SR17 c4"));
    }

    /// <summary>The legal neighbours of the refusal above, each a positive control: a BMP ordinal range under IN UCS-4, a
    /// supplementary ordinal that is NOT a THROUGH bound (c2's range is its only rule), and an ordinal THROUGH the native set.</summary>
    [Theory]
    [InlineData("PB1091OK1", "ALPHABET U4 FOR NATIONAL IS UCS-4\n    CLASS C9 FOR NATIONAL IS 66 THRU 91 IN U4.")]
    [InlineData("PB1091OK2", "ALPHABET U4 FOR NATIONAL IS UCS-4\n    CLASS C9 FOR NATIONAL IS 65537 IN U4.")]
    [InlineData("PB1091OK3", "CLASS C9 FOR NATIONAL IS 1 THRU 91.")]
    public void NumericOrdinals_AreNotNationalLiterals_SoC4DoesNotApply(string pid, string clauses)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, clauses), 2023);
        Assert.True(ok, $"[{pid}] must COMPILE: {string.Join("\n", errors)}");
    }

    /// <summary>kb/Work PB1091 — the national arm of the too-long ordinal (SR17 c2): an integer too long for an
    /// <c>int</c> is an INTEGER, held to the ordinal range rule by THE one ordinal reader, never reported as a
    /// noninteger literal (c3). The b arm is <see cref="Ordinal_IsSR17_2"/>'s PB1557B2L/I.</summary>
    [Theory]
    [InlineData("PB1091C2L", "CLASS C9 FOR NATIONAL IS 12345678901.", "COBOLNET1671: CLASS C9 FOR NATIONAL: the ordinal 12345678901 does not exist in the native national character set")]
    [InlineData("PB1091C2I", "ALPHABET U4 FOR NATIONAL IS UCS-4\n    CLASS C9 FOR NATIONAL IS 99999999999 IN U4.", "the ordinal 99999999999 does not exist in the character set referenced by the IN alphabet (UCS-4, 1112064 characters) — ISO §12.3.7.3 SR17 c2")]
    public void TooLongNationalOrdinal_IsSR17_c2(string pid, string clause, string expected) => Rejects(pid, clause, expected);

    /// <summary>§12.3.7.3 SR17 b) 5. — "The number of characters specified shall not exceed … the number of
    /// characters in the character set referenced by alphabet-name-4": the 256-character Latin-1 block, named
    /// THROUGH the native set, outnumbers STANDARD-1's 128 (the positive control's 26 do not).</summary>
    [Fact]
    public void MoreCharactersThanTheInSet_IsSR17_5() =>
        Rejects("PB976B5", "CLASS C9 IS 1 THRU 2 X\"00\" THRU X\"FF\" IN S1.",
            "256 characters are specified — more than the 128 characters of the character set referenced by alphabet-name-4 (STANDARD-1) (ISO §12.3.7.3 SR17 b5)");

    /// <summary>§12.3.7.3 SR11 — "Literal-1, literal-2, literal-3, literal-4, literal-5, literal-6, and literal-9 shall
    /// specify neither a symbolic-character figurative constant nor a zero-length literal", for the CLASS clause and
    /// the ALPHABET clause through the one decoder.</summary>
    [Theory]
    [InlineData("PB976Z1", "CLASS C9 IS \"\" \"A\".", "COBOLNET1671: CLASS C9: \"\" — an operand shall not be a zero-length literal (ISO §12.3.7.3 SR11)")]
    [InlineData("PB976Z2", "CLASS C9 IS \"A\".\n    ALPHABET AX IS \"\", \"A\".", "COBOLNET1906: ALPHABET AX: \"\" — an operand shall not be a zero-length literal (ISO §12.3.7.3 SR11)")]
    // kb/Work PB226 — SR11's OTHER half in the ALPHABET literal phrase: a symbolic-character name is refused under
    // SR11 itself (it used to draw the class rule, SR14 b2), whichever order the two clauses are written in.
    [InlineData("PB226S1", "ALPHABET AX IS \"A\" ALSO BEL\n    SYMBOLIC CHARACTERS BEL IS 8.", "COBOLNET1906: ALPHABET AX: BEL — an operand shall not be a symbolic-character figurative constant (ISO §12.3.7.3 SR11)")]
    [InlineData("PB226S2", "SYMBOLIC CHARACTERS BEL IS 8\n    ALPHABET AX IS \"A\" THRU BEL.", "COBOLNET1906: ALPHABET AX: BEL — an operand shall not be a symbolic-character figurative constant (ISO §12.3.7.3 SR11)")]
    public void ZeroLengthLiteral_IsSR11(string pid, string clause, string expected) => Rejects(pid, clause, expected);

    /// <summary>kb/Work PB977 — a FOR phrase written AFTER the clause's definition is refused BY NAME, the same code
    /// for all three clauses that print the phrase (ISO §12.3.7.2), at every edition. The ALPHABET spelling used to
    /// compile clean; the CLASS and SYMBOLIC CHARACTERS spellings drew a bare <c>COBOL0001: unexpected 'FOR'</c>.</summary>
    [Theory]
    [InlineData("PB977A", "ALPHABET A2 IS NATIVE FOR NATIONAL.", "COBOLNET2315: ALPHABET A2: the phrase 'FOR NATIONAL' follows the clause's definition — the FOR phrase belongs between the name and IS", 2023)]
    [InlineData("PB977A85", "ALPHABET A2 IS NATIVE FOR ALPHANUMERIC.", "COBOLNET2315: ALPHABET A2: the phrase 'FOR ALPHANUMERIC'", 85)]
    [InlineData("PB977C", "CLASS DG IS \"0\" THRU \"9\" FOR ALPHANUMERIC.", "COBOLNET2315: CLASS DG: the phrase 'FOR ALPHANUMERIC' follows the clause's definition", 2023)]
    [InlineData("PB977CI", "CLASS DG IS 49 THRU 58 IN S1 NATIONAL.", "COBOLNET2315: CLASS DG: the phrase 'NATIONAL'", 2014)]
    [InlineData("PB977S", "SYMBOLIC CHARACTERS SA IS 66 FOR NATIONAL.", "COBOLNET2315: SYMBOLIC CHARACTERS: the phrase 'FOR NATIONAL' follows the clause's definition — the FOR phrase belongs before the first symbolic-character-1", 2002)]
    public void TrailingForPhrase_IsRefusedByName(string pid, string clause, string expected, int edition) =>
        Rejects(pid, clause, expected, edition);

    /// <summary>THE POSITIVE CONTROL for every fact above: each class, with and without IN, with THROUGH, a
    /// figurative constant, an ALL literal and the FOR phrase at its printed position — and the ALPHABET clause's
    /// ALL literal (§8.3.3.6.4 GR3 c: "The length of the string is the length of literal-1"), which it used to
    /// refuse under SR11, a rule about something else.</summary>
    [Theory]
    [InlineData("PB976OKA", "CLASS C1 IS \"0\" THRU \"9\" SPACE ALL \"AB\" 66 THRU 68 X\"41\".")]
    [InlineData("PB976OKAF", "CLASS C1 FOR ALPHANUMERIC IS 66 THRU 91 IN S1.")]
    [InlineData("PB976OKN", "CLASS C2 FOR NATIONAL IS N\"0\" THRU N\"9\" HIGH-VALUE 946 THRU 970.")]
    [InlineData("PB976OKNI", "CLASS C2 NATIONAL IS 946 THRU 970 IN NA.")]
    [InlineData("PB976OKAL", "CLASS C1 IS \"A\".\n    ALPHABET AX IS ALL \"Z\", \"A\" THRU \"C\".")]
    [InlineData("PB976OKSY", "SYMBOLIC CHARACTERS FOR NATIONAL SN IS 946.")]
    public void LegalClauses_AreAccepted(string pid, string clause)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, clause), 2023);
        Assert.True(ok, $"[{pid}] must COMPILE: {string.Join("\n", errors)}");
    }
}
