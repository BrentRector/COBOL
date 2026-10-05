// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Data division rules: sections, file descriptions, data description entries,
// and the INITIALIZE statement.
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolData;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// DATA DIVISION
// ==========================================

dataDivision
    : DATA DIVISION DOT
      fileSection?
      workingStorageSection?
      localStorageSection?
      linkageSection?
      reportSection?
      screenSection?
    ;

// ==========================================
// FILE SECTION
// ==========================================

fileSection
    : FILE SECTION DOT (fileDescriptionEntry | sortMergeDescriptionEntry)*
    ;

fileDescriptionEntry
    : FD fileName fileDescriptionClauses? DOT dataDescriptionEntry*
    ;

// SD entry — per §13.4.6.3, only RECORD clause is permitted (not BLOCK, CODE-SET, etc.)
sortMergeDescriptionEntry
    : SD fileName sortMergeDescriptionClauses? DOT dataDescriptionEntry*
    ;

sortMergeDescriptionClauses
    : sortMergeDescriptionClause+
    ;

// ⛔ THE §13.4.6.2 GENERAL FORMAT IS CLOSED (kb/Work PB829; RENDERED from the printed page — PDF p376 / folio
// 346): `SD file-name-1 [ record-clause ] .` and nothing else. DATA RECORDS is the COBOL-85 §13.4.6.2 clause
// removed at COBOL-2002 (superset-parsed here, removal-gated post-bind like every other removed construct).
// The last alternative is the error production, NOT a vendor hook — see CobolExpressions.g4#unrecognizedClause.
sortMergeDescriptionClause
    : recordClause
    | dataRecordsClause
    | unrecognizedClause   // ⛔ LAST — refused BY NAME by ClosedFormatPass (COBOLNET1970)
    ;

fileDescriptionClauses
    : fileDescriptionClause+
    ;

// ⛔ THE §13.4.5.2 GENERAL FORMATS ARE CLOSED (kb/Work PB829; RENDERED from the printed pages — PDF p372-373 /
// folios 342-343). The union of Format 1 (sequential), Format 2 (relative-or-indexed) and Format 3 (report) is
// IS EXTERNAL [AS literal-1] · IS GLOBAL · FORMAT {BIT|CHARACTER|NUMERIC} DATA · BLOCK CONTAINS · record-clause ·
// linage-clause · CODE-SET · REPORT(S), plus the COBOL-85 clauses removed at COBOL-2002 (LABEL RECORDS, DATA
// RECORDS, VALUE OF) which are superset-parsed and removal-gated post-bind. The last alternative is the error
// production, NOT a vendor hook — see CobolExpressions.g4#unrecognizedClause.
// ⚠ The first five alternatives are the FILE CONTROL ENTRY's clauses (§12.4.5.1), which no FD format contains.
// They are SUPERSET-PARSED here so the binder can refuse them BY NAME ("a clause of the file control entry —
// write it in the SELECT entry", COBOLNET2604) instead of a token error; and which of the three formats an entry
// is — and so which of the other clauses it admits — is decided in ONE place, Binding/FileDescriptionFormats.cs
// (kb/Work PB1238), whose table FileDescriptionFormatDriftTests holds total over this rule's alternatives.
fileDescriptionClause
    : organizationClause
    | accessModeClause
    | recordKeyClause
    | alternateKeyClause
    | fileStatusClause
    | blockContainsClause
    | recordClause
    | codeSetClause
    | formatClause
    | labelRecordsClause
    | dataRecordsClause
    | valueOfClause
    | fileGlobalExternalClause
    | linageClause
    | reportClause
    | unrecognizedClause   // ⛔ LAST — refused BY NAME by ClosedFormatPass (COBOLNET1970)
    ;

// FORMAT clause (§13.18.24.2, printed general format p403 — RENDERED, not read off the OCR).
//
// ⛔ RECOGNIZE-ONLY. The FORMAT clause is item 1) of Annex A.4.8, an OPTIONAL language element this
// implementation does NOT claim (docs/CONFORMANCE.md §5). A.4.1: "An implementation shall accept the syntax
// and provide the functionality for an optional element only when support for that language element is
// claimed by the implementor" — so the clause is parsed in order to be REFUSED BY NAME at bind
// (DataBinder → COBOLNET1705), never bound and never silently ignored. Compiling it inert would change
// which bytes reach the medium (§13.18.24.4 GR1/GR2) — a wrong answer, not a missing facility, which is why
// this is an Error and not the additive-facility warning band (COBOLNET1560/1578/1579/1580).
//
// SHAPE: `FORMAT { | BIT | CHARACTER | NUMERIC | } DATA`. The three alternatives are enclosed in CHOICE
// INDICATORS (the `|` bars just inside the braces), so §5.2.6.4 makes them ONE OR MORE, each at most once,
// IN ANY ORDER — `FORMAT BIT CHARACTER DATA` is legal source, not a syntax error. `DATA` is NOT underlined
// in the printed format: an optional word. The "each at most once" half is a §5.2.6.4 syntax rule an EBNF
// `+` cannot express; it is not enforced separately because the whole clause is refused either way.
formatClause
    : FORMAT (BIT | CHARACTER | NUMERIC)+ DATA?
    ;

// REPORT(S) clause (§13.18.46): names the report(s) produced on this file by the Report Writer.
reportClause
    : (REPORT IS? | REPORTS ARE?) reportName+
    ;

// IS GLOBAL / IS EXTERNAL on an FD (§13.18.27/§13.18.22): GLOBAL makes the file-name and
// record visible to contained programs; EXTERNAL shares the file across the run unit.
// Parsed here; GLOBAL visibility is handled by nested-program name resolution.
// ⛔ THE TWO ARMS ARE NOT SYMMETRIC, and writing them as one `(GLOBAL | EXTERNAL)` hid it (kb/Work PB511):
// the file-description Formats 1/2/3 (§13.4.5.2, rendered) print `[ IS EXTERNAL [ AS literal-1 ] ]` and
// `[ IS GLOBAL ]` — the AS phrase rides EXTERNAL ONLY, because §13.18.22.4 GR5 makes literal-1 the name of
// the FILE CONNECTOR that is externalized to the operating environment and the GLOBAL clause (§13.18.27)
// has no such phrase at all. This is the FD arm of the one EXTERNAL clause; its data-description twin is
// externalClause below, and both take the SHARED externalizedNamePhrase (PB303's one gate, one screen).
fileGlobalExternalClause
    : IS? GLOBAL
    | IS? EXTERNAL externalizedNamePhrase?
    ;

// BLOCK CONTAINS clause (§13.18.10)
blockContainsClause
    : BLOCK CONTAINS? integerOperand (TO integerOperand)? (CHARACTERS | RECORDS)?
    ;

// RECORD clause (§13.18.43) — fixed-length, variable-length, or VARYING forms.
// ⛔ THE BYTES/CHARACTERS BRACE GROUP IS A PAIR, NOT ONE WORD: §13.18.43.3 SR2 — "The words BYTES and CHARACTERS
// are synonymous and may be used interchangeably" — and all three printed general formats carry the same brace
// group. Only CHARACTERS was admitted until kb/Work PB721, so `RECORD CONTAINS 20 BYTES` drew COBOLNET1970
// ("not a clause of the file description entry") at COBOL-2023, where it is legal source: refused.
// ⚠ THE SPELLING IS A 2023 ADDITION and the EDITION gate is NOT here: Annex E.3.3 item 13 adds BYTES to
// the §8.10 context-sensitive list at 2023, so `VersionConformancePass.VisitRecordClause` refuses it below
// that edition (COBOLNET0900, `record-clause-bytes-2023`). The word is admitted by the grammar at every
// edition on purpose — a left-edge predicate cannot steer a mid-alternative token, and a parse-arm gate
// names the edition instead of throwing a token error. BYTES is also in cobolWord (§8.10: a
// context-sensitive word outside its format "is treated as a user-defined word"), so `01 BYTES PIC X.`
// stays legal COBOL-85 — the gate is on the CLAUSE, never on the word.
recordClause
    : RECORD CONTAINS? integerOperand (TO integerOperand)? (CHARACTERS | BYTES)?
    | RECORD IS? VARYING IN? SIZE? (FROM? integerOperand)? (TO integerOperand)? (CHARACTERS | BYTES)? (DEPENDING ON? dataReference)?
    ;

