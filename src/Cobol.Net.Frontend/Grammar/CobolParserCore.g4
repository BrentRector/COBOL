// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

parser grammar CobolParserCore;

options {
    tokenVocab = CobolLexer;
    superClass = CobolParserCoreBase;
}

// Virtual token: ZERO rewritten to ZERO_ARITH by token rewriter when
// adjacent to arithmetic operators. Keeps ZERO and ZERO_ARITH in
// completely disjoint grammar rules — zero ambiguity.
tokens { ZERO_ARITH }

import CobolExpressions, CobolData, CobolSpecialNames, CobolReportWriter, CobolIO, CobolControlFlow, CobolOO, CobolScreen, CobolDeclined, CobolWords;

// ==========================================
// CONTEXT-SENSITIVE KEYWORDS
// ==========================================
// The cobolWord rule — tokens that have special meaning in specific contexts but are NOT COBOL-85 reserved
// words, so they may also appear as user-defined names — is GENERATED into the imported grammar
// Grammar/Core/CobolWords.g4 from tests/version-matrix/cobol-words.json (the nameSlot=true rows) by
// scripts/gen-cobol-words.ps1 (rearchitecture PHASE 04, Group A). It is single-sourced with the lexer
// subscript-trigger set (Parsing/CobolLexerWordSet.g.cs) and cross-checked by CobolWordsDriftTests, so the
// three former hand-synced copies (this rule, the lexer _dataNameTokens set, and the §8.9 ReservedWords table)
// can no longer silently desync. Do NOT re-add the rule here: edit cobol-words.json and re-run the generator.

// ==========================================
// ERROR RECOVERY
// ==========================================
//
// Statement-level sync: on parse error, skip to the nearest
// statement boundary (period or END-xxx terminator).
//
// @parser::members {
//     private void syncToStatementBoundary() {
//         while (_input.LA(1) != DOT
//             && !_input.LT(1).getText().startsWith("END-")
//             && _input.LA(1) != EOF) {
//             _input.consume();
//         }
//     }
// }
//
// Error nodes in AST:
//     public override void VisitErrorNode(IErrorNode node)
//     {
//         ast.Add(new ErrorNode(node.GetText(), node.Symbol.Line));
//     }

// --- top level ---

compilationUnit
    : compilationGroup* EOF
    ;

compilationGroup
    : (programUnit | classDefinition | interfaceDefinition)+   // OO/2002 rules live in Core/CobolOO.g4; introduction-gated post-bind by VersionConformancePass ParseArm.VisitClass/InterfaceDefinition (rearch 14g.3)
    ;

programUnit
    : identificationDivision
      environmentDivision?
      dataDivision?
      procedureDivision?
      nestedProgram*
      endProgramHeader?
    ;

nestedProgram
    : identificationDivision
      environmentDivision?
      dataDivision?
      procedureDivision?
      nestedProgram*
      endProgramHeader
    ;

endProgramHeader
    : END (PROGRAM | FUNCTION) programName DOT
    ;

// ==========================================
// IDENTIFICATION DIVISION
// ==========================================

// ISO §11.2.1 prints the division header in BRACKETS — `[ IDENTIFICATION DIVISION. ]` (RENDERED, PDF p293 /
// folio 263) — so a program or function source unit may open directly on its PROGRAM-ID / FUNCTION-ID paragraph,
// exactly as the class, factory, object, interface and method units (Core/CobolOO.g4) already do. X3.23-1985
// REQUIRED the header, so the header-less form is a 2002 relaxation, gated on recognition
// (VersionConformancePass ParseArm.VisitIdentificationDivision, identification-header-optional-2002; kb/Work
// PB829 — `PROGRAM-ID. X.` as a unit's first line was `COBOL0001: unexpected 'PROGRAM-ID'` at every edition).
// Position-safe: PROGRAM-ID and FUNCTION-ID are reserved tokens that open no other construct, so the optional
// header cannot let a unit boundary be mistaken for anything inside the preceding unit.
identificationDivision
    : (IDENTIFICATION DIVISION DOT)? identificationBody
    ;

identificationBody
    : programIdParagraph identificationParagraph*
    | functionIdParagraph identificationParagraph*
    ;

// FUNCTION-ID paragraph (COBOL-2002 user-defined function, ISO §11.5). The function unit is otherwise an
// ordinary source unit (its own ENVIRONMENT/DATA/PROCEDURE DIVISION USING…RETURNING) and is compiled as a
// callable program named after the function; a FUNCTION user-name(args) reference lowers onto it (M2-UDF-1,
// DEVLOG 615). Format 2 — `IS PROTOTYPE` (ISO §11.5 :13127 / §10.6) — is a signature-only prototype unit
// (LINKAGE-only data + a header-only procedure division, §10.6.2 SR4): it registers a signature but emits NO
// body, so a caller resolves a separately-compiled definition across the run unit (M2-UDF-3). The optional tail
// is a unique-leading-token additive change on a LOCAL rule (never a shared core), gated 2002+.
functionIdParagraph
    : FUNCTION_ID DOT programName externalizedNamePhrase? prototypePhrase? DOT  // [AS literal-1] rides BOTH formats (ISO §11.5.2 F1/F2 print it between the name and IS PROTOTYPE); IS PROTOTYPE introduction-gated by VersionConformancePass ParseArm.VisitPrototypePhrase (kb/Work PB894); position-safe (dedicated tail, programName consumes a bare name)
    ;

// ------------------------------------------
// The AS externalized-name phrase
// ------------------------------------------

// ISO §8.3.2.2, last paragraph: "For any externalized user-defined words for which the AS phrase is specified,
// the content of the literal specified in that AS phrase is a name that is externalized to the operating
// environment. The implementor defines the formation and mapping rules of these names." ONE rule for the ONE
// phrase, because the standard prints the identical `[ AS literal-n ]` on every surface that declares or names
// an externalized entity:
//   the PROGRAM-ID paragraph §11.10.2 (Formats 1 and 2) · the FUNCTION-ID paragraph §11.5.2 (Formats 1 and
//   2) · the CLASS-ID paragraph §11.3.2 · the INTERFACE-ID paragraph §11.6.2 · the METHOD-ID paragraph
//   §11.7.2 · the REPOSITORY paragraph's program-specifier §12.3.8.2 (the REPOSITORY paragraph's class,
//   interface, property and user-defined-function specifiers are the remaining sites — they wait on their
//   subsystems, kb/Work PB237).
// `literal` is deliberately wide here; each paragraph's own syntax rule (PROGRAM-ID §11.10.3 SR1,
// FUNCTION-ID §11.5.3 SR1, CLASS-ID §11.3.3 SR1, INTERFACE-ID §11.6.3 SR1, METHOD-ID §11.7.3 SR1 and the
// REPOSITORY paragraph §12.3.8.3 SR2 — the same sentence six times) narrows it at BIND time through the
// single screen `ExternalizedName.Screen`, matching the cancelTarget/callTarget discipline. The COBOL-2002
// introduction is gated post-parse by VersionConformancePass ParseArm.VisitExternalizedNamePhrase
// (externalized-name-as-2002) — a below-2002 use names its edition instead of drawing a bare syntax error.
externalizedNamePhrase
    : AS literal
    ;

// ------------------------------------------
// PROGRAM-ID paragraph
// ------------------------------------------

// ISO §11.10.2 Format 1: PROGRAM-ID. program-name-1 [AS literal-1] [IS {COMMON|INITIAL|RECURSIVE}… PROGRAM].
// IS and the trailing PROGRAM are optional noise words around the attribute list (IC401M writes
// `IC401M IS INITIAL.`); the attribute list itself stays required inside the group.
// ⚠ `AS literal-1` is a phrase of its OWN (externalizedNamePhrase), never an attribute: the figure prints
// it POSITIONALLY between program-name-1 and the attribute group, while the attributes are a §5.2.6.4
// choice-indicator group (any order, each at most once). Folding it into programIdAttribute is what produced
// kb/Work PB303's exact inversion — `AS` fell through dataReferenceAttribute → cobolWord, so 2002+ rejected
// it with COBOLNET0901 (AS is reserved from 2002) while '85 — the ONE edition whose PROGRAM-ID paragraph has
// no AS phrase — accepted it and folded the literal into the program name.
// ⛔ AND §11.10.2 FORMAT 2 (prototype) — `PROGRAM-ID. program-prototype-name-1 [ AS literal-1 ] IS PROTOTYPE .`
// (kb/Work PB894; the §10.6.1 program-prototype source unit prints the same paragraph). It used to be UNWRITABLE:
// this rule offered only the Format-1 attribute group, so `PROGRAM-ID. P IS PROTOTYPE.` drew COBOLNET0901 on the
// reserved word while the byte-identical FUNCTION-ID tail compiled. The two formats are alternatives — IS
// PROTOTYPE never combines with COMMON / INITIAL / RECURSIVE — and the tail is the ONE `prototypePhrase` both
// paragraphs share, so BinderDriver.MakeUnit reads one node for either unit kind.
programIdParagraph
    : PROGRAM_ID DOT programName externalizedNamePhrase? (prototypePhrase | IS? programIdAttributes PROGRAM?)? DOT
    ;

// The `IS PROTOTYPE` tail of §11.10.2 Format 2 and of the FUNCTION-ID paragraph's prototype format — ONE rule for
// the one phrase (IS is not underlined in either printed figure, so it is optional). The COBOL-2002 introductions
// (function-prototype-2002 / program-prototype-2002) are recognition-fired on THIS node by VersionConformancePass
// ParseArm.VisitPrototypePhrase, the parent paragraph picking the row.
prototypePhrase
    : IS? PROTOTYPE
    ;

// §11.10.2 program-name-1 is a user-defined word (§8.3.2.2). The `reservedGatedWord` alternative is the
// DECLARATION home of every reservation-gated word (kb/Work PB693/PB655, the dataName precedent): a gated word
// is never a cobolWord, so without this `PROGRAM-ID. UNLOCK.` at --std 2002 answers a raw COBOL0001 "no
// viable alternative" instead of the targeted COBOLNET0901 that names §8.9 — and where §8.9 leaves the word
// FREE, this match is the declaration the token-level gate retypes the word for. A retyped word is an
// IDENTIFIER, so the two alternatives never compete for one token.
programName
    : cobolWord
    | reservedGatedWord
    ;

programIdAttributes
    : programIdAttribute+
    ;

// The §11.10.2 Format 1 attribute group ONLY. `literalAttribute` / `dataReferenceAttribute` (a bare
// STRINGLIT / INTEGERLIT / cobolWord sink) were DELETED with PB303: no clause of §11.10.2 admits either,
// nothing but MakeUnit's AS-literal hack ever read them, and the cobolWord arm WAS the accidental '85 accept.
programIdAttribute
    : commonProgramAttribute
    ;

commonProgramAttribute
    : INITIAL_
    | COMMON
    | RECURSIVE
    | GLOBAL
    ;

// ------------------------------------------
// Other identification paragraphs
// ------------------------------------------

// ⛔ THE §11.2.1 GENERAL FORMAT IS CLOSED (kb/Work PB829; RENDERED from the printed page — PDF p293 / folio 263):
// `[ IDENTIFICATION DIVISION. ]`, then exactly one of the seven source-unit paragraphs (program-id, function-id,
// class-id, factory, object, method-id, interface-id — each matched by its own source-unit rule, not here), then
// `[ options-paragraph ]`. The AUTHOR / INSTALLATION / DATE-WRITTEN / DATE-COMPILED / SECURITY comment-entry
// paragraphs are the COBOL-85 §11.2.1 list, removed at COBOL-2002 (superset-parsed, removal-gated post-bind);
// REMARKS is the COBOL-74 one. ⚠ Their `~DOT+` / `(IDENTIFIER|STRINGLIT)+` bodies are ALSO unbounded token runs
// and are CORRECT: what those removed paragraphs took was a COMMENT-ENTRY — arbitrary text by definition, which
// is why ISO/IEC 1989:2023 defines no syntax for one anywhere. A sink over a COMMENT-ENTRY is right; a sink over
// a CLAUSE or PARAGRAPH LIST is what this last alternative closes.
identificationParagraph
    : optionsParagraph
    | authorParagraph
    | installationParagraph
    | dateWrittenParagraph
    | dateCompiledParagraph
    | securityParagraph
    | remarksParagraph
    | unrecognizedClause DOT   // ⛔ LAST — refused BY NAME by ClosedFormatPass (COBOLNET1971)
    ;

// OPTIONS paragraph (COBOL-2002, ISO §11.9) — fully parsed into a structured clause tree (the model is consumed
// program-wide; see CobolNet.Binding.OptionsModel / OptionsBinder). Each of the seven clauses begins with a
// distinct keyword token, so `optionsClause+` is LL(1)-clean and order-independent (a superset of the spec's
// fixed clause order). Per §11.9.3 the terminating separator period is present iff at least one clause is given;
// no clause body contains a period, so the loop ends cleanly at the period.
optionsParagraph
    : OPTIONS DOT (optionsClause+ DOT)?
    ;

optionsClause
    : arithmeticClause
    | defaultRoundedClause
    | entryConventionClause
    | floatBinaryClause
    | floatDecimalClause
    | optionsInitializeClause
    | intermediateRoundingClause
    ;

// §11.9.5 — ARITHMETIC IS {NATIVE | STANDARD-BINARY | STANDARD-DECIMAL}. Bare STANDARD is also accepted (a common
// vendor spelling; the CCVS uses `ARITHMETIC IS STANDARD`).
arithmeticClause
    : ARITHMETIC IS? arithmeticMethod
    ;

arithmeticMethod
    : NATIVE
    | STANDARD_BINARY
    | STANDARD_DECIMAL
    | STANDARD
    ;

// §11.9.6 — DEFAULT ROUNDED MODE IS rounding-mode. Reuses the shared 8-mode roundingModeName.
defaultRoundedClause
    : DEFAULT ROUNDED MODE? IS? roundingModeName
    ;

