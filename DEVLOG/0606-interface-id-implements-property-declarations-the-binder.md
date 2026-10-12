## Entry 606 — 2026-07-04 18:56 PDT — INTERFACE-ID + IMPLEMENTS + PROPERTY declarations: the binder-authoritative 0841 conformance pass, covariant adapters, pinned accessors (VCR M2 OO)

**What landed (the INTERFACE/PROPERTY wave — declarations complete; references staged).**

- **INTERFACE-ID (§11.5/§11.6) end-to-end.** Grammar: `interfaceDefinition` ({is2002()}? in
  compilationGroup), `implementsClause` in FACTORY/OBJECT paragraphs, `methodPropertySelector`,
  `propertyClause`, REPOSITORY INTERFACE/PROPERTY specifiers. Words: GET/PROPERTY/INTERFACE §8.9-reserved
  2002+ (XOR recipe — user words at 85, 0901 ≥2002; proven by the word-continuity theory tests);
  IMPLEMENTS is §8.10 CONTEXT-SENSITIVE (spec :10853) — a user word at EVERY edition (85 AND 2023 tested),
  never CheckedTokenTypes.
- **Pass-1 interfaces + prototypes.** OoInterfaceSymbol built FIRST (0840 structural family: the ONE
  class/interface namespace §8.3.2.2, END INTERFACE §10.7, SR2/SR8 no prototype attributes, §10.6.2 SR4
  header-only + LINKAGE-only); prototype LINKAGE binds via the SAME OoBindMethodData machinery
  (OoBindInterfaceData), so the conformance pass compares RESOLVED descriptions.
- **The 0841 conformance pass is BINDER-authoritative (ValidateImplements, §9.3.11 via §9.3.8.2.3 over the
  §11.8.4 GR2 closure — direct + interface-INHERITed + class-INHERITed, instance and factory sides).**
  Roslyn is provably insufficient BOTH directions: PIC 9(4) and 9(8) formals both project `ref long`
  (under-reject — caught only by DescriptionMismatch, the headline test), and C# FORBIDS the covariant
  interface-implementation returns §9.3.8.2.3 rules 5a/5c2 PERMIT (over-reject — cured by AdapterPairs →
  explicit interface implementations `PROTO_RET IFACE.M(…) => this.M(…);`, proven by the
  oo_interface_covariant golden compiling AND running WOOF through an interface-typed factory prototype).
- **Interface-typed receivers.** 0813 accepts interfaces (§13.18.60.2); INVOKE through one resolves over
  AllPrototypes() (§14.9.23.3 SR4e; 0825 on miss) → static C# interface dispatch behind the same GR5 null
  guard; ObjectRefWideningMismatch gained the interface branch (implements-closure; interface-INHERITS)
  and ALL NEW/RETURNING/argument paths route through it (the NEW-into-interface-receiver fix).
- **PROPERTY (§13.18.42) declarations.** The clause synthesizes accessors per GR1/GR2 under the PINNED
  §11.7.4 GR1a names `__GET_<P>`/`__SET_<P>`; the "identical-description clone" is the SUBJECT DataItem
  itself, and the emitter renders DIRECT field bodies — observably identical to the implicit-MOVE methods.
  Explicit `METHOD-ID. GET|SET PROPERTY p` joins the roster under the SAME pinned names, so
  override/0829/implements machinery applies to accessors unchanged. The 0842 band: SR6/SR7 shapes, SR5
  clause+explicit dup, §13.18.42.3 SR4 superclass collision, no-OCCURS/no-FILLER subjects.
