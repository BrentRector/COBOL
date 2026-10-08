# DESIGN — FLAG-02 / FLAG-14 migration-flagging directives (§7.3.14 / §7.3.15)

> **Status: IMPLEMENTED (P13 Wave D).** All 17 option-detectors (FLAG-14 b–m, FLAG-02 b–f) + the 2 directive-word
> edition gates are landed; `FlagConformancePass` + the frontend collection + the two frontend-inline options are the
> shipped subsystem. Canonical deep-dive for the `>>FLAG-02` (§7.3.14) and `>>FLAG-14`
> (§7.3.15) migration-incompatibility flagging directives — the ONE shared flagging subsystem (a
> frontend-collected `FlagState`, a dedicated post-bind `FlagConformancePass`, and the two frontend-inline
> option detectors that have no bound residue). Design SSOT for the Wave-D "real FLAG-14/FLAG-02 flagging"
> item and VCR **Table 5** (rows 98, 100–113 + the directive-word rows 11/64/91/96/115). Keep CURRENT
> (describes the compiler as built); the how-we-got-here narrative lives only in `DEVLOG.md`.

## 0. What the directives are, and the master constraint

Both directives instruct the implementor's **warning mechanism** (each GR1: *"The implementor shall provide a
warning mechanism that flags the incompatibilities potentially affecting existing programs for the selected
option"*) to flag source whose **behavior differs between two editions**:

* `>>FLAG-02` (§7.3.14) — 2002 ↔ 2014 incompatibilities. **Obsolete** in 2023 (§7.3.14.1 NOTE) but still
  recognized and still required to flag.
* `>>FLAG-14` (§7.3.15) — 2014 ↔ 2023 incompatibilities. GR1's NOTE points at **Annex E.2** (*"Substantive
  changes potentially affecting existing programs"*) as the complete change list.

**Master constraint — flagging is DIRECTIVE-driven, not edition-driven.** A flag fires because a `>>FLAG-nn
OPTION ON` directive is in effect at the construct's source position, **regardless of `--std`**, and is
**always a Warning** (a migration aid; it never fails the compile). This is the axis that separates flagging
from edition conformance (`VersionConformancePass`, keyed on `_edition.Year`) and is the reason flagging is a
**separate pass**, not a bolt-on (§2, D1).

### 0.1 Directive syntax (settled from the corrected figure notes — no PDF render needed)

Both general formats (spec figures at §7.3.14.2 / §7.3.15.2) wrap the option list in **§5.2.6.4 choice
indicators** (the `|…|` bars the OCR dropped, restored in the `.md` figure notes 2026-07-19):

```
>> FLAG-02  { ALL | { |EC-PROGRAM-EXCEPTIONS | I-O-STATUS-07 | MOVE-TO-SAME-NAME
                       | RANGE-EXCEPTION-FOR-INDEX | TERMINATE-WITH-VARYING| } }  { ON | OFF }
>> FLAG-14  { ALL | { |COMPILE-TIME-ARITHMETIC-EXPRESSIONS | EVALUATE | I-O-DECLARATIVE
                       | I-O-STATUS-04 | I-O-STATUS-07 | NUM-ED-ZERO-FIGCONST | READ-PREVIOUS
                       | REF-MOD-ZERO-LENGTH | VALUE-EDITING | VALUE-FIG-CON-LENGTH | VALUE-ZERO
                       | WRITE-END-OF-PAGE| } }  { ON | OFF }
```

* One directive names **`ALL`** or **one-or-more option words** (each at most once, **any order**; a repeated
  word is a syntax error, COBOLNET1622, §5.2.6.4), then `ON | OFF`. In BOTH directives the printed format leaves
  `ALL` and `ON` un-underlined (rendered ISO PDF pp. 100 and 102): optional words (§5.2.3), so each is the
  DEFAULT of its braces (§5.2.6.3) and is selected when the other alternative is not written. A bare directive
  is `ALL ON`, a lone `OFF` is `ALL OFF`, a lone option list is `ON`, and `ON`/`OFF` is the last word
  (kb/Work PB1374). One parse rule for both directives; only the option vocabulary differs.
* **SR1 (placement, both):** only between clauses outside the procedure division, and between statements
  within it — i.e. a free-standing directive line at a clause/statement boundary.
