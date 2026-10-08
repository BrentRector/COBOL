# DESIGN — Target Frontend & Grammar Architecture

Status: DESIGN — substantially implemented. The dead-grammar/JSON-XML deletions, the single-sourced
context-sensitive word set, the shared-lowest `Cobol.Net.Editions` registry, and the one `VersionConformancePass`
edition gate are all in the tree; the §8 questions are resolved as decided. Remaining frontend work: completing the
`Cst/` typed-façade migration (§3.7, paced by the binder track), the D10 SUBSCRIPT-mode removal (§9, scheduled for
PHASE 15), and the residual preprocessor namespace / `using Core =` alias cleanup.
Scope: `src/Cobol.Net.Frontend/**` — the ANTLR lexer/parser grammars (`Grammar/**/*.g4`), the hand-written
preprocessor (`Preprocessor/`), the parse-stage machinery (`Parsing/`), the diagnostics model
(`Diagnostics/`), the generated-parser build (`GenerateIfNewer.ps1` + `Invoke-Antlr4CSharp.ps1` + csproj
targets), and the frontend's contract to its ONE consumer, the `Cobol.Net.Compiler` binder.

This document is the decision-complete target for the frontend. It is a companion to
`docs/rearchitecture/DESIGN-edition-framework.md` (the edition/version framework — this doc owns the
parse-time construct-id ANNOTATION and defers the registry to that doc; edition GATING itself lives in the
`VersionConformancePass`, `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`) and to the binder
rearchitecture docs (which own the parse-tree *consumption* side; this doc specifies the façade the binder
consumes).

> **This is a SUPERSET grammar.** It recognises the union of all editions — there are no edition `{isXXXX()}?` gates;
> every construct parses at every `--std`. Each version-gated rule carries a committed-match construct-id ANNOTATION (a
> parse *action*, not a speculative gating predicate — which would be evaluated speculatively during a failing parse),
> while version NUMBERS stay single-sourced in `constructs.json`. Edition gating lives entirely in ONE
> `VersionConformancePass` over the bound tree (parse → edition-agnostic bind → VersionConformancePass → emit-if-clean →
> backend); there is no `ReservedWordEditionHints`. Full design:
> `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`.

---

## 0. Hard invariants this design upholds

1. **Typed-native only** — the frontend produces a *syntactic* parse tree; it never touches storage form. No
   change here can regress the byte invariant. (Unaffected but stated for completeness.)
2. **Spec-first** — every gate, token, and severity cites ISO/IEC 1989:2023 (`specs/ISO_COBOL.md`). The
   canonical construct/edition metadata is `tests/version-matrix/constructs.json` +
   `tests/version-matrix/reserved-words.json`; the frontend must consume that single source, never a copy.
