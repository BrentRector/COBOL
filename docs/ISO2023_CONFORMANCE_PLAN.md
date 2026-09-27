# COBOL.NET — ISO/IEC 1989:2023 Conformance Plan (LEDGER — the M2/M3/M4 post-85 feature catalog)

> **⚠ FROZEN HISTORICAL LEDGER (2026-07-19):** rows here are NOT live tracking — the P13 review found several
> stale (e.g. 'no named track' rows that ARE now tracked). Live state = the plan §0 + its §8 residue ledger +
> the review ledger §24. The PHASE-10 doc it cites was absorbed into the plan (Part III record + §8).

> ✅ **M2 CATALOG FLIPPED TO GREENFIELD TRUTH (2026-07-17 — the PHASE-10 close, DEVLOG 854–870).** Every M2
> (COBOL-2002) non-OO track is LANDED on the greenfield substrate or carries a NAMED staged residue: the
> per-track record is the PHASE-10 doc's verdict table + per-step as-builts
> (`docs/rearchitecture/PHASE-10-m2-residual-catalog.md` — its STATUS banner lists the forward ledger), with the
> per-wave row flips in `docs/PHASE4_RECONCILIATION.md`. §3's per-item marks below remain the LEGACY-era record
> per the superseded-banner note — consult the P10 table for greenfield truth, this file for the FEATURE LIST.

> ⚠️ **PARTIALLY SUPERSEDED (2026-06-09, DEVLOG 521).** This doc predates the greenfield PIVOT (DEVLOG 457): its
> framing — the byte-engine "data-model migration" being the #1 priority before conformance (§0 / §0.5), stages,
> EnableTypedFields, "island the byte engine" — is **OBSOLETE** (the greenfield is born typed-native; there is no
> migration). **Use this doc ONLY for the M2/M3/M4 FEATURE CATALOG (the still-valid list of post-85 features to
> implement).** ⚠ **Every ☑/◐/🐛 status and every recipe/file path in §1/§3 records the LEGACY byte-engine
> compiler (`src/CobolSharp.*` — differential oracle only, deleted at G8). In the greenfield `src/Cobol.Net.*` a
> feature counts as done ONLY when implemented there per the `COBOLNET_*` deep-dives + the ISO spec, with BOTH
> per-edition obligations: the complete spec behavior AND the rejecting diagnostic under every `--std` edition
> that lacks it.** The live plan is `docs/COBOLNET_REARCHITECTURE_PLAN.md` §0 (THE ONE PLAN — it absorbed `resume-prompt.md`, now deleted) over the SSOT `docs/COBOLNET_DESIGN.md`;
> multi-edition correctness is driven by `docs/VERSION_TEST_MATRIX_DESIGN.md` / `docs/VERSION_CHANGE_REFERENCE.md`.

> **Purpose.** This is the durable, authoritative work-breakdown to take the compiler to **complete,
> production-quality ISO/IEC 1989:2023 COBOL support**. Work *from* this document — do **not** re-run the gap
> analysis each session (it has been done at least three times). When an item lands, tick it here in the same
> commit. When you discover a new gap, add it here. This supersedes ad-hoc backlogs scattered across DEVLOG /
> memory; the live plan is `docs/COBOLNET_REARCHITECTURE_PLAN.md` §0 over the SSOT `docs/COBOLNET_DESIGN.md` (the former milestone view
> `docs/MULTIVERSION_ROADMAP.md` was deleted with the pre-PIVOT docs, DEVLOG 523–524).
>
> **Provenance.** Built 2026-06-05 from a 15-area parallel spec-conformance audit (workflow
> `iso2023-conformance-audit`, 15 agents vs `specs/ISO_COBOL.md` + the actual compiler) plus the session's own
> surveys. Audit-confirmed findings are tagged **[A]**.

## 0. How to use this document

