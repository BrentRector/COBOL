# SURVEY — Runtime IO / Control / Exceptions / Intrinsics (`src/Cobol.Net.Runtime`)

Status: SURVEY (rearchitecture review; backfill of the missing IO/Control/Exceptions/Intrinsics survey).
Scope: the runtime facades WiseOwl COBOL-generated C# calls — file I/O (`IO/`), inter-program + run-unit control
(`Control/`), the EC exception engine (`Exceptions/`), and the intrinsic-function catalog (`Intrinsics/`). Assesses
against the HARD INVARIANTS: typed-native data only; spec-first (`specs/ISO_COBOL.md`); one canonical mechanism per
job; no god classes.

Method: every assigned file was read in full. Citations are `file:line`. LOC counts are whole-file line counts.

---

## 1. Responsibilities

| Area | Responsibility | Substrate crossing |
|---|---|---|
| `IO/` | The static `CobolFile` facade + three organization connectors (Sequential / Relative / Indexed), SORT/MERGE store, Report Writer engine, file sharing/locking, ACCEPT sources. Owns the ISO §9.1.13 I-O status machine, the §14.9.30/§14.9.35 read-position state, framing, LINAGE. | record ⇄ **character image (`string`)** at the on-disk edge (the ONLY typed→char boundary in I/O) |
| `Control/` | Inter-program CALL/CANCEL (`ProgramRegistry`), the §14.6.2.3 state model, EXTERNAL store, `FUNCTION MODULE-NAME` stack (`CobolModule`), the CALL ABI (`ManagedPointer`/`StorageCell`/`CobolArg`/`ICobolProgram`/`CobolArgAdapt`), pointer ops (`CobolPtr`), OO root (`CobolObject`), external switches, control signals. | CALL-ABI adapters + `StorageCell` are the sanctioned char-image seams |
| `Exceptions/` | The §14.6.13 Table-13 catalog (`ExceptionCatalog`), the run-unit LAST EXCEPTION register + propagation slots + ambient EC-checking gates (`ExceptionState`), the EXCEPTION-* function backends (`EcFunctions`), fatal-condition + RESUME signals. | none (pure state) |
| `Intrinsics/` | The §15 catalog: float math, exact scaled-`Int128`/`long` numerics + NUMVAL family, character functions, date/time engine. | scaled-long / `double` / `string` — no byte substrate |

The typed-native invariant holds: the only `string`-image boundaries are the disk edge (`FileConnector`s,
`CobolSort`), the EXTERNAL/Tier-B `StorageCell`, and the CALL-ABI adapters. No byte `ProgramState` anywhere.

---

## 2. Key types (name · role · LOC · assessment)

