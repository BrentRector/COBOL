# SURVEY — Frontend Plumbing (preprocessor · parsing · diagnostics · edition-gate coupling)

> Scope: `src/Cobol.Net.Frontend/{Preprocessor, Parsing, Pipeline, Common, Diagnostics}`. AS-BUILT
> assessment for the rearchitecture. Every claim is cited to `file:line` as read in the tree on
> 2026-07-07. Companion plan docs: `DESIGN-frontend-grammar.md`, `DESIGN-edition-framework.md`,
> `PHASE-02-…`, `PHASE-04-…`. The "ROADMAP GAP CHECK" section at the end reconciles this survey with
> those plans.

---

## 1. Responsibilities & pipeline place

The frontend is the **source → preprocessed free-form text → ANTLR parse tree** stage. It is the
compiler's first phase and its only ANTLR-facing surface; downstream, `Cobol.Net.Compiler`'s binder
consumes the raw generated parse contexts. The orchestrator is `Pipeline/Frontend.cs` (147 loc),
which runs two phases:

- **Phase 0 — `Preprocess`** (`Frontend.cs:75-108`): a fixed 6-stage text pipeline
  `StripNistArchiveMarkers → NormalizeToFreeForm(edition,permissive) → ConditionalCompilation →
  CopyProcessor(edition,permissive) → NistPreprocessor(if NIST) → TurnDirectiveProcessor`.
- **Phase 1 — `LexAndParse`** (`Frontend.cs:118-146`): `CobolLexer` → `CommonTokenStream` →
  `ZeroTokenRewriter.Rewrite` → two-stage `SLL(BailErrorStrategy)` then `LL(CobolErrorStrategy)`
  parse, returning `CompilationUnitContext` or `null` if `NumberOfSyntaxErrors > 0`.

The `Common/` and `Diagnostics/` folders provide the shared value types (`SourceLocation`,
`TextSpan`, `SourceText`) and the diagnostic model (`Diagnostic`, `DiagnosticBag`,
`DiagnosticDescriptor(s)`, `DiagnosticSeverity`) used by every stage.

---

## 2. Key types (name · role · LOC · assessment)

