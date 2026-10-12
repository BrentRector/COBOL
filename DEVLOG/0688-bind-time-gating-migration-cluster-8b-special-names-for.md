## Entry 688 — 2026-07-08 04:38 PDT — Bind-time gating migration Cluster 8b — SPECIAL-NAMES FOR ALPHANUMERIC/NATIONAL + REPOSITORY CLASS

Second half of Cluster 8. SPECIAL-NAMES FOR (ALPHANUMERIC | NATIONAL) (§12.3.7, 2002) appears on THREE clauses —
ALPHABET / CLASS / SYMBOLIC CHARACTERS — each with the hard-reserved lead token FOR. Ungated the three identical
`{is2002()}? FOR (…)` sites (`CobolSpecialNames.g4:60/75/85`) and added a FOR-presence `Check(SpecialNamesForNational2002)`
at each recognition point in `DataBinder.Switches.cs`: `AlphabetBind`, `SwitchBindClass`, and a **NEW
`symbolicCharactersClause` branch** in the `SwitchBindSpecialNames` loop (SYMBOLIC CHARACTERS was entirely unbound —
parsed then dropped; the new branch gates its FOR phrase and keeps the base clause accepted-inert). REPOSITORY CLASS
(§12.3.8, 2002; CLASS is 85-reserved): ungated `CobolParserCore.g4:455` and added a NEW `re.CLASS()` branch in the
repository loop (`DataBinder.cs`) with `Check(RepositoryClass2002, $"REPOSITORY CLASS '{name}'")` — the loop dropped
CLASS specifiers before. Retired the two `EditionGateHints` arms (`FOR`-in-specialNames, `CLASS`-in-repository).
Beyond-recipe: `using CobolNet.Editions;` in `DataBinder.Switches.cs`. Verified: ALPHABET/CLASS/SYMBOLIC … FOR
NATIONAL and REPOSITORY CLASS → COBOLNET0900 at 85, compile at 2002; a plain ALPHABET (no FOR) stays clean; the
repository message names the class. Battery: conformance 2055 · unit 224 · FULL legacy guard NIST 353 MATCH.

