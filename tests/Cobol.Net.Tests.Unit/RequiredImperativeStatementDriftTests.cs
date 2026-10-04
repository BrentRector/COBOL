// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN IMPERATIVE-STATEMENT OPERAND THE GENERAL FORMAT LEAVES UNBRACKETED IS REQUIRED, AND THE QUANTIFIER IS
/// WHERE THAT RULE LIVES (kb/Work PB396).
///
/// <para><b>The rule, one sentence.</b> ISO §5.2.6.2 gives the omission licence to BRACKETED portions only —
/// "Brackets, [ ], enclosing a portion of a general format indicate that the syntax element contained within the
/// brackets … may be explicitly specified or that portion of the general format may be omitted" — and §5.2.6.3
/// says a brace group "shall be explicitly specified". §14.9.19.3 SR1 states the same cardinality a second way
/// for IF: "Statement-1 and statement-2 represent either one or more imperative statements or a conditional
/// statement optionally preceded by one or more imperative statements".</para>
///
/// <para><b>What was measured before the fix.</b> Every USE of <c>statementBlock</c> in the control-flow grammar
/// was written <c>statementBlock*</c>, which restores the zero case the printed formats forbid, so
/// <c>IF X = 1 END-IF</c>, a WHEN phrase with no body, <c>PERFORM UNTIL … END-PERFORM</c> with no body and a
/// SEARCH WHEN with no body all compiled SILENTLY at every edition — the PERFORM case into a loop whose
/// condition its (absent) body cannot change.</para>
///
/// <para><b>Why the guard is over the GRAMMAR SOURCE and not over a list of verbs.</b> The requirement is a
/// property of the quantifier, so the drift test asserts the quantifier: no <c>statementBlock*</c> (and no
/// <c>statementBlock?</c>) anywhere in the grammar. That makes the NEXT general format automatic — a new phrase
/// rule written with the zero-admitting quantifier fails here — which a per-verb list could never do.</para>
/// </summary>
public sealed class RequiredImperativeStatementDriftTests : CobolNetTestBase
{
    private static string GrammarDir()
        => Path.Combine(TestRepo.Root, "src", "Cobol.Net.Frontend", "Grammar");

    /// <summary>Grammar source with <c>//</c> line comments removed — the prose in this repo's .g4 files QUOTES
    /// the forbidden spelling when it explains why it is forbidden, and a guard that cannot tell a rule from its
    /// explanation would force the explanation out.</summary>
    private static IEnumerable<(string Path, string Text)> GrammarFiles()
    {
        foreach (string p in Directory.EnumerateFiles(GrammarDir(), "*.g4", SearchOption.AllDirectories))
            yield return (p, Regex.Replace(File.ReadAllText(p), @"//[^\n]*", ""));
    }

