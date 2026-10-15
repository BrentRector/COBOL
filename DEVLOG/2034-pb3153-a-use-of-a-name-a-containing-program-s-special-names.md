## Entry 2034 — 2026-10-14 16:31 PDT — PB3153: a use of a name a containing program's SPECIAL-NAMES declares is a §8.3.2.2 use in the using element; A method's CAPACITY register is a name of the method, not of the unit (PB3165)

### PB3153: a use of a name a containing program's SPECIAL-NAMES declares is a §8.3.2.2 use in the using element

**The defect (kb/Work PB3153, wave 1078 G).** ISO §8.3.2.2 says "Within a source element, a given user-defined word
may be used as only one type of user-defined word". A contained program that writes `IF X IS DIGS` over its
container's `CLASS DIGS` and also declares a paragraph `DIGS.` uses DIGS as two types in its own source element, yet
it compiled and ran (`DIG`, `IN`) while the flat program with the same two words was refused COBOLNET2692. The census
saw declarations and, since PB3132, object-class-name references; the class-condition, alphabet, mnemonic,
symbolic-character, switch-condition, locale, ordering and dynamic-length-structure lookups recorded nothing, and the
inherited name is declared in ANOTHER element. Re-probed on this worktree's build: reproduced. A sibling arm surfaced
at once: `InheritConfiguration` copied every SPECIAL-NAMES registry except the SYMBOLIC CHARACTERS, so a contained
program's `MOVE BEL TO X` over its container's symbolic character was refused COBOLNET1639 on conforming source
(§8.4.6.1 lists symbolic-character among the names a contained element may reference; §12.3.4 GR1).

**The fix, as a shape.** The seven data-binder registries are now `ConfigurationNames<T>` tables with no
`TryGetValue`: a reference the binder commits to resolves through `TryUse(site, name)`, which records the use in the
census of the element the site lies in through ONE recorder, `DataBinder.RecordConfigurationNameUse` (into PB3132's
`OoClassTable.RecordReferenceUse`); a predicate, a duplicate check or a re-read asks the non-recording `Declares` /
`TryPeek`. The mnemonic-names keep their parse-tree registry, whose map is now private behind the same `TryUse` /
`TryPeek` pair. So a new consumer of any of these names cannot resolve one without choosing, and
`UserWordDeclarationDriftTests.EveryConfigurationSectionNameType_ResolvesThroughARecordingTable` parses §8.4.6.1's
eight-type list from the standard and keeps every type's registry a recording one. The symbolic characters inherit
with their siblings.

**Where the two readings differ.** §8.4.6.1's second sentence ("a source element's reference to such a name, even when
it is a different type of user-defined word, is to the item which that source element describes") refuses the same
programs the census does, and decides one more case: where a format admits an identifier as well as the inherited
name, the element's own item wins. Inheriting the symbolic characters would otherwise have let an inherited `BEL`
shadow a contained program's own `01 BEL` (a wrong answer) and record a false use (COBOLNET2692 on legal source), so
the three such positions (a bare literal-alias operand, `SET LOCALE ... TO`, `WRITE ... ADVANCING`) ask the reference
resolver first.

**Goldens.** `negative/pb3153-contained-program-paragraph-spells-an-inherited-class-it-uses` (reject-at 85 2002 2014
2023; its second contained program declares the same paragraph without the use and draws nothing);
`85/pb3153_inherited_special_names_words` (`A`, `DIGIT`, `Q`, `PARA`: the inherited symbolic character is ordinal 66 of
the native set, §12.3.7.4 GR11 b; the local data item named like it wins); `InheritedConfigurationNameUseTests`, one
row per §8.4.6.1 type at its first edition plus a method over its class's SPECIAL-NAMES. No inventory row isolates the
rule; the note closes none (reason recorded).

**Self-review.** Architecture: one recorder, one table type, the funnels pinned by the drift tests. Full code: every
lookup site was classified as a use or a non-use; the class-condition alphabet arm keeps its fall-through to the
national table. Performance: one dictionary operation per committed use; the added probes run only when an inherited
name matched. Duplication: the two-map "is an alphabet-name" test that four sites spelled out now calls
`IsAlphabetName`.

