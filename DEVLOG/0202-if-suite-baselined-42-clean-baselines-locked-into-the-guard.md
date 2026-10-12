## Entry 202 — 2026-05-28: IF suite baselined — 42 clean baselines locked into the guard (137 NIST guarded)

With the IF suite at 0 FAIL*, captured a `tests/nist/valid/<TEST>.txt` baseline for each of the
42 clean IF programs (IF101A–IF142A) and added them to `scripts/guard.sh`'s `NIST_TESTS` list.
The guard now regression-guards **137 NIST programs (95 NC + 42 IF)** — every one MATCH, no
FAIL* in any baseline. The CCVS report header carries only a fixed version string (`Apr 1993`),
no wall-clock, so the baselines are deterministic under the guard's `normalize()` (which already
strips trailing spaces and masks `COMPUTED=` digits).

IF401M/402M/403M are deliberately excluded: they are flagging-conformance modules that emit no
CCVS report, so there is nothing to compare — guarding them would be a false negative. Noted in
the guard comment.

This completes the IF suite for the depth-first NIST push (IF → 100%). Net journey this session:
IF CLEAN 8 → 42 via seven systematic fixes (intrinsic crash-robustness, MAX/MIN category
propagation, untrimmed string args, nested subscripts, signed-decimal lexing, table(ALL)
expansion, additive-expression arguments, CHAR/ORD 1-based ordinal, LENGTH of nested string
functions). Next suite: SM (source text manipulation / COPY).

