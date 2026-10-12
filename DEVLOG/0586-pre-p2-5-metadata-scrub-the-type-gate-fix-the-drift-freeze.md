## Entry 586 — 2026-07-03 18:10 PDT — Pre-P2.5 metadata scrub + the TYPE gate fix — the drift-freeze inputs are now TRUE

**The roadmap's pre-P2.5 scrub (Phase-1 amendment, ISO-validation D-series) is COMPLETE — constructs.json is
corrected BEFORE the P2.5 registry + drift tests freeze it:**
- **(a) JSON/XML de-fictionalized:** the `json-generate-2014`/`xml-generate-2014` rows (whose "ISO 2014
  §14.9.x" citations were fictional — zero spec hits) are PARKED verbatim in the new
  `tests/version-matrix/vendor-constructs.json` (owner decision 2: vendor-dialect, post-G8, dialect axis if
  ever built). The matrix loses their 8 cells; the CobolDialect.g4 doc block re-tags the is2014 JSON/XML gates
  as the vendor surface's floor.
- **(b) the TYPE gate fixed:** `Core/CobolData.g4` typeClause `{is2023()}?` → `{is2002()}?` (PROVISIONAL —
  the ISO validation proved the 2023 seed wrong: ~33 TYPEDEF body hits, zero Annex E rows; refinement blocked
  on the older standards per decision 1). Matrix row renamed `type-clause-2002`, now expecting COMPILE at
  2002/2014 — and it DOES (the bind path was edition-independent all along). CobolParserGenerics.g4 confirmed
  DEAD (not in the root grammar's import list) — the live gate was the only one.
- **(d)+(e) the pending-row mechanism + four seeds:** constructs.json gains an optional `status: pending` —
  catalogued (metadata frozen by the upcoming drift tests) but compile-asserted only when the owning phase
  lands; `PendingRows_AreCataloguedWithActivationContracts` enforces the contract shape. Seeded:
  `usage-float-short-2002` + `usage-float-binary32-2014` (the D16 float-family edge split),
  `constant-entry-2002` (§13.10 + §13.18.15, D5), `concat-operator-2002` (§8.8.3, D6 — the 2023 CONCAT is
  defined by reference to it). ONE pending mechanism, shared conceptually with the corpus manifests
  (feedback_singular_pattern).
- (c) XOR rows: deferred to W3 as planned (no catalogue rows exist yet).
Conformance **1069/1069** (8 vendor cells out, the pending fact in, TYPE green at four editions); unit 36/36;
FULL legacy guard on the grammar change: pending at entry-write, gated before commit.

