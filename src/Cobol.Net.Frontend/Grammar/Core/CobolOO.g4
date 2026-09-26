// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Object-Orientation grammar (COBOL-2002, ISO §11.2 identification division structure, §11.3 CLASS-ID,
// §11.7 METHOD-ID, §11.8 OBJECT + §14.9.23 INVOKE). This is a version-factored
// fragment imported by CobolParserCore.g4 — OO rule BODIES live here; the minimal {is2002()}?-gated HOOK alternatives
// (classDefinition in compilationGroup, invokeStatement in statement, objectReferenceUsage in usageKeyword) are added
// to the core rules they extend. Keeps 2002 OO out of the COBOL-85 base (see memory feedback_grammar_version_factoring).
// Imported (no options block); shares CobolLexer's tokens and references core rules (environmentDivision, dataDivision,
// procedureDivision, cobolWord, objectReference, literal, dataReference, arithmeticExpression).

parser grammar CobolOO;

options { tokenVocab = CobolLexer; }

// ── Class definition (ISO §11.2 composition, §12739) ──
// A class compilation unit: CLASS-ID, an optional instance (OBJECT) definition holding instance data + methods, and
// END CLASS. (FACTORY, INHERITS, ENVIRONMENT/REPOSITORY are later slices.)
classDefinition
    : (IDENTIFICATION DIVISION DOT)? classIdParagraph
      optionsParagraph?    // §10.6.1 [options-paragraph] (kb/Work PB135; §11.9.4 GR1 — inherited by the contained definitions)
      environmentDivision?
      factoryParagraph?
      objectParagraph?
      endClassHeader
    ;

// FACTORY definition (ISO §11.4 :13069; order per the §10.6 class-definition format :12745-12748) — factory
// data (DATA DIVISION → the factory singleton's instance fields, brief D11) + factory methods (same
// methodDefinition rule as the OBJECT paragraph, so the whole method machinery reuses verbatim). IMPLEMENTS
// is the INTERFACE-ID slice (deliberately unparsed, matching objectParagraph). END FACTORY carries NO name
// (§10.6 :12760).
factoryParagraph
    : (IDENTIFICATION DIVISION DOT)? FACTORY DOT implementsClause?
      optionsParagraph?    // §10.6.1 (kb/Work PB135)
      environmentDivision?
      dataDivision?
      (PROCEDURE DIVISION DOT methodDefinition*)?
      END FACTORY DOT
    ;

// ⛔ FROM IS AN OPTIONAL WORD, AND NO AUDIT COULD SEE IT (kb/Work PB695 family 3 sibling sweep / PB715): a word
// REQUIRED INSIDE an optional group never becomes an optional-word candidate, because the enclosing `( … )?`
// already excuses it. MEASURED on printed page 294 / folio 264: INHERITS' box 90.94–136.92 carries a rule at
// 92.40–136.05 (95.0% cover) while FROM's box 140.01–168.09 has NO rule in its band — so
// `CLASS-ID. C INHERITS B.` is conforming source (§8.3.2.4.3) that this rule refused with COBOL0001. FROM is
// §8.9-reserved at every edition, so it can never be a className and `FROM?` cannot mis-bind one.
classIdParagraph
    : CLASS_ID DOT className externalizedNamePhrase? (IS? FINAL)? (INHERITS FROM? className+)?
      (USING ooParameterName+)?   // §11.3.2 — a PARAMETERIZED class (kb/Work PB759; see ooParameterName)
      DOT
    ;   // [AS literal-1] sits between object-class-name-1 and [IS FINAL] per the §11.3.2 format (kb/Work PB303); [IS FINAL] precedes INHERITS per the §10.6 format (:12742-12744); a FINAL class shall not be a superclass (§11.3 GR3 — bind-gated 0839).
        // INHERITS repetition PARSES per the §11.3.2 format (superset parse — P3 doctrine); v1 REJECTS 2+ bases
        // LOUDLY at pass-1 (COBOLNET0849; SSOT §18 #18 / A.4.10) — never a bare syntax error, never a silent drop.