3. **Battery green throughout** — the FULL battery (greenfield conformance + unit + characterization + the
   NIST legacy guard; current counts live in the plan's STATUS banner) stays
   green at *every* step. The migration in §5 is a sequence of individually-green commits; no big-bang except
   the sanctioned G8 namespace rename, which this design brings forward where it is free.
4. **Singular pattern** — ONE source of truth for the context-sensitive word set (today triplicated), ONE
   edition-gating mechanism (the `VersionConformancePass` over the bound tree), ONE diagnostic type end-to-end.
5. **Four editions in one** — ONE superset grammar (no edition predicates; each version-gated rule carries a
   committed-match construct-id annotation), validated by the VERSION TEST MATRIX. Edition gating is the single
   `VersionConformancePass` over the bound tree, not parse-time predicates — the one-grammar approach kept (correct and
   cheap), with both the duplication and the scattered gating removed. *(`DESIGN-version-conformance-pipeline.md`.)*
6. **JSON/XML are non-ISO** — 0 spec occurrences; they are deleted from the live grammar (§4).
7. **⛔ THE REPETITION ARITY IS THE GENERAL FORMAT'S, NEVER A CONVENIENCE.** A rule may parse a SUPERSET of the
   operand *shapes* the standard admits (the parse-wide/bind-narrow doctrine) — it may **not** invent a
   REPETITION the general format does not print. A loop the standard never licensed cannot accept new legal
   source; all it can do is give the parser a second, wrong way to read source that is already legal.
   **Worked example (PB45).** `evaluateWhenGroup : NOT? evaluateWhenItem+` against §14.9.13.2's
   `{ { WHEN selection-object [ ALSO selection-object ] … } … }`, where objects repeat ONLY through ALSO. The
   spare `+` let the parser PEEL a function's argument list off it — `WHEN FUNCTION SQRT(X) > 1` parsed as the
   two objects `FUNCTION SQRT` and `(X) > 1`, because `functionCall`'s argument list is optional and
   `primaryExpression` has a `LPAREN arithmeticExpression RPAREN` alternative. It compiled clean and threw at run
   time. ⚠ **The hazard is ANTLR's first-*viable*-alternative rule, not its first-alternative rule**: where both
   readings are viable the greedy one wins (measured: `DISPLAY`, `STRING`, `INSPECT`, nested function arguments
   and multi-operand `ADD` never peeled), so the peel appears only where the correct reading is NOT viable — which
   is precisely where it is hardest to notice. ⚠ And the binder had invented an uncited semantics ("additional
   items AND in") to give the spare loop a meaning, so the misparse had somewhere to land.
   **The check, when writing or reviewing any `+`/`*` in a statement rule: point at the `…` in the printed general
   format that licenses it, and at the separator it repeats over.** If there is no `…`, there is no loop. A
   PHASE-13 research artifact had already flagged this exact rule as a first-alternative hazard
   (`evidence/PHASE-13-grammar-batch-research.json`) and nothing acted on it — findings live in `kb/Work/`, not in
   evidence files.
8. **⛔ THE OPTIONALITY IS THE GENERAL FORMAT'S TOO — AND WHERE THE GRAMMAR STAYS PERMISSIVE, THE BINDER OWES A
   NAMED SCREEN.** Invariant 7's mirror. ISO §5.2.6.2 makes BRACKETS the only convention that lets a portion of
   a general format be omitted, and §5.2.2 makes an underlined keyword required subject to those conventions —
   so a phrase printed on its own line with no brackets is MANDATORY, and writing it `phrase?` in the grammar
   under-rejects. That is the **falsely-permissive twin of the OCR's falsely-restrictive bias**: the
   transcription's diagrams were lossy toward *rejecting* legal source (CLAUDE.md rule 1), and a grammar written
   from a lossy diagram fails the other way just as silently.
   **The decision (kb/Work PB350, 2026-09-05): such a phrase is enforced at BIND time, not by requiring it in
   the grammar.** `StatementValidation.ScreenOmittedRequiredPhrase` → `DiagnosticCatalog.FormatRequiredPhraseOmitted`
   (COBOLNET1850), the mirror of the forbidden-phrase screen `ScreenForbiddenPhrase`/COBOLNET1720 — one
   descriptor carrying the SHAPE, each call site quoting its own §. Two reasons, and they generalize:
   a bind-time diagnostic NAMES the omitted phrase, the statement and the clause where an ANTLR parse error can
   only report an unexpected token; and the screen can be placed AFTER the binder has normalized the parse
   tree's shape away, so ONE test covers every grammar arm.
   **Worked example (PB350).** §14.9.34.2 prints RETURN's `AT END imperative-statement-1` unbracketed between a
   bracketed `[ NOT AT END … ]` and `[ END-RETURN ]` (rendered from the printed page, folio 708). The grammar
   wrote `returnAtEndPhrase?` AND `returnAtEndPhrase`'s reversed alternative made the AT END half optional too,
   so a RETURN with no AT END compiled at all four editions and, at end of data, control fell THROUGH the
   statement onto a record area §14.9.34.4 GR3 leaves undefined — a loop written on RETURN could never terminate
   from the statement. The screen tests `atEnd is null` after `PhraseBlocks.Split`, which covers BOTH arms;
   testing the phrase NODE would have left the reversed NOT-only arm compiling (the two-arm dispatch with one
   arm fixed). ⚠ `readAtEnd` is NOT the same case: §14.9.30.2 prints READ's AT END / NOT AT END pair inside
   brackets WITH choice indicators, which §5.2.6.4 reads as "zero or more", so there the phrase really is
   optional. The asymmetry is why the two pages are measured separately.
   **The check, when writing or reviewing any `?` on a statement PHRASE rule: point at the brackets in the
   printed general format that license it.** If the line is unbracketed, the phrase is required and owes a
   screen. `RequiredFormatPhraseDriftTests` re-derives the whole set from the transcription — today RETURN's AT
   END (screened) and SEARCH's WHEN (already `+` in the grammar) — so a transcription repair or a new format
   fails until its phrase is screened or adjudicated.

---

## 1. Current problems (grounded in the survey + code)

Evidence is cited to file:line as verified in the tree.

### 1.1 Dead and mislabeled grammar files
Five top-level `.g4` files are **neither generated nor referenced** by any C#, csproj, or build script
(`grep` over `src/**` for their names hits only the files themselves and the `obj/antlr-lib` staging copy):
`Grammar/CobolDialect.g4` (105 loc), `Grammar/CobolParserGenerics.g4` (101), `Grammar/CobolParserJsonXml.g4`
(118), `Grammar/CobolParserOO.g4` (160), `Grammar/CobolPreprocessor.g4` (98). `CobolParserOO.g4` /
`CobolPreprocessor.g4` are stale duplicates of live code (`Core/CobolOO.g4`, the hand-written
`Preprocessor/`). They mislead every reader about what actually compiles and inflate the grammar surface a
maintainer must reason about.

### 1.2 Non-ISO JSON/XML inside a LIVE imported fragment
`Core/CobolExtensionsJsonXml.g4` is imported by `CobolParserCore.g4` (line 17) and its `jsonStatement` /
`xmlStatement` are wired into the `statement` rule under `{is2014()}?` gates
(`CobolParserCore.g4:716-717`). JSON/XML have **0 occurrences in the ISO spec** — a hard-invariant violation
that they sit in a live fragment and a live dispatch arm. The same fragment ALSO holds
`inlineMethodInvocationStatement` (at the time believed to be a real 2023 OO construct) — so the fragment
was mis-named: 90% dead-non-ISO and 10% live-OO. That surviving rule turned out to be a THIRD kind of
wrong: it matched `dataReference ( argumentList )` as a STATEMENT, a shape ISO defines nowhere, while the
real §8.4.3.4 construct — the §8.4.3.1.2 Format 4 IDENTIFIER, led by the §8.7.4 `::` invocation operator
— had no surface at all. It is now `inlineMethodInvocation` in `Core/CobolOO.g4`, an OPERAND alternative
wherever `functionCall` is one (kb/Work PB428). Its parenthesis pair, when written, holds at least one argument
(the §8.4.3.4.2 brace repeats inside the one pair, unlike §8.4.3.2.2's bracketed function arguments), so
`O :: "M" ( )` is refused by the grammar and named COBOLNET2994 by `CobolErrorStrategy` (kb/Work PB1430).

### 1.3 The context-sensitive word set is triplicated and hand-synced
The set of tokens that are keywords in context but legal user-defined words elsewhere is maintained in
**three** physically separate places that must agree by hand:
- lexer `_dataNameTokens` HashSet — `Core/CobolLexer.g4:30-72` (drives SUBSCRIPT-mode entry);
- parser `cobolWord` rule — `CobolParserCore.g4:26-73` (admits the word in a name slot);
- compiler `ReservedWords` table — `src/Cobol.Net.Compiler/Validation/ReservedWords.Table.cs` (the §8.9
  reserved-word funnel).
The comment at `CobolLexer.g4:20-21` literally instructs the maintainer to hand-mirror the two grammar
copies ("It MUST mirror the parser's cobolWord rule … When adding a token to cobolWord, add it here"). A
silent desync mis-triggers or fails to trigger SUBSCRIPT mode → a wrong-or-missing parse error with no
diagnostic pointing at the cause. **Note the asymmetry:** `ReservedWords.Table.cs` is ALREADY generated from
`tests/version-matrix/reserved-words.json` by `scripts/gen-reserved-words.ps1` (header at
`ReservedWords.Table.cs:1-6`), and a drift test binds them — so the codegen discipline the other two copies
lack already exists and is proven. The fix is to extend that generator, not invent one.

### 1.4 EditionGateHints re-derives what the `{isXXXX()}?` gate already knew
When an introduction predicate rejects a too-new construct, ANTLR reports a generic `NoViableAlternative`.
`Parsing/EditionGateHints.cs` (207 loc, `Recognize`) then **reverse-engineers** which gated construct was
rejected, via a 29-entry table of `(offending-token, rule-stack, lookahead-window)` signatures
(`EditionGateHints.cs:35-169`). The signatures were "derived empirically" (its own remark) and
are inherently brittle: several arms carry dual-path token-adjacency fallbacks because the rule can pop off
the stack before the error is reported (`:85-88`, `:100-131`). This is a whole subsystem whose only job is
to recover an identity the gate *had at reject time and threw away*. The dossier reproduced a duplicate
diagnostic for an edition-gated construct as a direct consequence.

### 1.5 DialectLevel is double-sourced; edition metadata straddles the assembly seam
`DialectLevel` is stored independently on `CobolParserCoreBase` (`:17`), on `Frontend`
(`Frontend.cs:45`), and on the compiler's `EditionContext`. The canonical `ConstructRegistry` lives in
`Cobol.Net.Compiler`, which the frontend cannot see (Compiler references Frontend, not vice-versa) — so the
frontend `EditionGateHints` + the preprocessor's `ReferenceFormatProcessor.EditionGates` /
`CopyProcessor` each carry their **own** copy of the edition metadata and the strict/permissive severity
policy. Three independent renders of "removed = error strict / warning permissive."

### 1.6 The binder consumes the tree by string interpretation, not the generated visitor
The `Cobol.Net.Compiler` binder hand-walks the raw ANTLR context types with **~336 `GetText()` calls** and
string comparisons (dossier; e.g. `DataBinder.cs:155`), rather than the generated typed visitor. ANTLR's
`-visitor` output is generated (`Invoke-Antlr4CSharp.ps1:71`) but only `EditionValidator` uses it. The raw
parse-tree shape is thus a wide, un-narrowed, stringly-typed cross-assembly contract with no façade: any
grammar rule rename ripples into dozens of `GetText()` sites invisibly.

### 1.7 Namespace/assembly coherence (resolved)
Generated code emits into namespace `CobolNet.Frontend.Generated`, the same root as the assembly `Cobol.Net.Frontend`
(RootNamespace `CobolNet.Frontend`); the package name is the csproj's `AntlrNamespace` property. The preprocessor, parsing
and diagnostics files declare `CobolNet.Frontend.*` (PHASE 01 renamed the stale legacy-assembly namespaces), and `Frontend.cs`
is self-contained: the preprocessor and parse machinery, `DiagnosticBag` and `TurnEvent` all live in this assembly.

### 1.8 Committed build-output caches and brittle two-stage error handling
`Grammar/.antlr/` and `Grammar/Core/.antlr/` hold ANTLR java-target IDE caches checked into the tree
(`CobolLexer.java`, `CobolPreprocessor.java`, `.interp`, `.tokens`) — build output, and one of them
(`CobolPreprocessor.*`) is a cache of a DEAD grammar. `Frontend.cs:135` catches `catch (Exception)` on the
SLL bail — this masks predicate/lexer-action bugs (a `NullReferenceException` in a semantic predicate) as a
"retry with LL," hiding real defects behind a silent second parse.

### 1.9 Parallel SUBSCRIPT lexer vocabulary — RESOLVED (kb/Work PB2113, §9)
SUBSCRIPT mode re-declared its own `SUB_*` literal/operator/identifier tokens paralleling the DEFAULT-mode tokens,
because the disambiguation of `x(i)`-as-subscript vs `(a+b)`-as-grouping was punted from the grammar into a lexer
mode driven by `PreviousTokenCouldBeDataName()`, and the binder re-parsed the captured token run by hand
(`ReferenceResolver.SplitSubscriptTokens`). §9 removed both: the '(' after a data-name-capable word is now the
virtual token `REF_LPAREN` (its ')' `REF_RPAREN`), the tokens between them are the DEFAULT vocabulary, and the
grammar parses the list (`subscriptPart : REF_LPAREN subscriptList? REF_RPAREN`). One vocabulary, no mini-parser.

The trigger has ONE region exception, and it is a spec rule, not a heuristic (kb/Work PB924): ISO §13.18.54.3
SR9 makes a SUM not preceded by FUNCTION inside a report description entry the report-writer SUM CLAUSE, whose
addend may open with '(' — so the lexer tracks the REPORT SECTION (`TrackReportSection`: opened by its header,
closed by the next section or division header) and a SUM there opens no reference paren
(`IsReportSumClauseKeyword`). SUM is the only §8.9 ∩ §8.11 word that begins a report or data description clause
whose operand can open with '('; `CobolLexerModeDriftTests` pins both regions for all four words.

---

## 2. Target architecture — overview

The frontend becomes a **three-layer, single-source-of-truth** stack with a **narrow typed façade** to the
binder:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Cobol.Net.Editions            (NEW lowest assembly — see DESIGN-edition-framework.md)  │
│    ConstructRegistry · ReservedWords · EditionSeverityPolicy · EditionInfo    │
│    (generated from constructs.json + reserved-words.json + cobol-words.json)   │
└───────────────▲───────────────────────────────────────────────▲──────────────┘
                │ referenced by                                  │ referenced by
┌───────────────┴───────────────────────────┐   ┌───────────────┴──────────────┐
│  Cobol.Net.Frontend                        │   │  Cobol.Net.Compiler          │
│   Preprocessor/   (ns → CobolNet.Frontend) │   │   Binding/ (via façade only) │
│   Grammar/        (2 generated units)      │   │ Validation/VersionConformance│
│   Generated/      (ns → CobolNet.Frontend. │──▶│     Pass (sole edition gate) │
│                    Generated)              │   │   … consumes Cst façade,     │
│                                            │   │      not raw contexts        │
│   Parsing/        (ParseSession, recovery) │   └──────────────────────────────┘
│   Cst/            (NEW typed façade layer) │
│   Diagnostics/    (one Diagnostic model)   │
└────────────────────────────────────────────┘
```

Key decisions, each expanded below:

- **D1. ONE superset grammar — no per-edition forks, no edition predicates.** The single grammar recognises
  the union of all editions; every construct parses at every `--std`. Being "four compilers in one" is the
  job of the ONE `VersionConformancePass` over the bound tree, not of the parser — the only surviving
  semantic predicates are the two load-bearing forward-detects (D3). We remove the *duplication and
  reverse-engineering* around edition identity (D3, D4).
- **D2. Generate the context-sensitive word set** (lexer HashSet + parser `cobolWord` rule + the
  `ReservedWords`/gate metadata) from ONE declarative source, extending the existing
  `gen-reserved-words.ps1` codegen. Kill the triple hand-sync.
- **D3. Committed-match construct-id ANNOTATION; edition diagnosis in the `VersionConformancePass`.** Each
  version-gated rule carries a construct-id annotation — a parse *action* plus side-table storage keyed by
  parse context, never a predicate (ANTLR evaluates hoisted predicates speculatively during a failing parse,
  so a gating/stamping predicate mis-attributes constructs). The pass reads the annotation (or a
  self-identifying bound node) and gates via `ConstructRegistry.Check`. No signature table, no
  reverse-signature recogniser — `ReservedWordEditionHints` does not exist in the end state. The two
  load-bearing forward-detects survive: the `openClause` `{is2002() || retryPhraseAhead()}?` and the
  `boolExprAhead()`-based boolean-condition ENTRY (§3.2).
- **D4. Move the canonical edition registry to a shared lowest assembly** (`Cobol.Net.Editions`) both the
  frontend and compiler reference, deleting the frontend's + preprocessor's metadata/severity copies. (Owned
  by DESIGN-edition-framework.md; this doc specifies the frontend's *consumption*.)
- **D5. Delete the 5 dead grammars; split the JSON/XML fragment** — move the live
  `inlineMethodInvocationStatement` into `Core/CobolOO.g4`, delete the non-ISO JSON/XML rules and their
  dispatch arms, delete the now-empty fragment.
- **D6. Introduce a `Cst/` typed façade** (thin wrappers over the generated contexts) as the ONLY surface the
  binder consumes — retire the ~336 `GetText()` hand-walks incrementally behind it.
- **D7. One `Diagnostic` type end-to-end**, one `ParseSession` orchestrator, narrowed exception handling,
  fail-hard portable regen unchanged in behavior but with the package name in an MSBuild property.
- **D8. Bring the generated-namespace rename forward** to `CobolNet.Frontend.Generated` (it is free and
  removes the `using Core =` alias everywhere), and fix the stale `Frontend.cs` banner + preprocessor
  namespaces in the same wave.

---

## 3. Target design — detail

### 3.1 Grammar fragment factoring (unchanged shape, corrected membership)

The two generated units stay: `Grammar/Core/CobolLexer.g4` (lexer) and `Grammar/CobolParserCore.g4`
(parser). The parser keeps importing Core fragments, but the import list is corrected:

```
import CobolExpressions, CobolData, CobolSpecialNames, CobolReportWriter,
       CobolIO, CobolControlFlow, CobolOO, CobolScreen;
```

`CobolExtensionsJsonXml` is **removed from the import list** (D5). Fragment responsibilities are re-stated as
a one-line banner at the top of each fragment (there is currently no per-fragment ownership note):

| Fragment | Owns | Version-gated content (construct-id annotated; gated by the pass) |
|---|---|---|
| `CobolExpressions.g4` | arithmetic/condition/boolean tiers, FUNCTION calls, literals | boolean operator tiers (2002), XOR (2023) |
| `CobolData.g4` | level entries, PIC, USAGE (incl. the shared `encodingPhrase`/`endiannessPhrase` the OPTIONS clauses also cite), OCCURS(+DYNAMIC), REDEFINES, VALUE, TYPEDEF/TYPE | OCCURS DYNAMIC (2014), TYPEDEF/TYPE/BASED/PROPERTY (2002), the USAGE float format phrases (2014) |
| `CobolSpecialNames.g4` | SPECIAL-NAMES, ALPHABET/CLASS/CURRENCY | FOR NATIONAL (2002) |
| `CobolIO.g4` | FILE-CONTROL/SELECT, OPEN/READ/…, SORT/MERGE, sharing/lock | SHARING/LOCK/RETRY/UNLOCK (2002) |
| `CobolControlFlow.g4` | IF/EVALUATE/PERFORM/GO/CALL/RAISE/RESUME | RAISE/RESUME/RAISING (2002) |
| `CobolOO.g4` | CLASS/INTERFACE/METHOD, INVOKE, **inline method invocation** (`::`, §8.4.3.4) | all (2002) |
| `CobolReportWriter.g4` | RD, report groups | — |
| `CobolScreen.g4` | SCREEN SECTION (parse-only today) | — |

**Rationale for keeping the fragment split:** ANTLR import fragments give physical cohesion per subsystem
with zero runtime cost. Version-gated rules live *inside* their subsystem fragment, each carrying its
committed-match construct-id annotation (introduction editions single-sourced in `constructs.json`, decided
by the `VersionConformancePass`); fragment-at-a-time landing keeps the incremental discipline of
`feedback_grammar_version_factoring`. Forking grammars per edition would 4× the maintenance for a model the
superset grammar + one pass already express correctly.

### 3.2 Edition-gating strategy — superset parse + committed-match construct-id annotation

**Principle:** the grammar recognises the union of all editions and never decides edition legality. A
parse-time reject would force the parser to re-derive the construct's identity after the fact (the
`EditionGateHints`/`ReservedWordEditionHints` reverse-signature trap, §1.4) — and a *predicate* cannot carry
the identity either, because ANTLR evaluates hoisted predicates speculatively at the stuck token during
failing/exploratory parses, so a gating or stamping predicate fires on paths that never match and
mis-attributes the construct. The design therefore uses a **committed-match construct-id ANNOTATION**: a
parse *action* that runs only when its alternative has actually matched, storing the `ConstructId` in a
side table keyed by the parse context:

```csharp
// CobolParserCoreBase (target)
private readonly Dictionary<ParserRuleContext, ConstructId> _constructIds = new();
/// Committed-match annotation: invoked from a rule ACTION after the alternative matched.
protected void Construct(ConstructId c) => _constructIds[Context] = c;
public IReadOnlyDictionary<ParserRuleContext, ConstructId> ConstructIds => _constructIds;
```

Grammar sites carry the annotation, not a gate — every alternative parses at every `--std`:

```
    | occursDynamicClause {Construct(ConstructId.OccursDynamic);}   // parses at EVERY --std
    | allocateStatement   {Construct(ConstructId.Allocate);}
```

Edition legality is decided in exactly ONE place: the `VersionConformancePass` over the bound tree
(`docs/rearchitecture/DESIGN-version-conformance-pipeline.md`). Bound nodes carry a `.Syntax`
back-reference; the pass reads the construct id (from the side table, or from a self-identifying bound
node), asks `ConstructRegistry.Check` (single-sourced from `constructs.json`), and emits the
COBOLNET0900-band diagnostic through `EditionSeverityPolicy`. Because the identity is never thrown away,
there is no reverse-signature recogniser: `ReservedWordEditionHints` does not exist in the end state, and a
new gated construct is self-registering (add the `constructs.json` row + the annotation; no empirical
signature step).

**The two load-bearing forward-detects.** Exactly two semantic predicates survive in the grammar, and both
are *disambiguation* devices (choosing between two parses that are each legal somewhere), not edition gates:

1. **`openClause` — `{is2002() || retryPhraseAhead()}?`.** `retryPhraseAhead()` is true iff, with `RETRY` at
   the lookahead in the OPEN-clause position, the following tokens form a complete retry tail
   (arithmetic-expression `TIMES` | `FOR?` arithmetic-expression `SECONDS` | `FOREVER`) AND at least one
   further candidate file-name token remains before the sentence terminator (`openFileSpec+` must stay
   satisfiable). Consequences: `OPEN INPUT RETRY FOREVER.` (no trailing name) stays a two-file-name list at
   85; `OPEN INPUT RETRY 5 TIMES F.` forward-detects (an integer can never be a file name); the genuinely
   ambiguous `OPEN INPUT RETRY FOREVER F.` resolves to file names below 2002 (`RETRY` is a legal §8.9 user
   word there) and to the phrase at ≥2002 (`is2002()` is true; the §8.9 funnel reserves RETRY). Fail-safe: a
   missed real gate degrades to a neutral parse error, never a wrong edition claim.
2. **The boolean-condition ENTRY — `boolExprAhead()`** (`CobolParserCoreBase.cs:69`), generalized to fire at
   all editions using operand-adjacency (the highest-scrutiny site: it shares the comparison DFA).
   Legality of the boolean operators themselves is decided by the pass
   (`Check(BooleanOperators2002)`).

No other `is85()/is2002()/is2014()/is2023()` predicate survives in the grammar.

### 3.3 Lexer & token organization — one word source, shared literal fragments

**3.3a Single source for context-sensitive words (D2).** A new declarative source
`tests/version-matrix/cobol-words.json` (sibling of the existing `reserved-words.json`) lists every
context-sensitive word with the facts all three consumers need:

```json
{ "word": "LENGTH", "token": "LENGTH", "userWordAt": [85,2002,2014,2023],
  "reservedFunnelAt": [], "note": "FUNCTION LENGTH / START WITH LENGTH" }
```

`scripts/gen-cobol-words.ps1` (extends the proven `gen-reserved-words.ps1` pattern) generates THREE outputs
from it:
1. `Parsing/CobolLexerWordSet.g.cs` — a committed `partial class CobolLexer` (NOT under `Generated/`, so
   `dotnet clean` keeps it) carrying the subscript-trigger set; a partial `.cs` is the minimal buildable form
   because ANTLR has no portable `@members` text-include and the set references token-type `int` constants on
   the generated lexer.
2. `Grammar/Core/CobolWords.g4` — a generated parser fragment holding TWO rules, `cobolWord` and
   `reservedGatedWord`, imported by `CobolParserCore.g4`.
3. It cross-checks against `reserved-words.json` (RW-1: a subscript-trigger-only word must be 2023-reserved,
   with exact reconciliation pins on both asymmetry sides) so the two sources cannot silently disagree; a
   violation fails generation (fail-hard, like the existing drift test).

**The reservation gate is TOKEN-LEVEL, and the gate set itself is DERIVED — there is no flag (kb/Work PB693,
PB655).** ISO §8.3.2.1 rule 1 is the whole rule: *"Reserved words shall not be used as user-defined words or
system-names."* A lexer token that §8.9 reserves at SOME editions is a keyword there and an ordinary user-defined
word at the others (GOBACK and NULL at COBOL-85, ALTER and AUTHOR from 2002, CONSTANT at 85, …). Step 4b of
`gen-cobol-words.ps1` computes that set from `reserved-words.json`: a `nameSlot` row whose word is reserved at
>=1 edition is gated, full stop. A gated word is **never a `cobolWord` alternative**; its only grammar home is
the generated `reservedGatedWord` rule — an alternative of every DEFINITION slot (`dataName`, `programName`,
`sectionName`, `paragraphName`, the SELECT file-name) with no predicate and one action, `gatedDeclaration`.

The user-word reading is decided on the TOKEN, before the parse that counts (`Parsing/ReservationGateRewriter`;
its `ParseToFixpoint` is THE gate loop, and EVERY whole-group parse calls it — the greenfield
`Frontend.LexAndParse` and the legacy differential oracle's `Compilation.LexAndParse` alike, because both parse
this grammar and a front end that skips the loop can never read a gated word as a name; train 49 dropped PB655
over exactly that third arm, and `ReservationGateParseEntryDriftTests` now fails on any source file that calls
`compilationUnit()` without the loop). A parse pass that meets a gated word the compile edition leaves free
(`userWordHere`) either in a `reservedGatedWord` definition slot, or as the offending token of a syntax error
(the keyword reading failed, and the only other reading §8.9 leaves is a user-defined word — this is how a name
introduced in a slot that does not offer `reservedGatedWord`, an index-name or a SPECIAL-NAMES class-name,
is reached), has found a user-defined word: every occurrence of that word is retyped to `IDENTIFIER` and the
source is parsed again. Each round frees at least one more word and a retyped token can never be declared again,
so the loop ends; a program that declares no such word parses exactly once, and only the last pass's diagnostics
are reported. Where §8.9 RESERVES the word nothing is retyped: a declaration matches `reservedGatedWord` and
`VersionConformancePass`'s funnel answers with the targeted COBOLNET0901 (`VisitReservedGatedWord`, which hangs on
the RULE — a new definition slot needs no C#), and a reference fails to parse and is re-coded to COBOLNET0901 by
`CobolErrorListener` (below).

⛔ **WHY NOT A PREDICATE — it was one, and it could not work (kb/Work PB655, `feedback_left_edge_predicates`).**
Until PB655 every gated word was a `cobolWord` alternative behind `{userWordHere("W")}?`. ANTLR evaluates a
semantic predicate only at the LEFT EDGE of the decision it is predicting, and `cobolWord` sits deep inside every
reference, so for every ENCLOSING decision the gate was invisible: at COBOL-2002, where NULL is reserved,
`SET P TO NULL` chose the SET-to-identifier format on the strength of a NULL "name" the gate then refused, and the
parse failed with COBOLNET0901. Admitting PB655's 73 words that way turned 115 Conformance cases red. A token
type is visible to every prediction; that is the whole reason the gate lives there.

⛔ **WHY DECLARED, NOT MERELY FREE — a DETERMINATION.** The union grammar still spells a construct at the editions
that lack it so its named edition gate can answer (`GOBACK.` at COBOL-85 → "the GOBACK statement requires
COBOL-2002"), and the migration mode admits a word the edition ADDED while that word is also the edition's
keyword. Retyping every free word unconditionally (GnuCOBOL's per-standard reserved list) would turn `GOBACK.` on
its own line at COBOL-85 into a PARAGRAPH named GOBACK — silently. So a free word is a user word exactly when the
program uses it as one. A keyword alternative whose word is free at the compile edition and that the program did
not declare is an undeclared user-word reference where §8.9 gives it no keyword reading: `className`'s BOOLEAN
and the seven 2014 numeric-content words are read as class-name-1 by `ConditionBinder` there (→ COBOLNET1639),
which the ORDER of the `className` alternatives used to do while the gate was a predicate.

**The same decision reaches every re-parse of the text (`Parsing/TokenRetypes`).** The binder RE-LEXES source
text for the D18 subscript / reference-modifier segment and the D2 keyword-omitted argument list
(`FragmentParse`); a fragment that lexed afresh would read `T(GOBACK + 1)` — GOBACK a declared item at COBOL-85 —
as the keyword again. `TokenRetypes` (the `>>COBOL-WORDS` map plus the freed words) is stored on the tree
(`CompilationUnitContext.TokenRetypes`), carried by `BindSession` → `DataBinder` → `BinderContext`, and applied by
`FragmentParse` exactly as `LexAndParse` applies it — which also gives the fragments the `>>COBOL-WORDS` retype
they lacked.

⛔ **NO LEXER TOKEN MAY COST THE USER A WORD ISO LEAVES FREE (kb/Work PB655).** A word the lexer tokenizes never
reaches `IDENTIFIER` by itself, so without a `nameSlot` row it is refused as a user-defined word at every edition.
`CobolWordsDriftTests.LexerWordTokens_Cost_The_User_No_Word_Iso_Leaves_Free` joins the COMPILED lexer vocabulary
against the rows: every single-word token must be admitted (a §8.10 context-sensitive word as a plain `cobolWord`,
a §8.9-straddling word through the token-level gate), be §8.9-reserved at all four editions, or be one of the
`extensionReserved` words ISO §4.2.10 lets an implementation reserve for a nonstandard extension (documented as
`docs/CONFORMANCE.md` D-RW1).

**`userWordHere` is the §8.9 admission rule, and it lives in `ReservedWordSet.AdmitsAsUserWord`** — the assembly
that owns §8.9 — so the grammar gate and the funnel that reports COBOLNET0901 cannot disagree about which
occurrences are names. It is `reservedHere` plus the migration mode: `--permissive` accepts what the edition
REMOVED, and a word §8.9 took away is exactly that (`ConstructAvailability.Removed`), so there the gate stands
down, the pre-reservation reading is restored, and the 0901 comes back as a WARNING on a program that runs. A gate
that ignored the permissive axis would turn the whole class into parse errors no severity policy can downgrade.

⛔ **BUT THE EXEMPTION IS NOT A BLANKET `Edition.Permissive ||` (kb/Work PB792).** A spelling §8.9 reserves at the
compile edition AND at every older edition this compiler targets — COLUMN and DESTINATION are reserved at all four
— was never a user-defined word anywhere, so migration mode has no pre-removal reading to restore; its verdict is
`NotYetIntroduced`, an error on BOTH axes. Written as a blanket bypass the gate handed all fifty-nine gated words
back to `cobolWord` under `--permissive`, and every greedy operand list absorbed them: at `--std 2023 --permissive`
NIST **RW104A** card 024500 — `03 VALUE "DETAIL LINE " COLUMN 20 PIC X(12).`, legal because §13.15.3 SR2 says
*"All other clauses may be written in any order"* — parsed COLUMN and 20 as VALUE operands, lost its §13.18.14
COLUMN clause, and printed the field at column 1 with no diagnostic at all. The axis therefore routes through the
ONE `EditionSeverityPolicy`: the slot admits the word exactly where a §8.9 violation here would be DOWNGRADED to a
warning, never where the policy says error. `ReservedWordMigrationGateDriftTests` pins both halves, derived from
`reserved-words.json` over every word × edition × axis, so the next always-reserved word is covered by
construction. ⚠ The report-section VALUE clause must NOT be narrowed to dodge this: §13.18.63.2 format 4 prints
`{ VALUE IS | VALUES ARE } { literal-1 } …` — one or MORE literals (§13.18.63.3 SR35 confines the multi-operand
form to a repeating entry) — so the operand list is greedy by design and the reservation gate is what stops it.

`reservedHere` keeps its own meaning ("is this token the reserved keyword here") for `facilityWord`
and the SPECIAL-NAMES CRT/CURSOR clause guards, which must keep recognizing their clauses under `--permissive`.

**The gate has exactly ONE exclusion, and it is derived too: the §15 intrinsic function names that collide with a
reserved word** (the `functionName` rule in `Grammar/Core/CobolExpressions.g4` — of the name-slot rows, LENGTH,
NATIONAL and BIT). A `cobolWord` occurrence of one of these is the KEYWORD-OMITTED function reference §15 permits
(`COMPUTE N = LENGTH(A)` reaches the name through `cobolWord`, not through a FUNCTION-led rule) — a use OF the
reserved word, the same distinction `IsBareFunctionArgumentWord` draws for §15 phrase words. Gating them turned
five conforming 2023 goldens into COBOL0001. They also carry no swallow risk: a function name leads no statement
and no clause.

⛔ **A KEYWORD SLOT MAY NOT BORROW `cobolWord` (kb/Work PB693).** The gate gives `cobolWord` one meaning —
*user-defined word* — so a rule that used it to match a reserved KEYWORD that happens to carry a lexer token is
broken at exactly the editions §8.9 reserves that word, and the parse-error path below then prints a FALSE
"'W' … cannot be used as a user-defined word" over a slot where the program never used it as a name. One rule
did: `validateStatusStage` (`Grammar/Core/CobolDeclined.g4`), the VALIDATE-STATUS clause's ON phrase, whose three
choice indicators FORMAT / CONTENT / RELATION are all UNDERLINED in §13.18.62.2 — keywords (§5.2.2), not names.
FORMAT is §8.9-reserved from 2002 and does have a token (the FD FORMAT clause, §13.18.24), so `ON FORMAT …`
became a parse error at 2002+ instead of the declined-facility COBOLNET1708. **The fix is a token arm in the
BORROWING rule, never an exclusion from the gate** — the word is reserved, and every other slot must keep
rejecting it. **⛔ THE SWEEP THAT FOLLOWED THAT SENTENCE WAS SCOPED WRONG, AND THE CORRECTION IS kb/Work PB704.**
It read: "FORMAT is the ONLY §8.9-reserved word carrying a lexer token that any rule expects `cobolWord` to match
as a keyword. Every other keyword the grammar routes through `cobolWord` — LOCALE, ORDER, CLASSIFICATION,
ATTRIBUTE, UCS-4/UTF-8/UTF-16, RELATION, NONE, RECEIVED, COBOL — is §8.10 context-sensitive or unclassified by
the standard, has NO lexer token, arrives as IDENTIFIER, and no gate can reach it." Two halves of that are FALSE.
LOCALE, ORDER, COBOL, NESTED, SYSTEM-DEFAULT and USER-DEFAULT are §8.9-RESERVED, not context-sensitive
(`reserved-words.json`), and **"no gate can reach it" was never measured**: the reservation GATE inside
`cobolWord` cannot reach an IDENTIFIER, but the §8.9 FUNNEL can and does — `VisitCobolWord`'s `CheckedTokenTypes`
holds `IDENTIFIER` and screens it POSITION-BLIND, precisely because the 2023 additions all lex as IDENTIFIER. So
having no token is not safety, it is the SAME defect by the other route, and `SORT … WITH DUPLICATES IN ORDER`
(§14.9.40.2) was refused as a user-defined-word use at 2002/2014/2023 for two trains
(`feedback_reachability_is_measured_not_deduced`). **The sweep's real axis is "is this word §8.9-reserved at any
edition", never "does it carry a token"**, and the fix is the same either way: the word becomes a lexer token
with a `cobol-words.json` nameSlot row, so the reservation gate + `reservedGatedWord` keep the NAME slot correct
at every edition and the KEYWORD slot never enters `cobolWord`. ORDER took that shape at PB704 (retiring the
`orderTableAhead()` text predicate and the funnel's ORDER TABLE slot exemption with it).

⛔ **A KEYWORD THAT CANNOT TAKE A TOKEN RIDES `formatWord`, NEVER `cobolWord` (kb/Work PB764).** Some keywords
cannot become tokens: LOCALE must stay a bare word inside `LOWER-CASE(x LOCALE …)`, which
`IntrinsicBinder.KeywordWordOf` reads by text, and the LC_ categories and CLASSIFICATION are §8.10
context-sensitive. Until PB764 each §8.9-reserved one (LOCALE, NESTED, COBOL, SYSTEM-DEFAULT, USER-DEFAULT) reached
the funnel as a `cobolWord` and was held off it by a NAMED exemption in `VisitCobolWord` — six `ctx.Parent is`
arms and an ancestor walk, a hand list and therefore the next PB704. They now parse through the shared rule
`formatWord : IDENTIFIER` (`CobolParserCore.g4`), the twin of `reservedGatedWord`: the funnel visits `cobolWord`
only, so it never meets a `formatWord` BY CONSTRUCTION, and the list is gone. The recognizing predicate stays at a
LEFT EDGE, in one of two shapes: the enclosing clause's own text predicate where the slot is only the keyword
(`localeClause`, `pictureLocalePhrase`, `characterClassificationClause`, `setLocaleStatement`), or a
literal-argument `{wordAhead("NESTED")}?` on the keyword arm of a brace that offers the keyword OR a name
(`callAsPhrase`, `entryConventionName`, `localePhrase`, `setLocaleSource`, `alphabetLocalePhrase`). Never a rule
parameter: a predicate that reads `$w` is context-dependent and ANTLR cannot evaluate it while predicting from an
enclosing rule. The NAME arm of each brace stays `cobolWord`, and that split exposed two slots the ladder had
exempted WRONGLY: `entry-convention-name-1` and `external-locale-name-1` are SYSTEM-NAMES (§8.3.2.3.1), and §8.3.2.1
rule 1 forbids a reserved word there exactly as in a user-defined word, so `LOCALE L1 IS USER-DEFAULT` is now
COBOLNET0901 (`conformance:negative/pb764-reserved-word-as-system-name`). What the funnel still lets pass is
NON-positional only: a bare §15 function-argument phrase word, a construct declined whole
(`DeclinedFacilityPass.EnclosingDeclinedConstruct`, its set drift-tested against `CobolDeclined.g4`'s entry points),
and the EXCEPTION-OBJECT / DEBUG- registers. `FormatWordDriftTests` derives the tokenless §8.9-reserved words from
`reserved-words.json` and the lexer, parses every enabled positive golden that writes one at its own edition, and
fails on any such word that reaches `cobolWord` outside those allowances — so a new format that borrows
`cobolWord` for its keyword fails even if a ladder arm is re-grown to silence its golden. The §8.10
context-sensitive keywords in a keyword-OR-name slot (ATTRIBUTE, RELATION, UCS-4/UTF-8/UTF-16) may still ride
`cobolWord`: §8.9 never reserves them, so the funnel is inert for them. The computer paragraphs' old word sink
(`computerAttributes`) is gone (kb/Work PB830, §3.10): their residue is the `unrecognizedClause` error
production, and their deleted '85 clauses are modelled rules.
**A REFERENCE to a gated word is answered by the parse-error path.** The gate leaves no name-slot alternative for
`DISPLAY CONSTANT.` at `--std 2002`, and a source that fails to parse never reaches the bound-tree funnel. So
`CobolErrorListener` asks the parser whether the offending token is reservation-gated (the generated
`CobolLexer.IsReservationGated` set) and still rejects at this edition, and re-codes the diagnostic to
COBOLNET0901 with the ONE message `ReservedWordSet.UserWordViolationMessage` owns. The cause is named in every
position — reference slots included, which the funnel deliberately never screened. It is named ONCE per
occurrence of the word: ANTLR raises TWO syntax errors on one offending token (the prediction failure and
`CobolErrorStrategy`'s recovery message), which read as two different sentences before the re-code and as the
same sentence twice after it — and §8.3.2.1 rule 1 is broken once by one occurrence.

⛔ **AN OPERAND WHOSE INTRODUCING WORD IS OPTIONAL MAY NOT SWALLOW THE NEXT CLAUSE (kb/Work PB848).** §13.18.60.2
prints `POINTER [ TO type-name-1 ]`, `FUNCTION-POINTER TO function-prototype-name-1` and
`PROGRAM-POINTER [ TO program-prototype-name-1 ]` with TO NOT underlined (rendered, folio 503), so by §5.2.3 the
operand may follow the usage keyword directly. With TO omitted the operand slot is a bare `cobolWord` at the END
of a clause, so it would swallow any keyword that both BEGINS a §13.16.2 data-description clause and reaches
`cobolWord`. **The reservation gate above does not settle that on its own**: it admits a clause keyword wherever
the keyword is not reserved (an older edition, `--permissive`), and it deliberately EXEMPTS the §15 function names
— and BIT and NATIONAL are also bare USAGE keywords, so `01 P USAGE POINTER BIT.` bound BIT as a type-name. That
was MEASURED, not reasoned: a first cut dropped the guard after PROPERTY (gated) proved safe without it, and the
drift test then failed on BIT; a second cut that excluded only clause-LEADING words then let
`USAGE POINTER HIGH-ORDER-RIGHT` bind the endianness phrase (UsageFloatFormatPhraseDriftTests caught it). So the
TO-less arm is `{pointerOperandHere()}? cobolWord`, a LEFT-EDGE predicate (`CobolParserCoreBase`) refusing any
token in FOLLOW(`usageKeyword`) — the USAGE tail phrases and whatever may follow the clause — **computed off the
generated ATN** (`UsageFollowSet`, a context-free FOLLOW over every call site) — never a hand list, so a clause or
USAGE phrase added to the grammar is excluded automatically. `PointerUsageOperandDriftTests` (Unit) derives
FOLLOW(usageKeyword) ∩ FIRST(cobolWord) from the ATN and, for every such word at every edition, parses
`USAGE {POINTER|PROGRAM-POINTER|FUNCTION-POINTER} W.` and demands an empty operand. The binders read the OPERAND's presence, never the TO token's (`DataBinder`:
the §13.18.60.3 SR18/SR19 screen and FUNCTION-POINTER's mandatory operand). The same
REQUIRED-INSIDE-AN-OPTIONAL-GROUP shape was OBJECT REFERENCE's `(FACTORY OF)?`, whose OF is also unstressed on
folio 503; it is `(FACTORY OF?)?` now. Both are pinned by `OptionalWordSubsetDriftTests` rows, because
`audit_grammar_optional_words.py` cannot see a word required inside an optional group (kb/Work PB715).

⛔ **Why this is generated and not written by hand (kb/Work PB300, CLAUDE.md rule 5).** The second half used to be
a hand-written list of two words inside `CobolData.g4`'s `dataName`, paired with a hand-written
`ctx.COMMIT() ?? ctx.ROLLBACK()` extraction in the funnel — three places naming the same set. It had already
rotted: CRT and CURSOR were reservation-gated by kb/Work PB301 and added to neither, so `01 CRT PIC X.` at
`--std 2002` answered a parse error that never named §8.9. Then the GATE SET itself was a hand-set
`reservationGated` row property, and **fifty-one further §8.9-straddling words never got one** — UNLOCK among
them, so a period-less `UNLOCK F1` after a `MOVE` was swallowed as two more receivers and legal COBOL-2002 source
was rejected (kb/Work PB693). Deriving the set killed the flag: `gen-cobol-words.ps1` now THROWS if a
`reservationGated` property reappears in the JSON. `reservedGatedWord`'s alternatives are all single
tokens, so the funnel reads the subrule's own text and needs no list either. `PROCEDURE` stays a hand-written
`dataName` alternative ON PURPOSE — it is reserved at *every* edition and NC205A legally names a data item with
it, so it must never reach the funnel.

⛔ **WHICH READING WINS WHEN A WORD COULD BE AN OPERAND OR THE NEXT KEYWORD — ONE derived predicate (kb/Work
PB805).** A greedy operand list whose items bottom out at `cobolWord` absorbs any keyword token `cobolWord`
admits, and the construct that keyword begins vanishes. The answer used to be per word and per list — PROPERTY
in two VALUE loops, DEFAULT in INITIALIZE, five phrase words in DELETE FILE — and ALL(*) lookahead luck
everywhere else, so under `--permissive` (which restores GROUP-USAGE as a user word) `01 G VALUE N"AB"
GROUP-USAGE NATIONAL.` lost its §13.18.29 clause. Now every keyword-token `cobolWord` alternative carries the
generated `{!keywordContinuesHere()}?` (`CobolParserCoreBase`). ANTLR hoists it into the loop decision of every
list whose body opens with a name; there it walks the ATN from the loop's EXIT through the live rule-invocation
stack and ends the list when the lookahead can be matched as a KEYWORD of what follows (`cobolWord` and
`reservedGatedWord` count as name positions, not keyword positions). The rule it implements: §8.3.2.1 1)
(a reserved word is never a user-defined word) and 3) (a context-sensitive word is a keyword inside its
construct). A §8.9-straddling word reaches such a list only as the IDENTIFIER the token-level gate made of a
DECLARED free word, so it is simply one more operand (`DISPLAY "A=" ALTER` at 2002); undeclared, it is still its
keyword token, `cobolWord` does not offer it, and the union grammar's named edition gate answers
(`DISPLAY X END-DISPLAY` at 85). `declareName` (a grammar action on `dataName` and the SELECT file-name) remains
only for the §15 function-name words BIT/LENGTH/NATIONAL, which are `cobolWord` alternatives by the gate's one
exclusion. `OperandListKeywordDriftTests` bans hand-written `TokenStream.LA(`
guards in the parser grammar and proves the GROUP-USAGE family over its derived population.

`CobolWordsDriftTests` asserts the generated grammar fragments match the JSON (parallel to the existing
`ReservedWordsDriftTests`), and `CobolWordsG4_ReservationGate_Is_Derived_From_Section89` RECOMPUTES the step-4b
derivation independently — from `reserved-words.json` and the `functionName` rule, never from the grammar it is
checking — then pins set-equality in BOTH directions against the emitted `reservedGatedWord`, that none of it is
a `cobolWord` alternative, that no `userWordHere` predicate survives in the generated grammar, plus the confidence
alignment the generator throws on (a gated word must be a high-confidence row, or the grammar would reject where
the conservative funnel stays silent). A word reserved at EVERY edition is gated too: §8.9 never frees it, so the
token-level gate never retypes it and the declaration half carries the whole job. Result: adding a context-sensitive keyword — a
reservation-gated one included — is a **one-line JSON edit**; the lexer HashSet, the two parser rules, and the
reserved-word funnel can no longer silently disagree.

⛔ **A DECLINED MODULE TOKENIZES ITS WORDS, AND OWES EVERY ONE OF THEM A `nameSlot` ROW (kb/Work PB301).** This
compiler declines Annex A.4.2 (screen handling) by TOKENIZING the module's words, so the refusal can name itself
(COBOLNET1560 / COBOLNET1707) instead of drawing a generic parse error — but §8.3.2.1 rule 3 ("Context-sensitive
words may be used as user-defined words and system-names in contexts other than the language construct in which
they are defined") means a lexer token is not a reservation, and the fifteen §8.10 screen words are legal
user-defined words at EVERY edition while the seven §8.9 ones (COL, COLS, COLUMN, COLUMNS, CRT, CURSOR, SCREEN)
are barred only where §8.9 reserves them — all but COLUMN from 2002. Declining a module may cost the user its
constructs; it may never cost the user its words. So each of the 22 carries a `nameSlot` row and the §8.9 seven
ride the derived gate above, and the SPECIAL-NAMES pair is gated the other way too: `crtStatusClause` /
`cursorClause` are `{reservedHere("CRT")}?` / `{reservedHere("CURSOR")}?`, so `SPECIAL-NAMES. CURSOR IS FOO.` at
`--std 85` stays the §12.3.7 implementor-switch entry it is instead of drawing a screen verdict.
**The population is DERIVED, never listed** (CLAUDE.md rule 5): `ScreenFacilityConstructDriftTests` reads
`CobolScreen.g4` plus the SPECIAL-NAMES screen clauses, joins them against `CobolLexer.g4`'s single-literal
tokens, keeps what §8.9 leaves free at ≥ 1 edition, and asserts (a) every member has a `nameSlot` row and
(b) every rule `ScreenFacility` refuses — read off its `Report*` context-parameter types — is either in
`CobolScreen.g4`, in the named external pair, a `screenClause` alternative shared with the data description
entry, or explicitly excluded. Each external rule is scraped BY NAME and must be found: a length-only guard was
an OR over the two rules and left the whole gate green when one was renamed. The same shape is what any future
declined module owes its words.

**3.3b SUBSCRIPT-mode vocabulary (D9 partial).** The SUBSCRIPT mode stays (the `x(i)` vs `(a+b)`
disambiguation genuinely needs lexer-mode context and cannot be cleanly expressed in the parser given
COBOL's grammar), but the *duplicated* literal/operator token bodies are factored into a shared lexer
fragment `fragment` rule set (`NUM_BODY`, `STR_BODY`, `NAT_BODY`, `BOOL_BODY`, `NAME_BODY`) referenced by
both the DEFAULT tokens and the `SUB_*` tokens, so `"-15.6"` / `N"AB"` tokenization rules exist once. This
removes the "keep SUB_* in sync with DEFAULT" hazard without touching the mode-switch strategy.
**The trigger is narrowed per compile (kb/Work PB1465).** `_dataNameTokens` is edition-blind — every word that
is a user-defined word in SOME edition — so `TokenRetypes.PrimeLexer(lexer, edition)` hands the lexer the
trigger tokens whose word the edition RESERVES (§8.9 at the edition's year, `--permissive` off) and the program
has not DECLARED (`ReservedWordSet.AdmitsAsUserWord` for the strict edition, §8.3.2.1 rule 1), minus the parser's
`functionName` first-set (a keyword-omitted intrinsic call keeps its capture, §8.4.3.2.3 SR2). A '(' after such
a word groups: `IF BZ B-OR (BW B-AND BW)` at 2002+ parses by §8.8.2 Table 4. Under `--permissive` the word's
legacy reading is the PROGRAM's declaration of it (kb/Work PB1669): the parse finds declarations (the §8.9
gate's `FreedReservedWords`, which `--permissive` also fills for a reserved word it admits), and when a found
declaration changes which '(' open a SUBSCRIPT (`TokenRetypes.LexesDifferentlyFrom`) `Frontend.LexAndParse` lexes
and parses the text again with the word declared — so a legacy table `B-OR (1)` still subscripts beside an
undeclared B-AND operator that takes a parenthesis. A program that declares no reserved word parses once. The
binder-side subscript re-parse (`ReferenceResolver.SplitSubscriptTokens`) is addressed by the binder
rearchitecture (structured `Place` path segments), not here; the frontend's contribution is to preserve the
captured token run faithfully.

**3.3c Mode inventory** stays: DEFAULT, PICMODE, SUBSCRIPT, COMMENT_MODE (`CobolLexer.g4:497,651,726,782`).
No change to mode semantics; only the shared-fragment factoring in 3.3b.

**3.3e ⛔ The lexer carries NO semantic predicate — context the lexer needs goes in an ACTION (kb/Work PB1715).**
ANTLR 4.13.1 caches a mode's start state only when no predicate is in its closure (`LexerATNSimulator.MatchATN`),
and never caches a DFA edge whose target passed through one (`AddDFAEdge` returns before storing the edge), so a
token that reaches a predicate re-runs ATN simulation — and re-adds its DFA state under `lock (dfa.states)` — on
EVERY occurrence. Six left-edge predicates in DEFAULT mode did that to every token of every compile, which made
the lexer most of a check-only compile's time and serialized parallel compiles on the one lock
(`DESIGN-test-build-ci.md` §3.14.5 M6). Each context-dependent rule now matches unconditionally and its action
decides what the match is:
- `DEFINED` (§7.3.8.4.4) has no rule: it is a VIRTUAL token (the `tokens { }` block, beside `FNARG_LPAREN` /
  `FNARG_RPAREN`) that `IDENTIFIER`'s action retypes when the lexer is primed for a compiler-directive expression.
  ⚠ A literal rule `'DEFINED' {retype}` would lex the same tokens but publish the literal name `'DEFINED'`, and the
  vocabulary's literal names MEAN "the lexer makes this word a keyword token" to `CobolKeywordTokens` (the
  `>>COBOL-WORDS` word → token map) and `CobolWordsDriftTests` — a token-context keyword must never publish one;
- `FNARG_SEPARATOR` matches the §8.3.5 2) comma/semicolon-space separator everywhere and `Skip()`s it outside a
  function-argument region (the unpredicated `COMMA_SEP` twin it needed is deleted);
- the four `FN_SIGNED_*` twins match `[+-]` + a numeric body, and `OnSignedLiteral` keeps the signed literal where
  `SignedLiteralCanStart` holds (§8.3.3.3.2 2) / §8.7.1); elsewhere it cuts the token back to its one-character
  sign (`PLUS`/`MINUS`, the column restored) and the lexer resumes at the digits — exactly the two tokens the
  predicate-false path lexed.
`LexerDfaCacheDriftTests` holds the invariant over the suite's sources (a private DFA; after a warm-up pass, no
ASCII character of a re-lex leaves the cached DFA and every mode has a cached start state). A character above 127
has no DFA edge in ANTLR (`MAX_DFA_EDGE`) and the EOF transition is never cached; both are reported, never asserted.

**3.3d The ALL figurative's literal-1 (kb/Work PB71, 2026-08-18; PB1627, 2026-09-26).** `figurativeConstant`'s
Format-6 arm is `ALL allLiteral`, where `allLiteral : concatenationExpression | STRINGLIT | HEXLIT | NATLIT |
BOOLLIT` — ONE arm for the four literal kinds §8.3.3.6.3 SR2 admits (alphanumeric plain or hexadecimal, national
N/NX, boolean B/BX), and literal-1 "may be a concatenation expression", which is THE `concatenationExpression`
rule below, operands and all. Two load-bearing decisions: (1) `nonNumericLiteral` lists `figurativeConstant` BEFORE
`concatenationExpression`, because `ALL "A" & "B"` is genuinely ambiguous between "ALL over the concatenated
literal-1 AB" (legal, SR2) and "a concatenation whose first operand is the figurative ALL "A"" (illegal, §8.8.3.2
SR1) — ANTLR resolves a true ambiguity toward the lower alternative, so the legal reading wins, while `"X" & ALL
"A"` still parses as a concatenation and is rejected COBOLNET1541 by `ConcatFolder`. (2) Every binder consumer
reads literal-1 through ONE reader, `ConcatFolder.FoldAllLiteral` — the procedure-division figurative binder
(`BoundAllLiteral.Of(Folded)`), the boolean channel and its routing predicate, the VALUE reader
(`RawValueOperandText`) and the SPECIAL-NAMES ALPHABET/CLASS literal phrase — so literal-1 is folded, classed and
screened identically everywhere. It resolves all three written shapes: a quoted literal; a concatenation expression,
through the same pairwise `Walk` as every other concatenation (so `ALL "A" & SPACE`, `ALL "A" & K` and `ALL "A" &
STAR` are legal — §8.8.3.2 SR1 admits figurative operands, §13.10.3 SR2 a constant-name); and a constant-name
written alone, which parses as the `ALL cobolWord` arm (the parse cannot tell it from a symbolic-character) and
is resolved as Format 6 before the Format 7 readers see it. With a reporting context it asks SR2 — COBOLNET1648 for
a zero-length literal-1 (structurally: contiguous delimiters, never "decodes to nothing"), COBOLNET2491 for a
constant-name standing for a numeric literal — and every §8.8.3.2 rule of a concatenated literal-1; the
per-position diagnostic dedup keeps a figurative read at two sites to one report. The version pass's
`VisitFigurativeConstant` keeps only the statement-scoped 2002 gate for a national/boolean literal-1 (the `&` gate is
`VisitConcatenationExpression`'s, on recognition); the §8.3.3 literal-token rules are LiteralScreenPass's. The rules
moved to the binder because they need the program's constant-names, which the parse walk has not got (the version
pass used to ask them over a quoted-only operand list — kb/Work PB1393's arm, PB1627).

**The `&` operand and its class (kb/Work PB1406, 2026-09-26).** `concatOperand : STRINGLIT | NATLIT | BOOLLIT |
HEXLIT | figurativeConstant | cobolWord`. The `cobolWord` arm is how §8.8.3.1's literal-1 / literal-2 admit the two
WORDS that stand for a literal — a constant-name (§13.10.3 SR2) and a symbolic-character, a figurative constant by
§12.3.7.4 GR11 a) — which the parse cannot tell from a data-name, so the arm admits any word and
`ConcatFolder` resolves it through its `LiteralEnvironment` (COBOLNET2473 for a word that stands for no literal;
COBOLNET2474 for a symbolic-character operand in a §12.3.7.3 SR11 SPECIAL-NAMES literal). `&` appears in no
other rule, so a word followed by `&` in a position that also admits an identifier is predicted as a
concatenation by one token of lookahead. The grammar still FLATTENS the left-recursive format to `operand (&
operand)+`; `ConcatFolder` folds the class back pairwise, left to right (§8.8.3.3 GR1 a/b/c per pair), so `SPACE &
SPACE & N"AB"` is the alphanumeric expression `SPACE & SPACE` concatenated with a national literal — COBOLNET1540 —
never a chain classed by its first non-figurative operand. Every fold site names its `LiteralEnvironment`
(`Program`, `SpecialNames(nationalPhrase, sr11)` or `Unscoped`): the environment carries the HIGH-/LOW-VALUE
characters of the position (the program collating sequences outside SPECIAL-NAMES, §12.3.7.4 GR10's native
extremes of the clause's NATIONAL phrase inside it) and the constant-name / symbolic-character tables, and its
constructor is private so no site can omit the national table again. The ALL literal-1 arm above shares this
operand model: its concatenation IS this rule (kb/Work PB1627).

**3.3e The computer paragraphs (kb/Work PB78, 2026-08-18).** `objectComputerParagraph : OBJECT_COMPUTER DOT
(({!objectComputerClauseAhead()}? computerName)? objectComputerClause* DOT)?` — ISO §12.3.6.2's `[computer-name-1]`
is optional and the two clauses (`programCollatingSequenceClause | characterClassificationClause`) may follow the
period in any order (each at most once, §5.2.6.4 — a duplicate is COBOLNET1652 in the binder). Two load-bearing
decisions: (1) the X3.23-1985 clauses ISO 2002 deleted are MODELLED — `memorySizeClause` and `segmentLimitClause`
are `objectComputerClause` alternatives, `debuggingModeClause` hangs off SOURCE-COMPUTER's name — and each is
gated at its own node (`VisitMemorySizeClause` / `VisitSegmentLimitClause` / `VisitDebuggingModeClause`, the last one
obsolete at 2002 and removed at 2014 — row `debugging-mode-removed-2014`, owner decision R61); both
paragraphs end in the `unrecognizedClause` error production (§3.10; kb/Work PB830 replaced the `~DOT` sink that
was there). (2) `characterClassificationClause :
{classificationAhead()}? CHARACTER? formatWord (…)` — CLASSIFICATION is not a token (a plain word at '85), so the
arm is predicated on the word itself, from the left edge; the clause BINDS since kb/Work PB64 T5 (A.4.9 item 7
claimed; it was parse-to-diagnose COBOLNET1518 until then — the LOCALE clause's shape), and its keywords
(CLASSIFICATION, and LOCALE / SYSTEM-DEFAULT / USER-DEFAULT in `localePhrase`) are `formatWord`s the §8.9 funnel
never meets, while a locale-name-1 reference stays a `cobolWord` (kb/Work PB764). The name-less clause form is the 2002 relaxation of the '85 required-name
format (`computer-name-optional-2002`, `VisitObjectComputerParagraph`); `sourceComputerParagraph` is
`((computerName debuggingModeClause? unrecognizedClause?)? DOT)?`.

### 3.4 Delete dead grammars; quarantine JSON/XML (D5)

- **Delete** `Grammar/CobolDialect.g4`, `CobolParserGenerics.g4`, `CobolParserJsonXml.g4`,
  `CobolParserOO.g4`, `CobolPreprocessor.g4` (all unreferenced; two are stale duplicates of live code).
- **Move** `inlineMethodInvocationStatement` (the one live rule) from `Core/CobolExtensionsJsonXml.g4` into
  `Core/CobolOO.g4` (its true home — 2023 OO); it parses at every `--std`, carries its construct-id
  annotation at the `statement`-rule dispatch site, and is gated by the `VersionConformancePass`.
- **Delete** `jsonStatement` / `xmlStatement` from the fragment and the two `{is2014()}? jsonStatement` /
  `xmlStatement` arms from `CobolParserCore.g4:716-717`, plus the `jsonXmlExceptionPhrases` rule, and the
  `JSON`/`XML`/`PARSE`/`PROCESSING`-as-JSON handling. **Delete** the now-empty
  `Core/CobolExtensionsJsonXml.g4`.
- **Lexer tokens:** `JSON`/`XML` tokens: keep `PARSE`/`PROCESSING` (they remain context-sensitive user words
  per `cobolWord`), but remove `JSON`/`XML` keyword tokens if no ISO construct uses them (they become plain
  IDENTIFIERs — verify no lexer rule else-branch depends on them). The vendor JSON/XML→COBOL0313 disposition
  lives in `CobolErrorStrategy` as a token-keyed vendor hint — it is a parse-error re-diagnosis of
  hard-reserved tokens, not an ISO edition gate — so no recogniser is involved.

### 3.5 Generated-parser build (D7 + D8)

Behavior is already correct (portable flat-output regen, fail-hard, `.gitignored` `Generated/`,
java+pwsh prerequisites — `Invoke-Antlr4CSharp.ps1`, `GenerateIfNewer.ps1`). Target changes are hygiene:

1. **Package name → MSBuild property (DONE).** The generated namespace `CobolNet.Frontend.Generated` is a csproj property
   (`AntlrNamespace`) passed to `GenerateIfNewer.ps1` as `-PackageName`; this performed the D8 namespace rename with one
   property (the `using Core = CobolParserCore` aliases in consumers are a separate cleanup).
2. **Delete the committed `.antlr/` caches** (`Grammar/.antlr/`, `Grammar/Core/.antlr/`) and add
   `**/.antlr/` to `.gitignore`. They are IDE build output; one caches a dead grammar.
3. **Narrow the SLL-bail catch** (`Frontend.cs:135`): `catch (Antlr4.Runtime.Misc.ParseCanceledException)`
   (thrown by `BailErrorStrategy`) plus `catch (RecognitionException)`. A `NullReferenceException` /
   `InvalidOperationException` from a buggy semantic predicate or lexer action now propagates as an internal
   compiler error (surfaced by the driver's top-level boundary — see DESIGN-driver.md) instead of being
   silently retried under LL.
4. Keep the two-stage SLL→LL strategy verbatim (proven; the design does not touch the prediction pipeline).
   `ZeroTokenRewriter`'s ALGORITHM is likewise untouched — but see §7 for the one thing that changed underneath
   it: what counts as an arithmetic parenthesis (fix-queue PB48).

### 3.6 Preprocessor pipeline (namespace + one contract fix)

The preprocessor runs in this order (`Frontend.Normalize` then `Frontend.Preprocess`, `src/Cobol.Net.Frontend/
Pipeline/Frontend.cs`):

```
PhysicalLines.Read (line entry: terminators, tabs, --nist archive markers — kb/Work PB1800) → NormalizeToFreeFormMapped (§6.5, main source)
  → ConditionalCompilationProcessor.Manipulate, INTERLEAVED with CopyProcessor (§7.2.1; library text normalized by
    the same §6.5 walker — PB1067) → NistPreprocessor (if --nist) → DirectiveSiteProcessor → TurnDirectiveProcessor
  → PropagateDirectiveProcessor → RefModZeroLengthDirectiveProcessor → FlagDirectiveProcessor → COBOL-WORDS / LEAP-SECOND
```

Every stage after the COPY/conditional-compilation merge is line-count preserving (asserted). Status of the target
changes:
1. **Namespace** `CobolNet.Frontend.Preprocessor` (D8) — done.
2. **One severity policy** (D4) — done: `ReferenceFormatProcessor.ReferenceFormatDiagnostics` asks
   `EditionSeverityPolicy.For` and reads its rows from `ConstructRegistry`; no stage keeps a local `if (permissive)`.
   ⚠ Library text still gets NO `ReferenceFormatDiagnostics` (`CopyProcessor.NormalizeCopybookMapped` passes
   `diagnostics: null` — kb/Work PB1640), and the object is built per normalization call, so its once-per-compilation
   gates are once per FILE.
3. **Keep** the `TurnDirectiveProcessor` line-count-neutrality assertion (`Frontend.cs:103-105`, hazard H3) —
   it is a real safety invariant for TURN anchoring; convert the `throw` into a recorded internal diagnostic
   (consistent with the top-level exception boundary), not a raw exception.
4. The preprocessor remains hand-written (not the dead `CobolPreprocessor.g4`); that grammar is deleted (§3.4).

#### 3.6.0 Reference format — the ISO §6.5 logical conversion is ONE pass (kb/Work PB1491, PB1493)

`ReferenceFormatProcessor` (`ReferenceFormatProcessor.LogicalConversion.cs`) IS the §6.5 logical conversion, for both
reference formats, and the only place source comments are recognized. Its model:

- **The initial format is a SELECTION, fixed by default** (kb/Work PB1362). §7.3.24.3 2) "The default reference format
  of a compilation group is fixed form"; `InitialReferenceFormat` (`--source-format fixed|free|auto`,
  `CompilerDriver.Options.SourceFormat`, `Frontend.InitialFormat`) is the §4.2.10 3) selection mechanism, and
  `IsFixedForm` runs only under `auto` — no detector can be the default, because the sequence area may hold any
  character (§6.3.2) and text past margin R is legal, so every detector misreads some conforming fixed-form text.
  Library text starts in the format in effect for its COPY statement (§7.3.24.3 3)).
  ⚠ The test harnesses (conformance, characterization) pass `auto`, and the detector's past-column-72 veto reads
  2,905 of the 3,576 conformance sources as FREE form, so most goldens do not exercise the CLI's fixed-form reading
  (kb/Work PB1757, measured 2026-09-30; `tests/conformance/README.md` states the contract).
- **ONE line-entry stage: `PhysicalLines`** (`PhysicalLines.cs`, kb/Work PB1800 / PB1586 / PB1496 / PB1803) — the only
  place decoded source or library text becomes LINES, called once, at the entry of `NormalizeToFreeFormMapped` (main
  source and library text alike). It owns the four determinations ISO §6.1 leaves to the implementor:
  **terminators** (DOC-A.1-156: LF, or CR LF; a CR not followed by LF is a character of the line — GnuCOBOL's
  `ppinput`; the lexer's line end is the LF only, so no stage ends a line at a lone CR); **tabs** (DOC-A.1-157: every
  TAB becomes the one to eight spaces up to the next tab stop — positions 1, 9, 17, … — everywhere, literals included,
  owner decision R55; the line keeps a `TabMap` from expanded position to physical index ONLY when it held a tab, for the
  physical columns PB1801 needs); **the `--nist` archive markers** (`*HEADER,` / `*END-OF,` become empty lines, only when
  `ccvsIndicators`, PB1803); and the line NUMBER (every `PhysicalLine` carries the file's own, so a SOURCE FORMAT segment
  needs no offset). Every consumer — `IsFixedForm` (the Auto detector), `FirstSourceDirectiveCandidate` / `DirectiveText`
  / `FormatSegments`, `FixedFormConverter.Convert` and `ConvertFreeLines` — takes `ReadOnlySpan<PhysicalLine>` and never
  a string, so no caller can bypass the reader; `StripNistArchiveMarkers`, `ConvertFixedToFree`, the string
  `IsFixedForm` and the per-segment re-join are deleted. The free-form 255-position limit (§6.1 3) a); COBOLNET2653) is
  asked in the free-form arm (`ConvertFreeLines`) on the expanded line — never on a fixed-form line, which may run past
  margin R (§6.1 2) b)). Nothing after the stage has a tab or CR rule: the lexer's `WS` set is `[ \n]`, `TextWordScanner`
  and `SeparatorRule` agree, and `PhysicalLinesDriftTests` fails if a pipeline stage spells `\t` or `\r`, if a
  reference-format walker splits text itself, or if `PhysicalLines.Read` gains a second production caller.
  ⚠ The stage's scope estimate ("five consumers, one slot") held; what the estimate missed is that the lexer and the
  text-word scanner each carried a private copy of the tab and CR rules (nine lexer sites, `TextWord`, `SeparatorRule`,
  three `TrimEnd('\r')` in later stages) — deleted in the same change.
- **The source map has no column** (kb/Work PB1801): `MappedText` records one `SourceOrigin(File, Line)` per
  resultant line, so a fixed-form diagnostic column is program-text-relative (7 short — PB1688), a COPY/REPLACE
  diagnostic is column 1 (PB1671), and a continuation fragment keeps the continued line's origin — and, after PB1586, a
  diagnostic column on a line that held a tab is the EXPANDED position, not the editor's physical column (the `TabMap`
  is the stage's answer; nothing consumes it yet). **Target:** an origin span list per resultant line, born in the
  line-entry stage and carried by `OriginWriter` (PB1801).
- **The format is STATE of an in-order walk** (`FormatSegments`, kb/Work PB1361): each line is asked whether it is a
  directive in the program-text area of the format in effect (`DirectiveText` — positions 8 to margin R of a source
  line in fixed form, the whole line in free form; §7.3.3 SR2/SR3), `>>SOURCE` and a format-changing `>>POP` update the
  state, and a SOURCE FORMAT directive on the first line may be in either format (§7.3.24.3 4)).
  `CompilerDirectiveLine.TryParse` has ONE reading — only spaces before `>>` — shared with every later stage.

- **ONE `LogicalLineBuilder` both formats feed** (kb/Work PB1804, PB1359, PB1492): the resultant compilation group is a
  list of lines with a parallel origin list (kb/Work PB82) — one resultant line per physical line, except a continuation
  line, which occupies none — together with the LATEST LOGICAL line and the literal state that line ends in
  (`LiteralState`: the opening quotation symbol, the literal kind, the form of continuation used so far, and whether
  the line ended in a floating indicator). Each reference format supplies only a per-line classifier:
  `FixedFormConverter` reads the indicator area and the program-text area (positions 8–72, space-filled), the free
  converter takes the whole line. The builder's operations are the §6.5 rules: a blank or comment line is logically
  discarded (§6.5 2)) — it keeps its slot as an EMPTY line, is never the join target and never touches the literal state
  (§6.3.5 / §6.4.2 "Comment lines and blank lines may be interspersed among lines containing the parts of a literal");
  a source line is a new logical line (§6.5 5), 7)); a fixed continuation line (hyphen in column 7) joins the latest
  logical line (§6.5 6)); and **the line after one that ended in a floating literal continuation indicator** — a
  quotation symbol followed by a hyphen, ending the line inside a literal — is the continuation line of §6.5 8), in
  EITHER format (§6.5 4) cuts the continued line before the indicator, §6.5 8) appends the continuation line's content
  after its opening quotation symbol). Writing the join against "the last line written" is what made an interspersed
  comment line receive the continuation and a blank line crash the origin tracking.
  The continuation RULES are checks of this state, written once for both formats: the first nonblank character of a
  continuation line is the literal's opening quotation symbol (§6.2.3.2 SR6; COBOLNET2684), a national literal is
  continued only by the floating form (§6.3.5 2); 2685), a floating indicator never stands on a fixed-continuation
  line (SR5; 2686), a literal is continued by one form only (SR4; 2687), a join never spells `==` or `::` (§6.3.5 2); 2688,
  and `*>` / `>>` keep 2496), no inline comment follows the indicator (§6.3.7.3 / §6.4.4.3; 2689), and a free-form
  line of a continued literal holds literal content (§6.4.2; 2690). §6.2.3.2 SR4's first sentence — the floating
  indicator only for an alphanumeric, boolean or national literal — has no violation to detect: the indicator is
  recognized only inside a literal, and every quotation symbol opens one of those three kinds. The one literal-aware
  scan also finds a compiler directive written after program text (§7.3.3 SR2; 2691), so a rule broken in a copybook
  or in free form is found where it is broken. Pseudo-text continues by these rules for free (§7.2.3.3 SR8, §7.2.4.3 8)):
  the join precedes text manipulation.
- **The join target is the LATEST LOGICAL line** (§6.5 6) a)/b) "appended immediately to the right of the last
  character in the latest logical line of the resultant compilation group"), tracked by index. **A compiler directive
  line is never a join target** (§7.3.3 1) "specified on one line", kb/Work PB1360): the builder records whether the
  latest logical line is a directive line (`_latestIsDirective`, set where the line is emitted by the SAME
  `CompilerDirectiveLine.TryParse` + `CompilerDirectiveCatalog.IsDirective` recognition every directive stage uses), and
  a fixed-form hyphen line or the line after a floating literal continuation indicator that follows one is
  COBOLNET2696 and takes a discarded slot instead of extending the directive's operand — in both formats, IF and
  EVALUATE lines included (§7.3.16.3 1), §7.3.13.3 2) each put the operand entirely on its line).
- **The indicator area holds a §6.2.2 fixed indicator or it is diagnosed** (kb/Work PB1494): `*` `/` `-` space, and
  COBOL-85's debugging `D`; anything else is COBOLNET2616 and the line is read as source. The NIST CCVS column-7
  conventions (S/Y debugging, P/J/H/E/U excluded alternates, any other letter a primary-configuration line) are a
  DIALECT — `ccvsIndicators`, on only under `--nist` (`FixedFormConverter.KindOf`). The obsolete comment-entry reading
  of AUTHOR … REMARKS applies only inside an IDENTIFICATION DIVISION (`_inIdentificationDivision`).
  The edition-scoped questions are decided by LATER stages and the registry, not by this pass (kb/Work PB1802, owner
  decision kb/Work R61): a comment-entry paragraph (AUTHOR … SECURITY, REMARKS) keeps its HEADER as program text — the
  header line is written `AUTHOR. .`, the paragraph's terminating period standing for the discarded comment-entry,
  because COBOL-85 ended the entry at the next Area-A word — and only the entry (the rest of the header line and its
  Area-B body) is discarded, at every edition, so the `identification-comments-removed-2002` / `remarks-removed-2002`
  gate (COBOLNET0902 from 2002) sees a fixed-form paragraph exactly as it sees a free-form one. The comment-entry exists
  only in an IDENTIFICATION DIVISION, so library text is converted starting in the division its COPY statement stands in
  (`DivisionCursor` reads it off the text-words before the COPY; `CopyProcessor` hands it to the shared walker, and the
  state is carried across a text's SOURCE FORMAT segments) — a `REMARKS.` paragraph copied into a PROCEDURE DIVISION is
  program text; and the `D` indicator is
  gated by the registry row `debugging-line-removed-2014` (accepted at 85, obsolete at 2002 — COBOLNET0903 — removed at
  2014 — COBOLNET0902; the NIST S / Y letters are the CCVS dialect, never gated, and excluded as comment lines). A `D`
  line is NOT decided here (kb/Work PB1705, owner decision R56): the clause that makes it source is further down the text
  than the line, so the converter CARRIES it — `ReferenceFormatProcessor.DebugLineCarrier` + its program text — and the
  lexer reads the carrier as the hidden `DEBUG_LINE` marker and the text as ordinary tokens. The builder reads the line
  like any source line (`LogicalLineBuilder.EmitDebugging`: the literal state it ends in is tracked, a blank one is a
  blank line, kb/Work PB1914) and, so that both readings agree on every other line, a `D` line written while the latest
  logical line is inside a literal that needs its continuation is passed over like a comment — the continuation line
  after it continues the literal (§6.3.5 2)) — and keeps a logical line of its own after the joined one
  (docs/CONFORMANCE.md D-DEBUG);
  `Parsing/DebuggingLineRewriter` (first in `Frontend.LexAndParse`) keeps those tokens when the line's source unit, or a
  unit containing it (§12.3.5.4 GR1), declares SOURCE-COMPUTER … WITH DEBUGGING MODE and moves them to the
  `ABSENT_DEBUG_LINE` channel otherwise, so the parser, `SeparatorRule` and `PictureSeparatorPeriodRule` never read an
  absent line. It reads the unit nesting (PROGRAM-ID … END PROGRAM) and the clause off the TOKENS, so a change to the
  `sourceComputerParagraph` / `debuggingModeClause` rules is a change to its `DeclaresMode`. A debugging line inside an open
  PICTURE clause or subscript region is the one shape it cannot settle on tokens (kb/Work PB1913): which of "source" and
  "comment" the line's text is decides whether the text moves the LEXER's own mode (a `)` that closes the subscript,
  the picture string that ends PICMODE). PICMODE and SUBSCRIPT therefore skip the line's text and leave a
  `DEBUG_LINE_IN_REGION` marker (`PIC_DEBUG_LINE` / `SUB_DEBUG_LINE`); `Rewrite` returns the FIRST such line whose unit
  declares the mode, `Frontend.LexAndParse` blanks that line's carrier (`WithCarrierBlanked`: same columns, same
  offsets) and lexes the text again, and the loop ends when no region line is source. Only the first per pass,
  because the tokens after a skipped source line were lexed without its text; the tokens up to and including it are
  exact, and a region sits after the unit's SOURCE-COMPUTER paragraph. The fragment re-parses
  (`FragmentParse`, `ReparseArgs`) read a slice of THAT text, in which every carrier left is a comment line, so
  `HideAll` is exactly right for them.
- **The program-text area is always positions 8–72**, a shorter record read as space-filled to margin R
  (DOC-A.1-157), so a continued literal carries every position to margin R (§6.3.5).
- **A column is a CHARACTER POSITION — one code point — and never a string index** (DOC-A.1-157, kb/Work PB1966): a
  supplementary-plane character is two UTF-16 units and one position. `CharacterPositions` (count, index of a position,
  position of an index) is the one definition, `FixedFormLine` the one reader of a fixed-form line's columns (sequence
  area, indicator, program-text area, margin R), `PhysicalLines` expands tabs by position, and the builder's diagnostic
  columns are positions; `PhysicalLinesDriftTests.NoPipelineStage_ReadsAColumnAsAStringIndex` refuses a column read as
  `line[6]`, `line[7..72]` or `line.Length > 72`.
- **ONE literal-aware scan** (`ScanProgramText`) serves every line kind, with the literal state CARRIED IN — so §6.5
  3) removes an inline comment on a continuation line too, a quotation symbol ends a literal only when it is the one
  that opened it, and it stops at the floating literal continuation indicator and at a directive after program text.
  It replaced two scanners that disagreed (a comment strip that assumed "not in a literal" and a
  state scan that treated either quotation symbol as closing).
- **Comments never leave this pass.** A comment line becomes an empty line and an inline comment is cut, in fixed
  AND free form, so no later stage (the lexer's picture mode, the COPY/REPLACE text-word scanner, the NIST
  substitutions) has a comment rule to get wrong. The lexer keeps `COMMENT_START` (and `PIC_COMMENT` /
  `SUB_COMMENT` in its other modes) only for the INTERNAL `*>` carriers written after this pass: the COPY stage's
  not-found notes. The debugging-line carrier is read as the `DEBUG_LINE` marker (kb/Work PB1705) — and inside a
  PICTURE or subscript region as `DEBUG_LINE_IN_REGION`, which the frontend turns into plain source or leaves a
  skipped comment (kb/Work PB1913).
- **The reference-format diagnostics live in ONE per-COMPILATION object** (kb/Work PB1640): `ReferenceFormatDiagnostics`
  (edition, strictness, the bag), created once per front-end pass and shared by the main source and EVERY copybook —
  library text is read by the same §6.5 walker (§6.5 applies to "lines of source text and library text"), so a rule
  broken in a copybook is reported at the copybook's file and line, and a once-per-compilation gate fires once, not
  once per file (the file is an argument of each report, never state of the instance). It holds the edition gates —
  `floating-comment-indicator-2002` and `floating-literal-continuation-2002` (COBOLNET0900, once per compilation, asked
  in FIXED form only: free form is itself a 2002 introduction reached below 2002 only through the `--source-format
  free|auto` selection, and its comment and its literal continuation are the floating ones), `debugging-line-removed-2014`,
  `col7-continuation-obsolete-2023` and `fixed-form-word-continuation-removed-2023` — and the syntax-rule diagnostics
  above, plus SR2 (a `*>` not preceded by a separator space — COBOLNET2495; the text after it is still the comment) and
  SR3 (a continuation line completing a `*>` or `>>` begun at the end of the line it continues — COBOLNET2496).

#### 3.6.1 Directive state — `>>PUSH` / `>>POP` (kb/Work PB941)

ISO §7.3.22.4 GR2 makes PUSH/POP a rule about a SET — "the state of all of the directives other than EVALUATE, IF,
PAGE, POP, or PUSH" — while the state itself lives where each directive is processed: the reference format in the
normalizer, the compilation variables and the frontend-inline FLAG options in the conditional-compilation driver,
the line-scoped toggles (TURN, REF-MOD-ZERO-LENGTH, FLAG-02/14) and the group-prefix values (COBOL-WORDS,
LEAP-SECOND) in the post-COPY stages. The design is therefore ONE mechanism with carriers, never per-directive save
code:

* **`DirectiveStateStack`** (Frontend/Preprocessor) — the PUSH/POP pairing: a stack PER pushable directive row,
  `PUSH ALL` fanning out to `CompilerDirectiveCatalog.PushableRows` (DERIVED from the PUSH row's
  `excludedDirectives`, so a new directive row is pushed by existing), a POP removing the most recent save, and
  `Apply` answering §7.3.20.4 GR2's "unsuccessful". Each state-holding stage runs its own instance over the same
  PUSH/POP sequence and registers an `IDirectiveStateCarrier` for the rows it holds; every instance keeps a stack
  for every row, so the pairing is identical in every stage.
* **Where the ops come from.** The normalizer and the conditional-compilation driver meet PUSH/POP lines in
  encounter order and `Apply` them directly (the driver only in an emitting branch). `DirectiveSiteProcessor`
  records the ops of the FINAL text, blanks the lines, and issues the one COBOLNET2297 warning; every later stage
  REPLAYS those ops with `AdvanceTo(line)` as it scans.
* **Line-scoped folds** keep their event lists: a stage COLLECTS its events into a `DirectiveEventLog` (row, line,
  event) and `ToTimeline(ops)` REPLAYS the op program over them through the stack — a per-row carrier saves "how
  many events were in effect" and restores by REVOKING every event recorded since, at the POP line. The binder folds
  a `DirectiveTimeline<T>` with one extra test (`InEffectAt` / TurnState's `RevokedFor`); events are never reordered
  or synthesized. A timeline keeps its events AND its op program, so its revocations are always a replay, never a
  patch — which is what lets a later phase add ops only it can place (`WithStackOps`, below).
* **Group-prefix values** (COBOL-WORDS, LEAP-SECOND) replay the ops up to the first compilation unit — the point
  §7.3.10.3 SR1 / §7.3.17.3 SR1 close the region — and take the state in effect there.
* **The compilation-variable table is ALSO a timeline** (kb/Work PB1368). The conditional-compilation driver holds the
  live `_defines` map for the directives it evaluates (IF / EVALUATE / DEFINE / DISPLAY operands), but §7.3.11.4 GR1
  permits two uses OUTSIDE conditional compilation — "in any compiler directive where a literal of the category
  associated with the name is permitted … or in a constant entry where the FROM phrase is specified" — and each must
  read the table AS OF ITS OWN LINE ("in text that follows a DEFINE directive"), so a final map is the wrong product.
  The driver records every change a DEFINE made as a `CompilationVariableEvent` (resultant line, name, `CtValue` or
  null for OFF / a value-less PARAMETER, the operand as written) at the encounter's line; `Frontend.Preprocess` collects
  them under the DEFINE row and replays the PUSH/POP program over them like any other timeline
  (`DirectiveResults.CompilationVariables`, claimed by the `DefineDirective2002` registry entry; GR14's implicit ops
  join through `WithStackOps`). The ONE fold is `CompilationVariableTimeline.DefinitionAt(name, line)` — the per-name
  `TryLastInEffectBefore`. Readers: `CobolWordsDirectiveProcessor` (a word in a literal slot is a variable defined
  before the directive, screened by SR2 as the literal its DEFINE wrote) and `DataBinder.BindConstantFrom`
  (§13.10.3 SR8 / §13.10.4 GR1–GR2).
* **`DirectiveStateRegistry`** accounts for every pushable row: carried (naming the stage types and the
  `DirectiveResults` members) or, with its rule, why this compiler holds no state (LISTING — no listing is produced,
  §7.3.18.3 GR1; DISPLAY — stateless; CALL-CONVENTION — no state implemented; FLAG-85 /
  FLAG-NATIVE-ARITHMETIC — removed at 2023, the only edition with PUSH). `DirectiveStateStackTests` is the drift
  gate: the registry equals the pushable set, every carrier is a real stage, every `DirectiveResults` member is
  claimed, and every carried row has a PUSH/change/POP behavioural case.

* **The implicit PUSH ALL / POP ALL of §14.9.28.4 GR14** ("An implicit PUSH ALL followed by TURN OFF ALL is
  assumed at the end of imperative-statement-1. Immediately preceding the END PERFORM phrase, there is an implicit
  POP ALL …") go through THIS mechanism (kb/Work PB1004, PB1066). Only the parse tree can place them, so the FRONT
  END collects them after the parse (`ExceptionPerformDirectiveScope.ImplicitOps`, in `Frontend.Preprocessor`: PUSH
  at imperative-statement-1's last line, POP at the END-PERFORM line, token order on a shared line; the Format-3
  test is `Frontend.Parsing.PerformFormat.IsFormat3`, the one discriminator the binder and the edition gate also
  ask) and delivers them to BOTH places pushable state lives:
  - **the post-COPY line-scoped timelines** — `DirectiveResults.WithStackOps` replays them with the written ops into
    EVERY event timeline, so a TURN, REF-MOD-ZERO-LENGTH or FLAG-02/14 written in a WHEN or FINALLY phrase is
    revoked at END-PERFORM, and one written in imperative-statement-1 (before the PUSH) survives. `Frontend.Directives`
    carries the replayed timelines; the binder folds them as given. `ExceptionPerformDirectiveScopeTests` holds the
    drift test that every `DirectiveTimeline<T>` member is replayed.
  - **the conditional-compilation driver's state** — the compilation-variable table (§7.3.22.4 GR3: "all instances
    of that directive are pushed") and the frontend-inline FLAG options. The driver runs BEFORE the parse, so
    `Frontend.Parse` is a FIXED POINT: `ConditionalCompilationProcessor.Manipulate` returns the resultant text with
    every directive ENCOUNTER (its resultant line and whether it changed the driver's state), the parse places the
    ops, `ConditionalCompilationResult.KeyImplicitOps` keys each op to the first directive encounter on a later line
    and drops every PUSH/POP pair that encloses no state change, and the text manipulation re-runs with that program
    (the driver applies a keyed op immediately before its encounter, through its own `DirectiveStateStack`) until
    the parse places the program it was run with. An ordinary source — no DEFINE, FLAG or PUSH/POP inside a handler
    — converges on the first pass with no re-run. Each pass settles at least one more encounter (an op changes only
    the text after the first directive whose state it changes), and a pass that does not parse is final unless its
    recovered tree places ops the pass did not run with (the bootstrap below). Only the
    converged pass's diagnostics are reported. The encounter lines are exact across COPY (the driver tracks the
    output-frame line at which each copybook's expansion is spliced — `ExpandCopiesOneLevel` hands the splice
    offset to the callback) and across REPLACE (`CopyProcessor.ReplaceLineMap` runs the REPLACE pass over
    line-index origins; a line REPLACE removed or joined maps to where that text now starts).
  - **the reference-format normalizer's state** — the SOURCE FORMAT in force (§7.3.22.4 GR2: ALL includes it), the one
    pushable state settled before every other stage, because the §6.5 logical conversion reads the PHYSICAL lines in
    order with the format as state. The same fixed point covers it, keyed in the normalizer's own frame:
    `ImplicitFormatOps.Place` maps each op from the resultant-line frame through the source-line map to its FILE and
    PHYSICAL line (each file — the compilation group and every library text — is converted by its own walker with its
    own format state, §7.3.24.3 3 and 5), keeps only a bracket that encloses a written format directive of that file
    (`ReferenceFormatMap.DirectiveLines`; a pair across two files is kept by neither), and the walker
    (`ReferenceFormatProcessor.FormatSegments`, through the same `DirectiveStateStack` the written `>>PUSH`/`>>POP` use)
    applies each op as a BOUNDARY between physical lines (`ImplicitBoundaries`): the PUSH after the line that ends
    imperative-statement-1, the POP BEFORE the line that holds END-PERFORM — so the restored format governs the
    END-PERFORM line itself ("immediately preceding the END PERFORM phrase"; §7.3.24.3 1: a format governs the text
    following the directive). `Frontend.Parse` re-normalizes each pass and `CopyProcessor` hands each library text its
    own ops. **Bootstrap:** the unrestored format often makes the text after the handler unparsable (a free-form
    `END-PERFORM` read as a sequence area), so a pass that did not parse reads its ops off the parser's RECOVERED tree
    and, where the recovered construct has no END-PERFORM token, `Frontend.LocateEndPerform` finds the phrase in the
    PHYSICAL text (the first `END-PERFORM` word after imperative-statement-1 outside a comment line and outside a nested
    PERFORM that parsed). That is a guess, verified by the fixed point: speculative re-runs are made while they place
    ops the previous run did not have (each settles at least one more bracket; at most `SpeculationBound` — the
    directives the text holds, plus two), and if none parses the FIRST failure's diagnostics — the source's own — are
    reported. The TURN OFF ALL half is
    `TurnState.WithAllDisabledFrom` (the handler floor) in `EcBinder`.
* **Where the ALL form may be written** (§7.3.22.3 SR3 / §7.3.20.3 SR3: "only in a compilation unit, between clauses
  in divisions other than the procedure division, and between statements in the procedure division") is decided by
  `Validation/DirectivePlacementPass (renamed from PushPopAllPlacementPass, kb/Work PB1065; it also judges FLAG-02/14 and the LEAP-SECOND/PROPAGATE placement rows)` from the directive sites (`DirectiveSite.AllForm`) and the parse tree: the
  directive's position is the gap between the tokens around its line; outside every unit, or inside the innermost
  `…Clause` / statement context spanning the gap (unless the gap is a boundary of a nested one), draws the §4.2.2
  warning COBOLNET2344 (kb/Work PB1005). A reference-format note that belongs here: §7.2.1 Step 1 REQUIRES a SOURCE
  FORMAT directive in the false path of an IF/EVALUATE to be processed, which is why the normalizer runs over the
  whole text before conditional compilation (kb/Work PB1006, retired).

### 3.7 Parse-tree consumption — a typed `Cst/` façade (D6)

**Contract problem:** the binder reaches into raw generated contexts with ~336 `GetText()` calls. Renaming a
grammar rule silently breaks dozens of string-keyed walks.

**Target:** a `Cst/` (Concrete-Syntax-Tree façade) namespace in `Cobol.Net.Frontend` that wraps the generated
contexts in **thin, typed, read-only accessors** — the ONE surface the binder consumes. Not a re-parse: each
façade type holds the generated context and exposes typed properties instead of positional `GetText()`:

```csharp
// Cst/DataDescriptionCst.cs  (wraps CobolParserCore.DataDescriptionEntryContext)
public readonly struct DataDescriptionCst(CobolParserCore.DataDescriptionEntryContext ctx)
{
    public int Level          => ParseLevel(ctx.levelNumber());        // was int.Parse(x.GetText())
    public string? Name       => ctx.entryName()?.GetText();
    public PictureCst? Picture => ctx.pictureClause() is {} p ? new(p) : null;
    public UsageCst? Usage    => …;
    public bool IsTypedef     => ctx.typedefClause() is not null;
    public SourceSpan Span    => SourceSpan.Of(ctx);                    // line/col for diagnostics
    …
}
```

The façade is generated-context-shaped (1:1 with grammar rules), so it does NOT re-introduce a parallel model
— it is the *typed reading discipline* over the parse tree. Migration is incremental (§8): the binder can mix
raw-context and façade access during transition; the end state is that `Grammar/Generated` types are
`internal` to `Cobol.Net.Frontend` and only `Cst/` types cross the assembly boundary. This makes a grammar
rule rename a **compile error in one façade file** instead of a silent `GetText()` drift, and it is the
enabling step for the binder's "consume a typed tree, not strings" reorg.

The `VersionConformancePass` does not consume the parse tree at all — it walks the *bound* tree (each bound
node carries a `.Syntax` back-reference for spans and construct ids), so it needs neither the façade nor the
generated visitor. The façade is for the *binder*.

### 3.8 Diagnostics model — one `Diagnostic` type

The frontend already has the good pieces: `Diagnostics/DiagnosticDescriptor` (a typed
`{Code, Severity, MessageTemplate}` record) and `DiagnosticDescriptors` (a registry:
`CBL0901`, `COBOL0303`, `COBOLNET0900`, …) — `Diagnostics/DiagnosticDescriptors.cs`. This is exactly the
model the *compiler* side lacks (its 163 codes are bare strings — the understandability-critique HIGH). The
target:

1. **`DiagnosticBag`** (frontend) becomes the ONE diagnostic collector for the whole pipeline; its
   `Diagnostic` carries `{DiagnosticDescriptor, SourceSpan, args}` (code + location + severity structured all
   the way to the CLI). It moves namespace to `CobolNet.Frontend.Diagnostics` (D8).
2. The compiler-side `EditionContext.Diagnostics`/`Warnings` `List<string>` accumulators are replaced by this
   `DiagnosticBag` (owned by DESIGN-edition-framework.md / DESIGN-driver.md; the frontend provides the type). The
   frontend's descriptor-registry pattern is the template the compiler's 163-code registry adopts.
3. `DiagnosticDescriptors` is the frontend's registry; construct/edition codes (the COBOLNET0900 band) are
   sourced from `Cobol.Net.Editions` so frontend and compiler emit identical wording (one message, two emit
   layers — the EditionGateHints remark's own goal, achieved structurally).

### 3.9 Error recovery

- The two-stage SLL(bail)→LL(recover) strategy stays.
- ⛔ **The SLL pass carries NO error listener** (kb/Work PB396). SLL is an APPROXIMATION of LL: it can fail on
  input full LL prediction accepts, and when it fails the LL pass re-derives every diagnostic from token 0. A
  listener attached across both passes therefore reported each real syntax error TWICE — once in ANTLR's own
  wording (`BailErrorStrategy` inherits `DefaultErrorStrategy.ReportError`), once in `CobolErrorStrategy`'s —
  and reported a FALSE error whenever SLL was merely the weaker predictor. `Frontend.Parse` now attaches
  `CobolErrorListener` inside the LL retry only, so the authoritative pass is the only one that diagnoses.
- `CobolErrorStrategy` keeps its COBOL-intent heuristics (`GuessCobolIntent`, 19 rules) — these are genuinely
  useful and not duplicative. It carries no edition logic: edition diagnosis lives in the
  `VersionConformancePass` (§3.2), and the recogniser call (`CobolErrorStrategy.cs:113`) is gone with the
  recogniser. It hosts the token-keyed vendor JSON/XML→COBOL0313 hint (§3.4), and the two §3.11 arms.
- The `[code] message` construction (`CobolErrorStrategy.cs:93-95`) is retargeted to build a structured
  `Diagnostic` (descriptor + span) rather than a pre-formatted string, so downstream layers keep structure.
- ⛔ **A parenthesized group is atomic to recovery** (kb/Work PB2152; ISO §8.3.5 4), parentheses only in balanced
  pairs). `CobolErrorStrategy.ConsumeUntil` consumes a group it meets while resynchronizing WHOLE, through its
  balancing ')' of any of the three paren twins (stopping early at the separator period or end of file), and
  `SingleTokenDeletion` never deletes a '('. Once the reference paren became its own token (kb/Work PB2113) the
  integer inside `05 A(1)` or `X(3)` resynced as a level-number and the orphaned ')' reported a second, spurious
  error; the behavior-neutrality oracle caught it, and `ParseRecoveryParenGroupTests` pins the count.
- Recovery beyond that and the sync-point behavior is out of scope for this rearchitecture (the battery does
  not exercise multi-error recovery quality; changing it risks the green net for no measured gain) — but a
  change in the diagnostics a negative fixture produces is visible to the oracle
  (`DESIGN-architecture-review.md` §4.1), so a recovery regression no longer passes unseen.

### 3.10 Closed general formats, and the ONE ERROR PRODUCTION that replaced the vendor catch-all (kb/Work PB487, PB829)

**The rule.** A general format in the standard is a CLOSED list. §4.2.2 makes the general formats and syntax
rules the definition of what may be written, so a word the format does not print is not admissible — and a
compiler that DISCARDS such a word compiles a program its author did not write.

**What was there.** `dataDescriptionClause` ended in
`genericDataClause -> genericClause : IDENTIFIER (IDENTIFIER|literal)*`, a vendor-extension hook matching any run
of words at the tail of a §13.16.2 Format-1 entry. Three harms, all the same harm:

1. the §13.18.1 ALIGNED clause — a REAL clause of that format, with no rule of its own — parsed and did
   nothing, so `05 B2 PIC 1(4) USAGE BIT ALIGNED.` laid out at the wrong bit offset in silence;
2. the bare `01 M MESSAGE-TAG.` that §13.18.60.2 makes legal bound with no usage at all, and the missing
   `PicInfo` escaped the binder into an unhandled `NullReferenceException` in the emitter;
3. every "…the only other clauses permitted are…" rule of §13.16.3 was unenforceable against a word the parser
   never classified — SR12, SR13, SR17 and SR18 alike.

**The replacement.** `unrecognizedClause` (`Core/CobolExpressions.g4`), an ERROR PRODUCTION placed LAST in the
alternative list. It still RECOGNIZES the word run, which keeps recovery local (one diagnostic per entry rather
than a cascade), and every instance is REFUSED by name — **COBOLNET1941** for this format, naming the word and
citing §13.16.2. Refused at every edition and every strictness: **a vendor extension is admitted only under the
dialect that owns it, never by a catch-all**, and this compiler declares no vendor dialect. The strictness axis
is for documented leniency about REMOVED constructs, which this is not.

**The structural half.** Closing the list is only half the fix, because the §13.16.3 permitted-set rules were
each an `||` chain over whichever local decode flags their author remembered, and each was incomplete. Those
rules now read ONE clause-presence SET — `DataClauseKind` / `DataDescriptionCst.WrittenClauses`, one bit per
Format-1 clause slot — with the permitted/excluded sets written as constants transcribed from the rules' own
sentences. `DataClauseKindDriftTests` reflects over the GENERATED parser and fails, in both directions, when a
`dataDescriptionClause` alternative has no `DataClauseKind` or a mapped context type is no longer produced. That
is what makes the next clause automatic rather than remembered.

**The placement half (kb/Work PB507 / PB512 / PB518 / PB519).** The same vocabulary carries the rules about WHERE
a clause may be written. `ClausePlacementRules.Rules` (`Binding/DataBinder.ClausePlacement.cs`) holds one row per
rule sentence, of five shapes — `ElementaryOnly` (§13.18.40.3 SR1, §13.16.3 SR11), `Residence` (level 1 in named
sections: §13.16.3 SR6 / §13.18.27.3 SR1 b) for GLOBAL, §13.18.22.3 SR1 for EXTERNAL), `DataNameRequired`
(§13.16.3 SR7, both halves), `NotAtLevel` (§13.18.38.3 SR1 a), OCCURS at level 01/66/77/88 — kb/Work PB1260) and
`NotWith` (§13.16.3 SR5) — and TWO sites read it: `BindEntry` applies the
entry-local rows through `ScreenClausePlacement` (level, entry-name, section and the written set are all known
there) and `CheckElementaryOnlyClauses` applies the `ElementaryOnly` rows over the finished forest. A refused
EXTERNAL / GLOBAL clause never reaches `DataItem.HasExternalClause` / `HasGlobalClause`, and those two facts are the
ONLY inputs of `CallBindExternalAndGlobal`'s run-unit re-basing and global-name registration — the post-bind scan no
longer re-derives either rule from the parse tree (it used `continue` filters, which annulled a misplaced clause
without a word, and never read the FILE SECTION). The subject rules of the same two clauses — what the elementary
item may BE (§13.18.8.3 SR1/SR2, §13.18.32.3 SR3; SR4 is §13.16.3 SR18's COBOLNET1563) — are the `CheckClauseSubjects` pass, after usage
inheritance. `ClausePlacementRules.PlacementAnsweredElsewhere` names, for every clause kind with no row, the site
that owns its placement, and `DataClausePlacementDriftTests` requires every `DataClauseKind` to be answered exactly
once and every `ElementaryOnly` subject to be refused on a group — so a clause added to the grammar is asked where
it may be written.

**The generalization — ONE production, ONE pass, ONE table (kb/Work PB829).** `genericClause` was not one
catch-all; it was one RULE reached from SIX sites spanning EIGHT closed general formats, and PB487's sibling
sweep — which read the grammar by rule NAME — counted four of the remaining five and missed the I-O-CONTROL
paragraph's INLINE alternative, which is not a named `xxxClause : genericClause` wrapper. All eight are now
closed the same way, and the way is structural rather than repeated — and the two computer paragraphs, whose
residue was a DIFFERENT sink (`computerAttributes : ({!objectComputerClauseAhead()}? ~DOT)+`, kb/Work PB830),
joined the table the same way, which is the table doing its job: a tenth format was a table row, not a mechanism.

| § | format | grammar site (alternative list) | code |
|---|---|---|---|
| §13.16.2 | data description entry | `dataDescriptionClause` | COBOLNET1941 |
| §13.4.5.2 | file description entry (FD) | `fileDescriptionClause` | COBOLNET1970 |
| §13.4.6.2 | sort-merge file description entry (SD) | `sortMergeDescriptionClause` | COBOLNET1970 |
| §12.4.5.1 | file control entry (SELECT) | `fileControlClauses` | COBOLNET1970 |
| §12.4.6.2 | I-O-CONTROL paragraph | `ioControlClause` | COBOLNET1970 |
| §12.3.7.2 | SPECIAL-NAMES paragraph | `specialNameEntry` | COBOLNET1970 |
| §12.3.5.2 | SOURCE-COMPUTER paragraph | `sourceComputerParagraph` (after the name) | COBOLNET1970 |
| §12.3.6.2 | OBJECT-COMPUTER paragraph | `objectComputerClause` | COBOLNET1970 |
| §12.3.2 | configuration section | `configurationParagraph` | COBOLNET1971 |
| §11.2.1 | identification division | `identificationParagraph` | COBOLNET1971 |

- **ONE production.** `genericClause` is referenced from `unrecognizedClause` and nowhere else; every site spells
  `| unrecognizedClause`, LAST.
- **ONE table.** `Frontend/Cst/ClosedFormats.cs` keys the format's §, subject, noun (`clause` vs `paragraph` —
  the §12.3.2 and §11.2.1 lists are lists of PARAGRAPHS, and calling one a clause sends the reader to the wrong
  subclause) and code on the PARENT context type.
- **ONE pass.** `Compiler/Validation/ClosedFormatPass.cs`, a sibling of `DeclinedFacilityPass` run from
  `BinderDriver`. A parse-tree walk rather than a binder hook, and here that is load-bearing: the §13.16.2
  refusal used to live in `DataBinder.BindEntry`, which the level-66 and level-88 paths return before, so
  `88 CN WIBBLE VALUE 1.` dropped the word in silence — measured on the pre-change build, where only
  COBOLNET1747 fired. Four of the other formats bind nothing at all by construction.
- **ONE drift guard.** `ClosedFormatDriftTests` reads the `.g4` files (the word-run matcher is referenced only by
  the error production) AND reflects over the generated parser (every context type carrying an
  `unrecognizedClause()` accessor has a table row, and every row is still carried), so adding a closed format is
  `| unrecognizedClause` plus a table row, and forgetting the row fails the build.
- **The other half of a closed format — an element printed ONCE, written twice (kb/Work PB917).** A bracket with no
  ellipsis permits its element at most once (§5.2.6.2, §5.2.7), and the grammar writes only the order licence
  (`clause*`). `Frontend/Cst/ClauseCardinality.cs` (`ClauseCardinalities.ByClauseContext`) keys the clause lists whose
  figures were read for ellipses — the §13.16.2 data description entry, the §13.15.2 report group description entry,
  the §13.4.5.2 file description entry, the §13.4.6.2 SD entry and the §12.4.5.1 file control entry — and names the
  alternatives that DO repeat (the file control entry's ALTERNATE RECORD KEY and collating-sequence clauses, an
  error production, the declined validation clauses); `ClosedFormatPass.VisitChildren` counts every clause list's
  elements against it and reports COBOLNET2423 through the one `UnrepeatedElements` reader the RD entry already uses.
  `ClauseCardinalityDriftTests` makes the table total: every closed format is a row or a reviewed exemption
  (SPECIAL-NAMES, I-O-CONTROL, OBJECT-COMPUTER and SOURCE-COMPUTER repeat their elements or hold one), so a new
  closed format cannot arrive without its figure being read for ellipses.
- **And the ORDER half (kb/Work PB1508).** §5.2.1: the elements of a general format "shall be written in the
  compilation group in the sequence given in the general format, unless otherwise specified by the rules of that
  format". Every row DECIDES its order through `ClauseList.Sequence`: null where a syntax rule frees it (§13.16.3 SR4,
  §13.15.3 SR2, §13.4.5.3 SR2, §12.4.5.2 SR1), else the once-only elements in printed sequence — the §11.9.2 OPTIONS
  paragraph's seven clauses (its rule was an order-free `optionsClause+` folded last-wins, so a reordered or repeated
  clause compiled clean), the §12.3.2 CONFIGURATION SECTION's four paragraphs and the §11.2.1 identification
  division's OPTIONS paragraph (a second one was never read). `ClosedFormatPass` reports an element written after one
  the figure prints later as COBOLNET2987, at the misplaced element; a repeat stays COBOLNET2423, whose message no
  longer grants an order licence to a fixed-sequence list. The grammar keeps the order-free superset (a named
  diagnostic, never a raw COBOL0307 at the second clause), and `ClauseCardinalityDriftTests` requires a sequence to rank
  exactly its list's once-only alternatives, so a clause added to such a list cannot arrive unranked. The OPTIONS
  paragraph's independent `[.]` is its own grammar alternative: `OPTIONS DOT (optionsClause+ DOT | DOT)?` is §11.9.2's
  bracket plus §11.9.3 SR1 (a period is required once a clause is written), so `OPTIONS. .` is accepted.
- **Its second error production — a KNOWN phrase in the WRONG position.** `misplacedSpecialNamesForPhrase`
  (kb/Work PB977) parses a SPECIAL-NAMES `FOR {ALPHANUMERIC | NATIONAL}` phrase written AFTER the definition of an
  ALPHABET, CLASS or SYMBOLIC CHARACTERS clause — a position §12.3.7.2 never prints and no dialect owns — so
  `ClosedFormatPass.VisitMisplacedSpecialNamesForPhrase` refuses it by name (COBOLNET2315) at every edition, one
  visitor for the three clauses. It replaced two opposite answers to one spelling: the ALPHABET postfix was an
  accepted "historical superset", the CLASS postfix a bare `COBOL0001: unexpected 'FOR'`. The binders read it only
  to recover the intended class (`DataBinder.ForPhraseIsNational`).
- **A closed UNION of formats — the file description entry (kb/Work PB1238).** `fileDescriptionClause` is the
  union of §13.4.5.2's three formats, and §13.4.5.3 SR5/SR7/SR8 bind each entry to exactly ONE of them. Which one
  is decided once, in `Compiler/Binding/FileDescriptionFormats.cs` (`Of`: a relative or indexed file is Format 2;
  otherwise a REPORT clause anywhere in the entry makes it Format 3; else Format 1), and the same file's table
  keys each alternative's context type to the formats that print it. `DataBinder.FileDescriptionClauseAdmitted`
  refuses a clause its entry's format does not contain (COBOLNET2604) and does not bind it; the same table
  carries the five FILE CONTROL ENTRY clauses the grammar superset-parses in the FD position (ORGANIZATION,
  ACCESS MODE, RECORD KEY, ALTERNATE RECORD KEY, FILE STATUS) with no admitting format and a home entry, so they
  are refused BY NAME with "write it in the SELECT entry" instead of a token error — they used to parse and be
  silently dropped. §13.4.5.3 SR4 (LINE SEQUENTIAL: no BLOCK CONTAINS, no RECORD CONTAINS; the VARYING arm stays
  legal) is asked in the same method (COBOLNET2605). `FileDescriptionFormatDriftTests` reflects over the generated
  parser so a new alternative cannot land without a row.

**The ordering a fixer owes, and why.** Audit the format from the RENDERED printed page first, model what the
grammar is missing, and only THEN close the list. Doing (3) before (1) and (2) rejects legal source that is
silently accepted today, which is strictly worse than the defect — the ALIGNED trap of PB487 in reverse. Each
site's rendered clause list, with its PDF page and printed folio, is recorded in a comment above its alternative
list in the `.g4`, and `tests/conformance/2023/pb829_closed_formats_legal.cob` writes a LEGAL clause in every
newly-closed format as the standing witness that the rendering was right.

**What is NOT this mechanism, and is deliberately still open.**

- **The COMMENT-ENTRY sinks are correct.** `dateCompiledContent`, `securityContent` (`~DOT+`) and
  `remarksContent` are also unbounded token runs, and a comment-entry is arbitrary text by definition — which is
  why ISO/IEC 1989:2023 defines no syntax for one anywhere.
  A sink over a COMMENT-ENTRY is right; a sink over a CLAUSE or PARAGRAPH LIST is the defect.
- **A `~DOT` sink is the same defect as a `genericClause` site** when it sits over a clause list. The computer
  paragraphs' `computerAttributes` was one — kept for the deleted '85 clauses (MEMORY SIZE, SEGMENT-LIMIT, WITH
  DEBUGGING MODE) and never gated, so `SOURCE-COMPUTER. IBM-370 WIBBLE WOBBLE.` compiled at every edition. It also
  REJECTED legal '85 source: running to the period, it left the '85 printed order `MEMORY SIZE … PROGRAM COLLATING
  SEQUENCE … SEGMENT-LIMIT …` nowhere to put SEGMENT-LIMIT. kb/Work PB830 deleted it: the three '85 clauses are
  modelled rules gated at their nodes, and the residue is the error production.
- **`SPECIAL-NAMES. WIBBLE WOBBLE.`** is NOT a §12.3.7.2 general-format violation and is not closed here.
  `implementorSwitchEntry` is `cobolWord ( IS? cobolWord switchStatusPhrases? | switchStatusPhrases )` — a
  continuation is REQUIRED (kb/Work PB716: a lone word reaches `unrecognizedClause`, COBOLNET1970) — and IS is
  un-underlined in the printed format, so a two-word entry is shape-legal as `device-name-1 [IS]
  mnemonic-name-3`. What refuses it is §12.3.7.3 SR8 ("The implementor shall specify the names that are available
  for switch-name-1, feature-name-1, and device-name-1") — a semantic rule over the implementor's name table,
  `Cobol.Net.Editions/ImplementorNames.cs`, refused at bind as COBOLNET2241 (kb/Work PB862).
- **The §11.2.1 header is BRACKETED, and the grammar now says so for every unit kind.** The OO units always had
  `(IDENTIFICATION DIVISION DOT)?`; the program and function units required the header, so `PROGRAM-ID. X.` as a
  unit's first line was COBOL0001 at every edition. `identificationDivision` now takes the header optionally,
  gated below 2002 as `identification-header-optional-2002` (X3.23-1985 required it). The text-level directive
  stages that must know where the first unit begins (COBOL-WORDS §7.3.10.3 SR1, LEAP-SECOND §7.3.17.3 SR1) read
  ONE test, `Preprocessor/CompilationUnitStart.IsAt` — the COBOL-WORDS copy had known only the header line.
- **The §12.4.5.1 ASSIGN TO phrase is a LIST, and the grammar now says so.** Every format prints `ASSIGN [TO]
  {device-name-1 | literal-1} …` with the ellipsis on the inner brace pair; `assignClause` took ONE
  `assignTarget`, so `ASSIGN TO "a" "b"` was COBOL0308. It is `assignTarget+` (superset parse), and WHICH lists are
  allowed is §12.4.5.2 SR5's implementor determination (`docs/CONFORMANCE.md` §7 DOC-A.1-71: one operand, or DISK /
  PRINTER then one operand), read at bind in ONE place, `Binding/AssignTargetRule`, and refused BY NAME
  (COBOLNET2256). A consequence for this mechanism: a word run after an ASSIGN operand is now further TO operands,
  not a residue clause, so it draws COBOLNET2256 rather than COBOLNET1970.
- **The §12.3.7.2 `dynamic-length-structure-clause` is modelled** (kb/Work PB829 finisher). It had no rule at all
  (`DYNAMIC LENGTH STRUCTURE DLS1 IS PREFIXED.` was COBOL0001 — DYNAMIC is a reserved token no alternative admitted,
  so the catch-all never saw it). `dynamicLengthStructureClause` writes the RENDERED format: STRUCTURE and IS
  optional (un-underlined), the PREFIXED / DELIMITED pair as `(…)+` with the at-most-once half of its choice
  indicators read by `ChoiceIndicators.AtMostOnce` at bind, and the context words STRUCTURE / SHORT / PREFIXED
  recognized by text through the parser base's `wordAhead` (the one `Word` funnel), each as the left-edge predicate
  of its own one-word rule. Gated below 2014 (`dynamic-length-structure-2014`); its semantics are
  `DESIGN-data-model.md` §2.1.
- **The FOR ALPHANUMERIC / FOR NATIONAL pair is read in ONE place** (kb/Work PB1075). Five formats print the same
  `{ IS x-1 [x-2] | { | FOR ALPHANUMERIC IS x-1 | FOR NATIONAL IS x-2 | } }` shape — the PROGRAM COLLATING SEQUENCE
  and CHARACTER CLASSIFICATION clauses (§12.3.6.2), the file-level COLLATING SEQUENCE clause (§12.4.5.7.2 Format 1),
  the SORT/MERGE COLLATING SEQUENCE phrase (§14.9.40.2 / §14.9.24.2) and the CODE-SET clause (§13.18.13.2). The
  grammar writes the FOR phrases as a `+` superset; `ChoiceIndicators.ForPhrasePair` reads the pair and refuses a
  repeated alternative (§5.2.6.4, COBOLNET2104) for all five, and `ChoiceIndicators.AlphabetPair` adds the IS form
  for the four whose operands are alphabet-names. `ForPhrasePairDriftTests` derives the repeated FOR-phrase rules
  from the grammar and fails on a binder that walks one itself. The file clause's Format 2 operand is a
  `dataReference` (a qualified data-name, SR6's subscript refused by name through `DataBinder.ClauseDataName`)
  resolved to the key ITEM among the file's RECORD KEY / ALTERNATE RECORD KEY items.

### 3.11 The required imperative-statement operand — the quantifier IS the rule (kb/Work PB396)

**The rule.** ISO §5.2.6.2 gives the omission licence to BRACKETED portions only ("Brackets, [ ], enclosing a
portion of a general format indicate that the syntax element contained within the brackets … may be explicitly
specified or that portion of the general format may be omitted"), and §5.2.6.3 requires one alternative of a
BRACE group to be explicitly specified. So an `imperative-statement-n` a general format prints outside brackets,
or stacks inside braces, is REQUIRED — and §14.9.19.3 SR1 states the same cardinality for IF a second way
("Statement-1 and statement-2 represent either one or more imperative statements or a conditional statement
optionally preceded by one or more imperative statements").

**Where it lives.** In the QUANTIFIER, at every such position. `statementBlock : statement+` is the right
cardinality, so a BARE `statementBlock` reference already spells the format's own "one or more"; the defect was
that every USE wrote `statementBlock*`, restoring the zero case. The nine positions — IF's THEN and ELSE arms
(§14.9.19.2), EVALUATE's WHEN clause (§14.9.13.2), PERFORM's inline body and its WHEN / WHEN OTHER / WHEN COMMON
/ FINALLY bodies (§14.9.28.2 Formats 2 and 3), SEARCH's and SEARCH ALL's WHEN bodies (§14.9.37.2) — now all read
`statementBlock`, each with the rendered page and printed folio recorded in the `.g4` comment above it. The
ON EXCEPTION / AT END / INVALID KEY / ON SIZE ERROR / ON OVERFLOW families already spelled it that way.

**The drift guard, and why it is over the grammar SOURCE.** `RequiredImperativeStatementDriftTests` asserts that
NO `.g4` under `src/Cobol.Net.Frontend/Grammar/` contains `statementBlock*` or `statementBlock?` (with `//`
comments stripped, because the prose quotes the forbidden spelling to explain it), plus a population assertion so
the guard cannot pass vacuously. That makes the NEXT general format automatic: a phrase rule written with the
zero-admitting quantifier fails the build, which a per-verb list could never do.

**WHEN OTHER is the same format read one level up.** `[ WHEN OTHER imperative-statement-2 ]` is ONE bracketed
phrase that FOLLOWS the `{ … } …` repetition, because §5.2.7 scopes an ellipsis to "the portion of the format
between the determined pair of delimiters" immediately to its left. It is therefore a trailing
`evaluateWhenOther?` on `evaluateStatement`, never a second alternative of the repeated clause — which is what
had made it admissible any number of times at any position, with `EvaluateBinder`'s `other = body` silently
discarding all but the last.

**The diagnostics (COBOLNET2072 / COBOLNET2073).** Because the grammar rules cannot match empty, the violation
arrives as a syntax error, and `CobolErrorStrategy` re-codes it in TWO arms — both structural, neither a table of
verbs. COBOLNET2072 fires when the expected set admits every token that can start a `statementBlock` (computed
once from the ATN) and the offending token starts none of them, naming the enclosing statement from the rule
invocation stack (`searchAllStatement` → "SEARCH ALL"). COBOLNET2073 fires on a WHEN OTHER phrase whose position
is wrong, separated from an EMPTY WHEN OTHER body by one token of lookahead past OTHER (and past PERFORM Format
3's un-underlined optional EXCEPTION). **Known message gap:** a wholly empty inline `PERFORM … END-PERFORM` with
no loop-control phrase fails on `performStatement`'s own alternatives decision, where `GetExpectedTokens()`
answers with a single token, so it is rejected with the generic parse error rather than COBOLNET2072; pinned by
`AWhollyEmptyInlinePerform_IsRejected` so the rejection cannot regress.

**⚠ DETERMINATION (owner-overturnable).** The two IF formats stay ONE grammar rule, so `END_IF?` also admits
`IF X = 1 NEXT SENTENCE END-IF` — a Format-2 body under a Format-1 terminator that no single printed format
shows. It is ACCEPTED, unchanged, and behaves per §14.9.19.4 GR4. That combination is a cross-format latitude
question rather than the cardinality slip §3.11 fixes, and refusing it would newly reject source this compiler
has always compiled; recorded here and in the `.g4` rather than inherited silently from the quantifier.

**The SEARCH formats (kb/Work PB446).** `searchAtEndClause` is `AT? END statementBlock` — the READ/RETURN
`NOT AT END` arm it used to carry leaked into BOTH SEARCH formats through the shared rule — and
`searchAllStatement` carries exactly ONE `searchAllWhenClause` (Format 2's ellipsis is on the `[ AND … ]`
bracket) and no KEY phrase (the home-grown `searchAllKeyPhrase` was read by no binder). The complement is a syntax
error that `CobolErrorStrategy.SearchFormatShapeMessage` re-codes as COBOLNET2269 from the parser state
(COBOLNET_CONTROL_FLOW_DESIGN D18). **NEXT SENTENCE stays a `statement` alternative** so that `statementBlock`
spells the printed `{ statement | NEXT SENTENCE }` braces; its POSITION is decided in the binder funnel
(`StatementBinder.IsNextSentenceArm`), because narrowing it in the grammar would re-parse the illegal
`IF A NEXT SENTENCE DISPLAY X.` with DISPLAY as the sentence's next statement — a silent change of meaning.

### 3.12 The lexer keeps every distinction the standard draws (kb/Work PB510, PB569)

**One token per reserved word.** ZERO, ZEROS and ZEROES — and SPACE/SPACES, HIGH-VALUE/HIGH-VALUES,
LOW-VALUE/LOW-VALUES, QUOTE/QUOTES — are distinct §8.9 reserved words. They are interchangeable ONLY where the
§8.3.3.6.2 figurative constant is written, and that interchangeability is written ONCE, as the parser rules
`zeroWord` / `spaceWord` / `highValueWord` / `lowValueWord` / `quoteWord` (`CobolExpressions.g4`) that
`figurativeConstant` references. A general format that prints ONE spelling as a keyword (§5.2.2) writes that token
and so admits no other: `BLANK WHEN? ZERO` (§13.18.8.2), the sign condition's `ZERO` (§8.8.4.7.2), OPTIONS
INITIALIZE's `BINARY ZEROES | HIGH_VALUES | LOW_VALUES | SPACES` (§11.9.10.2). The lexer used to fold each family
into one token, which made the distinction unrecoverable and let every such format accept every spelling. The ONE
multi-spelling keyword rule left is `PIC : 'PIC' | 'PICTURE'` — §13.18.40.3 SR5 makes PIC an abbreviation of
PICTURE everywhere — and `KeywordSpellingDriftTests.NoTwoReservedWords_ShareATokenType_ExceptPicAndPicture` derives
that from the spec tables and the lexer itself, so a re-fold fails the build naming the words. A misspelled keyword
is refused at the parse layer as COBOLNET2418 by `CobolErrorStrategy` arm 0d, which reads the families from the ATN
(the FIRST sets of the five rules): the expected set holds a sibling spelling but not the written one. The sign
condition is the one position where ANTLR's fallback (the alternative that finished the decision rule — the bare
operand) reports the failure at the preceding IS / NOT, or at the word with no ZERO in the decision-state
expected set; arm 0d recognizes that shape explicitly (procedure division only).

**§13.18.40.3 SR7 is decided from the characters PICMODE delimited.** `PIC_STRING` trims ONE trailing
separator at its right edge (a '.' + space is the separator period, a ','/';' + space a separator — §8.3.5 rules 2
and 3). After the trims the token ends in ',' or '.' in exactly two situations: the separator period follows it
(legal — NIST NC125A's `PIC 9,9,…,9,.`), or a separator comma/semicolon does (`PIC 999,, USAGE …`,
`PIC 999., VALUE …`) — the violation, for BOTH symbols. `PictureSeparatorPeriodRule` checks every `PIC_STRING`
token once, after the post-lex rewrites in `Frontend.LexAndParse`, and reports COBOLNET2419 through the
syntax-error listener; a separator period necessarily ends the entry, so "last clause" needs no second test. It
covers every PICTURE parent (data, report group and screen description entries) because it never looks at the
parent.

**§8.3.5's separator-context rules are decided the same way, by `SeparatorRule` (kb/Work PB1394).** The lexer skips
the separator space, so no grammar rule can see whether a separator was written — `N,M` and `N, M`, `"AB"N` and
`"AB" N` are the same token run. `SeparatorRule` runs beside `PictureSeparatorPeriodRule` in `Frontend.LexAndParse`,
over EVERY token of the unit (all channels), and judges each from the source characters beside it: rule 2 — a
COMMA / SUB_COMMA / SUB_SEMICOLON, or a lone ';' (the `SEMICOLON` token rides the HIDDEN channel so this rule sees it
and the parser does not), not immediately followed by a space → COBOLNET2631, except a numeric literal's
DECIMAL-POINT IS COMMA decimal point (a digit after, a digit / sign / '(' / space before), which the numeric-literal
rules own; rule 3 — a DOT not followed by a space → COBOLNET2632 (a period inside a numeric literal or PICTURE
string is part of that token); rule 5 — every literal token of either mode (`LiteralTokens.Types`, the one set
`LiteralScreenPass` also reads) whose opening delimiter is not preceded by a space, '(' or '==' or whose closing
delimiter is not followed by a space, ',', ';', '.', ')' or '==' → COBOLNET2633, touching literals reported once.
The operator clauses that state their own separator context are decided at the same site (kb/Work PB1430): §8.7.3's
concatenation operator '&' and §8.7.4's invocation operator '::' "shall be immediately preceded and followed by a
separator space" → COBOLNET2993, naming the side that touches; when that neighbour is a literal the operator owns the
boundary and the literal's COBOLNET2633 stands down, so one missing space is one report. §8.7.1's arithmetic operators
(with the unary-operator exception) are the next case here (kb/Work PB2017). "Separator space" is the lexer's own `WS` set plus the start and end of the text. The parse-recovery hints that
guessed at rule 5 on a parse error (COBOL0301/0302) are gone: the rule has one home. It applies to the main
compilation-group lex; the D2 keyword-omitted re-parse needs no second screen because every one of its tokens was
already a SUBSCRIPT-mode token of the main lex.

**A prefixed literal is delimited whatever its content (kb/Work PB1441).** `X"`, `N"`, `B"`, `NX"` and `BX"` are
opening delimiters (§8.3.5 5)), so every prefixed body fragment is `<prefix> STR_BODY`; the §8.3.3 content
repertoires (hexadecimal digits for X / NX / BX, '0'/'1' for B) are `LiteralScreenPass`'s COBOLNET2630, never the
lexer's — a body that spelled its repertoire fell back to IDENTIFIER + Format 1 literal on the first bad character,
which beside a data item of the prefix's name compiled as two operands. The literal rules have one ordered entry,
`CobolLiteral.SyntaxViolation` (repertoire, then grouping, then length), asked of the unit's tree by
`LiteralScreenPass` and of a compiler-directive operand — lexed apart from the unit — by
`CompileTimeExpressionEvaluator`. A lexer action that ends a token early does
so through `CutTokenTo(length)`, which restores the column with the input position (kb/Work PB1728).

### 3.13 The characters of a COBOL word, and the ONE fold that compares words (kb/Work PB1402)

ISO §8.3.2.1 makes a COBOL word basic letters, basic digits, hyphen, underscore and **extended letters**; §8.1.3.2 GR4
makes the extended letters Annex B less the basic characters, and GR3 b) / GR4 b) compare words by folding uppercase to
lowercase with Annex C. Three pieces, each in one place:

