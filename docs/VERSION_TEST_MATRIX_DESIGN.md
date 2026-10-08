# Version Test Matrix — Design (testing WiseOwl COBOL as N per-edition compilers)

> **STATUS BANNER — DESIGN + live implementation record. The (construct × edition) test matrix is in place: the
> harness scaffold (`VersionMatrixTests` over (construct × edition) + the INV-1 continuity `[Theory]`), the default
> edition flipped to COBOL-2023, the seeded catalogue + `EditionHarness`, and the full INV-1 continuity sweep. Edition
> gating is implemented and enforced by the two-arm `VersionConformancePass` (the SOLE edition gate — §8 / the
> edition-gating implementation section). The negative corpus + obsolete/archaic/new-reserved-word flagging (Phase 3) is in build-out.
> LIVE design reference.**
>
> **Premise (owner, 2026-06-09):** *"Conceptually we have a different COBOL compiler for each ISO edition (1985 /
> 2002 / 2014 / 2023). We should test it as such."* A single corpus run at one dialect cannot validate that. This
> document designs the **(construct × target-edition) test matrix** and the test-infrastructure rework to drive it,
> sourced from [`VERSION_CHANGE_REFERENCE.md`](VERSION_CHANGE_REFERENCE.md). Companion memories:
> `feedback_four_editions_one_compiler`, `feedback_four_editions_one_compiler`, `project_dialect_two_axes`.
>
> **Scope:** the **greenfield** compiler (`src/Cobol.Net.*`, the active engine) is the target. The legacy
> compiler was the **blueprint** (it had the per-edition machinery) and is deleted (P15 Cut 2).
>
> **Scope honesty (don't over-read this):** this matrix validates the edition **deltas / boundaries** — where gating
> bugs live (the right ~80%): "feature introduced in E is rejected below E", "feature removed by E is rejected at E",
> "85 program still compiles later unless removed or word-collided", per-edition behavior where it differs. It is NOT
> comprehensive per-edition *construct* coverage — exhaustively re-validating every 2002/2014/2023 construct stays
> gated on building those **positive corpora** (`tests/conformance/<ver>/`), which the survey confirms are young
> (≈34 × 2002, ~0 × 2014, 1 × 2023). Delta-correctness first; corpus breadth is a parallel, longer effort.

---

## 1. The conceptual model

`cobol --std V prog.cob` **is** the "COBOL-V compiler." Correctness is per-edition: for every COBOL construct
and every target edition V ∈ {85, 2002, 2014, 2023}, the compiler must produce the **edition-correct outcome** — accept
with the V behavior, or reject with an edition-appropriate diagnostic. We therefore test four logical compilers, not
one, over a shared matrix.

## 2. The matrix

**Axes:** `{construct/feature test case}` × `{target edition 85 | 2002 | 2014 | 2023}`.

Each construct case carries metadata (from the reference doc, see §4):
- `introducedIn` — the edition the construct first became standard (85 = baseline).
- `removedIn` / `deprecatedIn` — the edition that removed / obsoleted it (if any).
- `behaviorVariants` — for constructs whose *behavior* changed across editions, the expected result per edition.

**Expected-outcome function** `f(case, V)` — *computed*, never hand-maintained per cell:

| Condition | Expected outcome at edition V |
|---|---|
| `V < introducedIn` | **REJECT** — diagnostic "feature requires COBOL-`introducedIn`" (introduction-gating) |
| `removedIn ≤ V` | **REJECT / removed** — diagnostic per the reference-doc row (error in strict V; or warn, per policy §8) |
| `introducedIn ≤ V < removedIn` | **COMPILE** — and, if the construct has `behaviorVariants`, emit `behaviorVariants[V]` |

A cell is *green* when the actual outcome equals `f(case, V)`.

## 3. The three correctness invariants (as property tests)

- **INV-1 — Continuity.** ∀ COBOL-85 program P (the NIST CCVS85 corpus) and ∀ V ≥ 85: P **compiles** at V, *unless* P
  hits one of **two** legitimate-breakage classes documented in the reference doc: **(a)** P uses a construct with
  `removedIn ≤ V` (a removed/obsoleted feature); or **(b)** P uses, as a user-defined word, a word that became a
  **reserved word** at some edition ≤ V (Annex E.3.2 — "changes possibly affecting because of the addition of new
  words/names"). A compile failure at a later edition is conformant **only if it traces to a reference-doc removal row
  (a) or new-reserved-word row (b)**; any other failure is a **regression**, not conformance. (Owner's rule: "the ones
  that do not [work later] must have been deprecated by that later version for the failure to be correct.")
  > **Why class (b) matters:** a program using a word newly reserved in 2023 (Row 32 —
  > B-SHIFT-*, COMMIT, RECEIVE, SEND, XOR, …) as a user-defined name must compile pre-2023 and be **rejected at 2023**;
  > the naive "unless `removedIn ≤ V`" form would wrongly flag it as a regression. In the NC corpus `RECEIVE` occurs
  > only as `SPACING-RECEIVE` (hyphenated — a word-boundary occurrence, not a bare user-word collision), and the
  > continuity sweep finds **zero** NC breaks at 2023, so class (b) has no live NC witness. It is instead exercised by
  > synthetic negative-corpus cases: the `VersionConformancePass` reserves words per edition (§8.9) via `ReservedWords`,
  > so a bare 2023-reserved word used as a user-defined name is rejected at 2023 and accepted below.
- **INV-2 — Introduction-gating.** ∀ construct C introduced in edition E and ∀ V < E: C is **rejected** at V with the
  edition diagnostic. (A word newly reserved in 2023 is still usable as a user-defined name at 85/2002/2014.)
- **INV-3 — Behavior-correctness.** ∀ behavior-variant construct and ∀ valid V: the output equals `behaviorVariants[V]`
  (e.g. a hypothetical edition-dependent rounding/sign rule — none confirmed yet; the three investigated de-sign/DISPLAY
  differences are version-INVARIANT).

## 4. Construct catalogue — sourced from the reference doc

**⚠ Source the catalogue from THREE places, not just the reference doc — the 85↔non-85 boundary (the owner's #1
priority) is the part the 2023 reference doc covers *least*** (its Annex E is the 2014→2023 delta only). Harvest the
edition metadata mechanically from the structured in-code sources that already encode it:

| Metadata | Authoritative source | Covers |
|---|---|---|
| `introducedIn` (2002/2014/2023) | the **grammar `is2002()/is2014()/is2023()` gates** (39, per the survey) — each gated rule's predicate IS its introducing edition | the post-85 introduction points — the 85-rejects-post-85-feature half of the owner's #1 |
| `removedIn` + 85↔2002 deltas | the **legacy `FlagsFeaturesRemovedAfter85` + `DialectStrictnessChecks` (ALTER, OPEN REVERSED, L1–L5) + FLAG-02** | constructs removed after 85; the 2002 incompatibilities the ref doc only points at |
| 2014→2023 deltas, obsolete/archaic, new reserved words | **`VERSION_CHANGE_REFERENCE.md`** | the latest delta + the flagging rows |

Each source feeds the same catalogue; the reference doc is canonical for the 2014→2023 slice, the grammar+legacy for
the earlier slices. (This closes the gap the survey flagged: only ~30–40 ref-doc rows are mechanically testable, but
the grammar gates + legacy registry supply the 85↔2002/2014 boundary directly.)

A single **construct catalogue** (data, not code) is the source of truth for the matrix. Each entry:

```
{ id, title, refDocRows[], category,            // from VERSION_CHANGE_REFERENCE.md
  introducedIn, removedIn?, deprecatedIn?,        // edition metadata
  snippet,                                        // a minimal COBOL program exercising the construct
  behaviorVariants?: { "85": out, "2002": out, … },
  expectDiagnostic?: "CBL####" }                  // the code expected on rejection
```

**Mapping reference-doc `gatingImplication` → matrix obligation:**

| `gatingImplication` | `introducedIn` / `removedIn` | Matrix obligation |
|---|---|---|
| `new-feature-gate` | `introducedIn = <delta-to edition>` | positive at ≥ intro; **negative (reject) at intro−1** |
| `new-reserved-word` | `introducedIn = <edition>` | user-name OK at < intro; **reject as reserved at ≥ intro** (INV-2 inverted: words are *additive* — collision only at ≥ intro) |
| `flag-obsolete` | `removedIn` / `deprecatedIn` | accepted+warn while archaic; **reject (or warn) once removed**, per policy |
| `gate-behavior-by-dialect` | behavior change | `behaviorVariants` per edition; same snippet, different expected output |
| `none` | — | informational; no matrix row |

**Tag-ability (from the survey):** ~30–40 of the 130 rows are mechanically test-able now (new reserved words → Row 32's
16 words; removed constructs → ALTER/EXIT METHOD/OPEN REVERSED; etc.). Intrinsic-function, EC-condition, and
Unicode-table rows need curated snippets. **Highest-value seed rows:** 1, 3, 9, 27, 28, 29, 30, 32, 35, 65, 89, 90 —
they cover ~70% of real dialect collisions and exercise all five gating patterns.

## 5. Corpora

- **Positive — COBOL-85 baseline:** the NIST CCVS85 corpus (`tests/nist/programs` + `tests/nist/valid`, 364 programs)
  is the authoritative 85 positive set. `NistDifferentialTests` passes **DialectLevel 85 explicitly** (intentional —
  the corpus is COBOL-85 and edition-targeting harnesses name their edition, §10 #2). **INV-1 extends this:** run
  each NIST program at 2002/2014/2023 too and
  assert it still compiles (output need not re-match the golden at a later edition unless behavior is edition-invariant,
  which for the nucleus it is — so re-matching the golden is the strong form; start with "still compiles").
- **Positive — per-edition:** `tests/conformance/<ver>/*.cob` + `.out`, auto-discovered by the legacy `ConformanceTests`
  (dir → `DialectMode`). The greenfield needs the analogous discovery. Each program compiles/runs at ≥ its edition.
- **Negative — NEW (the main build):** for each `new-feature-gate` / removed / reserved-word row, a case asserting the
  **rejection diagnostic** at the wrong edition. Proposed layout: `tests/conformance/<ver>/_negative/<name>.cob` +
  `<name>.err` (the expected `CBL####`), discovered alongside the positives.
- **Behavior-variant:** the same snippet with a per-edition `.out` (only where INV-3 actually applies).

## 6. Harness design

**6.1 Make the compiler-under-test edition-parameterized (prerequisite).**
- `CompilerDriver.Options.DialectLevel` already exists and flows to the front-end. **Thread it into the BINDER only**
  (dual-backend discipline, G4: ALL semantics live in the binder/bound tree behind `ICodeGenBackend`; emitters only
  render): an edition-variant behavior is resolved at bind time and carried in the bound tree in edition-resolved
  form, so the Roslyn emitter AND the future Cecil/CIL backend satisfy INV-3 for free. Do NOT pass `dialectLevel`
  into `CSharpEmitter`/`OperandText`/`ConditionRenderer` for semantic decisions (the §3 INV-3 hook; not needed until
  a real behavior-variant appears, but the seam belongs bind-side).
- **`CobolNetCompiler.CompileAndRun` and `NistDifferentialTests` hard-code `DialectLevel: 85`** — add a target-edition
  parameter (the differential `ICompilerUnderTest` gains a dialect). `CobolNetTestBase` already exposes a
  `dialectLevel` param — generalize its use.

**6.2 Port the legacy per-edition model to the greenfield (the core rework).** The legacy already solved this; reuse
the design (do not re-invent — `feedback_one_mechanism_per_job`):
- **`DialectConfig` (two-axis, one cached object queried everywhere)** — strictness (`IsStrict`) × version thresholds
  (`IsCobol2002OrLater`…), `FlagsFeaturesRemovedAfter85`, `DisplayName` for diagnostics. Ported from
  the legacy compiler's `Semantics/DialectConfig.cs` (archived).
- **A post-parse edition validator** — in the greenfield this is the parse-tree arm of the `VersionConformancePass`
  (`Validation/VersionConformancePass.cs`) — implementing the **validator pattern** from `DialectStrictnessChecks`: the
  grammar parses the permissive superset; the validator decides accept / warn / reject per `(construct, DialectConfig)`
  and emits the diagnostic. This is where `introducedIn`/`removedIn` gating lives.
- **A `ConstructDialectStatus` registry** (survey recommendation): one table mapping each construct → per-edition
  severity {ERROR | WARNING | SILENT}. The registry is the code-side twin of the construct catalogue (§4) and is
  generated/checked against `VERSION_CHANGE_REFERENCE.md` so the doc and the compiler cannot drift.
- **Version diagnostics:** the edition-gating band `COBOLNET0900–0999` (`CobolNet.Editions.EditionCodes`):
  0900 "feature requires COBOL-YYYY", 0901 reserved word, 0902 "removed in COBOL-YYYY", 0903 obsolete/archaic flag,
  reported through `ConstructRegistry.Check` — the greenfield successors of legacy `CBL3601/3602/3607/3611…3618`.

**6.3 The matrix test (the payoff).** A parameterized xUnit `[Theory]` over the catalogue × editions:
```
[Theory] [MemberData(nameof(MatrixCases))]   // (caseId, targetEdition)
public void Construct_MatchesEditionExpectation(string caseId, int edition) {
    var case = Catalogue[caseId];
    var expected = ExpectedOutcome(case, edition);     // the f(case,V) of §2
    var actual = CobolNet.CompileAndRun(case.snippet, edition);
    if (expected.Rejected) AssertRejectedWith(actual, expected.Diagnostic);
    else { AssertCompiles(actual); if (case.behaviorVariants is {}) AssertOutput(actual, expected.Output); }
}
```
plus the INV-1 continuity property test (NIST corpus × {2002,2014,2023}) and a negative-corpus discovery test.

When the Cecil/CIL backend lands (G4, `--backend cil`), INV-3 behavior-variant cells run per `--backend` (they
exercise codegen); INV-1/INV-2 gating cells are front-end-only and run once — keep the harness's compile-and-run
seam backend-parameterizable.

**6.4 Negative-test harness (greenfield):** port `DiagnosticTestBase.GetDiagnostics(source, edition)` +
`AssertHasDiagnostic(code)` / `AssertNoDiagnostic(code)` to the greenfield (it has no diagnostic-assertion harness
today — only output differential). Reuse the legacy `FlaggingConformanceTests` shape for obsolete-element flagging.

## 7. The matrix is the worklist

Any construct without edition gating leaves its cells **RED** for V ≠ 85. **Each red cell is a
version-gating task, driven by its reference-doc row** — the matrix is simultaneously the worklist and the acceptance
test for the gating implementation (TDD). "Done" = the row's cells are green at every edition. This makes the
`VERSION_CHANGE_REFERENCE.md` `Status` column actionable: `TODO` → `done` as cells go green.

## 8. Phased rollout (the "substantial rework")

- **Phase 0 — harness scaffold ✅ DONE.** `tests/Cobol.Net.Tests.Conformance/VersionMatrixTests.cs`
  stands up the matrix `[Theory]` over (construct × edition) with the computed `f(case,V)` and an inline seed catalogue,
  plus the INV-1 continuity `[Theory]`, exercising **introduction-gating both
  directions** (DELETE FILE, introduced 2023 — rejected at 85/2002/2014, compiles at 2023) and **continuity**
  (NC101A/NC211A/NC136A compile at later editions). (ALTER was the design's
  example, but ALTER is a lexer token with no greenfield statement rule — removed-construct gating goes through the
  `VersionConformancePass`; DELETE FILE is the cleaner proof since it is fully grammar-gated AND compiles at its intro
  edition.) **Decision #2 — default `--std`:** the `CompilerDriver.Options.DialectLevel` default is
  **2023** (`src/Cobol.Net.Compiler/CompilerDriver.cs`), pinned by a `CompilerDriverTests` regression test; the CLI
  `--std` defaults to 2023 and `--nist` without an explicit `--std` targets 85 (`src/Cobol.Net.Cli/CliOptions.cs`).
  **Phase 1 — seed + continuity ✅ DONE:** the canonical **`tests/version-matrix/constructs.json`** (12 seeded rows —
  85 baseline; 2002: ALLOCATE, FREE, INVOKE, GOBACK RETURNING, STOP-status, BASED, PROCEDURE…RETURNING; 2014: JSON
  GENERATE, XML GENERATE; 2023: DELETE FILE, TYPE IS) loaded by `VersionMatrixTests` — 48 matrix cells, all green;
  the **`EditionHarness`** (`Compile`/`CompileNist`/`GetDiagnostics`/`AssertHasDiagnostic` — THE per-edition
  compile path every edition-targeted test shares); and the **full INV-1 continuity sweep**
  (`scripts/version-continuity-sweep.sh`): **342 NIST programs compile at 85 AND at 2002/2014/2023 — ZERO
  breaks** (117 don't compile at 85 yet — feature gaps, outside the witness set; re-run as features land).
  The JSON/XML statement grammar rules carry the real seam-level GENERATE/PARSE surface (statement heads + COUNT +
  PROCESSING PROCEDURE + exception phrases; detail phrases ride the subsystem wave). `DialectLevel` threads into the
  binder (bind-side only — see §6.1) via `EditionContext`, and grammar-gate rejections carry edition-NAMING
  diagnostics asserted via `AssertHasDiagnostic`.
- **Phase 1 — seed + continuity.** ✅ Done as above.
- **Phase 2 — backfill + implement gating ✅ DONE.** The catalogue grew across the mechanically-testable rows; edition
  gating is built (the two-arm `VersionConformancePass` — its parse-tree arm the absorbed edition validator — plus the
  `ConstructDialectStatus` + `ConstructRegistry`); red cells go green per row. Each new feature ships its
  matrix rows (extends `feedback_goldens_ship_with_the_feature`). The full implementation is the next section.
- **Phase 3 — negative corpus + flagging (in build-out).** The `_negative/` corpus; obsolete/archaic/new-reserved-word
  flagging; behavior-variant rows where INV-3 applies; auto-check the registry against the reference doc (drift guard).

---

## Edition-gating implementation — the `VersionConformancePass`

> The canonical edition-gating design and its as-built shape. The edition validator is the parse-tree arm of the
> two-arm `VersionConformancePass` (the SOLE edition gate); the gating is organized in three waves (§P2.6–P2.8);
> each gate cites its `VERSION_CHANGE_REFERENCE.md` (VCR) row and ISO §.

### P2.1 Channels and policy seam (`Binding/EditionContext.cs`)

`EditionContext` is the seam (its doc comment already names it so). It gains: ctor
`(int dialectLevel, bool permissive = false)`; `Permissive` (the legacy `DialectMode.Default` axis, §10 #1 —
strict is the default, every named `--std` is strict); `List<string> Warnings` + `Warning(code,msg)`;
`HasErrors`; and the ONE severity seam `Removed(code,msg)` = Error when strict / Warning when permissive.
**`Diagnostics` stays ERRORS-ONLY** — `CompilerDriver.Compile` returns on any error diagnostic, so a warning
appended there would fail the compile; `Warning()` is the only writer to `Warnings`. Carriers:
`CompilerDriver.Options` + `bool Permissive = false`; `Result` + `IReadOnlyList<string> Warnings = []` (set on
every return); CLI `--permissive` flag (orthogonal to `--std`/`--nist`; the `--nist`-without-`--std` ⇒ 85 logic
untouched); `Program.cs` prints warnings to stderr always.

### P2.2 The conformance pass (`src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs`)

- **Two arms.** The gate is the two-arm `VersionConformancePass`: a **parse-tree arm** (the edition validator — it
  fires the syntactic introduction/removal/phrase gates + the §8.9 reserved-word funnel on RECOGNITION) and a
  **bound-tree arm** (the genuinely-semantic statement-level gates).
- **Walk (parse-tree arm):** NO listener is generated (ANTLR runs `-no-listener -visitor`) — the arm derives from the
  generated `CobolParserCoreBaseVisitor<object?>` (namespace `CobolNet.Frontend.Generated`); overrides return
  `base.VisitChildren(ctx)` to keep descending. Root = the compilation unit produced by `Frontend.Parse`.
- **Hook:** the pass is the NAMED TERMINAL pass of the binder manifest (run behind `emitter.Bind`), so the returned
  `BoundCompilation` already carries EVERY edition diagnostic on the sink — for both a full compile and a CheckOnly
  verdict. `CompilerDriver.Compile` fails fast on `edition.Diagnostics` BEFORE the backend runs (a removed/unintroduced
  construct may have no emit path). Edition errors ride the SAME `EditionContext` → the existing BindError path; no new
  `Outcome`.
- **Division of labor:** syntax-only gating lives in the parse-tree arm; gating that needs bind/type information
  (e.g. the MOVE rows) lives in the bound-tree arm or binder-side — but EVERY severity decision routes through
  `Removed()`/the registry (one policy, several emit sites; `feedback_one_mechanism_per_job`).

### P2.3 Diagnostics band (COBOLNET0900–0999, verified unused)

| Code | Meaning | Severity |
|---|---|---|
| COBOLNET0900 | construct requires COBOL-YYYY (introduction gating, validator-visible) | error |
| COBOLNET0901 | word reserved in COBOL-YYYY used as a user-defined word (ISO §8.9) | error strict / warning permissive |
| COBOLNET0902 | construct removed in COBOL-YYYY | error strict / warning permissive (via `Removed`) |
| COBOLNET0903 | obsolete/archaic-element flag | warning (always) |

Existing codes are KEPT where tests/history pin them: 0873 (DATA RECORDS, FD+SD), 0810/0811 (ALTER / bare
GO TO), 0882 (CALL ON OVERFLOW).

### P2.4 Per-edition reserved words (ISO §8.9; the INV-2-inverted obligation)

- **The funnel:** every user-defined word reaches the tree through the `cobolWord` rule (CobolParserCore.g4:25 —
  `IDENTIFIER` + an allowlist of context keywords). 2023-new words (COMMIT, ROLLBACK, FINALLY, RECEIVE, SEND,
  EDITING, LOCATION, MESSAGE-TAG, END-RECEIVE, END-SEND, B-SHIFT-\*) are NOT lexer tokens — they lex as
  IDENTIFIER, so the check is TEXT-based: `VisitCobolWord` tests `ctx.Start.Text` (upper) against
  `ReservedWords.IsReservedAt(word, edition)`, deduped to ONE diagnostic per distinct word per compilation.
  This also correctly rejects the allowlisted context keywords (RAISE/RAISING/RESUME/CONDITION/LENGTH/SUM…) at
  editions where §8.9 reserves them — the grammar's "legal user word at every edition" posture stays (permissive
  superset), the validator enforces per edition.
- **Tables:** per-word 4-edition flags `(r85, r2002, r2014, r2023)` + confidence + provenance. Generation is
  scripted (`scripts/gen-reserved-words.ps1`): parse the in-repo spec §8.9 (`specs/ISO_COBOL.md` lines
  10306–10788; strip the 15 symbol entries; OCR fixes: EMD-START→END-START, `i-O`→I-O, I-OICONTROL→I-O-CONTROL,
  en-dash minus; ADD the OCR-omitted METHOD) ⊕ a small authored delta file
  (`tests/version-matrix/reserved-word-deltas.json`: the authoritative 16-word 2023 additions = VCR row 32; the
  hand-authored X3.23-1985 list; a medium-confidence 2014 IEEE-754 family). Derivation: ∈2023 ∧ ∈85 ∧ ∉added2023
  ⇒ continuous; ∈added2023 ⇒ 2023-only EXCEPT RECEIVE/SEND (85-reserved → unreserved 2002/2014 → re-reserved
  2023); ∈85 ∧ ∉2023 ⇒ 85-only (removed 2002, flagged); remainder ⇒ added 2002. ROUNDED-mode words etc. are
  §8.10 CONTEXT-SENSITIVE in 2023 — not reserved, not in the table. Script emits BOTH the C# table
  (`src/Cobol.Net.Editions/ReservedWords.Table.cs`) AND the canonical `tests/version-matrix/reserved-words.json`; a
  drift test compares them.
- **Conservative policy:** only `confidence: high` rows REJECT; lower-confidence rows are present but inert
  (documented), per the VCR scope-limit rule ("confirm against the older standard before gating") — a wrong
  entry must never reject a valid program.
- **⚠ Operational:** build the word tables by script — never have a subagent emit the big lists as
  output (the API content filter blocks dense recalled word dumps).

### P2.5 `ConstructDialectStatus` registry + drift checks

`src/Cobol.Net.Editions/ConstructDialectStatus.cs` (record: Id, Display, IntroducedIn, RemovedIn, ObsoleteIn?,
DiagnosticCode, Citation) + `ConstructRegistry` with `StatusAt(edition, permissive)` and the one `Check(...)`
entry. `tests/version-matrix/constructs.json` stays THE canonical catalogue (§10 #5; gains an optional
`expectDiagnostic` field); a unit drift test asserts registry↔json metadata equality both directions, and the
ReservedWords drift test does the same one level up.

### P2.6 Wave-1 construct checklist (validator overrides + binder migrations)

85→2002 removals — `Removed` ≥2002 (VCR Table 7 grows a row per item): LABEL RECORDS (FD,
Core/CobolData.g4:106; 0902) · VALUE OF (:118; 0902) · DATA RECORDS FD+SD (:111; **0873** — the DataBinder.cs:322
SD gate MIGRATES into the validator, one enforcement site; Table 7 row 7.1's recorded follow-up) · MEMORY SIZE,
SEGMENT-LIMIT (the modelled `memorySizeClause` / `segmentLimitClause`, CobolParserCore.g4 — kb/Work PB830; 0902) ·
WITH DEBUGGING MODE (the modelled `debuggingModeClause`; NOT a 2002 removal — obsolete 2002 with 0903, removed 2014,
owner decision kb/Work R61, row `debugging-mode-removed-2014`) · MULTIPLE FILE [TAPE] (CobolIO.g4:170; 0902) · the five identification comment
paragraphs AUTHOR/INSTALLATION/DATE-WRITTEN/DATE-COMPILED/SECURITY (0902) · REMARKS (0902 ≥2002 ONLY — CCVS
uses it; never flag at 85 until the FIPS-flagging strictness work) · STOP literal (CobolControlFlow.g4:222;
0902) · OPEN REVERSED (CobolIO.g4:193; 0902).
2014→2023 removals — `Removed` ≥2023: CLOSE WITH LOCK (CobolIO.g4:214; 0902; VCR row 7) · EXIT METHOD / EXIT
FUNCTION (CobolControlFlow.g4:213; ALSO introduction-gated 0900 <2002; VCR rows 5/6).
2023 archaic flags — 0903 warnings @≥2023: EXIT PROGRAM (VCR 89) · NEXT SENTENCE (VCR 90). No 85-obsolete
warnings this wave (NIST noise; the 85 FIPS flagger is future strictness work).
Binder migrations — `Error`→`Removed` at: AlterSwitches 0810/0811, Call 0882; VERIFY each site still BINDS the
construct after a permissive warn (permissive @2002 must run ALTER with 85 semantics).

### P2.7 Harness/test changes (and the continuity consequence)

`EditionHarness` gains `permissive` params + a `CompileFull` returning (Ok, Errors, Warnings) (old tuple methods
delegate); `AssertNoDiagnostic`. `VersionMatrixTests` asserts `expectDiagnostic` on reject cells and gains the
permissive theory: rows with `removedIn`, compiled at ≥removedIn with permissive:true, MUST compile and carry
the diagnostic as a warning (the §10 #1 contract). **⚠ Consequence:** every NIST FD writes LABEL RECORDS, so
once gating lands the INV-1 continuity legs at ≥2002 (the file-suite `[InlineData]` rows AND
`scripts/version-continuity-sweep.sh`) legitimately break under strict — they FLIP TO PERMISSIVE mode (the
migration posture; INV-1's formal statement becomes "compiles permissive at later editions; strict failures
must trace to removal/reserved rows"). New matrix rows: one per P2.6 gate, plus reserved-word rows via the
interval encoding — `user-word-commit-2023` (intro 85 / removedIn 2023 / 0901), `user-word-raising-2002`
(intro 85 / removedIn 2002 / 0901), and `receive-as-user-word` (intro **2002** / removedIn 2023 / 0901 — encodes
85-reserved → 2002/2014-free → 2023-re-reserved in one f(case,V) row).

> **As built (deltas from the plan above).** The reserved-word 85/2002/2014 lists come from GnuCOBOL's per-standard
> `config/*.words` (curl disk-to-disk, gitignored `.cache/`, facts-only into the repo) ⊕ the in-repo spec §8.9 ⊕ VCR
> row 32: the re-reservation set is THREE words incl. the END- scope terminator; the EC words are reserved since 2002
> (not 2023-only); Annex E overrides the GnuCOBOL 2002/2014 curation, which keeps the communication trio. ORDER carries
> a CCVS-proof not-85 override (conforming ST127A uses it as a data name). The interval rows include
> `end-receive-as-user-word`. The INV-1-strong behavioral leg is `COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1`
> over the golden run — byte-exact at the default edition (the roadmap's fatal-challenge criterion). The four binder
> Error→`Removed()` migrations (0810/0811/0882/0873-SD) run the construct after a permissive warn (zero behavioral
> diffs).

### P2.8 Waves 2–3 (follow-on, same Phase)

W2 (parallel agents, disjoint files): the MOVE rows (VCR 1 — alphanumeric-figurative→numeric `Removed` ≥2023
with the digit-only-ALL exception; VCR 92/128 — ALL-digit→integer obsolete 0903 @2023) + fix the two latent
bugs found by the inventory (STOP literal silently binds as STOP RUN, in `ControlFlowBinder` (the STOP verb) → runtime-loud or
implement; MOVE ALL "5" TO integer fails loud though valid at EVERY edition, §14.9.25 GR5); the `_negative/`
corpus discovery (→ Phase 3); VCR Status flips; adversarial review.
W3 (frontend/grammar — FULL legacy guard + committed regen): the XOR/EXCLUSIVE-OR
hole (unconditional lexer tokens wrongly commented "COBOL-2002" — they are 2023, VCR rows 41/32; not in
`cobolWord` ⇒ unusable as user words at EVERY edition and the operator un-gated below 2023); the notInGrammar
85-acceptance set (RERUN, ENTER, USE FOR DEBUGGING, section segment-numbers — generic parse errors today, a G1
co-equal-diagnostic violation at 85); the preprocessor rows (COPY REPLACING non-pseudo-text ≥2023 = VCR 4,
fixed-form word continuation ≥2023 = VCR 2, col-7 hyphen obsolete flag = VCR 94 — `DialectLevel` is not
threaded into `CopyProcessor`/`ReferenceFormatProcessor` today). The COMMUNICATION module stays an M2-scope
decision.

> **W2 — as built.**
> **(a) MOVE rows** — the binder-side gate is `MoveBinder.cs` (`Binding/Procedure/Verbs/`): a digit-only
> single-character ALL → integer numeric elementary receiver rides `move-all-digit-integer-obsolete-2023`
> (0903 ≥2023, VCR 92/128); every other alphanumeric-figurative/ALL → numeric/numeric-edited elementary move
> rides `move-alphanumeric-figurative-removed-2023` (0902 @2023, VCR 1) with the §-mandated exemptions (ZERO
> §8.3.3.6.4 GR4; group receivers §14.9.25.4 GR4; ref-mod receivers §8.4.3.3.4).
> ALL-digit folds to its GR6d3b/GR2 value compile-time (`AllDigitFill` in `MoveEmitter`, `CodeGen/Verbs/MoveEmitter.cs`;
> ALL "5"→9(3)=555, →9V9=5.5); non-digit fills deposit the character image via the `StoreAsImage` character-image
> storage form (legacy-oracle-adjudicated, provisional per ratified decision 1). Two legacy NON-conformances
> documented and not mirrored (legacy CBL0906 compile-rejects QUOTE/HIGH/LOW→numeric at every standard;
> legacy DISPLAYs a space-filled numeric as empty). A multi-character ALL literal
> is NOT on these rows: §8.3.3.6.3 SR3 bars its association with a numeric/numeric-edited item, removed 2002
> (`all-literal-multichar-numeric-removed-2002`, VCR Table 7 row 7.13 — a derived edge; kb/Work PB422).
> **(b) The loud-guard sweep** — `PictureAnalyzer.ParseUsage`/`Analyze` take `(EditionContext, where)`: the silent
> Display catch-all is dead; the 2002+ recognized-but-unimplemented inventory (NATIONAL, BIT, POINTER, OBJECT
> REFERENCE, BINARY-CHAR family, FLOAT-SHORT/LONG/EXTENDED, PIC N/1/E) routes its registry row (0900 below
> 2002) + a COBOLNET0899 not-implemented error at ≥2002 naming the owning phase; an unknown usage keyword or
> PICTURE symbol is loud (new COBOLNET0808 invalid-PICTURE-symbol; §13.18.40.3 SR2 whitelist honoring the
> program currency symbol). `UsageKeyword` reads tokens, not stripped text (bare `BINARY-CHAR SIGNED` gates
> identically to the full form). Skeleton enum members (PicCategory.National/.Boolean + 11 Usage members) throw
> loud from every storage-mapping member if reached. `CallCollectUnits` no longer silently drops a
> `classDefinition` (0899, Phase 3). ⚠ This exposed the allocate/free/invoke matrix rows as DOUBLE-silent-hole
> false-greens (pointer usage misbound to Display + statements bind BoundUnsupported) — flipped to pending
> until Phase 4b/3.
> **(c) The negative corpus** — 18 cases enabled (11 2002-removal gates, 3 2023 removals, 4 reserved-word
> interval witnesses), every (case × edition) rejection AND every pre-removal-edition clean compile verified
> against the CLI before enablement.
> **(d) Position-aware reserved words** — reserved-word checking is position-aware:
> non-IDENTIFIER/non-EC-band `cobolWord` occurrences reject 0901 when (and only when) they occupy a slot no
> cobolWord-admitted keyword can legally occupy — the data entry-name (`dataName` under
> `dataDescriptionEntry`/`constantEntry`), paragraph/section DEFINITIONS, the SELECT file-name, and
> the three `programName` sites (`VersionConformancePass.IsProvableUserWordPosition`, grammar-proved per slot). The
> mis-parse-prone optional entry-name slots (`dataName` under `reportGroupEntry` — the RW104A COLUMN hazard — and `screenName`)
> and all reference positions stay unchecked (conservative false-negative, never false-positive). Of the 34
> excluded band tokens only 7 have §8.9 table rows (2 continuous-since-85
> incl. COLUMN, 5 added-2002); the other 27 are §8.10 context-sensitive words with no reservation to enforce —
> the position machinery closes the ENTIRE real gap. New correct strictness: a data item/paragraph/SELECT
> file/program named with a continuously-reserved band word now rejects 0901 even at 85 strict (§8.3.2.1 rule
> 1); corpus-swept clean (the only corpus band-word hits are RW102A/103A/104A report-group COLUMN clause
> keywords — excluded positions by construction). ⚠ Standing hazard, flagged in the slot comments: a future
> grammar change adding a data-description clause that BEGINS with a cobolWord-admitted token would silently
> re-open the RW104A-style hazard for the entry-name slot.

> **W1.5 — as built (with the W2 adversarial-review fixes).** The ~24
> intro-gate upgrade is a PARSE-LAYER mapping (no grammar change): every gated site surfaces below its
> edition as a generic NoViableAlternative, so `EditionGateHints` (frontend) recognizes the per-site
> (token, rule-stack, adjacency) signature and — only when the targeted edition is below the construct's
> introduction — emits the `ConstructRegistry.Check` wording on COBOLNET0900 through `CobolErrorStrategy`
> (priority-0 hint; the code rides the `[code]`-prefix extraction). All 17 reachable sites verified; JSON/XML
> map to the vendor hint COBOL0313 (not ISO — the 0900 band would be a lie); unmapped residue documented
> in-code (inline method invocation / UDF parameterDescription / SET-TO-objectReference). The 0860/0861
> double allocation (READ PREVIOUS / START FIRST-LAST vs the WRITE END-OF-PAGE diagnostics) resolved by
> registry migration; currency-picture-symbol re-pinned 0893; 5 new rows (repository-class, start-with-length,
> special-names-for-national, call-by-value active; class-definition pending) and `expectDiagnostic:
> COBOLNET0900` on 21 rows — the matrix reject cells assert the CODE.
> QUOTE→numeric rides its own dual row `move-quote-numeric-obsolete-2014` (Annex E.2 item 21: 0903@2014,
> 0902@2023 — the ObsoleteMatrix theory is bounded below `removedIn` and asserts the FIXED 0903 band code);
> the version-invariant §14.9.25.3 SR1 class-index MOVE check is COBOLNET0809 (every edition, both operands);
> ref-mod slice stores on numeric-DISPLAY items image-back the item at bind time for EVERY sender
> (`MarkRefModStoreImage` — the round-trip-loss fix). ⚠ KNOWN MISBIND queued to W3: the trailing `,`
> clause-separator twin of the fixed `;` over-capture (VCR Table 7 row 7.14 — needs the lexer-mode cure).

> **W3 ④ — as built: the notInGrammar 85-acceptance set (VCR Table 7 rows 7.15–7.18).** All four constructs
> parse UNGATED at every edition (the STOP-literal house style — a
> `{is85()}?` predicate would be wrong: at ≥2002 they must produce 0902 with edition naming, not a parse
> error) and gate via `VersionConformancePass` → `ConstructRegistry`. Grammar: 7 new lexer tokens (RERUN, ENTER,
> EVERY, CLOCK-UNITS, DEBUGGING, REFERENCES, PROCEDURES), each admitted to cobolWord + `_dataNameTokens` +
> `CheckedTokenTypes` (position-safe: their keyword occurrences parse through dedicated rules, never a name
> slot); `rerunClause` (CobolIO.g4, all four EVERY forms), `enterStatement` (operands are SYSTEM-names via
> `enterOperand : IDENTIFIER | LINKAGE` — NOT cobolWord, else the funnel false-0901s the conforming
> `ENTER COBOL.`), the `USE FOR DEBUGGING ON? useDebugTarget+` format, and `SECTION integerLiteral?` on both
> section-header rules. Inert-at-85 bindings: RERUN rides `BindIoControl`'s non-SAME skip; ENTER →
> `BoundNop`; segment-numbers ignored by the collectors; USE FOR DEBUGGING implements the '85 dual rule —
> switch-absent ⇒ the section is compiled AS IF COMMENT LINES (binder skips it in `DeclCollectSection` AND
> the validator skips the body in `VisitDeclarativeSection`, still visiting the USE so its ≥2002 gate fires;
> DB103M = the corpus witness), switch-present ⇒ compiled-but-never-triggered (`_debuggingModeDeclared`,
> reset per top-level unit so nested programs inherit), and a DEBUG-* register reference under the switch
> diagnoses 0899 not-implemented instead of the false 0901 (DB101A). New coverage: 4 registry+json rows,
> 4 negative-corpus cases, `Ansi85AcceptanceTests` (23 facts: inert runs, operand forms, the SAME+RERUN
> one-period adjacency, DB residue compiles, per-word §8.9 freeing editions per ReservedWords.Table —
> RERUN/ENTER 2002, DEBUGGING 2014, EVERY/CLOCK-UNITS/REFERENCES/PROCEDURES 2023).

## 9. Reuse / do-not-duplicate map

| Need | Existing (reuse / port) |
|---|---|
| Edition config object | legacy `DialectConfig` (two-axis) |
| Accept/warn/reject-by-edition validator | legacy `DialectStrictnessChecks` (validator pattern) + `ControlFlowBinder` gating |
| Per-edition positive corpus discovery | legacy `ConformanceTests` (`tests/conformance/<ver>/`) |
| Obsolete-element flagging conformance | legacy `FlaggingConformanceTests` + `CompileNistDiagnostics(name, dialect)` |
| Diagnostic-assertion harness | legacy `DiagnosticTestBase.GetDiagnostics/AssertHasDiagnostic` |
| 85 positive + differential | `NistDifferentialTests` + the `ICompilerUnderTest` differential harness (add a dialect param) |

## 10. Decisions (owner-resolved 2026-06-09 + remaining defaults)

1. **Removed-construct outcome — RESOLVED:** **ERROR under a strict `--std NNNN`; WARNING under the permissive
   `Default`/`--nist` mode** (mirrors legacy `FlagsFeaturesRemovedAfter85`). The matrix's reject cells for removed
   constructs assert an error at strict editions ≥ `removedIn`, a warning under permissive.
2. **Default `--std` — RESOLVED:** **COBOL-2023** (the latest) when no `--std` is given. ⚠ This DIFFERS from
   the legacy default (`StrictCobol85`): the greenfield `CompilerDriver.Options.DialectLevel` default is 2023.
   Consequence: an unflagged compile of legacy source may hit new-2023 reserved words / removed
   constructs by default (that is intended — newest-standard-by-default). The test harnesses that target a specific
   edition (NIST at 85, the differential harness, per-edition conformance) **pass the edition explicitly**, so the
   default flip does not affect them. CLI `--std` defaults to 2023; `--nist` without an
   explicit `--std` targets 85 (the CCVS corpus's edition). The permissive/strict axis (the legacy `Default` mode)
   is implemented: `EditionContext.Permissive` (set by the CLI `--permissive` flag) drives the `Removed()` severity
   seam — error under a strict `--std`, warning under permissive.
3. **Next step — RESOLVED:** build **Phase 0** now (this session).
4. **INV-1 strong vs weak form:** "still compiles" (weak) vs "re-matches the 85 golden" (strong) at later editions.
   *Default:* weak first (compiles), strengthen to golden-match where behavior is edition-invariant.
5. **Catalogue ↔ registry source of truth:** make ONE **structured data file** canonical — e.g.
   `tests/version-matrix/constructs.json` (or `.yaml`) holding every catalogue entry (§4) with its edition metadata.
   BOTH the human-readable `VERSION_CHANGE_REFERENCE.md` table AND the in-code `ConstructDialectStatus` registry are
   derived/diffed from that file (a CI drift check). The "cannot drift" guarantee must **not** parse markdown — the doc
   is a rendering of the data, not the source.
