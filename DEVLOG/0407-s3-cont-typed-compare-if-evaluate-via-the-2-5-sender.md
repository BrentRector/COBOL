## Entry 407 — S3 cont.: typed COMPARE (`IF`/`EVALUATE`) via the §2.5 sender-materialize (byte-identical)

Typed character fields now work in relational conditions. A comparison operand is a read-only **sender**, so the
new `CilLocationEmitter.EmitLocationArgsMaterializingTyped` materializes a typed field to a scratch byte window
(`CobolString.ToWindow(string,int)` → `byte[]`, Latin-1) and the existing byte compare runs over it — guaranteeing
byte-identity (literally the same `StorageHelpers.CompareFieldToField`/`CompareFieldToString` path, incl. the
collating-sequence variants). Wired into all seven string-compare operand sites in `CilComparisonEmitter`
(`EmitStringCompare`/`…Literal`/`…WithSequence`/`…LiteralWithSequence`/`EmitStringExprCompare`). The materialize
helper is **sender-only** (no write-back) — receivers stay on the loud `EmitLocationArgs` guard until their own
cells/materialize-with-write-back land. Autonomous /loop tick; done per [[feedback_continue_dont_wait]]. Test
(`TypedFieldFlipTests`): `IF` on typed fields — `=`literal, `=`field, `NOT =`, `<` — byte-identical to the byte
path. Guard **ALL GREEN: 1187 unit / 488 integration / 364 NIST**. NEXT: the read-op sender-materialize for
INSPECT/STRING/refmod, then numeric typed fields (hard-gated on the `CobolNum` oracle), then OCCURS/nested groups.

