## Entry 774 — 2026-07-11 11:19 PDT — CI RED ×3 (P6.6–P6.7b): the watermark gate was [Conditional("DEBUG")] and CI's RELEASE leg stripped it — fixed ALWAYS-ON; the honest accounting

**The miss (owner flagged the red CI):** I landed the P6.6 watermark gate as `[Conditional("DEBUG")]` and validated
with the LOCAL battery — which builds Debug — then pushed P6.6, P6.7a, and P6.7b without running the CI-equivalent
RELEASE leg. CI builds/tests Release: the attribute strips every `Require` call site (including the TESTS' own
direct calls), so `Require_BeforeProducingPassRan_Throws` and `CapacityRegisters_ReadBeforeOccursResolved_Trips`
failed with "Assert.Throws() Failure: No exception was thrown" — three consecutive red pushes. This is EXACTLY the
failure mode `feedback_guard_fast_not_ci_complete` records (cost last time: 11 red pushes); I re-made it in a
smaller form by treating the local Debug battery as CI-complete. Standing correction applied to my loop: any change
whose semantics DIFFER by build configuration gets the Release test leg run locally BEFORE push.

**The fix (the design was wrong, not the test):** the gate is now ALWAYS-ON — `Require` and `RequireAll` lose
`[Conditional("DEBUG")]`. Rationale beyond the CI mechanics: the guard is a handful of integer compares per pass
entry (immaterial), and a mis-ordered pass in a RELEASE production compiler is a silent miscompile — precisely when
the loud throw matters most. A Debug-only gate created an entire Release-divergence class for zero real saving.
Verified BOTH legs locally: Release 281/281 + Debug 281/281; comments/docs corrected (DataBinder.Require,
BinderDriver.RequireAll, WatermarkTests header, the PHASE-06 STATUS ledger).

