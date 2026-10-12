## Entry 890 — 2026-07-17 23:30 PDT — PHASE-13 Wave I (partial) — adversarial review of the 8 Wave C constructs; 5 defects fixed

Ran the adversarial find→verify review (17-agent Workflow, fresh-context skeptics — one finder per construct + a
per-finding verifier defaulting to REFUTED). **9 confirmed defects across the 8 constructs.** Fixed the 5 that
reject/miscompute VALID input (the worst class); documented 2 accept-invalid gate-refinements where the runtime is
already spec-correct; and the review VINDICATED the earlier no-defect constructs (word-length: 0 findings).

**Fixed (verified via CLI probe; battery re-run):**
1. **`statusPhrase` grammar (3 findings, pre-existing STOP bug inherited by sharing the rule):** the rule wrongly
   REQUIRED `WITH` (so `GOBACK NORMAL.` / `STOP RUN NORMAL.` were rejected — WITH is an optional word, §5.2.3), bound
   the STATUS keyword to its operand (so `WITH ERROR STATUS.` with the operand omitted was rejected), and admitted a
   keyword-less `STATUS operand` (over-accepting — §14.9.42.2 makes `{ERROR|NORMAL}` a required brace group). New rule:
   `WITH? (ERROR | NORMAL) (STATUS (dataReference | literal)?)?`. Fixes both STOP and GOBACK.
2. **CONTINUE fractional-negative (§14.9.9 GR1a/b):** the sign test ran on the m=0-TRUNCATED integer, so a negative
   fractional interval in (-1,0) truncated to 0 and failed to set EC-CONTINUE-LESS-THAN-ZERO. Now the interval is
   rendered at full precision (`NumericRenderer.Real`) and the sign test precedes the truncation — `CONTINUE AFTER
   -0.5` under CHECKING ON now correctly sets the EC (was silently '00').
3. **WRITE combined-advance LINAGE (§14.9.51 GR25e/f + GR26/GR7c):** the two advances collapsed into a single
   LINAGE-COUNTER increment, mishandling a page boundary between them. Now `Advance`+`AdvanceLinageCounter` run
   separately per operation (before, then after) so each advance gets its own overflow/footing logic.
4. **Boolean shift × binary-operator precedence (§8.8.2 rule 7b):** the documented rule-7b limitation was
   silently mis-grouping VALID mixed expressions. Now a shift at the same level as a B-AND/B-OR/B-XOR is REJECTED
   LOUD (COBOLNET1569, "parenthesize to disambiguate") via a precise parse-ancestry check — LOUD-not-silently-wrong;
   the unmixed default (the Table A.2 oracle + realistic usage) is unaffected.
5. **NO SIGN diagnostic §-citation:** COBOLNET1566 cited §13.18.60.3 SR31, but SR31 (the 'S'-forbidding rule) is in
   the PICTURE clause §13.18.40.3 (verified spec line 20397) — corrected.

**Documented as follow-ons (accept a non-conforming program, but the runtime behavior on valid input is spec-correct):**
- **SET SIZE SR34** (a literal integer-2 exceeding the LIMIT / negative is not compile-time-diagnosed): the runtime
  already floors negative → 0 and clamps > LIMIT per GR37/GR38, so the behavior is correct; only the early
  syntax-rule diagnostic is missing (no LIMIT ⇒ only a negative literal is even a violation).
- **PERFORM UNTIL EXIT SR8** (UNTIL EXIT nested UNDER a VARYING PERFORM is not statically rejected): a cross-statement
  static check; the same-statement VARYING+UNTIL-EXIT form cannot parse (they are alternatives of performOptions).

Diag band: COBOLNET1569 now used (boolean-shift-mixed); next free **1570**.

