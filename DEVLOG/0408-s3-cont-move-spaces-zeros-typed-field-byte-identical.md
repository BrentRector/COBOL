## Entry 408 — S3 cont.: MOVE SPACES/ZEROS → typed field (byte-identical)

`MOVE SPACES`/`MOVE ZEROS` to a typed field (the ubiquitous field-clear idiom) now stores a width-long fill
string (`new string(' '|'0', width)`) directly to the typed field, via the flat/struct store helpers — byte-
identical (the byte path fills the window with the same byte). Restricted to SPACE/ZERO (printable, byte-identical);
HIGH/LOW-VALUE/QUOTE/NULL on a typed field keep the byte path / loud guard (the alphanumeric-DISPLAY ASCII-vs-Latin-1
quirk makes non-printable figurative display deferrable). Two branches in `CilDataEmitter.EmitMoveFigurative`.
Autonomous /loop tick, [[feedback_continue_dont_wait]]. Test (`TypedFieldFlipTests`): `MOVE ZEROS`→`"0000"`,
`MOVE SPACES`→cleared. Guard **ALL GREEN: 1187 unit / 489 integration / 364 NIST**. NEXT: numeric typed fields (the
big remaining capability — hard-gated on the `CobolNum` oracle), the read-op sender-materialize for
INSPECT/STRING/refmod, then OCCURS/nested groups.

