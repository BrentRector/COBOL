// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// SPECIAL-NAMES paragraph and all its clauses.
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolSpecialNames;

options {
    tokenVocab = CobolLexer;
}

// SPECIAL-NAMES.
specialNamesParagraph
    // §12.3.7.2 prints `SPECIAL-NAMES. [clauses] .` and §12.3.7.3 SR31 lets ONE of the two separator periods go
    // when no clause is written (kb/Work PB1090) — the full empty spelling `SPECIAL-NAMES. .` is the second alternative,
    // as the SOURCE-COMPUTER / OBJECT-COMPUTER siblings write theirs. A non-empty paragraph's closing period is its
    // last entry's own DOT?.
    : SPECIAL_NAMES DOT (specialNameEntry+ | DOT)?
    ;

specialNameEntry
    : currencySignClause DOT?
    | decimalPointClause DOT?
    | classDefinitionClause DOT?
    | symbolicCharactersClause DOT?
    | alphabetClause DOT?
    // CRT STATUS / CURSOR (§12.3.7; Annex A.4.2 item 25 — the DECLINED screen module, refused by name at bind
    // with COBOLNET1560). ⚠ RESERVATION-GATED. §8.9 reserves CRT and CURSOR from 2002 ONLY, so at COBOL-85 they
    // are ordinary user words and `SPECIAL-NAMES. CURSOR IS FOO.` is a legal '85 implementor-switch entry that
    // must keep parsing as one — the localeClause precedent exactly. Without the gate the '85 program drew a
    // screen-facility refusal for a switch entry that has nothing to do with screens (kb/Work PB301).
    | {reservedHere("CRT")}?    crtStatusClause DOT?
    | {reservedHere("CURSOR")}? cursorClause DOT?
    | channelClause DOT?
    | reserveClause DOT?
    // MUST precede implementorSwitchEntry: `LOCALE FR IS "fr_FR"` otherwise matches `cobolWord (IS cobolWord)?`
    // as far as the bare word LOCALE, then re-enters as `FR IS "fr_FR"` and dies on the LITERAL with
    // COBOL0308 "a data-name is expected here, not a literal" — a parse error pointing at the wrong token, for a
    // clause the standard defines (fix-queue PB25).
    | localeClause DOT?
    // MUST precede implementorSwitchEntry, for the LOCALE clause's reason: at COBOL-85 §8.9 leaves ORDER
    // user-definable, so `cobolWord`'s gated alternative still admits it and `ORDER TABLE OT1 IS "…"` would
    // otherwise match `cobolWord (IS cobolWord)?` as far as the bare word ORDER, and the entry loop re-enters on
    // TABLE — a keyword token no cobolWord admits — dying on a token the user never wrote a clause around
    // (kb/Work PB101). ORDER became a lexer token at kb/Work PB704, which retired the clause's text predicate but
    // NOT this ordering requirement.
    | orderTableClause DOT?
    // DYNAMIC is reserved at every edition, so it heads no switch entry — but it is listed ahead of
    // implementorSwitchEntry anyway, the LOCALE / ORDER TABLE posture for a clause with a keyword anchor.
    | dynamicLengthStructureClause DOT?
    | implementorSwitchEntry DOT?
    // ⛔ THE §12.3.7.2 GENERAL FORMAT IS CLOSED (kb/Work PB829; RENDERED from the printed page — PDF p320 /
    // folio 290): alphabet-name-clause · CLASS · CRT STATUS · CURRENCY SIGN · CURSOR · DECIMAL-POINT ·
    // dynamic-length-structure-clause · LOCALE · the switch-name/feature-name/device-name entry ·
    // symbolic-characters-clause · ORDER TABLE, and nothing else.
    // ⚠ REACH: `implementorSwitchEntry` above takes a word plus a REQUIRED continuation, and IS is un-underlined
    // in the printed format, so a TWO-word entry (`WIBBLE WOBBLE`) is shape-legal as `device-name-1 [IS]
    // mnemonic-name-3` and never reaches here — what the standard refuses there is §12.3.7.3 SR8 (the name is
    // not one the implementor specifies), a SEMANTIC rule refused by name at bind (COBOLNET2241, kb/Work PB862).
    // This alternative catches what NO arm of the format admits: a lone word (kb/Work PB716), a run carrying a
    // literal, or three or more words.
    | unrecognizedClause DOT?   // ⛔ LAST — refused BY NAME by ClosedFormatPass (COBOLNET1970)
    ;

