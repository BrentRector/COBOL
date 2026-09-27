// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Literals, arithmetic expressions, conditions, and comparisons.
// Imported by CobolParserCore.g4. tokenVocab enables VSCode ANTLR4 extension token resolution.

parser grammar CobolExpressions;

options {
    tokenVocab = CobolLexer;
}

// ⛔ NOT A CLAUSE, AND NOT A VENDOR HOOK — the ONE error production of the whole grammar (kb/Work PB829; the
// §13.16.2 half landed first as kb/Work PB487).
//
// `IDENTIFIER (IDENTIFIER|literal)*` matches ANY run of words, so wherever it sits in an alternative list it is a
// TOTAL SINK for everything the named alternatives missed. It used to be reached from SIX sites spanning EIGHT
// closed general formats — the data description entry (§13.16.2), the file description entry (§13.4.5.2), the
// sort-merge file description entry (§13.4.6.2), the file control entry (§12.4.5.1), the I-O-CONTROL paragraph
// (§12.4.6.2), the SPECIAL-NAMES paragraph (§12.3.7.2), the configuration section (§12.3.2) and the
// identification division (§11.2.1) — under the name of a "vendor/extension hook". It is not one: a vendor
// extension is admitted only under the dialect that owns it, never by a catch-all, and this compiler declares no
// vendor dialect. What it actually did was SWALLOW a word run the format does not define, at every edition and
// every strictness, with no diagnostic anywhere — §4.2.2's warning obligation cannot be met for a construct the
// compiler never represents.
//
// Every one of those sites now spells `unrecognizedClause`, and `genericClause` is referenced from NOWHERE ELSE
// (`ClosedFormatDriftTests` derives that from these .g4 files and fails the build otherwise). The alternative is
// still RECOGNIZED rather than deleted, because recognizing it keeps recovery local — the rest of the entry, the
// paragraph and the division still parse, so the user gets ONE named error instead of a cascade of "no viable
// alternative". `Validation/ClosedFormatPass.cs` refuses every instance BY NAME, naming the format and its §.
// It must be the LAST alternative at every site: ANTLR takes the first matching alternative.
unrecognizedClause
    : genericClause
    ;

// The word-run matcher `unrecognizedClause` is built on. Kept as its own rule because the diagnostic quotes the
// FIRST word of the run (`genericClause.IDENTIFIER(0)`) and the whole run as written.
genericClause
    : IDENTIFIER (IDENTIFIER | literal)*
    ;

// =========================
// Value operands and ranges
// =========================

// A "value" in comparisons/EVALUATE: full arithmetic expression or literal.
valueOperand
    : arithmeticExpression
    | nonNumericLiteral
    ;

// A "value" in VALUE clauses: single value (no binary arithmetic).
// Uses unaryExpression to prevent "5 -9999" from being parsed as subtraction.
valueClauseOperand
    : unaryExpression
    | nonNumericLiteral
    ;

// Range form for EVALUATE WHEN (full arithmetic) — §14.9.13.2's range-expression, INCLUDING the trailing
// `[ IN alphabet-name-1 ]` phrase, which is the only way a program can name the collating sequence that orders an
// alphanumeric / national range (§14.7.8 rule 2: "When the IN alphabet-name phrase is specified, the collating
// sequence used for range evaluation is the collating sequence defined by that alphabet"; with no phrase the
// ordering is "defined by the implementor"). MEASURED off the printed page (PDF p649 / folio 619): the bracket sits
// AFTER the right-hand operand brace, `IN` is NOT underlined (an optional word, §5.2.3) and alphabet-name-1 is a
// required operand of the bracketed phrase. The phrase was simply absent, so §14.9.13.3 SR3 — whose entire content
// is a constraint ON it — had no code site (kb/Work PB398).
// ⚠ `IN` IS ALSO THE QUALIFICATION CONNECTIVE (`qualification : (OF | IN) cobolWord`), so over an identifier-4 the
// two readings — `identifier-4 IN group` and `identifier-4` + the alphabet phrase — are BOTH complete parses and
// ANTLR's greedy `dataReferenceSuffix*` loop takes the qualifier. That is an ambiguity only the resolved SYMBOL can
// settle (§8.3.2.2: "a given user-defined word may be used as only one type of user-defined word", so an
// alphabet-name is never a data-name, record-name or file-name qualifier), and it is NOT settled here: flipping the
// grammar's preference would make the opposite legal reading — a genuinely qualified identifier-4 — unreachable
// instead. EvaluateBinder.BindRangeHigh settles it by symbol (kb/Work PB843): a LAST `IN word` suffix that names a
// declared alphabet is the phrase, and the reference is bound without it (ReferenceResolver.WithoutTrailingSuffix).
// The LITERAL-operand spelling the printed figure shows never meets the ambiguity, since a literal takes no
// qualifier.
// ⛔ AND `IN` IS OPTIONAL (kb/Work PB983): the printed phrase underlines neither `IN` nor anything else in the
// bracket (PDF p649 / folio 619 — "[ IN alphabet-name-1 ]" is plain type), so §5.2.3 makes `WHEN "M" THRU "A" AL`
// conforming. With IN omitted the word is unambiguous HERE: after a complete right-hand operand only ALSO, WHEN or
// the imperative statement's verb (all reserved) may follow a selection object, so a bare user-defined word can be
// nothing but alphabet-name-1 — whose TYPE the binder still checks (EvaluateBinder.RangeAlphabet). The VALUE
// clause's identical phrase cannot be decided by position (see CobolData.g4 valueClause); its binder peels it by
// symbol instead, and TrailingInPhraseDriftTests pins both formats together.
valueRange
    : valueOperand (THRU | THROUGH) valueOperand (IN? cobolWord)?
    ;

// Range form for VALUE clauses (no binary arithmetic).
valueClauseRange
    : valueClauseOperand (THRU | THROUGH) valueClauseOperand
    ;

// =========================
// Conditions (boolean)
// =========================

booleanLiteral
    : TRUE_
    | FALSE_
    ;

// signCondition has been merged into comparisonExpression to eliminate
// ANTLR prediction ambiguity — both rules started with valueOperand,
// causing exponential DFA growth on files with many figurative-constant comparisons.

condition
    : logicalOrExpression
    ;

logicalOrExpression
    : logicalXorExpression ( OR ( logicalXorExpression | abbreviatedAndChain ) )*
    ;

// COBOL-2023 logical exclusive-or (ISO §8.8.4.9; precedence NOT > AND > XOR > OR). XOR and EXCLUSIVE-OR are
// equivalent. Sits between OR and AND so `a OR b XOR c` parses as `a OR (b XOR c)`. The OPERATOR is a 2023
// addition (Annex E.2 item 25 reserves both words; VCR rows 32/41 — the W3 regating of the former "2002"
// mislabel): gated {is2023()}?; below 2023 both words are USER-DEFINED words (cobolWord admits the tokens;
// the §8.9 funnel + table enforce the 2023 reservation as 0901 in provable positions).
logicalXorExpression
    : logicalAndExpression ( ( XOR | EXCLUSIVE_OR ) logicalAndExpression )*   // XOR/EXCLUSIVE-OR: COBOL-2023; parses at all editions (superset — a bare user-word XOR is never valid in this connective slot), gated at BIND when the operator is genuinely present (BindCondition XOR arm → Check(LogicalXorOperator2023)) — residue migration #1, DESIGN-version-conformance-pipeline.md
    ;

