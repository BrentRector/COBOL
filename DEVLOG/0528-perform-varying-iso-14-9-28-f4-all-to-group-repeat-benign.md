## Entry 528 — 2026-06-10 01:25 PDT — PERFORM VARYING (ISO §14.9.28 F4) + ALL-to-group repeat + benign OOR subscripts → SIX new NC greens (33 total)

**The VARYING wave (diagnosis Wave 5) + the two real bugs NC243A's torture exposed.**

- **PERFORM VARYING complete per §14.9.28 GR12–13** (NOT scoped to the corpus): any number of AFTER levels,
  TEST BEFORE and TEST AFTER, omitted BY ⇒ 1 (GR12), index-name AND numeric induction variables, inline and
  out-of-line. The emission realizes the spec's EXACT sequencing: TEST BEFORE = nested `while(!cond)` where an
  inner condition going true RESETS its variable to FROM and AUGMENTS the variable to its left before the outer
  retest (GR13e.2a–c); TEST AFTER = body-first loops with right-variable re-initialization on each outer
  iteration (GR13b/c). FROM/BY render INLINE so every set/augment re-reads current contents (GR12 item
  identification — "changes … have immediate effect"). Bound as `PerformVarying(levels)` over `VaryingLevel(Var:
  BoundSetTarget, From, By, Until)` — the induction variable REUSES the SET target model, and the emitter now has
  ONE `StoreSetTarget`/`AugmentSetTarget` pair shared by SET TO / SET UP-DOWN / VARYING (singular-pattern).
- **Bug: ALL "literal" into a GROUP receiver space-padded instead of repeating** (ISO §8.3.3.6.4 GR2 — repeats to
  the RECEIVER width). NC243A seeds its 7-dimension OCCURS-2 nest with `MOVE ALL "ABC…Z" TO 7-DIMENSION-TBL`; the
  pad filled the tail leaves with blanks, so the 4/5/6/7-level VARYING searches legitimately failed. Fixed in
  `EmitGroupMove` (BoundAllLiteral → RepeatToWidth at the group image width).
- **Bug (diagnosis B5): out-of-range subscripts crashed with a raw CLR IndexOutOfRangeException.** Per ISO
  §8.4.2.3.4 GR2 with subscript checking OFF (the COBOL-85 semantics — '85 has no ECs; 2002+ default until the EC
  model), a past-end reference continues benignly — and the corpus REQUIRES it (a FAIL path prints
  `ENTRY-7-4 (X1 …)` with X1 already one past its UNTIL bound). New runtime `CobolTable.At<T>(table, occurrence)`
  — ref-returning; in-range → the element; out-of-range → a per-type re-defaulted scratch slot (reads
  default/empty, writes absorbed). EVERY emitted subscript routes through it (`ReferenceResolver.AccessPath`),
  replacing the raw `[expr - 1]`. When EC checking lands, CHECKING ON maps the same point to EC-BOUND-SUBSCRIPT.
- **Legacy-oracle hole found:** the legacy binder ITSELF crashes (IndexOutOfRange) on omitted-BY with multiple
  AFTER levels — `Varying_ThreeLevels_OmittedBy` is therefore SPEC-PINNED (2×2×2 = 8 executions), not differential.

**Result: NC239A, NC240A, NC241A, NC242A, NC243A, NC244A byte-match → 33 NC locked in.** New
`PerformVaryingDifferentialTests` (8 facts incl. iteration order, index induction, inline form, ALL-repeat,
benign-OOR). **348 conformance + 15 unit green.**

