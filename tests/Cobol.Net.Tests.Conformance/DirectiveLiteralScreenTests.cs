// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1616 — a literal in a COMPILER-DIRECTIVE operand obeys its own §8.3.3 syntax rules and the edition gates of
/// its format, exactly as the same literal written in the program text does (kb/Work PB1393, PB1545, PB895). The
/// directive operand is lexed apart from the unit, so the unit-tree screen never reached it: the evaluator asked the
/// rules itself, under COBOLNET1619, along the one path it evaluated — an omitted branch, an operand a failed
/// DEFINE left undefined and every edition gate of a literal FORMAT escaped. The conditional-compilation stage's three
/// fragment funnels now hand every parsed fragment to <c>LiteralScreen</c>, the body the unit walk uses, so the codes
/// are the unit lane's: COBOLNET0814 (length), COBOLNET1635 (grouping), COBOLNET2630 (repertoire), COBOLNET0900 (format).
/// Each case below is one DIRECTIVE POSITION a literal can be written in, paired with its legal twin so the rule is
/// pinned from both sides. Sources are built programmatically: an 8,192-position literal never belongs in a checked-in file.
/// </summary>
public sealed class DirectiveLiteralScreenTests
{
    /// <summary>The directive lines <paramref name="directives"/> in a minimal FREE-form program. A compiler directive is
    /// one line and cannot be continued, so a literal longer than a free-form line (255 positions, §6.1 3) a),
    /// COBOLNET2653) makes the line itself an error too: the length tests below read the literal's own diagnostic out of
    /// the errors and never ask whether the compile as a whole succeeded.</summary>
    private static string Program(string id, string directives) =>
        ">>SOURCE FORMAT FREE\nIDENTIFICATION DIVISION.\nPROGRAM-ID. " + id + ".\n" + directives
            + "\nPROCEDURE DIVISION.\nMAIN.\n    STOP RUN.\n";

    /// <summary>Every directive position that takes a compile-time operand: a DEFINE value, a >>IF operand, a >>EVALUATE
    /// subject, a >>WHEN object (both forms), a >>DISPLAY operand — and the same DEFINE inside an OMITTED branch, which
    /// §7.2.1 holds to the same syntax. <c>{L}</c> is the literal under test.</summary>
    public static TheoryData<string, string> Positions() => new()
    {
        { "DEFINE value",                  ">>DEFINE D AS {L}" },
        { ">>IF operand",                  ">>DEFINE V AS \"X\"\n>>IF V = {L}\n>>END-IF" },
        { ">>EVALUATE subject",            ">>EVALUATE {L}\n>>WHEN \"X\"\n>>END-EVALUATE" },
        { ">>WHEN object (Format 1)",      ">>DEFINE V AS \"X\"\n>>EVALUATE V\n>>WHEN {L}\n>>END-EVALUATE" },
        { ">>WHEN operand (EVALUATE TRUE)", ">>DEFINE V AS \"X\"\n>>EVALUATE TRUE\n>>WHEN V = {L}\n>>END-EVALUATE" },
        { ">>DISPLAY operand",             ">>DISPLAY {L}" },
        { "DEFINE in an omitted branch",   ">>IF 1 = 2\n>>DEFINE D AS {L}\n>>END-IF" },
        { ">>IF in an omitted branch",     ">>IF 1 = 2\n>>IF 3 = 4\n>>END-IF\n>>IF \"X\" = {L}\n>>END-IF\n>>END-IF" },
    };

    private static string Fill(string directives, string literal) => directives.Replace("{L}", literal);