logicalAndExpression
    : unaryLogicalExpression ( AND ( abbreviatedRelation | unaryLogicalExpression ) )*
    ;

// Abbreviated AND chain: one or more abbreviated relations connected by AND.
// Used after OR when the abbreviated form includes AND chaining:
//   IF A = B OR = C AND = D   → OR (= C AND = D)
abbreviatedAndChain
    : abbreviatedRelation ( AND abbreviatedRelation )*
    ;

// Abbreviated combined relation condition (ISO §8.8.4.12):
// After AND/OR, the left operand (and optionally the operator) can be
// elided from the previous comparison.
//   IF A > B OR < C          →  comparisonOperator comparisonOperand
//   IF A > B AND NOT < C     →  comparisonOperator comparisonOperand
//     (NOT < is already a comparisonOperator alternative)
// Bare operands (IF A = B OR C) are already handled by the full
// logicalAndExpression/unaryLogicalExpression path.
// NOT + bare operand (IF A = B AND NOT C) is handled by unaryLogicalExpression.
abbreviatedRelation
    : comparisonOperator comparisonOperand
    ;

// ── §14.9.13.3 SR5 — partial-expression-1, the EVALUATE selection object whose LEFTMOST portion is elided.
// "A selection object is a partial-expression if the leftmost portion of the selection object is a relational
// operator, a class condition without the identifier, a sign condition without the identifier, or a sign condition
// without the arithmetic expression." SR8 then says what it MEANS: the object "is treated as though it were
// specified as condition-2, where condition-2 is the conditional expression that results from preceding
// partial-expression-1 by the selection subject".
// ⛔ IT IS A CONDITION WITH ITS LEFTMOST OPERAND MISSING, NOT A NEW CONDITION LANGUAGE — SR7 d) says so outright:
// "Partial-expression-1 shall be a sequence of COBOL words such that, were it preceded by the corresponding
// selection subject, a conditional expression would result". `WHEN > 5 AND < 10` and `WHEN NUMERIC OR = 0` are
// therefore conforming source. So the spine below MIRRORS the condition tiers and delegates every TAIL to the very
// same rules (abbreviatedRelation / unaryLogicalExpression / logicalAndExpression / logicalXorExpression /
// abbreviatedAndChain): only the LEADING element differs, which is the whole of SR5. Because each tier is an
// iterative loop whose leftmost element is the tier below, this spine yields the IDENTICAL grouping
// (OR ( XOR ( AND … ) ) ) that `condition` yields — and PartialExpressionSpineDriftTests re-derives that from the
// two rules' own text so the mirror cannot rot.
// The leading element is deliberately NOT folded into `comparisonExpression`: that rule is shared by every IF /
// PERFORM UNTIL / SEARCH in the corpus and the DEVLOG-621 regression (a booleanExpression alternative there broke
// subscripted comparisons at 2002+) is the recorded cost of widening it. Partial expressions are EVALUATE's rule,
// so they get EVALUATE's own entry, reached only from evaluateWhenItem.
// A user-defined class-name / alphabet-name written BARE (`WHEN MY-CLASS`) is indistinguishable from identifier-2
// here — `className`'s cobolWord alternative and `valueOperand` both match one word — so evaluateWhenItem keeps
// valueOperand FIRST and EvaluateBinder resolves that spelling by SYMBOL (the same doctrine that makes a bare
// level-88 object condition-2): ConditionBinder.BareClassWord names it, the classifier makes it Table 15's
// partial-expression row, and the SR8 rewrite binds it. The same word LEADING a longer object (`WHEN MY-CLASS AND
// WS-F = "Y"`) is claimed by evaluateWhenItem's `condition` alternative and is re-read the same way
// (ConditionBinder.LeadingBareClassWord; kb/Work PB843). The `IS`-led spelling reaches this rule unambiguously.
partialExpression
    : partialXorExpression ( OR ( logicalXorExpression | abbreviatedAndChain ) )*
    ;

partialXorExpression
    : partialAndExpression ( ( XOR | EXCLUSIVE_OR ) logicalAndExpression )*
    ;

partialAndExpression
    : partialComparison ( AND ( abbreviatedRelation | unaryLogicalExpression ) )*
    ;

// SR5's four shapes, in `comparisonExpression`'s own order and spelling with the leading comparisonOperand removed:
// a relational operator (which IS abbreviatedRelation, §8.8.4.12's already-elided relation), a class condition
// without the identifier, and a sign condition without its identifier / arithmetic expression (one alternative —
// §8.8.4.7.2's two operand forms occupy the same position).
partialComparison
    : IS? NOT? className                                       // class condition without the identifier
    | IS? NOT? (POSITIVE | NEGATIVE | ZERO)                    // sign condition without its operand (ZERO the keyword, §8.8.4.7.2 — not ZEROS/ZEROES, PB510)
    | abbreviatedRelation                                      // leftmost portion is a relational operator
    ;

unaryLogicalExpression
    : NOT primaryCondition
    | primaryCondition
    ;

primaryCondition
    // COBOL-2002 boolean forms (ISO §8.8.4.2.2 relation / §8.8.4.3 simple condition) — gated by the
    // boolExprAhead() predicate so it fires ONLY when a B-operator is actually present in this condition;
    // a normal comparison returns false and falls to comparisonExpression UNCHANGED (the shared rule is
    // untouched — the DEVLOG-621 regression lesson). booleanExpression's leaf is valueOperand, so the binder
    // unwraps a B-op-free operand back to a normal operand (BindPrimaryBoolean).
    : {boolExprAhead()}? booleanExpression ( comparisonOperator booleanExpression )?
    | comparisonExpression
    | booleanLiteral
    | LPAREN condition RPAREN
    ;

// =========================
// Relational
// =========================

// ⛔ THE ADDRESS-IDENTIFIER IS A RELATION OPERAND (kb/Work PB1021). §8.8.4.2.2 Format 3
// (message-tag-object-or-pointer-reference) prints `identifier-3 … identifier-4`, and §8.4.3.1.2 identifier
// FORMAT 9 makes an address-identifier an identifier; §8.4.3.11.4 GR1 / §8.4.3.13.4 GR1 make each arm "a unique
// data item of class pointer", which is exactly what §8.8.4.2.3 SR5 asks of identifier-3 and identifier-4. With
// only valueOperand here `IF P = ADDRESS OF X` was a bare COBOL0001 on conforming source. It is the ONE
// `addressIdentifier` rule PB239 gave the CALL argument (CobolParserCore.g4), so the §8.4.3.11 storage forcing
// (DataBinder.PtrScanAddressOfTargets walks that rule's nodes) and the 2002 introduction gate
// (VersionConformancePass.VisitAddressIdentifier) reach this surface with no change of their own. ADDRESS is a
// reserved word that heads no valueOperand, so the new arm is unambiguous and every relation that parsed before
// parses identically — the shared comparison DFA (the DEVLOG-621 lesson) is not re-predicted on any old input.
// The abbreviated relation (§8.8.4.12) reaches it through the same rule.
comparisonOperand
    : addressIdentifier   // §8.8.4.2.2 Format 3 identifier-3 / identifier-4 (kb/Work PB1021)
    | valueOperand
    ;

