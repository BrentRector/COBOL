## Entry 765 — 2026-07-11 06:34 PDT — P7 §6h: the SYSTEMATIC AUDIT completed (grep-classified every bound-node switch), the completeness-critical dispatches converted, validated against ISO — plus a spec-gap the byte-neutral lens hid (⚠ the "LENGTH gap" was a FALSE alarm — see Entry 766)

Ran the audit properly this time: grepped EVERY `switch`/switch-expression with a bound-node arm across the compiler
(11 files), classified each, converted the ones a spec-completeness argument demands, and — per the owner's correction
(**validate against ISO §, not against the prior implementation or a green corpus**) — grounded each keep/convert in a
citation rather than "byte-neutral vs the old code."

**Converted — the completeness-critical dispatches** (a miss is a genuine bug):
- TOTAL result-dispatches (every node must yield a result; a loud/error/`null` default = a forgotten arm): EmitStatement
  (6b), NumericRenderer/ConditionRenderer/OperandText/BooleanRenderer/IntrinsicRenderer (6d/6e/6f), ArgExpr, StoreKindOf
  (6c) — all on the generated exhaustive `IBound*Visitor`. `StoreKindOf`'s `None` for a DELETE key is ISO §8.4.3.9.4
  GR1 (a sending occurrence), not a mechanism artifact.
- STATEMENT WALKERS (recurse the tree; a missed container silently drops nested content): `UsageCollectionPass`,
  `VersionConformancePass.Recurse`, `ContainsNextSentence`, `AlterCollectFields`, `ContainsIntrinsic` — all now recurse
  via the generated `BoundStatementTree.StatementChildren` (the ONE drift-proof container source), deleting five
  hand-listed recursions that were synced by PROSE ("cross-checked against …"). Output-neutral where the old set already
  equalled StatementChildren; a total superset (conservative, spec-aligned) for `ContainsIntrinsic` (EC-ARGUMENT-FUNCTION
  §14.6.13) and — verified — corpus-neutral.

**Reasoned KEEPS — spec-grounded, NOT completeness holes** (an exhaustive visitor would be gratuitous / harmful):
- PARTIAL PREDICATES whose default is a MEANINGFUL value, correct for every unlisted leaf: `CountExpr`/`LinesExpr`
  (`_ => "1"`; the operand is a bind-restricted integer literal/identifier, §14.9.51/§8.9 — the `_` arm is unreachable),
  the category/width predicates (`IsNational`, `Gr3Width`, MoveFigurative category, the Boolean-length predicates —
  `false` is correct for a non-matching category by definition), `GateMove` figText (§14.9.25.3 SR5).
- SELECTIVE CLASSIFIERS: `VersionConformancePass.GateStatement` (only later-edition constructs gate — the gated set IS
  `constructs.json`, the SSOT; a statement absent from it is correctly un-gated), the I/O-name classifier.
- SPEC-STABLE emit-dispatches over tiny closed roots: the emitter's `PerformControl` switch (`default:` = the once form,
  ISO §14.9.28 GR1 — spec-correct) and `StoreSetTarget`/`AugmentSetTarget` (§14.9.39, Index/Place). 4/2 spec-defined
  leaves; a new leaf would require a spec change. `void`+closure, so the generic-return visitor would force a dummy `T`
  + a per-call state visitor — more complex for zero spec-correctness gain.
- PARTIAL error-defaulted dispatches where the default is a SPEC-APPROPRIATE diagnostic for an unsupported form, not a
  forgotten arm: the CALL BY-CONTENT argument switch (`default:` = unsupported CALL argument form, §14.9.6; the
  identifier form is handled before the switch as the reference/content carrier) and `BindLengthFold` (§15.50.4).
- INTENTIONAL PARSE-CONTEXT switches (dispatch ANTLR contexts, not bound nodes — no `Accept` exists): `BindStatementCore`,
  `BindCondition` (OPEN Q5, confirmed correct).

**Final grep (pasted, this time actually run) — no total-dispatch loud/error default remains:** the only surviving
`_ => Loud/throw/new BoundError` arms are (a) non-bound switches over the `Usage`/`PicCategory` enums + a function-name
string, (b) the parse-context `BindCondition`, and (c) the two partial error-defaulted dispatches above (CALL arg,
LENGTH arg) — each spec-appropriate. All five statement walkers confirmed on `StatementChildren`.

**⚠ SPEC GAP surfaced by validating LENGTH against ISO (not the corpus) — flagged, not silently kept:** `BindLengthFold`
folds only a *string* literal; a **numeric literal** argument falls to `_ => BoundExprError`. But §15.50.1 admits a
numeric argument and §15.50.4 returns a literal's character-position count, so `FUNCTION LENGTH(12345)` should fold to
`5`, not error. This is a genuine conformance gap (needs a spec-derived fold + a conformance test — separate from this
dispatch audit); it is NOT converted-and-preserved as "correct." Tracked for the feature backlog.

**Lesson closed:** the earlier "complete/grep-verified" claims (Entries 761/763) were premature because I trusted a
hand-authored consumer list and a green corpus. The audit is complete now because it is grep-classified end-to-end AND
each decision is tied to an ISO §, not to the prior implementation's behavior. Battery 3158 conf + 269 unit + 32 char,
green throughout.

