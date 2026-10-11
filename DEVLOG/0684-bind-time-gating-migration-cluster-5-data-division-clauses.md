## Entry 684 — 2026-07-08 03:40 PDT — Bind-time gating migration Cluster 5 — data-division clauses (BASED / TYPE / TYPEDEF / OCCURS DYNAMIC)

Fifth cluster; four data-description clauses whose keyword is hard-reserved at all editions (absent from
`cobolWord`/`_dataNameTokens`). Ungated the four `{isXXXX()}?` sites in `CobolData.g4` (basedClause:242,
typeClause:255, typedefClause:262, occurs Format-4:346) and added the introduction `Check` at each recognition
point: BASED / TYPE / TYPEDEF in the `DataBinder.BindEntry` clause loop (`Constructs.BasedClause2002` /
`TypeClause2002` / `TypedefDef2002`, gated at 2002), OCCURS DYNAMIC in `OdoModel.OdoBindOccursSpec` inside
`if (occ.DYNAMIC() is not null)` (`Constructs.OccursDynamic2014`, gated at 2014). Retired the four `EditionGateHints`
arms (BASED/TYPE/TYPEDEF + the OCCURS-then-DYNAMIC/CAPACITY pair). Beyond-recipe (verify-by-building): `DataBinder.cs`
and `OdoModel.cs` both lacked `using CobolNet.Editions;` → added. No grammar ambiguity — the Report-Writer `TYPE`
clause is a separate rule taking `reportGroupType` (not IDENTIFIER); occurs Format-4 is LL-disjoint from Format 1/2
on the token after OCCURS (DYNAMIC ≠ integerLiteral). Verified: BASED/TYPE/TYPEDEF → COBOLNET0900 at 85, OCCURS
DYNAMIC → COBOLNET0900 at 2002; all compile past their gate. Battery: conformance 2055 · unit 224 · FULL legacy guard
NIST 353 MATCH.

