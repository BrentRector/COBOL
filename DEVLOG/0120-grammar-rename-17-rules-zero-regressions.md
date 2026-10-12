## Entry 120 — 2026-03-20: Grammar Rename — 17 Rules, Zero Regressions

### Why

The grammar had accumulated names from different eras: ANTLR defaults (`identifier`),
spec-literal translations (`relationalOperator`), and implementation artifacts
(`dataNameTail`, `imperativeStatement`). A user-curated audit proposed 17 renames
organized by impact: high-value clarity wins, medium-value COBOL terminology alignment,
and low-value consistency polish. All 17 were approved for immediate implementation.

### The renames

**High-value (clarity + spec alignment):**
- `identifier` → `dataReference` — the single most impactful rename. What the grammar
  called `identifier` was actually a full data reference: base name + subscripts +
  reference modification + qualification. Every COBOL programmer knows `WS-FIELD(IDX)(1:5)`
  is a data reference, not an identifier. This rename rippled through ~100 grammar
  references and ~120 C# references.
- `dataNameTail` → `dataReferenceSuffix` — subscripts, refmod, and qualification are
  suffixes on a data reference, not a "tail".
- `relationalExpression/Operator/Operand` → `comparisonExpression/Operator/Operand` —
  modern compiler terminology replacing dated "relational" naming.
- `logicalNotExpression` → `unaryLogicalExpression` — the rule was a passthrough with
  no NOT handling. Renamed to reflect its actual role AND added `NOT unaryLogicalExpression`
  as a proper alternative, making boolean NOT a first-class grammar construct.

**Medium-value (COBOL terminology):**
- `moveSource/moveTarget` → `moveSendingOperand/moveReceivingPhrase` — COBOL spec uses
  "sending" and "receiving", not "source" and "target".
- `givingReceiver` → `receivingOperand` — awkward COBOL-ism normalized.
- `arithmeticTarget` → `receivingArithmeticOperand` — explicit about what it receives.
- `imperativeStatement` → `statementBlock` — compiler terminology over COBOL spec jargon.

**Low-value (consistency):**
- `paragraphDeclaration/sectionDeclaration` → `paragraphDefinition/sectionDefinition`
- `procedureSectionOrParagraph` → `procedureUnit`
- `fileControlEntry` → `fileControlClauseGroup`
- `genericFileControlClause/genericConfigurationParagraph` → `vendorFileControlClause/vendorConfigurationParagraph`

### Process

Grammar renames were applied first (3 .g4 files), then ANTLR was regenerated, then a
background agent applied all 34 C# rename patterns across BoundTreeBuilder.cs (~120
occurrences, 27 distinct patterns), SemanticBuilder.cs (11 patterns), and
ReferenceResolver.cs. The agent also renamed internal helper methods for consistency:
`BindIdentifier` → `BindDataReference`, `BindRelational` → `BindComparison`,
`BindMoveSource` → `BindMoveSendingOperand`, etc.

Total: 10 files changed, ~1800 lines touched. Zero semantic changes. Zero regressions.

### Numbers

- 119 unit tests, 182 integration tests (1 skip), guard ALL GREEN

---

