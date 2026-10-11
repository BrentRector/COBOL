## Entry 1941 — 2026-10-08 15:41 PDT — Train 1043: wave 1043's A, C, E, H, G, D and the notes branches Z, Y, X — thirteen census Delete subjects, the softcap self-test flake, R2 batch 2 filed; B and F dropped at the landing check (PB2188, PB2191, PB2192, PB2196, PB2201, PB2167, PB2199, PB2208, PB2207, PB2174, PB2780 landed; PB2707, PB2708, PB2719–PB2779, PB2781–PB2783 filed)

**Tooling — the softcap self-test flake, landed once.** `test_orchestrate_stop.ps1` went red in the implementer gates of
C, D, E, F, G and B ("soft cap: a fleet launch after STOP-UNIT is refused by the dispatch guard: got '0', want '2'").
`testdata/fake-claude.ps1`'s `softcap` mode still waited only 60 s for the supervisor's STOP-UNIT; train 1037t had
raised the same hang guard to 600 s in `bigcontext` and `stopsme` and missed this third copy. Under several gates the
supervisor took about 100 s, so the fake ran the dispatch guard before STOP-UNIT existed. Four branches fixed it
independently; the train keeps C's version (the three waits become one `WaitStopUnit`) as its own tooling commit and
drops the copies on E, F and G, because `landing_check.py` attributes R3 work per commit (PB2703) and the file is
outside every cluster's declared set. Why the supervisor was 100 s late is PB2782.

**A — PB2188, PB2191, PB2192, PB2196, PB2201 (Delete program PB2119).** Fourteen unreferenced Frontend members deleted:
`SourceLocation`'s `==`/`!=`, `CstExtensions.AsInt`/`TryAsInt`, `DataDescriptionCst.Context`/`Span`,
`DataDescriptionClauseCst.FirstValueText` and its implicit conversion, the four `Frontend` forwarders of
`Frontend.Directives` (`TurnEvents`, `RefModZeroLengthEvents`, `FlagEvents`, `DirectiveSites`), and
`PhysicalLineSet.Count`/`Range` (its indexer is test-reachable and stays). Three docs name `Frontend.Directives` now.
Its three leads lay outside its declared set and became **PB2780**, fixed in the train as one fix-lane commit: the dead
`SourceSpan.cs` (no reference since PB2192 deleted `DataDescriptionCst.Span`), a `CompilerDriver` comment naming the
deleted `frontend.TurnEvents`, and the `Frontend.CobolWordsMap` forwarder, whose one reader (`FormatWordDriftTests`)
now reads `fe.Directives.CobolWordsMap`. `DESIGN-cobol-words-directive.md` §8 described the pre-PB65 threading and now
describes the current one.

