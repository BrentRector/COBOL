# kb/Work PB3173 — the inventory's empty `editions` rows decided (conformance suite slice S0b)

Wave 1104 C2, 2026-10-16. The design is `docs/rearchitecture/DESIGN-conformance-suite.md` §6 step 0 and §13 S0b.

- `decide.py`: every decision, as a band and a reason. A reason names the facility that bounds the rule and the
  evidence for that facility's introducing edition. The script checks each piece of evidence before it writes the
  batch: `cite.py --check` for an ISO citation, then a `constructs.json` row, the `IntrinsicCatalog` window, the
  first edition `reserved-words.json` reserves a word at, or a decided sibling row. A band that does not start at
  its evidence's edition is refused. `CORR` holds the sibling corrections (the old band is asserted): 56 from the
  slice's own sweep and 11 from branch review round 1 (two FORMAT-only rules, nine clause-15 rules that started
  before their function exists), which also moved DOC-A.1-112 to the LEAP-SECOND directive's 2002.
- `batch.json`: the record_verdicts batch it wrote (215 records), applied with
  `python scripts/spec/record_verdicts.py adjudication/pb3173-editions/batch.json`.

The script runs against the tree BEFORE the batch (PB3172's merge, 8eca73497), because its first check refuses a
row that is no longer empty or that no longer carries the old band. To re-run it, extract that tree and run it
from the extraction's root:

    git archive -o pre.tar 8eca73497 tests/version-matrix scripts/spec specs/ISO_COBOL.md src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs docs/rearchitecture/spec-rule-catalog.json
    (extract pre.tar into an empty directory, cd there)  python <worktree>/adjudication/pb3173-editions/decide.py <out.json>

The batch is applied to that tree's inventory. On a later tree, overturn one decision with one record_verdicts
record for that row.