### IO
| Type | File | LOC | Assessment |
|---|---|---|---|
| `CobolFile` (static, partial ×3) | `IO/CobolFile.cs` (+ `.Locks.cs`, tail of `IndexedFile.cs`) | 196 / 331 / — | The emitted file facade + registries. Owns `Files`/`Locked`/`_instSeq`/`_pendingObjectClose`. **Dispatches sequential-first then falls through to a parallel `Keyed*` fan-out** (`CobolFile.cs:103-108,116,169,172,177`). A second dispatch mechanism layered on the type split. |
| `SequentialFile` | `IO/SequentialFile.cs` | 438 | Line- & record-sequential + print-control ADVANCING + LINAGE. Re-implements the §9.1.13 status machine and read-position pair from scratch (`:31-32`). Own varying framing (`WriteFrameLength :290`). Coherent internally; duplicated externally. |
| `RelativeFile` | `IO/RelativeFile.cs` | 487 | Sparse `SortedDictionary` slot model. Re-implements status machine + read-position pair (`:108-109`) + `_modeKnown`/`OpenModeView` (`:132-133`) + `Stored()`/`Fit()`. `KeyedFrames` framing helper (`:27-79`) shared with indexed. |
| `IndexedFile` | `IO/IndexedFile.cs` | 707 | Record-list-as-truth design (arrival-ordered, indexes derived on demand — a genuinely good design, `:474-477`). Re-implements the same status machine + read-position pair (`:47-48`). **The file also carries the `partial class CobolFile` keyed registries + `Keyed*` routing (`:510-707`)** — an unrelated concern welded onto the connector file. |
| `KeyedAccess` enum | `IO/RelativeFile.cs:10` | — | Shared access-mode enum; fine. |
| `CobolSort` (static) | `IO/CobolSort.cs` | 207 | In-memory SORT/MERGE store. **Its OWN static `Dictionary<string,Store> Files` (`:36`)** — a FOURTH file-name registry, entirely separate from `CobolFile`'s three. Stable sort + k-way merge; numeric-key algebraic compare. Clean logic. |
| `CobolReport` / `ReportGroup*` | `IO/ReportWriter.cs` | 493 | Per-report RWCS engine (instance, one per RD). ONE compose-delegate mechanism, presentation-time MOVEs. Writes via `CobolFile.WriteAdvancing(_fileName,…)` — coupled by string name (`:371,457`). Well-factored; singular-pattern-clean. |
| `AcceptSource` (static) | `IO/AcceptSource.cs` | 102 | ACCEPT device + temporal sources. **`public static Func<DateTime> Now {get;set;}` (`:26`)** — a process-global mutable clock seam. `Device()` reads `Console.In` directly (`:95`). |
| `FileStatusCode` / enums | `IO/FileSupport.cs` | 144 | Status constants (`:74`) consumed by all three connectors, filed next to the open-mode/sharing enums. Coherent; only mis-located (constants vs enums in one file). |

### Control
| Type | File | LOC | Assessment |
|---|---|---|---|
| `ProgramRegistry` (static) + 10 others | `Control/ProgramRegistry.cs` | 595 | **God-FILE grab-bag:** the registry PLUS `ProgramReturn`, `CobolCallException`, `CobolPassMode`, `ManagedPointer`(+`Null`), `StorageCell`, `CellPointer`, `ManagedPointer<T>`, `CobolArg`, `ICobolProgram`, `CobolArgAdapt`, `ExternalStore`. 11 distinct types in one file. Registry logic itself (name scope §8.4.6.3, state model §14.6.2.3, CANCEL, sibling-module probe) is sound and well-cited. `ProbeSiblingModule (:558)` does `AssemblyLoadContext` load + reflection `Invoke` (cached per name). |
| `CobolObject` (abstract) | `Control/CobolObject.cs` | 90 | OO root: universal `__CobolInvoke`, null-guard, per-object instance-file tracking. **`~CobolObject` finalizer (`:85-89`)** — the prior data-race site; now enqueue-only (see §4). `SuppressFinalize` by default, re-arm only for file-owning objects (`:68,75`) — a thoughtful fix. |
| `CobolModule` (static) | `Control/CobolModule.cs` | 81 | `FUNCTION MODULE-NAME` stack. **The LONE `[ThreadStatic]` store (`:24`)** — inconsistent with every other (plain-static) run-unit store. |
| `CobolPtr` (static) | `Control/CobolPtr.cs` | 107 | ADDRESS OF / BASED / SET / ALLOCATE / FREE over `ManagedPointer`+`StorageCell`. Loud-fatal on null/freed/out-of-range. Clean; stateless. |
| `ExternalSwitches` (static) | `Control/ExternalSwitches.cs` | 49 | **Process-global `ConcurrentDictionary States` (`:22`)**, run-unit scope per §12.3.7 GR4, but its `Reset()` is "test isolation only" (`:47`) — switch state leaks across in-process run units. |
| `CobolInvokeArg` | `Control/CobolInvokeArg.cs` | 21 | Universal-dispatch arg carrier. Fine. |
| `MethodReturn`/`StopRun`/`NotImplemented(+Exception)` | `Control/{MethodReturn,StopRun,NotImplemented}.cs` | 17/10/31 | One-purpose control signals; scattered between `Control/` and `Exceptions/`. |

