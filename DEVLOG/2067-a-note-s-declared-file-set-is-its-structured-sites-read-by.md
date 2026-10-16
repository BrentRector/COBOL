## Entry 2067 — 2026-10-16 06:04 PDT — A note's declared file set is its structured sites, read by one reader on one index policy (PB3242, PB3203; PB2423 and PB2678 with them)

### The finding

The file-set partition (DESIGN-architecture-review §8.7) decides whether a wave may run beside the work already in
flight, and it decided on every path a note's TEXT named. Three scans did it. `plan_wave.note_file_set` ran the public
base's scored clustering heuristic, `fix_clusters.sites`, over the whole note (every path, bare file name,
`Type.Member` and CamelCase type in it), `member_index.py` ran its `SITE_PATH` over the whole note for its folder
expansion and its fallback, and the register's own `work.note_sites`, the one reader shaped right (it read only the
**Sites** block), accepted only `src/`, `tests/` and `docs/`, so a `scripts/` site declared nothing. Fifty-seven of
PB1754's sixty-three ready R2 notes DISCUSS `plan_wave.py` and `landing_check.py`, and every PB3160 slice says
"cite.py --check", so the two campaigns' dry runs planned one campaign group each: the rest waited on one another's
prose. Re-probed on the base (173dc4f35): `member_index.py --note PB3203` gave 57 C# files and neither of its two
`scripts/` sites, because its Python names, unknown to the C# index, were still widened by drift and `main` matched
as a word in every drifted file.

### The fix

Finding B's decision, as the Mythos session revised it on 2026-10-16: a note's declared set is its STRUCTURED sites
only. `work.note_sites` is now the one reader. It reads every site block (a bold `Code site` / `Site` / `Sites` label's
paragraph and the list under it, or a heading's section), keeps a path only under a campaign root of `model_rules.json`
(read once, `coord.CampaignDomain`), drops `:line`, `#member` and `(member)` suffixes, refuses placeholders and globs,
and returns the names of the `**Moves or changes:**` and `r2_members:` lines and the site blocks' documentation ids.
`member_index.note_file_set` reads through it and returns the whole declared set: the site files, the code files under
a site folder, and the index's callers of the names. It reads the index only for a note that names a member or
restructures, so a fix-lane note declares its sites and never every caller of every type declared in them, and it
widens only known names by drift. PB3203 folds in: one function, `plan_wave.declared_sets(repo, IndexPolicy)`, computes
the set on one explicit index policy (THE index of the tree), and the planner, the spec renderer and the landing check
all pass it. The landing check's copy had read the newest committed index, which cannot see what a landing added, and
regenerating costs 110 s on a cold worktree. Every group's collision set is its notes' declared sets plus its primary
file, the fix lane's too: fix_clusters' prose sites still rank and group the fix lane, and never enter a declared set.
`member_callers`, `plan_wave.note_file_set` and `landing_check.partitioned` are deleted. The same change closes PB2423
(a census member id's parameter type was a site) and PB2678 (an ignored benchmark report and the contract's
`compare_oracle.py` were sites of every R3 note).

### The measurement

After the fix, the PB1754 dry run plans 8 groups (6 campaign clusters and 2 finishers). Each of its 78 waits is a file
that real in-flight work changes: the external-repository slices, or in-flight and undecided worktrees on
`RuntimeApi.cs`, `BinderDriver.cs` and `ProgramEmitter.cs`. The PB3160 run plans its whole seven-slice chain with no
collision. Those slices carry no site block, so they now declare nothing, and their author should add one. PB2296
step 1 declares 69 files and still holds all six `DataBinder.Options` users.

### The tests

`test_plan_wave.py` section 10 adds four drift tests: a `Gate:` line declares nothing; every site shape declares its
file; the planner, the member index and the register return the same sites; and no module outside the reader carries a
campaign-root path regex or calls fix_clusters' `sites`. Each was seen failing on a planted whole-text reader and a
planted scanner module (11 checks red). `member_index.py --self-test` gains the fix-lane, unknown-name drift and folder
checks. DESIGN-architecture-review §8.7 and DESIGN-orchestrator-loop §9.1 now define the declared set as the
structured sites.

Branch review round 1 found two holes, both fixed at the reader. R1-1: the `**The files this step edits**` line that
`scripts/arch/file_census_notes.py` writes into a multi-step R3 note was not a site block, so PB2300's
`IntrinsicCatalog.cs` (a step file outside its Sites that no caller recovers) left its declared set and the landing
check would have stopped the wave. The label is now `work.STEP_FILES_LABEL`, a site label the reader reads and the
writer renders through `step_files_line`; a sweep of every bold-labelled line in open notes found no other generated
line that lists edited files. R1-2: `member_index.needs_index` read any wave kind and any `r2_members:` as a
restructuring note, so the 47 open R2 defects routed to the fix lane made every fix-lane plan read and regenerate the
member index (110 s per landing). `work.FIX_LANE_WAVE` is now the one spelling of that wave kind (the R2 collector and
the note writer use it), and a note of that kind declares its sites only. New checks in `test_plan_wave.py` and the
member-index self-test were each seen failing on a planted revert.

