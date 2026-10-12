## Entry 640 — 2026-07-07 00:10 PDT — M2-OO-1h step 4: OCCURS INDEXED BY in METHOD data scope — a per-method index namespace — ⛔🎉 M2-OO-1h DATA-MODEL RESIDUE COMPLETE

The last of the four M2-OO-1h data-model shapes. Index-names registered into the flat GLOBAL `IndexFields` with a
de-dup guard (`DataBinder.cs:951`), so two methods each `INDEXED BY IX` shared ONE `_IX_0` cell — cross-method
torn aliasing (§11.7.4 GR5 sibling-privacy), and a method IX could not shadow an object IX with its own cell.

**A per-method index namespace:** `OoMethodDataScope.IndexFields` (index-name → cell). Registration routes via a
`_bindingMethodScope` context field set around the method's `BindEntries` calls — a method index-name gets a
FRESH `_MIX_{seq}` cell (never de-duped; distinct prefix so method/global never collide). Resolution is
scope-aware: `TryGetVisibleIndexField` consults the active method's cells first, and a new `IndexFieldFor(name)`
accessor (the active method's cell, then global) replaces the three raw `data.IndexFields[…]` dict reads in
SEARCH (`StatementBinder.cs:962/969/1011`). Emission follows the table's section: a method-WS table's cell is a
class **STATIC** (`OoStaticIndexCells` → `private static long`); a LOCAL/LINKAGE table's cell is a **per-activation
method local** (emitted in `OoEmitMethod`, reset to 1 each activation, §14.5.3).

Goldens: `oo_method_indexed_search` (a method LOCAL-STORAGE table + SEARCH + SET — FOUND-AT=3) and
`oo_method_indexed_two_methods` (the GR5 regression: MA sets its IX=2 and INVOKEs MB which sets its IX=3; on
return MA's IX is still 2 → E1(IX)="BB", not torn to "CC"), byte-exact, GreenfieldOnly. Battery: 123 corpus
goldens · full greenfield battery GREEN.

**⛔🎉 M2-OO-1h data-model residue is COMPLETE** — all four shapes live in method scope (RENAMES 637 / ODO 638 /
REDEFINES 639 / INDEXED 640). `OoGateUnsupportedShapes` now stages nothing (its four `COBOLNET0899` branches are
gone). The remaining M2-OO-1h legs — method own ENV/FILE/REPORT/SCREEN and EXTERNAL/GLOBAL on method WS — are the
separate follow-up **M2-OO-1i** (orthogonal subsystems, their loud gates in `OoBindMethodData` kept). Next per the
plan: M2-OO-1i + the low-severity M2-PRE follow-ups, then Phase 6 (OCCURS DYNAMIC, TYPEDEF, floats) / 7.