### Exceptions
| Type | File | LOC | Assessment |
|---|---|---|---|
| `ExceptionCatalog` (static) | `Exceptions/ExceptionCatalog.cs` | 307 | The ONE machine form of Table 13 + the 3-level hierarchy + the I-O-status→EC bridge. **Immutable after `Build()`** — legitimately static, thread-safe, keep as-is. NAME-keyed (not enum) for the open EC-USER-*/EC-IMP-* families — correct singular-pattern call. Model exemplar. |
| `ExceptionState` (static) | `Exceptions/ExceptionState.cs` | 209 | **The prime shared-state smell.** ALL mutable process-global: `LastName`/`LastFatal`/`LastFile`/`LastIoStatus`/`LastLocation`/`LastStatement`/`ExceptionObject`, `_propagated`/`_propagatedObject` slots, and the ambient EC gates `ArgumentFunctionChecking (:177)` / `DataConversionChecking (:199)`. No reset owner of its own. |
| `EcFunctions` (static) | `Exceptions/EcFunctions.cs` | 49 | EXCEPTION-STATUS/LOCATION/STATEMENT/FILE backends over `ExceptionState`. Thin, fine. |
| `CobolFatalException` / `ResumeSignal` | `Exceptions/{CobolFatalException,ResumeSignal}.cs` | 19/24 | The fatal-EC + RESUME control signals. Fine. |

### Intrinsics
| Type | File | LOC | Assessment |
|---|---|---|---|
| `CobolIntrinsics` (static, partial ×4) | `Intrinsics/CobolIntrinsics{,.Exact,.Float,.Text}.cs` | 57/334/86/305 | Family-split partial. `.cs` = double→scaled-long spine (`FromDouble`, `Pow10D :43`, `Pow10I :51`). `.Exact` = exact `Int128` numerics + NUMVAL. `.Float` = trig/financial/stats + **`static Random _random (Float.cs:71)`** reassigned by `Random(seed) (:83)`. `.Text` = CHAR/ORD/SUBSTITUTE/CONVERT/TRIM. All domain errors funnel to `ExceptionState.ArgumentError`. Cohesive family split — keep. |
| `CobolDate` (static) | `Intrinsics/CobolDate.cs` | 368 | Date/time + the §15.3 format tokenizer/analyzer (ONE parse feeding emit + analyze — good singular pattern). Own `Pow10 (:154)`. Reads `DateTimeOffset.Now` directly (not `AcceptSource.Now`) — an inconsistency in clock sourcing. |

---

## 3. Architecture smells (severity · file:line)

### S1 — Three file organizations duplicate the §9.1.13 status machine + read-position state (HIGH)
`SequentialFile` / `RelativeFile` / `IndexedFile` are three independent `sealed` classes with **no common base**, each
re-declaring the same control state:
- read-position guard pair `_lastReadUnsuccessful` / `_prevOpWasSuccessfulRead` — verbatim at
  `SequentialFile.cs:31-32`, `RelativeFile.cs:108-109`, `IndexedFile.cs:47-48`.
- open-mode tracking `_mode` / `_modeKnown` / `OpenModeView` — `SequentialFile.cs:158-159`,
  `RelativeFile.cs:132-133`, `IndexedFile.cs:67-68`.
- `_optionalAbsent`/`IsOptional`, `Status` + `SetStatus`, the varying-bounds `Stored()` (`RelativeFile.cs:163` ≈
  `IndexedFile.cs:94`), the `Fit()` pad/truncate helper (`SequentialFile.cs:437`, `RelativeFile.cs:485`,
  `IndexedFile.cs:488`), and the OPEN preamble + `catch UnauthorizedAccessException→'37' / IOException→'30'`
  (`SequentialFile.cs:234-235`, `RelativeFile.cs:230-231`, `IndexedFile.cs:170-171`).

