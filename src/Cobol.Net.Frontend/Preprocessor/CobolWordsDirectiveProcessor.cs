// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Expressions;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The WiseOwl COBOL <c>&gt;&gt;COBOL-WORDS</c> directive stage (ISO §7.3.10; Annex D.12; Annex E.3.3 item 12;
/// greenfield-only — the legacy pipeline keeps consuming the word via
/// <see cref="ConditionalCompilationProcessor"/>'s <c>KnownIgnoredDirectives</c>): parses each surviving
/// <c>&gt;&gt;COBOL-WORDS { EQUATE l1 WITH l2 | UNDEFINE l3 | SUBSTITUTE l4 BY l5 | RESERVE l6 }</c> line of the
/// FINAL preprocessed text into a <see cref="CobolWordsOp"/>, edition-gates the directive word (a COBOL-2023
/// addition — the introduction gate routed through the ONE <see cref="ConstructRegistry"/>), enforces the
/// text-stage syntax rules SR1/SR2/SR5, and blanks the line (line-count preserving — the <c>&gt;&gt;TURN</c> H3
/// discipline). The resulting <see cref="CobolWordsMap"/> is the per-group override layer the lexer-applied
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
    /// <para><paramref name="compilationVariables"/> is the &gt;&gt;DEFINE timeline of the same text (kb/Work PB1368):
    /// §7.3.11.4 GR1 lets a compilation-variable-name defined before a directive stand in any of its literal slots
    /// where a literal of the name's category is permitted, and each literal of this directive is alphanumeric
    /// (§7.3.10.3 SR2).</para>
    /// <para>The third result is <c>FirstUnitLine</c>: the 1-based line on which the group's first compilation unit
    /// begins, <see cref="int.MaxValue"/> when none does — §7.3.10.3 SR1's boundary, decided HERE because this is the one
    /// stage that holds what decides it: a unit's header may be spelled with a word the group's own
    /// <c>&gt;&gt;COBOL-WORDS EQUATE</c> made a synonym (<c>IDENT DIVISION.</c>, §7.3.10.4 GR2), and only the entries in
    /// effect before the line can say so (<see cref="CompilationUnitStart.IsAt(string, CobolWordsMap)"/>). The placement
    /// rows that name this boundary are judged against it by <see cref="DirectiveSiteProcessor.JudgeFirstUnitPlacement"/>.</para>
    public static (string Text, CobolWordsMap Map, int FirstUnitLine) Process(
        string text, DiagnosticBag diagnostics, string sourcePath, SourceLineMap? lineMap = null,
        IReadOnlyList<DirectiveStackOp>? stackOps = null,
        DirectiveTimeline<CompilationVariableEvent>? compilationVariables = null)
    {
        var definitions = compilationVariables ?? DirectiveTimeline<CompilationVariableEvent>.Empty;
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, CobolWordsMap.Empty, CompilationUnitStart.FirstLine(text));
        var lines = text.Split('\n');
        var ops = new List<CobolWordsOp>();
        // SR5 (§7.3.10.3 / D.12.1): a COBOL word may be contained in a literal of at most ONE directive in the
        // group (the modified word AND its substitute both count). First occurrence wins; a repeat is the error.
        var seenWords = new Dictionary<string, int>(CobolNames.Comparer);
        bool sawFirstIdDivision = false;
        int firstUnitLine = int.MaxValue;
        var state = new DirectiveStateStack(stackOps ?? []).Carry(Constructs.CobolWordsDirective2023,
            new DirectiveValueCarrier<int>(() => ops.Count, keep => ops.RemoveRange(keep, ops.Count - keep)));

        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimSpacesStart();
            // SR1 boundary: the FIRST IDENTIFICATION DIVISION ends the region where COBOL-WORDS is legal — and its
            // header is optional (§11.2.1), so the boundary is the ONE unit-start test (kb/Work PB829), asked of the
            // line as the group's own synonyms read it (kb/Work PB1373): `IDENT DIVISION.` after `>>COBOL-WORDS EQUATE
            // "IDENTIFICATION" WITH "IDENT"` is the identification division header.
            if (!trimmed.StartsWith(">>", StringComparison.Ordinal))
            {
                if (!sawFirstIdDivision)
                {
                    bool atUnit = CompilationUnitStart.IsAt(trimmed);
                    if (!atUnit && ops.Count > 0)
                    {
                        state.AdvanceTo(i + 1);   // the entries in effect HERE
                        atUnit = CompilationUnitStart.IsAt(trimmed, new CobolWordsMap(ops));
                    }
                    if (atUnit)
                    {
                        sawFirstIdDivision = true;
                        firstUnitLine = i + 1;
                        state.AdvanceTo(i + 1);   // the PUSH/POP of the region act on the entries; later ones cannot
                    }
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

            // §7.3.10.3 SR1 — a COBOL-WORDS directive after the first IDENTIFICATION DIVISION is illegal — is judged by
            // the ONE placement screen, from the row's directivePlacement data (DirectiveSiteProcessor.
            // JudgeFirstUnitPlacement, COBOLNET2652, kb/Work PB1377), for the directive itself and for a PUSH/POP naming
            // it, against the FirstUnitLine this stage returns. The boundary is also STATE: the map is the set in effect
            // where the region closes.

            int directiveLine = i + 1;   // resultant, 1-based: the frame the DEFINE events carry
            if (TryParseOption(operand, i, diagnostics, loc, name => definitions.DefinitionAt(name, directiveLine), out var op))
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
        return (string.Join('\n', lines), ops.Count == 0 ? CobolWordsMap.Empty : new CobolWordsMap(ops), firstUnitLine);
    }

    /// <summary>The words a directive contributes to the SR5 uniqueness multiset (both operands).</summary>
    private static IEnumerable<string> Words(CobolWordsOp op)
    {
        if (op.Existing is { } e) yield return e;
        if (op.New is { } n) yield return n;
    }

    /// <summary>Parse the operand after <c>COBOL-WORDS</c> into one option (§7.3.10.2), enforcing SR2 per literal
    /// and the §8.3.2.2 user-word form for the fresh word. Returns false (with a COBOLNET1623) when malformed.
    /// <para><paramref name="definitionOf"/> reads the DEFINE timeline for a compilation-variable-name in a literal
    /// slot; null is the SYNTAX-ONLY parse of <see cref="CheckOperand"/>, where such a name is accepted by its shape
    /// and no word is looked up.</para></summary>
    private static bool TryParseOption(string operand, int line, DiagnosticBag diag, SourceLocation loc,
        Func<string, CompilationVariableEvent?>? definitionOf, out CobolWordsOp op)
    {
        op = null!;
        var toks = Tokenize(operand);
        foreach (var tok in toks)
            if (tok.Defect is { } defect)
            {
                Invalid(diag, loc, $">>COBOL-WORDS: the literal \"{tok.Text}\" is not an alphanumeric literal — {defect} "
                    + "(ISO §7.3.10.3 SR2)");
                return false;
            }
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
                return TryBinary(toks, "WITH", CobolWordsAction.Equate, line, diag, loc, definitionOf, out op);
            case "SUBSTITUTE":   // SUBSTITUTE literal-4 BY literal-5
                return TryBinary(toks, "BY", CobolWordsAction.Substitute, line, diag, loc, definitionOf, out op);
            case "UNDEFINE":   // UNDEFINE literal-3
                return TryUnary(toks, CobolWordsAction.Undefine, isExisting: true, line, diag, loc, definitionOf, out op);
            case "RESERVE":   // RESERVE literal-6
                return TryUnary(toks, CobolWordsAction.Reserve, isExisting: false, line, diag, loc, definitionOf, out op);
            default:
                Invalid(diag, loc,
                    $"'{kw.Text}' is not a >>COBOL-WORDS option — expected EQUATE, UNDEFINE, SUBSTITUTE, or RESERVE (ISO §7.3.10.2)");
                return false;
        }
    }

    /// <summary>EQUATE/SUBSTITUTE: <c>KW literal-a JOIN literal-b</c> (the existing word then the fresh word).</summary>
    private static bool TryBinary(IReadOnlyList<Tok> toks, string join, CobolWordsAction action, int line,
        DiagnosticBag diag, SourceLocation loc, Func<string, CompilationVariableEvent?>? definitionOf, out CobolWordsOp op)
    {
        op = null!;
        if (toks.Count != 4 || !toks[2].IsKeyword(join))
        {
            Invalid(diag, loc, $">>COBOL-WORDS {toks[0].Text.ToUpperInvariant()} expects "
                + $"literal-1 {join} literal-2 (ISO §7.3.10.2)");
            return false;
        }
        if (!Literal(toks[1], diag, loc, "the existing word", definitionOf, out string existing, out _)) return false;
        if (!Literal(toks[3], diag, loc, "the new word", definitionOf, out string @new, out bool newIsWritten)) return false;
        if (newIsWritten && !UserWord(@new, diag, loc)) return false;
        op = new CobolWordsOp(action, existing, @new, line);
        return true;
    }

    /// <summary>UNDEFINE/RESERVE: <c>KW literal</c>.</summary>
    private static bool TryUnary(IReadOnlyList<Tok> toks, CobolWordsAction action, bool isExisting, int line,
        DiagnosticBag diag, SourceLocation loc, Func<string, CompilationVariableEvent?>? definitionOf, out CobolWordsOp op)
    {
        op = null!;
        if (toks.Count != 2)
        {
            Invalid(diag, loc, $">>COBOL-WORDS {toks[0].Text.ToUpperInvariant()} expects a single literal (ISO §7.3.10.2)");
            return false;
        }
        if (!Literal(toks[1], diag, loc, isExisting ? "the word" : "the new word", definitionOf, out string word,
                out bool isWritten)) return false;
        // RESERVE's operand is a fresh user word (SR4 §8.3.2.2 form); UNDEFINE's is an existing word (no form check).
        if (!isExisting && isWritten && !UserWord(word, diag, loc)) return false;
        op = isExisting
            ? new CobolWordsOp(action, word, null, line)
            : new CobolWordsOp(action, null, word, line);
        return true;
    }

    /// <summary>SR2: a plain alphanumeric literal (quoted, non-hex/national, space-free). Returns the UPPER-CASE
    /// content. A WORD in the slot is a compilation-variable-name (§7.3.11.4 GR1: "In text that follows a DEFINE
    /// directive specifying compilation-variable-name-1 without the OFF phrase, compilation-variable-name-1 may be used
    /// … in any compiler directive where a literal of the category associated with the name is permitted"): it stands
    /// for the literal its DEFINE wrote, and that literal meets SR2 like a written one — so a name defined
    /// <c>AS X"…"</c> is still a hexadecimal-format literal (kb/Work PB1368).</summary>
    private static bool Literal(Tok tok, DiagnosticBag diag, SourceLocation loc, string role,
        Func<string, CompilationVariableEvent?>? definitionOf, out string content, out bool isWritten)
    {
        content = "";
        isWritten = true;
        if (!tok.IsLiteral && definitionOf is null)
        {
            // The syntax-only parse (an omitted conditional-compilation branch, §7.2.1): a compilation-variable-name in
            // the slot is a name by its shape, and what it denotes is not read — nothing in that branch is compiled.
            if (!CobolCharacterRepertoire.IsWordShape(tok.Text))
            {
                Invalid(diag, loc, $">>COBOL-WORDS: {role} must be an alphanumeric literal (ISO §7.3.10.3 SR2), or a "
                    + $"compilation-variable-name (ISO §7.3.11.4 GR1), not '{tok.Text}'");
                return false;
            }
            content = CobolNames.UpperFold(tok.Text);
            isWritten = false;
            return true;
        }
        if (!tok.IsLiteral)
        {
            if (definitionOf!(tok.Text) is not { Value: { } value } definition)
            {
                Invalid(diag, loc, $">>COBOL-WORDS: {role} must be an alphanumeric literal (ISO §7.3.10.3 SR2), or a "
                    + $"compilation-variable-name defined before the directive (ISO §7.3.11.4 GR1), not '{tok.Text}'");
                return false;
            }
            if (value.Category != CtCategory.Alphanumeric)
            {
                Invalid(diag, loc, $">>COBOL-WORDS: {role} names the compilation variable '{tok.Text}', whose value is "
                    + $"{value.Category.ToString().ToLowerInvariant()} — a compilation-variable-name stands only where a "
                    + "literal of its own category is permitted (ISO §7.3.11.4 GR1), and this literal is alphanumeric "
                    + "(ISO §7.3.10.3 SR2)");
                return false;
            }
            // The literal the name represents: the DEFINE operand as written (one literal — §7.3.3 SR10 keeps a
            // concatenation expression out of every directive — so its format is screened below like a written one),
            // or, for a value the PARAMETER phrase obtained from the environment (§7.3.11.4 GR4), which has no written
            // operand, that value as a plain literal.
            tok = Tokenize(definition.Written) is [{ IsLiteral: true, Defect: null } written]
                ? written : new Tok(value.Text, IsLiteral: true, Prefix: "");
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
        content = CobolNames.UpperFold(tok.Text);
        return true;
    }

    /// <summary>SR4 (frontend half): the fresh word (literal-2/5/6) is a well-formed user-defined word per
    /// §8.3.2.1 / §8.3.2.2 — the word shape (word characters, the extended letters included, neither hyphen nor
    /// underscore first or last) and at least one basic or extended letter (kb/Work PB1402 — this admitted only
    /// letters, digits and hyphens, so the 2002 underscore was refused, and counted a digit as the letter §8.3.2.2
    /// asks for). (Whether it is nonetheless reserved/context/intrinsic — the SR4 category bar — is checked in the
    /// compiler.)</summary>
    private static bool UserWord(string word, DiagnosticBag diag, SourceLocation loc)
    {
        bool ok = CobolCharacterRepertoire.IsWordShape(word) && word.Any(CobolCharacterRepertoire.IsLetter);
        if (!ok)
        {
            Invalid(diag, loc,
                $">>COBOL-WORDS: '{word}' is not a valid user-defined word (ISO §7.3.10.3 SR4 / §8.3.2.2)");
            return false;
        }
        // The rest of §8.3.2.1 — the 63-character ceiling and the Annex B placement of each extended character — is the
        // word's own rule (CobolWordRule, the one place it lives for a directive-carried word). A RESERVE'd or EQUATE'd
        // word no statement uses never reaches the tree funnel that asks it of every other user word, so asking it here
        // is the only place a 64-character fresh word is seen. The directive is a COBOL-2023 introduction, so the
        // edition whose limit applies is the one that introduced it.
        foreach (var (_, violation) in CobolWordRule.DirectiveWordViolations(word, CobolWordsMap.DirectiveEdition))
        {
            Invalid(diag, loc, $">>COBOL-WORDS: '{word}' is not a valid user-defined word — {violation} (ISO §7.3.10.3 SR4)");
            ok = false;
        }
        return ok;
    }

    /// <summary>The SYNTAX-ONLY check of one <c>&gt;&gt;COBOL-WORDS</c> operand — what ISO §7.2.1 asks of a directive
    /// whose line is in an OMITTED conditional-compilation branch ("syntactically correct in the initial source text and
    /// library text"; kb/Work PB2003): the option, its arity and join word, and each literal's form (SR2) and, for the
    /// fresh word, its user-word shape (SR4). Nothing is applied and nothing is looked up — SR3/SR4's word categories,
    /// SR5's uniqueness and the DEFINE timeline belong to the directive that is compiled. Reports COBOLNET1623.</summary>
    internal static void CheckOperand(string operand, DiagnosticBag diagnostics, SourceLocation loc) =>
        TryParseOption(operand, 0, diagnostics, loc, definitionOf: null, out _);

    private static void Invalid(DiagnosticBag diag, SourceLocation loc, string message) =>
        diag.ReportError(DiagnosticCatalog.CobolWordsDirectiveInvalid.Code, message, loc, default);

    // ── operand tokenizer ────────────────────────────────────────────────────────────────────────────────────
    /// <param name="Defect">Null for a well-formed token; otherwise why a LITERAL is not an alphanumeric literal at all
    /// (§8.3.3.2.3, §8.3.5 5) — SR2 requires each operand literal to be one.</param>
    private readonly record struct Tok(string Text, bool IsLiteral, string Prefix, string? Defect = null)
    {
        public bool IsKeyword(string kw) => !IsLiteral && CobolNames.Same(Text, kw);
    }

    /// <summary>Split the operand into barewords and quoted literals. The separators are the operand's own: the
    /// separator space, which the directive-line parse has already made of every separator comma and semicolon
    /// (<see cref="CompilerDirectiveLine.SeparatorsAsSpaces"/>, §8.3.5 2)). A letter run immediately followed by a quote
    /// (no space) is captured as a PREFIXED literal (e.g. <c>X"AB"</c>) so SR2 can reject it precisely.</summary>
    private static List<Tok> Tokenize(string s)
    {
        var toks = new List<Tok>();
        int i = 0;
        while (i < s.Length)
        {
            if (CobolSpace.IsSeparator(s[i])) { i++; continue; }
            if (s[i] is '"' or '\'')
            {
                i = ReadQuoted(s, i, prefix: "", toks);
                continue;
            }
            // a bareword; if it butts directly against a quote, it is a literal prefix (X"…", N"…", …)
            int start = i;
            while (i < s.Length && !CobolSpace.IsSeparator(s[i]) && s[i] is not ('"' or '\'')) i++;
            string word = s[start..i];
            if (i < s.Length && s[i] is '"' or '\'')
                i = ReadQuoted(s, i, prefix: word, toks);
            else
                toks.Add(new Tok(word, IsLiteral: false, Prefix: ""));
        }
        return toks;
    }

    /// <summary>Read a quoted literal starting at <paramref name="q"/> (its opening quote); returns the index past
    /// the closing quote. Emits a Literal token carrying its content: two contiguous quotation symbols matching the
    /// opening one are one occurrence of that character in the content (§8.3.3.2.3 3)), a literal with no closing
    /// delimiter is DEFECTIVE (SR2: "Each literal shall be an alphanumeric literal"), and so is one whose closing
    /// delimiter is not "immediately followed by one of the separators space, comma, semicolon, period, right
    /// parenthesis, or closing pseudo-text delimiter" (§8.3.5 5)) — the comma and semicolon are already spaces here,
    /// and a period or right parenthesis can only end an operand this directive's format does not admit, which is
    /// the same refusal.</summary>
    private static int ReadQuoted(string s, int q, string prefix, List<Tok> toks)
    {
        char quote = s[q];
        var content = new System.Text.StringBuilder();
        int j = q + 1;
        for (; j < s.Length; j++)
        {
            if (s[j] != quote) { content.Append(s[j]); continue; }
            if (j + 1 < s.Length && s[j + 1] == quote) { content.Append(quote); j++; continue; }
            break;
        }
        if (j >= s.Length)
        {
            toks.Add(new Tok(content.ToString(), IsLiteral: true, Prefix: prefix,
                Defect: "it has no closing quotation symbol (ISO §8.3.5 5))"));
            return j;
        }
        j++;   // step past the closing quote
        bool separated = j >= s.Length || CobolSpace.IsSeparator(s[j]);
        toks.Add(new Tok(content.ToString(), IsLiteral: true, Prefix: prefix,
            Defect: separated ? null
                : $"its closing quotation symbol is followed by '{s[j]}', and a separator space (or a comma or semicolon "
                  + "followed by one, and more of the operand) shall come next (ISO §8.3.5 2), 5))"));
        return j;
    }
}