// LOCALE locale-name-1 IS { external-locale-name-1 | literal-4 } (§12.3.7).
// ⛔ PARSED SO IT CAN BE DIAGNOSED, NOT SO IT CAN BE USED. This is an §A.4.9 item 10 optional-locale element —
// "SPECIAL-NAMES paragraph: LOCALE clause and LOCALE phrases in the ALPHABET clause (12.3.7)" — and WiseOwl COBOL's
// documented non-support of the locale module is conformant per §4.2.7 / §A.4.1 ONLY if it is DIAGNOSED. The
// binder emits COBOLNET1518, the same cited message the LOCALE phrase of LOWER-CASE / NUMVAL-C already gets; a
// raw parse error is not documented non-support, it is an unexplained rejection. Superset-parse / bind-narrow,
// exactly as the rest of this grammar does it.
// ⚠ The predicate is EDITION-GATED (2002+, where §8.9 reserves LOCALE). At COBOL-85 LOCALE is an ordinary user
// word, so `SPECIAL-NAMES. LOCALE IS FOO.` is a legal implementor-switch entry and must keep parsing as one —
// below 2002 localeClauseAhead() is false and this alternative is unreachable.
localeClause
    : {localeClauseAhead()}? formatWord cobolWord IS? (cobolWord | literal)
    ;
// ⚠ Three slots, three KINDS of word (kb/Work PB764): the keyword LOCALE is a formatWord (a use OF the reserved
// word, §5.2.2); locale-name-1 is a user-defined word (§8.3.2.2) and external-locale-name-1 a SYSTEM-NAME
// (§8.3.2.3.1) — §8.3.2.1 rule 1 forbids a reserved word as EITHER, so both stay cobolWord for the §8.9 funnel.

// ORDER TABLE ordering-name-1 IS literal-9 (ISO §12.3.7.2 — the LAST item of the SPECIAL-NAMES general format,
// bracketed and therefore at most once). ORDER *is* a lexer token (kb/Work PB704 — a keyword slot may not borrow
// `cobolWord`, whose §8.9 funnel refuses the word position-blind at 2002+), and so is TABLE, so the clause is
// recognized by its own two keywords and needs no text predicate: the retired `{orderTableAhead()}?` read the
// same word pair by text only because ORDER had no token.
// literal-9 names the cultural ordering table (§12.3.7.4 GR17 — "The implementor specifies the allowable content
// of literal-9"); ordering-name-1 "may be specified only in the STANDARD-COMPARE intrinsic function"
// (§12.3.7.3 SR9). The binder (DataBinder.OrderTableBind) enforces SR10/SR11 and registers the name.
// ⚠ NOT edition-gated, unlike localeClause: TABLE is reserved at EVERY edition, so the pair ORDER + TABLE has no
// competing COBOL-85 reading (nothing else in a SPECIAL-NAMES paragraph can be followed by, or begin with, TABLE)
// — and at 85, where §8.9 leaves ORDER user-definable, the word still reaches `implementorSwitchEntry` through
// `cobolWord`'s gated alternative, so a switch entry named ORDER keeps its '85 reading. This alternative stays
// AHEAD of implementorSwitchEntry in specialNamesEntry so `ORDER TABLE …` is never absorbed as a switch-name.
// Recognizing the clause at every --std is what lets the version pass answer below 2002 with the explanatory
// `order-table-2002` introduction gate rather than a parse error.
orderTableClause
    : ORDER TABLE cobolWord IS? literal
    ;

