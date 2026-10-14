## Entry 2013 — 2026-10-13 20:18 PDT — PB3121: one containing-definition set for the prototype-name scope; PB2937 arm 2: every bound data forest is edition-gated; PB2937 arm 1: an unexpanded parameterized class binds its method bodies as itself

### PB3121: one containing-definition set for the prototype-name scope; PB2937 arm 2: every bound data forest is edition-gated

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

### PB2937 arm 1: an unexpanded parameterized class binds its method bodies as itself

Wave 1072 group B (OO binder: PB2937, PB3121), the finisher of wave 1071 group E's SPLIT. The predecessor's four commits
(PB3121 landed; PB2937 arm 2, the edition gates over every bound data forest) were cherry-picked onto 2da05a28a; this
change closes PB2937's other arm, so PB2937 is landed.

**Re-probe.** `w1071e/oo/ed.cob` at `--std 2023` compiled clean on the base: an unexpanded `CLASS-ID. EDSK USING T.`
whose method moved into an undeclared name and moved `ALL "12"` into a numeric item was never asked anything, because
`OoDriver.BindParameterized` bound the skeleton's data and headers only. The measured obstacle was real: binding the
bodies with the universal stand-in for a formal drew five false refusals on `w1053a/p/legal.cob`. The same probe also
showed that `legal.cob` is not legal: an expansion of it (`w1072b/p/exp.cob`) refuses `SET ONE TO X` (ONE `E ONLY`,
X `E`; §14.9.39.3 SR12 a)1.) and `SET ITEM TO ANY1` (a universal sender into a typed receiver) whatever the actuals.

**Fix shape.** The rule is §9.3.8.2.4's — the definition is treated "as if the actual parameter classes or interfaces
were substituted for the parameters" — read as: a question that turns on WHICH class or interface a parameter-name
names is each expansion's, and every other question is asked of the skeleton (OO deep-dive D12 determination 6,
rewritten).
- `ObjectRefDescriptor.Formal`: an AXIS of the object-class-name / interface-name alternative, because §11.3.4 GR6
  lets a parameter-name stand only where an object-class-name or interface-name is permitted. It replaces the
  universal stand-in (`DataBinder.OoDescribeNamedReference`).
- SET-style pairs (§14.9.39.3, and the RETURNING and BY CONTENT conformances that are "as if a SET") ask the one
  runtime table through `OoClassTable.SetOperands`, where a formal side stands for the actual MOST FAVOURABLE to the
  pair. So only what no actual can change is refused: the FACTORY and ONLY axes, a universal sender into a typed
  receiver, a typed sender into ACTIVE-CLASS. Two references to the same parameter-name are one opaque class. An
  ACTIVE-CLASS sender in a skeleton whose superclass is a formal is most favourable too.
- Identical descriptions (§9.3.8.2.3 rule 2) admit a pair that differs only in a name a formal may take
  (`TurnsOnFormal`).
- A statement that selects a method through a formal is the expansion's. This covers an INVOKE through a formal-typed
  reference or the formal class-name, SELF of a method only the formal superclass can define, SUPER and `B OF SUPER`
  over a formal superclass, an inline invocation or object property through a formal, and `SET … TO` the formal
  class-name. The statement binds the operands whose rules do not read the method, records itself on the new
  expansion-deferral ledger (`EditionContext.NoteExpansionDeferred`, the third ledger beside refusals and unbuilt
  operands), and the statement funnel keeps a `BoundDeferredToExpansion` (never emitted) in its place.
- The skeleton symbol now records its written superclass as `Base` (so SUPER binds against it) or its formal one as
  `FormalBaseName`. `BinderDriver.Bind` binds every skeleton body after the emitted classes, the skeleton forests take
  part in `ResolvePointerRestrictions`, and `VersionConformancePass` walks their bound trees.

