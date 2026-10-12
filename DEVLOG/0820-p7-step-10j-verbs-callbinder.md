## Entry 820 — 2026-07-11 20:21 PDT — P7 Step 10j: `Verbs/CallBinder`

**What (the AS-BUILT PLAN's 10j batch).** CALL/CANCEL/GOBACK over `(ctx, host)`, with the two hooks that
OUTLIVE the batch kept as host edges exactly as planned: `InMethod`/`OoBindMethodGoback` (the OO half
converts LAST at 10s) and `EcBindRaising` (EcBinder lands at 10r) — all three made internal on their
partials. The 0884 (CALL RETURNING <2002) and 0880 (bare GOBACK <2002) gates moved VERBATIM, including the
0880 `dataReference() is null` condition that encodes the 0900-subsumption contract with the
VersionConformancePass (a GOBACK RETURNING below 2002 yields exactly ONE diagnostic — the pass's). The five
bound types (BoundCallArg/BoundCallProgram/BoundCancel/BoundExitProgram/BoundGoback) → records-only
`BoundCall.cs` (Udf cross-constructs BoundCallProgram; the pass reads UsedOverflowSpelling).

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

