## Entry 1915 — 2026-10-07 07:21 PDT — Train 1031: five wave-1031 clusters land (PB1229, PB480, PB434+PB387, PB397, PB2093+PB545+PB552); a review fix to PB397's STOP row; GAP 173 → 162

**D — PB1229 (compile-time arithmetic and the figurative ZERO).** `CompileTimeExpressionEvaluator.EvalArith` read the
`ZERO_ARITH` token as 0, so `01 K CONSTANT AS ZERO + 1` compiled and printed 1. §7.3.6.2 SR1b admits only fixed-point
numeric literals and §13.10.3 SR6 bars a figurative constant from arithmetic-expression-1. The arm now reports
COBOLNET1547 in the one recursion every consumer shares; the directive entry's §7.3.3 SR10 screen (COBOLNET1619) was
re-probed and already held. Negative `constant-figurative-arith-operand`, six-shape unit theory; three rows to CONFORMS.

**B — PB480 (variable-length groups across INVOKE).** The universal relation compared two variable-length groups by a
signature string whose equality refused compatible pairs, and both INVOKE lanes handed the argument's carrier to a
formal of another shape unconverted: the typed lane cut the argument's tail on write-back and read a dynamic table
from the wrong storage. The §8.5.1.12 walk now lives once, in the runtime (`GroupCompatibility.Walk` over `GroupAtom`
layouts), asked by the bind-time screens and the run-time relation alike; `ActivationDescription.Atoms` replaces the
signature, and `CobolVarGroup.Reshape` / `Overlay` convert a carrier and store a view back (§14.2.3 GR8). Golden
`2014/pb480_universal_variable_length_shapes`. Split: PB2076 stays open. The Format-2 CALL lane has the same carrier
defect (filed PB2280) and `CorrespondingSpans` is a second correspondence walk (filed PB2281).

**A — PB434 SR8 and PB387.** `UNTIL EXIT` with VARYING or a TEST phrase was a bare COBOL0001, and the "under" shapes
compiled. One grammar target (`performUntilTarget`) at every UNTIL level, one screen
(`ControlFlowBinder.CheckUntilExitPlacement`, COBOLNET2954) for both formats, and the out-of-line "under" decided once
the pc space is bound over `ProcedureReach.Closure`, a static model of §14.9.28.4 GR1 that PB812 can now reuse (pointer
added to PB812). Determination D-SR8: a TEST phrase counts only when written. PB387's IF rows were already fixed by
PB396; `VacuousTestDriftTests` now fails a test that cannot fail, and the 42 deleted stub claims are measured
(`GeneralFormatViolationTests`). The implementer's unverified lead, STRING without DELIMITED and PERFORM VARYING
without BY compiling at 85, is filed as PB2282 (it needs the 1985 text).

**E — PB397 (the statement-sequence rules), with a review fix.** §14.9.14.3 SR1, §14.9.17.3 SR2 and §14.9.42.3 SR1 were
enforced nowhere because the binder had no position axis. `StatementPosition` (a parse-tree view) and three rows of
`PlacementRules.RefusedOutOfSequence`, asked once per statement from `BindStatement`: COBOLNET2966-2968. **The review
found the STOP row too wide:** it matched every `stopStatement`, so the X3.23-1985 STOP literal, which communicates to
the operator and continues (`BoundStopLiteral`), was refused when a statement followed it. `STOP "OPERATOR-MSG"` then
`DISPLAY "AFTER"` drew COBOLNET2968 at `--std 85`, which is legal source. §14.9.42 has only STOP RUN, so the row now
matches STOP RUN; the pinning test became `StopLiteral_FollowedByAStatement_IsAccepted`, the 85 golden prints
AFTER-STOP-LITERAL (the first re-gate ran that golden against the unfixed binary and went red on exactly that line),
D-SEQ and D14b say so, and the row's renamed test-ref was retired by batch. A second re-gate went red only on
`SpecTraceabilityInventoryDriftTests` (the stale test-ref) before the retirement. The implementer's gate-accounting
flake (8 theory cases reported NEVER RAN once) is filed as PB2283.

**C — PB2093+PB2098, PB545, PB552 (PB517 discharged).** C# names for COBOL members come from one seeded allocator
(`CsNames`, `DataItem.AddMember`), so members named `AsImage`, `Equals` or a record named `CloseFiles` compile where
they drew CS0102. A file-section TYPE whose type holds an object reference is refused (§13.18.57.3 SR8, COBOLNET2960).
A level-88 THROUGH pair that does not ascend is refused (§13.18.63.3 SR26, COBOLNET2961) under D-RANGE-KNOWN: every
non-LOCALE sequence is known. That was settled by rule 1's precedence (GnuCOBOL checks nothing, IBM 6.4 requires
ascending), which superseded PB503's "needs an owner decision"; the owner can still overturn it. Seven goldens carried
inverted ranges and were reworked. PB667 was not started and stays open.

**The train.** Gate (`build-local.ps1 -Mode lander`, whole population, one leg), run 20261007T140835Z-26e5e1: GREEN —
Conformance 10,829/10,829 · Unit 31,683/31,683 · Characterization 35/35, every population EXACT. Linux gate
(`scripts/linux-gate.sh`, HEAD eb3de76be): GREEN on hooks, unit, characterization, conformance and guard. CI audits run locally, all clean (code and doc citations, evidence supersession, witness loss: 0
unexcused, drift rules current, rule catalog, `work.py check`); semgrep 46/2/273 before and after. The oracle against
the recorded baseline (f620ccf6d43d, train 1026) showed 86 differences; against a capture of the train's own base
(3f0604783e3d, the compiler of 1916f7a1c) 47, so the other 39 belong to trains 1027-1029 (PB2152 carries them). The
47, by class: 13 ADDED (the train's new goldens and negatives); 18 emitted-C# changes from B, where activation
descriptions now carry `Atoms` in place of `Layout`/`VariableSignature`, and pb204_vlg_invoke now calls
`CobolVarGroup.Reshape` because its two groups differ in shape; 8 from C's reworked golden sources (pb398, pb502,
pb728, pb761, ec_range_invalid, three pb367b programs); 1 from E's corrected pb413 source; the negative
pb434-finally-transfer-out now also draws E's COBOLNET2967 (its FINALLY phrase writes GO TO before EXIT PARAGRAPH,
which SR2 forbids); and six `perform-until-exit-2023` matrix cells whose COBOLNET0900 moved from column 13 (UNTIL) to
19 (EXIT), because A's edition gate now visits `performUntilTarget`. None is unexplained; the baseline is re-recorded
as e0b64a4e6edf. GAP 173 → 162 (D 3, A 3, E 3, C 2). Codes claimed: COBOLNET2954, 2960, 2961, 2966-2968; returned:
2955, 2956, 2957-2959, 2962, 2963-2965. Notes landed: PB1229, PB480, PB434, PB387, PB397, PB2093, PB2098, PB517, PB545,
PB552; filed: PB2280-PB2283. REVIEW: 1 finding, 1 fixed in the train, 0 clusters dropped.

