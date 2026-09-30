// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

lexer grammar CobolLexer;

options {
    caseInsensitive = true;
}

// Virtual token types for the FUNCTION argument-list parentheses (ISO §8.4.3.2.3 SR6). No lexer rule matches
// them: the '(' / ')' rules below RETYPE themselves from the _fnParenStack the lexer already maintains, which is
// the ONE place that knows which parens open an argument list. A distinct type is what lets every DOWNSTREAM
// consumer stop guessing — see the SR6 note on OnDefaultLParen.
// DEFINED is virtual for the same reason in another region: it is a keyword ONLY inside a primed compiler-directive
// expression, so IDENTIFIER's action retypes the word there (see IDENTIFIER). A rule spelling 'DEFINED' would publish
// that literal in the vocabulary, whose literal names are read as "the lexer makes this word a keyword token"
// (CobolKeywordTokens, CobolWordsDriftTests) — which, everywhere else, it does not.
tokens { FNARG_LPAREN, FNARG_RPAREN, DEFINED }

@members {
    // Track the types of the last TWO non-WS tokens emitted: one for subscript-mode detection, two for the
    // FUNCTION-argument suppression (P7 Step 12 — '(' after "FUNCTION name" opens an ARGUMENT list, ISO
    // §8.4.3.2 SR6, lexed in DEFAULT mode so arguments parse as real arithmeticExpressions).
    private int _lastNonWsTokenType = -1;
    private int _prevNonWsTokenType = -1;

    // SUBSCRIPT MODE TRIGGER: whitelist approach.
    //
    // In COBOL, '(' after a data-name means subscript/reference-modification; '(' after anything else means
    // arithmetic grouping (e.g., IF (A + B) > C). The whitelist is IDENTIFIER plus every context-sensitive
    // keyword that can appear as a user-defined data-name. Safe failure mode: a token missing from the set makes
    // the parser see LPAREN/RPAREN (arithmetic) instead of SUBSCRIPT-mode tokens — a clear parse error on the
    // first subscripted use.
    //
    // The set (_dataNameTokens) is GENERATED into Parsing/CobolLexerWordSet.g.cs from
    // tests/version-matrix/cobol-words.json (the subscriptTrigger=true rows) by scripts/gen-cobol-words.ps1
    // (rearchitecture PHASE 04, Group A) — single-sourced with the parser cobolWord rule (Grammar/Core/CobolWords.g4)
    // and cross-checked by CobolWordsDriftTests, retiring the by-hand "mirror cobolWord" discipline this comment
    // used to instruct. Do NOT hand-add a token here: edit cobol-words.json and re-run the generator.

    private bool PreviousTokenCouldBeDataName()
        => (_dataNameTokens.Contains(_lastNonWsTokenType) && !_reservedNonNames.Contains(_lastNonWsTokenType))
           || IsCobolWordsDataName(_lastNonWsTokenType);

    // ⛔ A WORD THIS COMPILE RESERVES IS NEVER A DATA-NAME, SO A '(' AFTER IT IS NEVER A SUBSCRIPT (kb/Work PB1465).
    // _dataNameTokens is edition-blind: it holds every word that is a user-defined word in SOME edition, because
    // the SUBSCRIPT decision is frozen here, before any parser predicate can ask the edition. That made the '(' after
    // a boolean OPERATOR — B-AND / B-OR / B-XOR / B-NOT, user words at COBOL-85 and reserved by ISO §8.9 from 2002 —
    // open SUBSCRIPT mode, so `IF BZ B-OR (BW B-AND BW)` and `COMPUTE BR = B-NOT (BW B-XOR BZ)` were COBOL0001
    // although §8.8.2's Table 4 admits '(' after every boolean operator. §8.3.2.1 rule 1 ("Reserved words shall not
    // be used as user-defined words") settles it for EVERY reservation-gated word at once: the compile's own answer
    // to "is this word a user-defined word here" (ReservedWordSet.AdmitsAsUserWord — the question the parser's
    // userWordHere gate and the §8.9 funnel ask) is handed in by TokenRetypes.PrimeLexer, and a word it refuses
    // stops triggering. An intrinsic-function-name word keeps its trigger (a keyword-omitted call, §8.4.3.2.3 SR2),
    // which is why PrimeLexer excludes the functionName rule's own tokens rather than this lexer listing them.
    private readonly System.Collections.Generic.HashSet<int> _reservedNonNames =
        new System.Collections.Generic.HashSet<int>();
    public void SetReservedNonDataNames(System.Collections.Generic.IReadOnlySet<int> types)
    {
        _reservedNonNames.Clear();
        foreach (int t in types) _reservedNonNames.Add(t);
    }

    /// <summary>The edition-blind SUBSCRIPT-trigger set (every word that is a data-name in some edition), for
    /// TokenRetypes.PrimeLexer to narrow per compile.</summary>
    public static System.Collections.Generic.IReadOnlySet<int> SubscriptTriggerTokens => _dataNameTokens;

    // ⛔ THE REPORT-WRITER SUM CLAUSE IS NOT THE SUM FUNCTION (kb/Work PB924). SUM carries subscriptTrigger for
    // §8.4.3.2.3 SR2's keyword-omitted intrinsic call `SUM(1 2 3)`, and a reserved word can never be a data-name,
    // so that trigger exists ONLY for the function. ISO §13.18.54.3 SR9: "Within a report description entry, if
    // the keyword SUM is preceded by the keyword FUNCTION, SUM is a reference to the SUM intrinsic function.
    // Otherwise, SUM refers to the report writer SUM clause" — so inside the REPORT SECTION a bare SUM's '(' can
    // only open a parenthesised addend (arithmetic-expression-1), and capturing it in SUBSCRIPT mode made
    // `SUM (WS-K * WS-M)` unparseable while `SUM WS-K * WS-M` compiled. SR9's discriminator is the REGION, which
    // is why the trigger is made contextual here rather than recovered after the fact: the SUBSCRIPT decision
    // is frozen at lex time. The FUNCTION-keyword form is unaffected (PreviousIsFunctionName keys on FUNCTION
    // before the name, and SUM then stays the argument-list FNARG_LPAREN), and outside the REPORT SECTION the
    // keyword-omitted SUM(…) still captures exactly as before. SUM is the only §8.9 ∩ §8.11 word (LENGTH,
    // RANDOM, SIGN, SUM) that begins a report or data description clause whose operand can open with '(' — the
    // SIGN clause (§13.18.52) takes none — so it is the only word the region changes (CobolLexerModeDriftTests).
    private bool _inReportSection;
    private bool IsReportSumClauseKeyword() => _inReportSection && _lastNonWsTokenType == SUM;

    // The REPORT SECTION region: opened by the `REPORT SECTION` header, closed by the next section or division
    // header — the data division's fixed section order puts only the SCREEN SECTION or the PROCEDURE DIVISION after
    // it, and a nested program's REPORT SECTION follows its own header again.
    private void TrackReportSection(int type)
    {
        if (type == SECTION) _inReportSection = _lastNonWsTokenType == REPORT;
        else if (type == DIVISION) _inReportSection = false;
    }

    // >>COBOL-WORDS (ISO §7.3.10.4 GR3/GR4): the per-compilation-group set of KEYWORD token types the directive
    // de-reserves (UNDEFINE/SUBSTITUTE) — a following '(' must open a SUBSCRIPT even though the word is still lexed
    // as its keyword token here (the post-lex CobolWordsRewriter retypes it to IDENTIFIER afterwards, but the
    // SUBSCRIPT-mode decision at '(' is frozen at lex time and cannot be repaired later). Null by default: the
    // legacy pipeline never sets it, so the '(' decision stays byte-identical.
    private readonly System.Collections.Generic.HashSet<int> _cobolWordsDataNames =
        new System.Collections.Generic.HashSet<int>();
    public void SetCobolWordsDataNames(System.Collections.Generic.IReadOnlySet<int> types)
    {
        _cobolWordsDataNames.Clear();
        foreach (int t in types) _cobolWordsDataNames.Add(t);
    }
    private bool IsCobolWordsDataName(int t) => _cobolWordsDataNames.Contains(t);

    // FUNCTION-ARGUMENT REGION (P7 Step 12). '(' after "FUNCTION functionName" is the function's argument-list
    // paren (ISO §8.4.3.2 SR6) — it stays in DEFAULT mode so the arguments parse through the ONE
    // arithmeticExpression grammar. The keyword-omitted form name(args) (§8.4.3.2 SR2, D2) has no FUNCTION
    // token before the name, so it still pushes SUBSCRIPT and stays token-captured. Inside an argument region a
    // '(' after a data-name still pushes SUBSCRIPT (nested subscripts/ref-mod are untouched — the D10/PHASE-15
    // deferral). The paren stack tracks which OPEN DEFAULT-mode parens are function-argument regions so the
    // sign-adjacent literal twins below fire only there.
    private bool PreviousIsFunctionName() => _prevNonWsTokenType == FUNCTION;

    private readonly System.Collections.Generic.List<bool> _fnParenStack = new();
    private bool _primeFunctionArgs;

    // COMPILER-DIRECTIVE EXPRESSION region (compile-time expression evaluator, ISO §7.3.6/§7.3.7/§7.3.8). The
    // frontend fragment-parses a directive operand / constant-conditional-expression with the lexer primed here.
    // Two directive-only effects (both context-sensitive — nil global blast radius): (1) 'DEFINED' becomes a
    // token (the §7.3.8.4.4 defined-condition keyword — reserved nowhere else); (2) every '(' is a grouping
    // LPAREN, never a subscript. Effect (2) is required because the subscript-vs-grouping decision treats a '('
    // after any word that COULD be a data-name as a subscript, and the boolean operators (B-AND …) are legal
    // data-names below 2023, so `A B-AND (…)` would mis-lex the group in SUBSCRIPT mode; a directive operand
    // never subscripts, so every '(' is unambiguously a group (confirmed by token dump, DESIGN §4.1).
    private bool _primeDirectiveExpr;

    private bool InFunctionArgs() => _primeFunctionArgs || _fnParenStack.Contains(true);

    // Fragment re-parse hook (the D2 keyword-omitted argument re-parse): treat the WHOLE input as one
    // function-argument region (the fragment is the text inside the argument parens).
    public void PrimeFunctionArgs() => _primeFunctionArgs = true;

    // Fragment re-parse hook (the compile-time directive-expression parse): treat the WHOLE input as a directive
    // operand / cce region — DEFINED is a token and every '(' groups (DESIGN §4.1).
    public void PrimeDirectiveExpr() => _primeDirectiveExpr = true;

    // IDENTIFIER's action in a primed directive expression: the §7.3.8.4.4 keyword DEFINED is the one word the region
    // makes a token (a virtual token — see the tokens block). Reached only when primed, so the Text it reads is never
    // built for an ordinary source identifier.
    private void RetypeDirectiveKeyword()
    {
        if (string.Equals(Text, "DEFINED", System.StringComparison.OrdinalIgnoreCase)) Type = DEFINED;
    }

    // ⛔ AN ARGUMENT-LIST PARENTHESIS IS NOT AN ARITHMETIC PARENTHESIS, AND EMITTING BOTH AS LPAREN MADE EVERY
    // DOWNSTREAM PASS GUESS (fix-queue PB48). §8.4.3.2.3 SR6: "If a function's definition permits arguments and a
    // left parenthesis immediately follows function-prototype-name-1 or intrinsic-function-name-1, the left
    // parenthesis is ALWAYS treated as the left parenthesis of that function's arguments" — categorically, so it
    // is never the '(' of a parenthesized arithmetic expression. ZeroTokenRewriter reads adjacency to '(' / ')'
    // as proof that a figurative ZERO sits inside parenthesized arithmetic, which is true for a grouping paren and
    // FALSE for this one; the result was that `FUNCTION LOWER-CASE(ZERO)` reached the binder as an arithmetic
    // zero and was rejected as class numeric, while the keyword-omitted `LOWER-CASE(ZERO)` — whose arguments are
    // re-lexed by FunctionArgFragment, where no parens are present — returned the correct "0". Two arms of one
    // dispatch, one right (feedback_two_arm_dispatch).
    // ⛔ THE FIX IS THE TOKEN TYPE, NOT A SECOND COPY OF THE PREDICATE. The rewriter could have re-derived "is
    // this '(' an argument list" by walking back two tokens, but that is this method's rule written down twice,
    // and the SUBSCRIPT decision above proves how easily the two copies drift (feedback_one_rule_one_place). The
    // stack below is the single source of truth; retyping publishes it, so the rewriter's own rule — "ZERO
    // adjacent to an ARITHMETIC paren is arithmetic" — becomes true as written, with no change to it at all.
    private void OnDefaultLParen()
    {
        // A directive-expression region never subscripts: every '(' groups (DESIGN §4.1). Checked first so a '('
        // after a boolean-operator data-name (e.g. `A B-AND (…)`, B-AND legal as a name below 2023) stays a group.
        if (!_primeDirectiveExpr && PreviousTokenCouldBeDataName() && !PreviousIsFunctionName()
            && !IsReportSumClauseKeyword())
        {
            PushMode(SUBSCRIPT);   // subscript / ref-mod capture — the matching ')' is SUB_RPAREN (popMode)
            return;
        }
        // Staying DEFAULT: record whether this paren opens a FUNCTION argument region, and publish it as the type.
        // ⚠ THE `&& PreviousTokenCouldBeDataName()` CONJUNCT THAT STOOD HERE IS GONE, and dropping it is part of
        // the fix rather than a tidy-up. SR6 keys on the '(' following an intrinsic-function-name — it says
        // nothing about whether that name's TOKEN happens to be one of the context-sensitive words that can also
        // be a data name. Of the ten `functionName` alternatives, nine are in _dataNameTokens and BIT is not, so
        // the conjunct silently made `FUNCTION BIT(…)` the one function call whose parens were not an argument
        // region — a difference with no rule behind it, and exactly the sort of accidental exception a later
        // reader would preserve as intentional.
        _fnParenStack.Add(PreviousIsFunctionName());
        if (PreviousIsFunctionName()) Type = FNARG_LPAREN;
    }

    private void OnDefaultRParen()
    {
        if (_fnParenStack.Count == 0) return;   // unbalanced source — the parser reports it
        bool wasFnArgs = _fnParenStack[_fnParenStack.Count - 1];
        _fnParenStack.RemoveAt(_fnParenStack.Count - 1);
        if (wasFnArgs) Type = FNARG_RPAREN;
    }

    // ⛔ NO SEMANTIC PREDICATE ON A PATH THE DFA MUST CACHE — CONTEXT GOES IN AN ACTION (kb/Work PB1715). ANTLR
    // 4.13.1 never caches a mode's start state whose closure passed through a predicate (LexerATNSimulator.MatchATN)
    // nor a DFA edge whose target did (AddDFAEdge), so every token that REACHES a predicate re-runs ATN simulation
    // and re-adds its DFA state under lock(dfa.states) — on every occurrence, not once. Six left-edge predicates in
    // this mode (the four FN_SIGNED_* twins, DEFINED, FNARG_SEPARATOR) did that to EVERY token of every compile:
    // the CLI paid the simulation serially and parallel compiles serialized on the one lock (3.5x peak at 8
    // threads, DESIGN-test-build-ci.md §3.14.5 M6). Each match is now unconditional and an ACTION decides what it
    // is (DEFINED became a virtual token IDENTIFIER's action retypes); actions do not suppress DFA edges.
    // LexerDfaCacheDriftTests holds it: re-lexing the suite's sources after a warm-up leaves the cached DFA nowhere.

    // A signed numeric literal's sign is the leftmost CHARACTER of the literal (ISO §8.3.3.3.2 2): "If a sign is
    // used, it shall appear as the leftmost character of the literal") and an arithmetic operator "shall be
    // preceded by a space and followed by a space" (§8.7.1) — so inside a function-argument region a [+-] that
    // follows a separator and touches its digits starts a NEW argument (a signed literal), never a binary
    // operator: MAX(A -4) is two arguments, MAX(A - 4) is one subtraction. Asked by the FN_SIGNED_* action after
    // the match, so the separator test reads the character before the TOKEN'S START, not before the input index.
    private bool SignedLiteralCanStart()
    {
        if (!InFunctionArgs()) return false;
        int start = TokenStartCharIndex;
        if (start == 0) return true;   // fragment start (the D2 re-parse) — a separator by definition
        int before = InputStream.LA(start - 1 - InputStream.Index);   // LA(-k) is the character k before the index
        return before == ' ' || before == '\n' || before == ',' || before == ';'
            || before == '(';
    }

    // The FN_SIGNED_* action. A [+-] touching a numeric body always matches as ONE token (maximal munch over the
    // unconditional rule); where SignedLiteralCanStart holds it is the signed literal. Elsewhere the sign is the
    // arithmetic operator on its own, exactly what PLUS / MINUS lex, and the digits are a literal of their own: the
    // token is cut back to its first character and the lexer resumes at the digits, which re-lex as the unsigned
    // literal the old predicate-false path produced. No body character is a line break, so only the column moves.
    private void OnSignedLiteral(int signedType)
    {
        if (SignedLiteralCanStart()) { Type = signedType; return; }
        Type = InputStream.LA(TokenStartCharIndex - InputStream.Index) == '+' ? PLUS : MINUS;
        CutTokenTo(1);
    }

    // ⛔ THE ONE WAY AN ACTION GIVES BACK PART OF ITS MATCH (kb/Work PB1728). Ending a token early is two facts, not
    // one: where the input resumes AND the column the next token starts at — the lexer's Column is advanced by the
    // match and is NOT recomputed by a Seek. PIC_STRING's two trims used to Seek back over the separator alone, so
    // every token after `PIC 9,` on that line reported a column one too high (`01 Y PIC 9, VALUE 1.`: VALUE at 26,
    // correct 25) and every diagnostic anchored there pointed one column right. The token's text follows from the
    // cut (ANTLR reads it from the input interval the token spans), so no caller sets Text. Every caller cuts a
    // match that holds no line break — a sign and its digits, a PICTURE character-string — which is what makes the
    // column arithmetic exact; Line is untouched for the same reason.
    private void CutTokenTo(int length)
    {
        InputStream.Seek(TokenStartCharIndex + length);
        Column = TokenStartColumn + length;
    }

    public override Antlr4.Runtime.IToken NextToken()
    {
        var token = base.NextToken();
        if (token.Type != WS && token.Type != SUB_WS && token.Type != Antlr4.Runtime.TokenConstants.EOF)
        {
            TrackReportSection(token.Type);   // before the shift: it asks what PRECEDED this token
            _prevNonWsTokenType = _lastNonWsTokenType;
            _lastNonWsTokenType = token.Type;
        }
        return token;
    }
}

