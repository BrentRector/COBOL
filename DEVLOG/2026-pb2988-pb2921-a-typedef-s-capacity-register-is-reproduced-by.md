## Entry 2026 — 2026-10-14 04:32 PDT — PB2988 + PB2921: a TYPEDEF's CAPACITY register is reproduced by every copy; synthesized C# names are prefix families; PB546: one reader for the bytes a national position occupies (w1070 A); PB3030's bit arm: one OCCURS DEPENDING tail counted in bits; PB2921 review round 2 (data named like a method)

### PB2988 + PB2921: a TYPEDEF's CAPACITY register is reproduced by every copy; synthesized C# names are prefix families

Wave 1069 group B (PB3030, PB2921, PB2988 — one root in the data model's naming and copy identity).

**PB2988 — a CAPACITY IN register written once in a type, used by two TYPE entries.** Re-probed on the wave's build
(ddee217e3): the note's e6 (`01 ST TYPEDEF` holding `OCCURS DYNAMIC CAPACITY IN DC`, then `01 A TYPE ST` and
`01 B TYPE ST`) still drew COBOLNET1523 at B's expansion and COBOLNET2127 at `DC OF B`. §13.18.38.3 SR30 says
"Data-name-3 shall not be defined elsewhere in the source element. If qualifiers are required for uniqueness, it shall
be treated as though implicitly defined at the same level as the entry containing the OCCURS clause" — the second
sentence exists exactly for the copies a TYPE (or SAME AS) entry makes of one written definition. The screen in
`DataBinder.DynamicResolve` counted each copy as another definition. It now compares WRITTEN definitions
(`DataItem.WrittenDefinition`, the `ClonedFrom` chain's end): copies of one written entry are listed side by side under
the name (the register map was already a list per name, for inherited GLOBAL registers), and only a different written
entry or an explicit data-name stays the SR30 duplicate.

The sibling arm was the resolver. `ReferenceResolver.CapacityRegisterFor` was documented "at most one register per
spelling in one element" and took the first qualifier match, so once copies could coexist the bare `DC` would have read
A's register silently. It now counts the registers the qualifiers reach in the nearest declaring element and states
§8.4.2.2.3 SR1 through the one ambiguity verdict, `DataBinder.UniqueOrReportAmbiguous` (COBOLNET1639; `--permissive`
warns and takes the first), as the new `CapacityRefFault.Ambiguous`. `SetBinder.BindSetCapacity` takes the resolver's
place for every non-None fault, so a SET Format 14 receiver obeys the same verdict. Goldens:
`2014/pb2988_capacity_register_type_copies` (two TYPE copies, a SAME AS copy, two TYPE copies inside one group read
through two qualifiers, SET through a qualified register — capacities 0 0, 1 3, 3 7, 2 4 5 6 from §8.5.1.9.1,
§8.5.1.9.3 and §14.9.39.4 GR30), negatives `pb2988-capacity-register-copies-unqualified` (COBOLNET1639) and
`pb2988-capacity-register-two-written-entries` (COBOLNET1523 still holds).

**PB2921 — the factory class's name was a suffix.** Re-probed: a group with `CLASS-ID. X.` and `CLASS-ID. X--FACTORY.`
was refused COBOLNET0820, because X's factory class was `X` + `__FACTORY`, which is also the C# identifier of the legal
class-name `X--FACTORY` (§8.3.2.1 allows inner hyphens; the injective word mapping writes `-` as `_`). The structural
fact is that only a name that BEGINS with `__` lies outside the mapping's image (no identifier begins with `__`), so the
factory class became the prefix family `NamingConvention.FactoryClassName` (`__FACTORY_X`, the runtime's
`__FACTORY_BASE`), every site routed through it, and `OoClassTable.Build` now treats a taken factory name as an internal
error. The same defect in another arm: the CS0542 rename of a METHOD-ID spelled like its CLASS-ID was the suffix `_M`
(the identifier of the method-name `X-M`); it is now the family `__method_`. Both join
`CobolWordDerivedPrefixes`, so `SynthesizedNameFamilyDriftTests` holds every emitter literal out of them, and
`IdentifierInjectivityTests` pins that no identifier begins with `__` and that the runtime's BASE factory type is the
convention's name for BASE. Golden `2002/pb2921_factory_named_class`. All 244 OO positive goldens re-run on the build.

Renaming the runtime's public `BASE__FACTORY` to `__FACTORY_BASE` is an incompatible change of the shipped surface, so
the gate's `RuntimeAbiPinDriftTests` asked for the documented step (DESIGN-external-repository §15.4): the product
version goes 2.0.0 → 3.0.0, the test pins major 3 beside the new surface hash, and the 14 characterization snapshots
name the runtime `3.0.0.0`. A local harness that re-ran the 244 OO goldens in their own directories rewrote a tracked
data file with CRLF and the gate's LINE ENDINGS audit caught it (`line_endings.py --fix`).

PB3030 (the bit-bearing OCCURS DEPENDING member beside a runtime-length table) re-probed and still the COBOLNET1756
stage; it is a third mechanism (the current-extent composer's ODO prefix in bit units, through the record layout) and
was split to a finisher with its design written into the note. Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched
gating, PB2515): GREEN ===` (run 20261012T194921Z-2feabf).

**The branch review's second finding (w1071 B, round 2).** Review R1-1: the OO deep-dive's "Emitted-name safety" item still
described the `_M` suffix that PB2921 replaced. The golden written to pin the prefix family (`2002/pb2921_class_named_method`,
METHOD-ID `PB2921C` in CLASS-ID `PB2921C` beside METHOD-ID `PB2921C-M`) failed before code generation: COBOLNET2692, because
the class binder kept ONE §8.3.2.2 census for the class and every method in it, so the rename it was meant to pin was
unreachable. A method is a source element of its own (§10.5, §8.4.6.5), so each method now carries its own census
(`UserWordCensus` on `OoMethodDataScope.Words`, selected by `DataBinder.EnterMethodDeclarations` for its data entries, its
METHOD-ID and its procedure-names). The same change admits a condition-name in one method spelled like a data-name in a
sibling method. The sibling arm: the method copy of the procedure-name walk declared no section-name and reported without a
position; program and method now share `ProcedureTableBuilder.CollectNondeclaratives` (negative
`pb2921-method-section-named-like-its-data`). The deep-dive item now describes the per-method census and the prefix families,
and plan §0 names the runtime base factory `__FACTORY_BASE`.

### PB546: one reader for the bytes a national position occupies (w1070 A)

Wave 1070 group A carried w1069 B's unlanded branch (PB2988, PB2921; its own pending entry) onto main 92cfea452 by
cherry-pick and re-gated it: `LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261013T012912Z-061c4a).

