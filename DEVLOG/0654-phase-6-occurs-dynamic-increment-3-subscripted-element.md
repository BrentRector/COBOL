## Entry 654 — 2026-07-07 02:27 PDT — Phase 6: OCCURS DYNAMIC increment 3 — subscripted element access + implicit growth (RefSending/RefReceiving)

Increment 3 of **OCCURS DYNAMIC** (ISO/IEC 1989:2023 §8.5.1.9.2/.9.3; data-model **D9**): a subscripted dynamic-table
element now reads and writes, and a RECEIVING subscript past the current capacity GROWS the table. Greenfield-only.

**The load-bearing correction (flagged by the inc-2 recon, recorded in D9).** The original D9 sketch —
`CobolTable.At(CobolDynTable, occ, receiving)`, a `receiving` bool baked into the access path — is WRONG: a single
`MemberPlace` holds ONE path string used for BOTH `Read()` (=path) and `Write()` (=path=rhs); baking one direction
breaks the other (a `receiving:false` read never grows; a `receiving:true` bake grows on every sending read, which
must be benign scratch). A fixed table escapes this because `CobolTable.At` returns a `ref T` that serves both
directions — but a dynamic table has two DISTINCT accessors (`RefSending` benign-scratch-on-OOB vs `RefReceiving`
grow-and-seed). So the direction can only be resolved at Read-vs-Write time, by two pre-computed paths.

**Implemented.** (1) `enum AccessDir { Sending, Receiving }` + `AccessPath(item, indexExprs, dir = Sending)`: the
OCCURS-level arity count switches from `Occurs is not null` to `IsTable` (a dynamic level, whose `Occurs` is null,
now counts as a dimension); a fixed OCCURS segment still renders `CobolTable.At(path, idx)` (unchanged — `ref T`
serves both directions), a dynamic segment renders `{path}.RefSending(idx)` (sending) or `{path}.RefReceiving(idx)`
(receiving), with any group-field / fixed-OCCURS tail appended after the accessor (`{tbl}.RefSending(i).Field`).
(2) A new `DynTablePlace(SendingPath, ReceivingPath, Item) : Place` — `Read()` emits the sending path, `Write(rhs)`
the receiving path (`{ReceivingPath} = {rhs};`). Because `RefReceiving` returns `ref T`, a `.Field = rhs` on a group
element mutates the array slot in place. (3) `PlaceForItem` detects a dynamic level on the item's chain and builds
both paths → `DynTablePlace`; the whole-table (unsubscripted) loud guard from inc 2 still catches
`indexExprs.Count == 0`.

A sending out-of-range subscript stays benign scratch (checking-off, the COBOL-85 default, matching `CobolTable.At`);
EC-BOUND-SUBSCRIPT-under-checking rides the general subscript-checking gate (a cross-cutting later increment, not
dyn-specific) — recorded in D9.

**Verified**: `dyn_implicit_grow` (FROM 2; `MOVE 42 TO ELT(5)` grows to 5 seeding occ 3–4=000, `ELT(5)=042`, and a
sending `ELT(9)` read past capacity is benign `000` — proving sending ≠ receiving), `dyn_initialized` (a GROUP element
`OCCURS DYNAMIC FROM 3 INITIALIZED` with VALUE clauses: occ 1 & 3 keep the seeded `----`/`07`, occ 2 overwritten to
`ABCD`/`99` — the RefSending/RefReceiving accessor + the group-field tail both directions, and every occurrence seeded
with the one-occurrence image). The `AccessPath` `IsTable`-arity change does NOT regress fixed tables (full greenfield
conformance green). Greenfield battery: 213 unit + conformance green.

