## Entry 309 — P1 commercial-hardening #10: runtime argument guards + CobolRuntimeException (Layer 1)

Last of the six P1 "diagnostics on invalid input" items. The runtime support routines the emitted CIL calls
did zero argument validation: a code-generation defect that passed a bad `(buffer, offset, length)` surfaced
as an opaque framework `ArgumentException`/`NullReferenceException`/`IndexOutOfRangeException` with no COBOL
context (the assessment's "compiler bug → undiagnosable crash").

**Layer 1 (this entry).** New `CobolRuntimeException` (runtime `RT####` codes + Operation + Target, mirroring
`StopRunException`) and a `RuntimeGuard.Buffer(area, offset, length, op, target)` helper that throws it on
null/negative/out-of-range — the same comparison the CLR's own bounds check performs immediately after, so it
**never trips on a valid run** (verified: full guard green). Guarded the layout-driven facades:
- `FileRuntime` — WriteRecord, ReadRecord, ReadPreviousRecord, ReadByKey (record + key), WriteRecordVariable
  (after the existing negative-length clamp), Rewrite, StartFile;
- `PicRuntime` — DecodeNumeric, EncodeNumeric (the hot numeric primitives);
- `SortRuntime` — RELEASE buffer + converted the 3 bare `InvalidOperationException` ("…not initialized")
  to a typed `CobolRuntimeException` (RT0002).
A misuse now reads `RT0001: internal runtime error during WRITE on 'CUSTOMER-FILE': buffer access out of
range (offset=…, length=…, bufferLength=…)` instead of a context-free framework throw.

**Deferred — deliberately, with rationale:**
- **`StorageArea` MOVE/compare primitives.** These are the hottest per-statement path AND receive
  *reference-modification*-computed offsets/lengths (`X(start:len)`). An out-of-range ref-mod is a runtime
  condition some corpus programs may exercise leniently, so guarding here risks false rejections — it needs a
  ref-mod-semantics analysis + its own dry-run. The file/numeric/sort facades above are layout-driven (offsets
  from fixed record/field layout), hence safe to guard now.
- **Layer 2 — top-level catch in the EMITTED program's `Main`** (clean exit 70 + suppressed stack for an
  uncaught `CobolRuntimeException`). This requires emitting an IL exception-handler region in `CilEmitter`
  with a correct `leave`; a malformed EH region is an `InvalidProgramException` for *every* emitted program
  (blast radius = all 349 baselines), so it warrants its own careful pass with `ilverify`. It pairs naturally
  with the P2 roadmap item — the `Dispatch` recursion guard (RL111A `StackOverflowException`) — which also
  needs Main-level handling. Note: even *uncaught*, a typed `CobolRuntimeException` already yields a clear
  managed message + non-zero exit — strictly better than the opaque framework throw it replaces.

**Verify:** build 0 warnings; +13 unit (`RuntimeGuardTests`: null/negative/range → throw, in-range → no
throw; FileRuntime.WriteRecord bad offset → RT0001/WRITE/target; PicRuntime.DecodeNumeric bad range → throw,
valid → decodes 1234; SortRuntime.ReleaseRecord uninitialized → RT0002). Full guard **ALL GREEN — 1034 unit
(1021 +13) / 347 integration / 349 NIST baselines, 0 FAIL\*** — confirming the guards are invisible on every
valid run.