**PB546.** Re-probed on this build (`E:\COBOL-coord\scratch\w1070a\p\p546a.cob`): `FUNCTION BYTE-LENGTH(BN(1:2))` of
`BN PIC 1(4) USAGE NATIONAL` returned 2, and `MOVE G3 TO DN` (a three-byte group into `PIC N DYNAMIC LENGTH`) left
`LENGTH(DN)` = 6. Both are one mechanism: a byte count that decided "national" by the item's CATEGORY, while ISO
§13.18.60.4 GR8 makes the representation a matter of USAGE and §13.18.60.3 SR12 lets a usage-national item be boolean,
national, national-edited, numeric or numeric-edited. The bytes a position occupies are now spelled once,
`PicInfo.BytesPerPosition`, and every byte count reads it (ElementaryByteWidth, RefModPlace.PositionBits,
RecordLayout.ImageContribution, ItemLength r2, FileModel's dynamic extent, record image unit and national-record test,
IntrinsicBinder's dynamic-length LENGTH arms and national-literal BYTE-LENGTH fold, the SUPPRESS WHEN key positions).
`OperandText.AsStorageImage`'s reference-modification arm keys on the usage, so a boolean usage-national slice
serializes its UTF-16BE pairs (§8.4.3.3.4 GR3; BYTE-LENGTH 4, CONVERT `00310030`). Design D-N8: a whole storage image
into a dynamic-length national receiver decodes through `ReceivingStore.ContentCharacters` over the new runtime
`CobolBits.NatFromStorage` (an odd tail completed with the alphanumeric space, §14.9.25.4 GR4, §8.5.1.10.4), for the
group MOVE and the dynamic-record READ alike; `LENGTH(DN)` is 4. `NationalUnitDriftTests` walks the compiler with Roslyn
and is red when a national-unit emission is chosen by `PicCategory.National` alone; `ImageWidthIsStorageWidthTests`
states the rule for every usage-national category instead of a "sanctioned divergence". Goldens
`2002/pb546_national_leaf_one_width`, `2014/pb546_group_into_national_dynamic`, negatives
`pb546-national-boolean-below-2002`, `pb546-group-into-national-dynamic-below-2014`. Rows GR-8.4.3.3.4-3 and
GR-13.18.60.4-8 re-verdicted CONFORMS and SR-14.8.2.2-1 re-probed (GAP 103 → 101).

**The sibling the sweep found, and PB3030's first half.** The variable-length group codec measured an OCCURS
DEPENDING member's table with one occurrence's `ImageWidth`, a national element's positions, while the image holds its
bytes: `DISPLAY G` of a group with a dynamic-length leaf and a member holding `PIC N OCCURS 1 TO 4 DEPENDING ON N`
(N = 2) sent two bytes of the third occurrence. The table's geometry is now one function, `OdoModel.GeometryOf`, read by
`OdoModel.WrapGroup` for a group operand and by the codec's current image, carrier, receiving splice and record layout
for a member; golden `2014/pb546_odo_national_tail` (DISPLAY, the GR9 group MOVE, WRITE / READ). This is the extraction
kb/Work PB3030's design read asked for; PB3030's bit arm (a USAGE BIT ODO member, whose record layout needs a count
source) is split to a successor, and PB3101 (one activation-conformance relation) was not started.

**The branch review's one finding (w1071 B).** Review R1-1: `FactoryClassOf` had been inserted between `OoUnivStop` and its
§14.9.23.4 GR7c doc comment, so `FactoryClassOf` carried two `<summary>` blocks and `OoUnivStop` none. The method now sits
above the comment, which documents `OoUnivStop` again. A sweep for doc comments that open `<summary>` twice found 58 more
on main, none from this branch; that tree-wide class is left for its own change.
The finisher's first gate was RED on `SpecTraceabilityInventoryDriftTests.EveryTestRef_ResolvesToARealTest`: rows
GR-8.4.3.3.4-3 and GR-13.18.60.4-8 named the class `unit:NationalUnitDriftTests`, which resolves to no test. A batch
retired that ref and named the two methods that witness the rule (verdicts unchanged, GAP 101). The predecessor's gate had
planned that drift test into leg 2, so it never ran there. Re-gate: `LEG 1 ONLY (batched gating, PB2515): GREEN`
(run 20261013T192134Z-5a6bf2).

**The same review, answered on a parallel line (wave 1072 A2).** The read-only review of the branch at `18d11007f` found one thing
(R1-1): `OoEmitter.FactoryClassOf` had been inserted between `OoUnivStop` and its §14.9.23.4 GR7c doc comment, so
`FactoryClassOf` carried two `<summary>` blocks and `OoUnivStop` none. `FactoryClassOf` now sits above the block. The
same file held a second instance of the shape, an orphaned `<summary>` describing a STRING-CARRIED copy-in read whose
method no longer exists, stacked on `EmitInvokePickup`'s own; it is deleted. The tree-wide sweep for two adjacent
`<summary>` blocks finds 54 more such members outside `OoEmitter.cs`; that mechanical sweep, with the drift test
that keeps it at zero, is a lead for its own landing rather than a widening of this one. The finisher merged the
branch onto main `2da05a28a` (unions in `kb/Work/PB3101.md` and two manifests), re-probed PB2921 (a class `X` and a
class `X--FACTORY` in one compilation group: `X FACTORY`, `X--FACTORY FACTORY`) and PB2988 (two `TYPE ST` entries with
`CAPACITY IN DC`: `0000000001 0000000003`) on the merged build, and re-recorded the arch-oracle baseline over main's
`a6376c64ff94`. The comparison: 3,462 of 7,813 cases differ and 9 are added (this branch's goldens), every difference
in one of four classes: the runtime major stamp `"2.0.0.0"` → `"3.0.0.0"` in the module registration (3,187 cases
differ by that alone); PB2921's factory family `X__FACTORY` → `__FACTORY_X` and `BASE__FACTORY` → `__FACTORY_BASE`
(about 266 OO cases); PB546's `PicInfo.BytesPerPosition`, which appears in the §8.5.3.1 type-identity key
(`StrongTypeModel.EssentialCharacteristicsKey` renders PicInfo's record text by design) of strongly-typed
activation descriptions; and PB546's D-N8 store, `NatReadWindow(…, Length / 2)` → `NatFromStorage(…)` for a
dynamic-length national receiver (`2014/pb1094_dynamic_length_structure_elementary_record`).

**The second branch review, answered (wave 1073 A2).** The review of `0d9ddd99f` found one defect (R1-1) in
PB2988's new ambiguity arm. `CapacityRegisterFor` ranks `CapacityRefFault.Ambiguous` above `UnderATable`, and the
reporting site's `--permissive` arm returned the place computed for the unique case, which is null when the register's
table lies under a table. So a bare `MOVE DC` over two `TYPE ST` copies of `05 R OCCURS 2. 10 D … CAPACITY IN DC`
gave the COBOLNET1639 warning and then the internal error COBOLNET2362, where §13.18.38.3 SR30 and SR31 with
§8.4.2.3.3 SR3 call for COBOLNET2126. The register's own judgment is now one helper,
`ReferenceResolver.CapacityRegisterPlaceOf`, asked of the register a unique reference names and of the one the
permissive disposition picks; a `CapacityRef` carries a place only when it has no fault. The sibling sweep over every
caller of `UniqueOrReportAmbiguous` found the same shape in the index-name resolver: `ResolveIndexName` screened
§13.7.3 SR4 only for a unique candidate, so a permissive pick whose table lay in a LINKAGE record outside the USING
phrase went unscreened. The pick is now screened, COBOLNET2746. The other callers return the
picked item to code that judges it like any other. Regression tests (the negative corpus runs strict only, so these
are `CompileFull(…, permissive: true)` tests): `OccursDynamicGuardTests.Permissive_AmbiguousCapacityRegister_*` (two
under-a-table shapes, plus the legal first-declared copy that runs and reads 3) and
`IndexNameUniquenessTests.Permissive_AmbiguousIndexName_ScreensTheLinkageRecordItResolvesTo`. Merging main brought
in PB3107, which commits no arch-oracle baseline, so the baseline the first review finisher re-recorded is deleted in
the merge; the four difference classes above stand as that comparison's record.

**The third branch review, answered (wave 1074 A2).** Wave 1074 A2 merged the wave-1074 A branch (PB3030's bit arm,
the R2 method-member names) with the wave-1073 A2 branch, unioning the two manifests and this entry, and answered
the review of `c5324ec37` (R1-1). The index-name CS0131 did not come from the missing linkage screen. `ResolveIndexName`
reports a written reference once, and SET asks about its operand twice: it classifies the operand, then binds it. The
second ask of an ambiguous reference answered `Failed` with no declaration, so the SET receiver was the inert
occurrence number `1`, and `1 = __set0;` failed Roslyn. Every `--permissive` ambiguous SET of an index-name failed
this way, legal shapes included. Re-probed: `SET IX TO 2`, `SET IX UP BY 1` over two tables `INDEXED BY IX` gave
three COBOLNET1639 warnings and then CS0131 twice. Now the verdict does not depend on whether the ask reports:
`DataBinder.UniqueOrReportAmbiguous` takes `report` and returns the same item either way (still the one reader of
the first-declared survivor that `DataNameResolutionDriftTests` pins), and `ResolveIndexName` gates only the
diagnostic. The program prints `THREE` and `A`. The sibling is `CapacityPlaceOf`, whose report-once guard returned
null for a probe or a repeated ask of an ambiguous register where the reporting ask returned the picked register's
place. It gets the same split. Regression tests:
`IndexNameUniquenessTests.Permissive_AmbiguousIndexName_SetResolvesToTheFirstDeclarationAtEveryAsk`,
`…_InAReferenceableLinkageRecord_Compiles` and
`OccursDynamicGuardTests.Permissive_AmbiguousCapacityRegister_InSubscriptAndRefModPositions_ReadsTheFirst`.
The finisher's first gate was RED in the self-tests on `test_orchestrate_stop.ps1` ("stale lock taken over"), which
passed alone. Its "dead" lock PID 999999 was not dead: this host's live PIDs pass 1,000,000, and Windows ignores a
PID's two low bits, so the lock named process 999996 whenever one was running. The case now writes
`Int32.MaxValue - 3`, which no Windows or Linux process can hold.

### PB3030's bit arm: one OCCURS DEPENDING tail counted in bits; PB2921 review round 2 (data named like a method)

Wave 1074 group A, finishing the w1071 B branch (merged at 1a473341f) and taking PB3030's second half.

**PB2921, review round 2.** R2-1: the per-method §8.3.2.2 census (round 2's fix) let an object data-name spelled
like a METHOD-ID through to the emitter, which gave the private data field and the method one C# member name
(CS0102 on legal source; probe `E:\COBOL-coord\scratch\w1074a\r2\p1.cob`, and `p5.cob` for the factory pair). The
object data and the instance methods share one emitted C# class, so `OoDriver.BindClassData` now reserves the methods'
C# names in the data binder's member scope before the data binds (`DataBinder.ReserveMethodMemberNames`) and the field
takes `PQ_2`. A derived class's data spelled like an inherited method (`p6.cob`) and a method's LOCAL-STORAGE item
spelled like a sibling method (`p8.cob`) were already right. Golden `2002/pb2921_data_named_like_method`. R2-2: the
census paragraph of `docs/COBOLNET_INTRINSICS_DESIGN.md` and two `DataBinder` comments still described one census per
binder and a method declared by the OO driver; both now name the per-method census and `OoBindMethodData`.

**PB3030.** Re-probed: `FUNCTION LENGTH` of a group holding a dynamic-capacity table of dynamic-length elements and an
OCCURS DEPENDING table of USAGE BIT items still warned COBOLNET1756 and aborted the run unit. `DataItem.
CurrentExtentComposes` refused the bit-bearing member; admitting it needed the variable-length group codec to count the
table in bits (§8.5.1.6.3 places each bit occurrence "at the next bit position in storage", so occurrences share
bytes). The fix is one tail: `OdoModel.FixedImageTail` states `OdoModel.GeometryOf` as the runtime `OdoTail`, which
gained a lead and a unit (`PositionsPerCharacter`, 8 for bits). `CutAt` takes the ceiling of the bits (§15.50.4 rule
9), `KeepUnused` is §13.18.38.4 GR8 a)'s receiving splice (a `CobolBits` window when the first unused occurrence starts
inside a byte), and `RecordUnit` / `RecordMaxUnits` state the table as the record's last component, one CHARACTER per
unit (new determination D-FRA (ix): a record of characters cannot say a bit count). The declared group's codec and a
CELL-BACKED group's window both read it. The cell arm was the sibling: it measured a bit table one character per
occurrence, and `DISPLAY` of an ADDRESS-OF-taken group of this shape threw ArgumentOutOfRange (`q8.cob`).

Golden `2014/pb3030_bit_odo_variable_group` covers LENGTH / BYTE-LENGTH at three counts, DISPLAY, a GR8 a) MOVE into a
declared and a cell-backed group, a nested CALL BY REFERENCE and a WRITE / READ round trip, each value derived by hand.
Unit tests `CobolVarGroupContiguousTests.BitOdoTail_*` cover the tail arithmetic. No edition-gated construct was added.
No inventory verdict changed (`closes_rows: []` with its reason). Found on the way, not fixed: a MOVE into a
CELL-BACKED group whose dynamic-capacity table has variable-length elements loses the table (capacity 0; `q9.cob`).
That is a lead in the report. PB3101 was not started.

**The branch review, answered (wave 1074 A2 finisher, review R1-1).** The bit tail above measured a table from the member
that holds it, and a member that CONTINUES a same-level §8.5.1.6.3 run has no characters of its own: `05 B PIC 1 USAGE
BIT. 05 TB PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON N.` directly in a variable-length group packed B and TB into the
leader's field at the maximum, while the codec cut TB's EMPTY field to the count. Re-probed: `DISPLAY G` threw
ArgumentOutOfRange, at OCCURS 1 TO 8 it showed a NUL character LENGTH did not count, a cell-backed group of the shape
showed the same extra character, and a FILE record of it threw at WRITE. Fix: `GroupImageCodec.VarParts` classifies a
run's leader field by the member the run holds, and `OdoModel.FixedImageTail` counts the run's earlier bits as lead
(`BitLayout.BitsBeforeInRun`, read off the one run law `RunsOf`), so the declared codec, the record layout and the cell
window measure the table from the run's first bit. Golden `2014/pb3030_bit_odo_run_continuation` covers the three arms,
a nine-bit leader, a GROUP-USAGE BIT holder continuing a run, a GR8 a) MOVE and a CALL at the maximum, each value derived
by hand; it fails on the reviewed head. The PB3030 golden's comment cited GR8 b) for a sending group whose data-name-1
lies outside it, which GR8 a) governs; corrected. Gate run `20261014T064005Z-0faf96`: `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN`, both PB3030 goldens Passed in leg 1; semgrep verify PASS.

