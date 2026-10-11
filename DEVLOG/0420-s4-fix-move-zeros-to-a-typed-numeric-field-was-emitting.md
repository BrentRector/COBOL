## Entry 420 — S4 fix: MOVE ZEROS to a typed numeric field (was emitting invalid IL)

Found by probing with `--typed-fields`: `MOVE ZEROS TO` a typed numeric field threw `InvalidProgramException` at
runtime. The figurative-MOVE cell built a width-long fill *string* for ZEROS/SPACES and stored it — correct for a
typed `string` field, but a string stored into a `long`/`decimal` field is invalid IL (silent mis-emit). Flag-ON-only
(corpus flag-OFF never reaches the typed cell, so the guard stayed green), but `MOVE ZEROS TO` a numeric field is
ubiquitous, so this had to be correct, not just guarded. Fixed: for a typed numeric destination, `MOVE ZEROS` stores
`0L` (long) / `0m` (decimal) — byte-identical, since the byte path zero-fills the digit image which DISPLAYs the same
as a zero-valued long/decimal. SPACE/HIGH-VALUE/LOW-VALUE/QUOTE/NULL into a numeric typed field (a byte-pattern fill
with no native equivalent) now fails with a loud compile-time `NotSupportedException` instead of mis-emitting. The
character-field figurative path is unchanged. New regression flip test
`MoveZeros_ToTypedNumericLongAndDecimal_ByteIdentical` — 17 flip tests, all flag-on≡flag-off. Guard **ALL GREEN: 1196
unit / 498 integration / 364 NIST**.

