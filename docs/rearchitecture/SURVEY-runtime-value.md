# SURVEY — Runtime Value Library (`Cobol.Net.Runtime` : Numeric / Strings / Text / Tables)

Status: SURVEY (rearchitecture recon). Scope: the elementary/aggregate **value** kernels a WiseOwl COBOL-generated C#
program calls — `Values`-to-be folders `Numeric/` (`CobolNum`, `NumProfile`, `CobolDec`, `CobolFloat`, `CobolEdit`,
`CobolRounding`, `CobolSizeError`), `Strings/` (`CobolInspect`, `CobolStringOps`), `Text/` (`CobolString`,
`CobolBool`, `CobolClass`), `Tables/` (`CobolTable`, `CobolDynTable`). Cross-read against the compile-time renderers
(`CodeGen/Emit/NumericRenderer.cs`, `FieldEmitter.cs`, `EmitCore.cs`) and `Binding/PicInfo.cs` to judge
numeric-model coherence and any compile/runtime duplication. Verified live via
`cobol.dll survtest.cob --std 2002 --run` (numeric multiply + edited MOVE reproduce correctly).

Bottom line up front: **the numeric model is coherent and effectively unified at ONE store funnel, but it is NOT a
single engine and its own documentation misdescribes it.** There are three spec-mandated intermediate arithmetic
engines (scaled-Int128 fixed-point, `CobolDec` decimal128, `CobolFloat` IEEE) that all converge on
`CobolNum.Store`/`TryStore`. There is **no** compile/runtime *reimplementation* duplication — the compiler emits calls
into these kernels and even invokes `CobolEdit.Format` at compile time to fold constant VALUEs (reuse, the
singular-pattern win). The real debts are documentation-vs-code drift (the "long fast-path" that does not exist), a
duplicated out-of-range-scratch policy between the two table types, and a concurrency-hostile static in `CobolTable`
that the Phase-08 audit grep would miss.

---

## 1. Responsibilities

| Folder | Responsibility |
|---|---|
| `Numeric/` | The value-level numeric substrate: scale/round/truncate/bound-check fixed-point (`CobolNum` over `Int128`); the STANDARD-DECIMAL intermediate (`CobolDec`, decimal128); the IEEE float↔fixed bridge (`CobolFloat`); numeric-edited PICTURE formatting + de-edit (`CobolEdit`); the ROUNDED-mode enum (`CobolRounding`); the evaluation-time size-error carrier (`CobolSizeError`); the runtime-facing per-item profile (`NumProfile`). |
| `Strings/` | Statement kernels, not values: INSPECT TALLYING/REPLACING/CONVERTING (`CobolInspect`); STRING/UNSTRING (`CobolStringOps`). |
| `Text/` | Character-position value semantics: MOVE/compare/ref-mod/justify over the fixed-width `string` substrate (`CobolString`); boolean-expression operators (`CobolBool`); class-condition predicates (`CobolClass`). |
| `Tables/` | Subscripted element access: fixed OCCURS (`CobolTable`); OCCURS DYNAMIC growable storage (`CobolDynTable<T>`). |

The unifying invariant across all four: **typed-native only** — values travel as native `long`/`Int128`/`double` or
.NET `string`; there is no byte substrate, no software `decimal`/`BigInteger`. Confirmed clean across every file read.

---

## 2. Key types (name · role · LOC · assessment)

