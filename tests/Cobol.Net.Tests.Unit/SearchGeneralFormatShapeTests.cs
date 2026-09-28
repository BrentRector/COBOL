// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;                             // TestRepo — the ONE repo-root locator
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SEARCH STATEMENT IS WRITTEN IN ONE OF ITS TWO PRINTED GENERAL FORMATS OR IT IS REFUSED AT COMPILE TIME
/// (kb/Work PB446, PB444). ISO/IEC 1989:2023 §14.9.37.2, rendered from PDF page 750 / printed folio 720:
/// <c>SEARCH identifier-1 [VARYING …] [AT END imperative-statement-1] {WHEN condition-1 {imperative-statement-2 |
/// NEXT SENTENCE}} … [END-SEARCH]</c> and <c>SEARCH ALL identifier-1 [AT END imperative-statement-1] WHEN …
/// [AND …] … {imperative-statement-2 | NEXT SENTENCE} [END-SEARCH]</c>; §14.9.37.3 SR4 forbids END-SEARCH with
/// NEXT SENTENCE. Every refused shape below COMPILED before — a KEY phrase no binder read, a second Format-2
/// WHEN, an AT END NEXT SENTENCE, a NEXT SENTENCE beside another statement, and the SR4 pair, which was given a
/// meaning — and every row is written behind a GO TO so the statement is never reached: the verdict is a
/// COMPILE-TIME one (§4.2.2), never a run-time one.
/// <para>This class replaces <c>CobolSharp.Tests.Unit/Overlenient/M421_OverlenientSearchTests</c>, whose three
/// facts had EMPTY bodies and passed without looking at anything while the row sat at DIVERGES.</para>
/// </summary>
public sealed class SearchGeneralFormatShapeTests
{
    private const string Head = """
IDENTIFICATION DIVISION.
PROGRAM-ID. PB446{0}.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 T.
   05 E OCCURS 5 ASCENDING KEY K INDEXED BY IX.
      10 K PIC 9(2).
PROCEDURE DIVISION.
MAIN.
    GO TO SKIPPER.

""";

    private static string Program(string id, string body) =>
        string.Format(Head, id) + body + "\nSKIPPER.\n    STOP RUN.\n";

    /// <summary>Each shape no general format prints, at the first and the last edition (the formats did not
    /// change shape across 1985/2002/2014/2023), refused under the code that names it.</summary>
    [Theory]
    [InlineData("A", "    SEARCH ALL E KEY IS K WHEN K (IX) = 3 DISPLAY \"H\" END-SEARCH.", "COBOLNET2269", "SEARCH ALL … KEY")]
    [InlineData("B", "    SEARCH E KEY IS K WHEN K (IX) = 3 DISPLAY \"H\" END-SEARCH.", "COBOLNET2269", "SEARCH … KEY")]
    [InlineData("C", "    SEARCH ALL E WHEN K (IX) = 5 DISPLAY \"A\" WHEN K (IX) = 3 DISPLAY \"B\" END-SEARCH.",
        "COBOLNET2269", "second WHEN phrase")]
    [InlineData("D", "    SEARCH ALL E WHEN K (IX) = 5 DISPLAY \"A\" WHEN K (IX) = 3 DISPLAY \"B\".",
        "COBOLNET2269", "second WHEN phrase")]
    [InlineData("E", "    SEARCH ALL E NOT AT END DISPLAY \"Y\" WHEN K (IX) = 1 DISPLAY \"H\" END-SEARCH.",
        "COBOLNET2269", "SEARCH ALL … NOT AT END")]
    [InlineData("F", "    SEARCH E AT END NEXT SENTENCE WHEN K (IX) = 9 DISPLAY \"H\".", "COBOLNET2269", "NEXT SENTENCE:")]
    [InlineData("G", "    SEARCH E WHEN K (IX) = 3 NEXT SENTENCE DISPLAY \"T\".", "COBOLNET2269", "NEXT SENTENCE:")]
    [InlineData("H", "    SEARCH E WHEN K (IX) = 3 NEXT SENTENCE END-SEARCH.", "COBOLNET2409", "§14.9.37.3 SR4")]
    [InlineData("I", "    SEARCH ALL E WHEN K (IX) = 3 NEXT SENTENCE END-SEARCH.", "COBOLNET2409", "§14.9.37.3 SR4")]
    [InlineData("J", "    SEARCH E WHEN K (IX) = 9 DISPLAY \"N\" WHEN K (IX) = 3 NEXT SENTENCE END-SEARCH.",
        "COBOLNET2409", "SEARCH: the END-SEARCH phrase")]
    public void ShapeNoFormatPrints_IsRefusedAtCompileTime(string id, string body, string code, string text)
    {
        foreach (int ed in new[] { 85, 2023 })
        {
            var (ok, errors) = Compile(Program(id, body), ed);
            Assert.False(ok, $"[{id}] must be refused at --std {ed}");
            Assert.Contains(errors, e => e.Contains(code, StringComparison.Ordinal)
                                         && e.Contains(text, StringComparison.Ordinal));
        }
    }