// CODE-SET clause (§13.18.13.2 — the 2002 two-class format; kb/Work PB110): IS alphabet-name-1
// [alphabet-name-2], or the FOR ALPHANUMERIC / FOR NATIONAL phrases — one or both, any order (the inner brace
// carries choice indicators, §5.2.6.4; the binder enforces each class at most once). The '85 one-name form is
// the first alternative's degenerate case.
codeSetClause
    : CODE_SET IS? cobolWord cobolWord?
    | CODE_SET codeSetForPhrase+
    ;

// ⛔ FOR IS AN OPTIONAL WORD (kb/Work PB695 family 2). §8.3.2.4.3: "uppercase words that are not underlined are
// called optional words and may be specified at the user's option with no effect on the semantics of the format".
// Measured off the printed §13.18.13.2 figure (PDF p414 / folio 384): the only underlined words in the whole
// format are CODE-SET, ALPHANUMERIC and NATIONAL — FOR and IS are plain, so `CODE-SET ALPHANUMERIC IS AL-1` is
// conforming source this rule used to reject. The class word stays REQUIRED, so the rule can never match empty
// and stays LL-disjoint from codeSetClause's first alternative (`CODE_SET IS? cobolWord …`): ALPHANUMERIC and
// NATIONAL are not in cobolWord, so no `?`-relaxation can make the two alternatives compete.
// ⚠ The COBOL-2002 gate keys on the SUBRULE (VersionConformancePass.VisitCodeSetClause reads
// `ctx.codeSetForPhrase().Length`), never on `ctx.FOR()` — relaxing the word cannot switch the gate off.
codeSetForPhrase
    : FOR? (ALPHANUMERIC | NATIONAL) IS? cobolWord
    ;

// LABEL RECORD(S) IS/ARE — obsolete COBOL-85 FD clause, semantically inert
labelRecordsClause
    : LABEL (RECORD IS? | RECORDS ARE?) (STANDARD | OMITTED | cobolWord+)
    ;

// DATA RECORD(S) IS/ARE — obsolete COBOL-74 FD clause, semantically inert
dataRecordsClause
    : DATA (RECORD IS? | RECORDS ARE?) cobolWord+
    ;

// VALUE OF implementor-name IS data-name/literal — obsolete COBOL-85 FD clause
// (§13.18 removed feature), semantically inert. Operands are implementor-defined label
// fields; consume the word/literal/IS sequence until the next clause keyword or the period.
valueOfClause
    : VALUE OF (cobolWord | literal | IS)+
    ;

// LINAGE clause (ISO §13.18.34) — page-based printing for sequential files
linageClause
    : LINAGE IS? (dataReference | integerLiteral) LINES?
      linageFootingPhrase?
      linageLinesAtTopPhrase?
      linageLinesAtBottomPhrase?
    ;

linageFootingPhrase
    : WITH? FOOTING AT? (dataReference | integerLiteral)
    ;

linageLinesAtTopPhrase
    : LINES? AT? TOP (dataReference | integerLiteral)
    ;

linageLinesAtBottomPhrase
    : LINES? AT? BOTTOM (dataReference | integerLiteral)
    ;

// ==========================================
// OTHER DATA SECTIONS
// ==========================================

workingStorageSection
    : WORKING_STORAGE SECTION DOT dataDescriptionEntry*
    ;

localStorageSection
    : LOCAL_STORAGE SECTION DOT dataDescriptionEntry*
    ;

// ISO §13.7.2: `LINKAGE SECTION. [ { 77-level-description-entry | constant-entry | record-description-entry |
// type-declaration-entry } ... ]` — the same four entry kinds as §13.5.2's working-storage section, all of which
// ride dataDescriptionEntry. ⛔ There is NO fifth kind (kb/Work PB1252): the `linkageProcedureParameter` alternative
// that stood here (`level dataName? USING [BY] {REFERENCE|VALUE|CONTENT} data-ref clauses.`) is defined by no ISO
// edition and no dialect this compiler declares, and every binder filtered it out, so `01 L-X USING BY VALUE W-A
// PIC 9(4).` compiled clean and the entry silently vanished. A USING phrase on a linkage entry is now a syntax
// error at every edition; a formal parameter and its passing mode are written in the procedure division header's
// using-phrase (§14.2.1).
linkageSection
    : LINKAGE SECTION DOT dataDescriptionEntry*
    ;

// ==========================================
// DATA DESCRIPTION ENTRIES
// ==========================================

dataDescriptionEntry
    : levelNumber dataName? dataDescriptionBody DOT
    ;

levelNumber
    : INTEGERLIT
    ;

dataName
    // The trailing action records the declared name for keywordContinuesHere() (kb/Work PB805/PB655): a word
    // §8.9 leaves free at this edition that the program DECLARES is a user word in every operand list; one it
    // does not declare keeps its keyword reading. Actions never run during prediction.
    : ( cobolWord
    | FILLER
    | PROCEDURE    // NC205A: PROCEDURE used as a data name (77 PROCEDURE-DIVISION PIC X)
    // kb/Work PB137/PB655: a reservation-gated word is never a cobolWord (so operand lists cannot absorb the
    // bare facility verbs), but a DECLARATION naming one must still PARSE — where §8.9 reserves it so the
    // funnel's targeted COBOLNET0901 can NAME the word instead of a generic parse error (the user-word-commit
    // pin), and where §8.9 frees it because this match is what the token-level gate retypes the word for. ⛔ THIS WAS A HAND-WRITTEN LIST OF TWO WORDS (COMMIT/ROLLBACK) and it silently
    // rotted: CRT and CURSOR became reservation-gated with kb/Work PB301 and were never added here, so
    // `01 CRT PIC X.` at --std 2002 answered COBOL0001 "no viable alternative" instead of naming §8.9.
    // `reservedGatedWord` is GENERATED from the SAME cobol-words.json `reservationGated` flag that generates
    // the cobolWord gates (kb/Work PB300, CLAUDE.md rule 5), so the two halves cannot drift apart and the next
    // gated word is automatic; CobolWordsDriftTests pins both directions. PROCEDURE stays a separate
    // alternative on purpose — it is reserved at EVERY edition and NC205A must keep compiling, so it must NOT
    // reach the funnel.
    | reservedGatedWord
      ) { declareName(TokenStream.LT(-1)); }
    ;

dataDescriptionBody
    : constantEntryBody
    | dataDescriptionClauses
    | renamesClause
    ;

// Constant entry (ISO §13.10, COBOL-2002): {1|01} constant-name CONSTANT [IS GLOBAL]
// {AS {arithmetic-expression-1 | BYTE-LENGTH OF data-name-1 | literal-1 | LENGTH OF data-name-2}
//  | FROM compilation-variable-name-1}.
// SUPERSET PARSE at every edition — the COBOL-2002 introduction gate is the VersionConformancePass parse arm
// (VisitConstantEntryBody → constant-entry-2002 → COBOLNET0900 below 2002). LL-disjoint from the clause list
// (no dataDescriptionClause begins with CONSTANT except constantRecordClause, whose SECOND token RECORD
// separates it) and from renamesClause (RENAMES). The binder (DataBinder.Constants.cs) folds the entry into
// the compile-time constant table — a constant occupies NO storage (§13.10.4 GR1/GR3: references substitute
// the literal).
// ⛔ IS, AS and OF are OPTIONAL words (kb/Work PB1225): the printed §13.10.2 format (PDF p.355, rendered) underlines
// 1/01, CONSTANT, GLOBAL, BYTE-LENGTH, LENGTH and FROM and nothing else, so `01 K CONSTANT 8.`,
// `01 K CONSTANT "AB".` and `01 K CONSTANT LENGTH W.` are the same entries as their AS / OF spellings (§5.2.3:
// optional words are "shown in uppercase and not underlined in general formats" and "may be written to add clarity"). No alternative of constantValue begins with a word the clause list or FROM can
// begin with (FROM and RECORD are reserved), so the bare operand stays LL-disjoint from `CONSTANT RECORD`.
constantEntryBody
    : CONSTANT (IS? GLOBAL)? (AS? constantValue | FROM cobolWord)
    ;

// The constant entry as a REPORT SECTION entry (kb/Work PB1226): §13.8.2's brace group
// `{ constant-entry | report-group-description-entry } …` lets a constant entry stand between the report
// description entry's report groups, and §13.10.2 gives it the same format wherever it stands. It is the
// dataDescriptionEntry arm's constantEntryBody under its own entry rule because the report section's entry
// list is a choice between two entry KINDS, not a data description entry (§13.16) with a report group body.
// ONE body rule (constantEntryBody), ONE binder (DataBinder.BindConstantEntry), ONE version gate
// (VersionConformancePass.VisitConstantEntryBody): the report section adds a host, never a second constant form.
constantEntry
    : levelNumber dataName? constantEntryBody DOT
    ;