| Type / file | Role | LOC | Assessment |
|---|---|---|---|
| `Pipeline/Frontend.cs` | Pipeline orchestrator; owns `DialectLevel`, `Permissive`, `NistTestName`, `TurnEvents` | 147 | **Good.** Small, linear, well-documented. Two defects: `catch (Exception)` on the SLL bail (`:135`) and a raw `throw` for the TURN line-count invariant (`:103-105`). Namespace is the only frontend file already at `CobolNet.Frontend`. |
| `Preprocessor/CopyProcessor.cs` | COPY + REPLACE expansion, text-word tokenization, REPLACING-operand mini-parser, copybook resolution, fixed/free copybook normalization | **737** | **Largest surface / mild god-object.** Cohesive around COPY/REPLACE but bundles 4 sub-responsibilities: a hand-rolled text-word tokenizer (`TokenizeTextWords :219`), a REPLACING-operand parser (`ReadReplaceOperand :609`), file resolution (`FindCopybook :705`), and a fixed-form detector (`NormalizeCopybook :443`). Carries its own edition-severity gate (`OnNonPseudoTextOperand :29`). |
| `Preprocessor/ReferenceFormatProcessor.cs` | Fixed↔free reference-format normalization, `>>SOURCE FORMAT`, continuation/literal state machine, comment-entry handling, NIST marker strip | 578 | **Dense but cohesive.** A cross-line literal state machine (correct, subtle). Hosts nested `EditionGates` class (`:120-149`) that re-implements the strict/permissive severity policy. Static class with static entry points — no per-file state, edition passed positionally. |
| `Preprocessor/ConditionalCompilationProcessor.cs` | `>>DEFINE/IF/ELSE/EVALUATE/WHEN` conditional compilation + a constant-conditional-expression parser | 403 | **Good, self-contained.** Its own tokenizer (`Tokenize :341`) + recursive-descent `CondParser`. No edition awareness (CC is 2002+ but ungated here — see §5). Byte-neutral on directive-free source. |
| `Preprocessor/NistPreprocessor.cs` | NIST CCVS X-card (`XXXXX###`) placeholder substitution | 213 | **Test-corpus glue, not a compiler concern.** A wall of `source.Replace(...)`/regex. Mixes naive `.Replace` (`:53,64,173-209`) with anchored regex (`:74,87,132`). Latent embedded-literal corruption risk for the naive ones (§6). |
| `Preprocessor/TurnDirectiveProcessor.cs` | `>>TURN` directive parsing → `TurnEvent` list; blanks lines (line-count preserving) | 151 | **Good, spec-cited.** Emits COBOLNET0875/0718/0719 as **raw string codes** (no descriptor). Uses a 0-based `SourceLocation` while `TurnEvent.Line` is 1-based (deliberate, but see the SourceLocation inconsistency in §6). |
| `Parsing/EditionGateHints.cs` | Reverse-engineers which edition-gated construct a generic `NoViableAlternative` was | 207 | **Worst smell in the frontend (see §3).** A 29-signature `(token, rule-stack, lookahead)` table, empirically derived, that re-derives an identity the gate discarded and re-copies edition metadata the frontend can't see. |
| `Parsing/CobolErrorStrategy.cs` | COBOL-aware error messages; 19 intent heuristics; edition-gate mapping | 238 | **Mostly good.** The 19 `GuessCobolIntent` heuristics (`:100-211`) are useful and non-duplicative. Only coupling smell: heuristic 0 calls `EditionGateHints.Recognize` (`:113`) and message is a pre-formatted `[code] text` string (`:95`) that `CobolErrorListener` must re-parse. |
| `Parsing/CobolParserCoreBase.cs` | ANTLR parser base: `DialectLevel`, `is85/2002/2014/2023` predicates, `IsAtLineStart`, `IsBareInspectOperand`, `boolExprAhead` | 114 | **Good.** Clean disambiguation predicates. Holds a **second** `DialectLevel` store (`:17`) beside `Frontend.DialectLevel`. Namespace `legacy-root.Generated`. |
| `Parsing/ZeroTokenRewriter.cs` | Rewrites `ZERO` → `ZERO_ARITH` in arithmetic context to avoid exponential ANTLR prediction | 146 | **Clean, self-contained, no smell.** Preserve verbatim. |
| `Parsing/CobolErrorListener.cs` | ANTLR `BaseErrorListener` → `DiagnosticBag`; extracts `[code]` prefix; caps at 20 | 56 | **Adequate.** Reconstructs a `DiagnosticDescriptor` code from a **string prefix** the error strategy embedded (`:41-49`) — a stringly-typed round-trip. Off-by-one `SourceLocation.Line` (§6). |
| `Common/SourceLocation.cs` | `(FileName, Position, Line, Column)` value | 47 | Fine. Documented **zero-based** Line, but producers disagree (§6). |
| `Common/TextSpan.cs` | `(Start, Length)` half-open span | 30 | Fine, complete. |
| `Common/SourceText.cs` | Source buffer with line-start index, binary line/col lookup | 129 | **DEAD in the frontend pipeline.** Referenced by no file but itself (grep: 1 hit). Every preprocessor stage recomputes line numbers by hand instead of using it (§4). |
| `Diagnostics/DiagnosticDescriptor(s).cs` | The descriptor record + the central `static partial` registry (~200 codes) | 694 | **The best-formed part of the model AND a layering smell (see §7).** A real registry, but it physically lives in the frontend while holding compiler-phase codes. |
| `Diagnostics/DiagnosticBag.cs` | Diagnostic collector | 47 | **Two parallel report paths** (descriptor-based `:38` and raw code+severity `:17-31`) — a mild singular-pattern violation. |
| `Diagnostics/Diagnostic.cs` / `DiagnosticSeverity.cs` | Diagnostic record + severity enum | 31 / 11 | Clean, structured. This is the model the compiler side *lacks*. |

