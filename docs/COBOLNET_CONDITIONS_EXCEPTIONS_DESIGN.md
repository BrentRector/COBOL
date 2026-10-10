# WiseOwl COBOL — Conditions & Exception Model (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the WiseOwl COBOL rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §11; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## The EC-OO slice: the exception-OBJECT channel

The OO exception-object model (ISO §14.6.13.1.5; the OO deep-dive slice 6, brief D-EO1–D-EO10 in
`docs/COBOLNET_OO_SLICE_BRIEFS.md`) rides this engine with ONE signal architecture — no parallel
OO mechanism (feedback_one_mechanism_per_job):

- **ExceptionState** carries: `CobolObject? ExceptionObject` (the §8.4.3.6 register — ONE per run unit,
  GR2; typed per the OO D-U1 universal model), the `"EXCEPTION-OBJECT"` **LastName sentinel** (§15.33.3
  r1's literal EXCEPTION-STATUS value — the EXCEPTION-* functions needed ZERO changes; the sentinel is NOT
  a catalog name and `ExceptionCatalog.TryGet` fails on it by design), `SetObject`,
  `SetPropagatingObject`/`TakePropagatedObject` (the object propagation slot — mutually exclusive with the
  named `_propagated` slot; each staging call clears the other), and the `SetPropagatingLast` object
  leg (GOBACK RAISING LAST with an object status re-propagates the OBJECT, §14.9.18.4 GR1b3a second sentence).
- **RAISE identifier-1** → `BoundRaiseObject` (grammar takes `objectReference` so NULL/SUPER get the
  targeted 0848, and RAISE SELF parses); NOT TURN-gated (§7.3.25 takes names only); NEVER fatal by itself
  (GR2 — the continue-after-RAISE path is the normal exit).
- **USE Format 4** (`USE AFTER {EXCEPTION OBJECT | EO} {object-class-name-1 | interface-name-1}` — a brace
  group with TWO alternatives, §14.9.49.2; EO is context-sensitive like EC) → `BoundDeclarative.Eo`, the
  discriminated `BoundEoOperand` (`BoundEoClass(OoClassSymbol)` | `BoundEoInterface(OoInterfaceSymbol)` — ONE
  value with a discriminator, never two nullable fields, and each case carries the RESOLVED SYMBOL, never a
  rendered C# name) → the generated `__EcObjDispatch(object?)`. ⛔ **§14.9.49.4 GR14 IS A TWO-PASS RULE and the
  emitted selector is two passes**: pass a) every object-class-name-1 entry in source order, and only if none
  qualifies does "all of the USE statements in the source element are analyzed again" for pass b) every
  interface-name-1 entry; −3 tail → §14.6.13.1.5. Interleaving the two would let an EARLIER interface entry
  beat a LATER class entry, which GR14 orders the other way (kb/Work PB365; `2002/pb365_use_eo_interface_two_pass`
  is the program that separates them). **Each pass renders its alternative's OWN CENSUS of emitted C# types as
  one `is` or-pattern — one COBOL name is not one C# type.** GR14 a) names BOTH object kinds in one clause ("a
  factory object or instance object of object-class-name-1 or of a subclass") and a COBOL class is emitted as
  TWO DISJOINT C# hierarchies, so a class entry tests `OoClassSymbol.FactoryOrInstanceCsTypes`
  (`__obj is FOO or FOO__FACTORY`); testing only the instance half selected NO declarative for any factory
  exception object, silently (kb/Work PB366). GR14 b)'s "described with an IMPLEMENTS clause that references
  interface-name-1" is §11.8.4 GR2 / §11.4.4 GR2's IMPLEMENTS closure, which rides C#'s `is` over the single
  emitted interface (`OoInterfaceSymbol.ImplementedCsTypes`) that both halves carry. "Or of a subclass" rides
  `is` in EACH hierarchy, since both mirror INHERITS. Both operands resolve through `OoNameResolution` —
  §14.9.49.3 SR16/SR17 scope them to the source element's REPOSITORY (§8.4.6.4), not to the compilation group.
  `Format4UseObjectSelectorDriftTests` asserts, against the GENERATED C#, that each census equals what the
  backend actually emits and that the selector tests every member.
  GR3: for an OBJECT raise F4 REPLACES the F1/F3 tiers. GR15 holds
  structurally (the raise site sets the register before dispatching).
- **⛔ THE STATEMENT AND ITS CLAUSE TRAVEL AS ONE VALUE — `EcRaiseSite`** (`Binding/EcRaiseSite.cs`;
  kb/Work PB388). RAISE (§14.9.29), GOBACK RAISING (§14.9.18) and EXIT PROGRAM / FUNCTION / METHOD RAISING
  (§14.9.14) are bound by ONE path — `EcBinder.EcBindRaising` over `EcNameResolution.TryResolve` — and every
  rule that path enforces is written down three times in the standard, once per statement, under a DIFFERENT
  ordinal: level-3 exception-name EXIT SR3 / GOBACK SR2 / RAISE SR1; identifier-1 an object reference EXIT SR5
  / GOBACK SR4 / RAISE SR2 (the declared-class constraint at `a)`, the universal one at `d)`); the LAST phrase
  EXIT SR6 / GOBACK SR5. The VERB was threaded and the CLAUSE was a literal per message, so every one of those
  messages printed GOBACK's or RAISE's rule number at an EXIT statement. The citation is a property of the
  SITE; each call site passes `EcRaiseSite.Raise` / `.Goback` / `.Exit(verb)` and the messages compose
  `site.Cite(site.Level3Rule)`. `EcRaiseSiteDriftTests` re-derives every ordinal from `spec-rule-catalog.json`
  by matching the rule TEXT and asserting the match is UNIQUE, so a renumbering fails a test instead of
  misdirecting a reader; the negative pair `l1-exit-raising-level2-name` / `pb388-goback-raising-level2-name`
  pins BOTH arms through the corpus, each `.err` naming its own statement's citation.
- **GOBACK / EXIT PROGRAM / method-return RAISING identifier-1** → `BoundRaising.ObjectSource` (exactly
  one of EcName/IsLast/ObjectSource); the no-universal sub-item (EXIT SR5d / GOBACK SR4d) + the
  declared-class-in-header one (EXIT SR5a / GOBACK SR4a, walking the base chain) = 0849 at COMPILE time — which statically discharges the activated-side item-1 check for an identifier-1
  (D-EO5). `RAISING LAST EXCEPTION` of an object status is tested at RUN time against the element's header census
  (`EcState.PdRaisingObjectCsTypes`, rendered by `EcEmitter.ObjectApplicableTest`) and, on a miss, stages the NAMED
  EC-OO-EXCEPTION instead of the object (kb/Work PB1121). The stage
  (`ExceptionState.SetPropagatingObject`) has no Enabled/Fatal logic — objects are not TURN-gated.
- **The pickup** (`CallEmitter.EmitPropagationPickup`) has an object branch (slots exclusive): GR1b2
  re-registers in the activator → F4 dispatch (rule 2) → on `NoHandler`, item 3 (PROPAGATE ON) and else the item-4
  conversion: `ExceptionState.ConvertObjectPropagationToNamed()` re-stages the object as the NAMED EC-OO-EXCEPTION
  for this activation, and the pickup's one NAMED arm (`TakeRaisedPropagation(site.ActivatorChecking…)`, the PB408
  activator gate) takes it from there: raised — last status set, the F3 tiers (EC-OO-EXCEPTION/EC-OO/EC-ALL match),
  unresumed ⇒ `CobolFatalException` (fatal per Table 13 — surviving needs RESUME AT NEXT STATEMENT, the standard
  fatal-EC protocol) — only when EC-OO-EXCEPTION checking is enabled in the ACTIVATING element, else nothing is
  raised and the statement completes (§14.9.18.4 GR1 b), §14.6.13.1.1). The object counts as "propagated" for the
  CALL's NOT ON EXCEPTION test (GR3i) only when a USE AFTER EXCEPTION OBJECT declarative took it or the named arm
  raised it. The SAME pickup runs after every CALL and every Instance/Self/Super/Factory INVOKE
  and every UNIVERSAL dispatch (`OoEmitter.EmitInvokePickup`); NEW needs none (the ctor runs no user statements).
  GR1b's result-before-exception ordering falls out of stage-then-`throw MethodReturn` + the entry catch
  delivering RETURNING/copy-outs before the site's pickup.
- **Headers**: the PD-header RAISING phrase (§14.2.1 — `exception-name-1 | [FACTORY OF] object-class-name-1 |
  interface-name-1`) is partitioned by ONE function, `RaisingPhrase.Partition`, for program, function and
  METHOD-ID headers alike, into `RaisingTarget` tuples (kind × name × FACTORY — kb/Work PB815/PB814): level-3
  EC-USER names (SR7), classes (SR8) and interfaces (SR9) of the element's REPOSITORY scope, else 0858. Methods
  carry theirs on `OoMethodSymbol.Raising`, loaded per-method (a method IS a source element). The GOBACK
  §14.9.18.3 SR4 / EXIT §14.9.14.3 SR5 identifier check (`EcBinder.RaisingObjectMismatch`) compares the
  operand's `ObjectRefDescriptor` against those tuples: a)/c) the class (ACTIVE-CLASS: the containing class)
  or a superclass with the SAME FACTORY presence; b) an interface that CONFORMS (§9.3.8.2.3,
  `OoConformance.InterfaceConformsTo`, which shares `MethodConformanceMismatches` with the IMPLEMENTS pass)
  to a listed interface; d) never universal.
- **PROPAGATE ON** (§14.6.13.1.5 EXIT/GOBACK item 3) — the activator's re-propagation of an object no declarative
  took is `EcEmitter.ObjectPropagationReturn`, emitted in the pickup's `-3` arm ahead of item 4 (kb/Work PB1119; see
  "Automatic propagation" below). Object VIEWS (EC-OO-CONFORMANCE) stay 0899-named; method declaratives are
  implemented (kb/Work PB1010 — the METHOD-SCOPED SELECTION paragraph below); the
  interface / FACTORY-OF / ACTIVE-CLASS legs of the RAISING phrase are implemented (above — kb/Work PB389 on the
  operand end, PB815/PB814 on the header end). Every exception-object raise site renders its operand through
  `RuntimeApi.AsExceptionObject`, the one explicit conversion an interface-typed reference needs.


## Summary

Decision-complete design for the conditions + exception subsystem of the greenfield COBOL→C#/Roslyn compiler (src/Cobol.Net.{Frontend,Compiler,Runtime,Cli}; C# namespaces `CobolNet.*`). Covers IF/ELSE/END-IF; EVALUATE (all forms); level-88 condition-names + SET cond TO TRUE/FALSE; class/sign/relational/abbreviated-combined conditions with NOT>AND>XOR>OR precedence; and the COBOL-2002/2023 EC exception model (EC-* hierarchy, >>TURN, RAISE/RESUME, USE…EXCEPTION/ERROR declaratives, EXCEPTION-OBJECT, ON SIZE ERROR/AT END/INVALID KEY/ON OVERFLOW/ON EXCEPTION).

Two C# code shapes — both are the Roslyn backend's rendering of the ONE backend-neutral bound tree (G4/ICodeGenBackend: all semantics live in the BoundCondition/EC bound nodes + the binder-computed TurnState; emitters only render; the future CIL backend lowers the SAME bound nodes to branches with its own private lowering): (1) conditions are PURE C# boolean expressions (ConditionRenderer.Render(BoundCondition)→string, no side effects) so they compose into if/while/?:/EVALUATE-arms and level-88 bool properties; (2) the exception model is stateful runtime (CobolNet.Runtime.Exceptions) plus emitted guards that appear ONLY when a program uses the feature — EC checking is OFF by default (ISO §14.6.13.1.1, §5000), so the typed-native fast path emits zero exception scaffolding in the common case.

Correct behavior is defined by the ISO spec (specs/ISO_COBOL.md — cite the §); the legacy engine's ConditionLowerer.cs and PicRuntime.cs (364 NIST green; deleted from `main`, archived) were a differential regression net and reference ONLY, never authority; the byte IMPLEMENTATION is rejected and re-derived over native string/long(/Int128)/bool — the numeric design's scaled integers; System.Decimal is rejected (docs/COBOLNET_NUMERIC_DESIGN.md). THIS document is that full prose (condensed view: docs/COBOLNET_DESIGN.md §11; brief overview: docs/COBOLNET_ARCHITECTURE.md). New diagnostics occupy a COBOLNET07xx band. New runtime classes: CobolClass (class-condition predicates over UTF-16 chars), ExceptionCatalog (generated from ISO Table 13: level-3→level-2→EC-ALL hierarchy + fatality), ExceptionState (last-exception register, EXCEPTION-OBJECT, file/location/statement), CobolException/CobolFatalException, ExceptionDispatch (declarative registry). Implementation is mechanical from here.

## The EC model — file map and engine specifics

The conditions half (D1–D8) is implemented as designed. The EC half (D9–D12) is implemented; the file map and
the engine's design specifics:

**File map.** Frontend: `Preprocessor/TurnDirectiveProcessor.cs` (the `>>TURN` stage — runs LAST, after COPY/NIST,
on the FINAL text so `TurnEvent.Line` is directly comparable to token `Start.Line`; line-count preserving, asserted
by `Frontend.Parse`; an emitting-branch TURN survives `ConditionalCompilationProcessor` via `leaveTurnDirectives` —
the legacy pipeline keeps consuming TURN there). Compiler: `Binding/TurnState.cs` (the compile-time §7.3.25.4 fold),
`Binding/Procedure/Verbs/EcBinder.cs` (RAISE/RESUME/SET LAST EXCEPTION/RAISING binds + the per-statement
`EcWrap` fold → `BoundEcChecked`), `CodeGen/EcEmitter.cs` (guards + the generated `__EcDispatch` /
`__IoCheckEc`), `CodeGen/Verbs/CallEmitter.cs` (RAISING staging, the CALL-site propagation pickup, the EC-PROGRAM
catch). Runtime: `Runtime/Exceptions/` — `ExceptionCatalog` (Table 13 + `DirectiveCovers`, the §7.3.25.4
GR2/GR3/GR4 expansion), `EcCheckingProfile` (an activating statement's checking state, folded at run time for a
run-time name — §14.9.18.4 GR1 b)), `ExceptionState` (last-exception register +
propagation slot + the EC-ARGUMENT-FUNCTION ambient gate), `EcFunctions` (§15.28/30/32/33), `CobolFatalException`,
`ResumeSignal`. `CompilerDriver` hands `Frontend.Directives` (whose `TurnEvents` build the TurnState) to `CSharpEmitter.Bind`; `EmitBound` renders the bound tree via `ProgramEmitter`.

**EC engine specifics:**
- **The declarative dispatch-result protocol.** Declaratives are pc
  RANGES run by the bounded `__Dispatch`, not C# methods — so RESUME throws the runtime `ResumeSignal`, the
  int-returning `__RunUse` (emitted only when the group is EC-active; the void form stays byte-identical otherwise)
  catches it, and every raise site speaks ONE protocol, the runtime type **`DispatchResult`**
  (`Runtime/Exceptions/DispatchResult.cs`; kb/Work PB1122 Task A, PB1761): `Normal` (`-1`) completion
  (§14.6.13.1.2), `ResumeNext` (`-2`) RESUME AT NEXT STATEMENT (suppresses a fatal — §14.6.13.1.3 #5 NOTE 2),
  `NoHandler` (`-3`) no qualifying declarative, a value `≥0` RESUME AT procedure-name's pc (≡ GO TO, §14.9.33.4 GR3),
  `HandledNonfatal` (`-4`) — from an I-O hook's SUCCESSFUL arm only: a WHEN phrase or USE declarative handled a warning
  a successful statement raised, so the statement's NOT phrase is skipped (§14.6.13.1.4 2)/3); a NOT phrase runs on
  `Normal` alone; kb/Work PB1120) — and `NotNormal` (`-5`): the declarative fell off its end but did not complete
  normally (§14.6.13.1.2 1: a fatal exception occurred within its scope, or a GR1 CONTINUE-RESUME was executed;
  `__RunUse` answers it from the declarative activation record, kb/Work PB1122 Task B — see "RESUME control flow"
  below). ⛔ **The emitters render the NAMES and the
  PREDICATES (`IsTransfer`, `SuppressesFatal`, `TerminatesSortMerge`, `FinishesStatement`, `ForHandledWarning`,
  `RanAHandler`), never the
  numbers** — `DispatchResultProtocolDriftTests` fails the build when a bare literal reappears in an emitted string, so
  a new result value lands in one file. There is no `ExceptionDispatch` registry class: the F3 selector is
  the GENERATED `__EcDispatch` (source-ordered GR3c–g tiers over the program's own declaratives).
  ⛔ **An emitted raise site renders the selection through ONE method, `EcEmitter.EmitSelection`** (kb/Work
  PB1549): the selector call, the RESUME landing, and — for a FATAL condition — the default of §14.6.13.1.3 5)/7)
  (`Normal` declarative completed normally and `NoHandler` both terminate the run unit abnormally; only `ResumeNext` /
  a transfer continues — `DispatchResult.SuppressesFatal`). A raise site whose exception-name is known at compile time calls
  `EmitConditionRaise` (or `EmitConditionSet` when its own conditional phrase takes the condition), which reads
  the fatality from Table 13 (`EcInfo.IsFatal`) instead of the site's author; a dynamic-name site (CALL catch, the
  GOBACK RAISING pickup, the EC-SIZE family, the statement guard's rethrow) passes its terminate statement.
  Before this, each site spelled `dispatch + ResumeTransfer` and had to remember the fatal default itself — the
  SET … ADDRESS OF PROGRAM / FUNCTION sites did not, so a failed SET with checking on and no declarative CONTINUED
  the run unit while the CALL arm of the same EC-PROGRAM-NOT-FOUND terminated it. `EcRaiseSelectionDriftTests`
  pins that nothing else renders the selector (the two nonfatal runtime-selector bridges excepted).
  ⛔ **The same `ResumeSignal` has a SECOND entry and a second landing** (kb/Work PB892). A declarative is entered by
  an exception (above) OR by a PERFORM in the nondeclarative portion (§14.9.49.3 SR4 — the only reference a
  nondeclarative procedure may make to one). For that PERFORM §14.9.33.4 GR2 b) puts RESUME AT NEXT STATEMENT's
  implicit CONTINUE "immediately follows the last statement of the terminating procedure referenced in that PERFORM
  statement", and GR3 makes RESUME AT procedure-name a GO TO. The binder marks such a PERFORM
  `BoundOutOfLinePerform.EntersDeclarative` (the range is declarative, `ctx.Enclosing.Declarative` is null — a
  PERFORM written INSIDE a declarative is not it: its RESUME belongs to whatever ran the enclosing declarative), and
  `ControlFlowEmitter.EmitPerformedRange` wraps that one execution of the range in `catch (ResumeSignal)` — NEXT
  STATEMENT completes the range (the control phrase goes on), procedure-name is `ResumeTransfer`. Emitted only in a
  unit that has a RESUME (`DispatchState.UnitHasResume`), so every other PERFORM is byte-identical.
