## Entry 1908 — 2026-10-07 02:55 PDT — The legacy engine is gone from main: Cut 2 landed (train 1027), the archive tag is on 48a44548c, four worktrees pruned by classification, wave 1028 dispatched (PB2111, PB2112, PB2113 → PB2114)

**Train 1027 (Entry 1907, `25849fba7`)** landed PB2110: the five `CobolSharp.*` trees and every reference to them are gone;
PB1861 retired as moot; PB2113 and PB2114 unblocked; PB2111 gained the code-comment sweep (item 7); PB1953 recorded its third
recurrence. The deletion commit itself (`36441a24c`) was the attended session's, after the classifier refused a subagent's
`git rm -r` (Entry 1905); the finisher merged it. The lander's review found and fixed one more legacy artifact the grep had
missed, `Run-Cobol85Audit.ps1` at the repository root. Wave 1027 cost 407,936 subagent tokens.

**The archive tag.** `legacy-byte-engine-final` is an annotated tag on `48a44548c`, the last commit of main that holds the
engine (verified: `git ls-tree 48a44548c src/` lists the three `CobolSharp.*` projects), pushed to origin with the WSL
reproduction recipe in its message; `docs/rearchitecture/LEGACY-ARCHIVE.md` is the pointer on main. 46,000 lines of frozen
oracle are now history, reachable from one tag.

**Worktrees, by classification (owner 2026-10-07: removed only after all their work is committed and landed).**
`prune_worktrees.py --apply` archived five unmerged tips in `E:\Temp\pruned-branches-20261007-024805.bundle` (verified) and
removed four MERGED or LANDED worktrees; it skipped two dirty ones (`battery88`, `worktree-agent-a42657b7f46d31559` — an
uncommitted edit is never removed), six recently touched ones (trains 1025 and 1027's, pruned at the next pass) and every
UNLANDED one (wave 1026's five, in flight; `wf_b3526bdd-e0d-4`, PB1470's pending finisher). Three CHECK rows stay until read
against their notes. Entry 1906's stamp was written ahead of the host clock and is corrected to 02:20 PDT.

**Wave 1028, by hand, beside wave 1026** (meter at dispatch, recorded: session 12 %, weekly 61 %, Fable 31 %; the cap is 85 %).
A PB2111 (Sonnet: the prose, register, skill and code-comment sweep; grammar edits comment-only), B PB2112 (Sonnet:
`CobolSharp.sln` → `Cobol.Net.sln` with every caller), C PB2113 (Opus: D10, the SUBSCRIPT lexer mode and the hand-rolled
re-parsers replaced by interpreted grammar rules per `DESIGN-frontend-grammar.md` §9, the §9.4 space-separator decision
resolved from the spec first), D PB2114 (Opus, successor of C: the grammar alternatives the frozen oracle forced become one
rule each; the additive-only caution retires). Concurrency 4 beside wave 1026's last implementer and lander, inside the
six-implementer budget; one train of four (`min_final_train` 2). Rendered by `make_dispatch_specs.py`, `check_practices.py`
GREEN, stall watchdog beside it. Leads take ids PB2143–PB2150.

