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
registers replace the unit's, a method data-name hides an object-level register) and
`ReferenceResolver.CapacityRegisterFor` reads it. `DeclaredCapacityRegisterTables` still yields the method tables, so
the class still emits their profiles. No new diagnostic code was needed: the sibling reference is the ordinary
COBOLNET1639 and the same-method duplicate is still COBOLNET1523.

Goldens: `pb3165_method_capacity_register_scope` (2014; SET and reads, same register spelling in two methods, an object
register shadowed by one method and read by another), negatives `pb3165-method-capacity-register-in-sibling-method`
and `pb3165-method-capacity-register-duplicates-method-data-name`. No inventory row is claimed or closed. Not fixed
here: PB3164, the same leak for a method's CONSTANT entries.
