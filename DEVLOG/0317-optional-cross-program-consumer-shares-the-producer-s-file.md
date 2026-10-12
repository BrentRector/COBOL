## Entry 317 — OPTIONAL cross-program consumer shares the producer's file → RL213A baselined (NIST 354→355)

First of four fixes integrated from a **parallel worktree-isolated implementation workflow** (4 agents each
implemented + built + full-guarded one backlog fix concurrently; I integrate each onto main sequentially,
guard-gated). **RL213A** (20 FAIL\*) is a producer/consumer chain: RL212A writes 500 records to RELATIVE file
RL-FS1 via the non-OPTIONAL `ASSIGN … XXXXP021` → shared `TF021`; RL213A then `SELECT OPTIONAL RL-FS1 ASSIGN …
XXXXX021`, OPENs EXTEND, appends 501–520, re-reads all 520. They diverged: `NistPreprocessor`'s SELECT-rewrite
maps a RELATIVE/INDEXED `XXXXX###` → `"TF###"` only when the SELECT is NOT OPTIONAL — correct isolation for the
self-producing absent-optional family (IX216A/217A/218A, SQ202A/203A) — so RL213A's OPTIONAL `XXXXX021` stayed
program-qualified and its EXTEND appended to a fresh empty file (every record-number check FAILed). OPTIONAL
governs presence semantics, not the physical assignment target (ISO §13.18.43). Fix: a narrow per-test
allow-list `OptionalSharedAssign = {("RL213A","021")}` in the SELECT-rewrite — for an OPTIONAL RELATIVE/INDEXED
SELECT, still map `XXXXX### → "TF###"` for exactly the (test, X-card) pairs that are cross-program consumers of
a separate producer's file, never touching the isolation family. (The per-test-name key is a documented smell;
NistPreprocessor can't see the producer from one file. Acceptable as a tiny, surgical, audited allow-list.)
RL213A → 521 OF 521, 0 FAIL\*, baselined; RL212A added as a NO_OUTPUT producer immediately before it. Guard ALL
GREEN: **1040 unit / 347 integration / 355 NIST** (RL 31→32), 0 regressions.

