## PB1636: the last derivation, DRV-GR-12.4.5.9.4-2, re-verified and parked for the owner's signature

Wave 1108 group C took kb/Work PB1636 (`status: half`) to finish its open half: inventory row GR-12.4.5.9.4-2,
§12.4.5.9.4 GR2, *"If the processor does not support record locking, record locks have no effect for the associated
file connector"* (`cite.py --check 12.4.5.9.4` OK, rule 2). The row closes only through a `docs/CONFORMANCE.md` §8
determination whose `Signed` cell is a dated owner signature (`inventory-schema.json` `derivation.signatures`,
refusal 5). No implementer can supply that, which is why the three dispatches after train 1042b could not land.

**Re-probe on this tree's build.** The population selector (`CONFORMS && GAP`) names this one row. The held draft
was an unpopulatable-antecedent argument, held because the L2 probe refuted it while record locks were per process.
Kb/Work PB2660 (relative, indexed), PB2692 (sequential) and PB2936 (every Unix other than Linux) have since landed,
and the PB2660/PB2692 two-process probe pairs, recompiled here, answer `51` to the second run unit on all three
organizations (relative and indexed: `READ WITH LOCK` 51, `REWRITE` 51, no lost update; sequential: `READ WITH LOCK`
51, `REWRITE` 43). The LINE SEQUENTIAL extension answers `51` to a second connector. `HostRegionLocks.HostOfThisProcess`
gives every host a cross-run-unit expression. Citations §9.1.16 and §9.1.13.8 1) re-checked OK. So the antecedent is
unpopulated and the draft stands, and no golden can witness the rule.

**What changed.** PB1636 moves from `status: half` to `status: owner`, which holds it out of the GAP campaign's
planner (`work.py cluster_order` now reports `waiting on status: owner`) and onto the ledger's owner-parked list. The
note carries the refreshed draft row, ready to paste into §8 once signed, and the bare question for the owner. The
draft names the current code sites and witnesses. The note's stale macOS caveat (written before PB2936 landed) is
corrected. No inventory row changed: GR-12.4.5.9.4-2 stays GAP, still claimed by PB1636, until the signature.
