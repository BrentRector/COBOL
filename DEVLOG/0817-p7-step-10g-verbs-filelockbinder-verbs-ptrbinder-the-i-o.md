## Entry 817 — 2026-07-11 19:58 PDT — P7 Step 10g: `Verbs/FileLockBinder` + `Verbs/PtrBinder` — the I/O tier's service leaves

**What (the AS-BUILT PLAN's 10g batch).** `FileLockBinder` — UNLOCK (`:225` arm rewired) + the two lock/RETRY
services the census's inverted-dependency hazard names: `CheckRecordLockPhrase` (consumed by the host's
sequential WRITE/READ/REWRITE legs + KeyedIo — its return value STAYS threaded into `BoundRead.Lock`) and
`BindVerbRetry` (KeyedIo); `BindRetry` itself stays a shared spine member on the host (OPEN consumes it
directly — internal now). `PtrBinder` — SET F7 both alternatives (the positional `GetChild(1)` ADDRESS-token
probe verbatim) + F10 UP/DOWN with the TRI-STATE contract preserved (null = fall through to the index path ·
BoundNop = error consumed · node = bound — the `:925` caller ordering untouched) + ALLOCATE/FREE (`:254/:255`
arms) + the first-bad-operand whole-statement-abort diagnostic shape; the raw `ctx.Data.ByName`
`PtrResolveBased` lookup stays BYTE-IDENTICAL (the documented SymbolTable bypass — convergence flagged, per
the plan block); zero edition gates reintroduced (all four 2002 introductions already fire in the
VersionConformancePass). `IndexFieldOf` internal (the non-consuming peek — SetBinder needs it at 10m too).

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

