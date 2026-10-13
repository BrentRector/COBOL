## PB3120 - a group formal's restricted pointer member is compared by its TYPE, not by the name it was written with

**What the note said, and what the re-probe showed.** PB3120 (filed from the PB2495 report) said the group-formal arm of
type-declaration equivalence compares the restricted prototype NAME: `StrongTypeModel.SameAnalyzedProfile` used
`PicInfo` record equality, which includes `RestrictedPrototypeName`, so pointer-typed groups restricted to differently
named prototypes of one signature were refused COBOLNET1513. The note's own repro (`grouppairs.cob`) still drew
COBOLNET1513, but along a different path than the note named: its group is NOT strongly typed, and a group with a
pointer leaf and no boundary image is refused earlier, by the Tier-C island arm of `OoConformance.DescriptionMismatch`
(the Tier-C island, `TierCIsland.Reason`; PB1116 lifted it for strong groups only). The note's arm is reachable only through a STRONGLY-typed group (leaf-carried,
PB1116), so the probe was re-cut with `01 TG IS TYPEDEF STRONG. 05 L-P TYPE T1.` in both programs: on the unfixed tree it
drew COBOLNET1513 for QA / QB (one signature, two names), and it compiles and runs after the fix.

**Three arms of one root, all fixed.** The pointer's type is decided in several places; the elementary arm had been
moved to the type relation (PB2464, PB2495) and the group arm and two siblings had not.
1. `SameAnalyzedProfile` now elides `RestrictedPrototypeName` from the record compare and asks
   `PrototypeSignatures.SameType` of the two items.
2. The strongly-typed sentence of a signature comparison (`OoConformance.StrongTypeMismatch` ->
   `StrongTypeModel.SameType` -> `EquivalentTypeDeclarations` -> `SameElementaryLayout`) never received the enclosing
   `PrototypeBisimulation`, so two prototypes that name each other through a strong group (`GS1 USING strong group
   holding a pointer TO GS2` and the converse) fell into two signature classes and SET B TO A drew COBOLNET1959. The
   bisimulation is now a parameter of the whole chain, and `PrototypeSignatures.SameDescription` passes the one it holds.
3. The same record compare also read `RestrictedTypeName` by value: a restricted DATA-pointer member of two strong groups
   was equivalent when the type-NAMES matched, though the elementary arm (`SameRestriction`) also asks the two
   declarations to be equivalent. Prototype / definition pairs whose `T-REC` differed (`PIC 9(4)` against `PIC X(4)`)
   were accepted silently by the group arm and refused by the elementary arm. The profile compare now asks
   `SameRestriction`. That recursion cannot rest on the record compare (a type whose pointer member is restricted back to
   it never terminates), so one private `Comparison` rides the walk: it carries the prototype bisimulation and the set of
   declaration pairs assumed equivalent while they are compared (the greatest fixed point; a difference anywhere still
   answers false for the whole conjunction).

`StrongTypeModel.TypeIdentityKey` (the run-time key a universal dispatch compares across source elements) read the same
record text, so it keyed a restricted prototype-pointer by the prototype's name and a data-pointer by its type-name
as written (case-sensitive, no declaration). It now keys the signature class, the folded type-name and the key of the
declaration it resolves to, with `@depth` back-references for a cycle.

**Goldens.** `2002/pb3120_group_formal_pointer_type` (QA / QB prototype / definition pair through a strong group, the GS1 /
GS2 mutual recursion, a self-referential `T-NODE` pair; output `PX NULL`, `set ok`), and negatives
`pb3120-group-formal-pointer-signature-mismatch` (QA against QC: still COBOLNET1513),
`pb3120-group-formal-data-pointer-declaration-mismatch` (two declarations of T-REC: COBOLNET1513) and
`pb3120-group-formal-pointer-type-below-2002` (the edition floor, COBOLNET0900). `PrototypePointerTypeDriftTests` gained
`EveryPicInfoProfileProjectionElidesTheRestrictedNames` (with a proof that it fails on the pre-repair projection).

**Found in passing, not fixed.** A group with a pointer leaf that is NOT strongly typed is still refused at a CALL and in
a signature pair (the Tier-C island, COBOLNET1688 / 1513): a different mechanism (the group has no boundary character
image), reported as a lead and not widened into this change.
