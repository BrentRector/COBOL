## Entry 605 — 2026-07-04 17:45 PDT — OVERRIDE / IS FINAL attributes (§11.7 SR2–SR8, §11.3): STRICT SR4a lands — the name-match leniency retired; sealed emission incl. the CS0549 factory trap

**The OVERRIDE/FINAL wave landed from its brief (docs/COBOLNET_OO_SLICE_BRIEFS.md): the grammar now carries
the ONLY ISO method attributes — `METHOD-ID … [OVERRIDE] [IS FINAL]` and `CLASS-ID … [IS FINAL]` (spec
order, §10.6 :12742-12744/:12798-12821) — and §11.7 is enforced STRICTLY at pass-1.** The brief's
0836/0837/0838 code plan shifted one slot (FACTORY took 0836 first): SR4a=0837, SR3=0838, FINAL=0839.

- **Grammar:** the OVERRIDE token via the XOR-recipe (lexer + _dataNameTokens + cobolWord +
  CheckedTokenTypes — its only keyword slot is the METHOD-ID attribute position, a direct token);
  FINAL needed nothing (a token since the Report Writer, '85-reserved continuously). Edition continuity
  proven both ways: `01 OVERRIDE PIC 9(2)` compiles at 85, 0901 at 2002+. No new registry/VCR rows
  (2002-introduced, never removed — the OO-introduction rationale).
- **SR4a (the headline):** redefining an inherited method WITHOUT OVERRIDE is **0837** through the ONE
  `EditionContext.Removed` policy seam — error strict, warning + the pre-wave name-match inference under
  `--permissive` (the documented migration leniency; the seam's doc widened to cover dialect-leniency
  gating — never a parallel Lenient() mechanism). The OoClassTable marking loop is REWRITTEN (one
  MarkRoster local over BOTH rosters): explicit OVERRIDE marks (0839 when the base method is FINAL, GR3);
  OVERRIDE with no base method is **0838** (SR3, incl. the no-INHERITS case); INHERITS FROM a FINAL class
  is 0839 (§11.3 GR3). 0829 signature conformance unchanged (still hard both axes).
- **Emission — the TOTAL D7 modifier table:** override → `override` (`sealed override` when itself FINAL
  in a non-sealed class); FINAL root method or ANY fresh slot in a FINAL class → NON-virtual; FINAL class
  → `sealed` on BOTH type halves. **The CS0549 trap fired exactly where the brief predicted, on the first
  golden compile** — a FINAL class's sealed FACTORY class held the `virtual __New` root → the root `__New`
  in a sealed factory now emits non-virtual. That's the loud-failure invariant working: the golden's
  KEEPER-IS-FINAL case exists precisely to keep this Roslyn-error-on-emitted-code class of bug dead.
- **Corpus conformance (D-F):** oo_inherit / oo_super / oo_self_polymorphic / oo_factory gained the
  SR4a-required OVERRIDE on their redefining METHOD-IDs (fixing INVALID source to conform to ISO —
  goldens byte-identical); they joined the legacy runner's GreenfieldOnly list (the frozen legacy grammar
  cannot parse the attribute). New golden **oo_override_final** (12/12 oo corpus): WOOF through an
  ANIMAL-typed ref proves `sealed override` dispatch; HELLO-KEEPER proves a sealed class + non-virtual
  method compiles and runs. OoSpineTests ×28 (+0837 strict/permissive-inference, 0838, 0839 both sites,
  OVERRIDE-word editions both ways).
- Deep-dive updated same change set: D7 AS-BUILT (attributes live, strict SR4a + the permissive
  inference), the grammar-seam Missing list (OVERRIDE/FINAL and FACTORY struck), the SR2/SR8
  method-prototype forward obligation recorded for the INTERFACE-ID wave.

Battery: conformance **1519/1519** · unit 101/101 · INV-1-STRONG 349/349 byte-exact at 2023-permissive ·
sweep 439 OK / 20 SKIP85 / 0 BREAKS · **FULL legacy guard ALL GREEN: NIST 353 MATCH / 0 regressions ·
legacy unit 1204 · legacy integration 538** (the three attributed oo_* sources + the two attribute-era
goldens ride GreenfieldOnly). NEXT: PROPERTY (§13.18.42) → INTERFACE-ID → universal reference → EC-OO
per the briefs.

