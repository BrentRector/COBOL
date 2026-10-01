// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1066, the SOURCE FORMAT half — ISO §14.9.28.4 GR14: "An implicit PUSH ALL followed by TURN OFF ALL is
/// assumed at the end of imperative-statement-1. Immediately preceding the END PERFORM phrase, there is an implicit
/// POP ALL". ALL is every pushable directive (§7.3.22.4 GR2: "all of the directives other than EVALUATE, IF, PAGE,
/// POP, or PUSH"), and SOURCE FORMAT is one, so a <c>&gt;&gt;SOURCE FORMAT</c> written in a WHEN or FINALLY phrase is
/// undone before END-PERFORM exactly as a <c>&gt;&gt;DEFINE</c> there is (<see cref="ExceptionPerformDefineScopeTests"/>).
/// <para><b>The decision these tests pin</b> (a reference format is a PER-LINE property, §6.5, so the op is a boundary
/// between physical lines): the POP restores the format BEFORE the physical line that holds END-PERFORM, so that line is
/// itself read in the restored format — the phrase is text FOLLOWING the POP, and §7.3.24.3 1) makes a format govern "the
/// source text … following" the directive. The PUSH takes effect after the line that ends imperative-statement-1.</para>
/// </summary>
public sealed class ExceptionPerformSourceFormatScopeTests
{
    // ── the program-level behavior ─────────────────────────────────────────────────────────────────────────────

    /// <summary>FIXED start; the handler switches to FREE; the sequence-numbered fixed-form line after END-PERFORM is read
    /// in the RESTORED fixed form. Before the fix it was read in free form and was a syntax error
    /// (<c>unexpected '000100'</c>).</summary>
    [Fact]
    public void FixedToFree_InAHandler_IsUndoneAtEndPerform()
    {
        var tree = Parse(InitialReferenceFormat.Fixed, """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1066F.
                   PROCEDURE DIVISION.
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                   >>SOURCE FORMAT IS FREE
                           DISPLAY "HANDLER"
                       END-PERFORM
            000100     DISPLAY "FIXED-AFTER".
                       STOP RUN.
            """);
        Assert.Contains("\"HANDLER\"", tree.GetText(), StringComparison.Ordinal);
        Assert.Contains("\"FIXED-AFTER\"", tree.GetText(), StringComparison.Ordinal);
    }

    /// <summary>FREE start; the handler switches to FIXED; END-PERFORM — written at column 1, which only free form
    /// reads as a word — is read in the RESTORED free form, because the POP precedes the phrase. Read in fixed form
    /// its <c>END-PE</c> would be a sequence area and its <c>R</c> an invalid indicator (COBOLNET2616).</summary>
    [Fact]
    public void FreeToFixed_InAHandler_RestoresBeforeTheEndPerformLineItself()
    {
        var tree = Parse(InitialReferenceFormat.Free, """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1066G.
            PROCEDURE DIVISION.
            PERFORM
                DISPLAY "IMP1"
            WHEN EC-SIZE
            >>SOURCE FORMAT IS FIXED
                       DISPLAY "HANDLER"
            END-PERFORM
            DISPLAY "FREE-AFTER".
            STOP RUN.
            """);
        Assert.Contains("\"HANDLER\"", tree.GetText(), StringComparison.Ordinal);
        Assert.Contains("\"FREE-AFTER\"", tree.GetText(), StringComparison.Ordinal);
    }

    /// <summary>The control: a directive in imperative-statement-1 PRECEDES the PUSH, so it survives END-PERFORM — the
    /// text after the phrase is still free form.</summary>
    [Fact]
    public void SourceFormat_InImperativeStatement1_PrecedesThePushAndSurvives()
    {
        var tree = Parse(InitialReferenceFormat.Fixed, """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1066I.
                   PROCEDURE DIVISION.
                       PERFORM
                   >>SOURCE FORMAT IS FREE
            DISPLAY "IMP1"
            WHEN EC-SIZE
            DISPLAY "HANDLER"
            END-PERFORM
            DISPLAY "FREE-AFTER".
            STOP RUN.
            """);
        Assert.Contains("\"FREE-AFTER\"", tree.GetText(), StringComparison.Ordinal);
    }

