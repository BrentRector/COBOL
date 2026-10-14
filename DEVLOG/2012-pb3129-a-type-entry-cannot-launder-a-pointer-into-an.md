## Entry 2012 — 2026-10-13 19:59 PDT — PB3129: a TYPE entry cannot launder a pointer into an ordinary group (§13.18.60.3 SR14 via §13.18.57.4 GR1)

Wave 1072, group F. PB3129 was filed as legal source rejected: a group that is not strongly typed, holding a
PROGRAM-POINTER leaf written `05 L-P TYPE T1`, could not cross `CALL PX USING G` (COBOLNET1688, the Tier-C island) and
its program prototype / definition pair disagreed (COBOLNET1513). The re-probe on this worktree reproduced both. It
also showed that `01 G. 05 A TYPE PT.` (PT an elementary POINTER, PROGRAM-POINTER or OBJECT REFERENCE type) compiled
clean, while its SAME AS twin `01 P USAGE POINTER. 01 G. 05 Q SAME AS P.` was already COBOLNET1724.

The spec decides it the other way. §13.18.60.3 SR14 allows those usages only "for an elementary data item at level 1
or an elementary data item subordinate to a type declaration that includes the STRONG phrase". §13.18.57.4 GR1 makes a
TYPE entry "as though the data description identified by type-name-1 had been coded in place of the TYPE clause", the
same words SAME AS uses (§13.18.49.4 GR1). So the note's group is not conforming source, and the defect was an
under-rejection at the declaration. The CALL and pair errors were where it surfaced. On conforming source, a group with
a pointer- or object-class leaf is always strongly typed, and PB1116 / PB1940 already carry that case. The legal
sibling, a STRONG group with a same-pointee program-pointer leaf, crosses the prototype pair and the CALL correctly.
⚠ The determination is recorded in the note: the rejected reading (SR14 binds only a written USAGE clause) would make
the TYPE and SAME AS twins disagree.

Mechanism: `ConformanceForest` prunes every entry that carries a TYPE clause, because the template was judged once.
That holds for a group type, but an elementary pointer type is a legal level-1 template, and only its TYPE site breaks
the rule. SR14's arm B is now one helper, `ScreenSr14ElementaryPlacement`. `CheckUsageDeclarations` calls it from the
written-entry loop and from the composed-entry loop (PB1212's EXTERNAL / BASED loop), there for every WRITTEN entry
that carries a TYPE clause and never for a clone. The message names the route. `COBOLNET_DATA_MODEL_DESIGN.md` D22 now
records the blind spot and closes its "everything else is rejected" list.

Goldens: `2002/pb3129_type_pointer_placement_ok` (level-1, level-77 and STRONG-member TYPE pointers, and the strong pair
crossing BY REFERENCE per §14.2.3 GR8) and `negative/pb3129-type-pointer-in-weak-group` (COBOLNET1724 at 2002, 2014 and
2023). A sweep compiled the 555 corpus programs that write a TYPE entry. One golden held the shape as a side case,
`2002/pb1408_address_of_restricted_pointer` (`05 GP TYPE PT` in a weak typedef). That case has no conforming spelling,
because ADDRESS OF a member of a strong group is COBOLNET2785, so it was removed and the program's output is unchanged.
Row `SR-13.18.60.3-14` was re-verdicted CONFORMS with the new witnesses (GAP 103 → 103).

Gate: the first run was red on the new negative's own comment line (261 positions, over the 255 that ISO §6.1 3) a)
allows a free-form line, COBOLNET2653). The re-gate (run `20261014T005513Z-22f025`) read `=== BUILD-LOCAL GATE: LEG 1
ONLY (batched gating, PB2515): GREEN`, with both new goldens and pb1408 Passed in leg 1. Self-review found no second
copy of the rule. The two loops of `CheckUsageDeclarations` visit disjoint sets: the first skips every entry with a
TYPE anchor, and the second visits only entries that carry a TYPE clause.

Branch review R1-1 found that the fix closed the TYPE-entry blind spot for SR14 only. The blind spot belongs to the
forest, which prunes every entry that carries a TYPE clause, so SR4 had it too: `01 IT IS TYPEDEF USAGE INDEX.` with
`05 A TYPE IT` inside a CONSTANT RECORD compiled and ran, and a pointer type in the same place was reported under SR14
instead of SR4 (§13.18.60.3 SR4). Sweeping the first loop's other arms found a third case. `01 G TYPE VT BASED.`, where VT
has a DYNAMIC LENGTH member, compiled and printed `A=ABC D=HELLO` (§13.18.5.3 SR2), and inventory row `SR-13.18.5.3-2`
claimed this spelling was screened. Each first-loop screen is now one helper, and the composed-entry loop asks every
one of them in the first loop's order: SR4 (`ScreenConstantRecordUsage`, which checks a TYPE entry's whole composed
subtree and reports once at the TYPE entry), EXTERNAL, BASED SR1, SR2 (`ScreenBasedSubjectShape`), then SR14 arm B.
The new `UsageDeclarationPlacementDriftTests.EveryWrittenEntryScreenIsAskedOfTheTypeEntryToo` reads both loops from
the source, so a future first-loop screen fails until the second loop asks it too or the test records why it does not
need to. Goldens: `negative/pb3129-type-index-in-constant-record`, `negative/pb3129-type-group-pointer-in-constant-record`
(COBOLNET1726), `negative/pb3129-type-based-variable-length-group` (COBOLNET2560), and the positive
`2002/pb3129_type_in_constant_record_ok`. Rows `SR-13.18.60.3-4` and `SR-13.18.5.3-2` were re-verdicted CONFORMS with
the TYPE-route witnesses (GAP 103 → 103).

