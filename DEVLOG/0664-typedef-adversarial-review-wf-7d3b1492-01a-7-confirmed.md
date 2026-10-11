## Entry 664 — 2026-07-07 14:38 PDT — TYPEDEF adversarial review (wf_7d3b1492-01a): 7 confirmed defects, ALL FIXED; + JSON/XML is non-ISO

The mandated adversarial find→verify review over TYPEDEF (data-model **D17**) increments 1–4 (5 independent lenses →
verify each candidate on the live CLI; wf_7d3b1492-01a, 13 agents, 1.03M tok). **8 candidates → 7 CONFIRMED, 1
REFUTED.** Every prior feature's review found real defects; this one found seven — all fixed this change set, each
reproduced BEFORE and re-verified AFTER on the CLI. (Two lenses independently raised the same SR2 gap → 6 distinct
fixes.)

**HIGH #A — strong TYPE nested in a strong TYPEDEF was wrongly rejected (false COBOLNET1532).** In `ExpandType`,
`item.StrongType`/`TypeName` were assigned AFTER the template's children were cloned; a nested `TYPE` ref expanded
inside `CloneItem` walks its ancestor chain for the SR6 strong-placement check, but the enclosing strong item's
`StrongType` was still false → the legal "subordinate to a strong type declaration" arm of §13.18.57.3 SR6 was dead
code. **Fix:** hoist the `TypeName`/`StrongType` assignment ABOVE the clone loop.

**HIGH #B — a cloned OCCURS DEPENDING ON bound data-name-1 GLOBALLY (`ByName[0]`), not in the clone's own record.**
`OdoResolve` resolved the DEPENDING counter via the scope lookup's first global match, so two TYPE references to one
ODO type (or one reference whose counter name collides with any outer item) bound every clone's table to the WRONG
counter → a silently-wrong logical length (whole-group image, MOVE/compare/WRITE extent). **Fix:** resolve
data-name-1 in `RootOf(item)`'s own subtree FIRST (§13.18.57.4 GR1 — the type is coded in place; §13.18.38 SR20 —
data-name-1 is in the same record), falling back to the scope lookup. Golden `typedef_odo` (`T1=<3ABC>` / `T2=<1Z>` —
independent lengths).

**MED #C — `SameStrongType` used the OUTERMOST strong root, mis-typing a nested strong subgroup (false 1533).** A
`TYPE INNER-T` subgroup nested in a strong `OUTER-T` record was identified as OUTER-T, so a legal same-type MOVE/
compare against a standalone `TYPE INNER-T` item was rejected. **Fix:** a new `DataItem.TypeAnchor` (the NEAREST
enclosing item that directly carries a TYPE clause) replaces `StrongRoot` in `SameStrongType`; the top-level and
relative-path cases are unchanged. (#A + #C together → golden `typedef_nested_strong`: `S1=[0012][0034]` / `SAME`.)

**MED #D — §13.18.57.3 SR7 unenforced for WEAK types → COBOLNET1536.** A level-77 subject referencing a GROUP type
was silently cloned into an illegal level-77 group (only the STRONG case was caught, by SR6). SR7 is version- AND
strength-invariant. **Fix:** in `ExpandType`, `item.Level == 77 && template.IsGroup` → 1536.

**MED #E — §13.18.57.3 SR2 unenforced → COBOLNET1537.** A TYPE entry followed immediately by a SUBORDINATE entry
silently merged the explicit member ahead of the cloned ones (a wrong record image; a raw Roslyn CS1061 leak for an
elementary type), and a following LEVEL-88 was silently accepted. **Fix:** in `BindEntries`, a subordinate or 88 whose
superior carries `TypeRefName` → 1537 (both the subordinate-attach and the 88 site).

**MED #F — §13.18.57.3 SR5 unenforced → COBOLNET1538.** A group SUPERORDINATE to a TYPE subject carrying a USAGE/SIGN
clause silently mutated the type's representation (StorageLength). **Fix:** in `ExpandType`, an ancestor with an
explicit `OwnUsage`/`OwnSign` → 1538.

**REFUTED (correctly) — "a CAPACITY register inside a TYPEDEF is unreachable."** The verifier proved both failure
modes reproduce byte-identically with ZERO TYPEDEF involvement — a pre-existing OCCURS-DYNAMIC CAPACITY-register edge,
mis-attributed to D17. Adversarial verification did its job (no wasted fix).

**Spec-check byproduct (DEVLOG-referenced elsewhere): JSON/XML is NON-ISO.** While scoping the next feature I searched
`specs/ISO_COBOL.md` for `JSON`/`XML` — **zero occurrences**. `JSON GENERATE/PARSE` and `XML GENERATE/PARSE` are IBM
Enterprise-COBOL vendor extensions, NOT part of ISO/IEC 1989:2023 — so plan item M3-3 is OUT of the "100% ISO" scope
(deferred to a vendor-dialect track, consistent with the pre-P2.5 constructs scrub). `docs/ISO2023_CONFORMANCE_PLAN.md`
M3-3 + the resume banner corrected.

**Diagnostics added: 1536 (SR7) · 1537 (SR2) · 1538 (SR5)** — the 15xx TYPEDEF band is now 1529–1538. **Tests:** goldens
`typedef_nested_strong` + `typedef_odo` (GreenfieldOnly; byte-verify the two false-positive fixes' run behavior) +
`TypedefReviewFixTests` ×5 (the three new SR negatives + the two SR2 forms + a nested-strong CompileClean). **Battery:
2028 conformance (+7) · 213 unit GREEN; no regressions** (the ODO subtree-first + `TypeAnchor` changes are behavior-
preserving on the existing corpus). **TYPEDEF / the TYPE clause is COMPLETE and review-hardened.** NEXT (an ISO-scoped
item): M3-4 (function/method pointers, conditional-expression enhancements, IEEE-754), the M4 version-gating audit, or
the flagged M2-OO method-scope typedefs / SAME AS.