    /// <summary>A directive in the FINALLY phrase is undone at END-PERFORM too (GR14 names the END PERFORM phrase, not the
    /// WHEN phrase).</summary>
    [Fact]
    public void FixedToFree_InAFinallyPhrase_IsUndoneAtEndPerform()
    {
        var tree = Parse(InitialReferenceFormat.Fixed, """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1066Y.
                   PROCEDURE DIVISION.
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                           DISPLAY "HANDLER"
                       FINALLY
                   >>SOURCE FORMAT IS FREE
                           DISPLAY "FINAL"
                       END-PERFORM
            000100     DISPLAY "FIXED-AFTER".
                       STOP RUN.
            """);
        Assert.Contains("\"FIXED-AFTER\"", tree.GetText(), StringComparison.Ordinal);
    }

    /// <summary>Nested constructs: the inner POP restores the OUTER handler's format (free), the outer POP the
    /// program's (fixed). <c>DISPLAY "BETWEEN"</c> at column 1 is legal only in free form, and the sequence-numbered line
    /// at the end only in fixed form.</summary>
    [Fact]
    public void NestedPerforms_EachPopRestoresItsOwnPush()
    {
        var tree = Parse(InitialReferenceFormat.Fixed, """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1066N.
                   PROCEDURE DIVISION.
                       PERFORM
                           DISPLAY "O-IMP1"
                       WHEN EC-SIZE
                   >>SOURCE FORMAT IS FREE
            PERFORM
                DISPLAY "I-IMP1"
            WHEN EC-SIZE
            >>SOURCE FORMAT IS FIXED
                       DISPLAY "I-HANDLER"
            END-PERFORM
            DISPLAY "BETWEEN"
                       END-PERFORM
            000100     DISPLAY "FIXED-AFTER".
                       STOP RUN.
            """);
        string text = tree.GetText();
        Assert.Contains("\"BETWEEN\"", text, StringComparison.Ordinal);
        Assert.Contains("\"FIXED-AFTER\"", text, StringComparison.Ordinal);
    }

    /// <summary>An inner exception-checking PERFORM that ENDS the outer one's imperative-statement-1 (its END-PERFORM is the
    /// last token of it): the inner POP is "immediately preceding" that END-PERFORM and the outer PUSH is "at the end of"
    /// imperative-statement-1, so the inner POP comes FIRST. Ordered by token index the outer PUSH came first, the stack
    /// paired the inner POP with it, and the inner POP was dropped — the inner handler's format outlived its END-PERFORM
    /// (here the sequence-numbered line after the OUTER END-PERFORM is then unreadable).</summary>
    [Fact]
    public void AnInnerPerformEndingTheOuterImperative1_PopsBeforeTheOuterPush()
    {
        var tree = Parse(InitialReferenceFormat.Fixed, """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1066Q.
                   PROCEDURE DIVISION.
                       PERFORM
                           PERFORM
                               DISPLAY "I-IMP1"
                           WHEN EC-SIZE
                   >>SOURCE FORMAT IS FREE
            DISPLAY "I-HANDLER"
                           END-PERFORM
                       WHEN EC-SIZE
                           DISPLAY "O-HANDLER"
                       END-PERFORM
            000100     DISPLAY "FIXED-AFTER".
                       STOP RUN.
            """);
        Assert.Contains("\"I-HANDLER\"", tree.GetText(), StringComparison.Ordinal);
        Assert.Contains("\"FIXED-AFTER\"", tree.GetText(), StringComparison.Ordinal);
    }