This is the singular-pattern violation the roadmap targets with a `FileConnector` base.

### S2 — A SECOND dispatch mechanism layered on the type split (HIGH)
Because the three organizations live in three registries, `CobolFile` dispatches by trying `Files` (sequential) then
falling through to a parallel `Keyed*` fan-out: `CobolFile.cs:103-108` (Open), `:116` (Close), `:169`/`:172`/`:177`
(LastReadLength/Status/OpenModeOf) → `KeyedOpen`/`KeyedClose`/`KeyedStatus`/`KeyedLastReadLength`/`KeyedOpenModeOf` at
`IndexedFile.cs:657-706`. Cross-registry operations must hand-fan across all three (`CobolFile.Locks.cs:272-304`
`ResolveConnector`/`HostPathOf`/`SetStatusOf`; `DeleteFile` at `IndexedFile.cs:637-640`). One polymorphic registry
would erase all of it.

### S3 — FOUR-plus separate file-name registries (HIGH)
`Files` (`CobolFile.cs:15`), `RelativeFiles` + `IndexedFiles` (`IndexedFile.cs:512-513`), **and a completely
independent `CobolSort.Files` (`CobolSort.cs:36`)**, plus the sharing `ConnectorShares`/`Physical`
(`CobolFile.Locks.cs:43,46`). No single "these are the run unit's open files" owner.

### S4 — Inconsistent run-unit-state threading model (HIGH — the shared-state smell)
Run-unit-lifetime mutable state is spread across unrelated statics, each with a DIFFERENT threading assumption:

| Store | Home | Threading as-built |
|---|---|---|
| Program registry | `ProgramRegistry.cs:354-356` | plain process-global |
| Last-exception + EC ambient gates | `ExceptionState.cs` (all `static`) | plain process-global |
| EXTERNAL store | `ProgramRegistry.cs:311` (`ExternalStore`) | plain process-global |
| MODULE-NAME stack | `CobolModule.cs:24` | **`[ThreadStatic]` — the lone thread-local** |
| File registries + GC-close queue | `CobolFile.cs:15-23` | single-thread dicts + lock-free finalizer hand-off |
| SORT store | `CobolSort.cs:36` | plain process-global |
| RANDOM sequence | `CobolIntrinsics.Float.cs:71` | plain process-global, reassigned on seed |
| ACCEPT clock seam | `AcceptSource.cs:26` | process-global mutable |
| External switches | `ExternalSwitches.cs:22` | process-global, never reset per run unit |

There are at least THREE reset entry points — `ProgramRegistry.Reset()` (`:359`, which reaches sideways into
`ExternalStore.Reset()` + `CobolModule.Reset()`, `:364-365`), `CobolFile.Init()` (`:26`), and (never) the clock /
`_random` / switches. "Start a clean run unit" has no single owner. The `[ThreadStatic]`-vs-plain-static split is a
latent thread-hop bug (S4a below).

### S5 — `ProgramRegistry.cs` is a 595-line god-file (MEDIUM)
11 distinct types in one file (the registry + the entire CALL ABI + `ExternalStore` + two signal/exception types).
Not a god *class*, but a one-concern-per-file violation; the ABI types are not "registry" concerns.

### S6 — Powers-of-ten recomputed by identical multiply-loops (MEDIUM, efficiency)
`CobolIntrinsics.Pow10D (:43)` / `Pow10I (:51)`, `CobolDate.Pow10 (:154)` — plus `CobolNum.Pow10`/`Pow10Wide`,
`CobolDec.Pow10`, `CobolFloat.Pow10` outside this survey's set (six total). Each a `for`-loop rebuilding a
compile-time-constant table on every store/rescale/format.