// The AS operand (§13.10.2). LENGTH OF and BYTE-LENGTH OF are listed FIRST so they win over arithmeticExpression's
// qualified-dataReference reading of the same tokens (`LENGTH OF X` — LENGTH is a cobolWord; `BYTE-LENGTH OF X` —
// BYTE-LENGTH is a §8.9 context-sensitive word, "constant entry", with no token of its own). A single numeric literal
// rides arithmeticExpression and is re-classified as a LITERAL by the binder (§13.10.3 SR1). Both length phrases
// carry their operand as a dataReference, and DataBinder.BindConstantLength binds both (§13.10.4 GR5 / GR6;
// kb/Work PB1227 — the BYTE-LENGTH form used to ride arithmeticExpression as the qualified reference
// `BYTE-LENGTH OF x`, recognized by its spelling and staged loud). OF is an optional word in both phrases (§13.10.2 does
// not underline it, kb/Work PB1225): `LENGTH W` and `BYTE-LENGTH W` are no arithmetic expression, so the omission is
// unambiguous.
constantValue
    : LENGTH OF? dataReference
    | constantByteLengthWord OF? dataReference
    | nonNumericLiteral
    | arithmeticExpression
    ;

// The keyword of the constant entry's byte-length phrase (§13.10.2), a context-sensitive word whose §8.9 context is
// "constant entry", so it has no token: recognized by its spelling through the ONE word-text predicate (wordAhead, so a
// >>COBOL-WORDS synonym steers it too).
constantByteLengthWord
    : {wordAhead("BYTE-LENGTH")}? IDENTIFIER
    ;

// ==========================================
// DATA DESCRIPTION CLAUSES
// ==========================================

dataDescriptionClauses
    : dataDescriptionClause*
    ;

// ⛔ THE §13.16.2 FORMAT-1 CLAUSE LIST IS CLOSED (kb/Work PB487; the general format is RENDERED from the printed
// page — PDF p393/folio 363 — and carries exactly the 21 optional clause slots transcribed above at §13.16.2).
// It used to END in `genericDataClause -> genericClause : IDENTIFIER (IDENTIFIER|literal)*`, a vendor-extension
// catch-all that swallowed ANY word sequence at the tail of an entry. Three harms followed from that ONE
// alternative, and all three are the same harm: a word the grammar does not know was DISCARDED rather than
// diagnosed.
//   (a) the §13.18.1 ALIGNED clause — a REAL clause of this general format with no rule of its own — parsed and
//       did nothing, so `05 B2 PIC 1(4) USAGE BIT ALIGNED.` laid out at the WRONG bit offset in silence;
//   (b) the bare (USAGE IS-less) `01 M MESSAGE-TAG.` that §13.18.60.2 makes legal bound with NO usage, and the
//       missing PicInfo escaped the binder into an unhandled NullReferenceException in MoveEmitter;
//   (c) every "…the only other clauses permitted are…" rule of §13.16.3 (SR12/SR13/SR17/SR18) was unenforceable
//       against a word the parser never classified, however carefully its condition list was maintained.
// The replacement is `unrecognizedClause` (CobolExpressions.g4), THE one error production of the whole grammar:
// it still RECOGNIZES the word run — which keeps recovery local, one diagnostic per entry instead of a cascade —
// but `Validation/ClosedFormatPass.cs` REFUSES it BY NAME (COBOLNET1941 for this format) at every edition and
// every strictness. There is no dialect that admits it: a vendor extension is admitted only under the dialect
// that owns it, never by a catch-all, and this compiler declares no vendor dialect. It is LAST because ANTLR
// takes the first matching alternative.
// ⚠ kb/Work PB829 generalized this: the SAME catch-all was wired into seven more closed general formats, and the
// rule is now written ONCE — one grammar production, one pass, one format table — rather than once per format.
dataDescriptionClause
    : pictureClause
    | usageClause
    | occursClause
    | redefinesClause
    | valueClause
    | signClause
    | syncClause
    | justifiedClause
    | blankWhenZeroClause
    | alignedClause   // ISO §13.18.1 (COBOL-2002); superset parse, introduction-gated by VersionConformancePass ParseArm.VisitAlignedClause
    | constantRecordClause   // COBOL-2002 §13.18.15; superset parse, introduction-gated by VersionConformancePass ParseArm.VisitConstantRecordClause
    | propertyClause   // COBOL-2002; parses at all editions (superset), introduction-gated post-bind by VersionConformancePass ParseArm.VisitPropertyClause (rearch 14g.2). (The VALUE-list PROPERTY guards below are KEPT — they are value-operand disambiguation, not an edition gate.)
    | externalClause
    | globalClause
    | typeClause
    | typedefClause
    | sameAsClause
    | basedClause
    | anyLengthClause
    | dynamicLengthClause
    | groupUsageClause   // COBOL-2002 §13.18.29 (kb/Work PB79); superset parse, introduction-gated by VersionConformancePass ParseArm.VisitGroupUsageClause
    | selectWhenClause   // ISO §13.18.51 — Annex A.4.8 item 2), DECLINED: recognize-only, refused by name at bind (COBOLNET1705)
    | {is2002()}? validationClause   // ISO §13.16.2 validation-clauses — Annex A.4.14, DECLINED: recognize-only, refused by name at bind (COBOLNET1708); the rule and its rationale are in Grammar/Core/CobolDeclined.g4
    | unrecognizedClause   // ⛔ LAST — the error production above; refused BY NAME by ClosedFormatPass (COBOLNET1941)
    ;

// ALIGNED clause (ISO §13.18.1.2 — the general format is the single required word `ALIGNED`, underlined).
// §13.18.1.3 SR1 confines it to "a bit group item or an elementary bit data item" and §13.18.1.4 GR1 makes it
// align the subject "on the first bit of the first available byte boundary"; both live at the ONE bit-layout
// site (Binding/Model/BitLayout.cs) and the ONE entry-shape site (DataBinder.BindEntry, COBOLNET1942). The
// COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitAlignedClause.
alignedClause
    : ALIGNED
    ;

// SELECT WHEN clause (§13.18.51.2, printed general format p481 — RENDERED).
//
// ⛔ RECOGNIZE-ONLY, the twin of formatClause above: item 2) of Annex A.4.8, an OPTIONAL element this
// implementation does not claim (docs/CONFORMANCE.md §5), parsed so it can be REFUSED BY NAME at bind
// (COBOLNET1705). An inert SELECT WHEN would select the WRONG record description entry
// (§13.18.51.4 GR1/GR2) with a status-45 failure path — a wrong answer, so Error, not the warning band.
//
// SHAPE: `SELECT WHEN { condition-name-1 | OTHER }`. Plain braces, NO choice indicators: exactly one of the
// two. SELECT, WHEN and OTHER are underlined (required words); condition-name-1 is a user-defined word.
// OTHER leads so ANTLR's first-match cannot route it into the conditionName slot (OTHER is reserved at every
// edition and is not cobolWord-admitted, so this is belt-and-braces, not load-bearing).
selectWhenClause
    : SELECT WHEN (OTHER | conditionName)
    ;

// condition-name-1 of the SELECT WHEN clause (§13.18.51.2) — a user-defined word, the level-88 name slot.
conditionName
    : cobolWord
    ;

// GROUP-USAGE clause (COBOL-2002 §13.18.29): the group item is treated as an elementary item of usage bit /
// category boolean (BIT) or usage national / category national (NATIONAL) — data-model design D20. Only the
// GROUP-USAGE token is unique to this clause; BIT / NATIONAL stand alone here (no USAGE prefix), which is why
// they are dedicated tokens rather than cobolWord-admitted here.
groupUsageClause
    : GROUP_USAGE IS? (BIT | NATIONAL)
    ;

// EXTERNAL clause (§13.18.22) — shared storage across run unit
propertyClause
    : PROPERTY (WITH? NO (GET | SET))? (IS? FINAL)?   // §13.18.42.2 :21146-21148 (WITH optional per the IS?-style tolerance)
    ;

