## Entry 498 — COBOL.NET G5 prep: nested Tier-B REDEFINES backing path (the NC101A blocker)

G5 (file I/O) kickoff. Advisor's de-risk step first: compile NC101A as-is through CobolNet *before* building the
files subsystem (the file verbs fall to compilable loud guards), to catalog what the program's gnarly DATA DIVISION
breaks. Result: the compiler does NOT crash — it emits 3818 lines of C#; the backend fails with **304 errors, ALL of
one class**: `The name '_redef_COMPUTED_A' / '_redef_CORRECT_A' does not exist in the current context`. Nothing else
(the edge PICs `SP(8)9`/`99P`/`P(4)9`/numeric-edited/COMP, the many `S9(n)V9(m)` REDEFINES pairs all bind clean). So
one REDEFINES bug — NOT a file-I/O bug — is the entire data-division blocker for NC101A.

**The bug.** A **nested** Tier-B REDEFINES class (CCVS `COMPUTED-X`: `03 COMPUTED-A PIC X(20)` redefined by
`COMPUTED-N PIC -9(9).9(9)`, `CM-18V0`, … — a class nested at level 03 inside two groups) emits its single string
backing `_redef_COMPUTED_A` as a **member of the containing record struct** (`_T_120`), correctly. But
`ReferenceResolver` built the `RedefViewPlace` with the **bare** `BackingCsName`, so a DIRECT procedure-division
reference to a nested view/canonical (CCVS BAIL-OUT `IF COMPUTED-A NOT EQUAL TO SPACE`, FAIL-ROUTINE
`MOVE COMPUTED-A TO XXCOMPUTED`) emitted an unqualified `_redef_COMPUTED_A` that is out of scope at the program level.
A bare name resolves ONLY for a top-level (static-field) class. The pre-existing `InGroup_TierBBacking_NestedImage`
test missed it because it referenced the nested backing only via the outer group's `AsImage()` — which is defined
INSIDE the struct, where the bare name IS in scope.

**The fix.** `ReferenceResolver.BackingPath(cls)`: a nested class's backing is reached through its containing struct's
access path (`OUTER.GROUP._redef_X`) via `AccessPath(canonical.Parent, [])`; a top-level class keeps the bare static
field; loud-null when the path is unavailable (canonical within an OCCURS — a later slice) rather than emit an
unqualified reference. The backing DECLARATION (FieldEmitter, the bare member name within its struct) was already
correct — only the reference site needed qualifying.

**Verification.** NC101A's 304 backend errors → 0 (it now compiles end-to-end, file verbs as loud guards). Three new
`RedefinesTierBDifferentialTests` reproduce the exact NC101A forms — read a nested canonical directly, write a nested
numeric view then read the nested alphanumeric canonical, and the BAIL-OUT `IF nested-view = SPACE` comparison — each
pinned to the legacy oracle. Conformance 210 → 213, 13 unit, all green. Greenfield-only; the shared front-end and the
legacy oracle are untouched (legacy guard unaffected). NEXT (the G5 body): the sequential files subsystem so NC101A
runs end-to-end.

