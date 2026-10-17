# The per-edition COBOL conformance suites

One tree of compiler-neutral tests, filed by ISO/IEC 1989:2023 clause, from which four suites are generated, one per
edition of standard COBOL (85, 2002, 2014, 2023). The design is
[`docs/rearchitecture/DESIGN-conformance-suite.md`](../docs/rearchitecture/DESIGN-conformance-suite.md) (kb/Work
PB3160); this folder is built slice by slice from its §13.

## The tree

| Path | What it holds |
|---|---|
| `cases/<clause>/` | The tests, filed by ISO clause (`14.9.8-COMPUTE/`), each `<id>.cob` with its expected outputs and the files it declares. `cases/_unassigned/` holds tests no rule cites yet; the suites leave them out |
| `wiseowl/cases/<clause>/` | WiseOwl COBOL's own regression tests, in the same format, never part of a suite |
| `wiseowl/diagnostics.json` | WiseOwl COBOL's refusals: per test, the editions at which it refuses the program and the code or text it reports |
| `implementor-defined.json` | The Annex A.1 items whose values the standard enumerates, and those values |
| `harness/generate.py` | The one reader of the test headers: refuses a malformed test (and a malformed adapter, profile or baseline), writes the suites, reports completeness |
| `harness/run.py` | The harness: runs the suites against a compiler through its adapter and reports a verdict per test |
| `harness/config.py` | The formats of the adapters, profiles and baselines, and their one loader |
| `harness/normalization-fixture.json` | The output normalization's one fixture, which both runners' normalizations are held to |
| `adapters/<compiler>.json` | How the harness drives a compiler: its compile, run and flag-mode commands, its edition map, how a diagnostic's line is read, file-name limits, crash codes |
| `profiles/<compiler>.json` | What a compiler claims: its editions, the Annex A.4 modules and A.3 elements it supports (with the evidence), its Annex A.1 values, the words it reserves beyond the standard |
| `baselines/<compiler>.json` | A compiler's recorded verdicts, against which a run reports its changes |
| `suites/` | Build output, never committed: `suites/<edition>/manifest.json`, `suites/wiseowl/manifest.json`, `completeness.json` |
| `results/` | Build output, never committed: `results/<compiler>/results.json` and `junit.xml` |

## A test

A test is one COBOL program whose metadata block opens the file (design §3):

```cobol
      *>--- conformance-case
      *> id: compute-multi-receiver-size-error
      *> rules: [GR-14.9.8.4-2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
```

Its expected stdout is `<id>.out`, `<id>.<edition>.out` where the standard's behaviour differs by edition, and
`<id>.<item>=<value>.out` for each value of an Annex A.1 item it names in `implementor-defined`. A test the standard
requires a compiler to diagnose carries `flag: {kind, at}`, names its `control` (the same program without the
violation) and marks the diagnosed line: `<<` opening the sequence number area in fixed form, a closing `*> <<` in free
form. Every field, every expectation and the portability lint (what a portable test may DISPLAY) are in design §3.

## Adding a test (the authoring guide)

**Every rule implemented or determination documented ships with at least one witness here, in the same commit as
the feature** (CLAUDE.md rule 3). The tests are the per-edition conformance evidence, WiseOwl COBOL's regression net,
and the `test-ref` witnesses the traceability inventory cites as `conformance:<tree>/<clause folder>/<id>`
(`tests/version-matrix/inventory-schema.json`); `docs/CONFORMANCE.md` holds the implementor-defined determinations
the tests pin.