// EXTERNAL clause (ISO §13.18.22.2, general format RENDERED from the printed page — PDF p430/folio 400):
//     IS <u>EXTERNAL</u> [ <u>AS</u> literal-1 ]
// IS is NOT underlined (an optional word, §8.3.2.4.3); EXTERNAL and AS are. The data-description Format 1
// (§13.16.2) and the file-description Formats 1/2/3 (§13.4.5.2) print the SAME slot as
// `[ IS EXTERNAL [ AS literal-1 ] ]`, so the phrase belongs to BOTH surfaces — see
// fileGlobalExternalClause above, which is this clause's other arm (kb/Work PB511).
// ⚠ `AS literal-1` is the SHARED externalizedNamePhrase, never a local `AS literal` of its own: PB303's
// landing made that rule the ONE surface and VersionConformancePass ParseArm.VisitExternalizedNamePhrase the
// ONE COBOL-2002 introduction gate (constructs.json externalized-name-as-2002), so a new AS site is gated by
// writing `externalizedNamePhrase?` and NOTHING else. §13.18.22.3 SR3 narrows the literal at bind
// (ExternalizedName.Screen → COBOLNET2156) and §13.18.22.4 GR5 makes it the record's externalized name.
externalClause
    : IS? EXTERNAL externalizedNamePhrase?
    ;

// BASED clause (COBOL-2002 §13.18.5) — level 01/77 only; the item is a template with an implicit
// data-address pointer (initially NULL) and NO storage until SET ADDRESS OF / ALLOCATE gives it one.
basedClause
    : BASED   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitBasedClause (rearch 14g.2)
    ;

// CONSTANT RECORD clause (COBOL-2002 §13.18.15) — identifies a STRUCTURED CONSTANT: the record's content is
// its normal initial content (§13.18.15.4 GR1 — as though INITIALIZE … WITH FILLER ALL TO VALUE THEN TO
// DEFAULT), and neither the record nor any subordinate may be a receiving operand (SR2 → COBOLNET1548 at
// bind). Structural SRs (§13.18.15 SR1 WS/LS-only; §13.16.3 SR3/SR6/SR13) bind-check in DataBinder.
constantRecordClause
    : CONSTANT RECORD   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitConstantRecordClause
    ;

// ANY LENGTH clause (COBOL-2002 §13.18.2) — the length of a LINKAGE item varies at runtime with the length
// of the corresponding argument (GR1). UNGATED superset parse (the basedClause pattern); the SR1–SR4 shape
// rules bind-check in DataBinder and the placement sweeps.
anyLengthClause
    : ANY LENGTH   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitAnyLengthClause
    ;

// DYNAMIC LENGTH clause (ISO §8.5.1.10 / §13.18.19, COBOL-2014) — a variable-length, minimum-length-zero PIC X or
// PIC N string. Format: DYNAMIC LENGTH [dynamic-length-structure-name] [LIMIT IS? integer]. UNGATED superset parse
// (the anyLengthClause pattern); the §13.18.19.3 SR1 (PICTURE exactly one N or X), §13.16.3 SR18 (permitted
// co-clauses), and the structure-name non-support bind-check in DataBinder (COBOLNET1561/1562/1563), and the
// COBOL-2014 introduction gate is VersionConformancePass ParseArm.VisitDynamicLengthClause → COBOLNET0900 below
// 2014. LL-disjoint from occursClause: occursClause always leads with OCCURS (its Format-4 dynamic-capacity
// alternative has DYNAMIC only as the SECOND token); here DYNAMIC is the leading token, and DYNAMIC appears
// nowhere else at the start of a dataDescriptionClause — so tokens DYNAMIC/LENGTH/LIMIT need no lexer change.
// ⛔ LENGTH IS AN OPTIONAL WORD (kb/Work PB695 family 2). Measured off the printed §13.18.19.2 figure (PDF p427 /
// folio 397): `DYNAMIC LENGTH [ dynamic-length-structure-name-1 ] [ LIMIT IS integer-1 ]` carries underlines
// under DYNAMIC and LIMIT ONLY, so LENGTH and IS are optional words (§8.3.2.4.3) and `DYNAMIC DLS-1 LIMIT 30` is
// conforming source. DYNAMIC stays the required anchor and heads no other data-description clause, so the
// relaxation adds no ambiguity; the COBOL-2014 gate keys on the CLAUSE context, not on the word.
dynamicLengthClause
    : DYNAMIC LENGTH? cobolWord? (LIMIT IS? integerOperand)?
    ;

// GLOBAL clause (§13.18.27) — visible to contained programs
globalClause
    : IS? GLOBAL
    ;

// TYPE clause (ISO §13.18.57; the TYPEDEF clause it names is §13.18.58): PROVISIONAL COBOL-2002 edge — the former {is2023()}? gate was
// PROVABLY wrong (ISO-validation, DEVLOG 582: TYPEDEF has ~33 hits in the 2023 spec body yet ZERO Annex E
// 2014→2023 change rows ⇒ it predates 2023). The 2002-vs-2014 refinement is blocked on the older standards
// (roadmap decision 1 provisional policy; tests/version-matrix/constructs.json row type-clause-2002).
// ⛔ THE OPTIONAL WORD IS `TO`, NOT `IS` (kb/Work PB889). §13.18.57.2 Format 1 prints `TYPE TO type-name-1` with TYPE
// underlined and TO not (rendered PDF page 524, printed folio 494) — §5.2.3 makes an un-underlined uppercase word an
// OPTIONAL word. The rule used to read `TYPE IS? IDENTIFIER`, so the standard's own spelling `TYPE TO T` was rejected
// (COBOL0001 "unexpected 'TO'") while a spelling no format prints was accepted. `TYPE IS` is Format 2's (the report
// group TYPE clause, CobolReportWriter.g4), not this one's.
typeClause
    : TYPE TO? IDENTIFIER   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitTypeClause (rearch 14g.2)
    ;

// TYPEDEF clause (ISO §13.18.58, COBOL-2002; data-model D17) — marks this data description entry as a TYPE
// DECLARATION (a named template; it allocates no storage). STRONG (§13.18.58.2) makes the type strongly-typed.
// LL-disjoint from externalClause/globalClause (IS? EXTERNAL | GLOBAL): the keyword after the optional IS differs.
typedefClause
    : IS? TYPEDEF STRONG?   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitTypedefClause (recognition; rearch 14g.2, DEVLOG 734)
    ;

// SAME AS clause (ISO §13.18.49, COBOL-2002): the subject takes the SAME data description as data-name-1's
// entry, subordinates included (GR1/GR2 — coded in place, minus data-name-1's level/name/CONSTANT RECORD/
// EXTERNAL/GLOBAL/REDEFINES/SELECT WHEN; subordinate levels renumber). §13.16.3 SR12 composes it only with
// CONSTANT RECORD / entry-name / EXTERNAL / GLOBAL / level-number / OCCURS — enforced at bind (COBOLNET1555).
// The target may be QUALIFIED (OF/IN — data-name-1 is an ordinary data-name reference) but never subscripted
// (§13.18.49 SR1 — data-name-1 shall not be subject to any OCCURS clause). LL-disjoint from every other
// dataDescriptionClause (unique leading token SAME; the I-O-CONTROL sameArea rule is a different context).
// Expansion rides the ONE TYPEDEF clone machinery (DataBinder.ExpandSameAs → CloneItem; data-model D17).
sameAsClause
    : SAME AS cobolWord ((OF | IN) cobolWord)*   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitSameAsClause (recognition; the typedefClause pattern)
    ;

// PIC Clause — PIC/PICTURE triggers PICMODE in the lexer, which emits a single PIC_STRING token (IS is consumed
// by PICMODE). The optional trailing EDITING phrases (ISO §13.18.40.2 Format 1, COBOL-2023) are lexed in DEFAULT
// mode: PIC_STRING stops at whitespace and pops PICMODE, so ` EDITING …` follows as an ordinary token stream.
// The whole EDITING group is additive/repeatable; SR11 (distinct character-1) is enforced at bind. The 2023
// introduction gate is VersionConformancePass ParseArm.VisitPictureClause (recognition on editingPhrase presence).
pictureClause
    : PIC PIC_STRING editingPhrase* pictureLocalePhrase?
    ;