**Gate.** The first gate (run 20261014T222553Z-2f9d3a) was RED on one case:
`LiteralAliasResolutionDriftTests` reads `LiteralAliasOf`'s body up to its first semicolon, and the new §8.4.6.1
comment inside that expression carried one; the comment now uses a comma. The re-gate, run 20261014T223239Z-4450f2:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 6,174/11,694, Unit 33,048/33,637,
Characterization 6/36 in leg 1; leg 2 left to CI). `scripts/semgrep/verify.py` PASS, every count unchanged.

**Review round 1.** The branch reviewer found the same trap at four more positions that admit a second reading, each
refusing legal source with COBOLNET2692 on the branch: FUNCTION STANDARD-COMPARE's three-argument position 3 (an
inherited ordering-name shadowed the element's own integer argument-4), the EVALUATE range's trailing `IN word` (an
inherited alphabet-name claimed `HI IN GRP` over the element's own group GRP), the VALUE clause's IN-omitted alphabet
peel (an inherited alphabet-name claimed the element's own constant-name; main answered N there, a wrong answer that
predates this branch), and the VALUE operand reader, which asked the now-inherited symbolic-character before the
constant-name. Each now asks the element's own reading first, under §8.4.6.1's second sentence; the four-argument
form's argument-4 yields too, while its position 3, which admits the ordering-name only, keeps the census refusal.
Golden `2014/pb3153_inherited_names_yield_to_own_items` runs both readings of each position (`IN Y < A`, then
`OUT Y < Z`). The finisher then classified every configuration-name lookup in `src`; none other admits a second
reading without asking the own reading first, and two configuration-section names of different types cannot meet
across elements, because neither a contained program nor a method has a configuration section (§12.3.3 SR1, SR2).
The finisher's gate, run 20261014T225418Z-18b752: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`
(Conformance 6,172/11,701, Unit 33,090/33,652, Characterization 6/36 in leg 1; leg 2 left to CI); semgrep verify PASS.

**Review round 2.** The reviewer found round 1's own-item test asked only the data-item probe, so an own
constant-name (§13.10.3 SR2: "may be used anywhere that a format specifies a literal of the class and category of
constant-name-1") at STANDARD-COMPARE's argument-4 was still claimed as the inherited ordering-name and refused
COBOLNET2692. The sweep found the same raw probe at WRITE ADVANCING integer-1 and an EVALUATE object (both reproduced
2692). One own-item test, `ReferenceResolver.DescribesOwnItem` (constant-name or data item), now serves every
position that pairs a configuration-section-name lookup with the own reading, and a drift test refuses the raw-probe
pairing. The golden gains PB3153Y3 (three own constant-names) and an inherited class-name arm; the unit theories gain
constant-name rows. A CALL / CANCEL target constant-name, refused COBOLNET1639 even in one element (PB780's
territory), is left as a lead.
The round-2 finisher's gate, run 20261014T231215Z-47b403: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515):
GREEN` (Conformance 6,192/11,704, Unit 33,104/33,653, Characterization 6/36 in leg 1; leg 2 left to CI); semgrep
verify PASS.


**Merge of main (PB3154).** Bringing origin/main in conflicted with PB3154, which landed meanwhile: it made a
contained unit inherit its container's SYMBOLIC CHARACTERS with a containment tier and HID an inherited
symbolic-character that a nearer data-, condition- or index-name (or the method being bound) declares (§8.4.6.1,
§8.4.6.2.1 3)). Both intents are kept: the symbolic table stays a `ConfigurationNames` table inherited through
`InheritFrom` (which now reports each name it takes, so the tier is recorded in the same pass), and ONE hiding test,
`DataBinder.HidesSymbolic`, guards the resolving lookup `SymbolicOf(site, word)` and the predicates `NamesSymbolic`
alike. A hidden symbolic-character is therefore neither resolved nor recorded as a §8.3.2.2 use: the word references
the nearer item. The two predicates this branch had moved to `SymbolicCharacters.Declares` (the VALUE-literal test
and `LiteralEnvironment.IsSymbolicCharacter`) ask `NamesSymbolic`, so main's hiding reaches them as it did through
main's `SymbolicOf(string)`.
The merged tree's leg 1 was GREEN but did not run main's `2014/pb3154_half_capacity_register_hides_symbolic`; run by
name it failed CS0103 ('U' does not exist). The merge did not cause it, it exposed it: this branch's own-item test
(`ReferenceResolver.DescribesOwnItem`) asks the resolver first, and `ReferenceResolver.CapacityRegisterFor` read the
class binder's one register index with no method scope, so method SHOWB's `DISPLAY S2` reached SHOWA's LOCAL-STORAGE
CAPACITY register instead of the class's symbolic-character. On main the same leak made a sibling method's reference
to such a register compile to C# that Roslyn refused, where §8.4.6.1 gives COBOLNET1639. A method's register is now
reached only from that method (`DataBinder.OwningMethodScope`, shared with `ActiveMethodDeclares`), with the
negative `pb3153-method-reaches-a-sibling-methods-capacity-register`. Sibling sweep: a method's CONSTANT entry has the
same leak (a sibling method reads it, no diagnostic), reported as a lead: the constant table carries no owning method.

### A method's CAPACITY register is a name of the method, not of the unit (PB3165)

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

Finisher (the merge queue bounced pull request #81, run 38794330038): both conformance `corpus-nist` shards failed
`pb3154_half_capacity_register_hides_symbolic` (2014) with `A2 CCC` for `A2 004`. That golden reached main from PB3154
after this branch's gate, and the failure is the interaction of the two changes, a semantic conflict with no textual
one. PB3154's method tier, `DataBinder.ActiveMethodDeclares`, found a method's own CAPACITY register in the CLASS's
register index; this change moved those registers into `OoMethodDataScope.CapacityRegisters`, so the method's register
`S2` no longer hid the class's `SYMBOLIC CHARACTERS S2` and `MOVE S2` moved the figurative (§8.4.6.1, §8.4.6.2.1 3) a),
§11.7.4 GR5). The root was a hand-written list of the method's name indexes kept in the binder, away from the scope that
owns them. The predicate is now `OoMethodDataScope.Declares`, beside the indexes, asking every one of them;
`ActiveMethodDeclares` and `SymbolTable.CapacityCandidates` (whose own hand list missed condition-names) both ask it, and
`DeclaredNearerThan` lost its now-dead method-table filter. `GlobalNameClassDriftTests.MethodDeclares_AsksEveryMethodNameIndex`
finds the scope's name indexes by reflection and holds `Declares` to them. `CapacityRegisterReferenceDriftTests` now
scans case-insensitively (the unit's private `_capacityRegisters` escaped it) and allows a register-map read only in the
one resolver and the two shadowing predicates (`AShadowingPredicate_IsItsFilesOnlyRegisterRead`). The golden
`pb3165_method_capacity_register_scope` gained `SHOWE`, a method condition-name spelled like the object's register.
`GlobalNameClassDriftTests` reached `src` through `Path.Combine`, which `drift_rules.py` cannot read, so it governed no
file it scans; it now uses `TestRepo.Src`, the same fix the first finisher made to its sibling above.
Why no local gate saw the bounce: the golden arrived with main's second merge at the landing, which by design runs no
second gate (the merge group is the whole population on the moved main).

Review finisher (branch review R2-1 at 4d1893ded): `SymbolTable.IndexCandidates` still carried the hand-written method
tier that the bounce fix had replaced in `CapacityCandidates`. It asked only the method's data-names and index-names and
then fell through to the object's index-names, so a method CAPACITY register `QI` spelled like an object index-name `QI`
made `COMPUTE N = QI` draw COBOLNET1637, legal source refused (ISO §11.7.4 GR5: "The declaration in the containing object
definition is inaccessible to this method"; §8.4.6.1 "even when it is a different type of user-defined word"). The sweep
of the other method tiers in `SymbolTable` found the same shape twice more, both accepting illegal source: `TryResolve`
let `MOVE X TO N` read the object's data-name `X` under a method condition-name `X` (printed `X 0007`), and
`TryResolveCondition` let `IF X` test the object's level-88 `X` under a method data-name `X`; a method condition-name
`QI` also let `SET QI TO 2` move the object's index. Every method tier now answers from its own class's method index,
then asks `OoMethodDataScope.Declares` and stops before the unit's maps. The new
`GlobalNameClassDriftTests.EverySymbolTableMethodTier_AsksTheMethodsWholeDeclaration` requires `m.Declares(name)` in every
`SymbolTable` member that opens a method tier; with the `TryResolveCondition` line removed it fails naming that member.
The golden gained `SHOWF` (`F 0003`, the method's own register at its minimum capacity) and three negatives pin the
siblings: `pb3165-method-condition-name-hides-object-data-name` (COBOLNET1639),
`pb3165-method-data-name-hides-object-condition-name` (COBOLNET2318) and
`pb3165-method-condition-name-hides-object-index-name` (COBOLNET1639), rejected at 2002, 2014 and 2023.

### Review round 3 of PB3153 and the merge with PB3165 (wave 1085 D)

PB3153's enqueue finisher and PB3165's branch fixed the same leak in two designs: a method's named CAPACITY register
reached a sibling method's references. The finisher filtered the class binder's one register index by owning method
at reference time (`DataBinder.OwningMethodScope`). PB3165 moved a method's registers into the method's own scope
(`OoMethodDataScope.CapacityRegisters`). This successor merged both branches (`worktree-wf_d6f41ebf-faa-3`, which
carries PB3165 and its review fix, and `worktree-wf_15ec6338-3a0-19`) and kept the scope-owned design. With it, every
reader of the name sees only the method's own registers. Those readers are `SymbolTable.CapacityCandidates`,
`OoMethodDataScope.Declares`, and the §13.18.38.3 SR30 duplicate test in `DataBinder.DynamicResolve`. The merge deleted
`OwningMethodScope` and PB3153's negative `pb3153-method-reaches-a-sibling-methods-capacity-register`, which duplicated
`pb3165-method-capacity-register-in-sibling-method`. It also corrected the interprogram design doc's sentence about it.

Review finding R1-2 was the gap in the reference-time design. The SR30 duplicate test still read the whole class's
index, so two methods that each declare `CAPACITY IN R2` were refused COBOLNET1523 (`two.cob`). On the merged build
`two.cob` prints `A 004` / `B 002`. Golden `2014/pb3165_method_capacity_register_scope` (SHOWA and SHOWB both declare
Q2) is its regression witness, and its header now names the finding. R1-1 (PB3165 left open) is answered in the
register: PB3165's note records which branch carries the fix and that it lands through this one. The CONSTANT lead is
PB3164, so no duplicate note is filed.

Merging main brought in PB3164, which keeps a method's constant entries in `OoMethodDataScope.Constants`. That exposed
a gap between the two fixes. `OoMethodDataScope.Declares`, which every `SymbolTable` method tier asks, did not ask the
method's constants, and the drift fact that holds it to every name index of the scope did not count a `ConstantScope`.
So a method's own `01 X CONSTANT AS 7` let `IF X` test the object's level-88 `X`, and a method's `01 QI CONSTANT AS 2`
let `SET QI TO 4` move the object's index-name (`mc5.cob` printed `A COND` / `A 1`). §11.7.4 GR5 makes the word mean the
method's declaration. `Declares` now asks `Constants.Declared`, and the drift fact counts a `ConstantScope` field as a
name index (a mutation check removing the new clause fails it, naming `Constants`). The method's references now draw
the same diagnostics as a program whose only `X` or `QI` is a constant: COBOLNET1639 and COBOLNET1548. New negatives
`pb3153-method-constant-hides-object-condition-name` and `pb3153-method-constant-hides-object-index-name` (2002, 2014,
2023); the 2002 golden `pb3164_method_constant_scope` gains ME (`ME 7 2`) and MF (`MF 5`, `MF CX`).

The two stamped entries the merged branches carried (2034 and 2047) are folded into this one, because a landing adds
exactly one entry.

Gate (run 20261015T211341Z-691c81 on `3a76cd1c8`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN —
leg 2 left to CI's whole-population run … Conformance 6,130/11,748 · Unit 33,225/33,674 · Characterization 6/36 cases
ran (skipped 0) in 1 of 2 leg(s) …`. The two new negatives, `2002/pb3164_method_constant_scope`,
`2014/pb3165_method_capacity_register_scope` and the four `GlobalNameClassDriftTests` facts passed in leg 1.

