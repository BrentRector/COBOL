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

## Running the generator

```
python conformance-suite/harness/generate.py            # refuse, or write suites/ and print the completeness report
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
baseline, on any FAIL or NO VERDICT). `--record-baseline` with `--editions` re-records only those editions and keeps
the others' verdicts; it cannot be combined with `--test`.

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
