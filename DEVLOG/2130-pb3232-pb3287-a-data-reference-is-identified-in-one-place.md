## Entry 2130 — 2026-10-16 19:18 PDT — PB3232 + PB3287: a data reference is identified in one place, and a sum counter is counted with the data-names of its spelling

Two R2 findings on `ReferenceResolver` (reference-resolution), one root: an entry that answers "which item does this
written name denote" kept its own copy of the rule.

**PB3232 (silent wrong answer).** An unqualified `TOT-N` that named both a REPORT SECTION sum counter and a
WORKING-STORAGE item compiled clean and bound the counter, so `MOVE 3 TO TOT-N` altered the report total.
`SumCounterFamilyFor` counted the counters alone and asked the ordinary lookup only when the qualifiers reached no
counter. A SUM entry's data-name is a data-name (§13.18.54.4 GR5), so §8.4.2.2.3 SR1 counts it with the ordinary items
the same qualifiers reach in the same source element. The count is now the sum; COBOLNET2145 above one. The table(ALL)
intrinsic argument asks the same method, so it inherits the verdict. Re-probe: reproduced (and `p1` printed the
counter). Corpus scan: none of 50 report programs nor the NIST RW set collided.

**PB3287.** `ResolveForAddressOf` re-sequenced `ResolveImplCore`'s front half by hand and dropped the §13.7.3 SR4
screen and the §8.4.2.2 report: `SET P TO ADDRESS OF L-REC` over an unreferenceable LINKAGE record compiled clean, and an
ambiguous or index-name operand was told it "names none". The front half is now `ReferenceResolver.Identify`; the two
entries keep only their tails (place and ref-mod tail; address tail, whose OCCURS term is the `FixedLevelTerm`
`WindowScopeOf` also takes). `forAddressOf` skips the property/view chain (not a section data item, §8.4.3.11.3 SR1) so it
registers no pending §8.4.3.9.4 operation. `ReportUnidentified` gained the missing constant-name arm.

**Siblings found and fixed in the same file (I9).** The two other entries naming a table rather than a place (the
SEARCH / SORT table operand and the table(ALL) argument) stopped at `FindItem`: `SEARCH L-TABLE` and
`FUNCTION SUM (L-TABLE (ALL))` over an unreferenceable LINKAGE table compiled clean. Both now call
`FindReportedItem` (lookup, report, screen).

**Goldens.** `85/pb3232_sum_counter_qualified_beside_data_item` and `2002/pb3287_address_of_linkage_sr4_admitted`
(positives, expected values derived from §13.18.54.4 GR2/GR3/GR7, §8.4.2.2.3 SR2, §13.7.3 SR4 a)-d), §8.4.3.11.4 GR1);
negatives `pb3232-sum-counter-beside-data-item` and six `pb3287-*`. Drift: `ReferenceIdentificationDriftTests`;
`SubscriptAdmissionDriftTests` counts the `ReadSubscripts` call sites as `Identify` and `ResolveForItem`.

**Self-review.** Architecture: one mechanism per job (the step, the reported lookup), no C# text in a bound node.
Code: sibling arms agree (ResolveForItem screens through PB1249's legs). Performance: `Identified` is a struct, the
`written` text is built only on failure paths, no delegate on the resolve hot path. Duplication: the displacement term and
the report+screen pair each exist once. Semgrep verify: PASS, counts unchanged.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261017T011127Z-346699: Conformance 5,677/11,898,
Unit 32,734/33,889, Characterization 5/36 cases ran). Every golden above ran in leg 1 (`leg-1-Conformance.trx`). The first
gate attempt was RED at the line-endings audit: my own corpus scans had rewritten 61 tracked report outputs with CRLF
(`line_endings.py --fix` restored them); run corpus programs from a scratch directory, not the golden's.

**Branch review, round 1 (two findings, both fixed).** R1-1: the by-hand corpus scans had left 62 run-output files
(`*.TXT`, `*.RPT`, `*.txt`, `*.rpt` a golden's `ASSIGN TO` wrote) tracked in `tests/conformance/` after a `git add -A`;
`CorpusRunFileDriftTests.NoCorpusDirectoryTracksARunFile` (a tier-0c tree sweep, not in leg 1) would have failed CI on them.
They are deleted again. R1-2: `pb3287-address-of-constant-name` was scoped `reject-at: 2023` on the false premise that the
constant entry is 2023-only; it is 2002-era (constructs.json `constant-entry-2002`) and the new constant-name arm of
`ReportUnidentified` is edition-independent, so it now runs at 2002, 2014 and 2023 (at 85 the CONSTANT entry and
SET ADDRESS OF are themselves 2002-era gates, so 85 is not a clean case).

