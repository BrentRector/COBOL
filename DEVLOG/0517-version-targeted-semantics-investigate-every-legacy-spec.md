## Entry 517 — 2026-06-09 16:34 PDT — Version-targeted semantics: investigate every legacy≠spec difference + build the ISO version-change reference

Owner directive (critical, cross-cutting): when the legacy differential oracle disagrees with the ISO 2023 spec, do
NOT blanket-pin to 2023 — first DEEP-INVESTIGATE whether the difference is a behavior CHANGE across COBOL editions
(1985 → 2002 → 2014 → 2023). If it changed, implement the version-TARGETED behavior gated by `DialectLevel` /
`--standard` (the legacy is NIST-CCVS85-green, so it generally encodes the CORRECT COBOL-85 behavior — not a bug to
"fix" to 2023). Only pin-to-spec when the difference is a version-INVARIANT legacy bug. Recorded as memory
`feedback_version_targeted_semantics`. The trap it fixes: the differential harness compiles the NIST corpus at
`DialectLevel: 85` against a COBOL-85 golden, so a `SPEC-ONLY` test asserting the 2023 value while compiling at 85 is
wrong for any version-specific behavior — such tests must be dialect-aware.

**Investigation (3-case investigate → adversarial-verify workflow, cross-checking Annex E + actual NIST-85 golden
coverage).** All three legacy-vs-spec differences enshrined this/earlier sessions are **version-INVARIANT legacy bugs
→ pin-to-spec for all dialects** (no gating needed; the SPEC-ONLY tests are correct, comments updated to record the
determination):
- **DISPLAY trailing-trim (§14.9.11.4)** — NOT a version change, HIGH confidence. NC109M (a COBOL-85 program) golden
  PRESERVES trailing spaces (DISP-TEST-GF-13 reconstructed byte-exact); Annex E lists no DISPLAY-content change; GR1/GR6
  require full-content transfer. Legacy trim is edition-invariant non-conformance.
- **Signed-vs-alphanumeric COMPARISON de-sign (§8.8.4.2.5)** — pin-to-spec (85≠2023 UNVERIFIED but leans "no change"):
  COBOL-85 de-signs the MOVE building block (NC114M MOVE-TEST-16 "STRIP MINUS SIGN" golden), and CCVS85 authors REDEFINE
  a signed item as `X(n)` to image-compare its sign (NC116A GRP-09/10) — implying a direct numeric-vs-alphanumeric
  compare de-signs. No '85 golden requires the legacy image-compare. Cannot be PROVEN version-invariant because the
  2023 spec's Annex E covers only 2014→2023 (not 85→2023) — the conservative pin-to-spec matches every known '85 golden.
- **Signed→GROUP de-sign (§14.9.25.4 GR6a / §8.8.4.1)** — pin-to-spec, same MOVE de-sign rule + group-as-alphanumeric.

**Key finding driving the reference doc:** the 2023 spec's **Annex E documents ONLY the 2014→2023 delta** — "Annex E
is silent ⇒ unchanged" is INVALID for 85/2002 changes. So we need a systematic catalogue.

**Built `docs/VERSION_CHANGE_REFERENCE.md`** (a 5-region parallel extraction workflow → 130 catalogue rows): a LIVE
version-gating checklist of every edition-to-edition change the 2023 spec documents — Annex E.2 (38) + E.3 (51, the
2014→2023 substantive changes), Annex F archaic/obsolete (7), FLAG-02/FLAG-14 GR4 incompatibility options (18, the
2002↔2014 / 2014↔2023 flagging model), and inline per-section edition NOTES (17 deduped). Each row carries §, edition
delta, old→new behavior, affects-existing, and the concrete **compiler gating action**
(gate-behavior-by-dialect / new-feature-gate / flag-obsolete / flag-new-reserved-word / none), all `TODO` except the 3
pin-to-spec rows. Status banner states the scope limit (2014→2023 complete; 85→2014 under-documented — confirm vs the
older standards before gating). Added to `DOC_INDEX` §7. This is the foundation for correctly version-gating every
remaining feature instead of discovering each difference reactively.

Doc/test-only (no compiler change); greenfield suites unaffected.

