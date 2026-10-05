// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Control flow statements: PERFORM, IF, EVALUATE, GO TO, SEARCH, ALTER, USE,
// EXIT, STOP, CONTINUE, NEXT SENTENCE.
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolControlFlow;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// PERFORM / END-PERFORM (§14.9.28)
// ==========================================

performStatement
    // Out-of-line: explicit forms to avoid greedy "PERFORM target" swallowing options
    : PERFORM procedureName performTimes                                       // PERFORM para N TIMES
    | PERFORM procedureName performUntil                                       // PERFORM para UNTIL cond
    | PERFORM procedureName performVarying                                     // PERFORM para VARYING ...
    | PERFORM procedureName (THRU | THROUGH) procedureName performOptions?     // PERFORM para THRU para [options]
    // Inline forms — Formats 2 & 3 MERGED into ONE inline alternative (design §1.4 ⇒ zero cross-format lookahead):
    //   ≥1 WHEN ⇒ Format 3 (exception-checking, §14.9.28.2 Format 3), enforced at BIND
    //   (PF3-STRUCT-WHEN-REQUIRED / COBOLNET1597); no WHEN ⇒ Format 2 (inline). performInlineHead? absent +
    //   no WHEN = the old bare-block inline PERFORM; performInlineHead = performOptions+ = the old options inline.
    // MUST precede the simple `PERFORM procedureName` below: `PERFORM LOCATION …` is genuinely ambiguous
    // (LOCATION is a cobolWord ⇒ a valid out-of-line target), and only the inline arm's trailing END_PERFORM
    // disambiguates — so it is tried first; a period-terminated `PERFORM LOCATION.` has no END_PERFORM and
    // correctly falls through to the out-of-line alternative (the continuity invariant).
    // imperative-statement-1 is UNBRACKETED in BOTH inline formats (rendered from PDF page 712 / printed folio
    // 682): Format 2 is `PERFORM [ times|until|varying ] imperative-statement-1 END-PERFORM`, Format 3 is
    // `PERFORM [ WITH LOCATION ] imperative-statement-1 { WHEN … } … [ WHEN OTHER … ] [ WHEN COMMON … ]
    // [ FINALLY … ] END-PERFORM`. §5.2.6.2 licenses omission only inside brackets, so the body is REQUIRED in
    // both — `statementBlock`, not `statementBlock*` (kb/Work PB396: `PERFORM UNTIL cond END-PERFORM` with no
    // body compiled to a loop whose condition the body cannot change).
    | PERFORM performInlineHead? statementBlock
        performWhenPhrase* performWhenOther? performWhenCommon? performFinally?
      END_PERFORM
    | PERFORM procedureName                                                    // PERFORM para (simple, out-of-line)
    ;

// The inline head: Format-2 loop control (TIMES/UNTIL/VARYING) OR the Format-3 [WITH] LOCATION phrase.
// ⛔ THE FORMAT PRINTS **ONE** BRACKET OVER THREE STACKED ALTERNATIVES, so at most ONE loop-control phrase is
// admitted — `PERFORM [ times-phrase | until-phrase | varying-phrase ] imperative-statement-1 END-PERFORM`
// (§14.9.28.2 Format 2, rendered from the printed page 683/PDF 713, not read off the OCR), and Format 3 prints
// NO loop-control phrase at all. The head stays a LIST here on the project's standing superset posture, so the
// BINDER still SEES a second phrase and can name the format it violates (COBOLNET2117 / COBOLNET2118 in
// ControlFlowBinder.CheckInlineHeadCardinality) instead of the programmer getting a bare
// `COBOL0001 no viable alternative`. It is spelled `performOptions performOptions*` — one required element plus
// a superset tail — rather than `performOptions+`, so the cardinality the format prints is legible in the
// grammar itself. kb/Work PB431: `BindPerformControl`'s `FirstOrDefault()` SILENTLY DELETED every phrase after
// the first (`PERFORM 3 TIMES UNTIL X > 100` ran 3 times with no diagnostic), and the Format-3 path dropped all
// of them (`PERFORM 3 TIMES … WHEN EC-ALL …` ran its body ONCE).
performInlineHead
    : performOptions performOptions*
    | performLocationPhrase
    ;

// [WITH] LOCATION (§14.9.28.2 Format 3, COBOL-2023) — WITH is an optional word (§8.3.2.4.3); bare LOCATION is
// 2023-only because below 2023 `PERFORM LOCATION` is an ordinary out-of-line PERFORM of a paragraph named
// LOCATION (the continuity invariant — LOCATION remains a cobolWord at every edition).
performLocationPhrase
    : WITH LOCATION
    | {is2023()}? LOCATION
    ;

