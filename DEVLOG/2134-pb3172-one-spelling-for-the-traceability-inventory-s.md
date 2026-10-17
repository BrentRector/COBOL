## Entry 2134 — 2026-10-16 20:12 PDT — PB3172: one spelling for the traceability inventory's `editions` field (conformance suite S0a)

The first slice of the PB3160 conformance-suite campaign (DESIGN-conformance-suite.md §6 step 0, §13 S0a), wave
1104 group C.

**Re-probe.** At 1a9d0435f the inventory held 4,195 rows: 3,402 in the comma-separated ascending form, 645 spaced
(`85, 2002, 2014, 2023` ×247, `2002, 2014, 2023` ×334, …; 37 of them out of order as well, such as
`2002, 2014, 2023, 85` ×19) and 148 empty. No row named an unknown or a repeated edition. The note's 683 was the
2026-10-14 count.

**The rule, in one place.** The schema's `editions` entry is now `{values, separator}` (the ascending editions and
`,`), as `code-location` and `test-ref` already carry their separators. `inventory_schema.py` gains the one
validator, `Schema.editions_error` (empty, or exactly the spelling of the editions it names; a space, an out-of-order
list, a doubled separator, an unknown or a repeated edition is refused with the spelling to write), and the one writer,
`Schema.spell_editions`, which raises on an unknown or repeated edition rather than tidy a wrong claim away. The module
now handles `--self-test`, which `scripts/self_tests.py` discovers, and the self-test proves both against the real
schema entry.

**Writers and readers.** `record_verdicts.py` asks the validator of every record; its own `split(ed, ",")` reader is
gone, and its hint for a list-typed field now gives the value the validator accepts (the editions spelled, a witness
field joined on its own separator). `build_inventory.py` carries each row's `editions` through the writer, so a rebuild
on any merged tree respells a row recorded before this landing; a carried field it cannot spell stops the rebuild.
Running it was the migration: 645 rows respelled and nothing else in the file changed (GAP 61 → 61;
`audit_witness_loss.py --check` GREEN). `normalize_batch.py` respells an agent's batch (string or list) and logs each
change; `grammar_findings_to_batch.py` spells a finding's editions, treats an unknown edition as a problem, and reads
the catalog through `inventory_schema.load_catalog` instead of its own copy of the path. `derive_verdict_batch.py`
carries the row's own value, which the drift test holds to the rule.

**The C# twin.** `SpecTraceabilityInventoryDriftTests.EditionsError` evaluates the same rule from the same schema
entry. `EveryEditionsField_IsTheOneSpelling` (renamed from `EveryEditionsField_NamesOnlyKnownEditions`) holds the whole
inventory to it: red with 645 findings on the old inventory, green on the new one. The fabricated-inventory self-test
adds the spaced, out-of-order, doubled-separator and repeated cases and a passing control.

**Docs.** Design §2, §6 step 0 and §13 S0a describe the landed shape; the lane-1 input template's record shape names
the one spelling; `normalize_batch.py`'s docstring names `ADJUDICATED` instead of a stale field list that lacked
`derivation`. kb/Work PB3172 is landed (`closes_rows: []`: no rule's verdict or witness changes), and PB3173 and PB3174,
blocked only by it, are unblocked.

**Self-review.** One mechanism per job (one validator, one writer, the C# evaluator a twin of the Python one, as the
schema module's division of labour already states); no second copy of the rule (the separator and order are schema
data); no compatibility reader (the old spelling is refused everywhere; only the writers respell, and the rebuild is
how a pre-S0a row reaches the one form). Silent-failure lens: an unspellable value is never dropped (rebuild stops,
normaliser leaves it for the validator, converter reports a problem).

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — … Conformance 6,084/11,889 · Unit
33,334/33,884 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) …` (run 20261017T003313Z-12dddf);
`EveryEditionsField_IsTheOneSpelling` and `TheseChecks_ActuallyFail_OnAFabricatedInventory` Passed in leg 1, and
`inventory_schema.py --self-test` GREEN among the gate's self-tests. Linux gate: the lander's (batched gating).

