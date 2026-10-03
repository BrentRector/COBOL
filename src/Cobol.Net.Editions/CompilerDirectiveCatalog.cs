// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// THE roster of ISO §7.3 compiler directives, keyed by the compiler-directive word that heads the line —
/// DERIVED from <see cref="ConstructRegistry.Entries"/>, never hand-written (kb/Work PB725).
///
/// <para>ISO/IEC 1989:2023 §7.3.2 gives one general format for every directive — <c>&gt;&gt;compiler-instruction</c>
/// — and §7.3.3 SR6 says compiler-instruction opens with a compiler-directive word (§8.12). So "which word may
/// head a <c>&gt;&gt;</c> line" and "from which edition" is ONE fact per directive, and it belongs in ONE table.
/// Before PB725 it lived in four places at once: a flat <c>KnownIgnoredDirectives</c> HashSet with no edition
/// column (11 words silently accepted at <c>--std 85</c>, where COBOL has no compiler directives at all), the
/// conditional-compilation switch's own arms (7 more), two hand-rolled <c>if (dialectLevel &lt; 2002)</c> tests
/// with bespoke diagnostic codes, and the five rows that did it correctly through
/// <see cref="ConstructRegistry.Check"/>. That is <c>feedback_one_mechanism_per_job</c> failing four ways on one
/// rule, and it is why half the §E.2 item 5 family was gated and half was not.</para>
///
/// <para>Now a directive is ONE <c>tests/version-matrix/constructs.json</c> row carrying
/// <c>directiveWords</c> plus a regen: the word becomes recognized, its introducing edition becomes enforced by
/// the one COBOLNET0900 producer, and the version matrix compiles its <c>source</c> at all four editions
/// automatically. <c>CompilerDirectiveCatalogDriftTests</c> keeps the roster honest against §7.3 itself.</para>
/// </summary>
public static class CompilerDirectiveCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, ConstructDialectStatus>> Map = new(() =>
    {
        var map = new Dictionary<string, ConstructDialectStatus>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in ConstructRegistry.Entries)
            foreach (string w in e.DirectiveWords)
                if (!map.TryAdd(w, e))
                    throw new InvalidOperationException(
                        $"constructs.json: directive word '{w}' is claimed by both '{map[w].Id}' and '{e.Id}' — "
                        + "one word, one row (ISO §7.3.3 SR6: a compiler-instruction opens with ONE "
                        + "compiler-directive word)");
        return map;
    });

    /// <summary>Every compiler-directive word the compiler recognizes, upper-case, ordered.</summary>
    public static IEnumerable<string> Words => Map.Value.Keys.Order(StringComparer.Ordinal);

    /// <summary>The registry row gating <paramref name="word"/>, or <see langword="null"/> when the word heads no
    /// recognized directive — an unrecognized <c>&gt;&gt;</c> word is LEFT IN the text so it surfaces downstream
    /// as a syntax error (catching typos like <c>&gt;&gt;IFF</c>) rather than being silently swallowed.</summary>
    public static ConstructDialectStatus? Find(string word) => Map.Value.GetValueOrDefault(word);

    /// <summary>True when <paramref name="word"/> heads a recognized ISO §7.3 compiler directive.</summary>
    public static bool IsDirective(string word) => Map.Value.ContainsKey(word);

    /// <summary>
    /// The ONE edition gate for a compiler-directive line (§7.3.3 SR6 + §7.3.2): reject <paramref name="word"/>
    /// below its introducing edition and after its removing one, through
    /// <see cref="ConstructRegistry.Check"/> — COBOLNET0900 for the introduction edge (error on both severity
    /// axes: no conforming source of the older edition can contain the line), COBOLNET0902 for a removal,
    /// COBOLNET0903 for an obsolete use. A no-op for an unrecognized word and for every edition at which the
    /// directive is available.
    /// </summary>
    public static void Check(string word, EditionInfo edition, IDiagnosticSink sink)
    {
        if (Map.Value.GetValueOrDefault(word) is { } row)
            ConstructRegistry.Check(edition, sink, row.Id, row.Display);
    }

    /// <summary>
    /// THE operand check for a compiler-directive line (kb/Work PB794): §7.3.3 SR6 composes compiler-instruction
    /// "as specified in the syntax of each directive", and for the ten directives whose syntax is a closed word
    /// set that specification is DATA on the row — <see cref="ConstructDialectStatus.DirectiveOperand"/>. One
    /// producer, <c>COBOLNET1911</c>, for the whole family; before PB794 seven directive lines that violate their
    /// own printed general format were accepted in silence while six stages each wrote the rule again with a code
    /// of its own.
    ///
    /// <para>A no-op when the word heads no recognized directive (the parser names it), when a downstream stage
    /// owns the operand (<see cref="DirectiveOperandForm.Stage"/>), and — deliberately — when the directive is not
    /// available at <paramref name="edition"/>: <see cref="Check"/> has already said the line may not appear at
    /// all, and a second complaint about the operand of a directive this edition does not have adds noise, not
    /// information.</para>
    /// </summary>
    /// <param name="word">The compiler-directive word, from <see cref="CompilerDirectiveLine"/>.</param>
    /// <param name="operand">The operand text — trimmed, inline comment already removed by that same parse.</param>
    public static void CheckOperand(string word, string operand, EditionInfo edition, IDiagnosticSink sink)
    {
        if (Map.Value.GetValueOrDefault(word) is not { } row) return;
        if (row.StatusAt(edition.Year) is ConstructAvailability.NotYetIntroduced or ConstructAvailability.Removed)
            return;
        if (row.DirectiveOperand is not { } syntax) return;
        // A word of a multi-word row whose own format writes no operand (kb/Work PB806): §7.3.3 SR3/SR4 — only spaces
        // and an optional inline comment may follow the directive, so ANY operand word is a violation, whatever the
        // row's Form says about the row's other words.
        if (operand.Length > 0 && syntax.NoOperandWords.Contains(word, StringComparer.OrdinalIgnoreCase))
        {
            sink.Report(new EditionDiagnostic(
                Diagnostics.DiagnosticCatalog.DirectiveMalformedOperand.Code, EditionSeverity.Error, row.Id,
                $">>{word.ToUpperInvariant()} is malformed: '{operand}' follows it, but the general format writes no operand "
                + "and only space characters and an optional inline comment may follow a compiler directive "
                + "(ISO §7.3.3 SR3/SR4; text-1 and text-2 begin on a new line)",
                row.Display, syntax.Citation));
            return;
        }
        // PRESENCE is the format's own fact and is checked here whoever reads the content (kb/Work PB807): a Text row
        // checks nothing more, a Stage row's owner checks the rest, a Words row checks the closed set.
        string? complaint = syntax.Form == DirectiveOperandForm.Words
            ? CheckWords(syntax, operand, edition)
            : syntax.OperandRequired && operand.Length == 0
                ? "the operand is required and none is written"
                : null;
        if (complaint is null) return;

        sink.Report(new EditionDiagnostic(
            Diagnostics.DiagnosticCatalog.DirectiveMalformedOperand.Code, EditionSeverity.Error, row.Id,
            $"{row.Display} is malformed: {complaint} — the general format admits "
            + $"{syntax.Admissible(syntax.DirectiveName ? OperandDirectiveNames(syntax, edition) : null)} ({syntax.Citation})",
            row.Display, syntax.Citation));
    }

    /// <summary>
    /// The single operand WORD of a closed-word-set directive, upper-cased, with the general format's OPTIONAL
    /// words (§5.2.3) dropped — the value the owning stage acts on. <c>&gt;&gt;SOURCE FORMAT IS FREE</c> and
    /// <c>&gt;&gt;SOURCE FREE</c> both yield <c>FREE</c>; a directive whose choice is omissible yields the empty
    /// string, which is the implied alternative (a bare <c>&gt;&gt;LEAP-SECOND</c> selects ON).
    ///
    /// <para>It exists so the optional-word list lives ONLY on the row: before kb/Work PB794 the
    /// reference-format stage carried <c>(?:FORMAT\s+)?(?:IS\s+)?</c> in a regex of its own, which is how a
    /// legal <c>&gt;&gt;SOURCE FORMAT FIXED *&gt; switch</c> came to be unrecognized. Returns false when the word
    /// heads no closed-word-set directive or the operand is not a single word — in which case
    /// <see cref="CheckOperand"/> has already said so and the caller applies no state change.</para>
    /// </summary>
    public static bool TryOperandWord(string word, string operand, out string operandWord)
    {
        operandWord = "";
        if (Map.Value.GetValueOrDefault(word) is not { DirectiveOperand: { Form: DirectiveOperandForm.Words } s })
            return false;
        var words = SignificantWords(s, operand);
        if (words.Count > 1) return false;
        operandWord = words.Count == 1 ? words[0].ToUpperInvariant() : "";
        return true;
    }

    /// <summary>The operand's words with the general format's OPTIONAL words dropped — §5.2.3: an optional word
    /// "may be written to add clarity", so it may stand anywhere the format prints it and means nothing when it
    /// does. ONE place, because <see cref="TryOperandWord"/> (what the owning stage acts on) and
    /// <see cref="CheckWords"/> (what the diagnostic screens) shall never disagree about which words count.</summary>
    private static List<string> SignificantWords(DirectiveOperandSyntax syntax, string operand) =>
        [.. operand.SplitSpaces()
                   .Where(w => !syntax.OptionalWords.Contains(w, StringComparer.OrdinalIgnoreCase))];

    /// <summary>The closed-word-set arm of <see cref="CheckOperand"/>: the optional words (§5.2.3) may be written
    /// anywhere and are ignored; what remains shall be exactly one admissible word, or nothing when the general
    /// format leaves an alternative un-underlined. Returns null when the operand conforms.</summary>
    private static string? CheckWords(DirectiveOperandSyntax syntax, string operand, EditionInfo edition)
    {
        var words = SignificantWords(syntax, operand);
        if (words.Count == 0)
            return syntax.ChoiceOmissible ? null : "no operand is written";
        if (words.Count > 1)
            return $"'{string.Join(' ', words)}' is more than one operand";

        string w = words[0];
        if (syntax.Choice.Contains(w, StringComparer.OrdinalIgnoreCase)) return null;
        if (syntax.DirectiveName && OperandDirectiveNames(syntax, edition).Contains(w, StringComparer.OrdinalIgnoreCase))
            return null;
        if (syntax.UserWord && IsCobolWord(w)) return null;
        return $"'{w}' is not an admissible operand";
    }

    /// <summary>The compiler-directive names §7.3.20.2 / §7.3.22.2 admit as <c>directive-name</c>, DERIVED from
    /// the catalog: every recognized directive word except those §7.3.20.3 SR1 / §7.3.22.3 SR1 exclude. The
    /// exclusion names a DIRECTIVE, so excluding EVALUATE excludes its whole row — a <c>&gt;&gt;PUSH WHEN</c>
    /// names the EVALUATE directive by one of its own words and is excluded with it. A directive-name names a
    /// directive of the targeted EDITION (kb/Work PB1377): FLAG-85 and FLAG-NATIVE-ARITHMETIC were removed at 2023
    /// (Annex E.2 item 21), the only edition with PUSH and POP, so neither is a compiler directive there and neither
    /// is admissible — the rows available at the edition ARE the set, so a removed or not-yet-introduced row drops
    /// out by itself.</summary>
    private static IReadOnlyList<string> OperandDirectiveNames(DirectiveOperandSyntax syntax, EditionInfo edition)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in syntax.ExcludedDirectives)
            foreach (string w in (Find(name)?.DirectiveWords ?? [name])) excluded.Add(w);   // the whole ROW, by one of its words
        return [.. Words.Where(w => !excluded.Contains(w)
                                    && Find(w)!.StatusAt(edition.Year) is not (ConstructAvailability.NotYetIntroduced
                                                                               or ConstructAvailability.Removed))];
    }

    /// <summary>A COBOL word (§8.3.2): basic letters, digits, hyphen and underscore, not a literal and not
    /// punctuation. The operand positions that admit an implementor-defined name accept one of these and
    /// nothing else — <c>&gt;&gt;CALL-CONVENTION "COBOL"</c> writes a literal where the format writes a name.</summary>
    private static bool IsCobolWord(string w)
    {
        foreach (char c in w) if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_') return false;
        return w.Length > 0 && w[0] != '-' && w[^1] != '-';
    }

    /// <summary>The row ids of every directive whose STATE a <c>&gt;&gt;PUSH ALL</c> saves — ISO §7.3.22.4 GR2:
    /// "the state of all of the directives other than EVALUATE, IF, PAGE, POP, or PUSH are saved" (kb/Work PB941).
    /// DERIVED, never hand-written: the catalog's rows minus the PUSH row's own
    /// <see cref="DirectiveOperandSyntax.ExcludedDirectives"/> (the §7.3.22.3 SR1 list, which is the same five), so a
    /// new directive row joins the pushed set by existing. Ordered by row id.</summary>
    public static IReadOnlyList<string> PushableRows => PushableRowsLazy.Value;

    private static readonly Lazy<IReadOnlyList<string>> PushableRowsLazy = new(() =>
    {
        var push = Find("PUSH")?.DirectiveOperand
            ?? throw new InvalidOperationException("constructs.json: no PUSH directive row with a directiveOperand");
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in push.ExcludedDirectives)
            if (Find(name) is { } row) excluded.Add(row.Id);
        return [.. Map.Value.Values.Select(r => r.Id).Distinct().Where(id => !excluded.Contains(id))
                   .Order(StringComparer.Ordinal)];
    });

    /// <summary>The row id of the directive <paramref name="directiveName"/> names when it is a PUSH/POP
    /// <c>directive-name</c> operand (§7.3.20.2 / §7.3.22.2) — null when the word heads no directive or names one
    /// §7.3.20.3 SR1 / §7.3.22.3 SR1 exclude.</summary>
    public static string? PushableRowOf(string directiveName) =>
        Find(directiveName) is { } row && PushableRows.Contains(row.Id, StringComparer.Ordinal) ? row.Id : null;

    /// <summary>
    /// The same gate addressed by ROW ID rather than by word — for the one stage that must gate a directive it
    /// consumes before the shared recognition point ever sees it (<c>&gt;&gt;SOURCE FORMAT</c>, whose line the
    /// reference-format normalizer removes so it can switch the following segment's format). Keyed on
    /// <c>Constructs.X</c> so an unregistered id is a COMPILE error, never a string that quietly stops matching.
    /// </summary>
    public static void CheckRow(string constructId, EditionInfo edition, IDiagnosticSink sink)
    {
        var row = ConstructRegistry.Find(constructId)
            ?? throw new ArgumentException($"unregistered construct id '{constructId}'", nameof(constructId));
        ConstructRegistry.Check(edition, sink, row.Id, row.Display);
    }
}