### S7 — Scattered one-purpose signals; mis-located status constants (LOW)
`StopRun`/`MethodReturn`/`NotImplemented` in `Control/`, `ResumeSignal`/`CobolFatalException` in `Exceptions/` — one
cohesive control-signal group split by folder. `FileStatusCode` constants live inside `FileSupport.cs:74` next to
enums though consumed by all connectors.

### S8 — Inconsistent clock sourcing (LOW)
`AcceptSource` reads its injectable `Now` seam (`:26`), but `CobolDate.CurrentDate()`/`FormattedCurrentDate()` read
`DateTimeOffset.Now` directly (`CobolDate.cs:32,250`). Two clock sources; a pinned test clock governs ACCEPT but not
the CURRENT-DATE intrinsic.

---

## 4. Coupling / global mutable state

- **`ExceptionState` is a hub of process-global mutable state** consumed everywhere: intrinsics
  (`ArgumentError`/`DataConversionError` from all four `CobolIntrinsics.*` + `CobolDate`), `ProgramRegistry`
  (`TakePropagated`/`ApplyPropagationDefault`, `ProgramRegistry.cs:404-409,460-463`), `CobolPtr`/`CobolObject` (throw
  `CobolFatalException`). It has no reset of its own — a checking-off program simply never writes it, which is why it
  "works" today, but it is the sharpest concurrency edge.
- **The `~CobolObject` finalizer race is correctly contained but locally.** `~CobolObject (CobolObject.cs:85-89)`
  runs on the GC finalizer thread and only ENQUEUES keys into `CobolFile._pendingObjectClose` (a
  `ConcurrentQueue`, `CobolFile.cs:23`); the mutator thread drains via `DrainPendingObjectCloses (:46-49)` at
  Init/Open/CloseAll; `MintInstanceKey` uses `Interlocked.Increment (:57)`. This is the ONE place threading was
  seriously engineered — and it is good. But that discipline is **local to `CobolFile`**; none of the other statics
  (esp. `ExceptionState`) carry equivalent guards, so the runtime's threading posture is "one carefully-fixed race
  inside a sea of unguarded process-global state."
- **Cross-registry fan-out coupling:** any whole-file operation (`DeleteFile`, `HostPathOf`, `SetStatusOf`,
  `ResolveConnector`, `CurrentRecordId`) must know about all three organization dictionaries
  (`CobolFile.Locks.cs:272-312`, `IndexedFile.cs:637-640`).
- **String-name coupling:** `ReportWriter` → `CobolFile.WriteAdvancing(_fileName,…)` by connector key
  (`ReportWriter.cs:371,457`); the emit-qualified `PROG::FILE` / `::EXT::` key convention leaks into `EcFunctions.File`
  (strips the prefix, `EcFunctions.cs:45-47`).

---

## 5. Latent-bug risks

1. **Concurrent / re-entrant run units are impossible today** (design-confirmed). Two run units in one process would
   collide on `Files`/`RelativeFiles`/`IndexedFiles`, `CobolSort.Files`, `ExceptionState.*`,
   `CobolIntrinsics._random`, `AcceptSource.Now`, and `ExternalSwitches.States`. This forces the test harness to spawn
   a process per program.
2. **`[ThreadStatic] CobolModule` vs plain-static everything else (S4a).** If a host ever resumes the run unit on a
   different thread (an `async` continuation on a pool thread), `CobolModule.Stack` is empty on the new thread →
   `FUNCTION MODULE-NAME` silently returns wrong values while `ProgramRegistry` state is intact. Silent-wrong, not
   loud.
3. **The ambient EC-checking gates are single booleans, not a save/restore stack**
   (`ExceptionState.cs:177,199`). The generated statement guard sets/resets `ArgumentFunctionChecking`; a nested
   statement (an intrinsic-bearing expression that drives a CALL whose body runs a statement with different checking)
   overwrites the flag and its `finally` resets it to `false`, not to the outer statement's prior value — the outer
   statement finishes with checking wrongly disabled. Latent within a *single* run unit; moving the gate onto a
   RunUnit instance does NOT fix it.