// ── COBOL-2002 boolean expressions (ISO §8.8.2; precedence B-NOT > B-AND > B-XOR > B-OR, rule 7b).
// Permissive-superset doctrine: the operand SHAPES (a boolean item / boolean literal / figurative ZERO /
// ALL B"…") are enforced at BIND (the boolean-expression constraint band); the tiers enforce the formation
// rules 1–3 and EVERY Table 4 adjacency cell but ONE structurally. ⛔ THE EXCEPTION IS (B-NOT, B-NOT): this
// comment used to claim Table 4 was enforced structurally in full, and it was not — `booleanFactor : B_NOT
// booleanFactor` self-recurses, so `B-NOT B-NOT x` parsed with no diagnostic at any --std while the comment
// asserted otherwise (a green-looking claim holding a gap open, kb/Work PB158). That cell is now screened by
// ExpressionFormationPass / ArithmeticFormationRules (COBOLNET1719), alongside §8.8.1.2 Table 3's matching
// (unary, unary) cell — one mechanism for both adjacency tables. The tiers are SUPERSET-parsed (no edition predicate); they are
// reached ONLY through the boolExprAhead()-gated primaryCondition ENTRY (or COMPUTE F2), so a B-op-free condition
// never enters them (the shared comparisonExpression rule is untouched — the DEVLOG-621 lesson). The COBOL-2002
// introduction gate is bind-time: Check(BooleanOperators2002) in BindBoolExpr when HasBoolOp — residue migration #2. ──
booleanExpression : booleanXorTerm ( B_OR booleanXorTerm )* ;
booleanXorTerm    : booleanAndTerm ( B_XOR booleanAndTerm )* ;
booleanAndTerm    : booleanShiftTerm ( B_AND booleanShiftTerm )* ;
// Boolean shift tier (ISO §8.8.2 rule 8, COBOL-2023). The shift's SECOND operand is an INTEGER operand (rule 5 /
// Table 4 — after a shift operator ONLY an identifier-or-literal integer may appear), never a booleanFactor. This
// fixed placement gives the shift tighter-than-B-AND binding, which realizes the default (unmixed) rule-7b case
// exactly; the context-sensitive rule-7b precedence (a shift inheriting the precedence of a preceding B-OR/B-XOR)
// is a documented refinement (see BoundBoolShift / the wave-C scout).
booleanShiftTerm  : booleanFactor booleanShiftSuffix* ;
// The shift's second operand is an INTEGER operand (ISO §8.8.2 rule 5). Permissive-superset: parse an
// arithmeticExpression (an integer literal / data item is a subset) — the count is used as an integer at runtime.
booleanShiftSuffix : (B_SHIFT_L | B_SHIFT_R | B_SHIFT_LC | B_SHIFT_RC) arithmeticExpression ;
booleanFactor     : B_NOT booleanFactor
                  | LPAREN booleanExpression RPAREN
                  | valueOperand
                  ;

comparisonExpression
    : comparisonOperand IS? NOT? OMITTED                           // omitted-argument condition (§8.8.4.8; 2002+ - kb/Work PB133)
    | comparisonOperand IS? NOT? className                         // class condition
    | comparisonOperand IS? NOT? (POSITIVE | NEGATIVE | ZERO)      // sign condition (merged from signCondition); §8.8.4.7.2 prints the keyword ZERO only — PB510
    | comparisonOperand ( comparisonOperator comparisonOperand )?  // existing relational + bare operand
    ;
    // NOTE (Phase-4a increment 2, DEVLOG 621): the boolean RELATION (§8.8.4.2.2) and the simple boolean
    // CONDITION (§8.8.4.3) are STAGED RESIDUE — a booleanExpression alternative here disturbed the shared
    // parser's comparison DFA (subscripted / ref-mod comparisons at 2002+ regressed: `ELEM(I) = x` → "no
    // viable alternative"), so the condition-context boolean forms are deferred to a focused grammar pass.
    // The boolean OPERATORS work in COMPUTE Format 2 (its own dedicated computeStatement alt, isolated from
    // conditions). `IF (a B-AND b)` etc. are NOT yet supported.

// ⛔ THE ONE CLASS-CONDITION ALTERNATIVE LIST (ISO §8.8.4.4.2), read off the PRINTED general format (PDF
// page 224 / printed 194, rendered): identifier-1 IS [NOT] followed by a brace group of FOURTEEN alternatives
// with no choice indicator — exactly one is selected. `IS` is NOT underlined there, so it is optional (IS?);
// every keyword alternative IS underlined, and alphabet-name-1 / class-name-1 are the two user-defined names,
// both served by the cobolWord alternative and told apart at bind (ConditionBinder.BindClassConditionOn).
//   It was written down TWICE (kb/Work PB590): a second rule `classCondition` served `evaluateSubject` with
// NUMERIC / ALPHABETIC / ALPHABETIC-LOWER / ALPHABETIC-UPPER / ALPHANUMERIC — an ALPHANUMERIC alternative the
// general format does not print at all, and no BOOLEAN, class-name-1 or alphabet-name-1 — so the same class
// test meant different things depending on whether it was written in an IF or as an EVALUATE subject. That
// rule is DELETED and evaluateSubject now names this one.
//   ALL FOURTEEN ARE OFFERED. Each keyword alternative is one line here plus one ClassConditionModel row plus
// one ConditionRenderer.RenderClass arm, and ClassConditionTableDriftTests fails if any of the three is missed
// (the seven COBOL-2014 numeric-content alternatives landed together — kb/Work PB225).
//   ⛔ BOOLEAN AND THE SEVEN 2014 WORDS ARE KEYWORDS HERE ONLY WHERE §8.9 RESERVES THEM (kb/Work PB655). BOOLEAN
// is a COBOL-2002 reserved word (not reserved at 85), the seven numeric-content words are reserved from 2014, and
// below those editions each is an ordinary user-defined word: `CLASS BOOLEAN IS "01"` in SPECIAL-NAMES is
// conforming COBOL-85 and `IF X IS BOOLEAN` names that user class. The token-level reservation gate retypes such
// a DECLARED class to IDENTIFIER, so it arrives through cobolWord; a keyword token that still arrives below its
// edition is an UNDECLARED class-name-1, and ConditionBinder reads it as one (→ COBOLNET1639) instead of as the
// §8.8.4.4.4 GR3 alternative. (Until PB655 the gate was a cobolWord predicate and the ORDER of these alternatives
// was the edition gate; a predicate past the left edge of an enclosing decision cannot be one.)
className
    : NUMERIC
    | ALPHABETIC
    | ALPHABETIC_LOWER
    | ALPHABETIC_UPPER
    | cobolWord                     // alphabet-name-1, or a user-defined CLASS from SPECIAL-NAMES
    | BOOLEAN                       // §8.8.4.4.4 GR3 e) — reserved (and so reachable here) at 2002+ only
    | FARTHEST_FROM_ZERO            // GR3 g) — this and the six below reserved (reachable here) at 2014+ only
    | FLOAT_INFINITY                // GR3 h)
    | FLOAT_NOT_A_NUMBER            // GR3 i)
    | FLOAT_NOT_A_NUMBER_QUIET      // GR3 j)
    | FLOAT_NOT_A_NUMBER_SIGNALING  // GR3 k)
    | IN_ARITHMETIC_RANGE           // GR3 l)
    | NEAREST_TO_ZERO               // GR3 m)
    ;

