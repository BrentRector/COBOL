# kb/Work PB3173 — the inventory's empty `editions` rows decided (conformance suite slice S0b)

Wave 1104 C2, 2026-10-16. The design is `docs/rearchitecture/DESIGN-conformance-suite.md` §6 step 0 and §13 S0b.

- `decide.py`: every decision, as a band and a reason. A reason names the facility that bounds the rule and the
  evidence for that facility's introducing edition. The script checks each piece of evidence before it writes the
  batch: `cite.py --check` for an ISO citation, then a `constructs.json` row, the `IntrinsicCatalog` window, the
  first edition `reserved-words.json` reserves a word at, or a decided sibling row. A band that does not start at
  its evidence's edition is refused. `CORR` holds the 56 sibling corrections (the old band is asserted).
- `batch.json`: the record_verdicts batch it wrote (204 records), applied with
  `python scripts/spec/record_verdicts.py adjudication/pb3173-editions/batch.json`.

The script ran once, from the worktree root, against the tree before the batch (PB3172's merge, 8eca73497). It is
kept as the record of how each band was decided. It cannot be re-run on a later tree: its first check refuses a
row that is no longer empty or that no longer carries the old band. To overturn one decision, write one
record_verdicts record for that row.
