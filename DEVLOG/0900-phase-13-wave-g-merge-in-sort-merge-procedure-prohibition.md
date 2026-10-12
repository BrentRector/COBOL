## Entry 900 — 2026-07-18 20:20 PDT — PHASE-13 Wave G — MERGE-in-SORT/MERGE-procedure prohibition (VCR 27, COBOLNET1572)

Implemented the COBOL-2023 static prohibition (ISO §14.9.24; Annex E.2 item 20): a MERGE statement is prohibited in
the output procedure of another MERGE, or the input or output procedure of a file-format SORT (the prior standard
allowed it with conflicting rules; SORT already disallowed it — "the results ... could cause an exception to be raised
or undefined action"). Rejected at `--std 2023` with **COBOLNET1572**; below 2023 the runtime EC-SORT-MERGE-ACTIVE seam
is the (checking-off) dynamic net.

**Design — a bind-time procedure-range cross-pass** (`VersionConformancePass.GateMergeInSortMergeProc`, called from
`WalkProgram`; the deferral comment at `SortBinder.cs:206-209` removed). **The pc-alignment is the crux, verified:** a
paragraph's pc IS its index in `BoundProgram.Paragraphs` (BoundTree.cs:98), and that list is built 1:1 from
`table.Paragraphs[i]` (StatementBinder.cs:145-158) — the SAME pc space as the SORT/MERGE procedure ranges
(`SortRange` → `ctx.Table.ResolveProcedure`). Two passes: (A) collect the prohibited ranges — every file-format
`BoundSort.InputProcedure`/`OutputProcedure` + every `BoundMerge.OutputProcedure` (a SORT/MERGE with only USING/GIVING
files contributes none); (B) flag every `BoundMerge` whose enclosing paragraph-pc ∈ a prohibited range. Both passes
descend nested containers via the generated `StatementChildren()` (a MERGE nested in an IF is still in its paragraph).
The owning MERGE never false-flags itself (its output proc is a distinct named procedure). Fires only at `_edition.Year
>= 2023`.

**Verification.** CLI-probed all four quadrants: MERGE in a MERGE's output proc → 1572; MERGE in a file-SORT's output
proc → 1572; a standalone MERGE (GIVING, no proc) → NO false positive; `--std 85/2002/2014` → clean compile. Test: the
version-matrix row `merge-in-sort-merge-proc-removed-2023` (introducedIn 85, **removedIn 2023**, COBOLNET1572) — the
matrix compiles the nested-MERGE source at every edition (85/2002/2014 clean, 2023 → 1572), the natural home for a
"prohibited-at-2023" behavior (no runtime output → no `.out` golden). Registered in DIAGNOSTICS.md + the regenerated
ConstructRegistry. **Gate: the FULL Conformance project** (the `04c32a93` lesson — an acceptance-semantics change needs
the differential suites, not a CorpusRunner-only filter).

**The full gate caught a --permissive gap (fixed):** the first pass hard-coded `EditionSeverity.Error`, which failed
`VersionMatrixTests.RemovedConstruct_CompilesPermissive_WithWarning` — a REMOVED construct under `--std 2023
--permissive` must COMPILE with a warning, not error. Fixed: the severity is now
`EditionSeverityPolicy.For(ConstructAvailability.Removed, _edition)` — Error under strict, Warning (compile succeeds)
under permissive migration mode. CLI-confirmed both. (Exactly the permissive question flagged during implementation —
the matrix's permissive leg is why gating through the full Conformance project matters.)