    /// <summary>The same program under <c>--source-format auto</c>, the mode the corpus runner compiles in: the detector sees
    /// the leading comment indicator, classifies the file FIXED, and the implicit POP restores that detected format (a file
    /// the detector reads as free form restores FREE, which is the format it is in).</summary>
    [Fact]
    public void AutoDetectedFixedForm_IsRestoredAtEndPerform()
    {
        var tree = Parse(InitialReferenceFormat.Auto, """
                  *> a fixed-form file (the column-7 indicator)
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1066A.
                   PROCEDURE DIVISION.
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                   >>SOURCE FORMAT IS FREE
                           DISPLAY "HANDLER"
                       END-PERFORM
            000100     DISPLAY "FIXED-AFTER".
                       STOP RUN.
            """);
        Assert.Contains("\"FIXED-AFTER\"", tree.GetText(), StringComparison.Ordinal);
    }

    /// <summary>The PERFORM lives in library text whose handler switches the format and whose END-PERFORM and following
    /// line need the restored one: library text is converted by its own walker with its own format state
    /// (§7.3.24.3 3 and 5), so the implicit ops reach it keyed to ITS physical lines.</summary>
    [Fact]
    public void ThePerformInALibraryText_IsRestoredByTheLibraryTextsOwnWalker()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"pb1066sf_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "PB1066SF.cpy"), string.Join("\n",
                "           PERFORM",
                "               DISPLAY \"IMP1\"",
                "           WHEN EC-SIZE",
                "       >>SOURCE FORMAT IS FREE",
                "               DISPLAY \"HANDLER\"",
                "           END-PERFORM",
                "000100     DISPLAY \"CB-AFTER\".", ""));
            var tree = Parse(InitialReferenceFormat.Fixed, """
                       IDENTIFICATION DIVISION.
                       PROGRAM-ID. PB1066L.
                       PROCEDURE DIVISION.
                           COPY PB1066SF.
                           STOP RUN.
                """, dir);
            Assert.Contains("\"CB-AFTER\"", tree.GetText(), StringComparison.Ordinal);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { /* best-effort */ } }
    }

    /// <summary>A program whose OWN syntax error sits after a handler's format directive reports that error — the
    /// speculative re-run the front end makes for the implicit POP is never the answer when it does no better, so the
    /// diagnostics are the source's, not the guess's.</summary>
    [Fact]
    public void ARealSyntaxError_AfterTheBracket_KeepsItsOwnDiagnostics()
    {
        var (tree, errors) = TryParse(InitialReferenceFormat.Fixed, """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1066E.
                   PROCEDURE DIVISION.
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                   >>SOURCE FORMAT IS FREE
                           DISPLAY "HANDLER"
                       END-PERFORM
                       FOO BAR BAZ.
                       STOP RUN.
            """);
        Assert.Null(tree);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("'FOO'", StringComparison.Ordinal));   // the program's own error, named
    }

    // ── the normalizer's boundary rule, directly ───────────────────────────────────────────────────────────────

    /// <summary>The ops are boundaries BETWEEN physical lines: the PUSH after the line that ends imperative-statement-1
    /// (line 1), the POP before END-PERFORM's line (line 4). Line 3 is free form (the directive on line 2 switched it);
    /// line 4 is the sequence-numbered fixed line the POP restores — read as fixed, its sequence area is dropped.</summary>
    [Fact]
    public void TheWalker_AppliesThePopBeforeItsLine_AndThePushAfterItsLine()
    {
        string source = string.Join("\n",
            "           DISPLAY \"A\".",
            "       >>SOURCE FORMAT IS FREE",
            "DISPLAY \"B\"",
            "000100     DISPLAY \"D\".", "");
        var withOps = ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, 2023, permissive: false, null, "t.cob",
            initialFixed: true, out var formats,
            implicitOps: [new(1, DirectiveStackKind.Push, null), new(4, DirectiveStackKind.Pop, null)]);
        string[] lines = withOps.Text.Split('\n');
        Assert.Equal("DISPLAY \"B\"", lines[2].Trim());
        Assert.Equal("DISPLAY \"D\".", lines[3].Trim());   // the fixed-form conversion dropped "000100" and the margin
        Assert.True(formats.LibraryTextDefaultAt(3) == false);
        Assert.True(formats.LibraryTextDefaultAt(4) == true);

        // …without the ops the same line is read in free form, sequence area and all — the unfixed behavior.
        var without = ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, 2023, permissive: false, null, "t.cob",
            initialFixed: true, out _);
        Assert.Contains("000100", without.Text.Split('\n')[3], StringComparison.Ordinal);
    }

    /// <summary>A PUSH and POP on ONE physical line bracket no line — no directive can lie between — so the pair is
    /// dropped whole and the format is untouched.</summary>
    [Fact]
    public void ABracketOnOneLine_ChangesNothing()
    {
        string source = string.Join("\n",
            "           DISPLAY \"A\".",
            "       >>SOURCE FORMAT IS FREE",
            "DISPLAY \"B\"", "");
        var m = ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, 2023, permissive: false, null, "t.cob",
            initialFixed: true, out var formats,
            implicitOps: [new(3, DirectiveStackKind.Push, null), new(3, DirectiveStackKind.Pop, null)]);
        Assert.Equal("DISPLAY \"B\"", m.Text.Split('\n')[2].Trim());
        Assert.True(formats.LibraryTextDefaultAt(3) == false);
    }

    // ── the placement ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Only a bracket that encloses a WRITTEN format directive of its own file is kept (a pair enclosing none
    /// restores the format it saved), each op at its physical line; a pair split across two files is kept by neither.</summary>
    [Fact]
    public void Place_KeepsOnlyBracketsEnclosingAFormatDirective_OfOneFile()
    {
        var origins = Enumerable.Range(1, 20).Select(n => new SourceOrigin("main.cob", n)).ToList();
        origins[14] = new SourceOrigin("lib.cpy", 3);   // resultant line 15 is library text
        var formats = new Dictionary<string, ReferenceFormatMap>
        {
            ["main.cob"] = ReferenceFormatMap.Create(true, false, [], [7], []),
            ["lib.cpy"] = ReferenceFormatMap.Create(true, false, [], [], []),
        };
        DirectiveStackOp Push(int l) => new(l, DirectiveStackKind.Push, null);
        DirectiveStackOp Pop(int l) => new(l, DirectiveStackKind.Pop, null);
        var placed = ImplicitFormatOps.Place(
            [Push(2), Pop(4),      // encloses no directive — dropped
             Push(5), Pop(9),      // encloses the directive on line 7 — kept
             Push(10), Pop(15)],   // POP is in library text — a pair across files, kept by neither
            origins, formats);
        Assert.Equal([Push(5), Pop(9)], placed.For("main.cob"));
        Assert.Empty(placed.For("lib.cpy"));
        Assert.True(ImplicitFormatOps.None.Equals(ImplicitFormatOps.Place([Push(2), Pop(4)], origins, formats)));
    }

    // ── fixture ───────────────────────────────────────────────────────────────────────────────────────────────

    private static CobolNet.Frontend.Generated.CobolParserCore.CompilationUnitContext Parse(InitialReferenceFormat initial, string source, string? copyDir = null)
    {
        var (tree, errors) = TryParse(initial, source, copyDir);
        Assert.True(tree is not null, string.Join("\n", errors));
        return tree;
    }

    private static (CobolNet.Frontend.Generated.CobolParserCore.CompilationUnitContext? Tree, IReadOnlyList<string> Errors) TryParse(
        InitialReferenceFormat initial, string source, string? copyDir = null)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"pb1066s_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "pb1066.cob");
            File.WriteAllText(path, source + "\n");
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = initial, DialectLevel = 2023 };
            if (copyDir is not null) frontend.AddCopySearchPath(copyDir);
            var tree = frontend.Parse(path, diags);
            return (tree, diags.HasErrors ? [.. diags.Diagnostics.Select(d => d.Message)] : []);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { /* best-effort */ } }
    }
}
