## Entry 1705 — 2026-09-25 04:54 PDT — Train 64: w60a, gate evidence (PB1573+PB1574+PB1583), tooling only, landed alone

**Wave 60 A — gate evidence (kb/Work PB1573, PB1574, PB1583).** No compiler change; three gate-instrument defects.
**PB1573** (reproduced): the retired keyword filter in `build-local.ps1` (case-insensitive `-match`) kept a failing
test's `Failed …` header and `Error Message:` label and DROPPED the message and the stack; `build-local.sh` carried the
same filter case-sensitively, and `guard-fast.sh` printed the last 6 matching lines in CI, where the log dies with the
runner. Fix shape: ONE reporter, `scripts/test_leg_report.py` — a GREEN leg prints its verdict line plus the log path,
a RED leg (non-zero exit, `Failed!`, or no verdict line) prints its COMPLETE output, exit 2 (log unreadable) is red —
called by both build-local twins and guard-fast's legacy unit/integration legs; the sibling in `battery.sh` (the
characterization leg kept no TRX and, at `--verbosity quiet`, no failure text) now writes a TRX. Drift test
`TestLegReportDriftTests` runs the reporter's `--self-test` (a planted message in a real DEVLOG 1700 failure
transcript must reach the report; both retired filters are calibration arms that drop it) and refuses any script line
that filters test output by keyword. **PB1574** (reproduced; premise stale): the four static audits were kept out of
CI because the checkout "cannot see the spec", but `specs/ISO_COBOL.md` is tracked. A new `audits` job (needs
`changes`, no `run_matrix` condition, so it runs on docs-only pushes too) runs code citations, doc citations, evidence
supersession, witness loss (`--base` from a new `changes.outputs.base`) and `work.py check`, one step each under
`!cancelled()`, and `ci-gate` lists it in `needs:`; `audit_code_citations --check` now REFUSES (exit 2) when the spec
is absent instead of skipping PHANTOM and the ordinals. **PB1583** (premise REFUTED): the conflict-marker sweep's red in
DEVLOG 1700 was recorded as a share-mode race, but the corpus runner reads with Read/Read share (compatible with
`File.OpenRead`) and a share conflict raises `IOException`, not the recorded `UnauthorizedAccessException`; 53 sweeps
concurrent with the whole `~CorpusRunner` leg (3,173 cases) were all green. The hardening landed anyway:
`ConflictMarkerDriftTests.Sweep` opens with `FileShare.ReadWrite|Delete`, reports a refused open per file with its
exception and attributes and keeps sweeping, and both tree tests go red with that list — so the next occurrence is
attributable. Each new test was seen to fail (restored `File.OpenRead`; restored guard-fast grep; a trimming mutant of
the reporter failed 7 of 13 self-test arms). No rows: all three notes `landed` with `closes_rows: []` and a
`closes_rows_reason`. Docs: plan §9 citation-gate paragraph, `DOC_INDEX.md` audit rows + `test_leg_report.py`.

**Train 64.** One cluster, landed alone on purpose (tooling only; the proof of the workflow change is this landing's
own `ci/<sha>` run, where the `audits` job must appear and pass under `ci-gate`). Brought in from
`worktree-agent-a94f7934378180045` @ 29b42ed4a (base bd139c33b) with no conflicts; rebased onto train 63 (938ef8a28)
cleanly, outside-docs conflicts none, so no re-gate. Gate (whole assemblies, `build-local.ps1 -Filter
"~CobolNet.Tests"`, Normal priority): Conformance 8838/8838, Unit 29302/29302, Characterization 33/33 —
`WAVE-LOCAL GATE: GREEN`; legacy `CobolSharp.Tests.Integration` 503 passed, 1 skipped, 0 failed; the four new tests
passed by name. The external corpus was fetched in this worktree. semgrep `verify.py` PASS (every count unchanged).
`work.py check` ✓ 1526. GAP 1248 → 1248 (no rows). Nothing dropped. One misstep, logged: the first gate launch used
`-Filter "~Cobol.Net.Tests"` — the namespace is `CobolNet.Tests` — and build-local's PB708 population check refused it
as a DEAD term (`NOT RUN`, exit 2) rather than letting it pass as a silent green; relaunched with the right name.

