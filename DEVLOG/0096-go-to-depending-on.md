## Entry 096 — 2026-03-16: GO TO ... DEPENDING ON

Extended GO TO to support multi-target DEPENDING ON form.

**Grammar**: `goToStatement : GO TO? identifier (identifier)* (DEPENDING ON? identifier)? ;`
DEPENDING is already a keyword token, so it acts as a natural delimiter between the target list
and the selector identifier. ANTLR's greedy `(identifier)*` consumes all IDENTIFIER tokens
until it hits the DEPENDING keyword.

**Bound model**: `BoundGoToStatement` now holds `IReadOnlyList<ParagraphSymbol> Targets` and
optional `DataSymbol? DependingOn`. `IsSimple` property distinguishes single-target from
DEPENDING form. Backward-compatible `Target` property for the simple case.

**Lowering**: Simple GO TO still emits `IrReturnConst(targetIndex)`. DEPENDING emits
`IrGoToDepending(selectorLocation, targetParagraphIndices)`. The CilEmitter decodes the
selector field to decimal via `PicRuntime.DecodeNumeric`, converts to int via
`Convert.ToInt32(decimal)`, then emits cascaded `bne.un` comparisons: if selector == 1,
ret target[0]; if selector == 2, ret target[1]; etc. No match = fall through.

**Bug fixed during implementation**: Initial `decimal→int` conversion used `op_Explicit` via
reflection, which is ambiguous (multiple overloads for byte, int, etc.). Fixed to use
`Convert.ToInt32(decimal)` directly.

**NC102A still fails**: The NIST GO TO test uses subscripted identifiers like `GO-SCRIPT(1)`
in the DEPENDING ON clause. Our `identifier` grammar rule doesn't support subscripts — that's
a separate grammar gap (reference modification / subscripting).

3 tests: correct target selection, out-of-range fallthrough, falls-into-next-paragraph.

---