comparisonOperator
    // Symbolic (with optional IS prefix per §8.8.4.2.2, the relation-condition general format)
    : IS? EQUALS
    | IS? NOTEQUAL
    | IS? LTEQUAL
    | IS? GTEQUAL
    | IS? LT
    | IS? GT
    // Abbreviated NOT + symbolic (ISO §8.8.4.12)
    | IS? NOT EQUALS       // NOT =
    | IS? NOT GT            // NOT >
    | IS? NOT LT            // NOT <
    | IS? NOT GTEQUAL       // NOT >=
    | IS? NOT LTEQUAL       // NOT <=
    // Word forms with optional IS and optional THAN
    | IS? EQUAL (TO | THAN)?
    | IS? NOT EQUAL (TO | THAN)?
    | IS? GREATER THAN? OR EQUAL TO?
    | IS? NOT GREATER THAN? OR EQUAL TO?
    | IS? LESS THAN? OR EQUAL TO?
    | IS? NOT LESS THAN? OR EQUAL TO?
    | IS? GREATER THAN?
    | IS? NOT GREATER THAN?
    | IS? LESS THAN?
    | IS? NOT LESS THAN?
    ;

// =========================
// Arithmetic
// =========================

arithmeticExpression
    : additiveExpression
    ;

additiveExpression
    : multiplicativeExpression ( addOp multiplicativeExpression )*
    ;

addOp
    : PLUS
    | MINUS
    ;

multiplicativeExpression
    : powerExpression ( mulOp powerExpression )*
    ;

mulOp
    : STAR
    | SLASH
    ;

powerExpression
    : unaryExpression ( POWER unaryExpression )*
    ;

// ⛔ THE SELF-RECURSION IS DELIBERATE AND MUST STAY (kb/Work PB158). §8.8.1.2 Table 3 marks (unary, unary) an
// INVALID pair, and the obvious fix — `addOp primaryExpression`, the non-self-recursive shape §8.8.4.11.3's
// Table 5 tier (unaryLogicalExpression) uses to exclude NOT NOT structurally — REJECTS LEGAL SOURCE here.
// §8.3.3.3.2 rule 2 makes a sign part of the numeric literal when the literal is one contiguous character-string,
// so `- -2` is the PERMISSIBLE (unary, literal) pair while `- - 2` is the invalid (unary, unary) one — and in the
// DEFAULT lexer mode both emit MINUS MINUS INTEGERLIT (the SIGNED_INTEGERLIT/SIGNED_DECIMALLIT twins that encode
// the adjacency exist only in the FUNCTION-argument and SUBSCRIPT regions). No CFG tier can separate them; only
// the TOKEN POSITIONS can. The cell is therefore screened post-parse by ArithmeticFormationRules (COBOLNET1719),
// which reads that adjacency off the token stream. Precedence is unaffected and correct as written: powerExpression
// puts a full unaryExpression in its base position, so `- 2 ** 2` binds as (-2)**2 = 4 per Table 3 GR2's rank 1
// over rank 2 — COBOL's one inversion of the mainstream convention.
unaryExpression
    : addOp unaryExpression          // unary + or -
    | primaryExpression
    ;

// =========================
// Primaries
// =========================

// ⛔ `inlineMethodInvocation` SITS WHERE `functionCall` SITS, AND THAT PAIRING IS A RULE, NOT A HABIT
// (kb/Work PB428). §8.4.3.1.2 makes a function-identifier (Format 1) and an inline method invocation
// (Format 4) two formats of ONE thing — an identifier — and their two exclusions are word-for-word twins:
// §8.4.3.2.3 SR1 "A function-identifier shall not be specified as a receiving operand" and §8.4.3.4.3 SR1
// "Inline method invocation shall not be specified as a receiving operand". So the SENDING positions that
// admit one admit the other, and the RECEIVING rules admit neither. InlineMethodInvocationOperandDriftTests
// enforces exactly that over the .g4 text, so the NEXT operand rule to gain functionCall cannot forget it.
// It precedes `dataReference` because its own first element IS a dataReference (ANTLR takes the first
// matching alternative — feedback_grammar_precedence).
primaryExpression
    : numericLiteral
    | ZERO_ARITH                       // figurative ZERO rewritten by token rewriter in arithmetic context
    | functionCall
    | inlineMethodInvocation           // §8.4.3.1.2 Format 4 (§8.4.3.4) — the Format-1 twin above
    | dataReference
    | LPAREN arithmeticExpression RPAREN
    ;

