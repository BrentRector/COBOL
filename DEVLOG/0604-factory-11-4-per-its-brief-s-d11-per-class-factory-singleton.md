## Entry 604 — 2026-07-04 17:24 PDT — FACTORY (§11.4) per its brief's D11: per-class factory SINGLETON classes (statics SUPERSEDED) — the first .g4 change of the OO drive; 10/10 oo corpus

**The FACTORY slice landed from `docs/COBOLNET_OO_SLICE_BRIEFS.md` (the workflow-regenerated brief) — and
its D11 is a design CORRECTION folded into the deep-dive in the same change set: a factory is a REAL
sibling singleton class (`FOO__FACTORY : BASE__FACTORY | CobolObject` + `public static readonly __Instance`
+ a covariant `public virtual/override FOO __New()`), NOT the static members the original sketch imagined.
Three spec facts kill statics:** (1) §8.6.4 :8765 — EVERY class owns its OWN copy of factory data it merely
INHERITS (DOG-without-a-factory-paragraph included; statics would share ONE — the factory-level trap-#1);
(2) SELF in a factory method dispatches on the RUNTIME factory (§14.9.23.3 SR4f + §8.4.3.8 GR2 — statics
cannot dispatch); (3) §9.3.6 factory resolution walks INHERITS — the factory C# hierarchy mirrors the class
hierarchy and Roslyn re-checks the binder's facts (D1).

- **Grammar (the FIRST .g4 change of the whole OO drive — the XOR-recipe applied):** `FACTORY` lexer token
  + `_dataNameTokens` + `cobolWord` alternative + `CheckedTokenTypes` (position-blind safe: its keyword
  slots are factoryParagraph/END FACTORY/FACTORY OF only); `factoryParagraph` in classDefinition (mirrors
  objectParagraph — the SAME methodDefinition rule, so the whole method machinery reused verbatim);
  `objectReferenceUsage` gained the FIRST alternative `OBJECT REFERENCE FACTORY OF className`
  (binder-staged 0899 — the universal-reference wave; the two-explicit-alternatives discipline held).
  Edition continuity proven BOTH ways by test: `01 FACTORY PIC 9(2)` compiles at 85, 0901 at 2002+.
- **Pass-1:** a SEPARATE factory roster on OoClassSymbol (dual dispatch: an instance method and a factory
  method may share a name, §9.3.6 — run-proven FACTORY-PING/INSTANCE-PING); factory METHOD-ID "NEW" →
  **new 0836** (the predefined New IS the generated ctor, D4 — overriding it is the documented v1
  restriction); factory overrides mark against the base FACTORY roster only, ride the same 0829 signature
  validation and the same crossing-form harmonization.
- **Bind:** `InvokeForm.Factory` + `InvokeForm.NewSelf`; `OoInFactory` selects the SELF/SUPER roster
  (SR4f–i); SELF|SUPER "NEW" in a factory method binds ACTIVE-CLASS creation (§16.2.1 GR1 — SUPER "NEW"
  deliberately identical: the restricted search finds the same predefined New); `INVOKE Class "M"`
  resolves the factory chain (miss → 0825 worded per SR3; an instance-receiver miss now HINTS when the
  name IS a factory method). The factory half binds through its OWN DataBinder forest + uid band (D13 —
  factory/instance name separation is structural, like method scoping); SR 10 works FREE (factory WS
  roots are unscoped → OoIsObjectData → the landed 0828/auto-CONTENT logic).
- **Emit:** `OoEmitTypeHalf` — the emit-into-a-type parameterization is now literally ONE routine called
  twice per CLASS-ID (instance class, factory class + header extras); CONTENT-conversion profile
  qualification picks `{OWNER}__FACTORY._P_n` for factory formals; `MarkStoreAsImage`/`_ecActive` cover
  the factory halves.
- **Proof (oo_factory, 10/10 oo corpus enabled):** FC=02/FC=01 = the §8.6.4 per-class inherited-data
  copies (new adversarial trap #11); WOOF = INVOKE FDOG "MAKE" running the INHERITED factory method whose
  SELF "NEW" created a FDOG (covariant __New) with the SPEAK override dispatching. OoSpineTests ×25
  (+ dual roster, 0836/0825-SR3/0827-factory-flavor/0828-SR10 band, FACTORY-word editions).
- **AI friction, honest:** two doc-patch attempts silently no-oped or crashed on encoding — the deep-dive
  is CRLF on disk and bash heredocs mangle escapes/em-dashes; the standing rule is now file-based patch
  scripts with LF-normalized anchors + CRLF-restoring writes AND an assert on EVERY replacement (a
  silent no-op left the DEVLOG-603 deep-dive banner stale until caught this entry).

Battery: conformance **1515/1515** · unit 101/101 · INV-1-STRONG 349/349 byte-exact at 2023-permissive ·
sweep 439 OK / 20 SKIP85 / 0 BREAKS · **FULL legacy guard: NIST 353 MATCH / 0 regressions · legacy unit
1204 · legacy integration 537** (oo_factory joined the runner's new `GreenfieldOnly` exclusion — FACTORY
never landed in the frozen legacy oracle, the deep-dive's "Never landed" list; greenfield CorpusRunnerTests
byte-compares it instead). NEXT: the OVERRIDE/FINAL attribute wave (strict §11.7 SR4a) → PROPERTY →
INTERFACE-ID → universal reference → EC-OO.