* **State semantics (GR2/GR3/GR5, both):** default **OFF** for every option (GR5). `ON` is **sticky-forward**
  per option until end-of-compilation-group, an **`ALL … OFF`** (turns off *all* options), or that option's own
  `OFF` (GR2/GR3). This is the same source-line-fold discipline as `>>TURN` / `>>REF-MOD-ZERO-LENGTH`.

### 0.2 Directive-word edition gating (a construct in its own right)

The directive *words* were added at different editions (Annex E.2 item 5 lists `FLAG-14` among the words new to
2023). This gates the DIRECTIVE, distinct from the flagging it drives:

* `>>FLAG-14` — a **2023 introduction** (below 2023 the word is a compilation-variable name, not a directive):
  introduction gate **COBOLNET0900** below 2023, via `ConstructRegistry.Check` (the `>>REF-MOD-ZERO-LENGTH`
  precedent). New registry construct `Flag14Directive2023`.
* `>>FLAG-02` — introduced with the **2014** standard (it flags 2002↔2014); **obsolete — NOT removed — at 2023**
  (§7.3.14.1 NOTE, PDF-confirmed p.100: *"an obsolete element in this Working Draft … to be deleted from the
  **next** edition"*). Per **§4.2.13** an implementation *shall support* obsolete elements and *shall provide a
  warning mechanism* to flag them — so at 2023 it must still **compile** and carry the **0903 obsolete warning**,
  never a `removedIn`/0902 rejection (that would refuse legal, still-supported source). Registry row
  `flag-02-directive-2014` = `introducedIn:2014, obsoleteIn:2023` (0900 below 2014, clean 2014–2022, 0903 at 2023).
  Recognized-and-flagged even when obsolete — never silently dropped. (F.2, which §4.2.13 says lists obsolete
  elements, does not name FLAG-02 — an ISO gap; the §7.3.14.1 NOTE is the direct authority.)

## 1. The verified census (adversarially reviewed 2026-07-21)

Every row: the GR4 sub-rule (verbatim-checked), the construct to detect, where it is visible today, and the
implementation increment. `[F]` = frontend-inline (no bound residue); `[B]` = bound/data walk in
`FlagConformancePass`; difficulty and any new infrastructure are noted.

### 1.1 FLAG-14 (§7.3.15.4 GR4 a–m — ALL + 12 options)

| Opt | Construct (GR4) | Visible at | Incr | Notes |
|-----|-----------------|-----------|------|-------|
| a ALL | fan-out to b–m | directive parse | 0 | sets every FLAG-14 option; `ALL OFF` is the GR2 reset |
| b COMPILE-TIME-ARITHMETIC-EXPRESSIONS | a compile-time arithmetic expr with a real operator, at EVERY site §7.3.6.1 lists: the DEFINE / EVALUATE / DISPLAY directive operands, a constant conditional expression, and a CONSTANT entry's arithmetic-expression-1 | `[F]` `ConditionalCompilationProcessor` (the directive operands) + `[B]` `FlagConformancePass.VisitConstantEntryBody` (the constant entry) — both ask the ONE predicate `FlagOptions.IsFlaggableCompileTimeArithmetic` | 2 | E.2 item 6. Guard on operator present (sole literal not flagged) |
| c EVALUATE | a `>>EVALUATE` directive with both a WHEN and a WHEN OTHER | `[F]` `ConditionalCompilationProcessor` Evaluate/When frames | 2 | E.2 item 8. NOT the EVALUATE statement |
| d I-O-DECLARATIVE | an INVALID-KEY-capable I-O stmt (WRITE/START/random-READ on a keyed file; REWRITE except on a sequential-organization file or a relative file in sequential access, §14.9.35.3 SR2; DELETE except on a file in sequential access mode, §14.9.10.3 SR2) without INVALID KEY, or a sequential READ without AT END, while a USE INPUT/OUTPUT/I-O/EXTEND declarative is present | `[B]` DONE — `VisitRead/Write/Rewrite/Delete/StartStatement` + `unit.Bound.Declaratives` modes + `_currentData.Files` org and access mode through `AdmitsInvalidKey` | 4 | E.2 item 19. ⚠ A FLAG, NOT A GATE: §7.3.15.4 GR4 d) is a normative FLAGGING obligation over Annex E.2's list, so the construct is flagged even though WiseOwl COBOL's own ANSWER for it does NOT vary by edition — the open-mode USE tier runs at 85/2002/2014/2023 alike (kb/Work PB344; VERSION_CHANGE_REFERENCE rows 25/26 are `ref-only`) |
| e I-O-STATUS-04 | a reference testing a FILE STATUS item for `'04'` — a relation (including the abbreviated combined relation `FS = "00" OR "04"`), a level-88 whose VALUE is `'04'`, or an EVALUATE subject/object pair (value or partial-expression object) | `[B]` DONE — `FlagConformancePass.FileStatus.cs` (`VisitCondition` / `VisitEvaluateStatement`) + FILE-STATUS name/88 sets from `unit.Data.Files[]` | 4 | E.2 item 15 |
| f I-O-STATUS-07 | a reference testing a FILE STATUS item for `'07'` — a relation, or a level-88 whose VALUE is `'07'` | `[B]` DONE — same detector as e (only the literal differs) | 4 | E.2 item 16 |
| g NUM-ED-ZERO-FIGCONST | figurative ZERO in a VALUE clause of a numeric-edited item (a numeric picture with BLANK WHEN ZERO is numeric-edited, §13.18.8.4 GR2) | `[B]` `FlagConformancePass.VisitDataDescriptionEntry` + `PictureAnalyzer` (BLANK WHEN ZERO and the unit's CURRENCY SIGN set threaded to it) | 1 | E.2 item 28. **Same predicate as l** — one detector serves both |
| h READ-PREVIOUS | a `READ … PREVIOUS` | `[B]` `BoundKeyedRead` Previous (already matched VCP:200) | 0 | E.2 item 22. The first end-to-end slice |
| i REF-MOD-ZERO-LENGTH | a ref-mod where the `>>REF-MOD-ZERO-LENGTH` directive is **not explicitly** ON/OFF **and** EC-BOUND-REF-MOD is on | `[B]` `RefModPlace` + `RefModZeroLengthState` (tri-state ext.) + TurnState | 3 | E.2 item 23. Needs tri-state `RefModZeroLengthState` + EC read |
| j VALUE-EDITING | a numeric-edited item whose VALUE is a literal with no editing symbols | `[B]` `GateData`/`DataItem.RawValue` + Pic | 1 | E.2 item 29 |
| k VALUE-FIG-CON-LENGTH | a figurative constant in a VALUE clause of an item with no specified length | `[B]` `GateData`/`DataItem` (fig word + length model) | 1 | E.2 item 11. GR4 spells it `VALUE-FIG-CON-NO-LENTH` (typo); the accepted word is the figure's `VALUE-FIG-CON-LENGTH` |
| l VALUE-ZERO | a numeric-edited item whose VALUE specifies figurative ZERO | `[B]` = same as g | 1 | E.2 item 28. Deduped with g |
| m WRITE-END-OF-PAGE | a WRITE that *allows* END-OF-PAGE (file has LINAGE) but omits the AT EOP phrase — a WRITE with only NOT AT END-OF-PAGE omits it (§14.9.51.3 SR19 names the two phrases separately) | `[B]` `VisitWriteStatement` + File LINAGE, `PhraseBlocks.HasOnBranch` | 1 | E.2 item 19 family (AT-EOP default); GR4 m normative |

### 1.2 FLAG-02 (§7.3.14.4 GR4 a–f — ALL + 5 options)

| Opt | Construct (GR4) | Visible at | Incr | Notes |
|-----|-----------------|-----------|------|-------|
| a ALL | fan-out to b–f | directive parse | 0 | |
| b EC-PROGRAM-EXCEPTIONS | a `>>TURN` for EC-ALL/EC-PROGRAM/EC-PROGRAM-ARG-OMITTED/EC-PROGRAM-NOT-FOUND in a source element (§3.164) that calls any function or invokes any method | `[B]` DONE — `TurnState.DirectiveLinesNaming` (post-walk) + the SOURCE-ELEMENT model (`_elements`) with calls from `VisitFunctionCall`/`VisitInvokeStatement`/`VisitInlineMethodInvocation` and the binder-recorded `DataBinder.ActivationSites` | 4 | GR4 normative (no Annex-E re-item); CALL of a program is NOT a trigger |
| c I-O-STATUS-07 | a CLOSE with WITH NO REWIND or the UNIT phrase | `[B]` `BoundClose` — **needs a NoRewind model bit** (`BoundCloseKind` has ReelUnit but not NoRewind) | 3 | E.2 item 16. `CobolIO.g4 closeOption` already parses both |
| d MOVE-TO-SAME-NAME | a MOVE whose send/receive resolve to the SAME DDE, and (1) the OPERANDS are category alphanumeric-edited (a reference-modified operand is alphanumeric, §8.4.3.3.4 GR6 a), or (2) a subordinate OCCURS…DEPENDING whose DEPENDING item is subordinate to that DDE | `[B]` parse-tree visitor + per-unit `ReferenceResolver.FindItem` (same-DDE = `ReferenceEquals`) | 3 | GR4 normative (no Annex-E re-item) |
| e RANGE-EXCEPTION-FOR-INDEX | an index-assignment/arithmetic SET whose receiver is an **index-name** (NOT a class-index DATA item — §14.9.39.4 Format-1 GR2b copies it unchanged, no range check), when EC-RANGE-INDEX checking is enabled | `[B]` parse-tree visitor: `DataBinder.IndexFields` + `TurnState` | 3 | GR4 normative |
| f TERMINATE-WITH-VARYING | a TERMINATE of a report whose RD contains a VARYING clause | `[B]` `ReportWriterBinder` BindTerminate + ReportModel `Varyings` | 1 | GR4 normative |

## 2. Structural decisions (the singular-pattern end-state)

**D1 — a dedicated `FlagConformancePass`, NOT a bolt-on `VersionConformancePass`.** Flagging is an orthogonal
trigger axis (user directive-state vs `--std`), always Warning, and two of its detectors have no bound-tree home
at all (b, c are frontend-only). Bolting per-case `if (flagState.IsOnAt…)` onto VCP's `GateStatement` switch
would dilute that pass's documented single-responsibility ("sole owner of edition gating", the two-arm
disjointness invariant) and its "one policy = `ConstructRegistry.Check` funnel" identity. `FlagConformancePass`
is a sibling post-bind pass in `Cobol.Net.Compiler/Validation`, invoked from `BinderDriver` right after the
`BindPipeline.GroupTail()` loop with `(GroupBindContext, FlagState, sink)`.

