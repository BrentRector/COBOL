## Entry 736 — 2026-07-09 23:26 PDT — PHASE-03 Step 14g.3 review-fix: OCCURS DYNAMIC over-fired in report/screen scopes (shared-rule hazard)

An adversarial find→verify review of the 14g.3 commit (`729b6c4f`, one subagent, H1–H5) found **1 confirmed
byte-neutrality violation** — and it exposes a general hazard for the whole parse-arm approach: a grammar rule shared
across sections. Fixed at the root cause; 2 regression witnesses added. (H1 CheckOnly divergence, H2 count, H4
where-string, H5 level-88 guard all cleanly refuted — notably H1 is the OPPOSITE of the 14g.2 finding: `OoClassTable.Build`
is on the BIND path (`CompilerDriver.cs:110`, before the CheckOnly early-return), so it ran in CheckOnly all along.)

**The finding.** `occursClause` is a grammar rule SHARED by three sections — data-description (`CobolData.g4`),
report-writer (`CobolReportWriter.g4:90`), and screen (`CobolScreen.g4:59`) — but the former `OccursDynamic2014` gate
lived in `OdoBindOccursSpec`, reached from EXACTLY ONE binder path (`BindEntry`, data only). Report groups route OCCURS
to COBOLNET0899 (unimplemented) and screen sections are unbound, so neither ever hit `OdoBindOccursSpec`. The new
`ParseArm.VisitOccursClause` walks the WHOLE tree and fired on every DYNAMIC occursClause, so it OVER-fired there:
- Report group at 85: OLD `{0899}` → NEW `{0899, 0900}` (an extra 0900).
- Screen section at 85: OLD `{}` (compiles clean) → NEW `{0900}` — a VERDICT change (compiles → rejects).

The 14g.2 data-clause gates (BASED/TYPE/PROPERTY/TYPEDEF) are NOT affected — those four clauses appear only under
`dataDescriptionClause`, never in the report/screen grammars (verified). Only `occursClause` is shared.

**The fix.** The guard's polarity was NEGATIVE ("fire unless in a level-66/88 entry"), which fires by default when
there is NO enclosing data entry — exactly the report/screen case. Replaced it with a POSITIVE test,
`InGatedDataEntry` (fire only when inside a non-66/88 `dataDescriptionEntry`), reproducing `BindEntry`'s exact reach —
method WS/LS/LINKAGE all use `dataDescriptionEntry`, so method scope is not under-fired. All FIVE data-clause gates now
use this one positive helper (byte-exact for the four data-only clauses, which always have a `dataDescriptionEntry`
ancestor; the fix for OCCURS DYNAMIC). `OoOccursDynEditionTests` +2 regressions (`OccursDynamicInReportGroup…` /
`OccursDynamicInScreenSection…`), 8 total.

Byte-neutral restored: characterization 32 byte-exact. Battery: greenfield conformance **3139** (+2) · unit **223** ·
characterization **32** · INV-1-strong @ 2023 permissive **349/349**. Greenfield-only change (`VersionConformancePass`
is not in the legacy compiler) — the FULL legacy guard runs in CI as the backstop. RESUME AT Step 14g.4.