- **The TURN fold runs at BIND time, not emit time:** bound nodes carry no parse context, so the
  binder (which has the statement's line) queries the TurnState and wraps the statement in `BoundEcChecked`
  carrying the decision (`EcStatementInfo`: enabled (name, file) pairs, WITH LOCATION, statement name, the §15.30.3
  r2 location string). Checking-off binds the bare node — zero scaffolding by construction.
- **EcWrap's relevant-family rule (the unimplemented-raise license).** A statement wraps only for names its kind
  can actually raise here: EC-SIZE-* (arithmetic), EC-I-O-* per referenced file, EC-OVERFLOW-STRING/-UNSTRING,
  EC-PROGRAM-* (CALL/CANCEL), EC-ARGUMENT-FUNCTION (intrinsic-bearing statements), EC-BOUND-REF-MOD/-OVERFLOW +
  EC-DATA-NOT-FINITE and EC-DATA-INCOMPATIBLE (any statement — ambient gates, below) and EC-DATA-OVERFLOW (a
  MOVE), EC-STORAGE-NOT-AVAIL (SET SIZE), EC-RANGE-INDEX (any statement — ambient, below),
  EC-RANGE-INSPECT-SIZE (an INSPECT — §14.9.22.4 GR14/GR15/GR22 are written over INSPECT's operands alone and
  `CobolInspect.Replace` / `Convert` are the only raise sites, so the node kind IS the rule's scope; kb/Work
  PB1126). A name this
  implementation still cannot raise binds no wrapper — §14.6.13.1.1 sets an indicator only
  "when the associated exception occurs".
- **EC-RANGE-INDEX is AMBIENT, deliberately, and that is the lesson from PB230 applied ahead of time**
  (kb/Work PB459). §13.18.38.4 GR2 names three statements that may drive an index outside the implementor's
  range — "An index may be modified only by a PERFORM VARYING statement, a SEARCH statement, and a SET
  statement" — and the §14.9.39.4 GR2 a) 1. b / 2. a / 3. b and GR4 a) limits are that same limit. A PRECISE
  `QueryFor` case per node kind would be a hand-maintained list of every emitter path that stores into an index
  (`SetEmitter`'s store/augment pair alone is ridden by three verbs), and the next such path would silently stop
  being CHECKABLE — which is exactly the EC-DATA-INCOMPATIBLE `node is BoundMove` defect. The raise fires only
  inside `CobolIndex`, so the flag around an index-free statement is a no-op. ⛔ **The condition had NO raise
  site at all before PB459** while reading as wired: `ExceptionCatalog` carried its Table 13 row and
  `FlagConformancePass` asked `_turn.Enabled("EC-RANGE-INDEX", …)` in order to FLAG a SET, so a grep for the
  name returned four hits and none of them was a raise.
- **THE RAISE RULE IS WRITTEN ONCE (`ExceptionEngine.FatalIfEnabled` / `NonfatalIfEnabled`; kb/Work PB676).**
  §14.6.13.1.1 is ONE sentence — "if checking for an exception that occurs is not enabled, no exception condition
  is raised" — applied once per (ambient checking flag, exception-name) pair, and that PAIR is declared in exactly
  one place: `EcEmitter`'s `FatalAmbientGates` / `NonfatalAmbientGates`, which say which flag the statement guard
  sets when a TURN enables the name. Every `ExceptionEngine.…Error` helper is therefore ONE expression naming its
  pair — `public void SubscriptError(string detail) => FatalIfEnabled(BoundSubscriptChecking, "EC-BOUND-SUBSCRIPT",
  detail);` — and never a hand-written copy of the gate/`Set`/throw shape (it was copied twenty-two times, and a
  copy carrying a NEIGHBOUR's flag or the wrong `fatal:` argument read exactly like its siblings).
  `ExceptionRaiseHelperDriftTests` proves the pairing BEHAVIOURALLY — invoke each helper with every flag on EXCEPT
  the one the emitter pairs to the name it raises, and it must raise nothing — and checks the recorded fatality
  against `ExceptionCatalog`'s Table 13 row, so neither mistake can reach a user's program. **Adding a condition**
  = a `CheckingFlags` field + a delegating engine property (+ its static shim forwarder) + a gate-table row + a
  one-line helper. Nothing else, and the drift tests fail if any of the five is missing.
- **The EC-ARGUMENT-FUNCTION ambient statement gate.** Intrinsics render inline inside arbitrary expressions;
  threading a checked-mask through every runtime signature would fork each intrinsic into twins. Instead the guard
  wraps the STATEMENT (`ExceptionState.ArgumentFunctionChecking` in the statement's checking scope + try/catch for the F3 dispatch), and
  EVERY §15.3 default-result site in the intrinsic runtime routes through `ExceptionState.ArgumentError` (raise
  when enabled, the documented default — 0 / one space — when off): `FromDouble` NaN/∞, FACTORIAL, MOD/REM zero
  divisor, NUMVAL/NUMVAL-C malformed, the CobolDate range checks, CHAR/ORD out-of-domain.
- **THE AMBIENT FLAGS ARE A SCOPE, NEVER A SET/RESET PAIR (kb/Work PB891 + PB841).** Enablement belongs to the
  SOURCE TEXT of the executing statement (§7.3.25.4 GR6; GR5 — a TURN inside a statement "applies to any
  succeeding statement … whether or not that succeeding statement is within the scope of the statement in which the
  TURN directive is specified"); the `ExceptionState.<Flag>` bits are only how a raise site deep in the runtime
  learns it. So there is ONE discipline, realized at every boundary: a statement guard SAVES the state
  (`ExceptionState.SaveChecking`), sets its own flags, and RESTORES it in its `finally`
  (`EcEmitter.OpenGateFlags` → `RestoreChecking`) — never `= false`, which cleared an ENCLOSING guard's enable (an
  inline-PERFORM's UNTIL stopped raising and the loop ran off the table). And wherever control reaches OTHER source
  statements while a guard's flags stand, those statements start from ALL-OFF (`PushAllCheckingOff` … `RestoreChecking`):
  a nested statement list (`EcEmitter.EnterNestedStatements` → `EnterCheckingBaseline`), a procedure range run by a
  PERFORM / SORT / MERGE (`StatementEmitter.EmitProcedureRange`, the ONE caller of `DispatchCall`), every USE
  procedure and F3 handler (`__RunUse`), a method body (`OoEmitter`), and a CALL or function activation
  (`ProgramTable.CallProgram`). The emitter tracks statically whether a guard's flags are standing
  (`EcState.FlagsStanding`), so a list no flag guard encloses — every paragraph, every unchecked program — emits no
  scope at all. §14.9.28.4 GR14's implicit PUSH ALL + TURN OFF ALL for imp-2..imp-5 is one instance of the baseline
  scope, not a separate mechanism. `CheckingScopeDriftTests` pins every boundary and the absence of any reset.
- **The §14.6.13.2 EXEMPTION TABLE IS A STRUCTURE, not a per-rule flag (`CodeGen/Emit/SendingRef.cs`).** The
  clause states five sibling conditions over ONE subject — the content of a sending operand that is not valid —
  and each carries its own list of contexts in which the reference is exempt. `SendingRef` names the context ONCE
  at the reference site (`Normal` · `ClassCondition` · `SignCondition` · `SameUsageMove` · `Validate`) and each
  rule reads its own list off it: `FixedPointChecked()` is rule 2's TWO exemptions (class condition, VALIDATE),
  `FloatChecked()` is rule 3's FOUR (those two plus a sign condition and a same-usage MOVE). **A boolean could not
  carry both**, which is why the retired `floatCheck` / `floatSendingExempt` pair could only ever serve rule 3 —
  and did, leaving rule 2 with no wiring at all until kb/Work PB230. The asymmetry is the standard's own:
  §8.8.4.7.4 GR2 gives a float sign test a defined answer for NaN (it reads the IEEE sign bit) and §14.9.25.4 GR6c
  makes a same-usage MOVE a verbatim transfer, and fixed-point has neither rule, so neither exemption. It is
  threaded through both sending-read channels — `NumericRenderer` (re-entrant `_sending`, saved/restored at every
  public entry) and `OperandText` (one cached visitor per `deSign` × `SendingRef` pair, indexed rather than laddered
  so a new member is covered by construction).
- **The EC-DATA-INCOMPATIBLE ambient statement gate (§14.6.13.2 rules 1 and 2).** Rule 1 is rule 2's BOOLEAN
  sibling — same condition, same fatal disposition, the SAME two-entry exemption list — and it is wired at the
  same time and in the same shape, because fixing one and leaving the other is the two-arm defect this whole
  change exists to retire. A category-boolean item's storage is one character per boolean position
  (§13.18.40.4 GR14's representation license, D-B1; `USAGE BIT` takes the same carrier), so a REDEFINES window
  can deposit a character that is no boolean value, and rule 1 makes referencing it EC-DATA-INCOMPATIBLE —
  measured, not assumed: `MOVE "1Q01"` into the window then `COMPUTE R = B` propagated the `Q` straight into the
  boolean RESULT. **`CobolBool.Sending`** raises it, at BOTH boolean sending-read channels: the value channel
  (`BooleanRenderer`'s `BoundBoolRef`) and the character channel (`OperandText.SendingVerbatim`, which is also
  the one place the three verbatim-read returns in `FieldAsString` now decide the category question — they each
  spelled a bare `PlaceRenderer.Read(p)` before, which is a shape a per-category rule cannot live in).
  **`CobolClass.IsBoolean`** is §8.8.4.4.4 GR3 e's predicate for the class condition; rule 1's test differs from it
  at exactly ZERO LENGTH — the class condition is false there (§8.8.4.4.4 GR1) while rule 1 has no content to
  call invalid (the clause's closing paragraph), and zero-length boolean operands are ordinary (§8.8.2 NOTE 2) —
  so the SCAN is shared (`HasNonBooleanPosition`) and each caller puts its own zero-length answer on it. Golden
  `2002/pb230_incompatible_boolean_sending` (both channels, B-NOT, and the zero-length non-raise).
  The BOOLEAN **class condition** is implemented (kb/Work PB590): `classConditionName` carries the alternative,
  `ClassConditionModel` its §8.8.4.4.3 operand rules, and `ConditionRenderer.RenderClass`'s `'B'` arm renders
  `CobolClass.IsBoolean` over the operand read with `SendingRef.ClassCondition` — so rule 1's class-condition
  exemption now has its site and the class test never raises on the content it was asked to report.
- **The fixed-point half of the same gate (§14.6.13.2 rule 2).** Rule 2 makes the condition
  exist whenever "the content of a numeric sending item that is not described with a standard floating-point usage
  is referenced during the execution of a statement and the content of that sending operand would evaluate to false
  in a numeric class condition". It is **not statement-specific** (rule 4 is — it names a de-editing MOVE), so the
  gate is ambient like its float twin, and the raise rides the sending READ: only a numeric leaf whose storage is a
  CHARACTER WINDOW (a Tier-B REDEFINES view, or a whole-group-aliased `StoreAsImage` leaf) can hold content that
  fails its own class condition — a native-carrier leaf can only hold digits, which is exactly why
  `ConditionRenderer` folds `IS NUMERIC` on one to the compile-time constant `true`. The two chokepoints mirror the
  float pair one for one: the numeric-value read (`NumericRenderer.FieldNumCore`'s `StoreAsImage` arm →
  `CobolNum.ParseImageSending`) and the string-image read (`OperandText.FieldAsString` / `NonTextBytes` →
  `CobolNum.SendingImage` for the ZONED pass-through, whose stored image IS its text and must NOT be
  decode-and-reformatted — that would silently turn the incompatible content into zeros). So ADD, SUBTRACT,
  MULTIPLY, DIVIDE, COMPUTE, relation and sign conditions, DISPLAY, STRING, a SORT key compare and a numeric MOVE
  all raise — rule 2 is the BROAD one, and the node-kind test that used to gate this family (`node is BoundMove`,
  scoping it to rule 4's de-editing MOVE) is why none of them did. ⛔ **A raise site is not enough where the
  comparison runs inside a FRAMEWORK-OWNED comparer**: the table SORT (§14.9.40 Format 2) hands a
  `Comparison<T>` to the runtime's array sort, which catches whatever a comparer throws and re-throws it as
  `InvalidOperationException` — so the fatal EC never matched the statement guard and the run unit died
  unhandled instead of running the program's own USE declarative. `CobolTable.SortInPlace` undoes that wrapper with
  `ExceptionDispatchInfo` at the ONE place a COBOL comparer meets the framework, covering any future comparer
  raise by construction. Measured, not deduced; pinned as the sweep golden's L12; with checking OFF the flag test short-circuits before the O(digits) scan and the decode stays the
  tolerant deterministic one the standard's "undefined" permits, byte-identical to a pre-slice build.
  **`CobolNum.IsNumericImage` is the ONE §8.8.4.4.4 GR3 n)1 predicate** — keyed on the item's `NumericByteForm`
  (zoned digits + declared sign presentation for n)1.a; valid BCD nibbles + a sign nibble for packed, and the
  PICTURE-range test for binary, both n)1.c; a `BinaryCapacity` item takes its range from its container per
  §13.18.60.4 GR12) — and the CLASS CONDITION calls the same predicate over the same RAW window, because the
  standard defines rule 2's test by reference to the class condition's. **§14.7.6's CORRESPONDING deferral**:
  "if any of the implied statements would set the EC-DATA-INCOMPATIBLE exception condition to exist, [it] is set to
  exist after all of the implied statements are completed" — a fatal raise at pair 1 would abandon pairs 2..n, so
  `CorrespondingEmitter.Deferred` opens a runtime latch region (`DataIncompatibleDeferBegin` / `DeferEnd`, left in a
  `finally`, raised after the `try` so it never displaces an exception already in flight) around the pairs, emitted
  only when the family is enabled. It is the same one-latch-one-dispatch shape the clause's SIZE ERROR paragraph
  already gets from `ArithmeticEmitter`'s `__sizeErr`. Goldens `2002/pb230_incompatible_sending_sweep` (every
  statement class, both exemption halves), `2023/pb230_incompatible_corresponding` (the §14.7.6 deferral, ADD and
  MOVE), `85/pb230_class_numeric_image` (the shared predicate over zoned / signed / packed / binary windows).
- **The float EC-DATA ambient statement gates (EC-DATA-NOT-FINITE / EC-DATA-OVERFLOW).** A float item's value is
  read inline in expressions, so — exactly like EC-ARGUMENT-FUNCTION / EC-BOUND-REF-MOD — the guard wraps the
  STATEMENT (`FloatNotFiniteChecking` / `FloatOverflowChecking` in the statement's checking scope via `FatalAmbientGates` + the try/catch F3
  dispatch) and the runtime raise sites consult the flag. Both are **always-emitted** (the singular pattern —
  `CobolString.RefMod`, not an emit-time fork) so a directive-free build is byte-identical (the flag defaults OFF ⇒
  the wrap is a pass-through). **EC-DATA-NOT-FINITE (§14.6.13.2 item 3)** is wrapped at the TWO float sending-read
  chokepoints — the numeric-value read (`NumericRenderer.FieldNumCore` line 140 → `RuntimeApi.FloatSending`) and the
  string-image read (`OperandText.FieldAsString` float arm → `RuntimeApi.FloatSending`) — so DISPLAY, STRING,
  MOVE-to-alphanumeric/group, arithmetic, relations, level-88, intrinsic args, and a different-usage float MOVE
  source all raise for a NaN/±Inf content. **`CobolFloat.Sending`** raises the fatal EC. The **four exemptions** are
  realized as a RAW (unwrapped) read at exactly the exempt sites, named by `SendingRef` (above): a **class
  condition** and a **sign condition** pass `ClassCondition` / `SignCondition` (a sign operand's whole sub-tree, via
  the re-entrant `NumericRenderer._sending`), a **same-usage MOVE** (source and receiver share the same `Usage` —
  endianness is a separate phrase, not a Usage value) passes `SameUsageMove`, and **VALIDATE** will pass `Validate`
  once its emitter lands (documented no-op today). Note the two lists differ — `SignCondition` and `SameUsageMove`
  suppress rule 3's wrap and NOT rule 2's. **EC-DATA-OVERFLOW (§14.9.25.4 GR6 d)4.a)** is MOVE-only: `CobolFloat.StoreSingleChecked` at the
  single-precision-float MOVE receiver raises when a FINITE source casts to ±Inf (cast-based, `double.IsFinite(src)
  && float.IsInfinity((float)src)` — never a MaxValue compare, since a double in `(float.MaxValue, ~3.4028e38]`
  rounds to a finite `float.MaxValue`). An arithmetic ±Inf store (`ArithmeticEmitter.StoreArith`) stays a bare cast
  (a valid §14.6.8.3 GR1 result, never this EC); a double receiver cannot overflow from a finite double. Both ECs
  apply to EVERY floating-point usage in the typed-native model (all map to IEEE binary) — mandatory for the standard
  usages (FLOAT-BINARY/-DECIMAL), an implementor determination for FLOAT-SHORT/-LONG/-EXTENDED and COMP-1/COMP-2 (see
  `CONFORMANCE.md §3`). Goldens `2023/ec_data_not_finite` (both chokepoints + all exemptions) and `2023/ec_data_overflow`.
- **RAISING propagation = an UNCONDITIONAL staging slot + a pickup that owns the whole raise.** GOBACK / EXIT
  PROGRAM / method-return RAISING stages (name, fatal, §15.32.3 r2 statement, §15.30.3 r2 location) via
  `ExceptionState.SetPropagating[Last]` (¶27403 SR2 — an EC-USER name must appear in the PD-header RAISING
  phrase, checked at bind as COBOLNET0717). **Staging raises nothing and tests nothing**, because §14.9.18.4 GR1 b)
  names ONE element twice and it is the other one: "an exception condition is raised in the activating runtime
  element IF CHECKING FOR THAT EXCEPTION CONDITION IS ENABLED IN THE ACTIVATING RUNTIME ELEMENT". The activating
  statement's pickup therefore does all of it — `ExceptionState.TakeRaisedPropagation(profile, …)` tests the
  ACTIVATOR's checking state for the propagated name, and only when it is enabled sets the last exception status
  (§14.6.13.1.1 — the status records conditions that were RAISED) and runs `__EcDispatch` → RESUME → the
  §14.6.13 fatal/nonfatal handling.
  - **The per-name question about a dynamic name is answered by `EcCheckingProfile`** (`Runtime/Exceptions/`):
    the activating statement carries its element's §7.3.25 directive prefix as one interned literal
    (`+EC-USER;-EC-SIZE`), and the runtime folds it with `ExceptionCatalog.DirectiveCovers` — the GR2/GR3/GR4
    expansion, written ONCE and shared with `TurnState`'s compile-time fold (`EcCheckingProfileDriftTests` keeps
    the two answers equal). `TurnState.ProfileAt(line)` builds it; `EcBinder.EcWrap` stamps it on every
    `IActivatingStatement` at the one `BindStatement` exit, recursing into a desugar sequence, so a new
    activating node inherits it by implementing the interface. Whether the pickup is EMITTED still gates on the
    group's EC participation (zero scaffolding); whether it RAISES gates on the profile.
  - **An OPERAND-EVALUATION step — a function reference, an inline method invocation, an object-property
    accessor, a D18 function-bearing subscript's §15.4 temporary store, a sending-value temp — has no statement of
    its own, and ONE landing gives it one** (kb/Work PB892, PB1432). §14.9.33.4 GR2 a) 2. makes the applicable
    statement for a condition an activation propagates "the statement in which the inline invocation or function
    invocation was specified", GR2 a) 1. makes it "the one in which the exception condition was raised" for a
    condition a store raises (NOTE 1: "If an exception condition was raised during the evaluation of 'a', transfer
    would be after the END-IF"), and GR2 a) 3. the LOWEST such statement. `UdfBinder.DrainPending` (the one drain
    of the pending pre-op list) and `OoBinder.OoWrapPropertyOps` mark every step they hand to a carrier
    `BoundStatement.OperandEvaluation`, and `DrainPending` stamps an activation's profile there — a per-evaluation
    window's steps live inside a CONDITION or an OPERAND, where `EcWrap`'s statement-shaped stamp never reaches.
    `StatementBinder.BindStatement` wraps a statement that drained any in `BoundActivationSite` (the
    `OperandEvaluations` save/zero/restore counter, so a nested statement's steps count only toward it).
    `StatementEmitter.EmitStatement` emits a marked step inside `DispatchState`'s operand-evaluation scope, and
    inside it the ONE landing, `DispatchState.ResumeTransfer`, THROWS `RaiseResumeSignal` for RESUME AT
    procedure-name and for RESUME AT NEXT STATEMENT; `EcEmitter.EmitActivationSite` catches it around the statement
    with the ordinary landing. No raise site chooses its landing (`EcEmitter.EmitSelection` takes none), and a
    statement-level `goto` requested inside the scope is an internal error rather than a CS0159 against generated
    source (PB1432: a subscript temporary store's size-error landing in a short-circuited operand's lambda). One mechanism covers both shapes the old code split: a HOISTED
    activation (whose -2 used to fall back INTO the statement — a COMPUTE completed with the function's result),
    and a PER-EVALUATION one inside an immediately-invoked lambda (where no `goto` can leave, which is why
    `CallEmitter.FunctionActivationText` used to emit no pickup and the registry DISCARDED every
    condition a function propagated from a PERFORM UNTIL, a SEARCH WHEN, an EVALUATE object or a short-circuited
    operand). That second activation text is deleted: the lambda body is `EmitCall`'s own output, captured, so the
    EC-FUNCTION-NOT-FOUND arm and the pickup are the same in both positions. `EcWrap.QueryFor` asks every
    activation family for a `BoundActivationSite`, because a window's activations are invisible to its
    statement-shaped cases.
  - **An activator that enables nothing raises nothing**, fatal or not, and execution continues as if the
    activation had returned without a RAISING phrase. There is no boundary "terminate loudly" default —
    §14.6.13.1.3 #8 governs a condition that already EXISTS, and GOBACK's §14.9.18.4 GR1 b) stops one coming into existence in an
    unchecked activator.
  - ⛔ **A staged condition NAMES THE ACTIVATION IT WAS STAGED FOR** (kb/Work PB892 Arm B). GR1 b) raises it in ONE
    element — the one the returning element returns to — so the staged slots (`ExceptionEngine._propagated` /
    `_propagatedObject`) carry the ACTIVATING activation's identity, and `TakeRaisedPropagation` /
    `TakePropagatedObject` take a staging only when the pickup runs IN that activation. The identity is the run
    unit's ONE activation record, `ModuleStack` (every mechanism §15.65.4 r5 names pushes a frame there: CALL,
    function reference and the main program in `ProgramTable`, INVOKE and inline invocation in the method body), and
    each frame carries a unique, never-reused activation id (`CurrentActivation` / `ActivatingActivation`). The
    anonymous slot it replaced let a pickup-free activator (an EC-free group — zero scaffolding) leave a staging
    for WHICHEVER EC-active pickup ran next anywhere in the run unit: a separately compiled EC-free main that
    INVOKEd a raising method made a later EC-active program's INVOKE of a method raising nothing run its
    declarative. The CALL path had hidden the hole behind a registry discard after every pickup-free CALL
    (`siteHandlesPropagation: false` → `ApplyPropagationDefault`); an INVOKE is a direct .NET call with no
    chokepoint for one, so the discard was the CALL-only half of a rule and is DELETED — one mechanism for every
    activation kind. A staging no pickup in its activator takes is never raised; a later staging overwrites it.
    The two separately-compiled witnesses are `ExceptionConditionConformanceTests.GobackRaising_*Activation*`;
    `StagedPropagationIdentityTests` (Unit) pins the rule on the engine.
  - **`RAISING LAST EXCEPTION` carries GR1b3a** (`SetPropagatingLast(pdRaising, stmt, loc)`): a level-3 EC-USER
    condition that the containing element's PD-header RAISING phrase does not name propagates as
    **EC-RAISING-NOT-SPECIFIED** (Table 13 Fatal, §14.6.13.1.6, whose third column names this case) instead. The
    header list crosses as one interned `;`-separated literal because only the runtime knows which name is being
    propagated — the STATIC sibling of the rule (§14.9.18.3 SR2) is discharged at bind for the
    `RAISING EXCEPTION exception-name-1` arm, whose name is known then.
  - A MAIN program's GOBACK RAISING has no activator, so its RAISING phrase is IGNORED and the program terminates
    as an ordinary STOP (§14.9.18.4 GR3); an EXIT PROGRAM RAISING with no calling element raises nothing and acts
    as CONTINUE (§14.9.14.4 GR2) — in both cases the staging is for NO activation (`ActivatingActivation` is −1 for
    the main frame), so nothing can take it and the run unit ends normally.
  - ⛔ **Historical (kb/Work PB408):** the enablement test used to be folded into `BoundRaising.Enabled` at bind
    time from the **RAISING element's own** TURN state, and `EmitRaisingStage` branched on it — staging nothing
    for a disabled nonfatal name and throwing `CobolFatalException` inside the CALLEE for a disabled fatal one.
    A declarative in an activator that had enabled the condition never ran when the callee had it off, and one in
    an activator that had disabled it ran when the callee had it on; §7.3.25.4 GR6/GR8 make both reachable inside
    ONE compilation group. `BoundRaising` now has no enablement field at all, by design.
