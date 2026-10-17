// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The CONSTRUCT POPULATION of the migration-flagging detectors (ISO §7.3.14.4 / §7.3.15.4 GR4): which constructs each
/// FLAG-02 / FLAG-14 option flags is the rule's own population, not whatever a syntactic proxy happens to see
/// (kb/Work PB1375, PB1376). One test per population edge, each paired with the neighbour it must NOT flag; every
/// expected warning is derived from the cited clause and asserted at its own source line, because a warning that names
/// no location, or collapses into an identical one, flags nothing a programmer can find.
/// <para>Programs are fixed-form with the code in area B. <see cref="FlagDirectiveTests.CompileWarnings"/> compiles
/// each at COBOL-2023 (the superset; both directives are processed there) and returns the warnings.</para>
/// </summary>
public sealed class FlagPopulationTests
{
    private static string Src(params string[] lines) => string.Concat(lines.Select(l => "       " + l + "\n"));

    /// <summary>The 1-based line of the first source line containing <paramref name="text"/>.</summary>
    private static int LineOf(string source, string text) =>
        Array.FindIndex(source.Split('\n'), l => l.Contains(text, StringComparison.Ordinal)) + 1;

    private static IReadOnlyList<string> Warnings(string source, string code) =>
        [.. FlagDirectiveTests.CompileWarnings(source).Where(w => w.Contains(code, StringComparison.Ordinal))];

    private static bool AtLine(string warning, int line) =>
        warning.Contains($"flag.cob({line},", StringComparison.Ordinal);

    /// <summary>The 1-based source line a warning is positioned at.</summary>
    private static int WarningLine(string warning) =>
        int.Parse(Regex.Match(warning, @"flag\.cob\((\d+),").Groups[1].Value);

    // ── FLAG-02 b EC-PROGRAM-EXCEPTIONS (§7.3.14.4 GR4 b): "A TURN directive for EC-ALL, EC-PROGRAM,
    //    EC-PROGRAM-ARG-OMITTED, or EC-PROGRAM-NOT-FOUND shall be flagged if 1. the source element calls any
    //    function, or 2. the source element invokes any method." A source element is "source unit excluding any
    //    contained source units" (§3.164). ──

    private const string FlagB = ">>FLAG-02 EC-PROGRAM-EXCEPTIONS ON";

