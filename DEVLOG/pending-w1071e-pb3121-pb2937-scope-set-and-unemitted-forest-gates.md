## PB3121: one containing-definition set for the prototype-name scope; PB2937 arm 2: every bound data forest is edition-gated

Wave 1071 group E (binder driver: PB3121, PB2937). PB3121 is landed. PB2937 is half landed: the edition-gate arm is
fixed and the skeleton-statement arm is measured and handed on (SPLIT).

**PB3121, the re-probe.** `mutual2.cob` (two PROTOTYPE programs, each with a formal restricted to ITSELF) drew
COBOLNET1959 at `SET B TO A`. The TO-phrase screen (`DataBinder`, the §8.4.6.8 scope) admitted the unit's own name
through `UnitSelfName`, but `BinderDriver.ProgramPrototypesOf` registered that name only under a written
`REPOSITORY. PROGRAM PS1.`. So the restriction resolved to no signature, and the greatest-fixed-point comparison
PB2495 built could not see that the two signatures are one. With the specifier written, the same program prints
`done`. The disagreement also ran the other way. A contained program's `USAGE PROGRAM-POINTER TO <container>` drew
COBOLNET1958 from the screen, though the registry always held the container. And `CALL <own-name>` in a RECURSIVE
program drew COBOLNET1639.

**PB3121, the determination.** §8.4.6.8's "a containing program definition" includes the element's own program
definition. §8.4.6.6 is the same sentence over functions, and its "the containing function definition" can only be
the function's own, because a function definition contains no other source unit (§10.2). §12.3.8.3 SR15 already
treats the self case and the container case as one consequence. The determination is recorded in INTERPROGRAM D6.

**PB3121, the fix.** `BinderDriver.ContainingDefinitions(unit)` (the unit, then each program containing it) is now
the ONE set. `ProgramPrototypesOf` registers every program member of it LAST, which also realizes SR15.
`UserFunctionsOf` takes its function member as the self entry and skips a self-naming specifier (SR11).
`BindUnitData` seeds `DataBinder.ContainingDefinitionNames` from it, replacing `UnitSelfName`. `SpecifiesItself` is
deleted. The sibling sweep found a third reader: `DataBinder.Ptr`'s `IsUserFunction` admitted a PROGRAM's own name
as a user function. It now asks `UnitIsFunction`. Goldens:
- `conformance:2002/pb3121_containing_definition_scope`: self-restricted prototypes, a RECURSIVE self-CALL by
  prototype word (N=3 three times), a contained program's TO <container>, and a non-recursive self-CALL that raises
  EC-PROGRAM-RECURSIVE-CALL into ON EXCEPTION.
- `conformance:negative/pb3121-sibling-program-not-in-prototype-scope`: a sibling nested program's name is still
  out of scope (COBOLNET1958).

No inventory row: §8.4.6.x carries no syntax or general rule, and the consuming rows were already CONFORMS.

**PB2937, arm 2 (edition gates).** An unexpanded parameterized class with `USAGE FLOAT-BINARY-64` compiled at
`--std 2002`, because `VersionConformancePass` walked only the emitted classes and the units. The sweep found the
sibling arm wider than the note said: no INTERFACE forest, written or parameterized, was ever gated, so a prototype
formal `USAGE FLOAT-BINARY-32` compiled at 2002 too. `OoDriver.BindParameterized` now keeps the skeleton forests.
`GroupBindContext` carries them outside `AllBinders`, so no pass that produces code sees them. The edition pass gates
the data of every skeleton half and of every interface forest. Goldens:
- `conformance:2014/pb2937_unemitted_forest_edition_gates`.
- `conformance:negative/pb2937-skeleton-data-edition-gate`.
- `conformance:negative/pb2937-interface-formal-edition-gate`.

OO deep-dive D12 determination 6 is updated.

**PB2937, arm 1 (skeleton statements), measured and not landed.** Binding the skeleton bodies through
`BindClassBody` reported the undeclared name and the obsolete `MOVE ALL "12"`. But the universal stand-in for a
formal drew five false refusals on the legal `legal.cob`: the universal SR6 literal ban, SUPER and `B OF SUPER` over
a formal base, and a formal as INVOKE's class-name. The experiment was reverted. The note now lists the sites that a
`ObjectRefKind.Formal` description kind must answer. That kind is the next agent's job.

**Self-review.** One mechanism per job: the scope set is written once, and the edition pass's forest list is one
expression. No skeleton forest reaches a producing pass. Allocations: one small `HashSet` per program unit, and one
list per `UserFunctionsOf` call. No duplicate rule is left: `SpecifiesItself` and `UnitSelfName` are deleted.
