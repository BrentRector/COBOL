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

**Review round 1 (R1-1).** `TypeIdentityKey` of a TYPE subject (`ADDRESS OF S`) and of its TYPEDEF template (a method's
`LP TYPE PT`) must be one string, and they were not. Measured: the pointer member inside the TEMPLATE carried no
`RestrictedTypeDecl`, because `DataBinder.ResolveRestrictedTypes` walked `AllItems()`, the storage roots, and a template is
kept off them; the subject's clone was resolved. The subject therefore keyed the member as `t-rec@1` and the template as
`t-rec`, so `INVOKE U "TR" USING ADDRESS OF S` raised EC-OO-METHOD where main selected the method (ISO 8.4.3.11.4 GR2 types
the address by the type of S, 14.8.2.3.2 matches it to a formal restricted to that type). Root fix: the three pointer
resolvers (`ResolveRestrictedTypes`, `ResolveRestrictedPrototypes`, `ClassifyRestrictedPrototypes`) walk
`ClauseResolutionForest()`, the templates too, and `TemplateClauseResolutionDriftTests` pins all three. A back-reference to
an open declaration is also recognized across the two forms of one declaration (`IsOpenDeclaration`: the template, or a
TYPE subject of that type-name). The prototype twin was the same hole with a visible symptom: a data-pointer restricted to
a record whose member is a program-pointer was compared through the template, where `QA` against `QB` (one signature)
drew COBOLNET1513; goldens `2002/pb3120_universal_type_subject_key` (equivalent: TR:REACHED; a class whose T-REC differs
in length: EC-OO-METHOD), `2002/pb3120_group_formal_template_pointer_signature` and negative
`pb3120-group-formal-template-pointer-signature-mismatch` (QA 9(4) against QB X(4): still COBOLNET1513).

**Review round 2 (R2-1).** The comment on `IsOpenDeclaration` justified matching a subject to its template by type-name with
"13.18.58 makes a type-name unique", and 13.18.58 states no uniqueness rule (`cite.py --check 13.18.58 "unique"` fails). The rule
is 8.3.2.2 (a user-defined word may be only one type of user-defined word within a source element; `--check` OK). The same
inherited wording sat at four sibling sites (DataBinder.cs, PicInfo.cs, StrongTypeModel.cs, DESIGN-data-model.md); all five now
cite 8.3.2.2. Comment and doc text only; no behavior change.