- **The EC-PROGRAM bridge rides `CobolCallException.EcName`.** The registry latches the Table 13 level-3 name
  (NOT-FOUND / RECURSIVE-CALL / CANCEL-ACTIVE — NOT ARG-OMITTED, which is a REFERENCE-time condition of the called
  element, raised by `OmittedFormal` under the element-kind ambient gate; kb/Work PB971); a CALL/CANCEL under
  enabled checking emits a
  name-FILTERED catch (`when (__ce.EcName == …)`) that sets the status and either flags the statement's own
  ON EXCEPTION phrase (it wins — §14.6.13.1.3 #1) or runs the F3 selection + fatal default. A non-enabled name
  falls through — checking-off behavior unchanged.
- **The EC-SIZE bridge rides `CobolSizeError.EcName`.** The runtime kernels latch the precise condition
  (EC-SIZE-ZERO-DIVIDE at the divide kernels; EC-SIZE-TRUNCATION at the TryStore/TryFormat receiver-capacity
  failures; EC-SIZE-OVERFLOW otherwise) and an ENABLED statement routes through the same two-phase TryStore shape
  even WITHOUT the phrase — the phrase, when present, handles (status still set, §14.6.13.1.4 #1).
  **And the family reaches every OTHER statement (kb/Work PB75, 2026-08-18).** §14.7.5: the size error condition
  "may occur as a result of … the evaluation of an arithmetic expression" — a condition, a function argument, a
  subscript, an INVOKE argument, all rendered inline — and without a phrase "processing proceeds as specified in
  14.6.13.1.3". So `CobolSizeError` IS a `CobolFatalException` (its EcName the Table 13 level-3 name); the binder's
  ambient tail queries the family for every non-arithmetic statement (`EcBinder.EcWrap`) and `EcEmitter.
  FatalAmbientGates` carries the four names with no flag (unconditional raise sites, like EC-OO-NULL) — a checked
  IF / DISPLAY / MOVE gets the generic guard: status set, the F3 selection (USE F3 / the enclosing PERFORM's WHEN,
  #4/#5), RESUME or the abnormal termination (#7). ARITHMETIC statements are excluded from that guard by the
  structural marker `IArithmeticStatement` (every bound statement carrying a `SizeErrorPhrase`; `EmitArith` owns
  their §14.7.5 shape). With checking OFF the raise reaches `ProgramTable.RunMain`'s `CobolFatalException` catch
  and terminates loudly (#8 — the implementor's documented choice; `IF 10 ** 100000 > 5` under STANDARD-DECIMAL
  was an unhandled stack trace, exit 127).
- **ONE dispatch per raise (kb/Work PB75).** A guard that processed a fatal condition (the F3 selection ran and did
  not RESUME) marks it `CobolFatalException.Dispatched = true` before rethrowing for the abnormal termination
  (#7); every guard's `catch … when (!Dispatched && name)` lets it pass, and every "dispatched-then-terminate"
  throw (RAISE unresumed, the arithmetic EC-SIZE default, the I-O fatal default, the CALL-site dispatches) throws
  it pre-marked. Before this, EVERY ENCLOSING statement re-dispatched the same condition on its way out — measured:
  a fatal EC-BOUND-REF-MOD raise inside `PERFORM 2 TIMES` ran the USE declarative twice, once from the MOVE's
  guard and once from the PERFORM's, before terminating; and with the new EC-SIZE gate on the PERFORM, a
  PERFORM-WHEN-handled size error fell through to the USE declarative and CONTINUED. Golden
  `pb75_sdidi_overflow_outside_arithmetic`, `SizeErrorDispositionTests`, `EcSizeGuardDriftTests`.
- **`__IoCheckEc`** is the EC-aware after-verb hook a statement with enabled EC-I-O checking calls INSTEAD of
  `__IoCheck`: same F1 behavior (phrase short-circuits §9.1.13.1, GR3a/b selection, the GR4b outward-GLOBAL walk)
  plus the §9.1.13.1 status→EC raise gated by the per-statement compile-time mask (`ExceptionCatalog.IoMaskNames`
  bit order), the F3 tiers BEHIND the F1 tiers, and the fatal-status default (3x/4x/7x/9x). The GR3g
  outward-GLOBAL continuation is realized only on this I-O path — F3 declaratives are not yet GLOBAL-walkable
  (no corpus or conformance driver exercises it; revisit with the OO/2002 wave).
  **⛔ THE RAISED NAME IS NOT DERIVED FROM THE STATUS BY THE HOOK** (kb/Work PB526). §9.1.13.1's
  status→EC correspondence is a DEFAULT: it covers every EC-I-O condition the standard reaches THROUGH a
  status, and it has no entry for the one EC-I-O condition a rule NAMES outright — §13.18.34.4 GR6 b) 2's
  **EC-I-O-LINAGE**, which would read as EC-I-O-IMP off its `'9x'` status. §14.6.13.1.1 licenses the
  override (*"Unless otherwise specified, if more than one exception is detected during the execution of a
  statement, the one that is set to exist is undefined"* — the specific rule is the "otherwise
  specified"), so the hook's `__ec` now comes from `CobolFile.IoConditionName(__f)`, which is
  `FileConnector.IoConditionName ?? ExceptionCatalog.IoEcOfStatus(status)` in ONE place. The connector sets
  the pair through `FileConnector.SetIoCondition` and the single `Status` setter clears the name, so a named
  condition cannot outlive the operation that named it. Everything downstream is unchanged: the name is
  masked by the same per-statement `__mask` (EC-I-O-LINAGE is in `IoMaskNames`, appended — the order is
  a compile-time integer and is APPEND-ONLY), selected by the same F1/F3 tiers, and defaulted by the same
  `IsFatalIoStatus` arm. **⚠ The other two non-status EC-I-O names, EC-I-O-EOP and EC-I-O-EOP-OVERFLOW
  (§14.9.51.4 GR27 a), are still never set to exist** — they have no mask bit and no raise site; the
  channel they would use now exists.
- **WITH LOCATION (§15.30.3 r1 choice):** without the LOCATION phrase this implementation saves NO location
  information — EXCEPTION-LOCATION returns one space, EXCEPTION-STATEMENT 63 spaces. With it, the bind-time
  pre-rendered "element; paragraph[ OF section]; line" string and the Table-12 statement name travel on the
  **AMBIENT statement context** (kb/Work R14): `EmitChecked` emits `ExceptionState.EnterStatement(name, loc,
  withLocationNames)` around each checked statement (save/restore, so a nested activation restores its
  caller's), and the 2-argument `ExceptionState.Set` — the form every raise site uses — resolves the pair from
  it PER-CONDITION (§15.32.3 r1 keys on the RAISED name's own TURN; kb/Work R06). The context is a field of the
  checking state itself (`CheckingFlags.Statement`), so it has the checking flags' scope: every checking BASELINE
  (`PushAllCheckingOff` — a CALL or function activation, a method body, a USE procedure, a PERFORMed range or a
  nested statement list) starts the other source text with NO context and its restore brings the activating
  statement's back; a raise in a callee whose own TURN has no LOCATION answers spaces, never the CALL's name and
  location (kb/Work PB2745). ⚠ The former design recorded
  the pair "at the raise site" via per-site baked literals, and every site the emitter could NOT hand literals
  to (SEARCH's range Sets, CONTINUE AFTER, `CobolString`/`CobolDynString`/`CobolTiming`) answered 63 spaces
  under WITH LOCATION — the F3 defect family. The ONE remaining positional channel is `__IoCheckEc`'s
  per-(name, FILE) `__locMask` (a name set cannot express file-scoped WITH LOCATION); its explicit operands
  always win over the ambient fallback. `AmbientExceptionContextTests` pins the contract.
- **The catalog is NAME-keyed, not a C# enum:** EC-USER-* is an
  OPEN family (§14.6.13.1.1 — user-defined by mention, always nonfatal ¶24505), so the canonical identity is the
  NAME; an enum would need a parallel name channel (two representations — the singular-pattern rule). EC-IMP-*suffix*
  is NOT open here: §14.6.13.1.1 leaves those names, their fatality and their raise sites to the implementor, this
  implementation defines none (CONFORMANCE DOC-A.1-99), and `ExceptionCatalog.TryGet` refuses them — COBOLNET0711 in
  RAISE / >>TURN / USE (kb/Work PB1531); only the compiler-directive RESERVATION (§8.12) tests the spelling.
- **Grammar continuity:** RAISE/RAISING/RESUME/STATEMENT/CONDITION/EC are context-sensitive tokens mirrored in
  `cobolWord` — legal user-defined words at EVERY edition (pinned by a version-matrix continuity test). The
  RAISE/RESUME statement alternatives are UNgated so `--std 85` gets the targeted COBOLNET0876 diagnostic, not a
  nameless parse error. `displayStatement` gained the `functionCall` operand alternative (§8.4.4.1 — an identifier
  includes a function-identifier; `DISPLAY FUNCTION EXCEPTION-STATUS` is the canonical interrogation shape).
- **Diagnostics band:** COBOLNET0710 (RAISE of a non-level-3), 0711 (unknown exception-name), 0712/0713/
  0714 (RESUME SR1/SR2/SR3), 0715/0716 (USE F3 SR13/SR14), 0717 (RAISING SR2 ¶27403), 0718/0719 (TURN SR3/SR1+SR4),
  0875–0879 (the 2002+ edition gates: TURN / RAISE+RESUME / USE F3 / per-name edition window / SET LAST EXCEPTION+
  RAISING).
- **Verification:** `ExceptionConditionConformanceTests` (48 spec-pinned facts — TURN scoping/expansion, RAISE
  fatal/nonfatal × enabled/off, RESUME both forms, F3 tier selection, the SIZE/OVERFLOW/I-O/PROGRAM/
  ARGUMENT-FUNCTION bridges, RAISING propagation both verbs, the EXCEPTION-* functions, the edition gates, the
  zero-scaffolding invariant asserted on generated source) + `TurnStateTests`/`ExceptionCatalogTests` (13 unit
  facts: the GR2/GR3 EC-I-O-WARNING exclusion, file-scoped events, last-event-wins, strict GR5 lines, Table 13
  fatalities, the §9.1.13.1 status map). The legacy engine has NO EC model — every expected value derives from the
  cited §.
- **The location string's three parts (§15.30.3 r2b — kb/Work PB63):** part 1 is the ELEMENT name "as specified in
  the FUNCTION-ID, METHOD-ID, or PROGRAM-ID paragraph of the … element containing the statement" — a statement inside
  a method names the METHOD-ID (`OoMethodScope.MethodName`; `ctx.EcState.ProgramName` is the program/class fallback);
  part 2 is the procedure field — (a) no paragraph-name and no section-name: EMPTY (`"; ; "` — the
  paragraph-name-OMITTED paragraph of §14.4.3 carries the empty name in `ProcedureTableBuilder`, never a display
  placeholder), (b) `paragraph[ OF section]`, (c) a section with no paragraph: the section-name alone; part 3 the
  implementor-defined line identifier — the resultant-text line (§7.2; CONFORMANCE.md ⚖ determination; kb/Work PB82
  for the source-line map). One builder, `EcBinder.EcLocation`.
- **EXCEPTION-FILE / EXCEPTION-FILE-N (§15.28.4 / §15.29.4 — kb/Work PB63):** the file-name is "exactly as
  specified in the SELECT clause" — `FileModel.SelectName` (never qualified, never case-folded) is registered on the
  connector (`FileConnector.SelectName`, the trailing `selectName:` argument of every `CobolFile.Register*`) and
  both the no-argument form (`EcFunctions.File` → `CobolFile.SelectNameOf(key)`) and the connector-argument form
  (`FileRegistry.ExceptionFile`) read it; the registry KEY (`PROG::`, `::EXT::` + the uppercased external name,
  `Class::INST::` + `#N`) is an emit-side namespace and is never displayed. r2a's "never been opened, attempted to be
  opened, or otherwise attempted to be accessed" is recorded on the ONE I-O-status assignment path — the
  `FileConnector.Status` setter (§9.1.13.1's CLOSE/DELETE/OPEN/READ/REWRITE/START/UNLOCK/WRITE all set it), so a
  CLOSE or READ on a never-opened connector (42/47) is an access. Argument-1 shall be "specified in an FD statement"
  (§15.28.3 r1 / §15.29.3 r1): `BindExceptionFileArg` requires `HasFd && !IsSortMerge` and cites the function's own
  clause (COBOLNET1574). Pinned by `pb63_exception_file_select_names` and the `pb63-exception-file-*` negatives.

**Automatic propagation — `>>PROPAGATE` (ISO §7.3.21; kb/Work PB1119).** The directive stage
(`PropagateDirectiveProcessor`) collects ON/OFF toggles into a `DirectiveTimeline` like every line-scoped directive
(PUSH/POP replayed; `DirectiveStateRegistry` carries it), and the binder folds them ONCE per source element at its
first line (`PropagateState` → `DataBinder.AutomaticPropagation` for a program or function,
`OoMethodSymbol.AutomaticPropagationHere` for a method — §7.3.21.4 GR1/GR3, default OFF by GR4). The emitter holds
the element's `AutomaticPropagation` (its GOBACK signal and its header RAISING classes) in `EcState.Propagation`, and
the rule is applied where the fatal default is decided: `EcEmitter.EmitSelection` emits, between §14.6.13.1.3 5)
(a declarative ran → terminate) and 7), `if (r == -3) <stage LAST; GOBACK>` — `ExceptionState.StageAutomaticPropagation`
excludes EC-FLOW-GLOBAL-EXIT/-GOBACK and stages the LAST exception for the activator, whose ordinary pickup then
raises it (§14.9.18.4 GR1 b)), so a chain of PROPAGATE ON elements propagates hop by hop. The EC-I-O arm of
`__IoCheckEc` applies the same return (§14.6.13.1.3 3)). A program propagates only while `__asCalled`; the
nonfatal and main-program readings are CONFORMANCE.md D-PROP. An element without the directive emits byte-identical
text.

**Still later waves:** the exception-checking PERFORM WHEN + `>>PROPAGATE` (2023 — VCR row 79/§4808),
RAISE/RAISING identifier (exception OBJECTS — the OO wave; the `ExceptionState.ExceptionObject` slot exists),
GLOBAL-walkable F3 declaratives, VALIDATE/EC-VALIDATE (§18.17). *(The national `-N` twins are LIVE — P10 Step 11:
`EcFunctions.FileN`/`LocationN` (§15.29/§15.31) = the base renderings through the ONE `CobolIntrinsics.NationalOf`
repertoire translator, result category National; golden `exception_file_n`, matrix row `exception-file-n-2002`,
85-window negative.)*

## Decisions

### D1. Conditions are bound to backend-neutral BoundCondition nodes (BoundRelational/BoundLogical/BoundNot/BoundCondition88/BoundSignCondition/BoundClassCondition); the Roslyn backend's ConditionRenderer.Render(BoundCondition)→string emits them as pure, side-effect-free C# boolean expressions. The grammar's rule cascade (logicalOr→logicalXor→logicalAnd→unaryLogical→primaryCondition) fixes precedence at parse/bind time. (src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs.)

**Rationale.** COBOL condition precedence NOT>AND>XOR>OR is already encoded by the grammar's rule cascade, so precedence is preserved by construction without us re-grouping. Pure expressions compose into if/while(!(…))/?:/EVALUATE arms/level-88 properties — one translator serves every consumer.

**Rejected alternatives.** Lowering conditions to imperative IR with temporaries (the legacy IrBinaryLogical model) — unnecessary in a C# target where the host language already has boolean expressions; it would also force statement context where an expression is wanted (e.g. ?:).

### D2. Fully parenthesize every emitted binary boolean node: (a && b), (a || b), (a ^ b).

**Rationale.** C#'s bool precedence is ! > & > ^ > | > && > || — so ^ binds TIGHTER than &&/||, which does NOT match COBOL's AND>XOR>OR. Explicit parens make the emitted tree's grouping exactly the COBOL parse tree's grouping, independent of any C# precedence subtlety.

**Rejected alternatives.** Rely on C# operator precedence — the ^-vs-&& ordering mismatch is a genuine correctness trap for logical XOR.

### D3. Emit short-circuiting && / || for COBOL AND / OR — the left-to-right evaluation order ISO §8.8.4.13 rule 1 mandates.

**Rationale.** ISO §8.8.4.13 rule 1: within a hierarchical level "the constituent connected conditions … are evaluated in order from left to right, and evaluation of that hierarchical level terminates as soon as a truth value for it is determined regardless of whether all the constituent connected conditions within that hierarchical level have been evaluated." Short-circuiting `&&` / `||` render exactly this: once the left operand fixes the level's truth value, the right operand is not evaluated. This is the conformant order (and is also idiomatic and faster). Corroborated corpus-safe: a scan of tests/nist/programs found ZERO guard-then-same-variable-subscript idioms; the 44 'AND <subscripted>' cases use a subscript independent of the guard (e.g. IF SUB4 = 6 AND WZ-X-CHAR(SUB2) = SPACE).

**Rejected alternatives.** Eager (non-short-circuit) evaluation as in the legacy ConditionLowerer lines 188–196 (both operands into temporaries, then combine) — non-conforming: §8.8.4.13 rule 1 requires evaluation of a hierarchical level to stop once its truth value is known, so eager evaluation can execute a right operand (a subscript, a function, a side effect) the standard requires be skipped. For example `IF I>0 AND TABLE(I)=X` must not reference TABLE(I) when I ≤ 0 — short-circuit is the only faithful rendering; the legacy engine's eager evaluation is the behavior to be corrected, not preserved.

### D4. EVALUATE (all forms) lowers to a chained if/else-if/else, NOT a C# switch.

**Rationale.** This is exactly ISO §14.9.13.4 GR4 (process each WHEN left-to-right, first match wins). COBOL WHEN arms are ranges, conditions, multiple ALSO subjects, ANY, partial expressions, and arbitrary per-subject values — they are not constant case labels. The if/else-if chain is correct, readable, and the C# compiler optimizes dense integer chains.

**Rejected alternatives.** C# switch — illegal for non-constant labels; ranges/conditions/ANY/partial-expressions have no switch form. (A future peephole may detect a single-subject all-single-integer EVALUATE and emit a switch for prettiness.)

### D5. EVALUATE selection subjects are hoisted into locals (var _e0=…; var _e1=…;) evaluated exactly once before the chain; bare identifiers/literals may stay inline.

**Rationale.** ISO §14.9.13.4 GR3: each selection subject is evaluated once at the start. Side-effecting subjects (functions, arithmetic) must not be re-evaluated per WHEN. Hoisting is always correct; inline is a readability shortcut only for the no-side-effect case.

**Rejected alternatives.** Re-render the subject in each WHEN match — re-evaluates side effects and arithmetic per arm; wrong.

**⛔ "Once" includes ONCE WHEN NO ARM READS IT, and the exemption is narrower than "one use" (kb/Work PB396).** GR3's evaluation is an obligation of the STATEMENT — "at the beginning of the execution of the EVALUATE statement" — not of whichever arm happens to consult the subject, and a COBOL subject evaluation is observable: it can name an undefined item (a compile-time §8.4.2.1 refusal), subscript out of range (EC-BOUND-SUBSCRIPT), or divide by zero (EC-SIZE-ZERO-DIVIDE). `EvaluateBinder` materialized the subject slot LAZILY, from inside the pair binder, so two shapes evaluated it NOWHERE: an EVALUATE whose every object is `ANY` (GR4 a) 1. makes the pair true without consulting the subject, so nothing renders it), and — before §14.9.13.2's grammar repair — an EVALUATE whose only clause was `WHEN OTHER`. All three losses were measured against the identical program with one dead `WHEN 999` arm added. Two corrections: `Bind` touches every slot BEFORE any arm binds, and the hoist exemption is now "exactly one relational use, IN THE FIRST ARM" (`SubjectUsage.NeedsIntermediate`) rather than "exactly one use" — a single use in a LATER arm sits inside an `else if` an earlier arm can skip, which is not "the beginning of the execution". `ANY` contributes ZERO uses, not one.

### D5a. EVALUATE classifies a selection PAIR, not two operands — ONE classifier, and §14.9.13.3 SR6 is a joint step over it.

**Rationale.** Table 15 (§14.9.13.3 SR10) is indexed by a subject COLUMN and an object ROW naming the same six operand kinds, and SR6 changes a BOOLEAN operand's kind "for a particular WHEN phrase" according to what the OTHER side of the pair is: against TRUE/FALSE a one-boolean-character boolean expression becomes condition-1 (b) / condition-2 (a); against anything else it stays boolean-expression-1 (d) / boolean-expression-2 (c). A per-side classifier therefore cannot express the rule, and two of them cannot even express the TABLE: every difference between them is a Table-15 asymmetry. So `EvaluateBinder.ClassifyPair` classifies the pair — `SubjectKind` / `ObjectKind` handle only the forms that are grammatical on one side (TRUE/FALSE and the subject's own class test; ANY, a THRU range and an explicit condition object), and everything else goes through the ONE `BareOperandKind` over the ONE `ConditionBinder.AnalyzeBareOperand`, mapped row→column by `EvaluateOperandCombinations.AsSubjectOperand`. SR6 a)/b) then run as a joint step; c)/d) are the identity, because a boolean operand's raw kind already IS boolean-expression.

A bare operand resolves ONCE. `AnalyzeBareOperand` returns which of the four §8.8.4 shapes it is (a plain value, a level-88 condition-name §8.8.4.2.7 r2, a switch-status condition-name §8.8.4.6, or a boolean expression §8.8.2) together with the bound condition or the bound boolean expression and that expression's §8.8.2 rules 9/10 RESULT length. The screen and the bind path both read that one analysis: nothing is resolved twice (an ambiguous condition-name used to report §8.4.2.2 twice), and a boolean operand is not wrapped as a §8.8.4.3 simple boolean condition — which is where that clause's SR1 length screen lives — until the classification says it IS one.

