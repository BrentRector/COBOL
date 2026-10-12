## Entry 241 — File-I/O wall, part 5: START key-relational + RECORD KEY parse forms (IX/RL)

Indexed/relative parse forms beyond the SQ-shared ones. All spec-conformant; fixed:

- **START … KEY [IS] [relational-operator] data-name** (ISO §14.9.41). The grammar used
  `KEY IS comparisonExpression` — a *full* comparison — but the START KEY phrase is an optional
  relational operator + key data-name with an implicit left operand (the key of reference). Changed
  to `KEY IS? comparisonOperator? dataReference`; `FileIoBinder.BindStart` maps the operator via the
  shared `ConditionBinder.ParseComparisonOperator` and assumes EQUAL when the operator is omitted
  (`START f KEY IS data-name`). Care: keeping the operator optional preserves the existing
  `FileIO_Start_PositionsForReadNext` integration test (which omits it) — caught by the guard and fixed.
- **RECORD KEY / ALTERNATE RECORD KEY — IS optional** (ISO §12.4.5): `RECORD KEY IS? dataReference`
  and `ALTERNATE RECORD? KEY IS? dataReference` — accepts `RECORD KEY data-name` without IS.

Result: **IX 1→19 compiling, RL 5→23 compiling** (of 42 / 35). No new baselines yet — the newly-
compiling indexed/relative tests move to FAIL*/NO_OUTPUT (indexed/relative RUNTIME-correctness tail),
not CLEAN. Full guard ALL GREEN (1000 / 348 / 181).

Remaining IX/RL COMPILE_FAILs are smaller distinct clusters: INVALID-without-KEY phrase (`READ …
RECORD INVALID <imp>`, ~8 — KEY apparently optional in CCVS but every ISO figure shows `INVALID KEY`,
so deferred pending a definitive read); a procedure-division `…-KEY` form (READ/START KEY phrase,
~11); START/READ KEY accepting an ALTERNATE key (CBL3002 "not a record key", ~6); OPEN EXTEND on
non-sequential (CBL3002, ~5 — verify against spec). Then the indexed/relative runtime FAIL* tail
(the bulk of remaining gains) and ST sort/merge.