// PICTURE Format 2 (locale) — `PIC IS character-string-1 LOCALE [IS locale-name-1] SIZE IS integer-1` (ISO
// §13.18.40.2; LIVE since kb/Work PB64 T6 — the item is fixed-point numeric-edited with locale editing, bound by
// DataBinder's format-2 arm; the 2002 introduction is the picture-locale-format2-2002 construct gate, so the
// predicate is NOT edition-gated — see pictureLocaleAhead's comment). LOCALE is not a lexer token (a plain word
// at COBOL-85, reserved 2002+), so the arm is text-predicated; the first cobolWord IS the word LOCALE. Both IS
// words are optional (non-underlined in the §13.18.40.2 figure; §5.2.3) — `LOCALE FR SIZE 12` is legal, and the
// required IS this rule used to demand before locale-name-1 rejected legal source (kb/Work PB114). A superset
// parse admits editingPhrase* alongside; format 2 has no EDITING phrase and the binder diagnoses the pairing.
pictureLocalePhrase
    : {pictureLocaleAhead()}? formatWord (IS? cobolWord)? SIZE IS? integerOperand   // LOCALE = formatWord (kb/Work PB764)
    ;

// EDITING character-1 { IS literal-1 | FOR { NEGATIVE/POSITIVE choice } } (ISO §13.18.40.2 Format 1). `IS` is
// optional noise (non-underlined in the figure). The FOR sub-group carries CHOICE INDICATORS (§5.2.6.4) —
// NEGATIVE and/or POSITIVE, each at most once, in either order — so it is TWO ordered alternatives, not the
// exclusive stacked braces the OCR transcription implied.
//
// ⛔ CHARACTER-1 IS WRITTEN BARE, NOT QUOTED (kb/Work PB568). The PRINTED general format — re-rendered from the
// canonical PDF, printed page 441 — is `EDITING character-1 { IS literal-1 | FOR { … } }`: character-1 carries
// no quotation marks and is named character-1, exactly as character-string-1 is, while literal-1/-2/-3 are named
// as literals. SR8 types it as "any basic letter in the COBOL character set", §13.18.40.4 GR14 ("Character-1 in
// character-string-1 represents a character position") and §13.18.40.5 rule 3 make it a PICTURE SYMBOL occurring
// in character-string-1, and SR9 — the rule that types the phrase's literals — enumerates only literal-1,
// literal-2 and literal-3. This rule used to demand a quoted literal there, so EVERY conforming EDITING phrase
// was a parse error and the SR8/SR10/SR11 checks were reachable only through a spelling the standard does not
// define. Parse-wide/bind-narrow: the quoted spelling still PARSES and draws the named COBOLNET2149 at bind
// (never a bare ANTLR syntax error), and the insertion literals stay broad so SR8/SR9 violations surface as
// named bind diagnostics.
//
// ⛔ LITERAL-1/-2/-3 ARE LITERAL POSITIONS, NOT THE NARROW `literal` RULE (kb/Work PB778). §13.10.3 SR2 puts a
// constant-name "anywhere that a format specifies a literal", §8.3.3.6.3 SR1 a figurative constant, and a
// symbolic-character is a figurative (§8.3.3.6.2 Format 7) — so the three operands are `editingLiteral`, the
// VALUE clause's literal-position superset, screened and substituted at bind by the ONE literal-position
// chokepoint (DataBinder.RawValueOperandText). Character-1 is a DIFFERENT operand kind (a picture symbol, not a
// literal) and keeps its own spelling above: the two kinds are separate rules so they can never again be
// widened or narrowed together by accident.
editingPhrase
    : EDITING ( cobolWord | literal ) ( IS? editingLiteral | FOR editingForPhrase )
    ;

editingForPhrase
    : NEGATIVE IS? editingLiteral ( POSITIVE IS? editingLiteral )?
    | POSITIVE IS? editingLiteral ( NEGATIVE IS? editingLiteral )?
    ;

editingLiteral
    : valueClauseOperand
    ;

// USAGE Clause (ISO §13.18.60.2): `[USAGE IS] usage-keyword` — the USAGE keyword is OPTIONAL for EVERY usage, so
// the bare and the prefixed spellings are ONE alternative over ONE usageKeyword rule (kb/Work PB95: the former
// hand-listed bare alternatives omitted POINTER, OBJECT REFERENCE, NATIONAL, BIT, PROGRAM-POINTER and
// FUNCTION-POINTER, so `PIC 1(3) BIT` was a parse error). The optional binarySign applies to the COBOL-2002
// BINARY-CHAR/SHORT/LONG/DOUBLE usages; it is grammatically tolerated after any usageKeyword and rejected by the
// binder for non-binary ones, as is noSignPhrase off PACKED-DECIMAL (§13.18.60.4 GR11, 2023). A 2002 usage word
// that is a USER word at 85 (BIT, NATIONAL, PROGRAM-POINTER, FUNCTION-POINTER — all cobolWord) needs no predicate
// here: in `05 BIT.` the entry-NAME alternative is tried first and wins (an '85 item named BIT), and `05 X BIT.`
// reads as the usage, which the binder / VersionConformancePass then names as the 2002 introduction — the
// superset-parse / bind-narrow direction of DESIGN-version-conformance-pipeline.
usageClause
    : (USAGE IS?)? usageKeyword binarySign? noSignPhrase? floatFormatPhrase*
    ;

// USAGE PACKED-DECIMAL WITH NO SIGN (ISO §13.18.60.2 / GR11 — a COBOL-2023 addition): no trailing sign nibble.
// Grammatically tolerated after any usageKeyword; the binder rejects it on a non-PACKED-DECIMAL usage (COBOLNET1565)
// and rejects an 'S' picture with NO SIGN (SR31, COBOLNET1566). WITH is the conventional optional noise word.
noSignPhrase
    : WITH? NO SIGN
    ;

// The floating-point FORMAT phrases of the USAGE clause (ISO §13.18.60.2 general format, verified against the
// PRINTED page — PDF p.533 / printed 503): `FLOAT-BINARY-32/-64/-128 [ endianness-phrase ]`, and FLOAT-DECIMAL-16/
// -34 followed by a BRACKETED CHOICE-INDICATOR group over { encoding-phrase, endianness-phrase }. §5.2.6.4 gives
// that group its semantics — "When enclosed by brackets, zero or more of the alternatives contained within the
// choice indicators shall be specified, but any single alternative may be specified only once" and "The
// alternatives may be specified in any order" — which IS the `*` written here plus a binder duplicate screen
// (COBOLNET1718). The `*` is on the CLAUSE, not inside usageKeyword, and that placement is load-bearing:
// DataBinder.UsageKeyword derives the canonical keyword from the usageKeyword node, so a phrase INSIDE it would
// glue ("FLOAT-BINARY-32HIGH-ORDER-LEFT") and fall to PictureAnalyzer.ParseUsage's internal-error arm — the exact
// failure that method's own doc comment records for bare `BINARY-CHAR SIGNED` (the W2 loud-guard sweep).
// Grammatically tolerated after ANY usageKeyword, the established binarySign/noSignPhrase posture: the binder
// rejects an endianness-phrase on a non-standard-float usage (COBOLNET1716, §13.18.60.4 GR19c/d scope it) and an
// encoding-phrase on anything but FLOAT-DECIMAL-16/-34 (COBOLNET1717, GR20). The 2014 introduction gate is
// VersionConformancePass ParseArm.VisitUsageClause (Constructs.UsageFloatFormatPhrase2014) — NOT a parse-time
// {is2014()}? predicate (COBOLNET_DESIGN §1.1: this tree has no parse-time edition predicates).
floatFormatPhrase
    : encodingPhrase
    | endiannessPhrase
    ;

// encoding-phrase / endianness-phrase (ISO §13.18.60.2 "where encoding-phrase is" / "where endianness-phrase is").
// ONE definition, THREE citing clauses: the USAGE clause above, the OPTIONS FLOAT-BINARY clause (§11.9.8) and the
// OPTIONS FLOAT-DECIMAL clause (§11.9.9) — the two OPTIONS rules live in CobolParserCore.g4 and reach these
// through the import merge. They are DEFINED here, in the imported fragment, so the reference direction is always
// importing-grammar → imported-rule.
encodingPhrase
    : BINARY_ENCODING
    | DECIMAL_ENCODING
    ;

endiannessPhrase
    : HIGH_ORDER_LEFT
    | HIGH_ORDER_RIGHT
    ;