Finisher (landing attempt 20261014T065404523254Z, `ENQUEUE: GATE RED`): the landing's gate ran
`SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree` in leg 1 and it was red. Review round 2's
per-method census deleted `OoDriver.DeclareMethodWords`, but inventory row SR-12.3.8.3-12 still named it as a
code-location; the branch's own gate had left that case in leg 2 (its `not-run-Unit.txt` lists it), so nothing local saw
it (kb/Work PB3131). The row is re-sited by a `record_verdicts.py` batch to `DataBinder.Oo.cs#OoBindMethodData`, which
now declares the METHOD-ID (or the property-name it names) through `DeclareUserWord` inside the method's own census,
and retires the deleted member with its reason; a sweep of every inventory code-location against the tree finds no
other dangling member, and the branch deleted no other method declaration.
Finisher gate run `20261014T072258Z-315405`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`,
`EveryCodeLocation_ResolvesInTheTree` Passed in leg 1 (the previous red, tier 0a).

Finisher (landing attempt 20261014T075420363360Z, `ENQUEUE: GATE RED` after main was merged in at a0d94a0f2): main's
PB3132 landing (PR #47) had added `negative/pb3132-method-id-spells-its-class-id-and-invokes-it`, a METHOD-ID spelled
like its CLASS-ID in a method whose body also writes `INVOKE PB3132X "NEW"`, and retired PB3132 because the shared
class census refused the METHOD-ID. Review round 2's per-method census (rightly: a method is a source element of its own,
§10.5) made the METHOD-ID legal, and with it the INVOKE: the class-name a method references is declared by its CLASS, so
no declaration of the method's announced it, while §8.3.2.2 says "used". Both intents now hold: `OoNameResolution.Lookup`,
the one class-name and interface-name resolver, records every name it resolves in the referring METHOD's census
(`OoClassTable.RecordReferenceUse`; `UserWordCensus.Record` takes a declaration or a use; `UserWordCensus.ConflictMessage`
is the one COBOLNET2692 wording), so the second of the declaration and the reference is refused, and a method that
never references the class keeps its class-named METHOD-ID (`2002/pb2921_class_named_method` unchanged). New negative
`pb3132-method-paragraph-spells-a-class-it-references` pins the reference-first path (a `USAGE OBJECT REFERENCE` item,
then a same-spelled paragraph) beside a sibling method that declares the word as a data-name and stays legal. A
drift test keeps the two census recorders the only ones. PB3132 reopened and landed with this branch; the nested-program
residue its note names (an inherited configuration name used beside a local declaration of another type) is unchanged.
Finisher gate run `20261014T083522Z-e09c86`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`; both
PB3132 negatives and `TheCensus_IsRecordedOnlyByTheDeclarationAndReferenceFunnels` Passed in leg 1; semgrep verify PASS.

