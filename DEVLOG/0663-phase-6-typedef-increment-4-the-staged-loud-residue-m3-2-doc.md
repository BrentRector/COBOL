## Entry 663 — 2026-07-07 14:02 PDT — Phase 6: TYPEDEF increment 4 — the staged-loud residue (+ M3-2 doc sync)

Increment 4 (final) of TYPEDEF / the TYPE clause (data-model **D17**, COBOL-2002): the four TYPEDEF sub-features that
are recognized but not yet fully modeled now fail LOUD at bind time rather than silently mis-compile (COBOLNET_DESIGN
§1.4). Binder-only. Each detected + verified by RUNNING the CLI before encoding a test.

- **COBOLNET1534 — an EXTERNAL type declaration** (`01 X-T TYPEDEF EXTERNAL.`). §13.18.57.4 GR5 / §13.18.22 — a
  run-unit-shared type (a strong external record's type must also be external); cross-program external TYPE sharing is
  not modeled, so a per-program clone would silently diverge. Guarded in `BindEntry` (`isTypedef && hasExternal`).
- **COBOLNET1535 — a RENAMES inside a TYPEDEF** (a level-66 in a template). §13.18.58.4 GR1 — the RENAMES is part of
  the type but `CloneItem` does not carry `Renames66` into a reference, so it would be silently dropped. Guarded in
  `BindEntries` (`rootIsTemplate` at the level-66 site).
- **COBOLNET1535 — a strong group with a boolean/object/pointer element compared with an ORDERING operator.**
  §8.8.4.2.3 SR4 — such a group may be compared only for equality. Guarded in the ONE `CheckedRelational` chokepoint
  (after the inc-2 same-type check), via a `ContainsNonOrderableLeaf` walk; equality (`=`/`<>`) stays clean.
- **COBOLNET1531 — a type whose OCCURS has an INDEXED BY phrase, referenced ≥2×.** §13.18.38 — two clones share one
  global index-name → one C# index field → silent cross-drive. Guarded by a `_typedIndexNames` collision set threaded
  through `CloneItem` (cleared per program at the top of `ExpandTypes`). A SINGLE reference works (the clone drives the
  one table through the one index) — golden `typedef_indexed` (`R1=P R2=Q R3=R`).

**Verified by RUNNING.** All four guards fire exactly (1534/1535-renames/1535-boolean/1531); the two positive
companions run/compile clean — a single INDEXED-type reference (SET IX + subscripted store/read) and a strong
boolean-group EQUALITY compare (SR4 bans only ordering).

**Tests + golden.** `typedef_indexed` (2002 corpus, GreenfieldOnly — single INDEXED-type reference, byte-verified).
`TypedefResidueTests` ×5: the four guards + a `SingleIndexedTypeAndBooleanEquality_CompileClean` companion.

**Matrix / doc sync (the design's inc-4 bookkeeping).** No new VERSION-MATRIX row: the STRONG phrase rides the SAME
`typedefClause` grammar gate as the existing `typedef-def-2002` row (introduction gating already covered), and
1531–1535 are edition-INVARIANT compile-time diagnostics (no cross-edition behavior variance to encode) — a STRONG row
would only re-test the identical 2002 gate. `docs/ISO2023_CONFORMANCE_PLAN.md` M3-2 synced: **TYPEDEF ◑ DONE** (incs
1–4), SAME AS / TYPE TO deferred; also ticked M3-1 (OCCURS DYNAMIC, D9) which was already complete. The D17 deep-dive's
increment list marks incs 3+4 landed.

**Battery: 2021 conformance (+6) · 213 unit GREEN.** No grammar change ⇒ no legacy guard; `typedef_indexed` joins the
GreenfieldOnly exclusion. 15xx band now FULLY used for TYPEDEF: 1529 (malformed TYPEDEF) · 1530 (TYPE unresolved/
recursive) · 1531 (INDEXED ≥2×) · 1532 (STRONG decl SR3/4/6) · 1533 (STRONG use SR1/SR2) · 1534 (EXTERNAL type) · 1535
(RENAMES-in-TYPEDEF / strong ordering compare). **TYPEDEF / the TYPE clause is now feature-COMPLETE (incs 1–4).**
NEXT: the adversarial find→verify review over TYPEDEF incs 1–4 (every prior feature's review found real defects — run
it), then the next roadmap item.