usageKeyword
    : DISPLAY
    | COMPUTATIONAL
    | COMPUTATIONAL_1
    | COMPUTATIONAL_2
    | COMPUTATIONAL_3
    | COMPUTATIONAL_4
    | COMPUTATIONAL_5
    | COMP
    | COMP_1
    | COMP_2
    | COMP_3
    | COMP_4
    | COMP_5
    | FLOAT_SHORT
    | FLOAT_LONG
    | FLOAT_EXTENDED
    | FLOAT_BINARY_32 | FLOAT_BINARY_64 | FLOAT_BINARY_128     // §13.18.60.4 GR14-16 IEEE binary32/64/128 (2014)
    | FLOAT_DECIMAL_16 | FLOAT_DECIMAL_34                       // §13.18.60.4 GR17-18 IEEE decimal64/128 (2014)
    | BINARY_CHAR
    | BINARY_SHORT
    | BINARY_LONG
    | BINARY_DOUBLE
    | BINARY
    | PACKED_DECIMAL
    | INDEX
    | MESSAGE_TAG           // §13.18.60.4 GR9 (COBOL-2023, Annex E.2 item 25) — the MCS message-tag usage. DECLINED
                            // (docs/CONFORMANCE.md §4 item 1 / Annex A.3 item 4): recognize-only, refused BY NAME
                            // at bind (COBOLNET1943). ⛔ It is in the CLAUSE grammar, not just the reserved-word
                            // table, because §13.18.60.2 prints `[ USAGE IS ]` as OPTIONAL for every usage: without
                            // an arm here the bare `01 M MESSAGE-TAG.` fell to the §13.16.2 catch-all and bound with
                            // NO usage at all (kb/Work PB487).
    | NATIONAL
    | BIT
    | dataPointerUsage       // USAGE POINTER [[TO] type-name] (§13.18.60.2; the operand makes a RESTRICTED data-pointer, GR23; TO is optional, kb/Work PB848)
    | programPointerUsage    // USAGE PROGRAM-POINTER [[TO] prototype] (§13.18.60 GR24/GR25, 2002) — introduction-gated post-bind (VersionConformancePass UsageConstructId)
    | functionPointerUsage   // USAGE FUNCTION-POINTER [TO] prototype (§13.18.60 GR26, 2014) — the operand is MANDATORY, screened in DataBinder (COBOLNET1958)
    | objectReferenceUsage   // USAGE OBJECT REFERENCE [class] (OO/2002) — introduction-gated at BIND time (PicInfo.ParseUsage → ConstructRegistry.Check), like NATIONAL/BIT/POINTER above
    ;

// ── THE THREE POINTER USAGES — `POINTER [ TO type-name-1 ]`, `FUNCTION-POINTER TO function-prototype-name-1`,
// `PROGRAM-POINTER [ TO program-prototype-name-1 ]` (ISO §13.18.60.2, RENDERED from the printed page — PDF p.533
// = printed folio 503). ⛔ TO IS AN OPTIONAL WORD IN ALL THREE (kb/Work PB848): the underline rule sits under
// POINTER / FUNCTION-POINTER / PROGRAM-POINTER only, never under TO, and §5.2.3 makes a non-underlined word an
// optional word "that may be written to add clarity", so `USAGE POINTER T`, `USAGE FUNCTION-POINTER P` and
// `USAGE PROGRAM-POINTER P` are conforming spellings. UNDERLINING decides required-vs-optional, never bracketing:
// the operand's BRACKETS (POINTER's and PROGRAM-POINTER's, not FUNCTION-POINTER's) are a different question,
// answered in DataBinder (FUNCTION-POINTER's mandatory operand, COBOLNET1958) — so every rule below is the same
// superset `(TO? operand)?` and the binder reads the OPERAND's presence, never the TO token's.
//
// ⛔ THE TO-LESS OPERAND MAY NOT SWALLOW THE NEXT CLAUSE. The slot is a bare `cobolWord` at the END of a clause,
// and ANTLR takes the first viable alternative, so any keyword that both BEGINS a §13.16.2 data-description
// clause and reaches `cobolWord` would be bound as the type-name. The reservation gate inside `cobolWord`
// (kb/Work PB693) stops most of them only where they are RESERVED — and not at all for the §15 function names it
// deliberately exempts: BIT and NATIONAL are bare USAGE keywords AND `cobolWord` alternatives, so without a guard
// `01 P USAGE POINTER BIT.` bound BIT as a type-name (MEASURED — PointerUsageOperandDriftTests caught it), as would
// PROPERTY under --permissive or at an edition before its reservation. So the TO-less arm is guarded by
// `pointerOperandHere()` (CobolParserCoreBase), which refuses a token in FOLLOW(usageKeyword) — the USAGE tail
// phrases (HIGH-ORDER-RIGHT was measured swallowed too) and the next clause —
// COMPUTED from the generated ATN — never a hand list — so a clause added to the closed format is excluded
// automatically. With TO written, the word IS the operand; the predicate guards only the TO-less arm.
//
// Each is a RULE, not a bare terminal with a tail: DataBinder.UsageKeyword derives the canonical keyword from the
// node, so the operand must not be glued into it ("POINTERT" — the OBJECT REFERENCE precedent).
//
// POINTER: the operand declares a RESTRICTED data-pointer, §13.18.60.4 GR23 ("If type-name-1 is specified, this
// data item is a restricted data-pointer"); §13.18.60.3 SR18 requires the SUBJECT to carry TYPEDEF (binder).
dataPointerUsage
    : POINTER (TO cobolWord | {pointerOperandHere()}? cobolWord)?
    ;

// PROGRAM-POINTER: a program-pointer data item (GR24); the operand declares a RESTRICTED program-pointer (GR25 —
// only NULL or a same-signature program's address); §13.18.60.3 SR19 requires TYPEDEF on the subject (binder).
programPointerUsage
    : PROGRAM_POINTER (TO cobolWord | {pointerOperandHere()}? cobolWord)?
    ;

// FUNCTION-POINTER: the operand is MANDATORY (unbracketed on folio 503; GR26 takes every function-pointer's
// signature restriction from it) — the superset `?` parses its omission so the binder can name the rule.
functionPointerUsage
    : FUNCTION_POINTER (TO cobolWord | {pointerOperandHere()}? cobolWord)?
    ;

// SIGNED (default) / UNSIGNED phrase on a fixed-width binary usage (ISO §13.18.60).
binarySign
    : SIGNED
    | UNSIGNED
    ;

// OCCURS Clause. Each fixed bound (integer-1/integer-2) is an integerOperand: an integer literal OR — COBOL-2002
// §13.10.3 SR2 — an integer CONSTANT-NAME ("if constant-name-1 is an integer, it may also be used to specify …
// repetition"; the OCCURS format's integer positions are literal positions, so a constant substitutes there per
// §13.10.4 GR1/GR3). The constant is resolved at BIND time from the compile-time constant table
// (DataBinder.Constants.cs) — a cobolWord bound in a program with no such constant rejects loud (COBOLNET1547).
occursClause
    : OCCURS integerOperand (TO integerOperand)? timesKeyword?
      (DEPENDING ON? dataReference)?
      occursStepPhrase?
      occursKeyClause*
      (INDEXED BY? dataReferenceList)?
    // Format 4 — a DYNAMIC-capacity table (ISO §13.18.38 Format 4, COBOL-2014; D9). LL-disjoint from Format 1/2
    // on the token after OCCURS (DYNAMIC is not an integerLiteral). occursDynamicPhrase* is a SUPERSET parse: the
    // printed once-each, CAPACITY-FROM-TO-INITIALIZED order (§13.18.38.2 with §5.2.1) is enforced at bind time
    // (DataBinder.Odo.cs#NarrowToDataDivisionFormats, COBOLNET2789; kb/Work PB1265), as is SR28 (COBOLNET1522).
    // The Format 1/2 alternative above is likewise the union of Formats 1, 2 and 3; the same binder site narrows a
    // data description entry to Formats 1 and 2, and DataBinder.Reports.cs#ReportOccursOf a report group entry to
    // Format 3. Edition-gated so a pre-2014 probe upgrades to COBOLNET0900.
    | OCCURS DYNAMIC occursDynamicPhrase* occursKeyClause* (INDEXED BY? dataReferenceList)?   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitOccursClause (rearch 14g.3)
    ;

