## Entry 616 — 2026-07-05 08:40 PDT — Phase 4 track (c): the EXIT FUNCTION leg (M2-PROC-6) closes

The small leg DEVLOG 615 unblocked, landed as a pure mirror of the EXIT METHOD pattern (feedback_singular_
pattern — no new mechanism anywhere): `UdfBindExitFunction` replaces the `BoundUnsupported` stub. Inside a
function definition (the `UdfSelfName` flag from 615) EXIT FUNCTION is the function-return synonym — bound
to `BoundGoback`, riding the §14.9.18.4 GR5 semantics end-to-end (the activation terminates, the RETURNING
item's value becomes the result; an optional RAISING tail stages exactly like GOBACK RAISING). Outside one
it is the 0827 EXIT-family placement violation (EXIT PROGRAM-in-method and EXIT METHOD-outside-method
already share that band). The statement was REMOVED by ISO 2023 (Annex E.2 :49036) — the
`exit-function-window` registry row (introduced 2002 / removed 2023) was already wired in the
EditionValidator's VisitExitStatement; what was missing was ONLY the pre-removal semantics.

The window's matrix row flipped pending→ACTIVE, discharging its documented reactivation contract (the
DEVLOG-595 W2 review had corrected its rationale: the old placeholder source compiled at 2002/2014 only
because the placement SR was unimplemented, and needed a FUNCTION-ID witness + per-edition
expectDiagnostic). The new witness is a whole-source caller + FUNCTION-ID unit: expectDiagnosticBelow
COBOLNET0900 at 85 (the {is2002()}? PD-header RETURNING hint), compiles 2002/2014, COBOLNET0902 at 2023
strict, and the RemovedMatrix theory proves the §10 #1 permissive migration contract (compiles + 0902
warning). This also discharges the DEVLOG-611 deferred-from-review note "exit-window conforming witnesses
need METHOD-ID/FUNCTION-ID units (Phase 3/4c)" for the FUNCTION half.

New golden `udf_exit_function` (6th udf_*, byte-exact first run, expectation derived before execution): the
early exit is IF-guarded so the trailing `MOVE 9999 TO L-R` stays emit-live — X=0014 proves the transfer
happened (9999 would mean fall-through). UdfInvocationTests +2 (0827 placement; the 2002-ok/2023-0902
window edges). Battery: conformance 1653/1653 (+8) · unit 123/123 · matrix exit-function-window ACTIVE
across all four editions + the permissive leg · zero regressions. Track (c) residue is now exactly
M2-UDF-3 (prototypes) + the M2-UDF-4 legs (ALL INTRINSIC bind, the FUNCTION-keyword-omitted form).

