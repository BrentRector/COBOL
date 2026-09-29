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
    private (bool Ok, List<string> Lines) Compile(string mainName, int edition = 2023, bool checkOnly = true)
    {
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
            Path.Combine(_dir, mainName), Path.Combine(_dir, "out.dll"), DialectLevel: edition, CheckOnly: checkOnly,
            CopyPaths: [_dir], SourceFormat: InitialReferenceFormat.Auto));
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

    [Fact] // a NIST archive marker line is blanked, not dropped — the physical numbering holds
    public void NistArchiveMarker_DoesNotShiftLaterLines()
    {
        Write("n.cob", "*HEADER,COBOL,PB82N\n" + Head + "       PROCEDURE DIVISION.\n           MOVE UNDEF TO W.\n           STOP RUN.\n");
        var (ok, lines) = Compile("n.cob");
        Assert.False(ok);
        AssertPositioned(lines, "n.cob", 8, "error COBOLNET1639");
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
