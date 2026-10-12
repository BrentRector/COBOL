## Entry 1868 — 2026-10-04 16:05 PDT — The lander fixes a confirmed review finding in the train instead of dropping the cluster

Train 1015's lander reviewed the diff, found one correctness defect in group Y (`OccursKeyItemFault`'s SR4 walk started at
`key.Parent`, so a key that is the table itself was charged with any OCCURS group that merely encloses the table, and a legal
table inside `OUTER OCCURS 2` was refused with COBOLNET2787), and dropped the whole cluster, four finished notes. Fixing it
afterwards took about ten tool calls (the skip is one line, plus the golden `85/pb1263_occurs_key_is_table_in_outer_table`),
because the finding, the code site and the repro were already written down; a fresh agent would have paid a full
orientation. The owner's direction (2026-10-04): fix such bugs right away while the context is in hand. MANDATORY-PRACTICES
L9 and `lander-train-brief.md` step 5b now say a confirmed correctness finding is FIXED by the lander in that cluster's
commit (repro to a regression golden, root cause once, whole-population gate re-run); the cluster is dropped only for a
redesign (more than one mechanism, or more than about 60 changed lines outside tests), when the gate cannot be made green,
or when the finding questions the cluster's premise. The earlier text said "never a fix-up by the lander". The same
refinements went to the public skills repo (landing review, the `cross-platform` pin-and-declare section, the rolling wave's
per-group model, an orchestrator-loop reference), labelled "Practice — not yet validated".

**Files:** `.claude/skills/workstream/templates/MANDATORY-PRACTICES.md`,
`.claude/skills/workstream/templates/lander-train-brief.md`. No diagnostic code used.