SR6's length test is `ConditionBinder.BoolResultLength`, a SIBLING of §14.9.8.4 GR3's COMPUTE width rather than the same question: GR3 counts ITEMS only (a literal contributes none), while a boolean LITERAL's own length is exactly what SR6 needs. A positionless operand (figurative ZERO / `ALL B"…"`), a run-time-length boolean function and a run-time-length reference-modified slice (`B(1:N)`, `B(K:)`) report null and are answered YES — failing OPEN, the direction that cannot refuse legal source; §8.8.4.3.3 SR1's length-1 screen (`BoolExprAllLengthOne`) fails open on the same two run-time shapes. Both static readers take an item's length through ONE reader, `StaticBoolRefLength`, over `RefModPlace.StaticLength` (a literal length, or an omitted length with a literal start).

GR3's width itself is NOT a bind-time number: an operand's length can be a run-time quantity, so `BooleanRenderer.RenderAtItemWidth` carries it BESIDE the value (`CobolBoolSized` — each item leaf `CobolBool.Item`, each literal `CobolBool.Literal`, the operators' overloads keep the max, `CobolBool.ToItemWidth` applies it once). COMPUTE Format 2 and the BY CONTENT boolean argument of CALL and INVOKE all render through it (kb/Work PB589 — the bind-time `Gr3Width` it replaced counted `B(1:N)` at the inner item's full length and a run-time-length BOOLEAN-OF-INTEGER as no item).

GR3 a) — "if the selection subject is a numeric data item or a boolean data item whose length is one boolean position, the selection subject for this evaluation is treated as identifier-1 and not an arithmetic or boolean expression" — is a GENERAL rule scoped to that evaluation: it fixes how the subject's VALUE is obtained (the item's value, not an expression evaluation), realized here by binding a bare boolean reference to a `BoundBoolRef` read. It does not move the operand out of Table 15's boolean-expression column, because §8.8.2 makes "an identifier referencing a boolean data item" a boolean expression and SR6 — a SYNTAX rule, explicitly per-WHEN-phrase — is what classifies it. The other reading leaves SR6 b) with no effect on the shape it plainly mirrors.

**A boolean EXPRESSION is a selection operand, in both positions (kb/Work PB1412).** §14.9.13.2 prints `boolean-expression-1` as a selection subject and `[ NOT ] boolean-expression-2` as a selection object, and Table 15 has a Boolean-expression column and row. The grammar names them: `evaluateSubject` and `evaluateWhenItem` each carry `{boolExprAhead()}? booleanExpression`, AFTER `valueOperand` (so a bare or parenthesized boolean operand parses as it always did) and BEFORE `condition` (so an operator-bearing expression with no relational tail is the Boolean-expression kind and not a condition held to §8.8.4.3.3 SR1's length-1 rule); a boolean RELATION (`A B-AND C = B"1000"`) has a tail the alternative cannot consume and stays a condition. `boolExprAhead` stops at ALSO, so one subject's scan never sees the next subject's operator. The expression is analysed by the ONE bare-operand analysis (`ConditionBinder.AnalyzeBooleanExpression` → `BareOperandAnalysis` form Boolean with the §8.8.2 rule 10 result length), so SR6's reclassification and Table 15 read the same two facts they read for a boolean item, with no second rule. GR3 d) assigns the subject "a boolean value": the slot's one bound tree is a `BoundBoolOperand` and, where more than one arm reads it, is stored in a boolean intermediate of its result length (`SendingValueTemp.MaterializeBoolean`, the store a boolean function argument takes) as a statement PRE-op. GR4 a) 6. builds a boolean-expression-2 pair through the ONE relation checkpoint (`subject [NOT] = object`), exactly as the IF form does. The operator-FREE spelling the discriminator can still take (`(B"1")`) reduces to its bare operand (`ConditionBinder.BareOperandOf`). The two new grammar sites are introduction-gated like every other host (`VersionConformancePass.VisitEvaluateSubject` / `VisitEvaluateWhenItem`; `BooleanExpressionGateSiteDriftTests` derives the list from the grammar). A Table 15 refusal ends the pair's binding: one COBOLNET1634, not that and the secondary diagnostics the same pair would draw about the same operand.

**The Condition column pairs with the Condition ROW, not only with TRUE/FALSE.** Table 15 marks the Condition object row `Y` under both the Condition and the TRUE-or-FALSE subject column, and §14.9.13.4 GR4 a) 3. and a) 4. state that cell's analysis in one shared sentence — "if the truth value of the selection subject and selection object match, the result of the analysis is true" — which GR3 e) ("any selection subject specified by condition-1 is assigned a truth value") makes meaningful for a condition-1 subject. Each rule's first sentence names the form the OTHER side takes in the case it was written for; neither retracts the cell. The pair lowers to `BoundNot(BoundLogical("^", [subject, object]))` — NOT XOR, so both truth values are evaluated exactly once, GR4 c)'s short-circuit being between PAIRS and never inside one.

**Rejected alternatives.** Two per-side classifiers with the boolean rows declined (the shipped shape until kb/Work PB400): it produced a run-time crash on conforming source (`EVALUATE BW WHEN TRUE`), a false rejection of a switch-status subject that the object side accepted, and a silently accepted blank cell (`EVALUATE -5 WHEN 6`). Classifying a boolean operand as a condition unconditionally: rejects the legal `EVALUATE BW WHEN B"01"` over a `PIC 1(2)` item. Answering SR6 in the BIND path only: the Table-15 screen then disagrees with what is bound, which is the split the joint classifier exists to make impossible.

**The sole-literal question is not the sole-primary question.** §14.9.13.4 GR1 ("if an operand consists of a single literal, that operand is treated as a literal, not as an expression") is answered by `SoleOperand.NumericLiteral`, which admits ONE sign ADJACENT to the digits per §8.3.3.3.2 rule 2, over the ONE contiguity test `ArithmeticFormationRules.SignIsPartOfLiteral`. `SolePrimary` keeps answering §8.8.4.7.3 SR2's "a single data item … not enclosed in parentheses", for which a sign makes the operand compound. §7.3.11.4 GR5 (DEFINE) and §13.10.3 SR1 (CONSTANT) ask GR1's question in the same words and share the same helper.

### D5b. Every EVALUATE syntax rule is screened where its SCOPE is, and §8.8.4.2's operand rules belong to the ONE relation checkpoint — never to a caller of it.

**Rationale.** §14.9.13.3's syntax rules split by the thing they speak about, and the screens follow that split rather than the binder's control flow. `ScreenPairing` owns SR10 (Table 15 — the PAIR). `ScreenObjectCount` owns SR2 (the two SETS). `ScreenRangeOperands` owns SR4 and SR9 (the range's two OPERANDS). SR7 a) owns nothing of its own, because it is a DELEGATION — the selection objects "shall be valid operands for comparison to the corresponding operand in the set of selection subjects in accordance with 8.8.4.2"; it is satisfied by routing every pair through the SAME `BoundRelational` construction a written relation uses.

**SR2 is a rule, not a bounds check (kb/Work PB399).** "The number of selection objects within each set of selection objects shall be equal to the number of selection subjects" is an EQUALITY, and a loop can only notice the direction in which it runs out of subjects. `Bind` used to carry `if (i >= subjects.Length) return new BoundUnsupported(…)` inside the pairing loop: an index guard wearing a rule's clothes, which delivered the MORE direction as a run-time `NotImplementedCobolFeatureException` (and only if that WHEN was reached) and the FEWER direction not at all — the loop simply ran to the number of objects written, leaving the surplus SUBJECTS unpaired and the phrase matching on a strict subset of its own selection set. `ScreenObjectCount` compares the two counts per WHEN PHRASE (a "set of selection objects" is one phrase, so consecutive phrases sharing an imperative-statement are counted separately), reports **COBOLNET2106** at every edition, and the loop then clamps to `min(objects, subjects)` as pure recovery.

**SR4 and SR9 are ONE screen over the range PAIR, and the Table-15 screen is structurally the wrong place for them.** `ObjectKind` names a `valueRange` as a single `RangeExpression` row and never looks inside it, which is correct — Table 15 is about the PAIRING. `ScreenRangeOperands` runs from the `valueRange` arm BEFORE the collating class is asked, reports **COBOLNET2107**, and returns a verdict: a rejected range yields a `BoundConditionError`, so the comparison its operands are inadmissible for is never lowered. **CLASS here is §8.5.2.1 Table 2's**, read through the ONE Table-2 lattice (`IntrinsicArgumentRules.ClassOf` projected by `TableTwoClass`) and never through `CollatingSelection.ForComparison`, which answers the different two-operand question of which §8.8.4.2 rule a RELATION selects and deliberately collapses alphabetic onto alphanumeric. A figurative answers a candidate SET rather than a class and abstains, which is the direction that cannot reject legal source. **SR9's term is §8.5.1.12.1's** — "a group item whose data description has at least one dynamic-length elementary item or dynamic-capacity table as a subordinate item" — so an OCCURS DEPENDING ON group is NOT one and a range ended by two of them is legal; the test is `ReferenceResolver.HasVariableLengthSubordinate`, the same definition the REDEFINES, VALUE and report screens ask.

**The §8.8.4.2.2 Format 3 band lives at the checkpoint (kb/Work PB399).** "A relation condition involving operands of class message-tag, object, or pointer is a message-tag-object-or-pointer-reference relation condition" (§8.8.4.2.1); that format prints only `IS [NOT] EQUAL TO` / `=` / `<>`, and §8.8.4.2.3 SR5 requires both operands to be of one of those three classes AND of the same CATEGORY. The band was written inside `ConditionBinder.BindComparison`'s relation arm — one CALLER of `CheckedRelational` → `StatementValidation.CheckRelationalOperands` — so it screened `IF WS-P >= WS-Q` and said nothing about the identical pair under EVALUATE (which ordered raw addresses), under an EVALUATE range, under SEARCH WHEN, under PERFORM UNTIL or under an abbreviated relation; `EVALUATE WS-P WHEN WS-X` reached the BACKEND and failed as a raw C# `CS1503`. It is now `StatementValidation.CheckFormat3Relation`, beside the class-boolean and strongly-typed-group rules of the same §8.8.4.2 band. Consolidating the two per-class copies also closed two holes neither had noticed: **SR5's same-CATEGORY test** (a data-pointer against a program-pointer was accepted), and class membership read as `Pic?.Category == PicCategory.Pointer` — one of the THREE categories Table 2 gathers into class pointer, so a program-pointer or function-pointer operand left the whole band silent. The predefined NULL takes the other operand's class (§8.4.3.10.1, §8.4.3.9) — but ONLY opposite an operand of class object or pointer; opposite any other decidable operand it keeps class pointer, so `IF X = NULL` / `IF N = NULL` / `EVALUATE X WHEN NULL` are Format 3 relations SR5 refuses (COBOLNET0869, kb/Work PB1427), where borrowing unconditionally had left the band silent and run NULL as LOW-VALUE — and it is exempt from the category test, §8.4.3.10.3 SR1 a) admitting it "in a pointer-or-object-reference relation condition" by name. NULL opposite NULL has no associated data item to take a class from, so no SR1 case applies and the pair is COBOLNET2576 (the one SR1 screen; kb/Work PB1427). The class-boolean band asks the same lattice's CANDIDATE set: an operand is boolean-compatible iff class boolean is among the classes it may present (ZERO may, §8.3.3.6.4 GR4; SPACE / QUOTE / HIGH-VALUE / LOW-VALUE may not; an index data item is class index), so `IF B1 = SPACE` draws COBOLNET0844.

**The general relation has ONE comparability table (kb/Work PB1468).** §8.8.4.2.1's "Comparisons are defined for the following:" is a CLOSED thirteen-item list. The checkpoint's other bands own items 4 (boolean), 9–11 (Format 3), 12 (strongly-typed groups) and 13 (variable-length groups); the rest — items 1–3 and 5–8 over classes numeric, alphabetic, alphanumeric, national and index — is `RelationComparability.Breach`, asked from `StatementValidation.CheckGeneralRelationComparability` for every pair no other band claims. Two rules there are class/usage/form facts of the source: §8.8.4.2.5's item-6 restriction ("The numeric integer operand shall be an integer literal or an integer numeric data item of usage display or national" — **COBOLNET2532**, an error with the leniency behind `--permissive` as a warning, the DA6 disposition) and §8.8.4.2.13's "Relation tests may be made only between" three index pairs (**COBOLNET2533**, unconditional: an index-name against an alphanumeric operand has no comparison to fall back on and used to abort the run unit). Class is read from the ONE Table-2 lattice (`IntrinsicArgumentRules.CandidateClasses`): a figurative is a candidate SET (ZERO opposite a character operand takes its character reading and is never item 6's numeric operand), an empty set fails open, and an INTEGER intrinsic function is admitted as item 6's integer operand by the same determination that moves it to an alphanumeric receiver (docs/CONFORMANCE.md D-RELCLASS). `CollatingSelection.ForComparison` still answers which §8.8.4.2 rule a DEFINED pair selects; it now only ever sees defined pairs. **Both comparison nodes pass the checkpoint, and it judges the operands AS WRITTEN.** An EVALUATE THROUGH range with an IN alphabet-name or EC-RANGE-INVALID checking lowers to `BoundRangeMembership`, which used to be built outside the checkpoint (the sibling arm: `EVALUATE NB WHEN "0001" THRU "0020" IN AL` compiled clean); it is now `ConditionBinder.CheckedRangeMembership`, which asks the checkpoint for §14.9.13.4 GR4 a) 5.'s two relations. And a subject read by more than one WHEN is materialized into an intermediate (§14.9.13.4 GR3) whose description is the implementor's — an index-name's is numeric — so `EvaluateBinder.BindSubjectValue` records which written operand the intermediate stands for (`ConditionBinder.RecordIntermediate`) and the checkpoint reads through it; the emitted node keeps the intermediate. **Drift test:** `RelationComparabilityTests` writes every ordered pair of 22 operand kinds and checks each verdict against the list as the standard words it (the expectation is computed from the kinds' declared properties, never from the table), plus the same breaches at every relation surface.

**One §8.8.4.2.1 rule is about the written CONDITION, not the compared pair, so it is asked one step above the checkpoint (kb/Work PB1470).** "A relation condition shall contain at least one reference to an operand that is not a literal" judges a relation condition the program writes. Every such condition is built through `ConditionBinder.WrittenRelational` — an IF, PERFORM UNTIL or SEARCH WHEN relation, a boolean relation, an abbreviated relation's inserted subject (`IF 1 = N OR 2` holds the relation `1 = 2`) and an EVALUATE partial-expression (§14.9.13.3 SR8: "treated as though it were specified as condition-2") — which asks `StatementValidation.CheckRelationHasNonLiteralOperand` of the operands as written and then calls `CheckedRelational`. EVALUATE's subject/object pairs and ranges call `CheckedRelational` directly: §14.9.13.3 SR10's Table 15 is the rule for which of those pairs are permitted, and it refuses a literal object against a literal subject while permitting a range-expression against one. A figurative constant, a symbolic-character and a constant-name are literals (§8.3.3.6; §8.3.2.2.29; §13.10.4 GR1); a data item, an arithmetic expression and a function-identifier, folded at bind time or not, are not. The indication is **COBOLNET3146, a WARNING** at every edition (§4.2.2's warning mechanism; docs/CONFORMANCE.md D-RELLITERAL), and the relation evaluates as written. **Drift test:** `RelationLiteralOperandTests` writes every ordered pair of 16 operand kinds (expected verdict computed from the kinds' one declared property) and every relation surface, with the EVALUATE pair, range and data-item-subject abbreviation as the complement.

**Drift test.** `Format3RelationSurfaceDriftTests` flips the axis the subject holds fixed: ONE operand pair, five relation SURFACES, plus a legal-pair complement leg so the theory cannot pass by rejecting everything. It is what catches a later refactor that puts the rule back into one caller.

**Rejected alternatives.** Copying the §8.8.4.2 question into the range screen (the shape kb/Work PB399 sketched): it makes two mechanisms for one rule and leaves SEARCH/PERFORM/abbreviated untouched — the defect this cluster keeps finding. Writing a second Table-2 class reader for SR4: `IntrinsicArgumentRules` already is one, already models the figurative candidate set, the alphabetic split, the index row and the one-class-three-pointer-categories row; a private copy was written and deleted. Leaving SR4's excluded classes to the relation checkpoint: an inverted or `IN alphabet-name` range builds a `BoundRangeMembership` and reaches no relation at all, so half the ranges would go unscreened.

### D6. Level-88 condition-names become C# expression-bodied static bool PROPERTIES derived from the conditional variable's live value (not stored bools).

**Rationale.** ISO §8.8.4.5: a condition-name is an abbreviation for 'conditional variable == one of its values'. A property recomputes truth from the current value, so any MOVE/arithmetic to the parent is reflected with no bookkeeping. SET cond TO TRUE/FALSE writes the PARENT, never a bool.

**Rejected alternatives.** A stored bool kept in sync on every assignment to the parent — fragile, requires intercepting every write path; semantically wrong (the value can change via REDEFINES/group MOVE).

### D7. SET cond-name TO TRUE moves the FIRST VALUE literal into the conditional variable; SET cond TO FALSE moves the WHEN SET TO FALSE literal (error COBOLNET2049 if none).

**Rationale.** ISO §14.9.39.4 GR6 (TRUE → "*the literal in the VALUE clause associated with condition-name-1*"; "*If more than one literal is specified … the value of the first literal that appears*", which for a THRU range is the range start) and §14.9.39.4 GR7 (FALSE → "*the literal in the FALSE phrase of the VALUE clause*"), which §13.18.63.4 GR20 states from the VALUE clause's side. §14.9.39.3 SR7 makes the FALSE phrase required for SET TO FALSE.

**⛔ ONE ARM, NOT TWO** (kb/Work PB555). GR6 and GR7 are the SAME sentence with one word changed — both place their literal "*in the conditional variable according to the rules for the VALUE clause, except that when the conditional variable is an alphanumeric group item, bit group item, or national group item to which a table is subordinate, its length is determined as specified in 13.18.38 … If the length of the conditional variable is zero, the SET statement leaves it unchanged*". So `BoundSetConditions` carries a single `ToTrue` flag that selects WHICH literal, and `SetEmitter.EmitSet` has one store path: the FALSE arm inherits the figurative fill, the group-image splice and the category funnel without a second copy. The FALSE arm returned `BoundUnsupported` until `Condition88` carried literal-4 at all; `SetBinder.BindSetCondition` now only has to enforce SR7 before binding.

**⛔ AND THE STORE IS NOT A RECIPE OF THE SET EMITTER'S AT ALL** (kb/Work PB560). GR6 does not describe a
store; it says the literal "*is placed in the conditional variable according to the rules for the VALUE clause*",
which names a recipe that already exists. `SetEmitter.EmitSet` therefore calls
`DataEmitter.ValueImageOf` → `ValueInitializer.InitializerFrom` — the SAME method that renders the conditional
variable's OWN VALUE clause and the report section's format-4 operand — and spells no part of the composition
itself. It used to carry a private three-arm switch, and every arm that recipe had and the switch did not was a
silent wrong answer: a NUMERIC-EDITED variable stored the raw literal text where §13.18.63.3 SR6 composes the
MOVE-converted image (`PIC ZZ9.99` held `10    `, not ` 10.00`), a FLOAT variable scaled the literal at its own
scale of zero (`VALUE 0.5` stored 0, against §13.18.63.4 GR17 → GR1), BLANK WHEN ZERO was ignored
(§13.18.63.3 SR8 NOTE 2), a PICTURE format-2 (LOCALE) variable had no runtime compose, and a whole-group-aliased
numeric variable handed a `long` to a `string` field — a Roslyn CS1503 that failed the compilation of legal
COBOL. Each is observable as a ROUND TRIP, because §8.8.4.5.3 GR3 makes `SET cond TO TRUE` followed by `IF cond`
an identity: the TEST arm already read the operand through this recipe's shared pieces, so every one of them
returned FALSE on the line after its own SET. `InitializerFrom` reads the receiver through `DataItem.OperandPic`
(the ONE category reader), and describes an ORDINARY group from the VALUE clause's own rule for that subject
(§13.18.63.3 SR4 — alphanumeric literals, bounded by the size of the group item), so the three group shapes
GR6 names explicitly are the recipe's own business. Held by
`Cobol.Net.Tests.Unit.ConditionValueRecipeDriftTests` — the SET emitter may name no part of the VALUE recipe,
the numeric-edited image has exactly two readers, and the recipe reads `OperandPic`.

⚠ **The code is COBOLNET2049, not the COBOLNET0705 this decision reserved.** 0705 was a placeholder that was never registered in `DiagnosticCatalog` and never reached `docs/DIAGNOSTICS.md`; diagnostic codes are now allocated centrally per fix, and this one was allocated with PB555.

**Rejected alternatives.** Treat SET cond TO TRUE as setting a bool flag — wrong; it is a MOVE of a specific literal into the parent per the VALUE-clause rules.

### D8. Class conditions (NUMERIC/ALPHABETIC/-LOWER/-UPPER/user CLASS) run over the character image via a new CobolClass runtime; for a pure native scaled-integer (long/Int128) item, IS NUMERIC folds to true (COBOLNET0706).

**Rationale.** In the typed model the value IS the field; class tests operate on the char image. A native numeric item cannot hold non-digits, so NUMERIC is constant-true (the meaningful test is on a PIC X holding digits). ALPHABETIC is the closed Latin set {A-Z,a-z,space} (ISO §8.8.4.4) — NOT char.IsLetter (must reject Unicode/accented letters; legacy comment).

**Rejected alternatives.** Reuse the legacy byte-buffer PicRuntime predicates — rejected byte substrate. Use char.IsLetter/char.IsDigit — wrongly accepts Unicode letters/digits; ISO defines closed character sets.

### D8a. ⛔ The §8.8.4.4.2 ALTERNATIVES and their §8.8.4.4.3 OPERAND RULES are ONE TABLE — `ClassConditionModel` — and one grammar rule, `classConditionName`.

**The scope correction (CLAUDE.md rule 5).** kb/Work PB571 and PB590 were filed as "the class-condition arm leaks SR1" and "one of the fourteen alternatives is missing". Implementation found the alternatives written down in FOUR places: the class-condition grammar rule (now `classConditionName`, kb/Work PB2292); a SECOND grammar rule `classCondition` serving `evaluateSubject`, which offered ALPHANUMERIC (not one of the general format's fourteen) and omitted BOOLEAN, class-name-1 and alphabet-name-1; the kind decode in `ConditionBinder.BindClassConditionOn`; and a third decode in `EvaluateBinder.SubjectAsCondition`. The OPERAND screen was a fifth partial — it returned early unless the operand's category was boolean, so SR1 was asked of nothing, SR3 and SR5 had no arm, and SR4 tested one of the three categories it names.

**As built.** The general format was read off the PRINTED page (PDF 224 / printed 194): fourteen alternatives in one brace group, no choice indicator, `IS` not underlined, every keyword underlined, alphabet-name-1 and class-name-1 not.
- **Grammar.** `classConditionName` (Core/CobolExpressions.g4) is the one alternative list; `classCondition` is deleted, and an EVALUATE class-test subject is now simply one shape of `evaluateSubject`'s `condition` alternative (kb/Work PB842 — condition-1 is the ONE `condition` rule). BOOLEAN sits AFTER `cobolWord` and that order IS its COBOL-2002 edition gate: `cobolWord`'s `{userWordHere("BOOLEAN")}?` alternative matches only below 2002, where `CLASS BOOLEAN IS "0" THROUGH "1"` is conforming source — so no binder-side introduction gate exists or is needed (the boolean-operator/XOR precedent).
- **Model.** `ClassConditionModel` (Binding/ClassConditionModel.cs) holds one row per OFFERED alternative — kind tag, the phrase a diagnostic names it by, and the §8.8.4.4.3 rules that name it, in APPLICATION order (category rules before the usage rule). SR1 is asked of every alternative before the row, through `ItemCategory.IsIndexMessageTagObjectOrPointer` (§13.18.60.3 SR4's phrase reader, which answers for the usages that never gain a `PicInfo`) and `IntrinsicArgumentRules.ClassOf` (the §8.5.2.1 Table-2 classifier, which answers for the operand shapes that are not a data-item reference) — no third class list. class-name-1 and alphabet-name-1 are DISTINCT kinds because SR4 names the first and not the second.
- **Diagnostics.** COBOLNET2200 = SR1's class/variable-length-group arms (its strongly-typed-group arm keeps COBOLNET1533, the strong-typing family being split by rule); COBOLNET2201 = SR5; COBOLNET2202 = SR3; COBOLNET0844 keeps SR4 and SR8, which it already reported. A user-defined word that names neither an alphabet-name nor a class-name is COBOLNET1639 (§8.4.2.1) at BIND — it used to compile clean and abort at run time.
- **All fourteen are offered** (kb/Work PB225 added the seven COBOL-2014 numeric-content alternatives — seven rows, two new rules, SR6 `NumericCategory` → COBOLNET2216 and SR7 `StandardFloatUsage` → COBOLNET2215). Their seven words are reserved from 2014, so they sit after `cobolWord` exactly as BOOLEAN does and are class-names below 2014. FLOAT-NOT-A-NUMBER-QUIET gained its lexer token with them (its only surface).
- **The operand the screen and the renderer ask about is the OPERAND, not its base item.** CATEGORY comes from THE ONE operand-category reader (`IntrinsicResultType.OperandCategory`), so a reference-modified slice is category alphanumeric / national (§8.4.3.3.4 GR6 c) with its item's USAGE (GR6's opening sentence): `IF NUM (1:2) IS ALPHABETIC` is not an SR4 violation, `IF NUM (1:2) IS NEAREST-TO-ZERO` IS an SR6 one, and `IF S9 (1:4) IS NUMERIC` takes GR3 n) 2.'s all-digits test (kb/Work PB823 — it had asked the item's picture and admitted the over-punched sign). `ClassConditionModel.Violates(rule, category, usage)` fails OPEN on a null for the negative rules (SR3/4/5/8) and treats a known non-matching category as a violation for the positive ones (SR6/SR7).
- **The truth values.** GR3 g)/m) FARTHEST-FROM-ZERO / NEAREST-TO-ZERO read `AlgebraicRanges` — the extremes the HIGHEST-/SMALLEST-ALGEBRAIC intrinsics and SET Format 15 already share (Annex D.32) — compared through the one relation renderer for a fixed-point item; ⚠ DETERMINATION: "whether that value is positive or negative" admits EITHER direction's extreme (it matters only for a two's-complement container, §13.18.60.4 GR12). §8.8.4.4.4 GR3 h)–k) and every float question (g/m over a float, l's finiteness, n) 1. b.'s "finite numeric value" for a STANDARD float usage) are decided by `CobolFloatClass` on the carrier's RAW ISO/IEC 60559 bits — the typed field, or a window image's bits via `CobolNum.ImageFloatBits` — never a widened value (a binary32→binary64 widening quiets a signaling NaN). GR3 l) IN-ARITHMETIC-RANGE compares the item's `AlgebraicRanges` against `ArithmeticModes.IntermediateExtremes` exactly; every declarable description is contained today, so it reduces to "a numeric value" (finite for a float), and an uncontained one renders LOUD rather than guessed.
- **Which CHARACTER CLASSIFICATION** an ALPHABETIC test uses (§12.3.6.4 GR5 a)–e) alphanumeric vs f)–j) national; GR7 b)) is `ObjectComputerEmit.ClassificationArg` — the ONE selector the UPPER-CASE / LOWER-CASE consumer (GR7 a) also calls (kb/Work PB760).

