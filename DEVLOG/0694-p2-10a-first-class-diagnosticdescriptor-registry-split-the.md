## Entry 694 — 2026-07-08 12:08 PDT — P2.10a — first-class DiagnosticDescriptor registry; split the COBOLNET0899 catch-all; disambiguate the reused COBOLNET1533

**PHASE 02 step 10 (part a — the registry + the split).** Fixes P8: the compiler emitted diagnostics as bare
`COBOLNETnnnn` string literals, with the `COBOLNET0899` "recognized but not implemented" catch-all spread
across ~44 sites and `COBOLNET1533` reused for three different strong-type rules — nothing documentable,
suppressible, or drift-bound.

**New (in `Cobol.Net.Editions`, below both frontend + compiler):**
- `Diagnostics/DiagnosticDescriptor.cs` — a record `(Code, Id, Severity, Title, IsoSection, SuppressKey?)`.
  `Id` is the STABLE kebab-case identity (survives code renumbering); `Code` is the emitted, possibly-shared
  number; `ResolvedSuppressKey = SuppressKey ?? Code`. Severity reuses `EditionSeverity` (no third parallel
  severity type — the merge with the frontend's 3-value `DiagnosticSeverity` is the P7 diagnostic unification).
- `Diagnostics/DiagnosticCatalog.cs` — the hand-written catalogue: the edition band (single-sourced from
  `EditionCodes`), the digit-capacity codes, the 3 `COBOLNET1533` strong-type rules split by ISO §, and the
  ~40 descriptors the `COBOLNET0899` catch-all splits into. `All` reflects the public fields (no
  hand-maintained list to forget).
- `EditionContext.Error(DiagnosticDescriptor, string)` — the descriptor-keyed emit overload; emits the
  descriptor's `Code` with the site-composed message, **byte-identical** to the former bare-code call.

**The 0899 split.** Every one of the 44 sites now references a catalogue descriptor with a distinct `Id`; the
emitted CODE stays `COBOLNET0899` (byte-stable — goldens + the differential net pin the text). The split
separates the two categories the code conflated: LEGAL-but-deferred features (national data, ~12 Report Writer
clauses, 7 OO refinements, EXTERNAL/debug residue) carry the shared `recognized-not-implemented` suppress
family so a developer can mute all "not implemented yet" diagnostics WITHOUT muting the ~15 genuine SEMANTIC
VALIDATION errors that also live on 0899 (unresolved Report Writer CONTROL/SOURCE/SUM operands, a
receiving-side LINE-COUNTER, …). Reclassifying those validation errors onto proper per-rule codes (out of the
0899 bucket) changes emitted codes, so it re-baselines goldens under review — tracked as a P3 follow-on, out
of P2's byte-stable scope.

**Reused-code disambiguation.** `COBOLNET1533` → `strong-move-mismatch` (§14.9.25.3 SR2) /
`strong-class-condition` (§8.8.4.4.3 SR1) / `strong-compare-mismatch` (§8.8.4.2.3 SR1) — three descriptors,
one shared code.

**Edition band unified.** The frontend's `COBOLNET0900` descriptor now single-sources its code from
`EditionCodes.Introduction` (one string, one home) instead of re-literalizing it.

**Scope.** P2 catalogues the edition band + the 0899 split + the 1533 reuse (the P8-named problems). The
broader "every one of the ~163 compiler codes → a descriptor" migration + folding the frontend's parse-layer
descriptors and its 3-value `DiagnosticSeverity` into this home is the P7 follow-on. Part b (P2.10b, next) adds
`docs/DIAGNOSTICS.md` generation + `DiagnosticRegistryDriftTests`.

Byte-stable: 15 compiler files touched (44 0899 + 3 1533 sites re-pointed; the emitted text unchanged).
**Battery:** greenfield conformance 2055 · unit 224 · characterization 32 GREEN; FULL legacy guard NIST
**353 MATCH** (0 regressions), legacy unit 1196, integration 607 GREEN.

