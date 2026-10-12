## Entry 550 — 2026-06-10 15:40 PDT — RENAMES/REDEFINES layout closeout → NC252A byte-matches (87/95)

NC252A (the REDEFINES + RENAMES feature program) needed four layered fixes, verified in an isolated worktree
because the wave-2 agents were already dropping files into the main tree (the established pattern: agents never
build; the orchestrator's tree is theirs to fill):
1. **Nested classes dissolve** (the `REDEF11._redef_RDF8` CS0103): a REDEFINES class whose ANCHOR lies inside
   another class's subtree (RDEF8 under the view REDEF11) shares THAT storage — the inner class object is removed
   and the outer subtree walk assigns every nested item its window over the ONE backing (an inner redefiner
   starts at its target's already-assigned offset).
2. **No-THRU RENAMES is an ALIAS** (§13.18.45 GR1 — same description as the renamed item): the resolver forwards
   to the FROM item's place outright, so `ADD 3500 TO RENAME-12` over a PIC 9(4) stays numeric; only the THRU
   form composes the alphanumeric span (GR2).
3. **THRU spans complete**: an OCCURS leaf contributes EVERY occurrence; a typed numeric-DISPLAY leaf
   participates through its character image via the NEW `CobolNum.StoreDisplay`/`FormatDisplay(string,…)`
   overload sets — the storage-form BRIDGE (the CobolTable.Occ pattern): whether the leaf field ends up native
   long/Int128 or an image-stored string is decided by the post-bind whole-group analysis AFTER the expression
   text is produced; C# overload resolution picks the conversion at backend-compile time. NumericImagePlace's
   write now rides the same bridge. Qualified RENAMES operands take the base word only.
4. **Group width/image exclude REDEFINER children** (§13.18.44 — an overlay adds no storage): REDEF10 is 46
   characters, not 46 + its RDF3 overlay subtree; both `DataItem.ImageWidth` and the backing-image composer were
   inflating the area and shifting every window after the overlay point. (A stale-build mirage hid this fix for
   one round — the main tree had stopped building under the incoming agent files; the worktree proved it.)

**87/95 NC GREEN (zero lost), 569 conformance + 15 unit (worktree-verified).** Remaining 8: NC105A (DIFF 34 — its
group-move rows share family-4's class of bug but through whole-group MOVE paths), NC107A/108M (DECIMAL-POINT
COMMA), NC235A/247A (ODO — agent in flight), NC236A (spec-pinned), NC214M/303M (no-golden by design).

