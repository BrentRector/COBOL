// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// COBOL-aware error strategy that replaces ANTLR's generic messages with
/// actionable, human-readable diagnostics. Pattern-matches common COBOL
/// mistakes and suggests fixes.
/// </summary>
public sealed class CobolErrorStrategy : DefaultErrorStrategy
{
    /// <summary>
    /// Structured diagnostic hint with a COBOL-level code and priority.
    /// Lower priority values sort first (higher importance).
    /// </summary>
    private readonly record struct DiagnosticHint(Diagnostics.DiagnosticDescriptor Descriptor, string Message, int Priority)
    {
        public string Code => Descriptor.Code;
    }

    protected override void ReportNoViableAlternative(Parser recognizer, NoViableAltException e)
    {
        var token = e.OffendingToken ?? recognizer.CurrentToken;
        var message = BuildMessage(recognizer, token, e, ErrorKind.NoViableAlternative);
        recognizer.NotifyErrorListeners(token, message, e);
    }

    protected override void ReportInputMismatch(Parser recognizer, InputMismatchException e)
    {
        var token = e.OffendingToken ?? recognizer.CurrentToken;
        string expected = e.GetExpectedTokens().ToString(recognizer.Vocabulary);
        var message = BuildMessage(recognizer, token, e, ErrorKind.InputMismatch, expected);
        recognizer.NotifyErrorListeners(token, message, e);
    }

    protected override void ReportMissingToken(Parser recognizer)
    {
        if (errorRecoveryMode) return;
        BeginErrorCondition(recognizer);

        var token = recognizer.CurrentToken;
        string expected = GetExpectedTokens(recognizer).ToString(recognizer.Vocabulary);
        var message = BuildMessage(recognizer, token, null, ErrorKind.MissingToken, expected);
        recognizer.NotifyErrorListeners(token, message, null);
    }

    protected override void ReportUnwantedToken(Parser recognizer)
    {
        if (errorRecoveryMode) return;
        BeginErrorCondition(recognizer);

        var token = recognizer.CurrentToken;
        string expected = GetExpectedTokens(recognizer).ToString(recognizer.Vocabulary);
        var message = BuildMessage(recognizer, token, null, ErrorKind.UnwantedToken, expected);
        recognizer.NotifyErrorListeners(token, message, null);
    }

    /// <summary>⛔ A PARENTHESIZED GROUP IS NEVER A RESYNCHRONIZATION POINT (kb/Work PB2152). ISO §8.3.5 4): "Except in
    /// pseudo-text, parentheses may appear only in balanced pairs of left and right parentheses" — so a '(' the parser
    /// skips while recovering opens a group whose content belongs to the construct that failed, never to the next one.
    /// ANTLR's resync consumes token by token until one is in the recovery set, and a token INSIDE the group can be in
    /// it: since the reference paren became its own token (kb/Work PB2113) the integer in <c>05 A(1) PIC X(3).</c> or
    /// <c>01 X PICTURE X(3).</c> resyncs as a level-number, and its ')' then reports a second, spurious
    /// "unexpected ')'" for the one error the source holds. So a group met while consuming is consumed WHOLE, through
    /// its balancing ')' of any flavour (the three paren twins), and stops early at the separator period or end of
    /// file, so an unbalanced '(' cannot swallow the rest of the program. A paren that IS in the recovery set is still
    /// the resync point: the set is asked first.</summary>
    protected override void ConsumeUntil(Parser recognizer, IntervalSet set)
    {
        var input = (ITokenStream)recognizer.InputStream;
        for (int ttype = input.LA(1); ttype != TokenConstants.EOF && !set.Contains(ttype); ttype = input.LA(1))
        {
            if (!IsOpenParen(ttype))
            {
                recognizer.Consume();
                continue;
            }
            int depth = 0;
            do
            {
                if (IsOpenParen(ttype)) depth++;
                else if (IsCloseParen(ttype)) depth--;
                recognizer.Consume();
                ttype = input.LA(1);
            }
            while (depth > 0 && ttype != TokenConstants.EOF && ttype != CobolLexer.DOT);
        }
    }

    /// <summary>The other half of the same rule: single-token deletion never deletes a '(' (kb/Work PB2152). Deleting
    /// one paren of a balanced pair (§8.3.5 4) orphans the other, so <c>DISPLAY (W-S).</c> recovered by dropping the
    /// '(' and then reported its ')' as a second error. Declining here sends the error to <see cref="ConsumeUntil"/>,
    /// which consumes the group whole.</summary>
    protected override IToken? SingleTokenDeletion(Parser recognizer) =>
        IsOpenParen(((ITokenStream)recognizer.InputStream).LA(1)) ? null : base.SingleTokenDeletion(recognizer);

    private static bool IsOpenParen(int ttype) =>
        ttype is CobolLexer.LPAREN or CobolLexer.FNARG_LPAREN or CobolLexer.REF_LPAREN;

    private static bool IsCloseParen(int ttype) =>
        ttype is CobolLexer.RPAREN or CobolLexer.FNARG_RPAREN or CobolLexer.REF_RPAREN;

    // ── Message construction ──

    private enum ErrorKind { NoViableAlternative, InputMismatch, MissingToken, UnwantedToken }

