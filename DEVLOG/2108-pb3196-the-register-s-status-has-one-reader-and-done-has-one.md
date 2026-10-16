## Entry 2108 — 2026-10-16 13:51 PDT — PB3196: the register's status has one reader and DONE has one vocabulary

**What was wrong.** The R0 tools read a `kb/Work` note's `status` a second and third time, beside
`work.parse_frontmatter` and `work.TERMINAL_STATUSES`. `scripts/arch/census.py` had its own regex and its own
`CLOSED_STATUSES` of six values, four of which `work.py check` rejects. `scripts/arch/file_census_notes.py` read the
status by raw line prefix in `scan_notes` and again in its sibling `scan_r2`, with the quotes left on. On the C# side,
`WorkRegister.TerminalStatuses` hand-copied the Python tuple outside the parity fixture that keeps the two readers in
step.

**Re-probe (base 7950e00b2).** With `archived` added to `work.TERMINAL_STATUSES`, `census.note_is_open` still answered
OPEN, so a note in that state would have kept a dead artifact classed register-open and its Delete finding would never
have been produced. `status: "open"` came back as the quoted `'"open"'` from both scanners, so `--rerender` skipped a
note the register calls open and `file_r2` would have merged a later finding into a note the register calls landed.

**The fix.** It is one reader and one vocabulary, and every caller changed:
- `census.note_is_open` asks `work.parse_frontmatter` and `work.TERMINAL_STATUSES`. A kb/ file with no frontmatter is
  still an open mention, and `CLOSED_STATUSES` is deleted.
- `file_census_notes.note_status` is the one status reader for `scan_notes` and `scan_r2`. The body-line scan keeps
  only the generator's own `census_ids`, `design_keys` and `r2_*` lines.
- The parity view in both engines reports `terminal`. `work.py parity` now refuses a fixture that leaves any status of
  `work.STATUSES` without a case, or any case without every field. A status added to the vocabulary therefore reaches
  `ClosesRowsBackLinkDriftTests.TheFrontmatterReader_AgreesWithTheFixtureAndWithPython` before either engine can read
  it differently. The fixture gained five cases: a quoted `"retired"`, `half`, `owner`, `blocked` and the
  out-of-vocabulary `superseded`, which is live in both engines. `WorkRegister.Note.IsTerminal` is the primary
  predicate, and `DefectiveRowCoverageDriftTests` lost its private `IsLive` asserts to the fixture.

**Evidence.**
- New self-test arms: census, every status quoted and bare; file_census_notes, a quoted `"open"` note re-renders and a
  quoted `"landed"` R2 note is `already-filed (its note is landed)`.
- Each new arm and the new parity checks were seen to fail: under the old readers, and on the unextended fixture
  (16 findings).
- On the live register the old and new readers agree on all 2,641 tracked kb/ files and on the status of all 2,582
  notes. The change is behavior-neutral today.
- No file the compiler builds changed, so the §4 oracle's emitted C# and diagnostics are untouched.

**Not fixed here.** Two siblings sit outside this wave's declared file set:
- `tests/Cobol.Net.Tests.Unit/EcRaiseSiteRegistryDriftTests.cs` tests "is a landed note" by substring over the whole
  file, body included. It is reported as a lead.
- `file_census_notes.py`'s explicit CRLF writers are kb/Work PB3240's.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261016T201910Z-c440a5; Conformance
6,140 · Unit 33,397 · Characterization 5 ran in leg 1). Leg 1 left the edited parity test and the two self-test drift
tests to leg 2, so they were run by name on the gate's binaries: 13 of 13 passed, among them
`TheFrontmatterReader_AgreesWithTheFixtureAndWithPython`, `CensusSelfTest_DrivesEveryPolicyArm` and
`EachFleetInstrument_PassesItsSelfTest(file_census_notes.py)`. With a wrong `terminal` planted in the fixture, the
parity test fails.

