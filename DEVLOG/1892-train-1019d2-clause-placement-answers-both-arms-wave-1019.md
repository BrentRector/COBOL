## Entry 1892 — 2026-10-05 11:02 PDT — Train 1019d2: clause placement answers both arms (wave 1019 group D2: PB516, PB486, PB1650, PB1744 landed; PB514 retired; PB872 landed by the same change; PB2051 filed)

A one-cluster train under the 1-2 cluster exception: group D2 was the only finished work. A first lander for this
train returned SPLIT at its first step, before doing any work, because the coordination STOP file was present. This
lander resumed in the same worktree, so its STATUS.md was the starting point. The cluster's diff base was its
predecessor group D's head `1b0648c23`, not the branch's original base. Group D's notes (PB1301, PB1302, PB1476)
were already on main through train 1020. The branch's own DEVLOG entry (numbered 1886) was dropped, and this entry
replaces it.

**Group D2: PB516, PB486, PB1650, PB1744 (PB514 discharged).** All five notes were re-probed first. Every PB514 repro
(ANY LENGTH with GLOBAL or PROPERTY, SAME AS with GROUP-USAGE or PROPERTY, DYNAMIC LENGTH under a CONSTANT RECORD)
was already rejected, because kb/Work PB487 replaced the hand-kept chains with one `DataClauseKind` permitted set per
rule. PB514 is therefore retired. Two parts of PB486 were also gone: REDEFINES's position (PB1280) and a clause
written twice (PB917). What remained were four rules that each had a home but were asked by only one of their two
arms. **BASED's residence (§13.16.3 SR16).** This rule has two sentences: BASED is allowed only in the linkage,
working-storage and local-storage sections, and only at level 1 or 77. A private arm of the BASED block asked the
level sentence, and nothing asked the section sentence, so `FD F. 01 R BASED PIC X(10).` compiled. SR16 is now a
Residence row of the clause-placement table, with a new `ResidenceLevels` field because SR16 is the only residence
sentence that admits 77. `ClausePlacementRule.ResidenceFault` is the one residence test. A written BASED reaches it
through `ScreenClausePlacement` (COBOLNET2404), and a TYPE- or SAME AS-composed BASED reaches it through
`ScreenComposedBased` (COBOLNET2510). **TYPEDEF's position (§13.16.3 SR4).** SR4 says "the TYPEDEF clause shall
immediately follow the entry-name clause", but only the REDEFINES half of SR4 was asked, so `01 T PIC X(4) IS
TYPEDEF.` compiled. The REDEFINES ordinal screen became `ScreenLeadingClausePosition`, a closed two-row table (SR4
ends "The remaining clauses may be written in any order"). Nine corpus programs, one constructs.json snippet and
three drift-test samples had written TYPEDEF after another clause, so they were reordered. **PROPERTY over a
composed BASED (§13.16.3 SR21 a)).** `ScreenComposedBased` now asks SR21 a) too, so `01 P TYPE T PROPERTY.` over a
BASED T is refused. **Parameter-names in their REPOSITORY's scope (§12.3.8.3 SR12/SR13).** No binder binds a
parameterized definition, so a formal's own spelling never reached `DeclareUserWord`, and `CLASS-ID. C USING SQRT.`
under `FUNCTION ALL INTRINSIC` compiled. `DataBinder.DeclareParameterNames` now declares the formals through that
funnel for both the class arm and the interface arm, and `AddRepositoryIntrinsics` is the one writer of the
intrinsic membership. **Found on the way (I9).** `DeclaredForest` walked `Roots` and then `LinkageRoots`, but every
LINKAGE 01/77 is already on `Roots`, so every per-item pass saw each linkage item twice. The CLI's diagnostic dedup
hid this. The spine now walks `Roots` alone. This is exactly kb/Work PB872 (open since 2026-09-13), which the
implementer had not connected. The lander found it in review and flipped it to landed. It is pinned by
`Based_InEveryAdmittedResidence_BindsBased(LINKAGE)`, which counts 2 based items where the double walk gave 4. The
TYPEDEF×REDEFINES message now cites §13.16.3 SR3 instead of a bare "§13.16". Goldens: four 2002 positives
(pb516_based_residence, pb486_leading_clauses, pb1650_property_typed, pb1744_parameter_name_all_intrinsic) and seven
negatives (2404, 2510, 2404, 2423, 2510, 1649, 1649). based-level-05.err was re-quoted for the code that moved from
the BASED block to the table row. Rows: SR-13.16.3-16, SR-13.16.3-4 and FMT-13.18.52.2 went PARTIAL → CONFORMS. SR-13.18.44.3-1,
SR-13.16.3-21 and SR-12.3.8.3-12/-13 were restated, and two witnesses were retired. No diagnostic codes were claimed
(COBOLNET2885-2887 were returned).

**The train.** The patch `1b0648c23..worktree-wf_f7e4c8a0-d77-8` was applied with the inventory and DEVLOG hunks
excluded. The two corpus manifests had conflicts, each over whole list elements, so both sides were kept and the
counts checked: 2002 went 721 + 4 = 725 and negative went 2401 + 7 = 2408. Verdict batch w1019d2 was re-applied
with `record_verdicts.py` on the merged tree, and **GAP went 197 → 194** (the implementer's 249 → 246 was measured
on its older base). The lander gate was GREEN: Conformance 10,646/10,646 · Unit 31,612/31,612 · Characterization
35/35 (run 20261005T175439Z-0f238c). The legacy Integration assembly passed 503/504 with one skip. semgrep verify
PASSED with no count increased. The review of `origin/main...HEAD` covered correctness, drift rules and three
citations (SR16, SR4 and SR21 rechecked with `cite.py --check`) and made two findings, both fixed in the cluster's
commit. First, two stale "Roots + LinkageRoots" descriptions of the forest remained (in the
`DataBinder.UsageDeclaration.cs` summary and in `docs/COBOLNET_DATA_MODEL_DESIGN.md`). Second, PB872 was still open
for the defect this cluster fixed. The implementer's lead 1 is now **kb/Work PB2051**: a parameterized class or
interface body is never bound, so an unexpanded skeleton holding `05 B BASED PIC X` compiles. That was re-probed on
the train build (exit 0), and the note is clustered with PB1784. Lead 2 is an expectation for the next battery: the
GnuCOBOL differential's syn_definition.at programs write `PIC X(50) IS TYPEDEF`, which §13.16.3 SR4 now rejects,
so expect flips there and adjudicate them against SR4. In the train's last commit, the lander templates
(`lander-train-brief.md`, `lander-brief.md`) also lose their stale hard-coded `Co-Authored-By: Claude Fable 5.1` and
`Claude-Session:` trailer lines in favor of the session's own attribution line. `check_practices.py` is GREEN.

