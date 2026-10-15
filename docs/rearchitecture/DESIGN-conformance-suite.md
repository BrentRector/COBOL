# The per-edition conformance suites — design (kb/Work PB3160)

**Status: PROPOSED, for the owner's review (2026-10-14).** Sections 3-7 were each approved in conversation before this
document was written; the written whole is reviewed by Mythos (the owner's request) and then by the owner. Nothing in
it is built yet. Until it is approved, `docs/VERSION_TEST_MATRIX_DESIGN.md` §5 and `tests/conformance/README.md`
describe the corpus as it is.

## 1. The direction and the decisions

The owner, 2026-10-14, verbatim: "Separately, I think we should evolve all test cases to separate suites for each
COBOL specification version, more or less positioning them as conformance tests equivalent to the NIST tests.
However, we need not use the NIST structure as that is a 40+ year old designs. We can modernize"

| Question | Owner's answer (2026-10-14) |
|---|---|
| Who should be able to run the suites? | "Any COBOL compiler" — expected results are outputs and kinds of rejection, never WiseOwl error codes; adapters run the suite against WiseOwl COBOL, GnuCOBOL and in principle IBM or Rocket; it can be licensed on its own |
| How are the four suites built? | "A: one tree, generated" (§4) |
| The case format, the tree, the harness, the migration (§3-§6) | "Looks right", each |
| Where does this rank against the compiler work? | "Pilot now, rest after GAP": the mechanical move and one pilot clause now; the clause-by-clause completion waits for zero GAP and zero known defects, like the other work held under kb/Work PB2911 |
| Review | "Also have Mythos review the design" |

**Success** is four complete suites, one per edition (85, 2002, 2014, 2023): every normative rule of an edition, the
rules it inherits included, is witnessed in that edition's suite by at least one test whose expected result comes from
the ISO standard, and any COBOL compiler can run a suite and get a per-test verdict.

## 2. What exists (measured 2026-10-14, main 11f757593)

- `tests/conformance/{85,2002,2014,2023}/`: 2,387 positive programs (`.cob` + expected stdout `.out`; 14 compile-only).
  `tests/conformance/negative/`: 2,678 must-reject programs, each naming its editions on a `*> reject-at:` line and
  its expected WiseOwl diagnostic in a `.err` file. Each directory has a `manifest.json` (`enabled`, `pending`).
- `CorpusRunnerTests` (`tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs`) compiles a positive program ONLY at
  its directory's edition (`ConformanceCorpus.PositiveOptions`, `DialectLevel: int.Parse(edition)`). So no directory
  is a suite: the 2014 directory holds the 2014 additions, and nothing runs an 85 program at 2014 and checks its
  output. `docs/VERSION_TEST_MATRIX_DESIGN.md` §5's "each program compiles/runs at ≥ its edition" describes this
  design's step 2, not the runner.
- The traceability inventory (`tests/version-matrix/traceability-inventory.json`, 4,347 rows) ties rules to tests
  through `test-ref`. 3,851 rows cite at least one corpus program; the other 496 cite only xUnit tests, a NIST
  program (4 rows) or nothing. Of the programs, 1,911 positive and 2,088 negative are cited by some row; 476 positive
  and 590 negative are cited by none. The `editions` field is written two ways and is empty on 153 rows.
- NIST CCVS85 (`tests/nist/`, 364 programs) covers COBOL-85 only; `VersionMatrixTests` compiles its green set at
  2002/2014/2023 check-only.
- 119 tracked files outside `tests/conformance/` name its paths or its runner classes (skills, scripts, docs, tests,
  source comments).
- GnuCOBOL 3.2 (`cobc` in WSL) offers `-std=cobol85`, `cobol2002`, `cobol2014`, and no 2023 dialect.

## 3. The test case

One test is ONE self-contained COBOL program that witnesses one rule (or a few tightly related rules), with a metadata
block at its top. The block is comment lines with `*` in column 7, so every compiler at every edition, COBOL-85
included, reads it as commentary.

```cobol
      *>--- conformance-case
      *> id: 14.9.8-compute-multi-receiver-size-error
      *> rules: [GR-14.9.8.4-2]
      *> clause: 14.9.8 COMPUTE statement
      *> accept: [85, 2002, 2014, 2023]
      *> requires: []
      *> implementor-defined: []
      *> options: {source-format: fixed}
      *>---
```

The rule in the example is real: `GR-14.9.8.4-2` applies at all four editions (inventory `editions`), and its seven
corpus witnesses are spread over all four directories (two in `85/`, one in `2002/`, two in `2014/`, two in `2023/`).
Each runs only at its own directory's edition, so `2014/ca5b_prohibited_multi_receiver_size_error` has never run at 85
or 2002 and the two `85/` programs have never run at 2014 or 2023. Which editions each of them belongs in is what §6
step 2 finds out.

| Field | Meaning |
|---|---|
| `id` | Stable forever; a changed expectation keeps the id and is named in the release notes |
| `rules`, `clause` | The inventory `rule-id`s witnessed and the ISO clause; the generator refuses an id the inventory lacks |
| `accept` | Editions at which the program must compile and run; expected stdout in `<id>.out`, or `<id>.<edition>.out` where the standard's behaviour differs by edition |
| `reject` | Editions at which the program must be refused, with the phase: `compile`, or `run` with the ISO exception-name (`EC-…`) when the rule names one. Never a vendor diagnostic |
| `requires` | Optional modules (Annex A.4) and processor-dependent elements (Annex A.3) the test needs; a compiler that does not claim one gets NOT APPLICABLE |
| `implementor-defined` | Annex A.1 items the expected result depends on; each compiler's profile (§4) supplies the values |
| `options` | Source format and any other compile-time setting the test needs |

Results are checked by comparing stdout with the `.out` file (normalized as `CutRunner.Normalize` does today: LF,
per-line trailing-space trim, no trailing newline). Tests do not self-check in the NIST manner: output comparison is
simpler and works with any compiler. A run-time rejection is judged by trapping where the program can: the test traps
the condition and prints its `EC-…` name, which makes it an output comparison; otherwise abnormal termination is the
expected result.

WiseOwl's stricter checks stay outside the cases: `wiseowl/diagnostics.json` maps a negative test's id to the exact
COBOLNET code (and its severity), read only by the WiseOwl runner.

## 4. The tree, the generator and where it lives

```
conformance-suite/
  LICENSE                     the suite's own license (kb/Work PB3159 decides it)
  README.md                   how to run it; ISO's acknowledgment paragraphs verbatim (§7)
  cases/<clause>/<id>.cob     the tests, filed by ISO clause, e.g. cases/14.9.8-COMPUTE/
  cases/<clause>/<id>[.<ed>].out
  profiles/<compiler>.json    supported editions, claimed A.4 modules and A.3 elements, A.1 values
  adapters/<compiler>.json    compile and run command templates and the edition map (§5)
  harness/                    the runner (§5)
  wiseowl/diagnostics.json    WiseOwl's codes for the negative tests (§3)
```

- **In this repository**, not a separate one: a compiler fix and its test land in one change and CI runs both on
  every change. The folder's own LICENSE lets it be licensed apart from the compiler.
- **Filed by clause**, because edition is metadata: a folder per clause shows coverage against the standard's
  structure, and an inherited rule's test is written once.
- **The generator** (`harness/generate.py`) validates every test and emits the four suites. It refuses: an unknown
  rule id; an `accept`/`reject` list that contradicts the construct registry (`tests/version-matrix/constructs.json`,
  each construct's `introducedIn`/`removedIn`, the f(case, V) of `VERSION_TEST_MATRIX_DESIGN.md` §2); a test with
  neither list; an `.out` for an edition not in `accept`. It writes `suites/<edition>/` (each applicable test with its
  expectation at that edition) and a `manifest.json` per suite. The suites are build output, never committed.
- **Completeness is measured, not asserted**: for each edition, every inventory row whose `editions` includes it must
  be witnessed by at least one test in that suite; the generator lists the rows that are not. The inventory's
  `editions` field is normalized (one spelling; the 153 empty rows decided) as part of the move.

## 5. The harness, the adapters and the results

- **`harness/run.py`, Python 3 standard library only.** It runs on Windows, Linux and z/OS (IBM ships Python for
  z/OS), so a vendor runs it where its compiler lives; a .NET tool would not run on z/OS.
- **An adapter is configuration**: compile and run command templates and an edition map. GnuCOBOL's:
  `compile: cobc -std={std} -x {src} -o {exe}`, `run: {exe}`, `editions: {85: cobol85, 2002: cobol2002, 2014: cobol2014}`.
  A compiler that needs more (IBM's batch jobs) supplies a Python hook with the same two operations. Shipped: WiseOwl
  COBOL (`cobol`) and GnuCOBOL; documented shapes for IBM Enterprise COBOL and Rocket Visual COBOL.
- **Verdicts:** PASS · FAIL (with the output difference, or compiled-when-it-should-reject and the reverse) ·
  NOT APPLICABLE (the profile lacks the edition, module or element) · NO VERDICT (the compiler or harness crashed or
  exceeded a generous hang guard). NO VERDICT is counted on its own and never folded into PASS or FAIL; there is no
  wall-clock limit on a test's expected behaviour.
- **Results** as JUnit XML and JSON, totals per edition and per clause.
- **In WiseOwl's own pipeline**: an xUnit runner in `Cobol.Net.Tests.Conformance` reads the same cases in-process
  (fast) and also checks `wiseowl/diagnostics.json`; CI also runs `harness/run.py` with the WiseOwl adapter, so the
  compiler-neutral path cannot break unseen; the GnuCOBOL adapter replaces the separate GnuCOBOL differential in the
  per-batch battery.

## 6. Migration

1. **One mechanical, behaviour-neutral move (one change).** A converter moves every program into
   `conformance-suite/cases/<clause>/` and writes its header from what is known: `rules` from the inventory's
   citations, `accept` = its current directory's edition only, `reject` from its `reject-at:` line, its `.err` code
   into `wiseowl/diagnostics.json`, `options` from today's `*> options:` header. The same change rewrites the
   inventory's `conformance:` citations (7,928) to the new ids, normalizes its `editions` field (one spelling; each of
   the 153 empty rows decided from the standard), replaces `CorpusRunnerTests` and `CorpusManifest` with
   the new runner, deletes `tests/conformance/` and its `manifest.json` files, and updates the 119 files that name the
   old paths (CLAUDE.md rule 4: no wrapper, alias or compatibility layer). Proof of neutrality: the same programs at the
   same editions give the same verdicts, case by case, before and after.
2. **Clause by clause, the suites are completed.** Each test is run at every edition after its own. Where WiseOwl's
   result is the same, the edition joins `accept`; where it differs, the ISO standard decides, never the compiler: a
   real edition difference (a per-edition `.out`, or a `reject`) or a compiler defect (a kb/Work note).
3. **The 1,066 uncited programs** are given rule ids in their clause's pass; one that witnesses no normative rule
   leaves the suite and stays a WiseOwl-only regression test.
4. **The 496 rows with no portable witness** get new programs, written from the standard by the golden lane.
5. **NIST CCVS85 stays in `tests/nist/`**, a separate regression net outside the suite.
6. Each clause pass runs in the loop as a PB3160 campaign wave, once released (§8).

## 7. Releases, versioning and the license

- CI builds the release archives from a tag: per edition, the suite, the harness, the adapters, the profiles, the
  README and the LICENSE. **Nothing is published until the owner has chosen the license (kb/Work PB3159)**; until
  then the folder is under the repository's Business Source License like the rest of it.
- The README carries the ISO standard's acknowledgment paragraphs verbatim: the standard permits reproducing it "in
  whole or in part" on that condition (its Introduction; transcribed in `specs/ISO_COBOL.md`), and the tests' comments
  quote it.
- The suite has its own version (for example `2026.1`); each archive names its edition, its suite version and the
  WiseOwl commit it was verified on.

## 8. Order of work

1. **Now** (owner: "Pilot now, rest after GAP"): the mechanical move (§6 step 1) and the pilot.
2. **The pilot is clause 11, the Identification Division**: about 114 rules and about 100 programs, most of them
   edition-limited (COBOL-85's PROGRAM-ID beside 2002's CLASS-ID, FUNCTION-ID and INTERFACE-ID), with negative tests.
   It runs §6 steps 2-4 for that clause at all four editions on WiseOwl and GnuCOBOL and measures the cost per test;
   that measurement replaces the earlier rough estimate (4-8 weeks of loop capacity) for the rest.
3. **After zero GAP and zero known defects**: the remaining clauses, as PB3160 campaign waves.

## 9. Drift tests

- The generator's own refusals (§4) run in every gate.
- Every inventory `test-ref` naming a suite test resolves to a file; every test's `rules` exist in the inventory.
- Every `accept`/`reject` list agrees with `constructs.json`.
- Every negative test has a `wiseowl/diagnostics.json` entry, and every entry names an existing test.
- The completeness report (§4) is printed by the generator and shown on the ledger per edition.

## 10. Rejected alternatives

- **Four physical copies** (each inherited test copied into every later edition's folder): four copies drift, and a
  hand-maintained duplicate is what CLAUDE.md rules 4 and 5 forbid.
- **Keep today's layout and run forward**: cheapest, but the tests stay WiseOwl-specific (COBOLNET codes, the xUnit
  harness), so no other compiler could run them.
- **A separate repository**: every compiler fix would become a two-repository change.
- **A .NET harness**: does not run on z/OS.
- **Self-checking programs in the NIST style**: more code per test for no gain over output comparison.
- **Vendor diagnostic codes in the cases**: no other compiler could pass a negative test.

## 11. Open

- The suite's license and product name (kb/Work PB3159).
- The IBM and Rocket adapters are documented but unverified: no access to either compiler.
- GnuCOBOL has no 2023 dialect; its 2023 suite is NOT APPLICABLE until it does.
