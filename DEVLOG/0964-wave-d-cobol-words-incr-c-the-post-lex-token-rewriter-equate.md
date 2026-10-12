## Entry 964 — 2026-07-21 21:05 PDT — Wave D: `>>COBOL-WORDS` Incr C — the post-lex token rewriter (EQUATE/UNDEFINE/SUBSTITUTE) + the map-aware lexer

**Incr C of Track 1 — the heart of the feature.** The owner-directed post-lex token rewriter makes
EQUATE/UNDEFINE/SUBSTITUTE actually re-tokenize per the `CobolWordsMap`, with no per-group grammar regen.
- **`CobolWordsRewriter`** (Frontend/Parsing) — runs beside `ZeroTokenRewriter` in `Frontend.LexAndParse`. Two
  disjoint retypes: SYNONYM (EQUATE lit2 / SUBSTITUTE lit5) — an `IDENTIFIER` whose text is a synonym → the
  canonical reserved/context word's token type (spelled canonically); DE-RESERVED (UNDEFINE lit3 / SUBSTITUTE
  lit4) — every token of the de-reserved word's keyword type → `IDENTIFIER` (source spelling kept). A word with no
  keyword token type (a pure intrinsic-function name) is left for the binder (Incr D).
- **Map-aware lexer** (`CobolLexer.g4`): `PreviousTokenCouldBeDataName()` now also consults a per-group set
  `SetCobolWordsDataNames(...)`, so a de-reserved word used as a SUBSCRIPTED data name opens SUBSCRIPT mode at its
  `(` even though it is still lexed as its keyword token (the `(` decision is frozen at lex time; the post-lex
  retype cannot repair it). Null/empty by default ⇒ byte-identical (the legacy pipeline never sets it).
  `Frontend.LexAndParse` sets it BEFORE tokenization when the map is non-empty.
- **⚠ VERIFY-BY-RUNNING caught the hazard:** the FIRST rewriter (no lexer change) failed the subscripted UNDEFINE
  case (`MOVE(2)` → "no viable alternative at input '('") because the lexer had frozen `(` as grouping — exactly the
  §6 hazard the design predicted. The lexer map-awareness fixed it; all four forms then produced correct output.

**Comprehensive gate (shared `.g4` ⇒ full legacy guard REQUIRED):** full greenfield Conformance **3804/0** (incl.
the 3 new goldens); `guard-fast.sh` ALL GREEN (NIST 353 MATCH, 0 regression). ⚠ **The first guard run reported
IF110A/ST107A "COMPILE FAILED — REGRESSION" — a FLAKY parallel-compile failure (JOBS=32 cold-start contention),
NOT a real regression** (both compile cleanly standalone + via the guard's exact command run serially; aggravated
by a `taskkill dotnet.exe` I ran right before launch — don't do that). Re-ran clean → ALL GREEN.
`CobolWordsDirectiveTests` **31/31** (2 new) · characterization **33/33** byte-exact. Three positive goldens
`cobol_words_{equate,substitute,undefine}` (run+compared at 2023) with GreenfieldOnly exclusions (the frozen legacy
only blanks the directive). **NEXT = Incr D** (intrinsic-function-name synonyms — `IntrinsicBinder` resolves the
name through `ctx.CobolWords`: a synonym → canonical intrinsic; an UNDEFINE/SUBSTITUTE-removed intrinsic → not a
function; DeReserved checked on the ORIGINAL written name so SUBSTITUTE works).

