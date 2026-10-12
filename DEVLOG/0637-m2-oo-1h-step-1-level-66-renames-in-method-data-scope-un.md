## Entry 637 — 2026-07-06 22:05 PDT — M2-OO-1h step 1: level-66 RENAMES in METHOD data scope — un-gated

Opened the M2-OO-1h data-model residue (extend REDEFINES / OCCURS INDEXED BY / OCCURS DEPENDING / level-66
RENAMES into a METHOD's data scope — currently staged loud COBOLNET0899 in `DataBinder.Oo.cs`
`OoGateUnsupportedShapes`). A recon workflow (wf_e97fb96d-65f, 4 readers + synthesis) produced the
decision-complete design, now in `docs/PHASE4_RECONCILIATION.md` (the M2-OO-1h section).

**The architectural finding (reframes the whole leg):** method items ARE in the program-scope `Roots` (`BindEntries`
does `Roots.Add` unconditionally), so the post-build passes DO traverse them — the fault is that `OoScopeSubtree`
moves method NAMES out of the global `ByName` before `BindResolve` runs, so name-based resolution breaks while
STRUCTURAL resolution still works. So the gates were CONSERVATIVE, not covering a real traversal gap. Gate-removal
order (smallest blast radius first): RENAMES → ODO → REDEFINES → INDEXED.

**Step 1 — RENAMES (this entry): no code fix needed beyond removing the gate.** `ResolveRedefines`'s RENAMES loop
resolves the alias FROM/THRU structurally via `FindDescendantOrSelf` over the owning record (`DataBinder.cs:1128-1152`),
independent of the name re-homing — so a method-scope level-66 alias was already correct; only the loud gate blocked
it. Verified end-to-end: golden `oo_method_renames` (a method LOCAL-STORAGE record with `66 BOTH RENAMES A-PART THRU
B-PART` = the 6-char span "ABCDEF", `66 JUSTA RENAMES A-PART` inheriting PIC XXX = "ABC", and a MOVE through the
no-THRU alias → REC "XYZDEF"), byte-exact, GreenfieldOnly. Battery: 118 corpus goldens · full greenfield battery
GREEN. Remaining M2-OO-1h steps 2-4 (ODO / REDEFINES / INDEXED) need the scope-aware resolution designed in the
reconciliation doc.

