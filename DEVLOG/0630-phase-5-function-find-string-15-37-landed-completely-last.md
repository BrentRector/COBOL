## Entry 630 — 2026-07-06 16:22 PDT — Phase 5: FUNCTION FIND-STRING (§15.37) — LANDED COMPLETELY (LAST / START AFTER / ANYCASE)

FIND-STRING is the second §15 intrinsic whose argument list interleaves phrase keywords with operands, so it
took a bespoke bind path like TRIM — and it is implemented in FULL (every phrase word, not scoped down).
General format (§15.37.2): `FUNCTION FIND-STRING argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE]`
(paren-enclosed per the §8.4.3.2.2 function-identifier format — the §15.37.2 presentation just shows the
argument-list content). Returns an integer position (§15.2 type integer).

- **Keyword extraction** — the whitespace splitter (`ReferenceResolver.SplitSubscriptTokens`) isolates each bare
  word as a lone `SUB_IDENTIFIER` segment, so `BindFindString` reads them exactly like TRIM's LEADING/TRAILING:
  `LAST` → `BoundIntrinsicCall.FindLast` (rule 1, the last occurrence); `ANYCASE` → `FindAnycase` (rule 4);
  `START`/`AFTER` are the optional argument-3 introducer words (noise — argument-3's presence alone selects the
  skip form). The operand segments are argument-1 (haystack), argument-2 (needle), optional integer argument-3.
- **Runtime** `CobolIntrinsics.FindString(hay, needle, last, skip, anycase)` — collects the 1-based positions of
  every NON-OVERLAPPING match (a match consumes needle.Length — §15.37 is silent on overlap, so this follows the
  INSPECT "all occurrences" reading §14.4.6, documented as the canonical refinement); rule 2 `skip` (argument-3)
  ignores that many matches counting from the first (or from the last under `LAST`); ANYCASE folds both via
  `ToLowerInvariant` (rule 4 — LOWER-CASE without a locale); zero-length argument-1/argument-2 (rule 5) or no
  remaining match (rule 3) ⇒ 0. Integer result renders through `RenderNum` (the `"FindString"` case).
- **Edition gate** — FIND-STRING is a 2023 addition (catalog IntroducedIn 2023 / §15.37); the whole-function
  COBOLNET1502 name+edition gate rejects it below 2023 automatically (no bespoke gating needed, unlike TRIM's
  argument-2 leg).

Golden `tests/conformance/2023/find_string.cob` (FIRST/LAST/SKIP1/LASTSKIP1/ANYCASE/CASED/NONE over
`"ABCABCABC"` matches at 1/4/7 + a `"Hello World"` ANYCASE case), **GreenfieldOnly** — the frozen legacy grammar
cannot bind the LAST/START AFTER/ANYCASE phrase. +9 `IntrinsicFunctionDifferentialTests` (the five position
forms, ANYCASE fold, case-sensitive default = 0, the 2014 name+edition gate). Battery: **1890 conformance (+9) ·
216 unit · 111 corpus goldens GREEN**; greenfield-only (catalog Deferred→Runtime, the FIND-STRING bind path, the
RenderNum case, the runtime body + two defaulted `BoundIntrinsicCall` init-properties). Phase-5 intrinsics live:
CONCAT, BASECONVERT (628), TRIM (629), FIND-STRING (630); residue = SUBSTITUTE/CONVERT (keyword-arg forms) +
MODULE-NAME/SMALLEST-ALGEBRAIC + the 2014 date family, each to be done in full when reached.