**D2 — a PARSE-TREE visitor (the source-line reason), reusing the generated ANTLR base visitor.** The flag fold
is **line-sensitive** (GR2: a flag applies to the text *following* the directive), so every detector needs its
construct's source line. `BoundStatement` does **not** carry a uniform source line — only a handful of nodes hold
a `SourceLine` (for DEBUG-LINE), and e.g. `BoundTerminate` has none — so a bound-tree walk cannot anchor the
fold. The **parse tree** carries `ctx.Start.Line` on every node, and because `FlagDirectiveProcessor` collects
events on the FINAL preprocessed text, those `Start.Line`s are directly comparable to `FlagEvent.Line` (the same
basis TurnState/RefModZeroLength use). Therefore `FlagConformancePass` is a `CobolParserCoreBaseVisitor<object?>`
over `group.Tree` — the SAME traversal mechanism VCP's `ParseArm` uses (the generated, drift-proof visitor; no
bespoke switch — `feedback_path_a_leverage_tooling`). Options whose trigger is **syntactic** (READ PREVIOUS,
CLOSE NO REWIND/UNIT, WRITE-without-EOP, SET-index, the VALUE-clause data options, MOVE shape) are decided from
the parse node directly; options needing a **resolved fact** (TERMINATE's report has a VARYING clause; a MOVE's
operands share a DDE; a condition tests a FILE STATUS item) look that fact up **by name** in the resolved models
reachable from `GroupBindContext` (`Units[].Data` forest, `.Reports`, `FilesByName`) — a cheap dictionary hit,
never a re-run of operand binding.