---

## 3. Architecture smells (severity · file:line)

### S1 — `EditionGateHints` reverse-engineers what the grammar already knew · **HIGH** · `Parsing/EditionGateHints.cs:35-207`
The single worst smell. The grammar's `{isXXXX()}?` introduction predicates
(`CobolParserCoreBase.cs:19-22`) reject a too-new construct during ANTLR prediction, which surfaces
as a generic `NoViableAlternative`. `EditionGateHints` then spends 207 loc **guessing back** which
construct was rejected via a 29-arm `switch` over `(offending-token, rule-stack, lookahead-window)`
signatures (`:89-155`), several with dual-path token-adjacency fallbacks because "the enclosing rule
can have POPPED off the invocation stack by the time the error is reported" (`:85-88`). The
signatures are self-described as "derived empirically (DEVLOG 594)" (`:21`). This is an entire
subsystem whose only job is to recover an identity the failing predicate *had and threw away*. It is
brittle against any grammar rule rename (rule-stack strings like `"gobackStatement"`,
`"dataDescriptionEntry"` are matched as literals, `:100-153`) and against any restructure that moves
where the error token surfaces. It is a second, backward mechanism for a concept the forward
predicate already owns — a singular-pattern violation at the core of "four editions in one".

### S2 — Frontend re-encodes edition metadata the compiler owns · **HIGH** · `EditionGateHints.cs:35-63`; `ReferenceFormatProcessor.cs:120-149`; `CopyProcessor.cs:29-41`
Because `Cobol.Net.Frontend` has no project reference to `Cobol.Net.Compiler` (where
`ConstructRegistry` lives), the frontend physically cannot see the canonical edition registry, so it
**re-copies** it three times: `EditionGateHints` hand-copies `(Display, IntroducedIn, Citation,
RowId)` for ~29 constructs (`:35-63`); `ReferenceFormatProcessor.EditionGates` and
`CopyProcessor.OnNonPseudoTextOperand` each inline the strict/permissive severity policy
(`if (permissive) ReportWarning else ReportError`, `ReferenceFormatProcessor.cs:144-147`,
`CopyProcessor.cs:37-40`). Three independent renders of "removed = error strict / warning permissive"
plus a full metadata duplicate. This is the frontend↔edition-metadata duplication the rearchitecture
must end.

### S3 — Two diagnostic models end-to-end · **HIGH** · `Diagnostics/*` vs compiler `EditionContext.Diagnostics/Warnings`
The frontend has a clean **structured** model (`Diagnostic{Code,Severity,Message,Location,Span}` +
`DiagnosticBag` + `DiagnosticDescriptor`). The compiler side accumulates diagnostics as
`List<string>` (`$"error {code}: {message}"`, per `PHASE-02` P6/P8). So the pipeline carries two
incompatible diagnostic representations, and the *good* one is trapped in the frontend assembly.
Within the frontend itself `DiagnosticBag` exposes two report paths (`DiagnosticBag.cs:17` raw code
vs `:38` descriptor) that are used inconsistently (§7).

### S4 — Preprocessor emits raw string codes, bypassing the descriptor registry · **MEDIUM** · `TurnDirectiveProcessor.cs:50,87,123,134,143`; `ReferenceFormatProcessor.cs:129,145,147`; `CopyProcessor.cs:38,40`
The COBOLNET07xx/08xx/09xx band (0718, 0719, 0875, 0902, 0903) is emitted via
`ReportError("COBOLNET0902", literalMessage, …)` with **no `DiagnosticDescriptor` entry** — the
message text is inlined at the call site. Meanwhile the same files use the descriptor path for the
CBL36xx COPY codes (`CopyProcessor.cs:60,318,372`). So the frontend inconsistently mixes
descriptor-based and stringly-typed emission. The registry exists but is only ~half adopted.

