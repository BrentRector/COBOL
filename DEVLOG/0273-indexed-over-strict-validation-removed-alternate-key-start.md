## Entry 273 — Indexed over-strict validation removed: alternate-key START/READ + EXTEND access-mode (compile-unblock, runtime tail)

Three `BoundTreeValidator` checks were stricter than ISO and rejected conformant indexed programs at compile
time — the same over-strictness pattern as the file-I/O wall (DEVLOG 237+). Fixed (no new baselines yet; the
unblocked tests have an alternate-key *runtime* tail — see below):

- **CBL1603 (START KEY) and CBL1703 (READ KEY)** accepted only the prime record key. ISO §14.9.41 / §14.9.30
  allow the KEY operand to name the prime key **or any alternate record key**. Added
  `IsRecordOrAlternateKey(file, name)` (prime ∪ AlternateKeys) and routed both checks through it.
- **CBL0701 (OPEN EXTEND)** rejected any non-sequential *organization*. ISO §14.9.30 GR2 ties EXTEND to
  *sequential access mode*, and GR15 explicitly defines EXTEND for relative and indexed files (position after
  the highest prime key). Changed the check to the access mode, so EXTEND is valid on a sequential-access
  indexed/relative file and rejected only for RANDOM/DYNAMIC.
- **Indexed EXTEND runtime** added to `IndexedFileHandler.Open`: EXTEND now loads the existing records (it
  previously didn't, so Close would overwrite the file with only the appended records) and seeds
  `_lastWrittenKey` with the highest existing key so the ascending-order WRITE check continues correctly;
  EXTEND on a missing optional file creates it (05), on a missing non-optional file is 35.

This moves IX205A/206A/207A/212A/213A/216A/217A from COMPILE_FAIL to FAIL* (they now compile). They are not
yet baselineable: the failures are the alternate-**key-of-reference** runtime (after a START/READ on an
alternate key, READ NEXT must continue in *alternate*-key order and walk duplicate alternate keys — the
handler currently always sequences by prime key), plus generic/partial-key START (`KEY IS … IX-FS1-KEY-1-5`,
a leftmost key prefix — IX209A/210A/214A/215A still COMPILE_FAIL on CBL1603 for the prefix operand) and
variable-length indexed records (IX105A). Guard ALL GREEN: 1000 unit / 347 integration / **253 NIST**
(unchanged count — validation-only), 0 regressions. Next IX chunk: the alternate-key-of-reference subsystem.

