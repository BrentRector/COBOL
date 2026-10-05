// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The compiler-directive roster is derived, not hand-kept (kb/Work PB725) — these tests are what keeps
/// "derived" true.
///
/// <para>The defect they close: <c>ConditionalCompilationProcessor</c> carried a flat
/// <c>KnownIgnoredDirectives</c> HashSet of directive NAMES with no edition column, so eleven ISO §7.3
/// directives — <c>&gt;&gt;PUSH</c>, <c>&gt;&gt;POP</c> and <c>&gt;&gt;DISPLAY</c> among them — compiled clean at
/// <c>--std 85</c>, an edition that has no compiler directives at all, while their siblings from the same
/// Annex E.2 item 5 list drew COBOLNET0900. A name set cannot be wrong about an edition it does not record, and
/// nothing could have failed. Now the roster IS the <c>directiveWords</c> column of
/// <c>tests/version-matrix/constructs.json</c>, and <see cref="Roster_Matches_TheSpecSection"/> re-derives it
/// from §7.3 itself, so a directive added to the standard's clause list without a row is a red test rather than
/// a silent under-rejection.</para>
/// </summary>
public sealed class CompilerDirectiveCatalogDriftTests
{
    /// <summary>Every clause under ISO §7.3 whose heading names a directive — the spec's own roster.</summary>
    private static Dictionary<string, string> SpecDirectives()
    {
        string path = TestRepo.Specs("ISO_COBOL.md");
        Assert.True(File.Exists(path),
            $"the ISO transcription is missing: {path} — run `git submodule update --init --recursive`");
        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(File.ReadAllText(path),
                     @"^#{4}\s+(7\.3\.\d+)\s+(.+?)\s+directive\s*$", RegexOptions.Multiline))
        {
            // "SOURCE FORMAT directive" — the directive is headed by its FIRST word (§7.3.3 SR6: compiler-
            // instruction opens with ONE compiler-directive word; FORMAT and IS are optional words in the
            // §7.3.24.2 general format).
            string word = m.Groups[2].Value.Split(' ')[0].Trim();
            found[word] = m.Groups[1].Value;
        }

