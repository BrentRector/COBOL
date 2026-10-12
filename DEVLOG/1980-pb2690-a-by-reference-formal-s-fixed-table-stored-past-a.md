## Entry 1980 — 2026-10-12 16:43 PDT — PB2690: a BY REFERENCE formal's fixed table stored past a dynamic argument's capacity now creates the occurrences

The note's last open arm asked for an adjudication: a Format 2 CALL BY REFERENCE of a group holding a dynamic-capacity
table, opposite a formal whose corresponding table is FIXED, lost the callee's store to an occurrence past the argument's
capacity (`G1=[xpzz] C1=1`, the probes `b6.cob` and `c6.cob` of wave 1045 F). Re-probed on this build: still reproduced.

**What the standard says.** The earlier code recorded an "implementor determination" that a fixed table has no capacity
to state, so it cannot change the argument's. The text does not leave that latitude. Section 14.2.3 GR8 makes a BY
REFERENCE formal occupy "the same storage area as the argument", so a store through the formal's occurrence 2 is a
receiving reference to the argument's dynamic-capacity table, and section 8.5.1.9.3 says what that does: "a new element
is automatically created and the capacity of the table is increased to the value given by the subscript". Section
8.5.1.12.3 sentence 3 makes the pair legal "for purposes of determining compatibility" and nothing more, so a call that
stores nothing past the capacity creates nothing. All citations were run through `cite.py --check`. The one choice left
to the implementation is how a store is recognised: the boundary hands the callee a copy and sees only the finished view,
so a store counts when an occurrence past the capacity holds anything but the space fill the view showed. A callee that
only reads there creates nothing; one that stores spaces into an occurrence the argument lacks cannot be told from one
that did not. That is D-VGB in `docs/CONFORMANCE.md` section 3 (no vendor behavior consulted: the ISO text decides the
arm that matters).

**The fix** is one place, `CobolVarGroup.Overlay`'s fixed-table arm: `OverlaidTable` for a table of scalar elements,
`GrownCount` for a dynamic table of variable-length elements (and the same law one level down, for the inner table of an
element). `OverlayImage` (a fixed GROUP formal over a variable-length argument), the variable-length formal with a fixed
table, and both INVOKE lanes (the typed `RuntimeApi.VarGroupOverlay*` and the universal `CobolInvokeArg`) all reach it.
The reverse pair (a variable-length formal over a FIXED argument) cannot grow, as before.

**The performance residue** the train 1052 lander found: several boundary paths built a fresh `FixedRun(n)` atom array
per call, so each CALL missed the pair-plan and geometry caches, keyed by array reference, and added an entry.
`GroupCompatibility.FixedRun` now returns one array per width (registered in `ProcessLifetimeCollections`).

**Witnesses.** Goldens `2014/pb2690_fixed_table_formal_grows_dynamic_argument_call` (scalar elements; the callee that
reads only, BY CONTENT, a fixed GROUP formal, a store into the dynamic-length item alone),
`..._vlg_element_table_call` (elements holding an inner table), `..._dynamic_argument_invoke`; negative
`pb2690-fixed-table-formal-grows-dynamic-argument-below-2014`; unit tests in `BoundaryGroupCorrespondenceTests`. Two
existing tests pinned the old reading and were re-derived from the text (`...AFixedGroupFormal_ThroughThePairsCorrespondence`,
the inner-occurrence store of `...ReachesAFixedGroupFormal_AndItsStoresOverlay`), and so was the OUTPUT of golden
`2014/pb965_vlg_into_fixed_invoke` (`VCAP=2` became `[mno]` and `VCAP=3` after a method stored `LT(3)`).

**Self-review.** The silent-failure lens found the one edge above (stored spaces) and it is documented, not hidden. The
sibling sweep found no other writer that drops a store past a dynamic table's capacity: MOVE and RETURNING recreate the
receiving table through `Reshape`, and the fixed-argument direction has no occurrence to create. No diagnostic codes used.