    /// <summary>The length rule (§8.3.3.2.3 SR1 alphanumeric, §8.3.3.4.3 SR1 boolean, §8.3.3.5.3 SR1 national: at most
    /// 8,191 positions of the class) at every directive position: an 8,192-position literal is COBOLNET0814, once, and
    /// its 8,191-position twin draws no such diagnostic (the line it sits on still draws COBOLNET2653, which is the
    /// line's, not the literal's).</summary>
    [Theory]
    [MemberData(nameof(Positions))]
    public void LiteralLength_IsScreenedAtEveryDirectivePosition(string position, string directives)
    {
        foreach (int positions in new[] { 8191, 8192 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(
                Program("DLS" + positions, Fill(directives, "\"" + new string('A', positions) + "\"")), 2023);
            Assert.False(ok, $"[{position}] the {positions}-position line is over §6.1 3) a)'s 255");
            if (positions <= 8191)
                Assert.DoesNotContain(errors, e => e.Contains("COBOLNET0814"));   // §8.3.3.2.3 SR1 '≤ 8,191'
            else
            {
                Assert.Single(errors, e => e.Contains("COBOLNET0814"));   // once: one screen
                Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1619") && e.Contains("literal"));   // not the evaluator's as well
            }
        }
    }

    /// <summary>The length rule for the other two classes at the one position where their category is legal.</summary>
    [Theory]
    [InlineData("national", "N\"", "A")]
    [InlineData("boolean", "B\"", "1")]
    public void LiteralLength_IsScreenedForEveryClass(string kind, string open, string ch)
    {
        var (_, errors, _) = EditionHarness.CompileFull(
            Program("DLC" + kind, ">>DEFINE D AS " + open + new string(ch[0], 8192) + "\""), 2023);
        Assert.Single(errors, e => e.Contains("COBOLNET0814"));   // an 8,192-position literal shall be rejected
    }

    /// <summary>The hexadecimal grouping rule (§8.3.3.2.3 SR6 pairs, §8.3.3.5.3 SR5 groups of four) and the content
    /// repertoire rules (§8.3.3.2.3 SR5 hexadecimal digits, §8.3.3.4.3 SR2 '0' or '1') at every directive position.</summary>
    [Theory]
    [MemberData(nameof(Positions))]
    public void HexGrouping_IsScreenedAtEveryDirectivePosition(string position, string directives)
    {
        foreach (string literal in new[] { "X\"414\"", "NX\"041\"" })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Program("DLH", Fill(directives, literal)), 2023);
            Assert.False(ok, $"[{position}] {literal} shall be rejected (§8.3.3.2.3 SR6 / §8.3.3.5.3 SR5)");
            Assert.Single(errors, e => e.Contains("COBOLNET1635"));
        }
    }

    [Theory]
    [InlineData("X\"GG\"", "COBOLNET2630")]
    [InlineData("B\"012\"", "COBOLNET2630")]
    public void LiteralRepertoire_IsScreenedInAnOmittedBranch(string literal, string code)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Program("DLR", ">>IF 1 = 2\n>>DEFINE D AS " + literal + "\n>>END-IF"), 2023);
        Assert.False(ok, $"{literal} shall be rejected even in an omitted branch (§7.2.1: directives are syntactically correct)");
        Assert.Single(errors, e => e.Contains(code));
    }

    /// <summary>A directive literal that the rules refuse has NO value, so a later reference neither evaluates it nor
    /// reports a second, misleading error about it: the one diagnostic is the literal's own.</summary>
    [Fact]
    public void RefusedLiteral_ReportsOnce_AndIsNeverEvaluated()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Program("DLONCE", ">>DEFINE V AS \"A\"\n>>IF V = X\"414\"\n>>END-IF"), 2023);
        Assert.False(ok);
        Assert.Single(errors);
        Assert.Contains("COBOLNET1635", errors[0]);
    }

    /// <summary>The edition gate of the literal's FORMAT (§8.3.3.1: the zero-length literal is a COBOL-2014 introduction,
    /// VCR row 7.30, kb/Work PB895) reaches a directive operand in a COMPILED branch — and only there, as the directive's
    /// own gate and the boolean-operator gate do: an omitted branch is not compiled.</summary>
    [Theory]
    [InlineData(2002, ">>DEFINE ZL AS \"\"", false)]
    [InlineData(2014, ">>DEFINE ZL AS \"\"", true)]
    [InlineData(2023, ">>DEFINE ZL AS \"\"", true)]
    [InlineData(2002, ">>DEFINE V AS \"A\"\n>>IF V = \"\"\n>>END-IF", false)]
    [InlineData(2002, ">>DEFINE V AS X\"41\"", true)]
    [InlineData(2002, ">>IF 1 = 2\n>>DEFINE ZL AS \"\"\n>>END-IF", true)]
    [InlineData(2002, ">>IF 1 = 2\n>>IF \"A\" = \"\"\n>>END-IF\n>>END-IF", true)]
    public void LiteralFormatEdition_IsGatedInACompiledBranchOnly(int edition, string directives, bool legal)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("DLG" + edition, directives), edition);
        if (legal) Assert.True(ok, string.Join("\n", errors));
        else
        {
            Assert.False(ok, "the zero-length literal is a COBOL-2014 introduction");
            Assert.Single(errors, e => e.Contains("COBOLNET0900") && e.Contains("zero-length literal"));
        }
    }
}
