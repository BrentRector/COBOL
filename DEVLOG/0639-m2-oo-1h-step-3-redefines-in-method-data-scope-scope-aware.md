## Entry 639 — 2026-07-06 23:25 PDT — M2-OO-1h step 3: REDEFINES in METHOD data scope — scope-aware target + Tier-B backing routing

Step 3 (the most involved) of the M2-OO-1h data-model residue. REDEFINES in method data had two faults: a
top-level 01/77 method redefiner resolved its target through the global `Roots` pool (`ResolveRedefines:1123`
used `item.Parent?.Children ?? Roots`, and a top-level redefiner has `Parent == null` → the cross-scope global,
matching the wrong same-named anchor); and a Tier-B string backing (`_redef_X`) for a method-scoped canonical was
not storage-routed (static-mismatch for method-WS; leaked as a class field for a method LOCAL/LINKAGE table).

- **Resolution (reuses the step-2 B.0 infra):** `ResolveRedefines` scopes a top-level method redefiner's target to
  the OWNING METHOD's own roots (`OoRootOwner` → `m.StaticRoots.Concat(LocalRoots).Concat(LinkageRoots)`), else
  `Roots` (program unchanged). A subordinate (02+) redefiner already scoped to `item.Parent.Children` (correct).
  A method 01 REDEFINES whose target isn't in the method's own roots → **COBOLNET1518** (§13.18.44.3 SR — a method
  item may not redefine object/program data), replacing the old silent cross-scope bind.
- **Backing routing:** new `OoRouteMethodRedefinesBackings` (after `ClassifyRedefinesClasses`) routes each
  method-scoped Tier-B class's `_redef_X`: a method-WS canonical → `OoStaticRootFields` (emits `static`, matching
  the static root); a method LOCAL/LINKAGE canonical → `CallSuppressedRootFields` (suppressed from the class field
  loop) + emitted as a method LOCAL in `OoEmitMethod` (new `FieldEmitter.MethodRedefinesBackingDecl` — the backing
  is the storage, not the root struct; a LINKAGE formal seeds it from the caller image). Subordinate canonicals
  ride the root's composed initializer, unchanged.

Goldens: `oo_method_redefines_local` (a subordinate REDEFINES `PARTS`/`FULL` → P1=ABC/P2=DEF, and a 01-level
Tier-B `NCHARS X(4)` over `NUM 9(4)`=1234 → NC=1234, backing as a method local) + `oo_method_redefines_ws` (method
WS Tier-B `CCHARS`/`CNT`, invoked twice — the STATIC backing persists: CNT=0001→0002), both byte-exact,
GreenfieldOnly. +1 OoSpine unit (`MethodRedefines_TargetInObjectScope_Rejected` → 1518). Battery: 121 corpus
goldens · full greenfield battery GREEN. Remaining M2-OO-1h: step 4 (OCCURS INDEXED BY — the per-method index
namespace).

