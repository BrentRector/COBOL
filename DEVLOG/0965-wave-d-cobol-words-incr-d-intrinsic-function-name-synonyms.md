## Entry 965 — 2026-07-21 21:20 PDT — Wave D: `>>COBOL-WORDS` Incr D — intrinsic-function-name synonyms; ⭐ TRACK 1 COMPLETE

**Incr D of Track 1 — the last increment.** Intrinsic-function-name EQUATE/UNDEFINE/SUBSTITUTE (the case the
token rewriter cannot handle, because intrinsic names are IDENTIFIERs, not keyword tokens — the rewriter skips
them by design). `IntrinsicBinder.BindIntrinsicCore` now resolves the FUNCTION name through `ctx.CobolWords`:
- a SYNONYM (EQUATE literal-2 / SUBSTITUTE literal-5) whose canonical is an intrinsic → the canonical name;
- an UNDEFINE'd (literal-3) / SUBSTITUTE'd-away (literal-4) intrinsic → no longer a function (COBOLNET1501).
`cobolWordsRemoved` is tested on the ORIGINAL written name so SUBSTITUTE works (literal-4 is BOTH in DeReserved
AND the synonym target — checking the original name means `FUNCTION literal-5` resolves while `FUNCTION literal-4`
is removed). Threading: `CobolWordsMap` rides `DataBinder.CobolWords` (a session init prop set in
`BinderDriver.BindUnitData`), exposed on `BinderContext.CobolWords`.

**Gate (wave-local — no `.g4` change, so no full legacy guard):** fresh build (0 warn) · `CobolWordsDirectiveTests`
**34/34** (3 new: EQUATE/UNDEFINE/SUBSTITUTE intrinsic) · characterization **33/33** byte-exact (the resolution is
a no-op for the empty map) · the `cobol_words_intrinsic` golden (EQUATE MAX WITH LARGEST → `FUNCTION LARGEST(3 7 5)`
= 7, run+compared) + its GreenfieldOnly exclusion. CLI-probed EQUATE/UNDEFINE/SUBSTITUTE(both directions).

**⭐ TRACK 1 (`>>COBOL-WORDS`, §7.3.10) IS COMPLETE** — all four options fully supported for reserved,
context-sensitive, AND intrinsic-function words; SR1–SR5 enforced; design SSOT
`docs/rearchitecture/DESIGN-cobol-words-directive.md` = IMPLEMENTED. **NEXT = Track 2: CC-directives-inside-COPY
(§7.2.1)** — merge the CC + COPY stages into one interleaved text-manipulation driver (spec Step 1 COPY-expand
→ Step 2 CC-process, so copybook directives are processed; the copybook >>SOURCE FORMAT GR5 reversion rides the
same driver). High blast radius; GnuCOBOL-differential gated before/after.