| Type | Role | LOC | Assessment |
|---|---|---|---|
| `CobolNum` (static) | Scaled-integer fixed-point kernel: `Rescale`, `Store`/`TryStore`, `Divide`/`DivideOrThrow`, `RoundDiv`, `FormatDisplay`/`ParseDisplay`/`FromAlphanumeric`, over-punch codec | 419 | **Strong, the spine.** Every store lands here; ROUNDED (8 modes), three truncation disciplines, sign conventions all one place. Two warts: the **class doc is stale** (claims `long` engine w/ Int128 "escape hatch" — the code is Int128-uniform); a **dead `long Pow10`** at :404. |
| `NumProfile` (readonly record struct) | The compact per-item numeric profile (Digits, FractionDigits, Signed, SignKind, Truncation, StorageLength) threaded into every store | 76 | **Clean.** One canonical carrier; single signed `FractionScale`; no P-field duplication. The right seam. |
| `CobolDec` (readonly record struct) | STANDARD-DECIMAL intermediate (SDIDI, §8.8.1.5): decimal128, exact 256-bit mul/div scratch, round-once-to-34-digits | 278 | **Strong, self-contained.** Own wide-scratch primitives (`Mul128`/`DivRem256`/`DivRem10_256`). Coherent w/ `CobolNum` via the `ToUnscaled`→`Store(CobolDec,...)` overload. Its `DivRem256` shift-subtract is O(256)/divide (documented, acceptable). |
| `CobolFloat` (static) | IEEE↔fixed bridge (D16): `ToScaled` (double→unscaled Int128, saturating), `Display`, `InexactAtScale` | 89 | **Clean and correct** — the NaN→0 / ±Inf-saturate handling is careful; `NearestTowardZero` correctly rejects `MidpointRounding.ToZero`. Lands into the same `CobolNum.Store` funnel. |
| `CobolEdit` (static) | Numeric-edited formatting + de-edit + alphanumeric-edited; comma-mode canonicalization; MaskScale/MaskCapacity folds | 404 | **Dense but cohesive.** The largest single-purpose value file; two-pass R-to-L / L-to-R algorithm. **Invoked at compile time** for constant VALUE folding (reuse). Watch: `MaskScale`/`MaskCapacity`/`FractionDigits` each re-scan the mask (internal repetition). |
| `CobolRounding` (enum) | The 8 ISO ROUNDED modes | 34 | **Fine** — but its numeric values are pinned to legacy `PicRuntime.Round*` int constants "while both pipelines coexist." That coupling note is now **obsolete** (no byte pipeline). |
| `CobolSizeError` (Exception) | Evaluation-time size-error carrier w/ EC-name | 20 | Clean, minimal. |
| `CobolInspect` (static) | INSPECT one-shared-cycle engine (TALLY/REPLACE/CONVERT, BACKWARD) | 307 | **Strong.** Heavily spec-cited (§14.9.22 GR8–GR23); the shared-cycle model is correct and subtle. Parallel-array API (kinds/patterns/befores/afters) is verbose but deliberate. |
| `CobolStringOps` (static) | STRING/UNSTRING kernels | 128 | **Clean**, per-operand/per-receiver granularity, spec-cited; the `< 1` pointer guard the legacy engine lacked is a genuine fix. |
| `CobolString` (static) | Char-position store/compare/ref-mod/splice, PROGRAM COLLATING compare | 114 | **Clean, central.** One substrate for alphanumeric/national/boolean via a `pad` char + optional `weights[]`. |
| `CobolBool` (static) | B-AND/OR/XOR/NOT, equality, ALL-figurative combine, resize | 114 | Clean; well-cited (§8.8.2 rules 9/10). |
| `CobolClass` (static) | Class-condition predicates (NUMERIC/ALPHABETIC/zoned-sign/user-class) | 90 | Clean; the closed-Latin ALPHABETIC (not `char.IsLetter`) is correct. |
| `CobolTable` (static) | Fixed-OCCURS `ref At<T>`, `Occ` subscript decode, `OdoExtent` | 55 | **Small but two smells:** the out-of-range **`Scratch<T>.Slot` is a process-global mutable static** returned by `ref`; and the zeroed-struct scratch policy **diverges** from `CobolDynTable`'s seeded scratch. |
| `CobolDynTable<T>` (sealed class) | OCCURS DYNAMIC growable store: capacity, `RefSending`/`RefReceiving` (grow+seed), SET/UP/DOWN, SEARCH guard | 119 | **Well-built.** Instance state (no static), proper seed-per-occurrence, EC-BOUND-TABLE-LIMIT/EC-FLOW-SEARCH guards. Its scratch is `_seed()` (initialized) — the *right* policy `CobolTable` should share. |

