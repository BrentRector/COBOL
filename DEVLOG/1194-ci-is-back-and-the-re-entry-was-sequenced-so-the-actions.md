## Entry 1194 — 2026-08-07 02:55 PDT — CI is back, and the re-entry was sequenced so the actions bump would be attributable

**GitHub's Actions incident resolved, so `Build and Test` is `active` again** after a day in
`disabled_manually`. The wedged run (`31126140891`, the one where `cancel` reported it completed while
`force-cancel` reported a re-run that never queued) settled to `completed/cancelled` on its own once the backlog
drained — nothing had to be done to it.

**THE ORDER WAS THE WHOLE POINT, and §0 had written it down a day earlier for exactly this moment:** enable →
dispatch ONE run → confirm every job gets a RUNNER → *only then* land the actions-version bump. The temptation
is to push the bump and read one verdict; that verdict would have been uninterpretable, because a red could
equally have been the bump, outage residue, or a runner that never picked up.

⚠ **DISPATCH, NOT PUSH — and the reason is in the workflow file.** `legacy-oracle` is guarded by
`if: github.event_name == 'schedule' || github.event_name == 'workflow_dispatch'`, so a push exercises FOUR of
the five jobs. "Confirm all five jobs get runners" is only satisfiable by a `workflow_dispatch`. Baseline run
**31141695885: all five SUCCESS** on `ab691eff` with the OLD action versions — Guard, Greenfield tests,
INV-1-strong, Windows build+tests, Legacy-oracle.

**THE BUMP, on top of that baseline:** `checkout` v4→v7, `cache` v4→v6, `setup-dotnet` v4→v6 — 15 references,
five each, no other actions in the file. The target versions were RE-DERIVED from
`repos/{action}/releases/latest` (v7.0.1 / v6.1.0 / v6.0.0) rather than taken from §0's note, which had recorded
them a day earlier: a remembered version is a hypothesis, and this one happened to still be true.

⛔ **THE STANDING FACT THE OUTAGE MADE VISIBLE, now recorded in §0 as a property rather than as an incident:
`bash scripts/battery.sh` IS NOT A SUPERSET OF CI.** The battery runs on WINDOWS in DEBUG; three of the five CI
jobs are `ubuntu-latest` and the guard leg is Linux-only bash. So the Linux legs and the Release configuration
are verified by CI and by nothing else — which was true before the outage too. The outage did not create the
hole, it just made a week of it unverifiable. That is why the baseline run matters beyond the bump: it is the
first evidence since 2026-08-06 that the Linux and Release axes are green across everything that landed in
between (PB51, PB46's arithmetic half, PB33, PB49, PB46's boolean half, and the ANY LENGTH follow-up).

`session-probe.ps1`'s CI line clears itself — it prints only while some workflow is non-active — so no edit was
needed to stop it announcing a resolved condition. Its COMMENT did assert "GITHUB ACTIONS IS DISABLED" as
present-tense state, which rule 6 does not allow outside `DEVLOG.md`; it now explains the CHECK and carries the
incident as the reason the check exists.

**VERDICT — the bump is GREEN.** Push run **31143400444**: Guard · Greenfield tests · INV-1-strong · Windows
build+tests all SUCCESS; `legacy-oracle` **skipped**, which is its `if: schedule || workflow_dispatch` guard
doing exactly what it should on a push — a skipped job is not a red, and reading it as one is how a correct
guard gets "fixed". Two facts fall out of it that the dispatched baseline could not give: the **push trigger**
itself has recovered, and `actions/checkout@v7` + `cache@v6` + `setup-dotnet@v6` are green on both `ubuntu-latest`
and `windows-latest` across all four push jobs. `session-probe.ps1`'s CI line is now absent from the probe
output, self-cleared as designed.

