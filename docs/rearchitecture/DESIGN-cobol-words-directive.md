# DESIGN — the `>>COBOL-WORDS` directive (ISO/IEC 1989:2023 §7.3.10)

> **STATUS: IMPLEMENTED (all four increments A–D landed; DEVLOG 962–965).** Subsystem deep-dive for the
> `>>COBOL-WORDS` compiler directive (ISO §7.3.10; Annex D.12; Annex E.3.3 item 12). Design SSOT for this
> subsystem; the plan (`COBOLNET_REARCHITECTURE_PLAN.md` §0) points here. When the design changes, update THIS
> doc in the same change set (process rule 4). All four options (EQUATE/UNDEFINE/SUBSTITUTE/RESERVE) are fully
> supported for reserved, context-sensitive, AND intrinsic-function words, with SR1–SR5 enforced; the token
> rewriter + composed `ReservedWordSet` + the map-aware lexer + the binder intrinsic-synonym resolution are all
> in place. The keyword-omitted intrinsic-synonym `name(args)` form (§6 hazard 2) is the one documented narrow
> advisory (the `FUNCTION synonym(args)` form is the faithful path).

## §1 What `>>COBOL-WORDS` is

`>>COBOL-WORDS` (COBOL-2023, a 2014→2023 addition) lets a source **modify which words are reserved words,
context-sensitive words, and intrinsic-function-names, and prohibit specific user-defined words**, per
compilation group. It is processed **during the text-manipulation stage** (§7.3.10.1), and applies to the
**entire compilation group** — it must appear *before the first IDENTIFICATION DIVISION* (SR1). General
format (§7.3.10.2), exactly one option:

```
                 ⎧ EQUATE literal-1 WITH literal-2   ⎫
                 ⎪ UNDEFINE literal-3                ⎪
>>COBOL-WORDS    ⎪ SUBSTITUTE literal-4 BY literal-5 ⎪
                 ⎩ RESERVE literal-6                 ⎭
```

Semantics (§7.3.10.4 GRs):
- **EQUATE lit1 WITH lit2** (GR2): lit2 becomes a **synonym** for lit1 — usable in any syntax requiring lit1.
- **UNDEFINE lit3** (GR3): lit3 is **no longer reserved/restricted** — usable as any user-defined word; the
  syntax requiring lit3 is no longer available.
- **SUBSTITUTE lit4 BY lit5** (GR4): lit5 **takes over lit4's role** in the syntax; lit4 becomes a user word
  and is no longer reserved.
- **RESERVE lit6** (GR5): lit6 **shall not be used as a user-defined word** (a new reserved word).
- GR6: `>>COBOL-WORDS` does **not** affect compiler-directing statements or directives.

Here lit1/lit3/lit4 are existing **reserved / context-sensitive / intrinsic-function** words (SR3, not a
special-character word); lit2/lit5/lit6 are fresh **user-defined words** (SR4, per §8.3.2.2). Every literal is
an **alphanumeric literal, non-hex, space-free, case-insensitive** (SR2). A COBOL word may appear in a literal
of **at most one** `>>COBOL-WORDS` directive in the group (SR5).

## §2 Mechanism decision — a token retype applied BY THE LEXER + composed ReservedWordSet (owner-directed)

The compiler uses a **static, generated ANTLR lexer**: reserved and context-sensitive words are fixed token
types (`MOVE : 'MOVE'`, `caseInsensitive=true`), and intrinsic-function-names reach the parser as `IDENTIFIER`
after `FUNCTION` (except nine reserved-word collisions listed explicitly in the `functionName` rule). Two prior
scouts concluded EQUATE was doable as pre-lex text substitution but SUBSTITUTE / general UNDEFINE were "not
faithfully implementable on the static lexer" and proposed a not-supported warning.

**The owner superseded that (plan §0): "ONE runtime override layer = `CobolWordsMap` → token retype +
composed `ReservedWordSet`; never regen the grammar per group."** A token retype is **strictly more capable** than
text substitution and faithfully realizes all four options **without** per-group grammar regeneration.

