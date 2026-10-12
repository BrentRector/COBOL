## Entry 602 — 2026-07-04 14:16 PDT — OO port slice 2: typed method parameters — LINKAGE→ref params over capturable locals, LOCAL-STORAGE→locals, method-WS statics + the §13.5.3 window, INVOKE USING/RETURNING under §14.8.2 strict conformance — oo_method_args GREEN (5 of 9)

**Methods now have real typed signatures.** `PROCEDURE DIVISION USING LK-AMT RETURNING LK-RES` over
`01 LK-AMT PIC 9(4)` emits `public virtual long ADDTO(ref long __LK_AMT)`; the INVOKE site passes
`ref AMT` DIRECTLY (the fast path — GR7a once-evaluation for free) and stores the return through the
receiver's own MOVE-semantics bridge. oo_method_args ran byte-exact; the legacy-never-landed
multi-method LINKAGE (COBOL0117's reject) is DONE net-new.

- **The load-bearing design fact (recorded in the deep-dive D6 AS-BUILT):** §14.8.2's STRICT
  BY REFERENCE/BY CONTENT conformance (identical description — category/digits/scale/sign/USAGE,
  alphanumeric length, exact obj-ref class, group image width; the new **0828** band) is what makes every
  crossing TYPE-PRESERVING — the call site never converts, so it never touches the invoked class's
  PRIVATE numeric profiles (the cross-class-profile trap that would otherwise force internal/qualified
  profile emission). Bind-time literal fit-checks (string math); BY VALUE args stage 0828 pending the
  unparsed header BY-phrases; OMITTED later.
- **§14.9.23.3 SR 10 implemented both ways** (spec re-read this session): a BARE object-data argument
  cannot satisfy SR 9/10, so GR6a2 assumes BY CONTENT (callee writes invisible — proven by run fact);
  EXPLICIT BY REFERENCE of object data is 0828. `DataBinder.OoIsObjectData` discriminates via the
  method-scoped-roots set.
- **Method data binding (new `DataBinder.Oo.cs`):** `Bind` split into `BindDeclarations`/`BindResolve`;
  `OoBindMethodData` binds each method's LINKAGE/LOCAL-STORAGE/WS between them (post-build passes cover
  method items — profiles/struct types emit at class level), moves the subtree's name+88 registrations
  into a per-method `OoMethodDataScope` (§11.7 GR5 — the resolver + ConditionOf overlays shadow object
  data and hide sibling scopes: trap #6 STRUCTURAL, cross-wiring test green with same-named different-
  typed LK-V in sibling methods), resolves the PD USING/RETURNING formals positionally (0888/0889 reuse),
  and stages loud the not-yet shapes (REDEFINES/INDEXED/ODO/66/EXTERNAL/GLOBAL in method data;
  FILE/REPORT/SCREEN method sections; PD RAISING → EC-OO slice). Class binding is now TWO-PHASE
  (all classes' data+signatures, THEN all bodies) so cross-class INVOKE sees signatures source-order-free.
- **The LOCAL-FUNCTION dispatcher realization (supersedes part 2's shared instance __Dispatch —
  deliberately, recorded in the emitter-seam AS-BUILT):** each method's paragraph slice emits inside
  `int __MDispatch(int,int)` — a local function that CAPTURES the method's locals by reference (zero
  allocation; a `ref` param is NOT capturable, hence the param→local copy-in/copy-out at the method
  boundary realizing BY REFERENCE write-through). Reentrancy is structural — every activation owns fresh
  locals; the 3-deep recursion-through-an-obj-ref-formal test (methods are implicitly RECURSIVE, :12032)
  shows perfect frame nesting where the legacy's static `_linkage_` fields could not. `EmitDispatchMethod`
  gained (header, fromPc, toPc); `_dispatchName` threads the PERFORM/SORT emit sites; `__N` stays one
  class-level const; the program emission path is unchanged.
- **Method WS → STATIC fields (D3 complete):** FieldEmitter emits `private static` for
  `OoStaticRootFields`; the two-instance shared-counter run fact (CTR=01/02/03 across T1/T2/T1) proves
  shared-and-persistent — the exact counter the naive instance mapping silently miscompiles. The 2023
  §13.5.3 SR 1 ban: new `method-working-storage-window` registry row + `VisitMethodDefinition` (0900
  below 2002 / 0902 at 2023; permissive keeps the static semantics — RemovedMatrix-verified). ⚠ The
  2002/2014-legal boundary is PINNED PROVISIONAL (Annex E.2 does not itemize the removal; the 2002/2014
  texts are not in-repo) — **VCR Table 6 row 130e** records the pin + the one-line shift if disproven.
- **Matrix mechanism extended — the exit-method-window reactivation contract CLOSED:** new
  `expectDiagnosticBelow` field (dual-window rows reject with 0900 below introducedIn and 0902 at/after
  removedIn); `exit-method-window` flipped ACTIVE with a real driver+class+method source (its pending
  note demanded exactly this: the placement SR now enforced — 0827 — and METHOD-ID units exist);
  `method-working-storage-window` added ACTIVE. VersionMatrix 310/310 incl. the RemovedMatrix
  permissive-migration cells; registry drift green.
- **Tests:** OoSpineTests grew to ×18 — trap #3 arity 0828 (the DEVLOG-449 shift-blocker, now
  compile-time), trap #6 cross-wiring, recursion/reentrancy + LOCAL-STORAGE re-init, SR 10 both
  directions, strict-conformance 0828 (9(4) vs 9(5)) + RETURNING pairing both ways, method-WS static
  semantics + full edition window. Manifest: **oo_method_args ENABLED** (5 of 9; oo_inherit/oo_super
  await 3a, oo_self/oo_self_polymorphic 3b).
- **One emit bug caught by the first oo_method_args compile:** the C# param named identically to the
  item's local (CS0136) — params now `__`-prefixed (a COBOL name can never image `__`).
- **Battery:** conformance 1500/1500 · unit 101/101 · INV-1-STRONG 349/349 byte-exact at
  2023-permissive · sweep 439 OK / 20 SKIP85 / 0 BREAKS · drift + matrix green. Zero grammar and zero
  legacy-compiler exposure (no .g4, no `src/CobolSharp.Compiler` change) — the legacy guard basis is
  unchanged (the part-1/part-2 posture).

**NEXT: slice 3a INHERITS** — `: BASE` emission (single-inheritance v1, SSOT §18.18), override under the
base's EXACT roster spelling (trap #2), subclass-own OBJECT data posture, un-stage the pass-1 INHERITS
0899, oo_inherit green (oo_super likely needs 3a+3b together — INVOKE SUPER); then 3b SELF/SUPER (D5:
`this.M()` virtual / `base.M()` non-virtual; SUPER-in-root diagnostic — trap #7) → oo_self +
oo_self_polymorphic; then FACTORY → PROPERTY → INTERFACE-ID → universal reference → EC-OO.

