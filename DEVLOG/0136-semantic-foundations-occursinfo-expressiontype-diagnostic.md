## Entry 136 — 2026-03-21: Semantic Foundations — OccursInfo, ExpressionType, Diagnostic Registry

The first major semantic infrastructure push. Replaced the flat `OccursCount` integer with a
structured `OccursInfo` carrying min/max, DEPENDING ON, ASCENDING/DESCENDING KEY, and INDEXED BY.
Removed the backward-compat wrapper — all 19 call sites across 7 files updated to use `Occurs?.MaxOccurs ?? 1`.
The user explicitly rejected a backward-compat property, insisting all callers be migrated. Right call —
the compat wrapper would have hidden bugs where code should have been checking `Occurs != null`.

**OccursInfo + RenamesInfo (Phase 1.1):** Full OCCURS clause decomposition in SemanticBuilder —
parses min/max from `OCCURS m TO n`, DEPENDING ON data-name, KEY data-names from `occursKeyClause`,
and INDEXED BY names. Grammar accessor mismatch hit: `occursKeyClause` has `dataReference+` directly,
not `dataReferenceList()`. Fixed by reading the .g4 — lesson: always check the grammar for accessor names.
`DataItemClassifier` validates OCCURS on 01/77 (CBL0801), BLANK WHEN ZERO on non-numeric-DISPLAY
(CBL0802), JUSTIFIED on non-alphanumeric (CBL0803), DEPENDING ON integer requirement (CBL1101),
and KEY subordination (CBL1103/CBL1104).

**ExpressionType (Phase 1.2):** `NumericType` (Precision/Scale/IsSigned/NumericKind) and
`ExpressionType` (Kind + optional NumericType). `Promote` implements standard widening for
arithmetic: max scale, max integer digits, floating wins. Wired into `BoundExpression.ResultType`
via a `Typed<T>()` helper that infers type from expression kind at construction. Only attached
at `BindDataReferenceWithSubscripts` return points — sufficient since all identifier expressions
flow through there.

**Diagnostic Registry:** 90 `DiagnosticDescriptor` instances covering the full CBL code range
(CBL0801–CBL3502). `DiagnosticBag.Report(descriptor, location, span, args...)` overload with
string.Format templating. All descriptors in one file as static readonly fields — easy to audit
for completeness and no string typos.

**Arithmetic Enforcement (Phase 2.1):** `ArithmeticTypeSystem.ValidateArithmeticStatement()`
checks all operands (CBL2601), results (CBL2602), ROUNDED targets (CBL2603), and REMAINDER
integer requirement (CBL2605). Wired via `ValidatedArithmetic()` helper that wraps all 5
arithmetic statement construction sites.

**MOVE Enforcement (Phase 2.2):** Category compatibility checking in BindMove using existing
`CategoryCompatibility.IsMoveLegal`. Hit a real bug immediately: MOVE ZEROS TO numeric-field
was rejected because figurative constants carry CobolCategory.Alphanumeric. Initial fix was
too broad (skip all figuratives + literals). User caught this — only ZERO should be treated
as Numeric for MOVE purposes. Fixed to compute `effectiveSrcCat` per-figurative: ZERO → Numeric,
all others → Alphanumeric.

**Flow Analysis (Phase 3.1):** `ProcedureGraph` builds adjacency from paragraphs + fall-through +
PERFORM/GO TO edges. BFS reachability from entry. Cross-section fall-through detection.
Recursive statement walker for nested IF/EVALUATE/SEARCH/COMPOUND transfer edges.

**Additional infrastructure:** `SymbolValidator` (Linkage VALUE/REDEFINES rules),
`FileStatusValidator` (FILE STATUS type/length/group checks), `CompilationOptions` (DialectMode
enum for future strict COBOL-85 gating), `StorageAreaKind` extended with LinkageSection/LocalStorage,
`DiagnosticTestBase` shared test harness, `InternalsVisibleTo` for unit test access to internal
members.

**Test results:** 143 unit (was 119, +24 new), 176 integration, all pass. NIST regression green.

**AI missteps:**
- Grammar accessor name guessed wrong (`occursKeyClause.dataReferenceList()` doesn't exist —
  the rule uses `dataReference+` directly). Fixed by reading the .g4.
- MOVE enforcement too aggressive on first pass — needed to exempt figurative constants and
  literals. Then over-corrected by exempting ALL figuratives. User caught the logic error:
  only ZERO is numerically compatible.

---

