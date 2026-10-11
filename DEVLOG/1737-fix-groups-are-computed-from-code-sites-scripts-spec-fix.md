## Entry 1737 — 2026-09-27 12:25 PDT — Fix groups are computed from code sites: scripts/spec/fix_clusters.py

Owner, 2026-09-27: "Can't we be better and more optimal at fixes? Can we group fixes so all fixes in one source file,
or one small set of related code, all get fixed in one pass?" The 2026-09-13 group-related-fixes rule already made
the fill unit a group, but the groups were assembled by hand, from a lead note plus keyword matches on its class
names. Waves 65 and 66 carried 1–3 notes per implementer, and wave 65 split `ConditionRenderer.cs`'s five open
defects across two implementers (A and E) that edited the same file separately.
`scripts/spec/fix_clusters.py` is a generated view of `kb/Work/`, never a list (rule 8). It resolves every open
actionable defect's named code sites to real `src/Cobol.Net.*` files: explicit paths, `Type.Member` references,
partial-class files and bare type names. It gives each note a primary file (the heaviest site, with ties broken
toward the more specific file), clusters notes by that file with a cap of 5, and absorbs singletons into a cluster
whose file they also name. Clusters rank by summed harm. First run: 411 defects → 125 clusters (sizes 5×42, 4×15,
3×10, 2×23, 1×35), with 30 notes that name no resolvable site. A six-slot wave therefore carries about 25–30 defects
instead of about 10. The workstream skill now says to fill each slot with the top-ranked cluster of a subsystem not
already in flight. Wave 66 was dispatched before the tool existed and runs as it is. From wave 67 on, dispatches
take clusters.

