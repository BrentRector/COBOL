## Entry 135 — 2026-03-21: genericClause Binder Discipline — Context-Classified Extension Nodes

Every genericClause occurrence in the grammar is now captured, classified, and tracked by the
binder. No genericClause is silently ignored.

**Model**: `GenericClauseNode` with `GenericClauseContext` enum (8 values:
IdentificationParagraph, ConfigurationVendor, SpecialNames, FileDescription,
DataDescription, ReportGroup, FileControl, IOControl). Operands decomposed into
`IdentifierOperand` and `LiteralOperand`.

**SemanticBuilder**: 8 new visitor overrides capture genericClause at each context point.
`CaptureGenericClause()` builds a `GenericClauseNode` with the correct context enum.

**SemanticModel**: `GenericClauses` list populated via `AddGenericClause()`. Available
for binder inspection, diagnostic emission, and future strict-mode enforcement.

**Compilation.cs**: Wires captured clauses from SemanticBuilder to SemanticModel.

This is the foundation for: context-specific extension handlers, strict COBOL-85 mode
(rejecting unrecognized extensions), and vendor-pattern recognition.

---

