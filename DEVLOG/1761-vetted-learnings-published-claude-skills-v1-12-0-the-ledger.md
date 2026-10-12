## Entry 1761 — 2026-09-28 16:04 PDT — Vetted learnings published (claude-skills v1.12.0); the ledger records where each lives

The owner pushed claude-skills v1.12.0 (fc991f8), which applies validation run 1 under his rule "Remove refuted,
flag unproven":
- LEARNINGS.md is republished with only the 46 VETTED learnings, each carrying the validator's corrected evidence
  and its validation sources.
- **The 9 REFUTED were reviewed one by one.** Seven were refuted only in part: correct figures, but a wrong root
  cause or an overstated claim. Four skill passages were corrected:
  - agent-fleet §12: a continuation may redo a half-applied step;
  - §6/§7: the "twice the cost" figure is labelled modelled, and related fixes are grouped;
  - review Step 6: a null result is valid;
  - a lander template's reason.
- About 100 UNPROVEN rules are labelled "Practice — not yet validated", naming the missing evidence.
- Vetted rules are marked validated.
- Rules that map to no candidate are listed for their own validation.

`learnings-ledger/encoded.jsonl` records the skill, section and version for the 42 vetted learnings whose fix lives
in a skill (63 location lines). Four vetted learnings are in no skill yet: C6 (re-probe a stale backlog), C9
(a registrar re-measures every lead), C25 (run the comprehensive battery in its own worktree) and C52 (assert on
conflict markers between staging and committing). They are proposed to the owner as v1.13.0.