---

## 3. Is the numeric model UNIFIED? (the core question)

**Verdict: coherent and unified at the boundary; three engines by spec necessity in the middle; misdocumented.**

- **One convergence point (the store funnel).** Every numeric result — fixed, decimal, or float — lands into a
  receiver through `CobolNum.Store`/`TryStore`. There are three overloads/paths, and they are the *only* store paths:
  - fixed-point Int128 → `CobolNum.Store(Int128, valueScale, NumProfile, mode)` (`CobolNum.cs:56`);
  - STANDARD-DECIMAL → `CobolNum.Store(CobolDec, NumProfile, mode)` (`CobolNum.cs:109`, via `CobolDec.ToUnscaled`);
  - IEEE float → `CobolFloat.ToScaled(double,…)→Int128` then the same funnel (`CSharpEmitter.cs:1094`, `:801`).
  `NumProfile` is the single capacity/scale/sign authority all three obey. This is genuine unification — a real
  singular-pattern win.
- **Three intermediate engines, selected by a discriminated `NumX`.** The compile-time carrier
  `NumX(Expr, Scale, Dec, Real)` (`EmitCore.cs:91`) routes each expression to exactly one engine
  (`NumericRenderer.Combine`, `:136`): `Real` → native `double`; `Dec`/STANDARD-DECIMAL → `CobolDec`; else scaled
  Int128. **This tri-split is ISO-mandated** (§8.8.1: native vs standard-decimal vs floating arithmetic are
  distinct), not accidental parallelism. So "parallel value engines" is the wrong frame — it is *one coordinated
  poly-engine with a common landing*.
- **The `long`-vs-`Int128` split is STORAGE-ONLY, and the docs get this wrong.** `PicInfo.ClrType` (`PicInfo.cs:246`)
  picks `long` (≤18 digits) or `Int128` (19–38) as the *stored field type*. But **all arithmetic is Int128-uniform**:
  `CobolNum`'s public surface takes/returns `Int128` throughout, and `NumericRenderer` casts every operand to
  `(Int128)` before `+`/`-`/`*` (`:142,:174,:215`). There is **no `long` arithmetic fast-path anywhere.** Yet
  `CobolNum`'s class doc (`CobolNum.cs:7-16`) says "operating entirely on hardware-native `long` … `Int128` … escape
  hatch … added when a program needs it," and `DESIGN-runtime-library.md §2.3` repeats "scaled long/Int128 kernel."
  A reader reasonably concludes a dual arithmetic path exists. **It does not** — `long` is a storage-narrowing detail
  applied only at the final `Narrow(...)` cast on the store result. This doc/code drift is the numeric model's chief
  coherence debt.

---

## 4. Compile-time (renderer) vs runtime DUPLICATION

**Finding: there is NO reimplementation duplication of numeric/string value logic between compiler and runtime.** The
compile/runtime split is clean and, in the edited path, exemplary reuse:

- The renderers **emit calls** into the value kernels; they do not re-derive value math. `NumericRenderer` emits
  `CobolNum.Divide`/`Rescale`/`MulChecked`, `CobolDec.Add/Sub/Mul/Div`, `System.Math.Pow`; trivial `+/-/*` are inline
  native operators — a reasonable "inline the cheap, delegate the hard (round/divide/store)" division of labor.
- **Compile-time invocation of the runtime for constants (reuse, not dup).** `FieldEmitter` calls the *runtime*
  `CobolEdit.Format(...)` at compile time to bake an edited VALUE literal into a C# string constant
  (`FieldEmitter.cs:170`, `:418`), and `NumericRenderer` calls `CobolEdit.MaskScale(...)` for the de-edit scale
  (`NumericRenderer.cs:80`). ONE editing implementation serves both a runtime MOVE and a compile-time constant — the
  singular pattern working as intended.