**Pinned by** `ClassConditionTableDriftTests` (every `classConditionName` keyword alternative has a model row and a renderer arm), `CobolFloatClassTests` (every SET Format 15 encoding classifies as what it was written as; the float `AlgebraicRanges` extremes are the bit patterns GR3 g)/m) test), goldens `85/pb571_class_condition_one_table`, `2002/pb590_boolean_class_condition`, `2014/pb225_float_class_conditions`, `2002/pb225_float_class_words_are_user_words`, `85/pb823_refmod_class_numeric`, `2002/pb760_classification_national_group`, and negatives `pb571-class-condition-index-operand`, `pb590-boolean-class-condition-below-2002`, `pb590-boolean-class-numeric-operand`, `pb590-boolean-class-usage-bit`, `pb225-float-class-not-standard-float`, `pb225-numeric-content-class-not-numeric`, `pb225-float-class-below-2014`.

### D8b. ⛔ A relation's COMPARISON CLASS is decided ONCE, from BOTH operands, by `ConditionRenderer.RelationCategories` → `CollatingSelection.ForComparison`.

**Why it is a decision and not an implementation detail.** kb/Work PB649: the figurative branch of the relation renderer derived the pair's category from the non-figurative anchor alone and then handed that answer back to itself as the figurative's own category, so the two-operand rule was asked a two-operand question with one operand's answer twice. Under `ALPHABET AL IS "ZYX…A"` as the program collating sequence, `IF ALL N"AB" < XA` answered 0 while `IF NB < XA` over the identical values answered 1 — the same comparison by §8.8.4.2.6, opposite answers. `RelationCategories` gives a CATEGORY-LESS figurative WORD its context's category (§8.3.3.6.4 GR1) and never overrides an `ALL literal-1`, which carries its literal's own class (§8.3.3.6.3 SR2); both relation legs then read the one answer, and `EmitCore.CollateArgFor` asks `ForComparison` over the same pair, so the collating sequence and the comparison class cannot disagree. The level-88 membership site passes its variable's category twice ON PURPOSE and says so: §13.18.63.3 SR2/SR4/SR5/SR10 make a level-88 VALUE literal the variable's own category, so there the pair genuinely is one category twice.

### D9. EC checking is OFF by default; conditional phrases (ON SIZE ERROR/AT END/INVALID KEY/ON OVERFLOW/ON EXCEPTION) are ALWAYS active when written and do NOT require >>TURN.

**Rationale.** ISO §14.6.13.1.1/§5000: default is EC-ALL CHECKING OFF. ISO §14.6.13.1.4 GR1: an explicit conditional phrase handles the condition regardless of TURN state. The phrases are the COBOL-85/2002 handler form the NIST corpus uses; >>TURN/EC-name declaratives are the secondary 2002+ mechanism (edition-gated; diagnosed at --std=85 — see Per-edition gating). Result: programs that don't use exceptions emit zero scaffolding (commercial-quality fast path).

**Rejected alternatives.** Always-on EC checking — huge per-statement runtime cost (ISO NOTE warns of significant penalty) and non-ISO default. Require >>TURN for the classic phrases — breaks COBOL-85 ON SIZE ERROR / AT END semantics.

### D10. >>TURN is resolved at COMPILE time by a TurnState that walks the procedure division in source order; it decides WHETHER the emitter emits an EC guard at all for each statement.

**Rationale.** ISO §4970/§5018: TURN enables checking for the source text that follows in the compilation group. EC-ALL expands to all level-3 names; a level-2 name expands to its children (§5002-5004); EC-I-O-WARNING only toggles explicitly (§5006). Compile-time resolution means OFF compiles to nothing — the key C#-native win.

**Rejected alternatives.** A runtime per-EC enabled-flags table consulted at every statement — defeats the zero-overhead property and adds branches where none are needed.

### D11. USE…EXCEPTION/ERROR declaratives compile to paragraph-methods plus a compile-time declarative registry keyed (EC / file / open-mode); the dispatch call is injected at the operation site and the declarative method RETURNS a ResumeAction enum {Default, NextStatement, Procedure(name)}.

**Rationale.** ISO §9.1.12 'first one in the list that matches' (file-specific > open-mode > exception-name) and §14.6.13.1.4 GR3 (declarative runs when no explicit phrase handled it). Returning ResumeAction lets RESUME (§14.9.33) redirect control: NextStatement falls through past the offending statement; Procedure does a goto (as if GO TO). USE GLOBAL chains to the parent program's registry.

**Rejected alternatives.** A single program-wide try/catch that re-dispatches — loses the precise 'resume after the statement' semantics and the applicable-statement selection; harder to debug.

**The USE duplicate-operand screens are ONE mechanism (kb/Work PB364).** §14.9.49.3 SR7 (an open-mode phrase), SR8 (a file-name), SR9 (a report group) and SR14 (an (exception-name-2, file-name-2) pair) are four instances of a single sentence — *"the same X shall not be … in more than one USE statement within the same procedure division"* — so `ProcedureTableBuilder` enforces all four through one `ConstructOperandRegister<TKey>` rather than four hand-rolled sets. **The boundary those rules draw is the STATEMENT, never the operand:** §14.9.49.2 writes Format 1's list as `{ file-name-1 } …` and Format 3's scope as `exception-name-2 { FILE file-name-2 } … …`, so ONE statement may write the same operand twice and one statement is never *more than one*. The register therefore keys each operand to the ORDINAL of the USE statement that most recently registered it and bumps the counter at `DeclEndUseStatement`, called from the ONE place a USE statement is bound: the same ordinal means this statement has already accounted for the operand, an earlier one is the repeat across statements the rule forbids. Because it remembers the LAST statement rather than the first, a violating statement that ALSO repeats the operand internally (`… ON TF TF` after an earlier `… ON TF`) reports the rule ONCE. **The register is not USE-specific and does not live here:** it is `Binding/ConstructOperandRegister.cs`, and §12.4.5.7.3 SR8's COLLATING SEQUENCE screen in `DataBinder.ResolveFileCollating` is its second consumer with the CLAUSE as its construct (kb/Work PB703); the type's own doc comment carries the consumer list. A repeat inside the statement binds ONCE and is silent (it must bind once — the emitted F1 `switch (__f)` would otherwise carry two identical case labels), a repeat across statements is the diagnostic. **SR14's key is the PAIR:** a bare exception-name is exception-name-**1**, not exception-name-2 (the Format-3 figure puts exception-name-2 only in the FILE alternative and §14.9.49.4 GR3 c)/d) against e)/f)/g) split on the same axis), so a repeated bare name never enters the register — GR3 gives it its outcome (*"The first declarative that satisfies the selection criteria is executed and no other declaratives are executed"*) instead of forbidding it.

**The dispatch site of a NONFATAL condition raised INSIDE THE RUNTIME is the runtime (kb/Work PB367b).** D11's
"injected at the operation site" holds wherever the emitter can see the raise — a RAISE statement, an I-O status,
a latched ON OVERFLOW-less STRING, ALLOCATE/FREE, CONTINUE AFTER, SEARCH's range conditions. Four conditions have
no such site: an untranslatable code unit inside an arbitrary expression (EC-DATA-CONVERSION, §15.19.4 r3), a
dynamic-capacity table grown under a receiving subscript (EC-BOUND-OVERFLOW, §8.5.1.9.6 GR1), an inverted THROUGH
range inside a condition (EC-RANGE-INVALID, §14.7.8 rule 2) and a dynamic-length resize (EC-STORAGE-NOT-AVAIL,
§14.9.39 Format 16 GR37/GR38). They are detected by `ExceptionEngine`'s own raise helpers, which is why the
§14.6.13.1.1 rule already lives there per (ambient flag, exception-name) pair — and why the §14.6.13.1.4 #3
selection now lives there too: `NonfatalIfEnabled` sets the last exception status AND asks
`ExceptionEngine.NonfatalDispatcher` — the `ICobolProgram` of the ACTIVATION now executing, installed and
restored by `ProgramTable.RunMain`/`CallProgram` beside the ModuleStack frame — for its declarative. The
interface member (`int NonfatalDispatch(string ec)`) defaults to the dispatch protocol's "no qualifying
declarative", so a program with no Format-3 machinery overrides nothing and the zero-scaffolding invariant is
untouched; a program with it emits one expression-bodied override onto its existing `EcDispatchExpr` funnel, so
§14.6.13.1.4 #2's "No associated USE EXCEPTION declarative is executed" (a PERFORM WHEN preempts) keeps holding
through `__EcPerform`.

*The return.* `-3`/`-1` return to the raise point and the statement finishes under its own rules — §14.6.13.1.4
#3's "execution continues as specified in the rules for normal execution", and each of the four conditions has a
rule that names that continuation outright (the substitution character is used, the capacity change happens, the
range is empty, the length is 0 or clamped). §14.9.49.4 GR13 a)'s "control is returned to an implicit CONTINUE
statement following the statement" is the alternative for a condition whose statement rules name none, where the
raise is at the end of the statement's own execution and the two readings coincide. A RESUME is different — it is
an explicit transfer — and it unwinds through `RaiseResumeSignal`, a type DISTINCT from `ResumeSignal` because the
two have different landing sites: `ResumeSignal` travels from a RESUME statement to `__RunUse`, `RaiseResumeSignal`
from the raise site to the emitted nonfatal-gate wrapper. Sharing one type made the nearer landing site swallow the
farther signal.

*The SORT/MERGE arm.* A USE procedure invoked from a SORT/MERGE **implicit** transfer (§14.9.40.4 GR12/GR15,
§14.9.24.4 GR7/GR12) is not attached to a statement the program wrote, so §14.9.33.4 GR2 a) 1.'s "applicable
statement" is the SORT/MERGE itself and §14.9.40.4 GR17 states SORT's half outright: "If a USE procedure invoked
while a format 1 SORT statement is active does not complete normally, the SORT statement is terminated."
`SequentialIoEmitter.EmitUseHook` takes the enclosing statement's end label for exactly those call sites and jumps
to it on the RESUME-AT-NEXT-STATEMENT action (§14.6.13.1.2 #1 is what makes a RESUME a completion that is not
normal); the RESUME-AT-procedure-name action already leaves through `__pc`, and every other not-normal completion
(GOBACK / EXIT PROGRAM / STOP / a fatal condition) unwinds by its own signal. The label sits inside the statement body whose `finally` releases the sort store, so a terminated SORT statement is not a leaked sort. The same label is where a procedure that DID
complete normally lands when the verb's own rule terminates the statement: §14.6.13.1.3 2) ("If the executed
statement is a MERGE or SORT statement, then the rules for those statements apply") precedes 5)/7), so these
sites call `__IoCheckEc(…, __verbRule: true)`, which skips the fatal throw, and dispose of the status from
`SortEmitter.RuleFor` (kb/Work PB993; `COBOLNET_FILES_DESIGN.md`, the implicit-transfer section). The conditions the statement raises itself take the same rule from the statement guard: a SORT/MERGE
statement asks the EC-SORT-MERGE names it can raise (and the EC-I-O family per USING/GIVING file, which is what arms
the hooks above), `CobolSort`'s statement entries MARK what they raise (`CobolFatalException.RaisedBySortMerge`, set
by an exception filter that declines the catch), and `EcEmitter.VerbDisposes` makes the SORT/MERGE catch run the
declarative and then fall out of the statement for a marked condition instead of re-throwing — the store is released
by the statement body's `finally`. The mark, not the name, decides, because 2) keys on the statement that executed: a
RELEASE or RETURN statement's EC-SORT-MERGE-ACTIVE, and anything a procedure statement raises, keep 5)/7)
(kb/Work PB1036; `docs/CONFORMANCE.md` §3 D-SMA).

**The re-entrancy guard IS the EC-FLOW-USE raise site (kb/Work PB368).** §14.9.49.4 GR2 (ALL FORMATS) is one
sentence and its whole normative content is a RAISE: *"During the execution of a USE procedure, if a statement
raises an exception condition that would cause the execution of a USE procedure that had previously been activated
and had not yet returned control to the activating entity, the EC-FLOW-USE exception condition is set to exist."*
It says nothing about the declining invocation, so the generated `__RunUse` guard (`if (__useActive[__id]) …`) is
the right consequence and was for a long time the ONLY one — EC-FLOW-USE existed nowhere but its
`ExceptionCatalog` row, so a program could not detect its own declarative recursion and the §14.6.13.1.6 Table 13
Fatal default could never fire. The raise is written in `DispatchEmitter.EmitRunUseBody`, the ONE body every
selection path reaches the active procedure through (`__IoCheck`, `__IoCheckEc`, `__EcDispatch`,
`__EcObjDispatch`, the container's `__RunGlobalUse`, and the report engine's BEFORE REPORTING hook — a per-path
copy is exactly how an arm comes to disagree), and it calls `ExceptionState.FlowUseError`, the same
`FatalIfEnabled(flag, name, detail)` one-liner every other fatal condition uses. With checking enabled the fatal
condition unwinds to the RAISING statement's own guard for the §14.6.13.1.3 #5/#7 dispatch (a USE AFTER EXCEPTION
CONDITION EC-FLOW-USE declarative and its RESUME, else abnormal termination); with checking off nothing is raised
(§14.6.13.1.1) and the quiet decline stands as the §14.6.13.1.3 #8 implementor answer.

*Two things this raise gets right by construction.* (a) **It is AMBIENT, per statement.** GR2's subject is *a
statement* that raises a condition selecting an active USE procedure, and the statements that can do that are
every statement that can reach a declarative at all — an I-O verb under the GR3 a)/b) tiers, an RWCS verb under
GR8, a RAISE, a CALL, or any statement whose inline raise site (subscript, ref-mod, pointer, size error) reaches
the GR3 c)–g) tiers. There is no node kind to key on, so `EcBinder.EcWrap` adds EC-FLOW-USE for any statement in a
checking-on region, exactly as for EC-BOUND-REF-MOD; the SYNTACTIC filter "a statement inside DECLARATIVES" is
wrong outright, because a declarative may PERFORM a paragraph anywhere and GR2 says *during the execution of* a
USE procedure. (b) **Only a DECLARATIVE id is a USE procedure.** The exception-checking PERFORM's imp-2/3/4
handler ranges share `__RunUse` and its `__useActive` array but are selected by §14.9.28.4 GR17, not §14.9.49.4
GR3 — GR2 does not reach them — so the emitted guard raises only below `DispatchState.DeclCount` (an OO method's
method-local `__RunUse` numbers the METHOD's own declaratives first, then the class's handler slots — the same
split, so a method with no declaratives emits the bare guard unchanged).

*METHOD-SCOPED SELECTION (kb/Work PB1010).* §14.2.2 SR10 admits the declaratives format in a method definition, and
§14.9.49.4 GR3/GR4 a) select over "the USE statements in the source element" containing the raising statement — the
METHOD, which is contained in no source element with a procedure division of its own (§14.2.2 SR12/SR13), so no
GR4 b) GLOBAL walk exists for it. Binding: `StatementBinder.BindMethodRoster` collects each method's declarative
sections through `ProcedureTableBuilder.CollectMethodDeclaratives` — the SAME `DeclCollectSection` a program's take,
with the SR7/SR8/SR9/SR14 registers re-made per method procedure division and the sections declared in the method
scope — at the head of the method's pc slice (`OoMethodBinding.DeclStartPc .. EntryPc−1`, §14.2.3 GR1), onto
`OoMethodBinding.Declaratives`. Emission: `OoEmitter.EmitMethod` points the per-unit selection state
(`UnitHasF3`/`UnitHasF4`/`UnitHasF3Perform`, `UseDecls`, `DeclCount`, `F3HandlerBasePc`) at the method for its
body and calls the ONE `DispatchEmitter.EmitUseMachinery` with `asLocal` — `__useActive`, `__RunUse`, `__IoCheck`,
`__IoCheckEc`, `__EcDispatch`, `__EcObjDispatch`, `__EcPerform`, `__RunF3` become LOCAL FUNCTIONS that capture the
method's data and shadow any class-level funnel, so every raise site's unchanged call reaches the method's own
selection. A RUNTIME-site nonfatal raise reaches it through the activation's `INonfatalSelector`: the method installs
a `NonfatalSelectorFn` over its selection (or `NonfatalSelectorFn.None` when it has none — so a method's raise can
no longer select its INVOKER's declaratives) and restores the invoker's in the activation's `finally`, the twin of
`ProgramTable`'s install. An OBJECT/FACTORY file's key is a per-instance field, so `UseTierEmitter` labels it with
a case guard rather than a constant. Every arm is pinned by
`FlowUseReentrancyTests` plus `conformance:2002/pb368_flow_use_reentrancy`.

*The MULTI-OPERAND arm — the per-implicit-statement boundary (kb/Work PB419).* §14.9.33.4 GR2 a) qualifies its
own answer: the implicit CONTINUE follows the end of the statement that was executing "unless general rules
associated with the applicable statement specify otherwise". SEVEN statements specify otherwise, in identical
words — CLOSE (§14.9.6.4 GR10), FREE (§14.9.15.4 GR2), INITIALIZE (§14.9.20.4 GR3), INITIATE (§14.9.21.4 GR5),
OPEN (§14.9.27.4 GR20), TERMINATE (§14.9.46.4 GR4) and VALIDATE (§14.9.50.4 GR3): a multi-operand statement IS a
separate statement per operand in source order, and "processing resumes at the next implicit … statement, if
any". The resume protocol addresses STATEMENT SITES (the `-2` action falls out of the statement's own guard), so
the boundary exists exactly where each operand owns a site. `BoundImplicitSeries` (Binding/Bound/BoundTree.cs) IS
that and nothing more: each verb binder builds ONE bound node per operand, `EcBinder.EcWrap` DISTRIBUTES the
`BoundEcChecked` wrapper over the members (one written statement ⇒ one `EcStatementInfo`, one >>TURN scope, one
Table-12 name, one §15.30.3 r2 location), and `StatementEmitter` renders the members consecutively. `Of` returns
the bare member for one operand — every one of the seven rules is conditioned on "more than one" — and with
checking OFF no wrapper is built, so the emitted text is byte-identical to the pre-series flat run (the
zero-scaffolding invariant, §18.16). A bind-time desugar (the UDF activation hoist, the OO property pre-op
triple) goes through `BoundImplicitSeries.Rewrap`, which lands the hoist on the FIRST implicit statement rather
than burying the series in a `BoundSequence` where `EcWrap` could no longer see it.