**D3 — ONE FlagState, ONE FlagOption catalog, ONE directive-line parser.** A single `FlagState`
(`Cobol.Net.Compiler/Binding`, cloned from `RefModZeroLengthState` and extended per-option) folds the toggle
events to `IsOnAt(siteLine, FlagOption)`; a single `FlagOption` enum + descriptor table enumerates all 17 real
options across both directives (ALL is a parse-time fan-out, not a stored option); a single `FlagDirectiveLine`
parser splits `>>FLAG-nn [ALL | opt…] {ON|OFF}` into `(FlagDirective, ISet<FlagOption>, bool On)` and is reused
by BOTH collection sites (§3).

**D4 — TWO collection sites, forced by construct-visibility across pipeline stages (not a fork).**

* **Bound options** (all `[B]` rows) need events **line-anchored to final tokens**, so they are collected
  **post-COPY** by a new `FlagDirectiveProcessor` on the FINAL text (the `>>TURN` / `>>REF-MOD-ZERO-LENGTH`
  stage template; H3 line-count-preserving; introduction-gates the directive word). Its events build the
  compiler-side `FlagState`.
* **Frontend-only options b, c** are CONSUMED by the conditional-compilation stage (pre-COPY) and never reach
  the bound tree, so they MUST be flagged inside `ConditionalCompilationProcessor`, which already walks every
  `>>` line in order. It tracks FLAG on/off state as it scans (a `FlagScanState` using the SAME
  `FlagDirectiveLine` parser) and emits b/c inline via the `DiagnosticBag`.

