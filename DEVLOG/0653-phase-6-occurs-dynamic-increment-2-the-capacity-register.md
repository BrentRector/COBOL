## Entry 653 — 2026-07-07 02:18 PDT — Phase 6: OCCURS DYNAMIC increment 2 — the CAPACITY register (read) + SET Format 14 (TO/UP BY/DOWN BY)

Increment 2 of **OCCURS DYNAMIC** (ISO/IEC 1989:2023 §13.18.38 GR15 / §8.5.1.9.1 / §14.9.39; data-model **D9**): the
run-time capacity is now READABLE (the CAPACITY register) and WRITABLE (SET Format 14). Greenfield-only — no shared
frontend/`.g4` touched, so the greenfield battery (not the full legacy guard) is the gate.

**Recon-first (ultracode cadence).** A background workflow (wf_973560a9-bb6 — 6 parallel readers → an xhigh synthesis,
761k subagent tokens) produced a decision-complete code-integration map for increments 2–5. It CONFIRMED the approach
and caught three things a design-only read would have missed, all incorporated: (a) the register-as-receiver **1523**
must be a bind-time chokepoint in `ResolveReceiving`, else a `MOVE … TO reg` reaches `CapacityRegisterPlace.Write` and
throws a compiler crash; (b) a dedicated `TablePath` helper (null if ANY ancestor is a table) is safer than
`AccessPath([])` (which mishandles a dynamic-table ancestor); (c) a whole (unsubscripted) dynamic-table reference
would otherwise fold to a `MemberPlace` wrapping the bare `CobolDynTable<T>` object → uncompilable C#.

**Landed.** (1) **CAPACITY register READ** — a new `CapacityRegisterPlace(TablePath, RegisterItem) : Place` whose
`Read()` emits `{tablePath}.Capacity` (the runtime `long`); a `DataBinder.DynamicResolve` post-build pass (run right
after `OdoResolve`) synthesizes the implicitly-defined register (SR30) — an unsigned-integer native-binary `PicInfo`
(`BinaryLong`, 10 digits ≥ the `CobolDynTable` implementor max) — and indexes it in `DataBinder.CapacityRegisters`
(name → owning table); an early `ReferenceResolver.Resolve` hook (mirroring LINAGE-COUNTER) + the new
`ReferenceResolver.TablePath` whole-table-path helper build the place. The register reads as a scale-0 integer through
the numeric pipeline's plain branch (no profile needed there); its `NumProfile` IS emitted (a profile, NOT a field —
`FieldEmitter`) for the DISPLAY / `FormatDisplay` path (the one CS0103 hazard the recon flagged — caught by
compile-AND-run, the DEVLOG-651 lesson). So the register works everywhere an integer does: MOVE source (incl. to a
numeric-edited item), relational operand, arithmetic operand. (2) **SET Format 14** (`SET reg {TO|UP BY|DOWN BY} n`) —
a bind-time reroute in `BindSetTo`/`BindSetUpDown` on a `CapacityRegisterPlace` first target (the pointer-reroute
pattern), → `BoundSetCapacity(TablePath, amount, Kind)` → `SetCapacity`/`CapacityUpBy`/`CapacityDownBy`
(raise/lower/seed, clamp-to-FROM on lowering). (3) Diagnostics **1523** (register as an ordinary receiver — the
`ResolveReceiving` chokepoint; and the SR30 register-name collision in `DynamicResolve`) and **1524** (SET F14 with a
second/mixed target). A whole unsubscripted dynamic-table reference now fails LOUD (a `PlaceForItem` guard) — a
`NotImplemented` runtime stub for now; inc 5 upgrades it to a bind-time **1527**.

**Deferred, with rationale (process rule):** (a) **EC-BOUND-OVERFLOW** on exceeding the TO/expected capacity is
NONFATAL and checking-gated — with runtime checking off (the default) the operation continues either way (identical
observable behavior), so the runtime `_expected` enforcement + the `>>TURN … CHECKING ON` gate ride the EC-integration
pass (`dyn_capacity_bounds` proves the checking-off continue: SET past TO 4 → capacity 9). (b) **FUNCTION LENGTH over a
dynamic table** (= `Capacity × elemWidth`) + the `DynWholeTablePlace` whole-table place belong with the inc-5 §14.6.9
**1527** whole-/variable-length-group work; inc 2 ships the clean whole-table loud guard as the interim. Both recorded
in the D9 deep-dive.

**Also recorded in D9 (a load-bearing correction for inc 3):** the D9 sketch `CobolTable.At(CobolDynTable, occ,
receiving)` is WRONG — a single `MemberPlace` path cannot carry read-vs-write polarity (`ref T` covers the fixed case
only). The corrected design is an `AccessDir { Sending, Receiving }` threaded through `AccessPath` + a
`DynTablePlace(SendingPath, ReceivingPath)` whose `Read()`→`RefSending` and `Write()`→`RefReceiving` (grow-and-seed).

**Verified**: `dyn_capacity_read` (register reads FROM=3; MOVE-to-edited `CAP=[  3]`; `= 3`; `× 2 = 0006`),
`dyn_capacity_set` (TO 7 → UP BY 2 = 9 → DOWN BY 3 = 6 → DOWN BY 100 clamps to FROM 2), `dyn_capacity_bounds` (SET past
TO 4 → 9, checking-off). Greenfield battery green (Conformance + 213 unit). Diagnostic band: 15xx now through 1524.

