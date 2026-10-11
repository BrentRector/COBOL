## Entry 1759 — 2026-09-28 13:53 PDT — Learnings must be vetted: LEARNINGS.md and the status guard withdrawn from the public skills; PB1711 validation filed; the guard is a project experiment

**Owner:** "We need to validate all learnings first. It's reasonable that the devlog summarizes to a learning that
subsequently we show to be incorrect. The LEARNINGS file should always only contain the final vetted learnings,
battle tested and proven useful. Only those learnings should be encoded into a skill." Asked how to handle what was
already public, he chose "Withdraw now".

The LEARNINGS.md published this morning (claude-skills v1.10.1) was compiled straight from this log. The log records
hypotheses as well as conclusions, and the same day showed one refuted: "transcripts are written lazily" (PB1702).
So all 145 items are CANDIDATES until validated.

**Public (claude-skills v1.11.0, pending the owner's push):**
- LEARNINGS.md is withdrawn.
- agent-fleet's `status_guard.py` is withdrawn. Its sandbox A/B was null for staleness, and it is unproven in
  production.
- The new devlog skill carries the three-stage pipeline (devlog → CANDIDATE learnings → VALIDATION → vetted
  LEARNINGS) and a validation brief.
- Kept, but first in the validation queue: the stamped handoff (88-agent A/B), `stall_watch.py` (it caught two real
  hangs on its first live run), and the rolling-wave ceiling and dead-agent handling (fixes for reproduced defects).

**Project.**
- The guard stays as a local experiment: `scripts/hooks/status_guard.py`, a single copy now that the public one is
  gone, wired from `.claude/settings.json`. Production measurement over waves 71+ against the 21 % baseline is what
  can vet it (PB1701).
- MANDATORY-PRACTICES P4 says so.
- PB1711 files the validation of every candidate and the audit of every public skill rule against a vetted
  learning.

**Also.**
- Train 70b landed: `8a20a95ea`, DEVLOG 1758, GAP 883 → 866.
- The U2 finisher landed PB1075 on its branch with a CURRENT status stamp, then ended without a structured result
  mid-PB1083. Its work is preserved as a labelled commit, and status_delta reports STALE by 1. U2 lands in the next
  wave's first train, which gives it the full gate it has not had.
- Every landed wave-70 worktree is removed.

