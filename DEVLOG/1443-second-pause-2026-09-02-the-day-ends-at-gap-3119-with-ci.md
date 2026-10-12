## Entry 1443 — 2026-09-02 23:49 PDT — Second pause 2026-09-02: the day ends at GAP 3119 with CI green, twenty landings, every owner question but one decided, and the checkpoint file that reached main becomes a rule

Owner, evening: "Finish lander 3 and pb280 then pause. I need to reserve quota for other tasks" — and, when the runs
were read, "Do not leave ci cd broken." Both done. Lander 3 landed the A.4.2 screen witnesses (163 rows), the
citation-repair sweep (~296 sites; `audit_code_citations.py` at zero), cluster D and the A.4.14/A.4.3/A.4.10
witnesses (108 rows), folding the three "declined facility" seams three implementers had each built into ONE
(`EditionContext.Declined`, the `--permissive` policy moved onto the descriptor as `PermissiveInert`, the EC-name
refusal one table in `Advise`), and cluster D's raw band codes became catalogue descriptors — semgrep
`raw-diagnostic-code-literal` 475 → 439, PB175's first recorded decrease. The PB280 selector landed concurrently
(`a68d959f`): an A.1-OPTIONAL item whose §7 determination reads "Not provided." is DOCUMENTED-NON-SUPPORT by
predicate, and the anchor exemption is keyed on "has a §7 row" instead of on the verdict. Day total: GAP 3636 → 3119,
DOCUMENTED-NON-SUPPORT 36 → 340, closed 675 → 1192.

CI had been red on both conformance shards from the screen-witness landing (`60bf02d7`) through cluster D:
`ReservedWordPositionConformanceTests.DataItemNamedColumn_Rejected0901_EvenAt85` — B1's `{!screenPositionAhead()}?`
guarded the mandatory FIRST iteration of `displayStatement`'s operand loop, so an illegal `DISPLAY COLUMN.` died as a
raw COBOL0001 "failed predicate" instead of reaching the §8.9 funnel's COBOLNET0901. The comment beside it said
"nothing legal is lost", which was true and beside the point: illegal source must still reach its NAMED diagnostic.
Fixed in the B2 landing by requiring the first operand unconditionally and letting the predicate stop only the
continuation (the memory `left_edge_predicates` shape, met once more); run 33724112469 on `56878ebf` is green, first
attempt. A second, older red is named rather than dismissed: `Collation.CollationKeyCacheTests.ConcurrentCallers_
GetOneKeyPerText_AndNeverAWrongOne` failed on two of seven Linux unit runs today and passed the rest — a
schedule-dependent test in the A12 shape, to be registered and fixed on resume, never re-run into silence.

Process, kept current in the same change set: a lander's `STATUS.md` checkpoint reached main inside a landing commit,
and the next lander's rebase then had to choose between two agents' checkpoints for one path — the file is now
untracked and gitignored, and the `workstream` skill and its lander template exclude it from every landing `git add`.
Every owner question raised today is decided (PB280 Q1–Q3, PB371, the CLASS clause — `kb/Work/PB375`); only the
introducing edition of `USAGE POINTER TO type-name` (`A1`) remains, and it is not a bare question. Stopped for the
pause with their checkpoints on disk: lane 3 batch 2 (one subject adjudicated whole, three partial) and golden-lane
round 2 (61 rows in six files). Owed first on resume: battery #42, twenty landings past #41.

