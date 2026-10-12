## Entry 1099 — 2026-07-29 01:42 PDT — V59 step 3: the codec, and the width table that could not hold what it claimed

The discriminator says WHICH representation; this step writes it. `CobolNum.Image.cs` adds `FormatImage` /
`ParseImage` beside `FormatDisplay` — one dispatch on `NumProfile.StorageForm`, carrying the bytes in the SAME
Latin-1 string the record framing already uses, so a binary or packed leaf rides the existing image carrier and
no second whole-group mechanism appears.

**BINARY** = two's complement, big-endian, exactly `StorageLength` bytes; an unsigned item holds the absolute
value (§14.9.25.4 GR8). **PACKED** = BCD two digits per byte, trailing sign nibble `0xC`/`0xD`, `0xF` when the
item has no operational sign; **PackedNoSign** drops the nibble entirely. Decoding accepts the foreign readings
too — `0xB` and `0xD` negative, everything else positive — so a file another COBOL system wrote decodes right.
`NumericStorageForm.None` at a byte boundary THROWS: an INDEX item reaching a record image is a compiler
invariant break, and inventing bytes for it is the exact class of defect this codec retires.

**Then the sweep found a sibling, and it was load-bearing.** Rule 4 says every bug is a pattern, so before
writing the encoder I asked what width it would be handed for a wide picture. `PIC S9(31) COMP` answered **8**.
§13.18.60.4 GR4 closes with "Sufficient computer storage shall be allocated by the implementor to contain the
maximum range of values implied by the associated decimal picture character-string" — and 8 bytes cannot hold
10^31, or even a signed 19-digit 10^19−1 (2^63−1 ≈ 9.22×10^18). The ladder `1-2-4-8` was correct through 18
digits and then just stopped, because nothing above 18 had ever needed a width: `IsWide` already stores those
pictures as `Int128`, and `FUNCTION BYTE-LENGTH` was the only reader. Verified by running it —
`FUNCTION BYTE-LENGTH` answered 8 for `PIC S9(19)`, `PIC 9(20)` and `PIC S9(31)` COMP alike, while the value
9,999,999,999,999,999,999 round-tripped correctly through the Int128 storage. So the ladder gains a 16-byte
tier, and the encoder can no longer be handed a width that silently truncates.

The tier is deliberately **sign-independent** — an unsigned 19-digit value would fit 8 bytes, but GR12 pins
SIGNED and UNSIGNED to one width for the fixed-width binary usages and every surveyed compiler follows that, so
the boundary is set by the signed worst case rather than splitting one picture into two widths. Corpus impact:
zero — the only ≥19-digit pictures in the corpus are DISPLAY (`arith_standard`, `ca4_giving_composite`).

**Every expected value in `RecordImageCodecTests` is computed from the pinned form, never from output.** 1234 as
`PIC 9(4) COMP` is `04 D2` because 1234 = 0x04D2; as `PIC S9(4) COMP` negative it is `FB 2E` because
65536−1234 = 0xFB2E; as `PIC S9(4) COMP-3` it is `01 23 4C`. The test that matters most is the COLLISION one: at
3 digits the signed and WITH NO SIGN packed forms are BOTH 2 bytes and lay them out differently (`12 3C` vs
`01 23`), which is the executable statement of why the form — never the width — decides whether a sign nibble is
present. Assertions compare HEX renderings, because a failing raw Latin-1 diff is unreadable.

Three invariants run over the whole (usage × digits × sign) grid: the image is EXACTLY the storage width (the
§15.14.4 r1 / §15.50.4 r3 agreement, at the codec) · every emitted char is ≤ 0xFF (a char above that is a byte
the framing cannot write) · encode/decode round-trips, with an unsigned item round-tripping the MAGNITUDE.

**Not wired yet.** The image codec still calls `FormatDisplay`; step ④ re-bases `ElementaryImageWidth` and sweeps
the 129 `ImageWidth` references, and that is the commit where the on-disk layout actually changes.

**Gates.** Wave-local: greenfield Unit **837/839** (2 skipped = the V59 facts; 675 → 837 as the codec grid lands)
· characterization **33/33** byte-identical, unchanged — nothing emits differently yet. Full Conformance is not
re-run here: no emitted program changes, and the one compiler-visible change (the 16-byte tier) has no corpus
instance. It rides the batch gate.

