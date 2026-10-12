## Entry 281 — Qualified/REDEFINES key resolution + DELETE-by-key + duplicate-key arrival order → IX215A (NIST 268→269; IX 39/42)

IX215A — the last actionable IX test — exercises three indexed-file features against three files with
deliberately awkward key declarations: IX-FD1/FD2 keys with REDEFINES-of-key START operands, and **IX-FD3
with THREE keys all named `IX-FD3-KEY`** distinguished only by qualification (`IX-FD3-KEY IN
IX-FD3-RECKEY-AREA`, `… IN IX-FD3-ALTKEY1-AREA`, `… IN IX-FD3-ALTKEY2-AREA`). It went from 9 CBL1603
compile errors → 33/33, via three fixes, each rooted in the spec.

**(1) Qualified-name key resolution (compile — 9× CBL1603 → 0).** `FileSymbol.RecordKey` /
`AlternateKeyInfo.DataName` stored `dataReference().GetText()` — the *concatenated* qualified text
("IX-FD3-KEYINIX-FD3-RECKEY-AREA"), unresolvable by `ResolveData` and identical across the three keys. Now
the **base data-name + its OF/IN qualifiers** are stored separately (`RecordKeyQualifiers`,
`AlternateKeyInfo.Qualifiers`, filled by a new `ExtractQualifiers`). `SemanticModel.ResolveQualifiedData`
(outermost-first walk, mirrors the expression binder) + `ResolveKeyData(file, keyIndex)` resolve each key to
its own DataSymbol. `ResolveKeyOfReference` was rewritten to be **purely position-based** (ISO §14.9.41 — a
START/READ operand identifies a key by beginning at the key's leftmost byte and being no longer than it):
compare storage *locations* (Area + Offset + Length ≤), not names — which is what lets the three
identically-named qualified keys, a REDEFINES-of-key, and a leftmost subfield all resolve correctly. (Added
an Area check the old name+offset code lacked.) Binder runtime key-offset registration and the START/READ
lowering now route through `ResolveKeyData` too, so the right record bytes are used at runtime; the dead
name-matching `ResolveStartKeyIndex` was removed.

**(2) RANDOM/DYNAMIC INDEXED DELETE was deleting the wrong record (6 REC-KEY + 3 QUAL fails).** `Delete()`
used the stale `_currentKey` (last START/READ position). A keyed READ passes its key and a REWRITE carries
it in the record content, but a DELETE writes no record — so the prime RECORD KEY in the record-key data
item was never conveyed (ISO §14.9.10 GR). Added `IrSetIndexedKey` (mirrors `IrSetRelativeKey`): `LowerDelete`
emits it before the delete for random/dynamic INDEXED, `FileRuntime.SetIndexedKey` →
`IndexedFileHandler.SetPendingKey` sets the prime key the next DELETE removes. (The 9 "WRONG RECORD NUMBER"
fails were DELETE-then-START-EQUAL-on-the-deleted-key tests expecting INVALID KEY; the stale-key DELETE left
the record present, so START found it.)

**(3) Duplicate alternate-key retrieval order (2 ALT-KEY-2 fails).** A START/READ on an alternate key with
DUPLICATES, then READ NEXT, must return the duplicates in **arrival order** — the order records were released
to that duplicate set by WRITE, or by a REWRITE that *created* the duplicate value (ISO §14.9.30 GR26 /
§14.9.35 / §14.9.41 / §14.9.30 GR32); a REWRITE that changes an alternate key re-positions the record LAST.
The handler ordered duplicates by prime key. Added a per-record `_arrival` sequence (seeded on OPEN in load
order — so never-rewritten duplicates are unchanged — assigned on WRITE, bumped on a REWRITE that changes an
alternate key, dropped on DELETE); START / READ NEXT / keyed READ now break reference-key ties by `_arrival`,
not prime key, and track `_currentArrival` as part of the READ NEXT position. (GF-09: record 176 holds the
alt-key-2 value from the load; record 4 — smaller prime — is rewritten to it; the test expects 176 then 4,
i.e. arrival not prime order.)

No persistence change was needed: each test that shares the file re-creates it in its own format, and the
duplicate sets in play are always (loaded record + same-session rewrite), so an in-memory arrival sequence
suffices. Guard ALL GREEN: 1000 unit / 347 integration / **269 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ
+ 19 RL + **39 IX**), 0 regressions. **IX 39/42**; remaining: IX110A (order-fragile — under evaluation),
IX301M/IX401M (flagging-conformance modules — no CCVS report, excluded by design like IF401M/SQ303M).

