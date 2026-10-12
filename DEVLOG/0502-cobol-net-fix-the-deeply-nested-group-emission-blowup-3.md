## Entry 502 — COBOL.NET: fix the deeply-nested-group emission blowup (3 compiler hangs → compile in ~1.5s)

The corpus map (Entry 501) flagged 3 NC programs that HUNG the compiler (NC126A/NC207A/NC246A). Diagnosis: not the
shared front-end (the legacy compiles them fine), and not an infinite loop — an **exponential O(2^depth)** blowup in
the DATA-DIVISION emitter. NC126A nests a group ~49 levels deep with a leaf AND a subgroup at each level (the CCVS
`GP-n`/`GPLEVEL-n` shape). `FieldEmitter.PhysicalFields`, building each level's fields, computed every group child's
physical *width* (`PhysicalImageWidth`) and *composed initializer* (`FieldInit`) independently — and each recursed
into the child's subtree via `PhysicalFields` again. So each level spawned ≥2 full recursive descents into the next:
2^depth. Confirmed empirically: depth 18 → 1.2s, 24 → 9.3s, 28 → hang (×~7 per +6 levels).

**Fix — memoize.** `FieldEmitter` now caches the physical-field list **per item** (`PhysicalChildrenOf`, plus the
root forest in `_rootPhysCache`); every consumer (field decls, AsImage/FromImage, a parent's width, the composed
initializer) reuses one computed list per node, so emission is **O(total items)**. The rewrite is behavior-transparent
(the cached `Physical` carries the same Width/Init it always computed). depth 48 now compiles in ~1s; NC126A/207A/246A
in ~1.5–1.9s. New `DeepNestingTests.DeeplyNestedGroup_CompilesInLinearTime` (depth 45, asserts a prompt finish) guards
the regression. Conformance 232 (unchanged — transparent), unit 13 → 14, green. Greenfield-only.

A production compiler must never hang on legal source; these three now fail (if at all) the way every other
unsupported program does — a clean loud guard or diagnostic — not a wedge.