4. **RANDOM sequence is process-global, not run-unit-scoped** (`CobolIntrinsics.Float.cs:71,83`). The code comment
   says "ONE current pseudo-random sequence per run unit (§15.75.3)", but the static is never reset at a run-unit
   boundary — a second in-process run unit inherits the first's `_random` state, and two concurrent run units clobber
   each other's sequence (breaks §15.75.4 rule-2 per-process determinism NIST IF131A relies on).
5. **`DeleteFile` can never produce status 62.** `DeleteFileSharing = "62"` is defined (`FileSupport.cs:143`) but
   `DeleteFile (IndexedFile.cs:629-653)` only checks the *named* connector's `IsOpen` for 41; it never consults the
   `Physical` sharing registry for *another* connector holding the file open, so §9.1.13.9 item 2 (62) is unreachable.
   Orthogonal to the reorg, but a real spec gap.
6. **`ExternalSwitches` state leaks across in-process run units** (`ExternalSwitches.cs:22,47`) — `SET switch TO ON`
   persists past a run-unit boundary because the only `Reset()` is documented "test isolation only."
7. **ACCEPT `Device()` reads `Console.In` directly** (`AcceptSource.cs:95`) — genuinely process-global stdin; even a
   perfect clock injection leaves ACCEPT device I/O un-isolated across run units.

---

## 6. Reorg suggestions (independent of, but converging with, the existing plan)

1. **One `FileConnector` base + one polymorphic `FileRegistry`** — hoist the S1 shared state (status machine,
   read-position pair, mode, host path, `Fit`, OPEN/CLOSE preamble); delete the `Keyed*` fallthrough (S2). Migrate one
   organization at a time behind its NIST goldens (the base must be the TRUE common denominator; the sequential
   REWRITE-'43' gate and the keyed key rules stay overrides).
2. **One `RunUnit` context owning ALL run-unit-lifetime state** (S3/S4) — program table, exception engine, EXTERNAL
   store, module stack, file registry, **the SORT store, the RANDOM sequence, the external-switch table**, and the
   clock. Uniform ambient (drop the lone `[ThreadStatic]`). One begin/end lifecycle replacing the ≥3 reset entry
   points.
3. **Split `ProgramRegistry.cs`** into per-concern files (registry vs CALL-ABI vs `ExternalStore` vs signals) (S5).
4. **One table-driven `Pow10`** (S6); **role-based folders** (values vs verbs vs IO vs control) and a signals group
   (S7); **one clock** for both ACCEPT and CURRENT-DATE (S8).
5. Keep `ExceptionCatalog` static+immutable — it is the model to emulate.

---

## ROADMAP GAP CHECK

Assessed against `DESIGN-runtime-library.md` + `PHASE-08-runtime-library-reorg-rununit.md`.

**Verdict: the plan is strong and directly fixes the two headline findings.** The `FileConnector` base + polymorphic
`FileRegistry` (DESIGN §2.2 / PHASE-08 Step 4-5) dedupes S1 and deletes the S2 `Keyed*` fallthrough; the `RunUnit`
context with `AsyncLocal` ambient (DESIGN §2.1 / Steps 7-8) fixes S4's inconsistent threading, removes the lone
`[ThreadStatic]`, and gives run-unit state ONE owner; the `Pow10` dedup (Step 1), the `ProgramRegistry.cs` split (Step
2c), and the clock injection (Step 9) cover S5/S6/S8. The migration is correctly ordered (safest first) and each step
is behind the exact NIST/EC goldens that verify it. The plan's own §1.2 duplication cites and §2.1 threading table
match this survey's S1/S4 precisely — it was clearly grounded in the same code.

**Gaps / corrections (all in the run-unit-state-ownership dimension — the plan under-scopes WHICH statics are
run-unit state):**

