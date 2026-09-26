// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1393 — a literal's OWN syntax rules are asked wherever the literal is written. The length rule
/// (ISO §8.3.3.2.3 SR1 alphanumeric, §8.3.3.4.3 SR1 boolean, §8.3.3.5.3 SR1 national: at most 8,191 character
/// positions of the class → COBOLNET0814) and the hexadecimal grouping rule (§8.3.3.2.3 SR6 / §8.3.3.5.3 SR5 →
/// COBOLNET1635) used to live in ONE funnel each — the length cap only in the binder's national and boolean
/// procedure-operand arms, the grouping rule only where the token was a direct child of <c>nonNumericLiteral</c> or of
/// an ALL figurative — so an over-long alphanumeric literal anywhere, any over-long VALUE / level-88 / CONSTANT / ALL
/// literal, a malformed <c>X"4"</c> concatenation operand and a malformed keyword-omitted intrinsic argument all
/// compiled clean. Each case below is one POSITION a literal can be written in, and each is paired with its 8,191-
/// position twin so the boundary is pinned from both sides (the rule says "less than or equal to 8,191").
/// Sources are built programmatically: an 8,192-character literal never belongs in a checked-in file.
/// </summary>
public sealed class LiteralScreenTests
{
    private static string Program(string id, string data, string proc, string special = "") =>
        ">>SOURCE FORMAT FREE\nIDENTIFICATION DIVISION.\nPROGRAM-ID. " + id + ".\n"
        + (special.Length > 0 ? "ENVIRONMENT DIVISION.\nCONFIGURATION SECTION.\n" + special + "\n" : "")
        + "DATA DIVISION.\nWORKING-STORAGE SECTION.\n" + data + "\nPROCEDURE DIVISION.\nMAIN.\n" + proc
        + "\n    STOP RUN.\n";

    /// <summary>Each position × class the length rule reaches. <c>{0}</c> is the literal's BODY length in
    /// positions: the case is compiled at 8,191 (legal) and 8,192 (COBOLNET0814). The BX case writes a quarter of
    /// the positions as hexadecimal digits — §8.3.3.4.4 GR5 maps each digit to four boolean characters, so 2,048
    /// digits ARE 8,192 boolean positions.</summary>
    public static TheoryData<string, string, string> LengthCases() => new()
    {
        // position                              data                                              procedure
        { "MOVE alphanumeric (procedure)",       "01 W PIC X(9000).",                             "    MOVE \"{A}\" TO W." },
        { "VALUE alphanumeric",                  "01 W PIC X(9000) VALUE \"{A}\".",               "    DISPLAY \"X\"." },
        { "VALUE hexadecimal-alphanumeric",      "01 W PIC X(9000) VALUE X\"{H}\".",              "    DISPLAY \"X\"." },
        { "VALUE ALL alphanumeric",              "01 W PIC X(9000) VALUE ALL \"{A}\".",           "    DISPLAY \"X\"." },
        { "CONSTANT alphanumeric",               "01 K CONSTANT AS \"{A}\".",                     "    DISPLAY \"X\"." },
        { "VALUE boolean",                       "01 B PIC 1(9000) VALUE B\"{Z}\".",              "    DISPLAY \"X\"." },
        { "VALUE hexadecimal-boolean",           "01 B PIC 1(9000) VALUE BX\"{Q}\".",             "    DISPLAY \"X\"." },
        { "level-88 VALUE boolean",              "01 B PIC 1(9000).\n   88 C VALUE B\"{Z}\".",    "    DISPLAY \"X\"." },
        { "MOVE ALL boolean (procedure)",        "01 B PIC 1(9000).",                             "    MOVE ALL B\"{Z}\" TO B." },
        { "VALUE national",                      "01 N PIC N(9000) VALUE N\"{A}\".",              "    DISPLAY \"X\"." },
        { "DISPLAY national (procedure)",        "01 N PIC N(1).",                                "    DISPLAY N\"{A}\"." },
    };

    [Theory]
    [MemberData(nameof(LengthCases))]
    public void LiteralLength_IsScreenedAtEveryPosition(string position, string data, string proc)
    {
        foreach (int positions in new[] { 8191, 8192 })
        {
            string Fill(string s) => s.Replace("{A}", new string('A', positions))
                .Replace("{H}", string.Concat(Enumerable.Repeat("41", positions)))
                .Replace("{Z}", new string('0', positions))
                .Replace("{Q}", new string('0', positions / 4));
            if (data.Contains("{Q}") && positions % 4 != 0) continue;   // BX counts in fours — 8,192 only (8,188 is below)
            var (ok, errors, _) = EditionHarness.CompileFull(Program("LS" + positions, Fill(data), Fill(proc)), 2023);
            if (positions <= 8191)
                Assert.True(ok, $"[{position}] a {positions}-position literal is legal (§8.3.3 SR1 '≤ 8,191'):\n"
                    + string.Join("\n", errors));
            else
            {
                Assert.False(ok, $"[{position}] an 8,192-position literal shall be rejected (§8.3.3 SR1)");
                Assert.Single(errors, e => e.Contains("COBOLNET0814"));   // once — one screen, no second funnel
            }
        }
    }

