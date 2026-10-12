## Entry 137 — 2026-03-21: Statement Enforcement + Flow Analysis Wiring

Completed remaining enforcement phases: STRING (CBL1301/1304), UNSTRING (CBL1401/1405/1406),
INSPECT (CBL1501/1502), SEARCH (CBL1105), SEARCH ALL (CBL1202/1204). VALUE clause validation
in DataItemClassifier: group VALUE warning (CBL1001), category mismatch error (CBL1002).

**SEARCH ALL CBL1204 severity lesson:** Initially made "SEARCH ALL requires KEY" an error.
Six integration tests failed — tables defined without KEY but with ordered data are common.
COBOL-85 allows SEARCH ALL without KEY if data is pre-sorted. Downgraded to warning.

Wired ProcedureGraph.Analyze into Binder.Bind() after bound tree construction and before
IR lowering — the only point where both BoundProgram and SemanticModel are available.

Added ProcedureSymbol + ProcedureParameter + ParameterMode to ProgramSymbol.cs for future
CALL/USING validation. ReportWriterValidator stub ready for when Report Writer codegen lands.

DiagnosticReachabilityTests: 8 tests verifying key diagnostic codes fire correctly, plus
registry completeness (all codes unique, >= 90 descriptors). 151 unit tests total.

---

