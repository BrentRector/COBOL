## Entry 373 — M4-1: DELETE FILE statement execution (ISO §14.9.10, COBOL-2023)

Plan Wave 1 item (cheap win). `DELETE FILE file-name` parsed (is2023()-gated) but was a **parse-only no-op** — no
bound node, no lowering, no runtime — so it silently did nothing. Implemented the full statement pipeline,
mirroring the DELETE RECORD path:
- `BoundDeleteFileStatement` (+ `BoundNodeKind.DeleteFileStatement`); `FileIoBinder.BindDeleteFile`;
  `BoundTreeBuilder` dispatch (`ctx.deleteFileStatement()`).
- `IrDeleteFile(fileName, assignTarget)`; `FileIoLowerer.LowerDeleteFile`; `Binder` lowering dispatch.
- `CilFileIoEmitter.EmitDeleteFile` + `CilEmitter` dispatch.
- `FileRuntime.DeleteFile(fileName, assignTarget)`: `ResolveHostPath` + `File.Delete`, setting the connector
  I-O status — `00` deleted, `35` not available (ISO §14.9.10: the referenced file is not available), `30` other
  host error (e.g. open/locked).

Verified end-to-end: OPEN OUTPUT + WRITE + CLOSE, then `DELETE FILE F`, then OPEN INPUT F → status **35** (a
no-op would have left it readable, status 00), and the physical file is gone. Tests:
`DeleteFile_DeletesThePhysicalFile` + conformance `tests/conformance/2023/delete_file` (first 2023 corpus entry).

Scope (documented follow-up): the optional `ON EXCEPTION` / `NOT ON EXCEPTION` phrases are accepted by the
grammar but not yet honored (no conditional branching on the delete status); multiple file-names in one
`DELETE FILE` (the grammar takes one).

Plan: M4-1 ticked ☑. Guard ALL GREEN: **1047 unit / 463 integration (+2) / 364 NIST**, 0 regressions.

