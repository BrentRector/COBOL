## Entry 491 — COBOL.NET REDEFINES/RENAMES (1/n): the classification foundation (ISO §13.18.44/45, the 4-tier model)

Starting the full REDEFINES/RENAMES subsystem per `COBOLNET_REDEFINES_DESIGN.md` (the 4-tier one-canonical-backing
model) + ISO §13.18.44/§13.18.45 — built to the SPEC, not to any test (memory `feedback_spec_scopes_not_tests`,
after an owner correction for scoping a slice to NC101A's references). A planning workflow (5 parallel ISO/legacy-port
probes + synthesis) produced the decision-complete, 6-commit staging: (1) model+classification → (2) Tier A → (3) Tier
B → (4) RENAMES THRU → (5) Tier D reject → (6) Tier C byte codec. A/B/D + RENAMES is the complete typed model and
covers the entire near-term DISPLAY-homogeneous NIST path; Tier C (the confined byte[] codec for genuine mixed-USAGE
puns) is off the critical path (the design's own staging).

This commit = step 1, **behavior-neutral** (the classification is computed but not yet consumed by emit, so the 156
conformance tests stay green):
- New `Binding/RedefinesModel.cs`: `RenamesInfo`, `RedefinesTier` (Alias / StringCanonical / ByteCanonical /
  Rejected), `RedefinesClass` (one canonical backing + N views, tier, class-max width).
- `DataItem`: `RedefinesTargetName`/`RedefinesTarget`, `Renames`, `Class`, `IsCanonical` (defaults true so the whole
  existing corpus is untouched), `ClassOffset`, `Renames66`.
- `DataBinder`: capture the REDEFINES clause target; stop dropping level-66 and bind RENAMES (`BindRenames`, attached
  to the owning record's `Renames66`, not the storage tree — SR2/SR3); a post-build `ResolveRedefines` (resolve
  targets + RENAMES FROM/THRU) + `ClassifyRedefinesClasses` — group every redefiner with the non-redefining anchor it
  ultimately overlays (chasing the chain, SR11/SR7), mark the anchor canonical and the rest views, assign the tier
  (cascade D>C>B>A: a COMP/float leaf → Tier-C-reject interim; same-CLR-type+same-width members → Alias; else
  DISPLAY-homogeneous → StringCanonical), size to class-max width (SR8), and propagate view-suppression to a view's
  subordinates (SR9).
- 10 classification unit tests (`RedefinesClassificationTests`): Tier-A identical-PIC + numeric-over-numeric
  same-digits-different-scale; Tier-B group-view + partial-overlap; Tier-C mixed-usage reject; nested chain (SR11);
  multiple redefinitions (SR7); RENAMES no-THRU/THRU; standalone-stays-classless. Unit 3→13 green; conformance 156
  green (neutral). Reconciliation noted for the Tier-B commit: `CobolNum` already ships `ParseDisplay`/
  `FormatDisplaySigned`, so the design doc's "overpunch deferred" sequencing dependency is already discharged.

