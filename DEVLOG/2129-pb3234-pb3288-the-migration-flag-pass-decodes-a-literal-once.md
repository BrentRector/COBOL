## Entry 2129 — 2026-10-16 18:35 PDT — PB3234 + PB3288: the migration-flag pass decodes a literal once and resolves every name in its own unit

`FlagConformancePass` (FLAG-02 / FLAG-14, ISO §7.3.14.4 / §7.3.15.4 GR4) carried two private mechanisms beside the
canonical ones, and both were wrong answers a source change could not silence.

**PB3234, the literal.** `StripLiteral` deleted every quote and apostrophe in a token and ignored the `X`/`N`
prefixes. Re-probe on the pin: with `88 FS-HEX04 VALUE X"3034"` on a FILE STATUS item and `>>FLAG-14 I-O-STATUS-04`,
`IF WS-FS = "0'4"` drew COBOLNET1621 (the value is the three characters `0'4`, which StripLiteral turned into `04`)
while `IF WS-FS = X"3034"` and `IF FS-HEX04` (value `04`, which it turned into `X3034`) drew nothing. The same
function fed the VALUE-EDITING scan. It is deleted: the relation operand, the level-88 VALUE reading and the
VALUE-EDITING scan call `CobolLiteral.Decode`, the one decoder (§8.3.3.2.4 GR1 delimiters are no part of the value;
§8.3.3.2.3 SR3 / §8.3.3.5.3 SR3 a doubled one is one character; §8.3.3.2.4 GR4 `X"3034"` is `04`). Finding on the way: the hexadecimal literal is a token of its
own (`HEXLIT`), which a relation reader written for `STRINGLIT` never saw; `CharacterLiteralText` now names
STRINGLIT, NATLIT and HEXLIT once for both readers, so `IF FS = N"04"` and a hexadecimal `VALUE` on a numeric-edited
item are judged by their value too.

**PB3288, the names.** `Run` built five group-wide `HashSet<string>`s (LINAGE write targets, VARYING reports, FILE
STATUS names, the 88s valued '04'/'07') keyed by spelling over every unit. Re-probe: program PN's own `WS-STAT`, its
88 and its `LREC` were flagged because program PA's `WS-STAT` was a FILE STATUS item and PA's `LREC` a LINAGE record;
inside one program `FS OF G2` and `IS-04 OF FS OF G2` were flagged for G1's; and `WRITE REC IN LF` was missed (the
detector matched the text `RECINLF`). Fix shape: the pass keeps no name set. Each detector resolves its operand in the
current unit's scope and reads the role off the resolved object: a data reference through `ResolveItem` is a FILE
STATUS reference when the item IS (reference identity) the `FileStatusItem` of a file of the unit or a containing
program; a condition-name through the new `DataBinder.QualifiedConditionCandidates` (the pure part of
`ConditionBinder.ConditionOf`, extracted so the binder and this pass share ONE resolution) when its conditional
variable is; a WRITE/REWRITE through the record-name (qualified or not) or `FILE file-name` to a `FileModel` among
`VisibleFiles()` (own files, then containers' GLOBAL files, §13.18.27.4 GR1); a TERMINATE report-name among
`DataBinder.VisibleReports`. The d I-O-DECLARATIVE file lookups share `VisibleFiles`, so a contained program's READ
or WRITE of its container's GLOBAL file now resolves (it did not before: a false negative).
`DESIGN-flag-directives.md` loses the "collision-tolerant" idiom and its stale OO-method scope note (PB1952).

**Witnesses** (all `FlagPopulationTests`, leg 1 of the gate): the PB3234 rows of
`IoStatus_FlagsEveryConditionThatTestsTheFileStatusItem` (`X"3034"`, `N"04"`, `"0'4"`, `'0''4'`, the hex and quote
88s), `ValueEditing_AHexadecimalLiteralIsScannedByItsValue`, `IoStatus_ANameSharedWithAnotherProgramsFileStatusItem_*`
(both program orders), `IoStatus_ANameSharedByAnotherGroupOfTheSameProgram_*`,
`IoStatus_AContainedProgramReferencingAContainersGlobalFileStatusItem_IsFlagged`, `WriteEndOfPage_*` (shared record
name, qualified record name) and `TerminateWithVarying_AReportNameShared*` (both orders). Each collision case was
reproduced against the pre-change `cobol.exe` first. Rows: both notes close none (`closes_rows: []` with reasons);
they are advisory-directive defects, not inventory rows.

**Self-review.** Architecture: one decoder, one condition resolution, role read off the resolved object, no cache
that outlives its unit. Full code: `FileByRecordOrName` resolves the record by item identity, so REWRITE gained the
qualified-record fix too; the d arm and the m arm now agree. Performance: the FILE STATUS guard and `IsFileStatusItem`
walk the unit chain only when a condition is scanned; no allocation on the no-directive path. Duplication: the
condition-name candidate walk existed once (ConditionOf) and now still exists once. Not changed, by design: a range
`VALUE "00" THRU "09"` is still no test for '04'; ALL-literal 88 VALUEs are not decoded (neither arm ever read them).

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261017T002742Z-c3d3cd; Unit 33,331 /
Conformance 6,055 / Characterization 5 cases ran in leg 1).

