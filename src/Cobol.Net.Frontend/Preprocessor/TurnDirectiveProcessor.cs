// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>One <c>&gt;&gt;TURN</c> directive event (ISO/IEC 1989:2023 §7.3.25), anchored to its 1-based line in
/// the FINAL preprocessed text — directly comparable to an ANTLR token's <c>Start.Line</c>. The compile-time
/// TurnState folds these events to decide WHETHER an EC guard is emitted at each statement (deep-dive D10).</summary>
/// <param name="Line">The directive's 1-based line in the final preprocessed text. A directive applies to
/// SUCCEEDING statements only (§7.3.25.4 GR5); strict <c>eventLine &lt; statementLine</c> realizes that because a
/// directive occupies its own whole line after free-form normalization.</param>
/// <param name="Names">The (exception-name, file-name-or-null) pairs the directive toggles, pre-expansion —
/// EC-ALL / level-2 expansion (GR2/GR3) is the TurnState's matching predicate, not stored pairs.</param>
/// <param name="On">CHECKING ON vs OFF.</param>
/// <param name="WithLocation">ON WITH LOCATION (GR7 — saves EXCEPTION-LOCATION/-STATEMENT information).</param>
public sealed record TurnEvent(int Line, IReadOnlyList<(string Ec, string? File)> Names, bool On, bool WithLocation);

