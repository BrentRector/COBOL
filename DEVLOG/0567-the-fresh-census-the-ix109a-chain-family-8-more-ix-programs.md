## Entry 567 — 2026-06-10 21:08 PDT — The fresh census + the IX109A chain family → 8 more IX programs locked (817 conformance, 244 NIST)

**A fresh chain-aware full-corpus census** (`/e/tmp/nc-sweep/census-current.txt`, 361 programs): **268 GREEN**,
33 NO_GOLDEN, and the residue now classifies into clean families. Two census-side findings first: the NC174A/
NC254A "DIFFs" were the census run missing `COBOL_SWITCH_1=ON` — the sweep script now EXPORTS it (guard.sh:120
parity, no longer caller-dependent); and the six ST `NO_REPORT`s are the no-golden chain intermediates running
by design.

**The IX110A/114A–120A cluster (8 programs, DIFF 16–28) was ONE root cause: the wrong producer.** They are I-O
status-test consumers of TF024 — but TF024 has TWO writers with DIFFERENT key universes: IX101A writes
`ABCDLKJXYZ…` keys (the IX102A/103A chain), while the multi-unit IX109A creates the `RECORD-KEY…END-OF-KEY`
records these eight tests probe (duplicate-prime WRITE '22', REWRITE-of-absent '23', OPEN I-O '00' on a
present file). Behind IX109A all eight are byte-green — `chains.tsv` rows added with the two-producers note
(the per-consumer isolated chain model absorbs the legacy guard's "must stay consecutive" hazard here: each
consumer names ITS producer). Locked: IX110A, IX114A–IX120A.

**Remaining residue by family (the forward worklist):** NC105A (brief at `nc105a-brief.md`); the SQ-M cluster
(SQ101M DIFF 11; SQ201M/207M/208M/209M/210M loud NotImplemented — truncated messages, untriaged); SM = COPY
support (6 CMPL_FAIL parse the COPY library format + 6 DIFF); IC cross-FILE dynamic CALL (IC109A/110A/117M/
205A/210A — `EC-PROGRAM-NOT-FOUND`, the pre-G8 cross-assembly CALL work item) + IC113A timeout + IC222A (CALL
ON EXCEPTION gated 2002+ yet used by a CCVS-85 program — needs the version-targeted investigation) + IC233A/
234A (cross-program GLOBAL USE, designed §5.6) + IC207A/227A (EXTERNAL FD) + IC401M (parse, `unexpected 'IS'`);
the four Tier-C COMP-record SORTs; and the OWNER SIGN-OFF queue (IX210A/214A/215A C9 + IX111A + NC235A/236A).
**817 conformance + 15 unit green; 244 NIST programs locked.**

