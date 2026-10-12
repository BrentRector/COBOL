## Entry 527 — 2026-06-10 01:12 PDT — Sections as procedure targets + qualified procedure-names + TIMES-once → FIVE new NC greens (27 total)

**The control-flow wave (diagnosis Wave 1).** The 6-agent diagnosis traced every "GO TO unknown paragraph" to ONE
root cause: `StatementBinder.CollectParagraphs` flattened a section's paragraphs into the pc sequence but DROPPED
the section's own name — so `GO TO section-name` (NC138A/140A/245A, NC102A ×11 sites) and every qualified
procedure-name (NC208A — `GetText()` of the whole context concatenates `PAR-1A OF SEC-1` into an unmatchable
`PAR-1AOFQUAL-SECTION-1`) failed loud.

**The rework (binder-only; the dispatcher needed NO change — `__Dispatch(start,end)` + `__pc = target` already
support ranges):**
- `SectionInfo` — a section IS the inclusive pc range [StartPc, EndPc] of its contiguous flattened paragraphs
  (ISO §14.4.3), plus its OWN paragraph map; `_paraSection` records each pc's owning section and `_currentSection`
  is threaded through binding.
- `ResolveProcedure(ctx)` — THE procedure-name resolution (ISO §8.4.2.2): decomposes head/qualifier from the
  context CHILDREN (never whole-context GetText); explicit `OF/IN section` → that section's map; unqualified →
  same-section paragraph first (rule 6 — implicit qualification of duplicated names), then the global
  first-defined paragraph, then a section name. GO TO → range.Start (§14.9.17 GR1: a section target = its first
  paragraph); PERFORM → [first.Start, (THRU ? thru.End : first.End)] (§14.9.28: a section = first statement of
  its first paragraph through last of its last). PERFORM of an EMPTY section = no-op; an INVERTED THRU range
  (the THRU procedure physically precedes the first — NC102A PFM-TEST-F1-10's deliberate torture) is legal and
  flows through the dispatcher's exit-completion return unchanged.
- **Two more real bugs NC102A then exposed:** (1) my first cut no-op'd `start > end` for THRU too — wrong, that's
  the legal inverted range (only a genuinely empty SECTION no-ops); (2) **PERFORM … TIMES re-read its count every
  iteration** (`for (_i=0; _i < COUNT-FIELD; …)`) — ISO §14.9.28 GR7 determines the count ONCE at statement start,
  and NC102A's PFM-TEST-F2-6 increments the count item inside the body (got 907, wanted 707; the CCVS run-away
  guard then aborted the group). Now `long __nK = count;` then loop on `__iK` (also fixes the latent nested-`_i`
  C# name-collision).
- **Spec-vs-legacy divergence found by the new differential net:** an unqualified DUPLICATED paragraph-name
  referenced from a section that contains one resolves to THAT section's paragraph (ISO §8.4.2.2.1 rule 6 +
  .3 SR7); the legacy resolves it global-first — a version-invariant legacy non-conformance (the rule is unchanged
  since '85) → the test case pins to the SPEC, not the oracle (`UnqualifiedDuplicate_ResolvesWithinOwnSection`).

**Result: NC102A, NC138A, NC139A, NC140A, NC245A byte-match → 27 NC programs locked in.** New
`SectionDifferentialTests` (7 facts: GO TO section, PERFORM section, inverted THRU, OF/IN qualification, the
spec-pinned same-section rule, TIMES-once, TIMES zero/negative). **334 conformance + 15 unit green.**
Logged for the version matrix: the §14.4.3 unnamed-first-paragraph / sentences-directly-under-a-section grammar
shape is still unsupported (no NIST program needs it; needs a shared-front-end grammar change + legacy guard).

