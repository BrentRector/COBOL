## PB3156: the extent of an ALIGNED bit table is one rule (BitLayout.TableBits), so FUNCTION LENGTH of its bit group, the group's bit string and its image agree

FUNCTION LENGTH of `01 BG GROUP-USAGE BIT. 05 BA PIC 1 USAGE BIT. 05 TA PIC 1(3) USAGE BIT ALIGNED OCCURS 1 TO 4 DEPENDING ON N.`
was the named stage COBOLNET1756 ("its occurrences are padded to whole bytes, which a boolean-position count cannot state"),
and the re-probe on a fresh build reproduced it (BYTE-LENGTH answered 3). Staging it was the visible half of a missing rule.
§13.18.1.4 GR2 puts every occurrence of an ALIGNED table at the first bit of a byte, so the occurrences sit one STRIDE apart;
§8.5.1.6.3 generates filler only "as needed to advance alignment to a required natural boundary for the next item", and the
last occurrence has no next one. The extent of n occurrences is therefore `stride × (n − 1) + width`, and nothing for none; the
filler in front of the table is an item of its own and stays (LENGTH is 8 / 11 / 19 / 35 positions at 0 / 1 / 2 / 4
occurrences, 19 being what the same table fixed at 2 gives).

**The rule was spelled three times** (`BitLayout.RunBits`, `FileModel.MinExtent`, `SequentialIoEmitter.OdoRecordExceedsMaximum`)
and the OCCURS DEPENDING readers counted `count × stride`, which is the fourth spelling gone wrong. It is `BitLayout.TableBits`
now, read by all three, with `BitLayout.TrailingFillerBits` the padding that `OdoGeometry.TrailingUnits` /
`OdoGroupPlace.TrailingUnits` / `BoundOdoExtent.TrailingWidth` give back once an occurrence exists. The runtime surface gained
two members (additive, `PublicAPI.Unshipped.txt`, no major): `CobolTable.OdoExtentTrimmed` (`fixed + c × stride − (c > 0 ?
trailing : 0)`, the positions channel's extent) and `CobolBits.Strided` (the occurrences' carrier, padded between them). The
stage in `IntrinsicBinder.VariableLengthGroupSum` is deleted; `AlignedBitTableExtentDriftTests` keeps the spelling out of every
other source.

**Siblings found and fixed in the same files (I9).** The record struct's bit carrier laid an ALIGNED table's occurrences one
WIDTH apart instead of one stride (`GroupImageCodec.BitCarrierOf`, `EmitRunMemberFromBits`, `InitialBitCarrierOf`; the Tier-B
window lane and `GroupValueSlicer` already used `StrideBits`). Measured before the fix: `DISPLAY` of a bit group holding
`OCCURS 2 ... ALIGNED` printed 14 positions where its LENGTH is 19; `MOVE` between two ALIGNED-ODO bit groups threw
`ArgumentOutOfRangeException` (the bit channel sliced at `count × stride`, longer than the 20-position carrier); a table VALUE
behind a REDEFINES seeded the wrong bytes; `AsImage()` / `FromImage` of an alphanumeric group holding such a table packed the
occurrences contiguously. All four lanes (struct bit string, struct image, Tier-B seed, EXTERNAL cell) now print / pack
1 0000000 101 00000 011.

**Golden** `2002/pb3156_aligned_odo_bit_table_extent` (values derived in its header from §13.18.1.4 GR1/GR2, §8.5.1.6.3,
§15.50.4 r1/r5/r9, §15.14.4 r4): LENGTH and BYTE-LENGTH at 0 / 1 / 2 / 4 occurrences, the fixed twin, the alphanumeric group's
LENGTH and image, DISPLAY and a group MOVE of the bit group, the seeded VALUE. No negative golden: the change removes a
refusal and adds none (`BoundDeferralDriftTests` forbids pinning the deferral code as a refusal). `IntrinsicFunctionDifferentialTests`
`.StagedLoudResidue_FailsLoud_NeverWrong` pinned this stage as the last function-argument residue; none is left, so it is retired
into `ByteLengthOfARefModArgument_IsTheWindowLength` (its first half, a positive test); the §1.4 doctrine's pin stays
`BoundDeferralDriftTests.GenuineDeferral_StillCompiles_AndAnnounces`. Its old comment cited §8.4.3.3.4 GR6 for "a ref-mod is a
data item"; `cite.py --check` puts that sentence at rule 5, and the comment now says so.

**Left open, reported as a lead:** an ALIGNED item inside a bit group that itself starts MID-byte is aligned relative to the group's
start (`BitLayout.Walk`'s `RoundUpToByte`), not to a byte of the record, so §13.18.1.4 GR1 is not met there (a bit group at bit 2
holding `10 SA PIC 1 BIT. 10 TA PIC 1(5) BIT ALIGNED OCCURS 3` lays TA at bit 10 and answers 31 where the record-aligned layout
is 29), and the BYTE-LENGTH of such a group stages "its fixed extent is negative" and its alphanumeric-group character
channel overshoots (a crash on MOVE). The character-channel arithmetic is unchanged by this landing; the cause is the layout
walk, which needs to know its absolute origin.

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 5,552 · Unit 633 · Characterization 30 (scope leg1) — Conformance 6,113/11,665 · Unit 32,994/33,627 · Characterization 6/36 cases ran (skipped 0) in 1 of 2 leg(s)` (run 20261014T193116Z-148547); the golden's `UnitTestResult` (`CorpusRunnerTests_P6.EnabledProgram_CompilesStrict_AndMatchesOutIfPresent(edition: "2002", name: "pb3156_aligned_odo_bit_table_extent")`) is Passed in `leg-1-Conformance.trx`. `scripts/semgrep/verify.py`: PASS, no count moved. Inventory: no row claimed, none re-verdicted (`closes_rows: []` with its reason).