className
    : cobolWord
    ;

// §11.3.2 / §11.6.2 `[ USING { parameter-name-1 } … ]` — the FORMAL parameters of a PARAMETERIZED class or
// interface (§9.3.12 / §9.3.13; kb/Work PB759). USING is underlined on BOTH printed pages (folios 264 and 268,
// each measured on its own). Its OWN rule, never `className`/`interfaceName`: those rules' positional accessors
// (`className(0)` = the class, `className().Skip(1)` = the INHERITS bases; `interfaceName()` = name, INHERITS,
// END INTERFACE) are read by OoClassTable and OoRepositoryScope, and a parameter-name is neither. A
// parameterized definition is a SKELETON: it binds and emits nothing of its own; each REPOSITORY `EXPANDS`
// phrase creates a class or interface from it (Oo/OoExpansion.cs, §12.3.8.4 GR5/GR8).
ooParameterName
    : cobolWord
    ;

// OBJECT (instance) definition — instance data (DATA DIVISION) + instance methods (in the PROCEDURE DIVISION).
// IMPLEMENTS rides the paragraph HEADER with its OWN trailing period (§11.8.2 :13305; instance-definition
// format :12765) — never the CLASS-ID (the dead sketch put it there; spec-wrong).
objectParagraph
    : (IDENTIFICATION DIVISION DOT)? OBJECT DOT implementsClause?
      optionsParagraph?    // §10.6.1 (kb/Work PB135)
      environmentDivision?
      dataDivision?
      (PROCEDURE DIVISION DOT methodDefinition*)?
      END OBJECT DOT
    ;

implementsClause
    : IMPLEMENTS interfaceName+ DOT
    ;

interfaceName
    : cobolWord
    ;

// INTERFACE definition (§11.6 :13157; §10.6 :12783-12796 — NO data division at the interface level; methods
// are PROTOTYPES: header + optional LINKAGE-only data division, no procedure body — enforced at pass-1,
// COBOLNET0840). INHERITS repetition is SUPPORTED (C# interface lists are native — the deliberate asymmetry
// with the class-side single-inheritance restriction, SSOT §18.18).
interfaceDefinition
    : (IDENTIFICATION DIVISION DOT)? INTERFACE_ID DOT interfaceName
      // The INTERFACE-ID paragraph general format (§11.6.2) prints [AS literal-1] between interface-name-1
      // and the INHERITS clause; kb/Work PB303.
      externalizedNamePhrase?
      // FROM is an optional word here too — measured on printed page 298 / folio 268, where INHERITS carries a
      // rule at 95.0% cover and FROM's box 139.98–168.06 carries none (§8.3.2.4.3; the same sweep that fixed
      // classIdParagraph above — one rule, two sites, both measured rather than assumed from the other).
      (INHERITS FROM? interfaceName+)?
      (USING ooParameterName+)?   // §11.6.2 — a PARAMETERIZED interface (kb/Work PB759; see ooParameterName)
      DOT
      optionsParagraph?    // §10.6.1 (kb/Work PB135; prototypes bind no bodies — the parse is the obligation)
      environmentDivision?
      (PROCEDURE DIVISION DOT methodDefinition*)?
      END INTERFACE interfaceName DOT
    ;

// METHOD-ID … END METHOD — a method definition (its own DATA/PROCEDURE divisions). ISO §11.7 / §12798.
methodDefinition
    : (IDENTIFICATION DIVISION DOT)? METHOD_ID DOT
      ( methodName externalizedNamePhrase? | methodPropertySelector )   // §11.7.2 prints [AS literal-1] on the method-name-1 ARM ONLY — a GET/SET PROPERTY method's name is implementor-defined (§11.7.4 GR1a), so it has none (kb/Work PB303)
      OVERRIDE? (IS? FINAL)? DOT
      optionsParagraph?    // §10.6.1 [options-paragraph] in the method skeleton (kb/Work PB135; overrides per §11.9.4 GR1)
      environmentDivision?
      dataDivision?
      procedureDivision?
      END METHOD methodName? DOT
    ;   // the ONLY ISO method attributes: [OVERRIDE] [IS FINAL] (§10.6 :12798-12821; Spec corrections #4 — ABSTRACT is NOT ISO); SR4a/SR3/FINAL enforced at bind (0837/0838/0839)

