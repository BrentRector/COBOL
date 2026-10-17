## PB3177: the conformance suite's slice S2, the corpus moves into the suite

kb/Work PB3177, slice S2 of the conformance-suite campaign (docs/rearchitecture/DESIGN-conformance-suite.md §6 step 1,
§13 S2). Built on the branch of its predecessors S0b-S1c (PB3173-PB3176), which land through it.

**The converter.** `conformance-suite/harness/convert_s2.py`, one-time and incremental (it converts whatever lies
under `tests/conformance/`, so the re-run after the drain converts exactly what main added), moved the 5,219 programs
of the former corpus (2,468 positive, 2,751 negative) into 5,110 tests of `cases/<clause>/` and 109 of
`wiseowl/cases/<clause>/` (documented non-support, a WiseOwl compile option, a library folder, free form at 85), with
`rules` from the inventory rows that cite each one (a row whose band lacks the program's edition is left out, 24
programs), `accept` its old directory's edition, `source-format` from the `--source-format auto` detector the old
runner compiled under (85's fixed-form programs with long comment lines are written fixed), `files` from its COPY
statements and the candidates its library search passes over, and `portable: unverified`. Its 2,752 refusals went to
`wiseowl/diagnostics.json`, two compile options to `wiseowl/options.json`; 31 base names two programs shared took an
edition suffix. Every `conformance:` citation of the inventory, and (through `sweep_s2.py` and the converter's map)
those of 155 other files, now names the suite test.

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

**Gate:** see the report (`E:\COBOL-coord\scratch\reports\w1104c6-PB3177-report.md`); the slice is split, the rest is
the finisher's.