// FUNCTION calls (1989 Amendment to COBOL-85 — intrinsic functions, ISO §15; function-identifier §8.4.3.2).
// P7 Step 12: the argument-list '(' after "FUNCTION functionName" stays in DEFAULT lexer mode (the lexer's
// FUNCTION suppression — §8.4.3.2 SR6: that '(' is ALWAYS the argument list), so each argument parses as a real
// arithmeticExpression through the ONE expression grammar (SR8: an argument is an identifier, literal, boolean
// expression, or arithmetic expression). Arguments are separated by separators only (§8.3.5: space, or
// comma/semicolon followed by space — both skipped by the lexer), so the list is plain juxtaposition; the
// space-vs-adjacent sign distinction (MAX(A -4) = two args, MAX(A - 4) = one subtraction; §8.7.1 operator
// spacing + §8.3.3.3.2 literal-sign adjacency) is preserved by the lexer's argument-region SIGNED_* twins.
// A nested data-reference subscript inside an argument still lexes in SUBSCRIPT mode (the D10/PHASE-15
// deferral is untouched). The empty-parens form (FUNCTION RANDOM ()) is the §8.4.3.2 SR6 NOTE's shape.
// The keyword-omitted form name(args) (§8.4.3.2 SR2) has NO grammar alternative (D2 — irreducible ambiguity
// with a subscripted dataReference); the binder re-parses its captured argument text through
// functionArgListFragment below.
//
// ── REFERENCE-MODIFYING THE FUNCTION RESULT (fix-queue PB8, ISO §8.4.3.3.3 SR2) ────────────────────────────
// §8.4.3.1.2 Format 3 makes `identifier-1 reference-modifier-1` an IDENTIFIER, and §8.4.3.3.3 SR2 admits a
// function-identifier as identifier-1 ("If identifier-1 is a function-identifier, it shall reference an
// alphanumeric, boolean, or national function"). §8.4.3.1.4 GR1 fixes the order: (f) the argument list applies
// to the function-name on its left, THEN (g) "a reference modifier applies to the identifier on the left" — so
// the tail sits after the optional argument list, and BOTH of these are legal:
//     FUNCTION CURRENT-DATE (1:4)              -- zero-argument function, ref-modified
//     FUNCTION UPPER-CASE("abc") (1:2)         -- argument list, THEN ref-modified
// ⛔ NO LEXER CHANGE IS INVOLVED, and the fix-queue entry that said otherwise was wrong. A token dump of both
// shapes (pinned by CobolLexerModeDriftTests) shows the ref-mod paren lexed in DEFAULT mode in each case: after
// a function NAME the lexer's FUNCTION suppression already keeps it out of SUBSCRIPT mode, and after the
// argument list's ')' the previous token is not a data-name so the SUBSCRIPT trigger never fires. Both therefore
// reach the DEFAULT-mode refModPart (COLON, not SUB_COLON) that already exists for dataReference.
// ⚠ `refModPart*`, not `refModPart?`, and that is deliberate: §8.4.3.3.3 SR3 ("identifier-1 shall not be a
// reference-modification format identifier") forbids ref-modifying a ref-mod, and making the ARITY carry that
// rule here would enforce SR3 in a SECOND place — the data-reference side already counts ref-mods in
// ReferenceResolver, because `dataReferenceSuffix*` cannot express the limit either. One rule belongs in one
// place, so both sides parse the extra modifier and the binder rejects it with COBOLNET1630, giving the same
// cited message for FUNCTION F(X) (1:4)(1:2) as for A (3:4)(2:2) instead of a raw parse error on one side only.
// ⚠ The two alternatives are disjoint on the COLON and need no predicate: a functionArgList can never contain a
// DEFAULT-mode COLON (a nested data-item ref-mod inside an argument lexes SUB_COLON in SUBSCRIPT mode). §8.4.3.2
// SR6 — "if a function's definition permits arguments … the left parenthesis is ALWAYS … that function's
// arguments" — is a CATALOG question the grammar cannot answer, so the binder enforces it (IntrinsicBinder):
// a bare ref-mod directly after an argument-PERMITTING function name is an SR6/SR8 argument-list error.
// ── THE KEYWORD-OMITTED FORM FOR A RESERVED INTRINSIC NAME (fix-queue PB9) ────────────────────────────────
// §8.4.3.2.3 SR2 lets the word FUNCTION be omitted for any intrinsic the REPOSITORY declares. For an ordinary
// intrinsic name that omission is the D2 IRREDUCIBLE AMBIGUITY — `FOO(1)` is equally a subscripted data
// reference — so it has no grammar alternative and the binder re-routes a dataReference instead.
// ⛔ THAT ARGUMENT DOES NOT APPLY TO A RESERVED WORD, and that is the whole basis for this alternative: a
// reserved word can NEVER be a user-defined data name (§8.3.2.4.1), so `SUM(1 2 3)` cannot be a subscripted
// data reference and there is nothing to be ambiguous with. §8.9 ∩ §8.11 is exactly four words — LENGTH,
// RANDOM, SIGN, SUM. LENGTH already reaches the binder through the cobolWord name slot (it needs to be there
// for `START WITH LENGTH` anyway) and keeps that route; the other three arrive here.
// ⚠ WHY NOT JUST ADD THEM TO cobolWord LIKE LENGTH — this was tried and NIST NC116A caught it. A word in the
// name slot is admissible as a VALUE-clause literal (a constant-name), so the greedy literal list swallows a
// FOLLOWING data-description clause keyword:
//     01 W PICTURE S99999 VALUE ZERO
//            SIGN LEADING SEPARATE.        ->  COBOLNET1585 "takes exactly one literal"
// LENGTH is safe there only because LENGTH does not BEGIN a data-description clause; SIGN and SUM both do
// (§13.18.52 SIGN, §13.18.54 report-writer SUM). This alternative is confined to expression/operand positions and
// cannot reach a data description at all, so that whole class is structurally out of reach.
// ⚠ THE ARGUMENT LIST IS A `subscriptPart`, NOT `LPAREN functionArgList RPAREN`, and a token dump is what says
// so: these words carry subscriptTrigger=true, so with no FUNCTION keyword before them the lexer pushes
// SUBSCRIPT mode at the '(' and the arguments arrive as SUB_* tokens —
//     COMPUTE N = SUM(1 2 3)  ==>  COMPUTE IDENTIFIER EQUALS SUM LPAREN SUB_INTEGERLIT×3 SUB_RPAREN
// which is exactly the D2 carrier the keyword-omitted form already uses. The binder therefore re-parses it
// through the SAME `ReparseArgs` → `functionArgListFragment` path as every other keyword-omitted reference,
// rather than growing a second argument grammar. (Pinned by CobolLexerModeDriftTests.)
// ⛔ THE ARGUMENT GROUP IS REQUIRED FOR SIGN AND SUM, AND THAT IS WHAT KEEPS THIS SAFE — it is derived from the
// functions' own general formats, not an ad-hoc guard. §15.81.2 writes `FUNCTION SIGN ( argument-1 )` and
// §15.88.2 `FUNCTION SUM ( { argument-1 } … )`: neither has a no-argument form, so a BARE `SIGN` or `SUM` is
// never a function reference. Requiring the group is what stops the collision NIST NC116A found:
//     01 W PICTURE S99999 VALUE ZERO
//            SIGN LEADING SEPARATE.
// A VALUE clause operand is a `unaryExpression`, which reaches functionCall, and the operand loop is greedy —
// so admitting a BARE SIGN made the loop swallow the SIGN CLAUSE of the NEXT line as a second VALUE literal
// (COBOLNET1585). With the group required, `SIGN LEADING` cannot match a functionCall at all and the loop stops
// where it should. The same applies to the report-writer `SUM OF` clause (§13.18.54).
// RANDOM is different and needs no group: §15.75.2 brackets the whole parenthesised part, so the bare form is
// legal — and RANDOM begins no data-description clause, so nothing can swallow it.
// ⛔ THE ARGUMENT-LIST PARENS ARE FNARG_LPAREN / FNARG_RPAREN, NOT LPAREN / RPAREN (fix-queue PB48). §8.4.3.2.3
// SR6 makes the '(' immediately after an intrinsic-function-name ALWAYS the argument list, never a grouping
// paren, and the lexer retypes it from the _fnParenStack it already keeps. Writing the distinction into the
// token vocabulary is what stops the post-lex passes from having to re-derive it: ZeroTokenRewriter's "ZERO
// adjacent to a paren is arithmetic" rule was reading THIS paren and converting `FUNCTION LOWER-CASE(ZERO)`'s
// figurative into an arithmetic zero, so the §15.3 class screen saw class numeric and rejected legal source.
// A grouping paren INSIDE the list (`FUNCTION MAX((A + B) 2)`) is an ordinary LPAREN and is unaffected.
functionCall
    : FUNCTION functionName (FNARG_LPAREN functionArgList? FNARG_RPAREN)? refModPart*
    | reservedIntrinsicArgFn subscriptPart refModPart*
    | RANDOM subscriptPart? refModPart*
    ;

