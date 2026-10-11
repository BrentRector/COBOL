## Entry 1213 — 2026-08-07 21:52 PDT — Session close: seven ranked defects resolved, three funnels extracted, and the two next items carry their complete plans

**Context refresh for the next session — docs only.** Plan §0's handoff now leads with the evening close:
**R04 · R05 · R06 · R07 · R08 · R09 (closed stale) · R10 (float half)** — plus, discovered and fixed en route,
the §8.3.2.1 directive-word hole (`>>TURN`/`>>DEFINE` evaded the word-length ceiling) and the USE-site level-2
edition-gate hole (`USE AFTER EC EC-MCS` at 2002). Every landed item probe-verified, wave-gated, pushed behind
a watched CI verdict; runs 31234673404 / 31236040786 / 31237206814 / 31238417792 all green on all four jobs.

**The premise-failure pattern stands at 14 of 16 across two sessions.** Today's contributions: R04's token
axis, R05's "truncation" that was the rule itself, R09 closed by a PROBE against a stale note. R06 and R08 are
the only premises that held as filed. Three extractions each carry a drift guard: `Table12StatementNames`,
`EcNameResolution` (four verbatim diagnostic copies collapsed; the guard failed once on its own subject before
its first commit), `CobolWordRule`. R07 added the miniature two-arm lesson: an ENGINE + STATIC FACADE pair
fails at generated-code compile time when only one arm learns a new signature.

**NEXT (both fully planned in their notes): R10's COMP-5 unsigned-carrier change set (owner decision
2026-08-07 — `ulong`/`UInt128`; WrapBinary's bits=128 shift-mask bug in the same change set; the
container-list drift test) and R00's 82-finding ledger audit (the residue block's "16" is itself a hypothesis;
disposition per finding, orphans become R11+ notes first).**

