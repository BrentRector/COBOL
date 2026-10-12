## Entry 1942 — 2026-10-08 16:52 PDT — Train 1043b: wave 1043's B and F re-landed, the wave's branch decisions, the planner holds dispatched notes (PB2200, PB2163, PB2806 landed; PB2795, PB2807 filed; PB2188, PB2199, PB2207 branches ABANDONED)

**F — PB2200 (Delete program PB2119, census R0-0164).** `CopyProcessor.AddSearchPath` had no caller; `git grep` finds
only its declaration. The sibling sweep found the standalone `CopyProcessor.Process(string, string)` caller-free as
well: census R0-0088, a "test-only-family" finding whose only caller was the deleted legacy `FixedFormTests`. Both are
deleted, with what only they kept alive: `ExpandCopyStatements`, the string `ApplyReplaceStatements` and
`ExpandCopiesOneLevel` overloads, the constructor's `sourceName` parameter, and the mutable search-path list (now an
array fixed at construction). COPY expansion has one entry, the mapped `ExpandCopiesOneLevel` that
`ConditionalCompilationProcessor.Manipulate` drives. `Frontend.cs` and ten unit-test constructor calls changed in the
same commit, with no forwarder. `DESIGN-cc-in-copy.md` §2 item 3 and §4 now describe the one wiring that exists. The
change is behavior-neutral: the oracle is IDENTICAL for it. Train 1043 dropped F at the landing check because its
branch carried a duplicate copy of the fake-claude softcap fix outside its declared set. The branch reverted that copy
(1debd71b8), so main's `scripts/orchestrator/testdata/fake-claude.ps1` stands unchanged (verified: the train's diff
does not touch it).

**B — PB2163.** `ArithmeticEmitter.StoreArith` spelled the edited receiver's carrier switch twice, in the LOCALE arm
and the masked arm. The two copies differed only in scale, and both scales are `PicInfo.ReceiverScale`. It is now
`NumericRenderer.EditedLanding`, the edited sibling of `StoreArgs`, a switch over `NumX.Carrier` that throws on a
carrier it does not map. Both arithmetic arms and the MOVE numeric-to-edited arm call it. Two more respellings of the
receiver-scale rule (`NumericRenderer`'s de-edit arm, `DataBinder.StoredShapeOf`) now ask `PicInfo.ReceiverScale`, and
the dead `RuntimeApi.MaskScale` is deleted. The sweep showed the switch was not total. A 16-byte unsigned COMP-5 item
(the `UInt128` carrier) stored into any numeric-edited receiver (masked, LOCALE or floating-point; COMPUTE with or
without ON SIZE ERROR, or MOVE) failed with CS1503 on accepted source. The unsigned lane now lands through
`CobolNum.RescaleCheckedU`/`RescaleStoreCapU`. The floating-point edited form lands through
`CobolEdit.FormatFloatStoreU`/`TryFormatFloatU`, which fold a 39-digit value round-to-odd into the `Int128` lane, so no
new BigInteger appears (semgrep 44 → 44). The store carries the full container range (DOC-A.1-179), and a no-phrase
store or a MOVE keeps the low-order digits (DOC-A.1-70), per §14.7.7 1), §14.9.25.4 6), §14.7.5 3) and §14.7.4.3 7)
(each re-checked with `cite.py --check`). Golden `2023/pb2163_unsigned_wide_into_edited` has 19 hand-derived lines;
for example, 2^128 − 1 MOVEd to `Z(28)9.99` shows its low 29 integer digits, and the same value under ON SIZE ERROR
leaves the receiver unchanged. Two unit tests pin the carrier totality and the kernels. The commit also carries the
train-1043 lander's reviewed `closes_rows_reason`.

**Y — PB2679, PB2188, PB2199, PB2207, PB2708, PB2795 (kb/Work only).** Train 1043 landed wave 1043 A, E and G. Their
branches (`worktree-wf_2ca5e648-fef-1`, `-fef-5`, `-fef-7`) held nothing else main lacks (a line census), only the
duplicate softcap copies that train dropped, so each lead note records its branch ABANDONED (superseded). PB2679
records the hand re-plan that brought B and F into this train. PB2708 drops the three WAITING TO LAND rows it misread.
PB2795 is filed: the dispatch ledger kept a returned group's declared set in flight while any of its notes was open,
and nine stale entries were released. `ledger-trend.json` gains the program-series point from train 1043's ledger
render. After this landing the land unit runs `prune_worktrees.py --apply --abandon` on the three branches.

**X — PB2806, PB2807 (orchestrator tooling).** The fix-lane planner read no dispatch ledger, so wave 1044 re-planned
seven of the eight groups of the Mythos session's recorded, unlaunched wave 1045. `plan_wave.py` now has
`held_by_dispatch()`: an open note named by a dispatch-ledger group, with no report written since that dispatch, waits
with the holding dispatch named, in both lanes. `next_wave()` also counts the ledger's wave numbers, so a planned but
unreported wave's number is never reused. `test_plan_wave.py` reports 119/119 checks OK on the train head, and
`DESIGN-orchestrator-loop.md` §9 is updated. PB2807 (file-level overlap with another session's dispatch) is filed.
X had no gate of its own; this train's gates are its gate.

**The train.** Four clusters, brought in from their branches without a conflict. Origin/main was still the base
(685b44315) when the lease was taken, so no rebase was needed. The whole-population gate was GREEN on the first run:
Conformance 11,269/11,269 · Unit 32,929/32,929 · Characterization 36/36 (run 20261008T233820Z-660f56), and its
self-tests (67, including the softcap case) were GREEN. B and F had gated LEG 1 ONLY under the batched-gating trial,
so this was their first whole-population run, and nothing outside their own tests broke. The arch oracle was DIFFERENT
by exactly one case, `ADDED corpus/2023/pb2163_unsigned_wide_into_edited` (cluster B's new golden). No emitted C# or
diagnostic of an existing program changed, which confirms F's deletion and B's unification are neutral and B's fix
reaches only source that used to fail to compile. The baseline was re-recorded (7ca7cc22f046 → c6339810570b).
`semgrep verify` PASS before and after (no count moved). `landing_check.py` PASS with one warning: Mythos's unlaunched
wave 1045 A shares `DataBinder.cs` with B and was dispatched later, so it re-plans onto this landing. The first Linux
gate run was GREEN in every leg but reported RED on its repository-written check. The lander had re-committed the
train into per-cluster commits while that run was in progress, so the check saw HEAD move. No test wrote to the
repository. The gate was re-run on the final head with the repository left alone (verdict in the report). Review: 0
findings. No diagnostic codes claimed; no inventory row moved (GAP unchanged).

