## Entry 685 — 2026-07-08 03:54 PDT — Bind-time gating migration Cluster 6 — GOBACK RETURNING + PROCEDURE DIVISION RETURNING

Sixth cluster; the two RETURNING phrases (hard-reserved RETURNING/GIVING tokens). GOBACK RETURNING (§14.9.18,
2002): ungated `CobolParserCore.g4:1140`, added `Check(…, Constructs.GobackReturning2002, "GOBACK … RETURNING")` in
`CallBindGoback`. **Double-diagnostic handled:** GOBACK itself is 2002+ and already emits COBOLNET0880; when the
RETURNING phrase is present, its more-specific 0900 subsumes the 0880 (the 0880 fires only for bare GOBACK), so a
<2002 `GOBACK RETURNING` yields exactly one diagnostic — matching `EditionGateDiagnosticTests.GobackReturning_At85`
(0900) while bare GOBACK keeps its 0880. PROCEDURE DIVISION RETURNING (§14.2, 2002): ungated the returningClause
predicate at `CobolParserCore.g4:487` (LEFT the sibling raisingClause predicate — a gateless residue with no
constructs.json row), added `Check(…, Constructs.ProcedureReturning2002, …)` in `DataBinder.Linkage.cs` before the
returningClause binding. Retired the two RETURNING `EditionGateHints` arms. Beyond-recipe: added `using
CobolNet.Editions;` to `DataBinder.Linkage.cs`. Verified: GOBACK RETURNING → single COBOLNET0900 at 85 (no 0880),
bare GOBACK → COBOLNET0880, PROCEDURE DIVISION RETURNING → COBOLNET0900 at 85; all compile at 2002. Battery:
conformance 2055 · unit 224 · FULL legacy guard NIST 353 MATCH.

