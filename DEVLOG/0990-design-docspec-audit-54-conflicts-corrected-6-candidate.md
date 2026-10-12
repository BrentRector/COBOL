## Entry 990 — 2026-07-22 20:30 PDT — Design-doc↔spec audit: 54 conflicts corrected + 6 candidate conformance bugs (owner concern)

The owner flagged that supposedly-validated design docs kept surfacing spec-wrong designs (the V5 EXIT SECTION D8
above being the trigger). Ran a systematic design-doc↔spec audit (`wf_480d50f5`, 15 agents, one per behavior-bearing
deep-dive) targeting two high-risk categories: **"port the legacy" decisions** (the legacy is a regression net, not
spec authority — `feedback_use_the_spec`) and **unvalidated paper designs** (nothing forced them against the spec).

**Result: 54 doc↔spec conflicts across 13 of 15 docs** (2 clean: VALIDATION, runtime-library). The doc side was
corrected by a parallel correction pass (`wf_16d53d4e`, one agent per doc) that REWROTE each doc spec-faithful —
owner directive: rewrite to be correct, NO addenda / no was-X-now-Y (that history lives here in DEVLOG only). ~25 were
wrong-§ mis-citations (SEARCH §14.9.38→.37, LENGTH §15.24→.50, STOP §14.9.43→.42, JUSTIFIED §13.18.36→.32, OCCURS
§13.18.40→.38, the whole §8.8.4 condition family); the rest stale-vs-correct-code descriptions (short-circuit AND/OR
is the §8.8.4.13-mandated order, NOT a "divergence"; F3-PERFORM GR14 handler scoping; GOBACK/EXIT-PROGRAM RAISING
propagation per §14.9.18.4 GR1b) and incomplete-to-spec gaps. Verified the correction pass (spot-checked the
CONDITIONS_EXCEPTIONS rewrite = excellent; scanned all diffs — no addenda, no surviving legacy-port rationale).

**6 findings are spec-wrong AND IMPLEMENTED = candidate conformance bugs** (the docs now describe the spec-correct
target; the code gap is filed §24 **V54–V59**, verify-then-fix): V54 MAX/MIN result category (National→Alphanumeric,
§15.59.1) · V55 EC-OO-UNIVERSAL raised unconditionally (§14.9.23.4 GR7c) · V56 float relation compared native-double
not SDIDI under STANDARD-DECIMAL (§8.8.4.2.4) · V57 F3-PERFORM handlers bind under base TurnState not TURN OFF ALL
(§14.9.28.4 GR14) · V58 GOBACK/EXIT-PROGRAM RAISING a fatal into an EC-free activator terminates instead of not-raising
(§14.9.18.4 GR1b/GR3) · V59 REDEFINES Tier-B stores BINARY/PACKED as a zoned char image (§13.18.60 GR4/GR11).

**Root cause (confirmed across docs):** designs anchored on the legacy / existing-code shape instead of deriving the
end-state from the spec (`feedback_design_structural_singular_first`). "Validated" held for implemented+tested features
but was overstated for paper designs and legacy-port decisions. SSOT: `docs/rearchitecture/DESIGN-SPEC-RECONCILIATION.md`.
Follow-up: a broader code-vs-spec audit (the design-doc audit only caught bugs where a doc happened to describe the
wrong thing) — launched separately.

