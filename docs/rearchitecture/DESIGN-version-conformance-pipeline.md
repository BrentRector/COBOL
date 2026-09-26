# DESIGN — Version-Conformance Pipeline (superset parse · edition-agnostic bind · one gating pass)

> **STATUS: DESIGN — ✅ IMPLEMENTED (PHASE-03; completed by Exec Step E).** The two-arm
> `VersionConformancePass` is the SOLE edition gate; the binder is edition-agnostic save the documented
> exception ledger (§1.1 — the UDF Check, the catalog-driven per-name windows, the two behavioral reads, the
> owner-disposition SYNC-on-group site). Exec Step E folded the last ~19 inline binder gates into the pass;
> all 9 PHASE-03 exit criteria hold.
> How COBOL.NET enforces edition (85 / 2002 / 2014 / 2023) conformance. The gating pass runs **post-bind** as a
> two-arm walk — one arm over the bound tree (semantic gates), one over the raw parse tree (syntactic gates + the
> §8.9 reserved-word funnel); version *identity* is recovered by the pass itself (bound-node type/attribute or
> parse-tree re-recognition — no `.Syntax` back-reference, no grammar annotation), while version *numbers* stay
> single-sourced in `constructs.json`. This doc is the canonical design for the
> version-conformance pipeline; the version-gating
> *framework* primitives (`EditionInfo`, `IDiagnosticSink`, `ConstructRegistry`, `constructs.json`) remain owned by
> [`DESIGN-edition-framework.md`](DESIGN-edition-framework.md). Cross-refs: [`DESIGN-frontend-grammar.md`](DESIGN-frontend-grammar.md),
> [`DESIGN-binder-bound-tree.md`](DESIGN-binder-bound-tree.md). SSOT for settled invariants stays `docs/COBOLNET_DESIGN.md`.

> **TWO-ARM ARCHITECTURE (the load-bearing invariant).** NO `.Syntax`/raw parse context is added to any bound node
> — the `BoundTree.cs` invariant STANDS. The pass (`VersionConformancePass.cs`) is **TWO-ARM**:
> (1) a BOUND-tree arm that re-identifies gates by bound-node TYPE or a resolved semantic ATTRIBUTE (the
> genuinely-semantic gates — MOVE-category, the USAGE/PICTURE-category and pointer-conditioned gates, and the
> DATA/PICTURE/OO attribute gates over every source-declared item); (2) a **presence-based PARSE-tree arm**
> (`ParseArm`, over `GroupBindContext.Tree`, running AFTER bind so a construct's semantic errors also accumulate) for
> the SYNTACTIC introduction/removal/phrase/expression/literal gates + the §8.9 reserved-word funnel — it ABSORBED
> the former `EditionValidator`. **WHY the split:** an INTRODUCTION/removal gate must fire on the construct's
> syntactic RECOGNITION, NOT its bound node — a bound-arm gate silently DROPS the 0900 whenever a below-edition
> construct ALSO has a semantic error (it binds to `BoundUnsupported`/`BoundNop`, so the distinctive node is never
> produced). The pipeline's end-state goals (superset parse · edition-agnostic bind · ONE pass · emit-if-clean · no
> `ReservedWordEditionHints`) hold.

## 0. Design rationale

Edition conformance is **one concern with one owner** — so it is one mechanism with clean, error-gated phase
boundaries, not logic spread across the grammar, the binder, and a post-hoc recogniser. Four fragmentation anti-patterns
this design rules out, each a way version gating decays if it is *not* a single post-bind pass:

| Anti-pattern | Why it fails |
| --- | --- |
| A post-hoc reverse-signature *recogniser* that re-diagnoses a failed parse | It *guesses* a construct's identity from token/rule/adjacency signatures — inherently heuristic; it mis-fires on legitimate §8.9 user words + garbled syntax. |
| Edition `{isYYYY()}?` *predicates* scattered through the grammar | Gating logic duplicated per rule instead of one version table; and a bare predicate can only fail to a generic parse error, never name the required edition. (Four physical per-edition grammars are worse — ~95% duplicated and still unable to say "requires COBOL-2002" without cross-referencing a newer grammar.) |
| `ConstructRegistry.Check` calls *embedded in the binder* | Couples version conformance to name/type binding; no single place owns the policy. |
| Gating discovered *during* code emission (bind and emit fused) | Codegen runs on an errored tree and the output is discarded — wasted work and a phase-boundary violation. |

The single mechanism defined below — **superset parse + construct identity recovered by the pass +
edition-agnostic bind + one two-arm `VersionConformancePass` (post-bind) + emit-if-clean** — is the direct answer to
all four (`feedback_one_mechanism_per_job`: one canonical mechanism per job, with error-gated phase boundaries).

## 1. Target architecture