⛔ **The retype is applied BY THE LEXER, to each token as it is emitted (kb/Work PB1372), not by a pass over the
filled stream.** It started life as a post-lex pass (the `ZeroTokenRewriter` pattern), and that could reach none of
the decisions the lexer takes from a keyword and freezes at lex time: PIC's switch to PICMODE, the FUNCTION
argument region, the SUBSCRIPT trigger. A synonym of PIC/PICTURE never entered PICMODE; a synonym of FUNCTION never
opened an argument region; a de-reserved PICTURE still swallowed the next word as a picture string; and every
later reader of the filled stream (`DebuggingLineRewriter`'s source-unit boundaries, kb/Work PB1915) saw the words
the DIRECTIVE had changed. `CobolLexer.NextToken` now applies `CobolWordsRewriter.Plan` (one per group, built from
the map, memoized) FIRST, then records the EFFECTIVE type as the previous token and acts on it (PIC pushes PICMODE
there, not as a rule command). Nothing is retyped a second time downstream. `CobolLexerWordRetypeDriftTests` holds
that no DEFAULT-mode keyword rule carries a mode command.

| Option | Token action (Frontend) | Reserved-set action (Compiler) | Intrinsic action (Compiler) |
|---|---|---|---|
| EQUATE lit1←lit2 | retype `IDENTIFIER("lit2")` → lit1's keyword token type (if lit1 has one) | — | if lit1 is intrinsic: lit2 resolves to lit1's sig |
| UNDEFINE lit3 | retype lit3-keyword-type tokens → `IDENTIFIER` | force `RejectsAt(lit3)=false` | if lit3 is intrinsic: lit3 is no longer a function |
| SUBSTITUTE lit4←lit5 | retype lit4-keyword-type → `IDENTIFIER`; retype `IDENTIFIER("lit5")` → lit4's type | force `RejectsAt(lit4)=false` | if lit4 is intrinsic: lit5 resolves to lit4's sig; lit4 not a function |
| RESERVE lit6 | — (lit6 stays `IDENTIFIER`) | add lit6 as high-confidence reserved-at-edition ⇒ `RejectsAt(lit6)=true` | — |

The rewriter operates on **token TYPE** (keyword→identifier) and **token TEXT** (identifier→keyword), which are
disjoint sets, so within a directive order is irrelevant; across directives SR5 forbids overlap. The
keyword→identifier direction matches on the token's TEXT as well as its type, because one token type can carry
several COBOL words (`PIC : 'PICTURE' | 'PIC'` — the one multi-spelling keyword rule; the figurative spellings
ZERO/ZEROS/ZEROES etc. are one token per word since kb/Work PB510, pinned by `KeywordSpellingDriftTests`) while SR3
names exactly one.

### §2.1 The rewriter is HALF the mechanism — the name-level resolution is the other half

⛔ **This section was wrong until kb/Work PB250, and the wrong version cost a silent defect.** It read: "A word
with **no keyword token type** (a pure intrinsic-function-name, or an unknown SR3 violation) is simply skipped by
the rewriter and handled by the Compiler layer — the layers compose cleanly." The premise — that the only words
without a keyword token type are intrinsic names — is false, and nothing measured it. **Measured against ISO
§8.9 ∪ §8.10 (552 words): 447 are keyword tokens the rewriter can reach; 17 more ARE lexed but publish no ANTLR
literal NAME (a multi-spelling rule), so the token map built from those names could not see them; and 88 are no
token at all** — ANYCASE, LOCALE, HEX, NAT, ANUM, BYTE, CURRENT, ACTIVATING, NESTED, STACK, TOP-LEVEL, the LC_
categories, UCS-4/UTF-8/UTF-16 and the rest, deliberately left as bare `IDENTIFIER`s so the §15 phrase words
parse as ordinary space-separated arguments. For every one of those the retype is a **silent no-op in both
directions**: an EQUATEd synonym never became the keyword (legal source rejected) and an UNDEFINE'd word was
still read as one (a wrong answer with no diagnostic).

The design is therefore **two mechanisms over one rule**:

| | reaches | how |
|---|---|---|
| **token retype** — `CobolWordsRewriter.Plan.Apply`, called by `CobolLexer.NextToken` | a word the lexer makes a keyword TOKEN | retype + re-spell canonically, as the token is emitted |
| **name resolution** — `CobolWordsMap.Resolve` / `.Is` | a word that lexes as a bare `IDENTIFIER` | at each point that classifies a word BY NAME |

`CobolWordsMap.Resolve` is the ONE reading of GR2/GR3/GR4 (canonical for a synonym, `null` for a de-reserved
word, the word itself otherwise) and both mechanisms call it. `CobolKeywordTokens` decides which mechanism owns
a word, and answers mechanically — the ANTLR vocabulary as the fast path, then a **lex probe** (run the real
lexer over the word and read the type it produces) for the multi-spelling rules the vocabulary hides. No
hand-maintained list, so the next such rule is covered automatically.

⛔ **The directive is applied to a word EXACTLY ONCE.** The retype already re-spells what it reaches, so text
taken from a non-`IDENTIFIER` token has been resolved and must be compared RAW; resolving it again reads a
SUBSTITUTE'd literal-4 — canonical and de-reserved at once — as "not a keyword" and loses the synonym the user
wrote. `CobolWordsRewriter.CanonicalWordOf` / `.TokenIs` are the token-aware pair that knows this;
`CobolWordsMap.Is` takes a word as WRITTEN and is for the words the lexer does not tokenize.
The binder's intrinsic-function-name sites hold the answer in ONE value, `FunctionWord` (the written word and its
canonical word, null when the directive removed it), made only by `FunctionWord.OfToken` (a word read off a token,
through `CanonicalWordOf`) or `FunctionWord.OfWrittenWord` (source text no lexer token carries); `DefinitionPermitsArguments`,
`BindIntrinsicCore`, `DataBinder.TryIntrinsicOfThisCompilation` and `IsRepositoryIntrinsic` take it, so none can
resolve a name twice (kb/Work PB1372: `SUBSTITUTE "SUM" BY "TOTAL"` made `FUNCTION TOTAL(1 2 3)` — and the
REPOSITORY specifier and the keyword-omitted `TOTAL(1 2 3)` — "not an intrinsic function", because the second
resolution read the canonical literal-4 as removed).

