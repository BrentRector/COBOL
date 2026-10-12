## Entry 600 — 2026-07-04 01:12 PDT — Phase 3 (M2 OO port) OPENED: spine part 1 — CobolObject runtime base + USAGE OBJECT REFERENCE LIVE (universal), the first OO skeleton row retired

**Roadmap Phase 3 begins (the ratified plan: SPINE first — the BoundMethodReturn/BoundStopRun split + the
emit-into-a-type parameterization — then legacy slices 1–6, then FACTORY → PROPERTY → INTERFACE-ID →
universal reference → EC-OO; `docs/COBOLNET_OO_DESIGN.md` is the deep-dive, regenerated 2026-07-03 with the
four spec corrections). This increment lands the spine's self-contained data-model half:**

- **`CobolObject` (new, `src/Cobol.Net.Runtime/Control/CobolObject.cs`)** — the runtime base of every
  emitted COBOL class (deep-dive D2): the reflection-free `__CobolInvoke(name, args)` universal/dynamic
  dispatch default (reaching it = EC-OO-METHOD, §14.9.23.4 GR7b) + `RequireNonNull` (EC-OO-NULL, GR5) —
  both through the LANDED §14.6.13 EC engine (`CobolFatalException`; the EC-OO Table-13 family has been in
  `ExceptionCatalog` since DEVLOG 577). AOT/WASM-safe by design (D2's rejected alternative was
  reflection-dependent).
- **USAGE OBJECT REFERENCE is LIVE (universal form)** — the first W2 skeleton row retired:
  `PicCategory.ObjectReference` + `Usage.ObjectReference` left the skeleton band; `PicInfo.
  ObjectReferenceItem(className?)` (the IndexItem synthesis pattern — PICTURE-less per §13.18.60.4) carries
  the declared class (null = universal → `object?`; typed → the class's emitted C# type, nullable — COBOL
  initial state IS null, §13.18.63); ParseUsage's arm keeps ONLY the introduction gate (0900 below 2002 via
  `usage-object-reference-2002`, silent at 2002+). DataBinder captures the class-name operand from
  `objectReferenceUsage`, rejects PICTURE-with-reference loud (**new COBOLNET0812**, §13.18.60.4), and
  `ResolveIndexItems` gained the GR1 inheritance leg (a group header sheds the reference profile; a
  PICTURE-less leaf below takes it, class flowing with the shared immutable PicInfo).
- **STAGING (explicit, loud):** a TYPED reference (`OBJECT REFERENCE GREETER`) still 0899s naming the spine
  — its field type needs the pass-1 class symbol table (D1); emitting it now would surface a Roslyn CS0246
  on user source (a loud-failure violation). Removed in spine part 2. The `classDefinition` 0899 in
  `CallCollectUnits` (W2) likewise stands until part 2 collects real ClassUnits.
- **Tests flipped to the live contract** (the skeleton facts VERIFIED the interim posture; the feature
  retires them): LoudGuardTests' OBJECT REFERENCE rows → a live-at-2002/0900-at-85 fact;
  DataSkeletonEditionTests' 0899 fact → universal-compiles-at-2002+/0900-at-85 + the 0812 PICTURE-conflict
  fact. CLI-verified: `01 U USAGE OBJECT REFERENCE.` compiles+runs at 2002, 0900 with the W1.5
  edition-naming hint at 85.

Battery: conformance 1454/1454 · unit 101/101 · zero NIST exposure (no CCVS program uses OBJECT
REFERENCE; no .g4 touched — the legacy guard basis is unchanged). NEXT (spine part 2): the ClassUnit
collection + pass-1 class symbol table (CallCollectUnits), the emit-into-a-type parameterization
(CallEmitProgramClass IS the template), method PC-dispatch + BoundMethodReturn (D8), INVOKE binding
(NEW / no-arg instance), oo_hello green (the 2002 manifest's 9 oo_* pairs are the run contracts).