// A Format-3 WHEN phrase (§14.9.28.2). Two DISJOINT operand forms; each operand list's CONTINUATION is bounded
// by whenOperandAhead() so it cannot annex the leading verb of imperative-statement-2 (design §1.2/§1.5). The
// first operand after WHEN / WHEN EXCEPTION is taken UNCONDITIONALLY (superset posture — a bad first operand
// binds and the binder emits the specific COBOLNET0711 rather than a generic parse error).
// imperative-statement-2 is unbracketed inside the WHEN group's brace ⇒ REQUIRED (§5.2.6.3; kb/Work PB396).
performWhenPhrase
    : WHEN EXCEPTION performWhenModeList statementBlock      // EXCEPTION { INPUT|OUTPUT|I-O|EXTEND | file-name-1… }
    | WHEN           performWhenEcList   statementBlock      // exception-name-1… | exception-name-2 FILE file-name-2…
    ;

// EXCEPTION { INPUT | OUTPUT | I-O | EXTEND | {file-name-1}… }. The figure's "IO" denotes I-O (§8.9; a standard
// typesetting defect — every sibling format prints I-O), so the existing I_O token is reused. A single WHEN
// EXCEPTION selects exactly ONE mode OR a file-name list — never a mix; bind-enforced (COBOLNET1598).
performWhenModeList
    : INPUT | OUTPUT | I_O | EXTEND
    | fileName ({whenOperandAhead()}? fileName)*             // gated CONTINUATION only (first operand unconditional)
    ;

// { exception-name-1 }… | { exception-name-2 FILE file-name-2 }… — the EC set is open (EC-USER-*) ⇒ cobolWord.
performWhenEcList
    : performWhenEcItem ({whenOperandAhead()}? performWhenEcItem)*   // gated CONTINUATION only
    ;

performWhenEcItem
    : cobolWord (FILE fileName)*             // inner FILE-loop is self-bounding (FILE ∉ cobolWord, leads no statement)
    ;
// KNOWN LIMITATION (documented; masked while the F3 runtime is staged): a WHEN body that is a BARE 2023
// inline-method-invocation (`WHEN EC-X  obj(args)`) whose object is a cobolWord can be annexed here as a
// spurious operand — whenOperandAhead() is token-based and cannot see the following '('. The interceptor
// wave must add an LA(2)==LPAREN gate (or equivalent). Any WHEN body with a preceding statement is unaffected.

// Each of the three trailing phrases is bracketed AS A WHOLE — `[ WHEN OTHER EXCEPTION imperative-statement-3 ]`,
// `[ WHEN COMMON EXCEPTION imperative-statement-4 ]`, `[ FINALLY imperative-statement-5 ]` — so the PHRASE may be
// omitted but its imperative-statement may not be written empty (§5.2.6.2; kb/Work PB396).
performWhenOther
    : WHEN OTHER  EXCEPTION? statementBlock   // the 2nd EXCEPTION is an optional word (§8.3.2.4.3)
    ;

performWhenCommon
    : WHEN COMMON EXCEPTION? statementBlock   // the 2nd EXCEPTION is an optional word (§8.3.2.4.3)
    ;

performFinally
    : FINALLY statementBlock
    ;

performTarget
    : procedureName ((THRU | THROUGH) procedureName)?
    ;

performOptions
    : performTimes
    | performUntil
    | performVarying
    ;

// §14.9.28.2 Format 2: `{identifier-1 | integer-1} TIMES` — an identifier includes a function-identifier
// (§8.4.3.1.2 / §8.4.3.2.4 GR1: it references a temporary item; SR2's "an integer" is met by an integer-type
// function). functionCall FIRST — it begins with the FUNCTION token, so the alternation is unambiguous (the
// DISPLAY / INSPECT / FROM slots follow the same shape). kb/Work PB86: the arm was missing (a parse error), and
// the keyword-omitted spelling bound but ran ONCE.
performTimes
    : (integerLiteral | functionCall | inlineMethodInvocation | dataReference) TIMES
    ;

performUntil
    : (WITH? TEST (BEFORE | AFTER))? UNTIL condition
    | UNTIL EXIT                                       // §14.9.28.4 GR11 (2023) — an infinite loop; SR8 forbids TEST here
    ;

// §14.9.28.2's varying-phrase, rendered from the printed page 683 / PDF 713 (the OCR'd diagrams are lossy toward
// falsely-restrictive syntax, so the delimiters below come from the page):
//     [ WITH TEST {BEFORE|AFTER} ]
//     VARYING {identifier-2|index-name-1} FROM {identifier-3|index-name-2|literal-1}
//             [ BY {identifier-4|literal-2} ] UNTIL condition-1
//     [ AFTER {identifier-5|index-name-3} FROM {identifier-6|index-name-4|literal-3}
//             [ BY {identifier-7|literal-4} ] UNTIL condition-2 ] …
// ⛔ EACH FROM/BY SLOT IS A BRACE GROUP, NOT AN ARITHMETIC EXPRESSION. The slots used to be typed
// `arithmeticExpression`, which was simultaneously WIDER than the printed group (`FROM A + B` compiled with no
// diagnostic at any --std) and NARROWER (`FROM ZERO` — legal, because §14.9.28.3 SR3 restricts every literal
// here to NUMERIC and §8.3.3.6.3 SR1 permits ZERO wherever a literal so restricted appears — was a hard parse
// error, since `primaryExpression`'s only figurative arm is the ZERO_ARITH token ZeroTokenRewriter mints by
// ADJACENCY to an operator, and a bare operand has no neighbour). kb/Work PB432.
// The slot is now `valueOperand` — the SAME `arithmeticExpression | nonNumericLiteral` rule every comparison
// operand uses, so the figurative arm is the one the binder already reads (ExpressionBinder's ONE
// numeric-context literal reading: ZERO ⇒ 0, every other figurative ⇒ COBOLNET0844) rather than a second copy
// — and the brace group itself is screened at BIND, where the diagnostic can name the format and the syntax
// rule (COBOLNET2119 for a shape outside the group, COBOLNET2120 for §14.9.28.3 SR4/SR5/SR6). Superset parse,
// bind narrow: the project's standing posture for a general-format shape violation.
performVarying
    : (WITH? TEST (BEFORE | AFTER))?
      VARYING dataReference FROM valueOperand
      (BY valueOperand)?    // BY is optional per COBOL-85 spec (default = 1, §14.9.28.4 GR12)
      UNTIL condition
      performVaryingAfter*
    ;

