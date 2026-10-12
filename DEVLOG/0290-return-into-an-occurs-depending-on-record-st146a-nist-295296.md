## Entry 290 — RETURN INTO an OCCURS DEPENDING ON record → ST146A (NIST 295→296)

ST146A is a procedural SORT whose records carry an OCCURS DEPENDING ON table; it RELEASEs records with the
table populated (1–9 elements) and `RETURN ST-FR1 INTO ODO-RECORD`, checking the elements round-trip. It
returned truncated tables ("9 ACTIVE: 123" instead of "…123456789"). The `RETURN … INTO` move
(`IrMoveFieldToField(SD-record → ODO-RECORD)`) sized the destination by the ODO group's depending item — but
that item lives INSIDE the record being moved, so its STALE (pre-move) value sized the copy: too few table
elements were copied, then the count field was overwritten to 9. Fix: resolve the INTO destination with
`receiving: true` so the move uses the group's MAXIMUM length (the same format-3 ODO treatment as the
variable-record READ path, DEVLOG 257) — both the AT-END-phrase and no-phrase INTO moves in `LowerReturn`.
ST146A → 4/4, baselined. The change only affects RETURN INTO an ODO group (a fixed group's max == declared
length, unchanged). Guard ALL GREEN: 1000 unit / 347 integration / **296 NIST** (… + 26 ST), 0 regressions.

