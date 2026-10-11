## Entry 942 — 2026-07-21 09:30 PDT — Wave D (cont.): compile-time expression evaluator FOUNDATION — shared lexical utilities relocated + the §8.8.2 rule-7b boolean precedence resolver (ledger C2)

Started the go-forward fix for review-ledger **C2** (the MAJOR silent-wrong-value defect: `>>DEFINE`/
`>>IF`/`>>EVALUATE` directive operands and CONSTANT-entry arithmetic silently mis-bind multi-token
expressions — `>>DEFINE X AS 1 + 2` binds `X = 1`). The fix is ONE shared compile-time expression evaluator
reachable by both the frontend conditional-compilation stage and the CONSTANT-entry binder. This entry lands
the FOUNDATION; the evaluator, the ANTLR fragment/cce grammar, and the frontend rewire follow. Design SSOT:
`docs/rearchitecture/DESIGN-compile-time-expressions.md`.

**Landed (all guards green — see below):**
- **`CobolLiteral` relocated** `Cobol.Net.Compiler/Common` → `Cobol.Net.Frontend/Common` (the layering forces the
  shared evaluator into Frontend, so its one string-literal codec must live where both layers reach it). Pure
  rename; namespace `CobolNet.Common` unchanged, so all 30 Compiler `using`s still resolve.
- **`NumericLiteral.Normalize` extracted** to `Cobol.Net.Frontend/Common` as the one §12.3.7 GR14a normalizer;
  `DataBinder.NormalizeNumericLiteral` delegates and keeps its COBOLNET0895 diagnostic — byte-identical (diff-
  verified). No second normalizer for the evaluator to drift from.
- **`BooleanExpressionResolver`** (`Cobol.Net.Frontend/Expressions`) — the one ISO §8.8.2 boolean precedence
  mechanism. A CFG cannot express rule 7b's context-inherited shift precedence, so the resolver flattens a
  `booleanExpression` and precedence-climbs (shunting-yard), generic over the combine ops so the SAME grouping
  will serve the compile-time bit-string fold AND the runtime COMPUTE-Format-2 bind. 13 unit tests prove the
  groupings (`A B-AND B B-SHIFT-L 2` → `(A B-AND B) B-SHIFT-L 2`, etc.).

**Guards:** legacy `guard.sh` `=== ALL GREEN ===`; greenfield Unit **379**/0 (incl. the new resolver tests),
Characterization **33**/0 byte-exact, Conformance **3783**/0. No compiler-output change (relocations neutral,
resolver not yet wired).

**Process, honestly (owner corrected three fundamentals — transparency).** My first design took the low-diff
path and hit exactly the anti-patterns the owner has been correcting: (1) it kept a hand-rolled constant-
conditional parser + bespoke operand tokenizer beside ANTLR — owner: "do not have multiple parsers; that is
ANTLR's job." Removed; all directive-expression syntax will be ANTLR fragment rules. (2) It queried whether the
fixed/free reference-format switch should use ANTLR lexer modes — it correctly does not (a pre-lex text pass, not
a second lexer). (3) It proposed to **defer** the §8.8.2 rule-7b shift-precedence as a "documented non-
conformance" — owner: "why yet again are we deferring correct per the spec implementation… you regularly prefer
to accumulate technical debt rather than do it correctly. That is sloppy." Correct. Worse, I had *assumed* the
runtime path silently mis-grouped; reading it showed it REJECTS the mixed form loudly with COBOLNET1569 — which
is itself debt (it refuses legal source). The resolver removes that reject and implements rule 7b correctly for
both paths. Two durable lessons recorded: resolve the STRUCTURAL singular-pattern first (don't anchor on existing
code's shape); never default to deferral/"documented non-conformance"/reject-legal-source — implement to spec
regardless of effort. A 4-lens adversarial design critique (run before coding) additionally caught the
`NormalizeNumericLiteral` coded-diagnostic side-effect, the GR5/GR3-truncation layering, and missing §7.3.8/§7.3.13
category rules — all folded into the design before implementation.

