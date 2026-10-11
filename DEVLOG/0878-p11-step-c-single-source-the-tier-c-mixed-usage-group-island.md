## Entry 878 — 2026-07-17 13:35 PDT — P11 Step C — single-source the Tier-C mixed-usage-group island reason (TierCIsland); exit criterion 2 MET; Step D deferred

The Tier-C DECISION (P11 exit criterion 2): the rejection is single-sourced. The re-scout reshaped the plan
substantially — the phase doc's "~10 scattered guards, single-source them, TierCWindow.Read/Write backstop,
RedefinesClassifier type" premise was stale. Reality (scout §code:tier-c-code):

- **The REDEFINES-CLASS Tier-C rejection was ALREADY single-sourced.** A genuine mixed-USAGE REDEFINES pun
  (`RedefinesTier.ByteCanonical`) is rejected in ONE place — `DataBinder.ComputeTier` (whose reason arms carry
  the ISO citations) through the ONE `RedefinesClass.Classify` mutator, threaded to references by
  `ExpressionBinder.RefFailure`. `ByteCanonical` is dead-by-construction (ComputeTier never returns it);
  `StorageForm.TierCWindow` is quarantined (no Read/Write — StorageForm is a pure classification record; the
  de-facto backstop is `ReferenceResolver.PlaceForItem` → null). There is NO `RedefinesClassifier` type.
- **What was scattered is the SEPARATE classless mixed-usage-GROUP image island** — a plain group (not a
  REDEFINES pun) with a float/COMP-5/INDEX/BINARY-* leaf (`IsImageCapable`, P1) or a not-promoted COMP/binary
  leaf (`IsCharacterImage`, P2) has no whole-group character image, so ~12 emit verbs staged loud with
  copy-pasted, DRIFTED text ("byte island" vs "byte path", ±§4.2). Step C routes every guard through the ONE
  `Binding/Model/TierCIsland.Reason` source, **PRESERVING each site's own predicate** (P1 vs P2 — no lossy
  single-predicate collapse) and its lead + offending-leaf descriptor. Sites (12): OperandText (whole-group
  image), MoveEmitter ×2, StringEmitter ×2, AcceptDisplayEmitter ×2, InspectEmitter, CallEmitter,
  SequentialIoEmitter ×2, SortEmitter. The OO/SORT/UDF bind-time conformance guards keep their context-specific
  messages by design (UdfBinder's Binary/Packed strictness is deliberate).
- NEW `TierCRejectionTests` (6 facts) locks that every shape STILL fails loud through the one reason (the
  "Tier-C" substring) — the collapse is behavior-neutral (no test pinned the exact text; verified by grep).
- `DESIGN-data-model.md` §2.3 re-based to the AS-BUILT reality (correcting the stale RedefinesClassifier /
  init-only Tier/Width / TierCWindow.Read-Write sketch), and records Step D as a DEFERRED, to-be-re-designed
  increment.

**Step D (the confined `byte[]` codec) DEFERRED** per the phase doc's own decision gate. It is the one
sanctioned `byte[]` boundary of invariant #1, but its §2.5/§3 design does not match reality (ByteCanonical
dead, TierCWindow no Read/Write, no RedefinesClassifier) and no NIST program requires it — a properly-designed
future wave, not a rushed P11 tail. When it lands, ComputeTier's arm 1 flips ByteCanonical from Rejected to
modeled and the ONE TierCIsland predicate admits the class automatically (the payoff of the single source).

**ALL FIVE P11 EXIT CRITERIA MET:** (1) zero Deferred; (2) Tier-C decided [rejection single-sourced]; (3)
value goldens; (4) window/disposition negatives; (5) full battery green. Battery: conformance **3521/3521**
(+6 TierCRejectionTests) · unit **301/301** · legacy 1196+636 · guard-fast **0 regressions, ALL GREEN —
first pass, no flake**. No golden flips.