    private static string BuildMessage(
        Parser recognizer, IToken token, RecognitionException? e,
        ErrorKind kind, string? expectedTokens = null)
    {
        string baseMsg = kind switch
        {
            ErrorKind.NoViableAlternative => $"cannot parse construct near '{Truncate(token.Text, 40)}'",
            ErrorKind.InputMismatch => $"unexpected '{Truncate(token.Text, 40)}'",
            ErrorKind.MissingToken => $"missing token before '{Truncate(token.Text, 40)}'",
            ErrorKind.UnwantedToken => $"unexpected '{Truncate(token.Text, 40)}'",
            _ => "syntax error"
        };

        var hints = GuessCobolIntent(recognizer, token, expectedTokens);

        if (hints.Count == 0)
            return baseMsg;

        // Deduplicate by code prefix (first 8 chars), sort by priority, cap at 2
        var seen = new HashSet<string>();
        var filtered = new List<DiagnosticHint>();
        foreach (var h in hints.OrderBy(h => h.Priority))
        {
            string prefix = h.Code.Length >= 8 ? h.Code[..8] : h.Code;
            if (seen.Add(prefix) && filtered.Count < 2)
                filtered.Add(h);
        }

        string code = filtered[0].Code;
        string hintText = string.Join(" ", filtered.Select(h => h.Message));
        return $"[{code}] {baseMsg}. {hintText}";
    }

    // ── COBOL-specific heuristics ──