// dynamic-length-structure-clause (ISO §12.3.7.2; kb/Work PB829 — it had NO rule, so the clause was COBOL0001
// "unexpected 'DYNAMIC'"):
//   DYNAMIC LENGTH [STRUCTURE] dynamic-length-structure-name-1 [IS]
//       { |[SIGNED] [SHORT] PREFIXED|  |DELIMITED| }  |  physical-structure-name-1
// ⛔ RENDERED from the printed page (PDF p321 / folio 291), not the OCR: DYNAMIC, LENGTH, SIGNED, SHORT, PREFIXED and DELIMITED are
// underlined; STRUCTURE and IS are not, so both are optional words (§5.2.3). The PREFIXED / DELIMITED pair sits in
// CHOICE INDICATORS (§5.2.6.4 — braces: ONE OR MORE of the alternatives, any order, each at most once), which the
// grammar writes as `(…)+` and `ChoiceIndicators.AtMostOnce` reads the at-most-once half of at bind — the one
// reader of that rule. STRUCTURE, SHORT and PREFIXED are not lexer tokens (context words, never reserved), so they
// are recognized by TEXT through the parser base's one `Word` funnel; each predicate sits at the LEFT EDGE of its
// own rule, where it steers prediction (a mid-alternative predicate throws instead of steering). The clause is a
// COBOL-2014 introduction, gated at its node by VersionConformancePass (`dynamic-length-structure-2014`).
dynamicLengthStructureClause
    : DYNAMIC LENGTH dynamicLengthStructureWord? cobolWord IS? dynamicLengthLayout
    ;

dynamicLengthStructureWord
    : {wordAhead("STRUCTURE")}? IDENTIFIER
    ;

dynamicLengthLayout
    : (dynamicLengthPrefixedPhrase | dynamicLengthDelimitedPhrase)+
    | cobolWord                                   // physical-structure-name-1 (§12.3.7.3 SR32 — implementor names)
    ;

// [SIGNED] [SHORT] PREFIXED — the length-field prefix (SPECIAL-NAMES paragraph, §12.3.7.4 GR18).
dynamicLengthPrefixedPhrase
    : SIGNED? dynamicLengthShortWord? dynamicLengthPrefixedWord
    ;

dynamicLengthShortWord
    : {wordAhead("SHORT")}? IDENTIFIER
    ;

dynamicLengthPrefixedWord
    : {wordAhead("PREFIXED")}? IDENTIFIER
    ;

// DELIMITED — the binary-zero delimiter (SPECIAL-NAMES paragraph, §12.3.7.4 GR19). A rule of its own so the at-most-once reader sees it as a
// context like its sibling.
dynamicLengthDelimitedPhrase
    : DELIMITED
    ;

// SPECIAL-NAMES switch entry: switch-name-1 [IS mnemonic-name-1] [ON [STATUS] [IS] condition-name-1] [OFF [STATUS] [IS]
// condition-name-2] (ISO §12.3.7.2). ⛔ MEASURED, NOT TRANSCRIBED (kb/Work PB695, §5.2.3 / §8.3.2.4.3): on printed folio 290 the
// switch rows carry EXACTLY TWO underline rules — under ON and under OFF. IS and STATUS are un-underlined in
// every one of the three lines, so `SW1 MN1`, `ON STATUS C1`, `ON C1` and `OFF C2` are all conforming spellings
// of the same entry. The IS of `[IS mnemonic-name-1]` was demanded here and the ON arm's STATUS/IS were split
// across two alternatives that between them still could not spell `ON STATUS condition-name-1`.
// ⛔ A CONTINUATION IS REQUIRED (kb/Work PB716; RENDERED — PDF p320 / folio 290). The switch arm's continuation
// is in BRACES — `IS mnemonic-name-1 [ |ON …|OFF …| ]` or `{ |ON …|OFF …| }` — and the feature-name and
// device-name arms require `IS mnemonic-name`, so NO arm is a bare word. The old `cobolWord (IS? cobolWord)?
// switchOnClause? switchOffClause?` had a one-word minimum spelling, and inside the unbounded `specialNameEntry*`
// loop every word left over after a clause became a complete, silent entry (`CLASS DIGITS IS "0" ZOTZOT.` ran).
// Which system-name the first word IS — and so which arm it may be written in — is §12.3.7.3 SR8's implementor
// table (Binding/ImplementorNames.cs, COBOLNET2241), a semantic rule the grammar cannot decide: the three arms
// share one shape here and the NAME picks the arm.
implementorSwitchEntry
    : cobolWord ( IS? cobolWord switchStatusPhrases?
                | switchStatusPhrases
                )
    ;

