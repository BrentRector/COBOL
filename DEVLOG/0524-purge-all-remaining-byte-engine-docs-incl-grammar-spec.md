## Entry 524 — 2026-06-09 18:36 PDT — Purge ALL remaining byte-engine docs (incl. grammar/spec excerpts) — docs/ is now greenfield-only

Owner decision (after Entry 523): "Delete EVERYTHING byte-engine incl. grammar/spec." Removed the residual ~96 files
the Entry-523 CobolSharp* sweep left: all non-`COBOLNET_*` `docs/*.md` (BINDER/SEMANTIC-ANALYSIS/DATA-DIVISION/
IL-BYTECODE designs, OO_IMPLEMENTATION_DESIGN, REPORT_WRITER_*, collating-*, BATCH5/M407/M411, batch3, the byte-engine
plans COBOL85_COMPLIANCE_PLAN / CONFORMANCE / MULTIVERSION_ROADMAP / SPEC_FIX_* / SPEC_GAP, the GRAMMAR/CATEGORY/SCOPE
rule docs, ANTLR4-* + REPORT_WRITER_DESIGN.json), the `docs/binder|boundtree|cilemitter|ir` decomposition docs, the
extracted spec excerpts `docs/spec-text/` + `docs/spec-grammar/` (redundant with the authoritative `specs/ISO_COBOL.md`
submodule), the root `audit/` (31 files), and the root byte-engine docs (AUDIT_REPORT, GRAMMAR_AUDIT, MIGRATION_LEDGER,
NIST_TEST_REPORT, ANTLR4_VSCODE_IMPORT_BUG[_RESPONSE]).

**docs/ is now greenfield-only (18 files):** the `COBOLNET_*` design corpus (SSOT + overview + 12 deep-dives), the
version-correctness docs, the M2/M3/M4 conformance catalog, and DOC_INDEX. **DOC_INDEX.md was rewritten fresh** as a
concise index of the surviving corpus. **Kept at root:** `resume-prompt.md`, `CLAUDE.md`, `DEVLOG.md`, `CONSTRAINTS.md`
+ `PROMPT.md` (doctrine), `README.md`. **Authoritative spec:** `specs/ISO_COBOL.md` (submodule, intact). Dangling refs
fixed in PROMPT.md (→ resume-prompt + DEVLOG). The conformance-catalog doc keeps its byte-engine-framing banner (use
only §3 = the feature catalog). Doc-only; the greenfield build + suites are unaffected (no doc is a build input).

