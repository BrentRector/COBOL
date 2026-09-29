# adj-a1-w1-s1 — Annex A.1 documentation rows, wave 1 session 1 of 4

- **Pinned sha:** `0a83d2d22836ddf39644d0a32d5303542edbdb94` (Debug build of CobolSharp.sln, 0 errors)
- **Rows:** 20 DOC-A.1 rows: 3, 4, 6, 7, 8, 9, 11, 13, 14, 15, 16, 17, 21, 23, 24, 25, 26, 27, 28, 29
- **Wall-clock (UTC 2026-09-24):**

  | Phase | Start | End | Duration |
  |---|---|---|---|
  | Setup and build | 21:06 | 21:08 | ~2 min |
  | Adjudication | 21:08 | 21:19 | ~11 min |
  | Refute | 21:19 | 21:23 | ~4 min |
  | Deliver | 21:23 | 21:25 | ~2 min |
  | **Total** | | | **~19 min** |

- **Final verdicts:**

  | Verdict | Count | Rows |
  |---|---|---|
  | DIVERGES | 10 | 6, 8, 9, 14, 17, 21, 23, 25, 26, 29 |
  | NEEDS-OWNER-DECISION | 5 | 3, 4, 11, 27, 28 |
  | PARTIAL | 4 | 7, 13, 15, 16 |
  | CONFORMS | 1 | 24 |

- **Refuter:** one independent refuter checked the single CONFORMS row (DOC-A.1-24). It was upheld, with 0 overturns. The refuter verified the '30'-on-close-failure, store-persistence and lock-release edges by probe, and noted that no test-ref pins the first two.
- **One post-refute correction:** DOC-A.1-29's editions changed from "2014, 2023" to "2002, 2014, 2023".

## Files

| File | Contents |
|---|---|
| `adjudicate.jsonl` | Per-row checkpoints, as decided |
| `refute.jsonl` | The refuter's record |
| `final.jsonl` | Verdicts with the refute and the correction applied |
| `findings.json` | Defects clustered by mechanism, with owning notes |

Probe programs are in /tmp/adj/probe. They are not committed.
