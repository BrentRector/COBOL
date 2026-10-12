## Entry 429 — Stage-4 pointers: settled design (managed refs, ONE carrier) + grammar approved; corrections

Owner steered the pointer design across four points, all now settled and recorded: (1) pointers use **managed .NET
references** (`PointerRegistry` REJECTED — settled, NOT gated); (2) the deref needs **no `unsafe`** — a pointer's
owner is always a managed `byte[]` (because `ADDRESS OF` is classifier trigger-6), so deref is a bounds-checked
`Buffer.AsSpan(Offset, Length)` into the same byte-path runtime; (3) **ONE canonical carrier**, used for BOTH
`CALL … BY REFERENCE` and pointers — **no parallel type** (singular-pattern directive); (4) that one carrier is
**renamed `CobolDataPointer` → `ManagedPointer`** (owner's name choice — emphasizes the safe/GC nature; spelled-out,
not the ADR's abbreviated `ManagedPtr`). I **overrode the investigation's recommendation** to ADD a new `ManagedPtr`
alongside `CobolDataPointer`: its `object? Owner` argument is moot under trigger-6 (owner is always `byte[]`), so the
existing carrier's `byte[] Buffer` is exactly right — the resolution is a rename of the ONE type, not a second type.
Did the rename across the codebase (63 refs, file `CobolDataPointer.cs` → `ManagedPointer.cs`), guard-green. ADR §6
rewritten to one carrier; memories `feedback_singular_pattern` + `feedback_managed_pointers` added/updated.

A 3-agent investigation workflow mapped the seams (recorded in `RECORD_STRUCT_STORAGE_DESIGN.md §10`): Phase-1
`USAGE POINTER` is an 8-byte byte-window handle (not a managed ref); `SET ADDRESS OF … TO …` parses but `BindSet`
drops it; `ADDRESS OF`-as-sender has no grammar; `BASED` lexes as a generic noise word AND wrongly gets WS storage;
trigger 6 is deferred; `ALLOCATE`/`FREE` are unimplemented. **Owner APPROVED the grammar additions** (BASED clause +
`ADDRESS OF` sender + `SET ADDRESS OF` bind + pointer `SET UP/DOWN BY` + `ALLOCATE`/`FREE`).

Design + slices in §10: a typed `USAGE POINTER` flips to a `ManagedPointer` field; a byte-backed pointer keeps the
8-byte handle floor (NULL/compare-only — a managed ref can't losslessly round-trip a byte window, the first such
typed representation); a `BASED` item has no storage and derefs through a new **pointer-relative `IrLocation`**.
Slices: (1) BASED + ADDRESS OF + SET ADDRESS OF + deref core (scaffolding); (2) pointer arithmetic; (3) ALLOCATE/FREE.
This entry's commit is the rename + design/docs sync (guard re-run); NEXT: implement slice 1.

