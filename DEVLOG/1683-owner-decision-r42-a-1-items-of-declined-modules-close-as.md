## Entry 1683 — 2026-09-24 15:37 PDT — Owner decision R42: A.1 items of declined modules close as DOCUMENTED-NON-SUPPORT

**What.** Cloud adjudication wave 1 (PB1522) returned 10 NEEDS-OWNER-DECISION rows; 9 are A.1 items that exist only
inside modules `docs/CONFORMANCE.md` §5 declines — screen handling A.4.2 (items 3, 4, 11, 27, 41, 45, 83, 91) and
commit/rollback A.4.3 (item 28). No agent may choose DOCUMENTED-NON-SUPPORT (D13) and no derived selector reaches an
A.1 row, so every such item stalled. Asked as one bare question; the owner answered **"yes, close them as
documented non-support"** — recorded as `kb/Work/R42.md`: the same withdrawal ground (a) as DOC-A.1-84/85/86/173
(A.1 preamble: "the item is not required if the optional … feature is not implemented"), notes citing R42 + the §5
row, witnessed by the tests that diagnose the declined construct. DOC-A.1-47 (FLOAT-DECIMAL) is excluded — PB579.
PB1522's registrar step now carries the rule, and the running wave-2 orchestrator was told in-session.

**Also reported by the wave-2 orchestrator (to be filed by its registrar):** a structural blocker — the inventory
schema requires every DOC-row verdict except DOCUMENTED-NON-SUPPORT to carry a `docs/CONFORMANCE.md#DOC-A.1-N`
anchor that the drift gate resolves, so the ~112 A.1 rows judged DIVERGES *because* §7 has no row cannot be recorded
until those determination rows are written; and further NEEDS-OWNER-DECISION rows (31 across two batches), clause
rules rather than A.1 items.

