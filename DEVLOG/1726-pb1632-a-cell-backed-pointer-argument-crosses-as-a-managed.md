## Entry 1726 — 2026-09-27 04:06 PDT — PB1632: a cell-backed pointer argument crosses as a managed slot, not a crash

**What.** `SET WP TO ADDRESS OF WP` followed by `CALL … BY CONTENT WP` crashed the COMPILER ("no character image for
category Pointer"). Taking a pointer's address, or declaring it BASED, puts it in a cell-backed area, where every
member is a `RedefViewPlace` and a managed member carries the `SlotWindow` coding. `CallPlaceIsManaged` excluded
every `RedefViewPlace` by shape, so the one crossing classifier `CrossingOf` sent the pointer to the Text arm.
**Fix.** The predicate now checks the coding, the same rule `CallPlaceIsVarGroup` applies to `VarGroupWindow`.
`PlaceRenderer.Read`/`Write` already render a slot window, so BY CONTENT, BY REFERENCE/RETURNING and INVOKE all
follow from the classifier. §14.8.2.3.3 2): a pointer formal conforms "as if a SET statement were performed".
**Golden.** `2002/pb1632_pointer_arg_cell_backed`: a self-addressed pointer, a BASED pointer and a cell-backed
program-pointer, each BY CONTENT and BY REFERENCE. The expected lines are derived from §14.8.2.3.3 2) and §14.2.3
GR8/GR9. The design doc (`COBOLNET_INTERPROGRAM_DESIGN.md`) states the coding rule.
**Found on the way → PB1633 (new, wrong answer):** `SET PP TO ADDRESS OF PROGRAM "n"` for a program n CONTAINED in
the setting program yields NULL. The ENTRY spelling does the same.
**Gate.** Characterization 33/33; Conformance (Call, Pointer, CorpusRunner, Drift, Invoke, Based, Address)
3818/3818; Unit 29491/29491.

