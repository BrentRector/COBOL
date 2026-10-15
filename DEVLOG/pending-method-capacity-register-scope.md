## A method's CAPACITY register is a name of the method, not of the unit (PB3165)

A table declared in one method with `OCCURS DYNAMIC CAPACITY IN Q2` could be read from a sibling method that declared
nothing, and the backend then failed with CS0103 because the table is a C# local of the first method only. The
re-probe (`r9.cob`) reproduced it on this tree.

Root cause: `DataBinder.CapacityRegisters` is one map for the whole unit, the one name class of a data description
entry that `OoScopeSubtree` never moved into the method's scope (data-names, level-88s and index-names all were). The
same flat map had two more consequences, found by probing the sibling arms: two methods that each write
`CAPACITY IN Q2` drew COBOLNET1523 on legal source (§13.18.38.3 SR30's "elsewhere in the source element" was judged
across the whole class, but a method is a source element of its own, §10.5), and a method register spelled like an
object-level data-name was a false duplicate (§11.7.4 GR5: the object's declaration is inaccessible to the method).

Fix shape: the register has the visibility of the entry that implicitly defines it. A method's registers live in
`OoMethodDataScope.CapacityRegisters`; `DynamicResolve` registers into the owning scope and judges the duplicate and
the one-type-per-word census there; `SymbolTable.CapacityCandidates` is the one scope-aware lookup (a method's
registers replace the unit's, a method data-name or index-name hides an object-level register) and
`ReferenceResolver.CapacityRegisterFor` reads it. `DeclaredCapacityRegisterTables` still yields the method tables, so
the class still emits their profiles. No new diagnostic code was needed: the sibling reference is the ordinary
COBOLNET1639 and the same-method duplicate is still COBOLNET1523.

Goldens: `pb3165_method_capacity_register_scope` (2014; SET and reads, same register spelling in two methods, an object
register shadowed by one method and read by another), negatives `pb3165-method-capacity-register-in-sibling-method`
and `pb3165-method-capacity-register-duplicates-method-data-name`. No inventory row is claimed or closed. Not fixed
here: PB3164, the same leak for a method's CONSTANT entries.

Branch review round 1 (R1-1): the lookup applied §11.7.4 GR5's hiding to a method-local DATA-name only, so a method's
own `INDEXED BY QO` did not hide an object-level `CAPACITY IN QO`, and `SET QO TO 3` in the method resized the object's
table. `CapacityCandidates` now also yields nothing when the method declares the spelling as an index-name; the golden
gained a method `SHOWD` that exercises it (its `SET N TO QO` was also refused with COBOLNET2326 before).

Finisher (the landing's Linux unit leg was RED): `CapacityRegisterReferenceDriftTests` still pinned the one register-name
lookup to `ReferenceResolver.cs`, and this change had moved it, as it had to, into `SymbolTable.CapacityCandidates`, the
one scope-aware name resolver, which now reads two maps (the method's own and the unit's). The test now pins that shape:
no file but `DataBinder.Odo.cs` and `SymbolTable.cs` reads a register map, and the new
`TheOneLookup_HoldsEveryRead_AndHasOneCaller` holds every `SymbolTable.cs` read inside `CapacityCandidates`, whose one
caller is `ReferenceResolver.CapacityRegisterFor`. Why the Windows gate never ran it: the class reached `src` through a
private `Path.Combine` helper that `drift_rules.py` cannot read, so it was neither a specific rule nor a tree-wide
sweep; it now uses `TestRepo.Src` and carries `[Trait("Sweep", "tree")]` (tier 0c). The one-place scan itself still
misses tier 0c: the shared timings store holds a single 24.5 s sample of it (about 0.1 s alone), over the planner's
2 s sweep limit, and a leg-1-only gate never re-measures a case it does not run.