The verbs whose raise reaches the declarative through a PER-OPERAND inline dispatch already had the boundary and
keep it — OPEN/CLOSE through `EmitUseHook`'s `__IoCheck`/`__IoCheckEc` call at each file, FREE through its
per-operand EC-STORAGE-NOT-ALLOC block: a `-2` there falls into the next operand, which is the next implicit
statement. What the series adds for them is the same boundary for a condition that UNWINDS to the statement
guard. INITIALIZE, INITIATE and TERMINATE had NO per-operand dispatch at all, and that is where the defect was
measured: `INITIALIZE EN (BADX) A1 A2` with a declarative resuming NEXT STATEMENT left `A1` and `A2` at their
declared values — a silent wrong answer (`tests/conformance/2002/pb419_initialize_resume_boundary.cob` pins all
four legs; `ImplicitStatementSeriesDriftTests` re-derives the verb census FROM the spec so an eighth verb, or an
implementation of the declined VALIDATE facility, cannot join without joining the mechanism).

VALIDATE is the one member with no binder: Annex A.4.14 is owner-declined (docs/CONFORMANCE.md §4 item 3), so the
grammar recognizes `validateFacilityStatement` only to name the refusal. The drift test asserts that premise
rather than assuming it.

### D12. The exception-checking PERFORM (ISO §14.9.28 Format 3, COBOL-2023 — VCR row 79; introduction-gated at --std=85|2002|2014 with COBOLNET0900) is a PER-STATEMENT exception interceptor scoped to imperative-statement-1 — NOT a block C# try/catch. FULLY IMPLEMENTED (recognize/validate/diagnose/gate + the pc-RANGE runtime interceptor — the F3 PERFORM compiles and runs); a few sub-forms remain staged (cross-CALL "in range", `>>PROPAGATE`, exception-object raise in imp-1). The open-mode WHEN operand and F3-in-a-method are NOT among them, and no COBOLNET0899 is raised on this path at all: the open-mode form is the GR3b tier-1 arm at the raise site (decision (c) below — measured 2026-09-21: a READ past end-of-file inside `PERFORM … WHEN EXCEPTION INPUT` runs the handler), and the method case is implemented per design SSOT §9.10. kb/Work PB595 found the stale binder comment that said otherwise. EC-FLOW-USE is NO LONGER among them (kb/Work PB368): §14.9.49.4 GR2 raises through the ONE `EcDispatchExpr` funnel, which is `__EcPerform` in an F3 unit, so the condition is offered to the active frames (GR17) before the USE declaratives like every other name. The as-built implementation SSOT is `docs/rearchitecture/evidence/PHASE-13-c5-perform-format3-DESIGN.md` §9.

**Grammar (greenfield, `CobolControlFlow.g4`).** Formats 2 and 3 merge into ONE inline `performStatement` alternative (`PERFORM performInlineHead? statementBlock* performWhenPhrase* performWhenOther? performWhenCommon? performFinally? END-PERFORM`); ≥1 ordinary WHEN ⇒ Format 3 (enforced at bind, COBOLNET1597). A WHEN operand list's CONTINUATION is bounded by the `whenOperandAhead()` predicate (`CobolParserCoreBase.WhenOperandStopTokens`) so a body verb that is also a `cobolWord` (RESUME/RAISE/VALIDATE/UNLOCK/SEND/RECEIVE/COMMIT/ROLLBACK/ENTER, + GET/PARSE forward) is not annexed as a spurious exception-name; the merged inline arm precedes the out-of-line `PERFORM procedureName` so `PERFORM LOCATION imp… END-PERFORM` disambiguates on END-PERFORM. `LOCATION`/`FINALLY` are new-2023 reserved tokens: LOCATION stays a `cobolWord` (the continuity invariant — a paragraph named LOCATION / `PERFORM LOCATION` parses below 2023; it appears only in the head, so no operand-swallow); FINALLY is a pure reserved keyword (NOT a `cobolWord`) — as a trailing phrase keyword after imperative statements it would be swallowed by a preceding DISPLAY/MOVE operand list, so it is treated as reserved at every edition (a documented, negligible deviation — FINALLY was never a COBOL identifier idiom).

**Binder (`EcBinder.ExceptionPerform.cs` → `BoundExceptionPerform`).** Resolves each WHEN's operands (exception-name at ANY level per the USE GR3a–3g tiers — not the RAISE level-3-only rule; SR16 EC-I-O prefix; the per-name edition window), enforces the §14.9.28.3 syntax rules and the cross-statement bans by lexical region (A = imp-1, B = whole PERFORM, C = WHEN phrases, D = imp-2..5) via parse-subtree walks: COBOLNET1597 (≥1 WHEN), 1599/1600/1601 (SR14/15/16), 1604 (EXIT PERFORM CYCLE), 1605/1606/1607 (INITIATE/TERMINATE/VALIDATE >1), 1608 (GO TO in a WHEN), 1610 (RESUME AT proc in a WHEN), 1611 (RAISE outside imp-1), 1612/1614/1615/1616/1617 (multi-CLOSE / dup-INITIALIZE / MERGE / dup-OPEN / SORT in imp-1). RESUME's SR1 relaxes inside a WHEN (`EcBindState.InF3When`): RESUME NEXT STATEMENT is legal there, RESUME AT proc is COBOLNET1610. GR14 is a bind-time overlay on `TurnState` (`WithImplicitEnable` — a synthetic line-0 enable per WHEN-named EC over imp-1, WITH LOCATION iff the PERFORM specifies LOCATION; a real >>TURN OFF inside imp-1 overrides it). At the end of imp-1 GR14 assumes an implicit PUSH ALL followed by TURN OFF ALL, so imp-2..5 (the WHEN / OTHER / COMMON handler bodies and the FINALLY block) bind with ALL exception checking OFF — no ambient >>TURN state is visible to them, not even checking that was enabled before the PERFORM. Immediately preceding END-PERFORM an implicit POP ALL restores the pre-imp-1 state and issues an implicit TURN … OFF for any exception that was implicitly enabled over imp-1; GR22 then governs what checking carries past the PERFORM (a pre-PERFORM enable that fired stays enabled, a >>TURN within the range is retained, otherwise the WHEN-named ECs are not enabled after the statement).

**Runtime — the pc-RANGE interceptor (IMPLEMENTED; SSOT `PHASE-13-c5-perform-format3-DESIGN.md` §9).** A per-statement interceptor is REQUIRED (not a block try/catch): a block catch unwinds past the remaining imp-1 statements and cannot deliver §14.9.28.4 GR20's nonfatal resume-in-place. As-built: imp-1 emits INLINE inside a `try`; a raise site within it consults an ambient `PerformFrame` stack (`ExceptionEngine`, run-unit-scoped) BEFORE the USE declaratives (GR17 — a matching WHEN ignores USE), via the funnel `EcDispatchExpr` → `__EcPerform` (→ `RunTopFrame`, a top-down walk with a deferred `Handling`-clear that keeps a skipped inner frame transparent while a selected outer handler runs, GR21). The WHEN/OTHER/COMMON handler bodies (imp-2/3/4) are emitted as **synthetic UNREFERENCEABLE pc-range paragraphs** appended above the main pc space (the fall-through walled off at `F3HandlerBasePc − 1`) and run via the reused `__RunUse(id, pc, pc)` — so RESUME reuses `ResumeSignal`→`__RunUse`→`-2` verbatim. The frame's matcher is a closure that does **tier-ordered** WHEN selection (GR17 → §14.9.49.4 GR3c-g: file+L3 → file+L2/bare-file → L3 → L2 → L1/EC-ALL, source order only within a tier — mirrors `__EcDispatch`) and, on match, invokes `__RunF3` (imp-2 then WHEN COMMON imp-4). FINALLY (imp-5) is the INLINE trailing block. **EXIT PERFORM** is region-aware (`DispatchState.F3Cur`): imp-1 → `goto __f3fin`, a handler pc-range → `throw ExitPerformSignal(Id)` (caught at the PERFORM boundary — a handler runs in a nested `__Dispatch` a `goto` cannot leave, the reason the rejected lambda-matcher-body approach could not implement it), FINALLY → `goto __f3end`; a nested inline PERFORM saves/restores `F3Cur=None` so its own EXIT PERFORM breaks the inner loop (§14.9.14.4 GR5a). The fatal/nonfatal split (GR20) is realized by each raise site's existing throw idiom + the `-1/-2` protocol — the matcher carries no `fatal`. Every emission is gated on `EcState.UnitHasF3Perform` so a non-F3 unit is byte-identical.

**Decisions recorded (design panel + §9.6):** (a) **RESUME NEXT STATEMENT in a WHEN SKIPS WHEN COMMON** — GR17 hands to imp-4 "at the completion of imp-2", and a RESUME is a transfer OUT (not a completion), so `__RunF3` runs COMMON only on the handler's `-1` (chosen interpretation; the standard is silent). (b) **FINALLY does NOT run on the fatal abnormal-termination path** (normal / EXIT-path only) — a genuine STANDARD DEFECT (§14.9.28.4 NOTE 8, "includes the statements in a FINALLY phrase, if it is specified", vs GR20's fatal branch → §14.6.13.1.3 abnormal termination, which never re-enters the end of the PERFORM; the two cannot both hold). Realized because a `CobolFatalException` unwinds past the inline FINALLY block. (c) **the WHEN operand match follows the full §14.9.49.4 GR3a→g priority**: bare file-name (GR3a) > open-mode `WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND` (GR3b, matched at the raise site by `CobolFile.OpenModeOf(__f)`) > file+L3 (GR3c) > file+L2 (GR3d) > L3 (GR3e) > L2 (GR3f) > L1/EC-ALL (GR3g) — file/mode scope OUTRANKS an exception-name, source order only within a tier. The open-mode form is implemented (an OPEN-failure's mode is best-effort — the connector reports its mode only once open).

**Not emitted (spec-fidelity):** COBOLNET1598 ("operand-form exclusivity") — no §14.9.28.3 SR backs it (SR14/15/16 are the only WHEN-operand rules, and the figure's `{exception-name-1 | exception-name-2 FILE file-name-2}…` reading permits interleaving), so it is not enforced. XS-RESUME-PLACEMENT is subsumed by COBOLNET0712 (RESUME may appear only in a declarative or a WHEN phrase). XS-POP/XS-PUSH (COBOLNET1602/1603) stay reserved as CODES and are never reallocated, but the bans themselves are now ENFORCED: §7.3.22.3 SR4 (PUSH), §7.3.20.3 SR4 (POP) and §7.3.25.3 SR5 (TURN) are three rules of one shape decided by ONE lexical-containment predicate (`EcBinder.CheckDirectiveBans`) over the directive SITES the frontend records on the final text (`DirectiveSiteProcessor`), reported as the suppressible warning COBOLNET2187 per owner decision D20 — a FLAT ban with the program still compiling (§4.2.2). That is independent of the >>PUSH / >>POP directive-state SEMANTICS, which remain unimplemented (kb/Work PB595).

**Rejected alternatives.** Model every EC via C# exceptions/try-catch globally — exceptions are for RESUME's declarative unwind (`ResumeSignal`); the common inline phrases and declaratives use status-flag/branch control flow for correctness (resume-after-statement) and zero default cost.

## C# mapping

IF: `IF c [THEN] s1 [ELSE s2] END-IF` → `if (<RenderCondition(c)>) { s1 } else { s2 }`. CONTINUE→empty block. NEXT SENTENCE→lower the sentence as a labeled block + `goto <after_sentence>;` (COBOLNET0701). Nested IF: each branch is fully braced so C# dangling-else is structurally impossible.

RELATIONAL: numeric (both operands numeric) → render as scaled longs, align to larger scale via existing NumX/Align, then `(<l> <op> <r>)` (exact, no truncation). Alphanumeric (either side non-numeric) → `(CobolString.Compare(a,b,weights?) <op> 0)` with space-extension of the shorter operand (ISO §8.8.4.2.7 rule 2). Pointer (= / NOT = only) → `ReferenceEquals(p,q)` / `p is null`. Figurative ZERO vs numeric → numeric 0. Literal-vs-literal constant-folds to true/false. Operator mapping via existing MapOperator (all symbolic + word + NOT-prefixed forms; needs an ~18-form unit-test matrix).

SIGN: `op IS [NOT] POSITIVE|NEGATIVE|ZERO` → `(<num> > 0)` / `(<num> < 0)` / `(<num> == 0)`, NOT wraps in !(…). (NOT POSITIVE = ≤0, includes zero — the !(…) handles it.)

CLASS: `IF X IS NUMERIC` → `if (CobolClass.IsNumeric(X))`; `IS ALPHABETIC`→`CobolClass.IsAlphabetic(X)` ({A-Z,a-z,space} closed set). Numeric long item: folds to `if (true)` (COBOLNET0706). User CLASS HEX → `CobolClass.IsUserClass(X, "0123456789ABCDEF")` (THRU ranges expanded). NOT wraps in !(…).
New runtime: `static class CobolClass { bool IsNumeric(string); bool IsNumericDisplay(string,NumProfile); bool IsAlphabetic(string); bool IsAlphabeticLower(string); bool IsAlphabeticUpper(string); bool IsUserClass(string, ReadOnlySpan<char>); }` — ported verbatim from PicRuntime.IsNumericClass/IsAlphabeticClass (legacy 2379–2464) but over UTF-16 chars, sign-aware (overpunch {,A-I,},J-R / separate +,-).

LOGICAL: AND→`(a && b)`, OR→`(a || b)`, XOR→`(a ^ b)`, NOT→`(!(p))` — all fully parenthesized, short-circuiting.

ABBREVIATED COMBINED (ISO §8.8.4.12): walk keeping a current subject+operator from the last full relation; `op operand`→expand to `subject op operand`; bare `operand`→`subject currentOp operand`; leading NOT negates that relation only. Example `IF A = B OR C OR > D` → `((A==B) || (A==C) || (A>D))`.
**Grammar: one tail at every tier (kb/Work PB1390).** §8.8.4.12.1 lets "any relation condition except the first" be abbreviated and §8.8.4.12.2 repeats ONE group — {AND | OR | EXCLUSIVE-OR | XOR} {NOT | relational operator} object-1 — with any connective in any position. So the condition tiers come in two spellings that differ in the leading element only: the LEADING tiers `logicalOrExpression` / `logicalXorExpression` / `logicalAndExpression` hold the first simple condition, and the operand of EVERY connective is a SUCCEEDING tier (`succeedingXorExpression` / `succeedingAndExpression` / `succeedingCondition`, the last being `abbreviatedRelation | unaryLogicalExpression`). The old grammar spelled the abbreviated tail three different ways (an OR-only `abbreviatedAndChain`, an AND-only arm, nothing after XOR), so `A = B OR < C AND D`, `A = B XOR < C` and `A = B OR < C XOR = D` were COBOL0001. The binder folds leading and succeeding tiers with the one `BindFlatSequence`, threading `AbbrevCarry` in source order — GR1's insertion, and its termination at a complete simple condition (`A = B OR < D AND C = 1`). The exclusive-or connective is the one `xorOperator` rule every tier (condition, partial expression, constant conditional expression) spells it through; `PartialExpressionSpineDriftTests` derives the succeeding tiers' tails from the leading tiers and `ConditionTierConnectiveDriftTests` pins the `xorOperator` spelling.

LEVEL-88: 
```
01 WS-STATE PIC 9.   88 ACTIVE VALUE 1.   88 PENDING VALUE 2 THRU 4.   88 DONE VALUE 5 9 WHEN SET TO FALSE 0.
```
→
```
private static long WS_STATE = 0L;
private static bool ACTIVE  => WS_STATE == 1L;
private static bool PENDING => WS_STATE >= 2L && WS_STATE <= 4L;
private static bool DONE    => WS_STATE == 5L || WS_STATE == 9L;
```
Single value→`==`; THRU→`>= from && <= to`; multiple→OR; alpha values space-extended to parent width, ALL literal repeated to width. Subscripted cond-name→method `bool COND(long i)=>parent[i-1]==v;` (tables are supported — OCCURS→T[]). SET ACTIVE TO TRUE→`WS_STATE = CobolNum.Store(1L,0,_P_WS_STATE);`. SET DONE TO FALSE→`WS_STATE = CobolNum.Store(0L,0,_P_WS_STATE);`.
DataItem gains `List<ConditionName> ConditionNames`; `record ConditionName(string CobolName, string CsName, IReadOnlyList<CondValue> TrueValues, CondValue? FalseValue)`; `readonly record struct CondValue(string FromLiteral, string? ThruLiteral, bool IsAll)`.

EVALUATE: hoist subjects → chained if/else-if/else. One WHEN clause's match = OR over its WHEN phrases ( AND over ALSO subjects ( per-subject match ) ). Per-subject: ANY→true; value→`==` (scaled/collating); range v1 THRU v2→`>=v1 && <=v2`; partial-expr (item starts with relop/class/sign)→prepend the subject's ONE bound value (never a re-bind of its node; a class test reads a data item's in-place content — kb/Work PB912); TRUE/FALSE↔condition subject (condition-1 = any `condition`, §14.9.13.4 GR3 e)→the subject's ONE truth value, held in a one-position boolean intermediate (`SendingValueTemp.MaterializeTruth`) when more than one arm reads it, or its negation; group NOT→negate the group's conjunction. WHEN OTHER→final else.
Example `EVALUATE WS-DAY ALSO TRUE / WHEN 1 THRU 5 ALSO WS-OPEN … / WHEN 6 7 ALSO ANY … / WHEN OTHER …` →
```
var _e0 = WS_DAY; var _e1 = true;
if (((_e0>=1L && _e0<=5L) && (WS_OPEN==_e1))) {…} else if (((_e0==6L||_e0==7L) && true)) {…} else {…}
```

ON SIZE ERROR (ISO §14.7.5): the checked store is **`CobolNum.TryStore`** (SSOT §14.7) — one operation that stores plus runs the capacity/inexact check, leaving the receiver unchanged on overflow (returns `bool`; `false` = ON SIZE ERROR). It rounds to scale FIRST, then tests integer-part capacity; division-by-zero / exponentiation-rule → size error. On error the receiver is LEFT UNCHANGED, so stage the value and assign conditionally. Multiple receivers: OR the per-receiver flags; non-overflowing receivers ARE updated. (Signature: `bool TryStore(CobolInt value, in NumProfile receiver, CobolRounding mode, out long stored)`.)
```
bool _se=false; { var _v = new CobolInt(…, _s); if (CobolNum.TryStore(_v, _P_B, _mode, out long _r)) B = _r; else _se = true; }
if (_se) { <ON SIZE ERROR> } else { <NOT ON SIZE ERROR> }
```

AT END / INVALID KEY / ON OVERFLOW / ON EXCEPTION: status flag from the op drives a branch; NOT form = the else (success). `var _st = CobolFile.ReadShared(f,…); if (CobolStatus.IsAtEnd(_st)) {AT END} else {NOT AT END}` (the ONE governed READ entry — there is no ungoverned one, kb/Work PB683). AT END↔EC-I-O-AT-END↔status 1x; INVALID KEY↔2x; ON OVERFLOW↔EC-OVERFLOW-STRING/UNSTRING; ON EXCEPTION (CALL)↔EC-PROGRAM-NOT-FOUND.

EC RUNTIME (CobolNet.Runtime.Exceptions), as built: `ExceptionCatalog` holds ISO Table 13 as string-keyed rows (name, level, level-2 parent, fatality `EcFatality` {Fatal, Nonfatal, Imp}, introducing edition) with the level3→level2→EC-ALL hierarchy predicates (`UnderLevel2`, `DirectiveCovers`) — there is no `ExceptionCondition` enum. `ExceptionState` is the run-unit register (last name + fatality + file / I-O status / statement / location, `ExceptionObject`, the propagation slots, the per-EC checking flags of `CheckingFlags`) written through `Set` / `SetIo` / `SetObject` / `Clear`. RAISE EXCEPTION ec → emitted `ExceptionState.Set(name, fatal, …)` followed by `EcEmitter.EmitSelection` (the §14.9.49 selection, RESUME landing and fatal default); RAISE identifier → `ExceptionState.SetObject` + the generated `__EcObjDispatch`; an unhandled fatal condition → `CobolFatalException`, caught at the run-unit boundary (`ProgramTable.RunMain`) as abnormal termination. The EXCEPTION-* functions read the register through `EcFunctions` (§15.28/§15.30/§15.32/§15.33).

USE declaratives (as built — the D11 sketch's paragraph-methods and `ResumeAction` enum were not how it landed): a declarative SECTION is a pc RANGE of the one `__Dispatch`, run by the generated `__RunUse(id, startPc, endPc)`; the generated selectors (`__IoCheck` / `__IoCheckEc` for the file and open-mode tiers, `__EcDispatch` for the Format-3 GR3c–g tiers, `__EcObjDispatch` for Format 4, `__EcPerform` in front of them when an exception-checking PERFORM is active) pick the first match at COMPILE-TIME-known order — there is no runtime `ExceptionDispatch` registry — and the raise site reads the int dispatch result (see "The declarative dispatch-result protocol" under EC engine specifics).

## Hard problems

### Short-circuit (C# &&/||) is the ISO-mandated evaluation order — IF I>0 AND TABLE(I)=X must NOT evaluate TABLE(I) when I ≤ 0 (§8.8.4.13 rule 1), so the guarded subscript is never touched (no EC-BOUND, no IndexOutOfRangeException in the typed model).

ISO §8.8.4.13 rule 1 terminates a hierarchical level's evaluation as soon as its truth value is determined, so `&&`/`||` are the faithful rendering: a left operand that fixes the level's truth value skips the right. The legacy engine's eager (non-short-circuit) evaluation is the non-conforming behavior, and is corrected here. Corroborated corpus-safe by scanning tests/nist/programs: ZERO guard-then-same-variable-subscript idioms (the 44 'AND <subscripted>' cases use independent subscripts).

### Abbreviated combined conditions (IF A > B AND < C OR = D) — the subject and/or operator are elided after the first relation; the LEGACY emitter silently dropped them — the greenfield binder must expand them into full relations.

Expand at BIND time into ordinary full BoundRelational nodes (G4: the expansion is semantics, so it lives in the binder — every backend receives the already-expanded tree): maintain current-subject + current-operator from the most recent full relation; `op operand`→`subject op operand`; bare `operand`→`subject currentOp operand`; reset on each full comparison; leading NOT negates that relation only (it is part of the operator, not the subject). Ships with a dedicated test set; flagged as the single most error-prone condition feature.

### EVALUATE partial expressions (`EVALUATE X ALSO Y / WHEN > 5 ALSO "A" THRU "M"`) — a WHEN object whose LEFTMOST portion is elided is spliced onto its subject (ISO §14.9.13.3 SR5 / SR7 d) / SR8, §14.9.13.4 GR4 a) 2.).

**The rewrite is the whole implementation.** §14.9.13.3 SR5 defines partial-expression-1 by its left edge only — "the leftmost portion of the selection object is a relational operator, a class condition without the identifier, a sign condition without the identifier, or a sign condition without the arithmetic expression" — and SR8 gives it its meaning: the object "is treated as though it were specified as condition-2, where condition-2 is the conditional expression that results from preceding partial-expression-1 by the selection subject", with "the corresponding selection subject … treated as though it were specified by the word TRUE". So the pair's term IS that condition, with no comparison wrapped around it, and GR4 a) 2. evaluates it. SR7 d) ("a sequence of COBOL words such that, were it preceded by the corresponding selection subject, a conditional expression would result") is NOT a fourth check: splice the subject in, and either an ordinary condition results or the ordinary condition binder refuses it with the ordinary diagnostic.

**The splice IS §8.8.4.12's subject insertion.** `ConditionBinder.BindPartialExpression` seeds the `AbbrevCarry` — the same mutable holder that carries "the last preceding stated subject" through an abbreviated combined relation condition — with the selection subject, and the leading `partialComparison` consumes it. That is why `WHEN > 5 AND < 10` works with no code of its own: SR7 d) licenses it and §8.8.4.12.4 GR1 already knew how to read the tail.

**Grammar (`CobolExpressions.g4`): a four-rule spine that MIRRORS the condition tiers and delegates every tail to them.** `partialExpression` / `partialXorExpression` / `partialAndExpression` / `partialComparison` differ from `logicalOrExpression` / `logicalXorExpression` / `logicalAndExpression` / `comparisonExpression` in the LEADING element only; each tail is the very same sub-rule (`succeedingXorExpression`, `succeedingAndExpression`, `succeedingCondition`, joined by `OR` / `xorOperator` / `AND`), so the two spines cannot accept different operators. `PartialExpressionSpineDriftTests` EXTRACTS each condition tier's tail from the grammar and requires the partial tier's to be identical — a hard-coded expectation would rot the moment a connective is added. The rule is not folded into `comparisonExpression`: that rule is shared by every IF / PERFORM UNTIL / SEARCH, and the DEVLOG-621 regression (a `booleanExpression` alternative there broke subscripted comparisons at 2002+) is the measured cost of widening it. `evaluateWhenItem` lists `partialExpression` LAST, because `classConditionName`'s user-word alternative matches a bare word and would otherwise claim every bare identifier-2 object.

**SR6 e)** — "if the selection object is a partial expression and the selection subject is a data item of the class boolean or numeric, the selection subject is treated as an identifier" — runs in `ClassifyPair` AFTER a)–d), because it is keyed on the OBJECT being a partial expression and OVERRIDES d) for a one-boolean-character boolean data item. It is a no-op for a numeric data item, which the bare-operand classifier already calls an identifier; implementing only the boolean half of one sentence would be the two-arm shape again.

