# The per-edition conformance suites — design (kb/Work PB3160)

**Status: APPROVED by the owner, 2026-10-14 ("approved", revision 2).** Revision 1 (ec8138043) wrote up what the
owner approved section by section in conversation. Mythos reviewed it at the owner's request
(`E:/COBOL-coord/scratch/PB3160-design-review-mythos.md`, verdict REVISE) and the operator folded the findings in
(§12 lists each); revision 2 changed how a must-reject test is judged (§3.2), and the owner approved the written
whole. Implementation is the slices of §13, each a kb/Work note in the PB3160 campaign. Until slice S2 lands,
`docs/VERSION_TEST_MATRIX_DESIGN.md` §5 and `tests/conformance/README.md` describe the corpus as it is.

## 1. The direction and the decisions

The owner, 2026-10-14, verbatim: "Separately, I think we should evolve all test cases to separate suites for each
COBOL specification version, more or less positioning them as conformance tests equivalent to the NIST tests.
However, we need not use the NIST structure as that is a 40+ year old designs. We can modernize"

| Question | Owner's answer (2026-10-14) |
|---|---|
| Who should be able to run the suites? | "Any COBOL compiler" — expected results are outputs and kinds of diagnosis, never WiseOwl error codes; adapters run the suite against WiseOwl COBOL, GnuCOBOL and in principle IBM or Rocket; it can be licensed on its own |
| How are the four suites built? | "A: one tree, generated" (§4) |
| The case format, the tree, the harness, the migration | "Looks right", each, in revision 1 |
| Where does this rank against the compiler work? | "Pilot now, rest after GAP": the mechanical move and the pilot now; the clause-by-clause completion waits for zero GAP and zero known defects, like the other work held under kb/Work PB2911 |
| Review | "Also have Mythos review the design" (done; §12) |

**Success** is four suites, one per edition (85, 2002, 2014, 2023), each witnessing every rule of that edition whose
obligation a program can observe, the rules it inherits included, by tests whose expected results come from the ISO
standard; and any conforming COBOL compiler can run a suite and get a per-test verdict that FAILs only for a
non-conformance. A rule WiseOwl COBOL declines (documented non-support) is witnessed only once a test written from the
standard has been passed by some compiler (§6 step 1).

## 2. What exists (measured 2026-10-14 at 11f757593 and aee17f1b4)

Counting rule: a "program" is a manifest entry (`enabled` + `pending`); the files on disk add two unlisted `.cob`
(`2002/_pending_oo/oo_hello.cob`, `85/pb1355lib/pb1355d.cob`), which account for every small difference below.

- `tests/conformance/{85,2002,2014,2023}/`: 2,387 positive programs (423 · 859 · 250 · 855), each `.cob` with its
  expected stdout `.out` (14 have no `.out` and are compile-only), plus copybooks (35 `.cpy` at the directories' top
  level), a library directory (`85/pb1355lib/`), data files and two `.cbl` compilation units.
  `tests/conformance/negative/`: 2,678 must-reject programs, each naming its editions on a `*> reject-at:` line and
  its expected WiseOwl diagnostic in a `.err` file, which the runner matches with `Contains`: of the 2,679 `.err`
  files, 1,908 are a bare code and 771 are something else, 540 of them a message substring with no code at all. Each
  directory has a `manifest.json` (`enabled`, `pending`).
- `CorpusRunnerTests` compiles a positive program ONLY at its directory's edition (`ConformanceCorpus.PositiveOptions`,
  `DialectLevel: int.Parse(edition)`), with the source format DETECTED (`InitialReferenceFormat.Auto`) unless the
  program's `*> options:` says otherwise. So no directory is a suite: nothing runs an 85 program at 2014 and checks
  its output.
- The traceability inventory (4,347 rows) ties rules to tests through `test-ref` (7,928 `conformance:` citations).
  3,851 rows cite at least one corpus program. The other 496 cite only xUnit tests, a NIST program or nothing; by
  verdict they are 394 CONFORMS (222 of them Annex A.1 documentation rows pinned by `docs/CONFORMANCE.md`, 46 carrying
  a derivation), 45 DOCUMENTED-NON-SUPPORT, 44 PARTIAL, 12 DIVERGES, 1 NOT-IMPLEMENTED. 476 positive and 590
  negative programs are cited by no row. 457 rows are DOCUMENTED-NON-SUPPORT; 84 negative programs witness them,
  asserting that WiseOwl refuses a standard facility. The `editions` field has one spelling (§13 S0a) and is
  decided on every row (§13 S0b): a run from the rule's introducing edition through 2023.
- NIST CCVS85 (`tests/nist/`, 364 programs) covers COBOL-85 only.
- Consumers of the layout: `git grep -l -E 'tests/conformance|CorpusRunnerTests|ConformanceCorpus|CorpusManifest'`,
  excluding the history trees (`kb/`, `DEVLOG/`, `adjudication/`) and the corpus itself, lists 144 files (60 docs, 84
  others: tests, scripts, skills, CI, hooks, `src/` comments); 64 open kb/Work notes name the paths.
- GnuCOBOL 3.2 (`cobc` in WSL): `-std=cobol85 | cobol2002 | cobol2014`, no 2023. `-std` selects "warnings/features
  for a specific dialect"; it is not a conformance mode. Mythos ran a 40 + 40 sample of today's corpus through it as
  revision 1's adapter would: 5 of 40 positive 2002 programs passed (30 compile failures: object orientation, national,
  a `COMP-3` usage, long file base names; 4 output differences; 1 run failure), and most failures were not GnuCOBOL
  non-conformances. That sample is why §3 and §5 changed.