// The CHOICE INDICATORS of both switch-arm status groups (§5.2.6.4 — "any single alternative shall be specified
// only once", in any order): ON then optional OFF, or OFF then optional ON. Spelled as the two orders rather than
// `(switchOnClause | switchOffClause)*` so "each at most once" is the SHAPE and needs no bind-time count; the old
// `switchOnClause? switchOffClause?` refused the legal `OFF STATUS IS C2 ON STATUS IS C1` with COBOL0001.
switchStatusPhrases
    : switchOnClause switchOffClause?
    | switchOffClause switchOnClause?
    ;

// ONE alternative, not two: STATUS is an optional word, so `ON STATUS? IS? …` IS the printed format. The old
// two-arm spelling was a hand-written power set of the same two optional words and, as such power sets do, it
// dropped a member (STATUS written, IS omitted) — see OptionalWordSubsetDriftTests for the mechanical form.
switchOnClause
    : ON STATUS? IS? cobolWord
    ;

switchOffClause
    : OFF STATUS? IS? cobolWord
    ;

// CURRENCY SIGN IS literal [WITH PICTURE SYMBOL literal]
// PICMODE exploit: PICTURE triggers PIC token + pushes PICMODE, which captures
// "SYMBOL" as PIC_STRING. Parser sees: WITH PIC PIC_STRING literal.
// Semantic validation ensures PIC_STRING == "SYMBOL".
// ⛔ WITH IS AN OPTIONAL WORD (kb/Work PB695): folio 290 prints `[ WITH PICTURE SYMBOL literal-8 ]` with rules
// under PICTURE and SYMBOL only — `CURRENCY SIGN IS "$" PICTURE SYMBOL "#"` is conforming and was rejected.
currencySignClause
    : CURRENCY SIGN? IS? literal (WITH? PIC PIC_STRING literal)?
    ;

// [ DECIMAL-POINT IS COMMA ] — folio 290 underlines DECIMAL-POINT and COMMA, never the IS (kb/Work PB695).
decimalPointClause
    : DECIMAL_POINT IS? IDENTIFIER    // DECIMAL-POINT IS COMMA (COMMA is IDENTIFIER)
    ;

// CLASS class-name-1 [FOR {ALPHANUMERIC|NATIONAL}] IS {literal-5 [THROUGH literal-6]}... [IN alphabet-name-4]
// ⛔ THE FOR PHRASE FOLLOWS class-name-1 AND PRECEDES IS (kb/Work PB716; RENDERED — PDF p320 / folio 290: the
// bracketed FOR group is on the CLASS line, the IS line holds the literals and the IN phrase). It used to be
// spelled AFTER the literals, so the printed `CLASS X FOR NATIONAL IS "0"` was REFUSED (COBOL0305 at FOR) and the
// unprinted `CLASS X IS "0" NATIONAL` was accepted — the exact inverse. No dialect owns the postfix spelling, so it
// is never ACCEPTED; it is PARSED only as misplacedSpecialNamesForPhrase so ClosedFormatPass can refuse it BY NAME
// (kb/Work PB977 — superset-parse / bind-narrow; it drew a bare `COBOL0001: unexpected 'FOR'`). FOR is an optional
// word (kb/Work PB695: folio 290 rules ALPHANUMERIC and NATIONAL, never FOR), so `CLASS X NATIONAL IS "0"` is
// conforming. The phrase is the SHARED specialNamesForPhrase, so its 2002 gate keys on the SUBRULE and not on a
// word the standard lets the user omit.
classDefinitionClause
    : CLASS cobolWord specialNamesForPhrase? IS? classValueSet (IN cobolWord)? misplacedSpecialNamesForPhrase?
    ;

// { literal-5 [ THROUGH literal-6 ] }… — JUXTAPOSED groups (the SPECIAL-NAMES paragraph's §12.3.7.2
// diagram carries its own ellipsis
// on the group; the comma is the optional separator it is everywhere). `(COMMA x)*` REQUIRED a comma between
// groups and rejected `CLASS HEXDIG IS "0" THRU "9" "A" THRU "F"` — legal COBOL at every edition (kb/Work PB60's
// configuration-inheritance golden surfaced it, 2026-08-17; the OO in-line invocation's argumentList had the
// same shape and was swept in the same change).
classValueSet
    : classValueItem (COMMA? classValueItem)*
    ;

