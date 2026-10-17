## PB3173: every inventory row names its editions, and a band runs through 2023

Conformance-suite slice S0b (kb/Work PB3173; `docs/rearchitecture/DESIGN-conformance-suite.md` §6 step 0, §13 S0b),
on PB3172's branch. A per-edition suite needs to know which editions each rule belongs to, and 148 inventory rows
named none.

**Re-probe.** 148 rows had an empty `editions` field on the merged tree: 85 CONFORMS and 63 DOCUMENTED-NON-SUPPORT.
Of these, 66 are Annex A.1 items and 82 are SR, GR, AR, RV or FMT rules. That matches the count the design records.

**How the rows were decided.** `adjudication/pb3173-editions/decide.py` gives each row a band and a reason. A reason
names the facility whose existence bounds the rule, plus the evidence for the edition that introduced it. The
script checks every piece of evidence before it writes the batch, and it refuses a band that does not start at its
evidence's edition:

- **2023 introductions** are cited from Annex E through `cite.py --check`: asynchronous messaging (E.3.2 item 1),
  commit and rollback (item 2), DELETE FILE (E.3.3 item 15), MODULE-NAME (item 28), SMALLEST-ALGEBRAIC (item 29)
  and PUSH/POP (item 38). STANDARD-BINARY is cited from Annex F.2 item 3: obsolete, but still in the standard.
- **Edges below 2023** cannot be quoted, because the repo holds no 2002 or 2014 text. They come from the compiler's
  own edition record instead: a `constructs.json` row, the `IntrinsicCatalog` window, the first edition that
  `reserved-words.json` reserves a word at, or a sibling row that was already decided.

98 evidence items were checked and all were OK. Each row's notes carry its decision and its checked citations.

**Design correction.** The catalog is extracted from the 2023 text, so every row is a 2023 rule. Its band therefore
ends at 2023 and has no gap. Three changes enforce this:

- The schema's `editions` entry now states it as data: `required` and `run-through-last`.
- `Schema.editions_error` and its C# twin `EditionsError` refuse an empty field and any band that is not a run
  through 2023, and name the band to write.
- `record_verdicts.py` asks the validator about the row as it will be written, so a witness-only record keeps the
  row's band. `grammar_findings_to_batch.py` reports a finding with no band as a problem.

The sweep found 20 rows that broke the rule:

- a start edition written alone: `2002` on nine constant-entry rows, and `85` on six COLUMN and INSPECT rows;
- endpoints written as a list: `85,2023` and `85,2002,2023`;
- `85,2002,2014` on three CLOSE, OPEN and USE rules that the 2023 text states.

It found 36 more whose band contradicted the evidence a decided row uses:

- 31 FORMAT and SELECT WHEN (A.4.8) rows at `85`, although FORMAT is first reserved at 2002;
- GR-13.18.13.4-5, which is conditioned on FORMAT;
- two NUMVAL-F rows at 2014, although the compiler's window is 2002;
- MOD's AR-15.64.3-1 at 2002, although MOD is an 85 function;
- the standard-decimal arm GR-14.7.5-7 at 2002, although standard-decimal arithmetic is 2014.

All 56 were corrected in the same batch: 204 rows changed, and GAP stayed at 60.

**Lower-confidence decisions** are named in PB3173 so that each can be overturned with a single record:

- the split-key SOURCE phrase (2002);
- the A.4.8 module's 2002 edge;
- DOC-A.1-34 and -37 (2002);
- DOC-A.1-188 (2002);
- DOC-A.1-202 (2014).

**Branch review round 1** (kb/Work PB3027) returned three findings, all fixed in `decide.py`. The script re-ran
against the pre-batch tree (8eca73497, extracted with `git archive`) and wrote 215 records; the 203 it already wrote
are byte-identical, so the change moved exactly 12 rows:

- R1-1: SR-13.4.5.3-6 and SR-14.9.27.3-9 are wholly FORMAT-clause rules but were left at 85. Both are now 2002, like
  the other A.4.8 rows. A sweep of every row whose rule text names FORMAT or SELECT WHEN found no other such row.
- R1-2: DOC-A.1-112 documents the LEAP-SECOND directive's General rule 4, a 2002 rule, but had been decided at 2014
  under the time-forms reason. It is now 2002.
- R1-3: nine clause 15 rules started before their function exists, such as ABS's at 2002 (ABS is 2014) and
  TEST-NUMVAL's at 85 (TEST-NUMVAL is 2002). Each now starts at its `IntrinsicCatalog` window. The new
  `EveryFunctionRule_StartsNoEarlierThanItsFunction` refuses this shape from now on. It reads each section's
  function from the standard's headings through `SpecClauseText.FunctionSections`, which replaces two private
  copies of the same regex. On the reviewed inventory it named exactly these nine rows.

**Witnesses.** No goldens were written: this is a tooling and adjudication slice with no COBOL behaviour.

- `inventory_schema.py --self-test` gains an empty field, a gap, an inner gap, an early stop and a lone start
  edition.
- `grammar_findings_to_batch.py --self-test` gains a finding with no band and a finding with a gap.
- `SpecTraceabilityInventoryDriftTests.TheseChecks_ActuallyFail_OnAFabricatedInventory` gains the same four C#
  cases, and `EveryEditionsField_IsTheOneSpelling` now holds the whole inventory to the stricter rule.

**Gate.** The first run (20261017T011355Z-6839cd) was RED on its LINE ENDINGS audit: `decide.py` wrote its batch
without `newline=`, so the file checked out with CRLF. The writer now passes `newline` as LF. The re-gate
(20261017T011528Z-adddfa) reads `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to
CI's whole-population run: Conformance 6,046 · Unit 1,010 · Characterization 31`. In `leg-1-Unit.trx`, both
`EveryEditionsField_IsTheOneSpelling` and `TheseChecks_ActuallyFail_OnAFabricatedInventory` read Passed.
`semgrep/verify.py` passes with no count raised.