// ⛔ A GENERAL-FORMAT KEYWORD WITH NO LEXER TOKEN, MATCHED BY TEXT (kb/Work PB764) — the twin of
// `reservedGatedWord`. §5.2.2: "Keywords are reserved words or context-sensitive words … required in order to
// select the functionality associated with that keyword"; §5.2.3 makes the uncapitalized ones optional words. A
// word standing where its own general format prints it is a USE OF that word, never the user-defined word or
// system-name §8.3.2.1 rule 1 governs — so a keyword slot may not borrow `cobolWord` (DESIGN-frontend-grammar
// "A KEYWORD SLOT MAY NOT BORROW cobolWord"), whose every occurrence the §8.9 funnel screens as a name.
// Some keywords cannot take a lexer token (LOCALE must stay a bare word inside `LOWER-CASE(x LOCALE …)`, which
// IntrinsicBinder reads by text; the LC_ categories and CLASSIFICATION are §8.10 context-sensitive), so they ride
// this rule instead: IDENTIFIER, never `cobolWord`, so the funnel (`VersionConformancePass.VisitCobolWord`) does
// not meet it BY CONSTRUCTION — no per-slot exemption list exists to forget the next slot. The recognizing
// predicate stays where it always was: at the enclosing clause's left edge (localeClauseAhead,
// classificationAhead, …) for a slot that is ONLY the keyword, or at the left edge of the brace alternative
// (`{wordAhead("NESTED")}? formatWord | cobolWord`) where the general format offers the keyword OR a name.
// A literal-argument predicate, never a rule parameter: a parameterized predicate is context-dependent and ANTLR
// cannot evaluate it while predicting from an enclosing rule (feedback_left_edge_predicates).
// `FormatWordDriftTests` fails when a keyword the parser recognizes by text reaches `cobolWord` instead.
formatWord
    : IDENTIFIER
    ;

// §11.9.7 — ENTRY-CONVENTION IS {COBOL | entry-convention-name}. The value is matched as cobolWord (COBOL or an
// implementor name), text-distinguished in the binder, so COBOL need not be reserved globally.
entryConventionClause
    : ENTRY_CONVENTION IS? entryConventionName
    ;

// { COBOL | entry-convention-name-1 } — COBOL is the format's KEYWORD (a formatWord, kb/Work PB764);
// entry-convention-name-1 is a SYSTEM-NAME (§8.3.2.3.1), which §8.3.2.1 rule 1 forbids a reserved word to be, so it
// stays a cobolWord the §8.9 funnel screens.
entryConventionName
    : {wordAhead("COBOL")}? formatWord
    | cobolWord
    ;

// §11.9.8 — FLOAT-BINARY [DEFAULT] IS {HIGH-ORDER-LEFT | HIGH-ORDER-RIGHT}.
floatBinaryClause
    : FLOAT_BINARY DEFAULT? IS? endiannessPhrase
    ;

// §11.9.9 — FLOAT-DECIMAL [DEFAULT] IS { | encoding-phrase | endianness-phrase | }: the braces enclose CHOICE
// INDICATORS (printed folio 305, confirmed by figure_geometry.py — kb/Work PB1508), so per §5.2.6.4 at least one
// alternative is written and "The alternatives may be specified in any order". That is exactly the USAGE clause's
// group, so the clause reuses its ONE rule, floatFormatPhrase (Core/CobolData.g4), instead of a second, ORDERED
// copy that refused the legal endianness-first spelling with a raw parse error (the two-arm defect PB174 left:
// it fixed the USAGE arm only). The clause is DECLINED whole — COBOLNET2424 (OptionsBinder.DeclineFloatDecimal,
// docs/CONFORMANCE.md §2 row 13; kb/Work R43) — so every legal spelling must reach that warning, and §4.2.6 frees
// the implementation from diagnosing a repeated phrase inside the unsupported syntax.
floatDecimalClause
    : FLOAT_DECIMAL DEFAULT? IS? floatFormatPhrase+
    ;

// encodingPhrase / endiannessPhrase are DEFINED in the imported Core/CobolData.g4, beside the USAGE clause that
// is their third citing site (ISO §13.18.60.2) — one rule, one place, reached here through the import merge.

// §11.9.10 — INITIALIZE {ALL | {LOCAL-STORAGE | SCREEN | WORKING-STORAGE}...} [SECTION]
//            TO {BINARY ZEROES | HIGH-VALUES | literal-1 | LOW-VALUES | SPACES}.
// Named distinctly from the PROCEDURE-DIVISION initializeStatement (disjoint parse contexts — no ambiguity).
// ⛔ TO IS AN OPTIONAL WORD (kb/Work PB695, §5.2.3): printed folio 277's underline census for this format is
// exactly {ALL, BINARY, HIGH-VALUES, INITIALIZE, LOCAL-STORAGE, LOW-VALUES, SCREEN, SPACES, WORKING-STORAGE,
// ZEROES} — SECTION and TO are absent from it. SECTION was already relaxed and TO was not, so
// `INITIALIZE ALL SPACES.` was rejected. The fill words are disjoint from the target words, so the omission
// leaves the clause unambiguous.
optionsInitializeClause
    : INITIALIZE optionsInitializeTarget SECTION? TO? optionsInitializeFill
    ;

optionsInitializeTarget
    : ALL
    | optionsInitializeSection+
    ;

optionsInitializeSection
    : LOCAL_STORAGE
    | SCREEN
    | WORKING_STORAGE
    ;

optionsInitializeFill
    // §11.9.10.2 prints each fill word in ONE spelling — the plural — as a keyword, not as a figurative
    // constant, so no other spelling is admitted (kb/Work PB510; §5.2.2).
    : BINARY ZEROES
    | HIGH_VALUES
    | LOW_VALUES
    | SPACES
    | literal              // literal-1 (one-byte hex-alphanumeric, §11.9.10.3 — checked in the binder)
    ;

// §11.9.11 — INTERMEDIATE ROUNDING IS {NEAREST-AWAY-FROM-ZERO | NEAREST-EVEN | PROHIBITED | TRUNCATION}. A
// SEPARATE 4-mode rule (NOT roundingModeName) so the grammar enforces §11.9.11's restricted set.
intermediateRoundingClause
    : INTERMEDIATE ROUNDING IS? intermediateRoundingMode
    ;

intermediateRoundingMode
    : NEAREST_AWAY_FROM_ZERO
    | NEAREST_EVEN
    | PROHIBITED
    | TRUNCATION
    ;

// AUTHOR.
authorParagraph
    : AUTHOR DOT authorContent? DOT
    ;

authorContent
    : ~DOT+
    ;

// INSTALLATION.
installationParagraph
    : INSTALLATION DOT installationContent? DOT
    ;

installationContent
    : ~DOT+
    ;

// DATE-WRITTEN.
dateWrittenParagraph
    : DATE_WRITTEN DOT dateWrittenContent? DOT
    ;

dateWrittenContent
    : ~DOT+
    ;

// DATE-COMPILED.
dateCompiledParagraph
    : DATE_COMPILED DOT dateCompiledContent? DOT
    ;

dateCompiledContent
    : ~DOT+
    ;

// SECURITY.
securityParagraph
    : SECURITY DOT securityContent? DOT
    ;

securityContent
    : ~DOT+
    ;

// REMARKS. (a COBOL-74 carryover, accepted at 85 for the CCVS and removal-gated at 2002+ — COBOLNET0902.)
// ⛔ THE SIXTH COMMENT-ENTRY BODY, AND IT WAS THE ODD ONE OUT (kb/Work PB829). The five paragraphs above all
// spell their body `~DOT+` with a terminating DOT; this one spelled it `(IDENTIFIER | STRINGLIT)+` with NO
// terminating DOT, so a comment-entry word that happens to be a LEXER TOKEN ended the content early and the
// remainder of the entry fell out of the paragraph. That was invisible only because the §11.2.1 paragraph list
// then ended in a vendor catch-all which swallowed it: `REMARKS. CCVS-STYLE REMARK TEXT.` parsed as the REMARKS
// paragraph "CCVS-STYLE REMARK" plus a generic paragraph "TEXT." — TEXT being a token — and closing that list
// turned a silent mis-parse into a diagnostic on legal '85 source (VersionMatrixTests remarks-removed-2002).
// A comment-entry is arbitrary text by definition, which is why ISO/IEC 1989:2023 defines no syntax for one
// anywhere; `~DOT+` is what that means, and it is now written the same way in all six places.
remarksParagraph
    : REMARKS DOT remarksContent? DOT
    ;

remarksContent
    : ~DOT+
    ;

// ==========================================
// ENVIRONMENT DIVISION
// ==========================================

environmentDivision
    : ENVIRONMENT DIVISION DOT
      configurationSection?
      inputOutputSection?
    ;

// ==========================================
// CONFIGURATION SECTION
// ==========================================

configurationSection
    : CONFIGURATION SECTION DOT configurationParagraph*
    ;

// ⛔ THE §12.3.2 GENERAL FORMAT IS CLOSED (kb/Work PB829; RENDERED from the printed page — PDF p313 / folio 283):
// `CONFIGURATION SECTION.` followed by an optional source-computer-paragraph, object-computer-paragraph,
// special-names-paragraph and repository-paragraph — four bracketed paragraphs, and nothing else. The last
// alternative is the error production, NOT a vendor hook (see Core/CobolExpressions.g4#unrecognizedClause); the
// DOT is written HERE because a PARAGRAPH is period-terminated and the shared production is not.
configurationParagraph
    : sourceComputerParagraph
    | objectComputerParagraph
    | specialNamesParagraph
    | repositoryParagraph
    | unrecognizedClause DOT   // ⛔ LAST — refused BY NAME by ClosedFormatPass (COBOLNET1971)
    ;

// REPOSITORY paragraph (COBOL-2002, ISO §12.3.8) — declares the program prototypes, function prototypes, classes,
// interfaces and properties a source element references, plus the intrinsic-function-names usable without the word
// FUNCTION. Each entry starts with its specifier keyword, so the rule cannot over-run into the next section; an
// optional period after each entry tolerates both the one-period-per-paragraph and period-per-entry styles.
// ⛔ EVERY SPECIFIER THAT PRINTS `[ AS literal-n ]` CARRIES THE ONE `externalizedNamePhrase` (kb/Work PB974; rendered
// PDF p334-335 / folios 304-305 — AS underlined, the phrase bracketed, after the declared name and BEFORE the class and
// interface specifiers' EXPANDS phrase). §12.3.8.4 GR2: "literal-1, literal-2, literal-3, or literal-5 is the
// externalized name by which the class, interface, function, or program, respectively, is known to the operating
// environment", and the property specifier's literal-4 names the property the declared classes implement. The local
// name stays the word the source element writes; what it RESOLVES to is found by the literal — OoRepositoryScope
// (class / interface), BinderDriver.UserFunctionsOf (function), BinderDriver.ProgramPrototypesOf (program) and
// ReferenceResolver.OoTryBindPropertyReference (property). The PROGRAM specifier had it alone (kb/Work PB237) and the
// other four refused `CLASS BOXY AS "R11BOX"` with COBOLNET0901 (AS reserved).
// ⛔ AND THE INTRINSIC SPECIFIER TAKES A LIST: `FUNCTION { intrinsic-function-name-1 } … INTRINSIC` (same page) — one
// name only refused `FUNCTION PI E INTRINSIC` (COBOL0307). The user-defined-function specifier is the one-name form
// WITHOUT INTRINSIC; ALL(*) tells the two apart on the trailing INTRINSIC keyword.
repositoryParagraph
    : REPOSITORY DOT (repositoryEntry DOT?)*
    ;

repositoryEntry
    : FUNCTION ALL INTRINSIC
    | FUNCTION functionName+ INTRINSIC
    | FUNCTION functionName externalizedNamePhrase?   // §12.3.8.2 user-defined-function-specifier [AS literal-5]
    | CLASS className externalizedNamePhrase? expandsPhrase?   // OO (2002): CLASS class-name [AS literal-1] [EXPANDS …] — introduction-gated post-bind by VersionConformancePass ParseArm.VisitRepositoryEntry (rearch 14g.5); className rule in Core/CobolOO.g4
    | INTERFACE interfaceName externalizedNamePhrase? expandsPhrase?   // OO (2002): the interface specifier [AS literal-2] — introduction-gated post-bind by VersionConformancePass ParseArm.VisitRepositoryEntry (rearch 14g.5); position-safe (entry-leading keyword in a closed alt set)
    | PROGRAM programPrototypeName externalizedNamePhrase?   // §12.3.8.2 program-specifier (2002) — kb/Work PB237; introduction-gated post-bind by VersionConformancePass ParseArm.VisitRepositoryEntry; position-safe (entry-leading keyword in a closed alt set — no configuration paragraph, section header or division header begins with the PROGRAM token)
    | PROPERTY propertyName externalizedNamePhrase?     // OO (2002): the property specifier [AS literal-4] — introduction-gated post-bind by VersionConformancePass ParseArm.VisitRepositoryEntry (rearch 14g.5); position-safe (§8.4.3.9.3 SR1)
    ;

// §12.3.8.2 class-specifier / interface-specifier `[ EXPANDS object-class-name-2 USING { object-class-name-3 |
// interface-name-1 } … ]` (the interface twin: interface-name-3 USING { object-class-name-4 | interface-name-4 } …)
// — the EXPANSION of a parameterized class or interface (§12.3.8.4 GR5/GR8, §9.3.12/§9.3.13; kb/Work PB759).
// EXPANDS is a §8.10 CONTEXT-SENSITIVE word ("class-specifier and interface-specifier of the REPOSITORY
// paragraph"), never reserved, so it is read by TEXT at the left edge (expandsAhead) rather than tokenized — the
// LOCALE / STEP discipline. The phrase is its own rule so `repositoryEntry.className()` stays the ONE declared
// name OoRepositoryScope collects: the parameterized name and the actuals are REFERENCES, each of which §12.3.8.3
// SR4/SR7 requires to be declared by its own specifier in the same paragraph. The specifier itself is already
// introduction-gated (RepositoryClass2002 / RepositoryInterface2002), so the phrase needs no gate of its own.
expandsPhrase
    : {expandsAhead()}? cobolWord expandsTarget USING expandsActual+
    ;

expandsTarget
    : cobolWord
    ;

expandsActual
    : cobolWord
    ;

// §12.3.8.2 program-specifier: `PROGRAM program-prototype-name-1 [ AS literal-3 ]` (PDF page 334 rendered — PROGRAM
// and AS underlined, the AS phrase bracketed). program-prototype-name-1 is a user-defined word (§8.4.6.8), so it
// rides `cobolWord` like every sibling name rule; literal-3's §12.3.8.3 SR2 class screen (alphanumeric or national,
// not a figurative constant, not zero-length) is a BIND-time narrowing of the deliberately wide `literal`, matching
// the cancelTarget/callTarget discipline. The phrase itself is the SHARED `externalizedNamePhrase` above.
programPrototypeName
    : cobolWord
    ;

