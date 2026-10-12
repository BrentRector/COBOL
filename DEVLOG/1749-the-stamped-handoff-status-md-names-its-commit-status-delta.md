## Entry 1749 — 2026-09-28 10:08 PDT — The stamped handoff: STATUS.md names its commit; status_delta.py reads only the rest (PB1698)

An outside reviewer on the owner's LinkedIn thread asked whether "read the commits, not the summary", the fallback
adopted after wave 68's stale checkpoints, gives back the orientation savings the summaries were meant to buy. It
does: the fallback re-reads the whole branch on every resume, stale or not. The owner, ~10:05: "Add it to the wave and
evaluate its effectiveness. I will wait to post the response until we have data". Then: "We can exceed today's quota
and use tomorrow's as well in order to test features relevant to the LinkedIn thread".

**Mechanism.**
- STATUS.md's first line is now `STATUS-AT: <sha>`, the commit it describes, written after the checkpoint commit
  (STATUS.md is gitignored, so HEAD does not move).
- `scripts/spec/status_delta.py` classifies the summary against the branch and prints the reading list:
  - CURRENT: the summary covers every commit;
  - STALE by N: only the N later commits;
  - UNSTAMPED or DIVERGED: every commit since the base.
- It is wired into the fix-lane implementer brief, the dispatch-spec template, MANDATORY-PRACTICES P4, the rolling
  wave's same-file successor text and `check_practices.py` (GREEN).

**Verified on the real case.** Wave-68 U's branch has a STATUS.md describing 59f19d6e8 and a later preservation commit,
aaf1823fc. Stamped, the tool reports STALE by 1 and lists that commit's 4 files. Unstamped, it lists all 6 commits since
the base.

**Evaluation is open in PB1698.** It has two parts:
- a controlled A/B: fresh read-only agents resume that branch with and without the stamp; measured on turns, tokens and
  whether they find the uncovered work;
- production data: waves 70+ resumers and successors against the wave 68/69 baseline.

Running wave-70 agents read their brief before this change. U2 is the first successor that starts after it, but its
predecessor U writes an unstamped STATUS.md, so U2 will see UNSTAMPED.

