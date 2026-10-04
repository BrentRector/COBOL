# Phase 4 Greenfield-vs-Catalog Reconciliation

> **⚠ FROZEN HISTORICAL LEDGER (2026-07-19):** authoritative AS OF the P10 close only. The P13 review found
> multiple rows stale ('Deferred … no named track' items now tracked in the plan §0 Wave D / the review ledger
> §24; the 1518 'last-used' note corrected in place). Live state = the plan §0/§8 + the review ledger.

> **STATUS: the AUTHORITATIVE greenfield-truth view of the M2/M3/M4 catalog** (the ratified
> Phase-4 entry audit — `docs/COMPLETION_ROADMAP_COUNCIL.md` Phase 4). The catalog
> `docs/ISO2023_CONFORMANCE_PLAN.md` §3 carries LEGACY-era ☑/◐ marks; THIS table supersedes them with the
> per-item greenfield status verified against `src/Cobol.Net.*` by a 10-agent audit (no LANDED/NOT-STARTED
> mark overturned on cross-check). The single largest finding: the M2-DATA "done" marks are
> LEGACY-ONLY mirages — national/boolean/pointers/floats were implemented in the retired byte engine and
> stage LOUD (COBOLNET0899) in the greenfield; Phase 4 reclaims them. Keep this in sync as tracks land.


Audit of docs/ISO2023_CONFORMANCE_PLAN.md §3 (M2/M3/M4 catalog) against the live greenfield tree
(src/Cobol.Net.*). Cross-checked 8 LANDED + 3 NOT-STARTED (all NOT-STARTED rows in the set) + spot
checks on ALLOCATE / VALIDATE / PicInfo skeletons. **No LANDED or NOT-STARTED mark overturned.** Minor
corrections in the Corrections section.

Legend: LANDED = end-to-end greenfield + green golden; STAGED-LOUD = recognized, fails with a named
diagnostic (0899/0313/COBOLNET15xx or generic BoundUnsupported); PARTIAL = some legs land, some gap;
NOT-STARTED = no greenfield surface; OBSOLETE = superseded by a ratified decision.

## M2-UDF — user-defined functions

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-UDF-1 | Inline UDF invocation FUNCTION user-name(args) | done | **LANDED** (+ the **category-carrying RETURNING channel**, P10 Step 9 2026-07-16: alphanumeric/edited/national/character-form-group results; the per-shape 1510 residues (float/boolean/pointer-class + strong-typed/REDEFINES/variable-length groups) were discharged by kb/Work PB1419) | UdfBinder.cs (Binding/Procedure/Verbs/; bind → hoisted CALL…RETURNING over a §8.4.3.2.4 GR1 result temp — a GROUP model deep-clones via the unregistered `CloneTempNode`); 6 udf_* runtime goldens ENABLED byte-exact (invocation, inline_expression, value_args, recursion, nested_args, returning_categories); UdfInvocationTests; user-function-invocation-2002 registry+matrix row | none | As-built + adversarial-review notes below (the two-phase bind was REALIZED, not found; the non-numeric-RETURNING finding is RESOLVED — see the review note). |
| M2-UDF-2 | Literal/arith args to a UDF | done | **LANDED (DEVLOG 615)** | §8.4.3.2.4 GR5b private-copy cells conformed by CobolArgAdapt; udf_value_args ENABLED byte-exact (LIT/ARI) | none | Folded into M2-UDF-1 as designed. |
| M2-UDF-3 | Separate-compilation function prototypes (§8.13 / §11.5 Format 2) | open | **LANDED (DEVLOG 624)** | `FUNCTION-ID … IS PROTOTYPE` parses (PROTOTYPE token, `functionIdParagraph` tail, 0900 at 85); a prototype registers a signature but emits no runtime module (CallUnit.IsPrototype filters CallEmitProgramClass/Register); cross-assembly resolution reuses the sibling probe (§12.3.8 GR11c); EC-FUNCTION-NOT-FOUND (Fatal) on a locate miss. Golden `udf_prototype` (P=000049, GreenfieldOnly) + 2 cross-assembly tests + 5 UdfInvocationTests | (c) | AS-BUILT below. Runtime half was free (D1). |
| M2-UDF-4 | Bind REPOSITORY FUNCTION specifiers (ALL INTRINSIC / named) | open | **LANDED (DEVLOG 626)** | `FUNCTION ALL INTRINSIC` (GR14) + `FUNCTION name INTRINSIC` now BIND (DataBinder.RepositoryAllIntrinsic / RepositoryIntrinsics, inherited into contained programs); the §8.4.3.2 SR2 **FUNCTION-keyword-omitted** reference form is LIVE bind-side (D2 — the ONE dataReference→Bound* chokepoints RefExpr/FieldOperand re-route `name(args)` to BindIntrinsicCore when the head is a repository intrinsic/ALL/user-function, SR6; data-item-wins guard). Gated ≥2002. Golden `udf_keyword_omitted` (MAX/MIN/MOD without FUNCTION, GreenfieldOnly) + 5 UdfInvocationTests | (c) | AS-BUILT below. GR12 named-specifier leg landed with UDF-1. |

### M2-UDF-1 — DECISION-COMPLETE DESIGN (recon workflow wf_a1e33856-215, 2026-07-05; ready to implement)

> Scope: the three whole-source corpus programs `udf_invocation` / `udf_inline_expression` / `udf_value_args`
> (each a `PROGRAM-ID` caller + a sibling `FUNCTION-ID DOUBLER` with `PROCEDURE DIVISION USING L-X RETURNING L-R`
> / `COMPUTE L-R = L-X * 2`), invoking `FUNCTION DOUBLER(WS-X)` / `DOUBLER(5)` / `DOUBLER(WS-A + 1)` in COMPUTE/MOVE.
> Goldens: `C=0042`/`M=0042`, `EXPR=0043`, `LIT=0010`/`ARI=0010`. **No grammar change** (functionCall,
> functionIdParagraph, `repositoryEntry : FUNCTION functionName INTRINSIC?` all already parse).

- **Spec (ISO 2023).** §9.4 a user function is a FUNCTION-ID unit that behaves like a program but RETURNs a value
  and is always RECURSIVE (independent activations). §12.3.8.2 the name must appear in the caller's REPOSITORY as
  `FUNCTION function-prototype-name` (no INTRINSIC) to resolve as a user function (GR12). §8.4.3.2.4 GR5 argument
  manner: an identifier valid as a receiving operand ⇒ **BY REFERENCE** (`DOUBLER(WS-X)`); a literal / arithmetic
  expression ⇒ **BY CONTENT** (`DOUBLER(5)`, `DOUBLER(WS-A+1)`) — a private copy, COMPUTE-conformed to the formal
  (§14.8.2.3.3 rule 2a; copy-in §14.2.3 GR9). §14.6.5 / §14.2.3 NOTE 1: the result is placed in a TEMPORARY item
  **allocated in the caller** whose description = the RETURNING linkage item. §8.4.3.2.3 SR1: a function-identifier
  is never a receiving operand.
- **KEY ENABLER — the bind is TWO-PHASE.** `CSharpEmitter.Call.cs:94` binds EVERY unit's DATA division
  (`CallBindUnit` → `data.Bind`) before ANY procedure body binds (`:327 binder.Bind`, after `MarkStoreAsImage`).
  So when a caller's PROCEDURE binds `FUNCTION DOUBLER`, the callee's `data.LinkageReturning` PicInfo + USING
  formals are ALREADY resolved — the forward reference (DOUBLER follows the caller in-file) is free, exactly as
  `OoClassTable` gives typed object refs their class defined-later (D1).
- **Registration.** (a) In `DataBinder.cs:131-135` (the repositoryEntry loop that today reads only PROPERTY), also
  collect `re.FUNCTION() is not null && re.INTRINSIC() is null ⇒ re.functionName().GetText()` into a new per-unit
  `UserFunctionNames` set (mirror `OoRepositoryProperties`, :77). (b) Thread the group `CallUnit` list (or a small
  `UserFunctionTable` name→CallUnit built in `CallCollectUnits`, like `OoClassTable`) into each `StatementBinder`
  (alongside `OoClasses`) so a call can find DOUBLER's CallUnit → its bound RETURNING DataItem/PicInfo + USING formals.
- **Dispatch.** `StatementBinder.Intrinsics.cs:55` — on `IntrinsicCatalog.TryGet` MISS, if the name is in the
  caller's `UserFunctionNames` AND the group table ⇒ bind a new `BoundUserFunctionCall`; else the existing
  COBOLNET1501. (One point serves both COMPUTE-expr `BindIntrinsic` and MOVE `IntrinsicOperand`.)
- **Lowering (reuse, no new emit path).** Synthesize a caller-side temp DataItem `__fnres_N` with the RETURNING
  PicInfo (a Roots-declared field, the property-ref synthesis pattern), bind a `BoundCallProgram` = CALL "DOUBLER"
  USING «args» RETURNING «temp» (args: identifier→Reference, literal/arith→Content over a temp of the formal's PIC —
  `BoundCallArg` already models all three modes), and HOIST it before the enclosing statement via the property-ref
  `BoundSequence` mechanism (`OoWrapPropertyOps`, DEVLOG 607); the expression/operand then reads «temp». Emission is
  the existing `CallEmitCall` → `ProgramRegistry.CallProgram(...)`; FUNCTION-ID units already emit as callable
  `_PRG_DOUBLER` with the RETURNING carrier.
- **EXACT SEAMS (all confirmed by reading — turn-key).** (1) Group table: build a `name→CallUnit` (or
  `name→{ReturningItem, UsingFormals}`) map AFTER the DATA-bind loop `CSharpEmitter.Call.cs:94`
  (`foreach unit CallBindUnit`) and BEFORE procedure binding (`:327`); each function unit's RETURNING item is
  `data.LinkageReturning` and its USING formals live in the DataBinder linkage (DataBinder.Linkage.cs). Thread it
  into each `StatementBinder` like `OoClasses` (constructed at `:325`). (2) Temp synthesis: mirror
  `DataBinder.OoCreatePropertyTemp` (DataBinder.Oo.cs:254 — synthesizes a level-1 elementary root DataItem from a
  model) to make `__fnres_N` from the RETURNING item. (3) Hoisting: the chokepoint is `StatementBinder.cs:158-160`
  (`int mark = data.OoPendingPropertyOps.Count; core = BindStatementCore(s); core = OoWrapPropertyOps(core,mark)`).
  Add a parallel `data.PendingUdfCalls` list + a `UdfWrap(core, udfMark)` that prepends one `BoundCallProgram`
  per pending call → `BoundSequence` (UDF is ALWAYS a pre-op — a function-identifier is never receiving, §8.4.3.2.3
  SR1 — so no `BoundStores.StoreKindOf` polarity step, unlike property refs). (4) Args: reuse the CALL
  `BoundCallArg` construction (StatementBinder.Call.cs) — identifier→Reference, literal/arith→Content over a temp
  of the formal's PIC. (5) The bound value the expression/MOVE reads is a `BoundComputedOperand`/field operand over
  `__fnres_N`, carrying the RETURNING PicInfo for arithmetic typing. Nested-UDF / property-op-ordering within one
  statement is a documented follow-up (the corpus has neither).
- **Tests / registry / docs.** Flip `udf_invocation`/`udf_inline_expression`/`udf_value_args` pending→enabled
  (`tests/conformance/2002/manifest.json`); add a `ConstructRegistry` row `user-function-invocation-2002`
  (IntroducedIn 2002) + a version-matrix row per feedback_conformance_tests_per_feature; a dedicated
  `UdfInvocationTests` (no-arg/ref/content/nested-in-expr). Unblocks M2-UDF-2 (folds in) and EXIT FUNCTION (M2-PROC-6).

#### M2-UDF-1 — AS BUILT (DEVLOG 615, 2026-07-05; all three goldens byte-exact on first run)

Landed exactly on the design's lowering (temp + hoisted `BoundCallProgram` + `BoundSequence`; zero new emit
surface; no grammar change), with these deviations/realizations — recorded per the process rule:

1. **The "KEY ENABLER" was REALIZED, not found.** The design read `CSharpEmitter.Call.cs` as already two-phase;
   in truth `CallBindUnit` bound each unit's DATA **and** PROCEDURE per-unit sequentially, so a caller's
   procedure bind could not see a later FUNCTION-ID unit's signatures. The as-built SPLITS it:
   `CallBindUnitData` (DataBinder + GLOBAL/index injection + bridges + ReferenceResolver) loops over ALL units,
   then `CallBuildUserFunctionTable`, then `CallBindUnitProcedure` loops (StatementBinder + formal resolution).
   Relative order vs `MarkStoreAsImage` and class binding is unchanged; INV-1-STRONG + the full battery prove
   the restructure behavior-neutral.
2. **The table is `Dictionary<string, UserFunctionSignature>`** (`DataBinder.Linkage.cs` — Name, Returning
   DataItem, LinkageFormals), not name→CallUnit: `CallUnit` is emitter-private and the binder needs only the
   bound signature. FUNCTION-ID units get `Recursive = true` structurally (§9.4 :12529 — always recursive).