## 3. The test case

One test is one COBOL program, plus any files it declares, that witnesses one rule (or a few tightly related rules),
with a metadata block opening the file (its first line is the block's first line). The block is comment lines with
`*` in column 7 in fixed form, so `*>` in columns 7-8 (every compiler at every edition, COBOL-85 included, reads them
as commentary; columns 1-6 are the sequence number area), and `*>` comment lines starting in column 1 in free form
(2002 and later, as free form itself is). Each field is one line, `*> <field>: <value>`, the value a scalar, a flow
list `[a, b]` or a flow map `{k: v}`.

```cobol
      *>--- conformance-case
      *> id: compute-multi-receiver-size-error
      *> rules: [GR-14.9.8.4-2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
```

The rule in the example is real: `GR-14.9.8.4-2` applies at all four editions, and its seven corpus witnesses are
spread over all four directories (two in `85/`, one in `2002/`, two in `2014/`, two in `2023/`). Each runs only at
its own directory's edition today; which editions each belongs in is what §6 step 2 finds out.

### 3.1 Fields

| Field | Meaning |
|---|---|
| `id` | Equal to the file's base name (the generator checks), unique across both trees; letters, digits, `-` and `_` (an expected output's name adds `.` parts to it). Carries no clause: the clause is the folder (§4), so a mis-filed test can move. Ids are stable from the first released suite version (§7) |
| `rules` | The inventory `rule-id`s witnessed; the generator refuses one the inventory lacks, and an edition the test is accepted at that is outside a rule's `editions` band (the rule does not exist there). Required; `[]` only in `cases/_unassigned/` |
| `accept` | Editions, ascending, at which the program must compile AND run, its stdout equal to `<id>.out`, or `<id>.<edition>.out` where the standard's behaviour differs by edition. A test that only proves translation ends in `STOP RUN` with an empty `.out`. Every accepted edition has its output and every output file is used by one |
| `constructs` | The `tests/version-matrix/constructs.json` rows the program uses whose window is not every edition (a 2002 introduction, an element 2002 removed). Each accepted edition lies inside each named construct's window (`introducedIn ≤ V < removedIn`, the f(case, V) of `VERSION_TEST_MATRIX_DESIGN.md` §2). The field is how the generator checks a test against the construct registry: a COBOL program's constructs cannot be read from it without compiling it |
| `flag` | §3.2: the editions at which a standard-conformance diagnostic is required, with its kind |
| `control` | §3.2: the id of the accepted twin of a flagged test |
| `requires` | Optional modules (Annex A.4) and processor-dependent elements (Annex A.3) the test needs, spelled `A.4.<n>` (the module's subclause, `A.4.11` Report Writer) and `A.3-<n>` (the element's item); the generator reads the valid names from the standard's annexes. A compiler whose profile does not claim one gets NOT APPLICABLE |
| `implementor-defined` | §3.3: Annex A.1 items whose standard-enumerated value the expected output depends on, by item number |
| `files` | Copybooks, data files and other compilation units the test needs, beside it; the generator refuses a COPY (in the program or in a declared copybook) of an undeclared library text, a declared file that is not there, and a file in a clause folder no test declares or expects |
| `source-format` | `fixed` or `free`, required on every test (compilers differ in their default) |
| `portable` | `unverified` on every migrated test until its clause pass (§6 step 2) clears it; the suites carry only cleared tests |

### 3.2 Tests a compiler must diagnose (revised)

The standard never requires a compiler to REFUSE a program. ISO §4.2.2 requires "a warning mechanism that optionally
may be invoked by the user at compile time to indicate violations of the general formats and the explicit syntax
rules", and a compiler "may, but is not required to, flag" violations of general rules; archaic and obsolete elements
(§4.2.12, §4.2.13), nonstandard extensions (§4.2.10) and unsupported processor-dependent elements (§4.2.6) follow the
same shape. A test that FAILs a compiler for producing an executable would fail conforming compilers (IBM Enterprise
COBOL with `FLAGSTD` warns and still translates). So:

