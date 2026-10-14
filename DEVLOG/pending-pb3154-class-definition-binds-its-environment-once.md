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
