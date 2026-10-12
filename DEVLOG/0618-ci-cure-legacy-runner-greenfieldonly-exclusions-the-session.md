## Entry 618 — 2026-07-05 14:47 PDT — CI cure (legacy-runner GreenfieldOnly exclusions) + the session state-save

**An honest miss caught at session close: all three of today's pushes (615/616/617) ran RED on CI while every
local battery was green** — the CI's LEGACY jobs run the SHARED `tests/conformance/2002/` corpus through the
frozen oracle, and I never re-ran the legacy integration suite locally after enabling the new goldens (the
DEVLOG-604/605 precedent — oo_factory etc. ride the `GreenfieldOnly` exclusion — should have been applied at
enable time). The frozen legacy's PARTIAL UDF support actually PASSES the plain invocation goldens
(udf_invocation / inline_expression / value_args / recursion — legacy had M2-era UDFs) and the three pointer
goldens (legacy had pointers); exactly TWO programs exercise what it never implemented: `udf_exit_function`
(no EXIT FUNCTION transfer — falls through to the trailing MOVE, X=9999 for X=0014) and `udf_nested_args`
(the GR5a by-ref argument mutation / intrinsic-in-UDF legs). Both join `GreenfieldOnly` with citations; the
legacy ConformanceTests re-ran green locally. **Process lesson (persisting to memory): enabling a shared-corpus
golden = run the LEGACY conformance suite too, or add the exclusion in the same change set — CI is the
backstop, not the discovery mechanism.**

The session's state is fully saved for a cold resume: the top STATE banner carries 615→618, the reconciliation
carries both decision-complete designs + as-builts + review dispositions, and the greenfield-state memory
mirrors the map. The proven Phase-4 track cadence (recon workflow → decision-complete design INTO the
reconciliation → implement → adversarial find→verify workflow → fix ALL confirmed same-set → full battery →
commit+push) is recorded in the banner for the next session.