**Branch review round 1.** Three findings. R1-2 (a wrong clause): the space fill of an occurrence the argument lacks is
section 14.6.9.2 2) ("all the remaining elements of the receiving table are space filled"), not 8.5.1.12.2's last sentence,
which governs a trailing table past the shorter group's end; D-VGB and the golden now cite the right clause. R1-3: the note
heading still said the related arm was open. R1-1 (a real departure): the occurrences a boundary store creates come back
space filled, where section 8.5.1.9.5 initializes the unreferenced items of an INITIALIZED table from the default (probe: the
caller prints `[ab|  |r ] 3`, a direct store prints `[ab|ab|rb] 3`). It is not fixed here: the table's default element exists
only as a C# seed expression on the `CobolDynTable`, so it would have to travel on the argument carrier from two builders
(`GroupImageCodec` AsVarImage, `StorageCell.VarGroupAt`) and six carrier rebuilds, a mechanism of its own. PB2690 is `half`
with a REMAINING section naming the sites, and D-VGB records the departure.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261012T192144Z-cfdfc4`, Conformance
5,008/11,492, Unit 32,713/33,454, Characterization 36/36; the three goldens and the negative Passed in
`leg-1-Conformance.trx`). The Linux gate is the landing's.

**Branch review round 2.** The reviewer found that the half-status note still carried a title and heading that read as finished work; the title now leads with the REMAINING INITIALIZED-default item, the heading says HALF, and spec_refs gains 8.5.1.9.3 and 8.5.1.9.5, so `work.py next` and the planners see what is open. Register-only change.

**Landing stop (finisher).** The landing's Linux gate went RED on two existing goldens that pinned the old reading and
that the branch's sweep had missed (the Windows leg 1 never selected them): `2014/pb2087_vlg_returning_cell_receiver`, a
copy of `pb965_vlg_into_fixed_invoke` plus a TOUCH, still expected `VCAP=0000000002`, and
`2014/pb480_universal_variable_length_shapes` still expected the callee's `MOVE "ZZ" TO BT(3)` past capacity 2 to be lost
(`C2=2`). Both expectations are wrong answers under section 14.2.3 GR8 with section 8.5.1.9.3 (cite.py OK), the text the
branch was decided on, so the goldens were re-derived and the mechanism kept: pb2087 now displays `VT(3)` and expects
`VG=[qq][mno][ij] VCAP=0000000003`; pb480 sets the capacity back to 2 before each lane, so the universal and the typed
invocation each create the third occurrence (`G2=hh/AABBZZ/xyz C2=3`). Sweep: all 28 corpus programs (2014, 2023) that hold
a dynamic-capacity table and cross a CALL, INVOKE or RETURNING were run on the branch build against their goldens: 27
identical, one (`pb1042_dyn_table_agreement`) differing only by trailing spaces the corpus runner trims. A unit-test comment
that still said a fixed formal table never re-sizes the argument's now says it never shrinks it.

**Landing stop (finisher, oracle).** `landing_oracle.py` refused the landing: the arch-oracle baseline `60d47c6d52a9`
was behind 19 changed inputs. Main had meanwhile recorded `69e756b0880b` (PB2721); after merging origin/main
`241c6de2d` cleanly, `compare_oracle.py` against it printed `=== ARCH-ORACLE: DIFFERENT — 7 of 7786 cases differ
(baseline 69e756b0880b, new 4dfb26c315bf) ===`. Every case is this branch's own, by class: ADDED 4, the new goldens
`2014/pb2690_fixed_table_formal_grows_dynamic_argument_call`, `..._dynamic_argument_invoke`,
`..._vlg_element_table_call` and the negative `pb2690-fixed-table-formal-grows-dynamic-argument-below-2014`; CSHARP 3,
`2014/pb2087_vlg_returning_cell_receiver`, `2014/pb480_universal_variable_length_shapes` and
`2014/pb965_vlg_into_fixed_invoke`, whose COBOL sources the branch edited (each now displays the third occurrence, and
pb480 adds `SET C2 TO 2` before each lane). No other case differs: the fix is in the runtime
(`CobolVarGroup`, `GroupCompatibility`), so the emitted C# and the diagnostics of every unedited program are unchanged,
including `pb965_vlg_into_fixed_boundary`, whose edit touched comments only. The baseline was re-recorded on the merged
head and replaces `69e756b0880b`.