**Siblings.** `IntrinsicBinder.KeywordOmittedFunction` read a class-name spelled like a zero-argument intrinsic as
the function: `INVOKE PI "NEW"` with `CLASS PI` drew COBOLNET1543, and the formal `E` did the same. A class, interface
or formal name in scope is a user-defined word (§8.3.2.1) and now wins. `ReferenceResolver.IsClassName` admits a
formal for `B OF SUPER`. `RaisingObjectMismatch` and `ActiveClassFormalMismatch` give a formal its most favourable
actual too. The golden `2002/pb2051_parameterized_definition_binds_itself` carried the same illegal ONLY SET as
`legal.cob`, so its L is now `A ONLY`.

**Goldens.** `2002/pb2937_skeleton_bodies_bind_as_themselves` writes every deferred position, a written superclass
and the class PI, and prints `PI-OK` / `PB2937-OK`. `negative/pb2937-skeleton-statement-rule` rejects with
COBOLNET1639 and `negative/pb2937-skeleton-only-receiver` with COBOLNET0867, both at 2002, 2014 and 2023.

**Branch review round 1.** The reviewer found two defects in the funnel, and both reproduced on the reviewed head
(`w1072b/r/*.cob`). R1-1: `IF CNT2 = 1 MOVE CNT2 TO SZP OF ITEM END-IF` in a skeleton drew a false COBOLNET2362
(internal error) on the IF, and an EVALUATE did the same. The nested MOVE's refusal node is the deferral's shadow. Its
own funnel took the deferral, but the refusal stayed on the ledger, and the IF answered for it. The refusal ledger is
now taken per statement, like the unbuilt and deferral ledgers beside it (`EditionContext.TakeRefusalsSince`). A
nested statement answers for its own refusals once. R1-2: `IF ITEM :: "SZ" = 1 MOVE ALL "12" TO CNT2 END-IF` compiled
clean, because the deferred IF replaced its whole bound statement, branches included. `BoundDeferredToExpansion` now
keeps what the verb binder built (`Bound`), and the generated `StatementChildren` walk reaches the branches, so the
nested MOVE draws COBOLNET0902 as it does in any class. The positive golden gains the nested shapes (IF, EVALUATE,
inline PERFORM, an IF condition through a formal). The new negative `negative/pb2937-skeleton-nested-edition-gate`
rejects with COBOLNET0902 at 2002, 2014 and 2023.

**Rows.** Batch `w1072b-pb2937-skeleton-bodies`: GR-11.3.4-6 (already CONFORMS) gains the PB2937 witnesses, and GAP
stays at 103. PB2937 lands with `closes_rows: []`.

**Self-review.** One mechanism per job: the deferral ledger mirrors the unbuilt-operand ledger and is reachable only
from a formal description, which only a skeleton's binders produce. The sibling arms of every descriptor-name class
lookup were swept (`Find` / `FindInterface` over a descriptor name), and each is guarded. No C# text was added to a
bound node.

**Gate.** Run `20261014T011922Z-526cce`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — … Conformance 5,816/11,551 · Unit 32,512/33,496 · Characterization 6/36 cases ran (skipped 0) in 1 of 2 leg(s) … ===`. The red on the way was run `20261014T005857Z-54f63e`: `ObjectReferenceSetDriftTests.BothPhases_AskTheOneTable` pinned the old one-line `Refusal(table.SetOperand(send), table.SetOperand(recv)` text, and it now pins the `SetOperands` pair. Arch oracle: DIFFERENT by 8 ADDED cases only (the five PB2937 and the two PB3121 goldens, plus PB2937 arm 2's 2014 golden) and none CHANGED; the re-recorded manifest was dropped at the merge of main, which no longer commits one (PB3107). After the review fixes, run `20261014T021753Z-db010c`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — … Conformance 5,840/11,550 · Unit 32,548/33,497 · Characterization 7/36 cases ran (skipped 0) in 1 of 2 leg(s) …`, every PB2937 golden and negative `Passed`. `semgrep/verify.py`: PASS. LINUX GATE: the lander's (batched gating).

