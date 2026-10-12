## Entry 720 — 2026-07-09 15:57 PDT — PHASE-03 Step 14c: 7 attribute-conditioned statement gates relocated to the pass

The second batch of statement gates moves binder→`VersionConformancePass` — those conditioned on a resolved node
ATTRIBUTE the binder already records (Group B of the recon ledger), not a distinctive node type. Each keeps its exact
`constructId` + where-string (byte-identical for the tested single-occurrence case):

- **FileSharingClause2002** — OPEN SHARING phrase → `BoundOpen.SharingOverride != null`.
- **CallByValue2002** — CALL … BY VALUE → a `BoundCallProgram` whose `Args` use `CobolPassMode.Value`.
- **GobackReturning2002** — GOBACK … RETURNING → `BoundGoback.ReturningSource != null`. The binder's bare-GOBACK
  introduction gate (0880, a non-Check `edition.Error`, stays binder-side per exit-criterion-8's "zero `Check` calls"
  scope) is collapsed to fire only when there is NO RETURNING and `--std < 2002` — preserving the "the 0900 subsumes
  the 0880" behaviour (a below-2002 GOBACK RETURNING yields exactly the pass's one 0900).
- **ReadPrevious2002** — READ … PREVIOUS → `BoundKeyedRead.Kind == Previous`.
- **RecordLockPhrase2002** — READ … ADVANCING ON LOCK → `BoundKeyedRead.AdvancingOnLock`. (The OTHER
  `RecordLockPhrase2002` source — the WITH LOCK/NO LOCK/IGNORING phrase, a different where-string via
  `CheckRecordLockPhrase` — stays binder-side; it needs a new `Lock` field on the sequential WRITE/READ/REWRITE nodes,
  a 14d C-flag.)
- **StartFirstLast2002** / **StartWithLength2002** — `BoundKeyedStart.Mode ∈ {First,Last}` / `.Length != null` (two
  independent 2002 phrases on one START; both gate, in the binder's order).

The pass restructures `BoundKeyedRead` / `BoundKeyedStart` / `BoundCallProgram` as multi-condition blocks (a node may
carry two independent gated phrases, so mutually-exclusive switch arms won't do). **Untested edge (design-sanctioned):**
a gate the binder fired per-occurrence in a loop (OPEN with two SHARING clauses; CALL with two BY VALUE args) now fires
once per node — a diagnostic-count reduction on an already-rejected program; the tested single-occurrence cases are
diagnostically identical. **Battery:** edition/keyed-IO/call tripwires **1840/1840**, conformance **3114**,
characterization **32**, unit **227**, FULL legacy guard NIST **353 MATCH** (0 regressions — greenfield-only).
NEXT — **14d:** the flag-backed statement gates (add ~5 semantic flags: CALL ON OVERFLOW, END-ACCEPT, STOP…STATUS,
ROUNDED MODE IS, sequential record-lock, RETRY, INVOKE-explicit).

