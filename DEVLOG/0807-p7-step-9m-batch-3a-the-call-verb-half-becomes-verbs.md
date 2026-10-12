## Entry 807 — 2026-07-11 17:48 PDT — P7 Step 9m/BATCH-3a: the CALL-verb half becomes `Verbs/CallEmitter`; the pipeline discipline lands (owner feedback)

**Owner feedback honored:** the speed answer was buried mid-stream and the interrupted cycle had regressed to a
singleton — from here cycles PIPELINE: batch N's conformance runs on the prebuilt binaries in the background
while batch N+1's edits are authored; commits stay verdict-gated (this entry's verdict landed while BATCH-3b's
script was being written).

**What.** `Verbs/CallEmitter.cs` (over ctx, num, ecState, callState, **EcEmitter direct** — the first
collaborator taking the EC services by ctor instead of host shims — and host): CALL with the BY
REFERENCE/CONTENT/VALUE carriers, the EC-PROGRAM catch + RAISING propagation pickup, CANCEL/GOBACK/EXIT
PROGRAM, and the ONE CALL-boundary string-carrier trio (`CallPlaceIsString`/`CallStringRead`/`CallStringWrite`,
now `CallEmitter` statics — ReportWriter/Oo/program-class references retargeted). The Call partial keeps the
run-unit/program-class half until 9n's ProgramEmitter. Ratchet: 27 = 14 + 13 exact.

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

