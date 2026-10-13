## Entry 1986 — 2026-10-12 20:52 PDT — PB2495: prototype signatures compare as the greatest fixed point; signature checks run after pointer types exist

Wave 1070, group F (kb/Work PB2495), one note and the sibling its re-probe found.

**Re-probe.** The note's repro (`selfref.cob`) puts a TYPEDEF in a prototype's WORKING-STORAGE, which §10.6.2 SR4 e)
forbids (COBOLNET2272), so it was rewritten as legal source: `PS1 IS PROTOTYPE` with `REPOSITORY. PROGRAM PS2.` and a
`USING` formal of a `TYPEDEF ... PROGRAM-POINTER TO PS2` type, `PS2` the mirror, `PS3` naming itself. On base `92cfea452`
`SET B TO A` between pointers TO PS1 and TO PS2 drew COBOLNET1959 (ISO §13.18.60.4 GR25 types the pointer by the
prototype's signature; §14.9.39.3 SR22 admits the SET when the signatures are the same — they are).

**Mechanism.** Inside `PrototypeSignatures.Same`, a formal that is itself a restricted pointer reached the typed
comparator's pointer arm, which compared stamped class identities — and while the classes are being built that identity
is the prototype's own NAME. So the relation was the least fixed point: two prototypes that name each other were never
equal. The sweep of the other arms found a second cause of the same refusal: the group-level signature checks
(`CheckPrototypeSignaturePairs`, §10.6.2 SR2 / SR3, COBOLNET1513; override COBOLNET0829; implements COBOLNET0841;
interface inheritance; property accessors) ran BEFORE `BinderDriver.ResolvePointerRestrictions`, so every one of them
compared restricted prototype-pointers by name, which PB2464 had removed only from the checks that run later. A
prototype / definition pair, an override and an implementation restricted to two differently named prototypes of one
signature were all refused (probed).

**Fix shape.** The pointer's resolved prototype is now its own model fact, `DataItem.RestrictedPrototype` (namespace,
externalized name, signature), attached by `DataBinder.ResolveRestrictedPrototypes` over every forest before
`DataBinder.ClassifyRestrictedPrototypes` stamps any class (two passes, because classifying follows a formal's
restriction into another forest). `PrototypeSignatures.SameType` is the one "of the same type" relation over two such
pointers: outside a signature comparison it compares the classes (the identity the universal INVOKE's run-time
descriptions carry); inside `Same` it compares the two named signatures with the pair assumed equal
(`PrototypeBisimulation`, threaded through `OoConformance.DescriptionMismatch`'s new `prototypes:` parameter) — the
standard construction for equality of recursive types, sound because the comparison is a conjunction. The five
signature checks moved after `ResolvePointerRestrictions`, and a drift test pins that order.
`PrototypeSignatureClasses.Identity(RestrictedPrototype)` replaced `ProgramIdentity` / `FunctionIdentity` (every caller
changed). The function-pointer carrier shares the code; probed separately at 2014.

**Goldens.** `2002/pb2495_recursive_prototype_signature` (SR22 SETs across PS1 / PS2 / PS3; a CALL through a prototype /
definition pair restricted to PS2 / PS1; a universal INVOKE that matches, and one into a prototype whose signature
differs two steps down, which sets EC-OO-METHOD), `2002/pb2495_pointer_type_in_signature_checks` (the SR2 pair, an
override and an implementation), negative `pb2495-recursive-prototype-signature-mismatch` (COBOLNET1959 at 2002, 2014,
2023: the fixed point still finds a difference a finite path reaches). Drift test
`PrototypePointerTypeDriftTests.EverySignatureCheckRunsAfterThePointerTypesAreResolved`. No inventory row closes
(SR-14.9.39.3-20 / -22 and GR-13.18.60.4-25 / -26 were already CONFORMS).

**Self-review.** One relation in one place (`SameType`); no name comparison added (the drift test's matcher was
extended with the new lines); no hot-path allocation beyond one small object per top-level `Same` call (its set is allocated only when a restricted-pointer pair is met). Two leads are
left for notes: the group-formal arm (`StrongTypeModel.SameAnalyzedProfile` compares `PicInfo` by record equality,
which includes the prototype NAME) and the self-name scope disagreement between the TO-phrase screen and
`ProgramPrototypesOf`.

**Branch review, round 1.** R1-1: the order pin compared TEXT offsets in `BinderDriver.cs`, so its `CheckPrototypeSignaturePairs` row passed on the very defect it named (on main that call sat in `BuildGroupRepository`, written below `Bind` but run before `ResolvePointerRestrictions`). It now parses the compiler with Roslyn and requires each check to be invoked once, as a top-level statement of `Bind` after the resolver's; `TheOrderPin_FiresOnThePB2495Defect` feeds it main's shape and catches both arms, and moving the call back into `BuildGroupRepository` in the tree turned the theory red. R1-2: the comment at the call site (and the test's summary) blamed the DATA phase; both now say the cause is running before `ResolvePointerRestrictions`, which a check after the DATA phase can still do.

