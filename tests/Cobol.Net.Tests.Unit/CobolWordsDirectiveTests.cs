// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using CobolNet;
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Parsing;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <c>&gt;&gt;COBOL-WORDS</c> directive (ISO §7.3.10) — Increment A: the text-stage parser
/// (<see cref="CobolWordsDirectiveProcessor"/>), the resulting <see cref="CobolWordsMap"/> composition, the
/// SR1/SR2/SR5 syntax rules, and the introduction gate (COBOLNET0900 below 2023). Design SSOT:
/// <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.
/// </summary>
public sealed class CobolWordsDirectiveTests
{
    private static (CobolWordsMap Map, DiagnosticBag Diags) Run(string src, int std = 2023)
    {
        var bag = new DiagnosticBag();
        // The PIPELINE order (Frontend.Preprocess): the conditional-compilation driver runs first and owns the
        // ONE directive-word edition gate (kb/Work PB725 — one rule, one place, at the point a >>WORD is
        // recognized); this stage then parses the >>COBOL-WORDS lines the driver left in the text. Running the
        // stage alone would test a path the compiler does not have.
        string text = ConditionalCompilationProcessor.Process(
            src, CobolNet.Frontend.Frontend.LeftDirectives, bag, "t.cob", std);
        // Then the site stage, which judges §7.3.10.3 SR1's placement from the row's directivePlacement data and
        // consumes PUSH/POP into the ops the state stages replay (kb/Work PB1377).
        var (sited, _, stackOps) = DirectiveSiteProcessor.Process(text, bag, "t.cob");
        var (_, map) = CobolWordsDirectiveProcessor.Process(sited, bag, "t.cob", stackOps: stackOps);
        return (map, bag);
    }

    private static bool Has(DiagnosticBag b, string code) => b.Diagnostics.Any(d => d.Code == code);

    // ── the four options build the map ──────────────────────────────────────────────────────────────────────

    [Fact] // GR2 — EQUATE literal-2 becomes a synonym for literal-1.
    public void Equate_BuildsSynonym()
    {
        var (map, diags) = Run(">>COBOL-WORDS EQUATE \"DISPLAY\" WITH \"SHOW\"\n");
        Assert.False(diags.HasErrors);
        Assert.Equal("DISPLAY", map.Synonyms["SHOW"]);
        Assert.False(map.IsEmpty);
    }

    [Fact] // GR3 — UNDEFINE literal-3 loses reserved status.
    public void Undefine_BuildsDeReserved()
    {
        var (map, diags) = Run(">>COBOL-WORDS UNDEFINE \"MOVE\"\n");
        Assert.False(diags.HasErrors);
        Assert.Contains("MOVE", map.DeReserved);
        Assert.Empty(map.Synonyms);
    }

    [Fact] // GR4 — SUBSTITUTE literal-5 takes over; literal-4 becomes a user word (de-reserved).
    public void Substitute_BuildsSynonymAndDeReserved()
    {
        var (map, diags) = Run(">>COBOL-WORDS SUBSTITUTE \"MOVE\" BY \"MOVE-IT\"\n");
        Assert.False(diags.HasErrors);
        Assert.Equal("MOVE", map.Synonyms["MOVE-IT"]);
        Assert.Contains("MOVE", map.DeReserved);
    }

