# adj-a1-w1-s3 — Annex A.1 documentation rows, wave 1 session 3 of 4

- **Pinned sha:** `0a83d2d22836ddf39644d0a32d5303542edbdb94` (probe compiler: Debug build of `CobolSharp.sln` at that sha)
- **Inputs:** `scripts/spec/phase_b_batch.py A.1 --max-rules 10`, the 20 assigned slugs only (DOC-A.1-53/54/55/60/61/63/65/66/67/68/69/72/75/76/77/78/79/80/81/83)
- **Wall-clock (UTC 2026-09-24):** setup + build 21:06–21:09 · adjudication 21:09–21:25 · refute 21:25–21:32 · deliver 21:32–21:34

## Verdicts (final.jsonl, after refutation)
| Verdict | n | Rows |
|---|---|---|
| DIVERGES | 9 | 53, 54, 55, 68, 75, 76, 77, 79, 80 |
| PARTIAL | 7 | 60, 61, 63, 65, 72, 78, 81 |
| NOT-IMPLEMENTED | 2 | 66, 67 |
| NEEDS-OWNER-DECISION | 1 | 83 |
| CONFORMS | 1 | 69 |

## Refuter (one independent subagent, CONFORMS rows only)
- DOC-A.1-69 upheld (CONFORMS; it corrected the "nothing detected at compile time" note: COBOLNET1662 pre-warns a fatal EC-ORDER-NOT-SUPPORTED, but code is never withheld).
- DOC-A.1-72 **overturned → PARTIAL**: the documented "textual sameness of the clause" rule is not what the check compares (DISK "x" vs "x", PRINTER vs DISK, device-name XADAT vs literal "XADAT" all pass); test-ref emptied (pb673 golden has no >>TURN).
- DOC-A.1-81 **overturned → PARTIAL**: FLOAT-SHORT receivers are double-rounded (owned by kb/Work PB1110); test-ref emptied (pb271 is a determination, out-of-range only).

## Files
`adjudicate.jsonl` (pre-refute checkpoint log) · `refute.jsonl` · `final.jsonl` · `findings.json` (12 mechanisms).
New, unowned mechanisms: DOC-A.1-75 host share-mode interaction undocumented; DOC-A.1-72 consistency check vs documented rule; DOC-A.1-63 unstructured dynamic-length item corrupts on WRITE/READ (arm of PB1094); prose/other-row determinations not filed under their own DOC-A.1 key (65, 78, and DOC-A.1-62's reference to a nonexistent item-60 row).
