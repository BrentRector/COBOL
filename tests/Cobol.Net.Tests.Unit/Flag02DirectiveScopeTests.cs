// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The SCOPE of a <c>&gt;&gt;FLAG-02</c> option, end to end through the compiler — ISO/IEC 1989:2023 §7.3.14.4
/// General rules 2, 3 and 5 (each validated with <c>scripts/spec/cite.py --check 7.3.14.4</c>):
/// <list type="bullet">
/// <item>GR2 — "If ON is explicitly or implicitly specified for an option, the warning mechanism is enabled for that
/// option for all text that follows until the end of the compilation group is reached, a FLAG-02 directive is
/// encountered that turns off all flagging options, or a FLAG-02 directive is encountered that turns off that
/// option."</item>
/// <item>GR3 — "If OFF is specified, flagging for the selected option or options is disabled."</item>
/// <item>GR5 — "If the FLAG-02 directive is not specified, the default for all options is off."</item>
/// </list>
/// A flag is a WARNING, which the conformance corpus cannot observe (a positive golden only asserts a clean compile
/// and its stdout), so these rules are pinned here. Every program has at most ONE site per option, so no assertion
/// depends on how two warnings of the same option are told apart. The sites are the two purely syntactic FLAG-02
/// detectors: GR4 c) I-O-STATUS-07 (<c>CLOSE … WITH NO REWIND</c> on a sequential file) and GR4 d)
/// MOVE-TO-SAME-NAME (<c>MOVE AE TO AE</c>, AE alphanumeric-edited). GR5 ("all options") additionally covers the
/// other three options, b) EC-PROGRAM-EXCEPTIONS, e) RANGE-EXCEPTION-FOR-INDEX and f) TERMINATE-WITH-VARYING, in a
/// second program (<c>StateCoupledProgram</c>). FLAG-02 exists at 2014 (introduced, §7.3.14.1)
/// and 2023 (obsolete element), so every case runs at both.
/// <para>The GR3 cases are built so they can FAIL: each enables the option BEFORE the OFF line, and its twin in the
/// GR2 group is the same source without the OFF line and IS flagged. A GR3 test with nothing enabled first passes
/// under GR5's default even if OFF were a no-op (the refutation that left rows GR-7.3.14.4-3 and -5 untested).</para>
/// </summary>
public sealed class Flag02DirectiveScopeTests
{
    private const string IoStatus07 = "flagged by >>FLAG-02 I-O-STATUS-07";
    private const string MoveToSameName = "flagged by >>FLAG-02 MOVE-TO-SAME-NAME";
    private const string EcProgramExceptions = "flagged by >>FLAG-02 EC-PROGRAM-EXCEPTIONS";
    private const string RangeExceptionForIndex = "flagged by >>FLAG-02 RANGE-EXCEPTION-FOR-INDEX";
    private const string TerminateWithVarying = "flagged by >>FLAG-02 TERMINATE-WITH-VARYING";

    /// <summary>One program: CONTINUE, OPEN, the CLOSE site, the MOVE site. <paramref name="pre"/> sits between the
    /// CONTINUE and the OPEN, <paramref name="mid"/> between the OPEN and the CLOSE, <paramref name="post"/> between
    /// the CLOSE and the MOVE — every directive is between two statements (§7.3.14.3 SR1: "only between statements in
    /// the procedure division", cite.py OK). The leading CONTINUE flags nothing.</summary>
    private static string Program(string id, string pre, string mid, string post) =>
        "       IDENTIFICATION DIVISION.\n" +
        "       PROGRAM-ID. " + id + ".\n" +
        "       ENVIRONMENT DIVISION.\n" +
        "       INPUT-OUTPUT SECTION.\n" +
        "       FILE-CONTROL.\n" +
        "           SELECT F ASSIGN TO \"f.dat\".\n" +
        "       DATA DIVISION.\n" +
        "       FILE SECTION.\n" +
        "       FD F.\n" +
        "       01 F-REC PIC X(4).\n" +
        "       WORKING-STORAGE SECTION.\n" +
        "       01 AE PIC X(3)BX(3).\n" +
        "       PROCEDURE DIVISION.\n" +
        "       MAIN.\n" +
        "           CONTINUE.\n" +
        Lines(pre) +
        "           OPEN INPUT F.\n" +
        Lines(mid) +
        "           CLOSE F WITH NO REWIND.\n" +
        Lines(post) +
        "           MOVE AE TO AE.\n" +
        "           STOP RUN.\n" +
        "       END PROGRAM " + id + ".\n";

