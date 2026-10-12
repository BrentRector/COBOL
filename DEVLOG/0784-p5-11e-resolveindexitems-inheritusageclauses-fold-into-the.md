## Entry 784 — 2026-07-11 13:52 PDT — P5.11e: `ResolveIndexItems` + `InheritUsageClauses` fold into the ONE `UsageInheritancePass`

**What.** PHASE-05 Step 11e (DESIGN-data-model §2.7) — the no-behavior rename+merge, both effects in the same
order: the pipeline's two adjacent entries (`ResolveIndexItems` — the PICTURE-less usage-MARKER resolution:
index/object-reference group shedding, the NATIONAL/BIT `Pending` adjudication — and `InheritUsageClauses` —
group-level USAGE onto subordinate elementaries) were two halves of the one §13.18.60 GR1 job. The manifest now
declares ONE `UsageInheritancePass` (TypesExpanded → UsageResolved); the two bodies stay as PRIVATE halves under
their own names (their doc anchors — the SYNC group gate, the DEVLOG-597 recovery notes — stay true), called in
the original order by the one internal pass method. `PassPhase.UsageResolved`'s doc names the merged pass.
`BindPipelineTests` validates the chain structurally (Requires/Produces), so no test edits.

**Step 11 is COMPLETE (a–e).** Remaining in EXEC STEP C: Step 14 — docs sync + phase close.

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 32 characterization; legacy guard 353 MATCH.

