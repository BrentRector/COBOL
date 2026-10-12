## Entry 648 — 2026-07-06 21:49 PDT — M2-OO-1i inc 5 (FINAL): EXTERNAL object/factory FD shares the one run-unit connector + record area — M2-OO-1i COMPLETE

Increment 5 closes M2-OO-1i: an `FD … IS EXTERNAL` in an OBJECT/FACTORY paragraph joins the run-unit-shared file
connector and record area exactly as a program's does (§13.18.22.4 GR4a/GR4b — one connector per external name, one
record area; §9.1.5 — an external connector is referenceable from any describing runtime element). The qualification
(`::EXT::name`, both `OoQualifyClassFiles` branches) and the record re-basing (`CallBindExternalAndGlobal`, which runs
on the class binder via `BindResolve`) were already in place from inc 3-4 — the missing half was emit-side.

**Root-cause fix (again caught by an incremental compile-and-run):** the EXTERNAL record-area backing property
(`private ref string _redef_X => ref ExternalStore.Cell("FD::name", …).Ref`) was emitted only in the PROGRAM emit
path, so a class EXTERNAL FD referenced a `_redef_O_REC` that was never declared (CS0103) — the same class-emit-gap
shape as inc 3's `anyFiles`. Extracted `EmitExternalBackings(data, w)` and call it from BOTH the program path
(`CallEmitProgramClass`, byte-identical) and the OO type-half (`OoEmitTypeHalf`, after `fields.Emit()`).

Golden `oo_external_file_shared` (GreenfieldOnly): a driver PROGRAM and a CLASS OBJECT each describe `FD EF IS
EXTERNAL`; the program OPENs OUTPUT + WRITEs record 1 through the shared connector, the object's method WRITEs record
2 **without opening** (the open mode lives on the shared connector) and through the shared record area, the program
reads both back → `R1=SHAREDABCD` / `R2=OBJWROTE12`. Proves the ONE connector (the object wrote through the program's
open) + the ONE record area (O-REC and P-REC re-based onto the same `ExternalStore` cell). Battery: **1969 conformance
(+1) · 216 unit · legacy integration 79 GREEN** (the EXTERNAL differential IC-227A-shape tests confirm the program
path stayed byte-identical), zero regressions.

**⛔🎉 M2-OO-1i COMPLETE (DEVLOG 644-648):** the OBJECT/FACTORY ENVIRONMENT + FILE division, referenceable from method
bodies. A method itself owns NO ENV/FILE/WS (§12.4.3/§13.4.3/§13.5.3 SR1 → COBOLNET1519); a factory file is a class
singleton; an object file is a per-object connector (§9.1.4); an EXTERNAL class FD shares the run-unit connector; a
GLOBAL class/method FD is COBOLNET1520 (§13.18.27.3 SR4). AS-BUILT + the 2 spec-correction deviations are recorded in
`docs/PHASE4_RECONCILIATION.md` §M2-OO-1i. RESUME NEXT: the low-severity M2-PRE preprocessor follow-ups; then Phase 6
(OCCURS DYNAMIC, TYPEDEF, floats) / Phase 7 (2023 finalization).

