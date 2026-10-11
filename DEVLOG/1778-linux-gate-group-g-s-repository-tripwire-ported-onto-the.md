## Entry 1778 — 2026-09-29 10:33 PDT — Linux gate: group G's repository tripwire ported onto the clone design

Wave 72's group G (PB1719) had independently hardened the OLD, GIT_DIR-exporting `linux-gate.sh` in its worktree
(`d23d1fac7`). Its fix snapshots the real repository's `HEAD|core.worktree` before the legs, and the gate goes RED
("a test wrote to the REAL repository") if it changed afterwards. G also made the git-creating self-tests drop git's
`--local-env-vars`.

Main's `linux-gate.sh` was meanwhile rewritten to test a Linux clone and export nothing (Entry 1777), so the two
edits of one file would conflict when G's branch lands. The tripwire is still worth having as a second guard: the
clone makes a breach impossible by construction, and the tripwire makes any future one LOUD. It is now on main,
reading through the script's inline-configured `wgit`. So the landing of G's branch resolves `scripts/linux-gate.sh`
by taking main's version, which carries both mechanisms.

Checked with the characterization leg on main (HEAD `d27fc4871`): GREEN in 16 s, the tripwire quiet, and the
uncommitted-change count correct (1, this script).

