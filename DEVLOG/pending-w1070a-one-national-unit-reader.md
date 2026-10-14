## PB546: one reader for the bytes a national position occupies (w1070 A)

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

**The branch review, answered (wave 1072 A2).** The read-only review of the branch at `18d11007f` found one thing
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
phrase reached Roslyn and failed it (CS0131). The pick is now screened, COBOLNET2746. The other callers return the
picked item to code that judges it like any other. Regression tests (the negative corpus runs strict only, so these
are `CompileFull(…, permissive: true)` tests): `OccursDynamicGuardTests.Permissive_AmbiguousCapacityRegister_*` (two
under-a-table shapes, plus the legal first-declared copy that runs and reads 3) and
`IndexNameUniquenessTests.Permissive_AmbiguousIndexName_ScreensTheLinkageRecordItResolvesTo`. Merging main brought
in PB3107, which commits no arch-oracle baseline, so the baseline the first review finisher re-recorded is deleted in
the merge; the four difference classes above stand as that comparison's record.