    [Fact] // GR5 — RESERVE literal-6 becomes reserved.
    public void Reserve_BuildsReserved()
    {
        var (map, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n");
        Assert.False(diags.HasErrors);
        Assert.Contains("FOO", map.Reserved);
    }

    [Fact] // SR2 — the literal content is case-insensitive (upper-cased).
    public void Literals_AreCaseInsensitive()
    {
        var (map, _) = Run(">>COBOL-WORDS EQUATE \"display\" WITH \"show\"\n");
        Assert.Equal("DISPLAY", map.Synonyms["SHOW"]);
    }

    // ── malformed directives → COBOLNET1623, no op collected ─────────────────────────────────────────────────

    [Theory]
    [InlineData(">>COBOL-WORDS BOGUS \"FOO\"\n")]                       // unknown option
    [InlineData(">>COBOL-WORDS EQUATE \"MOVE\"\n")]                     // EQUATE missing WITH literal-2
    [InlineData(">>COBOL-WORDS EQUATE \"MOVE\" BY \"MOVE-IT\"\n")]      // wrong join word (BY not WITH)
    [InlineData(">>COBOL-WORDS SUBSTITUTE \"MOVE\" WITH \"MOVE-IT\"\n")]// wrong join word (WITH not BY)
    [InlineData(">>COBOL-WORDS RESERVE FOO\n")]                         // SR2: unquoted literal
    [InlineData(">>COBOL-WORDS RESERVE X\"C1\"\n")]                     // SR2: hex-prefixed literal
    [InlineData(">>COBOL-WORDS RESERVE \"FO O\"\n")]                    // SR2: embedded space
    [InlineData(">>COBOL-WORDS\n")]                                     // no option
    public void Malformed_Emits1623_AndCollectsNoOp(string src)
    {
        var (map, diags) = Run(src);
        Assert.True(Has(diags, "COBOLNET1623"));
        Assert.True(map.IsEmpty);
    }

    [Fact] // SR4 — a new word that is not a well-formed user-defined word is rejected.
    public void Sr4_BadUserWord_Rejected1623()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"-BAD\"\n");
        Assert.True(Has(diags, "COBOLNET1623"));
    }

    [Fact] // SR5 — the same COBOL word may appear in at most one directive's literals.
    public void Sr5_DuplicateWord_Rejected1623()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n>>COBOL-WORDS EQUATE \"MOVE\" WITH \"FOO\"\n");
        Assert.True(Has(diags, "COBOLNET1623"));
    }

    [Fact] // SR1 — a directive after the first IDENTIFICATION DIVISION is illegal: COBOLNET2652, the ONE placement screen.
    public void Sr1_AfterIdDivision_RejectedAsAPlacementViolation()
    {
        var (_, diags) = Run("IDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n>>COBOL-WORDS RESERVE \"FOO\"\n");
        Assert.True(Has(diags, "COBOLNET2652"));
        Assert.False(Has(diags, "COBOLNET1623"));   // the directive stage no longer writes the rule a second time
    }

    [Fact] // §7.3.22.3 SR2 / §7.3.20.3 SR2 — a PUSH / POP NAMING COBOL-WORDS inherits its placement rule.
    public void PushOrPopNamingCobolWords_AfterIdDivision_Rejected()
    {
        foreach (string op in (string[])["PUSH", "POP"])
        {
            var (_, diags) = Run($">>PUSH COBOL-WORDS\nIDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n>>{op} COBOL-WORDS\n");
            Assert.True(Has(diags, "COBOLNET2652"), op);
        }

        // and the control: the same PUSH/POP BEFORE the first unit is legal.
        var (_, ok) = Run(">>PUSH COBOL-WORDS\n>>POP COBOL-WORDS\nIDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n");
        Assert.False(ok.HasErrors);
    }

    [Fact] // SR1 — a directive BEFORE the first IDENTIFICATION DIVISION is legal.
    public void Sr1_BeforeIdDivision_Accepted()
    {
        var (map, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\nIDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n");
        Assert.False(diags.HasErrors);
        Assert.Contains("FOO", map.Reserved);
    }

    // ── edition gate ────────────────────────────────────────────────────────────────────────────────────────

    [Fact] // §7.3.10 is a COBOL-2023 addition — below 2023 the directive word is COBOLNET0900.
    public void Below2023_Emits0900()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n", std: 2014);
        Assert.True(Has(diags, "COBOLNET0900"));
    }

    [Fact] // at 2023 the directive word introduces no gate diagnostic.
    public void At2023_NoGate()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n", std: 2023);
        Assert.False(Has(diags, "COBOLNET0900"));
    }

    // ── mechanics ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact] // the directive line is blanked (line-count preserving — the >>TURN H3 discipline).
    public void DirectiveLine_IsBlanked_LineCountPreserved()
    {
        const string src = ">>COBOL-WORDS RESERVE \"FOO\"\nIDENTIFICATION DIVISION.\n";
        var bag = new DiagnosticBag();
        var (outText, _) = CobolWordsDirectiveProcessor.Process(src, bag, "t.cob");
        Assert.Equal(src.Count(c => c == '\n'), outText.Count(c => c == '\n'));
        Assert.DoesNotContain("COBOL-WORDS", outText);
    }

    [Fact] // no >>COBOL-WORDS directive ⇒ the empty map (the zero-overhead invariant).
    public void NoDirective_EmptyMap()
    {
        var (map, diags) = Run("IDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n");
        Assert.True(map.IsEmpty);
        Assert.False(diags.HasErrors);
    }

    // ══ Increment B — RESERVE/UNDEFINE via the composed ReservedWordSet + SR3/SR4 category validation ══════════

    // ── ReservedWordSet.Compose (ISO §7.3.10.4 GR3/GR5) ─────────────────────────────────────────────────────

    [Fact] // an empty map composes to the Default set (byte-identical).
    public void Compose_EmptyMap_IsDefault()
    {
        Assert.Same(ReservedWordSet.Default, ReservedWordSet.Compose(CobolWordsMap.Empty));
    }

    [Fact] // GR5 — RESERVE makes a fresh word reject when used as a user-defined word.
    public void Compose_Reserve_RejectsTheNewWord()
    {
        var map = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Reserve, null, "ZZBAR", 0)]);
        Assert.True(ReservedWordSet.Compose(map).RejectsAt("ZZBAR", 2023));
        Assert.False(ReservedWordSet.Default.RejectsAt("ZZBAR", 2023));   // not reserved without the directive
    }

    [Fact] // GR3 — UNDEFINE de-reserves a base-reserved word (RejectsAt flips false).
    public void Compose_Undefine_SuppressesABaseReservedWord()
    {
        Assert.True(ReservedWordSet.Default.RejectsAt("ACCEPT", 2023));   // ACCEPT is high-confidence reserved
        var map = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Undefine, "ACCEPT", null, 0)]);
        Assert.False(ReservedWordSet.Compose(map).RejectsAt("ACCEPT", 2023));
    }

    // ── CobolKeywordTokens (the reverse vocab map) ──────────────────────────────────────────────────────────

    [Fact]
    public void Keyword_Reserved_And_Context_AreKeywords_UserWordIsNot()
    {
        Assert.True(CobolKeywordTokens.IsKeyword("MOVE"));       // a hard reserved word
        Assert.True(CobolKeywordTokens.IsKeyword("display"));    // case-insensitive
        Assert.False(CobolKeywordTokens.IsKeyword("ZZUSERWORD"));
        Assert.True(CobolKeywordTokens.TryTokenType("DISPLAY", out int t) && t > 0);
    }

    // ── end-to-end through the compiler (RESERVE 0901 · SR3/SR4 1623) ───────────────────────────────────────

    private static IReadOnlyList<string> CompileErrors(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_CWords_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "cw.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "cw.dll"), DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return r.Errors;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    [Fact] // GR5 end-to-end — a RESERVE'd word used as a user-defined name is COBOLNET0901-rejected.
    public void Reserve_UsedAsUserWord_Rejected0901()
    {
        var errors = CompileErrors(
            "       >>COBOL-WORDS RESERVE \"FOO\"\n" +
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWE1.\n" +
            "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 FOO PIC X(3) VALUE \"ABC\".\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY FOO.\n           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOLNET0901") && e.Contains("FOO"));
    }

    [Fact] // SR3 end-to-end — the existing word must be reserved/context/intrinsic.
    public void Sr3_ExistingNotAWord_Rejected1623()
    {
        var errors = CompileErrors(
            "       >>COBOL-WORDS EQUATE \"NOTAWORD\" WITH \"SYN\"\n" +
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWE2.\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY \"X\".\n           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOLNET1623") && e.Contains("SR3"));
    }

    [Fact] // SR4 end-to-end — the new word must not itself be reserved.
    public void Sr4_NewWordReserved_Rejected1623()
    {
        var errors = CompileErrors(
            "       >>COBOL-WORDS RESERVE \"MOVE\"\n" +
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWE3.\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY \"X\".\n           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOLNET1623") && e.Contains("SR4"));
    }

    // ── Increment C — the token rewriter's de-reserved token-type set (the lexer subscript-mode input) ─────────

    [Fact] // UNDEFINE of a keyword contributes its token type to the lexer's data-name trigger set.
    public void DeReservedTokenTypes_IncludesTheUndefinedKeyword()
    {
        var map = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Undefine, "MOVE", null, 0)]);
        CobolKeywordTokens.TryTokenType("MOVE", out int moveType);
        Assert.Contains(moveType, CobolWordsRewriter.DeReservedTokenTypes(map));
    }

    [Fact] // the empty map yields no de-reserved token types.
    public void DeReservedTokenTypes_EmptyMap_Empty()
    {
        Assert.Empty(CobolWordsRewriter.DeReservedTokenTypes(CobolWordsMap.Empty));
    }

    [Fact] // a well-formed EQUATE (existing reserved, new a user word) raises no SR / reserved diagnostic.
    public void ValidEquate_NoDiagnostic()
    {
        var errors = CompileErrors(
            "       >>COBOL-WORDS EQUATE \"DISPLAY\" WITH \"SHOW\"\n" +
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWE4.\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY \"X\".\n           STOP RUN.\n");
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1623") || e.Contains("COBOLNET0901"));
    }

    // ── Increment D — intrinsic-function-name synonyms (GR2/GR3/GR4 for a FUNCTION name) ─────────────────────

    private const string IntrinsicProgram =
        "{DIRECTIVE}" +
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWFN.\n" +
        "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 R PIC 9.\n" +
        "       PROCEDURE DIVISION.\n       MAIN.\n" +
        "           COMPUTE R = FUNCTION {NAME}(3 7 5).\n           STOP RUN.\n";

    [Fact] // GR2 — EQUATE a synonym for an intrinsic; FUNCTION synonym(...) resolves to the intrinsic (no error).
    public void Intrinsic_EquateSynonym_Resolves()
    {
        var errors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", "       >>COBOL-WORDS EQUATE \"MAX\" WITH \"MYMAX\"\n")
            .Replace("{NAME}", "MYMAX"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1501"));
    }

    [Fact] // GR3 — UNDEFINE an intrinsic; FUNCTION MAX(...) is no longer a function (COBOLNET1501).
    public void Intrinsic_Undefine_NoLongerAFunction()
    {
        var errors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", "       >>COBOL-WORDS UNDEFINE \"MAX\"\n")
            .Replace("{NAME}", "MAX"));
        Assert.Contains(errors, e => e.Contains("COBOLNET1501"));
    }

    [Fact] // GR4 — SUBSTITUTE: the new name resolves; the old intrinsic name is no longer a function.
    public void Intrinsic_Substitute_NewResolves_OldRemoved()
    {
        var okErrors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", "       >>COBOL-WORDS SUBSTITUTE \"MAX\" BY \"MYMAX\"\n")
            .Replace("{NAME}", "MYMAX"));
        Assert.DoesNotContain(okErrors, e => e.Contains("COBOLNET1501"));

        var oldErrors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", "       >>COBOL-WORDS SUBSTITUTE \"MAX\" BY \"MYMAX\"\n")
            .Replace("{NAME}", "MAX"));
        Assert.Contains(oldErrors, e => e.Contains("COBOLNET1501"));
    }
}
