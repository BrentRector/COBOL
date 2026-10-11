## Entry 388 — M2-DATA-5 (Pointers) Phase-1 investigated; turnkey recipe recorded in the plan

Ran the `m2-data5-pointers-p1-investigation` workflow (spec + usage/sizing recon + SET/NULL/compare recon) and
recorded a precise, spec-corrected, turnkey Phase-1 recipe in plan §3.2. Phase-1 = `USAGE POINTER` (8-byte
opaque handle) + `NULL` (= handle 0) + `SET p TO NULL` / `SET p TO q` + `= NULL` / `= q` equality. **Key
finding: Phase-1 does NOT need the deferred handle→address `PointerRegistry` design decision** — that is only
for Phase-2 dereference (ADDRESS OF / BASED / ALLOCATE), which hits the .NET managed-memory problem and remains
owner-gated. **Two spec corrections to the original handoff slice:** (a) `VALUE NULL` is **prohibited** on
pointer items (§13.18.26 SR9) — reject a VALUE clause on a pointer rather than implement it; (b) pointer
comparison is **equality only** (= / NOT =). **Lean approach (minimal new code):** model `CobolCategory.Pointer`
(8 bytes); lower `SET p TO NULL|q` as a MOVE (NULL → `MoveFigurativeToField` 0x00×8 since
`FigurativeToByte(Null)=0x00`; pointer←pointer = 8-byte byte-copy); reuse the figurative-comparison path for
`= NULL` and `IrStringCompare` for `= q` — so no new IR/runtime compare nodes. 10 touch-points enumerated in the
plan. **Phase-1 is thin standalone** (every pointer can only be NULL until ADDRESS OF/ALLOCATE exist) — its
value is the foundation; the useful Phase-2 is the owner's design decision. No code changed; next tick implements
from the recipe.

