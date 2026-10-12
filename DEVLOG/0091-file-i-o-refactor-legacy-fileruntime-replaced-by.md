## Entry 091 — 2026-03-16: File I/O Refactor — Legacy FileRuntime Replaced by CobolFileManager

Replaced the legacy `FileRuntime` static class (StreamWriter/StreamReader dictionaries, text-only,
hardcoded WRITE AFTER ADVANCING semantics) with a thin facade over the production
`CobolFileManager + SequentialFileHandler` architecture.

**The core problem**: Two parallel file I/O implementations existed — the legacy one used by CIL
emission, and the production one with proper handler architecture, binary/line-sequential modes,
and ISO status codes. Plain WRITE and WRITE AFTER ADVANCING were conflated into one code path.

**Architecture decision**: FileRuntime stays as a static facade (minimizing CIL emission changes)
but internally delegates everything to CobolFileManager. Two distinct write paths:
- **Plain WRITE** → `handler.Write()` (line-sequential: TrimEnd + WriteLine)
- **WRITE AFTER ADVANCING** → `handler.WriteRawText()` (CR/LF × n, then text, no trailing newline)

**Key debugging episode**: After the rewrite, NIST output went to `xxxxx055.txt` instead of
`print-file.txt`. Root cause: the Binder was using `fileSym.AssignTarget` for ALL files, but
NIST's `XXXXX055` is an identifier ASSIGN target (not a literal). The old code only registered
literal targets, falling back to the COBOL file name for everything else. Fix: check
`AssignIsLiteral` before using the target. Took ~30 minutes of adding debug output to
SequentialFileHandler and FileRuntime to trace — the file was being written but to the wrong path.

**Second subtle issue**: AFTER ADVANCING files need a trailing CR/LF on close. The old code did
`writer.WriteLine()` in `CloseFile`. New approach: `_afterAdvancingFiles` HashSet tracks which
files used WriteAfterAdvancing; CloseFile writes final CR/LF for those files before closing.

**What shipped**:
1. `WriteRawText` on SequentialFileHandler — direct stream write for print-control
2. FileRuntime rewritten: Init/RegisterFileHandler/OpenOutput/OpenInput/OpenIO/OpenExtend/
   CloseFile/WriteRecord/WriteAfterAdvancing/ReadRecord/IsAtEnd/GetLastStatus/Rewrite/CloseAll
3. Binder CreateEntryPoint emits Init + RegisterFileHandler per SELECT
4. BoundWriteStatement carries AdvancingLines; Binder routes to IrWriteAfterAdvancing vs
   IrWriteRecordFromStorage; CilEmitter handles both
5. FILE STATUS population: IrStoreFileStatus IR instruction, EmitFileStatus in Binder,
   GetLastStatus → MoveStringToField in CilEmitter
6. REWRITE full pipeline: BoundRewriteStatement, IrRewriteRecordFromStorage, CilEmitter dispatch
7. LINE SEQUENTIAL grammar: parser rule `LINE SEQUENTIAL` in organizationType
8. Guard script uses --nist flag, per-test output files

**Test results**: 119 unit, 72 integration (+6 from start), 5 skip (−2), 6 NIST at 100%.

**Mistake to remember**: Never run `find /` — it scans the entire filesystem. Always search within
the project directory.

---

