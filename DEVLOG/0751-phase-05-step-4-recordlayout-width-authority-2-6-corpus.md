## Entry 751 — 2026-07-10 20:00 PDT — PHASE 05 Step 4: `RecordLayout` width authority (§2.6) + corpus width-equivalence assert (D0, additive)

Landed PHASE-05 Step 4 (DESIGN-data-model §2.6) — the ONE physical-width authority, `Binding/Model/RecordLayout.cs`,
as a PARALLEL derived value proven equal to the legacy width geometry corpus-wide BEFORE any deletion (prove-then-delete
D0). ADDITIVE + greenfield-only + test-path only (`RecordLayout` is referenced solely by `StorageFormPass` in the
greenfield compiler; ZERO production wiring — the readers flip in Step 8).

**What landed:**
- `RecordLayout.ImageWidth(DataItem)` — the character-image width (leaf = its Step-2 `StorageForm.ImageWidth`; group =
  Σ non-redefining children × own OCCURS). Mirrors `DataItem.ImageWidth`, reading the Step-2 StorageForm.
- `RecordLayout.PhysicalWidth(DataItem)` — the tier-aware physical extent the emitted `AsImage()/FromImage()` codec
  spans (a Tier-B StringCanonical class contributes its ONE class-max backing once at the canonical, views nothing; a
  Tier-A alias view forwards). Mirrors `OdoModel.PhysicalWidth`/`SortPhysicalWidth`.
- Consolidated `StorageFormPass.ImageWidthOf` → `RecordLayout.ImageWidth` (single source, feedback_singular_pattern) —
  so Step-2's identity #3 (`ImageWidthOf == DataItem.ImageWidth`, corpus-proven) now ALSO proves
  `RecordLayout.ImageWidth == DataItem.ImageWidth`.
- Extended `StorageFormPass.Verify` with identity **#5**: `RecordLayout.PhysicalWidth(group) == OdoModel.PhysicalWidth(group)`
  for every group, over the whole NIST corpus + the crafted REDEFINES cases (the DESIGN §5.4 drift guard, held green
  until Step 9 deletes the duplicate copies).

**Scoping refinement (deviation from Step 4.1, documented per rule 4):** Step 4 delivers the WIDTH authority (the §5.4
named drift hazard) fully-proven. The two OFFSET copies — `StatementBinder.Sort`'s `SortOffsetInRecord`/`SortPlainOffset`
and `StatementBinder.KeyedIo`'s `KeyedAreaOffset`/`KeyedKeyIndex` — are DEFERRED to Step 8, because the two legacy
algorithms use DIFFERENT width bases for the running offset increment: Sort advances by `PhysicalWidth` (class-max,
matching the emitted codec) while Keyed advances by `ImageWidth` (the redefined item's own width). They agree only where
no key/sort-key follows a redefines whose redefiner is WIDER than its target, so `RecordLayout.OffsetOf` cannot be proven
byte-equal to BOTH as a pure port. The unification (onto the codec-correct `PhysicalWidth` basis — which may surface a
latent `KeyedAreaOffset` under-count) folds into `RecordLayout.OffsetOf`/`KeyIndexByPosition` at Step 8, under the
protection of the Sort/Keyed output goldens. (PHASE-05 doc Step 4 + Step 8 updated with this.)

**Battery:** greenfield conformance **3157** · unit **258** (the extended #5 assert rides the existing
`StorageFormEquivalenceTests`, no new count) · characterization **32 byte-exact** (test-path change; production emit
untouched) · FULL legacy guard NIST 353 MATCH (greenfield-only). Adversarial 2-lens review run (wf_05c59796-5fa).

**RESUME AT: PHASE 05 Step 5** — `UsageCollectionPass` owns `WholeGroupReferenced` (parallel, proven set-equal; D1 prove half).

