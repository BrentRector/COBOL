## Entry 598 — 2026-07-03 23:01 PDT — W3 ⑤: preprocessor DialectLevel threading — VCR rows 2/4/94 GATED (the frontend's first edition-aware gates)

**The three preprocessor-level edition obligations landed (roadmap W3 item ⑤): only the COLUMN-AWARE pass can
see the col-7 indicator and only the COPY expander sees the REPLACING operand forms, so these emit sites live
in the FRONTEND — the severity policy mirrors `EditionContext` locally (removed = error strict / warning
permissive; obsolete = warning always), the metadata stays registry-canonical (3 new drift-locked rows).**

- **VCR 2 (Annex E.2 item 1 bullet 2) — fixed-form WORD continuation removed 2023:**
  `ReferenceFormatProcessor.EditionGates.OnWordContinuation` — a NON-literal col-7 continuation whose splice
  joins two COBOL word characters (§6.2.4: the continuation's first nonblank immediately follows the
  preceding line's last nonblank). Literal continuation and non-word splices are untouched. 0902 once per
  file; pre-removal join semantics preserved on both axes.
- **VCR 4 (E.2 item 1 bullet 4) — non-pseudo-text COPY REPLACING operands removed 2023:**
  `CopyProcessor.OnNonPseudoTextOperand`, fired from the operand reads of the COPY call site only (the
  REPLACE statement is pseudo-text-only surface and NOT in the removal). The matrix row's source uses a
  deliberately-missing copybook (the non-strict comment fallback) so the gate is exercised without copybook
  plumbing.
- **VCR 94 (F.2 item 4) — the col-7 hyphen indicator obsolete 2023:** 0903 warning once per compilation, any
  continuation form (the F.2 text covers the whole mechanism incl. literal continuation).
- **Plumbing:** `Frontend.Permissive` (new option) threaded from `CompilerDriver`; frontend-bag WARNINGS now
  ride `Result.Warnings` on EVERY outcome (they were silently dropped — the 0903/permissive-0902 gates made
  the gap load-bearing); the FrontendError path now splits errors from warnings.
- **Verified end-to-end by probe** (all 8 edition × axis combinations): word-continuation runs silent @2014,
  0902-errors @2023 strict, warns+runs @2023 permissive; literal continuation gets ONLY the 0903 @2023;
  COPY REPLACING word-operand silent @85 (runs 456), 0902 @2023 strict, warns+runs permissive.
- Three registry+json rows; 2 negative-corpus witnesses (`fixed-form-word-continuation`,
  `copy-replacing-non-pseudo-text`, both @2023, construct-specific `.err`s); VCR rows 2/4/94 → GATED.
- ⚠ NIST exposure: ZERO — the CCVS corpus's col-7 continuations are literal continuations, and the golden
  runs compile at `--std 85` (silent); the 2023-permissive INV-1-STRONG leg re-proved 349/349 byte-exact
  (the 0903 warnings ride the warning channel, not program output).

**Battery: conformance 1398/1398 (+20) · unit 102/102 · INV-1-STRONG 349/349 at 2023-permissive · sweep
419 OK / 0 BREAKS · FULL legacy guard ALL GREEN (353 MATCH · 1204 unit · 537 integration).**
**W3 remaining: item ④ only (the notInGrammar 85-acceptance set — RERUN, ENTER, USE FOR DEBUGGING, section
segment-numbers) — the final Phase-2 work item before Phase 3 (the M2 OO port).**