**The bare class-name spelling is decided by SYMBOL (kb/Work PB843).** A BARE user-defined class-name or alphabet-name object (`WHEN MY-CLASS`) is indistinguishable from identifier-2 in the grammar, and the same word LEADING a longer object (`WHEN MY-CLASS AND WS-F = "Y"`) is claimed by `evaluateWhenItem`'s `condition` alternative before `partialExpression` is tried. Only the resolved symbol separates them — §8.3.2.2, "a given user-defined word may be used as only one type of user-defined word", so a class-name or alphabet-name is never a data-name — which is the same doctrine that makes a bare level-88 object condition-2. `ConditionBinder.BareClassWord` is the one symbol test (a bare, unqualified word that names a `UserClasses` / alphanumeric / national alphabet entry and does not probe as a data item); `AnalyzeBareOperand` reports it as `BareOperandForm.ClassName`, and `ConditionBinder.LeadingBareClassWord` finds it at the leftmost leaf of a `condition` object. The classifier maps both to Table 15's partial-expression row, and the ONE SR8 arm binds all three parses: the `partialExpression` rule, the bare word (`BindPartialClassName`) and the re-read `condition` (whose leftmost sole-operand leaf consumes the spliced subject exactly as `partialComparison` does). The user-word class body (`BindUserWordClassCondition`) is shared with the written `IS class-name` spelling, so the §8.8.4.4.3 SR2 LOCALE refusal and the SR3/SR4 operand screens apply unchanged. The `IS`-led spelling (`WHEN IS MY-CLASS`) reaches the partial rule unambiguously.

**Editions.** No introduction gate fires: the traceability inventory records these rows 2002+, but that edge is INFERRED from the feature history — `specs/ISO_COBOL.md` holds only the 2023 text and its Annex E records no EVALUATE change — and gating on an unproven edge would REJECT LEGAL SOURCE if the inference is wrong, whereas not gating under-rejects at worst. The surveyed implementations accept the construct in every dialect. Overturnable in one line (kb/Work PB398).

### A THROUGH range's `IN alphabet-name-1` phrase names the sequence the range is evaluated in — for BOTH clauses that print it (ISO §14.7.8 rule 2; §14.9.13.2's range-expression; §13.18.63.2 formats 3 and 5).

**One rule, therefore one resolver.** §14.7.8 opens "This specification applies to THROUGH phrases specified in the VALUE clause and the EVALUATE statement", so `DataBinder.TryResolveRangeAlphabet` is the single site: it enforces both sentences of §14.9.13.3 SR3 (= §13.18.63.3 SR31 for the VALUE clause) — the operand-class restriction, and the national-vs-alphanumeric class match, the two alphabet domains being disjoint per §12.3.6 SR1/SR2 — and registers the runtime carrier. Diagnostics: **COBOLNET1997** (the word declares no alphabet), **COBOLNET1998** (SR3 sentence 1 — the range is not of a class a collating sequence orders), **COBOLNET1999** (SR3 sentence 2 — the alphabet is of the other class, or names a coded character set only, §12.3.7.4 Table 6's empty collating column for UTF-8/UTF-16).

**Over an identifier-4 the phrase is recovered by SYMBOL (kb/Work PB843).** SR3 admits the phrase over "the literals or identifiers specified in the THROUGH phrase", but `IN` is also the qualification connective, so the parser's greedy `dataReferenceSuffix*` takes `WS-HI IN AL` as a qualified reference. Flipping the grammar's preference would make the genuinely qualified reading unreachable instead, so `EvaluateBinder.BindRangeHigh` decides it: a LAST suffix that is `IN word` (never `OF` — the phrase prints only `IN`; never subscripted — the phrase takes none) naming a declared alphabet of either class (`DataBinder.IsAlphabetName`) is the phrase, because §8.3.2.2 makes an alphabet-name a different type of user-defined word from every name a qualifier can be (§8.4.2.2.1). The reference is bound from `ReferenceResolver.WithoutTrailingSuffix`, a synthetic copy that ADOPTS the original children (the `BinderDriver.Reparent` technique) without mutating the tree, so every resolver walk sees the decided reference with no second resolution path. The VALUE clause has the same ambiguity over a WORD: its operand positions are literal positions, but §13.10.3 SR2 lets a constant-name stand in one, so `88 X VALUE "P" THRU K IN AL` parses `K IN AL` as a qualified reference. `DataBinder.RangeAlphabetPhraseOf` peels it by the same §8.3.2.2 argument and registers the operand in `_alphabetPeeledOperands`, which `BareValueOperandWord` — the ONE accessor every literal-position reader goes through — consults, so the constant-name reader sees K alone (kb/Work PB983). The CLASS clause's `IN alphabet-name-4` follows literals only and is not affected.

**`IN` is an OPTIONAL word in all three formats that print the phrase (kb/Work PB983).** Measured off the printed pages (PDF p649 / folio 619 for §14.9.13.2; p546 / folio 516 for §13.18.63.2 formats 3 and 5): `IN` is not underlined in any of them, so §5.2.3 makes `WHEN "M" THRU "A" AL` and `88 X VALUE "A" THRU "Z" AL` conforming. (The CLASS clause's `IN alphabet-name-4`, §12.3.7.2, IS underlined and stays required.) The two clauses decide the IN-less spelling in the two places that CAN decide it:
- **EVALUATE — by position, in the grammar.** `valueRange : valueOperand (THRU|THROUGH) valueOperand (IN? cobolWord)?`. After a complete right-hand operand only `ALSO`, `WHEN` or the imperative statement's verb — all reserved — may follow a selection object, so a bare user-defined word there can be nothing but alphabet-name-1; `EvaluateBinder.RangeAlphabet` still checks its TYPE through the one resolver.
- **VALUE — by symbol, in the binder.** A constant-name is a legal literal-2, so a bare trailing word is by position one more operand and the operand loop takes it. `DataBinder.RangeAlphabetPhraseOf` decides it: a LAST bare operand (after at least one literal-2) that names a declared alphabet is the phrase and is skipped as a literal; any other word keeps its literal-position reading and its COBOLNET1639.
`TryResolveRangeAlphabet` asks the word's TYPE before the range's class, so a stray word after a range draws COBOLNET1997 ("names no alphabet") rather than COBOLNET1998. `TrailingInPhraseDriftTests` pins both halves together. Goldens: `2002/pb983_range_alphabet_in_optional` (both clauses, both spellings, the constant-name arm) and `negative/pb983-range-trailing-word-not-alphabet`.

**The phrase OVERRIDES the default, PROGRAM COLLATING SEQUENCE included.** Rule 2's two arms are exclusive: with no phrase "the collating sequence is defined by the implementor" (this compiler's implementor choice is the program collating sequence), and with the phrase it is "the collating sequence defined by that alphabet". So a range written `IN <an identity alphabet>` collates NATIVELY inside a program whose PCS reorders the alphabet — the discriminator the `pb398_range_in_alphabet` golden pins.

**It governs the RANGES, not the singletons.** §14.7.8 is the THROUGH phrase's specification and rule 2 names the sequence "used for range evaluation"; a singleton condition-name value is compared by §8.8.4.5.3 GR2's ordinary relation rules, hence under the PCS. `ConditionRenderer.RenderMembershipTest` therefore takes two collate arguments, and the golden lists a singleton beside a range in one clause to pin the split. Correspondingly, the SR3 screens run only when the clause actually writes a THROUGH phrase: over a plain value list the phrase governs nothing and is inert, not erroneous.

**⛔ THE TWO CLAUSES NEED TWO GOLDENS, BECAUSE ONE PROGRAM CANNOT DISCRIMINATE FOR BOTH.** `pb398_range_in_alphabet` names its alphabet in `PROGRAM COLLATING SEQUENCE` — which is what makes its EVALUATE legs discriminating (leg B's `IN <identity alphabet>` answers natively where the PCS would not), but which for that same reason makes its three level-88 legs answer *identically whether the phrase is read or discarded*: with the PCS and the named alphabet being the same sequence, `RangeCollateArg`'s two branches return the same table. So the VALUE-clause arm takes `pb502_value_range_in_alphabet`, which declares **no** PCS: rule 2's no-phrase arm then resolves to the native order and every leg is the opposite of its no-phrase twin, so re-dropping the bind flips a line. It also carries the two legs `pb398_range_in_alphabet` has no room for — the EC-RANGE-INVALID sentence measured on the NAMED sequence in both directions (an alphabet-inverted range raises where the native order would not, and an alphabet-ascending one stays silent where the native order would raise), and the printed bracket order `IN alphabet-name-1` before `WHEN SET TO FALSE IS literal-4`. Diagnostics get the same treatment: COBOLNET1997/1998/1999 have a negative on **each** arm, `pb398-range-alphabet-*` for EVALUATE and `pb502-value-range-alphabet-*` for the VALUE clause (kb/Work PB502 — the code fixed both arms at once, the tests had only covered one).
**§14.7.8's rule-1 / rule-2 SPLIT is one predicate, and it keys on the range's CLASS (kb/Work PB401).** The clause has exactly two rules — rule 1 for "a range of values … defined by numeric literals" (algebraic, no collating sequence, **no exception condition**) and rule 2 for one "defined by alphanumeric or national literals", which owns the sequence, the `IN alphabet-name-1` phrase **and** EC-RANGE-INVALID together. `CollatingSelection.IsCollatedThroughRange(CollatingSelection.ThroughRangeClass(lo, hi))` is that split, written once, and **three sites ask it**: the EVALUATE range's EC gate, the level-88 VALUE range's EC gate, and `TryResolveRangeAlphabet`'s SR3 screen. The class is the PAIR's — `ForComparison` over both ends' categories — because §14.9.13.3 SR4 gives the two operands one class to have.

**Rule 2's "literals" is not a restriction on the OPERAND FORM.** §14.7.8 introduces the phrase as "a range of values, literal-1 through literal-2" and then speaks of literal-1/literal-2 throughout; EVALUATE's range-expression has **no** literal-1 or literal-2 at all (§14.9.13.2 prints literal-3 / identifier-3 / arithmetic-expression-3 THROUGH literal-4 / identifier-4 / arithmetic-expression-4), and §14.9.13.3 SR3 — a rule about this very phrase — says "the literals **or identifiers** specified in the THROUGH phrase are of class alphabetic, alphanumeric, or national". Reading "literals" as an operand-form restriction would make the whole of rule 2 — its sequence, its alphabet sentence and its exception alike — inapplicable to every EVALUATE range. The EVALUATE EC gate used to read it that way (`lo is BoundStringLiteral{…} && hi is BoundStringLiteral`), so the exception fired only where a reader of the source can already see the inversion and never in the run-time case rule 2's own "in the collating sequence in effect at runtime" is written for, while the level-88 gate one clause over already asked the class.

**`BoundRangeMembership` renders the relation pair it lowers to, in every respect.** §14.9.13.4 GR4 a) 5. is "selection-subject >= left-part AND selection-subject <= right-part", so the carrier owes whatever `RenderRelational` owes: a signed numeric operand drops its operational sign in an alphanumeric comparison (§8.8.4.2.5 → §14.9.25.4 GR6a — `deSign: true` on all three reads), and a FIGURATIVE end is a **seed**, materialized to the tested item's own character-position count by §8.3.3.6.4 GR2 rather than compared as one character. The seed comes from the same `FigSeed` producer the figurative relation uses (so HIGH-/LOW-VALUE is the tested category's own extreme) and the SIZING is `CobolString.ThruMember`'s `loFig`/`hiFig` overload, beside `CompareFig` — in the runtime, because the width is unknowable at emit time for a ref-mod slice with computed bounds, and because spelling `FigToWidth(seed, (read).Length)` in the emitter would evaluate the tested operand twice and give GR2's sizing a second author.

**One category reader behind all of it.** The range's class is read through `CollatingSelection.OperandCategory`, which is now `IntrinsicResultType.OperandCategory`'s body rather than a second copy of it. The second copy answered null for a computed operand, for a numeric-result intrinsic and for a boolean operand, and `ForComparison` reads a null as the ALPHANUMERIC arm — harmless for a relation (§8.8.4.2.5 makes a numeric operand against an alphanumeric one an alphanumeric comparison anyway) and a wrong answer for a range, where the null/alphanumeric read put an arithmetic-expression-ended range (numeric by §8.8.1.1) under rule 2: `EVALUATE N WHEN A + 1 THRU B + 2 IN AL` compiled clean past SR3 and was evaluated as a string range.

**The carrier is a per-TYPE field, not an inline expression** (`DataBinder.RangeCollations` → `ObjectComputerEmit`, one `static readonly CobolCollation` per named alphabet, collapsed onto `__COLLATE` / `__COLLATE_NAT` when the alphabet IS the program collating sequence, and omitted entirely for an identity sequence). A WHEN pair is re-analysed on every execution of its EVALUATE, so rendering `new AlphanumericCollation(new ushort[256]{…}, …)` there would allocate a 256-entry table per evaluation; SORT/MERGE and the indexed-file key registrations render theirs inline because each of those runs once per statement. The bound tree carries the COBOL alphabet-NAME and the renderer maps it, so no generated identifier travels in a bound node.

**Residue, named.** `IN` is also the qualification connective, so over an identifier-4 both readings — `identifier-4 IN group` and `identifier-4` plus the alphabet phrase — are complete parses and ANTLR's greedy suffix loop takes the qualifier. Only the resolved SYMBOL separates them (§8.4.1 puts the two name classes in disjoint domains) and flipping the grammar's preference would make the opposite legal reading unreachable instead, so the literal-operand spelling the printed figure shows is what works today; the identifier spelling reports COBOLNET1639 exactly as it did before the phrase existed. Closing it needs a `ReferenceResolver` that can re-resolve a reference without its trailing qualifier — its own mechanism.

### ON SIZE ERROR leaving the receiver unchanged + multiple receivers + rounding interaction (ISO §14.7.5).

CobolNum.TryStore (the single settled name — see the C# mapping) computes the candidate, rounds to scale FIRST, then tests integer-part capacity; writes the field only if no overflow (or no SIZE ERROR phrase present). Each receiver tested independently; non-overflowing receivers updated; the phrase fires if ANY overflowed (OR of per-receiver flags). Division-by-zero and exponentiation-rule violations route to the same size-error path (EC-SIZE-ZERO-DIVIDE / EC-SIZE-EXPONENTIATION).

### RESUME control flow (NEXT STATEMENT vs procedure-name vs GLOBAL-declarative≡CONTINUE) requires a declarative to redirect the caller's control after it returns (ISO §14.9.33).

As built there is no ResumeAction enum: declaratives are pc ranges, a RESUME statement throws `ResumeSignal`, and the generated `__RunUse` converts it into the `DispatchResult` every raise site reads (`ResumeNext` RESUME AT NEXT STATEMENT → fall through past the applicable statement, suppressing a fatal termination; `≥0` RESUME AT procedure-name's pc → as if GO TO; `Normal` fell off the end → continue for a nonfatal condition, terminate for a fatal one, §14.9.49.4 GR13) — the full protocol is "The declarative dispatch-result protocol" under EC engine specifics above. A RESUME written in a USE … GLOBAL declarative is refused at bind (COBOLNET0713, §14.9.33.3 SR2), whether written directly or nested in a WHEN phrase inside it (`EcBinder.BindResume` asks the GLOBAL question before the WHEN-phrase one, the order `PlacementRules.RefusedInGlobalDeclarative` gives GOBACK and EXIT). §14.9.33.4 GR1 — a RESUME *executed within the scope of* a global declarative (one the global declarative PERFORMs, §14.9.49.3 SR4) is a CONTINUE — is a RUN-TIME question, answered by the same per-program-instance `__useActive[global ids]` test (`DispatchState.InGlobalDeclarativeRangeTest`) that §14.9.18.4 GR6's EC-FLOW-GLOBAL-GOBACK uses: `EcEmitter.EmitResume` renders `if (!(<test>)) throw new ResumeSignal(…);` in a unit that declares a GLOBAL declarative, so the RESUME is a no-op while one is active and falls through (kb/Work PB1160); a unit with none emits the unconditional unwind. "Scope of execution" is read per program instance: a RESUME in a program CALLed from a global declarative resumes normally in its own activation (docs/CONFORMANCE.md §3). Consequently a declarative reached through `__RunGlobalUse` can never return a resume action. The §14.6.13.1.2 1) "fatal exception occurs within the scope of the declarative" half of normal completion is recorded by the DECLARATIVE ACTIVATION RECORD (kb/Work PB1122 Task B; the model is kb/Work PB1761): `__RunUse` of a DECLARATIVE id (`__id < DeclCount`; an exception-checking PERFORM's WHEN-handler ids share the invoker and are not declaratives) opens a record on the run-unit `ExceptionEngine` (`EnterDeclarative(__useActive)`), closes it in its `finally`, and on falling off its end answers `NotNormal` when the record was marked. Two marks: the fatal raise funnel (`ExceptionEngine.Set(…, fatal: true)` — "occurs" read as "is raised", i.e. checking enabled; it marks EVERY open record, the scope being dynamic across PERFORM and CALL) and the no-op RESUME of `EmitResume` (`MarkDeclarativesNotNormal(__useActive)` — only the records of the RESUME's own program instance, the rule saying "specified in this … program"). Only a rule keyed on normal completion tells `NotNormal` from `Normal`: the SORT (§14.9.40.4 GR17) and MERGE (§14.9.24.4 GR7, GR12) implicit transfers (`SequentialIoEmitter.EmitUseHook` jumps on `DispatchResult.TerminatesSortMerge`); every other consumer lets the statement finish (`FinishesStatement`, `ForHandledWarning`), because §14.9.49.4 GR13 and GR7 b)/c), GR12 b)/c) dispose of the condition by its FATALITY after the procedure returns. The Format 4 object selector's result is likewise treated as `Normal` (§14.6.13.1.5 names only the normal case; docs/CONFORMANCE.md §3). Pinned by `conformance:2002/pb1122_declarative_fatal_in_scope_merge` and `unit:DeclarativeActivationRecordTests`.