methodName
    : cobolWord
    ;

// METHOD-ID. GET|SET PROPERTY prop-name — the explicit property-accessor selector (§10.6 :12810-12814;
// §11.7 SR6/SR7 shape checks are pass-1, COBOLNET0842).
methodPropertySelector
    : (GET | SET) PROPERTY propertyName
    ;

propertyName
    : cobolWord
    ;

endClassHeader
    : END CLASS className DOT
    ;

// ── INVOKE statement (ISO §14.9.23) ──
// INVOKE {class-name | object-ref} "method" [USING …] [RETURNING id]. objectReference (dataReference | NULL_ | SELF |
// SUPER) is the SET-pointer rule in CobolParserCore.g4; a class-name and an object reference are both data-references
// syntactically (the binder distinguishes them by resolved symbol kind).
invokeStatement
    : INVOKE invokeTarget invokeMethodName invokeUsing? invokeReturning? END_INVOKE?
    ;

invokeTarget
    : objectReference
    ;

invokeMethodName
    : literal
    | dataReference
    ;

invokeUsing
    : USING invokeArgument+
    ;

// ⛔ BY CONTENT TAKES AN EXPRESSION, AND OMITTING IT MIS-PARSED RATHER THAN FAILING TO PARSE (fix-queue PB46).
// The §14.9.23.2 general format's BY CONTENT branch admits `arithmetic-expression-1 | boolean-expression-1 |
// identifier-5 | literal-2` — read off the rendered figure, not the prose. With only `(dataReference | literal)`
// here, `INVOKE O "M" USING BY CONTENT N + 1` did not fail: `N` matched dataReference and `+ 1` started a
// SECOND invokeArgument, so the statement reported `COBOLNET0828: 2 USING argument(s) for 1 formal parameter(s)`
// — an arity diagnostic about a rule the program does not violate, which sends the reader at the method
// signature. A silent mis-parse is worse than a parse error.
// ⛔ `literal` FIRST, AND `dataReference` IS DELIBERATELY ABSENT — both facts were learned by breaking them.
// `arithmeticExpression` SUBSUMES `dataReference` and every numeric literal, so an alternation listing them
// alongside it does not mean what it reads as (feedback_grammar_precedence: ANTLR takes the first matching
// alternative). A first cut wrote `(nonNumericLiteral | arithmeticExpression)` and silently DESTROYED the
// identifier path: `BY CONTENT A` began matching the expression arm, losing the §14.9.23.3 SR9/SR10 object-data
// rules, the §14.8.2.3.2 conformance check and the ref-mod handling — and `BY CONTENT "XY"` stopped binding at
// all. `literal` is kept because the proven literal arm keys on it; the identifier case is recovered IN THE
// BINDER from a sole-dataReference expression (OoBinder, the SoleDataReference shape ConditionBinder and
// IntrinsicBinder already use), because the grammar cannot express "a reference, unless it is part of an
// expression" without the ambiguity that caused the original defect.
// ⚠ BY VALUE keeps `arithmeticExpression` alone and does NOT gain the boolean arm: the format's BY VALUE branch
// is `arithmetic-expression-1 | identifier-5 | literal-2` — the two phrases genuinely differ, and only
// BY CONTENT carries boolean-expression-1.
// ⛔ `boolean-expression-1` IS A SEPARATE VALUE CHANNEL, NOT A THIRD SPELLING OF THE ARITHMETIC ONE (D-B1: a
// '0'/'1' bit-string world that never enters the numeric or DISPLAY channels), so it takes the proven
// {boolExprAhead()}? gate — the same predicate `primaryCondition` and `compileTimeOperand` use. Unguarded it is
// AMBIGUOUS with the arithmetic alternative, because `booleanExpression`'s leaf is `valueOperand`, which is
// `arithmeticExpression | nonNumericLiteral` — i.e. it matches every operand the other two arms match.
// ⚠ THE PREDICATE'S SCAN IS CONDITION-SHAPED AND CAN OVER-REACH HERE, BY DESIGN OF THE SHARED MECHANISM. It runs
// to the statement's period, so in `USING BY CONTENT N + 1 BY CONTENT B1 B-AND B2` the FIRST argument's decision
// already sees the SECOND argument's B-AND and takes this alternative. That is harmless and deliberately left
// harmless: the binder reduces a B-operator-FREE booleanExpression back to its bare `valueOperand` (the same
// UnwrapBareBool reduction BindPrimaryBoolean uses), so the predicate decides only which NODE an operand parses
// into, never what it MEANS. Narrowing the scan to the argument boundary would tighten a SHARED condition
// predicate for a local reason — the DEVLOG-621 lesson — and would buy nothing the reduction does not already
// guarantee.
// ⛔ THREE ARMS, AND PB130 RELAXED ONLY ONE OF THEM (kb/Work PB695 family 3 — the repo's most reproducible
// defect shape: a dispatch with two-or-more arms, one arm fixed). BY is un-underlined in ALL THREE printed
// phrases, so `USING REFERENCE X` and `USING CONTENT X` are conforming source that reached COBOL0001 while
// `USING VALUE N` already compiled. MEASURED on printed page 681 / folio 651, all three occurrences of BY:
// boxes 325.97–336.30, 325.96–336.30 and 325.90–336.24 — NONE of them has a horizontal rule in its band,
// while REFERENCE (96.3%), CONTENT (95.4%), VALUE (92.9%), OMITTED (95.0%), USING (92.8%) and RETURNING
// (96.2%) all carry one. §8.3.2.4.3. The same shape is already spelled `BY?` in CALL's three phrases
// (CobolParserCore.g4 callByReference / callByValue / callByContent) and in the procedure-division header
// (CobolData.g4), so this brings the last copy into line rather than inventing a posture.
// ⚠ NOTHING DOWNSTREAM KEYS ON `BY`: OoBinder.OoBindInvokeArg selects the passing mode from `arg.VALUE()`,
// `arg.REFERENCE()` and `arg.CONTENT()` — the UNDERLINED words — and VersionConformancePass
// .VisitInvokeArgument gates the boolean OPERATORS, not the phrase word. Verified by grep, not assumed.
// ⛔ OMITTED IS A BRACE ALTERNATIVE OUTSIDE THE [BY REFERENCE] BRACKET (kb/Work PB757). §14.9.23.2 prints
// `[ BY REFERENCE ] { identifier-3 | OMITTED }` — measured on printed folio 651: OMITTED underlined (95.0%), and
// the brace sits outside the bracket, so BOTH `USING BY REFERENCE OMITTED` and bare `USING OMITTED` are printed
// spellings (§5.2.6.2 brackets / §5.2.6.3 braces). §14.9.23.3 SR18 legislates the phrase by name. CALL's
// callByReference / callArgument have carried the same pair since PB130; this was the one consumer without it.
// OMITTED is a reserved word (§8.9), so neither arm shadows a data-name.
// ⛔ THE ADDRESS-IDENTIFIER ARM, IN EVERY PHRASE (kb/Work PB1021 — the INVOKE twin of PB239's CALL fix).
// §14.9.23.3 SR9: "Identifier-3 shall be an address-identifier or shall reference a data item defined in the file,
// working-storage, local-storage, or linkage section", and SR19 makes it a SENDING operand. identifier-5 (the
// BY CONTENT / BY VALUE operand) is an identifier too, and §8.4.3.1.2 identifier FORMAT 9 is an identifier, so
// the ONE `addressIdentifier` rule joins all four operand slots — exactly the four CALL's callArgument gives it.
// ADDRESS is reserved and heads no other alternative, so each arm is unambiguous and every spelling that parsed
// before parses identically.
invokeArgument
    // BY optional (kb/Work PB130 — only VALUE is underlined). `literal` is the printed literal-2 of the BY VALUE brace
    // and the carrier of the predefined NULL object reference (§8.4.3.7 — an identifier-5, which `figurativeConstant`
    // spells): without it `USING BY VALUE NULL` was a parse error (kb/Work PB1137). The binder decides it.
    : BY? VALUE (addressIdentifier | arithmeticExpression | literal)
    | BY? REFERENCE (addressIdentifier | dataReference | OMITTED)
    // SELF is identifier-5 (§8.4.3.8 — an identifier format whose only role bar is "shall not be a receiving
    // operand"), so the BY CONTENT brace and the keyword-less form admit it (kb/Work PB1137); NULL rides `literal`.
    | BY? CONTENT (addressIdentifier | SELF | {boolExprAhead()}? booleanExpression | literal | arithmeticExpression)
    | OMITTED
    | addressIdentifier   // §14.9.23.3 SR9 / SR19 — a sending operand whatever the mode
    | SELF
    | dataReference
    | literal
    ;

