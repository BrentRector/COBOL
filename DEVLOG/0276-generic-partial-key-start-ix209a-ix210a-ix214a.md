## Entry 276 — Generic/partial-key START → IX209A/IX210A/IX214A

IX209A/210A/214A `START IX-FS1 KEY IS … IX-FS1-KEY-1-5` — the KEY operand is a *generic key*: a data item at
a key's leftmost byte that is shorter than the key, naming the leftmost portion to position on (ISO §14.9.41,
"data item … whose leftmost character position corresponds to that of a record key"). CBL1603 rejected it
(neither the prime key name nor an alternate-key name), and even if it compiled the runtime compared the full
key against the short operand. These tests use prefixes of BOTH the prime key (`IX-FS1-KEY-1-5/1-10`) and the
alternate keys (`IX-FS1-ALTKEY1-1-5`, `IX-FS1-ALTKEY2-1-5`), so each prefix must map to the key it prefixes.

- **`SemanticModel.ResolveKeyOfReference(file, operand)`** — the shared resolver: returns -1 (prime), 0+
  (alternate index), or null (not a key operand). Accepts a direct key by name, or a *generic prefix* — a
  data item whose storage offset equals a key's and whose length is ≤ it (`IsLeftmostPrefix`, using the
  offsets that `StorageLayoutComputer` has already assigned).
- **Validation** — `BoundTreeValidator` now receives the `SemanticModel` (scoped static field; the binder
  validates one program at a time) and CBL1603 accepts the operand iff `ResolveKeyOfReference` is non-null,
  so a generic prefix of any key passes.
- **Compiler** — `LowerStart` resolves the operand's `DataSymbol` directly (was re-resolving the prime key by
  name) and sets the START's `KeyIndex = ResolveKeyOfReference(...)`, so a prefix of an alternate key
  positions on, and sequences by, that alternate.
- **Runtime** — `IndexedFileHandler.Start` compares the key truncated to the search value's length
  (`r.Substring(0, targetKey.Length)`), so a shorter generic operand matches the key's leading bytes; a
  full-key START is unchanged (lengths equal).

IX209A/210A/214A → CLEAN, baselined. **IX215A is NOT covered** — it is a deeper REDEFINES/qualification test:
START operands that REDEFINE the key (`R-REDF-RECKEY-1-7 REDEFINES R-RECKEY-1-7`) and qualified key names with
three identically-named keys (`RECORD KEY IS IX-FD3-KEY IN IX-FD3-RECKEY-AREA` + two alternates also named
`IX-FD3-KEY`), which need qualified-name key matching beyond a leftmost-offset prefix. Guard ALL GREEN: 1000
unit / 347 integration / **260 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 30 IX), 0 regressions.

