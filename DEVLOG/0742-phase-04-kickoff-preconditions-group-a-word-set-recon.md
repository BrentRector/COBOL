## Entry 742 — 2026-07-10 14:11 PDT — PHASE 04 kickoff: preconditions + Group-A word-set recon (context-doc sweep for a new session)

Opened PHASE 04 (frontend consolidation) and completed its Step-0 recon; this entry + the doc sweep exist so a new
session resumes cleanly at Group A without re-deriving anything.

**§1 preconditions ALL PASS:** P1 done (generated ns `CobolNet.Frontend.Generated`; dead grammars + the JSON/XML fragment
removed), P2 done (`Cobol.Net.Editions` present), and the proven codegen+drift pattern to EXTEND is present
(`gen-reserved-words.ps1` + `ReservedWordsDriftTests` + `reserved-words.json`). Neutrality baseline captured: the lexer
`.tokens` at `/e/tmp/CobolLexer.tokens.baseline` (951 lines) — the byte-identity proof for the Group-A/B grammar flips.

**Group-A Step-A1 extraction/reconciliation (DONE, deterministic):** the context-sensitive word set is **77 words** — the
union of `nameSlot` (the parser `cobolWord` rule, `CobolParserCore.g4:25-113`, 71 tokens incl. IDENTIFIER) and
`subscriptTrigger` (the lexer `_dataNameTokens` set, `CobolLexer.g4:30-72`, 76 tokens incl. IDENTIFIER). The symmetric
difference confirms EXACTLY the doc's predicted FU-1 asymmetries — captured AS-IS (a "fix" would change tokenization and
break byte-neutrality): 70 words in BOTH; `BIT` nameSlot-only (in `cobolWord`, not the lexer set — the latent
under-trigger); `DISPLAY/MERGE/RANDOM/SIGN/SORT/SUM` subscriptTrigger-only (in the lexer set for the `functionName`
collision, not `cobolWord`). Recorded in the PHASE-04 doc STATUS resume block.

**⚠ Group-D re-assessment flag:** PHASE 03 built the two-arm `VersionConformancePass` (a parse-arm walking the RAW parse
tree + a bound-arm over resolved facts), NOT the design's grammar-action "construct-id annotation side-table" — so Group
D's annotation convention is LIKELY SUPERSEDED (the parse-arm reads the tree directly) and the superset grammar is already
complete (P3 dropped every edition predicate save the two forward-detects). Re-assess Group D against the AS-BUILT pass
when reached; it may reduce to a reconciliation note. Groups A–C proceed as the doc writes.

**RESUME AT (Group A execution):** author `tests/version-matrix/cobol-words.json` (77 rows) → `scripts/gen-cobol-words.ps1`
→ `CobolWordsDriftTests` → flip the lexer + parser to the generated artifacts → verify `.tokens` byte-identical + FULL
legacy guard ALL GREEN + the name-slot smoke probe → COMMIT A5. Then Group B (literal fragments), Group C (`Cst/` façade),
the Group-D re-assessment, and the D10 SUBSCRIPT-mode removal sub-track (its own characterization proof; likely a fresh
`DESIGN-frontend-grammar.md` note first). Battery at Phase-04 start: greenfield conformance 3157 · unit 223 ·
characterization 32 · INV-1-strong 349/349 · FULL legacy guard NIST 353 MATCH.