3. **GR12 dispatch precedence.** §12.3.8.2 GR12 (:14885) makes a REPOSITORY-declared name refer to the USER
   function "and not to an intrinsic function of the same name" (the spec's factorial example :43651) — so the
   user-function check runs BEFORE `IntrinsicCatalog.TryGet`, not on its miss as the design sketched.
   `DataBinder.UserFunctionNames` collects the non-INTRINSIC FUNCTION specifiers (the M2-UDF-4 named leg).
4. **The content-arg "temp of the formal's PIC" is realized by the runtime ABI, not a bind-time temp:**
   literal/arith args ride the existing `BoundCallArg` value forms; `CobolArgAdapt.Num/Text` conform the cell
   to the callee's profile (same-scale cells alias; a scale difference gets the rescaling truncation view) —
   observably the §14.2.3 GR9 copy-in. ~~Header BY VALUE formals (§14.2.3 GR10) are not modeled (LinkageFormal carries
   no mode); follow-up with the program-CALL header modes.~~ **CLOSED P10 Step 10 (2026-07-16):** the §14.2.2
   using-phrase parses per-parameter, `LinkageFormal.ByValue` threads the §14.2.3 GR4 transitivity, and the
   callee adopts a GR10 DETACHED value-copy cell (`CobolArgAdapt.NumValue`/`TextValue`, copy-out skipped) on
   BOTH activation paths; SR2 = COBOLNET1553, §8.4.3.2.3 SR10 = COBOLNET1554.
5. **The pending-call list lives on StatementBinder** (`_udfPendingCalls`), not DataBinder: registration
   happens inside the binder itself (property ops needed DataBinder only because ReferenceResolver registers
   them). The UDF wrap is the INNER sequence at the BindStatement chokepoint (before `OoWrapPropertyOps`), so
   a property-reference argument's GET still precedes the activation consuming its temp.
6. **A per-iteration re-evaluation guard the design missed:** a once-hoisted activation cannot honor a
   PERFORM UNTIL/VARYING condition (or FROM/BY operand) or SEARCH WHEN condition, which re-evaluate per
   iteration (§14.9.28/§14.9.37) — COBOLNET1509 loud, never a stale-temp loop. Body statements are safe
   (they drain their own suffix). **P10 Step 10 (2026-07-16) LIFTED the guard for every CONDITION window**
   (`BoundUdfEvaluated` per-evaluation attach, rendered as an IIFE — §8.4.3.2.4 GR1/GR6a); 1509 remains only
   for a VARYING BY operand, an AFTER-level FROM, and an EVALUATE selection subject.
7. **Temp synthesis generalized:** `OoCreatePropertyTemp` now delegates to the ONE `CreateCompilerTemp`
   (feedback_singular_pattern); the UDF result temp is `CreateCompilerTemp(returning, "__FNRES-", "__fnres", name)`.
8. **Diagnostics band:** 1501 (+ a GR12 hint when the group defines the FUNCTION-ID), 1505 declared-but-undefined
   (the M2-UDF-3 prototype gap, also class-unit references), 1506 arity (§14.8.2 positional; empty parens are a
   §8.4.3.2 parse error by design), 1507 function without PD RETURNING (§14.2 :23666, checked once per unit even
   uncalled), 1508 duplicate FUNCTION-ID, 1509 re-evaluation guard.
9. **Residue (named, staged):** function prototypes/cross-assembly locate (UDF-3 → EC-FUNCTION-NOT-FOUND
   surface), ALL INTRINSIC semantic bind + the FUNCTION-keyword-omitted reference form §8.4.3.2 SR2 (UDF-4),
   EXIT FUNCTION (M2-PROC-6 leg — now unblocked, needs the in-function placement flag), UDF references from
   class-unit methods, ~~per-evaluation activation (1509)~~ (CLOSED P10 Step 10 for condition windows; 1509
   narrowed to VARYING BY / AFTER-level FROM / EVALUATE subjects), ~~BY VALUE header formals~~ (CLOSED P10
   Step 10), §14.8.2.3 static description-conformance for reference args (runtime-adapted today, the
   program-CALL posture).

#### M2-UDF-1 — the ADVERSARIAL REVIEW WAVE (same change set; 28 raw findings → 24 confirmed → fixed/staged/documented)

The 4-lens find→2-skeptic-verify workflow (wf_e38982d1-0d2) over the landed diff confirmed 24 findings
(4 refuted). Disposition, all in the same change set:

**Fixed:**
- **StoreAsImage clone desync (major).** `StoreAsImage` is still mutable while procedure bodies bind (a
  ref-mod store / non-digit figurative MOVE in the callee flips its RETURNING item AFTER a caller's temp
  cloned it) — the two activation-boundary sides then disagree on the carrier and `StoreReturn(long,string)`
  silently drops a non-digit image. Cured structurally: `CreateCompilerTemp` records every (temp, model)
  pair (`DataBinder.CompilerTempClones`) and the run-unit emitter re-syncs `StoreAsImage` after ALL
  procedure binds — property-reference temps ride the same cure.
- **EcWrap sequence transparency (major).** The family selection switched on the bound node's TYPE, so a
  hoisted-activation `BoundSequence` matched nothing and the carrying statement silently LOST its >>TURN
  checking (a hole the property-op sequence had since DEVLOG 607). `QueryFor` now recurses into sequence
  steps: the carrying statement keeps its families, each hoisted CALL contributes EC-PROGRAM, duplicates
  dedup.
- **ContainsNextSentence sequence transparency (major).** A NEXT SENTENCE inside a UDF/property-carrying
  statement was invisible to the label machinery — `BoundSequence` arm added.
- **§8.8.4.13 short-circuit + EVALUATE over-evaluation (major).** Rule 1 terminates a hierarchical level as
  soon as its truth value is determined; rule 2 evaluates functions "if and when the conditions containing
  them are evaluated" — a hoisted activation in a NON-FIRST AND/OR operand (XOR exempt) or in EVALUATE
  selection (whose subjects this backend's chained lowering re-renders per WHEN) would over-evaluate.
  Guarded loud: `UdfGuardConditionalOperand` at BindFlatSequence + `BoundEvaluate` in the 1509 shape check.
- **§12.3.4 GR1 configuration inheritance (major).** A contained program (which cannot own a CONFIGURATION
  SECTION, §12.3.3 SR1) now inherits the containers' `UserFunctionNames` — conforming references bound,
  the misleading add-a-REPOSITORY hint can no longer fire there. (The same inheritance for PROPERTY
  specifiers is a pre-existing hole, noted for the OO track.)
- **§8.4.6.6 self-name (major).** A function's OWN name is referable with no repository entry
  (self-recursion; §12.3.8 GR11 makes a present self-entry a no-op) — `UdfSelfName` threaded per unit;
  the udf_recursion golden (5! = 120 through five nested activations) proves it end-to-end.
- **Non-numeric RETURNING mis-carry (critical) — RESOLVED (P10 Step 9).** The result reads through
  `BoundNumRef`, but every operand chokepoint maps it through the ONE `IntrinsicBinder.OperandOf` to a
  `BoundFieldOperand` whose `Place.Item` IS the cloned RETURNING description (§8.4.3.2.4 GR1) — so an
  alphanumeric/national result compares as text under its category, a numeric-edited result carries its
  mask image, and a GROUP RETURNING deep-clones its subtree into the temp (the unregistered
  `CloneTempNode` — a temp's subordinates are never referenceable, §8.4.3.2.3 SR1) and crosses the CALL
  boundary as its AsImage/FromImage character image. Delivery is the existing string ABI
  (`CobolArgAdapt.StoreReturn(string)`). The relation chokepoint (`ComparisonOperandOf`) routes its
  computed fallback through `OperandOf` for exactly this. The staged-loud COBOLNET1510 shapes (FLOAT, BOOLEAN,
  pointer / object / index classes, strong-typed / REDEFINES / variable-length groups) were all DISCHARGED by
  kb/Work PB1419 — every RETURNING category is carried, the boolean channel reads a boolean-returning user
  function, and the residue list is deleted. Golden: `udf_returning_categories`.
- **Honest argument diagnostics (minor).** A ref-mod/figurative/unresolvable argument now reports ITS
  actual staged shape in COBOLNET1506, never a message claiming a legal form is illegal.
- **§8.4.3.2.4 mis-citation (minor).** Every "§8.4.3.2.6" citation corrected (the GR1 temp rule and GR2/GR5/
  GR6 all live under §8.4.3.2.4 General rules).
- **Coverage (majors on the tests lens):** the 0900 BINDER gate now has a caller-only 85 witness (the
  whole-source matrix row's 85 leg trips the {is2002()}? PD-header RETURNING parse hint first); 1508 and
  the SEARCH/VARYING/EVALUATE/short-circuit 1509 legs and 1510 both categories gained facts; runtime
  coverage grew two goldens — udf_recursion and udf_nested_args (sibling activations, UDF-in-UDF,
  intrinsic-in-UDF, and the GR5a BY REFERENCE argument mutation visible in the caller: A=0005).

**Documented deviations (deliberate, cited):**
- **§12.3.8.3 SR10 forward reference without a prototype.** SR10 admits a repository FUNCTION specifier only
  naming (a) a function PROTOTYPE in the group, (b) a definition specified PREVIOUSLY, or (c) an
  external-repository entry. The whole-source corpus places callers FIRST — strictly conforming spelling
  needs `IS PROTOTYPE`, which is M2-UDF-3 (NOT-STARTED). Accepting the in-group forward DEFINITION is a
  deliberate, documented leniency (GnuCOBOL-compatible); the SR10 ordering diagnostic lands WITH prototypes,
  when the conforming spelling becomes available.
- **§14.6.2.3.2/.3 static-data state under the recursive model.** Functions ride the pre-existing D3/D4
  registry model (`Initial || Recursive` ⇒ fresh instance per activation), so a function's WORKING-STORAGE
  re-initializes per activation, where the spec keeps static data last-used after the FIRST activation.
  This deviation PREDATES UDFs (any PROGRAM-ID … RECURSIVE unit has it) and is observable only when WS
  carries state across activations; the conforming split (shared static WS + per-activation
  automatic/LOCAL-STORAGE/formals) is a named follow-up on the interprogram track.

### M2-UDF-3 + M2-UDF-4 — DECISION-COMPLETE DESIGN (recon 2026-07-06, four parallel readers; grammar pre-authorized; M2-UDF-3 IMPLEMENTED — see AS-BUILT)

> **M2-UDF-3 AS BUILT (DEVLOG 624, 2026-07-06; landed exactly on the design's D1/D2/D3/D4).** The design's seams
> held with no material deviation. Notes: (1) the `CallProgram` EC-name distinction is a trailing optional param
> `notFoundEc` (default `"EC-PROGRAM-NOT-FOUND"`), set by `CallEmitCall` when `BoundCallProgram.IsFunction`; (2)
> `RunMain` targets the first top-level PROGRAM unit (`units.FirstOrDefault(u => u.Parent is null && !u.IsFunction)`)
> — a function/prototype-only module emits `Register()` and NO `RunMain` (a callable library); (3) the §10.6.2 SR3
> pair check is COBOLNET1513 (arity only — full §8.13 conformance staged); (4) the prototype-at-85 clean 0900 is a
> new `EditionGateHints.FunctionPrototype` gate recognizing the `IS`/`PROTOTYPE` token inside `functionIdParagraph`.
> **M2-UDF-4 AS BUILT (DEVLOG 626, 2026-07-06; landed on the design's D2).** `FUNCTION ALL INTRINSIC` (GR14) and
> `FUNCTION name INTRINSIC` are collected into `DataBinder.RepositoryAllIntrinsic`/`RepositoryIntrinsics` (and
> inherited into contained programs alongside `UserFunctionNames`, §12.3.4 GR1). The keyword-omitted form landed
> exactly as D2 prescribed — **bind-side, no grammar change**: `KeywordOmittedFunction(dref)` re-routes `name(args)`
> to `BindIntrinsicCore` at the TWO `dataReference→Bound*` chokepoints (`RefExpr` / `FieldOperand`, each documented
> as "the ONE mapping used by every path") when the head is a repository intrinsic / ALL / user-function /
> prototype AND is NOT a declared data item (the data-item-wins guard — a table named like an intrinsic stays a
> subscript; SR13 enforcement is staged). Gated `< 2002 ⇒ inert`, so the 85/NIST surface is byte-invariant (the
> greenfield NIST differential confirms). **NO version-matrix row**: keyword omission is a RUNTIME-observable
> behavior (below 2002 `MAX(a,b)` degrades to an undefined-reference §1.4 loud-fail, not a compile-time
> rejection), so it does not fit the compile-time introduction-gating matrix model — the golden + unit tests are
> its conformance coverage. **Track (c) — the UDF subsystem — is now COMPLETE** (invocation 615, EXIT FUNCTION
> 616, prototypes/cross-assembly 624, REPOSITORY specifiers + keyword omission 626); residue is the named
> staged-loud items only. The runtime cross-assembly leg is proven in `InterProgramFileDifferentialTests` (present
> → P=000049, absent → EC-FUNCTION-NOT-FOUND).

> Scope: COMPLETE the user-defined-function subsystem (track (c) residue) to spec. Two catalog items:
> **M2-UDF-3** = separate-compilation function **prototypes** (`FUNCTION-ID … IS PROTOTYPE`, §11.5 Format 2) +
> cross-assembly function resolution (→ **EC-FUNCTION-NOT-FOUND** on absence) + the §12.3.8 GR11/SR10
> forward-reference resolution. **M2-UDF-4** = REPOSITORY FUNCTION-specifier binding — `FUNCTION ALL INTRINSIC`
> (§12.3.8 GR14) + the §8.4.3.2 SR2 **FUNCTION-keyword-omitted** reference form. The in-group inline invocation
> (`FUNCTION user-name(args)`) already LANDED (DEVLOG 615/616); this closes the residue named there.

- **Spec (ISO/IEC 1989:2023; `specs/ISO_COBOL.md` :line anchors, all read directly this wave).**
  - **Prototype unit.** §11.5 FUNCTION-ID Format 2 (:13127) `FUNCTION-ID. function-prototype-name-1 [AS literal-1] IS PROTOTYPE.` (GR2 :13143 — literal-1 is the externalized name). §10.6.2 SR4 (:12852) makes a prototype a **signature-only** unit: (a) no ARITHMETIC clause, (b) no object-computer, (c) SPECIAL-NAMES limited, (d) **no input-output section**, (e) **data division may contain only a LINKAGE SECTION**, (f) **procedure division shall contain only a procedure division header** (no statements). §10.6.2 SR1 (:12837) — function/program prototypes **shall precede all other source units** in the group. §10.6.2 SR3 (:12850) — an in-group definition + prototype with the same externalized name **shall have the same signature**. §10.6.3 GR1 (:12871) — compiling a prototype generates the external-repository info (§8.13 :11117; :11135 "whether from a prototype or a definition, the signature info is the same").
  - **Resolution (deterministic — NOT implementor-defined).** §12.3.8 GR11 (:14869): a `FUNCTION function-prototype-name` specifier resolves in order — **(a)** :14871 same externalized name as a function **definition specified PREVIOUSLY in the group** ⇒ details from that definition (repository ignored); **(b)** :14875 else a **prototype definition in the group** ⇒ details from the prototype, the activated function is the same-externalized-name one; **(c)** :14883 else the **external repository** entry. GR12 (:14885) — within the environment-division scope a reference to the prototype-name is to the **user function, not a same-named intrinsic**. SR10 (:14780) — the specifier name shall be (a) an in-group prototype, (b) a **previously**-specified in-group definition, or (c) an external-repository entry. SR11 (:14786) — a self-referential specifier is ignored (§8.4.6.6 self-name). §8.4.6.7 (:7938) — a user-function-name is referable in the REPOSITORY of any element that **follows** that definition, and (if the repository is updated) in any **subsequently-compiled** unit.
  - **Runtime locate + EC.** §8.4.3.2.4 GR6b (:6997) — for a function-prototype-name/pointer the runtime **locates** the function (§8.4.6/§8.4.6.6 + §12.3.8); **if not found, EC-FUNCTION-NOT-FOUND is set, the function is not activated**. Table 13 (:24702) — **EC-FUNCTION-NOT-FOUND is `Fatal`** ("Function not found or function pointer does not point to a function"). GR1 (:6963) — the caller's result temp takes the description of the LINKAGE item named in the prototype's RETURNING phrase. GR3 (:6973) — the activated function is identified per §12.3.8 (GR11 a/b/c).
  - **Keyword omission.** §8.4.3.2.2 (:6887) — the general format is `[ FUNCTION ] { function-pointer-name | function-prototype-name | intrinsic-function-name } [(args…)]` (FUNCTION optional in the syntax; SRs govern when it may drop). SR2 (:6902) — FUNCTION **may be omitted** iff intrinsic-function-name-1 **or ALL** is in the REPOSITORY paragraph, **or** a function-prototype-name/function-pointer-name is specified; otherwise FUNCTION is required. SR6 (:6918) — **a `(` immediately following a function-prototype-name-1 or intrinsic-function-name-1 that permits arguments is ALWAYS the argument list** (the disambiguator). SR3 (:6912) — function-prototype-name-1 shall be the containing definition's user-function-name or a REPOSITORY prototype.
  - **ALL INTRINSIC.** §12.3.8 GR14 (:14889) — `ALL` = as if every §8.11 intrinsic-function-name were listed (minus COBOL-WORDS-undefined; SUBSTITUTE/EQUATE edits the list). GR13 (:14887) — a REPOSITORY intrinsic name is usable **without** FUNCTION. SR12 (:14788)/SR13 (:14790) — a named / (under ALL) **any** intrinsic-function-name shall **not** be a user-defined word within the REPOSITORY scope. §9.4 (:12529) — a UDF is always implicitly RECURSIVE; **FUNCTION-ID carries NO explicit RECURSIVE keyword** (unlike PROGRAM-ID) — the only FUNCTION-ID options are `AS literal` and `IS PROTOTYPE`.

- **Editions.** The whole subsystem is a **COBOL-2002 introduction** (prototypes, REPOSITORY FUNCTION specifiers, keyword omission), carried unchanged through 2014/2023 (`VERSION_CHANGE_REFERENCE.md` has no 85→2002 rows — derive from the 2002 standard; the one documented 2014→2023 delta touching it is Annex E item 13 :49178, seven **new** intrinsics prohibited as user words inside `ALL INTRINSIC` scope — the ALL reserved-word set edition-tracks §8.11, but the FEATURE is 2002). `PROTOTYPE` §8.9 row already present (`ReservedWords.Table.cs:340`, `r85=false / 2002,14,23=true`) — no `reserved-words.json` regen needed. Gate the new grammar `{is2002()}?`.

- **⛔ SYNTHESIS DECISIONS.**
  - **D1 — the RUNTIME half of cross-assembly UDF-3 is FREE (reuse the Fix-G sibling probe).** A FUNCTION-ID unit already emits as `_PRG_<name> : ICobolProgram`, registers under its function name via the public `__CobolModule.Register()`, and delivers RETURNING via `CobolArgAdapt.StoreReturn`. A `FUNCTION name(args)` reference already lowers to `BoundCallProgram(name,…,tempPlace,…)` → `ProgramRegistry.CallProgram` → `ResolveVisible` → `ProbeSiblingModule` (loads `<name>.dll` from `AppContext.BaseDirectory`, runs its registrar). This is byte-identical to the DEVLOG-575 cross-assembly CALL (`InterProgramFileDifferentialTests` Fix-G, :303/:320). **So UDF-3 is entirely compile-time + one EC-name distinction:** (i) a signature source for the out-of-group target (a prototype), (ii) a no-body emit filter for prototype units, (iii) `EC-FUNCTION-NOT-FOUND` (not `-PROGRAM-`) on a UDF miss.
  - **D2 — the keyword-omitted form (SR2/SR6) is resolved at BIND, NOT in the grammar.** `name(args)` already parses as a subscripted `dataReference` (`primaryExpression`→`dataReference`→`subscriptPart`). Adding a keyword-omitted `functionCall` alternative would be **structurally identical** to a subscripted data reference — an irreducible ANTLR ambiguity; and the `boolExprAhead()` predicate does **NOT** transfer (that discriminator is a downstream token; function-vs-subscript is **semantic** — repository membership). Per SR6, when the head name of a `dataReference(subscript)` resolves to a REPOSITORY-declared user function/prototype or an in-effect intrinsic (rather than a data item), the reference resolver **reinterprets** the node as a function call. **Zero DFA hazard** — honors the DEVLOG-621 "never restructure a shared core rule" lesson. (`primaryExpression`/`dataReference`/`dataReferenceSuffix`/`subscriptPart` stay UNTOUCHED — the one move to avoid.)
  - **D3 — KEEP the DEVLOG-615 forward-bare-definition leniency (no regression); prototypes are the CONFORMING alternative.** SR10(b) admits a bare definition only if specified **previously**; the udf corpus places callers first (forward bare definitions) — a hard SR10 error would regress `udf_invocation`/`udf_inline_expression`/`udf_value_args`/`udf_recursion`/`udf_nested_args`. The leniency stays documented (GnuCOBOL-compatible); `IS PROTOTYPE` provides the strictly-conforming spelling. The **strict** SR10 ordering DIAGNOSTIC (forward reference lacking a prototype under `--std … ` non-permissive) is **staged residue** — a later strictness pass, not this change set (avoids a mass corpus re-spelling now).
  - **D4 — prototype+definition coexistence is NOT a duplicate.** `CallBuildUserFunctionTable`'s `TryAdd` first-wins would false-trip COBOLNET1508 because SR1 puts the prototype FIRST. The as-built must treat a same-name (prototype, definition) pair as ONE function: register the **definition's** signature (GR11a authoritative), ignore the prototype's; only (definition, definition) is 1508. A basic SR3 signature check (arity + RETURNING category) between the pair is a documented light check (full §8.13 signature conformance = staged).

- **Grammar plan (pre-authorized; ALL edits ADDITIVE + unique-leading-token — the 85/NIST surface stays bit-identical).**
  1. **Lexer** (`CobolLexer.g4`, keyword band): ADD `PROTOTYPE : 'PROTOTYPE' ;`. ADD `PROTOTYPE` to `_dataNameTokens` (:30–69) so `01 PROTOTYPE PIC X.`/`PROTOTYPE(1)` still lex as a user data-name at 85 (the FACTORY/OVERRIDE/GET precedent).
  2. **`cobolWord`** (`CobolParserCore.g4:25–112`): ADD `PROTOTYPE` (user word at 85, funnel-0901'd ≥2002 via the existing table row).
  3. **`functionIdParagraph`** (`CobolParserCore.g4:186`): `FUNCTION_ID DOT programName ({is2002()}? IS? PROTOTYPE)? DOT` — a LOCAL rule reached only from `identificationBody` (NOT a shared expression/statement core), optional tail with a unique leading token `PROTOTYPE`, hard-`DOT`-bounded — the same shape as `programIdParagraph`'s existing `(IS? programIdAttributes PROGRAM?)?` tail. **LOW hazard.**
  4. **`FUNCTION ALL INTRINSIC` / `FUNCTION functionName INTRINSIC?`** — already parse (`repositoryEntry:449–450`). **No grammar change** for UDF-4; binder-only.
  5. **Keyword-omitted reference** — **No grammar change** (D2, bind-side).
  6. **`AS literal`** on FUNCTION-ID / REPOSITORY (the externalized-name phrase) — **LANDED**: FUNCTION-ID's `AS literal-1` with PB303 (the shared `externalizedNamePhrase`), and every REPOSITORY specifier's `[ AS literal-n ]` with kb/Work PB974 — a `FUNCTION name AS literal-5` specifier resolves to the function whose externalized name is literal-5 (`BinderDriver.UserFunctionsOf`, §12.3.8.4 GR11 NOTE 2). Without AS the externalized name is the function-prototype-name.
  7. **Regen both OSes** (`Generated/` is build output); **FULL `scripts/guard.sh`** on the grammar-touching commit (85 surface must be byte-invariant — PROTOTYPE behaves as a user word at `--nist`).

- **Reserved-word funnel** (`EditionValidator.cs`): ADD `CobolLexer.PROTOTYPE` to `CheckedTokenTypes` (:268–295) — position-blind-safe (PROTOTYPE appears only in the FUNCTION-ID slot at 2002+, never a name slot); the table row (:340) drives 0901. No `reserved-words.json` / drift-test change.

- **Parse-side + CallUnit** (`CSharpEmitter.Call.cs`): `CallUnit` gains `bool IsPrototype` (:30–51). `CallMakeUnit` (:221) sets `isPrototype = fid?.PROTOTYPE() is not null` (a prototype is `IsFunction=true, IsPrototype=true`). A `{is2002()}?` gate already blocks the tail below 2002, so `IsPrototype` at <2002 is unreachable.

- **Binder / emitter seams.**
  - **Signature registration** — `CallBuildUserFunctionTable` (:397): still walks `IsFunction` units, but (D4) FIRST partition definitions vs prototypes; for each name register the DEFINITION's signature if one exists, else the sole PROTOTYPE's; a (prototype, definition) pair is one entry (no 1508); (definition, definition) → 1508. A prototype without a PD RETURNING is 1507 (a signature must carry a result — §14.2). Optional light SR3 check (arity/returning-category mismatch between an in-group pair) → a new `COBOLNET1513` (message-differentiated; full §8.13 conformance staged).
  - **No-body emit filter** — `CallEmitRunUnit` (:175–178): skip `IsPrototype` units in `CallEmitProgramClass` (a prototype has no body — emitting it would create an empty program that shadows the real definition). `CallEmitEntryWrapper` (:673): skip `IsPrototype` units in the `Register()` loop (they must NOT register — the separately-compiled definition registers itself; an in-group same-name definition registers normally). **RunMain** (:693): target the first **top-level non-prototype PROGRAM** unit (`units.FirstOrDefault(u => !u.IsPrototype && !u.IsFunction && u.Parent is null)`), not `units[0]` — a prototype precedes everything (SR1) so `units[0]` may be a prototype; a program-less module (a function/prototype library) emits `Register()` + a Main that does NOT call RunMain (it is only ever CALLed via the sibling probe). No regression: every existing golden has a PROGRAM-ID first.
  - **EC-FUNCTION-NOT-FOUND** — `BoundCallProgram` (Bound/BoundTree.cs) gains `bool IsFunction = false`; `UdfBindCall` (:138) constructs it `true`. `CallEmitCall` (:713) passes a `notFoundEc` argument; `ProgramRegistry.CallProgram` (:417) gains an overload/param `string notFoundEc = "EC-PROGRAM-NOT-FOUND"` and stamps it on the `CobolCallException` (:421). EC-FUNCTION-NOT-FOUND is already in the catalog (Fatal, `ExceptionCatalog.cs:107`) — no catalog change; being Fatal it terminates the run unit loudly, which the absent-function test asserts. (`>>TURN EC-FUNCTION` call-site catching = a mirror of `ProgramNames`/`CallEmitProgramEcCatch` — STAGED residue; a fatal raise is spec-correct for the first cut.)
  - **1505 rework** — `UdfBindCall` (:61–68): the "no FUNCTION-ID bound" branch now fires ONLY when the name has neither an in-group definition NOR a prototype NOR (future) an external repository entry — a genuine unresolved-function error (reworded off "not implemented"). A prototype-declared name resolves (its signature drives the temp + args; runtime does the locate).
  - **UDF-4 ALL INTRINSIC + named-intrinsic** — `DataBinder.cs` REPOSITORY loop (:140–148): ADD legs collecting `bool RepositoryAllIntrinsic` (the `FUNCTION ALL INTRINSIC` alt — `re.ALL() is not null && re.INTRINSIC() is not null`) and `HashSet<string> RepositoryIntrinsics` (the `FUNCTION name INTRINSIC` alt — `re.INTRINSIC() is not null && re.functionName() is {}`). Inherit into contained programs like `UserFunctionNames` (:329, §12.3.4 GR1). SR12/SR13 (a named/all intrinsic name used as a user word in scope) → a `COBOLNET1513`-band check at data-name declaration (light: flag a WS/LINKAGE name equal to a repository intrinsic; full §8.11-set enumeration for ALL is a documented light form — enumerate the catalog's known names).
  - **Keyword-omitted dispatch (D2)** — the reference resolver's data-name binding (`ReferenceResolver` / `StatementBinder` operand path): when a `dataReference` head name does **not** resolve to a data item BUT is in `UserFunctionNames` / a prototype / (`RepositoryAllIntrinsic` || `RepositoryIntrinsics`.Contains) an intrinsic, and a subscript/`(args)` follows, re-route to `BindIntrinsicCore(name, argTokens)` (the SAME chokepoint the FUNCTION-keyword form uses, `StatementBinder.Intrinsics.cs:54`). GR12 precedence (user shadows intrinsic) already lives there (:61). SR6 makes the `(` unambiguously the arg list. Guard: only when the name is NOT also a visible data item (a data item wins — the reference is a subscript, §8.4.3.2 is about names that ARE functions).

- **Diagnostics.** Reuse **1505** (reworded: unresolved function — neither defined, prototyped, nor in the external repository). **NEW `COBOLNET1513`** (next free — 1501–1512 taken; 1512 = the file-sharing SR band): message-differentiated for (a) an in-group prototype/definition signature mismatch (§10.6.2 SR3), (b) a REPOSITORY intrinsic-name used as a user-defined word (§12.3.8 SR12/SR13). Runtime **EC-FUNCTION-NOT-FOUND** (Fatal) on a UDF locate miss (§8.4.3.2.4 GR6b). Introduction gate = the existing `user-function-invocation-2002` row (prototypes/keyword-omission ride the same 2002 gate) + a new `function-prototype-2002` registry row for the `IS PROTOTYPE` construct.

- **Tests / goldens / docs (SAME change set).**
  - **NEW golden `tests/conformance/2002/udf_prototype.cob` + `.out`** (manifest `enabled`, alphabetical; `GreenfieldOnly` — the frozen legacy cannot parse `IS PROTOTYPE`): a single source, SR1-ordered — `FUNCTION-ID SQUARER IS PROTOTYPE.` (LINKAGE `L-X`/`L-R` + `PROCEDURE DIVISION USING L-X RETURNING L-R.` header only) FIRST, then `PROGRAM-ID` caller `REPOSITORY. FUNCTION SQUARER.` invoking `COMPUTE R = FUNCTION SQUARER(WS-N)` (+ the keyword-omitted `MOVE SQUARER(WS-N) TO R` for UDF-4), then `FUNCTION-ID SQUARER.` DEFINITION (`COMPUTE L-R = L-X * L-X`). Proves prototype parse + no-body emit + GR11(a/b) resolution + keyword omission. Expected `.out` derived (e.g. `P=0049 / K=0049` for WS-N=7).
  - **NEW golden `tests/conformance/2002/udf_all_intrinsic.cob` + `.out`** (`GreenfieldOnly` if legacy can't bind ALL INTRINSIC keyword-omission): `REPOSITORY. FUNCTION ALL INTRINSIC.` then `COMPUTE R = MAX(A, B)` / `MIN`/`MOD` without the FUNCTION keyword (SR2). Expected derived.
  - **NEW cross-assembly test** (`InterProgramFileDifferentialTests` Fix-G style, or a new `UdfCrossAssemblyTests`): `CompileTo` a caller+prototype module and a separately-compiled `FUNCTION-ID` definition module into one dir; `CutRunner.Run` the caller → assert the RETURNING crossed the assembly boundary; a second test with the definition module ABSENT → assert the failure detail contains **`EC-FUNCTION-NOT-FOUND`** (the §8.4.3.2.4 GR6b Fatal surface — mirrors `Call_AbsentSiblingModule_StillNotFound`).
  - **`UdfInvocationTests`** additions: prototype-provides-signature; prototype+definition-not-duplicate; prototype-no-RETURNING→1507; keyword-omitted intrinsic + user-function; a data-name shadowing an intrinsic still binds as data (no false function re-route); ALL-INTRINSIC/named collection.
  - **Matrix / registry:** `function-prototype-2002` row (IntroducedIn 2002) in `ConstructDialectStatus.cs` + `constructs.json` mirror (0900 at 85). Legacy exclusions in `ConformanceTests.cs` for the new goldens (DEVLOG 618 rule).
  - **Same-change-set docs:** this file's M2-UDF-3 (NOT-STARTED→LANDED) + M2-UDF-4 (PARTIAL→LANDED-or-residue) rows; the stale grammar comments (`CobolParserCore.g4:182–185`, `:438–443` — "later slice / not yet bound"); the DEVLOG entry; the resume-prompt STATE banner; and `COBOLNET_INTERPROGRAM_DESIGN.md`'s function section (the prototype-signature loader + the D1 cross-assembly-via-sibling-probe posture).

- **Implementation order (small green commits; FULL `scripts/guard.sh` on the grammar commit).** **(1)** Lexer `PROTOTYPE` + `_dataNameTokens` + `cobolWord` + `CheckedTokenTypes` + `functionIdParagraph` tail + regen → greenfield battery + **FULL legacy guard** (85 surface invariant). **(2)** `IsPrototype` on CallUnit + `CallBuildUserFunctionTable` dedupe + no-body emit filter + RunMain first-program + `BoundCallProgram.IsFunction` + `CallProgram` EC-name + 1505 rework → battery. **(3)** `udf_prototype` golden + `GreenfieldOnly` + cross-assembly tests (present + absent→EC-FUNCTION-NOT-FOUND) + registry/matrix rows → battery + **legacy conformance**. **(4)** UDF-4: `DataBinder` ALL-INTRINSIC/named collection + inheritance + the keyword-omitted bind-side re-route + SR12/SR13 1513 check + `udf_all_intrinsic` golden + tests → battery. **(5)** Docs + DEVLOG + resume-prompt + memory + commit/push (feedback_fully_autonomous_push); confirm CI.

- **STAGED RESIDUE (named, loud/documented — each with its §).** (1) `AS literal` externalized-name phrase (FUNCTION-ID + REPOSITORY) — landed (PB303 + PB974; see item 6). (2) The **strict SR10 ordering diagnostic** (forward reference lacking a prototype) — the DEVLOG-615 leniency stays; a later strictness pass. (3) **`>>TURN EC-FUNCTION` call-site catching** (a `FunctionNames`/`CallEmitProgramEcCatch` mirror) — a fatal raise is spec-correct for now (EC-FUNCTION-NOT-FOUND is Fatal). (4) **Full §8.13 external-repository signature conformance** (SR3 beyond arity/returning-category; the repository update/flag mechanism §8.13 :11137) — light in-group check only. (5) **Non-numeric/group/float RETURNING** for prototypes — inherits the DEVLOG-615 COBOLNET1510 restriction. (6) **EXPANDS / generic class/interface REPOSITORY specifiers** (§12.3.8) — out of the UDF track (an OO-track item). (7) `FUNCTION ALL INTRINSIC` §8.11 exhaustive edition-set enumeration for SR13 — light form over the implemented catalog names.

- **ANCHOR LIST.** Grammar: `CobolLexer.g4` keyword band + `_dataNameTokens` :30–69; `CobolParserCore.g4` `cobolWord` :25–112, `functionIdParagraph` :186–188, `identificationBody` :177–180, `repositoryEntry` :448–454, `compilationGroup`/`programUnit` :143–167. Validation: `EditionValidator.cs` `CheckedTokenTypes` :268–295, `VisitCobolWord` :309; `ReservedWords.Table.cs:340` (PROTOTYPE row); `ConstructDialectStatus.cs`; `constructs.json`. Binder: `StatementBinder.Udf.cs` `UdfBindCall` :55–140 (1505 :63, temp :134, register :138), `StatementBinder.Intrinsics.cs` `BindIntrinsicCore` :54–68 (GR12 gate :61); `DataBinder.cs` REPOSITORY loop :140–148, `UserFunctionNames` :83, inheritance :329; `DataBinder.Linkage.cs` `UserFunctionSignature` :37, `LinkageReturning` :58/:148. Emit: `CSharpEmitter.Call.cs` `CallMakeUnit` :221–267 (:230/:256/:265), `CallBuildUserFunctionTable` :397–414, `CallEmitRunUnit` :82–180 (:103/:175–178), `CallEmitEntryWrapper` :673–703 (:677/:693), `CallEmitCall` :713–742 (:722/:725), `CallUnit` :30–51. Runtime: `ProgramRegistry.cs` `CallProgram` :417–458 (:421 throw), `ResolveVisible` :511–545, `ProbeSiblingModule` :552–574; `ExceptionCatalog.cs:107` (EC-FUNCTION-NOT-FOUND Fatal). Tests: `InterProgramFileDifferentialTests.cs` Fix-G :262–331, `ConformanceTests.cs` GreenfieldOnly :66–115, `manifest.json`, `UdfInvocationTests.cs`. Spec: :6887/:6902/:6912/:6918/:6963/:6973/:6997/:7546/:7938/:11117/:11135/:12529/:12837/:12850/:12852/:12871/:13127/:13143/:14780/:14786/:14788/:14790/:14869/:14885/:14887/:14889/:24702/:49178. Legacy: `CobolSharp.Compiler.csproj:25`.

## M2-DATA — new data types

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-DATA-1 | USAGE BINARY-CHAR/SHORT/LONG/DOUBLE [SIGNED\|UNSIGNED] | done | **LANDED (DEVLOG 614)** | PICTURE-less native 1/2/4/8-byte two's-complement integers (SIGNED default / UNSIGNED widens) on the COMP-5 BinaryCapacity discipline: `PicInfo.BinaryItem` + `Usage.BinaryChar/Short/Long/Double` un-skeletoned; `CobolNum.WrapBinary`/`InBinaryRange` implement the byte-width wrap + SIZE-ERROR range check (was a documented stub); `binary_usage.cob` ENABLED byte-exact; PICTURE prohibited COBOLNET0870 (§13.16.3 SR8); +24 BinaryCapacityTests unit + BinaryUsageDataTests end-to-end + DataSkeleton/LoudGuard/VersionMatrix flips | none | Implied DISPLAY width 3/5/10/19·20 (GR21 implementor choice); 0900 below 2002. |
| M2-DATA-2 | USAGE FLOAT-SHORT/LONG/EXTENDED | done | STAGED-LOUD | PicInfo.cs:75-81 skeleton → 0899; ConstructDialectStatus 114-115 "Phase 6"; float_usage.cob PENDING | phase 6 | IEEE-float families deferred to Phase 6 (D16). |
| M2-DATA-3 | National data — USAGE NATIONAL + PIC N | done | **LANDED (Phase 4a track (a), 2026-07-05)** | One UTF-16 char per national position (D-N1) on the string substrate; SR13a implied usage; Table 16 MOVE legality (0819) incl. the sanctioned-narrowing reject; §8.8.4.2.9 ordinal comparison (no alphanumeric PCS); byte-surface guards (REDEFINES/cells/FD records/SORT keys → 0899, D-N2); N"…" literals at every funnel (0814 band); `national_data` ENABLED (N2A leg re-baselined per Table 16 + full-width §14.9.11.4 GR6 — LegacyDivergent) | none (residue in the design section) | national-edited LANDED 2026-09-12 (kb/Work PB492 - category National carrying an EditMask, D-N6; GR10's character-1 leg included); the national-form NUMERIC / NUMERIC-EDITED / BOOLEAN shapes of §13.18.60.3 SR12 LANDED 2026-09-13 (kb/Work PB646 — design D-N7: the DISPLAY twin's carrier and character image composed with the ONE national byte transform; the four COBOLNET0899 staging arms are gone and all five SR12 picture shapes are live); **P10 wave 2026-07-16: DISPLAY-OF/NATIONAL-OF (§15.26/§15.66) LANDED as Runtime rows (both argument forms — argument-2 is a substitution character in the 2023 text) and the non-Latin-1 N"…" guard LIFTED (full UTF-16 repertoire, D-N1; substitution + EC-DATA-CONVERSION via the ONE CONVERT translator; `national_intrinsics` ENABLED, COBOLNET1546 negatives)**; **P10 Step 11 (2026-07-16): the remaining -N legs LANDED — CHAR-NATIONAL §15.16 (`CharNational`, the native national PCS = UTF-16 order) + ORD-over-national §15.70.4 r2 (`char_national` ENABLED), EXCEPTION-FILE-N §15.29 / EXCEPTION-LOCATION-N §15.31 (`EcFunctions.FileN/LocationN` via the ONE `NationalOf` translator; `exception_file_n` ENABLED)**. |
| M2-DATA-4 | Boolean & bit — USAGE BIT + PIC 1 | done | **LANDED (Phase 4a track (a), 2026-07-05)** | One '0'/'1' char per boolean position for BOTH usages (GR14 R14, D-B1); zero-fill stores (§14.6.8.6) via the CobolString pad params; equality-only relations (0844, CheckedRelational); VALUE/level-88 validation (0898); figurative SR7 (0819); `boolean_data` ENABLED byte-exact | increment 2 = the B-AND/B-OR/B-XOR/B-NOT operator leg (design below; grammar grant 2026-07-05) | True bit-packing stays an optional residue (never required — R14). |
| M2-DATA-5 | Pointers & based addressing (POINTER/NULL/SET/BASED/ADDRESS OF) | partial | **LANDED (increments 1+2, DEVLOG 613/617)** | Increment 1: USAGE POINTER + SET TO NULL/pointer + [NOT] EQUAL. Increment 2: ADDRESS OF / BASED / SET ADDRESS OF / F10 arithmetic on the StorageCell+CellPointer window model (structural §8.8.4.2 equality; the deref bridge makes §13.18.5 GR3/GR4 loud); based_pointer/pointer_alloc/pointer_arith ENABLED byte-exact; 6 negative cases; CobolPtrTests ×6; 0869/0881 bands; set-address-2002 + pointer-arithmetic-2002 rows | none (residue named in the as-built) | CALL-boundary pointers, restricted `POINTER TO`, FUNCTION/PROGRAM-POINTER stay staged (M3-4 leg). |

### M2-DATA-5 / M2-PROC-5 — increment 2 (ADDRESS OF / BASED / SET ADDRESS OF / ALLOCATE-FREE) — DECISION-COMPLETE DESIGN (recon wave 2026-07-05; ready to implement)

> Scope: the three PENDING corpus programs (tests/conformance/2002/manifest.json:44-49) — `based_pointer`
> (`SET P TO ADDRESS OF X` / `SET ADDRESS OF B TO P` / read+write aliasing through an elementary BASED item;
> golden `B=HELLO`,`X=WORLD`), `pointer_alloc` (`ALLOCATE B` form-2 / `ALLOCATE 5 CHARACTERS RETURNING P`
> form-1 / rebase / `FREE P`; golden `B=HELLO`,`B2=WORLD`,`FREED=YES`), `pointer_arith` (`SET P UP BY 4` /
> `DOWN BY 2` byte-granular + a 1-byte BASED probe; golden `C0=A`,`C4=E`,`C2=C`). **No .g4 change**: both
> setAddressStatement alternatives (CobolParserCore.g4:1039-1042), allocate/free (:1047-1055, dispatcher-gated
> `{is2002()}?` :660-661), `SET … UP/DOWN BY` via setIndexStatement (:1074-1076), and `basedClause`
> (Core/CobolData.g4:240-242) all parse TODAY; edition gates are binder-side registry checks (below).

- **Spec (ISO 2023; specs/ISO_COBOL.md :line anchors).** ADDRESS OF §8.4.3.11 (:7443-7491 — GR1 a unique
  data-pointer containing the item's address; SR1 FILE/WS/LS/LINKAGE items legal; SR5 never a receiving operand,
  but the SET receiving form is "syntactical notation", D.9.2.1 :44419). BASED §13.18.5 (:17701-17729 — GR1
  template + implicit data-address pointer; GR2 initial NULL — ⚠ standard erratum: GR2 cites "SET GR 17, 18, 19"
  but the 2023 numbering puts Format 7 at GR12-13 :31600-31604, cite both in code; GR3 deref-while-NULL →
  **EC-DATA-PTR-NULL** Fatal :24669; GR4 invalid address → **EC-BOUND-PTR** Fatal :24643); §13.16 SR16 (:17261)
  level 1/77 in WS/LS/LINKAGE only; no REDEFINES (:17215) / no EXTERNAL (:17219) with BASED; lifecycle §8.6.5
  (:8789-8801 — `ADDRESS OF based-item` READS the implicit pointer :8791). SET F7 §14.9.39 (:31172-31175; SR18
  :31399 **the ADDRESS OF receiver shall be BASED** — the IBM non-BASED-linkage idiom is NOT ISO; GR12-13
  :31602-31604 the address VALUE is assigned — snapshot, never live-tracking). SET F10 (:31194-31199 —
  **pointer arithmetic IS ISO 2023, not a vendor extension**; SR23-24 :31424-31426; GR18 NULL →
  EC-DATA-PTR-NULL :31631; GR19 non-integer → EC-SIZE-ADDRESS Fatal :31633; GR20 moves by **bytes**, outside
  the implementor's range → EC-RANGE-PTR :31635). ALLOCATE §14.9.3 (:25936-26008 — SR1 data-name-1 BASED
  :25959; SR2 CHARACTERS ⇒ RETURNING required :25961; SR3 RETURNING is category data-pointer :25963; GR1 bytes,
  non-integer rounds UP :25972; GR2 ≤0 → NULL, **no EC** :25974; GR3 based size incl. ODO max :25976; GR4a/b
  :25978-25990; GR5 EC-STORAGE-NOT-AVAIL NF :25992-25998; GR6 INITIALIZED+CHARACTERS = binary zeros :26000;
  GR7 INITIALIZED+based = `INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT` :26002; GR8/9 otherwise
  undefined / pointer+object subordinates null :26004-26006; GR10 lifetime :26008). FREE §14.9.15 (:27485-27513
  — SR1 operands are data-pointers ONLY :27500, `FREE based-item` is a vendor extension the corpus does NOT
  use; GR1a start-of-allocation → release + null THE OPERAND only :27505-27511; GR1b NULL → no-op; GR1c else →
  **EC-STORAGE-NOT-ALLOC** NF :24856; GR2 per-operand sequencing :27513). Pointer relations §8.8.4.2
  (:9591-9599 equality-only, no ordering; :9772-9774 "equal if they reference the same address" —
  **STRUCTURAL** equality over (storage, offset), not carrier-object identity).
- **GOLDEN-vs-ISO AUDIT — no re-editioning, no owner conflict.** All three goldens are ISO-conformant as
  written: pointer_arith is SET Format 10 (standard — clears the "pointer arithmetic may be vendor" flag);
  pointer_alloc FREEs the RETURNING pointer P (the ISO data-pointer form, and `FREED=YES` = GR1a's operand-null);
  based_pointer's read/write aliasing = F7 GR12-13 + one shared storage. pointer_arith's re-`SET ADDRESS OF`
  after each step is consistent with GR13 snapshot semantics (the design implements snapshot; the corpus cannot
  distinguish, the spec decides). Introduction edition: 2002 for all rows — filed per
  docs/ISO2023_CONFORMANCE_PLAN.md:205-214 and Annex E's silence (predates 2014, spec :49014ff); keep the
  existing `vcr: "2002 introduction (derive from the 2002 standard)"` caveat on new rows (in-repo texts cannot
  prove 2002-vs-2014; see OWNER-5).
- **The storage-model DECISION (D-P2): ONE run-unit cell type + a window pointer, generalizing what EXTERNAL
  already ships.** Promote `ExternalStore.Holder` (ProgramRegistry.cs:264-286) to a general `StorageCell`
  (`string Ref` + `bool Allocated` + `bool Freed`); add **`CellPointer(StorageCell Cell, long Offset)`** as the
  third concrete `ManagedPointer` beside `NullManagedPointer` and `ManagedPointer<T>` (ProgramRegistry.cs:69-127);
  `SameTarget` (ProgramRegistry.cs:80-81) gains the structural branch its own doc comment promises
  (:77-79 "when ADDRESS OF lands … it compares referenced storage"): window vs window ⇒
  `ReferenceEquals(Cell) && Offset ==`; anything vs the null carrier ⇒ existing both-null test; legacy
  `ManagedPointer<T>` closures ⇒ existing `ReferenceEquals` (cross-kind never-equal unless same instance —
  unreachable until the CALL-boundary residue lands). Justification: (a) **feedback_singular_pattern** — the
  job "shared, aliasable, re-basable character storage" already has ONE canonical mechanism, the Tier-B
  string-canonical backing aliased through a heap cell + `ref`-property (`CallMakeExternal`
  DataBinder.Linkage.cs:207-242; the emitted bridge CSharpEmitter.Call.cs:512-514); ADDRESS OF, BASED deref,
  and ALLOCATE are all instances of that job, and a second near-identical cell type is exactly what the rule
  forbids. (b) **the no-byte-substrate directive** — the cell holds the record's CHARACTER IMAGE string, the
  design-sanctioned Tier-B shape (`ExternalStore`'s own doc: "never a persisted byte substrate",
  ProgramRegistry.cs:258-263); no `byte[]` anywhere; byte offsets are character offsets in this model
  (byte=char holds for the alphanumeric/zoned world — see RESIDUE-11 for NATIONAL/bit). (c) **SSOT authority**
  — COBOLNET_DESIGN.md:627-628: the ONE carrier serves "USAGE POINTER, ADDRESS OF, BASED, ALLOCATE/FREE, SET
  ADDRESS OF". **Rejected:** `ManagedPointer<T>.OverField/Cell` closures as the pointer VALUE — they fail
  structural equality (§8.8.4.2.16), have no byte offset for F10 GR20 (pointer_arith unrealizable), and no
  window algebra for a 1-byte BASED view over a 10-byte buffer; OverField stays what it is: the CALL-ABI
  carrier. A PointerRegistry-style side table stays rejected (memory feedback_managed_pointers). The
  `Cell(T)`-doc's "also the ALLOCATE backing" claim (ProgramRegistry.cs:93,113-114) is SUPERSEDED — update the
  doc comment + COBOLNET_DESIGN §9.1 in the same change set (feedback_follow_design_docs_and_spec).
- **ADDRESS OF ⇒ byte-backing (the CallMakeExternal generalization).** A parse-tree pre-scan collects the
  `SET p TO ADDRESS OF x` operands (alt 2 of setAddressStatement — the ONLY ADDRESS OF surface in the grammar;
  alt 1's operand is the BASED receiver, never forced); a late hook `PtrBindAddressables(program)` runs beside
  `CallBindExternalAndGlobal` (DataBinder.cs:187 — the proven post-`ClassifyRedefinesClasses` overwrite seam,
  :183) and, for each resolved non-BASED target, forces the target's ROOT-01 class to Tier-B StringCanonical
  over a per-instance `StorageCell` field: factor `CallMakeExternal`'s body (DataBinder.Linkage.cs:207-242)
  into ONE `ForceStringCanonical(anchor, gate)` used by both callers — the external caller keeps its proven
  Display-only leaf gate THIS increment; the addressable caller uses the CLASSIFIER's current gate
  (Display + fixed-point BINARY/PACKED as zoned image, DataBinder.cs:966-990) since that is the canonical
  Tier-B acceptance rule (gate convergence = follow-up, see RESIDUE). Emission mirrors the EXTERNAL bridge
  (CSharpEmitter.Call.cs:512-514): `private readonly StorageCell _cell_X = new() { Ref = «CallInitialImage» };`
  + `private ref string _redef_X => ref _cell_X.Ref;` (initial image via the existing `CallInitialImage`,
  DataBinder.Linkage.cs:244; nested classes reach the cell through the same parent path `BackingPath` builds,
  ReferenceResolver.cs:326-333). All reads/writes then flow through the UNCHANGED Tier-B machinery
  (`PlaceForItem` → `RedefViewPlace`, ReferenceResolver.cs:230-253; Place.cs:57-77) — zero new verb code. The
  pointer value: `SET P TO ADDRESS OF X` ⇒ `P = ManagedPointer.At(_cell_X, «X.ClassOffset»)`; an
  EXTERNAL+ADDRESS-OF item needs no special case — after unification its cell IS `ExternalStore.Cell(...)`
  (same `StorageCell`), and `At` takes it directly. `SET p TO ADDRESS OF B` where B is BASED reads the implicit
  pointer (§8.6.5 :8791): `p = __addr_B`. Rejected-tier targets, OCCURS-resident anchors (`BackingPath` null),
  and carrier-resident LINKAGE formals (no local cell, DataBinder.Linkage.cs:122-129) stage LOUD (0869).
- **BASED ⇒ pointer-routed Tier-B views.** New `basedClause` arm in the `BindEntry` clause loop
  (DataBinder.cs:630-676 — today the clause is silently DROPPED and the item gets real private storage,
  silently wrong per §13.18.5): set `item.IsBased`, emit NO stored field, classify the entry (elementary or
  group) as a synthetic StringCanonical class whose backing is reached THROUGH the implicit pointer. Two
  emitted members per BASED 01/77: the implicit pointer field `private ManagedPointer __addr_B =
  ManagedPointer.Null;` (GR2 — the increment-1 `PicInfo.PointerItem` init machinery, PicInfo.cs:183-184/:251)
  and the deref bridge `private ref string __based_B => ref CobolPtr.Deref(__addr_B, «classWidth»).Ref;` where
  `Deref` throws `CobolFatalException("EC-DATA-PTR-NULL", …)` on the null carrier (GR3) and
  `CobolFatalException("EC-BOUND-PTR", …)` on a freed cell, a non-window carrier, or `Offset + classWidth >
  Cell.Ref.Length` (GR4 — the bounds check lives here because the bridge knows the class width). References
  build `RedefViewPlace(Backing: "__based_B", OffsetExpr: $"CobolPtr.OffsetOf(__addr_B) + {classOffset}", …)`
  — `OffsetExpr` is already an arbitrary runtime `long` expression (Place.cs:66-69), and both Read and Write
  render the Backing FIRST (Place.cs:69,:75-76), so `Deref` trips before `OffsetOf` (which is null-lenient).
  **No new Place subtype** — RedefViewPlace over a swappable ref-property IS the fit. `SET ADDRESS OF B TO P`
  ⇒ `__addr_B = «P.Read()»` (F7 GR13 snapshot). The active `based-clause-2002` matrix witness (a
  declaration-only BASED GROUP, constructs.json:88-95) keeps compiling: classification + fields emit fine and
  the bridge is never invoked. Declaration validation rides the new 0881 band (below); a VALUE clause on a
  BASED entry follows the LINKAGE posture (level-88 only — verify the exact §13.18.5.3 SR at implementation).
- **ALLOCATE / FREE.** New binder arms in the statement dispatch (StatementBinder.cs:171-226 — today both fall
  to the generic `BoundUnsupported` tail at :226). `BoundAllocate`: form-1 `ALLOCATE expr CHARACTERS
  [INITIALIZED] RETURNING p` ⇒ `«p» = CobolPtr.Allocate(«ceil(expr)», zeroFill: INITIALIZED)` (GR1 round-up;
  GR2 n≤0 ⇒ the helper returns `Null`, no EC; GR6 INITIALIZED ⇒ `'\0'`-fill — the faithful binary-zeros image
  in the character model; GR8 otherwise ⇒ space-fill, conformant under "undefined"); form-2 `ALLOCATE B
  [INITIALIZED] [RETURNING p]` ⇒ `__addr_B = CobolPtr.Allocate(«B.classWidth»)` (+ `p = __addr_B` per GR4a),
  with INITIALIZED lowered to a `BoundSequence` [allocate; `BoundInitialize(B, WithFiller, ToValue,
  ThenDefault)`] reusing the LANDED M2-PROC-1 machinery verbatim (GR7's exact wording); GR9's
  pointer/object-subordinate nulling is moot this increment (a pointer/object leaf fails the Tier-B gate ⇒ the
  BASED group stages loud). GR5 (EC-STORAGE-NOT-AVAIL) is documented-unreachable: allocation failure under .NET
  is a platform OOM (RESIDUE-7). `BoundFree` (one or more operands, each category data-pointer — 0869 otherwise
  per SR1 :27500): emit sequentially per GR2 as `«p.Write»(CobolPtr.Free(«p.Read()», out bool __notAlloc));`
  — the helper implements the GR1 three-way: window at Offset 0 over an `Allocated && !Freed` cell ⇒ mark
  `Freed`, clear `Ref` (GC reclaims; every dangling alias later hits the Deref `Freed` trap = the "contents
  become undefined" license made loud), return `Null`; null carrier ⇒ return unchanged (no-op); anything else
  ⇒ `__notAlloc = true`, return unchanged — the emitter appends the TurnState-gated
  `ExceptionState.Set("EC-STORAGE-NOT-ALLOC", fatal:false, …)` block ONLY when that EC's checking is turned
  (the DEVLOG 577 raise-site pattern, ExceptionState.cs:10-13), so unchecked NF behavior = continue, exactly GR1c.
- **SET Format 7 / Format 10 binding.** Replace the staged `BoundUnsupported` at StatementBinder.cs:727-728
  with `BindSetAddress` handling both grammar alternatives: alt 1 receiver must be BASED (SR18 :31399 — 0869
  otherwise; see OWNER-4 for the IBM idiom), sender must resolve to `PicCategory.Pointer` (SR17); alt 2 routes
  into the existing `BindSetPointer` (:752-789) with the new AddressOf source leg. `BindSetUpDown` (:819+, the
  85-form setIndexStatement) gains a POINTER ARM mirroring the D-U7 category re-route precedent (:799-804): if
  the first target resolves `PicCategory.Pointer` ⇒ all targets must be pointers (0869 on a mix, SR23) and the
  amount must be statically integer-typed (0869 on a fractional-scaled expression — the bind-time face of GR19;
  the runtime EC-SIZE-ADDRESS leg is RESIDUE-7) ⇒ `BoundSetPointerUpDown`; emit `«p» = CobolPtr.UpBy(«p», «±n»)`
  (GR18 null ⇒ EC-DATA-PTR-NULL throw; GR20's implementor data-pointer range is the CARRIER's — the signed
  64-bit interval of character-position displacements, recorded as DOC-A.1-216 in `docs/CONFORMANCE.md` §7.
  An address INSIDE it but outside the addressed cell is a legal pointer VALUE and surfaces at deref as
  EC-BOUND-PTR; an address OUTSIDE it is GR20's own EC-RANGE-PTR with identifier-9 unchanged. kb/Work PB465
  corrected the earlier "unbounded" wording, which the carrier contradicted: the displacement WRAPPED at 2^63
  and `SET P UP BY 18446744073709551618` moved the pointer by 2).
- **EXACT SEAMS (all confirmed by reading — turn-key).** (1) Pre-scan + late hook: `PtrBindAddressables(program)`
  beside `CallBindExternalAndGlobal(program)` at DataBinder.cs:187 (post-`ClassifyRedefinesClasses` :183 —
  the proven tier-overwrite point); factor `ForceStringCanonical` out of `CallMakeExternal`
  (DataBinder.Linkage.cs:207-242). (2) basedClause arm: the clause loop DataBinder.cs:630-676; the
  PICTURE+POINTER rejection joins the 0812/0870 pattern block at DataBinder.cs:692-718. (3) Cell/bridge
  emission: beside the `CallExternalBackings` loop CSharpEmitter.Call.cs:512-514 (new `AddressableBackings` +
  `BasedBridges` lists on DataBinder). (4) Statement dispatch: new `allocateStatement()`/`freeStatement()`
  arms before the generic tail StatementBinder.cs:226. (5) SET F7: replace :727-728; extend `BindSetPointer`
  :752-789 (drop the ":783 ADDRESS OF senders are a later increment" message). (6) SET F10: the pointer arm at
  the top of `BindSetUpDown` :819+. (7) Emit arms: dispatch beside `case BoundSetPointer` CSharpEmitter.cs:433;
  new emitters next to `EmitSetPointer` :1198-1203. (8) Runtime: `StorageCell`/`CellPointer`/`SameTarget` in
  ProgramRegistry.cs:69-127+264-286 (the carrier family stays in ONE file); the `CobolPtr` static helpers may
  live in a new `Control/CobolPtr.cs` (helpers, not a second carrier). (9) Whole-group-image guard: reject a
  group image spanning a pointer leaf where `BuildPhysicals` walks widths (FieldEmitter.cs:80-112) — closing
  increment-1 gap #10 loud.
- **Bound-node additions** (Bound/BoundTree.cs, beside `BoundSetPointer` :346): `BoundAddressOf(DataItem Item)`
  (an operand-shaped record; the emitter derives cell path + ClassOffset, or reads `__addr_x` when Item is
  BASED); EXTEND `BoundSetPointer` with a nullable `BoundAddressOf Address` source leg (one node per job —
  never a parallel SET-pointer node); `BoundSetAddressOfBased(DataItem Based, Place Source)`;
  `BoundSetPointerUpDown(IReadOnlyList<Place> Targets, BoundExpression Amount, bool Up)`;
  `BoundAllocate(DataItem? Based, BoundExpression? Chars, bool Initialized, Place? Returning)`;
  `BoundFree(IReadOnlyList<Place> Operands)`.
- **Runtime additions** (namespace CobolNet.Runtime): `StorageCell` (Holder generalized — `ExternalStore.Cells`
  becomes `Dictionary<string, StorageCell>`; the generated `ref ….Ref` bridge shape is UNCHANGED, so IC-series
  baselines must stay byte-identical — guard-proved); `CellPointer` + `ManagedPointer.At(cell, offset)`;
  `SameTarget` structural branch; `CobolPtr.Deref(p, classWidth) → ref string`, `.OffsetOf(p) → long`,
  `.UpBy(p, n)`, `.Allocate(n, zeroFill=false)`, `.Free(p, out notAlloc)` — fatal ECs throw
  `CobolFatalException` (the documented "runtime raise points" channel + §14.6.13.1.3 #8 loud-failure doctrine,
  CobolFatalException.cs:5-13); NF ECs report via out-param for the emitter's TurnState-gated
  `ExceptionState.Set` block.
- **Diagnostics band plan.** (i) **0869 (existing pointer band — extend, message-differentiated):** SET ADDRESS
  receiver not BASED (SR18); F7 sender not a data-pointer (SR17); ADDRESS OF target unresolvable /
  Rejected-tier / OCCURS-resident / carrier-resident-formal (staged-loud wording); UP/DOWN pointer-mix or
  statically non-integer amount (SR23/GR19); ALLOCATE data-name not BASED (SR1), CHARACTERS without RETURNING
  (SR2 — the grammar leaves RETURNING optional, :1048), RETURNING not a pointer (SR3); FREE operand not a
  pointer (SR1 :27500); **the SR9 misuse sweep** (§13.18.60.4 SR9 :22730 enumerates the ONLY legal reference
  contexts) — MOVE/DISPLAY/arithmetic over a `PicCategory.Pointer` operand rejects loud (closes increment-1
  gap #7; safe: the active usage-pointer-2002 witness displays a literal); whole-group image spanning a
  pointer leaf (gap #10). (ii) **NEW 0881 (declaration-entry band; 0881 verified free — 0898 is the only other
  free 08xx code):** PICTURE with USAGE POINTER (§13.18.60.4 pointer is PICTURE-less; today it silently
  misbinds by picture at DataBinder.cs:720-721 — the W2 hazard class); BASED level not 01/77 or outside
  WS/LS/LINKAGE (§13.16 SR16 :17261); BASED+REDEFINES (:17215); BASED+EXTERNAL (:17219); VALUE on a BASED
  entry (LINKAGE posture). (iii) **0900 introduction gates** via `ConstructRegistry.Check` (the
  PicInfo.cs:515-520 pattern — binder-side, NOT a `{is2002()}?` predicate inside setStatement: a failing
  predicate there would fall through to the OTHER SET alternatives and mis-diagnose).
- **Edition gating (registry + matrix).** Existing rows: `usage-pointer-2002` ACTIVE (constructs.json:439-447
  + ConstructDialectStatus.cs:111) — update BOTH descriptions to drop the stale "ADDRESS OF/BASED/ALLOCATE are
  increment 2+" clause; `allocate-2002` (:40-48, ConstructDialectStatus.cs:64) and `free-2002` (:50-58, :65)
  flip pending→active AND strip the PENDING sentence (the PendingRows contract, VersionMatrixTests.cs:64-74);
  `based-clause-2002` (:88-95, ConstructDialectStatus.cs:69) stays active — its declaration-only group witness
  must keep compiling (verified above). NEW rows + registry entries: `set-address-2002` (ISO §14.9.39 F7 +
  §8.4.3.11; checked in `BindSetAddress`) and `pointer-arithmetic-2002` (F10; checked in the BindSetUpDown
  pointer arm — the 85 index form itself stays ungated); both introducedIn 2002, expectDiagnostic
  COBOLNET0900, vcr caveat "2002 introduction (derive from the 2002 standard)". EditionGateHints needs NO new
  entries (ALLOCATE/FREE/BASED already covered :36-37/:43/:84-85/:97; the new gates are binder-side).
- **Tests / goldens / docs.** Flip `based_pointer`/`pointer_alloc`/`pointer_arith` pending→enabled
  (manifest.json — keep both lists alphabetical; `boolean_data`/`float_usage`/`national_data` stay pending).
  New `PointerAddressingTests` unit battery: two independent `ADDRESS OF X` compare EQUAL (the SameTarget
  structural case increment 1 could not have); UP/DOWN composition; deref-while-NULL and deref-after-FREE
  throw the named ECs; Deref bounds (offset+width > cell length); ALLOCATE form-1 zero/negative → NULL;
  form-2 + INITIALIZED GR7; FREE three-way incl. mid-block operand → notAlloc; ADDRESS OF an EXTERNAL item.
  NEGATIVE corpus additions (per feedback_conformance_tests_per_feature — none exist today, grep-verified):
  allocate_non_based (SR1), allocate_chars_no_returning (SR2), set_address_non_based (SR18), free_non_pointer
  (SR1 :27500), move_pointer (SR9), pic_pointer_conflict (0881). Matrix: the two flips + two new rows + the
  description refresh. The three goldens double as the phase demo (feedback_demo_per_phase) — outputs
  ISO-derived above, not just non-crashing. Full battery + legacy guard green pre-commit with the IC-series
  watched specifically for the Holder→StorageCell rename (behavior-neutral by construction; escalate on any
  diff). Same-change-set doc updates: COBOLNET_DESIGN §9.1 (supersede the `Cell(T)`-as-ALLOCATE-backing claim
  with the StorageCell/CellPointer model + WHY), PicInfo.cs:31-35 and ProgramRegistry.cs:77-79/:93 doc
  comments, this file's M2-DATA-5/M2-PROC-5 rows, DEVLOG entry, resume-prompt STATE banner.
- **STAGED RESIDUE (named, loud).** (1) Pointers across the CALL boundary: no CobolArgAdapt pointer arm
  (ProgramRegistry.cs:168-255) / no CallEmitArg leg (CSharpEmitter.Call.cs:820-860) / LINKAGE POINTER formals
  / `CALL … USING ADDRESS OF` (grammar) — the SR9 CALL/INVOKE/PD-header contexts. (2) The general
  data-address-identifier as an operand (relations, function args) — `ADDRESS` exists ONLY inside
  setAddressStatement today. (3) Multi-receiver F7 + `ADDRESS OF` / NULL senders in the receiver form (grammar
  is single-receiver, plain-dataReference sender :1040). (4) ADDRESS OF: OCCURS-resident anchors (BackingPath
  null), carrier-resident LINKAGE formals, GLOBAL items referenced from contained programs — all staged 0869.
  (5) Restricted data-pointers (`POINTER TO type-name`, SR18 :22760) + STRONG-typedef interactions (F10 SR24).
  (6) FUNCTION-POINTER / PROGRAM-POINTER (F8/F9) — the M3-4 track-(b) leg. (7) Runtime EC-SIZE-ADDRESS (a
  non-integer amount reaching runtime) and EC-STORAGE-NOT-AVAIL (unreachable under .NET OOM — documented
  implementor posture). (8) §8.6.5 end-of-life-cycle disassociation for LOCAL-STORAGE BASED items
  (per-activation re-NULL; WS static lifetime matches today). (9) FILE STATUS / LINAGE / RECORD-clause items
  shall-not-be-BASED (:15498/:15903) — file-subsystem validation sweep. (10) The EXTERNAL leaf-gate
  convergence onto the classifier gate (widens EXTERNAL acceptance — separately testable). (11) byte=char
  breaks under NATIONAL/bit data behind a pointer — coordinate with track (a). (12) Full SR14 strictness for
  USAGE POINTER placement (see OWNER-3).
- **⚠ OWNER-DECISION ITEMS (explicit; defaults chosen, none block implementation).** **OWNER-1** Holder→
  StorageCell unification touches the EXTERNAL emission surface (IC baselines): DECIDED unify per
  feedback_singular_pattern with guard-proof; escalate only if the guard shows a diff. **OWNER-2** GR6
  "binary zeros" for `ALLOCATE … CHARACTERS INITIALIZED` in the character model: DECIDED `'\0'` fill (the
  faithful image; DISPLAY of such storage is undefined-content anyway) — flag if a vendor-comparison corpus
  ever expects spaces. **OWNER-3** §13.18.60.4 SR14 literally says USAGE POINTER at "level 1" (elementary, or
  STRONG-typedef subordinate) — unlike BASED SR16/§14.2.2 SR1 which spell "1 or 77" — yet the repo's own
  matrix witnesses use `77 P USAGE POINTER` (constructs.json:47,:57): DECIDED lenient (01/77 + elementary
  group members, the vendor norm; reading the omission as editorial), strict enforcement deferred to a
  dialect-strictness row. **OWNER-4** the IBM `SET ADDRESS OF non-BASED-linkage-item` idiom is NOT ISO (SR18
  :31399): DECIDED strict-ISO reject (0869); if NIST/legacy corpus ever needs it, add a dialect-leniency
  registry row (project_dialect_strictness) — never silent acceptance. **OWNER-5** `introducedIn` provenance
  for set-address/pointer-arithmetic rows is 2002 by catalog+corpus placement, but no in-repo text can prove
  2002-vs-2014 (Annex E only covers 2014→2023); if matrix authority is required, source the 1989:2002 text.

#### Increment 2 — AS BUILT (DEVLOG 617, 2026-07-05; all three goldens byte-exact)

Landed on the design's storage decision exactly (StorageCell + CellPointer + the CobolPtr helpers; the
Holder→StorageCell unification; the deref-bridge BASED realization over the UNCHANGED Tier-B machinery;
zero grammar change). Deviations/realizations, recorded per the process rule:

1. **The addressable cell's seed honors VALUE via `ImageInitOf`, not `CallInitialImage`** — the design cited
   `CallInitialImage`, which produces the DEFAULT image (the EXTERNAL GR6 posture); based_pointer's first run
   read `B=` five spaces because X's `VALUE "HELLO"` was lost. The emitter now seeds the cell with the SAME
   VALUE-honoring `CobolString.Store(ImageInitOf(canonical), width)` expression the Tier-B stored backing
   uses (`FieldEmitter.ImageInitOf` made internal; the ONE image-seed producer).
2. **`ForceStringCanonical` keeps the proven EXTERNAL leaf gate for ALL THREE callers** (Display-only
   fixed-point leaves) — the design suggested the classifier's broader zoned-BINARY/PACKED gate for
   addressable/BASED callers; the narrower gate only rejects MORE loudly (never silently wrong), and gate
   convergence stays the named follow-up (RESIDUE-10).
3. **The §13.18.60.4 SR9 misuse sweep is DEFERRED** (was increment-1 gap #7, stays a documented gap): a
   pointer operand misused in MOVE/DISPLAY/arithmetic today fails at the Roslyn level (a loud CS error over
   the ManagedPointer field — never silent), not with a named 0869; the total operand-context sweep is
   follow-up. The `move_pointer` negative case is dropped with it.
4. **ALLOCATE based-item INITIALIZED — LANDED (P10 Step-6 slice, 2026-07-16, DEVLOG 858):** the §14.9.3 GR7
   lowering binds the allocation + EXACTLY the spec's `INITIALIZE data-name-1 WITH FILLER ALL TO VALUE THEN
   TO DEFAULT` expansion (`InitializeBinder.BindAllocateInitialized` → a `BoundSequence`; the ONE INITIALIZE
   mechanism, no new emitter leg); witness = the ENABLED `2002/allocate_initialized` golden (GR7 + GR6
   zero-fill + GR4b RETURNING byte-verified; GreenfieldOnly in the legacy runner — legacy only zero-fills).
5. **Negative-corpus lesson:** the `.err` contract matches ONE CONTIGUOUS substring, and diagnostics
   interpose the item context after the code (`COBOLNET0881: data item 'B': …`) — an `.err` of
   `CODE: message` never matches; write the message fragment only.
6. **Diagnostics:** the SR checks landed on 0869 (statement band) + the NEW 0881 (declaration band:
   PIC-with-POINTER, BASED level/REDEFINES/VALUE); EcWrap gained the BoundFree arm (EC-STORAGE-NOT-ALLOC
   family selection). GR20's data-pointer range is the CARRIER's signed 64-bit displacement interval
   (DOC-A.1-216; kb/Work PB465 — EC-RANGE-PTR raises at SET time for an address outside it, with identifier-9
   unchanged, while out-of-cell-but-representable addressing still surfaces at deref as EC-BOUND-PTR).
7. **Residue (named):** pointers across the CALL boundary (CobolArgAdapt/CallEmitArg legs, LINKAGE POINTER
   formals), the general data-address-identifier as an operand (relations/args — `ADDRESS` exists only in
   setAddressStatement), multi-receiver F7 + ADDRESS OF/NULL senders in the receiver form,
   qualified/subscripted ADDRESS OF operands, GLOBAL BASED items from contained programs, restricted
   `POINTER TO` + FUNCTION/PROGRAM-POINTER (M3-4), EC-STORAGE-NOT-AVAIL posture (unreachable under .NET),
   LOCAL-STORAGE BASED end-of-cycle re-NULL, the FILE-subsystem shall-not-be-BASED sweep, byte≠char under
   NATIONAL/bit (track a), SR9 sweep (item 3), EXTERNAL-gate convergence (item 2).

#### Increment 2 — the ADVERSARIAL REVIEW WAVE (same change set; 24 raw → 23 confirmed / 1 refuted; ~10 distinct issues)

The 4-lens find→2-skeptic-verify workflow (wf_4c49e522-ec0) over the landed diff. Disposition, all same set:

**Fixed:**
- **ADDRESS OF a SUBORDINATE of a BASED record dropped its ClassOffset (major)** — the BASED leg of
  `PtrAddressOfText` returned the raw implicit pointer for ANY item under a based root (silent wrong-bytes
  aliasing); now `CobolPtr.UpBy(__addr_X, classOffset)` for non-root items (§8.4.3.11 GR1 — the address OF
  THE ITEM; UpBy's GR18 null trap is the right posture for a child of an unallocated record). CLI-probed:
  `ADDRESS OF B2` reads `PQR`, not the base's `XYZ`.
- **GR19 was a silent truncation (major)** — `SET P UP BY 2.5` moved by 2 via the Align(…, 0) rescale, and
  the shipped registry/matrix text falsely claimed a bind-time reject. Realized EXACTLY now:
  `CobolPtr.UpByAmount(p, scaledValue, scale, down)` — the divisibility test IS §14.9.39.4 GR19's integer-VALUE
  rule (2.0 moves by 2; 2.5 → EC-SIZE-ADDRESS fatal); registry + matrix text corrected. kb/Work PB465 then split
  GR20's RANGE test out of it: a magnitude no address can hold is EC-RANGE-PTR, never GR19's condition. This SUPERSEDES the
  design's "statically integer-typed 0869" plan — GR19 is a value rule, and the runtime check is strictly
  more conformant than a static-type reject.
- **BASED+EXTERNAL undetected (§13.16.3 SR5)** — both mechanisms would emit a bridge under the ONE
  BackingCsName (CS0102); the 0881 band gained the arm.
- **A BASED USING formal undetected (§14.2 SR1 :23658** — the same sentence as the implemented REDEFINES
  half**)** — the carrier-resident CsName rewrite would poison the based BackingCsName into invalid C#;
  0889 now covers both clauses.
- **BASED/ADDRESS OF in a CLASS unit → CS0103** — class binders ran the pointer pass but the OO emitter has
  no cell/bridge loops; staged loud (0899) until kb/Work PB956 — the OO type-halves now render the SAME
  members through `OoEmitter.EmitPointerBackings`, with method LOCAL-STORAGE members per activation.
- **FREE's nonfatal EC set never ran the declarative** — the checked leg now emits the §14.6.13.1.3 #5
  sequence (status set → F3 selection → RESUME-AT honored, no-handler continues).

**Reverted over-rejection:** the 0881 **VALUE-on-BASED arm was WRONG** — ISO defines VALUE semantics for
based entries (it seeds `ALLOCATE … INITIALIZED`'s GR7 TO-VALUE leg; un-INITIALIZED content is GR8-undefined,
so the space-filled cell is conformant). The arm is removed; a prohibition existed in no spec sentence.

**Documented (named residue, not fixed):** (a) >>TURN-enabled FATAL pointer ECs (EC-DATA-PTR-NULL /
EC-BOUND-PTR) raise as raw fatal terminations without consulting a USE F3 declarative — the EcWrap
family-selection walk for based-referencing statements is a named follow-up (checking-OFF behavior, the
corpus-wide default, is conformant as-is); (b) the whole-group-image pointer-leaf guard (increment-1 gap
#10, the design's seam 9) did NOT ship — a pointer leaf inside an image group still contributes zero width
silently (pre-existing increment-1 behavior, unchanged by this wave; still open).

**Coverage added (the tests lens):** PointerAddressingTests ×9 — ADDRESS OF an EXTERNAL record (the
unification's named witness), F10 multi-target + the SR23 mix 0869, UNMASKED gate-identity facts at 85 (the
review showed the two new 0900 gates were deletable without any test failing — the facts assert the gates'
own where-texts), the ALLOCATE legs (fractional round-up / INITIALIZED / form-2 RETURNING / zero→NULL),
FREE under >>TURN'd checking (both emit legs compile), subordinate-of-BASED; CobolPtrTests +1 (UpByAmount
both ways); negative corpus +1 (based-level-05).

### M2-DATA-3 / M2-DATA-4 — track (a) NATIONAL + BOOLEAN data (data-model legs only) — DECISION-COMPLETE DESIGN (synthesis 2026-07-05; ready to implement)

> Scope: the COMPLETE ISO data-model semantics for category **national** (§8.5.2.10) and category **boolean**
> (§8.5.2.5) — classification (PIC N / PIC 1 / USAGE NATIONAL / USAGE BIT), N"…"/B"…" literal binding, VALUE,
> MOVE (Table 16 legality + alignment), comparisons, DISPLAY, INITIALIZE, figurative constants, JUSTIFIED,
> ref-mod, group images — implemented to spec+design, never to the goldens (tests VERIFY, not SCOPE). Every leg
> the spec defines but this increment stages is NAMED with its § and a loud guard (RESIDUE list). **Grammar is
> UNTOUCHED**: the bit-operator leg (B-AND/B-OR/B-XOR/B-NOT, §8.8.2) stays OUT of THIS increment
> (residue #1 — UNBLOCKED by the owner's 2026-07-05 blanket grammar grant; queued as track (a) increment 2 with its
> own recon), as does every other .g4-requiring leg (INITIALIZE REPLACING BOOLEAN/NATIONAL category words,
> ALL N"…"/ALL B"…", NX"/BX", GROUP-USAGE, IS BOOLEAN class test — all named residue). Deliverables: both
> pending goldens ENABLED (`tests/conformance/2002/national_data`, `boolean_data`), the national golden
> ISO-re-baselined (one leg, audit below), the staged-loud test set flipped, registry/matrix rows LIVE.

- **Spec (ISO 2023; specs/ISO_COBOL.md :line anchors).** Literals: boolean §8.3.3.4 (:6082–6155 — SR1 ≤8,191
  positions :6111; SR2 only '0'/'1' :6115; GR4 zero-length legal :6147), national §8.3.3.5 (:6158–6248 — SR1
  ≤8,191 national positions :6196; SR2 repertoire + NOTE 1 mixed representation :6198–6204; GR3 compile→runtime
  CCS :6238). Classes §8.5.2 Table 2 (:8427–8439 — class National ⊃ national, national-edited, AND
  numeric-edited-usage-national; class Boolean ⊃ boolean only). PICTURE §13.18.40: GR8 boolean = only 1s
  (:20467), GR9 national = only Ns (:20469), GR10 national-edited = N + B/0// (:20471–20474), GR14 symbol 1
  **R14**: "each boolean character can be represented … as a bit, an alphanumeric character, or a national
  character" (:20567), GR14 symbol N (:20523), precedence Table 10 (1 accepts only 1 :21006; N accepts B 0 / N
  :21007 — so "boolean-edited" does not exist). USAGE §13.18.60.4: SR5 BIT ⇒ boolean PIC (:22722); SR12
  NATIONAL ⇒ PIC boolean/national/national-edited/numeric/numeric-edited (:22744); SR13 no USAGE + PIC N ⇒
  **implied NATIONAL**, bare PIC 1 ⇒ implied DISPLAY (:22746–22750); SR20 PIC N ⇒ only NATIONAL may be
  specified (:22764); GR8 national char size = equal to OR an integer multiple of the alphanumeric size,
  implementor-specified (:22793). MOVE §14.9.25: Table 16 (:28839–28852 — **verified in-spec this recon**:
  National→Alphanumeric/AN-edited = **No** :28847; Boolean→AN = Yes :28846; National→Boolean = Yes;
  AN→Boolean = Yes; AN-edited→Boolean = No; Numeric-int→National = Yes :28849); SR7 non-boolean figurative →
  boolean prohibited (:28817); GR2/3 zero-length literal ⇒ SPACE / ZERO (:28895–28897); GR6a alignment +
  A→N correspondence + EC-DATA-CONVERSION (:28909–28925); GR6d3 AN/national → numeric as unsigned integer.
  Receivers: national fills/truncates right with **national spaces** §14.6.8.5 (:24297–24301); boolean with
  **boolean zeros** §14.6.8.6 (:24304–24308); JUSTIFIED §13.18.32 SR3 legal for boolean/national, GR1/2 left
  fill = national spaces / bit zeros (:19264–19273). Comparisons §8.8.4.2: boolean = Format 2 **equality only**
  (F1 SR2/SR3 exclude class boolean :9608–9610; F2 :9566–9581), by VALUE regardless of usage, right-extend with
  boolean zeros (§8.8.4.2.8 :9683–9689); national = full ordering under the **national** program collating
  sequence, right-extend national spaces (§8.8.4.2.9/10 :9692–9715); AN-vs-national converts AN to a national
  temp (§8.8.4.2.6 :9645). DISPLAY §14.9.11 SR1 admits both; device conversion implementor-defined (GR1 :26893).
  VALUE §13.18.63 SR5 national literal for national items (:23232), SR10 boolean literal ≤ size (:23254),
  SR29 no THROUGH for boolean (:23327). INITIALIZE §14.9.20 GR6c fill table: Boolean → ZEROES, National →
  national SPACES (:27995–28009). Figuratives §8.3.3.6: GR4 ZERO is boolean '0's by context (:6375); GR1/6/7
  national SPACE/QUOTE/HIGH/LOW (:6350, :6383–6407); no boolean SPACE/QUOTE/HIGH/LOW (MOVE SR7). Ref-mod
  §8.4.3.3: positions are boolean/national positions, never bytes (GR1 :7073; GR5a bit positions :7083).
  Storage §8.1.2: char sizes per-charset, compile-time, implementor (:5083–5095); UTF-16 national is a
  conforming choice, not mandated (NOTE 2 :5077).

- **GOLDEN-vs-ISO AUDIT — ⛔ ONE re-baseline required (the rest conforming).** `national_data.cob:43–45` +
  `.out` line 8 (`N2A=GHI`) exercise **MOVE national → alphanumeric — prohibited by Table 16 at every
  national-bearing edition** (:28847 National row, AN column = No; DISPLAY-OF §15.26 :35335 is the sanctioned
  narrowing, itself residue). The golden predates the pivot (authored against the permissive legacy engine,
  commits d8cf144/338ba5a) — per the #1 process rule the spec wins: **DELETE the N2A leg** (source lines 42–45,
  `.out` line 8 → 12 lines), extend the header's deferral note with the Table-16 citation, and convert the
  adjudication into enforced coverage via a NEW negative case `move-national-to-an` (below). Legacy re-run on
  the edited program in the same commit (pure deletion — expected green; escalation ladder in the test plan).
  Everything else audited conforming: A2N (Table 16 AN→National Yes + GR6a conversion), NUM=042
  (Numeric-int→National Yes), boolean JR=0011 (§13.18.32 GR2 bit-zero left fill), MOVE ZERO/INITIALIZE fills
  (GR4 :6375 / GR6c), equality-only boolean relations, `<` national ordering under the default national PCS,
  `MOVE B-NAME TO B-FLAG` display-form→bit-form (§8.8.4.2.8's usage-independence, value moved per §14.9.25.4 GR6 a)).

- **STORAGE DECISIONS (the documented implementor choices — record in COBOLNET_DESIGN with the WHY).**
  **D-N1 National representation**: an elementary national item is a plain C# `string` of `Length` characters
  (Length = count of N positions); .NET strings are natively UTF-16, so the golden header's "two bytes per
  character position" is the *documented implementor choice* (§13.18.60.4 GR8 + §8.1.2 NOTE 2). ALL width
  machinery stays CHARACTER-position based (`CobolString.Store/RefMod/SpliceInto/Compare`, `ImageWidth`,
  `FUNCTION LENGTH`) — exactly the §14.6.8.5/§8.4.3.3/§15.50 unit. **An elementary item's ImageWidth is NEVER
  doubled** (a national leaf's is its `Length` positions; its storage is the separate `ByteWidth` member). A
  GROUP's `ImageWidth` is in its class's unit: national positions for a `GROUP-USAGE NATIONAL` group, storage
  characters for an alphanumeric group, which counts a national position as the two it occupies (kb/Work PB1665). **D-N2 byte=char containment**: byte=char does NOT hold for
  national under D-N1 — every byte-addressed surface REFUSES a national leaf, loud (REDEFINES via ComputeTier,
  EXTERNAL/ADDRESS-OF/BASED cells via ForceStringCanonical, FD/SD records via a new record gate; details
  below). Rationale over the size-equal-1-byte alternative GR8 would also permit: forward-compat — the named
  residue (non-Latin-1 correspondence, NX", BYTE-LENGTH = 2×chars, national collating) all presume the 2-byte
  choice, and baking 1-byte layouts into REDEFINES/files/cells now would force a breaking re-layout later
  (RESIDUE-11 coordination with track (b)). **D-B1 Boolean representation**: one alphanumeric character
  ('0'/'1') per boolean position for BOTH usage display AND usage bit — the §13.18.40.4 GR14 R14 license
  (:20567), a PERMANENTLY conforming choice (not an interim hack). `Usage.Bit` stays a distinct enum member
  (declaration fidelity, SR5 checking, future packing option) but maps to the identical string storage.
  **byte=char HOLDS for boolean**, so boolean leaves (both usages) are admitted at every char surface:
  Tier-B REDEFINES windows, group images, FD/SD records, EXTERNAL/cell classes, pointer windows (F10 GR20
  "bytes" = one byte per boolean position under D-B1 — conforming). True bit-packing = residue (an opt-in
  future representation, never required for conformance). **D-N3 National collating**: the national program
  collating sequence defaults to the native UTF-16 code-unit ordinal (§8.8.4.2.9's implementor default);
  the alphanumeric PCS **never** applies to national comparisons (separate sequences). A NON-native national
  sequence is LIVE (P10 Step 4): `ALPHABET … FOR NATIONAL` literal phrases build the sparse
  `NationalCollatingTable` (§12.3.7 GR7 k over the 65,536-code-unit set), resolved by `PROGRAM COLLATING
  SEQUENCE FOR NATIONAL`/alphabet-name-2 (§12.3.6) into the emitted `__COLLATE_NAT` channel (relations,
  condition-names, CHAR-NATIONAL/ORD, national HIGH-/LOW-VALUE = the sequence's extremes per §12.3.7
  GR8/GR9); UCS-4 collates identically to NATIVE (ISO 10646 order over one-code-unit-per-position characters
  IS code-unit order — §8.5.1.4 recognizes no surrogate pairs), and UTF-8/UTF-16 name coded character sets
  only (GR7 Table 6 — referencing them as a collating sequence is the 0898 reject). The NATIVE-default
  HIGH/LOW-VALUE in national (and alphanumeric) contexts is U+FFFF/U+0000, the native sequence's extremes
  (owner decision kb/Work R52 retired the former U+00FF pin — kb/Work PB1093).
  **D-N4 Repertoire**: the FULL national repertoire — one UTF-16 code unit per position (the Latin-1-only
  0814 guard was LIFTED with the P10 Step-5 DISPLAY-OF/NATIONAL-OF wave). The alphanumeric↔national
  correspondence is the TOTAL UTF-16 IDENTITY in both directions (Annex A.1 item 33, CONFORMANCE.md §7 —
  the PB59 landing 2026-08-09 retired the interim Latin-1 NAT→ANUM cut and its '?'+EC substitution). A→N
  widening and 9→N digit imaging are ≤U+00FF by construction.

- **PicInfo (the M2-DATA-1 BinaryItem un-skeleton recipe, adapted).** ⛔ **THE RECIPE BELOW IS THE PLAN THAT WAS
  EXECUTED, NOT A DESCRIPTION OF THE TREE** — the live status is the M2-DATA-3 row above. Every "SR12-legal but
  STAGED … 0899 Phase 4a residue" it names is GONE: national-edited landed with kb/Work PB492 and the
  national-form numeric / numeric-edited / boolean shapes with PB646 (design D-N7), so all five §13.18.60.3 SR12
  picture shapes bind and `ProfileInitializer` is reached by them. (1) **Classification**
  (`PicInfo.Analyze`, PicInfo.cs:314–444): remove `hasN`/`has1` from the gate block :372–387 (keep `hasE` +
  `invalid`); add a national/boolean classification block BEFORE the `anyAlpha`/`anyEdit` branches — expanded
  all-'N' ⇒ `ConstructRegistry.Check(edition, "national-data-2002", where)` (0900 below 2002, silent above) +
  return `new PicInfo(PicCategory.National, Usage.National, Length: count of N, Digits:0, Scale:0,
  Signed:false)`; expanded ⊆ {N,B,0,/} with ≥1 N ⇒ `NotImplementedSkeleton(edition, "national-edited-2002",
  "Phase 4a residue", where)` (NEW pending row, below) + alphanumeric recovery; expanded all-'1' ⇒
  `Check("boolean-data-2002")` + return `PicCategory.Boolean` with Length = count of 1s; any other mix
  containing N or 1 ⇒ COBOLNET0808 (Table 10: 1 accepts only 1; N accepts only B 0 / N). `ExpandRepeats`
  :459–480 already handles `N(4)`/`1(8)`. (2) **Usage resolution inside Analyze** (SR5/SR12/SR13/SR20;
  signature gains `bool explicitUsage = false`, threaded from DataBinder's `usageText is not null` :688/756):
  PIC N + implicit Display ⇒ usage National (SR13a); PIC N + explicit non-NATIONAL ⇒ **COBOLNET0881** (SR20);
  PIC 1 + Display (implicit or explicit) ⇒ Usage.Display char-form (SR13b); PIC 1 + BIT ⇒ Usage.Bit; PIC 1 +
  NATIONAL ⇒ SR12-legal but STAGED — direct 0899 "national-form boolean, Phase 4a residue"; PIC 1 + any other
  usage ⇒ 0881; USAGE BIT + non-boolean PIC ⇒ 0881 (SR5); USAGE NATIONAL + numeric/numeric-edited PIC ⇒
  SR12-legal but STAGED — direct 0899 "national-form numeric (national digits), Phase 4a residue"; USAGE
  NATIONAL + alphabetic/AN PIC ⇒ 0881 (SR12 + §13.18.40.3 SR30 :20395). (3) **`ParseUsage`** :513–514 flips
  both arms from `SkeletonUsage(...)` to the POINTER pattern :518–520: `ConstructRegistry.Check(edition,
  "national-data-2002"|"boolean-data-2002", where); return Usage.National|Usage.Bit;` (0899 gone; 0900
  introduction edge stays). (4) **`IsUnimplementedSkeleton`** :165–168 — delete `PicCategory.National or
  PicCategory.Boolean` and `Usage.National or Usage.Bit` (the master un-skeleton switch; FloatShort/Long/
  Extended remain); re-document Usage.National/Bit :74–77 as LIVE with phase + DEVLOG (the :88–100 precedent).
  (5) **Storage-mapping arms**: `ClrType` :228 gains `or PicCategory.National or PicCategory.Boolean` on the
  string arm; `DefaultInitializer` :253 splits — National ⇒ `new string(' ', {Length})` (national space,
  Latin-1 identity), Boolean ⇒ `new string('0', {Length})` (§13.18.63 GR — boolean initial/default zero fill);
  `StorageWidth` :264–276 falls to the existing `_ => 0` arm like Display (document); `ProfileInitializer`
  :286 is unreachable for the new categories (numeric-only caller) — the staged national-form-numeric 0899
  keeps it that way. (6) NO picture-less factory (unlike BinaryItem): USAGE NATIONAL/BIT **require** a PICTURE
  on an elementary item — a picture-less elementary NATIONAL/BIT entry errors **0881** at the group-fixup pass
  (the RecoveryItem-clearing site, DataBinder.cs:855 area — a group header sheds the usage to subordinates per
  §13.18.60.4 GR1, unchanged) and takes `PicInfo.RecoveryItem` for the doomed emit; the DataBinder ladder
  :736–743 drops its `skeletonUsage` reliance for these two usages (pic stays null → group-or-error path).

- **Literals — N"…"/B"…" bind arms (the silent-misbind fix; feedback_scan_all_similar sweep).**
  **Representation (feedback_singular_pattern — ONE literal node per job):** `BoundStringLiteral`
  (Bound/BoundTree.cs:93) gains `PicCategory Category { get; init; } = PicCategory.Alphanumeric`; same prop on
  `BoundAllLiteral` (:164). NO new bound-literal classes. **Decode helper**: extend `EmitText.DecodeCobolString`
  (CodeGen/Emit/EmitCore.cs:99–101) and `DataBinder.DecodeString` (DataBinder.cs:529) to strip a leading
  `N`/`B` (case-insensitive) when followed by the opening quote — safe (no other raw shape starts `N"`/`B"`).
  **Chokepoint arms** at `LiteralOperand` (StatementBinder.cs:937–943): `nn?.NATLIT()` ⇒
  `Check("national-data-2002")` + Latin-1 guard (any char > U+00FF ⇒ **COBOLNET0814**, the staged non-Latin-1
  correspondence §8.3.3.5 SR2/GR3 + §8.1.2 :5137) + length ≤ 8,191 (SR1, 0814) ⇒
  `BoundStringLiteral(decoded){Category = National}`; `nn?.BOOLLIT()` ⇒ `Check("boolean-data-2002")` + length
  cap ⇒ `{Category = Boolean}` (lexer already restricts content to [01]+, CobolLexer.g4:596–598; zero-length
  N"" ⇒ SPACE per MOVE GR2 falls out of `Store("", w)`). **Sweep the remaining funnels** — each gains an arm
  that either decodes (string-legal context) or rejects **COBOLNET0844** (numeric context — a boolean/national
  literal is not a numeric operand): StatementBinder.cs:1380 + Evaluate.cs:161 (`SoleNumLiteral` paths — EVALUATE
  selection objects take the decode arm), Intrinsics.cs:358 (numeric chokepoint — 0844), Inspect.cs:199–204 and
  Call.cs:68/183 (decode arms), plus the `figurativeConstant` note: `ALL NATLIT`/`ALL BOOLLIT` are NOT in the
  grammar (CobolExpressions.g4:291–299 — only ALL STRINGLIT/HEXLIT, verified) ⇒ grammar residue; FigurativeOperand
  :948–958 unchanged. The one existing decode precedent stays: StatementBinder.Oo.cs:991–995.

- **MOVE.** **Bind legality** — new `MoveCategoryLegality(source, targets)` beside `MoveFigurativeEditionGates`
  (StatementBinder.MoveFigurative.cs:42, called from BindMove StatementBinder.cs:462–483), all
  **COBOLNET0819**, version-invariant at ≥2002 (the operands themselves are 0900-gated below 2002): the Table
  16 boolean/national arms — receiver Boolean rejects senders alphabetic (`IsAlphabetic`), AN-edited
  (`EditMask`), numeric, numeric-edited; receiver National rejects non-integer numeric senders (Scale > 0,
  float); sender National rejects receivers alphabetic, alphanumeric[-edited] (the re-baselined leg), numeric
  non-integer stays legal→numeric per :28847 col Numeric = Yes; sender Boolean rejects receivers alphabetic,
  numeric[-edited]; **SR7** (:28817): BoundFigurative S/Q/H/L or a BoundAllLiteral containing a non-'0'/'1'
  char → category-Boolean receiver rejected (ZERO and digit-only ALL of 0/1s legal). Ref-mod receivers keep the
  §8.4.3.3 GR6 view (the RefModPlace exemption pattern :79). **Emit** — `ConvertSource` (CSharpEmitter.cs:701–
  762) gains two arms: `case PicCategory.National:` ⇒ `CobolString.Store(OperandText.AsString(source, deSign:
  true), pic.Length{, justifiedRight})` (identical shape to the Alphanumeric arm :738–741 — A→N widening, N→N,
  9→N digit imaging, boolean→N all ride `AsString`, the Latin-1 identity correspondence per GR6 :28909); `case
  PicCategory.Boolean:` ⇒ same with `pad: '0'` (§14.6.8.6). The figurative early-return :709–712 already
  produces the right fills (FigFill 'Z'→'0' for MOVE ZERO→boolean; 'S'→' ' for MOVE SPACE→national; SR7 shapes
  never reach emit — bind-rejected). National→numeric rides the existing `case PicCategory.Numeric` :742–758
  via `_num.AsNum` — **sweep item**: NumericRenderer's field-category dispatch must treat a National sender
  like the existing AN sender (`CobolNum.FromAlphanumeric`, GR6d3). **Runtime**: `CobolString.Store`
  (Runtime/Text/CobolString.cs:17–26) gains `char pad = ' '` (PadRight/PadLeft with pad — JR=0011 falls out);
  `SpliceInto` :52–62 gains the same (ref-mod stores into boolean pad '0'; RefModPlace.Write threads it when
  `Item.Pic.Category is Boolean`). All existing call sites untouched (default ' ').

- **Comparisons.** `OperandText.IsString` (OperandText.cs:37–47) — the field arm :41 adds `or
  PicCategory.National or PicCategory.Boolean`; `FieldAsString` :97 likewise (both are string-stored — `Read()`
  verbatim). `ConditionRenderer.RenderRelational` (ConditionRenderer.cs:38–76) gains a category dispatch ahead
  of the generic string leg :67–69 (the object/pointer precedent :44–62): a new `CategoryOf(BoundOperand)`
  helper (field pic category / literal Category tag). **Boolean leg**: either operand boolean ⇒ the other must
  be boolean (field or B-literal; mixed class ⇒ **COBOLNET0844** at bind — F1 SR2/SR3 exclude class boolean);
  operator must be `==`/`!=` (ordering ⇒ 0844 at bind, §8.8.4.2.2 vs F2 :9566–9581); emit
  `CobolString.Compare(l, r, pad: '0') == 0` — value compare, usage-independent, zero-extended (§8.8.4.2.8);
  `Compare` gains `char pad = ' '` on the 2-arg overload (:68–79; the ' ' defaults keep every existing site
  byte-identical). **National leg**: either operand national ⇒ full ordering via `CobolString.Compare(l, r)`
  **without `ctx.CollateArg`** — the alphanumeric PCS never governs national comparisons (§8.8.4.2.9; D-N3
  ordinal = the national default; the &0xFF weight-table aliasing hazard at :94–95 is thereby unreachable);
  AN-vs-national mixes ride the same leg (§8.8.4.2.6 — Latin-1 identity conversion). Figurative-vs-national
  comparisons ride the existing `RenderFigurativeRelational` :81+ (width-materialized, FigFill values per
  D-N3); figurative-vs-boolean: ZERO legal (zero fill), others 0844 (no boolean SPACE/QUOTE/HIGH/LOW —
  §8.3.3.6). Level-88 boolean/national VALUES: the 88 decode path (DataBinder.DecodeString callers) reads the
  prefix-stripped text; THROUGH with boolean values ⇒ **COBOLNET0898** (SR29 :23327); national THROUGH needs a
  national alphabet ⇒ 0899 staged (SR31 :23331, residue).

- **DISPLAY / VALUE / INITIALIZE / figuratives / ref-mod.** **DISPLAY** (CSharpEmitter.cs:467–474): zero code —
  national/boolean fields and tagged literals flow through `OperandText.AsString` as strings; console
  conversion is implementor-defined (§14.9.11 GR1); ASCII-only goldens are encoding-safe (no OutputEncoding
  change). **VALUE**: `FieldEmitter.InitializerFor` (FieldEmitter.cs:335–382) — the :376–377 arm becomes
  `PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National` ⇒ `CobolString.Store(
  DecodeCobolString(raw), Length)` and a Boolean arm with `pad: '0'`; `ImageInitOf` :146–147 and the default
  tail :149–151 gain the same category+pad arms (defensive — national never reaches Tier-B backings, boolean
  does); `FigurativeInitializer`/`FillCharFor` :404–428 already yield VALUE ZERO ⇒ '0'-fill and VALUE SPACE ⇒
  ' '-fill correctly. **VALUE validation** (**COBOLNET0898**, §13.18.63 SR5/SR10, checked at DataBinder entry
  build where RawValue+pic meet): category National requires an N"…" literal or a legal figurative
  (SPACE/QUOTE/HIGH/LOW/ZERO — GR1 :6350); category Boolean requires B"…" or ZERO; conversely N"/B" VALUE on
  any other category ⇒ 0898; length ≤ item size (SR5/SR10). **INITIALIZE** (StatementBinder.Initialize.cs):
  enum :36 gains `Boolean, National` members (binder-side only — the `initializeCategory` grammar rule
  CobolData.g4:431–437 has no such tokens, verified: REPLACING BOOLEAN/NATIONAL DATA = grammar residue, a
  parse error today = loud); `InitializeItemCategory` :206–215 gains `{ Category: PicCategory.Boolean } =>
  Boolean` and National likewise; `InitializeSender` :189–200 default-fill arm: Boolean joins the
  `BoundFigurative('Z')` leg, National the `'S'` leg (GR6c :27995–28009 — the golden's INIT=0000/INIT=R);
  `InitializeValueOperand` :233–246 gains N"/B" prefix decode arms (TO VALUE fidelity). **Figuratives**: no
  FigFill changes (EmitCore.cs:89–97 — Z '0', S ' ', H (now U+FFFF, kb/Work R52), L U+0000, Q '"' all conform under D-N3/D-N4).
  **Ref-mod**: zero structural change — `CobolString.RefMod`/`SpliceInto` are position-based (§8.4.3.3 GR1;
  under D-B1 a bit position IS a char index, so GR5a's bit-position rule is satisfied); the boolean splice pad
  is the one addition (above). **Group images**: `DataItem.IsCharacterImage` :142–145 category test gains
  `or PicCategory.National or PicCategory.Boolean` (a national/boolean leaf is string-stored ⇒ groups
  containing them get AsImage/FromImage, group MOVE/DISPLAY/compare work in char space); `ElementaryImageWidth`
  :180–189 already returns `pic.Length` ✓; `ElementType` :193 → "string" via ClrType ✓.

- **Byte-addressed guards (D-N2 enforcement — each loud, each cited).** (1) **REDEFINES**: `ComputeTier`
  (DataBinder.cs:1068–1099) gains a reject arm before the Tier-B fall-through: any leaf of category National ⇒
  `RedefinesTier.Rejected` with reason naming national + "§13.18.44 lays the shared area in bytes; the
  documented 2-byte national character (D-N1/D-N2) has no char-window overlay — Phase 4a residue" (the
  existing float/COMP-5 reject pattern :1082–1087; references then fail loud like today). Boolean leaves (both
  usages) fall through to Tier-B legitimately (char windows over '0'/'1'). (2) **Cells (EXTERNAL / ADDRESS OF /
  BASED — RESIDUE-11)**: `ForceStringCanonical`'s gate (DataBinder.Linkage.cs:243–249) ALREADY rejects
  `Usage.National`/`Usage.Bit` leaves (verified: `l.Pic is not { IsFloat: false, Usage: Usage.Display }`);
  extend the RejectReason wording to NAME national/bit + cite RESIDUE-11 (F10 byte arithmetic vs 2-byte
  national). Display-form boolean leaves PASS the gate and are cell-safe (byte=char, D-B1) — deliberate,
  document. Usage.Bit stays rejected in cells (conservative until the packing residue is closed — one leg, one
  posture). (3) **FD/SD records**: a new late DataBinder pass over `Files[*].Records` (the enumeration
  precedent DataBinder.Linkage.cs:205–209): any record leaf of category National ⇒ direct **0899** "national
  data in a file record — staged: the record codec is Latin-1 single-byte (SequentialFile.cs:422); the 2-byte
  national record layout is Phase 4a residue". Boolean record leaves flow (1 char = 1 byte ✓). (4) **PCS/
  intrinsic 256-table guards**: unreachable by construction — national compares never take CollateArg (above),
  `special-names-for-national-2002` already gates FOR NATIONAL (ConstructDialectStatus.cs:121 +
  EditionGateHints :111), CHAR/ORD take alphanumeric args only per their existing binds (StatementBinder.
  Intrinsics.cs:128 pattern — add a national-arg 0844 guard there for belt-and-braces), and BYTE-LENGTH is
  already `IntrinsicBind.Deferred` (IntrinsicCatalog.cs:125 — MUST return 2×chars for national when it lands;
  never port the legacy `Encoding.ASCII.GetByteCount`). `FUNCTION LENGTH` folds from ImageWidth = char
  positions ✓ §15.50 (Intrinsics.cs:163, no change). (5) **SORT/MERGE keys of category national** ⇒ 0899
  staged at key bind (file sorts are already caught by the record gate; the table-SORT key check is the only
  extra site).

- **Diagnostics band plan (all four free 08xx codes allocated — verified free this recon: 0814/0819/0844/0898;
  0900–0903 = the edition band; 0899 = the pinned staging code).** **NEW 0814** — national/boolean literal
  repertoire/size: non-Latin-1 char in N"…" (staged correspondence, §8.3.3.5 SR2/GR3 + §8.1.2 :5137); length
  > 8,191 (§8.3.3.4 SR1 :6111 / §8.3.3.5 SR1 :6196). **NEW 0819** — MOVE category legality: the Table 16
  boolean/national arms (§14.9.25.3 SR10 + Table 16 :28839–28852) and SR7 figurative→boolean (:28817).
  **NEW 0844** — operand/relation misuse: ordering operator on boolean operands (§8.8.4.2.2 F1 SR2/SR3
  :9608–9610); boolean-vs-non-boolean relation mix; boolean/national literal in a numeric-expression position
  (§8.8.1); CHAR/ORD national-arg guard. **NEW 0898** — VALUE clause category mismatch (§13.18.63 SR5 :23232 /
  SR10 :23254, both directions) + boolean THROUGH (SR29 :23327). **0881 (existing declaration band, extended
  message-differentiated — the M2-DATA-5 precedent)**: SR5 USAGE BIT with non-boolean PIC (:22722); SR20 PIC N
  with explicit non-NATIONAL usage (:22764); SR12/SR30 illegal PIC-with-NATIONAL shapes (:22744/:20395);
  picture-less elementary USAGE NATIONAL/BIT. **0899 direct (staged-legal sub-legs of the LIVE rows — the
  ParseUsage:548/EditionValidator:326 direct-emission precedent)**: national-form numerics (PIC 9/9V9/edited
  USAGE NATIONAL), national-form boolean (PIC 1 USAGE NATIONAL), FD/SD national records, national SORT keys,
  national THROUGH (SR31). **0899 via NotImplementedSkeleton + NEW pending registry row
  `national-edited-2002`**: N+B/0// pictures (§13.18.40.4 GR10, §8.5.2.11).

- **Edition gating (registry + matrix + drift-lock).** Flip `national-data-2002` (constructs.json:408–417) and
  `boolean-data-2002` (:418–427): `"status": "pending"` → `"active"`, descriptions rewritten LIVE ("LIVE
  (Phase 4a track (a), DEVLOG NNN): compiles at 2002+; below 2002 the introduction gate rejects
  (COBOLNET0900)" — the `usage-binary-char-family-2002` :459–465 pattern), keep `introducedIn: 2002` +
  `expectDiagnostic: "COBOLNET0900"` (matrix reject cells at 85 keep passing — the Check calls remain at every
  entry point: PicInfo.Analyze, ParseUsage, LiteralOperand). Update both registry citations
  (ConstructDialectStatus.cs:108–109) from "…PENDING" to LIVE wording — metadata fields unchanged, so
  `ConstructRegistryDriftTests` (:33–53) stays green by construction. ADD the pending row
  `national-edited-2002` BOTH sides (json + registry — drift-locked): introducedIn 2002, DiagnosticCode
  Introduction/0900, description+vcr+source all carrying "PENDING" (the `PendingRows_AreCataloguedWith
  ActivationContracts` contract, VersionMatrixTests.cs:64–74). **EditionGateHints: NO new entries** (N"/B"
  tokens parse at all editions — the gate is binder-side; verified only `special-names-for-national-2002` :47
  exists and stays).

- **Tests / goldens / legacy-runner (definitive).** (1) `tests/conformance/2002/manifest.json`: move
  `boolean_data` (after `binary_usage`, line 5) and `national_data` (after `initialize_phrases`, line 8) into
  `enabled`; `pending` becomes `["float_usage"]`. (2) **Golden re-baseline**: `national_data.cob` N2A leg
  deleted + header note (Table 16 citation, DISPLAY-OF named as the sanctioned narrowing residue);
  `national_data.out` drops line 8 (13→12 lines). (3) **LEGACY runner: NO GreenfieldOnly exclusion** — the
  seams brief's closing claim is WRONG for these two programs: discovery is manifest-blind
  (ConformanceTests.cs:30–42) and the legacy engine ALREADY compiles+runs both goldens green TODAY (they were
  authored against the legacy national/boolean implementation, commits d8cf144/338ba5a; verified neither
  appears in GreenfieldOnly :62–102). The DEVLOG-618 standing rule applies only to programs the frozen legacy
  cannot run. ONE contingency: after the N2A re-baseline, re-run the legacy conformance suite in the same
  commit ([[feedback_legacy_suite_on_shared_corpus]]) — a pure statement deletion is expected green; if the
  legacy output diverges anyway ⇒ `LegacyDivergent` entry with the Table-16 citation (the initialize_phrases
  precedent :51–55); only a legacy COMPILE failure (not expected) would justify GreenfieldOnly. (4)
  **Staged-loud flips**: DataSkeletonEditionTests.cs — delete rows NAT1 :37, BIT1 :38, PICN :42, PIC1 :43 from
  `SkeletonConstructs()`; add `NationalData_CompilesAt2002Plus_RejectedAt85` and
  `BooleanData_CompilesAt2002Plus_RejectedAt85` on the :114–128 BinaryCharFamily pattern; update the :30–34
  doc comment. LoudGuardTests.cs — delete NATIONAL/BIT rows :96–97 from the 0899-at-2023 usage theory and
  `N(4)`/`1(8)` rows :138–139 from the 0899-at-2023 Analyze theory; ADD
  `ParseUsage_NationalBit_LiveAt2002_IntroductionGatedAt85` (the :62–76 pattern, expecting
  `Usage.National`/`Usage.Bit` silently at 2002 + 0900 at 85) and positive Analyze facts
  (`Analyze_PicN_ClassifiesNational_UsageImplied` — category National, Usage National, Length 4;
  `Analyze_Pic1_ClassifiesBoolean_UsageDisplay` — Length 8); KEEP the 0900-at-85 Analyze rows :126–127 (still
  true via the retained Check). (5) **NEW unit/conformance batteries**:
  `NationalBooleanLiteralTests` (literal decode at every funnel — DISPLAY N"AB"/B"01", EVALUATE WHEN, INSPECT
  args, CALL USING literal, 0814 non-Latin-1, 0844 numeric-context); `NationalBooleanMoveTests` (every Table
  16 Yes/No cell reachable — the No cells assert 0819; SR7 cells; zero-length N""); `CobolStringPadTests`
  (Store/SpliceInto/Compare pad-char overloads — byte-identical defaults); `NationalBooleanDataTests`
  (0881 SR5/SR20 shapes, 0898 VALUE mismatches + boolean THROUGH, 0899 staged shapes: national-edited PIC,
  PIC 9 USAGE NATIONAL, FD national record, REDEFINES national reject, EXTERNAL cell reject naming RESIDUE-11,
  boolean ordering 0844); level-88 B"…" condition test. (6) **Negative corpus** (contract: first line
  `*> reject-at: 2002 2014 2023`, `.err` = ONE case-insensitive substring): `move-national-to-an` ("national
  sending operand"), `move-space-to-boolean` (SR7), `pic-n-usage-display` (SR20), `bit-usage-numeric-pic`
  (SR5), `boolean-ordering-relation` (0844), `value-boolean-mismatch` (SR10); manifest.json (negative) gains
  all six in `enabled`. (7) Both goldens double as the phase demo — outputs ISO-derived above
  (feedback_demo_per_phase / feedback_verify_demo_output).

- **Implementation order (small commits, battery-gated).** **Commit 1 — BOOLEAN (M2-DATA-4)**: CobolString
  pad params → PicInfo boolean legs (Analyze/ParseUsage/skeleton-list removal for Boolean+Bit only) →
  BoundStringLiteral.Category + BOOLLIT arms at all funnels → ConvertSource Boolean arm + SpliceInto pad →
  MoveCategoryLegality boolean half (0819) + relation guard (0844) + Compare pad → InitializeCategory.Boolean
  → VALUE decode/validation (0898) + 0881 SR5 → IsCharacterImage Boolean → boolean test flips + new tests +
  negative cases → `boolean_data` enabled + registry/json boolean flip + docs (COBOLNET_DESIGN data §: D-B1) +
  DEVLOG. GATE: full greenfield battery + LEGACY ConformanceTests + guard-fast. **Commit 2 — NATIONAL
  (M2-DATA-3)**: PicInfo national legs + `national-edited-2002` pending row + SR12/SR20/SR13 resolution (0881/
  0899 shapes) → NATLIT arms + 0814 guard → ConvertSource National arm + Table 16 national half → national
  comparison leg (no CollateArg) → InitializeCategory.National → VALUE arms → IsCharacterImage National →
  byte-addressed guards (ComputeTier reject, ForceStringCanonical wording, FD record gate, SORT-key guard,
  CHAR/ORD guard) → **golden re-baseline (N2A)** + `national_data` enabled + `move-national-to-an` negative +
  registry/json national flip + docs (D-N1..D-N4 + the M2-DATA-3/4 row flips in THIS file) + DEVLOG. GATE:
  full battery + legacy ConformanceTests on the EDITED golden + FULL legacy guard. **Commit 3** — adversarial
  find→verify wave over the whole diff (the proven track cadence), fixes same-set; resume-prompt STATE banner;
  push every checkpoint (feedback_fully_autonomous_push).

- **ANCHOR LIST (verified this recon unless marked [brief]).** PicInfo.cs :18–24, :66–77, :115–121, :165–168,
  :209–212, :217/:228, :245–261, :264–276, :282–298, :314–315, :342–387, :416–424, :459–480, :492–553
  (:513–514, :518–520), :559–587; StatementBinder.cs :937–943, :948–958, :1380; StatementBinder.Evaluate.cs
  :161; StatementBinder.Intrinsics.cs :358, :128 [brief], :163 [brief]; StatementBinder.Inspect.cs :199–204
  [brief]; StatementBinder.Call.cs :68, :183 [brief]; StatementBinder.Oo.cs :986–1008;
  StatementBinder.MoveFigurative.cs :42–125 (:44–59 0809 SR1 precedent), :140–146; StatementBinder.Initialize.cs
  :36, :189–200, :206–215, :233–246; Bound/BoundTree.cs :93, :164 [brief]; CSharpEmitter.cs :467–474, :476–493,
  :510–529, :551–570, :692–698, :701–762 (:709–715, :738–758); CodeGen/Emit/ConditionRenderer.cs :38–76
  (:67–69), :81+; CodeGen/Emit/OperandText.cs :17–33, :37–47 (:41), :49–100 (:97); CodeGen/Emit/FieldEmitter.cs
  :122–152 (:137–151), :174–228, :335–382 (:343, :376–377), :404–428; CodeGen/Emit/EmitCore.cs :89–101,
  :103–115; CodeGen/Emit/NumericRenderer.cs (AsNum field-category dispatch — sweep site); Binding/DataItem.cs
  :142–145, :160–164, :172–189, :193; Binding/DataBinder.cs :529, :667–668, :685–743 (:688, :729–743), :855
  [brief], :971–1042, :1050–1063, :1068–1099; Binding/DataBinder.Linkage.cs :205–223, :232–259;
  Runtime/Text/CobolString.cs :17–26, :34–44, :52–62, :68–79, :88–99; Runtime/Text/CobolClass.cs [brief];
  Validation/ConstructDialectStatus.cs :108–109, :121; Frontend/Parsing/EditionGateHints.cs :47, :111 [brief];
  Grammar/Core/CobolLexer.g4 :584–598 [brief]; Grammar/Core/CobolExpressions.g4 :264–270, :291–299;
  Grammar/Core/CobolData.g4 :428–437; tests/version-matrix/constructs.json :408–427;
  tests/conformance/2002/manifest.json :5, :8, :46; tests/conformance/2002/national_data.cob :42–45 + .out
  line 8; tests/CobolSharp.Tests.Integration/ConformanceTests.cs :51–55, :62–102, :108;
  tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs :25–31, :61–88, :97–115 [brief];
  DataSkeletonEditionTests.cs :30–45, :50–77, :114–128 [brief]; LoudGuardTests.cs :62–76, :84–110, :125–147
  [brief]; VersionMatrixTests.cs :53–74 [brief]; ConstructRegistryDriftTests.cs :33–53 [brief];
  Runtime SequentialFile.cs :199–222, :422 [brief]; IntrinsicCatalog.cs :125 [brief]. Plus the
  **category-set sweep command**: grep `PicCategory.Alphanumeric or PicCategory.NumericEdited` across
  src/Cobol.Net.Compiler — every hit consciously extended (National/Boolean join) or exempted with a comment
  (feedback_scan_all_similar).

- **STAGED RESIDUE (named, loud — each with § + guard).** (1) **Bit operators** B-AND/B-OR/B-XOR/B-NOT/
  B-SHIFT-* + boolean COMPUTE (§8.8.2 :9323–9420) — GRAMMAR; approval GRANTED 2026-07-05 (blanket grammar
  grant) — queued as track (a) increment 2 (own recon/design); no lexer tokens ⇒ parse error today. (2) ~~**NATIONAL-EDITED** pictures (§13.18.40.4 GR10; §8.5.2.11)~~ — **LANDED 2026-09-12, kb/Work PB492**: the category is `{ Category: National, EditMask: not null }` (data-model design D-N6), rendered by the ONE Table-7 simple-insertion formatter, and the old
  `national-edited-2002` pending row is ACTIVE. (3) **NX"…"/BX"…"** (§8.3.3.5.2/§8.3.3.4.2) — no lexer
  rules (CobolLexer.g4 comments name it); parse error. (4) **Non-Latin-1 national content** (§8.3.3.5 SR2,
  MOVE GR6 correspondence + EC-DATA-CONVERSION :28919) — 0814 at literal bind. (5) **National collating
  suite**: ALPHABET/CLASS FOR NATIONAL (§12.3.7 — existing `special-names-for-national-2002` gate), SORT/MERGE
  COLLATING FOR NATIONAL, indexed national keys, CODE-SET FOR NATIONAL, national HIGH/LOW-VALUE under a
  declared sequence — parse/0900-gated + the SORT-key 0899. (6) **True bit-packing + §8.5.1.6.3 alignment** —
  permanently optional under D-B1 (R14); revisit only with GROUP-USAGE BIT. (7) **GROUP-USAGE BIT/NATIONAL**
  (§13.18.29) — grammar; parse error. (8) **National-form numerics / national-form boolean** (PIC 9 / PIC 1
  USAGE NATIONAL, §13.18.60.4 SR12) — direct 0899 in Analyze. (9) **FD/SD national records** (§8.1.2 +
  Latin-1 codec posture) — 0899 record gate. (10) **REDEFINES / EXTERNAL / ADDRESS-OF / BASED over national**
  (RESIDUE-11) — ⛔ **DISCHARGED** (kb/Work PB231, 2026-09-05), and with it the "USAGE BIT in cells" half
  discharged a day earlier (kb/Work PB203 for the REDEFINES surface, PB231 for the EXTERNAL / ADDRESS-OF /
  BASED cells): a USAGE BIT run rides every byte-window surface on the §8.5.1.6.3 packing the class offset
  walk already lays out, and a NATIONAL leaf rides it on its §13.18.60.4 GR8 two-bytes-per-position storage
  extent — which is exactly the "per-item byte offsets + UTF-16BE cell images" this ledger entry named as the
  real fix. The class walk's per-member advance became ONE authority (`DataBinder.ClassExtentBits` →
  `BitLayout.WidthBits`), the class width and the in-class OCCURS stride became `ByteWidth`, and
  `Place.NationalWindow` transcodes the pair through `CobolBits.NatReadWindow`/`NatWriteWindow`. Positive
  witnesses `conformance:2002/pb231_based_bit_leaf`, `conformance:2002/pb231_based_national_leaf`,
  `conformance:2002/pb231_national_byte_window`; drift pin `ByteWindowResidueDriftTests`. ⛔ The pointer/
  object-class leaf — the third and last — is **DISCHARGED TOO** (kb/Work PB231, 2026-09-05): it is the one
  leaf kind a byte window cannot express at all, so the shared area grew a second half. `StorageCell` carries
  MANAGED SLOTS keyed by byte offset beside its byte image, the member's bytes there are reserved placeholders
  of its `ByteWidth`, and `Place.SlotWindow` (the third `WindowCoding`) reads/writes the slot through
  `CobolPtr.SlotRead`/`SlotWrite` — see **D-SLOT** in `COBOLNET_DATA_MODEL_DESIGN.md`. §14.9.3.4 GR9's
  null-seeding is satisfied by construction (an unwritten slot reads null). Positive witness
  `conformance:2002/pb231_based_pointer_leaf`; the two NONCONFORMING shapes the residue diagnostic used to
  reject for the wrong reason now draw their own rules (COBOLNET1797 = §13.18.5.3 SR1, COBOLNET1796 =
  §13.18.22.3 SR4). **The byte-window carriage gate has no residue left.** ✅ Item (9), the FD/SD
  national record gate, was discharged next, by kb/Work PB327 (2026-09-05): the record codec IS the byte
  channel — `AsImage()`/`FromImage()` ships a national leaf's two UTF-16BE bytes, `GateNationalRecords` became
  `GateFileRecordByteSurface` asking the ONE byte-window predicate, and D-N5 in
  `COBOLNET_DATA_MODEL_DESIGN.md` records the discharge. Witnesses
  `conformance:2002/pb327_national_fd_record`, `conformance:2002/pb327_national_line_sequential_fill`,
  `conformance:2002/pb327_national_record_key`.
  (11) **STANDARD-COMPARE §15.85 / BYTE-LENGTH §15.14** — IntrinsicBind.Deferred (BYTE-LENGTH must answer 2×
  for national when it lands). *(NATIONAL-OF §15.66 / DISPLAY-OF §15.26 LANDED — P10 national wave; CHAR-NATIONAL
  §15.16 + the EC -N twins §15.29/§15.31 LANDED — P10 Step 11.)*
  (12) **STRING/UNSTRING/INSPECT class-mix SR validation for national/boolean operands** (§14.9.43/48/22) —
  operands flow char-correct for legal all-national programs; the SR diagnostics sweep is residue. (13)
  **Simple boolean condition** `IF bool-item` (§8.8.4.3) — binds as an unresolved condition-name today (loud).
  (14) **IS BOOLEAN class condition** + NUMERIC-on-national (§8.8.4.4) — grammar/bind residue. (15)
  **Concatenation `&`** of boolean/national literals (§8.8.3). (16) **INITIALIZE REPLACING BOOLEAN/NATIONAL
  category words** (§14.9.20.2) — grammar (CobolData.g4:428–437 comment already names it); parse error. (17)
  **ALL N"…"/ALL B"…"** (§8.3.3.6.3 SR2) — grammar (figurativeConstant has only ALL STRINGLIT/HEXLIT). (18)
  **Level-88 national THROUGH** (SR31) — 0899. (19) **MOVE CORR bit/national groups as groups** (NOTE 5
  :29039) — moot until (7). (20) **VALIDATE N/1 format checks** (:20625/:20644) — VALIDATE subsystem. (21)
  **Screen/ACCEPT national items** (USAGE SR17/VALUE SR15) + boolean ACCEPT zero-pad — ACCEPT stores chars
  today; content legs ride the screen-section residue. (22) **Dynamic-length national/boolean** (zero-length
  semantics §8.5.4) — not modeled. (23) **Zero-length boolean literal `B""`** (§8.3.3.4 GR4 — spec-legal) —
  the lexer's `BOOLLIT : 'B' '"' [01]+ '"'` requires ≥1 position, so `B""` lexes as `B` + `""`, not an empty
  boolean literal (adversarial-review, DEVLOG 620); relaxing to `[01]*` risks `B`-identifier-then-string
  ambiguity, deferred. (`N""` already lexes — NATLIT's body is `…*`.)

- **⚠ FLAGS (brief conflicts, resolved in-repo).** **F1** Goldens brief §1 lists `N2A` as a behavior to
  implement; the spec brief marks national→AN illegal — **verified in the spec itself** (Table 16 :28847) ⇒
  re-baseline, negative case added (the spec-over-golden process rule). **F2** Seams brief's closing note
  demands a GreenfieldOnly exclusion; goldens brief §7 proves none is needed — **verified**
  (ConformanceTests.cs:62–102 + manifest-blind discovery; legacy implements both). **F3** Seams brief calls
  the INITIALIZE category words "grammar-gated work" — verified (CobolData.g4:431–437); the ENUM+default-fill
  legs are binder-side and IN scope, the REPLACING words are residue #16. **F4** Storage brief's
  REDEFINES-stage-loud vs the GR8 size-equal alternative — DECIDED for the 2-byte documented choice (D-N2
  rationale). **F5** PicInfo anchors in the older reconciliation (317/328/467) have drifted — current anchors
  verified and used throughout. **F6** ForceStringCanonical already rejects Usage.National/Bit generically
  (verified :243) — only the naming/citation changes. **F7** The picture-less elementary USAGE NATIONAL/BIT
  error site (group-fixup pass, DataBinder.cs:855 area) is the one anchor taken from the briefs without a
  direct read this session — verify at implementation.

#### M2-DATA-3/4 — AS BUILT (2026-07-05, same day; deviations + notes)

- Implemented exactly per the design with these adjudications: (1) **Call.cs:68/:183 are NOT decode arms** —
  CALL/CANCEL literal targets must be alphanumeric (§14.9.4.3 SR2 / §14.9.5.2 SR1); the existing loud
  BoundUnsupported is the correct posture (the design's funnel list marked them decode — refuted at
  implementation). (2) **A THIRD DecodeCobolString twin** existed at StatementBinder.cs (the design's decode
  plan named two) — all three per-layer twins now strip the N/B prefix; found via the boolean golden probing
  raw `B"01` stores. (3) **SUB-mode literal tokens added** (SUB_NATLIT/SUB_BOOLLIT, CobolLexer.g4 SUBSCRIPT
  mode): in SUBSCRIPT mode `N"AB"` lexed as SUB_IDENTIFIER('N') + SUB_STRINGLIT — FUNCTION LENGTH(N"AB")
  misbound; the proper-token rule (feedback_proper_fixes) under the 2026-07-05 blanket grammar grant.
  (4) **The boolean relation checkpoint is ONE factory** — CheckedRelational at every BoundRelational site
  (IF / EVALUATE pairing + THRU ranges / UNTIL / SEARCH), which also discharges §14.9.13.3 SR4 (boolean THRU
  range) for free. (5) **The national golden was re-baselined twice-in-one**: the N2A leg deleted (Table 16
  :28847, verified in-spec before the edit) AND the .out flipped to full-width DISPLAY (§14.9.11.4 GR6, the
  DEVLOG-597 posture) → the legacy runner carries a `LegacyDivergent` entry; boolean_data needs none
  (exact-width values). (6) **Group USAGE NATIONAL/BIT conformance** rides the `UsageInheritancePass` shed site
  (SR12/SR5 over subordinate leaves); a picture-less elementary NATIONAL/BIT errors 0881 there (the
  NationalUsagePending/BitUsagePending marker singletons — the RecoveryItem pattern).
- Battery: unit 159 (was 130) · conformance grew by the two goldens + 4 new test batteries + 6 negative
  cases; legacy ConformanceTests green with the one LegacyDivergent addition.

**ADVERSARIAL REVIEW WAVE (wf_ecf6ff42, 5 lenses × find→2-skeptic-verify; the Fable limit killed 34/53 verify
agents mid-run, so the unverified findings were adjudicated in-session on Opus). 6 dual-confirmed + 3
self-adjudicated-real fixed SAME change set; the rest refuted or staged:**
1. **Apostrophe-delimited `N'…'`/`B'…'` stored their quotes** (silent-wrong-value) — all THREE DecodeCobolString
   twins unwrapped only `"`; now unwrap EITHER delimiter and collapse the doubled OPENING quote (§8.3.3.1 both delimiters, §8.3.3.2.3 r3 the doubling).
   ALSO closed the pre-existing plain-`'…'` STRINGLIT leg. Probed `N'AB'`→`AB `, `N'IT''S'`→`IT'S`.
2. **§14.9.25.3 SR8 unenforced** (spec-violation) — a BINARY-CHAR/-SHORT/-LONG/-DOUBLE sender to a national
   (or any non-numeric) receiver compiled silently; `MoveCategoryLegality` gained a top-of-loop SR8 guard
   (0819) preceding the Table-16 arms. Corpus-safe (the family is 2002+). Negative: `move-binary-to-national`.
3. **Figurative MOVE into a ref-mod slice filled only position 1** (silent-wrong-value) — `MOVE ZERO TO N5(2:3)`
   gave `A0␠␠E`; new `RefModPlace.WriteFill` (empty slice + fill-char pad) fills EVERY position (§8.3.3.6.4 GR2 /
   §8.4.3.3 GR5), category-aware fill. Now `A000E`.
4. **Boolean figurative relation dropped the boolean-zero pad** (silent-wrong-value) — `IF B(1:2) = ZERO`
   space-extended and compared unequal; `RenderFigurativeRelational` now threads `pad: '0'` for a boolean
   anchor (§8.8.4.2.8), mirroring the direct + 88 legs.
5. **Level-88 VALUE category unchecked both directions** (spec-violation) — `88 X VALUE B"01"` under PIC X (and
   the reverse) silently accepted; `BindCondition` routes every 88 VALUE operand (singleton + THRU bounds)
   through the ONE `ValidateValueCategory` (0898, §13.18.63 SR4/SR5/SR24→SR10). Negatives:
   `level88-boolean-value-alnum`, `level88-alnum-value-boolean`.
6. **Class conditions on boolean operands accepted silently** (spec-violation) — `IF bit-item IS NUMERIC`
   answered a tautology; `IS ALPHABETIC` too. New `CheckClassConditionOperand` at BOTH BoundClassCondition
   sites (0844, §8.8.4.4.3 SR8/SR4 — DISPLAY-form boolean IS NUMERIC stays legal per SR8). Negatives:
   `class-numeric-on-bit`, `class-alphabetic-on-boolean`.
7. **`N"…"`/`B"…"` intrinsic args were a hard PARSE error / the Intrinsics decode arms were DEAD CODE** — the
   SUB_NATLIT/SUB_BOOLLIT tokens were added to the lexer but not to the `subToken` parser rule (as-built claim
   #3 above was aspirational until this). Added the two `subToken` alternatives; `FUNCTION LENGTH(N"AB")`=2 now.
8. **`ValidateValueCategory` falsely rejected `VALUE ALL SPACES` on PIC N / `ALL ZEROS` on PIC 1** — the
   National arm rejected any `ALL`-prefixed raw; now distinguishes `ALL "quoted"` (illegal) from the figurative
   WORD forms (legal). Both arms accept the figurative-word set.
9. **SET condition-name TO TRUE stored the figurative WORD** — `88 F VALUE ZERO` + `SET F TO TRUE` stored
   `"ZERO"`; `EmitSet` now fills via `FigurativeWordFill` (category-aware) for figurative-word 88 VALUEs
   (§14.9.39 F5 + §8.3.3.6.4 GR2). New surface for boolean/national; the pre-existing alphanumeric leg is
   cured by the same code.
- Also fixed while here: **the HIGH/LOW-VALUE fill for national/boolean now uses the native extremes** (U+FFFF/U+0000 since kb/Work R52),
  never the alphanumeric program collating sequence's extreme — new `EmissionContext.FigFill(kind, cat)`,
  threaded through ConvertSource / ref-mod / comparison / VALUE / SET (a declared alphanumeric PCS + national
  HIGH/LOW was the one-vote finding). **The USAGE syntax-rule citations were corrected `§13.18.60.4 → .3`** (SRs
  live in .3 / GRs in .4). **The double COBOLNET0900** for `PIC N USAGE NATIONAL` at <2002 (both ParseUsage and
  Analyze gated) — Analyze now skips the gate when the usage already fired it.
- **Refuted / staged (not fixed):** national HIGH/LOW-VALUE COMPARISON under a *declared* alphanumeric PCS
  (the FigFill fix covers stores/fills; the comparison leg already exempts the PCS via the D-N3 collate
  omission); `national_data` "asserted by no runner" — REFUTED (CorpusRunnerTests compiles+runs+byte-compares
  every enabled program). **NEW named residue: zero-length `B""`** — the lexer's BOOLLIT is `[01]+`, so `B""`
  (spec-legal per §8.3.3.4 GR4) lexes as `B` + `""` rather than an empty boolean literal; deferred (the
  ambiguity risk of `[01]*` outweighs the rare zero-length case). `N""` already lexes (NATLIT is `…*`).
- Battery (post-review): conformance **1776** · unit **159** · legacy ConformanceTests **55** ·
  FULL legacy guard on the subToken .g4 change.

#### M2-DATA-4 increment 2 — AS BUILT (2026-07-05, DEVLOG 621) — LANDED

The boolean operators B-AND/B-OR/B-XOR/B-NOT are LIVE in **COMPUTE Format 2** (byte-exact vs the ISO Annex A
Table A.2 oracle `1100 B-AND 0101 = 0100 / B-OR 1101 / B-XOR 1001 / B-NOT 0011`), including **nesting/precedence
via parens** and the **figurative `ALL B"…"`** operand (§8.3.3.6.4 GR2), plus the **simple boolean condition
over a bare length-1 boolean item** (`IF flag`, §8.8.4.3, via the pre-existing generic condition path).
**✅ RESIDUE CLOSED (DEVLOG 622): the boolean RELATION (§8.8.4.2.2) and the simple boolean condition (§8.8.4.3)
now work in conditions — unparenthesized (`IF a B-AND b`, `IF a B-AND b = c`) and parenthesized alike — with ZERO
regression.** The DEVLOG-621 lesson applied: NOT via `comparisonExpression` (whose modification regressed 31
legacy integration tests — subscript/ref-mod comparisons at 2002+ — caught only by the FULL legacy guard). The
working fix is a new `primaryCondition` alternative gated by a semantic predicate `boolExprAhead()`
(CobolParserCoreBase) that scans for a B-operator ahead of the condition boundary; a normal comparison returns
false and falls to `comparisonExpression` UNCHANGED (the predicate prunes at parse time and never enters the
shared rule's static DFA). The binder `BindPrimaryBoolean` unwraps a B-op-free relation operand to its normal
binding. Guard re-green (556 integration, the 31 all pass). As-built vs the design:
- **Diagnostic band = COBOLNET1511** (NOT 0898 — increment 1 consumed 0898 for the VALUE band; the C1 conflict
  resolved as recorded). 1511 covers: non-boolean operand (§8.8.2), both-ALL rule 4, F2 receiver-not-boolean
  (SR2), ROUNDED/SIZE-ERROR on F2, solely-ALL RHS (SR3), ordering/mixed-class boolean relation (§8.8.4.2.2),
  boolean COMPUTE receiver mix.
- **Runtime `CobolBool`** (And/Or/Xor/Not/Equal/IsTrue/Resize + the `…All` figurative forms) — rule-9 right-zero-
  extension, rule-10 result length; 28 unit facts (`CobolBoolTests`) against the Annex A oracle.
- **The bound channel** (`BoundBoolExpr` + `BoundBoolOperand`/`BoundBooleanCondition`/`BoundComputeBoolean`),
  binder (`StatementBinder.Boolean.cs`), emitter (`BooleanRenderer.cs`) all per the design; the total-walk
  registrations (BoundStores/Exceptions/ConditionRenderer) landed. C2 confirmed: the item↔item boolean relation
  branch already zero-extended (increment 1); increment 2 added `CobolBool.Equal` for the expression channel and
  taught `CheckedRelational.IsBoolOperand` to recognize `BoundBoolOperand`. C3: `ALL BOOLLIT` in
  figurativeConstant landed (residue #17 closed).
- **Grammar**: the 4 lexer tokens + `_dataNameTokens` + `cobolWord` + `subToken`(N/A) + the `booleanExpression`
  tier (`{is2002()}?`-gated) + the `computeStatement` F2 alt + the `comparisonExpression` boolean alt
  (`booleanExpression (comparisonOperator booleanExpression)?`) + `evaluateSubject` + reserved-word funnel +
  registry `boolean-operators-2002`/`user-word-b-and-2002` + matrix rows + EditionGateHints. FULL legacy guard
  on the shared .g4 change.
- **⚠ RESIDUE (deferred, documented)**: (a) the **UNPARENTHESIZED top-level boolean condition** `IF a B-AND b`
  / `IF a B-AND b = c` is an ANTLR operand-vs-condition ambiguity that resolves only under a clean follow
  context — so a boolean expression in a CONDITION must be **PARENTHESIZED** this increment (`IF (a B-AND b)`,
  `IF (a B-AND b) = c`); COMPUTE needs no parens. (b) the **85-rejection of a boolean COMPUTE gives a generic
  parse error**, not the friendly COBOLNET0900 (the EditionGateHints token-map fires on the B-op token in an IF,
  but the COMPUTE-F2-dead-at-85 path errors at `COMPUTE`); the rejection IS correct, only the message is generic.
  (c) intrinsic/UDF operands inside a boolean expression, EVALUATE boolean-subject THRU ranges, the 2023 shift
  operators, and BX"…" hex booleans stay named residue below.
- Battery: conformance **1782+** (the `boolean_ops` golden byte-exact + `BooleanOperatorTests` ×15) · unit
  **187** (incl. CobolBool ×28) · legacy ConformanceTests green (`boolean_ops` GreenfieldOnly — legacy has no
  boolean expressions) · FULL legacy guard on the .g4 change.

### M2-DATA-4 / track (a) — increment 2: BOOLEAN OPERATORS B-AND / B-OR / B-XOR / B-NOT — DECISION-COMPLETE DESIGN (recon wave 2026-07-05; grammar pre-authorized; ready to implement AFTER the boolean-data increment)

> Scope: the ISO §8.8.2 boolean-expression machinery over the boolean-DATA substrate the parallel M2-DATA-4
> increment lands FIRST (PIC 1 / USAGE-BIT-as-display items stored as '0'/'1' strings, `B"…"` literals,
> MOVE/compare/VALUE/INITIALIZE). This increment adds: the four operators with §8.8.2 precedence and
> parenthesization; COMPUTE Format 2 (boolean-compute, §14.9.8); the boolean relation condition (Format 2,
> §8.8.4.2.2); the simple boolean condition (§8.8.4.3) in IF / PERFORM UNTIL / SEARCH WHEN / EVALUATE via the
> generic condition path; EVALUATE boolean-expression subjects/objects (§14.9.13); the `boolean-operators-2002`
> gate + matrix row + EditionGateHints; runtime `CobolBool`; golden `boolean_ops` (GreenfieldOnly — legacy has
> ZERO B-op support). 2023 shift operators, BY CONTENT expression arguments, BX literals, `&`-concat and the
> compile-time directive expressions stage LOUD (named residue below). **HARD DEPENDENCY: lands after the data
> increment flips `boolean-data-2002` active** (the matrix-row source and every golden use PIC 1 items).

- **Spec (ISO 2023; specs/ISO_COBOL.md :line anchors).** Operator catalog §8.7.2 (:8865–8878 — binary B-AND
  conjunction / B-OR inclusive / B-XOR exclusive disjunction; unary B-NOT; the ":8878 '-NOT'" is an OCR artifact,
  the word is B-NOT per :9343/:10340). Boolean expressions §8.8.2 (:9323–9420): operand forms :9325–9334
  (boolean-item identifier, boolean literal, figurative ZERO, `ALL boolean-literal`, B-NOT-prefixed, binary
  combination, parenthesized — shift forms are 2023 residue); formation rules 1–3 (:9338–9356 begin/end/balanced
  parens) + Table 4 adjacency (:9370–9382) — enforced STRUCTURALLY by the tier grammar; rule 4 (:9364) both
  operands of a binary op shall not both be `ALL literal`; precedence rule 7 (:9384–9406): parens innermost-first,
  then 1st B-NOT, 2nd B-AND, 3rd B-XOR, 4th B-OR, equal precedence left-to-right; **rule 9 (:9416)** ops performed
  without regard to usage, equal lengths combine positionwise left→right, **unequal lengths ⇒ the shorter operand
  is treated as extended on the RIGHT with boolean zeros — no error, no EC**; NOTE 2 (:9418) lengths are
  per-operation in evaluation order, zero-length operands ⇒ zero-length result; **rule 10 (:9420)** each
  operation's result length = the LARGER item referenced in that operation (B-NOT ⇒ its operand's length).
  COMPUTE §14.9.8 (:26538–26606): Format 2 (:26558–26560) `COMPUTE {identifier-2}… = boolean-expression-1
  [END-COMPUTE]` — **no rounded-phrase, no [NOT] ON SIZE ERROR at the format level**; SR2 (:26573) identifier-2 =
  elementary boolean item; SR3 (:26575) the expression shall not consist solely of `ALL literal`; **GR3
  (:26604–26606)** the stored value's boolean positions = "the number of boolean positions in the largest boolean
  ITEM referenced in the expression" — this can DIFFER from the root operation's rule-10 width (literal-only
  larger sides don't count; a ref-mod operand is its own §8.4.3.3 unique data item at the ref-mod length), and it
  is observable only through the store (JUSTIFIED interplay) — encode GR3 as written. Storing §14.6.8.6
  (:24303–24308): category-boolean receivers left-aligned, zero-fill or truncate on the RIGHT (JUSTIFIED per
  §13.18.32 follows the data increment's posture). Boolean relation §8.8.4.2.2 (:9566–9581): two boolean
  expressions, **equality/inequality ONLY** (`IS [NOT] EQUAL TO / IS [NOT] = / IS <>`); comparison semantics
  §8.8.4.2.8 (:9683–9689): positionwise, usage-independent, unequal lengths ⇒ shorter zero-extended right, two
  zero-length operands EQUAL. Simple boolean condition §8.8.4.3 (:9795–9817): `[NOT] boolean-expression-1`; SR1
  (:9810) the expression shall reference only boolean items of LENGTH 1; GR1 (:9815) true iff the result is 1
  (bind-check literals to length 1 too — GR1's binary premise); it is a §8.8.4.2.1 simple condition (:9493) ⇒
  flows into IF/UNTIL/SEARCH/EVALUATE-WHEN-condition through the generic conditional-expression rules — no
  per-statement work. EVALUATE §14.9.13: boolean-expression subject :27164 / `[NOT] boolean-expression-2` object
  :27176; SR4 (:27206) range operands shall NOT be class boolean; SR6a–d (:27212–27218) a length-1 boolean
  expression paired with TRUE/FALSE reclassifies as a boolean CONDITION; SR7a + Table 15 (:27230/:27244–27260)
  pairing; GR1/GR3a/GR3d (:27271/:27277/:27283) sole-literal/sole-identifier stay literal/identifier. Figurative
  `ALL boolean-literal` materializes against the other operand (§8.3.3.6.4 GR2). Truth tables + worked COMPUTEs:
  Annex D.10 (:44534–44625, Table A.2 :44594–44605) — the unit-test oracle. **No boolean-operator EC exists**
  (§8.8.2 defines none; F2 has no SIZE ERROR ⇒ no EC-SIZE path); EC-DATA-INCOMPATIBLE (§14.6.13.2 r1 :24869) and
  EC-BOUND-REF-MOD ride existing/residue machinery.
- **Editions.** B-AND/B-NOT/B-OR/B-XOR reserved 2002/2014/2023, NOT 85 (§8.9 :10339–10341/:10347; absent from
  Annex E.2 item 25's 2023-new list :49320–49344 ⇒ pre-2014; catalogued M2 = **2002 introduction** per the VCR
  preamble derivation rule, VERSION_CHANGE_REFERENCE.md:20). `ReservedWords.Table.cs:43-50` ALREADY has all four
  rows (R85=false, high confidence) + the four 2023-only B-SHIFT rows. **Operators gated `{is2002()}?`; the words
  are USER-defined at 85** — the exact shipped XOR pattern (DEVLOG 596), inverted edition (2002 not 2023).
  **Do not conflate** with logical `XOR`/`EXCLUSIVE-OR` (2023, §8.8.4.9, VCR row 41 — already gated): those
  combine condition TRUTH VALUES; B-ops combine boolean BIT-STRING values. No evidence of any 2002→2014 operator
  change (Annex E covers 2014→2023 only) — treat as edition-continuous 2002→2023 with the standard
  `vcr: "2002 introduction (derive from the 2002 standard)"` caveat.
- **Grammar plan (pre-authorized; ALL edits keep the 85 parse surface bit-identical — every new alternative is
  head-gated `{is2002()}?` so prediction kills it instantly at 85/NIST).**
  (1) **Lexer** (`Grammar\Core\CobolLexer.g4`, the hyphenated band :116–135, beside `EXCLUSIVE_OR` :120):
  `B_AND : 'B-AND' ;  B_OR : 'B-OR' ;  B_XOR : 'B-XOR' ;  B_NOT : 'B-NOT' ;` — maximal munch safe (`B-AND-MASK`/
  `B-ORDER` stay IDENTIFIER by longer match; `BOOLLIT` :596–598 disjoint — next char is a quote).
  (2) **`_dataNameTokens`** (:30–62, beside `XOR, EXCLUSIVE_OR` :38): add the four (85 user table `B-AND(3)` must
  trigger SUBSCRIPT mode — the whitelist mirror rule :19–29).
  (3) **`cobolWord`** (`CobolParserCore.g4:25-98`, beside XOR/EXCLUSIVE_OR :45-46): add the four with the comment
  "user words at 85, funnel-0901'd ≥2002; keyword occurrences parse only through the gated operator tier — never a
  name slot".
  (4) **New boolean tier** (CobolExpressions.g4, after the condition section — precedence per §8.8.2 rule 7b):
  ```
  // ── COBOL-2002 boolean expressions (ISO §8.8.2; precedence B-NOT > B-AND > B-XOR > B-OR, rule 7b).
  // Permissive-superset doctrine: operand SHAPES (boolean item / boolean literal / ZERO / ALL B"…") are
  // enforced at bind (COBOLNET0898); the tiers enforce formation rules 1–3 + Table 4 structurally.
  booleanExpression : booleanXorTerm ( {is2002()}? B_OR booleanXorTerm )* ;
  booleanXorTerm    : booleanAndTerm ( {is2002()}? B_XOR booleanAndTerm )* ;
  booleanAndTerm    : booleanFactor  ( {is2002()}? B_AND booleanFactor )* ;
  booleanFactor     : {is2002()}? B_NOT booleanFactor
                    | {is2002()}? LPAREN booleanExpression RPAREN
                    | valueOperand ;
  ```
  (paren alt ordered BEFORE valueOperand so `(A B-AND B)` predicts the boolean paren; `(A)` alone still reaches it
  first but reduces identically — bind treats both shapes as one).
  (5) **`comparisonOperand`** (:115–117): `comparisonOperand : valueOperand | {is2002()}? booleanExpression ;` —
  order preserves every existing accessor/prediction; a B-op-containing operand is unviable as alt 1 (B_AND is in
  no follow set) so full-context prediction picks alt 2. This ONE edit delivers the boolean relation (both
  operands), the simple boolean condition (the bare-operand comparisonExpression path), abbreviated-relation
  operands, and EVALUATE WHEN condition-path objects. ⚠ DFA watch-item: alt 2 shares the valueOperand prefix (the
  signCondition lesson :55–57) — safe at 85 (dead predicate), measure conformance parse time at 2002+ in the
  battery gate; escalate if growth appears.
  (6) **`computeStatement`** (CobolParserCore.g4:869-871) — two alternatives, alt-1 subtree shape UNCHANGED (the
  legacy binder's `arithmeticExpression()` accessor keeps working untouched):
  ```
  computeStatement
      : COMPUTE computeStore+ EQUALS arithmeticExpression computeOnSizeError? END_COMPUTE?   // F1 (§14.9.8)
      | {is2002()}? COMPUTE computeStore+ EQUALS booleanExpression computeOnSizeError? END_COMPUTE?  // F2 (boolean-compute)
      ;
  ```
  `COMPUTE X = A` (sole identifier) deterministically picks alt 1 — the binder re-routes a boolean-receiver/
  boolean-RHS Format-1 tree to the boolean bind (the SET-object-reference "ANTLR alternative-order reality"
  precedent, ConstructDialectStatus.cs:130). Alt 2 keeps `computeStore+`'s roundedPhrase and the size-error tail
  parseable; bind rejects both (F2 format :26558–26560) for co-equal diagnostics.
  (7) **`evaluateSubject`** (Core/CobolControlFlow.g4:83-86): add `| {is2002()}? booleanExpression` as the LAST
  alternative (sole identifiers keep alt 2; only B-op-containing subjects reach it). Objects need NO grammar edit —
  `evaluateWhenItem : … | condition` reaches the new comparisonOperand alternative.
  (8) **`figurativeConstant`** (CobolExpressions.g4:291-305): add `| ALL BOOLLIT` (§8.8.2 :9331 / §8.3.3.6.4 —
  COORDINATE: one line, either increment may land it first; do not duplicate).
  (9) **Regen both OSes** (Generated/ is build output — feedback_commit_generated_parser); **FULL legacy guard
  (`scripts/guard.sh`) MANDATORY** — the grammar is shared (CobolSharp.Compiler.csproj:25); NIST runs at `--nist`
  ⇒ 85 where every gate is off and the four words behave as user words (identical to today's IDENTIFIER behavior
  once steps 2–3 land).
- **Reserved-word funnel** (`Validation\EditionValidator.cs`): add `CobolLexer.B_AND, CobolLexer.B_OR,
  CobolLexer.B_XOR, CobolLexer.B_NOT` to `CheckedTokenTypes` (:268–287, beside XOR :276) — position-blind safe by
  the XOR argument (keyword occurrences parse only through the gated operator tiers, never a name slot). Funnel →
  `ReservedWordSet.RejectsAt` → 0901 at ≥2002; table rows already exist. NO table edits needed.
- **Bound nodes** (Bound/BoundTree.cs — a new value channel; boolean values are '0'/'1' strings, never numerics):
  `public abstract record BoundBoolExpr;` with `BoundBoolLiteral(string Bits)` (B"1010" decoded),
  `BoundBoolRef(Place Place)` (a category-Boolean item, incl. static ref-mod), `BoundBoolAll(string Bits)`
  (figurative `ALL B"…"`; figurative ZERO normalizes to `BoundBoolAll("0")` at bind; `B-NOT ALL …` constant-folds
  to the flipped `BoundBoolAll` — ALL is positionless), `BoundBoolBinary(BoundBoolExpr Left, char Op,
  BoundBoolExpr Right)` (Op ∈ '&','|','^'), `BoundBoolNot(BoundBoolExpr Operand)`, `BoundBoolError(string
  Feature)`. Plus: **`BoundBoolOperand(BoundBoolExpr Expr) : BoundOperand`** — the ONE relation-operand carrier
  (never a parallel relational node; the item↔item compares of the data increment ride the SAME
  `BoundRelational` + renderer branch — feedback_singular_pattern); **`BoundBooleanCondition(BoundBoolExpr Expr)
  : BoundCondition`** (§8.8.4.3 — negation composes via the existing `BoundNot`); **`BoundComputeBoolean(
  BoundBoolExpr Rhs, IReadOnlyList<Place> Targets, int Gr3Width) : BoundStatement`** (Gr3Width computed at BIND =
  max static boolean positions over items referenced, §14.9.8 GR3; all-literal expressions use the root
  operation's rule-10 width — GR3 is silent, documented implementor reading; a DYNAMIC-length ref-mod operand
  stages 0898 loud this increment).
- **Binder** (new partial `Bound\StatementBinder.Boolean.cs` + touch points):
  `BindBoolExpr(Core.BooleanExpressionContext)` walks the tiers left-to-right (rule 7c); operand resolution from
  `booleanFactor`/`valueOperand`: BOOLLIT → decode (ONE shared decoder with the data increment); figurative
  ZERO / `ALL BOOLLIT` → `BoundBoolAll`; sole dataReference → resolve; **category must be `PicCategory.Boolean`**
  else 0898 (§8.8.2 operand list); any non-sole-ref arithmeticExpression / STRINGLIT / other figurative → 0898.
  Rule-4 both-ALL binary ⇒ 0898 (:9364). `BindCompute` (:596–601): if `compute.booleanExpression()` non-null ⇒
  `BindComputeBoolean` (receivers: elementary boolean per SR2 else 0898; roundedPhrase present ⇒ 0898; size-error
  phrase present ⇒ 0898; SR3 solely-ALL ⇒ 0898); ALSO the F1 re-route: an alt-1 tree whose RHS is a sole
  boolean-category ref OR whose first receiver is boolean re-routes to the boolean bind (mixed
  boolean/non-boolean receivers ⇒ 0898). `BindComparison` (:1264+): add `IsBoolOperand` beside `IsPtrOperand`
  (:1323–1324) — true for `BoundBoolOperand` or a `BoundFieldOperand` with Category Boolean; boolean sides admit
  only `==`/`!=` (0898 — §8.8.4.2.2 Format 2 is equality-only; **the grammar/seams brief's "ordinal over
  positions" note is WRONG — flagged below**); both sides must be boolean-valued (boolean expr / item / literal /
  ZERO / ALL) else 0898. `ComparisonOperand` (:1370) + `BindValueOperand` (StatementBinder.Evaluate.cs:155): add
  the `operand.booleanExpression()` arm → `BoundBoolOperand`, and bind a BOOLLIT literal as
  `BoundBoolOperand(BoundBoolLiteral)` (zero-extension semantics, NOT `BoundStringLiteral` — space-padding is
  wrong; coordinate with the data increment). Bare-operand simple boolean condition: in `BindComparison`'s
  `operands.Length == 1` branch (:1343–1364), AFTER the 88-name and switch-status resolutions (NC211A order),
  a sole boolean expression / boolean-category ref / BOOLLIT binds `BoundBooleanCondition` with the §8.8.4.3 SR1
  bind check (every referenced item AND literal length 1, else 0898). EVALUATE
  (`StatementBinder.Evaluate.cs`): `SubjectCondition`/`BindWhenItem` gain the boolean-subject leg —
  `subject.booleanExpression()` binds as a value subject `BoundBoolOperand`; TRUE/FALSE objects against a
  length-1 boolean subject reclassify per SR6a/b to `BoundBooleanCondition`(±`BoundNot`); operand objects pair as
  `BoundRelational(subj, "==", obj)`; `valueRange` objects with a boolean side ⇒ 0898 (SR4 :27206). **UDF guard:**
  NO `BindFlatSequence` change — B-ops are expression-channel, and §8.8.2 rule 7 has no short-circuit, so the
  hoisted-activation model is inherently safe INSIDE a boolean expression; a boolean expression sitting in a
  non-first AND/OR condition operand already trips the existing COBOLNET1509 guard via `_udfPendingCalls`
  (Udf.cs:220–222) — document at the guard.
- **TOTAL-walk checklist** (every exhaustive switch the new nodes visit): `BoundStores.StoreKindOf`
  (BoundStores.cs:47-182) — **add the `BoundComputeBoolean` arm** (Targets → Write; a miss returns null = loud
  staging, DEVLOG 607 rule); `StatementBinder.Exceptions.cs` — `ContainsIntrinsic` (:388–408) add
  `BoundComputeBoolean => BoolExprHasIntrinsic(Rhs)`; `OpHasIntrinsic` (:410–414) add `BoundBoolOperand`;
  `CondHasIntrinsic` (:425–432) add `BoundBooleanCondition`; new `BoolExprHasIntrinsic` walk (intrinsic operands
  are residue but the walk must be total from day one); `ConditionRenderer.Render` (:16–36) — add
  `BoundBooleanCondition` arm; `RenderRelational` (:38–76) — boolean branch (below); `NumericRenderer.AsNum` /
  `IntrinsicRenderer.NumStaticExpr` / `CheckComposite.OfExpr` — NO arms (BoundBoolExpr is a separate root type
  that never enters the numeric channel; the LoudValue defaults are the correct staging — note in code);
  `OperandText.AsString/IsString` (:17–47) — `BoundBoolOperand` correctly hits the loud default / returns false
  (a boolean EXPRESSION is not a DISPLAY/MOVE operand — MOVE takes identifiers/literals only; note in code).
- **Emitter + runtime.** New `CodeGen\Emit\BooleanRenderer.cs`: `Render(BoundBoolExpr)` → side-effect-free C#
  over `CobolBool` — `BoundBoolBinary` ⇒ `CobolBool.And/Or/Xor(l, r)`; an ALL side (rule 4 guarantees at most
  one) ⇒ `CobolBool.AndAll/OrAll/XorAll(concrete, bits)` (no double-render of the concrete side — intrinsic/UDF
  operands must evaluate once); `BoundBoolNot` ⇒ `CobolBool.Not(…)`; ref ⇒ `Place.Read()`; literal ⇒
  `EmitText.CsLiteral(bits)`; `BoundBoolError` + default ⇒ `EmitText.LoudValue("string", …)`.
  `ConditionRenderer.RenderRelational`: boolean branch **after** the pointer branch and **before** the
  figurative/string branches (boolean comparison zero-extends, `CobolString.Compare` space-pads — §8.8.4.2.8) —
  `CobolBool.Equal(BoolRead(l), BoolRead(r))` (ALL side ⇒ `EqualAll`), negate for `!=`. `Render`:
  `BoundBooleanCondition b => $"CobolBool.IsTrue({bool.Render(b.Expr)})"`. `CSharpEmitter`: dispatch arm
  `case BoundComputeBoolean` beside `BoundCompute`; `EmitComputeBoolean` — render RHS once,
  `CobolBool.Resize(rhs, Gr3Width)` (GR3), materialize a temp when Targets.Count > 1 (the §14.7.7-GR4-shaped
  EmitCompute precedent :856–869), store each via the data increment's ONE boolean store rule (§14.6.8.6
  left-align / zero-fill / truncate right — if the data increment inlined it at MOVE sites, factor
  `CobolBool.Fit(value, width)` and make both use it; feedback_singular_pattern). **New
  `src\Cobol.Net.Runtime\Text\CobolBool.cs`** (beside CobolString.cs/CobolClass.cs, sibling namespace):
  `And/Or/Xor(string a, string b)` — rule 9 right-zero-extend shorter to max length, positionwise, result length
  = max (rule 10); `AndAll/OrAll/XorAll(string a, string bits)` — bits repeated/truncated to `a.Length`
  (§8.3.3.6.4 GR2) then combined; `Not(string a)` — flip, length preserved; `Equal(string a, string b)` —
  §8.8.4.2.8 zero-extension, two empty strings EQUAL (:9689); `EqualAll(string a, string bits)`;
  `IsTrue(string a)` ⇒ `a == "1"` (§8.8.4.3.4 GR1); `Resize(string v, int w)` — right zero-fill / right
  truncate. Zero-length operands flow naturally as `""` (NOTE 2 :9418). No EC raise points (none exist —
  see Spec bullet); non-0/1 content = the §14.6.13.2 "undefined result" license, proceeds charwise (the
  EC-DATA-INCOMPATIBLE bridge is residue #8).
- **Diagnostics.** **NEW band COBOLNET0898** — the boolean-expression/boolean-compute constraint band,
  message-differentiated (the 0869 pointer-band precedent): non-boolean operand (§8.8.2 operand list); both-ALL
  (rule 4 :9364); F2 receiver not elementary boolean (SR2 :26573); ROUNDED / [NOT] ON SIZE ERROR on F2
  (:26558–26560); solely-ALL RHS (SR3 :26575); ordering operator or mixed-class boolean relation (§8.8.4.2.2 /
  §8.8.4.2.1); simple-boolean-condition operand not length 1 (SR1 :9810); EVALUATE boolean range operand (SR4
  :27206); mixed boolean/non-boolean COMPUTE receivers; dynamic-length ref-mod GR3 width (staged). **0898 is the
  LAST free 08xx code** (0814/0819/0844 are retired per the M2-DATA-5 allocation note; grep confirms 0801–0903
  otherwise dense) — ⚠ CONFLICT-1 below coordinates it with the parallel data increment. Next-free 15xx = 1511 —
  NOT needed here (no new intrinsic/UDF interplay; BOOLEAN-OF-INTEGER/INTEGER-OF-BOOLEAN stay catalog-Deferred,
  IntrinsicCatalog.cs:124/:143). Introduction gating = 0900 via the registry row; reserved words = 0901 via the
  funnel; NO binder-side `ConstructRegistry.Check` (the XOR precedent: grammar predicate + parse-layer hint ARE
  the gate — at 85 the binder is unreachable).
- **Registry / matrix / hints (SAME commit — the drift test asserts both directions).**
  `ConstructDialectStatus.cs` (beside `logical-xor-operator-2023` :135): `new("boolean-operators-2002",
  "the boolean operators B-AND/B-OR/B-XOR/B-NOT (boolean expressions)", 2002, null, null,
  EditionCodes.Introduction, "ISO §8.7.2/§8.8.2; COMPUTE F2 §14.9.8; boolean relation §8.8.4.2.2; simple boolean
  condition §8.8.4.3; {is2002()}?-gated operator tiers + W1.5 parse-layer 0900 mapping; 2002 introduction (derive
  from the 2002 standard — Annex E covers 2014→2023 only)")` + ONE representative interval row
  `new("user-word-b-and-2002", "the word B-AND as a user-defined word", 85, 2002, null,
  EditionCodes.ReservedWord, "§8.9 interval encoding: user-definable at 85, reserved since 2002
  (ReservedWords.Table rows cover all four; single representative — the user-word-raising-2002 precedent)")`.
  `tests/version-matrix/constructs.json`: mirror both rows (active), `expectDiagnostic: "COBOLNET0900"` /
  `"COBOLNET0901"`, embedded `source` programs — the operator row's source declares two PIC 1(4) items and
  executes `COMPUTE R = A B-AND B` (0900 at 85 via the hint; compiles clean at 2002+ — hence the data-increment
  dependency); the user-word row declares `01 B-AND PIC 9.` (clean at 85, 0901 at ≥2002). Refresh the
  `boolean-data-2002` row descriptions (registry :109 + json :419–427) to drop "boolean OPERATIONS ride D4"
  staleness in the same commit the operators land. `EditionGateHints.cs`: `private static readonly Gate
  BooleanOps = new("the boolean operators B-AND/B-OR/B-XOR/B-NOT", 2002, "ISO §8.7.2/§8.8.2 (COMPUTE F2
  §14.9.8; relation §8.8.4.2.2)", "boolean-operators-2002");` + token arm beside :117:
  `CobolLexer.B_AND or CobolLexer.B_OR or CobolLexer.B_XOR or CobolLexer.B_NOT => BooleanOps` — token-type match
  alone (the rule stack has popped; the XOR :114–116 argument transfers exactly: as user words the tokens parse
  through cobolWord and never error, so an error AT the token is the gated operator).
- **Tests / goldens / docs.** NEW golden `tests/conformance/2002/boolean_ops.cob` + `.out` (manifest `enabled`,
  alphabetical; `boolean_data` stays the data increment's). Data: `BA PIC 1(4) VALUE B"1100"`, `BB PIC 1(4)
  VALUE B"1010"`, `BS PIC 1(2) VALUE B"11"`, `B1 PIC 1 VALUE B"1"`, `B0 PIC 1 VALUE B"0"`, `BR PIC 1(4)`,
  `BW PIC 1(6)`. Expected lines (each ISO-derived, shown with its derivation): `AND=1000` (1100∧1010);
  `OR=1110`; `XOR=0110`; `NOT=0011` (B-NOT BA); `PREC=1100` (`BA B-OR BB B-AND BS` — B-AND first: 1010∧(11→1100
  rule-9 extension)=1000; 1100∨1000); `PAREN=0100` (`(BA B-XOR BB) B-AND BS` = 0110∧1100); `EXT=100000`
  (`COMPUTE BW = BA B-AND BB` — GR3 width 4, stored into PIC 1(6) per §14.6.8.6 right zero-fill); `REL=Y`
  (`IF BA B-AND BB = B"1000"`); `NEQ=Y` (`IF BS <> BA B-AND BB` — "11"→"1100" vs "1000"); `SBC=Y`
  (`IF B1 B-OR B0`); `NSBC=Y` (`IF NOT (B1 B-AND B0)` — GR2 :9817); `EVAL=1` (`EVALUATE BA B-XOR BB WHEN
  B"0110"`); `ZED=Y` (`IF BA B-AND ZERO = B"0000"` — figurative ZERO operand :9329); `ALL=Y`
  (`IF BA B-OR ALL B"1" = B"1111"`). **Legacy-runner impact: legacy has ZERO B-op support** (grep-verified —
  only the greenfield reserved table + the legacy CBL2601 boolean-in-arithmetic REJECTION,
  ArithmeticTypeSystem.cs:115-121; its byte-engine boolean support was data-only) ⇒ **`GreenfieldOnly` exclusion
  `("2002", "boolean_ops")` in ConformanceTests.cs:62 in the SAME commit** (feedback_legacy_suite_on_shared_corpus,
  the DEVLOG 618 precedent). NEW unit battery `CobolBoolTests`: the four truth tables against Annex D.10 Table
  A.2 (:44594–44605); rule-9 unequal-length extension both directions; rule-10 result widths; zero-length ⇒
  zero-length (NOTE 2) and zero-length Equal (:9689); AllTo repetition/truncation; Resize; IsTrue. NEGATIVE cases
  (the pointer-wave negative harness, each asserting 0898): bool_op_nonbool_operand, bool_op_arith_operand
  (`A + B` as a B-AND operand), bool_compute_rounded, bool_compute_size_error, bool_compute_all_only (SR3),
  bool_both_all (rule 4), bool_relation_ordering (`<` over boolean operands), bool_condition_len (SR1 length-1),
  bool_compute_nonbool_receiver (SR2). Matrix expectations: the two new rows green at all four editions.
  boolean_ops doubles as the phase demo leg (feedback_demo_per_phase — outputs derived above, not just
  non-crashing). Same-change-set docs: this file's M2-DATA-4 row + track-(a) row, DEVLOG entry, resume-prompt
  STATE banner; COBOLNET_DESIGN's boolean section gets the CobolBool/BoundBoolExpr model + the GR3-vs-rule-10
  width distinction (feedback_follow_design_docs_and_spec).
- **Implementation order + battery gates** (each step ends green before the next): **(0)** PRE: the boolean-data
  increment merged (`boolean-data-2002` active; '0'/'1' storage + BOOLLIT decode live). **(1)** Lexer tokens +
  `_dataNameTokens` + `cobolWord` + `CheckedTokenTypes` + regen → full greenfield battery + **FULL legacy guard**
  (85 surface must be invariant: the four words behave as user words end-to-end). **(2)** Grammar tiers +
  comparisonOperand/computeStatement/evaluateSubject/figurativeConstant edits + EditionGateHints + registry/json
  rows → battery + FULL legacy guard + the DFA watch-item timing check; empirically verify the two prediction
  claims (`COMPUTE X = A B-AND B` picks alt 2; `WHEN B1 B-AND B2` picks the condition item). **(3)** Binder
  (nodes + BindBoolExpr + BindComputeBoolean + relation/condition/EVALUATE seams + 0898 checks + total-walk
  arms) → battery. **(4)** CobolBool + BooleanRenderer + emitter arms + CobolBoolTests → battery. **(5)** Golden
  + GreenfieldOnly exclusion + negatives + manifest + matrix activation → full battery + legacy conformance +
  FULL legacy guard. **(6)** Docs + DEVLOG + commit/push (feedback_fully_autonomous_push).
- **STAGED RESIDUE (named, loud). [AS-BUILT ADDITIONS: (0a) CLOSED (DEVLOG 622) — the boolean CONDITION/RELATION
  forms (`IF a B-AND b`, `IF (a B-AND b) = c`) now work via the `boolExprAhead()`-gated `primaryCondition` alt,
  zero regression. (0b) the 85-rejection of a boolean COMPUTE now emits COBOLNET0900 (the EditionGateHints
  COMPUTE-token arm with a B-op lookahead, DEVLOG 621) — CLOSED.]**
  (1) **B-SHIFT-L/R/LC/RC** (2023-only — §8.7.2 :8880–8885; rules 5/8/9
  :9366/:9408–9416; contextual precedence :9395; Table 4 shift row :9376 no-paren/no-B-NOT after a shift; VCR
  rows 9 [gate TODO]/32): NOT tokenized — they lex as IDENTIFIER ⇒ loud parse error at the operator position;
  user-word misuse 0901s at 2023 via the existing table rows :46–49. The tier design pre-plans the slot:
  contextual precedence = a shift repetition inside EACH tier. (2) **CALL/INVOKE `BY CONTENT
  boolean-expression`** (§14.9.4 :26060, SR17 :26126, GR :26165/:26237; §14.9.23 :28381, SR21 :28477) —
  grammar-absent ⇒ loud parse error (the M2-DATA-5 grammar-absent-residue precedent). (3) **Inline
  method-invocation boolean args** (§8.4.3.4.2 :7121–7140) — rides the inline-invocation residue. (4)
  **Compile-time boolean expressions** in >>DEFINE/>>EVALUATE/>>IF (§7.3.7 :3833 SR1 :3844; §7.3.8; :4051+/
  :4221+) — already a documented deferral, ConditionalCompilationProcessor.cs:20–21. (5) **BX"…" literals**
  (lexer comment :595) stay a residue; **`&` boolean concatenation** (§8.8.3 :9429–9450) is ✅ LANDED — P10
  Step 14 (2026-07-16): a compile-time fold (`Binding/ConcatFolder.cs`, §8.8.3.3 GR3) incl. the boolean leg
  (`B"01" & B"10"` folds to `B"0110"` and routes the boolean relation channel); `concat-operator-2002` ACTIVE. (6) **BOOLEAN-OF-INTEGER / INTEGER-OF-BOOLEAN / LENGTH-over-boolean** (§15; catalog Deferred rows
  IntrinsicCatalog.cs:124/:143 — already loud). (7) **Dynamic-length ref-mod operand in F2's GR3 width** — 0898.
  (8) **The boolean EC-DATA-INCOMPATIBLE bridge** (§14.6.13.2 r1 :24869 — non-0/1 sender via REDEFINES/aliasing;
  unchecked = the "undefined result" license, charwise). (9) **USAGE BIT positions under ref-mod** (GR5a :7083)
  — coordinate with the data increment's BIT residue. (10) **Strongly-typed groups containing boolean in =/<>**
  (§8.8.4.2 SR4 :9612) — rides the TYPEDEF residue. (11) **Boolean operands in ARITHMETIC contexts** (legacy
  CBL2601 precedent) — verify the data increment's category checks cover `ADD B1 TO X`; add to ITS residue if not.
- **ANCHOR LIST.** Grammar: CobolLexer.g4:116–135 (:120 model), :30–62 (:38), :596–598; CobolParserCore.g4:25–98
  (:45–46), :869–871; CobolExpressions.g4:24–27, :55–57 (DFA lesson), :63–109 (:72–74 XOR model), :115–123,
  :264–270, :291–305; Core/CobolControlFlow.g4:77–108 (:83–86 subject, :103–108 whenItem); predicates
  CobolParserCoreBase.cs:19–22. Validation: EditionValidator.cs:268–287, :301–336; ReservedWords.Table.cs:43–50;
  ConstructDialectStatus.cs:58–152 (:109, :135–137), :166–186; EditionGateHints.cs:55, :114–117, :135–138.
  Binder: StatementBinder.cs:596–601, :1204–1218, :1239–1251 (:1250), :1264–1367 (:1307–1309, :1323–1336,
  :1343–1364), :1370–1383; StatementBinder.Evaluate.cs:26–164 (:77–113, :119–139, :155–164);
  StatementBinder.Udf.cs:220–222; StatementBinder.Exceptions.cs:388–432; BoundTree.cs:90–210, :277;
  BoundStores.cs:47–182. Emit: ConditionRenderer.cs:16–76; CSharpEmitter.cs:852–876; OperandText.cs:17–47;
  NumericRenderer.cs:24–62; IntrinsicRenderer.cs:271. Runtime: src/Cobol.Net.Runtime/Text/ (new CobolBool.cs).
  Tests: constructs.json:419–427, :614; manifest.json:46–47; ConformanceTests.cs:62/:108. Spec: :8865–8885,
  :9323–9420, :9566–9581, :9683–9689, :9795–9817, :10339–10347, :23254, :24303–24308, :24869–24881,
  :26538–26606, :27153–27283, :49320–49344, :44534–44625. Legacy: ArithmeticTypeSystem.cs:67–74/:115–121;
  CobolSharp.Compiler.csproj:25.
- **CONFLICTS FLAGGED (brief-vs-brief / parallel-change-set).** (C1) **COBOLNET0898 contention — RESOLVED at the
  increment-1 merge (DEVLOG 619/620): increment 1 CONSUMED 0898** for the VALUE-clause category-mismatch band
  (§13.18.63 SR5/SR10, both directions). So **increment 2 must pick a DIFFERENT code** for the
  boolean-expression/boolean-compute constraint band — 08xx is otherwise dense, so use the **15xx band
  (next-free 1511)** for the boolean-op constraints, OR reuse 0844 (the operand/relation-misuse band increment 1
  established — a good fit for "ordering operator on boolean operands" / "non-boolean operand in a boolean
  expression"). Decide at implementation; update every "0898" in this increment-2 design to the chosen code. (C2) **The boolean relation branch is ONE branch**: the
  data increment's item↔item compares MUST route through `CobolBool.Equal` (zero-extension, §8.8.4.2.8 :9683),
  never `CobolString.Compare` (space-padding gives wrong verdicts); whichever change set lands first creates the
  `RenderRelational` boolean branch. **AS BUILT (increment 1):** the boolean item↔item branch already exists and
  zero-extends CORRECTLY via `CobolString.Compare(l, r, pad: '0')` (D-B1: a boolean position IS a '0'/'1' char,
  so the boolean-zero pad IS §8.8.4.2.8 zero-extension). Increment 2 adds `CobolBool` + the `BoundBoolOperand`
  expression path; for pure item/literal operands it may keep that path or unify on `CobolBool.Equal` (both
  zero-extend). The "space-padding is wrong" hazard was already avoided.
  (C3) **`ALL BOOLLIT` in figurativeConstant** — NOT landed by increment 1 (still residue #17); increment 2
  lands the one .g4 line. **The BOOLLIT/NATLIT decoder DID land** (increment 1: the three
  `DecodeCobolString`/`DecodeString` twins) — increment 2 REUSES it, does not re-add.
  (C4) **The grammar/seams brief's COMPUTE citation "§14.9.7" is wrong** — boolean-compute is §14.9.8 Format 2
  (:26545–26560, spec-verified). (C5) **The grammar/seams brief's "ISO §8.8.4.2 boolean relation = ordinal over
  positions" is wrong** — Format 2 admits equality/inequality ONLY (:9566–9581); the design implements
  `Equal`/negation, no ordering. (C6) **BOOLLIT comparison operands**: if the data increment bound them as
  `BoundStringLiteral`, re-route to `BoundBoolOperand(BoundBoolLiteral)` here (zero-extension again). (C7) The
  data increment's `boolean-data-2002` registry/json descriptions go stale the moment operators land — refreshed
  in this change set (listed above).

## M2-ARITH — arithmetic

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-ARITH-1 | ROUNDED MODE (8) + DEFAULT ROUNDED + PROHIBITED→EC-SIZE-TRUNCATION | partial | **LANDED (DEVLOG 611, track e)** | 8 modes + DEFAULT ROUNDED + the PROHIBITED-inexact EDITED-receiver cure (CobolNum.RescaleChecked; the leak was the edited-store path calling plain Rescale — silent truncation — while the numeric path used TryStore); rounded_mode_prohibited.cob (2014) ENABLED; OnSizeErrorDifferentialTests +4 (COMPUTE/ADD-GIVING/DIVIDE + exact) | none | The fix is in the ONE shared StoreArith edited branch, so it holds across every arithmetic verb. |
| M2-ARITH-2 | Remaining OPTIONS clauses (ARITHMETIC/INTERMEDIATE/FLOAT DEFAULT/ENTRY-CONVENTION) | partial | **PARTIAL→(STANDARD landed, DEVLOG 611)** | STANDARD-DECIMAL + plain STANDARD both route to the CobolDec engine for fixed-point (§8.8.1.2/§8.8.1.4 — identical there; STANDARD removed at 2023 keeps 0807); options_paragraph.cob (2014) ENABLED as a real behavior test (2/7*7 = 2.00000 vs native 1.99997). Residual: FLOAT DEFAULT waits on standard-float types (phase 6); ENTRY-CONVENTION parsed-inert; STANDARD-BINARY documented-unsupported 0806 | (e residual → phase 6) | The float-operand divergence rides the deferred float families. |

## M2-PROC — procedural statements

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-PROC-1 | INITIALIZE …TO VALUE/DEFAULT/WITH FILLER | done | LANDED | **Verified:** InitializeBinder.cs (Binding/Procedure/Verbs/) + InitializeEmitter.cs (CodeGen/Verbs/); initialize_phrases.cob ENABLED | none | Re-implemented greenfield; pre-85 phrases gated 0830-0835. |
| M2-PROC-2 | INSPECT …BACKWARD | done | LANDED | **Verified:** InspectBinder.cs (Binding/Procedure/Verbs/) Backward flag, gated 2023 via COBOLNET0845; inspect_backward.cob ENABLED (2023 manifest) | none | Gated 2023 (not 2002); ISO annex E.3.3 #34. |
| M2-PROC-3 | VALIDATE statement + validation clauses | open | OBSOLETE | **Verified:** zero VALIDATE grammar hits; council decision-3 = documented non-support is conformance-legal (F.2 #5 obsolete) | phase 7 | By design. Residual = a flag-obsolete WARNING row (Phase 7). Currently bare parse error. |
| M2-PROC-4 | Exception handling (RAISE/EC/>>TURN/RESUME/USE AFTER EXCEPTION) | open | LANDED | **Verified:** EcBinder.cs (Binding/Procedure/Verbs/), TurnState.cs (Binding/), EcEmitter.cs (CodeGen/); oo_ec_* ENABLED | none | Catalog stale-open; landed post-catalog. **The -N EC twins LANDED (P10 Step 11, 2026-07-16):** EXCEPTION-FILE-N §15.29 / EXCEPTION-LOCATION-N §15.31 (the ONLY two ISO defines) = `EcFunctions.FileN/LocationN`, the base renderings through the ONE `NationalOf` repertoire translator, result category National; `exception_file_n` ENABLED + `exception-file-n-2002` matrix row + the 85-window negative; the 2023 optional-argument form stays loud (VCR rows 68/69 → PHASE-13 Step 9). |
| M2-PROC-5 | ALLOCATE / FREE (based storage) | open | **LANDED (DEVLOG 617; GR7 complete DEVLOG 858)** | Both ALLOCATE formats (GR1 round-up / GR2 ≤0→NULL / GR6 zero-fill / GR3-GR4 based) + FREE's GR1 three-way (nonfatal EC-STORAGE-NOT-ALLOC through the TurnState-gated block; dangling aliases loud at Deref); pointer_alloc + allocate_initialized ENABLED byte-exact; allocate-2002/free-2002 matrix rows ACTIVE | none | **The based INITIALIZED GR7 lowering LANDED (P10 Step-6 slice, 2026-07-16):** the spec's INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT expansion via `InitializeBinder.BindAllocateInitialized` (a BoundSequence after the allocation); GR6/GR4b now runtime-byte-verified by `allocate_initialized`. |
| M2-PROC-6 | GOBACK RETURNING (done); EXIT variants; CONTINUE AFTER (deferred) | partial | LANDED (+ EXIT FUNCTION) | **Verified:** BoundGoback ReturningSource+Raising (CallBinder.cs, Binding/Procedure/Verbs/), CallEmitGoback; goback_returning.cob ENABLED. **EXIT FUNCTION leg LANDED (Phase 4c):** UdfBindExitFunction → BoundGoback (the §14.9.18.4 GR5 function-return synonym; RAISING tail rides GOBACK's), 0827 placement band, the exit-function-window matrix row flipped ACTIVE with a conforming FUNCTION-ID witness (expectDiagnosticBelow 0900 / 0902 at 2023); udf_exit_function golden proves the early return (X=0014) | phase 7 residue | EXIT SECTION → BoundUnsupported. CONTINUE AFTER not in grammar → Phase 7. |

## M2-PRE — preprocessor

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-PRE-1 | Preprocessor robustness trio (SOURCE FORMAT switch / CC-in-copy / unknown >>) | partial | PARTIAL | Leg (c) landed (ConditionalCompilationProcessor.cs:126-137); (a) first-wins SOURCE FORMAT only; (b) CC-then-COPY order bug (Frontend.cs:88-93) | none | Low-severity follow-ups (WS-2002-FORMAT). (b) = stage-order swap; (a) = per-line format state. |
| M2-PRE-2 | Directive semantics depth (>>TURN toggle / recognize-ignore / >>DEFINE AS PARAMETER / >>CALL-CONVENTION) | open | PARTIAL | >>TURN EC-toggle LANDED (TurnState/StatementBinder.Exceptions); recognize-and-ignore LANDED (KnownIgnoredDirectives); >>DEFINE AS PARAMETER Deferred; >>CALL-CONVENTION inert | none | Load-bearing legs landed. Residual low-value, no named track. |

## M2-FILE — file I/O

### M2-FILE-1 — SHARING / LOCK MODE / RETRY / UNLOCK — ⛔🎉 LANDED (Phase 4d, DEVLOG 623, 2026-07-06)

> **AS-BUILT (deviations from the design below, each keeping the deep-dive current per feedback_follow_design_docs_and_spec):**
> **(1)** The default sharing posture is **"outside the subsystem unless a SHARING/LOCK MODE clause is declared"** (a
> connector is sharing-active only once `RegisterSharing` is emitted for it — files with neither clause keep the legacy
> exclusive path **byte-for-byte**), NOT the design's "every file defaults to NoOther." This achieves the design's
> *stated reason* for the NoOther default (preserve the pre-2002 corpus byte-exact) more conservatively: the whole
> legacy corpus never touches the physical-file registry, so the legacy integration suite + NIST are provably invariant.
> **(2)** The `Locked` HashSet is **retained** for the ≤2014 CLOSE…WITH LOCK/38 path (not merged into
> `PhysicalFileState` — the design's "free shared-path 38 fix" is a deferred cleanup). **(3)** ~~keyed-READ-only
> locking~~ — **CLOSED by P10 Step 8 (2026-07-16):** record locking now runs on EVERY organization and EVERY
> lock-bearing verb — the lock identity is polymorphic on `FileConnector` (`LastReadRecordId` /
> `MutationTargetRecordId` / `LastWrittenRecordId`: RRN / prime key / the sequential record's ORDINAL position),
> the mutating verbs pre-check the record-operation conflict (`WriteShared`/`RewriteShared`/`DeleteShared` —
> §14.9.35 GR11+GR12, §14.9.10 GR6+GR7, §14.9.51 GR10+GR11), the sequential READ pre-checks its next ordinal
> (`ReadShared` — §14.9.30 GR9 with the GR10a FPI-unchanged conflict + GR22 ADVANCING ON LOCK skip-scan), RETRY
> binds+emits on READ/WRITE/REWRITE/DELETE/DELETE FILE (§14.7.9; the general formats give START **no** RETRY or
> lock phrase — §14.9.41 + §12.4.5.9 GR6 excepts START), and DELETE FILE produces **62** when the physical file
> is open by another connector (§9.1.13.9 item 2 / §14.9.10 GR15). **(4)** `RetryLoop` never sleeps: §14.7.9.3 GR2
> clamps the SECONDS period to the A.1 item 166 maximum meaningful value of ZERO, so an exhausted retry
> lands the CONFLICT'S OWN §9.1.13 status under every form except §14.7.9.3 GR3's FOREVER on a RECORD
> conflict, which is the §9.1.13.8 item 2 deadlock '52'. **Evidence:** goldens `file_sharing`
> (`OPEN-A=00/OPEN-B=00/READA=00/READB=51/RETRYB=51/IGN=ALPHA/AFTER=00/EXCL=61`), `file_sharing_seq` (sequential
> ordinals: 51/RETRY/IGNORING/43-after-conflict/GR11a auto-release/REWRITE-WITH-LOCK 51/EXTEND WRITE-WITH-LOCK
> 51/FOREVER→52), `file_sharing_mutate` (REWRITE/DELETE 51 + RETRY + write-lock), `2023/delete_file_sharing`
> (62/62/62/62/62/00/05/35 — every retry form keeps the file-sharing class, §9.1.13.9 defining no deadlock value); `CobolFileLockTests` (18), 4 SR negatives (SR8/SR4/SR2/SR1→1512), 6 matrix rows.
> Runtime: `src/Cobol.Net.Runtime/IO/FileRegistry.cs` + `IO/Sharing/PhysicalFileTable.cs` (the P8 homes — the
> SSOT for this subsystem).

### M2-FILE-1 — SHARING / LOCK MODE / RETRY / UNLOCK — DECISION-COMPLETE DESIGN (synthesis 2026-07-06; grammar pre-authorized; IMPLEMENTED — see AS-BUILT above)

> Scope: the ISO-2002 file-sharing / record-locking subsystem end-to-end — the `SHARING` file-control clause + the `OPEN SHARING` phrase (§12.4.5.15 / §14.9.27), the `LOCK MODE` clause (§12.4.5.9), the `RETRY` phrase (§14.7.9) on OPEN/READ/WRITE/REWRITE/DELETE, the READ record-lock phrases `WITH LOCK` / `WITH NO LOCK` / `IGNORING LOCK` / `ADVANCING ON LOCK` (§14.9.30) + `WITH [NO] LOCK` on WRITE/REWRITE (§14.9.51/§14.9.35), the `UNLOCK` statement (§14.9.47), the six new I-O statuses **51/52/53/54/61/62** (§9.1.13.8/9), and their edition gate + the `COBOLNET1512` SR band. **SYNTHESIS DECISION D1 (below): build the sharing/locking machinery FOR REAL, not stubbed** — §9.1.13.9 defines 61/62 over *"another file connector"* (not another run unit), and two `SELECT`s bound to the same physical file are a realizable, deterministic in-process scenario (§9.1.15 note, spec :11729) — so the physical-file registry, Table-19 open-conflict (61), record-operation-conflict (51), self re-access, single/multiple release, the RETRY loop, UNLOCK, IGNORING/ADVANCING LOCK, and the 53/54 limits are all exercised in one run unit. The genuinely-unrealizable legs (cross-OS-process/cross-run-unit locks, wall-clock RETRY, APPLY COMMIT) are named single-run-unit no-ops with loud guards. The EC/USE/FILE-STATUS machinery downstream is ALREADY complete (the 5x/6x → EC bridge — no catalog change). **No 2023-removal here; this is pure 2002 introduction, gated `{is2002()}?`.**

- **Spec (ISO/IEC 1989:2023; `specs/ISO_COBOL.md` :line anchors, all read directly this wave).** SHARING clause §12.4.5.15 (:15807; fmt `SHARING WITH {ALL OTHER | NO OTHER | READ ONLY}` :15823; SELECT skeletons :15011/:15053/:15096). Sharing modes + precedence §9.1.15 (:11694–11731 — **OPEN phrase > SHARING clause > implementor default** :11698; NO OTHER = exclusive, record locks *ignored* :11723; READ ONLY :11725; ALL OTHER :11727; note :11729 *"separate paths of access … in the same runtime element"* = the in-process multi-connector license). **Table 19 open-conflict matrix** :29214–29227 → status **61**. LOCK MODE clause §12.4.5.9 (:15521; fmt **MANUAL/AUTOMATIC only** — verified :14994/:15533 — `LOCK MODE IS {MANUAL|AUTOMATIC} [WITH LOCK ON [MULTIPLE] {RECORD|RECORDS}]`; SR1 :15538 not-with-APPLY-COMMIT; SR2 :15540 no MULTIPLE for sequential org/access; GR1 omitted-default :15545; GR3 NO OTHER ⇒ no effect :15557; GR4 AUTOMATIC lock-on-any-READ :15559; GR5 MANUAL lock-only-on-explicit :15569; GR6 single ≤1 lock, any I-O except START releases prior :15571; GR7 MULTIPLE, impl max ≥15/connector ≥255/run-unit → 53/54 :15573). §9.1.16 record-locking (:11744–11764 — locked record inaccessible to any other connector except IGNORING LOCK :11752; CLOSE releases all :11754). RETRY §14.7.9 (:25199; fmt `RETRY {arith-1 TIMES | FOR arith-2 SECONDS | FOREVER}` :25209; GR1 n-times/round-up :25220; GR2 seconds :25222; GR3 FOREVER :25224; **GR4a RETRY-absent-or-≤0 ⇒ unsuccessful, status per 9.1.13** :25228 — the single-run-unit escape hatch). OPEN §14.9.27 (:29130 fmt `OPEN {mode} [sharing] [retry] {file [WITH NO REWIND]}…` — **verified: sharing+retry attach per mode-group**; SR7 no-SHARING-with-APPLY-COMMIT :29169; **SR8 ALL ⇒ LOCK MODE mandatory** :29171; GR21-24 :29325). READ §14.9.30 (fmt1/2 :29791/:29808; SR3 LOCK⊗IGNORING :29845; SR4 none-under-AUTOMATIC :29847; SR5 none-under-APPLY-COMMIT :29849; GR7-12 lock governance :29901–29935; GR22 ADVANCING ON LOCK skip-scan :30068). WRITE §14.9.51 (SR22 no WITH[NO]LOCK under AUTOMATIC :33415; GR11 set-lock :33494; GR16 RETRY :33513). REWRITE §14.9.35 (SR4-5 :30472; GR12 release/set :30590). DELETE record retry §14.9.10 fmt1 (:26673; GR6 conflict→51 :26727; GR7 release :26737); DELETE FILE retry fmt2 (:26683; GR15 conflict→62 :26769). UNLOCK §14.9.47 (:32657; fmt `UNLOCK file [RECORD|RECORDS]` :32668; SR1-2 no sort/merge/APPLY-COMMIT :32675; GR1 releases all this-connector locks, always succeeds :32682; GR3 updates I-O status :32688; not-open ⇒ **42** :11579). Statuses §9.1.13.8/9 (verified: **51** record locked by another connector :11621; **52** deadlock, impl-detected :11631; **53** run-unit lock max :11633; **54** connector lock max :11635; **61** OPEN sharing conflict, sub-cases (a)-(e) :11640–11651; **62** DELETE FILE while open by another connector :11652). First-digit EC map §9.1.13.1 (`'5'→EC-I-O-RECORD-OPERATION`, `'6'→EC-I-O-FILE-SHARING` :11396). START carries **no** lock/retry (fmt :32067, GR6 :15571 does-not-release-single). CLOSE carries **no** WITH LOCK in 2023 (the ≤2014 leg is `close-with-lock-removed-2023`, already gated).

- **Editions.** The ENTIRE subsystem is a **COBOL-2002 introduction** (carried unchanged through 2014/2023; Annex E enumerates only 2014→2023 deltas and the sole sharing-related delta is the CLOSE…WITH LOCK removal, E.2 #1 :49038 — already landed as `close-with-lock-removed-2023`). Gate everything `{is2002()}?`. **§8.9 reserved-since-2002** (table rows verified present, `ReservedWords.Table.cs`): `SHARING` :400, `RETRY` :372, `UNLOCK` :450 (R85=false, R2002/14/23=true); `LOCK` :269 / `MODE` :279 are continuous-since-85 (reused, no funnel). **§8.10 context-sensitive (absent from the table — the IMPLEMENTS model):** `MANUAL`, `AUTOMATIC`, `IGNORING`, `FOREVER`, `SECONDS`, `ONLY` — user-legal at every edition, no table row, no funnel. `vcr: "2002 introduction (derive from the 2002 standard — Annex E covers 2014→2023 only)"` on every new row (the pointer/boolean precedent). **No 2002→2014 change** (Annex E is 2014→2023 only). Cross-check `docs/VERSION_CHANGE_REFERENCE.md` rows for the sharing family.

- **⛔ SYNTHESIS DECISION D1 — REAL machinery over a physical-file registry (supersedes the RUNTIME/SEAMS brief's "dead-branch" posture).** The two recon briefs diverge: the ISO brief says build it for real (multiple connectors per run unit are in scope, :11729); the runtime brief says model-but-dead-branch (one connector per SELECT ⇒ no competitor). **The ISO brief wins, on spec text + the process rule** (feedback_spec_scopes_not_tests — implement COMPLETE, not scope to the easy path): §9.1.13.9 keys 61/62 on *"another file connector,"* and two non-EXTERNAL `SELECT`s with the same resolved host path are two distinct connectors over one physical file, opened concurrently in one single-threaded process — a **deterministic, testable** conflict. So:
  - **REAL (fully enforced — parse + bind + validate + observable runtime, all in one run unit):** the physical-file open-connector registry + Table-19 → **61**; `DELETE FILE` while open by another connector → **62**; record locks with **51** on another-connector conflict; self re-access (GR8 — a connector reads its own locked record as if unlocked); AUTOMATIC (lock-on-READ) vs MANUAL (lock-on-explicit); single (any I-O except START releases prior, GR6) vs MULTIPLE release semantics (READ GR11 / REWRITE GR12 / DELETE GR7); **UNLOCK** (release all this-connector locks); **IGNORING LOCK** (read despite another's lock); **ADVANCING ON LOCK** (skip-scan locked records on NEXT/PREVIOUS); **53/54** lock-count limits (connector max 15 → 54, run-unit max 255 → 53); the **RETRY loop** (n-times evaluates the registry n+1 times).
  - **Named single-run-unit NO-OPS (documented, each with a loud guard — the SAME-RECORD-AREA / EXTERNAL-conformance precedent):**
    1. **Cross-run-unit / cross-OS-process file & record locks** (§9.1.15 "prevents other run units"; §9.1.16 "a different run unit") — no second process exists ⇒ these conflicts never arise; the registry is process-local by construction. Guard: doc comment on `CobolFile.Locks` + the EXTERNAL-one-connector precedent (`CobolFile.cs:39`).
    2. **RETRY `FOR n SECONDS` / `FOREVER` true wall-clock blocking** (§14.7.9 GR2/GR3) — with no external releaser a single-threaded retry cannot change the outcome; **a FOREVER wait on a record locked by another connector of the executing run unit resolves to status 52 (deadlock — §9.1.13.8 item 2, impl-defined detection)** rather than hang. `n TIMES` loops the registry-check n+1 times then reports the conflict's own 51/61/62; `FOR n SECONDS` makes no further attempt at all, because GR2 clamps its timeout period to the A.1 item 166 maximum meaningful value of ZERO, and reports the same — only FOREVER, and only on a RECORD conflict, reaches **52**. Guard: `RetryLoop` never calls `Thread.Sleep`; comment cites GR4a + the 52 impl-license. Config items #165/#166 (:39679/:39681) are ours to document.
    3. **APPLY COMMIT** (§14.9.5) implicit `AUTOMATIC WITH LOCK ON MULTIPLE RECORDS` + the "shall-not-with-APPLY-COMMIT" SRs (LOCK MODE SR1 :15538, OPEN SHARING SR7 :29169, UNLOCK SR2 :32677, READ SR5 :29849) — **APPLY COMMIT is not implemented** (0 grammar hits) ⇒ these exclusion SRs are vacuously satisfied. Guard: a named residue row + a `// residue: APPLY COMMIT unimplemented — SR vacuous` at each SR site so re-wiring is one edit when APPLY COMMIT lands.
    4. **52 deadlock cross-process detection**, OS **advisory-lock** enforcement, device-concurrency model (config #52/#75/#76 :39387/:39445/:39447) — locks are **in-memory only** (§9.1.16, never persisted), documented device model.
    5. Implementor-defined **default sharing mode** (OPEN GR23 :29329) + **default lock mode** (LOCK MODE GR1 :15545): **our documented choice (kb/Work PB322, docs/CONFORMANCE.md DOC-A.1-77 / -131 / -153) = default sharing a function of the open mode: `OPEN INPUT` is `READ ONLY`, `OPEN OUTPUT` / `I-O` / `EXTEND` are `NO OTHER` (exclusive, record locks ignored per GR3), default lock mode = no locking.** This preserves every existing single-connector program byte-for-byte (a lone connector never self-conflicts; the CLOSE…WITH-LOCK/38 path is untouched) and makes record locking observable *only* when the program opts in via `SHARING WITH ALL OTHER`/`READ ONLY` + `LOCK MODE` (which SR8 already couples). Config #153 (:39649).

- **Grammar plan (pre-authorized; ALL edits ADDITIVE + `{is2002()}?`-head-gated with UNIQUE leading tokens — the 85/NIST parse surface stays bit-identical; ⚠ the DEVLOG 621/622 lesson: NEVER restructure a shared core rule the DFA depends on — every edit below is a NEW gated alternative or a NEW optional phrase, never a rewrite of an existing alternative's no-phrase path).** `Grammar/Core/CobolIO.g4` + `CobolParserCore.g4` + `CobolLexer.g4` (the shared frontend — `CobolSharp.Compiler.csproj:25` consumes the same `Generated/`).
  1. **Lexer** (`CobolLexer.g4`, default-mode keyword band, alphabetical): ADD **`SHARING`, `RETRY`, `UNLOCK`, `MANUAL`, `AUTOMATIC`, `IGNORING`, `FOREVER`, `SECONDS`, `ONLY`** (`SHARING:'SHARING';` etc.). **Reuse present tokens** (verified): `LOCK`(:439) `MODE`(:442) `MULTIPLE`(:516) `TIMES`(:544) `ADVANCING`(:284) `REWIND`(:507) `WITH NO OTHER ON IS FOR ALL READ RECORD RECORDS FILE`. **Do NOT add `EXCLUSIVE`** (see CONFLICT-4) **or `KEPT`** (CONFLICT-2). `_dataNameTokens` (:30–65, beside `XOR,EXCLUSIVE_OR` :38): ADD all nine so `01 SHARING PIC X.`/`01 MANUAL PIC 9.` at 85 trigger SUBSCRIPT mode on a following `(` (the whitelist-mirror rule :19–29); `subToken`/SUBSCRIPT mode itself needs no change.
  2. **`cobolWord`** (`CobolParserCore.g4:25–98`, beside `XOR/EXCLUSIVE_OR` :45–46): ADD all nine with the comment *"user words at 85, funnel-0901'd ≥2002 for the three §8.9 words (SHARING/RETRY/UNLOCK); the six §8.10 words stay user-legal at all editions; keyword occurrences parse only through the gated sharing/lock rules — never a name slot."*
  3. **File-control entry** (`CobolIO.g4` `fileControlClauses` :46–67): add two alternatives before `vendorFileControlClause` (:66), each unique-leading-token, zero DFA hazard:
     ```
     | {is2002()}? sharingClause
     | {is2002()}? lockModeClause
     ```
     New rules (near :142):
     ```
     sharingClause  : SHARING WITH? sharingMode ;                       // §12.4.5.15
     sharingMode    : ALL OTHER? | NO OTHER? | READ ONLY ;
     lockModeClause : LOCK MODE IS? (MANUAL | AUTOMATIC) lockOnPhrase? ; // §12.4.5.9 (MANUAL/AUTOMATIC only)
     lockOnPhrase   : WITH? LOCK ON? MULTIPLE? (RECORD | RECORDS) ;
     ```
  4. **OPEN** (`openClause` :208 — verified §14.9.27 attaches sharing+retry per mode-group, before the file list):
     ```
     openClause : openMode ({is2002()}? sharingPhrase)? ({is2002()}? retryPhrase)? openFileSpec+ ;
     sharingPhrase : SHARING WITH? sharingMode ;
     ```
  5. **Two shared helper rules** (once, near the OPEN block ~:238):
     ```
     retryPhrase      : RETRY (arithmeticExpression TIMES | FOR? arithmeticExpression SECONDS | FOREVER) ;   // §14.7.9
     recordLockPhrase : IGNORING LOCK | WITH? NO LOCK | WITH? LOCK ;                                          // §14.9.30
     ```
     (order `WITH? NO LOCK` before `WITH? LOCK` so `WITH NO LOCK` doesn't shadow to `WITH LOCK`.)
  6. **READ** (`readStatement` :243 — insert AFTER `readKey?` :248, BEFORE `readAtEnd?` :249, §14.9.30 order):
     ```
       ({is2002()}? readAdvancingOnLock)?     // ADVANCING ON LOCK (fmt1 only)
       ({is2002()}? retryPhrase)?
       ({is2002()}? recordLockPhrase)?
     ```
     `readAdvancingOnLock : ADVANCING ON LOCK ;`
  7. **WRITE** (`writeStatement` :290 — after `writeBeforeAfter?` :293, before `writeAtEndOfPage?` :294): `({is2002()}? retryPhrase)? ({is2002()}? recordLockPhrase)?`.
  8. **REWRITE** (`rewriteStatement` :330 — after `rewriteFrom?` :332, before `rewriteInvalidKeyPhrase?` :333): same pair.
  9. **DELETE record** (`deleteStatement` :352 — after `RECORD?` :353, before `deleteInvalidKeyPhrase?` :354): `({is2002()}? retryPhrase)?` **only** (no lock phrase — DELETE releases). **DELETE FILE** (`deleteFileStatement` :369): add `({is2002()}? retryPhrase)?` after `fileName` (:370) per :26683.
  10. **UNLOCK** (new rule in `CobolIO.g4` ~:238): `unlockStatement : UNLOCK fileName (RECORD | RECORDS)? ;`. **Dispatch** (`CobolParserCore.g4` `statement` :657–711): add `| {is2002()}? unlockStatement` beside `allocateStatement`/`freeStatement` (:670–671 — the exact model).
  11. **START — UNTOUCHED** (CONFLICT-3: ISO 2023 START carries no lock/retry; a defensive bind guard, below, rejects any that leaks in).
  12. **Regen both OSes** (`Generated/` is build output — feedback_commit_generated_parser); **FULL `scripts/guard.sh` MANDATORY** on every grammar-touching commit (NIST runs `--nist`=85 where every gate is dead and the nine words behave as user words — must be byte-invariant).

- **Reserved-word funnel** (`EditionValidator.cs`): add `CobolLexer.SHARING, CobolLexer.RETRY, CobolLexer.UNLOCK` to `CheckedTokenTypes` (:268–291, beside `XOR` :276) — position-blind-safe by the XOR argument (SHARING→sharingClause/Phrase, RETRY→retryPhrase, UNLOCK→unlockStatement — a keyword occurrence never reaches a name slot). Funnel → `ReservedWordSet.RejectsAt` → **0901** at ≥2002; table rows already exist ⇒ **no `gen-reserved-words.ps1` / `reserved-words.json` change, no `ReservedWordsDriftTests` change.** The six §8.10 words (MANUAL/AUTOMATIC/IGNORING/FOREVER/SECONDS/ONLY) get **no** funnel entry, **no** table row (IMPLEMENTS model — user-legal at all editions).

- **FileModel + Bound nodes** (`FileModel.cs`, beside `FileOrganization`/`FileAccessMode` :7/:10; fields after `Linage` ~:127): `enum SharingMode { None, AllOther, NoOther, ReadOnly }`, `enum LockKind { None, Manual, Automatic }`, `record LockModeInfo(LockKind Kind, bool Multiple)`; on `FileModel`: `SharingMode Sharing = SharingMode.None;`, `LockModeInfo? LockMode;`. **Bound tree** (`Bound/BoundTree.cs`): `enum BoundRecordLock { None, WithLock, WithNoLock, IgnoringLock }`; `record RetrySpec(RetryKind Kind, BoundExpr? Amount)` with `enum RetryKind { Times, Seconds, Forever }`; extend `BoundOpen` (:481) per-file with `SharingMode? SharingOverride, RetrySpec? Retry`; add `RecordLock Lock`/`RetrySpec? Retry`/`bool AdvancingOnLock` to the bound READ/WRITE/REWRITE/DELETE nodes; NEW `record BoundUnlock(FileModel File, bool Records) : BoundStatement`. **CLOSE is untouched** — `BoundCloseKind.WithLock` (:474) already fully handles the ≤2014 leg.

- **Binder** (`DataBinder.cs` `BindFileControl` :279–310 + the verb binders):
  - **SELECT clauses** — add two arms to the clause loop after :305 (mirroring `MapOrganization`/`MapAccessMode`): `else if (clauses.sharingClause() is {} sh) file.Sharing = MapSharing(sh);` and `else if (clauses.lockModeClause() is {} lm) file.LockMode = MapLockMode(lm);`. New static `MapSharing`/`MapLockMode` beside `MapOrganization` (:512).
  - **Verb phrases** — the OPEN SHARING override + RETRY bind onto `BoundOpen`; READ/WRITE/REWRITE record-lock + RETRY + ADVANCING onto the bound verb; UNLOCK → `BoundUnlock`.
  - **SR validation → NEW band `COBOLNET1512`** (message-differentiated — the 0869/1511 precedent), each citing its §: LOCK MODE `MULTIPLE` on sequential org/access (SR2 :15540); `SHARING WITH ALL OTHER` (clause or OPEN phrase) without a `LOCK MODE` clause (SR8 :29171); READ `IGNORING LOCK` + `WITH LOCK` together (SR3 :29845); any of IGNORING/WITH LOCK/WITH NO LOCK under a file whose effective LOCK MODE is AUTOMATIC (READ SR4 :29847 / WRITE SR22 :33415 / REWRITE SR4 :30472); UNLOCK on a SORT/MERGE file (SR1 :32675); a lock/retry phrase that somehow reached START (defensive — grammar forbids). APPLY-COMMIT SRs are vacuous (residue #3). **Edition gate** = registry rows (below) via the parse-layer 0900 map + `EditionValidator` visitor rows; NO binder-side `ConstructRegistry.Check` needed for the introduction gate (the pointer/boolean precedent: at 85 the gated grammar is dead and the binder is unreachable) — the `COBOLNET1512` checks run at ≥2002 only.

- **Runtime** (`src/Cobol.Net.Runtime/IO/`):
  - **`FileSupport.cs` `FileStatusCode`** (:36 band): ADD `RecordLocked="51"`, `Deadlock="52"`, `RunUnitLockLimit="53"`, `ConnectorLockLimit="54"`, `FileSharingConflict="61"`, `DeleteFileSharing="62"` (the singular status table). **KEEP `FileLocked="38"`** but reframe its doc comment: *"38 — OPEN of a file previously CLOSEd WITH LOCK (the ≤2014 CLOSE…WITH LOCK leg; NOT part of the 2002 5x/6x sharing family — that construct is 0902-rejected at 2023 via close-with-lock-removed-2023)."* (Refines the ISO brief's "retire 38": it stays, correctly, for the still-legal ≤2014 path.)
  - **NEW partial `CobolFile.Locks.cs`** — the ONE physical-file registry (singular pattern): `Dictionary<string /*resolvedHostPath*/, PhysicalFileState>` where `PhysicalFileState { Dictionary<string connectorName,(SharingMode,FileOpenMode)> Open; Dictionary<string recordId,string ownerConnector> RecordLocks; bool ClosedWithLock; }`. Absorbs the existing `Locked` HashSet (:16) — `ClosedWithLock` per physical path replaces the by-name set (fixes the latent shared-path 38 bug for free). `Init()` (:19/:23) clears it. Helpers: `RegisterSharing(name, path, SharingMode, LockModeInfo?)` (emitted after `Register`, mirroring `SetLinage` :86); `TryOpenShared(name, path, mode, sharingOverride) → status` (Table-19 classify vs OTHER open connectors → 61 or register+00); `LockRecord(name, path, recordId, mode) → bool granted` (another owner ⇒ deny→51 unless self; enforce 15/connector→54, 255/run-unit→53); `IsLockedByOther(...)`; `ReleaseSingle`/`ReleaseAllForConnector(name)` (UNLOCK + CLOSE); `RetryLoop(Func<string> attempt, RetrySpec)` (n-times; a SECONDS period clamped to the A.1 item 166 maximum meaningful value of ZERO so it makes no further attempt; only FOREVER on a RECORD conflict bails to the §9.1.13.8 item 2 deadlock 52; never sleeps).
  - **Facade threading** — `Open` (:50) becomes sharing-aware: consult `TryOpenShared` before `f.Open(mode)`; the `ClosedWithLock` check replaces `Locked.Contains` (:54) → 38. Keyed `KeyedOpen` (:651) mirrors. READ/WRITE/REWRITE/DELETE facade methods + keyed twins gain `(BoundRecordLock, RetrySpec)`-derived args: acquire/deny the record lock (record id = RRN for relative / prime-key for indexed / byte-offset for sequential — the connectors already own these: `RelativeFile._lastSlot`, `IndexedFile._lastReadPrime`, `SequentialFile._lastReadBlockStart`), set 51 on denial, apply single/multiple release. AUTOMATIC ⇒ auto-lock on any READ; MANUAL ⇒ only on explicit `WITH LOCK`. `IGNORING LOCK` ⇒ bypass the deny. `ADVANCING ON LOCK` ⇒ on NEXT/PREVIOUS, skip records `IsLockedByOther` until unlocked/EOF (GR22). NEW `Unlock(name, records)` → 42 if not open else `ReleaseAllForConnector` + status 00. Every verb already emits `EmitStoreFileStatus` + `EmitUseHook`, so the new 5x/6x codes flow to FILE STATUS and the EC bridge automatically.
  - **EC bridge — NO CHANGE** (verified complete): `ExceptionCatalog.cs` `EC-I-O-FILE-SHARING` :113 / `EC-I-O-RECORD-OPERATION` :120 (both nonfatal), `IoEcOfStatus` `'5'`/`'6'` arms :277/:278, `IsFatalIoStatus` excludes 5/6 :288, mask bits :294, `__IoCheckEc` continues-not-throws for 5x/6x (`CSharpEmitter.Exceptions.cs:290`). Producing a 51/61 status auto-raises the right continuable EC — the ONLY runtime work is producing the code.
  - **Emitter** (`CSharpEmitter.cs` + `.KeyedIo.cs`): `EmitFileRegistration` (:1362) emits a follow-up `CobolFile.RegisterSharing(name, path, sharing, lockKind, multiple)` beside `SetLinage`; `EmitOpen` (:1417) passes the sharing-override + retry to the open call; new `case BoundUnlock` → `CobolFile.Unlock(name, records)` + the two hooks; the READ/WRITE/REWRITE/DELETE emitters pass the lock/retry/advancing args.

- **Diagnostics.** **NEW band `COBOLNET1512`** (next-free — grep-verified: 08xx dense 0801–0899, 0900–0903 = edition gates, 15xx dense 1501–1511; **1512 is the next free constraint-band code**), message-differentiated across the SR set above (each message names its §/SR). Introduction gating = **0900** via the registry rows + `EditionGateHints`; reserved-word misuse = **0901** via the funnel; no `RemovedConstruct`/`ObsoleteFlag` codes (nothing removed/obsoleted here — CLOSE…WITH LOCK's 0902 already exists).

- **Registry / matrix / hints (SAME commit — the drift test asserts both directions).** `ConstructDialectStatus.cs` (beside `allocate-2002` :64 / `based-clause-2002` :69), five introduction rows + one representative user-word row, all `EditionCodes.Introduction` except the last (`EditionCodes.ReservedWord`):
  ```
  new("file-sharing-clause-2002", "the SHARING clause / OPEN SHARING phrase", 2002, null, null, EditionCodes.Introduction, "ISO §12.4.5.15 / §14.9.27; 2002 introduction (derive from the 2002 standard — Annex E covers 2014→2023 only)"),
  new("lock-mode-clause-2002",    "the LOCK MODE clause",                     2002, null, null, EditionCodes.Introduction, "ISO §12.4.5.9 (MANUAL/AUTOMATIC [WITH LOCK ON [MULTIPLE] RECORD(S)]); 2002 introduction"),
  new("retry-phrase-2002",        "the RETRY phrase",                         2002, null, null, EditionCodes.Introduction, "ISO §14.7.9 (OPEN/READ/WRITE/REWRITE/DELETE); 2002 introduction"),
  new("unlock-statement-2002",    "the UNLOCK statement",                     2002, null, null, EditionCodes.Introduction, "ISO §14.9.47; 2002 introduction"),
  new("record-lock-phrase-2002",  "a record-lock phrase (WITH LOCK / WITH NO LOCK / IGNORING LOCK / ADVANCING ON LOCK)", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.30/§14.9.51/§14.9.35; 2002 introduction"),
  new("user-word-sharing-2002",   "the word SHARING as a user-defined word",  85, 2002, null, EditionCodes.ReservedWord, "§8.9 interval: user-definable at 85, reserved since 2002 (ReservedWords.Table covers SHARING/RETRY/UNLOCK; single representative — the user-word-raising-2002 precedent)"),
  ```
  `EditionGateHints.cs` (beside `Based` :43): `Gate Sharing/LockMode/Retry/Unlock` + token arms in the `switch` (:82): `CobolLexer.SHARING when InRule(ruleStack,"fileControlClauseGroup")||InRule(ruleStack,"openStatement") => Sharing`; `CobolLexer.LOCK when Next(stream,token,1)?.Type==CobolLexer.MODE => LockMode` (MODE-lookahead disjoins CLOSE…WITH LOCK); `CobolLexer.RETRY => Retry`; `CobolLexer.UNLOCK => Unlock` (as user words they parse through `cobolWord` and never error, so an error AT the token below 2002 IS the gated construct — the XOR/boolean argument). `EditionValidator` visitor rows (beside the CLOSE-WITH-LOCK gate :150): `VisitSharingClause`/`VisitLockModeClause`/`VisitUnlockStatement`/`VisitRetryPhrase` → `ConstructRegistry.Check(_edition, "…-2002", …)` for the ≥-check at bind (redundant with the parse-layer hint but the four-per-edition-compiler discipline, feedback_version_test_matrix). `tests/version-matrix/constructs.json`: mirror all six rows (`active`, `expectDiagnostic: "COBOLNET0900"` for the five introduction rows / `"COBOLNET0901"` for the user-word row), each with an embedded `source` (a minimal SELECT-with-SHARING / LOCK MODE / a `RETRY`/`UNLOCK` statement that compiles clean at 2002+ and 0900s at 85; the user-word row declares `01 SHARING PIC X.` — clean at 85, 0901 at ≥2002).

- **Tests / goldens / docs.** **NEW golden `tests/conformance/2002/file_sharing.cob` + `.out`** (manifest `enabled`, alphabetical) — the single-run-unit, two-connector real test: **two `SELECT`s (F-A MANUAL, F-B MANUAL) both `ASSIGN "share.dat"` RELATIVE/DYNAMIC `SHARING WITH ALL OTHER` `LOCK MODE`** (SR8-satisfied). Flow + ISO-derived expected lines: seed via an exclusive OUTPUT connector (records 1=`ALPHA`,2=`BRAVO`); `OPEN I-O F-A`/`OPEN I-O F-B` → `OPEN-A=00`,`OPEN-B=00` (both ALL OTHER); `READ F-A(1) WITH LOCK` → `READA=00`; `READ F-B(1)` → `READB=51` (locked by another connector, GR9); `READ F-B(1) RETRY 2 TIMES` → `RETRYB=51` (single-thread, holder can't release mid-loop — GR4b exhausts); `READ F-B(1) IGNORING LOCK` → `IGN=ALPHA` (GR12); `UNLOCK F-A` then `READ F-B(1)` → `AFTER=00`; then `OPEN SHARING WITH NO OTHER I-O F-B` while F-A still open → `EXCL=61` (§9.1.13.9(b)). Expected `.out`: `OPEN-A=00 / OPEN-B=00 / READA=00 / READB=51 / RETRYB=51 / IGN=ALPHA / AFTER=00 / EXCL=61`. This doubles as the phase demo (feedback_demo_per_phase — outputs derived, not just non-crashing). **LEGACY-runner impact: the frozen legacy binder/emitter have ZERO sharing/lock support** (legacy `CobolFile` has only the by-name `Locked`/38 primitive; no SHARING/LOCK MODE binding, no UNLOCK statement case). The shared grammar will PARSE the clauses at 2002, but legacy bind chokes ⇒ **`GreenfieldOnly` exclusion `("2002","file_sharing")` in `ConformanceTests.cs:66` in the SAME commit** (feedback_legacy_suite_on_shared_corpus; the DEVLOG 618 `udf_exit_function` / DEVLOG 621 `boolean_ops` precedent). **NEW unit battery `CobolFileLockTests`** (against the `CobolFile.Locks` registry directly): Table-19 all five 61 sub-cases (a)-(e); 51 grant/deny + self re-access (GR8); single-release (any I-O except START, GR6) vs MULTIPLE; 53 (256th run-unit lock) / 54 (16th connector lock); UNLOCK-clears + UNLOCK-not-open→42; RETRY n-times exhaust + FOREVER→52 deadlock-bail; IGNORING-LOCK bypass; ADVANCING-ON-LOCK skip-scan; default-NO-OTHER preserves lone-connector open. **NEGATIVE cases (each asserts `COBOLNET1512`, the pointer/boolean negative-harness):** `lock_mode_multiple_sequential` (SR2), `sharing_all_no_lockmode` (SR8), `read_ignoring_and_lock` (SR3), `read_auto_with_lock` (SR4). **Edition-gate negatives (0900 at 85):** `sharing_clause_85`, `lock_mode_85`, `retry_85`, `unlock_85`. **Matrix:** the six `constructs.json` rows green at all four editions. **Same-change-set docs:** this file's M2-FILE-1 row (NOT-STARTED → LANDED), the **M3-4 row** (the "OPEN SHARING/record LOCK/RETRY/UNLOCK reserved-only" clause → satisfied) and the **M2-FILE-2 note** (correct "sharing-tied (9x)" → **5x/6x**, CONFLICT-1), the DEVLOG entry, the resume-prompt STATE banner, and COBOLNET_DESIGN's file-I/O section (the `CobolFile.Locks` physical-file registry + the D1 single-run-unit posture; feedback_follow_design_docs_and_spec).

- **Implementation order (small commits; each green before the next; FULL `scripts/guard.sh` on every grammar-touching commit).** **(1)** Lexer 9 tokens + `_dataNameTokens` + `cobolWord` + `CheckedTokenTypes`(3) + regen → greenfield battery + **FULL legacy guard** (85 surface invariant — the nine words behave as user words end-to-end). **(2)** Grammar (sharingClause/lockModeClause; openClause sharing+retry; retryPhrase/recordLockPhrase; READ advancing/retry/lock; write/rewrite/delete; unlockStatement + dispatch) + `EditionGateHints` + registry/json rows + regen → battery + **FULL legacy guard** + DFA timing check; empirically verify no viable-alt regressions on the untouched READ/WRITE/OPEN no-phrase paths. **(3)** FileModel fields + `BindFileControl` arms + Bound nodes + verb-phrase binding + `COBOLNET1512` SR checks + EditionValidator visitor rows → battery. **(4)** `FileStatusCode` 51/52/53/54/61/62 + `CobolFile.Locks.cs` registry + Open/Read/Write/Rewrite/Delete/Unlock threading + `RegisterSharing` + emitter arms + `CobolFileLockTests` → battery. **(5)** `file_sharing` golden + `GreenfieldOnly` + the four SR negatives + the four 85-gate negatives + manifest + matrix activation → full battery + **legacy conformance** + **FULL legacy guard**. **(6)** Docs + DEVLOG + resume-prompt + commit/push (feedback_fully_autonomous_push).

- **STAGED RESIDUE (named, loud — each with its § + guard).** (1) **Cross-run-unit / cross-OS-process locks** (§9.1.15/§9.1.16) — process-local registry, no second run unit; doc-comment guard. (2) **RETRY wall-clock `SECONDS`/`FOREVER`** (§14.7.9 GR2/3) — no external releaser, so neither sleeps. GR2's timeout period is clamped to this implementation's maximum meaningful value of ZERO (A.1 item 166) and lands the conflict's OWN §9.1.13 status; only GR3's FOREVER, and only on a RECORD conflict, bails to **52** (§9.1.13.8 item 2, A.1 item 109). Determination and guard on `RetryLoop`. (3) **APPLY COMMIT** (§14.9.5) + its four exclusion SRs — unimplemented ⇒ SRs vacuous; `// residue: APPLY COMMIT` at each SR site. (4) **52 cross-process deadlock detection / OS advisory locks / device concurrency** (config #52/#75/#76) — in-memory-only model. (5) **`START` WITH lock/retry** (not ISO 2023) — grammar forbids; defensive bind guard = residue-flagged. (6) **`WITH KEPT LOCK`** (vendor Micro Focus, not ISO) — not tokenized ⇒ loud parse error; vendor-dialect leniency if ever wanted (CONFLICT-2). (7) **`EXCLUSIVE` lock mode** (vendor, not §12.4.5.9) — not tokenized ⇒ loud parse error (CONFLICT-4). (8) **AUTOMATIC-single implicit release timing edge cases** on mixed keyed/sequential connectors — the single-release hook rides `_prevOpWasSuccessfulRead`; verify per-organization at implementation, else 1512-flag. (9) **CLOSE…WITH LOCK / status 38** — the ≤2014 leg is unchanged (already `close-with-lock-removed-2023`-gated at 2023); 38 retained, reframed.

- **ANCHOR LIST.** Grammar: `CobolLexer.g4` keyword band (:284/:439/:442/:507/:516/:544), `_dataNameTokens` :30–65 (:38); `CobolParserCore.g4` `cobolWord` :25–98 (:45–46), `statement` dispatch :657–711 (:669–671), predicates `CobolParserCoreBase.cs` (is2002/is2023); `CobolIO.g4` `fileControlClauses` :46–67 (:63/:66), `openClause` :208, `openFileSpec` :214, `closeOption` :233–237 (untouched), `readStatement` :243–253 (:248/:249), `writeStatement` :290–298 (:293/:294), `rewriteStatement` :330–336 (:332/:333), `deleteStatement` :352–357 (:353/:354), `deleteFileStatement` :369–374. Validation: `EditionValidator.cs` :150–156 (CLOSE-WITH-LOCK gate model), `CheckedTokenTypes` :268–291 (:276); `ReservedWords.Table.cs` :269/:279/:372/:400/:450; `ConstructDialectStatus.cs` :63–69/:98/:112–114; `EditionCodes.cs` :16/:21; `EditionGateHints.cs` :43–56/:82–128/:144–147; `CobolErrorStrategy.cs:116`. Binder: `DataBinder.cs` `BindFileControl` :279–310 (:291–305), `MapOrganization` :512; `FileModel.cs` :7/:10/:33/:36/:127/:159; `Bound/BoundTree.cs` `BoundOpen` :481, `BoundCloseKind` :474. Runtime: `IO/FileSupport.cs` `FileStatusCode` :21–70 (:36); `IO/CobolFile.cs` :13–16/:19–24/:36–43/:50–64/:86/:130 (+ new `CobolFile.Locks.cs`); `IO/SequentialFile.cs` (:39 offset, :162 SetStatus), `IO/RelativeFile.cs` (:97 slots, :107 _lastSlot, :128 SetStatus), `IO/IndexedFile.cs` (:42 _lastReadPrime, :477 KeyOf, :508–509/:651–659 keyed registry/open); `Exceptions/ExceptionCatalog.cs` :113/:120/:277/:278/:288/:294 (NO change). Emit: `CSharpEmitter.cs` :1362/:1417/:1436, `CSharpEmitter.KeyedIo.cs` :31/:43/:52/:117, `CSharpEmitter.Exceptions.cs` :273–312. Tests: `ConformanceTests.cs` :66/:116; `constructs.json`; `tests/conformance/2002/manifest.json`. Spec: :11396/:11579/:11621–11652/:11694–11764/:14994/:15011/:15521–15575/:15807–15824/:25199–25242/:29130–29171/:29214–29227/:29325–29331/:29782–30076/:30426–30607/:32657–32688/:33274–33513/:49038. Legacy: `CobolSharp.Compiler.csproj:25`.

- **CONFLICTS FLAGGED (brief-vs-brief / parent-requirements-vs-spec).** **(C1)** The parent requirement's **"status codes (9x record-locked etc.)" is WRONG** — the ISO-2002 sharing/locking statuses are **5x (record) / 6x (file)** (§9.1.13.8/9 verified: 51/52/53/54/61/62); there is no 9x sharing family. The M2-FILE-2 note ("sharing-tied (9x) ride Phase 4(d)") inherits the error and is corrected in this change set. **(C2)** The parent's **`[WITH [NO|KEPT] LOCK]` — `KEPT` is NOT ISO 2023** (grep 0 hits; it is a Micro Focus vendor form); dropped from core, staged as vendor-leniency residue (#6). **(C3)** The parent's inclusion of **START — START carries NEITHER lock NOR retry in ISO 2023** (fmt :32067; GR6 :15571 explicitly does-not-release-single); `startStatement` untouched, defensive bind guard only (residue #5). **(C4)** The GRAMMAR brief's **`EXCLUSIVE` in `lockModeKind` is WRONG** — spec-verified §12.4.5.9 (:14994/:15533) is **`{MANUAL | AUTOMATIC}` only**; `EXCLUSIVE` dropped (vendor residue #7). **(C5)** The RUNTIME/SEAMS brief's **"model-the-classification-but-the-conflict-branch-is-dead" posture is SUPERSEDED** by DECISION D1 — §9.1.13.9 keys 61/62 on "another file connector," realizable via two `SELECT`s → one physical file in one run unit, so the machinery is built REAL (registry + live 51/61/62/53/54/UNLOCK/RETRY), exercised by `file_sharing`. The runtime brief is a valid conservative reading; the synthesis chooses the fuller spec-faithful build per feedback_spec_scopes_not_tests. **(C6)** The ISO brief's **"retire/gate FileLocked=38" is REFINED** — 38 is the still-legal ≤2014 CLOSE…WITH LOCK reopen code (that construct is 0902-rejected at 2023 by the existing gate, not by deleting 38); 38 is retained and its doc reframed out of the 2002 5x/6x family. **(C7)** The ISO brief's `SemanticBuilder.VisitFileControlClauseGroup` SR seam is superseded by the greenfield `DataBinder`/`EditionValidator` seams (the CBL3615/3616 reference is legacy) — the SR checks land as `COBOLNET1512` in the greenfield binder.



| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-FILE-1 | SHARING, LOCK MODE/LOCK ON, RETRY | done | **LANDED (Phase 4d, DEVLOG 623)** | Physical-file registry (`CobolFile.Locks.cs`) — Table-19 → 61, record locks → 51, 53/54 ceilings, UNLOCK, RETRY loop (an exhausted retry lands the conflict's own status; only FOREVER on a record conflict → 52), IGNORING-LOCK bypass; golden `file_sharing` byte-exact in one run unit; 10 CobolFileLockTests; 4 SR negatives (1512); 6 matrix rows; EditionGateHints gates (0900) | (d) | AS-BUILT deviations logged in the design section above (sharing-active-on-clause default; sequential record-lock effect = residue). |
| M2-FILE-2 | Line-sequential org + 2002 FILE STATUS codes | open | LANDED | **Verified:** DataBinder.cs:496 MapOrganization, SequentialFile.cs:161/273/276 WriteLine/ReadLine; FileIoDifferentialTests:175 passes | none | Named deliverable landed. **CORRECTION (C1, DEVLOG 623):** the sharing/locking statuses are **5x/6x** (51/52/53/54/61/62 — §9.1.13.8/9), NOT "9x" — landed in Phase 4d M2-FILE-1. Narrow status codes 04/39 remain. |

## M2-OO — object orientation (parent M2-OO-1 + sub-features a–h)

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M2-OO-1 | OO COBOL umbrella (CLASS/METHOD/INVOKE/INHERITS/FACTORY/PROPERTY/INTERFACE/universal/EC-OO) | partial | PARTIAL | 24 oo_* goldens ENABLED (verified on disk — corrected from 25); DEVLOG 600-609. Bulk landed, residue = 1h | none (residue = Phase 3 port) | Catalog ◐ is stale-low. |
| M2-OO-1a | Instance data + typed params + INHERITS + INVOKE SELF/SUPER + multi-method | remain | LANDED | oo_instance_data/method_args/self/inherit/super/object_group ENABLED; DEVLOG 601-603 | none | 45-agent sweep fixed 22 bugs. |
| M2-OO-1b | FACTORY (static) methods and data | remain | LANDED | oo_factory ENABLED; DEVLOG 604 | none | FACTORY OF/ACTIVE-CLASS RAISING → 1h. |
| M2-OO-1c | OVERRIDE / IS FINAL | remain | LANDED | oo_override_final ENABLED; DEVLOG 605 | none | |
| M2-OO-1d | INTERFACE-ID + IMPLEMENTS | post-slice-6 | LANDED | oo_interface / oo_interface_covariant ENABLED; DEVLOG 606 | none | |
| M2-OO-1e | PROPERTY (GET/SET) decl + refs | remain | LANDED | oo_property{,_ref,_explicit_ref,_factory_ref,_methods} ENABLED; DEVLOG 606-607 | none | Pinned __GET_/__SET_ accessors. |
| M2-OO-1f | Universal object ref + __CobolInvoke dispatch | remain | LANDED | oo_universal{,_inherit,_name,_relation} ENABLED; DEVLOG 608 | none | |
| M2-OO-1g | EC-OO-* model (RAISE id/EXCEPTION-OBJECT/USE F4/RAISING via INVOKE) | remain | LANDED | oo_ec_raise_object / oo_ec_goback_raising ENABLED; DEVLOG 609; conformance 1592/1592 | none | |
| M2-OO-1h | OO residue (method own ENV/FILE/SCREEN, REDEFINES/ODO/RENAMES/INDEXED in method data, PROPAGATE ON, FACTORY-OF/ACTIVE-CLASS RAISING, object VIEWS, STOP…RAISING) | implied ◐ | **DATA-MODEL LANDED (DEVLOG 637-640)** | **The four method-scope data-model shapes are LIVE: RENAMES (637, gate-only) / ODO (638, scope-aware LookupDataInScopeOf) / REDEFINES (639, scope-aware target + Tier-B backing static/local routing + COBOLNET1518) / INDEXED (640, per-method index namespace, GR5). `OoGateUnsupportedShapes` stages nothing. Goldens oo_method_{renames,odo,redefines_local,redefines_ws,indexed_search,indexed_two_methods}.** Residue → M2-OO-1i (the OBJECT/FACTORY ENV+FILE division referenceable from methods — a method canNOT own ENV/FILE/WS per §12.4.3/§13.4.3/§13.5.3 SR1; the object/factory FILE gates are CSharpEmitter.Oo.cs:87-90/108-111; method-scope ENV/FILE become hard 1519 errors) + the OO-specific legs (PROPAGATE ON / FACTORY-OF/ACTIVE-CLASS RAISING / object VIEWS / STOP…RAISING) | none (Phase 3 port residue) | Data-model DONE. M2-OO-1i (see its DECISION-COMPLETE design below) + OO-RAISING/VIEWS legs remain (some 2014/2023 surface). |

### M2-OO-1h data-model design (REDEFINES / ODO / INDEXED / RENAMES in METHOD scope) — DECISION-COMPLETE

*Recon workflow wf_e97fb96d-65f (4 readers + synthesis); anchors verified by reading. Un-gates the four
`COBOLNET0899` stages in `DataBinder.Oo.cs` `OoGateUnsupportedShapes` (~:366-384).*

**The one architectural fact.** Method items ARE in the program-scope `Roots` (`BindEntries` does `Roots.Add`
unconditionally, `DataBinder.cs:275`; the per-method lists hold a SECOND reference to the same objects). So the
post-build passes (`UsageInheritancePass`, `ResolveRedefines`, `ClassifyRedefinesClasses`, `OdoResolve`) DO traverse
method items. The fault is `OoScopeSubtree` (`DataBinder.Oo.cs:210-237`, run INSIDE `OoBindMethodData` BEFORE
`BindResolve`): it moves each method item's NAME out of the class-global `ByName` into `m.DataScope.ByName`, but
leaves structure (Parent/Children/RedefinesTarget/Renames66) and the flat global `IndexFields` untouched. **⇒
every pass that resolves by STRUCTURE already works for method items; every pass that resolves a NAME through the
global `ByName`/`IndexFields`/`Roots` is broken in method scope.** The fix adds NO cross-scope machinery — it
makes the four steps consult the method's own scope, reusing the program-scope algorithms verbatim (§11.7.4 GR5:
method-scope-first, then a visible-unshadowed global fallback).

**Root cause per shape.**
- **level-66 RENAMES** — NO real bug; gate over-conservative. `ResolveRedefines`'s RENAMES loop resolves FROM/THRU
  via `FindDescendantOrSelf(root, …)` (structural, scoped to the owning record; `DataBinder.cs:1128-1152`). Verified.
- **OCCURS DEPENDING ON** — BROKEN. `OdoResolve` resolves data-name-1 through the global `ByName` (`OdoModel.cs:239`);
  the method-local name was moved out → false `COBOLNET0851` or a torn read of a same-named program item.
- **REDEFINES** — subordinate (02+) OK (`item.Parent?.Children`); top-level 01/77 BROKEN (`item.Parent?.Children
  ?? Roots` at `:1123` → a top-level method redefiner has Parent==null → global `Roots`, matches the wrong same-named
  anchor). Plus emission: the Tier-B backing `cls.BackingCsName` isn't scope-routed for a method canonical root
  (static-mismatch for method-WS; leaks as a class field for a method LOCAL/LINKAGE table).
- **OCCURS INDEXED BY** — BROKEN. Index-names register into the flat global `IndexFields` with a de-dup guard
  (`DataBinder.cs:946-951`), so two methods each `INDEXED BY IX` share one `_IX` cell (torn cross-method aliasing,
  §11.7.4 GR5 violation); emission scope (static vs per-activation local) also wrong.

**Shared infra (B.0).** Add `internal Dictionary<DataItem,OoMethodSymbol> OoRootOwner` (ReferenceEqualityComparer),
populated in the `OoBindMethodData` root loop (`:130`). Add `LookupDataInScopeOf(anchorRoot, name)` = the owning
method's `DataScope.ByName` first, then the globals (used by ODO + REDEFINES since `ActiveMethodScope` is null
during `BindResolve`).

**Per-shape fix.**
- **RENAMES:** delete the gate (`:377-379`). No binder/emitter change.
- **ODO:** `OdoResolve` — replace the raw `ByName` lookup with `LookupDataInScopeOf(RootOf(item), depName)`. Delete
  gate (`:374-376`). A method table's data-name-1 legitimately resolving to a visible object item is LEGAL (GR5).
- **REDEFINES:** `ResolveRedefines:1123` — top-level scope becomes the owning method's own roots
  (`m.StaticRoots.Concat(LocalRoots).Concat(LinkageRoots)`) via `OoRootOwner`, else `Roots`. A method 01 REDEFINES
  whose target isn't in the method's own roots → a new "target not in scope" diagnostic (a method may NOT redefine
  object data, §13.18.44.3 SR). Backing scope: route `cls.BackingCsName` to `OoStaticRootFields` (method-WS static)
  or emit as a method local beside the LOCAL/LINKAGE `RootDecl` loops (`CSharpEmitter.Oo.cs:455-474`). Delete gate
  (`:368-370`).
- **INDEXED BY:** new per-method index namespace — add `Dictionary<string,string> IndexFields` to
  `OoMethodDataScope`; route registration via a `_bindingMethodScope` context field (always a FRESH cell for GR5
  privacy); make `TryGetVisibleIndexField` + SEARCH (`StatementBinder.cs:962/969/1011/1020`) consult
  `ActiveMethodScope?.IndexFields` first; emit the cell static (method-WS) or per-activation local (LOCAL/LINKAGE).
  Delete gate (`:371-373`).

**Gate-removal order (each ships green + golden + full legacy guard + DEVLOG, smallest blast radius first):**
1. RENAMES (`oo_method_renames`) · 2. ODO (`oo_method_odo`, +B.0) · 3. REDEFINES (`oo_method_redefines_local` +
`oo_method_redefines_ws` @`--std 85` + a scope-err unit) · 4. INDEXED (`oo_method_indexed_search` +
`oo_method_indexed_two_methods` GR5 regression + a shadow unit).

**Scope rules (§11.7.4 GR5; REDEFINES §13.18.44.3 SR2/4/6/7; RENAMES §13.18.45.3 SR2/3/4; OCCURS §13.18.38.3
SR2/7/20):** a method's names are method-private (sibling-method invisibility); GR5 grants a method SHADOWING over
same-spelled object names, not blanket object-data access — no facility for a method item to REDEFINES/RENAMES
instance data. Method WS is §13.5.3-SR1-banned at 2023 (the EditionValidator `method-working-storage-window` row) —
so the shape goldens target LOCAL-STORAGE/LINKAGE (2023-clean) + one method-WS golden pinned `--std 85`.

**Separate follow-ups (NOT this leg):** method own ENV/FILE/REPORT/SCREEN (`OoBindMethodData:92-101`) and
EXTERNAL/GLOBAL on method WS (`:111-116`) — orthogonal subsystems, keep their loud gates → **M2-OO-1i**.

### M2-OO-1i — the OBJECT/FACTORY ENVIRONMENT + FILE division (files referenceable from methods) — ⛔🎉 LANDED & REVIEW-HARDENED (DEVLOG 644-649)

**REVIEW (DEVLOG 649, wf_7355579f-e66 — 4 reviewers → per-finding verify): 8 confirmed defects, ALL FIXED.** The
review predicted a THIRD class-emit gap and found three: REPORT SECTION in an object/factory (the complete Report
Writer is now WIRED into the class path via `RwEmitReportMembers`/`RwEmitReportConstruction` — golden `oo_object_report`;
NOT gated, an owner correction), an OBJECT SD → CS0103 (SD keeps a static `Class::SORT::` key — golden `oo_method_sort`,
SORT-in-object works), and a method EC-I-O `__IoCheckEc` gap. Runtime: a `~CobolObject` finalizer DATA RACE (mutated
single-thread registries from the GC thread — now enqueue-on-finalizer / drain-on-mutator), the EXTERNAL keyed-register
`::EXT::` idempotency guard, a keyed `CloseAndDrop` leak (`KeyedDrop`). Diagnostics: COBOLNET1519 REPORT/SCREEN §
citations corrected (§13.8.3/§13.9.3), and GLOBAL-on-data-items → COBOLNET1520 (§13.18.27.3 SR4, was FD-only). Battery
after review: 1973 conformance · 216 unit · legacy integration 81 GREEN.

**AS-BUILT (all 5 increments landed exactly to the design below; DEVLOG 644-648).** (1) method ENV/FILE/REPORT/SCREEN
→ hard COBOLNET1519 (a method may not own them, §12.4.3/§13.4.3 SR1). (2) `FileKeyExpr` — the one canonical
connector-key expression (byte-identical sweep of ~28 sites). (3) FACTORY files register in the class singleton
(`OoQualifyClassFiles` `Class::FACT::name` + `OoEmitFileMembers` ctor registration; COBOLNET1520 GLOBAL guard); a
root-cause fix made the `using CobolNet.Runtime.IO` + entry-wrapper `CobolFile.Init/CloseAll` scaffolding count class
files (`anyFiles`). (4) OBJECT files are PER-OBJECT connectors — a minted `__fkey_X = MintInstanceKey("Class::INST::name")`
field, ctor registration + `__TrackInstanceFile`, and a §9.1.4 `~CobolObject()` finalizer refined to suppress-by-
default / re-arm-only-for-file-owners. (5) EXTERNAL object/factory FD shares the one run-unit connector + record area
(`::EXT::` key + the extracted `EmitExternalBackings` now emitted for classes too — `CallBindExternalAndGlobal`
already ran on the class binder). Goldens: `oo_factory_file`, `oo_object_file`, `oo_object_file_two_instances`,
`oo_external_file_shared` (all GreenfieldOnly) + the `FactoryFile_Global_1520` / `ObjectFile_Global_1520` /
`MethodMayNotOwnEnvOrFileSection_1519` OoSpineTests facts. **Two AS-BUILT deviations from the recon's original framing,
both spec-corrections:** the recon's "method-scoped file map + scope-aware resolver" was discarded (a method cannot
own files at all — §13.4.3 SR1); and the `anyFiles`/`EmitExternalBackings` class-emit gaps (not in the recon) were
caught by incremental compile-and-run, the standing lesson that emit only proves itself when you run it. Diagnostics:
COBOLNET1519 (method placement), 1520 (GLOBAL in factory/instance/method). **Deferred (documented, out of scope):**
REPORT/SCREEN sections + SORT/MERGE SD in an object/factory (their own legs); method-WS EXTERNAL/GLOBAL (keeps its
0899); dynamic `ASSIGN TO data-name` resolving the field value (a pre-existing limitation surfaced here, not caused).

### M2-OO-1i — the OBJECT/FACTORY ENVIRONMENT + FILE division (files referenceable from methods) — DECISION-COMPLETE DESIGN

*Recon workflow wf_5d22beb6-140 (4 readers + xhigh synthesis, 2026-07-06). **All 7 load-bearing spec citations
verified verbatim in-session** (§12.4.3 SR1 :14920 · §13.4.3 SR1 :16184 · §13.5.3 SR1 :16461 · §13.6.3 SR1 :16513 ·
§13.18.27.3 SR4 :19038 · §9.1.4 :11206-11218 · §11.7.4 GR5 :13281) and the code gates/reparent/grammar confirmed by
reading (CSharpEmitter.Oo.cs:87-90/108-111/144-164, CobolOO.g4:31/50).*

**⛔ THE RE-FRAMING (the ticket title is spec-impossible).** A method definition **cannot** own an ENVIRONMENT
DIVISION, a FILE SECTION, a WORKING-STORAGE / REPORT / SCREEN SECTION — §12.4.3 SR1 / §13.4.3 SR1 / §13.5.3 SR1 all
say those sections may appear *only in a factory or instance definition, not in a method definition* (a method may
own **only** LOCAL-STORAGE §13.6.3 + LINKAGE §13.7.3). So M2-OO-1i's real construct is **the OBJECT-paragraph and
FACTORY-paragraph INPUT-OUTPUT SECTION (FILE-CONTROL) + FILE SECTION (FDs), referenceable from the class's method
bodies** via §11.7.4 GR5 (:13281 — a name in the containing object definition is visible in its methods). The two
emitter gates `CSharpEmitter.Oo.cs:87-90` (OBJECT) / `:108-111` (FACTORY) — `if (data.Files.Count > 0) 0899` — are
what stand in for it today; the DataBinder.Oo.cs method-ENV/FILE gates are for a construct the spec **forbids**.

**Three facts that collapse most machinery.** (1) **Object/factory file binding is already wired** —
`OoReparentClassData`/`OoReparentFactoryData` (:144-164) reparent `obj/fac.environmentDivision()` +
`.dataDivision()` into the synthetic unit, so `data.BindDeclarations` already runs `BindFileControl` +
`BindFileSection`; that is *why* `data.Files.Count > 0` at the gate. FD records already flow to `data.Roots` →
`FieldEmitter` as instance fields (object) / factory-type fields (factory) — correct storage, no re-routing. (2)
**Method bodies already see object/factory files with zero scoping** — they bind through `new
StatementBinder(cls.Data, cls.Refs)` (:120), and `cls.Data.FilesByName` holds the object files, so OPEN/CLOSE/
READ/WRITE/`FileOfRecord`/keyed-I/O/USE/LINAGE-COUNTER resolve unchanged. FILE STATUS / RECORD KEY / DEPENDING
operands are *object WS* items in `data.ByName` (never moved by `OoScopeSubtree`, which only re-homes **method**
names) → `ResolveFiles` needs no scope-awareness. **Discard the recon's method-scoped-file-map / scope-aware-resolver
idea — it is for a construct that cannot exist.** (3) **The only real gaps:** (a) the loud gates → proper
diagnostics/emission, and (b) connector REGISTRATION + connector-KEY namespacing (the class emit path never runs
`__Activate`/`EmitFileRegistration`; the qualification loop at `CSharpEmitter.Call.cs:138-142` iterates *program
units only*, so class FileModels are never registered nor key-qualified).

**Decided semantics (each spec-cited).**
- **(a) Instance file-connector lifetime = per-object** (not per-class-shared). §9.1.4 (:11216) executes an implicit
  CLOSE "for file connectors in an object when the object is deleted"; the NOTE (:11218) licenses GC-deferred close.
  Realized as a per-object connector key in an instance field, closed by a finalizer (run-unit `CloseAll` is the
  backstop). *Distinct from method-WS's static-per-class call — a file connector is object-lifetime.*
- **(b) EXTERNAL object/factory FD → the one run-unit-shared connector.** `IS EXTERNAL` is permitted on any FD
  (§13.18.22 SR1); §9.1.3/§9.1.5 make it one run-unit connector by name. `CallBindExternalAndGlobal` already
  re-bases the record area onto the `ExternalStore` cell (it runs on `cls.Data` via `BindResolve`); the only missing
  half is the emit-side `::EXT::` connector-key rewrite (today program-units only). `CobolFile.Register`'s idempotent
  guard then yields exactly one shared connector — no per-object key for EXTERNAL.
- **(c) GLOBAL in a factory/instance/method = hard error.** §13.18.27.3 SR4 (:19038, verified): "The GLOBAL clause
  shall not be specified in a factory definition, an instance definition, or a method definition." → **COBOLNET1520**
  (not a no-op, not a silent accept). Program↔class file sharing is achievable *only* via EXTERNAL (§9.1.5).
- **(d) Name scope** — object/factory files are visible in method bodies (§11.7.4 GR5); a surrounding program's
  files are **not** visible across the class boundary (GLOBAL barred here; a method is not a contained program).
  Two sibling methods cannot collide on a file-name — neither owns a file; they share the one object/factory file.

**Implementation plan (file-by-file).**
- *Binder `DataBinder.Oo.cs`* — replace the method-placement catch-all 0899s with **COBOLNET1519** (one class of
  error, "construct X not permitted in a method definition", message names the specific section + §): the method
  `environmentDivision()` gate (§12.3.3 SR2 / §12.4.3 SR1) and the `fileSection`/`reportSection`/`screenSection`
  split (§13.4.3 SR1 / §13.9 / §13.10). Method WORKING-STORAGE keeps its existing edition-window path; method-WS
  EXTERNAL/GLOBAL keeps its 0899 (follow-up). No other binder change.
- *Emitter `CSharpEmitter.Oo.cs`* — remove the two object/factory FILE gates (:87-90, :108-111); add a **GLOBAL guard
  (COBOLNET1520)** post-`BindResolve` on both `data`/`fdata` (`foreach f in Files if (f.IsGlobal) …`); add
  `OoQualifyClassFiles(cls)` (mirror of the program loop Call.cs:138-142): instance file →
  `f.IsExternal ? "::EXT::"+ext : cls.CsName+"::INST::"+name` and set `f.InstanceKeyField="__fkey_"+San(name)`;
  factory file → `… "::FACT::"+name`. New `OoEmitFileMembers(data, factory, w)` in `OoEmitTypeHalf` after
  `fields.Emit()`: instance-non-EXTERNAL → an instance field `__fkey_<name> = CobolFile.MintInstanceKey(base)` + a
  registration line in the emitted instance ctor (base ctor chains first, D4 order) + `__TrackInstanceFile(key)`;
  instance-EXTERNAL / factory → literal-key registration (factory in the factory-singleton ctor; EXTERNAL
  registration idempotent). Report-file / keyed / sharing registration reuse the existing
  `EmitFileRegistration`/`KeyedEmitRegistration`/`EmitSharingRegistration` bodies with the connector-key arg swapped
  to `FileKeyExpr(file)`.
- *Emitter `FileKeyExpr` sweep* — one helper
  `FileKeyExpr(f) => f.InstanceKeyField is {} fld ? $"this.{fld}" : CsLiteral(f.CobolName)`; replace every I/O
  connector-name site that emits `CsLiteral(file.CobolName)` (a full grep sweep across CSharpEmitter.cs / .KeyedIo /
  .Sort / .ReportWriter / .Exceptions + NumericRenderer LINAGE-COUNTER — feedback_scan_all_similar). For every
  program/EXTERNAL file `InstanceKeyField` is null → identical literal → **byte-identical output**, proven by the
  full battery (not a new golden; feedback_refactor_first_always / feedback_singular_pattern).
- *Runtime* — `CobolFile.cs`: `MintInstanceKey(base) => base+"#"+Interlocked.Increment(ref _instSeq)` (`_instSeq`
  reset in `Init()` for determinism) + `CloseAndDrop(key)` (per-object implicit CLOSE + registry removal); `Register`
  unchanged. `CobolObject.cs`: `__instFiles` list + `__TrackInstanceFile` + a finalizer `~CobolObject()` calling
  `CobolFile.CloseAndDrop` (§9.1.4). `FileModel.cs`: add `string? InstanceKeyField`.

**Diagnostics.** COBOLNET**1519** (a section not permitted in a method: ENV / FILE / REPORT / SCREEN — one error
class, message names which) · COBOLNET**1520** (GLOBAL on a factory/instance/method FD, §13.18.27.3 SR4). Both in the
free 15xx band (1518 was last-used HERE; the M2-OO REDEFINES leg was later renumbered to 1577 when 1518 was double-allocated to the locale non-support — P13 review V11). Edition-invariant — OO cannot parse below 2002, so no explicit dialect guard.
*Not over-diagnosed:* method-WS keeps 0900/0902; method-WS EXTERNAL/GLOBAL keeps 0899.

**Golden & test plan (one golden per branch; all GreenfieldOnly — legacy can't parse OO).** Positive corpus goldens
in `tests/conformance/2002/` (driver PROGRAM + CLASS; sequential FD OPEN OUTPUT→WRITE→CLOSE→OPEN INPUT→READ→DISPLAY):
(1) `oo_object_file` — instance file, one object → `GOT=HELLO OBJECT`; (2) `oo_object_file_two_instances` — two
objects write/read their own files un-closed → `A=AAA`/`B=BBB` (proves distinct `__fkey#n`, §9.1.4); (3)
`oo_factory_file` — factory singleton file → `FGOT=FACTORY DATA`; (4) `oo_external_file_shared` — EXTERNAL program↔
object shared connector + record area. Registration recipe per golden: `.cob`+`.out` + `enabled` in `manifest.json`
+ `("2002","<name>")` in `ConformanceTests.cs` `GreenfieldOnly` (same change set — feedback_legacy_suite_on_shared_corpus).
Negative facts in `OoSpineTests` (CRLF-safe `.Replace("\r\n","\n")` splice, DEVLOG 641 lesson): method FILE/ENV/
REPORT/SCREEN SECTION → 1519 (one assert each); OBJECT/FACTORY `FD … IS GLOBAL` → 1520.

**Increments (smallest first; build + full battery + commit + DEVLOG each).** (1) **method-placement diagnostics**
(DataBinder.Oo.cs → 1519 split + the OoSpineTests facts; no emission change). (2) **`FileKeyExpr` refactor** (helper
+ `FileModel.InstanceKeyField` + the sweep; byte-identical, battery-proven, no golden). (3) **factory files** (remove
:108-111 gate; `OoQualifyClassFiles` factory branch + ctor registration + 1520 factory half; golden 3 + the FACTORY
1520 fact). (4) **instance files** (remove :87-90 gate; instance branch + `MintInstanceKey`/`CloseAndDrop` +
`__TrackInstanceFile`/finalizer + 1520 object half; goldens 1+2 + the OBJECT 1520 fact). (5) **EXTERNAL object/factory
FD** (`::EXT::` qualification; golden 4). Update this doc's AS-BUILT row + resume-prompt STATE at close of (5).

**Out of scope (stay loud / follow-up):** REPORT (RD) / SCREEN sections in an object/factory paragraph (their own
legs); SORT/MERGE SD in an object/factory (in-memory store + method RELEASE/RETURN — the `FileKeyExpr` sweep touches
SD sites for uniformity but no SD golden ships); method-WS EXTERNAL/GLOBAL. **Open questions: none require the owner.**

## M3 — COBOL-2014

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M3-1 | OCCURS DYNAMIC [CAPACITY IN][FROM…TO] | open | NOT-STARTED | **Verified:** zero CAPACITY/OCCURS DYNAMIC grammar hits; only ACCESS MODE DYNAMIC exists; 2014 corpus seeded empty | phase 6 | Phase 6 serial spine (deep-dive first). |
| M3-2 | TYPEDEF / SAME AS / TYPE TO | open | PARTIAL | TYPEDEF + TYPE DONE (data-model D17; goldens typedef_*); SAME AS DONE (P10 Step 16 — §13.18.49 via the ONE CloneItem/ExpandSameAs; golden typedef_same_as; row same-as-clause-2002 active); EXTERNAL type declarations DONE (§13.18.22 = 1558; golden typedef_external) | phase 6 | Remaining: TYPE TO (the pointer-target form) — deferred, P12 Step 10 catalogues it. |
| M3-3 | JSON & XML GENERATE/PARSE + XML-CODE/JSON-CODE | open | STAGED-LOUD | Seam grammar only; COBOL0313 vendor diagnostic (EditionGateHints.cs:72-74) | phase 6 | Owner decision-2: VENDOR extensions, zero ISO hits. ISO framing OBSOLETE; statements staged loud. |
| M3-4 | File lock finalization / function-method pointers / IEEE-754 / limits / cond-expr | open | PARTIAL | CLOSE…WITH LOCK binds + 2023-removal gated; OPEN SHARING/record LOCK/RETRY/UNLOCK reserved-only; FUNCTION/PROGRAM-POINTER reserved-only | d (+ b, 6/7) | Bundle: (d) sharing/lock; (b) USAGE FUNCTION/PROGRAM-POINTER; IEEE/limits/cond-expr → 6/7. "method pointers" wording wrong (0 spec hits). |

## M4 — COBOL-2023

| ItemId | Title | CatalogMark | GreenfieldStatus | Evidence | Phase4Track | Notes |
|---|---|---|---|---|---|---|
| M4-1 | DELETE FILE execution (§14.9.10 Fmt 2) | done | **LANDED (DEVLOG 612, track d)** | Keyed + SEQUENTIAL legs LANDED (CobolFile.DeleteFile handles all organizations via SequentialFile.HostPath); delete_file + delete_file_absent (2023) ENABLED (GR13 round-trip 35 + GR14 absent 05) | (d residual) | Single file-name only; the multiple-file-name form (`DELETE FILE f1 f2…`) needs the fileName+ grammar — a documented follow-up. |
| M4-2a | logical XOR / EXCLUSIVE-OR | done | LANDED | **Verified:** StatementBinder.cs:1144 BindFlatSequence(xorExpr,"^"); logical_xor.cob+.out ENABLED; re-editioned 2023 (DEVLOG 596) | none | Parse+bind+emit(^)+green golden. |
| M4-2b | SMALLEST-ALGEBRAIC + EXCEPTION-FILE-N intrinsics | open | **LANDED** | SMALLEST-ALGEBRAIC = a compile-time PICTURE fold (`IntrinsicBind.Fold`, BindAlgebraicFold — the HIGHEST/LOWEST family, DEVLOG 634; `smallest_algebraic` ENABLED). EXCEPTION-FILE-N (+ EXCEPTION-LOCATION-N) LANDED P10 Step 11 (2026-07-16): `EcFunctions.FileN/LocationN` via the ONE `NationalOf` translator, category National; `exception_file_n` ENABLED | none | The former "blocked on national" note is resolved (national landed P10 Steps 2/5). Residue: only the 2023 file-connector-argument form (both base + twin) — loud, VCR rows 68/69, PHASE-13 Step 9. |
| M4-3 | Other 2023 intrinsic/bit/boolean + dynamic-table finalization | open | PARTIAL | **CONCAT (§15.18) + BASECONVERT (§15.12) LANDED (DEVLOG 628); TRIM (§15.96) LANDED COMPLETELY (DEVLOG 629)** — all forms (LEADING/TRAILING/both, default-space + the 2023 argument-2 char set, gated 1502 below 2023) via a bespoke BindTrim path; goldens intrinsics_string_2023 + intrinsics_trim (GreenfieldOnly). Residue STAGED-LOUD: CONVERT/FIND-STRING/MODULE-NAME/SMALLEST-ALGEBRAIC/SUBSTITUTE | phase 5 (+ a, 6) | Umbrella. Intrinsics→5 (FIND-STRING/SUBSTITUTE/CONVERT carry keyword modifiers → a shared keyword-arg mechanism; MODULE-NAME needs module context; SMALLEST-ALGEBRAIC needs PIC metadata); boolean/bit→4(a) not-started; dynamic-table→6 (overlaps M3-1). |

## Wave sizing (remaining work per track)

Excludes 13 LANDED and 1 OBSOLETE-by-design (M2-PROC-3, warning-row only). 24 rows carry remaining work.

| Track | Owns (remaining) | Count | Notes |
|---|---|---|---|
| **(a) national/boolean** | M2-DATA-3, M2-DATA-4; + boolean/bit leg of M4-3; ~~+ EC-OO -N twins, EXCEPTION-FILE-N national leg (M4-2b)~~ **the -N/EC-N legs LANDED (P10 Step 11)** | 2 primary | National runtime + boolean ops. The -N intrinsic legs this track blocked are now live (CHAR-NATIONAL, EXCEPTION-FILE-N/-LOCATION-N). |
| **(b) pointers/ALLOCATE/BASED** | ~~M2-DATA-5, M2-PROC-5~~ **LANDED (DEVLOG 613/617)**; + USAGE FUNCTION/PROGRAM-POINTER leg of M3-4 | 0 primary (+1 shared leg) | Data pointers live end-to-end (StorageCell+CellPointer); residue = CALL-boundary pointers + the M3-4 typed-pointer leg (as-built list). |
| **(c) UDF/prototypes** | ~~M2-UDF-1, M2-UDF-2~~ **LANDED (615)**; ~~EXIT FUNCTION leg of M2-PROC-6~~ **LANDED (616)**; ~~M2-UDF-3 prototypes/cross-assembly~~ **LANDED (624)**; ~~M2-UDF-4 ALL INTRINSIC + keyword-omitted~~ **LANDED (626)**; residue: `>>CALL-CONVENTION` (loose), strict SR10/SR12/SR13 (staged) | **0 primary** | ⛔🎉 The UDF subsystem (track (c)) is COMPLETE — invocation + EXIT FUNCTION + prototypes/cross-assembly + REPOSITORY specifiers/keyword-omission all live. |
| **(d) file sharing/lock/retry** | M2-FILE-1, M4-1 (sequential leg); + file-lock leg of M3-4; + narrow status codes of M2-FILE-2 | 2 primary (+2 legs) | Runtime CobolFile.Locked primitive exists. |
| **(e) arithmetic** | M2-ARITH-1 (PROHIBITED move-COMPUTE fix), M2-ARITH-2 (golden rebaseline + inert legs) | 2 (both small) | Effectively bugfix + rebaseline, not new features. |
| **(f)** | — | 0 | No catalog item maps to (f). |
| **(g)** | — | 0 | No catalog item maps to (g). |
| **Phase 4 misc (unlettered)** | ~~M2-DATA-1 (BINARY-CHAR family numeric-usage)~~ **LANDED (DEVLOG 614)** | 0 | Native fixed-width binary integers + the BinaryCapacity wrap (also cures COMP-5's stubbed overflow). |
| **Phase 3 OO port residue** | M2-OO-1h (0899-staged edges) | 1 | Not an (a)-(g) track; owned by the OO port. |
| **Phase 5 (intrinsics)** | M4-2b, M4-3 (intrinsic slice: BASECONVERT/CONCAT/CONVERT/FIND-STRING/MODULE-NAME/SMALLEST-ALGEBRAIC/SUBSTITUTE) | 2 | Catalogued+gated+loud; runtime bodies remain. |
| **Phase 6** | M2-DATA-2 (IEEE floats), M3-1 (OCCURS DYNAMIC), M3-2 (TYPEDEF/SAME AS/TYPE TO), M3-3 (JSON/XML vendor), + dynamic-table leg of M4-3 | 4 primary (+1 leg) | OCCURS DYNAMIC deep-dive is the serial spine. |
| **Phase 7** | M2-PROC-3 (VALIDATE warning row), M2-PROC-6 (CONTINUE AFTER remnant); + M2-PRE-1, M2-PRE-2 (preprocessor robustness, no lettered track) | 2 primary (+2 preproc) | Disposition sweep / low-severity follow-ups. |

## Corrections (claims adjusted or clarified)

1. **M2-OO-1 golden count: 25 → 24.** The audit row says "25 oo_* goldens ENABLED"; disk truth is 24
   oo_*.out files and 24 distinct oo_* names in the 2002 manifest. Sub-feature evidence (1a-1g) is
   otherwise accurate. Correction is cosmetic — does not change the LANDED verdict for any sub-item.
2. **M2-PROC-2 diagnostic code named.** Audit left the code unstated; the greenfield gate is
   `COBOLNET0845` (InspectBinder.cs), gated to 2023 (not 2002). Confirms the row's own
   2023-framing note over the catalog's 2002 framing. No status change.
3. **No status marks overturned.** All 8 spot-checked LANDED (INITIALIZE, INSPECT BACKWARD, Exception
   handling, GOBACK RETURNING, line-sequential, OO-1a/e/g representative, XOR) verified genuinely
   end-to-end with green goldens. All 3 NOT-STARTED (UDF-3 prototypes, OCCURS DYNAMIC, SHARING/LOCK/RETRY)
   verified as zero greenfield surface. Extra checks (M2-PROC-5 ALLOCATE falls to generic
   BoundUnsupported; M2-PROC-3 VALIDATE zero grammar; PicInfo 19 skeleton-usage hits for DATA-1..5)
   all match the audit. The audit rows are trustworthy for wave planning.

## GreenfieldStatus@P10 (2026-07-16 — the PHASE-10 Step-1 reconciliation audit)

The M2 non-OO tracks were re-audited against the POST-REARCHITECTURE tree (StorageForm / FileConnector /
ManagedPointer / RunUnit) by 13 parallel file:line-citing auditors; the authoritative per-track verdict table +
evidence lives in `docs/rearchitecture/PHASE-10-m2-residual-catalog.md` §Step-1 AUDIT RESULT. Headline: NO track
is fully confirmed end-to-end; 9 are PARTIAL (UDF, national, boolean, pointers, allocate, file-sharing,
line-seq, arithmetic-standard, typedef-residue), 3 were NOT-STARTED (the & concatenation operator — since
LANDED, P10 Step 14 2026-07-16, as the compile-time `ConcatFolder` fold per §8.8.3.3 GR3 — CONSTANT
entries/records, Report Writer 2002 additions), and the EC `-N` twins were STAGED-LOUD blocked on the national
intrinsics (since LANDED — P10 Step 11, 2026-07-16). The stale ☑/◐ marks in this file's older tables are
SUPERSEDED by that section.
