## PB3177: the conformance suite's slice S2, the corpus moves into the suite

kb/Work PB3177, slice S2 of the conformance-suite campaign (docs/rearchitecture/DESIGN-conformance-suite.md §6 step 1,
§13 S2). Built on the branch of its predecessors S0b-S1c (PB3173-PB3176), which land through it.

**The converter.** A one-time, incremental converter (on the branch for review, deleted with its sweep and map
before the landing, design §13 S2) (it converts whatever lies
under `tests/conformance/`, so the re-run after the drain converts exactly what main added), moved the 5,219 programs
of the former corpus (2,468 positive, 2,751 negative) into 5,110 tests of `cases/<clause>/` and 109 of
`wiseowl/cases/<clause>/` (documented non-support, a WiseOwl compile option, a library folder, free form at 85), with
`rules` from the inventory rows that cite each one (a row whose band lacks the program's edition is left out, 24
programs), `accept` its old directory's edition, `source-format` from the `--source-format auto` detector the old
runner compiled under (85's fixed-form programs with long comment lines are written fixed), `files` from its COPY
statements and the candidates its library search passes over, and `portable: unverified`. Its 2,752 refusals went to
`wiseowl/diagnostics.json`, two compile options to `wiseowl/options.json`; 31 base names two programs shared took an
edition suffix. Every `conformance:` citation of the inventory, and (through the sweep and the converter's map)
those of 357 other files (the moved tests' own comments included), now names the suite test.

**The runner.** `ConformanceSuiteTestsBase` (24 partitions, `ConformancePartitioningDriftTests`) replaced
`CorpusRunnerTests`; the wiseowl manifest's reader moved to `tests/_shared/SuiteManifest.cs` so the Unit tests read the
generator's manifest instead of the old directories; `TestRepo.SuiteCase` finds a test by its id; `ArchOracle` enrolls
the suite's rows; `StageSource` stages companions as bytes, in library folders; `CorpusRunFileDriftTests` went, its
rule being the generator's refusal of a file no test claims.

**The generator's corrections** (each a self-test arm): a library text read by its byte-order mark; SOURCE FORMAT
directives followed (ISO 7.3.24.3 GR 1); no COPY read in a directive's text (7.3.19.4 GR 1); a refused-only test may
COPY an unavailable text (7.2.3.4 GR 2); header lists continued on indented lines (ISO 6.1 3) a)'s 255 positions); a
lone CR kept a character (DOC-A.1-156); WiseOwl library folders, options and free form at 85 in the wiseowl tree; annex
clause folders; and the refusal of a file under the removed `tests/conformance/`.

**Neutrality.** The suite runner over the moved corpus: 10,571 of 10,571 rows pass, every former positive at its
former edition and every former negative refused with its former diagnostic. The header moves the code lines, so the
expectations that quote a line moved with them (13 diagnostics' `(line,column)`, 6 EXCEPTION-LOCATION outputs, each
verified by running the program); the 14 compile-only programs carry their recorded output.

**The consumers (wave 1107 B, the split's continuation).** Five mechanisms were still on the old layout:

- *The witness-loss audit read 7,920 re-spelled references as lost.* A witness is the test, not its spelling:
  `inventory_schema.witness_losses` now excuses a lost test-ref whose new spelling the row cites, and
  `audit_witness_loss.respellings` reads which file became which from git's rename record across the change (each
  side spelled by its own tree's schema), so no map has to outlive the move and the history walk stays right; a
  clause pass that re-files a test is covered the same way. Six self-test arms. `--check`: 0 unexcused, 7,920
  re-spelled. The 59 rows that cited the deleted `CorpusRunnerTests.EnabledNegativeCase_RejectsWithItsDiagnostic`
  (a reference that resolved to no file) were moved by a record_verdicts batch to
  `ConformanceSuiteTests.WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic`, the retirements marked.
- *The gate could not order a changed suite test into leg 1.* `impacted_tests` looked for manifests and edition
  directories; `suite_tests` now maps a changed program, expected output or header-declared file to its test id (the
  generator's one header reader), `wiseowl/diagnostics.json` and `options.json` by changed entry, and `gate_plan`
  matches the runner's `id:` argument. CI's two shard filters named `CorpusRunnerTests`, which matched nothing, so the
  whole suite would have fallen into the `rest` shard: they name `ConformanceSuiteTests`.
- *The citation audits had silently lost the suite.* `citation_corpus.declaration_files` still globbed the deleted
  tree, so 0 of 5,238 suite programs reached the construct-agreement check (4,185 files scanned for phantoms instead of
  9,411); and once they did, every test's metadata block would have been read as its comment's head, which disables
  the HEADER check exactly as the corpus's `reject-at:` line once did. The audit now reads past the block
  (`without_metadata`); 0 findings.
- *The ledger and the probe measured a deleted directory.* `gen_ledger.measure_suite` counts the tests, those accepted
  at each edition and WiseOwl's refusals through the generator's reader; `session-probe.ps1` counts the same.
- *Writers and guides named the old layout.* `normalize_batch` spells a path through `Schema.ref_for_path` instead of
  its own copy of the template; the `golden_registered.py` hook, which had no manifest left to check, echoes the
  generator's refusal at the write of a file under the removed tree; the skills (`land-a-fix`, `new-construct`), the
  implementer briefs, the report template, MANDATORY-PRACTICES I7, the lane-1 draft and validate workflows and their
  inputs, README, CONTRIBUTING, the PR template, `.gitignore`, `VERSION_TEST_MATRIX_DESIGN.md` §5, `CONFORMANCE.md`
  (DOC-A.1-158: a suite test states its reference format and is never compiled `auto`), the plan, the design and the
  open notes' `area`. The first sweep's pattern refused a path followed by a sentence-ending period; fixed and re-run.

**Self-review.** One mechanism per job: the re-spelling is one predicate in the one definition of a loss, read by
both the writer and the auditor; the ref/path inverse lives once, on the schema. Silent failure: the shard filters,
the citation corpus and the ledger each pointed at nothing without a red, which is what this change corrects;
`suite_tests` returns None (every test tier 1, the reason printed) for a file no test claims.

**Left for the owner.** The `golden_registered.py` hook stays wired in `.claude/settings.json` (a configuration change
an agent does not make); it now only echoes the refusal.

**Gate.** Two reds on the way, each fixed at its root: the LINE ENDINGS writers audit (main's PB3176 widened it to
the suite harness, and the move's own generator self-test arm wrote without `newline=`), and
`HostWitnessTraitDriftTests` (`drift_rules.py` knew neither `TestRepo.Suite` nor a `conformance-suite/` literal, so
`IntrinsicModeFlagCoverageDriftTests`, which reads every suite test, stopped being a tree-wide sweep; drift_rules now
sees the suite). Then run 20261017T112021Z-9beab1: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`
— Conformance 13,878/17,265 (10,568 suite rows among them, every one passed: the neutrality proof again, on the merged
tree), Unit 33,501/33,923, Characterization 5/36; leg 2 is CI's. LINUX GATE: the lander's (batched gating).

**Review round 1 (wave 1107 B finisher).** Three findings, each answered at its root.
- *R1-1: the converter was deleted before main was quiet.* It was, and main was not: PB3261 (PR 179) and then PB780
  (PR 181) each landed tests under `tests/conformance/` after the move's base, and git's directory-rename detection
  put them in `cases/_unassigned/` without the metadata header every suite test carries, and the inventory still cited
  them by their old spelling. The
  converter, its sweep and its map are restored unchanged and re-run over each merge: PB3261's 2 tests (one under
  `cases/14.9.49-USE/`, one uncited under `_unassigned/`) and PB780's 11 (nine under `cases/13.10-Constant/`, two
  uncited negatives under `_unassigned/`; 7 refusals). The inventory's conflicted rows take main's verdicts and are
  re-spelled through the map. Each delta is proved neutral by the suite runner over its ids (2/2 and 23/23 rows), and
  PB780's new `ConstantNameLiteralPositionDriftTests`, which read the removed tree's manifests, now reads the suite's
  accepted tests through `SuiteManifest` (3/3). The tools stay on the branch until the lander's final merge of main
  and are deleted by the branch's last commit; the design's As built and the note say so.
- *R1-2: the sweep moved paths but not line numbers.* The header the move added above every program shifted each
  `file.cob:N` pointer by 5-9 lines. Every pointer outside the history was re-pointed by its file's line delta (24, each
  verified by comparing the old line's text with the new one), and the one that named the removed `reject-at` line now
  names its `wiseowl/diagnostics.json` entry. Sibling: the first sweep had also rewritten six FROZEN evidence files,
  whose record is never edited; they are restored (the supersession marker the audit asks for stays).
- *R1-3: a comment named a test class that does not exist.* `ConformanceSuiteTests.cs` names
  `ConformancePartitioningDriftTests` and what it pins.
- *Re-gate.* One red on the way: LINE ENDINGS (this entry's appended paragraph was written CRLF by the shell; the index
  held LF; `line_endings.py --fix`). Then run 20261017T115951Z-adfc77: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
  PB2515): GREEN` (Conformance 13,924/17,290 with the 25 moved PB3261/PB780 rows passed, Unit 33,523/33,928,
  Characterization 5/36). The planner left `ConstantNameLiteralPositionDriftTests.EveryWordAdmittingLiteralSlot…` to leg
  2; it passed when run alone on the same build (3/3). LINUX GATE: the lander's (batched gating).
