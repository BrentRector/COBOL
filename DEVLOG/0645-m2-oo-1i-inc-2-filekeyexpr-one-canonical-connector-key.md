## Entry 645 — 2026-07-06 21:10 PDT — M2-OO-1i inc 2: FileKeyExpr — ONE canonical connector-key expression (byte-identical refactor)

Increment 2 of M2-OO-1i: the emit-side plumbing that lets a file's runtime connector key be either a static literal
(program / factory / EXTERNAL file) or a per-object minted-key field (`this.__fkey_X`, an instance file — inc 4),
selected in ONE place. Added `FileModel.InstanceKeyField` (null for every file today) and the canonical helper
`EmitText.FileKeyExpr(FileModel f) => f.InstanceKeyField is {} fld ? $"this.{fld}" : CsLiteral(f.CobolName)`, then
swept EVERY connector-key emission site (~28, across CSharpEmitter.cs / .Call / .KeyedIo / .Exceptions / .Sort /
.ReportWriter + NumericRenderer's LINAGE-COUNTER) from `CsLiteral(file.CobolName)` to `FileKeyExpr(file)` — the
OPEN/CLOSE/READ/WRITE/REWRITE/DELETE/START/UNLOCK/Status/LastReadLength/registration (Register/RegisterSharing/
RegisterRelative/RegisterIndexed/SetLinage/OpenShared) sites, the SORT/MERGE SD sites, the report-file constructor,
and the USE-declarative file selectors (`__f == FileKeyExpr(f)`). This is the singular-pattern discipline
(feedback_singular_pattern / feedback_refactor_first_always): after inc 2 there is exactly ONE way to name a
connector, so inc 3-5 flip individual files to a per-object key with no site-by-site edits.

**Byte-neutral by construction:** every existing file (program / EXTERNAL) has `InstanceKeyField == null`, so
`FileKeyExpr` returns the identical `CsLiteral(file.CobolName)` — and the per-program qualification rename
(Call.cs:138-142) still runs before emit, so the literal is the same qualified key as before. No new golden — the
FULL BATTERY is the proof (feedback_refactor_first_always): **1963 conformance (unchanged) · 216 unit GREEN**, every
file-I/O series (SQ/RL/IX/IC, keyed, sort/merge, report, LINAGE) byte-for-byte unchanged, zero regressions.

