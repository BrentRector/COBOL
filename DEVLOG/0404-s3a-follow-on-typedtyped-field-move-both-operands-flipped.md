## Entry 404 — S3a follow-on: typed↔typed field MOVE (both operands flipped) — byte-identical

Widens the S3a typed-field flip with the field→field move cell: `MOVE WS-A TO WS-B` where **both** are flipped
standalone string fields now re-stores the source at the receiver's width (`CobolString.Store` — ISO §14.9.25
space-pad/truncate; a ref copy when widths match) via a typed×typed branch at the top of
`CilDataEmitter.EmitMoveFieldToField`, instead of routing to the byte path (which would hit the
`EmitLocationArgs` typed guard). A **mixed** typed/byte pair still hits that loud guard until the materialize
fallback (§2.5) lands — the next increment. Test: `MOVE WS-A PIC X(5) "HELLO" TO WS-B PIC X(3)` → `"HEL"`,
asserted byte-identical to the flag-off byte path. Guard **ALL GREEN: 1187 unit / 484 integration / 364 NIST**.
NEXT (unchanged): S3b (`01` group → `record struct`) + the typed↔byte materialize fallback.