- **The lexical class** — `CobolCharacterRepertoire.IsWordCharacter` (Editions), mirrored by the lexer's `EXT_CHAR`
  fragment in `NAME_BODY` / `NAME_TAIL`: the basic word characters plus EVERY character outside the basic repertoire
  except the noncharacters the text stages use as markers. It is a SUPERSET of Annex B on purpose: the lexer reads
  UTF-16 code units (a supplementary letter is two surrogates, both in the class), it has no edition, and a character
  outside Annex B must be NAMED rather than end the word with "unexpected". Every text-stage word scanner (the
  directive line, `>>DEFINE` / `>>COBOL-WORDS` word shapes, the fixed-form word-continuation test) asks the same
  predicate, so all stages agree where a word ends.
- **The legality** — `CobolCharacterRepertoire.Violation(word, edition)` over the edition's Annex B.3 position classes
  (anywhere / not first / medial), asked by `CobolWordRule` from the tree funnel (`VisitCobolWord`) and the directive
  stages: COBOLNET2773 for a character outside the edition's Annex B or a B.3 item 2 character first; the construct
  `user-word-character-removed-2023` (COBOLNET0902) for a character only COBOL 2023 refuses (E.2 item 4); the construct
  `user-word-extended-letter-2002` (COBOLNET0900) below 2002. The tables are COBOL 2023's, transcribed list for list
  from the standard by `scripts/spec/gen_annex_bc.py`; COBOL 2002/2014 are derived by reversing Annex E (E.2 item 4,
  E.3.3 items 4 and 5 — the standard records no 2002→2014 change, so 2002 uses 2014's). `AnnexBCDriftTests` re-reads
  every list from `specs/ISO_COBOL.md`. A word's length is counted in characters (code points), so a combining
  character counts separately (GR4 c).
- **The fold** — `CobolNames` (Runtime, because the run-time externalized-name match needs it too): `Fold`, the
  `Comparer` every name table uses, and `Same` / `StartsWith` / `EndsWith` / `Contains` over spans. No compiler or
  frontend file compares a word by `OrdinalIgnoreCase` (`CobolNameFoldDriftTests`; a string that is not a COBOL word
  says so on its line). The table is COBOL 2023's; the only fold difference a COBOL 2002/2014 word can observe is E.2
  item 14 (U+0131 → i, U+03C2 → U+03C3 before 2023), so the lexer writes those letters in their edition's fold
  (`CobolNames.EditionSpelling`, primed through `TokenRetypes.PrimeLexer`) and the one comparer then answers for the
  edition. A C# identifier derived from a word keeps its extended letters, escaping only those C# cannot hold
  (`DataItem.IdentifierCharacters`). The EXTERNAL-file group, the SR14/SR15 WHEN census and the REPOSITORY
  specification table are keyed by the same comparer. Not yet on it: the Editions word tables (`CobolWordsMap`, the
  directive catalog), because `Cobol.Net.Editions` references nothing and the fold lives in Runtime, and the C#
  identifier derivations `Sanitize(x).ToUpperInvariant()`.
- **The encoding of the grammar itself** (kb/Work PB1944) — ANTLR reads a `.g4` in the JVM's default charset (UTF-8 on
  Java 18+ and on Linux, Cp1252 on Windows CI's JVM), so `Invoke-Antlr4CSharp.ps1` passes `-encoding UTF-8` and every
  grammar file is valid UTF-8 without a byte-order mark (`GrammarEncodingDriftTests`). The encoding is declared, so
  no grammar has to avoid non-ASCII text to dodge the JVM default. A character that does not display (a control
  character or a noncharacter such as `EXT_CHAR`'s U+FDD0 to U+FDEF, U+FFFE and U+FFFF) is written as a Unicode escape
  so a reader can see it; a displayable character may be written raw (owner 2026-10-04). A raw noncharacter in a set read as three Latin
  characters under Cp1252 and failed Windows CI's warnings-as-errors build with `warning(180)` when nothing declared
  the encoding.

---

### 3.14 Procedure-division general formats the superset grammar leaves to a named refusal (kb/Work PB351, PB1146)

**The posture.** The grammar parses the UNION of what the procedure division's formats print, so a refusal can NAME
the rule (the §3.11 posture, and `ExpressionFormationPass`'s for the suffix order). The screens live in ONE pre-bind
parse-tree pass, `Compiler/Validation/ProcedureFormatPass.cs`, run right after `IntegerOperandPass` from
`BinderDriver.Bind`, at every edition (each rule is the same in 1985, 2002, 2014 and 2023).

**An imperative-statement operand holds no conditional statement (COBOLNET2796; §14.5.1, Table 12).** Every
conditional phrase spells its operand `statementBlock`, which is `statement+`, so `READ F AT END IF X = 1 … NOT AT
END …` compiled. §14.5.1: a conditional statement is "any statement with a conditional phrase that is not terminated
by its explicit scope terminator", and an imperative statement may be one only "delimited by its explicit scope
terminator". The classifier, `Binding/ConditionalStatements.IsConditional`, reads both halves off the parse — a
conditional phrase is written when the statement's OWN syntax holds a `statementBlock`, and the terminator is the
token `END_` + the statement's Table 12 name (`Table12StatementNames`) — so a new statement rule is classified with
no edit. The pass screens every `statementBlock` whose parent is not `ifStatement`: IF's statement-1/statement-2 is
the one operand that may END in a conditional statement (§14.9.19.3 SR1), so the strict reading is the default and
a NEW phrase rule is screened automatically. `Table12StatementNameDriftTests` re-derives the terminator column from
the scraped Table 12 and proves the proxy sound (only a row with a conditional phrase, or PERFORM with its REQUIRED
END-PERFORM, reaches a `statementBlock`).

**Format 1 (with sections) versus Format 2 (without sections) (COBOLNET2797; §14.2.1, §14.4.1).** The rule
`declarativePart* sentence* procedureUnit*` is the union of the two formats. Rendered from PDF page 557: Format 1
prints ONE bracketed DECLARATIVES portion and nothing between the header (or END DECLARATIVES) and the first section
header; §14.4.1 adds "If one paragraph is in a section, all paragraphs shall be in sections". So once a division has
a section or a DECLARATIVES portion, a leading sentence, a paragraph outside every section, or a second DECLARATIVES
portion is refused — one diagnostic per violation kind, at its first occurrence.

### 3.15 One rule per printed operand shape (kb/Work PB2114)

A general format names its operands by metalanguage (`identifier-n`, `literal-n`, `integer-n`), and the grammar
writes each SHAPE of operand once, as a rule every position with that shape names. A copy written inline at a second
position accepts the same input, so nothing but the `.g4` shows it, and each copy grows its own binder dispatch over
its own generated accessors.

| Rule | Shape | Positions | Bound by |
|---|---|---|---|
| `sendingOperand` | `literal \| functionCall \| inlineMethodInvocation \| dataReference`: a SENDING `{identifier-n \| literal-n}` (§8.4.3.1.2 Formats 1 and 4 are identifiers) | the operands of ADD, SUBTRACT, MULTIPLY, DIVIDE ahead of TO / FROM / BY / INTO, DIVIDE's BY operand, MOVE, the WRITE / REWRITE / RELEASE FROM phrases, INITIALIZE REPLACING, every INSPECT operand after identifier-1 | `MoveBinder.SendingOperand` (INITIALIZE first admits NULL on the literal slot, §8.4.3.10.3 SR1; INSPECT applies its own §14.9.22.3 SR3 screens); the arithmetic verbs bind it through the expression spine |
| `receiversOrSendingOperand` | Format 1's receivers `{identifier [rounded-phrase]} …` or Format 2's one sending operand | ADD TO, SUBTRACT FROM, DIVIDE INTO | `ArithmeticBinder.Format1Receivers` / `Format2SendingOperand`, through `ArithmeticOperandRole` |
| `arithmeticGivingPhrase` | `GIVING {identifier [rounded-phrase]} …` | ADD, SUBTRACT, MULTIPLY, DIVIDE | `ArithmeticBinder` |

MULTIPLY's BY operand prints the same two shapes as `receiversOrSendingOperand` but keeps its own rule,
`multiplyByOperand+`, which admits a repeated sending operand and a ROUNDED literal; the binder names those
(COBOLNET1689) where the shared rule's three users take a parse error, so folding MULTIPLY in would change what
parses. A position whose printed shape differs is not a copy: an identifier only (INSPECT's identifier-1, GO TO
DEPENDING, STRING / UNSTRING's sender), an identifier or an integer (PERFORM TIMES, WRITE ADVANCING), and the
argument forms of CALL and INVOKE. DISPLAY's `{identifier-1 | literal-1} …` IS this shape but still writes the set
inline: naming the rule parses every legal DISPLAY identically and moves the error-recovery boundary, so `DISPLAY
COLUMN.` (a data item named by a reserved word) counts more syntax errors on its way to the §8.9 funnel's named
diagnostic. It is the drift test's one recorded exemption until that recovery is settled.

`SendingOperandDriftTests` fails when any rule other than `sendingOperand` offers its four shapes as sibling
alternatives, and `ArithmeticOperandRoleDriftTests` pins the two mixed-role rules. A unification is proven
behavior-neutral by a token-stream and parse-tree-shape differential over every lexer input the three test
assemblies produce, with the new wrapper rules flattened (DESIGN-architecture-review §4 item 3).

## 4. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| delete | `Grammar/CobolDialect.g4` | — | dead: unreferenced by any C#/csproj/script (§1.1) |
| delete | `Grammar/CobolParserGenerics.g4` | — | dead: unreferenced (§1.1) |
| delete | `Grammar/CobolParserJsonXml.g4` | — | dead + non-ISO (§1.1) |
| delete | `Grammar/CobolParserOO.g4` | — | dead: stale duplicate of live `Core/CobolOO.g4` (§1.1) |
| delete | `Grammar/CobolPreprocessor.g4` | — | dead: stale duplicate of hand-written `Preprocessor/` (§1.1) |
| delete | `Grammar/.antlr/`, `Grammar/Core/.antlr/` | — | committed IDE build-output caches; one caches a dead grammar (§1.8) |
| split | `Core/CobolExtensionsJsonXml.g4` | `inlineMethodInvocationStatement` → `Core/CobolOO.g4`; rest deleted | separate the one live 2023-OO rule from the non-ISO JSON/XML; delete non-ISO (§3.4) |
| delete | `jsonStatement`/`xmlStatement` + arms in `CobolParserCore.g4:716-717` | — | non-ISO, 0 spec hits (hard invariant 6) |
| create | — | `tests/version-matrix/cobol-words.json` | single declarative source for context-sensitive words (§3.3a) |
| create | — | `scripts/gen-cobol-words.ps1` | generate lexer HashSet + `cobolWord` + drift-check vs reserved-words.json (§3.3a) |
| create | — | `Grammar/Core/CobolWords.g4` (parser fragment), `Parsing/CobolLexerWordSet.g.cs` (lexer partial class) | generated word-set outputs (§3.3a) |
| refactor | `Core/CobolLexer.g4:30-72` `_dataNameTokens` | consume generated word-set | delete the hand-synced HashSet (§3.3a) |
| refactor | `CobolParserCore.g4:26-73` `cobolWord` | import generated alternative list | delete the hand-synced rule (§3.3a) |
| refactor | `Core/CobolLexer.g4:726-776` SUBSCRIPT `SUB_*` | shared `fragment` bodies with DEFAULT tokens | one tokenization rule per literal shape (§3.3b) |
| delete | `Parsing/EditionGateHints.cs` (207 loc) | — | no recogniser exists: identity is carried by the committed-match annotation; gating in the `VersionConformancePass` (§3.2) |
| refactor | `Parsing/CobolParserCoreBase.cs` | add the committed-match `Construct(ConstructId)` action + `ConstructIds` side table; keep only the two forward-detects (`retryPhraseAhead()`, `boolExprAhead()`) | constructs name themselves on match; delete signature table + double-sourced dialect (§3.2, §1.5) |
| refactor | grammar `{isXXXX()}?` edition predicates | dropped — superset parse + `{Construct(ConstructId.X);}` annotation actions | every construct parses at every `--std`; the pass gates (§3.2) |
| refactor | `Parsing/CobolErrorStrategy.cs:113` | drop the recogniser call; emit structured `Diagnostic`; host the token-keyed vendor JSON/XML→COBOL0313 hint | edition diagnosis moves to the `VersionConformancePass`; keep structure (§3.2, §3.4, §3.8) |
| create | — | `Cobol.Net.Editions` assembly | shared lowest layer both Frontend + Compiler reference (§2 D4) — owned by DESIGN-edition-framework.md |
| move | `EditionGateHints` metadata / preprocessor severity copies | `Cobol.Net.Editions` (`ConstructRegistry`, `EditionSeverityPolicy`) | one edition-metadata + one severity source (§1.5, §3.6) |
| move | `Diagnostics/DiagnosticBag.cs` ns → `CobolNet.Frontend.Diagnostics` | become the ONE pipeline diagnostic bag | one diagnostic model (§3.8) |
| create | — | `Cst/` façade namespace (`DataDescriptionCst`, `StatementCst`, …) | typed narrow contract; retire ~336 `GetText()` walks (§3.7) |
| edit | `Frontend.cs:135` `catch (Exception)` | `catch (ParseCanceledException)` + `RecognitionException` | stop masking predicate/lexer-action bugs (§3.5) |
| edit | `Frontend.cs:16` stale banner; `:103-105` throw | correct banner; record internal diagnostic | doc drift + exception-boundary consistency (§1.7, §3.6) |
| edit | `Invoke-Antlr4CSharp.ps1:29`, csproj | `-PackageName $(AntlrPackage)` | package name in one MSBuild property (§3.5) |
| edit | `.gitignore` | add `**/.antlr/` | prevent re-committing IDE caches (§1.8) |

---

## 5. Migration plan (keep the battery green at every step)

Each numbered step is an independently-committable, individually-green change. Order chosen so no step
depends on a later one and the highest-risk items (word-set generation, predicate-drop migration) come after the safe
deletions build confidence.

**M1 — dead-grammar + cache deletion (zero-risk).** Delete the 5 unreferenced `.g4` files and the `.antlr/`
caches; add `**/.antlr/` to `.gitignore`. Nothing generates or imports them → build + full battery unchanged.
Verify: `dotnet build` regenerates identically; the full battery (conformance + legacy guard) green.

**M2 — JSON/XML quarantine.** Move `inlineMethodInvocationStatement` into `Core/CobolOO.g4`; delete the
JSON/XML rules, the two `statement` arms, `jsonXmlExceptionPhrases`, and the empty fragment; remove the
`EditionGateHints` JSON/XML arm (its COBOL0313 disposition relocates to `CobolErrorStrategy` as a
token-keyed vendor hint, §3.4); drop the `json-generate-2014` version-matrix row. Verify: inline-invoke
golden still parses; no conformance test asserted a JSON/XML parse success (grep the test corpus first); full
guard green. This is the hard-invariant-6 close.

**M3 — build hygiene.** Package name → MSBuild property (performs the `Generated` namespace rename); narrow
the `Frontend.cs` catch; fix the stale banner + the line-count `throw`→diagnostic. The namespace rename
touches every `using Core =` consumer — a mechanical, compiler-verified sweep. Verify: clean build on
Windows AND WSL/Linux (portability is the known risk area); battery green.

**M4 — namespace cleanup (preprocessor + parsing) — DONE (PHASE 01).** The front-end files declare `CobolNet.Frontend.*`.

**M5 — single-source word set.** Create `cobol-words.json` from the current `_dataNameTokens` + `cobolWord`
contents (mechanical extraction), write `gen-cobol-words.ps1` + the `CobolWordsDriftTests`, wire the
generated fragments into the lexer/parser, delete the hand-maintained HashSet + rule. The drift test and the
full parse of the conformance corpus prove byte-identical tokenization. Highest-value dedup; medium risk
(tokenization changes are wide) — land it alone, guard-fast + full guard.

> The word set is single-sourced per §3.3a: `tests/version-matrix/cobol-words.json` → `scripts/gen-cobol-words.ps1`
> → the `Grammar/Core/CobolWords.g4` parser fragment (the imported `cobolWord`) + the committed
> `Parsing/CobolLexerWordSet.g.cs` lexer partial (the subscript-trigger set), cross-checked by RW-1 and guarded by
> `CobolWordsDriftTests`. `.tokens` output is byte-identical to the retired hand-synced sources.

**M6 — `Cobol.Net.Editions` extraction.** (Coordinated with DESIGN-edition-framework.md — that doc owns it; the
frontend's part is to consume `EditionInfo`/`ConstructRegistry`/`EditionSeverityPolicy` and delete the
preprocessor metadata/severity copies.) Verify: version-matrix accept/reject unchanged across all four
`--std` values.

**M7 — superset migration: drop the edition predicates.** After M6 (so `ConstructId`/`EditionInfo` exist):
migrate the version-gated constructs in small batches (each batch = one commit, one regen, one guard-fast —
the `feedback_grammar_version_factoring` discipline): drop the `{isXXXX()}?` predicate, add the
committed-match construct-id annotation, and gate at a bind-side `ConstructRegistry.Check` site until the
`VersionConformancePass` skeleton funnels all `Check` sites (the residue-first sequencing, batch record, and
pass skeleton are owned by `DESIGN-version-conformance-pipeline.md` §5 Stages 1–3). The two load-bearing
forward-detects (`retryPhraseAhead()`, the `boolExprAhead()` ENTRY) are the only predicates that remain.
When the last batch lands, delete the reverse-signature recogniser (`ReservedWordEditionHints`); the vendor
JSON/XML COBOL0313 disposition relocates to `CobolErrorStrategy` as a token-keyed hint. Verify per batch:
the version-matrix rows for those constructs still emit COBOLNET0900 with the same wording; the
duplicate-diagnostic case the dossier reproduced is now single. This is the biggest correctness win and the
most steps — do it incrementally, never as one batch.

**M8 — `Cst/` façade.** Introduce the façade types alongside the binder rearchitecture (this step is paced
by the binder work, not the frontend). Migrate binder `GetText()` sites to façade accessors incrementally;
when a rule's consumers are all on the façade, that rule's context can be considered migrated. End state:
generated contexts `internal`, `Cst/` the only cross-assembly surface. Verify continuously: the binder's
output bound tree is unchanged (differential oracle + conformance).

**Rollback posture:** M1–M4 are trivially revertible. M5/M7 are the risk points — each is gated behind its
own drift/matrix test and the full legacy guard, and each is landed as the *only* change in its commit so a
regression bisects to one step.

---

## 6. Risks

- **R1 (M5, high-blast):** the generated word-set could tokenize differently from the hand-maintained
  HashSet if the extraction misses a word (e.g. a word present in `cobolWord` but absent from
  `_dataNameTokens` today, which would be a *latent existing* asymmetry the generation would expose).
  Mitigation: the extraction script diffs the two current sources and fails on asymmetry, forcing the owner
  to resolve it as a real spec question before generation; the drift test + full-corpus parse are the net.
- **R2 (M7, correctness):** dropping a predicate changes the parse space, and the two surviving
  forward-detects are the delicate sites — `boolExprAhead()` shares the comparison DFA, and
  `retryPhraseAhead()` must resolve the OPEN name-list collision exactly per §3.2 (a missed real gate must
  degrade to a neutral parse error, never a wrong edition claim). Mitigation: one batch per commit with the
  FULL legacy guard; the version-matrix negative fixtures (every gated construct probed below its edition)
  are the regression net; annotation actions run only on committed matches, so speculative parses cannot
  mis-name a construct.
- **R3 (M2): retired.** The legacy compiler that also parsed via this front-end is deleted (P15 Cut 2), so no legacy test can depend on JSON/XML parsing.
- **R4 (M3, portability):** the package-name property + regen must work identically on Windows and Linux
  (the flat-output hazard). Mitigation: the existing portable-regen logic is untouched; only the
  `-package` argument value changes; verify on WSL per `reference_wsl_linux_repro.md`.
- **R5 (M8, scope creep):** the `Cst/` façade could balloon into a second data model. Mitigation: façade
  types are strictly 1:1 with grammar rules and hold the context (no computed/semantic state) — enforced by
  code review; semantic facts belong to the binder's model, not the façade.
- **R6 (cross-doc coupling):** M6/M7 depend on `Cobol.Net.Editions` (DESIGN-edition-framework.md). If that assembly
  slips, M7 blocks. Mitigation: M1–M5 + M8 are independent of it and deliver most of the frontend cleanup;
  sequence M6/M7 after the editions assembly lands.

---

## 7. What explicitly does NOT change (and why)

- **The single SUPERSET-grammar model** (one grammar, no per-edition forks) — correct and cheapest for
  four-editions-in-one; edition legality is the `VersionConformancePass`'s job, and we remove duplication
  *around* the grammar, not the one-grammar model itself.
- **The two-stage SLL→LL parse** — proven, subtle, and not a smell; untouched.
- **`ZeroTokenRewriter`'s algorithm** — untouched, but ⛔ **its INPUT vocabulary changed and the line that stood
  here ("proven … untouched") was hiding a defect** (fix-queue PB48). The pass reads adjacency to `(` / `)` as
  proof that a figurative ZERO sits inside parenthesized arithmetic. An argument list is delimited by those same
  characters, so every bare `ZERO` argument was converted to an arithmetic zero before any function was known and
  `FUNCTION LOWER-CASE(ZERO)` was rejected as class numeric — legal source, refused. ISO §8.4.3.2.3 SR6 makes the
  argument-list paren categorically not a grouping paren, so the LEXER now types it `FNARG_LPAREN`/`FNARG_RPAREN`
  from the `_fnParenStack` it already maintains, and the rewriter's rule became true as written. The pass also
  gained the reference-modification `COLON` as arithmetic context (§8.4.3.3.3 SR4 — both positions are arithmetic
  expressions), because a ref-mod written directly after a function name is delimited by FNARG parens too.
  ⚠ `refModPart` accepts BOTH paren flavours: SR6's precondition is "if a function's definition **permits
  arguments**", a catalog question no lexer can answer, so after a zero-argument name that token is the ref-mod.
  ⚠ The FUNCTION of `ADDRESS [OF] FUNCTION word` (§8.4.3.12.2) heads no function-identifier — its operand is a
  prototype name or identifier-1 — so the lexer does NOT retype the `(` after that word: it is identifier-1's
  subscript or reference modifier (`_functionIsAddressOperand`, kb/Work PB1416).
  ⚠ **The rewriter is still the standing answer to "where does figurative ZERO become arithmetic", and it is
  still incomplete** — a bare `COMPUTE X = ZERO` and a `TB(ZERO + 1)` subscript are the positions no adjacency
  arm reaches (`kb/Work/PB51.md`, `kb/Work/PB50.md`). Retiring the pass in favour of a `primaryExpression`
  alternative is the structural candidate recorded there, with the ambiguity survey it requires.
- **The five preprocessor stages and their order** — correct per the TURN-anchoring hazard analysis; only
  namespaces + the injected severity policy change.
- **SUBSCRIPT lexer mode existence** — the `x(i)`/`(a+b)` disambiguation genuinely needs it; we dedup its
  token bodies, not its strategy.
- **`CobolErrorStrategy`'s 19 COBOL-intent heuristics** — useful, non-duplicative; only the recogniser
  call is removed (edition diagnosis lives in the `VersionConformancePass`; the token-keyed vendor
  JSON/XML hint is hosted here).

---

## 8. Open questions for the owner

1. **JSON/XML final disposition (M2).** Confirm hard-delete of the JSON/XML grammar + the `json-generate-2014`
   version-matrix row now (this design's assumption per hard-invariant 6),
   versus keeping the dead rules quarantined behind a permanent `--enable-vendor-json` flag for a possible
   future vendor-dialect mode. Recommendation: hard-delete; re-add as a real subsystem if ever scoped.
2. **`Cobol.Net.Editions` assembly boundary (M6).** This design assumes a new lowest assembly both Frontend
   and Compiler reference. If the owner prefers to keep the registry in `Cobol.Net.Compiler` and instead
   *inject* the metadata into the frontend at parse time (avoiding a new assembly), M7's construct-id
   annotation still works — `ConstructId` is the only shared type the grammar needs; introduction editions
   are read solely by the `VersionConformancePass`. Which dependency shape?
   (DESIGN-edition-framework.md needs the same answer.)
3. **`Cst/` façade timing (M8).** The façade is most valuable *with* the binder rearchitecture and risky to
   land before it. Should the frontend ship M1–M7 first and defer M8 to the binder track, or co-develop the
   façade as the binder's first refactor? Recommendation: defer M8 to the binder track; it is enabling
   infrastructure for that work, not standalone frontend cleanup.
4. **Generated-namespace rename timing (M3 vs G8) — RESOLVED.** The `Generated` namespace rename was brought forward
   (PHASE 01); it renames only the front-end's generated code and was independent of the legacy assembly, which is now deleted.
5. **SUBSCRIPT-mode elimination — D10 = FULLY REMOVE (§9 below is the design).** The owner ruled the SUBSCRIPT
   mode + the binder subscript re-parse are removed rather than deferred; the token-body dedup (§3.3b) is in the
   tree, and the removal itself (**§9**) is scheduled for PHASE 15 §"CUT 2.5" (it needed the legacy compiler, which
   shared the `SUB_*` grammar, deleted first — §9.3; done at Cut 2). It still needs ONE owner decision — the space-separator
   question (§9.4) — resolved before the §9.5 stages run.

---

## 9. D10 — SUBSCRIPT-mode removal (owner override): design + the one open decision

> **Status: FIRST HALF LANDED (kb/Work PB2113, PHASE 15 §"CUT 2.5").** §9.4 is resolved (Option A, from the spec), the
> SUBSCRIPT lexer mode and every `SUB_*` token are deleted, subscripts / reference modifiers / keyword-omitted argument
> lists are parse nodes, and the binder's hand-rolled splitter and captured-group readers are gone (§9.5 records what each
> stage became). The SECOND half remains (kb/Work PB2151): subscript and reference-modifier positions still reach the code generator as
> the D10 TRANSITIONAL string carrier (`ReferenceResolver.RenderSegment`, and the D18 materializer's text re-parse
> through `subscriptExpressionFragment`); moving them onto `BoundExpr` deletes both. §9.3's entanglement cleared when
> PB2110 deleted the legacy tree.

### 9.1 Goal
Replace the lexer **SUBSCRIPT mode** (`CobolLexer.g4` — entered via `LPAREN` after a data-name token, emits the
`SUB_*` token family, popped by `SUB_RPAREN`) and the **flat uninterpreted stream** it feeds
(`subscriptOrRefMod : subToken*`, `CobolParserCore.g4`) — which the binder RE-PARSES by hand
(`ReferenceResolver.InterpretSubscripts` / `HasDepth0Colon` / `SplitSubscriptTokens` / `RenderSegment`, and the
~250-line recursive-descent arithmetic parser over `SUB_*` in `StatementBinder.Intrinsics.cs`) — with **real
grammar parse nodes**: a subscript list, a ref-mod, and a FUNCTION argument list parsed as `arithmeticExpression`
/ the existing `argumentList`, so the binder walks bound nodes instead of re-lexing a token soup. This kills a
whole hand-rolled parser (the biggest single simplification left in the frontend) and unifies the two arg-capture
paths (`subscriptPart` vs `inlineMethodInvocationStatement`'s `argumentList`) into one (singular-pattern).

### 9.2 Why the mode exists — the two hard constraints
The mode is not gratuitous; it solves two problems a naïve DEFAULT-mode rule reintroduces:
- **The LPAREN ambiguity.** `(` after a data-name is a subscript / ref-mod / function-argument opener; `(`
  elsewhere is arithmetic grouping. The mode disambiguates by the *preceding* token
  (`PreviousTokenCouldBeDataName` over the generated `_dataNameTokens` set). A grammar-level rule must recover this
  another way (the same ambiguity the `{is2023()}? inlineMethodInvocationStatement` predicate already fights).
- **⚠ Separator loss (the blocker).** DEFAULT mode `-> skip`s `WS`, and outside a function-argument region
  `FNARG_SEPARATOR`'s action skips the comma/semicolon-space separator. But ISO/IEC 1989:2023 §8.3.5
  admits a **space** as a subscript / argument separator: `X (I J)` and `MAX (A B)` are legal, alongside the comma
  forms `X (I, J)`. The SUBSCRIPT mode keeps `SUB_WS` a real token so `SplitSubscriptTokens` can split
  space-separated operands. Once WS is skipped, `X(I J)` (two subscripts) and `X(I J)` is indistinguishable from a
  malformed single expression, and — worse — there is no token boundary to split on. A pure comma-only grammar
  rule cannot parse the space-separated forms.
- **⚠ Sign adjacency.** `+1` / `-15.6` (a signed literal — `SIGNED_INTEGERLIT` / `SIGNED_DECIMALLIT`, sign
  ADJACENT to the digits) vs `+ 1` / `- 1` (a *relative* subscript offset — `SUB_PLUS SUB_WS SUB_INTEGERLIT`) are
  today distinguished LEXICALLY by the presence of `SUB_WS`. With WS skipped, `I+1`, `I + 1`, and `+1` all lex
  identically, so the relative-offset-vs-signed-literal distinction and the `-15.6` fraction-drop hazard
  (`CobolLexer.g4` SIGNED_DECIMALLIT must precede SIGNED_INTEGERLIT) can no longer be re-derived from tokens alone.

### 9.3 Entanglement with the legacy compiler — CLEARED
The old **structured** subscript rules `subscriptList / subscriptEntry / subscriptQualification / relativeOffset`
(`CobolParserCore.g4`) were dead in the grammar (rooted at the unreferenced `subscriptList`), but the generated
`CobolParserCore.SubscriptEntryContext` type was still consumed by the legacy compiler's `ExpressionBinder.BindSubscriptEntry`,
so deleting the dead rules broke the legacy build (`CS0426`) and the `SUB_*` tokens were shared with the legacy path. P15 Cut 2
deleted that compiler (kb/Work PB2110), so the SUBSCRIPT machinery no longer has a second consumer and D10 is no longer
sequenced behind a deletion. The removal itself is kb/Work PB2113.

### 9.4 THE DECISION — WiseOwl COBOL preserves ISO §8.3.5 space-separated lists (RESOLVED from the spec, kb/Work PB2113)
**Resolution: Option A, and the spec leaves no latitude.** §8.3.5 1): "The COBOL character space is a separator", and
2): "The COBOL characters comma and semicolon, immediately followed by a space, are separators that may be used anywhere
the separator space is used" (`cite.py --check 8.3.5` OK, both) — so `X (I J)` is two subscripts exactly as `X (I, J)`
is, and Option B would reject legal source. §8.3.3.3.2 2) ("If a sign is used, it shall appear as the leftmost
character of the literal") and §8.7.1 (the operators "shall be preceded by a space and followed by a space") make
`X (I -1)` two subscripts and `X (I - 1)` one.
**The mechanism, as built.** No lexer MODE: the lexer's paren stack (`_parenRegions`, the mechanism the FUNCTION
argument list already had) records which open parens are LIST REGIONS — an argument list (`FNARG_LPAREN`) or a
reference's parenthesized tail (`REF_LPAREN`, typed by the same `PreviousTokenCouldBeDataName` trigger that used to push
the mode). Inside a list region the §8.3.5 2) separator survives as `FNARG_SEPARATOR` and a sign that follows a separator
and touches its digits is retyped `SIGNED_INTEGERLIT` / `SIGNED_DECIMALLIT` (actions, never predicates — PB1715);
everything else is the DEFAULT vocabulary, so the grammar parses the list by juxtaposition, the shape `functionArgList`
always had. That is the "minimal WS / sign-adjacency mechanism" Option A predicted, and nothing more.
⚠ A sign-adjacent literal is retyped in ANY list region, including a group nested in one: `X ((I -1))` is now refused
(two juxtaposed operands inside a parenthesized expression, as `MAX((A -1))` always was) where the captured stream read
it as `I - 1`. The spec agrees with the refusal; no corpus program writes the shape.
The options as they were weighed:
- **Option A — spec-faithful (recommended): keep space-separated subscript/argument lists.** Then a **scoped
  WS-significance mechanism is unavoidable** (an island-grammar region, or a `WS`-non-skipping lexer predicate
  active only inside a data-name's `(...)` tail). "Full removal of the mode" then *reduces* to: **replace the flat
  uninterpreted `SUB_*` stream + the hand-rolled C# re-parsers with INTERPRETED grammar rules**, while retaining a
  minimal WS/`sign`-adjacency mechanism. This achieves the owner's REAL goal (real parse nodes, delete the ~250-line
  re-parser, unify arg capture) — it does not achieve a literal "zero lexer mechanism," because the spec forbids it.