// The §8.9-reserved words that are also §8.11 intrinsic function names AND require arguments. LENGTH is the
// fourth member of §8.9 ∩ §8.11 and is absent deliberately: it already reaches the binder through the cobolWord
// name slot (where it must be anyway, for `START WITH LENGTH`), and adding it here would make `LENGTH OF x`
// ambiguous. Legal only when the REPOSITORY declares the function — a question the grammar cannot answer, so
// IntrinsicBinder enforces §8.4.3.2.3 SR2.
reservedIntrinsicArgFn
    : SIGN
    | SUM
    ;

// Arguments separate by space (no token — plain juxtaposition) or by the §8.3.5 comma/semicolon-plus-space
// separator, which inside an argument region survives as FNARG_SEPARATOR so a following '(' reads as a
// parenthesized ARGUMENT, never as a subscript of the previous argument's data-name.
functionArgList
    : functionArgument (FNARG_SEPARATOR? functionArgument)*
    ;

// One function argument (§8.4.3.2 SR8 + the §15 per-function phrase words). The phrase-keyword alternative
// admits the RESERVED words that appear inside §15 argument lists (TRIM LEADING/TRAILING §15.96; FIND-STRING
// LAST/START/AFTER §15.37; SUBSTITUTE FIRST/LAST §15.87; CONVERT ANY/ALPHANUMERIC/NATIONAL §15.19) — words that
// are plain IDENTIFIERs (ANYCASE, HEX, NAT, ANUM, BYTE, CURRENT, ACTIVATING, NESTED, STACK, TOP-LEVEL) arrive
// through dataReference inside arithmeticExpression and are classified by name in the binder. OMITTED is the
// §8.4.3.2.2 format's argument alternative (SR7 bars it for intrinsics — a bind-time diagnostic, not a parse
// error). A superset rule: which words a given function admits is the binder's §15 job.
// §8.4.3.2.3 SR8: "Argument-1 shall be an identifier, a literal, a boolean expression, or an arithmetic
// expression." The boolean arm sits behind the ARGUMENT-scoped boolArgAhead() predicate (kb/Work PB65,
// FMT-15.45.2): it fires only when a B-operator belongs to THIS argument, so a bare boolean literal / item still
// takes its literal / expression arm and a following B-AND outside the argument list is not this argument's.
functionArgument
    : fnArgPhraseWord
    | OMITTED
    | {boolArgAhead()}? booleanExpression
    | nonNumericLiteral
    | arithmeticExpression
    ;

fnArgPhraseWord
    : LEADING
    | TRAILING
    | LAST
    | FIRST
    | ANY
    | START
    | AFTER
    | ALPHANUMERIC
    | NATIONAL
    ;

// The D2 keyword-omitted re-parse entry (binder-invoked only): the argument text captured by a dataReference's
// subscriptPart, re-lexed with the lexer primed as a function-argument region (PrimeFunctionArgs), parses
// through the SAME functionArgList rule — ONE argument grammar for both reference forms.
functionArgListFragment
    : functionArgList? EOF
    ;

// The D18 SUBSCRIPT-EXPRESSION re-parse entry (binder-invoked only; ISO §8.4.2.3.2 admits `arithmetic-expression-1`
// as a subscript and §8.4.3.3.3 SR4 as a reference-modifier position). A subscript / ref-mod SEGMENT that the
// SUBSCRIPT-mode token renderer (ReferenceResolver.RenderSegment) cannot render — today a function-identifier,
// fix-queue PB17 — is re-lexed from its VERBATIM source text and parsed through the ONE arithmeticExpression rule,
// so it binds through ExpressionBinder.BindExpr exactly as every other arithmetic expression does. That is what
// keeps the token renderer a token renderer instead of growing a THIRD hand-written expression compiler beside
// ExpressionBinder and IntrinsicRenderer.
// Referenced by nothing in compilationUnit — the functionArgListFragment precedent, so ZERO blast radius on the
// main parse; the SUBSCRIPT lexer mode and the main subscript grammar are untouched (the D10/PHASE-15 deferral
// stands).
// ⛔ DEFAULT lexer mode, NOT PrimeFunctionArgs: a subscript is not a function-argument region, so the
// §8.3.3.3.2 sign-adjacent literal twinning (which makes `MAX(A -4)` two arguments) must NOT fire here — inside a
// subscript `A -4` is the subtraction §8.7.1 says it is.
subscriptExpressionFragment : arithmeticExpression EOF ;

// ── COMPILE-TIME DIRECTIVE-EXPRESSION fragments (ISO §7.3.6 arithmetic / §7.3.7 boolean / §7.3.8 constant-
// conditional-expression). Isolated fragment entry rules — reachable ONLY from the frontend's directive-expression
// re-parse (the functionArgListFragment precedent: referenced by nothing in compilationUnit, so ZERO blast radius
// on the main parse). They reuse the existing operand sub-rules (arithmeticExpression / booleanExpression /
// nonNumericLiteral / comparisonOperator / cobolWord) — no duplicated expression grammar. The lexer is primed with
// PrimeDirectiveExpr() so DEFINED is a token and every '(' groups. Evaluated by the ONE shared
// CompileTimeExpressionEvaluator (DESIGN-compile-time-expressions.md §4). ──

// One compile-time operand (a >>DEFINE value, a >>EVALUATE selection-subject / >>WHEN object). Operand-kind
// disambiguation reuses the boolExprAhead() predicate (the primaryCondition mechanism): booleanExpression is
// entered ONLY when a real B-operator is present; otherwise an arithmetic operand (a single numeric literal too —
// GR5 reclassification is in the evaluator) or a non-numeric literal. The evaluator dispatches on WHICH sub-node
// parsed, not a token guess (booleanExpression's leaf would otherwise match every arithmetic/non-numeric operand).
compileTimeOperandFragment : compileTimeOperand EOF ;
compileTimeOperand
    : {boolExprAhead()}? booleanExpression      // a genuine boolean expression (a B-operator is present)
    | arithmeticExpression                       // numeric operand (a single numeric literal too — GR5 in eval)
    | nonNumericLiteral                          // string / national / boolean / hex literal operand
    ;