    /// <summary>Each '|'-separated directive text becomes one fixed-form directive line starting in column 8.</summary>
    private static string Lines(string directives) =>
        string.Concat(directives.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => "       >>FLAG-02 " + d.Trim() + "\n"));

    private static IReadOnlyList<string> CompileWarnings(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Flag02Scope_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "flag02.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "flag02.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            return r.Warnings;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    private static bool Has(IReadOnlyList<string> warnings, string option) =>
        warnings.Any(w => w.Contains("warning COBOLNET1620", StringComparison.Ordinal)
            && w.Contains(option, StringComparison.Ordinal));

    // ── GR5: no FLAG-02 directive anywhere → every option is off ─────────────────────────────────────────────

    /// <summary>GR5, with its control: the SAME program flags both sites under <c>&gt;&gt;FLAG-02 ALL</c> (GR4 a),
    /// so the silence without a directive is the default, not an undetectable construct.</summary>
    [Theory]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Gr5_NoDirective_NoOptionIsOn(int edition)
    {
        var none = CompileWarnings(Program("F02S5A", "", "", ""), edition);
        Assert.DoesNotContain(none, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));

        var control = CompileWarnings(Program("F02S5B", "ALL", "", ""), edition);
        Assert.True(Has(control, IoStatus07), string.Join("\n", control));
        Assert.True(Has(control, MoveToSameName), string.Join("\n", control));

        // GR5 says ALL options: the second pair covers the other three, GR4 b), e) and f). b) and e) are the
        // state-coupled options — their conditions are keyed to >>TURN state, so an implementation that flagged them
        // whenever the TURN is present would pass the first pair. The >>TURN lines are present in BOTH programs; only
        // the >>FLAG-02 ALL line differs.
        var noneBef = CompileWarnings(StateCoupledProgram("F02S5C", flagAll: false), edition);
        Assert.DoesNotContain(noneBef, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));

        var controlBef = CompileWarnings(StateCoupledProgram("F02S5D", flagAll: true), edition);
        Assert.True(Has(controlBef, EcProgramExceptions), string.Join("\n", controlBef));
        Assert.True(Has(controlBef, RangeExceptionForIndex), string.Join("\n", controlBef));
        Assert.True(Has(controlBef, TerminateWithVarying), string.Join("\n", controlBef));
    }

    /// <summary>One program with a site for each of GR4 b), e) and f):
    /// b) EC-PROGRAM-EXCEPTIONS — "A TURN directive for EC-ALL, EC-PROGRAM, EC-PROGRAM-ARG-OMITTED. or
    /// EC-PROGRAM-NOT-FOUND shall be flagged if 1. the source element calls any function" (cite.py OK): a
    /// <c>&gt;&gt;TURN EC-PROGRAM-NOT-FOUND CHECKING ON</c> in a source element that references FUNCTION MAX;
    /// e) RANGE-EXCEPTION-FOR-INDEX — an index-assignment SET whose receiver is an index-name "shall be flagged when
    /// checking for EC-RANGE-INDEX is enabled" (cite.py OK): <c>&gt;&gt;TURN EC-RANGE-INDEX CHECKING ON</c> then
    /// <c>SET IDX TO 1</c>; f) TERMINATE-WITH-VARYING — "A TERMINATE statement shall be flagged if the report being
    /// terminated contains a VARYING clause" (cite.py OK): the report R carries <c>VARYING RV FROM WS-SEQ BY 1</c>.
    /// With <paramref name="flagAll"/>, <c>&gt;&gt;FLAG-02 ALL</c> (GR4 a) precedes both TURN lines, so every site
    /// follows it (GR2). Every directive sits between two statements (§7.3.14.3 SR1).</summary>
    private static string StateCoupledProgram(string id, bool flagAll) =>
        "       IDENTIFICATION DIVISION.\n" +
        "       PROGRAM-ID. " + id + ".\n" +
        "       ENVIRONMENT DIVISION.\n" +
        "       INPUT-OUTPUT SECTION.\n" +
        "       FILE-CONTROL.\n" +
        "           SELECT RPT ASSIGN \"r.rpt\".\n" +
        "       DATA DIVISION.\n" +
        "       FILE SECTION.\n" +
        "       FD RPT REPORT IS R.\n" +
        "       WORKING-STORAGE SECTION.\n" +
        "       01 WS-SEQ PIC 9 VALUE 0.\n" +
        "       01 N PIC 9(4) VALUE 5.\n" +
        "       01 W-R PIC 9(4).\n" +
        "       01 T.\n" +
        "          05 E OCCURS 5 TIMES INDEXED BY IDX PIC X.\n" +
        "       REPORT SECTION.\n" +
        "       RD R.\n" +
        "       01 DET TYPE DE.\n" +
        "          02 LINE PLUS 1.\n" +
        "             03 COLUMNS ARE 1 5 PIC Z9 SOURCE IS RV\n" +
        "                VARYING RV FROM WS-SEQ BY 1.\n" +
        "       PROCEDURE DIVISION.\n" +
        "       MAIN.\n" +
        "           CONTINUE.\n" +
        (flagAll ? "       >>FLAG-02 ALL\n" : "") +
        "       >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON\n" +
        "       >>TURN EC-RANGE-INDEX CHECKING ON\n" +
        "           COMPUTE W-R = FUNCTION MAX(N 3).\n" +
        "           SET IDX TO 1.\n" +
        "           OPEN OUTPUT RPT.\n" +
        "           INITIATE R.\n" +
        "           GENERATE DET.\n" +
        "           TERMINATE R.\n" +
        "           CLOSE RPT.\n" +
        "           STOP RUN.\n" +
        "       END PROGRAM " + id + ".\n";

    // ── GR2: ON (explicit or implied) enables that option for ALL text that follows ──────────────────────────

    [Theory]
    [InlineData(2014, "I-O-STATUS-07 ON")]   // ON explicit
    [InlineData(2023, "I-O-STATUS-07 ON")]
    [InlineData(2014, "I-O-STATUS-07")]      // ON implied (§7.3.14.2: ON is not underlined)
    [InlineData(2023, "I-O-STATUS-07")]
    public void Gr2_On_EnablesThatOptionForTheTextThatFollows(int edition, string directive)
    {
        var w = CompileWarnings(Program("F02S2A", "", directive, ""), edition);
        Assert.True(Has(w, IoStatus07), string.Join("\n", w));
        Assert.False(Has(w, MoveToSameName));   // "for that option" — the other option stays at its GR5 default
    }

    /// <summary>GR2 "for all text that FOLLOWS": a directive after the site does not reach back to it.</summary>
    [Theory]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Gr2_On_DoesNotReachTextBeforeTheDirective(int edition)
    {
        var w = CompileWarnings(Program("F02S2B", "", "", "I-O-STATUS-07 ON"), edition);
        Assert.False(Has(w, IoStatus07), string.Join("\n", w));
    }

    /// <summary>GR2 "until the end of the compilation group": ON in the first program of a two-program group still
    /// flags a site in the second program.</summary>
    [Theory]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Gr2_On_LastsToTheEndOfTheCompilationGroup(int edition)
    {
        string first =
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. F02S2C1.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       P1.\n" +
            "           CONTINUE.\n" +
            "       >>FLAG-02 I-O-STATUS-07 ON\n" +
            "           STOP RUN.\n" +
            "       END PROGRAM F02S2C1.\n";
        var w = CompileWarnings(first + Program("F02S2C2", "", "", ""), edition);
        Assert.True(Has(w, IoStatus07), string.Join("\n", w));
    }

    /// <summary>GR2's third terminator is an OFF for "that option": turning a DIFFERENT option off does not end it.</summary>
    [Theory]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Gr2_On_IsNotEndedByTurningADifferentOptionOff(int edition)
    {
        var w = CompileWarnings(Program("F02S2D", "I-O-STATUS-07 ON", "MOVE-TO-SAME-NAME OFF", ""), edition);
        Assert.True(Has(w, IoStatus07), string.Join("\n", w));
    }

    /// <summary>The GR3 cases' twin: enabled before the OPEN with nothing turning it off → flagged. Each GR3 case
    /// below is this source plus one OFF line.</summary>
    [Theory]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Gr2_On_BeforeTheOpen_StillFlagsTheClose(int edition)
    {
        var w = CompileWarnings(Program("F02S2E", "ALL ON", "", ""), edition);
        Assert.True(Has(w, IoStatus07), string.Join("\n", w));
        Assert.True(Has(w, MoveToSameName), string.Join("\n", w));
    }

    // ── GR3: OFF disables the selected option(s) that an earlier ON enabled ─────────────────────────────────

    [Theory]
    [InlineData(2014, "I-O-STATUS-07 OFF")]                     // that option, by name
    [InlineData(2023, "I-O-STATUS-07 OFF")]
    [InlineData(2014, "ALL OFF")]                               // every option (GR2's second terminator)
    [InlineData(2023, "ALL OFF")]
    [InlineData(2014, "MOVE-TO-SAME-NAME I-O-STATUS-07 OFF")]   // a list of options (§7.3.14.2 choice indicators)
    [InlineData(2023, "MOVE-TO-SAME-NAME I-O-STATUS-07 OFF")]
    public void Gr3_Off_DisablesAPreviouslyEnabledOption(int edition, string off)
    {
        // Positive control IN this method: the same source without the OFF line IS flagged, so the pre-OPEN ALL ON
        // took effect and the silence below is GR3, not the GR5 default.
        var twin = CompileWarnings(Program("F02S3Z", "ALL ON", "", ""), edition);
        Assert.True(Has(twin, IoStatus07), string.Join("\n", twin));

        var w = CompileWarnings(Program("F02S3A", "ALL ON", off, ""), edition);
        Assert.False(Has(w, IoStatus07), string.Join("\n", w));
    }

    /// <summary>GR3 disables only the SELECTED options: after <c>I-O-STATUS-07 OFF</c> the MOVE-TO-SAME-NAME option
    /// that ALL enabled is still on; after the list form both are off.</summary>
    [Theory]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Gr3_Off_DisablesOnlyTheSelectedOptions(int edition)
    {
        var one = CompileWarnings(Program("F02S3B", "ALL ON", "I-O-STATUS-07 OFF", ""), edition);
        Assert.False(Has(one, IoStatus07), string.Join("\n", one));
        Assert.True(Has(one, MoveToSameName), string.Join("\n", one));

        var both = CompileWarnings(Program("F02S3C", "ALL ON", "MOVE-TO-SAME-NAME I-O-STATUS-07 OFF", ""), edition);
        Assert.False(Has(both, IoStatus07), string.Join("\n", both));
        Assert.False(Has(both, MoveToSameName), string.Join("\n", both));
    }

    /// <summary>After an OFF, a later ON enables the option again for the text that follows it (GR2 applies anew).</summary>
    [Theory]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Gr3_Off_ThenOnAgain_ReEnables(int edition)
    {
        var w = CompileWarnings(Program("F02S3D", "ALL ON|ALL OFF", "I-O-STATUS-07 ON", ""), edition);
        Assert.True(Has(w, IoStatus07), string.Join("\n", w));
        Assert.False(Has(w, MoveToSameName), string.Join("\n", w));
    }
}
