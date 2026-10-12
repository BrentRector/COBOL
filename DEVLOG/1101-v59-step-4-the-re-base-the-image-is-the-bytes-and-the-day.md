## Entry 1101 — 2026-07-29 02:00 PDT — V59 step 4: the re-base — the image IS the bytes, and the day BYTES ARE NOT TEXT became a rule

The width, the codec and the discriminator were all in the tree; this is the commit where they meet and the
on-disk layout actually changes. `DataItem.ElementaryImageWidth` now answers `StorageWidth` for a BINARY or
PACKED leaf, `GroupImageCodec` encodes and decodes through `CobolNum.FormatImage`/`ParseImage`, and the
one-width invariant test from step 1 is UN-SKIPPED and green: 85 cases, `ImageWidth == ByteWidth` for every
image-capable item.

**The 129 `ImageWidth` references mostly did not need touching, and that is the architecture working.** Every
width in the compiler single-sources through `ElementaryImageWidth` — `RecordLayout` reads it, `StorageFormPass`
derives `StorageForm.Width` from it, file record sizes and REDEFINES offsets and SORT windows all compose from
it. Changing the leaf answer moved all of them at once. What DID need touching were the sites where "width"
never meant bytes at all.

**BYTES ARE NOT TEXT — the distinction the old design never had to make.** While the image was a zoned digit run,
an item's record image and its DISPLAY text were the same string, so one width and one formatter served both. They
are now different things:

- **`DataItem.DisplayTextWidth`** (new) is the CHARACTER width of an item's DISPLAY/device text — digits plus a
  separate sign. ACCEPT's device window reads it (a `PIC 9(4) COMP` receiver takes FOUR typed digits and stores
  TWO bytes), DISPLAY's fit uses it, and the Report Writer's print columns use it (§13.18.14.4 GR9 — a report line
  is characters). Everything storage-facing keeps `ImageWidth`.
- **`OperandText.NonTextBytes`** decodes a non-zoned STORED image before rendering it as text, because
  §14.9.25.4 GR6 (and §8.8.4.2.2 for a numeric↔nonnumeric comparison) treat an ELEMENTARY numeric operand used as
  text "as though it were moved to an alphanumeric data item" — its DIGITS. It returns null for every zoned item,
  which keeps two things: existing behaviour byte-identical, and the incompatible content a group MOVE can
  legitimately deposit (spaces in a numeric leaf) passing through instead of being silently reformatted to zeros.
  A GROUP operand is the opposite case and was already right: §8.8.4.1.1 makes it alphanumeric over the items'
  REPRESENTATION, so its text IS the record image, bytes and all.

**Two mechanisms died, which is the real prize.** `PicInfo.ImageSignKind` is DELETED — it existed only to give a
binary leaf a fixed-width trailing-overpunch sign inside a zoned window, and a two's-complement byte carries its
own sign. With it goes the Tier-B classifier's `leaf.Pic = bp with { SignKind = bp.ImageSignKind }` rewrite, so an
image-stored COMP leaf now keeps its declared `BinaryMinus` and DISPLAY of it still prints `-100`. And the SORT key
descriptor stopped carrying `(Signed, SignKind)` re-derived from the window width; it carries the KEY ITEM, so
`CobolSort.NumericKey` decodes through the leaf's own profile. That one had teeth: a signed COMP key's first byte
is `0xFF`, so a text decode would have sorted every negative LAST.

**The gate found exactly two reds, and both were tests of the retired design.** `MixedUsageRecordImageDifferential`
asserted `"000PAB"` for a group whose COMP leaf holds −7; it now asserts the leaf occupies TWO positions with "AB"
at 3-4 and `FUNCTION LENGTH` = 4 — the same defence against a shifting leaf, stated in the current representation.
The non-aligned group MOVE now reads `FF 85` positionally into two one-byte receivers as −1 and −123. Both expected
values were re-derived from the pinned form, not observed.

**Two goldens, and a preference worth stating.** `v59_byte_image` is the conforming-program proof: `BYTE-LENGTH(G)`
and `LENGTH(G)` both answer 5 (they said 5 and 8 before), and `FUNCTION ORD` over an alphanumeric REDEFINES reads
the actual bytes — `005 211` for `1234` in `PIC 9(4) COMP` (0x04D2) and `002 036 080` for the same value packed
(0x01 0x23 0x4F, the unsigned `0xF` nibble). `v59_sort_binary_key` sorts −50, −3, 0, 7, 1000 through a signed COMP
key. I wrote both to read a byte at a time rather than dumping raw bytes: an expected file full of Latin-1
control characters is unreviewable, and a golden nobody can read is a golden nobody will maintain.

**Two unrelated gaps surfaced while writing them, and both are QUEUED rather than absorbed** (DA2, DA3). A
FUNCTION operand of DISPLAY (`DISPLAY FUNCTION ORD(C)`) throws "computed expression in a string context" at run
time — legal source per §14.9.11.2 Format 1 + §8.4.3.1.2, hidden until now because a compile-time-foldable
intrinsic like BYTE-LENGTH prints fine. And a HEX literal as a comparison operand (`IF G = X"FFF94142"`) throws
"comparison operand" — legal per §8.3.3.2, hidden because hex literals DO work in VALUE and ALPHABET positions
(DA1 landed that decode). Each golden routes around its gap in ordinary COBOL and says in a comment that it is
doing so, so neither disappears into a green suite.

**Migration, stated plainly because a user must not discover it from wrong output:** this changes the ON-DISK
RECORD LAYOUT. A data file written by an earlier build carries a BINARY/PACKED field as zoned digits and this
build reads that window as bytes; a fixed-length sequential file carries no self-description, so it would present
as silent garbage. The provable case — a fixed-length file whose byte length is not a multiple of its record
length — becomes a diagnostic in the next step. The compensation is real: these forms are what IBM, Micro Focus
and GnuCOBOL write, so files interchange where they previously could not.

**Gates — the comprehensive battery, because this changes every emitted program and the on-disk layout.**
FULL greenfield Conformance **4115/4115** (4113 + the two new goldens), nothing red · greenfield Unit **904/904,
ZERO skipped** — the step-1 invariant is un-skipped and green · characterization **33/33** after a re-baked emit
diff that is exactly `FormatDisplay` -> `FormatImage` at the storage sites, 10 lines over 4 files ·
`guard-fast.sh` **ALL GREEN**, NIST **353 MATCH / 0 REGRESSION** including the whole ST mixed-usage series ·
GnuCOBOL differential **2 flips, both FIXES, zero regressions** — and both are in `data_packed.at`
('PACKED-DECIMAL numeric test (1)' and '(2)'), which is the external oracle confirming that the pinned byte forms
are the ones the rest of the world writes. New differential baseline: 474 / 173 / 572 / 104 over 1323 cases.