**C — PB2167.** The caller-free `StatementBinder.DataRefs` (its only reference was its own recursion) is deleted, with
an orphaned STOP RUN `<summary>` in `StatementBinder.cs` that had attached itself to `BindRetry` (the edition gate is
`VersionConformancePass`'s).

**E — PB2199.** `ZeroTokenRewriter.ArithmeticOperators` was dead from the commit that introduced it (`00f6421c3`, where
the two adjacency sets were written as copies of it). Deleted; the class summary, which still said the pass rewrites to
`INTEGERLIT("0")`, now names `ZERO_ARITH`, all three figurative-zero spellings and the ref-mod COLON arm. Its lead
(no build rule stops a new unread private member: IDE0051/IDE0052 are off) is **PB2781**.

**H — PB2208.** `CldrLocaleLoader.TryLoad` and `CldrLocaleNotFoundException.LocaleName` deleted, with the exception's
constructor parameter that only fed `LocaleName`; the exception stays as `Load`'s documented failure.

**G — PB2207.** `CobolModule.PushMain` deleted (the run-unit main is pushed by `ProgramTable.RunMain` straight onto
`ModuleStack.PushMain`). Its lead is appended to **PB2181**: once `RuntimeApi.ModulePushMethod` and `ModulePop` go,
`CobolModule.Push` and `Pop` have no reader and go in the same change.

**D — PB2174.** `ProcedureTableBuilder.HandlerBasePc` deleted; the F3 handler base has one owner,
`BoundProgram.F3HandlerBasePc`, and `AddF3Handler`'s summary and the PERFORM Format 3 design §9.1/§9.10 name it. D's
implementer stopped at STOP-loop with its work committed and only the softcap flake red; this train's whole-population
gate is its gate.

**C's and D's comment fixes, as fix lane.** Three comment-only fixes the C and D implementers made (I9) lay outside their
waves' declared sets: `StatementBinder.cs`'s two Format-3 comments (one named the deleted `HandlerBasePc`, one cited a
stale `Bind():163` and an "empty until M4" claim M4 made false), `ExpressionBinder.cs`'s remarks naming `DataRefs`, and
`CobolControlFlow.g4`'s stopStatement comment placing the STOP RUN status-phrase gate at bind time. They land as their
own fix-lane commit.

**Z, Y, X — notes only.** Z files PB2708 (`prune_worktrees.py` never reads train manifests, so a landed train's member
reads WAITING TO LAND) and records ten superseded branches ABANDONED in their notes (PB2169, PB2688, PB1636, PB2230,
PB2193, PB2605, PB1722, PB2617, PB2639, PB2708), appends PB2701 (the six wide `Workflow:` ledger entries were the R2
review fleet), and adds train 1042b's ledger trend point. Y files PB2707 (an R2 relaunch transcribed ~95 KB of per-pair
state inline; R2 batches are not sized against the session window and the week). X files R2 batch 2 (pin 8be230068,
65 pairs: PB2719–PB2766 and the invalid-record triage PB2770–PB2777, MAJOR PB2729, PB2734, PB2748, PB2757, PB2773),
records the owner's answers to PB468 Q5–Q8 (PB2767–PB2769), corrects the stale owner statuses of PB1099, PB935 and
PB2557, and files PB2778 and PB2779 from the 14:32 wind-down.

**The train, and the two clusters it dropped.** The first head carried all eleven members (MANDATORY-PRACTICES O11):
every patch applied without a conflict, and every gate was GREEN on the first run (build-local lander Conformance
11,269/11,269 · Unit 32,929/32,929 · Characterization 36/36, run 20261008T220919Z-78f5f8; Linux gate; semgrep; oracle
DIFFERENT only by B's added golden). It stopped at `landing_check.py`: R3 commits of C, D, F and B changed 17 files
outside their groups' recorded declared sets (PB2679's mechanism; the per-cluster lists are in that note), and B's
files (`DataBinder.cs`, `NumericRenderer.cs`, `RuntimeApi.cs`, `CobolNum.cs`, `CobolEdit.Float.cs`) also collided with
earlier dispatches: two never-started wave-1039 ledger entries and four superseded branches Z records ABANDONED. C's and
D's out-of-set edits were comments and moved to the fix-lane commit above. **B (PB2163)** and **F (PB2200)** are DROPPED
with their branches intact: B is a Unify whose helper (`NumericRenderer.EditedLanding`) lives outside its declared set,
and it carries a real compile fix (a 16-byte unsigned COMP-5 item stored into any numeric-edited receiver failed CS1503
on accepted source; golden `2023/pb2163_unsigned_wide_into_edited`), so it should re-land on a hand re-plan rather than
wait for PB2679's planner fix; F deleted `CopyProcessor`'s constructor parameter, whose callers (`Frontend.cs` and eight
unit tests) its set missed. PB2163 and PB2200 stay open. The re-check then stopped on X's own clerk dispatch, a wide
`hand` ledger entry (400 declared files, PB2701's shape) that bound F's worktree to wave-1039 notes; it was released
(`plan_wave.py --release PB906`), and the check PASSED with one warning (F's worktree must rebase onto this). Rebuilt
train: build-local lander GREEN — Conformance 11,268/11,268 · Unit 32,927/32,927 · Characterization 36/36 (run
20261008T223238Z-bf055e); Linux gate GREEN; semgrep verify PASS (no count changed); oracle IDENTICAL over 7,641 cases
(baseline 7ca7cc22f046 kept). No inventory row moved and no diagnostic code was claimed. Review: one finding (PB2163's
`closes_rows_reason` called the cluster purely behavior-neutral), moot once B was dropped; recorded in PB2679 for B's
re-land.

