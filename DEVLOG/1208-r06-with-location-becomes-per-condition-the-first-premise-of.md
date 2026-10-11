## Entry 1208 — 2026-08-07 20:13 PDT — R06: WITH LOCATION becomes per-CONDITION — the first premise of fifteen that survived contact with the spec

**R06 is LANDED, and for once the entry's stated defect WAS the defect** — the citation validated before any
code moved: §15.32.3 r1 keys the 63-spaces answer on "the LOCATION option of the TURN directive … that enabled
checking for THE EXCEPTION CONDITION associated with the last exception status" — per-condition. `EcWrap`
collapsed it to `enabled.Any(…)` — per-statement — so a DIVIDE checked for EC-SIZE (WITH LOCATION) and
EC-BOUND-SUBSCRIPT (without) recorded location information for the BOUND-SUBSCRIPT raise too:
EXCEPTION-STATEMENT answered "DIVIDE" where r1 requires 63 spaces.

**The bit now travels on each (name, file) pair** — `EcStatementInfo.Enabled` is (Ec, File, WithLocation)
triples and the statement-level bool is DELETED, which made the COMPILER enumerate every consumer: a leftover
use cannot build. Two renderings serve the eight consumers: `EcStmtLoc(info, ec[, file])` for a
statically-known name; `EcStmtLocExpr(info, names, nameExpr)` for a runtime-selected one — CONSTANTS when the
subset is uniform (byte-identical to the pre-R06 output, the overwhelmingly common case), a per-name
conditional only when genuinely mixed. The EC-I-O bridge needed the structural variant: the raised name is
chosen INSIDE `__IoCheckEc` by the §9.1.13.1 status mapping, so the helper gained a `__locMask` sharing
`__mask`'s bit positions (`IoLocMaskFor` beside `IoMaskFor`). `BoundRaise` was already per-condition — RAISE
names one ec — and is untouched.

**The fact is the mixed statement itself** (ECT053): one DIVIDE, both conditions, two raises. The subscript
raise answers `S=[63 spaces] L=[ ]` — under the old bool it read `S=[DIVIDE…]`, so the fact discriminates —
and the zero-divide raise keeps its Table-12 name and three-part location. Its first run corrected my own
expected string: the paragraph sits in MAIN SECTION, so §15.30.3 renders "MAIN-PARA OF MAIN", not bare
"MAIN-PARA".

**GATE (wave-local):** solution build clean · EC + PerformFormat3 + IO conformance battery green · Unit
4048/4048. R05's CI run reported success before this push.