    /// <summary>The hexadecimal grouping rule at the two positions its former checks could not see: a
    /// concatenation operand (a <c>concatOperand</c>, never a <c>nonNumericLiteral</c>) and the keyword-omitted
    /// intrinsic argument (captured in the SUBSCRIPT lexer mode and re-parsed from text the version pass never
    /// walked) — for the alphanumeric <c>X"…"</c> form (pairs, §8.3.3.2.3 SR6) and the national <c>NX"…"</c> form
    /// (groups of four, §8.3.3.5.3 SR5). Each malformed literal used to decode to the empty string in silence.</summary>
    [Theory]
    [InlineData("concatenation operand, X", "01 W PIC X(4).", "    MOVE \"Z\" & X\"4\" TO W.", "")]
    [InlineData("concatenation operand, NX", "01 W PIC N(4).", "    MOVE N\"Z\" & NX\"041\" TO W.", "")]
    [InlineData("ALL literal-1 concatenation operand", "01 W PIC X(4).", "    MOVE ALL \"A\" & X\"4\" TO W.", "")]
    [InlineData("keyword-omitted intrinsic argument, X", "01 L PIC 9(4).", "    MOVE LENGTH(X\"414\") TO L.",
        "REPOSITORY.\n    FUNCTION ALL INTRINSIC.")]
    [InlineData("keyword-omitted intrinsic argument, NX", "01 L PIC 9(4).", "    MOVE LENGTH(NX\"041\") TO L.",
        "REPOSITORY.\n    FUNCTION ALL INTRINSIC.")]
    public void HexGrouping_IsScreenedAtEveryPosition(string position, string data, string proc, string special)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("LSHEX", data, proc, special), 2023);
        Assert.False(ok, $"[{position}] a malformed hexadecimal literal shall be rejected (§8.3.3.2.3 SR6 / §8.3.3.5.3 SR5)");
        Assert.Single(errors, e => e.Contains("COBOLNET1635"));
    }

    /// <summary>The well-formed twins still compile and yield the §8.3.3.2.4 / §8.3.3.5.4 values — the SUBSCRIPT-mode
    /// <c>SUB_HEXLIT</c> token the screen needed must not change what a keyword-omitted argument binds to:
    /// <c>X"4142"</c> is two alphanumeric characters and <c>NX"00410042"</c> two national ones, so LENGTH is 2 for
    /// both, and <c>"Z" &amp; X"41"</c> is <c>ZA</c> (§8.8.3.3 GR2).</summary>
    [Fact]
    public void WellFormedHexLiterals_AtTheNewlyScreenedPositions_KeepTheirValues()
    {
        string src = Program("LSHEXOK", "01 L PIC 9(4).\n01 W PIC X(4).",
            "    MOVE LENGTH(X\"4142\") TO L.\n    DISPLAY \"X=\" L.\n"
            + "    MOVE LENGTH(NX\"00410042\") TO L.\n    DISPLAY \"NX=\" L.\n"
            + "    MOVE \"Z\" & X\"41\" TO W.\n    DISPLAY \"C=[\" W \"]\".",
            "REPOSITORY.\n    FUNCTION ALL INTRINSIC.");
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 2023);
        Assert.True(ok, detail);
        Assert.Equal("X=0002\nNX=0002\nC=[ZA  ]", stdout.Replace("\r\n", "\n").TrimEnd());
    }

    /// <summary>§8.8.3.2 SR2–SR4 reach the concatenated ALL literal-1 (§8.3.3.6.3 SR2: literal-1 "may be a
    /// concatenation expression"), whose fold is diagnostic-free: 4,096 + 4,095 positions is legal, 4,096 + 4,096
    /// is COBOLNET1545 — for every class.</summary>
    [Theory]
    [InlineData("\"", "A", "01 W PIC X(9000).")]
    [InlineData("B\"", "1", "01 W PIC 1(9000).")]
    [InlineData("N\"", "A", "01 W PIC N(9000).")]
    public void ConcatenatedAllLiteral_ResultLength_IsScreened(string open, string ch, string data)
    {
        string Lit(int n) => open + string.Concat(Enumerable.Repeat(ch, n)) + "\"";
        var (okAt8191, errors8191, _) = EditionHarness.CompileFull(
            Program("LSALL1", data, $"    MOVE ALL {Lit(4096)} & {Lit(4095)} TO W."), 2023);
        Assert.True(okAt8191, string.Join("\n", errors8191));
        var (okAt8192, errors8192, _) = EditionHarness.CompileFull(
            Program("LSALL2", data, $"    MOVE ALL {Lit(4096)} & {Lit(4096)} TO W."), 2023);
        Assert.False(okAt8192, "an 8,192-position concatenated ALL literal-1 shall be rejected (§8.8.3.2 SR2–SR4)");
        Assert.Single(errors8192, e => e.Contains("COBOLNET1545"));
    }
}
