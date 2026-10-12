## Entry 425 — S4: OCCURS slice 2 — numeric typed tables (`long[]` / `decimal[]`), now unblocked & byte-identical

With the byte engine fixed (424) to initialize every occurrence from a table VALUE, the numeric-OCCURS typed flip
re-lands cleanly. Re-enabled the Binder OCCURS branch's numeric arm (it now routes the element through the shared
`ClassifyTypedNumeric` core kept in 423): a fixed OCCURS over an unsigned-integer element → `long[]`, over a
signed/scaled element → `decimal[]` (numeric elements still require a VALUE, same defined-init rule as standalone
numerics). No codegen change was needed — slice 1's three generalized value-access primitives already index
generically and the emitter/resolver already handle numeric element kinds; only the Binder gate changed.

Verified byte-for-byte: `_TA_N` emits as `Int64[]`, `_TA_D` as `Decimal[]`; a probe over VALUE-init / MOVE-literal /
ADD / COMPARE / variable subscript / scaled element matches the byte path exactly (`007 042 008 N8 042 01E 02{`). New
flip test `NumericOccursTable_FlipsToTypedLongAndDecimalArray_AcrossOps_ByteIdentical` — 20 flip tests, all
flag-on≡flag-off. Guard **ALL GREEN: 1196 unit / 502 integration / 364 NIST**. OCCURS tables (char + numeric) are now
fully typed. NEXT: PERFORM VARYING / SEARCH over a typed table (slice 3), then nested groups, then Stage-4 pointers/OO.