**Where the name resolution is called** (each is a §8.9/§8.10 word the lexer does not tokenize):
`IntrinsicBinder.KeywordWordOf` — the single funnel every §15 phrase word is read through, so TRIM, FIND-STRING,
SUBSTITUTE, CONVERT, MODULE-NAME, LENGTH and NUMVAL-C/TEST-NUMVAL-C are covered by one call;
`CobolParserCoreBase.Word` — the single text comparison behind every parser predicate (LOCALE, ORDER,
CLASSIFICATION, ATTRIBUTE, the LC_ categories); `SetBinder` (SET LOCALE categories and USER-DEFAULT/
SYSTEM-DEFAULT); `DataBinder.Switches` (the SPECIAL-NAMES LOCALE clause, the ALPHABET LOCALE phrase and the
UCS-4/UTF-8/UTF-16 coded-set names); `CallBinder` and `VersionConformancePass` (`CALL … AS NESTED`, both arms).
A new site that writes its own `string.Equals(text, "KEYWORD")` re-opens the defect.

`CobolWordsReachDriftTests` measures all of this: every lexed §8.9/§8.10 word resolves to a token type, the lex
probe agrees with the vocabulary wherever both answer, the multi-spelling words are reached ONLY by the probe,
the population really is partitioned between the two mechanisms, and `Resolve` implements GR2–GR5.

**Why not text substitution?** Text renaming cannot make a reserved word *stop* being a keyword (UNDEFINE /
SUBSTITUTE lit4) and cannot distinguish keyword vs user-word uses; the token rewriter does both by construction.

## §3 Architecture / layering

`CobolWordsMap` is a **pure-string data carrier** built in the Frontend and threaded to the Compiler, exactly
like `FlagState`/`RefModZeroLengthState`.

- **`Cobol.Net.Editions`** — `CobolWordsMap` (the four normalized operation lists, all words upper-cased);
  `ReservedWordSet` extended with an **overlay** (a `Reserve` set → high-confidence reserved-at-edition, and a
  `Suppress` set from UNDEFINE + SUBSTITUTE-lit4 → forces `RejectsAt=false`). `constructs.json` row
  `cobol-words-directive-2023` (introducedIn 2023, COBOLNET0900). Malformed/SR descriptor
  `CobolWordsDirectiveInvalid` = **COBOLNET1623**.
