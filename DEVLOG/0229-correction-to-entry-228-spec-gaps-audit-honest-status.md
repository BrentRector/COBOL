## Entry 229 — CORRECTION to Entry 228 + spec-gaps audit (honest status)

Two claims in Entry 228 / the first cut of docs/spec-gaps.md were WRONG. Correcting them here rather
than rewriting history.

1. **WHEN-COMPILED is NOT actually fixed yet.** Entry 228 added a baked-constant special-case in
   `CilExpressionEmitter.EmitIrIntrinsicCall`, and the source is present in HEAD (commit 00713a9) and
   correct *for that path* — but `MOVE FUNCTION WHEN-COMPILED TO x` still returns the execution-time
   clock. Verified end-to-end with a guaranteed-fresh build: two runs of one compiled DLL produce
   different timestamps (2026053011401818 vs …402121). So the value reaches the program through a
   path that does NOT pass the special-case (the MOVE-of-function lowering builds an `IrFunctionCall`
   whose emit, despite nominally calling `EmitIrIntrinsicCall`, does not yield the baked constant —
   to be traced). The integration test `IntrinsicWhenCompiledTests` only checks format + date, so it
   passed without proving the fix; that test does NOT guard cross-run stability. **STATUS: open bug,
   partial code in place, not effective.** Do not treat WHEN-COMPILED as done.

2. **LENGTH / FUNCTION LENGTH of an OCCURS DEPENDING ON group IS a real bug** (the first audit cut
   wrongly called it "verified-works"). Proven: `FUNCTION LENGTH(TBL)` over `ELT … OCCURS 1 TO 10
   DEPENDING ON N` returns 40 (max 10×4) for both N=3 and N=7; spec requires 12 and 28. Root cause:
   `ExpressionBinder.BindLength`/`StaticLength` fold LENGTH to a compile-time constant
   `Symbol.ElementSize` (the max layout) and have no ODO branch. A correct fix must compute
   base + dependingOnValue×elementSize at RUNTIME for an ODO group (and `LENGTH OF` likewise).
   **STATUS: open bug.**

What Entry 228 got right and remains TRUE: ON SIZE ERROR is verified correct (the apparent bug was
dead code + a wrong test expectation), and the §2 "verified-works" features in spec-gaps.md (PERFORM
VARYING AFTER, INSPECT CONVERTING, abbreviated conditions, multi-target SET, OCCURS DEPENDING ON
compile, DIVIDE REMAINDER, FUNCTION REM/MOD) were each empirically confirmed.

Net spec-conformance sweep result: 0 of the originally-suspected silent bugs are actually fixed yet
(WHEN-COMPILED in progress, ODO-LENGTH open, ON-SIZE-ERROR was never broken). docs/spec-gaps.md
updated to match. Guard remains ALL GREEN (1000 / 343 / 148) — these are latent correctness gaps with
no baselined coverage, not regressions.