- `flag: {kind, at}` replaces revision 1's `reject`. `kind` is one of `syntax`, `obsolete`, `archaic`, `nonstandard`,
  `processor-dependent`; `at` lists the editions. Meaning: with the compiler's standard-conformance warning mechanism
  invoked (the adapter's `flag-mode`, §5), a diagnostic is reported at the test's marked line. Whether the compiler
  also refuses to translate is not judged. A test may be accepted at some editions and flagged at others, never both
  at one. Each edition it is flagged at lies inside every rule's band, or outside the window of a construct it names
  (the construct does not exist there, so using it is the violation).
- **The marked line**: in free form the line ends with the floating comment `*> <<`; in fixed form its sequence
  number area (columns 1-6) opens with `<<`. Fixed form cannot use a floating comment, which COBOL-85 lacks, nor a
  column past 72, because margin R "is a fixed position defined by the implementor" (ISO §6.3.1); the sequence number
  area "may consist of any character in the computer's coded character set" (§6.3.2) at every edition. Exactly one
  line of a flagged test is marked; a mark in a test that is not flagged is refused.
- Only syntax rules and general formats (inventory `SR-` and `FMT-` rows) may carry `flag: syntax`. A general rule
  (`GR-`) in a flagged test is refused by the generator unless the test states why its violation is syntactically
  detectable (`flag: {kind: syntax, at: [...], why: "..."}`); any other kind of row is refused.
- **Every flagged test has a control**: the same program with the violation removed, named by `control:` and
  accepted at every edition the test is flagged at; a suite test's control is itself a suite test. A flagged test is
  PASS only when its control compiled, so a compiler that lacks the whole feature (no object orientation, no national
  class) gets NOT APPLICABLE, never a vacuous PASS. A `control` on a test that is not flagged is refused.
- **Run-time violations** are judged only where a program can observe them, from 2002 on (exception checking and the
  `EC-` names are 2002 features; at 85 a violated rule's result is undefined): the test turns checking on, traps the
  condition and prints its `EC-` name, which makes it an ordinary `accept` test. The generator refuses a run-time
  expectation at 85 (a program that observes an exception condition: a TURN directive, an `EC-` name or an exception
  function, §15.28-15.33), and abnormal termination is not a portable expectation (exit status is not standard).
- WiseOwl's own stricter checks (that it refuses, and with which code) stay outside the cases, in
  `wiseowl/diagnostics.json` (§4).

### 3.3 Expected output that is portable

The standard leaves the device form of most DISPLAYed data to the implementor: §14.9.11.4 GR 1 "Any conversion of
data required between literal-1 or the data item referenced by identifier-1 and the device is defined by the
implementor" (Annex A.1 item 56). Two conforming compilers print a signed numeric DISPLAY item as `01E` and `+01.5`.
So:

- **A portability lint the generator enforces**: a portable test DISPLAYs only alphanumeric literals, alphanumeric
  items and numeric-edited items; never an un-edited numeric item, a function result, an arithmetic expression or a
  national operand. GR 1 makes the conversion of EVERY operand to the device implementor-defined, and the national
  operand is where it shows: for `PIC N(3) VALUE N"ABC"` at 2002 WiseOwl prints `ABC` (bytes 41 42 43) and GnuCOBOL 3.2 prints the
  UTF-16BE bytes (NUL `A` NUL `B` NUL `C`), both conforming (branch review R1-2). A test that must show a number
  MOVEs it to a numeric-edited item first; a test of national data compares it inside the program (IF, or MOVE to
  an alphanumeric item) and DISPLAYs an alphanumeric literal naming the outcome. A figurative constant is allowed:
  DISPLAY shows "the alphanumeric representation of that figurative constant" (§14.9.11.4 GR 3), except an ALL
  national literal. A reference-modified numeric or numeric-edited item of usage display is alphanumeric (§8.4.3.3.4
  GR 6 c) and allowed; one of computational usage would show its storage and is not. The lint also refuses `UPON`
  (an implementor-defined device, §14.9.11.3 SR 2, whose output no expected stdout holds) and, in fixed form,
  program text past column 72 (margin R, above). It applies to the tests the suites carry; a name the generator
  cannot classify (a RENAMES item, a special register) is refused rather than guessed.
- **`implementor-defined`** names Annex A.1 items whose values the standard enumerates. The example is item 148,
  "RECORD clause (whether fixed or variable records produced for fixed-or-variable-length format)": §13.18.43.4 GR 17
  "It is implementor defined whether format 3 of the RECORD clause produces fixed-length records or variable-length
  records" names exactly two values. Such a test carries one expected output per value, `<id>.<item>=<value>.out`
  (`<id>.148=fixed.out`, `<id>.148=variable.out`; with a per-edition file, `<id>.<edition>.<item>=<value>.out`), and
  each compiler's profile (§4) states its value. The enumerated items and their values are data,
  `conformance-suite/implementor-defined.json`, each entry naming its inventory row (`DOC-A.1-<item>`) and quoting
  the rule that enumerates the values; a test naming an item not there is refused. An A.1 item
  whose values the standard does not enumerate cannot be an expectation: that test is WiseOwl-only. The sign
  convention is such an item: §13.18.52.4 GR 4 "The implementor shall specify the position and representation of
  the operational sign" and GR 5 b) "The implementor defines what constitutes valid signs for data items" (items 177
  and 178) enumerate nothing, so the tests that encode it today (`*> options: sign-encoding=ascii`) go to
  `wiseowl/cases/` (branch review R1-1).

## 4. The tree, the generator and where it lives

```
conformance-suite/
  LICENSE                     the suite's own license (kb/Work PB3159 decides it)
  README.md                   how to run it; ISO's acknowledgment paragraphs verbatim (§7)
  cases/<clause>/<id>.cob     the tests, filed by ISO clause, e.g. cases/14.9.8-COMPUTE/
  cases/<clause>/<id>[.<ed>].out, and the files a test declares
  cases/_unassigned/          migrated programs not yet given a rule (§6); excluded from the suites
  implementor-defined.json    the A.1 items whose values the standard enumerates, and those values (§3.3)
  suites/                     build output, git-ignored: suites/<edition>/, suites/wiseowl/ and completeness.json
  profiles/<compiler>.json    supported editions, claimed A.4 modules and A.3 elements, A.1 values, extra reserved words
  adapters/<compiler>.json    compile, run and flag commands, the edition map, the ASSIGN mapping (§5)
  baselines/<compiler>.json   that compiler's recorded verdicts (§5)
  harness/                    the generator and the runner (§5)
  wiseowl/diagnostics.json    WiseOwl's refusals: per test, the editions at which WiseOwl refuses it and the code
                              or message substring it reports (today's reject-at + .err, carried over by §6 step 1)
  wiseowl/cases/<clause>/     WiseOwl-only regression tests, same format: documented non-support, tests of no
                              normative rule, tests that cannot be made portable; the generator writes them to a
                              `wiseowl` manifest only, never to a suite
```

- **In this repository**, not a separate one: a compiler fix and its test land in one change and CI runs both. The
  folder's own LICENSE lets it be licensed apart from the compiler.
- **Filed by clause**, because edition is metadata: a folder per clause shows coverage against the standard's
  structure, and an inherited rule's test is written once.
- **The generator** (`harness/generate.py`, with `harness/cobol_source.py` for the facts it reads from program
  text) is the ONE reader of test headers. It refuses: an unknown rule id; an `id` unequal to its file name, or one
  two tests share; `accept`/`flag` lists that contradict the inventory's `editions` bands or the construct registry
  (`tests/version-matrix/constructs.json`, through the test's `constructs`, §3.1); a flagged test with no control, a
  control not accepted where the test is flagged, a mark that is missing, doubled or on a test not flagged, or a
  GR-only flagged test without its justification; a run-time expectation at 85; free form at 85; an undeclared COPY;
  a test missing `source-format`, or whose block is written in the other form; an unknown field; a missing or unused
  expected output; an A.1 item or `requires` name the registries lack; a test filed under a clause none of its rules
  belongs to, or with no rules outside `_unassigned/`; a folder not named `<clause>-<Name>`; a file no test declares;
  a `wiseowl/diagnostics.json` entry naming no test or refusing a test where it must be accepted, and a flagged test
  without one; a portability-lint violation in a test the suites carry. It writes `suites/<edition>/manifest.json`
  per edition (the cleared tests of `cases/`, not `_unassigned/`, that apply there, with their expectation: the
  expected-output file per A.1 value, or the flag kind, the marked line and the control), and
  `suites/wiseowl/manifest.json`: every test of both trees at each edition it names, portable or not, and the
  `wiseowl/diagnostics.json` refusals, which is what WiseOwl's own runner executes (S1c). The suites are build
  output, never committed.
- **Completeness is reported per witness kind**, never as one count that can never reach zero: for each edition, the
  inventory rows witnessed by a suite test, by a documented determination (`docs/CONFORMANCE.md`), by a derivation,
  or only by a WiseOwl unit test, and the rows with no witness. The suite's own measure is "rows whose obligation a
  program can observe, witnessed in this suite". A DOCUMENTED-NON-SUPPORT row reads "unwitnessed until implemented".
  Each row whose band holds the edition is counted once, under the first kind that holds, in the order documented
  non-support, suite test, determination (a DOC row §7 carries), derivation (one that stands), WiseOwl-only (a
  spec-derived `test-ref`, or a test of the wiseowl manifest), none; the rows a program can observe are all but the
  documented non-support and the derivations.

## 5. The harness, the adapters and the results

- **`harness/run.py`, Python 3 standard library only**, runs on Windows, Linux and z/OS (IBM ships Python for
  z/OS); a .NET tool would not run on z/OS. It executes the generator's manifests, never headers.
- **An adapter is configuration**: command templates for compile, run and flag-mode (how the conformance warning
  mechanism is invoked: GnuCOBOL `-std=<edition>`, IBM `FLAGSTD`), how a diagnostic's line number is read from the
  compiler's output, how an ASSIGN name maps to a file, any limit on file names (cobc refused long base names), the
  exit codes that mean the compiler crashed, and the edition map. GnuCOBOL: `cobc -std={std} -fformat={source-format}
  -x -I . -o {program} {source}`, `{work}/{program}`, `{85: cobol85, 2002: cobol2002, 2014: cobol2014}`, base names
  of at most 31 characters (measured on 3.2.0: "invalid file base name ... length exceeds maximum" at 32). A compiler
  that needs more (IBM's batch jobs and DD names) supplies a Python hook with the same operations (`compile(ctx)`,
  `run(ctx)`, `flag(ctx)`, each returning a `Step`). Shipped: WiseOwl COBOL (`cobol`) and GnuCOBOL; documented
  shapes for IBM Enterprise COBOL and Rocket Visual COBOL in the folder's README. `harness/config.py` is the ONE
  loader and validator of adapters, profiles and baselines: `run.py` checks their shape, and the generator's audit
  (`generate.py --check`, §9) checks every one of them against what only the repository knows (the editions, the
  annexes' names, the A.1 items, each suite's test ids), so a malformed adapter is red in seconds like a malformed test.
- **A profile is evidence, not assertion**: each claimed A.4 module and A.3 element carries the evidence for the
  claim. WiseOwl's claims are the rows `docs/CONFORMANCE.md` §5 and §2 dispose Claimed or Partial (§5's preamble: the
  conformance summary claims what is Claimed or Partial there), held to those tables by
  `ConformanceSuiteProfileDriftTests`; GnuCOBOL's were measured on cobc 3.2.0, and the S3 pilot verifies them.
  Every profile states its value of every A.1 item `implementor-defined.json` enumerates (the generator refuses one
  that does not), and its extra reserved words per edition (GnuCOBOL's: the words `cobc --list-reserved
  -std=<dialect>` lists that `reserved-words.json` does not reserve there).
- **Each test runs in its own fresh working directory** with its declared files, stdin closed (end of file, as
  today), and is cleaned up afterwards; tests run in parallel without sharing a file. A flagged test's control is
  compiled (the adapter's compile command) in a directory of its own inside the test's.
- **Verdicts:** PASS · FAIL (the output difference; an accepted test that did not translate or run; a flagged test
  whose control compiled but no diagnostic was reported at the marked line) · NOT APPLICABLE (the profile lacks the
  edition, module or element; the flagged test's control did not compile; the test uses one of the profile's extra
  reserved words, which §4.2.10 permits an implementation to add) · NO VERDICT (the compiler or harness crashed or
  exceeded a generous hang guard). NO VERDICT is counted on its own, never folded into PASS or FAIL. A program's exit
  status is not judged (§3.2: it is not standard); its output is.
- **"Uses an extra reserved word" means uses it as a user-defined word.** ISO §4.2.10: "An implementation that
  introduces additional reserved words as nonstandard extensions conforms"; such a word stops translation only where
  a program uses it as a user-defined word. A context-sensitive word in its own context (ISO §8.10: "reserved in the
  specified language construct or context") is a keyword use: GnuCOBOL 3.2 reserves PREVIOUS at -std=cobol2002, and
  `READ ... PREVIOUS` translates there. So the generator writes each manifest entry's `words` as the program's words
  (copied library text included) that are none of the standard's words (the edition's reserved words, and the
  context-sensitive words, intrinsic function names and compiler-directive words of ISO §8.10-8.12), together with
  every name the program defines (data, condition, file, program, paragraph and section names) that its edition
  does not reserve; a runner intersects them with the profile's list and reads no program. The entry also names its
  `clause` folder, which the results total by.
- **Results** as JUnit XML and JSON, totals per edition and per clause, and the CHANGES against the compiler's
  recorded baseline (`baselines/`), so a second compiler's run is a regression net rather than a permanently red leg:
  a regression (a recorded PASS lost), a new FAIL or NO VERDICT, any other change, an improvement, and a recorded test
  that did not run. A run is RED when a verdict differs from its recorded one (or has none) and is FAIL or NO VERDICT,
  or was a recorded PASS; with no baseline, any FAIL or NO VERDICT is RED. `--record-baseline` writes a whole run's
  verdicts (never a `--test`-narrowed one). `run.py` executes the per-edition suites; the `wiseowl` manifest and its
  refusals are the in-process xUnit runner's (S1c).
- **In WiseOwl's own pipeline**: the xUnit runner in `Cobol.Net.Tests.Conformance` executes the same manifests
  in-process (fast) and also checks `wiseowl/diagnostics.json`; CI also runs `harness/run.py` with the WiseOwl
  adapter and a drift test requires the two runners' verdict sets to be equal. The output normalization exists in
  both languages by nature (`CutRunner.Normalize` in C#; LF, per-line trailing-space trim) and both are pinned to
  one shared fixture. The GnuCOBOL adapter ADDS a signal to the per-batch battery; it does not replace the GnuCOBOL
  differential (`scripts/gnucobol_differential.py`), which runs GnuCOBOL's own testsuite through WiseOwl, the other
  direction.

## 6. Migration

0. **First, as its own wave: the inventory's `editions` field.** One spelling, and each empty row decided
   from the standard. These are adjudications, not mechanical edits, so they do not ride in step 1.
1. **One mechanical, behaviour-neutral move (one change).** A converter moves every program, with the files it uses,
   into `conformance-suite/cases/<clause>/`, the clause taken from the rows that cite it (a program cited from several
   clauses goes to the first; one cited by none goes to `cases/_unassigned/`), and writes its header from what is
   known: `rules` from those rows; `accept` = its current directory's edition only; `source-format` for EVERY test,
   from the same detector the runner uses today; `files` from its COPY statements and data files; `portable:
   unverified`. Negative programs keep their `reject-at` editions and their `.err` expectation, code or message
   substring, in `wiseowl/diagnostics.json`; they become flagged tests only in their clause pass (step 2), when a
   control is written. **Programs that witness documented non-support** (every citing row DOCUMENTED-NON-SUPPORT, or a
   non-support diagnostic in the `.err`: the 84 negatives among them) go to `wiseowl/cases/` (§4), never to
   `cases/`; `DocumentedNonSupportWitnessTests` and `DeclinedFacilityTests` keep pinning them there.
   The same change rewrites the inventory's 7,928 `conformance:` citations and the ref grammar in
   `inventory-schema.json`; replaces `CorpusRunnerTests` and `CorpusManifest` with the manifest runner; re-records the
   arch-oracle baseline (`ArchOracle.cs` enumerates through `CorpusManifest`, kb/Work PB3107); replaces the
   `golden_registered.py` hook's manifest check with the generator's refusal; moves `gate_plan.py`,
   `impacted_tests.py`, `filter_population.py`, `gen_ledger.py`'s corpus row, `session-probe.ps1`, the `guard-*.sh`
   scripts and CI, `.gitattributes`, `.gitignore`, the skills (`land-a-fix`, `new-construct`, the workstream
   templates and lane-1 workflows) and the other 144 consumers (§2's command) to the new layout; deletes
   `tests/conformance/` and its `manifest.json` files (CLAUDE.md rule 4: no wrapper, alias or compatibility layer;
   every consumer moves in the same landing or the guard job is red). Open kb/Work notes that name old paths get the
   new ones in the same change. **Proof of neutrality**, run in the same change before the old tree is deleted: the
   arch-oracle comparison (the same programs at the same editions give the same verdicts, case by case) plus each
   negative's diagnostic.
2. **Clause by clause, the suites are completed.** For each test: run it at every edition after its own on WiseOwl;
   write a control for each negative and turn it into a flagged test (§3.2); apply the portability lint (§3.3). Each
   difference has one of THREE outcomes, decided by the ISO standard, never by the compiler: a real edition difference
   (a per-edition `.out`, or a `flag`); a compiler defect (a kb/Work note); or a non-portable test (rewritten to be
   portable, or kept WiseOwl-only). A test that clears is marked portable.
3. **The 1,066 uncited programs** are given rule ids in their clause's pass; one that witnesses no normative rule
   leaves the suite and stays a WiseOwl-only regression test.
4. **The rows with no portable witness** whose obligation a program can observe get new programs, written from the
   standard by the golden lane.
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
  WiseOwl commit it was verified on. Test ids are stable from the first released version.

## 8. Order of work

1. **Now** (owner: "Pilot now, rest after GAP"): step 0 (the `editions` wave), step 1 (the move) and the pilot.
2. **The pilot** runs §6 steps 2-4 at all four editions on WiseOwl and GnuCOBOL for: clause 11, the Identification
   Division (114 rows, 120 cited programs, 58 of them negative; most rows edition-limited, COBOL-85's PROGRAM-ID beside
   2002's CLASS-ID, FUNCTION-ID and INTERFACE-ID); §14.9.8, the COMPUTE statement (arithmetic and output, the
   portability lint's main ground); and one file statement (`pb355_start_length_past_operand`, which differed on
   GnuCOBOL in the review's sample: ASSIGN mapping and file status). Clause 11 alone has almost no run-time output,
   so it would not measure where the cost is. The pilot measures the cost per test and the share of each of step 2's
   three outcomes; that replaces the earlier rough estimate (4-8 weeks of loop capacity) for the rest.
3. **After zero GAP and zero known defects**: the remaining clauses, as PB3160 campaign waves.

## 9. Drift tests

- The generator's refusals (§4) run in every gate as an audit (`generate.py --check`, one entry of
  `run_gate_legs.py`'s AUDITS and a command of CI's `audits` job), before the gate slot and the build, so a refused
  test costs a red in seconds, never a build.
- Every inventory `test-ref` naming a suite test resolves to a file; every test's `rules` exist in the inventory.
- Every `accept`/`flag` list agrees with `constructs.json`; every flagged test's control exists and is accepted at
  the same editions.
- Every flagged test has a `wiseowl/diagnostics.json` entry, and every entry names an existing test.
- The xUnit runner and `harness/run.py` (WiseOwl adapter) report the same verdict for every test.
- The two output normalizations agree on the shared fixture.
- The completeness report (§4) is printed by the generator and shown on the ledger per edition.

## 10. Rejected alternatives

- **Four physical copies** (each inherited test copied into every later edition's folder): four copies drift, and a
  hand-maintained duplicate is what CLAUDE.md rules 4 and 5 forbid.
- **Keep today's layout and run forward**: cheapest, but the tests stay WiseOwl-specific (COBOLNET codes, the xUnit
  harness), so no other compiler could run them.
- **A separate repository**: every compiler fix would become a two-repository change.
- **A .NET harness**: does not run on z/OS.
- **Self-checking programs in the NIST style**: more code per test for no gain over output comparison.
- **Must-refuse negatives (revision 1's `reject`)**: the standard requires a warning mechanism, not refusal (§3.2).
- **Vendor diagnostic codes in the tests**: no other compiler could pass a flagged test.
- **The clause in the test id**: a mis-filed test could never move.
- **Two parsers of the test headers (C# and Python)**: two implementations of the suite's semantics; the generator's
  manifest is the one input.

## 11. Open

- The suite's license and product name (kb/Work PB3159).
- The IBM and Rocket adapters are documented but unverified: no access to either compiler.
- GnuCOBOL has no 2023 dialect; its 2023 suite is NOT APPLICABLE until it does.

## 12. Revision 2: the review's findings and what became of them

| Finding (Mythos) | Severity | Disposition |
|---|---|---|
| F1 — the standard requires a warning mechanism, never a refusal | Critical | Adopted: §3.2 `flag`, the adapter's flag-mode and diagnostic line |
| F2 — a negative passes vacuously on a compiler lacking the feature | Critical | Adopted: every flagged test has a control (§3.2) |
| F3 — DISPLAY of numerics is implementor-defined | Critical | Adopted: the portability lint, `implementor-defined` limited to enumerated A.1 values, `portable: unverified`, three outcomes in step 2 (§3.3, §6) |
| F4 — documented non-support would become must-reject tests | Critical | Adopted: those programs go to WiseOwl's regression tree; the success criterion and the completeness report say so (§1, §4, §6) |
| F5 — the move as written is neither neutral nor fully scoped | High | Adopted: `.err` substrings kept, `files`, `source-format` everywhere, the consumers named with the command, ids without the clause, `_unassigned/`, the `editions` wave first, the neutrality proof named (§3.1, §4, §6) |
| F6 — the GnuCOBOL adapter cannot replace the differential | High | Adopted: it adds a signal; per-compiler baselines (§5) |
| F7 — the completeness metric never reaches zero | Warning | Adopted: reported per witness kind (§4) |
| F8 — run-time rejection at 85; abnormal termination | Warning | Adopted (§3.2) |
| F9 — compile-only positives | Warning | Adopted: every accepted test runs; an empty `.out` (§3.1) |
| F10 — working directory, file names, stdin, vendor reserved words, limits | Warning | Adopted (§4 profiles, §5) |
| F11 — clause 11 alone measures the cheap kind of test | Suggestion | Adopted: the pilot adds COMPUTE and one file statement (§8) |
| F12 — counting rules; "`CutRunner.Normalize` does not exist" | Suggestion | Counting rule adopted (§2). The class claim is wrong: `internal static class CutRunner` with `Normalize` is `tests/Cobol.Net.Tests.Conformance/CompilerUnderTest.cs:12` |
| F13 — two runners parsing headers | Suggestion | Adopted: the generator's manifests are the only input to both runners, with a verdict drift test (§5) |
| F14 — free-form header; id and file name | Suggestion | Adopted (§3, §3.1) |

## 13. Implementation slices

Each slice is one kb/Work note in the PB3160 campaign (`cluster: [PB3160]`), ordered by its `blocked_by`; the notes
hold the status, this section the scope. Every slice ships its own tests and drift tests, gates with
`build-local.ps1`, and updates this document where it corrects it (CLAUDE.md rules 2, 5, 6).

- **S0a — one spelling for `editions` (PB3172).** Every row's `editions` is the comma-separated ascending list with no
  spaces (`85,2002,2014,2023`). The rule is data, the schema's `editions` entry (`values` in ascending order and the
  `separator`). `scripts/spec/inventory_schema.py` holds the one validator, `Schema.editions_error`, which refuses any
  other spelling (a space, an out-of-order list, a doubled separator, an unknown or repeated edition), and the one
  writer of the spelling, `Schema.spell_editions`. `record_verdicts.py` asks the validator of every record;
  `build_inventory.py` carries each row's field through the writer (so a rebuild on a merged tree respells a row
  recorded before S0a), as do `normalize_batch.py` (an agent's batch) and `grammar_findings_to_batch.py` (a
  grammar finding); `derive_verdict_batch.py` carries the row's own value. `SpecTraceabilityInventoryDriftTests`
  (`EveryEditionsField_IsTheOneSpelling`) holds the whole inventory to the rule's C# twin, and
  `inventory_schema.py --self-test` proves the rule. Empty stays legal until S0b.
- **S0b — the empty `editions` rows decided (PB3173; 148 on 2026-10-16).** For each, the editions at which the rule exists, derived
  from the standard and the version-change reference (`docs/VERSION_CHANGE_REFERENCE.md`), each with its citation
  checked by `cite.py`; then the validator refuses an empty field. An adjudication slice: an adjudicator decides, a
  refuter checks each decision. **As landed:** each decision names the facility that bounds the rule and its
  evidence (an Annex E or F item through `cite.py`; for an edge below 2023, which the 2023 text cannot quote, the
  compiler's own edition record: a `constructs.json` row, the `IntrinsicCatalog` window, the first edition
  `reserved-words.json` reserves the word at, or a decided sibling row), and the decision script checks every piece
  before it writes the batch; the row's `notes` carry the decision. **One design correction:** every row is a rule of
  the 2023 text, so its band always ends at 2023 and has no gap. The schema's `editions` entry now says so as data
  (`required`, `run-through-last`), the one validator and its C# twin refuse any other band, and the 20 rows that
  wrote one (a start edition alone, endpoints, or a band stopping at 2014) were corrected in the same batch, with the
  wrong FORMAT / SELECT WHEN band (85, a clause whose keyword is first reserved at 2002) and the sibling bands
  that contradicted the compiler's own edition windows. A second structural rule: a rule of a clause 15 function
  exists from that function's introducing edition, never earlier, so `EveryFunctionRule_StartsNoEarlierThanItsFunction`
  refuses a band that starts before the `IntrinsicCatalog` window (the function of a section is read from the
  standard's headings). A band that starts later than the window reads the rule's 2023 wording and is allowed.
- **S1a — the generator (PB3174).** `conformance-suite/harness/generate.py`: the one header parser (fixed and free
  form), every refusal of §4, the per-edition manifests and the `wiseowl` manifest, the completeness report by witness
  kind (§4), `--check` (the refusals only, writing nothing) as a gate audit (§9), `--self-test` (discovered by
  `scripts/self_tests.py`). Built and proved on a seed of tests written from
  the standard into `cases/` (an accepted test, a per-edition output, a flag test with its control, an A.1-valued
  test, a test with a copybook), never on the old corpus. **As landed:** six seeds, each probed on WiseOwl at all four
  editions (`14.9.8-COMPUTE`: a multi-receiver size error, the alphanumeric-receiver flag test of §14.9.8.3 SR 1 and
  its control; `14.9.30-READ`: READ PREVIOUS after OPEN on an indexed file, the 2023 at-end beside the first record
  E.2 item 22 records for 2002 and 2014; `7.3.25-TURN`: location information without LOCATION, A.1 item 204, two
  values; `7.2.3-COPY`: a copybook). Three corrections to this design, written above: the `constructs` field (§3.1),
  because no reader short of a compiler can tell which registry constructs a program uses; the fixed-form marked line
  in the sequence number area (§3.2); and the lint's figurative, reference-modification, `UPON` and margin rules
  (§3.3). The completeness report is printed and written to `suites/completeness.json`. It is not yet on the
  ledger (§9's last bullet): the ledger's content is the owner's (kb/Work PB2912), so the S1a report raised it as a
  lead for its own note rather than adding a section to the page.
- **S1b — the harness (PB3175).** `conformance-suite/harness/run.py` with the adapter, profile and baseline formats
  of §5, the WiseOwl and GnuCOBOL adapters and profiles, a fresh working directory per test, stdin closed, the
  verdicts and their JUnit XML and JSON, `--record-baseline` and the CHANGES report; `--self-test` drives a stub
  compiler (a Python script) through every verdict, so the logic is tested without `cobc`. Runs on Windows and
  Linux. **As landed:** `harness/run.py` (the runner, its results and CHANGES, a 58-arm `--self-test` passing on
  Windows and under WSL) and `harness/config.py` (the formats); `adapters/` and `profiles/` for `wiseowl` and
  `gnucobol`; `baselines/gnucobol.json` recorded on the S1a seed under WSL (14 PASS, 6 NOT APPLICABLE at 2023, and 2
  FAIL: READ PREVIOUS after OPEN is at end at -std=cobol2002 and -std=cobol2014, the 2023 rule, where E.2 item 22
  records that those editions retrieved the first record); the seed PASSes 22 of 22 on WiseOwl. Three corrections to
  this design, written above: the manifest entry's `words` and `clause` (the generator reads the program, so no runner
  does), since a word list that held keyword uses made the READ PREVIOUS seed NOT APPLICABLE on GnuCOBOL, whose
  -std=cobol2002 reserves the context-sensitive PREVIOUS; profiles carry their evidence, and WiseOwl's is held to
  `docs/CONFORMANCE.md` by a drift test; and the generator's audit, not a second one, checks every adapter, profile
  and baseline. On the way, the `cobol` command's exit code for a source the binding phase refuses (a syntax rule the
  binder checks, an edition diagnostic) became 65, like a parse error, where it had fallen to 70, the code of the
  compiler's own failure.
- **S1c — WiseOwl's manifest runner (PB3176).** An xUnit runner in `tests/Cobol.Net.Tests.Conformance` that executes
  the generator's manifests in-process (accept: compile, run, compare through `CutRunner.Normalize`; flag: the
  diagnostic at the marked line; `wiseowl/diagnostics.json`: the refusal and its code or message), and the drift
  tests of §9: equal verdicts with `run.py` on the WiseOwl adapter, and the two normalizations on one shared fixture.
- **S2 — the move (PB3177).** §6 step 1, in one landing: the converter (committed on the branch for review, deleted
  before it lands: the move is its output), the moved tree, `wiseowl/cases/` and `wiseowl/diagnostics.json`, the
  inventory's citations and ref grammar, every consumer of §2's command, the arch-oracle baseline, the hook, CI, the
  skills and the open notes, `CorpusRunnerTests`/`CorpusManifest`/`tests/conformance/` deleted, and the neutrality
  proof's evidence in the note. `tests/conformance/README.md`'s authoring guide moves to `conformance-suite/README.md`,
  and `docs/VERSION_TEST_MATRIX_DESIGN.md` §5 (the corpus layout and its replacement paragraph) is rewritten to
  describe the moved tree (CLAUDE.md rule 6).
  **It lands at a quiet point, so no branch crosses it.** A branch cut before the move that adds a test under
  `tests/conformance/` would merge without a conflict (the directory is gone) and its test would silently never run.
  Which branches those are is known when the move is ready, so the move is sequenced around them, never left for a
  gate to find: once the move's branch is reviewed, the operator steers the loop's drain (`plan_wave.py --drain`: no
  new fix wave starts, the started branches land); the move's branch then merges main, re-runs its converter on the
  merged tree (the converter stays on the branch until this point), repeats the neutrality proof, and is reviewed for
  that delta and landed; the drain ends and every later branch is cut from a main that has only `cases/`. The cost
  is the fix lane's idle time between the drain and the landing, never a gate spent on a known failure. The move also
  adds one refusal to the generator, a file under the removed `tests/conformance/` path, for a branch the drain does
  not hold (an attended or Mythos branch): it is a gate audit (§9), so such a branch is red in seconds, before the
  build, and the fix is moving the file into `cases/`.
- **S3 — the pilot (PB3178).** §8: clause 11, §14.9.8 and `pb355_start_length_past_operand` through §6 steps 2-4 at
  all four editions on WiseOwl and GnuCOBOL; GnuCOBOL's baseline recorded; the cost per test and the share of each of
  step 2's three outcomes measured and written into PB3160 and §8, with the estimate for the remaining clauses.
- **S4 — the release archives (PB3179).** §7's CI job, held until the owner chooses the license (kb/Work PB3159).

The remaining clauses are filed as campaign notes from S3's measurement, held until zero GAP and zero known defects
(§8).
