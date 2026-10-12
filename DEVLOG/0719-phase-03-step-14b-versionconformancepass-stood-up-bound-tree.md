## Entry 719 — 2026-07-09 15:42 PDT — PHASE-03 Step 14b: VersionConformancePass stood up (bound-tree arm) + the first 8 statement gates relocated

The pass exists. `src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs` is THE single post-bind edition-conformance
pass (`DESIGN-version-conformance-pipeline.md` §2.4): it runs over the bound tree between bind and emit, and the driver
HALTs before emit if it produced errors. TWO-ARM by design — this commit builds the **bound-tree arm**; the parse-tree
arm (absorbing `EditionValidator`) lands in 14e.

- **The walker (the load-bearing part).** `WalkProgram` over every program unit + every class's OBJECT/FACTORY
  `BoundProgram.Paragraphs` (a METHOD-ID is a pc slice of the same Paragraphs, so this covers every method body), then a
  COMPLETE `Recurse` that descends EVERY nested-statement container — IF/EVALUATE/inline-PERFORM/SEARCH/size-error
  (all 11 arithmetic verbs + COMPUTE + CORRESPONDING)/READ/RETURN/keyed-IO invalid-key/STRING/UNSTRING/WRITE-EOP/CALL
  ON EXCEPTION/DELETE-FILE ON EXCEPTION/SEQUENCE/EC-checked. Cross-checked against the binder's own traversals
  (`BoundStores.StoreKindOf` + the phrase fields); notably it INCLUDES `BoundKeyedDelete.InvalidKey`, which
  `StoreKindOf` treats as a leaf — a missed container would silently drop a nested gate, so completeness is the
  invariant.
- **8 gates RELOCATED** binder→pass — the constructs whose identity IS a distinctive bound-node type, each keeping its
  exact `constructId` + where-string (byte-identical diagnostics): UNLOCK (`BoundUnlock`), ALLOCATE (`BoundAllocate`),
  FREE (`BoundFree`), SET…object-reference F5 (`BoundSetObjectRef`), SET pointer UP/DOWN BY F10
  (`BoundSetPointerUpDown`), ALTER (`BoundAlter`), DELETE FILE (`BoundKeyedDeleteFile`), SET ADDRESS OF F7
  (`BoundSetAddressOfBased` + `BoundSetPointer{Address}`). The binder sites drop their `ConstructRegistry.Check` (they
  are edition-agnostic there now) with a comment pointing to the pass.
- **Driver:** `bind → VersionConformancePass.Run(bound, edition, sink) → HasErrors gate → CheckOnly? return : emit`. The
  pass runs for BOTH a full compile AND a CheckOnly verdict (the 1047-cell continuity sweep + CLI `check-batch` consume
  CheckOnly; the edition band codes come from the pass, so a bind-only verdict would silently drop them).
- **A-caveat (design-sanctioned, §4 of the pipeline design):** a gate that formerly fired BEFORE its node was fully
  resolved (e.g. UNLOCK before file resolution) now loses the below-edition diagnostic ONLY when the construct is ALSO
  malformed (an already-terminal unit that emits another diagnostic); the common case — a well-formed below-edition
  construct — produces the distinctive node, so the pass gates it. No test exercises the edge combo.
- **Battery:** edition/version-matrix tripwires **1804/1804** (VersionMatrix + the CheckOnly-driven continuity sweep +
  negative corpus + reserved-word + edition-gate), conformance **3114**, characterization **32**, unit **227**, FULL
  legacy guard NIST **353 MATCH** (0 regressions — greenfield-only).
- NEXT — **14c:** the attribute-conditioned + flag-backed statement gates (OPEN SHARING, CALL BY VALUE, GOBACK
  RETURNING, READ PREVIOUS, START FIRST/LAST/WITH LENGTH, INVOKE, CALL ON OVERFLOW, END-ACCEPT, STOP…STATUS, ROUNDED
  MODE IS, record-lock, RETRY) + the expression-descent gates (boolean/XOR operators, national/boolean literals,
  MOVE-category).

