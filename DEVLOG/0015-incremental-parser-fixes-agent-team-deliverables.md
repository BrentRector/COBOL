## Entry 015 — 2026-03-14: Incremental Parser Fixes, Agent Team Deliverables

### Agent Team Results

Launched 5 expert agents. Results:

1. **COBOL spec expert** — Delivered `docs/SCOPE-RULES.md` (scope termination rules).
   Limitation: couldn't read the ISO PDF (no bash), synthesized from training data.
2. **Grammar expert (in-place)** — Expanded `GRAMMAR-REFERENCE.md` from 1402 to 1775 lines.
   Added 22 missing statement formats, corrected IF/PERFORM/SET/CALL/EVALUATE formats.
3. **Grammar validator** — Identified 36 issues. Findings overlap with in-place agent.
4. **Parser architecture reviewer** — Delivered `PARSER-ARCHITECTURE-REVIEW.md`. Identified
   every infinite loop risk, recommended the sentence-based architecture that was implemented.
5. **OCR agents** (3 attempts) — ALL FAILED on bash/read permissions for the 394MB rasterized
   PDF. The approach of pymupdf + Claude vision works (verified manually) but agents can't
   execute it. This remains unresolved.

### Parser Fixes Applied

- `IsEndProgram()` — multi-program source files now correctly stop at `END PROGRAM`
- Parenthesized conditions — `(A >= B)` inside IF conditions now parsed correctly
- PROCEDURE DIVISION USING/RETURNING clause parsing
- DECLARATIVES section handling (skip until END DECLARATIVES)
- OCCURS n TO m range form
- Level 88 VALUES ARE: only consume actual "ARE" word
- MOVE/ADD/SUBTRACT target loops: stop at ON/NOT keywords
- NEXT SENTENCE, RETURN, RELEASE added as statement starts

### NIST Progress
- Start of session: 78/391 (20%), ALL programs hanging
- After parser rewrite: 78/391 (20%), 0 hangs
- After signed literals: 78/391 (20%), NC101A errors 119→12
- After latest fixes: batch running, expecting improvement from multi-program and
  parenthesized condition fixes

### Process Lessons
- OCR agents fail consistently on permissions. Need to do OCR extraction in the main
  conversation with direct bash access, not via agents.
- Agents that can't build/test produce incomplete work. The fix agents that had bash
  access produced better results than those without.

---