- **Where.** A test of a rule a program can observe portably goes in `cases/<clause>-<Name>/`, the folder of its
  rules' clause (`cases/14.9.8-COMPUTE/`). A test of WiseOwl COBOL's own behaviour goes in
  `wiseowl/cases/<clause>-<Name>/`: documented non-support, an implementor-defined value the standard does not
  enumerate, a WiseOwl compile option, free form at 85 (WiseOwl's `--source-format free` extension), a library
  folder (`COPY X OF LIB`, whose location is the implementor's). The generator refuses a file under the removed
  `tests/conformance/`.
- **The program.** `<id>.cob`, opening with its metadata block (above; a long list continues on lines indented four
  spaces past `*>`, so a fixed-form block stays inside margin R and a free-form line inside its 255 positions), then a
  comment block naming the ISO clause, quoting the rule (each citation through `scripts/spec/cite.py --check`) and
  stating **why each leg can fail**. The expected values are DERIVED from the standard (or from a documented
  implementor determination), never copied from a run. `PROGRAM-ID`s are unique across both trees (.NET would serve a
  stale same-named assembly). UTF-8, no BOM, LF (`.gitattributes`); a library text may carry a UTF-16 or UTF-32 mark.
- **The expectation.** An accepted test's `<id>.out` is its exact standard output, compared through
  `CutRunner.Normalize` / `run.py`'s `normalize()` (LF, per-line trailing-space trim, no trailing newline); a test that
  only proves translation ends in `STOP RUN` with an empty `.out`.
- **A program WiseOwl must refuse** names no `accept` edition and has an entry in `wiseowl/diagnostics.json`: the
  editions WiseOwl refuses it at (`refuse-at`) and the code or message substring it reports (`expect`).
  `ConformanceSuiteTestsBase.WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic` compiles it at each and requires the
  front end or the binder to refuse it with that text. A clause pass (design §6 step 2) turns such a test into a
  flagged test with a control.
- **A WiseOwl compile option** a WiseOwl test needs is its entry in `wiseowl/options.json` (`sign-encoding`:
  `ibm` · `ascii`; `flag-extensions`: `on` · `off`); the generator refuses one for a test of `cases/`.
- **Migrated tests.** The programs of the former `tests/conformance/` corpus (design §6 step 1, kb/Work PB3177)
  carry `portable: unverified`, are accepted at the edition their old directory named, and wait for their clause
  pass; one no inventory row cited is in `cases/_unassigned/`.

## Running the generator

```
python conformance-suite/harness/generate.py            # refuse, or write suites/ and print the completeness report
python conformance-suite/harness/generate.py --out DIR  # the same, written to DIR instead of suites/ (DIR new, empty or a previous run's output)
python conformance-suite/harness/generate.py --check    # the refusals only, writing nothing
python conformance-suite/harness/generate.py --self-test
```

`--check` is an audit of WiseOwl COBOL's local gate and of its CI, so a malformed test, adapter, profile or baseline
is red in seconds.

## Running a suite against a compiler

Generate the suites first, then run them with the compiler's adapter (Python 3, standard library only):

```
python conformance-suite/harness/generate.py
python conformance-suite/harness/run.py --compiler wiseowl --set cobol=<path to the cobol command>
python3 conformance-suite/harness/run.py --compiler gnucobol            # where cobc is (on Windows, under WSL)
python conformance-suite/harness/run.py --compiler gnucobol --record-baseline
python conformance-suite/harness/run.py --self-test
```

`--editions 85,2014` and `--test <id>` narrow a run, `--jobs`, `--hang-guard` and `--keep` tune it. Each test runs in
its own fresh working directory with its declared files and stdin closed. Its verdict is **PASS**, **FAIL** (the
output differs, an accepted test did not translate or run, or no diagnostic was reported at a flagged test's marked
line), **NOT APPLICABLE** (the profile lacks the edition, a required module or element, or the test uses a word the
compiler reserves beyond the standard; or a flagged test's control did not compile) or **NO VERDICT** (the compiler
crashed, a process exceeded the hang guard, or the harness could not run the test). Results go to
`results/<compiler>/` as JSON and JUnit XML, with totals per edition and per clause and the changes against
`baselines/<compiler>.json`: a run is RED only when a verdict became worse than the recorded one (or, with no
baseline, on any FAIL or NO VERDICT).

### In WiseOwl COBOL's own gate

WiseOwl COBOL's test assembly `tests/Cobol.Net.Tests.Conformance` runs the suite in-process (design §5, §13 S1c):
`ConformanceSuiteTests` generates the manifests into a directory of its own (`generate.py --out`) and runs every test
of `suites/wiseowl/manifest.json` (both trees, at every edition each names) through `ConformanceSuiteRunner`, which
judges exactly as `run.py` does with the WiseOwl adapter, red on FAIL or NO VERDICT; and it compiles every refusal of
`wiseowl/diagnostics.json` at its editions, which must be refused with its code or text. `ConformanceSuiteRunnerDriftTests`
runs `run.py` through the WiseOwl adapter on the same generated suites and requires both runners to give every suite
test the same verdict, holds both output normalizations to the one fixture `harness/normalization-fixture.json`, and
holds the WiseOwl adapter's locale pin (a program's default locales are its environment's) to the in-process one.

### Writing an adapter for another compiler

An adapter is configuration (`harness/config.py` documents every field). Its command templates fill `{source}`,
`{program}`, `{work}`, `{std}`, `{source-format}`, `{python}` and the adapter's own `variables`. The shapes for two
compilers this repository cannot run, unverified:

- **IBM Enterprise COBOL** (z/OS, Python for z/OS): the warning mechanism is the `FLAGSTD` compiler option, the
  editions map to `FLAGSTD` levels, and an ASSIGN name is a DD name, so the adapter's `assign` sets an environment
  variable per declared file, or a `hook` (a `.py` file beside the adapter defining `compile(ctx)`, `run(ctx)` and
  `flag(ctx)`, each returning a `config.Step`) submits the compile and go steps as a batch job and reads the listing.
- **Rocket (Micro Focus) Visual COBOL**: `cob -x {source} -o {program}` with the standard-flagging directive (`FLAG`
  set to the edition's level) for the flag-mode; an ASSIGN name maps through `dd_<name>` environment variables.

## Acknowledgment

The tests quote ISO/IEC 1989:2023, whose Introduction permits reproducing the COBOL standard "in whole or in part"
and requests that the following acknowledgment paragraphs be reproduced in their entirety:

COBOL is an industry language and is not the property of any company or group of companies, or of any organization or
group of organizations.

No warranty, expressed or implied, is made by any contributor or by the CODASYL COBOL Committee as to the accuracy and
functioning of the programming system and language. Moreover, no responsibility is assumed by any contributor, or by
the committee, in connection therewith.

The authors and copyright holders of the copyrighted materials used herein:

— FLOW-MATIC™<sup>1</sup>;

— IBM®<sup>2</sup> Commercial Translator Form No F 28-8013, copyrighted 1959 by IBM;

— FACT©, DSI 27A5260-2760, copyrighted 1960 by Minneapolis-Honeywell

have specifically authorized the use of this material in whole or in part, in the COBOL specifications. Such
authorization extends to the reproduction and use of COBOL specifications in programming manuals or similar
publications.

<sup>1</sup> FLOW-MATIC™ is the trademark of a product supplied by Sperry Rand Corporation. This information is given
for the convenience of users of this document and does not constitute an endorsement by ISO or IEC of the product
named.

<sup>2</sup> IBM is the trademark of International Business Machines Corporation. This information is given for the
convenience of users of this document and does not constitute an endorsement by ISO or IEC of the product named.

## License

Until the owner chooses the suite's own license (kb/Work PB3159), this folder is under the repository's license.