performVaryingAfter
    : AFTER dataReference FROM valueOperand
      (BY valueOperand)?    // BY is optional per COBOL-85 spec (default = 1, §14.9.28.4 GR12)
      UNTIL condition
    ;

// ==========================================
// IF / END-IF (§14.9.19)
// ==========================================

// ⛔ THE IMPERATIVE IS REQUIRED, AND THE QUANTIFIER IS WHERE THAT RULE LIVES (kb/Work PB396). Rendered from the
// PRINTED page (PDF page 665 / printed folio 635): Format 1 is `IF condition-1 THEN statement-1
// [ ELSE statement-2 ] END-IF` — statement-1 carries NO bracket — and Format 2 stacks
// `{ statement-1 | NEXT SENTENCE }` inside BRACES. §5.2.6.2 gives the omission licence to BRACKETED portions
// only; §5.2.6.3 requires one brace alternative to be "explicitly specified". §14.9.19.3 SR1 says it a second
// way as a cardinality: "Statement-1 and statement-2 represent either one or more imperative statements or a
// conditional statement optionally preceded by one or more imperative statements".
// `statementBlock` IS `statement+`, so the bare reference is the format's own cardinality; the former
// `statementBlock*` restored the zero case and `IF X = 1 END-IF` compiled to an empty C# block in silence.
// The ELSE arm is the same rule: the bracket encloses `ELSE statement-2` as a UNIT, so writing ELSE obliges
// statement-2. Pinned by RequiredImperativeStatementDriftTests (no `statementBlock*` may reappear in any .g4).
// ⚠ DETERMINATION (kb/Work PB396, owner-overturnable): the two formats stay ONE rule, so `END_IF?` also admits
// `IF X = 1 NEXT SENTENCE END-IF` — a Format-2 body under a Format-1 terminator that no single printed format
// shows. It is ACCEPTED, unchanged, and behaves per §14.9.19.4 GR4: the combination is a cross-format latitude
// question, not the cardinality slip this rule fixes, and rejecting it would newly refuse source that eight
// editions of this compiler have compiled. Recorded rather than silently inherited from the quantifier.
ifStatement
    : IF condition THEN?
      statementBlock
      (ELSE statementBlock)?
      END_IF?
    ;

// ==========================================
// EVALUATE / END-EVALUATE (§14.9.13)
// ==========================================

// ⛔ THE FORMAT'S OWN SHAPE, ONE ELEMENT PER LINE (kb/Work PB396; rendered from PDF page 648 / printed folio 618):
//     EVALUATE selection-subject [ ALSO selection-subject ] …
//     { { WHEN selection-object [ ALSO selection-object ] … } … imperative-statement-1 } …
//     [ WHEN OTHER imperative-statement-2 ]
//     [ END-EVALUATE ]
// Two cardinalities follow and BOTH used to be lost to one flattened `evaluateWhenClause+`:
//  1. imperative-statement-1 sits INSIDE the outer brace group, unbracketed ⇒ REQUIRED (§5.2.6.3), and the
//     outer `{ … } …` is itself unbracketed ⇒ at least ONE ordinary WHEN group is required. `EVALUATE X
//     WHEN OTHER …` with no ordinary WHEN is therefore not a spelling of this statement.
//  2. `[ WHEN OTHER imperative-statement-2 ]` is ONE optional phrase that follows the repetition: per §5.2.7
//     the ellipsis applies only to the portion between the matching braces immediately to its left, so WHEN
//     OTHER is outside it — at most once, and only last. Written as a second ALTERNATIVE of the repeated
//     clause it became admissible any number of times at any position, and EvaluateBinder's `other = body`
//     silently discarded every earlier one (§14.9.13.4 GR5 b) is written for exactly one such phrase).
evaluateStatement
    : EVALUATE evaluateSubject (ALSO evaluateSubject)*
      evaluateWhenClause+
      evaluateWhenOther?
      END_EVALUATE?
    ;

