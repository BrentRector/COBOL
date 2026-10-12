## Entry 626 — 2026-07-06 12:26 PDT — Phase 4 track (c) residue: REPOSITORY specifiers + keyword-omitted references (M2-UDF-4) — LANDED; the UDF subsystem is COMPLETE

**`FUNCTION ALL INTRINSIC` binding + the §8.4.3.2 SR2 FUNCTION-keyword-omitted reference form are live**, closing
the last M2-UDF item. With `FUNCTION ALL INTRINSIC` (or `FUNCTION name INTRINSIC`) in the REPOSITORY, an intrinsic
may be referenced WITHOUT the word FUNCTION: `COMPUTE R = MAX(A, B)`, `MOVE MOD(B, 10) TO R`. Built FROM the
`docs/PHASE4_RECONCILIATION.md` §M2-UDF-3/4 design (decision **D2**).

**D2 — bind-side, NO grammar change (the recon's key finding).** `name(args)` already parses as a subscripted
`dataReference`; a keyword-omitted `functionCall` grammar alternative would be an irreducible ambiguity with it,
and the `boolExprAhead()` predicate does NOT transfer (the discriminator is SEMANTIC — repository membership —
not a downstream token). So the re-route is at bind:
- `DataBinder` collects `RepositoryAllIntrinsic` (GR14) + `RepositoryIntrinsics` (the named `FUNCTION name
  INTRINSIC` form), inherited into contained programs alongside `UserFunctionNames` (§12.3.4 GR1).
- `KeywordOmittedFunction(dref)` re-routes `name(args)` to the existing `BindIntrinsicCore` at the TWO
  `dataReference→Bound*` chokepoints — `RefExpr` (→ BoundExpr) and `FieldOperand` (→ BoundOperand), each already
  documented as "the ONE mapping used by every path", so every channel (COMPUTE, MOVE, conditions) is covered by
  intercepting those two — when the head name is a repository intrinsic / ALL / user-function / prototype (SR6: a
  `(` after such a name is always the argument list).
- **Data-item-wins guard** (zero regression): the re-route fires only when the name is NOT a declared data item
  (`data.LookupData(name)` empty), so a table named like an intrinsic stays a subscript. SR13 (the reserved-word
  prohibition on such a declaration) is staged; the guard makes the ambiguity SAFE regardless.
- Gated `DialectLevel < 2002 ⇒ inert`, so the 85/NIST surface is byte-invariant by construction (the greenfield
  NIST differential confirms — `RefExpr`/`FieldOperand` behave identically at 85).

**Evidence.** New golden `tests/conformance/2002/udf_keyword_omitted.cob` (`GreenfieldOnly` — the frozen legacy
rejects `MAX`/`MIN`/`MOD` without FUNCTION as undefined data-names, CBL3128), byte-exact
`MAX=0034 / MIN=0012 / MOD=0004`. Five new `UdfInvocationTests`: ALL-INTRINSIC binds, named-intrinsic binds,
**data-item-wins** (a `MOD` table under ALL INTRINSIC stays a subscript → TBL=77), the required-declaration
property proven at RUNTIME (an undefined `MAX(a,b)` without a specifier §1.4 loud-fails, naming the reference —
a compile-time assertion CANNOT distinguish it from the intrinsic-routed case since both compile). **NO
version-matrix row**: keyword omission is a runtime-observable 2002 behavior (below 2002 it degrades to the §1.4
loud-fail, not a compile rejection), so it does not fit the compile-time introduction-gating matrix — the golden
+ unit tests are its coverage. Battery: **201→ unit + 1868 conformance GREEN**; the shared greenfield-binder
change is 85-byte-invariant.

**⛔🎉 Track (c) — the user-defined-function subsystem — is COMPLETE**: inline invocation (615), EXIT FUNCTION
(616), separate-compilation prototypes + cross-assembly + EC-FUNCTION-NOT-FOUND (624), REPOSITORY specifiers +
keyword omission (626). Residue is the named staged-loud items only (strict SR10 ordering, SR12/SR13 reserved-word
prohibition, keyword-omitted refmod-on-result, non-numeric RETURNING, `AS literal` externalized name).

