## Entry 418 — S4: typed numeric field→field MOVE for all long/decimal combinations (retires the 417 guard)

Completes the decimal numeric vertical slice. Replaced 417's loud guard on a decimal-involved typed field→field
MOVE with `EmitNumericFieldToFieldViaCodec`: encode the source value into a **destination-shaped** scratch window
(`EncodeNumeric` with the dst pic — applies the dst's sign/scale/high-order-truncation exactly as the byte
`MoveNumeric` does), decode it back, and store (as `decimal`, or narrowed to `long`). One routine covers every
combination — decimal→decimal, long→decimal, decimal→long — byte-identically; the both-`long` case keeps its faster
`% 10^digits` path. Verified the three combos byte-for-byte against the byte path (`1.50`→`0015{`; `42`→`0420{`;
`1.50`→trunc→`00001`). New flip test `NumericFieldMove_AllLongDecimalCombos_ByteIdentical` — 15 flip tests, all
flag-on≡flag-off. Guard **ALL GREEN: 1196 unit / 496 integration / 364 NIST**.

With this, **both numeric representations (`long` + `decimal`) are complete across the full op set** — VALUE / MOVE
(literal + field, all combos) / DISPLAY / COMPARE / class-condition / arithmetic — over DISPLAY/COMP/BINARY usage,
all byte-identical and gated behind `EnableTypedFields`. NEXT: OCCURS → `CobolTable<T>` (the table substrate), then
nested groups, then Stage-4 pointers/OO.

