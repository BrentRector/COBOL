## Entry 274 — Alternate-key-of-reference runtime → IX212A/IX213A (key of reference, _records-derived alt keys)

Built the indexed alternate-key-of-reference subsystem the user asked for. A START or a keyed READ may name
the prime record key OR an alternate record key (ISO §14.9.41/§14.9.30); the chosen key becomes the *key of
reference*, and a subsequent sequential READ NEXT walks records in that key's order (ascending alternate
value, then prime key for records sharing an alternate value), including duplicate alternate keys.

Compiler — the key index is now derived from the operand, not assumed to be the prime key:
- `IrStartFile` / `IrReadByKey` carry a `KeyIndex` (-1 prime, 0+ alternate). `LowerStart` reads the START
  KEY operand (was always `File.RecordKey` — wrong + it extracted the search value from the prime key item
  instead of the operand's); `LowerRead` uses `read.KeyDataName`. Both resolve the index via new
  `ResolveStartKeyIndex` (prime ∪ AlternateKeys). Threaded `keyIndex` through `StartFile`/`ReadByKey` →
  `CobolFileManager` → `IFileHandler.Start`/`ReadByKey` (relative/sequential ignore it).

Runtime (`IndexedFileHandler`):
- `_keyOfReference` set by START and keyed READ; READ NEXT orders by `(KeyForReference(rec), prime)` tuples,
  re-derived from `_records` each call. START positions to the first record satisfying the relation in
  key-of-reference order; READ … KEY IS alt-key returns the prime-smallest matching record.
- **All alternate-key views are now derived from `_records`** (the prior clone-based `_alternateIndices`
  went stale after a REWRITE/DELETE): `CountByAlternate` backs the WRITE duplicate check (22 / 02 via
  `HasDuplicateAlternateKey`), the new REWRITE alternate-key uniqueness check (22, ISO §14.9.35), and the
  alternate-key READ lookup. `_alternateIndices`/`IndexAlternateKeys` removed.
- **Regression caught + fixed by the full guard:** the first cut computed the current position's reference
  value by re-extracting from `_records[_currentKey]`, which fails after a sequential DELETE removes that
  record — the scan then restarted from the file's start, corrupting IX103A/IX203A's delete-and-count. Added
  `_currentRefKey`, the cached reference value of the last-returned record, so the position survives the
  record's deletion.

IX212A (13→0) and IX213A (16→0) baselined. IX205A/206A reduced to 1 FAIL* each (blocked only by SAME RECORD
AREA — a separate record-storage-sharing feature, not alternate keys), IX207A to 4 (duplicate-alternate-key
read sequencing/02-status timing — a deeper nuance). Guard ALL GREEN: 1000 unit / 347 integration / **255
NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 25 IX), 0 regressions.