// SOURCE-COMPUTER. [computer-name-1] . (ISO §12.3.5.2 — computer-name-1 is OPTIONAL; §12.3.5.3 SR1: without it the
// second period may be omitted). ⛔ THE FORMAT IS CLOSED (kb/Work PB830; RENDERED from the printed page — PDF p314 /
// folio 284): after the optional name there is NOTHING but the period. The empty paragraph was legal in X3.23-1985
// too; the '85 WITH DEBUGGING MODE clause hung off a REQUIRED name, and is modelled here as its own clause so the
// deleted-2002 gate reads a NODE (VersionConformancePass ParseArm.VisitDebuggingModeClause) — a name-less
// `SOURCE-COMPUTER. WITH DEBUGGING MODE.` stays illegal at every edition ('85 required the name, 2002 deleted the
// clause). Everything else after the name is the error production, refused BY NAME by ClosedFormatPass
// (COBOLNET1970). It used to be `computerAttributes : ~DOT+`, an UNGATED token sink that accepted
// `SOURCE-COMPUTER. IBM-370 WIBBLE WOBBLE.` at every edition in silence.
sourceComputerParagraph
    : SOURCE_COMPUTER DOT ((computerName debuggingModeClause? unrecognizedClause?)? DOT)?
    ;

// X3.23-1985 `WITH DEBUGGING MODE` (the '85 debug facility's compile-time switch; WITH is not underlined). Deleted
// by ISO 2002 — VCR row 7.9, `debugging-mode-removed-2002`. Its presence also decides the USE FOR DEBUGGING posture
// (DataBinder.DebuggingModeDeclared, VCR row 7.17).
debuggingModeClause
    : WITH? DEBUGGING MODE
    ;

// OBJECT-COMPUTER. [computer-name-1] [ | CHARACTER CLASSIFICATION … | PROGRAM COLLATING SEQUENCE … | ]… . (ISO
// §12.3.6.2 — the name is OPTIONAL and the two clauses may follow the period directly, in any order, each at most
// once — the figure notes; SR4: with nothing at all the second period may be omitted). X3.23-1985 hung MEMORY SIZE /
// PROGRAM COLLATING SEQUENCE / SEGMENT-LIMIT off a REQUIRED name — the clauses WITHOUT a name are the 2002
// relaxation, gated on recognition (VersionConformancePass ParseArm.VisitObjectComputerParagraph,
// computer-name-optional-2002 — kb/Work PB78: `OBJECT-COMPUTER. PROGRAM COLLATING SEQUENCE IS REV.` was `unexpected
// 'PROGRAM'`).
// ⛔ THE FORMAT IS CLOSED (kb/Work PB830; RENDERED — PDF p315 / folio 285): the clause list is the two standard
// clauses, plus the two X3.23-1985 clauses ISO 2002 deleted — MEMORY SIZE and SEGMENT-LIMIT, MODELLED as clauses
// so their deleted-2002 gates read a NODE — and then the error production, refused BY NAME by ClosedFormatPass
// (COBOLNET1970). The '85 clauses used to be a `~DOT` token SINK behind the name (`computerAttributes`), which
// swallowed `OBJECT-COMPUTER. IBM-370 WIBBLE WOBBLE.` at every edition AND, because the sink ran to the period,
// REJECTED the '85 format's own printed order `MEMORY SIZE … PROGRAM COLLATING SEQUENCE … SEGMENT-LIMIT …` —
// the sink ate PROGRAM COLLATING SEQUENCE's position and SEGMENT-LIMIT after it had nowhere to go.
// ⛔ THE OPTIONAL computer-name-1 IS GUARDED BY THE CLAUSE LOOKAHEAD (kb/Work PB695). Once CHARACTER and PROGRAM
// became optional words, both clauses can OPEN with an ordinary word (`CLASSIFICATION …`) or with a token the
// name slot would otherwise have to be told apart from, and `(computerName …)?` is greedy: without the guard
// `OBJECT-COMPUTER. CLASSIFICATION IS SYSTEM-DEFAULT.` binds CLASSIFICATION as the computer name and the rest
// is mis-read. The predicate is LEFT-EDGE (it steers the enter/skip decision of the optional group);
// mid-alternative it would let the group be entered and then throw.
objectComputerParagraph
    : OBJECT_COMPUTER DOT (({!objectComputerClauseAhead()}? computerName)? objectComputerClause* DOT)?
    ;

objectComputerClause
    : programCollatingSequenceClause
    | characterClassificationClause
    | memorySizeClause         // X3.23-1985 only — deleted 2002 (VCR row 7.7)
    | segmentLimitClause       // X3.23-1985 only — deleted 2002 (VCR row 7.8)
    | unrecognizedClause       // ⛔ LAST — refused BY NAME by ClosedFormatPass (COBOLNET1970)
    ;

// X3.23-1985 `MEMORY SIZE integer {WORDS | CHARACTERS | MODULES}`. MEMORY, WORDS and MODULES are not lexer tokens
// (none is reserved at 2002+), so the clause is recognized by the TEXT of its words through the parser base's one
// word funnel — the left-edge predicate reads MEMORY followed by the SIZE token, the unit predicate the unit word.
memorySizeClause
    : {memorySizeAhead()}? IDENTIFIER SIZE integerLiteral (CHARACTERS | {memoryUnitAhead()}? IDENTIFIER)
    ;

// X3.23-1985 `SEGMENT-LIMIT IS segment-number` (the Segmentation module; IS is not underlined). Recognized by
// TEXT for the same reason — SEGMENT-LIMIT is no lexer token.
segmentLimitClause
    : {segmentLimitAhead()}? IDENTIFIER IS? integerLiteral
    ;

// CHARACTER CLASSIFICATION {IS locale-phrase-1 [locale-phrase-2] | {FOR ALPHANUMERIC IS locale-phrase-1 | FOR
// NATIONAL IS locale-phrase-2}…} (§12.3.6.2), locale-phrase = locale-name | LOCALE | SYSTEM-DEFAULT | USER-DEFAULT.
// ⛔ PARSED SO IT CAN BE DIAGNOSED, NOT SO IT CAN BE USED — an §A.4.9 item 7 optional-locale element ("OBJECT-COMPUTER
// paragraph, CHARACTER CLASSIFICATION clause"); the binder emits the documented-non-support COBOLNET1518 the LOCALE
// clause carries (kb/Work PB78 — it was swallowed silently by the attribute sink after a name, a parse error without
// one). CLASSIFICATION is not a token (an ordinary word at '85), so the arm is predicated on the word's text after
// the CHARACTER token; the four locale-phrase spellings are all words here — the binder never gets to tell them
// apart, since the clause is rejected whole.
// ⛔ CHARACTER AND FOR ARE OPTIONAL WORDS (kb/Work PB695, §5.2.3): printed folio 285's underline census for
// §12.3.6.2 is exactly {ALPHANUMERIC, CLASSIFICATION, LOCALE, NATIONAL, OBJECT-COMPUTER., SEQUENCE,
// SYSTEM-DEFAULT, USER-DEFAULT} — the rule under `CHARACTER CLASSIFICATION` covers CLASSIFICATION alone, and
// the FOR of `FOR ALPHANUMERIC IS locale-phrase-1` carries none. `CLASSIFICATION IS SYSTEM-DEFAULT.` and
// `CHARACTER CLASSIFICATION NATIONAL IS L2.` are both conforming and were both rejected.
// ⛔ THE PREDICATE MOVED TO THE LEFT EDGE. It used to sit after the CHARACTER token, where it could only be
// asserted once the alternative had already been chosen; with CHARACTER optional the rule opens on an ordinary
// word and the predicate has to STEER prediction, not throw after it (the `feedback_left_edge_predicates`
// shape). classificationAhead() therefore reads the CHARACTER-then-CLASSIFICATION pair or a bare leading
// CLASSIFICATION, and demands the clause actually continue — a lone word before the period is a computer-name.
characterClassificationClause
    : {classificationAhead()}? CHARACTER? formatWord (classificationForPhrase+ | IS? localePhrase localePhrase?)
    ;

classificationForPhrase
    : FOR? (ALPHANUMERIC | NATIONAL) IS? localePhrase
    ;

// §12.3.6.2 locale-phrase-1 / locale-phrase-2: { locale-name-1 | LOCALE | SYSTEM-DEFAULT | USER-DEFAULT } — three
// KEYWORDS (formatWord, kb/Work PB764) and one REFERENCE to a SPECIAL-NAMES locale-name (§12.3.6.3 SR3).
localePhrase
    : {wordAhead("LOCALE", "SYSTEM-DEFAULT", "USER-DEFAULT")}? formatWord
    | cobolWord
    ;

// PROGRAM COLLATING SEQUENCE {IS alphabet-name-1 [alphabet-name-2] | {FOR ALPHANUMERIC IS alphabet-name-1 |
// FOR NATIONAL IS alphabet-name-2}…} (ISO §12.3.6.2). The 85 surface is the single-name IS form; the second
// name and the FOR forms arrived with the national class (2002) — introduction-gated on recognition by
// VersionConformancePass ParseArm.VisitProgramCollatingSequenceClause (program-collating-national-2002).
// collatingForPhrase is the ONE shared FOR-class subrule (CobolIO.g4 — SORT/MERGE reuse it).
// ⛔ PROGRAM IS AN OPTIONAL WORD TOO (kb/Work PB695): folio 285 prints `PROGRAM COLLATING SEQUENCE` with the
// rule under SEQUENCE only, so `SEQUENCE IS REV.` and `COLLATING SEQUENCE IS REV.` are conforming spellings.
// COLLATING was already relaxed and PROGRAM was not — the two-word head was half-measured.
programCollatingSequenceClause
    : PROGRAM? COLLATING? SEQUENCE (collatingForPhrase+ | IS? cobolWord cobolWord?)
    ;

computerName
    : cobolWord
    ;

// ==========================================
// PROCEDURE DIVISION
// ==========================================

procedureDivision
    : PROCEDURE DIVISION usingClause? (returningClause)? (raisingClause)? DOT   // returningClause + raisingClause introduction-gated post-bind by VersionConformancePass ParseArm.VisitReturning/RaisingClause (rearch 14g.4, InMethodDefinition-guarded — program-unit PDs only; this rule is SHARED with method PDs)
      declarativePart*
      sentence*          // §14.4.3: the paragraph-name-OMITTED paragraph — "one or more successive sentences
                         // following the procedure division header or a section header". Legal COBOL with no
                         // paragraph at all; NIST/CCVS never exercises it (it always writes paragraph names),
                         // which is why the GnuCOBOL external corpus was what surfaced it (DEVLOG 931).
      procedureUnit*
    ;

usingClause
    : USING usingParameter (COMMA? usingParameter)*
    ;

// The procedure-division-header using-phrase parameter forms (ISO §14.2.2 :23636):
//   { [BY REFERENCE] { [OPTIONAL] data-name-1 }… | BY VALUE { data-name-1 }… }…
// Parsed FLAT (one parameter per node, the CALL callArgument precedent) — the §14.2.3 GR4 transitivity
// ("both phrases are transitive across the parameters that follow them") is threaded by the binder
// (DataBinder.CallBindLinkage), never encoded structurally. BY VALUE is a COBOL-2002 introduction —
// gated post-bind by VersionConformancePass ParseArm.VisitUsingByValue (pd-header-by-value-2002).
usingParameter
    : usingByReference
    | usingByValue
    | OPTIONAL? dataReference
    ;

usingByReference
    : BY? REFERENCE OPTIONAL? dataReference
    ;

usingByValue
    : BY? VALUE dataReference   // BY is optional (§14.2.1 — only VALUE is underlined; kb/Work PB130)
    ;

returningClause
    : RETURNING dataReference
    ;

dataReferenceList
    : dataReference (COMMA? dataReference)*
    ;

dataReference
    // LINAGE-COUNTER special register (ISO §8.4.3.14): a read-only unsigned integer holding the
    // current line within the page body of a LINAGE file, optionally qualified by file-name when more
    // than one LINAGE file exists. Listed first so the distinct LINAGE_COUNTER token is recognized as
    // the register rather than a data name.
    : LINAGE_COUNTER ((OF | IN) cobolWord)?
    // LINE-COUNTER / PAGE-COUNTER special registers (ISO §8.4.3.15/§13.x): read-only counters the Report
    // Writer Control System maintains per report, optionally qualified by report-name. Listed before the
    // generic data-name alternative so the distinct tokens are recognized as registers, not data names.
    | LINE_COUNTER ((OF | IN) cobolWord)?
    | PAGE_COUNTER ((OF | IN) cobolWord)?
    | cobolWord dataReferenceSuffix*
    ;

dataReferenceSuffix
    : subscriptPart
    | refModPart
    | qualification
    ;

qualification
    : (OF | IN) cobolWord (subscriptPart | refModPart)*
    ;

// subscriptPart uses SUBSCRIPT-mode tokens (entered via LPAREN after IDENTIFIER).
// SUB_RPAREN pops back to default mode. Also handles ref-mod (colon form).
subscriptPart
    : LPAREN subscriptOrRefMod SUB_RPAREN
    ;

// Inside SUBSCRIPT mode: captures all content as a flat sequence of SUBSCRIPT-mode tokens.
// The binding layer interprets the content: SUB_COLON → ref-mod, else → subscript list.
// This avoids the need for the grammar to distinguish subscripts from ref-mod.
// ⛔ THE GROUP MAY BE EMPTY (kb/Work PB969). This one capture also carries the keyword-omitted function-identifier's
// argument list (§8.4.3.2.3 SR2 lets FUNCTION be omitted for a prototype, a function-pointer, a REPOSITORY-declared
// intrinsic), and §8.4.3.2.2 prints that list `[ ( [ {argument-1 | OMITTED} ] … ) ]` — the arguments bracketed INSIDE
// the parentheses, so `F()` is the zero-argument spelling, and for a function-pointer §8.4.3.2.3 SR5 ("If
// function-pointer-name-1 is specified, the parentheses shall be specified") makes it the ONLY one. `subToken+` refused
// it with COBOL0001 while `F( )` — one SUB_WS token — parsed; an empty group is now that same whitespace-only group. A
// DATA reference with empty parentheses is still illegal (§8.4.2.3.2 writes `( subscript … )`, §8.4.3.3.2 a required
// leftmost-position) and is refused BY NAME in the resolver (ReferenceResolver.ScreenEmptyParentheses), never here:
// only the resolved symbol says whether the word before the parenthesis is a function or a data item.
subscriptOrRefMod
    : subToken*
    ;