// OCCURS … STEP integer-3 — the REPORT-WRITER format's own phrase (ISO §13.18.38.2 Format 3:
// `OCCURS [ integer-1 TO ] integer-2 TIMES [ DEPENDING ON data-name-1 ] [ STEP integer-3 ]`, verified against the
// printed general-format diagram). Without it a conforming repeating report entry died at the word STEP.
// STEP is a §8.10 CONTEXT-SENSITIVE word ("OCCURS clause"), NEVER reserved at any edition, so it gets NO lexer
// token — a token would have to join `_dataNameTokens` to keep `STEP (1)` subscripting a user item named STEP, and
// that set is generated from the §8.9 RESERVED word table. It is therefore read as TEXT, the LOCALE / ATTRIBUTE
// precedent (`pictureLocaleAhead`). Not edition-gated: the phrase is legal only in a report group description
// entry, which the binder enforces (DataBinder.Odo.cs#NarrowToDataDivisionFormats refuses it in a data description
// entry, COBOLNET2789; §13.18.38.2 — the TO/DEPENDING/STEP shape is Format 3), and no other clause
// can begin with a user word here, so there is no earlier-edition reading to protect.
occursStepPhrase
    : {occursStepAhead()}? cobolWord integerOperand
    ;

occursDynamicPhrase
    : CAPACITY IN? dataReference   // CAPACITY IN data-name-3 (the current-capacity register, §13.18.38 GR15)
    | FROM integerOperand         // integer-4 — the minimum / initial capacity (GR16)
    | TO integerOperand           // integer-5 — the expected capacity (GR17)
    | INITIALIZED                 // seed new occurrences per §8.5.1.9.5
    ;

occursKeyClause
    : (ASCENDING | DESCENDING) KEY? IS? dataReference+
    ;

timesKeyword
    : TIMES
    ;

// REDEFINES Clause
redefinesClause
    : REDEFINES dataReference
    ;

// RENAMES (Level 66)
renamesClause
    : RENAMES dataReference ((THRU | THROUGH) dataReference)?
    ;

// VALUE Clause — IS is optional noise word
// For level-88 condition entries, valueItem supports THRU ranges.
// Format 3 (§13.18.63): WHEN SET TO FALSE IS literal for condition-names;
//                        IN alphabet-name for character comparisons.
// ⛔ NO PER-WORD LOOP GUARD (kb/Work PB805). Whether the next word is one more operand or the keyword of what
// follows is decided in ONE place: every keyword-token alternative of `cobolWord` carries the generated
// `{!keywordContinuesHere()}?`, which ANTLR hoists into this loop's decision and which ends the list wherever
// the entry can read the word as its next clause keyword (CobolParserCoreBase; the follow set comes from the
// ATN). The hand-written PROPERTY guard that stood here covered one of five clause-initial words the
// migration mode restores — GROUP-USAGE was swallowed under --permissive — and is gone.
valueClause
    // Format 2 (table, ISO §13.18.63.2, COBOL-2002) — literals keyed to occurrence subscripts by a MANDATORY FROM
    // phrase. This arm is FIRST: the mandatory FROM terminates the operand loop, so ALL(*) selects it whenever FROM
    // is present; a bare-list-first ordering would consume the literals then die on FROM (DEVLOG note). FROM/TO are
    // not subscript-trigger words, so `FROM (1)` lexes as DEFAULT LPAREN/RPAREN. The 2002 introduction gate is
    // recognition-fired by VersionConformancePass ParseArm.VisitValueClause on a valueClauseTablePhrase.
    : (VALUE | VALUES) (IS | ARE)? valueClauseTablePhrase+
    | (VALUE | VALUES) (IS | ARE)? valueItem (COMMA? valueItem)*
      // ⛔ WHEN, SET AND TO ARE OPTIONAL WORDS (kb/Work PB695 family 2 — the sibling sweep the audit cannot see,
      // because a word REQUIRED INSIDE an optional group never reaches its candidate list). Measured off the
      // printed §13.18.63.2 format 3 (PDF p546 / folio 516): the bracket reads `[ WHEN SET TO FALSE IS
      // literal-4 ]` and the ONLY rule on that line sits under FALSE (x 228.02–255.10, 92% cover) — WHEN, SET,
      // TO and IS carry no rule at all, and the transcription's own figure note agrees. §8.3.2.4.3 therefore
      // makes `88 X VALUE 1 FALSE 0` conforming source. FALSE_ stays the required anchor and is reachable from
      // neither `valueClauseOperand` nor `cobolWord`, so the greedy operand loop above cannot swallow it.
      // ⚠ WHEN is underlined in format 5's `[ WHEN condition-1 ]` (same page, x 215.18–243.66) — a different
      // phrase in a different format, and `validateValidPhrase` keeps it REQUIRED.
      //
      // ⛔ AND THE TWO TRAILING BRACKETS WERE IN THE WRONG ORDER. The printed format 3 ends
      // `… [ IN alphabet-name-1 ]` and puts `[ WHEN SET TO FALSE IS literal-4 ]` on the LINE AFTER it, and
      // format 5 likewise brackets `[ IN alphabet-name-1 ]` before its `{IS INVALID | ARE VALID}` tail — so
      // IN comes FIRST in both. The rule had them reversed, which rejected the printed spelling
      // `88 CN VALUE 1 IN AL1 WHEN SET TO FALSE 0` (COBOL0001) while accepting an order no format prints.
      // Found by the same sweep; §5.2.6.2 makes bracket ORDER part of the format, not free.
      // ⚠ `IN` ITSELF IS UN-UNDERLINED (an optional word — formats 3 AND 5, PDF p546 / folio 516) AND IS
      // DELIBERATELY NOT RELAXED HERE. §13.10.3 SR2 lets a constant-name stand "anywhere that a format specifies
      // a literal", so alphabet-name-1 with IN omitted is indistinguishable BY POSITION from one more literal-2 —
      // the greedy operand loop above always takes it, and an `IN?` here would be a dead branch. It is decided
      // where it CAN be: by symbol, in the binder (DataBinder.RangeAlphabetPhraseOf, kb/Work PB983) — §8.3.2.2
      // puts alphabet-names and constant-names in disjoint types, so a LAST operand that names a declared
      // alphabet is the phrase. alphabet-name-1 is a user-defined word, so the IN spelling takes `cobolWord`
      // (the EVALUATE range-expression's own spelling), not the narrower IDENTIFIER token.
      (IN cobolWord)?
      valueClauseFalsePhrase?
      // Format 5 (content-validation-entry, ISO §13.18.63.2) — the DECLINED A.4.14 tail
      // `[IS|ARE] {INVALID|VALID} [WHEN condition-1]`, refused by name with COBOLNET1708 at bind. Written as
      // a tail of the condition-name arm because formats 3 and 5 share their literal/THRU list; the printed
      // format-5 figure differs only in dropping the IS/ARE connective before the list, which this arm's
      // `(IS|ARE)?` already tolerates as a superset. {is2002()}? at the left edge of the optional block: VALID
      // is a user-defined word at COBOL-85 (§8.9).
      ({is2002()}? validateValidPhrase)?
    ;

// Format 3's SECOND literal position: `[ WHEN SET TO FALSE IS literal-4 ]` (ISO §13.18.63.2 format 3 — the
// bracket printed on the line after the IN phrase; WHEN/SET/TO/IS are optional words, see the valueClause
// comment above for the measurement).
// ⛔ THE OPERAND IS `valueClauseOperand`, NOT `literal` (kb/Work PB732). literal-4 is a literal POSITION like
// every other operand of the clause, so §13.10.3 SR2 ("constant-name-1 may be used anywhere that a format
// specifies a literal") and §8.3.3.6.2 format 7 (symbolic-character-1) both reach it — and the narrower
// `literal` rule REJECTED both, with COBOL0001 + COBOL0309 "A literal value is expected here, not a data-name"
// on the conforming `88 CN VALUE 1 WHEN SET TO FALSE IS K`. Its own rule (rather than an inline group) so the
// binder can NAME the node it screens instead of relying on "the only direct valueClauseOperand child".
valueClauseFalsePhrase
    : WHEN? SET? TO? FALSE_ IS? valueClauseOperand
    ;

// One Format-2 table phrase: a literal list, then FROM (subscript-1 …) [TO (subscript-2 …)]. The subscripts are
// integer literals (SR19), one per OCCURS dimension (SR20/SR21) — validated at bind (COBOLNET1585-1590).
// ⛔ THE SUBSCRIPTS ARE `signedIntegerLiteral`, NOT `integerLiteral` (kb/Work PB553). §13.18.63.2 Format 2
// prints these slots as `subscript-1` / `subscript-2`, NOT as `integer-n`, so §5.5 1)'s unsigned-and-nonzero
// default does not reach them; §13.18.63.3 SR19 — "Subscript-1 and subscript-2 shall be integer numeric
// literals" — routes through §5.5 2) a) to §8.3.3.3.2, which admits a leading sign. `FROM (+1) TO (+3)` is
// therefore conforming source and used to be `error COBOL0001: unexpected '+'`. A NEGATIVE or zero subscript
// still fails, but by §8.4.2.3.4 GR2 ("The value of a subscript shall be a positive integer") through the
// existing SR20/SR21 range screen's named COBOLNET1586/1587 — never as a parse error.
valueClauseTablePhrase
    : valueClauseOperand (COMMA? valueClauseOperand)*
      FROM LPAREN signedIntegerLiteral (COMMA? signedIntegerLiteral)* RPAREN
      (TO LPAREN signedIntegerLiteral (COMMA? signedIntegerLiteral)* RPAREN)?
    ;