- **`Cobol.Net.Frontend`** — `CobolWordsDirectiveProcessor` (text stage): parse the four options into a
  `CobolWordsMap`, validate SR1/SR2/SR5, edition-gate (0900 via `ConstructRegistry.Check`), blank the directive
  lines (line-count preserving, the H3 discipline). `CobolWordsRewriter.Plan` (applied by the lexer's `NextToken`,
  primed by `TokenRetypes.PrimeLexer`): retype tokens using `CobolLexer.DefaultVocabulary` (the reverse word→type
  map) as they are emitted, so every lexer decision keyed on a keyword — PIC's mode switch, the FUNCTION argument
  region, the SUBSCRIPT trigger — reads the word the directive made of it (resolves the frozen-lex hazards, §6).
  `Frontend` exposes `CobolWordsMap` and primes every lexer of the group (the main parse and every fragment
  re-parse) with it.
- **`Cobol.Net.Compiler`** — `BinderDriver.Bind` receives the map; `VersionConformancePass` consults a
  **composed** `ReservedWordSet` (replacing the hard-coded `ReservedWordSet.Default` at `ParseArm._reservedWords`)
  so RESERVE rejects (COBOLNET0901) and UNDEFINE/SUBSTITUTE-lit4 no longer reject. `VersionConformancePass.Run`
  also runs the **SR3/SR4 semantic validation** of the map (needs all three registries — `ReservedWords`
  [Editions, §8.9], `ContextSensitiveWords` [Editions, §8.10], `IntrinsicCatalog` [Compiler, §8.11]).
  `IntrinsicBinder` consults the map to resolve function-name synonyms / removals before `IntrinsicCatalog.TryGet`.

**Greenfield/legacy split:** `COBOL-WORDS` is a recognized directive of the ONE roster
(`CompilerDirectiveCatalog`, from the `cobol-words-directive-2023` row's `directiveWords` — it was a member of the
flat `KnownIgnoredDirectives` name set until kb/Work PB725 replaced that set with the registry-derived catalog), so
legacy callers still blank it; `Frontend.LeftDirectives` lets the greenfield pipeline's emitting-branch directive
survive to the stage below. Legacy oracle stays byte-identical. ⛔ The 2023 introduction gate is NOT emitted by
that stage any more: it fires once, at the directive-recognition point, like every other directive's.

## §4 The four options — realization detail

Let `W→T` be the reverse map `CobolLexer.DefaultVocabulary` yields (upper-cased literal name → token type),
covering every **reserved word and context-sensitive word** (they are `'LITERAL'` lexer tokens). `IDENTIFIER`
is the user-word token type.

- **EQUATE lit1 WITH lit2.** If `lit1∈W→T` (reserved/context word): every `IDENTIFIER` token whose upper text
  == lit2 is retyped to `T(lit1)` (text preserved for source fidelity). If lit1 is instead an intrinsic-name
  (no token type): the map records `equateFn[lit2]=lit1`; the binder resolves.
- **UNDEFINE lit3.** If `lit3∈W→T`: every token of type `T(lit3)` is retyped to `IDENTIFIER`, AND lit3 is added
  to the ReservedWordSet suppress set (so a base-reserved lit3 used as a user word is not COBOLNET0901-rejected)
  AND to the lexer's map-aware data-name set. If lit3 is intrinsic-only: the map records `undefFn+=lit3`.
- **SUBSTITUTE lit4 BY lit5.** If `lit4∈W→T`: tokens of type `T(lit4)` → `IDENTIFIER` (lit4 suppressed +
  data-name-aware, as UNDEFINE); `IDENTIFIER("lit5")` → `T(lit4)`. If lit4 intrinsic-only: `substFn[lit5]=lit4`,
  `undefFn+=lit4`.
- **RESERVE lit6.** lit6 added to the ReservedWordSet reserve overlay (high-confidence, reserved at the target
  edition) ⇒ `VisitCobolWord`→`RejectsAt`→COBOLNET0901 at every user-word occurrence.

## §5 Syntax-rule validation (split by where the facts live)