/// <summary>
/// The WiseOwl COBOL <c>&gt;&gt;TURN</c> directive stage (ISO §7.3.25; greenfield-only — the legacy pipeline keeps
/// consuming TURN in <see cref="ConditionalCompilationProcessor"/>): parses each surviving <c>&gt;&gt;TURN</c>
/// line of the FINAL preprocessed text into a <see cref="TurnEvent"/>, enforces the directive's syntax rules
/// (SR3 — no duplicate (exception-name, file-name) pair per directive; SR4 — a file-name only with an
/// <c>EC-I-O…</c> name), and blanks the line (line-count preserving, so every later token line number is stable
/// — scout hazard H3: the stage asserts the line count is unchanged).
/// </summary>
public static class TurnDirectiveProcessor
{
    /// <summary>Process <paramref name="text"/>: collect the TURN events, blank the directive lines. At
    /// <paramref name="dialectLevel"/> &lt; 2002 a <c>&gt;&gt;TURN</c> is the four-compilers diagnostic (the EC
    /// model is 2002+ — COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN per-edition gating), never silently ignored.</summary>
    /// <para><paramref name="stackOps"/> are the PUSH/POP directives of the same text (§7.3.20 / §7.3.22), replayed
    /// over this stage's events so a POP revokes the toggles written since its PUSH (kb/Work PB941).</para>
    public static (string Text, DirectiveTimeline<TurnEvent> Events) Process(
        string text, int dialectLevel, DiagnosticBag diagnostics, string sourcePath, SourceLineMap? lineMap = null,
        IReadOnlyList<DirectiveStackOp>? stackOps = null)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, DirectiveTimeline<TurnEvent>.Empty);
        var lines = text.Split('\n');
        var events = new DirectiveEventLog<TurnEvent>();
        for (int i = 0; i < lines.Length; i++)
        {
            // The ONE compiler-directive line parse (kb/Work PB794): the indicator's optional space (§7.3.3 SR5)
            // and the trailing inline comment (SR3/SR4) are its rules, not this stage's.
            if (!CompilerDirectiveLine.TryParse(lines[i], "TURN", out string operand)) continue;

            var loc = lineMap?.Locate(i + 1, sourcePath) ?? new SourceLocation(sourcePath, 0, i, 0);   // the SOURCE origin of resultant line i (kb/Work PB82)
            // The introduction gate fired at the ONE directive-recognition point (CompilerDirectiveCatalog,
            // from the turn-directive-2002 row) — this stage ran its own `if (dialectLevel < 2002)` with a
            // BESPOKE COBOLNET0875 until kb/Work PB725 reconciled the three mechanisms onto the registry's
            // COBOLNET0900. COBOLNET0875 is RETIRED; never reallocate it. dialectLevel survives here because
            // ParseTurn passes it to the exception-name edition window (§14.6.13.1), a different rule.
            if (ParseTurn(operand, i + 1, dialectLevel, diagnostics, loc) is { } ev)
                events.Add(Constructs.TurnDirective2002, i + 1, ev);
            lines[i] = "";   // blank, never delete — line-count preserving (H3)
        }
        return (string.Join('\n', lines), events.ToTimeline(stackOps ?? []));
    }

    /// <summary>Parse one directive body: <c>{ec-name [file-name]…}… CHECKING {ON [WITH LOCATION] | OFF}</c>
    /// (§7.3.25.2). SR1: any word starting <c>EC-</c> is an exception-name, not a file-name. Null on a malformed
    /// directive (reported).</summary>
    private static TurnEvent? ParseTurn(string body, int line, int dialectLevel,
        DiagnosticBag diagnostics, SourceLocation loc)
    {
        var words = body.SplitSpaces();
        var names = new List<(string Ec, string? File)>();
        var seen = new HashSet<string>(CobolNames.Comparer);
        bool? on = null;
        bool withLocation = false;
        int checkingEnd = -1;   // the index just past the last word the CHECKING phrase consumed

        // §8.3.2.1 applies to DIRECTIVE-carried words too — the tree-walk funnel never sees them, so this was
        // the hole through which a 44-character exception-name compiled at --std 2002 (CobolWordRule; the R05
        // fact that found it). One check per operand word, exception-names and file-names alike.
        void CheckWord(string w)
        {
            foreach (var (code, violation) in Common.CobolWordRule.DirectiveWordViolations(w, dialectLevel))
                diagnostics.ReportError(code, violation, loc, default);
        }

        int k = 0;
        string? currentEc = null;
        bool currentEcHasFiles = false;
        void Flush()
        {
            // An EC name with no trailing file-names is one (ec, null) pair.
            if (currentEc is not null && !currentEcHasFiles) AddPair(currentEc, null);
            currentEc = null;
            currentEcHasFiles = false;
        }
        void AddPair(string ec, string? file)
        {
            if (!seen.Add(ec + "|" + (file ?? "")))
                diagnostics.ReportError("COBOLNET0718",
                    $">>TURN: the exception-name/file-name combination '{ec}{(file is null ? "" : " " + file)}' is "
                    + "specified more than once in this directive (ISO §7.3.25.3 SR3)", loc, default);
            else
                names.Add((ec.ToUpperInvariant(), file?.ToUpperInvariant()));
        }

        for (; k < words.Length; k++)
        {
            string w = words[k];
            if (CobolNames.Same(w, "CHECKING"))
            {
                Flush();
                // { ON [ WITH LOCATION ] | OFF } — the printed diagram (PDF page 115) underlines OFF and LOCATION and
                // NOTHING ELSE: ON and WITH are optional words (§5.2.3), so an alternative made of them is omissible
                // and `CHECKING` alone, `CHECKING WITH LOCATION` and `CHECKING LOCATION` select ON, which §7.3.25.4 GR6
                // says in as many words: "If the ON phrase is specified or implied" (kb/Work PB1365).
                int q = k + 1;
                if (q < words.Length && CobolNames.Same(words[q], "OFF"))
                {
                    on = false;
                    q++;
                }
                else
                {
                    on = true;
                    if (q < words.Length && CobolNames.Same(words[q], "ON")) q++;
                    int r = q;
                    if (r < words.Length && CobolNames.Same(words[r], "WITH")) r++;
                    if (r < words.Length && CobolNames.Same(words[r], "LOCATION"))
                    {
                        withLocation = true;   // GR7
                        q = r + 1;
                    }
                }
                checkingEnd = q;
                break;
            }
            if (CobolNames.StartsWith(w, "EC-"))   // SR1 — an EC- word is an exception-name
            {
                Flush();
                CheckWord(w);
                currentEc = w;
            }
            else if (currentEc is not null)
            {
                // A non-EC word after an exception-name is a file-name (SR1) — unless it duplicates a compiler-directive
                // word (§8.12), which SR1's second sentence interprets as that directive word: ON, OFF, WITH, LOCATION,
                // ALL… can never be file-name-1, so the directive matches no format (kb/Work PB1365).
                if (CompilerDirectiveWords.IsReserved(w))
                {
                    diagnostics.ReportError("COBOLNET0718",
                        $">>TURN: '{w}' duplicates a compiler-directive word, so it is interpreted as that directive "
                        + "word and not as file-name-1, and the directive matches no format (ISO §7.3.25.3 SR1; §8.12)",
                        loc, default);
                    return null;
                }
                // SR4 — a file-name only with an EC-I-O… name.
                if (!CobolNames.StartsWith(currentEc, "EC-I-O"))
                    diagnostics.ReportError("COBOLNET0719",
                        $">>TURN: file-name '{w}' specified with exception-name '{currentEc}', which does not begin "
                        + "with 'EC-I-O' (ISO §7.3.25.3 SR4)", loc, default);
                else
                {
                    CheckWord(w);
                    AddPair(currentEc, w);
                    currentEcHasFiles = true;
                }
            }
            else
            {
                diagnostics.ReportError("COBOLNET0718",
                    $">>TURN: unexpected word '{w}' — expected an exception-name (a word beginning 'EC-', "
                    + "ISO §7.3.25.3 SR1)", loc, default);
                return null;
            }
        }

        // ⛔ THE DIRECTIVE ENDS AT THE CHECKING PHRASE (kb/Work PB1365). §7.3.3 SR3: a directive "may be followed only by
        // space characters and an optional inline comment". TURN is owned by THIS stage, so the shared closed-word screen
        // (CompilerDirectiveCatalog.CheckOperand, COBOLNET1911) skips it by design — and the loop above `break`s at CHECKING,
        // so a word after ON / OFF [WITH LOCATION] was never looked at (`>>TURN EC-ALL CHECKING OFF JUNK` took effect).
        if (checkingEnd >= 0 && checkingEnd < words.Length)
        {
            diagnostics.ReportError("COBOLNET0718",
                $">>TURN: unexpected word '{words[checkingEnd]}' after the CHECKING phrase — the directive ends at "
                + "CHECKING {ON [WITH LOCATION] | OFF} (ISO §7.3.25.2), and may be followed only by space characters "
                + "and an optional inline comment (ISO §7.3.3 SR3)", loc, default);
            return null;
        }

        if (on is null || names.Count == 0)
        {
            diagnostics.ReportError("COBOLNET0718",
                ">>TURN: malformed directive — the format is '>>TURN {exception-name [file-name]…}… CHECKING "
                + "{ON [WITH LOCATION] | OFF}' (ISO §7.3.25.2)", loc, default);
            return null;
        }
        return new TurnEvent(line, names, on.Value, withLocation);
    }
}
