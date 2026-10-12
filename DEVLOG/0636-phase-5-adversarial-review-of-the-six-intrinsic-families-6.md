## Entry 636 — 2026-07-06 21:10 PDT — Phase 5: adversarial review of the six intrinsic families — 6 confirmed defects FIXED

After landing all six Phase-5 intrinsic families (630–635) I ran an adversarial find→verify review workflow
(wf_18f20f2b-d5e — one reviewer per family cross-checking the code against the spec, then per-finding adversarial
verification). 7 findings raised, **6 confirmed real and FIXED** (1 refuted). Every one is a genuine spec
divergence the golden/differential tests had missed — exactly the value of the adversarial pass.

- **FIND-STRING overlapping occurrences (§15.37.4 r1).** The scan advanced by argument-2's length (non-
  overlapping, an INSPECT §14.4.6 reading the code comment itself flagged as an unjustified import). Rule 1
  defines an occurrence as ANY position where argument-2 matches a substring — overlapping included. Fixed to
  advance by 1. `FIND-STRING("aaaa" "aa" LAST)` now = 3 (was 1); `… START AFTER 1` = 2 (was 0). +3 tests.
- **SUBSTITUTE ANYCASE fold (§15.87.4 r5).** Used `OrdinalIgnoreCase` (invariant UPPER-fold) instead of the
  LOWER-CASE fold rule 5 mandates (and FIND-STRING already uses). Diverges for asymmetric-case chars (µ U+00B5 vs
  μ U+03BC upper-fold equal but lower-fold distinct). Fixed to `ToLowerInvariant` on precomputed lowered images
  (length-preserving, positions align).
- **CONVERT ANY over a national item (§15.19.3 SR7) — high.** ANY reduced via Latin-1 (1 byte/char with
  substitution) regardless of usage, corrupting a national operand's bits. SR7 takes the operand's RAW storage;
  resolved at bind (`BindConvert`) — a national ANY operand now uses the NAT (UTF-16BE) reduction. `CONVERT(N"A"
  ANY ANUM HEX)` = "0041" (was "41").
- **CONVERT malformed HEX source (§15.19.3 SR4).** A bad HEX input set the nonfatal EC-DATA-CONVERSION, but a
  violated ARGUMENT rule is the FATAL EC-ARGUMENT-FUNCTION (the nonfatal EC is only for an untranslatable RESULT
  value, r1/r3). Switched `HexDigitsToBytes` to `ArgumentError`.
- **MODULE-NAME STACK from a nested program (§15.65.4 r9).** Walked every CALL frame, so a contained program
  emitted its outermost id twice (`MAIN;MAIN`). STACK entries are runtime elements (compilation units); fixed to
  emit by outermost id and collapse consecutive same-unit frames. +1 test (nested STACK = `OUTERM;`).
- **NUMVAL-F exponent sign (§15.69.3).** Accepted `1E2` (no sign after E) and returned 100, while TEST-NUMVAL-F
  correctly rejects it — an internal inconsistency. §15.69.3 makes `{+|-}` mandatory; NUMVAL-F now rejects a
  signless exponent (→ the §15.3 default). +2 test rows.

Battery: **1946 conformance (+5 new regression tests) · 216 unit · 117 corpus goldens GREEN**; greenfield-only.
The refuted 7th finding (verified not-a-bug) is left as-is. Phase-5 intrinsics remain COMPLETE and now
adversarially hardened.

