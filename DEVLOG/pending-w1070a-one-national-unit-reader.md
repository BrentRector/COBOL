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

**The branch review's one finding (w1071 B).** Review R1-1: `FactoryClassOf` had been inserted between `OoUnivStop` and its
§14.9.23.4 GR7c doc comment, so `FactoryClassOf` carried two `<summary>` blocks and `OoUnivStop` none. The method now sits
above the comment, which documents `OoUnivStop` again. A sweep for doc comments that open `<summary>` twice found 58 more
on main, none from this branch; that tree-wide class is left for its own change.
