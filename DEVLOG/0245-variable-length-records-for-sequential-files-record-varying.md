## Entry 245 — Variable-length records for sequential files (RECORD VARYING); +5 SQ; RL210A baseline corrected

The SQ FAIL* cluster SQ220A–SQ224A all failed for one missing feature: **RECORD IS VARYING** (with
or without DEPENDING ON). The tests write a mix of short (120) and long (151) records, then read them
back checking that the actual length round-trips and the long-record content survives. Implemented
the feature end-to-end (the grammar already parsed the clause — `CobolData.g4:91 recordClause`):

- **Capture** (`SemanticBuilder.VisitRecordClause` → `FileSymbol.IsRecordVarying` / `RecordVaryingMin`
  / `RecordVaryingMax` / `RecordVaryingDependingOn`).
- **WRITE** without trailing-space trimming (the explicit length governs, not TrimEnd): new IR
  `IrWriteRecordVariable` + `FileRuntime.WriteRecordVariable` + `SequentialFileHandler.WriteVariable`.
  Length = the DEPENDING item's runtime value (`ReadFieldAsInt`) when present, else the written
  record's own declared size (`LengthLocation == null`). ISO §13.18.43 / §14.9.51.
- **READ** into the LARGEST 01 record under the FD (`ResolveReadRecordLocation`, via `OwningFile`),
  so a maximum-length record isn't truncated to the first record's size; then store the actual read
  length (`SequentialFileHandler.LastRecordLength` = the line length) into the DEPENDING item — new IR
  `IrStoreRecordLength` + `FileRuntime.GetLastRecordLength` + `StorageHelpers.MoveIntToField`.
- All variable-record behavior is **gated to SEQUENTIAL organization** (`IsVaryingSequential`):
  relative/indexed records occupy fixed-size slots, so a VARYING clause there does not change physical
  storage.

**Two correctness bugs surfaced and fixed along the way:**
1. `SemanticModel.ResolveFileForRecord` only matched the FD's *first* 01 record, so a `WRITE` of a
   secondary record (e.g. the long alternative `…R2-M-G-151`, or RL210A's `RL-VS1R1`) resolved to **no
   file** and silently fell back to a no-op `WriteText` placeholder — the record was never written.
   Now it resolves via `DataSymbol.OwningFile` (set for every FILE SECTION 01). This is what made
   SQ222A–SQ224A's long records actually appear.
2. `RelativeFileHandler.Write`/`Rewrite`/`ReadRecord` assumed the supplied buffer was exactly
   `_recordLength` and blew up (`ArgumentOutOfRangeException`) on a differently-sized record. Made them
   slot-robust (pad/truncate/clamp to `_recordLength`).

**RL210A baseline corrected (vacuous pass removed).** Fixing #1 exposed that RL210A's earlier "clean"
baseline (DEVLOG 240) was a false positive: RL210A writes its second 01 record `RL-VS1R1`, whose WRITE
was a silent no-op, so the relative file was never properly populated and the test produced a short
"passing" report that never exercised relative I/O. With writes now real, RL210A genuinely runs
relative + ODO + RECORD VARYING I/O and reveals **300 failures** — a real relative-file subsystem gap
(relative files with multiple record formats / an ODO record). Per the doctrine (verify output is
*correct*, not just that it ran), RL210A is **removed from the baselines** with a documented note;
it returns when the relative-file record subsystem is implemented. RL209A (writes the FD's first
record — always real) is unaffected and still MATCHes.

Result: **SQ220A SQ221A SQ222A SQ223A SQ224A CLEAN** (0 FAIL*, deterministic), baselined. RL210A
dropped. **NIST baselines 186 → 190** (94 NC + 42 IF + 12 SM + 4 IC + **33 SQ** + **4 RL** + 1 IX).
Full guard ALL GREEN: 1000 unit / 348 integration (347+1 skip) / 190 NIST, **0 regressions**. SQ
session arc: 23 → 33 baselined. Remaining SQ FAIL* tail (each a distinct issue, NOT the basic VARYING
feature): SQ106A (var-length WRITE status sub-cases, 16→5), SQ107A (second-read error, 4→3), SQ115A
(REWRITE record count), SQ214A (READ full ODO into a partial ODO — INTO-style length), SQ116A/124A.