### S5 — `Frontend.cs:135 catch (Exception)` masks predicate/lexer-action bugs · **MEDIUM** · `Pipeline/Frontend.cs:135`
The SLL→LL fallback catches **all** exceptions, so a `NullReferenceException` or
`InvalidOperationException` thrown from a buggy semantic predicate (`boolExprAhead`) or lexer action
is silently swallowed and retried under LL, hiding a real defect behind a second parse. Should be
narrowed to `ParseCanceledException` + `RecognitionException`.

### S6 — `CopyProcessor` is a 737-loc multi-responsibility class · **MEDIUM** · `Preprocessor/CopyProcessor.cs`
Not a true god class (it is cohesive around COPY/REPLACE), but it hand-rolls a COBOL text-word
tokenizer, a REPLACING-operand parser, copybook file resolution, and a fixed-form heuristic in one
file — each a candidate for extraction, and each duplicating tokenization logic that also exists in
`ConditionalCompilationProcessor` and the lexer (§4).

### S7 — Dead `SourceText` abstraction; line-number logic re-rolled per stage · **LOW** · `Common/SourceText.cs` (unused); `Frontend.cs:110`, `CopyProcessor.cs:64`, `ReferenceFormatProcessor.cs:249`, `TurnDirectiveProcessor.cs:39`
`SourceText` provides exactly the line/column index every stage needs, yet it is referenced nowhere
(grep: 1 self-hit). Instead `Frontend.CountLines` counts `\n`, `CopyProcessor.LineOf` scans,
`ReferenceFormatProcessor` tracks `lineNo`, `TurnDirectiveProcessor` uses the split index — four
ad-hoc line mechanisms plus a dead one.

### S8 — Stale namespaces on 16 of 17 frontend files · **LOW** · every file except `Frontend.cs`
All Preprocessor/Parsing/Common/Diagnostics files still declare `namespace legacy-root.*`
(grep confirmed 16 files) though physically in `Cobol.Net.Frontend`. `Frontend.cs:16` further claims
it "is the ONE place WiseOwl COBOL reuses the legacy compiler assembly" — **stale**: the
code was already physically extracted; only the namespace strings remain legacy.

---

## 4. Coupling · mutable state · cross-layer reach

**Frontend ↔ edition-metadata duplication (the headline coupling).** Root cause: assembly layering is
`Runtime → Frontend → Compiler → Cli`, and `Frontend` has no reference to `Compiler`. Everything in
S2 flows from that single fact. `DialectLevel` is the mutable-state symptom: it is stored
independently on `CobolParserCoreBase.DialectLevel` (`:17`, `{get;set;}=85`) and
`Frontend.DialectLevel` (`:45`, `{get;init;}=85`), plus a third copy on the compiler's
`EditionContext` — three stores kept equal by hand-threading (`Frontend.cs:124` copies one into the
other). `Permissive` is threaded the same way (`Frontend.cs:83,92`).

**Parse-layer ↔ grammar coupling.** `EditionGateHints` (S1) is the tightest coupling: it depends on
(a) `CobolLexer.*` token-type constants — dozens, `:80-153`; (b) parser rule-**names as strings** in
`ruleStack` — `:100,105,110,115` etc.; (c) empirically observed error-token *positions*. Token
renames are caught by the compiler (constants), but rule-name renames and grammar restructures break
it **silently**. `CobolErrorStrategy` couples to grammar the same way (`IsInRule(ruleStack, "moveStatement")`
`:128`, etc.) but there the coupling is inherent to the heuristic and lower-risk.

**Stringly-typed diagnostic round-trip.** `CobolErrorStrategy.BuildMessage` formats `[{code}] {msg}`
(`:95`); `CobolErrorListener.SyntaxError` then string-parses the `[code]` back out (`:41-49`). A
structured `Diagnostic` is decomposed to a string and re-parsed across a single call boundary —
avoidable if the strategy handed the descriptor through.

**Mutable state.** Mostly well-contained: `CopyProcessor._nonPseudoTextFlagged` (`:22`) and
`EditionGates._col7Flagged/_wordFlagged` (`:122`) are "once-per-compilation" latches; `DiagnosticBag`
and `CobolErrorListener._errorCount` accumulate. `ConditionalCompilationProcessor` and
`ZeroTokenRewriter` are pure/static over their inputs. No global mutable state.

