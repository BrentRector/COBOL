## Entry 638 — 2026-07-06 22:40 PDT — M2-OO-1h step 2: OCCURS DEPENDING ON in METHOD data scope — scope-aware resolution

Step 2 of the M2-OO-1h data-model residue. OCCURS … DEPENDING ON in method data was staged loud because
`OdoResolve` resolved data-name-1 through the raw global `ByName` (`OdoModel.cs:239`), which — after
`OoScopeSubtree` moves method names out — either false-`COBOLNET0851`'d ("data-name-1 is not defined") or bound a
same-named PROGRAM item (a torn read of the wrong count, §13.18.38 GR8).

**The shared B.0 infrastructure (also feeds step 3 REDEFINES):** `DataBinder.OoRootOwner` (method root → owning
method symbol, populated in the `OoBindMethodData` root loop) + `LookupDataInScopeOf(anchorRoot, name)` — the
owning method's `DataScope.ByName` first (§11.7.4 GR5 shadowing), then the class/program globals (a method may
reference a visible, unshadowed object name). This is the post-build twin of `LookupData` (which keys off
`ActiveMethodScope`, null during `BindResolve`).

**The fix:** `OdoResolve` replaces the raw `ByName` lookup with `LookupDataInScopeOf(RootOf(item), depName)`; all
downstream SR checks (SR16/SR17/SR2/SR20) are structural and unchanged. Removed the ODO gate. Golden
`oo_method_odo` (a method LOCAL-STORAGE `OCCURS 1 TO 5 DEPENDING ON CNT` with CNT method-local: a whole-group send
honors the current extent — `TBL=ABC` at CNT=3, `TBL2=AB` at CNT=2), byte-exact, GreenfieldOnly. +1 OoSpine unit
(`MethodOdo_DependingOnVisibleObjectData` — the depending-name resolving to a VISIBLE OBJECT item via the global
fallback, GR5). Battery: 119 corpus goldens · full greenfield battery GREEN. Remaining: step 3 REDEFINES (reuses
B.0), step 4 INDEXED.

