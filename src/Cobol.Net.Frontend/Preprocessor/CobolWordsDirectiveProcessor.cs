// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The WiseOwl COBOL <c>&gt;&gt;COBOL-WORDS</c> directive stage (ISO §7.3.10; Annex D.12; Annex E.3.3 item 12;
/// greenfield-only — the legacy pipeline keeps consuming the word via
/// <see cref="ConditionalCompilationProcessor"/>'s <c>KnownIgnoredDirectives</c>): parses each surviving
/// <c>&gt;&gt;COBOL-WORDS { EQUATE l1 WITH l2 | UNDEFINE l3 | SUBSTITUTE l4 BY l5 | RESERVE l6 }</c> line of the
/// FINAL preprocessed text into a <see cref="CobolWordsOp"/>, edition-gates the directive word (a COBOL-2023
/// addition — the introduction gate routed through the ONE <see cref="ConstructRegistry"/>), enforces the
/// text-stage syntax rules SR1/SR2/SR5, and blanks the line (line-count preserving — the <c>&gt;&gt;TURN</c> H3
/// discipline). The resulting <see cref="CobolWordsMap"/> is the per-group override layer the post-lex
/// <c>CobolWordsRewriter</c> and the compiler's <see cref="ReservedWordSet"/> / intrinsic
/// resolution consult. SR3/SR4 (the reserved/context/intrinsic CATEGORY of each word) are validated later in the
/// compiler, where all three registries are reachable. Design SSOT:
/// <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.
/// </summary>
public static class CobolWordsDirectiveProcessor
{
    private const string Keyword = "COBOL-WORDS";   // 11 characters

