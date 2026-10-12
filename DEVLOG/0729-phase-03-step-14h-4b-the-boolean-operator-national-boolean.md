## Entry 729 — 2026-07-09 20:33 PDT — PHASE-03 Step 14h.4b: the boolean-operator + national/boolean literal gates → parse-arm (SYNTACTIC migration COMPLETE)

The delicate final gate-relocation commit — it moves the last five bind-time SYNTACTIC Checks and thereby leaves the
binder with ZERO statement/expression-level edition Checks. Two hazards the recon (wf_be806171-b25) flagged, both
handled:

- **Boolean OPERATORS (BooleanOperators2002)** fire from two sites — `BindPrimaryBoolean` (a condition) and
  `BindComputeBoolean` (COMPUTE F2). Detected in the parse-arm at the `primaryCondition` / `computeStatement`
  ALTITUDE with a whole-subtree `HasBoolOp` scan (`VisitPrimaryCondition` / `VisitComputeStatement`), NEVER per
  `booleanExpression` node — the tiers nest via parentheses and the relation form, so a per-node gate would
  over-count. `HasBoolOp` is mirrored into the parse-arm (a pure B-op-terminal predicate; the binder keeps its own
  for channel routing).
- **National/boolean LITERALS (NationalData2002 / BooleanData2002)** fire from `NationalLiteralOperand` /
  `BooleanLiteralOperand` / the boolean-expression operand path — all PROCEDURE-division statement operands. The
  parse-arm `VisitNonNumericLiteral` is scoped to a `StatementContext` ancestor (`InStatement`), so a DATA-division
  VALUE literal is left to the data/PIC gate (its item's national/boolean USAGE, Step 14g); firing here too would
  double the below-2002 diagnostic. Firing count matches (one Check per statement literal occurrence).

Because the gates are silent at/above their edition and only diagnose below it (where the program is rejected
anyway), exact below-edition COUNT is immaterial to the contains-based tests; the requirement — fire ≥1 below-edition,
0 at/above, and no NEW data-division firing — holds. Characterization snapshots unchanged (the 3 snapshot negatives
are data-attribute gates, untouched by 14h) — no re-baseline.

**Step 14h SYNTACTIC migration is now COMPLETE (14h.1–14h.4b).** The `VersionConformancePass` parse-arm owns every
syntactic introduction/removal/phrase/expression/literal gate + the §8.9 reserved-word funnel; the bound-arm owns
only the genuinely-semantic gates (MOVE-category + the file-org/USAGE/pointer-conditioned ones). Remaining for the
binder: only the DATA/PICTURE/OO gates (→ Step 14g). UDF stays a documented bind-time exception.

Battery: greenfield conformance 3114 · unit 227 · characterization 32 · INV-1-strong @ 2023 permissive 349/349 ·
FULL legacy guard 353 MATCH · Release warnings-as-errors clean.

