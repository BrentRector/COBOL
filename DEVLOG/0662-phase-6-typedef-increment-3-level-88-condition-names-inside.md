## Entry 662 — 2026-07-07 13:50 PDT — Phase 6: TYPEDEF increment 3 — level-88 condition-names inside a TYPEDEF

Increment 3 of TYPEDEF / the TYPE clause (ISO/IEC 1989:2023 §13.18.58.4 GR1, COBOL-2002; data-model **D17**): a
TYPEDEF's level-88 condition-names are now PART of the type — each `TYPE` reference gets its OWN clone of them, and
the TEMPLATE's names are NOT globally referenceable (GR1). Binder-only.

**The bug this fixes.** Before inc 3, a level-88 in a TYPEDEF template was registered in the GLOBAL by-name index
(`DataBinder.Conditions`) with its Parent pointing at the storage-less template item — so referencing it resolved to
the template (silently wrong), and it was NOT carried onto the referencing records (a `TYPE`-referenced record's 88s
did not exist). Both wrong.

**Mechanism.** `DataItem` gains `Own88s` (the item's own level-88s — the source of truth). `BindCondition` now always
attaches the `Condition88` to `parent.Own88s`, but registers it in the GLOBAL index only when `registerGlobal` — and
`BindEntries` passes `registerGlobal: !rootIsTemplate`, so a template's 88s stay OFF the global index (GR1) while a
normal 88 registers as before. `ExpandType` (group + elementary branches) and `CloneItem` (each cloned node) then call
the new `CloneConditionOnto` — a fresh `Condition88` with Parent = the clone, the VALUE set copied, added to the
clone's `Own88s` AND (unlike the template) to the global index (clones ARE referenceable). Runs in the ExpandTypes
pass (top of BindResolve), so the cloned condition-names are in the global index before any PROCEDURE binding resolves
an `IF`/`SET`.

**Verified by RUNNING.** Golden `typedef_88`: two records `DOOR`/`GATE` of one type `STATE-T` (an `88 OPEN-ST`/
`CLOSED-ST` on an inner `ST-CODE`); `SET OPEN-ST OF DOOR TO TRUE`, `MOVE "C" TO ST-CODE OF GATE`, three qualified
`IF`s → `DOOR OPEN` / `GATE CLOSED` / `DOOR NOT CLOSED` — proving per-clone INDEPENDENCE + qualified duplicate-name
resolution (§8.4.2.2 Format 2). GR1 confirmed two ways: a single-clone UNQUALIFIED `SET OPEN-ST TO TRUE` / `IF
OPEN-ST` resolves cleanly (would be an ambiguous two-entry match if the template still leaked), and a template-only
88 with NO `TYPE` reference is unresolvable (the pre-existing loud path — a runtime `NotImplemented: condition`, never
a silent template resolve).

**Tests + golden.** `typedef_88` (2002 corpus, GreenfieldOnly). `TypedefConditionTests` ×2:
ClonedConditionResolvesUnqualified (the GR1-leak regression) + TwoRecordsQualifiedConditions.

**Battery: 2015 conformance (+3) · 213 unit GREEN.** No grammar change ⇒ no legacy guard; `typedef_88` joins the
GreenfieldOnly exclusion. 15xx band unchanged (1529/1530/1532/1533 used; 1531/1534/1535 reserved). NEXT: TYPEDEF inc 4
(staged-loud residue — EXTERNAL type decl 1534, RENAMES-in-TYPEDEF 1535, strong bool/object non-equality 1535,
INDEXED-BY-type used ≥2× 1531 — + the matrix behavior/continuity rows + DOC_INDEX / ISO2023_CONFORMANCE_PLAN sync),
then the adversarial find→verify review over incs 1–4.