// ⛔ condition-1 IS THE ONE `condition` RULE, NOT A HAND-PICKED SUBSET OF IT (kb/Work PB842). §14.9.13.2's
// selection-subject brace prints `condition-1` beside identifier-1 / literal-1 / arithmetic-expression-1 /
// boolean-expression-1 / TRUE / FALSE, and §14.9.13.4 GR3 e) gives it its semantics ("Any selection subject
// specified by condition-1 is assigned a truth value according to the rules for evaluating conditional
// expressions"). This rule used to be `valueOperand (IS? NOT? className)?` — ONE shape of condition (the class
// test) spelled as an operand suffix — so `EVALUATE NOT BW` (§8.8.4.3.2's `[ NOT ] boolean-expression-1`),
// `EVALUATE WS-N > 1`, a sign condition and every combined condition were raw parse errors. The class test is
// now simply one of the forms `condition` already spells (comparisonExpression's className arm — the ONE
// §8.8.4.4 alternative list, kb/Work PB590), bound by the ONE condition binder.
// ALTERNATIVE ORDER IS LOAD-BEARING: a BARE word (`EVALUATE X`, `EVALUATE COND-88`) is both a valueOperand and a
// condition, and ANTLR resolves that ambiguity to the FIRST alternative — the value subject, whose bare-operand
// resolution (EvaluateBinder.ClassifyPair) is what tells a level-88 / switch-status condition-name from a data
// item, exactly as for a bare selection object. `condition` is reached only when the subject has a shape no
// value operand spells (a relational operator, a class/sign test, NOT, AND/OR, a parenthesised condition), so
// no subject that parsed before changes its parse.
// ⛔ boolean-expression-1 (§14.9.13.4 GR3 d) — "Any selection subject in which boolean-expression-1 is specified is
// assigned a boolean value according to the rules for evaluating boolean expressions" — IS an alternative here
// (kb/Work PB1412), behind the SAME `{boolExprAhead()}?` discriminator every other boolean-expression host uses
// (primaryCondition, callArgument, argument, functionArgument …), because booleanExpression's leaf is valueOperand and
// would otherwise claim every bare operand. Placed AFTER valueOperand, so a subject that parsed as a value operand
// before still does (a bare boolean item or literal, a parenthesized one, an identifier), and BEFORE condition, so an
// operator-bearing expression with no relational tail — `EVALUATE A B-AND C WHEN B"1000"` — is Table 15's
// Boolean-expression column rather than a condition held to §8.8.4.3.3 SR1's length-1 rule. A boolean RELATION
// (`EVALUATE A B-AND C = B"1000"`) has a tail the alternative cannot consume and still parses as `condition`.
// An ADDRESS-IDENTIFIER (§8.4.3.1.2 identifier Format 9) IS identifier-1 / identifier-2, and §14.9.13.3 SR7 a) makes
// each subject/object pair "valid operands for comparison … in accordance with 8.8.4.2" — the relation that compares
// data-pointers (kb/Work PB1060). It is the ONE `addressIdentifier` rule PB1021 gave comparisonOperand, so the
// storage forcing and the 2002 introduction gate reach this surface unchanged. ADDRESS heads no valueOperand, so
// the arm is unambiguous; it fell to `condition` before and drew a false COBOLNET2318 "used as a condition".
evaluateSubject
    : booleanLiteral                                     // EVALUATE TRUE / FALSE
    | addressIdentifier                                  // identifier-1 that is an address-identifier (PB1060)
    | valueOperand                                       // identifier-1 / literal-1 / arithmetic-expression-1
    | {boolExprAhead()}? booleanExpression               // boolean-expression-1 (§14.9.13.4 GR3 d)
    | condition                                          // condition-1 (§14.9.13.4 GR3 e)
    ;

// One or more consecutive WHEN phrases share the following imperative (ISO 1989:1985
// 14.8.4): "WHEN a  WHEN b  WHEN c  imperative" executes the imperative if a, b, OR c
// matches. Each phrase is bound to its own match arm over the shared body.
// `statementBlock` (= `statement+`) is imperative-statement-1, unbracketed inside the outer brace group.
evaluateWhenClause
    : evaluateWhenPhrase+ statementBlock
    ;

// `[ WHEN OTHER imperative-statement-2 ]` — ONE optional trailing phrase of evaluateStatement, never a member
// of the repeated clause (§5.2.7). OTHER is a hard keyword (CobolLexer.g4) and is not reachable from
// `cobolWord`, so no selection-object can spell it and the two rules do not compete in prediction.
evaluateWhenOther
    : WHEN OTHER statementBlock
    ;

evaluateWhenPhrase
    : WHEN evaluateWhenGroup (ALSO evaluateWhenGroup)*
    ;