// A constant-conditional-expression (§7.3.8) — the >>IF operand and the >>EVALUATE-TRUE >>WHEN operand.
// Precedence NOT > AND > OR (§8.8.4.9), parentheses grouping (LPAREN is always a group under PrimeDirectiveExpr).
constantConditionalExpressionFragment : constantConditionalExpression EOF ;
constantConditionalExpression : cceOr ;
cceOr      : cceAnd ( OR cceAnd )* ;
cceAnd     : cceNot ( AND cceNot )* ;
cceNot     : NOT cceNot | ccePrimary ;
ccePrimary : LPAREN constantConditionalExpression RPAREN
           | definedCondition
           | cceRelationOrBoolean ;
// §7.3.8.4.4 defined-condition. DEFINED is a token only under PrimeDirectiveExpr (a primed-lexer keyword). Listed
// before cceRelationOrBoolean so a trailing DEFINED selects it; a cobolWord with no DEFINED falls through.
definedCondition : cobolWord IS? NOT? DEFINED ;
cceRelationOrBoolean
    : {boolExprAhead()}? booleanExpression                            // §8.8.4.3 simple boolean condition (length-1)
    | compileTimeOperand ( IS? NOT? comparisonOperator compileTimeOperand )? ;   // §7.3.8.2 relation (or a bare operand)

// Function names are normally IDENTIFIERs, but several intrinsic function names
// collide with reserved words (lexer tokens). List them explicitly so the parser
// accepts them after FUNCTION.
functionName
    : IDENTIFIER
    | DISPLAY
    | LENGTH
    | MERGE
    | NATIONAL
    | BIT
    | RANDOM
    | SIGN
    | SORT
    | SUM
    ;

// The in-line method invocation's arguments (CobolOO.g4) — juxtaposed like every COBOL argument list; the comma
// is the optional separator (the same `SEPARATOR?` shape functionArgList uses). Swept with classValueSet.
argumentList
    : argument (COMMA? argument)*
    ;

// ⛔ THE FIVE ARGUMENT FORMS ARE THE PRINTED ONES, AND THREE OF THEM WERE MISSING (kb/Work PB428). §8.4.3.4.2's
// brace group — rendered from the canonical PDF page 163, printed folio 133 — is
// `{ arithmetic-expression-1 | boolean-expression-1 | identifier-2 | literal-2 | OMITTED }`, with OMITTED the
// only underlined word. This rule read `arithmeticExpression | literal | dataReference`, which is the shape the
// misnamed `id(args)` statement needed; boolean-expression-1 and OMITTED had no surface at all.
// ⛔ THE ORDER IS THE `invokeArgument` BY-CONTENT ORDER, AND FOR THE SAME MEASURED REASON (CobolOO.g4's PB46
// note): `arithmeticExpression` SUBSUMES `dataReference` and every numeric literal, so identifier-2 is NOT a
// separate alternative here — it is recovered IN THE BINDER from a sole-dataReference expression
// (ConditionBinder.SoleDataReference, the shape OoBindInvokeArg already uses), because the grammar cannot
// express "a reference, unless it is part of an expression" without the ambiguity that caused PB46. `literal`
// precedes it so a non-numeric literal-2 keeps the literal arm, and `booleanExpression` takes the proven
// {boolExprAhead()}? gate because its leaf `valueOperand` matches everything the other two arms match.
// ⚠ OMITTED IS FIRST AND IS A RESERVED WORD (§8.9), so it can never be a data-name and shadows nothing.
// ⛔ THE ADDRESS-IDENTIFIER JOINS AS identifier-2 (kb/Work PB1021): §8.4.3.4.4 GR1 makes these the arguments of
// `INVOKE … USING`, whose §14.9.23.3 SR9 names the address-identifier outright — the SAME `addressIdentifier` rule
// the INVOKE statement's own invokeArgument takes. ADDRESS is reserved and heads no other alternative.
argument
    : OMITTED
    | addressIdentifier   // §14.9.23.3 SR9 / SR19 via §8.4.3.4.4 GR1 (kb/Work PB1021)
    | {boolExprAhead()}? booleanExpression
    | literal
    | arithmeticExpression
    ;

// =========================
// Literals
// =========================

literal
    : numericLiteral
    | nonNumericLiteral
    ;

numericLiteral
    : signedNumericLiteral
    ;

// A concatenation expression (ISO §8.8.3) is an ALTERNATIVE OF the non-numeric-literal rule because §8.8.3.3
// GR3 makes it "equivalent to a literal of the same class and value, [usable] anywhere a literal of that class
// may be used" — every literal position (VALUE clauses, statement operands, FUNCTION arguments, …) inherits it
// through this one rule. The concat tier is a distinct ADDITIVE alternative (first, per ANTLR first-match
// precedence); the plain single-token alternatives are untouched, so a non-concatenated literal parses with
// exactly the same shape as before. AMPERSAND appears in no other rule, so prediction is unambiguous: a literal
// followed by '&' can only be a concatenation expression.
// figurativeConstant is listed FIRST (kb/Work PB71): `ALL "A" & "B"` is the figurative ALL over the concatenated
// literal-1 "AB" (§8.3.3.6.3 SR2 — literal-1 "may be a concatenation expression"), and ANTLR resolves the tie
// between that reading and "a concatenation whose first operand is ALL "A"" (illegal — §8.8.3.2 SR1) toward the
// lower alternative. A concatenation that merely CONTAINS an ALL figurative (`"X" & ALL "A"`) still parses as a
// concatenation and is rejected COBOLNET1541 by ConcatFolder as before.
nonNumericLiteral
    : figurativeConstant
    | concatenationExpression
    | STRINGLIT
    | NATLIT
    | BOOLLIT
    | HEXLIT
    ;

// ISO §8.8.3.1 general format: {literal-1 | concatenation-expression-1} & literal-2 — left-recursive in the
// spec, flattened here to operand (& operand)+; ConcatFolder folds the class back LEFT-to-right, pair by pair
// (§8.8.3.3 GR1 — kb/Work PB1406). Operands are literals of class alphanumeric (STRINGLIT and its X"…" hex
// format), national, or boolean, or figurative constants (§8.8.3.2 SR1); numeric literals are NOT operands (SR1
// admits only the three classes), so `5 & …` is a parse error by construction. A WORD is an operand too: the two
// words that stand for a literal — a constant-name (§13.10.3 SR2: "anywhere that a format specifies a literal of
// the class and category of constant-name-1") and a symbolic-character (a figurative constant its SPECIAL-NAMES
// SYMBOLIC CHARACTERS clause defines, §12.3.7.4 GR11 a)
// — are both user-defined words, which the parse cannot tell apart from a data-name, so the arm admits any
// cobolWord and ConcatFolder resolves it (COBOLNET2473 for a word that stands for no literal). The SR1 same-class
// rule, the no-ALL-figurative rule, and the SR2–SR4 8,191-position caps are BIND-time checks too (a superset
// parse, per the repo's parse-wide/bind-narrow doctrine). Gated 2002+ by the VersionConformancePass parse arm
// (concat-operator-2002 → COBOLNET0900 below 2002); the grammar itself is edition-agnostic (superset parse at
// every --std).
concatenationExpression
    : concatOperand (AMPERSAND concatOperand)+
    ;

