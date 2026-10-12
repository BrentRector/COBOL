## Entry 1217 â 2026-08-08 00:08 PDT â R10 COMP-5 half: unsigned carriers land â the container range is finally representable, and the instruments caught three of my own mistakes on the way

R10's COMP-5 half is landed: unsigned COMP-5 now emits `ulong` (8-byte container) / `UInt128` (16-byte), the
item owns its full container range (owner decision 2026-08-07), and the HIGHEST-ALGEBRAIC fold values stand â
2^64â1 and 2^128â1 store back into their own items losslessly.

The three ledger findings each turned out exactly as diagnosed, and each demanded a different layer:

- **F74 (WrapBinary bits=128)**: a C# Int128 shift count is masked to 7 bits, so `1 << 128` was modulus 1.
  Every signed 16-byte store collapsed to 0 AND the signed range read as empty â every checked 16-byte store
  reported a size error. The 16-byte tier now returns CONTAINER BITS and the emitted `unchecked((UInt128))`
  cast reinterprets them; `InBinaryRange` answers true for the whole Int128 domain at bitsâ¥128.
- **F73 (the fold literal crash)**: `Decimalize`'s BigInteger arm was LIVE, not defensive â its own doc
  comment asserted "nothing here currently produces one", which is the unverified claim that hid the bug.
  `EmitCore.IntLiteralX` now renders a positive magnitude beyond Int128 as `UInt128.Parse` on a new
  unsigned-wide NumX lane (`U`).
- **F75 (fold/storage disagreement)**: `PicInfo.ClrType` keyed the carrier on digit count alone; it now keys
  on the CONTAINER (`IsUnsignedLongBinary`/`IsUnsignedWideBinary`), and `Narrow` casts by carrier, never by
  digits â the digit-count key is precisely how the disagreement shipped.

**What the change set taught:**

1. **Overloading on Int128/UInt128 is a trap.** An int constant converts implicitly to BOTH (the
   constant-expression conversion chains through uint), so my first cut â same-name Store/TryStore/
   FormatDisplay overloads â made 119 corpus programs fail CS0121 at once. The lane is now picked by NAME
   (`StoreU`/`TryStoreU`/`FormatDisplayU`) from the operand's static carrier. The corpus caught it in one run.
2. **The engine ceiling became reachable.** HIGHEST-ALGEBRAIC of `PIC S9(19) COMP-5` is exactly
   Int128.MaxValue â the first legal COBOL value AT the intermediate's edge (source literals cap at 31
   digits) â so `ADD 1` under ON SIZE ERROR overflowed the ENGINE, wrapped to the container's far end, and
   stored "in range" with no error. `AddChecked`/`SubChecked` join `MulChecked` (same Â§14.7.5 case-5 mapping,
   same no-phrase-stays-unchecked contract). Arithmetic READING a value beyond the intermediate goes through
   `CobolNum.Widen` â the size-error condition, never a silent wrap; Â§14.7.5 item 5 turns out to define
   exactly this shape once the implementor declares intermediate-range checking (CONFORMANCE.md items 123/179,
   both new).
3. **The citation sweep found the inherited-wrong-clause pattern twice more.** The unsigned-magnitude rule is
   Â§14.9.25.4 GR6d2b; "Â§14.9.25 GR8" â real text, wrong clause â sat in CobolNum.cs five times and in
   CONFORMANCE.md item 205. And `audit_annex_a1.py`'s number/element cross-check flagged the MODULE-NAME row
   filed under item 213 (which is USAGE NATIONAL) since 2026-08-05; its true item is 135.
4. **The fold's hand-maintained usage list is deleted** â it now reads `PicInfo.Truncation`, the ONE capacity
   table, and `AlgebraicFoldContainerAgreementTests` pins fold-vs-runtime boundary agreement per container,
   the usage set, the 16-byte bits contract, and the carrier table (`RoundDiv` genericized over `INumber<T>`
   so both lanes round by one kernel). The characterization RuntimeApi guard then made me route the new
   emission through RuntimeApi helpers â the guard doing its job.

Golden `highest_algebraic_comp5_unsigned` (2023) pins both container maxes, the UInt128 round trip, the
genuine 16-byte size-error boundary and the Widen size error. Two new register notes spawned rather than
scope-crept: R12 (the CALL-boundary picture-digit image silently truncates a beyond-picture BinaryCapacity
value â pre-existing, now register-visible) and R13 (adjudicate DISPLAY of a beyond-picture value â
implementor latitude, survey first). Docs: CONFORMANCE.md items 208 extended + 123/179 added (the A.1
register now discharges 13 of 199, audit-verified), COBOLNET_DESIGN Â§1.2/Â§3.2, DESIGN-data-model Â§1.

Gate: Unit 4077/4077 Â· characterization 33/33 Â· wave-local Corpus|Intrinsic|Arithmetic|Move 882/882 with the
new golden green by name. The comprehensive battery is owed at the batch boundary.

