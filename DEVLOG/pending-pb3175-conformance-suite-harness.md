## PB3175 — the conformance suite's harness (slice S1b): run.py, adapters, profiles and baselines for WiseOwl COBOL and GnuCOBOL

kb/Work PB3175, design `docs/rearchitecture/DESIGN-conformance-suite.md` §5 and §13 S1b (approved by the owner
2026-10-14). Wave 1104 C4, on PB3174's branch, so PB3172, PB3173 and PB3174 land with it: the harness runs the
manifests the S1a generator writes.

**The runner.** `conformance-suite/harness/run.py`, Python 3 standard library only, executes
`suites/<edition>/manifest.json` against a compiler described by `adapters/<compiler>.json` (compile, run and
flag-mode command templates, the edition map, the diagnostic-line pattern, the file-name limit, crash exit codes,
the ASSIGN mapping, an optional Python hook) under the claims of `profiles/<compiler>.json` (editions, Annex A.4
modules and A.3 elements each with its evidence, A.1 values, extra reserved words per edition). Each test runs in a
fresh working directory with its declared files, stdin closed and a hang guard on every process. The verdicts are
design §5's: PASS; FAIL (output differs, did not translate, did not run, no diagnostic at a flagged test's marked
line); NOT APPLICABLE (edition, module or element not claimed, an extra reserved word used as a user-defined word,
or a flagged test's control that did not compile); NO VERDICT (a crash, the hang guard, a harness failure), counted
on its own. A program's exit status is not judged. Results: `results/<compiler>/results.json` (totals per edition
and per clause, every reason, the CHANGES) and `junit.xml` (git-ignored). `--record-baseline` writes
`baselines/<compiler>.json`; a run is RED only on a verdict worse than its recorded one, or, with no baseline, on
any FAIL or NO VERDICT. The output normalization is `CutRunner.Normalize` restated (the .NET line ends, trailing
spaces, trailing line ends), for S1c's shared-fixture drift test.

**The formats.** `harness/config.py` is the one loader and validator of adapters, profiles and baselines, and holds
the hook interface (`Step`, `StepContext`, so a hook builds the class the runner checks). `run.py` checks shape
only (a released suite carries no inventory); the generator's audit (`generate.py --check`, already a gate audit
and a CI `audits` step) now also refuses a malformed adapter, a profile claiming a module the standard lacks or
stating no value of an enumerated A.1 item, and a baseline of no adapter or naming no test of its suite.

**Re-probed on this branch.** WiseOwl (`--set cobol=<this build's cobol.exe>`): the seed PASSes 22 of 22 at
85/2002/2014/2023. GnuCOBOL 3.2.0 under WSL: 14 PASS, 6 NOT APPLICABLE (no 2023 dialect), 2 FAIL —
`read-previous-after-open-indexed` at -std=cobol2002 and -std=cobol2014 prints `P1=ATEND|10`, the 2023 at-end rule
(ISO §14.9.30.4 GR 21 d) 3.), where Annex E.2 item 22 records that 2002 and 2014 retrieved the first record: a real
GnuCOBOL divergence, recorded in `baselines/gnucobol.json`. Measured for the GnuCOBOL adapter and profile: cobc
refuses a source base name over 31 characters; format 3 of the RECORD clause wrote a 2-character record as 6 bytes
(variable, A.1 item 148); the TURN seed printed LOCATION AVAILABLE (item 204); a SCREEN SECTION and a Report Writer
RD translate at -std=cobol2002/2014, the LOCK MODE / SHARING clauses do not; and the extra reserved words are cobc's
`--list-reserved` per dialect less `reserved-words.json`'s (3, 60 and 82).

**One design correction, found by the first GnuCOBOL run.** The manifest entry's word list first held every word of
the program, so the READ PREVIOUS seed was NOT APPLICABLE on GnuCOBOL: -std=cobol2002 reserves PREVIOUS, which ISO
§8.10 makes a context-sensitive word, and `READ ... PREVIOUS` is its keyword use, which translates. ISO §4.2.10 3)
permits added reserved words because they "may prevent translation of some conforming compilation groups": only a
use as a user-defined word is stopped. The generator now writes `words` as the program's words that are none of the
standard's (the edition's reserved words; ISO §8.10-8.12's context-sensitive words, function names and directive
words, read from the transcription by `standard_keywords`) together with every name the program defines
(`cobol_source.defined_names`: data, condition, file, program, paragraph and section names) that its edition does
not reserve; and `clause`, the folder the results total by. Written into design §5 with the other two corrections:
profiles carry evidence, and the generator's audit (not a second audit) checks the harness configuration.

**WiseOwl's profile** claims the A.4 modules and A.3 elements `docs/CONFORMANCE.md` §5 and §2 dispose Claimed or
Partial (§5's preamble: the conformance summary claims what is Claimed or Partial there); the new
`ConformanceSuiteProfileDriftTests` holds the file to those tables through their one parsers
(`AnnexA3RegisterDriftTests.Register`, `ReadmeDeclinedSummaryDriftTests.AnnexA4Register`, made internal for it).

**Found and fixed on the way (I9).** `cobol` exited 70, "internal software error", for a source the binding phase
refuses (COBOLNET1675 on the flagged seed, an edition diagnostic): `RunCompile`'s sysexits switch had no BindError
arm and its `_` default was 70, so a wrong program read as a compiler failure. `Program.ExitCode` names every outcome
(BindError 65, like a parse error; an unnamed one throws) and `CliParserTests` pins each.

**Tests.** `run.py --self-test` 58 arms (Windows and WSL), `generate.py --self-test` 75 arms (the words, the defined
names, the standard's keywords, the configuration refusals), `ConformanceSuiteProfileDriftTests`, two `CliParserTests`.
No inventory row: test infrastructure (`closes_rows: []`, its reason filed). PB3176 (S1c) unblocked.

**Gate:** see the report.