    /// <summary>The CONFORMING spellings next to each refusal, so a screen that refuses too much is red here:
    /// NEXT SENTENCE as the whole WHEN body with no END-SEARCH, in both formats; CONTINUE where NEXT SENTENCE is
    /// forbidden; the AT-less AT END (AT is not underlined — §5.2.3); and IF Format 2's two NEXT SENTENCE arms.</summary>
    [Theory]
    [InlineData("K", "    SEARCH E AT END DISPLAY \"N\" WHEN K (IX) = 3 NEXT SENTENCE.")]
    [InlineData("L", "    SEARCH ALL E AT END DISPLAY \"N\" WHEN K (IX) = 3 NEXT SENTENCE.")]
    [InlineData("M", "    SEARCH ALL E END DISPLAY \"N\" WHEN K (IX) = 3 DISPLAY \"H\" END-SEARCH.")]
    [InlineData("N", "    IF K (1) = 1 NEXT SENTENCE ELSE NEXT SENTENCE.")]
    [InlineData("O", "    SEARCH E WHEN K (IX) = 3 DISPLAY \"A\" WHEN K (IX) = 4 CONTINUE END-SEARCH.")]
    public void ConformingSpelling_Compiles(string id, string body)
    {
        foreach (int ed in new[] { 85, 2023 })
        {
            var (ok, errors) = Compile(Program(id, body), ed);
            Assert.True(ok, $"[{id}] --std {ed}: {string.Join("\n", errors)}");
        }
    }

    /// <summary>⛔ THE GRAMMAR IS THE FORMAT (kb/Work PB446). Pins the three rule shapes the PDF page decides, so
    /// the over-acceptances cannot come back as a quiet grammar edit: no KEY phrase rule, exactly one Format-2
    /// WHEN, and an AT END phrase with no NOT arm.</summary>
    [Fact]
    public void SearchGrammar_SpellsThePrintedFormats()
    {
        // The RULES only — the grammar's own comments name the deleted shapes to explain why they are gone.
        string g4 = string.Join("\n", File.ReadAllLines(Path.Combine(TestRepo.Src("Cobol.Net.Frontend"), "Grammar",
                "Core", "CobolControlFlow.g4"))
            .Select(l => l.IndexOf("//", StringComparison.Ordinal) is var c and >= 0 ? l[..c] : l));
        Assert.DoesNotContain("searchAllKeyPhrase", g4, StringComparison.Ordinal);
        Assert.DoesNotContain("searchAllWhenClause+", g4, StringComparison.Ordinal);
        Assert.DoesNotContain("searchAllWhenClause*", g4, StringComparison.Ordinal);
        int at = g4.IndexOf("\nsearchAtEndClause", StringComparison.Ordinal);
        Assert.True(at >= 0, "searchAtEndClause rule not found");
        string rule = g4[at..g4.IndexOf(';', at)];
        Assert.DoesNotContain("NOT", rule, StringComparison.Ordinal);
    }

    private static (bool Ok, IReadOnlyList<string> Errors) Compile(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB446_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "pb446.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb446.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success, r.Errors);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