// ==========================================
// DEFAULT MODE
// ==========================================
// Assumes preprocessed input: fixed→free normalized, COPY/REPLACE expanded.

// The separator space is the space and the line end ONLY: the line-entry stage (PhysicalLines, kb/Work PB1800) has
// already expanded every TAB and taken every CR LF to its LF, so neither has a rule here.
WS           : [ \n]+ -> skip ;
COMMENT_START: '*>' -> skip, pushMode(COMMENT_MODE) ;

// ── END-xxx paired terminators (must precede END and IDENTIFIER) ──

END_IF       : 'END-IF' ;
END_PERFORM  : 'END-PERFORM' ;
END_EVALUATE : 'END-EVALUATE' ;
END_RECEIVE  : 'END-RECEIVE' ;  // MCS scope terminator (ISO 14.9.31)
END_READ     : 'END-READ' ;
END_SEND     : 'END-SEND' ;     // MCS scope terminator (ISO 14.9.38)
END_SEARCH   : 'END-SEARCH' ;
END_CALL     : 'END-CALL' ;
END_SORT     : 'END-SORT' ;
END_MERGE    : 'END-MERGE' ;
END_RETURN   : 'END-RETURN' ;
END_REWRITE  : 'END-REWRITE' ;
END_DELETE   : 'END-DELETE' ;
END_WRITE    : 'END-WRITE' ;
END_START    : 'END-START' ;
END_INVOKE   : 'END-INVOKE' ;
END_JSON     : 'END-JSON' ;
END_XML      : 'END-XML' ;
END_METHOD   : 'END-METHOD' ;
END_ADD      : 'END-ADD' ;
END_SUBTRACT : 'END-SUBTRACT' ;
END_MULTIPLY : 'END-MULTIPLY' ;
END_DIVIDE   : 'END-DIVIDE' ;
END_COMPUTE  : 'END-COMPUTE' ;
END_STRING   : 'END-STRING' ;
END_UNSTRING : 'END-UNSTRING' ;
END_ACCEPT   : 'END-ACCEPT' ;
END_DISPLAY  : 'END-DISPLAY' ;

END_OF_PAGE  : 'END-OF-PAGE' ;
EOP          : 'EOP' ;

// ── Hyphenated keywords (must precede IDENTIFIER) ──

