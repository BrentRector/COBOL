## Entry 631 — 2026-07-06 16:58 PDT — Phase 5: FUNCTION SUBSTITUTE (§15.87) — LANDED COMPLETELY (per-pair ANYCASE/FIRST/LAST + multi-pair)

SUBSTITUTE is the third keyword-bearing §15 intrinsic — and the first with PER-PAIR phrase keywords: its format
(§15.87.2) is `FUNCTION SUBSTITUTE ( argument-1 { [ANYCASE] [FIRST|LAST] argument-2 argument-3 } … )`. argument-1
is the source; each following group is a replacement pair (match argument-2, emit argument-3) with its OWN phrase
modifiers. Implemented in FULL — every modifier, and the variadic multi-pair form.

- **`BindSubstitute`** — the whitespace splitter isolates each phrase word as a lone `SUB_IDENTIFIER` segment;
  the bind accumulates `pending` flags (ANYCASE = bit 2, FIRST = bit 0, LAST = bit 1) then, on every two operand
  segments after the source, completes a pair (its mode → `BoundIntrinsicCall.SubstituteModes`, one entry per
  pair; `Args` = [source, from₁, to₁, from₂, to₂, …]). FIRST+LAST together, a half pair, a trailing keyword, or
  no pair are all COBOLNET1504 malformed.
- **`CobolIntrinsics.Substitute(source, froms[], tos[], modes[])`** — ONE left-to-right pass (rules 3/4): at each
  position the first pair (in listed order) whose argument-2 matches AND is eligible is substituted, then the scan
  resumes past the matched SOURCE substring (a substituted argument-3 is never re-scanned; occurrences count over
  argument-1, non-overlapping). FIRST/LAST target the pair's first/last SOURCE occurrence (rule 3.a/3.b);
  ANYCASE folds via `OrdinalIgnoreCase` (rule 5). A zero-length argument-1 or any zero-length argument-2 sets
  EC-ARGUMENT-FUNCTION and returns "" (rule 1). Multi-pattern-overlap resolution (order-of-listing precedence +
  per-pair source occurrences) is documented as the canonical refinement where the spec is silent.
- **Renderer** — the `"Substitute"` RenderString case → `RenderSubstitute`, which unzips `Args[1..]` into parallel
  `from`/`to`/`mode` C# arrays.
- **Edition** — 2023 addition (catalog IntroducedIn 2023 / §15.87); the whole-function COBOLNET1502 gate rejects
  it below 2023.

Golden `tests/conformance/2023/substitute.cob` (ALL / FIRST / LAST over "ABABAB"; ANYCASE over "aAaA"; the
two-pair "CAT DOG"→"FISH BIRD"; a growing "AB"→"WXYZ" replacement), **GreenfieldOnly** — the frozen legacy
grammar cannot bind the per-pair phrase keywords. +7 `IntrinsicFunctionDifferentialTests`. Battery: **1897
conformance (+7) · 216 unit · 112 corpus goldens GREEN**; greenfield-only (catalog Deferred→Runtime, the
SUBSTITUTE bind path, the RenderString case + `RenderSubstitute`, the runtime body + a defaulted
`BoundIntrinsicCall.SubstituteModes`). Phase-5 intrinsics live: CONCAT/BASECONVERT (628), TRIM (629),
FIND-STRING (630), SUBSTITUTE (631); residue = CONVERT (keyword-arg form) + MODULE-NAME/SMALLEST-ALGEBRAIC +
the 2014 date family, each to be done in full when reached.

