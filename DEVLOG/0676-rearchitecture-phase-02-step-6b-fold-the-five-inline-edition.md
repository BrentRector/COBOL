## Entry 676 — 2026-07-07 21:49 PDT — Rearchitecture PHASE 02 step 6b — fold the five inline edition gates into the registry

Killed the parallel gating mechanism (P4): the five inline binder gates that emitted pinned 08xx/0882 codes via
`data.Edition.Error/Removed(...)` outside the `ConstructRegistry.Check` funnel — and so never entered the version matrix —
are now `constructs.json` rows routed through `Check`. Each fold keeps its STRUCTURAL guard and drops the redundant
edition `if` (Check evaluates availability via `StatusAt`): `Accept.cs` `if(AcceptHasTerminator)` → Check(EndAccept2002);
`StatementBinder.cs` `if(roundingModeName)` → Check(RoundedModeIs2014); `AlterSwitches.cs` bare-GO-TO → Check(BareGotoRemoved2002),
ALTER → Check(AlterRemoved2002); `Call.cs` `if(isOverflow)` → Check(CallOnOverflowRemoved2023). The pinned codes are
preserved (single-edge rows keep their `diagnosticCode`; the removal rows emit it via the strict/permissive seam); the
message text moves to the uniform Check template, chosen so the substring assertions still hold
(display "the ALTER statement" / "GO TO without a procedure-name…" keep ALTER / GO TO; the ROUNDED message keeps
"COBOL-2014"). Four new active `constructs.json` rows (alter/bare-goto removed-2002, call-on-overflow removed-2023) +
reconciled `rounded-mode-is-2014` (diagnosticCode 0900→0803 + expectDiagnostic) — each with a positive fixture, now driven
by `VersionMatrixTests` (accept/reject per edition + the pinned code, incl. the new permissive-warning coverage the inline
gates never had). Re-baselined tests stayed green unchanged (`StandardDecimalTests` 0803+"2014", `AlterSwitchesDifferentialTests`
ALTER/GO TO, `CallExceptionPhraseEditionTests` 0882).

**⚠ Transparency — the END-ACCEPT (0816) gate is UNREACHABLE dead code, caught by the battery.** My first `end-accept-2002`
fixture failed to compile at every edition: the `acceptStatement` grammar rule is `ACCEPT dataReference (FROM acceptSource)?`
— it has NO `END_ACCEPT`, so `AcceptHasTerminator` is always false and the inline 0816 gate never fired (that is why no
0816 test ever existed). Adding END-ACCEPT parsing is a grammar feature, out of scope for a fold. So 0816 is still folded
through `Check` (uniform, forward-compatible) but its row is marked **`pending`** — catalogued + edition-frozen by the drift
tests, its impossible compile SKIPPED by the matrix — flipping active when END-ACCEPT parsing lands with a real fixture.

Greenfield-only (binder gates, no grammar change → legacy guard invariant): conformance **2055** (+19 active matrix cells) ·
unit **224**. Build 0/0. 84 active / 11 pending constructs.json rows.