    /// <summary>Process <paramref name="text"/>: edition-gate each <c>&gt;&gt;COBOL-WORDS</c> line, parse its
    /// option, enforce SR1/SR2/SR5, and blank the directive lines. Returns the composed override map (empty when
    /// no directive is present). Line-count preserving.</summary>
    /// <para><paramref name="stackOps"/> are the PUSH/POP directives of the same text (§7.3.20 / §7.3.22; kb/Work
    /// PB941), replayed over the entries up to the first IDENTIFICATION DIVISION: a POP COBOL-WORDS there
    /// withdraws every entry written since its PUSH, and the map is the set in effect where §7.3.10.3 SR1 closes
    /// the region.</para>
    public static (string Text, CobolWordsMap Map) Process(
        string text, DiagnosticBag diagnostics, string sourcePath, SourceLineMap? lineMap = null,
        IReadOnlyList<DirectiveStackOp>? stackOps = null)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, CobolWordsMap.Empty);
        var lines = text.Split('\n');
        var ops = new List<CobolWordsOp>();
        // SR5 (§7.3.10.3 / D.12.1): a COBOL word may be contained in a literal of at most ONE directive in the
        // group (the modified word AND its substitute both count). First occurrence wins; a repeat is the error.
        var seenWords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        bool sawFirstIdDivision = false;
        var state = new DirectiveStateStack(stackOps ?? []).Carry(Constructs.CobolWordsDirective2023,
            new DirectiveValueCarrier<int>(() => ops.Count, keep => ops.RemoveRange(keep, ops.Count - keep)));

        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd('\r').TrimStart();
            // SR1 boundary: the FIRST IDENTIFICATION DIVISION ends the region where COBOL-WORDS is legal — and its
            // header is optional (§11.2.1), so the boundary is the ONE unit-start test (kb/Work PB829).
            if (!trimmed.StartsWith(">>", StringComparison.Ordinal))
            {
                if (!sawFirstIdDivision && CompilationUnitStart.IsAt(trimmed))
                {
                    sawFirstIdDivision = true;
                    state.AdvanceTo(i + 1);   // the PUSH/POP of the region act on the entries; later ones cannot
                }
                continue;
            }
            // The ONE compiler-directive line parse (kb/Work PB794): the indicator's optional space (§7.3.3 SR5)
            // and the trailing inline comment (SR3/SR4) are its rules, not this stage's — `>>COBOL-WORDS
            // RESERVE "ZQX" *> why` used to be rejected as a malformed entry list.
            if (!CompilerDirectiveLine.TryParse(lines[i], Keyword, out string operand)) continue;
            if (!sawFirstIdDivision) state.AdvanceTo(i + 1);   // the PUSH/POP written before this directive

            var loc = lineMap?.Locate(i + 1, sourcePath) ?? new SourceLocation(sourcePath, 0, i, 0);   // the SOURCE origin of resultant line i (kb/Work PB82)

            // The introduction gate (§7.3.10 is a COBOL-2023 addition, Annex E.3.3 item 12) already fired at
            // the ONE directive-recognition point — CompilerDirectiveCatalog, from the cobol-words-directive-2023
            // row's directiveWords (kb/Work PB725). This stage parses; it does not re-decide the edition.

            // SR1 — a COBOL-WORDS directive after the first IDENTIFICATION DIVISION is illegal (§7.3.10.3 SR1).
            if (sawFirstIdDivision)
                Invalid(diagnostics, loc,
                    ">>COBOL-WORDS may be specified only before the first IDENTIFICATION DIVISION (ISO §7.3.10.3 SR1)");

            if (TryParseOption(operand, i, diagnostics, loc, out var op))
            {
                // SR5 — every literal's content across all directives is unique.
                foreach (string w in Words(op))
                {
                    if (seenWords.TryGetValue(w, out _))
                        Invalid(diagnostics, loc,
                            $"the COBOL word '{w}' is used in more than one >>COBOL-WORDS directive (ISO §7.3.10.3 SR5)");
                    else
                        seenWords[w] = i;
                }
                ops.Add(op);
            }

            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        if (!sawFirstIdDivision) state.AdvanceToEnd();   // no unit at all: the state at the end of the text
        return (string.Join('\n', lines), ops.Count == 0 ? CobolWordsMap.Empty : new CobolWordsMap(ops));
    }

    /// <summary>The words a directive contributes to the SR5 uniqueness multiset (both operands).</summary>
    private static IEnumerable<string> Words(CobolWordsOp op)
    {
        if (op.Existing is { } e) yield return e;
        if (op.New is { } n) yield return n;
    }

    /// <summary>Parse the operand after <c>COBOL-WORDS</c> into one option (§7.3.10.2), enforcing SR2 per literal
    /// and the §8.3.2.2 user-word form for the fresh word. Returns false (with a COBOLNET1623) when malformed.</summary>
    private static bool TryParseOption(string operand, int line, DiagnosticBag diag, SourceLocation loc,
        out CobolWordsOp op)
    {
        op = null!;
        var toks = Tokenize(operand);
        if (toks.Count == 0)
        {
            Invalid(diag, loc, ">>COBOL-WORDS requires an EQUATE, UNDEFINE, SUBSTITUTE, or RESERVE option (ISO §7.3.10.2)");
            return false;
        }
        var kw = toks[0];
        if (kw.IsLiteral)
        {
            Invalid(diag, loc, ">>COBOL-WORDS requires an EQUATE, UNDEFINE, SUBSTITUTE, or RESERVE option (ISO §7.3.10.2)");
            return false;
        }
        switch (kw.Text.ToUpperInvariant())
        {
            case "EQUATE":   // EQUATE literal-1 WITH literal-2
                return TryBinary(toks, "WITH", CobolWordsAction.Equate, line, diag, loc, out op);
            case "SUBSTITUTE":   // SUBSTITUTE literal-4 BY literal-5
                return TryBinary(toks, "BY", CobolWordsAction.Substitute, line, diag, loc, out op);
            case "UNDEFINE":   // UNDEFINE literal-3
                return TryUnary(toks, CobolWordsAction.Undefine, isExisting: true, line, diag, loc, out op);
            case "RESERVE":   // RESERVE literal-6
                return TryUnary(toks, CobolWordsAction.Reserve, isExisting: false, line, diag, loc, out op);
            default:
                Invalid(diag, loc,
                    $"'{kw.Text}' is not a >>COBOL-WORDS option — expected EQUATE, UNDEFINE, SUBSTITUTE, or RESERVE (ISO §7.3.10.2)");
                return false;
        }
    }

    /// <summary>EQUATE/SUBSTITUTE: <c>KW literal-a JOIN literal-b</c> (the existing word then the fresh word).</summary>
    private static bool TryBinary(IReadOnlyList<Tok> toks, string join, CobolWordsAction action, int line,
        DiagnosticBag diag, SourceLocation loc, out CobolWordsOp op)
    {
        op = null!;
        if (toks.Count != 4 || !toks[2].IsKeyword(join))
        {
            Invalid(diag, loc, $">>COBOL-WORDS {toks[0].Text.ToUpperInvariant()} expects "
                + $"literal-1 {join} literal-2 (ISO §7.3.10.2)");
            return false;
        }
        if (!Literal(toks[1], diag, loc, "the existing word", out string existing)) return false;
        if (!Literal(toks[3], diag, loc, "the new word", out string @new)) return false;
        if (!UserWord(@new, diag, loc)) return false;
        op = new CobolWordsOp(action, existing, @new, line);
        return true;
    }

    /// <summary>UNDEFINE/RESERVE: <c>KW literal</c>.</summary>
    private static bool TryUnary(IReadOnlyList<Tok> toks, CobolWordsAction action, bool isExisting, int line,
        DiagnosticBag diag, SourceLocation loc, out CobolWordsOp op)
    {
        op = null!;
        if (toks.Count != 2)
        {
            Invalid(diag, loc, $">>COBOL-WORDS {toks[0].Text.ToUpperInvariant()} expects a single literal (ISO §7.3.10.2)");
            return false;
        }
        if (!Literal(toks[1], diag, loc, isExisting ? "the word" : "the new word", out string word)) return false;
        // RESERVE's operand is a fresh user word (SR4 §8.3.2.2 form); UNDEFINE's is an existing word (no form check).
        if (!isExisting && !UserWord(word, diag, loc)) return false;
        op = isExisting
            ? new CobolWordsOp(action, word, null, line)
            : new CobolWordsOp(action, null, word, line);
        return true;
    }

    /// <summary>SR2: a plain alphanumeric literal (quoted, non-hex/national, space-free). Returns the UPPER-CASE
    /// content.</summary>
    private static bool Literal(Tok tok, DiagnosticBag diag, SourceLocation loc, string role, out string content)
    {
        content = "";
        if (!tok.IsLiteral)
        {
            Invalid(diag, loc, $">>COBOL-WORDS: {role} must be an alphanumeric literal (ISO §7.3.10.3 SR2), not '{tok.Text}'");
            return false;
        }
        if (tok.Prefix.Length != 0)
        {
            Invalid(diag, loc, $">>COBOL-WORDS: {role} must be a plain alphanumeric literal, not a "
                + $"{tok.Prefix.ToUpperInvariant()}-prefixed literal (ISO §7.3.10.3 SR2)");
            return false;
        }
        if (tok.Text.Length == 0 || tok.Text.Contains(' '))
        {
            Invalid(diag, loc, $">>COBOL-WORDS: {role} literal must be a non-empty, space-free COBOL word (ISO §7.3.10.3 SR2)");
            return false;
        }
        content = tok.Text.ToUpperInvariant();
        return true;
    }

    /// <summary>SR4 (frontend half): the fresh word (literal-2/5/6) is a well-formed user-defined word per
    /// §8.3.2.2 — letters/digits/hyphens, at least one letter or digit, no leading/trailing hyphen. (Whether it is
    /// nonetheless reserved/context/intrinsic — the SR4 category bar — is checked in the compiler.)</summary>
    private static bool UserWord(string word, DiagnosticBag diag, SourceLocation loc)
    {
        bool ok = word.Length > 0 && word[0] != '-' && word[^1] != '-'
            && word.All(c => char.IsLetterOrDigit(c) || c == '-')
            && word.Any(char.IsLetterOrDigit);
        if (!ok)
            Invalid(diag, loc,
                $">>COBOL-WORDS: '{word}' is not a valid user-defined word (ISO §7.3.10.3 SR4 / §8.3.2.2)");
        return ok;
    }

    private static void Invalid(DiagnosticBag diag, SourceLocation loc, string message) =>
        diag.ReportError(DiagnosticCatalog.CobolWordsDirectiveInvalid.Code, message, loc, default);

    // ── operand tokenizer ────────────────────────────────────────────────────────────────────────────────────
    private readonly record struct Tok(string Text, bool IsLiteral, string Prefix)
    {
        public bool IsKeyword(string kw) => !IsLiteral && Text.Equals(kw, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Split the operand into barewords and quoted literals. A letter run immediately followed by a quote
    /// (no space) is captured as a PREFIXED literal (e.g. <c>X"AB"</c>) so SR2 can reject it precisely.</summary>
    private static List<Tok> Tokenize(string s)
    {
        var toks = new List<Tok>();
        int i = 0;
        while (i < s.Length)
        {
            if (char.IsWhiteSpace(s[i])) { i++; continue; }
            if (s[i] is '"' or '\'')
            {
                i = ReadQuoted(s, i, prefix: "", toks);
                continue;
            }
            // a bareword; if it butts directly against a quote, it is a literal prefix (X"…", N"…", …)
            int start = i;
            while (i < s.Length && !char.IsWhiteSpace(s[i]) && s[i] is not ('"' or '\'')) i++;
            string word = s[start..i];
            if (i < s.Length && s[i] is '"' or '\'')
                i = ReadQuoted(s, i, prefix: word, toks);
            else
                toks.Add(new Tok(word, IsLiteral: false, Prefix: ""));
        }
        return toks;
    }

    /// <summary>Read a quoted literal starting at <paramref name="q"/> (its opening quote); returns the index past
    /// the closing quote (or end of string when unterminated). Emits a Literal token carrying its content.</summary>
    private static int ReadQuoted(string s, int q, string prefix, List<Tok> toks)
    {
        char quote = s[q];
        int j = q + 1;
        while (j < s.Length && s[j] != quote) j++;
        string content = s[(q + 1)..Math.Min(j, s.Length)];
        toks.Add(new Tok(content, IsLiteral: true, Prefix: prefix));
        return j < s.Length ? j + 1 : j;   // step past the closing quote
    }
}
