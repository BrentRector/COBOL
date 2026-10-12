## Entry 1278 — 2026-08-09 17:09 PDT — PB59 family 5b: ANY reads the storage it always claimed to, and the padding rules turn out to already be half-built

Family 5 closes. The ANY source-format now delivers what §15.19.3 r7 licenses — the item's RAW STORAGE
bits — through ONE new storage channel, `OperandText.AsStorageImage` (char==byte), where each leaf
shape renders the representation the conformance items already document: a COMP item its radix-2 bytes
(`CONVERT(C ANY ANUM HEX)` of 258 = "0102", where the old display channel would have hexed the DIGITS),
a zoned item its stored characters, a USAGE BIT item its packed bits, a national item its UTF-16BE
bytes through the ONE `CobolBits.NatBytes` serializer the runtime NAT arm now also rides, a group its
`AsImage()`; float/COMP-5 leaves have no defined image (IsImageCapable) and stage LOUD. The bind-time
ANY→NAT remap and `IsNationalOperand` are deleted — the node carries the WRITTEN source again, and
`Convert_AnyOverNational`'s derivation was rewritten before its green run was allowed to count.

The satisfying discovery: §15.19.4 r2's zero-bit padding needed NO new arithmetic. `CobolBits.Pack`
already zero-fills the trailing partial byte high-order-first, so a sub-byte ANY source arrives with
r2's pad materialized — B"101" → 0xA0 → "A0", exactly the inventory's own derivation — and r4's 16-bit
pad reduces to an odd-byte zero append in the dstHex leg: `CONVERT("A" ANUM NAT HEX)` = "4100" (the
map's predicted golden, per 1277's corrected derivation), `B"101" ANY NAT HEX` = "A000". The mechanism
map's bit-count companion parameter proved unnecessary — the packed byte image carries the information
— so the runtime signature is UNCHANGED. NOTE 3's b and c examples are both ruled DEFECTIVE against the
rules they illustrate (E0 underivable; X"41" is the one-character string "A", not the digits "41") —
RV-15.19.4-5's inventory note carries the ruling. RV-15.19.4-2(c) — the '?'+EC on a >0xFF unit — is
DECIDED as the documented item-209 disposition (the r1/r3-analogous substitution under the partial
1-byte serialization; CONFORMANCE.md item-33 row tail), never silent under checking.

Evidence: the six-probe CLI battery (AN=4100 · BA=A0 · BN=A000 · NA=0041 · CA=0102 · ZA=303035) before
the goldens; corpus `intrinsics_convert` grows to eleven rows; Convert_Formats_2023 to eleven; wave-
local Conformance `~Intrinsic|~Corpus` 715/715, Unit 49/49, the rewritten theories re-run by name
(13/13); SpecTraceability 10/10 after four flips — AR-15.19.3-7, RV-15.19.4-2, RV-15.19.4-4,
RV-15.19.4-5 → CONFORMS. GAP 4163 → 4159. PB59 stands at 26 of 28; open: family 7 (the ALPHABETIC
result rider + OperandCategory totalization) and the BASECONVERT screen set on the landed StaticUsageOf
axis. A comprehensive battery is owed for the accumulated 5a+5b batch before the next merge-grade
checkpoint; the next session starts there or at family 7 per §0.

