## Entry 601 — 2026-07-04 12:37 PDT — Phase 3 spine part 2: classes ARE C# classes — ClassUnit collection + the pass-1 symbol table, emit-into-a-type, BoundMethodReturn (D8), INVOKE NEW/no-arg — 4 of 9 oo_* goldens GREEN under the run contract

**The OO spine is CLOSED (parts 1+2). A CLASS-ID now compiles to a real `public class FOO : CobolObject`;
`INVOKE GREETER "NEW" RETURNING G` / `INVOKE G "SAYHELLO"` are direct typed C# (`G = new GREETER();` /
`CobolObject.RequireNonNull(G).SAYHELLO();`). oo_hello ran byte-exact on the FIRST compile — and so did
oo_instance_data (trap #1 two-object independence), oo_method_perform, and oo_object_group; all four are
ENABLED in the 2002 manifest run contract. Legacy port slice 1 (+ the 455-slice method scoping) is subsumed.**

- **Pass-1 class symbol table (D1; new `Binding/OoClassTable.cs`):** built in `CallCollectUnits` from the
  parse contexts BEFORE any unit binds, so a driver's typed refs + INVOKEs see classes defined later in the
  file. Structural diagnostics at build: 0820 duplicate class / END CLASS↔CLASS-ID (and END METHOD↔METHOD-ID)
  mismatch, 0821 unknown INHERITS base (trap #8 — never silently a root), 0822 duplicate method (D9 v1),
  known-base INHERITS 0899-staged to slice 3a (the base LINK is recorded — method lookup walks the chain
  day one). CsName mapping = `Sanitize+upper` — the SAME convention part 1 baked into `PicInfo.ClrType`.
- **Typed object references UN-STAGED (DataBinder):** the part-1 0899 replaced by table validation — unknown
  declared class → **new 0813** (§13.18.60.4); the field emits as `FOO?`.
- **Binder (new `StatementBinder.Oo.cs`):** `BindClassBody` flattens every method's paragraphs into the
  class's ONE pc space; per-method `OoMethodScope`s make paragraph/section names METHOD-LOCAL (§11.7) with
  NO program-wide fallback — a cross-method PERFORM/GO TO fails loud with a method-scope hint (trap #10
  structural; deliberate delta from the legacy "method-first then program-wide" — recorded in the deep-dive).
  SECTIONs inside methods are now IMPLEMENTED (method-scoped ranges — supersedes legacy COBOL0116's reject).
  `OoBindInvoke`: identifier-shadows-class-name resolution; NEW requires RETURNING + §14.8 receiver
  conformance (0826), instance lookup over the base chain (unknown method → **0825**, the compile-time
  GR7b analog), receiver-not-a-reference **0824**, NULL/unresolvable target **0823**; SELF/SUPER (3b),
  factory, USING/RETURNING (slice 2), universal dispatch (D10) stage LOUD. Method contexts: GOBACK →
  `BoundMethodReturn` (§14.9.18.4 GR4), EXIT METHOD → same (0902-at-2023 rides the existing
  exit-method-window row; **0827** outside a method), EXIT PROGRAM in a method → **0827** (§14.9.14.3 SR7).
- **D8 realized CATCH-AT-ENTRY (deliberate, documented in the deep-dive's AS-BUILT note):** the preferred
  `return`-past-the-loop form cannot unwind the NESTED bounded `__Dispatch` frames an out-of-line PERFORM
  stacks — so `BoundMethodReturn` → `throw new MethodReturn()` (new `Runtime/Control/MethodReturn.cs`, the
  ProgramReturn pattern scoped to methods), caught at the method's public entry. STOP RUN in a method stays
  StopRun (must NOT be caught at the method boundary — the run unit dies, proven by test).
- **Emitter (new `CSharpEmitter.Oo.cs`):** `OoClassUnit` + `OoBindClassUnit` (per-class DataBinder over a
  CallReparent-style synthetic unit adopting the OBJECT paragraph's divisions; class FILE SECTION 0899) +
  `OoEmitClassUnit` — the SAME per-unit emitter-state switch as `CallEmitProgramClass`: FieldEmitter
  instance fields (VALUE → field initializers; the implicit ctor IS the predefined NEW, D4), one
  `public virtual` method per METHOD-ID running `__Dispatch(entry, last)` exit-bounded (trap #4 structural),
  and the shared `__Dispatch` body — extracted as `EmitDispatchMethod`, consumed unchanged by programs AND
  classes (the emit-into-a-type parameterization; every PERFORM/SORT emit site reuses it untouched).
  Classes emit before programs; a class-only unit is legal (empty Main).
- **Tests:** new `OoSpineTests` ×12 — traps #4/#10 as runtime facts, D8 GOBACK-unwinds-nested-PERFORM,
  STOP-RUN-in-method, EXIT METHOD 2002/2014 run + placement 0827s, the 0813/0820/0821/0822/0825/0826 band,
  staged-forms-stay-loud. DataSkeletonEditionTests' classDefinition 0899 fact flipped to
  compiles-at-2002+/rejected-at-85. The 3 pending matrix rows (class-definition/invoke/usage-object-
  reference-2002) flipped ACTIVE per their activation contracts (+12 matrix cells; the invoke row's source
  upgraded to a live typed driver+class). Manifest: oo_hello/oo_instance_data/oo_method_perform/
  oo_object_group ENABLED (byte-compared); oo_method_args (slice 2), oo_inherit/oo_super/oo_self/
  oo_self_polymorphic (3a/3b) stay pending.
- **Battery:** conformance **1472/1472** (+18) · unit 101/101 · **INV-1-STRONG 349/349 byte-exact at
  2023-permissive** · continuity sweep 438 OK + ST137A transient BREAKS@2023 (solo re-run CLEAN at
  2002/2014/2023 — the DEVLOG-590 watched parallel-run flake pattern, third sighting; still JOBS-race
  suspected) / 20 SKIP85 · registry drift green. Zero grammar (.g4 untouched) and zero legacy-compiler
  exposure (no `src/CobolSharp.Compiler` change) — the legacy guard basis is unchanged (the part-1 posture).
- **AI friction, honest:** two false starts editing `constructs.json` (a whole-file json.dumps rewrite
  reformatted 249 unrelated lines — reverted; then a heredoc mangled `\n` escapes into real newlines —
  reverted); the surgical fix used json.dumps per-VALUE only. The file also carries pre-existing mojibake
  (`Â§`) in the invoke-2002 row's original text, which defeated exact-match replacement — worth a cleanup
  pass someday.

**NEXT (Phase 3, the ported slices in the banner order): slice 2** — method LINKAGE→typed params +
LOCAL-STORAGE→locals (+ the method-WS edition gate: static fields ≤2014, §13.5.3-SR1 diagnostic at 2023 —
Spec corrections #1), INVOKE USING/RETURNING marshaling (D6), `BoundMethodReturn` carrying the RETURNING
delivery, oo_method_args green. Then 3a INHERITS (`: BASE`, override-under-exact-name — trap #2) →
oo_inherit/oo_super, 3b SELF/SUPER → oo_self/oo_self_polymorphic; then FACTORY → PROPERTY → INTERFACE-ID →
universal reference → EC-OO.

