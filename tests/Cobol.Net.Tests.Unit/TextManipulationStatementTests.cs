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
/// kb/Work PB1357 / PB1358 / PB1353 / PB1356 — the COPY and REPLACE statements as the text-manipulation stage reads
/// them: the §7.2.4.4 GR4–GR7 REPLACE state machine (ALSO pushes, LAST pops, OFF cancels), a REPLACE statement
/// recognized wherever REPLACE is a text-word (§7.2.4.3 SR1), the operand content rules shared word for word by
/// §7.2.3.3 and §7.2.4.3 (one screen), the §8.3.5 6) pseudo-text delimiter separation, and the rules on the text a
/// replacing action produces (§7.2.3.4 GR12 / GR13, §7.2.4.4 GR9). Every case runs the real merged driver.
/// </summary>
public sealed class TextManipulationStatementTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB1357_" + Guid.NewGuid().ToString("N")[..8]);

    public TextManipulationStatementTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ } }

    private void Copybook(string name, string content) => File.WriteAllText(Path.Combine(_dir, name), content);

    private (string Text, DiagnosticBag Diags) Run(string mainText, int edition = 2023)
    {
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, "t.cob", dialectLevel: edition, permissive: false);
        string text = ConditionalCompilationProcessor.ProcessWithCopy(mainText, copy,
            CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: edition);
        return (text, bag);
    }

    private static void Clean(DiagnosticBag bag) => Assert.False(bag.HasErrors, string.Join("\n", bag.Diagnostics));

    private static void Rejects(DiagnosticBag bag, string code, string fragment)
        => Assert.True(bag.Diagnostics.Any(d => d.Code == code && d.Message.Contains(fragment, StringComparison.Ordinal)),
            $"expected {code} mentioning '{fragment}'; got:\n{string.Join("\n", bag.Diagnostics)}");

    /// <summary>The words of the resultant text that stand where <c>SHOW</c> markers were: the argument of each.</summary>
    private static string[] Shown(string text)
        => text.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("SHOW ", StringComparison.Ordinal))
            .Select(l => l[5..].Trim()).ToArray();

    // ── PB1357: §7.2.4.4 GR4–GR7 ─────────────────────────────────────────────────────────────────────────────────

    [Fact] // GR7 a) ALSO: current operands first, then the pushed statement's; GR7 c) LAST OFF pops it back.
    public void AlsoStacks_CurrentFirst_AndLastOffPops()
    {
        var (text, bag) = Run(" REPLACE ==X1== BY ==A1==.\n SHOW X1 X2\n REPLACE ALSO ==X2== BY ==B2==.\n SHOW X1 X2\n"
            + " REPLACE ALSO ==X1== BY ==C1==.\n SHOW X1 X2\n REPLACE LAST OFF.\n SHOW X1 X2\n REPLACE LAST OFF.\n"
            + " SHOW X1 X2\n REPLACE LAST OFF.\n SHOW X1 X2\n");
        Clean(bag);
        Assert.Equal(["A1 X2", "A1 B2", "C1 B2", "A1 B2", "A1 X2", "X1 X2"], Shown(text));
    }

    [Fact] // GR6 a) ALSO with none active has no effect; GR6 b) a format 2 statement with none active has no effect.
    public void WithNoneActive_AlsoAndLastOffHaveNoEffect()
    {
        var (text, bag) = Run(" REPLACE LAST OFF.\n SHOW X1\n REPLACE ALSO ==X1== BY ==A1==.\n SHOW X1\n REPLACE OFF.\n"
            + " REPLACE OFF.\n SHOW X1\n");
        Clean(bag);
        Assert.Equal(["X1", "A1", "X1"], Shown(text));
    }

    [Fact] // GR7 b) a format 1 statement without ALSO cancels the active one AND the queue; GR7 d) OFF cancels all.
    public void WithoutAlso_TheQueueIsCanceled_AndOffCancelsAll()
    {
        var (text, bag) = Run(" REPLACE ==X1== BY ==A1==.\n REPLACE ALSO ==X2== BY ==B2==.\n REPLACE ==X3== BY ==C3==.\n"
            + " SHOW X1 X2 X3\n REPLACE LAST OFF.\n SHOW X1 X2 X3\n REPLACE ==X1== BY ==A1==.\n"
            + " REPLACE ALSO ==X2== BY ==B2==.\n REPLACE OFF.\n REPLACE LAST OFF.\n SHOW X1 X2 X3\n");
        Clean(bag);
        Assert.Equal(["X1 X2 C3", "X1 X2 X3", "X1 X2 X3"], Shown(text));
    }

    [Theory] // constructs row replace-also-last-2002: both phrases are COBOLNET0900 below 2002, silent from 2002.
    [InlineData(85, true)]
    [InlineData(2002, false)]
    [InlineData(2014, false)]
    [InlineData(2023, false)]
    public void AlsoAndLast_AreGatedBelow2002(int edition, bool rejected)
    {
        var (_, bag) = Run(" REPLACE ALSO ==X1== BY ==A1==.\n REPLACE LAST OFF.\n", edition);
        Assert.Equal(rejected ? 2 : 0, bag.Diagnostics.Count(d => d.Code == "COBOLNET0900"));
    }

    [Theory] // constructs row replacing-partial-word-2002 (kb/Work PB1670): ONE gate for COPY and REPLACE, once per statement.
    [InlineData(85, 2)]
    [InlineData(2002, 0)]
    [InlineData(2014, 0)]
    [InlineData(2023, 0)]
    public void PartialWordPhrases_AreGatedBelow2002_OncePerStatement(int edition, int expected)
    {
        Copybook("bkpw.cpy", " 01 PFX-A PIC X.\n");
        var (_, bag) = Run(" COPY bkpw REPLACING LEADING ==PFX-== BY ==C1-== TRAILING ==-A== BY ==-B==.\n"
            + " REPLACE LEADING ==X1== BY ==Y1== TRAILING ==X2== BY ==Y2==.\n", edition);
        Assert.Equal(expected, bag.Diagnostics.Count(d => d.Code == "COBOLNET0900"));
    }

    [Fact] // the whole-pseudo-text operands of both statements are 1985 forms: no gate at any edition
    public void WholePseudoTextOperands_AreNotGated()
    {
        Copybook("bkpw.cpy", " 01 PFX-A PIC X.\n");
        var (_, bag) = Run(" COPY bkpw REPLACING ==PFX-A== BY ==C1-A==.\n REPLACE ==X1== BY ==Y1==.\n", 85);
        Assert.DoesNotContain(bag.Diagnostics, d => d.Code == "COBOLNET0900");
    }

    [Fact] // §7.2.4.2 format 2: LAST is followed by OFF.
    public void LastWithoutOff_IsASyntaxError()
    {
        var (_, bag) = Run(" REPLACE LAST ==X1== BY ==A1==.\n");
        Rejects(bag, "COBOLNET2449", "REPLACE LAST is followed by OFF");
    }

    // ── PB1358: §7.2.4.3 SR1 / SR2 ───────────────────────────────────────────────────────────────────────────────

    [Fact] // SR1: a REPLACE statement after another statement on the same line, and inside a statement.
    public void Replace_IsRecognizedMidLine()
    {
        var (text, bag) = Run(" DISPLAY \"START\". REPLACE ==X1== BY ==A1==.\n SHOW X1\n DISPLAY \"B\" REPLACE ==X2== BY ==B2==. X2\n");
        Clean(bag);
        Assert.Equal(["A1"], Shown(text));
        Assert.Contains("DISPLAY \"B\"", text);
        Assert.Contains("B2", text);
        Assert.DoesNotContain("REPLACE", text);
    }

    [Fact] // A word that merely contains REPLACE, or REPLACE inside a literal, is no REPLACE statement.
    public void ReplaceInsideAWordOrLiteral_IsNotAStatement()
    {
        var (text, bag) = Run(" 01 REPLACE-FLAG PIC X(9) VALUE \"REPLACE ==\".\n SHOW REPLACE-FLAG\n");
        Clean(bag);
        Assert.Equal(["REPLACE-FLAG"], Shown(text));
    }

    [Fact] // SR2: a REPLACE not preceded by a space.
    public void ReplaceAfterAParenthesis_IsNotPrecededByASpace()
    {
        var (_, bag) = Run(" MOVE (REPLACE ==X1== BY ==A1==.\n");
        Rejects(bag, "COBOLNET2449", "§7.2.4.3 SR2");
    }

    [Fact] // SR2: REPLACE glued behind a period forms no statement, and says so.
    public void ReplaceGluedBehindAPeriod_IsDiagnosed()
    {
        var (_, bag) = Run(" DISPLAY X.REPLACE ==X1== BY ==A1==.\n");
        Rejects(bag, "COBOLNET2449", "REPLACE is glued");
    }

    // ── PB1353: the operand content rules (§7.2.3.3 / §7.2.4.3 — one screen) and §8.3.5 6) ──────────────────────

    [Theory]
    [InlineData(" REPLACE ==,== BY ==AAA==.\n", "§7.2.4.3 SR3")]
    [InlineData(" REPLACE ==== BY ==AAA==.\n", "§7.2.4.3 SR3")]
    [InlineData(" REPLACE LEADING ==OLD- XYZ== BY ==NEW-==.\n", "§7.2.4.3 SR5")]
    [InlineData(" REPLACE LEADING ==== BY ==NEW-==.\n", "§7.2.4.3 SR5")]
    [InlineData(" REPLACE LEADING ==OLD-== BY ==NEW- X==.\n", "§7.2.4.3 SR6")]
    [InlineData(" REPLACE LEADING ==\"AB\"== BY ==XY==.\n", "§7.2.4.3 SR7")]
    [InlineData(" REPLACE TRAILING ==AB== BY ==\"XY\"==.\n", "§7.2.4.3 SR7")]
    [InlineData(" REPLACE ==XX1\n >>TURN EC-ALL CHECKING OFF\n== BY ==AAA==.\n", "§7.2.4.3 SR10")]
    public void ReplaceOperandContent_IsScreened(string source, string rule)
    {
        var (_, bag) = Run(source);
        Rejects(bag, "COBOLNET2572", rule);
    }

    [Theory]
    [InlineData("COPY bk1 REPLACING ==,== BY ==BB==.", "§7.2.3.3 SR6")]
    [InlineData("COPY bk1 REPLACING LEADING ==AA -X== BY ==CC==.", "§7.2.3.3 SR11")]
    [InlineData("COPY bk1 REPLACING LEADING ==AA== BY ==CC DD==.", "§7.2.3.3 SR12")]
    [InlineData("COPY bk1 REPLACING LEADING ==\"HE\"== BY ==XY==.", "§7.2.3.3 SR13")]
    public void CopyOperandContent_IsScreened(string statement, string rule)
    {
        Copybook("bk1.cpy", " 01 AA-X PIC X(5) VALUE \"HELLO\".\n");
        var (_, bag) = Run(" " + statement + "\n");
        Rejects(bag, "COBOLNET2572", rule);
    }

    [Fact] // SR9: a text-word longer than 65,535 characters, in pseudo-text and (COPY) in library text.
    public void OverlongTextWord_IsScreened_InPseudoTextAndLibraryText()
    {
        string word = new('Q', 70_000);
        var (_, bag) = Run(" REPLACE ==" + word + "== BY ==A==.\n");
        Rejects(bag, "COBOLNET2572", "§7.2.4.3 SR9");
        Copybook("bklong.cpy", " 01 " + word + " PIC X.\n");
        var (_, bag2) = Run(" COPY bklong.\n");
        Rejects(bag2, "COBOLNET2572", "§7.2.3.3 SR9");
    }

    [Fact] // The legal forms stay legal: a comma beside a word, an empty pseudo-text-2, '==' inside a literal.
    public void LegalOperands_AreNotScreened()
    {
        var (text, bag) = Run(" REPLACE ==X1 , X2== BY ==A1== ==X3== BY ==== ==X4== BY ==\"A==B\"==\n"
            + " LEADING ==P-== BY ====.\n SHOW X1 , X2 X3 X4 P-Q\n");
        Clean(bag);
        Assert.Equal("A1 \"A==B\" Q", string.Join(' ', Shown(text).Single().Split(' ', StringSplitOptions.RemoveEmptyEntries)));
    }

    [Theory] // §8.3.5 6): an opening == is preceded by a space; a closing == is followed by a space, ',' ';' or '.'.
    [InlineData(" REPLACE ==XX==BY ==DISPLAY==.\n", "closing ==")]
    [InlineData(" REPLACE ==XX== BY==DISPLAY==.\n", "opening ==")]
    public void PseudoTextDelimiters_AreSeparated(string source, string which)
    {
        var (_, bag) = Run(source);
        Rejects(bag, "COBOLNET2573", which);
    }

    // ── PB1356: the text a replacing action produces ─────────────────────────────────────────────────────────────

    [Fact] // §7.2.4.4 GR9: REPLACE shall not produce a REPLACE statement or (via a partial-word) a COPY statement.
    public void ReplaceResult_ShallNotHoldCopyOrReplace()
    {
        var (_, bag) = Run(" REPLACE ==YY== BY ==REPLACE==.\n YY\n");
        Rejects(bag, "COBOLNET2574", "a REPLACE statement");
        var (_, bag2) = Run(" REPLACE LEADING ==ZZ== BY ==CO==.\n ZZPY CPB.\n");
        Rejects(bag2, "COBOLNET2574", "a COPY statement");
    }

    [Fact] // §7.2.4.4 GR9 "shall not contain … a comment": a partial-word result that SPELLS a comment indicator — the
           // text-word scanner skips a comment, so this is asked of the produced TEXT, never of a word; a *> inside a
           // produced literal is literal content, not a comment.
    public void ReplaceResult_ShallNotHoldAComment_ButALiteralMayHoldItsCharacters()
    {
        var (_, bag) = Run(" REPLACE LEADING ==Q== BY ==*==.\n SHOW Q>1 \"B\".\n");
        Rejects(bag, "COBOLNET2574", "a comment");
        var (_, bag2) = Run(" REPLACE LEADING ==Q== BY ==X*==.\n SHOW Q>1\n");
        Rejects(bag2, "COBOLNET2574", "a comment");
        var (text, bag3) = Run(" REPLACE ==YY== BY ==\"*>\"==.\n SHOW YY\n");
        Clean(bag3);
        Assert.Equal(["\"*>\""], Shown(text));
    }

    [Fact] // §7.2.3.4 GR13 "shall not introduce … a comment" — the COPY twin of the REPLACE rule, through the one screen.
    public void CopyResult_ShallNotHoldAComment()
    {
        Copybook("bkcm.cpy", " SHOW Q>1 \"B\".\n");
        var (_, bag) = Run(" COPY bkcm REPLACING LEADING ==Q== BY ==*==.\n");
        Rejects(bag, "COBOLNET2574", "§7.2.3.4 GR13");
    }

    [Fact] // DOC-A.1-40: only a period in the FILE NAME is an extension; a period in a directory part ("../lib/BOOK",
           // "lib.v2/BOOK") still lets the name take the copybook suffixes.
    public void LibraryText_APeriodInADirectoryPart_IsNotAnExtension()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "lib.v2"));
        File.WriteAllText(Path.Combine(_dir, "lib.v2", "bookd.cpy"), " SHOW FOUND-D\n");
        string up = "../" + Path.GetFileName(_dir) + "/lib.v2/bookd";
        foreach (var name in new[] { "lib.v2/bookd", "./lib.v2/bookd", up })
        {
            var (text, bag) = Run($" COPY \"{name}\".\n");
            Clean(bag);
            Assert.Equal(["FOUND-D"], Shown(text));
        }
        File.WriteAllText(Path.Combine(_dir, "lib.v2", "bookd.v1.cpy"), " SHOW NOT-THIS\n");
        var (_, missing) = Run(" COPY \"lib.v2/bookd.v1\".\n");   // the file name has a period: tried only as spelled
        Assert.Contains(missing.Diagnostics, d => d.Code == "CBL3620");
    }

    [Fact] // §6.5 2): a blank line written inside pseudo-text-2 was logically discarded before text manipulation, so the
           // replacement produces no blank line (the NIST SM208A shape); an operand that never matches produces nothing.
    public void ReplaceResult_ABlankLineInPseudoText2_IsDiscarded_AndAnUnmatchedOperandProducesNothing()
    {
        var (text, bag) = Run(" REPLACE ==YY== BY ==AA\n\n BB==.\n YY\n");
        Clean(bag);
        Assert.Contains("AA", text);
        Assert.Contains("BB", text);
        var (_, bag2) = Run(" REPLACE LEADING ==ZZ== BY ==CO==.\n NEVER-MATCHED\n");
        Clean(bag2);
    }

    [Fact] // §7.2.3.4 GR13: the COPY replacing action shall not introduce a COPY statement (here via LEADING); a
           // REPLACE statement it produces is legal (§7.2.1: REPLACE is read after COPY's replacing action).
    public void CopyResult_ShallNotHoldCopy_ButMayHoldReplace()
    {
        Copybook("bkr.cpy", " ZZPY bkx.\n");
        var (_, bag) = Run(" COPY bkr REPLACING LEADING ==ZZ== BY ==CO==.\n");
        Rejects(bag, "COBOLNET2574", "§7.2.3.4 GR13");
        Copybook("bkrep.cpy", " YY ==X1== BY ==A1==.\n SHOW X1\n");
        var (text, bag2) = Run(" COPY bkrep REPLACING ==YY== BY ==REPLACE==.\n");
        Clean(bag2);
        Assert.Equal(["A1"], Shown(text));
    }

    [Fact] // §7.2.3.4 GR12: a COPY inside library text shall not have a REPLACING phrase.
    public void NestedCopyWithReplacing_IsRejected()
    {
        Copybook("p6in.cpy", " 01 IN-A PIC X(2) VALUE \"IN\".\n");
        Copybook("p6out.cpy", " COPY p6in REPLACING ==IN-A== BY ==IN-B==.\n");
        var (_, bag) = Run(" COPY p6out.\n");
        Rejects(bag, "COBOLNET1640", "GR12");
        Copybook("p6ok.cpy", " COPY p6in.\n");
        var (text, bag2) = Run(" COPY p6ok.\n");
        Clean(bag2);
        Assert.Contains("IN-A", text);
    }
}
