## Entry 743 — 2026-07-10 15:46 PDT — PHASE 04 Group A: single-source the context-sensitive word set from cobol-words.json (commit boundary A5)

Landed PHASE-04 Group A: the context-sensitive word set — the tokens that are keywords in context yet legal
user-defined words elsewhere — is now GENERATED from one declarative source instead of hand-mirrored across two grammar
files. This kills the triple hand-sync the `CobolLexer.g4:20-21` comment used to literally instruct.

**What changed.** `tests/version-matrix/cobol-words.json` (77 rows: `token`/`nameSlot`/`subscriptTrigger`/`note`) is the
single source. `scripts/gen-cobol-words.ps1` emits two committed artifacts — the parser `cobolWord` rule
(`Grammar/Core/CobolWords.g4`, an imported fragment grammar) and the lexer subscript-trigger set
(`Parsing/CobolLexerWordSet.g.cs`, a `partial class CobolLexer` holding `_dataNameTokens`) — extending the proven
`gen-reserved-words.ps1` codegen. `CobolParserCore.g4` deletes its hand-written `cobolWord` rule and adds `CobolWords`
to the import list; `CobolLexer.g4` @members drops the `_dataNameTokens` HashSet (keeping `_lastNonWsTokenType`,
`PreviousTokenCouldBeDataName`, `NextToken`). `CobolWordsDriftTests` (×4) binds the three consumers so they cannot
silently desync: the parser rule text, the RUNTIME reflected `_dataNameTokens` (via `CobolLexer.DefaultVocabulary`), and
the reserved-words cross-check.

**Byte-neutral, proven multiple ways:** (1) `CobolLexer.tokens` byte-identical to pre-flip — the set is C# runtime data,
not part of the ATN/token vocab, and the parser `cobolWord` move does not touch the lexer; (2) an INDEPENDENT re-parse
of the pre-flip grammar sources shows the generated `cobolWord` set == old set (71 tokens) and the generated
`_dataNameTokens` set == old set (76 tokens); (3) greenfield conformance 3157; (4) unit 227 (223 + 4 new drift tests);
(5) characterization 32 byte-exact; (6) FULL legacy guard NIST **353 MATCH, 0 regressions, ALL GREEN** (the frontend is
SHARED with the legacy oracle — a grammar change must pass it). A cold `dotnet clean` + rebuild (fresh-CI simulation)
regenerates from scratch with `.tokens` still identical and the committed partial surviving the `Generated/*` wipe (it
lives under `Parsing/`). A free-format smoke probe confirms `SECURE (2)` still triggers SUBSCRIPT mode and `NORMAL`
binds as a data name.

**The two documented asymmetries (FU-1), captured AS-IS** (a "fix" would change tokenization → break neutrality): `BIT`
is nameSlot-only (in `cobolWord`, not the lexer trigger set — a safe latent under-trigger); `DISPLAY/MERGE/RANDOM/SIGN/
SORT/SUM` are subscriptTrigger-only (in the lexer set for the `functionName` collision, not `cobolWord`).

**DESIGN DEVIATION (recorded per process rule 4 — "explain why the original wasn't followed").** The PHASE-04 doc's
Step-A2 item-4 proposed reserved-words cross-check — "a `subscriptTrigger` word must be user-legal at ≥1 edition" — is
UNSOUND against the actual data: the six functionName-collision words are RESERVED keywords (not user words), and two
nameSlot words (`COLUMN`, `LENGTH`) plus `SCREEN` are §8.9-reserved at all four editions yet appear in `cobolWord`
(syntactically admitted; the §8.9 funnel makes the semantic rejection). Implemented instead: **RW-1** (every
`subscriptTrigger`-ONLY word maps to a 2023-reserved entry) + exact **reconciliation pins** on BOTH asymmetry sides.
The PHASE-04 doc Step-A2 item-4 is updated with this as-built deviation.

**Adversarial review (wf_16cc83d1-1cc, 4 lenses → 10 agents) raised the ONE real defect + it was FIXED.** All five
CONFIRMED findings converged on a false-green gap in the NEW drift guard: it pinned `nameSlot-only == {BIT}` but left
`subscriptTrigger-only` un-pinned, so a future JSON edit flipping a shared+2023-reserved word (`COLUMN`/`LENGTH`/`SCREEN`)
to `nameSlot=false` would silently drop its cobolWord admission while all tests stayed green (RW-1 can't catch it — those
words ARE reserved). Fix: a SYMMETRIC exact pin `subscriptTrigger-only == {DISPLAY,MERGE,RANDOM,SIGN,SORT,SUM}` in both
the generator and the drift test, plus a corrected overclaiming docstring. Proven by a mutation test — flipping `COLUMN`
to `nameSlot=false` now makes the generator throw; the pins are validation-only so the emitted artifacts stay
byte-identical. (Missteps logged for transparency: the doc's smoke probe assumed fixed-format + words that don't declare
cleanly — the real path is free-format, and a concurrent `dotnet clean` during the first guard run produced a spurious
"1 regression" that a clean re-run cleared to 353 MATCH.)

**RESUME AT: PHASE-04 Group B** — share the SUBSCRIPT/DEFAULT literal token bodies via `fragment` rules
(`STR_BODY`/`NAT_BODY`/`BOOL_BODY`/`INT_BODY`/`DEC_BODY`/`NAME_BODY`), an independent low-risk lexer refactor with the
same `.tokens`-byte-identity + full-legacy-guard discipline (commit boundary B1).

