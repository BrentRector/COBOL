## Entry 1996 — 2026-10-12 23:44 PDT — Enqueue leads filed: PB3119-PB3122, and a PB175 line

Clerk pass over four implementer reports' "New defects" sections.
- PB3119 (new): a failed `>>DEFINE` leaves a later `>>IF` reporting an undefined compilation variable; from the PB1616 report. Names landed PB1367.
- PB3120 (new): the group-formal arm of `StrongTypeModel.SameAnalyzedProfile` compares the restricted prototype name; from the PB2495 report, item 1.
- PB3121 (new): the TO-phrase screen admits `UnitSelfName` but `ProgramPrototypesOf` registers it only with a written specifier; from the PB2495 report, item 2. Points at open PB2626.
- PB3122 (new): the inventory test-ref resolver exists in `SpecTraceabilityInventoryDriftTests.UnresolvedTestRefs` and `audit_annex_a1.py#check_pins`; from the PB3017 report.
- PB175 (extended): one dated line, the raw-diagnostic-code-literal baseline lockable at 198, from the PB1616 report.

No code changed. The PB3031 report's "possible sibling" had no repro and was skipped.

- PB3123 (new): the DEVLOG number is stamped before GitHub orders the merge queue, so the lower number bounces (PR #25 behind #26, 2026-10-13). Cluster PB3025, PB3023, PB3107.
- PB3108 (extended): the 1000-leaf relation test went RED again under the working PB3104 gate cap.
- PB3107 (extended): a "Measured 2026-10-13" paragraph, the oracle-baseline race also bounces in the merge queue.