    /// <summary>⛔ THE INVARIANT. <c>statementBlock</c> IS <c>statement+</c>, so a bare reference already spells
    /// "one or more" — the format's own cardinality. A <c>*</c> or <c>?</c> on it spells "zero or more", which no
    /// unbracketed operand and no brace alternative in any general format means.</summary>
    [Fact]
    public void NoGrammarRule_AdmitsAnEmptyStatementBlock()
    {
        var files = GrammarFiles().ToList();
        Assert.True(files.Count >= 8, $"only {files.Count} .g4 files found under {GrammarDir()} — the scrape broke");

        var offenders = new List<string>();
        foreach (var (path, text) in files)
            foreach (Match m in Regex.Matches(text, @"statementBlock\s*[*?]"))
                offenders.Add($"{Path.GetFileName(path)}: {m.Value.Trim()}");

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} grammar reference(s) admit an EMPTY statement block: {string.Join(", ", offenders)}. "
            + "ISO §5.2.6.2 licenses an omission only for a BRACKETED portion and §5.2.6.3 requires one alternative "
            + "of a brace group to be explicitly specified, so an imperative-statement operand is required wherever "
            + "the printed general format leaves it outside brackets (kb/Work PB396).");
    }

    /// <summary>The population the invariant governs must not be empty — a guard that measures nothing is green
    /// for the wrong reason. There are nine such operand positions across IF, EVALUATE, PERFORM and SEARCH.</summary>
    [Fact]
    public void TheGrammar_StillUsesStatementBlockAtEveryPhrasePosition()
    {
        int uses = GrammarFiles().Sum(f => Regex.Matches(f.Text, @"\bstatementBlock\b").Count);
        Assert.True(uses >= 30, $"only {uses} statementBlock references — the rule was renamed or the phrases were "
            + "rewritten; this guard must move with them rather than pass vacuously");
    }

    /// <summary>⛔ WHEN OTHER IS ONE OPTIONAL PHRASE THAT FOLLOWS THE REPETITION, not a second alternative of the
    /// repeated clause (ISO §14.9.13.2, with §5.2.7: an ellipsis "applies to the portion of the format between
    /// the determined pair of delimiters" — the brace group immediately to its left, which the OTHER phrase is
    /// outside of). Written as an alternative it was admissible any number of times at any position, and
    /// <c>EvaluateBinder</c>'s <c>other = body</c> silently discarded every earlier one.</summary>
    [Fact]
    public void EvaluateWhenOther_IsASingleTrailingPhrase_NotAnAlternativeOfTheRepeatedClause()
    {
        string text = GrammarFiles().Single(f => f.Path.EndsWith("CobolControlFlow.g4", StringComparison.Ordinal)).Text;

        var clause = Regex.Match(text, @"^evaluateWhenClause\s*\r?\n\s*:(?<body>.*?);",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(clause.Success, "evaluateWhenClause not found");
        Assert.DoesNotContain("OTHER", clause.Groups["body"].Value, StringComparison.Ordinal);

        var stmt = Regex.Match(text, @"^evaluateStatement\s*\r?\n\s*:(?<body>.*?);",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(stmt.Success, "evaluateStatement not found");
        string body = stmt.Groups["body"].Value;
        int clausePos = body.IndexOf("evaluateWhenClause+", StringComparison.Ordinal);
        int otherPos = body.IndexOf("evaluateWhenOther?", StringComparison.Ordinal);
        Assert.True(clausePos >= 0, $"evaluateStatement no longer repeats evaluateWhenClause: {body}");
        Assert.True(otherPos > clausePos,
            $"evaluateWhenOther? must follow evaluateWhenClause+ — the printed format puts it after the `{{ … }} …` "
            + $"repetition, so it is admitted once and last (ISO §14.9.13.2 / §5.2.7). Body: {body}");
    }

    // ── The nine operand positions, end to end ───────────────────────────────────────────────────────────

    private const string Head =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {0}.\n       DATA DIVISION.\n"
        + "       WORKING-STORAGE SECTION.\n       01 X PIC 9(4) VALUE 1.\n       01 N PIC 9(4) VALUE 0.\n"
        + "       01 T.\n          05 R PIC 9(4) OCCURS 3 TIMES INDEXED BY I.\n"
        + "       PROCEDURE DIVISION.\n       MAIN-P.\n";

    private static string Program(string id, string body) => string.Format(Head, id) + body + "\n           STOP RUN.\n";

    /// <summary>Every unbracketed imperative-statement operand, written EMPTY, is rejected — and rejected with
    /// the named diagnostic rather than a generic parse failure, so the user is told which rule was broken.
    /// COBOLNET2072 is the empty operand; COBOLNET2073 is the WHEN OTHER phrase written out of position, which is
    /// the same format read one level up.</summary>
    [Theory]
    // IF Format 1 / Format 2 — statement-1 unbracketed, and a brace alternative (ISO §14.9.19.2).
    [InlineData("PB396A", "           IF X = 1\n           END-IF\n           DISPLAY \"A\"", "COBOLNET2072")]
    [InlineData("PB396B", "           IF X = 1\n               DISPLAY \"T\"\n           ELSE\n           END-IF", "COBOLNET2072")]
    [InlineData("PB396C", "           IF X = 1.\n           DISPLAY \"A\".", "COBOLNET2072")]
    // EVALUATE — imperative-statement-1 inside the outer brace group (ISO §14.9.13.2).
    [InlineData("PB396D", "           EVALUATE X\n               WHEN 3\n           END-EVALUATE", "COBOLNET2072")]
    [InlineData("PB396E", "           EVALUATE X\n               WHEN 3 DISPLAY \"T\"\n               WHEN OTHER\n"
                        + "           END-EVALUATE", "COBOLNET2072")]
    // …and the WHEN OTHER phrase out of position: repeated, or ahead of a WHEN phrase.
    [InlineData("PB396F", "           EVALUATE X\n               WHEN OTHER DISPLAY \"O1\"\n"
                        + "               WHEN 3 DISPLAY \"T\"\n           END-EVALUATE", "COBOLNET2073")]
    [InlineData("PB396G", "           EVALUATE X\n               WHEN 3 DISPLAY \"T\"\n"
                        + "               WHEN OTHER DISPLAY \"O1\"\n               WHEN OTHER DISPLAY \"O2\"\n"
                        + "           END-EVALUATE", "COBOLNET2073")]
    // PERFORM Format 2 — imperative-statement-1 unbracketed between the phrase bracket and END-PERFORM.
    [InlineData("PB396H", "           PERFORM UNTIL X > 5\n           END-PERFORM", "COBOLNET2072")]
    // SEARCH / SEARCH ALL — `{ imperative-statement-2 | NEXT SENTENCE }`, a brace alternation (ISO §14.9.37.2).
    [InlineData("PB396J", "           SET I TO 1\n           SEARCH R\n               AT END DISPLAY \"NF\"\n"
                        + "               WHEN R (I) = 7\n           END-SEARCH", "COBOLNET2072")]
    public void AnEmptyRequiredImperative_IsRejected(string id, string body, string code)
    {
        var (ok, _, detail) = CompileAndRun(Program(id, body));
        Assert.False(ok, $"{id} compiled and ran: an imperative-statement the general format leaves unbracketed "
            + "was accepted empty (ISO §5.2.6.2 / §5.2.6.3).");
        Assert.Contains(code, detail, StringComparison.Ordinal);
    }

    /// <summary>The wholly empty inline PERFORM — no loop-control phrase, no body — is REJECTED, which is the
    /// rule (§14.9.28.2 Format 2 prints imperative-statement-1 unbracketed). It is measured separately because
    /// it is the ONE shape that does not carry the named COBOLNET2072: with no <c>performInlineHead</c> to
    /// commit the parse, the failure is a no-viable-alternative on <c>performStatement</c>'s own alternatives
    /// decision, and at that state <c>Parser.GetExpectedTokens()</c> answers with a single token rather than the
    /// 167 that can start a statement, so the ATN test in <c>CobolErrorStrategy</c> cannot recognise it. Pinned
    /// here so the rejection cannot regress and so the message gap is measured rather than remembered.</summary>
    [Fact]
    public void AWhollyEmptyInlinePerform_IsRejected()
    {
        var (ok, _, detail) = CompileAndRun(Program("PB396I", "           PERFORM\n           END-PERFORM"));
        Assert.False(ok, "an inline PERFORM with no imperative-statement-1 was accepted (ISO §14.9.28.2 Format 2).");
        Assert.Contains("END-PERFORM", detail, StringComparison.Ordinal);
    }

    /// <summary>⛔ THE COMPLEMENT, so the guard above cannot be satisfied by rejecting the construct outright:
    /// each position written WITH its imperative still compiles and runs, and produces the value the general
    /// rules give it.</summary>
    [Theory]
    [InlineData("PB396K", "           IF X = 1\n               DISPLAY \"ONE\"\n           END-IF", "ONE")]
    [InlineData("PB396L", "           IF X = 9\n               DISPLAY \"NINE\"\n           ELSE\n"
                        + "               DISPLAY \"ONE\"\n           END-IF", "ONE")]
    [InlineData("PB396M", "           EVALUATE X\n               WHEN 1 DISPLAY \"ONE\"\n"
                        + "               WHEN OTHER DISPLAY \"OTHER\"\n           END-EVALUATE", "ONE")]
    [InlineData("PB396N", "           EVALUATE X\n               WHEN 7\n               WHEN 1 DISPLAY \"ONE\"\n"
                        + "           END-EVALUATE", "ONE")]
    [InlineData("PB396O", "           EVALUATE X\n               WHEN 7 DISPLAY \"SEVEN\"\n"
                        + "               WHEN OTHER DISPLAY \"ONE\"\n           END-EVALUATE", "ONE")]
    [InlineData("PB396P", "           PERFORM UNTIL X > 3\n               ADD 1 TO X\n           END-PERFORM\n"
                        + "           DISPLAY \"ONE\"", "ONE")]
    [InlineData("PB396Q", "           MOVE 7 TO R (2)\n           SET I TO 1\n           SEARCH R\n"
                        + "               AT END DISPLAY \"NF\"\n               WHEN R (I) = 7 DISPLAY \"ONE\"\n"
                        + "           END-SEARCH", "ONE")]
    public void TheSamePosition_WrittenWithItsImperative_StillCompilesAndRuns(string id, string body, string expected)
    {
        var (ok, stdout, detail) = CompileAndRun(Program(id, body));
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout);
    }

    // ── §14.5.1: an imperative-statement operand admits no CONDITIONAL statement (kb/Work PB351) ──────────

    /// <summary>⛔ THE OPERAND IS AN IMPERATIVE-STATEMENT IN EVERY PHRASE BUT IF'S. §14.5.1: "Any statement with a
    /// conditional phrase that is not terminated by its explicit scope terminator is a conditional statement", and an
    /// imperative statement may be a conditional statement only "delimited by its explicit scope terminator". The
    /// screen (<c>ProcedureFormatPass</c>) asks it of every <c>statementBlock</c> whose parent is not the IF statement,
    /// so each phrase family below is refused COBOLNET2796 with no code of its own — the undelimited inner statement
    /// is an IF in some rows and a conditional ADD in others, because the classification is Table 12's, not IF's.</summary>
    [Theory]
    [InlineData("PB351A", "           ADD 1 TO X ON SIZE ERROR IF X = 1 DISPLAY \"A\"\n           END-ADD")]
    [InlineData("PB351B", "           EVALUATE X\n               WHEN 1 ADD 1 TO X ON SIZE ERROR DISPLAY \"S\"\n"
                        + "               WHEN OTHER DISPLAY \"O\"\n           END-EVALUATE")]
    [InlineData("PB351C", "           EVALUATE X\n               WHEN 7 DISPLAY \"S\"\n"
                        + "               WHEN OTHER IF X = 1 DISPLAY \"O\"\n           END-EVALUATE")]
    [InlineData("PB351D", "           PERFORM 2 TIMES\n               IF X = 1 DISPLAY \"P\"\n           END-PERFORM")]
    [InlineData("PB351E", "           SET I TO 1\n           SEARCH R\n               AT END IF X = 1 DISPLAY \"NF\"\n"
                        + "               WHEN R (I) = 7 DISPLAY \"F\"\n           END-SEARCH")]
    public void AnUndelimitedConditionalStatement_AsAnImperativeOperand_IsRejected(string id, string body)
    {
        var (ok, _, detail) = CompileAndRun(Program(id, body));
        Assert.False(ok, $"{id} compiled: a conditional statement was accepted as an imperative-statement (§14.5.1).");
        Assert.Contains("COBOLNET2796", detail, StringComparison.Ordinal);
    }

    /// <summary>The complement: the same inner statements DELIMITED by their END- terminators are imperative
    /// statements and compile, and IF's statement-1 may END in an undelimited conditional statement (§14.9.19.3 SR1:
    /// "either one or more imperative statements or a conditional statement optionally preceded by one or more
    /// imperative statements"). X is 1, so ADD 9 TO X (PIC 9(4)) does not overflow and EVALUATE takes WHEN 1.</summary>
    [Theory]
    [InlineData("PB351K", "           EVALUATE X\n               WHEN 1 ADD 9 TO X ON SIZE ERROR DISPLAY \"S\" END-ADD\n"
                        + "                      DISPLAY \"ONE\"\n           END-EVALUATE", "ONE")]
    [InlineData("PB351L", "           IF X = 1\n               DISPLAY \"ONE\"\n"
                        + "               ADD 1 TO X ON SIZE ERROR DISPLAY \"S\"\n           END-IF", "ONE")]
    public void TheDelimitedForm_AndIfsOwnStatementOperand_StillCompile(string id, string body, string expected)
    {
        var (ok, stdout, detail) = CompileAndRun(Program(id, body));
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout);
    }

    // ── §14.9.13.4 GR3: the selection subject is evaluated at the BEGINNING of the statement ─────────────

    /// <summary>⛔ GR3 IS AN OBLIGATION OF THE STATEMENT, NOT OF WHICHEVER ARM READS THE SUBJECT. "At the
    /// beginning of the execution of the EVALUATE statement, each selection subject is evaluated and assigned a
    /// value, a range of values, or a truth value" (ISO §14.9.13.4 GR3). An <c>ANY</c> object makes the pair true
    /// without consulting the subject (GR4 a) 1.), so before kb/Work PB396 an EVALUATE whose objects were all ANY
    /// bound the subject lazily — which is to say never — and the out-of-range reference it names raised nothing.
    /// The A/B pair differs ONLY in the object, so the subject's evaluation is the single variable.</summary>
    [Fact]
    public void EvaluateSubject_IsEvaluated_EvenWhenNoArmReadsIt()
    {
        const string body = "           >>TURN EC-BOUND-SUBSCRIPT CHECKING ON\n"
            + "           EVALUATE R (9)\n               WHEN ANY DISPLAY \"ARM\"\n           END-EVALUATE\n"
            + "           DISPLAY \"AFTER\"";
        var (ok, stdout, detail) = CompileAndRun(Program("PB396GR3A", body), dialectLevel: 2023);
        Assert.False(ok, $"the subscript 9 on a 3-occurrence table was never evaluated: {stdout}");
        Assert.Contains("EC-BOUND-SUBSCRIPT", detail, StringComparison.Ordinal);
    }

    /// <summary>The B twin — the identical statement with an object that DOES read the subject — so the test
    /// above cannot pass because the reference is rejected for some unrelated reason.</summary>
    [Fact]
    public void EvaluateSubject_IsEvaluated_WhenAnArmReadsIt()
    {
        const string body = "           >>TURN EC-BOUND-SUBSCRIPT CHECKING ON\n"
            + "           EVALUATE R (9)\n               WHEN 5 DISPLAY \"ARM\"\n           END-EVALUATE\n"
            + "           DISPLAY \"AFTER\"";
        var (ok, _, detail) = CompileAndRun(Program("PB396GR3B", body), dialectLevel: 2023);
        Assert.False(ok);
        Assert.Contains("EC-BOUND-SUBSCRIPT", detail, StringComparison.Ordinal);
    }

    /// <summary>And the control: the SAME statement with a subscript in range runs, so the two assertions above
    /// are about the bound and not about the shape.</summary>
    [Fact]
    public void EvaluateSubject_InRange_RunsNormally()
    {
        const string body = "           >>TURN EC-BOUND-SUBSCRIPT CHECKING ON\n"
            + "           EVALUATE R (2)\n               WHEN ANY DISPLAY \"ARM\"\n           END-EVALUATE\n"
            + "           DISPLAY \"AFTER\"";
        var (ok, stdout, detail) = CompileAndRun(Program("PB396GR3C", body), dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal("ARM\r\nAFTER", stdout);
    }
}
