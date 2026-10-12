## Entry 690 — 2026-07-08 05:12 PDT — Bind-time gating migration Cluster 11 — position-safe reservation words (REPOSITORY INTERFACE / PROPERTY + FUNCTION-ID IS PROTOTYPE); LAST hard-reserved cluster

Final MOVE_TO_BINDTIME cluster: three RESERVATION_WORDs (INTERFACE / PROPERTY / PROTOTYPE are user-defined names below
2002) that the recon argued are POSITION-SAFE — their `{is2002()}?` predicate gates only a specifier/tail slot in a
closed alternative set, NOT a name slot, so ungating cannot re-read a valid lower-edition program (their user-word
admission is via `cobolWord`/`_dataNameTokens`, independent of these predicates). GUARD-GATED per the plan (ship only
if byte-identical). Ungated `CobolParserCore.g4:456` (repository INTERFACE), `:457` (repository PROPERTY), `:191`
(FUNCTION-ID IS PROTOTYPE) and added the Check at each recognition point: repository INTERFACE (new `re.INTERFACE()`
branch) + PROPERTY (alongside the existing `OoRepositoryProperties.Add`) in the `DataBinder` repository loop;
FUNCTION-ID IS PROTOTYPE at `CSharpEmitter.CallMakeUnit` (emit-time — `isPrototype` + the sink `edition` are there,
beside the sibling RECURSIVE COBOLNET0885 gate). Retired the four `EditionGateHints` arms (repository INTERFACE/PROPERTY
+ the two PROTOTYPE arms); PRESERVED the DISTINCT PROPERTY-in-dataDescription arm (property-clause-2002 residue).
Beyond-recipe: `using CobolNet.Editions;` in `CSharpEmitter.Call.cs`. **Position-safety PROVEN:** `01 INTERFACE PIC 9`
/ `01 PROPERTY PIC 9` / `01 PROTOTYPE PIC 9` + `MOVE 1 TO INTERFACE` at 85 parse CLEAN (the words still work as data
names), AND the FULL legacy guard is byte-identical — so bind-time shipped (no fallback needed). Verified: all three
→ COBOLNET0900 at 85 (naming the entity), compile at 2002. Battery: conformance 2055 · unit 224 · FULL legacy guard
NIST 353 MATCH.

**Migration hard-reserved phase COMPLETE.** All 24 MOVE_TO_BINDTIME constructs now gate at their bind-time recognition
point through the ONE `ConstructRegistry.Check` funnel. `EditionGateHints` is down to its irreducible residue: the 6
reservation-word arms (XOR / B-AND/B-OR/B-XOR/B-NOT [+ COMPUTE-F2 lookahead] / SHARING / RETRY / UNLOCK /
PROPERTY-in-dataDescription) + the JSON/XML vendor COBOL0313 branch — each a keyword that is a valid user word below
its edition, so it MUST stay parse-gated (ungating would miscompile). Next: docs closeout (rename EditionGateHints to
the reserved-word hint table).

