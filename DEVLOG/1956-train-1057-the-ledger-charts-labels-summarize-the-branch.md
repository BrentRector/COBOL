## Entry 1956 — 2026-10-11 01:31 PDT — Train 1057: the ledger charts' labels summarize, the branch survey sees a landed train's members, and the trend points through train 1056b

Train 1057 is the last train of the night stop (owner, 2026-10-10 21:35 PDT: "Fix PB2708 but land it after everything
currently in process"). It carries four process and evidence clusters in the order the land unit set, A to D, with D
(PB2708) last. No compiler source changes. Every implementer gated leg 1 only (batched gating, PB2515), so this train's
whole-population gate was each cluster's first population check.

**A — records-t1053 (PB2426, with records in PB1941, PB2614, PB2695, PB2708 and PB2885).** The land unit's render after
train 1053 appended that train's trend point, and it ran from its own worktree, so the main checkout stayed clean. The
cluster records four train-1053 members ABANDONED as superseded by train 1053: `w1053p-PB2885`,
`worktree-wf_8d59ef7b-0cb-1`, `worktree-wf_8d59ef7b-0cb-7` and `branch-records-r1716`. Each record cites a line census
against main. Two conflicts came in. `ledger-trend.json` took main's file in A's commit, because C carries the 211-point
superset (checked point by point below). `PB2885.md` already had a one-line ABANDONED record for `w1053p-PB2885`,
written by train 1055's lander; the merge keeps A's longer record, which names the head and the census, and drops the
duplicate. The lander then checked the four records against today's repository. None of the four branches exists now:
no local ref, no `origin/` ref and no worktree. PB2614's instruction to run `--abandon` later no longer applies. A
separate train commit adds one state line to each of the four notes.

**B — PB2942: the ledger charts' accessible labels summarize the series.** `gen_ledger.py#trend_svg` and
`#register_svg` wrote every trend point into the chart's `aria-label`: 3,458 and 6,645 characters at 191 points, one
entry longer per landing. The owner said on 2026-10-10: "we do not need detail for each dot". The dots and the polyline
are unchanged. Each label is now a fixed-size summary: the count, the first and last points, and the trend's low and
high. The rendered page shrank from 72,435 to 62,255 bytes. A `--self-test` check pins the label length as independent
of the point count (3 points against 400), and it fails against the old generator. B also extends PB2708 with the
finisher-superseded SPLIT predecessor case and PB2915 with the owner's UTF-8 console direction. Its PB2708 hunk merged
cleanly with A's.

**C — records-t1055 (PB2426): the trend points through train 1056b.** These are the points `gen_ledger.py` back-filled
(`missed_points`) after trains 1055 and 1056b. The file has 211 points and 8 program points. The lander checked it as a
point-by-point superset of A's file (194), main's file at `dc2bca9a7` (200) and its own first commit (208), with no
duplicates. It applied cleanly on A's commit, because A's commit had kept main's file.

**D — PB2708 + PB2987: the branch survey sees a landed train's members, cherry-picks, and the train landing now.**
`scripts/prune_worktrees.py` read only a branch's own merge against main and its report. So a member that a lander had
edited in its train read CHECK, WAITING TO LAND or DECISION after the train landed, and `--train` picked up the members
of a train that was still landing. `classify` now also reads a branch as LANDED in two cases (`carried`). In the first,
a landed train's manifest names its head and that cluster's `Train-Cluster:` trailer commit lies in the train's own
range: walking back along the first parent from the train's last `Train <label>:` commit, the walk stops at the
member's base or at another train's commit, because trains reuse letters. In the second, every one of its commits is
patch-equivalent to one on main (`git cherry`) and it declares no name main lacks. Declared names now come from the net
diff. The members of the train whose lander holds the landing lease read IN FLIGHT. `--apply` deletes
`origin/<branch>` too, unless the remote holds commits the local branch did not. There are eight new self-test plants,
and each fix was proven by mutation. D's branch was built on `land-train-cap-r2100`, which train 1056b already landed,
so its manifest base was `7bafdc673` and only D's three commits came in. The `PB2708.md` conflict was two independent
dated sections, A's train-1053 recurrence and D's landing record. The merge keeps both. PB2708 stays `half`: the
supervisor's unit-start cleanup remains. PB2987 is filed open: the owed-ledger check reads a stale checkout. The
lander's review found one doc out of date. MANDATORY-PRACTICES O10, the lifecycle's SSOT, did not yet name the carried
LANDED path, the IN FLIGHT train-lease members or the `origin/` deletion. The train's last commit adds all three.

**The train.** `train_apply.py` brought the four in and stopped twice, at A and at D, on the conflicts above. Its
code-citation, doc-citation and work-register audits all passed. Origin/main had not moved since the base
`dc2bca9a7`, so the final rebase under the landing lease was a no-op. Windows lander gate run 20261011T082237Z-a5a266:
`GREEN — Conformance 11,460/11,460 · Unit 33,360/33,360 · Characterization 36/36`, with every population EXACT, the
first run. SELF-TESTS: GREEN, 73 ran. The Linux gate on the same head (`c95097857`): `GREEN (legs selftests unit
characterization conformance guard selftests-built)`. Semgrep: no count moved. Arch-oracle: IDENTICAL across 7,761
cases against baseline `7c00d5d5d729`. `landing_oracle.py`: PASS, no input changed, so no re-record. The review
covered `gen_ledger.py` and `prune_worktrees.py`. It found no correctness defect. It fixed the O10 doc above. No
inventory verdicts, so the GAP holds at 103. No diagnostic codes. No cluster was dropped or ejected.

