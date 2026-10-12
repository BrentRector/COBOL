## Entry 560 — 2026-06-10 19:35 PDT — Chain-consumer harness support: chains.tsv + predecessor runs → 22 programs locked with ZERO compiler change (798 conformance)

**The resume-prompt's RESUME-AT ①, implemented from the two scout briefs** (`st-chain-brief.md` §3,
`rlix-diffs-brief.md` C1): the swept "failures" RL102A/103A/109A/110A/202A/203A/207A/208A/213A,
IX102A/103A/202A/203A (incl. the IX102A "timeout"), and the seven ST verifiers were never compiler bugs — they
are NIST producer→consumer chain CONSUMERS whose first op is OPEN INPUT of a TF### file a predecessor creates.
In the harness's isolated temp dir that file is absent, the non-OPTIONAL OPEN INPUT sets I-O status '35'
(ISO §9.1.13.6), every keyed READ then returns a logic status (neither '1x' AT-END nor '2x' INVALID-KEY), and
the CCVS report cascades FAIL — IX102A's GO-TO retrieval loop never exits at all (the swept TIMEOUT decoded).

**The design (singular-pattern):** the chain topology lives in exactly ONE place — the new
**`tests/nist/chains.tsv`** (one line per consumer: `consumer pred1 pred2 …`, X-card-derived per program UNIT by
the scout, including the no-golden intermediates like ST102A/ST110A so their sweep runs are meaningful). Both
harnesses read it: `NistDifferentialTests` gains a lazy `Chains` table + a predecessor loop in `RunNist` —
each producer compiles from its OWN `.cob` (robust against multi-unit emission picking entry units) and runs
inside the consumer's OWN GUID temp dir before the target, same env (`COBOL_SWITCH_1=ON`), `.dat` piped when
present, failures surfaced as `[chain P] compile/run …`. Chains re-run per consumer ⇒ deterministic
start-clean, zero cross-test coupling under xunit parallelism — the legacy guard's fragile "MUST stay
consecutive" prose constraints (guard.sh:18–79) deliberately do NOT carry over, because every chain's first
member re-creates its TF### via OPEN OUTPUT. The any-`*.txt` print-file discovery fallback is disabled in a
chain dir (it would diff a predecessor's report or a `tf###.txt` data file); exact-name discovery only.

**Sweep hardening** (`/e/tmp/nc-sweep/sweep-one.sh`, both fixes the briefs proved necessary): (1) the same
chains.tsv predecessor loop (CHAINERR classification); (2) compile success is now judged by a NON-EMPTY dll
(`-s`, not `-f`) — a failed Roslyn emit leaves a 0-byte dll the old check misread as compiled, producing the
phantom "hostpolicy.dll" RUNERRs (ST133A/ST134A/SQ203A are deterministic backend CS0029s, not flakes); plus
NO_REPORT over diffing an arbitrary `.txt` when chained.

**22 new locked rows** (each verified byte-green behind its chain by the scouts on the frozen build, now green
in the live harness): ST103A/105A/107A/114M/117A/121A/126A (ST golden coverage 21/29), RL102A/103A/109A/110A/
202A/203A/207A/208A/213A (RL207A/208A green even while RL206A's own golden still DIFFs — the FD RECORD VARYING
gap, next), IX102A/103A/202A/203A, OBSQ4A/OBSQ5A. **798 conformance + 15 unit green; 248 NIST programs locked.**
No compiler/runtime source touched in this change.