    [Fact]
    public void EcProgram_EachQualifyingTurnIsFlaggedAtItsOwnLine()
    {
        // PB1376: the warning carried no location, and two identical unlocated warnings collapsed into ONE.
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. TWOTURN.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
            "01 X PIC X(3) VALUE \"abc\".", "01 Y PIC X(3).", FlagB, "PROCEDURE DIVISION.",
            ">>TURN EC-PROGRAM CHECKING ON",
            "    MOVE FUNCTION UPPER-CASE(X) TO Y.",
            ">>TURN EC-PROGRAM-NOT-FOUND CHECKING ON",
            "    DISPLAY Y.", "    STOP RUN.");
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Equal(2, warnings.Count);
        Assert.Contains(warnings, w => AtLine(w, LineOf(source, "EC-PROGRAM CHECKING")));
        Assert.Contains(warnings, w => AtLine(w, LineOf(source, "EC-PROGRAM-NOT-FOUND CHECKING")));
    }

    [Fact]
    public void EcProgram_ATurnBeforeTheIdentificationDivision_BelongsToTheUnitThatFollowsIt()
    {
        // PB1376: a TURN before IDENTIFICATION DIVISION is in no unit's span, so it was never flagged. §7.3.25.4 GR6:
        // a TURN applies to the text that follows it — the element it governs is the one it is flagged for.
        string body = "IDENTIFICATION DIVISION.|PROGRAM-ID. TURNFIRST.|DATA DIVISION.|WORKING-STORAGE SECTION.|"
            + "01 X PIC X(3) VALUE \"abc\".|01 Y PIC X(3).|PROCEDURE DIVISION.|{STMT}|    DISPLAY Y.|    STOP RUN.";
        string Program(string stmt) => Src([FlagB, ">>TURN EC-PROGRAM CHECKING ON", ..body.Replace("{STMT}", stmt).Split('|')]);

        string withCall = Program("    MOVE FUNCTION UPPER-CASE(X) TO Y.");
        var flagged = Warnings(withCall, "COBOLNET1620");
        Assert.Single(flagged);
        Assert.True(AtLine(flagged[0], 2), flagged[0]);

        Assert.Empty(Warnings(Program("    MOVE X TO Y."), "COBOLNET1620"));
    }

    [Fact]
    public void EcProgram_ContainedProgramIsItsOwnSourceElement()
    {
        // §3.164: the contained program is a source element of its own, so the container's TURN is not flagged for a
        // call that only the containee makes — and the containee's TURN is. This also compiled to a crash (the pass
        // read the Start token of the synthetic context a contained program is bound over) before PB1376.
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. OUTERP.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
            "01 X PIC X(3) VALUE \"abc\".", FlagB, "PROCEDURE DIVISION.",
            ">>TURN EC-PROGRAM CHECKING ON",
            "    CALL \"INNERP\".", "    STOP RUN.",
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. INNERP.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
            "01 Y PIC X(3).", "PROCEDURE DIVISION.",
            ">>TURN EC-PROGRAM-ARG-OMITTED CHECKING ON",
            "    MOVE FUNCTION UPPER-CASE(\"a\") TO Y.", "    EXIT PROGRAM.",
            "END PROGRAM INNERP.", "END PROGRAM OUTERP.");
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "EC-PROGRAM-ARG-OMITTED CHECKING")), warnings[0]);
    }

    [Fact]
    public void EcProgram_AFunctionWrittenWithoutTheKeyword_IsStillACall()
    {
        // §12.3.8.4 GR13 / §8.4.3.2.3 SR2: "intrinsic-function-name-1 may be specified as a function-identifier without
        // being preceded by the keyword FUNCTION" — a function reference all the same. Only the binder can tell it
        // from a subscripted data item.
        string Program(string stmt) => Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. NOKEYWD.", "ENVIRONMENT DIVISION.", "CONFIGURATION SECTION.",
            "REPOSITORY.", "    FUNCTION ALL INTRINSIC.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
            "01 X PIC X(3) VALUE \"abc\".", "01 Y PIC X(3).", FlagB, "PROCEDURE DIVISION.",
            ">>TURN EC-PROGRAM CHECKING ON", stmt, "    DISPLAY Y.", "    STOP RUN.");

        string source = Program("    MOVE UPPER-CASE(X) TO Y.");
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "EC-PROGRAM CHECKING")), warnings[0]);

        // The REPOSITORY paragraph alone is no call.
        Assert.Empty(Warnings(Program("    MOVE X TO Y."), "COBOLNET1620"));
    }

    [Theory]
    [InlineData("    MOVE TB(FUNCTION LENGTH(X)) TO Y.", "")]
    [InlineData("    MOVE TB(LENGTH(X)) TO Y.", "REPOSITORY.|    FUNCTION ALL INTRINSIC.")]
    public void EcProgram_AFunctionInsideASubscript_IsACall(string stmt, string repository)
    {
        // The subscript is a captured token group with no FunctionCall node of its own; the renderer that recognizes the
        // function records its head token.
        string source = Src([
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. INSUBS.",
            .. (repository.Length == 0 ? [] : new[] { "ENVIRONMENT DIVISION.", "CONFIGURATION SECTION." }.Concat(repository.Split('|'))),
            "DATA DIVISION.", "WORKING-STORAGE SECTION.", "01 X PIC X(3) VALUE \"abc\".", "01 T.",
            "   05 TB OCCURS 5 PIC X.", "01 Y PIC X.", FlagB, "PROCEDURE DIVISION.",
            ">>TURN EC-PROGRAM CHECKING ON", stmt, "    DISPLAY Y.", "    STOP RUN."]);
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "EC-PROGRAM CHECKING")), warnings[0]);
    }

    [Fact]
    public void EcProgram_EachMethodIsItsOwnSourceElement()
    {
        // §3.164 + §7.3.14.4 GR4 b 2: GO1 invokes a method, GO2 invokes nothing. The old walk recorded calls only inside
        // a programUnit, so a method's call and invoke were invisible and neither TURN was flagged.
        string source = Src(
            "IDENTIFICATION DIVISION.", "CLASS-ID. FLGMETH.", "FACTORY.", "PROCEDURE DIVISION.",
            "IDENTIFICATION DIVISION.", "METHOD-ID. GO1.", "DATA DIVISION.", "LOCAL-STORAGE SECTION.",
            "01 X PIC X(3) VALUE \"abc\".", FlagB, "PROCEDURE DIVISION.",
            ">>TURN EC-PROGRAM CHECKING ON",
            "    INVOKE SELF \"GO2\".",
            "END METHOD GO1.",
            "IDENTIFICATION DIVISION.", "METHOD-ID. GO2.", "DATA DIVISION.", "LOCAL-STORAGE SECTION.",
            "01 Y PIC X(3) VALUE \"abc\".", "PROCEDURE DIVISION.",
            ">>TURN EC-PROGRAM-NOT-FOUND CHECKING ON",
            "    DISPLAY Y.",
            "END METHOD GO2.", "END FACTORY.", "END CLASS FLGMETH.");
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "EC-PROGRAM CHECKING")), warnings[0]);
    }

    [Fact]
    public void EcProgram_AFunctionCalledInAMethod_FlagsThatMethodsTurn()
    {
        string source = Src(
            "IDENTIFICATION DIVISION.", "CLASS-ID. FLGMETF.", "FACTORY.", "PROCEDURE DIVISION.",
            "IDENTIFICATION DIVISION.", "METHOD-ID. GO1.", "DATA DIVISION.", "LOCAL-STORAGE SECTION.",
            "01 X PIC X(3) VALUE \"abc\".", "01 Y PIC X(3).", FlagB, "PROCEDURE DIVISION.",
            ">>TURN EC-ALL CHECKING ON",
            "    MOVE FUNCTION UPPER-CASE(X) TO Y.", "    DISPLAY Y.",
            "END METHOD GO1.", "END FACTORY.", "END CLASS FLGMETF.");
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "EC-ALL CHECKING")), warnings[0]);
    }

    [Fact]
    public void EcProgram_AnObjectPropertyReference_InvokesTheAccessorMethod()
    {
        // §8.4.3.9.4 GR1: a property reference occurrence IS an invocation of the get (or set) property method, though the
        // text `BAL OF A` is a qualified data reference. The program invokes nothing else (the object arrives as a
        // parameter), so the property reference alone makes its TURN qualify.
        string Program(string stmt) => Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. PROPCALL.", "ENVIRONMENT DIVISION.", "CONFIGURATION SECTION.",
            "REPOSITORY.", "    CLASS CFLGPROP", "    PROPERTY BAL.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
            "01 N PIC 9(5).", "LINKAGE SECTION.", "01 A USAGE OBJECT REFERENCE CFLGPROP.", FlagB,
            "PROCEDURE DIVISION USING A.", ">>TURN EC-PROGRAM CHECKING ON", stmt, "    GOBACK.",
            "END PROGRAM PROPCALL.", "",
            "IDENTIFICATION DIVISION.", "CLASS-ID. CFLGPROP INHERITS FROM BASE.", "ENVIRONMENT DIVISION.",
            "CONFIGURATION SECTION.", "REPOSITORY.", "    CLASS BASE.", "IDENTIFICATION DIVISION.", "OBJECT.",
            "DATA DIVISION.", "WORKING-STORAGE SECTION.", "01 BAL PIC 9(5) VALUE 100 PROPERTY.",
            "PROCEDURE DIVISION.", "END OBJECT.", "END CLASS CFLGPROP.");

        string source = Program("    MOVE BAL OF A TO N.");
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "EC-PROGRAM CHECKING")), warnings[0]);

        Assert.Empty(Warnings(Program("    MOVE 1 TO N."), "COBOLNET1620"));
    }

    // ── FLAG-02 d MOVE-TO-SAME-NAME (§7.3.14.4 GR4 d): flagged when the operands are described by the same DDE and
    //    1. "the operands are of category alphanumeric-edited" — a reference-modified operand is category alphanumeric
    //    (§8.4.3.3.4 GR6 a), or 2. the DDE has a subordinate OCCURS … DEPENDING whose object is subordinate to it. ──

    private static string MoveProgram(string entries, string stmt) => Src([
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGMVR.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
        .. entries.Split('|'), "PROCEDURE DIVISION.", ">>FLAG-02 MOVE-TO-SAME-NAME ON", stmt, "    STOP RUN."]);

    [Fact]
    public void MoveToSameName_ReferenceModifiedAlphanumericEditedOperands_AreCategoryAlphanumeric()
    {
        const string entry = "01 AE PIC X(3)BX(3) VALUE \"ABC DEF\".";
        Assert.Empty(Warnings(MoveProgram(entry, "    MOVE AE(1:3) TO AE(5:3)."), "COBOLNET1620"));
        Assert.Empty(Warnings(MoveProgram(entry, "    MOVE AE TO AE(5:3)."), "COBOLNET1620"));
        Assert.Single(Warnings(MoveProgram(entry, "    MOVE AE TO AE."), "COBOLNET1620"));
    }

    [Fact]
    public void MoveToSameName_SubordinateOdo_IsAPropertyOfTheEntry_NotOfTheReferenceModification()
    {
        const string entries = "01 G.|   05 CNT PIC 9 VALUE 3.|   05 T OCCURS 1 TO 5 DEPENDING ON CNT PIC X.";
        Assert.Single(Warnings(MoveProgram(entries, "    MOVE G(1:2) TO G(3:2)."), "COBOLNET1620"));
    }

    [Fact]
    public void MoveToSameName_InAContainedProgram_ResolvesInThatProgramsScope()
    {
        // The pass selected a unit's name scope through the outermost program's context only, so a contained program's
        // operands were resolved (to nothing) in its container's scope and its MOVE was never flagged.
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. OUTER1.", "PROCEDURE DIVISION.",
            "    CALL \"INNER1\".", "    STOP RUN.",
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. INNER1.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
            "01 AE PIC X(3)BX(3) VALUE \"ABC DEF\".", "PROCEDURE DIVISION.",
            ">>FLAG-02 MOVE-TO-SAME-NAME ON", "    MOVE AE TO AE.", "    EXIT PROGRAM.",
            "END PROGRAM INNER1.", "END PROGRAM OUTER1.");
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "MOVE AE TO AE")), warnings[0]);
    }

    // ── FLAG-14 b COMPILE-TIME-ARITHMETIC-EXPRESSIONS (§7.3.15.4 GR4 b): a compile-time arithmetic expression "may be
    //    specified in the DEFINE and EVALUATE directives, in a constant conditional expression, and in a constant
    //    entry" (§7.3.6.1). ──

    [Fact]
    public void CompileTimeArithmetic_ConstantEntryExpression_IsFlagged_ALiteralIsNot()
    {
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGCONST.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
            ">>FLAG-14 COMPILE-TIME-ARITHMETIC-EXPRESSIONS ON",
            "01 K1 CONSTANT AS 5 * 2.", "01 K2 CONSTANT AS 7.",
            "PROCEDURE DIVISION.", "    DISPLAY K1 K2.", "    STOP RUN.");
        var warnings = Warnings(source, "COBOLNET1621");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "K1 CONSTANT")), warnings[0]);
    }

    [Fact]
    public void CompileTimeArithmetic_ConstantEntryExpression_IsNotFlagged_WhenTheOptionIsOff()
    {
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGCOFF.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
            ">>FLAG-14 COMPILE-TIME-ARITHMETIC-EXPRESSIONS OFF",
            "01 K1 CONSTANT AS 5 * 2.",
            "PROCEDURE DIVISION.", "    DISPLAY K1.", "    STOP RUN.");
        Assert.Empty(Warnings(source, "COBOLNET1621"));
    }

    // ── FLAG-14 i REF-MOD-ZERO-LENGTH (§7.3.15.4 GR4 i): "A reference modification of a data-item shall be flagged" — the
    //    reference modification of a function's RESULT is not a data item's. ──

    [Theory]
    [InlineData("    MOVE W(2:2) TO R.", true)]
    [InlineData("    MOVE FUNCTION UPPER-CASE(W)(2:2) TO R.", false)]
    [InlineData("    MOVE FUNCTION UPPER-CASE(W(1:2)) TO R.", true)]   // the argument W(1:2) is a data item's
    public void RefModZeroLength_FlagsAReferenceModificationOfADataItem_NotOfAFunctionResult(string stmt, bool flagged)
    {
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGREFM.", "DATA DIVISION.", "WORKING-STORAGE SECTION.",
            "01 W PIC X(5) VALUE \"HELLO\".", "01 R PIC X(3).", "PROCEDURE DIVISION.",
            ">>TURN EC-BOUND-REF-MOD CHECKING ON", ">>FLAG-14 REF-MOD-ZERO-LENGTH ON", stmt, "    STOP RUN.");
        var warnings = Warnings(source, "COBOLNET1621");
        if (!flagged) { Assert.Empty(warnings); return; }
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "MOVE ")), warnings[0]);
    }

    // ── FLAG-14 d I-O-DECLARATIVE (§7.3.15.4 GR4 d): "An input-output statement that can be specified with an INVALID
    //    KEY phrase" — REWRITE shall not specify it for a relative file in sequential access mode (§14.9.35.3 SR2) and
    //    DELETE RECORD shall not for a file in sequential access mode (§14.9.10.3 SR2). ──

    private static string InvalidKeyProgram(string selectTail, string stmt) => Src([
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGINV.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
        "FILE-CONTROL.", "    SELECT F ASSIGN TO \"f.dat\"", .. selectTail.Split('|'),
        "DATA DIVISION.", "FILE SECTION.", "FD F.", "01 F-REC.", "   05 F-KEY PIC X(4).",
        "WORKING-STORAGE SECTION.", "01 RK PIC 9(4).", "PROCEDURE DIVISION.",
        "DECLARATIVES.", "D-SEC SECTION.", "    USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.",
        "D-PARA.", "    DISPLAY \"E\".", "END DECLARATIVES.", "MAIN SECTION.", "M.",
        ">>FLAG-14 I-O-DECLARATIVE ON", "    OPEN I-O F.", stmt, "    CLOSE F.", "    STOP RUN."]);

    private const string RelativeSequential =
        "    ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL|    RELATIVE KEY IS RK.";
    private const string RelativeDynamic =
        "    ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC|    RELATIVE KEY IS RK.";
    private const string IndexedSequential =
        "    ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL|    RECORD KEY IS F-KEY.";
    private const string IndexedDynamic =
        "    ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC|    RECORD KEY IS F-KEY.";

    [Theory]
    [InlineData(RelativeSequential, "    REWRITE F-REC.", false)]   // §14.9.35.3 SR2: no INVALID KEY on this file
    [InlineData(RelativeSequential, "    DELETE F.", false)]        // §14.9.10.3 SR2: sequential access mode
    [InlineData(IndexedSequential, "    DELETE F.", false)]        // §14.9.10.3 SR2: sequential access mode
    [InlineData(IndexedSequential, "    REWRITE F-REC.", true)]    // an indexed file may take the phrase in any access mode
    [InlineData(RelativeDynamic, "    REWRITE F-REC.", true)]
    [InlineData(RelativeDynamic, "    DELETE F.", true)]
    [InlineData(IndexedDynamic, "    DELETE F.", true)]
    [InlineData(IndexedSequential, "    WRITE F-REC.", true)]      // §14.9.51.3 SR3: format 2 for every keyed file
    public void IoDeclarative_FlagsOnlyTheStatementsThatCanTakeAnInvalidKeyPhrase(string selectTail, string stmt, bool flagged)
    {
        string source = InvalidKeyProgram(selectTail, stmt);
        var warnings = Warnings(source, "COBOLNET1621");
        if (!flagged) { Assert.Empty(warnings); return; }
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, stmt.Trim())), warnings[0]);
    }

    // ── FLAG-14 e I-O-STATUS-04 / f I-O-STATUS-07 (§7.3.15.4 GR4 e/f): "A reference to a data item specified in a FILE
    //    STATUS clause that tests for '04'" / "that specifies '07'" — every place a condition can test it: the relation,
    //    the abbreviated combined relation (§8.8.4.12.4 GR1: the last preceding stated subject is inserted), a level-88
    //    condition-name, and an EVALUATE selection subject/object pair (§14.9.13.3 SR8). ──

    private static string StatusProgram(params string[] statements) => Src([
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGSTAT.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
        "FILE-CONTROL.", "    SELECT F ASSIGN TO \"f.dat\" ORGANIZATION IS INDEXED",
        "        ACCESS MODE IS DYNAMIC RECORD KEY IS F-KEY",
        "        FILE STATUS IS FS.",
        "DATA DIVISION.", "FILE SECTION.", "FD F.", "01 F-REC.", "   05 F-KEY PIC X(4).",
        "WORKING-STORAGE SECTION.", "01 FS PIC XX.", "   88 FS-DUP VALUE \"07\".", "   88 FS-EOF VALUE \"10\".",
        "   88 FS-HEX04 VALUE X\"3034\".", "   88 FS-QUOTE04 VALUE \"0'4\".", "   88 FS-HEX07 VALUE X\"3037\".",
        "01 WS PIC XX.", "PROCEDURE DIVISION.", "MAIN.",
        ">>FLAG-14 I-O-STATUS-04 ON", ">>FLAG-14 I-O-STATUS-07 ON",
        .. statements, "    STOP RUN."]);

    [Theory]
    [InlineData("    IF FS = \"00\" OR \"04\"|        CONTINUE|    END-IF.", "I-O-STATUS-04", 1)]
    [InlineData("    IF FS = \"00\" OR = \"07\"|        CONTINUE|    END-IF.", "I-O-STATUS-07", 1)]
    [InlineData("    IF FS = \"05\" AND NOT = \"04\"|        CONTINUE|    END-IF.", "I-O-STATUS-04", 1)]
    [InlineData("    IF FS NOT = \"07\" AND FS NOT = \"10\"|        CONTINUE|    END-IF.", "I-O-STATUS-07", 1)]
    [InlineData("    EVALUATE FS|        WHEN \"07\" CONTINUE|    END-EVALUATE.", "I-O-STATUS-07", 1)]
    [InlineData("    EVALUATE FS|        WHEN \"00\" CONTINUE|        WHEN \"04\" CONTINUE|    END-EVALUATE.", "I-O-STATUS-04", 1)]
    [InlineData("    EVALUATE FS|        WHEN = \"04\" CONTINUE|    END-EVALUATE.", "I-O-STATUS-04", 1)]
    [InlineData("    EVALUATE TRUE|        WHEN FS-DUP CONTINUE|    END-EVALUATE.", "I-O-STATUS-07", 1)]
    [InlineData("    EVALUATE TRUE ALSO TRUE|        WHEN WS = \"00\" ALSO FS = \"04\" CONTINUE|    END-EVALUATE.", "I-O-STATUS-04", 1)]
    [InlineData("    IF WS = \"00\" OR \"04\"|        CONTINUE|    END-IF.", "I-O-STATUS-04", 0)]    // not a FILE STATUS item
    [InlineData("    EVALUATE WS|        WHEN \"04\" CONTINUE|    END-EVALUATE.", "I-O-STATUS-04", 0)]
    [InlineData("    EVALUATE FS|        WHEN \"05\" CONTINUE|    END-EVALUATE.", "I-O-STATUS-04", 0)]  // another status
    [InlineData("    EVALUATE FS|        WHEN \"04\" CONTINUE|    END-EVALUATE.", "I-O-STATUS-07", 0)]  // per-option gating
    [InlineData("    IF FS-EOF|        CONTINUE|    END-IF.", "I-O-STATUS-04", 0)]                       // an 88 for '10'
    // kb/Work PB3234: the literal's VALUE is decoded once (CobolLiteral.Decode), not its text with every quote deleted.
    [InlineData("    IF FS = X\"3034\"|        CONTINUE|    END-IF.", "I-O-STATUS-04", 1)]       // §8.3.3.2.4 GR4: X"3034" is '04'
    [InlineData("    IF FS = X\"3034\"|        CONTINUE|    END-IF.", "I-O-STATUS-07", 0)]
    [InlineData("    IF FS = N\"04\"|        CONTINUE|    END-IF.", "I-O-STATUS-04", 1)]         // a national '04'
    [InlineData("    IF FS = \"0'4\"|        CONTINUE|    END-IF.", "I-O-STATUS-04", 0)]         // §8.3.3.2.4 GR1: three characters 0'4
    [InlineData("    IF FS = '0''4'|        CONTINUE|    END-IF.", "I-O-STATUS-04", 0)]           // §8.3.3.2.3 SR3: a doubled delimiter is ONE character
    [InlineData("    IF FS-HEX04|        CONTINUE|    END-IF.", "I-O-STATUS-04", 1)]              // an 88 VALUE X"3034"
    [InlineData("    IF FS-HEX07|        CONTINUE|    END-IF.", "I-O-STATUS-07", 1)]
    [InlineData("    IF FS-HEX07|        CONTINUE|    END-IF.", "I-O-STATUS-04", 0)]
    [InlineData("    IF FS-QUOTE04|        CONTINUE|    END-IF.", "I-O-STATUS-04", 0)]             // an 88 VALUE "0'4"
    public void IoStatus_FlagsEveryConditionThatTestsTheFileStatusItem(string statements, string option, int expected)
    {
        var warnings = Warnings(StatusProgram(statements.Split('|')), "COBOLNET1621")
            .Where(w => w.Contains(option, StringComparison.Ordinal)).ToList();
        Assert.Equal(expected, warnings.Count);
    }

    [Fact]
    public void IoStatus_AbbreviatedObjectOnALaterLine_IsFlaggedAtItsOwnLine()
    {
        string source = StatusProgram(
            "    IF FS = \"00\"", "        OR \"04\"", "        CONTINUE", "    END-IF.");
        var warnings = Warnings(source, "COBOLNET1621");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "OR \"04\"")), warnings[0]);
    }

    // ── FLAG-14 g NUM-ED-ZERO-FIGCONST / j VALUE-EDITING / l VALUE-ZERO (§7.3.15.4 GR4 g/j/l) concern a "numeric-edited
    //    data item". "If the subject of the entry is described by its picture character-string as category numeric,
    //    the BLANK WHEN ZERO clause defines the item as numeric-edited" (§13.18.8.4 GR2). ──

    private static string ValueProgram(string specialNames, params string[] entries) => Src([
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGNED.",
        .. (specialNames.Length == 0 ? [] : new[] { "ENVIRONMENT DIVISION.", "CONFIGURATION SECTION.", "SPECIAL-NAMES.", specialNames }),
        "DATA DIVISION.", "WORKING-STORAGE SECTION.",
        ">>FLAG-14 NUM-ED-ZERO-FIGCONST ON", ">>FLAG-14 VALUE-ZERO ON", ">>FLAG-14 VALUE-EDITING ON",
        .. entries, "PROCEDURE DIVISION.", "    STOP RUN."]);

    [Fact]
    public void NumericEdited_ANumericPictureWithBlankWhenZero_IsNumericEdited()
    {
        string source = ValueProgram("",
            "01 X PIC 999 BLANK WHEN ZERO VALUE ZERO.",
            "01 Y PIC 999 BLANK WHEN ZERO VALUE 12.");
        var warnings = Warnings(source, "COBOLNET1621");
        Assert.Equal(3, warnings.Count);
        Assert.Equal(2, warnings.Count(w => AtLine(w, LineOf(source, "01 X PIC"))));   // g and l: the figurative ZERO
        Assert.Single(warnings, w => AtLine(w, LineOf(source, "01 Y PIC")));            // j: a literal with no editing symbol
    }

    [Fact]
    public void NumericEdited_ANumericPictureWithoutBlankWhenZero_IsNotNumericEdited()
    {
        string source = ValueProgram("",
            "01 X PIC 999 VALUE ZERO.",
            "01 Y PIC 999 VALUE 12.",
            "01 Z PIC 999 BLANK WHEN ZERO.");
        Assert.Empty(Warnings(source, "COBOLNET1621"));
    }

    [Fact]
    public void NumericEdited_AnEditingSymbolThatIsTheUnitsCurrencySign_IsNumericEdited()
    {
        string source = ValueProgram("    CURRENCY SIGN IS \"#\".",
            "01 C PIC #ZZ9.99 VALUE ZERO.");
        Assert.Equal(2, Warnings(source, "COBOLNET1621").Count);   // g and l
    }

    // ── FLAG-14 m WRITE-END-OF-PAGE (§7.3.15.4 GR4 m): "A WRITE statement that allows an END-OF-PAGE phrase when the
    //    END-OF-PAGE phrase is not specified shall be flagged." §14.9.51.3 SR19 names the END-OF-PAGE phrase and the NOT
    //    END-OF-PAGE phrase separately, so a WRITE with only the NOT phrase does not specify the END-OF-PAGE phrase. ──

    private static string WriteProgram(string stmt) => Src(
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGEOP.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
        "FILE-CONTROL.", "    SELECT OUTF ASSIGN \"of.txt\".", "DATA DIVISION.", "FILE SECTION.",
        "FD OUTF LINAGE IS 60 LINES.", "01 OUT-REC PIC X(80).", "PROCEDURE DIVISION.", "MAIN.",
        "    OPEN OUTPUT OUTF.", ">>FLAG-14 WRITE-END-OF-PAGE ON", stmt, "    CLOSE OUTF.", "    STOP RUN.");

    [Theory]
    [InlineData("    WRITE OUT-REC NOT AT END-OF-PAGE CONTINUE END-WRITE.", true)]
    [InlineData("    WRITE OUT-REC.", true)]
    [InlineData("    WRITE OUT-REC AT END-OF-PAGE CONTINUE END-WRITE.", false)]
    [InlineData("    WRITE OUT-REC AT END-OF-PAGE CONTINUE NOT AT END-OF-PAGE CONTINUE END-WRITE.", false)]
    [InlineData("    WRITE OUT-REC NOT AT END-OF-PAGE CONTINUE AT END-OF-PAGE CONTINUE END-WRITE.", false)]
    public void WriteEndOfPage_IsFlaggedUnlessThePositivePhraseIsSpecified(string stmt, bool flagged)
    {
        string source = WriteProgram(stmt);
        var warnings = Warnings(source, "COBOLNET1621");
        if (!flagged) { Assert.Empty(warnings); return; }
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "WRITE OUT-REC")), warnings[0]);
    }

    // ── FLAG-02 f TERMINATE-WITH-VARYING (§7.3.14.4 GR4 f): "if the report being terminated contains a VARYING clause" —
    //    any VARYING clause of the report description, on a group entry included (§13.18.64.3 SR1). ──

    [Fact]
    public void TerminateWithVarying_AVaryingClauseOnAGroupEntry_FlagsTheTerminate()
    {
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGVARY.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
            "FILE-CONTROL.", "    SELECT RPT ASSIGN \"r.rpt\".", "DATA DIVISION.", "FILE SECTION.",
            "FD RPT REPORT IS R.", "REPORT SECTION.", "RD R.",
            "01 DL TYPE DETAIL LINE PLUS 1.",
            "   05 G1 OCCURS 3 VARYING K FROM 1 BY 1.",
            "      10 COLUMN + 2 PIC X(3) VALUE \"AB\".",
            "PROCEDURE DIVISION.", "    OPEN OUTPUT RPT.", "    INITIATE R.", "    GENERATE DL.",
            ">>FLAG-02 TERMINATE-WITH-VARYING ON", "    TERMINATE R.", "    CLOSE RPT.", "    STOP RUN.");
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "TERMINATE R")), warnings[0]);
    }

    // ── kb/Work PB3288: a name is RESOLVED in the current unit's scope; the role (FILE STATUS item, LINAGE file, VARYING
    //    report) is read off the resolved object. A same-named item of another program or another group is not it. ──

    private static string[] StatusUnit(string id, bool withFileStatus, bool directive) => [
        "IDENTIFICATION DIVISION.", $"PROGRAM-ID. {id}.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
        "FILE-CONTROL.",
        withFileStatus ? "    SELECT F ASSIGN TO \"f.dat\" FILE STATUS IS WS-STAT." : "    SELECT F ASSIGN TO \"f.dat\".",
        "DATA DIVISION.", "FILE SECTION.", "FD F.", "01 F-REC PIC X(4).",
        "WORKING-STORAGE SECTION.", "01 WS-STAT PIC XX.", "   88 STAT-04 VALUE \"04\".",
        "PROCEDURE DIVISION.", "MAIN.",
        .. (directive ? (string[])[">>FLAG-14 I-O-STATUS-04 ON"] : []),
        "    IF WS-STAT = \"04\"", "        CONTINUE", "    END-IF.",
        "    IF STAT-04", "        CONTINUE", "    END-IF.",
        "    STOP RUN.", $"END PROGRAM {id}."];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IoStatus_ANameSharedWithAnotherProgramsFileStatusItem_IsNotAReference(bool statusProgramFirst)
    {
        // Program PS names WS-STAT in a FILE STATUS clause; program PN declares its own WS-STAT and STAT-04 and no
        // FILE STATUS clause. The FLAG-14 e directive (in the first program) stays on for the text that follows it,
        // across the group, so only the program whose WS-STAT IS the FILE STATUS item draws its two warnings.
        string[] first = StatusUnit(statusProgramFirst ? "PS" : "PN", withFileStatus: statusProgramFirst, directive: true);
        string[] second = StatusUnit(statusProgramFirst ? "PN" : "PS", withFileStatus: !statusProgramFirst, directive: false);
        string source = Src(first.Concat(second).ToArray());
        var warnings = Warnings(source, "COBOLNET1621");
        int boundary = LineOf(source, statusProgramFirst ? "PROGRAM-ID. PN." : "PROGRAM-ID. PS.");
        Assert.Equal(2, warnings.Count);
        Assert.All(warnings, w => Assert.True(statusProgramFirst ? WarningLine(w) < boundary : WarningLine(w) > boundary, w));
    }

    [Fact]
    public void IoStatus_ANameSharedByAnotherGroupOfTheSameProgram_IsNotAReference()
    {
        // FS OF G1 is the FILE STATUS item; FS OF G2 and the level-88 IS-04 OF FS OF G2 are other items that merely
        // share the spelling (§8.4.2.2: qualification selects the item), so only G1's two references are flagged.
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGGRP.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
            "FILE-CONTROL.", "    SELECT F ASSIGN TO \"f.dat\" FILE STATUS IS FS OF G1.",
            "DATA DIVISION.", "FILE SECTION.", "FD F.", "01 F-REC PIC X(4).",
            "WORKING-STORAGE SECTION.",
            "01 G1.", "   05 FS PIC XX.", "      88 IS-04 VALUE \"04\".",
            "01 G2.", "   05 FS PIC XX.", "      88 IS-04 VALUE \"04\".",
            "PROCEDURE DIVISION.", "MAIN.", ">>FLAG-14 I-O-STATUS-04 ON",
            "    IF FS OF G1 = \"04\"", "        CONTINUE", "    END-IF.",
            "    IF FS OF G2 = \"04\"", "        CONTINUE", "    END-IF.",
            "    IF IS-04 OF FS OF G1", "        CONTINUE", "    END-IF.",
            "    IF IS-04 OF FS OF G2", "        CONTINUE", "    END-IF.",
            "    STOP RUN.");
        var warnings = Warnings(source, "COBOLNET1621");
        Assert.Equal(2, warnings.Count);
        Assert.Contains(warnings, w => AtLine(w, LineOf(source, "IF FS OF G1")));
        Assert.Contains(warnings, w => AtLine(w, LineOf(source, "IF IS-04 OF FS OF G1")));
    }

    [Fact]
    public void IoStatus_AContainedProgramReferencingAContainersGlobalFileStatusItem_IsFlagged()
    {
        // The FILE STATUS item and the LINAGE file are the container's, GLOBAL (§13.18.27.4 GR1), and the contained
        // program's references to them resolve there: both the data item and its level-88, and both WRITE spellings.
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGOUT.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
            "FILE-CONTROL.", "    SELECT LF ASSIGN TO \"lf.txt\" FILE STATUS IS WS-STAT.",
            "DATA DIVISION.", "FILE SECTION.", "FD LF GLOBAL LINAGE IS 10 LINES.", "01 LREC PIC X(4).",
            "WORKING-STORAGE SECTION.", "01 WS-STAT PIC XX GLOBAL.", "   88 STAT-04 VALUE \"04\".",
            "PROCEDURE DIVISION.", "MAIN.", ">>FLAG-14 I-O-STATUS-04 ON", ">>FLAG-14 WRITE-END-OF-PAGE ON",
            "    CALL \"FLGINN\".", "    STOP RUN.",
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGINN.", "DATA DIVISION.", "PROCEDURE DIVISION.", "MAIN.",
            "    IF WS-STAT = \"04\"", "        CONTINUE", "    END-IF.",
            "    IF STAT-04", "        CONTINUE", "    END-IF.",
            "    WRITE LREC.", "    WRITE LREC IN LF.", "    GOBACK.",
            "END PROGRAM FLGINN.", "END PROGRAM FLGOUT.");
        var warnings = Warnings(source, "COBOLNET1621");
        Assert.Equal(4, warnings.Count);
        Assert.Contains(warnings, w => AtLine(w, LineOf(source, "IF WS-STAT")));
        Assert.Contains(warnings, w => AtLine(w, LineOf(source, "IF STAT-04")));
        Assert.Contains(warnings, w => AtLine(w, LineOf(source, "WRITE LREC.")));
        Assert.Contains(warnings, w => AtLine(w, LineOf(source, "WRITE LREC IN LF.")));
    }

    private static string[] LinageUnit(string id, string fdClause) => [
        "IDENTIFICATION DIVISION.", $"PROGRAM-ID. {id}.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
        "FILE-CONTROL.", "    SELECT OUTF ASSIGN \"of.txt\".", "DATA DIVISION.", "FILE SECTION.",
        $"FD OUTF{fdClause}.", "01 OUT-REC PIC X(80).", "PROCEDURE DIVISION.", "MAIN.",
        "    OPEN OUTPUT OUTF.", "    WRITE OUT-REC.", "    CLOSE OUTF.", "    STOP RUN.", $"END PROGRAM {id}."];

    [Fact]
    public void WriteEndOfPage_ARecordNameSharedWithAnotherProgramsLinageFile_IsNotFlagged()
    {
        // OUT-REC belongs to a LINAGE file in PL and to a file with no LINAGE in PN: only PL's WRITE allows END-OF-PAGE.
        string source = "       >>FLAG-14 WRITE-END-OF-PAGE ON\n"
            + Src(LinageUnit("PL", " LINAGE IS 60 LINES").Concat(LinageUnit("PN", "")).ToArray());
        var warnings = Warnings(source, "COBOLNET1621");
        Assert.Single(warnings);
        Assert.True(WarningLine(warnings[0]) < LineOf(source, "PROGRAM-ID. PN."), warnings[0]);
    }

    [Fact]
    public void WriteEndOfPage_AQualifiedRecordNameResolvesToItsOwnFile()
    {
        // Two files, one record spelling: `WRITE REC IN LF` writes the LINAGE file's record (flagged), `WRITE REC IN PLAINF`
        // the other's (§8.4.2.2.2 Format 1: the FD is the outermost qualifier). Matching the written text `RECINLF`
        // never found either.
        string source = Src(
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. FLGQREC.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
            "FILE-CONTROL.", "    SELECT LF ASSIGN \"lf.txt\".", "    SELECT PLAINF ASSIGN \"pf.txt\".",
            "DATA DIVISION.", "FILE SECTION.", "FD LF LINAGE IS 60 LINES.", "01 REC PIC X(80).",
            "FD PLAINF.", "01 REC PIC X(80).", "PROCEDURE DIVISION.", "MAIN.", ">>FLAG-14 WRITE-END-OF-PAGE ON",
            "    WRITE REC IN LF.", "    WRITE REC IN PLAINF.", "    STOP RUN.");
        var warnings = Warnings(source, "COBOLNET1621");
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "WRITE REC IN LF")), warnings[0]);
    }

    private static string[] ReportUnit(string id, bool varying) => [
        "IDENTIFICATION DIVISION.", $"PROGRAM-ID. {id}.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.",
        "FILE-CONTROL.", "    SELECT RPT ASSIGN \"r.rpt\".", "DATA DIVISION.", "FILE SECTION.",
        "FD RPT REPORT IS R.", "REPORT SECTION.", "RD R.", "01 DL TYPE DETAIL LINE PLUS 1.",
        .. (varying
            ? (string[])["   05 G1 OCCURS 3 VARYING K FROM 1 BY 1.", "      10 COLUMN + 2 PIC X(3) VALUE \"AB\"."]
            : ["   05 COLUMN + 2 PIC X(3) VALUE \"AB\"."]),
        "PROCEDURE DIVISION.", "MAIN.", "    OPEN OUTPUT RPT.", "    INITIATE R.", "    GENERATE DL.",
        "    TERMINATE R.", "    CLOSE RPT.", "    STOP RUN.", $"END PROGRAM {id}."];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TerminateWithVarying_AReportNameSharedWithAnotherProgramsVaryingReport_IsNotFlagged(bool varyingFirst)
    {
        // Report R has a VARYING clause in PV and none in PN: only the TERMINATE of PV's R is flagged, whichever
        // program comes first (§7.3.14.4 GR4 f: "the report being terminated contains a VARYING clause").
        string[] first = ReportUnit(varyingFirst ? "PV" : "PN", varying: varyingFirst);
        string[] second = ReportUnit(varyingFirst ? "PN" : "PV", varying: !varyingFirst);
        string source = "       >>FLAG-02 TERMINATE-WITH-VARYING ON\n" + Src(first.Concat(second).ToArray());
        var warnings = Warnings(source, "COBOLNET1620");
        Assert.Single(warnings);
        int boundary = LineOf(source, varyingFirst ? "PROGRAM-ID. PN." : "PROGRAM-ID. PV.");
        Assert.True(varyingFirst ? WarningLine(warnings[0]) < boundary : WarningLine(warnings[0]) > boundary, warnings[0]);
    }

    [Fact]
    public void ValueEditing_AHexadecimalLiteralIsScannedByItsValue()
    {
        // §7.3.15.4 GR4 j: a VALUE literal "that does not contain any editing symbols" is flagged. X"3132" is the
        // value "12" (flagged); X"312E" is "1." and carries an editing symbol (kb/Work PB3234: a hexadecimal literal
        // was scanned as the text X312E, never as its value).
        string source = ValueProgram("",
            "01 A PIC 99 BLANK WHEN ZERO VALUE X\"3132\".",
            "01 B PIC 99 BLANK WHEN ZERO VALUE X\"312E\".");
        var warnings = Warnings(source, "COBOLNET1621").Where(w => w.Contains("VALUE-EDITING", StringComparison.Ordinal)).ToList();
        Assert.Single(warnings);
        Assert.True(AtLine(warnings[0], LineOf(source, "01 A PIC")), warnings[0]);
    }
}
