## Entry 385 — M2-DATA-4 (Boolean/bit) investigated + first-slice recipe recorded in the plan

Ran the `m2-data4-boolean-investigation` workflow (spec + PIC/category recon + literal/operator recon) and recorded
a precise, corpus-checked first-slice recipe in plan §3.2 (it mirrors the National build almost exactly). Key
decisions captured there: **storage = 1 byte per boolean position holding ASCII `'0'`/`'1'`** (spec-permitted by
§13.18.40.4 R14 — a boolean char "may be represented … as an alphanumeric character"; true bit-packing §8.5.1.6.3
deferred), so boolean ≈ "alphanumeric with `'0'` fill + a distinct category + no DISPLAY trim". Corpus-clean: NIST
has no `PIC 1`, no `USAGE BIT`, and no real `B"…"` (only `"B"` strings, which stay STRINGLIT). **Deferring the bit
operators (B-AND/B-OR/B-XOR/B-NOT)** for the first slice — they carry a reserved-word collision risk (`B-NOT` is
used as an identifier in a unit test) and need the XOR-operator wiring pattern (DEVLOG 375). Implementation will
apply the National-review lesson up front: wire the WHOLE dispatch surface (MOVE both ways + literal, VALUE,
INITIALIZE, figurative, compare, DISPLAY) in one pass, then adversarially review before commit. No code changed.