concatOperand
    : STRINGLIT
    | NATLIT
    | BOOLLIT
    | HEXLIT
    | figurativeConstant
    | cobolWord          // a constant-name or a symbolic-character (kb/Work PB1406); LAST, after the keyword figuratives
    ;

signedNumericLiteral
    : (PLUS | MINUS)? numericLiteralCore
    ;

// Numeric literal assembly.
// DOT-based decimals use DECIMALLIT from the lexer (maximal munch resolves
// DOT-as-decimal vs DOT-as-sentence-terminator unambiguously).
// COMMA-based decimals for DECIMAL-POINT IS COMMA are assembled here in the parser.
numericLiteralCore
    : FLOATLIT                             // 1.5E3, 2.5E-2 (floating-point literal, ISO §8.3.3.3.3 — D16)
    | COMMA_FLOATLIT                       // 1,5E3 (the DECIMAL-POINT IS COMMA floating-point literal — kb/Work PB98)
    | DECIMALLIT                           // 123.45 or .45 (dot decimal from lexer)
    | SIGNED_DECIMALLIT                    // -15.6 (sign-adjacent literal — FUNCTION-argument regions only, P7 Step 12)
    | SIGNED_INTEGERLIT                    // -4 (sign-adjacent literal — FUNCTION-argument regions only)
    | INTEGERLIT COMMA INTEGERLIT          // 123,45 (comma decimal — DECIMAL-POINT IS COMMA)
    | COMMA INTEGERLIT                     // ,45 (leading comma decimal)
    | INTEGERLIT                           // 123 (integer)
    ;

// ⛔ TWO SLOTS, AND THE DIFFERENCE IS THE SIGN — NOT ONE RULE WITH A HOLE IN IT (kb/Work PB553).
// `integerLiteral` is the metalanguage `integer-n` of a printed general format, and its UNSIGNED shape IS the
// rule: ISO §5.5 1) — "When the term 'integer-n' (n = 1, 2, …) is used in a general format and associated
// rules, it refers to a fixed-point integer literal that shall be unsigned and nonzero unless otherwise
// specified in the associated rules." So `01 T PIC X OCCURS +3.` is correctly rejected (§13.18.38.3 SR16
// otherwise-specifies only the NONZERO half, never the sign), and widening THIS rule would accept it.
// `signedIntegerLiteral` is the other slot: one whose own syntax rule constrains the operand as an INTEGER
// NUMERIC LITERAL rather than as `integer-n` — the Format 2 (table) VALUE clause's FROM/TO subscripts. §5.5 2) a) — "if that operand is a literal, it shall be an
// integer literal, as defined in 8.3.3.3.2, Fixed-point numeric literals" — and §8.3.3.3.2 2) admits the sign:
// "A literal shall not contain more than one sign character. If a sign is used, it shall appear as the
// leftmost character of the literal." §13.18.63.3 SR19, "Subscript-1 and subscript-2 shall be integer numeric
// literals", is the one such slot in the whole grammar today; SignedIntegerSlotDriftTests pins that inventory
// so a second one cannot be added silently, in either direction.
// ⚠ SUPERSET-PARSE / BIND-NARROW, the standing doctrine: the sign is a SEPARATE token here (a DEFAULT-mode
// `(` after FROM/TO opens no signed-literal lexer region), so `FROM ( + 1 )` — a space between the sign and
// its digits, which §8.3.3.3.2 forbids because a literal is one character-string — also parses, and
// DataBinder reports it BY NAME (COBOLNET2155) instead of leaving it to the ANTLR error reporter.
integerLiteral
    : INTEGERLIT
    ;

signedIntegerLiteral
    : (PLUS | MINUS)? INTEGERLIT
    ;

// The literal-1 of the ALL figurative (§8.3.3.6.3 SR2): "an alphanumeric, boolean, or national literal, any of which
// may be a concatenation expression" — one quoted literal of any class (plain / hexadecimal alphanumeric, national
// N"…" / NX"…", boolean B"…" / BX"…"), or THE §8.8.3 concatenationExpression, the same rule every other literal
// position uses, so its operands are every §8.8.3.2 SR1 operand: a figurative constant ("may be specified as one or
// both operands") and a constant-name or symbolic-character word included (kb/Work PB1627 — a second, quoted-only
// operand rule here refused `ALL "A" & SPACE` and `ALL "A" & K`). Greedy: the ALL binds the WHOLE concatenation
// (kb/Work PB71). A constant-name literal-1 written ALONE is the figurativeConstant `ALL cobolWord` arm below (the
// parse cannot tell it from a symbolic-character); ConcatFolder.FoldAllLiteral is the one reader of both shapes.
allLiteral
    : concatenationExpression
    | STRINGLIT
    | HEXLIT
    | NATLIT
    | BOOLLIT
    ;

// §8.3.3.6.2 formats 1–5: the figurative spellings a format's braces make INTERCHANGEABLE — and only here
// (kb/Work PB510). Each spelling is its own lexer token (one token per §8.9 reserved word), so a general format
// that names ONE of them as a keyword (BLANK WHEN ZERO, the sign condition's ZERO, OPTIONS INITIALIZE's BINARY
// ZEROES / HIGH-VALUES / LOW-VALUES / SPACES) writes that token and admits no other spelling.
zeroWord      : ZERO | ZEROS | ZEROES ;
spaceWord     : SPACE | SPACES ;
highValueWord : HIGH_VALUE | HIGH_VALUES ;
lowValueWord  : LOW_VALUE | LOW_VALUES ;
quoteWord     : QUOTE_ | QUOTES ;

figurativeConstant
    : zeroWord
    | spaceWord
    | highValueWord
    | lowValueWord
    | quoteWord
    | NULL_
    | ALL allLiteral    // Format 6 — ALL literal-1 (§8.3.3.6.3 SR2: an alphanumeric, boolean or national literal, which
                        // may be a concatenation expression; kb/Work PB71 — ONE arm for the four literal kinds)
    | ALL zeroWord
    | ALL spaceWord
    | ALL highValueWord
    | ALL lowValueWord
    | ALL quoteWord
    | ALL cobolWord   // Format 7 — ALL symbolic-character-1 (§8.3.3.6.2; SR4: a SYMBOLIC CHARACTERS name — kb/Work PB110),
                      // OR Format 6 over a constant-name literal-1 (§13.10.3 SR2 — kb/Work PB1627): the parse cannot
                      // tell the two words apart, so ConcatFolder.FoldAllLiteral resolves a constant-name first and
                      // the Format 7 readers take the rest. LAST so the keyword forms and ALL literal-1 win; the bare
                      // form is a word reference (the constant-name substitution seams)
    ;