- **The VALUE-clause grammar collision (a real §8.9 consequence).** `01 X PIC 9(5) VALUE 100 PROPERTY.`
  parsed PROPERTY as a CONSTANT-NAME operand (valueClauseOperand → cobolWord — the 85-user-word admission
  cuts both ways) and 0901'd. Cure: loop guards at BOTH consumption points (the valueItem list AND the
  multi-operand `valueClauseOperand+` item — the first guard alone was insufficient, the inner `+` was the
  actual consumer): at 2002+ PROPERTY terminates a VALUE clause (reserved ⇒ never a constant-name). C#
  target note: the predicate is `TokenStream.LA(1)`, not Java's `_input`.
- **STAGED loud (named 0899, never a generic guard):** property REFERENCES (`P OF obj` — the §8.4.3.9.4
  GR1–GR3 implicit-INVOKE desugar) detected at the ReferenceResolver resolution-failure CHOKEPOINT (the
  one funnel all operand binds share — the earlier StatementBinder helper was deleted for it,
  feedback_singular_pattern) by the single-qualifier + roster-property shape; and GET/SET PROPERTY
  prototypes in interfaces.
- **Registry/matrix:** 6 ConstructDialectStatus rows (interface-definition / repository-interface /
  repository-property / implements-clause / property-clause / method-property-selector, all -2002);
  4 constructs.json ACTIVE rows (the independently-reachable gates) + W1.5 EditionGateHints signatures
  (INTERFACE-ID unit incl. the IDENTIFICATION-adjacent scan now shared with CLASS-ID; both repository
  specifiers; the property clause) — all four probed 0900 at 85 and clean at 2002.
- **Goldens (4 new, greenfield-only):** oo_interface (MEOW/WOOF — two classes implementing one interface,
  polymorphic dispatch through an interface-typed reference), oo_interface_covariant (WOOF — the adapter),
  oo_property (00100/00142 — the clause, VALUE-then-PROPERTY order), oo_property_methods (explicit
  accessors). 16 oo_* goldens enabled.
- **Tests:** +11 OoSpineTests (the 9(4)-vs-9(8) 0841 headline, missing-method 0841, 0840 family ×2, 0842
  ×2, the named-0899 stage, word continuity ×3 + IMPLEMENTS-at-all-editions ×2, the non-implementing
  NEW-into-interface negative 0826). OoSpine+Corpus+Matrix = 427/427.

**AI friction (honest).** (1) The first VALUE-loop guard targeted the OUTER list; the real consumer was the
inner `valueClauseOperand+` — one wasted regen cycle; lesson: find the actual greedy loop before gating.
(2) `_input` is a Java-ism; the C# ANTLR target wants `TokenStream` in predicates — one build break.
(3) The new negative test initially expected 0828 where the compiler correctly emits 0826 (the NEW-band
owns NEW-shaped INVOKEs) — the test was wrong, not the compiler.

**Deep-dive updated in the same change set** (feedback_follow_design_docs_and_spec): the STATUS banner
(605's stale NEXT retired too), the INTERFACE/PROPERTY AS-BUILT section, the gate-inventory line, TWO
open-questions bullets flipped (grammar scope mostly retired; the interface-conformance question RESOLVED:
explicit binder pass required). The brief in COBOLNET_OO_SLICE_BRIEFS.md carries its LANDED banner + the
deltas (codes shifted to 0840–0843; factory IMPLEMENTS now emits per D11).

**Battery:** greenfield conformance **1552/1552** (from 1519 — the wave added 33: 11 OoSpine facts incl. theories, 4 goldens, the matrix rows and their below-edition legs) · unit **101/101** (the registry drift test forced the two transitively-gated rows into constructs.json as status:pending — catalogued + edition-frozen, not probed: no independent below-2002 signature exists) · continuity sweep **439 OK / 20 SKIP85 / 0 BREAKS** (baseline-exact) · FULL legacy guard **353 MATCH / 0 mismatch — ALL GREEN** (grammar changed ⇒ full run). Synthesized-accessor bodies are Roslyn-verified via the oo_property golden; their RUNTIME exercise arrives with the property-reference increment (nothing can call `__GET_/__SET_` from COBOL until `P OF obj` binds).