**Ungated cross-edition surface.** `ConditionalCompilationProcessor` implements a COBOL-2002 feature
(`>>DEFINE/IF`, ISO §7.3) but takes no `dialectLevel` and emits no diagnostic when used below 2002
(`Process :45`) — an edition-gating hole in the preprocessor (the four-editions invariant is not
enforced for CC directives). `TurnDirectiveProcessor` *does* gate (`:48-55`), so the two CC stages
are inconsistent.

---

## 5. Latent-bug risks

- **B1 (`SourceLocation.Line` convention is inconsistent across producers) · MEDIUM.**
  `SourceLocation.Line` is documented **zero-based** (`SourceLocation.cs:19-20`) and `ToString` renders
  `Line + 1` (`:36`). But `CobolErrorListener` passes ANTLR's **1-based** `line` straight in
  (`CobolErrorListener.cs:51`), so a syntax error on source line *N* prints as line *N+1*, while the
  preprocessor stages pass genuinely 0-based indices (`CopyProcessor.LineOf` returns 0-based `:64`;
  `TurnDirectiveProcessor.cs:47` passes `i`). Same field, two conventions → off-by-one error locations
  from the parser vs the preprocessor. Verify against goldens before "fixing" either side.
- **B2 (`Frontend.cs:103-105` raw throw crashes the compiler) · MEDIUM.** The TURN line-count-neutrality
  assertion throws `InvalidOperationException` from `Preprocess`, which runs *before* `LexAndParse` and
  is therefore **not** covered by the SLL `catch` — a real TURN bug becomes an unhandled crash rather
  than a diagnostic. `DESIGN-frontend-grammar.md §3.6` calls for converting it to a recorded internal
  diagnostic; that is correct.
- **B3 (S5 masks predicate crashes) · MEDIUM.** As in S5, `catch (Exception)` at `Frontend.cs:135` can
  turn a genuine predicate/lexer-action bug into a silent LL retry — a latent-defect amplifier.
- **B4 (NIST naive `.Replace` embedded-literal corruption) · LOW.** `NistPreprocessor` anchors only
  `XXXXX065/063/064` with token-boundary regex (`:74,87,132`) precisely because they can appear inside
  baselined literals; the other ~13 substitutions (`XXXXX055,058,051,052,056,057,082,083,068,084,081,090,091`)
  use unanchored `source.Replace` (`:53,64,173-209`) and would corrupt any longer literal that embeds
  the placeholder. NIST-only, so low blast radius today.
- **B5 (`EditionGateHints` furthest-token assumption) · LOW-MEDIUM.** The whole mechanism assumes the
  reported error token is at/near the gated keyword; the dual-path fallbacks (`:106-131`) exist because
  that assumption already fails for optional statement tails. Any prediction-path change can
  re-mis-attribute a construct (the dossier's duplicate-diagnostic case). Its replacement (predicate
  stamping) inherits the *same* furthest-token premise (see R1 below), so this risk migrates, it does
  not vanish.

---

## 6. Reorg suggestions (frontend-local; reconciled with the plan in §"ROADMAP GAP CHECK")

1. **Delete `EditionGateHints`; make the gate predicate stamp its own identity** (S1/S2). The forward
   fix the plan already specifies — the single highest-value change.
2. **Introduce a shared lowest assembly (`Cobol.Net.Editions`) below Frontend** so the registry,
   reserved words, and the ONE `EditionSeverityPolicy` are visible to the frontend, deleting the three
   metadata/severity copies (S2). Root-cause fix.
3. **Unify on one structured diagnostic model** (S3) and make `DiagnosticDescriptors` the single
   emission path — retire the raw-string-code calls in the preprocessor (S4) and `DiagnosticBag`'s raw
   `Report` overload.
