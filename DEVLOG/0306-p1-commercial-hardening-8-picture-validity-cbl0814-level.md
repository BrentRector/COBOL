## Entry 306 — P1 commercial-hardening #8: PICTURE validity (CBL0814) + level-number guard (CBL0815)

Third P1 item, two related data-definition robustness fixes.

**Level-number guard (CBL0815, unconditional).** `SemanticBuilder.VisitDataDescriptionEntry` did
`int level = int.Parse(levelCtx.GetText())`; the grammar's `INTEGERLIT` permits any digit run, so a level
like `999999999999` threw an uncaught `OverflowException` (a compiler crash, not a diagnostic), and
out-of-range-but-parseable levels (0, 50-65, …) were silently accepted. Replaced with `int.TryParse` + a
range check (ISO §8.5.1.2: 1-49, 66, 77, 88); on failure it reports CBL0815 and skips the entry instead of
crashing. **Unconditional** (not dialect-gated): no valid program has an out-of-range level, so it never
fires on the corpus — and the full guard (Default mode) therefore validates it *directly*.

**Illegal-PICTURE-symbol (CBL0814, strict-gated).** The runtime `PicDescriptorFactory.FromPicBody` is a
char-counting parser whose `default:` arm silently skips any unrecognized character, so a mixed picture like
`9Q9` compiles with the `Q` swallowed (category still Numeric — only an all-invalid picture becomes
`Unknown`). Added a pure compiler-side helper `PicUsageResolver.FindIllegalPicSymbol` that scans the
EXPANDED pattern (so `9(5)` repeat-count parens aren't mistaken for symbols) for any char outside the valid
set `9 X A N S V P Z * + - , . / B 0` + the program's currency symbol + the two-char `CR`/`DB`;
`SemanticBuilder` emits CBL0814, dialect-gated to `>= StrictCobol85` (staged like CBL3128). The runtime
`FromPicBody` is left pure (no DiagnosticBag) — validation lives entirely in the compiler layer.

**Dry-run (the value of staging).** Compiled all 349 baselined NIST programs in StrictCobol85 (temporary
harness). First pass flagged 4 programs (NC203A/245A/251A/252A) on a `;` in pictures like `99;` / `S999;`.
Root cause: `;` is a COBOL *separator* (equivalent to a space, never a picture symbol) that the PIC lexer
captures as a trailing artifact — `FromPicBody` already ignores it, so it's benign. Fixed the validator to
treat `;`/space as separators (correct COBOL, not a workaround). Re-run: **0/349** — so unlike CBL3128 (one
IC228A blocker), CBL0814's eventual Default-flip is already corpus-clean.

**Deferred (documented).** The intricate ISO §13.18.40.3 structural rules — V/'.' mutual exclusion, P
contiguous-edge-run, S-must-be-first, Z/'*' exclusion, length/digit-position limits — carry higher
complexity and false-positive risk (dual-edge P, floating Z/+/-, DECIMAL-POINT IS COMMA) and are a separate
pass. This item delivers the crash-fix + illegal-symbol detection.

**Test refactor.** Hoisted the strict-mode compile helper into `DiagnosticTestBase` as a
`GetDiagnostics(source, dialect)` overload (item-5's test now uses it — dedup).

**Verify:** build 0 warnings; +6 unit (`PicValidityTests`: out-of-range/huge level → CBL0815, valid levels
clean; `9Q9` strict → CBL0814, Default → none; `999DB`/`ZZ,ZZZ.9`/`SP(8)9`/long-V → none). Full guard
**ALL GREEN — 1015 unit (1009 +6) / 347 integration / 349 NIST baselines, 0 FAIL\***.