- The runtime overload triplets (`CobolNum.StoreDisplay`/`FormatDisplay` × {long, Int128, string};
  `CobolTable.Occ` × {long, string}) are a **deliberate storage-form bridge**, not duplication — they let the binder
  emit one expression whose field storage form (`StoreAsImage`) is decided later, reconciled by C# overload
  resolution. `DESIGN-data-model.md §2.1` already owns this (StorageForm makes it non-load-bearing).

The only *actual* duplication in the neighborhood is **compile-time-internal** and out of this dimension's runtime
scope: three numeric-literal parsers — `EmitText.UnscaledLit` (`EmitCore.cs:174`), `EmitText.UnscaledAtScale`
(`:201`), and `FieldEmitter.TryParseNumeric` (`FieldEmitter.cs:434`) — all parse a dot-decimal literal into
unscaled+scale, differing only in output form. `DESIGN-data-model.md §2.8` lists the triplicated `DecodeCobolString`
for consolidation but **omits these three numeric parsers** (see gap check §8).

---

## 5. Architecture smells (severity · file:line)

1. **[LOW→MED] `CobolNum` class doc contradicts the code — the phantom `long` fast-path.** `CobolNum.cs:7-16` (and
   `DESIGN-runtime-library.md §2.3`) describe a native-`long` engine with an Int128 escape hatch; the implementation
   is Int128-uniform. Misleads any future maintainer about where a performance path lives. Fix the doc (or, if a
   `long` fast-path is actually wanted, it is unbuilt).
2. **[LOW] Dead code: `CobolNum.Pow10(int)→long` (`CobolNum.cs:404`)** has zero call sites (only `Pow10Wide` is used).
   Phase-08 Step 1 already anticipates deleting it — confirm it is dropped, not table-wrapped.
