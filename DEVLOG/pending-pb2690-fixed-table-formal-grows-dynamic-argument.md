## PB2690: a BY REFERENCE formal's fixed table stored past a dynamic argument's capacity now creates the occurrences

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

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261012T192144Z-cfdfc4`, Conformance
5,008/11,492, Unit 32,713/33,454, Characterization 36/36; the three goldens and the negative Passed in
`leg-1-Conformance.trx`). The Linux gate is the landing's.