PROGRAM_ID      : 'PROGRAM-ID' ;
FUNCTION_ID     : 'FUNCTION-ID' ;   // COBOL-2002 user-defined function unit header (ISO §11.5)
EXCLUSIVE_OR    : 'EXCLUSIVE-OR' ;  // COBOL-2023 logical exclusive-or operator, = XOR (ISO §8.8.4.9; a 2023 addition per Annex E.2 item 25 — VCR rows 32/41; the former "2002" note was the W3-corrected mislabel)
// COBOL-2002 BOOLEAN OPERATORS (ISO §8.7.2 / §8.8.2). Maximal-munch safe: `B-ANDER`/`B-ORDER` stay IDENTIFIER
// by longer match; `B-AND` exact reduces to the keyword by rule order (hyphenated keywords precede IDENTIFIER);
// BOOLLIT `B"…"` is disjoint (the char after B is a quote). User words at 85 (admitted via cobolWord +
// _dataNameTokens); the operator meaning is {is2002()}?-gated in the expression tiers.
B_AND           : 'B-AND' ;
B_OR            : 'B-OR' ;
B_XOR           : 'B-XOR' ;
B_NOT           : 'B-NOT' ;
// Boolean shift operators (ISO §8.8.2, COBOL-2023). Order-sensitive: the -LC/-RC literals FIRST so a longer
// match wins over the -L/-R prefix (ANTLR first-match; feedback_grammar_precedence).
B_SHIFT_LC      : 'B-SHIFT-LC' ;
B_SHIFT_RC      : 'B-SHIFT-RC' ;
B_SHIFT_L       : 'B-SHIFT-L' ;
B_SHIFT_R       : 'B-SHIFT-R' ;
FLOAT_SHORT     : 'FLOAT-SHORT' ;   // COBOL-2002 standard floating point: IEEE-754 single (= COMP-1)
FLOAT_LONG      : 'FLOAT-LONG' ;    // COBOL-2002 standard floating point: IEEE-754 double (= COMP-2)
FLOAT_EXTENDED  : 'FLOAT-EXTENDED' ;// COBOL-2002 extended float — mapped to IEEE-754 double (.NET has no quad)
// COBOL-2002 ROUNDED MODE names (ISO §14.7.4). Reserved words; corpus-checked (only in comments).
AWAY_FROM_ZERO          : 'AWAY-FROM-ZERO' ;
NEAREST_AWAY_FROM_ZERO  : 'NEAREST-AWAY-FROM-ZERO' ;
NEAREST_EVEN            : 'NEAREST-EVEN' ;
NEAREST_TOWARD_ZERO     : 'NEAREST-TOWARD-ZERO' ;
TOWARD_GREATER          : 'TOWARD-GREATER' ;
TOWARD_LESSER           : 'TOWARD-LESSER' ;
PROHIBITED              : 'PROHIBITED' ;
TRUNCATION              : 'TRUNCATION' ;
METHOD_ID       : 'METHOD-ID' ;
CLASS_ID        : 'CLASS-ID' ;
INTERFACE_ID    : 'INTERFACE-ID' ;
WORKING_STORAGE : 'WORKING-STORAGE' ;
LOCAL_STORAGE   : 'LOCAL-STORAGE' ;
SENTENCE        : 'SENTENCE' ;
DATE_WRITTEN    : 'DATE-WRITTEN' ;
DATE_COMPILED   : 'DATE-COMPILED' ;
SOURCE_COMPUTER : 'SOURCE-COMPUTER' ;
OBJECT_COMPUTER : 'OBJECT-COMPUTER' ;
OBJECT      : 'OBJECT' ;
OVERRIDE    : 'OVERRIDE' ;  // METHOD-ID attribute (ISO §11.7, 2002+; §8.9-reserved 2002+ — user word at 85, funnel row exists)
IMPLEMENTS  : 'IMPLEMENTS' ;  // FACTORY/OBJECT paragraph clause (§11.8; §8.10 CONTEXT-SENSITIVE — a user word at EVERY edition, never funneled)
PROPERTY    : 'PROPERTY' ;  // the PROPERTY clause / GET-SET selector / repository specifier (§13.18.42/§11.7/§12.3.8; §8.9-reserved 2002+)
GET         : 'GET' ;       // METHOD-ID GET PROPERTY selector (§11.7; §8.9-reserved 2002+ — user word at 85)
INTERFACE   : 'INTERFACE' ; // END INTERFACE + the repository INTERFACE specifier (§11.6/§12.3.8; §8.9-reserved 2002+)
FACTORY     : 'FACTORY' ;   // the FACTORY paragraph (ISO §11.4, 2002+; §8.9-reserved 2002+ — user word at 85, funnel row exists)
PROTOTYPE   : 'PROTOTYPE' ; // FUNCTION-ID … IS PROTOTYPE (ISO §11.5 Format 2 / §10.6; §8.9-reserved 2002+ — user word at 85, funnel row exists)
INHERITS    : 'INHERITS' ;   // COBOL-2002 OO: CLASS-ID. name INHERITS FROM base (ISO §11.3); corpus-clean
SPECIAL_NAMES   : 'SPECIAL-NAMES' ;
FILE_CONTROL    : 'FILE-CONTROL' ;
I_O_CONTROL     : 'I-O-CONTROL' ;
I_O             : 'I-O' ;
PACKED_DECIMAL  : 'PACKED-DECIMAL' ;
// COBOL-2002 fixed-width binary usages of the USAGE clause (ISO §13.18.60). Must precede
// BINARY/IDENTIFIER; maximal munch
// matches the full 'BINARY-xxx' over the shorter 'BINARY' token.
BINARY_CHAR     : 'BINARY-CHAR' ;
BINARY_SHORT    : 'BINARY-SHORT' ;
BINARY_LONG     : 'BINARY-LONG' ;
BINARY_DOUBLE   : 'BINARY-DOUBLE' ;
// COBOL-2002 OPTIONS-paragraph clause words (ISO §11.9). All are CONTEXT-SENSITIVE — reserved only inside the
// OPTIONS paragraph — so each is ALSO listed in cobolWord (CobolParserCore.g4) and mirrored in _dataNameTokens
// above, keeping it legal as a user-defined name everywhere else. Hyphenated forms must precede IDENTIFIER so
// maximal-munch + first-rule-wins picks the whole keyword (e.g. 'STANDARD-BINARY' over IDENTIFIER); a bare
// 'STANDARD' still lexes as STANDARD, and 'WC-COBOL' still lexes as one IDENTIFIER. Corpus-checked: zero of these
// appear as user-defined words in the test corpus.
STANDARD_BINARY   : 'STANDARD-BINARY' ;
STANDARD_DECIMAL  : 'STANDARD-DECIMAL' ;
ENTRY_CONVENTION  : 'ENTRY-CONVENTION' ;
FLOAT_BINARY      : 'FLOAT-BINARY' ;
FLOAT_DECIMAL     : 'FLOAT-DECIMAL' ;
// The COBOL-2014 IEEE-754 interchange float USAGES (ISO §13.18.60.4 GR14-18): FLOAT-BINARY-32/64/128 =
// ISO/IEC 60559:2020 binary32/64/128, FLOAT-DECIMAL-16/34 = decimal64/128. Dedicated hyphenated tokens declared
// BEFORE IDENTIFIER so maximal-munch + first-rule-wins picks the whole keyword (the source text 'FLOAT-BINARY-32'
// lexes as ONE IDENTIFIER otherwise — the numeric suffix is swallowed by NAME_BODY). Introduction-gated to 2014
// post-bind (VersionConformancePass, via the Usage member); binary32/64 are LIVE (native float/double), the
// binary128/decimal formats are processor-dependent non-support (Annex A.3 items 17/19, COBOLNET1564).
FLOAT_BINARY_32   : 'FLOAT-BINARY-32' ;
FLOAT_BINARY_64   : 'FLOAT-BINARY-64' ;
FLOAT_BINARY_128  : 'FLOAT-BINARY-128' ;
FLOAT_DECIMAL_16  : 'FLOAT-DECIMAL-16' ;
FLOAT_DECIMAL_34  : 'FLOAT-DECIMAL-34' ;
// The COBOL-2014 NUMERIC-CONTENT value words (ISO §14.9.39.2 Format 15 — `SET CONTENT OF … TO`; kb/Work PB452).
// Hyphenated, so each must precede IDENTIFIER: the source text lexes as ONE IDENTIFIER of the SAME length
// otherwise, and an equal-length tie is broken by rule order. FLOAT-NOT-A-NUMBER-SIGNALING needs no ordering
// against FLOAT-NOT-A-NUMBER (maximal munch takes the longer match regardless of order) but is written first so
// the pair reads the way it lexes. Reserved only from 2014 (ReservedWords.Table), so every one is ALSO a
// cobolWord alternative under the derived reservation gate and mirrored in _dataNameTokens
// (tests/version-matrix/cobol-words.json) — `01 FLOAT-INFINITY PIC X.` stays legal at --std 85/2002 and draws
// COBOLNET0901 by name at 2014+.
// All seven are also §8.8.4.4.2 class-condition alternatives (kb/Work PB225, the `className` rule).
// FLOAT-NOT-A-NUMBER-QUIET's ONLY surface is that class condition — Format 15 does not offer it — so its token
// landed with the class condition that reads it; maximal munch orders it against FLOAT-NOT-A-NUMBER.
FARTHEST_FROM_ZERO           : 'FARTHEST-FROM-ZERO' ;
NEAREST_TO_ZERO              : 'NEAREST-TO-ZERO' ;
IN_ARITHMETIC_RANGE          : 'IN-ARITHMETIC-RANGE' ;
FLOAT_INFINITY               : 'FLOAT-INFINITY' ;
FLOAT_NOT_A_NUMBER_SIGNALING : 'FLOAT-NOT-A-NUMBER-SIGNALING' ;
FLOAT_NOT_A_NUMBER_QUIET     : 'FLOAT-NOT-A-NUMBER-QUIET' ;
FLOAT_NOT_A_NUMBER           : 'FLOAT-NOT-A-NUMBER' ;
HIGH_ORDER_LEFT   : 'HIGH-ORDER-LEFT' ;
HIGH_ORDER_RIGHT  : 'HIGH-ORDER-RIGHT' ;
BINARY_ENCODING   : 'BINARY-ENCODING' ;
DECIMAL_ENCODING  : 'DECIMAL-ENCODING' ;
ARITHMETIC        : 'ARITHMETIC' ;
DEFAULT           : 'DEFAULT' ;
INTERMEDIATE      : 'INTERMEDIATE' ;
ROUNDING          : 'ROUNDING' ;
// BLANK [WHEN] ZERO is parsed as individual tokens in the parser grammar
// X3.23-1985 RERUN clause unit (obsolete '85 element deleted by ISO 2002 — accepted-inert at 85, 0902 ≥2002
// per VCR Table 7 row 7.15). Hyphenated, so it must precede IDENTIFIER; a longer user word like
// CLOCK-UNITS-X still lexes as one IDENTIFIER (maximal munch). Also a legal user word via cobolWord where
// the §8.9 funnel frees it (mirrored in _dataNameTokens).
CLOCK_UNITS     : 'CLOCK-UNITS' ;
// GROUP-USAGE (ISO §13.18.29, COBOL-2002 — kb/Work PB79): hyphenated, so it precedes IDENTIFIER; a user word
// below 2002 via cobolWord (mirrored in _dataNameTokens), funnel-0901'd at >=2002 as a name.
GROUP_USAGE     : 'GROUP-USAGE' ;
DAY_OF_WEEK     : 'DAY-OF-WEEK' ;
REVERSE_VIDEO   : 'REVERSE-VIDEO' ;
FOREGROUND_COLOR: 'FOREGROUND-COLOR' ;
BACKGROUND_COLOR: 'BACKGROUND-COLOR' ;

// ── Division/section keywords ──

IDENTIFICATION : 'IDENTIFICATION' ;
CONFIGURATION : 'CONFIGURATION' ;
DIVISION    : 'DIVISION' ;
ENVIRONMENT : 'ENVIRONMENT' ;
DATA        : 'DATA' ;
PROCEDURE   : 'PROCEDURE' ;
REPORT      : 'REPORT' ;
SCREEN      : 'SCREEN' ;
SECTION     : 'SECTION' ;
LINKAGE     : 'LINKAGE' ;
INPUT_OUTPUT : 'INPUT-OUTPUT' ;
FD          : 'FD' ;
RD          : 'RD' ;
SD          : 'SD' ;

// ── Statement keywords ──

ACCEPT      : 'ACCEPT' ;
ADD         : 'ADD' ;
ALTER       : 'ALTER' ;
CALL        : 'CALL' ;
CANCEL      : 'CANCEL' ;
CAPACITY    : 'CAPACITY' ;   // OCCURS DYNAMIC … CAPACITY IN data-name (ISO §13.18.38 Format 4, COBOL-2014; D9)
CLOSE       : 'CLOSE' ;
COMPUTE     : 'COMPUTE' ;
CONTINUE    : 'CONTINUE' ;
DELETE      : 'DELETE' ;
DISPLAY     : 'DISPLAY' ;
// X3.23-1985 ENTER statement verb (obsolete '85 element deleted by ISO 2002 — accepted-inert at 85,
// 0902 ≥2002 per VCR Table 7 row 7.16). A legal user word via cobolWord at editions where the §8.9
// funnel frees it (85-only reserved per ReservedWords.Table); mirrored in _dataNameTokens.
ENTER       : 'ENTER' ;
GENERATE    : 'GENERATE' ;
DIVIDE      : 'DIVIDE' ;
EVALUATE    : 'EVALUATE' ;
EXIT        : 'EXIT' ;
GOBACK      : 'GOBACK' ;
GO          : 'GO' ;
IF          : 'IF' ;
INITIATE    : 'INITIATE' ;
INITIALIZE  : 'INITIALIZE' ;
INITIALIZED : 'INITIALIZED' ;
INSPECT     : 'INSPECT' ;
ALLOCATE    : 'ALLOCATE' ;
FREE        : 'FREE' ;
INVOKE      : 'INVOKE' ;
JSON        : 'JSON' ;
// MESSAGE-TAG usage (ISO §13.18.60 — the MCS data surface; reserved 2023 only per §8.9 / Annex E.2 item 25).
// Maximal munch keeps this ahead of MESSAGE: 'MESSAGE-TAG' is the longer match. It had NO token until kb/Work
// PB487, so the bare (USAGE-less) spelling §13.18.60.2 makes legal fell to the genericDataClause catch-all and
// the entry bound with NO usage at all — a compiler crash on the elementary path. A legal user word below 2023,
// so it is cobolWord/_dataNameTokens-admitted (cobol-words.json) and funnel-0901'd at 2023.
MESSAGE_TAG : 'MESSAGE-TAG' ;
MESSAGE     : 'MESSAGE' ;      // MCS: the CONTINUE AFTER MESSAGE RECEIVED phrase (ISO 14.9.31)
MERGE       : 'MERGE' ;
MOVE        : 'MOVE' ;
MULTIPLY    : 'MULTIPLY' ;
OPEN        : 'OPEN' ;
PERFORM     : 'PERFORM' ;
READ        : 'READ' ;
RELEASE     : 'RELEASE' ;
REPOSITORY  : 'REPOSITORY' ;   // COBOL-2002 REPOSITORY paragraph header (ISO §12.3.8)
RETURN      : 'RETURN' ;
REWRITE     : 'REWRITE' ;
SEARCH      : 'SEARCH' ;
SET         : 'SET' ;
SORT        : 'SORT' ;
START       : 'START' ;
STOP        : 'STOP' ;
STRING      : 'STRING' ;
SUBTRACT    : 'SUBTRACT' ;
SUPPRESS    : 'SUPPRESS' ;
TERMINATE   : 'TERMINATE' ;
UNSTRING    : 'UNSTRING' ;
WRITE       : 'WRITE' ;
XML         : 'XML' ;