// ⛔ EXACTLY ONE selection-object per position — NOT a list (fix-queue PB45, the open half).
// §14.9.13.2's general format is `{ { WHEN selection-object [ ALSO selection-object ] … } … imperative-statement-1 } …`
// (verified against the PRINTED page, folio 618): selection-objects repeat ONLY through ALSO — which is
// evaluateWhenGroup's own caller — never by juxtaposition. §14.9.13.3 SR2 fixes the count against the subjects
// ("The number of selection objects within each set of selection objects shall be equal to the number of selection
// subjects"), and the transcription's 900-dpi figure note records the object brace as "a single pair of braces
// spanning ten stacked alternatives — exactly one shall be selected".
// ⚠ THIS WAS `evaluateWhenItem+`, AND THE UNLICENSED REPETITION SILENTLY MISCOMPILED A FUNCTION-IDENTIFIER OBJECT.
// `WHEN FUNCTION SQRT(W-Z) > 1` has two readings: the CORRECT one (a relation condition whose left operand is the
// function-identifier) and a PEELED one that takes `FUNCTION SQRT` as a bare zero-argument object and re-reads the
// ARGUMENT parenthesis as a second, parenthesised object `(W-Z) > 1`. The greedy/correct reading cannot consume the
// trailing `> 1` once the item ends, so under `+` it was not viable and only the peel survived — and because
// `valueOperand` precedes `condition` in evaluateWhenItem, ANTLR preferred it. The result was a VALUE object under an
// `EVALUATE TRUE` subject: a clean compile that threw at RUN TIME (and, for an alphanumeric function, a raw parse
// error). `FUNCTION PI > 1` always worked precisely because it has no argument parenthesis to peel.
// ⛔ THE FIX IS THE ARITY, NOT THE ALTERNATIVE ORDER. Putting `condition` before `valueOperand` would also retarget
// `EVALUATE X / WHEN Y` where Y is a level-88 condition-name — a value comparison silently becoming a condition test —
// and §14.9.13.4 Table 15 makes the object's legality depend on the SUBJECT, which no context-free ordering can
// express. With one item the peel is simply not a parse of this rule, and the alternative order is left untouched.
// Pinned by EvaluateSelectionObjectArityDriftTests.
evaluateWhenGroup
    : NOT? evaluateWhenItem
    ;

// ⛔ ALTERNATIVE ORDER IS LOAD-BEARING AND IS NOT THE PLACE TABLE 15 IS DECIDED (see the block above). `valueRange`
// leads because its THRU is what distinguishes it; `valueOperand` precedes `condition` because a BARE word is both
// and only the resolved SYMBOL can say which (EvaluateBinder.ClassifyPair asks, once); `partialExpression` is last
// of the shapes because `className`'s user-word alternative would otherwise claim a bare identifier-2.
// partial-expression-1 (§14.9.13.3 SR5) had NO alternative here at all, so `EVALUATE WS-N WHEN > 5` — the first
// shape SR5's own list names and the ordinary way to write a threshold arm — was a raw parse error, and FIVE rules
// (SR5, SR7 d), SR8, SR6 e) and §14.9.13.4 GR4 a) 2.) had no code site, with Table 15's Partial-expression row a
// permanently dead lookup (kb/Work PB398).
// boolean-expression-2 (§14.9.13.2's `[ NOT ] boolean-expression-2`, kb/Work PB1412) sits between valueOperand and
// condition for the subject rule's reason: an operator-bearing object with no relational tail is Table 15's
// Boolean-expression ROW, not condition-2. `valueRange` leads and is untouched (a boolean operand may not end a
// range, SR4).
evaluateWhenItem
    : valueRange                         // WHEN A THRU N, WHEN 1 THRU 10, WHEN "A" THRU "M" IN ALPH
    | addressIdentifier                  // identifier-2 that is an address-identifier (kb/Work PB1060, see evaluateSubject)
    | valueOperand                       // single value: "A", 1, VAR
    | {boolExprAhead()}? booleanExpression   // WHEN A B-AND C — boolean-expression-2 (§14.9.13.4 GR4 a) 6.)
    | condition                          // for EVALUATE TRUE / complex WHEN
    | partialExpression                  // WHEN > 5, WHEN NUMERIC, WHEN POSITIVE, WHEN > 5 AND < 10 (§14.9.13.3 SR5)
    | ANY                                // match anything
    ;

// ==========================================
// GO TO (§14.9.17)
// ==========================================

