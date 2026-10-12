## Entry 360 — M2 (WS-2002-FORMAT): `>>DEFINE` / `>>IF` / `>>ELSE` / `>>END-IF` conditional compilation

The third and richest `>>` directive piece of WS-2002-FORMAT (after `*>` 358 and `>>SOURCE FORMAT` 359):
COBOL-2002 conditional compilation (ISO §7.3.11 DEFINE, §7.3.16 IF). New self-contained text-manipulation pass
`ConditionalCompilationProcessor`, wired into `Compilation.Preprocess` right after `NormalizeToFreeForm` and
**before** COPY expansion — so an `>>IF` can include/omit COPY statements in its branches (§7.3.16 GR1).

Implemented from the spec grammar (and the D.28 worked examples):
- `>>DEFINE name [AS] {literal | OFF} [OVERRIDE]` — define/undefine a compilation variable to a numeric or
  alphanumeric literal (GR15: a lone numeric literal is a literal, not an arithmetic expression). A DEFINE inside
  an omitted branch has no effect.
- `>>IF cce` / `>>ELSE` / `>>END-IF` with arbitrary nesting, via a frame stack tracking
  {ParentActive, Emitting, BranchTaken} so a nested IF inside an omitted branch stays omitted and ELSE fires only
  when the IF arm did not.
- A recursive-descent evaluator for the **constant-conditional-expression**: defined-condition
  (`name [IS] [NOT] DEFINED`), relation (`operand [IS] [NOT] {= <> < > <= >=} operand`, numeric compare when both
  sides numeric else ordinal string compare), combined with `AND` / `OR` / `NOT` and parentheses. A small
  tokenizer handles quoted literals (with doubled-quote escapes), signed/decimal numbers, two-char relops, and
  user-defined words.

Directive lines and omitted lines are **blanked** (not deleted) so downstream source-line numbers stay aligned
for diagnostics. **Exact no-op** for any source with no `>>` lines — verified: a directive-free file is
reproduced byte-for-byte, so all 364 NIST baselines are untouched.

Four integration tests mirror the spec examples: defined-condition + `>>ELSE` (`DEBUG-ON`); relational `AND`
(`LVL IS > 10 AND LVL IS < 20` → `MID`); compound `(N=1 AND SYS="type A") OR (N=2 AND SYS="type Q")` with parens
and alphanumeric compare → `>>ELSE` (`NO`); `>>DEFINE A AS OFF` + nested IF inside `>>ELSE` (`UNDEF`).

Deferred (documented follow-ups): `>>EVALUATE`/`>>WHEN` multi-branch; arithmetic/boolean expression operands and
the `PARAMETER` (environment) source in DEFINE; and conditional-compilation directives located inside copied
library text (this pass runs before COPY expansion).

Guard ALL GREEN: **1047 unit / 443 integration / 364 NIST**, 0 regressions.

