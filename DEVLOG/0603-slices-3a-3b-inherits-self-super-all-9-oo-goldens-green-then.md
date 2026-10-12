## Entry 603 — 2026-07-04 17:00 PDT — Slices 3a+3b (INHERITS + SELF/SUPER): ALL 9 oo_* GOLDENS GREEN — then a 45-agent adversarial workflow found 22 real conformance bugs in the OO wave; all fixed same-session

**The owner directed maximum workflows/parallelism. The wave: (1) slices 3a+3b landed serially (INHERITS
`: BASE`, override marking, cycle 0820, §9.3.8.2 signature 0829; SELF → `this.M(…)` virtual GR2 / SUPER →
`base.M(…)` non-virtual GR3, 0827 placement band incl. trap #7; the full slice-2 marshaling shared via
`OoBindResolvedInvoke`) — all four remaining goldens ran byte-exact on the FIRST build, 9/9 enabled; then
(2) a Workflow fanned out 45 agents (4 review lenses × the whole OO wave → 29 findings → 24 adversarially
verified → 22 CONFIRMED / 2 refuted; 12/12 behavior probes on the prebuilt CLI — the 3a/3b surface itself
was solid; 5 implementation briefs for the remaining slices → `docs/COBOLNET_OO_SLICE_BRIEFS.md`, indexed);
and (3) every confirmed finding was fixed in-session.** The review's headline: slice 2's "strict §14.8.2
conformance everywhere" MISREAD the spec — identical-description is §14.8.2.3.2, BY REFERENCE ONLY.

**The 22 confirmed findings, deduplicated into the landed fixes (all with probes/tests):**
- **Effective-mode conformance dispatch (the big one):** BY REFERENCE keeps strict identity — now ALSO
  comparing SIGN representation (`ImageSignKind` — a LEADING SEPARATE vs overpunch pair silently corrupted
  images before), BLANK WHEN ZERO, JUSTIFIED, and honoring §14.8.2.2 rule 1 (a BY REFERENCE GROUP formal may
  be SMALLER than the argument — prefix crossing, tail-preserving splice-back). BY CONTENT (explicit +
  SR-10 auto + literals) now follows **§14.8.2.3.3**: COMPUTE rules for numeric formals (ANY numeric
  argument/literal, rescaled+truncated through the OWNER's now-`internal` profile, qualified
  `{OWNER}._P_n` — the cross-class-profile rule), SET rules for object references, MOVE rules otherwise;
  §9.3.6 rule 5 makes truncating literals LEGAL (the hard fit-errors removed for CONTENT).
- **RETURNING = SET rules (§14.8.3.3 rule 1):** subclass results deliver into SUPERCLASS-typed and
  UNIVERSAL receivers (`ObjectRefWideningMismatch` — shared with BY CONTENT obj-ref args and the new
  COVARIANT override RETURNING per §9.3.8.2.3 5a/5c2, emitted as C# 9+ covariant returns). The NEW path
  already had the right direction — the two RETURNING paths now share ONE rule.
- **Silent-misbind fixes:** method GROUP formals now register whole-group-referenced (the spaces→zeros
  image-round-trip corruption); `OoHarmonizeOverrideCrossings` unions the crossing form across override
  chains post-MarkStoreAsImage (a one-sided StoreAsImage flip emitted `M(ref string)` vs
  `override M(ref long)` — raw CS0115 on legal COBOL); INVOKE emission order corrected per GR8 (call →
  BY REFERENCE copy-outs → identifier-4 store LAST — receiver/argument overlap); float args read the float
  value directly (silent truncate-to-long killed); reference-modified args conform by their EFFECTIVE
  §8.4.2.4 description (elementary alnum of the window), with RefModPlace read/splice special-cases.
- **Scope-bypass fixes (§8.4.6.2.1 rule 3a):** `DataBinder.LookupData` + `TryGetVisibleIndexField` route
  SEARCH/SEARCH ALL/SORT-table/INITIALIZE and every index-name-first lookup (ReferenceResolver subscripts,
  IndexFieldOf, intrinsics) through the method scope — a method-local name now shadows the object level in
  EVERY path; the dead level-66 gate walks `Renames66` (was Children-only — never fired).
- **Crash/emission hardening:** `DescriptionMismatch` Pic-null guards (NRE on PIC-less items → loud
  diagnostic); METHOD-ID named like its CLASS-ID renames the SYMBOL (`_M` — §8.3.2.2 implementor mapping;
  overrides adopt the base slot's CsName; the derived-class-named-like-inherited-slot corner is a 0820
  restriction, never CS0542); N"…"/X"…" method-name literals decode (SR2), zero-length literal-1 diagnosed
  per SR2, three wrong SR citations fixed; the CobolObject.__CobolInvoke doc no longer claims emitted
  overrides that don't exist yet.
- **Refuted (2, correctly):** the GR6 SR9-membership hypothesis and a ConditionOf qualified-88 shadow claim.
- **Note on the workflow mechanics:** 3.18M subagent tokens, 858 tool uses, ~23 min wall; the probe agents
  ran the PREBUILT CLI only (the shared-tree no-build rule held); the verify phase reproduced findings with
  runnable programs before confirming — two lenses' four duplicate findings converged on the same two
  spec rules, which is the redundancy working as designed.

Deep-dive: the D6 AS-BUILT note is CORRECTED in place (the mode-dispatch design + all five realizations);
`docs/COBOLNET_OO_SLICE_BRIEFS.md` (new, LEDGER, DOC_INDEX row) carries the regenerated FACTORY /
OVERRIDE-FINAL / universal-dispatch / EC-OO / INTERFACE+PROPERTY briefs. OoSpineTests grew to 22 (widening
RETURNING run fact, covariant + SIGN facts, CONTENT COMPUTE-rule conversions, SEARCH/index shadowing, the
66 gate). **Battery: conformance 1512/1512 · unit 101/101 · INV-1-STRONG 349/349 byte-exact at
2023-permissive · sweep 439 OK / 20 SKIP85 / 0 BREAKS** · zero grammar/legacy exposure across the whole
wave (no .g4, no `src/CobolSharp.Compiler` change).

**NEXT: FACTORY** (per the brief: grammar factoryParagraph — the OO grammar grant is standing — static
members/methods, INVOKE Class "M", SELF-in-factory) → OVERRIDE/FINAL attrs → PROPERTY → INTERFACE-ID →
universal reference → EC-OO; then the Phase-4 catalog toward 100%.

