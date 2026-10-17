## PB3174 — the conformance suite's generator (slice S1a): one header reader, its refusals, the manifests and the completeness report

kb/Work PB3174, design `docs/rearchitecture/DESIGN-conformance-suite.md` §3, §4, §9 and §13 S1a (approved by the
owner 2026-10-14). Wave 1104 C3, on PB3173's branch, so PB3172 (one spelling of `editions`) and PB3173 (every band
decided) land with it: the generator reads those bands.

**Re-probe.** A greenfield slice: `conformance-suite/` did not exist. What it needs from the tree was measured first:
every inventory row has a decided band after PB3173 (4,195 rows); `constructs.json` carries 266 constructs, each
with a window and no link to any test; the test-ref forms are `conformance` (7,920), `unit` (1,779),
`conformance-test` (1,259) and `nist` (16).

**The mechanism.** `conformance-suite/harness/generate.py` is the ONE reader of the test headers (design §10: never
two parsers). It parses the metadata block that opens each test, in fixed form (`*>` in columns 7-8, a comment line
at every edition) and free form (`*>` in column 1), and refuses: an unknown field or rule; an id unequal to its file
name or shared by two tests; editions out of order; an accepted edition outside a rule's band or a named construct's
window; a flagged test without a control accepted where it is flagged, without exactly one marked line, or
witnessing a general rule without `why`; a mark on a test that is not flagged; a run-time expectation (a TURN
directive, an `EC-` name, an exception function) at 85; free form at 85; an undeclared COPY, in the program or in a
declared copybook; a missing or unused expected output; an A.1 item or `requires` name the registries lack; a test
filed under a clause none of its rules belongs to; a stray file or folder; a `wiseowl/diagnostics.json` entry naming
no test or refusing an accepted edition, and a flagged test without one; and, in a test the suites carry, the
portability lint of §3.3. It writes `suites/<edition>/manifest.json` (the cleared tests of `cases/`),
`suites/wiseowl/manifest.json` (everything WiseOwl runs, with its refusals) and `suites/completeness.json`, all
git-ignored build output, and prints the completeness report: each row of an edition counted once under its first
witness kind (documented non-support, suite test, determination, derivation, WiseOwl-only, none), and the suite's
own measure, the observable rows a suite test witnesses. At 2023 today: 8 of 3,704 observable rows, the seed.
`harness/cobol_source.py` reads the program-text facts the refusals need (the reference format, tokens, COPY, the
class of each data item, the DISPLAY operands) and refuses what it cannot classify rather than guessing.

**Wiring.** `--check` (refusals only, about 0.2 s) is the gate audit `CONFORMANCE SUITE` in `run_gate_legs.py`
AUDITS and a step of CI's `audits` job, so a refused test is red before the slot and the build (§9). `--self-test`
(68 arms) is discovered by `scripts/self_tests.py`, whose discovery trees became `ROOTS = ("scripts",
"conformance-suite/harness")`; `SelfTestDiscoveryDriftTests` reads that tuple for its net and names the generator
among the known self-tests.

**The seed**, six tests written from the standard and each probed on this branch's `cobol.exe` at all four editions,
every output as the standard gives it: a COMPUTE whose small receiver keeps its value on a size error while the large
one is stored (§14.7.5); the alphanumeric-receiver flag test of §14.9.8.3 SR 1 (WiseOwl: COBOLNET1675 at the marked
line, recorded in `wiseowl/diagnostics.json`) with its control; READ PREVIOUS after OPEN on an indexed file (at end
at 2023 by §14.9.30.4 GR 21 d) 3; the first record at 2002 and 2014, the rule Annex E.2 item 22 records); location
information without LOCATION (A.1 item 204, one output per enumerated value); and a copybook. The generator refused
one of them on its first run, a comment line past column 72, and the line was rewrapped.

**Design corrections, in the design.** A `constructs` field (§3.1): the registry check needs the test to name the
constructs it uses, because no reader short of a compiler can find them. The fixed-form marked line opens the
sequence number area (§3.2): COBOL-85 has no floating comment and margin R is implementor-defined after 85 (ISO
§6.3.1), while the sequence number area takes any character at every edition (§6.3.2). The lint's figurative,
reference-modification, `UPON` and margin rules (§3.3). `implementor-defined.json` as the registry of the A.1 items
whose values the standard enumerates. The manifests' contents and the order of the witness kinds (§4).

**Rows.** None: test infrastructure (`closes_rows: []`, the note's reason). PB3175 (S1b) is unblocked.

**Self-review.** One mechanism per job (the header and refusals in `generate.py`, program text in `cobol_source.py`);
the edition spelling reuses the inventory schema's one writer (`spell_editions`) and the evidence predicates
(`is_spec_derived`, `derivation_stands`, the §7 register), so no rule is copied. Silent failures looked for: an
unreadable header, an unclassifiable DISPLAY operand and an unknown registry name are each refused, never skipped.

**Not done:** the completeness report on the ledger (§9's last bullet), whose content is the owner's (kb/Work
PB2912); raised as a lead in the wave report.

**Gate:** GATE-VERDICT-PENDING
