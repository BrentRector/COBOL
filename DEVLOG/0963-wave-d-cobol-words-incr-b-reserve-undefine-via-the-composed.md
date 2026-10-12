## Entry 963 — 2026-07-21 20:25 PDT — Wave D: `>>COBOL-WORDS` Incr B — RESERVE/UNDEFINE via the composed `ReservedWordSet` + SR3/SR4 category validation

**Incr B of Track 1.** Wired the `CobolWordsMap` into the compiler so RESERVE/UNDEFINE actually change the
reserved-word decision, and added the SR3/SR4 category validation that needs all three registries.
- **Threading:** `CobolWordsMap` rides `BindSession.CobolWords` (defaulting `Empty`), passed
  `Frontend.CobolWordsMap` → `CompilerDriver` → `CSharpEmitter.Bind` → `BinderDriver.Bind` → the session. No
  `VersionConformancePass.Run` signature change — it reads `group.Session.CobolWords`.
- **Composed `ReservedWordSet`** (Editions) — a `Compose(CobolWordsMap)` overlay: RESERVE (GR5) adds a word
  (`RejectsAt`→true, high-confidence-equivalent); UNDEFINE/SUBSTITUTE-lit4 (GR3/GR4) add to a suppress set
  (`RejectsAt`→false even for a base-reserved word — suppression wins). `Empty`⇒`Default` (byte-identical, same
  reference). `VersionConformancePass.ParseArm` now takes the composed set (was the hard-coded
  `ReservedWordSet.Default`), so the §8.9 funnel (`VisitCobolWord`→`RejectsAt`→COBOLNET0901) honors the directive.
- **SR3/SR4 category validation** (`VersionConformancePass.ValidateCobolWords`) — the EXISTING word (lit1/3/4)
  must be reserved/context/intrinsic (SR3); the NEW word (lit2/5/6) must be none (SR4). Category facts:
  `CobolKeywordTokens` (Frontend — the reverse `word→lexer-token-type` map built once from
  `CobolLexer.DefaultVocabulary`, reachable from the Compiler) for reserved+context words; `IntrinsicCatalog`
  for functions; `ReservedWords` for the §8.9 table. Both checks err AWAY from rejecting legal source (SR3
  accepts on ANY signal; SR4 rejects only on a CERTAIN one). Emits COBOLNET1623 via the edition sink.
- `CobolKeywordTokens` iterates `GetLiteralName(t)` to a generous fixed bound (null for out-of-range — the
  `IVocabulary`/`Vocabulary` types don't expose a MaxTokenType accessor in this ANTLR build).

RESERVE is now fully functional end-to-end; UNDEFINE's suppress-set is composed but only observable once the
token rewriter lands (Incr C — the de-reserved word must first re-lex as an IDENTIFIER to appear in a user-word
slot). **Gate (wave-local):** fresh build (0 warn) · `CobolWordsDirectiveTests` **29/29** (8 new: Compose,
CobolKeywordTokens, RESERVE→0901, SR3/SR4, valid-EQUATE) · `ReservedWordPosition` **10/10** (base 0901 path
byte-identical) · characterization **33/33** · CLI-probed RESERVE→0901 / SR3 / SR4 / valid-EQUATE. Negative
golden `cobol-words-reserve-rejects` (0901@2023). Full VersionMatrix confirmed green at Incr A (927/0 + 840/0;
a ~15-min per-batch gate, not run per-increment). **NEXT = Incr C** (the token rewriter — EQUATE/SUBSTITUTE
identifier↔keyword + UNDEFINE keyword→identifier + the map-aware lexer data-name gate).

