## Entry 659 — 2026-07-07 11:23 PDT — Phase 6: TYPEDEF increment 1 — the weak-TYPE spine (a template registry + subtree clone)

Increment 1 of TYPEDEF / the TYPE clause (ISO/IEC 1989:2023 §13.18.58 / §13.18.57, COBOL-2002; data-model **D17**):
a WEAK TYPE reference now clones its type declaration's description into the referencing item. Implemented directly
from the D17 design (recon wf_5dd41937-6d8). This is the ONLY grammar/legacy-guard slice; increments 2–4 (STRONG
typing, level-88s in a TYPEDEF, staged-loud residue) are binder-only.

**Grammar (SHARED .g4 → FULL legacy guard; additive).** The TYPE-REFERENCE rule already existed (`typeClause :
{is2002()}? TYPE IS? IDENTIFIER`). Added the DEFINITION side: a `STRONG` lexer token; `typedefClause : {is2002()}?
IS? TYPEDEF STRONG? ;` on `dataDescriptionClause` (LL-disjoint from `IS? EXTERNAL`/`GLOBAL`); an
`EditionGateHints.TypedefClause` entry so a pre-2002 `TYPEDEF` probe upgrades the raw parse error to COBOLNET0900
naming COBOL-2002. Matrix/registry: the paired `typedef-def-2002` row (active) in both `constructs.json` and
`ConstructDialectStatus` — and the existing `type-clause-2002` row's source, which referenced an UNDEFINED type (it
would now emit 1530), updated to a resolvable TYPE + a TYPEDEF (drift + matrix green).

**Binder (the mechanism, zero emitter change — a weak TYPE is pure macro-expansion).** `DataItem` gains
`IsTypedef`/`TypedefStrong`/`TypeRefName` (+ `TypeName`/`StrongType` for inc 2). `BindEntry` reads the two clauses.
`BindEntries` routes a level-01 TYPEDEF root to the new **`TypeDecls`** registry — kept OFF `Roots` and (root +
subordinates) OFF `ByName` (a template allocates no storage; its names are not globally referenceable, §13.18.58.4
GR1/GR2); a subordinate TYPEDEF / FILLER / duplicate type-name is **COBOLNET1529** (`RegisterTypeDecl`). A post-build
**`ExpandTypes`** pass (at the top of `BindResolve` — after ALL `BindEntries` so forward references resolve, before
the resolution passes so the clone is a normal part of the forest they walk) clones each `TYPE IS type-name`
reference's template in: an ELEMENTARY type copies its PICTURE onto the item (a leaf, the subject VALUE/OCCURS kept —
§13.18.57.4 GR3/SR14), a GROUP type clones each subordinate under it. **`CloneItem`** generalizes the flat OO
compiler-temp clone: a FRESH `Uid` per node (StructName/ProfileName ride on it — a shared Uid would collide the
clone's emitted type/profile with the template's), the immutable `Pic` shared, `CsName` re-uniquified in the NEW
scope, and — unlike the template — REGISTERED (clones ARE referenceable). The `OccursSpec` is CLONED, never shared
(D17 risk #1 addressed proactively — its `Depending`/`CapacityRegister` are resolved per-clone by OdoResolve/
DynamicResolve, so a shared spec would let two clones of the same group type collide; verified with a group type
whose OCCURS child is referenced twice → independent storage). Unresolved / self-referential → **COBOLNET1530**. This also FIXES the silent-drop bug D17 flagged (at 2002+ `TYPE IS name` used to parse and be
dropped).

**Verified**: `typedef_weak_elem` (`77 AMOUNT TYPE DOLLARS VALUE 12.34` → AMT=0001234, +1 → 0001334 — the cloned PIC
9(5)V99 + the subject VALUE override), `typedef_weak_group` (two `TYPE FEATURE` items → independent KIND/CNT clones,
qualified refs), plus a forward reference (TYPE before its TYPEDEF → resolves), a STRONG typedef (parses + works;
the checks are inc 2), and the diagnostics (0900 at 85, 1530 unresolved, 1529 subordinate/dup). Greenfield battery
green (213 unit incl. the drift test); the FULL legacy guard (shared `STRONG`/`typedefClause` grammar). Diagnostic
band 15xx: 1529/1530 now used (1531–1535 reserved for incs 2–4). NEXT: inc 2 — STRONG typing (same-type MOVE/compare
gates).