classValueItem
    : literal ((THRU | THROUGH) literal)?
    ;

// SYMBOLIC CHARACTERS [FOR {ALPHANUMERIC|NATIONAL}]
//   {name}... {IS|ARE} {integer}... [IN alphabet-name] ...
// N:N positional mapping: first name ↔ first integer, etc. (SPECIAL-NAMES paragraph, §12.3.7)
// ⛔ CHARACTERS, FOR, IS AND ARE ARE ALL OPTIONAL WORDS (kb/Work PB695). Printed folio 292 ("where
// symbolic-characters-clause is:") carries underline rules under SYMBOLIC, ALPHANUMERIC, NATIONAL and IN and
// under nothing else: the one rule on the heading line ends at x=112.24, three points short of CHARACTERS' left
// edge, and the IS/ARE pair is stacked inside a SQUARE BRACKET with no rule under either. So `SYMBOLIC SC1 65`
// is conforming. ⚠ specs/ISO_COBOL.md's figure NOTE for this clause CLAIMED SYMBOLIC *and* CHARACTERS were
// underlined while its own <pre> wrote only <u>SYMBOLIC</u>; the page sides with the <pre>, and THIS COMMIT
// repairs that note (2026-09-06). The earlier "(note repaired)" written here was false for as long as it stood —
// the claim was made in the same change set that decided the grammar, and nothing checked it against the file.
symbolicCharactersClause
    : SYMBOLIC CHARACTERS? specialNamesForPhrase?
      symbolicCharacterEntry+ (IN cobolWord)? misplacedSpecialNamesForPhrase?
    ;

symbolicCharacterEntry
    : cobolWord+ (IS | ARE)? integerLiteral+
    ;

// ALPHABET alphabet-name-1 [FOR ALPHANUMERIC] IS {NATIVE|STANDARD-1|STANDARD-2|literal-phrase…}
// ALPHABET alphabet-name-2 FOR NATIONAL IS {NATIVE|UCS-4|UTF-8|UTF-16|literal-phrase…}   (ISO §12.3.7.2)
// The FOR phrase's ISO position is BETWEEN the name and IS. ⛔ The historical postfix position (after the
// definition) is NOT a superset any dialect owns (kb/Work PB977, CLAUDE.md rule 1's precedence: GnuCOBOL's grammar
// takes the FOR target only between the name and IS; IBM Enterprise COBOL's ALPHABET clause has no FOR phrase at
// all — surveyed from the vendors' published syntax, not measured here): it compiled clean at strict 2023, and it
// is now parsed only as misplacedSpecialNamesForPhrase, refused by name at every edition. UCS-4/UTF-8/UTF-16 are §8.9 CONTEXT-SENSITIVE words (ALPHABET clause
// scope) — they arrive as ordinary cobolWord entries and are recognized BY TEXT in the binder/pass, never
// as lexer keywords (they stay user-definable outside this clause).
// ⛔ IS AND FOR ARE OPTIONAL WORDS (kb/Work PB695). Printed folio 291 ("where alphabet-name-clause is:") rules
// ALPHABET, ALPHANUMERIC, NATIONAL, LOCALE, NATIVE, STANDARD-1, STANDARD-2, UCS-4, UTF-8 and UTF-16 — and
// neither IS in `alphabet-name-1 [ FOR ALPHANUMERIC ] IS …` / `alphabet-name-2 FOR NATIONAL IS …`, nor either
// FOR. `ALPHABET A NATIVE` and `ALPHABET N NATIONAL IS UTF-8` are conforming and were both rejected.
alphabetClause
    : ALPHABET cobolWord specialNamesForPhrase? IS? alphabetDefinition misplacedSpecialNamesForPhrase?
    ;

