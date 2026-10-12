## Entry 1953 — 2026-10-10 21:55 PDT — Train 1056: class run-unit keys, dynamic tables of bits, the SDIDI operand lift, exact ref-mod positions, X"FF" as the storage byte

Train 1056 carries five wave-1056 clusters in one landing (B, F, C, E, D), brought in by `train_apply.py` in 9.4 s
with no conflict. Every implementer gated leg 1 only (batched gating, PB2515), so the lander's whole-population gate
was each cluster's first population check, and it found three reds owned by two clusters, all fixed in the train.

**B — PB2920: a class's run-unit keys rooted at the name it is externalized by.** Two separately compiled modules
that each declared `CLASS-ID. ACCT` (one `AS "LEDGER-ACCT"`) shared one factory file connector (`ACCT::FACT::FF`):
module A's CLOSE returned 42. And the EXTERNAL describer identity was the C# name, so the second module's class was
skipped as "self". §8.3.2.2 2) makes the externalized name the one that identifies an entity in a run unit.
`ProgramTable.ClassPath` / `ClassHalfPath` now form the root once, `"/" + OutermostPath(externalized name)` (a leading
`/` no program path can begin with). `OoClassSymbol.RunUnitPath` / `HalfPath` call them, and
`BinderDriver.QualifyClassFiles` and `OoEmitter.EmitTypeHalf` use them for factory, instance and SORT keys and for the
describer identity. Tests: `ModuleRegistrationHostTests` (both arms, each failing before the fix) and
`ModuleRegistrationTests`.

**F — PB2818: a dynamic-capacity table of USAGE BIT elements is out-of-line storage.** At `--std 2014` such a table
was packed into its group's bit run (`CobolBits.Pack(A + T, 2)`), which is CS0029. §8.5.1.9.1 makes the table's
storage implementor-defined and not contiguous, so the table shares no byte either way (§8.5.1.6.3 "all other bit
data items"). Each occurrence now takes ceil(n/8) characters with its positions packed (§15.14.4 r4), and the table
counts as one element for §8.5.1.12.2/3 (CONFORMANCE DOC-A.1-61). `BitLayout` (RunMember, SharesByteWith, RunBits)
and the `GroupImageCodec` bit lane changed, and `VariableLengthCompatibility.Unplaced` is deleted. The implementer
found and fixed the sibling arm: `Place.For`'s bit origin used the parent-scope reservation for a dynamic table where
the byte origin used 0, so a bit table beside other members of an EXTERNAL/BASED group lost every store. Goldens
2014/pb2818_dyn_bit_table and negative pb2818-dyn-bit-table-below-2014. The PB2691 negative is re-worded (it still
refuses, now by §8.5.1.12.2 on D's byte).

**C — PB2918: the SDIDI operand lift rounds a wide container.** Under STANDARD-DECIMAL, `COMPUTE R = S + 1` with
S = -2^127 in `PIC S9(19) COMP-5` took ON SIZE ERROR. `CobolDec.From` put the 39-digit significand into the
intermediate unrounded, and `ToUnscaledCore` counted digits (`> 38`) and took `Int128.Abs(MinValue)`. §8.8.1.5.2 r1
rounds an operand that cannot be expressed exactly in an SDIDI. `NumericRenderer.DecOperand` now sends such an operand
through `CobolDec.FromParsed` (or the new `FromUnsigned`), and the landing asks `CobolNum.WideningFits`. Goldens
2023/pb2918_sdidi_lift_of_wide_binary_container, pb2918_sdidi_lift_prohibited and
pb2918_native_dec_lane_wide_binary_operand; `CobolDecWideLiftTests`, `SdidiOperandLiftDriftTests`; NUMERIC D27.
**Lander fix in the train:** the gate turned 2023/pb156_float_literal_exact_native's LANE leg EQ (reproduced on C's own
build, NE on F's). The rounding lift also caught a NATIVE operand of known 35 to 38 digits (`A18 * B18`), which the
native Dec lane carries exactly (`CobolDec.AddSigned`'s 38-digit scratch, PB1510) and a native relation compares
exactly (DOC-A.1-179 (3)). Pre-rounding it rounded the value twice. `NumericRenderer.ExactLiftDigits` is now 34
under STANDARD-DECIMAL and 38 otherwise, so in a native statement only a container's 39th digit or an unknown bound
rounds at the lift. D27 and DOC-A.1-179 (3) say so. The pb156 LANE/LANEF legs are the regression witness.

**E — PB2938: EC-BOUND-REF-MOD names the position the program holds.** The detail printed the `int` clamp
(`reference modification (2147483647:1)`) for an operand past `int`. The ref-mod chain now carries PB2695's
`Occurrence` end to end: `RefModPosition`, `RefMod` / `SpliceInto` / `RefModStartOffset`, one `RefModViolation` in
place of three message copies, and `Int128` expression intake. An integer literal or constant-name past `long` is a
`PositionWideConstant` (`ReferenceResolver.HostPosition`), which also fixes the subscript-literal arm and the
compile-time COBOLNET2670/2671 text. §8.4.3.3.4 5) and §14.6.13.1.3 8) were re-checked with cite.py. Golden
2002/pb2938_wide_refmod_position_and_length; `WideRefModDiagnosticTests` (33) and `OccurrenceTests`. The runtime hash
was re-pinned (major stays 2).

**D — PB2919: byte 0xFF has one character.** A binary item's byte 0xFF read as U+FFFF through an alphanumeric
REDEFINES while the literal `X"FF"` was U+00FF, so `IF GX = X"FFFFFFFE"` was false after `MOVE -2 TO S`. The
storage-byte law cannot move (PB1759 step 0, R52), so the literal moves. By §8.3.3.2.4 4) each character "has the bit
configuration specified by one occurrence of hex-character-sequence-1" (cite.py OK), and IBM, Micro Focus and GnuCOBOL
agree that X"FF" is HIGH-VALUE. `CobolLiteral.DecodeHexGroups` now images pairs through `StorageByte.ToChar`. Two
private copies of the mapping were folded in: `CopyProcessor.DecodeHexadecimal` (deleted; `DecodeHexDigits`) and
`OptionsBinder.OneByteHexAlphanumeric`. Golden 2002/pb2919_hex_literal_ff_is_the_storage_byte, six goldens re-derived,
and `StorageByteLawDriftTests`. **Owner awareness:** this reverses the consequence sentence R52 and DOC-A.1-31 carried
("the literal X"FF" stays U+00FF"). The DOC-A.1-95 example `ORD(X"80FF0041")` is now 129 65536 1 66, and R52 gains a
"consequence revised" paragraph. **Lander fixes in the train:** SR-7.2.3.3-5's code-location named the deleted
`DecodeHexadecimal` (retired by batch; the successor is named), and the DOC-A.1-95 row named the token DOC-A.1-31
outside a row key (now "A.1 item 31"). The notes of GR-8.3.3.2.4-4, GR-8.3.3.2.4-5 and DOC-A.1-95 were restated for
X"FF" = U+FFFF, and the new golden was added as a witness. Verdicts are unchanged.