3. **[MED] Two divergent out-of-range scratch policies for the same job.** `CobolTable.At<T>` returns a **zeroed
   `default` struct** whose string members are null (`CobolTable.cs:28`, self-documented as "may still fail loudly on
   a group"), whereas `CobolDynTable.RefSending` returns a **properly seeded** element (`CobolDynTable.cs:55`). Same
   "subscript-checking-off benign continuation" semantics, two implementations, one of them crash-prone. Singular-
   pattern violation.
4. **[MED — concurrency] `CobolTable.Scratch<T>.Slot` is a process-global mutable static returned by `ref`**
   (`CobolTable.cs:32-35`). Under the `RunUnit`/`AsyncLocal` concurrent-run-unit goal (`DESIGN-runtime-library.md`
   Open Q1), two run units racing an out-of-range access share and tear this slot. It is exactly the "hidden mutable
   static that should be per-run-unit" the plan warns about — **but Phase-08's audit grep
   (`static … (Dictionary|List|HashSet|int|bool|string|Func)`) will MISS it** because the field type is generic `T`.
5. **[LOW] `CobolRounding` pinned to legacy `PicRuntime.Round*` constants** "while both numeric pipelines coexist"
   (`CobolRounding.cs:8-11`). The byte pipeline is gone; the coexistence rationale is stale — the pin can be dropped
   (or re-justified on its own merits) at G8.
6. **[LOW] `CobolEdit` mask re-scanning.** `Format`, `TryFormat`, `MaskScale`, `MaskCapacity`, and `FractionDigits`
   each independently re-walk the picture counting `+/-/currency` occurrences (`CobolEdit.cs:60-87,281-307,312-359`).
   A single parsed `EditMask` descriptor would remove the repeated scans and the "mirrors Format's prologue exactly"
   fragility comment (`:279`).
7. **[LOW] Folder taxonomy is word-based** (`Text/` = value types, `Strings/` = statement kernels) — already captured
   by `DESIGN-runtime-library.md §1.4` / Phase-08 (`Values/` + `Verbs/`). Noted for completeness.

---

## 6. Coupling

- **Compiler → runtime is a genuine compile-time dependency, not just emitted strings.** Beyond the ~60 emitted
  string references the plan tracks, the compiler makes **direct C# calls** into the runtime for constant folding:
  `CobolEdit.Format` / `CobolEdit.MaskScale` (public), driven by `using CobolNet.Runtime;` in `NumericRenderer`/
  `FieldEmitter`. A namespace rename at G8 breaks these *compiler-side* calls, which the emitter's `RuntimeApi`
  façade (which governs emitted `using`s) does **not** cover. (See gap check §8.4.)
- **`NumProfile` is re-materialized as a string initializer** by `PicInfo.ProfileInitializer` (`PicInfo.cs:301`) — the
  Binding layer deliberately does not depend on the runtime value type, emitting `new NumProfile { … }` text instead.
  `ImageProfileOf` (`FieldEmitter.cs:271`) further does a `with { SignKind = … }` override for binary/packed image
  form. A field-rename on `NumProfile` breaks silently at generated-compile time today — `DESIGN-data-model.md §2.7`
  already flags routing this through the façade.
- **`CobolNum` is the hub**: `CobolEdit.Rescale`, `CobolFloat.ToScaled`→Store, `CobolDec`→Store overload, `CobolTable.Occ`
  → `FromAlphanumeric`, and `FieldEmitter` image codec (`FormatDisplay`/`ParseDisplay`) all depend on it. Refactors to
  `CobolNum`'s public surface ripple widely — but the surface is small and stable, so this is healthy centralization.
- **`CobolString` is the text hub** (Store/Compare) used by `CobolBool` resize semantics, ref-mod splices, and the
  whole-group image codec. Clean.
- `CobolDynTable` depends on `Exceptions.CobolFatalException` (`CobolDynTable.cs:3`) — the only value type reaching
  into the Exceptions namespace; correct (EC-BOUND-TABLE-LIMIT is fatal).

---

## 7. Latent-bug risks

1. **[LOW] `CobolNum.FromAlphanumeric` (`:301`) accumulates in Int128 with no width cap / no overflow guard.** An
   alphanumeric operand of >38 digit characters used in a numeric context (or as a `CobolTable.Occ(string)` subscript)
   overflows `Int128` silently. Realistically unreachable (subscripts/decodes are short), but unguarded. Spec-wise the
   value should reduce to the receiver's capacity — which `Store` does downstream — so only the *intermediate* is at
   risk.
2. **[MED, deferred-by-design] `CobolTable.At<T>` group scratch NRE.** As documented at `CobolTable.cs:18-20`, a
   group-typed out-of-range element hands back a zeroed struct with null string members; group-level use then throws.
   The fix is trivially available (`CobolDynTable`'s seed pattern) — see reorg §8.
3. **[MED, concurrency] the `ref`-returning static scratch (§5.4)** is a data race the moment concurrent run units are
   enabled. Not a bug today (single run unit per process), latent under the stated roadmap direction.
4. **[LOW] `CobolFloat.ToScaled` saturation constant `1.7014118e38` (`:39`)** is a hand-written approximation of
   `Int128.MaxValue`. Correct in spirit (guards the undefined out-of-range `(Int128)double` cast) but a magic literal;
   a named constant derived from `Int128.MaxValue` would be safer against future edits.
5. **[INFO — CLOSED by kb/Work PB1126] `CobolInspect` identifier-fed size mismatches** now set
   `EC-RANGE-INSPECT-SIZE` (§14.9.22.4 GR14/15/22) through `ExceptionState.RangeInspectSizeError`; the skip/clamp
   survives only as the deterministic outcome of the undefined result when checking is off.

---

## 8. Reorg suggestions

### 8.1 Numeric-model coherence (documentation-first, then the folder move)
- **Correct the `long`/`Int128` story.** Rewrite `CobolNum`'s class doc to state the truth: **computation is
  Int128-uniform; `long` is a storage-narrowing choice made by `PicInfo.ClrType`/`Narrow`, not an arithmetic path.**
  Update `DESIGN-runtime-library.md §2.3`'s "scaled long/Int128 kernel" phrasing to match. This is the single highest-
  value coherence fix and costs no code.
- **Document the store funnel as the unification invariant.** Add to the design: "ONE numeric landing —
  `CobolNum.Store`/`TryStore` over `NumProfile`; the three intermediate engines (fixed Int128 / `CobolDec` /
  `CobolFloat`) are spec-mandated and converge there." Today no doc states *why* the type set is coherent; it just
  asserts "coherent as-is."
- **Keep the three engines** — do not attempt to fold `CobolDec`/`CobolFloat` into `CobolNum`; the tri-split is ISO
  §8.8.1, not accidental.

### 8.2 One out-of-range scratch policy (singular pattern)
Give `CobolTable.At<T>` a seed like `CobolDynTable` — either an injected `Func<T>` element initializer or a shared
`CobolScratch` helper — so the fixed and dynamic tables share ONE benign-continuation policy and the group-NRE risk
(§7.2) disappears. This also lets §8.3 remove the static.

### 8.3 Kill the process-global `ref` scratch (concurrency)
Move `CobolTable.Scratch<T>.Slot` off the process-global static (onto `RunUnit`, or thread it like `CobolDynTable`'s
instance `_scratch`). **Extend Phase-08's §5 hidden-static audit grep** to catch generic `static T`/`ref`-returning
fields — the current pattern list misses exactly this class of state.

### 8.4 G8 namespace flip must cover compiler-side direct calls, not just emitted `using`s
Enumerate the compiler's **direct** compile-time calls into the runtime (`CobolEdit.Format`, `CobolEdit.MaskScale`,
and any `CobolNet.Runtime.*` invoked from `CodeGen/Emit/*` / `Binding/*`) alongside the emitted string surface. The
`RuntimeApi` façade governs generated `using`s; these compiler-code calls need their own `using` update in the same
G8 change or the compiler itself won't build.

### 8.5 Compile-time-internal dedup (fold into the data-model wave)
Add the three numeric-literal parsers (`EmitText.UnscaledLit`, `EmitText.UnscaledAtScale`,
`FieldEmitter.TryParseNumeric`) to `DESIGN-data-model.md §2.8`'s "one literal decoder" consolidation — a single
`CobolNumericLiteral.Parse(text) → (Int128 unscaled, int scale)` that all three project from. This is emitter-side,
but it is the same *value-literal decoding* concern §2.8 already centralizes for strings.

### 8.6 `CobolEdit` mask descriptor
Parse the picture once into an `EditMask` (fixed/floating counts, fraction scale, capacity) and have `Format`/
`TryFormat`/`MaskScale`/`MaskCapacity` read it — removes 4× re-scan and the "mirrors the prologue exactly" fragility.
Behavior-neutral; guardable by the existing edited-field goldens.

---

## ROADMAP GAP CHECK

Does the existing plan (`DESIGN-runtime-library.md` + `DESIGN-data-model.md` + `PHASE-08-runtime-library-reorg-rununit.md`)
adequately address the numeric-model coherence and the compile/runtime duplication found above?

**What the plan already covers well.**
- Pow10 dedup (all six copies, incl. `CobolNum.Pow10`/`CobolFloat.Pow10`) — `PHASE-08 §2.3` / Step 1. ✔ (This also
  disposes of the dead `long Pow10`, smell §5.2.)
- The `long`/`Int128`/`string` **storage** multiplicity and the runtime overload bridge — `DESIGN-data-model.md §2.1`
  (`StorageForm`, D0–D5) owns it thoroughly; the `Occ`/`StoreDisplay`/`FormatDisplay` triplets are correctly framed as
  a bridge, not duplication. ✔
- `NumProfile` re-materialization through a future `RuntimeApi` façade — `DESIGN-data-model.md §2.7`. ✔
- Folder role-grouping (`Values/`/`Verbs/`) — `PHASE-08` Step 2. ✔
- Value-literal decoder consolidation (strings) — `DESIGN-data-model.md §2.8`. ✔ (partial — see gaps)

**Gaps / corrections (in priority order).**

1. **The numeric-model coherence question is deferred, not answered — and the docs perpetuate the wrong model.**
   `DESIGN-runtime-library.md §2.3` declares `CobolNum`/`CobolDec`/`CobolFloat`/`CobolEdit` "coherent as-is" and
   explicitly **punts** the "force everything through Int128 even when it fits in `long`" concern to "the
   NumericRenderer/emitter dimension, out of scope." But that concern is precisely the coherence question, and the
   answer is *there is no `long` path* — the doc (and `CobolNum`'s own class comment) still describe a `long` engine
   that does not exist. **Correction:** the plan must (a) fix the `long`/Int128 documentation drift, and (b) record
   the store-funnel-over-`NumProfile` as the explicit unification invariant. Neither doc currently states either.
   (Reorg §8.1.)

2. **The two divergent out-of-range scratch policies (`CobolTable` zeroed-default vs `CobolDynTable` seeded) are
   unaddressed.** No doc notes that the two table types implement the *same* checking-off benign-continuation with
   different, one-crash-prone, code. **Add** a Phase-08 (or data-model) item to unify on one scratch policy. (Reorg §8.2.)

3. **The concurrency-hygiene gate has a hole exactly where a value type sits.** `PHASE-08 §5` greps for hidden mutable
   statics but its pattern (`Dictionary|List|HashSet|int|bool|string|Func`) **cannot match `CobolTable.Scratch<T>.Slot`**
   (generic `static T`, returned by `ref`). The stated concurrent-run-unit goal (`DESIGN-runtime-library` Open Q1) is
   silently unsafe for out-of-range table access. **Correction:** broaden the audit grep to generic/`ref`-returning
   statics and move this slot onto `RunUnit`. (Reorg §8.3.)

4. **The G8 namespace flip is scoped to emitted `using`s, but the compiler makes direct compile-time calls into the
   runtime** (`CobolEdit.Format`, `CobolEdit.MaskScale`) for constant folding. `DESIGN-runtime-library §2.8` and
   `PHASE-08 §3` frame the flip entirely around the emitted surface + `RuntimeApi` façade. **Correction:** the flip
   plan must also list and update the compiler-code `using CobolNet.Runtime;` call sites in `CodeGen/Emit/*`. (Reorg §8.4.)

5. **The three numeric-literal parsers are omitted from the literal-dedup list.** `DESIGN-data-model.md §2.8` catches
   triplicated `DecodeCobolString` but not `UnscaledLit`/`UnscaledAtScale`/`TryParseNumeric`. **Add** them to the same
   consolidation. (Reorg §8.5.)

6. **Stale `CobolRounding`↔legacy-constant pin** (`CobolRounding.cs:8-11`) is not on any cleanup list even though the
   byte pipeline it existed for is deleted. Minor; fold into the G8 close-out.

**Net:** the plan is strong on *storage-form* coherence, *state ownership*, and *folder role-grouping*, and correctly
finds no compile/runtime value-logic reimplementation. Its blind spot is **numeric-model coherence proper** — it
declares the value types coherent without reconciling the documented `long` engine against the Int128-uniform code,
without naming the store funnel as the unifying invariant, and without noticing the duplicated table-scratch policy or
the concurrency hole in a value-type static. These are documentation-and-small-code corrections, not architectural
rework — the value library is, in fact, in good shape.
