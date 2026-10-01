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
    int dialectLevel = 85,
    bool permissive = false,
    CompilationInputs? inputs = null,
    bool ccvsIndicators = false,   // the --nist column-7 conventions for library text too (kb/Work PB1494)
    ImplicitFormatOps? implicitFormatOps = null)   // §14.9.28.4 GR14's implicit PUSH/POP ALL written in library text (kb/Work PB1066)
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

    /// <summary>The suffixes tried, in order, after a text-name as spelled (the DOC-A.1-40 determination —
    /// GnuCOBOL's list and order; see <see cref="LocateInLibrary"/>).</summary>
    private static readonly string[] CopybookSuffixes = [".CPY", ".CBL", ".COB", ".cpy", ".cbl", ".cob"];

    private readonly List<string> _searchPaths = new(searchPaths ?? []);

    // Diagnostic plumbing (DEVLOG 307). Optional so the standalone preprocess CLI and tests can construct
    // a CopyProcessor without a bag; when absent, nothing is reported.
    private readonly DiagnosticBag? _diagnostics = diagnostics;
    private readonly string _sourceName = sourceName;

    /// <summary>Add a directory to search for copybooks.</summary>
    public void AddSearchPath(string path) => _searchPaths.Add(path);

    /// <summary>The reference format each text was read in, by file (kb/Work PB1067): the compilation group's, which
    /// the caller registers after normalizing it, and each library text's, registered as it is normalized — so a
    /// COPY statement's format is known wherever it is written.</summary>
    private readonly Dictionary<string, ReferenceFormatMap> _referenceFormats = new(StringComparer.Ordinal);

    /// <summary>Record the reference format <paramref name="file"/> was read in — the normalizer's
    /// <see cref="ReferenceFormatMap"/> for the compilation group — so library text copied from it starts in the format
    /// in effect for its COPY statement (§7.3.24.3 3)). A text with no registered map (a caller that normalized
    /// without one) has its library text's initial format detected, as the compilation group's is.</summary>
    public void RegisterReferenceFormat(string file, ReferenceFormatMap formats) => _referenceFormats[file] = formats;

    /// <summary>The reference format map of EVERY text read so far — the compilation group's and each library text's —
    /// by file: where each text's written format directives are (<see cref="ReferenceFormatMap.DirectiveLines"/>), which
    /// decides whether a §14.9.28.4 GR14 implicit PUSH ALL / POP ALL bracket can change the format at all
    /// (<see cref="ImplicitFormatOps.Place"/>; kb/Work PB1066).</summary>
    public IReadOnlyDictionary<string, ReferenceFormatMap> ReferenceFormats => _referenceFormats;

    /// <summary>Report at a SOURCE origin (kb/Work PB82) — the file and physical line the text at a position came
    /// from, never an ordinal of the text being processed.</summary>
    private void Report(DiagnosticDescriptor descriptor, SourceOrigin at, params object[] args)
        => _diagnostics?.Report(descriptor, at.ToLocation(), TextSpan.Empty, args);

    /// <summary>
    /// Process all COPY and REPLACE statements in the source text — the LEGACY oracle's path (the product runs the
    /// merged driver, <see cref="ConditionalCompilationProcessor.Manipulate"/>). The legacy compiler keeps its
    /// historical search, which adds <paramref name="sourceDir"/> as the first configured search path; WiseOwl
    /// COBOL's own default library (DOC-A.1-40) does not search the source directory.
    /// </summary>
    public string Process(string sourceText, string sourceDir)
    {
        if (!_searchPaths.Contains(sourceDir))
            _searchPaths.Insert(0, sourceDir);

        string expanded = ExpandCopyStatements(sourceText, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        return ApplyReplaceStatements(expanded, _diagnostics, _sourceName, EditionInfo.Of(dialectLevel, permissive));
    }

    /// <summary>The REPLACE pass over a plain text (the legacy oracle's <see cref="Process"/>).</summary>
    internal static string ApplyReplaceStatements(string text, DiagnosticBag? diagnostics = null,
        string sourceName = "<source>", EditionInfo? edition = null)
        => ApplyReplaceStatements(MappedText.Identity(text, sourceName), diagnostics, edition).Text;

    /// <summary>
    /// Step 3 of text manipulation (§7.2.1): the REPLACE statements of the conditionally-processed compilation group
    /// are applied in order, over its §7.2.2.5 text-words. A REPLACE statement is recognized wherever the character-
    /// string REPLACE stands as a text-word (§7.2.4.3 SR1 "A REPLACE statement may be specified anywhere in source text
    /// or in library text that a character-string or a separator, other than the closing delimiter of a literal, may
    /// appear"; kb/Work PB1358 — never only as the first word of a line), parsed in its §7.2.4.2 general format
    /// (format 1 <c>REPLACE [ALSO] operands… .</c>, format 2 <c>REPLACE [LAST] OFF .</c>), and fed to the
    /// <see cref="ReplaceStates"/> machine of §7.2.4.4 GR4–GR7 (kb/Work PB1357). The text between two REPLACE statements
    /// is replaced by the operands of the statement ACTIVE there (GR8 "begins with the text immediately following the
    /// REPLACE statement"). MAPPED (kb/Work PB82): a REPLACE statement's own text vanishes from the resultant text, the
    /// kept text keeps its origins, and a replacement's lines take the origin of the line its match started on.
    /// <para>A non-pseudo-text operand (kb/Work R39) draws COBOLNET1641: REPLACE's operands were never literals in any
    /// ISO edition (§7.2.4.2; §7.2.4.3 SR7). <paramref name="edition"/> (null: no edition gate — the line-map replay)
    /// gates the ALSO and LAST phrases at their introducing edition (constructs row replace-also-last-2002).</para>
    /// </summary>
    internal static MappedText ApplyReplaceStatements(MappedText mapped, DiagnosticBag? diagnostics = null,
        EditionInfo? edition = null)
    {
        string text = mapped.Text;
        var w = new OriginWriter();
        var states = new ReplaceStates();
        int pos = 0;

        while (pos < text.Length)
        {
            int replaceIdx = FindStatementKeyword(text, pos, "REPLACE", glued => diagnostics?.ReportError(
                Editions.Diagnostics.DiagnosticCatalog.TextManipulationStatementSyntax.Code,
                "REPLACE is glued to the character-string before it (no space follows the period, comma or semicolon), "
                + "so it forms no REPLACE statement — §7.2.4.3 SR2: \"A REPLACE statement shall be preceded by a space "
                + "except when it is the first statement in a compilation group\"", mapped.OriginAt(glued).ToLocation(), default));
            if (replaceIdx < 0)
            {
                w.AppendMapped(ApplyReplacements(mapped.Slice(pos, text.Length - pos), states.Active, ReplaceResultRule, diagnostics));
                break;
            }

            w.AppendMapped(ApplyReplacements(mapped.Slice(pos, replaceIdx - pos), states.Active, ReplaceResultRule, diagnostics));

            // The statement is parsed over text-words (§7.2.4.2 general format) through its separator period — a
            // period INSIDE pseudo-text is a text-word of the operand, never the statement's end (kb/Work PB1354).
            var statement = new StatementCursor(text, replaceIdx + "REPLACE".Length, (p, message) =>
                diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.TextManipulationStatementSyntax.Code,
                    "REPLACE statement: " + message + " (§7.2.4.2)", mapped.OriginAt(p).ToLocation(), default));
            // §7.2.4.3 SR2: "A REPLACE statement shall be preceded by a space except when it is the first statement in
            // a compilation group" — REPLACE directly after a parenthesis, a colon, a literal or a pseudo-text delimiter.
            if (replaceIdx > 0 && !TextWordScanner.IsSeparatorSpace(text[replaceIdx - 1]))
                statement.Error(replaceIdx, $"REPLACE is not preceded by a space (it follows '{text[replaceIdx - 1]}') — "
                    + "§7.2.4.3 SR2: \"A REPLACE statement shall be preceded by a space except when it is the first "
                    + "statement in a compilation group\"");

            void Gate(in TextWord phrase)
            {
                if (edition is { } e && diagnostics is not null)
                    ConstructRegistry.Check(e, new BagSink(diagnostics, mapped.OriginAt(phrase.Start).ToLocation()),
                        Constructs.ReplaceAlsoLast2002, $"REPLACE {phrase.Value.ToUpperInvariant()}");
            }

            statement.TryPeek(out var first);
            if (first.IsWord("LAST") || first.IsWord("OFF"))
            {
                // Format 2 (off): REPLACE [LAST] OFF.
                bool last = first.IsWord("LAST");
                if (last)
                {
                    Gate(first);
                    statement.Advance(first);
                }
                if (statement.TryPeek(out var off) && off.IsWord("OFF")) statement.Advance(off);
                else statement.Error(statement.Pos, "REPLACE LAST is followed by OFF");
                if (!statement.HasError) states.Off(last);
            }
            else
            {
                // Format 1 (replacing): REPLACE [ALSO] operands… .
                bool also = first.IsWord("ALSO");
                if (also)
                {
                    Gate(first);
                    statement.Advance(first);
                }
                var operands = new List<Replacement>();
                ParseReplacingOperands(statement, operands, new OperandScreen(ReplaceOperandRules, diagnostics, mapped), nestedCopy: null,
                    nonPseudoText: p =>
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
                states.Replacing(operands, also);
            }
            statement.ExpectSeparatorPeriod();
            pos = statement.Pos;
        }

        return w.Finish();
    }

    /// <summary>The states of the REPLACE statements met so far (§7.2.4.4 GR4): the ACTIVE statement's operands (null
    /// when none is active) and the last-in first-out queue of INACTIVE ones; a canceled statement is simply gone.
    /// kb/Work PB1357 — before, one flat operand list was cleared by every REPLACE, and ALSO / LAST could not be
    /// represented.</summary>
    private sealed class ReplaceStates
    {
        private readonly Stack<IReadOnlyList<Replacement>> _inactive = new();
        private IReadOnlyList<Replacement>? _active;

        /// <summary>The operands of the active REPLACE statement, in the order they are compared; empty when none is
        /// active.</summary>
        public IReadOnlyList<Replacement> Active => _active ?? [];

        /// <summary>A format 1 REPLACE statement, <paramref name="also"/> when it has the ALSO phrase.</summary>
        public void Replacing(IReadOnlyList<Replacement> operands, bool also)
        {
            if (_active is not null && also)
            {
                // GR7 a): "1. the active REPLACE statement is made inactive and is pushed into the queue of inactive
                // REPLACE statements. 2. The current REPLACE statement is expanded into a single REPLACE statement …
                // having as its operands all the operands of the current statement followed by the operands of the most
                // recent statement pushed into the queue of inactive REPLACE statements."
                _inactive.Push(_active);
                _active = [.. operands, .. _active];
                return;
            }
            // GR6 a): with none active it "is placed in the active state … The ALSO phrase, if specified, has no
            // effect"; GR7 b): without ALSO it "cancels the active REPLACE statement and cancels any REPLACE statements
            // in the queue of inactive REPLACE statements. Then the current REPLACE statement is placed in the active
            // state."
            _inactive.Clear();
            _active = operands;
        }

        /// <summary>A format 2 REPLACE statement, <paramref name="last"/> when it has the LAST phrase.</summary>
        public void Off(bool last)
        {
            // GR6 b): with none active "A format 2 REPLACE statement has no effect." GR7 c): LAST "cancels the active
            // REPLACE statement and pops the last statement that was pushed into the queue of inactive REPLACE
            // statements, if any. The popped statement, if any, is placed in the active state." GR7 d): without LAST it
            // "cancels the active REPLACE statement and cancels all REPLACE statements in the queue".
            if (_active is null) return;
            _active = last && _inactive.TryPop(out var popped) ? popped : null;
            if (!last) _inactive.Clear();
        }
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
        if (FindStatementKeyword(text, 0, "REPLACE") < 0)
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
    /// partial-word-1 as the text-words it is compared by, the replacement text exactly as written, and — for a whole
    /// pseudo-text-2 — what its replacing action would introduce that its statement's rule forbids
    /// (<see cref="Introduces"/>; null when nothing).</summary>
    private readonly record struct Replacement(IReadOnlyList<TextWord> From, string To, ReplaceKind Kind,
        string? Introduces = null);

    /// <summary>The rule on the text a replacing action produces (kb/Work PB1356) — §7.2.3.4 GR13 for COPY, §7.2.4.4
    /// GR9 for REPLACE, which differ only in that a COPY replacing action MAY produce a REPLACE statement (REPLACE
    /// statements "shall be syntactically correct after the action of the replacing phrase of the COPY statement",
    /// §7.2.1).</summary>
    private sealed record ResultRule(string Statement, bool ForbidsReplaceStatement, string Rule, string Quoted);

    private static readonly ResultRule CopyResultRule = new("COPY", false, "§7.2.3.4 GR13",
        "The replacing action of a COPY statement shall not introduce a COPY statement, a SOURCE FORMAT directive, a "
        + "comment, or a blank line.");

    private static readonly ResultRule ReplaceResultRule = new("REPLACE", true, "§7.2.4.4 GR9",
        "The text produced as a result of processing a REPLACE statement shall not contain a COPY statement, a REPLACE "
        + "statement, a SOURCE FORMAT directive, a comment, or a blank line.");

    /// <summary>What <paramref name="produced"/> — text a replacing action places into the resultant text — holds that
    /// <paramref name="rule"/> forbids: a COPY statement, a REPLACE statement (REPLACE's rule only), a SOURCE FORMAT
    /// directive or a comment; null when nothing. Comments and blank lines were removed from pseudo-text before text
    /// manipulation began — §6.5 2) "If the line is a comment line or a blank line, that line is logically discarded"
    /// — so a comment can arrive only as the <c>*&gt;</c> a partial-word result spells (<c>LEADING ==Q== BY ==*==</c>
    /// on <c>Q&gt;1</c>), which is asked of the TEXT: the text-word scanner skips a comment, so no word ever starts with
    /// its indicator. A blank line cannot arrive at all: an empty line inside a written pseudo-text-2 (NIST SM208A) is
    /// the line-count-preserving trace of a discarded blank line, not a blank line of the resultant text.</summary>
    private static string? ForbiddenIn(string produced, ResultRule rule)
    {
        if (TextWordScanner.HoldsComment(produced)) return "a comment";
        int pos = 0;
        bool directiveNext = false;
        while (TextWordScanner.TryNext(produced, ref pos, out var w))
        {
            if (w.IsWord("COPY")) return "a COPY statement";
            if (rule.ForbidsReplaceStatement && w.IsWord("REPLACE")) return "a REPLACE statement";
            var span = w.Span;
            if (directiveNext && span.StartsWith("SOURCE", StringComparison.OrdinalIgnoreCase)
                || span.StartsWith(">>") && span[2..].TrimStart(TextWordScanner.SeparatorSpaces)
                    .StartsWith("SOURCE", StringComparison.OrdinalIgnoreCase))
                return "a SOURCE FORMAT directive";
            directiveNext = span.SequenceEqual(">>");
        }
        return null;
    }

    /// <summary>
    /// Apply COPY REPLACING / REPLACE substitutions (ISO §7.2.3.4 GR 9 / §7.2.4.4 GR 8) over the §7.2.2.5 text-words
    /// of the text: at each leftmost text-word the operands are tried in the order written and the first match wins
    /// (d)); an unmatched word is copied (e)); after a match the word following the rightmost matched word is the new
    /// leftmost, so inserted text is never rescanned (f)). Two text-words compare by
    /// <see cref="TextWord.MatchesForReplacing"/> (c) 3.–4.). A match's source span is replaced verbatim by the
    /// replacement text, so white space and line breaks between the matched words vanish with them; the unmatched
    /// text keeps its origins (kb/Work PB82) and a replacement takes the origin of the line its match started on.
    /// </summary>
    /// <para><paramref name="rule"/> screens what each replacing action produces (§7.2.3.4 GR13 / §7.2.4.4 GR9, kb/Work
    /// PB1356): a forbidden result is reported (COBOLNET2574, once per operand pair per call) and still produced, so the
    /// downstream parse stays coherent — the diagnostic is the verdict.</para>
    private static MappedText ApplyReplacements(MappedText mapped, IReadOnlyList<Replacement> replacements,
        ResultRule rule, DiagnosticBag? diagnostics)
    {
        var active = replacements.Where(r => r.From.Count > 0).ToList();   // an empty operand cannot match
        if (active.Count == 0) return mapped;
        HashSet<int>? reported = null;
        void Forbidden(int operand, SourceOrigin at, string what)
        {
            if (diagnostics is null || !(reported ??= []).Add(operand)) return;
            diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.ReplacingResultForbidden.Code,
                $"the replacing action of a {rule.Statement} statement produces {what} — {rule.Rule}: \"{rule.Quoted}\"",
                at.ToLocation(), default);
        }

        string text = mapped.Text;
        // The words compared, and — apart — the compiler directive lines: each is "a single space" for matching
        // (c) 5.), so a match runs across it, but "A compiler directive line is not affected by the replacing action"
        // (§7.3.4 1)), so one lying inside a matched span is written back, on its own line, after the replacement.
        var words = new List<TextWord>();
        var directives = new List<TextWord>();
        foreach (var t in TextWordScanner.Scan(text))
        {
            if (t.Kind == TextWordKind.DirectiveLine) directives.Add(t);
            else if (!t.IsSpaceForMatching) words.Add(t);
        }
        int nextDirective = 0;
        var sb = new OriginWriter();
        int copiedUpTo = 0; // chars of `text` already emitted
        int w = 0;
        while (w < words.Count)
        {
            bool matched = false;
            for (int operand = 0; operand < active.Count; operand++)
            {
                var (from, to, kind, introduces) = active[operand];
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
                    if (introduces is not null) Forbidden(operand, mapped.OriginAt(matchStart), introduces);
                    while (nextDirective < directives.Count && directives[nextDirective].Start < matchStart) nextDirective++;
                    bool keptDirective = false;
                    for (; nextDirective < directives.Count && directives[nextDirective].Start < matchEnd; nextDirective++)
                    {
                        var directive = directives[nextDirective];
                        sb.NewLine(mapped.OriginAt(directive.Start));
                        sb.AppendSlice(mapped, directive.Start, directive.End - directive.Start);
                        keptDirective = true;
                    }
                    if (keptDirective) sb.NewLine(mapped.OriginAt(matchEnd));
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
                string produced = leading ? string.Concat(to.AsSpan(), word[part.Length..])
                    : string.Concat(word[..^part.Length], to.AsSpan());
                sb.Append(produced.AsSpan(), at);
                if (ForbiddenIn(produced, rule) is { } what) Forbidden(operand, at, what);
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
    internal OneCopyResult ResolveOneCopy(MappedText mapped, int copyIdx, HashSet<string> alreadyIncluded, bool inLibraryText,
        out int afterCopy)
    {
        string text = mapped.Text;
        SourceOrigin at = mapped.OriginAt(copyIdx);   // the COPY statement's SOURCE origin (kb/Work PB82)

        // §7.2.3.3 SR2: "A COPY statement shall be preceded by a space except when it is the first statement in a
        // compilation group" — a COPY word directly after a parenthesis, a colon, a literal's closing delimiter or a
        // pseudo-text delimiter. (A COPY glued behind a period, comma or semicolon is not even a text-word of its
        // own — see FindStatementKeyword.)
        if (copyIdx > 0 && !TextWordScanner.IsSeparatorSpace(text[copyIdx - 1]))
            ReportPlacement(at, $"COPY is not preceded by a space (it follows '{text[copyIdx - 1]}') — §7.2.3.3 SR2: "
                + "\"A COPY statement shall be preceded by a space except when it is the first statement in a "
                + "compilation group\"");

        var statement = ParseCopyStatement(mapped, copyIdx);
        afterCopy = statement.End;
        if (statement.TextName is null)
            return new OneCopyResult(CopyOutcome.NotFound, "*> COPY statement not processed — see its diagnostic", null);
        string libraryName = statement.TextName;
        // §7.2.3.4 GR12: "If the REPLACING phrase is not specified, the library text may contain a COPY statement that
        // does not include a REPLACING phrase" — so a COPY met INSIDE library text shall not have one (kb/Work PB1356;
        // GR10, the outer-REPLACING arm of the same rule, is checked below once the library text is read).
        if (inLibraryText && statement.ReplacingSpecified)
            _diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.CopyReplacingNestedCopy.Code,
                $"COPY {libraryName} REPLACING is written inside library text — ISO §7.2.3.4 GR12: \"If the REPLACING "
                + "phrase is not specified, the library text may contain a COPY statement that does not include a "
                + "REPLACING phrase\". Move the REPLACING phrase to the outermost COPY, or flatten the copybook.",
                at.ToLocation(), default);

        string? copybookPath = FindCopybook(libraryName, statement.LibraryName);
        if (copybookPath == null)
        {
            // §7.2.3.4 GR1 "Text-name-1 or literal-1 identifies the library text to be processed by the COPY
            // statement" and GR2 "Library-name-1 names a resource that shall be available to the compiler and shall
            // provide access to the library text": text that cannot be located is an error at every edition and on
            // every path — never a silently omitted COPY (kb/Work PB1355).
            string named = statement.LibraryName is { } lib ? $"{libraryName} OF {lib}" : libraryName;
            Report(DiagnosticDescriptors.CBL3620, at, named, string.Join("; ", LibraryPlaces()));
            return new OneCopyResult(CopyOutcome.NotFound, $"*> COPY {named} — library text not found", null);
        }
        if (!alreadyIncluded.Add(copybookPath))
        {
            // ISO §7.2.3.4 GR12: "The library text being copied shall not cause the processing of a COPY statement
            // that directly or indirectly copies itself."
            Report(DiagnosticDescriptors.CBL3621, at, libraryName);
            return new OneCopyResult(CopyOutcome.Circular, $"*> COPY {libraryName} — circular include skipped", null);
        }

        // Library text goes through the same §6.5 logical conversion as the source text, starting in the format in
        // effect for this COPY statement (§7.3.24.3 3)) — its own >>SOURCE FORMAT directives switch it from there, and
        // the COPY's text is unaffected after it (5), the revert). Then COPY … REPLACING (§7.2.3.4 9)).
        bool? copyFixed = _referenceFormats.TryGetValue(at.File, out var copyFormats) ? copyFormats.LibraryTextDefaultAt(at.Line) : null;
        var normalizedMapped = NormalizeCopybookMapped(_inputs.ReadAllText(copybookPath), copybookPath, copyFixed);
        string normalized = normalizedMapped.Text;
        // §7.2.3.3 SR9: "The length of a text-word within pseudo-text and within library text shall be from 1 through
        // 65,535 character positions" — the library-text half (the pseudo-text half is ScreenOperandPair's).
        for (int scan = 0; TextWordScanner.TryNext(normalized, ref scan, out var libraryWord);)
            if (libraryWord.Kind != TextWordKind.DirectiveLine && libraryWord.End - libraryWord.Start > MaxTextWordLength)
            {
                new OperandScreen(CopyOperandRules, _diagnostics, normalizedMapped).Content(libraryWord.Start,
                    CopyOperandRules.Length, "The length of a text-word within pseudo-text and within library text",
                    $"a text-word of {libraryWord.End - libraryWord.Start} characters is written in library text "
                    + $"{libraryName}; the limit is 65,535");
                break;
            }
        // §7.2.3.4 GR10 (kb/Work R34): "If the REPLACING phrase is specified, the library text shall not
        // contain a COPY statement" — GR12 permits nesting only WITHOUT replacing. Before this check the
        // caller recursed into the spliced text OUTSIDE the replacement scope, so the illegal combination
        // produced arbitrary partial text and a misleading downstream undefined-reference on whatever name
        // failed to materialize (GnuCOBOL's recursive-replacement EXTENSION accepts this shape; ISO does
        // not). Detection uses the SAME FindStatementKeyword the expander splices by, so the report and the
        // recursion can never disagree about what counts as a COPY statement. Expansion continues after the
        // report — the diagnostic is the verdict; the splice keeps the downstream parse coherent.
        if (statement.ReplacingSpecified && FindStatementKeyword(normalized, 0, "COPY") >= 0)
            _diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.CopyReplacingNestedCopy.Code,
                $"COPY {libraryName} REPLACING: the library text contains a COPY statement — ISO §7.2.3.4 "
                + "GR10 forbids the combination (\"If the REPLACING phrase is specified, the library text "
                + "shall not contain a COPY statement\"); nesting is permitted only without REPLACING "
                + "(GR12). Flatten the copybook, or drop the REPLACING phrase.",
                at.ToLocation(), default);
        var copybookMapped = ApplyReplacements(normalizedMapped, statement.Replacements, CopyResultRule, _diagnostics);
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
            ParseReplacingOperands(c, replacements, new OperandScreen(CopyOperandRules, _diagnostics, mapped),
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
            int copyIdx = FindStatementKeyword(text, pos, "COPY", glued => ReportPlacement(mapped.OriginAt(glued),
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

            var one = ResolveOneCopy(mapped, copyIdx, alreadyIncluded, inLibraryText: depth > 0, out int afterCopy);
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

    /// <summary>Normalize library text to logical free form (kb/Work PB82 / PB1067) through the ONE §6.5 walker the
    /// source text uses — <see cref="ReferenceFormatProcessor.NormalizeToFreeFormMapped(string, int, bool, DiagnosticBag?, string, bool?, out ReferenceFormatMap)"/>
    /// — starting in <paramref name="copyFixed"/>, the format in effect for the COPY statement (§7.3.24.3 3); null
    /// when that is unknown, and then detected as the compilation group's is). The text's own &gt;&gt;SOURCE FORMAT
    /// directives switch it (1), 5)); the resulting map is registered so a COPY inside this library text starts its
    /// library text in the format in effect THERE. Per line: the copybook's path and physical line.</summary>
    private MappedText NormalizeCopybookMapped(string text, string copybookPath, bool? copyFixed)
    {
        var mapped = ReferenceFormatProcessor.NormalizeToFreeFormMapped(text, dialectLevel, permissive,
            diagnostics: null, copybookPath, copyFixed, out var formats, ccvsIndicators,
            implicitOps: implicitFormatOps?.For(copybookPath));   // GR14's implicit ops written IN this library text
        _referenceFormats[copybookPath] = formats;
        return mapped;
    }

    /// <summary>
    /// Find the next COPY or REPLACE statement (<paramref name="keyword"/>) from <paramref name="startPos"/> (a
    /// position between statements): the next text-word that is that character-string (§7.2.2.3 "A character-string is
    /// either a text-word or the word 'COPY'"). Both statements may appear anywhere a character-string may (§7.2.3.3 SR1
    /// / §7.2.4.3 SR1) — after a level number (77 COPY K1W03.), after a data-name (01 TST-TEST COPY K101A.), inside a
    /// statement (ADD COPY K1P01. TO …), after another statement on the same line (DISPLAY X. REPLACE …) — and because
    /// the search walks <see cref="TextWordScanner"/> words it can never match inside a literal, a comment, a compiler
    /// directive line, or a longer word (COPYSECT-1, REPLACE-FLAG).
    /// <para>A keyword glued behind a period, comma or semicolon (<c>PIC X.COPY BK.</c>) is not a text-word at all —
    /// those characters separate only when a space follows (§8.3.5 2) / 3)) — so it is not a statement; it is the
    /// SR2 mistake ("shall be preceded by a space"), and <paramref name="onGlued"/> is told where it is so the caller
    /// can name the rule instead of leaving the downstream parser to trip over the word.</para>
    /// </summary>
    private static int FindStatementKeyword(string text, int startPos, string keyword, Action<int>? onGlued = null)
    {
        int pos = startPos;
        while (TextWordScanner.TryNext(text, ref pos, out var word))
        {
            if (word.IsWord(keyword)) return word.Start;
            if (onGlued is not null && word.Kind == TextWordKind.CharacterString && word.Span.Length > keyword.Length
                && word.Span.EndsWith(keyword, StringComparison.OrdinalIgnoreCase)
                && word.Span[^(keyword.Length + 1)] is '.' or ',' or ';')
                onGlued(word.End - keyword.Length);
        }
        return -1;
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

        /// <summary>The next text-word, not consumed, stepping over separator commas and semicolons and compiler
        /// directive lines (<see cref="TextWord.IsSpaceForMatching"/> — each stands where a separator space may).</summary>
        public bool TryPeek(out TextWord word)
        {
            int p = Pos;
            while (TextWordScanner.TryNext(Text, ref p, out word))
                if (!word.IsSpaceForMatching) return true;
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

    /// <summary>The syntax rules on the CONTENT of REPLACING operands. They are written word for word twice — §7.2.3.3
    /// (COPY) and §7.2.4.3 (REPLACE), numbered differently — and checked by ONE screen,
    /// <see cref="ScreenOperandPair"/> (kb/Work PB1353); a row holds one statement's rule numbers.</summary>
    private sealed record OperandRules(string Statement, string Clause, int PseudoText1, int PartialWord1,
        int PartialWord2, int Literal, int Length, int DirectiveLines, ResultRule Result);

    /// <summary>§7.2.3.3 SR6, SR11, SR12, SR13, SR9, SR10.</summary>
    private static readonly OperandRules CopyOperandRules = new("COPY", "§7.2.3.3", 6, 11, 12, 13, 9, 10, CopyResultRule);

    /// <summary>§7.2.4.3 SR3, SR5, SR6, SR7, SR9, SR10.</summary>
    private static readonly OperandRules ReplaceOperandRules = new("REPLACE", "§7.2.4.3", 3, 5, 6, 7, 9, 10, ReplaceResultRule);

    /// <summary>The longest text-word pseudo-text (and COPY library text) may hold (§7.2.3.3 SR9 / §7.2.4.3 SR9).</summary>
    private const int MaxTextWordLength = 65_535;

    /// <summary>Where a REPLACING phrase reports: its statement's operand rules and the statement's diagnostics, at
    /// SOURCE origins (kb/Work PB82).</summary>
    private sealed class OperandScreen(OperandRules rules, DiagnosticBag? diagnostics, MappedText mapped)
    {
        public OperandRules Rules { get; } = rules;

        /// <summary>COBOLNET2572 — an operand whose content breaks one of <see cref="Rules"/>.</summary>
        public void Content(int at, int rule, string quoted, string what)
            => diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.ReplacingOperandContent.Code,
                $"{Rules.Statement} statement: {what} — {Rules.Clause} SR{rule}: \"{quoted}\"",
                mapped.OriginAt(at).ToLocation(), default);

        /// <summary>COBOLNET2573 — a pseudo-text delimiter not separated as §8.3.5 6) requires.</summary>
        public void Delimiter(int at, string what)
            => diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.PseudoTextDelimiterPlacement.Code,
                $"{Rules.Statement} statement: {what} — §8.3.5 6): \"An opening pseudo-text delimiter shall be "
                + "immediately preceded by a space; a closing pseudo-text delimiter shall be immediately followed by one "
                + "of the separators space, comma, semicolon, or period\"", mapped.OriginAt(at).ToLocation(), default);
    }

    /// <summary>Screen one operand pair against its statement's content rules (kb/Work PB1353). Only a
    /// <c>==pseudo-text==</c> operand is screened: the COPY forms that are not pseudo-text (identifier, literal, word —
    /// COBOL-85 through 2014) are single operands the rules do not address.</summary>
    private static void ScreenOperandPair(OperandScreen screen, in Operand from, in Operand to, ReplaceKind kind)
    {
        var r = screen.Rules;
        foreach (var operand in (ReadOnlySpan<Operand>)[from, to])
        {
            if (!operand.IsPseudoText) continue;
            foreach (var e in operand.Elements)
            {
                if (e.Kind == TextWordKind.DirectiveLine)
                    screen.Content(e.Start, r.DirectiveLines, "Compiler directive lines shall not be specified within "
                        + "pseudo-text-1, pseudo-text-2, partial-word-1, or partial-word-2",
                        "a compiler directive line is written inside a REPLACING operand");
                else if (e.End - e.Start > MaxTextWordLength)
                    screen.Content(e.Start, r.Length, "The length of a text-word within pseudo-text",
                        $"a text-word of {e.End - e.Start} characters is written in pseudo-text; the limit is 65,535");
            }
        }
        if (kind == ReplaceKind.Whole)
        {
            if (from.IsPseudoText && from.MatchWords.Count == 0)
                screen.Content(from.Start, r.PseudoText1, "Pseudo-text-1 shall contain one or more text-words, at least "
                    + "one of which shall be neither a separator comma nor a separator semicolon",
                    "pseudo-text-1 holds no text-word to match");
            return;
        }
        if (from.IsPseudoText && from.MatchWords.Count != 1)
            screen.Content(from.Start, r.PartialWord1, "Partial-word-1 shall consist of one text-word",
                $"partial-word-1 holds {from.MatchWords.Count} text-words");
        if (to.IsPseudoText && to.MatchWords.Count > 1)
            screen.Content(to.Start, r.PartialWord2, "Partial-word-2 shall consist of zero or one text-word",
                $"partial-word-2 holds {to.MatchWords.Count} text-words");
        foreach (var operand in (ReadOnlySpan<Operand>)[from, to])
            if (operand.IsPseudoText && operand.MatchWords.Find(e => e.Kind == TextWordKind.Literal) is { Source: not null } literal)
                screen.Content(literal.Start, r.Literal, "An alphanumeric, boolean, or national literal shall not be "
                    + "specified as partial-word-1 or partial-word-2", $"the literal {literal.Value} is a partial-word");
    }

    /// <summary>Parse the operands of a REPLACING phrase (COPY, §7.2.3.2) or a format-1 REPLACE statement
    /// (§7.2.4.2) into <paramref name="into"/>: one or more <c>[LEADING | TRAILING] operand BY operand</c> pairs, up
    /// to (not including) the separator period, each pair screened by <see cref="ScreenOperandPair"/>.
    /// <paramref name="nonPseudoText"/> is told of each operand that is not <c>==pseudo-text==</c> (COPY: the Annex
    /// E.2 removal gate; REPLACE: COBOLNET1641); <paramref name="nestedCopy"/> (COPY only) of each COPY word inside
    /// pseudo-text (§7.2.3.3 SR1).</summary>
    private static void ParseReplacingOperands(StatementCursor c, List<Replacement> into, OperandScreen screen,
        Action<int>? nonPseudoText, Action<int>? nestedCopy)
    {
        bool any = false;
        while (c.TryPeek(out var w) && !w.IsSeparatorPeriod)
        {
            ReplaceKind kind = ReplaceKind.Whole;
            if (w.IsWord("LEADING")) { kind = ReplaceKind.Leading; c.Advance(w); }
            else if (w.IsWord("TRAILING")) { kind = ReplaceKind.Trailing; c.Advance(w); }

            if (ReadOperand(c, screen, nonPseudoText, nestedCopy) is not { } from) return;
            bool more = c.TryPeek(out var by);
            if (!more || !by.IsWord("BY"))
            {
                c.Error(more ? by.Start : c.Pos, "BY and the replacement operand must follow each REPLACING operand");
                return;
            }
            c.Advance(by);
            if (ReadOperand(c, screen, nonPseudoText, nestedCopy) is not { } to) return;
            ScreenOperandPair(screen, from, to, kind);
            into.Add(new Replacement(from.MatchWords, to.Text, kind,
                kind == ReplaceKind.Whole ? ForbiddenIn(to.Text, screen.Rules.Result) : null));
            any = true;
        }
        if (!any) c.Error(c.Pos, "the REPLACING phrase names no operands");
    }

    /// <summary>One REPLACING operand as read from its statement: its text as written (the content of
    /// <c>==pseudo-text==</c> without its delimiters, or the operand's own span), where it starts, whether it is
    /// pseudo-text, and its elements AS SCANNED IN THE STATEMENT — never re-scanned out of context, where the operand's
    /// first characters would sit at a line start (a <c>==&gt;&gt;PAGE==</c> written mid-line is a text-word, not a
    /// compiler directive line).</summary>
    private readonly record struct Operand(string Text, List<TextWord> Elements, int Start, bool IsPseudoText)
    {
        /// <summary>The text-words compared (§7.2.3.4 9) c) / §7.2.4.4 8) c): no separator comma or semicolon and no
        /// compiler directive line — each is a single space).</summary>
        public List<TextWord> MatchWords => Elements.FindAll(e => !e.IsSpaceForMatching);
    }

    /// <summary>Read one REPLACING operand (see <see cref="Operand"/>): the content of a <c>==pseudo-text==</c>
    /// (bounded by the pseudo-text-delimiter TEXT-WORDS, so an <c>==</c> inside a literal does not end it; each
    /// delimiter's separation checked against §8.3.5 6)), or — the COBOL-85 / 2002 / 2014 COPY forms (removed by ISO
    /// 2023, Annex E.2 item 1) — a literal, or an identifier / word: a word with optional OF/IN qualifiers and one
    /// balanced subscript group. Null (and a syntax error) when no operand is there.</summary>
    private static Operand? ReadOperand(StatementCursor c, OperandScreen screen, Action<int>? nonPseudoText,
        Action<int>? nestedCopy)
    {
        bool more = c.TryPeek(out var w);
        if (!more || w.IsSeparatorPeriod || w.IsWord("BY"))
        {
            c.Error(more ? w.Start : c.Pos, "a REPLACING operand is missing");
            return null;
        }

        if (w.Kind == TextWordKind.PseudoTextDelimiter)
        {
            if (w.Start > 0 && !TextWordScanner.IsSeparatorSpace(c.Text[w.Start - 1]))
                screen.Delimiter(w.Start, $"the opening == follows '{c.Text[w.Start - 1]}' with no space");
            c.Advance(w);
            var elements = new List<TextWord>();
            while (c.TryTake(out var t))
            {
                if (t.Kind == TextWordKind.PseudoTextDelimiter)
                {
                    if (t.End < c.Text.Length && c.Text[t.End] is not (',' or ';' or '.')
                        && !TextWordScanner.IsSeparatorSpace(c.Text[t.End]))
                        screen.Delimiter(t.Start, $"the closing == is followed by '{c.Text[t.End]}'");
                    return new Operand(c.Text[w.End..t.Start].Trim(TextWordScanner.SeparatorSpaces), elements, w.Start,
                        IsPseudoText: true);
                }
                if (t.IsWord("COPY")) nestedCopy?.Invoke(t.Start);
                elements.Add(t);
            }
            c.Error(w.Start, "the ==pseudo-text== has no closing delimiter");
            return null;
        }

        nonPseudoText?.Invoke(w.Start);
        if (w.Kind == TextWordKind.Literal)
        {
            c.Advance(w);
            return new Operand(w.Value, [w], w.Start, IsPseudoText: false);
        }
        if (w.Kind != TextWordKind.CharacterString)
        {
            c.Error(w.Start, $"'{w.Value}' is not a REPLACING operand");
            return null;
        }

        // identifier-1/2 or word-1/2: a data-name with optional OF/IN qualifiers and an optional subscript —
        // e.g. WRK IN GRP-002 (1). A plain word (including a signed number such as +2) is the degenerate
        // single-text-word case. The verbatim span is returned with the text-words it holds: those are compared, and
        // as a replacement it is inserted as written.
        int start = w.Start;
        var words = new List<TextWord> { w };
        c.Advance(w);
        while (c.TryPeek(out var q) && (q.IsWord("OF") || q.IsWord("IN")))
        {
            c.Advance(q);
            words.Add(q);
            if (c.TryPeek(out var qualifier) && qualifier.Kind == TextWordKind.CharacterString)
            {
                c.Advance(qualifier);
                words.Add(qualifier);
            }
        }
        if (c.TryPeek(out var open) && open.Kind == TextWordKind.Separator && open.Span[0] == '(')
        {
            int depth = 0;
            while (c.TryTake(out var t))
            {
                words.Add(t);
                if (t.Kind != TextWordKind.Separator) continue;
                if (t.Span[0] == '(') depth++;
                else if (t.Span[0] == ')' && --depth == 0) break;
            }
        }
        return new Operand(c.Text[start..c.Pos], words, start, IsPseudoText: false);
    }

    /// <summary>The places searched for library text, in order — THE default COBOL library of the DOC-A.1-40
    /// determination (§7.2.3.4 GR3 "The implementor defines the mechanism for identifying the default COBOL
    /// library"; GnuCOBOL's order, CLAUDE.md rule 1): the compiler process's current working directory, then each
    /// configured search path (<c>--copy DIR</c> in command-line order, then the <c>--nist</c> copylib) — and nothing
    /// else, so the source file's own directory is searched only when it is one of these.</summary>
    private IEnumerable<string> LibraryPlaces()
    {
        yield return _inputs.GetWorkingDirectory();
        foreach (var searchPath in _searchPaths) yield return searchPath;
    }

    /// <summary>The library text <paramref name="textName"/> names in the directory <paramref name="library"/>: the
    /// name as spelled, then — unless its FILE NAME (the part after the last directory separator) already contains a
    /// period — with each of <see cref="CopybookSuffixes"/> in order; the FIRST regular file that exists IS the
    /// library text, so a text-name names exactly one text in a library (§7.2.3.3 SR3 "Within one COBOL library,
    /// each text-name shall be unique"). A period in a directory part (<c>"../lib/BOOK"</c>) is not an extension, so
    /// that name still takes the suffixes. Null when the library holds no such text.</summary>
    private string? LocateInLibrary(string library, string textName)
    {
        string spelled = Path.Combine(library, textName);   // an absolute name stays as written
        if (_inputs.FileExists(spelled)) return spelled;
        if (Path.GetFileName(textName.AsSpan()).Contains('.')) return null;
        foreach (var suffix in CopybookSuffixes)
            if (_inputs.FileExists(spelled + suffix)) return spelled + suffix;
        return null;
    }

    /// <summary>Locate the library text a COPY statement names (§7.2.3.4 GR1–GR3; the DOC-A.1-40 determination in
    /// docs/CONFORMANCE.md §7). Without OF/IN each place of <see cref="LibraryPlaces"/> is a candidate library, tried
    /// in order. With OF/IN, library-name-1 (or literal-2's value) names a subdirectory: the FIRST place holding a
    /// subdirectory of that name IS the library, and the text is located in it alone — GR2 "Library-name-1 names a
    /// resource that shall be available to the compiler and shall provide access to the library text", so there is
    /// no fallback to another library. Null when no text is found (the caller reports CBL3620).</summary>
    private string? FindCopybook(string textName, string? libraryName)
    {
        if (!string.IsNullOrEmpty(libraryName))
        {
            foreach (var place in LibraryPlaces())
            {
                string library = Path.Combine(place, libraryName);
                if (_inputs.DirectoryExists(library)) return LocateInLibrary(library, textName);
            }
            return null;
        }
        foreach (var place in LibraryPlaces())
            if (LocateInLibrary(place, textName) is { } found) return found;
        return null;
    }
}