4. **Narrow `Frontend.cs:135` to `ParseCanceledException`+`RecognitionException`** (S5/B3) and convert
   the `:103-105` throw to a diagnostic (B2).
5. **Single-source `DialectLevel`/`Permissive`** on one immutable edition value threaded through both
   the parser base and the pipeline (S2 mutable-state).
6. **Extract `CopyProcessor`'s tokenizer + REPLACING-operand parser** into a shared COBOL-text-word
   tokenizer used by CC too (S6/§4 tokenizer triplication).
7. **Either adopt `SourceText` for all stage line math or delete it** (S7); fix the `SourceLocation`
   line convention once (B1).
8. **Rename the 16 stale namespaces** to `CobolNet.Frontend.*` and fix the stale `Frontend.cs:16`
   banner (S8).
9. **Gate `ConditionalCompilationProcessor` on dialect** the way `TurnDirectiveProcessor` is (§4 hole).

---

## ROADMAP GAP CHECK

Cross-referencing this survey against `DESIGN-frontend-grammar.md`, `DESIGN-edition-framework.md`,
`PHASE-02-editions-assembly-diagnostic-registry.md`, and
`PHASE-04-frontend-consolidation-cst-facade.md`.

### What the plan addresses well (findings fully covered)
- **S1 (EditionGateHints reverse-engineering)** — squarely covered. `DESIGN-frontend-grammar §3.2`
  (self-identifying `Intro(ConstructId)`), `DESIGN-edition-framework §2.7`, and `PHASE-02 steps 7–8`
  (predicate stamping `Gate(int, GateId)` → delete `EditionGateHints`, run-in-parallel-then-delete).
  My B5 risk is explicitly acknowledged as R1 in both design docs — good.
- **S2 (metadata/severity triplication + `DialectLevel` triple-source)** — covered by the
  `Cobol.Net.Editions` leaf assembly (`DESIGN-edition-framework §2.1`, `PHASE-02 steps 1–3, 9`),
  `EditionInfo` single-sourcing (`§2.2`, P5), and `EditionSeverityPolicy` (`§2.3`, P2). `PHASE-02
  step 9` names both `ReferenceFormatProcessor.EditionGates` and `CopyProcessor` exactly.
- **S5/B3 (catch Exception)** — `DESIGN-frontend-grammar §3.5 (3)` narrows it precisely.
- **B2 (TURN throw)** — `DESIGN-frontend-grammar §3.6 (3)` converts it to a diagnostic.
- **S8 (stale namespaces/banner)** — P1 mechanical rename + `DESIGN §3.5(1)/§1.7`.
- **S7 tokenizer duplication (partial)** — `PHASE-04` shares the SUBSCRIPT literal fragment bodies
  and single-sources the *word set*.

### Gaps / corrections the plans should absorb

- **GAP-1 — The preprocessor's raw-string-code emission (S4) is under-scoped.** The plans focus the
  diagnostic-registry work on the **compiler** side (the COBOLNET0899 catch-all, reused `1533`,
  `EditionContext` `List<string>` — `PHASE-02 step 10`, P8). But the **frontend preprocessor** itself
  emits COBOLNET0718/0719/0875/0902/0903 as bare string literals with inlined message text and **no
  `DiagnosticDescriptor`** (`TurnDirectiveProcessor.cs:50,87,123,134,143`;
  `ReferenceFormatProcessor.cs:129,145,147`; `CopyProcessor.cs:38,40`). `PHASE-02 step 9` moves the
  *severity decision* for the 0902/0903 gates into `EditionSeverityPolicy`, but does **not** say these
  codes get descriptors, and it does **not** mention the 07xx/0875 TURN band at all. Correction: add to
  `PHASE-02 step 9/10` (or the frontend consolidation phase) an explicit task to give the COBOLNET07xx/
  08xx/09xx band descriptors and route them through the ONE emission path — otherwise the "one
  diagnostic model" exit criterion is met on the compiler side while the frontend keeps a second,
  stringly-typed style.

