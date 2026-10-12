## Entry 731 — 2026-07-09 21:10 PDT — PHASE-03 Step 14g: decision-complete plan recorded (recon wf_0d98d218-087)

With Step 14h closed, a 5-agent recon (`wf_0d98d218-087`, 688k tok) mapped Step 14g — the ~30 remaining bind-time
DATA/PICTURE/OO edition Checks — and produced a decision-complete plan, now folded into the PHASE-03 doc's STATUS block
(per [[feedback_plans_in_repo]]) + summarized in the resume-prompt banner.

**The load-bearing re-scoping finding:** "14g moves the DATA/PIC/OO gates to the BOUND-arm" is WRONG for ~18 of the ~30
Checks. The invariant is *zero binder/PicInfo/OoClassTable/OdoModel/emitter `Check` except the ONE UDF exception* —
satisfied by moving a gate into `VersionConformancePass`, NOT necessarily its bound-tree arm. Only ~12 are resolved-fact
fits for the new `DataItem`/`FileModel`/`CallUnit` enumerator (the 8 PicInfo USAGE gates + FILE SHARING/LOCK-MODE +
TYPEDEF + FUNCTION-PROTOTYPE); the other ~18 (BASED/TYPE/PROPERTY, class/interface defs, OCCURS-DYNAMIC,
SPECIAL-NAMES-FOR, PD RETURNING/RAISING, REPOSITORY, skeleton E/national-edited) MUST go to the parse-arm — a bound-arm
home would hit the DEVLOG-724 drop (flags cleared/nulled on declaration error), a TYPEDEF-clone over-count, or "no bound
carrier". This mirrors the 14h lesson: recognition-vs-resolved-fact decides the arm, not the construct's category.

Design highlights: `DataBinder.ConformanceForest()` = `Roots ∪ LinkageRoots ∪ TypeDecls.Values` (⚠ the latter two are
OFF `Roots` — the existing private `AllItems()` misses them); dedup EXCLUDES TYPE-clones + compiler temps (shared
`PicInfo` by reference) and never keys on `PicInfo` identity (`PointerItem` is a static singleton); USAGE keyed on
`(OwnUsage, Pic.Category, Pic.Usage)` with `OwnUsage` mandatory (group headers shed `Pic`); scope-aware where-strings
(main vs `RD '…' printable item`). Six sub-commits 14g.1–14g.6 (14g.1 also rewrites `LoudGuardTests` off its direct
`PicInfo.ParseUsage`/`Analyze` calls + adds exact-count witnesses — the conformance suite is contains-based and blind to
an over-count). No code change this commit — plan only.

