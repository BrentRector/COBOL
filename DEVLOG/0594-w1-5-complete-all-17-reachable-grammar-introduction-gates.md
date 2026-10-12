## Entry 594 — 2026-07-03 21:02 PDT — W1.5 COMPLETE — all 17 reachable grammar introduction gates now diagnose 0900 with edition naming; the 0860/0861 code collision resolved

**The critic's mechanism caveat held exactly:** every gated site probed below its edition surfaces as a
generic `NoViableAlternative` (the predicate suppresses the alternative during adaptive prediction), so W1.5
is a PARSE-LAYER mapping, not a grammar change: `EditionGateHints` (new, frontend Parsing/) recognizes each
site's (offending-token, rule-stack, adjacent-token) signature — dual-path where the enclosing rule pops
before the report (GOBACK RETURNING, STOP RUN WITH) — and, ONLY when the parser targets an edition below the
construct's introduction, upgrades the message to the same wording family as `ConstructRegistry.Check`:
`[COBOLNET0900] <display> requires COBOL-YYYY (targeting COBOL-XXXX) — <citation> (constructs.json row <id>)`.
`CobolErrorStrategy` consults it at priority 0; the new `COBOLNET0900` descriptor carries the band code
through the existing `[code]`-prefix extraction.

- **All 17 reachable sites verified end-to-end** (probe sweep, this session): DELETE FILE (names COBOL-2023
  at 85 AND at 2014), ALLOCATE, FREE, INVOKE, GOBACK RETURNING, PROCEDURE DIVISION RETURNING, STOP RUN WITH
  status, START KEY WITH LENGTH, BASED, TYPE, USAGE OBJECT REFERENCE, REPOSITORY CLASS, the three
  SPECIAL-NAMES FOR ALPHANUMERIC/NATIONAL sites, CLASS-ID unit (lookahead scan — the failure reports at the
  unit start), CALL BY VALUE. **JSON/XML map to the vendor hint** (new COBOL0313: "not an ISO/IEC 1989
  construct — vendor-dialect extension, deferred"; the 0900 band would be a lie) — below 2014 only; at ≥2014
  the legacy grammar stub still parses (runtime-loud; W3 owns re-gating it to the vendor axis).
- **Unmapped residue (documented in EditionGateHints):** inlineMethodInvocationStatement (2023 — `x(…)` has
  no distinctive token; OO wave), parameterDescription (2002 — unreachable without a UDF prototype),
  SET…TO objectReference (no signature distinct from the 85 SET forms).
- **The 0860/0861 collision found and resolved:** READ PREVIOUS / START FIRST-LAST emitted ad-hoc
  COBOLNET0860/0861 — codes ALSO used by the WRITE END-OF-PAGE diagnostics (a double allocation), and not in
  the P2.3 pinned set — migrated to `ConstructRegistry.Check` (0900, uniform wording); pinned by
  EditionGateDiagnosticTests with explicit no-0860/0861 assertions.
- **Registry truth-fixes:** currency-picture-symbol-2002's DiagnosticCode corrected to the pinned
  COBOLNET0893 (was mislabeled 0900 — it IS in the roadmap traceability band). Five NEW rows (drift-locked):
  repository-class-2002, start-with-length-2002, special-names-for-national-2002, call-by-value-2002
  (active — sources verified compiling at 2002), class-definition-2002 (pending — 0899 at ≥2002 until
  Phase 3). `expectDiagnostic: COBOLNET0900` added to 21 rows — the matrix reject cells now assert the CODE,
  not just rejection.
- **Verification battery (combined with DEVLOG 595's fixes, one commit): build 0W/0E · unit 102/102 ·
  conformance 1353/1353 (+49) · sweep 419 OK / 0 BREAKS · INV-1-STRONG 349/349 byte-exact at 2023-permissive
  · FULL legacy guard-fast ALL GREEN (353 MATCH + expected LEGACY_DIVERGENT; 1204 unit + 536 integration) —
  the frontend change (error strategy) verified against the legacy side.**