// ⛔ THE ONE `FOR {ALPHANUMERIC | NATIONAL}` PHRASE OF THE SPECIAL-NAMES PARAGRAPH (ISO §12.3.7.2) — its ALPHABET
// clause, its CLASS clause and its SYMBOLIC CHARACTERS clause
// all print it, and until kb/Work PB695 each spelled its own copy. The copies drifted
// exactly where a copy always drifts: the ALPHABET one was a subrule and its 2002 gate tested the SUBRULE, while
// the other two were inline groups whose gates tested `ctx.FOR()` — an OPTIONAL WORD, so relaxing FOR anywhere
// would have silently switched those two edition gates off. One rule, one gate shape, one place to relax.
// The class word stays REQUIRED, so the rule can never match empty and each site's enclosing `?` keeps its meaning.
specialNamesForPhrase
    : FOR? (ALPHANUMERIC | NATIONAL)
    ;

// ⛔ AN ERROR PRODUCTION, NOT A FORMAT (kb/Work PB977): the FOR phrase written AFTER a clause's definition — the
// position no edition of §12.3.7.2 prints and no dialect owns. It exists so that ONE visitor
// (ClosedFormatPass.VisitMisplacedSpecialNamesForPhrase) refuses it by name for all three FOR-bearing clauses, and
// the binders read it only to RECOVER the class the user evidently meant. A distinct rule, so no clause's
// specialNamesForPhrase() accessor can ever return the misplaced one by mistake.
misplacedSpecialNamesForPhrase
    : specialNamesForPhrase
    ;

// NATIVE, STANDARD-1, STANDARD-2 are dedicated lexer tokens.
alphabetDefinition
    : NATIVE
    | STANDARD_1
    | STANDARD_2
    | alphabetLocalePhrase
    | alphabetEntry (COMMA? alphabetEntry)*
    ;

// `IS LOCALE [ locale-name-2 ]` (§12.3.7.2, either branch; kb/Work PB100/PB101): LOCALE is the phrase's own
// KEYWORD — a formatWord (kb/Work PB764), recognized by text because it has no lexer token (IntrinsicBinder reads
// `LOWER-CASE(x LOCALE …)` as a bare word) — and locale-name-2 a REFERENCE to a SPECIAL-NAMES locale-name
// (§12.3.7.3 SR24). Not edition-gated: below 2002 the ONE construct gate (alphabet-locale-2002) names the phrase.
// ALL(*) prefers the entry list whenever the words that follow cannot end the phrase (`LOCALE ALSO "A"`).
alphabetLocalePhrase
    : {wordAhead("LOCALE")}? formatWord cobolWord?
    ;

// literal-1 [ {THROUGH|THRU} literal-2 | {ALSO literal-3}… ] — ⚠ A DELIBERATE SUPERSET (kb/Work PB790). The printed
// figure (RENDERED — PDF p321 / folio 291) stacks the two phrases in ONE bracket with no choice indicators, so an
// entry carries at most one of them (§5.2.6.2). The rule still admits both so the binder can refuse the pair BY NAME
// (COBOLNET2242, DataBinder.AlphabetLiteralPhrase) instead of a parse error at ALSO that names no rule.
alphabetEntry
    : (cobolWord | literal) ((THRU | THROUGH) (cobolWord | literal))?
      (ALSO (cobolWord | literal))*
    ;

// [ CRT STATUS IS data-name-2 ] — folio 290 rules CRT and STATUS, not IS (kb/Work PB695). The clause is still
// refused by name at bind (COBOLNET1560, the DECLINED screen module); an optional word omitted must reach that
// documented refusal, not a parse error.
crtStatusClause
    : CRT STATUS IS? dataReference
    ;

// [ CURSOR IS data-name-1 ] — folio 290 rules CURSOR alone (kb/Work PB695).
cursorClause
    : CURSOR IS? dataReference
    ;

// CHANNEL integer IS data-name — A VENDOR EXTENSION, not an ISO clause: `grep -c CHANNEL specs/ISO_COBOL.md`
// is 0 and the §12.3.7.2 format lists no CHANNEL. Its IS is therefore not measurable against any printed page
// and must NOT be relaxed by analogy with the clauses above (kb/Work PB695 group 1 — the audit reported it
// only because the SPECIAL-NAMES closure swept up a same-token stranger).
channelClause
    : CHANNEL integerLiteral IS dataReference
    ;

// RESERVE integer [CHANNELS]
reserveClause
    : RESERVE integerLiteral IDENTIFIER?
    ;