### >>TURN must gate WHETHER a guard is emitted per statement, in source order, with EC-ALL/level-2 expansion — without a runtime cost when OFF.

A compile-time TurnState walks the procedure division in source order maintaining the enabled-EC set (EC-ALL→all; level-2→its level-3 children; EC-I-O-WARNING explicit-only). The emitter consults TurnState.IsEnabled(ec, atStatement) before emitting any EC check. OFF compiles to nothing — no runtime branch. WITH LOCATION makes the guard pass (file,line,verb) into ExceptionState.

### Whole-group comparison (IF GROUP-A = GROUP-B, or group vs literal) compares the group as one alphanumeric value, but a group is a record struct in the typed model (no character buffer).

The whole-group character-image facility: every group record struct emits AsImage()/FromImage() (GroupImageCodec), and a character-image group operand routes through .AsImage() into CobolString.Compare (OperandText). COBOLNET0708 is retired. This is the one IF/EVALUATE operand kind that native field comparison cannot do alone.

### Distinguishing a bare data-name operand in a condition: level-88 condition-name vs boolean PIC 1 vs mnemonic switch vs numeric-implicit-≠0 vs alphanumeric truthiness.

`ConditionBinder.AnalyzeBareOperand` resolves the SYMBOL once (D5a): a level-88 condition-name → the conditional
variable's bool property; a switch-status condition-name → the switch test; a one-position boolean item or literal
→ a §8.8.4.3 simple boolean condition (SR1's length screen, COBOLNET1511); a bare class-name / alphabet-name → an
EVALUATE partial-expression object only (§14.9.13.3 SR5, kb/Work PB843). Inside an abbreviated combined relation
any other operand is the OBJECT of the carried subject and operator (§8.8.4.12). Everything else — an alphanumeric
or numeric data item, a literal, a figurative, an arithmetic expression, a function reference, a class-name
outside EVALUATE, a switch's mnemonic-name — is **not a condition** (§8.8.4.2.1: "The simple conditions are the
relation, boolean, class, condition-name, switch-status, sign, and omitted-argument conditions") and is refused at
compile time by `ConditionBinder.RefuseNonCondition` with COBOLNET2318, which names what the operand IS. There is no
numeric-≠0 or alphanumeric-truthiness reading: the legacy engine's were non-ISO, and until kb/Work PB982 the
greenfield binder compiled such a condition with no diagnostic and aborted the run unit when it was reached.

**Parentheses around a simple condition change nothing (kb/Work PB1464).** §8.8.4.2.1: "The inclusion in parentheses
of simple conditions does not change the simple condition truth value." The grammar reads `(X1)` as a parenthesized
ARITHMETIC operand (`primaryCondition` tries `comparisonExpression` before `LPAREN condition RPAREN`, and reordering
them is the DEVLOG-621 regression), so the classification seam looks through the enclosing parentheses:
`ConditionBinder.ConditionOperandExpression` (over `SoleOperand.Unparenthesized`) is the ONE read, used by
`AnalyzeBareOperand` (level-88, switch-status, boolean item), `IsBooleanValueOperand` / `BindBoolOperandValue` and
the not-a-condition report. `SoleOperand.Primary` stays the OPPOSITE reading on purpose (§8.8.4.7.3 SR2's "not
enclosed in parentheses"). A parenthesized boolean operand is also the boolean operand of a relation or an EVALUATE
subject (`ConditionBinder.EnclosedBooleanOperand`, §8.8.2 "a boolean expression enclosed in parentheses"); a
parenthesized NON-boolean operand stays an arithmetic expression (§8.8.1.1), and a parenthesized class-name is no
class condition.

**The truth words are no condition (kb/Work PB1668).** TRUE and FALSE are an EVALUATE selection subject or object
(§14.9.13.3 SR7 b); `IF TRUE`, `A = 1 AND TRUE` and the like are COBOLNET2318 naming the word. EVALUATE decides a
TRUE-or-FALSE × TRUE-or-FALSE pair by Table 15 and GR3 f) / GR4 a) 4. as a constant (`EvaluateBinder.Constant`) and
never binds the object word as a condition.

**One boolean-operator token list (kb/Work PB1412).** `BooleanOperatorTokens` (Frontend/Expressions) classifies the
eight operator tokens — infix (B-AND, B-OR, B-XOR and the four 2023 shifts), prefix (B-NOT) — and is asked by the
parser's `boolExprAhead` / `boolArgAhead`, `ConditionBinder.HasBoolOp` and `BooleanOperatorGate`; the binder's copy
had omitted the shifts, so a shift-only boolean expression in an IF or a relation was refused.
`BooleanOperatorTokenDriftTests` reads the lexer vocabulary and bars a second list. The EVALUATE half of the same note
is D5a's "A boolean EXPRESSION is a selection operand" paragraph: `EVALUATE A B-AND C WHEN B"1000"` is Table 15's
Boolean-expression column, not a condition.

**The relational operator is the printed Format 1 set (kb/Work PB1034).** `comparisonOperator` holds exactly the
alternatives §8.8.4.2.2 Format 1 prints (rendered page PDF p217): `[NOT]` on GREATER THAN, >, LESS THAN, <, EQUAL TO
and = only, EQUAL's optional word TO. The five spellings it used to add (NOT >=, NOT <=, NOT GREATER [THAN] OR EQUAL
[TO], NOT LESS [THAN] OR EQUAL [TO], EQUAL THAN) are now parse errors in every consumer — IF, EVALUATE, PERFORM UNTIL,
SEARCH WHEN, the abbreviated tails and the `>>IF` relation, whose own `IS? NOT?` prefix was removed. START's
membership screen (`InGeneralRelationFormat`) is gone with it; only the NOT EQUAL exclusion of §14.9.41.3 SR3 stays
START's own. `RelationalOperatorFormatDriftTests` expands the rule into spellings and requires them to equal the
printed set.

**§8.8.4.12.3 SR1 is a property of the relation's class (kb/Work PB1391).** `AbbrevCarry.BooleanRelation` is set where
the carry is seeded — `BindComparison` (either operand of class boolean, figurative ZERO excluded: boolean only by
context), `BindPrimaryBoolean`'s relation arm, and `BindPartialComparison` (after the spliced relation, which is
relation-condition-1) — and both consumers of the carry (`BindAbbreviatedRelation`, the bare object of
`BindSoleOperandCondition`) refuse through `RefuseAbbreviationAfterBooleanRelation` (COBOLNET2723). It used to hold
only where the boolean-operator parse path happened to reset the carry, and `B1 = B2 OR <> B3` was silently expanded.

**One construction site for a condition error node (kb/Work PB982).** `BoundConditionError` is built ONLY by
`ConditionBinder.Refused(feature)` (`EvaluateBinder` reaches it through `host.Cond`). A refusal reports the rule's
own diagnostic first; `Refused` then fails the compile with the internal COBOLNET2319 if no failing diagnostic has
been recorded, so a condition error node can never reach a successful compile and the emitter's
`NotImplemented` lowering of it is unreachable. `ConditionErrorConstructionDriftTests` (Unit) keeps every other
`new BoundConditionError(` out of the compiler.

## Edge cases

- NEXT SENTENCE (obsolete) is NOT CONTINUE: it jumps past the next period, so it lowers via a labeled sentence block + goto, unlike a plain if-fall-through (COBOLNET0701).
- ALPHABETIC is the closed set {A-Z,a-z,space} only (ISO §8.8.4.4) — must NOT use char.IsLetter (rejects accented/Unicode letters); legacy comment line 2436.
- IS NUMERIC on a signed numeric-DISPLAY item accepts the overpunch sign ({,A-I positive; },J-R negative) or separate sign (+/-) at the sign position; spaces are NOT digits so a field with embedded/trailing spaces is NOT NUMERIC.
- IS NUMERIC on a pure native long/Int128 item folds to constant true (COBOLNET0706): the fold applies ONLY to a numeric item with no REDEFINES/overlay view; an aliased item routes through the runtime byte-form-keyed check (CobolNum.IsNumericImage, §8.8.4.4.4 GR3 n)1; CobolClass.IsNumeric is n)2's non-numeric-category arm and n)1.a's zoned delegate).
- NOT POSITIVE means ≤ 0 (includes zero), which is NOT the same as NEGATIVE — the !(>0) wrap gets it right.
- Figurative ZERO compared with a numeric value is the numeric 0 (ISO §8.3.3.6.4 r4), not the character '0'.
- Literal-vs-literal comparisons constant-fold at emit time to true/false (clean output, matches mainstream compilers).
- SET cond TO FALSE with no WHEN SET TO FALSE phrase is a syntax error (§14.9.39.3 SR7 — the FALSE phrase is required) → COBOLNET2049. The TRUE arm needs no twin screen: §13.18.63.3 SR24 already makes a VALUE clause mandatory on a level-88 entry.
- literal-4 shall not be a value the condition-name is TRUE for (§13.18.63.3 SR27) → COBOLNET2048, screened where literal-4 is bound (`DataBinder.CheckFalseValueDistinct`; derivation in COBOLNET_DATA_MODEL_DESIGN.md).
- SET cond TO TRUE on a THRU-range condition-name moves the range START (first literal).
- EVALUATE subjects with side effects (function calls / arithmetic) must be hoisted to a local and evaluated exactly once (ISO §14.9.13.4 GR3); bare identifiers/literals may stay inline.
- Multiple consecutive WHEN phrases sharing one body are OR-ed (WHEN a WHEN b … imperative = a OR b); ALSO subjects within one WHEN are AND-ed.
- WHEN with no match and no WHEN OTHER → EVALUATE does nothing (no final else emitted).
- ON SIZE ERROR leaves the receiver UNCHANGED; without the phrase, overflow silently truncates to the PICTURE low-order digits — UNLESS EC-SIZE is >>TURNed on (the bridge between the phrase and the EC mechanisms).
- ROUNDED happens BEFORE the size-error test (round to scale, then check integer-part capacity).
- AT END/INVALID KEY phrase, when present and the condition exists, suppresses all OTHER applicable exception processing (ISO §11409) — the phrase wins over declaratives.
- Pointer relations support only = / NOT = (ISO §8.8.4.2.2 Format 3 — the message-tag-object-or-pointer-reference general format admits only the EQUAL/=/<> operators; comparison rule §8.8.4.2.16) → ReferenceEquals / is null on ManagedPointer.
- EC-I-O-WARNING can only be turned on/off explicitly in a >>TURN or PERFORM WHEN (§5006); EC-ALL does not include it.
- Whole-group comparison routes through the record struct's AsImage() character image into CobolString.Compare.
- User-defined exceptions EC-USER-<suffix> are always nonfatal (ISO §24505) and only raisable by RAISE / EXIT…RAISING / GOBACK RAISING.
- A condition-name may be qualified/subscripted (cond-name OF grp (i)) → emit a parameterized bool method, not a property (tables are supported).

## Per-edition gating (G1 — one `cobol.exe`, four ISO editions via `--std`)

Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in
every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed).
Tests (NIST etc.) only VERIFY; they never SCOPE. Gating keys off the single `DialectMode` (SSOT §2); the per-construct
rows live in `docs/VERSION_CHANGE_REFERENCE.md` (VCR) — the 130-row edition-change checklist (2002→2023 deltas ONLY;
it has NO 85→2002 rows — derive 85↔2002 gating from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog) —
and become (construct × edition) cases in the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md`; Phase 0
done).

- **COBOL-85 baseline (valid in all four editions):** IF/ELSE/END-IF, EVALUATE, CONTINUE, level-88 + SET cond TO TRUE,
  class/sign/relation/abbreviated-combined conditions, the ON SIZE ERROR / AT END / INVALID KEY / ON OVERFLOW /
  ON EXCEPTION phrases, and USE AFTER STANDARD ERROR/EXCEPTION file declaratives.
- **XOR / EXCLUSIVE-OR (D1/D2): introduced 2023** (VCR rows 32/41 — user-defined words before). At `--std=85|2002|2014`
  an XOR operator in a condition is a diagnostic, and `XOR`/`EXCLUSIVE-OR` must still be accepted as user-defined words.
  The gate is `LogicalOperatorGate`, asked on RECOGNITION of the one `xorOperator` grammar node — by
  `VersionConformancePass.VisitXorOperator` for every condition tier and EVALUATE partial expression (kb/Work PB1392:
  the tier-keyed gate had missed the partial spine), and by the conditional-compilation stage per `>>IF` fragment
  (kb/Work PB1371).
- **The EC model is 2002+:** `>>TURN`, the EC-* exception-names, RAISE, RESUME, EXCEPTION-OBJECT, the EC-name USE
  declarative form (USE AFTER EXCEPTION CONDITION), and FUNCTION EXCEPTION-STATUS/-FILE/-LOCATION/-STATEMENT. At
  `--std=85` each gets a not-in-this-edition diagnostic.
- **2023-only EC additions, diagnosed at 85/2002/2014:** the exception-checking PERFORM (VCR row 79), `>>PROPAGATE`,
  EC-I-O-WARNING and the EC-MCS-*/EC-CONTINUE-*/EC-EXTERNAL-* names plus the THREE commit-and-rollback EC-FLOW
  names — EC-FLOW-APPLY-COMMIT, EC-FLOW-COMMIT, EC-FLOW-ROLLBACK (VCR rows 40/61) — and the optional
  file-connector argument of EXCEPTION-FILE/-N (VCR rows 68/69). ⛔ NOT `EC-FLOW-*` as a family: VCR row 40 names
  only those three, and the rest of the EC-FLOW level-3 names (EC-FLOW-USE, EC-FLOW-SEARCH, EC-FLOW-REPORT,
  EC-FLOW-RELEASE, EC-FLOW-RETURN, EC-FLOW-GLOBAL-EXIT/-GOBACK) are 2002 EC-model names — `ExceptionCatalog`
  carries each name's own introduction edition, which is what the gate reads.
- **SET cond-name TO FALSE / WHEN SET TO FALSE (D7): 2002+** — diagnosed at `--std=85` with COBOLNET0900, gated
  recognition-first in `VersionConformancePass.ParseArm` (`VisitValueClauseFalsePhrase`, `VisitSetBooleanStatement`)
  on the `constructs.json` rows `value-false-phrase-2002` / `set-condition-false-2002`; COBOLNET2049 (missing FALSE
  phrase) applies only in editions that have the phrase.
- **CALL … ON OVERFLOW: REMOVED in 2023** (VCR row 3) — accepted at 85/2002/2014, diagnosed at 2023 (ON EXCEPTION is
  the replacement).
- **VALIDATE / EC-VALIDATE: obsolete in 2023** (VCR row 125; SSOT §18.17) — flag obsolete.
- **NEXT SENTENCE:** edition-flagged per the spec's obsolete/archaic classification (see the edge-case note above).

## ISO citations

- ISO/IEC 1989:2023 §8.8.4.2 — simple relation conditions (algebraic numeric value comparison §8.8.4.2.4; alphanumeric comparison + space-extension of the shorter operand §8.8.4.2.7 rule 2; pointer = / NOT = §8.8.4.2.16, Format 3 general format §8.8.4.2.2)
- §8.8.4.12 — abbreviated combined relation conditions (elided subject/operator; NOT is part of the operator)
- §8.8.4.4 — simple class condition (NUMERIC; ALPHABETIC closed set {A-Z,a-z,space}); §8.8.4.7 — simple sign condition
- §8.8.4.5 — simple condition-name condition (88-level abbreviates 'conditional variable == one of its values')
- §8.8.4.9 — logical operators AND / OR / EXCLUSIVE-OR / XOR / NOT and their meanings; §8.8.4.11.3 — precedence NOT > AND > XOR > OR; §8.8.4.13 rule 1 — left-to-right order of evaluation with short-circuit termination of each hierarchical level
- §8.3.3.6.4 r4 — figurative ZERO as numeric 0; §8.4.3.6 — EXCEPTION-OBJECT predefined object reference
- §13.18.63 — VALUE clause condition-name format (THRU ranges; WHEN SET TO FALSE literal-4; GR20 SET TO FALSE)
- §14.6.13 / §14.6.13.1.1 — exception condition handling; default EC-ALL OFF; last-exception status; per-element indicators cleared at start of each statement
- §14.6.13.1.3 / §14.6.13.1.4 — fatal vs nonfatal exception condition handling order (phrase → PERFORM WHEN → USE declarative → continue/terminate)
- §14.6.13.1.5 — exception objects (RAISE id / RAISING); §14.6.13.1.6 + Table 13 — exception-name hierarchy (level-1 EC-ALL, 23 level-2 names, level-3 + fatality)
- §14.6.13.2 — incompatible data (EC-DATA-INCOMPATIBLE; the reason class conditions exist)
- §14.7.5 — SIZE ERROR phrase and size error condition (receiver unchanged on error; rounding before test; EC-SIZE-OVERFLOW/ZERO-DIVIDE/EXPONENTIATION)
- §14.9.9 CONTINUE; §14.9.13 EVALUATE (§14.9.13.3 syntax incl. Table 15 operand combinations; §14.9.13.4 GR3 subjects-once, GR4 left-to-right first-match, GR5 WHEN OTHER); §14.9.19 NEXT SENTENCE
- §14.9.29 RAISE statement (EXCEPTION ec-name / identifier object; nonfatal-unhandled acts as CONTINUE)
- §14.9.33 RESUME statement (NEXT STATEMENT / procedure-name; GLOBAL declarative ≡ CONTINUE)
- §14.9.18.4 GR1b/GR3 (GOBACK) + §14.9.14.4 GR2 (EXIT PROGRAM) — RAISING propagation: the condition is raised in the activator only if checking for it is enabled there; with no calling runtime element the RAISING phrase is ignored (GOBACK acts as STOP, EXIT PROGRAM as CONTINUE)
- §14.9.28.4 GR14 — the exception-checking PERFORM's implicit TURN scoping: WHEN-named ECs implicitly enabled over imperative-statement-1, then an implicit PUSH ALL + TURN OFF ALL for the handler bodies / FINALLY, and a POP ALL immediately preceding END-PERFORM (GR20 fatal/nonfatal return, GR22 post-PERFORM checking retention)
- §14.9.39 SET statement (GR6 condition-name TO TRUE → first VALUE literal; switch ON/OFF) ; §14.9.49 USE declaratives (GLOBAL; AFTER EXCEPTION/ERROR; ON file/INPUT/OUTPUT/I-O/EXTEND)
- §9.1.12 input-output exception processing (applicable exception processing statements; first-match selection); §9.1.13 I-O status → EC-I-O-* mapping (1x AT-END, 2x INVALID-KEY, 3x/4x/7x fatal, etc.)
- TURN compiler directive (§4970/§5000-§5024 — default EC-ALL OFF; EC-ALL/level-2 expansion; WITH LOCATION; EC-I-O-WARNING explicit-only); PROPAGATE directive (§4808)
- §15.28–15.33 — EXCEPTION-FILE/-LOCATION/-STATEMENT/-STATUS intrinsic functions

## Resolved questions (settled in `COBOLNET_DESIGN.md` §18 — answers recorded inline per the keep-deep-dives-current rule)

- SETTLED (kb/Work R05 — the §15.33 width collision): **FUNCTION EXCEPTION-STATUS returns exactly the 31-character
  value §15.33.3 r1 prescribes, truncating a longer exception-name to its 31-character prefix.** The collision is
  the STANDARD's own: COBOL-2023 words run to 63 characters (§8.3.2.1) and the §14.6.13.1.1 EC-USER-/EC-IMP-
  suffixes are unbounded, so level-3 names of 32..63 characters are legal yet indistinguishable through this one
  function — while checking, declarative selection, and PERFORM-WHEN matching all use the FULL name
  (`ExceptionState.LastName`). The compile-time **COBOLNET1636 advisory** (Warning — legal source stays legal)
  fires once per over-31 spelling from the ONE resolution funnel (`EcNameResolution`), so the truncation is never
  silent. Below 2023 the situation cannot arise: the 31-character COBOL-2002 word ceiling (COBOLNET1567,
  `CobolWordRule` — enforced for tree words AND directive-carried words) rejects the name first.

- SETTLED (§18.16): EC checking ships OFF by default (NIST-faithful, fast, ISO §5000), enabled only by >>TURN/phrases; the conformance corpus drives the EC-on paths.
- SETTLED (§18.16): an unhandled fatal EC terminates the run unit with a diagnostic + a nonzero exit (the ISO §14.6.13.1.3 implementor choice).
- PROPAGATE (§4808) and the exception-checking PERFORM WHEN (§14.9.28) are COBOL-2023 constructs (VCR row 79) — in scope for full-2023 (G1: diagnosed at --std=85|2002|2014); they land after the declarative/phrase path (the seams — declarative-returns-ResumeAction, runtime ExceptionState — admit them without rework).
- SETTLED (§18.17): VALIDATE / EC-VALIDATE is implemented minimally for the conformance corpus and flagged obsolete (2023 Table 13; VCR row 125).
- Program collating sequence for alphanumeric comparisons / HIGH-VALUE/LOW-VALUE remap is designed but deferred until the CobolNet collating subsystem lands; the API seam CobolString.Compare(a,b,weights?) is fixed now — confirm that seam is acceptable so call sites never change.
