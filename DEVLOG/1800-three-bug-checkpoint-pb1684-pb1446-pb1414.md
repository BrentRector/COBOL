## Entry 1800 — 2026-09-29 21:11 PDT — Three-bug checkpoint: PB1684 + PB1446 + PB1414

Batch 8, written while batch 7 was on the WSL gate.

- **PB1684** — EC-DATA-NOT-FINITE is scoped to a STANDARD floating-point usage (§14.6.13.2 3), §3.166/3.167): `SendingRefRules.FloatChecked(r, pic)` asks `UsageFamilies.IsStandardFloat` at all five wrap sites. `ec_data_not_finite` had asserted the raise on COMP-2 / COMP-1 and now uses FLOAT-BINARY-64 / -32; the FLOAT-LONG / COMP-2 case is a new golden.
- **PB1446** (half) — the comma-decimal alternatives of `numericLiteralCore` carry the left-edge predicate `tokensAreContiguous(n)`, so `1 ,5` is no longer fused into the literal 1,5 (= 1.5); it is refused (COBOL0307). The two PARTIAL rows await re-recording.
- **PB1414** (half) — INSPECT keeps a figurative operand as a `BoundFigurative` and reads it through the collating sequence of identifier-1's category; the sibling sweep of other char-constant figurative spellings is owed.

Tried and backed out: PB1689 — making REMARKS a user word contradicts the documented determination D-RW1 (docs/CONFORMANCE.md: REMARKS is an extensionReserved vendor word, reserved at every edition, following GnuCOBOL; CobolWordsDriftTests holds it). Recorded on the note as needing that determination overturned, not a code change.

Read and deliberately not taken: PB1667 (what an overprint looks like in a text file is an implementor determination), PB1586 (a tab-expansion pass touches every raw-line index and literal contents), PB1705 (needs a carrier the binder keeps or drops), PB1374 (needs the printed diagrams rendered), PB1382 (lands with PB1120).