valueItem
    : valueClauseRange
    | valueClauseOperand valueClauseOperand*
    ;

// SIGN Clause
signClause
    : (SIGN IS?)? (LEADING | TRAILING) (SEPARATE CHARACTER?)?
    ;

// JUSTIFIED / SYNCHRONIZED
justifiedClause
    : (JUSTIFIED | JUST) RIGHT?
    ;

syncClause
    : (SYNCHRONIZED | SYNC) (LEFT | RIGHT)?
    ;

// BLANK [WHEN] ZERO (§13.18.8.2) — WHEN is optional; ZERO is the keyword and the ONLY spelling admitted: ZEROS and
// ZEROES are distinct reserved words, interchangeable with ZERO only as the figurative constant (kb/Work PB510).
blankWhenZeroClause
    : BLANK WHEN? ZERO
    ;

// 88-LEVEL CONDITION ENTRIES — handled through valueClause with THRU support.
// Level number and condition name are already consumed by dataDescriptionEntry.
// The conditionEntry88 / valueSet / valueRange rules have been removed;
// valueClause now supports THRU ranges via valueItem for level-88 entries.

// ==========================================
// INITIALIZE (§14.9.20)
// ==========================================

initializeStatement
    : INITIALIZE initializeOperandList (WITH? FILLER)?
      initializeCategoryToValue?
      initializeReplacingPhrase?
      initializeDefaultPhrase?
    ;

// `{ identifier-1 } …` (§14.9.20.2). Relaxing THEN and TO to the optional words the printed format makes them
// leaves `INITIALIZE X DEFAULT` as the shortest legal spelling of `INITIALIZE X THEN TO DEFAULT`, and DEFAULT
// rides `cobolWord`. ⛔ The list ends at DEFAULT through the ONE generated `{!keywordContinuesHere()}?` on
// cobolWord's keyword alternatives (kb/Work PB805) — the follow set of this loop contains the DEFAULT phrase —
// never through a per-word guard here (the `reservedHere("DEFAULT")` predicate that stood here was one of
// three hand-written families). The keyword reading wins at every edition: the union grammar parses the
// DEFAULT phrase at 85 too and refuses it BY NAME there.
initializeOperandList
    : dataReference (COMMA? dataReference)*
    ;

// [ALL | category-name] TO VALUE (§14.9.20)
// ⛔ TO IS AN OPTIONAL WORD (kb/Work PB695 family 2). Measured off the printed §14.9.20.2 figure (PDF p667 /
// folio 637): the phrase's underlines fall on ALL, VALUE and the thirteen category names — TO is plain, so
// §8.3.2.4.3 makes `INITIALIZE X ALL VALUE` conforming source. VALUE stays the required anchor (it is not in
// cobolWord, so the preceding initializeOperandList loop cannot swallow it) and the COBOL-2002 gate keys on this
// SUBRULE's presence (VersionConformancePass.VisitInitializeStatement), never on `ctx.TO()`.
// ⛔ THE `?` ON THE CHOICE IS AN ERROR-RECOVERY AFFORDANCE, NOT A PERMISSION (kb/Work PB415). The printed figure
// draws `{ ALL | category-name }` inside a plain BRACE, and §5.2.6.3 says "one of the alternatives contained
// within the braces shall be explicitly specified or is implicitly selected" — so `INITIALIZE X TO VALUE` is NOT
// conforming source and InitializeBinder rejects it with COBOLNET1981. The rule keeps the optional subrule so the
// REJECTION IS THE NAMED RULE rather than a COBOL0001 "extraneous input 'TO'" that never says which rule was
// broken; with TO itself optional there is no token the parser could blame. ⛔ Do NOT read the `?` as a licence:
// the binder's check is the enforcement, and InitializeLaneDriftTests pins that it fires.
initializeCategoryToValue
    : (ALL | initializeCategory)? TO? VALUE
    ;

initializeReplacingPhrase
    : THEN? REPLACING initializeReplacingItem+
    ;

// THEN TO DEFAULT (§14.9.20). DEFAULT is now a token (added for the OPTIONS paragraph, ISO §11.9.6), so the
// phrase matches it directly — tightening the grammar to reject `TO <other-word>` (which the old IDENTIFIER form
// accepted). DEFAULT remains a legal data-name elsewhere (it is in cobolWord).
// ⛔ TO IS AN OPTIONAL WORD HERE TOO (kb/Work PB695 family 2) — printed §14.9.20.2 (PDF p667 / folio 637) writes
// `[ THEN TO DEFAULT ]` with an underline under DEFAULT alone, so §8.3.2.4.3 makes `INITIALIZE X THEN DEFAULT`
// and `INITIALIZE X DEFAULT` conforming source.
initializeDefaultPhrase
    : THEN? TO? DEFAULT
    ;

// ⛔ ADDITIVE — §14.9.20.3 SR4 states it outright: "a MOVE statement with identifier-2 or literal-1 as the
// SENDING item", so identifier-2 admits a function-identifier (§8.4.3.1.2 Format 1; §8.4.3.2.3 SR1 bars one
// only from a RECEIVING operand). See the writeFrom note above for why this adds an alternative instead of
// collapsing to `moveSendingOperand`: the accessors are load-bearing for the shared legacy binders.
initializeReplacingItem
    : initializeCategory DATA? BY (functionCall | inlineMethodInvocation | dataReference | literal)
    ;

// ⛔ category-name IS A SET, NOT ONE WORD (ISO §14.9.20.2 "where category-name is:"; kb/Work PB415). Rendered off
// the licensed PDF (p667 / printed folio 637): the thirteen words are enclosed by a BRACE carrying CHOICE
// INDICATORS — the pair of bars just inside it — and §5.2.6.4 reads them "one or more of the alternatives
// contained within the choice indicators shall be specified, but any single alternative shall be specified only
// once". So `REPLACING NUMERIC ALPHANUMERIC DATA BY SPACE` is conforming source (at COBOL-85 too: the five
// classic words are 85 words), and `category-name` occupies BOTH phrase slots as a set.
// The rule that produced the defect was a SINGLE-ALTERNATIVE scalar naming five of the thirteen; modelling the
// set here is what makes the fourteenth word one table row instead of a new shape (CLAUDE.md rule 5).
initializeCategory
    : initializeCategoryName+
    ;

// ONE printed category-name word. ⛔ EVERY ONE OF THE THIRTEEN IS UNDERLINED IN THE FIGURE, so each is a required
// word with exactly one spelling — the HYPHENATED one. The two-token `ALPHANUMERIC EDITED` / `NUMERIC EDITED`
// alternatives this rule used to carry are in no edition of the standard (EDITED is not an ISO §8.9 reserved word
// at 85, 2002, 2014 or 2023 and has no reserved-words.json row); they were the sole consumer of the hard EDITED
// lexer token, which shadowed IDENTIFIER and made `01 EDITED PIC X.` a COBOL0001 at every edition. Both are gone.
// EDITION GATING IS NOT HERE: the words that ISO §8.9 reserves above 85 are admitted unconditionally and named by
// VersionConformancePass.VisitInitializeStatement (COBOLNET0900 with the per-word constructs.json row), because
// this position only ever takes a category-name — no user-defined word can reach it, so there is nothing for a
// `userWordHere` predicate to disambiguate and a named gate beats a "no viable alternative".
initializeCategoryName
    : ALPHABETIC
    | ALPHANUMERIC_EDITED
    | ALPHANUMERIC
    | BOOLEAN
    | DATA_POINTER
    | FUNCTION_POINTER
    | MESSAGE_TAG
    | NATIONAL_EDITED
    | NATIONAL
    | NUMERIC_EDITED
    | NUMERIC
    | OBJECT_REFERENCE
    | PROGRAM_POINTER
    ;
