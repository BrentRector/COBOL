## Entry 409 — S3 cont.: class conditions (`IS NUMERIC`/`IS ALPHABETIC`) on a typed field (byte-identical)

A typed string field can now be the subject of a class condition. The subject is a read-only **sender**, so the
new `CilLocationEmitter.EmitLocationArgsWithPicMaterializingTyped` (and the existing
`EmitLocationArgsMaterializingTyped`) materialize it to a byte window and run the same byte class check
(`PicRuntime.IsNumericClass`/`IsAlphabeticClass…`/`IsInUserClass`) — byte-identical. Wired the three
class-condition subject sites in `CilComparisonEmitter` (`EmitClassCondition` numeric + non-numeric arms,
`EmitUserClassCondition`). With this, typed character fields cover the common read ops — MOVE (all pairs),
DISPLAY, COMPARE, class conditions, figurative — all byte-identical and gated. Autonomous /loop tick,
[[feedback_continue_dont_wait]]. Test (`TypedFieldFlipTests`): `IS NUMERIC`/`IS ALPHABETIC` on typed fields,
byte-identical. Guard **ALL GREEN: 1187 unit / 490 integration / 364 NIST**. NEXT (the major remaining stage):
**numeric typed fields** — substantively harder byte-identity (the in-memory `long` vs the on-disk digit image;
encode/format/truncation/sign), hard-gated on the `CobolNum` oracle; best started with fresh context.