```
  superset parse        grammar recognizes the UNION of all editions; NO edition {isYYYY()}? gates
        │               (a forward, identity-carrying lookahead survives ONLY where a construct is
        │                genuinely ambiguous across editions — the 2 load-bearing cases in §4).
        │               construct IDENTITY is recovered later, by the pass's parse-tree arm (§2.2).
        ▼
     Bind               edition-AGNOSTIC (the §1.1 exception ledger is the complete remainder). Produces the
        │               BoundProgram; ZERO ConstructRegistry.Check calls save the ONE documented UDF exception.
        │               NO bound node carries a `.Syntax`/parse back-reference (the BoundTree invariant).
        │               (No separate bind halt: the driver's ONE post-pass HasErrors gate covers both phases —
        │                bind + pass diagnostics ACCUMULATE, so a below-edition construct with a semantic error
        │                surfaces both; DEVLOG 14h.1.)
        ▼
  VersionConformancePass   ONE dedicated post-bind pass, in TWO arms. The SINGLE owner of all edition gating:
        │                  · syntactic gates → the parse-tree arm re-recognizes the construct → Check
        │                  · semantic gates  → inspect the resolved bound fact (is-group, operand-shape) → Check
        │                  strict: reject (COBOLNET0900/0902/0903).  permissive: accept-inert / warn.
        │               ── HALT if the pass produced errors ──   ← "no codegen on compile errors"
        ▼
     Emit               PURE codegen from a VALID bound tree. No diagnostics possible here except an ICE.
        ▼
     Roslyn
```

The pass is **table-driven** from `constructs.json`/`ConstructRegistry` (already the single version table) and is trivially testable in isolation: feed it a bound tree + an `EditionInfo`, assert the diagnostics — no binder, no emitter.

### 1.1 The gating-exception ledger (the COMPLETE list — everything else is pass-owned)

"Edition-agnostic bind" means: the binder makes NO hardcoded-year gating decision of its own. Exec Step E folded
the last ~19 inline `DialectLevel` diagnostic gates into the pass (each now a `constructs.json` row firing from
the correct arm, entering the version matrix with its negative witness). What legitimately REMAINS binder-side
is this closed, documented set:

1. **The ONE bind-time `Check`** — `UdfBinder` (UserFunctionInvocation2002): an intrinsic FUNCTION and a
   user-function call are syntactically identical, only the repository-resolved name set separates them, so the
   gate fires at bind on recognition (§2.4).