// Any token that can appear inside subscript/ref-mod parentheses
subToken
    : SUB_WS
    | SUB_IDENTIFIER
    | SUB_INTEGERLIT
    | SUB_DECIMALLIT
    | FLOATLIT         // 1.5E3 / -1.5E3 in a captured region (kb/Work R17 — the SUBSCRIPT-mode float forms
                       // re-type to the ONE FLOATLIT vocabulary; without this the keyword-omitted
                       // EXP(+1.5E1) failed the OUTER capture with 'no viable alternative')
    | COMMA_FLOATLIT   // 1,5E3 / -1,5E3 — the DECIMAL-POINT IS COMMA float twin (kb/Work PB98)
    | SUB_STRINGLIT
    | SUB_NATLIT       // national literal argument N"…" (ISO §15.50.3 — FUNCTION LENGTH(N"…") etc.)
    | SUB_BOOLLIT      // boolean literal argument B"…"
    | SUB_HEXLIT       // hexadecimal-alphanumeric literal argument X"…" (kb/Work PB1393 — the literal screen's twin)
    | SIGNED_DECIMALLIT
    | SIGNED_INTEGERLIT
    | SUB_PLUS
    | SUB_MINUS
    | SUB_POWER
    | SUB_STAR
    | SUB_SLASH
    | SUB_COMMA
    | SUB_SEMICOLON
    | SUB_COLON
    | SUB_OF
    | SUB_IN
    | SUB_ALL
    | SUB_LPAREN subToken* SUB_RPAREN                                  // nested parens — `MAX(RANDOM() A)` (§8.4.3.2.3 SR6's NOTE)
    ;

// refModPart for non-identifier context (default mode)
// ⛔ BOTH PAREN FLAVOURS, AND THE FNARG ONE IS NOT AN OVERSIGHT (fix-queue PB48). The lexer types a '(' that
// immediately follows `FUNCTION <name>` as FNARG_LPAREN, but §8.4.3.2.3 SR6 hands that paren to the argument list
// only "if a function's definition PERMITS arguments" — a CATALOG question no lexer or grammar can answer. So for
// a zero-argument function the very same token is the reference modifier: `FUNCTION CURRENT-DATE (1:8)` is the
// standard's own shape (D.14.3.6) and `FUNCTION PI (1:2)` is the SR2 negative case. Accepting either flavour here
// keeps the two alternatives disjoint on the COLON exactly as before — the superset parse — and leaves SR6's
// catalog half to IntrinsicBinder, which reads `functionCall.FNARG_LPAREN()` (a DIRECT child, so an argument list
// that actually parsed) to tell `FUNCTION RANDOM (1:4)` from `FUNCTION UPPER-CASE("x") (1:2)`.
refModPart
    : (LPAREN | FNARG_LPAREN) refModSpec (RPAREN | FNARG_RPAREN)
    ;

refModSpec
    : arithmeticExpression COLON arithmeticExpression?
    ;

// COBOL-85 §5.3: subscript list using SUBSCRIPT-mode tokens.
// Whitespace (SUB_WS) separates subscripts; commas are optional.
subscriptList
    : SUB_WS? subscriptEntry ( (SUB_WS+ | SUB_WS* SUB_COMMA SUB_WS*) subscriptEntry )* SUB_WS?
    ;

// Each subscript is one of the three COBOL-85 forms
subscriptEntry
    : SIGNED_INTEGERLIT                                                // +8, -3, +1
    | SUB_INTEGERLIT                                                   // 1, 10, 300
    | SUB_ALL                                                          // ALL
    | SUB_IDENTIFIER subscriptQualification* relativeOffset?           // W-2, INDEX1 + 2
    ;

// Qualification inside subscript: data-name OF/IN qualifier
subscriptQualification
    : SUB_WS? (SUB_OF | SUB_IN) SUB_WS? SUB_IDENTIFIER
    ;

// Relative subscript offset: {+|-} unsigned-integer
// The + or - is separated by whitespace from the data-name,
// distinguishing it from SIGNED_INTEGERLIT where sign is adjacent.
relativeOffset
    : SUB_WS (SUB_PLUS | SUB_MINUS) SUB_WS SUB_INTEGERLIT
    ;

fileName
    : cobolWord
    ;

// ==========================================
// DECLARATIVES
// ==========================================

declarativePart
    : DECLARATIVES DOT declarativeSection+ END DECLARATIVES DOT
    ;

// The optional integer after SECTION is the X3.23-1985 segment-number (Segmentation module, 0–99;
// ≥50 = independent segment) — an obsolete '85 element DELETED by ISO 2002. Parsed at every edition
// (accepted-inert at 85: all segments resident, a conforming posture); the EditionValidator flags it
// COBOLNET0902 ≥2002 (`segment-numbers-removed-2002`, VCR Table 7 row 7.18). The companion
// SEGMENT-LIMIT clause is row 7.8 (gated at segmentLimitClause — kb/Work PB830).
declarativeSection
    : sectionName SECTION integerLiteral? DOT sentence* declarativeParagraph*
    ;

declarativeParagraph
    : paragraphName DOT sentence*
    ;

// ==========================================
// MAIN PROCEDURE BODY
// ==========================================

// One COBOL sentence: one or more statements, terminated by a period.
// The period is the ONLY place DOT appears in procedure body.
sentence
    : statement+ DOT
    ;

procedureUnit
    : sectionDefinition
    | paragraphDefinition
    ;

// SECTION integerLiteral? — the '85 segment-number; see the declarativeSection note (VCR Table 7 row 7.18).
sectionDefinition
    : sectionName SECTION integerLiteral? DOT
      sentence*          // §14.4.3 — same paragraph-name-omitted form after a SECTION header
      paragraphDefinition*
    ;

// §14.4.2/§14.4.3 section-name-1 / paragraph-name-1 are user-defined words (§8.3.2.2), and BOTH are DEFINITION
// slots — the two IsProvableUserWordPosition procedure-name positions. The `reservedGatedWord` alternative is
// the declaration re-admission the reservation gate needs (kb/Work PB693, the dataName precedent): a
// §8.9-reserved word is gated OUT of cobolWord at the editions that reserve it, so without this `BIT SECTION.`
// at --std 2002 answers a raw COBOL0001 instead of the targeted COBOLNET0901 that names §8.9.
// ⛔ IT SITS ON THE DEFINITION WRAPPERS, NOT ON `procedureName`: procedureName is shared with every REFERENCE
// (GO TO … DEPENDING's procedure-name LIST above all), and re-admitting the word there would let that list
// absorb the next statement's leading keyword again — the very defect PB693 fixes. Consumers read these
// contexts with GetText(), so the alternative costs no binder change.
sectionName
    : procedureName
    | reservedGatedWord
    ;

paragraphDefinition
    : paragraphName DOT sentence*
    ;

paragraphName
    : {IsAtLineStart()}? procedureName
    | {IsAtLineStart()}? reservedGatedWord
    ;

procedureName
    : (cobolWord | INTEGERLIT)
      ((OF | IN) (cobolWord | INTEGERLIT))?
    ;

// ==========================================
// STATEMENT DISPATCHER
// ==========================================

statement
    : acceptStatement
    | addStatement
    | alterStatement
    | useStatement
    | callStatement
    | entryStatement
    | enterStatement
    | cancelStatement
    | closeStatement
    | computeStatement
    | deleteStatement
    | deleteFileStatement   // introduction-gated at BIND time (StatementBinder.KeyedIo → Check(DeleteFile2023)); disjoint from deleteStatement above on the 2nd token (FILE ∉ cobolWord)
    | allocateStatement
    | freeStatement
    | unlockStatement   // COBOL-2002; parses at all editions (superset), introduction-gated at BIND (BindUnlock → Check(UnlockStatement2002)) — DESIGN-version-conformance-pipeline.md residue migration #5
    | displayStatement
    | divideStatement
    | evaluateStatement
    | exitStatement
    | gobackStatement
    | goToStatement
    | ifStatement
    | initializeStatement
    | inspectStatement
    | mergeStatement
    | moveStatement
    | multiplyStatement
    | openStatement
    | performStatement
    | raiseStatement
    | readStatement
    | releaseStatement
    | resumeStatement
    | returnStatement
    | rewriteStatement
    | searchStatement
    | searchAllStatement
    | setStatement
    | sortStatement
    | startStatement
    | stopStatement
    | stringStatement
    | subtractStatement
    | unstringStatement
    | writeStatement
    | initiateStatement
    | generateStatement
    | terminateStatement
    | suppressStatement   // §14.9.45 — SUPPRESS PRINTING; SR1/GR1 (USE-BEFORE-REPORTING context) enforced at bind
    | invokeStatement   // introduction-gated in the VersionConformancePass parse arm (VisitInvokeStatement → Check(Invoke2002))
    // ── Wave H: RECOGNIZE-AND-NAME the facilities COBOL.NET does not implement. TWO LICENCES, one posture
    //    (kb/Work PB709): RECEIVE / SEND (Annex A.3 item 4) and COMMIT / ROLLBACK (A.3 items 6-7) are
    //    PROCESSOR-DEPENDENT, and ISO §4.2.6 ¶3 makes the compile-time WARNING MECHANISM mandatory for them, so
    //    a GENERIC parse error there is a live non-conformance; VALIDATE is Annex A.4.14 and is not in A.3 at
    //    all, so §4.2.7 mandates no warning — naming it is the documented choice docs/CONFORMANCE.md §4 item 3
    //    records, and a generic parse error there is a wrong ANSWER to the user rather than a non-conformance.
    //    Each arm is
    //    KEYWORD-TOKEN-LED and predicated on facilityWord(), which fires only at the editions where §8.9
    //    actually reserves the word — an IDENTIFIER-led arm would poison the ALL(*) DFA (DEVLOG 903). ──
    | {facilityWord("RECEIVE")}?  mcsReceiveStatement
    | {facilityWord("SEND")}?     mcsSendStatement
    | {facilityWord("VALIDATE")}? validateFacilityStatement
    | {facilityWord("COMMIT")}?   commitFacilityStatement
    | {facilityWord("ROLLBACK")}? rollbackFacilityStatement
    | continueStatement
    | nextSentenceStatement
    ;

// ── The unsupported-facility statements (Wave H). The operand tails are parsed for REAL, not swallowed:
//    `statement` is reachable from `statementBlock`, so a `(~DOT)*` swallow would eat END-IF and break every
//    enclosing block — and both MCS formats carry `imperative-statement`s of their own, which a swallow would
//    also consume. Nothing obliges us to diagnose syntax errors WITHIN unsupported syntax — §4.2.6 says so
//    expressly for the A.3 arms, and for the A.4.14 VALIDATE arm the syntax is simply not ours (§4.2.7 /
//    A.4.1) — but nothing excuses us from leaving the surrounding program parseable either. ──

// ISO §14.9.31.2 — RECEIVE FROM data-name-1 GIVING identifier-1 data-name-2
//   [ CONTINUE AFTER { arithmetic-expression-1 SECONDS | MESSAGE RECEIVED } ]
//   [| ON EXCEPTION … |] [| NOT ON EXCEPTION … |] [ END-RECEIVE ]
// RECEIVED is not in §8.9's reserved list and is not a §8.10 context-sensitive word either — the standard
// simply never classifies it (recorded as a P14 Step-0 GAP row); matched as cobolWord, which is safe here.
// ISO 5.2.3 optional word — see the arithmeticOnSizeError note. Measured on page 732: RECEIVE, GIVING,
// CONTINUE, MESSAGE and RECEIVED carry underline rules; FROM carries NONE, so FROM may be omitted.
//
// ⚠ AFTER and SECONDS are NOT made optional here even though page 732 prints them without underlines, because
// THE STANDARD CONTRADICTS ITSELF about them. On page 634 — 14.9.9.2, the CONTINUE statement's OWN defining
// general format — AFTER and SECONDS ARE underlined while CONTINUE is not; page 732 is exactly inverted. One of
// the two pages is a typesetting defect, and page 634 is the defining occurrence, so it wins. It is also the
// only reading that makes sense: with AFTER and SECONDS optional, `CONTINUE 5` would be legal and meaningless.
// Recorded in specs/ISO_COBOL.md at both pages rather than silently resolved.
mcsReceiveStatement
    : RECEIVE FROM? dataReference GIVING dataReference dataReference
      (CONTINUE AFTER (arithmeticExpression SECONDS | MESSAGE cobolWord))?
      mcsExceptionPhrases?
      END_RECEIVE?
    ;

// ISO §14.9.38.2 — Format 1 (to-message-server) and Format 2 (message-server-response), merged: they differ
// only in the RETURNING vs RAISING tail, both optional here, which keeps one rule for one statement.
// ISO 5.2.3 optional word — see the arithmeticOnSizeError note. Measured on page 756, in BOTH send formats:
// SEND, FROM, RETURNING and RAISING carry underline rules; TO carries none, so it may be omitted.
// ⛔ THE SECOND `EXCEPTION` IS AN OPTIONAL WORD HERE TOO — THE THIRD SITE OF ONE RULE (kb/Work PB402, PB407).
// PB407 relaxed `raisingPhrase` to `LAST EXCEPTION?` on the GOBACK and EXIT Format-2 figures and this INLINE
// copy, which is not that rule, kept the falsely-restrictive spelling: `SEND TO D FROM I RAISING LAST` was a
// raw parse error. Measured on the printed §14.9.38.2 Format 2 figure (canonical PDF page 756 / printed folio
// 726, rendered at 190 dpi): RAISING is underlined, the FIRST EXCEPTION is underlined, LAST is underlined, and
// the EXCEPTION after LAST is NOT — the same typesetting as PDF 653 and PDF 661, which is now three pages
// carrying it (§5.2.3 makes a non-underlined uppercase word an optional word).
// ⚠ AND THIS RULE IS NOT `raisingPhrase`, DELIBERATELY: §14.9.38.2 Format 2's brace prints TWO alternatives,
// while §14.9.18.2 / §14.9.14.2 F2 print THREE — they add `identifier-1`, the exception-OBJECT form. Sharing
// the rule would admit `SEND … RAISING <identifier>`, a shape SEND's format does not print, which is the
// over-acceptance defect PB412/PB421 are. The shared FACT — that the second EXCEPTION is optional wherever the
// standard prints `LAST EXCEPTION` — is held across every spelling by
// PrintedFormatAlternativeDriftTests.EveryPrintedLastExceptionSpelling_MakesTheSecondExceptionOptional.
mcsSendStatement
    : SEND TO? (literal | dataReference) FROM dataReference
      (RETURNING dataReference)?
      (RAISING (EXCEPTION cobolWord | LAST EXCEPTION?))?
      mcsExceptionPhrases?
      END_SEND?
    ;

