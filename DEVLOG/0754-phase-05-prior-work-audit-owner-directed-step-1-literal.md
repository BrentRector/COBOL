## Entry 754 — 2026-07-10 22:44 PDT — PHASE 05 prior-work audit (owner-directed): Step-1 literal sweep was INCOMPLETE — 3 residual `"`-only guards fixed (2 real miscompiles)

Per the owner standing rule ([[feedback_no_workarounds_root_cause]] — after finishing work, retroactively audit the
prior/related work for similar shortcuts), a 4-auditor workflow (`wf_0c13be5f-13a`) audited PHASE-05 Steps 1–4. Verdict:
Steps 2 & 3 CLEAN; Step 1's pattern-sweep was INCOMPLETE (it fixed the six sites DESIGN §2.8 named but not the PATTERN,
violating feedback_scan_all_similar); Step 4's offset deferral is legitimate SEQUENCING but its "verified by goldens"
net had no golden for the actual bug shape.

**Step-1 sweep completion (fixed):**
- **[HIGH, real miscompile]** `CSharpEmitter.ReportWriter.cs:98` (`RwValueOperand`) guarded on `raw[0]=='"'` — a
  Report-Writer `VALUE 'x'` (apostrophe) fell through to `BoundNumericLiteral` (silent miscompile). → `CobolLiteral.IsStringLiteral`.
- **[MED, real miscompile]** `DataBinder.Switches.cs:384` (`LiteralChars`) guarded on `text[0]=='"'` + an inline
  `Replace("\"\"","\"")` decoder — a SPECIAL-NAMES `CLASS x IS '0' THRU '9'` (apostrophe) populated the class with
  APOSTROPHE characters, not the digit range (silent miscompile of the CLASS clause). → `CobolLiteral.IsStringLiteral`
  + `CobolLiteral.Decode`. Locked by a new golden `tests/conformance/85/apos_class.cob` (WS='123'→ALLDIGIT, 'A2C'→NO,
  CLI-verified).
- **[LOW, singular-pattern]** `StatementBinder.Intrinsics.cs:889` (`DecodeSubString`) was a 4th duplicate decoder body
  (behaviorally correct — both delimiters) → now a one-line delegate to `CobolLiteral.Decode` (one decoder;
  [[feedback_singular_pattern]]). The frozen SUBSCRIPT token path itself is still deleted at PHASE 15.

**Deferred-with-a-real-net (docs updated):** the Report-Writer apostrophe `VALUE` golden folds into the planned Step-13
`ApostropheValueDifferentialTests` (fix already landed); the `KeyedAreaOffset` `ImageWidth`-basis divergence golden (an
INDEXED ALTERNATE KEY after a wider REDEFINES) is now MANDATED as the first, failing-first action of Step 8 (the
offset fold-in) so the deferral's safety net is real, not hollow.

**Battery:** conformance **3158** (+1 apos_class) · unit 258 · characterization 32 · guard 353 MATCH (greenfield-only).

**NEXT (owner-directed): a similar workaround/hack audit across the completed rearchitecture PHASES 01–04.**