2. **Catalog-driven per-NAME windows** — version facts that live in their OWN tables, not constructs.json: the
   D8 intrinsic IntroducedIn/RemovedIn windows (`IntrinsicCatalog`, COBOLNET1502/1503, + the bespoke TRIM
   argument-2 window riding BindTrim), the `ExceptionCatalog` EC-name windows (COBOLNET0878 — EcBinder, the USE
   F3 name loop, `>>TURN`), the `PictureAnalyzer` picture-symbol rows (the ≥edition 0899 half of the W2 skeleton
   gate), and the §8.3.3.3.2 / §13.18.40 digit caps (`EditionContext.CheckDigitCapacity`, 0801/0802 —
   `EditionInfo.MaxDigits` is the table; §8.3.3.3.2 caps fixed-point literals at 1–31 digits and §13.18.40.3 SR14
   caps a numeric/fixed-point-numeric-edited item's PICTURE digit positions at 1–31).
3. **The two sanctioned BEHAVIORAL edition reads** (they select semantics for VALID programs, no diagnostic):
   the <2002 keyword-omitted FUNCTION routing gate (`IntrinsicBinder` — §8.4.3.2 SR2 routing is inert below
   2002) and the ≥2002 MOVE CORRESPONDING pair-selection window (`CorrespondingBinder` — the Table-16 NE row).
4. **The bind-time `Check` for SYNCHRONIZED on a group item** (`DataBinder.UsageInheritanceGroup`, row
   sync-on-group-2023): a group/elementary distinction the parse tree does not carry — whether the entry HAS
   subordinates is known only once the hierarchy is resolved — so this introduction fires at bind, through the
   canonical `ConstructRegistry.Check` funnel like every other. It is an ERROR ON BOTH AXES.
   ⛔ It was, until CA14 (2026-07-28), a manual `Edition.Removed`-severity emission chosen for accept-inert
   continuity; that made it the sole site contradicting `EditionSeverityPolicy.For(NotYetIntroduced) => Error`
   and the `EditionContext.Permissive` contract. **The exception is retired: there is no longer any site where
   an introduction is lenient.**

Any OTHER `DialectLevel` comparison appearing in `Binding/**` is a defect: relocate it into the pass.

**The introduction axis is now gate-enforced, not documented-and-hoped.**
`VersionMatrixTests.IntroducedConstruct_IsRejectedUnderPermissive` runs every active row × every edition below
its `introducedIn` under `--permissive` and requires a rejection. Until CA14 the permissive axis was tested only
for REMOVAL rows, which is why the exception above survived two phases — and why the same test immediately found
two more: `receive-as-user-word` and `end-receive-as-user-word` at COBOL-85, where the §8.9 reserved-word arm
hard-coded a `Removed` verdict. That arm now COMPUTES the verdict from the word's own reservation interval
(`ReservedWordSet.UserWordVerdictAt`): a spelling an edition took away is the migration case, a spelling reserved
at the target edition and at every edition before it never was a user word there.

## 2. Mechanisms

### 2.1 Superset parse
Remove the edition `{isYYYY()}?` predicates so every construct parses at every `--std`. The per-construct feasibility
analysis (§4) confirms **5 of 7** residue gates are *pure gating* (safe to remove outright); **2** are load-bearing for
disambiguation and keep a predicate — but **forward** (§2.3), not a reverse guess. This is the standard multi-version
model (Roslyn `LanguageVersion`, Clang/GCC `-std=`): parse a permissive superset, enforce the language level in a
later phase.

### 2.2 Construct identity (recovered by the pass, not stamped in the grammar)
So the pass can gate without the grammar carrying any edition facts, it recovers each construct's identity itself,
after parsing — never by a grammar action or a side table keyed on the parse node:

- **Syntactic gates → the parse-tree arm re-recognizes the construct.** The `ParseArm` visitor descends the raw
  compilation unit and, at each version-gated grammar rule, recognizes the construct from its OWN parse context (its
  rule type and tokens) and routes it through the one `Check`. Superset parsing (§2.1) is what makes this reliable:
  with the alternative no longer edition-gated, the rule is matched **deterministically**, so the parse node is
  present exactly when the construct is genuinely present. No hoisted `{…}?` gating predicate is needed — such a
  predicate is evaluated *speculatively* by ANTLR during failing prediction (the trap that killed the earlier
  forward-stamp attempt); recognition here happens post-parse over a fully-built tree.
- **The pass names the construct-ID, the table owns the version number.** A gate says only *"this node is
  `RaisingClause2002`"*; `constructs.json`/`ConstructRegistry` remains the sole owner of *"that construct requires 2002 /
  was removed at 2023."* No version facts in two places → no drift. The parse (or bound) shape says *what*, the table
  says *which edition*.
- **Semantic gates → the bound-tree arm keys on the resolved fact.** Where the identity is a RESOLVED bound
  attribute — a MOVE's source × receiver picture, an item's USAGE / PICTURE category, the operand USAGE that
  separates `SET × UP BY n`'s index form from its pointer form, the resolved category that re-routes a generic
  `SET a TO b` onto Format 5 or Format 16 — rather than mere presence, the bound-tree arm switches on the bound
  node instead.
- **The test is "WHICH RESOLVED FACT?", and a self-identifying bound-node TYPE is NOT an answer** (kb/Work
  PB353). That was the original wording, and it is what put five gates on the wrong arm: `BoundKeyedStart.Mode`,
  `BoundKeyedStart.Length`, `IBoundRead.Kind`, `IBoundRead.AdvancingOnLock` and `BoundInvoke` are all
  self-identifying, and all five are assigned from a PARSE fact and nothing else — so the bound-tree home bought
  no precision and cost the diagnostic on every path that returned `BoundUnsupported`/`BoundNop` before the node
  existed. `START NOSUCHF FIRST` at `--std 85` named COBOLNET1639 and no edition; so did `START SDF FIRST` on an
  SD, `START IXF KEY = NOSUCHK WITH LENGTH 3`, `READ NOSUCHF PREVIOUS ADVANCING ON LOCK` (two phrases at once)
  and `INVOKE NOSUCHO "M1"`. A gate belongs on the bound arm only when NO parse rule identifies the construct;
  everything else fires on recognition. `EditionGateArmDriftTests` derives both arms from the pass source and
  requires every bound-arm construct to name the resolved fact it needs, so the assignment is enforced rather
  than remembered.

### 2.3 Forward-detection (only where disambiguation is load-bearing)
Two constructs cannot be ungated outright without mis-parsing a valid below-edition program (§4 #2, #4). They keep a
grammar predicate, but generalized from a pure edition gate to an **identity-carrying forward detector**, following the
existing `boolExprAhead()` precedent (`CobolParserCoreBase.cs:83`):

```
( {is<E>() || <construct>Ahead()}? <constructRule> )?
```

`<construct>Ahead()` returns true **at all editions** iff the tokens genuinely form the construct in operator/phrase
position. When it fires below edition E, the construct is *recognized* (not mis-parsed as user names), so
the **same** conformance pass emits the exact diagnostic. Forward-detection is not a parallel diagnostic path — it only
steers the parse so the single pass can do the diagnosing. Contrast with the residue: the guesser infers identity
*after* a failed parse; forward-detection *proves* identity *during* the parse, then defers the verdict to the one pass.

The OPEN site's predicate is `{is2002() || retryPhraseAhead()}?`. **`retryPhraseAhead()` canonical spec:** true iff,
with RETRY at the lookahead in the OPEN-clause position, the tail after RETRY is an **UNAMBIGUOUS numeric** retry tail
(`n TIMES` | `FOR`? `n SECONDS`, where `n` contains an integer literal — an integer can never be a file name, so RETRY
must be the phrase keyword) **AND** at least one further candidate file-name token remains before the sentence
terminator (`openFileSpec+` must stay satisfiable). A bare `RETRY FOREVER` (FOREVER is a §8.10 user-legal word) or a
`RETRY <name>` count is *genuinely ambiguous* below 2002 and is EXCLUDED — those defer to `is2002()`. Consequences:
- `OPEN INPUT RETRY FOREVER.` (no numeric tail, no trailing name) stays a two-file-name list at 85;
- `OPEN INPUT RETRY 5 TIMES F.` forward-detects (the integer `5` disambiguates; `F` remains);
- the genuinely ambiguous `OPEN INPUT RETRY FOREVER F.` resolves to file names below 2002 (RETRY/FOREVER are legal user
  words there) and to the phrase at ≥2002 (`is2002()` is true; the §8.9 funnel reserves RETRY).

Fail-safe: a missed real gate (an ambiguous below-2002 tail) degrades to a neutral parse error, never a wrong edition
claim. The other five RETRY sites name their file BEFORE the phrase, so they carry no ambiguity and are gated by the
parse-tree arm on the RETRY phrase's recognition (`VisitRetryPhrase` → `Check(RetryPhrase2002)`), no forward detect needed.

### 2.4 The VersionConformancePass
- Input: the bound run unit (`GroupBindContext` — the bound programs + their raw parse `Tree`) + the target
  `EditionInfo` + an `IDiagnosticSink`. Output: diagnostics; no tree mutation in strict mode. In **permissive** mode
  it applies the accept-inert policy (warn, and where a removed construct has no emit path, elide it) — the natural
  home for the migration-mode "filtering."
- The parse-tree arm re-recognizes each syntactic construct and calls `ConstructRegistry.Check(edition, sink, id,
  where)`; the bound-tree arm computes the resolved fact from the bound node for the semantically-conditioned gates
  and Checks. Both funnel through the one `Check`.
- Absorbs the former `EditionValidator` (its §8.9 reserved-word funnel is the parse arm's `VisitCobolWord`, §3) and is
  the SINGLE home for every edition `Check`: the binder is edition-agnostic (the former `DataBinder*` /
  `StatementBinder*` / `OoClassTable` / `PicInfo` / `OdoModel` sites now fire from one of the pass's two arms), and the
  former emission-time gate (the anti-pattern #4) is gone. The ONE surviving bind-time `Check` is the documented
  UDF-invocation exception (`UdfBinder.cs`): an intrinsic FUNCTION and a user-function call are syntactically
  identical — only the repository-resolved name set separates them — so it fires at bind, on recognition, before
  operand binding.

#### 2.4.1 The SIBLING passes — what does NOT belong in VersionConformancePass

`VersionConformancePass` owns **edition gating**, and its charter ("the two arms are disjoint: a `Check` for any one
construct fires from EXACTLY one arm") describes construct edition checks. Sibling passes run in
`BinderDriver`, each on an axis orthogonal to editions. None is a bolt-on, and the reason is the same every time:
folding an orthogonal rule into the edition pass silently widens a charter that other decisions depend on.

| Pass | Axis | Severity | Keyed on | Runs |
|---|---|---|---|---|
| `FlagConformancePass` | migration flagging (§7.3.14 FLAG-02 / §7.3.15 FLAG-14) | always Warning | the user's `>>FLAG` directive state, regardless of `--std` | post-bind |
| `ExpressionFormationPass` | non-edition SYNTAX-RULE conformance (§8.8.1.2 Table 3, §8.8.2 Table 4) | Error | the parse tree alone — no edition, no directive state | post-bind |
| `LiteralScreenPass` | a LITERAL's own syntax rules (§8.3.3.2.3 SR1/SR6, §8.3.3.4.3 SR1, §8.3.3.5.3 SR1/SR5 — length ≤ 8,191 positions of its class, COBOLNET0814; hexadecimal grouping, COBOLNET1635) | Error | every literal TOKEN of the raw tree, DEFAULT-mode and SUBSCRIPT-mode twins alike (a plain walk, not a visitor, so no override can hide a literal — kb/Work PB1393; `LiteralScreenDriftTests` derives the token set from the lexer) | post-bind |
| `DeclinedFacilityPass` | Annex A.4 optional elements this implementation does not claim | Error | the claim register (`docs/CONFORMANCE.md` §5), not the edition | post-bind |
| `ClosedFormatPass` | a CLOSED general format's residue (§4.2.2 — every `unrecognizedClause` the grammar reached, COBOLNET1941/1970/1971, named by its format and § from `ClosedFormats.ByContext`) | Error | the parse tree alone, at every edition and strictness (kb/Work PB829) | post-bind |
| `LevelNumberPass` | the level-number's own syntax rules (§13.18.33.3 SR2/4/5/6, §13.18.33.4 GR2, §13.16.3 SR1/SR2) | Error | the parse tree alone — the entry's SECTION ancestry and its BODY | **pre-bind** |

**`LevelNumberPass` is the one that runs FIRST, and deliberately.** The others consume the bound model or sit
with the edition pass by convention; this one reads a token, the section the entry is in, and the shape of the
entry body, and needs nothing binding produces. Screening before the binder matters because a level-number outside
its permitted set makes the storage tree it heads meaningless: `78 K VALUE 5.` bound as a memberless GROUP nested
under whatever entry preceded it, so every later reference mis-qualified and the real fault surfaced as a RUN-time
`NotImplementedCobolFeatureException`. Running first replaces that cascade with one diagnostic at the entry, and
makes it impossible for a bind-time failure on the malformed structure to preempt the diagnostic that explains it.

**Two axes, one pass, two codes.** A level-number is constrained twice over and the constraints are independent:
by the SECTION its entry lives in (§13.18.33.3 — four *different* sets; 77 is legal in working-storage and illegal
in a record area) and by the general FORMAT the entry is written in (§13.18.33.4 GR2's "may be used only as
described by …" plus §13.16.3 SR1/SR2). `COBOLNET1746` reports the first, `COBOLNET1747` the second, because the
user action differs: one says the level-number may not appear here at all, the other says the level-number is fine
but the entry under it is not the format it requires. The FORMAT is read from the entry BODY and never from the
level-number — deciding it from the level would make GR2b and GR2c vacuous — so the level-number selects which
check runs and the body only answers it. `05 R RENAMES A THRU B.` is the case the format axis catches and the
section axis cannot: its level is perfectly in range, and before the pass it reached the EMITTER and produced
uncompilable C#.

**The arm list is DERIVED, not remembered.** Four grammar rules spell a `levelNumber` — `dataDescriptionEntry`,
`linkageProcedureParameter`, `reportGroupEntry`, `screenDescriptionEntry` — and they reach three different binders
plus, for the procedure-parameter form, none at all. A per-binder check would have been four copies of one rule
with a fifth site guaranteed to be forgotten. `LevelNumberArmDriftTests` reads the `.g4` files and fails until
`LevelNumberRules.Classify` names every rule that spells a level-number and every section that hosts a data
description entry (kb/Work PB485).

**Why the formation tables cannot be edition-gated.** They have no `introducedIn`: the (unary, unary) and
(B-NOT, B-NOT) pairs are invalid in 1985, 2002, 2014 and 2023 alike. There is no `constructs.json` row to write and
no `Check` to make.

**And no dialect arm exists either** — this is the part worth recording, because the contrary assumption is the
natural one. The two-axes model gates LENIENCIES, but `--permissive` softens exactly one verdict:
`EditionSeverityPolicy.For` maps `ConstructAvailability.Removed` to Warning and nothing else. An invalid symbol
pair was never legal at any edition, so it is not a removed construct, and a `--permissive` arm here would be a
flag nothing could ever set. Measured against `EditionSeverityPolicy` before the pass was written (kb/Work PB158).

The RULE itself lives in the frontend (`ArithmeticFormationRules`), not in the pass, because the compile-time
expression evaluator applies the same rule during compiler-directive processing — before any compiler pass exists.
The pass is the compiler's invocation of it, once per parse tree; `CompileTimeExpressionEvaluator` is the other
consumer. This is the `BooleanExpressionResolver` pattern: one frontend rule, consumer-generic, so the three
evaluating arms (`ExpressionBinder.BindExprCore`, `EvalArith`, and `SoleNumericLiteral`) cannot drift apart.

#### 2.4.2 The ONE gate site that runs BEFORE the pipeline — compiler directives (§7.3)

`VersionConformancePass` gates constructs the parser produced a tree for. A **compiler directive never becomes a
tree**: ISO §7.2 consumes it in the text-manipulation stage, so by the time any pass exists the line is a blank.
Its edition gate therefore cannot live in the pass, and the disjointness charter above extends to say **exactly
where it does live: `ConditionalCompilationProcessor`, at the single point it recognizes a `>>` word.** That one
site covers the whole §7.3 family — the directives it consumes, the ones a downstream stage owns
(`Frontend.LeftDirectives`), and the conditional-compilation directives its own switch handles — because §7.3.2
gives them ONE general format (`>>compiler-instruction`) and §7.3.3 SR6 opens compiler-instruction with ONE
compiler-directive word (§8.12). It calls the same `ConstructRegistry.Check` funnel and the same
`EditionSeverityPolicy`, so the codes are the pass's codes: COBOLNET0900 below the introducing edition,
COBOLNET0902 at a removing one, COBOLNET0903 at an obsoleting one.

**The roster is derived, and that is the load-bearing part.** `constructs.json` rows carry a `directiveWords`
column; `CompilerDirectiveCatalog` (`Cobol.Net.Editions`) inverts it into the word → row map the stage consults.
Adding a directive is ONE row plus `pwsh scripts/gen-constructs.ps1` — the word becomes recognized, its edition
becomes enforced, and the version matrix compiles its `source` at all four editions, in one change.

**The same site asks the OPERAND question, off the same row** (kb/Work PB794). §7.3.3 SR6 composes
compiler-instruction "as specified in the syntax of each directive", so "may this word head a `>>` line at this
edition" and "may these words follow it" are two questions with one answer each per directive; the second is the
row's `directiveOperand` column, and `CompilerDirectiveCatalog.CheckOperand` is its ONE producer —
**COBOLNET1911** for the whole family. The column is a TOTAL partition, enforced by the generator and by
`CompilerDirectiveCatalogDriftTests`: `words` (a closed set from the printed general format — with the optional
words, whether the choice may be omitted, whether a catalogued directive-name or a user-defined word is
admissible), `text` (the content is not checked, and the citation says by WHOSE decision — §7.3.19.3 SR2 for
PAGE's comment-text-1, unbuilt residue for §7.3.12.2's DISPLAY operand syntax, the removed clauses for the two
FLAG windows), or `stage` (a named downstream stage parses it: TURN, the FLAG pair, COBOL-WORDS, and the
conditional-compilation driver's own expression operands). ⛔ Whether the choice is omissible is read off the
PRINTED UNDERLINING, never the braces: §7.3.24.2 underlines FIXED and FREE so `>>SOURCE FORMAT` alone is
malformed, while §7.3.17.2/§7.3.18.2/§7.3.21.2/§7.3.23.2 leave `ON` un-underlined, so a bare `>>LEAP-SECOND` is
conforming (§5.2.3). Before PB794 this rule was written in six stages with six codes and not at all for the seven
directives the driver consumes, so `>>SOURCE FORMAT UNKNOWN`, `>>LISTING GARBAGE` and `>>PUSH GARBAGE` compiled
in silence. **COBOLNET0883 and COBOLNET1576 are RETIRED** by that consolidation; COBOLNET1650 keeps only
§7.3.17.3 SR1's placement rule.

**And the directive LINE has one parse.** `CompilerDirectiveLine` (`Cobol.Net.Editions`) owns the indicator with
its optional following space (§7.3.3 SR5), the directive word, and the operand with a §7.3.3 SR3/SR4 inline
comment removed (quote-aware, so the `*>` inside `>>DISPLAY "a *> b"` is data). Every stage calls it. Seven
hand-rolled copies of `trimmed[2..].TrimStart()` are gone, and with them a class of defect the copies all shared:
`>>PROPAGATE ON *> on` was rejected as a malformed operand, and `>>SOURCE FORMAT FIXED *> switch` was not
recognized at all — the following segment was then read in the WRONG reference format and the error surfaced on a
line the user had not written wrong. ⛔ A trailing PERIOD is not tolerated on any directive: SR3/SR4 admit spaces
and an inline comment and nothing else.

**Why this had to be restructured rather than extended** (kb/Work PB725): the same rule was written down THREE
times — this funnel in five downstream stages, a hand-rolled `if (dialectLevel < 2002)` with a bespoke code in two
more (`>>TURN`/COBOLNET0875, `>>PROPAGATE`/COBOLNET0883), and, for a flat `KnownIgnoredDirectives` name set with
no edition column, nowhere at all. Eleven ISO directives compiled clean at `--std 85`, an edition that has no
compiler directives whatsoever, and nothing could have failed, because a name set cannot be wrong about an edition
it does not record. Consolidating deleted the two bespoke gates (COBOLNET0875 is RETIRED; COBOLNET0883 kept only
its §7.3.21.2 malformed-operand half, until PB794 retired that too), deleted four byte-identical `BagSink`
copies, and removed `dialectLevel`/`permissive` from four stage signatures that no longer ask an edition
question. PB794 then removed the diagnostic channel itself from the PROPAGATE and REF-MOD-ZERO-LENGTH stages,
which no longer report anything at all.

**`>>SOURCE FORMAT` is the documented exception, and there is exactly one.** `ReferenceFormatProcessor` consumes
its line before the conditional-compilation driver runs — it must, because the following segment's reference
format depends on it — so that stage carries the gate, keyed on `Constructs.SourceFormatDirective2002`, and the
operand check with it. Same row, same producers, one stage earlier; it recognizes the directive by its WORD, so a
malformed operand is diagnosed and the line still consumed (the reference format in effect carries on unchanged,
because the directive selected none). `CompilerDirectiveCatalogDriftTests` re-derives the roster from the
`#### 7.3.N <WORD> directive` headings of `specs/ISO_COBOL.md`, checks each row cites its own clause, asserts
`Frontend.LeftDirectives` is a subset of the catalog, and drives the real stage to prove every recognized word is
rejected at 85 — so "one site, complete roster" stays true rather than being remembered.

### 2.5 Bind/emit phase separation (the "no codegen on errors" fix)
`CompilerDriver` runs `bind → conformance-pass → (halt if errors) → emit`. **Binding** (producing the `BoundProgram`)
and **emission** (rendering C# from a valid `BoundProgram`) are distinct phases and the driver gates between them.
Emission is never reached when parse, bind, or the conformance pass produced an error.

**Verdict surfaces include the pass.** `CheckOnly` / `check-batch` = stop after bind **+ the conformance pass** — the
pass is part of the verdict; only emit is skipped. `CheckOnly`, `check-batch`, `EditionHarness`, and the INV-1
continuity + INV-1-strong legs include pass diagnostics in their verdicts (the strict edition band
codes come from the pass, so a bind-only verdict would silently drop every edition diagnostic).

## 3. What this deletes / keeps

**Deleted:** `ReservedWordEditionHints.cs` in full (all 7 reverse-signature arms + the heuristic helpers
`PrecededByOperand` / `NextWithin` / `PrecededByAnyBeforeDot` / `InRule`). No positional or
signature heuristic survives it in any form.

**Deleted:** `EditionValidator` — absorbed into the `VersionConformancePass` (§2.4); its §8.9 reserved-word funnel is
the pass's parse-tree arm (`VisitCobolWord`).

**Kept (orthogonal — NOT introduction gates):**
- The **§8.9 reserved-word funnel**: rejects a *genuine user-word* spelling at/above the edition where the word became
  reserved (`COBOLNET0901`). Inverse gate (word→reserved), unchanged in behavior; it lives inside the
  `VersionConformancePass` (the parse-tree arm's `VisitCobolWord`).
- The **VALUE/PROPERTY boundary guards** (`CobolData.g4:382/389`): value-operand-loop disambiguation, not an edition gate.
- The **vendor JSON/XML `COBOL0313` disposition**: a dialect/vendor-extension disposition, not an ISO edition gate. It
  lives in `CobolErrorStrategy` as a **token-keyed vendor hint** — a parse-error re-diagnosis of
  hard-reserved tokens, not an ISO edition gate. No signature table is resurrected for it.

## 4. Per-construct residue classification (feasibility analysis)

| # | Construct (id) | Predicate role | Mechanism | Recognition / detect point | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | `LogicalXorOperator2023` (XOR / EXCLUSIVE-OR) | gating-only | bind-time → pass | `LogicalXorExpressionContext` (guard `ChildCount>1`) | low |
| 2 | `BooleanOperators2002` (B-AND/B-OR/B-XOR/B-NOT + condition ENTRY + COMPUTE F2) | **both** | hybrid: pass for operator tiers + COMPUTE F2; **forward-detect** `boolExprAhead()` for the condition ENTRY | `BindBoolExpr` (guard `HasBoolOp`) | medium |
| 3 | `FileSharingClause2002` (SELECT + OPEN SHARING) | gating-only | bind-time → pass (both sites) | SELECT clause; OPEN phrase | medium (OPEN name-list collision) |
| 4 | `RetryPhrase2002` (RETRY on OPEN/READ/WRITE/REWRITE/DELETE) | **both** | **6 predicate sites** (`openClause:232`, `readStatement:291`, `writeStatement:343`, `rewriteStatement:384`, `deleteStatement:407`, `deleteFileStatement:425` in `Core/CobolIO.g4`) — hybrid: bind-time → pass for the 5 statement sites; **forward-detect** `retryPhraseAhead()` (§2.3) for OPEN | `BindRetry` | medium |
| 5 | `UnlockStatement2002` | gating-only | bind-time → pass | `BoundUnlockStatement` (own token) | low |
| 6 | `ProcedureRaising2002` (PD … RAISING) | gating-only | bind-time → pass | PD linkage bind (`raisingClause() is not null`) | low |
| 7 | `PropertyClause2002` (data-desc PROPERTY) | gating-only | bind-time → pass | data-description clause loop | low |

No construct requires the guesser to survive. #2-ENTRY and #4-OPEN retain a *grammar* predicate, but forward and
identity-carrying. Disambiguation counterexamples that make the two load-bearing: `IF B-AND = 5` with `01 B-AND PIC 9`
at `--std 85` (#2); `OPEN INPUT RETRY FOREVER.` with files named RETRY and FOREVER at `--std 85` (#4 — stays a
two-file-name list under the `retryPhraseAhead()` canonical spec, §2.3).

## 5. Migration plan (incremental, each step guardable)

Any `.g4` edit ⇒ ANTLR regen + the **FULL** legacy guard (not `guard-fast`). Pure code steps ⇒ greenfield conformance +
unit, then the full legacy guard before commit. One construct/phase per commit, each with a DEVLOG entry + a
version-matrix continuity/introduction check + negative fixtures.

The pipeline lands **residue-first**: the residue gates migrate onto bind-time `Check`s one construct per commit
(each individually guardable), the recogniser then has nothing left to recognise and is deleted, and the skeleton
finally funnels every bind-time `Check` into the one pass.

- **Stage 1 — residue migration (batches, ascending risk).** Per §4: **A** (low) UNLOCK, PROPERTY, PD-RAISING, XOR →
  remove the grammar predicate, gate bind-time via `ConstructRegistry.Check`, delete the recogniser arm. **B**
  (medium) SHARING — the SELECT clause + OPEN phrase sites together (one construct, one commit), with adversarial
  characterization fixtures for the OPEN name-list collision (byte-identical parse). **C** (medium) RETRY — the six
  grammar predicate sites of §4 #4: the five statement sites drop their predicate and gate bind-time via
  `Check(RetryPhrase2002)` at the retry-binding site; the OPEN site becomes `{is2002() || retryPhraseAhead()}?`
  (§2.3) — and the boolean family (7a: the operator tiers + COMPUTE F2 are pure gating — drop the predicates + one
  `Check(BooleanOperators2002)` in `BindBoolExpr` guarded by `HasBoolOp`; 7b: the boolean-condition ENTRY generalizes
  `boolExprAhead()` to fire at all editions with operand-adjacency — highest scrutiny, it is the shared comparison
  DFA most prone to regression).
- **Stage 2 — delete `ReservedWordEditionHints.cs`** entirely, with its helpers; the vendor JSON/XML `COBOL0313`
  disposition relocates to `CobolErrorStrategy` as a token-keyed vendor hint in the same commit (§3); grep-sweep
  lingering refs; update `DESIGN-frontend-grammar.md`, `DOC_INDEX.md`, DEVLOG.
- **Stage 3 — the pipeline skeleton.** Build the two-arm `VersionConformancePass`; funnel every
  compiler-embedded `ConstructRegistry.Check` call site into it (`DataBinder*` / `StatementBinder*` / `OoClassTable` /
  `PicInfo` / `OdoModel` + `EditionValidator`'s own + the former emitter-side gate); absorb
  and DELETE `EditionValidator` (its §8.9 reserved-word funnel becomes the parse arm's `VisitCobolWord`); make the
  binder edition-agnostic (zero `Check`s save the documented UDF exception); no `.Syntax` back-reference is added to
  any bound node (the parse-tree arm re-recognizes the syntactic gates instead); split bind/emit in
  `CompilerDriver` (bind → pass → HALT on errors → emit); point `CheckOnly` / `check-batch` / `EditionHarness` /
  the INV-1 continuity + INV-1-strong legs so their verdicts include pass diagnostics (§2.5). No behavior change
  (same diagnostics, now from one pass) — proven byte-identical by the full legacy guard + INV-1 sweep. Delivers
  "no codegen on errors" + "dedicated pass".

## 6. Risks

- **ANTLR optional-block / DFA greediness** on the two OPEN name-list sites (SHARING, RETRY) and the shared comparison
  DFA (boolean ENTRY): the one place static reasoning cannot fully certify. Contained by sequencing these last, shipping
  the pre-specified adversarial characterization fixtures (byte-identical parse before/after), and the full legacy guard
  after each regen. The OPEN RETRY site keeps its forward `retryPhraseAhead()` by design (§2.3/§4); pre-designed
  fallback for the OPEN SHARING site: retain an analogous forward `openSharingAhead()` — a config flip, not a redesign.
- **Parse-arm ↔ bound-arm assignment** must be correct for every version-gated construct: an introduction/removal or
  otherwise purely-syntactic gate belongs on the parse-tree arm (so it fires on recognition even when the construct
  also fails to bind), and only a genuinely-semantic gate (identity = a resolved bound attribute) belongs on the
  bound-tree arm. No `.Syntax`/parse back-reference is added to a bound node to bridge the two (the BoundTree
  invariant); a misassigned syntactic gate would silently drop its diagnostic on a semantic-error path.
  **This risk MATERIALIZED for five gates and is now mechanically closed** (kb/Work PB353): the misassignment is
  no longer a review question but two CI facts — `EditionGateArmDriftTests` requires every BOUND-arm construct to
  name the resolved fact that keeps it there (and forbids an unadjudicated construct gated from both arms), and
  its survival theory recompiles every gated construct's own `constructs.json` program with the
  procedure-division operands broken, requiring the edition diagnostic to outlive the bind.

## 7. SSOT / doc impact

- `docs/COBOLNET_DESIGN.md` — records the pipeline as `parse → bind (edition-agnostic) → version-conformance pass →
  emit`, and that edition gating is a single bound-tree pass.
- `DESIGN-edition-framework.md` — add a pointer to this doc; note the pass is the single consumer of `ConstructRegistry`.
- `DESIGN-frontend-grammar.md` — the superset-parse + construct-id-annotation convention (action-not-predicate).
- `DESIGN-binder-bound-tree.md` — the binder is edition-agnostic; no bound node carries a `.Syntax`/parse back-reference.
- `DOC_INDEX.md` + `docs/COBOLNET_REARCHITECTURE_PLAN.md` — index this doc; slot the migration as a phase.