// ISO 5.2.6.4: the printed RECEIVE/SEND figures enclose the ON EXCEPTION / NOT ON EXCEPTION pair in CHOICE
// INDICATORS (verified against the PDF at 700 dpi), so BOTH may be written, each once, IN EITHER ORDER.
// ISO 5.2.3 optional word: ON is omittable here — see the arithmeticOnSizeError note.
mcsExceptionPhrases
    : ON? EXCEPTION statementBlock (NOT ON? EXCEPTION statementBlock)?
    | NOT ON? EXCEPTION statementBlock (ON? EXCEPTION statementBlock)?
    ;

// ISO §14.9.50.2 — VALIDATE { identifier-1 } …
validateFacilityStatement
    : VALIDATE dataReference+
    ;

// A.3 items 6–7: bare-verb transaction statements. Real tokens + real rules rather than refining the §8.9
// reserved-word diagnostic: binding COMMIT as a PARAGRAPH-NAME would split the enclosing paragraph and change
// control flow (PERFORM would stop at the new boundary) — a silent wrong answer under a rule that requires the
// statement to behave as CONTINUE.
commitFacilityStatement
    : COMMIT
    ;

rollbackFacilityStatement
    : ROLLBACK
    ;

// Safety net for vendor extensions — disabled to prevent exponential backtracking
// genericStatement
//     : IDENTIFIER (IDENTIFIER | literal)*
//     ;

// Imperative statement (used by AT END, ON EXCEPTION, etc.)
statementBlock
    : statement+
    ;

// ==========================================
// SHARED ARITHMETIC RULES
// ==========================================

// Unified GIVING-form receiving operand.
// COBOL-85: in any arithmetic GIVING form, the receiving operand may be
// either an dataReference or a literal. One rule, one source of truth.
receivingOperand
    : dataReference
    | literal
    ;

receivingArithmeticOperand
    : dataReference roundedPhrase?
    ;

// ROUNDED [MODE IS rounding-mode] (§14.7.4, COBOL-2002). The MODE phrase selects one of the
// eight ISO rounding modes; bare ROUNDED defaults to NEAREST-AWAY-FROM-ZERO.
roundedPhrase
    : ROUNDED (MODE IS? roundingModeName)?
    ;

roundingModeName
    : AWAY_FROM_ZERO
    | NEAREST_AWAY_FROM_ZERO
    | NEAREST_EVEN
    | NEAREST_TOWARD_ZERO
    | TOWARD_GREATER
    | TOWARD_LESSER
    | PROHIBITED
    | TRUNCATION
    ;

// ISO 5.2.6.4: the ON SIZE ERROR / NOT ON SIZE ERROR pair is enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER. The
// reversed order was rejected until 2026-07-19 (our spec transcription had dropped the bars); the shape below
// matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
// ISO 5.2.3: ON is printed WITHOUT an underline in every one of these figures, so it is an OPTIONAL WORD
// that may be written or omitted. Measured, not assumed: scripts/spec/figure_extract.py reads the
// underline rectangles per word, and ON comes back plain on pages 632 (COMPUTE), 644 (DIVIDE), 703
// (MULTIPLY), 607 and 756 (ON EXCEPTION). callOnExceptionPhrase already had `ON?`; these did not, so the
// grammar contradicted itself and rejected `ADD A TO B SIZE ERROR ...` - legal COBOL.
arithmeticOnSizeError
    : ON? SIZE ERROR statementBlock
      (NOT ON? SIZE ERROR statementBlock)?
    | NOT ON? SIZE ERROR statementBlock
      (ON? SIZE ERROR statementBlock)?
    ;

// ==========================================
// ADD (§14.9.2)
// ==========================================

addStatement
    : ADD (CORRESPONDING | CORR) dataReference TO dataReference roundedPhrase? arithmeticOnSizeError? END_ADD?
    | ADD addOperandList addToPhrase? addGivingPhrase? arithmeticOnSizeError? END_ADD?
    ;

addOperandList
    : addOperand+
    ;

// ⛔ THE FOUR SENDING ARITHMETIC OPERANDS BELOW ARE ONE RULE WRITTEN FOUR TIMES, AND ALL FOUR WERE WRONG
// (fix-queue PB45). ISO §8.4.3.1.2 Format 1 makes a function-identifier an identifier, so every position a
// format writes as `identifier-n | literal-n` in a SENDING role admits one — yet `ADD FUNCTION SQRT(X) TO Y`
// was a PARSE error across the WHOLE arithmetic family (ADD/SUBTRACT/MULTIPLY/DIVIDE, every format), while
// COMPUTE accepted it because its RHS is an arithmeticExpression.
// ⚠ THEY ARE NOT COLLAPSED INTO ONE RULE, AND THAT IS A RECORDED CONSTRAINT RATHER THAN A PREFERENCE: the
// FROZEN legacy compiler (src/CobolSharp.Compiler) shares this grammar and reads `.dataReference()`/`.literal()`
// off AddOperandContext / SubtractOperandContext / MultiplyOperandContext / DivideOperandContext by name, so
// both a collapse and an alias break it — the same freeze that blocks D10 until PHASE 15 CUT 2 deletes legacy.
// The change is therefore ADDITIVE (the standing grammar discipline), and
// ArithmeticSendingOperandDriftTests pins the four alternative sets IDENTICAL so they cannot drift apart while
// they must stay separate. Collapse them to ONE rule at CUT 2.
addOperand
    : literal
    | functionCall
    | inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4 (8.4.3.4) - the Format-1 twin above; kb/Work PB428
    | dataReference
    ;

// §14.9.2.2 (kb/Work PB134): Format 1 prints `TO {identifier-2 [rounded]}…` (receivers); Format 2 prints
// `TO {identifier-2 | literal-2}` — ONE operand in a SENDING role, which §8.4.3.1.2 lets a
// function-identifier fill. Parsed WIDE (the union of the three ADDITIVE alternatives — the frozen legacy
// compiler reads .receivingArithmeticOperand() off this context by name, so no wrapper rule);
// ArithmeticBinder narrows by the GIVING phrase. functionCall sits OUTSIDE the receiving rules so the
// §8.4.3.2.3 SR1 drift guard keeps holding the receiving side clean.
addToPhrase
    : TO receivingArithmeticOperand+
    | TO literal
    | TO functionCall
    | TO inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4; kb/Work PB428
    ;

addGivingPhrase
    : GIVING receivingArithmeticOperand+
    ;

// ==========================================
// SUBTRACT (§14.9.44)
// ==========================================

subtractStatement
    : SUBTRACT (CORRESPONDING | CORR) dataReference FROM dataReference roundedPhrase? arithmeticOnSizeError? END_SUBTRACT?
    | SUBTRACT subtractOperandList subtractFromPhrase? subtractGivingPhrase? arithmeticOnSizeError? END_SUBTRACT?
    ;

subtractOperandList
    : subtractOperand+
    ;

subtractOperand
    : literal
    | functionCall
    | inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4 (8.4.3.4) - the Format-1 twin above; kb/Work PB428
    | dataReference
    ;

subtractFromPhrase
    : FROM subtractFromOperand
    ;

subtractFromOperand
    : receivingArithmeticOperand (receivingArithmeticOperand)*
    | receivingOperand
    | functionCall      // §14.9.44.2 Format 2's sending `FROM {identifier-2 | literal-2}` (§8.4.3.1.2; kb/Work PB134)
    | inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4 (8.4.3.4) - the Format-1 twin above; kb/Work PB428
    ;

subtractGivingPhrase
    : GIVING receivingArithmeticOperand (receivingArithmeticOperand)*
    ;

// ==========================================
// MULTIPLY (§14.9.26)
// ==========================================

multiplyStatement
    : MULTIPLY multiplyOperand BY multiplyByOperand+ multiplyGivingPhrase? arithmeticOnSizeError? END_MULTIPLY?
    ;

multiplyOperand
    : literal
    | functionCall
    | inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4 (8.4.3.4) - the Format-1 twin above; kb/Work PB428
    | dataReference
    ;

multiplyByOperand
    : receivingOperand roundedPhrase?
    | functionCall      // §14.9.26.2 Format 2's sending `BY {identifier-2 | literal-2}` (§8.4.3.1.2; kb/Work PB134)
    | inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4 (8.4.3.4) - the Format-1 twin above; kb/Work PB428
    ;

multiplyGivingPhrase
    : GIVING receivingArithmeticOperand+
    ;

// ==========================================
// DIVIDE (§14.9.12)
// ==========================================

divideStatement
    : DIVIDE divideOperand (divideIntoPhrase | divideByPhrase)
      divideGivingPhrase? divideRemainderPhrase? arithmeticOnSizeError? END_DIVIDE?
    ;

divideOperand
    : literal
    | functionCall
    | inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4 (8.4.3.4) - the Format-1 twin above; kb/Work PB428
    | dataReference
    ;

divideIntoPhrase
    : INTO divideIntoOperand
    ;

divideIntoOperand
    : receivingArithmeticOperand+   // dataReference ROUNDED? (non-GIVING form, multiple targets)
    | literal             // numeric literal (GIVING form only)
    | functionCall        // §14.9.12.2 Format 2's sending `INTO {identifier-2 | literal-2}` (§8.4.3.1.2; kb/Work PB134)
    | inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4 (8.4.3.4) - the Format-1 twin above; kb/Work PB428
    ;

divideByPhrase
    : BY divideOperand
    ;

divideGivingPhrase
    : GIVING receivingArithmeticOperand+
    ;

divideRemainderPhrase
    : REMAINDER dataReference
    ;

// ==========================================
// COMPUTE (§14.9.8)
// ==========================================

computeStatement
    : COMPUTE computeStore+ EQUALS arithmeticExpression computeOnSizeError? END_COMPUTE?          // F1 (§14.9.8)
    | COMPUTE computeStore+ EQUALS booleanExpression computeOnSizeError? END_COMPUTE?  // F2 boolean-compute (§14.9.8 Format 2); superset-parsed (F1 arithmetic is tried first; only a genuine boolean RHS falls here), introduction-gated in VersionConformancePass.VisitComputeStatement → GateBooleanOperators (kb/Work PB157 corrected the stale BindBoolExpr claim here)
    ;

computeStore
    : dataReference roundedPhrase?
    ;

// ISO 5.2.6.4 choice indicators — see the arithmeticOnSizeError note: both phrases, each once, any order.
// ISO 5.2.3 optional word: ON is omittable here — see the arithmeticOnSizeError note.
computeOnSizeError
    : ON? SIZE ERROR statementBlock
      (NOT ON? SIZE ERROR statementBlock)?
    | NOT ON? SIZE ERROR statementBlock
      (ON? SIZE ERROR statementBlock)?
    ;

// ==========================================
// MOVE (§14.9.25)
// ==========================================

moveStatement
    : MOVE (CORRESPONDING | CORR) dataReference TO dataReference
    | MOVE moveSendingOperand moveReceivingPhrase
    ;

moveSendingOperand
    : literal
    | functionCall
    | inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4 (8.4.3.4) - the Format-1 twin above; kb/Work PB428
    | dataReference
    ;

// ⛔ ONE ALTERNATIVE, BECAUSE THE RECEIVING PHRASE HAS ONE PRINTED SHAPE (kb/Work PB421). §14.9.25.2 prints
// exactly two general formats — `MOVE { identifier-1 | literal-1 } TO { identifier-2 } …` and
// `MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4` — and the SECOND is spelled WHOLE by
// `moveStatement`'s first alternative above. This rule used to carry a second alternative
// `(CORRESPONDING | CORR) dataReference TO dataReference`, which composed with `MOVE moveSendingOperand
// moveReceivingPhrase` to admit `MOVE <sending-operand> CORRESPONDING id-3 TO id-4` — a shape NEITHER format
// prints, and one the binder could not build, so `MoveBinder.Bind` fell through to a BoundUnsupported and the
// program COMPILED CLEAN and died at run time claiming a COBOL feature was not yet implemented. §4.2.2's first
// paragraph fixes what an implementation may accept, and a construct no general format prints is not in it.
moveReceivingPhrase
    : TO dataReferenceList
    ;

// ==========================================
// CALL (§14.9.4)
// ==========================================

// §14.9.4.2 Format 2 prints `CALL ⎡{ identifier-1 | literal-1 } AS⎤ { NESTED | program-prototype-name-1 }` — the
// OPTIONAL BRACKET encloses the target brace AND the word AS (PDF page 619 rendered, kb/Work PB237; the earlier
// comment here paraphrased it with the bracket dropped, which is why this rule was believed to need a required
// target). Two consequences:
//   • `CALL … AS …` IS a syntactic Format-2 discriminator when it is written — PB46's "the formats are NOT
//     distinguishable at parse time" is refuted for that spelling, since Format 1 has no AS phrase at all.
//   • but the bracket may be omitted whole, so `CALL <program-prototype-name-1>` — no target operand, no AS — is
//     ALSO Format 2, and it is spelled exactly like Format 1's `CALL identifier-1`. That arm is genuinely
//     SEMANTIC: the binder selects it when the bare word names a program-prototype declared by the REPOSITORY
//     paragraph's program-specifier (§12.3.8.2) and does not resolve as a data item (CallBinder.BindCall). No
//     grammar change is needed for it — `callTarget → dataReference` already carries the word.
callStatement
    : CALL callTarget
      callAsPhrase?
      callUsingPhrase?
      callReturningPhrase?
      callExceptionPhrases?
      END_CALL?

    ;

callTarget
    : literal
    | dataReference
    ;

// `AS NESTED` | `AS program-prototype-name-1`. ⚠ NESTED IS NOT LEXED AS A TOKEN and deliberately so: §8.9 makes
// it a RESERVED word from 2002 (reserved-words.json r85=false, r2002+=true), but this repo enforces reservation
// through the VisitCobolWord funnel on the word's SPELLING, not through tokenization — which is how its sibling
// MODULE-NAME phrase words (CURRENT / ACTIVATING / STACK / TOP-LEVEL) are already handled. Tokenizing it would
// break `FUNCTION MODULE-NAME(NESTED)`, force an entry in fnArgPhraseWord, and make NESTED asymmetric with those
// siblings — all to answer a question the binder answers in one comparison.
// ⛔ BUT THE KEYWORD ARM IS A formatWord (kb/Work PB764): the brace is { NESTED | program-prototype-name-1 }, and
// only the NAME arm is a user-defined word the §8.9 funnel screens (a prototype-name is declared in the REPOSITORY
// paragraph, §14.9.4.3 SR16, so a reserved word there is a real violation). The predicate reads NESTED through the
// >>COBOL-WORDS map, so an UNDEFINE'd NESTED falls to the name arm and an EQUATEd synonym takes the keyword arm.
callAsPhrase
    : AS ({wordAhead("NESTED")}? formatWord | cobolWord)
    ;

