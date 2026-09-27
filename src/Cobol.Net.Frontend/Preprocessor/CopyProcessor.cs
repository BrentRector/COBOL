// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// Handles COPY statement preprocessing. COPY inserts the contents of a copybook
/// (library text) into the source before lexing. Supports COPY ... REPLACING.
/// </summary>
public sealed class CopyProcessor(
    IEnumerable<string>? searchPaths = null,
    DiagnosticBag? diagnostics = null,
    string sourceName = "<source>",
    bool strict = false,
    int dialectLevel = 85,
    bool permissive = false,
    CompilationInputs? inputs = null)
{
    /// <summary>The compilation's ambient-input gateway (kb/Work PB985): every copybook probe and read below goes
    /// through it, so the record names each library text the group incorporated AND each candidate that was not
    /// there. A caller that passes none (the legacy oracle, the standalone preprocess CLI) gets a private one.</summary>
    private readonly CompilationInputs _inputs = inputs ?? new CompilationInputs();

    // One COBOLNET0902 per compilation for the VCR-row-4 gate (COPY REPLACING non-pseudo-text, W3 — DEVLOG 598).
    private bool _nonPseudoTextFlagged;

    /// <summary>The VCR-row-4 gate: non-pseudo-text COPY REPLACING operands (identifier/literal/word forms)
    /// were REMOVED by ISO 2023 (Annex E.2 item 1 bullet 4 — "Removal of support for non-pseudo-text operands
    /// in the replacing phrase of the COPY statement"). Error strict / warning permissive at ≥2023, silent
    /// below (the ==pseudo-text== form is the only 2023-conforming shape); the pre-removal substitution
    /// semantics are preserved either way. The strict/permissive decision is the ONE
    /// <see cref="EditionSeverityPolicy"/> (P2.9 — never a local <c>if(permissive)</c>).</summary>
    private void OnNonPseudoTextOperand(MappedText mapped, int pos)
    {
        if (dialectLevel < 2023 || _nonPseudoTextFlagged || _diagnostics is null) return;
        _nonPseudoTextFlagged = true;
        var at = mapped.OriginAt(pos);   // the SOURCE origin (kb/Work PB82)
        int line = at.Line;
        const string msg = "a non-pseudo-text COPY REPLACING operand (identifier/literal/word) was removed in "
            + "COBOL-2023 (Annex E.2 item 1 bullet 4) — use ==pseudo-text==; first use at line ";
        var loc = at.ToLocation();
        var severity = EditionSeverityPolicy.For(ConstructAvailability.Removed, EditionInfo.Of(dialectLevel, permissive));
        if (severity == EditionSeverity.Error)
            _diagnostics.ReportError("COBOLNET0902", msg + line, loc, default);
        else
            _diagnostics.ReportWarning("COBOLNET0902", msg + line, loc, default);
    }
    /// <summary>Maximum COPY nesting depth to prevent infinite recursion.</summary>
    private const int MaxCopyDepth = 20;

    /// <summary>File extensions to try when searching for copybooks.</summary>
    private static readonly string[] CopybookExtensions = ["", ".cpy", ".cob", ".cbl", ".CPY", ".COB", ".CBL"];

    private readonly List<string> _searchPaths = new(searchPaths ?? []);

    // Diagnostic plumbing (DEVLOG 307). Optional so the standalone preprocess CLI and tests can construct
    // a CopyProcessor without a bag; when absent, behavior is unchanged (silent). `strict` gates the
    // missing-copybook error to named-strict dialects so the permissive Default/--nist path is unaffected.
    private readonly DiagnosticBag? _diagnostics = diagnostics;
    private readonly string _sourceName = sourceName;
    private readonly bool _strict = strict;

    /// <summary>Add a directory to search for copybooks.</summary>
    public void AddSearchPath(string path) => _searchPaths.Add(path);

    /// <summary>Ensure the source's own directory is searched FIRST (the <see cref="Process"/> setup, ISO §7.2.3)
    /// — used by the merged CC+COPY driver, which calls <see cref="ExpandCopiesOneLevel"/> directly.</summary>
    internal void RegisterSourceDir(string sourceDir)
    {
        if (!_searchPaths.Contains(sourceDir)) _searchPaths.Insert(0, sourceDir);
    }

    /// <summary>Report at a SOURCE origin (kb/Work PB82) — the file and physical line the text at a position came
    /// from, never an ordinal of the text being processed.</summary>
    private void Report(DiagnosticDescriptor descriptor, SourceOrigin at, params object[] args)
        => _diagnostics?.Report(descriptor, at.ToLocation(), TextSpan.Empty, args);

    /// <summary>
    /// Process all COPY and REPLACE statements in the source text.
    /// Returns the expanded source text with COPY expanded and REPLACE applied.
    /// </summary>
    public string Process(string sourceText, string sourceDir)
    {
        if (!_searchPaths.Contains(sourceDir))
            _searchPaths.Insert(0, sourceDir);

        string expanded = ExpandCopyStatements(sourceText, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        return ApplyReplaceStatements(expanded, _diagnostics, _sourceName);
    }

    /// <summary>
    /// Process REPLACE statements: REPLACE ==pseudo-text-1== BY ==pseudo-text-2==.
    /// REPLACE OFF turns off active replacements.
    /// A non-pseudo-text operand (kb/Work R39 — the GCOS/ACU literal spelling) draws COBOLNET1641 when a
    /// <paramref name="diagnostics"/> bag is supplied: REPLACE's operands were never literals in ANY ISO
    /// edition (§7.2.4.2 general format; §7.2.4.3 SR7), unlike COPY's, whose pre-2023 literal forms ride the
    /// separate COBOLNET0902 removal gate. Before this the illegal statement was silently half-parsed and the
    /// failure surfaced downstream as an unrelated undefined-reference.
    /// </summary>
    internal static string ApplyReplaceStatements(string text, DiagnosticBag? diagnostics = null,
        string sourceName = "<source>")
        => ApplyReplaceStatements(MappedText.Identity(text, sourceName), diagnostics, sourceName).Text;

    /// <summary>The MAPPED REPLACE pass (kb/Work PB82): a REPLACE statement's own lines vanish from the resultant
    /// text, and a replacement may change a line count — the kept text keeps its origins, a replacement's lines take
    /// the origin of the line its match started on.</summary>
    internal static MappedText ApplyReplaceStatements(MappedText mapped, DiagnosticBag? diagnostics = null,
        string sourceName = "<source>")
    {
        string text = mapped.Text;
        var w = new OriginWriter();
        var activeReplacements = new List<Replacement>();
        int pos = 0;

        while (pos < text.Length)
        {
            int replaceIdx = FindKeywordAtLineStart(text, pos, "REPLACE");
            if (replaceIdx < 0)
            {
                w.AppendMapped(ApplyReplacements(mapped.Slice(pos, text.Length - pos), activeReplacements));
                break;
            }

            w.AppendMapped(ApplyReplacements(mapped.Slice(pos, replaceIdx - pos), activeReplacements));

            // The statement is parsed over text-words (§7.2.4.2 general format) through its separator period — a
            // period INSIDE pseudo-text is a text-word of the operand, never the statement's end (kb/Work PB1354).
            var statement = new StatementCursor(text, replaceIdx + "REPLACE".Length, (p, message) =>
                diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.TextManipulationStatementSyntax.Code,
                    "REPLACE statement: " + message + " (§7.2.4.2)", mapped.OriginAt(p).ToLocation(), default));
            activeReplacements.Clear();
            if (statement.TryPeek(out var first) && first.IsWord("OFF"))
                statement.Advance(first);
            else
            {
                ParseReplacingOperands(statement, activeReplacements, nestedCopy: null, nonPseudoText: p =>
                {
                    if (statement.HasError || diagnostics is null) return;   // one report per REPLACE statement
                    statement.MarkError();
                    diagnostics.ReportError(
                        Editions.Diagnostics.DiagnosticCatalog.ReplaceOperandNotPseudoText.Code,
                        "a REPLACE statement operand is not pseudo-text — REPLACE admits ==pseudo-text== "
                        + "(and ==partial-word== under LEADING/TRAILING) operands only, in every ISO edition "
                        + "(§7.2.4.2; §7.2.4.3 SR7 bars literals as partial-words). Write ==operand== "
                        + "(empty ==== deletes)",
                        mapped.OriginAt(p).ToLocation(), default);
                });
            }
            statement.ExpectSeparatorPeriod();
            pos = statement.Pos;
        }

        return w.Finish();
    }

    /// <summary>Where each line of <paramref name="text"/> lands in the text <see cref="ApplyReplaceStatements(MappedText, DiagnosticBag?, string)"/>
    /// makes of it: element <c>i</c> is the 0-based RESULTANT line holding input line <c>i</c> (kb/Work PB1066). A
    /// kept line maps to itself shifted by the lines REPLACE removed above it; a line REPLACE removed (a REPLACE
    /// statement's own) or joined into a replacement maps to the resultant line where that text now starts — the
    /// replacement's first line, which takes the origin of the line its match started on. The identity when the
    /// text holds no REPLACE statement. The same pass run over line-index origins, without diagnostics, so it can
    /// never disagree with the real one.</summary>
    internal static int[] ReplaceLineMap(string text)
    {
        var inputOrigins = MappedText.Identity(text, "").Lines;   // origin Line = 1-based input line
        var map = new int[inputOrigins.Length];
        if (FindKeywordAtLineStart(text, 0, "REPLACE") < 0)
        {
            for (int i = 0; i < map.Length; i++) map[i] = i;
            return map;
        }
        var resultant = ApplyReplaceStatements(new MappedText(text, inputOrigins)).Lines;
        int j = 0;
        for (int i = 0; i < map.Length; i++)
        {
            while (j + 1 < resultant.Length && resultant[j + 1].Line - 1 <= i) j++;
            map[i] = j;
        }
        return map;
    }

    /// <summary>COPY/REPLACE replacement scope: a whole text-word sequence, or the LEADING/TRAILING characters of
    /// a single text-word (ISO §7.2.3.4 GR 9 b / §7.2.4.4 GR 8 b).</summary>
    private enum ReplaceKind { Whole, Leading, Trailing }

    /// <summary>One REPLACING-phrase operand pair of a COPY (§7.2.3) or REPLACE (§7.2.4) statement: pseudo-text-1 or
    /// partial-word-1 as the text-words it is compared by, and the replacement text exactly as written.</summary>
    private readonly record struct Replacement(IReadOnlyList<TextWord> From, string To, ReplaceKind Kind);

    /// <summary>
    /// Apply COPY REPLACING / REPLACE substitutions (ISO §7.2.3.4 GR 9 / §7.2.4.4 GR 8) over the §7.2.2.5 text-words
    /// of the text: at each leftmost text-word the operands are tried in the order written and the first match wins
    /// (d)); an unmatched word is copied (e)); after a match the word following the rightmost matched word is the new
    /// leftmost, so inserted text is never rescanned (f)). Two text-words compare by
    /// <see cref="TextWord.MatchesForReplacing"/> (c) 3.–4.). A match's source span is replaced verbatim by the
    /// replacement text, so white space and line breaks between the matched words vanish with them; the unmatched
    /// text keeps its origins (kb/Work PB82) and a replacement takes the origin of the line its match started on.
    /// </summary>
    private static MappedText ApplyReplacements(MappedText mapped, IReadOnlyList<Replacement> replacements)
    {
        var active = replacements.Where(r => r.From.Count > 0).ToList();   // an empty operand cannot match
        if (active.Count == 0) return mapped;

        string text = mapped.Text;
        var words = TextWordScanner.MatchWords(text);
        var sb = new OriginWriter();
        int copiedUpTo = 0; // chars of `text` already emitted
        int w = 0;
        while (w < words.Count)
        {
            bool matched = false;
            foreach (var (from, to, kind) in active)
            {
                if (kind == ReplaceKind.Whole)
                {
                    if (w + from.Count > words.Count) continue;
                    bool eq = true;
                    for (int k = 0; k < from.Count; k++)
                        if (!words[w + k].MatchesForReplacing(from[k])) { eq = false; break; }
                    if (!eq) continue;

                    int matchStart = words[w].Start;
                    int matchEnd = words[w + from.Count - 1].End;
                    sb.AppendSlice(mapped, copiedUpTo, matchStart - copiedUpTo);
                    sb.Append(to.AsSpan(), mapped.OriginAt(matchStart));
                    copiedUpTo = matchEnd;
                    w += from.Count;
                    matched = true;
                    break;
                }

                // LEADING / TRAILING (b)): partial-word-1 is equal "character for character" to the leftmost /
                // rightmost characters of ONE source text-word. The comparison is case-insensitive (c) 3.) without
                // exception: partial-word-1 is not a literal (§7.2.3.3 SR13 / §7.2.4.3 SR7), so it holds no quotation
                // symbol and can never reach the case-significant CONTENT of a literal text-word — a literal both
                // begins (after an optional prefix) and ends with its quotation symbol.
                var part = from[0].Span;
                var word = words[w].Span;
                bool leading = kind == ReplaceKind.Leading && word.StartsWith(part, StringComparison.OrdinalIgnoreCase);
                bool trailing = kind == ReplaceKind.Trailing && word.EndsWith(part, StringComparison.OrdinalIgnoreCase);
                if (!leading && !trailing) continue;

                sb.AppendSlice(mapped, copiedUpTo, words[w].Start - copiedUpTo);
                SourceOrigin at = mapped.OriginAt(words[w].Start);
                if (leading)
                {
                    sb.Append(to.AsSpan(), at);
                    sb.Append(word[part.Length..], at);
                }
                else // trailing
                {
                    sb.Append(word[..^part.Length], at);
                    sb.Append(to.AsSpan(), at);
                }
                copiedUpTo = words[w].End;
                w++;
                matched = true;
                break;
            }
            if (!matched) w++;
        }
        sb.AppendSlice(mapped, copiedUpTo, text.Length - copiedUpTo);
        return sb.Finish();
    }

    /// <summary>The recursive COPY-only expansion behind <see cref="Process"/> (the legacy compiler's path): the ONE
    /// one-level expander, <see cref="ExpandCopiesOneLevel"/>, fed with itself as the copybook expander.</summary>
    private string ExpandCopyStatements(string text, HashSet<string> alreadyIncluded, int depth)
        => ExpandCopiesOneLevel(text, alreadyIncluded, depth, (copybook, d) => ExpandCopyStatements(copybook, alreadyIncluded, d));

    /// <summary>The disposition of one resolved COPY statement.</summary>
    internal enum CopyOutcome { Found, NotFound, Circular }

    /// <summary>The result of resolving ONE COPY statement (non-recursive): the text to splice and, when
    /// <see cref="CopyOutcome.Found"/>, the copybook's path (so the caller removes it from the include set after
    /// recursing). <see cref="Text"/> is the copybook's NormalizeCopybook+ApplyReplacements text (Found) or the
    /// comment fallback (NotFound/Circular).</summary>
    /// <param name="Mapped">The incorporated library text with its origins (the copybook's path and physical lines,
    /// after normalization and COPY … REPLACING — kb/Work PB82); null for the not-found / circular comment lines.</param>
    internal readonly record struct OneCopyResult(CopyOutcome Outcome, string Text, string? CopybookPath, MappedText? Mapped = null);

    /// <summary>Parse and resolve ONE COPY statement whose keyword is at <paramref name="copyIdx"/> — parse it
    /// (<see cref="ParseCopyStatement"/>, advancing <paramref name="afterCopy"/> past its separator period), find the
    /// copybook, and return its NormalizeCopybook+ApplyReplacements text (NOT recursively expanded — the caller
    /// recurses). Shared by <see cref="ExpandCopyStatements"/> (legacy path) and <see cref="ExpandCopiesOneLevel"/>
    /// (the merged CC+COPY driver), so the two never diverge.</summary>
    internal OneCopyResult ResolveOneCopy(MappedText mapped, int copyIdx, HashSet<string> alreadyIncluded, out int afterCopy)
    {
        string text = mapped.Text;
        SourceOrigin at = mapped.OriginAt(copyIdx);   // the COPY statement's SOURCE origin (kb/Work PB82)

        // §7.2.3.3 SR2: "A COPY statement shall be preceded by a space except when it is the first statement in a
        // compilation group" — a COPY word directly after a parenthesis, a colon, a literal's closing delimiter or a
        // pseudo-text delimiter. (A COPY glued behind a period, comma or semicolon is not even a text-word of its
        // own — see FindCopyKeyword.)
        if (copyIdx > 0 && !char.IsWhiteSpace(text[copyIdx - 1]))
            ReportPlacement(at, $"COPY is not preceded by a space (it follows '{text[copyIdx - 1]}') — §7.2.3.3 SR2: "
                + "\"A COPY statement shall be preceded by a space except when it is the first statement in a "
                + "compilation group\"");

        var statement = ParseCopyStatement(mapped, copyIdx);
        afterCopy = statement.End;
        if (statement.TextName is null)
            return new OneCopyResult(CopyOutcome.NotFound, "*> COPY statement not processed — see its diagnostic", null);
        string libraryName = statement.TextName;

        string? copybookPath = FindCopybook(libraryName, statement.LibraryName);
        if (copybookPath == null)
        {
            // ISO §7.2.3.4 GR 2: library text shall be available. Hard error under named-strict dialects;
            // Default/--nist keep the lenient comment fallback (NIST safe).
            if (_strict)
                Report(DiagnosticDescriptors.CBL3620, at,
                    libraryName, string.Join("; ", _searchPaths));
            return new OneCopyResult(CopyOutcome.NotFound, $"*> COPY {libraryName} — copybook not found", null);
        }
        if (!alreadyIncluded.Add(copybookPath))
        {
            // ISO §7.2.3.4 GR12: "The library text being copied shall not cause the processing of a COPY statement
            // that directly or indirectly copies itself."
            Report(DiagnosticDescriptors.CBL3621, at, libraryName);
            return new OneCopyResult(CopyOutcome.Circular, $"*> COPY {libraryName} — circular include skipped", null);
        }

        // Library text is itself in reference (fixed) format — normalize to free form so inserted lines align in
        // the program's source area; then COPY … REPLACING (same text-word matching as REPLACE, ISO §7.2.4).
        var normalizedMapped = NormalizeCopybookMapped(_inputs.ReadAllText(copybookPath), copybookPath);
        string normalized = normalizedMapped.Text;
        // §7.2.3.4 GR10 (kb/Work R34): "If the REPLACING phrase is specified, the library text shall not
        // contain a COPY statement" — GR12 permits nesting only WITHOUT replacing. Before this check the
        // caller recursed into the spliced text OUTSIDE the replacement scope, so the illegal combination
        // produced arbitrary partial text and a misleading downstream undefined-reference on whatever name
        // failed to materialize (GnuCOBOL's recursive-replacement EXTENSION accepts this shape; ISO does
        // not). Detection uses the SAME FindCopyKeyword the expander splices by, so the report and the
        // recursion can never disagree about what counts as a COPY statement. Expansion continues after the
        // report — the diagnostic is the verdict; the splice keeps the downstream parse coherent.
        if (statement.ReplacingSpecified && FindCopyKeyword(normalized, 0) >= 0)
            _diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.CopyReplacingNestedCopy.Code,
                $"COPY {libraryName} REPLACING: the library text contains a COPY statement — ISO §7.2.3.4 "
                + "GR10 forbids the combination (\"If the REPLACING phrase is specified, the library text "
                + "shall not contain a COPY statement\"); nesting is permitted only without REPLACING "
                + "(GR12). Flatten the copybook, or drop the REPLACING phrase.",
                at.ToLocation(), default);
        var copybookMapped = ApplyReplacements(normalizedMapped, statement.Replacements);
        return new OneCopyResult(CopyOutcome.Found, copybookMapped.Text, copybookPath, copybookMapped);
    }

    /// <summary>A parsed COPY statement: text-name-1 or the value of literal-1 (null when the statement names no
    /// usable library text — its diagnostic is already reported), library-name-1 or the value of literal-2, whether
    /// the REPLACING phrase was written (§7.2.3.4 GR10 turns on that, not on how many operands parsed), its operands,
    /// and the position just past the statement's separator period.</summary>
    private readonly record struct CopyStatement(string? TextName, string? LibraryName, bool ReplacingSpecified,
        List<Replacement> Replacements, int End);

    /// <summary>Parse the COPY statement at <paramref name="copyIdx"/> in its §7.2.3.2 general-format order, over
    /// §7.2.2.5 text-words: <c>COPY {text-name-1 | literal-1} [{OF | IN} {library-name-1 | literal-2}]
    /// [SUPPRESS [PRINTING]] [REPLACING operands…] .</c> — the statement ends at its separator period (§7.2.3.4 GR6
    /// "beginning with the reserved word COPY and ending with the separator period, inclusive"), so a period inside
    /// pseudo-text or a literal no longer ends it. A word out of that order is COBOLNET2449; SUPPRESS has no effect on
    /// the resultant text (GR4 governs only a listing, which this compiler does not produce). kb/Work PB1354.</summary>
    private CopyStatement ParseCopyStatement(MappedText mapped, int copyIdx)
    {
        var c = new StatementCursor(mapped.Text, copyIdx + "COPY".Length, (p, message) =>
            _diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.TextManipulationStatementSyntax.Code,
                "COPY statement: " + message + " (§7.2.3.2)", mapped.OriginAt(p).ToLocation(), default));

        string? textName = ReadCopyName(mapped, c, "text-name-1 or literal-1");
        string? libraryName = null;
        if (c.TryPeek(out var w) && (w.IsWord("OF") || w.IsWord("IN")))
        {
            c.Advance(w);
            libraryName = ReadCopyName(mapped, c, "library-name-1 or literal-2");
        }
        if (c.TryPeek(out w) && w.IsWord("SUPPRESS"))
        {
            c.Advance(w);
            if (c.TryPeek(out w) && w.IsWord("PRINTING")) c.Advance(w);
        }

        var replacements = new List<Replacement>();
        bool replacing = false;
        if (!c.HasError && c.TryPeek(out w) && w.IsWord("REPLACING"))
        {
            replacing = true;
            c.Advance(w);
            // The VCR-row-4 gate rides the operand reads (COPY only — REPLACE is not in the E.2 removal).
            ParseReplacingOperands(c, replacements,
                nonPseudoText: p => OnNonPseudoTextOperand(mapped, p),
                nestedCopy: p => ReportPlacement(mapped.OriginAt(p), "a COPY statement is written inside the "
                    + "REPLACING phrase of another COPY statement — §7.2.3.3 SR1: \"a COPY statement shall not "
                    + "appear within a COPY statement\""));
        }
        if (!c.HasError && c.TryPeek(out w) && w.IsWord("COPY"))
        {
            ReportPlacement(mapped.OriginAt(w.Start), "a COPY statement begins inside another COPY statement — "
                + "§7.2.3.3 SR1: \"a COPY statement shall not appear within a COPY statement\"");
            c.MarkError();
        }
        c.ExpectSeparatorPeriod();
        return new CopyStatement(textName, libraryName, replacing, replacements, c.Pos);
    }

    /// <summary>The reserved words of the COPY statement's own general format — never a text-name or library-name
    /// where they stand (COPY itself is §7.2.3.3 SR1's business).</summary>
    private static bool IsCopyPhraseWord(in TextWord w)
        => w.IsWord("OF") || w.IsWord("IN") || w.IsWord("SUPPRESS") || w.IsWord("PRINTING") || w.IsWord("REPLACING");

    /// <summary>The figurative-constant words (§8.3.3.6.2 formats 1–6; a format-7 symbolic-character is a
    /// user-defined word, which as text-name-1 is simply a text-name), barred as literal-1 / literal-2 by
    /// §7.2.3.3 SR4.</summary>
    private static readonly HashSet<string> FigurativeConstantWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ZERO", "ZEROS", "ZEROES", "SPACE", "SPACES", "HIGH-VALUE", "HIGH-VALUES", "LOW-VALUE", "LOW-VALUES",
        "QUOTE", "QUOTES", "ALL",
    };

    /// <summary>Read text-name-1 / literal-1 (or library-name-1 / literal-2) and return the name it gives — a word as
    /// written, or the VALUE of an alphanumeric literal (a doubled quotation symbol collapsed; a hexadecimal literal
    /// decoded). §7.2.3.3 SR4 (no concatenation expression, no figurative constant) and SR5 ("Literal-1 and
    /// literal-2 shall be alphanumeric literals") are checked here, each as COBOLNET2450; null when no usable name
    /// was written.</summary>
    private string? ReadCopyName(MappedText mapped, StatementCursor c, string operand)
    {
        void LiteralForm(int at, string message)
            => _diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.CopyLiteralForm.Code,
                $"COPY {operand}: {message}", mapped.OriginAt(at).ToLocation(), default);

        if (!c.TryPeek(out var w))
        {
            c.Error(c.Pos, $"{operand} is missing");
            return null;
        }
        if (w.IsWord("COPY"))
        {
            ReportPlacement(mapped.OriginAt(w.Start), "a COPY statement begins inside another COPY statement — "
                + "§7.2.3.3 SR1: \"a COPY statement shall not appear within a COPY statement\"");
            c.MarkError();
            return null;
        }
        if (w.Kind is not (TextWordKind.CharacterString or TextWordKind.Literal) || IsCopyPhraseWord(w))
        {
            c.Error(w.Start, $"{operand} is missing — found '{w.Value}'");
            return null;
        }
        c.Advance(w);

        string? name;
        if (w.Kind == TextWordKind.Literal)
        {
            var literal = TextWordScanner.DecomposeLiteral(w.Value);
            name = !literal.Terminated || !literal.IsAlphanumeric ? null
                : literal.Prefix.Length == 0 ? literal.Content
                : DecodeHexadecimal(literal.Content);
            if (name is null)
                LiteralForm(w.Start, $"{w.Value} is not an alphanumeric literal — §7.2.3.3 SR5: \"Literal-1 and "
                    + "literal-2 shall be alphanumeric literals\"");
        }
        else if (FigurativeConstantWords.Contains(w.Value))
        {
            LiteralForm(w.Start, $"the figurative constant {w.Value.ToUpperInvariant()} names no library text — "
                + "§7.2.3.3 SR4: \"A concatenation expression or figurative constant shall not be specified for "
                + "literal-1, or literal-2\"");
            if (w.IsWord("ALL") && c.TryPeek(out var allLiteral) && allLiteral.Kind == TextWordKind.Literal)
                c.Advance(allLiteral);
            name = null;
        }
        else
            name = w.Value;

        if (c.TryPeek(out var amp) && amp.IsWord("&"))
        {
            LiteralForm(amp.Start, "a concatenation expression names no library text — §7.2.3.3 SR4: \"A "
                + "concatenation expression or figurative constant shall not be specified for literal-1, or literal-2\"");
            while (c.TryPeek(out amp) && amp.IsWord("&"))
            {
                c.Advance(amp);
                if (c.TryPeek(out var operandWord) && operandWord.Kind is TextWordKind.Literal or TextWordKind.CharacterString
                    && !IsCopyPhraseWord(operandWord))
                    c.Advance(operandWord);
            }
            return null;
        }
        return name;
    }

    /// <summary>The value of a hexadecimal alphanumeric literal's content (§8.3.3.2.2 format 2: each pair of
    /// hexadecimal digits is one alphanumeric character); null when the content is not an even number of
    /// hexadecimal digits.</summary>
    private static string? DecodeHexadecimal(string hexDigits)
    {
        if (hexDigits.Length % 2 != 0) return null;
        var chars = new char[hexDigits.Length / 2];
        for (int i = 0; i < chars.Length; i++)
        {
            if (!byte.TryParse(hexDigits.AsSpan(2 * i, 2), System.Globalization.NumberStyles.AllowHexSpecifier,
                    System.Globalization.CultureInfo.InvariantCulture, out byte b))
                return null;
            chars[i] = (char)b;
        }
        return new string(chars);
    }

    /// <summary>Report a §7.2.3.3 SR1 / SR2 placement violation (COBOLNET2451) at a SOURCE origin.</summary>
    private void ReportPlacement(SourceOrigin at, string message)
        => _diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.CopyStatementPlacement.Code,
            message, at.ToLocation(), default);

    /// <summary>Expand every COPY statement in <paramref name="text"/> ONE level (no recursion into the copybook):
    /// for each found copybook, its resolved text is handed to <paramref name="expandCopybook"/> (the merged
    /// CC+COPY driver, which processes the copybook's own directives AND its nested COPY), and the result is
    /// spliced with the same blank-line framing as <see cref="ExpandCopyStatements"/>. This is the COPY half of the
    /// interleaved text-manipulation driver (ISO §7.2.1) — the CC half drives, calling this only on emitting-branch
    /// text so an omitted-branch COPY is never expanded (design SSOT <c>DESIGN-cc-in-copy.md</c>).</summary>
    internal string ExpandCopiesOneLevel(string text, HashSet<string> alreadyIncluded, int depth,
        Func<string, int, string> expandCopybook)
        => ExpandCopiesOneLevel(MappedText.Identity(text, _sourceName), alreadyIncluded, depth,
            (m, d, _) => MappedText.Identity(expandCopybook(m.Text, d), m.Lines[0].File)).Text;

    /// <summary>The MAPPED one-level expansion (kb/Work PB82): the text before a COPY keeps its origins, the
    /// incorporated copybook's lines carry the copybook's, and the framing newlines belong to the COPY statement's
    /// own line — so a diagnostic or EXCEPTION-LOCATION inside copied text names the copybook file and line, and
    /// one after the COPY names the main source's own line, not the resultant ordinal.
    /// <para><paramref name="expandCopybook"/> receives the copybook, its depth, and the 0-based line piece of the
    /// RETURNED text at which the copybook's expansion begins — the one fact the merged driver needs to place a
    /// directive met inside the copybook in the resultant line frame (kb/Work PB1066).</para></summary>
    internal MappedText ExpandCopiesOneLevel(MappedText mapped, HashSet<string> alreadyIncluded, int depth,
        Func<MappedText, int, int, MappedText> expandCopybook)
    {
        string text = mapped.Text;
        if (depth > MaxCopyDepth)
        {
            Report(DiagnosticDescriptors.CBL3622, mapped.OriginAt(0), MaxCopyDepth);
            return mapped;
        }

        var w = new OriginWriter();
        int pos = 0;
        while (pos < text.Length)
        {
            int copyIdx = FindCopyKeyword(text, pos, glued => ReportPlacement(mapped.OriginAt(glued),
                "COPY is glued to the character-string before it (no space follows the period, comma or semicolon), "
                + "so it forms no COPY statement — §7.2.3.3 SR2: \"A COPY statement shall be preceded by a space "
                + "except when it is the first statement in a compilation group\""));
            if (copyIdx < 0)
            {
                w.AppendSlice(mapped, pos, text.Length - pos);
                break;
            }
            w.AppendSlice(mapped, pos, copyIdx - pos);
            SourceOrigin copyLine = mapped.OriginAt(copyIdx);

            var one = ResolveOneCopy(mapped, copyIdx, alreadyIncluded, out int afterCopy);
            if (one.Outcome == CopyOutcome.Found)
            {
                w.NewLine(copyLine);
                var processed = expandCopybook(one.Mapped!, depth + 1, w.LineCount);   // CC + nested COPY on the copybook
                w.AppendMapped(processed);
                w.NewLine(copyLine);
                alreadyIncluded.Remove(one.CopybookPath!);
            }
            else
            {
                w.Append(one.Text.AsSpan(), copyLine);
                w.NewLine(copyLine);
            }
            pos = afterCopy;
        }
        return w.Finish();
    }

    /// <summary>
    /// Find a keyword that is the first significant word on a line (after optional whitespace).
    /// Prevents false matches inside VALUE strings or other data contexts.
    /// </summary>
    private static int FindKeywordAtLineStart(string text, int startPos, string keyword)
    {
        int pos = startPos;

        while (pos < text.Length)
        {
            while (pos < text.Length && text[pos] == ' ')
                pos++;

            if (pos + keyword.Length <= text.Length &&
                MatchWord(text, pos, keyword) &&
                (pos + keyword.Length >= text.Length || !char.IsLetterOrDigit(text[pos + keyword.Length])))
            {
                return pos;
            }

            while (pos < text.Length && text[pos] != '\n')
                pos++;
            if (pos < text.Length) pos++;
        }
        return -1;
    }

    /// <summary>
    /// Normalize copy-library text to free form. Library members are reference (fixed) format,
    /// but CCVS members use non-standard indicator letters (C, G) in column 7 that the general
    /// <see cref="ReferenceFormatProcessor.IsFixedForm"/> heuristic rejects. Detect fixed form
    /// from the sequence-number area (columns 1-6 numeric) instead, then convert; fall back to
    /// the general normalizer for anything that does not look like a sequence-numbered member.
    /// </summary>
    private static string NormalizeCopybook(string text) => NormalizeCopybookMapped(text, "<copybook>").Text;

    /// <summary>The MAPPED copybook normalization (kb/Work PB82) — the ONE implementation: the free-form library text
    /// with, per line, the copybook's path and physical line (a fixed-form member's continuation joins are tracked
    /// exactly as the main source's are).</summary>
    private static MappedText NormalizeCopybookMapped(string text, string copybookPath)
    {
        var lines = text.Split('\n');
        int seqLines = 0, total = 0;
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line) || line.Length < 7) continue;
            total++;
            bool seqDigits = true, anyDigit = false;
            for (int i = 0; i < 6 && i < line.Length; i++)
            {
                if (char.IsDigit(line[i])) anyDigit = true;
                else if (line[i] != ' ') { seqDigits = false; break; }
            }
            if (seqDigits && anyDigit) seqLines++;
        }
        bool fixedForm = total > 0 && seqLines * 100 / total >= 50;
        return fixedForm
            ? ReferenceFormatProcessor.ConvertFixedToFreeMapped(text, copybookPath)
            : ReferenceFormatProcessor.NormalizeToFreeFormMapped(text, dialectLevel: 85, permissive: false, diagnostics: null, copybookPath);
    }

    /// <summary>
    /// Find the next COPY statement from <paramref name="startPos"/> (a position between statements): the next
    /// text-word that is the character-string COPY (§7.2.2.3 "A character-string is either a text-word or the word
    /// 'COPY'"). COPY may appear anywhere a character-string may (§7.2.3.3 SR1) — after a level number
    /// (77 COPY K1W03.), after a data-name (01 TST-TEST COPY K101A.), inside a statement (ADD COPY K1P01. TO …) — and
    /// because the search walks <see cref="TextWordScanner"/> words it can never match inside a literal, a comment,
    /// or a longer word (COPYSECT-1).
    /// <para>A COPY glued behind a period, comma or semicolon (<c>PIC X.COPY BK.</c>) is not a text-word at all —
    /// those characters separate only when a space follows (§8.3.5 2) / 3)) — so it is not a COPY statement; it is
    /// the §7.2.3.3 SR2 mistake, and <paramref name="onGluedCopy"/> is told where it is so the caller can name the
    /// rule instead of leaving the downstream parser to trip over the word.</para>
    /// </summary>
    private static int FindCopyKeyword(string text, int startPos, Action<int>? onGluedCopy = null)
    {
        int pos = startPos;
        while (TextWordScanner.TryNext(text, ref pos, out var word))
        {
            if (word.IsWord("COPY")) return word.Start;
            if (onGluedCopy is not null && word.Kind == TextWordKind.CharacterString && word.Span.Length > 4
                && word.Span.EndsWith("COPY", StringComparison.OrdinalIgnoreCase) && word.Span[^5] is '.' or ',' or ';')
                onGluedCopy(word.End - 4);
        }
        return -1;
    }

    private static bool MatchWord(string text, int pos, string word)
    {
        if (pos + word.Length > text.Length) return false;
        for (int i = 0; i < word.Length; i++)
        {
            if (char.ToUpperInvariant(text[pos + i]) != word[i])
                return false;
        }
        return true;
    }

    /// <summary>A cursor over the text-words of ONE COPY or REPLACE statement — the parser of their general formats
    /// (§7.2.3.2 / §7.2.4.2) reads through it. Separator commas and semicolons "may be used anywhere the separator
    /// space is used" (§8.3.5 2)), so <see cref="TryPeek"/> steps over them. Only a statement's FIRST syntax error is
    /// reported; recovery (<see cref="ExpectSeparatorPeriod"/>) skips to the statement's separator period.</summary>
    private sealed class StatementCursor(string text, int pos, Action<int, string> reportSyntax)
    {
        public string Text { get; } = text;

        /// <summary>The position just past the last text-word consumed.</summary>
        public int Pos { get; private set; } = pos;

        /// <summary>True once the statement has drawn an error (reported here or by the caller).</summary>
        public bool HasError { get; private set; }

        /// <summary>The next text-word, not consumed, stepping over separator commas and semicolons.</summary>
        public bool TryPeek(out TextWord word)
        {
            int p = Pos;
            while (TextWordScanner.TryNext(Text, ref p, out word))
                if (word.Kind != TextWordKind.SeparatorCommaOrSemicolon) return true;
            return false;
        }

        /// <summary>The next text-word of any kind, consumed — how a pseudo-text body is read.</summary>
        public bool TryTake(out TextWord word)
        {
            int p = Pos;
            if (!TextWordScanner.TryNext(Text, ref p, out word)) return false;
            Pos = p;
            return true;
        }

        public void Advance(in TextWord word) => Pos = word.End;

        /// <summary>Report a syntax error (the statement's first only).</summary>
        public void Error(int at, string message)
        {
            if (HasError) return;
            HasError = true;
            reportSyntax(Math.Min(at, Math.Max(0, Text.Length - 1)), message);
        }

        /// <summary>Record that the caller reported an error of its own, so no syntax error follows it.</summary>
        public void MarkError() => HasError = true;

        /// <summary>Consume the terminating separator period; anything else there is an error, recovered by
        /// skipping to the next separator period (or the end of the text).</summary>
        public void ExpectSeparatorPeriod()
        {
            if (TryPeek(out var w) && w.IsSeparatorPeriod) { Advance(w); return; }
            if (TryPeek(out w))
                Error(w.Start, $"'{w.Value}' is out of place — the general format allows no more phrases here, "
                    + "and the statement ends with a separator period");
            else
                Error(Pos, "the statement has no terminating separator period");
            while (TryTake(out w))
                if (w.IsSeparatorPeriod) return;
        }
    }

    /// <summary>Parse the operands of a REPLACING phrase (COPY, §7.2.3.2) or a format-1 REPLACE statement
    /// (§7.2.4.2) into <paramref name="into"/>: one or more <c>[LEADING | TRAILING] operand BY operand</c> pairs, up
    /// to (not including) the separator period. <paramref name="nonPseudoText"/> is told of each operand that is not
    /// <c>==pseudo-text==</c> (COPY: the Annex E.2 removal gate; REPLACE: COBOLNET1641); <paramref name="nestedCopy"/>
    /// (COPY only) of each COPY word inside pseudo-text (§7.2.3.3 SR1).</summary>
    private static void ParseReplacingOperands(StatementCursor c, List<Replacement> into,
        Action<int>? nonPseudoText, Action<int>? nestedCopy)
    {
        bool any = false;
        while (c.TryPeek(out var w) && !w.IsSeparatorPeriod)
        {
            ReplaceKind kind = ReplaceKind.Whole;
            if (w.IsWord("LEADING")) { kind = ReplaceKind.Leading; c.Advance(w); }
            else if (w.IsWord("TRAILING")) { kind = ReplaceKind.Trailing; c.Advance(w); }

            if (ReadOperand(c, nonPseudoText, nestedCopy) is not { } from) return;
            bool more = c.TryPeek(out var by);
            if (!more || !by.IsWord("BY"))
            {
                c.Error(more ? by.Start : c.Pos, "BY and the replacement operand must follow each REPLACING operand");
                return;
            }
            c.Advance(by);
            if (ReadOperand(c, nonPseudoText, nestedCopy) is not { } to) return;
            into.Add(new Replacement(TextWordScanner.MatchWords(from), to, kind));
            any = true;
        }
        if (!any) c.Error(c.Pos, "the REPLACING phrase names no operands");
    }

    /// <summary>Read one REPLACING operand and return its text as written: the content of a <c>==pseudo-text==</c>
    /// (bounded by the pseudo-text-delimiter TEXT-WORDS, so an <c>==</c> inside a literal does not end it), or — the
    /// COBOL-85 / 2002 / 2014 COPY forms (removed by ISO 2023, Annex E.2 item 1) — a literal, or an identifier / word:
    /// a word with optional OF/IN qualifiers and one balanced subscript group. Null (and a syntax error) when no
    /// operand is there.</summary>
    private static string? ReadOperand(StatementCursor c, Action<int>? nonPseudoText, Action<int>? nestedCopy)
    {
        bool more = c.TryPeek(out var w);
        if (!more || w.IsSeparatorPeriod || w.IsWord("BY"))
        {
            c.Error(more ? w.Start : c.Pos, "a REPLACING operand is missing");
            return null;
        }

        if (w.Kind == TextWordKind.PseudoTextDelimiter)
        {
            c.Advance(w);
            while (c.TryTake(out var t))
            {
                if (t.Kind == TextWordKind.PseudoTextDelimiter)
                    return c.Text[w.End..t.Start].Trim();
                if (t.IsWord("COPY")) nestedCopy?.Invoke(t.Start);
            }
            c.Error(w.Start, "the ==pseudo-text== has no closing delimiter");
            return null;
        }

        nonPseudoText?.Invoke(w.Start);
        if (w.Kind == TextWordKind.Literal)
        {
            c.Advance(w);
            return w.Value;
        }
        if (w.Kind != TextWordKind.CharacterString)
        {
            c.Error(w.Start, $"'{w.Value}' is not a REPLACING operand");
            return null;
        }

        // identifier-1/2 or word-1/2: a data-name with optional OF/IN qualifiers and an optional subscript —
        // e.g. WRK IN GRP-002 (1). A plain word (including a signed number such as +2) is the degenerate
        // single-text-word case. The verbatim span is returned: for matching it is scanned into text-words, and as
        // a replacement it is inserted as written.
        int start = w.Start;
        c.Advance(w);
        while (c.TryPeek(out var q) && (q.IsWord("OF") || q.IsWord("IN")))
        {
            c.Advance(q);
            if (c.TryPeek(out var qualifier) && qualifier.Kind == TextWordKind.CharacterString) c.Advance(qualifier);
        }
        if (c.TryPeek(out var open) && open.Kind == TextWordKind.Separator && open.Span[0] == '(')
        {
            int depth = 0;
            while (c.TryTake(out var t))
            {
                if (t.Kind != TextWordKind.Separator) continue;
                if (t.Span[0] == '(') depth++;
                else if (t.Span[0] == ')' && --depth == 0) break;
            }
        }
        return c.Text[start..c.Pos];
    }

    private string? FindCopybook(string textName, string? libraryName = null)
    {
        // COPY text-name OF/IN library-name selects the copy library (ISO §7.2.3). A library
        // name is resolved to a same-named subdirectory of a search path, so the same text-name
        // can resolve to different text in different libraries. If the qualified library has no
        // such member, fall back to the unqualified search (a single default library).
        if (!string.IsNullOrEmpty(libraryName))
        {
            foreach (var searchPath in _searchPaths)
            {
                string libDir = Path.Combine(searchPath, libraryName);
                if (!_inputs.DirectoryExists(libDir)) continue;
                foreach (var ext in CopybookExtensions)
                {
                    string fullPath = Path.Combine(libDir, textName + ext);
                    if (_inputs.FileExists(fullPath))
                        return fullPath;
                }
            }
        }

        foreach (var searchPath in _searchPaths)
        {
            foreach (var ext in CopybookExtensions)
            {
                string fullPath = Path.Combine(searchPath, textName + ext);
                if (_inputs.FileExists(fullPath))
                    return fullPath;
            }
        }
        return null;
    }
}
