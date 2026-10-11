## Entry 253 — Second relative chain (DYNAMIC, COMP keys) baselined → RL202A/RL203A

The PIC-aware COMP-key fix (252) was the only blocker on the RL2xx chain too. Re-surveying RL in
list order (which is chain order) showed RL201A→RL202A→RL203A all CLEAN with no further code change:
RL201A creates TF021 (`XXXXP021`, DYNAMIC access, `PIC 9 COMP` relative key), RL202A randomly
REWRITEs/DELETEs, RL203A verifies. Before 252 these consumers failed for the same reason RL102A/103A
did — the COMP relative key decoded as ASCII.

Validation: ran the exact guard RL sub-sequence with RL202A/RL203A inserted after RL201A — in list
order, in one directory, **without cleaning data files between tests** (replicating the guard) — and
confirmed every member (RL101A RL102A RL103A RL107A RL201A RL202A RL203A RL209A RL302M) at 0 FAIL*,
and the RL201A→RL202A→RL203A outputs deterministic across repeated runs. RL209A and RL302M are
unaffected: RL209A is itself an `XXXXP021` producer (opens OUTPUT, recreating TF021) so it does not
depend on the RL2xx chain's residual file state, and RL302M re-creates its inputs.

Baselined RL202A + RL203A with the second chain ordered consecutively after RL201A in
`scripts/guard.sh` (comment documents the second ordering invariant). **NIST baselines 199 → 201**
(94 NC + 42 IF + 12 SM + 4 IC + 39 SQ + **9 RL** + 1 IX). Full guard ALL GREEN: 1000 unit / 348
integration / 201 NIST, 0 regressions. No production code changed this entry — pure coverage gain
from 252. Remaining relative tail unchanged: no-KEY leniency (RL117A/IX, deferred non-conformant),
RL110A sequential creation, RL208A/RL204A (other 2xx consumers), RL210A/RL211A multiple-record-format/
ODO, COMP-keyed START.

