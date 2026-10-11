## Entry 707 — 2026-07-08 19:18 PDT — P3 Step 10 (part 2) — SYNCHRONIZED-on-group gated to 2023 (error strict / accept-inert permissive)

**PHASE 03 step 10 (owner chose FULL WIRING), second loud-hole fix.** SYNCHRONIZED on a GROUP item is a COBOL-2023
introduction (Annex E.3.2 item 6 / VCR row 43; before 2023 SYNC is permitted only on elementary items). COBOL.NET
was SILENTLY ACCEPTING it at every edition (SYNC is a no-op in the typed-native model — no byte alignment). Gated it
per the owner's chosen disposition ("reject strict / accept-inert permissive"):
- `DataItem.Synchronized` flag (a new `init` bool, NOT emitted); captured in `DataBinder`'s clause loop from the
  `syncClause` (the grammar rule is `syncClause`, not `synchronizedClause` — a comment-vs-rule-name gotcha).
- Gate in `DataBinder.ResolveIndexItems.Walk` at the group point (`item.Children.Count > 0`): if `item.Synchronized`
  and edition < 2023, `Edition.Removed(EditionCodes.Introduction, …)` — the **removed-severity seam (error strict /
  warning permissive)**, chosen deliberately over the error-both introduction gate so that below 2023 it is REJECTED
  strict but ACCEPTED-INERT permissive. That preserves INV-1 continuity (Step 8's standing sweep): a program carrying
  SYNC-on-group still compiles permissive at every edition. Verified: @2014 strict → error 0900, @2014 permissive →
  warning 0900, @2023 → clean; SYNC-on-elementary is untouched.
- `constructs.json` row `sync-on-group-2023` (introducedIn 2023, code 0900) + a negative corpus fixture
  `sync_group_below_2023` (`*> reject-at: 85 2002 2014`). Registry regenerated (99 rows / 58 GateId). No NIST/corpus
  program uses SYNC-on-group (the battery + guard stayed green, confirming no continuity break).

**Step 10 complete.** The two genuine holes (RAISING generic-COBOL0001, SYNC-on-group silent-accept) are wired loud.
The un-wireable residue is catalogued, not silently absent: `inline-method-invocation-2023` (pending row — no
distinctive token, OO-wave-owned) and NO SIGN of PACKED-DECIMAL (§E.3.2 item 5, not in the grammar → a `COBOL0307`;
gating it needs the grammar rule, deferred to the M4/2023 feature wave).

**Battery:** greenfield conformance **3108** · unit 227 · characterization 32 GREEN; FULL legacy guard NIST **353
MATCH**, legacy unit 1196, integration 608 GREEN. RESUME AT step 11 (close) — first the adversarial-verify pass.