// ⛔ ONE ALTERNATIVE PER PRINTED GENERAL FORMAT — NEVER THEIR UNION (kb/Work PB412). §14.9.17.2 prints exactly
// two formats, rendered from the canonical PDF page 660 / printed folio 630 at 190 dpi because a general-format
// DIAGRAM is load-bearing (CLAUDE.md rule 1):
//     Format 1 (unconditional):  GO TO procedure-name-1
//     Format 2 (depending):      GO TO { procedure-name-1 } … DEPENDING ON identifier-1
// Only GO and DEPENDING are underlined, so TO and ON are optional words (§5.2.3) and GO is required (§5.2.2);
// procedure-name-1 is UNBRACKETED in Format 1 (so exactly one, §5.2.6.2) and carries the ellipsis in Format 2
// (so one or more, §5.2.7). The rule used to be the single union
// `GO TO? procedureName? (procedureName)* (DEPENDING ON? dataReference)?`, whose COMPLEMENT — every shape the
// two formats do not print — was accepted and then answered downstream: `GO TO A B.` reached the Format-1 bind
// arm, which read names[0] and DISCARDED B without a word, and `GO TO DEPENDING ON X.` compiled clean and died
// at run time on a loud stage. §4.2.2 owes a compile-time indication for source no format admits, so the
// narrowing belongs HERE, in the parser, not in a pair of bind-time ifs.
// Arm order is deliberate — ANTLR takes the first matching alternative, and Format 2 is a proper superset of
// Format 1's prefix.
// identifier-1 is an IDENTIFIER, so it admits §8.4.3.1.2 Format 1 (a function-identifier) and Format 4 (an inline
// method invocation) as every sending identifier position does (kb/Work PB1421 — the PB86 PERFORM … TIMES shape:
// the arm was missing and `DEPENDING ON FUNCTION INTEGER(X)` was a parse error). A function-identifier references
// a temporary elementary data item (§8.4.3.2.4 GR1), so §14.9.17.3 SR1's "numeric elementary data item that is an
// integer" is met by an integer function and refused for a numeric one (§8.4.3.2.3 SR11) — at bind, by the ONE
// operand-class screen (OperandPositions.GoToDependingSelector). functionCall FIRST: it is keyword-led.
goToStatement
    : GO TO? procedureName+ DEPENDING ON?                 // §14.9.17.2 Format 2 (depending)
      (functionCall | inlineMethodInvocation | dataReference)
    | GO TO? procedureName                                // §14.9.17.2 Format 1 (unconditional)
    | GO TO?                                              // ANSI X3.23-1985 target-less GO TO — no ISO 2023
                                                          // format prints it; DELETED by ISO 2002 and gated by
                                                          // name (BareGotoRemoved2002) rather than left to a
                                                          // no-viable-alternative parse error, because the
                                                          // four-compilers rule wants the EDITION named.
    ;

// ==========================================
// SEARCH (§14.9.37 — Linear Search)
// ==========================================

searchStatement
    : SEARCH dataReference (VARYING dataReference)?
      searchAtEndClause?
      searchWhenClause+
      END_SEARCH?
    ;

// `{ WHEN condition-1 { imperative-statement-2 | NEXT SENTENCE } } …` (rendered from PDF page 750 / printed
// folio 720): the body is a BRACE alternation ⇒ exactly one shall be specified (§5.2.6.3). NEXT SENTENCE is a
// `statement` to the grammar, so `statementBlock` spells both alternatives, and the binder's ONE arm reader
// (`StatementBinder.BindArm`) is what admits it — and only as the WHOLE arm (kb/Work PB396, PB446).
searchWhenClause
    : WHEN condition statementBlock
    ;

// ⛔ `[ AT END imperative-statement-1 ]` — AND NOTHING ELSE, IN BOTH FORMATS (kb/Work PB446; PDF page 750). This
// rule used to carry a `(NOT AT? END statementBlock)?` arm: the READ/RETURN phrase, which neither SEARCH format
// prints, admitted on BOTH at once because the rule is shared — and refused only downstream, by a binder helper
// that existed solely to name a shape the grammar should never have produced. The shape now arrives as a syntax
// error that CobolErrorStrategy re-codes by name (COBOLNET2269, the one statement-format-shape code).
// ISO 5.2.3: AT is printed WITHOUT an underline in every AT END phrase in the standard — measured across all
// 11 occurrences on pages 600-829, none underlined. It is an OPTIONAL WORD, so `SEARCH … END …` is CONFORMING
// ISO, not a vendor extension.
searchAtEndClause
    : AT? END statementBlock
    ;

// ==========================================
// SEARCH ALL (§14.9.37 — Binary Search)
// ==========================================

// ⛔ FORMAT 2 PRINTS ONE WHEN PHRASE AND NO KEY PHRASE (kb/Work PB446; rendered from PDF page 750 / printed folio
// 720). The ellipsis sits on the `[ AND … ]` BRACKET, not on the WHEN brace (§5.2.7 scopes an ellipsis to the
// delimited portion immediately to its left), so the WHEN phrase occurs exactly once and its AND phrase repeats
// — the `searchAllWhenClause+` this rule carried admitted N WHENs and, nested in an enclosing WHEN-bearing
// statement, captured the enclosing statement's next WHEN as its own. And there is no `KEY IS` phrase in any
// edition's SEARCH format: the key lives in the OCCURS clause (§14.9.37.3 SR7), and the home-grown
// `searchAllKeyPhrase` this rule referenced was read by NO binder, so the phrase was accepted and silently
// discarded. Both shapes now arrive as syntax errors that CobolErrorStrategy re-codes as COBOLNET2269.
searchAllStatement
    : SEARCH ALL dataReference
      searchAtEndClause?
      searchAllWhenClause
      END_SEARCH?
    ;

