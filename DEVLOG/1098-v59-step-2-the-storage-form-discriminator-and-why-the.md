## Entry 1098 — 2026-07-29 00:47 PDT — V59 step 2: the storage-form discriminator, and why the capacity axis could never carry it

Step 1 put the one-width invariant in the tree as a red-but-labelled test. Step 2 gives the compiler the fact it
has been missing all along: **what bytes a numeric item actually occupies.**

The compiler already knew the WIDTH — `PicInfo.StorageWidth` pins BINARY at 1-2-4-8 and PACKED at `Digits/2+1`,
`DataItem.ByteWidth` documents them, `FUNCTION BYTE-LENGTH` reports them. What nothing carried was the FORM those
bytes take, and the reason the gap survived two phases is that the profile *looks* like it has the answer:
`NumericTruncation` sits right there with three members. It cannot serve and never could — it is the CAPACITY
discipline, the SIZE-ERROR boundary, and **USAGE DISPLAY and USAGE BINARY are both `DigitCount`** while occupying
entirely different bytes. Deriving the representation from the discipline is precisely how a `PIC 9(4) COMP`
came to reach a file as the four ASCII bytes `31 32 33 34`.

So `NumProfile` gains a **required** `StorageForm`, and `PicInfo` gains the `Usage → NumericStorageForm` mapping
that fills it. `NumericStorageForm`'s members ARE the §4.2.16 documentation of our implementor choices (Annex A.1
items 205 and 215 make USAGE BINARY's and USAGE PACKED-DECIMAL's "computer storage allocation, alignment and
representation of data" required user-documentation items): `Zoned` one byte per digit position (§13.18.60.4 GR7)
· `Binary` two's complement, big-endian, `StorageLength` bytes (GR4 "a radix of 2 is used", GR6, GR12) · `Packed`
BCD two digits per byte with a trailing sign nibble (GR11 "a radix of 10 … the minimum possible configuration")
· `PackedNoSign` the 2023 WITH NO SIGN form · `None`. Every citation `cite.py --check`-verified.

**Three things the implementation taught that the plan entry did not know.**

**(1) An INDEX item carries a `NumProfile`.** `PicInfo.IndexItem` is `PicCategory.Numeric, Usage.Index`, and
`RecordStructEmitter.EmitProfiles` emits a profile for every non-float numeric — so the mapping had to answer for
a usage that reaches no image at all (§13.18.60.4 GR10: an occurrence-number carrier only SET, SEARCH and
relation conditions may reference). That is what `None` is for, and it is why `None = 0`: an unstated storage form
now fails LOUD in a codec instead of silently claiming to be one byte per digit. A default of `Zoned` would have
been the same substitution bug one level down.

**(2) PACKED-DECIMAL WITH NO SIGN is a different FORM, not a narrower width — and the width cannot discriminate
it.** For 4 digits, signed packs to 3 bytes and WITH NO SIGN to 2; but for 3 digits **both are 2 bytes**
(3 digits + sign nibble = 4 nibbles; 3 digit nibbles + a leading pad = 4). A codec that inferred "has a sign
nibble" from the byte count would read the last digit of an odd-digit unsigned item as a sign. Hence a distinct
member rather than a width test.

**(3) The unsigned packed sign nibble diverges from our own legacy engine, deliberately.**
`PicRuntime.EncodeComp3` writes `0x0C` for a positive value whether or not the item is signed. IBM, Micro Focus
and GnuCOBOL all write `0xF` for an UNSIGNED packed item. Since no greenfield byte path exists yet — that is the
whole of V59 — there is no behaviour to preserve, so the pinned form follows the survey (`0xC`/`0xD` signed,
`0xF` unsigned) rather than the legacy. Big-endian for BINARY is likewise unanimous across those three. Noted
here because step ② will implement the codec against this paragraph.

**Made it fail once.** A drift test that has never been red is a decoration. Dropping `Usage.Binary` from the
Binary arm of `PicInfo.StorageForm` turns `NumericStorageFormDriftTests` red in four places at once — the table
row, the byte-form-vs-pinned-width agreement, the image-capable check, and the emitted profile text. Reverted; the
table is the point. It enumerates EVERY `Usage` member, so a usage added later cannot inherit a representation
nobody chose, and it asserts the cross-check that a byte form is exactly a positive pinned width.

`required` rather than defaulted, deliberately: the four hand-written `NumProfile` sites in the runtime
(`CallAbi` ×2, `CobolSort`, and the `BinaryCapacityTests` helper) now each state their form, and three of them
turn out to be character-image decoders — a SORT key window and the two CALL argument views — which is worth
having written down at the site.

**Gates.** Solution build green. Greenfield Unit **675/677, 2 skipped** (the 2 skips are step 1's V59 facts; the
count moves 580 → 675 because the two new theories are table-driven over all 25 usages). Characterization
**33/33** after a re-bake: the emitted profile gains one field and the diff is exactly that field, 13 snapshot
files, nothing else moved — and every profile in the characterization corpus reads `Zoned`, which is the same
"blast radius: zero" the plan measured, seen from the other side. Full greenfield Conformance re-run because the
change touches every emitted program.

**Friction, logged.** My first `cite.py` call passed `"13.18.60.4 GR4"` as a single argument and got back
`FAIL: there is no clause §13.18.60.4` — which reads exactly like a bad citation when it was a bad invocation
(the flag takes CLAUSE and TEXT as two arguments). I nearly went hunting for the "real" USAGE clause number. The
tool is right to be strict; the lesson is that a citation-checker's failure message should be read as being about
the *invocation* first and the *citation* second.


