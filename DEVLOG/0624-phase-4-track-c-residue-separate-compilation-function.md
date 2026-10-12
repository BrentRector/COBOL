## Entry 624 — 2026-07-06 11:00 PDT — Phase 4 track (c) residue: separate-compilation function prototypes (M2-UDF-3) — LANDED

**`FUNCTION-ID … IS PROTOTYPE` + cross-assembly user-function resolution are live end-to-end** — the last
NOT-STARTED primary item in the Phase-4 wave-sizing table (`docs/PHASE4_RECONCILIATION.md`). A caller may now
declare only a function's SIGNATURE (a §11.5 Format-2 prototype: LINKAGE-only data + a header-only PROCEDURE
DIVISION, §10.6.2 SR4) and invoke `FUNCTION name(args)`; the DEFINITION is resolved either in-group (§12.3.8
GR11a) or from a **separately-compiled sibling assembly** at run time, and a locate miss raises the correct
**EC-FUNCTION-NOT-FOUND** (Fatal, §8.4.3.2.4 GR6b / Table 13) — not the CALL's EC-PROGRAM-NOT-FOUND. Built FROM
the decision-complete design in `docs/PHASE4_RECONCILIATION.md` §M2-UDF-3/4 (recon: four parallel readers —
spec, greenfield UDF-1 seams, cross-assembly CALL infra, grammar).

**The runtime half was FREE (design decision D1).** A FUNCTION-ID unit already emits as `_PRG_<name> :
ICobolProgram`, registers via the public `__CobolModule.Register()`, and a `FUNCTION name(args)` reference already
lowers to `BoundCallProgram → ProgramRegistry.CallProgram → ResolveVisible → ProbeSiblingModule` (loads
`<name>.dll` from `AppContext.BaseDirectory`) — byte-identical to the DEVLOG-575 cross-assembly CALL (Fix-G). So
UDF-3 was three compile-time seams + one EC-name distinction:
- **Grammar** (additive, `{is2002()}?`-gated, LOW hazard): a new `PROTOTYPE` lexer token + `_dataNameTokens` +
  `cobolWord` + `CheckedTokenTypes`; `functionIdParagraph : FUNCTION_ID DOT programName ({is2002()}? IS? PROTOTYPE)? DOT`
  — a LOCAL rule (never a shared expression/statement core), a unique-leading-token optional tail (the
  `programIdParagraph` shape). Below 2002 a new `EditionGateHints` mapping names the edition (COBOLNET0900).
- **Prototype = signature, no body.** `CallUnit.IsPrototype`; `CallBuildUserFunctionTable` now PARTITIONS the
  group's FUNCTION-ID units into definitions vs prototypes (a prototype precedes all units per §10.6.2 SR1, so
  the old first-wins `TryAdd` would false-report the following same-name definition as a 1508 duplicate — the
  partition prevents that); the definition's signature is authoritative (GR11a), a lone prototype supplies it
  for a separately-compiled target, a same-name pair gets a light §10.6.2 SR3 arity check (new COBOLNET1513).
  A prototype is FILTERED from `CallEmitProgramClass` (no body) and `__CobolModule.Register()` (it must not
  shadow the real definition). `RunMain` now targets the first top-level PROGRAM unit (a prototype precedes
  everything, so `units[0]` may be a prototype; a function/prototype-only module is a callable library with no
  main — only its `Register()` is exposed for the sibling probe).
- **EC-FUNCTION-NOT-FOUND.** `BoundCallProgram.IsFunction` (init-only) → `CallEmitCall` passes `notFoundEc:
  "EC-FUNCTION-NOT-FOUND"` → `ProgramRegistry.CallProgram` (new `notFoundEc` param) stamps it on the
  `CobolCallException`. EC-FUNCTION-NOT-FOUND was already in the catalog (Fatal, inert) — now wired.
- **1505 reworded** — it now fires only when a REPOSITORY-declared name has NEITHER an in-group definition NOR a
  prototype (a genuine unresolved-function error), not the old "not implemented (M2-UDF-3)".

**Evidence.** New golden `tests/conformance/2002/udf_prototype.cob` (`GreenfieldOnly` — the frozen legacy cannot
parse `IS PROTOTYPE`): prototype + caller + in-group definition, byte-exact `P=000049` (SQUARER(7)=49). Two new
cross-assembly tests in `InterProgramFileDifferentialTests` (Fix-G style): the caller+prototype and the
separately-compiled `SQUARER.dll` definition compiled independently → `P=000049` across the boundary; the
definition absent → `EC-FUNCTION-NOT-FOUND` in the loud termination detail. Five new `UdfInvocationTests`
(prototype+definition non-duplicate, prototype-only-binds [the closed 1505 gap], SR3 mismatch→1513,
no-RETURNING→1507, prototype-at-85→0900). Registry rows `function-prototype-2002` + `user-word-prototype-2002`
with `constructs.json` mirrors (the drift test catches an unmirrored row — it did, and was fixed same wave).
Battery: **201 unit + 1863 conformance GREEN** (+12); FULL legacy guard GREEN (the shared-frontend grammar
change is 85-byte-invariant — `PROTOTYPE` behaves as a user word at `--nist`, verified `01 PROTOTYPE PIC X.`
compiles+runs `U=Y` at 85 and 0901s at 2002+).

**Staged residue** (named in the reconciliation as-built): the strict SR10 forward-reference ordering diagnostic
(the DEVLOG-615 leniency stays — a hard error would regress the caller-first udf goldens); `>>TURN EC-FUNCTION`
call-site catching (a fatal raise is spec-correct — EC-FUNCTION-NOT-FOUND is Fatal); full §8.13 external-repository
signature conformance (light in-group arity check only); the `AS literal` externalized-name phrase (default =
the name); non-numeric/group RETURNING (inherits the DEVLOG-615 COBOLNET1510 restriction).