- **GAP-2 — `DiagnosticBag`'s two `Report` paths and the `[code]` string round-trip are not called
  out.** `DESIGN-frontend-grammar §3.8` designates `DiagnosticBag` the ONE collector and says its
  `Diagnostic` should carry `{DiagnosticDescriptor, SourceSpan, args}`, but neither design nor
  `PHASE-02` notes that (a) `DiagnosticBag` today has a descriptor overload **and** a raw
  `(code,severity,message)` overload (`DiagnosticBag.cs:17` vs `:38`) that must be unified, nor (b)
  that `CobolErrorStrategy` → `CobolErrorListener` encode/decode the code as a `[code]` **string**
  (`CobolErrorStrategy.cs:95` / `CobolErrorListener.cs:41-49`). The retarget in `DESIGN §3.9` ("build a
  structured `Diagnostic` rather than a pre-formatted string") is the right intent but is not wired
  into any PHASE step's file list. Correction: name the `CobolErrorStrategy`/`CobolErrorListener`
  string round-trip and the `DiagnosticBag` dual path as concrete tasks.

- **GAP-3 — `SourceText` is dead but no doc says so (S7).** `DESIGN-frontend-grammar §2` keeps
  `Common/` as-is; neither plan notes that `SourceText` (129 loc) has zero consumers while four stages
  re-roll line math, nor the `SourceLocation.Line` 0-based-vs-1-based inconsistency (B1). Correction:
  add a decision — adopt `SourceText` as the single line/column authority (and fix B1) or delete it.
  B1 is a latent correctness bug (parser error locations off by one vs preprocessor locations), not
  just cleanup, so it deserves an explicit line in a phase.

- **GAP-4 — `ConditionalCompilationProcessor` edition-gating hole is unaddressed (§4).** The plans
  treat the "five preprocessor stages and their order" as correct-and-preserved
  (`DESIGN-frontend-grammar §7`, `§3.6`) and only re-home the severity policy. None notes that CC
  (`>>DEFINE/IF`, 2002+) accepts directives at any `--std` with no diagnostic, while `TurnDirectiveProcessor`
  *does* gate — a four-editions-in-one inconsistency in the same folder. Correction: add a CC dialect
  gate (or explicitly document why CC directives are edition-neutral) to the edition-framework or a
  frontend phase.

- **GAP-5 — `CopyProcessor` multi-responsibility size (S6) is not in scope anywhere.** `PHASE-04`
  bounds itself to the word set, SUBSCRIPT fragments, and the `Cst/` façade, and explicitly does **not**
  touch the preprocessor. No phase proposes extracting `CopyProcessor`'s embedded text-word tokenizer /
  REPLACING-operand parser, which is the same tokenization concept implemented a third time (lexer +
  CC + COPY). Correction: either add a "shared COBOL text-word tokenizer" task or record a conscious
  decision to leave the three tokenizers separate (the COPY/REPLACE text-word rules differ subtly from
  lexer tokens, so this may be legitimate — but it should be a stated decision, not an omission).

- **GAP-6 — NIST naive `.Replace` corruption risk (B4) is uncatalogued.** `NistPreprocessor` is
  test-corpus glue and out of the ISO frontend proper, so this is low priority, but no doc records the
  latent embedded-literal hazard on the ~13 unanchored substitutions. Minor: add a follow-up ledger
  entry, or migrate all X-card substitutions to the token-boundary-anchored form already used for
  065/063/064.

**Net:** the plan's *structural* backbone is sound and covers the two biggest smells (S1, S2)
thoroughly, with the correct root-cause fix (a shared `Editions` assembly + forward predicate
stamping). The gaps are all on the **diagnostics** leg: the frontend preprocessor's raw-string-code
emission (GAP-1), the `DiagnosticBag` dual path + `[code]` round-trip (GAP-2), and the dead
`SourceText`/line-convention bug (GAP-3) are real and currently fall between the compiler-focused
`PHASE-02 step 10` and the façade-focused `PHASE-04`. The edition-gating hole in CC (GAP-4) is the one
correctness-relevant item the "preserve the five stages" stance overlooks.