        return found;
    }

    /// <summary>
    /// THE spec-derived check: every §7.3.x directive the standard defines has a catalog row, and every row
    /// the catalog claims is a §7.3 directive is one — except the two Annex E.2 item 21 directives the 2023
    /// text REMOVED, which by construction no longer have a §7.3 clause to be found in and are named here so the
    /// exemption is explicit rather than a silently loose comparison.
    /// </summary>
    [Fact]
    public void Roster_Matches_TheSpecSection()
    {
        var spec = SpecDirectives();
        var catalog = CompilerDirectiveCatalog.Words.ToHashSet(StringComparer.Ordinal);

        // Removed in 2023 (Annex E.2 item 21): still recognized, and still gated, at the editions that HAD them,
        // so they are catalog rows with no 2023 clause. Every other exemption is a bug.
        string[] removedIn2023 = ["FLAG-85", "FLAG-NATIVE-ARITHMETIC"];
        foreach (string w in removedIn2023)
            Assert.True(CompilerDirectiveCatalog.Find(w)?.RemovedIn == 2023,
                $"{w} is exempted from the §7.3 roster check because Annex E.2 item 21 removed it in 2023 — "
                + "its row must say so");

        var missing = spec.Keys.Where(w => !catalog.Contains(w)).Order(StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0,
            $"ISO §7.3 defines {spec.Count} directives; these have NO constructs.json row and are therefore "
            + $"ungated at every edition: [{string.Join(", ", missing)}] — add a row with directiveWords and "
            + "re-run scripts/gen-constructs.ps1");

        // The reverse direction is asked PER ROW, not per word: §7.3.16's construct is spelled
        // >>IF / >>ELSE / >>END-IF and §7.3.13's is >>EVALUATE / >>WHEN / >>END-EVALUATE, so the companion words
        // have no clause heading of their own and must not be — a bare >>ELSE is not a directive. What must
        // hold is that every ROW is anchored: at least one of its words heads a §7.3 clause.
        var unanchored = ConstructRegistry.Entries
            .Where(e => e.DirectiveWords.Count > 0 && e.RemovedIn != 2023
                        && !e.DirectiveWords.Any(spec.ContainsKey))
            .Select(e => $"{e.Id} [{string.Join("/", e.DirectiveWords)}]").Order(StringComparer.Ordinal).ToList();
        Assert.True(unanchored.Count == 0,
            $"these rows claim directive words that head NO ISO §7.3 clause: [{string.Join(", ", unanchored)}] "
            + "— §7.3.3 SR9 reserves >>IMP for implementor directives, so a non-standard word needs either a "
            + "clause or a written exemption here");
    }

    /// <summary>
    /// The clause a row cites is the clause the spec puts the directive in. The PUSH/POP pairing is the reason
    /// this is a test and not a reading: §7.3.20 is POP and §7.3.22 is PUSH, the reverse of the order the old
    /// code comment listed them in (kb/Work PB725).
    /// </summary>
    [Fact]
    public void EveryRow_CitesItsOwnClause()
    {
        var wrong = new List<string>();
        foreach (var (word, clause) in SpecDirectives())
            if (CompilerDirectiveCatalog.Find(word) is { } row && !row.Citation.Contains("§" + clause + ";")
                && !row.Citation.Contains("§" + clause + " "))
                wrong.Add($"{word} is §{clause} but its row cites \"{row.Citation}\"");

        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    /// <summary>Structural sanity: a directive word heads at most one row, and every row that claims words
    /// gates them with the edition band the registry funnel emits.</summary>
    [Fact]
    public void Structural_Sanity()
    {
        var words = CompilerDirectiveCatalog.Words.ToList();
        Assert.True(words.Count >= 17, $"only {words.Count} directive words — §7.3 defines 17 directives");
        Assert.All(words, w => Assert.Matches("^[A-Z][A-Z0-9-]*$", w));
        Assert.Equal(words.Count, words.Distinct(StringComparer.Ordinal).Count());
        Assert.All(words, w =>
        {
            var row = CompilerDirectiveCatalog.Find(w)!;
            Assert.Contains(row.DiagnosticCode, new[] { "COBOLNET0900", "COBOLNET0902" });
            Assert.InRange(row.IntroducedIn, 2002, 2023);   // COBOL-85 has no compiler directives at all
        });
        Assert.True(CompilerDirectiveCatalog.IsDirective("push"));   // case-insensitive (§8.3.1: words fold)
        Assert.Null(CompilerDirectiveCatalog.Find("IFF"));           // a typo is NOT a directive
    }

    /// <summary>
    /// <c>Frontend.LeftDirectives</c> answers WHICH STAGE OWNS THE LINE; the catalog answers WHETHER THE WORD
    /// MAY APPEAR. They are different questions, but a word left for a downstream stage that no longer has a
    /// catalog row would sail past the gate — so the subset relation is asserted, not assumed.
    /// </summary>
    [Fact]
    public void LeftDirectives_AreAllCatalogRows()
    {
        var orphans = CobolNet.Frontend.Frontend.LeftDirectives
            .Where(w => !CompilerDirectiveCatalog.IsDirective(w)).Order(StringComparer.Ordinal).ToList();
        Assert.True(orphans.Count == 0,
            $"Frontend.LeftDirectives holds [{string.Join(", ", orphans)}] with no catalog row — those lines "
            + "would reach their stage with no edition gate");
    }

    /// <summary>
    /// The behavioural proof, and the one that would have failed before PB725: at <c>--std 85</c> EVERY
    /// recognized directive word is rejected, because COBOL-85 has no compiler directives. A green gate that
    /// never looked at what changed proves nothing (feedback_green_gates_arent_evidence), so this drives the
    /// real text-manipulation stage rather than inspecting the table.
    /// </summary>
    [Fact]
    public void EveryDirective_IsRejectedAt85()
    {
        var silent = new List<string>();
        foreach (string word in CompilerDirectiveCatalog.Words)
        {
            var bag = new DiagnosticBag();
            ConditionalCompilationProcessor.Process(
                $">>{word} ALL\nIDENTIFICATION DIVISION.\n", CobolNet.Frontend.Frontend.LeftDirectives,
                bag, "t.cob", dialectLevel: 85);
            if (!bag.Diagnostics.Any(d => d.Code == "COBOLNET0900")) silent.Add(word);
        }

        // >>SOURCE FORMAT is consumed one stage earlier, by ReferenceFormatProcessor, so the driver never sees
        // it — its gate is asserted separately below.
        silent.Remove("SOURCE");
        Assert.True(silent.Count == 0,
            $"[{string.Join(", ", silent)}] compile clean at --std 85, an edition with no compiler directives "
            + "(ISO §7.3 is a COBOL-2002 introduction) — the PB725 under-rejection has returned");
    }

    /// <summary>The one directive whose gate cannot live with its siblings: <c>&gt;&gt;SOURCE FORMAT</c> is
    /// consumed by the reference-format normalizer before the conditional-compilation driver runs, so its gate
    /// emits there — same row, same COBOLNET0900, one stage earlier.</summary>
    [Fact]
    public void SourceFormatDirective_IsRejectedAt85()
    {
        var bag = new DiagnosticBag();
        ReferenceFormatProcessor.NormalizeToFreeForm(
            ">>SOURCE FORMAT IS FREE\nIDENTIFICATION DIVISION.\n", new ReferenceFormatDiagnostics(85, false, bag), "t.cob");
        Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET0900");

        var ok = new DiagnosticBag();
        ReferenceFormatProcessor.NormalizeToFreeForm(
            ">>SOURCE FORMAT IS FREE\nIDENTIFICATION DIVISION.\n", new ReferenceFormatDiagnostics(2002, false, ok), "t.cob");
        Assert.DoesNotContain(ok.Diagnostics, d => d.Code == "COBOLNET0900");
    }

    // ── The OPERAND column (kb/Work PB794) ───────────────────────────────────────────────────────────────────
    //
    // The defect these close: the recognition point had an EDITION column and no OPERAND column, so SEVEN
    // directive lines that violate their own printed general format compiled in silence (>>SOURCE FORMAT
    // UNKNOWN, >>LISTING GARBAGE, >>PUSH/>>POP GARBAGE, >>DISPLAY and >>CALL-CONVENTION with no operand) while
    // six other stages each wrote the same rule again with a diagnostic code of its own. The check is now data
    // on the row; these tests are what keeps it honest — a partition assertion, a WITNESS that the rule really
    // fires through the real stage, and its complement, because a checker that rejects everything would pass a
    // witness-only test (feedback_measure_the_selectors_complement).

    /// <summary>Every directive row declares HOW its operand is checked, and there is no fourth state. A row
    /// that says a stage owns the operand names a stage that exists; a row that says the content is unchecked
    /// says WHY, in a citation — an unchecked operand is a decision on the record, never a silence.</summary>
    [Fact]
    public void EveryDirectiveRow_DeclaresItsOperandSyntax()
    {
        var bad = new List<string>();
        foreach (var row in ConstructRegistry.Entries.Where(e => e.DirectiveWords.Count > 0))
        {
            if (row.DirectiveOperand is not { } s) { bad.Add($"{row.Id}: no directiveOperand"); continue; }
            if (string.IsNullOrWhiteSpace(s.Citation)) bad.Add($"{row.Id}: directiveOperand has no citation");
            switch (s.Form)
            {
                case DirectiveOperandForm.Words when s.Choice.Count == 0 && !s.DirectiveName:
                    bad.Add($"{row.Id}: a words operand admits nothing"); break;
                case DirectiveOperandForm.Stage when
                    typeof(ConditionalCompilationProcessor).Assembly.GetTypes()
                        .All(t => t.Name != s.Owner):
                    bad.Add($"{row.Id}: operand owner '{s.Owner}' is not a type in Cobol.Net.Frontend"); break;
            }
        }

        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    /// <summary>
    /// THE witness, derived from the catalog rather than listed: for every directive whose operand the row
    /// declares as a closed word set, a malformed operand DRIVEN THROUGH THE REAL STAGE draws COBOLNET1911.
    /// The malformed spelling is derived too — a word the set does not admit, or a literal where the format
    /// writes a name, or nothing where the format requires an operand — so the next directive gets its witness
    /// from its row, with no test edit.
    /// </summary>
    [Fact]
    public void EveryClosedOperandDirective_DiagnosesAMalformedOperand()
    {
        var silent = new List<string>();
        var exempt = new List<string>();
        // Stage-owned operands are a different mechanism with their own per-stage codes (0718 TURN, 1622 FLAG,
        // 1623 COBOL-WORDS, 1619 the conditional-compilation expressions) — not this producer's subject, EXCEPT for
        // PRESENCE: a row that requires an operand (DISPLAY's, §7.3.12.2) has it checked here whoever reads the
        // content (kb/Work PB807).
        foreach (var row in ConstructRegistry.Entries
                     .Where(e => e.DirectiveOperand is { } o && (o.Form != DirectiveOperandForm.Stage || o.OperandRequired)))
        {
            string? malformed = MalformedOperandOf(row.DirectiveOperand!);
            if (malformed is null) { exempt.Add(row.Id); continue; }
            if (!Diagnose(row.DirectiveWords[0], malformed, row.IntroducedIn)
                    .Any(d => d.Code == "COBOLNET1911"))
                silent.Add($"{row.DirectiveWords[0]} {malformed}".Trim());
        }

        Assert.True(silent.Count == 0,
            $"these directive lines violate their own general format and are accepted in silence: "
            + $"[{string.Join(", ", silent)}] — the PB794 under-rejection has returned");
        // The exemptions are NAMED, so growing the set is a visible edit rather than a quiet one.
        Assert.Equal(["flag-85-directive-window", "flag-native-arithmetic-directive-window", "page-directive-2002"],
            exempt.Order(StringComparer.Ordinal));
    }

    /// <summary>The operand spelling a row's general format does not admit, derived from the row: a word the closed
    /// set does not admit, or nothing where the format requires an operand; null when no malformed operand exists
    /// (PAGE's comment-text-1, §7.3.19.3 SR2, and the two removed FLAG windows).</summary>
    private static string? MalformedOperandOf(DirectiveOperandSyntax s) => s.Form switch
    {
        // A word the set does not admit — for >>CALL-CONVENTION that is an undefined call-convention-name,
        // which names no convention WiseOwl COBOL defines (kb/Work PB1383; DOC-A.1-68).
        DirectiveOperandForm.Words => "ZZBOGUS",
        _ when s.OperandRequired => "",
        _ => null,
    };

    /// <summary>ISO §7.2.1 (kb/Work PB1363): "compiler directives" shall be "syntactically correct in the initial source
    /// text and library text", and the false path of an IF directive is part of it — so the operand column is a
    /// property of the directive line, not of the branch the line is in. Derived from the same rows as the test above:
    /// each malformed operand, written inside an OMITTED branch, still draws COBOLNET1911, and each conforming one
    /// there still draws nothing.</summary>
    [Fact]
    public void EveryClosedOperandDirective_IsCheckedInAnOmittedBranchToo()
    {
        var silent = new List<string>();
        var rejected = new List<string>();
        foreach (var row in ConstructRegistry.Entries
                     .Where(e => e.DirectiveOperand is { } o && (o.Form != DirectiveOperandForm.Stage || o.OperandRequired)))
        {
            var s = row.DirectiveOperand!;
            string word = row.DirectiveWords[0];
            if (MalformedOperandOf(s) is { } malformed
                && !Diagnose(word, malformed, row.IntroducedIn, omittedBranch: true).Any(d => d.Code == "COBOLNET1911"))
                silent.Add($"{word} {malformed}".Trim());
            string conforming = s.Form switch
            {
                DirectiveOperandForm.Words when s.Choice.Count > 0 => s.Choice[0],
                DirectiveOperandForm.Words when s.DirectiveName => "LISTING",
                DirectiveOperandForm.Text when s.OperandRequired => "\"x\"",
                _ => "",
            };
            if (s.Form != DirectiveOperandForm.Stage
                && Diagnose(word, conforming, row.IntroducedIn, omittedBranch: true).Any(d => d.Code == "COBOLNET1911"))
                rejected.Add($"{word} {conforming}".Trim());
        }

        Assert.True(silent.Count == 0,
            $"these directive lines are accepted in silence inside an omitted branch: [{string.Join(", ", silent)}] (ISO §7.2.1)");
        Assert.True(rejected.Count == 0,
            $"these conforming directive lines are rejected inside an omitted branch: [{string.Join(", ", rejected)}]");
    }

    /// <summary>The complement: every directive's own CONFORMING operand — the first alternative its general
    /// format admits — passes. A checker that rejected everything would satisfy the witness test above and
    /// reject legal source everywhere, which is the worse defect of the two.</summary>
    [Fact]
    public void EveryClosedOperandDirective_AcceptsAConformingOperand()
    {
        var rejected = new List<string>();
        foreach (var row in ConstructRegistry.Entries
                     .Where(e => e.DirectiveOperand is { Form: not DirectiveOperandForm.Stage }))
        {
            var s = row.DirectiveOperand!;
            string operand = s.Form switch
            {
                DirectiveOperandForm.Words when s.Choice.Count > 0 => s.Choice[0],
                DirectiveOperandForm.Words when s.DirectiveName => "LISTING",
                DirectiveOperandForm.Text when s.OperandRequired => "\"x\"",
                _ => "",
            };
            foreach (var d in Diagnose(row.DirectiveWords[0], operand, row.IntroducedIn))
                if (d.Code == "COBOLNET1911")
                    rejected.Add($"{row.DirectiveWords[0]} {operand}".Trim() + $" → {d.Message}");

            // And the omissible half, from the same column: where the printed format leaves an alternative
            // un-underlined (§5.2.3), writing nothing selects it and shall not be diagnosed.
            if (s is { Form: DirectiveOperandForm.Words, ChoiceOmissible: true }
                && Diagnose(row.DirectiveWords[0], "", row.IntroducedIn).Any(d => d.Code == "COBOLNET1911"))
                rejected.Add($">>{row.DirectiveWords[0]} with the omitted phrase implied");
        }

        Assert.True(rejected.Count == 0, string.Join("\n", rejected));
    }

    /// <summary>kb/Work PB806 — a row is a directive FAMILY, and a word of it that writes no operand
    /// (<see cref="DirectiveOperandSyntax.NoOperandWords"/>) is checked centrally whatever the row's Form says about
    /// its other words. Derived from the rows, so the next multi-word directive that declares an operand-less word is
    /// witnessed with no test edit: the word is a word OF ITS ROW, an operand after it draws COBOLNET1911 through the
    /// real stage, and its complement (the bare word, and the word with an inline comment) does not.</summary>
    [Fact]
    public void EveryNoOperandWord_RejectsAnOperandAndAcceptsTheBareWord()
    {
        var bad = new List<string>();
        int checkedWords = 0;
        foreach (var row in ConstructRegistry.Entries.Where(e => e.DirectiveOperand is { NoOperandWords.Count: > 0 }))
            foreach (string word in row.DirectiveOperand!.NoOperandWords)
            {
                checkedWords++;
                if (!row.DirectiveWords.Contains(word, StringComparer.OrdinalIgnoreCase))
                    bad.Add($"{row.Id}: noOperandWords names '{word}', which is not one of the row's directive words");
                if (!Diagnose(word, "ZZJUNK", row.IntroducedIn).Any(d => d.Code == "COBOLNET1911"))
                    bad.Add($">>{word} ZZJUNK is accepted in silence (ISO §7.3.3 SR3/SR4)");
                if (Diagnose(word, "", row.IntroducedIn).Any(d => d.Code == "COBOLNET1911")
                    || Diagnose(word, "*> why", row.IntroducedIn).Any(d => d.Code == "COBOLNET1911"))
                    bad.Add($">>{word} bare, or with an inline comment, is rejected — §7.3.3 SR3/SR4 allow both");
            }

        Assert.True(checkedWords >= 3, "the IF and EVALUATE rows declare ELSE, END-IF and END-EVALUATE operand-less");
        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    /// <summary>§7.3.3 SR3/SR4 — a directive "may be followed only by space characters and an optional inline
    /// comment". Six stages sliced their own operand and none of them knew that, so <c>&gt;&gt;PROPAGATE ON
    /// *&gt; on</c> was REJECTED and <c>&gt;&gt;SOURCE FORMAT FIXED *&gt; switch</c> was not recognized at all —
    /// the following segment was then read in the wrong reference format (kb/Work PB794).</summary>
    [Fact]
    public void InlineComment_IsNotPartOfTheOperand()
    {
        foreach (string line in (string[])["LISTING ON", "PROPAGATE ON", "LEAP-SECOND ON",
                                           "REF-MOD-ZERO-LENGTH ON", "PUSH ALL", "CALL-CONVENTION COBOL"])
        {
            string word = line.Split(' ')[0];
            var diags = Diagnose(word, line[(word.Length + 1)..] + "   *> why", 2023);
            Assert.DoesNotContain(diags, d => d.Code == "COBOLNET1911");
        }

        // The reference-format stage is the one that consumes its own line, so it is checked through its own
        // stage: the directive is RECOGNIZED with a comment on it, and the following segment switches.
        var bag = new DiagnosticBag();
        string free = ReferenceFormatProcessor.NormalizeToFreeForm(
            ">>SOURCE FORMAT IS FREE *> switch\nIDENTIFICATION DIVISION.\n", new ReferenceFormatDiagnostics(2023, false, bag), "t.cob");
        Assert.DoesNotContain(bag.Diagnostics, d => d.Code == "COBOLNET1911");
        Assert.DoesNotContain(">>SOURCE", free);   // consumed, so the parser never sees it

        // …and the quote-aware half: the *> inside a literal is data (§8.3.3.1), not a comment.
        Assert.True(CompilerDirectiveLine.TryParse(">>DISPLAY \"a *> b\"", out var d2));
        Assert.Equal("\"a *> b\"", d2.Operand);
        Assert.Equal("DISPLAY", d2.Word);
        // §7.3.3 SR5: the space after the indicator is optional, and its absence is treated as though present.
        Assert.True(CompilerDirectiveLine.TryParse(">> SOURCE FORMAT IS FIXED", "SOURCE", out string op));
        Assert.Equal("FORMAT IS FIXED", op);
    }

    /// <summary>A malformed <c>&gt;&gt;SOURCE FORMAT</c> is diagnosed AND consumed: the line is a directive line
    /// by its word, so leaving it in the text (which is what produced <c>COBOL0001: unexpected '&gt;'</c> before
    /// kb/Work PB725) is not the recovery. The reference format in effect is carried on unchanged, because the
    /// directive selected none.</summary>
    [Fact]
    public void MalformedSourceFormat_IsDiagnosedAndConsumed()
    {
        foreach (string operand in (string[])["UNKNOWN", "\"literal\"", "", "FORMAT"])
        {
            var bag = new DiagnosticBag();
            string outp = ReferenceFormatProcessor.NormalizeToFreeForm(
                $">>SOURCE FORMAT {operand}\nIDENTIFICATION DIVISION.\n", new ReferenceFormatDiagnostics(2023, false, bag), "t.cob");
            Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET1911");
            Assert.DoesNotContain(">>SOURCE", outp);
        }
    }

    /// <summary>Drive the REAL text-manipulation stage with one directive line and return what it reported.
    /// The edition is the directive's own introducing one, so the introduction gate never fires and what is
    /// measured is the operand check alone.</summary>
    private static IReadOnlyList<Diagnostic> Diagnose(string word, string operand, int edition, bool omittedBranch = false)
    {
        var bag = new DiagnosticBag();
        string line = $">>{word} {operand}".TrimEnd();
        // The line is in the false path of an IF directive (ISO §7.2.1). The reference-format stage reads every branch
        // (§7.2.1 Step 1: SOURCE FORMAT in a false path is processed, kb/Work PB1006) and is fixed form until a directive
        // says otherwise, so its case first switches to free form to make the wrapper's own `>>` lines recognizable.
        if (omittedBranch)
            line = (word == "SOURCE" ? ">>SOURCE FORMAT IS FREE\n" : "") + $">>IF 1 = 2\n{line}\n>>END-IF";
        if (word == "SOURCE")
            ReferenceFormatProcessor.NormalizeToFreeForm(
                line + "\nIDENTIFICATION DIVISION.\n", new ReferenceFormatDiagnostics(edition, false, bag), "t.cob");
        else
            ConditionalCompilationProcessor.Process(
                line + "\nIDENTIFICATION DIVISION.\n", CobolNet.Frontend.Frontend.LeftDirectives, bag, "t.cob",
                dialectLevel: edition);
        return bag.Diagnostics;
    }

    /// <summary>An UNRECOGNIZED <c>&gt;&gt;</c> word is not swallowed: it survives into the text so the parser
    /// names it, which is what catches a typo like <c>&gt;&gt;IFF</c>. The gate must not turn every unknown word
    /// into a consumed no-op.</summary>
    [Fact]
    public void UnrecognizedDirective_SurvivesForTheParser()
    {
        var bag = new DiagnosticBag();
        string outp = ConditionalCompilationProcessor.Process(
            ">>IFF X\nIDENTIFICATION DIVISION.\n", CobolNet.Frontend.Frontend.LeftDirectives, bag, "t.cob",
            dialectLevel: 2023);
        Assert.Contains(">>IFF", outp);
        Assert.DoesNotContain(bag.Diagnostics, d => d.Code == "COBOLNET0900");
    }
}