// ── Clause/phrase keywords ──

// ACTIVE-CLASS (ISO §13.18.60.2 USAGE OBJECT REFERENCE [FACTORY OF] ACTIVE-CLASS) — a required word of
// the format, §8.9-reserved 2002+ and a USER WORD at 85, so it rides the cobolWord reservation funnel like
// FACTORY (tests/version-matrix/cobol-words.json). Before kb/Work PB389 it had NO token: the phrase lexed as
// an IDENTIFIER, parsed as `OBJECT REFERENCE className`, and drew COBOLNET0813 'unknown class ACTIVE-CLASS'
// PLUS COBOLNET0901 'reserved word' — a required word of the general format reported as a botched user name.
ACTIVE_CLASS : 'ACTIVE-CLASS' ;
ACCESS      : 'ACCESS' ;
ADDRESS     : 'ADDRESS' ;
// ALIGNED clause (ISO §13.18.1; reserved 2002+ per §8.9) — a §13.16.2 Format-1 data-description clause. It had NO
// token and NO parser rule until kb/Work PB487, so it lexed as IDENTIFIER and the genericDataClause catch-all ate
// it: the clause was ACCEPTED and INERT, which is the silent-wrong-layout case §13.18.1.4 GR1 exists to prevent.
// A legal user word at COBOL-85, so it is cobolWord/_dataNameTokens-admitted (cobol-words.json) and funnel-0901'd
// ≥2002 by the VersionConformancePass §8.9 funnel (the CONSTANT/BASED precedent).
ALIGNED     : 'ALIGNED' ;
AREA        : 'AREA' ;
BASED       : 'BASED' ;
// CONSTANT (ISO §13.10 constant entry / §13.18.15 CONSTANT RECORD clause; reserved 2002+ per §8.9) — a legal
// user word at COBOL-85, so it is cobolWord/_dataNameTokens-admitted (cobol-words.json) and funnel-0901'd ≥2002
// by the VersionConformancePass §8.9 funnel (the PROTOTYPE/SHARING precedent).
CONSTANT    : 'CONSTANT' ;
// AS (the §13.10 constant entry's AS phrase; reserved 2002+ per §8.9) — the same interval as CONSTANT: a legal
// user word at COBOL-85, cobolWord/_dataNameTokens-admitted (cobol-words.json), funnel-0901'd ≥2002. Its only
// keyword slot is the constantEntryBody `AS` — a direct token, never a name slot.
AS          : 'AS' ;
// PROGRAM-POINTER / FUNCTION-POINTER (USAGE phrases, ISO §13.18.60; reserved 2002+ per §8.9) — the CONSTANT/AS
// interval treatment: legal user words at COBOL-85, cobolWord/_dataNameTokens-admitted (cobol-words.json),
// funnel-0901'd ≥2002. Their only keyword slots are the usageKeyword alternatives — direct tokens.
PROGRAM_POINTER  : 'PROGRAM-POINTER' ;
FUNCTION_POINTER : 'FUNCTION-POINTER' ;
// ── The remaining INITIALIZE category-names (ISO §14.9.20.2 "where category-name is:") ────────────────────────
// The printed figure encloses THIRTEEN underlined words in a brace carrying CHOICE INDICATORS; five of them had
// no lexer token at all, so eight of the thirteen could not be spelled (kb/Work PB415). BOOLEAN / DATA-POINTER /
// NATIONAL-EDITED / OBJECT-REFERENCE are the four that were still lexing as IDENTIFIER (MESSAGE-TAG,
// PROGRAM-POINTER, FUNCTION-POINTER and NATIONAL already had one). §8.9 reserves all four from 2002 only
// (reserved-words.json r85=false), so each gets the CONSTANT/AS interval treatment: a legal user word at
// COBOL-85, cobolWord/_dataNameTokens-admitted through cobol-words.json, funnel-0901'd ≥2002. The hyphenated
// spellings out-length DATA / NATIONAL / OBJECT by maximal munch, so rule order against them is not load-bearing.
BOOLEAN          : 'BOOLEAN' ;            // §8.5.2.5 category boolean (also the §13.18.60 USAGE BIT category)
DATA_POINTER     : 'DATA-POINTER' ;       // §8.5.2.6 category data-pointer (USAGE POINTER's category-name)
NATIONAL_EDITED  : 'NATIONAL-EDITED' ;    // §8.5.2.11 category national-edited
OBJECT_REFERENCE : 'OBJECT-REFERENCE' ;   // §8.5.2.12 category object-reference (USAGE OBJECT REFERENCE is TWO words)
AREAS       : 'AREAS' ;
ALPHABETIC       : 'ALPHABETIC' ;
ALPHABETIC_LOWER : 'ALPHABETIC-LOWER' ;
ALPHABETIC_UPPER : 'ALPHABETIC-UPPER' ;
ADVANCING   : 'ADVANCING' ;
AFTER       : 'AFTER' ;
ALL         : 'ALL' ;
ALSO        : 'ALSO' ;
ALPHANUMERIC_EDITED : 'ALPHANUMERIC-EDITED' ;
NUMERIC_EDITED : 'NUMERIC-EDITED' ;
ALPHANUMERIC : 'ALPHANUMERIC' ;
ALTERNATE   : 'ALTERNATE' ;
AND         : 'AND' ;
ANY         : 'ANY' ;
ASCENDING   : 'ASCENDING' ;
// APPLY COMMIT clause (ISO §12.4.6.3) — the declined A.4.3 commit-and-rollback facility's I-O-CONTROL surface.
// APPLY is a §8.10 CONTEXT-SENSITIVE word ("I-O-CONTROL paragraph"), never §8.9-reserved, so it stays a legal
// user-defined word at every edition through cobolWord.
APPLY       : 'APPLY' ;
ASSIGN      : 'ASSIGN' ;
ARE         : 'ARE' ;
AT          : 'AT' ;
AUTO        : 'AUTO' ;
AUTHOR      : 'AUTHOR' ;
BACKWARD    : 'BACKWARD' ;   // COBOL-2002 INSPECT … BACKWARD (right-to-left inspection, ISO §14.9.22)
BEFORE      : 'BEFORE' ;
BELL        : 'BELL' ;
BLINK       : 'BLINK' ;
BINARY      : 'BINARY' ;
BIT         : 'BIT' ;
BLANK       : 'BLANK' ;
BLOCK       : 'BLOCK' ;
BOTTOM      : 'BOTTOM' ;
BY          : 'BY' ;
// The RECORD clause's BYTES/CHARACTERS brace group (ISO §13.18.43.2, all three formats): §13.18.43.3 SR2 makes
// the two words SYNONYMOUS ("The words BYTES and CHARACTERS are synonymous and may be used interchangeably"),
// and until kb/Work PB721 there was no BYTES token at all, so `RECORD CONTAINS 20 BYTES` — legal COBOL in every
// supported edition — was refused as an unrecognized FD clause (COBOLNET1970). §8.10 context-sensitive ("RECORD
// clause"), never §8.9-reserved, so it stays a legal user word at every edition through cobolWord.
BYTES       : 'BYTES' ;
CF          : 'CF' ;
CH          : 'CH' ;
CHARACTER   : 'CHARACTER' ;
CHARACTERS  : 'CHARACTERS' ;
CLASS       : 'CLASS' ;
CODE        : 'CODE' ;
CODE_SET    : 'CODE-SET' ;
COL         : 'COL' ;
COLS        : 'COLS' ;      // COBOL-2002 COLUMN-clause spelling (ISO §13.18.14 SR1); usable as a user word via cobolWord (§8.9 funnel gates ≥2002)
COLUMN      : 'COLUMN' ;
COLUMNS     : 'COLUMNS' ;   // COBOL-2002 COLUMN-clause spelling (ISO §13.18.14 SR1); usable as a user word via cobolWord (§8.9 funnel gates ≥2002)
COLLATING   : 'COLLATING' ;
COMMIT      : 'COMMIT' ;       // commit/rollback facility (A.3 items 6-7) - recognized-not-supported
COMMON      : 'COMMON' ;
COMP        : 'COMP' ;
COMP_1      : 'COMP-1' ;
COMP_2      : 'COMP-2' ;
COMP_3      : 'COMP-3' ;
COMP_4      : 'COMP-4' ;
COMP_5      : 'COMP-5' ;
COMPUTATIONAL   : 'COMPUTATIONAL' ;
COMPUTATIONAL_1 : 'COMPUTATIONAL-1' ;
COMPUTATIONAL_2 : 'COMPUTATIONAL-2' ;
COMPUTATIONAL_3 : 'COMPUTATIONAL-3' ;
COMPUTATIONAL_4 : 'COMPUTATIONAL-4' ;
COMPUTATIONAL_5 : 'COMPUTATIONAL-5' ;
CONTAINS    : 'CONTAINS' ;
CONTENT     : 'CONTENT' ;
CONTROL     : 'CONTROL' ;
CONTROLS    : 'CONTROLS' ;
CONVERTING  : 'CONVERTING' ;
CORR        : 'CORR' ;
CURRENCY    : 'CURRENCY' ;
CYCLE       : 'CYCLE' ;
DECIMAL_POINT : 'DECIMAL-POINT' ;
CORRESPONDING : 'CORRESPONDING' ;
COUNT       : 'COUNT' ;
DATE        : 'DATE' ;
DAY         : 'DAY' ;
DE          : 'DE' ;
DETAIL      : 'DETAIL' ;
YYYYMMDD    : 'YYYYMMDD' ;
YYYYDDD     : 'YYYYDDD' ;
DECLARATIVES: 'DECLARATIVES' ;
DELIMITED   : 'DELIMITED' ;
DELIMITER   : 'DELIMITER' ;
DEPENDING   : 'DEPENDING' ;
DESCENDING  : 'DESCENDING' ;
// DESTINATION clause (ISO §13.18.18) — the declined A.4.14 VALIDATE facility's data-division surface. §8.9
// reserves the word at EVERY edition, but it is still admitted to cobolWord: the token must reach a name slot
// so `01 DESTINATION PIC X.` keeps drawing the NAMED COBOLNET0901 reserved-word diagnostic instead of the
// generic parse error a keyword-only token would produce (the PB100 lesson — a declined facility must not
// degrade a neighbouring diagnostic).
DESTINATION : 'DESTINATION' ;
DOWN        : 'DOWN' ;
DUPLICATES  : 'DUPLICATES' ;
DYNAMIC     : 'DYNAMIC' ;
// ── The X3.23-1985 notInGrammar 85-acceptance words (VCR Table 7 rows 7.15–7.18; obsolete '85 elements
//    deleted by ISO 2002; each is ALSO a legal user word via cobolWord at the editions where the §8.9
//    funnel frees it — all five are mirrored in _dataNameTokens above, with ENTER and CLOCK-UNITS in
//    their own bands) ──
DEBUGGING   : 'DEBUGGING' ;   // USE FOR DEBUGGING (the '85 debug facility, row 7.17)
EVERY       : 'EVERY' ;       // RERUN … EVERY (row 7.15)
RERUN       : 'RERUN' ;       // the I-O-CONTROL RERUN clause head (row 7.15)
REFERENCES  : 'REFERENCES' ;  // USE FOR DEBUGGING ON ALL REFERENCES OF (row 7.17); distinct from REFERENCE
PROCEDURES  : 'PROCEDURES' ;  // USE FOR DEBUGGING ON ALL PROCEDURES (row 7.17); distinct from PROCEDURE
// ⛔ NO `EDITED` TOKEN, DELIBERATELY (kb/Work PB415). EDITED is not an ISO §8.9 reserved word at 85, 2002, 2014 or
// 2023 — it has no reserved-words.json row — and its only grammar consumer was the two-token
// `ALPHANUMERIC EDITED` / `NUMERIC EDITED` spelling of an INITIALIZE category-name, which §14.9.20.2 prints only
// hyphenated. The hard token shadowed IDENTIFIER, so `01 EDITED PIC X.` was a COBOL0001 at every edition; with
// the token gone the word is an ordinary user-defined word, which is what §8.3.2.1 makes it. (EDITING, below, is
// a genuine 2023 reserved word and keeps its token.)
EDITING     : 'EDITING' ;                 // PICTURE EDITING phrase (ISO §13.18.40.2; new-in-2023 reserved word, Annex E.2 item 25)
ELSE        : 'ELSE' ;
END         : 'END' ;
EOL         : 'EOL' ;
EOS         : 'EOS' ;
ERASE       : 'ERASE' ;
ENTRY       : 'ENTRY' ;
EQUAL       : 'EQUAL' ;
ERROR       : 'ERROR' ;
EXCEPTION   : 'EXCEPTION' ;
EXTEND      : 'EXTEND' ;
EXTERNAL    : 'EXTERNAL' ;
FIRST       : 'FIRST' ;
FOOTING     : 'FOOTING' ;
FOR         : 'FOR' ;
FALSE_      : 'FALSE' ;
FILE        : 'FILE' ;
FILLER      : 'FILLER' ;
FINAL       : 'FINAL' ;
FINALLY     : 'FINALLY' ;                 // exception-checking PERFORM FINALLY phrase (ISO §14.9.28.2 Format 3; new-in-2023 reserved word, Annex E.2 item 25)
FORMAT      : 'FORMAT' ;                  // FD FORMAT clause (ISO §13.18.24). Reserved from 2002 only (ReservedWords.Table row: r85=false) — so it is cobolWord/nameSlot-admitted (tests/version-matrix/cobol-words.json) and a COBOL-85 program may still name a data item FORMAT; §8.9's COBOLNET0901 funnel names it at 2002+.
FULL_       : 'FULL' ;
POSITIVE    : 'POSITIVE' ;
NEGATIVE    : 'NEGATIVE' ;
REQUIRED    : 'REQUIRED' ;
RESERVE     : 'RESERVE' ;
FROM        : 'FROM' ;
FUNCTION    : 'FUNCTION' ;
GROUP       : 'GROUP' ;
HEADING     : 'HEADING' ;
HIGHLIGHT   : 'HIGHLIGHT' ;
INDICATE    : 'INDICATE' ;
LABEL       : 'LABEL' ;
LAST        : 'LAST' ;
LINAGE      : 'LINAGE' ;
LINAGE_COUNTER : 'LINAGE-COUNTER' ;
LIMIT       : 'LIMIT' ;
LIMITS      : 'LIMITS' ;
LINE_COUNTER : 'LINE-COUNTER' ;
GENERIC     : 'GENERIC' ;
GIVING      : 'GIVING' ;
GLOBAL      : 'GLOBAL' ;
GREATER     : 'GREATER' ;
SYMBOLIC    : 'SYMBOLIC' ;
TABLE       : 'TABLE' ;
ALPHABET    : 'ALPHABET' ;
CRT         : 'CRT' ;
CURSOR      : 'CURSOR' ;
CHANNEL     : 'CHANNEL' ;
PROCEED     : 'PROCEED' ;
UPON        : 'UPON' ;
USE         : 'USE' ;
STANDARD    : 'STANDARD' ;
REPORTING   : 'REPORTING' ;
SUM         : 'SUM' ;
IN          : 'IN' ;
INDEX       : 'INDEX' ;
INDEXED     : 'INDEXED' ;
INITIAL_    : 'INITIAL' ;
INPUT       : 'INPUT' ;
INSTALLATION: 'INSTALLATION' ;
INTRINSIC   : 'INTRINSIC' ;   // COBOL-2002 REPOSITORY FUNCTION … INTRINSIC (ISO §12.3.8)
INTO        : 'INTO' ;
INVALID     : 'INVALID' ;
IS          : 'IS' ;
JUST        : 'JUST' ;
JUSTIFIED   : 'JUSTIFIED' ;
KEY         : 'KEY' ;
LEADING     : 'LEADING' ;
LEFT        : 'LEFT' ;
LENGTH      : 'LENGTH' ;
LESS        : 'LESS' ;
LINE        : 'LINE' ;
LINES       : 'LINES' ;
LOCATION    : 'LOCATION' ;                // exception-checking PERFORM WITH LOCATION phrase (ISO §14.9.28.2 Format 3; new-in-2023 reserved word, Annex E.2 item 25)
LOCK        : 'LOCK' ;
LOWLIGHT    : 'LOWLIGHT' ;
// ── COBOL-2002 file sharing / record locking (ISO §12.4.5.9 LOCK MODE, §12.4.5.15 SHARING, §14.7.9 RETRY,
// §14.9.27 OPEN, §14.9.30 READ, §14.9.47 UNLOCK). SHARING/RETRY/
// UNLOCK are §8.9-reserved since 2002 (funnel-gated); MANUAL/AUTOMATIC/IGNORING/FOREVER/SECONDS/ONLY are
// §8.10 context-sensitive (user-legal at every edition). The operator meaning is {is2002()}?-gated in CobolIO.g4.
MANUAL      : 'MANUAL' ;
AUTOMATIC   : 'AUTOMATIC' ;
IGNORING    : 'IGNORING' ;
FOREVER     : 'FOREVER' ;
SECONDS     : 'SECONDS' ;
SHARING     : 'SHARING' ;
RETRY       : 'RETRY' ;
UNLOCK      : 'UNLOCK' ;
METHOD      : 'METHOD' ;
MODE        : 'MODE' ;
NATIONAL    : 'NATIONAL' ;
NATIVE      : 'NATIVE' ;
NEXT        : 'NEXT' ;
NORMAL      : 'NORMAL' ;
NO          : 'NO' ;
NUMBER      : 'NUMBER' ;
NUMBERS     : 'NUMBERS' ;   // COLUMN/LINE clause plural spelling (ISO §13.18.14/§13.18.35) — §8.10 context-sensitive (user word at every edition)
NOT         : 'NOT' ;
NUMERIC     : 'NUMERIC' ;
NULL_       : 'NULL' ;
OCCURS      : 'OCCURS' ;
OF          : 'OF' ;
OFF         : 'OFF' ;
ON          : 'ON' ;
ONLY        : 'ONLY' ;   // COBOL-2002 SHARING READ ONLY (ISO §12.4.5.15) — §8.10 context-sensitive (user word)
OR          : 'OR' ;
XOR         : 'XOR' ;   // COBOL-2023 logical exclusive-or operator (ISO §8.8.4.9; 2023 per Annex E.2 item 25 — VCR rows 32/41)
OMITTED     : 'OMITTED' ;
OPTIONAL    : 'OPTIONAL' ;
OPTIONS     : 'OPTIONS' ;   // COBOL-2002 OPTIONS paragraph header (ISO §11.9)
// The OPTIONAL word ORDER of the SORT DUPLICATES phrase (ISO §14.9.40.2 `[ WITH DUPLICATES IN ORDER ]`) and the
// REQUIRED word of the SPECIAL-NAMES ORDER TABLE clause (§12.3.7.2). §8.9 reserves ORDER from 2002
// (ReservedWords.Table row: r85=false), so — exactly like FORMAT — the token carries a `nameSlot` row in
// tests/version-matrix/cobol-words.json, which is what keeps `01 ORDER PIC X.` legal at COBOL-85 while the
// derived reservation gate + the §8.9 funnel name it at 2002+. ⛔ IT MUST BE A TOKEN: a keyword slot that
// borrows `cobolWord` is refused by the funnel's position-blind IDENTIFIER check (kb/Work PB704, the PB693
// shape). Maximal munch keeps HIGH-ORDER-LEFT/HIGH-ORDER-RIGHT and any ORDER-suffixed user word one longer match.
ORDER       : 'ORDER' ;
ORGANIZATION: 'ORGANIZATION' ;
OTHER       : 'OTHER' ;
OUTPUT      : 'OUTPUT' ;
OVERFLOW    : 'OVERFLOW' ;
PACKED      : 'PACKED' ;
PAGE        : 'PAGE' ;
PAGE_COUNTER : 'PAGE-COUNTER' ;
PADDING     : 'PADDING' ;
PARAGRAPH   : 'PARAGRAPH' ;
PARSE       : 'PARSE' ;        // JSON/XML PARSE (2014+); usable as a user word via cobolWord
PROCESSING  : 'PROCESSING' ;   // XML PARSE … PROCESSING PROCEDURE (2014+); usable as a user word via cobolWord
PF          : 'PF' ;
PH          : 'PH' ;
// PIC/PICTURE → push into PICMODE to capture the PIC string as one token.
// Handles: PIC X(120), PIC IS S9(18), PICTURE $$$,$$9.99CR, etc.
PIC         : ('PIC' | 'PICTURE') -> pushMode(PICMODE) ;
POINTER     : 'POINTER' ;
PLUSWORD    : 'PLUS' ;       // the reserved WORD PLUS (LINE/NEXT GROUP relative); distinct from PLUS ('+')
PRESENT     : 'PRESENT' ;   // PRESENT WHEN clause (ISO §13.18.41, 2002+); usable as a user word via cobolWord (§8.9 funnel gates ≥2002)
PREVIOUS    : 'PREVIOUS' ;
PRINTING    : 'PRINTING' ;
PROGRAM     : 'PROGRAM' ;
// ── EC exception-model words (ISO 2002+; each is ALSO a legal user word via cobolWord + _dataNameTokens) ──
RAISING     : 'RAISING' ;    // GOBACK/EXIT … RAISING + the PD-header RAISING phrase (ISO §14.9.18 / §14.2)
RAISE       : 'RAISE' ;      // RAISE statement (ISO §14.9.29)
RESUME      : 'RESUME' ;     // RESUME statement (ISO §14.9.33)
STATEMENT   : 'STATEMENT' ;  // RESUME AT NEXT STATEMENT (ISO §14.9.33)
CONDITION   : 'CONDITION' ;  // USE AFTER EXCEPTION CONDITION (ISO §14.9.49 Format 3)
EO          : 'EO' ;         // USE AFTER EO ≡ EXCEPTION OBJECT (ISO §14.9.49.3 SR15; the EC-OO wave) — same context-sensitive recipe as EC
EC          : 'EC' ;         // USE AFTER EC ≡ EXCEPTION CONDITION (ISO §14.9.49.3 SR12); maximal munch keeps
                             // EC-I-O-AT-END etc. one IDENTIFIER (the longer match wins)
