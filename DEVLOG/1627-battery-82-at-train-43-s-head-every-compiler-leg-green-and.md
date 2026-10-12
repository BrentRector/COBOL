## Entry 1627 — 2026-09-21 14:08 PDT — Battery #82 at train 43's head: every compiler leg green, and the ONE per-case differential flip is the divergence-adding shape — read from the case, licensed by §14.9.28.3 SR11, re-baselined; plan §9 reference moves to #82

Battery #82 ran at `c774def44` — landing train 43's head — in an isolated worktree already pinned at exactly
that commit, never on `main` and never in the shared checkout. The gitignored GPL GnuCOBOL corpus was copied in
FIRST, so `ExternalCorpusPopulationDriftTests` and the rest of the Unit assembly's external-population tests
measured a real corpus instead of being red by absence. One `bash scripts/battery.sh` invocation, **1080 s
wall**, artifacts in the session scratchpad (`summary.txt`, `conformance.trx`, `unit.trx`, `guard.log`,
`guard-witnesses.log`, `citations.log`, `gnucobol.log`, `gnucobol-report.json`, `gnucobol-rebaseline.log`,
`gnucobol-rebaseline.json`).

**Every compiler leg is GREEN.** Conformance **7709 / 7709** (0 failed, 12 m 17 s), Unit **24365 / 24365**
(0 failed, 2 m 48 s), characterization **33 / 33**, the guard's evidence-rule witnesses `ALL GREEN` and the
compiler-identity watchdog `ALL GREEN`, NIST **364 MATCH / 0 REGRESSION(S)** against the shipped `cobol`
compiler with `NIST AUDIT: CLEAN`, `guard-fast` verdict `ALL GREEN`, and all three static citation audits at
zero — `audit_code_citations` 0 findings, `audit_doc_citations` 0 MISFILED, `audit_evidence_supersession` 0
UNMARKED. The two test populations reproduce the train-43 lander's own whole-assembly gate **exactly** — 7709
and 24365 on both runs, on two different worktrees and two different builds — which is the strongest thing a
battery can say about a lander's numbers: they are not an artifact of that lander's tree.

**The differential carries ONE per-case flip, and it is the shape a regression takes.** `run_file:3586`,
`AGREE_ACCEPT → WE_REJECT_THEY_ACCEPT`, on COBOLNET2186 — the PERFORM-range-across-declaratives screen PB433
landed in this very train (cluster 3). A divergence-ADDING flip is exactly what a regression looks like in this
oracle, so it was not attributed from the note; it was read from the case and then derived from the standard.

The case is GnuCOBOL's `DECLARATIVES procedure referencing (multiple)`. It declares **two separate declarative
sections** inside DECLARATIVES, each with its own USE AFTER ERROR procedure, and then writes a single
`PERFORM <first> THRU <second>` from a non-declarative section — so procedure-name-1 and procedure-name-2 name
procedures in DIFFERENT declarative sections. **§14.9.28.3 SR11**: "When procedure-name-1 and procedure-name-2
are both specified and either is the name of a procedure in the declaratives portion of the procedure division,
both shall be procedure-names in the same declarative section." ⛔ That clause number was **re-derived**, not
inherited from the diagnostic text or from PB433's note: the sentence was located in `specs/ISO_COBOL.md` by
search and then validated with `python scripts/spec/cite.py --check 14.9.28.3 "…"`, which answers
`OK §14.9.28.3 11) (Syntax rules)` — CLAUDE.md rule 1's failure mode is inheriting a §, and a citation nobody
ran `--check` on is not a citation. `§14.9.28.4` GR4 (`--check` OK, rule 4) is the second half of the argument
and the reason the screen is an ERROR rather than the §4.2.2 warning: GR4's specified set of statements runs
from procedure-name-1's first statement to procedure-name-2's last, and across that boundary the intervening
procedures belong to a different USE procedure or to none, so there is no conforming meaning left to compile.
Before PB433 the source compiled clean at every `--std` and recursed until the CLR killed the run unit.

**GnuCOBOL's acceptance is their latitude, not the standard's.** Their own case invokes the compiler with
`-fno-section-exit-check`, and the comment above it in the case file says the check is disabled because
something leaving the section is "guaranteed to happen with this PERFORM THROUGH". They know the shape is
anomalous and suppress their own diagnostic for it; the standard does not license the acceptance, it forbids
the source. **So the standard licenses the new answer, and the row was re-baselined** —
`gnucobol_differential.py … --write-baseline` on the same tree, exit 0. The re-run reproduced the **identical**
four totals (WE_REJECT_THEY_ACCEPT 579, AGREE_ACCEPT 467, AGREE_REJECT 236, WE_ACCEPT_THEY_REJECT 41), so the
leg is deterministic on this host, and the resulting diff of `tests/external/gnucobol-verdict-baseline.tsv` is
**exactly one row**, tier column unchanged at `DEFAULT_DIALECT`, one insertion and one deletion, zero other
churn.

⛔ **The complement was MEASURED, because a screen is evidence about what it fired on and never about what it
passed over.** `python scripts/corpus_sweep.py --codes COBOLNET2186` over the differential report reports the
population first (`external population OK: 1323 case(s) == 1323 baseline row(s)` — the corpus did not move
under the diff) and then finds COBOLNET2186 on **one** case across 1323 compiled cases and 1611 extracted
programs. A pattern sweep for `PERFORM <name> THRU` finds 18 external programs, eight of which write it
anywhere after a DECLARATIVES header; the seven that are not `run_file:3586` were **already**
`WE_REJECT_THEY_ACCEPT` before this train, for unrelated reasons (USE FOR DEBUGGING, the report writer), and
not one of them moved. The screen fired where SR11 is violated and nowhere else, and it refused nothing legal.

⛔ **Battery #81's record required #82 to be "ALL GREEN with 0 flips". It has one, and that is written down as
a miss rather than rounded into the green.** `scripts/battery.sh` exits 1 and prints
`=== BATTERY: NOT GREEN (rc=1) ===` on any non-zero flip count by design — the script cannot tell a licensed
flip from a regression, only a person reading the case can — and that is the only reason for the non-zero exit;
no other gate in the run is anything but green. There was no corpus drift, no case without a compiler verdict
and no harness failure, so the one flip is the whole delta.

**Batch covered:** everything since battery #81's head `c701786d` — DEVLOG **1625**, landing train 43's six
clusters and twenty-eight notes (SET-TO-FALSE and the SET formats · INITIALIZE value occasions · exception
checking and PERFORM ranges · the data-description screens · the MOVE operands · citation fidelity), plus
registrar #7 and battery #81's own record, which touch no compiler source. Six new diagnostic codes
(COBOLNET2176, 2177, 2186, 2187, 2191, 2196), GAP **2339 → 2310**. Nothing was filed in `kb/Work/`: no red went
unattributed, and PB433 was already `landed` when train 43 closed, so no note's status moves.

⚠ **Owed and not done here:** the Conformance Ledger artifact refresh (`python scripts/spec/gen_ledger.py` plus
a publish to the ledger artifact's existing URL) for this battery close. It is recorded here and in the commit
message rather than left silent, because the owner's standing instruction is to refresh after every
GAP-moving landing and every battery close.

