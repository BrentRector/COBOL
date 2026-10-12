## Entry 047 — 2026-03-14: File I/O Wired — NC101A Writes 36 Records to Disk

FileRuntime implemented: OpenOutput creates host file, WriteText writes strings,
CloseFile flushes. Auto-flush was critical — without it, buffered writes produced
an empty file because STOP RUN doesn't call CloseAll.

Binder lowers OPEN/WRITE/CLOSE → IrRuntimeCall → FileRuntime methods.
CIL emitter dispatches each to the correct runtime import.

NC101A now writes 36 records to `print-file.txt`. Records are placeholder text
(`[RECORD: DUMMY-RECORD]`) — next step is wiring actual record bytes through
the storage model so MOVE + WRITE produces real COBOL print-file output.

The PERFORM chain proves correct: Main → OPEN-FILES → HEAD-ROUTINE →
COLUMN-NAMES-ROUTINE → WRITE-LINE → WRT-LN → WRITE DUMMY-RECORD,
executing all paragraph calls in the right order.

---

