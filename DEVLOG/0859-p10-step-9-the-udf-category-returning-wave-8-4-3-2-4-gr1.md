## Entry 859 — 2026-07-16 06:09 PDT — P10 Step 9 — the UDF category-RETURNING wave: §8.4.3.2.4 GR1 lifts COBOLNET1510 for alphanumeric / group / numeric-edited / national results; the channel was already there

**The audit's gap 1 (UDF track) closed far smaller than the phase recipe sized it.** The recipe expected a new
category-carrying result operand in BoundTree, a CallEmitter RETURNING-carrier leg, and CallAbi work. None of
that was needed, because the pieces the P7 rearchitecture left behind already compose into the channel:

- **The temp IS the category carrier.** `CreateCompilerTemp` clones the callee's RETURNING description
  (Pic/sign/justified/blank-when-zero) into a caller-side item — §8.4.3.2.4 GR1's words, "the description,
  class, and category of the temporary data item is that specified by the description in the linkage
  section". A `BoundFieldOperand` over its Place carries that Pic into MOVE Table-16 legality
  (MoveBinder.MoveCategoryLegality reads `Place.Item.Pic.Category`), relation class dispatch
  (ConditionRenderer.StringCategoryOf / OperandText.IsString), DISPLAY rendering (OperandText.FieldAsString),
  and the FUNCTION LENGTH fold (BindLengthFold reads `ImageWidth` — the golden proves L=08, the CLONE's
  width). No new bound node.
- **The ABI already ships strings.** The callee-side delivery was ALREADY category-switched
  (`ProgramEmitter`: `CallPlaceIsString(ret) ? StoreReturn(CallStringRead(ret)) : StoreReturn(read)`), and
  `CobolArgAdapt.StoreReturn(ManagedPointer?, string)` existed. The caller's RETURNING carrier
  (`CallEmitter.RefCarrier`) routes groups/alnum/edited/national through `ManagedPointer<string>.OverField`
  with FromImage distribution for groups. Verified, not built.
- **What was actually missing — three seams + the guard.** (1) The blanket 1510 reject in `UdfBinder`
  became the per-shape `UdfReturningResidue` staging. (2) A GROUP RETURNING needs a described temp:
  `CreateCompilerTemp` grew a group leg — the new `CloneTempNode` deep-clones the subtree UNREGISTERED
  (unlike the TYPEDEF `CloneItem`: a temp's subordinates are never referenceable — §8.4.3.2.3 SR1 makes a
  function result a sending operand only — and registering the CALLEE's linkage member names in the CALLER's
  scope would ambiguate legal caller names). The group temp's numeric-DISPLAY leaves are image-promoted for
  free: `UsageCollectionPass.Visit(BoundCallProgram)` collects `Returning` as a whole-group operand.
  (3) `ConditionBinder.ComparisonOperandOf`'s computed fallback now routes through the ONE
  `IntrinsicBinder.OperandOf` mapping — without it a relation operand's UDF result stayed a
  `BoundComputedOperand` and an ALPHANUMERIC result would have compared NUMERICALLY (the exact mis-carry the
  2026-07-05 adversarial review predicted). Numeric renderings are identical either way (AsNum unwraps both
  to the same FieldNum read) — pinned by the untouched 33/33 characterization set. (4) `ConformanceForest`
  prunes a temp's whole SUBTREE — a group temp's cloned children must not re-fire per-item data gates in the
  caller's unit (the elementary exclusion only skipped the root).
- **Staged loud, per shape (§1.4 — never half-wire):** FLOAT (the CALL-boundary string carrier has no float
  write half — `CallStringWrite` on a double field would emit uncompilable C#; landing it means a float
  round-trip leg in the carrier trio, its own descriptor now recorded in `UdfReturningResidue`); BOOLEAN
  (the §8.8.2 boolean-expression channel — `IF f(x)`, COMPUTE Format 2 — has no function-result arm;
  admitting MOVE/relations alone would half-wire the category); pointer/object/index classes; and the group
  residues (strong-typed identity §8.5.3.3, internal REDEFINES, variable-length §8.5.1.12, non-character
  binary/packed/COMP-5/float leaves — the Tier-C island). BY VALUE formals (1506's OPTIONAL/OMITTED band)
  and the 1509 per-evaluation hoist guard are UNTOUCHED — they are Step 10's gaps, still open.
- **Spec basis.** §14.2.2 SR5 places NO category restriction on a function's RETURNING item (any 01/77
  LINKAGE entry without BASED/REDEFINES); §8.4.3.2.4 GR1 clones its description into the reference's temp;
  §14.8.3.2/.3 give the returning-item conformance rules (trivially satisfied here — the temp IS a clone).
  So every remaining 1510 shape is an implementation residue named as such, not a conformance rule.
- **Witness.** `udf_returning_categories` golden (ENABLED, byte-exact; legacy GreenfieldOnly — the frozen
  legacy carries only numeric results): alnum MOVE/direct-DISPLAY/LENGTH-fold/relation, group MOVE +
  receiver child access + direct DISPLAY (AsImage), edited DISPLAY + edited→alnum MOVE, national MOVE (pad)
  + national relation. Hand-derived expected output confirmed against the run before recording the .out.
  `StagedReturningCategories_1510` rewritten as `ReturningCategories_CarriedVsStaged1510` (4 carried + 3
  staged shapes). Targeted battery: Udf 44/44 · CorpusRunner 190/190 · characterization 33/33 byte-exact.