invokeReturning
    : RETURNING dataReference
    ;

// ── USAGE OBJECT REFERENCE (ISO §13.18.60.2 general format; §13.18.60.4 GR22) ──
// ⛔ THE FORMAT IS A TUPLE, NOT A SCALAR (kb/Work PB389). Rendered from the canonical PDF (printed folio 503 —
// the diagram is load-bearing and the transcription of a stacked bracket is exactly what was mis-read before):
//
//     OBJECT REFERENCE ⎡ interface-name-1                            ⎤
//                      ⎢ [ FACTORY OF ] ACTIVE-CLASS                 ⎥
//                      ⎣ [ FACTORY OF ] object-class-name-1 [ ONLY ] ⎦
//
// ONE bracket pair enclosing THREE stacked alternatives — so at most one is written, and the bare
// `OBJECT REFERENCE` is GR22 b)'s universal object reference. FACTORY, ACTIVE-CLASS and ONLY are UNDERLINED
// (required words of their alternatives); OF is not, so it is an OPTIONAL word (§5.2.3) and
// `OBJECT REFERENCE FACTORY ACTIVE-CLASS` is conforming — the rule required OF until kb/Work PB848's sibling sweep
// (the same REQUIRED-INSIDE-AN-OPTIONAL-GROUP shape as the pointer usages' TO). Four INDEPENDENT axes: kind ×
// FACTORY × ONLY × name.
//
// Until PB389 this rule had three alternatives carrying ONE axis (`FACTORY OF className` / `className` / bare),
// so ACTIVE-CLASS lexed as a user word and drew COBOLNET0813+0901, and a trailing ONLY was a raw COBOL0307.
//
// FACTORY/ONLY vs interface-name-1: the grammar CANNOT separate an interface-name from an object-class-name
// (both are one cobolWord), so the `[FACTORY OF] className [ONLY]` alternative is the SUPERSET parse (P3
// doctrine) and the binder makes the general-format rejection once the name resolves — COBOLNET1925.
// ACTIVE-CLASS is its own alternative because it IS its own token.
// `FACTORY OF?` is the same printed `[ FACTORY OF ]` bracket the PROCEDURE DIVISION header's `raisingTarget`
// carries — PB848's and PB815's sibling sweeps each found it independently.
objectReferenceUsage
    : OBJECT REFERENCE (FACTORY OF?)? ACTIVE_CLASS         // GR22 e) — the active class; SR16 placement checked in the binder
    | OBJECT REFERENCE (FACTORY OF?)? className ONLY?      // GR22 c)/d) — interface-name-1 or object-class-name-1
    | OBJECT REFERENCE                                     // GR22 b) — the UNIVERSAL object reference
    ;