Both sites reuse the ONE parser + ONE FlagOption catalog. The `>>FLAG` lines survive the CC stage via a
`leaveFlagDirectives` arm (mirroring `leaveTurnDirectives`), so the post-COPY `FlagDirectiveProcessor` collects
and blanks them. **Copybook-internal `>>FLAG` directives ride the CC-in-COPY fix** (§7.2.1; a separate Wave-D
item) — a documented limitation shared by every copybook-internal directive today, not specific to FLAG.

**D5 — diagnostics: two catalog codes, per-option identity.** Two Warning descriptors —
`Flag02Warning` (**COBOLNET1620**) and `Flag14Warning` (**COBOLNET1621**) — are the channel; each emitted
`EditionDiagnostic` carries the **per-option** `ConstructId` (suppress-key), `Message` (naming the option + the
behavior change), and `Citation` (the exact `§7.3.1x.4 GR4 <letter>` + Annex-E item). This gives per-option
citation precision and suppressibility while keeping the band to two codes (the reflected `All` auto-lists them;
the `EveryEmittedCode_IsACatalogDescriptor` drift test is satisfied). Severity is hard-coded
`EditionSeverity.Warning` (NOT `EditionSeverityPolicy`, which is edition-derived).

**D6 — VCR anchoring.** The directive-WORD rows (11/64/91/96/115) anchor to the new `constructs.json` rows
`Flag14Directive2023` / `Flag02DirectiveObsolete2023` (`<!-- gate:… -->`, genuine edition gates). The per-OPTION
rows (98, 100–113) are directive-driven, not edition gates, so they carry `<!-- ref-only -->` pointing at
`FlagConformancePass` once their detector lands (confirm against `VcrDriftTests`; do NOT invent a
`constructs.json` edition row for a non-edition-gated behavior).

## 3. The three seams (each an exact copy of a proven template)

1. **COLLECT** — `record FlagEvent(int Line, FlagDirective Which, bool On, IReadOnlyList<FlagOption> Options)`
   (Options empty ⇒ ALL). New `FlagDirectiveProcessor` (`Cobol.Net.Frontend/Preprocessor`), a line-by-line
   clone of `RefModZeroLengthDirectiveProcessor` (blank-not-delete, H3-preserving). ⛔ The introduction gate is
   NOT here: kb/Work PB725 moved every directive's edition check to the ONE point
   `ConditionalCompilationProcessor` recognizes a `>>` word, so this stage takes no dialect at all and
   `flag-02-directive-2014` / `flag-14-directive-2023` gate through their `directiveWords`. Wired by
   `Frontend.LeftDirectives` (which answers only WHICH STAGE OWNS THE LINE; FLAG-02/14 are catalog rows either
   way, so legacy callers still consume them) and a new stage call in `Frontend.Preprocess` after the
   REF-MOD-ZERO-LENGTH stage, exposing `Frontend.Directives.FlagEvents`. The frontend-only b/c flags are emitted here-adjacent
   inside `ConditionalCompilationProcessor` from its own in-scan `FlagScanState`.
2. **THREAD + FOLD** — `FlagState` (`Cobol.Net.Compiler/Binding`): `Build(events)` + `IsOnAt(int siteLine,
   FlagOption)` folding "last toggle strictly before the site wins, default OFF" per option, with ALL fan-out
   and `ALL OFF` reset. Threaded `frontend.Directives` (the ONE `DirectiveResults` record, kb/Work PB65) →
   `CompilerDriver` (`emitter.Bind(tree, edition, frontend.Directives)`) → `CSharpEmitter.Bind` →
   `BinderDriver` (`FlagState.Build(flagEvents)`), carried on `BindSession` alongside `RefModZeroLength`.