callUsingPhrase
    : USING callArgument+
    ;

// kb/Work PB130: Format 2's keyword-less argument may be literal-2, arithmetic-expression-1,
// boolean-expression-1 or OMITTED (all three BY phrases print in plain brackets there) — parsed WIDE on the
// callByContent alternation's own precedent and narrowed in the binder by formatTwo (Format 1's bare
// argument is identifier-2 only). DETERMINATION (whitespace is lexer-skipped, so `N + 1` is ambiguous
// between one expression and the two arguments N and +1 — both legal Format-2 lists): the LIST reading
// wins; parenthesize — `USING (N + 1)` — to force the expression reading (the paren cannot start a
// dataReference or literal, so it selects the arithmeticExpression arm unambiguously). OMITTED joins both the bare list and the BY REFERENCE arm (§14.9.4.2
// Format 2: `[BY REFERENCE] {identifier-2 | OMITTED}`).
// ⛔ THE ADDRESS-IDENTIFIER ARM (kb/Work PB239). §14.9.4.3 SR3 — "Identifier-2 shall reference an
// address-identifier or a data item defined in the file, working-storage, local-storage, or linkage section" —
// and SR4 ("… or if identifier-2 is an address-identifier, identifier-2 is a sending operand") name a second
// operand kind that no CALL alternative could spell: ADDRESS OF existed only inside SET, so `CALL "S" USING
// BY CONTENT ADDRESS OF R` was a bare COBOL0001 on conforming source. It is the §8.4.3.1.2 identifier FORMAT 9
// (`addressIdentifier`, below) and it joins every operand slot identifier-2 / identifier-4 occupies — the bare
// argument, BY REFERENCE, BY CONTENT and BY VALUE (Annex D: address-identifiers "may be passed … with all
// three passing mechanisms"). ADDRESS is a reserved token that heads no other alternative, so each arm is
// unambiguous and every spelling that parsed before parses identically.
callArgument
    : callByReference
    | callByValue
    | callByContent
    | OMITTED
    | addressIdentifier   // §14.9.4.3 SR3/SR4 — a sending operand whatever the mode
    | {boolExprAhead()}? booleanExpression
    | literal
    | dataReference       // bare argument = the transitive mode (GR5) / the formal's mode (GR9)
    | arithmeticExpression
    ;

callByReference
    : BY? REFERENCE (addressIdentifier | dataReference | OMITTED)
    ;

// BY is an OPTIONAL word before VALUE exactly as before REFERENCE/CONTENT — only VALUE is underlined in the
// figure (§5.2.3; kb/Work PB130: `BY VALUE` required the word and rejected `USING VALUE X` on legal source).
// ⛔ THE OPERAND SET IS `arithmetic-expression-1 | identifier-4 | literal-2` (kb/Work PB238). §14.9.4.2
// Format 2's printed figure gives the BY VALUE brace THREE arms, and the single `arithmeticExpression` here
// carried only one and a half of them: the expression spine bottoms out at
// `primaryExpression : numericLiteral | ZERO_ARITH | functionCall | dataReference | ( arithmeticExpression )`,
// so a lone identifier-4 and a NUMERIC literal-2 were subsumed (and thereby MISCLASSIFIED — §14.9.4.4 GR8:
// "An argument that consists merely of a single identifier or literal is regarded as an identifier or literal
// rather than an arithmetic or boolean expression"), while a NON-numeric literal-2 could not be written at
// all. §14.9.4.3 SR23 makes a non-numeric literal-2 illegal under BY VALUE, but a SYNTAX RULE is a
// COMPILER'S verdict to deliver by name, not a hole in the grammar to leave the ANTLR error reporter — so
// the alternative is admitted here and CallBinder reports SR23 with its citation.
// ⚠ THE ORDER IS DELIBERATELY THE OPPOSITE OF `callByContent`'s, and the reason is `literal`'s reach.
// `literal` covers `figurativeConstant`, so putting it first would take `BY VALUE ZERO` away from
// `ZERO_ARITH` — the numeric-zero arm §8.3.3.6.3 SR1a sanctions where a literal is restricted to numeric —
// and re-route a conforming operand through the figurative/character channel. With `arithmeticExpression`
// first, EVERY spelling that parses today parses identically, and the `literal` arm catches exactly the
// residue the expression spine cannot reach: a NON-numeric literal-2. Its identifier-4 and numeric-literal-2
// arms need no alternative at all — the spine subsumes both, and CallBinder RECOVERS them before it binds
// (the GR8 reduction, `Gr8Classify`), which is `callByContent`'s discipline rather than its alternation.
// ⚠ ADDITIVE: `arithmeticExpression()` still exists as a generated accessor and merely returns null on the
// new arm, which the LEGACY binder (which shares this grammar until the P15 cut-over) now tests for.
callByValue
    : BY? VALUE (addressIdentifier | arithmeticExpression | literal)   // introduction-gated at BIND time (StatementBinder.Call → ConstructRegistry.Check(CallByValue2002))
    ;

// ⛔ THE TWO FORMATS' BY CONTENT OPERAND SETS DIFFER, AND ONE RULE CANNOT BE BOTH (fix-queue PB46, CALL half).
// §14.9.4.2 Format 1's BY CONTENT is `{ identifier-2 } …` and NOTHING else; Format 2's is
// `arithmetic-expression-1 | boolean-expression-1 | identifier-4 | literal-2`. This rule serves both, so it is
// parsed WIDE and narrowed in the binder by whether the AS phrase selected Format 2 — the repo's standing
// superset-parse / bind-narrow doctrine. Widening the GRAMMAR alone would trade a rejection of legal Format-2
// source for an acceptance of illegal Format-1 source, which is the trade this item's note correctly refused.
// ⚠ The alternation is the SAME shape invokeArgument uses, for the same reasons: `literal` FIRST because
// `arithmeticExpression` subsumes numeric literals; `dataReference` deliberately ABSENT because
// `arithmeticExpression` subsumes it and the identifier case is recovered in the binder from a sole-dataReference
// expression; and the boolean arm behind `{boolExprAhead()}?` because booleanExpression's leaf is valueOperand
// and an unguarded alternative is ambiguous with the arithmetic one.
// ⚠ `dataReference` STAYS IN THE ALTERNATION, unlike invokeArgument's, and §0's standing caution is why: this
// rule lives in the SHARED `CobolParserCore.g4`, and the LEGACY binder reads `callByContent.dataReference()`.
// Removing it deletes that generated accessor and breaks a compiler that shares this grammar until the P15
// cut-over — the change must be ADDITIVE. Placing it BEFORE `arithmeticExpression` is also what preserves the
// bare-identifier path: ANTLR predicts the alternative that matches the WHOLE operand, so `A` takes the
// dataReference arm and `A + 1` falls through to the expression one.
callByContent
    : BY? CONTENT (addressIdentifier | {boolExprAhead()}? booleanExpression | literal | dataReference | arithmeticExpression)
    ;

callReturningPhrase
    : RETURNING dataReference
    ;

// ISO 5.2.6.4 choice indicators — see the arithmeticOnSizeError note. CALL's ON EXCEPTION / NOT ON EXCEPTION
// pair carries them in the printed general format (Formats 1 and 2), so both may be written, each at most
// once, in either order. Held in ONE container rule rather than two independently-optional slots on
// callStatement, which admitted only the ON-then-NOT order.
callExceptionPhrases
    : callOnExceptionPhrase (callNotOnExceptionPhrase)?
    | callNotOnExceptionPhrase (callOnExceptionPhrase)?
    ;

callOnExceptionPhrase
    : ON? (EXCEPTION | OVERFLOW) statementBlock
    ;

callNotOnExceptionPhrase
    : NOT ON? (EXCEPTION | OVERFLOW) statementBlock
    ;

// ==========================================
// ENTRY (§14.9.14 — alternate entry point)
// ==========================================

entryStatement
    : ENTRY literal usingClause?
    ;

// ==========================================
// ENTER (X3.23-1985 Nucleus — obsolete '85 element DELETED by ISO 2002)
// ==========================================

// ENTER language-name-1 [routine-name-1] — the other-language entry statement. Accepted-inert at 85
// (comment-equivalent when only COBOL is supported — the conforming '85 posture; bound as a no-op);
// the EditionValidator flags it COBOLNET0902 ≥2002 (`enter-removed-2002`, VCR Table 7 row 7.16).
// The operands are deliberately NOT cobolWord: language-name-1 is a SYSTEM-name (`ENTER COBOL.` is the
// canonical '85 switch-back and COBOL is an '85 reserved word — a cobolWord slot would trip the §8.9
// funnel 0901 on conforming source), and routine-name-1 names an external routine, not a word in the
// program's name space. The classic `ENTER LINKAGE.` idiom collides with the LINKAGE SECTION keyword,
// so LINKAGE is admitted explicitly.
enterStatement
    : ENTER enterOperand enterOperand?
    ;

enterOperand
    : IDENTIFIER
    | LINKAGE
    ;

// ==========================================
// CANCEL (§14.9.5)
// ==========================================

cancelStatement
    : CANCEL cancelTarget+
    ;

cancelTarget
    : literal
    | dataReference
    ;

// ==========================================
// SET (§14.9.39 — all forms)
// ==========================================

setStatement
    : setLocaleStatement          // F11/F12 (A.4.9 item 9) — FIRST: predicated on the LOCALE word, so no other form can claim it (kb/Work PB92)
    | setScreenAttributeStatement // F6 (A.4.2 item 24) — predicated on the ATTRIBUTE word, refused by name at bind (COBOLNET1707)
    | setLastExceptionStatement
    | setContentStatement       // F15 (numeric-content) — CONTENT is a reserved token, so no other form can claim it
    | setSwitchStatement
    | setEntryStatement
    | setSizeStatement
    | setToValueStatement
    | setBooleanStatement
    | setFunctionAddressStatement // F8 with the §8.4.3.12 ADDRESS OF FUNCTION sender — before setAddressStatement,
                                  // whose sender arm is `TO ADDRESS OF? dataReference` and would otherwise be tried first
    | setProgramAddressStatement  // F9 with the §8.4.3.13 ADDRESS OF PROGRAM sender — same reason, same place
                                  // (kb/Work PB549). PROGRAM and FUNCTION are distinct reserved tokens, so the
                                  // two address-sender rules cannot claim each other's shape in either order.
    // ⛔ BEFORE setAddressStatement, AND THAT ORDER IS LOAD-BEARING (kb/Work PB450 half 2). Format 7's printed
    // sender identifier-6 may be the predefined address NULL (§14.9.39.3 SR19), and Format 5's may be the
    // predefined object reference NULL (SR9) — `SET X TO NULL` is token-identical in both, and only the
    // RECEIVER's category tells them apart, which is SetFormatSelection's question and not the parser's. With
    // setAddressStatement's receiving list widened to the printed `{ ADDRESS OF data-name-1 | identifier-5 } …`
    // it can now match that shape too, so listing it first would take every `SET obj-ref TO NULL` away from the
    // format-selection path. Listed here, the ordinary shapes route exactly as they did and only the ones
    // carrying an ADDRESS phrase — which setObjectReferenceStatement can never match, ADDRESS being a reserved
    // token — reach Format 7's own rule.
    | setObjectReferenceStatement
    | setAddressStatement
    | setIndexStatement
    ;

// SET LOCALE {LC_ALL | LC_COLLATE | LC_CTYPE | LC_MESSAGES | LC_MONETARY | LC_NUMERIC | LC_TIME | USER-DEFAULT} TO
// {identifier-10 | locale-name-1 | USER-DEFAULT | SYSTEM-DEFAULT} (ISO §14.9.39 Format 11, set-locale) and
// SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT} (Format 12, save-locale).
// IMPLEMENTED since kb/Work PB64 T1 (the binder's SetBinder.BindSetLocale / BindSaveLocale; it used to be parsed only to
// draw COBOLNET1518 — kb/Work PB92: F11 used to bind as a generic SET of a data item named LOCALE, "'LOCALE' is not
// defined" plus false 0901s about the format's own keywords; F12 was `unexpected '.'`). ⚠ THE CATEGORY OPERAND IS A
// SET: the printed format's inner LC_ brace carries CHOICE INDICATORS (§5.2.6.4 — one or more, each at most once, any
// order), hence `cobolWord+` after LOCALE; the binder enforces "each at most once" (the grammar cannot) and the
// USER-DEFAULT-first shape. The TO operand is ONE dataReference split at bind: a locale-name (§14.9.39.3 SR26), a
// data-pointer identifier-10 (SR27), USER-DEFAULT or SYSTEM-DEFAULT. LOCALE / LC_* / USER-DEFAULT / SYSTEM-DEFAULT are
// plain words (reserved 2002+, §8.9 — the predicates are edition-gated like localeClauseAhead), so the arms are
// predicated on the word texts, and every one of them is a `formatWord` (kb/Work PB764) — the format's own
// keywords, which the §8.9 funnel never meets. Only identifier-10 / locale-name-1 in the TO operand are names.
// ⛔ THE PREDICATES ARE LEFT-EDGE. A predicate after SET is not hoisted into prediction — it is asserted only when
// the alternative is entered — and the first cut put them mid-alternative: ANTLR chose this rule for `SET IDX-1 TO
// SUB-1.` (the two arms share the SET … TO … prefix) and every NIST program with a SET died with "rule
// setLocaleStatement failed predicate". Left-edge, a false predicate makes the alternative non-viable and the
// ordinary SET forms are chosen as before.
setLocaleStatement
    : {setLocaleAhead()}? SET formatWord formatWord+ TO setLocaleSource  // F11: LOCALE {category+ | USER-DEFAULT} TO …
    | {saveLocaleAhead()}? SET dataReference TO formatWord formatWord    // F12: identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT}
    ;

// Format 11's TO operand: { identifier-10 | locale-name-1 | USER-DEFAULT | SYSTEM-DEFAULT } — two keywords and two
// names; the names stay ONE dataReference the binder splits (§14.9.39.3 SR26 / SR27).
setLocaleSource
    : {wordAhead("USER-DEFAULT", "SYSTEM-DEFAULT")}? formatWord
    | dataReference
    ;