    private static List<DiagnosticHint> GuessCobolIntent(
        Parser recognizer, IToken token, string? expectedTokens)
    {
        var hints = new List<DiagnosticHint>();
        var stream = (ITokenStream)recognizer.InputStream;
        var prev = GetToken(stream, token.TokenIndex - 1);
        var ruleStack = recognizer.GetRuleInvocationStack().ToArray();
        string tokenUpper = token.Text?.ToUpperInvariant() ?? "";

        // 0. Vendor JSON/XML statements are NOT ISO/IEC 1989 constructs (0 spec occurrences; owner decision 2,
        // DEVLOG 581): the hard-reserved JSON/XML lexer tokens can only surface here as a misplaced vendor statement
        // behind a generic parse error → the vendor-extension hint (COBOL0313, priority 0 so its code wins the prefix),
        // never an edition gate. Every ISO reservation-word introduction gate now fires at BIND (superset parse +
        // bind-time ConstructRegistry.Check → the VersionConformancePass), so the former ReservedWordEditionHints
        // reverse-signature recogniser is gone (residue migration complete, DESIGN-version-conformance-pipeline.md).
        if (token.Type is CobolLexer.JSON or CobolLexer.XML)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0313,
                $"{(token.Type == CobolLexer.JSON ? "JSON" : "XML")} GENERATE/PARSE is not an ISO/IEC 1989 construct — "
                + "vendor-dialect extension, deferred (owner decision 2, DEVLOG 581).", 0));

        // ⛔ 0a. THE REQUIRED IMPERATIVE-STATEMENT ARM — ONE ARM FOR EVERY GENERAL FORMAT (kb/Work PB396).
        // Nine phrase rules carry an imperative-statement operand that the printed general format leaves
        // UNBRACKETED or stacks inside BRACES (IF's THEN and ELSE arms, EVALUATE's WHEN clause and its WHEN
        // OTHER tail, PERFORM's inline body and its WHEN / WHEN OTHER / WHEN COMMON / FINALLY bodies, SEARCH's
        // and SEARCH ALL's WHEN bodies), and every one of them is now spelled `statementBlock` — a rule that
        // cannot match empty. So the violation always arrives in the SAME shape: a parse position whose
        // expected set admits everything that can START a statement block, reached with a token that can start
        // none of them. That test is computed FROM THE ATN, not from a table of verbs, so a general format
        // added tomorrow inherits the diagnostic without editing this method — which is the whole reason the
        // cardinality lives in the grammar rather than in nine bind-time checks.
        if (IsEmptyRequiredStatementBlock(recognizer, token)
            && EnclosingStatementName(ruleStack) is { } stmtName)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2072,
                $"The {stmtName} statement's general format shows an imperative-statement here that is not "
                + "bracketed, so at least one statement shall be specified: brackets are what license an "
                + "omission (ISO §5.2.6.2), and one alternative of a brace group shall be explicitly specified "
                + "(ISO §5.2.6.3).", 0));

        // ⛔ 0b. THE WHEN OTHER PHRASE — BOTH OF ITS RULES, SEPARATED BY LOOKAHEAD (kb/Work PB396).
        // `[ WHEN OTHER imperative-statement-2 ]` follows the `{ … } …` repetition in EVALUATE's format, and
        // `[ WHEN OTHER EXCEPTION imperative-statement-3 ]` follows the WHEN group in PERFORM Format 3: ONE
        // bracketed phrase, LAST, whose imperative-statement is inside the bracket and therefore required once
        // the phrase is written. A syntax error at the phrase can only be one of those two rules, and which one
        // is decided by what follows OTHER — a token that can START a statement means the body is there and the
        // PHRASE is misplaced or repeated; anything else means the phrase is where it belongs and its
        // imperative-statement is missing. The parser reports at WHEN (the clause-repetition decision) or at
        // OTHER (a WHEN group cannot spell it), so both spellings are recognised here.
        int otherIndex = token.Type == CobolLexer.OTHER ? token.TokenIndex
            : token.Type == CobolLexer.WHEN && GetToken(stream, token.TokenIndex + 1)?.Type == CobolLexer.OTHER
                ? token.TokenIndex + 1
                : -1;
        if (otherIndex >= 0)
        {
            // PERFORM Format 3 prints `WHEN OTHER EXCEPTION imperative-statement-3` with EXCEPTION NOT
            // underlined — an optional word (§8.3.2.4.3), so it is never the body.
            var afterOther = GetToken(stream, otherIndex + 1);
            if (afterOther?.Type == CobolLexer.EXCEPTION) afterOther = GetToken(stream, otherIndex + 2);
            bool bodyPresent = afterOther is not null
                && StatementBlockStart(recognizer).Contains(afterOther.Type);
            hints.Add(bodyPresent
                ? new(Diagnostics.DiagnosticDescriptors.COBOLNET2073,
                    "A WHEN OTHER phrase may be specified at most once, and only after every WHEN phrase: the "
                    + "general format brackets it as a single phrase FOLLOWING the repeated WHEN group, and an "
                    + "ellipsis applies only to the portion between the delimiters immediately to its left "
                    + "(ISO §14.9.13.2 / §14.9.28.2 Format 3, with ISO §5.2.7).", 0)
                : new(Diagnostics.DiagnosticDescriptors.COBOLNET2072,
                    "The WHEN OTHER phrase carries an imperative-statement INSIDE its bracket, so writing the "
                    + "phrase obliges the statement: brackets license omitting the whole phrase, never emptying "
                    + "it (ISO §5.2.6.2, with §14.9.13.2 / §14.9.28.2 Format 3).", 0));
        }

        // ⛔ 0c. A WRITTEN SHAPE NO GENERAL FORMAT OF ITS CLAUSE PRINTS (kb/Work PB412, PB421). The grammar
        // rules for GO TO and MOVE each now carry ONE ALTERNATIVE PER PRINTED FORMAT, so their complement — the
        // shapes the old optional-everything union rules admitted — arrives here as a syntax error instead of
        // reaching a bind arm that discarded operands or emitted a loud stage. Both hints are read from the
        // PARSER STATE, not from a catalogue of bad source: the GO TO one asks the ONE format classifier what
        // the partially-built statement context actually holds, and the MOVE one is the position of a
        // statement-selecting keyword inside the rule that cannot spell it.
        if (GoToComplementMessage(recognizer, token, stream) is { } goToMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2172, goToMsg, 0));

        // ⛔ 0d. A FIGURATIVE SPELLING WHERE A GENERAL FORMAT NAMES ONE KEYWORD (kb/Work PB510). ZERO / ZEROS /
        // ZEROES (and each singular/plural figurative pair) are distinct §8.9 reserved words, one lexer token
        // each, interchangeable only as the §8.3.3.6 figurative constant. A format that prints ONE of them as a
        // keyword (§5.2.2) — BLANK WHEN ZERO, the sign condition's ZERO, OPTIONS INITIALIZE's BINARY ZEROES /
        // HIGH-VALUES / LOW-VALUES / SPACES — therefore fails the parse on a sibling spelling, and it arrives here
        // in ONE shape: the expected set holds a sibling of the offending token's family but not the token. The
        // families are read FROM THE ATN (the FIRST sets of the figurative-word rules), so a format added tomorrow
        // that names one spelling inherits this diagnostic without editing this method.
        if (FigurativeSpellingMessage(recognizer, token) is { } spellingMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2418, spellingMsg, -1));   // outranks 0a: the sign-
            // condition shape fails at an IS / NOT the enclosing statement cannot continue with, which 0a reads as an
            // empty imperative-statement — the misspelled keyword is the cause, the empty block its symptom.
        // ⛔ 0e. THE SEARCH PHRASES NO SEARCH FORMAT PRINTS (kb/Work PB446). The two SEARCH rules now spell the
        // two printed formats exactly, so their complement arrives here; each shape is read from the PARSER
        // STATE (the half-built or just-completed SEARCH context), never from a scan for a keyword.
        if (SearchFormatShapeMessage(recognizer, token, stream) is { } searchMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2269, searchMsg, 0));
        // ⛔ 0e'. A SUBSCRIPT, REFERENCE MODIFICATION OR QUALIFIER ON A MNEMONIC-NAME (kb/Work PB2499). ACCEPT's
        // FROM, DISPLAY's UPON and SET Format 3's receivers are spelled `cobolWord` because a mnemonic-name is a word
        // (§8.3.2.2.16), so the suffix arrives here — read from the PARSER STATE, worded by MnemonicNameSlot.
        if (MnemonicSuffixMessage(recognizer, token, stream) is { } mnemonicMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2269, mnemonicMsg, 0));
        // ⛔ 0f. AN INLINE METHOD INVOCATION'S EMPTY PARENTHESIS PAIR (kb/Work PB1430). `inlineInvocationSegment`
        // requires its argumentList inside the optional parentheses, so `O :: "M" ( )` arrives here, at the '(' or
        // the ')'. Outranks 0a for the same reason 0d does: the empty pair is the cause wherever the parse gives up.
        if (IsEmptyInlineArgumentList(stream, token))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2994,
                "An inline method invocation's parenthesis pair holds at least one argument: ISO §8.4.3.4.2 brackets "
                + "the whole parenthesised group but encloses the argument forms in BRACES repeated by the ellipsis "
                + "inside the one pair, and one alternative of a brace group shall be explicitly specified "
                + "(ISO §5.2.6.3). Omit the parentheses when the method is invoked without arguments.", -1));

        if (token.Type is CobolLexer.CORRESPONDING or CobolLexer.CORR && IsInRule(ruleStack, "moveStatement"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2173,
                "Both general formats of the MOVE statement put the whole sending specification directly after "
                + "the verb — Format 1 is 'MOVE { identifier-1 | literal-1 } TO { identifier-2 } …' and Format 2 "
                + "is 'MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4' — so no format admits a "
                + "CORRESPONDING phrase after a sending operand (ISO §14.9.25.2).", 0));

        // ⛔ NO "MISSING SPACE BEFORE / AFTER STRING LITERAL" GUESS HERE (kb/Work PB1394). A literal delimiter that is not
        // separated is §8.3.5 rule 5's violation, and SeparatorRule reports it — for every literal token, whether or
        // not the parse happens to fail there — as COBOLNET2633. The two recovery hints that stood here (COBOL0301 /
        // COBOL0302) stated that rule a second time, only on a parse error, and only for a quotation-mark delimiter.

        // 3. Missing TO in MOVE statement
        if (IsInRule(ruleStack, "moveStatement") && IsIdentifier(token) && prev != null && IsLiteral(prev))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0303, "In a MOVE statement, did you forget TO before the target?", 10));

        // 4. Missing period after paragraph name (shows as qualified name)
        if (token.Text is { } text && (CobolNames.Contains(text, ".MOVE") || CobolNames.Contains(text, ".ADD")
            || CobolNames.Contains(text, ".SUBTRACT") || CobolNames.Contains(text, ".PERFORM") || CobolNames.Contains(text, ".IF")))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0304, "Missing period after paragraph name — the parser is treating it as a qualified reference.", 5));

        // 5. STATUS where IDENTIFIER expected
        if (tokenUpper == "STATUS" && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0200, "STATUS is a reserved word here. For file status, use 'FILE STATUS IS <data-name>'.", 10));

        // 6. PROGRAM where IDENTIFIER expected
        if (tokenUpper == "PROGRAM" && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0201, "PROGRAM is a reserved word. If this is a paragraph name, it cannot be named PROGRAM.", 10));

        // 7. Unrecognized keyword in SPECIAL-NAMES
        if (IsInRule(ruleStack, "specialNamesParagraph") && !IsIdentifier(token) && !IsLiteral(token))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0305, "Unexpected token in SPECIAL-NAMES. Check implementor-name or mnemonic-name syntax.", 15));

        // 8. ASCENDING/DESCENDING KEY where the entry can only end (kb/Work PB1266). KEY is supported; what this
        // catches is its POSITION: §13.18.38.2 writes the OCCURS clause's KEY phrases BEFORE INDEXED BY, so a KEY phrase
        // after it finds the entry ready to end. The hint used to claim the clause was "not yet supported" and that a
        // table had been created without a key — false on both counts.
        if ((tokenUpper == "ASCENDING" || tokenUpper == "DESCENDING") &&
            expectedTokens?.Contains("'.'") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0100, "ASCENDING/DESCENDING KEY is out of place: in an OCCURS clause the KEY phrases are written before INDEXED BY (ISO §13.18.38.2).", 5));

        // 9. BLANK WHEN ZERO as separate tokens
        if (tokenUpper == "BLANK" && expectedTokens?.Contains("'.'") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0101, "BLANK WHEN ZERO may not be recognized. Check that it appears as a single clause on the data item.", 15));

        // 10. SET statement forms
        if (tokenUpper == "SET" && IsInRule(ruleStack, "procedureDivision"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0102, "This SET form may not be supported. Supported forms: SET identifier TO value, SET condition TO TRUE/FALSE, SET index UP/DOWN BY integer.", 15));

        // 11. SEARCH statement
        if (tokenUpper == "SEARCH" && IsInRule(ruleStack, "procedureDivision"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0103, "SEARCH statement may not be fully supported.", 15));

        // 12. THROUGH/THRU in unexpected context
        if ((tokenUpper == "THROUGH" || tokenUpper == "THRU") && expectedTokens?.Contains("'.'") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0300, "THROUGH/THRU is not recognized in this context. Check PERFORM or VALUE THROUGH syntax.", 10));

        // 13. Misplaced END-xxx terminators
        if (tokenUpper.StartsWith("END-"))
        {
            string stmt = tokenUpper[4..]; // "IF", "PERFORM", etc.
            if (!IsInMatchingRule(ruleStack, stmt))
                hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0306, $"{tokenUpper} appears without a matching {stmt} statement.", 5));
        }

        // 14. Missing period at end of sentence — a GUESS, and the lowest-priority one here, so it is suppressed
        // once a STRUCTURAL diagnosis has already named what the general format requires at this position
        // (priority 0). Otherwise `GO TO DEPENDING ON X.` read "…at least one procedure-name is required. A
        // period may be missing at the end of the previous sentence." — the second sentence contradicting the
        // first (kb/Work PB412).
        if (expectedTokens?.Contains("'.'") == true && !tokenUpper.StartsWith("END-")
            && hints.TrueForAll(h => h.Priority > 0))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0307, "A period may be missing at the end of the previous sentence.", 25));

        // 15. Literal where identifier expected
        if (IsLiteral(token) && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0308, "A data-name is expected here, not a literal.", 20));

        // 16. Identifier where literal expected
        if (IsIdentifier(token) &&
            (expectedTokens?.Contains("STRINGLIT") == true || expectedTokens?.Contains("INTEGERLIT") == true))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0309, "A literal value is expected here, not a data-name.", 20));

        // 17. Missing BY in INDEXED BY
        if (tokenUpper != "BY" && expectedTokens?.Contains("'BY'") == true &&
            IsInRule(ruleStack, "dataDescriptionEntry"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0310, "Missing BY keyword. INDEXED BY requires 'INDEXED BY <index-name>'.", 10));

        // 18. Complex EVALUATE forms
        if (tokenUpper == "ALSO" && IsInRule(ruleStack, "evaluateStatement"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0107, "EVALUATE with ALSO (multi-subject) may not be fully supported.", 10));

        // 19. FILE CONTROL / SELECT context errors
        if (IsInRule(ruleStack, "fileControlParagraph") &&
            !IsIdentifier(token) && !IsLiteral(token) &&
            tokenUpper != "SELECT" && tokenUpper != "ASSIGN" && tokenUpper != "FILE_CONTROL")
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0312,
                "Unexpected token in FILE-CONTROL paragraph. Check SELECT/ASSIGN TO syntax.",
                10));

        return hints;
    }

    // ── The GO TO general-format complement (kb/Work PB412) ──

    /// <summary>The §14.9.17.2 message for a GO TO written in a shape NEITHER general format prints, or null.
    /// <para>Two parser states carry the two complement shapes, and each is read structurally:</para>
    /// <list type="bullet">
    ///   <item>The statement context is still on the rule-invocation stack — the parser committed to the
    ///         Format-2 alternative on seeing a LIST of procedure-names and then found no DEPENDING. The
    ///         half-built <c>GoToStatementContext</c> already holds the children matched so far, so the counts
    ///         handed to <see cref="GoToFormats.DiagnoseWrittenShape"/> are the ones actually written — no
    ///         re-scan of the token stream, and no second copy of the format rule.</item>
    ///   <item>The offending token IS <c>DEPENDING</c> immediately after <c>GO</c> or <c>GO TO</c> — the
    ///         target-less alternative matched and the statement ended, so the context has already been popped
    ///         and the shape has to be read off the stream. The <c>GO</c> anchor is what keeps this off an
    ///         OCCURS … DEPENDING ON, whose DEPENDING never follows a GO.</item>
    /// </list></summary>
    private static string? GoToComplementMessage(Parser recognizer, IToken token, ITokenStream stream)
    {
        for (RuleContext? c = recognizer.Context; c is not null; c = c.Parent)
            if (c is CobolParserCore.GoToStatementContext g)
                return GoToFormats.DiagnoseWrittenShape(g.procedureName().Length, g.DEPENDING() is not null);

        if (token.Type != CobolLexer.DEPENDING) return null;
        var prev = GetToken(stream, token.TokenIndex - 1);
        if (prev?.Type == CobolLexer.TO) prev = GetToken(stream, token.TokenIndex - 2);
        return prev?.Type == CobolLexer.GO ? GoToFormats.DiagnoseWrittenShape(0, hasDepending: true) : null;
    }

    /// <summary>Whether the failure is at the empty parenthesis pair of an inline method invocation segment —
    /// <c>:: literal-1 ( )</c> (§8.4.3.4.2), with <paramref name="token"/> its '(' or its ')'. The §8.7.4 operator has
    /// no other role (§8.3.5 rule 7 makes '::' one lexical unit), so the four default-channel tokens are this format and
    /// nothing else. GROUPING-PAREN-ONLY: a '(' after literal-1 is always the plain token — the lexer retypes a '(' to
    /// FNARG_LPAREN only after <c>FUNCTION</c> function-name (§8.4.3.2.3 SR6) and to REF_LPAREN only after a word that
    /// could name a data item (<c>OnDefaultLParen</c>), and a literal is neither (the same reasoning as <c>InvocationSite.OfInlineSegment</c>).</summary>
    private static bool IsEmptyInlineArgumentList(ITokenStream stream, IToken token)
    {
        var open = token.Type == CobolLexer.LPAREN ? token
            : token.Type == CobolLexer.RPAREN ? PreviousDefault(stream, token.TokenIndex) : null;
        if (open?.Type != CobolLexer.LPAREN || NextDefault(stream, open.TokenIndex)?.Type != CobolLexer.RPAREN)
            return false;
        var name = PreviousDefault(stream, open.TokenIndex);
        return name is not null && LiteralTokens.Types.Contains(name.Type)
            && PreviousDefault(stream, name.TokenIndex)?.Type == CobolLexer.COLONCOLON;
    }

    // ── The SEARCH general-format complement (kb/Work PB446) ──

    /// <summary>The §14.9.37.2 message for a SEARCH written with a phrase neither general format prints, or null.
    /// Format 1 is <c>SEARCH identifier-1 [VARYING …] [AT END imperative-statement-1] {WHEN condition-1 {…}} …
    /// [END-SEARCH]</c>; Format 2 is <c>SEARCH ALL identifier-1 [AT END imperative-statement-1] WHEN … [AND …] …
    /// {…} [END-SEARCH]</c> (rendered from PDF page 750 / printed folio 720). Three complement shapes, each read
    /// from the parser state:
    /// <list type="bullet">
    ///   <item>A KEY phrase — the failure is AT the SEARCH context itself (the table operand is complete and the
    ///         statement's own next element was expected), and the offending token is KEY.</item>
    ///   <item>A NOT AT END phrase — a SEARCH context with no WHEN phrase yet is on the stack (the failure is in
    ///         the AT END region, possibly inside its last statement) and the tokens at the failure spell
    ///         <c>NOT [AT] END</c>.</item>
    ///   <item>A second Format-2 WHEN — the Format-2 statement has already COMPLETED (its one WHEN phrase
    ///         matched), so the SEARCH ALL context is the rightmost descendant of an enclosing context and ends
    ///         immediately before the offending WHEN.</item>
    /// </list></summary>
    private static string? SearchFormatShapeMessage(Parser recognizer, IToken token, ITokenStream stream)
    {
        const string formats = " ISO §14.9.37.2 prints two SEARCH formats and ";
        var ctx = recognizer.Context;
        if (token.Type == CobolLexer.KEY
            && ctx is CobolParserCore.SearchStatementContext or CobolParserCore.SearchAllStatementContext)
            return (ctx is CobolParserCore.SearchAllStatementContext ? "SEARCH ALL" : "SEARCH")
                + " … KEY:" + formats + "neither has a KEY phrase — the key of a SEARCH ALL is declared by the "
                + "ASCENDING/DESCENDING KEY phrase of identifier-1's OCCURS clause (§14.9.37.3 SR7), and the "
                + "Format-2 WHEN phrase names it. Remove the phrase.";

        bool notAtEnd = token.Type == CobolLexer.NOT ? IsAtEndAhead(stream, token.TokenIndex + 1)
            : token.Type is CobolLexer.AT or CobolLexer.END
              && PreviousDefault(stream, token.TokenIndex)?.Type == CobolLexer.NOT
              && IsAtEndAhead(stream, token.TokenIndex);
        // "No WHEN phrase yet" is asked of the WHEN TOKEN, not of the clause context: single-token insertion
        // reports a missing WHEN from INSIDE the just-entered (Format-2) WHEN clause, whose context already exists.
        if (notAtEnd)
            for (RuleContext? c = ctx; c is not null; c = c.Parent)
            {
                if (c is CobolParserCore.SearchStatementContext s1)
                    return s1.searchWhenClause().Any(w => w.WHEN() is not null) ? null
                        : "SEARCH … NOT AT END:" + formats + "each has an AT END phrase alone — there is no NOT "
                          + "AT END phrase. Test for a hit in a WHEN branch instead.";
                if (c is CobolParserCore.SearchAllStatementContext s2)
                    return s2.searchAllWhenClause()?.WHEN() is not null ? null
                        : "SEARCH ALL … NOT AT END:" + formats + "each has an AT END phrase alone — there is no "
                          + "NOT AT END phrase. Test for a hit in the WHEN branch instead.";
            }

        if (token.Type == CobolLexer.WHEN)
            for (var c = ctx; c is not null; c = c.Parent as ParserRuleContext)
                for (var d = LastRuleChild(c); d is not null; d = LastRuleChild(d))
                    if (d is CobolParserCore.SearchAllStatementContext sa && sa.Stop is { } stop
                        && NextDefault(stream, stop.TokenIndex)?.TokenIndex == token.TokenIndex)
                        return "SEARCH ALL with a second WHEN phrase: Format 2 prints exactly ONE WHEN phrase — its "
                            + "ellipsis sits on the [ AND … ] bracket, not on the WHEN brace (ISO §14.9.37.2, with "
                            + "§5.2.7), so a further key condition is written as an AND phrase of the one WHEN.";
        return null;
    }

    // ── A suffix on a mnemonic-name (kb/Work PB2499) ──

    /// <summary>The <see cref="MnemonicNameSlot"/> message for a subscript, reference modification or qualifier
    /// written on a mnemonic-name in a slot the grammar spells as a word, or null. Two shapes, each read from the
    /// parser state:
    /// <list type="bullet">
    ///   <item>ACCEPT … FROM and DISPLAY … UPON — the statement COMPLETED at the word, so the slot's context
    ///         (<c>acceptSource</c>'s word arm, <c>displayUpon</c>) is the rightmost descendant of an enclosing
    ///         context and ends immediately before the offending '(' / OF / IN.</item>
    ///   <item>SET Format 3 — no SET format admits the suffixed list, so the failure is inside the SET statement
    ///         itself; it is Format 3's when the receiving list that holds the suffix ends at <c>TO ON</c> or
    ///         <c>TO OFF</c>, which only Format 3 writes.</item>
    /// </list></summary>
    private static string? MnemonicSuffixMessage(Parser recognizer, IToken token, ITokenStream stream)
    {
        if (IsMnemonicSuffixStart(token.Type))
            for (var c = recognizer.Context; c is not null; c = c.Parent as ParserRuleContext)
                for (var d = LastRuleChild(c); d is not null; d = LastRuleChild(d))
                {
                    var (slot, format, word) = d switch
                    {
                        CobolParserCore.AcceptSourceContext a => ("ACCEPT … FROM", "§14.9.1.2 Format 1", a.cobolWord()),
                        CobolParserCore.DisplayUponContext u => ("DISPLAY … UPON", "§14.9.11.2 Format 1", u.cobolWord()),
                        _ => (null, null, null),
                    };
                    if (word?.Stop is { } stop && NextDefault(stream, stop.TokenIndex)?.TokenIndex == token.TokenIndex)
                        return MnemonicNameSlot.SuffixMessage(slot!, word.GetText(), format!);
                }

        for (RuleContext? c = recognizer.Context; c is not null; c = c.Parent)
            if (c is CobolParserCore.SetStatementContext set)
            {
                // SET word … <suffix> … TO {ON | OFF}: the first suffix names its word; the list must end at TO ON/OFF.
                IToken? suffixed = null;
                for (var t = NextDefault(stream, set.Start.TokenIndex); t is not null; t = NextDefault(stream, t.TokenIndex))
                {
                    if (t.Type is CobolLexer.DOT or TokenConstants.EOF) return null;
                    if (t.Type == CobolLexer.TO)
                        return suffixed is not null && NextDefault(stream, t.TokenIndex)?.Type is CobolLexer.ON or CobolLexer.OFF
                            ? MnemonicNameSlot.SuffixMessage("SET … TO ON/OFF", suffixed.Text, "§14.9.39.2 Format 3")
                            : null;
                    if (suffixed is null && IsMnemonicSuffixStart(t.Type))
                        suffixed = PreviousDefault(stream, t.TokenIndex);
                }
                return null;
            }
        return null;
    }

    /// <summary>A token that opens a subscript or reference modification (any of the three '(' twins) or a
    /// qualifier (OF / IN) — the suffixes a <c>dataReference</c> admits and a mnemonic-name does not.</summary>
    private static bool IsMnemonicSuffixStart(int type) =>
        IsOpenParen(type) || type is CobolLexer.OF or CobolLexer.IN;

    /// <summary>True when the default-channel tokens from <paramref name="index"/> spell <c>[AT] END</c>.</summary>
    private static bool IsAtEndAhead(ITokenStream stream, int index)
    {
        var t = GetToken(stream, index);
        while (t is not null && t.Channel != TokenConstants.DefaultChannel) t = GetToken(stream, t.TokenIndex + 1);
        if (t?.Type == CobolLexer.AT) t = NextDefault(stream, t.TokenIndex);
        return t?.Type == CobolLexer.END;
    }

    private static IToken? NextDefault(ITokenStream stream, int index)
    {
        for (var t = GetToken(stream, index + 1); t is not null; t = GetToken(stream, t.TokenIndex + 1))
            if (t.Channel == TokenConstants.DefaultChannel) return t;
        return null;
    }

    private static IToken? PreviousDefault(ITokenStream stream, int index)
    {
        for (var t = GetToken(stream, index - 1); t is not null; t = GetToken(stream, t.TokenIndex - 1))
            if (t.Channel == TokenConstants.DefaultChannel) return t;
        return null;
    }

    /// <summary>The LAST child when it is a rule context; null when it is a token (nothing to the right of a token
    /// can be the completed SEARCH ALL).</summary>
    private static ParserRuleContext? LastRuleChild(ParserRuleContext c) =>
        c.ChildCount > 0 ? c.GetChild(c.ChildCount - 1) as ParserRuleContext : null;

    // ── Parse-layer edition-gate rendering (rearch PHASE 02) ──

    /// <summary>Captures the single <see cref="EditionDiagnostic"/> a one-shot <see cref="ConstructRegistry.Check"/>
    /// reports, so the parse-layer error strategy can reuse the ONE funnel's exact COBOLNET0900 text (display /
    /// introduction edition / ISO citation all sourced from the construct's registry row, not hand-copied).</summary>
    private sealed class CaptureSink : IDiagnosticSink
    {
        public string? Message { get; private set; }
        public void Report(in EditionDiagnostic d) => Message = d.Message;
    }

    // ── The required-imperative test (kb/Work PB396) ──

    /// <summary>FIRST(<c>statementBlock</c>) — every token that can begin the grammar's imperative-statement
    /// operand — read ONCE from the ATN. Cached in a static because the ATN is a static of the generated parser;
    /// a benign race recomputes the same set.</summary>
    private static IntervalSet? _statementBlockStart;

    private static IntervalSet StatementBlockStart(Parser recognizer)
        => _statementBlockStart ??= recognizer.Atn.NextTokens(
               recognizer.Atn.ruleToStartState[CobolParserCore.RULE_statementBlock]);

    private static IntervalSet[]? _figurativeFamilies;

    /// <summary>The §8.3.3.6.2 figurative-word families — ZERO/ZEROS/ZEROES, SPACE/SPACES, HIGH-VALUE/HIGH-VALUES,
    /// LOW-VALUE/LOW-VALUES, QUOTE/QUOTES — each the FIRST set of the grammar rule that writes the family's
    /// interchangeability once (kb/Work PB510). Derived from the ATN, never spelled here.</summary>
    public static IntervalSet[] FigurativeFamilies(Parser recognizer)
        => _figurativeFamilies ??= [.. new[]
            {
                CobolParserCore.RULE_zeroWord, CobolParserCore.RULE_spaceWord, CobolParserCore.RULE_highValueWord,
                CobolParserCore.RULE_lowValueWord, CobolParserCore.RULE_quoteWord,
            }.Select(r => recognizer.Atn.NextTokens(recognizer.Atn.ruleToStartState[r]))];

    /// <summary>The COBOLNET2418 message when the parser failed ON one spelling of a figurative family at a
    /// position whose general format names a DIFFERENT spelling of that family as its keyword; null otherwise.
    /// A position that admits the figurative constant admits every spelling, so the test cannot fire there.</summary>
    private static string? FigurativeSpellingMessage(Parser recognizer, IToken token)
    {
        if (SignConditionSpellingMessage(recognizer, token) is { } signMsg) return signMsg;
        var family = FigurativeFamilies(recognizer).FirstOrDefault(f => f.Contains(token.Type));
        if (family is null) return null;
        var expected = recognizer.GetExpectedTokens();
        if (expected is null || expected.Contains(token.Type)) return null;
        var wanted = family.ToIntegerList().Where(expected.Contains).ToList();
        if (wanted.Count == 0) return null;
        string Spell(int t) => recognizer.Vocabulary.GetLiteralName(t)?.Trim('\'') ?? recognizer.Vocabulary.GetDisplayName(t);
        string required = string.Join(" or ", wanted.Select(Spell));
        return $"The general format requires the keyword {required} here, and {token.Text?.ToUpperInvariant()} is a "
            + "different reserved word (ISO §8.9): the spellings of a figurative constant are interchangeable only "
            + "where the figurative constant itself is written (ISO §8.3.3.6.2), and a keyword underlined in a "
            + "general format is required as printed (ISO §5.2.2).";
    }

    /// <summary>The one keyword position the expected-set test above cannot see: the SIGN CONDITION
    /// (§8.8.4.7.2, <c>operand IS [NOT] { POSITIVE | NEGATIVE | ZERO }</c>). When no comparisonExpression
    /// alternative survives <c>W IS ZEROS</c>, ANTLR's prediction falls back to the alternative that FINISHED the
    /// decision rule — the bare operand — so the failure is reported one token EARLY, at the IS / NOT that the
    /// enclosing statement cannot continue with (<c>W IS ZEROS</c>), or as a no-viable-alternative AT the word
    /// with a decision-state expected set that names no ZERO (<c>W IS NOT ZEROES</c>). Both are recognized here:
    /// a run of IS / NOT that the offending token starts, or ends just before it, meets a ZERO-family spelling
    /// that is not the one the sign condition's format prints (the family read from the ATN).</summary>
    private static string? SignConditionSpellingMessage(Parser recognizer, IToken token)
    {
        // A condition is written only in the procedure division (and its declaratives); an IS / NOT before a
        // figurative spelling elsewhere (VALUE IS ZEROS) is a figurative position, never a sign condition.
        if (!recognizer.GetRuleInvocationStack().Contains("procedureDivision")) return null;
        var stream = (ITokenStream)recognizer.InputStream;
        var zeroFamily = FigurativeFamilies(recognizer)[0];
        IToken? next = token;
        if (token.Type is CobolLexer.IS or CobolLexer.NOT)
        {
            int i = token.TokenIndex;
            do next = NextDefault(stream, ref i);
            while (next is not null && next.Type is CobolLexer.IS or CobolLexer.NOT);
        }
        else
        {
            int j = token.TokenIndex;
            if (PreviousDefault(stream, ref j) is not { Type: CobolLexer.IS or CobolLexer.NOT }) return null;
        }
        if (next is null || next.Type == CobolLexer.ZERO || !zeroFamily.Contains(next.Type)) return null;
        return $"The sign condition's general format requires the keyword ZERO here, and "
            + $"{next.Text?.ToUpperInvariant()} is a different reserved word (ISO §8.9): its format is "
            + "'IS [NOT] { POSITIVE | NEGATIVE | ZERO }' (ISO §8.8.4.7.2), and the spellings of a figurative "
            + "constant are interchangeable only where the figurative constant itself is written (ISO §8.3.3.6.2). "
            + "To compare against the figurative constant, write a relation condition: '= ZEROS'.";
    }

    private static IToken? PreviousDefault(ITokenStream stream, ref int index)
    {
        while (--index >= 0)
        {
            var t = stream.Get(index);
            if (t.Channel == Lexer.DefaultTokenChannel) return t;
        }
        return null;
    }

    private static IToken? NextDefault(ITokenStream stream, ref int index)
    {
        while (++index < stream.Size)
        {
            var t = stream.Get(index);
            if (t.Type == TokenConstants.EOF) return null;
            if (t.Channel == Lexer.DefaultTokenChannel) return t;
        }
        return null;
    }

    /// <summary>True when the parser failed AT a position where a statement block was required and had matched
    /// nothing: the expected set admits every token that can start one, and the offending token starts none.
    /// <para>The "expected ⊇ FIRST(statementBlock)" direction is what makes this specific — at a position where
    /// the block is one of several continuations the parse does not fail at all, and at a position inside a
    /// half-written statement (<c>MOVE TO Y</c>) the expected set is that statement's operand set, not the
    /// statement-start set.</para></summary>
    private static bool IsEmptyRequiredStatementBlock(Parser recognizer, IToken token)
    {
        var start = StatementBlockStart(recognizer);
        if (start.Contains(token.Type)) return false;          // the token CAN start a statement — a later error
        var expected = recognizer.GetExpectedTokens();
        if (expected is null) return false;
        foreach (int t in start.ToIntegerList())
            if (!expected.Contains(t)) return false;
        return true;
    }

    /// <summary>The enclosing statement's COBOL name, taken from the rule invocation stack and spelled from the
    /// RULE NAME rather than a table: <c>ifStatement</c> → IF, <c>searchAllStatement</c> → SEARCH ALL. Null when
    /// the failure is not inside a statement at all (a bad token at paragraph level), which is what keeps the
    /// required-imperative arm from firing outside a general format's operand.</summary>
    private static string? EnclosingStatementName(string[] ruleStack)
    {
        const string suffix = "Statement";
        foreach (string rule in ruleStack)
            if (rule.Length > suffix.Length && rule.EndsWith(suffix, StringComparison.Ordinal))
                return SplitCamelCase(rule[..^suffix.Length]).ToUpperInvariant();
        return null;
    }

    private static string SplitCamelCase(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 4);
        foreach (char c in name)
        {
            if (char.IsUpper(c) && sb.Length > 0) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }

    // ── Helpers ──

    private static IToken? GetToken(ITokenStream stream, int index)
        => index >= 0 && index < stream.Size ? stream.Get(index) : null;

    private static bool IsIdentifier(IToken token)
        => token.Type == CobolLexer.IDENTIFIER;

    private static bool IsLiteral(IToken token)
        => token.Type is CobolLexer.STRINGLIT or CobolLexer.INTEGERLIT or CobolLexer.DECIMALLIT;

    private static bool IsInRule(string[] ruleStack, string ruleName)
        => ruleStack.Any(r => CobolNames.Same(r, ruleName));

    private static bool IsInMatchingRule(string[] ruleStack, string stmtName)
    {
        string ruleName = stmtName.ToLowerInvariant() + "Statement";
        return ruleStack.Any(r => CobolNames.Same(r, ruleName));
    }

    private static string Truncate(string? text, int max)
    {
        if (text == null) return "<EOF>";
        return text.Length <= max ? text : text[..max] + "...";
    }
}
