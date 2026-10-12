## Entry 1165 — 2026-08-04 01:34 PDT — I tested my own triage signal against all eleven clauses and it was false; the payoff was the §15.4 parse gap that had been unowned for months

**Yesterday I invented a triage signal for the 100 unharvested clauses and wrote it into the manifest: a
single ascending list is weak evidence FOR admitting a clause, nested sub-lists weak evidence for a prose
enumeration. Today I read all eleven multi-sublist clauses. Every one of them is normative.**

| clause | what its items actually are |
|---|---|
| **§14.7.5** | *"The size error condition exists in the following cases…"* — the DEFINITION of the size error condition |
| **§15.4.1** | r1 = *"the returned value shall equal the value of the equivalent arithmetic expression"* |
| §9.3.6 · §9.3.8.2.3 · §14.8.2.3.2 · §14.8.2.3.3 · §14.8.3.3 | method-resolution and parameter-conformance rules |
| §14.6.2.3.2 · §14.6.13.1.5 · §9.1.14 | initial-state, exception-object and invalid-key semantics |
| §8.3.2.2 | *"shall be externalized"*, *"shall be referenced … only"* |

Multiple restarting sub-lists mean the clause states SEVERAL rules, each with its own lead-in sentence — a shape
the extractor already represents as `L<sublist>.<ordinal>` ids. It says nothing whatever about normativity.
⛔ **The signal was wrong in the dangerous direction**: used as written, it would have argued for excluding
§14.7.5 and §15.4.1 — the definition of the size error condition, and the rule this session leaned on all day.
The manifest now says shape records LIST STRUCTURE AND NOTHING MORE, expect the upper bound to be a HIGH one,
and READ THE CLAUSE.

⭐ **AND §15.4.1 IS THE ONE THAT STINGS.** I cited "§15.4.1 r1" repeatedly this session — for PB38, for the D3
design correction, for the §15.4.1 r1 argument in three commit messages — as though it were tracked. **It was not
in the catalog at all.** Checking exactly §15.4 / §15.4.1 / §15.4.2 returned **zero** rules.

⭐ **WHICH EXPLAINS A PARSE GAP THAT HAD BEEN OPEN AND UNOWNED FOR MONTHS.** Plan §0 recorded `RV §15.4
(Returned values)` as "PRE-EXISTING and unowned" — a recognised rule-block heading yielding zero rules. It is not
mysterious and it is not a transcription defect: **§15.4's rules live in the §15.4.1 sub-clause**, whose prose
heading `KINDS` does not recognise. It is PB29's defect class, one level down, and the content-keyed scan found
it because the scan asks what a clause CONTAINS rather than what it is called. §15.4 is now a DECLARED empty
block with that reason, so the extractor reports **✓ no parse gaps** for the first time — an expected condition
recorded once instead of warned about forever.

**ADMITTED (2 of the 11, the two best-grounded and most load-bearing):** §15.4.1 as **RV** (§5.3.5 — "Returned
value rules define how the arguments are used to derive the result of an intrinsic function"; its parent §15.4 is
titled *Returned values*) and §14.7.5 as **GR** (§5.3.3 — execution semantics, not syntax). Both citations
`cite.py --check` validated. **Denominator 3,882 → 3,904**; inventory rebuilt with all 421 verdicts carried
forward and 22 new rows; GAP 3,808 → 3,830.

**The other nine stay `pending` deliberately.** Each needs its own kind adjudicated against §5.3, and assigning
nine kinds quickly is how the §8.3.2.2 error happened in the first place. 93 clauses / 417 ordinals remain.

**GATE:** extractor `--check` clean (zero parse gaps) · `build_inventory` carried 421/421 ·
SpecTraceabilityInventory 10/10.