- **Process (unchanged project discipline):** implement **one item at a time on `main`**; build all layers
  together (preprocess/parse → binder/bound tree [ALL semantics live here] → backend render via `ICodeGenBackend`,
  Roslyn C# primary → runtime → **output-verifying test**); run
  `bash scripts/guard.sh` after each (must stay **ALL GREEN**, 0 NIST FAIL); commit with a **DEVLOG** entry; then
  tick the item here.
- **Conformance testing is PART AND PARCEL of every post-1985 feature (owner directive).** Every 2002/2014/2023
  feature ships, **in the same commit**, with at least one conformance test in `tests/conformance/<version>/` — a
  `<name>.cob` + `<name>.out` (expected stdout), **auto-discovered** by `ConformanceTests` and run inside the
  guard. This corpus is the NIST-equivalent for the post-1985 standards (NIST CCVS covers only '85). See
  `tests/conformance/README.md`. It is both conformance evidence and the regression net as features accrue.
- **⛔ TWO co-equal obligations per catalog item (owner, 2026-06-10):** for EVERY feature here, (1) implement the
  complete per-edition ISO spec behavior, AND (2) emit the correct diagnostic under every `--std` edition that
  lacks the feature (not-yet-introduced or removed) — e.g. an M2/2002 construct compiled `--std 85` must flag.
  Version-gating is driven by `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row edition-change checklist — 2002→2023
  deltas ONLY; it has NO 85→2002 rows; derive 85↔2002 gating from the 2002 standard / the M2 catalog in §3 below)
  and verified by the (construct × edition) matrix in `docs/VERSION_TEST_MATRIX_DESIGN.md`; the negative
  per-edition cases belong in that matrix alongside the positive `tests/conformance/<version>/` corpus.
- **Do NOT use worktree-isolated implementation workflows** for compiler fixes — in this repo `isolation:'worktree'`
  branches from a stale commit. Do compiler work directly on `main`. (Parallel *audit/design* agents are fine.)
- **Grammar changes are pre-authorized** in this conformance effort (log + full guard); new lexer keywords must be
  corpus-checked (grep NIST/fixtures for standalone occurrences) before adding.
- **Status legend:** ☐ todo · ◐ partial · ☑ done · 🐛 = a *correctness* defect (silent wrong result), not just a
  missing feature — these rank above pure feature gaps.

---

## 0.5 [RETIRED 2026-06-10 — pre-PIVOT history only] The byte-engine data-model migration (DEVLOG 393–456)

> ⛔ **OBSOLETE — do NOT execute anything in this section.** It describes the legacy byte-engine → typed-fields
> migration. The greenfield COBOL.NET (`src/Cobol.Net.*`) is born typed-native (no byte substrate, no migration);
> the Roslyn C# backend is PRIMARY with a Cecil/CIL backend future-additive behind `ICodeGenBackend`
> (`docs/COBOLNET_DESIGN.md`). Its supporting docs (`DATA_MODEL_ARCHITECTURE.md`, `DATA_MODEL_REVIEW.md`,
> `RECORD_STRUCT_STORAGE_DESIGN.md`, `OO_IMPLEMENTATION_DESIGN.md`) were DELETED (DEVLOG 523–524).

The owner had directed a foundational re-architecture to **"the best native .NET implementation of COBOL"** — this
was the **#1 work item of the pre-PIVOT byte-engine era. Do NOT do it now:** the greenfield is born typed-native
(no migration); the live plan is `docs/COBOLNET_REARCHITECTURE_PLAN.md` §0. Kept below for history only.

- **The design is settled and reviewed — do NOT re-litigate it** (the owner co-authored it across a long dialogue,
  DEVLOG 393). Read both first:
  - `docs/DATA_MODEL_ARCHITECTURE.md` — the ADR. Typed-native is the default: COBOL records → .NET `record struct`;
    elementary items → native value types (`long`/`decimal`/`double`/`bool`); **character data (PIC X *and* PIC N)
    → `string` (UTF-16)**; a byte image is only a **classifier-scoped fallback** for REDEFINES/RENAMES type-puns,
    file records, and a few hot loops (an inline value type embedded in the typed record — never a heap `byte[]`);
    pointers → managed references; OO → .NET classes; in-memory representation is decoupled from external encoding
    (`CODE-SET` is a boundary concern). ~~Cecil/CIL stays primary~~ **[SUPERSEDED — per the PIVOT the Roslyn C#
    backend is PRIMARY; a Cecil/CIL backend is future-additive behind `ICodeGenBackend` with its own private
    structure-to-branch lowering (no shared lowered IR).]**
  - `docs/DATA_MODEL_REVIEW.md` — a ~57-agent adversarial review; verdict **proceed-with-changes**; all 4 high + 6
    medium findings already folded into the ADR.
- **Execute the migration in the ADR's 7 stages (§10), guard-green at EVERY step, one rule at a time:** Stage 0
  scaffolding (classify *everything* byte-backed = today's behavior) → Stage 1 numeric pipeline + differential
  oracle → Stage 2 classifier (fallback on) → Stage 3 flip typed one rule at a time (**character data first — the
  cheapest, highest-payoff flip**) → Stage 4 pointers + OO → Stage 5 Roslyn C# backend w/ Cecil oracle → Stage 6
  finalize runtime + post-conformance rename (`CobolSharp` → `COBOL.NET`, exe `cobol.exe`).
  - **PROGRESS — Stage 0/1 slice 1 LANDED (DEVLOG 394):** ☑ the numeric substrate is in (`src/CobolSharp.Runtime/
    Numeric/`): `CobolRounding` (8 ISO modes), **`CobolDecimal`** (the exact `BigInteger` base-10 carrier — the
    owner-gated substrate), `NumProfile` (runtime numeric descriptor + `FromDescriptor` bridge), **`CobolNum`**
    (`ScaleAndRound`/`TryStore`: scale→round→capacity→SIZE-ERROR, never throws). ☑ the **Stage-1 differential
    oracle** proves `TryStore` bit-identical to the legacy byte pipeline across field×value×8-mode (DISPLAY/COMP/
    COMP-3/COMP-5 · signed/unsigned · leading-/trailing-P · 10–18-digit mid-range), with an independent
    `BigInteger`/two's-complement reference beyond the legacy faithful window (19–31 digits, 8-byte unsigned COMP-5,
    COMP-5 signed extreme, PROHIBITED). All additive — nothing in the pipeline calls it yet. The oracle surfaced &
    fixed **2 legacy spec bugs** (unsigned COMP-3 sign nibble; trailing-P `WouldOverflow` divide). Guard 1144/481/364.
  - **PROGRESS — Stage 1 first wiring LANDED (DEVLOG 395):** ☑ `PicRuntime.StoreArithmeticResult` (the single
    choke point for ALL COBOL arithmetic — ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE) now delegates its value-level
    scale→round→capacity→SIZE-ERROR decision to `CobolNum.TryStore`; the legacy `WouldOverflow`/`IsInexactAtScale`/
    `CountDigits` removed. The full NIST arithmetic corpus flows through `CobolNum` byte-identically (guard
    1144/481/364). The wiring + guard caught a real **layering correction**: the unsigned-magnitude rule belongs
    to the receiver's *representation* (the encoder), NOT the value-level store — a numeric-edited receiver renders
    its sign via the edit pattern and needs the signed value; `TryStore` now returns the signed rounded value
    (ADR §5 updated).
  - **PROGRESS — Stage 1 value-level scale/round COMPLETE (DEVLOG 396):** ☑ `ApplyScalingAndRounding` (the MOVE /
    numeric-edited / ACCEPT-to-numeric / MOVE-literal / DIVIDE-REMAINDER paths, 8 call sites) now delegates to
    `CobolNum.ScaleAndRound`; the legacy decimal `RoundToIntegerByMode`/`NearestTowardZero` retired. **Every**
    value-level numeric scale/round in the runtime now flows through `CobolNum` (arithmetic via `TryStore`,
    MOVE/edited/remainder via `ScaleAndRound`). Guard 1144/481/364, green first try (the 395 sign-layering fix held).
  - **PROGRESS — Stage 2 classifier Phase A LANDED (DEVLOG 397):** ☑ `RecordClassificationPass` (ADR §3 — the
    typed-vs-byte brain) — Phase-A data-division triggers (REDEFINES/RENAMES/FD-record/LINKAGE/EXTERNAL-GLOBAL/
    edited) + REDEFINES-class & downward-transitivity fixpoint; `Classify(items, categoryOf)`, default typed /
    "any doubt → byte". Additive + unit-tested (15), **not yet consumed by codegen** (Stage 2 = all byte-backed).
    2-lens/17-agent review: 0 confirmed / 15 refuted (Phase-A verified correct). Pipeline investigation map
    captured in DEVLOG 397 (IrLocation hierarchy @ `IrInstruction.cs:1246+`, the dispatch points, insertion
    point after `StorageLayoutComputer`).
  - **PROGRESS — Stage 2 classifier Phase B + Phase C LANDED → the classifier is COMPLETE (DEVLOG 398):** ☑ the
    `ProcedureScanner` bound-tree walk marks the use-observable triggers — (3) refmod base (demote unless a single
    elementary `string`-typed item: Alphanumeric/National/Alphabetic), (11) `CALL … BY REFERENCE` arg base
    (unconditional), (15) ODO whole-group operand, (4a) group-MOVE destination (demote unless an unsubscripted
    identical-layout source → a Phase-C struct-copy edge); group COMPARE/class-condition/CORR deliberately do NOT
    demote (materialize-on-demand / per-field, ADR §3.4). ☑ Phase C = one combined fixpoint over the structural
    closure + the struct-copy edges (byte on either edge end demotes both; monotone, terminates). Three triggers
    documented-deferred: (14) write-pattern perf peephole, (6) ADDRESS OF (unbound), (16) USE FOR DEBUGGING
    (`BoundUseStatement` stub; ADR §12). `SameLayout` uses the real `StorageLocation` offsets (available — layout
    runs before the bound tree, `Compilation.cs:123` vs `:142`). 4-lens adversarial review: **1 confirmed (low,
    the doc-only #16 note) / ~14 refuted**. +16 unit tests. Additive — not yet consumed by codegen. Guard
    1175/481/364.
  - **PROGRESS — Stage 0 character substrate `CobolString` LANDED (DEVLOG 399), guard 1184/481/364:** ☑
    `src/CobolSharp.Runtime/Text/CobolString.cs` — the typed-string analogue of `CobolNum`: COBOL alphanumeric
    MOVE value semantics (`Store` — width/justify/space-fill, ISO §14.9.25/§13.18.36) + ordinal space-extended
    comparison (`Compare`, ISO §8.8.4.2.7 r2) + the Latin-1 `IDataSlot` boundary codec (`FromWindow`/`ToWindow`, ADR
    R10/§2.5). ☑ a **differential oracle** (`CobolStringDifferentialTests`, +9) proves it byte-identical to the
    legacy `StorageHelpers` path (MOVE over binary/LOW-/HIGH-VALUE × widths × left/justified; window round-trip;
    compare sign-identical). Additive/unwired (guard green by construction). Surfaced (and deliberately left for
    the wiring step) a legacy nuance: `CompareFieldToField` uses `TrimEnd()` (all-whitespace) whereas COBOL
    space-extends with `0x20` only — `CobolString.Compare` is COBOL-correct; the two diverge only on non-space
    trailing bytes.
  - **OWNER DECISION (DEVLOG 400): build the REAL record-`struct` storage substrate (the ADR §9 literal intent).**
    A first-flip design investigation found the ADR's nominal typed home — records → .NET `record struct` via
    `RecordLayoutBuilder` — is **dead code** (`IrLoadField`/`IrStoreField` zero producers; `ARCHITECTURE_ASSESSMENT`
    item 17); live storage is `ProgramState` byte[] via `StorageLayoutComputer`. The owner chose to build the real
    record-`struct` substrate (Option B) over a parallel-static-field shortcut and explicitly over the dual-write/
    shadow hack (rejected, `feedback_change_the_dispatch_not_the_callers`). **Key insight:** byte-backed items stay in the byte
    areas (the §1.6 safety floor); only classifier-Typed items move into record-`struct` fields; they meet only at
    the §2.5 `IDataSlot` chokepoint (typed slots materialize a transient byte window — no shadow/drift). The
    concrete, staged, guard-green engineering roadmap is **`docs/RECORD_STRUCT_STORAGE_DESIGN.md`** (S0 IR+resolver
    scaffolding → S1 wire classifier OFF → S2 rebuild `RecordLayoutBuilder` as the real producer → S3 first
    all-character `01`→`record struct` flip → S4+ widen one rule at a time, each a conformance-tested guard-green
    commit, kill-switch `EnableTypedFields` reverts to byte-identical). ADR §9 corrected to match.
  - **PROGRESS — substrate S1 LANDED (DEVLOG 401), guard 1187/481/364:** ☑ the staged design passed adversarial
    review (verdict GO; 3 minor clarifications folded). ☑ `Binder.Bind` now runs the complete classifier (A+B+C)
    on **every** program, stores it on `LoweringContext.Classification`, and validates it via a new permanent
    `RecordClassification.ValidateInvariants()` fail-fast net (typed⇒typed-REDEFINES-target; typed⇒typed-parent).
    Not yet consumed by codegen → byte-identical; but it exercised Phase B's walker across the whole corpus (first
    contact with every real bound-tree shape) and passed first run. +3 unit tests.
  - **PROGRESS — S3a: the FIRST typed character flip LANDED (DEVLOG 403), guard 1187/483/364:** ☑ a standalone
    elementary alphanumeric/national/alphabetic WS item the classifier marks typed (no OCCURS/figurative/triggers)
    is now stored as a native static `.NET string` field, **byte-identical** to the byte path, gated by
    `EnableTypedFields` (default OFF → whole corpus byte-identical; a flag-ON `TypedFieldFlipTests` drives + pins the
    typed path). Path: `Binder.CollectTypedFields` → `IrModule.TypedFieldDefs`/`LoweringContext.TypedFieldRefs` →
    `LocationResolver`→`IrTypedFieldLocation` → `CilEmitter` static field + `InitializeState` init → `CilDataEmitter`
    typed MOVE-literal (`CobolString.Store`) + DISPLAY (`.TrimEnd()` to match `GetDisplayString`) cells; any other
    op on a typed field throws loudly in `EmitLocationArgs` (no silent miscompile). The byte-identity test caught +
    fixed a real DISPLAY-trim divergence.
  - **PROGRESS — S3a follow-on + S3b LANDED (DEVLOG 404–405), guard 1187/485/364:** ☑ typed↔typed field MOVE
    (404); ☑ **S3b — an all-character `01` group → a real .NET `record struct`** of `string` members + a static
    instance, member access `ldsflda instance; ldfld/stfld member` (405) — the owner's Option B realized,
    byte-identical, gated `EnableTypedFields`. `Binder.CollectTypedFields` group pass → `IrModule.TypedRecordDefs`;
    `IrTypedFieldLocation.InstanceName` (null=flat S3a / set=struct member); `CilEmitter` emits the struct type +
    instance + member init; the `CilDataEmitter` typed cells (MOVE-literal / field-MOVE / DISPLAY) share
    flat-vs-struct helpers. Reused `CilEmitter.DefineType`'s value-type shape. +4 `TypedFieldFlipTests`.
  - **PROGRESS — character flip COMPLETE; numeric flip STARTED (DEVLOG 404–410), guard 1196/490/364:** ☑ all
    common character ops byte-identical — MOVE (all typed/byte pairs, incl. the §2.5 materialize boundary), DISPLAY,
    COMPARE, class conditions, figurative SPACE/ZERO (S3a–c, 404–409). ☑ **S4 first numeric flip (410):** an
    unsigned-integer DISPLAY item with a VALUE → a native `.NET long` (VALUE init / MOVE-literal w/ truncation /
    DISPLAY via `CobolNum.FormatUnsignedDisplay`), byte-identical, gated. ☑ owner reaffirmed the full Stage 5
    (Roslyn backend + Cecil oracle, default deferred; ADR §12 Q#2). The sender-materialize
    (`EmitLocationArgsMaterializingTyped`) is the §2.5 read floor; the loud `EmitLocationArgs` guard catches any
    unhandled typed op.
  - **PROGRESS — numeric flip COMPLETE (DEVLOG 411–418), guard 1196/496/364:** ☑ sender-materialize + native
    numeric **COMPARE** / `IS NUMERIC` (413); ☑ numeric **arithmetic** ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE/REMAINDER
    via a receiver prologue/epilogue (`EncodeNumeric`→scratch→op→`DecodeNumeric`-writeback), byte-identical (414); ☑
    digit-count/byte-width decoupling (415) → **COMP/BINARY** usage (416, COMP-5/float/packed excluded) + a
    `--typed-fields` CLI flag; ☑ **signed/scaled → `decimal`** across VALUE/MOVE/DISPLAY(sign-overpunch)/COMPARE/
    arithmetic (417) + decimal field→field MOVE all long/decimal combos (418). Probe-driven `MOVE ZEROS`-to-numeric
    invalid-IL fix (420).
  - **PROGRESS — record-struct numeric members + OCCURS + nested groups (DEVLOG 419–427), guard 1196/506/364:** ☑
    record structs gain `long`/`decimal` members (419); ☑ **OCCURS** char tables → `string[]` (422, the first
    cross-cutting increment — abstract `IrTypedLocation` base + `IrTypedElementLocation` + 3 generalized value-access
    primitives + classifier whole-table demotion §9.3) and numeric tables → `long[]`/`decimal[]` (425, unblocked by
    424); ☑ PERFORM VARYING over a typed table works, SEARCH safely stays byte (426); ☑ **nested groups → nested
    `record struct`s** via a member-path walk (427). **BYTE-ENGINE CONFORMANCE FIX (424):** a VALUE on an OCCURS item
    now initializes EVERY occurrence (ISO §13.18.63.4 GR 9 — was only the last); zero baseline shifts; conformance
    test `tests/conformance/2002/table_value_occurs`.
  - **PROGRESS — Stage-3 "definition of done" (DEVLOG 428), guard 1196/507/364:** ☑ a representative ordinary
    business program flips its **WHOLE data division** (nested group + numeric OCCURS table + standalone longs;
    DISPLAY/decimal-ADD/MOVE/PERFORM-VARYING/COMPUTE/IF) and is byte-identical (`RepresentativeBusinessProgram_…`).
    **The core data model (Stage 3) is COMPLETE:** character→`string`; numeric→`long`/`decimal` (DISPLAY/COMP/BINARY,
    all ops); flat + nested groups→`record struct`s; fixed OCCURS (char+numeric)→`T[]`; every byte-trigger correctly
    stays byte. 24 flag-on≡flag-off differential tests; all gated behind `EnableTypedFields` (default OFF).
  - **PROGRESS — Stage-4 pointers slice 1b BOUNDARY 1 LANDED (DEVLOG 433), guard 1196/507/364:** ☑ a `USAGE POINTER`
    item is now ALWAYS a `static ManagedPointer _PTR_<name>` field (always-on `Binder.CollectPointerFields`, NOT
    `EnableTypedFields`-gated); `StorageLayoutComputer` skips its WS layout; `SET p TO NULL/q` →
    `BoundSetPointerStatement` → `IrPointerStore`; `IF p = q/NULL` → `IrPointerCompare` (address identity:
    `ReferenceEquals(Buffer)&&Offset==`, NOT struct `Equals`); the 8-byte handle paths (`CilDataEmitter` pointer-MOVE,
    `StorageLocation` pointer-PIC synth) DELETED. `pointer_data` green on the new path; rest of corpus byte-identical
    (the new paths early-return for non-pointer programs). New IR `IrPointerStore`/`IrPointerCompare`/`IrPointerFieldDef`.
  - **PROGRESS — Stage-4 pointers slice 1b BOUNDARY 2 LANDED → slice 1b COMPLETE (DEVLOG 434), guard 1196/508/364:**
    ☑ `SET p TO ADDRESS OF x` (`ManagedPointer.CreateByReference` over x's byte storage; x demoted to byte via
    classifier **trigger 6**), `SET ADDRESS OF b TO p` (copy p's ref into the BASED item's `_PTR_` field), and
    BASED-deref via a new `IrBasedDerefLocation` (`CilLocationEmitter.EmitBasedDerefArgs` pushes (Buffer,Offset,Length)
    from `_PTR_b` → byte engine reads+writes the pointed-to storage). `DataStatementBinder.BindSetAddress` (both
    grammar alts, token-order discriminated); `LocationResolver` intercepts a whole BASED reference. Conformance
    `tests/conformance/2002/based_pointer.cob`. **Pointer slice 1b COMPLETE — one `ManagedPointer` representation,
    always-typed, no 8-byte handle.**
  - **PROGRESS — Stage-4 pointer slices 2 + 3 LANDED → POINTER SUBSYSTEM COMPLETE (DEVLOG 436–437), guard
    1196/510/364:** ☑ **slice 2** pointer arithmetic `SET p UP/DOWN BY n` (`IrPointerAdjust`, Offset ± n bytes; conf
    `pointer_arith`); ☑ **slice 3** `ALLOCATE {n CHARACTERS | based-item} [INITIALIZED] [RETURNING p]` (`IrAllocate`
    + `ManagedPointer.Allocate`, new lexer tokens ALLOCATE/FREE/INITIALIZED) / `FREE p` (reuses pointer-store-NULL;
    conf `pointer_alloc`). **All pointers are ONE managed `ManagedPointer`, always-typed, no 8-byte handle.** Pointer
    follow-ups (PROGRAM-/FUNCTION-POINTER, pointer in a typed record/table, EC exception checks) tracked in §10.4.
  - **TOOLING (DEVLOG 435): `scripts/guard-fast.sh` — the parallel guard (~3.3 min vs ~11 min), PROVEN byte-identical
    to `scripts/guard.sh` via `scripts/guard-verify.sh`.** Use it for iteration; the serial guard remains the authority.
  - **Stage-4 OO → .NET classes: SLICE 1 DONE (DEVLOG 447)** — CLASS/NEW/INVOKE/OBJECT REFERENCE end-to-end (a
    class is an instance .NET type with per-instance `ProgramState`); see M2-OO-1 (§3.7) + `docs/OO_IMPLEMENTATION_DESIGN.md`.
    **[STALE NEXT — pre-PIVOT; live next steps = `resume-prompt.md`; the cited `OO_IMPLEMENTATION_DESIGN.md` and
    `RECORD_STRUCT_STORAGE_DESIGN.md` were DELETED (DEVLOG 523–524); OO design now = `docs/COBOLNET_OO_DESIGN.md`]**, then
    OO slices 3–6. Then Stage-5 **Roslyn C# backend** (Cecil oracle); Stage-6 finalize + flip-`EnableTypedFields`-
    on-by-default + rename. Substrate runtime ready (`CobolNum`/`CobolString` + oracles). See also
    `docs/RECORD_STRUCT_STORAGE_DESIGN.md` §6/§9/§10 + `resume-prompt.md`.
- **Owner success criterion: every currently-passing test stays green at 100% throughout — fix bugs as the
  migration surfaces them. Run autonomously, with maximal parallelism** (parallel design/audit agents are fine;
  do the compiler edits themselves directly on `main`, NOT in worktree-isolated workflows — they branch stale).
- **Resolve these owner-gated decisions at the stage that needs them** (ADR §12): **#1 numeric substrate =
  `BigInteger` (not `decimal`) for 19–31-digit values + intermediates — REQUIRED before Stage 1**; the
  classifier-trigger completeness (CALL…BY REFERENCE / LINKAGE / refmod-of-numeric-DISPLAY / group-with-COMP /
  PROGRAM COLLATING SEQUENCE) must be in place before Stage-3 typed flips; and the four tracked completeness
  investigations (USE FOR DEBUGGING, EXTERNAL memory model, EXEC SQL/CICS host-var ABIs, Stage-5 oracle
  determinism).
- **Why first:** the ADR opens with "we should have started with this." Every conformance feature added on the
  byte-array model has to be migrated again. Land the data model, keep the suite green, *then* resume the M2/M3/M4
  catalog below on the new foundation.

---

## 1. Current status — DONE (do not re-list as gaps)

- **M1 (COBOL-85): COMPLETE — on BOTH compilers.** Legacy: NIST CCVS85 = 364 baselines green. **GREENFIELD
  (COBOL.NET): Phase 1 closed the COBOL-85 corpus drive (DEVLOG 571–575, 2026-06-11)** — every golden-bearing
  NIST program locked byte-exact in `NistDifferentialTests` (318: NC/ST/RL/IX/IC/SQ/IF/SM/RW/OBSQ); Report
  Writer, collating, the full §15 intrinsic catalog (with edition windows), COPY/REPLACE, LINAGE, and file I/O
  (incl. variable-length records, EXTERNAL/GLOBAL FDs, the Tier-C record codec) all typed-native. ⚠ The ☑
  marks in §3 below record LEGACY-era implementations of post-85 features — each still re-lands on the
  greenfield substrate during G7 (the legacy code is the proven semantic reference, not the product).
- **M2 (COBOL-2002): in progress.** Landed this drive (DEVLOG 353–369):
  - **WS-2002-FORMAT — COMPLETE:** `*>` inline comments; `>>SOURCE FORMAT IS FREE|FIXED`; conditional compilation
    `>>DEFINE` / `>>IF`/`>>ELSE`/`>>END-IF` / `>>EVALUATE`/`>>WHEN`/`>>END-EVALUATE`; recognize-and-ignore of the
    other standard `>>` directives.
  - **OPTIONS paragraph** — parsed/accepted (clauses **not yet applied** — see M2-ARITH-1).
  - **REPOSITORY paragraph** — parsed/accepted (specifiers **not yet bound** — see M2-UDF-4).
  - **User-defined functions — CORE:** `FUNCTION-ID … END FUNCTION` units (compiled as callable programs);
    `FUNCTION user-name(args)` invocation as the **whole source of a MOVE/COMPUTE** (any arity, numeric **and**
    alphanumeric).
  - **CALL … RETURNING** into WORKING-STORAGE (the LINKAGE-location wiring fix).
  - **SORT Format-2** elementary self-key; **READ … PREVIOUS** (indexed + relative).
  - Intrinsic-function set already broad (incl. 2002/2014/2023 fns in `IntrinsicFunctions.cs`); EXIT
    PARAGRAPH/SECTION/PERFORM[ CYCLE] grammar present.
- **M2 — landed THIS session (DEVLOG 370–382), each with a `tests/conformance/2002/` test + this plan ticked:**
  - **Conformance framework + this SSOT** (370–371): the version-conformance corpus (`tests/conformance/<ver>/`,
    auto-discovered by `ConformanceTests`) and this plan document itself.
  - **M4-1** DELETE FILE (373) · **M4-2a** logical XOR / EXCLUSIVE-OR (375).
  - **M2-UDF-1/2** general inline UDF invocation + literal/arith args (372, 374).
  - **M2-DATA-2** FLOAT-SHORT/LONG/EXTENDED (376, 377) · **M2-DATA-1** BINARY-CHAR/SHORT/LONG/DOUBLE (380).
  - **M2-PROC-2** INSPECT … BACKWARD (378) · **M2-PROC-1** INITIALIZE TO VALUE/DEFAULT/FILLER (381).
  - **M2-ARITH-1** ROUNDED MODE — all 8 modes + per-statement `MODE IS` + CORRESPONDING (379, 382); ◐ two
    follow-ups remain (OPTIONS DEFAULT ROUNDED MODE, PROHIBITED→EC-SIZE-TRUNCATION).
  - **M2-DATA-3** National (UTF-16) data — CORE COMPLETE (383): `PIC N`/`USAGE NATIONAL`, `N"…"`, MOVE/VALUE/
    INITIALIZE/SPACE/DISPLAY/compare + national↔alpha/numeric conversion. Conformance `national_data`.
    An adversarial review caught + fixed silent corruption in the first slice before commit.
    **P10 national wave (2026-07-16): FUNCTION DISPLAY-OF (§15.26) + NATIONAL-OF (§15.66) implemented** on the
    greenfield (`CobolIntrinsics.DisplayOf/NationalOf`, riding CONVERT's ONE repertoire translator + the ambient
    EC-DATA-CONVERSION channel). Argument-2 is a one-character SUBSTITUTION character per the 2023 text
    (§15.26.3 r2 / §15.66.3 r2 — the format names no CCSID/codeset facility), so BOTH argument forms are fully
    implemented — nothing staged. The N"…" Latin-1-only literal guard was lifted (full UTF-16 repertoire,
    D-N1); binder SR checks = COBOLNET1546. Conformance `national_intrinsics` (greenfield-only — the legacy
    oracle's DISPLAY-OF is a pass-through approximation).
  - **M2-DATA-4** Boolean data — CORE COMPLETE (386): `PIC 1`/`USAGE BIT`, `B"…"`, MOVE/VALUE/INITIALIZE/ZERO/
    DISPLAY/compare/JUSTIFIED. Conformance `boolean_data`. A 2-agent review caught + fixed VALUE corruption,
    boolean-in-COMPUTE silent-numeric, and a JUSTIFIED spec/dead-code defect before commit. Bit operators deferred.
  - **M2-PROC-6** `GOBACK RETURNING` — DONE (387, dialect-gated 2002+; conformance `goback_returning`); EXIT
    variants verified present/green; only `CONTINUE AFTER` (non-deterministic) deferred.
  - **M2-DATA-5** Pointers **Phase-1 DONE** (389): `USAGE POINTER` (8-byte handle), `NULL`, `SET p TO NULL`/
    `SET p TO q`, `= NULL`/`= q`; pointer↔non-pointer MOVE + VALUE rejected; conformance `pointer_data`. A 2-agent
    review confirmed clean (self-review had caught 3 bugs first). **Phase-2 (ADDRESS OF/BASED/ALLOCATE) →
    MANAGED .NET REFERENCES (the ADR's `ManagedPointer`); the `PointerRegistry` design is REJECTED — SETTLED, NOT
    owner-gated (owner directive, DEVLOG 428). To be done with the data-model migration's Stage 4.**
  - **M2-ARITH-1 follow-up #1** OPTIONS `DEFAULT ROUNDED MODE` applied to bare ROUNDED — DONE (391; conformance
    `options_default_rounded`).
  - **M2-ARITH-1 follow-up #2** ROUNDED MODE PROHIBITED → SIZE ERROR — DONE at the observable level (392):
    `ON SIZE ERROR` now fires on an inexact PROHIBITED result and the receiver is left unchanged (conformance
    `rounded_mode_prohibited`). The named-EC exception object (EC-SIZE-TRUNCATION) + USE framework remains future
    work under M2-PROC-4.
  - **Bug fix (392):** `IS ALPHABETIC` restricted to ISO §8.8.4.4 {A–Z, a–z, space} (was `char.IsLetter`,
    Unicode-wide). Both 392 fixes were surfaced by the data-model ADR review.
  - **🏗 DATA-MODEL RE-ARCHITECTURE designed + adversarially reviewed (393)** — the ADR + review docs (see §0.5).
  - **🏗 DATA-MODEL MIGRATION STARTED — Stage 0/1 substrate (394) + Stage-1 first wiring (395):** the `BigInteger`
    numeric substrate (`CobolRounding`/`CobolDecimal`/`NumProfile`/`CobolNum`) + differential oracle (2 legacy spec
    bugs fixed: unsigned COMP-3 sign nibble, trailing-P `WouldOverflow`); then `StoreArithmeticResult` (all COBOL
    arithmetic) delegated to `CobolNum` byte-identically, which surfaced + corrected the unsigned-sign layering (ADR
    §5). See §0.5 PROGRESS.
- **Guard baseline:** **1159 unit / 481 integration / 364 NIST** (all green; `bash scripts/guard.sh`).
  (Data-model migration this drive (DEVLOG 394–397): Stage-0/1 numeric substrate + differential oracle (394),
  Stage-1 arithmetic + MOVE/edited/remainder wiring (395–396), Stage-2 classifier Phase A + 15 tests (397). All
  additive or byte-identical — no NIST/integration change. Prior tail (392/393): two ADR-review fixes.)
- **[STALE — pre-PIVOT, superseded by `resume-prompt.md`] NEXT UP (2026-06-08, DEVLOG 453):** the data-model migration CORE is done through Stage-4
  (char/numeric/groups/OCCURS + **pointers** — DEVLOG 394–437); Phase A enablers done (CLI dialect verified + CI,
  445–446); **OO slices 1–3a + 3b SUPER done** (M2-OO-1, DEVLOG 447–451). **Owner directive (DEVLOG 453): OO is now
  built TYPED-NATIVE per ADR §7** — object data → per-instance .NET fields, NOT the byte image. **OO-TYPED object data
  DONE for FLAT fields (453 char + 454 numeric):** object CHARACTER → per-instance .NET `string`, NUMERIC → per-instance
  `long`/`decimal` (`CollectTypedFields` always-on for a CLASS via `classFlatOnly`; instance dimension via
  `CilDataEmitter.EmitTypedFieldOwner`; filled the typed-numeric→byte MOVE cell). Verified instance fields via
  Mono.Cecil; proven per-instance by `oo_instance_data` + `OoTests.TypedObjectData_IsPerInstance` + `oo_method_args`
  (two objects accumulate independently). **OO TYPED-NATIVE OBJECT DATA COMPLETE + multi-method + SELF DONE (DEVLOG 453–456):** object data (flat
  char/numeric, GROUP→record struct, fixed OCCURS→array) → per-instance typed .NET fields (byte `State` empty on a
  fully-typed class); a class hosts N `METHOD-ID` units (each its own .NET method + exit-bounded dispatch range;
  per-method paragraph scope fixes CBL3104), `INVOKE SELF` → callvirt incl. polymorphic across INHERITS (COBOL0112
  lifted). Conformance: `oo_instance_data`/`oo_self`/`oo_self_polymorphic`/`oo_object_group`. Adversarially reviewed;
  edge gaps fail LOUD: SECTION-in-method COBOL0116, multi-method-with-params COBOL0117. **Immediate next picks (in
  order):** (a) **subclass own typed OBJECT data** (lift COBOL0113 — emit the subclass's own typed instance fields +
  init in the subclass ctor; needs factoring the typed-field emit/init out of `EmitProgramState`); (b) **per-method
  LINKAGE** (multi-method-with-args — per-method LINKAGE layout + `FindLinkageField`, lifts COBOL0117) + typed-field
  INVOKE args (the typed by-reference round-trip, COBOL0600 today); (c) **FACTORY** (static) methods + `INVOKE Class
  "M"` (slice 4); then OO slices 5 PROPERTY / 6 universal-ref+EC (`docs/OO_IMPLEMENTATION_DESIGN.md` §5). Then the rest of the M2 catalog:
  **M2-FILE-1/2** (SHARING/LOCK, line-sequential), **M2-ARITH-2** (standard/intermediate arithmetic), **M2-PRE-1**
  (preprocessor trio), **M2-UDF-3/4**; then the large subsystems **M2-PROC-4 EC/exceptions → RAISE/RESUME/USE** and
  **M2-PROC-3 VALIDATE**. Then M3, M4. Pick per §4 waves; tick + log here.

---

## 2. Milestones

| Milestone | Standard | Validation |
|---|---|---|
| **M2** | COBOL-2002 | custom corpus `tests/conformance/2002/` (+ the existing SpecFixTests) |
| **M3** | COBOL-2014 | custom corpus `tests/conformance/2014/` |
| **M4** | COBOL-2023 | custom corpus `tests/conformance/2023/` (vs in-repo `specs/ISO_COBOL.md`) |

Order: finish M2, then M3, then M4. Within a milestone, do correctness defects (🐛) and high-value/small items
first; large subsystems (pointers, exceptions, OO) last.

---

## 3. Work-breakdown — the catalog

> ⛔ **The ☑/◐ marks below are LEGACY-era. For GREENFIELD truth read `docs/PHASE4_RECONCILIATION.md`**
> (DEVLOG 610 — the ratified Phase-4 entry audit). Many "done" items (all of §3.2 M2-DATA:
> BINARY-CHAR / FLOAT / NATIONAL / BIT / POINTER; both §3.1 UDF invocation rows) were done in the RETIRED
> byte engine and stage LOUD (COBOLNET0899) in the greenfield — Phase 4 reclaims them. The OO umbrella
> (§3.7, catalogued ◐) is effectively DONE (24 goldens; DEVLOG 600-609). Use the reconciliation table's
> per-track wave sizing, not these marks, to scope work.

Each item: **ID** · feature · spec ref · severity · tractability · current state · recipe (files) · deps.

### 3.1 M2 — User-defined functions (finish the workstream)

- ☑ 🐛 **M2-UDF-1 — General inline UDF invocation (DONE — DEVLOG 372).** `FUNCTION user-name(args)` inside a
  larger expression now evaluates correctly (was silently 0). Implemented via runtime helper
  `CobolProgramRegistry.InvokeNumericFunction` (scratch RETURNING buffer + Resolve + Invoke + DecodeNumeric) so
  the inline emit only builds the args array; a Compilation pre-pass builds the signature registry
  (`SemanticModel.UserFunctionSignatures` = name → RETURNING length+PIC); `IrUserFunctionCall` +
  `ExpressionLowerer` routing. Verified `COMPUTE R = FUNCTION DOUBLER(X) + 1` → 43. Conformance:
  `tests/conformance/2002/udf_inline_expression`. **NOTE:** applies when every argument is a storage location;
  literal/arith args are M2-UDF-2 below.
- ☑ 🐛 **M2-UDF-2 — Literal / arithmetic-expression arguments (DONE — DEVLOG 374).** `FUNCTION FOO(5)` /
  `FOO(A + 1)` now evaluate correctly (were silently 0). The Compilation pre-pass also records each function's
  USING-parameter signatures (`SemanticModel.UserFunctionParameterSignatures`); `IrUserFunctionArg` carries either
  a location (BY CONTENT) or a value + the target parameter's length/PIC; the emit encodes a value argument via
  `CobolProgramRegistry.EncodeFunctionArg` (`PicRuntime.EncodeNumeric`). Verified `DOUBLER(5)` = 10,
  `DOUBLER(4+1)` = 10. Conformance `tests/conformance/2002/udf_value_args`. **The UDF correctness chapter
  (M2-UDF-1 + M2-UDF-2) is closed for numeric functions** — and, in the GREENFIELD (P10 Step 9, DEVLOG 859),
  the **category-carrying RETURNING channel** extends it to alphanumeric / numeric-edited / national /
  image-form-group results (every leaf `DataItem.ElementImageCapable` — character-stored or a pinned numeric
  byte form: zoned DISPLAY, binary, packed, COMP-5, IEEE float, INDEX) (§8.4.3.2.4 GR1; golden
  `udf_returning_categories`, greenfield-only — the frozen legacy carries only numeric results);
  float/boolean/pointer-class + strong-typed/REDEFINES/variable-length groups and a group with a
  pointer/object-class LEAF stay staged loud per-shape (COBOLNET1510, `UdfBinder.UdfReturningResidue`).
  A binary-leaf group was listed among those residues until PB164's F8 widening (kb/Work PB199).
- ☐ **M2-UDF-3 — Separate-compilation user functions (prototypes).** Caller + function in different translation
  units. *Medium.* §8.13 external repository + function-prototype. *Current:* caller and function must share one
  compilation group. *Recipe:* function-prototype definitions in the caller (or an external repository registry).
- ☐ **M2-UDF-4 — Bind REPOSITORY FUNCTION specifiers.** Enable `REPOSITORY. FUNCTION ALL INTRINSIC` (call
  intrinsics without the `FUNCTION` keyword) + named function specifiers. *Medium (parser ambiguity: `name(args)`
  vs subscript).* *Current:* REPOSITORY parses but specifiers are inert. §12.3.8.

### 3.2 M2 — Data types

- ☑ 🐛 **M2-DATA-1 — `USAGE BINARY-CHAR/SHORT/LONG/DOUBLE [SIGNED|UNSIGNED]` (DONE — DEVLOG 380).** §13.18.60.
  4 new `UsageKind` markers (BinaryChar/Short/Long/Double) + `SIGNED`/`UNSIGNED` reserved words + `binarySign`
  grammar; FieldSizeCalculator 1/2/4/8 bytes; `StorageLocation` synthesizes a COMP-5 descriptor (explicit width
  + TotalDigits 3/5/10/19-or-20 + sign); `DataSymbol.IsUnsignedBinary`/`IsFixedWidthBinary`; PicUsageResolver/
  RecordLayoutBuilder/SemanticBuilder threaded. Runtime COMP-5 codec extended to 1-byte (sbyte/byte) — no PIC'd
  COMP-5 is ever 1 byte. Verified BC=127/-128, BCU=255, full-width SHORT/LONG/DOUBLE, COMPUTE. Conformance
  `tests/conformance/2002/binary_usage`.
- ☑ **M2-DATA-2 — `USAGE FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED` (DONE — DEVLOG 376, 377).** Aliased onto
  COMP-1/COMP-2 (3 edits each: lexer tokens + usageKeyword/bare-form alts + `UsageMapper.FromUsageKeyword`
  FLOAT-SHORT→Comp1 / FLOAT-LONG→Comp2 / FLOAT-EXTENDED→Comp2). FLOAT-EXTENDED maps to double (.NET has no
  native 128-bit float — documented approximation; a true soft-float quad path is out of scope). Verified
  10.5*2=21, 100.25*4=401, 250.5*2=501. Conformance `tests/conformance/2002/float_usage` (all three usages).
- ☑ **M2-DATA-3 — National data (CORE DONE — DEVLOG 383).** `USAGE NATIONAL` + `PIC N(n)` (UTF-16, **2
  bytes/char**), `N"…"`/`n'…'` literals, and the full data-movement surface: MOVE national←national
  (left-justify, U+0020 pad, right truncate, JUSTIFIED RIGHT), national↔alphanumeric and numeric→national
  conversion (Latin-1 subset), figurative/literal **VALUE**, **MOVE SPACE / INITIALIZE** (national-space
  fill), **DISPLAY** (UTF-16 decode), and **national comparison** (field=field, field=literal). Single sizing
  pipeline (`ComputeStorageLength` doubles for `IsNationalLike`); `NATLIT` lexer token + `nonNumericLiteral`
  + binder; `USAGE NATIONAL`→`UsageKind.National` (category drives behavior); runtime `WriteNationalChars`/
  `MoveNationalToNational`/`MoveStringLiteralToNational`/`MoveAlphanumericToNational`/`MoveNumericToNational`/
  `MoveNationalToAlphanumeric[Edited]`/`CompareNational`/`CompareNationalToString`/national arm in
  `MoveFigurativeToField`; emit-time category dispatch in `CilDataEmitter`/`CilComparisonEmitter`/`CilEmitter`
  (VALUE). **An adversarial review (3 agents) caught that the first slice silently corrupted national↔alpha
  MOVE, figurative/VALUE/INITIALIZE fill, and comparison — all fixed before commit.** Conformance
  `tests/conformance/2002/national_data` (13 assertions); NATIONAL-EDITED landed 2026-09-12 (kb/Work PB492, `tests/conformance/{2002,2014,2023}/pb492_national_edited`). **Deferred:** `NX"…"`, full
  implementor correspondence + `EC-DATA-CONVERSION` (Latin-1 only), collating-sequence national compare,
  ref-mod ×2 byte adjustment, INSPECT-national.
- ☑ **M2-DATA-4 — Boolean & bit data (CORE DONE — DEVLOG 386).** `PIC 1(n)` / `USAGE BIT` (one byte/position,
  ASCII `'0'`/`'1'`), `B"…"`/`b'…'` literals, and the full data surface: MOVE boolean←boolean (`'0'` fill / right
  truncate / JUSTIFIED), literal/figurative-ZERO **VALUE**, **MOVE ZERO / INITIALIZE** (boolean zero), **DISPLAY**,
  **comparison** (field=field, field=literal), and **JUSTIFIED RIGHT** (§13.18.32 — the fix also un-deadened
  national JUSTIFIED). A 2-agent adversarial review caught + fixed three defects before commit: non-boolean VALUE
  corruption (§13.18.63 GR10 → CBL1002), a boolean nested in a COMPUTE tree read as numeric (→ CBL2601, recursed),
  and JUSTIFIED wrongly rejected on boolean/national (CBL0803). Conformance `tests/conformance/2002/boolean_data`.
  **Deferred:** bit operators B-AND/B-OR/B-XOR/B-NOT (reserved-word collision; needs the XOR wiring pattern),
  `BX"…"`, true bit-packing, GROUP-USAGE BIT, boolean↔non-boolean compare strictness, boolean ref-mod, the broader
  alphanumeric/national-in-COMPUTE latent hole, uninitialized-boolean=0x20 (matches the National precedent).
  - **Investigated first-slice recipe (workflow `m2-data4-boolean-investigation`, 2026-06-05) — mirrors the
    National build (DEVLOG 383) almost exactly.** Corpus-checked clean: NIST has no `PIC 1`, no `USAGE BIT`, no
    real `B"…"` (only `"B"` strings → STRINGLIT). **Storage model:** 1 byte per boolean position holding ASCII
    `'0'`/`'1'` — spec-permitted (§13.18.40.4 R14: a boolean char "may be represented … as an alphanumeric
    character"); simplest correct; true bit-packing (§8.5.1.6.3) deferred. So boolean ≈ "alphanumeric with `'0'`
    fill + a distinct category + no DISPLAY trim". **Touch (mirror National):** (1) `PicDescriptor.cs` add
    `CobolCategory.Boolean` + `IsBooleanLike()` + `UsageKind.Bit`; (2) `PicDescriptorFactory.cs` add a
    `hasBooleanChars` flag + `case '1'` (integerDigits += count) + category lattice (Boolean when only `'1'`);
    sizing = 1 byte/position (default arm, no multiplier); (3) `PicUsageResolver.cs` set `isBool` from category,
    add `'1'` to `IsValidPicSymbol`, map `"BIT"`→`UsageKind.Bit`, route `picString==null && usage==Bit`→Boolean;
    (4) `CobolData.g4` `usageKeyword += BIT`; (5) `CobolLexer.g4` add `BIT` token + `BOOLLIT : 'B' '"' [01]+ '"' |
    'B' '\'' [01]+ '\''` (before IDENTIFIER, maximal-munch safe); (6) `CobolExpressions.g4` `nonNumericLiteral +=
    BOOLLIT`; (7) `ExpressionBinder.BindNonNumericLiteral` BOOLLIT→`BoundLiteralExpression(text, Boolean)` (strip
    `B`+quotes; no doubled-quote); (8) `SemanticBuilder` VALUE-clause BOOLLIT extraction (mirror the NATLIT arm);
    (9) `CategoryCompatibility` add `(Boolean,Boolean)` move pair + `IsBooleanFamily` + comparison case; (10) **the
    full dispatch surface up front (National-review lesson):** runtime `MoveBooleanToBoolean` (`'0'` fill/right
    truncate), `MoveStringLiteralToBoolean`, `CompareBoolean`/`CompareBooleanToString` (`'0'` pad), boolean arm in
    `MoveFigurativeToField` (ZERO/`'0'`), `GetDisplayString` boolean branch (show raw `'0'`/`'1'`, **no TrimEnd**),
    `MoveBooleanLiteralToOccursField` (VALUE); emit dispatch in `CilDataEmitter.EmitMoveFieldToField` +
    `EmitMoveStringToField`, `CilComparisonEmitter`, `CilEmitter` VALUE-init, and INITIALIZE-default in
    `DataMovementLowerer`. **Conformance** `tests/conformance/2002/boolean_data` covering literal/MOVE/VALUE/
    INITIALIZE/DISPLAY/compare. **Defer:** `BX"…"` hex, **bit operators B-AND/B-OR/B-XOR/B-NOT** (corpus-collision
    risk — `B-NOT` seen as an identifier in a unit test; needs careful reserved-word handling + the XOR-operator
    wiring pattern, DEVLOG 375), true bit-packing, GROUP-USAGE BIT, SET cond-name interplay.
- ☑ **M2-DATA-4b — `&` concatenation expressions (GREENFIELD DONE — P10 Step 14, 2026-07-16).** ISO §8.8.3:
  the `&` operator joins alphanumeric (incl. `X"…"` hex) / national / boolean literals and figurative
  constants into ONE literal; §8.8.3.3 GR3 makes the result a compile-time literal usable anywhere a literal
  of its class may appear — implemented in the greenfield `src/Cobol.Net.*` as the `Binding/ConcatFolder.cs`
  fold (NO runtime operator; no emitter leg) — operands include a constant-name and a symbolic-character, and the
  class is folded pairwise per §8.8.3.3 GR1 (kb/Work PB1406) — with the §8.8.3.2 SRs as `COBOLNET1540/1541/1545`
  (a word standing for no literal `COBOLNET2473`; §12.3.7.3 SR11's symbolic-character operand `COBOLNET2474`), the
  `concat-operator-2002` VersionConformancePass parse-arm gate (0900 at `--std 85`), the enabled
  `tests/conformance/2002/literal_concat` golden (+ `concat_below_2002` / `concat_class_mismatch` negatives)
  and the active version-matrix row. Never existed in the legacy oracle (GreenfieldOnly).
- ◑ **M2-DATA-5 — Pointers & based addressing. PHASE-1 DONE (DEVLOG 389); Phase-2 = managed .NET refs, done in the data-model Stage 4 (NOT gated).** **[A] HIGH.**
  **Phase-1 COMPLETE:** `USAGE POINTER` (8-byte opaque handle, no PIC), `NULL`, `SET p TO NULL` / `SET p TO q`,
  `= NULL` / `NOT = NULL` / `= q` equality; pointer↔non-pointer MOVE rejected (CBL0901); `VALUE` on a pointer
  rejected (CBL1002); default INITIALIZE leaves a pointer unchanged. Conformance `tests/conformance/2002/
  pointer_data`. Lean reuse: SET→MOVE, NULL→0x00 figurative fill, `= NULL`→figurative compare, `= q`→
  `IrStringCompare` (no new IR nodes). **Phase-2/3 → MANAGED .NET REFERENCES (the ADR's `ManagedPointer` — GC-tracked
  `Owner`, no native heap / no handle table; the `PointerRegistry` design is REJECTED — SETTLED, NOT owner-gated,
  owner directive DEVLOG 428), to be built in the data-model migration's Stage 4:** `ADDRESS OF` / `SET ADDRESS OF`
  / `SET … UP/DOWN BY` / `BASED` deref → then `ALLOCATE`/`FREE` (M2-PROC-5) — all on `ManagedPointer`, no `GCHandle`
  pinning / native memory.
  Also deferred: ordering-operator rejection on pointers (`< >` compile to a byte-compare today; invalid input),
  PROGRAM-/FUNCTION-POINTER distinctions, and updating/removing the orphaned `LoweringTable` (no Pointer cases).
  **Phase-1 is thin standalone** — every pointer can only be NULL until ADDRESS OF/ALLOCATE exist. §13.18, §14.9.
  - **Investigated first slice (handoff audit, 2026-06-05) — the audit's RECOMMENDED next pick** (foundational +
    smallest Phase-1, no grammar ambiguity). Current state: `UsageKind.Pointer` enum value exists but is **inert**
    (no grammar for ADDRESS OF/SET ADDRESS OF/BASED/NULL, no bound pointer node); `ManagedPointer` exists but is
    used only for CALL BY REFERENCE/CONTENT, not USAGE POINTER. **Phase-1 slice:** `USAGE POINTER` (alias all
    three pointer usages) → 8-byte opaque **handle** storage; `NULL` literal = handle 0; `SET p TO NULL`; pointer
    `= NULL` comparison. (Phase-1 used an opaque 8-byte handle for NULL/equality only.) **Phase-2/3 (the data-model
    Stage 4) → managed .NET references (`ManagedPointer`):** `SET ADDRESS OF` / `SET … UP/DOWN BY`, `BASED` deref, then
    `ALLOCATE`/`FREE` (M2-PROC-5) — all GC-tracked managed refs, NO `GCHandle` pinning / native memory / handle
    table. (PointerRegistry REJECTED; SETTLED, NOT gated — DEVLOG 428.)
  - **Turnkey Phase-1 implementation map (workflow `m2-data5-pointers-p1-investigation`, 2026-06-05).** Phase-1
    (POINTER 8-byte handle + NULL + SET p TO NULL + SET p TO q + `= NULL`/`= q` equality) is standalone; Phase-2
    dereference (ADDRESS OF/BASED) uses **managed .NET references** (`ManagedPointer`) in the data-model Stage 4 — the
    `PointerRegistry` design is REJECTED (settled, NOT gated; DEVLOG 428). NULL = handle 0 = 8 zero bytes. **Spec corrections to the slice above:** (a) **`VALUE NULL` is PROHIBITED on
    pointer items** (§13.18.26 SR9) — do NOT implement it; instead REJECT a VALUE clause on a pointer (mirror the
    boolean VALUE check). (b) Comparison is **equality only** (= / NOT =), operands same category or NULL.
    **Lean approach — reuse existing machinery, minimal new code:** model pointer as `CobolCategory.Pointer`
    (8 bytes); **SET p TO NULL / SET p TO q → lower as a MOVE** in `BindSetToValue` when the target is a pointer
    (NULL figurative → `MoveFigurativeToField` default fills 0x00×8 since `FigurativeToByte(Null)=0x00`; pointer←
    pointer → an 8-byte byte-copy), bypassing the numeric index-set path; **`IF p = NULL` reuses the
    figurative-comparison path** (NULL→0x00 fill, like LOW-VALUE — verify `EmitLocationVsFigurative` handles
    `FigurativeKind.Null`→0x00) and **`IF p = q` reuses `IrStringCompare`** (byte compare, correct for equality on
    equal-length 8-byte handles) — so **no new IR/runtime compare nodes needed**. **Touch:** (1) `PicDescriptor.cs`
    add `CobolCategory.Pointer` (+ optional `IsPointerLike()`); `UsageKind.Pointer` already exists. (2) 8-byte
    sizing at the 4 sites (mirror BinaryDouble=8): `FieldSizeCalculator.ComputeElementSize` (`Pointer`→8),
    `PicDescriptorFactory.ComputeStorageLength` (`case Pointer`→8), `StorageLocation`/`CompilerPicDescriptorFactory`
    synth an 8-byte non-numeric descriptor (category Pointer), `RecordLayoutBuilder.MapToIrType` (`Pointer`→
    ByteArray). (3) `PicUsageResolver.ResolveForDataItem` `picString==null && usage==Pointer`→category Pointer.
    (4) `CobolExpressions.g4` add `NULL_` to `figurativeConstant`; `ExpressionBinder.BindFigurativeConstantExpression`
    `NULL_`→`BoundFigurativeExpression(FigurativeKind.Null)` (both exist; `NULL_` token currently used only in
    `objectReference` — disambiguate by target category, no grammar conflict). (5) `DataStatementBinder.BindSetToValue`
    pointer-target → emit a `BoundMoveStatement` (+ reject SET p TO a non-pointer/non-NULL). (6) `CilDataEmitter
    .EmitMoveFieldToField` `(Pointer,Pointer)`→8-byte byte copy; ensure `MoveFigurativeToField(Null)` over a pointer
    fills 0x00×8. (7) `CategoryCompatibility` add `(Pointer,Pointer)` move + comparison + `IsPointerFamily`.
    (8) `ConditionLowerer` route pointer Location/Location → `IrStringCompare`; handle pointer vs NULL figurative.
    (9) `DataItemClassifier.ValidateValueClause` REJECT a VALUE on a pointer item. (10) restrict pointer comparison
    to = / NOT = (reject ordering). Conformance `tests/conformance/2002/pointer_data` (declare, SET TO NULL, = NULL
    YES, two NULLs equal). **Note:** Phase-1 is thin standalone (every pointer can only be NULL until ADDRESS OF/
    ALLOCATE exist) — its value is the foundation; the useful Phase-2 (dereference) is the owner-gated design decision.

### 3.3 M2 — Arithmetic & configuration

- ◑ 🐛 **M2-ARITH-1 — `ROUNDED MODE` (all 8 modes DONE — DEVLOG 379).** All eight modes (AWAY-FROM-ZERO,
  NEAREST-AWAY-FROM-ZERO, NEAREST-EVEN, NEAREST-TOWARD-ZERO, PROHIBITED, TOWARD-GREATER, TOWARD-LESSER,
  TRUNCATION) implemented in `PicRuntime.RoundToIntegerByMode` + per-statement `ROUNDED MODE IS …`
  (new mode reserved words, `roundedPhrase` grammar, `BindRounded`, `BoundArithmeticTarget.RoundingMode`,
  lowerer threads the mode int — IR/emit were already mode-agnostic). Conformance
  `tests/conformance/2002/rounded_modes`; CORRESPONDING `ROUNDED MODE` also DONE (DEVLOG 382,
  `tests/conformance/2002/corresponding_rounded_mode`). **Follow-ups:** (1) ☑ **DONE (DEVLOG 391)** — OPTIONS
  `DEFAULT ROUNDED MODE` now sets the bare-ROUNDED default: `SemanticBuilder.VisitOptionsParagraph` token-scans
  the OPTIONS blob for `DEFAULT ROUNDED [MODE] [IS] <mode>`, `Compilation` transfers it to
  `SemanticModel.DefaultRoundingMode`, `BindRounded` (now instance) returns it for a bare ROUNDED; per-statement
  `MODE IS` still overrides. Conformance `options_default_rounded`. (2) ☐ PROHIBITED → raise EC-SIZE-TRUNCATION
  on an inexact result — **blocked on the EC framework (M2-PROC-4)**; needs the arithmetic store paths to detect
  precision loss. §14.9.4, §11.9.6.
- ◑ **M2-ARITH-2 — Apply remaining OPTIONS clauses.** `DEFAULT ROUNDED MODE` is now **applied** (DEVLOG 391, see
  M2-ARITH-1 follow-up #1 — the OPTIONS token-scan + `SemanticModel.DefaultRoundingMode` infrastructure is in
  place). Still recognize-and-ignore: `ARITHMETIC IS STANDARD/STANDARD-BINARY/STANDARD-DECIMAL`,
  `INTERMEDIATE ROUNDING`, `FLOAT-BINARY/DECIMAL DEFAULT`, `ENTRY-CONVENTION`. *Medium–large* (standard arithmetic
  is a real intermediate-precision change). §11.9.

### 3.4 M2 — Procedure-division statements

- ☑ 🐛 **M2-PROC-1 — `INITIALIZE … TO VALUE` / `THEN TO DEFAULT` / `WITH FILLER` (DONE — DEVLOG 381).** §14.9.20.
  Binder now captures all three phrases (`BoundInitializeStatement.WithFiller/ToValue/ToValueCategory/ToDefault`);
  the lowerer applies per-item precedence TO VALUE → REPLACING → default, includes FILLER under WITH FILLER, and
  suppresses the default for non-VALUE items under TO VALUE unless TO DEFAULT. `EmitValueClauseInit` emits each
  item's declared VALUE (figurative / ALL-literal / numeric / string). Conformance
  `tests/conformance/2002/initialize_phrases`.
- ☑ **M2-PROC-2 — `INSPECT … BACKWARD` (DONE — DEVLOG 378).** §14.9.21. New `BACKWARD` reserved word +
  grammar `INSPECT BACKWARD? …` + a `bool Backward` threaded Bound→IR→emit→runtime. Implemented as a
  reverse-wrapper: reverse the target + every multi-char operand/delimiter, run the existing forward
  TALLYING/REPLACING/CONVERTING cycle, reverse the result buffer back (TALLYING needs none; FROM/TO sets
  unreversed). Verified backward = exact forward mirror (FIRST→rightmost, LEADING→trailing-run, BEFORE-region
  flips side). Conformance `tests/conformance/2002/inspect_backward`.
- ☐ **M2-PROC-3 — `VALIDATE` statement + validation clauses.** CLASS/DEFAULT/DESTINATION/INVALID/PRESENT WHEN/
  VARYING + error handling. *Large (new facility).* §14.9 VALIDATE, §13.
- ☐ **M2-PROC-4 — Exception handling: `RAISE`, EC framework, `>>TURN` runtime checking, `RESUME`,
  `USE … AFTER EXCEPTION`.** **[A] medium, large.** *Recipe:* an EC-condition catalog, a TURN on/off map, a
  condition register, and runtime guards on checked operations. **Backbone for RAISE/RESUME/USE and
  exception-checking PERFORM.** §14.6, §14.9.
- ☑ **M2-PROC-5 — `ALLOCATE` / `FREE` (based storage).** *Medium.* **Depends on M2-DATA-5 (pointers).** §14.9.
  **LANDED greenfield (DEVLOG 617; the §14.9.3 GR7 based-INITIALIZED lowering completed P10 Step-6 slice,
  2026-07-16, DEVLOG 858):** both formats + FREE on `ManagedPointer`/`StorageCell`; INITIALIZED = GR6
  binary-zero fill (CHARACTERS) / the GR7 `INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT` expansion
  (based form, the ONE InitializeBinder mechanism); goldens `2002/pointer_alloc` + `2002/allocate_initialized`.
- ◑ **M2-PROC-6 — `GOBACK RETURNING` (DONE — DEVLOG 387); EXIT variants verified; `CONTINUE AFTER` deferred.**
  §14.9.16. `GOBACK RETURNING|GIVING identifier` (dialect-gated 2002+) lowers to a synthetic MOVE into the
  PROCEDURE DIVISION RETURNING item + `IrGoBack` (reuses the CALL…RETURNING wiring); conformance
  `tests/conformance/2002/goback_returning`. EXIT PROGRAM/PERFORM[ CYCLE]/SECTION/PARAGRAPH/METHOD/FUNCTION
  grammar present and EXIT PROGRAM/PERFORM green across NIST — no gap. **Remaining:** `CONTINUE AFTER expr SECONDS`
  (2002 timed delay) — deferred (non-deterministic, poor conformance fit, low value).

### 3.5 M2 — Preprocessor robustness & directives

- ◐ **M2-PRE-1 — Preprocessor robustness trio.** *medium.* **RE-SCOPED 2026-06-05 by an empirical
  repro (workflow `m2-pre1-investigation`): the "crashes the parser" claim is REFUTED** — nothing crashes
  (all exits 0/1, never an ICE); these are clean-error / mis-parse defects on *rare* 2002 constructs, so the
  severity is below a silent-wrong-result 🐛.
  - **(a) mid-file `>>SOURCE FORMAT` switching — MIS-PARSE (real).** `ReferenceFormatProcessor` honors only the
    FIRST directive, whole-file ("first wins", `ReferenceFormatProcessor.cs:86-111`). A genuinely fixed-form
    region after a `>>SOURCE FORMAT IS FREE` keeps its column-1-6 sequence numbers → stray tokens; a later
    FIXED switch is silently ignored. Spec **§7.3.24.3 R1/R5**: a SOURCE FORMAT directive governs the text
    *following it until the next directive* (and reverts at end of COPY'd library text). Fix needs **per-line
    format state** in the normalizer (or a new region-wise pass). Non-trivial.
  - **(b) conditional-compilation directives inside copied library text — CLEAN-ERROR (real).** `Compilation.cs:
    279-301` runs `ConditionalCompilationProcessor` (285) BEFORE `CopyProcessor` (295), so `>>IF`/`>>DEFINE` in a
    copybook pass through as text → stray `>` parse error. Spec **§7.2** order is: COPY *inclusion* (Step 1) →
    DEFINE/IF/EVALUATE (Step 2a) → COPY REPLACING (2d) → REPLACE (Step 3), i.e. **COPY-inclusion-then-CC**. Fix =
    swap so COPY runs before CC (low NIST risk — CC is a no-op on directive-free source). **CAUTION: the current
    order is a DELIBERATE decision** (its comment cites §7.3.16 GR1 to let `>>IF` gate a COPY); reconcile
    §7.3.16 vs §7.2 first — gating still works COPY-first (the expanded text falls in the IF false path, §7.2
    line 3314 "may be omitted"). Verify the `conditional_compilation` conformance test + add a copybook-CC test.
  - **(c) unknown/`>>`-directive handling — ALREADY CORRECT (no fix needed).** Known-ignored directives are
    consumed; an unknown `>>X` surfaces as a clean COBOL0001 by design (typo-catching). Spec §7.3.3/§7.3.4 leave
    unknown-directive behavior implementor-defined (only `>>IMP` reserved). The proposed directive-skip lexer
    rule is at most diagnostic polish — **dropped from the trio**.
- ☐ **M2-PRE-2 — Directive semantics depth.** Does `>>TURN` actually toggle EC checks (pairs with M2-PROC-4); is
  recognize-and-ignore of LISTING/PAGE/LEAP-SECOND/PROPAGATE/FLAG-* conformant; `>>DEFINE … AS PARAMETER` (env
  source); `>>CALL-CONVENTION` effect. *Low–medium.*

### 3.6 M2 — File & I/O (2002+ deltas only; the 85 file I/O is done)

- ☐ **M2-FILE-1 — `SHARING`, `LOCK MODE`/`LOCK ON`, `RETRY`.** *Medium.* §13 FILE-CONTROL, §14.9 OPEN.
- ☐ **M2-FILE-2 — Line-sequential organization** + 2002 FILE STATUS codes. *Medium.*

### 3.7 M2 — Object-Oriented COBOL (the single largest sub-project)

- ◐ **M2-OO-1 — OO COBOL. TYPED-NATIVE object data COMPLETE + multi-method + INVOKE SELF DONE (DEVLOG 447–456);
  subclass-own-data / per-method-LINKAGE / FACTORY / PROPERTY / universal-ref+EC remain.** Per ADR §7 a class's object
  data is per-instance typed .NET fields (flat `string`/`long`/`decimal` 453–454, group→`record struct` + OCCURS→array
  456; byte `State` empty on a fully-typed class), N `METHOD-ID`/class each its own .NET method with exit-bounded
  dispatch + `INVOKE SELF`→callvirt incl. polymorphic (455, COBOL0112 lifted); deferred edges fail loud (COBOL0116
  SECTION-in-method, COBOL0117 multi-method-with-params, COBOL0113 subclass-data). Conformance: 9 `oo_*` + `OoTests`.
  *(Historical detail of the earlier slices 1–3b below.)* Grammar DONE (`Core/CobolOO.g4`,
  `{is2002()}?`-gated, DEVLOG 439). **Slice 1 (CLASS-ID + single OBJECT method + NEW + no-arg INVOKE + OBJECT
  REFERENCE storage) is implemented end-to-end** — a class is an INSTANCE .NET reference type with a per-instance
  `ProgramState` (`EmissionContext.StateIsInstance` chokepoint), `INVOKE class "NEW" RETURNING o` (`newobj`+`.ctor`),
  `INVOKE o "method"` (`callvirt`, static cross-type resolution, classes emitted before programs), `USAGE OBJECT
  REFERENCE class` → a `_OBJ_` managed reference, and PERFORM inside an OBJECT method. Conformance
  `tests/conformance/2002/oo_hello` + `oo_method_perform`. Adversarial-reviewed (non-OO corpus byte-identical; 2
  latent landmines fixed: instance-State LOCAL-STORAGE load + receiver-less PERFORM). Turnkey map +
  per-slice scope in `docs/COBOLNET_OO_DESIGN.md`. **Slice 2 (DEVLOG 448): `INVOKE …
  USING/RETURNING`** — OO methods take USING args + a RETURNING value via the CALL ManagedPointer[] ABI (an OO method
  is an instance "Entry"); per-instance state proven (`oo_method_args`: two objects accumulate independently).
  **Slice 3a (DEVLOG 450): `INHERITS FROM` + virtual methods + polymorphism** — a subclass extends a base (`class
  Sub : Base`), inherits the root's per-instance State (ctor-chains), and overrides base methods; invoking through a
  base-typed reference dispatches to the override (`oo_inherit`: GENERIC/WOOF/WOOF). Deferred forms fail loudly
  (subclass own data → COBOL0113; INVOKE SELF → COBOL0112; root-class SUPER → COBOL0115; unknown base → COBOL0114).
  **Slice 3b (DEVLOG 451): INVOKE SUPER** — an override calls the base class's method non-virtually (`oo_super`).
  **Remaining (each its own guard-green + conformance-tested slice):** slice 3b subclass own OBJECT data (extend
  the shared State) + multi-method classes + `INVOKE SELF`; slice 4 `FACTORY` (static) methods/data; slice 5 `PROPERTY` (GET/SET); slice 6 polymorphism
  + universal object reference + `EC-OO-*`; plus multi-method classes (a richer per-class IrModule) and per-instance
  OBJECT REFERENCE / typed-native OBJECT fields. INTERFACE-ID is post-slice-6. §11 OO source units, §14.9.23 INVOKE.

### 3.8 M3 — COBOL-2014

- ☑ **M3-1 — Dynamic-capacity tables** `OCCURS DYNAMIC [CAPACITY IN dn] [FROM…TO]`. *Medium–large.* **DONE**
  (data-model D9, DEVLOG 652–657 — all 5 increments + adversarial review; note: 2014 feature but the corpus lives
  under `tests/conformance/2014`).
- ◑ **M3-2 — `TYPEDEF` / `SAME AS` / `TYPE TO`.** *Medium.* **TYPEDEF + the `TYPE` clause DONE** (data-model D17,
  DEVLOG 659–663 — inc 1 weak-TYPE spine, inc 2 STRONG typing, inc 3 level-88s, inc 4 staged-loud residue; goldens
  `typedef_weak_elem`/`typedef_weak_group`/`typedef_strong_ok`/`typedef_88`/`typedef_indexed`). **`SAME AS` DONE**
  (P10 Step 16 — §13.18.49 on the ONE `CloneItem`/`ExpandSameAs` machinery; the `AS` token landed with the CONSTANT
  wave; golden `typedef_same_as`, matrix row `same-as-clause-2002` ACTIVE, SR bands 1555/1556/1557). **EXTERNAL type
  declarations DONE** (P10 Step 16 — §13.18.22 GR2/GR3/SR5 = 1558; the 1534 stage lifted; golden `typedef_external`).
  **DEFERRED:** the RESTRICTED data-pointer `USAGE POINTER TO type-name` (ISO §13.18.60.2 general format / Annex
  D.9.2.2 — the P12 re-scout correction: plain `TYPE TO` is the TYPE clause's OPTIONAL word, §13.18.57.2 Format 1
  `TYPE [TO] type-name`, already covered by the live TYPE clause; the genuine "pointer-target" deferral is the
  restricted `USAGE POINTER [TO type-name]` form). Catalogued as the PENDING matrix row `usage-pointer-to-type-2014`.
  Diagnostic band 15xx = 1529–1533, 1535, 1555–1558.
- ⛔ **M3-3 — JSON & XML** `JSON GENERATE/PARSE`, `XML GENERATE/PARSE` + special registers (XML-CODE/JSON-CODE/…).
  **NON-ISO — OUT OF THE ISO-CONFORMANCE SCOPE.** `specs/ISO_COBOL.md` (the ISO/IEC 1989:2023 text) has ZERO `JSON`/
  `XML` occurrences — these are IBM Enterprise-COBOL vendor extensions, NOT part of the standard (confirmed 2026-07-07,
  DEVLOG 664; consistent with the pre-P2.5 constructs.json scrub that moved JSON/XML to vendor-dialect post-G8). Do
  NOT implement under the "100% ISO" mission; it belongs to a later vendor-dialect track. *Large* if/when done. The
  legacy `CobolParserJsonXml.g4` overlay is dead/unwired.
- ☐ **M3-4 — File sharing/locking finalization** (if not folded into M2-FILE-1), function/method pointers,
  IEEE-754 alignment, increased limits, conditional-expression enhancements.

### 3.9 M4 — COBOL-2023

- ☑ **M4-1 — `DELETE FILE` execution (DONE — DEVLOG 373).** Full statement pipeline (BoundDeleteFileStatement →
  IrDeleteFile → FileRuntime.DeleteFile = ResolveHostPath + File.Delete + I-O status 00/35/30). Verified status
  35 after delete + the physical file removed. Conformance `tests/conformance/2023/delete_file`. **Follow-up:**
  the `ON EXCEPTION`/`NOT ON EXCEPTION` phrases + multiple file-names are not yet honored.
- ☑ **M4-2a — logical `XOR` / `EXCLUSIVE-OR` operator (DONE — DEVLOG 375; RE-EDITIONED to 2023 — DEVLOG 596).**
  New `logicalXorExpression` grammar level (precedence NOT>AND>XOR>OR), `BoundBinaryOperatorKind.Xor` →
  `IrLogicalOp.Xor` → CIL `xor`. ⚠ CORRECTION (the W3 chair adjudication, roadmap §2 Phase 2): the former NOTE
  here claimed XOR is "2002-era, not a 2023 delta" — that parenthetical was UNSOURCED and is WRONG: Annex E.2
  item 25 / E.3.2 item 4 (the spec's own change annex) list XOR and EXCLUSIVE-OR among the words newly reserved
  by 2023 (they were user-defined words before), and presence in the 2023 body (§8.8.4.9) proves nothing about
  2002. The operator is `{is2023()}?`-gated; conformance re-editioned to `tests/conformance/2023/logical_xor`
  (VCR rows 32/41; constructs.json rows `logical-xor-operator-2023` + the two user-word interval rows).
- ☑ **M4-2b — `SMALLEST-ALGEBRAIC` + `EXCEPTION-FILE-N` intrinsics — BOTH LANDED.** SMALLEST-ALGEBRAIC (§15.83)
  is a compile-time PICTURE fold (`IntrinsicBind.Fold`, the HIGHEST/LOWEST `BindAlgebraicFold` family;
  `smallest_algebraic` ENABLED). EXCEPTION-FILE-N (§15.29) + EXCEPTION-LOCATION-N (§15.31) landed with the P10
  Step-11 EC-N wave (2026-07-16) once national data unblocked: `EcFunctions.FileN/LocationN` = the base
  renderings projected through the ONE `NationalOf` repertoire translator, result category National (Table-16
  legality); `exception_file_n` ENABLED + the `exception-file-n-2002` matrix row + the 85-window negative.
  Residue: the 2023 optional file-connector-argument form (base + twin) renders loud — VCR rows 68/69,
  PHASE-13 Step 9.
- ☐ **M4-3 — Other 2023 intrinsic/bit/boolean additions + dynamic-table finalization + clarifications.** Audit
  `specs/ISO_COBOL.md` 2023-marked changes when M4 begins (many intrinsics already in `IntrinsicFunctions.cs` —
  verify completeness + **version gating**, i.e. a 2023 fn used under `--std 85` / `2002` / `2014` MUST emit the
  per-edition diagnostic — per `docs/VERSION_CHANGE_REFERENCE.md` + the version test matrix).

---

### Recorded deferral — `>>PROPAGATE` directive semantics (§7.3.21) → PHASE-13

The PROPAGATE compiler directive (automatic propagation of unhandled exception conditions to the activating
runtime element, §7.3.21 + §14.6.13.1.3's "as though GOBACK RAISING LAST" rule) is RECOGNIZED by the
preprocessor's directive list but its semantics are UNIMPLEMENTED (P9 Step 13 finding, 2026-07-16). The
deliberate GOBACK/EXIT ... RAISING propagation path is landed (`oo_ec_goback_raising`). Note the spec
correction recorded with this finding: USE Formats 3/4 carry NO [GLOBAL] phrase (§14.9.49.2) — only the
file-exception Format 1 and reporting Format 2 do — so there is no "GLOBAL exception declarative walk" to
implement; PROPAGATE is the only cross-element automatic path.

### Recorded deferral — GOBACK status-phrase (2023; §14.9.18.2) → PHASE-13

`GOBACK [WITH {ERROR|NORMAL} STATUS [identifier|literal]]` is a COBOL-2023 addition (the STOP RUN twin is
LANDED — the `stop-run-status` construct row). As of P9 Step 14 (2026-07-16) the grammar's `gobackStatement`
carries no status alternative — DEFERRED to **PHASE-13 (M4 2023 EC remnants + behavior rows)**, where it lands
with: the grammar alternative (superset parse), a `goback-status-2023` constructs row (introduced 2023 — note
the asymmetry with `stop-run-status`'s 2002 introduction; derive both edges from the editions), the binder
lowering onto the same status machinery STOP RUN WITH STATUS uses (singular pattern), and a 2014-vs-2023
continuity matrix row. The D8 GOBACK/STOP-RUN SPLIT itself is verified LANDED: a method-context GOBACK binds
`BoundMethodReturn` (OoBinder), a program GOBACK `BoundGoback`, STOP RUN `BoundStop` — distinct signals, the
OO "only correctness blocker" closed.

## 4. Execution order (waves) — implement top-down

**Wave 1 — UDF correctness + cheap high-value (start here):**
1. **M2-UDF-1** general inline UDF (🐛 silent 0) — and **M2-UDF-2** literal/arith args (🐛) together.
2. **M2-DATA-1** BINARY-CHAR/SHORT/LONG/DOUBLE (🐛 mis-typing).
3. **M4-1** DELETE FILE execution (cheap), **M2-PROC-2** INSPECT BACKWARD (cheap), **M4-2** XOR + 2 intrinsics
   (cheap).
4. **M2-PROC-1** INITIALIZE TO VALUE/DEFAULT/FILLER (🐛).

**Wave 2 — arithmetic + data aliases:**
5. **M2-ARITH-1** ROUNDED MODE (all 8) + DEFAULT ROUNDED (🐛 2-of-8).
6. **M2-DATA-2** FLOAT-SHORT/LONG (alias COMP-1/2).
7. **M2-PRE-1** preprocessor robustness trio (re-scoped — NOT crashes; clean-error/mis-parse only, see §3.5).

**Wave 3 — new data categories:**
8. **M2-DATA-3** national data · **M2-DATA-4** boolean/bit · **M2-FILE-1/2** file 2002+.

**Wave 4 — big subsystems:**
9. **M2-DATA-5** pointers → **M2-PROC-5** ALLOCATE/FREE.
10. **M2-PROC-4** EC/exception framework → RAISE/RESUME/USE.
11. **M2-PROC-3** VALIDATE · **M2-ARITH-2** standard arithmetic.
12. **M2-OO-1** Object-Oriented COBOL.

**Wave 5 — M3:** dynamic tables · TYPEDEF · JSON/XML.
**Wave 6 — M4:** remaining 2023 deltas + version-gating of intrinsics.

---

## 5. Production / commercial-grade axes (run alongside conformance)

A commercial compiler needs more than spec checkboxes. Track these in parallel:

- **Diagnostics quality** — comprehensive `CBL####` coverage, accurate source locations, actionable messages,
  exit codes; never crash on invalid input (P1 hardening already started — see memory `project_p1_diagnostics`).
- **Dialect strictness** — the two-axis model (version `--std` × strictness); every leniency dialect-gated
  (see memory `project_dialect_two_axes`).
- **Conformance corpus (BUILT — `tests/conformance/`)** — the NIST-equivalent suite for 2002/2014/2023. A
  file-based corpus (`<name>.cob` + `<name>.out`) per version, auto-discovered + run under the matching
  `--std` by `ConformanceTests` (integration project), inside the guard. Seeded with M2 features (UDF
  invocation, conditional compilation, OPTIONS, REPOSITORY); **grow it with every feature** and backfill
  already-landed ones. Future: a per-version "% to spec" dashboard. Negative per-edition tests (a construct
  compiled under an edition that lacks it ⇒ the expected diagnostic) are NOT optional — they are the co-equal
  second obligation of every feature (owner, 2026-06-10), owned by the version test matrix
  (`docs/VERSION_TEST_MATRIX_DESIGN.md` over `docs/VERSION_CHANGE_REFERENCE.md`).
- **Performance** — parse/compile throughput on large programs; runtime efficiency of the generated C# as compiled
  by Roslyn (the primary backend; a future Cecil/CIL backend behind `ICodeGenBackend` gets its own perf track).
- **Tooling & packaging** — CLI UX, `--help`, listing output, single-exe packaging, NuGet/runtime deployment.

---

## 6. POST-CONFORMANCE MILESTONES — GATED (do NOT start until an operational ISO 2023 compiler exists)

> Owner-stated; see memory `project_required_reviews`. Do nothing here until M2→M4 conformance is achieved.

### 6.1 Full software architectural review
- Project **folder & file layout and naming** rationalized.
- **Class organization with proper isolation / single-responsibility — decompose any god classes.**
- Best software-design principles throughout; clear module boundaries.
- **Complete, accurate code documentation + code comments** across the source.
- Adopt the **latest C# language features**.

### 6.2 Project + executable rename — ☑ SUPERSEDED (realized by the PIVOT)
> The greenfield is born `src/Cobol.Net.*` with exe `cobol` (`docs/COBOLNET_PROJECT_ORG_DESIGN.md`); the legacy
> `CobolSharp.*` projects are deleted at G8, so no rename remains. The touch-points below are historical.
- Rename the project **`CobolSharp` → `COBOL.NET`** (rationale: it is COBOL for the .NET runtime — there is no
  "sharp" in it).
- **Produced executable MUST be named `cobol.exe`** (lowercase).
- Touch-points to plan for: `.csproj` `AssemblyName` + output exe name; the `.sln`; root namespaces
  (`CobolSharp.*`); `scripts/guard.sh` paths; `CLAUDE.md`/`README`/docs; the runtime assembly name; NuGet/package
  ids. Execute as one planned, guard-verified operation — not ad hoc.

---

## 7. Plan change-log
- **2026-06-05** — Created from the 15-area parallel conformance audit (workflow `iso2023-conformance-audit`) +
  session surveys. Initial catalog + waves. Established as SSOT.
- **2026-06-05 (session DEVLOG 372–382)** — Landed, each conformance-tested + ticked: M4-1 DELETE FILE; M4-2a
  XOR; M2-UDF-1/2 inline UDF invocation; M2-DATA-2 FLOAT-SHORT/LONG/EXTENDED; M2-DATA-1 BINARY-CHAR/SHORT/LONG/
  DOUBLE; M2-PROC-2 INSPECT BACKWARD; M2-PROC-1 INITIALIZE TO VALUE/DEFAULT/FILLER; M2-ARITH-1 ROUNDED MODE
  (all 8 + per-statement MODE IS + CORRESPONDING, ◐ two follow-ups left). Guard 1047/474/364. Next: M2-DATA-3
  National (§3.2). Process note: keep THIS document the singular live plan — tick items + log here as work lands;
  no separate resume/handoff docs.
- **2026-06-05 (session DEVLOG 383)** — **M2-DATA-3 National (UTF-16) data CORE COMPLETE** (conformance
  `tests/conformance/2002/national_data`, 13 assertions). `PIC N`/`USAGE NATIONAL` (2 bytes/char), `N"…"`
  literals, MOVE national←national, national↔alphanumeric + numeric→national conversion, literal/figurative
  VALUE, MOVE SPACE / INITIALIZE, DISPLAY, and national comparison — all char-aware. A 3-agent adversarial
  review caught that the initial slice silently corrupted national↔alpha MOVE, figurative/VALUE/INITIALIZE
  fill, and comparison; all fixed before commit (lesson: a narrowly-scoped passing conformance test is not
  evidence of completeness — verify the whole dispatch surface). Guard 1047/475/364. Next: M2-DATA-5 Pointers
  or M2-DATA-4 Boolean/bit (§3.2).
- **2026-06-05 (session DEVLOG 384–386)** — **M2-PRE-1 re-scoped** (384): an empirical repro refuted the
  "preprocessor crashes on valid source" claim (nothing crashes); the two real-but-rare defects + a spec caution
  are documented, item dropped to ◐. **M2-DATA-4 Boolean data CORE COMPLETE** (385 recipe, 386 implementation):
  `PIC 1`/`USAGE BIT`, `B"…"`, MOVE/VALUE/INITIALIZE/ZERO/DISPLAY/compare/JUSTIFIED; conformance `boolean_data`.
  A 2-agent adversarial review caught + fixed VALUE corruption (§13.18.63 GR10), a boolean-in-COMPUTE
  silent-numeric read, and JUSTIFIED wrongly rejected on boolean/national (the last also un-deadened national
  JUSTIFIED). Guard 1047/476/364. Next: M2-DATA-5 Pointers (needs the PointerRegistry design decision).
- **2026-06-05 (session DEVLOG 387)** — **M2-PROC-6 `GOBACK RETURNING`** (ISO §14.9.16, dialect-gated 2002+):
  lowers to a synthetic MOVE into the PROCEDURE DIVISION RETURNING item + `IrGoBack` (reuses CALL…RETURNING);
  conformance `goback_returning`. EXIT variants verified present/green; `CONTINUE AFTER` deferred. Guard
  1047/477/364. Still next: **M2-DATA-5 Pointers** (needs the PointerRegistry design decision — owner to confirm).
- **2026-06-05 (session DEVLOG 388–389)** — **M2-DATA-5 Pointers Phase-1 DONE** (388 recipe, 389 impl):
  `USAGE POINTER` (8-byte handle), `NULL`, `SET p TO NULL`/`SET p TO q`, `= NULL`/`= q`; pointer↔non-pointer MOVE
  + VALUE rejected; default INITIALIZE leaves pointers unchanged; conformance `pointer_data`. Lean reuse (SET→MOVE,
  NULL→0x00 figurative, compare via figurative/`IrStringCompare` — no new IR nodes). Self-review caught 3 bugs
  (`DataTypeSymbol.Category` not deriving Pointer → silent `SET p TO NULL` no-op; no-PIC pointer mis-classified
  `IsGroup`; VALUE-check ordering); the 2-agent adversarial review then confirmed clean (13 probes). Phase-1
  did NOT need the deferred design decision (NULL = 8 zero bytes, no targets to map). Guard 1047/478/364.
  **NEXT: the easy data items are done; remaining M2 is Phase-2 pointers (owner-gated), M2-PRE-1, OPTIONS
  arithmetic, file 2002, then the big subsystems (EC/exceptions, VALIDATE, OO).**
- **2026-06-05 (session DEVLOG 390–391)** — SORT Format-2 conformance backfill (390); **M2-ARITH-1 follow-up #1
  applied: OPTIONS `DEFAULT ROUNDED MODE`** (391) now sets the bare-ROUNDED default (token-scan in
  `VisitOptionsParagraph` → `SemanticModel.DefaultRoundingMode` → `BindRounded`); per-statement MODE still
  overrides; conformance `options_default_rounded`. Guard 1047/480/364. Follow-up #2 (PROHIBITED→EC) blocked on
  the EC framework. Remaining M2: Phase-2 pointers (owner-gated design decision), M2-PRE-1, file 2002, and the
  big subsystems (EC/exceptions, VALIDATE, OO, standard arithmetic).
- **2026-06-06 (session DEVLOG 394–428) — THE .NET-NATIVE DATA-MODEL MIGRATION: core (Stage 3) COMPLETE.** Built
  the numeric (`CobolNum`/`CobolDecimal` BigInteger) + character (`CobolString`) substrates + differential oracles
  (394–399); the `RecordClassificationPass` typed-vs-byte classifier (397–398) wired into the Binder (401); then
  flipped, ONE rule at a time, each guard-green + a flag-on≡flag-off differential test, all gated behind
  `EnableTypedFields` (default OFF → corpus byte-identical): character → `string` (403–409); numeric → `long`
  (unsigned-int) / `decimal` (signed-scaled) over DISPLAY/COMP/BINARY across VALUE/MOVE/DISPLAY/COMPARE/class-cond/
  **arithmetic**/figurative-ZEROS (410–420); record-struct numeric members (419); fixed **OCCURS** tables (char +
  numeric) → `T[]` with subscripted + PERFORM-VARYING access, SEARCH safely byte (422–426); **nested groups** →
  nested `record struct`s (427); end-to-end "definition of done" — a representative business program flips its whole
  data division byte-identically (428). Plus a **byte-engine ISO-2023 conformance fix**: a VALUE on an OCCURS item
  now initializes EVERY occurrence (§13.18.63.4 GR 9; 424; conformance `table_value_occurs`; zero baseline shifts).
  24 differential tests; guard **1196 unit / 507 integration / 364 NIST** (all green). **CORRECTION (428): pointers
  use MANAGED .NET REFERENCES (`ManagedPointer`); the `PointerRegistry` design is REJECTED — SETTLED, NOT owner-gated.**
  **NEXT (all autonomous-eligible): Stage-4 pointers (managed refs) + OO; Stage-5 Roslyn backend; Stage-6 finalize +
  flip-on-by-default + rename.**
- **2026-06-08 (session DEVLOG 453–456)** — **OO goes TYPED-NATIVE (ADR §7) + the multi-method keystone.** Owner
  course-correction: OO object data must be per-instance typed .NET fields (NOT the byte `ProgramState`); the
  `EnableTypedFields` gate is migration-safety for the legacy corpus, so OO is an **always-on consumer** of the typed
  pipeline (memory `feedback_typed_native_only`). Landed, each guard-green + conformance-tested + (455)
  adversarially reviewed: **453** object char data → per-instance `string`; **454** object numeric → per-instance
  `long`/`decimal` + the general typed-numeric→byte MOVE cell; **455** multi-method classes (N `METHOD-ID`/class, each
  its own .NET method + exit-bounded dispatch; per-method paragraph scope fixes CBL3104) + `INVOKE SELF`→`callvirt`
  incl. polymorphic across INHERITS (COBOL0112 lifted) — 3-agent panel → edge gaps fail loud (COBOL0116
  SECTION-in-method, COBOL0117 multi-method-with-params); **456** object group→per-instance `record struct` +
  `OCCURS`→per-instance typed array (byte `State` now empty on a fully-typed class). Instance dimension via one
  chokepoint `CilDataEmitter.EmitTypedFieldOwner`. Conformance: `oo_instance_data`/`oo_self`/`oo_self_polymorphic`/
  `oo_object_group` (+ the prior 5). Guard **1204 / 535 / 364**. NEXT: subclass own typed object data (lift COBOL0113);
  per-method LINKAGE (lifts COBOL0117) + typed-field INVOKE args; FACTORY; then PROPERTY / universal-ref+EC; then the
  rest of M2 → M3 → M4.
