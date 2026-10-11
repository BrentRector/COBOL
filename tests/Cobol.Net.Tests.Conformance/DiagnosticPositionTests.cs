// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB82 — every user-facing diagnostic names the file, line and column the USER edits, through the ONE
/// origin map the preprocessing chain builds: the parse layer (the ANTLR listener, which used to report every
/// syntax error one line late), the binder (whose ~160 report sites used to append a bare
/// <c>error CODE: message</c>), and the parse-tree conformance passes. A COPY, a REPLACE, a fixed-form
/// continuation join and a NIST archive marker each shift the RESULTANT text; none of them may shift a reported
/// line. Positions are <c>file(line,col)</c>, 1-based, the shape the parse-layer diagnostics always printed.
/// </summary>
public sealed class DiagnosticPositionTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pos_" + Guid.NewGuid().ToString("N")[..8]);

    public DiagnosticPositionTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ } }

    private void Write(string name, string content) => File.WriteAllText(Path.Combine(_dir, name), content);

    /// <summary>Compile <paramref name="mainName"/> (already written into the test directory) and return every
    /// error + warning line the driver produced. The test directory, which holds the copybooks too, is named as a
    /// <c>--copy</c> search path, as a user names theirs: the default COBOL library is the working directory plus the
    /// --copy directories, never the source file's own (docs/CONFORMANCE.md DOC-A.1-40, kb/Work PB1355).</summary>
    private (bool Ok, List<string> Lines) Compile(string mainName, int edition = 2023, bool checkOnly = true,
        string? nist = null, InitialReferenceFormat format = InitialReferenceFormat.Auto)
    {
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
            Path.Combine(_dir, mainName), Path.Combine(_dir, "out.dll"), NistTestName: nist, DialectLevel: edition,
            CheckOnly: checkOnly, CopyPaths: [_dir], SourceFormat: format));
        return (r.Success, [.. r.Errors, .. r.Warnings]);
    }

    private static void AssertPositioned(IEnumerable<string> lines, string fileName, int line, string codeAndText)
    {
        var all = lines.ToList();
        Assert.True(all.Any(d => d.Contains($"{fileName}({line},") && d.Contains(codeAndText)),
            $"expected a diagnostic positioned at {fileName}({line},…) containing '{codeAndText}'; got:\n"
            + string.Join("\n", all.DefaultIfEmpty("(none)")));
    }

    private const string Head =
        "       IDENTIFICATION DIVISION.\n" +
        "       PROGRAM-ID. PB82T.\n" +
        "       DATA DIVISION.\n" +
        "       WORKING-STORAGE SECTION.\n" +
        "       01 W PIC X.\n";

    [Fact] // the parse layer: ANTLR's 1-based line went into a 0-based SourceLocation — every syntax error was one late
    public void ParseError_NamesTheSourceLine()
    {
        Write("p.cob", Head + "       PROCEDURE DIVISION.\n           MOVE 1 TO.\n           STOP RUN.\n");
        var (ok, lines) = Compile("p.cob");
        Assert.False(ok);
        AssertPositioned(lines, "p.cob", 7, "error COBOL");
        Assert.DoesNotContain(lines, d => d.Contains("p.cob(8,"));
    }

    [Fact] // the binder: a bind-time diagnostic carries file(line,col) — the statement cursor
    public void BindError_NamesFileLineAndColumn()
    {
        Write("b.cob", Head + "       PROCEDURE DIVISION.\n           MOVE UNDEF TO W.\n           STOP RUN.\n");
        var (ok, lines) = Compile("b.cob");
        Assert.False(ok);
        AssertPositioned(lines, "b.cob", 7, "error COBOLNET1639");
        Assert.Contains(lines, d => d.Contains("b.cob(7,12): error COBOLNET1639"));   // column 12 = the M of MOVE
    }

    [Fact] // kb/Work PB1728: a PICTURE string's trimmed clause separator (`PIC X, VALUE …`) must not shift the column
    // of every later token on the line — the lexer used to seek back over the ',' without restoring its column, so a
    // diagnostic on the VALUE literal pointed one column right. The two lines differ only in ',' versus ' ', so the
    // literal is at the same column in both, and a diagnostic anchored on it must say so.
    public void TokenAfterTrimmedPictureSeparator_KeepsItsColumn()
    {
        string At(string name, string pictureTail)
        {
            Write(name, Head + $"       01 V PIC X{pictureTail} VALUE \"A\"B.\n       PROCEDURE DIVISION.\n           STOP RUN.\n");
            var (ok, lines) = Compile(name);
            Assert.False(ok);
            string d = Assert.Single(lines, l => l.Contains("COBOLNET2633"));   // §8.3.5 5): anchored on the literal
            return d[d.IndexOf($"{name}(", StringComparison.Ordinal)..d.IndexOf(')')];
        }
        Assert.Equal(At("s.cob", " ").Replace("s.cob", ""), At("c.cob", ",").Replace("c.cob", ""));
    }

    [Fact] // the entry cursor: a data-description diagnostic names its entry's line
    public void DataEntryError_NamesTheEntryLine()
    {
        Write("d.cob", Head + "       01 V PIC 9 VALUE 123.\n       PROCEDURE DIVISION.\n           STOP RUN.\n");
        var (ok, lines) = Compile("d.cob");
        Assert.False(ok);
        AssertPositioned(lines, "d.cob", 6, "error COBOLNET");
    }

    [Fact] // COPY: the lines AFTER a COPY are the main file's own; the copied text names the copybook
    public void Copy_MainLinesStayMain_CopiedLinesNameTheCopybook()
    {
        Write("three.cpy", "       01 C1 PIC X.\n       01 C2 PIC X.\n       01 C3 PIC X.\n");
        Write("proc.cpy", "      *> line 1 of proc.cpy\n           MOVE NOPE TO W.\n");
        Write("c.cob", Head
            + "       COPY \"three.cpy\".\n"                 // line 6 (three lines incorporated)
            + "       PROCEDURE DIVISION.\n"                 // line 7
            + "           MOVE UNDEFINED-ITEM TO W.\n"       // line 8 (resultant ordinal 10)
            + "           COPY \"proc.cpy\".\n"              // line 9
            + "           STOP RUN.\n");
        var (ok, lines) = Compile("c.cob");
        Assert.False(ok);
        AssertPositioned(lines, "c.cob", 8, "'UNDEFINED-ITEM' is not defined");
        AssertPositioned(lines, "proc.cpy", 2, "'NOPE' is not defined");
        Assert.DoesNotContain(lines, d => d.Contains("c.cob(10,"));
    }

    [Fact] // a syntax error INSIDE a copybook names the copybook's own line
    public void ParseErrorInsideCopybook_NamesTheCopybookLine()
    {
        Write("bad.cpy", "      *> line 1\n           MOVE 1 TO.\n");
        Write("k.cob", Head + "       PROCEDURE DIVISION.\n           COPY \"bad.cpy\".\n           STOP RUN.\n");
        var (ok, lines) = Compile("k.cob");
        Assert.False(ok);
        AssertPositioned(lines, "bad.cpy", 2, "error COBOL");
    }

    [Fact] // REPLACE: the statement's own line vanishes from the resultant text; later lines keep their numbers
    public void ReplaceStatement_DoesNotShiftLaterLines()
    {
        Write("r.cob", Head
            + "       REPLACE ==XYZZY== BY ==PLUGH==.\n"    // line 6 — vanishes
            + "       PROCEDURE DIVISION.\n"                 // line 7
            + "           MOVE UNDEF TO W.\n"                // line 8
            + "           STOP RUN.\n");
        var (ok, lines) = Compile("r.cob");
        Assert.False(ok);
        AssertPositioned(lines, "r.cob", 8, "error COBOLNET1639");
    }

    [Fact] // a fixed-form continuation JOINS two physical lines; the lines after it keep their physical numbers
    public void FixedFormContinuation_DoesNotShiftLaterLines()
    {
        Write("f.cob", Head
            + "       01 L PIC X(30) VALUE \"a continued literal spanning two physi\n"   // line 6
            + "      -    \"cal lines\".\n"                                                // line 7 (joined into 6)
            + "       PROCEDURE DIVISION.\n"                                              // line 8
            + "           MOVE UNDEF TO W.\n"                                             // line 9
            + "           STOP RUN.\n");
        var (ok, lines) = Compile("f.cob");
        Assert.False(ok);
        AssertPositioned(lines, "f.cob", 9, "error COBOLNET1639");
    }

    [Fact] // under --nist a CCVS archive marker line is blanked, not dropped — the physical numbering holds (kb/Work PB1803)
    public void NistArchiveMarker_DoesNotShiftLaterLines()
    {
        Write("n.cob", "*HEADER,COBOL,PB82N\n" + Head + "       PROCEDURE DIVISION.\n           MOVE UNDEF TO W.\n           STOP RUN.\n");
        var (ok, lines) = Compile("n.cob", nist: "PB82N");
        Assert.False(ok);
        AssertPositioned(lines, "n.cob", 8, "error COBOLNET1639");
    }

    [Fact] // and without --nist the same line is NOT discarded: its indicator area holds 'R' (§6.3.3; kb/Work PB1803)
    public void ArchiveMarker_OutsideNist_IsDiagnosedAtItsOwnLine()
    {
        Write("m.cob", "*HEADER,COBOL,PB82N\n" + Head + "       PROCEDURE DIVISION.\n           STOP RUN.\n");
        var (ok, lines) = Compile("m.cob", format: InitialReferenceFormat.Fixed);   // the standard default (§7.3.24.3 2))
        Assert.False(ok);
        AssertPositioned(lines, "m.cob", 1, "error COBOLNET2616");
    }

    /// <summary>⛔ kb/Work PB1688 / PB1801 — a diagnostic names the PHYSICAL COLUMN, in every producer arm of the source
    /// map. The fixed-form logical conversion (§6.5) keeps only the program-text area (positions 8 to margin R), joins a
    /// continuation line's text onto the latest logical line, and the line-entry stage expands a TAB; COPY and REPLACE
    /// splice text of other lengths into a line. The resultant column the parser and the binder hold is none of the
    /// user's columns, so each arm is pinned at the column the programmer's editor shows: `char_neg_typedef85` used to
    /// report `(5,9)` for a word written at column 16.</summary>
    [Theory]
    // a plain fixed-form line: the parse error at the '.' written in column 21
    [InlineData("plain", "       PROCEDURE DIVISION.\n           MOVE 1 TO.\n           STOP RUN.\n", "plain.cob(7,21)", "COBOL0001")]
    // the binder's statement cursor on a plain fixed-form line: DISPLAY at column 12
    [InlineData("cursor", "       PROCEDURE DIVISION.\n           DISPLAY UNDEF.\n           STOP RUN.\n", "cursor.cob(7,12)", "COBOLNET1639")]
    // a fixed continuation FRAGMENT: the '.' is on the continuation line, at its own line and column
    [InlineData("contin", "       PROCEDURE DIVISION.\n           MOVE \"ABC\n      -    \"DEF\" TO.\n           STOP RUN.\n", "contin.cob(8,20)", "COBOL0001")]
    // a TAB in the program text counts ONE column, as in the editor (the tab expanded to positions 18-24)
    [InlineData("tabbed", "       PROCEDURE DIVISION.\n           MOVE 1\tTO.\n           STOP RUN.\n", "tabbed.cob(7,21)", "COBOL0001")]
    // after a >>SOURCE FORMAT FREE switch the line is read whole: DISPLAY at column 4
    [InlineData("switch", "       >>SOURCE FORMAT FREE\nPROCEDURE DIVISION.\n   DISPLAY UNDEF.\n   STOP RUN.\n", "switch.cob(8,4)", "COBOLNET1639")]
    public void FixedForm_DiagnosticNamesThePhysicalColumn(string name, string tail, string position, string code)
    {
        Write(name + ".cob", Head + tail);
        var (ok, lines) = Compile(name + ".cob", format: InitialReferenceFormat.Fixed);
        Assert.False(ok);
        Assert.True(lines.Any(d => d.Contains(position + ": error " + code)),
            $"expected '{position}: error {code}'; got:\n" + string.Join("\n", lines));
    }

    [Fact] // kb/Work PB1688 / PB1801 — the copybook arm and the REPLACE arm keep the physical column
    public void FixedForm_CopybookAndReplacedText_NameThePhysicalColumn()
    {
        Write("cols.cpy", "      *> line 1\n               MOVE 1 TO.\n");   // the '.' in column 25 of the copybook's line 2
        Write("k7.cob", Head + "       PROCEDURE DIVISION.\n           COPY \"cols.cpy\".\n           STOP RUN.\n");
        var (ok, lines) = Compile("k7.cob", format: InitialReferenceFormat.Fixed);
        Assert.False(ok);
        Assert.Contains(lines, d => d.Contains("cols.cpy(2,25): error COBOL0001"));
        // REPLACE substitutes a SHORTER text before the error on the same line: the '.' keeps the column it was written in
        Write("r7.cob", Head + "       REPLACE ==LONG-REPLACED-OPERAND== BY ==1==.\n       PROCEDURE DIVISION.\n"
            + "           MOVE LONG-REPLACED-OPERAND TO.\n           STOP RUN.\n");
        (ok, lines) = Compile("r7.cob", format: InitialReferenceFormat.Fixed);
        Assert.False(ok);
        Assert.Contains(lines, d => d.Contains("r7.cob(8,41): error COBOL0001"));
    }

    /// <summary>⛔ kb/Work PB2986 — the --nist CCVS placeholder substitution is a MAPPED stage: a placeholder and its
    /// substitute differ in length, and every token to the right of one keeps the column the programmer wrote it at. Each
    /// arm of the stage is pinned with a ')' (a stray token the parser reports) written after the placeholder: the
    /// substitute LONGER than the placeholder (<c>XXXXX058</c> → <c>"CONTROL"</c>, 8 → 9), SHORTER (<c>XXXXX083</c> →
    /// <c>DOTNET</c>, 8 → 6), the SELECT-entry arm that rewrites inside a region (<c>XXXXP001</c> → <c>"TF001"</c>), a
    /// DELETED card (<c>XXXXX069</c>, 8 → 0), and a placeholder in COPY library text, which takes the copybook's
    /// own file, line and column.</summary>
    [Fact]
    public void Nist_PlaceholderSubstitution_KeepsTheColumnsToItsRight()
    {
        Write("nx.cob", Head + "       PROCEDURE DIVISION.\n           DISPLAY XXXXX058 W ) .\n           STOP RUN.\n");
        var (ok, lines) = Compile("nx.cob", edition: 85, nist: "NX");
        Assert.False(ok);
        Assert.Contains(lines, d => d.Contains("nx.cob(7,31): error COBOL0307"));   // longer: the ')' is in column 31

        Write("ns.cob", "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. NS.\n       ENVIRONMENT DIVISION.\n"
            + "       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XXXXX083 ) .\n"   // shorter: the ')' is in column 34
            + "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
            + "           SELECT F ASSIGN TO XXXXP001 ORGANIZATION IS RELATIVE ) .\n"   // in a SELECT entry: column 65
            + "       DATA DIVISION.\n       PROCEDURE DIVISION.\n           STOP RUN.\n");
        (ok, lines) = Compile("ns.cob", edition: 85, nist: "NS");
        Assert.False(ok);
        Assert.Contains(lines, d => d.Contains("ns.cob(5,34): error COBOL0307"));
        Assert.Contains(lines, d => d.Contains("ns.cob(8,65): error COBOL0312"));

        Write("nd.cob", Head + "       PROCEDURE DIVISION.\n           DISPLAY XXXXX069 W ) .\n           STOP RUN.\n");
        (ok, lines) = Compile("nd.cob", edition: 85, nist: "ND");
        Assert.False(ok);
        Assert.Contains(lines, d => d.Contains("nd.cob(7,31): error COBOL0307"));   // deleted: the ')' STAYS in column 31

        Write("nc.cpy", "           DISPLAY XXXXX058 W ) .\n");
        Write("nk.cob", Head + "       PROCEDURE DIVISION.\n           COPY \"nc.cpy\".\n           STOP RUN.\n");
        (ok, lines) = Compile("nk.cob", edition: 85, nist: "NK");
        Assert.False(ok);
        Assert.Contains(lines, d => d.Contains("nc.cpy(1,31): error COBOL0307"));
    }

    [Fact] // kb/Work PB1671 — a COPY / REPLACE / directive diagnostic names the column of what it is about, not column 1
    public void TextManipulationAndDirectiveDiagnostics_NameTheirColumn()
    {
        Write("t7.cob", Head
            + "           >>DEFINE\n"                           // line 6: the directive's indicator in column 12
            + "       PROCEDURE DIVISION.\n"
            + "           COPY \"no-such-member.cpy\".\n"       // line 8: COPY in column 12
            + "           REPLACE \"LIT\" BY ==X==.\n"          // line 9: the literal operand in column 20
            + "           STOP RUN.\n");
        var (ok, lines) = Compile("t7.cob", format: InitialReferenceFormat.Fixed);
        Assert.False(ok);
        Assert.Contains(lines, d => d.Contains("t7.cob(6,12): error COBOLNET2651"));
        Assert.Contains(lines, d => d.Contains("t7.cob(8,12): error CBL3620"));
        Assert.Contains(lines, d => d.Contains("t7.cob(9,20): error COBOLNET1641"));
        // >>SOURCE FORMAT is read by the reference-format stage, not the directive stage: the same indicator column
        // (train 1055 review — it named column 1)
        Write("s7.cob", Head + "           >>SOURCE FORMAT BOGUS\n       PROCEDURE DIVISION.\n           STOP RUN.\n");
        (ok, lines) = Compile("s7.cob", format: InitialReferenceFormat.Fixed);
        Assert.False(ok);
        Assert.Contains(lines, d => d.Contains("s7.cob(6,12): error COBOLNET1911"));
    }

    [Fact] // the parse-tree conformance arm: an edition gate names the construct's position
    public void EditionGate_NamesTheConstructLine()
    {
        Write("e.cob", Head + "       PROCEDURE DIVISION.\n       P1.\n           EXIT PARAGRAPH.\n           STOP RUN.\n");
        var (ok, lines) = Compile("e.cob", edition: 85);
        Assert.False(ok);
        AssertPositioned(lines, "e.cob", 8, "COBOLNET0900");
    }

    [Fact] // a diagnostic with no positioned construct stays bare — never a fabricated "line 0"
    public void UnpositionedDiagnostic_HasNoPrefix()
    {
        var ed = new Binding.EditionContext(2023) { SourceFile = "x.cob" };
        ed.Error("COBOLNET0000", "about the unit as a whole");
        Assert.Equal("error COBOLNET0000: about the unit as a whole", ed.Diagnostics.Single());
    }
}