RANDOM      : 'RANDOM' ;
RECEIVE     : 'RECEIVE' ;      // MCS RECEIVE (ISO 14.9.31) - recognized-not-supported (4.2.6, A.3 item 4)
RECORD      : 'RECORD' ;
RECORDS     : 'RECORDS' ;
REEL        : 'REEL' ;
REPORTS     : 'REPORTS' ;
RESET       : 'RESET' ;
RECURSIVE   : 'RECURSIVE' ;
REDEFINES   : 'REDEFINES' ;
REPLACING   : 'REPLACING' ;
REFERENCE   : 'REFERENCE' ;
RELATIVE    : 'RELATIVE' ;
REMAINDER   : 'REMAINDER' ;
REMOVAL     : 'REMOVAL' ;
REMARKS     : 'REMARKS' ;
RENAMES     : 'RENAMES' ;
RETURNING   : 'RETURNING' ;
REWIND      : 'REWIND' ;
REVERSED    : 'REVERSED' ;
RF          : 'RF' ;
RH          : 'RH' ;
ROLLBACK    : 'ROLLBACK' ;     // commit/rollback facility (A.3 items 6-7) - recognized-not-supported
ROUNDED     : 'ROUNDED' ;
RIGHT       : 'RIGHT' ;
RUN         : 'RUN' ;
SAME        : 'SAME' ;
STRONG      : 'STRONG' ;   // TYPEDEF STRONG (ISO §13.18.58.2, COBOL-2002; data-model D17)
SORT_MERGE  : 'SORT-MERGE' ;
MULTIPLE    : 'MULTIPLE' ;
TAPE        : 'TAPE' ;
POSITION    : 'POSITION' ;
SEND        : 'SEND' ;         // MCS SEND (ISO 14.9.38) - recognized-not-supported (4.2.6, A.3 item 4)
SECURE      : 'SECURE' ;
SECURITY    : 'SECURITY' ;
SELECT      : 'SELECT' ;
SELF        : 'SELF' ;
SEPARATE    : 'SEPARATE' ;
SEQUENCE    : 'SEQUENCE' ;
SEQUENTIAL  : 'SEQUENTIAL' ;
SIGN        : 'SIGN' ;
SIGNED      : 'SIGNED' ;     // COBOL-2002 BINARY-xxx SIGNED (USAGE clause, ISO §13.18.60); beats SIGN
UNSIGNED    : 'UNSIGNED' ;   // COBOL-2002 BINARY-xxx UNSIGNED
SIZE        : 'SIZE' ;
SOURCE      : 'SOURCE' ;
SOURCES     : 'SOURCES' ;    // COBOL-2002 SOURCE-clause plural spelling (ISO §13.18.53.2 / §13.18.53.3 SR1); usable as a user word via cobolWord (§8.9 funnel gates ≥2002)
STANDARD_1  : 'STANDARD-1' ;
STANDARD_2  : 'STANDARD-2' ;
STATUS      : 'STATUS' ;
SUPER       : 'SUPER' ;
SYNC        : 'SYNC' ;
SYNCHRONIZED: 'SYNCHRONIZED' ;
TALLYING    : 'TALLYING' ;
TEST        : 'TEST' ;
THAN        : 'THAN' ;
THEN        : 'THEN' ;
THROUGH     : 'THROUGH' ;
THRU        : 'THRU' ;
TIME        : 'TIME' ;
TIMES       : 'TIMES' ;
TO          : 'TO' ;
TOP         : 'TOP' ;
TRAILING    : 'TRAILING' ;
TRUE_       : 'TRUE' ;
TYPE        : 'TYPE' ;
TYPEDEF     : 'TYPEDEF' ;
UNDERLINE_  : 'UNDERLINE' ;
UNIT        : 'UNIT' ;
UNTIL       : 'UNTIL' ;
UP          : 'UP' ;
USAGE       : 'USAGE' ;
USING       : 'USING' ;
// VALIDATE-STATUS / VAL-STATUS (ISO §13.18.62; §13.18.62.3 SR9 makes the two words EQUIVALENT) and VALID (the
// §13.18.63 format-5 content-validation entry) — the declined A.4.14 surface. HYPHENATED, so VALIDATE_STATUS
// must precede IDENTIFIER (an equal-length match is broken by rule order); it also precedes VALIDATE here for
// readability — ANTLR's maximal munch already prefers the longer literal. All three are §8.9-reserved from
// 2002 and USER WORDS at 85, so all three ride cobolWord and the §8.9 funnel makes the ≥2002 rejection.
VALIDATE_STATUS : 'VALIDATE-STATUS' ;
VAL_STATUS      : 'VAL-STATUS' ;
VALID       : 'VALID' ;
VALIDATE    : 'VALIDATE' ;     // VALIDATE (ISO 14.9.50) - optional 4.2.7/A.4.14 + obsolete 4.2.13
VALUE       : 'VALUE' ;
VALUES      : 'VALUES' ;
VARYING     : 'VARYING' ;
WHEN        : 'WHEN' ;
WITH        : 'WITH' ;
// ⛔ ONE TOKEN PER RESERVED WORD — the figurative spellings are DISTINCT §8.9 reserved words (kb/Work PB510).
// ZERO, ZEROS and ZEROES (and each singular/plural pair below) are interchangeable ONLY where the §8.3.3.6.2
// figurative-constant format lists them as alternatives — the parser rules zeroWord / spaceWord /
// highValueWord / lowValueWord / quoteWord in CobolExpressions.g4 are that interchangeability, written once.
// Everywhere else a general format names ONE of them as a keyword (§5.2.2: "They are required in order to
// select the functionality associated with that keyword") and admits no other: BLANK WHEN ZERO (§13.18.8.2),
// the sign condition's ZERO (§8.8.4.7.2), the OPTIONS INITIALIZE clause's BINARY ZEROES / HIGH-VALUES /
// LOW-VALUES / SPACES (§11.9.10.2). Folding the spellings into one token made that distinction unrecoverable
// and every one of those formats accepted every spelling. The only multi-spelling keyword rule left is PIC
// (§13.18.40.3 SR5 "PIC is an abbreviation for PICTURE") — KeywordSpellingDriftTests pins that.
ZERO        : 'ZERO' ;
ZEROS       : 'ZEROS' ;
ZEROES      : 'ZEROES' ;
SPACE       : 'SPACE' ;
SPACES      : 'SPACES' ;
HIGH_VALUE  : 'HIGH-VALUE' ;
HIGH_VALUES : 'HIGH-VALUES' ;
LOW_VALUE   : 'LOW-VALUE' ;
LOW_VALUES  : 'LOW-VALUES' ;
QUOTE_      : 'QUOTE' ;
QUOTES      : 'QUOTES' ;