**The train.** No cluster claimed a diagnostic code (B 3452-3454, C 3455-3457, D 3458-3460, E 3461-3463 and F
3464-3466 are all returned). Gate 1 (Windows run 20261011T034200Z-ea6659; Linux on 22c70283d) was RED: C owned
pb156_float_literal_exact_native and D owned `AnnexA1RegisterDriftTests.EveryDocRow_IsFiledUnderTheItemAnnexA1Names`
and `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree`, attributed in about 10 minutes from the
Linux red verdict. Two Unit harness non-observations (`GateSlotDriftTests`, `GateLegDriftTests`: subprocess timeouts)
and the `test_orchestrate_startup.ps1` self-test were host load. Each fix was folded into its cluster's own commit.
Gate 2 on 6994ce119 (run 20261011T042135Z-46a50c): Conformance 11,408/11,408, Unit 33,306/33,306 and Characterization
36/36 EXACT with no failed case. The verdict line read RED only because six python and PowerShell self-tests hit
subprocess timeouts under host load (`landing_lease.py`, `gate_slot.py`, `test_alloc.py` and three hook tests). The
train touches no script, and each one re-run alone was GREEN. Linux gate GREEN on 6994ce119. Review: 3 findings (the
three gate reds), 3 fixed in the train, 0 clusters dropped. Arch-oracle: 180 of 7723 cases differ from 69041f6c6683,
plus 7 ADDED goldens, every one attributed by class. E: 131 ref-mod sites lose the `CobolNum.Position32(...)`
narrowing. C: 21 operands of unknown or wide bound (NIST IF*, the standard-decimal goldens) lift through
`CobolDec.FromParsed`. D: 12 hex literals containing FF now image U+FFFF, plus the re-derived goldens. B: 10 class keys
rooted at `/<externalized name>` (`::FACT::`, `::INST::`, `::SORT::`, `/OBJECT` describers). F: 1, the pb2691
negative's message. Re-recorded as 6994ce119e40. Semgrep 44/2/268/3 before and after. GAP 103 → 103. Leads filed:
PB2964 (F: LENGTH of a bit-bearing group with a runtime-length subordinate is a named stage), PB2965 (C:
STANDARD-DECIMAL exact-family functions keep the Int128 arm; needs an owner call on D3's residue), PB2966 (C: no
unsigned-wide SDIDI landing) and PB2967 (C: a binary64 result saturates silently into a 16-byte COMP-5 receiver).
PB2968 is unused.