Review round 4 (R4-1, answered by wave 1076 A): the reference record reached only a METHOD census, and its comment
justified that with "a reference outside every method is its own element's declaration already". That is false for a
contained program, whose container's REPOSITORY applies to it (§12.3.4 GR1) without being its declaration (§10.5): the
reviewer's `E:\tmp\r4probe\n1.cob` (a contained program with `USAGE OBJECT REFERENCE RQX` and a paragraph `RQX.`, the
class declared by the container) compiled clean while its flat twin was refused. Re-probed on the merged tree: compiled
clean. The fix makes the census one object per source element for the whole compilation: `OoClassTable.CensusOf(site)`
(a method's is its symbol's; every other element's is kept by parse node), the binders take theirs from it
(`DataBinder.OwnWords`, passed by `BinderDriver.BindUnitData` and `OoDriver`), and `RecordReferenceUse` records into it
whatever element refers, so a reference and a declaration meet whichever runs first. The first build of that shape
refused three parameterized goldens (`pb2051_parameterized_definition_binds_itself` and both `pb2937` skeleton goldens):
the skeleton's binder declares a formal's REPOSITORY specifier as an object-class-name or interface-name, while the
reference recorded it as a parameter-name. The reference now records the type the formal's own specifier declares
(§11.3.3 SR8 / §11.6.3 SR4). A compile sweep of the 633 corpus programs carrying CLASS-ID, INTERFACE-ID or REPOSITORY
draws no COBOLNET2692 the corpus does not pin. After the fix the reviewer's probe is refused at the paragraph, like its flat
twin. New negative `pb3132-contained-program-paragraph-spells-a-class-it-references` (with a sibling contained program
that declares the paragraph and never references the class) and positive `2002/pb3132_contained_program_word_it_never_references`.
A factory or instance definition shows no change: its binder already declares the class's REPOSITORY entries in its own
census (a pre-existing over-refusal, reported as a lead). Also still uncovered and reported as a lead: a contained program
USING a word a containing program's SPECIAL-NAMES passes down (a class condition's class-name) beside a same-spelled
paragraph (`E:\COBOL-coord\scratch\w1076a\s1.cob` compiles clean). The merge with main re-pinned the runtime surface hash
(800009C74EC953FB): both sides had raised the major from 2 to 3 for their own incompatible changes, and no 3.x has shipped,
so one major covers both.
R4-1 gate run `20261014T100113Z-d14226`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`; the new
negative and positive, both earlier PB3132 negatives and the census and runtime-pin drift tests Passed in leg 1; semgrep
verify PASS.

**Finisher: the landing's Linux gate (two reds, both this branch's).** `enqueue.py land` stopped on 739b86210 with the
Linux unit and conformance legs red. (1) `NulByteDriftTests.NoTrackedTextFileContainsANulByte`: the golden
`2014/pb546_odo_national_tail.out` pinned the raw UTF-16BE bytes of `N"X"` and `N"Y"` (00 58, 00 59), two NULs, so every
grep over the corpus read it as binary. The program now stores `NX"5831"` and `NX"5932"`, characters whose two bytes are
both printable, so DISPLAY, the GR9 group MOVE and the record round trip still pin every byte of the image, as text. The
drift test's hint `grep -c -P` counted 0 on a file holding a NUL (grep treats it as binary); it now says `grep -c -a -P`.
(2) `IntrinsicFunctionDifferentialTests.StagedLoudResidue_FailsLoud_NeverWrong` pinned as a loud stage the very shape
PB3030 closed (a runtime-length table beside a bit OCCURS DEPENDING table, now 0003 as the golden
`2014/pb3030_bit_odo_variable_group` pins): a green test holding a closed gap open. It now proves the doctrine on the
residue that is left, the ALIGNED OCCURS DEPENDING table of a GROUP-USAGE BIT group under LENGTH (§15.50.4 r1 counts
boolean positions; PB2498). Neither red was in the Windows gate's leg 1: the NUL sweep is a Sweep class too dear for tier
0c, and with no impact map the residue test was a leg-2 case; the landing's Linux whole-population legs caught both.

