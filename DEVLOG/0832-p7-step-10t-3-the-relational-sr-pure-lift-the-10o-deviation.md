## Entry 832 — 2026-07-11 22:06 PDT — P7 Step 10t/3: the relational-SR pure-lift — the 10o deviation-(b) discharged

**What (the FINAL-WIRING batch, sub-3 — discharging the deferred pure-lift).** `ConditionBinder.CheckedRelational`'s
edition-invariant SR band moved to `StatementValidation.CheckRelationalOperands(left, op, right)`: the
class-boolean comparability rules (§8.8.4.2.2 Format 2 — a boolean operand compares only with a boolean or
figurative ZERO, equality-only — COBOLNET0844) and the strongly-typed-group rules (§8.8.4.2.3 SR1 same-type /
SR4 non-orderable-leaf equality-only — StrongCompareMismatch / COBOLNET1535), plus the `ContainsNonOrderableLeaf`
helper. `CheckedRelational` is now `{ ctx.Validation.CheckRelationalOperands(…); return new BoundRelational(…); }`
— it stays THE one relation checkpoint (IF / EVALUATE pairings+ranges / PERFORM UNTIL / SEARCH WHEN / sole-operand
conditions all reach it). These checks were the GENUINELY-pure half of the 10o deferral: side-effect emission that
never gated the node, so `CheckRelationalOperands` returns void (the honest signature — no verdict to consume;
the convention's bool-returning Check* form is for callers that branch).

**The 10o deviation-(b) other half — the woven 1511 boolean-operand checks — stays in ConditionBinder, by
design.** `CheckBoolOperand`, `BindSoleOperandCondition`, and the boolean-relation arm emit COBOLNET1511 and
then RETURN a placeholder / gate the bind — they are control-flow-woven, not pure verdicts, which the
StatementValidation convention ("PURE checks only lift") explicitly excludes. Lifting them would split coupled
bind+diagnose logic across two files — a net regression against the singular-pattern doctrine. Recorded here and
in the phase doc as a resolved decision, not a pending deferral.

**Verify.** Sln Debug clean (0 warnings — the moved-out `DiagnosticCatalog`/`StrongTypeModel` refs still used by
ConditionBinder's other checks); 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

