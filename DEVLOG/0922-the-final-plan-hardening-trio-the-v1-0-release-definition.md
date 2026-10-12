## Entry 922 — 2026-07-19 18:28 PDT — The final plan-hardening trio: the v1.0 RELEASE DEFINITION, the session-probe script, the single-write rule — the plan is now DECLARED COMPLETE; execute it

Three last improvements, after which further plan work is diminishing returns:
1. **The v1.0 RELEASE DEFINITION** (a P15 checklist step): v1.0 SHIPS, not just passes — the user manual
   (installation · CLI reference · the per-edition language-support statement GENERATED from the traceability
   inventory · migration note · pointers to DIAGNOSTICS/CONFORMANCE + the A8 register) · packaging (dotnet
   tool + win-x64/linux-x64 binaries, reproducible from the tag) · semver + tag + CHANGELOG-from-DEVLOG ·
   the release gate (full battery + census guard + the §11 gates green ON THE TAG). D15-tiered: the
   language-support statement + the gate are v1.0-required; migration/packaging polish is QUALITY. Effort
   model: P15 3–5 → v1.0 ≈ 19–33 sessions.
2. **`scripts/session-probe.ps1`** (BUILT and verified, not just planned): mechanizes §0 bootstrap step ③ —
   branch/dirty/unpushed · the diag band from BOTH scans with a DISAGREE alarm (the 1573-collision class,
   now machine-checked every session) · VCR todo count · corpus counts · inventory GAP count (post-P14-Step-0).
   First run confirms: scans agree at 1577 → next free 1578, 104 VCR todos, 166 goldens · 109 negatives.
3. **THE SINGLE-WRITE RULE** (§0 preamble): live state is written ONLY in §0; every other section points,
   never duplicates (the §4 P13 row's duplicated worklist trimmed to a pointer). Duplication is where the
   stale-banner class breeds; finding live state written twice is itself a session task.

With D13–D18, the §11 backlog, the §12 risks, the effort model, the lane map, the release definition, and
mechanical state probing, the plan is COMPLETE as a plan. The best remaining improvement is execution: the
P13 grammar batch is the next unit of work.

