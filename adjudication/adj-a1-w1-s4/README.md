# adj-a1-w1-s4: Annex A.1 wave 1, session 4 of 4

- **Pinned sha:** `0a83d2d22836ddf39644d0a32d5303542edbdb94` (detached from main; Debug build, 0 errors)
- **Inputs:** `phase_b_batch.py A.1 --max-rules 10`, restricted to the 20 slugs named in the brief (DOC-A.1-88, 89, 91, 94–108, 111, 113)
- **Wall-clock:** setup + build ≈ 2 min · adjudication (20 rows, including probes) ≈ 14 min · refute + finalize ≈ 5.5 min · total ≈ 22 min
- **Files:** `adjudicate.jsonl` (checkpointed row by row) · `refute.jsonl` · `final.jsonl` (overturns applied) · `findings.json` (grouped by mechanism) · `probes/` (the probe programs)

## Final verdicts (20 rows)
DIVERGES 10 · PARTIAL 6 · NOT-IMPLEMENTED 2 · CONFORMS 1 · NEEDS-OWNER-DECISION 1

| row | verdict | one-liner |
|---|---|---|
| DOC-A.1-88 | PARTIAL | Condition can't arise (no non-COBOL functions; prototype → EC-FUNCTION-NOT-FOUND), but no row records it as vacuous |
| DOC-A.1-89 | DIVERGES | EC-PROGRAM-RESOURCES has no raise site and no row. Owned by PB1422 |
| DOC-A.1-91 | NEEDS-OWNER-DECISION | §9.2.2 is in A.4.2 (Not claimed), but no selector withdraws the A.1 row |
| DOC-A.1-94 | PARTIAL | Behaviour matches the §3 STOP/GOBACK bullet, but that bullet is filed under items 192/193, and there is no row for item 94 |
| DOC-A.1-95 | DIVERGES | X"xx" → U+00xx stored verbatim; not documented. Owned by PB1397 |
| DOC-A.1-96 | PARTIAL | Characters are 16-bit, so the condition doesn't arise; no row records it as vacuous |
| DOC-A.1-97 | DIVERGES | NX"D800" is stored verbatim; not documented. Owned by PB160 |
| DOC-A.1-98 | PARTIAL | Same as 96, for national literals |
| DOC-A.1-99 | DIVERGES | Any EC-IMP-suffix is accepted and treated as fatal; not documented, and possibly under-rejects |
| DOC-A.1-100 | DIVERGES | EC-PROGRAM-IMP is raised at one internal site; the Imp→fatal rule is stated only in code comments |
| DOC-A.1-101 | DIVERGES | A non-COBOL method is rejected at compile time (COBOLNET0813/0823); not documented |
| DOC-A.1-102 | DIVERGES | No resource check before INVOKE; not documented. PB1422 names this as a sibling. The A.1 item cites GR7e, but the rule is GR7b |
| DOC-A.1-103 | DIVERGES | With checking off a fatal status continues; with checking on it terminates. Not documented, and the '71' doc comment in FileStatus.cs says the opposite |
| DOC-A.1-104 | CONFORMS | One status is chosen in a deterministic order ('07' over '05'); pinned by conformance:2023/pb317_open_no_rewind |
| DOC-A.1-105 | NOT-IMPLEMENTED | No technique for correcting a permanent error, and no §7 "Not provided." row |
| DOC-A.1-106 | NOT-IMPLEMENTED | No 0x status value, and no §7 "Not provided." row |
| DOC-A.1-107 | DIVERGES | No boundary model and no row. Owned by PB1192 |
| DOC-A.1-108 | DIVERGES | Same as 107. Owned by PB1192 |
| DOC-A.1-111 | PARTIAL | The determination exists but is filed under item 112. Owned by PB1369 |
| DOC-A.1-113 | PARTIAL (overturned from CONFORMS) | The GC finalizer's close goes to an orphan RunUnit, so a dropped object's file stays open (OPEN returns 61) |

## Refuter
- **DOC-A.1-104:** stands as CONFORMS. Side note: when both '48' and '44' apply to a WRITE, the choice depends on organization — indexed places '44', relative/sequential place '48'.
- **DOC-A.1-113:** overturned from CONFORMS to PARTIAL. The adjudicator re-ran `probes/gc2.cob` at the pin and confirmed it (`OPEN-OUTPUT=61`).

## Editions
Editions come from the clause text and Annex E (2023). Where Annex E is silent, 2014 is assumed to be unchanged. Where a row lists editions before 2014, they come from the traceability inventory's convention for neighbouring rows, because this spec text cannot establish them.
