## Entry 1871 — 2026-10-04 15:50 PDT — `prune_worktrees.py` no longer calls a branch that adds only scripts or docs LANDED

`classify()` counted a missing file as proof of unlanded work only under `src/` and `tests/conformance`. The orchestrator
prototype branch adds only `scripts/orchestrator/*`, `docs/rearchitecture/DESIGN-orchestrator-loop.md` and `kb/Work` edits, so the
dry run reported it LANDED and `--apply` would have archived and deleted unlanded work (the rule is "never remove UNLANDED").
Any file added by the branch and absent from `origin/main` now makes it UNLANDED (generated `TestResults/`, the submodule
`tools/` and the gitignored `STATUS.md` excluded); a branch whose files main later renamed shows as UNLANDED and needs a look,
the safe direction. Checked on the prototype branch: UNLANDED, with `scripts/orchestrator/alloc.py`, `autostart.ps1` and the design
doc named. Found by reading the dry run's classification of the new branch before running `--apply`.

**Files:** `scripts/prune_worktrees.py`. No diagnostic code used.

