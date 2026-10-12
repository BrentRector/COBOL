## Entry 1428 — 2026-09-02 16:58 PDT — Annex A.1's 222 documentation obligations become inventory rows that can carry a verdict, and the anchor that files each one is COMPUTED — plus a back-fill close the orchestrator refused, because a test that names the right function still has to assert the claim

`kb/Work/A11` has said since 2026-08-03 that §7 of `docs/CONFORMANCE.md` already carried determinations the
burn-down could not see. It could not see them because there was no way for a DOC row to carry a verdict at all:
all 222 `DOC-A.1-*` rows sat `verdict: ""` / `state: GAP` while §7 held 39 written determinations — a third of
that work invisible. This lands the shape that makes them countable, and the first 37-record back-fill against it.

**The row kind, and why the anchor is computed rather than typed.** `inventory-schema.json` gains a `kinds`
object; `kinds.DOC` carries an `anchor-template` (`docs/CONFORMANCE.md#{rule-id}`) and an
`implementation-pattern`. An Annex A.1 row is not a behavioural rule — it is an obligation to SPECIFY and to
DOCUMENT an implementor-defined element (ISO §4.2.5: "the implementor shall specify, at a minimum, the
implementor-defined language elements that are identified as required"), so a binder symbol alone cannot
discharge it and a §7 determination can. The anchor EXPANDS THE ROW'S OWN RULE-ID, which is the structural answer
to A11's original finding: the §15.3.3.2 fractional-seconds determination had been filed under item **87**
(FORMATTED-CURRENT-DATE's accuracy) when it belongs to item **202**. The audit could *detect* that; a computed
anchor makes it **unspellable**. §7 is re-keyed from bare numbers to `DOC-A.1-<n>` for all 41 rows — the old
`#7`, which five live rows used, resolves against the digit 7 anywhere in a 790-line document and proved nothing.
Eight of the nine rows citing `CONFORMANCE.md` were repaired onto their own item anchors (`#7` → 133, 134, 136,
137, 134; three bare paths); `RV-15.31.3-L2.3`'s `#4.2.16` stays legal, and `code-location.anchored-files` now
makes a BARE path to an anchored file a violation in the writer AND in the gate.

**Observability is read off the row, not off a hand list.** A.1 conditions the documentation duty on provision
("If the element is provided by the implementor…"), so "can a program observe this?" IS "is there greenfield code
providing it?" — a question whose answer is a PATH a refuter can falsify, not an opinion. `implementation-pattern`
applies the SAME `^src/Cobol\.Net\.` predicate the audit's own `sweep_source_citations` uses, and there is exactly
one evaluator per language (`Schema.anchor_for`/`is_observable` in Python, `AnchorFor`/`IsObservable` in C#), so
the register audit and the inventory gate cannot disagree.

**The policy arm is deliberately ABSENT.** A DOC row closes exactly as every other row does — computed anchor
present, a greenfield implementing site, a spec-derived test — so a row with nothing in the compiler to observe
stays a GAP. Letting one close on the register alone would widen DESIGN §1(a)'s definition of DONE, which is the
owner's metric and not an agent's to redefine; it was also measured unreachable (all 37 back-fill rows carry a
greenfield site, so the arm would have had zero instances and its negative claim would have been cross-checkable
for only 13 of 222 items — a MISSING observation standing in for a NEGATIVE one). It is asked as a bare question
instead: **PB280** (kind decision, status owner) holds three — Q1 whether a "Not provided." determination on an
A.1-OPTIONAL item closes CONFORMS or DOCUMENTED-NON-SUPPORT (~30 items ride on it; `DOC-A.1-127` and `-206` are
held out of the back-fill pending it), Q2 the anchor-only close, Q3 whether this repo's `CONFORMANCE.md` IS the
§4.2.16 user documentation and may discharge an obligation by reference.

**The audit stops being a script somebody remembers to run.** `audit_annex_a1.py` gains an escape-aware row parser
(row 82 contains `\|v\|`), a per-token COUNT check (a set check passed on a prose duplicate), the `Pinned by` ↔
inventory `test-ref` agreement check, `--json`, and a `--self-test` grown from 7 cases to 15. It is wired into
`AnnexA1RegisterDriftTests`, which shells it from the Unit project on the repo's own precedent
(`ExternalCorpusPopulationDriftTests` → `corpus_sweep.py`) — so it runs in the wave-local gate, in battery
phase 1 and in both CI unit jobs. Until today it was wired into nothing. Every new gate was driven RED against
the real artifacts and restored: anchor removed; wrong-item anchor; `#7` restored; a policy row asserted OK; a
stale `Pinned by`; a prose `DOC-A.1-N` token in both the duplicate and no-row shapes; the old bare-number key.

**⭐ The back-fill close this landing REFUSED.** The `Pinned by` check exists because one test about item 171 was
closing both 171 and item 87: the first back-fill closed item 87 (FORMATTED-CURRENT-DATE accuracy) on
`SecondsPastMidnight_PinnedClock_ExactTicks`, which is item 171's test, cites §15.80.3 in its own comment and
never calls `FormattedCurrentDate`. The implementer found that and re-referenced item 87 onto
`NowFunctions_OnePinnedClock_OneInstant` — the only method in the tree that does call it — and recorded the
residue honestly: that test renders at `ss.ss`, **two** fraction digits of a pinned `.8124791`, while the
determination claims the clock tick, seven significant digits then zeros. **That is still not a pin.** A test
that names the right function and does not assert the claim cannot close the row, so item 87 is re-recorded
CONFORMS-but-UNTESTED with an EMPTY `test-ref`, its §7 `Pinned by` cell is empty, and the residue sentence stays
in the row, in §7 and in A11. It costs one row of the GAP and buys the invariant. A golden rendering a long
fraction field from a pinned clock is what finishes it.

**Numbers, measured on the merged tree rather than inherited.** 45 records across two batch files (37 A.1
back-fill + 8 `CONFORMANCE.md` anchor repairs): CONFORMS 44 — **18 closed, 19 CONFORMS-but-untested** — plus
1 PARTIAL. **GAP 3636 → 3618 (+18 closed of 4311).** `audit_annex_a1.py --check`: 39 of 222 items carry a §7
determination, **36 of the 199 the standard requires (18.1%)**, 163 obligations remaining, FINDINGS none.
Three prose counts that no longer matched what the tree measures were corrected in the same change set rather
than left to rot: the schema's "20 of the 39" (it is 19 of the 37 recorded), `DOC_INDEX`'s "20 of 199 discharged
/ 179 remain as of 2026-08-09" (36 / 163), and PB280's "19 closed and 18 untested".

**Gate (this worktree).** Build succeeded, 0 Warning(s), 0 Error(s). Unit filtered
`~SpecTraceabilityInventory|~DefectiveRowCoverage|~AnnexA1Register|~ConstructRegistry|~DiagnosticRegistry|~DerivedVerdict`:
`Passed! - Failed: 0, Passed: 28` (population verified by name — `AnnexA1RegisterDriftTests` 3,
`SpecTraceabilityInventoryDriftTests` 12, `DiagnosticRegistryDriftTests` 5, `ConstructRegistryDriftTests` 3,
`DefectiveRowCoverageDriftTests` 3, `DerivedVerdictDriftTests` 2). Conformance `~Corpus`:
`Passed! - Failed: 0, Passed: 1012`. Full Unit: `Failed! - Failed: 2, Passed: 5154, Total: 5156` — the two
`ExternalCorpusPopulationDriftTests` (GPL corpus absent in a fresh worktree; environmental).
Characterization: `Passed! - Failed: 0, Passed: 33`. `audit_annex_a1.py --check` rc=0, `--self-test` ALL GREEN
(15 cases). `scripts/semgrep/verify.py` unchanged against the PB175 baseline (36 / 475). `work.py check` ✓ 341.

