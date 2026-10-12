## Entry 588 — 2026-07-03 19:35 PDT — THE P2.7 FLIP: permissive continuity + the first removal gate + INV-1-STRONG AT 2023 — 349/349 goldens byte-exact at the default edition

**The roadmap's most carefully-ordered commit — the flip, the first gate, and the fatal-challenge fix — lands
as ONE change set, exactly as Phase 1 prescribed:**
- **The LABEL RECORDS gate** (the first P2.6 removal, shipped WITH the flip because every NIST FD writes it):
  `VisitLabelRecordsClause` → `ConstructRegistry.Check("label-records-removed-2002")` → 0902 strict ≥2002 /
  warning permissive. Registry + constructs.json rows land together (the drift discipline held).
- **The harness axis** (P2.7): `EditionHarness.CompileFull` (Ok, Errors, Warnings) + permissive params +
  `AssertNoDiagnostic`; `VersionMatrixTests` gains the `expectDiagnostic` reject-cell assertion (the
  two-obligation rule's diagnostic half) and the **permissive theory**: every removed-construct row × edition
  ≥ removedIn MUST compile permissive with its code as a WARNING (the §10 #1 migration contract).
- **INV-1 RESTATED:** the continuity legs (`Cobol85Program_StillCompilesAtLaterEdition` + the sweep script)
  flip to `--permissive` at ≥2002 — under strict, later editions legitimately reject the removed '85 elements
  every NIST program carries; a PERMISSIVE break is a regression, full stop. ST101A's fact updated: the
  validator fail-fasts BEFORE Emit so its 4× LABEL 0902s surface first (the SD 0873 re-asserts when P2.6
  migrates it validator-side).
- **The interval matrix rows:** user-word-commit-2023, user-word-raising-2002 (the 585 correction),
  receive-as-user-word AND end-receive-as-user-word (the mechanically-discovered third re-reservation) — each
  one f(case,V) row encoding reservation intervals, with 0901 expectDiagnostic.
- **⛔🎉 THE FATAL-CHALLENGE FIX EXECUTED — INV-1-STRONG AT 2023 IS GREEN:** `COBOLNET_NIST_STD` /
  `COBOLNET_NIST_PERMISSIVE` env overrides re-target the whole golden run; at `--std 2023 --permissive` the
  triage found **all 36 failures were the three un-migrated binder Error sites** (27× 0873 SD DATA RECORDS,
  4× 0810 ALTER, 5× 0882 CALL ON OVERFLOW) and **ZERO behavioral diffs** — the P2.6 binder migrations
  (Error→`Removed()`, all four sites verified bind-after-diagnose so permissive runs the 85 semantics) were
  folded in, after which **349/349 goldens are BYTE-EXACT at the shipping default edition**. The MaxDigits
  18→31 capacity change shifted nothing. The re-run is one env-var away for the Phase-8 promotion to a G7
  exit criterion.
- Suites: conformance 1098/1098 (20 new strict cells + the permissive theory) + unit 38/38 + full guard.
- **⚠ Guard-flake pattern logged (transparency): TWICE today** guard-fast reported a single-program COMPILE
  FAILED regression (first an unnamed test pre-583, now NC141A) that VANISHES on re-run and reproduces
  neither via the CLI nor the conformance suite — suspected JOBS=32 parallel-compile race in the guard's
  compile step. Two data points; if it recurs, the guard's parallel compile gets a root-cause dig (it would
  eventually bite CI). Never mask it: the guard is always run with its true exit code captured now (the
  tail-pipe lesson from 583).