3. **EMIT** — `FlagConformancePass.Run(group, flagState, sink)` invoked right after `VersionConformancePass.Run`
   in `BinderDriver`, per-option detector methods emitting
   `sink.Report(new EditionDiagnostic(code, EditionSeverity.Warning, constructId, msg, where, cite))` gated by
   `flagState.IsOnAt(node.Line, option)`.

## 4. Increment plan (ONE option-or-group per commit, conformance test + wave-local gate each)

* **Incr 0 — CORE + first slice (both directives, both codes, purely syntactic).** `FlagOption`/`FlagDirective`
  + option catalog; `FlagDirectiveLine` parser; `FlagEvent` + `FlagDirectiveProcessor` (+ `leaveFlagDirectives`
  wiring + `Frontend.FlagEvents`); `FlagState`; thread through the drivers; `FlagConformancePass` (the ANTLR
  parse-tree visitor) with two syntactic detectors — **FLAG-14 h READ-PREVIOUS** (`readDirection().PREVIOUS()`)
  and **FLAG-02 c I-O-STATUS-07** (`closeOption` = `UNIT` or `NO REWIND`; parse-visible, so NO `BoundClose`
  model change is needed — a simplification over the original Incr-3 plan). Catalog 1620 (Flag02Warning) + 1621
  (Flag14Warning), both exercised. Goldens: `>>FLAG-14 READ-PREVIOUS ON` + a READ PREVIOUS ⇒ warning (OFF ⇒
  none); `>>FLAG-02 I-O-STATUS-07 ON` + a CLOSE WITH NO REWIND ⇒ warning. Directive-word introduction gate +
  `constructs.json` rows land here too (or the immediately-following Incr 0b if kept tight).
* **Incr 1 — the data/VALUE + report/WRITE options** (parse arm + resolved lookups), delivered as sub-increments:
  * **1a (DONE)** g NUM-ED-ZERO-FIGCONST + l VALUE-ZERO — `VisitDataDescriptionEntry`; numeric-edited via the ONE
    `PictureAnalyzer` (discard edition); figurative ZERO via `FirstDescendant<FigurativeConstantContext>`.
  * **1c (DONE)** m WRITE-END-OF-PAGE (WRITE + `FileModel.Linage` + no `writeAtEndOfPage`) + FLAG-02 f
    TERMINATE-WITH-VARYING (TERMINATE + a report whose fields carry `Varyings`) — source-name lookups built in
    `Run` from `GroupBindContext.Units[].Data`.
  * **1b (DONE for j)** j VALUE-EDITING — a numeric-edited VALUE that is a LITERAL (not figurative) with no
    editing symbols (§13.18.63 SR6/SR11 + E.2 item 29: numeric literals get editing auto-supplied, alphanumeric/
    national literals now require it). "Editing symbols" scanned from the literal via the unambiguous insertion set
    `{space / , . + - $ *}` + trailing CR/DB; `0`/`B` insertions are a documented rare false-negative.
  * **1b (DONE) k VALUE-FIG-CON-LENGTH** — a figurative-constant VALUE (any figurative, via
    `FirstDescendant<FigurativeConstantContext>`) on a data item with **no specified length** (§7.3.15.4 GR4 k;
    E.2 item 11). "No specified length" is decided precisely from the parse: **no PICTURE clause**, **no
    length-implying USAGE** (`UsageGivesNoLength` — DISPLAY/absent has none without a PICTURE; COMP-*/INDEX/POINTER/
    float/binary families imply one — CLI-verified that `USAGE DISPLAY VALUE SPACE` IS flagged and `USAGE COMP-2
    VALUE ZERO` is NOT), and **not a group** (`HasSubordinates` — the immediately-following sibling entry is a real
    subordinate, level 2–49; a group's figurative VALUE is filled to the subordinates' length so its length IS
    specified, §13.18.63 SR13). Restricted to real data levels (1–49, 77) — a level-88 condition-name reaches
    `VisitDataDescriptionEntry` via `valueClause` and is excluded (`IsRealDataLevel`). k reaches ANY item (not just
    numeric-edited); g/l/j remain numeric-edited-gated.