- **SR1 (before first ID DIVISION)** — Frontend: the boundary is the first line `CompilationUnitStart.IsAt` accepts (the
  `IDENTIFICATION DIVISION` / `ID DIVISION` header, or, the header being optional, the unit's own `PROGRAM-ID` /
  `CLASS-ID` / `FUNCTION-ID` / `INTERFACE-ID` paragraph — §11.2.1, kb/Work PB829), **read through the group's own
  synonyms** (kb/Work PB1373): after `>>COBOL-WORDS EQUATE "IDENTIFICATION" WITH "IDENT"` the line `IDENT DIVISION.` IS
  the header (§7.3.10.4 GR2), so the entries in effect at the line (a POP that withdrew one has taken it back) rewrite the
  line's first two words before the test (`IsAt(trimmed, map)`). `CobolWordsDirectiveProcessor.Process` returns that line
  as `FirstUnitLine` (the third result), and `DirectiveSiteProcessor.JudgeFirstUnitPlacement` is the ONE judge of the row's
  `BeforeFirstCompilationUnit` placement — for the directive and for a PUSH/POP naming it (§7.3.20.3 SR2, §7.3.22.3
  SR2): a site after that line ⇒ COBOLNET2652 (it was COBOLNET1623 before kb/Work PB1377). The site stage cannot judge it on
  the raw text, because it runs before the directive stage and so before any synonym exists.