// Format 2's trailing `{ imperative-statement-2 | NEXT SENTENCE }` is a brace alternation ⇒ required
// (§5.2.6.3; kb/Work PB396). `condition` is the general rule on the standing superset posture: the Format-2
// operand shape (data-name-1 IS EQUAL TO … / condition-name-1, AND-joined) is decomposed and screened ONCE by
// SearchAllFormat2Rules (COBOLNET_CONTROL_FLOW_DESIGN D12), which names the rule a violation breaks.
searchAllWhenClause
    : WHEN condition statementBlock
    ;

// ==========================================
// ALTER (§14.9.2)
// ==========================================

alterStatement
    : ALTER alterEntry+
    ;

alterEntry
    : procedureName TO (PROCEED TO)? procedureName
    ;

// ==========================================
// USE (§14.9.49, declaratives)
// ==========================================

// ⛔ THE OPTIONAL WORDS OF A FORMAT ARE A PROPERTY OF THE FORMAT, not of what the witness corpus happens to
// spell. §14.9.49.2 was measured off the PDF's vector rectangles (`figure_extract.py 804`, confirmed on the
// 600 dpi render of PDF page 804 / printed folio 774, and the transcription's own figure notes agree): in
// Format 1 `USE`, `GLOBAL`, `EXCEPTION`, `ERROR`, `INPUT`, `OUTPUT`, `I-O` and `EXTEND` are underlined while
// `AFTER`, `STANDARD`, `PROCEDURE` and `ON` are NOT; in Formats 3 and 4 only `USE` and the bracketed keywords
// are underlined, and `AFTER` is not. §5.2.3 and §8.3.2.4.3 make every non-underlined uppercase word an
// OPTIONAL word "specified at the user's option with no effect on the semantics of the format", so all five
// are omittable. Until kb/Work PB332 this rule required AFTER and PROCEDURE and relaxed only STANDARD and ON —
// the two words the CCVS witnesses happened to omit. The corpus is a regression net, never the authority.
//
// ⛔ FORMAT ORDER IS LOAD-BEARING, and only because AFTER became optional. `useOnTarget`'s `fileName+` arm
// accepts any cobolWord, and CONDITION / EC / OBJECT / EO are context-sensitive words — §8.10 lists them and
// makes such a word "reserved in the specified language construct or context … otherwise it is treated as a
// user-defined word", which is exactly what `cobolWord` admits — so `USE EXCEPTION CONDITION EC-ALL` can be
// read as Format 1 over two file-names. The
// specific formats are therefore listed FIRST and ANTLR's first-match-wins prediction settles it; Format 1
// still claims `USE EXCEPTION F1` unambiguously, because Formats 3 and 4 demand CONDITION/EC/OBJECT/EO right
// after EXCEPTION and Format 3 demands at least one entry after them.
useStatement
    // Format 2: USE [GLOBAL] BEFORE REPORTING identifier-1 — identifier-1 references a REPORT GROUP (SR9), so
    // the operand is the shared `reportGroupReference` (§8.4.2.2.2 Format 1's `data-name-1
    // [ IN/OF report-name-1 ]`), NOT `procedureName`. The two spell the same tokens, but naming the rule after
    // the thing it references is what lets ONE funnel (ReportGroupResolution) bind this operand and GENERATE's
    // — the qualified spelling and the ambiguity rule are then the same code (kb/Work PB365).
    : USE GLOBAL? BEFORE REPORTING reportGroupReference
    // Format 3 (exception-name, EC model 2002+ — binder-gated): USE [AFTER] {EXCEPTION CONDITION | EC}
    // {exception-name-1 | exception-name-2 {FILE file-name-2}…}… (ISO §14.9.49.2; SR12: EC ≡ EXCEPTION
    // CONDITION). Exception-names are cobolWords — an OPEN set (EC-USER-*, §14.6.13.1.1 / §7.3.25.3 SR2), so
    // name validation (and SR13/SR14) is the binder's, never a token enumeration. (§7.3.25.3 is the TURN
    // compiler directive's syntax rules, which is where the EC-USER-* name shape is fixed.)
    | USE AFTER? (EXCEPTION CONDITION | EC) useEcEntry+
    // Format 4 (ISO §14.9.49.2 — USE [AFTER] {EXCEPTION OBJECT | EO} {class-name | interface-name}, ONE
    // operand; SR15: EO ≡ EXCEPTION OBJECT): the exception-OBJECT declarative selector (GR14 — class-or-
    // subclass / IMPLEMENTS match; GR3: F4 REPLACES the F1/F3 tiers for object raises). EC-OO wave.
    | USE AFTER? (EXCEPTION OBJECT | EO) cobolWord
    // Format 1: USE [GLOBAL] [AFTER] [STANDARD] {EXCEPTION | ERROR} [PROCEDURE] [ON] {file-name+ | INPUT | OUTPUT | I-O | EXTEND}
    | USE GLOBAL? AFTER? STANDARD? (EXCEPTION | ERROR) PROCEDURE? ON? useOnTarget
    // X3.23-1985 debug-module format (obsolete '85 element DELETED by ISO 2002 — the whole facility,
    // DEBUG-* registers included, is absent from the 2023 text): USE FOR DEBUGGING ON {cd-name-1 |
    // [ALL REFERENCES OF] identifier-1 | file-name-1 | procedure-name-1 | ALL PROCEDURES}… .
    // Accepted-inert at 85 (the section is compiled as if comment lines — the conforming posture when
    // WITH DEBUGGING MODE is absent, and our permanently-off object-time switch when present); the
    // EditionValidator flags it COBOLNET0902 ≥2002 (`use-for-debugging-removed-2002`, VCR Table 7
    // row 7.17). ON is written by every CCVS-85 witness but accepted as optional (house optional-word
    // tolerance, cf. Format 1's ON).
    | USE FOR DEBUGGING ON? useDebugTarget+
    ;