* **Incr 2 (DONE)** the frontend-inline options b COMPILE-TIME-ARITHMETIC-EXPRESSIONS + c EVALUATE, emitted in
  `ConditionalCompilationProcessor` (these are consumed at the CC stage, never reach the bound tree). A
  `FlagScanState` tracks FLAG-14 ON/OFF as the stage scans (still leaving the >>FLAG line for the post-COPY
  bound-option `FlagState`); `DirectiveDiag.FlagWarn` emits with the same code/message shape as
  `FlagConformancePass`. c: the `Frame` records `>>WHEN`/`>>WHEN OTHER` presence, flagged at `>>END-EVALUATE`. b:
  `EvaluateOperandText`/`EvaluateCceText` call `diag.FlagArithmetic` on the parsed fragment (a real addOp/mulOp,
  evaluated context only).
* **Incr 3 — the state-coupled options (COMPLETE).**
  * **i REF-MOD-ZERO-LENGTH** — tri-state `RefModZeroLengthState` extension + EC-BOUND-REF-MOD `TurnState` read.
  * **d MOVE-TO-SAME-NAME** — same-DDE by **exact `ReferenceEquals`** of the two operands resolved with
    `ReferenceResolver.FindItem` (the description entry — subscript/ref-mod-agnostic), then (1) alphanumeric-edited
    `{ Category: PicCategory.Alphanumeric, EditMask: not null }` (there is **no** `PicCategory.AlphanumericEdited`
    member) or (2) `OdoModel.TableUnder(item) is { OccursSpec.Depending: {} dep } && OdoModel.IsWithin(dep, item)`.
    Both MOVE forms; a literal/function sender is skipped. Introduced **per-unit name resolution** to the pass — a
    `VisitProgramUnit` save/restore that selects the current unit's `DataBinder`/`ReferenceResolver` from a
    `ProgramUnitContext→BoundUnit` map (§2 D2's "resolve by name in the models" made scope-exact, not a global merge).
  * **e RANGE-EXCEPTION-FOR-INDEX** — reuses d's `_currentData`: `VisitSetToValueStatement` (Format 1) +
    `VisitSetIndexStatement` (Format 2) flag once when a receiver's base name is in the current unit's
    `DataBinder.IndexFields` (an INDEXED BY **index-name**) AND `_turn.Enabled("EC-RANGE-INDEX", null, line)`.
    **⛔ Only an index-NAME receiver range-checks:** §14.9.39.4 Format-1 **GR2b** copies a class-index DATA item
    (USAGE INDEX) receiver UNCHANGED (no EC-RANGE-INDEX), and Format-2 GR4a checks only index-name-3 — so a USAGE
    INDEX data-item receiver is **NOT** flagged (matching only `IndexFields` names realizes this exactly, and the
    shared SET grammar's pointer / capacity / dynamic-length / object receivers are intrinsically excluded).
  * **Scope note (d/e):** an OO **METHOD body** MOVE/SET has no `BoundUnit` entry, so `_current*` stays null and the
    operand is not resolved — a documented advisory **false-negative** (never a false-positive). A contained program
    DOES have one: `_unitByIdentification` keys every program unit (outermost and contained) on its IDENTIFICATION
    DIVISION node, the one node a contained program shares with the synthetic `programUnit` its binder is built over
    (`BinderDriver.Reparent` adopts the children).
* **Incr 4 — the new-analysis options.**
  * **e/f I-O-STATUS-04/07 (DONE)** — one shared detector (`FlagConformancePass.FileStatus.cs`) flags a reference to
    a FILE-STATUS item that tests for '04'/'07', in every place a condition compares: a **relation** (a
    `comparisonExpression` with 2 `comparisonOperand`s + a `comparisonOperator`, the FILE-STATUS item on either side
    and the nonnumeric literal '04'/'07' on the other); an **abbreviated combined relation** (`FS = "00" OR "04"`,
    `AND NOT = "04"` — §8.8.4.12.4 GR1 inserts the last preceding stated subject, so the scan of a `condition`'s tiers
    carries the leftmost operand of the last relation in source order, and a parenthesised condition restarts it); a
    bare **level-88 condition-name** whose singleton `Condition88.Values` strips to '04'/'07' on the FILE-STATUS
    item; and an **EVALUATE** selection subject/object pair (§14.9.13.3 SR2 pairs them by position, SR8 reads a
    partial expression as preceded by its subject). A range (`WHEN "00" THRU "09"`) is not a test FOR '04': its
    membership depends on the collating sequence. The FILE-STATUS name set + the two 88-name sets are built in `Run` from
    `unit.Data.Files[]` (`FileModel.FileStatusItem.CobolName` + its `Own88s`) — the m/f global-name-set idiom (a
    role fact, collision-tolerant), NOT the scope-sensitive per-unit resolution d/e use. Operand navigation reuses
    the canonical `ConditionBinder.SoleDataRef` (a lone reference, not an expression) + `valueOperand()
    .nonNumericLiteral().STRINGLIT()` — no bespoke tree walk (`feedback_path_a_leverage_tooling`).
  * **d I-O-DECLARATIVE (DONE)** — two rules: (1) a statement that CAN be specified with an INVALID KEY phrase
    (`AdmitsInvalidKey`: WRITE/START or a random READ on a KEYED file — Organization RELATIVE/INDEXED, the only ones
    that raise an invalid-key condition; REWRITE unless the file is relative in sequential access mode,
    §14.9.35.3 SR2; DELETE unless the file is in sequential access mode, §14.9.10.3 SR2)
    lacking its INVALID KEY phrase, when the unit has ANY open-mode USE declarative; (2) an AT-END-capable READ (a
    sequential retrieval — a NEXT/PREVIOUS direction, or AccessMode Sequential) lacking AT END, when the unit has an
    INPUT or I-O declarative. Per-unit modes are read from the BOUND model (`unit.Bound.Declaratives` —
    `BoundDeclarative.ModeIndex` is the `FileOpenMode` ordinal; null for a file-name / Format-3/4 declarative, which
    GR4 d does not consider), set in `VisitProgramUnit` (order-independent, so it never depends on the declaratives
    preceding the body). Per-statement file classification is from `_currentData.Files` (WRITE/REWRITE map their
    record name to its file via `FileModel.Records`). A file that does not resolve stays unflagged.
  * **FLAG-02 b EC-PROGRAM-EXCEPTIONS (DONE)** — a `>>TURN` for an EC-PROGRAM-family exception (EC-ALL, EC-PROGRAM,
    EC-PROGRAM-ARG-OMITTED, EC-PROGRAM-NOT-FOUND) is flagged when its **source element** (§3.164: a source unit
    excluding its contained source units — a program, a function, a contained program, a class, an interface, a
    method) **calls a function** (a `functionCall` node, a keyword-omitted function-identifier §8.4.3.2.3 SR2, a
    function inside a subscript) or **invokes a method** (an `invokeStatement` / `inlineMethodInvocation` / a property
    reference §8.4.3.9, whose accessor is a method) — a CALL of a program is deliberately NOT a trigger (GR4 b names
    only function and method). The activations with no syntactic marker are not re-derived from syntax: the BINDER
    records them where it decides them (`DataBinder.ActivationSites`, identity-keyed parse nodes and subscript
    tokens; sites `IntrinsicBinder.KeywordOmittedFunction`, `ReferenceResolver.IsFunctionBearing`,
    `ReferenceResolver.ResolveObjectProperty`) and the pass reads that fact back. Because it flags a DIRECTIVE (a frontend `>>TURN`,
    not a parse node), it runs POST-walk: `TurnState.DirectiveLinesNaming` exposes the `>>TURN` lines naming any
    family name (ON/OFF alike; the raw events store the canonical name as-written, so a level-2 `EC-PROGRAM` matches
    without expansion); the walk opens a `SourceElement` (its own line span + whether it calls/invokes) per source unit
    and `SourceElementOf` maps each directive line to the INNERMOST element whose span contains it — a directive before
    an element's IDENTIFICATION DIVISION belongs to the element that follows (§7.3.25.4 GR6: a TURN governs the statements
    that follow). Each qualifying directive is flagged AT ITS OWN LINE (`FlagAtLine`), so two directives are two
    located warnings.
  All 17 option-detectors are landed — the subsystem is COMPLETE. Wave I merges with the rest of the Wave-D work.

## 5. Testing

Per `feedback_goldens_ship_with_the_feature`: every option ships a positive golden (ON ⇒ the warning) in the
SAME commit; the wave-local gate is fresh build + characterization + the FLAG unit/corpus filter + a CLI probe.
Because flagging is a Warning (never fatal), a golden asserts on stderr/warning text, not exit code. The
comprehensive gate (full Conformance + the NIST guard) runs once pre-merge; the GnuCOBOL differential is a triage
net (flagging adds warnings, never rejects — expect zero accept/reject flips).