// ── INLINE METHOD INVOCATION — ISO §8.4.3.4, an IDENTIFIER format (§8.4.3.1.2 Format 4) ──
// ⛔ THIS RULE USED TO WEAR THE NAME AND MATCH A DIFFERENT SHAPE (kb/Work PB428). It read
// `inlineMethodInvocationStatement : dataReference LPAREN argumentList? RPAREN` and was dispatched from
// `statement` under {is2023()}? — i.e. `id(args)` as a STATEMENT. The standard defines no such statement
// (§14.9's roster has none, and §8.4.3.4 sits in §8.4.3 *Identifiers*), and the real construct carries the
// §8.7.4 invocation operator, which had no lexer token at all. So the whole of Format 4 was a raw parse error
// in EVERY position, and the misnamed statement shape accepted source the standard does not define.
//
// MEASURED off the canonical PDF (printed page 133 / PDF page 163 — the general-format DIAGRAM is load-bearing
// and `scripts/render-spec-page.py 163` is what settled it, per CLAUDE.md rule 1):
//
//                                        ⎡   ⎧ arithmetic-expression-1 ⎫   ⎤
//     ⎧ object-class-name-1 ⎫            ⎢   ⎪ boolean-expression-1    ⎪   ⎥
//     ⎨                     ⎬ :: literal-1 ⎢ ( ⎨ identifier-2            ⎬ … ) ⎥
//     ⎩ identifier-1        ⎭            ⎢   ⎪ literal-2               ⎪   ⎥
//                                        ⎣   ⎩ OMITTED                 ⎭   ⎦
//
// OMITTED is the ONLY underlined word; `::` and the parentheses are required punctuation; the OUTER bracket
// makes the whole parenthesised argument list optional and the `…` repeats the brace group INSIDE the one pair.
//
// ⛔ THE RECEIVER IS `objectReference`, THE SAME RULE INVOKE'S RECEIVER USES — one activation mechanism, never a
// second (the dispatch's own words). §8.4.3.4.4 GR1 DEFINES this construct as the INVOKE statement it is
// equivalent to, so the two must not be able to disagree about what a receiver is: object-class-name-1 and
// identifier-1 are both one `dataReference` syntactically (the binder partitions them by resolved symbol kind,
// OoNameResolution.Lookup), and SELF/SUPER are §8.4.3.1.2 Format 6 identifiers of class object and therefore
// legal identifier-1s. NULL rides in too and is rejected LOUDLY by the binder per §8.4.3.4.3 SR2 ("neither the
// predefined object reference NULL nor a universal object reference shall be specified") — the P3 superset
// parse, never a general-format rejection dressed as a syntax error.
//
// ⛔ THE `::` SEGMENT REPEATS, because §8.4.3.1.3 SR1 says identifier is defined recursively ("whenever the
// format for an identifier allows another identifier to be specified, that other identifier may be any of the
// formats for an identifier, INCLUDING THE ONE BEING DEFINED"): the temporary an invocation references is
// itself an identifier-1, so `O :: "A" :: "B"` is conforming source. Writing the repetition here is what makes
// the recursion expressible without indirect left recursion through `objectReference`.
//
// ⚠ `refModPart*` mirrors `functionCall`'s tail and is derived, not copied: §8.4.3.1.4 GR1 orders the
// components — (e) the invocation operator applies "the literal method-name with optional arguments … on the
// right to the identifier on the left", THEN (g) "a reference modifier applies to the identifier on the left" —
// and §8.4.3.3.3 names no exclusion for an inline invocation (SR2 constrains a function-identifier, SR3 a
// ref-mod of a ref-mod). The two parenthesised tails stay disjoint on the COLON exactly as they do for
// functionCall, so no predicate is needed.
// ⚠ THE SEGMENT IS ITS OWN RULE so each `::` carries ITS OWN argument list: with the group written inline the
// generated context would expose FLAT `literal()` and `argumentList()` lists, and `A::"M"::"N"(X)` could not
// say which method the one argument belongs to.
inlineMethodInvocation
    : objectReference inlineInvocationSegment+ refModPart*
    ;

inlineInvocationSegment
    : COLONCOLON literal (LPAREN argumentList? RPAREN)?
    ;