// ⛔ SET program-pointer+ TO ENTRY {literal | identifier} IS A VENDOR EXTENSION, NOT AN ISO FORMAT — and this
// comment used to say the opposite (kb/Work PB549). It called the shape "ISO §14.9.39 Format 9 with the
// §8.4.3.13 program-address-identifier as the sender", and it is neither: Format 9 prints
// `SET { identifier-7 } … TO identifier-8` and §8.4.3.13.2 prints `ADDRESS OF PROGRAM { … }` — the words
// `TO ENTRY` occur nowhere in ISO/IEC 1989:2023 (a grep of specs/ISO_COBOL.md for "TO ENTRY" returns
// nothing). It is the Micro Focus / IBM spelling, and it is kept because the SEMANTICS behind it are the
// standard's and programs in the field are written to it. The STANDARD spelling is
// setProgramAddressStatement below, which is what §14.9.39 Format 9 + §8.4.3.13 actually admit.
// Listed BEFORE setToValueStatement: ENTRY is a reserved token (not in cobolWord), so no other SET form can
// claim the `TO ENTRY` prefix. A not-locatable program → EC-PROGRAM-NOT-FOUND + NULL (§8.4.3.13.4 GR4, which
// governs the shared semantics whichever surface reaches them).
setEntryStatement
    : SET dataReference+ TO ENTRY (nonNumericLiteral | dataReference)
    ;

// SET LAST EXCEPTION TO OFF (ISO §14.9.39 Format 13, saved-exception; 2002+ — binder-gated): the last
// exception status indicates no exception condition exists (§14.6.13.1.1). Listed FIRST: LAST is a reserved
// token (never a dataReference head), so no other SET form can claim the prefix.
setLastExceptionStatement
    : SET LAST EXCEPTION TO OFF
    ;

// SET CONTENT OF { identifier-14 } … TO { FARTHEST-FROM-ZERO [IN-ARITHMETIC-RANGE] | FLOAT-INFINITY |
// FLOAT-NOT-A-NUMBER | FLOAT-NOT-A-NUMBER-SIGNALING | NEAREST-TO-ZERO [IN-ARITHMETIC-RANGE] } [SIGN {NEGATIVE |
// POSITIVE}] (ISO §14.9.39.2 Format 15, numeric-content; COBOL-2014 — kb/Work PB452). Transcribed against the
// PRINTED general format (folio 732, rendered at 300 dpi): the outer brace of the value list is a PLAIN required
// choice with NO choice indicators — exactly one alternative — and each SIGN phrase is an optional bracket over a
// required NEGATIVE/POSITIVE choice, so it is factored out here rather than repeated five times.
// ⛔ OF IS AN OPTIONAL WORD, on the SAME evidence as Format 7's and Format 16's (kb/Work PB695): the figure
// underlines SET, CONTENT and TO and leaves OF bare, and §5.2.3 makes an unstressed reserved word optional. CONTENT
// is a reserved token at every edition (CALL … BY CONTENT is COBOL-85) and can never head a dataReference, so
// `SET CONTENT X TO …` stays unambiguous against setToValueStatement with or without the word.
// ⚠ The five value words are NOT reserved below 2014, so each is also a cobolWord alternative under the derived
// reservation gate; that costs this rule nothing, because the words appear here in KEYWORD position where the
// alternative is chosen by token type. The FORMAT ITSELF is introduction-gated at BIND (SetBinder.BindSetContent →
// ConstructRegistry.Check(SetNumericContent2014)) — a parse-level gate would answer a 2002 program with a raw
// ANTLR cascade instead of naming the edition.
setContentStatement
    : SET CONTENT OF? dataReference+ TO setContentValue
    ;

setContentValue
    : (FARTHEST_FROM_ZERO | NEAREST_TO_ZERO) IN_ARITHMETIC_RANGE? setContentSign?
    | (FLOAT_INFINITY | FLOAT_NOT_A_NUMBER | FLOAT_NOT_A_NUMBER_SIGNALING) setContentSign?
    ;

setContentSign
    : SIGN (NEGATIVE | POSITIVE)
    ;

// ⛔ THE PRINTED OUTER REPETITION IS A NAMED PHRASE RULE, IN EVERY FORMAT THAT PRINTS ONE (kb/Work PB450).
// §14.9.39.2 gives Format 3 and Format 4 the SAME printed skeleton — an inner brace `{ name } …` and an outer
// brace around the whole `{ name } … TO { keyword | keyword }` unit with a trailing `…` — and the RENDERED
// figure (PDF p760 / folio 730) shows both ellipses for Format 4 exactly as it does for Format 3. The grammar
// transcribed Format 3's outer `…` and DROPPED Format 4's, eighteen lines away in this same file, so
// `SET CF-A TO TRUE CG-A TO FALSE` was `error COBOL0001: unexpected 'TO'` on conforming source.
//   python scripts/spec/cite.py --check 14.9.39.2 "The outer braces enclose the single repeated unit; the trailing"
//   OK  §14.9.39.2   (General formats)  — Format 4's figure note
// ⛔ AND THE REPEATED UNIT IS A RULE, NOT AN INLINE `( … )+` GROUP. An inline group flattens the phrases into
// one list of `dataReference()` plus one list of `TO()`, which forced BOTH binders (the greenfield
// SetAlterBinder.SwitchBindSet and the legacy DataStatementBinder.BindSetSwitch) to re-derive the grouping by
// comparing token indices — two hand-written copies of a structure the parser already knew. One phrase rule per
// printed unit gives each group its own node, so the binder loops over groups and the NEXT format that prints an
// outer ellipsis needs no re-assembler at all (CLAUDE.md rule 5).
// `PrintedFormatAlternativeDriftTests.EverySetStatementRule_NamesItsRepeatedUnitInsteadOfInliningIt` holds it.

// SET {{mnemonic-name-1}… TO {ON | OFF}}… (COBOL-85 §14.9.39 Format 3, switch-setting)
setSwitchStatement
    : SET setSwitchPhrase+
    ;

// ONE printed Format-3 unit: `{ mnemonic-name-1 } … TO { ON | OFF }`.
setSwitchPhrase
    : dataReference+ TO (ON | OFF)
    ;

// SET dataReference+ TO arithmeticExpression (COBOL-85 §14.9.39 Format 1)
// SET [SIZE OF] data-name-3 TO {integer-2 | arithmetic-expression-5} (ISO §14.9.39 Format 16, COBOL-2023):
// set the current length of a DYNAMIC LENGTH elementary item. SIZE OF is the explicit form (SIZE is a reserved
// token, so it cannot head a dataReference — no ambiguity, listed before setToValueStatement). The SIZE-OF-absent
// bare form `SET dyn TO n` parses as setToValueStatement and re-routes at bind via a dynamic-length peek.
// ⛔ OF IS AN OPTIONAL WORD (kb/Work PB695): printed folio 732 sets Format 16 as `SET [ SIZE OF ] data-name-3
// TO …` with underline rules under SET, SIZE and TO and none under OF, so `SET SIZE DYN TO 5` is conforming.
setSizeStatement
    : SET SIZE OF? dataReference TO arithmeticExpression
    ;

setToValueStatement
    : SET dataReference+ TO arithmeticExpression
    ;

// SET {{condition-name-1}… TO {TRUE | FALSE}}… (COBOL-85; §14.9.39 Format 4, condition-setting — the FALSE
// arm is a COBOL-2002 addition, gated recognition-first on the FALSE_ token by VersionConformancePass).
// The outer repetition and the phrase rule are the Format-3 twin's, for the reason written above setSwitchStatement.
setBooleanStatement
    : SET setConditionPhrase+
    ;

// ONE printed Format-4 unit: `{ condition-name-1 } … TO { TRUE | FALSE }`. §14.9.39.4 GR8 — "If multiple
// condition-names are specified, the results are the same as if a separate SET statement had been written for
// each condition-name-1" — is why the binder may flatten the phrases into one ordered list of stores.
setConditionPhrase
    : dataReference+ TO (TRUE_ | FALSE_)
    ;

// ⛔ ONE PRODUCTION FOR THE WHOLE PRINTED FORMAT 7, AND THE REPEATED RECEIVING OPERAND IS A NAMED RULE
// (kb/Work PB450 half 2 — half 1 landed the same discipline on Formats 3 and 4). RENDERED, never inherited
// (specs-private PDF p760 / folio 730 at 300 dpi):
//     SET { ADDRESS OF data-name-1 | identifier-5 } … TO identifier-6
// The brace is a PLAIN required choice — exactly one of the two spellings — and the `…` is OUTSIDE it, so a
// Format-7 statement carries ONE OR MORE receiving operands, each independently either spelling, mixed in any
// order, while the sender identifier-6 is a SINGLE operand outside the repetition.
//   python scripts/spec/cite.py --check 14.9.39.2 "repeats the braced receiving operand"
//   OK  §14.9.39.2   (General formats)  — Format 7's figure note
// ⛔ BEFORE THIS THE RULE WAS TWO FIXED PRODUCTIONS SPLIT ON THE SENDER'S SPELLING, each hard-coding arity one,
// so four cells of the printed cross-product died as a bare COBOL0001 and no syntax rule ever screened them:
// `SET ADDRESS OF B1 ADDRESS OF B2 TO P1`, `SET P1 P2 TO ADDRESS OF R1`, `SET ADDRESS OF B1 P1 TO P2` and
// `SET ADDRESS OF B1 TO ADDRESS OF R1`. §14.9.39.4 GR12 and GR13 are each written over "EACH … in the order
// specified" — rules ABOUT the list — so a receiver LIST is what lets them be one loop instead of two arities.
// ⛔ OF IS AN OPTIONAL WORD IN BOTH POSITIONS (kb/Work PB695): the rendered figure rules ADDRESS and TO and
// leaves OF bare, and the sender's phrase is the §8.4.3.11.2 data-address-identifier on folio 140, whose whole
// underline roster is {ADDRESS}. ADDRESS is a reserved token and can never head a dataReference, so
// `SET ADDRESS P TO Q` stays unambiguous against setToValueStatement.
setAddressStatement
    : SET setAddressReceiver+ TO setAddressSender
    ;

// ONE printed Format-7 receiving operand — the brace `{ ADDRESS OF data-name-1 | identifier-5 }`. The operand
// CARRIES ITS OWN KIND: `ADDRESS()` present is data-name-1, the BASED receiver §14.9.39.3 SR18 screens and
// §14.9.39.4 GR13 assigns to; absent is identifier-5, the data-pointer receiver SR17 screens and GR12 stores
// into. A hand-written re-derivation from token positions is what the Format-3/4 half of this note deleted.
setAddressReceiver
    : (ADDRESS OF?)? dataReference
    ;

// identifier-6, the single sending operand. It may be an ordinary data-pointer item, a §8.4.3.11
// data-address-identifier (`ADDRESS OF x` — §8.4.3.11.4 GR1 "creates a unique data item of class pointer and
// category data-pointer", exactly what SR17's second sentence demands of identifier-6), or the predefined
// address NULL (§14.9.39.3 SR19). ⚠ The NULL arm is why setObjectReferenceStatement is listed BEFORE this rule
// in setStatement: `SET obj-ref TO NULL` is token-identical and belongs to Format 5.
// ⛔ THE SENDER'S ADDRESS PHRASE IS THE ONE `dataAddressIdentifier` RULE (kb/Work PB239), not an inline
// `ADDRESS OF?` spelling of its own: §8.4.3.11 is an IDENTIFIER format, and a second copy of it here is how the
// CALL argument list came to have none. The receiving `ADDRESS OF data-name-1` above is deliberately NOT that
// rule — Annex D: "the ADDRESS OF phrase in the receiving operand of a SET statement is not considered a
// data-address-identifier" (§8.4.3.11.3 SR5 forbids one as a receiving operand).
setAddressSender
    : dataAddressIdentifier
    | dataReference
    | NULL_
    ;

// §8.4.3.1.2 identifier FORMAT 9 — "address-identifier": `{ data-address-identifier-1 |
// program-address-identifier-1 }` (§8.4.3.1.3 SR10: "Address-identifiers are defined by 8.4.3.11,
// Data-address-identifier and 8.4.3.13, Program-address-identifier"). ⚠ The function-address-identifier of
// §8.4.3.12 is NOT a member: Format 9 prints two arms, and §8.4.3.1.2 lists no identifier format for the
// FUNCTION twin, so it is reachable only where a general format names it (SET Format 8). PROGRAM is a
// reserved token that can never head a dataReference, so the program arm cannot be claimed by the data arm.
addressIdentifier
    : programAddressIdentifier
    | dataAddressIdentifier
    ;

// §8.4.3.11.2 DATA-ADDRESS-IDENTIFIER: `ADDRESS OF identifier-1`, with ADDRESS the whole underline roster
// (rendered, folio 140 — kb/Work PB695), so OF is optional. ONE rule for every surface that takes it: the SET
// Format-7 sender and the CALL argument (kb/Work PB239); the data pass forces each named record onto cell
// storage by walking THESE nodes, so a new surface that uses the rule is storage-forced automatically.
dataAddressIdentifier
    : ADDRESS OF? dataReference
    ;

// SET { identifier-12 } … TO ADDRESS OF FUNCTION { function-prototype-name-1 | identifier-1 }
// — ISO §14.9.39.2 Format 8 (function-pointer-assignment) with the §8.4.3.12 FUNCTION-ADDRESS-IDENTIFIER as its
// sender. Format 8's own printed figure (folio 730) is just `SET { identifier-12 } … TO identifier-13` with no
// choice indicators, so the PLAIN Format 8 (`SET fp1 fp2 TO fp3`, `SET fp TO NULL`) needs no rule of its own —
// setToValueStatement and setObjectReferenceStatement already parse those shapes and SetBinder re-routes on the
// receiver's category, exactly as Format 9 does. What needs a rule is the SENDER: §8.4.3.12.2 prints
// `ADDRESS OF FUNCTION { function-prototype-name-1 | identifier-1 }`, and that phrase appears nowhere else in
// the grammar. kb/Work PB452 + PB817.
// ⛔ OF IS AN OPTIONAL WORD (kb/Work PB695 family): the rendered §8.4.3.12.2 figure underlines ADDRESS and
// FUNCTION and NOT OF, so §8.3.2.4.3 makes `SET FP TO ADDRESS FUNCTION FPROTO` conforming source.
// ⚠ ONE dataReference operand covers BOTH braced arms: function-prototype-name-1 is a user-defined word and
// identifier-1 is a data item reference, and the two are told apart at BIND (§8.4.3.12.3 SR1/SR2), where the
// REPOSITORY function-specifier table and the item's category are both facts. FUNCTION is a reserved token and
// can never head a dataReference, so this rule cannot be claimed by setAddressStatement's sender arm.
// ⚠ THERE IS NO literal-1 ARM, and that is measured, not an omission: §8.4.3.13's PROGRAM twin prints three
// operands including literal-1 and §8.4.3.12's FUNCTION figure prints only two.
setFunctionAddressStatement
    : SET dataReference+ TO ADDRESS OF? FUNCTION dataReference
    ;

