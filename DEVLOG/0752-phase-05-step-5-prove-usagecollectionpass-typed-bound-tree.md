## Entry 752 — 2026-07-10 21:57 PDT — PHASE 05 Step 5 (prove): `UsageCollectionPass` typed bound-tree walk — the CORRECT whole-group set (owner-directed redesign; legacy over-collects)

**Owner directive (2026-07-10):** never workaround/hack — REDESIGN for the best. My first two attempts at Step 5 were
BOTH rejected as shortcuts: (a) keep `ReferenceResolver`'s mid-resolve `WholeGroupReferenced` mutation "because moving
it is hard"; (b) a REFLECTIVE bound-tree Place walk. The correct design: an explicit TYPED walk of the (already
faithful) bound tree.

**The key finding (investigation `wf_fe251cf8-9d6`, CORRESPONDING agent, compile-test-proven):** the bound tree is the
CORRECT oracle for "which groups are whole-character-image operands" — a group has a `Place` in the tree EXACTLY where
it is used as a whole operand (MOVE/DISPLAY/compare/ACCEPT/record I-O/whole-group CORRESPONDING pair/boundary formal).
Legacy's `ReferenceResolver.PlaceForItem` (ReferenceResolver.cs:277,300) OVER-collected — it added ANY resolved group,
including constructs that decompose to member/element access and are NEVER whole-image operands: CORRESPONDING OPERANDS
(§14.7.6 — expands to per-pair implied statements), SEARCH TABLES (scanned by index), QUALIFIER groups (`TBL-ITEM-1 OF
TABLE-LEVEL-5A` — the qualifier is resolved but the operand is the leaf), INDEXED file KEYS, and ACCEPT-into-table
occurrences. Proven output-neutral: corrA (with CORR) emits a numeric-DISPLAY operand leaf as `string`; corrB (explicit
per-member ADD) emits `long`; byte-identical runtime output.

**What landed (D1 prove, PARALLEL — not yet flipped):**
- `Binding/Passes/UsageCollectionPass.cs` — an explicit typed `Visitor` (no reflection) modeled on
  `VersionConformancePass.Recurse`, collecting the group `Place.Item` at every whole-operand position (statement Place
  fields, `BoundFieldOperand`, relational/88/class `BoundCondition`, whole-group `CorrespondingPair`, `SetPlaceTarget`,
  INITIALIZE stores; full nested-statement descent) — a group NEVER appears in a numeric `BoundExpr`/`BoundBoolExpr`.
  Plus the structural sources I-O / a CALL crossing fill without a Place: FILE record areas (§9.1.2) + non-carrier
  program formals + OO method formals (§14.2.3 GR8). Fills `DataBinder.WholeGroupReferencedV2` (parallel).
- `UsageCollectionPass.EffectDiff` + `StorageFormEquivalenceTests` assert the EMIT-RELEVANT invariant: the correct set
  flips a strict SUBSET of legacy's numeric-DISPLAY leaves — **zero `v2-flips-not-legacy` corpus-wide** (V2 never adds
  spurious image storage and never misses a leaf legacy caught). The `legacy-flips-not-v2` difference is confined to
  exactly **3 programs** (NC202A/NC208A CORRESPONDING · NC207A qualifier), all legacy-spurious + output-neutral.

**Verification model (owner-chosen):** the correct set DIFFERS from legacy (legacy is buggy/over-inclusive), so the
prove is by OUTPUT (greenfield DifferentialGolden + conformance byte-exact runtime), NOT legacy set-equality; the
emitted C# changes for those 3 programs (a numeric-DISPLAY leaf `string`→`long`) will be re-baselined at the flip with
the runtime-golden proof. Battery at this checkpoint: conformance 3157 · unit 258 · characterization 32 (parallel — no
production wiring flipped yet). **NEXT (the Step-5 flip):** wire `UsageCollectionPass` → `WholeGroupReferenced`, delete
`ReferenceResolver`'s mid-resolve adds, verify runtime goldens, re-baseline the 3 programs' characterization snapshots.

