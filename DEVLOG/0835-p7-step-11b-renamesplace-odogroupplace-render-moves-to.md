## Entry 835 — 2026-07-12 01:55 PDT — P7 Step 11b: RenamesPlace + OdoGroupPlace render moves to PlaceRenderer (+ the top-down rule)

**What.** The first two `Place` subtypes lose their C# render text: `RenamesPlace` (the level-66 alias concat +
the multi-leaf distribute-store, §13.18.45) and `OdoGroupPlace`'s three GR8 seams (`LengthExpr`/`SendingImage`/
`ReceiveInto`, §13.18.38). Their rendering now lives in `PlaceRenderer` arms over the records' structure
(inner/leaf `Place`s + the ODO int fields), through two new `RuntimeApi` anchors (`StrSpliceInto`, `TableOdoExtent`);
the record methods became the `RenderedElsewhere()` tripwire. Both are top-level results — nothing ever wraps them —
so throwing their legacy render is safe.

**The load-bearing lesson (a red I caught and fixed before commit): migrate OUTERMOST-first.** I first also migrated
`NumericImagePlace` (throw), and 4 conformance tests went red (NC224A — ref-mod over a numeric-DISPLAY item). Cause:
an UN-migrated wrapper renders by calling its inner's `Place.Read()` DIRECTLY (it lives in `Binding`, so it cannot
route through the `CodeGen` `PlaceRenderer`), and a still-legacy `RefModPlace` over a `NumericImagePlace` hit the
throw. So a subtype may replace its legacy render with the tripwire ONLY once nothing un-migrated wraps it. The
wraps-DAG dictates the order: top-level results (`RefMod`/`Renames`/`Odo`) → `NumericImagePlace` (wrapped only by
`RefMod`/`Renames`) → the leaves. Reverted `NumericImagePlace` to legacy (it migrates WITH `RefModPlace`); recorded
the rule in the PHASE-07 Step 11 plan. **Transparency:** the characterization snapshots stayed green through the bad
state — the 32 snapshots don't exercise numeric ref-mod / RENAMES-numeric, so the 3166-case conformance differential
is what caught it (a reminder the snapshots are a necessary-not-sufficient byte guard).

**Battery green:** characterization 33/33 (byte-exact + ratchet), unit 281/281, conformance 3166/3166. NEXT: the
index infrastructure (subscript → `BoundExpr` + the byte-exact index renderer), then `RefModPlace`.