- **Option B — narrow the language: require commas.** The mode can be fully removed, but `X(I J)` / `MAX(A B)`
  become parse errors — a **spec violation** the NIST corpus + INV-1-strong would flag. Not recommended.
- **Option C — interpret in-mode.** Keep the mode but give it real grammar rules (parse `SUB_*` structurally
  instead of the flat `subToken*`), deleting only the C# re-parsers. Smaller, but leaves the mode.

**Taken: Option A** — it honors the spec (hard-invariant 3) and still deletes the hand-rolled parsers, which is the
substance of the owner's directive: "fully remove" is "remove the uninterpreted flat stream + C# re-parse," retaining
the smallest lexer assist the spec compels.

### 9.5 Staged plan — executed as PHASE 15 §"CUT 2.5", after Cut 2 (each stage: greenfield battery + `guard.ps1` + INV-1-strong)
These D10.1–D10.5 stages ARE the PHASE-15 §"CUT 2.5" step list. Sequenced because the LPAREN mode-entry is an
all-or-nothing decision — the interpreted rules cannot coexist with the live mode, so the mode flips as the last grammar
step (D10.5), and (per §9.3) that flip requires the legacy tree already deleted — which is why the whole sub-track runs
AFTER PHASE 15 Cut 2 (§9.3 resolved). By the time D10.5 runs, the legacy `guard.sh`/353-MATCH net is itself retired, so
the verification metric is the greenfield `guard.ps1` + the D10.1 corpus + INV-1-strong.
- **D10.1** — this design note (done) + the §9.4 decision + a NEW characterization/conformance corpus exercising
  every enumerated form (multi-subscript space/comma lists, relative offsets, signed literals, ref-mod, qualified
  subscripts, nested FUNCTION args, string/national/boolean args, `table(ALL)`) captured BEFORE the change.