// ── Numeric literals (must come BEFORE IDENTIFIER) ──
// DECIMALLIT handles DOT-based decimals in the lexer (maximal munch resolves
// DOT-as-decimal vs DOT-as-sentence-terminator). COMMA-based decimals for
// DECIMAL-POINT IS COMMA are handled in the parser via numericLiteralCore.

// Floating-point numeric literal (ISO §8.3.3.3.3): a significand (which SHALL include a decimal point, 1-36 digits)
// joined to an exponent (1-4 digits, optionally signed) by 'E', no intervening spaces — e.g. 1.5E3, 2.5E-2, .5E10.
// MUST precede DECIMALLIT so maximal munch keeps "1.5E3" ONE token, not DECIMALLIT "1.5" + IDENTIFIER "E3" (the
// old parse error). Additive: the no-space <decimal>E<digits> form was previously always a parse error. (D16.)
// The body is a FRAGMENT (the Group-B one-body discipline) because THREE tokens consume it: this one, the
// FN_SIGNED twin, and the SUBSCRIPT-mode forms (kb/Work R17 — the float shape was the one signed-capable
// literal with no twins, so `FUNCTION EXP(-1.5E3)` lexed as TWO arguments and drew a false COBOLNET1504).
fragment FLOAT_BODY : ( [0-9]+ '.' [0-9]* | '.' [0-9]+ ) 'E' [-+]? [0-9]+ ;
FLOATLIT    : FLOAT_BODY ;

// The DECIMAL-POINT IS COMMA twin of the floating-point literal (kb/Work PB98): under DECIMAL-POINT IS COMMA "the
// character written in numeric literals to represent the decimal separator shall be the comma" (ISO §12.3.7.4 GR14 a) —
// the floating-point form's significand included (§8.3.3.3.3: "two fixed-point numeric literals separated by the
// letter 'E'"), so `1,5E+3` is ONE literal. The lexer cannot know the SPECIAL-NAMES clause, so the shape is one token
// in EVERY mode; the binder's ONE normalizer canonicalizes it to dot-decimal and reports COBOLNET0895 when
// DECIMAL-POINT IS COMMA is not in effect. No dot-mode program legally spells `<digits>,<digits>E<exponent>` (the
// comma-decimal fixed form assembles in the parser as INTEGERLIT COMMA INTEGERLIT because ',' alone is a token; the
// float shape needs the E-tail, so it is lexed whole — before this a comma-mode `VALUE 1,5E+3` silently seeded 1 and
// `MOVE 1,5E+3 TO X` was a parse error). Same body discipline as FLOAT_BODY: fragment + FN_SIGNED twin + SUB forms
// (SignedLiteralShapeDriftTests scrapes every FLOAT*_BODY).
fragment FLOAT_COMMA_BODY : ( [0-9]+ ',' [0-9]* | ',' [0-9]+ ) 'E' [-+]? [0-9]+ ;
COMMA_FLOATLIT : FLOAT_COMMA_BODY ;

