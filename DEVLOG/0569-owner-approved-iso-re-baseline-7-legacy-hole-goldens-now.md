## Entry 569 — 2026-06-10 22:00 PDT — OWNER-APPROVED ISO re-baseline: 7 legacy-hole goldens now hold the conforming output (825 conformance; 272 NIST locked; legacy guard ALL GREEN with the documented nonconformance list)

**The owner approved the sign-off queue ("re-baseline" — confirming the legacy "did not 100% follow the spec"),
so the seven exceeds-golden programs now have ISO-CONFORMING goldens** — generated from greenfield runs, each
verified TWICE before overwriting: (a) zero `FAIL*` rows + clean footers in every produced report, and (b) the
diff against the old golden is EXACTLY the documented divergence, nothing else:
- **IX111A** (+7): the failed indexed OPEN ('35') fires the file-scoped USE declarative (§14.9.49.4 GR3a) —
  OPN-TEST-GF-01-0 PASS, 001 OF 001; the legacy never fired it.
- **IX210A / IX214A** (157 each): no FAIL-ROUTINE info lines after PASS rows (unreachable per §14.9.17's IF
  reading), and the 18 self-deleted START tests EXECUTE with the spec statuses '00'/'23' (§14.9.41 GR9 /
  §9.1.13.5) — 039 OF 039.
- **IX215A** (102): the same PRINT-DETAIL reading — 033 OF 033 (the qualified-key fix 564 made it run at all).
- **NC235A / NC236A** (8 each): the two SEARCH facts the SpecPinned tests carry (§14.9.37 F2 condition-name
  WHEN over ODO / §14.9.37.4 GR8b VARYING another index) — 013 OF 013 and 010 OF 010, nothing deleted.
- **SQ207M** (4): the AFTER-ADVANCING-mnemonic WRITE is RELEASED (§14.9.46 GR1) at the 0-line advance.

**The legacy guard keeps its full regression net with a documented exception:** `LEGACY_NONCONFORMANT` —
defined ONCE in `scripts/guard.sh` with each hole's ISO citation, extracted by `guard-fast.sh` (the NIST_TESTS
pattern) and honored by `guard-run-group.sh` — still compiles and RUNS the seven, reports
`LEGACY NONCONFORMANT (golden = ISO-conforming baseline; expected diff)`, and never counts them as
regressions. **Full guard re-run: 357 MATCH + 7 LEGACY NONCONFORMANT, 0 regressions; 1204 unit + 536
integration; ALL GREEN.** The SpecPinnedNistTests pins remain as the citation-bearing documentation (doc
comment updated — they no longer wait for G8).

**All seven locked as differential rows → 825 conformance + 15 unit green.** Count correction
(`feedback_transparency`): the running "NIST locked" tallies in entries 563–568 were UNDERCOUNTS from manual
arithmetic; the authoritative count is the `[InlineData]` row census — **272 unique programs locked**
(92 NC + 24 ST + 32 RL + 40 IX + 18 IC + 64 SQ + 2 OBSQ), zero duplicates, all passing.