- **D10.2** — converge ALL ref-mod onto the existing DEFAULT-mode `refModPart` path; delete the ref-mod branch of
  `InterpretSubscripts`.
- **D10.3** — introduce the interpreted subscript grammar rule (per §9.4's answer) + rewrite
  `ReferenceResolver`'s subscript interpreters over real nodes.
- **D10.4** — MOSTLY PRE-EMPTED by P7 Step 12 (FUNCTION arguments already parse as real trees via
  `functionArgList` + the lexer's FUNCTION suppression / `SIGNED_*` twins / `FNARG_SEPARATOR`; the recursive-descent
  `SUB_*` parser is deleted; Udf/keyword-omitted route through ONE `BindArgOperand`). Residual: reunify
  `functionArgList` with `argumentList`, and re-home the keyword-omitted D2 fragment re-parse onto D10.3's
  interpreted subscripts.
- **D10.5** — delete the SUBSCRIPT-mode block + the `LPAREN` entry action + `PreviousTokenCouldBeDataName` +
  reconcile the Group-A drift test (the `subscriptTrigger` column goes dead) — **gated on §9.3** (legacy retirement
  / G8 coordination, since `SUB_*`/`SubscriptEntryContext` are legacy-shared).

**Metric:** token equivalence is NOT the goal (tokens change by design) — prove OUTPUT/behavior equivalence via the
D10.1 corpus + greenfield battery + FULL legacy guard 353 MATCH + INV-1-strong 349/349 at every stage.

**As executed (kb/Work PB2113, one change set — the stages could not land apart, because the mode flip is
all-or-nothing):**
- **D10.1** — §9.4 resolved above; the corpus is `85/pb2113_subscript_list_separators` (space / comma / semicolon lists,
  literals, qualified and relative subscripts, index-name ± integer, a list across a line end, ref-mod `(a:b)`/`(a:)`,
  a subscript then a ref-mod, space-separated and sign-adjacent arguments), `2002/pb2113_subscript_expressions_and_arguments`
  (Annex D.3.5.3's two examples, sign adjacency in a subscript list, keyword-omitted arguments with all three separators,
  nested FUNCTION and keyword-omitted calls inside a subscript, a keyword-omitted result's ref-mod, `table(ALL)`) and
  `negative/pb2113-keyword-omitted-arguments-85`. Every line produced the same output on the pre-change compiler.
- **D10.2** — `refModPart` takes `REF_LPAREN` (a ref-mod straight after a name) beside `LPAREN` / `FNARG_LPAREN`, and
  `refModSpec`'s leftmost position is optional so `X (:2)` still reaches its named refusal (COBOLNET2876). Each position is the
  superset `functionArgument`, the operand a subscript item takes, so a position that is not an arithmetic expression
  (`X ("A" : 1)`) reaches its named refusal too (COBOLNET2363, kb/Work PB1030) instead of a raw parse error. One
  carrier, one `ReferenceResolver.ReadRefMod`.
- **D10.3** — `subscriptPart : REF_LPAREN subscriptList? REF_RPAREN`, `subscriptList : subscriptItem ((FNARG_SEPARATOR |
  COMMA)? subscriptItem)*`, `subscriptItem : ALL | functionArgument` (the superset: the same paren carries a keyword-omitted
  argument list; a bare COMMA parses so SeparatorRule names it, COBOLNET2631). `ReferenceResolver.SegmentsOf` takes one
  token segment per item and applies PB136's declaration-informed cut on the TREE (a dataReference at the item's own
  level whose name cannot be subscripted gives its paren to a new subscript). `RenderSegment` now renders those segments;
  as a TOKEN renderer it is the transitional carrier the second half replaces.
- **D10.4** — the keyword-omitted channel binds the parsed `functionArgument` items (`IntrinsicBinder.ArgumentsOf`);
  `FunctionArgFragment`, `functionArgListFragment` and the lexer's `PrimeFunctionArgs` are deleted. The
  `functionArgList` / `argumentList` reunification is kb/Work PB2114's.
- **D10.5** — the `mode SUBSCRIPT` block, `SUB_DEBUG_LINE` (a debugging line inside a subscript is now lexed like any
  other, behind the DEFAULT `DEBUG_LINE` marker) and the dead structured rules are deleted. ⚠ CORRECTED: under Option A
  `PreviousTokenCouldBeDataName` and the `cobol-words.json` `subscriptTrigger` column do NOT go dead — they type the
  reference paren.
- **Side effect, spec-correct:** a subscript's arithmetic is now an ordinary `arithmeticExpression` node, so the
  `ExpressionFormationPass` screens reach it — §8.8.1.2 Table 3's (unary, unary) cell in `X (- - 2)` is refused
  (kb/Work PB1968).
