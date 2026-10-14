## PB3154: the class definition binds its environment division once, and its factory and object definitions inherit it

**Mechanism.** A class definition, its FACTORY and its OBJECT definition are three source elements (§10.5 "A source
element is a source unit excluding any contained source units"; §3.165, Annex D.6.1.1), and §8.3.2.2's one-type-per-word
rule binds only "Within a source element". `OoDriver.BindClassData` handed the class definition's ENVIRONMENT DIVISION to
both half binders as part of their own synthetic unit, so every word the class's REPOSITORY and SPECIAL-NAMES declare was
declared again in the factory's census and in the instance census, which `OoClassTable.CensusOf` moreover mapped onto the
class's own (so the CLASS-ID too). A factory data item spelled like a class the REPOSITORY names (the note's repro), its
OBJECT twin, an alphabet-name the class's SPECIAL-NAMES declares, and an object data item spelled like the CLASS-ID were
all COBOLNET2692 on legal source (§8.4.6.1: "each source element may use identical user-defined words to name items
independent of the use of these user-defined words by other source elements"). Re-probed on this build: both repros
refused before, both compile after.

**Fix.** The class definition gets a binder of its own element (`OoDriver.ElementBinder`, now the one DataBinder
constructor for every OO element), which binds the class's environment division and declares the CLASS-ID; each half
seeds itself from it with `DataBinder.InheritConfiguration` (§12.3.4 GR1, the contained-program model of kb/Work PB990)
and binds only its own divisions (`OoDriver.OoReparent`, which replaces the two reparent copies, closing kb/Work PB2160).
`CensusOf` gives the OBJECT paragraph its own census. Every unit now carries one environment division, so
`DataBinder.EnvDivisions` (the reverse walk over [half env, class env]) is deleted and its six callers, `MnemonicRegistry`
included, read `environmentDivision()`. The architecture-review design's planned `ClassConfiguration` value is the same
shape, and its premise paragraph is updated.

**Sweep.** Two siblings, both fixed: `InheritConfiguration` never carried the SYMBOLIC CHARACTERS table, so a contained
program naming its container's symbolic-character was COBOLNET1639 "not defined" (the OO halves now depend on the same
copy); and an OBJECT data-name spelling the CLASS-ID, legal after the fix, emitted a C# field named like its enclosing type
(CS0542), so `DataBinder.ReserveClassMemberNames` (renamed from `ReserveMethodMemberNames`) reserves the type name beside
the methods'. The interface arm already had one element per binder; the method arm has its own census (PB2921).

**Goldens.** `2002/pb3154_half_data_named_like_class_words` (expected output derived from the rules: the factory and
object VALUEs, the native character at ordinal 67, "B", and the class condition true for "123");
`85/pb3154_contained_symbolic_character`; negative `pb3154-object-data-spells-a-class-it-references` (the object element
also USES the class-name: COBOLNET2692 at 2002, 2014 and 2023). No inventory row changes verdict.

**Review round 1 (R1-1, fixed).** The branch review found that the new SYMBOLIC CHARACTERS inheritance had no
shadowing: a bare reference consults the literal aliases (constant, then symbolic) BEFORE data, and only the constant
twin had a nearer-name check (`DropShadowedConstants`, kb/Work PB1047). So a contained program's `01 S1` spelled like its
container's symbolic-character, and an OBJECT data item spelled like the class's, read the figurative instead (main
printed `INNER Z`, the branch `INNER B`; §8.4.6.1 "even when it is a different type of user-defined word, is to the item
which that source element describes"). Fix: each inherited symbolic carries its containment distance
(`InheritConfiguration`), and `DropShadowedConstants` (a symbolic-character defines a figurative CONSTANT, §12.3.7.4
GR11 a) now hides every inherited constant or symbolic a nearer data-, condition- or index-name declares, through one
predicate, `DeclaredNearerThan` (§8.4.6.2.1 3) b)). A shadowed symbolic is hidden, never removed: a contained unit
inherits the whole table one level at a time, and a middle program's LOCAL `S1` hides nothing from the program it
contains. The OO halves run the same step after they bind. Sibling arm swept and fixed: a METHOD's own data-, condition-
or index-name hid neither the class's symbolic nor the object's constant-name (pre-existing on main; `MOVE K` in a method
declaring `01 K` read the object's `CONSTANT K`); both literal-alias lookups (`FindConstant`, `SymbolicOf`) now ask
`ActiveMethodDeclares` first (§8.4.6.2.1 3) a)). Goldens `85/pb3154_contained_symbolic_shadowed` (local, index-name,
middle GLOBAL and middle-local tiers) and `2002/pb3154_half_data_hides_class_symbolic` (object, factory and method tiers);
both differ from the pre-fix build. Left as a lead, a different mechanism: a method's own CONSTANT entries live in the
class binder's one table, so a sibling method reads them and two methods declaring the same constant-name with
different values are refused COBOLNET1547.

**Review round 2 (R2-1, fixed).** The round-1 predicate `DeclaredNearerThan` asked the data-name, condition-name and
index-name indexes, but a named OCCURS DYNAMIC CAPACITY register is a data-name kept off `ByName` (§13.18.38.3 SR30), and
so is a REPORT SECTION sum counter (§13.18.54.4 GR5). A contained program's `CAPACITY IN S1` spelled like its container's
symbolic-character moved the figurative (the branch refused `MOVE S1 TO N` with COBOLNET0902 at 2023; main printed
`INNER 003`); the same register spelled like a container's GLOBAL constant read the constant (`INNER 005`, pre-existing
on main); the OO halves and a method's own register likewise. Fix: the predicate asks both indexes (a
method's table is the method tier, excluded at the object tier), `ActiveMethodDeclares` asks the method's own registers,
and `GlobalNameClassDriftTests.DeclaredNearerThan_AsksEveryInheritedNameIndex` ties the predicate to the inherited
name-index list so the next name index is asked too. Goldens `2014/pb3154_capacity_register_hides_alias`,
`2014/pb3154_half_capacity_register_hides_symbolic` and `2002/pb3154_sum_counter_hides_symbolic`, each different on the
pre-fix build. Left as a lead, a different mechanism (pre-existing): a method's named CAPACITY register is visible to its
sibling methods (`ReferenceResolver.CapacityRegisterFor` reads the class binder's one register index with no method
filter), and a sibling's `DISPLAY` of it fails C# compilation (CS0103).
