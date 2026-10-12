## Entry 1260 — 2026-08-09 11:45 PDT — The financial/random domain guards land at one raise site per rule, both carriers — three more rows

ANNUITY carried no §15.9.3 r2 rate guard (ANNUITY(-0.5 3) returned +0.0714 silently while Log/Log10
in the same file carried their guards), PRESENT-VALUE none for §15.74.3 r2 (rate ≤ −1 inverted the
discount base and summed anyway), and RANDOM's seed mask folded a negative seed onto a positive one
(RANDOM(-5) silently aliased a legal sequence where §15.75.3 r2 says "zero or a positive integer" —
an incorrect argument VALUE that sets EC-ARGUMENT-FUNCTION). Each rule now has ONE raise site
(AnnuityDomain / PresentValueDomain / the Random guard), and the PB56 Dec-carrier twins funnel
through the SAME sites — the two-arm-dispatch scar checked at landing time, not discovered later.
Golden pb65_domain_guards: the three violations return the documented default 0 whole, the legal
control ANNUITY(0 4) = 0.25 still computes. Intrinsic unit filters 49/49, SpecTraceability 10/10.
THREE rows re-verdict CONFORMS+tested. GAP 4,198 → 4,195; the defect backlog stands at 193.

