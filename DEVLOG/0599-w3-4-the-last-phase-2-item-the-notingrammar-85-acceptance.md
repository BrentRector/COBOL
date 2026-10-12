## Entry 599 — 2026-07-04 00:41 PDT — W3 ④ (the LAST Phase-2 item): the notInGrammar 85-acceptance set — RERUN, ENTER, USE FOR DEBUGGING, section segment-numbers GATED (VCR Table 7 rows 7.15–7.18)

**Phase 2 is CLOSED. The four obsolete-'85 elements that had NO grammar at all — generic parse errors at
EVERY edition, the G1 co-equal-diagnostic violation the roadmap tracked as W3 ④ — now parse UNGATED at all
editions (the STOP-literal house style: a `{is85()}?` predicate would be wrong, ≥2002 must produce 0902 with
edition naming, not a parse error), bind ACCEPTED-INERT at 85 per the X3.23-1985 rules, and gate
`EditionValidator` → `ConstructRegistry` at ≥2002. No ISO-2023 § exists for any of them (Annex E covers only
2014→2023) — the registry rows cite the §8.9 ABSENCE pinpoints (RERUN @10661–10662, ENTER @10459–10460,
DEBUG-* @10407–10408, SEGMENT @10681–10682).**

- **Grammar (shared frontend ⇒ FULL legacy guard):** 7 new lexer tokens — RERUN, ENTER, EVERY, CLOCK-UNITS,
  DEBUGGING, REFERENCES, PROCEDURES — each admitted at ALL THREE user-word sites (cobolWord +
  `_dataNameTokens` + `CheckedTokenTypes`; the XOR/DEVLOG-596 recipe — position-safe because their keyword
  occurrences parse through dedicated rules, never a name slot). New rules: `rerunClause` in ioControlClause
  (all four X3.23 EVERY forms: [END OF] REEL/UNIT OF f · n RECORDS [OF f] · n CLOCK-UNITS · condition-name),
  `enterStatement` in the statement dispatcher, the `USE FOR DEBUGGING ON? useDebugTarget+` fourth
  useStatement format (ALL PROCEDURES · ALL REFERENCES OF dataReference · bare dataReference — DB201A's
  multi-level OF qualification covered), and `SECTION integerLiteral?` on BOTH header rules
  (sectionDefinition + declarativeSection).
- **The ENTER operand lesson (caught by CLI probe, would have been a conforming-source false-reject):**
  `ENTER COBOL.` is the canonical '85 switch-back and 'COBOL' is an '85 §8.9 reserved word — with the
  operands as cobolWord the position-blind funnel 0901'd it. The operands are SYSTEM-names, so
  `enterOperand : IDENTIFIER | LINKAGE` sits deliberately OUTSIDE the funnel (LINKAGE admitted for the
  classic `ENTER LINKAGE.` idiom, which collides with the SECTION keyword).
- **The '85 debug-facility dual posture (the spec-complete inert rule, not a shortcut):** WITHOUT
  SOURCE-COMPUTER WITH DEBUGGING MODE, X3.23-1985 compiles a USE FOR DEBUGGING section **as if comment
  lines** — the binder skips it (`DeclCollectSection`, explicit DEBUGGING branch — the former silent
  `return null` fallthrough is now an explicit arm) AND the validator skips the section BODY
  (`VisitDeclarativeSection` visits only the USE so its ≥2002 0902 gate still fires) — **DB103M is the
  corpus witness: no switch + 95 DEBUG-register references, designed by NIST to run inert; it now COMPILES
  at 85** (previously the funnel 0901'd DEBUG-LINE inside the comment-treated section). WITH the switch the
  section is compiled but the implementor-defined object-time switch is permanently OFF (never triggered —
  DB301M/302M/305M compile; verified NEVER-SEEN suppressed at runtime). A DEBUG-* register reference UNDER
  the switch now diagnoses **0899 not-implemented naming the deferred facility** (DB101A), never the false
  "reserved word as user-defined word" 0901 — diagnose-correctly is co-equal. `_debuggingModeDeclared`
  resets per TOP-LEVEL programUnit so nested programs inherit the outer switch (the '85 rule);
  `DataBinder.DebuggingModeDeclared` is the binder-side twin (per-unit — nested inheritance is
  validator-side only, a documented residue).
- **Other inert bindings:** RERUN rides `BindIoControl`'s non-SAME skip (doc'd — the MULTIPLE FILE posture;
  a checkpoint hint has no program-visible effect, a null rerun facility is conforming); ENTER →
  `BoundNop`; segment-numbers are accessor-invisible to the collectors (number parsed and discarded;
  all-resident is conforming — the independent-segment ALTER-reversion rules deliberately NOT implemented,
  documented on VCR row 7.18: ALTER is itself gated 0810/0811 and the SG reversion programs are golden-less).
- **Coverage shipped in the same commit:** 4 registry rows + 4 constructs.json rows (drift-locked, ×7 matrix
  cells each) · 4 negative-corpus cases (rerun / enter / use-for-debugging / segment-numbers) · the new
  `Ansi85AcceptanceTests` (23 facts: inert RUN semantics for all four incl. the RERUN read-back and the
  three extra EVERY forms, the SQ206A-style SAME+RERUN one-period adjacency pin, ENTER two-operand
  adjacency, the DB residue compile witnesses, DB101A's 0899-not-0901, SG101A's integer-named-section shape
  `00 SECTION 00.`, and the per-word §8.9 freeing editions straight from ReservedWords.Table — RERUN/ENTER
  free ≥2002, DEBUGGING ≥2014, EVERY/CLOCK-UNITS/REFERENCES/PROCEDURES ≥2023, 0901 at every reserved
  edition below).
- **CLI-verified end-to-end** (all four × {85 strict run · 2002 strict · 2002 permissive run}): inert output
  correct at 85 (RERUN-OK / 7+ENTER-OK / 3+DEBUG-OK with the declarative suppressed / 5+SEG-OK), 0902
  errors with edition naming + citation at 2002 strict, 0902 warnings + intact 85 semantics at 2002
  permissive.
- **Docs:** VCR Table 7 rows 7.15–7.18 (with the documented leniencies) · VERSION_TEST_MATRIX_DESIGN W3-④
  as-built block · COBOLNET_VALIDATION_DESIGN §3 coverage list.

Battery: conformance **1453/1453** (+55) · unit 102/102 · INV-1-STRONG 349/349 byte-exact at
2023-permissive · sweep **439 OK / 20 SKIP85 / 0 BREAKS** (grew from 419 OK — twenty DB/SG/OBIC residue
programs now compile at 85, the predicted SKIP85→OK migration) · FULL legacy guard ALL GREEN (353 MATCH ·
1204 unit · 537 integration). **RESUME = roadmap Phase 3: the M2 OO port** (docs/COBOLNET_OO_DESIGN.md
carries the port map; the OO grammar grant is standing).

