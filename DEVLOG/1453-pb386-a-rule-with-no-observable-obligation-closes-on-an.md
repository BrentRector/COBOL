## Entry 1453 — 2026-09-04 15:52 PDT — PB386: a rule with no observable obligation closes on an owner-signed DERIVATION, checked three ways

The owner opened the one door `DESIGN-spec-conformance-review.md` §1 says an agent may not: a rule the standard
leaves with **no observable obligation** may close on a checkable, owner-signed determination recorded in
`docs/CONFORMANCE.md` §8, in place of §1(c)'s spec-derived test. Eight rows qualified — every
CONFORMS-but-untested row on the tree except `DOC-A.1-93`, which is `PB383` and a real defect — and the
population was measured before the door opened rather than after.

`derivation` is now a sixth adjudicated inventory field carrying `docs/CONFORMANCE.md#DRV-<rule-id>`, COMPUTED
from the row's own rule-id exactly as a `kind: DOC` row's §7 anchor is, so a determination filed against a
neighbouring rule cannot be spelled. Three arms, all data in `inventory-schema.json`. The `undefined-A.2` arm is
**mechanical**: Annex A.2's 66 items each end in the standard's own citation of the rule they are about, so
`scripts/spec/extract_annex_a2.py` resolves item → `rule-id` into a generated artifact (66 items, 52 citing a
numbered rule, 61 distinct rule-ids) and the checker requires the named item to cover THIS row, which makes the
ninth A.2 row cost one register row and no list edit. Naming a real A.2 item about a different rule is refused —
driven for real by sabotaging §8's item-4 cell to `A.2 item 41` and watching both engines fire with the same
code, in the implementer's worktree and again in the lander's. The other two arms are reviewed arguments whose
SHAPE is checked (the closed set must be stated; the indistinguishable rule must exist and not be this one) and
whose reader is the owner, recorded by the signature.

Six refusals live in ONE predicate that the writer prints and the gate asks — including the owner's explicit
bound that a row already carrying a spec-derived test may not close on a derivation, because such a row
demonstrably HAS an observable obligation. `Schema.state_for` and its C# twin `DerivedState` learned the
evidence kind in this one change set (`PB315`'s lesson), and a 19-case parity fixture over a fabricated world
holds them to the same `state` AND the same refusal CODES — codes rather than a boolean, because two engines
refusing one row for two different reasons look identical under "it was refused", which is exactly what PB315
hid behind for months.

`PB280` Q2 is not reversed, and the coexistence is encoded by ORDER rather than by a special case: a kind's
anchor and observability costs are charged BEFORE the derivation is consulted, so an anchor-only DOC row still
computes GAP with a valid derivation attached, and a DOC row §7 does not document may not carry one at all.
`DOC-A.1-19` is admitted because its determination IS stated and it names greenfield code — only its witness is
impossible, which golden-lane round 2 discovered by writing that witness and deleting it.

Three second arms came out of the sweep. `DifferentialOnlyCoverage` would have reported every derivation-closed
row as "closed on differential evidence alone"; `audit_annex_a1.check_pins` keyed its §7-agreement demand on a
spec-derived `test-ref`, so a derivation-closed A.1 row would have dropped out of the check at the moment it
began moving the burn-down; and `normalize_batch.ALLOWED` was a second copy of `ADJUDICATED` that would have
rejected, whole, every batch the writer accepts. The `DOC-A.1-<n>` token invariant also had to learn the second
register, and its `--self-test` gained the pair that proves the widening did not blunt it. Both markdown
register parsers were reading to end-of-file — indistinguishable from reading the section only while §7 was
last — and are now bounded at the next `## `.

One inherited citation was caught by the tool rather than by reading, on both passes: the licence sentence *"A
COBOL run unit that allows these situations to happen is a conforming run unit"* is **§4.4 2)**, not §4.2.16.
`--check 4.2.16` fails; `--find` gives the real clause. It is cited correctly in the design doc, the schema and
§8.

The landing pass added one finding of its own. Three of the eight rows' `notes` still asserted the state this
decision reverses — *"the row stays a GAP"*, *"STAYS TEST-NEEDED"*, *"CLOSING THIS ROW NEEDS AN OWNER
ADJUDICATION"* — so the inventory would have shipped a row that closes the GAP while its own note says it
cannot. Each now carries a closing sentence in the file's own `||` continuation convention naming the §8
determination that answers it, appended rather than rewritten so the adjudication history stays readable.

**GAP 3021 → 3013** on the rebased tree (the implementer measured 3054 → 3046 against a base two
landings have since moved). The eight were the WHOLE of the CONFORMS-but-untested band as measured before
lane-3 batch 2 — nine rows, the ninth being `DOC-A.1-93` = `PB383`, closed by its own fix an hour earlier — so
none of that population survives; the band's current 46 rows are batch 2's test-needed CONFORMS, already
tracked in `PB370`. No diagnostic code claimed — `COBOLNET1728` stays free. Wave-local gate 55/55; whole Unit assembly
5301/5303 with both reds the known absent-external-corpus shape; Characterization 33/33; Conformance 5575/5575;
every `--check` and `--self-test` green, and both sabotage runs proven red before being restored.