- **Operand reading (§8.3.5 2, 5)** — the separator comma and semicolon (each followed by a space) are read as the space they
  stand for ONCE, by the directive-line parse (`CompilerDirectiveLine.SeparatorsAsSpaces`), for every directive operand, so
  `>>COBOL-WORDS EQUATE "DISPLAY", WITH "SHOW"` and `>>TURN EC-SIZE; CHECKING ON` are the space-only spellings (a comma or
  semicolon ending the operand stays: §7.3.3 SR3/SR4 allow only spaces and an inline comment after a directive). The
  literal reader then takes a doubled quotation symbol as one character (§8.3.3.2.3 3)) and gives a literal with no closing
  delimiter, or one the next character does not separate, as DEFECTIVE ⇒ COBOLNET1623 (SR2: "Each literal shall be an
  alphanumeric literal").
- **SR2 (alphanumeric literal, non-hex, space-free)** — Frontend, per literal at parse time. A WORD in a literal
  slot is a compilation-variable-name (§7.3.11.4 GR1, kb/Work PB1368): it must be defined before the directive
  (the `>>DEFINE` timeline's `DefinitionAt`, the same fold `CONSTANT … FROM` reads), its value alphanumeric (the slot's
  category), and the literal its DEFINE WROTE passes the same SR2 screen — so a variable defined `AS X"…"` is refused as
  hexadecimal format. Otherwise COBOLNET1623.
- **SR5 (a word in ≤1 directive's literals)** — Frontend: a group-wide multiset of every literal's content;
  a repeat ⇒ COBOLNET1623 (SR5). (Both the modified word and its substitute count, per D.12.1.)
- **SR3 (lit1/3/4 = reserved OR context OR intrinsic; not special-character)** — Compiler: ONE population predicate,
  `VersionConformancePass.IsReservedContextOrIntrinsic`, shared by SR3 and SR4 and read at the directive's own edition
  (`CobolWordsMap.DirectiveEdition`, 2023): reserved via `ReservedWords.Find(w).R2023` (the generated §8.9 table's 2023
  flag — a word §8.9 dropped before 2023, AUTHOR or MEMORY, is not in it), **context via `ContextSensitiveWords.Contains`
  (the generated §8.10 table)**, intrinsic via `IntrinsicCatalog.TryGet` inside its window (`IsDefinedAt`). None ⇒
  COBOLNET1623 (SR3). ⛔ The lexer vocabulary (`CobolKeywordTokens.TryTokenType`) is NOT a population of the standard and no
  longer answers either rule (kb/Work PB1373: SR3 accepted `UNDEFINE "AUTHOR"` because the lexer tokenizes it, and SR4
  refused `RESERVE "AUTHOR"` for the same reason).
  ⛔ This used to read "context via `DefaultVocabulary` (a keyword token exists)", which answers a DIFFERENT
  question — the vocabulary knows only the context words this compiler happens to tokenize, so SR3 rejected
  every legal directive naming one of the 31 that it does not (HEX, CURRENT, LC_ALL, ANUM, BYTE, ACTIVATING,
  STACK, TOP-LEVEL, UCS-4, UTF-8, UTF-16, …) and no directive could name them at all (kb/Work PB250). §8.10's
  own NOTE points the other way: "Words can be added or deleted from this list for a specific compilation
  group by use of the COBOL-WORDS directive." The §8.10 table is generated from the spec section by
  `scripts/gen-reserved-words.ps1` and drift-tested against it; it needs no per-edition flags because the
  directive is a COBOL-2023 introduction, so 2023 is the only edition at which SR3/SR4 are ever asked.
- **SR4 (lit2/5/6 NOT reserved/context/intrinsic; a valid user-defined word §8.3.2.2)** — Compiler: reject if
  the word is in the same 2023 population (`IsReservedContextOrIntrinsic`); the §8.3.2.1 / §8.3.2.2 well-formedness is a
  Frontend check at parse time (`CobolWordsDirectiveProcessor.UserWord`): the word shape (word characters — letters,
  digits, hyphen, underscore, the extended letters — neither hyphen nor underscore first or last), at least one letter, and
  `CobolWordRule.DirectiveWordViolations` at the directive's edition (the 63-character ceiling and Annex B's placement of
  each extended character) — asked here because a fresh word no statement uses never reaches the tree funnel.

**Omitted conditional-compilation branches (§7.2.1, kb/Work PB2003).** The directive in the false path of a `>>IF` is part
of the text that shall be "syntactically correct", but the conditional-compilation driver consumes that line and the stage
that parses the operand never sees it. Each stage that owns an operand therefore has a **check-only entry**
(`CobolWordsDirectiveProcessor.CheckOperand`, the same for `TurnDirectiveProcessor` and `FlagDirectiveProcessor`): the same
parse, the same diagnostic, nothing applied and no timeline read (a compilation-variable-name in a literal slot is accepted
by its shape; SR3/SR4 categories, SR5 uniqueness and the DEFINE lookup belong to the directive that is compiled).
`StageOperandChecks` registers one entry per owner stage and the driver asks it in an omitted branch;
`CompilerDirectiveCatalogDriftTests.EveryStageOwner_HasACheckOnlyEntryOrIsTheDriver` fails for a Stage row whose owner has
none.

## §6 Hazards & resolutions

The lexer freezes three decisions at lex time; because the retype is applied by the lexer itself (§2), each reads
the EFFECTIVE word:

1. **SUBSCRIPT-mode entry** (`_dataNameTokens` at `(`). The previous token's type is its EFFECTIVE type: a
   de-reserved keyword (UNDEFINE lit3 / SUBSTITUTE lit4) is an `IDENTIFIER` by the time `(` is lexed, so a later
   `lit3(sub)` enters SUBSCRIPT; a synonym is the keyword's type, so `lit2 (` follows the keyword's own rule. (This
   used to be a separate set of de-reserved types the lexer consulted; the retype made it redundant and it is gone.)
2. **PIC / FUNCTION** (`PicMode` push, `PreviousIsFunctionName`). A synonym of PIC/PICTURE enters PICMODE and a
   synonym of FUNCTION opens the argument region, because the token IS the keyword when the lexer acts on it; a
   de-reserved PICTURE/PIC/FUNCTION is a data-name and does neither (kb/Work PB1372).
3. **Token boundaries / maximal munch.** SR2 forbids spaces in a literal, so lit2/lit5 lex as a single
   `IDENTIFIER`; no munch issue.

## §7 Diagnostics

- **COBOLNET0900** — introduction gate (below 2023), via `ConstructRegistry.Check(Constructs.CobolWordsDirective2023)`.
- **COBOLNET1623** `cobol-words-directive-invalid` (Error) — a malformed directive (unknown option word,
  missing `WITH`/`BY`, missing/badly-formed or unterminated literal) OR a syntax-rule violation (SR2 literal
  form, SR3 lit1/3/4 category, SR4 lit2/5/6 category / user-word form, SR5 duplicate); the message names the SR. SR1's
  placement is COBOLNET2652 (`DirectivePlacementViolation`).
- **COBOLNET0901** — RESERVE's use-site rejection (existing edition-reserved-word funnel), one per distinct word.

## §8 Threading

`Frontend.CobolWordsMap` (built in `Preprocess`, applied in `LexAndParse`) → `CompilerDriver` →
`BinderDriver.Bind(..., cobolWordsMap)` → `VersionConformancePass.Run(ctx, edition, sink, cobolWordsMap)` (the
composed set + SR3/SR4 validation) and the bind session (for `IntrinsicBinder`). Empty map ⇒ every consumer is
a no-op and output is byte-identical (the zero-overhead invariant).

## §9 Increment plan

- **Incr A — recognition + parse + gate (no behavior).** `CobolWordsMap` + `CobolWordsDirectiveProcessor`
  (parse 4 options, SR1/SR2/SR5, `>>COBOL-WORDS` edition gate 0900), pipeline wiring + `leaveCobolWordsDirectives`,
  `constructs.json` row + regen, COBOLNET1623 descriptor. Unit tests (parser) + below-2023 negative golden.
- **Incr B — RESERVE + UNDEFINE (reserved-word semantics).** `ReservedWordSet` overlay + thread into
  `VersionConformancePass`; SR3/SR4 semantic validation. RESERVE (0901) + UNDEFINE goldens.
- **Incr C — token retype (EQUATE / UNDEFINE / SUBSTITUTE).** `CobolWordsRewriter.Plan` applied by the lexer
  data-name set. EQUATE + SUBSTITUTE goldens (observable stdout).
- **Incr D — intrinsic-function-name synonyms.** `IntrinsicBinder` map consultation. Intrinsic EQUATE/SUBSTITUTE
  goldens.

Each increment: fresh build → characterization + `CobolWordsDirectiveTests` filter + a CLI probe (wave-local
gate) → commit + push + a DEVLOG entry.

## §10 Tests

- **Unit** `tests/Cobol.Net.Tests.Unit/CobolWordsDirectiveTests.cs` — the directive parser (each option,
  malformed, SR1/2/5), the `CobolWordsMap` composition, `ReservedWordSet` overlay, and end-to-end via
  `CompilerDriver` (RESERVE→0901, EQUATE→resolves).
- **Conformance positive** `tests/conformance/2023/cobol_words_*.cob` (+ `.out`) — EQUATE (SHOW→DISPLAY prints
  `HI`), SUBSTITUTE, UNDEFINE, intrinsic-synonym — listed in `2023/manifest.json`.
- **Conformance negative** `tests/conformance/negative/cobol_words_*.cob` (+ `.err`) — below-2023 (0900),
  RESERVE-used-as-user-word (0901), SR violations (1623) — listed in `negative/manifest.json`.
- **Version matrix** — the `cobol-words-directive-2023` row drives `VersionMatrixTests` + `ConstructRegistryDriftTests`.
- **Reach drift** `tests/Cobol.Net.Tests.Unit/CobolWordsReachDriftTests.cs` — the §2.1 invariant, measured: every
  lexed §8.9/§8.10 word resolves to a token type; the lex probe agrees with the vocabulary wherever both answer;
  the multi-spelling keywords are reached ONLY by the probe (so removing it fails here, not silently in a user's
  program); the population really is partitioned between the two mechanisms; `Resolve` implements GR2–GR5; and
  `TokenIs` does not re-resolve a word the retype already resolved.
- **§8.10 table drift** `tests/Cobol.Net.Tests.Unit/ContextSensitiveWordsDriftTests.cs` — the generated table
  equals its JSON AND the spec section, and no word is in both §8.9-at-2023 and §8.10.
- **Conformance, the name-level half** `tests/conformance/2023/pb250_cobol_words_phrase_word_{equate,undefine}.cob`
  (a §15 phrase word the lexer does not tokenize, both directions),
  `tests/conformance/2023/pb250_cobol_words_multi_spelling.cob` (UNDEFINE "ZERO" leaves ZEROS/ZEROES reserved),
  and `tests/conformance/negative/pb250-cobol-words-undefined-keyword-syntax-withdrawn.cob` (GR3: the withdrawn
  syntax is not available).

> **Not conflated:** `tests/version-matrix/cobol-words.json` + `scripts/gen-cobol-words.ps1` are the STATIC
> context-sensitive word-set single-source (the `cobolWord` rule + `_dataNameTokens`); they are unrelated to
> this §7.3.10 SOURCE directive. Do not edit them for this feature.