// §8.4.3.13.2 PROGRAM-ADDRESS-IDENTIFIER — its OWN rule, because it is an IDENTIFIER format (§8.4.3), not a
// statement phrase. The printed general format (PDF p172/folio 142, RENDERED at 300 dpi) is
//     ADDRESS OF PROGRAM { identifier-1 | literal-1 | program-prototype-name-1 }
// with ADDRESS and PROGRAM underlined and OF NOT underlined, so §8.3.2.4.3 makes `ADDRESS PROGRAM "X"`
// conforming source too (the kb/Work PB695 family — the same evidence the §8.4.3.11 and §8.4.3.12 twins take
// their optional OF from). The braces are a plain required choice: exactly one operand.
// ⚠ ONE dataReference COVERS TWO OF THE THREE BRACED ARMS, as it does in setFunctionAddressStatement:
// program-prototype-name-1 is a user-defined word and identifier-1 is a data item reference, and the two are
// told apart at BIND (§8.4.3.13.3 SR1 vs SR3), where the REPOSITORY program-specifier table and the item's
// category are both facts. There IS a literal-1 arm here and the FUNCTION twin has none — that asymmetry is
// the standard's, measured on both printed figures, not an omission.
// ⛔ §8.4.3.13.3 SR4 ("This identifier format shall not be specified as a receiving operand") is why the rule
// appears only in the SENDER slot below.
programAddressIdentifier
    : ADDRESS OF? PROGRAM (nonNumericLiteral | dataReference)
    ;

// SET { identifier-7 } … TO program-address-identifier — ISO §14.9.39.2 Format 9 (program-pointer-assignment)
// with the §8.4.3.13 program-address-identifier as its sender. Format 9's own printed figure is just
// `SET { identifier-7 } … TO identifier-8` with no choice indicators, so the PLAIN Format 9
// (`SET pp1 pp2 TO pp3`, `SET pp TO NULL`) needs no rule of its own — setToValueStatement and
// setObjectReferenceStatement parse those shapes and SetBinder re-routes on the receiver's category. What
// needs a rule is the SENDER, and §8.4.3.13.2's phrase appeared NOWHERE in this grammar (kb/Work PB549): the
// only route into a program-pointer was setEntryStatement's `TO ENTRY`, a Micro Focus / IBM vendor spelling
// whose words are in no ISO general format, so the STANDARD spelling was a bare parse error at every edition
// while the extension was the only way in.
// Listed BEFORE setAddressStatement, whose sender arm is `TO ADDRESS OF? dataReference` and would otherwise
// be tried first; PROGRAM is a reserved token and can never head a dataReference, so nothing else can claim
// this shape. Gate: COBOL-2002 (the program-pointer family's edition — usage-program-pointer-2002).
setProgramAddressStatement
    : SET dataReference+ TO programAddressIdentifier
    ;

// ALLOCATE statement (COBOL-2002 §14.9.3): obtain dynamic storage, returned as a managed data-pointer.
//   ALLOCATE n CHARACTERS [INITIALIZED] [RETURNING p]   — allocate n bytes, return the pointer
//   ALLOCATE based-item   [INITIALIZED] [RETURNING p]   — allocate storage for a BASED item, set its address
allocateStatement
    : ALLOCATE arithmeticExpression CHARACTERS INITIALIZED? (RETURNING dataReference)?
    | ALLOCATE dataReference INITIALIZED? (RETURNING dataReference)?
    ;

// FREE statement (COBOL-2002 §14.9.15): release storage previously ALLOCATEd; set each data-pointer to NULL.
freeStatement
    : FREE dataReference+
    ;

// SET {identifier-3}... TO object/NULL/SELF (ISO 14.9.39 Format 5, OO 2002+). ANTLR-order reality
// (feedback_grammar_precedence): a dataReference SENDER parses as setToValueStatement (alternative 3
// precedes this rule and `TO arithmeticExpression` matches it) — BindSetTo re-routes SEMANTICALLY when a
// target is an object-reference item; only NULL/SELF/SUPER senders (no arithmeticExpression prefix) reach
// THIS rule. SUPER is admitted syntactically and rejected at bind (SR9 - 0867) for the better diagnostic.
setObjectReferenceStatement
    : SET dataReference+ TO objectReference   // introduction-gated at BIND time (StatementBinder.OoBindSetObjectRef → Check(SetObjectReference2002))
    ;

objectReference
    : dataReference
    | NULL_
    | SELF
    | SUPER
    ;

// SET dataReference+ UP/DOWN BY arithmeticExpression (COBOL-85 §14.9.39 Format 2)
setIndexStatement
    : SET dataReference+ ( UP | DOWN ) BY arithmeticExpression
    ;

// ==========================================
// ACCEPT (§14.9.1)
// ==========================================

// Formats 1 (device) and 2 (temporal) are IMPLEMENTED; format 3 (screen, §14.9.1.2 — Annex A.4.2 item 1) is a
// DECLINED optional element given a surface here purely so it draws the named COBOLNET1707 instead of a generic
// parse error, and so its MINIMAL shape stops being invisible: `ACCEPT screen-name-1` is token-identical to the
// format-1 device ACCEPT, so without the binder's screen-name test it silently transfers device input into a
// screen record (kb/Work PB260). The positioning and exception phrases live in CobolScreen.g4.
acceptStatement
    : ACCEPT dataReference (FROM acceptSource)? screenTail? END_ACCEPT?   // END-ACCEPT: 2002+ (gated in VersionConformancePass; kb/Work PB134)
    ;

acceptSource
    : DATE YYYYMMDD
    | DATE
    | TIME
    | DAY YYYYDDD
    | DAY
    | DAY_OF_WEEK
    | dataReference
    ;

// ==========================================
// DISPLAY (§14.9.11)
// ==========================================

// An operand may be a function-identifier (ISO §8.4.4.1 — an identifier includes a function-identifier;
// §14.9.11.2 identifier-1): DISPLAY FUNCTION EXCEPTION-STATUS is the EC model's canonical interrogation shape.
// functionCall starts with the FUNCTION token, so the alternative is unambiguous.
// Format 2 (screen, §14.9.11.2 — Annex A.4.2 item 9) gets the same declined-but-named surface as ACCEPT
// format 3. ⛔ THE OPERAND LOOP MUST STOP AT THE POSITIONING PHRASE. `DISPLAY SG COLUMN 5` used to bind as a
// THREE-operand device DISPLAY because COL/COLS/COLUMN/COLUMNS are cobolWord alternatives while AT/LINE are
// not — the same construct produced two different non-diagnoses (kb/Work PB260, GR-14.9.11.4-16).
// `{!screenPositionAhead()}?` closes it, honouring reservedHere for COL/COLS/COLUMNS, which are §8.9
// reserved only from 2002 (AT, LINE and COLUMN are reserved at every edition).
//
// ⛔ AND THE FIRST OPERAND IS DELIBERATELY UNGUARDED, which is the whole difference between this rule and
// the one that shipped. Guarding EVERY iteration of a `( … )+` loop leaves a DISPLAY whose first token is a
// positioning word with NO viable reading at all, so the rule dies on a raw `COBOL0001: failed predicate`.
// The argument for guarding it — "none of them can legally be a DISPLAY operand, so nothing legal is lost" —
// is true of LEGAL source and misses that ILLEGAL source must still reach its NAMED diagnostic:
// `01 COLUMN PIC 9.` … `DISPLAY COLUMN.` is a §8.3.2.1 rule-1 violation whose answer is COBOLNET0901 from
// the §8.9 funnel, and the guard turned that into an unexplained parse error at every edition
// (ReservedWordPositionConformanceTests.DataItemNamedColumn_Rejected0901_EvenAt85; the memory is
// `left_edge_predicates`). Requiring one operand unconditionally and guarding only the CONTINUATION keeps
// both answers: `DISPLAY COLUMN.` binds COLUMN as a dataReference and the funnel names it, while
// `DISPLAY SG COLUMN 5` still takes SG as its one operand and hands COLUMN 5 to screenTail.
displayStatement
    : DISPLAY (inlineMethodInvocation | dataReference | literal | functionCall)
      ({!screenPositionAhead()}? (inlineMethodInvocation | dataReference | literal | functionCall))*
      displayUpon? displayNoAdvancing? screenTail? END_DISPLAY?
    ;

displayUpon
    : UPON cobolWord
    ;

displayNoAdvancing
    : WITH? NO ADVANCING
    ;

// ==========================================
// GOBACK (§14.9.18)
// ==========================================

// ⛔ THE TAIL IS A CHOICE-INDICATOR GROUP, NOT AN ORDERED AT-MOST-ONE STACK (kb/Work PB407). §14.9.18.2's
// printed figure (PDF page 661 / printed folio 631, rendered at 300 dpi) encloses `raising-phrase` and
// `status-phrase` in a bracket carrying CHOICE INDICATORS — the pair of `|` bars just inside `⎡ … ⎤` — and
// §5.2.6.4 reads that as "zero or more of the alternatives … but any single alternative may be specified only
// once", with "The alternatives may be specified in any order." The rule used to be `(raisingPhrase |
// statusPhrase)?`, which is the FALSELY-RESTRICTIVE reading: `GOBACK RAISING EXCEPTION EC-USER-X WITH ERROR
// STATUS 5` and its reverse were both rejected as syntax errors. `*` expresses "zero or more, any order"; the
// "each at most once" half is §5.2.6.4's and is screened at bind (CallBinder.DecodeGobackPhrases →
// ChoiceIndicators.AtMostOnce), because a grammar cannot say it without enumerating the orderings.
//
// ⚠ RETURNING / GIVING IS AN EXTENSION, and §14.9.18.2 contains no such phrase — the figure above has exactly
// two alternatives. It is kept (the `goback-returning` registry row records the latitude and its authority);
// what it may NOT do is cite a GOBACK clause as if the standard printed it.
gobackStatement
    : GOBACK ((RETURNING | GIVING) dataReference)? (raisingPhrase | statusPhrase)*
    ;

// ==========================================
// RAISE / RESUME + the RAISING phrase (EC model, ISO §14.9.29 / §14.9.33 / §14.9.18; 2002+)
// ==========================================
// UNgated alternatives by design: the statements parse at every edition and the BINDER issues the targeted
// not-in-this-edition diagnostic at --std 85 (a no-viable-alternative parse error names nothing — the
// four-compilers rule wants the edition named).

// RAISE {EXCEPTION exception-name-1 | identifier-1} (ISO §14.9.29.2). The exception-name is a cobolWord —
// EC names are an OPEN set (EC-USER-*/EC-IMP-*, §14.6.13.1.1), so name validation is the binder's (SR1/SR2).
// RAISE identifier-1 takes objectReference (not bare dataReference) so SR2's "NULL and SUPER shall not
// be specified" gets a TARGETED diagnostic instead of a parse error, and RAISE SELF (legal) parses
// (§14.9.29.3 SR2; the EC-OO wave, deep-dive slice 6).
raiseStatement
    : RAISE (EXCEPTION cobolWord | objectReference)
    ;

// RESUME [AT] {NEXT STATEMENT | procedure-name-1} (ISO §14.9.33.2 — AT is an OPTIONAL word: it is not
// underlined in the general format, so `RESUME NEXT STATEMENT` / `RESUME procedure-name` are legal too).
resumeStatement
    : RESUME AT? (NEXT STATEMENT | procedureName)
    ;

// RAISING {EXCEPTION exception-name-1 | identifier-1 | LAST EXCEPTION} (ISO §14.9.18.2 / §14.9.14.2 F2) —
// the ONE raising-phrase rule GOBACK and EXIT PROGRAM share.
// ⛔ THE SECOND `EXCEPTION` IS AN OPTIONAL WORD (kb/Work PB407). Measured on the printed figures at 300 dpi —
// PDF page 661 (GOBACK, folio 631) and PDF page 653 (EXIT Format 2, folio 623): `RAISING`, `LAST` and the
// FIRST `EXCEPTION` are underlined; the `EXCEPTION` after `LAST` is NOT, and §5.2.3 makes a non-underlined
// uppercase word an optional word. `GOBACK RAISING LAST.` and `EXIT PROGRAM RAISING LAST.` are therefore legal
// and were rejected outright. §7.3.21.4 rule 2 is third-party evidence inside the standard: the PROPAGATE rule
// writes the phrase back as "as though a GOBACK RAISING LAST statement were executed".
raisingPhrase
    : RAISING (EXCEPTION cobolWord | LAST EXCEPTION? | dataReference)
    ;

// The PROCEDURE DIVISION header RAISING phrase (ISO §14.2.1), measured off the printed diagram (PDF page 557,
// printed folio 527):
//
//     RAISING { exception-name-1                    } …
//             { [ FACTORY OF ] object-class-name-1  }
//             { interface-name-1                    }
//
// ⛔ EACH ELEMENT IS A TUPLE, NOT A WORD (kb/Work PB815). This rule used to read `RAISING cobolWord+` — ONE
// axis — so `RAISING FACTORY OF C` died on FACTORY's §8.9 reservation (COBOLNET0901) and the binder had only a
// name, never the FACTORY phrase that §14.9.18.3 SR4 a)/c) and §14.9.14.3 SR5 a)/c) compare against the
// identifier's description. FACTORY is underlined (required when its bracket is taken); OF is NOT, so it is an
// optional word (§5.2.3). The three alternatives are all one cobolWord in the grammar — an exception-name, a
// class-name and an interface-name cannot be told apart without the EC catalog and the REPOSITORY scope — so
// the partition is the binder's (RaisingPhrase.Partition, the ONE partition every header arm calls); FACTORY is
// the only axis the grammar CAN see, and it is kept.
raisingClause
    : RAISING raisingTarget+
    ;

raisingTarget
    : FACTORY OF? cobolWord          // object-class-name-1 with its optional phrase; SR8 of §14.2.2 is the binder's
    | cobolWord                      // exception-name-1 | object-class-name-1 | interface-name-1 — §14.2.2 SR7/SR8/SR9
    ;

// ==========================================
// REUSABLE EXCEPTION PHRASES
// ==========================================

// (Deleted 2026-07-19: `exceptionPhrase` / `onExceptionPhrase` / `notOnExceptionPhrase` were DEAD — defined
// here but referenced by nothing. They were also a trap: `exceptionPhrase` modelled the pair as an exclusive
// one-of-two, which is exactly the 5.2.6.4 choice-indicator defect repaired across this file, so wiring them
// up would have reintroduced it. Statements with ON EXCEPTION own their phrase rules — see
// callExceptionPhrases above and deleteFileOnException in Core/CobolIO.g4.)
