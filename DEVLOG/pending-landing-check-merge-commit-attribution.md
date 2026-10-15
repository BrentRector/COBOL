## PB3180: the landing check charges a merge commit only with what it introduced

`landing_check.py` attributes R3 work per commit (PB2703), and it read each commit as `sha^..sha`. For a merge commit
`sha^` is the first parent, so a review finisher's `Merge <predecessor> for the review finisher` (first parent main)
was charged with the predecessor's whole diff, and a `Merge origin/main into <branch>` with main's own landings. One
commit's R3 status flip was smeared across other commits' files: PB3154's branch (worktree-wf_15ec6338-3a0-4) stopped
on `R3 work (PB2160) changed 5 file(s) outside its declared file set`, although the only commit working on PB2160
(18246d045) changes kb/Work and DEVLOG only. The files came from merge dacb04b6c. The stop held PB3154 out of the merge
queue, and PB3153 and PB3164 behind it.

The fix is one attribution function, `commit_work`: a non-merge commit is its diff against its parent; a merge commit
is what differs from every parent (the intersection of the per-parent file lists and status flips: conflict
resolutions and evil-merge edits) plus the ids leading its own subject. Four self-test cases pin it (a merged
predecessor, an R3 merge's resolution still stopping and a take-theirs resolution not, a `Merge origin/main`, a root
commit); three of them failed on the old code. The repro now reads `PASS (0 stop(s), 0 warning(s))`: its nine
warnings were the same smear. The sibling sweep found no other per-commit first-parent diff that is wrong
(`prune_worktrees.mechanical` already reads both parents). The module docstring and DESIGN-architecture-review §8.7
say how a merge is attributed. Found on the way and reported, not fixed: the landing arm's per-commit rule
under-attributes an R3 branch whose code commits are `WIP checkpoint:` and whose status flip is a separate commit,
while the in-flight arm classifies a branch whole.
