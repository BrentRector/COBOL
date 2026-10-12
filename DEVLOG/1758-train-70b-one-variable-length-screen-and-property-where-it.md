## Entry 1758 — 2026-09-28 13:19 PDT — Train 70b: one variable-length screen and PROPERTY where it belongs, one integer-literal reader, checked position reads with an ODO-aware table SORT, and a derived implementer gate filter

Three clusters in one landing, in the order U, I, C. None dropped. Group U is wave 68's group U: its agent died on an API error, and wave 70 finished it. Group C's agent hung after committing and gating (PB1704) and wrote no report, so its landing was verified from its commits, its notes' Landed sections and its scratch gate log. Group U's same-file successor, U2, is being finished separately and lands in train 70c.

**U: PB1088, PB1213, PB1273, PB1284, PB1579.** Wave 68 wrote the first four and gated none of them. The wave-70 finisher merged them onto train 69 and ran their first gate, which found two faults. PB1273's new SR1 refused the version matrix's own `property-clause-2002` sample, which wrote PROPERTY in a program; the sample moved into an instance definition. And a code-location still pointed at the folded `Sr17Shape`; batch `batch-PB1213-sr17-resite.json` re-sited it.
- PB1088: a contained program now ORs its container's WITH DEBUGGING MODE into its own (§12.3.5.4 GR1). The old assignment compiled a contained USE FOR DEBUGGING section as comment lines.
- PB1213: `VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup` is now the one screen for four rules: BASED SR2 (COBOLNET2560), CONSTANT SR12, REDEFINES SR17 and FILE STATUS SR3. CONSTANT LENGTH OF reads the one LENGTH fold, `ItemLength.Positions`.
- PB1273: the PROPERTY clause is recorded on its item. SR1 is screened in the binder (COBOLNET2563); SR2, SR3, SR5 and SR6 are checked per carrying item.
- PB1284: a RENAMES range's content is screened in declaration order (SR8, COBOLNET2561), and its bit area must be whole bytes (SR10, COBOLNET2562).
- PB1579: every COBOL integer-literal text now goes through one reader, `IntegerOperandRules.TryHostValue`, with one COBOLNET2427 sentence. An over-limit integer constant-name OCCURS bound and a CONTROL ref-mod literal now get COBOLNET2427; the ref-mod case used to die "Out of memory." at run time. Format 2 VALUE diagnostics quote the written subscript. CURRENCY SIGN asks the grammar for the literal's class. The semgrep rule was widened.
- Goldens: five positives and 14 negatives, all run by name. The implementer's gate: whole Conformance 9,294, Unit 29,695, Characterization 33.
- Leads filed: PB1705 (debugging lines at --std 85 are always comments), PB1706 (a CONTROL ref-mod literal past the item compiles clean and breaks at the wrong times; §8.4.3.3.4 5) b)/c)), and PB1707 (an unchecked huge ref-mod length crashes the run with "Out of memory.").

**I: PB1683.** The implementer's gate filter is now derived from a recorded per-test impact map instead of guessed from test names.
- `record_impact_map.py` records the map. It builds a detached worktree, where Cecil probes every method and compiled COBOL children are attributed back to their tests; a recording takes 18.7 min.
- `impacted_tests.py` looks the map up at method level. When it cannot bound a change, it falls back to the whole assembly and prints the reason. `ImpactedTestsDriftTests` guards that fallback.
- The finding matters as much as the tool. Replaying trains 65–69 against the map at `dbea12428`, every cluster's derived filter is the whole Conformance assembly. The main cause is the static constructors of the diagnostic and construct tables.
- The wiring (briefs, MANDATORY-PRACTICES I1/I2, the gate skill) therefore makes each implementer run one whole-Conformance leg. That trade is the owner decision PB1708. PB1709 records that no step records a map per main commit yet, and PB1710 that 172 child processes wrote no hits file.
- Merge: `check_practices.py` keeps both main's STATUS-AT / `status_delta.py` patterns (PB1698) and this cluster's `impacted_tests.py --base`.

**C: PB1117, PB1174, PB1268.**
- PB1117: a numeric position operand (subscript, ref-mod bound, ODO count, RECORD VARYING DEPENDING, report DEPENDING, SEARCH bound) reads through its own profile. `CobolTable.Occ(x, _P_n)` and `CobolString.RefModPosition(x, _P_n)` do the reads; `PlaceRenderer.CountRead` is the one codegen count read. The image arm is the checked §14.6.13.2 rule 2 read, so EC-DATA-INCOMPATIBLE is now reachable for a subscript. Two sibling defects were fixed: a signed image's sign was dropped, and a trailing-P item was read as its storage.
- PB1174: the table SORT sorts only the current occurrences (§14.9.40.4 GR20), through one `CurrentOccurrenceCount` that `table(ALL)` shares. A dynamic-capacity table is no longer refused as data-name-2 (§14.9.40.3 SR13).
- PB1268: EC-BOUND-ODO is now raised at an element reference through an ODO level (§13.18.38.4 GR7), but only where a >>TURN can enable it. The dynamic-capacity accessors now raise EC-BOUND-SUBSCRIPT the way the fixed-table accessor does.
- Goldens: five positives and three negatives. The implementer's gate2: Conformance 4,291 (with the corpus and NIST legs), Unit 29,694. PB1069, the group's fourth note, was not done and stays open.

**The train.**
- Merge: the manifests were unioned as whole elements, and the counts check (negative 1,737 + 14 + 3 = 1,754; 2002 557 + 3 = 560). The inventory was taken from main, and the ten verdict batches were re-applied with `record_verdicts.py`. The changed rows are identical to each branch's.
- Gate, whole assemblies at Normal priority: Conformance 9,317/9,317, Unit 29,734/29,734, Characterization 33/33, and the legacy Integration 503 (1 skipped). Verdict: `=== WAVE-LOCAL GATE: GREEN (filter FullyQualifiedName~.) ===`.
- Semgrep did not rise; raw-diagnostic-code literals went down, 301 → 300.
- Audits: code citations 0, doc citations 0 misfiled / 0 elided, evidence supersession 0 unmarked, witness loss 0 unexcused. `drift_rules --check` is current (227), `check_practices` GREEN, and `work.py check` passes.
- Review over `origin/main...HEAD`: 0 correctness findings, 0 clusters dropped, and eight report citations re-checked OK with `cite.py`.
- **GAP 883 → 866.** No new diagnostic codes. Lead ids PB1705–PB1710 were used and PB1711 is returned.

