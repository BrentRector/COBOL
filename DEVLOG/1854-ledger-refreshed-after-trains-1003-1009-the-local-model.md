## Entry 1854 — 2026-10-03 15:52 PDT — Ledger refreshed after trains 1003-1009; the local-model spike is recorded

**Ledger.** `docs/rearchitecture/evidence/ledger-trend.json` gained seven points, one per train from 1003 to 1009 (GAP 669, 598,
564, 544, 532, 523, 504; closed is 4,347 minus GAP, DNS 457 throughout), because the landers had not appended them, and the page
was regenerated with `gen_ledger.py` (`--check` clean) and republished to the same artifact (version 86). The hand-written
in-flight section, `ledger-in-flight.md`, still described 2026-09-26; it now describes the state after train 1009: nothing
running, the partly done items, the owner decisions of the day, and the measurements. Wave 1009's lander counted GAP 523 to 504
(cluster A closed 15 rows, B 2, C 2); the probe agrees (504 GAP, 215 actionable).

**Local-model spike recorded** as `docs/rearchitecture/evidence/fleet-optimization/2026-10-03-local-model-spike.md` with its raw
results and scripts, as the owner's rule for fleet experiments requires. One use shows a measured gain: BM25 fused with an embedding
model finds a note's governing clause in the top 10 for 0.81 to 0.82 of 1,446 notes against 0.713 for BM25 alone, and ranks it higher
(MRR 0.565 against 0.444). The report digest reproduces landed notes at F1 0.98 to 0.99 but keeps only 63 to 73 percent of the leads;
duplicate-lead matching reaches 0.56 at best in the top 5. Whether better retrieval saves implementer turns is unmeasured and is the
next experiment. No adoption; no code changed.

**Wave economics, for the next sizer.** Waves 1003-1007 cost 2.2-3.4 M subagent tokens each, 2-3 weekly points; wave 1008 (a finisher
for an already-gated branch plus one small fix, then a lander) cost 0.55 M and under one point; wave 1009 (three Sonnet clusters) cost
1.94 M. Weekly use rose from 87 percent (wave 1004) to 98 percent by the time wave 1009's lander started, against the owner's 99 percent
ceiling for the night.

**Files:** `docs/rearchitecture/evidence/ledger-trend.json`, `docs/rearchitecture/evidence/ledger-in-flight.md`,
`docs/rearchitecture/evidence/fleet-optimization/2026-10-03-local-model-spike*`. No code changed, no diagnostic code used.