- **G1 (TOP GAP) — Intrinsics + Sort + Switches run-unit state is left process-global.** DESIGN §2.7 / PHASE-08 Step 9
  scope Intrinsics to "Pow10 dedup + clock change ONLY," so **`CobolIntrinsics._random` (`Float.cs:71`) stays a
  process-global static** even though its own comment calls it "one … sequence per run unit (§15.75.3)". Likewise
  **`CobolSort.Files` (`CobolSort.cs:36`)** — a fourth per-run-unit file registry — is left in IO with no move onto
  `RunUnit` (DESIGN §2.2 only says "CobolSort … stays in IO"), and **`ExternalSwitches.States`
  (`ExternalSwitches.cs:22`)** is listed as a type that stays in `Control/` (DESIGN §2.5) but is never made
  RunUnit-owned. Result: the exit-criterion "ONE owner of run-unit-lifetime state" (PHASE-08 Step 8 goal) is **not
  fully met** — three per-run-unit mutable stores remain process-global, so the concurrency capability the design
  advertises (DESIGN §6 Q1) is still broken by RANDOM/SORT/switch collisions, and a second in-process run unit
  inherits the first's RANDOM sequence and switch settings. **Correction: add `_random`, the SORT store, and the
  external-switch table to `RunUnit` ownership (and reset them at the lifecycle boundary).**

- **G2 — the hidden-mutable-static CI gate cannot catch G1.** The concurrency-hygiene grep
  (PHASE-08 §5, line ~603) is
  `grep -rEn "static\s+(readonly\s+)?(Dictionary|List|HashSet|int|bool|string|Func)" … | grep -viE "…|readonly .* =|…"`.
  Two holes: (a) it does **not list `Random`, `SortedDictionary`, or `ConcurrentDictionary`**, so `static Random
  _random` slips through entirely; (b) the `readonly .* =` exclusion **discards exactly the `static readonly
  Dictionary<…> X = new()` pattern** that `CobolSort.Files`, `ExternalStore.Cells`, and (as `ConcurrentDictionary`)
  `ExternalSwitches.States` use — i.e. the gate excludes most of the registries it is meant to find. The gate gives
  false confidence. **Correction: broaden the type alternation (`Random|SortedDictionary|ConcurrentDictionary|…`) and
  drop/rework the `readonly .* =` exclusion so readonly-collection registries are audited, not skipped.**

- **G3 — the ambient EC-gate nesting bug survives the migration.** The plan converts
  `ArgumentFunctionChecking`/`DataConversionChecking` from static to instance flags (DESIGN §2.6 / Step 7), which
  fixes cross-run-unit bleed but keeps them **single booleans** — the nested-checked-statement clobber (§5 risk 3)
  remains. **Correction: note that the gate needs save/restore (or a small stack) semantics, and flag it as a
  behavior item separate from the ownership move (per the plan's own "don't smuggle behavior fixes into a reorg"
  rule).**

- **G4 (minor) — clock injection is incomplete.** Step 9 routes `AcceptSource` through `IClock`, but **`CobolDate`'s
  own direct `DateTimeOffset.Now` reads (`CobolDate.cs:32,250`) are not repointed** to `RunUnit.Clock`, and ACCEPT
  `Device()`'s `Console.In` coupling (`AcceptSource.cs:95`) is untouched — so CURRENT-DATE and device ACCEPT stay on a
  different (un-injected) clock/stream than DATE/TIME. **Correction: route `CobolDate`'s current-time reads through the
  same `RunUnit.Clock`.**

**Net:** the plan fully solves the worst two smells (file-org duplication + the ProgramRegistry/Exception/[ThreadStatic]
state-ownership tangle). Its one systematic blind spot is completeness of the run-unit-state inventory — RANDOM, the
SORT store, and external switches are per-run-unit state the migration does not capture, and the CI gate meant to
catch that omission is defeated by its own filter. Close G1+G2 together (home the three stores on `RunUnit`, fix the
grep) to actually deliver the "ONE owner" exit criterion.
