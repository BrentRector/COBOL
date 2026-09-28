// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// PERFORM Format 3 (§14.9.28) grammar parse tests (design §6.1). These are PARSE-only — they exercise the
/// greedy-safe grammar (the <c>whenOperandAhead()</c> continuation predicate) and the Formats-2/3 merge in
/// isolation from the binder/emitter. The load-bearing regression net is defect-2: a WHEN body's leading verb
/// (RESUME, RAISE, …) must NOT be annexed as a spurious second exception-name.
/// </summary>
public sealed class PerformFormat3ParseTests
{
    private static (CobolParserCore.CompilationUnitContext? Tree, DiagnosticBag Diags) Parse(string src, int edition)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_p3_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = edition }.Parse(path, diags);
            return (tree, diags);
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    private static string Prog(string procBody) =>
        "IDENTIFICATION DIVISION.\n" +
        "PROGRAM-ID. P3PARSE.\n" +
        "DATA DIVISION.\n" +
        "WORKING-STORAGE SECTION.\n" +
        "01 N PIC 9 VALUE 0.\n" +
        "PROCEDURE DIVISION.\n" +
        "MAIN.\n" + procBody + "\n";

    private static IEnumerable<T> Descendants<T>(IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T t) yield return t;
            foreach (var d in Descendants<T>(child)) yield return d;
        }
    }

    private static void AssertParses((CobolParserCore.CompilationUnitContext? Tree, DiagnosticBag Diags) r)
    {
        Assert.NotNull(r.Tree);
        Assert.False(r.Diags.HasErrors, string.Join("\n", r.Diags.Diagnostics.Select(d => d.ToString())));
    }

    // ── (1) The canonical WHEN body — defect-2 regression: RESUME is imperative-statement-2, not a 2nd EC-name.
    [Fact]
    public void CanonicalWhenBody_ResumeIsImp2_NotSecondExceptionName()
    {
        var r = Parse(Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-I-O-PERMANENT-ERROR RESUME AT NEXT STATEMENT\n    END-PERFORM.\n    STOP RUN."), 2023);
        AssertParses(r);
        // Exactly ONE exception-name operand (EC-I-O-PERMANENT-ERROR); RESUME was NOT swallowed as a second one.
        var ecItems = Descendants<CobolParserCore.PerformWhenEcItemContext>(r.Tree!).ToList();
        Assert.Single(ecItems);
        // The RESUME landed as the WHEN body (a resumeStatement inside the phrase).
        Assert.Single(Descendants<CobolParserCore.ResumeStatementContext>(r.Tree!));
    }

    // Every one of the 11 continuation-stop verbs must parse as the leading verb of imperative-statement-2.
    [Theory]
    [InlineData("RESUME AT NEXT STATEMENT")]
    [InlineData("RESUME NEXT STATEMENT")]
    [InlineData("RAISE EXCEPTION EC-USER-X")]
    [InlineData("VALIDATE N")]
    [InlineData("UNLOCK F")]
    [InlineData("SEND TO \"Q\" FROM N")]
    [InlineData("RECEIVE FROM N GIVING N N")]
    [InlineData("COMMIT")]
    [InlineData("ROLLBACK")]
    public void StopVerb_ParsesAsImp2_NotAnnexed(string imp2)
    {
        // A file F for UNLOCK / a data-item — provide both so each verb is well-formed.
        string ws = "01 N PIC 9 VALUE 0.\n";
        string env = "ENVIRONMENT DIVISION.\nINPUT-OUTPUT SECTION.\nFILE-CONTROL.\n" +
                     "  SELECT F ASSIGN \"f.dat\" ORGANIZATION LINE SEQUENTIAL.\n";
        string fd = "FILE SECTION.\nFD F.\n01 R PIC X(4).\n";
        string src =
            "IDENTIFICATION DIVISION.\nPROGRAM-ID. P3STOP.\n" + env +
            "DATA DIVISION.\n" + fd + "WORKING-STORAGE SECTION.\n" + ws +
            "PROCEDURE DIVISION.\nMAIN.\n" +
            "    PERFORM\n        ADD 1 TO N\n    WHEN EC-BOUND-SUBSCRIPT " + imp2 + "\n    END-PERFORM.\n    STOP RUN.\n";
        var r = Parse(src, 2023);
        AssertParses(r);
        Assert.Single(Descendants<CobolParserCore.PerformWhenEcItemContext>(r.Tree!));
    }

    // ── (2) RESUME AT? — all four spellings parse (the §14.9.33.2 AT-optional fix).
    [Theory]
    [InlineData("RESUME NEXT STATEMENT")]
    [InlineData("RESUME AT NEXT STATEMENT")]
    [InlineData("RESUME P2")]
    [InlineData("RESUME AT P2")]
    public void ResumeAtOptional_AllFourSpellingsParse(string resume)
    {
        string src =
            "IDENTIFICATION DIVISION.\nPROGRAM-ID. P3RES.\nPROCEDURE DIVISION.\n" +
            "DECLARATIVES.\nD SECTION.\n  USE AFTER EXCEPTION CONDITION EC-ALL.\nDP.\n    " + resume + ".\n" +
            "END DECLARATIVES.\nMAIN SECTION.\nM.\n    CONTINUE.\nP2.\n    STOP RUN.\n";
        AssertParses(Parse(src, 2023));
    }

    // ── (3) Each WHEN operand form parses.
    [Theory]
    [InlineData("WHEN EXCEPTION INPUT DISPLAY \"x\"")]
    [InlineData("WHEN EXCEPTION OUTPUT DISPLAY \"x\"")]
    [InlineData("WHEN EXCEPTION I-O DISPLAY \"x\"")]
    [InlineData("WHEN EXCEPTION EXTEND DISPLAY \"x\"")]
    [InlineData("WHEN EC-BOUND-SUBSCRIPT DISPLAY \"x\"")]
    [InlineData("WHEN EC-BOUND-SUBSCRIPT EC-SIZE DISPLAY \"x\"")]
    public void WhenOperandForms_Parse(string when)
    {
        var r = Parse(Prog("    PERFORM\n        ADD 1 TO N\n    " + when + "\n    END-PERFORM.\n    STOP RUN."), 2023);
        AssertParses(r);
    }

    // A pure-reserved verb (DISPLAY) needs no predicate — the cobolWord element stops the loop at it.
    [Fact]
    public void PureReservedVerb_StopsOperandLoop_WithoutPredicate()
    {
        var r = Parse(Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-BOUND-SUBSCRIPT DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN."), 2023);
        AssertParses(r);
        Assert.Single(Descendants<CobolParserCore.PerformWhenEcItemContext>(r.Tree!));
    }

    // ── (4) The trailing WHEN OTHER / WHEN COMMON / FINALLY lines, with and without the optional 2nd EXCEPTION.
    [Theory]
    [InlineData("WHEN OTHER DISPLAY \"o\"")]
    [InlineData("WHEN OTHER EXCEPTION DISPLAY \"o\"")]
    [InlineData("WHEN COMMON DISPLAY \"c\"")]
    [InlineData("WHEN COMMON EXCEPTION DISPLAY \"c\"")]
    public void OtherCommon_OptionalSecondException_Parse(string phrase)
    {
        AssertParses(Parse(Prog("    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"s\"\n    " + phrase + "\n    END-PERFORM.\n    STOP RUN."), 2023));
    }

    [Fact]
    public void FullFormat3_WithHeadOtherCommonFinally_Parses()
    {
        var r = Parse(Prog(
            "    PERFORM WITH LOCATION\n" +
            "        ADD 1 TO N\n" +
            "    WHEN EC-SIZE DISPLAY \"s\"\n" +
            "    WHEN OTHER EXCEPTION DISPLAY \"o\"\n" +
            "    WHEN COMMON EXCEPTION DISPLAY \"c\"\n" +
            "    FINALLY DISPLAY \"f\"\n" +
            "    END-PERFORM.\n    STOP RUN."), 2023);
        AssertParses(r);
        Assert.Single(Descendants<CobolParserCore.PerformFinallyContext>(r.Tree!));
        Assert.Single(Descendants<CobolParserCore.PerformWhenOtherContext>(r.Tree!));
        Assert.Single(Descendants<CobolParserCore.PerformWhenCommonContext>(r.Tree!));
    }

    // imp-2 immediately followed by FINALLY: FINALLY must be the phrase, not a 2nd exception-name.
    // ⛔ THE SUBJECT IS THE OPERAND BOUNDARY, AND IT IS MEASURED ON A CONFORMING STATEMENT (kb/Work PB396).
    // This case used to write `WHEN EC-SIZE` with an EMPTY imperative-statement-2. §14.9.28.2 Format 3 prints
    // imperative-statement-2 UNBRACKETED inside the WHEN brace, and §5.2.6.3 makes a brace group's content
    // required, so the empty spelling was never legal — the grammar accepted it only because the body was
    // written `statementBlock*`. The boundary question (does `whenOperandAhead()` let FINALLY be annexed as a
    // second exception-name?) is the same question with a body present, and is asked here on source the
    // standard admits; the empty form's REFUSAL is pinned by the companion assertion below.
    [Fact]
    public void Imp2_ThenFinally_FinallyIsThePhrase_NotAnEcOperand()
    {
        var r = Parse(Prog(
            "    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"s\"\n    FINALLY DISPLAY \"f\"\n"
            + "    END-PERFORM.\n    STOP RUN."), 2023);
        AssertParses(r);
        Assert.Single(Descendants<CobolParserCore.PerformFinallyContext>(r.Tree!));
        // EC-SIZE is the ONLY operand — FINALLY was not swallowed as a second exception-name.
        Assert.Single(Descendants<CobolParserCore.PerformWhenEcItemContext>(r.Tree!));
    }

    /// <summary>The same statement with imperative-statement-2 OMITTED is refused: §14.9.28.2 Format 3 leaves it
    /// unbracketed inside the WHEN brace, and §5.2.6.2 licenses an omission only for a bracketed portion
    /// (kb/Work PB396).</summary>
    [Fact]
    public void EmptyImp2_IsRejected()
    {
        var r = Parse(Prog(
            "    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE\n    FINALLY DISPLAY \"f\"\n    END-PERFORM.\n"
            + "    STOP RUN."), 2023);
        Assert.True(r.Diags.HasErrors,
            "a WHEN phrase with no imperative-statement-2 parsed — ISO §14.9.28.2 Format 3 prints it unbracketed.");
    }

    [Fact]
    public void FilePairedOperand_Parses()
    {
        string src =
            "IDENTIFICATION DIVISION.\nPROGRAM-ID. P3FILE.\n" +
            "ENVIRONMENT DIVISION.\nINPUT-OUTPUT SECTION.\nFILE-CONTROL.\n" +
            "  SELECT F1 ASSIGN \"f1\" ORGANIZATION LINE SEQUENTIAL.\n" +
            "  SELECT F2 ASSIGN \"f2\" ORGANIZATION LINE SEQUENTIAL.\n" +
            "DATA DIVISION.\nFILE SECTION.\nFD F1.\n01 R1 PIC X.\nFD F2.\n01 R2 PIC X.\n" +
            "WORKING-STORAGE SECTION.\n01 N PIC 9 VALUE 0.\n" +
            "PROCEDURE DIVISION.\nMAIN.\n" +
            "    PERFORM\n        ADD 1 TO N\n    WHEN EC-I-O-PERMANENT-ERROR FILE F1 FILE F2 DISPLAY \"e\"\n    END-PERFORM.\n    STOP RUN.\n";
        var r = Parse(src, 2023);
        AssertParses(r);
    }

    // The OTHER greedy-safe branch: performWhenModeList's file-name continuation gate (WHEN EXCEPTION file…).
    [Fact]
    public void ModeListFileContinuation_TwoFiles_Parse_AndStopVerbNotAnnexed()
    {
        string env = "ENVIRONMENT DIVISION.\nINPUT-OUTPUT SECTION.\nFILE-CONTROL.\n" +
            "  SELECT F1 ASSIGN \"f1\" ORGANIZATION LINE SEQUENTIAL.\n" +
            "  SELECT F2 ASSIGN \"f2\" ORGANIZATION LINE SEQUENTIAL.\n";
        string fd = "FILE SECTION.\nFD F1.\n01 R1 PIC X.\nFD F2.\n01 R2 PIC X.\n";
        string head = "IDENTIFICATION DIVISION.\nPROGRAM-ID. P3ML.\n" + env + "DATA DIVISION.\n" + fd +
            "WORKING-STORAGE SECTION.\n01 N PIC 9 VALUE 0.\nPROCEDURE DIVISION.\nMAIN.\n";
        // Two files in one WHEN EXCEPTION (the continuation gate keeps both as file operands).
        AssertParses(Parse(head + "    PERFORM\n        ADD 1 TO N\n    WHEN EXCEPTION F1 F2 DISPLAY \"x\"\n    END-PERFORM.\n    STOP RUN.\n", 2023));
        // A stop-verb after one file operand must NOT be annexed as a second file-name (RESUME is imp-2).
        var r = Parse(head + "    PERFORM\n        ADD 1 TO N\n    WHEN EXCEPTION F1 RESUME NEXT STATEMENT\n    END-PERFORM.\n    STOP RUN.\n", 2023);
        AssertParses(r);
        Assert.Single(Descendants<CobolParserCore.ResumeStatementContext>(r.Tree!));
    }

    // ── (5) Format-2 (no WHEN) must still parse — the merge is non-regressive.
    [Theory]
    [InlineData("    PERFORM 3 TIMES\n        ADD 1 TO N\n    END-PERFORM.")]
    [InlineData("    PERFORM UNTIL N > 5\n        ADD 1 TO N\n    END-PERFORM.")]
    [InlineData("    PERFORM VARYING N FROM 1 BY 1 UNTIL N > 5\n        DISPLAY N\n    END-PERFORM.")]
    [InlineData("    PERFORM\n        ADD 1 TO N\n    END-PERFORM.")]
    public void Format2Inline_StillParses_NoRegression(string body)
    {
        AssertParses(Parse(Prog(body + "\n    STOP RUN."), 2023));
    }

    // ── (6) LOCATION head + the continuity invariant.
    [Fact]
    public void BareLocationHead_ParsesAt2023()
    {
        AssertParses(Parse(Prog("    PERFORM LOCATION\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"s\"\n    END-PERFORM.\n    STOP RUN."), 2023));
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    public void PerformLocation_ParsesAsOutOfLine_BelowIntroduction(int edition)
    {
        // A paragraph named LOCATION + `PERFORM LOCATION` (out-of-line) is legal below 2023 (LOCATION is a user word).
        string src =
            "IDENTIFICATION DIVISION.\nPROGRAM-ID. P3LOC.\nPROCEDURE DIVISION.\n" +
            "MAIN.\n    PERFORM LOCATION.\n    STOP RUN.\nLOCATION.\n    CONTINUE.\n";
        AssertParses(Parse(src, edition));
    }
}