// ── Shared literal fragment bodies (rearchitecture PHASE 04, Group B) ──
// One definition per literal tokenization shape, referenced by BOTH the DEFAULT-mode literal tokens and their
// SUBSCRIPT-mode SUB_* twins (fragments are mode-independent). The two modes previously re-declared each body
// char-for-char; now a future string-escape / national-literal / data-name fix is applied ONCE and cannot diverge
// between modes (DESIGN-frontend-grammar §3.3b). Bodies are byte-identical to the retired inline forms.
fragment STR_BODY  : '"' (~["\n] | '""')* '"' | '\'' (~['\n] | '\'\'')* '\'' ;   // STRINGLIT / SUB_STRINGLIT
// ⛔ FORMAT 2 IS THE SAME LITERAL KIND, SO IT IS THE SAME TOKEN (fix-queue R03). §8.3.3.5.2 and §8.3.3.4.2 each
// print TWO general formats — `N"…"` / `NX"…"` and `B"…"` / `BX"…"` — and their ALL-FORMATS general rules put
// both formats in ONE class and category (§8.3.3.5.4 GR2 national, §8.3.3.4.4 GR2 boolean). Folding Format 2
// into the existing token is therefore what the standard says, and it is also what keeps the change tractable:
// 32 call sites already route NATLIT→national and BOOLLIT→boolean, and every one stays correct untouched.
// ⚠ THE ALTERNATIVE IS A MISTAKE ALREADY IN THIS FILE. `HEXLIT` is a token of its own even though §8.3.3.2 makes
// a hexadecimal literal "one FORM of an alphanumeric literal, not a separate kind of thing" (CobolLiteral's own
// words) — and that split is why its decoding had to be added at FOUR separate call sites, the fourth being a
// silent VALUE-clause miscompile. A second token here would have bought the same bill twice over.
// ⛔ A PREFIXED LITERAL'S BODY IS ANY QUOTED CONTENT — THE LEXER DELIMITS, IT DOES NOT JUDGE THE CONTENT (kb/Work
// PB1441, PB1394). §8.3.5 5) makes `X"`, `N"`, `B"` and the three-character `NX"` / `BX"` OPENING DELIMITERS, so
// once one is written the character-string can only be a literal of that format, well formed or not. The bodies
// used to spell each format's REPERTOIRE — `[0-9a-f]*` for X, NX and BX, `[01]*` for B — and a body outside it
// failed the whole alternative, so maximal munch fell back to IDENTIFIER `X` + a Format 1 STRINGLIT: beside a
// data item named X, `DISPLAY X"GG"` compiled and printed the item followed by GG, and `MAX(NX"00G1")` answered
// the item's value. The repertoire rules — §8.3.3.2.3 SR5 and §8.3.3.5.3 SR4 (hexadecimal digits), §8.3.3.4.3
// SR3 (a hexadecimal digit) and SR2 (a boolean character, '0' or '1') — are now asked of the whole token by
// LiteralScreenPass (CobolLiteral.RepertoireViolation → COBOLNET2630), beside the §8.3.3 grouping and length
// rules it already asked: the token must MATCH so something is left to diagnose (the R03 doctrine this block
// began with). Every prefixed body is therefore STR_BODY, doubled delimiters included, so no content can split one.
// ⚠ ZERO LENGTH IS LEGAL IN EVERY FORMAT, and STR_BODY admits it: §8.3.3.5.3 NOTE 2 "Hexadecimal-national literals
// can be of zero length", §8.3.3.4.3 NOTE "Hexadecimal-boolean literals can be of zero length", §8.3.3.4.4 GR4 for
// Format 1 boolean (kb/Work PB65), §8.3.3.2.3 NOTE 2 for X (PB59).
// Hexadecimal-alphanumeric X"…" / X'…' (§8.3.3.2.2 Format 2 — the printed page shows BOTH delimiters with
// the hex sequence OPTIONAL, verified at 300 dpi 2026-08-09): one body, both delimiters, zero length legal —
// the Group-B discipline.
// ⛔ SUB_HEXLIT IS ITS SUBSCRIPT-MODE TWIN (kb/Work PB1393). The keyword-omitted intrinsic capture re-lexes its
// argument text in DEFAULT mode, so the VALUE never needed the twin (PB59 measured that); the literal SYNTAX
// SCREEN does. LiteralScreenPass asks the §8.3.3 length and grouping rules of every literal TOKEN of the unit's
// one tree, and the re-parsed fragment is not part of that tree — so a captured literal is screened as its
// SUBSCRIPT-mode token or not at all. Without the twin, `LENGTH(X"414")` lexed here as SUB_IDENTIFIER `X` +
// SUB_STRINGLIT `"414"` and the malformed hexadecimal literal decoded to "" in silence. LiteralScreenDriftTests
// requires every token defined over one of these literal body fragments to be in the screen's token set.
fragment HEX_BODY  : [x] STR_BODY ;                                                  // §8.3.3.2.2 Format 2
fragment NAT_BODY  : 'N' STR_BODY                                                    // NATLIT / SUB_NATLIT F1
                   | 'NX' STR_BODY ;                                                 // §8.3.3.5.2 Format 2
fragment BOOL_BODY : 'B' STR_BODY                                                    // BOOLLIT / SUB_BOOLLIT F1
                   | 'BX' STR_BODY ;                                                 // §8.3.3.4.2 Format 2
fragment INT_BODY  : [0-9]+ ;                                                        // INTEGERLIT / SUB_INTEGERLIT
fragment DEC_BODY  : [0-9]+ '.' [0-9]+ | '.' [0-9]+ ;                                // DECIMALLIT / SUB_DECIMALLIT
// ⛔ THE UNDERSCORE IS A COBOL WORD CHARACTER AND HAS BEEN SINCE COBOL-2002 (fix-queue R02). §8.3.2.1: "Each
// character of a COBOL word … shall be selected from the set of basic letters, basic digits, extended letters,
// and the basic special characters HYPHEN AND UNDERSCORE. The hyphen or underscore shall not appear as the first
// or last character in such words." Without it `01 MY_NAME PIC X(6).` — legal at three of the four editions this
// compiler targets — did not parse at any of them, dying as `no viable alternative at input 'MY_'`.
// ⚠ SUPERSET-PARSE / BIND-NARROW, the repo's standing doctrine: the underscore is admitted at EVERY edition here
// and the COBOL-85 rejection is a BIND-time gate (Constructs.UserWordUnderscore2002 → COBOLNET0900, raised beside
// the §8.3.2.1 word-LENGTH check in VersionConformancePass.VisitCobolWord — same clause, same visitor). A lexer
// that refused it below 2002 would produce `no viable alternative`, which is an unexplained rejection rather
// than a named edition diagnostic.
// ⛔ AND "NOR LAST" IS NOW ENFORCED FOR BOTH SEPARATORS, WHICH IT WAS NOT. The same sentence bars a leading OR
// trailing hyphen or underscore. NOT-FIRST was already structural (every alternative opens with a letter or a
// digit) and NOT-LAST held only in the ALPHA-start alternative, whose tail is `[a-z0-9]`; the two DIGIT-start
// alternatives ended in `*` over the separator class, so `1A-` and `42-X-` were ACCEPTED — a pre-existing hole
// for the hyphen that the underscore would have silently inherited. Factoring the tail into NAME_TAIL, which
// cannot end on a separator, closes it for both characters in all three alternatives at once.
fragment NAME_TAIL : [a-z0-9_-]* [a-z0-9] ;   // a word tail that cannot END on a separator (§8.3.2.1)
fragment NAME_BODY                                                                   // IDENTIFIER / SUB_IDENTIFIER
    : [0-9]+ [-_] [a-z0-9] NAME_TAIL?   // digit-start with separator: 42-DATANAMES, 42_DATANAMES
    | [0-9]+ [a-z] NAME_TAIL?           // digit-start with letter: 11A, 25COUNT, 80PARTS
    | [a-z] NAME_TAIL?                  // alpha-start: A, WRK-DS-01V00, MY_NAME, MIXED_A-B
    ;

// ── Function-argument signed literals (P7 Step 12) ──
// Inside a FUNCTION argument region (and only there — the predicate), a sign that follows a separator and
// touches its digits is the leftmost CHARACTER of a numeric literal (ISO §8.3.3.3.2 r2); a binary operator is
// space-surrounded (§8.7.1). These twins re-type to the SUBSCRIPT-mode SIGNED_* token types so the parser sees
// one vocabulary: MAX(A -4) lexes A SIGNED_INTEGERLIT(-4) = two arguments; MAX(A - 4) lexes A MINUS 4 = one
// subtraction. Outside argument regions OnSignedLiteral cuts the token back to its sign, so [+-] lexes PLUS/MINUS
// and the digits their own literal, exactly as a lone sign always has. ⛔ The context test is the ACTION, never a
// predicate on the rule (kb/Work PB1715 — see the members): a predicate here re-simulated every `+`/`-` token.
// The decimal twin MUST precede the integer twin (the SUBSCRIPT-mode ordering note: else -15.6 orphans ".6"),
// and the FLOAT twin precedes them both (kb/Work R17): "-1.5E3" must stay ONE token — without this twin the
// signed-decimal rule won maximal munch at "-1.5" and orphaned "E3" as an IDENTIFIER, so the parser reported
// two arguments (§8.3.3.3.3 r2 makes the sign part of the literal; §15.3 type 10 admits the literal). The
// sign travels in the TOKEN TEXT and the type is plain FLOATLIT — one vocabulary, zero parser changes, and
// the emitter's E-form arm renders the signed text as a C# double literal directly.
FN_SIGNED_FLOATLIT   : [+-] FLOAT_BODY { OnSignedLiteral(FLOATLIT); } ;
FN_SIGNED_COMMA_FLOATLIT : [+-] FLOAT_COMMA_BODY { OnSignedLiteral(COMMA_FLOATLIT); } ;   // kb/Work PB98
FN_SIGNED_DECIMALLIT : [+-] DEC_BODY { OnSignedLiteral(SIGNED_DECIMALLIT); } ;
FN_SIGNED_INTEGERLIT : [+-] INT_BODY { OnSignedLiteral(SIGNED_INTEGERLIT); } ;

DECIMALLIT  : DEC_BODY ;

// ── IDENTIFIER (must come BEFORE INTEGERLIT) ──
// COBOL-85 user-defined words: 1-30 chars from {A-Z, a-z, 0-9, hyphen},
// must contain at least one letter, no leading/trailing hyphen.
// Digit-start forms: 42-DATANAMES (hyphen), 11A/25COUNT/80PARTS (letter).
// Pure digits remain INTEGERLIT (level numbers, paragraph numbers, etc.).
// The action is the ONE place a word that is a keyword only in a primed region is retyped: DEFINED, the §7.3.8.4.4
// defined-condition keyword (`compilation-variable-name IS [NOT] DEFINED`), is NOT a reserved word in the source
// language — it is a token ONLY inside a primed compiler-directive-expression fragment (the PrimeFunctionArgs
// precedent), and an ordinary IDENTIFIER (a legal user data-name) everywhere else. ⛔ An ACTION, never a predicate
// (kb/Work PB1715): the predicate the DEFINED rule carried kept every D-initial word of every compile (DATA,
// DIVISION, DISPLAY …) off the cached DFA.
IDENTIFIER  : NAME_BODY { if (_primeDirectiveExpr) RetypeDirectiveKeyword(); } ;

INTEGERLIT  : INT_BODY ;

// ── String literals ──

STRINGLIT   : STR_BODY ;
// National literal N"…" / N'…' (ISO §8.3.3.5, COBOL-2002). The leading N is part of the token so
// ANTLR's maximal-munch prefers it over IDENTIFIER (a bare N) and over a plain STRINGLIT; an
// identifier such as NAME is unaffected (it has no opening quote). NX"…" (Format 2, hexadecimal-national) is
// the same token (fix-queue R03).
NATLIT      : NAT_BODY ;
// ⚠ ZERO LENGTH IS LEGAL IN BOTH DELIMITERS — §8.3.3.2.3 NOTE 2, "Hexadecimal-alphanumeric literals can be
// of zero length", and §8.3.3.2.2 Format 2 brackets the hex sequence under BOTH delimiter spellings (page
// rendered and verified — PB59). The quotation arm's `+` was fixed by R03's sweep; the APOSTROPHE arm kept
// its `+` until PB59, so X'' split into IDENTIFIER + a zero-length Format-1 literal — the same silent-split
// hole, one delimiter over. The GROUPING rule (§8.3.3.2.3 r6, pairs of digits) and the REPERTOIRE rule (SR5,
// hexadecimal digits) are asked by LiteralScreenPass, not here: a lexer that refused an odd count or a non-hex
// digit would put the literal back in the silent-split hole this whole item exists to close (see HEX_BODY).
HEXLIT      : HEX_BODY ;
// Boolean literal B"0101" / B'0101' (ISO §8.3.3.4, COBOL-2002). The leading B is part of the token so
// maximal-munch prefers it over IDENTIFIER (a bare B) and over a plain STRINGLIT. BX"…" (Format 2,
// hexadecimal-boolean) is the same token; neither body is judged here (§8.3.3.4.3 SR2/SR3 — see HEX_BODY).
BOOLLIT     : BOOL_BODY ;

// ── Operators (multi-char before single-char) ──

POWER       : '**' ;
LTEQUAL     : '<=' ;
GTEQUAL     : '>=' ;
NOTEQUAL    : '<>' ;

// A period is a separator only when a space follows it (§8.3.5 rule 3). The token is every period the literal and
// PICTURE rules did not take; SeparatorRule reports one with no space after it (COBOLNET2632, kb/Work PB1394).
DOT         : '.' ;
// P7 Step 12: inside a FUNCTION-argument region the ','/';'-plus-space separator (§8.3.5 rules 1/2) is a REAL
// token — the argument boundary must survive to the parser: a '(' right after it opens a PARENTHESIZED
// ARGUMENT, and the LPAREN whitelist action sees the separator (not the previous argument's data-name) as the
// previous token, so `MAX(A * B, (C + 1) / 2, …)` (IF119A/IF123A) does not mis-lex the group as a subscript
// of B. Outside argument regions the ACTION skips it — ISO §8.3.5 2): "The COBOL characters comma and semicolon,
// immediately followed by a space, are separators that may be used anywhere the separator space is used" — so it
// is skipped exactly as the space is. ONE rule matches the separator everywhere and its action says which it is;
// ⛔ never a predicate choosing between two rules (kb/Work PB1715): the predicate this rule carried kept every
// `, ` and `; ` of every compile off the cached DFA, and the unpredicated twin it needed is gone with it.
FNARG_SEPARATOR : [,;] [ \n]+ { if (!InFunctionArgs()) Skip(); } ;
// A comma NOT followed by whitespace is preserved for DECIMAL-POINT IS COMMA, where it is a numeric literal's
// decimal point; anywhere else it is no separator (§8.3.5 rule 2) and SeparatorRule reports it (COBOLNET2631).
COMMA       : ',' ;
LPAREN      : '(' { OnDefaultLParen(); } ;
RPAREN      : ')' { OnDefaultRParen(); } ;
LT          : '<' ;
GT          : '>' ;
EQUALS      : '=' ;
PLUS        : '+' ;
MINUS       : '-' ;
STAR        : '*' ;
SLASH       : '/' ;
// ⛔ THE INVOCATION OPERATOR IS A TOKEN, AND IT HAD NONE (kb/Work PB428). ISO §8.7.4: "The invocation operator
// is the two contiguous COBOL characters '::'". §8.4.3.4.2 prints it as the required punctuation of the
// inline-method-invocation identifier format, and §8.3.5 rule 7 ("The COBOL character colon, EXCEPT AS PART
// OF THE INVOCATION OPERATOR, is a separator") makes the two characters one lexical unit rather than two
// colons. With no rule here the whole of §8.4.3.1.2 Format 4 was unwritable in EVERY identifier position: the
// lexer stopped at the first ':' and the parser reported punctuation. MUST precede COLON in source order for
// readability — ANTLR's longest-match already decides it, and CobolLexerModeDriftTests pins the token dump.
// ⚠ §8.7.4's "shall be immediately preceded and followed by a separator space" is NOT enforced at the token
// level, exactly as §8.7.3's identical sentence about '&' is not (see AMPERSAND below): the parser sees the
// skipped-WS stream, and '::' has no other lexical role, so `O::"M"` and `O :: "M"` are the same token run.
COLONCOLON  : '::' ;
COLON       : ':' ;
// The concatenation operator (ISO §8.7.3): the COBOL character '&', joining literals into one literal
// (§8.8.3). The §8.7.3 separator-space requirement ("immediately preceded and followed by a separator
// space") is not enforced at the token level — the parser sees the skipped-WS stream, the same leniency
// every other separator-adjacent operator (e.g. '::' §8.7.4) already has. '&' has no other lexical role,
// so the token is unambiguous with or without the spaces.
AMPERSAND   : '&' ;
// A ';' with no space after it (the §8.3.5 separator `; ` is FNARG_SEPARATOR above). It is no separator (§8.3.5 rule
// 2), and SeparatorRule reports it (COBOLNET2631) — so it rides the HIDDEN channel, where that post-lex rule sees it and
// the parser does not, rather than being skipped out of existence (kb/Work PB1394).
SEMICOLON   : ';' -> channel(HIDDEN) ;

// ── Catch-all for unrecognized characters ──

ANY_CHAR    : . ;

// ==========================================
// PICMODE — captures PIC/PICTURE string as one token
// ==========================================
// After PIC/PICTURE, optionally skip IS, then capture the entire
// PIC string (e.g., X(120), S9(18), $$$,$$9.99CR) as one token.
//
// Key insight: PIC strings never contain spaces. A period within
// a PIC string (like 9.99) is always followed by another PIC char,
// while a sentence-ending period is followed by whitespace/EOF.

mode PICMODE;

PIC_IS      : 'IS' -> skip ;              // optional IS keyword
PIC_WS      : [ \n]+ -> skip ;       // skip whitespace
// A comment line between PIC and its character-string (kb/Work PB1493). Source comments are removed by the
// reference-format stage (ISO §6.5 2) / 3)) before the lexer runs, but the fixed-form DEBUGGING-line carrier
// (ReferenceFormatProcessor.DebugLineCarrier) still reaches it as a *> line — and *> never begins a picture
// character-string (> is no PICTURE symbol). Longest match: it always reaches at least as far as PIC_STRING would.
PIC_COMMENT : '*>' ~[\n]* -> skip ;
PIC_STRING  : ( ~[ \n.] | '.' ~[ \n] )+
    {
        // Handle PIC "999999999999.." — greedy match consumed sentence-ending period.
        // If the PIC string ends with '.' and the char that caused the match was also '.',
        // trim the trailing period and back up so it becomes a DOT token (CutTokenTo: the input AND the column).
        var t = Text;
        if (t.Length > 1 && t[t.Length - 1] == '.')
        {
            CutTokenTo(t.Length - 1);
        }
        // A trailing ',' or ';' IMMEDIATELY FOLLOWED BY A SPACE is the CLAUSE SEPARATOR over-captured by the
        // greedy match (ISO §8.3.5 rule 2) — `77 X PIC 99, VALUE 3.` must lex the picture as "99" (VCR Table 7
        // row 7.14; the W2 adversarial-review finding; the ';' twin was NC203A/245A/251A/252A). The
        // following-character GUARD is load-bearing: a LEGAL trailing ',' (§13.18.40.3 SR7 — PICTURE as the
        // last clause) is followed by the separator PERIOD, not a space (NC125A's `PIC 9,9,…,9,.` — the token
        // ends at the ',' because '.'+newline cannot extend the match, so LA(1) is '.' and the ',' is a
        // PICTURE SYMBOL to keep). The seek-back re-lexes a trimmed separator in DEFAULT mode, where
        // FNARG_SEPARATOR's action skips it (a PICTURE clause is never inside a function-argument region).
        else if (t.Length > 1 && (t[t.Length - 1] == ',' || t[t.Length - 1] == ';'))
        {
            int la = InputStream.LA(1);
            if (la == ' ' || la == '\n' || la == Antlr4.Runtime.IntStreamConstants.EOF)
            {
                CutTokenTo(t.Length - 1);
            }
        }
    } -> popMode ;
    // Matches: any non-whitespace-non-period char,
    //      OR: a period followed by a non-whitespace char (embedded decimal)
    // Post-action: if PIC string ends with '.', the greedy match consumed a
    //   sentence-ending period — back up one char so it tokenizes as DOT;
    //   likewise a trailing ','/';' clause separator (§8.3.5 r2) backs up and re-lexes as a skip token.

// ==========================================
// COMMENT_MODE — *> to end of line
// ==========================================

// ==========================================
// SUBSCRIPT MODE — COBOL-85 §5.3 subscript lexing
// ==========================================
// Entered when '(' follows an IDENTIFIER. Whitespace is preserved (not skipped)
// and sign adjacency is distinguished: +1 (SIGNED_INTEGERLIT) vs + 1 (SUB_PLUS SUB_WS SUB_INTEGERLIT).

mode SUBSCRIPT;

SUB_WS              : [ \n]+ ;
// The same comment carrier inside a subscript or reference-modifier (see PIC_COMMENT): *> is no operator there.
SUB_COMMENT         : '*>' ~[\n]* -> skip ;

// Keywords must precede SUB_IDENTIFIER (same length → first rule wins)
SUB_OF              : 'OF' ;
SUB_IN              : 'IN' ;
SUB_ALL             : 'ALL' ;

// The FLOAT forms first (kb/Work R17 — this mode had NO float shape at all, so "1.5E3" in a captured region
// lexed SUB_DECIMALLIT "1.5" + SUB_IDENTIFIER "E3" and the signed form orphaned identically). Re-typed to
// the ONE FLOATLIT vocabulary, sign in the token text — same convention as the DEFAULT-mode twin.
SUB_SIGNED_FLOATLIT : [+-] FLOAT_BODY -> type(FLOATLIT) ;
SUB_FLOATLIT        : FLOAT_BODY -> type(FLOATLIT) ;
SUB_SIGNED_COMMA_FLOATLIT : [+-] FLOAT_COMMA_BODY -> type(COMMA_FLOATLIT) ;   // the DECIMAL-POINT IS COMMA twin (kb/Work PB98)
SUB_COMMA_FLOATLIT        : FLOAT_COMMA_BODY -> type(COMMA_FLOATLIT) ;

// Sign immediately adjacent to a decimal literal: -15.6, +0.2, -.5 (signed decimal
// argument to an intrinsic function, ISO §15). MUST precede SIGNED_INTEGERLIT so the
// fractional part is not orphaned: ANTLR longest-match makes "-15.6" one SIGNED_DECIMALLIT
// rather than SIGNED_INTEGERLIT "-15" + SUB_DECIMALLIT ".6" (which silently dropped the .6).
SIGNED_DECIMALLIT   : [+-] [0-9]+ '.' [0-9]+ | [+-] '.' [0-9]+ ;

// Sign immediately followed by digits: +1, -10 (signed literal subscript)
SIGNED_INTEGERLIT   : [+-] [0-9]+ ;

// Numeric literals
SUB_INTEGERLIT      : INT_BODY ;
SUB_DECIMALLIT      : DEC_BODY ;

// Alphanumeric literal — needed for string-valued intrinsic-function arguments,
// e.g. FUNCTION LOWER-CASE("ABC"), FUNCTION NUMVAL("12.3"). Mirrors STRINGLIT.
SUB_STRINGLIT       : STR_BODY ;

// National/boolean literal arguments (N"…"/B"…", ISO §8.3.3.5/§8.3.3.4) — mirror NATLIT/BOOLLIT. MUST
// precede SUB_IDENTIFIER (and win by longest match anyway) so the prefix letter is never orphaned as a
// one-character data-name with the quoted body becoming a separate SUB_STRINGLIT — that shape silently
// misbound FUNCTION LENGTH(N"AB") before these tokens existed (Phase 4a, the proper-token rule).
SUB_NATLIT          : NAT_BODY ;
SUB_BOOLLIT         : BOOL_BODY ;
// Hexadecimal-alphanumeric X"…" (§8.3.3.2 Format 2) — the twin of HEXLIT, for the literal syntax screen (see
// HEX_BODY above). Longest match makes it beat SUB_IDENTIFIER's one-character `X`.
SUB_HEXLIT          : HEX_BODY ;

// Data-name / index-name (must follow keywords to avoid capturing OF/IN/ALL)
SUB_IDENTIFIER      : NAME_BODY ;

// Operators and punctuation. Arithmetic operators (*, /, **) are needed because
// intrinsic-function arguments — captured in this mode to preserve comma/space
// separators — may be full arithmetic expressions (ISO §15), e.g. MEAN(9 * A, B / 2).
// SUB_POWER must precede SUB_STAR so '**' is one token, not two.
SUB_PLUS            : '+' ;
SUB_MINUS           : '-' ;
SUB_POWER           : '**' ;
SUB_STAR            : '*' ;
SUB_SLASH           : '/' ;
SUB_COMMA           : ',' ;
SUB_SEMICOLON       : ';' ;  // §8.3.5: semicolon is interchangeable with comma
SUB_COLON           : ':' ;
SUB_LPAREN          : '(' -> pushMode(SUBSCRIPT) ;
SUB_RPAREN          : ')' -> popMode ;
SUB_ANY             : . ;

// ==========================================
// COMMENT_MODE — *> to end of line
// ==========================================

mode COMMENT_MODE;

COMMENT_TEXT : ~[\n]+ -> skip ;
COMMENT_END  : [\n]   -> popMode, skip ;
