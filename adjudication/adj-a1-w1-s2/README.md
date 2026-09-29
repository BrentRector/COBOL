# adj-a1-w1-s2 — Annex A.1 documentation rows, wave 1 session 2 of 4

- **Pinned sha:** `0a83d2d22836ddf39644d0a32d5303542edbdb94` (main at session start)
- **Inputs:** `python scripts/spec/phase_b_batch.py A.1 --max-rules 10 --out /tmp/adj/in`, the 20 assigned slugs only.
- **Wall-clock (UTC, 2026-09-24):** setup + build 21:06–21:08 · adjudication 21:08–21:20 · refute 21:20–21:23 · deliver 21:24.

## Verdict counts (final.jsonl)
DIVERGES 12 · NOT-IMPLEMENTED 3 · NEEDS-OWNER-DECISION 3 · PARTIAL 1 · CONFORMS 1

None of the 20 items has a `| DOC-A.1-N |` row in docs/CONFORMANCE.md §7.

| Row | Verdict | One line |
|---|---|---|
| DOC-A.1-30 | NOT-IMPLEMENTED | >>IMP not defined (anonymous COBOL0001). Conditional item; nothing records that it is not provided |
| DOC-A.1-31 | DIVERGES | Alphanumeric/national sets and encoding not documented; UTF-16 in memory, Latin-1 at the byte/file boundary (PB690) |
| DOC-A.1-32 | DIVERGES | Coded values of SPACE/QUOTE/ZERO/editing characters are fixed in code but not documented (PB1397) |
| DOC-A.1-34 | NOT-IMPLEMENTED | Compile-time and runtime sets are the same UTF-16, so no conversion happens. Posture not recorded (PB690 names the row) |
| DOC-A.1-35 | CONFORMS | Invariant case mapping when no locale is in effect; need not be documented; refuter UPHELD |
| DOC-A.1-36 | DIVERGES | Composite UTF-16 set; which characters are alphanumeric is not documented and the code splits on it (new) |
| DOC-A.1-37 | NOT-IMPLEMENTED | Only one runtime encoding, no selector; conditional item with no posture recorded |
| DOC-A.1-38 | DIVERGES | Any UTF-16 character is always allowed in class alphanumeric, not as a user option; rules not documented (new) |
| DOC-A.1-39 | DIVERGES | Row missing (PB160), and a **wrong-answer**: a CONTINUE AFTER interval of 2^63 or more wraps before the 86,400 clamp (new) |
| DOC-A.1-40 | DIVERGES | COPY library-locating rules not documented (PB1355, PB1369) |
| DOC-A.1-41 | NEEDS-OWNER-DECISION | CRT status 9xxx: optional, screen module A.4.2 Not claimed, no 'Not provided.' row, no selector |
| DOC-A.1-42 | PARTIAL | literal-9 content documented under §2 row 25 and verified by probe, but not filed under the DOC-A.1-42 key |
| DOC-A.1-43 | DIVERGES | Currency-symbol equivalence (ToUpperInvariant) not documented (PB1092) |
| DOC-A.1-44 | DIVERGES | Extra prohibited currency characters (the empty set) not documented (PB1397) |
| DOC-A.1-45 | NEEDS-OWNER-DECISION | Cursor keys: optional, A.4.2 Not claimed, no 'Not provided.' row |
| DOC-A.1-46 | DIVERGES | No complete specification of representations; items 47/74/81/213/217 have no row (new) |
| DOC-A.1-47 | NEEDS-OWNER-DECISION | FLOAT-DECIMAL default encoding; the facility is declined under A.3 items 13/19 (PB579 owner question) |
| DOC-A.1-49 | DIVERGES | >>DEFINE … AS PARAMETER reads an environment variable named by the spelling as written (case-sensitive); not documented (new) |
| DOC-A.1-51 | DIVERGES | DELETE FILE effect (a single host unlink) not documented (new) |
| DOC-A.1-52 | DIVERGES | Devices that allow concurrent access: no determination made (PB322) |

## Refuter
One independent refuter checked the only CONFORMS row, DOC-A.1-35, and **UPHELD** it. It widened the test-ref to add the LOWER-CASE goldens `2023/pb8_refmod_function_result` and `2023/pb10_function_identifier_sending`. Overturns: 0.

Files: adjudicate.jsonl (per-row checkpoint), refute.jsonl, final.jsonl, findings.json (6 mechanisms).