// One '85 debug-operand: ALL PROCEDURES / [ALL REFERENCES OF] identifier / a bare name (file-name,
// procedure-name, cd-name, or unqualified identifier — the binder never distinguishes: the whole
// section is inert). dataReference covers the OF/IN-qualified identifier forms (DB201A writes
// `ABC1 OF AB2 OF A1`). The ALL-led alternatives precede the bare form (first-alternative-wins).
useDebugTarget
    : ALL PROCEDURES
    | ALL REFERENCES OF? dataReference
    | dataReference
    ;

// One Format-3 scope entry: an exception-name, optionally file-scoped ({FILE file-name-2}… — each file carries
// its own FILE word per the §14.9.49.2 format figure; SR13 requires an EC-I-O name when FILE is given).
useEcEntry
    : cobolWord (FILE fileName)*
    ;

useOnTarget
    : INPUT                     // all files opened for INPUT
    | OUTPUT                    // all files opened for OUTPUT
    | I_O                       // all files opened for I-O
    | EXTEND                    // all files opened for EXTEND
    | fileName+                 // specific file name(s)
    ;

// ==========================================
// EXIT (§14.9.14)
// ==========================================

exitStatement
    // EXIT PROGRAM [RAISING …] (ISO §14.9.14 Format 2 — the RAISING tail re-raises in the activator; archaic-
    // flagged in 2023, parsed at every edition and binder-gated). The METHOD/FUNCTION forms share the tail
    // syntactically (Formats 3/4); their semantics land with the OO wave.
    : EXIT ( PROGRAM raisingPhrase? | PERFORM CYCLE? | SECTION | PARAGRAPH | METHOD raisingPhrase? | FUNCTION raisingPhrase? )?
    ;

// ==========================================
// STOP (§14.9.42)
// ==========================================

stopStatement
    : STOP RUN (statusPhrase)?   // status phrase introduction-gated at BIND time (StatementBinder.BindStop → Check(StopRunStatus2002))
    | STOP literal                     // STOP literal (Format 2, obsolete)
    ;

// The shared run-unit-termination status phrase (ISO §14.9.42.2 STOP / §14.9.18.2 GOBACK). ONE rule referenced
// by BOTH stopStatement and gobackStatement — annex item 32: "GOBACK … now allows the same status phrase as
// the STOP statement" (feedback_singular_pattern). STOP-status is a 2002 introduction; GOBACK-status is 2023.
// [WITH] {ERROR|NORMAL} [STATUS] [id|lit] — WITH is an OPTIONAL word (§5.2.3, not underlined); exactly one of the
// underlined keywords ERROR/NORMAL is REQUIRED (§14.9.42.2/§14.9.18.2 brace group); the operand is bracketed
// (optional). The former rule wrongly required WITH, bound STATUS to its operand, and admitted a keyword-less
// `STATUS operand` (P13 Wave-I review findings 1/2/3).
// ⛔ `STATUS` IS AN OPTIONAL WORD TOO, and this rule used to make it MANDATORY BEFORE AN OPERAND (kb/Work
// PB407): `GOBACK WITH ERROR 5.` and `STOP RUN WITH ERROR 5.` were both `COBOL0001: no viable alternative at
// input '5'`. Measured on the printed figure at 300 dpi (PDF page 661 / printed folio 631, the GOBACK
// status-phrase) — only `ERROR` and `NORMAL` are underlined, and `WITH` and `STATUS` are not; the STOP figure
// at §14.9.42.2 carries the identical underlining, which is why ONE rule is right for both verbs and why
// fixing it here fixes both (feedback_scan_all_similar).
statusPhrase
    : WITH? (ERROR | NORMAL) STATUS? (dataReference | literal)?
    ;

// ==========================================
// CONTINUE (§14.9.9) / NEXT SENTENCE (a phrase of the IF statement, §14.9.19)
// ==========================================

// CONTINUE [AFTER arithmetic-expression-1 SECONDS] (ISO §14.9.9). Plain CONTINUE is a 1985-continuous no-op;
// the AFTER … SECONDS timed-pause phrase is a COBOL-2023 addition (introduction-gated on the phrase, not the verb).
continueStatement
    : CONTINUE (AFTER arithmeticExpression SECONDS)?
    ;

nextSentenceStatement
    : NEXT SENTENCE
    ;
