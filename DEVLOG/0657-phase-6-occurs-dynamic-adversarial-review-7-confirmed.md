## Entry 657 — 2026-07-07 03:44 PDT — Phase 6: OCCURS DYNAMIC adversarial review — 7 confirmed defects, all fixed

A 4-dimension find→verify review of OCCURS DYNAMIC increments 2–5 (wf_3f05d472-ad8: 4 finders × emit-C#-validity /
spec-fidelity / edge-cases-guards / regression-risk → 10 candidates → adversarial verify → **7 confirmed, 3 refuted**;
1.15M subagent tokens). As on every prior leg, the review earned its keep — three of the seven are silent-wrong-answer
or mis-compile bugs that no golden had reached. All fixed in this change set, greenfield-only.

- **[HIGH] a whole-GROUP receiving MOVE into a group nested BELOW the dynamic level dropped the write.** The
  `EmitGroupMove` distribution switch (`CSharpEmitter.cs`) had no `DynTablePlace` arm, so the `_ =>` fallback used
  `target.Read()` = the SENDING path (`RefSending`), which returns benign scratch for an occurrence past the current
  capacity — the table never grew and the MOVE was silently lost (the elementary sibling correctly used
  `RefReceiving`). Added a `DynTablePlace dyn => "{dyn.ReceivingPath}.FromImage(...)"` arm (§8.5.1.9.3). The
  reviewer's own repro was WRONG (a leaf directly under the dynamic level hits the image-capable loud guard, never
  the switch) — it reproduces only with a group nested a level deeper; the review caught the real shape by running
  both. Golden `dyn_nested_group_move` (CAP grows 2→5, the value lands).
- **[MED] MOVE CORRESPONDING mis-compiled over a dynamic-table member.** `CorrEligible` gated on `item.Occurs is null`
  (null for a dynamic table) instead of `!item.IsTable`, so a Format-4 element was wrongly eligible → member access
  emitted on the `CobolDynTable<T>` field → CS1061, aborting the whole program's compile. §14.7.6 rule 4 excludes any
  OCCURS item. Golden `dyn_corr`.
- **[MED] SEARCH of a dynamic table nested under a fixed OCCURS silently scanned ZERO.** `SearchBound` uses
  `refs.TablePath`, which returns null for a nested table (ambiguous whole-table path) → the bound fell back to
  `Count=0` → immediate AT END. Now BindSearch/BindSearchAll fail LOUD for a nested dynamic table (a subscripted
  current-capacity path over the enclosing indices is a later increment) rather than a wrong answer.
- **[MED] OCCURS DYNAMIC in the FILE SECTION was silently accepted.** §8.5.1.9.1 item 3 — a dynamic table "may be
  defined in any place, OTHER THAN the file section" (its out-of-line `CobolDynTable` has no place in a record image).
  Added a placement guard **COBOLNET1526** in `DynamicResolve` (rejecting a dynamic table whose storage root is an
  FD/SD record). 1526 was free — the ref-mod guard it was reserved for was SKIPPED in inc 5 (ref-mod of a dynamic
  element works). Guard test `FileSectionDynamicTable_Rejected1526`.
- **[MED] the SET Format 14 capacity peek double-resolved an OO property.** `DynTryBindSetCapacity` (inc 2) called
  `refs.Resolve(targets[0])`, which routes an unresolved `prop OF obj` first target through the property hook and
  enqueues a spurious `OoPendingPropertyOp` (→ a phantom getter / duplicate 0843). Replaced with a PURE
  `ReferenceResolver.CapacityRegisterFor` peek (a `CapacityRegisters` map lookup, no side effects); the `Resolve`
  early hook now shares it. (A separate pre-existing property-receiver double-resolve — MOVE into a property with no
  REPOSITORY specifier also duplicates 0843 — is out of scope; my fix makes SET match MOVE, removing the peek's
  extra resolve.)
- **[LOW] a GROUP dynamic table with a subordinate VALUE + a TO silently opened at FROM.** §13.18.63 GR16: a VALUE in
  the DYNAMIC entry OR any SUPERORDINATE entry derives the initial capacity (GR16b: no-TO-in-VALUE → the OCCURS
  expected capacity). The inc-5 1528 guard only caught the elementary case; extended it to the group case WHEN the
  OCCURS has a TO (an expected capacity for GR16b). A subordinate VALUE with NO TO has no expected capacity → the
  §14.6.2.3.2 default (capacity = FROM, elements seeded) applies and stays supported (the `dyn_initialize`/
  `dyn_initialized` goldens don't false-trip). Guard test `GroupSubordinateValueWithTo_Rejected1528`.
- **[LOW] the `CobolDynTable.GrowTo` docstring falsely claimed EC-BOUND-OVERFLOW was "wired".** Corrected — the
  nonfatal EC-BOUND-OVERFLOW (implicit growth past expected, §8.5.1.9.6) and EC-BOUND-SET (explicit SET past it,
  §14.9.39 GR30) are checking-gated and, being nonfatal, are observationally identical with checking OFF (the
  default); `_expected` is captured for that flagged follow-on.

**Refuted (3):** a SET F14 negative/non-integer operand (the GR29 premise was off); INITIALIZE of a group CONTAINING a
dynamic table being loud (that IS the intended staged behavior); the "dead `_expected` + mis-named EC" as a
standalone defect (folded into the docstring fix). **Battery: 1997 conformance · 213 unit, 0 failures.** Diagnostic
band 15xx→1528 (1526 now used). ⚠ LESSON re-reinforced: the review's per-finding repros were sometimes WRONG in
their specifics (the group-MOVE leaf level; the property repro shape) but named REAL bugs — verify-by-running both
confirms the defect AND corrects the repro. NEXT: TYPEDEF (recon wf_5dd41937-6d8 ready — net-new design D17).

